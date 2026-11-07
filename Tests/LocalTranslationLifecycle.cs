using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
public static class LocalTranslationLifecycle {
 static List<string> checks=new List<string>();static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);Console.WriteLine("PASS "+text);}
 static LocalRun Wait(string id,Func<LocalRun,bool> predicate,int seconds){var clock=Stopwatch.StartNew();while(clock.Elapsed.TotalSeconds<seconds){var r=LocalTranslation.Run(id);if(predicate(r))return r;if(r.State=="Needs attention")throw new Exception(r.Error);Thread.Sleep(150);}throw new Exception("Run wait expired: "+LocalTranslation.Run(id).State);}
 public static int Main(string[] args){try{
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(Path.Combine(root,"www/data"));Directory.CreateDirectory(Path.Combine(root,"www/js"));File.WriteAllText(Path.Combine(root,"package.json"),"{\"main\":\"www/index.html\"}");string native="<!doctype html><html><head></head><body><script src=\"js/main.js\"></script></body></html>";File.WriteAllText(Path.Combine(root,"www/index.html"),native);File.WriteAllText(Path.Combine(root,"www/js/main.js"),"");
  var items=new List<object>{null};for(int i=1;i<=12;i++)items.Add(new{id=i,name="回復薬"+i,description="HPを"+(40+i)+"回復する。"});LocalTranslation.Write(Path.Combine(root,"www/data/Items.json"),items);
  var profile=new Profile{Id="local-lifecycle",Name="Private local translation fixture",Root=root,Exe="Game.exe",SavePath=Path.Combine(root,"save")};
  bool rejected=false;try{LocalTranslation.Create(profile,"","0.8b","en",new string[0]);}catch(IOException){rejected=true;}Check(rejected,"empty scope rejected before run creation");
  var run=LocalTranslation.Create(profile,"Lifecycle fixture","0.8b","en",new[]{"Descriptions"});var index=LocalTextIndex.Build(run,()=>false);Check(index.Count==12&&run.Excluded==12,"native source index and excluded names");LocalTranslation.Save(run);
  LocalTranslation.Control(run.Id,"run");Wait(run.Id,r=>r.Done>0,90);LocalTranslation.Control(run.Id,"pause");var paused=Wait(run.Id,r=>r.State=="Paused"||r.State.StartsWith("Completed"),30);int saved=paused.Done;Check(saved>0,"pause checkpoints completed real model results");
  LocalTranslation.Select(profile,run.Id,true);Check(LocalTranslation.Binding(root).Enabled&&File.ReadAllText(Path.Combine(root,"www/index.html")).Contains("celesphonia-local/runtime.js"),"partial paused cache selectable without English pack");Check(File.ReadAllText(LocalTranslation.BaseFile(root,Path.Combine(root,"www/index.html")))==native,"local-only original entry recovery");
  LocalTranslation.Select(profile,run.Id,false);LocalTranslation.Prepare(profile);Check(File.ReadAllText(Path.Combine(root,"www/index.html"))==native,"disable and repeated prepare restore exact native entry");
  LocalTranslation.Control(run.Id,"run");var done=Wait(run.Id,r=>r.State.StartsWith("Completed"),120);Check(done.Done==12&&done.Failed==0&&done.Done>=saved,"resume retains earlier work and completes remaining requests");
  var copy=LocalTranslation.Create(profile,"Reuse fixture","0.8b","en",new[]{"Descriptions"});LocalTranslation.Control(copy.Id,"run");var reused=Wait(copy.Id,r=>r.State.StartsWith("Completed"),30);Check(reused.Done==12&&reused.Reused==12,"second run reuses exact model/language/context cache without inference");
  var french=LocalTranslation.Create(profile,"Different language fixture","0.8b","fr",new[]{"Descriptions"});Check(LocalTranslation.Results(french.Id).Count==0,"different-language run starts with isolated results");
  string journal=LocalTranslation.RunFile(run.Id,"results.jsonl");var count=LocalTranslation.Results(run.Id).Count;File.AppendAllText(journal,"{\"incomplete\":",Encoding.UTF8);Check(LocalTranslation.Results(run.Id).Count==count,"incomplete final journal record does not discard durable earlier results");
  LocalTranslation.RepairJournal(run.Id);Check(LocalTranslation.Results(run.Id).Count==12&&File.Exists(LocalTranslation.RunFile(run.Id,"interrupted-tail.txt")),"interrupted tail repaired with preserved evidence before append");
  LocalTranslation.Select(profile,copy.Id,true);LocalTranslation.Delete(copy.Id);Check(!Directory.Exists(LocalTranslation.RunFolder(copy.Id))&&!LocalTranslation.Binding(root).Enabled,"deleting active run detaches binding and preserves another run");
  Check(LocalTranslation.Results(run.Id).Count==12,"other run results preserved after deletion");
  LocalTranslation.Select(profile,run.Id,false);LocalTranslation.Delete(french.Id);LocalTranslation.Delete(run.Id);
  File.WriteAllText(args[1],LocalTranslation.Json.Serialize(new{status="PASS",checks=checks,realModelRun=true,completed=12,initialCheckpoint=saved,cacheHits=12,fixtureRoot=root}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
