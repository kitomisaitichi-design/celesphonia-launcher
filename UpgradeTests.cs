using System;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

public static class UpgradeTests {
    static void Assert(bool pass,string message){if(!pass)throw new Exception(message);}
    // No game extraction or save writes: exercise all registered forward
    // creator overlay shortcuts and prove the branch boundaries stay closed.
    public static int ForwardCreatorRoutes(){
        var catalog=RecipeSupport.Catalog();
        string[] releases={"01","02","03","04","051","052","053"};
        var ids=releases.Select(x=>"three-in-one-3-3-5-v1-"+x).ToArray();
        int checkedRoutes=0;
        for(int i=0;i<ids.Length;i++){
            var a=catalog.Recipes.Single(x=>x.Id==ids[i]);
            Assert(UpgradeSupport.CanTransfer(ids[i],ids[i]),"Same-version restore rejected: "+ids[i]);
            for(int j=0;j<ids.Length;j++){
                var b=catalog.Recipes.Single(x=>x.Id==ids[j]);
                bool expected=i<j;
                if(expected){
                    Assert(a.Files!=null&&b.Files!=null&&a.Files.Keys.All(x=>b.Files.ContainsKey(x)),
                        "Direct creator overlay leaves obsolete files: "+ids[i]+" -> "+ids[j]);
                    Assert(UpgradeSupport.MayUpgrade(ids[i],ids[j]),"Forward creator update rejected: "+ids[i]+" -> "+ids[j]);
                    UpgradeSupport.RequireRoute(ids[i],ids[j]);
                    Assert(UpgradeSupport.CanTransfer(ids[i],ids[j]),"Forward saves rejected: "+ids[i]+" -> "+ids[j]);
                    checkedRoutes++;
                }else if(i>j){
                    Assert(!UpgradeSupport.MayUpgrade(ids[i],ids[j]),"Downgrade update accepted: "+ids[i]+" -> "+ids[j]);
                    Assert(!UpgradeSupport.CanTransfer(ids[i],ids[j]),"Downgrade saves accepted: "+ids[i]+" -> "+ids[j]);
                }
            }
        }
        foreach(string source in new[]{"01","02","03"}){
            string id="three-in-one-3-3-5-v1-"+source;
            foreach(string target in new[]{"051","052"}){
                string to="three-in-one-3-3-5-v1-"+target;
                Assert(UpgradeSupport.MayUpgrade(id,to),"Direct early-to-final overlay unavailable: "+id+" -> "+to);
                if(target=="052")Assert(UpgradeSupport.IsDirect1052(id,to),"Direct v1.052 helper lost route: "+id);
            }
        }
        string unknown="three-in-one-3-3-5-v1-05";
        foreach(string known in ids){
            Assert(!UpgradeSupport.MayUpgrade(unknown,known)&&!UpgradeSupport.MayUpgrade(known,unknown),"Unknown v1.05 entered an update route");
            Assert(!UpgradeSupport.CanTransfer(unknown,known)&&!UpgradeSupport.CanTransfer(known,unknown),"Unknown v1.05 entered save migration");
        }
        foreach(string other in new[]{UpgradeSupport.Trial,"gorpg-1-23","maple-1-7-2","three-in-one-3-3-4","ch-current"}){
            Assert(!UpgradeSupport.MayUpgrade(other,ids.Last()),"Separate/unverified branch entered direct v1.052 path: "+other);
            Assert(!UpgradeSupport.CanTransfer(other,ids.Last())&&!UpgradeSupport.CanTransfer(ids.Last(),other),
                "Cross-branch v1.052 save transfer accepted: "+other);
        }
        Assert(UpgradeSupport.MayUpgrade(UpgradeSupport.Trial,"three-in-one-3-3-5-v1-04"),"Existing trial-to-v1.04 route lost");
        Assert(!UpgradeSupport.CanTransfer(null,null)&&!UpgradeSupport.CanTransfer("ch-current","maple-1-7-2")
               &&!UpgradeSupport.CanTransfer("gorpg-1-23",UpgradeSupport.Trial),"Unknown or cross-branch save transfer accepted");
        return checkedRoutes;
    }
    public static int SourceAutomationTests(){
        const string prefix="three-in-one-3-3-5-v1-";
        var saved=new UpgradeSourceChoice {Path="C:\\fake-saved",VersionId=prefix+"02",
            Kind="Installed",SaveGeneration=102,SaveCount=11,Profile=new Profile{Id="saved"}};
        var later=new UpgradeSourceChoice {Path="C:\\fake-new",VersionId=prefix+"051",
            Kind="Installed",SaveGeneration=1051,SaveCount=0,Profile=new Profile{Id="new"}};
        var archive=new UpgradeSourceChoice {Path="C:\\fake-archive",VersionId=prefix+"051",
            Kind="Full game archive",SaveGeneration=1051,SaveCount=0};
        var savedNext=new UpgradeSourceChoice {Path="C:\\fake-saved-next",VersionId=prefix+"04",
            Kind="Installed",SaveGeneration=104,SaveCount=11,Profile=new Profile{Id="saved-next"}};
        var ordered=UpgradeSupport.RankCompatibleSources(new[]{archive,later,saved,savedNext},"new");
        Assert(ordered.Count==4&&ordered[0]==savedNext&&ordered[1]==saved&&
               ordered[2]==later&&ordered[3]==archive,
               "Saved compatible installs must outrank empty recent installs and tentative archives");
        var tied=UpgradeSupport.RankCompatibleSources(new[]{later,new UpgradeSourceChoice{
            Path="C:\\fake-other",VersionId=prefix+"051",Kind="Installed",SaveGeneration=1051,
            Profile=new Profile{Id="other"}}},"other");
        Assert(tied[0].Profile.Id=="other","Selected profile must resolve ties among equivalent valid sources");
        Assert(UpgradeSupport.BestCompatibleSource(
            new Settings{Profiles=new List<Profile>{new Profile{
                Id="missing",Root=Path.Combine(Path.GetTempPath(),"cph-uninstalled-never-"+Guid.NewGuid().ToString("N"))}}},
            prefix+"052",null,System.Threading.CancellationToken.None)==null,
            "Stale installed profile was treated as an available source");
        Assert(!UpgradeSupport.MayUpgrade(prefix+"05",prefix+"052"),
            "Unknown v1.05 source was accepted for automated migration");
        string scratch=Path.Combine(Path.GetTempPath(),"Cph-upgrade-select-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try{
            string bogus=Path.Combine(scratch,"future-1.05.zip");
            using(var zip=new ZipArchive(File.Create(bogus),ZipArchiveMode.Create)){
                var entry=zip.CreateEntry("future.txt");
                using(var output=new StreamWriter(entry.Open()))output.Write("NOT A GAME");
            }
            string untouched=Path.Combine(scratch,"no-install");
            bool rejected=false;
            try{UpgradeSupport.InstallFromGameArchive(bogus,bogus,untouched,prefix+"052",false);}
            catch(IOException error){rejected=error.Message.Contains("not a recognized earlier full game");}
            Assert(rejected&&!Directory.Exists(untouched),
                "Invalid full-game archive created installation files or was accepted by filename");
            rejected=false;
            try{UpgradeSupport.PlanMixedGoRpg(new[]{scratch},System.Threading.CancellationToken.None);}
            catch(IOException error){rejected=error.Message.Contains("missing")&&error.Message.Contains("7208");}
            Assert(rejected,"Incomplete mixed source was accepted without canonical native SHA-256 bytes");
            return 5;
        }finally{
            if(Directory.Exists(scratch)&&Path.GetFileName(scratch).StartsWith("Cph-upgrade-select-",StringComparison.Ordinal)
               &&Path.GetDirectoryName(scratch).Equals(Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))
                Directory.Delete(scratch,true);
        }
    }
    // Exercise the real v1.03 -> v1.04 path without registering test profiles,
    // modifying existing games, or using a placeholder native-probe fixture.
    public static void RealChain(string cleanBase,string archive103,string archive104,string report){
        const string oldId="three-in-one-3-3-5-v1-03",newId="three-in-one-3-3-5-v1-04";
        Assert(RecipeSupport.IsCleanBase(cleanBase),"An intact clean Gorpg 1.23 game is required.");
        Assert(RecipeSupport.IdentifyArchive(archive103)!=null&&RecipeSupport.IdentifyArchive(archive103).Id==oldId,
            "The actual v1.03 creator archive did not match its registered SHA-256.");
        Assert(RecipeSupport.IdentifyArchive(archive104)!=null&&RecipeSupport.IdentifyArchive(archive104).Id==newId,
            "The actual v1.04 creator archive did not match its registered SHA-256.");
        string tempRoot=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"Cph-testchain-"+Guid.NewGuid().ToString("N").Substring(0,12)));
        string ownedBackup=null;
        Directory.CreateDirectory(tempRoot);
        try{
            string destination=Path.Combine(tempRoot,"games");
            var prior=RecipeSupport.Install(cleanBase,destination,"Private v1.03 forward-upgrade test",
                new List<LayerInput>{new LayerInput{Source=archive103}});
            Assert(UniversalSupport.Identify(prior.Root)==oldId,"Actual v1.03 extracted game was not identified as v1.03.");
            Assert(!Store.Running(new Settings{Profiles=new List<Profile>{prior}}),"Test game must not be running.");
            ownedBackup=Path.Combine(Store.Home,"Save Backups",prior.Id);
            Assert(!Directory.Exists(ownedBackup),"Refusing to use an already-existing backup folder.");
            Directory.CreateDirectory(prior.SavePath);
            var expected=new Dictionary<string,string>();
            for(int i=1;i<=11;i++){
                string name="file"+i+".rpgsave",path=Path.Combine(prior.SavePath,name);
                File.WriteAllText(path,"CELESPHONIA-V103-V104-INDEPENDENT-SAVE-"+i,new UTF8Encoding(false));
                expected[name]=Store.Hash(path);
            }
            EnglishSupport.Apply(prior);
            Assert(EnglishSupport.Status(prior).Enabled,"Real v1.03 English display overlay did not enable.");
            var next=UpgradeSupport.Install(prior,archive104,destination,newId,true);
            Assert(UniversalSupport.Identify(next.Root)==newId,"v1.03 upgrade failed to produce v1.04 markers.");
            RecipeSupport.VerifyProfile(next);
            var migrated=Store.SaveFiles(next.SavePath);
            Assert(migrated.Length==expected.Count,"v1.03 saves were not all copied into v1.04.");
            foreach(var item in expected){
                Assert(Store.Hash(Path.Combine(prior.SavePath,item.Key))==item.Value,"v1.03 original save changed: "+item.Key);
                Assert(Store.Hash(Path.Combine(next.SavePath,item.Key))==item.Value,"v1.04 save differs: "+item.Key);
            }
            var translation=EnglishSupport.Status(next);
            Assert(translation.Enabled&&translation.VersionId==newId,"v1.04 did not inherit its own English display overlay.");
            Assert(!UpgradeSupport.CanTransfer(newId,oldId),"The reverse v1.04 -> v1.03 save transfer was permitted.");
            File.WriteAllText(report,Store.Json.Serialize(new{
                status="PASS",creatorArchives="genuine v1.03 RAR -> genuine v1.04 RAR",
                from=oldId,to=newId,independentInstallations=true,profilesRegistered=false,
                originalSavesPreserved=true,migratedSavesVerified=expected.Count,
                developerFileHashesVerified=true,englishVersion=translation.VersionId,
                englishEnabled=translation.Enabled,reverseMigrationRejected=true,gameplayExecuted=false
            }),new UTF8Encoding(false));
        }finally{
            // Delete only test-owned directories created with this unique prefix.
            string tempPrefix=Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\')+Path.DirectorySeparatorChar;
            if(Directory.Exists(tempRoot)&&tempRoot.StartsWith(tempPrefix,StringComparison.OrdinalIgnoreCase)
                &&System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(tempRoot),@"^Cph-testchain-[a-f0-9]{12}$")){
                Store.CheckLinks(tempRoot);Directory.Delete(tempRoot,true);
            }
            if(ownedBackup!=null&&Directory.Exists(ownedBackup)
                &&Path.GetDirectoryName(Path.GetFullPath(ownedBackup)).Equals(
                    Path.GetFullPath(Path.Combine(Store.Home,"Save Backups")),StringComparison.OrdinalIgnoreCase)
                &&System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(ownedBackup),@"^[a-f0-9]{32}$")){
                Store.CheckLinks(ownedBackup);Directory.Delete(ownedBackup,true);
            }
        }
    }
    public static void Run(){
        string root=Store.Child(Store.Home,Path.Combine("Staging","upgrade-fixture-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(root);var checks=new List<string>();
        try{
            int sourceChecks=SourceAutomationTests();
            Assert(sourceChecks==5,"Automated source checks were skipped");
            checks.Add("Compatible saved-profile priority, selected tie break, stale profile exclusion, unknown 1.05, archive fail-closed, native-mixed missing-byte rejection");
            int directRoutes=ForwardCreatorRoutes();
            Assert(directRoutes==21,"Expected all 21 ordered creator version upgrades");
            checks.Add("All 21 creator forward routes verified through v1.053; reverse, unknown and cross-branch saves rejected");
            Assert(UpgradeSupport.Latest=="three-in-one-3-3-5-v1-053","The latest developer release must be v1.053");
            var catalog=RecipeSupport.Catalog();
            var newest=catalog.Recipes.First(x=>x.Id==UpgradeSupport.Latest);
            Assert(newest.SaveGeneration==1053&&newest.Files.Count==5163&&newest.ArchiveBytes==547645375,
                "v1.053 complete creator recipe missing");
            Assert(MixedSourceRecovery.LoadEmbeddedReference(newest.Id).Files.Count==11730,
                "v1.053 full original-file target inventory missing");
            var latest=catalog.Recipes.First(x=>x.Id=="three-in-one-3-3-5-v1-052");
            var previous=catalog.Recipes.First(x=>x.Id=="three-in-one-3-3-5-v1-051");
            Assert(latest.Files.Count==5163 && latest.SaveGeneration==1052 &&
                   latest.ArchiveBytes==547645049,"v1.052 full verified overlay recipe missing");
            Assert(previous.Files.Count==5163 && previous.ArchiveBytes==547645039,
                   "v1.051 compatibility recipe was removed");
            Assert(latest.Files.Keys.ToArray().OrderBy(x=>x).SequenceEqual(previous.Files.Keys.OrderBy(x=>x)) &&
                   latest.Files.Count(x=>previous.Files[x.Key]!=x.Value)==1 &&
                   latest.Files["data/Enemies.json"]!=previous.Files["data/Enemies.json"],
                   "v1.052 creator source changed files outside data/Enemies.json");
            Assert(latest.Fingerprints.Count>=6 && previous.Fingerprints.Count>=6 &&
                   latest.Fingerprints["data/Enemies.json"]!=previous.Fingerprints["data/Enemies.json"],
                   "v1.051 and v1.052 native signatures must differ on enemies");
            Assert(FileDiscovery.NameHint(latest.ArchiveName,catalog)==latest,"Renamed RAR filename detection failed");
            Assert(FileDiscovery.NameHint("future_three_in_one_1_054_unverified.rar",catalog)==null,
                   "Unknown future release misclassified as an older bundle");
            checks.Add("Verified both creator RAR signatures; v1.052 only Enemies changed; unknown future refused");
            Assert(UpgradeSupport.CanTransfer("three-in-one-3-3-5-v1-04",UpgradeSupport.Latest),"Forward v1.04 migration rejected");
            Assert(UpgradeSupport.CanTransfer("three-in-one-3-3-5-v1-04",previous.Id) &&
                   UpgradeSupport.CanTransfer(previous.Id,latest.Id),
                   "Independent v1.04→v1.051 and v1.051→v1.052 paths must remain valid");
            Assert(UpgradeSupport.CanTransfer("three-in-one-3-3-5-v1-03",UpgradeSupport.Latest),"Direct v1.03 to v1.052 should be supported");
            foreach(var prior in RecipeSupport.Catalog().Recipes.Where(x=>RecipeSupport.Catalog().Recipes.First(y=>y.Id==UpgradeSupport.Latest).UpgradeFrom.Contains(x.Id))){
                Assert(UpgradeSupport.CanTransfer(prior.Id,UpgradeSupport.Latest),"Prior combined release migration rejected: "+prior.Id);
                Assert(!UpgradeSupport.CanTransfer(UpgradeSupport.Latest,prior.Id),"Downgrade migration accepted: "+prior.Id);
            }
            foreach(string old in new[]{UpgradeSupport.Trial,"gorpg-1-23","maple-1-7-2","three-in-one-3-3-4","ch-current"})Assert(!UpgradeSupport.CanTransfer(UpgradeSupport.Latest,old),"Downgrade save migration accepted");
            Assert(!UpgradeSupport.CanTransfer("three-in-one-3-3-4",UpgradeSupport.Latest)&&UpgradeSupport.CanTransfer(UpgradeSupport.Latest,UpgradeSupport.Latest),"Separate branches or same-version restore mishandled");checks.Add("Forward-only migration; same-version restore; 3.3.4 branch isolation");
            var recipe=RecipeSupport.Catalog().Recipes.First(x=>x.Id==UpgradeSupport.Latest);var source=Store.Read().Profiles.First(x=>UniversalSupport.Identify(x.Root)=="three-in-one-3-3-5-v1-04");string web=Store.WebRoot(source.Root);
            // A renamed content ZIP is detected by structure and marker bytes.
            string zipPath=Path.Combine(root,"renamed.dat");using(var zip=new ZipArchive(File.Create(zipPath),ZipArchiveMode.Create)){foreach(var mark in RecipeSupport.Catalog().Recipes.First(x=>x.Id=="three-in-one-3-3-5-v1-04").Fingerprints){var e=zip.CreateEntry("wrapper/"+mark.Key);using(var output=e.Open())using(var input=File.OpenRead(Store.Child(web,mark.Key.Replace('/','\\'))))input.CopyTo(output);}}
            Assert(FileDiscovery.ArchiveFormat(zipPath)=="zip","ZIP signature was ignored");Assert(FileDiscovery.ProbeZip(zipPath).Single().RecipeId=="three-in-one-3-3-5-v1-04","Renamed overlay ZIP not identified");checks.Add("Renamed wrapped ZIP identified from structure and hashes");
            string malicious=Path.Combine(root,"unsafe.zip");using(var zip=new ZipArchive(File.Create(malicious),ZipArchiveMode.Create)){zip.CreateEntry("../data/System.json");}Assert(FileDiscovery.ProbeZip(malicious).Count==0,"Traversal archive recognized");checks.Add("Unsafe archive paths rejected");
            string cached=Path.Combine(root,"marker.json");File.WriteAllText(cached,"first");string hash=DetectionCache.Hash(cached);File.WriteAllText(cached,"changed marker");Assert(DetectionCache.Hash(cached)!=hash,"Changed marker used stale navigation hash");checks.Add("Changed file invalidates detection cache");
            string backup=Path.Combine(root,"newer-backup");Directory.CreateDirectory(backup);File.WriteAllText(Path.Combine(backup,"manifest.json"),Store.Json.Serialize(new{version=UpgradeSupport.Latest,files=new Dictionary<string,string>()}),Encoding.UTF8);
            bool rejected=false;try{Store.ReplaceSaves(backup,source,true);}catch(IOException){rejected=true;}Assert(rejected,"Low-level v1.052-to-v1.04 restore accepted");checks.Add("Backup restore enforces migration policy before writing saves");
            Application.EnableVisualStyles();using(var picker=new ChoicePicker()){picker.Items.AddRange(new object[]{"Trial","v1.01"});int fired=0;picker.SelectedIndexChanged+=delegate{fired++;};picker.SelectedIndex=0;for(int i=0;i<40;i++)picker.SelectedIndex=0;picker.SelectedIndex=1;picker.SelectedIndex=900;picker.SelectedIndex=900;Assert(fired==3&&picker.SelectedItem==null,"Picker duplicate or invalid selections trigger redundant updates");}checks.Add("Repeated/invalid picker clicks produce stable selection events");
            var watch=Stopwatch.StartNew();for(int i=0;i<40;i++)Assert(UniversalSupport.Identify(source.Root)=="three-in-one-3-3-5-v1-04","Cached profile identity changed");watch.Stop();checks.Add("Repeated navigation detection remains correct");
            File.WriteAllText(Path.Combine(Store.Home,"upgrade-test-results.json"),Store.Json.Serialize(new{status="PASS",checks=checks,navigationIdentityCalls=40,navigationMilliseconds=watch.ElapsedMilliseconds,fullGameplayVerified=false}),Encoding.UTF8);
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
