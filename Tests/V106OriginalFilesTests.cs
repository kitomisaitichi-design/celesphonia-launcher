using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
public static class V106OriginalFilesTests {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
 public static int Main(string[] args){Assembly.LoadFrom(Path.GetFullPath(args[0]));AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>Assembly.LoadFrom(Path.GetFullPath(args[0]));try{return Run(args);}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static int Run(string[] a){
  string id="three-in-one-3-3-5-v1-061",baseRoot=a[1],oldRoot=a[2],archive=a[3],web=a[4],older=a[5],parent=a[6],qa=Path.GetFullPath(a[7]);Directory.CreateDirectory(qa);
  var recipes=RecipeSupport.Catalog();var target=recipes.Recipes.Single(x=>x.Id==id);var previous=recipes.Recipes.Single(x=>x.Id=="three-in-one-3-3-5-v1-053");var reference=MixedSourceRecovery.LoadEmbeddedReference(id);
  Check(reference.Files.Count==11738&&target.Files.Count==5171,"complete latest target and overlay inventories");
  Check(UpgradeTests.ForwardCreatorRoutes()==36,"all 36 forward routes and reverse/cross-branch boundaries");
  Check(RecipeSupport.Identify(web).Id==id&&RecipeSupport.Identify(older).Id=="three-in-one-3-3-5-v1-06","native hashes distinguish 1.06/1.061 despite misleading folder label");
  Check(FileDiscovery.ProbeRar(archive,CancellationToken.None).Any(x=>x.RecipeId==id),"RAR signature detection accepts misleading suffix");
  string flat=Path.Combine(qa,"nested/creator/content"),zipPath=Path.Combine(qa,"partial-originals.update");Directory.CreateDirectory(flat);
  var changed=target.Files.Where(x=>!previous.Files.ContainsKey(x.Key)||previous.Files[x.Key]!=x.Value).Select(x=>x.Key).OrderBy(x=>x).ToArray();
  using(var zip=new ZipArchive(File.Create(zipPath),ZipArchiveMode.Create))for(int i=0;i<changed.Length;i++){
   string source=Path.Combine(web,changed[i].Replace('/',Path.DirectorySeparatorChar));
   if(i<changed.Length/2){var entry=zip.CreateEntry("wrapper/"+Path.GetFileName(source));using(var o=entry.Open())using(var input=File.OpenRead(source))input.CopyTo(o);}
   else File.Copy(source,Path.Combine(flat,Path.GetFileName(source)));
  }
  var mixed=MixedSourceRecovery.Probe(reference,new[]{baseRoot,oldRoot,Path.Combine(qa,"nested"),zipPath},CancellationToken.None);
  Check(mixed.Complete&&mixed.Selections.Any(x=>x.Kind=="zip"),"partial renamed ZIP plus recursive extracted originals and shared bases");
  var individuals=new List<string>{baseRoot,oldRoot};string pieces=Path.Combine(qa,"individuals");Directory.CreateDirectory(pieces);
  for(int i=0;i<changed.Length;i++){string dst=Path.Combine(pieces,"part-"+i+".fragment");File.Copy(Path.Combine(web,changed[i].Replace('/',Path.DirectorySeparatorChar)),dst);individuals.Add(dst);}
  Check(MixedSourceRecovery.Probe(reference,individuals,CancellationToken.None).Complete,"individual exact-hash files need no original names or complete update tree");
  var missing=MixedSourceRecovery.Probe(reference,individuals.Take(individuals.Count-1),CancellationToken.None);
  Check(!missing.Complete&&missing.Missing.Count==1,"missing original reported precisely");
  bool refused=false;string forbidden=Path.Combine(qa,"must-not-install");try{UpgradeSupport.InstallFromOriginalFiles(individuals.Take(individuals.Count-1),forbidden,id,null,CancellationToken.None);}catch(IOException){refused=true;}
  Check(refused&&!Directory.Exists(forbidden),"incomplete sources stop before installation output");
  var settings=Store.Read();var sourceProfile=settings.Profiles.Single(x=>Path.GetFullPath(x.Root).Equals(Path.GetFullPath(oldRoot),StringComparison.OrdinalIgnoreCase));
  var saves=Store.SaveFiles(sourceProfile.SavePath).ToDictionary(x=>x,Store.Hash);string oldSettings=Store.Json.Serialize(settings.Profiles);string theme=settings.LauncherTheme;
  var game=UpgradeSupport.InstallFromOriginalFiles(new[]{baseRoot,archive},parent,id,sourceProfile,CancellationToken.None);
  Check(UniversalSupport.Identify(game.Root)==id,"direct GoRPG plus latest archive installs without intermediate game files");
  RecipeSupport.VerifyProfile(game,false,true);
  Check(saves.All(x=>Store.Hash(x.Key)==x.Value&&Store.Hash(Path.Combine(game.SavePath,Path.GetFileName(x.Key)))==x.Value),"all original saves untouched and carried copies hash-identical");
  Check(game.RuntimeControls==sourceProfile.RuntimeControls,"runtime-control checkbox retained");
  string from=Path.Combine(Store.Home,"Control Sessions",sourceProfile.Id),to=Path.Combine(Store.Home,"Control Sessions",game.Id);
  if(Directory.Exists(Path.Combine(from,"save")))Check(Store.SaveFiles(Path.Combine(from,"save")).All(f=>File.Exists(Path.Combine(to,"save",Path.GetFileName(f)))&&Store.Hash(f)==Store.Hash(Path.Combine(to,"save",Path.GetFileName(f)))),"independent runtime save branch carried without merging native saves");
  if(File.Exists(Path.Combine(from,"panel-settings.json")))Check(Store.Hash(Path.Combine(from,"panel-settings.json"))==Store.Hash(Path.Combine(to,"panel-settings.json")),"panel settings including custom shortcut retained");
  var plan=InstallationRemoval.Plan(game,new Settings{Profiles=new List<Profile>{game},InstallRoot=parent},RemovalMode.InstalledGame);
  Check(plan.CanDeleteFiles&&plan.FileCount>11000,"new native inventory supports managed uninstall preview");
  Check(Store.Json.Serialize(settings.Profiles)==oldSettings&&settings.LauncherTheme==theme,"old profiles and launcher theme retained before registration");
  settings.Profiles.Add(game);settings.SelectedId=game.Id;Store.Write(settings);
  File.WriteAllText(a[8],Store.Json.Serialize(new{status="PASS",checks=checks,game=game,source=sourceProfile,changedOriginalFiles=changed.Length,originalSavesUnchanged=saves.Count,forwardRoutes=36,directGoRpg=true,partialSources=true,uninstallPreview=true,fullGameplayVerified=false}));
  return 0;
 }
}
