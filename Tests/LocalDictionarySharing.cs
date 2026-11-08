using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.IO.Compression;
using System.Collections.Generic;

public static class GlossaryProof {
 static string output;static List<object> checks=new List<object>();
 static void Check(string name,bool ok){checks.Add(new{name,pass=ok});if(!ok)throw new Exception(name);}
 static void Pump(int ms){var t=DateTime.UtcNow;while((DateTime.UtcNow-t).TotalMilliseconds<ms){Application.DoEvents();System.Threading.Thread.Sleep(15);}}
 static T Field<T>(object value,string name){return (T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);}
 static void Capture(Form f,string name){using(var bitmap=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bitmap,new Rectangle(0,0,f.Width,f.Height));bitmap.Save(Path.Combine(output,name+".png"));}}
 static void Call(object value,string name){value.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,null);}
 static void Reject(string name,Action operation){bool rejected=false;try{operation();}catch(Exception){rejected=true;}Check(name,rejected);}
 [STAThread]public static int Main(string[] args){output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);try{
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string home=Path.Combine(output,"Home");typeof(LocalTranslation).GetField("Home").SetValue(null,home);
  var profile=new Profile{Root=Path.GetFullPath(args[1]),Name="Three-in-One sharing fixture"};
  var run=LocalTranslation.Create(profile,"French · terminology & sharing proof","0.8b","fr",LocalTranslation.Categories);var covered=EnglishSupport.CoveredLocalSources(run.Root);
  string item="物品",term="\\I[2518]\\c[27]媚药侵染(超敏感)",fr="\\I[2518]\\c[27]Imprégnation aphrodisiaque (hypersensibilité)";
  Check("reported source and item are within curated English coverage",covered.ContainsKey(item)&&covered.ContainsKey(term));
  Check("technical color and expressions stay outside coverage",!covered.ContainsKey("black")&&!covered.ContainsKey("$gameVariables.value(1)"));
  var seed=new LocalResult{Key=LocalLibrary.Key(covered[item],item),Source=item,Target="Objets",Category=covered[item],Context="EnglishCoverage",Fields=EnglishSupport.PrepareLocalField(run.Root,item,"Objets")};
  File.WriteAllText(LocalTranslation.RunFile(run.Id,"results.jsonl"),LocalTranslation.Json.Serialize(seed)+"\n",Encoding.UTF8);string journalHash=Store.Hash(LocalTranslation.RunFile(run.Id,"results.jsonl"));
  var preset=LocalLibrary.Profile(run,LocalTranslation.Options(run));Check("French preset uses native aphrodisiaque glossary",preset.Terms.Any(x=>x.Source=="媚药"&&x.Target=="aphrodisiaque"));
  string policy=LocalLibrary.Policy(run,LocalTranslation.Options(run),term,"Aphrodisiac Infection");preset.Id="proof-profile";preset.Name="Proof glossary";preset.Terms.Add(new LocalGlossaryTerm{Source="unrelated-test-term",Target="unrelated"});LocalLibrary.SaveProfile(preset);var options=LocalTranslation.Options(run);options.GlossaryProfileId=preset.Id;LocalTranslation.SetOptions(run.Id,options);
  Check("unrelated glossary term does not invalidate this source",policy==LocalLibrary.Policy(run,options,term,"Aphrodisiac Infection"));
  string packFile=Path.Combine(output,"incoming.json"),future=covered.Keys.First(x=>x.Length>12&&!x.Contains("\\")&&!x.Contains("\n")&&!System.Text.RegularExpressions.Regex.IsMatch(x,@"\d"));string futureRole=covered[future];covered.Remove(future);
  var pack=new LocalDictionaryPack{Id="proof-pack",Name="French shared terminology",Family=run.Family,OriginVersion=run.OriginVersion,Language="fr",Entries=new List<LocalContentEntry>{new LocalContentEntry{Source=term,Target=fr,Role=covered[term]},new LocalContentEntry{Source=item,Target="Inventaire",Role=covered[item]},new LocalContentEntry{Source=future,Target="Texte partagé conservé",Role=futureRole},new LocalContentEntry{Source="black",Target="noir",Role="Labels"}}};LocalTranslation.Write(packFile,pack);
  var preview=LocalLibrary.Parse(packFile,run);Check("preview retains future/technical entries inactive",preview.Applicable==2&&preview.Inactive==2&&preview.Conflicts==1);LocalLibrary.Import(run,preview,true);
  Check("import takes precedence over model",LocalLibrary.Compare(run).Effective[item]=="Inventaire");
  var edit=new LocalEdit{Key=seed.Key,Source=item,Category=covered[item],Context="EnglishCoverage",Target="Objets (révisés)"};LocalTranslation.Edit(run.Id,edit);Check("authored override wins without replacing model",LocalLibrary.Compare(run).Effective[item]==edit.Target&&LocalLibrary.Compare(run).Model[item]=="Objets");
  string exported=Path.Combine(output,"dictionary-pack.json");LocalLibrary.Export(exported,run,new[]{term,item},"effective",true);string exportedText=File.ReadAllText(exported);Check("portable export excludes install paths, saves and adapter Fields",!exportedText.Contains(run.Root)&&!exportedText.Contains(home)&&!exportedText.Contains("\"Fields\"")&&!exportedText.Contains("rpgsave"));
  var roundtrip=LocalLibrary.Parse(exported,run);Check("selected-only portable roundtrip includes optional glossary",roundtrip.Applicable==2&&roundtrip.Pack.Entries.Count==2&&roundtrip.Pack.Glossary!=null);
  string mapFile=Path.Combine(output,"source-map.json");LocalTranslation.Write(mapFile,new Dictionary<string,string>{{item,"Objets"}});Check("source-target JSON auto-detected",LocalLibrary.Parse(mapFile,run).Applicable==1);
  File.WriteAllText(mapFile,LocalTranslation.Json.Serialize(seed)+"\n"+LocalTranslation.Json.Serialize(new{Source=term,Target=fr,Category=covered[term]})+"\n",Encoding.UTF8);Check("prior local JSONL auto-detected without importing Fields",LocalLibrary.Parse(mapFile,run).Applicable==2);
  string zipFile=Path.Combine(output,"shared.zip");using(var zip=ZipFile.Open(zipFile,ZipArchiveMode.Create)){zip.CreateEntryFromFile(exported,"dictionary-pack.json");}Check("bounded ZIP content auto-detected",LocalLibrary.Parse(zipFile,run).Applicable==2);
  string badZip=Path.Combine(output,"unsafe.zip");using(var zip=ZipFile.Open(badZip,ZipArchiveMode.Create)){zip.CreateEntryFromFile(exported,"../dictionary-pack.json");}Reject("unsafe archive path rejected",()=>LocalLibrary.Parse(badZip,run));
  pack.Language="de";LocalTranslation.Write(mapFile,pack);Reject("unrelated language rejected",()=>LocalLibrary.Parse(mapFile,run));pack.Language="fr";pack.Family="maple";LocalTranslation.Write(mapFile,pack);Reject("unrelated game family rejected",()=>LocalLibrary.Parse(mapFile,run));
  LocalTranslation.Write(mapFile,new[]{new{Source=term,Target="Control codes removed"}});Check("changed protected slots rejected",LocalLibrary.Parse(mapFile,run).Invalid==1);
  LocalTranslation.Write(mapFile,new[]{new{Source=item,Target="Objets"},new{Source=item,Target="Inventaire"}});Reject("conflicting duplicate sources rejected",()=>LocalLibrary.Parse(mapFile,run));
  File.WriteAllText(mapFile,"{\"incomplete\":",Encoding.UTF8);Reject("truncated input reported",()=>LocalLibrary.Parse(mapFile,run));
  covered[future]=futureRole;LocalLibrary.Publish(run);Check("later compatible coverage activates retained exact source",LocalLibrary.Compare(run).Imported.ContainsKey(future));
  string root2=Path.Combine(output,"another-install-root");Directory.CreateDirectory(root2);var next=new LocalRun{Id=Guid.NewGuid().ToString("N"),Root=root2,GameKey=LocalTranslation.GameKey(root2),Name="Later compatible install",Model=run.Model,ModelHash=run.ModelHash,Language=run.Language,Family=run.Family,OriginVersion="three-in-one-3-3-5-v1-08",Scope=run.Scope,Owner=Store.Home};LocalTranslation.Save(next);
  var coveredCache=(Dictionary<string,Dictionary<string,string>>)typeof(EnglishSupport).GetField("coveredLocal",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);coveredCache[root2]=new Dictionary<string,string>(covered);
  var catalogCache=(Dictionary<string,Dictionary<string,object>>)typeof(EnglishSupport).GetField("localCatalogs",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);catalogCache[root2]=catalogCache[run.Root];
  LocalLibrary.Carryover(next);Check("new root carries exact authored text and selected dictionary",LocalLibrary.Compare(next).User[item]==edit.Target&&LocalTranslation.Options(next).PackIds.Contains("proof-pack"));
  var changed=coveredCache[root2];changed[item]="IncompatibleRole";Check("changed display role does not reuse old model or authored text",!LocalLibrary.Compare(next).Model.ContainsKey(item)&&!LocalLibrary.Compare(next).User.ContainsKey(item));changed[item]=covered[item];
  string original=item;Check("changed native source is not fuzzy applied",!LocalLibrary.Compare(next).Effective.ContainsKey(original+" [changed]"));
  LocalLibrary.Batch(run,new[]{seed},"original");var layer=LocalTranslation.Read<Dictionary<string,object>>(LocalTranslation.RunFile(run.Id,"layers.json"));Check("leave-original bulk operation recorded",LocalTranslation.Edits(run.Id)[seed.Key].LeaveOriginal);
  LocalLibrary.Batch(run,new[]{seed},"clear");Check("clear override reveals imported target",LocalLibrary.Compare(run).Effective[item]=="Inventaire");
  LocalTranslation.Edit(run.Id,edit);LocalLibrary.Publish(run);LocalTranslation.Write(Path.Combine(output,"covered.json"),covered);LocalTranslation.Write(Path.Combine(output,"runtime-config.json"),new{run.Id,run.GameKey,home,binding=LocalTranslation.BindingPath(run.Root)});LocalTranslation.Write(LocalTranslation.BindingPath(run.Root),new LocalBinding{RunId=run.Id,Enabled=true});
  using(var window=new LocalTranslationEditor(run,Color.FromArgb(15,17,25),Color.FromArgb(27,30,42),Color.White,Color.CornflowerBlue)){
   window.Show();Pump(3500);var grid=Field<DataGridView>(window,"grid");var search=Field<TextBox>(window,"search");var target=Field<TextBox>(window,"target");Check("three comparison text columns and native multi-select",grid.Columns.Count==4&&grid.MultiSelect&&grid.SelectionMode==DataGridViewSelectionMode.FullRowSelect&&grid.RowCount>100);
   grid.ClearSelection();for(int i=1;i<4;i++)grid.Rows[i].Selected=true;Pump(30);Check("range selection keeps three stable identities",Field<HashSet<string>>(window,"selected").Count==3);search.Text="物品";Pump(600);Check("filter retains selected source identities",Field<HashSet<string>>(window,"selected").Count>=3);search.Clear();Pump(500);
   search.Text=term;Pump(500);grid.CurrentCell=grid.Rows[0].Cells[1];Call(window,"SelectRow");Check("English reference and target shown for reported term",Field<TextBox>(window,"english").Text.Length>0&&target.Text==fr);Capture(window,"comparison-after");
   target.Text=fr;target.Modified=true;string bound=Field<TextBox>(window,"native").Text;search.Text="物品";Pump(500);Check("unsaved edit stays bound to its original source",Field<TextBox>(window,"native").Text==bound&&target.Modified);target.Modified=false;
   window.ClientSize=new Size(1000,650);Pump(200);Check("minimum layout controls fit client bounds",window.Controls.Cast<Control>().Where(c=>c.Visible).All(c=>c.Right<=window.ClientSize.Width+1&&c.Bottom<=window.ClientSize.Height+1));window.Close();
  }
  using(var glossary=new LocalGlossaryWindow(run,Color.FromArgb(15,17,25),Color.FromArgb(27,30,42),Color.White,Color.CornflowerBlue)){glossary.Show();Pump(400);Capture(glossary,"glossary-after");glossary.Close();}
  Check("all successful original journal bytes retained",journalHash==Store.Hash(LocalTranslation.RunFile(run.Id,"results.jsonl")));
  LocalTranslation.Write(Path.Combine(output,"proof.json"),new{status="PASS",checks,sourceRun=run.Id,laterRun=next.Id,nativeGameFilesWritten=false,userSavesWritten=false});Console.WriteLine("PASS: "+checks.Count+" glossary, portable dictionary, compatibility and native editor checks");return 0;
 }catch(Exception e){LocalTranslation.Write(Path.Combine(output,"proof.json"),new{status="FAIL",checks,error=e.ToString()});Console.Error.WriteLine(e);return 1;}}
}
