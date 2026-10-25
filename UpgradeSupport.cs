using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;

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
// Read-only source selection result. An archive is a candidate until its
// extracted game has passed the full native+creator hash audit.
public sealed class UpgradeSourceChoice {
    public string VersionId {get;internal set;}
    public string Path {get;internal set;}
    public string Kind {get;internal set;}
    public Profile Profile {get;internal set;}
    public int SaveCount {get;internal set;}
    public int SaveGeneration {get;internal set;}
    public bool FullHashVerified {get;internal set;}
    public string Detail {get;internal set;}
}
public static class UpgradeSupport {
    public const string Trial="three-in-one-3-3-5-trial";
    public static string Latest {get{return RecipeSupport.Catalog().Recipes.Where(x=>x.Family=="combined-story"&&x.UpgradeFrom!=null&&x.UpgradeFrom.Length>0).OrderByDescending(x=>x.SaveGeneration).First().Id;}}
    // The creator's full v1.051/v1.052 bundle can be applied directly to an
    // earlier installed 3.3.5 game when the target contains every file in the
    // older full overlay. A delta pack or intermediate version is not needed.
    // Keep this restricted to registered complete creator releases; an unknown
    // v1.05 or a separate CH/Maple/3.3.4 branch is never inferred as compatible.
    static bool IsDirectCreatorOverlay(string source,string target){
        var catalog=RecipeSupport.Catalog();
        var a=catalog.Recipes.FirstOrDefault(x=>x.Id==source);
        var b=catalog.Recipes.FirstOrDefault(x=>x.Id==target);
        if(a==null||b==null||a.Family!="combined-story"||b.Family!=a.Family
            ||a.RequiredBase!="gorpg-1-23"||b.RequiredBase!=a.RequiredBase
            ||!a.CompleteOverlay||!b.CompleteOverlay
            ||a.SaveGeneration<101||a.SaveGeneration>=b.SaveGeneration
            ||b.SaveGeneration<1051
            ||a.Files==null||b.Files==null||a.Files.Count==0||b.Files.Count==0)
            return false;
        return a.Files.Keys.All(path=>b.Files.ContainsKey(path));
    }
    public static bool IsDirect1052(string source,string target){
        return target=="three-in-one-3-3-5-v1-052"&&IsDirectCreatorOverlay(source,target);
    }
    // Reconstruct the FINAL registered target from exact original bytes. Files
    // replaced by the creator overlay need not exist in the original GoRPG set.
    // Compatible progress is backed up separately; cross-branch saves stay out.
    public static Profile InstallFromOriginalFiles(IEnumerable<string> inputs,string installRoot,
                                  string targetId,Profile carrySource,CancellationToken cancellation) {
        var recipe=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==targetId);
        if(recipe==null||recipe.Family!="combined-story"||!recipe.CompleteOverlay)
            throw new IOException("Choose a registered complete creator target.");
        var reference=MixedSourceRecovery.LoadEmbeddedReference(targetId);
        var report=MixedSourceRecovery.Probe(reference,inputs,cancellation,true);
        if(!report.Complete)throw new IOException("Found "+report.FoundCount+"/"+report.RequiredCount+
            " exact files for "+recipe.Name+". Missing "+report.Missing.Count+": "+
            string.Join(", ",report.Missing.Take(8).ToArray())+
            ". Add original archives, folders or the missing individual files. Intermediate versions are not required.");
        string sourceId=null,backup=null,full=Path.GetFullPath(installRoot);
        InstallDestination.RequireSafe(full,report.Selections.Select(x=>x.Source).Concat(carrySource==null?new string[0]:new[]{carrySource.Root}));
        if(carrySource!=null){
            sourceId=UniversalSupport.Identify(carrySource.Root);RequireTransfer(sourceId,targetId);
            if(Store.Running(new Settings{Profiles=new List<Profile>{carrySource}}))
                throw new IOException("Save and close the source game before upgrading.");
            VerifyKnownCreatorSource(carrySource,sourceId,cancellation);
            string prefix=Path.GetFullPath(carrySource.Root).TrimEnd('\\')+"\\";
            if((full.TrimEnd('\\')+"\\").StartsWith(prefix,StringComparison.OrdinalIgnoreCase))
                throw new IOException("Install outside the source game folder.");
            backup=Store.Backup(carrySource);Store.VerifyBackup(backup);
        }
        Directory.CreateDirectory(full);
        string root=Store.Child(full,"Upgrade-335-"+recipe.SaveGeneration+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
        try{
            MixedSourceRecovery.Recover(report,root,cancellation);
            if(UniversalSupport.Identify(root)!=targetId)throw new IOException("Recovered target version does not match.");
            var state=new ReconstructionState{format=2,files=reference.Files.Select(x=>
                new ReconstructionFile{path=x.Path,hash=x.Hash,size=x.Size}).ToList()};
            File.WriteAllText(Path.Combine(root,"launcher-reconstruction.json"),Store.Json.Serialize(state),new UTF8Encoding(false));
            var game=Store.Detect(root,recipe.Name);Directory.CreateDirectory(game.SavePath);
            if(backup!=null)foreach(string file in Store.SaveFiles(backup)){
                string destination=Path.Combine(game.SavePath,Path.GetFileName(file));File.Copy(file,destination);
                if(Store.Hash(destination)!=Store.Hash(file))throw new IOException("Migrated save verification failed.");
            }
            File.WriteAllText(Path.Combine(root,"launcher-upgrade.json"),Store.Json.Serialize(new{
                sourceVersion=sourceId,targetVersion=targetId,sourceBackup=backup,carriedSaves=backup!=null,
                reverseAllowed=false,verifiedOriginalFiles=report.RequiredCount,sourceKind="pooled original files"}),new UTF8Encoding(false));
            if(carrySource!=null){game.RuntimeControls=carrySource.RuntimeControls;RuntimeSupport.CarryForward(carrySource,game,true);}
            game.Note="Independent "+recipe.Name+" from "+report.RequiredCount+" exact original file hashes; "+
                (backup==null?"empty saves":"compatible saves backed up and verified")+"; sources retained";
            return game;
        }catch{
            if(Directory.Exists(root)&&Path.GetDirectoryName(Path.GetFullPath(root)).Equals(full,StringComparison.OrdinalIgnoreCase))
                ExtendedSafeCleanup.RemoveTreeSafely(root);
            throw;
        }
    }
    public static bool MayUpgrade(string source,string target){
        var recipe=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==target);
        return recipe!=null&&recipe.UpgradeFrom!=null&&(recipe.UpgradeFrom.Contains(source)||IsDirectCreatorOverlay(source,target));
    }
    public static Profile StartingProfile(Settings settings,string target){
        return CompatibleSources(settings,target,null,CancellationToken.None).Where(x=>x.Kind=="Installed").Select(x=>x.Profile).FirstOrDefault();
    }
    // Prefer an installed source with real saved progress. Otherwise prefer
    // the most recent intact installed creator build, then a supplied complete
    // game folder, and finally a marker-recognized full-game archive.
    // This discovery is non-mutating; full-file verification happens before
    // migration, after extracting an archive into a private staging folder.
    public static List<UpgradeSourceChoice> CompatibleSources(Settings settings,string target,
                                     IEnumerable<string> originalSources,CancellationToken cancellation){
        if(!IsUpgrade(target))return new List<UpgradeSourceChoice>();
        var recipes=RecipeSupport.Catalog().Recipes.ToDictionary(x=>x.Id,x=>x);
        var results=new List<UpgradeSourceChoice>();
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if(settings!=null&&settings.Profiles!=null)foreach(var profile in settings.Profiles){
            cancellation.ThrowIfCancellationRequested();
            if(profile==null||string.IsNullOrWhiteSpace(profile.Root)||!Directory.Exists(profile.Root)||
               FileDiscovery.IsNativeProbe(profile.Root))continue;
            string full=Path.GetFullPath(profile.Root);
            if(!seen.Add(full))continue;
            string id=UniversalSupport.Identify(full);ModRecipe recipe;
            if(!MayUpgrade(id,target)||!recipes.TryGetValue(id,out recipe))continue;
            results.Add(new UpgradeSourceChoice{Path=full,VersionId=id,Profile=profile,Kind="Installed",
                SaveGeneration=recipe.SaveGeneration,SaveCount=Store.SaveFiles(profile.SavePath).Length,
                FullHashVerified=false,Detail="Registered installed version; creator and native files checked before copying saves"});
        }
        foreach(string input in originalSources??Enumerable.Empty<string>()){
            cancellation.ThrowIfCancellationRequested();
            if(string.IsNullOrWhiteSpace(input))continue;
            string full=Path.GetFullPath(input);
            if(!seen.Add(full))continue;
            if(Directory.Exists(full)){
                string game;
                try{game=UniversalSupport.FindGameRoot(full);}
                catch(IOException){continue;}
                catch(UnauthorizedAccessException){continue;}
                if(!seen.Add(game) && !game.Equals(full,StringComparison.OrdinalIgnoreCase))continue;
                string id=UniversalSupport.Identify(game);ModRecipe recipe;
                if(!MayUpgrade(id,target)||!recipes.TryGetValue(id,out recipe))continue;
                results.Add(new UpgradeSourceChoice{Path=game,VersionId=id,Kind="Game folder",
                    Profile=Store.Detect(game,UniversalSupport.DetectedName(game)??Path.GetFileName(game)),
                    SaveGeneration=recipe.SaveGeneration,SaveCount=0,
                    Detail="Original game folder; complete native and creator checks required before migration"});
            }else if(File.Exists(full)){
                // A matching filename/byte length alone is never evidence.
                // Only archives with the executable, package entry and native
                // marker hashes of a registered full game are considered.
                List<FoundSource> found;
                string format=FileDiscovery.ArchiveFormat(full);
                if(format=="zip")found=FileDiscovery.ProbeZip(full,cancellation);
                else if(format=="rar"||format=="7z")found=FileDiscovery.ProbeRar(full,cancellation);
                else continue;
                foreach(var item in found.Where(x=>x.Kind=="Game archive candidate")){
                    ModRecipe recipe;
                    if(!MayUpgrade(item.RecipeId,target)||!recipes.TryGetValue(item.RecipeId,out recipe))continue;
                    results.Add(new UpgradeSourceChoice{Path=full,VersionId=item.RecipeId,
                        Kind="Full game archive",SaveGeneration=recipe.SaveGeneration,SaveCount=0,
                        Detail="Version markers match; full extraction and every expected native file hash must pass"});
                    break;
                }
            }
        }
        return RankCompatibleSources(results,settings==null?null:settings.SelectedId);
    }
    // Extracted for testing and for future picker integrations. This never
    // confuses a newer empty download with the user's saved installed game.
    public static List<UpgradeSourceChoice> RankCompatibleSources(IEnumerable<UpgradeSourceChoice> choices,
                                                                  string selectedProfileId){
        return (choices??Enumerable.Empty<UpgradeSourceChoice>())
            .Where(x=>x!=null&&x.VersionId!=null&&x.Path!=null)
            .OrderByDescending(x=>x.Kind=="Installed"&&x.SaveCount>0)
            .ThenByDescending(x=>x.Kind=="Installed")
            .ThenByDescending(x=>x.SaveGeneration)
            .ThenByDescending(x=>x.SaveCount)
            .ThenByDescending(x=>x.Profile!=null&&x.Profile.Id==selectedProfileId)
            .ThenBy(x=>x.Path,StringComparer.OrdinalIgnoreCase).ToList();
    }
    public static UpgradeSourceChoice BestCompatibleSource(Settings settings,string target,
                                     IEnumerable<string> originalSources,CancellationToken cancellation){
        var errors=new List<string>();
        foreach(var choice in CompatibleSources(settings,target,originalSources,cancellation)){
            cancellation.ThrowIfCancellationRequested();
            if(choice.Kind=="Full game archive")return choice; // exact source audit follows extraction
            try{
                VerifyKnownCreatorSource(choice.Profile,choice.VersionId,cancellation);
                choice.FullHashVerified=true;
                choice.Detail="Full GoRPG native and creator files passed exact SHA-256 verification; saves can be backed up and carried";
                return choice;
            }catch(IOException error){
                errors.Add(choice.VersionId+" at "+choice.Path+": "+error.Message);
            }catch(UnauthorizedAccessException error){
                errors.Add(choice.VersionId+" at "+choice.Path+": "+error.Message);
            }
        }
        if(errors.Count>0)
            throw new IOException("No installed compatible source passed full native verification. "+
                string.Join(" | ",errors.Take(3).ToArray())+
                ". Choose another original game archive or combine pristine files.");
        return null;
    }
    // A source lacking launcher provenance must still be verified against a
    // complete original GoRPG base plus the EXACT target creator overlay.
    // This prevents marker-only or partially overwritten native game folders
    // from being upgraded as if they were intact.
    public static void VerifyKnownCreatorSource(Profile source,string sourceId,CancellationToken cancellation){
        if(source==null||string.IsNullOrWhiteSpace(source.Root)||UniversalSupport.Identify(source.Root)!=sourceId)
            throw new IOException("Source game does not match the selected creator version.");
        var recipe=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==sourceId);
        if(recipe==null||recipe.Family!="combined-story"||!recipe.CompleteOverlay||
           recipe.RequiredBase!="gorpg-1-23"||recipe.Files==null||recipe.Files.Count==0)
            throw new IOException("The source has no registered complete creator-file hashes; unknown versions cannot be migrated.");
        RecipeSupport.VerifyProfile(source,false,true);
        var clean=MixedSourceRecovery.LoadEmbeddedReference("gorpg-1-23");
        string web=Store.WebRoot(source.Root),nativeRoot=Path.GetFullPath(source.Root);
        foreach(var file in clean.Files){
            cancellation.ThrowIfCancellationRequested();
            string relative=file.Path,overlayPath=relative.StartsWith("www/",StringComparison.OrdinalIgnoreCase)?relative.Substring(4):null;
            if(overlayPath!=null&&recipe.Files.ContainsKey(overlayPath))continue;
            string candidate=Store.Child(nativeRoot,relative.Replace('/','\\'));
            candidate=EnglishSupport.OriginalFile(nativeRoot,candidate);
            if(!File.Exists(candidate)||new FileInfo(candidate).Length!=file.Size||
               Store.Hash(candidate)!=file.Hash)
                throw new IOException("Source is missing original GoRPG native bytes: "+relative+
                    ". Choose a complete original archive or combine pristine sources.");
        }
        foreach(var item in recipe.Files){
            cancellation.ThrowIfCancellationRequested();
            string file=Store.Child(web,item.Key.Replace('/','\\'));
            if(!File.Exists(file)||Store.Hash(file)!=item.Value)
                throw new IOException("Source creator file changed or missing: "+item.Key+
                    ". Recover exact original files from the matching creator archive.");
        }
        // A stray JavaScript/data/asset file can change the playable build even
        // while all marker hashes match. Refuse unregistered native content.
        var approved=new HashSet<string>(clean.Files.Select(x=>x.Path),StringComparer.OrdinalIgnoreCase);
        foreach(string path in recipe.Files.Keys)approved.Add("www/"+path);
        foreach(string directory in new[]{"audio","data","fonts","img","js","movies","css","icon"}){
            string folder=Path.Combine(web,directory);
            if(!Directory.Exists(folder))continue;
            foreach(string file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories)){
                cancellation.ThrowIfCancellationRequested();
                string relative="www/"+file.Substring(web.TrimEnd('\\','/').Length+1).Replace('\\','/');
                if(!approved.Contains(relative))
                    throw new IOException("Source has an unrecognized extra native file: "+relative+
                        ". Remove only after reviewing it, or select a pristine original installation.");
            }
        }
    }
    // Exact mixed-source preflight for a clean GoRPG base. Unidentified,
    // translated and partially overwritten inputs can contribute only bytes
    // matching the embedded native size and SHA-256 manifest.
    public static MixedRecoveryReport PlanMixedGoRpg(IEnumerable<string> inputs,CancellationToken cancellation){
        var paths=(inputs??Enumerable.Empty<string>()).Where(x=>!string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if(paths.Length==0)throw new IOException("Select an original GoRPG archive, an extracted folder, or several mixed sources.");
        var reference=MixedSourceRecovery.LoadEmbeddedReference("gorpg-1-23");
        var report=MixedSourceRecovery.Probe(reference,paths,cancellation);
        if(!report.Complete)
            throw new IOException("Exact GoRPG recovery needs "+report.RequiredCount+" native files; found "+
                report.FoundCount+", missing "+report.Missing.Count+" (e.g. "+
                string.Join(", ",report.Missing.Take(8).ToArray())+
                "). Add original game archives or folders containing the missing pristine bytes.");
        return report;
    }
    // Upgrade from an original full-game ZIP/RAR/7z containing a known earlier
    // creator installation. Extraction is private and temporary; the resulting
    // target is independent. If explicitly requested, archived saves are
    // backed up and copied by the same verified migration as installed saves.
    public static Profile InstallFromGameArchive(string originalArchive,string creatorBundle,
                                      string installRoot,string targetId,bool carryArchivedSaves){
        InstallDestination.RequireSafe(installRoot,new[]{creatorBundle});
        if(!File.Exists(originalArchive))
            throw new IOException("Original full-game archive is missing: "+originalArchive);
        if(!IsUpgrade(targetId))
            throw new IOException("Choose a registered creator update as the destination.");
        string full=Path.GetFullPath(installRoot);
        string token=Guid.NewGuid().ToString("N");
        string stage=Path.Combine(full,".cph-upgrade-archive-"+token);
        try{
            // Marker recognition is preliminary: original files must pass a
            // complete native + creator SHA-256 check after extraction.
            var sources=CompatibleSources(null,targetId,new[]{originalArchive},CancellationToken.None);
            var choice=sources.FirstOrDefault(x=>x.Kind=="Full game archive");
            if(choice==null)
                throw new IOException("The archive is not a recognized earlier full game for "+targetId+
                    ". The unknown version was not guessed from its filename.");
            Directory.CreateDirectory(full);
            var source=UniversalSupport.InstallOriginal(originalArchive,stage,choice.VersionId);
            VerifyKnownCreatorSource(source,choice.VersionId,CancellationToken.None);
            var installed=Install(source,creatorBundle,full,targetId,carryArchivedSaves);
            string audit=Path.Combine(installed.Root,"launcher-upgrade.json");
            var provenance=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(audit,Encoding.UTF8));
            provenance["sourceKind"]="verified full original game archive";
            provenance["sourceArchive"]=Path.GetFullPath(originalArchive);
            provenance["sourceArchiveBytes"]=new FileInfo(originalArchive).Length;
            provenance["nativeAndCreatorHashesVerified"]=true;
            provenance["temporaryExtractedSourceRemoved"]=true;
            File.WriteAllText(audit,Store.Json.Serialize(provenance),new UTF8Encoding(false));
            installed.Note="Forward upgrade from complete original "+choice.VersionId+
                " game archive; creator native hashes verified; "+
                (carryArchivedSaves?"archived saves copied and backed up":"new empty save set")+
                "; original archive retained";
            return installed;
        }finally{
            if(Path.GetDirectoryName(Path.GetFullPath(stage)).Equals(full,StringComparison.OrdinalIgnoreCase)
               &&Path.GetFileName(stage)==".cph-upgrade-archive-"+token&&Directory.Exists(stage))
                ExtendedSafeCleanup.RemoveTreeSafely(stage);
        }
    }
    // Fresh target from mixed original archives/folders. A missing GoRPG file
    // fails before any installation directory is created. This intentionally
    // carries no unrelated source-branch saves into a new 3.3.5 game.
    public static Profile InstallFromMixedGoRpg(IEnumerable<string> inputs,string creatorBundle,
                                      string installRoot,string targetId,CancellationToken cancellation){
        var recipe=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==targetId);
        if(recipe==null||recipe.Family!="combined-story"||recipe.RequiredBase!="gorpg-1-23"||
           !recipe.CompleteOverlay||recipe.Files==null||recipe.Files.Count==0)
            throw new IOException("This target needs a registered complete GoRPG creator update.");
        var report=PlanMixedGoRpg(inputs,cancellation);
        string full=Path.GetFullPath(installRoot);
        InstallDestination.RequireSafe(full,report.Selections.Select(x=>x.Source).Concat(new[]{creatorBundle}));
        Directory.CreateDirectory(full);
        string token=Guid.NewGuid().ToString("N");
        string stage=Path.Combine(full,".cph-upgrade-gorpg-"+token);
        try{
            MixedSourceRecovery.Recover(report,stage,cancellation);
            RecipeSupport.ValidateBase(stage);
            var installed=RecipeSupport.Install(stage,full,recipe.Name,
                new List<LayerInput>{new LayerInput{Source=creatorBundle}});
            if(UniversalSupport.Identify(installed.Root)!=targetId)
                throw new IOException("Creator installation differs from the selected destination version.");
            RecipeSupport.VerifyProfile(installed);
            installed.Note="Fresh "+recipe.Name+" from "+report.FoundCount+
                " exact GoRPG SHA-256 source files; separate empty saves";
            return installed;
        }finally{
            if(Path.GetDirectoryName(Path.GetFullPath(stage)).Equals(full,StringComparison.OrdinalIgnoreCase)
               &&Path.GetFileName(stage)==".cph-upgrade-gorpg-"+token&&Directory.Exists(stage))
                ExtendedSafeCleanup.RemoveTreeSafely(stage);
        }
    }
    public static bool IsUpgrade(string id){var r=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==id);return r!=null&&r.UpgradeFrom!=null&&r.UpgradeFrom.Length>0;}
    public static bool CanTransfer(string source,string target){
        if(string.IsNullOrEmpty(source)||string.IsNullOrEmpty(target))return false;
        var catalog=RecipeSupport.Catalog();
        var a=catalog.Recipes.FirstOrDefault(x=>x.Id==source);
        var b=catalog.Recipes.FirstOrDefault(x=>x.Id==target);
        if(source==target)return a!=null||source=="gorpg-1-23"||source=="ch-current";
        // Separate game branches keep their own save sets. Only a registered
        // forward 3.3.5 update (or a verified full-overlay shortcut) migrates.
        if(b==null||b.Family!="combined-story"||b.UpgradeFrom==null)return false;
        if(a==null||a.Family!="combined-story"||a.SaveGeneration>=b.SaveGeneration)return false;
        return b.UpgradeFrom.Contains(source)||IsDirectCreatorOverlay(source,target);
    }
    public static void RequireTransfer(string source,string target){if(!CanTransfer(source,target))throw new IOException("Save migration is forward-only. These saves cannot be copied or restored into the selected older or separate branch. Keep each branch's own saves; 3.3.4 retains its hypnosis route.");}
    public static void ValidateSaveSource(string folder,Profile target){
        // Standalone fixtures have no game root. A real destination whose
        // version cannot be identified must never accept cross-branch saves.
        if(string.IsNullOrEmpty(target.Root))return;
        string targetId=UniversalSupport.Identify(target.Root);
        if(targetId==null)throw new IOException("Cannot verify the destination game version. Register an intact game before copying or restoring saves.");
        string sourceId=null,record=Path.Combine(folder,"manifest.json");
        if(File.Exists(record)){var data=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(record,Encoding.UTF8));object value;if(data.TryGetValue("version",out value))sourceId=Convert.ToString(value);if(string.IsNullOrEmpty(sourceId)&&data.TryGetValue("originalSavePath",out value)){string path=Convert.ToString(value);var p=Store.Read().Profiles.FirstOrDefault(x=>Path.GetFullPath(x.SavePath).Equals(Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase));if(p!=null)sourceId=UniversalSupport.Identify(p.Root);}}
        if(string.IsNullOrEmpty(sourceId)){var p=Store.Read().Profiles.FirstOrDefault(x=>Path.GetFullPath(x.SavePath).TrimEnd('\\').Equals(Path.GetFullPath(folder).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase));if(p!=null)sourceId=UniversalSupport.Identify(p.Root);}
        if(string.IsNullOrEmpty(sourceId)){var parent=new DirectoryInfo(Path.GetFullPath(folder));for(int depth=0;parent!=null&&depth<4;depth++,parent=parent.Parent){if(File.Exists(Path.Combine(parent.FullName,"package.json"))){sourceId=UniversalSupport.Identify(parent.FullName);break;}}}
        if(string.IsNullOrEmpty(sourceId))throw new IOException("Choose saves from a registered compatible source version or a versioned backup. This folder has no verifiable source version.");
        RequireTransfer(sourceId,targetId);
    }
    public static void RequireRoute(string source,string target){
        if(IsUpgrade(source)&&source!=target)RequireTransfer(source,target);
        if(IsUpgrade(target)&&source!=target){var r=RecipeSupport.Catalog().Recipes.First(x=>x.Id==target);if(!MayUpgrade(source,target))throw new IOException("Choose a registered compatible source for "+r.Name+". Other branches remain separate.");}
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
        InstallDestination.RequireSafe(installRoot,new[]{source.Root,bundle});
        var recipe=RecipeSupport.Catalog().Recipes.First(x=>x.Id==targetId);if(sourceId==targetId)throw new IOException("Already installed; select this version in Files.");
        if(Store.Running(new Settings{Profiles=new List<Profile>{source}}))throw new IOException("Save and close the source game before upgrading.");
        // Recover intact native files from a partial/stale English sidecar,
        // but never approve modified native scripts or an unverifiable index.
        VerifyKnownCreatorSource(source,sourceId,CancellationToken.None);
        string staging=Store.ArchiveStaging("up");
        string createdRoot=null;
        try{
        string web=File.Exists(bundle)?Store.Extract(bundle,staging,true):RecipeSupport.ContentRoot(bundle);VerifyBundle(web,recipe);
        string full=Path.GetFullPath(installRoot),sourcePrefix=Path.GetFullPath(source.Root).TrimEnd('\\')+"\\";
        if((full.TrimEnd('\\')+"\\").StartsWith(sourcePrefix,StringComparison.OrdinalIgnoreCase))throw new IOException("Install the upgrade outside the source game folder.");
        Directory.CreateDirectory(full);long need=Store.TreeSize(source.Root)+Store.TreeSize(web)+256L*1024*1024;
        if(new DriveInfo(Path.GetPathRoot(full)).AvailableFreeSpace<need)throw new IOException("Not enough space for an independent upgrade; choose another installation location.");
        string backup=Store.Backup(source);Store.VerifyBackup(backup);
        string root=Store.Child(full,"Upgrade-335-"+recipe.SaveGeneration+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
        if(Directory.Exists(root)||File.Exists(root))throw new IOException("Refusing to overwrite an existing game directory.");
        createdRoot=root;
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
        p.RuntimeControls=source.RuntimeControls;
        RecipeSupport.VerifyProfile(p);RuntimeSupport.CarryForward(source,p,carrySaves);return p;
        }catch{
            if(createdRoot!=null&&Directory.Exists(createdRoot)&&
               Path.GetDirectoryName(Path.GetFullPath(createdRoot)).Equals(
                   Path.GetFullPath(installRoot).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase)&&
               System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(createdRoot),
                   @"^Upgrade-335-[0-9]+-[a-f0-9]{8}$")){
                try{ExtendedSafeCleanup.RemoveTreeSafely(createdRoot);}
                catch(IOException){}catch(UnauthorizedAccessException){}
            }
            throw;
        }finally{if(Directory.Exists(staging)){try{Store.CleanArchiveStaging(staging);}catch(IOException){}catch(UnauthorizedAccessException){}}}
    }
}
