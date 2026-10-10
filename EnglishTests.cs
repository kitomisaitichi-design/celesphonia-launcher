using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public static class EnglishTests {
    static int checks;
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception("English test failed: "+message);}
    public static void Run(){
        checks=0;var settings=Store.Read();var source=settings.Profiles.First(x=>UniversalSupport.Identify(x.Root)!=null&&UniversalSupport.Identify(x.Root)!="ch-current");string version=UniversalSupport.Identify(source.Root);
        string relative=Path.Combine("Tests","English-fixture-"+Guid.NewGuid().ToString("N"));string fixture=Store.Child(Store.Home,relative);Directory.CreateDirectory(fixture);
        var userFiles=new Dictionary<string,byte[]>();foreach(string name in new[]{"observed-catalog.json","user-translations.json","user-statistics.json"}){string file=Store.Child(Store.Home,Path.Combine("English",version,name));userFiles[file]=File.Exists(file)?File.ReadAllBytes(file):null;}
        try{
            string web=Path.Combine(fixture,"www"),sourceWeb=Store.WebRoot(source.Root);Directory.CreateDirectory(web);
            File.WriteAllText(Path.Combine(fixture,"package.json"),"{\"main\":\"www/index.html\"}",new UTF8Encoding(false));
            var build=UniversalSupport.ReadIndex().Versions.First(x=>x.Id==version);
            foreach(var mark in build.Fingerprints){string destination=Store.Child(web,mark.Key);Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(Store.Child(sourceWeb,mark.Key),destination);}
            string entry=Path.Combine(web,"index.html");byte[] original=new UTF8Encoding(true).GetPreamble().Concat(new UTF8Encoding(false).GetBytes("<!DOCTYPE html>\r\n<script src=\"js/main.js\"></script>\r\n")).ToArray();File.WriteAllBytes(entry,original);
            var p=new Profile{Id="english-test",Name="English fixture",Root=fixture,Exe="Fixture.exe",SavePath=Path.Combine(fixture,"save")};Directory.CreateDirectory(p.SavePath);File.WriteAllText(Path.Combine(p.SavePath,"file1.rpgsave"),"sentinel-save-bytes");
            string saveHash=Store.Hash(Path.Combine(p.SavePath,"file1.rpgsave")),originalHash=Store.Hash(entry);
            File.WriteAllText(Path.Combine(fixture,"launcher-reconstruction.json"),Store.Json.Serialize(new ReconstructionState{format=2,files=new List<ReconstructionFile>{new ReconstructionFile{path="www/index.html",hash=originalHash,size=original.Length}}}));
            Check(EnglishSupport.Compatible("\\c[2]A %1\nB","\\c[2]Text %1\nMore"),"valid controls");
            Check(!EnglishSupport.Compatible("\\v[12] %1","\\v[13] %1"),"changed variable is rejected");
            Check(!EnglishSupport.Compatible("%1 %2","%2 %1"),"reordered placeholders are rejected");
            Check(!EnglishSupport.Compatible("A\nB","A B"),"changed line count is rejected");
            Check(!EnglishSupport.Compatible("选项if(s[12])","Choiceif(s[13])"),"changed plugin condition is rejected");
            EnglishSupport.ApplyCore(p,null);Check(EnglishSupport.Status(p).Enabled,"enabled detected");
            Check(Store.Hash(EnglishSupport.OriginalFile(fixture,entry))==originalHash,"delta reconstruction resolves original entry");UniversalSupport.VerifyInstalled(p);checks++;
            Check(Store.Hash(Path.Combine(p.SavePath,"file1.rpgsave"))==saveHash,"saves preserved");
            Check(build.Fingerprints.All(mark=>Store.Hash(Store.Child(web,mark.Key))==mark.Value),"version databases and scripts preserved");
            EnglishSupport.ApplyCore(p,null);Check(EnglishSupport.Status(p).Enabled,"reapplication is idempotent");
            string seen=Path.Combine(web,"celesphonia-english","seen-untranslated.json"),import=Path.Combine(fixture,"import.json");
            File.WriteAllText(seen,Store.Json.Serialize(new Dictionary<string,string>{{"新出现的菜单","新出现的菜单"}}));
            File.WriteAllText(import,Store.Json.Serialize(new Dictionary<string,string>{{"新出现的菜单","New Menu"},{"unknown program key","Ignored"}}));
            string imported=EnglishSupport.Import(p,import);Check(imported.StartsWith("Imported 1"),"observed display text can be imported; unrelated keys ignored");
            Check(File.ReadAllText(Path.Combine(web,"celesphonia-english","dictionary.js")).Contains("New Menu"),"active overlay updates after import");
            var dictionary=Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(Store.Child(Store.Home,Path.Combine("English",version,"dictionary.json")),Encoding.UTF8));string controlKey=dictionary.Keys.First(x=>x.Contains("%1"));
            File.WriteAllText(import,Store.Json.Serialize(new Dictionary<string,string>{{controlKey,dictionary[controlKey].Replace("%1","%9")}}));bool importRejected=false;try{EnglishSupport.Import(p,import);}catch(IOException){importRejected=true;}Check(importRejected,"unsafe imported placeholder is rejected");
            string remaining=Path.Combine(fixture,"remaining.json");EnglishSupport.ExportRemaining(p,remaining);Check(!Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(remaining,Encoding.UTF8)).ContainsKey("新出现的菜单"),"translated observed text is excluded from remaining export");
            string runtime=Path.Combine(web,"celesphonia-english","runtime.js");byte[] runtimeBytes=File.ReadAllBytes(runtime);File.AppendAllText(runtime,"\n/* changed */");
            bool rejected=false;try{EnglishSupport.Verify(fixture);}catch(IOException){rejected=true;}Check(rejected,"changed overlay asset is detected");
            EnglishSupport.Restore(p);Check(File.ReadAllBytes(entry).SequenceEqual(original),"restores original BOM and CRLF exactly despite a changed overlay asset");Check(!EnglishSupport.Status(p).Enabled,"disabled detected");File.WriteAllBytes(runtime,runtimeBytes);
            EnglishSupport.ApplyCore(p,null);string modified=File.ReadAllText(entry)+"<!-- other tool -->";File.WriteAllText(entry,modified);rejected=false;try{EnglishSupport.Restore(p);}catch(IOException){rejected=true;}Check(rejected&&File.ReadAllText(entry)==modified,"preserves another tool's entry changes");
            File.WriteAllBytes(entry,original);EnglishSupport.Verify(fixture);checks++;
            Check(Store.Hash(Path.Combine(p.SavePath,"file1.rpgsave"))==saveHash,"saves unchanged after all apply/revert attempts");
            File.WriteAllText(Path.Combine(Store.Home,"english-test-results.json"),Store.Json.Serialize(new{status="PASS",checks=checks,scope="Small fixture: controls, original-byte restoration, integrity verification, reconstruction input, tampering, save and database immutability",fullGamePlaythrough=false}),new UTF8Encoding(false));
        }finally{foreach(var file in userFiles){if(file.Value==null){if(File.Exists(file.Key))File.Delete(file.Key);}else File.WriteAllBytes(file.Key,file.Value);}string verified=Store.Child(Store.Home,relative);if(!fixture.Equals(verified,StringComparison.OrdinalIgnoreCase)||!Path.GetDirectoryName(fixture).Equals(Path.Combine(Store.Home,"Tests"),StringComparison.OrdinalIgnoreCase))throw new IOException("Test cleanup path was not valid.");Directory.Delete(verified,true);}
    }
}
