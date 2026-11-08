using System;using System.IO;using System.Linq;using System.Drawing;using System.Reflection;using System.Collections.Generic;using System.Windows.Forms;
public static class LauncherLanguageSmoke {
 static string assembly;
 [STAThread]public static int Main(string[] args){assembly=Path.GetFullPath(args[0]);AppDomain.CurrentDomain.AssemblyResolve+=delegate(object s,ResolveEventArgs e){return e.Name.StartsWith("Celesphonia Portable Switcher,")?Assembly.LoadFrom(assembly):null;};Assembly.LoadFrom(assembly);return Run(args);}
 static IEnumerable<Control> All(Control c){yield return c;foreach(Control child in c.Controls)foreach(var x in All(child))yield return x;}
 static int Run(string[] args){try{
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);string root=Path.GetFullPath(args[1]);typeof(Store).GetField("Home").SetValue(null,root);typeof(LocalTranslation).GetField("Home").SetValue(null,Path.Combine(root,"LocalHome"));
  string error=null;Application.ThreadException+=delegate(object s,System.Threading.ThreadExceptionEventArgs e){error=e.Exception.ToString();};
  var settings=new Settings{Profiles=new List<Profile>(),SearchRoots=new List<string>(),SuppressedRoots=new List<string>(),InstallRoot=Path.Combine(root,"Games"),LauncherTheme="Midnight"};
  var checks=new List<object>();LauncherLanguage.Initialize(root);
  if(LauncherLanguage.Resolve("fr","ja-JP")!="fr"||LauncherLanguage.Resolve("auto","ja-JP")!="ja"||LauncherLanguage.Resolve(null,"pt-BR")!="pt"||LauncherLanguage.Resolve("auto","xx-ZZ")!="en")throw new Exception("Locale precedence");
  using(var f=new PolishedLauncher(true,settings)){
   f.ClientSize=new Size(1080,760);f.Show();LauncherLanguage.Attach(f);Application.DoEvents();
   foreach(string code in LauncherLanguage.Codes){LauncherLanguage.Select(code,false);Application.DoEvents();if(LauncherLanguage.Current!=code)throw new Exception(code+": "+LauncherLanguage.Problem);
    foreach(string page in new[]{"Play","Files","English","Local TL","Install","Saves","Help"}){var tab=All(f).OfType<Button>().Single(b=>b.Parent==f&&LauncherLanguage.Source(b)==page);tab.PerformClick();Application.DoEvents();if(tab.Left<0||tab.Right>f.ClientSize.Width||tab.Top<0||tab.Bottom>f.ClientSize.Height)throw new Exception(code+" navigation bounds");}
    var globe=All(f).OfType<LauncherLanguageButton>().Single();globe.PerformClick();Application.DoEvents();var owned=(System.Runtime.CompilerServices.ConditionalWeakTable<Control,ContextMenuStrip>)typeof(LauncherLanguage).GetField("menus",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);ContextMenuStrip menu;if(!owned.TryGetValue(globe,out menu)||menu.Items.Count!=18||!menu.Items.Cast<ToolStripItem>().Any(x=>x.Text=="日本語"))throw new Exception("Native language menu");menu.Close();Application.DoEvents();globe.PerformClick();Application.DoEvents();menu.Close();Application.DoEvents();
    if(code=="en"||code=="ja"||code=="ar"||code=="de"){using(var bmp=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(args[2],"launcher-"+code+".png"));}}
    checks.Add(new{language=code,navPages=7,canonicalActions=true,rtl=f.RightToLeftLayout});if(error!=null)throw new Exception(error);
   }
   LauncherLanguage.Select("ja",false);using(var notes=new NotesReader(0)){notes.Show(f);Application.DoEvents();notes.SelectSource(1);LauncherLanguage.Select("ar",false);Application.DoEvents();notes.SelectSource(2);notes.Close();}LauncherLanguage.Select("en",false);f.Close();
  }
  // Persist/restart resolution in the isolated root, never the user's preference file.
  LauncherLanguage.Select("ja",true);var preference=Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(root,"launcher-language.json")));if(preference["Language"]!="ja")throw new Exception("Preference save");LauncherLanguage.Select("auto",true);
  if(error!=null)throw new Exception(error);File.WriteAllText(Path.Combine(args[2],"ui-smoke.json"),Store.Json.Serialize(new{status="PASS",languages=16,pageSwitches=112,checks=checks,localePrecedence=true,savedSetting=true,notesSwitch=true,wholeTranslationCoverageVerified=false}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);File.WriteAllText(Path.Combine(args[2],"ui-error.txt"),e.ToString());return 1;}}
}
