using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

class UpdaterBehaviorFixture {
    static string Hash(string path){using(var f=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static int Main(string[] args){
        string root=Path.Combine(Path.GetTempPath(),"CelesphoniaUpdaterBehavior-"+Guid.NewGuid().ToString("N")),home=Path.Combine(root,"app");
        bool fixedExpected=args[4]=="fixed";var json=new JavaScriptSerializer{MaxJsonLength=256*1024*1024};
        var checks=new List<string>();
        try{
            string stage=Path.Combine(home,"Staging","LauncherUpdates","candidate"),payload=Path.Combine(stage,"payload"),zip=Path.Combine(stage,"Celesphonia-Delta-Launcher-CH1.zip");
            Directory.CreateDirectory(stage);ZipFile.ExtractToDirectory(args[1],home);File.Copy(args[2],zip);ZipFile.ExtractToDirectory(zip,payload);
            var files=Directory.GetFiles(payload,"*",SearchOption.AllDirectories).ToDictionary(p=>p.Substring(payload.Length+1).Replace('\\','/'),p=>Hash(p));
            var originals=files.Keys.ToDictionary(n=>n,n=>Hash(Path.Combine(home,n)));
            var changed=files.Keys.Where(n=>files[n]!=originals[n]).ToArray();
            DateTime frozen=new DateTime(2000,1,1,0,0,0,DateTimeKind.Utc);
            foreach(string n in files.Keys)File.SetLastWriteTimeUtc(Path.Combine(home,n),frozen);
            string config=Path.Combine(home,"profiles.json"),save=Path.Combine(home,"Game Profiles","test.rpgsave");
            Directory.CreateDirectory(Path.GetDirectoryName(save));File.WriteAllText(config,"PRIVATE-PROFILES");File.WriteAllText(save,"PRIVATE-SAVE");
            string manifest=Path.Combine(stage,"manifest.json");
            File.WriteAllText(manifest,json.Serialize(new{Tag="launcher-v1.0.22",Repository="example/example",ArchiveHash=Hash(zip),Files=files}));
            var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));var updater=assembly.GetType("LauncherUpdate",true);
            var apply=updater.GetMethod("ApplyInternal",BindingFlags.Static|BindingFlags.NonPublic);
            Action run=()=>apply.Invoke(null,new object[]{home,manifest,0,false});
            run();
            string rollback=Path.Combine(stage,"rollback");
            var backedUp=Directory.GetFiles(rollback,"*",SearchOption.AllDirectories).Select(p=>p.Substring(rollback.Length+1).Replace('\\','/')).ToArray();
            var rewritten=files.Keys.Where(n=>File.GetLastWriteTimeUtc(Path.Combine(home,n))!=frozen).ToArray();
            Require(files.All(x=>Hash(Path.Combine(home,x.Key))==x.Value),"Installed payload differs from verified target");checks.Add("target hashes match");
            Require(File.ReadAllText(config)=="PRIVATE-PROFILES"&&File.ReadAllText(save)=="PRIVATE-SAVE","Private state changed");checks.Add("private state unchanged");
            if(fixedExpected){
                Require(backedUp.OrderBy(n=>n).SequenceEqual(changed.OrderBy(n=>n)),"Unchanged assets copied to rollback");checks.Add("changed-only backups");
                Require(rewritten.OrderBy(n=>n).SequenceEqual(changed.OrderBy(n=>n)),"Unchanged installed assets replaced");checks.Add("unchanged timestamps preserved");
                Require(changed.All(n=>Hash(Path.Combine(rollback,n))==originals[n]),"Rollback originals differ");checks.Add("exact rollback originals retained");
                var start=updater.GetMethod("ApplyStartInfo",BindingFlags.Static|BindingFlags.NonPublic);
                Require(start!=null,"Verified staged-helper entry missing");
                var info=(ProcessStartInfo)start.Invoke(null,new object[]{home,manifest,0});
                Require(info.FileName==Path.Combine(payload,"Celesphonia Portable Switcher.exe")&&info.Arguments.Contains("--apply-launcher-update"),"Helper does not use verified staged executable");
                Require(!Directory.GetFiles(Path.GetDirectoryName(stage),"agent-*.exe").Any(),"Random helper copy created");checks.Add("verified staged helper, no self-copy");
                foreach(string n in files.Keys)File.SetLastWriteTimeUtc(Path.Combine(home,n),frozen);
                run();Require(files.Keys.All(n=>File.GetLastWriteTimeUtc(Path.Combine(home,n))==frozen),"No-op update rewrites files");checks.Add("current payload is a no-op");
                string tampered=Path.Combine(payload,"README.txt");File.AppendAllText(tampered,"TAMPERED");bool rejected=false;
                try{start.Invoke(null,new object[]{home,manifest,0});}catch(TargetInvocationException e){rejected=e.InnerException is IOException;}
                Require(rejected,"Modified staged payload can execute helper");checks.Add("tampered payload blocked before helper start");
                File.Copy(Path.Combine(home,"README.txt"),tampered,true);
                // Deny replacement of the last asset. Earlier changed assets must roll back.
                Directory.Delete(rollback,true);
                foreach(string n in changed){using(var oldZip=ZipFile.OpenRead(args[1]))using(var input=oldZip.GetEntry(n).Open())using(var output=File.Create(Path.Combine(home,n)))input.CopyTo(output);}
                bool failed=false;
                using(var locked=File.Open(Path.Combine(home,"Celesphonia Portable Switcher.exe"),FileMode.Open,FileAccess.Read,FileShare.Read)){
                    try{run();}catch(TargetInvocationException e){failed=e.InnerException is IOException;}
                }
                Require(failed,"Locked target did not fail explicitly");
                Require(originals.All(x=>Hash(Path.Combine(home,x.Key))==x.Value),"Failed update did not restore original hashes");
                Require(File.ReadAllText(Path.Combine(stage,"update-result.txt")).Contains("rollback errors=0"),"Rollback outcome is not reported");checks.Add("locked-target failure restores originals and reports outcome");
            }else Require(rewritten.Length>changed.Length&&backedUp.Length==files.Count,"Baseline does not reproduce redundant full replacement");
            File.WriteAllText(args[3],json.Serialize(new{status="PASS",fixedExpected,expectedChanged=changed.Length,actuallyRewritten=rewritten.Length,rollbackCopies=backedUp.Length,unchangedFiles=files.Count-changed.Length,checks,bitdefenderTested=false}));
            return 0;
        }catch(Exception e){File.WriteAllText(args[3],json.Serialize(new{status="FAIL",error=e.ToString(),checks}));return 1;}
        finally{
            if(Directory.Exists(root)&&Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\')+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(root).StartsWith("CelesphoniaUpdaterBehavior-",StringComparison.Ordinal))Directory.Delete(root,true);
        }
    }
}
