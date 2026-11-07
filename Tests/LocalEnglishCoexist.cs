using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
public class LocalEnglishCoexist {
 public static int Main(string[] args){try{
  var proof=LocalTranslation.Read<Dictionary<string,object>>(args[0]);var rows=new List<object>();
  foreach(Dictionary<string,object> row in (IEnumerable)proof["variants"]){
   var profile=Store.Detect(Convert.ToString(row["root"]),"Local translation coexistence");var id=Convert.ToString(row["run"]);
   if(!EnglishSupport.Status(profile).Enabled)throw new Exception("Expected existing English overlay");
   EnglishSupport.Restore(profile);if(EnglishSupport.Status(profile).Enabled)throw new Exception("English stayed enabled");
   if(!LocalTranslation.Prepare(profile))throw new Exception("Local selection lost when English removed");
   EnglishSupport.Apply(profile);if(!EnglishSupport.Status(profile).Enabled)throw new Exception("English did not reapply");
   if(!LocalTranslation.Prepare(profile))throw new Exception("Local selection lost when English reapplied");
   LocalTranslation.Select(profile,id,false);if(!EnglishSupport.Status(profile).Enabled)throw new Exception("Disabling local changed English");
   LocalTranslation.Select(profile,id,true);if(!EnglishSupport.Status(profile).Enabled)throw new Exception("Reenabling local changed English");
   EnglishSupport.Verify(profile.Root);rows.Add(new {root=profile.Root,englishOffOn=true,localOffOn=true,underlyingEnglishVerified=true});
  }
  File.WriteAllText(args[1],LocalTranslation.Json.Serialize(new{status="PASS",variants=rows}));Console.WriteLine("PASS: English/local entry coexistence in both games");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
