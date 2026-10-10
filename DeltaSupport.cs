using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Security.Cryptography;

public class DeltaFile {
    public string Path { get; set; }
    public long Size { get; set; }
    public string Hash { get; set; }
    public string Source { get; set; }
    public string SourceHash { get; set; }
    public long Prefix { get; set; }
    public long Suffix { get; set; }
    public string Payload { get; set; }
}
public class DeltaManifest {
    public int Format { get; set; }
    public string Name { get; set; }
    public string BaseDescription { get; set; }
    public List<DeltaFile> Files { get; set; }
}
public sealed class DigestSink : Stream {
    SHA256 digest = SHA256.Create();
    public long Count;
    public string Finish() { digest.TransformFinalBlock(new byte[0], 0, 0); return BitConverter.ToString(digest.Hash).Replace("-", "").ToLowerInvariant(); }
    public override void Write(byte[] b, int o, int n) { digest.TransformBlock(b, o, n, b, o); Count += n; }
    public override bool CanRead { get { return false; } } public override bool CanSeek { get { return false; } } public override bool CanWrite { get { return true; } }
    public override long Length { get { return Count; } } public override long Position { get { return Count; } set { throw new NotSupportedException(); } }
    public override void Flush() { } public override int Read(byte[] b,int o,int n) { throw new NotSupportedException(); } public override long Seek(long n,SeekOrigin o) { throw new NotSupportedException(); } public override void SetLength(long n) { throw new NotSupportedException(); }
    protected override void Dispose(bool disposing) { if (disposing) digest.Dispose(); base.Dispose(disposing); }
}
public static class DeltaSupport {
    static Dictionary<string,string> hashCache = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    static string Hash(string p) { string h; if (!hashCache.TryGetValue(p,out h)) hashCache[p] = h = Store.Hash(p); return h; }
    static string Relative(string root,string path) { return path.Substring(root.TrimEnd('\\').Length + 1).Replace('\\','/'); }
    public static string Resolve(string basis,string source) {
        if (source.StartsWith("content/")) return EnglishSupport.OriginalFile(basis,Store.Child(Store.WebRoot(basis),source.Substring(8).Replace('/','\\')));
        if (source.StartsWith("runtime/")) return EnglishSupport.OriginalFile(basis,Store.Child(basis,source.Substring(8).Replace('/','\\')));
        throw new IOException("Invalid delta source path.");
    }
    public static IEnumerable<string> Candidates(string basis,string relative,string webRelative) {
        string prefix=webRelative.Trim('/')+"/";
        bool runtime=relative.StartsWith("locales/",StringComparison.OrdinalIgnoreCase)||relative.StartsWith("swiftshader/",StringComparison.OrdinalIgnoreCase)||relative.IndexOf('/')<0&&(new[]{".exe",".dll",".pak",".bin",".dat",".ini",".ico"}.Contains(Path.GetExtension(relative).ToLowerInvariant())||relative.Equals("package.json",StringComparison.OrdinalIgnoreCase)||relative.Equals("steam_appid.txt",StringComparison.OrdinalIgnoreCase));
        string token=webRelative.Length==0?(runtime?"runtime/":"content/")+relative:relative.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)?"content/"+relative.Substring(prefix.Length):"runtime/"+relative;
        string ext = System.IO.Path.GetExtension(token).ToLowerInvariant();
        string alternate = ext == ".rpgmvp" ? ".png" : ext == ".rpgmvo" ? ".ogg" : ext == ".rpgmvm" ? ".m4a" : ext==".png"?".rpgmvp":ext==".ogg"?".rpgmvo":ext==".m4a"?".rpgmvm":null;
        // Plain CH assets are stable across encryption-key changes; prefer them when possible.
        if (alternate != null) { string alt = token.Substring(0,token.Length-ext.Length)+alternate; if(File.Exists(Resolve(basis,alt))) yield return alt; }
        if(File.Exists(Resolve(basis,token))) yield return token;
    }
    public static IEnumerable<string> GameFiles(string root) {
        string web = Store.WebRoot(root);
        var runtimeExtensions = new HashSet<string>(new[] { ".exe", ".dll", ".json", ".pak", ".bin", ".dat", ".html", ".ico", ".ini" });
        foreach(string file in Directory.GetFiles(root)) if(runtimeExtensions.Contains(System.IO.Path.GetExtension(file).ToLowerInvariant())||Path.GetFileName(file).Equals("steam_appid.txt",StringComparison.OrdinalIgnoreCase)) yield return file;
        foreach(string folder in Directory.GetDirectories(root)) {
            string name = System.IO.Path.GetFileName(folder).ToLowerInvariant();
            if(name == "www" || name == "locales" || name == "swiftshader" || name == "audio" || name == "data" || name == "fonts" || name == "img" || name == "js" || name == "movies" || name == "css" || name == "icon") {
                foreach(string file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories)) {
                    string rel=Relative(root,file);
                    if(rel.Split('/').Any(x => x == "save" || x == "output" || x == "decrypted" || x == "celesphonia-english")) continue;
                    if(Store.IsSave(file) || System.IO.Path.GetExtension(file).Equals(".docx",StringComparison.OrdinalIgnoreCase)) continue;
                    yield return file;
                }
            }
        }
    }
    public static long[] Common(string a,string b) {
        using(var source=File.OpenRead(a)) using(var target=File.OpenRead(b)) {
            long limit=Math.Min(source.Length,target.Length), prefix=0,suffix=0;
            byte[] x=new byte[65536],y=new byte[65536];
            while(prefix<limit) {
                int n=(int)Math.Min(x.Length,limit-prefix); source.Read(x,0,n); target.Read(y,0,n);
                int i=0; while(i<n && x[i]==y[i]) i++; prefix+=i; if(i<n) break;
            }
            long remaining=limit-prefix;
            while(suffix<remaining) {
                int n=(int)Math.Min(x.Length,remaining-suffix);
                source.Position=source.Length-suffix-n; target.Position=target.Length-suffix-n; source.Read(x,0,n);target.Read(y,0,n);
                int i=0; while(i<n && x[n-1-i]==y[n-1-i]) i++; suffix+=i;if(i<n)break;
            }
            return new[] {prefix,suffix};
        }
    }
    public static void CopyPart(Stream input,Stream output,long start,long size) {
        input.Position=start;byte[] b=new byte[65536];
        while(size>0) {int n=input.Read(b,0,(int)Math.Min(size,b.Length));if(n==0)throw new EndOfStreamException();output.Write(b,0,n);size-=n;}
    }
    public static void Build(string basis,string config) {
        hashCache.Clear();
        var settings=Store.Json.Deserialize<Settings>(File.ReadAllText(config,Encoding.UTF8));
        string packs=System.IO.Path.Combine(Store.Home,"Delta Packs");Directory.CreateDirectory(packs);
        var report=new List<object>();
        foreach(var p in settings.Profiles.Where(x => x.Id != "ch-current")) {
            string targetWeb=Store.WebRoot(p.Root);
            string pack=System.IO.Path.Combine(packs,p.Id+".cgdelta.zip");
            var files=GameFiles(p.Root).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(x=>Relative(p.Root,x),x=>x,StringComparer.OrdinalIgnoreCase);
            report.Add(BuildFiles(basis,p.Name,pack,Path.GetFullPath(p.Root).TrimEnd('\\').Equals(targetWeb.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)?"":Relative(p.Root,targetWeb),files));
            File.WriteAllText(System.IO.Path.Combine(Store.Home,"delta-build-report.json"),Store.Json.Serialize(report),Encoding.UTF8);
        }
    }
    static object BuildFiles(string basis,string name,string pack,string webRelative,Dictionary<string,string> files) {
            var manifest=new DeltaManifest {Format=1,Name=name,BaseDescription="CH installation matching the verified reused files; absolute installation paths are not required",Files=new List<DeltaFile>()};
            long bytes=0,payloadBytes=0; int reused=0,spliced=0,full=0;
            using(var zip=new ZipArchive(File.Create(pack),ZipArchiveMode.Create)) {
                foreach(var item in files.OrderBy(x=>x.Key)) {
                    string file=item.Value;
                    var info=new FileInfo(file);long size=info.Length;bytes+=size;
                    var record=new DeltaFile {Path=item.Key,Size=size,Hash=Hash(file)};
                    long best=size;
                    foreach(string candidate in Candidates(basis,item.Key,webRelative)) {
                        string source=Resolve(basis,candidate); long sourceSize=new FileInfo(source).Length;
                        if(sourceSize==size && Hash(source)==record.Hash) {
                            record.Source=candidate;record.SourceHash=Hash(source);record.Prefix=size;record.Suffix=0;best=0;break;
                        }
                        // Large unchanged tails handle RPG Maker encrypted/plain asset differences as tiny deltas.
                        if(size<65536 || sourceSize<65536) continue;
                        long[] common=Common(source,file);long middle=size-common[0]-common[1];
                        if(middle<best && middle<size/2) {best=middle;record.Source=candidate;record.SourceHash=Hash(source);record.Prefix=common[0];record.Suffix=common[1];}
                    }
                    if(record.Source!=null && best==0) reused++;
                    else {
                        if(record.Source!=null) spliced++;else full++;
                        record.Payload="payload/"+manifest.Files.Count.ToString("D6");
                        var entry=zip.CreateEntry(record.Payload,CompressionLevel.Optimal);
                        using(var output=entry.Open()) using(var input=File.OpenRead(file)) CopyPart(input,output,record.Prefix,size-record.Prefix-record.Suffix);
                        payloadBytes+=size-record.Prefix-record.Suffix;
                    }
                    manifest.Files.Add(record);
                }
                var metadata=zip.CreateEntry("manifest.json",CompressionLevel.Optimal);
                using(var writer=new StreamWriter(metadata.Open(),new UTF8Encoding(false))) writer.Write(Store.Json.Serialize(manifest));
            }
            return new {name=name,pack=System.IO.Path.GetFileName(pack),targetBytes=bytes,downloadBytes=new FileInfo(pack).Length,literalBytes=payloadBytes,reusedFiles=reused,splicedFiles=spliced,fullFiles=full,totalFiles=manifest.Files.Count};
    }
    public static void BuildLayered(string ch,string cleanBase,string overlay) {
        hashCache.Clear();RecipeSupport.ValidateBase(cleanBase);overlay=RecipeSupport.ContentRoot(overlay);
        var recipe=RecipeSupport.Identify(overlay);
        if(recipe==null || !recipe.CompleteOverlay || recipe.RequiredBase!="gorpg-1-23")throw new IOException("Select a recognized complete Gorpg overlay bundle.");
        string webRelative=Relative(cleanBase,Store.WebRoot(cleanBase));
        var files=GameFiles(cleanBase).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(x=>Relative(cleanBase,x),x=>x,StringComparer.OrdinalIgnoreCase);
        foreach(string source in Directory.GetFiles(overlay,"*",SearchOption.AllDirectories)){
            string rel=Relative(overlay,source);
            if(rel.Split('/').Any(x=>x.Equals("save",StringComparison.OrdinalIgnoreCase)) || Store.IsSave(source) || Path.GetExtension(source).Equals(".docx",StringComparison.OrdinalIgnoreCase))continue;
            files[webRelative+"/"+rel]=source;
        }
        string packs=Path.Combine(Store.Home,"Delta Packs");Directory.CreateDirectory(packs);
        string pack=Path.Combine(packs,recipe.Id+".cgdelta.zip");
        var report=BuildFiles(ch,recipe.Name,pack,webRelative,files);
        File.WriteAllText(Path.Combine(Store.Home,"delta-build-"+recipe.Id+".json"),Store.Json.Serialize(report),Encoding.UTF8);
    }
    static DeltaManifest Manifest(ZipArchive zip) {
        var entry=zip.GetEntry("manifest.json");if(entry==null)throw new IOException("This is not a Celesphonia delta pack.");
        using(var reader=new StreamReader(entry.Open(),Encoding.UTF8)) {
            var result=Store.Json.Deserialize<DeltaManifest>(reader.ReadToEnd());
            if(result.Format!=1 || result.Files==null)throw new IOException("Unsupported delta format.");return result;
        }
    }
    static void Preflight(string basis,DeltaManifest manifest) {
        foreach(var f in manifest.Files) {
            Store.Child(Store.Home,f.Path.Replace('/','\\'));
            if(f.Size<0 || f.Prefix<0 || f.Suffix<0 || f.Prefix+f.Suffix>f.Size)throw new IOException("Invalid delta sizes.");
            if(f.Source!=null) {
                string source=Resolve(basis,f.Source);
                if(!File.Exists(source) || Hash(source)!=f.SourceHash)throw new IOException("This CH build differs from the required base file: "+f.Source+". No existing game or saves were changed. This pack cannot reconstruct modified/missing base assets.");
                if(f.Prefix+f.Suffix>new FileInfo(source).Length)throw new IOException("Invalid source ranges.");
            }
        }
    }
    static void Reconstruct(ZipArchive zip,string basis,DeltaFile f,Stream output) {
        if(f.Source!=null) using(var source=File.OpenRead(Resolve(basis,f.Source))) CopyPart(source,output,0,f.Prefix);
        if(f.Payload!=null) {
            var entry=zip.GetEntry(f.Payload);if(entry==null)throw new IOException("Missing patch payload.");
            if(entry.Length!=f.Size-f.Prefix-f.Suffix)throw new IOException("Invalid patch payload size.");
            using(var literal=entry.Open()) literal.CopyTo(output);
        } else if(f.Prefix+f.Suffix!=f.Size)throw new IOException("Incomplete delta.");
        if(f.Source!=null && f.Suffix>0)using(var source=File.OpenRead(Resolve(basis,f.Source)))CopyPart(source,output,source.Length-f.Suffix,f.Suffix);
    }
    public static Profile Install(string pack,string basis,string installRoot) {
        hashCache.Clear();
        using(var zip=ZipFile.OpenRead(pack)) {
            var manifest=Manifest(zip);Preflight(basis,manifest);
            Directory.CreateDirectory(installRoot);
            long required=manifest.Files.Sum(x=>x.Size)+1024L*1024*1024;
            if(new DriveInfo(System.IO.Path.GetPathRoot(installRoot)).AvailableFreeSpace<required)throw new IOException("Insufficient disk space to reconstruct this profile. Allow about "+(required/(1024.0*1024*1024)).ToString("0.0")+" GB.");
            string id=Guid.NewGuid().ToString("N"),root=Store.Child(installRoot,"Delta-"+id.Substring(0,8));Directory.CreateDirectory(root);
            foreach(var f in manifest.Files) {
                string destination=Store.Child(root,f.Path.Replace('/','\\'));Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
                using(var output=File.Create(destination))Reconstruct(zip,basis,f,output);
                if(new FileInfo(destination).Length!=f.Size || Store.Hash(destination)!=f.Hash)throw new IOException("Reconstructed file failed verification: "+f.Path+". This unfinished profile has not been registered.");
            }
            var p=Store.Detect(root,manifest.Name);Directory.CreateDirectory(p.SavePath);p.Note="Reconstructed from CH + portable delta pack; separate saves";return p;
        }
    }
    public static void Verify(string basis) {
        hashCache.Clear();var verified=new List<object>();
        foreach(string pack in Directory.GetFiles(System.IO.Path.Combine(Store.Home,"Delta Packs"),"*.cgdelta.zip")) {
            using(var zip=ZipFile.OpenRead(pack)) {
                var manifest=Manifest(zip);Preflight(basis,manifest);
                foreach(var f in manifest.Files) using(var sink=new DigestSink()) {Reconstruct(zip,basis,f,sink);if(sink.Count!=f.Size || sink.Finish()!=f.Hash)throw new IOException("Delta verification failed: "+manifest.Name+" / "+f.Path);}
                verified.Add(new {profile=manifest.Name,verifiedFiles=manifest.Files.Count,status="PASS: every reconstructed byte matches the installed target; no files written"});
            }
            File.WriteAllText(System.IO.Path.Combine(Store.Home,"delta-verification.json"),Store.Json.Serialize(verified),Encoding.UTF8);
        }
    }
    public static void VerifyPack(string basis,string pack,string target) {
        hashCache.Clear();
        using(var zip=ZipFile.OpenRead(pack)){
            var manifest=Manifest(zip);Preflight(basis,manifest);
            foreach(var f in manifest.Files){
                using(var sink=new DigestSink()){Reconstruct(zip,basis,f,sink);if(sink.Count!=f.Size || sink.Finish()!=f.Hash)throw new IOException("Delta verification failed: "+f.Path);}
                if(!string.IsNullOrEmpty(target)){string file=Store.Child(target,f.Path.Replace('/','\\'));if(!File.Exists(file)||new FileInfo(file).Length!=f.Size||Store.Hash(file)!=f.Hash)throw new IOException("Installed target differs from delta: "+f.Path);}
            }
            File.WriteAllText(Path.Combine(Store.Home,"delta-pack-verification.json"),Store.Json.Serialize(new {profile=manifest.Name,verifiedFiles=manifest.Files.Count,installedTargetCompared=!string.IsNullOrEmpty(target),status="PASS: every reconstructed file matches its SHA-256 and the supplied installed target"}),Encoding.UTF8);
        }
    }
    public static void Attach(Form form,Settings settings,Action<Profile> added,Action<bool> setBusy) {
        form.ClientSize=new Size(880,645);form.MinimumSize=new Size(896,684);
        foreach(Control c in form.Controls)if(c.Location.Y==516)c.Location=new Point(c.Location.X,594);
        var button=new Button {Text="Install portable delta pack (reuse an existing CH installation)",Location=new Point(28,508),Size=new Size(820,42),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(87,75,181),ForeColor=Color.White};
        button.FlatAppearance.BorderSize=0;form.Controls.Add(button);
        form.Controls.Add(new Label {Text="Delta packs rebuild a separate version. Your CH game and saves stay in place.",Location=new Point(28,560),Size=new Size(820,25),ForeColor=Color.FromArgb(166,179,204)});
        button.Click+=async delegate {
            if(Store.Running(settings)){MessageBox.Show(form,"Save and close the game first.");return;}
            string pack;using(var d=new OpenFileDialog {Title="Select a portable delta pack",Filter="Celesphonia delta packs|*.cgdelta.zip",InitialDirectory=System.IO.Path.Combine(Store.Home,"Delta Packs")}){if(d.ShowDialog(form)!=DialogResult.OK)return;pack=d.FileName;}
            string basis;using(var d=new FolderBrowserDialog {Description="Select the existing CH game folder containing package.json and Game.exe.",SelectedPath=settings.Profiles.First().Root,ShowNewFolderButton=false}){if(d.ShowDialog(form)!=DialogResult.OK)return;basis=d.SelectedPath;}
            setBusy(true);foreach(Control c in form.Controls)c.Enabled=false;
            bool installing=true;FormClosingEventHandler prevent=delegate(object sender,FormClosingEventArgs e){if(installing)e.Cancel=true;};form.FormClosing+=prevent;
            string originalTitle=form.Text;form.Text="Installing delta pack - validating and reconstructing game files...";
            try {var p=await Task.Run(()=>Install(pack,basis,settings.InstallRoot));added(p);MessageBox.Show(form,"Installed "+p.Name+". Select it and press Switch & Play. It has its own empty save set.");}
            catch(Exception e){MessageBox.Show(form,e.Message,"Delta install",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally {installing=false;form.FormClosing-=prevent;form.Text=originalTitle;foreach(Control c in form.Controls)c.Enabled=true;setBusy(false);}
        };
    }
}
public static class PortableMain {
    [STAThread] public static int Main(string[] args) {
        try {
            Store.Json.MaxJsonLength=256*1024*1024;
            // The isolated helper runs from an EXE copy outside Store.Home;
            // pass the actual installed home explicitly before normal startup.
            if(args.Length==4 && args[0]=="--apply-launcher-update"){
                LauncherUpdate.Apply(args[1],args[2],int.Parse(args[3]));return 0;
            }
            if(args.Length==3 && args[0]=="--test-launcher-update"){
                LauncherUpdate.Test(args[1],args[2]);return 0;
            }
            if(args.Length==3 && args[0]=="--test-launcher-apply"){
                LauncherUpdate.TestApply(args[1],args[2]);return 0;
            }
            if(args.Length>2 && args[0]=="--prepare-control-session"){var p=Store.Detect(args[1],"Private runtime verification");p.Id="control-verification-fixture";File.WriteAllText(args[2],RuntimeSupport.Prepare(p),new UTF8Encoding(false));return 0;}
            if(args.Length>1 && args[0]=="--english-pack-report") {File.WriteAllText(args[1],Store.Json.Serialize(EnglishRelease.Report()),new UTF8Encoding(false));return 0;}
            if(args.Length>2 && args[0]=="--english-status") {File.WriteAllText(args[2],Store.Json.Serialize(EnglishSupport.Status(Store.Detect(args[1],"Selected game"))),new UTF8Encoding(false));return 0;}
            if(args.Length>2 && args[0]=="--verify-upgrade-bundle") {
                var recipe=RecipeSupport.Catalog().Recipes.First(x=>x.Id==args[2]);
                bool archived=File.Exists(args[1]);string temp=archived?Store.ArchiveStaging("verify"):null;
                try {
                    var root=archived?Store.Extract(args[1],temp,true):RecipeSupport.ContentRoot(args[1]);
                    UpgradeSupport.VerifyBundle(root,recipe);return 0;
                }
                finally {
                    // The original archive and any user-supplied extracted
                    // folder belong to the user. Only our unique temp root
                    // is eligible for cleanup, even after extraction fails.
                    if(archived)Store.CleanArchiveStaging(temp);
                }
            }
            if(args.Length>0 && args[0]=="--test-upgrade") {UpgradeTests.Run();return 0;}
            if(args.Length==5 && args[0]=="--test-real-upgrade-chain") {UpgradeTests.RealChain(args[1],args[2],args[3],args[4]);return 0;}
            if(args.Length>4 && args[0]=="--upgrade-335") {var source=Store.Read().Profiles.FirstOrDefault(x=>Path.GetFullPath(x.Root).Equals(Path.GetFullPath(args[1]),StringComparison.OrdinalIgnoreCase))??Store.Detect(args[1],"Compatible 3.3.5 source");var profile=UpgradeSupport.Install(source,args[2],args[3],UpgradeSupport.Latest,true);File.WriteAllText(args[4],Store.Json.Serialize(profile),Encoding.UTF8);return 0;}
            if(args.Length>2 && args[0]=="--probe-archive") {
                string format=FileDiscovery.ArchiveFormat(args[1]);
                var zip=format=="zip"?FileDiscovery.ProbeZip(args[1]):new List<FoundSource>();
                var rar=format=="rar"?FileDiscovery.ProbeRar(args[1],System.Threading.CancellationToken.None):new List<FoundSource>();
                var recipe=RecipeSupport.IdentifyArchive(args[1]);
                File.WriteAllText(args[2],Store.Json.Serialize(new{format=format,zip=zip,rar=rar,
                    recipe=recipe,recognizedVersion=recipe==null?(zip.Concat(rar).Select(x=>x.RecipeId).FirstOrDefault()):recipe.Id}),Encoding.UTF8);return 0;
            }
            if(args.Length==3 && args[0]=="--scan-folder"){
                var hits=FileDiscovery.ScanFolders(new[]{args[1]},System.Threading.CancellationToken.None,null);
                File.WriteAllText(args[2],Store.Json.Serialize(hits),new UTF8Encoding(false));return 0;
            }
            if(args.Length>0 && args[0]=="--test-english") {EnglishTests.Run();return 0;}
            if(args.Length>1 && args[0]=="--english-apply") {EnglishSupport.Apply(Store.Detect(args[1],"Selected game"));return 0;}
            if(args.Length>1 && args[0]=="--english-restore") {EnglishSupport.Restore(Store.Detect(args[1],"Selected game"));return 0;}
            if(args.Length>0 && args[0]=="--build") {DeltaSupport.Build(args[1],args[2]);return 0;}
            if(args.Length>0 && args[0]=="--build-any") {UniversalSupport.Build(args[1],args[2]);return 0;}
            if(args.Length>0 && args[0]=="--verify-any") {UniversalSupport.VerifyAll(args[1],args[2]);return 0;}
            if(args.Length>0 && args[0]=="--install-any") {var p=UniversalSupport.Install(args[1],args[2],args[3],args[4]);File.WriteAllText(args[5],Store.Json.Serialize(p),Encoding.UTF8);return 0;}
            if(args.Length>0 && args[0]=="--build-layers") {DeltaSupport.BuildLayered(args[1],args[2],args[3]);return 0;}
            if(args.Length>0 && args[0]=="--verify") {DeltaSupport.Verify(args[1]);return 0;}
            if(args.Length>0 && args[0]=="--verify-pack") {DeltaSupport.VerifyPack(args[1],args[2],args.Length>3?args[3]:null);return 0;}
            if(args.Length>0 && args[0]=="--install-layers") {var profile=RecipeSupport.Install(args[1],args[2],args[3],args.Skip(4).Select(x=>new LayerInput{Source=x}).ToList());var settings=Store.Read();settings.Profiles.Add(profile);settings.SelectedId=profile.Id;Store.Write(settings);return 0;}
            if(args.Length>0 && args[0]=="--recipe-test") {RecipeSupport.SelfTest();return 0;}
            if(args.Length>0 && args[0]=="--self-test") {Store.SelfTest();return 0;}
            if(args.Length>1 && args[0]=="--folder-report") {var config=Store.Read();var report=config.Profiles.Select(p=>new {id=p.Id,root=p.Root,save=p.SavePath,folders=LauncherFolders.Targets(p,config.InstallRoot,UniversalSupport.Identify(p.Root))}).ToList();File.WriteAllText(args[1],Store.Json.Serialize(report),Encoding.UTF8);return 0;}
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length>1 && args[0]=="--menu-test"){LauncherMenuLifetime.Test(args[1]);return 0;}
            if(args.Length>2 && args[0]=="--layout-test"){LauncherLayoutTests.Run(args[1],args[2]);return 0;}
            if(args.Length>1 && args[0]=="--instance-probe")return LauncherInstance.Probe(args[1],args.Length>2?int.Parse(args[2]):0);
            bool preview=args.Length>0 && args[0]=="--render-preview";
            using(var instance=preview?null:LauncherInstance.Acquire()){
            if(!preview&&instance==null)return 0;
            if(!File.Exists(Store.Config)) {
                // Open the library immediately. Its cancellable background scan
                // finds the current user's games; distributions contain no profiles.
                Store.Write(new Settings{Profiles=new List<Profile>(),InstallRoot=Path.Combine(Store.Home,"Game Profiles")});
            }
            if(args.Length>0 && args[0]=="--render-preview") {using(var f=new PolishedLauncher(true)){f.Show();f.PreviewPage(args.Length>1?args[1]:"Play");Application.DoEvents();using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(Point.Empty,f.Size));bitmap.Save(System.IO.Path.Combine(Store.Home,"portable-preview"+(args.Length>1?"-"+args[1].ToLowerInvariant():"")+".png"));}}return 0;}
            using(var launcher=new PolishedLauncher()){if(args.Contains("--english"))launcher.Shown+=delegate{launcher.OpenEnglishPage();};Application.Run(launcher);}
            }return 0;
        }catch(Exception e){File.WriteAllText(System.IO.Path.Combine(Store.Home,"delta-error.txt"),e.ToString(),Encoding.UTF8);if(args.Length==0)MessageBox.Show(e.Message,"Portable delta switcher");return 1;}
    }
}
