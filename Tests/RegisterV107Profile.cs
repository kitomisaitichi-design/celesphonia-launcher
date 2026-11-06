using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
public static class RegisterV107Profile {
 public static int Main(string[] args){try{
  var data=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(args[0]));
  var game=Store.Json.Deserialize<Profile>(Store.Json.Serialize(data["game"]));
  var source=Store.Json.Deserialize<Profile>(Store.Json.Serialize(data["source"]));
  if(UniversalSupport.Identify(game.Root)!="three-in-one-3-3-5-v1-07")throw new Exception("Target not 1.07");
  var settings=Store.Read();var existing=settings.Profiles.ToDictionary(p=>p.Id,p=>Store.Json.Serialize(p));string selection=settings.SelectedId,theme=settings.LauncherTheme;
  var donor=settings.Profiles.FirstOrDefault(p=>Path.GetFullPath(p.Root).Equals(Path.GetFullPath(source.Root),StringComparison.OrdinalIgnoreCase));
  if(donor!=null)RuntimeSupport.CarryForward(donor,game,true);
  if(!settings.Profiles.Any(p=>Path.GetFullPath(p.Root).Equals(Path.GetFullPath(game.Root),StringComparison.OrdinalIgnoreCase)))settings.Profiles.Add(game);
  if(settings.SelectedId==source.Id)settings.SelectedId=game.Id;
  if(existing.Any(x=>Store.Json.Serialize(settings.Profiles.Single(p=>p.Id==x.Key))!=x.Value)||settings.LauncherTheme!=theme)throw new Exception("Old profile/preferences changed");
  Store.Write(settings);File.WriteAllText(args[1],Store.Json.Serialize(new{status="PASS",registeredRoot=game.Root,oldProfilesPreserved=existing.Count,previousSelection=selection,currentSelection=settings.SelectedId,themePreserved=true}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
