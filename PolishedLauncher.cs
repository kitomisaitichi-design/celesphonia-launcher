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
    CheckBox controlsToggle;ChoicePicker themePicker;bool settingControls;
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
        using(var logoFont=new Font("Segoe UI",16,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,"C",logoFont,new Rectangle(24,30,34,34),LauncherPaint.Paper,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        using(var pen=new Pen(LauncherPaint.Surface))e.Graphics.DrawLine(pen,24,94,1154,94);e.Graphics.Restore(saved);
    }
    Label LabelAt(Control parent,string text,int x,int y,int w,int h,float size,bool bold,Color? color=null){var label=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),AutoEllipsis=true,UseMnemonic=false,Font=new Font(size>=23?"Segoe UI":"Segoe UI",size,size>=23?FontStyle.Regular:bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color??ForeColor,BackColor=Color.Transparent};parent.Controls.Add(label);return label;}
    Button ButtonAt(Control parent,string text,int x,int y,int w,int h,Action action,bool primary){var b=new SleekButton{Text=text,Location=new Point(x,y),Size=new Size(w,h),Primary=primary,BackColor=primary?accent:Color.FromArgb(24,38,48),ForeColor=ForeColor,Font=new Font("Segoe UI",10,primary?FontStyle.Bold:FontStyle.Regular),Cursor=Cursors.Hand};b.Click+=delegate{Guard(action);};parent.Controls.Add(b);return b;}
    void BindHint(Control control,Label label,string text){string normal=label.Text;control.MouseEnter+=delegate{label.Text=text;};control.MouseLeave+=delegate{label.Text=normal;};}
    void LayoutViewport(){
        if(status==null||sidebar==null)return;
        int height=Math.Max(330,ClientSize.Height-190);
        main.Height=height;sidebar.Height=height;profiles.Height=height-160;addGame.Top=height-78;sidebarNote.Top=height-30;
        status.Top=111+height+23;progress.Top=111+height+4;
        FitPage();
    }
    Panel Page(){var p=new GrimoirePage{Dock=DockStyle.Fill,BackColor=background,Visible=false};main.Canvas.Controls.Add(p);return p;}
    void DrawProfile(object sender,DrawItemEventArgs e){
        if(e.Index<0)return;var p=profiles.Items[e.Index] as Profile;var update=profiles.Items[e.Index] as ModRecipe;bool selected=(e.State&DrawItemState.Selected)!=0;
        using(var b=new SolidBrush(card))e.Graphics.FillRectangle(b,e.Bounds);
        if(selected)LauncherPaint.Box(e.Graphics,new Rectangle(e.Bounds.X+3,e.Bounds.Y+3,e.Bounds.Width-6,e.Bounds.Height-6),ControlPaint.Light(card,.12f),Color.Empty,9);
        string name=(p==null?update.Name:p.Name).Replace(" - current installation","").Replace(" - base","");int slots=p==null?0:CountSaves(p);
        using(var font=new Font("Segoe UI",10,selected?FontStyle.Bold:FontStyle.Regular))TextRenderer.DrawText(e.Graphics,name,font,new Rectangle(e.Bounds.X+14,e.Bounds.Y+6,e.Bounds.Width-28,30),ForeColor,TextFormatFlags.WordBreak|TextFormatFlags.NoPadding);
        using(var font=new Font("Segoe UI",8.5f))TextRenderer.DrawText(e.Graphics,p==null?"Update available · click to install":slots+" saves  ·  "+(p.Name.Contains("trial")?"Trial build":"Installed"),font,new Rectangle(e.Bounds.X+14,e.Bounds.Y+37,e.Bounds.Width-28,18),muted,TextFormatFlags.NoPadding);
    }
    int CountSaves(Profile p){return Store.SaveFiles(p.SavePath).Count(x=>System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(x),"^file[0-9]+\\.rpgsave$",System.Text.RegularExpressions.RegexOptions.IgnoreCase));}
    void BuildPlay(){
        playPage=Page();LabelAt(playPage,"READY TO PLAY",0,0,600,22,10,true,muted);playName=LabelAt(playPage,"",0,31,806,49,25,true);
        playInfo=LabelAt(playPage,"",0,83,802,42,10,false,muted);
        var saves=new SleekCard{Location=new Point(0,354),Size=new Size(812,90),BackColor=card};playPage.Controls.Add(saves);
        saveCount=LabelAt(saves,"",18,14,754,26,13,true);savePath=LabelAt(saves,"",18,49,774,30,9,false,muted);
        var runtimeCard=new SleekCard{Location=new Point(0,132),Size=new Size(812,90),BackColor=card};playPage.Controls.Add(runtimeCard);
        controlsToggle=new RuntimeControlsToggle{Text="Enable runtime controls for this game",Location=new Point(12,8),Size=new Size(788,46),BackColor=Color.Transparent,ForeColor=ForeColor,Font=new Font("Segoe UI",12,FontStyle.Bold),AccessibleName="Enable runtime controls before launching",AccessibleDescription="Click anywhere on this row or press Space to choose runtime controls for the next launch."};runtimeCard.Controls.Add(controlsToggle);
        controlsToggle.CheckedChanged+=delegate{if(settingControls||Selected==null)return;Selected.RuntimeControls=controlsToggle.Checked;if(!previewMode)Store.Write(settings);};
        var runtimeNote=LabelAt(runtimeCard,"F8 opens the menu · Separate session saves · Original game files remain unchanged",16,59,780,23,9,false,muted);
        BindHint(controlsToggle,runtimeNote,"Remembered per game. Click this row or press Space to choose controls for the next launch.");
        playButton=ButtonAt(playPage,"Switch & Play",0,238,320,50,()=>Guard(Launch),true);gameFolderButton=ButtonAt(playPage,"Game folder",336,238,230,50,()=>Guard(()=>LauncherFolders.Open(Selected.Root)),false);saveFolderButton=ButtonAt(playPage,"Saves folder",582,238,230,50,()=>Guard(OpenSaves),false);
        controlsButton=ButtonAt(playPage,"Control session folder",0,300,300,42,()=>Guard(()=>{if(Selected!=null){var folder=Store.Child(Path.Combine(Store.Home,"Control Sessions"),Selected.Id);Directory.CreateDirectory(folder);LauncherFolders.Open(folder);}}),false);
        var actionNote=LabelAt(playPage,"Close the active game before switching versions. Themes are in Help.",316,300,496,42,10,false,muted);
        BindHint(playButton,actionNote,"Launch this version with its own saves and the control setting above.");
        BindHint(gameFolderButton,actionNote,"Open the original installation folder for this game.");
        BindHint(saveFolderButton,actionNote,"Open this version's original save folder.");
        BindHint(controlsButton,actionNote,"Open the separate control session and its edited saves.");
    }
    void BuildInstall(){
        installPage=Page();LabelAt(installPage,"ADD A VERSION",0,0,760,24,10,true,muted);LabelAt(installPage,"What files do I need?",0,31,480,43,24,true);
        LabelAt(installPage,"Choose a version and what you already have. The path below updates with your choice.",0,82,812,23,10,false,muted);
        targets=new ChoicePicker{Location=new Point(0,137),Size=new Size(394,36),Font=new Font("Segoe UI",10),BackColor=card,ForeColor=ForeColor,AccessibleName="Version to install"};
        var recipes=RecipeSupport.Catalog().Recipes.Where(x=>x.Id!="abu-51-01").ToList();recipes.Add(new ModRecipe{Id="gorpg-1-23",Name="Gorpg 1.23 - clean base",RequiredBase="standalone",Components=new[]{"Clean Chinese Gorpg 1.23"},Standalone=true});
        recipes.Add(new ModRecipe{Id="ch-current",Name="CH",RequiredBase="standalone",Components=new[]{"CH reference build"},Standalone=true});
        targets.Items.AddRange(recipes.ToArray());installPage.Controls.Add(targets);
        methods=new ChoicePicker{Location=new Point(410,137),Size=new Size(402,36),Font=new Font("Segoe UI",10),BackColor=card,ForeColor=ForeColor,AccessibleName="Installation method"};
        methods.Items.AddRange(new object[]{"Use a version-to-version pack","Use the original bundle / game","Add an already installed game"});installPage.Controls.Add(methods);
        targets.SelectedIndexChanged+=delegate{Guard(UpdateInstall);};methods.SelectedIndexChanged+=delegate{Guard(UpdateInstall);};targets.SelectedIndex=0;methods.SelectedIndex=0;
        installDiagram.Location=new Point(0,190);installDiagram.Size=new Size(812,183);installPage.Controls.Add(installDiagram);
        var checklist=new SleekCard{Location=new Point(0,389),Size=new Size(812,112),BackColor=card};installPage.Controls.Add(checklist);installNeed=LabelAt(checklist,"",17,12,778,94,10,false);
        installNote=LabelAt(installPage,"",0,512,800,37,9,false,muted);
        installNote.Width=560;ButtonAt(installPage,"Starting version...",580,512,232,35,()=>ChooseStartingVersion(),false);
        installButton=ButtonAt(installPage,"Choose files & install",0,556,260,42,()=>GuidedInstall(),true);
        ButtonAt(installPage,"Bundle + hotfixes...",274,556,225,42,()=>Guard(OrderedLayers),false);
        ButtonAt(installPage,"Install location...",514,556,190,42,()=>Guard(ChangeLocation),false);
        installLocation=LabelAt(installPage,"",0,609,810,22,9,false,muted);
        discoveryButton=ButtonAt(installPage,"Find files",668,31,144,36,()=>ScanAgain(),false);
        ButtonAt(installPage,"View found files",502,31,153,36,()=>ShowFoundFiles(),false);
        discoveryStatus=LabelAt(installPage,"Scanning nearby files...",0,107,805,24,9,false,muted);
    }
    void BuildSaves(){
        savePage=Page();LabelAt(savePage,"SAVE LIBRARY",0,0,760,24,10,true,muted);saveTitle=LabelAt(savePage,"",0,32,812,48,24,true);
        LabelAt(savePage,"Your saves return automatically when you switch back to this version.",0,91,790,34,11,false,muted);
        var panel=new SleekCard{Location=new Point(0,150),Size=new Size(812,175),BackColor=card};savePage.Controls.Add(panel);saveNote=LabelAt(panel,"",18,16,776,31,15,true);saveFolder=LabelAt(panel,"",18,59,776,61,10,false,muted);
        ButtonAt(panel,"Open save folder",18,126,210,36,()=>Guard(OpenSaves),false);
        backupButton=ButtonAt(savePage,"Back up now",0,353,248,46,()=>Guard(()=>{if(!GameClosed())return;status.Text="Save backup created: "+Store.Backup(Selected);UpdateProfile();}),true);
        restoreButton=ButtonAt(savePage,"Restore a backup",264,353,252,46,()=>Guard(Restore),false);
        copyButton=ButtonAt(savePage,"Copy from another version...",531,353,281,46,()=>Guard(CopySaves),false);
        var caution=new SleekCard{Location=new Point(0,430),Size=new Size(812,173),BackColor=Color.FromArgb(40,33,36)};savePage.Controls.Add(caution);
        LabelAt(caution,"Before copying saves across versions",18,18,775,28,13,true,Color.FromArgb(238,203,164));
        LabelAt(caution,"Copying preserves the source and backs up the destination. It does not convert a save.\r\n\r\nThe 3.3.5 trial removed the hypnosis storyline. Older and newer versions can use different IDs or state. A transferred save may not work correctly. Keep your original save set.",18,61,775,99,10,false,muted);
    }
    void BuildEnglish(){
        englishPage=Page();LabelAt(englishPage,"ENGLISH TEXT · REVERSIBLE",0,0,800,24,10,true,muted);
        englishTitle=LabelAt(englishPage,"Choose a version",0,31,812,52,24,true);
        englishState=LabelAt(englishPage,"",0,93,810,28,13,true);
        englishCoverage=new SleekProgress{Location=new Point(0,140),Size=new Size(812,10),Minimum=0,Maximum=1000};englishPage.Controls.Add(englishCoverage);
        englishCounts=LabelAt(englishPage,"",0,170,812,54,10,false,muted);
        englishDiagram.Location=new Point(0,237);englishDiagram.Size=new Size(812,178);englishDiagram.Connect=false;englishPage.Controls.Add(englishDiagram);
        englishDetail=LabelAt(englishPage,"",0,431,812,56,10,false,muted);
        englishEnable=ButtonAt(englishPage,"Enable / update English",0,505,282,43,()=>EnglishAction(0),true);
        englishRestore=ButtonAt(englishPage,"Use original text",299,505,238,43,()=>EnglishAction(1),false);
        englishImport=ButtonAt(englishPage,"Import translated JSON",554,505,258,43,()=>EnglishAction(2),false);
        englishExport=ButtonAt(englishPage,"Export untranslated",0,565,282,40,()=>EnglishAction(3),false);
        ButtonAt(englishPage,"English pack folder",299,565,238,40,()=>Guard(()=>LauncherFolders.Open(englishCurrent!=null&&englishCurrent.Supported?Path.Combine(Store.Home,"English",englishCurrent.VersionId):Path.Combine(Store.Home,"English"))),false);
        ButtonAt(englishPage,"Seen text log",554,565,258,40,()=>Guard(()=>{if(Selected==null)return;string path=Path.Combine(Store.WebRoot(Selected.Root),"celesphonia-english","seen-untranslated.json");if(!File.Exists(path)){MessageBox.Show(this,"This log appears after English is enabled and untranslated text is displayed in game.");return;}Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}),false);
        englishSeen=LabelAt(englishPage,"",0,611,812,19,9,false,muted);
    }
    void UpdateEnglish(){
        if(englishTitle==null)return;var s=englishCurrent??new EnglishStatus{Detail="Checking this version's English overlay…"};var pack=s.Pack;
        englishTitle.Text=Selected==null?"Choose a version":Selected.Name.Replace(" - current installation","");
        englishState.Text=Selected==null?"Add an installed game to enable English":s.Modified?"Overlay needs attention":!s.Supported?s.VersionId=="ch-current"?"CH · existing English reference":"Unsupported or missing game":s.Enabled?s.UpdateAvailable?"English enabled · update available":"English enabled · partial translation":"Original text active · English available";
        englishState.ForeColor=s.Modified?Color.FromArgb(243,180,135):s.Enabled?Color.FromArgb(141,220,188):ForeColor;
        englishCoverage.Value=pack==null||s.TotalTexts==0?0:Math.Min(1000,(s.Enabled?s.AppliedTexts:s.AvailableTexts)*1000/s.TotalTexts);
        englishCounts.Text=Selected==null?"Use + Add an installed game, or open Install to prepare a version.":pack==null?s.Detail:(s.Enabled?s.AppliedTexts:s.AvailableTexts).ToString("N0")+" / "+s.TotalTexts.ToString("N0")+" catalog entries available · "+(100.0*(s.Enabled?s.AppliedTexts:s.AvailableTexts)/Math.Max(1,s.TotalTexts)).ToString("0.0")+"%\r\n"+pack.ReviewedTexts.ToString("N0")+" reviewed · "+pack.ReferenceTexts.ToString("N0")+" reference matches · "+pack.DerivedTexts.ToString("N0")+" description-line matches";
        englishDiagram.Set(new[]{"This game version","English display layer","Original & saves"},new[]{s.VersionId=="ch-current"?"CH is read only.":Selected==null?"Select an installed game.":Selected.Name,pack==null?"A matching pack is needed.":"Messages, objectives and UI labels render in English when matched. Untranslated text stays visible.","Original entry page is backed up. Restore it with one button; saves keep their names and IDs."},new[]{s.Supported,pack!=null,true});
        if(pack!=null&&pack.IndexedEventFieldsPending.HasValue)englishDiagram.Set(new[]{"Event text review","Other text to review","Native metadata"},new[]{pack.IndexedEventFieldsPending.Value.ToString("N0")+" pending whole event fields in the current index. Full gameplay routes remain unverified.",pack.OtherDisplayReviewFields.GetValueOrDefault().ToString("N0")+" fields need usage or translation review: menus, descriptions and old render logs.",pack.DeveloperVariableLabels.GetValueOrDefault().ToString("N0")+" variable labels are tracked separately. Native IDs and values stay intact."},new[]{pack.IndexedEventFieldsPending.Value==0,false,true});
        englishDetail.Text=pack==null?"Overlays are separate for Gorpg 1.23, Maple 1.7.2, 3.3.4 and 3.3.5 trial.":s.Detail+"\r\nThe bar measures catalog coverage; it does not mean the game is fully translated.";
        int seen;englishSeen.Text=Selected==null?"":seenCounts.TryGetValue(Selected.Id,out seen)?"Historical render log: "+seen.ToString("N0")+" entries; some are covered by the current overlay.":"Checking the render log…";
        UpdateActions();
    }
    async void EnglishAction(int action){
        if(busy||Selected==null)return;var profile=Selected;string file=null;
        if(action<3&&!GameClosed())return;
        if(action==2){file=PickArchive("Import a translated MTool JSON (source keys, translated values)","MTool translation JSON|*.json",profile.Root,"ManualTransFile.json");if(file==null)return;}
        if(action==3){using(var d=new SaveFileDialog{Title="Export this version's remaining text",Filter="MTool JSON|*.json",FileName="Remaining-"+englishCurrent.VersionId+".json",InitialDirectory=Path.Combine(Store.Home,"English")}){if(d.ShowDialog(this)!=DialogResult.OK)return;file=d.FileName;}}
        busy=true;SetEnabled(false);progress.Visible=true;status.Text=action==0?"Preparing the reversible English display layer...":action==1?"Restoring the exact original entry page...":"Processing the translation JSON...";
        try{string message=await Task.Run(()=>{if(action==0){EnglishSupport.Apply(profile);return "English overlay enabled. This batch is partial; remaining text stays in its original language.";}if(action==1){EnglishSupport.Restore(profile);return "Original text restored; your saves and game data were preserved.";}if(action==2)return EnglishSupport.Import(profile,file);EnglishSupport.ExportRemaining(profile,file);return "Remaining text exported: "+file;});status.Text=message;UpdateProfile();UpdateAvailability();}
        catch(Exception e){MessageBox.Show(this,e.Message,"English overlay",MessageBoxButtons.OK,MessageBoxIcon.Error);status.Text="English operation stopped: "+e.Message;}
        finally{busy=false;progress.Visible=false;SetEnabled(true);UpdateActions();UpdateEnglish();}
    }
    void BuildHelp(){
        helpPage=Page();LabelAt(helpPage,"HOW IT WORKS",0,0,760,24,10,true,muted);LabelAt(helpPage,"Choose the branch you want to play",0,32,812,54,23,true);
        LabelAt(helpPage,"LATEST THREE-IN-ONE RELEASE: Ver1.052\r\nFrom a verified Three-in-One 3.3.5 v1.04 installation, choose the creator's Ver1.051 or Ver1.052 RAR. The launcher recognizes renamed RAR/ZIP files and extracted folders by their contents, keeps the original game, and installs a separate version with its matching English pack.\r\n\r\n01   Older versions remain supported\r\nCH, Gorpg 1.23, Maple 1.7.2, 3.3.4 and older Three-in-One releases retain installation and English support. Development focuses on the newest two releases and new Maple releases.\r\n\r\n02   Original files work\r\nUse complete game folders or ZIP/RAR/7z archives. The bundled RAR5 extractor handles both creator releases, including their Chinese filenames.\r\n\r\n03   Files shows what is ready\r\nInstalled games and matching source archives are detected together. Choose the desired version; verified v1.052 includes the boss-restraint fix.\r\n\r\n04   Saves and English remain separate\r\nUpgrades copy compatible saves forward and preserve originals. English is reversible and changes only displayed text, not gameplay scripts or saved values.",0,110,800,404,11,false,muted);
        ButtonAt(helpPage,"Quick start guide",0,540,345,44,()=>Guard(()=>Process.Start(new ProcessStartInfo(Path.Combine(Store.Home,"README.txt")){UseShellExecute=true})),true);
        ButtonAt(helpPage,"Open delta packs",361,540,250,44,()=>Guard(()=>{Directory.CreateDirectory(Path.Combine(Store.Home,"Delta Packs"));Process.Start("explorer.exe",Store.Q(Path.Combine(Store.Home,"Delta Packs")));}),false);
        ButtonAt(helpPage,"Game changelog",625,540,180,44,()=>Guard(OpenGameChangelog),false);
        LabelAt(helpPage,"LAUNCHER APPEARANCE",0,615,400,25,10,true,muted);
        themePicker=new ChoicePicker{Location=new Point(0,651),Size=new Size(310,40),Font=Font,BackColor=card,ForeColor=ForeColor,AccessibleName="Launcher theme"};themePicker.Items.AddRange(new object[]{"Midnight","Violet","Ocean","Daylight"});themePicker.SelectedItem=settings.LauncherTheme??"Midnight";helpPage.Controls.Add(themePicker);
        themePicker.SelectedIndexChanged+=delegate{ApplyTheme(themePicker.SelectedItem.ToString());if(!previewMode)Store.Write(settings);};
        LabelAt(helpPage,"Themes are saved for the launcher. Set the game menu's theme and opacity in its top bar; drag the menu header to move it.",330,649,482,53,10,false,muted);
        LabelAt(helpPage,"LAUNCHER UPDATES  ·  v"+LauncherUpdate.CurrentVersion,0,719,490,24,10,true,muted);
        ButtonAt(helpPage,"Check launcher updates",0,750,286,43,()=>CheckLauncherUpdate(),true);
        ButtonAt(helpPage,"GitHub source",306,750,230,43,()=>ConfigureLauncherUpdates(),false);
        LabelAt(helpPage,"Uses GitHub Releases with a required SHA-256 checksum. Offers updates when a verified newer launcher is available; your installed games, saves, F8 sessions and profiles stay untouched.",0,801,805,62,10,false,muted);
    }
    void OpenGameChangelog(){
        string file=Path.Combine(Store.Home,"GAME MOD CHANGELOG.md");
        if(!File.Exists(file))LauncherUpdate.ExportChangelog(file);
        Process.Start(new ProcessStartInfo(file){UseShellExecute=true});
    }
    bool ConfigureLauncherUpdates(){
        if(busy)return false;
        var config=LauncherUpdate.Load();
        using(var dialog=new Form{Text="Launcher release source",ClientSize=new Size(610,210),Font=Font,
            StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,
            MinimizeBox=false,MaximizeBox=false}){
            var label=new Label{Location=new Point(20,17),Size=new Size(566,45),
                Text="Official updates: "+LauncherUpdate.OfficialRepository+"\r\nUse owner/repository format. Disable automatic checks below if preferred."};
            var input=new TextBox{Location=new Point(20,77),Width=560,Text=config.Repository??""};
            var auto=new CheckBox{Location=new Point(20,119),Width=540,Text="Automatically check for launcher updates on startup",Checked=config.AutoCheck};
            var save=new Button{Location=new Point(440,163),Width=140,Text="Save",DialogResult=DialogResult.OK};
            dialog.Controls.Add(label);dialog.Controls.Add(input);dialog.Controls.Add(auto);dialog.Controls.Add(save);dialog.AcceptButton=save;
            if(dialog.ShowDialog(this)!=DialogResult.OK)return false;
            config.Repository=input.Text.Trim();config.AutoCheck=auto.Checked;config.LastNotifiedTag=null;
            LauncherUpdate.Save(config);status.Text=string.IsNullOrEmpty(config.Repository)?
                "GitHub update source not configured. Launcher still works offline.":
                "Launcher update source set to "+config.Repository;
            return !string.IsNullOrEmpty(config.Repository);
        }
    }
    async void CheckLauncherUpdate(){
        if(busy||previewMode)return;
        try{
            var config=LauncherUpdate.Load();
            if(string.IsNullOrEmpty(config.Repository)&&!ConfigureLauncherUpdates())return;
            config=LauncherUpdate.Load();
            busy=true;status.Text="Checking GitHub Releases for launcher updates…";
            var info=await Task.Run(()=>LauncherUpdate.Check(config));
            if(IsDisposed)return;
            if(info==null){
                status.Text="Celesphonia launcher v"+LauncherUpdate.CurrentVersion+" is up to date.";
                MessageBox.Show(this,"You're running the newest launcher version available from "+config.Repository+
                    ".\r\nLatest supported game/mod release: Three-in-One 3.3.5 Ver1.052. Game releases are independent of launcher updates.","Launcher updates");
                return;
            }
            status.Text="Launcher v"+info.Version+" available from "+info.Repository;
            if(MessageBox.Show(this,"Install Celesphonia launcher "+info.Version+" from "+info.Repository+
                "?\r\n\r\nThe ZIP and GitHub checksum will be verified. The launcher will close, update, and restart. "+
                "Game directories, saves, profiles and F8 sessions are excluded. A rollback copy will be retained.",
                "Update launcher",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            SetEnabled(false);progress.Visible=true;status.Text="Downloading and verifying launcher update…";
            string manifest=await Task.Run(()=>LauncherUpdate.Stage(info));
            LauncherUpdate.StartApply(manifest);
            busy=false;progress.Visible=false;
            status.Text="Update verified · restarting launcher…";
            Close();
        }catch(Exception problem){
            if(!IsDisposed){status.Text="Launcher update stopped: "+problem.Message;
                MessageBox.Show(this,problem.Message,"Launcher update",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }finally{
            busy=false;if(!IsDisposed){progress.Visible=false;SetEnabled(true);}
        }
    }
    void FitPage(){
        var active=new[]{playPage,installPage,savePage,helpPage,filesPage,englishPage}.FirstOrDefault(p=>p!=null&&p.Visible);
        if(active!=null)main.Canvas.Height=Math.Max(main.ClientSize.Height,active.Controls.Cast<Control>().Select(c=>c.Bottom+12).DefaultIfEmpty(0).Max());
        main.RefreshRange();
    }
    void ShowPage(string page){
        currentPage=page;
        playPage.Visible=page=="Play";installPage.Visible=page=="Install";savePage.Visible=page=="Saves";helpPage.Visible=page=="Help";filesPage.Visible=page=="Files";englishPage.Visible=page=="English";
        foreach(var b in navigation){((SleekButton)b).Selected=b.Text==page;b.ForeColor=b.Text==page?LauncherPaint.Gold:ForeColor;b.BackColor=background;b.Invalidate();}
        if(page=="Install")UpdateInstall();if(page=="Files")UpdateAvailability();
        if(page=="English"){if(Selected!=null&&!busy){if(previewMode)englishCurrent=EnglishSupport.Status(Selected);else LoadEnglishStatus(Selected);}UpdateEnglish();}
        FitPage();main.Position=0;
    }
    void RefreshProfiles(){
        var id=settings.SelectedId;var visible=LibraryProfiles;var old=settings.Profiles.FirstOrDefault(x=>x.Id==id);
        profiles.BeginUpdate();try{profiles.Items.Clear();foreach(var entry in ProfileLibrary.Entries(visible,id))profiles.Items.Add(entry);
            profiles.SelectedItem=visible.FirstOrDefault(x=>x.Id==id)??(old==null?null:visible.FirstOrDefault(x=>x.Name==old.Name))??profiles.Items.OfType<Profile>().FirstOrDefault();
        }finally{profiles.EndUpdate();}UpdateProfile();
    }
    void UpdateProfile(){
        var p=Selected;settingControls=true;controlsToggle.Checked=p!=null&&p.RuntimeControls;settingControls=false;controlsToggle.Enabled=p!=null&&!busy;if(p==null){
            englishCurrent=null;playName.Text="Add your first game";playInfo.Text="Open Files or Install to use an original archive, extracted game folder, or starting version.";
            playDiagram.Set(new[]{"Add a version","Choose English","Ready to play"},new[]{"Register an installed game or prepare one in Install.","Enable the matching English display pack from the English page.","Each version keeps its own saves and launch settings."},new[]{false,false,false});
            saveCount.Text="No game selected";savePath.Text="Add an installed version to see its save folder.";
            saveTitle.Text="Your save library";saveNote.Text="Add an installed version first";saveFolder.Text="Save folders and backups appear here after you choose a game.";
            UpdateEnglish();UpdateActions();return;
        }int count=CountSaves(p);playName.Text=p.Name.Replace(" - current installation","");statusCache.TryGetValue(p.Id,out englishCurrent);playInfo.Text=p.Note+"\r\nText: "+(englishCurrent==null?"Checking…":englishCurrent.Short);LoadEnglishStatus(p);
        playDiagram.Set(new[]{"Selected version","Its own saves","Back up & play"},new[]{p.Name,"Return to these saves every time you select this version.","A timestamped save backup is created before launch."},new[]{true,true,true});
        saveCount.Text=count+" save slots  ·  "+(count==0?"Ready for a new game":"Your progress is here");savePath.Text=p.SavePath;
        saveTitle.Text=p.Name;saveNote.Text=count+" numbered save slots";saveFolder.Text=p.SavePath;
        profiles.Invalidate();UpdateEnglish();UpdateActions();
    }
    async void LoadEnglishStatus(Profile profile){
        int generation=++statusGeneration;Task<Tuple<EnglishStatus,int>> job;
        if(!statusJobs.TryGetValue(profile.Id,out job)){job=Task.Run(()=>Tuple.Create(EnglishSupport.Status(profile),EnglishSupport.SeenCount(profile)));statusJobs[profile.Id]=job;}
        try{var result=await job;if(IsDisposed)return;statusCache[profile.Id]=result.Item1;seenCounts[profile.Id]=result.Item2;if(generation!=statusGeneration||Selected==null||Selected.Id!=profile.Id)return;englishCurrent=result.Item1;playInfo.Text=profile.Note+"\r\nText: "+result.Item1.Short;UpdateEnglish();UpdateActions();}
        catch(Exception e){if(!IsDisposed&&generation==statusGeneration)status.Text="Could not check this version: "+e.Message;}
        finally{Task<Tuple<EnglishStatus,int>> current;if(statusJobs.TryGetValue(profile.Id,out current)&&current==job)statusJobs.Remove(profile.Id);}
    }
    Profile CH(){return Installed("ch-current")??LibraryProfiles.FirstOrDefault(x=>FileDiscovery.IsCH(x.Root));}
    Profile SupportedSource(string targetId){if(startingSource!=null&&UniversalSupport.Identify(startingSource.Root)!=null&&UniversalSupport.Identify(startingSource.Root)!=targetId)return startingSource;if(Selected!=null&&UniversalSupport.Identify(Selected.Root)!=null&&UniversalSupport.Identify(Selected.Root)!=targetId)return Selected;return LibraryProfiles.FirstOrDefault(x=>UniversalSupport.Identify(x.Root)!=null&&UniversalSupport.Identify(x.Root)!=targetId);}
    Profile Installed(string id){return LibraryProfiles.FirstOrDefault(x=>UniversalSupport.Identify(x.Root)==id);}
    FoundSource AnyPack(string id){if(UpgradeSupport.IsUpgrade(id))return Found(id,"Any-version pack");return Found(id,"Any-version pack")??(File.Exists(Path.Combine(Store.Home,"Delta Packs",UniversalSupport.PackName))?new FoundSource{Kind="Any-version pack",Path=Path.Combine(Store.Home,"Delta Packs",UniversalSupport.PackName),RecipeId=id}:null);}
    FoundSource Original(string id){return Found(id,"Full game archive")??Found(id,"Extracted bundle")??Found(id,"Original archive");}
    Profile Clean(){return Installed("gorpg-1-23");}
    string DefaultPack(ModRecipe recipe){return Path.Combine(Store.Home,"Delta Packs",recipe.Id+".cgdelta.zip");}
    FoundSource Found(string recipe,string kind){FoundSource chosen;if(preferredSources.TryGetValue(recipe+"/"+kind,out chosen)&&string.IsNullOrEmpty(chosen.Problem)&&(File.Exists(chosen.Path)||Directory.Exists(chosen.Path)))return chosen;return discovered.FirstOrDefault(x=>x.RecipeId==recipe&&x.Kind==kind&&string.IsNullOrEmpty(x.Problem));}
    string ArchiveHint(ModRecipe recipe){
        if(!string.IsNullOrEmpty(recipe.ArchiveName))return recipe.ArchiveName;
        if(recipe.Id=="gorpg-1-23")return "魔法少女苍蓝星1.23gorpg汉.7z.001 + .002";
        if(recipe.Id=="ch-current")return "The full CH game archive or extracted CH game folder";
        return "The complete 3.3.4 bundle (.001 through .004 together), or its extracted content folder";
    }
    void UpdateInstall(){
        if(Target==null || methods.SelectedIndex<0 || installNeed==null)return;var recipe=Target;int route=methods.SelectedIndex;
        var ch=CH();var clean=Clean();var any=AnyPack(recipe.Id);var delta=any??Found(recipe.Id,"Delta pack");bool found=delta!=null||File.Exists(DefaultPack(recipe));var bundle=Original(recipe.Id);
        if(UpgradeSupport.IsUpgrade(recipe.Id)&&route!=2){
            var source=UpgradeSupport.StartingProfile(settings,recipe.Id);var installed=Installed(recipe.Id);
            installDiagram.Set(new[]{source==null?"Compatible source":source.Name,recipe.Name,"Keep your progress"},new[]{source==null?"Add a compatible source installation first.":"Verified source build; original folder retained.",bundle==null?"Choose the creator's complete update archive or extracted folder.":"Update found; all content hashes checked before copying.","New independent installation; saves backed up, copied and verified. Forward migration only."},new[]{source!=null,bundle!=null,installed!=null});
            installNeed.Text=(source==null?"• Needed: registered compatible source":"✓ Starting version: "+source.Name)+"\r\n"+(bundle==null?"• Needed: the complete "+recipe.Name+" update bundle":"✓ Update: "+Path.GetFileName(bundle.Path))+"\r\n✓ 3.3.4 hypnosis stays separate; newer saves cannot move backward";
            installNote.Text="Saves and English carry forward into a verified private copy.";installButton.Text=installed==null?"Upgrade & carry saves":"Use installed version";installLocation.Text="New installations: "+settings.InstallRoot;UpdateActions();return;
        }
        if(route==0){
            var source=any==null?ch:SupportedSource(recipe.Id);bool already=Installed(recipe.Id)!=null;
            installDiagram.Set(new[]{source==null?"Any supported version":UniversalSupport.DetectedName(source.Root)??source.Name,recipe.Name+" route",already?"Already installed":"Independent game"},new[]{source==null?"CH, Gorpg, Maple, 3.3.4, or 3.3.5. Choose Starting version.":"Version identified from its files, not its folder name.",any!=null?"Any-version pack found. No CH prerequisite.":found?"Legacy CH-only pack found.":"Choose the any-version pack.",already?"Use Files to switch directly with its existing saves.":"A complete new folder with its own empty saves."},new[]{source!=null,found,already});
            installNeed.Text=(source!=null?"✓ Starting game: "+(UniversalSupport.DetectedName(source.Root)??source.Name):"• Choose a supported starting game folder or full archive")+"\r\n"+(any!=null?"✓ Any-version pack found":found?"• Legacy pack needs CH; use the any-version pack for other starting games":"• Needed: "+UniversalSupport.PackName)+"\r\n"+(already?"✓ This target is already installed — switch directly in Files":"✓ Original games and their saves stay intact");
            installNote.Text="Any-version routes verify the starting files. Old single-target packs remain CH-only.";
        }else if(route==1 && !recipe.Standalone && (bundle==null||bundle.Kind!="Full game archive")){
            installDiagram.Set(new[]{"Clean Gorpg 1.23",recipe.Name+" bundle","Independent game"},new[]{clean==null?"Register or reconstruct the clean base first.":"Verified base is registered.",bundle==null?"Choose the complete bundle.":"Matching bundle found nearby.","Private base copy + ordered overwrites. Saves start empty."},new[]{clean!=null,bundle!=null,true});
            installNeed.Text=(clean!=null?"✓ Clean Gorpg base is registered":"• Needed: clean Gorpg 1.23; reconstruct it from any supported version or use its original archive")+"\r\n"+(bundle==null?"• Needed bundle: "+ArchiveHint(recipe):"✓ Bundle found: "+Path.GetFileName(bundle.Path))+"\r\n• Never stack another complete bundle or Maple with this bundle";
            installNote.Text="Content folders overwrite matching folders inside www. 3.3.5 already contains its three components.";
        }else{
            installDiagram.Set(new[]{"Complete game",recipe.Name,"Register & play"},new[]{"Folder containing Game.exe or nw.exe and package.json.","Use this branch's own complete installation.","Existing files and saves are kept in their current folder."},new[]{false,false,true});
            installNeed.Text="• Use an extracted game folder, ZIP, RAR, or split .7z.001 full archive\r\n"+(route==1?(bundle!=null?"✓ Original files found: "+Path.GetFileName(bundle.Path):"• Original archive: "+ArchiveHint(recipe)):"• Select its game folder; wrapper folders are detected automatically")+"\r\n✓ Detection checks the game structure and version files";
            installNote.Text=recipe.Id=="gorpg-1-23"?"Keep both .001 and .002 together. The launcher can extract and register the game.":"Full standalone games can be installed from original archives without CH or a delta pack.";
        }
        installButton.Text=route==0&&Installed(recipe.Id)!=null?"Use installed version":route==0&&found?"Install from detected pack":route==0?"Choose pack & install":route==1&&bundle!=null?"Use detected original files":route==1?"Choose original files":"Choose installed game folder";
        var incomplete=discovered.FirstOrDefault(x=>x.RecipeId==recipe.Id&&!string.IsNullOrEmpty(x.Problem));if(route==1&&incomplete!=null)installNote.Text=incomplete.Problem+" · keep all parts together. View found files shows their folder.";
        installLocation.Text="New installations: "+settings.InstallRoot;UpdateActions();installDiagram.Invalidate();
    }
    void UpdateActions(){
        if(playButton==null)return;bool allowed=!busy&&!running&&!discovering;playButton.Enabled=!busy&&!running&&Selected!=null;
        gameFolderButton.Enabled=Selected!=null&&Directory.Exists(Selected.Root);saveFolderButton.Enabled=Selected!=null;
        if(copyButton!=null){copyButton.Enabled=allowed&&Selected!=null&&LibraryProfiles.Any(x=>x.Id!=Selected.Id&&UpgradeSupport.CanTransfer(UniversalSupport.Identify(x.Root),UniversalSupport.Identify(Selected.Root)));backupButton.Enabled=allowed&&Selected!=null;restoreButton.Enabled=allowed&&Selected!=null;}
        if(controlsButton!=null)controlsButton.Enabled=Selected!=null&&!busy;
        if(controlsToggle!=null)controlsToggle.Enabled=!busy&&Selected!=null;
        if(installButton!=null)installButton.Enabled=!busy&&!running&&Target!=null;
        if(englishEnable!=null){bool supported=englishCurrent!=null&&englishCurrent.Supported&&englishCurrent.Pack!=null&&!englishCurrent.Modified;englishEnable.Enabled=allowed&&supported;englishRestore.Enabled=allowed&&englishCurrent!=null&&(englishCurrent.Enabled||englishCurrent.Modified);englishImport.Enabled=allowed&&supported;englishExport.Enabled=!busy&&supported;}
        if(running&&!busy)status.Text="Game running · save and close it before switching versions, installing, or changing saves.";
        else if(!busy&&(status.Text=="Ready"||status.Text.StartsWith("Game running")))status.Text="Ready · choose a version to play, or open Install for the files you need.";
    }
    bool GameClosed(){running=Store.Running(settings);if(!running)return true;UpdateActions();MessageBox.Show(this,"Save and close the active game first.","Game is running");return false;}
    void Guard(Action action){try{action();}catch(Exception e){MessageBox.Show(this,e.Message,"Celesphonia Launcher",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    void Launch(){Launch(controlsToggle!=null&&controlsToggle.Checked);}
    async void Launch(bool controls){
        if(busy||Selected==null||!GameClosed())return;
        if(!ProfileLibrary.IsPlayable(Selected)){
            string missing=Selected.Name;
            RefreshProfiles();UpdateActions();
            status.Text="Unavailable installation excluded · "+missing;
            MessageBox.Show(this,"This game's folder was moved, deleted or is incomplete. Its library entry is now hidden until the game is available again. No game files, saves or backups were deleted.",
                "Game installation unavailable",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            return;
        }
        if(scanCancellation!=null)scanCancellation.Cancel();
        var profile=Selected;Store.Write(settings);busy=true;SetEnabled(false);progress.Visible=true;
        var updates=new Progress<string>(message=>{if(!IsDisposed&&busy)status.Text=message;});
        var clock=Stopwatch.StartNew();
        try{
            var process=await Task.Run(()=>controls?RuntimeSupport.Launch(profile,message=>((IProgress<string>)updates).Report(message)):Store.Launch(profile,message=>((IProgress<string>)updates).Report(message)));
            running=true;status.Text=(controls?"Runtime controls started · F8 · ":"Game started · ")+profile.Name;
            File.WriteAllText(Path.Combine(Store.Home,"launch-status.json"),Store.Json.Serialize(new{status="STARTED",profile=profile.Name,root=profile.Root,exe=profile.Exe,pid=process.Id,seconds=clock.Elapsed.TotalSeconds}),new System.Text.UTF8Encoding(false));
            Store.Write(settings);
        }catch(Exception e){
            status.Text="Launch stopped: "+e.Message;
            File.WriteAllText(Path.Combine(Store.Home,"launch-status.json"),Store.Json.Serialize(new{status="FAILED",profile=profile.Name,root=profile.Root,exe=profile.Exe,error=e.ToString(),seconds=clock.Elapsed.TotalSeconds}),new System.Text.UTF8Encoding(false));
            MessageBox.Show(this,e.Message,"Launch",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }finally{busy=false;progress.Visible=false;SetEnabled(true);UpdateActions();}
    }
    void OpenSaves(){Directory.CreateDirectory(Selected.SavePath);Process.Start("explorer.exe",Store.Q(Selected.SavePath));}
    string Folder(string title,string initial){using(var d=new FolderBrowserDialog{Description=title,SelectedPath=initial,ShowNewFolderButton=false})return d.ShowDialog(this)==DialogResult.OK?d.SelectedPath:null;}
    string PromptName(string initial){using(var dialog=new Form{Text="Name this version",ClientSize=new Size(475,125),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var text=new TextBox{Location=new Point(16,16),Size=new Size(443,30),Text=initial};dialog.Controls.Add(text);var ok=new Button{Text="Add version",Location=new Point(313,71),Size=new Size(146,34),DialogResult=DialogResult.OK};dialog.Controls.Add(ok);dialog.AcceptButton=ok;return dialog.ShowDialog(this)==DialogResult.OK?text.Text.Trim():null;}}
    void AddProfile(){string root=Folder("Select a game folder or the folder containing its extracted game.",settings.InstallRoot);if(root==null)return;root=UniversalSupport.FindGameRoot(root);Register(root,Path.GetFileName(root));}
    void Register(string root,string suggested){
        var existing=LibraryProfiles.FirstOrDefault(x=>Path.GetFullPath(x.Root).Equals(Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase));if(existing!=null){settings.SelectedId=existing.Id;RefreshProfiles();ShowPage("Play");status.Text="Selected existing installation · "+existing.Name;return;}
        string detected=UniversalSupport.DetectedName(root);string name=PromptName(detected??suggested);if(string.IsNullOrEmpty(name))return;var p=Store.Detect(root,name);if(detected!=null)p.Note="Detected "+detected+"; existing game files and saves";Added(p);
    }
    void Added(Profile p){if(FileDiscovery.IsNativeProbe(p.Root))throw new IOException("This is an isolated verification copy. Choose your installed game folder.");var existing=LibraryProfiles.FirstOrDefault(x=>ProfileLibrary.Identity(x).Equals(ProfileLibrary.Identity(p),StringComparison.OrdinalIgnoreCase));if(existing!=null)p=existing;else settings.Profiles.Add(p);settings.SelectedId=p.Id;Store.Write(settings);RefreshProfiles();status.Text=p.Name+" selected · its own saves are ready.";UpdateInstall();ShowPage("Play");}
    void ChangeLocation(){if(busy)return;string path=Folder("Choose where NEW independent game profiles will be installed. Existing profiles stay where they are.",settings.InstallRoot);if(path==null)return;settings.InstallRoot=path;Store.Write(settings);UpdateInstall();}
    string PickArchive(string title,string filter,string initial,string fileName){using(var d=new OpenFileDialog{Title=title,Filter=filter,InitialDirectory=initial,FileName=fileName??""})return d.ShowDialog(this)==DialogResult.OK?d.FileName:null;}
    string PickBundle(ModRecipe recipe){
        using(var dialog=new Form{Text="Choose the complete mod bundle",ClientSize=new Size(595,182),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"Choose an archive, or a folder already extracted from it.",18,16,565,35,11,false,Color.Black);
            var archive=new Button{Text="Choose archive",Location=new Point(18,79),Size=new Size(265,43),DialogResult=DialogResult.Yes};var folder=new Button{Text="Choose extracted folder",Location=new Point(304,79),Size=new Size(272,43),DialogResult=DialogResult.No};dialog.Controls.Add(archive);dialog.Controls.Add(folder);
            var choice=dialog.ShowDialog(this);if(choice==DialogResult.Yes)return PickArchive("Select ONE complete bundle; choose .001 for split archives","Mod archives|*.zip;*.rar;*r删ar;*.7z;*.001;*.7删z|All files|*.*",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","Telegram Desktop"),recipe.ArchiveName);
            if(choice==DialogResult.No)return Folder("Select the bundle's extracted folder containing data, js, img, and audio.",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"));return null;
        }
    }
    async void GuidedInstall(){
        if(busy||Target==null||!GameClosed())return;
        var recipe=Target;int route=methods.SelectedIndex;string location=settings.InstallRoot;
        busy=true;SetEnabled(false);installButton.Enabled=false;
        try{
            if(discovering){
                status.Text="Stopping file search to prepare installation…";
                if(scanCancellation!=null)scanCancellation.Cancel();
                while(discovering&&!IsDisposed)await Task.Delay(25);
                if(IsDisposed)return;
            }
            if(UpgradeSupport.IsUpgrade(recipe.Id)&&route!=2){var already=Installed(recipe.Id);if(already!=null){settings.SelectedId=already.Id;RefreshProfiles();ShowPage("Play");return;}var source=UpgradeSupport.StartingProfile(settings,recipe.Id);if(source==null){status.Text="Add a compatible source installation first.";return;}var detected=Original(recipe.Id);string update=detected==null?PickBundle(recipe):detected.Path;if(update==null)return;await RunInstall(()=>UpgradeSupport.Install(source,update,location,recipe.Id,true));return;}
            if(route==0){
                var already=Installed(recipe.Id);if(already!=null){settings.SelectedId=already.Id;RefreshProfiles();ShowPage("Play");return;}
                var detected=AnyPack(recipe.Id)??Found(recipe.Id,"Delta pack");string pack=detected==null?(File.Exists(DefaultPack(recipe))?DefaultPack(recipe):PickArchive("Choose an any-version pack or legacy delta pack","Celesphonia packs|*.cgdelta.zip;*.zip|All files|*.*",Path.Combine(Store.Home,"Delta Packs"),UniversalSupport.PackName)):detected.Path;if(pack==null)return;
                if(UniversalSupport.IsPack(pack)){var source=SupportedSource(recipe.Id);if(source==null){ChooseStartingVersion();return;}await RunInstall(()=>UniversalSupport.Install(pack,source.Root,location,recipe.Id));return;}
                string basis=CH()==null?Folder("This older pack is CH-only. Select CH, or choose the new any-version pack instead.",settings.InstallRoot):CH().Root;if(basis==null)return;
                using(var zip=System.IO.Compression.ZipFile.OpenRead(pack)){var entry=zip.GetEntry("manifest.json");if(entry==null)throw new IOException("This file is not a launcher delta pack.");using(var reader=new StreamReader(entry.Open())){var info=Store.Json.Deserialize<DeltaManifest>(reader.ReadToEnd());if(!info.Name.Equals(recipe.Name,StringComparison.OrdinalIgnoreCase) && MessageBox.Show(this,"This pack installs "+info.Name+", rather than the selected "+recipe.Name+". Install the pack's version?","Check selected version",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;}}
                await RunInstall(()=>DeltaSupport.Install(pack,basis,location));
            }else if(route==1 && !recipe.Standalone){
                var fullArchive=Found(recipe.Id,"Full game archive");if(fullArchive!=null){await RunInstall(()=>UniversalSupport.InstallOriginal(fullArchive.Path,location,recipe.Id));return;}
                var basis=Clean();if(basis==null){MessageBox.Show(this,"First register clean Gorpg 1.23, use its original archive, or reconstruct it from any supported starting version. A mod bundle alone does not contain the base game.");return;}
                var detected=Found(recipe.Id,"Extracted bundle")??Found(recipe.Id,"Original archive");string source=detected==null?PickBundle(recipe):detected.Path;if(source==null)return;
                if(detected!=null&&!string.IsNullOrEmpty(detected.Problem))throw new IOException(detected.Problem);
                await RunInstall(()=>{ModRecipe found=Directory.Exists(source)?RecipeSupport.Identify(RecipeSupport.ContentRoot(source)):RecipeSupport.IdentifyArchive(source);if(found!=null&&found.Id!=recipe.Id)throw new IOException("The chosen files are "+found.Name+". Choose the matching version.");return RecipeSupport.Install(basis.Root,location,recipe.Name,new List<LayerInput>{new LayerInput{Source=source}});});
            }else if(route==1){var original=Original(recipe.Id);string file=original==null?PickBundle(recipe):original.Path;if(file==null)return;if(Directory.Exists(file))Register(UniversalSupport.FindGameRoot(file),recipe.Name);else await RunInstall(()=>UniversalSupport.InstallOriginal(file,location,recipe.Id));}
            else{string root=Folder("Choose any EXTRACTED complete game folder. Version files will identify it.",settings.InstallRoot);if(root!=null)Register(UniversalSupport.FindGameRoot(root),recipe.Name);}
        }catch(Exception e){MessageBox.Show(this,e.Message,"Install version",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{busy=false;if(!IsDisposed){SetEnabled(true);UpdateActions();}}
    }
    async Task RunInstall(Func<Profile> job){
        busy=true;SetEnabled(false);progress.Visible=true;status.Text="Installing a complete private copy and verifying its files. Please keep this window open.";UseWaitCursor=true;
        try{var p=await Task.Run(job);Added(p);MessageBox.Show(this,"Installed "+p.Name+". It has its own save folder; all previous versions remain in your library.","Version ready");}
        catch(Exception e){status.Text="Installation did not finish. Existing games and saves were preserved.";MessageBox.Show(this,e.Message,"Installation",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{UseWaitCursor=false;progress.Visible=false;busy=false;SetEnabled(true);UpdateActions();}
    }
    void SetEnabled(bool enabled){foreach(Control control in Controls)if(control!=status)control.Enabled=enabled;}
    async void OrderedLayers(){
        if(busy||!GameClosed())return;var clean=Clean();if(clean==null){MessageBox.Show(this,"Add or reconstruct clean Gorpg 1.23 first. An existing patched branch cannot serve as the clean base.");return;}
        using(var dialog=new Form{Text="Bundle first, hotfixes afterward",ClientSize=new Size(790,450),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"Start with ONE complete bundle. Later hotfix files overwrite earlier files.",18,15,752,32,11,true,Color.Black);
            LabelAt(dialog,"Base: "+clean.Name+"  ·  Saves start empty in a new private game folder.",18,54,750,30,10,false,Color.DimGray);
            var list=new ListBox{Location=new Point(18,97),Size=new Size(752,184),HorizontalScrollbar=true};dialog.Controls.Add(list);
            Action<bool> pick=delegate(bool archive){if(archive){using(var d=new OpenFileDialog{Title="Choose bundle first; add separate hotfixes afterward",Multiselect=true,Filter="Mod archives|*.rar;*r删ar;*.zip;*.7z;*.001;*.7删z|All files|*.*"})if(d.ShowDialog(dialog)==DialogResult.OK)foreach(string file in d.FileNames)list.Items.Add(new LayerInput{Source=file});}else{using(var d=new FolderBrowserDialog{Description="Choose an extracted content folder"})if(d.ShowDialog(dialog)==DialogResult.OK)list.Items.Add(new LayerInput{Source=d.SelectedPath});}};
            ButtonAt(dialog,"Add archive",18,295,144,37,()=>pick(true),false);ButtonAt(dialog,"Add folder",173,295,138,37,()=>pick(false),false);
            ButtonAt(dialog,"Move up",326,295,131,37,()=>{int i=list.SelectedIndex;if(i>0){object item=list.Items[i];list.Items.RemoveAt(i);list.Items.Insert(i-1,item);list.SelectedIndex=i-1;}},false);
            ButtonAt(dialog,"Move down",468,295,143,37,()=>{int i=list.SelectedIndex;if(i>=0&&i<list.Items.Count-1){object item=list.Items[i];list.Items.RemoveAt(i);list.Items.Insert(i+1,item);list.SelectedIndex=i+1;}},false);
            ButtonAt(dialog,"Remove",623,295,147,37,()=>{if(list.SelectedIndex>=0)list.Items.RemoveAt(list.SelectedIndex);},false);
            LabelAt(dialog,"3.3.5 already includes Abu 56.6 + story 1.0 + skills 3.3.5.\r\nDo not stack Maple, 3.3.4, or a separate complete Abu bundle.",18,349,750,48,10,false,Color.DimGray);
            var ok=new Button{Text="Install reviewed order",Location=new Point(517,402),Size=new Size(253,34),DialogResult=DialogResult.OK};dialog.Controls.Add(ok);dialog.AcceptButton=ok;
            if(dialog.ShowDialog(this)!=DialogResult.OK || list.Items.Count==0)return;var layers=list.Items.Cast<LayerInput>().ToList();string location=settings.InstallRoot;await RunInstall(()=>RecipeSupport.Install(clean.Root,location,"Custom layered version",layers));
        }
    }
    Profile ChooseProfile(string title,IEnumerable<Profile> options){using(var dialog=new Form{Text=title,ClientSize=new Size(595,145),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var box=new ComboBox{Location=new Point(18,24),Size=new Size(559,31),DropDownStyle=ComboBoxStyle.DropDownList};box.Items.AddRange(options.ToArray());if(box.Items.Count>0)box.SelectedIndex=0;dialog.Controls.Add(box);var ok=new Button{Text="Select",Location=new Point(429,87),Size=new Size(148,35),DialogResult=DialogResult.OK};dialog.Controls.Add(ok);dialog.AcceptButton=ok;return dialog.ShowDialog(this)==DialogResult.OK?box.SelectedItem as Profile:null;}}
    void CopySaves(){if(Selected==null||!GameClosed())return;var source=ChooseProfile("Copy compatible saves from which version?",LibraryProfiles.Where(x=>x.Id!=Selected.Id&&UpgradeSupport.CanTransfer(UniversalSupport.Identify(x.Root),UniversalSupport.Identify(Selected.Root))));if(source==null)return;
        if(MessageBox.Show(this,"Copy "+source.Name+" saves into "+Selected.Name+"?\r\n\r\n"+RecipeSupport.SaveWarning(source,Selected),"Review save transfer",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        Store.ReplaceSaves(source.SavePath,Selected);UpdateProfile();status.Text="Saves copied. Source saves and the destination's previous backup are preserved.";
    }
    void Restore(){if(!GameClosed())return;string parent=Path.Combine(Store.Home,"Save Backups",Selected.Id);Directory.CreateDirectory(parent);string folder=Folder("Choose the timestamped backup folder containing manifest.json.",parent);if(folder==null)return;Store.VerifyBackup(folder);
        if(MessageBox.Show(this,"Restore this save backup to "+Selected.Name+"? Your current save set will be backed up first.","Restore saves",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;Store.ReplaceSaves(folder,Selected,true);UpdateProfile();status.Text="Backup restored; the previous save set was backed up first.";
    }
    public void PreviewPage(string page){discovered=FileDiscovery.Scan(settings);discoveryStatus.Text="Found "+discovered.Count+" sources · launcher folders, Documents, Desktop, Downloads + browser folders";foreach(var p in LibraryProfiles)statusCache[p.Id]=EnglishSupport.Status(p);if(page=="English")profiles.SelectedItem=Installed(UpgradeSupport.Latest)??LibraryProfiles.FirstOrDefault(x=>x.Name.Contains("3.3.5"))??Selected;if(Selected!=null)englishCurrent=statusCache[Selected.Id];ShowPage(page);}
    public void OpenEnglishPage(){ShowPage("English");}
    public void TestPage(string page){ShowPage(page);}
    void BuildFiles(){
        filesPage=Page();LabelAt(filesPage,"FILES & VERSIONS",0,0,760,24,10,true,muted);LabelAt(filesPage,"What you already have",0,32,800,46,24,true);
        LabelAt(filesPage,"Green rows are ready to switch. Other rows show files that can prepare a missing version.",0,89,800,44,10,false,muted);
        availability=new DataGridView{Location=new Point(0,149),Size=new Size(812,350),ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=card,BorderStyle=BorderStyle.None,EnableHeadersVisualStyles=false,RowTemplate={Height=48},AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None};
        availability.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(41,47,64);availability.ColumnHeadersDefaultCellStyle.ForeColor=ForeColor;availability.ColumnHeadersHeight=43;
        availability.DefaultCellStyle.BackColor=card;availability.DefaultCellStyle.ForeColor=ForeColor;availability.DefaultCellStyle.SelectionBackColor=Color.FromArgb(68,58,103);availability.DefaultCellStyle.WrapMode=DataGridViewTriState.True;availability.GridColor=Color.FromArgb(48,56,73);
        availability.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;availability.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;availability.DefaultCellStyle.Padding=new Padding(7,0,4,0);availability.DefaultCellStyle.SelectionForeColor=ForeColor;availability.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(28,34,47);
        availability.Columns.Add("Version","Version");availability.Columns[0].Width=183;availability.Columns.Add("Game","Game folder");availability.Columns[1].Width=110;availability.Columns.Add("Original","Original files");availability.Columns[2].Width=116;availability.Columns.Add("Pack","Patch pack");availability.Columns[3].Width=113;availability.Columns.Add("English","English text");availability.Columns[4].Width=143;availability.Columns.Add("Ready","Action");availability.Columns[5].Width=143;
        availability.SelectionChanged+=delegate{Guard(()=>{if(availabilityNote==null||availability.SelectedRows.Count==0)return;string id=Convert.ToString(availability.SelectedRows[0].Tag);if(string.IsNullOrEmpty(id))return;var p=Installed(id);var original=Original(id);availabilityNote.Text=p!=null?"Installed: "+p.Root:original!=null?"Original files: "+original.Path:"Select this version to see exactly which files are needed.";});};
        availability.CellDoubleClick+=delegate{UseAvailable();};filesPage.Controls.Add(availability);
        availability.CellMouseDown+=delegate(object sender,DataGridViewCellMouseEventArgs e){if(e.Button!=MouseButtons.Right||e.RowIndex<0)return;availability.ClearSelection();availability.Rows[e.RowIndex].Selected=true;string id=Convert.ToString(availability.Rows[e.RowIndex].Tag);var game=Installed(id);if(game!=null){profiles.SelectedItem=game;ShowFolders(availability,availability.PointToClient(Cursor.Position));}else{var source=Original(id)??AnyPack(id);if(source!=null)Guard(()=>LauncherFolders.Open(source.Path));}};
        availabilityNote=LabelAt(filesPage,"",0,511,812,45,10,false,muted);
        ButtonAt(filesPage,"Use selected version",0,572,277,45,()=>UseAvailable(),true);ButtonAt(filesPage,"View detected files",295,572,252,45,()=>ShowFoundFiles(),false);ButtonAt(filesPage,"Search another folder",565,572,247,45,()=>AddSearchFolder(),false);
    }
    void UpdateAvailability(){
        if(availability==null||targets==null)return;string prior=availability.SelectedRows.Count>0?Convert.ToString(availability.SelectedRows[0].Tag):null;availability.Rows.Clear();foreach(var recipe in targets.Items.Cast<ModRecipe>()){
            var game=Installed(recipe.Id);var original=Original(recipe.Id);var pack=AnyPack(recipe.Id)??Found(recipe.Id,"Delta pack");
            bool ready=game!=null,canInstall=UpgradeSupport.IsUpgrade(recipe.Id)?original!=null&&Installed(UpgradeSupport.Trial)!=null:pack!=null&&(pack.Kind=="Any-version pack"?SupportedSource(recipe.Id)!=null:CH()!=null)||original!=null&&(recipe.Standalone||original.Kind=="Full game archive"||Clean()!=null);
            EnglishStatus cached;string textStatus=game!=null?(statusCache.TryGetValue(game.Id,out cached)?cached.Short:"Select to check English"):recipe.Id=="ch-current"?"English reference":"Install game first";
            int row=availability.Rows.Add(recipe.Name,ready?"Installed · "+CountSaves(game)+" saves":"Not installed",original==null?"Not found":original.Kind=="Full game archive"?"Version files match":original.Kind=="Extracted bundle"?"Folder found":"Archive found",UpgradeSupport.IsUpgrade(recipe.Id)?"Forward upgrade":pack==null?"Not found":pack.Kind=="Any-version pack"?"Any-version":"Legacy CH-only",textStatus,ready?"Ready to switch":canInstall?"Ready to install":"Needs files");availability.Rows[row].Tag=recipe.Id;
            availability.Rows[row].DefaultCellStyle.ForeColor=settings.LauncherTheme=="Daylight"?(ready?Color.FromArgb(21,112,76):canInstall?Color.FromArgb(105,62,154):Color.FromArgb(130,87,28)):ready?Color.FromArgb(141,220,188):canInstall?Color.FromArgb(208,194,249):Color.FromArgb(229,192,136);
        }
        var selectedRow=availability.Rows.Cast<DataGridViewRow>().FirstOrDefault(x=>Convert.ToString(x.Tag)==prior)??availability.Rows.Cast<DataGridViewRow>().FirstOrDefault();if(selectedRow!=null){availability.ClearSelection();selectedRow.Selected=true;}
    }
    void UseAvailable(){Guard(()=>{if(availability.SelectedRows.Count==0)return;string id=Convert.ToString(availability.SelectedRows[0].Tag);var game=Installed(id);if(game!=null){settings.SelectedId=game.Id;RefreshProfiles();ShowPage("Play");return;}targets.SelectedItem=targets.Items.Cast<ModRecipe>().First(x=>x.Id==id);methods.SelectedIndex=AnyPack(id)!=null?0:Original(id)!=null?1:0;ShowPage("Install");});}
    async void AddSearchFolder(){string folder=Folder("Choose another folder to search for games, archives, and patches.",settings.InstallRoot);if(folder==null)return;if(settings.SearchRoots==null)settings.SearchRoots=new List<string>();if(!settings.SearchRoots.Contains(folder))settings.SearchRoots.Add(folder);Store.Write(settings);await Discover();UpdateAvailability();}
    async void ChooseStartingVersion(){
        if(busy||!GameClosed())return;
        try{using(var dialog=new Form{Text="Choose your starting installation",ClientSize=new Size(645,214),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"CH, Gorpg 1.23, Maple 1.7.2, 3.3.4, or 3.3.5 can be the starting game.",18,17,610,46,10,false,Color.Black);
            var list=new ComboBox{Location=new Point(18,72),Size=new Size(610,32),DropDownStyle=ComboBoxStyle.DropDownList,ForeColor=Color.Black};list.Items.AddRange(LibraryProfiles.Where(x=>UniversalSupport.Identify(x.Root)!=null).ToArray());if(list.Items.Count>0)list.SelectedIndex=0;dialog.Controls.Add(list);
            var use=new Button{Text="Use selected",Location=new Point(18,137),Size=new Size(188,40),DialogResult=DialogResult.OK};var folder=new Button{Text="Choose game folder",Location=new Point(223,137),Size=new Size(198,40),DialogResult=DialogResult.Yes};var archive=new Button{Text="Choose full archive",Location=new Point(438,137),Size=new Size(190,40),DialogResult=DialogResult.No};dialog.Controls.Add(use);dialog.Controls.Add(folder);dialog.Controls.Add(archive);
            var result=dialog.ShowDialog(this);if(result==DialogResult.OK){startingSource=list.SelectedItem as Profile;UpdateInstall();return;}
            if(result==DialogResult.Yes){string root=Folder("Choose an intact complete game folder or its wrapper folder.",settings.InstallRoot);if(root==null)return;root=UniversalSupport.FindGameRoot(root);if(UniversalSupport.Identify(root)==null)throw new IOException("This folder does not match a supported reference version.");startingSource=Store.Detect(root,UniversalSupport.DetectedName(root));if(!LibraryProfiles.Any(x=>x.Root.Equals(root,StringComparison.OrdinalIgnoreCase)))Added(startingSource);ShowPage("Install");UpdateInstall();}
            else if(result==DialogResult.No){string file=PickArchive("Choose any supported FULL game archive","Game archives|*.zip;*.rar;*.7z;*.001;*.7删z|All files|*.*",settings.InstallRoot,null);if(file==null)return;busy=true;SetEnabled(false);progress.Visible=true;try{startingSource=await Task.Run(()=>UniversalSupport.InstallOriginal(file,settings.InstallRoot,null));Added(startingSource);ShowPage("Install");UpdateInstall();}finally{busy=false;progress.Visible=false;SetEnabled(true);UpdateActions();}}
        }}catch(Exception e){MessageBox.Show(this,e.Message,"Starting version",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    void ShowFoundFiles(){
        using(var dialog=new Form{Text="Files found nearby",ClientSize=new Size(970,528),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"Launcher folders, Documents, Desktop, native Downloads, and configured browser download folders.",18,15,932,31,10,false,Color.DimGray);
            var grid=new DataGridView{Location=new Point(18,56),Size=new Size(934,302),ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false,BackgroundColor=Color.White,ForeColor=Color.Black};
            grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Version",HeaderText="Version / bundle",Width=280});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Kind",HeaderText="File type",Width=166});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Status",HeaderText="Detection / status",Width=345});
            grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Size",HeaderText="Size",Width=110});
            foreach(var source in discovered){int i=grid.Rows.Add(source.Name,source.Kind,string.IsNullOrEmpty(source.Problem)?source.Detection??"Native folder / content match":source.Problem,source.Bytes>0?(source.Bytes/1048576.0).ToString("N1")+" MB":"Folder");grid.Rows[i].Tag=source;}
            dialog.Controls.Add(grid);var path=new TextBox{Location=new Point(18,378),Size=new Size(934,63),ReadOnly=true,Multiline=true};dialog.Controls.Add(path);
            grid.SelectionChanged+=delegate{if(grid.SelectedRows.Count>0){var selected=(FoundSource)grid.SelectedRows[0].Tag;path.Text=selected.Path+(selected.Bytes>0?"\r\n"+selected.Bytes.ToString("N0")+" bytes":"");}};
            var use=new Button{Text="Use selected source",Location=new Point(688,465),Size=new Size(264,40)};dialog.Controls.Add(use);
            ButtonAt(dialog,"Open containing folder",18,465,230,40,()=>Guard(()=>{if(grid.SelectedRows.Count>0)LauncherFolders.Open(((FoundSource)grid.SelectedRows[0].Tag).Path);}),false);
            use.Click+=delegate{
                if(grid.SelectedRows.Count==0)return;var source=(FoundSource)grid.SelectedRows[0].Tag;if(!string.IsNullOrEmpty(source.Problem)){MessageBox.Show(dialog,source.Problem+". Put all required volumes in the same folder first.");return;}
                if(source.Kind=="Installed game"){var p=LibraryProfiles.FirstOrDefault(x=>x.Root.Equals(source.Path,StringComparison.OrdinalIgnoreCase));if(p==null){p=Store.Detect(source.Path,source.Name);Added(p);}else{settings.SelectedId=p.Id;RefreshProfiles();ShowPage("Play");}dialog.Close();return;}
                var recipe=targets.Items.Cast<ModRecipe>().FirstOrDefault(x=>x.Id==source.RecipeId);if(recipe==null){MessageBox.Show(dialog,"This source is not a cataloged bundle. Use Bundle + hotfixes with its author's installation instructions.");return;}
                preferredSources[source.RecipeId+"/"+source.Kind]=source;targets.SelectedItem=recipe;methods.SelectedIndex=source.Kind=="Delta pack"||source.Kind=="Any-version pack"?0:1;UpdateInstall();ShowPage("Install");dialog.Close();
            };
            dialog.ShowDialog(this);
        }
    }
    async void ScanAgain(){if(discovering){if(scanCancellation!=null)scanCancellation.Cancel();return;}await Discover();}
    async Task Discover(){
        if(busy||discovering)return;discovering=true;scanCancellation=new System.Threading.CancellationTokenSource();var cancellation=scanCancellation;
        UpdateActions();discoveryButton.Enabled=true;discoveryButton.Text="Stop search";discoveryStatus.Text="Searching local game folders and archives…";
        bool limitReached=false;
        var updates=new Progress<DiscoveryProgress>(p=>{if(IsDisposed||!discovering)return;limitReached=p.LimitReached;
            if(p.Sources!=null&&p.Sources.Any(x=>!discovered.Any(y=>y.Path==x.Path&&y.Kind==x.Kind&&y.RecipeId==x.RecipeId))){discovered=p.Sources.Concat(discovered).GroupBy(x=>x.Path+"|"+x.Kind+"|"+x.RecipeId,StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).ToList();if(!busy){UpdateInstall();if(currentPage=="Files")UpdateAvailability();}}
            discoveryStatus.Text=p.FoldersVisited+" folders checked · "+p.SourcesFound+" sources found"+(p.LimitReached?" · search limit reached":"")+" · "+(p.CurrentFolder??"complete");});
        try{
            var snapshot=new Settings{Profiles=LibraryProfiles.ToList(),InstallRoot=settings.InstallRoot,SearchRoots=settings.SearchRoots==null?null:settings.SearchRoots.ToList()};
            var result=await Task.Run(()=>{var sources=FileDiscovery.Scan(snapshot,cancellation.Token,p=>((IProgress<DiscoveryProgress>)updates).Report(p));var additions=sources.Where(x=>x.Kind=="Installed game"&&!snapshot.Profiles.Any(p=>Path.GetFullPath(p.Root).Equals(x.Path,StringComparison.OrdinalIgnoreCase))).Select(x=>Store.Detect(x.Path,x.Name)).ToList();return Tuple.Create(sources,additions);});
            if(IsDisposed)return;cancellation.Token.ThrowIfCancellationRequested();discovered=result.Item1;int added=0;
            foreach(var p in result.Item2){if(LibraryProfiles.Any(x=>Path.GetFullPath(x.Root).Equals(p.Root,StringComparison.OrdinalIgnoreCase)))continue;p.Note="Automatically found installation; existing files and saves";settings.Profiles.Add(p);added++;}
            if(added>0){Store.Write(settings);RefreshProfiles();}
            int incomplete=discovered.Count(x=>!string.IsNullOrEmpty(x.Problem));
            discoveryStatus.Text="Found "+discovered.Count+" sources"+(added>0?" · "+added+" games added":"")+(incomplete>0?" · "+incomplete+" incomplete archives":"")+(limitReached?" · search limit reached; choose a specific folder":"")+" · content verified before installation";
            UpdateInstall();if(currentPage=="Files")UpdateAvailability();
        }catch(OperationCanceledException){if(!IsDisposed)discoveryStatus.Text="Search stopped · results already found retained";}
        catch(Exception e){if(!IsDisposed)discoveryStatus.Text="Discovery could not finish: "+e.Message;}
        finally{discovering=false;scanCancellation=null;cancellation.Dispose();if(!IsDisposed){discoveryButton.Text="Find files";discoveryButton.Enabled=true;UpdateActions();}}
    }
    void ShowFolders(Control anchor,Point? location=null){
        var menu=new ContextMenuStrip{BackColor=card,ForeColor=ForeColor,Font=Font,Renderer=new LauncherMenuRenderer(),ShowImageMargin=false,ShowItemToolTips=true};
        var heading=new ToolStripMenuItem(Selected==null?"Choose a version":Selected.Name.Replace(" - current installation","")){Enabled=false};menu.Items.Add(heading);
        var folders=LauncherFolders.Targets(Selected,settings.InstallRoot,englishCurrent==null?null:englishCurrent.VersionId);
        string version=englishCurrent==null?null:englishCurrent.VersionId;
        if(version!=null){var original=Original(version);var pack=AnyPack(version)??Found(version,"Delta pack");if(original!=null)folders.Add(new LauncherFolder{Label="Detected original files",Path=Directory.Exists(original.Path)?original.Path:Path.GetDirectoryName(original.Path)});if(pack!=null)folders.Add(new LauncherFolder{Label="Detected patch pack",Path=Path.GetDirectoryName(pack.Path)});}
        foreach(var folder in folders){
            if(folder.Label=="Launcher folder")menu.Items.Add(new ToolStripSeparator());
            var item=new ToolStripMenuItem(folder.Label){Enabled=folder.Exists,ToolTipText=folder.Path+(folder.Exists?"":"\r\nNot created for this version yet"),ForeColor=ForeColor,BackColor=card};
            string path=folder.Path;item.Click+=delegate{Guard(()=>LauncherFolders.Open(path));};menu.Items.Add(item);
        }
        LauncherMenuLifetime.RetireAfterClose(menu);menu.Show(anchor,location??new Point(0,anchor.Height));
    }
}
