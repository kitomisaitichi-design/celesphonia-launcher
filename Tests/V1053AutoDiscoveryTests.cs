using System;
using System.IO;
using System.Linq;
using System.Threading;
public static class AutoV1053Sources {
 public static int Main(string[] args){try{
  string id="three-in-one-3-3-5-v1-053";
  var settings=Store.Json.Deserialize<Settings>(File.ReadAllText(args[0]));
  var found=FileDiscovery.FindTargetArchives(FileDiscovery.NativeRoots(settings),id,CancellationToken.None);
  if(!found.Any(x=>x.Path.Equals(args[1],StringComparison.OrdinalIgnoreCase)))throw new Exception("Automatic preflight missed the creator archive");
  var reference=MixedSourceRecovery.LoadEmbeddedReference(id);
  var pooled=MixedSourceRecovery.Probe(reference,new[]{args[2]}.Concat(found.Select(x=>x.Path)),CancellationToken.None);
  if(!pooled.Complete)throw new Exception("Preflight pool still missing "+pooled.Missing.Count);
  Console.WriteLine("PASS automatic creator preflight from empty discovery state; "+pooled.FoundCount+"/"+pooled.RequiredCount+" exact target files");
  File.WriteAllText(args[3],Store.Json.Serialize(new{status="PASS",initialDiscovered=0,automaticCreatorFound=true,found=pooled.FoundCount,required=pooled.RequiredCount}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
