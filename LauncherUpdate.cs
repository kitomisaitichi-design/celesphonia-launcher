using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

// Launcher releases are distinct from game/mod releases (v1.04 etc.).
// A release contains only program assets. profiles.json, saves, settings,
// game folders, and the owner's user-translations.json are never replaced.
public class LauncherUpdateConfig {
    public string Repository {get;set;}
    public bool AutoCheck {get;set;}
    public string LastNotifiedTag {get;set;}
}
public class LauncherUpdateInfo {
    public string Tag {get;set;}
    public string Version {get;set;}
    public string Notes {get;set;}
    public string Repository {get;set;}
    public string ArchiveUrl {get;set;}
    public string ChecksumUrl {get;set;}
}
public class LauncherUpdateStage {
    public string Tag {get;set;}
    public string Repository {get;set;}
    public string ArchiveHash {get;set;}
    public Dictionary<string,string> Files {get;set;}
}
public static class LauncherUpdate {
    public const string CurrentVersion="1.0.30";
    public const string OfficialRepository="kitomisaitichi-design/celesphonia-launcher";
    public const string Asset="Celesphonia-Delta-Launcher-CH1.zip";
    const string ChecksumAsset="Celesphonia-Delta-Launcher-CH1.zip.sha256.txt";
    // GitHub account names cannot start with dots; reject relative components
    // rather than interpreting an arbitrary URI path as a release repository.
    static readonly Regex RepositoryPattern=new Regex(@"^[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9_.-]+$");
    static readonly Regex TagPattern=new Regex(@"^launcher-v(\d+\.\d+\.\d+)$",RegexOptions.IgnoreCase);
    static string ConfigFile {get{return Path.Combine(Store.Home,"launcher-update.json");}}
    public static string StageHome {get{return Path.Combine(Store.Home,"Staging","LauncherUpdates");}}
    public static LauncherUpdateConfig Load(){
        // Fresh portable downloads work without a manually created settings
        // file. Existing configurations (including an explicitly empty
        // repository or disabled auto-check) are never replaced.
        if(!File.Exists(ConfigFile))return new LauncherUpdateConfig{Repository=OfficialRepository,AutoCheck=true};
        var result=Store.Json.Deserialize<LauncherUpdateConfig>(File.ReadAllText(ConfigFile,Encoding.UTF8));
        return result??new LauncherUpdateConfig{Repository=OfficialRepository,AutoCheck=true};
    }
    public static void Save(LauncherUpdateConfig config){
        string repo=(config.Repository??"").Trim();
        if(repo!=""&&!ValidRepository(repo))throw new IOException("Use GitHub owner/repository (for example, your-account/celesphonia-launcher).");
        config.Repository=repo;
        string temp=ConfigFile+".new";
        File.WriteAllText(temp,Store.Json.Serialize(config),new UTF8Encoding(false));
        if(File.Exists(ConfigFile))File.Replace(temp,ConfigFile,null);else File.Move(temp,ConfigFile);
    }
    static bool ValidRepository(string value){
        return RepositoryPattern.IsMatch(value)&&value.Split('/')[1]!="."&&value.Split('/')[1]!="..";
    }
    public static bool IsNewer(string other){
        Version latest,current;
        return Version.TryParse(other,out latest)&&Version.TryParse(CurrentVersion,out current)&&latest>current;
    }
    class TimedWebClient:WebClient {
        protected override WebRequest GetWebRequest(Uri address){
            var request=base.GetWebRequest(address);request.Timeout=15000;
            var http=request as HttpWebRequest;if(http!=null)http.ReadWriteTimeout=15000;
            return request;
        }
    }
    static WebClient Client(){
        ServicePointManager.SecurityProtocol|=(SecurityProtocolType)3072; // TLS 1.2 on .NET Framework 4.x
        var client=new TimedWebClient();client.Headers[HttpRequestHeader.UserAgent]="Celesphonia-Mod-Launcher/"+CurrentVersion;
        client.Headers[HttpRequestHeader.Accept]="application/vnd.github+json";
        return client;
    }
    static string Value(Dictionary<string,object> row,string key){
        object obj;return row.TryGetValue(key,out obj)&&obj!=null?Convert.ToString(obj):"";
    }
    static string AssetUrl(Dictionary<string,object> release,string name,string repo,string tag){
        object raw;if(!release.TryGetValue("assets",out raw)||!(raw is IEnumerable))return null;
        foreach(object entry in (IEnumerable)raw){
            var item=entry as Dictionary<string,object>;if(item==null||Value(item,"name")!=name)continue;
            string url=Value(item,"browser_download_url");
            string expected="https://github.com/"+repo+"/releases/download/"+Uri.EscapeDataString(tag)+"/"+name;
            if(!url.Equals(expected,StringComparison.Ordinal))throw new IOException("Release asset URL does not match the configured GitHub repository.");
            return url;
        }
        return null;
    }
    // GitHub's /latest is publication-date ordered. A newer version may have
    // been published before a maintenance re-release, so compare every stable
    // launcher tag numerically. Ignore unrelated game-mod or other tags.
    static LauncherUpdateInfo SelectLatest(IEnumerable releases,string repo){
        Dictionary<string,object> best=null;
        Version bestVersion=null;
        string bestTag=null;
        foreach(object raw in releases){
            var row=raw as Dictionary<string,object>;if(row==null)continue;
            if(row.ContainsKey("draft")&&Convert.ToBoolean(row["draft"])
                ||row.ContainsKey("prerelease")&&Convert.ToBoolean(row["prerelease"]))continue;
            string tag=Value(row,"tag_name");var match=TagPattern.Match(tag);
            if(!match.Success)continue;
            Version parsed;
            if(!Version.TryParse(match.Groups[1].Value,out parsed))continue;
            if(bestVersion==null||parsed>bestVersion){best=row;bestVersion=parsed;bestTag=tag;}
        }
        if(best==null||!IsNewer(bestVersion.ToString()))return null;
        string zip=AssetUrl(best,Asset,repo,bestTag),digest=AssetUrl(best,ChecksumAsset,repo,bestTag);
        if(zip==null||digest==null)throw new IOException(
            "Newest launcher release "+bestTag+" must contain both the ZIP and its SHA-256 checksum.");
        return new LauncherUpdateInfo{Tag=bestTag,Version=bestVersion.ToString(),
            Notes=Value(best,"body"),Repository=repo,ArchiveUrl=zip,ChecksumUrl=digest};
    }
    public static LauncherUpdateInfo Check(LauncherUpdateConfig config){
        string repo=(config.Repository??"").Trim();
        if(!ValidRepository(repo))throw new IOException("Configure a GitHub Releases repository in Help → GitHub source first.");
        string api="https://api.github.com/repos/"+repo+"/releases?per_page=100";
        string body;using(var client=Client())body=client.DownloadString(api);
        var data=Store.Json.Deserialize<List<Dictionary<string,object>>>(body);
        if(data==null)throw new IOException("GitHub Releases returned an unexpected format.");
        return SelectLatest(data,repo);
    }
    static string Hash(string filename){
        using(var file=File.OpenRead(filename))using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
    }
    static string SafeRelative(string name){
        if(string.IsNullOrEmpty(name)||name.Contains("\\")||name.StartsWith("/")||name.Contains(":")||name.Contains("//"))
            throw new IOException("Unsafe launcher update ZIP entry: "+name);
        var bits=name.Split('/');
        if(bits.Any(x=>x==""||x=="."||x==".."))throw new IOException("Update ZIP attempts directory traversal.");
        // Keep the original 1.0.0–1.0.2 ZIP allowlist compatible. Older
        // installed launchers must be able to verify any newer release,
        // without encountering an unapproved new root entry.
        bool root=new[]{"Celesphonia Portable Switcher.exe","EnglishRuntime.js","README.txt","mod-recipes.json","version-fingerprints.json"}.Contains(name);
        bool english=Regex.IsMatch(name,@"^English/[a-z0-9-]+/(manifest|catalog|dictionary)\.json$");
        if(!root&&!english)throw new IOException("Update contains an unapproved or private file: "+name);
        return name;
    }
    static string Child(string root,string relative){
        string baseDir=Path.GetFullPath(root).TrimEnd('\\','/')+Path.DirectorySeparatorChar;
        string full=Path.GetFullPath(Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar)));
        if(!full.StartsWith(baseDir,StringComparison.OrdinalIgnoreCase))throw new IOException("Update escapes its intended directory.");
        return full;
    }
    public static Dictionary<string,string> VerifyZip(string zip,bool extract,string output){
        var files=new Dictionary<string,string>(StringComparer.Ordinal);
        long uncompressed=0;
        using(var archive=ZipFile.OpenRead(zip)){
            if(archive.Entries.Count<5||archive.Entries.Count>60)throw new IOException("Unexpected launcher release file count.");
            foreach(var entry in archive.Entries){
                string name=SafeRelative(entry.FullName);
                if(files.ContainsKey(name))throw new IOException("Duplicate launcher asset: "+name);
                // UNIX symlinks and directories are forbidden in a release.
                if(((entry.ExternalAttributes>>16)&0xF000)==0xA000||entry.Name=="")throw new IOException("Linked/directory update ZIP entries are not supported.");
                uncompressed+=entry.Length;
                if(entry.Length>45L*1024*1024||uncompressed>180L*1024*1024)throw new IOException("Update ZIP has oversized payloads.");
                string temp=extract?Child(output,name):null;
                if(extract)Directory.CreateDirectory(Path.GetDirectoryName(temp));
                using(var src=entry.Open())using(var sha=SHA256.Create()){
                    Stream dest=extract?(Stream)File.Create(temp):Stream.Null;
                    try{
                        byte[] block=new byte[65536];int read;
                        while((read=src.Read(block,0,block.Length))>0){
                            sha.TransformBlock(block,0,read,null,0);
                            if(extract)dest.Write(block,0,read);
                        }
                        sha.TransformFinalBlock(new byte[0],0,0);
                    }finally{if(extract)dest.Dispose();}
                    files[name]=BitConverter.ToString(sha.Hash).Replace("-","").ToLowerInvariant();
                }
            }
        }
        foreach(string required in new[]{"Celesphonia Portable Switcher.exe","mod-recipes.json","version-fingerprints.json","README.txt"})
            if(!files.ContainsKey(required))throw new IOException("Launcher release missing: "+required);
        return files;
    }
    static string Checksum(string contents){
        var match=Regex.Match(contents.Trim(),@"^([a-fA-F0-9]{64})(?:\s+\*?Celesphonia-Delta-Launcher-CH1\.zip)?\s*$");
        if(!match.Success)throw new IOException("Release checksum asset has an unexpected format.");
        return match.Groups[1].Value.ToLowerInvariant();
    }
    public static string ChangelogText(){
        // Bundled INSIDE the EXE (an existing allowlisted asset) so the
        // documentation can be exported without introducing a ZIP entry
        // that old launcher updaters would reject.
        using(var stream=typeof(LauncherUpdate).Assembly.GetManifestResourceStream("Celesphonia.GameModChangelog.md")){
            if(stream==null)throw new IOException("Embedded game-mod changelog resource missing.");
            using(var reader=new StreamReader(stream,Encoding.UTF8,true))return reader.ReadToEnd();
        }
    }
    public static void ExportChangelog(string path){
        if(File.Exists(path))throw new IOException("Refusing to overwrite an existing changelog.");
        File.WriteAllText(path,ChangelogText(),new UTF8Encoding(false));
    }
    public static string Stage(LauncherUpdateInfo info){
        if(info==null||!IsNewer(info.Version)||!TagPattern.IsMatch(info.Tag))throw new IOException("Not a newer launcher version.");
        string dir=Child(StageHome,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        string zip=Path.Combine(dir,Asset);
        try{
            string checksum;
            using(var client=Client())checksum=Checksum(client.DownloadString(info.ChecksumUrl));
            using(var client=Client())client.DownloadFile(info.ArchiveUrl,zip);
            if(new FileInfo(zip).Length>200L*1024*1024||Hash(zip)!=checksum)throw new IOException("Downloaded launcher ZIP failed the published SHA-256 checksum.");
            string output=Path.Combine(dir,"payload");
            var fileHashes=VerifyZip(zip,true,output);
            string executable=Path.Combine(output,"Celesphonia Portable Switcher.exe");
            using(var stream=File.OpenRead(executable))
                if(stream.ReadByte()!=0x4d||stream.ReadByte()!=0x5a)throw new IOException("Downloaded launcher is not a Windows executable.");
            var marker=new LauncherUpdateStage{Tag=info.Tag,Repository=info.Repository,ArchiveHash=checksum,Files=fileHashes};
            string manifest=Path.Combine(dir,"manifest.json");
            File.WriteAllText(manifest,Store.Json.Serialize(marker),new UTF8Encoding(false));
            return manifest;
        }catch{ /* Keep staged contents for diagnosis; do not modify installed app. */ throw; }
    }
    public static void StartApply(string manifest){
        var start=ApplyStartInfo(Store.Home,manifest,Process.GetCurrentProcess().Id);
        if(Process.Start(start)==null)throw new IOException("Could not start the verified staged launcher update.");
    }
    static ProcessStartInfo ApplyStartInfo(string home,string manifest,int parent){
        string directory;ReadVerifiedStage(home,manifest,out directory);
        // The downloaded launcher already supports this helper command. Use
        // that verified executable instead of copying ourselves to agent-GUID.exe.
        string executable=Child(Path.Combine(directory,"payload"),"Celesphonia Portable Switcher.exe");
        return new ProcessStartInfo(executable,"--apply-launcher-update "+Store.Q(home)+" "+Store.Q(manifest)+" "+parent){
            WorkingDirectory=directory,UseShellExecute=false,CreateNoWindow=true
        };
    }
    public static void Apply(string home,string manifest,int parent){
        ApplyInternal(home,manifest,parent,true);
    }
    static LauncherUpdateStage ReadVerifiedStage(string home,string manifest,out string directory){
        string baseDir=Path.GetFullPath(Path.Combine(home,"Staging","LauncherUpdates")).TrimEnd('\\')+Path.DirectorySeparatorChar;
        string path=Path.GetFullPath(manifest);directory=Path.GetDirectoryName(path);
        if(!path.StartsWith(baseDir,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(path)!="manifest.json")
            throw new IOException("Update manifest must be staged inside the launcher update directory.");
        string zip=Path.Combine(directory,Asset),payload=Path.Combine(directory,"payload");
        var marker=Store.Json.Deserialize<LauncherUpdateStage>(File.ReadAllText(path,Encoding.UTF8));
        if(marker==null||!TagPattern.IsMatch(marker.Tag)||marker.Files==null||marker.Files.Count<5)
            throw new IOException("Invalid staged update manifest.");
        if(Hash(zip)!=marker.ArchiveHash)throw new IOException("Staged update archive checksum differs.");
        var actual=VerifyZip(zip,false,null);
        if(actual.Count!=marker.Files.Count||actual.Any(x=>!marker.Files.ContainsKey(x.Key)||marker.Files[x.Key]!=x.Value))
            throw new IOException("Staged launcher update contents differ from the verified ZIP.");
        foreach(var item in marker.Files)if(!File.Exists(Child(payload,item.Key))||Hash(Child(payload,item.Key))!=item.Value)
            throw new IOException("Staged launcher update file modified: "+item.Key);
        return marker;
    }
    static void ApplyInternal(string home,string manifest,int parent,bool restart){
        string directory;var marker=ReadVerifiedStage(home,manifest,out directory);
        string payload=Path.Combine(directory,"payload");
        if(parent>0)try{
            using(var process=Process.GetProcessById(parent))
                if(!process.WaitForExit(60000))throw new IOException("Original launcher did not exit; update not applied.");
        }catch(ArgumentException){/* Parent already exited. */}
        string rollback=Child(directory,"rollback"),log=Path.Combine(directory,"update-result.txt");
        Directory.CreateDirectory(rollback);
        var modified=new List<string>();var newFiles=new List<string>();int unchanged=0;
        try{
            foreach(var item in marker.Files.OrderBy(x=>x.Key=="Celesphonia Portable Switcher.exe"?1:0)){
                string source=Child(payload,item.Key),target=Child(home,item.Key),saved=Child(rollback,item.Key);
                // Translation catalogs are large and normally unchanged. Do
                // not back up or replace an already-identical installed file.
                if(File.Exists(target)&&Hash(target)==item.Value){unchanged++;continue;}
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                bool existed=File.Exists(target);
                if(existed){
                    Directory.CreateDirectory(Path.GetDirectoryName(saved));File.Copy(target,saved);
                }
                string temp=target+".launcher-upgrade-new";
                File.Copy(source,temp,true);
                if(existed){File.Replace(temp,target,null);modified.Add(item.Key);}
                else{File.Move(temp,target);newFiles.Add(item.Key);}
                if(Hash(target)!=item.Value)throw new IOException("Installed asset digest mismatched: "+item.Key);
            }
            File.WriteAllText(log,"INSTALLED "+marker.Tag+"; changed="+(modified.Count+newFiles.Count)+"; unchanged="+unchanged+"; rollback retained in "+rollback,Encoding.UTF8);
        }catch(Exception problem){
            var rollbackErrors=new List<string>();
            foreach(string name in modified.AsEnumerable().Reverse())try{
                string to=Child(home,name),previous=Child(rollback,name),temp=to+".launcher-rollback";
                if(!File.Exists(previous))throw new IOException("Rollback original is missing.");
                File.Copy(previous,temp,true);if(File.Exists(to))File.Replace(temp,to,null);else File.Move(temp,to);
                if(Hash(to)!=Hash(previous))throw new IOException("Restored asset digest mismatched.");
            }catch(Exception restoreProblem){rollbackErrors.Add(name+": "+restoreProblem.Message);}
            foreach(string name in newFiles)try{
                string target=Child(home,name);
                if(File.Exists(target)){
                    if(Hash(target)!=marker.Files[name])throw new IOException("New file changed externally; preserving it.");
                    File.Delete(target);
                }
            }catch(Exception restoreProblem){rollbackErrors.Add(name+": "+restoreProblem.Message);}
            File.WriteAllText(log,"FAILED "+problem+"; rollback errors="+rollbackErrors.Count+"; rollback retained in "+rollback+"\r\n"+string.Join("\r\n",rollbackErrors),Encoding.UTF8);
            if(rollbackErrors.Count>0)throw new IOException(problem.Message+"\r\nRollback errors:\r\n"+string.Join("\r\n",rollbackErrors),problem);
            throw;
        }
        if(restart)Process.Start(new ProcessStartInfo(Path.Combine(home,"Celesphonia Portable Switcher.exe")){WorkingDirectory=home,UseShellExecute=true});
    }
    public static void TestApply(string zip,string output){
        // Keep fixture paths short enough for older .NET Framework/Win32 MAX_PATH.
        // The disposable folder is created under the current user's temp root.
        string root=Path.Combine(Path.GetTempPath(),"CelesphoniaUpdaterFixture-"+Guid.NewGuid().ToString("N")),
            home=Path.Combine(root,"app");
        Directory.CreateDirectory(home);
        try{
            string stage=Path.Combine(home,"Staging","LauncherUpdates","candidate");
            Directory.CreateDirectory(stage);
            string archive=Path.Combine(stage,Asset);
            File.Copy(zip,archive);
            var hashes=VerifyZip(archive,true,Path.Combine(stage,"payload"));
            string exe=Path.Combine(home,"Celesphonia Portable Switcher.exe"),config=Path.Combine(home,"profiles.json"),
                save=Path.Combine(home,"Game Profiles","existing-save.rpgsave");
            Directory.CreateDirectory(Path.GetDirectoryName(save));
            File.WriteAllText(exe,"OLD-LAUNCHER");
            File.WriteAllText(config,"PRIVATE-PROFILES-SENTINEL");
            File.WriteAllText(save,"PRIVATE-SAVE-SENTINEL");
            var marker=new LauncherUpdateStage{Tag="launcher-v1.0.5",Repository="example/example",
                ArchiveHash=Hash(archive),Files=hashes};
            string manifest=Path.Combine(stage,"manifest.json");
            File.WriteAllText(manifest,Store.Json.Serialize(marker));
            ApplyInternal(home,manifest,0,false);
            if(Hash(exe)!=hashes["Celesphonia Portable Switcher.exe"]||
               File.ReadAllText(config)!="PRIVATE-PROFILES-SENTINEL"||
               File.ReadAllText(save)!="PRIVATE-SAVE-SENTINEL"||
               File.ReadAllText(Path.Combine(stage,"rollback","Celesphonia Portable Switcher.exe"))!="OLD-LAUNCHER")
               throw new IOException("Isolated staged launcher replacement or rollback preservation failed.");
            File.WriteAllText(output,Store.Json.Serialize(new{
                status="PASS",isolatedInstallation=true,exeReplaced=true,originalRollbackPreserved=true,
                privateProfilesPreserved=true,privateSavesPreserved=true,restartSkippedInFixture=true
            }),new UTF8Encoding(false));
        }finally{
            // Only clean the exact uniquely-created test folder under our
            // launcher-owned Staging directory, never a game or user folder.
            if(Directory.Exists(root)&&Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\')+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)
                &&Path.GetFileName(root).StartsWith("CelesphoniaUpdaterFixture-",StringComparison.Ordinal))
                Directory.Delete(root,true);
        }
    }
    public static void TestFirstRunDefaults(string output){
        // Run only inside a dedicated disposable testing installation.
        // Never overwrite real update preferences or delete any user files.
        if(!Path.GetFileName(Store.Home).StartsWith("Celesphonia-Update-QA-",StringComparison.Ordinal)
           ||File.Exists(ConfigFile))throw new IOException("Fresh-update test needs its own empty QA installation.");
        var fresh=Load();
        if(fresh.Repository!=OfficialRepository||!fresh.AutoCheck||fresh.LastNotifiedTag!=null)
            throw new IOException("Fresh download does not automatically check the official release feed.");
        var disabled=new LauncherUpdateConfig{Repository="",AutoCheck=false,LastNotifiedTag="user-disabled"};
        Save(disabled);
        var readDisabled=Load();
        if(readDisabled.Repository!=""||readDisabled.AutoCheck||readDisabled.LastNotifiedTag!="user-disabled")
            throw new IOException("Explicit opt-out preferences were not preserved.");
        var custom=new LauncherUpdateConfig{Repository="custom-owner/custom-launcher",AutoCheck=true,LastNotifiedTag="launcher-v9.0.0"};
        Save(custom);
        var again=Load();
        if(again.Repository!=custom.Repository||!again.AutoCheck||again.LastNotifiedTag!=custom.LastNotifiedTag)
            throw new IOException("User custom repository was overwritten by the official default.");
        File.WriteAllText(output,Store.Json.Serialize(new{status="PASS",version=CurrentVersion,
            officialRepository=OfficialRepository,freshAutoCheck=true,withoutInitialConfig=true,
            manualSourceSetupRequired=false,optOutPreserved=true,customRepositoryPreserved=true,
            existingPreferencesPreserved=true}),new UTF8Encoding(false));
    }
    public static void Test(string archive,string output){
        var positive=VerifyZip(archive,false,null);
        if(!positive.ContainsKey("Celesphonia Portable Switcher.exe")||
           positive.ContainsKey("GAME MOD CHANGELOG.md")||IsNewer(CurrentVersion)||
           !IsNewer("1.0.7")||IsNewer("1.0.5")||!TagPattern.IsMatch("launcher-v1.0.7")||
           TagPattern.IsMatch("v1.04")||RepositoryPattern.IsMatch("../escape")||
           !Checksum(new string('a',64)+"  "+Asset).Equals(new string('a',64)))throw new IOException("Launcher update validation invariants failed.");
        bool traversal=false,privateFile=false;
        try{SafeRelative("../profiles.json");}catch(IOException){traversal=true;}
        try{SafeRelative("Save Backups/slot1.rpgsave");}catch(IOException){privateFile=true;}
        if(!traversal||!privateFile)throw new IOException("Unsafe update ZIP entry was permitted.");
        Func<string,Dictionary<string,object>> release=tag=>{
            string baseUrl="https://github.com/example/launcher/releases/download/"+tag+"/";
            return new Dictionary<string,object>{
                {"tag_name",tag},{"draft",false},{"prerelease",false},
                {"assets",new object[]{
                    new Dictionary<string,object>{{"name",Asset},{"browser_download_url",baseUrl+Asset}},
                    new Dictionary<string,object>{{"name",ChecksumAsset},{"browser_download_url",baseUrl+ChecksumAsset}}
                }}
            };
        };
        var latest=SelectLatest(new object[]{release("launcher-v1.0.4"),
            release("v1.04"),release("launcher-v1.2.0"),release("launcher-v1.0.9")},
            "example/launcher");
        if(latest==null||latest.Version!="1.2.0"||latest.Tag!="launcher-v1.2.0")
            throw new IOException("GitHub release version ordering ignored semantic version.");
        var skipped=release("launcher-v9.0.0");skipped["prerelease"]=true;
        if(SelectLatest(new object[]{skipped,release("launcher-v1.0.0")},"example/launcher")!=null)
            throw new IOException("Prerelease or installed version was treated as an update.");
        var missing=release("launcher-v1.3.0");missing["assets"]=new object[0];
        bool missingRejected=false;try{SelectLatest(new object[]{missing,release("launcher-v1.2.0")},"example/launcher");}
            catch(IOException){missingRejected=true;}
        if(!missingRejected)throw new IOException("Incomplete newest launcher version was silently skipped.");
        string text=ChangelogText();
        foreach(string version in new[]{"v1.01","v1.02","v1.03","v1.04"})
            if(!text.Contains(version))throw new IOException("Embedded creator changelog missing "+version);
        if(positive.Count!=29)throw new IOException("Updater compatibility requires the original 29 release entries.");
        File.WriteAllText(output,Store.Json.Serialize(new{status="PASS",version=CurrentVersion,zipFiles=positive.Count,
            gameAndSaveFilesExcluded=true,pathTraversalRejected=true,forwardVersionsOnly=true,
            githubChecksumRequired=true,gameModVersionIndependent=true,
            numericVersionOrderingVerified=true,prereleasesIgnored=true,incompleteNewestReleaseRejected=true}),new UTF8Encoding(false));
    }
}
