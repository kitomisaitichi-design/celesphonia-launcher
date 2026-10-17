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
        if(r.Width<=0||r.Height<=0){var empty=new GraphicsPath();return empty;}
        int d=Math.Max(1,Math.Min(radius*2,Math.Min(r.Width,r.Height)));var p=new GraphicsPath();
        p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    public static void Box(Graphics g,Rectangle r,Color fill,Color border,int radius){
        if(r.Width<=1||r.Height<=1)return;
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var shape=Round(r,radius)){using(var b=new SolidBrush(fill))g.FillPath(b,shape);if(border!=Color.Empty)using(var pen=new Pen(border))g.DrawPath(pen,shape);}
    }
    public static Color Blend(Color a,Color b,double amount){amount=Math.Max(0,Math.Min(1,amount));return Color.FromArgb((int)(a.R+(b.R-a.R)*amount),(int)(a.G+(b.G-a.G)*amount),(int)(a.B+(b.B-a.B)*amount));}
    public static void Glow(Graphics g,Rectangle r,int radius,double amount){
        if(amount<.01||r.Width<=1||r.Height<=1)return;g.SmoothingMode=SmoothingMode.AntiAlias;
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
    public SleekButton(){
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
        FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseMnemonic=false;TabStop=true;
        Cursor=Cursors.Hand;motion=new HoverMotion(this);
    }
    protected override void OnMouseEnter(EventArgs e){Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){pressed=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){pressed=true;Invalidate();}base.OnKeyDown(e);}
    protected override void OnKeyUp(KeyEventArgs e){pressed=false;Invalidate();base.OnKeyUp(e);}
    protected override void OnLostFocus(EventArgs e){pressed=false;Invalidate();base.OnLostFocus(e);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?LauncherPaint.Ink:Parent.BackColor);
        if(Width<9||Height<9)return;
        double animation=Enabled?motion.Amount:0;
        Color c=Enabled?BackColor:LauncherPaint.Blend(LauncherPaint.Surface,LauncherPaint.Ink,.43);
        Color border=LauncherPaint.Blend(LauncherPaint.Line,LauncherPaint.Gold,animation*.63);
        if(Enabled&&pressed)c=LauncherPaint.Blend(c,LauncherPaint.Ink,.19);
        else c=LauncherPaint.Blend(c,LauncherPaint.Gold,animation*.075);
        var rbox=new Rectangle(3,3,Width-7,Height-7);
        if(Navigation){
            if(animation>.01||Selected){
                LauncherPaint.Box(e.Graphics,rbox,Selected?LauncherPaint.Blend(LauncherPaint.Surface,LauncherPaint.Gold,.105):LauncherPaint.Blend(LauncherPaint.Ink,LauncherPaint.Surface,animation),Selected?LauncherPaint.Blend(LauncherPaint.Gold,LauncherPaint.Line,.40):border,8);
                if(Selected)using(var marker=new SolidBrush(LauncherPaint.Gold))
                    e.Graphics.FillRectangle(marker,4,Math.Max(5,Height/2-9),3,18);
            }
        }else{
            LauncherPaint.Box(e.Graphics,rbox,c,Primary?LauncherPaint.Blend(LauncherPaint.Gold,c,.52):border,8);
        }
        Rectangle label=new Rectangle(Math.Max(9,Padding.Left+10),4,Math.Max(1,Width-Padding.Horizontal-20),Math.Max(1,Height-8));
        Color textColor=Enabled?(Selected?LauncherPaint.Paper:LauncherPaint.Blend(ForeColor,LauncherPaint.Paper,.15)):LauncherPaint.Blend(LauncherPaint.Muted,LauncherPaint.Surface,.25);
        TextFormatFlags flags=TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;
        if(TextAlign==ContentAlignment.MiddleLeft)flags|=TextFormatFlags.Left;
        else if(TextAlign==ContentAlignment.MiddleRight)flags|=TextFormatFlags.Right;
        else flags|=TextFormatFlags.HorizontalCenter;
        Font fit=Font;
        if(TextRenderer.MeasureText(e.Graphics,Text,Font,new Size(int.MaxValue,Height),TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width>label.Width&&Font.Size>8.5f)
            fit=new Font(Font.FontFamily,Math.Max(8.5f,Font.Size-1.2f),Font.Style);
        try{TextRenderer.DrawText(e.Graphics,Text,fit,label,textColor,flags|TextFormatFlags.NoPadding);}
        finally{if(!object.ReferenceEquals(fit,Font))fit.Dispose();}
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(6,6,Width-13,Height-13),LauncherPaint.Paper,c);
    }
}
public class SleekCard : Panel {
    public SleekCard(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw|ControlStyles.AllPaintingInWmPaint,true);}
    protected override void OnPaintBackground(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?BackColor:Parent.BackColor);
        LauncherPaint.Box(e.Graphics,new Rectangle(1,1,Width-3,Height-3),BackColor,LauncherPaint.Blend(LauncherPaint.Line,BackColor,.33),10);
    }
}
public class SleekProgress : Control {
    int val,maximum=1000;
    public int Minimum{get;set;}
    public int Maximum{get{return maximum;}set{maximum=Math.Max(1,value);Invalidate();}}
    public int Value{get{return val;}set{val=Math.Max(Minimum,Math.Min(Maximum,value));Invalidate();}}
    public SleekProgress(){DoubleBuffered=true;BackColor=Color.FromArgb(29,42,50);ForeColor=Color.FromArgb(81,173,175);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?LauncherPaint.Ink:Parent.BackColor);
        LauncherPaint.Box(e.Graphics,new Rectangle(0,0,Width-1,Height-1),BackColor,LauncherPaint.Line,5);
        int w=(int)((Width-1)*(double)(Value-Minimum)/Math.Max(1,Maximum-Minimum));
        if(w>=3)LauncherPaint.Box(e.Graphics,new Rectangle(0,0,w,Height-1),ForeColor,Color.Empty,5);
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
    // A profile is metadata, not proof that its game is still installed.
    // Portable copies and the shared installation index can retain paths to
    // deleted/moved folders; never show such orphans as playable installations.
    // Keep the saved metadata and backups intact so relocated drives recover.
    public static bool IsPlayable(Profile p){
        if(p==null||string.IsNullOrWhiteSpace(p.Root)||string.IsNullOrWhiteSpace(p.SavePath)
            ||FileDiscovery.IsNativeProbe(p.Root))return false;
        try{
            if(!Directory.Exists(p.Root))return false;
            Profile detected=Store.Detect(p.Root,p.Name??"Game");
            if(!PathKey(detected.SavePath).Equals(PathKey(p.SavePath),StringComparison.OrdinalIgnoreCase))
                return false;
            using(var exe=File.OpenRead(Path.Combine(p.Root,detected.Exe)))
                return exe.ReadByte()=='M'&&exe.ReadByte()=='Z';
        }catch(Exception e){
            if(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||
               e is InvalidOperationException||e is NotSupportedException||
               e is System.Security.SecurityException)return false;
            throw;
        }
    }
    public static List<Profile> Visible(IEnumerable<Profile> profiles,string selectedId){
        return (profiles??Enumerable.Empty<Profile>()).Where(IsPlayable)
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
// The stock WinForms VScrollBar is painted by Windows and ignores launcher
// colours. Use a compact owner-painted rail for the page; DataGridView keeps
// its independent scrollbar for long file tables.
public sealed class DarkPageScrollBar : Control {
    int value,maximum,largeChange=1,smallChange=36;
    bool dragging;
    int dragStart,dragValue;
    public event EventHandler ValueChanged;
    public int Minimum {get{return 0;}}
    public int Maximum {get{return maximum;}set{maximum=Math.Max(0,value);SetValue(this.value);Invalidate();}}
    public int LargeChange {get{return largeChange;}set{largeChange=Math.Max(1,value);SetValue(this.value);Invalidate();}}
    public int SmallChange {get{return smallChange;}set{smallChange=Math.Max(1,value);}}
    public int ScrollLimit {get{return Math.Max(0,maximum-largeChange+1);}}
    public int Value {get{return value;}set{SetValue(value);}}
    public DarkPageScrollBar(){
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|
            ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
        Width=12;TabStop=true;Cursor=Cursors.Hand;
        BackColor=LauncherPaint.Ink;ForeColor=LauncherPaint.Muted;
        AccessibleName="Scroll page content";
        AccessibleDescription="Drag, use arrow keys, Page Up, Page Down, Home or End to scroll the launcher page";
    }
    public void SetRange(int contentHeight,int viewportHeight,int requestedValue){
        maximum=Math.Max(0,contentHeight-1);
        largeChange=Math.Max(1,viewportHeight);
        SetValue(requestedValue);
        Invalidate();
    }
    void SetValue(int next){
        next=Math.Max(0,Math.Min(ScrollLimit,next));
        if(next==value)return;
        value=next;Invalidate();
        if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);
    }
    Rectangle Track {get{return new Rectangle(2,3,Math.Max(1,Width-4),Math.Max(1,Height-6));}}
    Rectangle Thumb {
        get {
            Rectangle track=Track;
            int content=Math.Max(1,maximum+1);
            int height=Math.Min(track.Height,Math.Max(28,(int)((long)track.Height*largeChange/content)));
            int travel=Math.Max(0,track.Height-height);
            int y=track.Y+(ScrollLimit>0?(int)((long)travel*value/ScrollLimit):0);
            return new Rectangle(track.X,y,track.Width,height);
        }
    }
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(LauncherPaint.Ink);
        if(!Enabled||ScrollLimit==0)return;
        var track=Track;
        LauncherPaint.Box(e.Graphics,track,LauncherPaint.Blend(LauncherPaint.Ink,LauncherPaint.Surface,.60),
            LauncherPaint.Blend(LauncherPaint.Line,LauncherPaint.Ink,.60),5);
        Color thumb=Focused||dragging?LauncherPaint.Blend(LauncherPaint.Gold,LauncherPaint.Muted,.45):
            LauncherPaint.Blend(LauncherPaint.Muted,LauncherPaint.Surface,.23);
        LauncherPaint.Box(e.Graphics,Thumb,thumb,LauncherPaint.Blend(LauncherPaint.Line,thumb,.22),5);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(0,0,Width-1,Height-1));
    }
    protected override void OnMouseDown(MouseEventArgs e){
        base.OnMouseDown(e);
        if(e.Button!=MouseButtons.Left||ScrollLimit==0)return;
        Focus();
        if(Thumb.Contains(e.Location)){
            dragging=true;dragStart=e.Y;dragValue=value;Capture=true;Invalidate();
        }else {
            Value+=e.Y<Thumb.Top?-largeChange:largeChange;
        }
    }
    protected override void OnMouseMove(MouseEventArgs e){
        base.OnMouseMove(e);
        if(!dragging)return;
        int travel=Math.Max(1,Track.Height-Thumb.Height);
        Value=dragValue+(int)((long)(e.Y-dragStart)*ScrollLimit/travel);
    }
    void FinishDrag(){dragging=false;Capture=false;Invalidate();}
    protected override void OnMouseUp(MouseEventArgs e){if(dragging)FinishDrag();base.OnMouseUp(e);}
    protected override void OnMouseCaptureChanged(EventArgs e){if(dragging)FinishDrag();base.OnMouseCaptureChanged(e);}
    protected override bool IsInputKey(Keys keyData){
        Keys key=keyData&Keys.KeyCode;
        if(key==Keys.Up||key==Keys.Down||key==Keys.PageUp||key==Keys.PageDown||
           key==Keys.Home||key==Keys.End)return true;
        return base.IsInputKey(keyData);
    }
    protected override void OnKeyDown(KeyEventArgs e){
        switch(e.KeyCode){
            case Keys.Up:Value-=smallChange;break;
            case Keys.Down:Value+=smallChange;break;
            case Keys.PageUp:Value-=largeChange;break;
            case Keys.PageDown:Value+=largeChange;break;
            case Keys.Home:Value=0;break;
            case Keys.End:Value=ScrollLimit;break;
            default:base.OnKeyDown(e);return;
        }
        e.Handled=true;e.SuppressKeyPress=true;
    }
    protected override void OnMouseWheel(MouseEventArgs e){
        Value-=Math.Sign(e.Delta)*smallChange*Math.Max(1,SystemInformation.MouseWheelScrollLines);
        base.OnMouseWheel(e);
    }
}
public class ContentScroller : Panel {
    public readonly DarkPageScrollBar Slider=new DarkPageScrollBar();
    public readonly Panel Canvas=new Panel();
    readonly Panel viewport=new Panel();
    bool updatingRange;
    int effectiveLimit;
    public int Position {get{return Slider.Value;}set{Slider.Value=Math.Max(0,Math.Min(Limit,value));Canvas.Top=-Slider.Value;}}
    public int Limit {get{return effectiveLimit;}}
    public ContentScroller(){
        DoubleBuffered=true;Slider.Dock=DockStyle.Right;Slider.Width=12;Slider.SmallChange=36;
        Slider.Visible=false;Slider.Enabled=false;
        viewport.Dock=DockStyle.Fill;viewport.BackColor=LauncherPaint.Ink;
        Canvas.Size=new Size(812,630);Canvas.BackColor=LauncherPaint.Ink;
        viewport.Controls.Add(Canvas);Controls.Add(viewport);Controls.Add(Slider);
        Slider.ValueChanged+=delegate{Canvas.Top=-Slider.Value;};
        viewport.Resize+=delegate{RefreshRange();};
        Canvas.SizeChanged+=delegate{RefreshRange();};
        HookWheel(viewport);
    }
    int ActiveContentBottom(){
        int bottom=-1;
        foreach(Control page in Canvas.Controls){
            if(!page.Visible)continue;
            // Page panels dock to Canvas, so inspect their direct children,
            // not the page itself (which always reaches Canvas.Bottom).
            if(page is Panel){
                foreach(Control item in page.Controls){
                    if(item.Visible&&!item.IsDisposed)bottom=Math.Max(bottom,item.Bottom);
                }
            }else bottom=Math.Max(bottom,page.Bottom);
        }
        return bottom;
    }
    public void RefreshRange(){
        if(updatingRange)return;updatingRange=true;
        try{
            int viewportHeight=Math.Max(0,viewport.ClientSize.Height);
            // PolishedLauncher reserves 12px after the final page control.
            // That trailing breathing space must not produce a white/empty
            // scrollbar on otherwise fully visible compact pages.
            int bottom=ActiveContentBottom();
            int required=bottom>=0?
                (bottom<=viewportHeight?viewportHeight:Math.Max(Canvas.Height,bottom+12)):
                Math.Max(Canvas.Height,viewportHeight);
            effectiveLimit=viewportHeight>0?Math.Max(0,required-viewportHeight):0;
            bool overflow=effectiveLimit>0;
            int previous=Slider.Value;
            if(Slider.Visible!=overflow)Slider.Visible=overflow;
            Slider.Enabled=overflow;
            Slider.SetRange(overflow?required:viewportHeight,Math.Max(1,viewportHeight),overflow?previous:0);
            if(Canvas.Top!=-Slider.Value)Canvas.Top=-Slider.Value;
        }finally{updatingRange=false;}
    }
    public void HookWheel(Control parent){
        if(!(parent is ListBox)&&!(parent is DataGridView)&&!(parent is TextBoxBase)&&!(parent is ComboBox))
            parent.MouseWheel+=delegate(object sender,MouseEventArgs e){
                if(Limit<=0)return;
                Position-=Math.Sign(e.Delta)*Math.Max(1,SystemInformation.MouseWheelScrollLines)*Slider.SmallChange;
                var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;
            };
        parent.ControlAdded+=delegate(object sender,ControlEventArgs e){HookWheel(e.Control);};
        foreach(Control child in parent.Controls)HookWheel(child);
    }
}
public class LauncherMenuRenderer : ToolStripProfessionalRenderer {
    public LauncherMenuRenderer(){RoundedEdges=false;}
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){
        e.Graphics.Clear(e.ToolStrip is ToolStripDropDown?LauncherPaint.Surface:LauncherPaint.Ink);
    }
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){
        if(!(e.ToolStrip is ToolStripDropDown)||e.ToolStrip.Width<2||e.ToolStrip.Height<2)return;
        using(var pen=new Pen(LauncherPaint.Blend(LauncherPaint.Line,LauncherPaint.Gold,.18)))
            e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);
    }
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){
        var drop=e.ToolStrip as ToolStripDropDownMenu;
        if(drop==null||!drop.ShowCheckMargin)return;
        int width=Math.Min(30,drop.Width/5);
        using(var brush=new SolidBrush(LauncherPaint.Blend(LauncherPaint.Ink,LauncherPaint.Surface,.52)))
            e.Graphics.FillRectangle(brush,1,1,width,Math.Max(0,drop.Height-2));
        using(var pen=new Pen(LauncherPaint.Blend(LauncherPaint.Line,LauncherPaint.Surface,.42)))
            e.Graphics.DrawLine(pen,width+1,7,width+1,drop.Height-8);
    }
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){
        if(e.Item.Width<8||e.Item.Height<7)return;
        bool hot=e.Item.Enabled&&(e.Item.Selected||e.Item.Pressed);
        if(!hot)return;
        var rectangle=new Rectangle(3,1,e.Item.Width-7,e.Item.Height-3);
        var fill=LauncherPaint.Blend(LauncherPaint.Surface,LauncherPaint.Gold,e.Item.Pressed?.14:.085);
        LauncherPaint.Box(e.Graphics,rectangle,fill,LauncherPaint.Blend(LauncherPaint.Line,LauncherPaint.Gold,.38),6);
        using(var accent=new SolidBrush(LauncherPaint.Gold))
            e.Graphics.FillRectangle(accent,3,Math.Max(3,rectangle.Y+rectangle.Height/2-7),2,14);
    }
    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){
        int left=e.Item.Owner is ToolStripDropDownMenu?36:12;
        int y=e.Item.Height/2;
        using(var pen=new Pen(LauncherPaint.Blend(LauncherPaint.Line,LauncherPaint.Surface,.25)))
            e.Graphics.DrawLine(pen,left,y,Math.Max(left,e.Item.Width-11),y);
    }
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){
        e.ArrowColor=e.Item.Enabled?(e.Item.Selected?LauncherPaint.Gold:LauncherPaint.Muted):
            LauncherPaint.Blend(LauncherPaint.Muted,LauncherPaint.Surface,.47);
        base.OnRenderArrow(e);
    }
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){
        var r=e.ImageRectangle;
        if(r.Width<4||r.Height<4)return;
        int size=Math.Min(15,Math.Min(r.Width,r.Height));
        var box=new Rectangle(r.Left+(r.Width-size)/2,r.Top+(r.Height-size)/2,size,size);
        LauncherPaint.Box(e.Graphics,box,LauncherPaint.Blend(LauncherPaint.Surface,LauncherPaint.Gold,.24),LauncherPaint.Gold,4);
        using(var check=new Pen(LauncherPaint.Gold,2f)){
            check.StartCap=LineCap.Round;check.EndCap=LineCap.Round;check.LineJoin=LineJoin.Round;
            e.Graphics.DrawLines(check,new[]{
                new Point(box.Left+3,box.Top+size/2),
                new Point(box.Left+size/2-1,box.Bottom-4),
                new Point(box.Right-3,box.Top+3)
            });
        }
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){
        e.TextColor=e.Item.Enabled?(e.Item.Selected?LauncherPaint.Paper:LauncherPaint.Paper):
            LauncherPaint.Blend(LauncherPaint.Muted,LauncherPaint.Surface,.32);
        base.OnRenderItemText(e);
    }
}
// Call after populating a context menu and before showing it. Styles its
// nested submenus, rather than only the outer border. No menu commands,
// checked states or callbacks are changed.
public static class LauncherMenuStyle {
    public static ContextMenuStrip Create(){
        var menu=new ContextMenuStrip();
        Apply(menu);
        return menu;
    }
    public static void Apply(ContextMenuStrip menu){
        if(menu==null)throw new ArgumentNullException("menu");
        ApplyDropDown(menu);
        menu.DropShadowEnabled=true;
        menu.ShowItemToolTips=true;
        menu.ShowImageMargin=false;
        menu.ShowCheckMargin=true;
        menu.MinimumSize=new Size(186,0);
    }
    static void ApplyDropDown(ToolStripDropDown menu){
        menu.Renderer=new LauncherMenuRenderer();
        menu.BackColor=LauncherPaint.Surface;
        menu.ForeColor=LauncherPaint.Paper;
        menu.Font=new Font("Segoe UI",9.5f,FontStyle.Regular,GraphicsUnit.Point);
        menu.Padding=new Padding(4,5,4,5);
        foreach(ToolStripItem item in menu.Items)StyleItem(item);
    }
    static void StyleItem(ToolStripItem item){
        if(item is ToolStripSeparator){
            item.AutoSize=false;item.Height=11;item.Margin=new Padding(0,1,0,1);
            return;
        }
        item.Margin=new Padding(1,0,1,0);
        item.Padding=new Padding(8,5,8,5);
        item.BackColor=LauncherPaint.Surface;
        item.ForeColor=item.Enabled?LauncherPaint.Paper:LauncherPaint.Muted;
        var sub=item as ToolStripMenuItem;
        if(sub!=null&&sub.HasDropDownItems){
            ApplyDropDown(sub.DropDown);
            var nested=sub.DropDown as ToolStripDropDownMenu;
            if(nested!=null){nested.ShowImageMargin=false;nested.ShowCheckMargin=true;}
        }
    }
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
