using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Management;

public class Profile {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Root { get; set; }
    public string Exe { get; set; }
    public string SavePath { get; set; }
    public string Note { get; set; }
    public bool RuntimeControls { get; set; }
    public override string ToString() { return Name; }
}
public class Settings {
    public List<Profile> Profiles { get; set; }
    public string SelectedId { get; set; }
    public string InstallRoot { get; set; }
    public string LauncherTheme { get; set; }
    public List<string> SearchRoots {get;set;}
}
public static class Store {
    public static readonly string Home = Path.GetDirectoryName(typeof(Store).Assembly.Location);
    public static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
    public static string Config { get { return Path.Combine(Home, "profiles.json"); } }
    public static Settings Read() {
        var settings=Json.Deserialize<Settings>(File.ReadAllText(Config, Encoding.UTF8));
        if(settings.Profiles==null)settings.Profiles=new List<Profile>();
        MapPaths(settings,false);
        if(string.IsNullOrWhiteSpace(settings.InstallRoot))settings.InstallRoot=Path.Combine(Home,"Game Profiles");
        SharedInstallationIndex.Sync(settings);
        return settings;
    }
    public static string PortablePath(string value,bool encode) {
        if(string.IsNullOrWhiteSpace(value))return value;
        const string marker="@launcher/";
        if(!encode)return value.StartsWith(marker,StringComparison.Ordinal)?Child(Home,value.Substring(marker.Length).Replace('/',Path.DirectorySeparatorChar)):value;
        string full=Path.GetFullPath(value),prefix=Path.GetFullPath(Home).TrimEnd('\\','/')+Path.DirectorySeparatorChar;
        return full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)?marker+full.Substring(prefix.Length).Replace('\\','/'):full;
    }
    static void MapPaths(Settings s,bool encode) {
        foreach(var p in s.Profiles){p.Root=PortablePath(p.Root,encode);p.SavePath=PortablePath(p.SavePath,encode);}
        s.InstallRoot=PortablePath(s.InstallRoot,encode);
        if(s.SearchRoots!=null)s.SearchRoots=s.SearchRoots.Select(x=>PortablePath(x,encode)).ToList();
    }
    public static void Write(Settings settings) {
        string temp = Config + ".new";
        var portable=Json.Deserialize<Settings>(Json.Serialize(settings));
        MapPaths(portable,true);
        File.WriteAllText(temp, Json.Serialize(portable), new UTF8Encoding(false));
        if (File.Exists(Config)) File.Replace(temp, Config, Config + ".previous"); else File.Move(temp, Config);
        SharedInstallationIndex.Remember(settings.Profiles);
    }
    public static string Q(string s) { return "\"" + s.Replace("\"", "") + "\""; }
    public static string Child(string root, string relative) {
        string prefix = Path.GetFullPath(root).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        string value = Path.GetFullPath(Path.Combine(root, relative));
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException("Path escapes the selected game folder: " + relative);
        return value;
    }
    // Short isolated extraction roots avoid legacy Win32 MAX_PATH when an
    // author RAR has long Chinese wrapper filenames and nested asset paths.
    public static string ArchiveStaging(string purpose) {
        if (!new[]{ "up", "verify", "layers", "legacy" }.Contains(purpose))
            throw new IOException("Unrecognized temporary game-extraction purpose.");
        return Path.Combine(Path.GetTempPath(),"Cph-"+purpose+"-"+Guid.NewGuid().ToString("N").Substring(0,12));
    }
    public static void CleanArchiveStaging(string target) {
        string path=Path.GetFullPath(target),temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\')+Path.DirectorySeparatorChar;
        string name=Path.GetFileName(path);
        if(!path.StartsWith(temp,StringComparison.OrdinalIgnoreCase)||
           !System.Text.RegularExpressions.Regex.IsMatch(name,@"^Cph-(up|verify|layers|legacy)-[a-f0-9]{12}$")||
           !Directory.Exists(path))return;
        // RAR archives can contain extended Unicode paths that managed
        // File.GetAttributes cannot traverse. Audit links before deleting;
        // never mirror/delete through an unchecked reparse point.
        ExtendedSafeCleanup.RemoveTreeSafely(path);
    }
    public static string WebRoot(string root) {
        var p = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(root, "package.json"), Encoding.UTF8));
        string main = Convert.ToString(p["main"]);
        string path = Child(root, main);
        if (!File.Exists(path)) throw new IOException("The game entry page is missing: " + path);
        return Path.GetDirectoryName(path);
    }
    public static Profile Detect(string root, string name) {
        root = Path.GetFullPath(root);
        string web = WebRoot(root);
        if (!File.Exists(Path.Combine(web, "data", "System.json"))) throw new IOException("Select a Celesphonia game folder containing package.json and its game data.");
        string exe = File.Exists(Path.Combine(root, "Game.exe")) ? "Game.exe" : "nw.exe";
        if (!File.Exists(Path.Combine(root, exe))) throw new IOException("Game.exe or nw.exe is missing.");
        string save = Path.Combine(web, "save");
        string plugins = Path.Combine(web, "js", "plugins.js");
        if (File.Exists(plugins)) {
            string txt = File.ReadAllText(plugins, Encoding.UTF8);
            int start = txt.IndexOf('['), end = txt.LastIndexOf(']');
            if (start >= 0 && end > start) {
                var list = Json.Deserialize<List<Dictionary<string, object>>>(txt.Substring(start, end - start + 1));
                foreach (var plugin in list) {
                    if (Convert.ToString(plugin["name"]) == "SavePathRelocation" && Convert.ToBoolean(plugin["status"])) {
                        var parameters = (Dictionary<string, object>)plugin["parameters"];
                        string basis = Convert.ToString(parameters["BaseAt"]) == "exe" ? root : web;
                        save = Child(basis, Convert.ToString(parameters["RelatedName"]).TrimEnd('/', '\\'));
                    }
                }
            }
        }
        return new Profile { Id = Guid.NewGuid().ToString("N"), Name = name, Root = root, Exe = exe, SavePath = save, Note = "Independent game files and saves" };
    }
    public static bool IsSave(string path) {
        string n = Path.GetFileName(path);
        return n.EndsWith(".rpgsave", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".rpgsave.bak", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".rmmzsave", StringComparison.OrdinalIgnoreCase) || n == "steam_autocloud.vdf";
    }
    public static string[] SaveFiles(string folder) { return Directory.Exists(folder) ? Directory.GetFiles(folder).Where(IsSave).ToArray() : new string[0]; }
    public static string Hash(string path) { using (var s = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant(); }
    public static string Backup(Profile p) {
        string folder = Path.Combine(Home, "Save Backups", p.Id, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
        Directory.CreateDirectory(folder);
        var hashes = new Dictionary<string, string>();
        foreach (string f in SaveFiles(p.SavePath)) { string to = Path.Combine(folder, Path.GetFileName(f)); File.Copy(f, to); hashes[Path.GetFileName(f)] = Hash(to); }
        File.WriteAllText(Path.Combine(folder, "manifest.json"), Json.Serialize(new { profile = p.Name, version = UniversalSupport.Identify(p.Root), originalSavePath = p.SavePath, created = DateTimeOffset.Now.ToString("o"), files = hashes }), Encoding.UTF8);
        return folder;
    }
    public static void ReplaceSaves(string source, Profile target, bool allowEmpty = false) {
        UpgradeSupport.ValidateSaveSource(source,target);
        string[] files = SaveFiles(source);
        if (files.Length == 0 && !allowEmpty) throw new IOException("The source contains no RPG Maker save files.");
        if (Path.GetFullPath(source).TrimEnd('\\').Equals(Path.GetFullPath(target.SavePath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) throw new IOException("Source and destination save folders are the same.");
        string recovery = Backup(target);
        Directory.CreateDirectory(target.SavePath);
        string stage = Path.Combine(Home, "Staging", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        foreach (string f in files) File.Copy(f, Path.Combine(stage, Path.GetFileName(f)));
        try {
            // Copy complete files into place; delete only recognized save files absent from this set.
            foreach (string f in SaveFiles(stage)) {
                string destination = Path.Combine(target.SavePath, Path.GetFileName(f));
                string temp = destination + ".launcher-new";
                File.Copy(f, temp, true);
                if (File.Exists(destination)) File.Replace(temp, destination, null); else File.Move(temp, destination);
            }
            var names = new HashSet<string>(files.Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);
            foreach (string f in SaveFiles(target.SavePath)) if (!names.Contains(Path.GetFileName(f))) File.Delete(f);
        } catch (Exception error) { throw new IOException("Save operation failed. Your previous saves are backed up at " + recovery + ". " + error.Message, error); }
        finally { foreach (string f in Directory.GetFiles(stage)) File.Delete(f); Directory.Delete(stage); }
    }
    public static void VerifyBackup(string folder) {
        string manifest = Path.Combine(folder, "manifest.json");
        if (!File.Exists(manifest)) throw new IOException("Select a timestamped backup folder containing manifest.json.");
        var data = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(manifest, Encoding.UTF8));
        var hashes = (Dictionary<string, object>)data["files"];
        foreach (var item in hashes) {
            string path = Child(folder, item.Key);
            if (!File.Exists(path) || Hash(path) != Convert.ToString(item.Value)) throw new IOException("Backup verification failed: " + item.Key);
        }
        if (hashes.Count != SaveFiles(folder).Length) throw new IOException("Backup contents differ from its manifest.");
    }
    public static bool Running(Settings settings) {
        using (var query = new ManagementObjectSearcher("SELECT ExecutablePath, CommandLine FROM Win32_Process WHERE Name = 'Game.exe' OR Name = 'nw.exe'")) {
            foreach (ManagementObject process in query.Get()) using (process) {
                if (Convert.ToString(process["CommandLine"]).IndexOf("--type=crashpad-handler", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                string path = Convert.ToString(process["ExecutablePath"]);
                if (settings.Profiles.Any(x => Path.GetFullPath(Path.Combine(x.Root, x.Exe)).Equals(path, StringComparison.OrdinalIgnoreCase) || Path.GetFullPath(Path.Combine(Home,"Control Sessions",x.Id,"app",x.Exe)).Equals(path,StringComparison.OrdinalIgnoreCase))) return true;
            }
        }
        return false;
    }
    public static Process Launch(Profile p) { return Launch(p,null); }
    public static Process Launch(Profile p,Action<string> progress) {
        Action<string> report=message=>{if(progress!=null)progress(message);};
        report("Checking game entry and runtime…");
        var detected=Detect(p.Root,p.Name);
        if(!File.Exists(Path.Combine(p.Root,p.Exe)))p.Exe=detected.Exe;
        if(!Path.GetFullPath(p.SavePath).Equals(Path.GetFullPath(detected.SavePath),StringComparison.OrdinalIgnoreCase))throw new IOException("This profile's save path no longer matches the installed game. Add the moved game folder again before launching.");
        using(var binary=File.OpenRead(Child(p.Root,p.Exe)))if(binary.ReadByte()!='M'||binary.ReadByte()!='Z')throw new IOException("The selected game executable is not a Windows program.");
        report("Checking translation and game scripts…");
        RecipeSupport.VerifyProfile(p,true);
        report("Backing up saves…");
        Backup(p);
        string runtime = Path.Combine(Home, "Runtime", p.Id);
        Directory.CreateDirectory(runtime);
        report("Starting "+p.Name+"…");
        var started=Process.Start(new ProcessStartInfo(Child(p.Root,p.Exe), "--user-data-dir=" + Q(runtime)+" "+Q(p.Root)) { WorkingDirectory = p.Root, UseShellExecute = false });
        if(started==null)throw new IOException("The game runtime did not create a process.");
        if(started.WaitForExit(1500))throw new IOException("The game runtime exited during startup (code "+started.ExitCode+"). Check that the complete game runtime was extracted; launch details are in launch-status.json.");
        return started;
    }
    public static void CopyTree(string source, string target, bool gameCopy, string gameRoot = null) {
        if(gameCopy&&gameRoot==null&&File.Exists(Path.Combine(source,"package.json")))gameRoot=source;
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source)) {
            string n = Path.GetFileName(file), ext = Path.GetExtension(n).ToLowerInvariant();
            if (gameCopy && (IsSave(file) || ext == ".7z" || ext == ".rar" || ext == ".zip" || ext == ".001" || ext == ".002")) continue;
            string destination = Path.Combine(target, n);
            File.Copy(gameCopy&&gameRoot!=null?EnglishSupport.OriginalFile(gameRoot,file):file, destination, true);
        }
        foreach (string dir in Directory.GetDirectories(source)) {
            var info = new DirectoryInfo(dir);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked folders are not supported: " + dir);
            string n = info.Name.ToLowerInvariant();
            if (gameCopy && (n == "save" || n == "output" || n == "decrypted" || n == "node_modules" || n == "runtime" || n == "cache" || n=="celesphonia-english")) continue;
            if (gameCopy && Directory.Exists(Path.Combine(dir, "www"))) continue;
            CopyTree(dir, Path.Combine(target, info.Name), gameCopy,gameRoot);
        }
    }
    public static long TreeSize(string path) {
        long n = Directory.GetFiles(path).Sum(x => new FileInfo(x).Length);
        foreach (string dir in Directory.GetDirectories(path)) {
            var d = new DirectoryInfo(dir);
            if ((d.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked folders are not supported.");
            if (new[] { "save", "output", "decrypted", "node_modules", "runtime", "cache" }.Contains(d.Name.ToLowerInvariant())) continue;
            n += TreeSize(dir);
        }
        return n;
    }
    public static string FindOverlay(string folder) {
        if (File.Exists(Path.Combine(folder, "data", "System.json")) && File.Exists(Path.Combine(folder, "js", "plugins.js"))) return folder;
        var matches = Directory.GetFiles(folder, "System.json", SearchOption.AllDirectories).Where(x => new DirectoryInfo(Path.GetDirectoryName(x)).Name.Equals("data", StringComparison.OrdinalIgnoreCase)).Select(x => Directory.GetParent(Path.GetDirectoryName(x)).FullName).Where(x => File.Exists(Path.Combine(x, "js", "plugins.js"))).ToArray();
        if (matches.Length != 1