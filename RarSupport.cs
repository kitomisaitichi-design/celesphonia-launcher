using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

// This RAR5 extractor is bundled into the EXE so the portable release retains
// the current fixed-file allowlist and its legacy-updater-safe layout.
// The original, version-verified creator archive is never modified.
public static class RarSupport {
    const string ExeResource = "Celesphonia.Rar7z.exe";
    const string DllResource = "Celesphonia.Rar7z.dll";
    static byte[] Embedded(string resource) {
        using(var input=typeof(RarSupport).Assembly.GetManifestResourceStream(resource)) {
            if(input==null)throw new IOException("The launcher is missing its verified RAR5 extractor: "+resource);
            using(var output=new MemoryStream()){input.CopyTo(output);return output.ToArray();}
        }
    }
    static string Run(string exe,string arguments) {
        var config=new ProcessStartInfo(exe,arguments) {
            UseShellExecute=false,CreateNoWindow=true,
            RedirectStandardOutput=true,RedirectStandardError=true,
            StandardOutputEncoding=new UTF8Encoding(false),
            StandardErrorEncoding=new UTF8Encoding(false)
        };
        using(var p=Process.Start(config)) {
            var error=p.StandardError.ReadToEndAsync();
            string output=p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if(p.ExitCode!=0)throw new IOException("RAR extraction/listing failed: "+error.Result+" "+output.Substring(Math.Max(0,output.Length-3000)));
            return output;
        }
    }
    static List<string> SafeEntries(string listing,string target) {
        int start=listing.IndexOf("----------",StringComparison.Ordinal);
        if(start<0)throw new IOException("RAR archive has no readable file inventory.");
        string[] rows=listing.Substring(start).Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
        var entries=new List<string>();
        var unique=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(string row in rows) {
            if((row.StartsWith("Symbolic Link = ",StringComparison.Ordinal)
                && row.Substring("Symbolic Link = ".Length).Trim().Length>0)
               ||(row.StartsWith("Hard Link = ",StringComparison.Ordinal)
                && row.Substring("Hard Link = ".Length).Trim().Length>0)
               ||(row.StartsWith("Copy Link = ",StringComparison.Ordinal)
                && row.Substring("Copy Link = ".Length).Trim().Length>0))
                throw new IOException("RAR archive links are not supported.");
            if(!row.StartsWith("Path = ",StringComparison.Ordinal))continue;
            string name=row.Substring(7).Replace('/','\\').TrimEnd('\\');
            if(name.Length==0 || name.IndexOf('\0')>=0 || name.StartsWith("\\",StringComparison.Ordinal)
               || Path.IsPathRooted(name) || name.Contains(":")
               || name.Split('\\').Any(x=>x==""||x=="."||x==".."))
                throw new IOException("RAR archive contains an unsafe path.");
            Store.Child(target,name); // resolve and constrain before extraction
            if(!unique.Add(name))throw new IOException("RAR archive contains duplicate or case-colliding paths.");
            entries.Add(name);
        }
        if(entries.Count==0 || entries.Count>100000)
            throw new IOException("RAR file inventory is empty or unexpectedly large.");
        return entries;
    }
    public static bool Supported(string archive) {
        string format=FileDiscovery.ArchiveFormat(archive);
        return format=="rar"||format=="7z"||archive.EndsWith(".001",StringComparison.OrdinalIgnoreCase);
    }
    public static List<FoundSource> ProbeFingerprints(string archive,CancellationToken cancellation) {
        var found=new List<FoundSource>();
        if(!Supported(archive))return found;
        cancellation.ThrowIfCancellationRequested();
        string owned=Path.Combine(Path.GetTempPath(),"CphRAR-"+Guid.NewGuid().ToString("N"));
        string staging=Store.ArchiveStaging("verify");
        Directory.CreateDirectory(owned);
        try {
            string exe=Path.Combine(owned,"7z.exe"),dll=Path.Combine(owned,"7z.dll");
            File.WriteAllBytes(exe,Embedded(ExeResource));
            File.WriteAllBytes(dll,Embedded(DllResource));
            var entries=new HashSet<string>(SafeEntries(Run(exe,"l -slt -p- -sccUTF-8 "+Store.Q(archive)),staging)
                .Select(x=>x.Replace('\\','/')),StringComparer.OrdinalIgnoreCase);
            string suffix="data/System.json";
            var prefixes=entries.Where(x=>x.EndsWith(suffix,StringComparison.OrdinalIgnoreCase))
                .Select(x=>x.Substring(0,x.Length-suffix.Length))
                .Where(x=>entries.Contains(x+"js/plugins.js"))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(prefixes.Length!=1)return found;
            string prefix=prefixes[0];
            var recipes=RecipeSupport.Catalog().Recipes.Where(x=>x.Fingerprints!=null&&x.Fingerprints.Count>=3
                &&x.Fingerprints.Keys.All(marker=>entries.Contains(prefix+marker))).ToArray();
            bool completeGame=entries.Any(x=>x.EndsWith("package.json",StringComparison.OrdinalIgnoreCase))
                &&entries.Any(x=>x.EndsWith("Game.exe",StringComparison.OrdinalIgnoreCase)||x.EndsWith("nw.exe",StringComparison.OrdinalIgnoreCase));
            var games=completeGame?UniversalSupport.ReadIndex().Versions.Where(x=>x.Fingerprints!=null
                &&x.Fingerprints.Keys.All(marker=>entries.Contains(prefix+marker))).ToArray():new VersionBuild[0];
            if(recipes.Length==0&&games.Length==0)return found;
            string[] markers=recipes.SelectMany(x=>x.Fingerprints.Keys)
                .Concat(games.SelectMany(x=>x.Fingerprints.Keys))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x=>entries.First(e=>e.Equals(prefix+x,StringComparison.OrdinalIgnoreCase))).ToArray();
            cancellation.ThrowIfCancellationRequested();
            Directory.CreateDirectory(staging);
            Run(exe,"x -y -p- -sccUTF-8 -bso0 -bsp0 -o"+Store.Q(staging)+" "+Store.Q(archive)+" "+
                string.Join(" ",markers.Select(Store.Q).ToArray()));
            Store.CheckLinks(staging);
            if(!completeGame)foreach(var recipe in recipes) {
                cancellation.ThrowIfCancellationRequested();
                if(!recipe.Fingerprints.All(mark=>{
                    string path=Store.Child(staging,(prefix+mark.Key).Replace('/','\\'));
                    return File.Exists(path)&&new FileInfo(path).Length<=64L*1024*1024&&Store.Hash(path)==mark.Value;
                }))continue;
                found.Add(new FoundSource{Kind="Original archive",Path=archive,RecipeId=recipe.Id,
                    Name=recipe.Name+" · RAR internal version hashes match",
                    Detection="RAR signature / extracted version fingerprints; full payload verified before install"});
                break;
            }
            if(found.Count==0)foreach(var game in games){
                cancellation.ThrowIfCancellationRequested();
                if(!game.Fingerprints.All(mark=>{
                    string path=Store.Child(staging,(prefix+mark.Key).Replace('/','\\'));
                    return File.Exists(path)&&new FileInfo(path).Length<=64L*1024*1024&&Store.Hash(path)==mark.Value;
                }))continue;
                found.Add(new FoundSource{Kind="Full game archive",Path=archive,RecipeId=game.Id,
                    Name=game.Name+" · native game files match",
                    Detection="Embedded archive structure / version fingerprints; complete files checked after extraction"});
                break;
            }
            return found;
        } finally {
            if(Directory.Exists(staging))Store.CleanArchiveStaging(staging);
            string tempRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\')+"\\";
            if(owned.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase)
                && Regex.IsMatch(Path.GetFileName(owned),@"^CphRAR-[a-f0-9]{32}$")) {
                try{Directory.Delete(owned,true);}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }
    }
    public static string Extract(string archive,string target,bool allowPartial,bool raw) {
        if(!Supported(archive))
            throw new IOException("Choose a ZIP, RAR, 7z or first split .001 archive.");
        string owned=Path.Combine(Path.GetTempPath(),"CphRAR-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(owned);
        try {
            string exe=Path.Combine(owned,"7z.exe"),dll=Path.Combine(owned,"7z.dll");
            File.WriteAllBytes(exe,Embedded(ExeResource));
            File.WriteAllBytes(dll,Embedded(DllResource));
            string inventory=Run(exe,"l -slt -p- -sccUTF-8 "+Store.Q(archive));
            SafeEntries(inventory,target);
            Run(exe,"x -y -p- -sccUTF-8 -bso0 -bsp0 -o"+Store.Q(target)+" "+Store.Q(archive));
            Store.CheckLinks(target);
            return raw?target:allowPartial?RecipeSupport.ContentRoot(target):Store.FindOverlay(target);
        }finally {
            string tempRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\')+"\\";
            if(owned.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase)
               && Regex.IsMatch(Path.GetFileName(owned),@"^CphRAR-[a-f0-9]{32}$")) {
                try { Directory.Delete(owned,true); }catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }
    }
}
