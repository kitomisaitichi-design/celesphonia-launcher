using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading;using System.Collections.Generic;using System.Diagnostics;using System.Web.Script.Serialization;
class Map027RecoveryBoundaries {
 static List<object> checks=new List<object>();static void Check(string n,bool ok){checks.Add(new{name=n,pass=ok});if(!ok)throw new Exception(n);}
 static bool Rejected(Action act){try{act();return false;}catch(TargetInvocationException e){if(e.InnerException is IOException)return true;throw;}}
 static int Main(string[] a){AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>Assembly.LoadFrom(a[0]);try{return Run(a);}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static int Run(string[] a){
  var j=new JavaScriptSerializer();var c=j.Deserialize<Dictionary<string,string>>(File.ReadAllText(a[1]));string latest="three-in-one-3-3-5-v1-061",old="three-in-one-3-3-5-v1-053";
  var donor=typeof(UpgradeSupport).GetMethod("VerifySaveDonor",BindingFlags.Static|BindingFlags.NonPublic);var install=typeof(UpgradeSupport).GetMethod("InstallVerifiedOriginalFiles",BindingFlags.Static|BindingFlags.NonPublic);
  var source=Store.Detect(c["source"],"Private donor");source.Id="map027-boundary-donor";
  var baseGame=Store.Detect(c["base"],"Private branch check");
  Check("different save branch rejected",Rejected(()=>donor.Invoke(null,new object[]{baseGame,latest,CancellationToken.None})));
  string unknown=Path.Combine(c["caseRoot"],"unknown");Directory.CreateDirectory(unknown);var bogus=new Profile{Root=unknown,SavePath=Path.Combine(unknown,"save"),Exe="Game.exe"};
  Check("unknown source version rejected",Rejected(()=>donor.Invoke(null,new object[]{bogus,latest,CancellationToken.None})));
  string installed=Directory.GetDirectories(Path.Combine(c["caseRoot"],"after-games"),"Upgrade-335-1061-*").Single();var newer=Store.Detect(installed,"Private newer game");
  Check("save downgrade rejected",Rejected(()=>donor.Invoke(null,new object[]{newer,old,CancellationToken.None})));
  var reference=MixedSourceRecovery.LoadEmbeddedReference(latest);var plan=MixedSourceRecovery.Probe(reference,new[]{installed},CancellationToken.None);
  Check("cached full target remains exact",plan.Complete&&plan.RequiredCount==11738);
  string inside=Path.Combine(source.Root,"must-not-install");Check("source/destination overlap rejected",Rejected(()=>install.Invoke(null,new object[]{plan,inside,latest,source,CancellationToken.None}))&&!Directory.Exists(inside));
  string busyDir=Path.Combine(source.Root,"busy-probe");Directory.CreateDirectory(busyDir);string busyExe=Path.Combine(busyDir,"Game.exe");File.Copy(a[2],busyExe,true);
  using(var busy=Process.Start(new ProcessStartInfo(busyExe){UseShellExecute=false,CreateNoWindow=true})){
   try{Thread.Sleep(150);source.Exe="busy-probe/Game.exe";Check("running source rejected",Rejected(()=>donor.Invoke(null,new object[]{source,latest,CancellationToken.None})));}
   finally{if(!busy.HasExited){busy.Kill();busy.WaitForExit();}source.Exe="Game.exe";}
  }
  string from=Path.Combine(Store.Home,"Control Sessions",source.Id);Directory.CreateDirectory(Path.Combine(from,"save"));File.WriteAllText(Path.Combine(from,"save/file2.rpgsave"),"PRIVATE LEGACY CONTROL SAVE");File.WriteAllText(Path.Combine(from,"panel-settings.json"),"{\"hotkey\":\"F12\",\"theme\":\"Midnight\"}");
  newer.Id="map027-boundary-target";RuntimeSupport.CarryForward(source,newer,true);string to=Path.Combine(Store.Home,"Control Sessions",newer.Id);
  Check("legacy control save remains byte-identical",Store.Hash(Path.Combine(from,"save/file2.rpgsave"))==Store.Hash(Path.Combine(to,"save/file2.rpgsave")));
  Check("custom control preferences remain byte-identical",Store.Hash(Path.Combine(from,"panel-settings.json"))==Store.Hash(Path.Combine(to,"panel-settings.json")));
  Check("shared source user map untouched",Store.Hash(c["protectedOriginalMap"])==c["protectedOriginalMapHash"]);
  File.WriteAllText(a[3],j.Serialize(new{status="PASS",checks,privateStateOnly=true}));Console.WriteLine("PASS boundaries checks="+checks.Count);return 0;
 }
}
