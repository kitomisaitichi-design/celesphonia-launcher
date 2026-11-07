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
 static readonly Regex Asset=new Regex(@"(?:\.(?:png|rpgmvp|ogg|rpgmvo|m4a|json|js|mp4|webm|ttf)(?:$|[\s""'])|\$game|prototype\.|function\s*\(|=>|https?://)",RegexOptions.IgnoreCase);
 public static bool Displayed(string s){return !string.IsNullOrWhiteSpace(s)&&s.Length<=12000&&!Asset.IsMatch(s)&&!Regex.IsMatch(s,@"^\s*<[^>]+>\s*$");}
 static LocalUnit Unit(string text,string category,string context){return new LocalUnit{Source=text,Category=category,Context=context,Key=LocalTranslation.TextHash(category+"\u001f"+context+"\u001f"+text)};}
 static bool DisplayTable(string context){return new[]{"SystemMessage/","VariableIconName/","LoseMessage/","BattleMessage/","psmessage/","PervertTemplate/","ParameterBase/","ClothData/","HypnosisBase/","TagSkillList/","trap_list/"}.Any(x=>context.StartsWith(x,StringComparison.Ordinal));}
 static void Add(Dictionary<string,LocalUnit> output,string text,string category,string context){if(!Displayed(text))return;var row=Unit(text,category,context);output[row.Key]=row;}
 static object Decode(string file){
  byte[] bytes=File.ReadAllBytes(file);string text=Encoding.UTF8.GetString(bytes).TrimStart('\ufeff');
  try{return LocalTranslation.Json.DeserializeObject(text);}catch(ArgumentException){byte[] raw=Convert.FromBase64String(text);byte[] key=Encoding.ASCII.GetBytes("maple");for(int i=0;i<raw.Length;i++)raw[i]^=key[i%key.Length];return LocalTranslation.Json.DeserializeObject(Encoding.UTF8.GetString(raw).TrimStart('\ufeff'));}
 }
 static string S(object x){return x as string;}
 static void Walk(object value,string context,Dictionary<string,LocalUnit> output){
  var obj=value as Dictionary<string,object>;var array=value as object[];
  if(obj!=null){
   object v;int code=0;if(obj.ContainsKey("parameters")&&obj.TryGetValue("code",out v))int.TryParse(Convert.ToString(v),out code);
   if(code!=0&&obj.TryGetValue("parameters",out v)){
    var p=v as object[];
    if(p!=null&&p.Length>0){
     if(code==401||code==405)Add(output,S(p[0]),"Dialogue",context);
     if(code==102){var choices=p[0] as object[];if(choices!=null)foreach(var c in choices){string text=S(c);if(text!=null)Add(output,Regex.Replace(text,@"\s+(?:if|en)\([^\r\n]*\)\s*$",""),"Choices",context);}}
     if(code==356&&S(p[0])!=null){string text=S(p[0]);if(text.StartsWith("D_TEXT ",StringComparison.OrdinalIgnoreCase))Add(output,text.Substring(7),"Interface",context);}
     if(code==355||code==655){string text=S(p[0]);if(text!=null)foreach(Match match in Regex.Matches(text,@"(?:drawText|setText|showText|addText|addTicker)\s*\(\s*(['""])(.*?)\1"))Add(output,match.Groups[2].Value,"Interface",context);}
    }return;
   }
   foreach(var pair in obj){
    string text=S(pair.Value);
    if(text!=null){
     if(pair.Key=="name"||pair.Key=="nickname"||pair.Key=="displayName")Add(output,text,"Names",context);
     else if(pair.Key=="description"||pair.Key=="profile"){Add(output,text,"Descriptions",context);foreach(string line in text.Split('\n'))Add(output,line,"Descriptions",context);}
     else if(Regex.IsMatch(pair.Key,@"^message[1-4]$")||pair.Key=="text")Add(output,text,"Dialogue",context);
     else if(context.StartsWith("System/terms",StringComparison.Ordinal)||DisplayTable(context)&&pair.Key!="note")Add(output,text,"Interface",context);
    }else Walk(pair.Value,context+"/"+pair.Key,output);
   }
  }else if(array!=null){for(int i=0;i<array.Length;i++)Walk(array[i],context+"/"+i,output);}else if(value is string&&(context.StartsWith("System/terms",StringComparison.Ordinal)||DisplayTable(context)))Add(output,(string)value,"Interface",context);
 }
 public static List<LocalUnit> Build(LocalRun run,Func<bool> cancelled){
  string target=LocalTranslation.RunFile(run.Id,"index.json"),web=Store.WebRoot(run.Root),data=Path.Combine(web,"data");var units=new Dictionary<string,LocalUnit>();var opaque=new List<string>();var sources=new Dictionary<string,string>();
  if(!Directory.Exists(data))throw new IOException("The selected game has no RPG Maker data folder.");
  var stampFiles=Directory.GetFiles(data,"*.json").Concat(Directory.Exists(Path.Combine(web,"js/plugins"))?Directory.GetFiles(Path.Combine(web,"js/plugins"),"*.js"):new string[0]).Select(x=>new FileInfo(x)).OrderBy(x=>x.FullName).ToArray();
  string live=LocalTranslation.Child(LocalTranslation.Home,"Incoming/"+run.GameKey+".jsonl"),stamp=LocalTranslation.TextHash(string.Join("|",stampFiles.Select(x=>x.Name+":"+x.Length+":"+x.LastWriteTimeUtc.Ticks))+"|"+string.Join(",",run.Scope)+"|"+(File.Exists(live)?new FileInfo(live).Length:0));
  if(run.SourceStamp==stamp&&File.Exists(target))return LocalTranslation.Read<List<LocalUnit>>(target);
  foreach(string file in Directory.GetFiles(data,"*.json").OrderBy(x=>x)){
   if(cancelled())throw new OperationCanceledException();string name=Path.GetFileNameWithoutExtension(file);sources[Path.GetFileName(file)]=Store.Hash(file);
   object native;try{native=Decode(file);}catch(Exception e){if(!(e is ArgumentException||e is FormatException))throw;opaque.Add(Path.GetFileName(file));continue;}
   var array=native as object[];var map=native as Dictionary<string,object>;
   if(name.StartsWith("Map",StringComparison.Ordinal)&&name!="MapInfos"&&map!=null){
    object events;if(map.TryGetValue("displayName",out events))Add(units,S(events),"Names",name);
    if(map.TryGetValue("events",out events)&&events is object[]){var ev=(object[])events;for(int i=0;i<ev.Length;i++)Walk(ev[i],name+"/event"+i,units);}
   }else if(array!=null){for(int i=0;i<array.Length;i++)Walk(array[i],name+"/"+i,units);}
   else Walk(native,name,units);
  }
  string plugins=Path.Combine(web,"js/plugins");if(Directory.Exists(plugins))foreach(string file in Directory.GetFiles(plugins,"*.js")){
   if(cancelled())throw new OperationCanceledException();string text=File.ReadAllText(file,Encoding.UTF8);foreach(Match m in Regex.Matches(text,@"(?:drawText|addCommand|setText)\s*\(\s*(['""])(.*?)\1"))Add(units,m.Groups[2].Value,"Interface","Plugin/"+Path.GetFileName(file));
  }
  string incoming=LocalTranslation.Child(LocalTranslation.Home,"Incoming/"+run.GameKey+".jsonl");if(File.Exists(incoming))using(var input=new FileStream(incoming,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var reader=new StreamReader(input,Encoding.UTF8)){
   string line;while((line=reader.ReadLine())!=null){try{var row=LocalTranslation.Json.Deserialize<LocalUnit>(line);if(row!=null&&LocalTranslation.Categories.Contains(row.Category))Add(units,row.Source,row.Category,row.Context??"Live");}catch(ArgumentException){if(!reader.EndOfStream)throw;}}run.LiveBytes=input.Length;
  }
  var chosen=units.Values.Where(x=>run.Scope.Contains(x.Category)).OrderBy(x=>x.Context).ThenBy(x=>x.Key).ToList();run.Excluded=units.Count-chosen.Count;run.SourceDigest=LocalTranslation.TextHash(LocalTranslation.Json.Serialize(sources));run.Total=chosen.Count;
  run.SourceStamp=stamp;LocalTranslation.Write(target,chosen);LocalTranslation.Write(LocalTranslation.RunFile(run.Id,"index-status.json"),new{indexed=chosen.Count,excluded=run.Excluded,unreadableFiles=opaque,imagesIncluded=false,sourceDigest=run.SourceDigest});return chosen;
 }
}
public sealed class LocalInference:IDisposable {
 Process server;int port;string id;readonly object logGate=new object();
 public LocalInference(string model,Action<string> status,Func<bool> cancelled){
  id=model;var m=LocalTranslation.Model(model);if(!LocalTranslation.ModelInstalled(model))throw new IOException("Install "+m.Label+" first.");
  bool gpu=false;string record=Path.Combine(m.Folder,"model.json");if(File.Exists(record)){var info=LocalTranslation.Read<Dictionary<string,object>>(record);gpu=info.ContainsKey("gpu")&&Convert.ToBoolean(info["gpu"]);}
  if(!File.Exists(LocalTranslation.RuntimeExe(gpu)))throw new IOException("The local runtime is missing. Choose Install / Repair model.");
  if(Store.Hash(m.PathName)!=m.Hash)throw new IOException("The model does not match its pinned SHA-256. Repair it before use.");
  string folder=LocalTranslation.RuntimeFolder(gpu),owned=Path.Combine(folder,"owned-files.json");
  if(File.Exists(owned)){var files=LocalTranslation.Read<Dictionary<string,string>>(owned);foreach(var f in files)if(!File.Exists(LocalTranslation.Child(folder,f.Key))||Store.Hash(LocalTranslation.Child(folder,f.Key))!=f.Value)throw new IOException("A managed runtime file is missing or changed. Repair the model setup.");}
  var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);listener.Start();port=((System.Net.IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
  string args="-m "+Store.Q(m.PathName)+" --host 127.0.0.1 --port "+port+" --alias plamo-local --jinja --no-prefill-assistant -c 8192 -np 1 -t "+Math.Max(1,Math.Min(8,Environment.ProcessorCount/2))+(gpu?"":" -ngl 0");
  var start=new ProcessStartInfo(LocalTranslation.RuntimeExe(gpu),args){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=folder,RedirectStandardError=true,RedirectStandardOutput=true};
  server=new Process{StartInfo=start};server.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data==null)return;lock(logGate){string f=LocalTranslation.Child(LocalTranslation.Home,"Status/backend.log");Directory.CreateDirectory(Path.GetDirectoryName(f));if(File.Exists(f)&&new FileInfo(f).Length>2*1024*1024)File.WriteAllText(f,"");File.AppendAllText(f,e.Data.Substring(0,Math.Min(2000,e.Data.Length))+Environment.NewLine);}};server.OutputDataReceived+=delegate{};
  server.Start();server.BeginErrorReadLine();server.BeginOutputReadLine();LocalTranslation.Write(LocalTranslation.Child(LocalTranslation.Home,"Status/backend.json"),new{model=id,pid=server.Id,port=port,state="Starting"});status("Starting model");
  var clock=Stopwatch.StartNew();try{while(clock.Elapsed.TotalSeconds<90){if(cancelled())throw new OperationCanceledException();if(server.HasExited)throw new IOException("The local model runtime exited ("+server.ExitCode+"). See model details.");try{var health=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+port+"/health");health.Timeout=500;using(var response=health.GetResponse()){status("Running");return;}}catch(WebException){}Thread.Sleep(150);}throw new IOException("Local model startup timed out. Try CPU mode or the smaller model.");}catch{Dispose();throw;}
 }
 public string Translate(string source,string language,string context,Func<bool> cancelled){
  var controls=new List<string>();string masked=Regex.Replace(source,@"(?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|%[1-9]|\s+(?:if|en)\([^\r\n]*\)$",m=>{controls.Add(m.Value);return "__CTL_"+(controls.Count-1).ToString("D4")+"__";});
  if(string.IsNullOrWhiteSpace(Regex.Replace(masked,@"__CTL_\d+__","")))return source;
  var body=new{model="plamo-local",messages=new[]{new{role="user",content=masked}},chat_template_kwargs=new{target_lang=language,context="RPG game text. "+context+". Tokens __CTL_0000__ are literal protected control slots."},temperature=0,max_tokens=Math.Min(3072,Math.Max(384,masked.Length*3)),stop=new[]{"<|plamo:tag|>","<|plamo:bos|>"}};
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
 static bool Cancelled(LocalRun run){return LocalTranslation.Requested(run.Id)!="run";}
 public static int Main(){Directory.CreateDirectory(LocalTranslation.Home);bool created;using(var mutex=new Mutex(true,"Local\\Celesphonia.LocalTranslation.Worker",out created)){if(!created)return 0;try{
  for(;;){var next=LocalTranslation.Runs(null).Where(x=>LocalTranslation.Requested(x.Id)=="run"&&Pending(x)).OrderBy(x=>x.Updated).FirstOrDefault();if(next==null)return 0;Execute(next);}
 }finally{mutex.ReleaseMutex();}}}
 static bool Pending(LocalRun run){
  string control=LocalTranslation.RunFile(run.Id,"control.json"),incoming=LocalTranslation.Child(LocalTranslation.Home,"Incoming/"+run.GameKey+".jsonl");DateTime updated;DateTime.TryParse(run.Updated,out updated);
  return run.State!="Completed"&&run.State!="Completed with issues"||File.GetLastWriteTimeUtc(control)>updated.ToUniversalTime()||File.Exists(incoming)&&new FileInfo(incoming).Length>run.LiveBytes;
 }
 static void Execute(LocalRun run){
  using(var owned=new FileStream(LocalTranslation.RunFile(run.Id,"worker.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
   var clock=Stopwatch.StartNew();double prior=run.Seconds;LocalTranslation.RepairJournal(run.Id);var results=LocalTranslation.Results(run.Id);var failures=new Dictionary<string,string>();
   try{
    run.State="Indexing";run.Error="";LocalTranslation.Save(run);var units=LocalTextIndex.Build(run,()=>Cancelled(run));run.Done=results.Count;LocalTranslation.Save(run);LocalTranslation.Snapshot(run,results);
    if(units.Count==0){run.State="Waiting for game text";run.Error="No readable display text was indexed. Launch the game once; the live adapter will capture its loaded text.";LocalTranslation.Control(run.Id,"pause");LocalTranslation.Save(run);return;}
    var reuse=new Dictionary<string,LocalResult>();foreach(var other in LocalTranslation.Runs(run.Root).Where(x=>x.Id!=run.Id&&x.ModelHash==run.ModelHash&&x.PromptVersion==run.PromptVersion&&x.Language==run.Language))foreach(var row in LocalTranslation.Results(other.Id).Values)reuse[row.Key]=row;
    var pending=new List<LocalUnit>();using(var journal=new StreamWriter(new FileStream(LocalTranslation.RunFile(run.Id,"results.jsonl"),FileMode.Append,FileAccess.Write,FileShare.Read),new UTF8Encoding(false))){
     foreach(var unit in units){if(results.ContainsKey(unit.Key))continue;LocalResult cached;if(reuse.TryGetValue(unit.Key,out cached)){results[unit.Key]=cached;journal.WriteLine(LocalTranslation.Json.Serialize(cached));run.Reused++;}else pending.Add(unit);}journal.Flush();run.Done=results.Count;LocalTranslation.Save(run);LocalTranslation.Snapshot(run,results);
     if(pending.Count>0)using(var model=new LocalInference(run.Model,state=>{run.State=state;LocalTranslation.Save(run);},()=>Cancelled(run))){
      int since=0;var checkpoint=Stopwatch.StartNew();foreach(var unit in pending){
       if(Cancelled(run))throw new OperationCanceledException();string target=null,error=null;
       for(int attempt=0;attempt<2;attempt++)try{var lines=unit.Source.Split('\n');var translated=new List<string>();foreach(string line in lines){if(Cancelled(run))throw new OperationCanceledException();translated.Add(string.IsNullOrWhiteSpace(line)?line:model.Translate(line,run.Language,unit.Category+"; "+unit.Context,()=>Cancelled(run)));}target=string.Join("\n",translated);if(!EnglishSupport.Compatible(unit.Source,target))throw new IOException("Control/layout mismatch.");break;}catch(OperationCanceledException){throw;}catch(Exception e){target=null;error=e.Message;}
       if(target==null){failures[unit.Key]=error;run.Failed=failures.Count;}else{var row=new LocalResult{Key=unit.Key,Source=unit.Source,Category=unit.Category,Context=unit.Context,Target=target};results[unit.Key]=row;journal.WriteLine(LocalTranslation.Json.Serialize(row));journal.Flush();run.Done=results.Count;}
       run.Seconds=prior+clock.Elapsed.TotalSeconds;run.State="Running";LocalTranslation.Save(run);if(++since>=8||checkpoint.Elapsed.TotalSeconds>=3){LocalTranslation.Snapshot(run,results);since=0;checkpoint.Restart();}
      }
     }
    }
    LocalTranslation.Write(LocalTranslation.RunFile(run.Id,"failures.json"),failures);run.State=failures.Count>0?"Completed with issues":"Completed";run.Error=failures.Count>0?failures.Count+" entries stayed original. Resume to retry them.":"";
   }catch(OperationCanceledException){run.State="Paused";run.Error="Completed translations remain available.";}catch(Exception e){run.State="Needs attention";run.Error=e.Message;LocalTranslation.Control(run.Id,"pause");}
   finally{run.Done=results.Count;run.Seconds=prior+clock.Elapsed.TotalSeconds;LocalTranslation.Snapshot(run,results);LocalTranslation.Save(run);}
  }
 }
}
