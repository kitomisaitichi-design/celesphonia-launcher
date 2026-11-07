using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
public class LocalModelRemoval {
 public static int Main(string[] args){string backup=args[0];var model=LocalTranslation.Model("0.8b");try{
  if(Directory.Exists(backup))throw new Exception("Use a fresh retained-model directory");Directory.CreateDirectory(backup);
  foreach(var file in Directory.GetFiles(model.Folder))File.Copy(file,Path.Combine(backup,Path.GetFileName(file)));
  var runs=LocalTranslation.Runs(null).ToDictionary(r=>r.Id,r=>File.Exists(LocalTranslation.RunFile(r.Id,"results.jsonl"))?Store.Hash(LocalTranslation.RunFile(r.Id,"results.jsonl")):"");
  LocalTranslation.Uninstall("0.8b");if(Directory.Exists(model.Folder))throw new Exception("Model folder still present");
  foreach(var pair in runs)if(pair.Value!=""&&Store.Hash(LocalTranslation.RunFile(pair.Key,"results.jsonl"))!=pair.Value)throw new Exception("Cached run changed");
  Directory.Move(backup,model.Folder);LocalTranslation.Install("0.8b",false,CancellationToken.None);
  if(Store.Hash(model.PathName)!=model.Hash)throw new Exception("Restored model hash differs");
  File.WriteAllText(args[1],LocalTranslation.Json.Serialize(new{status="PASS",realModelRemoved=true,cachedRunsPreserved=runs.Count,modelRestoredWithoutRedownload=true}));Console.WriteLine("PASS: real model removal keeps caches; model restored without download");return 0;
 }catch(Exception e){if(!Directory.Exists(model.Folder)&&Directory.Exists(backup))Directory.Move(backup,model.Folder);Console.Error.WriteLine(e);return 1;}}
}
