using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Security.Cryptography;

public class DeltaFile {
    public string Path { get; set; }
    public long Size { get; set; }
    public string Hash { get; set; }
    public string Source { get; set; }
    public string SourceHash { get; set; }
    public long Prefix { get; set; }
    public long Suffix { get; set; }
    public string Payload { get; set; }
}
public class DeltaManifest {
    public int Format { get; set; }
    public string Name { get; set; }
    public string BaseDescription { get; set; }
    public List<DeltaFile> Files { get; set; }
}
public sealed class DigestSink : Stream {
    SHA256 digest = SHA256.Create();
    public long Count;
    public string Finish() { digest.TransformFinalBlock(new byte[0], 0, 0); return BitConverter.ToString(digest.Hash).Replace("-", "").ToLowerInvariant(); }
    public override void Write(byte[] b, int o, int n) { digest.TransformBlock(b, o, n, b, o); Count += n; }
    public override bool CanRead { get { return false; } } public override bool CanSeek { get { return false; } } public override bool CanWrite { get { return true; } }
    public override long Length { get { return Count; } } public override long Position { get { return Count; } set { throw new NotSupportedException(); } }
    public override void Flush() { } public override int Read(byte[] b,int o,int n) { throw new NotSupportedException(); } public override long Seek(long n,SeekOrigin o) { throw new NotSupportedException(); } public override void SetLength(long n) { throw new NotSupportedException(); }
    protected override void Dispose(bool disposing) { if (disposing) digest.Dispose(); base.Dispose(disposing); }
}
public static class DeltaSupport {
    static Dictionary<string,string> hashCache = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    static string Hash(string p) { string h; if (!hashCache.TryGetValue(p,out h)) hashCache[p] = h = Store.Hash(p); return h; }
    static string Relative(string root,string path) { return path.Substring(root.TrimEnd('\\').Length + 1).Replace('\\','/'); }
    public static string Resolve(string basis,string source) {
        if (source.StartsWith("content/")) return EnglishSupport.OriginalFile(basis,Store.Child(Store.WebRoot(basis),source.Substring(8).Replace('/','\\')));
        if (source.StartsWith("runtime/")) return EnglishSupport.OriginalFile(basis,Store.Child(basis,source.Substring(8).Replace('/','\\')));
        throw new IOException("Invalid delta source path.");
    }
    public static IEnumerable<string> Candidates(string basis,string relative,string webRelative) {
        string prefix=webRelative.Trim('/')+"/";
        bool runtime=relative.StartsWith("locales/",StringComparison.OrdinalIgnoreCase)||relative.StartsWith("swiftshader/",StringComparison.OrdinalIgnoreCase)||relative.IndexOf('/')<0&&(new[]{".exe",".dll",".pak",".bin",".dat",".ini",".ico"}.Contains(Path.GetExtension(relative).ToLowerInvariant())||relative.Equals("package.json",StringComparison.OrdinalIgnoreCase)||relative.Equals("steam_appid.txt",StringComparison.OrdinalIgnoreCase));
        string token=webRelative.Length==0?(runtime?"runtime/":"content/")+relative:relative.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)?"content/"+relative.Substring(prefix.Length):"runtime/"+relative;
        string ext = System.IO.Path.GetExtension(token).ToLowerInvariant();
        string alternate = ext == ".rpgmvp" ? ".png" : ext == ".rpgmvo" ? ".ogg" : ext == ".rpgmvm" ? ".m4a" : ext==".png"?".rpgmvp":ext==".ogg"?".rpgmvo":ext==".m4a"?".rpgmvm":null;
        // Plain CH assets are stable across encryption-key changes; prefer them when possible.
        if (alternate != null) { string alt = token.Substring(0,token.Length-ext.Length)+alternate; if(File.Exists(Resolve(basis,alt))) yield return alt; }
        if(File.Exists(Resolve(basis,token))) yield return token;
    }
    public static IEnumerable<string> GameFiles(string root) {
        string web = Store.WebRoot(root);
        var runtimeExtensions = new HashSet<string>(new[] { ".exe", ".dll", ".json", ".pak", ".bin", ".dat", ".html", ".ico", ".ini" });
        foreach(string file in Directory.GetFiles(root)) if(runtimeExtensions.Contains(System.IO.Path.GetExtension(file).ToLowerInvariant())||Path.GetFileName(file).Equals("steam_appid.txt",StringComparison.OrdinalIgnoreCase)) yield return file;
        foreach(string folder in Directory.GetDirectories(root)) {
            string name = System.IO.Path.GetFileName(folder).ToLowerInvariant();
            if(name == "www" || name == "locales" || name == "swiftshader" || name == "audio" || name == "data" || name == "fonts" || name == "img" || name == "js" || name == "movies" || name == "css" || name == "icon") {
                foreach(string file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories)) {
                    string rel=Relative(root,file);
                    if(rel.Split('/').Any(x => x == "save" || x == "output" || x == "decrypted" || x == "celesphonia-english")) continue;
                    if(Store.IsSave(file) || System.IO.Path.GetExtension(file).Equals(".docx",StringComparison.OrdinalIgnoreCase)) continue;
                    yield return file;
                }
            }
        }
    }
    public static long[] Common(string a,string b) {
        using(var source=File.OpenRead(a)) using(var target=File.OpenRead(b)) {
            long limit=Math.Min(source.Length,target.Length), prefix=0,suffix=0;
            byte[] x=new byte[65536],y=new byte[65536];
            while(prefix<limit) {
                int n=(int)Math.Min(x.Length,limit-prefix); source.Read(x,0,n); target.Read(y,0,n);
                int i=0; while(i<n && x[i]==y[i]) i++; prefix+=i; if(i<n) break;
            }
            long remaining=limit-prefix;
            while(suffix<remaining) {
                int n=(int)Math.Min(x.Length,remaining-suffix);
                source.Position=source.Length-suffix-n; target.Position=target.Length-suffix-n; source.Read(x,0,n);target.Read(y,0,n);
                int i=0; while(i<n && x[n-1-i]==y[n-1-i]) i++; suffix+=i;if(i<n)break;
            }
            return new[] {prefix,suffix};
        }
    }
    public static void CopyPart(Stream input,Stream output,long start,long size) {
        input.Position=start;byte[] b=new byte[65536];
        while(size>0) {int n=input.Read(b,0,(int)Math.Min(size,b.Length));if(n==0)throw new EndOfStreamException();output.Write(b,0,n);size-=n;}
    }
    public static void Build(string basis,string config) {
        hashCache.Clear();
        var settings=Store.Json.Deserialize<Settings>(File.ReadAllText(config,Encoding.UTF8));
        string packs=System.IO.Path.Combine(Store.Home,"Delta Packs");Directory.CreateDirectory(packs);
        var report=new List<object>();
        foreach(var p in settings.Profiles.Where(x => x.Id != "ch-current")) {
            string targetWeb=Store.WebRoot(p.Root);
            string pack=System.IO.Path.Combine(packs,p.Id+".cgdelta.zip");
            var files=GameFiles(p.Root).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(x=>Relative(p.Root,x),x=>x,StringComparer.OrdinalIgnoreCase);
            report.Add(BuildFiles(basis,p.Name,pack,Path.GetFullPath(p.Root).TrimEnd('\\').Equals(targetWeb.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)?"":Relative(p.Root,targetWeb),files));
            File.WriteAllText(System.IO.Path.Combine(Store.Home,"delta-build-report.json"),Store.Json.Serialize(report),Encoding.UTF8);
        }
    }
    static object BuildFiles(string basis,string name,string pack,string webRelative,Dictionary<string,string> files) {
            var manifest=new DeltaManifest {Format=1,Name=name,BaseDescription="CH installation matching the verified reused files; absolute installation paths are not required",Files=new List<DeltaFile>()};
            long bytes=0,payloadBytes=0; int reused=0,spliced=0,full=0;
            using(var zip=new ZipArchive(File.Create(pack),ZipArchiveMode.Create)) {
                foreach(var item in files.OrderBy(x=>x.Key)) {
                    string file=item.Value;
                    var info=new FileInfo(file);long size=info.Length;bytes+=size;
                    var record=new DeltaFile {Path=item.Key,Size=size,Hash=Hash(file)};
                    long best=size;
                    foreach(string candidate in Candidates(basis,item.Key,webRelative)) {
                        string source=Resolve(basis,candidate); long sourceSize=new FileInfo(source).Length;
                        if(sourceSize==size && Hash(source)==record.Hash) {
                            record.Source=candidate;record.SourceHash=Hash(source);record.Prefix=size;record.Suffix=0;best=0;break;
                        }
                        // Large unchanged tails handle RPG Maker encrypted/plain asset differences as tiny deltas.
                        if(size<65536 || sourceSize<65536) continue;
                        long[] common=Common(source,file);long middle=size-common[0]-common[1];
                        if(middle<best && middle<size/2) {best=middle;record.Source=candidate;record.SourceHash=Hash(source);record.Prefix=common[0];record.Suffix=common[1];}
                    }
                    if(record.Source!=null && best==0) reused++;
                    else {
                        if(record.Source!=null) spliced++;else full++;
                        record.Payload="payload/"+manifest.Files.Count.ToString("D6");
                        var entry=zip.CreateEntry(record.Payload,CompressionLevel.Optimal);
                        using(var output=entry.Open()) using(var input=File.OpenRead(file)) CopyPart(input,output,record.Prefix,size-record.Prefix-record.Suffix);
                        payloadBytes+=size-record.Prefix-record.Suffix;
                    }
                    manifest.Files.Add(record);
                }
                var metadata=zip.CreateEntry("manifest.json",CompressionLevel.Optimal);
                using(var writer=new StreamWriter(metadata.Open(),new UTF8Encoding(false))) writer.Write(Store.Json.Serialize(manifest));
            }
            return new {name=name,pack=System.IO.Path.GetFileName(pack),targetBytes=bytes,downloadBytes=new FileInfo(pack).Length,literalBytes=payloadBytes,reusedFiles=reused,splicedFiles=spliced,fullFiles=full,totalFiles=manifest.Files.Count};
    }
    public static void BuildLayered(string ch,string cleanBase,string overlay) {
        hashCache.Clear();RecipeSupport.ValidateBase(cleanBase);overlay=RecipeSupport.ContentRoot(overlay);
        var recipe=RecipeSupport.Identify(overlay);
        if(recipe==null || !recipe.CompleteOverlay || recipe.RequiredBase!="gorpg-1-23")throw new IOException("Select a recognized complete Gorpg overlay bundle.");
        string webRelative=Relative(cleanBase,Store.WebRoot(cleanBase));
        var files=GameFiles(cleanBase).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(x=>Relative(cleanBase,x),x=>x,StringComparer.OrdinalIgnoreCase);
        foreach(string source in Directory.GetFiles(overlay,"*",SearchOption.AllDirectories)){
            string rel=Relative(overlay,source);
            if(rel.Split('/').Any(x=>x.Equals("save",StringComparison.OrdinalIgnoreCase)) || Store.IsSave(source) || Path.GetExtension(source).Equals(".docx",StringComparison.OrdinalIgnoreCase))continue;
            files[webRelative+"/"+rel]=source;
        }
        string packs=Path.Combine(Store.Home,"Delta Packs");Directory.CreateDirectory(packs);
        string pack=Path.Combine(packs,recipe.Id+".cgdelta.zip");
        var report=BuildFiles(ch,recipe.Name,pack,webRelative,files);
        File.WriteAllText(Path.Combine(Store.Home,"delta-build-"+recipe.Id+".json"),Store.Json.Serialize(report),Encoding.UTF8);
    }
    static DeltaManifest Manifest(ZipArchive zip) {
        var entry=zip.GetEntry("manifest.json");if(entry==null)throw new IOException("This is not a Celesphonia delta pack.");
        using(var reader=new StreamReader(entry.Open(),Encoding.UTF8)) {
            var result=Store.Json.Deserialize<DeltaManifest>(reader.ReadToEnd());
            if(result.Format!=1 || result.Files==null)throw new IOException("Unsupported delta format.");return result;
        }
    }
    static void Preflight(string basis,DeltaManifest manifest) {
        foreach(var f in manifest.Files) {
            Store.Child(Store.Home,f.Path.Replace('/','\\'));
            if(f.Size<0 || f.Prefix<0 || f.Suffix<0 || f.Prefix+f.Suffix>f.Size)throw new IOException("Invalid delta sizes.");
            if(f.Source!=null) {
                string source=Resolve(basis,f.Source);
                if(!File.Exists(source) || Hash(source)!=f.SourceHash)throw new IOException("This CH build differs from the required base file: "+f.Source+". No existing game or saves were changed. This pack cannot reconstruct modified/missing base assets.");
                if(f.Prefix+f.Suffix>new FileInfo(source).Length)throw new IOException("Invalid source ranges.");
            }
        }
    }
    static void Reconstruct(ZipArchive zip,string basis,DeltaFile f,Stream output) {
        if(f.Source!=null) using(var source=File.OpenRead(Resolve(basis,f.Source))) CopyPart(source,output,0,f.Prefix);
        if(f.Payload!=null) {
            var entry=zip.GetEntry(f.Payload);if(entry==null)throw new IOException("Missing patch payload.");
            if(entry.Length!=f.Size-f.Prefix-f.Suffix)throw new IOException("Invalid patch payload size.");
            using(var literal=entry.Open()) literal.CopyTo(output);
        } else if(f.Prefix+f.Suffix!=f.Size)throw new IOException("Incomplete delta.");
        if(f.Source!=null && f.Suffix>0)using(var source=File.OpenRead(Resolve(basis,f.Source)))CopyPart(source,output,source.Length-f.Suffix,f.Suffix);
    }
    public static Profile Install(string pack,string basis,string installRoot) {
        hashCache.Clear();
        using(var zip=ZipFile.OpenRead(pack)) {
            var manifest=Manifest(zip);Preflight(basis,manifest);
            Directory.CreateDirectory(installRoot);
            long required=manifest.Files.Sum(x=>x.Size)+1024L*1024*1024;
            if(new DriveInfo(System.IO.Path.GetPathRoot(installRoot)).AvailableFreeSpace<required)throw new IOException("Insufficient disk space to reconstruct this profile. Allow about "+(required/(1024.0*1024*1024)).ToString("0.0")+" GB.");
            string id=Guid.NewGuid().ToString("N"),root=Store.Child(installRoot,"Delta-"+id.Substring(0,8));Directory.CreateDirectory(root);
            foreach(var f in manifest.Files) {
                string destination=Store.Child(root,f.Path.Replace('/','\\'));Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
                using(var output=File.Create(destination))Reconstruct(zip,basis,f,output);
                if(new FileInfo(destination).Length!=f.Size || Store.Hash(destination)!=f.Hash)throw new IOException("Reconstructed file failed verification: "+f.Path+". This unfinished profile has not been registered.");
            }
            var p=Store.Detect(root,manifest.Name);Directory.CreateDirectory(p.SavePath);p.Note="Reconstructed from CH + portable delta pack; separate saves";return p;
        }
    }
    public static void Verify(string basis) {
        hashCache.Clear();var verified=new List<object>();
        foreach(string pack in Directory.GetFiles(System.IO.Path.Combine(Store.Home,"Delta Packs"),"*.cgdelta.zip")) {
            using(var zip=ZipFile.OpenRead(pack)) {
                var manifest=Manifest(zip);Preflight(basis,manifest);
                foreach(var f in manifest.Files) using(var sink=new DigestSink()) {Reconstruct(zip,basis,f,sink);if(sink.Count!=f.Size || sink.Finish()!=f.Hash)throw new IOEx