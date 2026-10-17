using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

// Isolated visual verification; does not use game data or native installations.
public static class LauncherVisualSmoke {
    static void Check(bool condition,string name){if(!condition)throw new InvalidOperationException(name);}
    public static int Main(string[] args) {
        if(args.Length==0)throw new ArgumentException("A screenshot directory is required.");
        string target=Path.GetFullPath(args[0]);
        Directory.CreateDirectory(target);
        Application.EnableVisualStyles();
        using(var form=new Form{Text="Celesphonia · UI theme fixture",ClientSize=new Size(920,520),
            FormBorderStyle=FormBorderStyle.FixedToolWindow,StartPosition=FormStartPosition.Manual,
            Location=new Point(80,80),BackColor=LauncherPaint.Ink,ForeColor=LauncherPaint.Paper,
            Font=new Font("Segoe UI",10),ShowInTaskbar=false}) {
            var header=new Label{Text="Library controls",Location=new Point(24,25),Size=new Size(800,44),
                Font=new Font("Segoe UI",21,FontStyle.Bold),ForeColor=LauncherPaint.Paper};
            form.Controls.Add(header);
            var card=new SleekCard{Location=new Point(24,85),Size=new Size(870,375),
                BackColor=LauncherPaint.Surface};
            form.Controls.Add(card);
            int x=24;
            foreach(var item in new[]{Tuple.Create("Play selected",true,false),
                                        Tuple.Create("Manage installations",false,false),
                                        Tuple.Create("Saves and backups",false,false),
                                        Tuple.Create("Options",false,true)}) {
                var button=new SleekButton{Location=new Point(x,27),Size=new Size(x==24?160:190,44),
                    Text=item.Item1,Primary=item.Item2,Navigation=item.Item3,
                    BackColor=item.Item2?Color.FromArgb(26,86,97):LauncherPaint.Surface,
                    ForeColor=LauncherPaint.Paper,Font=new Font("Segoe UI",10,FontStyle.Bold)};
                card.Controls.Add(button);x+=button.Width+15;
            }
            var caption=new Label{Text="Native files remain where they are. Manage the launcher without touching archive sources.",
                ForeColor=LauncherPaint.Muted,Font=new Font("Segoe UI",10),Location=new Point(29,103),Size=new Size(805,44)};
            card.Controls.Add(caption);
            var progress=new SleekProgress{Location=new Point(28,157),Size=new Size(805,8),Value=625};
            card.Controls.Add(progress);
            var menu=new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Open game folder  (Ctrl+O)"));
            menu.Items.Add(new ToolStripMenuItem("Scan nearby archives and extracted games"));
            var nested=new ToolStripMenuItem("Installation actions");
            nested.DropDownItems.Add(new ToolStripMenuItem("Register existing installation"));
            nested.DropDownItems.Add(new ToolStripMenuItem("Remove launcher entry only"));
            nested.DropDownItems.Add(new ToolStripSeparator());
            nested.DropDownItems.Add(new ToolStripMenuItem("Uninstall game  •  keep ZIP and RAR archives"));
            menu.Items.Add(nested);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Keep original archive files"){Checked=true});
            menu.Items.Add(new ToolStripMenuItem("Unavailable original game version"){Enabled=false});
            LauncherMenuStyle.Apply(menu);
            Check(menu.ShowCheckMargin&&menu.Renderer is LauncherMenuRenderer,"Menu style not installed.");
            Check(menu.Items.Count==6&&nested.DropDownItems.Count==4,"Menu content changed.");
            form.Show();Application.DoEvents();
            using(var image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height)){
                form.DrawToBitmap(image,new Rectangle(Point.Empty,form.ClientSize));
                image.Save(Path.Combine(target,"launcher-shell-controls.png"));
            }
            menu.Show(form,new Point(35,263));Application.DoEvents();
            Check(menu.Width>=186&&menu.Height>=100,"Menu isn't legible.");
            using(var image=new Bitmap(menu.Width,menu.Height)){
                menu.DrawToBitmap(image,new Rectangle(0,0,menu.Width,menu.Height));
                image.Save(Path.Combine(target,"launcher-shell-menu.png"));
            }
            nested.ShowDropDown();Application.DoEvents();
            Check(nested.DropDown.Width>=190&&nested.DropDown.Height>=90,"Nested menu too small.");
            using(var image=new Bitmap(nested.DropDown.Width,nested.DropDown.Height)){
                nested.DropDown.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height));
                image.Save(Path.Combine(target,"launcher-shell-submenu.png"));
            }
            nested.HideDropDown();
            menu.Close();Application.DoEvents();
            menu.Dispose();
            form.Close();
            Console.WriteLine("PASS LauncherShell visual smoke: menu "+menu.Width+"x"+menu.Height+
                "; six menu entries / four nested entries, themed controls and screenshot PNGs.");
        }
        LauncherMenuLifetime.Test(Path.Combine(target,"menu-lifetime-result.json"));
        Console.WriteLine("PASS LauncherMenuLifetime: 30 popup close cycles.");
        return 0;
    }
}
