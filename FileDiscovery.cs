using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

public class FoundSource {
    public string Kind,Path,RecipeId,Name,Problem,Detection; public long Bytes;
    public override string ToString(){return Name+"  ·  "+Kind+(string.IsNullOrEmpty(Problem)?"":"  ·  "+Problem);}
}
public class DiscoveryProgress {public int FoldersVisited,SourcesFound;public string CurrentFolder;public bool LimitReached;public FoundSource[] Sources;}
public class KnownInstallation {public string Root {get;set;}public string VersionId {get;set;}}
public class InstallationLocations {public int Format {get;set;}public List<KnownInstallation> Installations {get;set;}}
// Machine-local discovery hints. Profiles, controls and selections remain portable/local.
public static class SharedInstallationIndex {
    public static readonly string IndexFile=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Celesphonia","Launcher","installations.json");
    public static string Problem {get;private set;}
    static string Key(string path){if(string.IsNullOrWhiteSpace(path))return "";try{return System.IO.Path.GetFullPath(path).TrimEnd('\\','/');}catch(ArgumentException){return path;}}
    static InstallationLocations Read(string file){
        if(!File.Exists(file))return new InstallationLocations{Format=1,Installations=new List<KnownInstallation>()};
        if(new FileInfo(file).Length>2*1024*1024)throw new IOException("Installation index is too large.");
        var data=Store.Json.Deserialize<InstallationLocations>(File.ReadAllText(file,Encoding.UTF8));
        if(data==null||data.Format!=1||data.Installations==null)throw new IOException("Invalid installation index format.");
        return data;
    }
    static Profile Verified(string root,string version=null){
        try{
            if(string.IsNullOrWhiteSpace(root)||!System.IO.Path.IsPathRooted(root))return null;
            root=Key(root);if(root.StartsWith(@"\\")||FileDiscovery.IsNativeProbe(root))return null;
            for(var folder=new DirectoryInfo(root);folder!=null;folder=folder.Parent)if(folder.Name.Equals("Control Sessions",StringComparison.OrdinalIgnoreCase))return null;
            if(!File.Exists(System.IO.Path.Combine(root,"package.json")))return null;
            string id=UniversalSupport.Identify(root);if(id==null||(version!=null&&id!=version))return null;
            var p=Store.Detect(root,UniversalSupport.ReadIndex().Versions.First(x=>x.Id==id).Name);
            using(var executable=File.OpenRead(System.IO.Path.Combine(root,p.Exe)))if(executable.ReadByte()!='M'||executable.ReadByte()!='Z')return null;
            return p;
        }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}return null;
    }
    public static List<Profile> VerifiedProfiles(string file=null){
        file=file??IndexFile;try{return Read(file).Installations.Where(x=>x!=null).Select(x=>Verified(x.Root,x.VersionId)).Where(x=>x!=null).GroupBy(x=>Key(x.Root),StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();}
        catch(Exception e){if(!(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException))throw;Problem="Could not read shared installation locations: "+e.Message;return new List<Profile>();}
    }
    public static int Restore(Settings settings,string file=null){
        if(settings.Profiles==null)settings.Profiles=new List<Profile>();int added=0;
        foreach(var p in VerifiedProfiles(file)){
            if(settings.Profiles.Any(x=>x!=null&&string.Equals(Key(x.Root),Key(p.Root),StringComparison.OrdinalIgnoreCase)))continue;
            p.Note="Existing installation found; using its current files and saves";settings.Profiles.Add(p);added++;
        }
        return added;
    }
    public static void Sync(Settings settings){Problem=null;Remember(settings.Profiles);Restore(settings);}
    public static void Remember(IEnumerable<Profile> profiles,string file=null){
        file=file??IndexFile;
        try{
            var valid=(profiles??Enumerable.Empty<Profile>()).Where(x=>x!=null).Select(x=>Verified(x.Root)).Where(x=>x!=null).ToList();if(valid.Count==0)return;
            string mutexName=@"Local\CelesphoniaInstallationIndex-"+EnglishRelease.Hash(Encoding.UTF8.GetBytes(Key(file))).Substring(0,24);
            using(var mutex=new Mutex(false,mutexName)){
                bool owned=false;try{
                    try{owned=mutex.WaitOne(2000);}catch(AbandonedMutexException){owned=true;}
                    if(!owned)throw new IOException("Another launcher is updating installation locations; retry discovery.");
                    InstallationLocations data;try{data=Read(file);}catch(Exception e){if(!(e is IOException||e is ArgumentException||e is InvalidOperationException))throw;Problem="Damaged shared installation index recovered; previous contents retained in installations.json.previous. "+e.Message;data=new InstallationLocations{Format=1,Installations=new List<KnownInstallation>()};}
                    var entries=data.Installations.Where(x=>x!=null&&!string.IsNullOrWhiteSpace(x.Root)&&System.IO.Path.IsPathRooted(x.Root)).GroupBy(x=>Key(x.Root),StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.First(),StringComparer.OrdinalIgnoreCase);
                    foreach(var p in valid)entries[Key(p.Root)]=new KnownInstallation{Root=Key(p.Root),VersionId=UniversalSupport.Identify(p.Root)};
                    data.Installations=entries.Values.OrderBy(x=>x.Root,StringComparer.OrdinalIgnoreCase).ToList();string text=Store.Json.Serialize(data);
                    if(File.Exists(file)&&File.ReadAllText(file,Encoding.UTF8)==text)return;
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file)));
                    string temp=file+".new-"+Guid.NewGuid().ToString("N");File.WriteAllText(temp,text,new UTF8Encoding(false));
                    try{if(File.Exists(file))File.Replace(temp,file,file+".previous");else File.Move(temp,file);}finally{if(File.Exists(temp))File.Delete(temp);}
                }finally{if(owned)mutex.ReleaseMutex();}
            }
        }catch(Exception e){if(!(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException||e is System.Security.SecurityException))throw;Problem="Could not share installation locations: "+e.Message;}
    }
}
public static class FileDiscovery {
    public static bool IsNativeProbe(string root){
        try{var folder=new DirectoryInfo(System.IO.Path.GetFullPath(root));for(;folder!=null;folder=folder.Parent)if(File.Exists(System.IO.Path.Combine(folder.FullName,".celesphonia-native-probe")))return true;}
        catch(ArgumentException){}catch(IOException){}catch(UnauthorizedAccessException){}return false;
    }
    public static string ArchiveFormat(string file){
        try{using(var s=File.OpenRead(file)){var b=new byte[8];int n=s.Read(b,0,b.Length);if(n>=4&&b[0]==0x50&&b[1]==0x4b&&((b[2]==3&&b[3]==4)||(b[2]==5&&b[3]==6)||(b[2]==7&&b[3]==8)))return "zip";if(n>=7&&b[0]==0x52&&b[1]==0x61&&b[2]==0x72&&b[3]==0x21&&b[4]==0x1a&&b[5]==0x07)return "rar";if(n>=6&&b[0]==0x37&&b[1]==0x7a&&b[2]==0xbc&&b[3]==0xaf&&b[4]==0x27&&b[5]==0x1c)return "7z";}}catch(IOException){}catch(UnauthorizedAccessException){}return null;
    }
    class ScanBudget{
        public int Remaining=3500,Visited;public HashSet<string> Expanded=new HashSet<string>(StringComparer.OrdinalIgnoreCase);public CancellationToken Cancellation;public Action<DiscoveryProgress> Progress;long notified;
        public void Check(){Cancellation.ThrowIfCancellationRequested();}
        public void Report(string folder,List<FoundSource> found,bool final=false){Check();long now=Environment.TickCount; if(Progress!=null&&(final||now-notified>=200)){notified=now;Progress(new DiscoveryProgress{FoldersVisited=Visited,SourcesFound=found.Count,CurrentFolder=folder,LimitReached=Remaining<=0,Sources=found.ToArray()});}}
    }
    public static bool IsCH(string root){try{var marks=RecipeSupport.Catalog().ChFingerprint;if(marks==null||marks.Count==0)return false;string web=Store.WebRoot(root);return marks.All(x=>File.Exists(Store.Child(web,x.Key.Replace('/','\\')))&&Store.Hash(Store.Child(web,x.Key.Replace('/','\\')))==x.Value);}catch{return false;}}
    static string Token(string name){return new string(name.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());}
    public static ModRecipe NameHint(string name,RecipeCatalog catalog){
        string token=Token(name);var exact=catalog.Recipes.FirstOrDefault(x=>!string.IsNullOrEmpty(x.ArchiveName)&&Token(x.ArchiveName)==token);if(exact!=null)return exact;
        string lower=name.ToLowerInvariant();string id=null;
        if((lower.Contains("maple")||lower.Contains("夜枫"))&&Regex.IsMatch(lower,@"1[._-]7[._-]2"))id="maple-1-7-2";
        else if(lower.Contains("3.3.4")||lower.Contains("3_3_4"))id="three-in-one-3-3-4";
        else if(lower.Contains("3.3.5")||lower.Contains("3_3_5")){
            var number=Regex.Match(lower,@"(?:v|ver|版|[_ -])1[._-](\d{2,3})(?:[^0-9]|$)");
            if(number.Success){
                id="three-in-one-3-3-5-v1-"+number.Groups[1].Value;
                // An unknown future release must stay unverified, never be
                // silently treated as the old trial or a previous version.
                if(!catalog.Recipes.Any(x=>x.Id==id))return null;
            }else id="three-in-one-3-3-5-trial";
        }
        return catalog.Recipes.FirstOrDefault(x=>x.Id==id);
    }
    static void AddRoot(List<string> roots,string value){
        if(string.IsNullOrWhiteSpace(value))return;try{string full=System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(value));if(!full.StartsWith(@"\\")&&Directory.Exists(full)&&!roots.Any(x=>x.Equals(full,StringComparison.OrdinalIgnoreCase)))roots.Add(full);}catch(ArgumentException){}catch(IOException){}
    }
    public static List<string> NativeRoots(Settings settings){
        var roots=new List<string>();string user=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach(var profile in SharedInstallationIndex.VerifiedProfiles())AddRoot(roots,profile.Root);
        AddRoot(roots,Store.Home);AddRoot(roots,settings.InstallRoot);
        AddLocalLibrary(roots,Store.Home);
        // Another extracted launcher beside this one can own a real installation.
        // Inspect only recognized launcher libraries, before the broad disk search.
        try{var parent=Directory.GetParent(Store.Home);if(parent!=null)foreach(string sibling in Directory.GetDirectories(parent.FullName))
            if(File.Exists(System.IO.Path.Combine(sibling,"Celesphonia Portable Switcher.exe"))&&File.Exists(System.IO.Path.Combine(sibling,"mod-recipes.json")))AddLocalLibrary(roots,sibling);
        }catch(IOException){}catch(UnauthorizedAccessException){}
        if(settings.Profiles!=null)foreach(var profile in settings.Profiles){AddRoot(roots,profile.Root);var parent=Directory.GetParent(profile.Root);if(parent!=null&&parent.Parent!=null)AddRoot(roots,parent.FullName);}
        if(settings.SearchRoots!=null)foreach(string root in settings.SearchRoots)AddRoot(roots,root);
        try{using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))if(key!=null)AddRoot(roots,Convert.ToString(key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}")));}catch(System.Security.SecurityException){}
        AddRoot(roots,System.IO.Path.Combine(user,"Downloads","Telegram Desktop"));AddRoot(roots,System.IO.Path.Combine(user,"Downloads"));
        AddRoot(roots,Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        foreach(string browser in BrowserDownloadFolders())AddRoot(roots,browser);
        // Discover current-machine library locations, never authoring-machine paths.
        try{using(var steam=Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))if(steam!=null){
            string basePath=Convert.ToString(steam.GetValue("SteamPath"));AddRoot(roots,System.IO.Path.Combine(basePath,"steamapps","common"));
            string libraries=System.IO.Path.Combine(basePath,"steamapps","libraryfolders.vdf");
            if(File.Exists(libraries)&&new FileInfo(libraries).Length<1048576)foreach(Match m in Regex.Matches(File.ReadAllText(libraries),"\"path\"\\s+\"([^\"]+)\""))AddRoot(roots,System.IO.Path.Combine(m.Groups[1].Value.Replace(@"\\",@"\"),"steamapps","common"));
        }}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(System.Security.SecurityException){}
        foreach(var drive in DriveInfo.GetDrives().Where(x=>x.DriveType==DriveType.Fixed))foreach(string relative in new[]{"Games","GOG Games",@"SteamLibrary\steamapps\common"})AddRoot(roots,System.IO.Path.Combine(drive.Name,relative));
        AddRoot(roots,Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        return roots;
    }
    static void AddLocalLibrary(List<string> roots,string launcher){
        string library=System.IO.Path.Combine(launcher,"Game Profiles");AddRoot(roots,library);
        try{if(Directory.Exists(library))foreach(string game in Directory.GetDirectories(library).Take(64))
            if(!IsNativeProbe(game))AddRoot(roots,game);
        }catch(IOException){}catch(UnauthorizedAccessException){}
    }
    static IEnumerable<string> BrowserDownloadFolders(){
        var roots=new List<string>();string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),roaming=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        foreach(string relative in new[]{@"Google\Chrome\User Data",@"Google\Chrome Beta\User Data",@"Microsoft\Edge\User Data",@"Microsoft\Edge Beta\User Data",@"BraveSoftware\Brave-Browser\User Data",@"Chromium\User Data",@"Vivaldi\User Data"}){
            string root=System.IO.Path.Combine(local,relative);try{if(!Directory.Exists(root))continue;foreach(string dir in Directory.GetDirectories(root)){string name=System.IO.Path.GetFileName(dir);if(name=="Default"||name.StartsWith("Profile ",StringComparison.OrdinalIgnoreCase))ReadChromiumPreferences(System.IO.Path.Combine(dir,"Preferences"),roots);}}catch(IOException){}catch(UnauthorizedAccessException){}
        }
        foreach(string opera in new[]{"Opera Stable","Opera GX Stable","Opera Developer"})ReadChromiumPreferences(System.IO.Path.Combine(roaming,"Opera Software",opera,"Preferences"),roots);
        string firefox=System.IO.Path.Combine(roaming,"Mozilla","Firefox","Profiles");
        try{if(Directory.Exists(firefox))foreach(string profile in Directory.GetDirectories(firefox))ReadFirefoxPreferences(System.IO.Path.Combine(profile,"prefs.js"),roots);}catch(IOException){}catch(UnauthorizedAccessException){}
        string profilesIni=System.IO.Path.Combine(roaming,"Mozilla","Firefox","profiles.ini");
        try{if(File.Exists(profilesIni)){bool relative=true;foreach(string line in File.ReadLines(profilesIni)){if(line.StartsWith("["))relative=true;else if(line.StartsWith("IsRelative="))relative=line.Substring(11).Trim()!="0";else if(line.StartsWith("Path=")){string path=line.Substring(5).Trim().Replace('/','\\');if(relative)path=System.IO.Path.Combine(roaming,"Mozilla","Firefox",path);ReadFirefoxPreferences(System.IO.Path.Combine(path,"prefs.js"),roots);}}}}catch(IOException){}catch(UnauthorizedAccessException){}
        return roots;
    }
    public static List<string> ReadChromiumDownloadSetting(string file){var roots=new List<string>();ReadChromiumPreferences(file,roots);return roots;}
    public static List<string> ReadFirefoxDownloadSetting(string file){var roots=new List<string>();ReadFirefoxPreferences(file,roots);return roots;}
    static void ReadChromiumPreferences(string file,List<string> roots){
        try{if(!File.Exists(file)||new FileInfo(file).Length>16L*1024*1024)return;var values=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(file));object section;
            if(values.TryGetValue("download",out section)){var download=section as Dictionary<string,object>;object directory;if(download!=null&&download.TryGetValue("default_directory",out directory))AddRoot(roots,Convert.ToString(directory));}
        }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
    }
    static void ReadFirefoxPreferences(string file,List<string> roots){
        try{if(!File.Exists(file)||new FileInfo(file).Length>4L*1024*1024)return;foreach(string line in File.ReadLines(file)){
            var match=Regex.Match(line,"^user_pref\\(\"browser\\.download\\.(?:dir|lastDir)\",\\s*(\"(?:[^\"\\\\]|\\\\.)*\")\\s*\\);");if(match.Success)AddRoot(roots,Store.Json.Deserialize<string>(match.Groups[1].Value));
        }}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}
    }
    public static List<FoundSource> Scan(Settings settings){
        return Scan(settings,CancellationToken.None,null);
    }
    public static List<FoundSource> Scan(Settings settings,CancellationToken cancellation,Action<DiscoveryProgress> progress){
        cancellation.ThrowIfCancellationRequested();
        var roots=NativeRoots(settings);foreach(string root in roots.ToArray())AddRoot(roots,System.IO.Path.Combine(root,"Telegram Desktop"));
        return ScanFolders(roots,cancellation,progress);
    }
    public static List<FoundSource> ScanFolders(IEnumerable<string> folders,CancellationToken cancellation,Action<DiscoveryProgress> progress){
        cancellation.ThrowIfCancellationRequested();
        var catalog=RecipeSupport.Catalog();var results=new List<FoundSource>();var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var roots=new List<string>();
        foreach(string root in folders)AddRoot(roots,root);
        // Share the work limit across overlapping roots; do not multiply it by
        // the number of configured folders. Signature checks ignore filenames.
        var budget=new ScanBudget{Cancellation=cancellation,Progress=progress};
        // Check files directly in every nearby/search folder before descending.
        foreach(string root in roots){budget.Check();Walk(root,0,visited,results,catalog,budget,true);}
        foreach(string root in roots){budget.Check();Walk(root,0,visited,results,catalog,budget);}
        budget.Report(null,results,true);
        return results.GroupBy(x=>x.Path+"|"+x.Kind+"|"+x.RecipeId,StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).OrderBy(x=>x.Kind).ThenBy(x=>x.Name).ToList();
    }
    public static List<FoundSource> ProbeZip(string file){
        return ProbeZip(file,CancellationToken.None);
    }
    // RAR5 is inspected through the same bundled extractor used for installation.
    // The probe extracts only the version-marker paths into private staging.
    public static List<FoundSource> ProbeRar(string file,CancellationToken cancellation){
        if(ArchiveFormat(file)!="rar"||new FileInfo(file).Length>2L*1024*1024*1024)return new List<FoundSource>();
        cancellation.ThrowIfCancellationRequested();
        try{return RarSupport.ProbeFingerprints(file,cancellation);}
        catch(IOException){return new List<FoundSource>();}
        catch(UnauthorizedAccessException){return new List<FoundSource>();}
    }
    static string ArchiveFingerprint(System.IO.Compression.ZipArchiveEntry entry,Dictionary<string,string> hashes,CancellationToken cancellation){
        cancellation.ThrowIfCancellationRequested();string digest;if(hashes.TryGetValue(entry.FullName,out digest))return digest;
        using(var sink=new DigestSink())using(var stream=entry.Open()){
            var buffer=new byte[65536];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){cancellation.ThrowIfCancellationRequested();sink.Write(buffer,0,count);}digest=sink.Finish();
        }hashes[entry.FullName]=digest;return digest;
    }
    public static List<FoundSource> ProbeZip(string file,CancellationToken cancellation){
        cancellation.ThrowIfCancellationRequested();
        var found=new List<FoundSource>();try{using(var zip=System.IO.Compression.ZipFile.OpenRead(file)){
            if(zip.GetEntry("index.json")!=null&&zip.GetEntry("index.json").Length<=4L*1024*1024){try{var index=UniversalSupport.PackIndex(zip);foreach(var version in index.Versions)found.Add(new FoundSource{Kind="Any-version pack",Path=file,RecipeId=version.Id,Name=version.Name+" · any starting version"});return found;}catch(IOException){}catch(ArgumentException){}}
            var legacy=zip.GetEntry("manifest.json");if(legacy!=null&&legacy.Length<64L*1024*1024){using(var reader=new StreamReader(legacy.Open())){var manifest=Store.Json.Deserialize<DeltaManifest>(reader.ReadToEnd());if(manifest.Format==1&&manifest.Files!=null&&!string.IsNullOrEmpty(manifest.Name)){var recipe=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Name.Equals(manifest.Name,StringComparison.OrdinalIgnoreCase));found.Add(new FoundSource{Kind="Delta pack",Path=file,RecipeId=recipe==null?(manifest.Name.StartsWith("Gorpg")?"gorpg-1-23":null):recipe.Id,Name=manifest.Name});return found;}}}
            if(zip.Entries.Count>100000)return found;
            var entries=zip.Entries.Where(x=>!x.FullName.EndsWith("/")).GroupBy(x=>x.FullName.Replace('\\','/').TrimStart('/'),StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.OrdinalIgnoreCase);
            var fingerprintHashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            if(entries.Count!=zip.Entries.Count(x=>!x.FullName.EndsWith("/"))||entries.Keys.Any(x=>x.Contains(":")||x.Split('/').Any(p=>p=="..")))return found;
            var roots=entries.Keys.Where(x=>x.EndsWith("package.json",StringComparison.OrdinalIgnoreCase)).Select(x=>x.Substring(0,x.Length-12)).Where(x=>entries.ContainsKey(x+"Game.exe")||entries.ContainsKey(x+"nw.exe")).ToArray();
            if(roots.Length==0){
                var content=entries.Keys.Where(x=>x.EndsWith("data/System.json",StringComparison.OrdinalIgnoreCase)).Select(x=>x.Substring(0,x.Length-16)).Where(x=>entries.ContainsKey(x+"js/plugins.js")).ToArray();if(content.Length!=1)return found;
                foreach(var recipe in RecipeSupport.Catalog().Recipes.Where(x=>x.Fingerprints!=null)){
                    bool same=true;foreach(var mark in recipe.Fingerprints){cancellation.ThrowIfCancellationRequested();System.IO.Compression.ZipArchiveEntry e;if(!entries.TryGetValue(content[0]+mark.Key,out e)||e.Length>64L*1024*1024){same=false;break;}if(ArchiveFingerprint(e,fingerprintHashes,cancellation)!=mark.Value){same=false;break;}}
                    if(same){found.Add(new FoundSource{Kind="Original archive",Path=file,RecipeId=recipe.Id,Name=recipe.Name+" · overlay structure and version hashes match"});break;}
                }return found;
            }
            if(roots.Length!=1)return found;
            string prefix=roots[0];var packageEntry=entries[prefix+"package.json"];if(packageEntry.Length>1048576)return found;string web;
            using(var executable=entries.ContainsKey(prefix+"Game.exe")?entries[prefix+"Game.exe"].Open():entries[prefix+"nw.exe"].Open()){if(executable.ReadByte()!='M'||executable.ReadByte()!='Z')return found;}
            using(var reader=new StreamReader(packageEntry.Open())){var package=Store.Json.Deserialize<Dictionary<string,object>>(reader.ReadToEnd());object main;if(package==null||!package.TryGetValue("main",out main))return found;string mainPath=Convert.ToString(main).Replace('\\','/');if(mainPath.StartsWith("/")||mainPath.Contains(":")||mainPath.Split('/').Any(x=>x=="..")||!entries.ContainsKey(prefix+mainPath))return found;int at=mainPath.LastIndexOf('/');web=prefix+(at>=0?mainPath.Substring(0,at+1):"");}
            var builds=UniversalSupport.ReadIndex().Versions.OrderByDescending(x=>x.Fingerprints.Count);foreach(var build in builds){bool same=true;foreach(var mark in build.Fingerprints){cancellation.ThrowIfCancellationRequested();System.IO.Compression.ZipArchiveEntry entry;if(!entries.TryGetValue(web+mark.Key,out entry)||entry.Length>64L*1024*1024){same=false;break;}if(ArchiveFingerprint(entry,fingerprintHashes,cancellation)!=mark.Value){same=false;break;}}if(same){found.Add(new FoundSource{Kind="Full game archive",Path=file,RecipeId=build.Id,Name=build.Name+" · version files match"});break;}}
        }}catch(IOException){}catch(System.IO.InvalidDataException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
        return found;
    }
    static void Walk(string root,int depth,HashSet<string> visited,List<FoundSource> found,RecipeCatalog catalog,ScanBudget budget,bool shallow=false){
        budget.Check();
        if(depth>8 || budget.Remaining<=0 || !Directory.Exists(root)||IsNativeProbe(root))return;
        string full=System.IO.Path.GetFullPath(root);
        bool first=visited.Add(full);if(!shallow&&!budget.Expanded.Add(full))return;
        if(first){budget.Remaining--;budget.Visited++;}budget.Report(full,found);
        try{
            if((new DirectoryInfo(full).Attributes&FileAttributes.ReparsePoint)!=0)return;
            bool gameFolder=File.Exists(System.IO.Path.Combine(full,"package.json")),contentFolder=File.Exists(System.IO.Path.Combine(full,"data","System.json"));
            if(first){
            if(File.Exists(System.IO.Path.Combine(full,"package.json")) && (File.Exists(System.IO.Path.Combine(full,"Game.exe"))||File.Exists(System.IO.Path.Combine(full,"nw.exe")))){
                gameFolder=true;try{
                string executable=File.Exists(System.IO.Path.Combine(full,"Game.exe"))?System.IO.Path.Combine(full,"Game.exe"):System.IO.Path.Combine(full,"nw.exe");
                bool validExe;using(var stream=File.OpenRead(executable))validExe=stream.ReadByte()=='M'&&stream.ReadByte()=='Z';if(!validExe)throw new IOException("Not a Windows game executable.");
                var profile=Store.Detect(full,System.IO.Path.GetFileName(full));var recipe=RecipeSupport.Identify(Store.WebRoot(full));bool clean=RecipeSupport.IsCleanBase(full);
                var system=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(System.IO.Path.Combine(Store.WebRoot(full),"data","System.json")));
                object titleValue;string title=system.TryGetValue("gameTitle",out titleValue)?Convert.ToString(titleValue):"";
                bool ch=IsCH(full);
                string identified=UniversalSupport.Identify(full),name=identified==null?null:UniversalSupport.ReadIndex().Versions.First(x=>x.Id==identified).Name;
                if(recipe!=null||clean||ch||identified!=null||title.IndexOf("Celesphonia",StringComparison.OrdinalIgnoreCase)>=0||title.Contains("苍蓝星"))found.Add(new FoundSource{Kind="Installed game",Path=full,RecipeId=identified??(ch?"ch-current":clean?"gorpg-1-23":recipe==null?null:recipe.Id),Name=name??(ch?"CH - detected installation":clean?"Gorpg 1.23 - clean base":recipe==null?profile.Name:recipe.Name)});
                }catch(IOException){}catch(ArgumentException){}
            }
            if(File.Exists(System.IO.Path.Combine(full,"data","System.json"))&&File.Exists(System.IO.Path.Combine(full,"js","plugins.js"))){
                contentFolder=true;var recipe=RecipeSupport.Identify(full);found.Add(new FoundSource{Kind="Extracted bundle",Path=full,RecipeId=recipe==null?null:recipe.Id,Name=recipe==null?System.IO.Path.GetFileName(full):recipe.Name});
            }
            foreach(string file in Directory.GetFiles(full)){
                budget.Check();
                string name=System.IO.Path.GetFileName(file),lower=name.ToLowerInvariant();
                if(((int)File.GetAttributes(file)&(4096|0x00400000))!=0){if(name.Contains("苍蓝星")||name.IndexOf("Celesphonia",StringComparison.OrdinalIgnoreCase)>=0||lower.EndsWith(".cgdelta.zip"))found.Add(new FoundSource{Kind="Cloud file",Path=file,Name=name,Problem="Not stored locally; download the file first"});continue;}
                if(lower.EndsWith(".crdownload")||lower.EndsWith(".part")||lower.EndsWith(".download"))continue;
                string format=ArchiveFormat(file);
                long length=new FileInfo(file).Length;
                var hint=NameHint(name,catalog);
                var sized=catalog.Recipes.Where(x=>x.ArchiveBytes>0&&x.ArchiveBytes==length).ToArray();
                string detection=hint!=null?"Filename hint":sized.Length==1?"Exact byte-size hint":"Archive structure";
                if(hint==null&&sized.Length==1)hint=sized[0];
                // Always verify creator ZIP contents, even if their names or
                // byte counts imitate another version. Delta packs are routed
                // through their separate manifest branch below.
                if(format=="zip"&&!lower.EndsWith(".cgdelta.zip")){
                    var probed=ProbeZip(file,budget.Cancellation);
                    if(probed.Count>0){foreach(var item in probed){item.Bytes=length;item.Detection="Archive structure / fingerprints";}found.AddRange(probed);continue;}
                    if(hint!=null||name.Contains("苍蓝星")||name.Contains("蒼藍星")||name.IndexOf("Celesphonia",StringComparison.OrdinalIgnoreCase)>=0)
                        found.Add(new FoundSource{Kind="Unverified archive",Path=file,Name=name,Bytes=length,Detection="ZIP signature; internal version fingerprints unknown"});
                    continue;
                }
                // A differently named RAR is identified from the embedded
                // version markers even if its total size mimics another
                // release. A filename hint with mismatched bytes is also
                // rechecked against contents before a version is displayed.
                if(format=="rar"){
                    var probed=ProbeRar(file,budget.Cancellation);
                    if(probed.Count>0){foreach(var item in probed)item.Bytes=length;found.AddRange(probed);continue;}
                    if(name.Contains("苍蓝星")||name.Contains("蒼藍星")||name.IndexOf("Celesphonia",StringComparison.OrdinalIgnoreCase)>=0)
                        found.Add(new FoundSource{Kind="Unverified archive",Path=file,Name=name,Bytes=length,Detection="RAR signature; internal version fingerprints unknown"});
                    continue;
                }
                if(lower.EndsWith(".cgdelta.zip")){
                    try{using(var zip=System.IO.Compression.ZipFile.OpenRead(file)){var entry=zip.GetEntry("manifest.json");if(entry==null)continue;using(var reader=new StreamReader(entry.Open())){
                        var manifest=Store.Json.Deserialize<DeltaManifest>(reader.ReadToEnd());if(manifest.Format!=1||manifest.Files==null||string.IsNullOrEmpty(manifest.Name))continue;
                        var recipe=catalog.Recipes.FirstOrDefault(x=>x.Name.Equals(manifest.Name,StringComparison.OrdinalIgnoreCase));string id=recipe==null?(manifest.Name.StartsWith("Gorpg")?"gorpg-1-23":null):recipe.Id;
                        found.Add(new FoundSource{Kind="Delta pack",Path=file,RecipeId=id,Name=manifest.Name});
                    }}}catch{}
                    continue;
                }
                bool volume=System.Text.RegularExpressions.Regex.IsMatch(lower,@"\.00[1-4]$");
                if(!(format!=null||volume||lower.EndsWith(".7删z")))continue;
                var match=hint;
                string recipeId=match==null?null:match.Id;
                if(name.Contains("1.23gorpg")&&volume)recipeId="gorpg-1-23";
                if(recipeId==null){
                    if(name.Contains("苍蓝星")||name.Contains("蒼藍星")||name.IndexOf("Celesphonia",StringComparison.OrdinalIgnoreCase)>=0)found.Add(new FoundSource{Kind="Unverified archive",Path=file,Name=name,Bytes=length,Detection="Archive signature; version unknown"});
                    continue;
                }
                string problem=null;
                string selected=file;
                if(volume){
                    selected=file.Substring(0,file.Length-3)+"001";
                    if(!lower.EndsWith(".001")&&File.Exists(selected))continue;
                    int count=recipeId=="gorpg-1-23"?2:recipeId=="three-in-one-3-3-4"?4:2;
                    var missing=new List<string>();string prefix=file.Substring(0,file.Length-3);
                    for(int i=1;i<=count;i++)if(!File.Exists(prefix+i.ToString("D3")))missing.Add("."+i.ToString("D3"));
                    if(missing.Count>0)problem="Missing volume(s): "+string.Join(", ",missing);
                }
                found.Add(new FoundSource{Kind="Original archive",Path=selected,RecipeId=recipeId,Name=match==null?"Gorpg 1.23 base volumes":match.Name,Problem=problem,Bytes=length,Detection=detection+"; verified before install"});
            }
            }
            budget.Report(full,found,shallow);if(shallow)return;
            foreach(string dir in Directory.GetDirectories(full).OrderBy(x=>System.IO.Path.GetFileName(x),StringComparer.OrdinalIgnoreCase)){
                string name=System.IO.Path.GetFileName(dir).ToLowerInvariant();
                if(new[]{"save","save backups","runtime","staging","tools","node_modules","cache",".git",".venv","venv","__pycache__",".cache"}.Contains(name))continue;
                if((gameFolder||contentFolder)&&new[]{"www","audio","data","fonts","img","js","movies","css","icon","locales","swiftshader"}.Contains(name))continue;
                Walk(dir,depth+1,visited,found,catalog,budget);
            }
        }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
    }
}
