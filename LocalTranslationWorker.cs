using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class LocalTextIndex {
 public static bool Displayed(string text){return !string.IsNullOrWhiteSpace(text)&&text.Length<=12000;}
 public static List<LocalUnit> Build(LocalRun run,Func<bool> cancelled){
  if(cancelled())throw new OperationCanceledException();var options=LocalTranslation.Options(run);run.Scope=options.Scope;run.OptionsRevision=options.Revision;
  var covered=EnglishSupport.CoveredLocalSources(run.Root);var units=new List<LocalUnit>();
  foreach(var pair in covered.OrderBy(x=>x.Key)){if(!LocalLibrary.Selected(options,pair.Value,pair.Key))continue;string context="EnglishCoverage";units.Add(new LocalUnit{Source=pair.Key,Category=pair.Value,Context=context,Key=LocalTranslation.TextHash(pair.Value+"\u001f"+context+"\u001f"+pair.Key)});}
  run.Total=units.Count;run.Excluded=covered.Count-units.Count;run.SourceDigest=LocalTranslation.TextHash(LocalTranslation.Json.Serialize(covered));run.SourceStamp=run.SourceDigest+":"+options.Revision;
  LocalTranslation.Write(LocalTranslation.RunFile(run.Id,"index.json"),units);LocalTranslation.Write(LocalTranslation.RunFile(run.Id,"index-status.json"),new{indexed=units.Count,excluded=run.Excluded,coverage="Existing English dictionary keys only",unreadableFiles=new string[0],imagesIncluded=false,sourceDigest=run.SourceDigest});return units;
 }
}
public sealed class LocalInference:IDisposable {
  Process server;int port;string id,unitReference;readonly object logGate=new object();LocalRunOptions settings;LocalGlossaryProfile terminology;Dictionary<string,string> references=new Dictionary<string,string>();
  public void Configure(LocalRun run,LocalRunOptions options){settings=options;terminology=LocalLibrary.Profile(run,options);references=EnglishSupport.LocalReference(run.Root);}
  public void ReferenceUnit(string source){references.TryGetValue(source,out unitReference);}
 public LocalInference(string model,Action<string> status,Func<bool> cancelled):this(model,new LocalRunOptions(),status,cancelled){}
 public LocalInference(string model,LocalRunOptions options,Action<string> status,Func<bool> cancelled){
  id=model;var m=LocalTranslation.Model(model);if(!LocalTranslation.ModelInstalled(model))throw new IOException("Install "+m.Label+" first.");
  bool gpu=options.Backend!="cpu";
  if(gpu&&string.IsNullOrWhiteSpace(options.Device)){var devices=LocalBackend.Devices();if(devices.Count==0)throw new IOException("No Vulkan GPU is available. Set this run to CPU or install a compatible driver.");options.Device=devices.OrderByDescending(x=>x.Value.Contains("NVIDIA")||x.Value.Contains("Radeon RX")).First().Key;}
  if(!File.Exists(LocalTranslation.RuntimeExe(gpu)))throw new IOException("The local runtime is missing. Choose Install / Repair model.");
  if(Store.Hash(m.PathName)!=m.Hash)throw new IOException("The model does not match its pinned SHA-256. Repair it before use.");
  string folder=LocalTranslation.RuntimeFolder(gpu),owned=Path.Combine(folder,"owned-files.json");
  if(File.Exists(owned)){var files=LocalTranslation.Read<Dictionary<string,string>>(owned);foreach(var f in files)if(!File.Exists(LocalTranslation.Child(folder,f.Key))||Store.Hash(LocalTranslation.Child(folder,f.Key))!=f.Value)throw new IOException("A managed runtime file is missing or changed. Repair the model setup.");}
  var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);listener.Start();port=((System.Net.IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
  string args="-m "+Store.Q(m.PathName)+" --host 127.0.0.1 --port "+port+" --alias plamo-local --jinja --no-prefill-assistant -c 8192 -np 1 -t "+Math.Max(1,options.Threads)+" -ngl "+(options.Backend=="cpu"?0:options.Backend=="gpu"?999:options.GpuLayers)+(gpu&&!string.IsNullOrWhiteSpace(options.Device)?" --device "+Store.Q(options.Device):"");
  var start=new ProcessStartInfo(LocalTranslation.RuntimeExe(gpu),args){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=folder,RedirectStandardError=true,RedirectStandardOutput=true};
  server=new Process{StartInfo=start};server.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data==null)return;lock(logGate){string f=LocalTranslation.Child(LocalTranslation.Home,"Status/backend.log");Directory.CreateDirectory(Path.GetDirectoryName(f));if(File.Exists(f)&&new FileInfo(f).Length>2*1024*1024)File.WriteAllText(f,"");File.AppendAllText(f,e.Data.Substring(0,Math.Min(2000,e.Data.Length))+Environment.NewLine);}};server.OutputDataReceived+=delegate{};
  server.Start();server.BeginErrorReadLine();server.BeginOutputReadLine();LocalTranslation.Write(LocalTranslation.Child(LocalTranslation.Home,"Status/backend.json"),new{model=id,pid=server.Id,port=port,state="Starting",backend=options.Backend,device=options.Device,gpuLayers=options.Backend=="cpu"?0:options.Backend=="gpu"?999:options.GpuLayers,command=args});status("Starting model");
  var clock=Stopwatch.StartNew();try{while(clock.Elapsed.TotalSeconds<90){if(cancelled())throw new OperationCanceledException();if(server.HasExited)throw new IOException("The local model runtime exited ("+server.ExitCode+"). See model details.");try{var health=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+port+"/health");health.Timeout=500;using(var response=health.GetResponse()){status("Running");return;}}catch(WebException){}Thread.Sleep(150);}throw new IOException("Local model startup timed out. Try CPU mode or the smaller model.");}catch{Dispose();throw;}
 }
 public string Translate(string source,string language,string context,Func<bool> cancelled){
  var controls=new List<string>();string masked=Regex.Replace(source,@"(?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|%[1-9]|\s+(?:if|en)\([^\r\n]*\)$",m=>{controls.Add(m.Value);return "__CTL_"+(controls.Count-1).ToString("D4")+"__";});
  if(string.IsNullOrWhiteSpace(Regex.Replace(masked,@"__CTL_\d+__","")))return source;
   string english;if(!references.TryGetValue(source,out english))english=unitReference;string information="RPG game text. "+context+". Tokens __CTL_0000__ are literal protected control slots.";
  var kwargs=new Dictionary<string,object>{{"target_lang",language}};
  if(terminology!=null){information+="\n"+terminology.Context;if(settings.ReferenceEnglish&&terminology.UseEnglishReference&&!string.IsNullOrEmpty(english))information+="\nCurated English reference (meaning only; preserve the input's protected slots): "+english.Substring(0,Math.Min(2000,english.Length));if(settings.UseGlossary)kwargs["glossary"]=terminology.Terms.Where(x=>source.IndexOf(x.Source,StringComparison.OrdinalIgnoreCase)>=0).Take(40).Select(x=>new{source=x.Source,target=x.Target}).ToArray();}
  kwargs["context"]=information;
  var body=new{model="plamo-local",messages=new[]{new{role="user",content=masked}},chat_template_kwargs=kwargs,temperature=0,max_tokens=Math.Min(3072,Math.Max(384,masked.Length*3)),stop=new[]{"<|plamo:tag|>","<|plamo:bos|>"}};

  var request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+port+"/v1/chat/completions");request.Method="POST";request.ContentType="application/json";request.Timeout=180000;request.ReadWriteTimeout=180000;byte[] bytes=Encoding.UTF8.GetBytes(LocalTranslation.Json.Serialize(body));request.ContentLength=bytes.Length;
  using(var cancellation=new Timer(_=>{if(cancelled())request.Abort();},null,100,100)){
   try{using(var stream=request.GetRequestStream())stream.Write(bytes,0,bytes.Length);using(var response=request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8)){
    var result=LocalTranslation.Json.Deserialize<Dictionary<string,object>>(reader.ReadToEnd());var choices=result["choices"] as IEnumerable;var choice=choices==null?null:choices.Cast<object>().OfType<Dictionary<string,object>>().FirstOrDefault();if(choice==null||!choice.ContainsKey("message"))throw new IOException("The local model returned no translation choice.");if(Convert.ToString(choice["finish_reason"])=="length")throw new IOException("Translation exceeded its output limit.");string target=Convert.ToString(((Dictionary<string,object>)choice["message"])["content"]).Trim();
    for(int i=0;i<controls.Count;i++){string slot="__CTL_"+i.ToString("D4")+"__";if(Regex.Matches(target,Regex.Escape(slot)).Count!=1)throw new IOException("Model changed a protected control slot.");target=target.Replace(slot,controls[i]);}
    string numericTarget=Regex.Replace(target,@"\b\d{1,3}(?:,\d{3})+\b",m=>m.Value.Replace(",",""));
    if(Regex.IsMatch(target,@"__CTL_\d+__")||string.IsNullOrWhiteSpace(target)||!EnglishSupport.Compatible(source,target)||!Regex.Matches(source,@"\d+(?:\.\d+)?").Cast<Match>().Select(m=>m.Value).SequenceEqual(Regex.Matches(numericTarget,@"\d+(?:\.\d+)?").Cast<Match>().Select(m=>m.Value)))throw new IOException("Model output did not preserve the original controls, numbers or line layout.");return target;
   }}catch(WebException){if(cancelled())throw new OperationCanceledException();throw;}
  }
 }
 public void Dispose(){if(server!=null){try{if(!server.HasExited){server.Kill();server.WaitForExit(5000);}}catch(InvalidOperationException){}server.Dispose();server=null;}LocalTranslation.Write(LocalTranslation.Child(LocalTranslation.Home,"Status/backend.json"),new{state="Stopped"});}
}
public static class LocalTranslationWorker {
 static bool Cancelled(LocalRun run){return LocalTranslation.Requested(run.Id)!="run"||LocalTranslation.Options(run).Revision!=run.OptionsRevision;}
 public static int Main(){Directory.CreateDirectory(LocalTranslation.Home);bool created;using(var mutex=new Mutex(true,"Local\\Celesphonia.LocalTranslation.Worker",out created)){if(!created)return 0;try{
  for(;;){var next=LocalTranslation.Runs(null).Where(x=>LocalTranslation.Requested(x.Id)=="run"&&Pending(x)).OrderBy(x=>x.Updated).FirstOrDefault();if(next==null)return 0;Execute(next);}
 }finally{mutex.ReleaseMutex();}}}
 static bool Pending(LocalRun run){DateTime updated;DateTime.TryParse(run.Updated,out updated);return !run.State.StartsWith("Completed")||LocalTranslation.Options(run).Revision!=run.OptionsRevision||File.GetLastWriteTimeUtc(LocalTranslation.RunFile(run.Id,"control.json"))>updated.ToUniversalTime();}
 static void Execute(LocalRun run){using(var owned=new FileStream(LocalTranslation.RunFile(run.Id,"worker.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
  var clock=Stopwatch.StartNew();double prior=run.Seconds;LocalTranslation.Migrate(run);LocalLibrary.Bind(run);LocalTranslation.RepairJournal(run.Id);var results=LocalTranslation.Results(run.Id);var failures=LocalTranslation.Read<Dictionary<string,string>>(LocalTranslation.RunFile(run.Id,"failures.json"));var options=LocalTranslation.Options(run);var edits=LocalTranslation.Edits(run.Id);run.OptionsRevision=options.Revision;
  try{
   run.State="Indexing";run.Error="";LocalTranslation.Save(run);var units=LocalTextIndex.Build(run,()=>Cancelled(run));
   if(options.RetryGeneration>run.LastRetryGeneration){if((options.RetryKeys??new string[0]).Length==0)failures.Clear();else foreach(string key in options.RetryKeys)failures.Remove(key);run.LastRetryGeneration=options.RetryGeneration;}
   var comparison=LocalLibrary.Compare(run);var reuse=LocalLibrary.ModelRows(run,true);var reference=comparison.English;var regenerate=new HashSet<string>(options.RegenerateGeneration>run.LastRegenerateGeneration?options.RegenerateKeys??new string[0]:new string[0]);
   var pending=new List<LocalUnit>();run.Done=run.Failed=run.Deferred=0;
   using(var journal=new StreamWriter(new FileStream(LocalTranslation.RunFile(run.Id,"results.jsonl"),FileMode.Append,FileAccess.Write,FileShare.Read),new UTF8Encoding(false))){
    foreach(var unit in units){LocalEdit edit;if(edits.TryGetValue(unit.Key,out edit)&&(edit.Deferred||edit.LeaveOriginal)){run.Deferred++;continue;}if(edit!=null&&edit.Target!=null){run.Done++;continue;}if(comparison.Imported.ContainsKey(unit.Source)){run.Done++;continue;}bool forced=regenerate.Contains(unit.Key)&&(!results.ContainsKey(unit.Key)||results[unit.Key].Generation<options.RegenerateGeneration);if(results.ContainsKey(unit.Key)&&!forced){run.Done++;continue;}LocalResult cached;
     if(regenerate.Count>0&&!regenerate.Contains(unit.Key))continue;
     if(!forced&&reuse.TryGetValue(unit.Source,out cached)&&EnglishSupport.Compatible(unit.Source,cached.Target)){var row=new LocalResult{Key=unit.Key,Source=unit.Source,Category=unit.Category,Context=unit.Context,Target=cached.Target,PolicyHash=cached.PolicyHash,ReferenceHash=cached.ReferenceHash,ModelHash=run.ModelHash,GeneratedAt=cached.GeneratedAt,Generation=cached.Generation,Fields=EnglishSupport.PrepareLocalField(run.Root,unit.Source,cached.Target)};results[row.Key]=row;journal.WriteLine(LocalTranslation.Json.Serialize(row));run.Reused++;run.Done++;}
     else if(failures.ContainsKey(unit.Key)){run.Failed++;}else pending.Add(unit);
    }journal.Flush();LocalTranslation.Save(run);LocalTranslation.Snapshot(run,results);
    if(pending.Count>0)using(var model=new LocalInference(run.Model,options,state=>{run.State=state;run.ActiveBackend=options.Backend+(string.IsNullOrEmpty(options.Device)?"":" · "+options.Device);LocalTranslation.Save(run);},()=>Cancelled(run))){
     model.Configure(run,options);var speed=Stopwatch.StartNew();int completed=0;foreach(var unit in pending){
      if(Cancelled(run))throw new OperationCanceledException();model.ReferenceUnit(unit.Source);string target=null,error=null;
      for(int attempt=0;attempt<2;attempt++)try{var translated=new List<string>();foreach(string line in unit.Source.Split('\n')){if(Cancelled(run))throw new OperationCanceledException();translated.Add(string.IsNullOrWhiteSpace(line)?line:model.Translate(line,run.Language,unit.Category+"; "+unit.Context,()=>Cancelled(run)));}target=string.Join("\n",translated);if(!EnglishSupport.Compatible(unit.Source,target))throw new IOException("Control/layout mismatch.");break;}catch(OperationCanceledException){throw;}catch(Exception e){target=null;error=e.Message;}
      if(target==null){failures[unit.Key]=error;run.Failed++;}else{var row=new LocalResult{Key=unit.Key,Source=unit.Source,Category=unit.Category,Context=unit.Context,Target=target,PolicyHash=LocalLibrary.Policy(run,options,unit.Source,reference.ContainsKey(unit.Source)?reference[unit.Source]:""),ReferenceHash=LocalTranslation.TextHash(reference.ContainsKey(unit.Source)?reference[unit.Source]:""),ModelHash=run.ModelHash,GeneratedAt=DateTimeOffset.Now.ToString("o"),Generation=options.RegenerateGeneration,Fields=EnglishSupport.PrepareLocalField(run.Root,unit.Source,target)};results[unit.Key]=row;journal.WriteLine(LocalTranslation.Json.Serialize(row));journal.Flush();run.Done++;run.Processed++;}
      completed++;run.RecentRate=completed*60/Math.Max(1,speed.Elapsed.TotalSeconds);run.Seconds=prior+clock.Elapsed.TotalSeconds;run.State="Running";LocalTranslation.Write(LocalTranslation.RunFile(run.Id,"failures.json"),failures);LocalTranslation.Save(run);
     }
    }
   }
   run.LastRegenerateGeneration=options.RegenerateGeneration;if(regenerate.Count>0){run.State="Paused";run.Error="Selected model refresh finished. Other pending work is kept; Resume pending continues it.";LocalTranslation.Control(run.Id,"pause");}else{run.State=run.Failed>0||run.Deferred>0?"Completed · deferred entries":"Completed";run.Error=run.Failed+" failed and "+run.Deferred+" deferred entries retained. Retry failures is optional; completed translations are kept.";}
  }catch(OperationCanceledException){run.State="Paused";run.Error="Completed translations kept. Resume continues pending covered entries.";}catch(Exception e){run.State="Needs attention";run.Error=e.Message;LocalTranslation.Control(run.Id,"pause");}
  finally{LocalTranslation.Write(LocalTranslation.RunFile(run.Id,"failures.json"),failures);run.Seconds=prior+clock.Elapsed.TotalSeconds;LocalTranslation.Snapshot(run,results);LocalTranslation.Save(run);LocalLibrary.Publish(run);}
 }}
}
