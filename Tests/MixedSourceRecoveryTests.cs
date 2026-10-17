using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

public static class MixedSourceRecoveryTests {
    static int checks;
    static void Check(bool pass,string message) {if(!pass)throw new Exception("FAILED: "+message);checks++;}
    static string Hash(byte[] bytes) {
        using(var digest=SHA256.Create())return BitConverter.ToString(digest.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
    }
    static byte[] Data(string value) {return Encoding.UTF8.GetBytes(value);}
    static void Put(string root,string relative,byte[] bytes) {
        string path=Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,bytes);
    }
    static MixedNativeFile Target(string path,byte[] bytes) {return new MixedNativeFile{Path=path,Hash=Hash(bytes),Size=bytes.Length};}
    static void Fail(Action action,string expected) {
        bool failed=false;try{action();}catch(IOException){failed=true;}
        Check(failed,expected);
    }
    static void Zip(string path,string relative,byte[] bytes) {
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)) {
            var entry=zip.CreateEntry(relative);
            using(var output=entry.Open())output.Write(bytes,0,bytes.Length);
        }
    }
    static void AppendZip(string path,string relative,byte[] bytes) {
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update)) {
            var entry=zip.CreateEntry(relative);
            using(var output=entry.Open())output.Write(bytes,0,bytes.Length);
        }
    }
    static void SevenZip(string sevenZipExe,string source,string output) {
        var psi=new ProcessStartInfo(sevenZipExe,"a -t7z -y \""+output+"\" \""+source+"\"") {
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
        };
        using(var process=Process.Start(psi)) {
            process.StandardError.ReadToEndAsync();process.StandardOutput.ReadToEnd();
            process.WaitForExit();Check(process.ExitCode==0,"7z fixture created");
        }
    }
    public static int Main(string[] args) {
        string root=Path.Combine(Path.GetTempPath(),"CphMixedFixture-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            string folder=Path.Combine(root,"folder"),zip=Path.Combine(root,"partial.zip");
            byte[] system=Data("native-system"),plugin=Data("correct-native-plugin"),image=Data("asset-bytes"),
                exe=Data("MZ mock fixture"),index=Data("<html><script src='js/main.js'></script></html>");
            Put(folder,"data/System.json",system); // Maple layout: game web at root
            Put(folder,"js/plugins.js",Data("patched-plugin")); // should not be selected
            Put(folder,"index.html",Data("<html>injected English</html>"));
            Put(folder,"celesphonia-english/original-entry.bin",index); // reversible pristine backup
            Put(folder,"Game.exe",exe);
            Put(folder,"save/file1.rpgsave",Data("DO NOT COPY"));
            Zip(zip,"wrapper/www/js/plugins.js",plugin);
            string imageSource=Path.Combine(root,"image-source");
            Put(imageSource,"wrapper/www/img/picture.dat",image);
            string sevenZip=Path.Combine(root,"partial.7z");
            bool have7z=args.Length>0&&File.Exists(args[0]);
            if(have7z)SevenZip(args[0],Path.Combine(imageSource,"wrapper"),sevenZip);
            else AppendZip(zip,"wrapper/www/img/picture.dat",image);
            var files=new List<MixedNativeFile>{
                Target("www/data/System.json",system),Target("www/js/plugins.js",plugin),
                Target("www/img/picture.dat",image),Target("Game.exe",exe),Target("www/index.html",index)
            };
            string[] inputs=have7z?new[]{folder,zip,sevenZip}:new[]{folder,zip};
            var report=MixedSourceRecovery.Probe("gorpg-test",files,inputs,CancellationToken.None);
            Check(report.Complete && report.FoundCount==5 && report.Missing.Count==0,"all expected bytes recovered");
            Check(report.Selections.Single(x=>x.Path=="www/js/plugins.js").Kind=="zip","modified folder file bypassed for pristine ZIP");
            Check(report.Selections.Single(x=>x.Path=="www/index.html").Entry.EndsWith("original-entry.bin"),"native index restored by exact hash from reversible English backup");
            if(have7z)Check(report.Selections.Single(x=>x.Path=="www/img/picture.dat").Kind=="7z","7z entry selected without full extraction");
            string dest=Path.Combine(root,"installed");
            MixedSourceRecovery.Recover(report,dest,CancellationToken.None);
            foreach(var file in files)Check(Hash(File.ReadAllBytes(Path.Combine(dest,file.Path.Replace('/',Path.DirectorySeparatorChar))))==file.Hash,"output "+file.Path);
            Check(!Directory.Exists(Path.Combine(dest,"save")),"save not copied into reconstructed native game");
            Check(File.ReadAllText(Path.Combine(folder,"js","plugins.js"))=="patched-plugin","source folder preserved");
            Fail(()=>MixedSourceRecovery.Recover(report,dest,CancellationToken.None),"existing destination rejected");
            var missing=MixedSourceRecovery.Probe("gorpg-test",files,new[]{folder},CancellationToken.None);
            Check(!missing.Complete && missing.Missing.Count==2,"partial source reports precise missing original files");
            Fail(()=>MixedSourceRecovery.Recover(missing,Path.Combine(root,"incomplete"),CancellationToken.None),"incomplete plan cannot install");
            byte[] original=File.ReadAllBytes(Path.Combine(folder,"data","System.json"));
            Put(folder,"data/System.json",Data("changed-after-probe"));
            string failedDest=Path.Combine(root,"tampered");
            Fail(()=>MixedSourceRecovery.Recover(report,failedDest,CancellationToken.None),"source mutation detected during copy");
            Check(!Directory.Exists(failedDest) && Directory.GetDirectories(root,".cph-mixed-*").Length==0,"failed recovery removes only its staging");
            Put(folder,"data/System.json",original);
            string malicious=Path.Combine(root,"malicious.zip");
            Zip(malicious,"../Game.exe",exe);
            Fail(()=>MixedSourceRecovery.Probe("gorpg-test",files,new[]{malicious},CancellationToken.None),"ZIP traversal rejected");
            string duplicate=Path.Combine(root,"duplicate.zip");
            Zip(duplicate,"Game.exe",exe);AppendZip(duplicate,"game.exe",exe);
            Fail(()=>MixedSourceRecovery.Probe("gorpg-test",files,new[]{duplicate},CancellationToken.None),"case colliding ZIP entries rejected");
            Fail(()=>MixedSourceRecovery.Probe("gorpg-test",new[]{new MixedNativeFile{Path="../Game.exe",Hash=Hash(exe),Size=exe.Length}},inputs,CancellationToken.None),"bad target path rejected");
            string dashedPath="js/plugins/------------------------.js";
            Put(folder,dashedPath,Data("intact Maple nested dash filename"));
            var dash=MixedSourceRecovery.Probe("maple-test",new[]{Target(dashedPath,Data("intact Maple nested dash filename"))},
                new[]{folder},CancellationToken.None);
            Check(dash.Complete && dash.FoundCount==1,"authentic Maple nested dash-prefixed plugin filename accepted");
            Fail(()=>MixedSourceRecovery.Probe("maple-test",new[]{Target("-7z-option.js",Data("not a safe CLI option"))},
                new[]{folder},CancellationToken.None),"archive CLI-leading hyphen target name rejected");
            if(have7z) {
                string bulkRoot=Path.Combine(root,"bulk-original","wrap");
                var bulk=new List<MixedNativeFile>();
                for(int i=0;i<70;i++) {
                    string relative="www/img/image"+i.ToString("000")+".dat";
                    byte[] content=Data("immutable native asset "+i);
                    Put(bulkRoot,relative,content);
                    bulk.Add(Target(relative,content));
                }
                string bulk7z=Path.Combine(root,"bulk.7z");
                SevenZip(args[0],bulkRoot,bulk7z);
                var bulkReport=MixedSourceRecovery.Probe("gorpg-test",bulk,new[]{bulk7z},CancellationToken.None);
                Check(bulkReport.Complete && bulkReport.Selections.Count==70,"more than one bounded 7z batch completed");
                string bulkTarget=Path.Combine(root,"bulk-installed");
                MixedSourceRecovery.Recover(bulkReport,bulkTarget,CancellationToken.None);
                Check(bulk.All(x=>Hash(File.ReadAllBytes(Path.Combine(bulkTarget,x.Path.Replace('/',Path.DirectorySeparatorChar))))==x.Hash),
                    "multi-batch 7z recovery verifies every restored file");
            }
            if(args.Length>1&&args[1]=="resource") {
                var expectedCounts=new Dictionary<string,int> {
                    {"gorpg-1-23",7208},{"ch-current",13654},{"maple-1-7-2",9727},
                    {"three-in-one-3-3-4",9474}
                };
                foreach(var item in expectedCounts) {
                    var reference=MixedSourceRecovery.LoadEmbeddedReference(item.Key);
                    Check(reference.Format==1&&reference.VersionId==item.Key&&reference.Files.Count==item.Value,
                        "complete "+item.Key+" embedded inventory has exact expected count");
                    Check(reference.Files.Select(x=>x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()==item.Value,
                        "complete "+item.Key+" embedded inventory has no case-insensitive duplicate paths");
                }
                Fail(()=>MixedSourceRecovery.LoadEmbeddedReference("unknown-version"),"unavailable version fails closed");
                Fail(()=>MixedSourceRecovery.LoadEmbeddedReference("../gorpg-1-23"),"untrusted version identifier fails closed");
            }
            Console.WriteLine("PASS MixedSourceRecovery: "+checks+" checks; folder + ZIP"+(have7z?" + 7z":"")+", mixed pristine hashes, source preservation, complete ledger, path security, rollback");
            return 0;
        } catch(Exception ex) {Console.Error.WriteLine(ex);return 1;}
        finally {
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }
}
