using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
public static class V1053SourceSelectionTests {
 [STAThread] public static int Main(string[] args){try{
  string id="three-in-one-3-3-5-v1-053";
  var settings=Store.Json.Deserialize<Settings>(File.ReadAllText(args[0]));
  using(var form=new PolishedLauncher(true,settings)){
   var discovered=new List<FoundSource>{
    new FoundSource{Path=args[1],RecipeId=id,Kind="Original archive",Name="Verified creator archive"},
    new FoundSource{Path=args[2],RecipeId=id,Kind="Extracted bundle",Name="Broad library candidate"}
   };
   typeof(PolishedLauncher).GetField("discovered",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,discovered);
   var method=typeof(PolishedLauncher).GetMethod("NativeRecoveryInputs",BindingFlags.Instance|BindingFlags.NonPublic);
   var inputs=(string[])method.Invoke(form,new object[]{id,null});
   var target=MixedSourceRecovery.LoadEmbeddedReference(id);
   var pool=MixedSourceRecovery.Probe(target,inputs,CancellationToken.None,true);
   if(!pool.Complete||pool.Selections.Any(x=>x.Source.Equals(args[2],StringComparison.OrdinalIgnoreCase)))throw new Exception("Broad library was needed or final files remain missing");
   File.WriteAllText(args[3],Store.Json.Serialize(new{status="PASS",selectedSources=inputs,found=pool.FoundCount,required=pool.RequiredCount,
    broadLibraryTraversed=false,actualGuiSelectionMethod=true}));
   Console.WriteLine("PASS actual GUI source selection: "+pool.FoundCount+"/"+pool.RequiredCount+" exact files; broad download library not traversed");
  }return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
