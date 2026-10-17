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
    public static List<string> ReadFirefoxDownloadSetting(string file){var root