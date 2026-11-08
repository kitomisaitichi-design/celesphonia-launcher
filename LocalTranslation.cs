using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

public class LocalModel {
 public string Id,Label,File,Revision,Hash;public long Bytes;
 public string Folder {get{return LocalTranslation.Child(LocalTranslation.Home,"Models/"+Id);}}
 public string PathName {get{return System.IO.Path.Combine(Folder,File);}}
 public string Url {get{return "https://huggingface.co/pfnet/plamo-3-translate-"+Id+"-GGUF/resolve/"+Revision+"/"+File;}}
 public override string ToString(){return Label;}
}
public class LocalUnit {public string Key,Source,Category,Context;}
public class LocalResult:LocalUnit {public string Target,PolicyHash,ReferenceHash,ModelHash,GeneratedAt;public int Generation;public EnglishDisplay Fields;}
public class LocalRun {
 public int Format=1;public string Id,Name,Root,GameKey,Model,Language,State="Ready",Error="",Updated,Owner,SourceDigest,SourceStamp,ModelHash;public int PromptVersion=1;public string Family,OriginVersion;public int LastRegenerateGeneration;
 public string[] Scope;public int Total,Done,Failed,Reused,Excluded,Deferred,Processed,OptionsRevision,LastRetryGeneration;public double Seconds,RecentRate;public long LiveBytes;public string ActiveBackend;
 public override string ToString(){return Name+"  ·  "+State;}
}
public class LocalRunOptions {public int Format=2,Revision=1,RetryGeneration=0,GpuLayers=8,Threads=Math.Max(1,Math.Min(8,Environment.ProcessorCount/2));public string Backend="cpu",Device="";public string[] Scope;public string GlossaryProfileId="",RenderLayer="effective";public bool UseGlossary=true,ReferenceEnglish=true;public int RegenerateGeneration;public string[] PackIds=new string[0],IncludeSources=new string[0],IncludeKeys=new string[0],RegenerateKeys=new string[0],RetryKeys=new string[0];}
public class LocalEdit:LocalUnit {public EnglishDisplay Fields;public string Target,Updated;public bool LeaveOriginal,Deferred;}
public class LocalBinding {public int Format=1;public bool Enabled;public string RunId;}
public class LocalEntry {public int Format=1;public string Entry,BaseHash,PatchedHash;}
public class LocalInstall {public string State,Detail;public long Received,Total;}
public static class LocalTranslation {
 [ThreadStatic]static JavaScriptSerializer serializer;
 public static JavaScriptSerializer Json{get{return serializer??(serializer=new JavaScriptSerializer{MaxJsonLength=128*1024*1024,RecursionLimit=200});}}
 public static readonly string Home=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Celesphonia","LocalTranslation");
 public static readonly LocalModel[] Models={
  new LocalModel{Id="0.8b",Label="PLaMo 3 · 0.8B · 573 MB",File="plamo-3-translate-0.8b-Q4_K_M.gguf",Revision="0c4af2b003fdd94b6868aa955414c1c86ee96997",Bytes=573125952,Hash="73a1d942d8ab1e654d83898d997b8b4808c32ba669b378df612230989d18f079"},
  new LocalModel{Id="2b",Label="PLaMo 3 · 2B · 1.81 GB",File="plamo-3-translate-2b-Q4_K_M.gguf",Revision="d44589ecd5287cd64452e9cd1e2cfdad904fe216",Bytes=1807202144,Hash="2189a8a0b4b359f59443ec36cfb9c9c3f603a678c00dbcee8598b3f7ea7c12fb"}};
 public static readonly string[] Categories={"Dialogue","Choices","Descriptions","Names","Interface"};
 public const string RuntimeTag="b12189";
 public static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 public static string TextHash(string s){return Hash(Encoding.UTF8.GetBytes(s));}
 public static string Child(string root,string rel){return Store.Child(root,rel.Replace('/',Path.DirectorySeparatorChar));}
 public static T Read<T>(string file) where T:new(){for(int attempt=0;;attempt++)try{using(var input=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new StreamReader(input,Encoding.UTF8))return Json.Deserialize<T>(reader.ReadToEnd());}catch(FileNotFoundException){return new T();}catch(DirectoryNotFoundException){return new T();}catch(IOException){if(attempt>=7)throw;Thread.Sleep(25*(attempt+1));}}
 public static void Write(string file,object value){Atomic(file,Encoding.UTF8.GetBytes(Json.Serialize(value)));}
 public static void Atomic(string file,byte[] bytes){Directory.CreateDirectory(Path.GetDirectoryName(file));string tmp=file+"."+Guid.NewGuid().ToString("N")+".new";File.WriteAllBytes(tmp,bytes);try{for(int attempt=0;;attempt++)try{if(File.Exists(file))File.Replace(tmp,file,null);else File.Move(tmp,file);break;}catch(IOException){if(attempt>=7)throw;Thread.Sleep(25*(attempt+1));}}finally{if(File.Exists(tmp))File.Delete(tmp);}}
 public static LocalRunOptions Options(LocalRun run){var value=Read<LocalRunOptions>(RunFile(run.Id,"options.json"));if(value.Scope==null)value.Scope=run.Scope;return value;}
 public static void SetOptions(string id,LocalRunOptions options){if(options.Scope==null||options.Scope.Length==0||options.Scope.Any(x=>!Categories.Contains(x)))throw new IOException("Select at least one covered category.");if(!new[]{"cpu","gpu","hybrid"}.Contains(options.Backend))throw new IOException("Unknown backend.");options.Threads=Math.Max(1,Math.Min(Environment.ProcessorCount,options.Threads));options.GpuLayers=Math.Max(1,Math.Min(200,options.GpuLayers));options.Revision++;Write(RunFile(id,"options.json"),options);}
 public static Dictionary<string,LocalEdit> Edits(string id){return Read<Dictionary<string,LocalEdit>>(RunFile(id,"overrides.json"));}
 public static void Edit(string id,LocalEdit edit){if(edit.Source==null||edit.Key==null)throw new IOException("Choose an indexed entry.");if(edit.Target!=null&&(!EnglishSupport.Compatible(edit.Source,edit.Target)||!System.Text.RegularExpressions.Regex.Matches(edit.Source,@"\d+(?:\.\d+)?").Cast<System.Text.RegularExpressions.Match>().Select(x=>x.Value).SequenceEqual(System.Text.RegularExpressions.Regex.Matches(edit.Target,@"\d+(?:\.\d+)?").Cast<System.Text.RegularExpressions.Match>().Select(x=>x.Value))))throw new IOException("Keep the source control codes, numbers and line breaks unchanged.");var run=Run(id);if(!EnglishSupport.CoveredLocalSources(run.Root).ContainsKey(edit.Source))throw new IOException("This source is outside the existing English coverage. It stays inactive.");using(var lease=new FileStream(RunFile(id,"edit.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){var edits=Edits(id);edit.Fields=edit.Target==null?null:EnglishSupport.PrepareLocalField(run.Root,edit.Source,edit.Target);edit.Updated=DateTimeOffset.Now.ToString("o");edits[edit.Key]=edit;Write(RunFile(id,"overrides.json"),edits);}var options=Options(run);SetOptions(id,options);LocalLibrary.Publish(run);}
 public static string GameKey(string root){return TextHash(Path.GetFullPath(root).TrimEnd('\\').ToUpperInvariant()).Substring(0,24);}
 public static string BindingPath(string root){return Child(Home,"Bindings/"+GameKey(root)+".json");}
 public static LocalBinding Binding(string root){return Read<LocalBinding>(BindingPath(root));}
 public static LocalModel Model(string id){return Models.Single(x=>x.Id==id);}
 public static string RunFolder(string id){if(id==null||!System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-f0-9]{32}$"))throw new IOException("Invalid local translation run.");return Child(Home,"Runs/"+id);}
 public static string RunFile(string id,string file){return Child(RunFolder(id),file);}
 public static LocalRun Run(string id){return Read<LocalRun>(RunFile(id,"run.json"));}
 public static List<LocalRun> Runs(string root){string folder=Path.Combine(Home,"Runs");if(!Directory.Exists(folder))return new List<LocalRun>();return Directory.GetDirectories(folder).Select(x=>Read<LocalRun>(Path.Combine(x,"run.json"))).Where(x=>x.Id!=null&&(root==null||x.GameKey==GameKey(root))).OrderByDescending(x=>x.Updated).ToList();}
 public static void Save(LocalRun run){run.Updated=DateTimeOffset.Now.ToString("o");Write(RunFile(run.Id,"run.json"),run);}
 public static void Migrate(LocalRun run){
  string file=RunFile(run.Id,"migration.json");if(!File.Exists(RunFile(run.Id,"technical-colors.json"))){var recovery=new Dictionary<string,string>();foreach(var row in Results(run.Id).Values)if(System.Text.RegularExpressions.Regex.IsMatch(row.Source,@"^(?:black|white|red|green|blue|yellow|cyan|magenta|gray|grey|transparent|#[0-9a-fA-F]{3,8}|rgba?\([\d\s.,%]+\))$"))recovery[row.Target]=row.Source;Write(RunFile(run.Id,"technical-colors.json"),recovery);}if(Read<Dictionary<string,object>>(file).ContainsKey("coveredOnly"))return;
  var covered=EnglishSupport.CoveredLocalSources(run.Root);var fields=new Dictionary<string,EnglishDisplay>();int inactive=0;var results=Results(run.Id);
  foreach(var row in results.Values){if(covered.ContainsKey(row.Source))fields[row.Key]=EnglishSupport.PrepareLocalField(run.Root,row.Source,row.Target);else inactive++;}
  var oldIndex=Read<List<LocalUnit>>(RunFile(run.Id,"index.json"));var oldFailures=Read<Dictionary<string,string>>(RunFile(run.Id,"failures.json"));var failures=new Dictionary<string,string>(oldFailures);
  foreach(var row in oldIndex){string error,kind;if(oldFailures.TryGetValue(row.Key,out error)&&covered.TryGetValue(row.Source,out kind)){string key=TextHash(kind+"\u001fEnglishCoverage\u001f"+row.Source);failures[key]=error;}}
  Write(RunFile(run.Id,"failures.json"),failures);var options=Options(run);var completed=new HashSet<string>(results.Values.Where(x=>covered.ContainsKey(x.Source)).Select(x=>x.Source));var active=covered.Where(x=>options.Scope.Contains(x.Value)).ToList();run.Total=active.Count;run.Excluded=covered.Count-active.Count;run.Done=active.Count(x=>completed.Contains(x.Key));run.Failed=active.Count(x=>!completed.Contains(x.Key)&&failures.ContainsKey(TextHash(x.Value+"\u001fEnglishCoverage\u001f"+x.Key)));run.State="Paused";run.Error="Upgraded to existing English coverage only. Successful work retained; resume pending entries when ready.";Save(run);
  Write(RunFile(run.Id,"legacy-fields.json"),fields);if(!File.Exists(RunFile(run.Id,"options.json")))Write(RunFile(run.Id,"options.json"),Options(run));
  Write(file,new{coveredOnly=true,retainedGeneratedEntries=fields.Count,inactiveLegacyEntries=inactive,originalJournalPreserved=true});
 }
 public static LocalInstall InstallStatus(string id){return Read<LocalInstall>(Child(Home,"Status/install-"+id+".json"));}
 static void InstallProgress(string id,string state,long got,long total,string detail){Write(Child(Home,"Status/install-"+id+".json"),new LocalInstall{State=state,Received=got,Total=total,Detail=detail});}
 public static bool ModelInstalled(string id){var m=Model(id);return File.Exists(m.PathName)&&new FileInfo(m.PathName).Length==m.Bytes;}
 public static string RuntimeFolder(bool gpu){return Child(Home,"Runtime/"+RuntimeTag+(gpu?"-vulkan":"-cpu"));}
 public static string RuntimeExe(bool gpu){return Path.Combine(RuntimeFolder(gpu),"llama-server.exe");}
 static void Download(string id,string url,string destination,long size,string hash,CancellationToken cancellation){
  if(File.Exists(destination)&&new FileInfo(destination).Length==size&&Store.Hash(destination)==hash)return;
  Directory.CreateDirectory(Path.GetDirectoryName(destination));string part=destination+".partial";long offset=File.Exists(part)?new FileInfo(part).Length:0;if(offset>=size){if(offset==size&&Store.Hash(part)==hash){if(File.Exists(destination))File.Replace(part,destination,null);else File.Move(part,destination);return;}offset=0;}
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(url);request.UserAgent="Celesphonia-Local-TL/1.0.41";request.Timeout=30000;request.ReadWriteTimeout=30000;if(offset>0)request.AddRange(offset);
  using(cancellation.Register(()=>request.Abort()))using(var response=(HttpWebResponse)request.GetResponse()){
   if(offset>0&&response.StatusCode!=HttpStatusCode.PartialContent)offset=0;
   using(var input=response.GetResponseStream())using(var output=new FileStream(part,offset>0?FileMode.Append:FileMode.Create,FileAccess.Write,FileShare.Read)){
    byte[] bytes=new byte[262144];int n;var clock=Stopwatch.StartNew();long tick=-1000;
    while((n=input.Read(bytes,0,bytes.Length))>0){cancellation.ThrowIfCancellationRequested();output.Write(bytes,0,n);offset+=n;if(offset>size)throw new IOException("Download exceeded the pinned size.");if(clock.ElapsedMilliseconds-tick>=400){InstallProgress(id,"Downloading",offset,size,Path.GetFileName(destination));tick=clock.ElapsedMilliseconds;}}
   }
  }
  cancellation.ThrowIfCancellationRequested();InstallProgress(id,"Checking download",size,size,Path.GetFileName(destination));if(new FileInfo(part).Length!=size||Store.Hash(part)!=hash)throw new IOException("Download did not match its official SHA-256. Retry to repair it.");
  if(File.Exists(destination))File.Replace(part,destination,null);else File.Move(part,destination);
 }
 static FileStream SetupLease(){Directory.CreateDirectory(Home);try{return new FileStream(Child(Home,"model-setup.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException e){throw new IOException("Another local model setup or removal is in progress. Wait for it to finish, then retry.",e);}}
 public static void Install(string id,bool gpu,CancellationToken cancellation){
  var m=Model(id);Directory.CreateDirectory(Home);using(var gate=SetupLease())try{
   if(new DriveInfo(Path.GetPathRoot(Home)).AvailableFreeSpace<(!ModelInstalled(id)?m.Bytes:0)+180L*1024*1024)throw new IOException("Not enough free space for the model and local runtime.");
   string folder=RuntimeFolder(gpu),name="llama-"+RuntimeTag+"-bin-win-"+(gpu?"vulkan":"cpu")+"-x64.zip";
   Download(id,"https://github.com/ggml-org/llama.cpp/releases/download/"+RuntimeTag+"/"+name,Path.Combine(folder,name),gpu?37706485:24749596,gpu?"9d56405579492423833cdde94cf46cae15d38333f585f3957cafebfc6fb0e182":"f912ea5e3e397c63405c81e732131ab0fa6e72411c80546bb5ad643f7dd82aac",cancellation);
   var files=new Dictionary<string,string>();using(var zip=ZipFile.OpenRead(Path.Combine(folder,name)))foreach(var entry in zip.Entries){cancellation.ThrowIfCancellationRequested();if(entry.FullName.EndsWith("/"))continue;string target=Child(folder,entry.FullName);using(var input=entry.Open())using(var output=new MemoryStream()){input.CopyTo(output);Atomic(target,output.ToArray());}files[entry.FullName]=Store.Hash(target);}
   if(!File.Exists(RuntimeExe(gpu)))throw new IOException("The official runtime did not contain llama-server.exe.");Write(Path.Combine(folder,"owned-files.json"),files);
   Download(id,m.Url,m.PathName,m.Bytes,m.Hash,cancellation);Write(Path.Combine(m.Folder,"model.json"),new{format=1,id=m.Id,hash=m.Hash,revision=m.Revision,gpu=gpu,source=m.Url});InstallProgress(id,"Installed",m.Bytes,m.Bytes,"Ready to start a translation run");
  }catch(OperationCanceledException){InstallProgress(id,"Paused download",0,m.Bytes,"Resume installation to continue the partial download");throw;}catch(Exception e){if(cancellation.IsCancellationRequested){InstallProgress(id,"Paused download",0,m.Bytes,"Resume installation to continue the partial download");throw new OperationCanceledException();}InstallProgress(id,"Setup needs attention",0,m.Bytes,e.Message);throw;}
 }
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct FileOperation {public IntPtr hwnd;public uint function;[MarshalAs(UnmanagedType.LPWStr)]public string from;[MarshalAs(UnmanagedType.LPWStr)]public string to;public ushort flags;public bool aborted;public IntPtr mappings;[MarshalAs(UnmanagedType.LPWStr)]public string title;}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHFileOperation(ref FileOperation op);
 public static void RecycleOwned(string path){
  string full=Path.GetFullPath(path);if(!full.StartsWith(Home+"\\",StringComparison.OrdinalIgnoreCase)||full==Home||!Directory.Exists(full))throw new IOException("Only owned local translation data can be removed.");
  Store.CheckLinks(full);var op=new FileOperation{function=3,from=full+'\0'+'\0',flags=0x0040|0x0010|0x0400|0x0004};int result=SHFileOperation(ref op);if(result!=0||op.aborted||Directory.Exists(full))throw new IOException("Windows could not recycle this local translation data ("+result+").");
 }
 public static void Uninstall(string id){using(var gate=SetupLease())UninstallCore(id);}
 static void UninstallCore(string id){
  var m=Model(id);foreach(var run in Runs(null).Where(x=>x.Model==id))Control(run.Id,"pause");
  string use=Child(Home,"Status/backend.json");var clock=Stopwatch.StartNew();while(clock.ElapsedMilliseconds<12000){var state=Read<Dictionary<string,object>>(use);if(!state.ContainsKey("model")||Convert.ToString(state["model"])!=id)break;Thread.Sleep(150);}
  var current=Read<Dictionary<string,object>>(use);if(current.ContainsKey("model")&&Convert.ToString(current["model"])==id)throw new IOException("The model is still stopping. Retry removal after its current request finishes.");
  if(Directory.Exists(m.Folder)){if(File.Exists(m.PathName)&&Store.Hash(m.PathName)!=m.Hash)throw new IOException("Model file has changed. Its folder was preserved.");if(Directory.GetDirectories(m.Folder).Length>0||Directory.GetFiles(m.Folder).Any(x=>!new[]{m.File,m.File+".partial","model.json"}.Contains(Path.GetFileName(x))))throw new IOException("The model folder contains unknown user files. They were preserved.");RecycleOwned(m.Folder);}InstallProgress(id,"Not installed",0,m.Bytes,"Cached runs are kept and can still be used");
 }
 public static void Control(string id,string action){RunFolder(id);if(action!="run"&&action!="pause")throw new IOException("Unknown run action.");Write(RunFile(id,"control.json"),new{action=action});if(action=="run")StartWorker();}
 public static string Requested(string id){var d=Read<Dictionary<string,object>>(RunFile(id,"control.json"));return d.ContainsKey("action")?Convert.ToString(d["action"]):"pause";}
 public static void Delete(string id){
  var run=Run(id);Control(id,"pause");var clock=Stopwatch.StartNew();while(clock.ElapsedMilliseconds<12000){run=Run(id);if(run.State!="Running"&&run.State!="Starting model"&&run.State!="Indexing")break;Thread.Sleep(100);}
  if(run.State=="Running"||run.State=="Starting model"||run.State=="Indexing")throw new IOException("This run is still stopping. Retry deletion shortly.");
  bool released=false;while(clock.ElapsedMilliseconds<12000){try{using(var lease=new FileStream(RunFile(id,"worker.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){released=true;}break;}catch(IOException){Thread.Sleep(100);}}if(!released)throw new IOException("This run is still releasing its files. Retry shortly.");
  string[] ownedFiles={"run.json","control.json","results.jsonl","index.json","index-status.json","failures.json","worker.lock","display.json","interrupted-tail.txt","options.json","overrides.json","edit.lock","migration.json","legacy-fields.json","technical-colors.json","layers.json","layers-meta.json"};if(Directory.GetDirectories(RunFolder(id)).Length>0||Directory.GetFiles(RunFolder(id)).Any(x=>!ownedFiles.Contains(Path.GetFileName(x))))throw new IOException("This run contains unknown user files. They were preserved.");
  var binding=Binding(run.Root);if(binding.RunId==id){binding.Enabled=false;binding.RunId=null;Write(BindingPath(run.Root),binding);}RecycleOwned(RunFolder(id));
 }
 public static void StartWorker(){var info=new ProcessStartInfo(Path.Combine(Store.Home,"Celesphonia Portable Switcher.exe"),"--local-tl-worker"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=Store.Home};Process.Start(info);}
 public static void PauseOwned(){foreach(var run in Runs(null).Where(x=>x.Owner==Store.Home&&(x.State=="Running"||x.State=="Starting model"||Requested(x.Id)=="run")))Control(run.Id,"pause");}
 static string Entry(string root){var package=Read<Dictionary<string,object>>(Path.Combine(root,"package.json"));if(!package.ContainsKey("main"))throw new IOException("This installed game has no entry point.");return Child(root,Convert.ToString(package["main"]));}
 static string EntryFolder(string root){return Path.Combine(Store.WebRoot(root),"celesphonia-local");}
 public static string BaseHash(string root,string file,string hash){
  var state=Read<LocalEntry>(Path.Combine(EntryFolder(root),"entry.json"));if(state.Entry==null)return hash;
  if(!Path.GetFullPath(file).Equals(Entry(root),StringComparison.OrdinalIgnoreCase))return hash;
  if(hash==state.PatchedHash){string backup=Path.Combine(EntryFolder(root),"base-entry.bin");if(!File.Exists(backup)||Store.Hash(backup)!=state.BaseHash)throw new IOException("Local translation's original entry backup changed.");return state.BaseHash;}return hash;
 }
 public static string BaseFile(string root,string file){var state=Read<LocalEntry>(Path.Combine(EntryFolder(root),"entry.json"));if(state.Entry!=null&&Path.GetFullPath(file).Equals(Entry(root),StringComparison.OrdinalIgnoreCase)&&Store.Hash(file)==state.PatchedHash){BaseHash(root,file,state.PatchedHash);return Path.Combine(EntryFolder(root),"base-entry.bin");}return file;}
 public static byte[] Compose(Profile profile,byte[] baseEntry){
  var binding=Binding(profile.Root);string folder=EntryFolder(profile.Root),entry=Entry(profile.Root),record=Path.Combine(folder,"entry.json");
  string html=Encoding.UTF8.GetString(baseEntry);if(html.Contains("<!-- Celesphonia local translator -->"))throw new IOException("Local translation entry was supplied twice.");
  if(!binding.Enabled){if(File.Exists(record)){Atomic(Path.Combine(folder,"base-entry.bin"),baseEntry);Write(record,new LocalEntry{Entry=entry.Substring(profile.Root.TrimEnd('\\').Length+1),BaseHash=Hash(baseEntry),PatchedHash=Hash(baseEntry)});}return baseEntry;}
  string anchor="<script src=\"celesphonia-english/dictionary.js\"></script>";int at=html.IndexOf(anchor,StringComparison.Ordinal);
  if(at<0){var m=System.Text.RegularExpressions.Regex.Match(html,@"<script\b[^>]*\bsrc\s*=\s*['""]js/main\.js['""][^>]*>",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(!m.Success)throw new IOException("This game entry has no supported RPG Maker main script.");at=m.Index;}
  Migrate(Run(binding.RunId));LocalLibrary.Publish(Run(binding.RunId));Directory.CreateDirectory(folder);Atomic(Path.Combine(folder,"base-entry.bin"),baseEntry);
  byte[] runtime;using(var source=typeof(LocalTranslation).Assembly.GetManifestResourceStream("Celesphonia.LocalTranslationRuntime.js"))using(var memory=new MemoryStream()){if(source==null)throw new IOException("Local translation adapter is missing.");source.CopyTo(memory);runtime=memory.ToArray();}
  Atomic(Path.Combine(folder,"runtime.js"),runtime);Atomic(Path.Combine(folder,"shared-renderer.js"),EnglishRelease.Runtime());Write(Path.Combine(folder,"covered.json"),EnglishSupport.CoveredLocalSources(profile.Root));Atomic(Path.Combine(folder,"config.js"),Encoding.UTF8.GetBytes("window.CelesphoniaLocalConfig="+Json.Serialize(new{binding=BindingPath(profile.Root),home=Home,gameKey=GameKey(profile.Root),covered=Path.Combine(folder,"covered.json")})+";"));
  string markup="<!-- Celesphonia local translator -->\n<script src=\"celesphonia-local/config.js\"></script>\n<script src=\"celesphonia-local/runtime.js\"></script>\n<script src=\"celesphonia-local/shared-renderer.js\"></script>\n";
  html=html.Insert(at,markup);
  // Select the rendering source at boot even when an older English runtime
  // remains installed. Its exact reversible base and saved toggle stay intact.
  foreach(string name in new[]{"dictionary.js","runtime.js"}){string tag="<script src=\"celesphonia-english/"+name+"\"></script>";html=html.Replace(tag,"<script>if(!window.CelesphoniaLocalMode)document.write('<script src=\"celesphonia-english/"+name+"\"><\\/script>');</script>");}
  byte[] patched=Encoding.UTF8.GetBytes(html);Write(record,new LocalEntry{Entry=entry.Substring(profile.Root.TrimEnd('\\').Length+1),BaseHash=Hash(baseEntry),PatchedHash=Hash(patched)});return patched;
 }
 public static bool Prepare(Profile profile){
  string entry=Entry(profile.Root),folder=EntryFolder(profile.Root),statePath=Path.Combine(folder,"entry.json");var state=Read<LocalEntry>(statePath);byte[] bytes=File.ReadAllBytes(entry);
  if(state.Entry!=null){string hash=Hash(bytes);if(hash==state.PatchedHash){BaseHash(profile.Root,entry,hash);bytes=File.ReadAllBytes(Path.Combine(folder,"base-entry.bin"));}else if(hash!=state.BaseHash)throw new IOException("The game entry was edited outside the launcher. Its changes were preserved.");}
  else if(Directory.Exists(folder)&&Directory.EnumerateFileSystemEntries(folder).Any())throw new IOException("An unrecognized celesphonia-local folder exists; it was preserved.");
  byte[] combined=Compose(profile,bytes);if(!File.ReadAllBytes(entry).SequenceEqual(combined))Atomic(entry,combined);return Binding(profile.Root).Enabled;
 }
 public static void Select(Profile profile,string id,bool enabled){
  var run=Run(id);if(run.GameKey!=GameKey(profile.Root))throw new IOException("This run belongs to another installed game.");if(enabled)LocalLibrary.Publish(run);Write(BindingPath(profile.Root),new LocalBinding{Enabled=enabled,RunId=id});
  if(!Store.Running(new Settings{Profiles=new List<Profile>{profile}}))Prepare(profile);
 }
 public static LocalRun Create(Profile profile,string name,string model,string language,string[] scope){
  if(profile==null||!File.Exists(Path.Combine(profile.Root,"package.json")))throw new IOException("Choose an installed RPG Maker game.");Model(model);if(!LocalLanguages.All.ContainsKey(language))throw new IOException("Choose a supported target language.");if(scope==null||scope.Length==0||scope.Any(x=>!Categories.Contains(x)))throw new IOException("Select at least one text category.");
  var run=new LocalRun{Id=Guid.NewGuid().ToString("N"),Root=Path.GetFullPath(profile.Root),GameKey=GameKey(profile.Root),Model=model,ModelHash=Model(model).Hash,Language=language,Scope=scope,Name=string.IsNullOrWhiteSpace(name)?LocalLanguages.All[language]+" · "+profile.Name:name.Trim(),Owner=Store.Home,State="Ready"};Save(run);LocalLibrary.Carryover(run);return run;
 }
 public static Dictionary<string,LocalResult> Results(string id){var result=new Dictionary<string,LocalResult>();string file=RunFile(id,"results.jsonl");if(!File.Exists(file))return result;using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new StreamReader(stream,Encoding.UTF8)){string line;while((line=reader.ReadLine())!=null){if(line.Length==0)continue;try{var row=Json.Deserialize<LocalResult>(line);if(row!=null&&row.Key!=null&&row.Target!=null)result[row.Key]=row;}catch(ArgumentException){if(!reader.EndOfStream)throw;}}}return result;}
 public static void RepairJournal(string id){
  string file=RunFile(id,"results.jsonl");if(!File.Exists(file))return;
  using(var stream=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.Read|FileShare.Delete)){
   if(stream.Length==0)return;stream.Position=stream.Length-1;if(stream.ReadByte()==10)return;
   long start=Math.Max(0,stream.Length-512*1024);stream.Position=start;byte[] tail=new byte[(int)(stream.Length-start)];int length=stream.Read(tail,0,tail.Length),at=length-1;while(at>=0&&tail[at]!=10)at--;
   if(at<0&&start>0)throw new IOException("An interrupted cache record is unexpectedly large. Its file was preserved.");int offset=at+1;byte[] remaining=tail.Skip(offset).ToArray();bool complete=false;
   try{var row=Json.Deserialize<LocalResult>(Encoding.UTF8.GetString(remaining));complete=row!=null&&row.Key!=null&&row.Target!=null&&row.Source!=null&&EnglishSupport.Compatible(row.Source,row.Target);}catch(ArgumentException){}
   if(complete){stream.Position=stream.Length;stream.WriteByte(10);}else{Atomic(RunFile(id,"interrupted-tail.txt"),remaining);stream.SetLength(start+offset);}
  }
 }
 public static void Snapshot(LocalRun run,Dictionary<string,LocalResult> results){
  // Rendering tails the append-only journal. Checkpoints remain constant-size;
  // never rewrite every prior translation after each small inference batch.
  Write(RunFile(run.Id,"display.json"),new{format=1,id=run.Id,language=run.Language,scope=run.Scope,entries=results.Count,journal="results.jsonl",generation=DateTime.UtcNow.Ticks});
 }
}
public static class LocalLanguages {
 public static readonly Dictionary<string,string> All=new Dictionary<string,string>{{"ja","Japanese"},{"en","English"},{"zh-hans","Chinese (Simplified)"},{"zh-hant","Chinese (Traditional)"},{"ko","Korean"},{"ar","Arabic"},{"az","Azerbaijani"},{"be","Belarusian"},{"bg","Bulgarian"},{"bn","Bengali"},{"ca","Catalan"},{"ceb","Cebuano"},{"de","German"},{"es","Spanish"},{"et","Estonian"},{"eu","Basque"},{"fa","Persian"},{"fr","French"},{"gu","Gujarati"},{"he","Hebrew"},{"hi","Hindi"},{"hr","Croatian"},{"hu","Hungarian"},{"hy","Armenian"},{"id","Indonesian"},{"it","Italian"},{"jv","Javanese"},{"ka","Georgian"},{"kk","Kazakh"},{"km","Khmer"},{"lt","Lithuanian"},{"lv","Latvian"},{"ml","Malayalam"},{"mr","Marathi"},{"ms","Malay"},{"my","Burmese"},{"ne","Nepali"},{"nl","Dutch"},{"pa","Punjabi"},{"pl","Polish"},{"ru","Russian"},{"si","Sinhala"},{"sk","Slovak"},{"sl","Slovenian"},{"sr","Serbian"},{"sw","Swahili"},{"ta","Tamil"},{"te","Telugu"},{"th","Thai"},{"tl","Tagalog"},{"ur","Urdu"},{"uz","Uzbek"},{"vi","Vietnamese"}};
}
