using System;
using System.IO;
using System.Linq;
using System.Reflection;
public static class V106ControlCarryTests {
 public static int Main(string[] a){Assembly.LoadFrom(a[0]);AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>Assembly.LoadFrom(a[0]);try{return Run(a);}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static int Run(string[] a){
  string parent=Path.Combine(Store.Home,"Control Sessions"),token="v106-carry-test-"+Guid.NewGuid().ToString("N");
  var source=Store.Detect(a[1],"Private carry check");source.Id=token+"-source";
  var target=Store.Detect(a[1],"Private carry target");target.Id=token+"-target";
  string from=Store.Child(parent,source.Id),to=Store.Child(parent,target.Id);Directory.CreateDirectory(Path.Combine(from,"save"));
  try{
   File.WriteAllText(Path.Combine(from,"save/file1.rpgsave"),"independent runtime save sentinel");File.WriteAllText(Path.Combine(from,"panel-settings.json"),"{\"hotkey\":\"Ctrl+Shift+G\",\"opacity\":0.8}");
   File.WriteAllText(Path.Combine(from,"seed.json"),Store.Json.Serialize(new{source=source.SavePath}));source.Id=token+"-remapped";
   string original=Store.Hash(Path.Combine(from,"save/file1.rpgsave"));RuntimeSupport.CarryForward(source,target,true);
   if(Store.Hash(Path.Combine(to,"save/file1.rpgsave"))!=original||Store.Hash(Path.Combine(from,"save/file1.rpgsave"))!=original)throw new Exception("Runtime saves changed");
   if(Store.Hash(Path.Combine(from,"panel-settings.json"))!=Store.Hash(Path.Combine(to,"panel-settings.json")))throw new Exception("Preferences changed");
   if(!File.ReadAllText(Path.Combine(to,"seed.json")).Contains("sourceBackup"))throw new Exception("Verified branch backup absent");
   bool conflict=false;try{RuntimeSupport.CarryForward(source,target,true);}catch(IOException){conflict=true;}if(!conflict)throw new Exception("Existing destination not protected");
   File.WriteAllText(a[2],Store.Json.Serialize(new{status="PASS",checks=4,independentRuntimeSave=true,preferences=true,backup=true,conflictProtection=true}));return 0;
  }finally{foreach(string folder in new[]{from,to})if(Directory.Exists(folder)){Store.CheckLinks(folder);Directory.Delete(folder,true);}}
 }
}
