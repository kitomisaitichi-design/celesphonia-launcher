using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

// Navigation caches never replace fresh installation/integrity checks.
public static class DetectionCache {
    static readonly object gate=new object();
    static readonly Dictionary<string,Tuple<long,long,string>> hashes=new Dictionary<string,Tuple<long,long,string>>(StringComparer.OrdinalIgnoreCase);
    public static string Hash(string file){
        return Hash(file,System.Threading.CancellationToken.None);
    }
    public static string Hash(string file,System.Threading.CancellationToken cancellation){
        cancellation.ThrowIfCancellationRequested();
        var info=new FileInfo(file);if(!info.Exists)return null;long initialLength=info.Length,initialStamp=info.LastWriteTimeUtc.Ticks;
        lock(gate){Tuple<long,long,string> hit;if(hashes.TryGetValue(file,out hit)&&hit.Item1==info.Length&&hit.Item2==info.LastWriteTimeUtc.Ticks)return hit.Item3;}
        string value;
        using(var digest=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(file)){
            var buffer=new byte[1024*1024];int read;
            while((read=stream.Read(buffer,0,buffer.Length))>0){cancellation.ThrowIfCancellationRequested();digest.TransformBlock(buffer,0,read,null,0);}
            cancellation.ThrowIfCancellationRequested();digest.TransformFinalBlock(new byte[0],0,0);value=BitConverter.ToString(digest.Hash).Replace("-","").ToLowerInvariant();
        }
        info.Refresh();if(!info.Exists||info.Length!=initialLength||info.LastWriteTimeUtc.Ticks!=initialStamp)throw new IOException("File changed while it was being checked: "+file);
        lock(gate)hashes[file]=Tuple.Create(initialLength,initialStamp,value);return value;
    }
    public static string Stamp(string file){var f=new FileInfo(file);return f.Exists?f.Length+":"+f.LastWriteTimeUtc.Ticks:"missing";}
}
public static class UpgradeSupport {
    public const string Trial="three-in-one-3-3-5-trial";
    public static string Latest {get{return RecipeSupport.Catalog().Recipes.Where(x=>x.Family=="combined-story"&&x.UpgradeFrom!=null&&x.UpgradeFrom.Length>0).OrderByDescending(x=>x.SaveGeneration).First().Id;}}
    public static Profile StartingProfile(Settings settings,string target){
        var recipe=RecipeSupport.Catalog().Recipes.First(x=>x.Id==target);
        return settings.Profiles.Where(x=>!FileDiscovery.IsNativeProbe(x.Root)&&recipe.UpgradeFrom.Contains(UniversalSupport.Identify(x.Root))).OrderByDescending(x=>RecipeSupport.Catalog().Recipes.First(r=>r.Id==UniversalSupport.Identify(x.Root)).SaveGeneration).FirstOrDefault();
    }
    public static bool IsUpgrade(string id){var r=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==id);return r!=null&&r.UpgradeFrom!=null&&r.UpgradeFrom.Length>0;}
    public static bool CanTransfer(string source,string target){
        if(source==target)return true;
        var a=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==source);var b=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==target);
        if(a!=null&&a.SaveGeneration>100)return b!=null&&b.SaveGeneration>a.SaveGeneration&&b.UpgradeFrom!=null&&b.UpgradeFrom.Contains(source);
        if(b!=null&&b.UpgradeFrom!=null&&b.UpgradeFrom.Length>0)return b.UpgradeFrom.Contains(source);
        return true;
    }
    public static void RequireTransfer(string source,string target){if(!CanTransfer(source,target))throw new IOException("Save migration is forward-only. These saves cannot be copied or restored into the selected older or separate branch. Keep each branch's own saves; 3.3.4 retains its hypnosis route.");}
    public static void ValidateSaveSource(string folder,Profile target){
        string targetId=UniversalSupport.Identify(target.Root);if(targetId==null)return;
        string sourceId=null,record=Path.Combine(folder,"manifest.json");
        if(File.Exists(record)){var data=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(record,Encoding.UTF8));object value;if(data.TryGetValue("version",out value))sourceId=Convert.ToString(value);if(string.IsNullOrEmpty(sourceId)&&data.TryGetValue("originalSavePath",out value)){string path=Convert.ToString(value);var p=Store.Read().Profiles.FirstOrDefault(x=>Path.GetFullPath(x.SavePath).Equals(Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase));if(p!=null)sourceId=UniversalSupport.Identify(p.Root);}}
        if(string.IsNullOrEmpty(sourceId)){var p=Store.Read().Profiles.FirstOrDefault(x=>Path.GetFullPath(x.SavePath).TrimEnd('\\').Equals(Path.GetFullPath(folder).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase));if(p!=null)sourceId=UniversalSupport.Identify(p.Root);}
        if(string.IsNullOrEmpty(sourceId)){var parent=new DirectoryInfo(Path.GetFullPath(folder));for(int depth=0;parent!=null&&depth<4;depth++,parent=parent.Parent){if(File.Exists(Path.Combine(parent.FullName,"package.json"))){sourceId=UniversalSupport.Identify(parent.FullName);break;}}}
        if(string.IsNullOrEmpty(sourceId)&&IsUpgrade(targetId))throw new IOException("Choose saves from a registered compatible source version or a versioned backup. This folder has no verifiable source version.");
        if(!string.IsNullOrEmpty(sourceId))RequireTransfer(sourceId,targetId);
    }
    public static void RequireRoute(string source,string target){
        if(IsUpgrade(source)&&source!=target)RequireTransfer(source,target);
        if(IsUpgrade(target)&&source!=target){var r=RecipeSupport.Catalog().Recipes.First(x=>x.Id==target);if(!r.UpgradeFrom.Contains(source))throw new IOException("Choose a registered compatible source for "+r.Name+". Other branches remain separate.");}
    }
    public static void VerifyBundle(string root,ModRecipe recipe){
        if(recipe.Files==null)return;Store.CheckLinks(root);var actual=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(string file in Directory.GetFiles(root,"*",SearchOption.AllDirectories)){
            string relative=file.Substring(root.TrimEnd('\\').Length+1).Replace('\\','/');
            if(!new[]{"audio","data","fonts","img","js","movies","css","icon"}.Contains(relative.Split('/')[0]))continue;
            if(relative.Split('/').Any(x=>x.Equals("save",StringComparison.OrdinalIgnoreCase))||Store.IsSave(file)||(File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("Unexpected save or linked update path: "+relative);
            actual.Add(relative,Store.Hash(file));
        }
        if(actual.Count!=recipe.Files.Count||recipe.Files.Any(x=>!actual.ContainsKey(x.Key)||actual[x.Key]!=x.Value))throw new IOException("Update contents differ from the verified creator bundle. Choose the intact "+recipe.Name+" archive or its complete extracted folder.");
    }
    public static Profile Install(Profile source,string bundle,string installRoot,string targetId,bool carrySaves){
        string sourceId=UniversalSupport.Identify(source.Root);RequireRoute(sourceId,targetId);
        var recipe=RecipeSupport.Catalog().Recipes.First(x=>x.Id==targetId);if(sourceId==targetId)throw new IOException("Already installed; select this version in Files.");
        if(Store.Running(new Settings{Profiles=new List<Profile>{source}}))throw new IOException("Save and close the source game before upgrading.");
        RecipeSupport.VerifyProfile(source);
        string staging=Store.ArchiveStaging("up");
        try{
        string web=File.Exists(bundle)?Store.Extract(bundle,staging,true):RecipeSupport.ContentRoot(bundle);VerifyBundle(web,recipe);
        string full=Path.GetFullPath(installRoot),sourcePrefix=Path.GetFullPath(source.Root).TrimEnd('\\')+"\\";
        if((full.TrimEnd('\\')+"\\").StartsWith(sourcePrefix,StringComparison.OrdinalIgnoreCase))throw new IOException("Install the upgrade outside the source game folder.");
        Directory.CreateDirectory(full);long need=Store.TreeSize(source.Root)+Store.TreeSize(web)+256L*1024*1024;
        if(new DriveInfo(Path.GetPathRoot(full)).AvailableFreeSpace<need)throw new IOException("Not enough space for an independent upgrade; choose another installation location.");
        string backup=Store.Backup(source);Store.VerifyBackup(backup);
        string root=Store.Child(full,"Upgrade-335-"+recipe.SaveGeneration+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
        Store.CopyTree(source.Root,root,true);string destinationWeb=Store.WebRoot(root);
        foreach(string name in new[]{"launcher-build.json","launcher-reconstruction.json","launcher-base.json","launcher-upgrade.json"}){string path=Store.Child(root,name);if(File.Exists(path))File.Delete(path);}
        var baseline=new Dictionary<string,string>();
        foreach(string file in Directory.GetFiles(root,"*",SearchOption.AllDirectories)){
            string relative=file.Substring(root.Length+1).Replace('\\','/'),original=EnglishSupport.OriginalFile(source.Root,Store.Child(source.Root,relative.Replace('/','\\')));string hash=Store.Hash(original);
            if(Store.Hash(file)!=hash)throw new IOException("Upgrade base verification failed: "+relative);baseline[relative]=hash;
        }
        foreach(var item in recipe.Files){string to=Store.Child(destinationWeb,item.Key.Replace('/','\\'));Directory.CreateDirectory(Path.GetDirectoryName(to));File.Copy(Store.Child(web,item.Key.Replace('/','\\')),to,true);if(Store.Hash(to)!=item.Value)throw new IOException("Update verification failed: "+item.Key);}
        var state=new LayerState{BaseRoot=source.Root,RequiredBase=sourceId,RecipeId=targetId,Components=recipe.Components,StorageMode="Independent verified upgrade; source retained",BaseFiles=baseline,Layers=new List<LayerRecord>{new LayerRecord{Name=recipe.Name,RecipeId=recipe.Id,Files=recipe.Files}}};
        File.WriteAllText(Path.Combine(root,"launcher-build.json"),Store.Json.Serialize(state),new UTF8Encoding(false));
        var p=Store.Detect(root,recipe.Name);Directory.CreateDirectory(p.SavePath);
        if(UniversalSupport.Identify(root)!=targetId)throw new IOException("Unexpected upgrade target build.");
        if(carrySaves){foreach(string file in Store.SaveFiles(backup))File.Copy(file,Path.Combine(p.SavePath,Path.GetFileName(file)));foreach(string file in Store.SaveFiles(p.SavePath))if(Store.Hash(file)!=Store.Hash(Path.Combine(backup,Path.GetFileName(file))))throw new IOException("Migrated save verification failed.");}
        File.WriteAllText(Path.Combine(root,"launcher-upgrade.json"),Store.Json.Serialize(new{sourceVersion=sourceId,targetVersion=targetId,sourceBackup=backup,carriedSaves=carrySaves,reverseAllowed=false}),new UTF8Encoding(false));
        p.Note="Forward upgrade from "+source.Name+"; "+(carrySaves?"saves copied and verified":"empty save set")+"; source retained";
        // An updated game can contain new text not present in the earlier
        // translation catalog. Only carry English if the target has its own
        // verified version-specific pack; never force an older dictionary.
        var english=EnglishSupport.Status(p);
        if(EnglishSupport.Status(source).Enabled && english.Pack!=null && !english.Modified)
            EnglishSupport.Apply(p);
        else if(EnglishSupport.Status(source).Enabled)
            p.Note+="; English display overlay needs a verified "+recipe.Name+" pack";
        RecipeSupport.VerifyProfile(p);return p;
        }finally{if(Directory.Exists(staging)){try{Store.CleanArchiveStaging(staging);}catch(IOException){}catch(UnauthorizedAccessException){}}}
    }
}
