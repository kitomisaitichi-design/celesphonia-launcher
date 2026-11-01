using System;using System.Reflection;using System.IO;using System.Collections;using System.Collections.Generic;using System.Threading;using System.Security.Cryptography;using System.Web.Script.Serialization;
class Map027ManualInputTests {
 static object Get(object obj,string key){return obj.GetType().GetProperty(key).GetValue(obj,null);}
 static int Main(string[] a){
  var j=new JavaScriptSerializer();var c=j.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(a[1]),"map027-case.json")));
  var asm=Assembly.LoadFrom(a[0]);var type=asm.GetType("UpgradeSupport",true);var fn=type.GetMethod("ProbePickedOriginalTarget",BindingFlags.Static|BindingFlags.NonPublic);var checks=new List<object>();
  Action<string,bool> check=(name,ok)=>{checks.Add(new{name,pass=ok});if(!ok)throw new Exception(name);};
  var recovery=asm.GetType("MixedSourceRecovery",true);var native=recovery.GetMethod("LoadEmbeddedReference").Invoke(null,new object[]{"three-in-one-3-3-5-v1-061"});
  var files=(IList)Get(native,"Files");object map=null;foreach(object f in files)if((string)Get(f,"Path")=="www/data/Map027.json")map=f;
  var selected=(IList)Activator.CreateInstance(files.GetType());selected.Add(map);native.GetType().GetProperty("Files").SetValue(native,selected,null);
  string baseWeb=(string)asm.GetType("Store").GetMethod("WebRoot").Invoke(null,new object[]{c["base"]}),baseMap=Path.Combine(baseWeb,"data/Map027.json");
  Func<string[],string,CancellationToken,object> probe=(prior,picked,cancel)=>fn.Invoke(null,new object[]{native,prior,picked,cancel});
  check("base map alone remains unsuitable",!(bool)Get(probe(new[]{baseMap},baseMap,CancellationToken.None),"Complete"));
  check("manually picked creator archive supplies the exact map",(bool)Get(probe(new[]{baseMap},c["archive"],CancellationToken.None),"Complete"));
  string scratch=Path.Combine(Path.GetDirectoryName(a[1]),"map027-picker-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);string file=Path.Combine(scratch,"Map027.fragment");
  var good=File.ReadAllBytes(c["protectedOriginalMap"]);var bad=(byte[])good.Clone();bad[0]^=1;File.WriteAllBytes(file,bad);DateTime stamp=File.GetLastWriteTimeUtc(file);
  bool wrong=!(bool)Get(probe(null,file,CancellationToken.None),"Complete");File.WriteAllBytes(file,good);File.SetLastWriteTimeUtc(file,stamp);
  check("same path/size/timestamp replacement gets a fresh hash",wrong&&(bool)Get(probe(new[]{file},file,CancellationToken.None),"Complete"));
  var duplicate=probe(new[]{file.ToUpperInvariant(),file},file,CancellationToken.None);check("duplicate Windows paths count once",(bool)Get(duplicate,"Complete")&&(int)Get(duplicate,"FoundCount")==1);
  bool cancelled=false;var token=new CancellationTokenSource();token.Cancel();try{probe(new[]{baseMap},c["archive"],token.Token);}catch(TargetInvocationException e){cancelled=e.InnerException is OperationCanceledException;}check("cancelled scan stops before archive work",cancelled);
  bool empty=false;try{probe(null,null,CancellationToken.None);}catch(TargetInvocationException e){empty=e.InnerException is IOException;}check("empty selection is rejected",empty);
  string originalHash;using(var hash=SHA256.Create())originalHash=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(c["protectedOriginalMap"]))).Replace("-","").ToLowerInvariant();
  if(originalHash!=c["protectedOriginalMapHash"])throw new Exception("Original source map changed");
  File.WriteAllText(a[1],j.Serialize(new{status="PASS",checks,originalSourceMapUnchanged=true,artifactRoot=scratch,oneFilePlannerFixture=true,fullTargetProofRequiredByInstaller=true}));Console.WriteLine("PASS manual input checks=6");return 0;
 }
}
