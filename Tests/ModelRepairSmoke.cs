using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
public static class ModelRepairSmoke {
 static string live;
 public static int Main(string[] args){live=Path.GetFullPath(args[0]);AppDomain.CurrentDomain.AssemblyResolve+=delegate(object s,ResolveEventArgs e){return e.Name.StartsWith("Celesphonia Portable Switcher,")?Assembly.LoadFrom(live):null;};Assembly.LoadFrom(live);return Run(args);}
 static int Run(string[] args){try{typeof(Store).GetField("Home").SetValue(null,Path.GetFullPath(args[2]));var run=LocalTranslation.Run("af2b45bbd7ba4a87ac45e54ed8cedac9");var options=LocalTranslation.Options(run);var rows=new List<object>();string[] samples={"\\\\n[1]准备好了<测试状态>","这是一个测试。\r","物品","%1的%2 上升到了%3！"," 5. ウィンドウの1行のあたりの高さ(※2)\r","\\I[2518]\\c[27]媚药侵染(超敏感)","\\c[4]等待中\\c[0] en(v(1)>30)"};
 using(var model=new LocalInference(run.Model,options,s=>Console.WriteLine(s),()=>false)){model.Configure(run,options);foreach(string source in samples){model.ReferenceUnit(source);string target=model.Translate(source,"fr","Interface",()=>false);if(!LocalLibrary.Valid(source,target)||source.EndsWith("\r")&&!target.EndsWith("\r")||target.Contains("<br"))throw new Exception("Structure failed: "+source);rows.Add(new{source,target,valid=true});Console.WriteLine(source.Replace("\r","<CR>")+" => "+target.Replace("\r","<CR>"));}}
 if(!LocalLibrary.Valid("%1的%2 上升到了%3！","Le %2 de %1 est monté à %3 !")||LocalLibrary.Valid("\\c[4]HP 50\\c[0]","\\c[0]HP 50\\c[4]")||LocalLibrary.Valid("HP 50","PV 60")||LocalLibrary.Valid("%1获得了%2","%1 a obtenu %3"))throw new Exception("Negative guard check failed");
 LocalTranslation.Write(args[1],new{status="PASS",samples=rows,negativeControlValueTests=true});return 0;
 }catch(Exception e){LocalTranslation.Write(args[1],new{status="FAIL",error=e.ToString()});Console.Error.WriteLine(e);return 1;}}
}
