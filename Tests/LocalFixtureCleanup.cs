using System;
using System.IO;
using System.Collections.Generic;
public class LocalFixtureCleanup {
 public static int Main(string[] args){try{
  string root=Path.GetFullPath(args[0]);var removed=new List<string>();
  foreach(var run in LocalTranslation.Runs(root)){
   if(!Path.GetFullPath(run.Root).Equals(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Unexpected fixture root");
   LocalTranslation.Control(run.Id,"pause");LocalTranslation.Delete(run.Id);removed.Add(run.Id);
  }
  File.WriteAllText(args[1],LocalTranslation.Json.Serialize(new{status="PASS",privateRunsRecycled=removed}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
