using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading;using System.Collections.Generic;using System.Web.Script.Serialization;
class Map027UpgradeRecoveryTests {
 static List<object> checks=new List<object>();static void Check(string n,bool pass){checks.Add(new{name=n,pass});if(!pass)throw new Exception(n);}
 static int Main(string[] args){AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>Assembly.LoadFrom(args[0]);try{return Run(args);}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static int Run(string[] a){
  var json=new JavaScriptSerializer{MaxJsonLength=32*1024*1024};var c=json.Deserialize<Dictionary<string,string>>(File.ReadAllText(a[1]));
  string root=c["source"],target="three-in-one-3-3-5-v1-061",old="three-in-one-3-3-5-v1-053",map=Path.Combine(root,"www/data/Map027.json");bool fixedBuild=a[2]=="after";
  var profile=Store.Detect(root,"Private old-source recovery check");profile.Id="map027-private-source";profile.RuntimeControls=true;
  Check("private launcher home",Store.Home.Equals(c["home"],StringComparison.OrdinalIgnoreCase));
  File.Copy(c["gold"],map,true);Check("genuine older native version identified",UniversalSupport.Identify(root)==old);
  UpgradeSupport.VerifyKnownCreatorSource(profile,old,CancellationToken.None);Check("intact private source passes strict native validation",true);
  Directory.CreateDirectory(profile.SavePath);File.WriteAllText(Path.Combine(profile.SavePath,"file1.rpgsave"),"PRIVATE SYNTHETIC SAVE FOR BYTE-COPY TEST");var saveHash=Store.Hash(Path.Combine(profile.SavePath,"file1.rpgsave"));
  var settings=new Settings{Profiles=new List<Profile>{profile},SelectedId=profile.Id};
  File.Copy(Path.Combine(Store.WebRoot(c["base"]),"data/Map027.json"),map,true);
  var inputs=new[]{root,c["base"],c["archive"]};var reference=MixedSourceRecovery.LoadEmbeddedReference(target);
  var report=MixedSourceRecovery.Probe(reference,inputs,CancellationToken.None,true);
  Check("all final native bytes available despite bad donor map",report.Complete&&report.RequiredCount==11738);
  Check("correct target map selected from creator archive",report.Selections.Single(x=>x.Path=="www/data/Map027.json").Kind=="7z");
  bool rejected=false;try{UpgradeSupport.VerifyKnownCreatorSource(profile,old,CancellationToken.None);}catch(IOException e){rejected=e.Message.Contains("Source creator file changed or missing: data/Map027.json");}
  Check("strict native-source verification still rejects changed map",rejected);
  string output=Path.Combine(c["caseRoot"],fixedBuild?"after-games":"before-games");
  if(!fixedBuild){
   rejected=false;try{UpgradeSupport.BestCompatibleSource(settings,target,null,CancellationToken.None);}catch(IOException e){rejected=e.Message.Contains("No installed compatible source")&&e.Message.Contains("Map027");}Check("conversion source selection reproduces reported failure",rejected);
   rejected=false;try{UpgradeSupport.InstallFromOriginalFiles(inputs,output,target,profile,CancellationToken.None);}catch(IOException e){rejected=e.Message.Contains("Source creator file changed or missing: data/Map027.json");}Check("creator-bundle route reproduces reported failure",rejected&&!Directory.Exists(output));
  }else{
   var policy=typeof(UpgradeSupport).GetMethod("UseOriginalTargetRecovery",BindingFlags.Static|BindingFlags.NonPublic);
   Check("conversion button selects complete original-target recovery",(bool)policy.Invoke(null,new object[]{0,false,report}));
   Check("creator-bundle button selects the same complete recovery",(bool)policy.Invoke(null,new object[]{1,false,report}));
   Check("existing-game import is not rerouted",!(bool)policy.Invoke(null,new object[]{2,true,report}));
   var missing=MixedSourceRecovery.Probe(reference,new[]{Path.Combine(Store.WebRoot(c["base"]),"data/Map027.json")},CancellationToken.None);
   Check("conversion without complete originals keeps strict source path",!(bool)policy.Invoke(null,new object[]{0,false,missing}));
   var complete=typeof(UpgradeSupport).GetMethod("InstallVerifiedOriginalFiles",BindingFlags.Static|BindingFlags.NonPublic);
   string refusedOutput=Path.Combine(c["caseRoot"],"missing-target-must-not-exist");rejected=false;
   try{complete.Invoke(null,new object[]{missing,refusedOutput,target,profile,CancellationToken.None});}catch(TargetInvocationException e){rejected=e.InnerException is IOException&&e.InnerException.Message.Contains("Missing");}
   Check("missing/wrong target bytes fail before any destination write",rejected&&!Directory.Exists(refusedOutput));
   var subset=MixedSourceRecovery.Probe(target,reference.Files.Where(x=>x.Path=="www/data/Map027.json"),new[]{c["gold"]},CancellationToken.None);
   // Loose single files can be selected by exact bytes even with a changed name.
   Check("subset fixture has an exact map but lacks the complete native target",subset.Complete&&subset.RequiredCount==1);
   rejected=false;try{complete.Invoke(null,new object[]{subset,refusedOutput,target,profile,CancellationToken.None});}catch(TargetInvocationException e){rejected=e.InnerException is IOException&&e.InnerException.Message.Contains("authoritative");}
   Check("partial proof cannot masquerade as a complete target",rejected&&!Directory.Exists(refusedOutput));
   var game=UpgradeSupport.InstallFromOriginalFiles(inputs,output,target,profile,CancellationToken.None);
   Check("independent target version installed",UniversalSupport.Identify(game.Root)==target);
   var audit=MixedSourceRecovery.Probe(reference,new[]{game.Root},CancellationToken.None,true);Check("all 11738 installed native hashes exact",audit.Complete);
   Check("normal save copied without changing source",Store.Hash(Path.Combine(profile.SavePath,"file1.rpgsave"))==saveHash&&Store.Hash(Path.Combine(game.SavePath,"file1.rpgsave"))==saveHash);
   Check("runtime controls preference carried",game.RuntimeControls);
   Check("source map left unchanged",Store.Hash(map)!=Store.Hash(c["gold"]));
   File.Delete(map);var donor=typeof(UpgradeSupport).GetMethod("VerifySaveDonor",BindingFlags.Static|BindingFlags.NonPublic);
   Check("missing old map remains eligible only as a save donor",Convert.ToString(donor.Invoke(null,new object[]{profile,target,CancellationToken.None}))==old);
   File.Copy(c["gold"],map);
   var state=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(game.Root,"launcher-upgrade.json")));Check("reconstruction provenance records old save donor",Convert.ToString(state["sourceVersion"])==old&&Convert.ToInt32(state["verifiedOriginalFiles"])==11738);
   Check("unknown/cross-branch/downgrade save boundaries preserved",!UpgradeSupport.CanTransfer("maple-1-7-2",target)&&!UpgradeSupport.CanTransfer(target,old)&&!UpgradeSupport.CanTransfer(null,target));
  }
  Check("user native source map unchanged",Store.Hash(c["protectedOriginalMap"])==c["protectedOriginalMapHash"]);
  File.WriteAllText(a[3],json.Serialize(new{status="PASS",checks,phase=a[2],sourceVersion=old,targetVersion=target,exactReporter1052Replay=false,fullGameplayVerified=false}));
  Console.WriteLine("PASS "+a[2]+" checks="+checks.Count);return 0;
 }
}
