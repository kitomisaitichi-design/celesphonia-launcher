using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
public static class LocalNativeSetup {
 public static int Main(string[] args){try{
  if(args[0]=="finish"){
   var proof=LocalTranslation.Read<Dictionary<string,object>>(args[1]);var variants=(System.Collections.IEnumerable)proof["variants"];
   foreach(Dictionary<string,object> item in variants){var p=Store.Detect(Convert.ToString(item["root"]),"Native local proof");string id=Convert.ToString(item["run"]);LocalTranslation.Select(p,id,false);LocalTranslation.Prepare(p);LocalTranslation.Delete(id);if(Store.Hash(Path.Combine(Store.WebRoot(p.Root),"index.html"))!=Convert.ToString(item["entryHash"]))throw new Exception("Entry not restored");}
   Console.WriteLine("Original game entries restored; private native runs recycled");return 0;
  }
  var settings=Store.Read();var games=new[]{settings.Profiles.First(p=>p.Root.Contains("1070-")),settings.Profiles.First(p=>p.Root.Contains("Maple 1.7.2"))};var rows=new List<object>();
  using(var model=new LocalInference("0.8b",s=>Console.WriteLine(s),()=>false))foreach(var p in games){
   string entry=Path.Combine(Store.WebRoot(p.Root),"index.html"),entryHash=Store.Hash(entry);var run=LocalTranslation.Create(p,"Private native French proof","0.8b","fr",LocalTranslation.Categories);var units=LocalTextIndex.Build(run,()=>false);if(units.Count<5000)throw new Exception("Native index unexpectedly small: "+units.Count);
   var chosen=new[]{units.First(x=>x.Context=="Items/15"&&x.Category=="Names"),units.First(x=>x.Context=="Items/15"&&x.Category=="Descriptions"&&x.Source.Contains("\n"))};var results=new Dictionary<string,LocalResult>();
   foreach(var unit in chosen){var target=string.Join("\n",unit.Source.Split('\n').Select(line=>model.Translate(line,"fr",unit.Category+"; "+unit.Context,()=>false)));var row=new LocalResult{Key=unit.Key,Source=unit.Source,Category=unit.Category,Context=unit.Context,Target=target};results[row.Key]=row;Console.WriteLine(p.Name+" "+row.Source+" => "+target);}
   File.WriteAllLines(LocalTranslation.RunFile(run.Id,"results.jsonl"),results.Values.Select(x=>LocalTranslation.Json.Serialize(x)),new UTF8Encoding(false));run.Done=results.Count;run.State="Paused";LocalTranslation.Save(run);LocalTranslation.Snapshot(run,results);LocalTranslation.Select(p,run.Id,true);
   rows.Add(new{variant=p.Root.Contains("Maple")?"maple":"creator",root=p.Root,run=run.Id,indexed=units.Count,entryHash=entryHash,expected=results.Values.ToArray()});
  }
  File.WriteAllText(args[1],LocalTranslation.Json.Serialize(new{status="PASS",variants=rows,curatedPacksChanged=false}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
