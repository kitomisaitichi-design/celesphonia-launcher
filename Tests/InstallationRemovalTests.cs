using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

// Synthetic file-only fixtures: no game installed, launched, or removed.
public static class InstallationRemovalTests {
    static int checks;
    static void Check(bool condition,string name) {
        checks++;
        if(!condition)throw new IOException("FAIL: "+name);
    }
    static void Refused(Action test,string name) {
        bool blocked=false;
        try{test();}catch(IOException){blocked=true;}
        Check(blocked,name);
    }
    static string Add(string root,string relative,string content) {
        string file=Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllText(file,content,new UTF8Encoding(false));
        return file;
    }
    static Profile FixtureProfile(string root) {
        Add(root,"package.json","{\"main\":\"www/index.html\"}");
        Add(root,"www/index.html","fixture entry");
        return new Profile{Id=Guid.NewGuid().ToString("N"),Name="Synthetic file fixture",
            Root=root,Exe="Game.exe",SavePath=Path.Combine(root,"www","save")};
    }
    static Settings Settings(Profile profile,string managed) {
        return new Settings{Profiles=new List<Profile>{profile},SelectedId=profile.Id,InstallRoot=managed};
    }
    static MixedNativeFile Trusted(string root,string rel) {
        string file=Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar));
        return new MixedNativeFile{Path=rel,Hash=Store.Hash(file),Size=new FileInfo(file).Length};
    }
    public static int Main(string[] args) {
        string fixture=Path.Combine(Store.Home,"removal-synthetic-"+Guid.NewGuid().ToString("N"));
        // This second scratch root sits outside the test EXE's Store.Home,
        // so the launcher-marker ownership rule can be exercised independently.
        string externalFixture=Path.Combine(Path.GetDirectoryName(Store.Home),
            "removal-synthetic-external-"+Guid.NewGuid().ToString("N"));
        try{
            string managed=Path.Combine(fixture,"managed"),root=Path.Combine(managed,"Version-fixture");
            var profile=FixtureProfile(root);
            string game=Add(root,"Game.exe","MZ + synthetic bytes");
            string native=Add(root,"www/data/System.json","fixture-native");
            string changed=Add(root,"www/data/Actors.json","fixture-expected");
            var reference=new[]{Trusted(root,"Game.exe"),Trusted(root,"www/data/System.json"),Trusted(root,"www/data/Actors.json")};
            File.WriteAllText(changed,"personal modified database");
            string userArchive=Add(root,"backup-game.zip","synthetic archive");
            string rar=Add(root,"orig.rar","synthetic RAR");
            string seven=Add(root,"source.7z","synthetic 7z");
            string split=Add(root,"source.001","split original");
            string save=Add(root,"www/save/file1.rpgsave","valuable save");
            string backup=Add(root,"backup/file1.rpgsave","valuable backup");
            string unknown=Add(root,"www/js/custom.js","custom tweak");
            var settings=Settings(profile,managed);
            var preview=InstallationRemoval.PlanFixture(profile,settings,reference);
            Check(preview.CanDeleteFiles&&preview.FileCount==2,"exact hash matches only");
            Check(preview.FileBytes==new FileInfo(game).Length+new FileInfo(native).Length,"preview byte count");
            Check(preview.PreservedArchives==4,"ZIP RAR 7z and split archives counted");
            Check(preview.PreservedSaves>=1&&preview.PreservedBackups>=1,"saves and backups counted");
            Check(preview.RetainedSample.Contains("www/data/Actors.json"),"changed native file excluded");
            var index=InstallationRemoval.Plan(profile,settings,RemovalMode.IndexOnly);
            Check(!index.CanDeleteFiles&&index.FileCount==0,"index-only leaves files alone");
            InstallationRemoval.ExecuteFixture(index);
            Check(File.Exists(game)&&File.Exists(save)&&File.Exists(rar),"index-only fixture is read-only");
            File.AppendAllText(native,"-changed-after-preview");
            Refused(()=>InstallationRemoval.ExecuteFixture(preview),"changed bytes block removal before first delete");
            Check(File.Exists(game)&&File.Exists(native),"preflight makes no partial deletions");
            File.WriteAllText(native,"fixture-native");
            var fresh=InstallationRemoval.PlanFixture(profile,settings,reference);
            var result=InstallationRemoval.ExecuteFixture(fresh);
            Check(result.DeletedFiles==2&&!File.Exists(game)&&!File.Exists(native),"verified files deleted");
            Check(File.Exists(changed)&&File.Exists(unknown),"modified and unknown files preserved");
            Check(File.Exists(userArchive)&&File.Exists(rar)&&File.Exists(seven)&&File.Exists(split),"archives preserved");
            Check(File.Exists(save)&&File.Exists(backup),"saves and backups preserved");
            Check(Directory.Exists(root),"root retained when personal files remain");
            string unmanaged=Path.Combine(externalFixture,"Game");
            var other=FixtureProfile(unmanaged);
            Add(unmanaged,"Game.exe","MZ external fixture");
            var otherReference=new[]{Trusted(unmanaged,"Game.exe")};
            var externalSettings=Settings(other,managed);
            var blocked=InstallationRemoval.PlanFixture(other,externalSettings,otherReference);
            Check(!blocked.CanDeleteFiles&&blocked.Reason.Contains("outside"),"unowned external folder cannot be removed");
            Add(unmanaged,"launcher-build.json",Store.Json.Serialize(new LayerState{
                BaseFiles=new Dictionary<string,string>{{"Game.exe",otherReference[0].Hash}},
                Layers=new List<LayerRecord>(),StorageMode="Independent verified upgrade",
                RecipeId="synthetic"}));
            var marked=InstallationRemoval.PlanFixture(other,externalSettings,otherReference);
            Check(marked.CanDeleteFiles&&marked.FileCount>=1,"launcher ownership marker enables exact verified deletion");
            var duplicate=new Profile{Id="duplicate",Name="another",Root=other.Root,
                SavePath=other.SavePath,Exe="Game.exe"};
            externalSettings.Profiles.Add(duplicate);
            Check(!InstallationRemoval.PlanFixture(other,externalSettings,otherReference).CanDeleteFiles,
                "shared registered folder refuses file deletion");
            externalSettings.Profiles.Remove(duplicate);
            Refused(()=>InstallationRemoval.Plan(other,new Settings{InstallRoot=managed,Profiles=new List<Profile>()}),
                "must be registered to plan");
            var guarded=new Profile{Id="home",Root=Store.Home,SavePath=Path.Combine(Store.Home,"save")};
            var guardedSettings=Settings(guarded,managed);
            Check(!InstallationRemoval.PlanFixture(guarded,guardedSettings,otherReference).CanDeleteFiles,
                "launcher home never removable");
            Console.WriteLine("PASS "+checks+" bounded synthetic removal checks; real games/saves untouched");
            return 0;
        }catch(Exception e) {Console.Error.WriteLine(e);return 1;}
        finally {
            // Only this test's freshly generated, uniquely named sandbox.
            if(Directory.Exists(fixture)&&Path.GetFileName(fixture).StartsWith("removal-synthetic-",StringComparison.Ordinal)
                &&Path.GetDirectoryName(fixture)==Store.Home)Directory.Delete(fixture,true);
            if(Directory.Exists(externalFixture)&&Path.GetFileName(externalFixture).StartsWith("removal-synthetic-external-",StringComparison.Ordinal)
                &&Path.GetDirectoryName(externalFixture)==Path.GetDirectoryName(Store.Home))Directory.Delete(externalFixture,true);
        }
    }
}
