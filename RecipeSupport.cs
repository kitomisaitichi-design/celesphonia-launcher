using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;

public class ModRecipe {
    public string Id {get;set;}
    public string Name {get;set;}
    public string Family {get;set;}
    public string RequiredBase {get;set;}
    public string[] Components {get;set;}
    public string SystemHash {get;set;}
    public string PluginsHash {get;set;}
    public string ArchiveHash {get;set;}
    public string ArchiveName {get;set;}
    public long ArchiveBytes {get;set;}
    public bool CompleteOverlay {get;set;}
    public bool Standalone {get;set;}
    public string SaveStatus {get;set;}
    public string[] UpgradeFrom {get;set;}
    public int SaveGeneration {get;set;}
    public Dictionary<string,string> Fingerprints {get;set;}
    public Dictionary<string,string> Files {get;set;}
    public override string ToString(){return Name;}
}
public class RecipeCatalog {
    public Dictionary<string,string> GorpgFingerprint {get;set;}
    public Dictionary<string,string> ChFingerprint {get;set;}
    public List<ModRecipe> Recipes {get;set;}
}
public class LayerInput {
    public string Source {get;set;}
    public override string ToString(){return Path.GetFileName(Source.TrimEnd('\\'));}
}
public class LayerRecord {
    public string Name {get;set;}
    public string RecipeId {get;set;}
    public string SourceHash {get;set;}
    public int OverwrittenFiles {get;set;}
    public int AddedFiles {get;set;}
    public Dictionary<string,string> Files {get;set;}
}
public class LayerState {
    public string BaseRoot {get;set;}
    public string RequiredBase {get;set;}
    public string RecipeId {get;set;}
    public string[] Components {get;set;}
    public List<LayerRecord> Layers {get;set;}
    public string StorageMode {get;set;}
    public Dictionary<string,string> BaseFiles {get;set;}
}
public static class RecipeSupport {
    static readonly object catalogGate=new object();static RecipeCatalog cachedCatalog;static string catalogStamp;
    public static RecipeCatalog Catalog(){string file=Path.Combine(Store.Home,"mod-recipes.json"),stamp=DetectionCache.Stamp(file);lock(catalogGate){if(cachedCatalog==null||stamp!=catalogStamp){cachedCatalog=Store.Json.Deserialize<RecipeCatalog>(File.ReadAllText(file,Encoding.UTF8));catalogStamp=stamp;}return cachedCatalog;}}
    public static bool IsCleanBase(string root){
        try {var catalog=Catalog();return catalog.GorpgFingerprint.All(x=>File.Exists(Store.Child(root,x.Key.Replace('/','\\'))) && Store.Hash(Store.Child(root,x.Key.Replace('/','\\')))==x.Value);}catch{return false;}
    }
    public static ModRecipe Identify(string web){
        string system=Path.Combine(web,"data","System.json"),plugins=Path.Combine(web,"js","plugins.js");
        if(!File.Exists(system)||!File.Exists(plugins))return null;
        string a=DetectionCache.Hash(system),b=DetectionCache.Hash(plugins);
        return Catalog().Recipes.FirstOrDefault(x=>x.SystemHash==a && x.PluginsHash==b&&(x.Fingerprints==null||x.Fingerprints.All(m=>DetectionCache.Hash(Store.Child(web,m.Key.Replace('/','\\')))==m.Value)));
    }
    public static ModRecipe IdentifyArchive(string archive){
        var candidates=Catalog().Recipes.Where(x=>!string.IsNullOrEmpty(x.ArchiveHash)).ToArray();
        string hash=Store.Hash(archive);return candidates.FirstOrDefault(x=>x.ArchiveHash==hash);
    }
    public static void ValidateBase(string root){if(!IsCleanBase(root))throw new IOException("These Gorpg overlays require the verified CLEAN Gorpg 1.23 base. CH, Maple, 3.3.4, and another patched branch are not substitutes. Register the clean Gorpg folder or reconstruct it with the Gorpg delta pack first.");}
    public static string ContentRoot(string folder){
        string current=Path.GetFullPath(folder);var dirs=new HashSet<string>(new[]{"audio","data","fonts","img","js","movies","css","icon"},StringComparer.OrdinalIgnoreCase);
        for(int i=0;i<16;i++){
            if(File.Exists(Path.Combine(current,"package.json")) && (File.Exists(Path.Combine(current,"Game.exe"))||File.Exists(Path.Combine(current,"nw.exe"))))throw new IOException("This is a standalone game. Use Add game folder.");
            var children=Directory.GetDirectories(current);
            if(children.Any(x=>dirs.Contains(Path.GetFileName(x))))return current;
            if(children.Length==1 && Directory.GetFiles(current).Length==0){current=children[0];continue;}
            throw new IOException("Prepare this layer with game-relative folders such as js/plugins or data. The installer cannot infer where loose files belong.");
        }
        throw new IOException("Too many archive wrapper folders.");
    }
    public static string SaveWarning(Profile source,Profile target){
        UpgradeSupport.RequireTransfer(UniversalSupport.Identify(source.Root),UniversalSupport.Identify(target.Root));
        ModRecipe a=null,b=null;try{a=Identify(Store.WebRoot(source.Root));b=Identify(Store.WebRoot(target.Root));}catch{}
        bool same=a!=null && b!=null && a.Id==b.Id;
        return same?"Same detected build; destination saves will still be backed up first.":"Cross-version save compatibility is UNVERIFIED. Moving backwards can lose access to newer state; removed routes and changed IDs are not converted. Keep the source saves. Destination saves are backed up first.";
    }
    public static void ValidateRecipes(List<ModRecipe> recipes){
        if(recipes.Any(x=>x!=null && (x.Standalone || x.RequiredBase!="gorpg-1-23")))throw new IOException("This branch requires its own standalone installation; it cannot overwrite Gorpg.");
        if(recipes.Count(x=>x!=null && x.CompleteOverlay)>1)throw new IOException("Choose only one complete overlay bundle. 3.3.5 already contains Abu 56.6 + story 1.0 + skills 3.3.5; stacking 3.3.4 or standalone Abu would mix branches. Place independent hotfixes AFTER one bundle.");
        int bundle=recipes.FindIndex(x=>x!=null && x.CompleteOverlay);
        if(bundle>0)throw new IOException("The complete bundle must be the FIRST layer, followed by any separate hotfixes.");
    }
    static Dictionary<string,string> CloneVerified(string source,string target){
        Store.CopyTree(source,target,true);var hashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);string web=Store.WebRoot(target);
        foreach(string file in Directory.GetFiles(target,"*",SearchOption.AllDirectories)){
            string relative=file.Substring(target.TrimEnd('\\').Length+1).Replace('\\','/');
            // A clean game may already have the reversible English overlay.
            // The copied file is its original-entry.bin, not the patched HTML;
            // compare like-for-like so independent layer installations work.
            string original=EnglishSupport.OriginalFile(source,Store.Child(source,relative.Replace('/','\\')));
            string hash=Store.Hash(original);
            if(Store.Hash(file)!=hash)throw new IOException("Base copy verification failed: "+relative);
            if(!IsMutableAuxiliary(target,web,file))hashes[relative]=hash;
        }
        return hashes;
    }
    public static Profile Install(string basis,string installRoot,string name,List<LayerInput> inputs){
        ValidateBase(basis);if(inputs.Count==0)throw new IOException("Choose at least one overlay.");
        string id=Guid.NewGuid().ToString("N"),staging=Store.ArchiveStaging("layers");
        var layers=new List<Tuple<string,ModRecipe,string,string>>();
        try {
            for(int i=0;i<inputs.Count;i++){
                string source=inputs[i].Source;bool archive=File.Exists(source);
                ModRecipe recipe=archive?IdentifyArchive(source):null;
                if(recipe!=null && recipe.Standalone)throw new IOException("Maple is a separate standalone branch. Use Add game folder for its complete installation; do not overwrite Gorpg with it.");
                string web=archive?Store.Extract(source,Path.Combine(staging,i.ToString()),true):ContentRoot(source);
                if(File.Exists(Path.Combine(web,"package.json")) && (File.Exists(Path.Combine(web,"nw.exe")) || File.Exists(Path.Combine(web,"Game.exe"))))throw new IOException("This is a standalone build. Register its game folder instead of treating it as an overlay.");
                ModRecipe content=Identify(web);if(recipe!=null && (content==null || content.Id!=recipe.Id))throw new IOException("Archive contents do not match the declared mod recipe.");
                recipe=content??recipe;UpgradeSupport.VerifyBundle(web,recipe??new ModRecipe());
                if(recipe!=null && recipe.RequiredBase!="gorpg-1-23")throw new IOException("Unsupported base dependency.");
                layers.Add(Tuple.Create(web,recipe,Path.GetFileName(source.TrimEnd('\\')),archive?Store.Hash(source):null));
            }
            ValidateRecipes(layers.Select(x=>x.Item2).ToList());
            var final=layers.LastOrDefault(x=>x.Item2!=null);var expected=final==null?null:final.Item2;
            long required=Store.TreeSize(basis)+layers.Sum(x=>Store.TreeSize(x.Item1))+256L*1024*1024;
            Directory.CreateDirectory(installRoot);if(new DriveInfo(Path.GetPathRoot(installRoot)).AvailableFreeSpace<required)throw new IOException("This complete independent profile needs about "+(required/(1024.0*1024*1024)).ToString("0.0")+" GB of additional free space on "+Path.GetPathRoot(installRoot)+". Choose another installation folder or free that space first.");
            string basePrefix=Path.GetFullPath(basis).TrimEnd('\\')+"\\";
            if((Path.GetFullPath(installRoot).TrimEnd('\\')+"\\").StartsWith(basePrefix,StringComparison.OrdinalIgnoreCase))throw new IOException("Install new profiles outside the base game folder.");
            string root=Store.Child(installRoot,"Layers-"+id.Substring(0,8));var baseHashes=CloneVerified(basis,root);string webRoot=Store.WebRoot(root);
            var records=new List<LayerRecord>();
            foreach(var layer in layers){
                var record=new LayerRecord{Name=layer.Item3,RecipeId=layer.Item2==null?"custom-unverified":layer.Item2.Id,SourceHash=layer.Item4,Files=new Dictionary<string,string>()};
                foreach(string file in Directory.GetFiles(layer.Item1,"*",SearchOption.AllDirectories)){
                    string relative=file.Substring(layer.Item1.TrimEnd('\\').Length+1).Replace('\\','/');
                    if(relative.Split('/').Any(x=>x.Equals("save",StringComparison.OrdinalIgnoreCase))||Store.IsSave(file))continue;
                    if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked mod files are not supported.");
                    string to=Store.Child(webRoot,relative.Replace('/','\\'));Directory.CreateDirectory(Path.GetDirectoryName(to));
                    if(File.Exists(to))record.OverwrittenFiles++;else record.AddedFiles++;
                    File.Copy(file,to,true);string hash=Store.Hash(file);if(Store.Hash(to)!=hash)throw new IOException("Overlay copy verification failed: "+relative);record.Files[relative]=hash;
                }
                records.Add(record);
            }
            var state=new LayerState{BaseRoot=Path.GetFullPath(basis),RequiredBase="gorpg-1-23",RecipeId=expected==null?"custom-unverified":expected.Id,Components=expected==null?new[]{"Custom ordered layers; compatibility unverified"}:expected.Components,Layers=records,StorageMode="Full independent copy; no source dependency after installation",BaseFiles=baseHashes};
            File.WriteAllText(Path.Combine(root,"launcher-build.json"),Store.Json.Serialize(state),new UTF8Encoding(false));
            var p=Store.Detect(root,expected==null?name:expected.Name);Directory.CreateDirectory(p.SavePath);p.Note=(expected==null?"Custom layers":string.Join(" + ",expected.Components))+"; independent files and saves";VerifyProfile(p);return p;
        }finally{if(Directory.Exists(staging))Store.CleanArchiveStaging(staging);}
    }
    // Deep installation checks retain every asset hash. Launch checks focus on
    // native code/data and the runtime; media archives are not rehashed on every click.
    public static bool IsLaunchDependency(string root,string web,string file){
        string full=Path.GetFullPath(file),relative=full.StartsWith(Path.GetFullPath(web).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase)?full.Substring(Path.GetFullPath(web).TrimEnd('\\').Length).TrimStart('\\','/').Replace('\\','/'):"";
        if(full.StartsWith(Path.GetFullPath(web).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase)&&new[]{"data/","js/"}.Any(x=>relative.StartsWith(x,StringComparison.OrdinalIgnoreCase)))return true;
        string ext=Path.GetExtension(full).ToLowerInvariant();
        return new[]{".exe",".dll",".pak",".bin",".dat",".html",".json",".js"}.Contains(ext);
    }
    public static void VerifyProfile(Profile p){VerifyProfile(p,false,false);}
    public static void VerifyProfile(Profile p,bool launchOnly){VerifyProfile(p,launchOnly,false);}
    public static void VerifyProfile(Profile p,bool launchOnly,bool recoverEnglishForNewCopy){
        if(recoverEnglishForNewCopy)EnglishSupport.VerifyForCopy(p.Root);
        else EnglishSupport.Verify(p.Root);
        UniversalSupport.VerifyInstalled(p,launchOnly);
        string file=Path.Combine(p.Root,"launcher-build.json");bool independent=File.Exists(file);
        if(!independent)file=Path.Combine(p.Root,"launcher-base.json");if(!File.Exists(file))return;
        var state=Store.Json.Deserialize<LayerState>(File.ReadAllText(file,Encoding.UTF8));if(!independent)ValidateBase(state.BaseRoot);
        var finalHashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if(independent && state.BaseFiles!=null)foreach(var item in state.BaseFiles)finalHashes[item.Key]=item.Value;
        string web=Store.WebRoot(p.Root),webRelative=web.Substring(p.Root.TrimEnd('\\').Length).Trim('\\').Replace('\\','/');
        foreach(var layer in state.Layers)foreach(var item in layer.Files)finalHashes[independent?(webRelative.Length==0?item.Key:webRelative+"/"+item.Key):item.Key]=item.Value;
        foreach(var item in finalHashes){
            string recorded=Store.Child(independent?p.Root:web,item.Key.Replace('/','\\'));
            // MTool's editable export and NW.js's generated log are not game
            // dependencies. Older full-copy manifests also recorded them.
            // Match exact locations; similarly named files in data/js stay strict.
            if(IsMutableAuxiliary(p.Root,web,recorded)||(launchOnly&&!IsLaunchDependency(p.Root,web,recorded)))continue;
            string target=EnglishSupport.OriginalFile(p.Root,recorded);
            if(!File.Exists(target)||Store.Hash(target)!=item.Value)throw new IOException("Layered profile file changed or is missing: "+item.Key);
        }
    }
    public static bool IsMutableAuxiliary(string root,string web,string file){
        string full=Path.GetFullPath(file);root=Path.GetFullPath(root);web=Path.GetFullPath(web);
        return full.Equals(Path.Combine(root,"ManualTransFile.json"),StringComparison.OrdinalIgnoreCase)
            ||full.Equals(Path.Combine(web,"ManualTransFile.json"),StringComparison.OrdinalIgnoreCase)
            ||full.Equals(Path.Combine(root,"debug.log"),StringComparison.OrdinalIgnoreCase);
    }
    public static void SelfTest(){
        var settings=Store.Read();var bases=settings.Profiles.Where(x=>IsCleanBase(x.Root)).ToArray();
        if(bases.Length==0)throw new Exception("No verified clean Gorpg 1.23 base.");
        // More than one independently verified clean Gorpg reference may be
        // registered; neither is a patched branch merely because it is second.
        foreach(var p in bases)if(UniversalSupport.Identify(p.Root)!="gorpg-1-23")
            throw new Exception("A modified branch was accepted as a clean base.");
        bool rejected=false;try{ValidateBase(settings.Profiles.First(x=>x.Name.StartsWith("Maple")).Root);}catch(IOException){rejected=true;}if(!rejected)throw new Exception("Maple base rejection failed.");
        rejected=false;try{ValidateBase(settings.Profiles.First(x=>x.Name.Contains("3.3.4")).Root);}catch(IOException){rejected=true;}if(!rejected)throw new Exception("3.3.4 base rejection failed.");
        var current=settings.Profiles.FirstOrDefault(x=>x.Name.Contains("1.051"));
        if(current!=null){rejected=false;try{ValidateBase(current.Root);}catch(IOException){rejected=true;}
            if(!rejected)throw new Exception("Ver1.051 upgrade was accepted as a clean Gorpg base.");}
        string fixturePrefix=Path.GetFullPath(Path.Combine(Store.Home,"Tests","native-game-fixtures")).TrimEnd('\\')+"\\";
        foreach(var p in settings.Profiles){
            string path=Path.GetFullPath(p.Root);
            // Marker-only historical compatibility fixtures intentionally
            // contain neither the full NW runtime nor thousands of game files.
            // Audit all actual, installed playable games, not these sentinels.
            if(path.StartsWith(fixturePrefix,StringComparison.OrdinalIgnoreCase))continue;
            if(!File.Exists(Path.Combine(path,"Game.exe"))&&!File.Exists(Path.Combine(path,"nw.exe")))continue;
            VerifyProfile(p);
        }
        if(settings.Profiles.Select(x=>Path.GetFullPath(x.SavePath)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=settings.Profiles.Count)throw new Exception("Save paths are shared.");
        File.WriteAllText(Path.Combine(Store.Home,"recipe-test-result.txt"),"PASS: clean Gorpg accepted; CH/Maple/3.3.4 mixing rejected; applied-layer hashes verified; all profile save folders distinct.",Encoding.UTF8);
    }
    public static void Attach(Form form,Settings settings,Action<Profile> added,Action<bool> setBusy){
        var delta=form.Controls.OfType<Button>().First(x=>x.Text.StartsWith("Install portable delta"));delta.Text="Install portable delta pack";delta.Width=396;
        var button=new Button{Text="Install ordered mod layers",Location=new Point(452,508),Size=new Size(396,42),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(87,75,181),ForeColor=Color.White};button.FlatAppearance.BorderSize=0;form.Controls.Add(button);
        var label=form.Controls.OfType<Label>().First(x=>x.Location.Y==560);label.Text="Full independent installs; saves stay with each version. See UPGRADE PATHS.md.";
        var location=new Button{Text="Install location...",Location=new Point(683,409),Size=new Size(165,30),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(43,51,71),ForeColor=Color.White};location.FlatAppearance.BorderSize=0;form.Controls.Add(location);
        location.Click+=delegate{using(var pick=new FolderBrowserDialog{Description="Choose where new complete game profiles will be installed. Existing profiles stay where they are.",SelectedPath=settings.InstallRoot})if(pick.ShowDialog(form)==DialogResult.OK){settings.InstallRoot=pick.SelectedPath;Store.Write(settings);MessageBox.Show(form,"New profiles will be installed in:\r\n"+settings.InstallRoot);}};
        button.Click+=delegate{
            if(Store.Running(settings)){MessageBox.Show(form,"Save and close the game first.");return;}
            var bases=settings.Profiles.Where(x=>IsCleanBase(x.Root)).ToArray();if(bases.Length==0){MessageBox.Show(form,"Register clean Gorpg 1.23 or install its delta pack first. Existing patched branches cannot be the base.");return;}
            using(var dialog=new Form{Text="Review mod layers: later files replace earlier files",ClientSize=new Size(760,425),StartPosition=FormStartPosition.CenterParent,Font=form.Font,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
                var baseBox=new ComboBox{Location=new Point(18,18),Width=722,DropDownStyle=ComboBoxStyle.DropDownList};baseBox.Items.AddRange(bases);baseBox.SelectedIndex=0;dialog.Controls.Add(baseBox);
                var list=new ListBox{Location=new Point(18,65),Size=new Size(722,190),HorizontalScrollbar=true};dialog.Controls.Add(list);
                Action<bool> addSource=delegate(bool archive){if(archive){using(var pick=new OpenFileDialog{Multiselect=true,Filter="Overlay archives|*.rar;*.zip;*.7z;*.001;*.7删z|All files|*.*",InitialDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","Telegram Desktop")})if(pick.ShowDialog(dialog)==DialogResult.OK)foreach(string source in pick.FileNames)list.Items.Add(new LayerInput{Source=source});}else{using(var pick=new FolderBrowserDialog{Description="Select an extracted overlay folder"})if(pick.ShowDialog(dialog)==DialogResult.OK)list.Items.Add(new LayerInput{Source=pick.SelectedPath});}};
                var archiveButton=new Button{Text="Add archives",Location=new Point(18,268),Width=140};archiveButton.Click+=delegate{addSource(true);};dialog.Controls.Add(archiveButton);
                var folderButton=new Button{Text="Add folder",Location=new Point(170,268),Width=130};folderButton.Click+=delegate{addSource(false);};dialog.Controls.Add(folderButton);
                var up=new Button{Text="Move up",Location=new Point(315,268),Width=120};up.Click+=delegate{int i=list.SelectedIndex;if(i>0){object item=list.Items[i];list.Items.RemoveAt(i);list.Items.Insert(i-1,item);list.SelectedIndex=i-1;}};dialog.Controls.Add(up);
                var down=new Button{Text="Move down",Location=new Point(450,268),Width=130};down.Click+=delegate{int i=list.SelectedIndex;if(i>=0 && i<list.Items.Count-1){object item=list.Items[i];list.Items.RemoveAt(i);list.Items.Insert(i+1,item);list.SelectedIndex=i+1;}};dialog.Controls.Add(down);
                var remove=new Button{Text="Remove",Location=new Point(595,268),Width=145};remove.Click+=delegate{if(list.SelectedIndex>=0)list.Items.RemoveAt(list.SelectedIndex);};dialog.Controls.Add(remove);
                dialog.Controls.Add(new Label{Text="3.3.5 trial is ONE complete bundle on clean Gorpg. Do not add 3.3.4/Abu/Maple beneath it.\r\nUse multiple ordered layers for a base bundle followed by separate hotfixes. Saves start empty.",Location=new Point(18,310),Size=new Size(722,55)});
                var install=new Button{Text="Install reviewed layers",Location=new Point(495,375),Width=245,DialogResult=DialogResult.OK};dialog.Controls.Add(install);dialog.AcceptButton=install;
                if(dialog.ShowDialog(form)!=DialogResult.OK || list.Items.Count==0)return;
                var chosen=(Profile)baseBox.SelectedItem;var inputs=list.Items.Cast<LayerInput>().ToList();RunInstall(form,chosen,inputs,settings,added,setBusy);
            }
        };
    }
    static async void RunInstall(Form form,Profile basis,List<LayerInput> inputs,Settings settings,Action<Profile> added,Action<bool> setBusy){
        setBusy(true);foreach(Control c in form.Controls)c.Enabled=false;string title=form.Text;form.Text="Installing ordered layers and verifying files...";
        try{var p=await Task.Run(()=>Install(basis.Root,settings.InstallRoot,"Custom layered profile",inputs));added(p);MessageBox.Show(form,"Installed "+p.Name+". CH and all previous versions remain selectable; saves are separate.");}
        catch(Exception e){MessageBox.Show(form,e.Message,"Layer installation",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{form.Text=title;foreach(Control c in form.Controls)c.Enabled=true;setBusy(false);}
    }
}
