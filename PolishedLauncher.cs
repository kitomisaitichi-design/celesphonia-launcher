using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

public class ChoicePicker : SleekButton {
    public List<object> Items=new List<object>();int selected=-1;public event EventHandler SelectedIndexChanged;
    public int SelectedIndex{get{return selected;}set{int next=value>=0&&value<Items.Count?value:-1;if(next==selected)return;selected=next;Text=next>=0?Items[next].ToString()+"   ▾":"Choose...";if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
    public object SelectedItem{get{return selected>=0&&selected<Items.Count?Items[selected]:null;}set{SelectedIndex=Items.IndexOf(value);}}
    public ChoicePicker(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=1;FlatAppearance.BorderColor=Color.FromArgb(80,88,111);TextAlign=ContentAlignment.MiddleLeft;Padding=new Padding(8,0,8,0);Cursor=Cursors.Hand;
        Click+=delegate{var menu=new ContextMenuStrip{BackColor=BackColor,ForeColor=ForeColor,Font=Font,Renderer=new LauncherMenuRenderer()};for(int i=0;i<Items.Count;i++){int choice=i;var item=new ToolStripMenuItem(Items[i].ToString()){Checked=i==SelectedIndex,ForeColor=ForeColor,BackColor=BackColor};item.Click+=delegate{SelectedIndex=choice;};menu.Items.Add(item);}LauncherMenuLifetime.RetireAfterClose(menu);menu.Show(this,new Point(0,Height));};
    }
}
public class PathDiagram : Control {
    public string[] Titles=new string[3], Notes=new string[3];
    public bool[] Ready=new bool[3];
    public bool Connect=true;
    public PathDiagram(){DoubleBuffered=true;BackColor=Color.FromArgb(29,35,49);}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        int gap=28,w=(Width-32-gap*2)/3,y=22,h=Height-44;
        for(int i=0;i<3;i++){
            int x=16+i*(w+gap);var box=new Rectangle(x,y,w,h);
            using(var pen=new Pen(Color.FromArgb(51,72,78)))g.DrawLine(pen,x,y+1,x+w,y+1);
            using(var pen=new Pen(Ready[i]?Color.FromArgb(77,164,161):LauncherPaint.Gold,2))g.DrawLine(pen,x,y+1,x+32,y+1);
            using(var font=new Font("Segoe UI",11,FontStyle.Italic))TextRenderer.DrawText(g,(i+1).ToString("00"),font,new Rectangle(x+12,y+10,w-24,22),LauncherPaint.Gold,TextFormatFlags.NoPadding);
            using(var font=new Font("Segoe UI",13))TextRenderer.DrawText(g,Titles[i]??"",font,new Rectangle(x+12,y+38,w-24,44),LauncherPaint.Paper,TextFormatFlags.WordBreak|TextFormatFlags.NoPadding);
            using(var font=new Font("Segoe UI",9))TextRenderer.DrawText(g,Notes[i]??"",font,new Rectangle(x+12,y+86,w-24,Math.Max(20,h-92)),LauncherPaint.Muted,TextFormatFlags.WordBreak|TextFormatFlags.NoPadding);
            if(Connect&&i<2){using(var pen=new Pen(Color.FromArgb(152,145,189),2)){int ax=x+w+5,ay=y+h/2;g.DrawLine(pen,ax,ay,ax+18,ay);g.DrawLine(pen,ax+12,ay-5,ax+18,ay);g.DrawLine(pen,ax+12,ay+5,ax+18,ay);}}
        }
    }
    public void Set(string[] titles,string[] notes,bool[] ready){Titles=titles;Notes=notes;Ready=ready;Invalidate();}
}
public class RuntimeControlsToggle : CheckBox {
    readonly HoverMotion motion;
    public double HoverAmount{get{return motion.Amount;}}
    public RuntimeControlsToggle(){
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        AutoSize=false;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.CheckButton;motion=new HoverMotion(this);
    }
    protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
    protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(LauncherPaint.Surface);}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.Clear(LauncherPaint.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;
        double animation=Enabled?motion.Amount:0;
        LauncherPaint.Box(g,new Rectangle(4,4,Width-9,Height-9),LauncherPaint.Blend(LauncherPaint.Surface,LauncherPaint.Gold,animation*.07),Color.Empty,9);
        var box=new Rectangle(12,(Height-28)/2,28,28);
        LauncherPaint.Glow(g,box,5,animation);
        LauncherPaint.Box(g,box,Checked?LauncherPaint.Gold:LauncherPaint.Ink,LauncherPaint.Gold,5);
        if(Checked)using(var pen=new Pen(LauncherPaint.Ink,3)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new Point(box.X+6,box.Y+14),new Point(box.X+12,box.Y+20),new Point(box.X+23,box.Y+8)});}
        LauncherPaint.Text(g,Text,Font,new Rectangle(56,0,Width-150,Height),LauncherPaint.Blend(LauncherPaint.Paper,LauncherPaint.Gold,animation*.25),animation,true);
        var badge=new Rectangle(Width-82,(Height-26)/2,70,26);
        LauncherPaint.Box(g,badge,LauncherPaint.Ink,LauncherPaint.Line,5);
        TextRenderer.DrawText(g,Checked?"ON":"OFF",Font,badge,Checked?LauncherPaint.Gold:LauncherPaint.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,4,Width-10,Height-8),LauncherPaint.Gold,BackColor);
    }
}
public class PolishedLauncher : ReplacementAwareForm {
    Color background=LauncherPaint.Ink,card=Color.FromArgb(17,29,38),muted=Color.FromArgb(153,174,182),accent=Color.FromArgb(29,91,99);
    CheckBox controlsToggle,englishOnInstall;ChoicePicker themePicker;bool settingControls;
    Dictionary<Control,Tuple<Color,Color>> originalColors=new Dictionary<Control,Tuple<Color,Color>>();
    Settings settings;ListBox profiles=new ListBox();ContentScroller main=new ContentScroller();Panel sidebar,playPage,installPage,savePage,helpPage,filesPage,englishPage;
    Button addGame;Label sidebarNote;System.Threading.CancellationTokenSource scanCancellation;bool checkingRunning;
    Label playName,playInfo,saveCount,savePath,status,installNeed,installNote,installLocation,saveTitle,saveNote,saveFolder;
    Button controlsButton,playButton,installButton,copyButton,restoreButton,backupButton,gameFolderButton,saveFolderButton;ChoicePicker targets,methods;
    PathDiagram playDiagram=new PathDiagram(),installDiagram=new PathDiagram();
    readonly List<Button> navigation=new List<Button>();
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
    bool busy,running,discovering;string currentPage="Play";
    List<FoundSource> discovered=new List<FoundSource>();Label discoveryStatus;Button discoveryButton;ProgressBar progress;
    Dictionary<string,FoundSource> preferredSources=new Dictionary<string,FoundSource>();
    DataGridView availability;Label availabilityNote;Profile startingSource;
    Label englishTitle,englishState,englishCounts,englishDetail,englishSeen;SleekProgress englishCoverage;
    Button englishEnable,englishRestore,englishImport,englishExport;PathDiagram englishDiagram=new PathDiagram();EnglishStatus englishCurrent;
    Profile Selected{get{return profiles.SelectedItem as Profile;}}
    List<Profile> LibraryProfiles{get{return ProfileLibrary.Visible(settings.Profiles,settings.SelectedId);}}
    int statusGeneration;Dictionary<string,EnglishStatus> statusCache=new Dictionary<string,EnglishStatus>();bool previewMode;
    readonly Dictionary<string,int> seenCounts=new Dictionary<string,int>();
    readonly Dictionary<string,Task<Tuple<EnglishStatus,int>>> statusJobs=new Dictionary<string,Task<Tuple<EnglishStatus,int>>>();
    ModRecipe Target{get{return targets.SelectedItem as ModRecipe;}}
    public PolishedLauncher(bool preview=false,Settings previewSettings=null){
        previewMode=preview;
        settings=previewSettings??Store.Read();Text="Celesphonia · Mod Library";ClientSize=new Size(1180,820);MinimumSize=new Size(960,640);StartPosition=FormStartPosition.CenterScreen;
        AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",10);BackColor=background;ForeColor=Color.FromArgb(239,242,249);
        AutoScroll=true;AutoScrollMinSize=new Size(1180,0);
        var work=Screen.FromControl(this).WorkingArea;ClientSize=new Size(Math.Min(1180,Math.Max(944,work.Width-48)),Math.Min(820,Math.Max(601,work.Height-72)));
        DoubleBuffered=true;CanReplace=()=>!busy&&!discovering;
        ReplacementRequested+=delegate{if(scanCancellation!=null)scanCancellation.Cancel();status.Text="Opening a new launcher window · finishing active operations…";};
        Label title=LabelAt(this,"Celesphonia",70,23,292,38,22,false);title.Font=new Font("Segoe UI",22,FontStyle.Bold);title.ForeColor=LauncherPaint.Paper;
        LabelAt(this,"GAME LIBRARY",72,63,280,22,9,false,muted);
        var folders=ButtonAt(this,"Folders  ▾",380,30,144,42,()=>{},false);folders.Click+=delegate{ShowFolders(folders);};folders.AccessibleName="Open native folders for the selected version";
        int nx=548;foreach(string name in new[]{"Play","Files","English","Install","Saves","Help"}){string page=name;var b=ButtonAt(this,name,nx,30,96,42,()=>ShowPage(page),false);((SleekButton)b).Navigation=true;navigation.Add(b);nx+=102;}
        sidebar=new SleekCard{Location=new Point(24,111),Size=new Size(296,630),BackColor=card};Controls.Add(sidebar);
        LabelAt(sidebar,"YOUR GAMES",20,20,256,25,9,true,muted);
        profiles.Location=new Point(12,58);profiles.Size=new Size(272,470);profiles.BorderStyle=BorderStyle.None;profiles.BackColor=card;profiles.ForeColor=ForeColor;profiles.DrawMode=DrawMode.OwnerDrawFixed;profiles.ItemHeight=58;profiles.IntegralHeight=false;profiles.DrawItem+=DrawProfile;sidebar.Controls.Add(profiles);
        profiles.SelectedIndexChanged+=delegate{Guard(()=>{
            var update=profiles.SelectedItem as ModRecipe;
            if(update!=null){UpdateProfile();targets.SelectedItem=update;methods.SelectedIndex=1;ShowPage("Install");status.Text=update.Name+" · update available · choose Upgrade & carry saves to prepare it";}
            else if(Selected!=null){settings.SelectedId=Selected.Id;UpdateProfile();}
        });};
        profiles.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Right)return;int index=profiles.IndexFromPoint(e.Location);if(index<0)return;profiles.SelectedIndex=index;ShowFolders(profiles,e.Location);};
        addGame=ButtonAt(sidebar,"+ Add an installed game",16,552,264,42,()=>Guard(AddProfile),false);sidebarNote=LabelAt(sidebar,"Each version keeps a separate save folder.",16,600,264,24,9,false,muted);
        main.Location=new Point(344,111);main.Size=new Size(832,630);main.BackColor=background;Controls.Add(main);
        BuildPlay();BuildInstall();BuildSaves();BuildHelp();BuildFiles();BuildEnglish();
        foreach(var diagram in new[]{playDiagram,installDiagram,englishDiagram})diagram.BackColor=background;
        status=LabelAt(this,"Ready",26,764,1130,34,10,false,muted);progress=new ProgressBar{Location=new Point(26,745),Size=new Size(1130,5),Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=28,Visible=false};Controls.Add(progress);RefreshProfiles();ShowPage("Play");
        if(!string.IsNullOrEmpty(SharedInstallationIndex.Problem))status.Text=SharedInstallationIndex.Problem+" · local discovery remains available";
        if(!preview)Shown+=async delegate{await Discover();};
        if(!preview)Shown+=async delegate{
            try{
                var config=LauncherUpdate.Load();
                if(!config.AutoCheck||string.IsNullOrEmpty(config.Repository))return;
                var latest=await Task.Run(()=>LauncherUpdate.Check(config));
                if(latest==null||IsDisposed||latest.Tag==config.LastNotifiedTag)return;
                config.LastNotifiedTag=latest.Tag;LauncherUpdate.Save(config);
                status.Text="Launcher "+latest.Version+" is available · Help → Check launcher updates";
                if(MessageBox.Show(this,"Celesphonia launcher "+latest.Version+" is available from "+latest.Repository+
                    ".\r\nGame versions, saves and settings will stay untouched.\r\n\r\nCheck and install this launcher update now?",
                    "Launcher update",MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)CheckLauncherUpdate();
            }catch(Exception){/* Startup is fully usable offline; manual check shows diagnostics. */}
        };
        Activated+=delegate{if(!previewMode&&!busy&&Selected!=null)LoadEnglishStatus(Selected);};
        timer.Interval=3500;timer.Tick+=async delegate{if(busy||checkingRunning||previewMode)return;checkingRunning=true;try{var snapshot=new Settings{Profiles=LibraryProfiles.ToList()};bool now=await Task.Run(()=>Store.Running(snapshot));if(!IsDisposed&&now!=running){running=now;UpdateActions();}}catch{}finally{checkingRunning=false;}};timer.Start();
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){e.Cancel=true;if(!ReplacementPending)status.Text="Finishing the current operation · please close this window afterward.";}else{if(scanCancellation!=null)scanCancellation.Cancel();if(!preview)Store.Write(settings);}};
        FormClosed+=delegate{timer.Stop();timer.Dispose();};
        ApplyTheme(settings.LauncherTheme??"Midnight");
        Resize+=delegate{LayoutViewport();};LayoutViewport();
    }
    void ApplyTheme(string name){
        var palettes=new Dictionary<string,Color[]>{
            {"Midnight",new[]{Color.FromArgb(16,18,24),Color.FromArgb(27,31,40),Color.FromArgb(241,244,250),Color.FromArgb(153,164,181),Color.FromArgb(68,99,221),Color.FromArgb(153,176,255)}},
            {"Violet",new[]{Color.FromArgb(31,24,44),Color.FromArgb(50,38,65),Color.FromArgb(246,236,255),Color.FromArgb(190,170,208),Color.FromArgb(99,67,136),Color.FromArgb(208,173,255)}},
            {"Ocean",new[]{Color.FromArgb(13,34,40),Color.FromArgb(24,54,61),Color.FromArgb(233,255,251),Color.FromArgb(156,195,196),Color.FromArgb(25,99,103),Color.FromArgb(120,219,201)}},
            {"Daylight",new[]{Color.FromArgb(236,242,249),Color.FromArgb(255,255,255),Color.FromArgb(32,52,78),Color.FromArgb(78,103,129),Color.FromArgb(206,224,245),Color.FromArgb(40,93,158)}}};
        if(!palettes.ContainsKey(name))name="Midnight";var p=palettes[name];settings.LauncherTheme=name;
        background=p[0];card=p[1];muted=p[3];accent=p[4];LauncherPaint.Ink=background;LauncherPaint.Paper=p[2];LauncherPaint.Gold=p[5];LauncherPaint.Muted=muted;LauncherPaint.Surface=card;LauncherPaint.Line=ControlPaint.Dark(card,.15f);LauncherPaint.Header=card;
        Action<Control> visit=null;visit=c=>{if(!originalColors.ContainsKey(c))originalColors[c]=Tuple.Create(c.BackColor,c.ForeColor);var old=originalColors[c];
            if(old.Item1!=Color.Transparent){bool surface=old.Item1==Color.FromArgb(17,29,38)||old.Item1==Color.FromArgb(24,38,48)||c is ListBox;c.BackColor=surface?card:background;}
            c.ForeColor=old.Item2==Color.FromArgb(153,174,182)?muted:old.Item2==Color.FromArgb(193,165,110)?p[5]:p[2];
            var b=c as SleekButton;if(b!=null&&!b.Navigation)b.BackColor=b.Primary?accent:card;
            foreach(Control child in c.Controls)visit(child);c.Invalidate();};visit(this);
        if(availability!=null){availability.BackgroundColor=card;availability.DefaultCellStyle.BackColor=card;availability.DefaultCellStyle.ForeColor=p[2];availability.DefaultCellStyle.SelectionBackColor=accent;availability.DefaultCellStyle.SelectionForeColor=p[2];availability.ColumnHeadersDefaultCellStyle.BackColor=card;availability.ColumnHeadersDefaultCellStyle.ForeColor=p[2];availability.AlternatingRowsDefaultCellStyle.BackColor=background;availability.GridColor=LauncherPaint.Line;}
        ShowPage(currentPage);Invalidate(true);
    }
    protected override void OnPaintBackground(PaintEventArgs e){
        base.OnPaintBackground(e);var saved=e.Graphics.Save();e.Graphics.TranslateTransform(AutoScrollPosition.X,AutoScrollPosition.Y);
        LauncherPaint.Box(e.Graphics,new Rectangle(24,31,34,34),accent,Color.Empty,10);
        using(va