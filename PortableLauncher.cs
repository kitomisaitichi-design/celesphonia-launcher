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
    // User-removed entries must not be resurrected by shared-index discovery.
    // An explicit Add installed game action can restore an ignored location.
    public List<string> SuppressedRoots {get;set;}
}
public static class Store {
    public static readonly string Home = Path.GetDirectoryName(typeof(Store).Assembly.Location);
    public static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
    public static string Config { get { return Path.Combine(Home, "profiles.json"); } }
    public static Settings Read() {
        var settings=Json.Deserialize<Settings>(File.ReadAllText(Config, Encoding.UTF8));
        if(settings.Profiles==null)settings.Profiles=new List<Profile>();
        if(settings.SuppressedRoots==null)settings.SuppressedRoots=new List<string>();
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
        if(s.SuppressedRoots!=null)s.SuppressedRoots=s.SuppressedRoots.Select(x=>PortablePath(x,encode)).ToList();
    }
    static string CanonicalRoot(string root){return Path.GetFullPath(root).TrimEnd('\\','/');}
    public static bool IsSuppressed(Settings settings,string root){
        if(settings==null||settings.SuppressedRoots==null||string.IsNullOrWhiteSpace(root))return false;
        string exact=CanonicalRoot(root);
        return settings.SuppressedRoots.Any(x=>!string.IsNullOrWhiteSpace(x)&&
            string.Equals(CanonicalRoot(x),exact,StringComparison.OrdinalIgnoreCase));
    }
    public static void SuppressRoot(Settings settings,string root){
        if(settings.SuppressedRoots==null)settings.SuppressedRoots=new List<string>();
        if(!IsSuppressed(settings,root))settings.SuppressedRoots.Add(CanonicalRoot(root));
    }
    public static void UnsuppressRoot(Settings settings,string root){
        if(settings.SuppressedRoots==null)return;
        string exact=CanonicalRoot(root);
        settings.SuppressedRoots.RemoveAll(x=>!string.IsNullOrWhiteSpace(x)&&
            string.Equals(CanonicalRoot(x),exact,StringComparison.OrdinalIgnoreCase));
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
        if (matches.Length != 1) throw new IOException("The mod folder must contain exactly one data/System.json and js/plugins.js pair. Choose that mod's folder.");
        return matches[0];
    }
    public static string CommandArchive(string exe, string args) {
        using (var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })) {
            Task<string> err = p.StandardError.ReadToEndAsync();
            string output = p.StandardOutput.ReadToEnd(); p.WaitForExit();
            if (p.ExitCode != 0) throw new IOException("Archive extraction failed: " + err.Result + output);
            return output;
        }
    }
    static string Command(string exe, string args){return CommandArchive(exe,args);}
    public static string Extract(string archive, string target, bool allowPartial = false, bool raw = false) {
        Directory.CreateDirectory(target);
        string archiveFormat=FileDiscovery.ArchiveFormat(archive);
        if(archiveFormat=="rar"||archiveFormat=="7z"||archive.EndsWith(".001",StringComparison.OrdinalIgnoreCase))
            return RarSupport.Extract(archive,target,allowPartial,raw);
        if (archiveFormat=="zip") {
            using (var zip = System.IO.Compression.ZipFile.OpenRead(archive)) {
                foreach (var entry in zip.Entries) {
                    string name = entry.FullName.TrimEnd('/', '\\');
                    if (name.Length > 0) Child(target, name.Replace('/', '\\'));
                    if (((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000) throw new IOException("Linked archive entries are not supported.");
                }
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(archive, target);
            return raw ? target : allowPartial ? RecipeSupport.ContentRoot(target) : FindOverlay(target);
        }
        string tool = Path.Combine(Home, "Tools", "7z.exe");
        bool rar = archiveFormat=="rar";
        if (!rar && !File.Exists(tool)) throw new IOException("Extract this 7z archive with your own extractor, then use Install overlay folder. Portable delta packs and ZIP overlays need no external extractor.");
        string listing = rar ? Command("tar.exe", "-tf " + Q(archive)) : Command(tool, "l -slt -p- " + Q(archive));
        if (rar) {
            string verbose = Command("tar.exe", "-tvf " + Q(archive));
            if (verbose.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x.StartsWith("l") || x.StartsWith("h"))) throw new IOException("Archives with symbolic or hard links are not supported.");
        } else if (listing.Contains("Symbolic Link =") || listing.Contains("Hard Link =")) throw new IOException("Archives with linked paths are not supported.");
        string[] entries;
        if (rar) entries = listing.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        else {
            int start = listing.IndexOf("----------", StringComparison.Ordinal);
            if (start < 0) throw new IOException("Unsupported archive format.");
            entries = listing.Substring(start).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.StartsWith("Path = ")).Select(x => x.Substring(7)).ToArray();
        }
        foreach (string entry in entries) {
            string cleaned = entry.TrimEnd('/', '\\').Replace('/', '\\');
            if (cleaned.Length == 0 || cleaned == ".") continue;
            if (Path.IsPathRooted(cleaned) || cleaned.Contains(":") || cleaned.Split('\\').Any(x => x == "..")) throw new IOException("Archive contains an unsafe path: " + entry);
            Child(target, cleaned);
        }
        if (rar) Command("tar.exe", "-xf " + Q(archive) + " -C " + Q(target));
        else Command(tool, "x -y -p- -bso0 -bsp0 -o" + Q(target) + " " + Q(archive));
        // Do not follow symbolic links out of the extracted folder.
        CheckLinks(target);
        return raw ? target : allowPartial ? RecipeSupport.ContentRoot(target) : FindOverlay(target);
    }
    public static void CheckLinks(string folder) {
        foreach (string entry in Directory.GetFileSystemEntries(folder)) {
            FileAttributes attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Archive contains a linked path.");
            if ((attributes & FileAttributes.Directory) != 0) CheckLinks(entry);
        }
    }
    public static Profile Install(Settings settings, Profile basis, string mod, bool archive, string name) {
        string id = Guid.NewGuid().ToString("N");
        string staging = Path.Combine(Home, "Staging", id);
        string overlay = archive ? Extract(mod, staging) : FindOverlay(mod);
        long required = TreeSize(basis.Root) + TreeSize(overlay) + 1024L * 1024 * 1024;
        Directory.CreateDirectory(settings.InstallRoot);
        if (new DriveInfo(Path.GetPathRoot(settings.InstallRoot)).AvailableFreeSpace < required) throw new IOException("Insufficient disk space. This profile needs about " + (required / (1024.0 * 1024 * 1024)).ToString("0.0") + " GB including working space.");
        string safeName = new string(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        if (safeName.Length == 0) safeName = "Mod";
        string destination = Path.Combine(settings.InstallRoot, safeName + "_" + id.Substring(0, 6));
        CopyTree(basis.Root, destination, true);
        string web = WebRoot(destination);
        // Only overlay game content, preserving the chosen base's executable and entry layout.
        foreach (string dir in new[] { "audio", "data", "fonts", "img", "js", "movies", "css", "icon" }) {
            string src = Path.Combine(overlay, dir);
            if (Directory.Exists(src)) CopyTree(src, Path.Combine(web, dir), false);
        }
        Profile p = Detect(destination, name);
        Directory.CreateDirectory(p.SavePath);
        p.Note = "Overlay on " + basis.Name + "; starts with its own empty save set";
        if (archive) {
            string expected = Child(Home, Path.Combine("Staging", id));
            if (Path.GetFullPath(staging).Equals(expected, StringComparison.OrdinalIgnoreCase)) Directory.Delete(expected, true);
        }
        return p;
    }
    public static void SelfTest() {
        string root = Path.Combine(Home, "Staging", "self-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var a = new Profile { Id = "test-" + Guid.NewGuid().ToString("N"), Name = "A", SavePath = Path.Combine(root, "A") };
        var b = new Profile { Id = "test-" + Guid.NewGuid().ToString("N"), Name = "B", SavePath = Path.Combine(root, "B") };
        Directory.CreateDirectory(a.SavePath); Directory.CreateDirectory(b.SavePath);
        File.WriteAllText(Path.Combine(a.SavePath, "file1.rpgsave"), "A");
        File.WriteAllText(Path.Combine(b.SavePath, "file2.rpgsave"), "B");
        File.WriteAllText(Path.Combine(b.SavePath, "keep.txt"), "keep");
        string backup = Backup(b); VerifyBackup(backup); ReplaceSaves(a.SavePath, b);
        if (File.ReadAllText(Path.Combine(b.SavePath, "file1.rpgsave")) != "A" || File.Exists(Path.Combine(b.SavePath, "file2.rpgsave")) || !File.Exists(Path.Combine(b.SavePath, "keep.txt"))) throw new Exception("Save copy test failed.");
        ReplaceSaves(backup, b);
        if (File.ReadAllText(Path.Combine(b.SavePath, "file2.rpgsave")) != "B" || File.Exists(Path.Combine(b.SavePath, "file1.rpgsave"))) throw new Exception("Save restore test failed.");
        File.WriteAllText(Path.Combine(backup, "file2.rpgsave"), "tampered");
        bool rejected = false; try { VerifyBackup(backup); } catch (IOException) { rejected = true; }
        if (!rejected) throw new Exception("Backup integrity test failed.");
        rejected = false; try { Child(root, "..\\escape"); } catch (IOException) { rejected = true; }
        if (!rejected) throw new Exception("Path boundary test failed.");
        string empty = Backup(new Profile { Id = a.Id, Name = "Empty", SavePath = Path.Combine(root, "Empty") });
        VerifyBackup(empty); ReplaceSaves(empty, b, true);
        if (SaveFiles(b.SavePath).Length != 0 || !File.Exists(Path.Combine(b.SavePath, "keep.txt"))) throw new Exception("Empty backup restore test failed.");
        foreach (var p in Read().Profiles) {
            Profile detected = Detect(p.Root, p.Name);
            if (!detected.SavePath.Equals(p.SavePath, StringComparison.OrdinalIgnoreCase)) throw new Exception("Save path mismatch for " + p.Name);
        }
        foreach (var p in new[] { a, b }) Directory.Delete(Child(Home, Path.Combine("Save Backups", p.Id)), true);
        Directory.Delete(Child(Home, Path.Combine("Staging", Path.GetFileName(root))), true);
        File.WriteAllText(Path.Combine(Home, "self-test-result.txt"), "PASS: isolated save replacement, rollback restore, empty-backup restore, unrelated-file preservation, backup hashes, path boundaries, and installed profile/save-path validation.\r\n", Encoding.UTF8);
    }
}
public class Launcher : Form {
    Settings settings;
    ListBox profiles = new ListBox();
    Label detail = new Label(), status = new Label();
    Button play, open, backup, restore, copy, add, overlayFolder, overlayArchive;
    bool busy;
    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    Profile Selected { get { return profiles.SelectedItem as Profile; } }
    public Launcher() {
        settings = Store.Read();
        Text = "Celesphonia Mod Launcher"; ClientSize = new Size(880, 575); MinimumSize = new Size(896, 614); StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(22, 26, 36); ForeColor = Color.FromArgb(235, 238, 245);
        Controls.Add(new Label { Text = "CELESPHONIA", Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true, Location = new Point(24, 19) });
        Controls.Add(new Label { Text = "Choose a mod. Its own saves return every time you play.", AutoSize = true, Location = new Point(28, 66), ForeColor = Color.FromArgb(166, 179, 204) });
        profiles.Location = new Point(28, 106); profiles.Size = new Size(340, 276); profiles.IntegralHeight = false; profiles.Font = new Font("Segoe UI", 12); profiles.ItemHeight = 29;
        profiles.BackColor = Color.FromArgb(32, 38, 52); profiles.ForeColor = ForeColor; profiles.BorderStyle = BorderStyle.FixedSingle; Controls.Add(profiles);
        detail.Location = new Point(394, 110); detail.Size = new Size(454, 176); Controls.Add(detail);
        play = Make("Switch & Play", 394, 302, 270, delegate { LaunchSelected(); }); play.BackColor = Color.FromArgb(87, 75, 181); play.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        open = Make("Open saves", 676, 302, 172, delegate { Guard(delegate { Directory.CreateDirectory(Selected.SavePath); Process.Start("explorer.exe", Store.Q(Selected.SavePath)); }); });
        backup = Make("Back up saves", 394, 350, 145, delegate { Guard(delegate { status.Text = "Backup saved: " + Store.Backup(Selected); }); });
        restore = Make("Restore backup", 549, 350, 145, delegate { Restore(); });
        copy = Make("Copy saves from...", 704, 350, 144, delegate { CopySaves(); });
        Controls.Add(new Label { Text = "ADD ANOTHER MOD", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(28, 420), ForeColor = Color.FromArgb(166, 179, 204) });
        add = Make("Add game folder", 28, 450, 240, delegate { AddProfile(); });
        overlayFolder = Make("Install overlay folder", 288, 450, 270, delegate { Overlay(false); });
        overlayArchive = Make("Install overlay archive", 578, 450, 270, delegate { Overlay(true); });
        status.Location = new Point(28, 516); status.Size = new Size(820, 45); status.ForeColor = Color.FromArgb(166, 179, 204); Controls.Add(status);
        profiles.SelectedIndexChanged += delegate { UpdateDetails(); };
        DeltaSupport.Attach(this, settings, delegate(Profile p) { settings.Profiles.Add(p); settings.SelectedId = p.Id; Store.Write(settings); RefreshProfiles(); }, delegate(bool installing) { busy = installing; UpdateButtons(); });
        RecipeSupport.Attach(this, settings, delegate(Profile p) { settings.Profiles.Add(p); settings.SelectedId = p.Id; Store.Write(settings); RefreshProfiles(); }, delegate(bool installing) { busy = installing; UpdateButtons(); }); RefreshProfiles(); timer.Interval = 2500; timer.Tick += delegate { UpdateButtons(); }; timer.Start();
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { e.Cancel = true; MessageBox.Show(this, "Wait for the current file operation to finish.", Text); } };
    }
    Button Make(string text, int x, int y, int width, EventHandler click) {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 40), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(43, 51, 71), ForeColor = ForeColor };
        b.FlatAppearance.BorderSize = 0; b.Click += click; Controls.Add(b); return b;
    }
    void RefreshProfiles() {
        profiles.Items.Clear(); foreach (var p in settings.Profiles) profiles.Items.Add(p);
        var chosen = settings.Profiles.FirstOrDefault(x => x.Id == settings.SelectedId);
        profiles.SelectedItem = chosen ?? settings.Profiles.FirstOrDefault(); UpdateDetails();
    }
    void UpdateDetails() {
        var p = Selected; if (p == null) return;
        settings.SelectedId = p.Id;
        int slots = Store.SaveFiles(p.SavePath).Count(x => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(x), "^file[0-9]+\\.rpgsave$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        detail.Text = p.Name + "\r\n\r\n" + slots + " save slots\r\n" + p.Note + "\r\n\r\nSaves: " + p.SavePath;
        UpdateButtons();
    }
    void UpdateButtons() {
        bool running = Store.Running(settings), selected = Selected != null;
        foreach (var b in new[] { play, open, backup, restore, copy, add, overlayFolder, overlayArchive }) if (b != null) b.Enabled = !busy && selected;
        if (running && !busy) { play.Enabled = false; backup.Enabled = false; restore.Enabled = false; copy.Enabled = false; overlayFolder.Enabled = false; overlayArchive.Enabled = false; status.Text = "A game is running. Save and close it before switching mods or changing saves."; }
        else if (!busy && status.Text.StartsWith("A game is running.")) status.Text = "Ready. Each launch backs up the selected profile's saves.";
        else if (status.Text.Length == 0) status.Text = "Ready. Each launch backs up the selected profile's saves.";
    }
    bool CheckClosed() { if (!Store.Running(settings)) return true; MessageBox.Show(this, "Save your progress and close the game before switching mods or changing saves.", Text); return false; }
    void Guard(Action action) { try { action(); UpdateDetails(); } catch (Exception e) { MessageBox.Show(this, e.Message, "Celesphonia Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
    void LaunchSelected() { Guard(delegate { if (!CheckClosed()) return; Store.Write(settings); Store.Launch(Selected); status.Text = "Starting " + Selected.Name + " with its own save set..."; }); }
    string Folder(string description, string initial) { using (var d = new FolderBrowserDialog { Description = description, SelectedPath = initial, ShowNewFolderButton = false }) return d.ShowDialog(this) == DialogResult.OK ? d.SelectedPath : null; }
    string NamePrompt(string initial) {
        using (var f = new Form { Text = "Name this mod profile", ClientSize = new Size(440, 115), StartPosition = FormStartPosition.CenterParent, Font = Font, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false }) {
            var t = new TextBox { Text = initial, Location = new Point(15, 15), Width = 410 };
            var ok = new Button { Text = "Add profile", DialogResult = DialogResult.OK, Location = new Point(310, 60), Width = 115 };
            f.Controls.Add(t); f.Controls.Add(ok); f.AcceptButton = ok;
            return f.ShowDialog(this) == DialogResult.OK && t.Text.Trim().Length > 0 ? t.Text.Trim() : null;
        }
    }
    Profile ChooseSource(string title, IEnumerable<Profile> options) {
        using (var f = new Form { Text = title, ClientSize = new Size(540, 130), StartPosition = FormStartPosition.CenterParent, Font = Font, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false }) {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(15, 20), Width = 510 };
            foreach (var p in options) c.Items.Add(p); if (c.Items.Count > 0) c.SelectedIndex = 0;
            var ok = new Button { Text = "Select", DialogResult = DialogResult.OK, Location = new Point(410, 75), Width = 115 };
            f.Controls.Add(c); f.Controls.Add(ok); f.AcceptButton = ok;
            return f.ShowDialog(this) == DialogResult.OK ? c.SelectedItem as Profile : null;
        }
    }
    void AddProfile() { Guard(delegate {
        string root = Folder("Choose the installed game folder containing Game.exe or nw.exe and package.json.", settings.InstallRoot); if (root == null) return;
        if (settings.Profiles.Any(x => Path.GetFullPath(x.Root).Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))) throw new IOException("This game folder already has a profile.");
        string name = NamePrompt(new DirectoryInfo(root).Name); if (name == null) return;
        var p = Store.Detect(root, name); settings.Profiles.Add(p); settings.SelectedId = p.Id; Store.Write(settings); RefreshProfiles();
    }); }
    void Restore() { Guard(delegate {
        if (!CheckClosed()) return;
        string root = Path.Combine(Store.Home, "Save Backups", Selected.Id); Directory.CreateDirectory(root);
        string source = Folder("Choose the timestamped backup folder to restore.", root); if (source == null) return;
        Store.VerifyBackup(source);
        if (MessageBox.Show(this, "Restore this backup to " + Selected.Name + "? Current saves will be backed up first.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        Store.ReplaceSaves(source, Selected, true); status.Text = "Backup restored. Your previous saves were backed up first.";
    }); }
    void CopySaves() { Guard(delegate {
        if (!CheckClosed()) return;
        var source = ChooseSource("Copy saves from which profile?", settings.Profiles.Where(x => x.Id != Selected.Id)); if (source == null) return;
        if (MessageBox.Show(this, "Replace " + Selected.Name + " saves with saves from " + source.Name + "?\r\n\r\n" + RecipeSupport.SaveWarning(source,Selected), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        Store.ReplaceSaves(source.SavePath, Selected); status.Text = "Saves copied. Original source saves and destination backup are preserved.";
    }); }
    async void Overlay(bool archive) {
        if (!CheckClosed()) return;
        string mod = null;
        if (archive) { using (var d = new OpenFileDialog { Title = "Select the mod archive (choose .001 for split archives)", Filter = "Mod archives|*.zip;*.7z;*.rar;*.001;*.7删z|All files|*.*", InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") }) if (d.ShowDialog(this) == DialogResult.OK) mod = d.FileName; }
        else mod = Folder("Choose an overlay folder containing audio, data, img, and js.", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        if (mod == null) return;
        var candidates = settings.Profiles.Where(x => RecipeSupport.IsCleanBase(x.Root));
        if (!candidates.Any()) { MessageBox.Show(this,"Register clean Gorpg 1.23 or install its delta pack first. Overlays require the verified clean base."); return; }
        Profile basis = ChooseSource("Choose the clean Gorpg 1.23 base for this overlay", candidates); if (basis == null) return;
        string name = NamePrompt(Path.GetFileNameWithoutExtension(mod)); if (name == null) return;
        busy = true; status.Text = "Installing " + name + "... The game files are being copied; this can take several minutes."; UpdateButtons();
        try { Profile p = await Task.Run(() => RecipeSupport.Install(basis.Root, settings.InstallRoot, name, new List<LayerInput> { new LayerInput { Source = mod } })); settings.Profiles.Add(p); settings.SelectedId = p.Id; Store.Write(settings); RefreshProfiles(); status.Text = "Installed " + p.Name + ". It has its own empty save set."; }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Installation failed", MessageBoxButtons.OK, MessageBoxIcon.Error); status.Text = "Installation failed. Existing profiles are unchanged; unfinished files remain for inspection."; }
        finally { busy = false; UpdateButtons(); }
    }
    [STAThread] public static int Main(string[] args) {
        try {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Contains("--self-test")) { Store.SelfTest(); return 0; }
            if (args.Contains("--render-preview")) { using (var form = new Launcher()) { form.Show(); Application.DoEvents(); using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size)); bitmap.Save(Path.Combine(Store.Home, "launcher-preview.png")); } } return 0; }
            using (var mutex = new System.Threading.Mutex(false, "Local\\CelesphoniaModLauncher")) {
                if (!mutex.WaitOne(0)) { MessageBox.Show("Celesphonia Mod Launcher is already open."); return 0; }
                try { Application.Run(new Launcher()); } finally { mutex.ReleaseMutex(); }
            }
            return 0;
        } catch (Exception e) { File.WriteAllText(Path.Combine(Store.Home, "launcher-error.txt"), e.ToString(), Encoding.UTF8); MessageBox.Show(e.Message, "Celesphonia Launcher"); return 1; }
    }
}
