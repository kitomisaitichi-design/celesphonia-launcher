using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

public static class Discovery080Tests {
    static int checks;
    static void Require(bool pass,string name){checks++;if(!pass)throw new Exception(name);}
    static string Zip(string name,Dictionary<string,string> files) {
        string path=Path.Combine(Store.Home,"sources",name);
        using(var z=ZipFile.Open(path,ZipArchiveMode.Create))
            foreach(var f in files){using(var w=new StreamWriter(z.CreateEntry(f.Key).Open()))w.Write(f.Value);}
        return path;
    }
    static Dictionary<string,string> Game(){
        return new Dictionary<string,string>{{"game/package.json","{\"main\":\"www/index.html\"}"},{"game/Game.exe","MZfixture"},{"game/www/index.html","fixture"},{"game/www/data/System.json","{\"gameTitle\":\"Celesphonia fixture\"}"},{"game/www/js/plugins.js","fixture"}};
    }
    public static int Main(){
        try{
            string sources=Path.Combine(Store.Home,"sources");Directory.CreateDirectory(sources);
            string system=Path.Combine(Store.Home,"system.tmp"),plugins=Path.Combine(Store.Home,"plugins.tmp");
            File.WriteAllText(system,Game()["game/www/data/System.json"]);File.WriteAllText(plugins,"fixture");
            var marks=new Dictionary<string,string>{{"data/System.json",Store.Hash(system)},{"js/plugins.js",Store.Hash(plugins)}};
            string rar=Path.Combine(sources,"renamed.data");File.WriteAllBytes(rar,new byte[]{82,97,114,33,26,7,1,0,22,3,5});
            var recipe=new ModRecipe{Id="fixture",Name="Fixture",ArchiveName="different.rar",ArchiveBytes=new FileInfo(rar).Length,ArchiveHash=Store.Hash(rar),Fingerprints=marks};
            File.WriteAllText(Path.Combine(Store.Home,"mod-recipes.json"),Store.Json.Serialize(new RecipeCatalog{Recipes=new List<ModRecipe>{recipe},GorpgFingerprint=new Dictionary<string,string>(),ChFingerprint=new Dictionary<string,string>()}));
            File.WriteAllText(Path.Combine(Store.Home,"version-fingerprints.json"),Store.Json.Serialize(new VersionIndex{Format=1,Versions=new List<VersionBuild>{new VersionBuild{Id="fixture",Name="Fixture",Fingerprints=marks}}}));
            Require(FileDiscovery.ArchiveFormat(rar)=="rar","RAR signature ignores extension");
            string seven=Path.Combine(sources,"seven.data");File.WriteAllBytes(seven,new byte[]{55,122,188,175,39,28});
            Require(FileDiscovery.ArchiveFormat(seven)=="7z","7z signature ignores extension");
            string good=Zip("game.data",Game());Require(FileDiscovery.ArchiveFormat(good)=="zip","ZIP signature ignores extension");
            Require(FileDiscovery.ProbeZip(good).Single().RecipeId=="fixture","wrapped game fingerprints");
            var files=Game();files.Remove("game/www/index.html");
            Require(!FileDiscovery.ProbeZip(Zip("missing-main.zip",files)).Any(),"missing entry rejected");
            files=Game();files["game/Game.exe"]="invalid";
            Require(!FileDiscovery.ProbeZip(Zip("fake-exe.zip",files)).Any(),"non-MZ executable rejected");
            files=Game();files["game/www/js/plugins.js"]="modified";
            var wrongFingerprint=FileDiscovery.ProbeZip(Zip("tampered.zip",files));
            Require(wrongFingerprint.Count==1 && wrongFingerprint[0].Kind=="Archive candidate" &&
                string.IsNullOrEmpty(wrongFingerprint[0].RecipeId) &&
                !new[]{"Original archive","Full game archive","Game archive candidate","Extracted bundle","Installed game"}.Contains(wrongFingerprint[0].Kind),
                "native fingerprint mismatch remains an unverified, non-installable archive candidate");
            files=Game();files["game/package.json"]="{\"main\":\"../index.html\"}";
            Require(!FileDiscovery.ProbeZip(Zip("unsafe-main.zip",files)).Any(),"unsafe main rejected");
            files=Game();files["../outside"]="bad";
            Require(!FileDiscovery.ProbeZip(Zip("traversal.zip",files)).Any(),"archive traversal rejected");
            files=Game();files["GAME/WWW/js/plugins.js"]="duplicate";
            Require(!FileDiscovery.ProbeZip(Zip("duplicate.zip",files)).Any(),"case-duplicate paths rejected");
            files=Game();foreach(var f in Game())files.Add(f.Key.Replace("game/","second/"),f.Value);
            Require(!FileDiscovery.ProbeZip(Zip("ambiguous.zip",files)).Any(),"ambiguous game roots rejected");
            string split=Path.Combine(sources,"Celesphonia 1.23gorpg.001");File.WriteAllText(split,"partial");
            var progress=new List<DiscoveryProgress>();
            string excluded=Path.Combine(sources,"isolated-native-probe");Directory.CreateDirectory(excluded);
            File.WriteAllText(Path.Combine(excluded,".celesphonia-native-probe"),"fixture");
            File.Copy(rar,Path.Combine(excluded,"hidden.data"));
            var found=FileDiscovery.ScanFolders(new[]{sources,sources.ToUpperInvariant()},CancellationToken.None,p=>progress.Add(p));
            Require(!found.Any(x=>x.Path.StartsWith(excluded)),"private native fixtures excluded from discovery");
            string playable=Path.Combine(sources,"playable");Directory.CreateDirectory(playable);
            Directory.CreateDirectory(Path.Combine(playable,"www","data"));
            File.WriteAllText(Path.Combine(playable,"package.json"),"{\"main\":\"www/index.html\"}");
            File.WriteAllText(Path.Combine(playable,"www","index.html"),"fixture");
            File.WriteAllBytes(Path.Combine(playable,"Game.exe"),new byte[]{(byte)'M',(byte)'Z',0,0});
            File.WriteAllText(Path.Combine(playable,"www","data","System.json"),Game()["game/www/data/System.json"]);
            string save=Path.Combine(playable,"www","save");
            var profiles=new[]{new Profile{Id="a",Root=playable,SavePath=save},new Profile{Id="b",Root=playable.ToUpperInvariant()+"\\",SavePath=save.ToUpperInvariant()},new Profile{Id="private",Root=excluded,SavePath=Path.Combine(excluded,"save")},new Profile{Id="different",Root=playable,SavePath=Path.Combine(playable,"custom-save")}};
            var visible=ProfileLibrary.Visible(profiles,"b");
            Require(visible.Count==1&&visible.Any(p=>p.Id=="b")&&!visible.Any(p=>p.Id=="private"),"library hides invalid fixtures and deduplicates normalized playable roots");
            Require(!visible.Any(p=>p.Id=="different"),"mismatched save location is not presented as playable");
            Require(profiles.Length==4,"library view preserves registered records");
            Require(found.Count(x=>x.Path==rar&&x.RecipeId=="fixture")==1,"renamed archive byte-size hint; installation verifies full digest");
            Require(found.Count(x=>x.Path==good&&x.RecipeId=="fixture")==1,"overlapping search roots deduplicated");
            Require(found.Any(x=>x.Path==split&&x.Problem.Contains(".002")),"missing split volume exposed");
            Require(progress.Count>0&&progress.Last().FoldersVisited==1,"one shared scan budget and final progress");
            Require(DetectionCache.Hash(rar)==Store.Hash(rar),"chunked digest matches integrity digest");
            string first=DetectionCache.Hash(seven);File.AppendAllText(seven,"changed");
            Require(DetectionCache.Hash(seven)!=first&&DetectionCache.Hash(seven)==Store.Hash(seven),"changed source invalidates digest cache");
            using(var cancel=new CancellationTokenSource()){
                cancel.Cancel();bool stopped=false;try{FileDiscovery.ScanFolders(new[]{sources},cancel.Token,null);}catch(OperationCanceledException){stopped=true;}
                Require(stopped,"scan cancellation propagates");
                stopped=false;try{FileDiscovery.ProbeZip(good,cancel.Token);}catch(OperationCanceledException){stopped=true;}
                Require(stopped,"ZIP cancellation propagates");
                stopped=false;try{DetectionCache.Hash(rar,cancel.Token);}catch(OperationCanceledException){stopped=true;}
                Require(stopped,"cached hash respects cancellation");
            }
            using(var cancel=new CancellationTokenSource()){
                bool stopped=false;try{FileDiscovery.ScanFolders(new[]{sources},cancel.Token,p=>cancel.Cancel());}catch(OperationCanceledException){stopped=true;}
                Require(stopped,"scan can cancel after progress");
            }
            File.WriteAllText(Path.Combine(Store.Home,"result.json"),Store.Json.Serialize(new{status="PASS",checks=checks,syntheticSourcesOnly=true,userSaveReads=0,userSaveWrites=0}));
            Console.WriteLine("PASS "+checks+" source detection checks");return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
