using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public enum RemovalMode { IndexOnly, InstalledGame }

public sealed class RemovalFile {
    public string Path { get; internal set; }
    public long Size { get; internal set; }
    public string Hash { get; internal set; }
}

// Plans are previews. Execute rebuilds the plan and checks every hash again.
public sealed class RemovalPlan {
    public RemovalMode Mode { get; internal set; }
    public string Root { get; internal set; }
    public string ProfileId { get; internal set; }
    public bool CanDeleteFiles { get; internal set; }
    public string Reason { get; internal set; }
    public string Message { get; internal set; }
    public int FileCount { get { return Files.Count; } }
    public long FileBytes { get { return Files.Sum(x=>x.Size); } }
    public long Bytes { get { return FileBytes; } }
    public int RetainedCount { get; internal set; }
    public long RetainedBytes { get; internal set; }
    public int PreservedArchives { get; internal set; }
    public int PreservedSaves { get; internal set; }
    public int PreservedBackups { get; internal set; }
    public List<string> RetainedSample { get; internal set; }
    public List<RemovalFile> Files { get; internal set; }
    internal Settings Settings;
    internal Profile Profile;
    internal IEnumerable<MixedNativeFile> FixtureReference;
}

public sealed class RemovalResult {
    public string Root { get; internal set; }
    public RemovalMode Mode { get; internal set; }
    public int DeletedFiles { get; internal set; }
    public long DeletedBytes { get; internal set; }
    public int RetainedFiles { get; internal set; }
    public string Message { get; internal set; }
}

public static class InstallationRemoval {
    const int MaxEntries = 250000;
    static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;
    static readonly string[] MarkerNames = { "launcher-build.json", "launcher-reconstruction.json" };
    static readonly string[] References = { "gorpg-1-23", "ch-current", "maple-1-7-2", "three-in-one-3-3-4", "three-in-one-3-3-5-v1-053", "three-in-one-3-3-5-v1-06", "three-in-one-3-3-5-v1-061" };

