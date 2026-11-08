using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Windows.Forms;

// Presentation only. Native game data, action IDs and model target languages never use this catalog.
public static class LauncherLanguage {
 public static readonly string[] Codes={"en","zh","es","fr","de","ja","ko","pt","ru","ar","hi","bn","id","it","tr","vi"};
 public static readonly string[] Names={"English","简体中文","Español","Français","Deutsch","日本語","한국어","Português","Русский","العربية","हिन्दी","বাংলা","Bahasa Indonesia","Italiano","Türkçe","Tiếng Việt"};
 public static string Current="en",Preference="auto",Problem="";
 public static bool Rtl {get{return Current=="ar";}}
 public static event Action Changed;
 static string home;static bool started;static Dictionary<string,object> resources;
 static Dictionary<string,string> strings=new Dictionary<string,string>();static readonly Dictionary<string,string> reverse=new Dictionary<string,string>();
 static readonly Dictionary<string,string> cache=new Dictionary<string,string>();
 sealed class Template{public string Source,Target,Prefix,Anchor;public Regex Pattern;}
 static List<Template> templates=new List<Template>();
 static readonly ConditionalWeakTable<Control,Binding> bindings=new ConditionalWeakTable<Control,Binding>();
 static readonly ConditionalWeakTable<ToolStripItem,ItemBinding> items=new ConditionalWeakTable<ToolStripItem,ItemBinding>();
 static readonly ConditionalWeakTable<Control,ContextMenuStrip> menus=new ConditionalWeakTable<Control,ContextMenuStrip>();
 static readonly ToolTip tips=new ToolTip{AutoPopDelay=15000,InitialDelay=450,ReshowDelay=100};
 public static string Resolve(string saved,string system){if(saved!="auto"&&Codes.Contains(saved))return saved;string code=(system??"").Replace('_','-').ToLowerInvariant().Split('-')[0];return Codes.Contains(code)?code:"en";}
 public static void Initialize(string root){
  if(started)return;started=true;home=root;
  try{string path=Path.Combine(home,"launcher-language.json");if(File.Exists(path)){var d=Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(path,Encoding.UTF8));string v;if(d.TryGetValue("Language",out v))Preference=v;}}
  catch(Exception e){Problem="Could not read launcher language preference: "+e.Message;}
  Current=Resolve(Preference,CultureInfo.CurrentUICulture.Name);Load();
  Application.Idle+=Idle;
 }
 static Dictionary<string,object> Object(object v){return v as Dictionary<string,object>;}
 static void Load(){
  strings=new Dictionary<string,string>();templates.Clear();cache.Clear();reverse.Clear();if(Current=="en")return;
  try{
   if(resources==null){var root=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(home,"mod-recipes.json"),Encoding.UTF8));resources=Object(root["LauncherLocalization"]);}
   var packs=Object(resources["Packs"]);var pack=Object(packs[Current]);byte[] bytes=Convert.FromBase64String((string)pack["Data"]);string json;
   using(var input=new MemoryStream(bytes))using(var gzip=new GZipStream(input,CompressionMode.Decompress))using(var output=new MemoryStream()){gzip.CopyTo(output);var raw=output.ToArray();using(var sha=System.Security.Cryptography.SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(raw)).Replace("-","").ToLowerInvariant();if(hash!=(string)pack["Sha256"])throw new IOException("Language catalog checksum mismatch.");}json=Encoding.UTF8.GetString(raw);}
   var data=Store.Json.Deserialize<Dictionary<string,object>>(json);foreach(var p in Object(data["Strings"]))strings[p.Key]=(string)p.Value;foreach(var p in strings)if(p.Value!=p.Key&&!strings.ContainsKey(p.Value)&&!reverse.ContainsKey(p.Value))reverse[p.Value]=p.Key;
   foreach(var o in (System.Collections.IEnumerable)data["Templates"]){string key=(string)o,target;if(!strings.TryGetValue(key,out target))continue;string pattern=Regex.Replace(Regex.Escape(key),@"\\\{(\d+)\\\}",m=>"(?<v"+m.Groups[1].Value+">.*?)");int first=key.IndexOf('{');if(first<0)continue;templates.Add(new Template{Source=key,Target=target,Prefix=key.Substring(0,first),Anchor=Regex.Split(key,@"\{\d+\}").OrderByDescending(x=>x.Length).First(),Pattern=new Regex("^"+pattern+"$",RegexOptions.Singleline,TimeSpan.FromMilliseconds(30))});}
  }catch(Exception e){Current="en";strings.Clear();templates.Clear();Problem="Launcher language resources could not be loaded. English is available. "+e.Message;}
 }
 public static string Filter(string source){var parts=source.Split('|');for(int i=0;i<parts.Length;i+=2)parts[i]=T(parts[i]);return string.Join("|",parts);}
 public static string T(string source){
  if(string.IsNullOrEmpty(source)||Current=="en")return source;string target;if(strings.TryGetValue(source,out target))return target;if(cache.TryGetValue(source,out target))return target;
  foreach(var t in templates){if(!source.StartsWith(t.Prefix,StringComparison.Ordinal)||source.IndexOf(t.Anchor,StringComparison.Ordinal)<0)continue;var m=t.Pattern.Match(source);if(!m.Success)continue;target=Regex.Replace(t.Target,@"\{(\d+)\}",x=>m.Groups["v"+x.Groups[1].Value].Value);if(cache.Count>2000)cache.Clear();cache[source]=target;return target;}return source;
 }
 static string Canonical(string text){string v;return text!=null&&reverse.TryGetValue(text,out v)?v:text;}
 public static string Source(Control c){Binding b;return bindings.TryGetValue(c,out b)?b.Source:c.Text;}
 public static void Select(string language,bool persist=true){
  if(language!="auto"&&!Codes.Contains(language))throw new ArgumentException("Unsupported launcher language.");Preference=language;Current=Resolve(language,CultureInfo.CurrentUICulture.Name);Problem="";Load();
  string saveError=null;if(persist)try{string path=Path.Combine(home,"launcher-language.json"),temp=path+".tmp";File.WriteAllText(temp,Store.Json.Serialize(new Dictionary<string,string>{{"Language",language}}),Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}catch(Exception e){if(!(e is IOException)&&!(e is UnauthorizedAccessException))throw;saveError="Could not save the launcher language. This choice applies to this session only.\r\n\r\n"+e.Message;Problem=saveError;}
  try{foreach(Form f in Application.OpenForms.Cast<Form>().ToArray()){Apply(f);f.Invalidate(true);}var callback=Changed;if(callback!=null)callback();}finally{}if(saveError!=null)Show(saveError,"Launcher language",MessageBoxButtons.OK,MessageBoxIcon.Warning);
 }
 static void Idle(object sender,EventArgs e){foreach(Form f in Application.OpenForms.Cast<Form>().ToArray())if(!f.IsDisposed)Attach(f);}
 static void Apply(Control c){Attach(c);Binding b;if(bindings.TryGetValue(c,out b))b.Refresh();foreach(Control child in c.Controls)Apply(child);}
 public static void Attach(Control c){
  if(c==null||c.IsDisposed)return;Binding b;if(bindings.TryGetValue(c,out b))return;b=new Binding(c);bindings.Add(c,b);c.ControlAdded+=delegate(object s,ControlEventArgs e){Attach(e.Control);};foreach(Control child in c.Controls)Attach(child);
  var combo=c as ComboBox;if(combo!=null){if(combo.DrawMode==DrawMode.Normal){combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem+=delegate(object sender,DrawItemEventArgs e){e.DrawBackground();if(e.Index>=0)DrawText(e.Graphics,combo.Items[e.Index].ToString(),e.Font,e.Bounds,e.ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);e.DrawFocusRectangle();};}}
  var grid=c as DataGridView;if(grid!=null){grid.CellFormatting+=delegate(object s,DataGridViewCellFormattingEventArgs e){if(e.ColumnIndex<0||!(e.Value is string))return;string n=grid.Columns[e.ColumnIndex].Name.ToLowerInvariant();if(n=="status"||n=="category"||n=="origin")e.Value=T((string)e.Value);};}
  b.Refresh();
 }
 sealed class Binding {
  public string Source;readonly Control control;bool writing;Font ownedFont;float originalSize;FontStyle originalStyle;string accessible;readonly Dictionary<DataGridViewColumn,string> headers=new Dictionary<DataGridViewColumn,string>();
  public Binding(Control c){control=c;Source=Canonical(c.Text);accessible=c.AccessibleName;originalSize=c.Font.Size;originalStyle=c.Font.Style;c.Disposed+=delegate{if(ownedFont!=null)ownedFont.Dispose();};c.TextChanged+=delegate{if(writing||c.IsDisposed)return;Source=Canonical(c.Text);Refresh();};c.SizeChanged+=delegate{if(!writing)Fit();};}
  bool IsText {get{return control is Label||control is ButtonBase||control is GroupBox||control is Form||control is TabPage;}}
  public void Refresh(){if(control.IsDisposed)return;writing=true;try{
   if(IsText)control.Text=T(Source);
   if(!string.IsNullOrEmpty(accessible))control.AccessibleName=T(accessible);var grid=control as DataGridView;if(grid!=null)foreach(DataGridViewColumn col in grid.Columns){if(!headers.ContainsKey(col))headers[col]=Canonical(col.HeaderText);col.HeaderText=T(headers[col]);}
   if(IsText){string family=Current=="hi"||Current=="bn"?"Nirmala UI":Current=="zh"?"Microsoft YaHei UI":Current=="ja"?"Yu Gothic UI":Current=="ko"?"Malgun Gothic":"Segoe UI";if(control.Font.Name!=family)SetFont(new Font(family,originalSize,originalStyle));control.RightToLeft=Rtl?RightToLeft.Yes:RightToLeft.No;var form=control as Form;if(form!=null)form.RightToLeftLayout=Rtl;Fit();}
  }finally{writing=false;}}
  void SetFont(Font f){var old=ownedFont;ownedFont=f;control.Font=f;if(old!=null)old.Dispose();}
  static Size Measure(string text,FontFamily family,float size,FontStyle style){using(var f=new Font(family,size,style))return TextRenderer.MeasureText(text,f);}
  void Fit(){if(!IsText||control.IsDisposed||control.Width<35)return;if(control is ButtonBase){float size=originalSize;int width=control.Width-16;while(size>8.0f&&Measure(control.Text,control.Font.FontFamily,size,originalStyle).Width>width)size-=.5f;if(Math.Abs(control.Font.Size-size)>.1)SetFont(new Font(control.Font.FontFamily,size,originalStyle));}if(Current!="en"&&(control is Label||control is ButtonBase))tips.SetToolTip(control,control.Text);}
 }
 sealed class ItemBinding {public string Source;public bool Writing;public ItemBinding(ToolStripItem item){Source=item.Text;item.TextChanged+=delegate{if(Writing)return;Source=item.Text;Refresh(item);};}public void Refresh(ToolStripItem item){Writing=true;try{item.Text=T(Source);}finally{Writing=false;}}}
 static void TranslateItems(ToolStripItemCollection all){foreach(ToolStripItem item in all){ItemBinding b;if(!items.TryGetValue(item,out b)){b=new ItemBinding(item);items.Add(item,b);}b.Refresh(item);var sub=item as ToolStripDropDownItem;if(sub!=null)TranslateItems(sub.DropDownItems);}}
 public static void Menu(ContextMenuStrip menu){menu.Opening+=delegate{if((string)menu.Tag=="LauncherLanguage")return;TranslateItems(menu.Items);menu.RightToLeft=Rtl?RightToLeft.Yes:RightToLeft.No;};}
 public static void LanguageMenu(Control anchor){ContextMenuStrip menu;if(!menus.TryGetValue(anchor,out menu)){menu=LauncherMenuStyle.Create();menu.Tag="LauncherLanguage";menus.Add(anchor,menu);anchor.Disposed+=delegate{menu.Dispose();};}if(menu.Visible){menu.Close();return;}menu.Items.Clear();var automatic=new ToolStripMenuItem(T("Automatic (system)")){Checked=Preference=="auto"};menu.Items.Add(automatic);automatic.Click+=delegate{Select("auto");};menu.Items.Add(new ToolStripSeparator());for(int i=0;i<Codes.Length;i++){string id=Codes[i];var item=new ToolStripMenuItem(Names[i]){Checked=Current==id&&Preference!="auto"};item.Click+=delegate{Select(id);};menu.Items.Add(item);}menu.Show(anchor,new Point(0,anchor.Height));}

 public static void DrawText(IDeviceContext g,string text,Font font,Rectangle rect,Color color,TextFormatFlags flags){TextRenderer.DrawText(g,T(text),font,rect,color,Rtl?flags|TextFormatFlags.RightToLeft:flags);}
 public static void DrawText(IDeviceContext g,string text,Font font,Rectangle rect,Color color){TextRenderer.DrawText(g,T(text),font,rect,color);}
 public static void DrawText(IDeviceContext g,string text,Font font,Point point,Color color){TextRenderer.DrawText(g,T(text),font,point,color);}
 public static void DrawString(Graphics g,string text,Font font,Brush brush,RectangleF rect,StringFormat format){g.DrawString(T(text),font,brush,rect,format);}
 public static void DrawString(Graphics g,string text,Font font,Brush brush,float x,float y){g.DrawString(T(text),font,brush,x,y);}
 public static DialogResult Show(params object[] args){
  var owner=args.OfType<IWin32Window>().FirstOrDefault();var text=args.OfType<string>().ToArray();string message=text.Length>0?text[0]:"",title=text.Length>1?text[1]:"Celesphonia";var buttons=args.OfType<MessageBoxButtons>().DefaultIfEmpty(MessageBoxButtons.OK).First();var def=args.OfType<MessageBoxDefaultButton>().DefaultIfEmpty(MessageBoxDefaultButton.Button1).First();
  using(var f=new Form{Text=T(title),StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(620,340),MinimumSize=new Size(450,240),BackColor=LauncherPaint.Ink,ForeColor=LauncherPaint.Paper,ShowInTaskbar=false,MinimizeBox=false,MaximizeBox=false}){
   var body=new RichTextBox{Text=T(message),ReadOnly=true,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,BackColor=f.BackColor,ForeColor=f.ForeColor,Font=new Font(Current=="hi"||Current=="bn"?"Nirmala UI":"Segoe UI",10),DetectUrls=true,RightToLeft=Rtl?RightToLeft.Yes:RightToLeft.No};var pad=new Panel{Dock=DockStyle.Fill,Padding=new Padding(22)};pad.Controls.Add(body);f.Controls.Add(pad);var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=66,Padding=new Padding(16,10,16,10),FlowDirection=FlowDirection.RightToLeft};f.Controls.Add(row);
   string[] labels=buttons==MessageBoxButtons.YesNoCancel?new[]{"Yes","No","Cancel"}:buttons==MessageBoxButtons.YesNo?new[]{"Yes","No"}:buttons==MessageBoxButtons.OKCancel?new[]{"OK","Cancel"}:buttons==MessageBoxButtons.RetryCancel?new[]{"Retry","Cancel"}:buttons==MessageBoxButtons.AbortRetryIgnore?new[]{"Abort","Retry","Ignore"}:new[]{"OK"};var choices=new List<Button>();
   foreach(string label in labels.Reverse()){DialogResult result=(DialogResult)Enum.Parse(typeof(DialogResult),label);var button=new SleekButton{Text=T(label),Size=new Size(120,38),DialogResult=result};row.Controls.Add(button);choices.Insert(0,button);if(label=="Cancel"||label=="No")f.CancelButton=button;}
   f.AcceptButton=choices[Math.Min(choices.Count-1,(int)def/256)];return owner==null?f.ShowDialog():f.ShowDialog(owner);
  }
 }
}
public sealed class LauncherLanguageButton:SleekButton {
 readonly ToolTip languageHint=new ToolTip();
 public LauncherLanguageButton(){Action refresh=delegate{languageHint.SetToolTip(this,LauncherLanguage.T("Launcher language")+": "+LauncherLanguage.Names[Array.IndexOf(LauncherLanguage.Codes,LauncherLanguage.Current)]+(LauncherLanguage.Problem.Length>0?"\n"+LauncherLanguage.Problem:""));};LauncherLanguage.Changed+=refresh;Disposed+=delegate{LauncherLanguage.Changed-=refresh;languageHint.Dispose();};MouseEnter+=delegate{refresh();};Text="";AccessibleName="Launcher language";Width=42;Height=40;Cursor=Cursors.Hand;Click+=delegate{LauncherLanguage.LanguageMenu(this);};}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var pen=new Pen(ForeColor,1.5f)){var r=new Rectangle(Width/2-10,Height/2-10,20,20);g.DrawEllipse(pen,r);g.DrawEllipse(pen,new Rectangle(r.X+5,r.Y,10,20));g.DrawLine(pen,r.Left,r.Y+10,r.Right,r.Y+10);g.DrawArc(pen,r.X,r.Y+3,20,8,0,180);}}
}

