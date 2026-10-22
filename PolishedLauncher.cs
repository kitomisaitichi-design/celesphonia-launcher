using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class ChoicePicker : SleekButton {
    public List<object> Items=new List<object>();int selected=-1;public event EventHandler SelectedIndexChanged;
    public int SelectedIndex{get{return selected;}set{int next=value>=0&&value<Items.Count?value:-1;if(next==selected)return;selected=next;Text=next>=0?Items[next].ToString()+"   ▾":"Choose...";if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
    public object SelectedItem{get{return selected>=0&&selected<Items.Count?Items[selected]:null;}set{SelectedIndex=Items.IndexOf(value);}}
    public ChoicePicker(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=1;FlatAppearance.BorderColor=Color.FromArgb(80,88,111);TextAlign=ContentAlignment.MiddleLeft;Padding=new Padding(8,0,8,0);Cursor=Cursors.Hand;
        Click+=delegate{var menu=new ContextMenuStrip{BackColor=BackColor,ForeColor=ForeColor,Font=Font,Renderer=new LauncherMenuRenderer()};for(int i=0;i<Items.Count;i++){int choice=i;var item=new ToolStripMenuItem(Items[i].ToString()){Checked=i==SelectedIndex,ForeColor=ForeColor,BackColor=BackColor};item.Click+=delegate{SelectedIndex=choice;};menu.Items.Add(item);}LauncherMenuStyle.Apply(menu);LauncherMenuLifetime.RetireAfterClose(menu);menu.Show(this,new Point(0,Height));};
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
    Label playName,playInfo,saveCount,savePath,status,installNeed,installNote,installLocation,installNext,saveTitle,saveNote,saveFolder,saveGuide,filesGuide;
    Button controlsButton,playButton,installButton,copyButton,restoreButton,backupButton,openSaveButton,gameFolderButton,saveFolderButton,playManageButton,filesManageButton,installDetailsButton,installSourceButton,installFolderButton,installLayersButton,installStartingButton,installDetectedButton,installSearchButton;
    ChoicePicker targets,methods;bool installDetailsExpanded;
    PathDiagram playDiagram=new PathDiagram(),installDiagram=new PathDiagram();
    readonly List<Button> navigation=new List<Button>();
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
    bool busy,running,discovering;string currentPage="Play";
    List<FoundSource> discovered=new List<FoundSource>();Label discoveryStatus;Button discoveryButton;ProgressBar progress;
    Dictionary<string,FoundSource> preferredSources=new Dictionary<string,FoundSource>();
    readonly List<string> originalInputs=new List<string>();
    Label sourceInputNote;
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
        Label title=LabelAt(this,"Celesphonia",70,23,202,38,22,false);title.Font=new Font("Segoe UI",22,FontStyle.Bold);title.ForeColor=LauncherPaint.Paper;
        LabelAt(this,"GAME LIBRARY",72,63,194,22,9,false,muted);
        var folders=ButtonAt(this,"Folders  ▾",380,30,144,42,()=>{},false);folders.Click+=delegate{ShowFolders(folders);};folders.AccessibleName="Open native folders for the selected version";
        int nx=548;foreach(string name in new[]{"Play","Files","English","Install","Saves","Help"}){string page=name;var b=ButtonAt(this,name,nx,30,96,42,()=>ShowPage(page),false);((SleekButton)b).Navigation=true;navigation.Add(b);nx+=102;}
        sidebar=new SleekCard{Location=new Point(24,111),Size=new Size(296,630),BackColor=card};Controls.Add(sidebar);
        LabelAt(sidebar,"YOUR LIBRARY",20,20,256,25,9,true,muted);
        profiles.Location=new Point(12,58);profiles.Size=new Size(272,470);profiles.BorderStyle=BorderStyle.None;profiles.BackColor=card;profiles.ForeColor=ForeColor;profiles.DrawMode=DrawMode.OwnerDrawFixed;profiles.ItemHeight=58;profiles.IntegralHeight=false;profiles.DrawItem+=DrawProfile;sidebar.Controls.Add(profiles);
        profiles.SelectedIndexChanged+=delegate{Guard(()=>{
            var update=profiles.SelectedItem as ModRecipe;
            if(update!=null){UpdateProfile();targets.SelectedItem=update;methods.SelectedIndex=1;ShowPage("Install");status.Text=update.Name+" · available for installation";}
            else if(Selected!=null){settings.SelectedId=Selected.Id;UpdateProfile();}
        });};
        profiles.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Right)return;int index=profiles.IndexFromPoint(e.Location);if(index<0)return;profiles.SelectedIndex=index;var managed=profiles.Items[index] as Profile;if(managed!=null)ShowManageMenu(profiles,managed,e.Location);else ShowInstallMenu(profiles,profiles.Items[index] as ModRecipe,e.Location);};
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
    // Modals use the launcher's current theme, including native WinForms inputs.
    // Call only after populating each dialog so nested lists and buttons inherit it.
    void StyleDialog(Form dialog){
        dialog.BackColor=background;dialog.ForeColor=ForeColor;dialog.Font=Font;
        dialog.ShowIcon=false;dialog.ShowInTaskbar=false;
        bool light=settings.LauncherTheme=="Daylight";
        Color warning=light?Color.FromArgb(147,50,31):Color.FromArgb(255,180,137);
        Action<Control> visit=null;visit=control=>{
            var label=control as Label;
            if(label!=null){
                label.BackColor=Color.Transparent;
                if(label.ForeColor==Color.Black||label.ForeColor==Color.DimGray||
                   label.ForeColor==Color.DarkGray||label.ForeColor==SystemColors.ControlText)
                    label.ForeColor=label.Font.Bold?ForeColor:muted;
            }else if(control is SleekButton){
                var sleek=(SleekButton)control;
                sleek.BackColor=sleek.Primary?accent:card;sleek.ForeColor=ForeColor;
            }else if(control is Button){
                var button=(Button)control;
                button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;
                button.FlatAppearance.BorderColor=LauncherPaint.Line;
                button.FlatAppearance.BorderSize=1;button.BackColor=card;
                button.ForeColor=ForeColor;
            }else if(control is ListBox){
                var list=(ListBox)control;
                list.BackColor=card;list.ForeColor=ForeColor;
                list.BorderStyle=BorderStyle.FixedSingle;
            }else if(control is ComboBox){
                var combo=(ComboBox)control;
                combo.BackColor=card;combo.ForeColor=ForeColor;
                combo.FlatStyle=FlatStyle.Flat;
            }else if(control is TextBox){
                var input=(TextBox)control;
                input.BackColor=card;input.ForeColor=ForeColor;
                input.BorderStyle=BorderStyle.FixedSingle;
            }else if(control is DataGridView){
                var grid=(DataGridView)control;
                grid.BackgroundColor=card;grid.ForeColor=ForeColor;
                grid.BorderStyle=BorderStyle.None;grid.GridColor=LauncherPaint.Line;
                grid.EnableHeadersVisualStyles=false;
                grid.ColumnHeadersDefaultCellStyle.BackColor=card;
                grid.ColumnHeadersDefaultCellStyle.ForeColor=ForeColor;
                grid.DefaultCellStyle.BackColor=card;
                grid.DefaultCellStyle.ForeColor=ForeColor;
                grid.DefaultCellStyle.SelectionBackColor=accent;
                grid.DefaultCellStyle.SelectionForeColor=ForeColor;
                grid.AlternatingRowsDefaultCellStyle.BackColor=background;
                grid.RowHeadersDefaultCellStyle.BackColor=card;
                grid.RowHeadersDefaultCellStyle.ForeColor=ForeColor;
                foreach(DataGridViewRow row in grid.Rows)
                    if(row.DefaultCellStyle.ForeColor==Color.FromArgb(154,53,38))
                        row.DefaultCellStyle.ForeColor=warning;
            }
            foreach(Control child in control.Controls)visit(child);
        };
        foreach(Control child in dialog.Controls)visit(child);
    }
    void BindHint(Control control,Label label,string text){string normal=label.Text;control.MouseEnter+=delegate{label.Text=text;};control.MouseLeave+=delegate{label.Text=normal;};}
    void LayoutViewport(){
        if(status==null||sidebar==null)return;
        int height=Math.Max(330,ClientSize.Height-190);
        main.Height=height;sidebar.Height=height;profiles.Height=height-160;addGame.Top=height-78;sidebarNote.Top=height-30;
        status.Top=111+height+23;progress.Top=111+height+4;
        status.Width=Math.Max(400,Math.Min(1130,ClientSize.Width-52));progress.Width=status.Width;
        // Keep every navigation tab visible on 960–1100px displays. The
        // content area retains its own scrolling, but navigation never moves
        // beyond the right edge or requires horizontal page scrolling.
        bool compact=ClientSize.Width<1170;
        var folder=Controls.OfType<Button>().FirstOrDefault(b=>b.Text.StartsWith("Folders",StringComparison.Ordinal));
        if(folder!=null){folder.Left=compact?288:380;folder.Width=compact?106:144;}
        for(int i=0;i<navigation.Count;i++){
            var button=navigation[i];
            button.Left=compact?400+i*92:548+i*102;
            button.Width=compact?87:96;
        }
        FitPage();
    }
    Panel Page(){var p=new GrimoirePage{Dock=DockStyle.Fill,BackColor=background,Visible=false};main.Canvas.Controls.Add(p);return p;}
    void DrawProfile(object sender,DrawItemEventArgs e){
        if(e.Index<0)return;var p=profiles.Items[e.Index] as Profile;var update=profiles.Items[e.Index] as ModRecipe;bool selected=(e.State&DrawItemState.Selected)!=0;
        using(var b=new SolidBrush(card))e.Graphics.FillRectangle(b,e.Bounds);
        if(selected)LauncherPaint.Box(e.Graphics,new Rectangle(e.Bounds.X+3,e.Bounds.Y+3,e.Bounds.Width-6,e.Bounds.Height-6),ControlPaint.Light(card,.12f),Color.Empty,9);
        string name=(p==null?update.Name:p.Name).Replace(" - current installation","").Replace(" - base","");int slots=p==null?0:CountSaves(p);
        using(var font=new Font("Segoe UI",10,selected?FontStyle.Bold:FontStyle.Regular))TextRenderer.DrawText(e.Graphics,name,font,new Rectangle(e.Bounds.X+14,e.Bounds.Y+6,e.Bounds.Width-28,30),ForeColor,TextFormatFlags.WordBreak|TextFormatFlags.NoPadding);
        using(var font=new Font("Segoe UI",8.5f))TextRenderer.DrawText(e.Graphics,p==null?"Available · click to install":slots+" saves  ·  "+(p.Name.Contains("trial")?"Trial build":"Installed · right-click to manage"),font,new Rectangle(e.Bounds.X+14,e.Bounds.Y+37,e.Bounds.Width-28,18),muted,TextFormatFlags.NoPadding);
    }
    int CountSaves(Profile p){return Store.SaveFiles(p.SavePath).Count(x=>System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(x),"^file[0-9]+\\.rpgsave$",System.Text.RegularExpressions.RegexOptions.IgnoreCase));}
    void BuildPlay(){
        playPage=Page();LabelAt(playPage,"READY TO PLAY",0,0,600,22,10,true,muted);playName=LabelAt(playPage,"",0,31,806,49,25,true);
        playInfo=LabelAt(playPage,"",0,83,802,42,10,false,muted);
        var saves=new SleekCard{Location=new Point(0,351),Size=new Size(812,90),BackColor=card};playPage.Controls.Add(saves);
        saveCount=LabelAt(saves,"",18,14,754,26,13,true);savePath=LabelAt(saves,"",18,49,774,30,9,false,muted);
        var runtimeCard=new SleekCard{Location=new Point(0,132),Size=new Size(812,90),BackColor=card};playPage.Controls.Add(runtimeCard);
        controlsToggle=new RuntimeControlsToggle{Text="Enable runtime controls for this game",Location=new Point(12,8),Size=new Size(788,46),BackColor=Color.Transparent,ForeColor=ForeColor,Font=new Font("Segoe UI",12,FontStyle.Bold),AccessibleName="Enable runtime controls before launching",AccessibleDescription="Click anywhere on this row or press Space to choose runtime controls for the next launch."};runtimeCard.Controls.Add(controlsToggle);
        controlsToggle.CheckedChanged+=delegate{if(settingControls||Selected==null)return;Selected.RuntimeControls=controlsToggle.Checked;if(!previewMode)Store.Write(settings);};
        var runtimeNote=LabelAt(runtimeCard,"Ctrl+Shift+G opens the menu · Change shortcut in panel · Separate session saves",16,59,780,23,9,false,muted);
        BindHint(controlsToggle,runtimeNote,"Remembered per game. Click this row or press Space to choose controls for the next launch.");
        playButton=ButtonAt(playPage,"Play selected game",0,238,354,50,()=>Guard(Launch),true);
        playManageButton=ButtonAt(playPage,"Manage / Uninstall  ▾",371,238,241,50,()=>{if(Selected!=null)ShowManageMenu(playManageButton,Selected);},false);
        gameFolderButton=ButtonAt(playPage,"Open game folder",627,238,185,50,()=>Guard(()=>LauncherFolders.Open(Selected.Root)),false);
        saveFolderButton=ButtonAt(playPage,"Open saves",0,303,170,38,()=>Guard(OpenSaves),false);
        controlsButton=ButtonAt(playPage,"Control sessions",185,303,197,38,()=>Guard(()=>{if(Selected!=null){var folder=Store.Child(Path.Combine(Store.Home,"Control Sessions"),Selected.Id);Directory.CreateDirectory(folder);LauncherFolders.Open(folder);}}),false);
        var actionNote=LabelAt(playPage,"Manage includes backups, English, upgrades and safe removal.",395,303,417,40,10,false,muted);
        BindHint(playButton,actionNote,"Launch this version with its own saves and the control setting above.");
        BindHint(gameFolderButton,actionNote,"Open the original installation folder for this game.");
        BindHint(saveFolderButton,actionNote,"Open this version's original save folder.");
        BindHint(controlsButton,actionNote,"Open the separate control session and its edited saves.");
    }
    void BuildInstall(){
        installPage=Page();
        LabelAt(installPage,"INSTALL A VERSION",0,0,760,25,10,true,muted);
        LabelAt(installPage,"Install or upgrade",0,35,812,43,24,true);
        LabelAt(installPage,"Select your version and source. The launcher checks the original files before installing.",0,83,812,28,10,false,muted);
        LabelAt(installPage,"VERSION",0,124,394,19,9,true,muted);
        LabelAt(installPage,"SOURCE METHOD",410,124,392,19,9,true,muted);
        targets=new ChoicePicker{Location=new Point(0,151),Size=new Size(394,44),Font=new Font("Segoe UI",10),BackColor=card,ForeColor=ForeColor,AccessibleName="Version to install"};
        var recipes=RecipeSupport.Catalog().Recipes.Where(x=>x.Id!="abu-51-01").ToList();recipes.Add(new ModRecipe{Id="gorpg-1-23",Name="Gorpg 1.23 - clean base",RequiredBase="standalone",Components=new[]{"Clean Chinese Gorpg 1.23"},Standalone=true});
        recipes.Add(new ModRecipe{Id="ch-current",Name="CH",RequiredBase="standalone",Components=new[]{"CH reference build"},Standalone=true});
        targets.Items.AddRange(recipes.ToArray());installPage.Controls.Add(targets);
        methods=new ChoicePicker{Location=new Point(410,151),Size=new Size(402,44),Font=new Font("Segoe UI",10),BackColor=card,ForeColor=ForeColor,AccessibleName="Installation method"};
        methods.Items.AddRange(new object[]{"Version conversion pack","Original files / creator bundle","Existing installed game"});installPage.Controls.Add(methods);
        var checklist=new SleekCard{Location=new Point(0,223),Size=new Size(812,137),BackColor=card};installPage.Controls.Add(checklist);
        LabelAt(checklist,"SOURCE STATUS",18,14,765,22,10,true,muted);
        installNeed=LabelAt(checklist,"Select a target version.",18,43,776,82,10,false);
        installButton=ButtonAt(installPage,"Install / Upgrade",0,382,392,51,()=>GuidedInstall(),true);
        installSourceButton=ButtonAt(installPage,"Add source files...",409,382,206,51,()=>Guard(ChooseOriginalInputs),false);
        installSearchButton=ButtonAt(installPage,"Find files",630,382,182,51,()=>ScanAgain(),false);discoveryButton=installSearchButton;
        englishOnInstall=new CheckBox{Location=new Point(4,460),Size=new Size(780,29),Text="Apply matching English translation when available",Checked=true,AutoSize=false,ForeColor=ForeColor,BackColor=background};installPage.Controls.Add(englishOnInstall);
        englishOnInstall.CheckedChanged+=delegate{if(!busy)UpdateInstall();};
        installLocation=LabelAt(installPage,"",0,513,812,42,9,false,muted);
        installDetailsButton=ButtonAt(installPage,"More options  ▾",0,571,236,43,()=>ToggleInstallDetails(),false);
        installDetectedButton=ButtonAt(installPage,"View detected files",251,571,244,43,()=>ShowFoundFiles(),false);
        installFolderButton=ButtonAt(installPage,"Change install folder",510,571,302,43,()=>Guard(ChangeLocation),false);
        installStartingButton=ButtonAt(installPage,"Choose starting game",0,633,257,40,()=>ChooseStartingVersion(),false);
        installLayersButton=ButtonAt(installPage,"Advanced: ordered layers",275,633,289,40,()=>Guard(OrderedLayers),false);
        var rescan=ButtonAt(installPage,"Search another folder",582,633,230,40,()=>AddSearchFolder(),false);
        installNote=LabelAt(installPage,"",0,691,805,49,9,false,muted);
        sourceInputNote=LabelAt(installPage,"",0,751,805,35,9,false,muted);
        installNext=LabelAt(installPage,"",0,798,805,78,9,false,muted);
        discoveryStatus=LabelAt(installPage,"Source discovery is available.",0,901,805,53,9,false,muted);
        installDiagram.Visible=false;installDiagram.Connect=false;
        installDetailsExpanded=false;
        installStartingButton.Visible=false;installLayersButton.Visible=false;rescan.Visible=false;
        installNote.Visible=false;sourceInputNote.Visible=false;installNext.Visible=false;discoveryStatus.Visible=false;
        targets.SelectedIndexChanged+=delegate{Guard(UpdateInstall);};methods.SelectedIndexChanged+=delegate{Guard(UpdateInstall);};
        int latest=targets.Items.FindIndex(x=>x is ModRecipe&&((ModRecipe)x).Id==UpgradeSupport.Latest);
        targets.SelectedIndex=latest>=0?latest:0;methods.SelectedIndex=1;
    }
    void ToggleInstallDetails(){
        installDetailsExpanded=!installDetailsExpanded;
        installDetailsButton.Text=installDetailsExpanded?"Fewer options  ▴":"More options  ▾";
        foreach(Control control in new Control[]{installStartingButton,installLayersButton,installNote,sourceInputNote,installNext,discoveryStatus})
            control.Visible=installDetailsExpanded;
        // The third advanced button is a small independent search shortcut.
        foreach(Control child in installPage.Controls)
            if(child is Button&&child.Text=="Search another folder")child.Visible=installDetailsExpanded;
        FitPage();
    }
    void BuildSaves(){
        savePage=Page();LabelAt(savePage,"SAVE MANAGEMENT",0,0,760,24,10,true,muted);
        saveTitle=LabelAt(savePage,"",0,32,812,48,24,true);
        LabelAt(savePage,"Saves are separate for each game version. Choose the game from the left library.",0,91,790,34,10,false,muted);
        var panel=new SleekCard{Location=new Point(0,145),Size=new Size(812,155),BackColor=card};savePage.Controls.Add(panel);
        saveNote=LabelAt(panel,"",18,16,776,32,15,true);
        saveFolder=LabelAt(panel,"",18,57,776,46,10,false,muted);
        openSaveButton=ButtonAt(panel,"Open save folder",18,109,234,35,()=>Guard(OpenSaves),false);
        backupButton=ButtonAt(savePage,"Back up now",0,333,248,47,()=>Guard(BackupNow),true);
        restoreButton=ButtonAt(savePage,"Restore backup",263,333,252,47,()=>Guard(Restore),false);
        copyButton=ButtonAt(savePage,"Copy from other version",531,333,281,47,()=>Guard(CopySaves),false);
        saveGuide=LabelAt(savePage,"Backups are timestamped; restores and copy operations first preserve existing destination saves.",0,402,812,75,10,false,muted);
        LabelAt(savePage,"CROSS-VERSION SAVES",0,481,812,22,10,true,muted);
        LabelAt(savePage,"Some updates change event IDs and routes. Copy only between verified compatible releases.\r\nSaves are not converted. Your source game and its saves remain untouched.",0,509,812,56,10,false,muted);
        ButtonAt(savePage,"Manage this game  ▾",0,566,290,40,()=>{if(Selected!=null)ShowManageMenu(savePage,Selected,new Point(0,606));},false);
    }
    void BuildEnglish(){
        englishPage=Page();LabelAt(englishPage,"TRANSLATION",0,0,800,24,10,true,muted);
        englishTitle=LabelAt(englishPage,"Choose a version",0,32,812,50,24,true);
        englishState=LabelAt(englishPage,"",0,91,810,33,13,true);
        englishCoverage=new SleekProgress{Location=new Point(0,141),Size=new Size(812,12),Minimum=0,Maximum=1000};englishPage.Controls.Add(englishCoverage);
        englishCounts=LabelAt(englishPage,"",0,173,812,62,10,false,muted);
        englishEnable=ButtonAt(englishPage,"Enable / update English",0,266,390,49,()=>EnglishAction(0),true);
        englishRestore=ButtonAt(englishPage,"Restore original text",408,266,404,49,()=>EnglishAction(1),false);
        LabelAt(englishPage,"Translation is reversible. Unsupported lines remain in the original language; saved games are unchanged.",0,341,812,58,10,false,muted);
        englishDetail=LabelAt(englishPage,"",0,416,812,65,10,false,muted);
        LabelAt(englishPage,"TRANSLATION TOOLS",0,466,812,23,10,true,muted);
        englishImport=ButtonAt(englishPage,"Import translated JSON",0,502,265,41,()=>EnglishAction(2),false);
        englishExport=ButtonAt(englishPage,"Export untranslated",282,502,260,41,()=>EnglishAction(3),false);
        ButtonAt(englishPage,"Open English files",560,502,252,41,()=>Guard(()=>LauncherFolders.Open(englishCurrent!=null&&englishCurrent.Supported?Path.Combine(Store.Home,"English",englishCurrent.VersionId):Path.Combine(Store.Home,"English"))),false);
        ButtonAt(englishPage,"Open untranslated log",0,559,265,41,()=>Guard(()=>{if(Selected==null)return;string path=Path.Combine(Store.WebRoot(Selected.Root),"celesphonia-english","seen-untranslated.json");if(!File.Exists(path)){MessageBox.Show(this,"This log appears after English is enabled and untranslated text is displayed in game.");return;}Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}),false);
        englishSeen=LabelAt(englishPage,"",282,563,530,30,9,false,muted);
        englishDiagram.Visible=false;englishDiagram.Connect=false;
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
        helpPage=Page();LabelAt(helpPage,"HELP & SETTINGS",0,0,760,24,10,true,muted);
        LabelAt(helpPage,"Everything in one place",0,32,812,47,24,true);
        LabelAt(helpPage,"INSTALLING",0,109,790,24,10,true,muted);
        LabelAt(helpPage,"Install → choose a version → choose original files.\r\nUse a complete creator update plus clean GoRPG for new installations. An existing compatible version can be upgraded with verified save carryover.",0,144,805,87,10,false,muted);
        LabelAt(helpPage,"MANAGING GAMES",0,248,790,24,10,true,muted);
        LabelAt(helpPage,"Right-click an installed game (or use Manage / Uninstall) to play, open folders, back up saves, upgrade, change English, or remove a version.\r\nUninstall only removes hash-verified managed files; archives, saves and unknown files stay.",0,284,805,87,10,false,muted);
        ButtonAt(helpPage,"Quick start guide",0,390,281,44,()=>OpenNotes(0),true);
        ButtonAt(helpPage,"Notes & changelogs",294,390,245,44,()=>OpenNotes(1),false);
        ButtonAt(helpPage,"Open delta packs",552,390,260,44,()=>Guard(()=>{Directory.CreateDirectory(Path.Combine(Store.Home,"Delta Packs"));Process.Start("explorer.exe",Store.Q(Path.Combine(Store.Home,"Delta Packs")));}),false);
        LabelAt(helpPage,"APPEARANCE",0,471,400,23,10,true,muted);
        themePicker=new ChoicePicker{Location=new Point(0,503),Size=new Size(310,42),Font=Font,BackColor=card,ForeColor=ForeColor,AccessibleName="Launcher theme"};themePicker.Items.AddRange(new object[]{"Midnight","Violet","Ocean","Daylight"});themePicker.SelectedItem=settings.LauncherTheme??"Midnight";helpPage.Controls.Add(themePicker);
        themePicker.SelectedIndexChanged+=delegate{ApplyTheme(themePicker.SelectedItem.ToString());if(!previewMode)Store.Write(settings);};
        LabelAt(helpPage,"Themes apply to all launcher pages and menus.",329,509,478,39,10,false,muted);
        LabelAt(helpPage,"LAUNCHER UPDATES  ·  v"+LauncherUpdate.CurrentVersion,0,552,650,21,10,true,muted);
        ButtonAt(helpPage,"Check launcher updates",0,581,310,37,()=>CheckLauncherUpdate(),true);
        ButtonAt(helpPage,"GitHub source",330,581,236,37,()=>ConfigureLauncherUpdates(),false);
    }
    NotesReader notesReader;
    void OpenNotes(int source){
        if(notesReader==null||notesReader.IsDisposed){notesReader=new NotesReader(source);notesReader.Show(this);}
        else{notesReader.SelectSource(source);notesReader.Activate();}
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
            StyleDialog(dialog);if(dialog.ShowDialog(this)!=DialogResult.OK)return false;
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
                    ".\r\nLatest supported game/mod release: Three-in-One 3.3.5 Ver1.053. Game releases are independent of launcher updates.","Launcher updates");
                return;
            }
            status.Text="Launcher v"+info.Version+" available from "+info.Repository;
            if(MessageBox.Show(this,"Install Celesphonia launcher "+info.Version+" from "+info.Repository+
                "?\r\n\r\nThe ZIP and GitHub checksum will be verified. The launcher will close, update, and restart. "+
                "Game directories, saves, profiles and control sessions are excluded. A rollback copy will be retained.",
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
        if(active!=null)main.Canvas.Height=Math.Max(main.ClientSize.Height,active.Controls.Cast<Control>().Where(c=>c.Visible).Select(c=>c.Bottom+8).DefaultIfEmpty(0).Max());
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
            saveTitle.Text="Choose an installed game";saveNote.Text="No game selected";saveFolder.Text="Open Files to select a game, or Install to add one.";
            if(saveGuide!=null)saveGuide.Text="2 · Choose a version first. Backup, restore, and copy actions then apply only to that game's saves.";
            UpdateEnglish();UpdateActions();return;
        }int count=CountSaves(p);playName.Text=p.Name.Replace(" - current installation","");statusCache.TryGetValue(p.Id,out englishCurrent);playInfo.Text=p.Note+"\r\nText: "+(englishCurrent==null?"Checking…":englishCurrent.Short);LoadEnglishStatus(p);
        playDiagram.Set(new[]{"Selected version","Its own saves","Back up & play"},new[]{p.Name,"Return to these saves every time you select this version.","A timestamped save backup is created before launch."},new[]{true,true,true});
        saveCount.Text=count+" save slots  ·  "+(count==0?"Ready for a new game":"Your progress is here");savePath.Text=p.SavePath;
        saveTitle.Text=p.Name;saveNote.Text=count+" numbered save slots for this version";saveFolder.Text=p.SavePath;
        if(saveGuide!=null){
            string backupRoot=Path.Combine(Store.Home,"Save Backups",p.Id);
            int copies=Directory.Exists(backupRoot)?Directory.GetDirectories(backupRoot).Length:0;
            int donors=LibraryProfiles.Count(x=>x.Id!=p.Id&&UpgradeSupport.CanTransfer(UniversalSupport.Identify(x.Root),UniversalSupport.Identify(p.Root)));
            saveGuide.Text="2 · "+copies+" timestamped backup folder"+(copies==1?"":"s")+" available · "+
                (donors==0?"No other verified compatible version to copy from.":donors+" compatible source version"+(donors==1?"":"s")+" for Copy.")+
                "\r\nBackup creates a copy; Restore and Copy first preserve the destination's prior saves.";
        }
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
    FoundSource OriginalCandidate(string id){return Found(id,"Game archive candidate");}
    FoundSource LocatedOriginal(string id){return Original(id)??OriginalCandidate(id);}
    Profile Clean(){return Installed("gorpg-1-23");}
    string DefaultPack(ModRecipe recipe){return Path.Combine(Store.Home,"Delta Packs",recipe.Id+".cgdelta.zip");}
    FoundSource Found(string recipe,string kind){FoundSource chosen;if(preferredSources.TryGetValue(recipe+"/"+kind,out chosen)&&string.IsNullOrEmpty(chosen.Problem)&&(File.Exists(chosen.Path)||Directory.Exists(chosen.Path)))return chosen;return discovered.FirstOrDefault(x=>x.RecipeId==recipe&&x.Kind==kind&&string.IsNullOrEmpty(x.Problem));}
    string ArchiveHint(ModRecipe recipe){
        if(!string.IsNullOrEmpty(recipe.ArchiveName))return recipe.ArchiveName;
        if(recipe.Id=="gorpg-1-23")return "魔法少女苍蓝星1.23gorpg汉.7z.001 + .002";
        if(recipe.Id=="ch-current")return "The full CH game archive or extracted CH game folder";
        return "The complete 3.3.4 bundle (.001 through .004 together), or its extracted content folder";
    }
    void UpdateInstallNext(ModRecipe recipe,int route){
        if(installNext==null)return;
        int bad=discovered.Count(x=>x.RecipeId==recipe.Id&&!string.IsNullOrEmpty(x.Problem));
        int pending=discovered.Count(x=>x.RecipeId==recipe.Id&&x.Kind=="Game archive candidate"&&string.IsNullOrEmpty(x.Problem));
        int stale=originalInputs.Count(x=>!Directory.Exists(x)&&!File.Exists(x));
        string prefix="4 · NEXT: ";
        if(route==2)
            installNext.Text=prefix+"Select the existing extracted game directory. Its files and saves stay in that folder.";
        else if(route==0)
            installNext.Text=prefix+"Choose a verified version pack and starting installation; the launcher checks the build before installing.";
        else if(Installed(recipe.Id)!=null)
            installNext.Text=prefix+"This version is already registered. Use it directly from Files or Play.";
        else if(recipe.Id=="three-in-one-3-3-4")
            installNext.Text=prefix+"Combine original 3.3.4 files, or provide clean GoRPG plus the hypnosis overlay. All 9,474 native hashes must match.";
        else if(HasNativeInventory(recipe.Id))
            installNext.Text=prefix+"Choose original game archives/folders; missing bytes can come from several sources. Full native hashes are verified.";
        else if(UpgradeSupport.IsUpgrade(recipe.Id))
            installNext.Text=prefix+"Choose the matching creator update and a compatible installed source, or clean GoRPG for a fresh installation.";
        else
            installNext.Text=prefix+"Choose the intact creator bundle and original GoRPG base. The completed installation is checked before registration.";
        if(pending>0)installNext.Text+="\r\n"+pending+" nearby archive candidate"+(pending==1?"":"s")+" still require"+(pending==1?"s":"")+" complete-file verification.";
        if(bad>0)installNext.Text+="\r\n"+bad+" incomplete source"+(bad==1?"":"s")+" found; open View found files for details.";
        if(stale>0)installNext.Text+="\r\n"+stale+" selected input"+(stale==1?" is":"s are")+" missing; open Combine original sources to replace.";
    }
    void UpdateInstall(){
        if(Target==null || methods.SelectedIndex<0 || installNeed==null)return;var recipe=Target;int route=methods.SelectedIndex;
        UpdateInstallNext(recipe,route);
        var ch=CH();var clean=Clean();var any=AnyPack(recipe.Id);var delta=any??Found(recipe.Id,"Delta pack");bool found=delta!=null||File.Exists(DefaultPack(recipe));var bundle=LocatedOriginal(recipe.Id);
        if(sourceInputNote!=null)sourceInputNote.Text=originalInputs.Count==0
            ?"Choose ZIP/RAR/7z files, extracted folders, or installed games together."
            :originalInputs.Count+" selected original source"+(originalInputs.Count==1?"":"s")+" (verified during installation)";
        if(UpgradeSupport.IsUpgrade(recipe.Id)&&route!=2){
            var chosen=startingSource??CompatibleFolderSource(recipe.Id)??UpgradeSupport.StartingProfile(settings,recipe.Id);
            var source=chosen!=null&&UpgradeSupport.MayUpgrade(UniversalSupport.Identify(chosen.Root),recipe.Id)?chosen:CompatibleFolderSource(recipe.Id)??UpgradeSupport.StartingProfile(settings,recipe.Id);
            var installed=Installed(recipe.Id);
            bool fresh=source==null;var baseGame=Clean();var baseArchive=LocatedOriginal("gorpg-1-23");
            installDiagram.Set(new[]{fresh?"GoRPG source":"Earlier version","Creator update",fresh?"New game":"Carry saves"},
                new[]{fresh?(baseGame!=null?"Clean base registered.":baseArchive!=null?"Original found; full SHA check next.":"Add original GoRPG files below."):
                        "Choose a compatible installed game.",
                      bundle==null?"Choose the complete creator bundle.":"Detected files need final verification.",
                      fresh?"Independent game; empty saves.":"Existing saves backed up and copied."},
                new[]{fresh?(baseGame!=null||baseArchive!=null||AnyPack("gorpg-1-23")!=null):source!=null,bundle!=null,installed!=null});
            installNeed.Text=(fresh?(baseGame!=null?"✓ Verified clean GoRPG base: "+baseGame.Name:baseArchive!=null?"• GoRPG source to check: "+Path.GetFileName(baseArchive.Path):"• Needed: complete GoRPG 1.23 originals. Combine ZIP/RAR/7z or folders below."):"✓ Earlier game: "+source.Name)+"\r\n"+(bundle==null?"• Needed: complete "+recipe.Name+" creator update":"• Source candidate: "+Path.GetFileName(bundle.Path)+" (verified during installation)")+"\r\n"+(fresh?"✓ New independent game with empty saves":"✓ Compatible saves carried forward after backup")+" · English "+(englishOnInstall.Checked?"requested":"not requested");
            installNote.Text=fresh?"Native GoRPG files can be recovered from selected original sources, then combined with a verified creator bundle.":"Direct verified forward upgrade; backups retained. English is optional.";
            installButton.Text=installed!=null?"Use installed version":fresh?"Install fresh mod + English":"Upgrade & carry saves";installLocation.Text="New installations: "+settings.InstallRoot;RefreshCompactInstall();UpdateActions();return;
        }
        if(route==1&&recipe.Id=="three-in-one-3-3-4"){
            var native=LocatedOriginal(recipe.Id);var cleanBase=Clean();
            installDiagram.Set(new[]{"Original 3.3.4","Hypnosis update","SHA-256 check"},
                new[]{"Combine archives and game folders.",
                      "Fallback: GoRPG plus creator bundle.",
                      "All 9,474 native files must match."},
                new[]{native!=null||originalInputs.Count>0,cleanBase!=null,Installed(recipe.Id)!=null});
            installNeed.Text=(native!=null?"• Original candidate: "+Path.GetFileName(native.Path):"• Choose original 3.3.4 archives/folders")+
                "\r\n"+(cleanBase!=null?"✓ GoRPG source: "+cleanBase.Name:"• GoRPG original sources can be combined if needed")+
                "\r\n• Creator overlay must supply all 2,637 changed/new files with exact hashes";
            installNote.Text="3.3.4 is registered only after all 9,474 native files pass SHA-256 verification.";
            installButton.Text=Installed(recipe.Id)!=null?"Use installed version":"Verify sources & install";
            installLocation.Text="New installations: "+settings.InstallRoot;RefreshCompactInstall();UpdateActions();return;
        }
        if(route==0){
            var source=any==null?ch:SupportedSource(recipe.Id);bool already=Installed(recipe.Id)!=null;
            installDiagram.Set(new[]{"Starting game","Version pack",already?"Installed":"New game"},
                new[]{source==null?"Choose your source version.":"Recognized original game.",any!=null?"Any-version pack detected.":found?"Legacy pack is CH-only.":"Choose a verified pack.",already?"Switch to it from Files.":"Independent folder and saves."},
                new[]{source!=null,found,already});
            installNeed.Text=(source!=null?"✓ Starting game: "+(UniversalSupport.DetectedName(source.Root)??source.Name):"• Choose a supported starting game folder or full archive")+"\r\n"+(any!=null?"✓ Any-version pack found":found?"• Legacy pack needs CH; use the any-version pack for other starting games":"• Needed: "+UniversalSupport.PackName)+"\r\n"+(already?"✓ This target is already installed — switch directly in Files":"✓ Original games and their saves stay intact");
            installNote.Text="Any-version routes verify the starting files. Old single-target packs remain CH-only.";
        }else if(route==1 && !recipe.Standalone && (bundle==null||bundle.Kind!="Full game archive")){
            installDiagram.Set(new[]{"GoRPG base","Creator bundle","New game"},
                new[]{clean==null?"Add exact original base files.":"Clean game registered.",bundle==null?"Choose complete creator files.":"Matching candidate found.","Independent installation; empty saves."},
                new[]{clean!=null,bundle!=null,true});
            installNeed.Text=(clean!=null?"✓ Clean Gorpg base is registered":"• Needed: clean Gorpg 1.23; reconstruct it from any supported version or use its original archive")+"\r\n"+(bundle==null?"• Needed bundle: "+ArchiveHint(recipe):"✓ Bundle found: "+Path.GetFileName(bundle.Path))+"\r\n• Never stack another complete bundle or Maple with this bundle";
            installNote.Text="Content folders overwrite matching folders inside www. 3.3.5 already contains its three components.";
        }else{
            installDiagram.Set(new[]{"Original game","Version check","Ready to play"},
                new[]{"ZIP/RAR/7z or extracted folder.","Check native hashes and version.","Keep this game's own saves."},
                new[]{false,false,true});
            installNeed.Text="• Use an extracted game folder, ZIP, RAR, or split .7z.001 full archive\r\n"+(route==1?(bundle!=null?"• Source candidate: "+Path.GetFileName(bundle.Path):"• Original archive: "+ArchiveHint(recipe)):"• Select its game folder; wrapper folders are detected automatically")+"\r\n• Archive detection checks version markers; installation must verify the rest";
            installNote.Text=recipe.Id=="gorpg-1-23"?"Keep both .001 and .002 together. The launcher can extract and register the game.":"Full standalone games can be installed from original archives without CH or a delta pack.";
        }
        installButton.Text=route==0&&Installed(recipe.Id)!=null?"Use installed version":route==0&&found?"Install from detected pack":route==0?"Choose pack & install":route==1&&bundle!=null?"Use detected original files":route==1?"Choose original files":"Choose installed game folder";
        var incomplete=discovered.FirstOrDefault(x=>x.RecipeId==recipe.Id&&!string.IsNullOrEmpty(x.Problem));if(route==1&&incomplete!=null)installNote.Text=incomplete.Problem+" · keep all parts together. View found files shows their folder.";
        installLocation.Text="New installations: "+settings.InstallRoot;RefreshCompactInstall();UpdateActions();installDiagram.Invalidate();
    }
    void RefreshCompactInstall(){
        if(Target==null||installNeed==null)return;
        var recipe=Target;int route=methods.SelectedIndex;
        var existing=Installed(recipe.Id);
        var detected=LocatedOriginal(recipe.Id);
        var creator=UpgradeSupport.IsUpgrade(recipe.Id)?detected:Original(recipe.Id);
        var candidate=discovered.FirstOrDefault(x=>x.RecipeId==recipe.Id&&!string.IsNullOrEmpty(x.Problem));
        var prior=startingSource!=null&&UpgradeSupport.MayUpgrade(UniversalSupport.Identify(startingSource.Root),recipe.Id)
            ?startingSource:UpgradeSupport.StartingProfile(settings,recipe.Id);
        bool upgrade=UpgradeSupport.IsUpgrade(recipe.Id);
        if(existing!=null){
            installNeed.Text="Already installed: "+existing.Name+
                "\r\nNo reinstall needed. Open the installed version from Play or Files.";
            installButton.Text="Open installed version";
        }else if(route==2){
            installNeed.Text="Register an existing extracted game folder.\r\nThe game stays in its current location; no copy or patch is applied.";
            installButton.Text="Choose installed folder";
        }else if(route==0){
            installNeed.Text="Conversion pack: "+(AnyPack(recipe.Id)!=null?"Detected; verified during installation.":"Choose a compatible version-conversion pack.")+
                "\r\nSource: "+(SupportedSource(recipe.Id)!=null?"Installed game available.":"Select the starting game in More options.");
            installButton.Text="Install using pack";
        }else if(upgrade){
            installNeed.Text=prior!=null?"Upgrade from "+prior.Name+" (saves backed up and carried).":
                "Fresh installation: add original GoRPG files or a compatible complete game archive.";
            installNeed.Text+="\r\nCreator update: "+(detected!=null?"Found candidate; verified during install.":"Choose the original creator bundle when prompted.");
            installButton.Text=prior!=null?"Upgrade & carry saves":"Install new version";
        }else{
            installNeed.Text=detected==null?
                "Choose original ZIP/RAR/7z archives or an extracted game folder.":
                "Found "+detected.Kind.ToLowerInvariant()+": "+Path.GetFileName(detected.Path)+".";
            installNeed.Text+="\r\nThe full native-file hashes are checked before registration.";
            installButton.Text="Install this version";
        }
        if(candidate!=null&&!string.IsNullOrEmpty(candidate.Problem))
            installNeed.Text+="\r\nIncomplete detected source — open View detected files.";
        installLocation.Text="Destination: "+settings.InstallRoot;
        if(originalInputs.Count>0)installLocation.Text+="  ·  "+originalInputs.Count+" original source"+(originalInputs.Count==1?"":"s")+" selected";
    }
    void UpdateActions(){
        if(playButton==null||status==null)return;bool allowed=!busy&&!running&&!discovering;playButton.Enabled=!busy&&!running&&Selected!=null;
        gameFolderButton.Enabled=Selected!=null&&Directory.Exists(Selected.Root);saveFolderButton.Enabled=Selected!=null;
        if(copyButton!=null){copyButton.Enabled=allowed&&Selected!=null&&LibraryProfiles.Any(x=>x.Id!=Selected.Id&&UpgradeSupport.CanTransfer(UniversalSupport.Identify(x.Root),UniversalSupport.Identify(Selected.Root)));backupButton.Enabled=allowed&&Selected!=null;restoreButton.Enabled=allowed&&Selected!=null;}
        if(openSaveButton!=null)openSaveButton.Enabled=allowed&&Selected!=null;
        if(controlsButton!=null)controlsButton.Enabled=Selected!=null&&!busy;
        if(controlsToggle!=null)controlsToggle.Enabled=!busy&&Selected!=null;
        if(installButton!=null)installButton.Enabled=!busy&&!running&&Target!=null;
        if(englishEnable!=null){bool supported=englishCurrent!=null&&englishCurrent.Supported&&englishCurrent.Pack!=null&&!englishCurrent.Modified;englishEnable.Enabled=allowed&&supported;englishRestore.Enabled=allowed&&englishCurrent!=null&&(englishCurrent.Enabled||englishCurrent.Modified);englishImport.Enabled=allowed&&supported;englishExport.Enabled=!busy&&supported;}
        if(running&&!busy)status.Text="Game running · save and close it before switching versions, installing, or changing saves.";
        else if(!busy&&(status.Text=="Ready"||status.Text.StartsWith("Game running")))status.Text="Ready · choose a version to play, or open Install for the files you need.";
    }
    bool GameClosed(){running=Store.Running(settings);if(!running)return true;UpdateActions();MessageBox.Show(this,"Save and close the active game first.","Game is running");return false;}
    void Guard(Action action){try{action();}catch(Exception e){MessageBox.Show(this,e.Message,"Celesphonia Launcher",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    static string InstallErrorAdvice(Exception error){
        string message=error.Message??"";
        if(message.IndexOf("space",StringComparison.OrdinalIgnoreCase)>=0)
            return "Next: choose another Install location or make room for the independent game.";
        if(message.IndexOf("missing",StringComparison.OrdinalIgnoreCase)>=0||
           message.IndexOf("incomplete",StringComparison.OrdinalIgnoreCase)>=0)
            return "Next: open Combine original sources and add the missing original archives or extracted folders.";
        if(message.IndexOf("hash",StringComparison.OrdinalIgnoreCase)>=0||
           message.IndexOf("mismatch",StringComparison.OrdinalIgnoreCase)>=0||
           message.IndexOf("differ",StringComparison.OrdinalIgnoreCase)>=0)
            return "Next: select the intact original version and matching creator update, then retry verification.";
        return "Next: inspect the exact error and selected sources in View found files.";
    }
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
            running=true;status.Text=(controls?"Runtime controls started · Use Controls or your shortcut · ":"Game started · ")+profile.Name;
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
    string PromptName(string initial){using(var dialog=new Form{Text="Name this version",ClientSize=new Size(475,125),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var text=new TextBox{Location=new Point(16,16),Size=new Size(443,30),Text=initial};dialog.Controls.Add(text);var ok=new Button{Text="Add version",Location=new Point(313,71),Size=new Size(146,34),DialogResult=DialogResult.OK};dialog.Controls.Add(ok);dialog.AcceptButton=ok;StyleDialog(dialog);return dialog.ShowDialog(this)==DialogResult.OK?text.Text.Trim():null;}}
    void AddProfile(){string root=Folder("Select a game folder or the folder containing its extracted game.",settings.InstallRoot);if(root==null)return;root=UniversalSupport.FindGameRoot(root);Register(root,Path.GetFileName(root));}
    void Register(string root,string suggested){
        var existing=LibraryProfiles.FirstOrDefault(x=>Path.GetFullPath(x.Root).Equals(Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase));if(existing!=null){settings.SelectedId=existing.Id;RefreshProfiles();ShowPage("Play");status.Text="Selected existing installation · "+existing.Name;return;}
        string detected=UniversalSupport.DetectedName(root);string name=PromptName(detected??suggested);if(string.IsNullOrEmpty(name))return;var p=Store.Detect(root,name);if(detected!=null)p.Note="Detected "+detected+"; existing game files and saves";Added(p);
    }
    void Added(Profile p){if(FileDiscovery.IsNativeProbe(p.Root))throw new IOException("This is an isolated verification copy. Choose your installed game folder.");var existing=LibraryProfiles.FirstOrDefault(x=>ProfileLibrary.Identity(x).Equals(ProfileLibrary.Identity(p),StringComparison.OrdinalIgnoreCase));if(existing!=null)p=existing;else settings.Profiles.Add(p);Store.UnsuppressRoot(settings,p.Root);settings.SelectedId=p.Id;Store.Write(settings);RefreshProfiles();status.Text=p.Name+" selected · its own saves are ready.";UpdateInstall();ShowPage("Play");}
    void ChangeLocation(){if(busy)return;string path=Folder("Choose where NEW independent game profiles will be installed. Existing profiles stay where they are.",settings.InstallRoot);if(path==null)return;settings.InstallRoot=path;Store.Write(settings);UpdateInstall();}
    string PickArchive(string title,string filter,string initial,string fileName){using(var d=new OpenFileDialog{Title=title,Filter=filter,InitialDirectory=initial,FileName=fileName??""})return d.ShowDialog(this)==DialogResult.OK?d.FileName:null;}
    void AddOriginalInput(string path){
        if(string.IsNullOrWhiteSpace(path))return;
        path=Path.GetFullPath(path);
        if(!File.Exists(path)&&!Directory.Exists(path))throw new IOException("Source no longer exists: "+path);
        if(!originalInputs.Any(x=>x.Equals(path,StringComparison.OrdinalIgnoreCase)))originalInputs.Add(path);
    }
    void ChooseOriginalInputs(){
        if(busy)return;
        using(var dialog=new Form{Text="Original game files · combine multiple sources",ClientSize=new Size(875,502),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"Select as many original downloads, extracted folders and installed game directories as you have. Recovery checks every native file against its trusted hash.",16,12,841,50,10,false,Color.Black);
            var items=new ListBox{Location=new Point(16,68),Size=new Size(843,245),HorizontalScrollbar=true};dialog.Controls.Add(items);
            Action<string> add=path=>{if(string.IsNullOrWhiteSpace(path))return;string absolute=Path.GetFullPath(path);
                if(!File.Exists(absolute)&&!Directory.Exists(absolute)){MessageBox.Show(dialog,"Missing source: "+absolute);return;}
                if(!items.Items.Cast<string>().Any(x=>x.Equals(absolute,StringComparison.OrdinalIgnoreCase)))items.Items.Add(absolute);};
            foreach(string path in originalInputs)add(path);
            ButtonAt(dialog,"Add archives...",16,327,160,37,()=>{
                using(var d=new OpenFileDialog{Title="Select all original ZIP, RAR or 7z archives (choose .001 for split archives)",Multiselect=true,
                    Filter="Original archives|*.zip;*.rar;*.7z;*.001;*.7z.001|All files|*.*",
                    InitialDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")})
                    if(d.ShowDialog(dialog)==DialogResult.OK)foreach(string file in d.FileNames)add(file);
            },false);
            ButtonAt(dialog,"Add folder...",187,327,151,37,()=>{
                using(var d=new FolderBrowserDialog{Description="Select an original game folder, extracted archive, or wrapper folder",ShowNewFolderButton=false})
                    if(d.ShowDialog(dialog)==DialogResult.OK)add(d.SelectedPath);
            },false);
            var choices=discovered.Where(x=>(x.Kind=="Game archive candidate"||x.Kind=="Original archive"||x.Kind=="Extracted bundle"||x.Kind=="Installed game")&&string.IsNullOrEmpty(x.Problem))
                .Concat(LibraryProfiles.Where(x=>Directory.Exists(x.Root)).Select(x=>new FoundSource{
                    Kind="Installed game",Path=x.Root,RecipeId=UniversalSupport.Identify(x.Root),Name=x.Name}))
                .GroupBy(x=>x.Path,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).OrderBy(x=>x.Name).ToArray();
            var detected=new ComboBox{Location=new Point(16,378),Size=new Size(633,31),DropDownStyle=ComboBoxStyle.DropDownList,ForeColor=Color.Black};
            detected.Items.AddRange(choices);if(detected.Items.Count>0)detected.SelectedIndex=0;dialog.Controls.Add(detected);
            ButtonAt(dialog,"Add detected",663,376,196,37,()=>{var chosen=detected.SelectedItem as FoundSource;if(chosen!=null)add(chosen.Path);},false);
            ButtonAt(dialog,"Remove selected",350,327,167,37,()=>{if(items.SelectedIndex>=0)items.Items.RemoveAt(items.SelectedIndex);},false);
            ButtonAt(dialog,"Clear",528,327,121,37,()=>items.Items.Clear(),false);
            LabelAt(dialog,"Installed versions can be selected from Found files or Starting version too. Nothing is extracted while choosing.",16,420,830,25,9,false,Color.DimGray);
            var ok=new Button{Text="Use these sources",Location=new Point(636,451),Size=new Size(223,36),DialogResult=DialogResult.OK};dialog.Controls.Add(ok);dialog.AcceptButton=ok;
            StyleDialog(dialog);if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            originalInputs.Clear();foreach(string path in items.Items.Cast<string>())AddOriginalInput(path);
            methods.SelectedIndex=1;UpdateInstall();
        }
    }
    string PickBundle(ModRecipe recipe){
        using(var dialog=new Form{Text="Choose the complete mod bundle",ClientSize=new Size(595,182),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"Choose an archive, or a folder already extracted from it.",18,16,565,35,11,false,Color.Black);
            var archive=new Button{Text="Choose archive",Location=new Point(18,79),Size=new Size(265,43),DialogResult=DialogResult.Yes};var folder=new Button{Text="Choose extracted folder",Location=new Point(304,79),Size=new Size(272,43),DialogResult=DialogResult.No};dialog.Controls.Add(archive);dialog.Controls.Add(folder);
            StyleDialog(dialog);var choice=dialog.ShowDialog(this);if(choice==DialogResult.Yes)return PickArchive("Select ONE complete bundle; choose .001 for split archives","Mod archives|*.zip;*.rar;*r删ar;*.7z;*.001;*.7删z|All files|*.*",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","Telegram Desktop"),recipe.ArchiveName);
            if(choice==DialogResult.No)return Folder("Select the bundle's extracted folder containing data, js, img, and audio.",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"));return null;
        }
    }
    static bool HasNativeInventory(string id){
        return id=="gorpg-1-23" || !string.IsNullOrEmpty(id) &&
            typeof(MixedSourceRecovery).Assembly.GetManifestResourceInfo("Celesphonia.NativeReferenceFiles."+id+".json")!=null;
    }
    string[] NativeRecoveryInputs(string versionId,string explicitSource=null){
        var paths=new List<string>();
        bool pooledCreator=UpgradeSupport.IsUpgrade(versionId)&&HasNativeInventory(versionId);
        if(pooledCreator){
            // Intact installed games and exact target archives must be tried
            // before a broad extracted/download folder found by discovery.
            paths.AddRange(LibraryProfiles.Where(x=>Directory.Exists(x.Root)&&!FileDiscovery.IsNativeProbe(x.Root))
                .Select(x=>new{Profile=x,Id=UniversalSupport.Identify(x.Root)})
                .Where(x=>x.Id!=null)
                .OrderBy(x=>UpgradeSupport.MayUpgrade(x.Id,versionId)?0:x.Id=="gorpg-1-23"?1:2)
                .ThenByDescending(x=>RecipeSupport.Catalog().Recipes.Where(r=>r.Id==x.Id).Select(r=>r.SaveGeneration).FirstOrDefault())
                .Select(x=>x.Profile.Root));
            paths.AddRange(discovered.Where(x=>x.RecipeId==versionId&&string.IsNullOrEmpty(x.Problem)&&File.Exists(x.Path)&&
                new[]{"Original archive","Game archive candidate"}.Contains(x.Kind)).Select(x=>x.Path));
        }
        paths.AddRange(originalInputs);
        var found=LocatedOriginal(versionId);
        if(found!=null&&string.IsNullOrEmpty(found.Problem))paths.Add(found.Path);
        if(!string.IsNullOrEmpty(explicitSource))paths.Add(explicitSource);
        if(pooledCreator){
            paths.AddRange(discovered.Where(x=>x.RecipeId!=null&&string.IsNullOrEmpty(x.Problem)&&
                UniversalSupport.ReadIndex().Versions.Any(v=>v.Id==x.RecipeId)&&
                new[]{"Original archive","Game archive candidate","Archive candidate","Extracted bundle","Installed game"}.Contains(x.Kind))
                .OrderBy(x=>x.RecipeId==versionId?0:1).Select(x=>x.Path));
        }
        return paths.Where(x=>!string.IsNullOrWhiteSpace(x)&&(File.Exists(x)||Directory.Exists(x)))
            .Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    Profile CompatibleFolderSource(string targetId){
        foreach(string input in originalInputs.Where(Directory.Exists)){
            string game;
            try{game=UniversalSupport.FindGameRoot(input);}
            catch(IOException){continue;}
            catch(UnauthorizedAccessException){continue;}
            string id=UniversalSupport.Identify(game);
            if(UpgradeSupport.MayUpgrade(id,targetId))
                return LibraryProfiles.FirstOrDefault(x=>x.Root.Equals(game,StringComparison.OrdinalIgnoreCase))
                    ??Store.Detect(game,UniversalSupport.DetectedName(game)??Path.GetFileName(game));
        }
        return null;
    }
    static MixedRecoveryReport VerifiedNativeSources(string versionId,string[] sources){
        var reference=MixedSourceRecovery.LoadEmbeddedReference(versionId);
        var report=MixedSourceRecovery.Probe(reference,sources,CancellationToken.None);
        if(!report.Complete)
            throw new IOException("Original "+versionId+" files are incomplete: "+report.FoundCount+"/"+report.RequiredCount+
                " exact native files found. Missing "+report.Missing.Count+": "+
                string.Join(", ",report.Missing.Take(6).ToArray())+
                (report.Missing.Count>6?"…":"")+
                ". Add another original archive or extracted folder using Combine original sources.");
        return report;
    }
    static Profile InstallRecoveredVersion(string versionId,string[] sources,string location){
        var report=VerifiedNativeSources(versionId,sources);
        return InstallRecoveredVersion(versionId,report,location);
    }
    static Profile InstallRecoveredVersion(string versionId,MixedRecoveryReport report,string location){
        if(report==null||!report.Complete||report.VersionId!=versionId)
            throw new IOException("Full exact native source audit is required before installation.");
        Directory.CreateDirectory(location);
        string root=Path.Combine(location,"Original-"+versionId+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
        MixedSourceRecovery.Recover(report,root,CancellationToken.None);
        if(UniversalSupport.Identify(root)!=versionId)
            throw new IOException("Recovered native game failed its reference-version check.");
        string name=UniversalSupport.ReadIndex().Versions.Where(x=>x.Id==versionId).Select(x=>x.Name).FirstOrDefault()??versionId;
        var game=Store.Detect(root,name);
        Directory.CreateDirectory(game.SavePath);
        game.Note="Original "+name+" verified against "+report.RequiredCount+" native file hashes; independent game and saves";
        return game;
    }
    static Profile InstallWithRecoveredGoRpg(string[] sources,string update,string location,ModRecipe recipe,bool english){
        var report=VerifiedNativeSources("gorpg-1-23",sources);
        Directory.CreateDirectory(location);
        string staging=Path.Combine(location,".cph-original-"+Guid.NewGuid().ToString("N"));
        try{
            MixedSourceRecovery.Recover(report,staging,CancellationToken.None);
            RecipeSupport.ValidateBase(staging);
            var game=RecipeSupport.Install(staging,location,recipe.Name,new List<LayerInput>{new LayerInput{Source=update}});
            if(UniversalSupport.Identify(game.Root)!=recipe.Id)
                throw new IOException("Recovered original and creator bundle did not produce the selected version.");
            SetInstalledEnglish(game,english);RecipeSupport.VerifyProfile(game);
            game.Note="Independent "+recipe.Name+" from exact original native files and verified creator bundle; "+
                (english?"English enabled":"native language")+"; empty saves";
            return game;
        }finally{
            // This is the unique scratch directory owned by this invocation.
            // The final layered profile has independently copied and hashed all its files.
            if(Path.GetDirectoryName(Path.GetFullPath(staging)).TrimEnd('\\')==
                Path.GetFullPath(location).TrimEnd('\\') &&
                Path.GetFileName(staging).StartsWith(".cph-original-",StringComparison.Ordinal) &&
                Directory.Exists(staging))Directory.Delete(staging,true);
        }
    }
    static Dictionary<string,string> Validate334Overlay(string web,MixedNativeReference target,MixedNativeReference clean){
        if(target.VersionId!="three-in-one-3-3-4"||target.Files.Count!=9474||
           clean.VersionId!="gorpg-1-23"||clean.Files.Count!=7208)
            throw new IOException("Complete 3.3.4 and GoRPG native inventories are required.");
        Store.CheckLinks(web);
        var required=target.Files.ToDictionary(x=>x.Path,x=>x,StringComparer.OrdinalIgnoreCase);
        var baseline=clean.Files.ToDictionary(x=>x.Path,x=>x,StringComparer.OrdinalIgnoreCase);
        var changed=target.Files.Where(x=>!baseline.ContainsKey(x.Path)||baseline[x.Path].Hash!=x.Hash).ToArray();
        if(changed.Any(x=>!x.Path.StartsWith("www/",StringComparison.OrdinalIgnoreCase) ||
                           !new[]{"audio","data","fonts","img","js","movies","css","icon"}.Contains(x.Path.Substring(4).Split('/')[0],StringComparer.OrdinalIgnoreCase)&&
                           !(x.Path.Substring(4).IndexOf('/')<0&&x.Path.EndsWith(".txt",StringComparison.OrdinalIgnoreCase))))
            throw new IOException("3.3.4 requires native changes outside the supported creator overlay directories.");
        var overlayFiles=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        var allOverlayPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(string file in Directory.GetFiles(web,"*",SearchOption.AllDirectories)){
            string relative=file.Substring(web.TrimEnd('\\','/').Length+1).Replace('\\','/');
            string top=relative.Split('/')[0];
            bool asset=new[]{"audio","data","fonts","img","js","movies","css","icon"}.Contains(top,StringComparer.OrdinalIgnoreCase);
            bool nativeNote=relative.IndexOf('/')<0&&relative.EndsWith(".txt",StringComparison.OrdinalIgnoreCase);
            if((!asset&&!nativeNote) ||
               relative.Split('/').Any(x=>x.Equals("save",StringComparison.OrdinalIgnoreCase))||
               Store.IsSave(file)||(File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Unexpected creator overlay file: "+relative);
            MixedNativeFile expected;
            if(!required.TryGetValue("www/"+relative,out expected) ||
               new FileInfo(file).Length!=expected.Size || Store.Hash(file)!=expected.Hash)
                throw new IOException("Creator hypnosis overlay differs from the trusted 3.3.4 target: "+relative);
            if(!allOverlayPaths.Add(relative))
                throw new IOException("Duplicate creator overlay path: "+relative);
            // VerifyBundle covers the eight native content directories; the
            // creator's seven root-level .txt files still receive exact target
            // hashes here and again in the complete installed-tree audit.
            if(asset)overlayFiles.Add(relative,expected.Hash);
        }
        var missing=changed.Where(x=>!allOverlayPaths.Contains(x.Path.Substring(4))).ToArray();
        if(missing.Length>0)
            throw new IOException("Hypnosis creator overlay is incomplete: "+missing.Length+" of "+
                changed.Length+" required changed/new files missing (e.g. "+
                string.Join(", ",missing.Take(5).Select(x=>x.Path).ToArray())+").");
        return overlayFiles;
    }
    static void VerifyStrict334Target(string root,MixedNativeReference reference){
        var result=MixedSourceRecovery.Probe(reference,new[]{root},CancellationToken.None);
        if(!result.Complete||result.RequiredCount!=9474||result.FoundCount!=9474)
            throw new IOException("Built 3.3.4 failed the exact 9,474-file native audit: "+
                result.FoundCount+"/"+result.RequiredCount+" SHA-256 matches. Missing "+
                result.Missing.Count+": "+string.Join(", ",result.Missing.Take(5).ToArray()));
        if(UniversalSupport.Identify(root)!="three-in-one-3-3-4")
            throw new IOException("Built 3.3.4 does not have the exact reference-version fingerprints.");
        var paths=new HashSet<string>(reference.Files.Select(x=>x.Path),StringComparer.OrdinalIgnoreCase);
        foreach(string file in Directory.GetFiles(root,"*",SearchOption.AllDirectories)){
            string relative=file.Substring(root.TrimEnd('\\','/').Length+1).Replace('\\','/');
            if(paths.Contains(relative)||relative.Equals("launcher-build.json",StringComparison.OrdinalIgnoreCase)||
               relative.Equals("launcher-base.json",StringComparison.OrdinalIgnoreCase)||
               relative.Equals("launcher-reconstruction.json",StringComparison.OrdinalIgnoreCase)||
               relative.StartsWith("www/save/",StringComparison.OrdinalIgnoreCase)&&Store.IsSave(file))continue;
            throw new IOException("Built hypnosis 3.3.4 contains an unrecognized extra file: "+relative);
        }
    }
    static Profile InstallStrict334Overlay(string baseRoot,string overlaySource,string location,ModRecipe recipe){
        if(recipe.Id!="three-in-one-3-3-4")throw new IOException("Incorrect creator-overlay target.");
        var target=MixedSourceRecovery.LoadEmbeddedReference(recipe.Id);
        var clean=MixedSourceRecovery.LoadEmbeddedReference("gorpg-1-23");
        var verifiedBase=MixedSourceRecovery.Probe(clean,new[]{baseRoot},CancellationToken.None);
        if(!verifiedBase.Complete)
            throw new IOException("Clean GoRPG source is incomplete: "+verifiedBase.FoundCount+"/"+verifiedBase.RequiredCount+
                " exact files. Add an intact original GoRPG archive or folder.");
        Directory.CreateDirectory(location);
        string token=Guid.NewGuid().ToString("N");
        string extracted=Path.Combine(location,".cph-334-overlay-"+token);
        string build=Path.Combine(location,".cph-334-build-"+token);
        try{
            string web=File.Exists(overlaySource)?Store.Extract(overlaySource,extracted,true):
                RecipeSupport.ContentRoot(overlaySource);
            var matching=RecipeSupport.Identify(web);
            if(matching==null||matching.Id!=recipe.Id)
                throw new IOException("Selected bundle is not the original 3.3.4 hypnosis creator overlay.");
            var trustedFiles=Validate334Overlay(web,target,clean);
            if(trustedFiles.Count==0)throw new IOException("Hypnosis creator overlay has no verified target files.");
            Profile built;
            // RecipeSupport's current 3.3.4 catalog has no complete Files ledger.
            // Supply only the independently verified exact target hashes for this
            // installation invocation; restore the shared recipe in all outcomes.
            lock(recipe){
                var previous=recipe.Files;
                try{
                    recipe.Files=trustedFiles;
                    built=RecipeSupport.Install(baseRoot,build,recipe.Name,
                        new List<LayerInput>{new LayerInput{Source=web}});
                }finally{recipe.Files=previous;}
            }
            VerifyStrict334Target(built.Root,target);
            string destination=Path.Combine(location,"Layers-334-"+Guid.NewGuid().ToString("N").Substring(0,8));
            if(Directory.Exists(destination)||File.Exists(destination))
                throw new IOException("The installation destination already exists.");
            Directory.Move(built.Root,destination);
            var game=Store.Detect(destination,recipe.Name);
            game.Note="Verified hypnosis 3.3.4: 9,474 native SHA-256 files matched; independent installation and saves";
            return game;
        }finally{
            // Remove only uniquely named scratch folders created by this invocation.
            foreach(string path in new[]{extracted,build}){
                if(Path.GetDirectoryName(Path.GetFullPath(path)).TrimEnd('\\')==
                   Path.GetFullPath(location).TrimEnd('\\') &&
                   Path.GetFileName(path).Contains(token) && Directory.Exists(path))
                    Directory.Delete(path,true);
            }
        }
    }
    async Task Install334(ModRecipe recipe,string location){
        var already=Installed(recipe.Id);
        if(already!=null){settings.SelectedId=already.Id;RefreshProfiles();ShowPage("Play");return;}
        string[] targetSources=NativeRecoveryInputs(recipe.Id);
        string chosen=null;
        if(targetSources.Length==0){
            chosen=PickBundle(recipe);
            if(chosen==null)return;
            targetSources=NativeRecoveryInputs(recipe.Id,chosen);
        }
        MixedRecoveryReport exact=null;
        if(targetSources.Length>0){
            status.Text="Checking complete original 3.3.4 files against all 9,474 expected hashes…";
            exact=await Task.Run(()=>MixedSourceRecovery.Probe(
                MixedSourceRecovery.LoadEmbeddedReference(recipe.Id),targetSources,CancellationToken.None));
            if(exact.Complete){
                await RunInstall(()=>InstallRecoveredVersion(recipe.Id,exact,location));return;
            }
        }
        var detected=Found(recipe.Id,"Extracted bundle")??Found(recipe.Id,"Original archive");
        string overlay=chosen??(detected==null?PickBundle(recipe):detected.Path);
        if(overlay==null)return;
        var clean=Clean();
        if(clean!=null){
            bool complete=await Task.Run(()=>MixedSourceRecovery.Probe(
                MixedSourceRecovery.LoadEmbeddedReference("gorpg-1-23"),
                new[]{clean.Root},CancellationToken.None).Complete);
            if(complete){
                await RunInstall(()=>InstallStrict334Overlay(clean.Root,overlay,location,recipe));return;
            }
        }
        string[] baseSources=NativeRecoveryInputs("gorpg-1-23");
        if(baseSources.Length==0){
            string baseArchive=PickArchive("Choose an original GoRPG 1.23 archive (.001 for split archives), or select folders through Combine original sources",
                "Game archives|*.zip;*.rar;*.7z;*.001|All files|*.*",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"),null);
            if(baseArchive==null)return;
            baseSources=NativeRecoveryInputs("gorpg-1-23",baseArchive);
        }
        string[] verifiedSources=baseSources;
        await RunInstall(()=>{
            var baseReport=VerifiedNativeSources("gorpg-1-23",verifiedSources);
            Directory.CreateDirectory(location);
            string tempBase=Path.Combine(location,".cph-334-base-"+Guid.NewGuid().ToString("N"));
            try{
                MixedSourceRecovery.Recover(baseReport,tempBase,CancellationToken.None);
                return InstallStrict334Overlay(tempBase,overlay,location,recipe);
            }finally{
                if(Path.GetDirectoryName(Path.GetFullPath(tempBase)).TrimEnd('\\')==
                    Path.GetFullPath(location).TrimEnd('\\') &&
                   Path.GetFileName(tempBase).StartsWith(".cph-334-base-",StringComparison.Ordinal) &&
                   Directory.Exists(tempBase))Directory.Delete(tempBase,true);
            }
        });
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
            if(route==1&&recipe.Id=="three-in-one-3-3-4"){
                await Install334(recipe,location);return;
            }
            if(UpgradeSupport.IsUpgrade(recipe.Id)&&HasNativeInventory(recipe.Id)&&route!=2&&(route==1||originalInputs.Count>0)){
                var already=Installed(recipe.Id);
                if(already!=null){settings.SelectedId=already.Id;RefreshProfiles();ShowPage("Play");return;}
                status.Text="Checking source folders for "+recipe.Name+" before installing…";
                var sourceSettings=new Settings{Profiles=LibraryProfiles.ToList(),InstallRoot=settings.InstallRoot,
                    SearchRoots=settings.SearchRoots==null?null:settings.SearchRoots.ToList()};
                var targetSources=await Task.Run(()=>FileDiscovery.FindTargetArchives(FileDiscovery.NativeRoots(sourceSettings),recipe.Id,CancellationToken.None));
                discovered=targetSources.Concat(discovered).GroupBy(x=>x.Path+"|"+x.Kind+"|"+x.RecipeId,StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).ToList();
                string[] sources=NativeRecoveryInputs(recipe.Id);
                if(sources.Length==0){var first=PickBundle(recipe);if(first==null)return;sources=NativeRecoveryInputs(recipe.Id,first);}
                bool english=englishOnInstall.Checked;
                Profile carry=startingSource!=null&&UpgradeSupport.MayUpgrade(UniversalSupport.Identify(startingSource.Root),recipe.Id)
                    ?startingSource:UpgradeSupport.StartingProfile(settings,recipe.Id);
                var verifiedSource=carry;
                await RunInstall(()=>{var game=UpgradeSupport.InstallFromOriginalFiles(sources,location,recipe.Id,verifiedSource,CancellationToken.None);
                    SetInstalledEnglish(game,english);return game;});return;
            }
            if(route==1&&HasNativeInventory(recipe.Id)){
                var already=Installed(recipe.Id);
                if(already!=null){settings.SelectedId=already.Id;RefreshProfiles();ShowPage("Play");return;}
                string[] native=NativeRecoveryInputs(recipe.Id);
                if(native.Length==0){
                    string first=PickArchive("Choose original "+recipe.Name+" archive (.001 for split files), or use Combine original sources for folders",
                        "Game archives|*.zip;*.rar;*.7z;*.001|All files|*.*",
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"),null);
                    if(first==null)return;
                    native=NativeRecoveryInputs(recipe.Id,first);
                }
                await RunInstall(()=>InstallRecoveredVersion(recipe.Id,native,location));return;
            }
            if(UpgradeSupport.IsUpgrade(recipe.Id)&&route!=2){
                var already=Installed(recipe.Id);if(already!=null){settings.SelectedId=already.Id;RefreshProfiles();ShowPage("Play");return;}
                bool english=englishOnInstall.Checked;
                var full=Found(recipe.Id,"Full game archive");
                var detected=Found(recipe.Id,"Extracted bundle")??Found(recipe.Id,"Original archive");
                string update=detected==null?PickBundle(recipe):detected.Path;if(update==null)return;
                var candidatePaths=originalInputs.Concat(discovered.Where(x=>x.Kind=="Game archive candidate"&&
                    string.IsNullOrEmpty(x.Problem)&&UpgradeSupport.MayUpgrade(x.RecipeId,recipe.Id)).Select(x=>x.Path))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                status.Text="Checking installed versions and original archives for a compatible upgrade source…";
                var automatic=await Task.Run(()=>UpgradeSupport.BestCompatibleSource(settings,recipe.Id,candidatePaths,CancellationToken.None));
                var explicitSource=startingSource;
                bool explicitlyChosen=explicitSource!=null&&UpgradeSupport.MayUpgrade(UniversalSupport.Identify(explicitSource.Root),recipe.Id);
                var source=explicitlyChosen?explicitSource:automatic!=null&&automatic.Kind!="Full game archive"?automatic.Profile:null;
                bool fresh=source==null&&(automatic==null||automatic.Kind!="Full game archive");
                if(automatic!=null&&automatic.Kind=="Full game archive"&&!explicitlyChosen){
                    string archive=automatic.Path;
                    status.Text="Found compatible "+automatic.VersionId+" archive; complete source hashes will be checked before saves are copied.";
                    await RunInstall(()=>{var game=UpgradeSupport.InstallFromGameArchive(archive,update,location,recipe.Id,true);
                        SetInstalledEnglish(game,english);return game;});return;
                }
                if(fresh&&full!=null){
                    await RunInstall(()=>{var game=UniversalSupport.InstallOriginal(full.Path,location,recipe.Id);SetInstalledEnglish(game,english);return game;});return;
                }
                if(!fresh){
                    status.Text="Using compatible "+UniversalSupport.Identify(source.Root)+" installation and backing up its saves before upgrading…";
                    await RunInstall(()=>{var game=UpgradeSupport.Install(source,update,location,recipe.Id,true);SetInstalledEnglish(game,english);return game;});return;
                }
                var baseGame=Clean();var originalBase=Original("gorpg-1-23");
                var packedBase=AnyPack("gorpg-1-23");
                var starting=startingSource??LibraryProfiles.FirstOrDefault(x=>UniversalSupport.Identify(x.Root)!=null&&UniversalSupport.Identify(x.Root)!="gorpg-1-23"&&UniversalSupport.Identify(x.Root)!=recipe.Id);
                string[] nativeInputs=NativeRecoveryInputs("gorpg-1-23");
                if(baseGame==null&&nativeInputs.Length>0){
                    await RunInstall(()=>{var game=UpgradeSupport.InstallFromMixedGoRpg(nativeInputs,update,location,recipe.Id,CancellationToken.None);
                        SetInstalledEnglish(game,english);return game;});return;
                }
                // If the only game on hand is v1.03/v1.04/v1.051, a
                // direct forward upgrade is possible without GoRPG rebuilding.
                var oldArchive=new[]{"three-in-one-3-3-5-v1-052","three-in-one-3-3-5-v1-051","three-in-one-3-3-5-v1-04","three-in-one-3-3-5-v1-03","three-in-one-3-3-5-v1-02","three-in-one-3-3-5-v1-01"}
                    .Select(id=>Found(id,"Full game archive")).FirstOrDefault(x=>x!=null);
                if(baseGame==null&&originalBase==null&&oldArchive!=null&&route==0){
                    await RunInstall(()=>{var old=UniversalSupport.InstallOriginal(oldArchive.Path,location,oldArchive.RecipeId);
                        var game=UpgradeSupport.Install(old,update,location,recipe.Id,true);SetInstalledEnglish(game,english);return game;});return;
                }
                string cleanArchive=null;
                if(baseGame==null&&originalBase==null&&(packedBase==null||starting==null)){
                    cleanArchive=PickArchive("Choose clean GoRPG game ZIP, RAR, split 7z, or extracted folder via Starting version...",
                        "Game archives|*.zip;*.7z;*.001;*.rar|All files|*.*",
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"),null);
                    if(cleanArchive==null)return;
                }
                string selectedArchive=cleanArchive;
                if(baseGame==null&&originalBase==null&&selectedArchive!=null){
                    string[] manualNative=NativeRecoveryInputs("gorpg-1-23",selectedArchive);
                    await RunInstall(()=>{var game=UpgradeSupport.InstallFromMixedGoRpg(manualNative,update,location,recipe.Id,CancellationToken.None);
                        SetInstalledEnglish(game,english);return game;});return;
                }
                await RunInstall(()=>{
                    Profile cleanBase=baseGame;
                    if(cleanBase==null){
                        if(originalBase!=null||selectedArchive!=null){
                            string original=originalBase!=null?originalBase.Path:selectedArchive;
                            cleanBase=Directory.Exists(original)?Store.Detect(UniversalSupport.FindGameRoot(original),"GoRPG 1.23 - base")
                                :UniversalSupport.InstallOriginal(original,location,"gorpg-1-23");
                        }else if(packedBase!=null&&starting!=null&&UniversalSupport.IsPack(packedBase.Path))
                            cleanBase=UniversalSupport.Install(packedBase.Path,starting.Root,location,"gorpg-1-23");
                        else throw new IOException("A creator bundle is a mod overlay, not the full game. Supply a clean GoRPG base, or CH/Maple plus a verified any-version reconstruction pack.");
                    }
                    RecipeSupport.ValidateBase(cleanBase.Root);
                    var game=RecipeSupport.Install(cleanBase.Root,location,recipe.Name,new List<LayerInput>{new LayerInput{Source=update}});
                    if(UniversalSupport.Identify(game.Root)!=recipe.Id)throw new IOException("Fresh install failed version verification.");
                    SetInstalledEnglish(game,english);RecipeSupport.VerifyProfile(game);
                    game.Note="Independent verified "+recipe.Name+"; "+(english?"English enabled":"native language")+"; empty saves";
                    return game;
                });return;
            }
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
        }catch(Exception e){status.Text="Could not prepare "+recipe.Name+": "+e.Message;
            MessageBox.Show(this,e.Message+"\r\n\r\n"+InstallErrorAdvice(e),"Install version",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{busy=false;if(!IsDisposed){SetEnabled(true);UpdateActions();}}
    }
    static void SetInstalledEnglish(Profile game,bool enabled){
        var status=EnglishSupport.Status(game);
        if(!status.Supported)return;
        if(enabled){if(!status.Enabled||status.UpdateAvailable)EnglishSupport.Apply(game);}
        else if(status.Enabled)EnglishSupport.Restore(game);
    }
    async Task RunInstall(Func<Profile> job){
        busy=true;SetEnabled(false);progress.Visible=true;status.Text="Checking original sources, preparing an independent game, and verifying file hashes. Large archives can take time.";UseWaitCursor=true;
        try{var p=await Task.Run(job);
            string record=Path.Combine(p.Root,"launcher-build.json");
            if(File.Exists(record)){
                var layer=Store.Json.Deserialize<LayerState>(File.ReadAllText(record,System.Text.Encoding.UTF8));
                if(layer!=null&&Directory.Exists(layer.BaseRoot)&&RecipeSupport.IsCleanBase(layer.BaseRoot)&&!settings.Profiles.Any(x=>Path.GetFullPath(x.Root).Equals(Path.GetFullPath(layer.BaseRoot),StringComparison.OrdinalIgnoreCase))){
                    var baseProfile=Store.Detect(layer.BaseRoot,"GoRPG 1.23 - base");baseProfile.Note="Clean GoRPG source; original files preserved";settings.Profiles.Add(baseProfile);
                }
            }
            Added(p);MessageBox.Show(this,"Installed "+p.Name+". It has its own save folder; all previous versions remain in your library.","Version ready");}
        catch(Exception e){status.Text="Installation stopped: "+e.Message;
            MessageBox.Show(this,e.Message+"\r\n\r\n"+InstallErrorAdvice(e),
                "Installation could not finish",MessageBoxButtons.OK,MessageBoxIcon.Error);}
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
            StyleDialog(dialog);if(dialog.ShowDialog(this)!=DialogResult.OK || list.Items.Count==0)return;var layers=list.Items.Cast<LayerInput>().ToList();string location=settings.InstallRoot;await RunInstall(()=>RecipeSupport.Install(clean.Root,location,"Custom layered version",layers));
        }
    }
    Profile ChooseProfile(string title,IEnumerable<Profile> options){using(var dialog=new Form{Text=title,ClientSize=new Size(595,145),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){var box=new ComboBox{Location=new Point(18,24),Size=new Size(559,31),DropDownStyle=ComboBoxStyle.DropDownList};box.Items.AddRange(options.ToArray());if(box.Items.Count>0)box.SelectedIndex=0;dialog.Controls.Add(box);var ok=new Button{Text="Select",Location=new Point(429,87),Size=new Size(148,35),DialogResult=DialogResult.OK};dialog.Controls.Add(ok);dialog.AcceptButton=ok;StyleDialog(dialog);return dialog.ShowDialog(this)==DialogResult.OK?box.SelectedItem as Profile:null;}}
    void BackupNow(){
        if(Selected==null||!GameClosed())return;
        var game=Selected;
        status.Text="Creating a timestamped backup of "+game.Name+" saves…";
        string folder=Store.Backup(game);
        status.Text="Backup created for "+game.Name+": "+folder;
        UpdateProfile();
        if(saveGuide!=null)saveGuide.Text+="\r\nLatest backup: "+folder;
    }
    void CopySaves(){
        if(Selected==null||!GameClosed())return;
        var destination=Selected;
        var eligible=LibraryProfiles.Where(x=>x.Id!=destination.Id&&UpgradeSupport.CanTransfer(UniversalSupport.Identify(x.Root),UniversalSupport.Identify(destination.Root))).ToArray();
        if(eligible.Length==0){MessageBox.Show(this,"No verified compatible version is registered. Add the source version first, then select Copy again.","No compatible save source");return;}
        var source=ChooseProfile("Select the source of saves to copy",eligible);if(source==null)return;
        if(MessageBox.Show(this,"COPY SAVES\r\nFrom: "+source.Name+"\r\nTo: "+destination.Name+
            "\r\n\r\nThe destination is backed up before replacement; source saves stay in place.\r\n\r\n"+
            RecipeSupport.SaveWarning(source,destination),"Confirm destination and source",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        status.Text="Backing up "+destination.Name+" and copying compatible saves from "+source.Name+"…";
        Store.ReplaceSaves(source.SavePath,destination);UpdateProfile();status.Text="Copied saves to "+destination.Name+". Source and destination backup preserved.";
    }
    void Restore(){
        if(Selected==null||!GameClosed())return;
        var game=Selected;string parent=Path.Combine(Store.Home,"Save Backups",game.Id);
        Directory.CreateDirectory(parent);
        string folder=Folder("Choose a backup folder for "+game.Name+" containing manifest.json.",parent);
        if(folder==null)return;
        status.Text="Checking backup integrity for "+game.Name+"…";
        Store.VerifyBackup(folder);
        if(MessageBox.Show(this,"RESTORE SAVES\r\nDestination: "+game.Name+"\r\nBackup: "+folder+
            "\r\n\r\nCurrent destination saves will first be backed up. Restore then replaces the destination saves.","Confirm save restore",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        status.Text="Backing up existing saves and restoring "+game.Name+"…";
        Store.ReplaceSaves(folder,game,true);UpdateProfile();status.Text="Backup restored to "+game.Name+"; previous destination saves backed up.";
    }
    static string ReadableBytes(long bytes){
        if(bytes>=1024L*1024*1024)return (bytes/(1024.0*1024*1024)).ToString("N2")+" GB";
        if(bytes>=1024L*1024)return (bytes/(1024.0*1024)).ToString("N1")+" MB";
        return bytes.ToString("N0")+" bytes";
    }
    async void RemoveVersion(Profile game,RemovalMode? requested=null){
        if(busy||game==null)return;
        RemovalMode mode;
        if(requested.HasValue)mode=requested.Value;
        else using(var picker=new Form{Text="Remove a registered game · choose what stays",ClientSize=new Size(790,309),
            Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,
            MinimizeBox=false,MaximizeBox=false}){
            LabelAt(picker,game.Name,18,15,755,31,14,true,Color.Black);
            LabelAt(picker,"Choose an action. Each choice shows a review and requires confirmation before changes are made.",18,50,754,41,10,false,Color.DimGray);
            LabelAt(picker,"Remove from launcher only\r\nForget the registration. The entire game folder, saves, backups and archives stay on disk.",18,98,753,58,10,false,Color.Black);
            LabelAt(picker,"Uninstall verified managed game files\r\nRemove only exact SHA-256 verified launcher-managed game files; keep saves, archives and unknown files.",18,166,753,60,10,false,Color.Black);
            var indexOnly=new Button{Text="Remove registration",Location=new Point(18,253),Size=new Size(223,39),DialogResult=DialogResult.Yes};
            var managed=new Button{Text="Review file uninstall",Location=new Point(258,253),Size=new Size(250,39),DialogResult=DialogResult.No};
            var cancel=new Button{Text="Cancel",Location=new Point(647,253),Size=new Size(125,39),DialogResult=DialogResult.Cancel};
            picker.Controls.Add(indexOnly);picker.Controls.Add(managed);picker.Controls.Add(cancel);picker.CancelButton=cancel;
            StyleDialog(picker);var decision=picker.ShowDialog(this);
            if(decision!=DialogResult.Yes&&decision!=DialogResult.No)return;
            mode=decision==DialogResult.Yes?RemovalMode.IndexOnly:RemovalMode.InstalledGame;
        }
        if(mode==RemovalMode.InstalledGame&&!GameClosed())return;
        RemovalPlan plan;
        busy=true;SetEnabled(false);progress.Visible=true;UseWaitCursor=true;
        status.Text=mode==RemovalMode.IndexOnly?"Checking the saved launcher registration…":
            "Auditing managed game files and their SHA-256 hashes. Archives and saves are excluded…";
        try{plan=await Task.Run(()=>InstallationRemoval.Plan(game,settings,mode));}
        catch(Exception e){
            status.Text="Could not prepare removal for "+game.Name+": "+e.Message;
            MessageBox.Show(this,e.Message,"Removal preview failed",MessageBoxButtons.OK,MessageBoxIcon.Error);return;
        }
        finally{busy=false;UseWaitCursor=false;progress.Visible=false;SetEnabled(true);UpdateActions();}
        if(mode==RemovalMode.InstalledGame&&!plan.CanDeleteFiles){
            status.Text="File uninstall unavailable for "+game.Name+". The registration may still be removed.";
            MessageBox.Show(this,plan.Reason+"\r\n\r\nChoose Remove from launcher only to forget this entry while keeping its files.",
                "No verified managed files to uninstall",MessageBoxButtons.OK,MessageBoxIcon.Information);return;
        }
        string summary=mode==RemovalMode.IndexOnly?
            "REMOVE REGISTRATION ONLY\r\nNo game files, saves, backups or archives will be removed.":
            "UNINSTALL VERIFIED FILES\r\n"+plan.FileCount.ToString("N0")+" verified game files ("+ReadableBytes(plan.FileBytes)+") will be removed."+
            "\r\n"+plan.RetainedCount.ToString("N0")+" unrecognized/protected files ("+ReadableBytes(plan.RetainedBytes)+") stay, including "+
            plan.PreservedSaves+" saves, "+plan.PreservedBackups+" backups and "+plan.PreservedArchives+" archives.";
        string preview="Game: "+game.Name+"\r\nFolder: "+plan.Root+"\r\n\r\n"+summary+
            "\r\n\r\n"+plan.Message+
            (mode==RemovalMode.InstalledGame&&plan.RetainedSample.Count>0?
                "\r\n\r\nExamples preserved: "+string.Join(", ",plan.RetainedSample.Take(3).ToArray()):"")+
            "\r\n\r\nConfirm this exact action?";
        if(MessageBox.Show(this,preview,"Review removal · confirmation required",MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes){
            status.Text="Removal canceled. "+game.Name+" is unchanged.";return;
        }
        busy=true;SetEnabled(false);progress.Visible=true;UseWaitCursor=true;
        status.Text=mode==RemovalMode.IndexOnly?"Updating the launcher library; game files are staying in place…":
            "Rechecking managed file hashes, then removing only verified game files…";
        try{
            var result=await Task.Run(()=>InstallationRemoval.Execute(plan));
            if(startingSource!=null&&startingSource.Root.Equals(game.Root,StringComparison.OrdinalIgnoreCase))startingSource=null;
            RefreshProfiles();UpdateAvailability();UpdateInstall();
            status.Text=result.Message;
            MessageBox.Show(this,result.Message+"\r\n\r\nFolder: "+result.Root+
                (mode==RemovalMode.InstalledGame?"\r\nVerified files removed: "+result.DeletedFiles.ToString("N0")+
                "\r\nUnmatched/protected files retained: "+result.RetainedFiles.ToString("N0"):""),
                "Removal completed",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }catch(Exception e){
            status.Text="Removal stopped for "+game.Name+": "+e.Message;
            MessageBox.Show(this,e.Message+"\r\n\r\nRefresh the library and review this game's current folder before trying again.",
                "Removal stopped",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }finally{busy=false;UseWaitCursor=false;progress.Visible=false;SetEnabled(true);UpdateActions();}
    }
    public void PreviewPage(string page){discovered=FileDiscovery.Scan(settings);discoveryStatus.Text="Found "+discovered.Count+" sources · launcher folders, Documents, Desktop, Downloads + browser folders";foreach(var p in LibraryProfiles)statusCache[p.Id]=EnglishSupport.Status(p);if(page=="English")profiles.SelectedItem=Installed(UpgradeSupport.Latest)??LibraryProfiles.FirstOrDefault(x=>x.Name.Contains("3.3.5"))??Selected;if(Selected!=null)englishCurrent=statusCache[Selected.Id];ShowPage(page);}
    public void OpenEnglishPage(){ShowPage("English");}
    public void TestPage(string page){ShowPage(page);}
    void BuildFiles(){
        filesPage=Page();LabelAt(filesPage,"VERSIONS",0,0,760,24,10,true,muted);
        LabelAt(filesPage,"Your games and sources",0,32,800,46,24,true);
        LabelAt(filesPage,"Choose an installed game to play or manage; choose an uninstalled version to prepare it.",0,88,800,32,10,false,muted);
        availability=new DataGridView{Location=new Point(0,135),Size=new Size(812,257),ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=card,BorderStyle=BorderStyle.None,EnableHeadersVisualStyles=false,RowTemplate={Height=48},AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None};
        availability.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(41,47,64);availability.ColumnHeadersDefaultCellStyle.ForeColor=ForeColor;availability.ColumnHeadersHeight=43;
        availability.DefaultCellStyle.BackColor=card;availability.DefaultCellStyle.ForeColor=ForeColor;availability.DefaultCellStyle.SelectionBackColor=Color.FromArgb(68,58,103);availability.DefaultCellStyle.WrapMode=DataGridViewTriState.True;availability.GridColor=Color.FromArgb(48,56,73);
        availability.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;availability.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;availability.DefaultCellStyle.Padding=new Padding(7,0,4,0);availability.DefaultCellStyle.SelectionForeColor=ForeColor;availability.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(28,34,47);
        availability.Columns.Add("Version","Version");availability.Columns[0].Width=250;
        availability.Columns.Add("Game","Installation");availability.Columns[1].Width=165;
        availability.Columns.Add("Original","Local sources");availability.Columns[2].Width=180;
        availability.Columns.Add("Ready","Next action");availability.Columns[3].Width=195;
        availability.SelectionChanged+=delegate{Guard(UpdateFilesSelection);};
        availability.CellDoubleClick+=delegate{UseAvailable();};filesPage.Controls.Add(availability);
        availability.CellMouseDown+=delegate(object sender,DataGridViewCellMouseEventArgs e){
            if(e.Button!=MouseButtons.Right||e.RowIndex<0)return;availability.ClearSelection();availability.Rows[e.RowIndex].Selected=true;
            string id=Convert.ToString(availability.Rows[e.RowIndex].Tag);var game=Installed(id);
            if(game!=null)ShowManageMenu(availability,game,availability.PointToClient(Cursor.Position));
            else ShowInstallMenu(availability,targets.Items.Cast<ModRecipe>().FirstOrDefault(x=>x.Id==id),availability.PointToClient(Cursor.Position));
        };
        availabilityNote=LabelAt(filesPage,"Select a version to inspect.",0,407,812,77,10,false,muted);
        ButtonAt(filesPage,"Open selected / Install",0,509,304,46,()=>UseAvailable(),true);
        filesManageButton=ButtonAt(filesPage,"Manage / Uninstall  ▾",321,509,279,46,()=>ManageSelectedInFiles(),false);
        ButtonAt(filesPage,"Find source files",616,509,196,46,()=>ShowFoundFiles(),false);
        filesGuide=LabelAt(filesPage,"Original downloads and personal saves remain untouched by library removal.",0,565,812,20,9,false,muted);
        filesGuide.Visible=false;
        ButtonAt(filesPage,"Add source files...",0,571,290,39,()=>Guard(ChooseSourcesForSelectedVersion),false);
        ButtonAt(filesPage,"Search another location",307,571,274,39,()=>AddSearchFolder(),false);
    }
    void ManageSelectedInFiles(){
        if(availability.SelectedRows.Count==0)return;
        string id=Convert.ToString(availability.SelectedRows[0].Tag);var game=Installed(id);
        if(game==null){
            MessageBox.Show(this,"This version is not installed. Choose Open selected / Install to prepare it.","Not installed");
            return;
        }
        ShowManageMenu(filesManageButton,game);
    }
    void UpdateFilesSelection(){
        if(availabilityNote==null||availability.SelectedRows.Count==0)return;
        string id=Convert.ToString(availability.SelectedRows[0].Tag);
        if(string.IsNullOrEmpty(id))return;
        var installed=Installed(id);
        var recipe=targets.Items.Cast<ModRecipe>().FirstOrDefault(x=>x.Id==id);
        var original=LocatedOriginal(id);
        var invalid=discovered.Where(x=>x.RecipeId==id&&!string.IsNullOrEmpty(x.Problem)).ToArray();
        if(installed!=null){
            availabilityNote.Text="READY · "+installed.Name+" is registered.\r\nGame: "+installed.Root+
                "\r\nNext: Open selected version to play with its own saves.";
        }else if(original!=null){
            availabilityNote.Text=(original.Kind=="Game archive candidate"?"CANDIDATE · Version markers match; remaining files still need SHA verification.":
                "SOURCE FOUND · "+original.Kind+". Installation will validate the files.")+
                "\r\n"+original.Path+"\r\nNext: Open selected version to check the installation steps.";
        }else if(invalid.Length>0){
            availabilityNote.Text="INCOMPLETE · "+invalid[0].Problem+
                "\r\nNext: Inspect found sources, provide the missing volume/files, then install.";
        }else{
            availabilityNote.Text="NOT FOUND · No local source has been identified for "+(recipe==null?id:recipe.Name)+
                ".\r\nNext: Search another folder or combine original archives and extracted files.";
        }
        if(filesGuide!=null)filesGuide.Text=installed!=null?
            "2 · This version is ready. Select Saves to back up or inspect its progress.":
            "2 · You can add multiple Telegram downloads, ZIP/RAR/7z archives, or folders to fill missing original bytes.";
    }
    void ChooseSourcesForSelectedVersion(){
        if(availability.SelectedRows.Count==0)return;
        string id=Convert.ToString(availability.SelectedRows[0].Tag);
        var target=targets.Items.Cast<ModRecipe>().FirstOrDefault(x=>x.Id==id);
        if(target==null)return;
        targets.SelectedItem=target;methods.SelectedIndex=1;ShowPage("Install");ChooseOriginalInputs();
    }
    void UpdateAvailability(){
        if(availability==null||targets==null)return;string prior=availability.SelectedRows.Count>0?Convert.ToString(availability.SelectedRows[0].Tag):null;availability.Rows.Clear();foreach(var recipe in targets.Items.Cast<ModRecipe>()){
            var game=Installed(recipe.Id);var original=LocatedOriginal(recipe.Id);var pack=AnyPack(recipe.Id)??Found(recipe.Id,"Delta pack");
            bool ready=game!=null,canInstall=UpgradeSupport.IsUpgrade(recipe.Id)?original!=null&&(UpgradeSupport.StartingProfile(settings,recipe.Id)!=null||Clean()!=null||Original("gorpg-1-23")!=null):pack!=null&&(pack.Kind=="Any-version pack"?SupportedSource(recipe.Id)!=null:CH()!=null)||original!=null&&original.Kind!="Game archive candidate"&&(recipe.Standalone||original.Kind=="Full game archive"||Clean()!=null);
            EnglishStatus cached;string textStatus=game!=null?(statusCache.TryGetValue(game.Id,out cached)?cached.Short:"Select to check English"):recipe.Id=="ch-current"?"English reference":"Install game first";
            string source=original==null?(pack==null?"Not found":"Conversion pack"):original.Kind=="Game archive candidate"?"Unverified archive":original.Kind=="Extracted bundle"?"Extracted folder":"Original files found";
            int row=availability.Rows.Add(recipe.Name,ready?"Installed · "+CountSaves(game)+" saves":"Not installed",source,ready?"Play / Manage":canInstall?"Install now":original!=null&&original.Kind=="Game archive candidate"?"Verify sources":"Add original files");availability.Rows[row].Tag=recipe.Id;
            availability.Rows[row].DefaultCellStyle.ForeColor=settings.LauncherTheme=="Daylight"?(ready?Color.FromArgb(21,112,76):canInstall?Color.FromArgb(105,62,154):Color.FromArgb(130,87,28)):ready?Color.FromArgb(141,220,188):canInstall?Color.FromArgb(208,194,249):Color.FromArgb(229,192,136);
        }
        var selectedRow=availability.Rows.Cast<DataGridViewRow>().FirstOrDefault(x=>Convert.ToString(x.Tag)==prior)??availability.Rows.Cast<DataGridViewRow>().FirstOrDefault();if(selectedRow!=null){availability.ClearSelection();selectedRow.Selected=true;}
        UpdateFilesSelection();
    }
    void UseAvailable(){Guard(()=>{if(availability.SelectedRows.Count==0)return;string id=Convert.ToString(availability.SelectedRows[0].Tag);var game=Installed(id);if(game!=null){settings.SelectedId=game.Id;RefreshProfiles();ShowPage("Play");return;}targets.SelectedItem=targets.Items.Cast<ModRecipe>().First(x=>x.Id==id);methods.SelectedIndex=1;ShowPage("Install");});}
    async void AddSearchFolder(){string folder=Folder("Choose another folder to search for games, archives, and patches.",settings.InstallRoot);if(folder==null)return;if(settings.SearchRoots==null)settings.SearchRoots=new List<string>();if(!settings.SearchRoots.Contains(folder))settings.SearchRoots.Add(folder);Store.Write(settings);await Discover();UpdateAvailability();}
    async void ChooseStartingVersion(){
        if(busy||!GameClosed())return;
        try{using(var dialog=new Form{Text="Choose your starting installation",ClientSize=new Size(645,214),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"CH, Gorpg 1.23, Maple 1.7.2, 3.3.4, or 3.3.5 can be the starting game.",18,17,610,46,10,false,Color.Black);
            var list=new ComboBox{Location=new Point(18,72),Size=new Size(610,32),DropDownStyle=ComboBoxStyle.DropDownList,ForeColor=Color.Black};list.Items.AddRange(LibraryProfiles.Where(x=>UniversalSupport.Identify(x.Root)!=null).ToArray());if(list.Items.Count>0)list.SelectedIndex=0;dialog.Controls.Add(list);
            var use=new Button{Text="Use selected",Location=new Point(18,137),Size=new Size(188,40),DialogResult=DialogResult.OK};var folder=new Button{Text="Choose game folder",Location=new Point(223,137),Size=new Size(198,40),DialogResult=DialogResult.Yes};var archive=new Button{Text="Choose full archive",Location=new Point(438,137),Size=new Size(190,40),DialogResult=DialogResult.No};dialog.Controls.Add(use);dialog.Controls.Add(folder);dialog.Controls.Add(archive);
            StyleDialog(dialog);var result=dialog.ShowDialog(this);if(result==DialogResult.OK){startingSource=list.SelectedItem as Profile;UpdateInstall();return;}
            if(result==DialogResult.Yes){string root=Folder("Choose an intact complete game folder or its wrapper folder.",settings.InstallRoot);if(root==null)return;root=UniversalSupport.FindGameRoot(root);if(UniversalSupport.Identify(root)==null)throw new IOException("This folder does not match a supported reference version.");startingSource=Store.Detect(root,UniversalSupport.DetectedName(root));if(!LibraryProfiles.Any(x=>x.Root.Equals(root,StringComparison.OrdinalIgnoreCase)))Added(startingSource);ShowPage("Install");UpdateInstall();}
            else if(result==DialogResult.No){
                string file=PickArchive("Choose an original full game archive (.001 for split files)","Game archives|*.zip;*.rar;*.7z;*.001;*.7删z|All files|*.*",settings.InstallRoot,null);if(file==null)return;
                var discoveredVersions=FileDiscovery.ArchiveFormat(file)=="zip"?FileDiscovery.ProbeZip(file):FileDiscovery.ProbeRar(file,CancellationToken.None);
                var versionIds=discoveredVersions.Where(x=>x.Kind=="Game archive candidate"&&HasNativeInventory(x.RecipeId))
                    .Select(x=>x.RecipeId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if(versionIds.Length!=1)throw new IOException("This archive has no uniquely verified complete-game reference. Choose an extracted installed game folder, or add original sources using Combine original sources.");
                string id=versionIds[0];string[] sources=NativeRecoveryInputs(id,file);
                busy=true;SetEnabled(false);progress.Visible=true;
                try{startingSource=await Task.Run(()=>InstallRecoveredVersion(id,sources,settings.InstallRoot));Added(startingSource);ShowPage("Install");UpdateInstall();}
                finally{busy=false;progress.Visible=false;SetEnabled(true);UpdateActions();}
            }
        }}catch(Exception e){MessageBox.Show(this,e.Message,"Starting version",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    void ShowFoundFiles(){
        using(var dialog=new Form{Text="Detected sources · inspect before installing",ClientSize=new Size(970,573),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
            LabelAt(dialog,"Sources from launcher folders, Downloads and search folders. A candidate needs complete-file verification; an incomplete archive needs its missing parts.",18,12,932,48,10,false,Color.DimGray);
            var grid=new DataGridView{Location=new Point(18,66),Size=new Size(934,305),ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowHeadersVisible=false,BackgroundColor=Color.White,ForeColor=Color.Black};
            grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Version",HeaderText="Version / bundle",Width=280});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Kind",HeaderText="File type",Width=166});grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Status",HeaderText="Detection / status",Width=345});
            grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Size",HeaderText="Size",Width=110});
            foreach(var source in discovered.OrderBy(x=>string.IsNullOrEmpty(x.Problem)?0:1).ThenBy(x=>x.Name)){
                bool invalid=!string.IsNullOrEmpty(source.Problem);
                int i=grid.Rows.Add(source.Name,invalid?"Incomplete · "+source.Kind:source.Kind,
                    invalid?source.Problem:source.Kind=="Game archive candidate"?"Version markers match · full file verification pending":source.Detection??"Detected files; rechecked during installation",
                    source.Bytes>0?(source.Bytes/1048576.0).ToString("N1")+" MB":"Folder");
                grid.Rows[i].Tag=source;
                if(invalid)grid.Rows[i].DefaultCellStyle.ForeColor=Color.FromArgb(154,53,38);
            }
            dialog.Controls.Add(grid);
            LabelAt(dialog,"Select a source to see its exact path and next step:",18,378,920,22,10,true,Color.Black);
            var path=new TextBox{Location=new Point(18,407),Size=new Size(934,88),ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical};dialog.Controls.Add(path);
            var use=new Button{Text="Use selected source",Location=new Point(688,515),Size=new Size(264,40)};dialog.Controls.Add(use);
            grid.SelectionChanged+=delegate{
                if(grid.SelectedRows.Count==0)return;
                var selected=(FoundSource)grid.SelectedRows[0].Tag;
                bool invalid=!string.IsNullOrEmpty(selected.Problem);
                path.Text=selected.Path+"\r\n"+(invalid?"INCOMPLETE · "+selected.Problem:
                    selected.Kind=="Game archive candidate"?"CANDIDATE · Add it to Install; remaining native files will be checked by SHA-256.":"DETECTED · "+(selected.Detection??"Source verification continues during installation."));
                use.Text=invalid?"Review missing files":selected.Kind=="Game archive candidate"?"Add candidate to Install":"Use selected source";
            };
            ButtonAt(dialog,"Open containing folder",18,515,230,40,()=>Guard(()=>{if(grid.SelectedRows.Count>0)LauncherFolders.Open(((FoundSource)grid.SelectedRows[0].Tag).Path);}),false);
            use.Click+=delegate{
                if(grid.SelectedRows.Count==0)return;var source=(FoundSource)grid.SelectedRows[0].Tag;
                if(!string.IsNullOrEmpty(source.Problem)){
                    MessageBox.Show(dialog,source.Problem+"\r\n\r\nNext: supply the missing archive volumes in the same folder, or combine other complete originals using Install → Combine original sources.","Source incomplete",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;
                }
                if(source.Kind=="Installed game"){var p=LibraryProfiles.FirstOrDefault(x=>x.Root.Equals(source.Path,StringComparison.OrdinalIgnoreCase));
                    if(p==null){p=Store.Detect(source.Path,source.Name);Added(p);}
                    if(UniversalSupport.Identify(p.Root)!=null)startingSource=p;
                    methods.SelectedIndex=1;UpdateInstall();ShowPage("Install");dialog.Close();return;}
                var recipe=targets.Items.Cast<ModRecipe>().FirstOrDefault(x=>x.Id==source.RecipeId);if(recipe==null){MessageBox.Show(dialog,"This source is not a cataloged bundle. Use Bundle + hotfixes with its author's installation instructions.");return;}
                if(source.Kind=="Game archive candidate"){
                    AddOriginalInput(source.Path);targets.SelectedItem=recipe;methods.SelectedIndex=1;
                    UpdateInstall();ShowPage("Install");dialog.Close();return;
                }
                preferredSources[source.RecipeId+"/"+source.Kind]=source;targets.SelectedItem=recipe;methods.SelectedIndex=source.Kind=="Delta pack"||source.Kind=="Any-version pack"?0:1;UpdateInstall();ShowPage("Install");dialog.Close();
            };
            StyleDialog(dialog);dialog.ShowDialog(this);
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
            foreach(var p in result.Item2){if(Store.IsSuppressed(settings,p.Root)||LibraryProfiles.Any(x=>Path.GetFullPath(x.Root).Equals(p.Root,StringComparison.OrdinalIgnoreCase)))continue;p.Note="Automatically found installation; existing files and saves";settings.Profiles.Add(p);added++;}
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
        LauncherMenuStyle.Apply(menu);LauncherMenuLifetime.RetireAfterClose(menu);menu.Show(anchor,location??new Point(0,anchor.Height));
    }
    ContextMenuStrip NewActionMenu(){
        var menu=new ContextMenuStrip{BackColor=card,ForeColor=ForeColor,Font=Font,
            Renderer=new LauncherMenuRenderer(),ShowImageMargin=false,ShowItemToolTips=true};
        LauncherMenuStyle.Apply(menu);return menu;
    }
    ToolStripMenuItem MenuAction(ContextMenuStrip menu,string label,Action run,bool enabled=true){
        var item=new ToolStripMenuItem(label){Enabled=enabled,BackColor=card,ForeColor=ForeColor};
        item.Click+=delegate{Guard(run);};menu.Items.Add(item);return item;
    }
    void ShowInstallMenu(Control anchor,ModRecipe recipe,Point? location=null){
        if(recipe==null)return;
        var menu=NewActionMenu();
        MenuAction(menu,"Install "+recipe.Name,()=>{
            targets.SelectedItem=recipe;methods.SelectedIndex=1;ShowPage("Install");
        });
        MenuAction(menu,"Inspect available source files",ShowFoundFiles);
        LauncherMenuStyle.Apply(menu);LauncherMenuLifetime.RetireAfterClose(menu);menu.Show(anchor,location??new Point(0,anchor.Height));
    }
    void SelectManagedGame(Profile profile,string page){
        if(profile==null)return;
        profiles.SelectedItem=LibraryProfiles.FirstOrDefault(x=>x.Id==profile.Id)??profile;
        settings.SelectedId=profile.Id;UpdateProfile();ShowPage(page);
    }
    void PrepareUpgrade(Profile profile){
        if(profile==null)return;
        string id=UniversalSupport.Identify(profile.Root);
        var recipes=targets.Items.Cast<ModRecipe>().Where(x=>UpgradeSupport.MayUpgrade(id,x.Id)).ToArray();
        if(recipes.Length==0){
            MessageBox.Show(this,"No registered forward upgrade is available for "+profile.Name+
                ". You can still install another branch separately from Install.",
                "No compatible upgrade",MessageBoxButtons.OK,MessageBoxIcon.Information);return;
        }
        var latest=recipes.OrderByDescending(x=>x.SaveGeneration).First();
        startingSource=profile;targets.SelectedItem=latest;methods.SelectedIndex=1;ShowPage("Install");
    }
    void ShowManageMenu(Control anchor,Profile profile,Point? location=null){
        if(profile==null)return;
        var menu=NewActionMenu();
        MenuAction(menu,"Play "+profile.Name,()=>{SelectManagedGame(profile,"Play");Launch();},!busy&&!running);
        menu.Items.Add(new ToolStripSeparator());
        MenuAction(menu,"Open game folder",()=>LauncherFolders.Open(profile.Root),Directory.Exists(profile.Root));
        MenuAction(menu,"Open save folder",()=>{SelectManagedGame(profile,"Saves");OpenSaves();});
        MenuAction(menu,"Back up saves",()=>{SelectManagedGame(profile,"Saves");BackupNow();},!busy&&!running);
        menu.Items.Add(new ToolStripSeparator());
        MenuAction(menu,"English translation",()=>SelectManagedGame(profile,"English"));
        MenuAction(menu,"Upgrade this version",()=>PrepareUpgrade(profile),!busy&&!running);
        menu.Items.Add(new ToolStripSeparator());
        MenuAction(menu,"Remove from launcher only…",()=>RemoveVersion(profile,RemovalMode.IndexOnly),!busy&&!running);
        var uninstall=MenuAction(menu,"Uninstall managed game files…",()=>RemoveVersion(profile,RemovalMode.InstalledGame),!busy&&!running);
        uninstall.ToolTipText="Verify hashes first. Keeps saves, backups, ZIP/RAR/7z and unmatched files.";
        LauncherMenuStyle.Apply(menu);LauncherMenuLifetime.RetireAfterClose(menu);menu.Show(anchor,location??new Point(0,anchor.Height));
    }
}
