using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
public static class LocalRepairProof {
 public static int Main(string[] args){try{var checks=new List<object>();Action<string,bool> check=(name,pass)=>{checks.Add(new{name,pass});if(!pass)throw new Exception(name);};
  string root=Path.GetFullPath(args[1]);Directory.CreateDirectory(root);
  if(args[0]=="io"){
   string file=Path.Combine(root,"state.json");LocalTranslation.Write(file,new Dictionary<string,string>{{"state","kept"}});var held=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None);var release=new Thread(()=>{Thread.Sleep(230);held.Dispose();});release.Start();var state=LocalTranslation.Read<Dictionary<string,string>>(file);release.Join();check("transient shared-file lock recovers",state["state"]=="kept");
   var writer=new Thread(()=>{for(int i=0;i<60;i++)LocalTranslation.Write(file,new Dictionary<string,string>{{"state",i.ToString()}});});writer.Start();for(int i=0;i<60;i++)check("atomic refresh "+i,LocalTranslation.Read<Dictionary<string,string>>(file).ContainsKey("state"));writer.Join();
  }else if(args[0]=="gpu"){
   LocalTranslation.Install("0.8b",true,CancellationToken.None);var devices=LocalBackend.Devices();check("Vulkan detects available GPU",devices.Count>0);var selected=devices.OrderByDescending(x=>x.Value.Contains("NVIDIA")).First();var modes=new List<object>();
   foreach(string mode in new[]{"cpu","gpu","hybrid"}){var option=new LocalRunOptions{Backend=mode,Device=mode=="cpu"?"":selected.Key,GpuLayers=8};using(var model=new LocalInference("0.8b",option,s=>Console.WriteLine(mode+": "+s),()=>false)){string result=model.Translate("HPを50回復する。","en","Descriptions",()=>false);check(mode+" real inference preserves HP value",result.Contains("50"));modes.Add(new{mode,device=option.Device,result});}}
   LocalTranslation.Write(Path.Combine(root,"backends.json"),new{devices,modes});
  }
  LocalTranslation.Write(Path.Combine(root,args[0]+".json"),new{status="PASS",checks});Console.WriteLine("PASS: "+args[0]);return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