// Keep the original page's coordinate system while adapting rows to translated text.
public static class LauncherLocalizedLayout {
 sealed class Bounds {public Rectangle Original,Applied;public Bounds(Rectangle r){Original=Applied=r;}}
 static readonly ConditionalWeakTable<Control,Bounds> boxes=new ConditionalWeakTable<Control,Bounds>();
 public static int Fit(Control parent,int originalWidth,int width){
  var children=parent.Controls.Cast<Control>().Where(c=>c.Dock==DockStyle.None).ToArray();
  foreach(var c in children){Bounds b;if(!boxes.TryGetValue(c,out b)){b=new Bounds(c.Bounds);boxes.Add(c,b);}else if(c.Bounds!=b.Applied){var r=b.Original;if(c.Left!=b.Applied.Left)r.X=c.Left;if(c.Top!=b.Applied.Top)r.Y=c.Top;if(c.Width!=b.Applied.Width)r.Width=c.Width;if(c.Height!=b.Applied.Height)r.Height=c.Height;b.Original=r;}}
  int offset=0,lastBottom=0,rowTop=-1,rowBottom=0,rowGrowth=0;double ratio=Math.Min(1.0,(double)width/Math.Max(1,originalWidth));
  foreach(var c in children.OrderBy(x=>boxes.GetValue(x,k=>new Bounds(k.Bounds)).Original.Top)){
   var b=boxes.GetValue(c,k=>new Bounds(k.Bounds));var r=b.Original;
   if(rowTop<0||r.Top>=rowBottom){offset+=rowGrowth;rowTop=r.Top;rowBottom=r.Bottom;rowGrowth=0;}else rowBottom=Math.Max(rowBottom,r.Bottom);
   int x=(int)Math.Round(r.X*ratio),w=Math.Max(28,(int)Math.Round(r.Width*ratio)),h=r.Height;
   var label=c as Label;if(label!=null&&!label.AutoSize&&label.Visible){var measured=TextRenderer.MeasureText(label.Text,label.Font,new Size(Math.Max(20,w-4),10000),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);h=Math.Max(h,measured.Height+4);label.AutoEllipsis=false;}
   if(c is Panel&&!(c is FlowLayoutPanel)&&!(c is TableLayoutPanel)&&!(c is ContentScroller))h=Math.Max(h,Fit(c,r.Width,w));
   c.SetBounds(x,r.Y+offset,w,h);b.Applied=c.Bounds;if(c.Visible){rowGrowth=Math.Max(rowGrowth,h-r.Height);lastBottom=Math.Max(lastBottom,c.Bottom+10);}
  }
  return lastBottom;
 }
}

public static class LauncherLocaleEnvelope {
 // Producer writes this top-level field last. Compact/reordered user JSON falls back to the ordinary parser.
 public static string Core(string json){int start=json.LastIndexOf("\n  \"LauncherLocalization\":",StringComparison.Ordinal);if(start<0)return json;int value=json.IndexOf('{',start),depth=0;bool quoted=false,escape=false;if(value<0)return json;for(int i=value;i<json.Length;i++){char ch=json[i];if(quoted){if(escape)escape=false;else if(ch=='\\')escape=true;else if(ch=='"')quoted=false;continue;}if(ch=='"'){quoted=true;continue;}if(ch=='{')depth++;else if(ch=='}'&&--depth==0){if(json.Substring(i+1).Trim()!="}")return json;string head=json.Substring(0,start).TrimEnd();return head.EndsWith(",")?head.Substring(0,head.Length-1)+"\n}":json;}}return json;}

}
