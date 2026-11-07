using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
public static class LocalModelSmoke {
 public static int Main(string[] args){try{
  var rows=new List<object>();if(LocalLanguages.All.Count!=53)throw new Exception("Official language count changed");
  foreach(string id in new[]{"0.8b","2b"}){
   LocalTranslation.Install(id,false,CancellationToken.None);
   using(var model=new LocalInference(id,state=>Console.WriteLine(id+" "+state),()=>false)){
    string text=model.Translate("HPを50回復する。","en","Item description",()=>false);if(!text.Contains("50"))throw new Exception("Native number missing");rows.Add(new{model=id,source="HPを50回復する。",target=text,language="en"});Console.WriteLine(id+" en: "+text);
    text=model.Translate("回復薬","fr","Item name",()=>false);rows.Add(new{model=id,source="回復薬",target=text,language="fr"});Console.WriteLine(id+" fr: "+text);
    text=model.Translate("\\C[2]HPを50回復する。\\C[0]","en","Protected description",()=>false);if(!EnglishSupport.Compatible("\\C[2]HPを50回復する。\\C[0]",text))throw new Exception("Protected slots changed");rows.Add(new{model=id,source="\\C[2]HPを50回復する。\\C[0]",target=text,language="en"});Console.WriteLine(id+" controls: "+text);
   }
  }
  File.WriteAllText(args[0],LocalTranslation.Json.Serialize(new{status="PASS",models=2,languages=53,realCalls=rows.Count,translations=rows}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
