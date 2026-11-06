using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
public static class V107OriginalFilesTests {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
 public static int Main(string[] args){Assembly.LoadFrom(Path.GetFullPath(args[0]));AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>Assembly.LoadFrom(Path.GetFullPath(args[0]));try{return Run(args);}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static int Run(string[] a){
  string id="three-in-one-3-3-5-v1-07",baseRoot=a[1],oldRoot=a[2],archive=a[3],web=a[4],parent=a[5],qa=Path.GetFullPath(a[6]);Directory.CreateDirectory(qa);
  var recipes=RecipeSupport.Catalog();var target=recipes.Recipes.Single(x=>x.Id==id);var previous=recipes.Recipes.Single(x=>x.Id=="three-in-one-3-3-5-v1-061");var reference=MixedSourceRecovery.LoadEmbeddedReference(id);
  Check(reference.Files.Count==11732&&target.Files.Count==5165,"exact 1.07 native and creator inventories");
  var earlier=recipes.Recipes.Where(x=>x.Id!=id&&x.Family=="combined-story"&&x.CompleteOverlay&&x.SaveGeneration<1070).ToArray();
  Check(earlier.Length==10,"all ten earlier 3.3.5 releases including trial indexed");
  foreach(var source in earlier){Check(UpgradeSupport.MayUpgrade(source.Id,id)&&UpgradeSupport.CanTransfer(source.Id,id)&&!UpgradeSupport.MayUpgrade(id,source.Id)&&!UpgradeSupport.CanTransfer(id,source.Id),"forward upgrade/save route and reverse rejection: "+source.Id);UpgradeSupport.RequireRoute(source.Id,id);}
  foreach(string other in new[]{"gorpg-1-23","maple-1-7-2","three-in-one-3-3-4","ch-current","three-in-one-3-3-5-v1-05"})Check(!UpgradeSupport.CanTransfer(other,id)&&!UpgradeSupport.MayUpgrade(other,id),"separate/unknown save family remains excluded: "+other);
  Check(UpgradeTests.ForwardCreatorRoutes()==36,"all 36 historical direct routes preserved");
  Check(RecipeSupport.Identify(web).Id==id&&UpgradeSupport.Latest==id&&LauncherUpdate.CurrentVersionMessage("repo").Contains("Ver1.07"),"native content detection and current update notice show 1.07");
  Check(FileDiscovery.ProbeRar(archive,CancellationToken.None).Any(x=>x.RecipeId==id),"original RAR detected despite misleading extension");
  string flat=Path.Combine(qa,"nested/creator/content"),zipPath=Path.Combine(qa,"partial-originals.update");Directory.CreateDirectory(flat);
  var changed=target.Files.Where(x=>!previous.Files.ContainsKey(x.Key)||previous.Files[x.Key]!=x.Value).Select(x=>x.Key).OrderBy(x=>x).ToArray();
  using(var zip=new ZipArchive(File.Create(zipPath),ZipArchiveMode.Create))for(int i=0;i<changed.Length;i++){
   string source=Path.Combine(web,changed[i].Replace('/',Path.DirectorySeparatorChar));
   if(i<changed.Length/2){var entry=zip.CreateEntry("wrapper/"+Path.GetFileName(source));using(var o=entry.Open())using(var input=File.OpenRead(source))input.CopyTo(o);}
   else File.Copy(source,Path.Combine(flat,Path.GetFileName(source)));
  }
  var mixed=MixedSourceRecovery.Probe(reference,new[]{baseRoot,oldRoot,Path.Combine(qa,"nested"),zipPath},CancellationToken.None);
  Check(mixed.Complete&&mixed.RequiredCount==11732&&mixed.Selections.Any(x=>x.Kind=="zip"),"all final hashes recovered from partial renamed ZIP, recursive files and shared bases");
  var individuals=new List<string>{baseRoot,oldRoot};string pieces=Path.Combine(qa,"individuals");Directory.CreateDirectory(pieces);
  for(int i=0;i<changed.Length;i++){string dst=Path.Combine(pieces,"part-"+i+".fragment");File.Copy(Path.Combine(web,changed[i].Replace('/',Path.DirectorySeparatorChar)),dst);individuals.Add(dst);}
  Check(MixedSourceRecovery.Probe(reference,individuals,CancellationToken.None).Complete,"renamed individual originals also complete the full target");
  var missing=MixedSourceRecovery.Probe(reference,individuals.Take(individuals.Count-1),CancellationToken.None);
  Check(!missing.Complete&&missing.Missing.Count==1,"missing exact original remains actionable");
  bool refused=false;string forbidden=Path.Combine(qa,"must-not-install");try{UpgradeSupport.InstallFromOriginalFiles(individuals.Take(individuals.Count-1),forbidden,id,null,CancellationToken.None);}catch(IOException){refused=true;}
  Check(refused&&!Directory.Exists(forbidden),"incomplete target rejected before installation output");
  var settings=Store.Read();var sourceProfile=settings.Profiles.Single(x=>Path.GetFullPath(x.Root).Equals(Path.GetFullPath(oldRoot),StringComparison.OrdinalIgnoreCase));
  var saves=Store.SaveFiles(sourceProfile.SavePath).ToDictionary(x=>x,Store.Hash);string oldProfiles=Store.Json.Serialize(settings.Profiles);string theme=settings.LauncherTheme;
  var game=UpgradeSupport.InstallFromOriginalFiles(new[]{baseRoot,archive},parent,id,sourceProfile,CancellationToken.None);
  Check(UniversalSupport.Identify(game.Root)==id,"direct GoRPG plus 1.07 RAR installed without intermediate versions");RecipeSupport.VerifyProfile(game,false,true);
  Check(saves.All(x=>Store.Hash(x.Key)==x.Value&&Store.Hash(Path.Combine(game.SavePath,Path.GetFileName(x.Key)))==x.Value),"source saves preserved and carried copies hash-identical");
  Check(game.RuntimeControls==sourceProfile.RuntimeControls,"runtime controls preference preserved");
  string from=Path.Combine(Store.Home,"Control Sessions",sourceProfile.Id),to=Path.Combine(Store.Home,"Control Sessions",game.Id);
  if(Directory.Exists(Path.Combine(from,"save")))Check(Store.SaveFiles(Path.Combine(from,"save")).All(f=>File.Exists(Path.Combine(to,"save",Path.GetFileName(f)))&&Store.Hash(f)==Store.Hash(Path.Combine(to,"save",Path.GetFileName(f)))),"separate runtime branch copied without merging saves");
  var plan=InstallationRemoval.Plan(game,new Settings{Profiles=new List<Profile>{game},InstallRoot=parent},RemovalMode.InstalledGame);
  Check(plan.CanDeleteFiles&&plan.FileCount>=11732,"managed uninstall recognizes new native reference");
  Check(Store.Json.Serialize(settings.Profiles)==oldProfiles&&settings.LauncherTheme==theme,"existing profiles and theme retained before registration");
  settings.Profiles.Add(game);settings.SelectedId=game.Id;Store.Write(settings);
  File.WriteAllText(a[7],Store.Json.Serialize(new{status="PASS",checks=checks,game=game,source=sourceProfile,changedOriginalFiles=changed.Length,originalSavesUnchanged=saves.Count,forwardRoutes=earlier.Length,directGoRpg=true,mixedTargetHashPlanning=true,partialSources=true,uninstallPreview=true,fullGameplayVerified=false}));return 0;
 }
}
