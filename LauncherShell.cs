using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Windows.Forms;

public static class LauncherPaint {
    public static Color Gold=Color.FromArgb(193,165,110), Ink=Color.FromArgb(12,20,29), Paper=Color.FromArgb(218,224,218);
    public static Color Surface=Color.FromArgb(24,38,48),Muted=Color.FromArgb(153,174,182),Line=Color.FromArgb(63,79,88),Header=Color.FromArgb(19,34,43);
    public static void Sigil(Graphics g,PointF center,float radius,Color color){
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var pen=new Pen(color,1)){
            g.DrawEllipse(pen,center.X-radius,center.Y-radius,radius*2,radius*2);
            float inner=radius*.76f;g.DrawEllipse(pen,center.X-inner,center.Y-inner,inner*2,inner*2);
            var points=new PointF[5];for(int i=0;i<5;i++){double a=-Math.PI/2+i*Math.PI*4/5;points[i]=new PointF(center.X+(float)Math.Cos(a)*inner,center.Y+(float)Math.Sin(a)*inner);}g.DrawPolygon(pen,points);
            g.DrawLine(pen,center.X-radius-6,center.Y,center.X-radius+5,center.Y);g.DrawLine(pen,center.X+radius-5,center.Y,center.X+radius+6,center.Y);
        }
    }
    public static GraphicsPath Round(Rectangle r,int radius){
        int d=Math.Min(radius*2,Math.Min(r.Width,r.Height));var p=new GraphicsPath();
        p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    public static void Box(Graphics g,Rectangle r,Color fill,Color border,int radius){
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var shape=Round(r,radius)){using(var b=new SolidBrush(fill))g.FillPath(b,shape);if(border!=Color.Empty)using(var pen=new Pen(border))g.DrawPath(pen,shape);}
    }
    public static Color Blend(Color a,Color b,double amount){amount=Math.Max(0,Math.Min(1,amount));return Color.FromArgb((int)(a.R+(b.R-a.R)*amount),(int)(a.G+(b.G-a.G)*amount),(int)(a.B+(b.B-a.B)*amount));}
    public static void Glow(Graphics g,Rectangle r,int radius,double amount){
        if(amount<.01)return;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=Round(r,radius))foreach(var layer in new[]{new[]{8,16},new[]{4,32},new[]{1,150}})using(var pen=new Pen(Color.FromArgb((int)(layer[1]*amount),Gold),layer[0]))g.DrawPath(pen,path);
    }
    public static void Text(Graphics g,string text,Font font,Rectangle r,Color color,double glow,bool left=false){
        g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using(var format=new StringFormat{Alignment=left?StringAlignment.Near:StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap}){
            if(glow>.01)using(var light=new SolidBrush(Color.FromArgb((int)(38*glow),Gold))){
                foreach(var offset in new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)}){var halo=r;halo.Offset(offset);g.DrawString(text,font,light,halo,format);}
            }
            using(var brush=new SolidBrush(color))g.DrawString(text,font,brush,r,format);
        }
    }
}
public sealed class HoverMotion {
    readonly Control control;readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=15};bool hover;
    readonly Stopwatch elapsed=new Stopwatch();double start;
    public double Amount{get;private set;}
    void Change(bool next){if(next==hover)return;hover=next;start=Amount;elapsed.Restart();timer.Start();}
    public HoverMotion(Control owner){
        control=owner;
        owner.MouseEnter+=delegate{Change(true);};owner.MouseLeave+=delegate{Change(false);};
        owner.EnabledChanged+=delegate{if(!owner.Enabled)Change(false);};
        owner.Disposed+=delegate{timer.Stop();timer.Dispose();};
        timer.Tick+=delegate{double target=hover&&control.Enabled?1:0;double t=Math.Min(1,elapsed.ElapsedMilliseconds/160.0);double ease=1-Math.Pow(1-t,3);Amount=start+(target-start)*ease;if(t>=1){Amount=target;timer.Stop();}control.Invalidate();};
    }
}
public class SleekButton : Button {
    bool pressed;
    readonly HoverMotion motion;
    public double HoverAmount{get{return motion.Amount;}}
    public bool Navigation,Primary,Selected;
    public SleekButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseMnemonic=false;motion=new HoverMotion(this);}
    protected override void OnMouseEnter(EventArgs e){Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){pressed=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){pressed=true;Invalidate();}base.OnKeyDown(e);}
    protected override void OnKeyUp(KeyEventArgs e){pressed=false;Invalidate();base.OnKeyUp(e);}
    protected override void OnLostFocus(EventArgs e){pressed=false;Invalidate();base.OnLostFocus(e);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?BackColor:Parent.BackColor);
        double animation=Enabled?motion.Amount:0;
        Color c=Enabled?BackColor:LauncherPaint.Surface;
        c=LauncherPaint.Blend(c,LauncherPaint.Gold,animation*.13);if(Enabled&&pressed)c=ControlPaint.Dark(c,.12f);
        if(Navigation){
            if(animation>0||Selected)LauncherPaint.Box(e.Graphics,new Rectangle(4,4,Width-9,Height-9),Selected?LauncherPaint.Surface:LauncherPaint.Blend(LauncherPaint.Ink,LauncherPaint.Surface,animation),Color.Empty,12);
        }else{
            var rbox=new Rectangle(4,4,Width-9,Height-9);LauncherPaint.Box(e.Graphics,rbox,c,Color.Empty,12);
        }
        LauncherPaint.Glow(e.Graphics,new Rectangle(4,4,Width-9,Height-9),12,animation);
        var r=new Rectangle(Padding.Left+9,1,Width-Padding.Horizontal-18,Height-3);
        LauncherPaint.Text(e.Graphics,Text,Font,r,Enabled?LauncherPaint.Blend(ForeColor,LauncherPaint.Gold,animation*.32):LauncherPaint.Muted,animation,TextAlign==ContentAlignment.MiddleLeft);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(7,7,Width-15,Height-15),ForeColor,c);
    }
}
public class SleekCard : Panel {
    public SleekCard(){DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?BackColor:Parent.BackColor);
        LauncherPaint.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),BackColor,Color.Empty,14);
    }
}
public class SleekProgress : Control {
    int val,maximum=1000;
    public int Minimum{get;set;}
    public int Maximum{get{return maximum;}set{maximum=Math.Max(1,value);Invalidate();}}
    public int Value{get{return val;}set{val=Math.Max(Minimum,Math.Min(Maximum,value));Invalidate();}}
    public SleekProgress(){DoubleBuffered=true;BackColor=Color.FromArgb(29,42,50);ForeColor=Color.FromArgb(81,173,175);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Parent.BackColor);LauncherPaint.Box(e.Graphics,new Rectangle(0,0,Width-1,Height-1),BackColor,Color.Empty,5);
        int w=(int)((Width-1)*(double)Value/Maximum);if(w>=3)LauncherPaint.Box(e.Graphics,new Rectangle(0,0,w,Height-1),ForeColor,Color.Empty,5);
    }
}
public class GrimoireHeader : Control {
    public GrimoireHeader(){DoubleBuffered=true;}
    protected override void OnPaint(PaintEventArgs e){
        using(var brush=new LinearGradientBrush(ClientRectangle,Color.FromArgb(19,34,43),LauncherPaint.Ink,0f))e.Graphics.FillRectangle(brush,ClientRectangle);
        LauncherPaint.Sigil(e.Graphics,new PointF(48,47),25,LauncherPaint.Gold);
        using(var pen=new Pen(Color.FromArgb(62,75,78))){e.Graphics.DrawLine(pen,24,94,Width-26,94);e.Graphics.DrawLine(pen,24,96,Width-26,96);}
        using(var pen=new Pen(LauncherPaint.Gold))e.Graphics.DrawLine(pen,24,94,139,94);
    }
}
public class GrimoirePage : Panel {
    public GrimoirePage(){DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e){
        e.Graphics.Clear(BackColor);
    }
}
// A separate content viewport keeps navigation and status visible while the
// scrollbar, wheel and keyboard move the active page on shorter screens.
public static class ProfileLibrary {
    static string PathKey(string path){try{return Path.GetFullPath(path).TrimEnd('\\','/');}catch{return path??"";}}
    public static string Identity(Profile p){return PathKey(p.Root)+"|"+PathKey(p.SavePath);}
    public static List<Profile> Visible(IEnumerable<Profile> profiles,string selectedId){
        return (profiles??Enumerable.Empty<Profile>()).Where(p=>p!=null&&!FileDiscovery.IsNativeProbe(p.Root))
            .GroupBy(Identity,StringComparer.OrdinalIgnoreCase).Select(g=>g.FirstOrDefault(p=>p.Id==selectedId)??g.First()).ToList();
    }
    static string VersionOrder(string id){
        if(id=="ch-current")return "00";
        if(id=="gorpg-1-23")return "10";
        if(id!=null&&id.StartsWith("maple-",StringComparison.Ordinal))return "20";
        if(id!=null&&id.StartsWith("three-in-one-",StringComparison.Ordinal)){
            var parts=id.Substring("three-in-one-".Length).Split('-');
            var version=parts.Take(3).Select(x=>{int n;return int.TryParse(x,out n)?n.ToString("D6"):x;});
            var recipe=RecipeSupport.Catalog().Recipes.FirstOrDefault(x=>x.Id==id);
            return "30-"+string.Join("-",version)+"-"+(recipe==null?0:recipe.SaveGeneration).ToString("D6");
        }
        return "90-"+(id??"");
    }
    // Installed copies and available updates share one chronological order.
    // Deduplicate roots/save folders, never different copies with their own saves.
    public static List<object> Entries(IEnumerable<Profile> profiles,string selectedId){
        var installed=Visible(profiles,selectedId).Select(p=>new{Item=(object)p,Id=UniversalSupport.Identify(p.Root),Name=p.Name,Tie=Identity(p)}).ToList();
        var rows=installed.Concat(RecipeSupport.Catalog().Recipes
            .Where(r=>UpgradeSupport.IsUpgrade(r.Id)&&!installed.Any(p=>p.Id==r.Id))
            .Select(r=>new{Item=(object)r,Id=r.Id,Name=r.Name,Tie=r.Id}));
        return rows.OrderBy(x=>VersionOrder(x.Id),StringComparer.Ordinal).ThenBy(x=>x.Name,StringComparer.OrdinalIgnoreCase)
            .ThenBy(x=>x.Tie,StringComparer.OrdinalIgnoreCase).Select(x=>x.Item).ToList();
    }
}
public class ContentScroller : Panel {
    public readonly VScrollBar Slider=new VScrollBar();
    public readonly Panel Canvas=new Panel();
    readonly Panel viewport=new Panel();
    bool updatingRange;
    public int Position {get{return Slider.Value;}set{Slider.Value=Math.Max(0,Math.Min(Limit,value));Canvas.Top=-Slider.Value;}}
    public int Limit {get{return Math.Max(0,Canvas.Height-viewport.ClientSize.Height);}}
    public ContentScroller(){
        DoubleBuffered=true;Slider.Dock=DockStyle.Right;Slider.Width=18;Slider.SmallChange=36;
        Slider.AccessibleName="Scroll page content";Slider.AccessibleDescription="Move through the active launcher page";
        viewport.Dock=DockStyle.Fill;viewport.BackColor=LauncherPaint.Ink;Canvas.Size=new Size(812,630);Canvas.BackColor=LauncherPaint.Ink;
        viewport.Controls.Add(Canvas);Controls.Add(viewport);Controls.Add(Slider);
        Slider.ValueChanged+=delegate{Canvas.Top=-Slider.Value;};
        viewport.Resize+=delegate{RefreshRange();};
        Canvas.SizeChanged+=delegate{RefreshRange();};
        HookWheel(viewport);
    }
    public void RefreshRange(){
        if(updatingRange)return;updatingRange=true;
        try{
            int value=Slider.Value;bool overflow=Limit>0;
            Slider.LargeChange=Math.Max(1,viewport.Height);Slider.Maximum=Math.Max(0,Canvas.Height-1);
            Slider.Visible=overflow;Slider.Enabled=overflow;Position=overflow?value:0;
        }finally{updatingRange=false;}
    }
    public void HookWheel(Control parent){
        if(!(parent is ListBox)&&!(parent is DataGridView)&&!(parent is TextBoxBase)&&!(parent is ComboBox))parent.MouseWheel+=delegate(object sender,MouseEventArgs e){Position-=Math.Sign(e.Delta)*Math.Max(1,SystemInformation.MouseWheelScrollLines)*Slider.SmallChange;var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;};
        parent.ControlAdded+=delegate(object sender,ControlEventArgs e){HookWheel(e.Control);};
        foreach(Control child in parent.Controls)HookWheel(child);
    }
}
public class LauncherMenuRenderer : ToolStripProfessionalRenderer {
    public LauncherMenuRenderer(){RoundedEdges=false;}
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(LauncherPaint.Surface);}
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using(var p=new Pen(Color.FromArgb(67,73,92)))e.Graphics.DrawRectangle(p,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(e.Item.Selected&&e.Item.Enabled)LauncherPaint.Box(e.Graphics,new Rectangle(3,1,e.Item.Width-7,e.Item.Height-2),ControlPaint.Light(LauncherPaint.Surface),Color.Empty,5);}
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Enabled?LauncherPaint.Paper:Color.FromArgb(125,138,160);base.OnRenderItemText(e);}
}
public static class LauncherMenuLifetime {
    public static void RetireAfterClose(ContextMenuStrip menu){
        bool queued=false;
        menu.Closed+=delegate{
            if(queued||menu.IsDisposed||menu.Disposing)return;queued=true;
            // WinForms' modal menu filter still accesses the handle after Closed.
            // Release the popup on the next UI message, after that stack unwinds.
            menu.BeginInvoke((MethodInvoker)delegate{menu.Dispose();});
        };
    }
    public static void Test(string report){
        int cycles=0;
        using(var owner=new Form{ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(-10000,-10000),Size=new Size(100,100)}){
            owner.Show();Application.DoEvents();
            foreach(var reason in new[]{ToolStripDropDownCloseReason.AppClicked,ToolStripDropDownCloseReason.ItemClicked,ToolStripDropDownCloseReason.Keyboard}){
                for(int i=0;i<10;i++){
                    var menu=new ContextMenuStrip();menu.Items.Add("Native folder");RetireAfterClose(menu);menu.Show(owner,Point.Empty);
                    menu.Close(reason);
                    // Exercise the native filter's post-close access before dispatching
                    // the next message, then require disposal instead of leaking menus.
                    if(menu.IsDisposed||menu.Handle==IntPtr.Zero)throw new InvalidOperationException("Popup retired before native close completed.");
                    menu.Hide();Application.DoEvents();
                    if(!menu.IsDisposed)throw new InvalidOperationException("Closed popup was not released.");
                    cycles++;
                }
            }
            owner.Close();
        }
        File.WriteAllText(report,Store.Json.Serialize(new{status="PASS",nativeCloseCycles=cycles,closeReasons=new[]{"Outside click","Item selection","Keyboard"},postCloseHandleAccess=true,closedMenusReleased=true}),Encoding.UTF8);
    }
}
public class ReplacementAwareForm : Form {
    public Func<bool> CanReplace=()=>true;
    public event EventHandler ReplacementRequested;
    readonly System.Windows.Forms.Timer replacementTimer=new System.Windows.Forms.Timer{Interval=200};
    public bool ReplacementPending{get;private set;}
    public ReplacementAwareForm(){replacementTimer.Tick+=delegate{if(LauncherInstance.ConsumeReplacement())RequestReplacement();if(ReplacementPending&&CanReplace()){replacementTimer.Stop();Close();}};replacementTimer.Start();}
    void RequestReplacement(){if(ReplacementPending)return;ReplacementPending=true;if(ReplacementRequested!=null)ReplacementRequested(this,EventArgs.Empty);}
    protected override void WndProc(ref Message m){
        if(m.Msg==LauncherInstance.ReplaceMessage){
            RequestReplacement();
            replacementTimer.Start();m.Result=new IntPtr(LauncherInstance.Acknowledgement);return;
        }
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing){if(disposing)replacementTimer.Dispose();base.Dispose(disposing);}
}
public class LauncherFolder {
    public string Label,Path;
    public bool Exists{get{return !string.IsNullOrEmpty(Path)&&Directory.Exists(Path);}}
}
public static class LauncherFolders {
    public static List<LauncherFolder> Targets(Profile profile,string installRoot,string versionId){
        var result=new List<LauncherFolder>();
        Action<string,string> add=(label,path)=>result.Add(new LauncherFolder{Label=label,Path=path});
        if(profile!=null){
            string web=Store.WebRoot(profile.Root);
            add("Game folder",profile.Root);add("Game content",web);add("Game data",System.IO.Path.Combine(web,"data"));
            add("Game plugins",System.IO.Path.Combine(web,"js","plugins"));add("Saves",profile.SavePath);
            add("Save backups",System.IO.Path.Combine(Store.Home,"Save Backups",profile.Id));
            string upgradeRecord=System.IO.Path.Combine(profile.Root,"launcher-upgrade.json");
            if(File.Exists(upgradeRecord)){try{var upgrade=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(upgradeRecord));object backup;if(upgrade.TryGetValue("sourceBackup",out backup))add("Upgrade source save backup",Convert.ToString(backup));}catch(IOException){}catch(ArgumentException){}catch(InvalidOperationException){}}
            add("English overlay & recovery",System.IO.Path.Combine(web,"celesphonia-english"));
            if(!string.IsNullOrEmpty(versionId)&&versionId!="ch-current")add("This version's English pack",System.IO.Path.Combine(Store.Home,"English",versionId));
            add("This version's runtime",System.IO.Path.Combine(Store.Home,"Runtime",profile.Id));
        }
        add("Launcher folder",Store.Home);add("Patch packs",System.IO.Path.Combine(Store.Home,"Delta Packs"));
        add("All English packs",System.IO.Path.Combine(Store.Home,"English"));add("Game profiles location",installRoot);
        return result;
    }
    public static void Open(string path){
        if(File.Exists(path))path=System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if(string.IsNullOrEmpty(path)||!Directory.Exists(path))throw new DirectoryNotFoundException("This folder has not been created yet.");
        Process.Start("explorer.exe",Store.Q(System.IO.Path.GetFullPath(path)));
    }
}
public sealed class LauncherInstance : IDisposable {
    public const int Acknowledgement=0x43454c;
    public static readonly int ReplaceMessage=RegisterWindowMessage("Celesphonia.PortableLauncher.Replace.v2");
    Mutex owner;bool owned;EventWaitHandle signal;
    static EventWaitHandle activeSignal;
    public static bool ConsumeReplacement(){return activeSignal!=null&&activeSignal.WaitOne(0);}
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int RegisterWindowMessage(string name);
    [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr hwnd,int msg,IntPtr wp,IntPtr lp,uint flags,uint timeout,out IntPtr result);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd,int msg,IntPtr wp,IntPtr lp);
    delegate bool WindowVisitor(IntPtr hwnd,IntPtr parameter);
    [DllImport("user32.dll")] static extern bool EnumWindows(WindowVisitor visitor,IntPtr parameter);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd,StringBuilder text,int capacity);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint processId);
    delegate bool DesktopVisitor(string name,IntPtr parameter);
    [DllImport("user32.dll")] static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool EnumDesktops(IntPtr station,DesktopVisitor visitor,IntPtr parameter);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr OpenDesktop(string name,uint flags,bool inherit,uint access);
    [DllImport("user32.dll")] static extern bool EnumDesktopWindows(IntPtr desktop,WindowVisitor visitor,IntPtr parameter);
    [DllImport("user32.dll")] static extern bool CloseDesktop(IntPtr desktop);
    static bool Take(Mutex mutex){try{return mutex.WaitOne(0);}catch(AbandonedMutexException){return true;}}
    static List<IntPtr> OtherWindows(bool fixture){
        var handles=new List<IntPtr>();uint self=(uint)Process.GetCurrentProcess().Id;
        WindowVisitor visit=delegate(IntPtr hwnd,IntPtr parameter){
            uint pid;GetWindowThreadProcessId(hwnd,out pid);if(pid==self)return true;
            var text=new StringBuilder(512);GetWindowText(hwnd,text,text.Capacity);string title=text.ToString();
            bool match=fixture?title.StartsWith("Celesphonia IPC fixture",StringComparison.Ordinal):title.StartsWith("Celesphonia · Mod Library",StringComparison.Ordinal)||title.StartsWith("Celesphonia · Finding your versions",StringComparison.Ordinal)||title=="Celesphonia Mod Launcher";
            if(match)handles.Add(hwnd);return true;
        };
        EnumWindows(visit,IntPtr.Zero);
        EnumDesktops(GetProcessWindowStation(),delegate(string name,IntPtr parameter){IntPtr desktop=OpenDesktop(name,0,false,0x0041);if(desktop!=IntPtr.Zero){try{EnumDesktopWindows(desktop,visit,IntPtr.Zero);}finally{CloseDesktop(desktop);}}return true;},IntPtr.Zero);
        return handles.Distinct().ToList();
    }
    static bool WaitFor(Func<bool> ready,string detail){
        if(ready())return true;
        bool complete=false;
        using(var dialog=new Form{Text="Celesphonia · Opening launcher",ClientSize=new Size(500,154),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(17,21,30),ForeColor=LauncherPaint.Paper,Font=new Font("Segoe UI",10)})
        using(var tick=new System.Windows.Forms.Timer{Interval=200}){
            dialog.Controls.Add(new Label{Text=detail,Location=new Point(22,22),Size=new Size(456,73)});
            dialog.Controls.Add(new ProgressBar{Location=new Point(22,112),Size=new Size(456,7),Style=ProgressBarStyle.Marquee});
            tick.Tick+=delegate{if(ready()){complete=true;dialog.Close();}};dialog.Shown+=delegate{tick.Start();};Application.Run(dialog);
        }
        return complete;
    }
    public static LauncherInstance Acquire(bool fixture=false){
        string suffix=fixture?"Fixture":"";
        using(var gate=new Mutex(false,"Local\\CelesphoniaLauncherStartup"+suffix)){
            if(!WaitFor(()=>Take(gate),"Another launcher is starting. This window will open as soon as it is ready."))return null;
            try{
                var instance=new LauncherInstance{owner=new Mutex(false,fixture?"Local\\CelesphoniaPortableLibraryFixture":"Local\\CelesphoniaPortableLibrary")};
                var requested=new HashSet<string>();
                string eventPrefix="Local\\CelesphoniaLauncherReplace"+suffix+".";
                Func<bool> ready=delegate{
                    bool remaining=false;
                    foreach(var p in Process.GetProcesses())using(p){
                        if(p.Id==Process.GetCurrentProcess().Id)continue;
                        try{using(var request=EventWaitHandle.OpenExisting(eventPrefix+p.Id)){request.Set();}}catch(WaitHandleCannotBeOpenedException){}catch(UnauthorizedAccessException){}
                    }
                    foreach(IntPtr handle in OtherWindows(fixture)){
                        uint pid;GetWindowThreadProcessId(handle,out pid);
                        remaining=true;string key=pid+"/"+handle.ToInt64();
                        if(requested.Add(key)){
                            IntPtr reply;SendMessageTimeout(handle,ReplaceMessage,IntPtr.Zero,IntPtr.Zero,2,1500,out reply);
                            if(reply.ToInt64()!=Acknowledgement)PostMessage(handle,0x0010,IntPtr.Zero,IntPtr.Zero);
                        }
                    }
                    if(!instance.owned)instance.owned=Take(instance.owner);
                    return instance.owned&&!remaining;
                };
                if(!WaitFor(ready,"Closing the previous launcher…\r\nActive operations finish before its window closes.")){instance.Dispose();return null;}
                instance.signal=new EventWaitHandle(false,EventResetMode.AutoReset,eventPrefix+Process.GetCurrentProcess().Id);activeSignal=instance.signal;
                return instance;
            }finally{gate.ReleaseMutex();}
        }
    }
    public void Dispose(){if(signal!=null){activeSignal=null;signal.Dispose();signal=null;}if(owner!=null){if(owned)owner.ReleaseMutex();owner.Dispose();owner=null;}}
    public static int Probe(string output,int busyMs){
        using(var instance=Acquire(true)){if(instance==null)return 2;
            using(var form=new ReplacementAwareForm{Text="Celesphonia IPC fixture",ClientSize=new Size(300,80),ShowInTaskbar=false}){
                DateTime ready=DateTime.UtcNow.AddMilliseconds(busyMs);form.CanReplace=()=>DateTime.UtcNow>=ready;
                form.Shown+=delegate{File.WriteAllText(output,Process.GetCurrentProcess().Id.ToString());};Application.Run(form);
            }
        }return 0;
    }
}
