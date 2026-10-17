using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Text;

// Isolated test assembly: compile to its own temporary directory. Store.Home
// then points there, so it cannot modify launcher recipes or real profiles.
public static class DiscoveryFastFixtures {
    static int checks;
    static void Require(bool yes,string label) {if(!yes)throw new Exception("FAIL: "+label);checks++;}
    static void Write(string root,string name,string contents) {
        string path=Path.Combine(root,name.Replace('/',Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,contents,Encoding.UTF8);
    }
    static string Zip(string folder,string filename,Dictionary<string,string> entries) {
        string archive=Path.Combine(folder,filename);
        using(var z=ZipFile.Open(archive,ZipArchiveMode.Create))
            foreach(var item in entries)using(var writer=new StreamWriter(z.CreateEntry(item.Key).Open()))
                writer.Write(item.Value);
        return archive;
    }
    static void Make7z(string sevenZip,string input,string archive) {
        var psi=new ProcessStartInfo(sevenZip,"a -t7z -y \""+archive+"\" \""+input+"\"") {
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
        };
        using(var process=Process.Start(psi)) {
            process.StandardError.ReadToEndAsync();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Require(process.ExitCode==0,"partial 7z fixture generated");
        }
    }
    public static int Main(string[] args) {
        string root=Path.Combine(Store.Home,"discovery-fixtures");
        try {
            Directory.CreateDirectory(root);
            var marks=new Dictionary<string,string>();
            Write(root,"_reference/data/System.json","native-system");
            Write(root,"_reference/js/plugins.js","native-plugins");
            marks["data/System.json"]=Store.Hash(Path.Combine(root,"_reference","data","System.json"));
            marks["js/plugins.js"]=Store.Hash(Path.Combine(root,"_reference","js","plugins.js"));
            var recipe=new ModRecipe{Id="fixture",Name="Fixture",Fingerprints=marks};
            File.WriteAllText(Path.Combine(Store.Home,"mod-recipes.json"),Store.Json.Serialize(new RecipeCatalog{
                Recipes=new List<ModRecipe>{recipe},GorpgFingerprint=new Dictionary<string,string>(),
                ChFingerprint=new Dictionary<string,string>()
            }));
            File.WriteAllText(Path.Combine(Store.Home,"version-fingerprints.json"),Store.Json.Serialize(new VersionIndex {
                Format=2,Versions=new List<VersionBuild>{new VersionBuild{Id="fixture",Name="Fixture",Fingerprints=marks}}
            }));
            string partial=Path.Combine(root,"partial-original");
            Write(partial,"package.json","{\"main\":\"www/index.html\"}");
            Write(partial,"www/data/System.json","native-system");
            // There is no runtime EXE, entry HTML or plugin file: candidate only.
            string zip=Zip(root,"renamed-native.data",new Dictionary<string,string> {
                {"bundle/www/data/System.json","native-system"},
                {"bundle/www/js/plugins.js","DIFFERENT VERSION"}
            });
            var detected=FileDiscovery.ProbeZip(zip);
            Require(detected.Count==1&&detected[0].Kind=="Archive candidate"&&detected[0].RecipeId==null,
                "partial ZIP mismatched markers appears as unverified candidate");
            string malicious=Zip(root,"unsafe.zip",new Dictionary<string,string> {
                {"../game/www/data/System.json","native-system"},{"../game/www/js/plugins.js","native-plugins"}
            });
            Require(FileDiscovery.ProbeZip(malicious).Count==0,"unsafe ZIP cannot become a candidate");
            string split=Path.Combine(root,"Celesphonia 1.23gorpg.001");
            File.WriteAllBytes(split,new byte[]{55,122,188,175,39,28,0,0});
            string media=Path.Combine(root,"assets");
            Directory.CreateDirectory(media);
            for(int i=0;i<1250;i++)File.WriteAllBytes(Path.Combine(media,"asset"+i.ToString("0000")+".png"),new byte[]{137,80,78,71,0,1});
            // Bounded scan should avoid calling ArchiveFormat on 1,250 PNGs.
            var clock=Stopwatch.StartNew();
            var result=FileDiscovery.ScanFolders(new[]{root,root.ToUpperInvariant()},CancellationToken.None,null);
            clock.Stop();
            Console.WriteLine("TRACE "+string.Join(" | ",result.Select(x=>x.Kind+":"+Path.GetFileName(x.Path)+":"+x.RecipeId).ToArray()));
            Require(result.Count(x=>x.Path==zip)==1,"renamed partial ZIP found once across overlapping roots");
            Require(result.Any(x=>x.Path==partial&&x.Kind=="Partial source folder"),"partial original folder detected");
            Require(result.Any(x=>x.Path==split&&x.Problem!=null&&x.Problem.Contains(".002")),"incomplete split volume is explicit");
            Require(result.All(x=>x.Path.IndexOf("asset",StringComparison.OrdinalIgnoreCase)<0),"image assets never reported as archives");
            string compressedSource=Path.Combine(root,"partial-compressed");
            Write(compressedSource,"www/data/System.json","native-system");
            Write(compressedSource,"www/js/plugins.js","DIFFERENT VERSION");
            if(args.Length>0&&File.Exists(args[0])) {
                string seven=Path.Combine(root,"partial-7z.7z");
                Make7z(args[0],compressedSource,seven);
                var found=FileDiscovery.ProbeRar(seven,CancellationToken.None);
                Require(found.Count==1 && found[0].Kind=="Archive candidate" && found[0].RecipeId==null,
                    "partial 7z inventory detected without extracting full archive");
            }
            string index=Path.Combine(root,"remembered.json");
            File.WriteAllText(index,Store.Json.Serialize(new InstallationLocations{Format=1,
                Installations=new List<KnownInstallation> {
                    new KnownInstallation{Root=partial,VersionId="fixture"},
                    new KnownInstallation{Root=media,VersionId="fixture"}
                }}));
            Require(SharedInstallationIndex.DiscoveryHints(index).Count==2,"shared discovery hints need no full game verification");
            Require(SharedInstallationIndex.Forget(partial,index),"one exact shared index path removed");
            Require(!SharedInstallationIndex.Forget(partial,index),"forget same path twice is harmless");
            Require(SharedInstallationIndex.DiscoveryHints(index).Single().Equals(media,StringComparison.OrdinalIgnoreCase),
                "shared index preserves unrelated entries and folders");
            var settings=new Settings{Profiles=new List<Profile>(),SuppressedRoots=new List<string>{partial}};
            Require(SharedInstallationIndex.Restore(settings,index)==0,"suppressed roots never auto-register");
            Console.WriteLine("PASS "+checks+" discovery checks; "+clock.ElapsedMilliseconds+
                " ms for overlapping roots + 1,250 ordinary assets + partial ZIP/folder/split");
            return 0;
        }catch(Exception error) {Console.Error.WriteLine(error);return 1;}
        finally {
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }
}
