using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class EditorRepairProbe {
 static string live,output;static List<string> errors=new List<string>();static List<object> checks=new List<object>();
 delegate bool EnumWindow(IntPtr hwnd,IntPtr value);
 [DllImport("user32.dll")]static extern bool EnumThreadWindows(uint thread,EnumWindow callback,IntPtr value);
 [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr hwnd,System.Text.StringBuilder buffer,int count);
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,uint message,IntPtr wparam,IntPtr lparam);
 static void Pump(int ms){var end=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<end){Application.DoEvents();System.Threading.Thread.Sleep(10);}}
 static T Field<T>(object obj,string name){return (T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);}
 static void Invoke(object obj,string name){obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);}
 [STAThread]public static int Main(string[] args){live=Path.GetFullPath(args[0]);output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){return e.Name.StartsWith("Celesphonia Portable Switcher,")?Assembly.LoadFrom(live):null;};Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs e){errors.Add(e.Exception.ToString());};Application.EnableVisualStyles();return Run(args);}
 static int Run(string[] args){try{
  if(args.Length>2)typeof(Store).GetField("Home").SetValue(null,Path.GetFullPath(args[2]));string actualHome=LocalTranslation.Home;var real=LocalTranslation.Runs(null).First(x=>x.Language=="fr"&&x.Root.Contains("1070-"));string journal=LocalTranslation.RunFile(real.Id,"results.jsonl");string root=real.Root;real.Id=Guid.NewGuid().ToString("N");real.Name="Editor repair private diagnostic";real.Scope=LocalTranslation.Categories;typeof(LocalTranslation).GetField("Home").SetValue(null,Path.Combine(output,"Home"));LocalTranslation.Save(real);File.Copy(journal,LocalTranslation.RunFile(real.Id,"results.jsonl"));
  var watch=Stopwatch.StartNew();var covered=EnglishSupport.CoveredLocalSources(root);double coverageMs=watch.Elapsed.TotalMilliseconds;watch.Restart();var comparison=LocalLibrary.Compare(real);double comparisonMs=watch.Elapsed.TotalMilliseconds;
  var pack=new LocalDictionaryPack{Id="diagnostic-pack",Name="Real-size diagnostic only",Family=real.Family,Language="fr",OriginVersion=real.OriginVersion,Entries=comparison.English.Select(x=>new LocalContentEntry{Source=x.Key,Target=x.Value,Role=covered.ContainsKey(x.Key)?covered[x.Key]:null}).ToList()};comparison.Packs[pack.Id]=pack;
  watch.Restart();for(int i=0;i<60;i++)LocalLibrary.Layer(comparison,"pack:"+pack.Id);double sixtyLookupsMs=watch.Elapsed.TotalMilliseconds;
  var folder=Path.Combine(LocalLibrary.Home,"Packs",real.Family,"fr");Directory.CreateDirectory(folder);LocalTranslation.Write(Path.Combine(folder,pack.Id+".json"),pack);
  using(var form=new LocalTranslationEditor(real,Color.FromArgb(15,17,25),Color.FromArgb(27,30,42),Color.White,Color.CornflowerBlue)){
   watch.Restart();form.Show();var grid=Field<DataGridView>(form,"grid");var end=DateTime.UtcNow.AddSeconds(25);while((grid.RowCount<100||Field<bool>(form,"busy"))&&DateTime.UtcNow<end)Pump(20);double loadMs=watch.Elapsed.TotalMilliseconds;if(grid.RowCount<100)throw new Exception("Editor did not load");
   var filterBoxes=Field<Dictionary<string,CheckBox>>(form,"originFilters");var textSizes=filterBoxes.Values.Select(c=>new{text=c.Text,width=c.ClientSize.Width,height=c.ClientSize.Height,padding=c.Padding.Horizontal,measured=TextRenderer.MeasureText(c.Text,c.Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width}).ToArray();
   watch.Restart();for(int i=0;i<5;i++)Invoke(form,"Filter");double filterMs=watch.Elapsed.TotalMilliseconds/5;
   watch.Restart();for(int i=1;i<=5;i++){grid.ClearSelection();grid.Rows[i].Selected=true;}double selectionMs=watch.Elapsed.TotalMilliseconds/5;
   using(var picture=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(picture,new Rectangle(0,0,form.Width,form.Height));picture.Save(Path.Combine(output,"editor.png"));}
   Invoke(form,"ExportMenu");Pump(30);ContextMenuStrip menu=null;EnumThreadWindows(GetCurrentThreadId(),delegate(IntPtr hwnd,IntPtr value){var found=Control.FromHandle(hwnd) as ContextMenuStrip;if(found!=null&&found.Visible)menu=found;return true;},IntPtr.Zero);bool foundMenu=menu!=null;
   var cancel=new System.Windows.Forms.Timer{Interval=70};cancel.Tick+=delegate{EnumThreadWindows(GetCurrentThreadId(),delegate(IntPtr hwnd,IntPtr value){var title=new System.Text.StringBuilder(400);GetWindowText(hwnd,title,title.Capacity);if(title.ToString().StartsWith("Export "))SendMessage(hwnd,0x0010,IntPtr.Zero,IntPtr.Zero);return true;},IntPtr.Zero);};cancel.Start();
   if(menu!=null){var item=menu.Items[0];int x=item.Bounds.Left+item.Bounds.Width/2,y=item.Bounds.Top+item.Bounds.Height/2;IntPtr point=(IntPtr)((y<<16)|x);SendMessage(menu.Handle,0x0201,(IntPtr)1,point);SendMessage(menu.Handle,0x0202,IntPtr.Zero,point);Pump(250);}cancel.Stop();cancel.Dispose();form.Close();Pump(30);
   LocalTranslation.Write(Path.Combine(output,"report.json"),new{status="MEASURED",assemblyPath=typeof(LocalLibrary).Assembly.Location,assemblyHash=Store.Hash(typeof(LocalLibrary).Assembly.Location),coverageMs,comparisonMs,loadMs,filterMs,selectionMs,packEntries=pack.Entries.Count,sixtyPackLookupsMs=sixtyLookupsMs,textSizes,actualExportMenuFound=foundMenu,errors,rows=grid.RowCount,userDataChanged=false});
  }
  Console.WriteLine("Diagnostic recorded; errors="+errors.Count+"; 60 pack lookups="+sixtyLookupsMs.ToString("F1")+" ms");return 0;
 }catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Console.Error.WriteLine(e);return 1;}}
}
