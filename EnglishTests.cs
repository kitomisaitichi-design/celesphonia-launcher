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
            string importedDictionary=File.ReadAllText(Path.Combine(web,"celesphonia-english","dictionary.js"));
            if(!importedDictionary.Contains("New Menu"))File.WriteAllText(Path.Combine(Store.Home,"Tests","english-import-debug.json"),Store.Json.Serialize(new{version=version,enabled=EnglishSupport.Status(p).Enabled,modified=EnglishSupport.Status(p).Modified,imported=imported,hasUserTranslation=File.Exists(Path.Combine(Store.Home,"English",version,"user-translations.json")),seen=File.ReadAllText(seen),dictionaryLength=importedDictionary.Length,dictionaryPrefix=importedDictionary.Substring(0,Math.Min(1000,importedDictionary.Length))}),Encoding.UTF8);
            Check(importedDictionary.Contains("New Menu"),"active overlay updates after import");
            var dictionary=Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(Store.Child(Store.Home,Path.Combine("English",version,"dictionary.json")),Encoding.UTF8));string controlKey=dictionary.Keys.First(x=>x.Contains("%1"));
            File.WriteAllText(import,Store.Json.Serialize(new Dictionary<string,string>{{controlKey,dictionary[controlKey].Replace("%1","%9")}}));bool importRejected=false;try{EnglishSupport.Import(p,import);}catch(IOException){importRejected=true;}Check(importRejected,"unsafe imported placeholder is rejected");
            string remaining=Path.Combine(fixture,"remaining.json");EnglishSupport.ExportRemaining(p,remaining);Check(!Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(remaining,Encoding.UTF8)).ContainsKey("新出现的菜单"),"translated observed text is excluded from remaining export");
            string runtime=Path.Combine(web,"celesphonia-english","runtime.js");byte[] runtimeBytes=File.ReadAllBytes(runtime);File.AppendAllText(runtime,"\n/* changed */");
            bool rejected=false;try{EnglishSupport.Verify(fixture);}catch(IOException){rejected=true;}Check(rejected,"changed overlay asset is detected");
            EnglishSupport.VerifyForCopy(fixture);
            string copy=Path.Combine(Path.GetDirectoryName(fixture),"English-copy-"+Guid.NewGuid().ToString("N"));
            Store.CopyTree(fixture,copy,true);
            Check(File.ReadAllBytes(Path.Combine(copy,"www","index.html")).SequenceEqual(original),"copy repairs index from managed English with broken runtime");
            Check(!Directory.Exists(Path.Combine(copy,"www","celesphonia-english")),"copy drops obsolete English runtime and dictionary");
            Check(!Directory.Exists(Path.Combine(copy,"www","save")),"copy does not inherit source saves");Directory.Delete(copy,true);
            // An old pre-state build with the exact familiar injected index
            // can recover via its original-entry.bin. No guessing on edits.
            string stateFile=Path.Combine(web,"celesphonia-english","state.json");byte[] oldState=File.ReadAllBytes(stateFile);File.Delete(stateFile);
            EnglishSupport.VerifyForCopy(fixture);
            Check(EnglishSupport.OriginalFile(fixture,entry).EndsWith("original-entry.bin"),"state-less known English injection recovered");
            string staleCopy=Path.Combine(Path.GetDirectoryName(fixture),"English-copy-"+Guid.NewGuid().ToString("N"));
            Store.CopyTree(fixture,staleCopy,true);
            Check(File.ReadAllBytes(Path.Combine(staleCopy,"www","index.html")).SequenceEqual(original),"pre-state source index replaced with exact original in copy");Directory.Delete(staleCopy,true);
            File.AppendAllText(entry,"<!-- unknown legacy change -->");
            bool stopped=false;
            try{EnglishSupport.VerifyForCopy(fixture);}catch(IOException){stopped=true;}
            Check(stopped,"unknown mixed index change is rejected, never overwritten");
            File.WriteAllBytes(entry,original);File.WriteAllBytes(stateFile,oldState);
            EnglishSupport.Restore(p);Check(File.ReadAllBytes(entry).SequenceEqual(original),"restores original BOM and CRLF exactly despite a changed overlay asset");Check(!EnglishSupport.Status(p).Enabled,"disabled detected");File.WriteAllBytes(runtime,runtimeBytes);
            EnglishSupport.ApplyCore(p,null);string modified=File.ReadAllText(entry)+"<!-- other tool -->";File.WriteAllText(entry,modified);rejected=false;try{EnglishSupport.Restore(p);}catch(IOException){rejected=true;}Check(rejected&&File.ReadAllText(entry)==modified,"preserves another tool's entry changes");
            File.WriteAllBytes(entry,original);EnglishSupport.Verify(fixture);checks++;
            Check(Store.Hash(Path.Combine(p.SavePath,"file1.rpgsave"))==saveHash,"saves unchanged after all apply/revert attempts");
            // Isolated installation-index test: registration replaces absent
            // old folder metadata and preserves a rollback copy of the index.
            File.WriteAllBytes(Path.Combine(fixture,"Game.exe"),new byte[]{77,90,0,0});
            string testIndex=Path.Combine(fixture,"installation-index-fixture.json");
            string missing=Path.Combine(fixture,"moved-old-game");
            File.WriteAllText(testIndex,Store.Json.Serialize(new InstallationLocations{Format=1,Installations=new List<KnownInstallation>{
                new KnownInstallation{Root=missing,VersionId=version}}}),new UTF8Encoding(false));
            SharedInstallationIndex.Remember(new[]{source},testIndex);
            var entries=Store.Json.Deserialize<InstallationLocations>(File.ReadAllText(testIndex,Encoding.UTF8)).Installations;
            if(!(entries.Count==1&&entries[0].Root==Path.GetFullPath(source.Root)&&entries[0].VersionId==version))File.WriteAllText(Path.Combine(Store.Home,"Tests","index-reconciliation-debug.json"),Store.Json.Serialize(new{expected=Path.GetFullPath(source.Root),version=version,entries=entries,problem=SharedInstallationIndex.Problem,sourceVersion=UniversalSupport.Identify(fixture),gameExe=File.Exists(Path.Combine(fixture,"Game.exe"))}),Encoding.UTF8);
            Check(entries.Count==1&&entries[0].Root==Path.GetFullPath(source.Root)&&entries[0].VersionId==version,"verified relocation prunes only stale index metadata");
            Check(File.Exists(testIndex+".previous"),"index rewrite retains previous metadata backup");
            File.WriteAllText(Path.Combine(Store.Home,"english-test-results.json"),Store.Json.Serialize(new{status="PASS",checks=checks,scope="Small fixture: controls, original-byte restoration, integrity verification, reconstruction input, tampering, save and database immutability",fullGamePlaythrough=false}),new UTF8Encoding(false));
        }finally{foreach(var file in userFiles){if(file.Value==null){if(File.Exists(file.Key))File.Delete(file.Key);}else File.WriteAllBytes(file.Key,file.Value);}string verified=Store.Child(Store.Home,relative);if(!fixture.Equals(verified,StringComparison.OrdinalIgnoreCase)||!Path.GetDirectoryName(fixture).Equals(Path.Combine(Store.Home,"Tests"),StringComparison.OrdinalIgnoreCase))throw new IOException("Test cleanup path was not valid.");Directory.Delete(verified,true);}
    }
}