    static string Full(string path) {
        if(string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))throw new IOException("A registered absolute game path is required.");
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
    }
    static bool Same(string a,string b){return Paths.Equals(Full(a),Full(b));}
    static bool Below(string path,string parent) {
        string prefix=Full(parent).TrimEnd('\\','/')+Path.DirectorySeparatorChar;
        return Full(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase);
    }
    static bool Within(string path,string folder){return Same(path,folder)||Below(path,folder);}
    static void NoLink(string path) {
        if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
            throw new IOException("Linked/junction/reparse path refused: "+path);
    }
    static void CheckParents(string root) {
        for(string current=Full(root); !string.IsNullOrEmpty(current);current=Path.GetDirectoryName(current)) {
            if(File.Exists(current)||Directory.Exists(current))NoLink(current);
            string parent=Path.GetDirectoryName(current);
            if(string.IsNullOrEmpty(parent)||Paths.Equals(current,parent))break;
        }
    }
    static bool Sha(string hash){return hash!=null && hash.Length==64 && hash.All(c=>Uri.IsHexDigit(c));}
    static bool SafeRelative(string relative) {
        if(string.IsNullOrWhiteSpace(relative)||relative.Length>1024||Path.IsPathRooted(relative)
           ||relative.IndexOfAny(new[]{':','\0','\r','\n','*','?','|','<','>','"'})>=0)return false;
        return relative.Replace('\\','/').Split('/').All(x=>x.Length>0&&x!="."&&x!=".."&&!x.EndsWith(".")&&!x.EndsWith(" "));
    }
    static string Relative(string root,string file) {
        if(!Below(file,root))throw new IOException("File escaped the game root: "+file);
        return Full(file).Substring(Full(root).Length+1).Replace('\\','/');
    }
    static bool Archive(string path) {
        string name=Path.GetFileName(path),ext=Path.GetExtension(name).ToLowerInvariant();
        return ext==".zip"||ext==".rar"||ext==".7z"||ext==".001"||ext==".002"||ext==".003"||ext==".004"
            ||(ext.Length==4&&ext[0]=='.'&&ext.Skip(1).All(char.IsDigit))
            ||System.Text.RegularExpressions.Regex.IsMatch(name,@"\.r\d\d$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
    static bool SaveOrBackup(string file, RemovalPlan plan, out bool backup) {
        backup=false;
        if(!string.IsNullOrWhiteSpace(plan.Profile.SavePath)&&Within(file,plan.Profile.SavePath))return true;
        if(plan.Settings.Profiles!=null)foreach(var p in plan.Settings.Profiles)
            if(p!=null&&!string.IsNullOrWhiteSpace(p.SavePath)&&Within(file,p.SavePath))return true;
        string[] segments=Relative(plan.Root,file).Split('/');
        if(segments.Any(x=>x.Equals("backup",StringComparison.OrdinalIgnoreCase)
            ||x.Equals("backups",StringComparison.OrdinalIgnoreCase)
            ||x.Equals("save backups",StringComparison.OrdinalIgnoreCase))) { backup=true;return true; }
        if(segments.Any(x=>x.Equals("save",StringComparison.OrdinalIgnoreCase)
            ||x.Equals("saves",StringComparison.OrdinalIgnoreCase)))return true;
        if(Store.IsSave(file))return true;
        return false;
    }
    static bool Marker(string root) {
        try {
            string built=Path.Combine(root,"launcher-build.json");
            if(File.Exists(built)) {
                NoLink(built);
                if(new FileInfo(built).Length>8*1024*1024)return false;
                var state=Store.Json.Deserialize<LayerState>(File.ReadAllText(built,Encoding.UTF8));
                if(state!=null&&state.Layers!=null&&state.BaseFiles!=null&&state.BaseFiles.Count>0
                    &&state.StorageMode!=null&&state.StorageMode.IndexOf("independent",StringComparison.OrdinalIgnoreCase)>=0)
                    return true;
            }
            string reconstructed=Path.Combine(root,"launcher-reconstruction.json");
            if(File.Exists(reconstructed)) {
                NoLink(reconstructed);
                if(new FileInfo(reconstructed).Length>8*1024*1024)return false;
                var state=Store.Json.Deserialize<ReconstructionState>(File.ReadAllText(reconstructed,Encoding.UTF8));
                if(state!=null&&state.format==2&&state.files!=null&&state.files.Count>0)return true;
            }
        }catch(IOException){throw;}catch(UnauthorizedAccessException){throw;}catch{ /* invalid marker cannot confer ownership */ }
        return false;
    }
    static bool Owned(RemovalPlan plan) {
        string root=plan.Root,home=Full(Store.Home),install=plan.Settings.InstallRoot;
        if(Same(root,home)||Below(home,root)||Same(root,Store.Config))return false;
        if(Within(root,Path.Combine(home,"Runtime"))||Within(root,Path.Combine(home,"Save Backups"))
            ||Within(root,Path.Combine(home,"Control Sessions"))||Within(root,Path.Combine(home,"Staging")))return false;
        // The managed location may live underneath Store.Home, but must not BE the launcher folder.
        bool managed=!string.IsNullOrWhiteSpace(install)&&Path.IsPathRooted(install)
            &&!Same(install,home)&&!Below(home,install)&&Below(root,install);
        // The install folder is the only place inside the launcher home where
        // an owned game may live; a stray marker in launcher data proves nothing.
        if(Below(root,home)&&!managed)return false;
        return managed||Marker(root);
    }
    static Dictionary<string,HashSet<string>> Trusted(RemovalPlan plan) {
        var expected=new Dictionary<string,HashSet<string>>(Paths);
        Action<string,string> add=(path,hash)=>{
            if(!SafeRelative(path)||!Sha(hash))return;
            HashSet<string> hashes;
            path=path.Replace('\\','/');
            if(!expected.TryGetValue(path,out hashes)){hashes=new HashSet<string>(Paths);expected[path]=hashes;}
            hashes.Add(hash);
        };
        if(plan.FixtureReference!=null) {
            foreach(var f in plan.FixtureReference)if(f!=null)add(f.Path,f.Hash);
            return expected;
        }
        foreach(string id in References) {
            try {
                foreach(var file in MixedSourceRecovery.LoadEmbeddedReference(id).Files)add(file.Path,file.Hash);
            }catch(IOException e){if(e.Message.IndexOf("no embedded",StringComparison.OrdinalIgnoreCase)<0)throw;}
        }
        // The creator's 3.3.5 overlay records game-web-relative SHA-256 hashes.
        // Its complete overlay plus known native baselines covers upgraded profiles.
        string web=Store.WebRoot(plan.Root),prefix=RelativeFolder(plan.Root,web);
        foreach(var recipe in RecipeSupport.Catalog().Recipes) {
            if(recipe==null||!recipe.Id.StartsWith("three-in-one-3-3-5-",StringComparison.Ordinal)
                ||recipe.Files==null)continue;
            foreach(var kv in recipe.Files)add((prefix.Length==0?"":prefix+"/")+kv.Key,kv.Value);
        }
        return expected;
    }
    static string RelativeFolder(string root,string child) {
        if(Same(root,child))return "";
        if(!Below(child,root))throw new IOException("The web root escaped the game folder.");
        return Relative(root,child);
    }
    static void Enumerate(string root,List<string> files) {
        var stack=new Stack<Tuple<string,int>>();stack.Push(Tuple.Create(root,0));
        int count=0;
        while(stack.Count>0){
            var next=stack.Pop();
            if(next.Item2>100)throw new IOException("Excessive game folder depth.");
            NoLink(next.Item1);
            foreach(string item in Directory.EnumerateFileSystemEntries(next.Item1)) {
                if(++count>MaxEntries)throw new IOException("Game folder inventory exceeds the safety limit.");
                NoLink(item);
                if(Directory.Exists(item))stack.Push(Tuple.Create(item,next.Item2+1));
                else if(File.Exists(item))files.Add(item);
                else throw new IOException("Unrecognized filesystem entry: "+item);
            }
        }
    }
    public static RemovalPlan Plan(Profile profile,Settings settings) {
        return Plan(profile,settings,RemovalMode.InstalledGame);
    }
    public static RemovalPlan Plan(Profile profile,Settings settings,RemovalMode mode) {
        return BuildPlan(profile,settings,mode,null);
    }
    // Isolated, synthetic references are test-only: production cannot substitute caller hashes.
    internal static RemovalPlan PlanFixture(Profile profile,Settings settings,IEnumerable<MixedNativeFile> synthetic) {
        return BuildPlan(profile,settings,RemovalMode.InstalledGame,synthetic);
    }
    static RemovalPlan BuildPlan(Profile profile,Settings settings,RemovalMode mode,IEnumerable<MixedNativeFile> fixture) {
        if(profile==null||settings==null||settings.Profiles==null||string.IsNullOrWhiteSpace(profile.Id))
            throw new IOException("Select a registered profile.");
        var saved=settings.Profiles.SingleOrDefault(x=>x!=null&&x.Id==profile.Id);
        if(saved==null||!Same(saved.Root,profile.Root)||!Same(saved.SavePath,profile.SavePath))
            throw new IOException("The selected profile registration changed. Reload profiles and try again.");
        var plan=new RemovalPlan{Mode=mode,Root=Full(profile.Root),ProfileId=profile.Id,
            Profile=saved,Settings=settings,Files=new List<RemovalFile>(),RetainedSample=new List<string>(),FixtureReference=fixture};
        if(mode==RemovalMode.IndexOnly) {
            plan.Reason="Remove the launcher registration; game files, saves, backups, and archives remain.";
            plan.Message=plan.Reason;
            return plan;
        }
        if(mode!=RemovalMode.InstalledGame)throw new IOException("Unknown removal mode.");
        if(!Directory.Exists(plan.Root)) {plan.Reason="The game folder no longer exists. Use index-only removal.";return plan;}
        CheckParents(plan.Root);
        if(!Owned(plan)) {
            plan.Reason="This folder is outside the managed install location and has no verified launcher ownership marker. Use index-only removal.";
            return plan;
        }
        if(settings.Profiles.Any(x=>x!=null&&x.Id!=profile.Id&&Same(x.Root,plan.Root))) {
            plan.Reason="Another registered profile shares these game files. Remove its index entry first.";
            return plan;
        }
        var trusted=Trusted(plan);
        var files=new List<string>();
        Enumerate(plan.Root,files);
        foreach(string file in files) {
            string rel=Relative(plan.Root,file);
            bool backup;
            bool personal=SaveOrBackup(file,plan,out backup),archive=Archive(file);
            bool critical=Within(file,Store.Config)||Same(file,Store.Config)
                ||Within(file,Path.Combine(Store.Home,"Runtime"))||Within(file,Path.Combine(Store.Home,"Save Backups"));
            HashSet<string> hashes;
            var info=new FileInfo(file);
            bool owned=trusted.TryGetValue(rel,out hashes);
            if(!personal&&!archive&&!critical && (owned||MarkerNames.Any(x=>x.Equals(rel,StringComparison.OrdinalIgnoreCase))
                ||rel.Equals("launcher-upgrade.json",StringComparison.OrdinalIgnoreCase))) {
                string hash=Store.Hash(file);
                if(owned&&hashes.Contains(hash) || (!owned&&(MarkerNames.Any(x=>x.Equals(rel,StringComparison.OrdinalIgnoreCase))
                    ||rel.Equals("launcher-upgrade.json",StringComparison.OrdinalIgnoreCase)))) {
                    plan.Files.Add(new RemovalFile{Path=file,Hash=hash,Size=info.Length});
                    continue;
                }
            }
            plan.RetainedCount++;
            plan.RetainedBytes+=info.Length;
            if(archive)plan.PreservedArchives++;
            if(personal){if(backup)plan.PreservedBackups++;else plan.PreservedSaves++;}
            if(plan.RetainedSample.Count<24)plan.RetainedSample.Add(rel);
        }
        plan.CanDeleteFiles=plan.Files.Count>0;
        plan.Reason=plan.CanDeleteFiles
            ?"Remove exact SHA-256 verified game files; keep "+plan.RetainedCount+" unmatched/protected files, all saves and original archives."
            :"No files match trusted native or creator hashes. The installation remains untouched; use index-only removal.";
        plan.Message=plan.Reason;
        return plan;
    }
    static void RemoveVerified(RemovalPlan original,RemovalPlan current) {
        if(!current.CanDeleteFiles || original.FileCount!=current.FileCount
            ||original.Files.Any(x=>!current.Files.Any(y=>Paths.Equals(x.Path,y.Path)&&x.Hash==y.Hash&&x.Size==y.Size)))
            throw new IOException("Game files changed since the removal preview. Refresh the plan before removing anything.");
        // All files must still verify, before the first deletion.
        CheckParents(current.Root);
        foreach(var f in current.Files){
            NoLink(f.Path);
            if(!File.Exists(f.Path)||new FileInfo(f.Path).Length!=f.Size||Store.Hash(f.Path)!=f.Hash)
                throw new IOException("File changed since verification: "+f.Path);
        }
        foreach(var f in current.Files){
            CheckParents(Path.GetDirectoryName(f.Path));
            NoLink(f.Path);
            if(new FileInfo(f.Path).Length!=f.Size||Store.Hash(f.Path)!=f.Hash)
                throw new IOException("Game file changed during removal: "+f.Path);
            File.Delete(f.Path);
        }
        // Remove only now-empty directories. The containing folder is retained
        // whenever saves, backups, archives or unknown user files remain.
        var dirs=new List<string>();
        var queue=new Stack<string>();queue.Push(current.Root);
        while(queue.Count>0){
            string dir=queue.Pop();NoLink(dir);dirs.Add(dir);
            foreach(string sub in Directory.GetDirectories(dir)){NoLink(sub);queue.Push(sub);}
        }
        foreach(string dir in dirs.OrderByDescending(x=>x.Length)) {
            NoLink(dir);
            if(Directory.GetFileSystemEntries(dir).Length==0)Directory.Delete(dir);
        }
    }
    public static RemovalResult Execute(RemovalPlan plan) {
        return ExecuteCore(plan,true);
    }
    // Synthetic filesystem fixture only. Never touches profiles.json or the shared index.
    internal static RemovalResult ExecuteFixture(RemovalPlan plan) {return ExecuteCore(plan,false);}
    static RemovalResult ExecuteCore(RemovalPlan plan,bool persist) {
        if(plan==null||plan.Settings==null||plan.Profile==null)throw new IOException("Build a removal preview first.");
        var fresh=BuildPlan(plan.Profile,plan.Settings,plan.Mode,plan.FixtureReference);
        if(plan.Mode==RemovalMode.InstalledGame){
            if(!fresh.CanDeleteFiles)throw new IOException(fresh.Reason);
            if(persist&&Store.Running(new Settings{Profiles=new List<Profile>{plan.Profile}}))
                throw new IOException("Close the running game before removing installed files.");
            RemoveVerified(plan,fresh);
        }
        string warning=null;
        if(persist) {
            plan.Settings.Profiles.RemoveAll(x=>x!=null&&x.Id==plan.ProfileId&&Same(x.Root,plan.Root));
            if(!plan.Settings.Profiles.Any(x=>x!=null&&Same(x.Root,plan.Root))){
                Store.SuppressRoot(plan.Settings,plan.Root);
            }
            if(plan.Settings.SelectedId==plan.ProfileId) {
                var replacement=plan.Settings.Profiles.FirstOrDefault();
                plan.Settings.SelectedId=replacement==null?null:replacement.Id;
            }
            try {Store.Write(plan.Settings);}
            catch(Exception e) {
                if(plan.Mode==RemovalMode.InstalledGame)
                    throw new IOException("Verified game files were removed ("+fresh.FileCount+
                        "), but the launcher could not save the profile change. The files were already removed; reload the launcher and use index-only removal. "+e.Message,e);
                throw;
            }
            if(!plan.Settings.Profiles.Any(x=>x!=null&&Same(x.Root,plan.Root))) {
                try {SharedInstallationIndex.Forget(plan.Root);}
                catch(IOException e) {warning=" The shared installation index could not be refreshed: "+e.Message+
                    ". This install remains suppressed in the saved launcher settings.";}
                catch(UnauthorizedAccessException e) {warning=" The shared installation index could not be refreshed: "+e.Message+
                    ". This install remains suppressed in the saved launcher settings.";}
            }
        }
        return new RemovalResult{Root=plan.Root,Mode=plan.Mode,DeletedFiles=fresh.FileCount,
            DeletedBytes=fresh.FileBytes,RetainedFiles=fresh.RetainedCount,
            Message=(plan.Mode==RemovalMode.IndexOnly?"Profile removed from the launcher; personal files retained."
                :"Verified game files removed; saved games, archives, backups, and unmatched files retained.")+(warning??"")};
    }
}
