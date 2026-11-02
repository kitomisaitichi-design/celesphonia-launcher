using System;using System.IO;using System.Linq;using System.Reflection;using System.Threading;using System.Collections.Generic;using System.Web.Script.Serialization;
class SplitArchiveTests {
 static List<object> checks=new List<object>();static void Check(string n,bool ok){checks.Add(new{name=n,pass=ok});if(!ok)throw new Exception(n);}
 static int Main(string[] a){AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>Assembly.LoadFrom(a[0]);try{return Run(a);}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static int Run(string[] a){var json=new JavaScriptSerializer{MaxJsonLength=8*1024*1024};var c=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(a[1]));string root=(string)c["root"],hash=(string)c["payloadHash"];bool after=a[2]=="after";
  var expected=new[]{new MixedNativeFile{Path="www/data/payload.bin",Size=6000,Hash=hash}};
  Func<string,MixedRecoveryReport> probe=name=>MixedSourceRecovery.Probe("synthetic-split-check",expected,new[]{Path.Combine(root,name)},CancellationToken.None);
  Check("ordinary ZIP control",probe("plain.zip").Complete);
  string first=Path.Combine(root,"splitzip/sample.zip.001");bool rejected=false;string error=null;MixedRecoveryReport split=null;
  try{split=probe("splitzip/sample.zip.001");}catch(InvalidDataException e){rejected=true;error=e.Message;}catch(IOException e){rejected=true;error=e.Message;}
  if(!after){Check("baseline reproduces split ZIP failure with all parts present",rejected&&error.Contains("Central Directory"));}
  else{
   Check("discovery routes split ZIP through volume reader",FileDiscovery.ArchiveFormat(first)=="7z");Check("split ZIP completes with all parts",split!=null&&split.Complete);
   string destination=Path.Combine(root,"recovered");MixedSourceRecovery.Recover(split,destination,CancellationToken.None);Check("recovered payload hash exact",Store.Hash(Path.Combine(destination,"www/data/payload.bin"))==hash);
   Check("raw first-volume name works",probe("rawparts/sample.001").Complete);Check("uppercase first-volume name works",probe("uppercase/SAMPLE.ZIP.001").Complete);
   Check("single ZIP with misleading extension preserved",probe("plain.mislabelled").Complete);Check("complete single ZIP named first volume works",probe("standalone.zip.001").Complete);
   Check("split 7z control",probe("split7z.7z.001").Complete);
   foreach(string label in new[]{"missing","corrupt","truncated","unsafe","duplicate","symlink"}){
    rejected=false;try{rejected=!probe(label+"/sample.zip.001").Complete;}catch(InvalidDataException){rejected=true;}catch(IOException){rejected=true;}Check(label+" split archive not accepted",rejected);
   }
   foreach(string label in new[]{"unsafe","duplicate","symlink"}){
    rejected=false;try{RarSupport.ProbeFingerprints(Path.Combine(root,label+"/sample.zip.001"),CancellationToken.None);}catch(InvalidDataException){rejected=true;}catch(IOException){rejected=true;}Check(label+" rejected during discovery too",rejected);
   }
   string loose=Path.Combine(root,"native.002");File.Copy(Path.Combine(root,"payload.bin"),loose,true);Check("renamed individual native fragment preserved",MixedSourceRecovery.Probe("synthetic-split-check",expected,new[]{loose},CancellationToken.None).Complete);
   var map=new[]{new MixedNativeFile{Path="www/data/Map027.json",Size=75206,Hash=(string)c["rarMapHash"]}};Check("existing real RAR path preserved",MixedSourceRecovery.Probe("synthetic-rar-map",map,new[]{(string)c["rar"]},CancellationToken.None).Complete);
   var cancelled=new CancellationTokenSource();cancelled.Cancel();rejected=false;try{MixedSourceRecovery.Probe("synthetic-split-check",expected,new[]{first},cancelled.Token);}catch(OperationCanceledException){rejected=true;}Check("cancelled scan stops",rejected);
  }
  var originals=(Dictionary<string,object>)c["sourceHashes"];Check("synthetic source archives unchanged",originals.All(p=>Store.Hash(p.Key)==(string)p.Value));
  File.WriteAllText(a[3],json.Serialize(new{status="PASS",phase=a[2],checks,baselineError=error,userGameOrSaveWrites=false,fullGameplayVerified=false}));Console.WriteLine("PASS "+a[2]+" checks="+checks.Count);return 0;
 }
}
