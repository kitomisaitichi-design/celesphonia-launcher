using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class LauncherLayoutTests {
    static void Animate(int milliseconds){var watch=System.Diagnostics.Stopwatch.StartNew();while(watch.ElapsedMilliseconds<milliseconds){Application.DoEvents();System.Threading.Thread.Sleep(5);}}
    public static void TestOrphans(string report,string profileFile){
        byte[] original=File.ReadAllBytes(profileFile);
        Settings recorded=Store.Json.Deserialize<Settings>(System.Text.Encoding.UTF8.GetString(original).TrimStart('\ufeff'));
        if(recorded.Profiles==null)throw new IOException("Orphan test settings contain no profiles.");
        // Read without writing or syncing the machine-wide installation index.
        foreach(Profile p in recorded.Profiles){
            if(p==null)continue;
            p.Root=Store.PortablePath(p.Root,false);
            p.SavePath=Store.PortablePath(p.SavePath,false);
        }
        var visible=ProfileLibrary.Visible(recorded.Profiles,recorded.SelectedId);
        var unavailable=recorded.Profiles.Where(p=>!ProfileLibrary.IsPlayable(p)).ToList();
        if(visible.Any(p=>unavailable.Contains(p)))throw new InvalidOperationException("An orphaned game remains visible.");
        string stage=Path.Combine(Store.Home,"Staging","OrphanProfileTests-"+Guid.NewGuid().ToString("N"));
        string good=Path.Combine(stage,"good"),web=Path.Combine(good,"www"),data=Path.Combine(web,"data");
        string bad=Path.Combine(stage,"broken");
        try{
            Directory.CreateDirectory(data);Directory.CreateDirectory(bad);
            File.WriteAllText(Path.Combine(good,"package.json"),"{\"main\":\"www/index.html\"}",System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(web,"index.html"),"<html><head></head><body></body></html>",System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(data,"System.json"),"{}",System.Text.Encoding.UTF8);
            File.WriteAllBytes(Path.Combine(good,"Game.exe"),new byte[]{0x4d,0x5a});
            // A new installation legitimately has no save directory yet.
            Profile valid=new Profile{Id="good",Name="Valid no-save installation",Root=good,
                Exe="Game.exe",SavePath=Path.Combine(web,"save")};
            Profile missing=new Profile{Id="missing",Name="Removed install",Root=Path.Combine(stage,"deleted"),
                Exe="Game.exe",SavePath=Path.Combine(stage,"deleted","save")};
            Profile incomplete=new Profile{Id="incomplete",Name="Broken package",Root=bad,
                Exe="Game.exe",SavePath=Path.Combine(bad,"save")};
            Profile wrongSave=new Profile{Id="wrong-save",Name="Stale save mapping",Root=good,
                Exe="Game.exe",SavePath=Path.Combine(stage,"unrelated","save")};
            if(!ProfileLibrary.IsPlayable(valid)||Directory.Exists(valid.SavePath))
                throw new InvalidOperationException("A valid game with no saves was filtered.");
            if(ProfileLibrary.IsPlayable(missing)||ProfileLibrary.IsPlayable(incomplete)||
               ProfileLibrary.IsPlayable(wrongSave))throw new InvalidOperationException("A broken profile passed validation.");
            var fixture=new[]{valid,missing,incomplete,wrongSave};
            var filtered=ProfileLibrary.Visible(fixture,"missing");
            if(filtered.Count!=1||filtered[0]!=valid)throw new InvalidOperationException("Stale selection was not safely skipped.");
            if(ProfileLibrary.Entries(fixture,"missing").OfType<Profile>().Any(p=>p!=valid))
                throw new InvalidOperationException("Sidebar included a broken installed row.");
            File.WriteAllText(Path.Combine(good,"launcher-base.json"),
                Store.Json.Serialize(new{BaseRoot=missing.Root,StorageMode="Full independent copy"}),System.Text.Encoding.UTF8);
            if(RuntimeSupport.OptionalBaseWeb(good)!=null)throw new InvalidOperationException("A missing historical base was treated as required.");
            File.Delete(Path.Combine(data,"System.json"));
            if(ProfileLibrary.IsPlayable(valid))throw new InvalidOperationException("Broken game content was shown as playable.");
            File.WriteAllText(Path.Combine(data,"System.json"),"{}",System.Text.Encoding.UTF8);
            if(!ProfileLibrary.IsPlayable(valid))throw new InvalidOperationException("Restored game content stayed hidden.");
            File.Delete(Path.Combine(good,"Game.exe"));
            if(ProfileLibrary.IsPlayable(valid))throw new InvalidOperationException("Missing executable was shown as playable.");
            File.WriteAllBytes(Path.Combine(good,"Game.exe"),new byte[]{0x4d,0x5a});
            if(!ProfileLibrary.IsPlayable(valid))throw new InvalidOperationException("Restored executable stayed hidden.");
            if(!original.SequenceEqual(File.ReadAllBytes(profileFile)))throw new InvalidOperationException("Orphan filtering modified the user's profile database.");
            File.WriteAllText(report,Store.Json.Serialize(new{status="PASS",realProfiles=recorded.Profiles.Count,
                realUnavailable=unavailable.Select(p=>new{id=p.Id,name=p.Name}).ToArray(),realVisible=visible.Count,
                orphanSelectionFallsBack=true,missingRootExcluded=true,missingGameFilesExcluded=true,
                invalidSaveMappingExcluded=true,validEmptySaveFolderAllowed=true,restoredGameBecomesVisible=true,
                absentHistoricalBaseOptional=true,privateProfilesPreserved=true,settingsBytesPreserved=true,
                gameFilesDeleted=0,userSavesDeleted=0}),System.Text.Encoding.UTF8);
        }finally{
            if(Directory.Exists(stage)&&Path.GetFileName(stage).StartsWith("OrphanProfileTests-",StringComparison.Ordinal))
                Directory.Delete(stage,true);
        }
    }
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
    static IEnumerable<Control> Descendants(Control parent){foreach(Control c in parent.Controls){yield return c;foreach(var child in Descendants(c))yield return child;}}
    static int Check(Control parent){
        int count=0;
        foreach(Control c in parent.Controls){
            if(!c.Visible)continue;
            if(c is Button){
                count++;
                foreach(Control sibling in parent.Controls){
                    if(sibling==c||!sibling.Visible)continue;
                    if(c.Bounds.IntersectsWith(sibling.Bounds))throw new InvalidOperationException("Overlapping button: "+c.Text+" / "+sibling.Text);
                }
                var measured=TextRenderer.MeasureText(c.Text,c.Font,new Size(10000,10000),TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);
                if(measured.Width>c.Width-c.Padding.Horizontal-18||measured.Height>c.Height-6)throw new InvalidOperationException("Clipped button caption: "+c.Text);
            }
            count+=Check(c);
        }
        return count;
    }
    public static void Run(string report,string config){
        byte[] before=File.ReadAllBytes(config);
        var settings=Store.Json.Deserialize<Settings>(System.Text.Encoding.UTF8.GetString(before).TrimStart('\ufeff'));
        var pages=new List<object>();int volumeCount=0,privateProfilesExcluded=settings.Profiles.Count(p=>FileDiscovery.IsNativeProbe(p.Root));string folder=Path.GetDirectoryName(Path.GetFullPath(report));Directory.CreateDirectory(folder);
        using(var form=new PolishedLauncher(true,settings)){
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-10000,-10000);form.Show();form.ClientSize=new Size(1180,820);Application.DoEvents();
            if(!Descendants(form).OfType<RuntimeControlsToggle>().Single().Visible)throw new InvalidOperationException("Launcher did not open directly on Play.");
            var sidebar=Descendants(form).OfType<ListBox>().Single();
            // The same sidebar intentionally interleaves installed games and
            // currently available creator updates. Both are valid rows.
            var volumes=sidebar.Items.OfType<Profile>().ToList();
            var available=sidebar.Items.OfType<ModRecipe>().ToList();
            if(volumes.Count+available.Count!=sidebar.Items.Count)
                throw new InvalidOperationException("Unknown version sidebar row type.");
            if(volumes.Count>0 && !settings.Profiles.Any(p=>UniversalSupport.Identify(p.Root)==UpgradeSupport.Latest)
               && !available.Any(r=>r.Id==UpgradeSupport.Latest))
                throw new InvalidOperationException("The available v1.04 upgrade is missing from the sidebar.");
            volumeCount=volumes.Count;
            if(volumes.Any(p=>FileDiscovery.IsNativeProbe(p.Root))||volumes.Select(ProfileLibrary.Identity).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=volumes.Count)throw new InvalidOperationException("Duplicate/private profiles remain in Volumes.");
            var scroller=Descendants(form).OfType<ContentScroller>().Single();
            foreach(string page in new[]{"Play","Files","English","Install","Saves","Help"}){
                var tab=Descendants(form).OfType<Button>().First(b=>b.Parent==form&&b.Text==page);tab.PerformClick();Application.DoEvents();
                int buttons=Check(form);
                if(page=="Play"){
                    if(scroller.Slider.Visible||scroller.Limit!=0||scroller.Canvas.Top!=0)throw new InvalidOperationException("Play has unnecessary scrolling or a clipped heading.");
                    var runtimeToggle=Descendants(form).OfType<RuntimeControlsToggle>().Single();
                    var launch=Descendants(form).OfType<Button>().Single(b=>b.Text=="Play selected game");
                    if(runtimeToggle.Height<44||runtimeToggle.Width<700||runtimeToggle.PointToScreen(Point.Empty).Y>=launch.PointToScreen(Point.Empty).Y)throw new InvalidOperationException("Runtime toggle is not prominent above launch.");
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    foreach(string flag in new[]{"running","discovering"}){
                        typeof(PolishedLauncher).GetField(flag,flags).SetValue(form,true);
                        typeof(PolishedLauncher).GetMethod("UpdateActions",flags).Invoke(form,null);
                        if(volumes.Count>0&&!runtimeToggle.Enabled)throw new InvalidOperationException("Next-launch preference was disabled by "+flag);
                        typeof(PolishedLauncher).GetField(flag,flags).SetValue(form,false);
                    }
                    typeof(PolishedLauncher).GetMethod("UpdateActions",flags).Invoke(form,null);
                    if(volumes.Count>0){
                        bool value=runtimeToggle.Checked;
                        typeof(CheckBox).GetMethod("OnClick",flags).Invoke(runtimeToggle,new object[]{EventArgs.Empty});
                        if(runtimeToggle.Checked==value)throw new InvalidOperationException("Checkbox row click did not toggle.");
                        typeof(CheckBox).GetMethod("OnClick",flags).Invoke(runtimeToggle,new object[]{EventArgs.Empty});
                    }
                }
                using(var bitmap=new Bitmap(form.Width,form.Height)){
                    using(var graphics=Graphics.FromImage(bitmap)){IntPtr dc=graphics.GetHdc();try{if(!PrintWindow(form.Handle,dc,0))throw new InvalidOperationException("Native window capture failed.");}finally{graphics.ReleaseHdc(dc);}}
                    bitmap.Save(Path.Combine(folder,"launcher-"+page.ToLowerInvariant()+".png"));
                }
                pages.Add(new{page=page,visibleButtonsChecked=buttons,nativeWindowCaptured=true});
            }
            form.TestPage("Play");Application.DoEvents();
            var hovered=Descendants(form).OfType<SleekButton>().First(b=>b.Text==(volumeCount>0?"Play selected game":"Play"));
            var mouseFlags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(Control).GetMethod("OnMouseLeave",mouseFlags).Invoke(hovered,new object[]{EventArgs.Empty});Animate(360);
            typeof(Control).GetMethod("OnMouseEnter",mouseFlags).Invoke(hovered,new object[]{EventArgs.Empty});Animate(55);
            if(hovered.HoverAmount<=0||hovered.HoverAmount>=.95)throw new InvalidOperationException("Hover highlight is not smoothly animated.");
            Animate(360);if(hovered.HoverAmount<.99)throw new InvalidOperationException("Hover animation did not finish.");
            using(var bitmap=new Bitmap(form.Width,form.Height)){using(var graphics=Graphics.FromImage(bitmap)){IntPtr dc=graphics.GetHdc();try{if(!PrintWindow(form.Handle,dc,0))throw new InvalidOperationException("Hover capture failed.");}finally{graphics.ReleaseHdc(dc);}}bitmap.Save(Path.Combine(folder,"launcher-hover.png"));}
            typeof(Control).GetMethod("OnMouseLeave",mouseFlags).Invoke(hovered,new object[]{EventArgs.Empty});Animate(360);
            if(hovered.HoverAmount>.01)throw new InvalidOperationException("Hover highlight did not fade out.");
            form.TestPage("Help");Application.DoEvents();
            form.ClientSize=new Size(960,650);Application.DoEvents();
            if(form.HorizontalScroll.Maximum<1179||scroller.Limit<=0||!scroller.Slider.Enabled)throw new InvalidOperationException("Small-screen content scrollbar is unavailable.");
            form.AutoScrollPosition=new Point(1180,0);scroller.Position=scroller.Limit;Application.DoEvents();
            var guide=Descendants(form).OfType<Button>().First(b=>b.Text=="Quick start guide");Rectangle bounds=form.RectangleToClient(guide.RectangleToScreen(guide.ClientRectangle));
            if(!form.ClientRectangle.Contains(bounds))throw new InvalidOperationException("Bottom controls cannot be reached by scrolling.");
            var header=Descendants(form).OfType<Button>().First(b=>b.Parent==form&&b.Text=="Help");
            if(header.Top<0||header.Bottom>111)throw new InvalidOperationException("Content scrolling moved the navigation.");
            foreach(string page in new[]{"Play","Files","English","Install","Saves","Help"}){
                Descendants(form).OfType<Button>().First(b=>b.Parent==form&&b.Text==page).PerformClick();Application.DoEvents();
                if(scroller.Position!=0||scroller.Canvas.Top!=0)throw new InvalidOperationException("Switching pages retained an unrelated scroll offset.");
                Check(form);
                foreach(var button in Descendants(scroller.Canvas).OfType<Button>().Where(b=>b.Visible)){
                    scroller.Position=0;Application.DoEvents();
                    int top=scroller.Canvas.PointToClient(button.PointToScreen(Point.Empty)).Y;
                    scroller.Position=top;Application.DoEvents();
                    Rectangle screenBounds=form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
                    if(!form.ClientRectangle.Contains(screenBounds))throw new InvalidOperationException("Small-screen button is unreachable: "+button.Text);
                }
            }
            scroller.Position=scroller.Limit;Application.DoEvents();
            using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(folder,"launcher-scrolling.png"));}
            if(!File.Exists(Path.Combine(Store.Home,"README.txt")))throw new FileNotFoundException("Quick start guide is missing.");
            Descendants(form).OfType<Button>().First(b=>b.Parent==form&&b.Text=="Play").PerformClick();scroller.Position=0;Application.DoEvents();
            var toggle=Descendants(form).OfType<CheckBox>().Single(c=>c.Text=="Enable runtime controls for this game");
            if(volumes.Count>0){toggle.Checked=true;if(!volumes[0].RuntimeControls&& !volumes.Any(v=>v.RuntimeControls))throw new InvalidOperationException("Runtime checkbox did not update profile.");}
            Descendants(form).OfType<Button>().First(b=>b.Parent==form&&b.Text=="Help").PerformClick();Application.DoEvents();
            var picker=Descendants(form).OfType<ChoicePicker>().Single(c=>c.AccessibleName=="Launcher theme");
            foreach(string theme in new[]{"Midnight","Violet","Ocean","Daylight"}){
                picker.SelectedItem=theme;Application.DoEvents();Check(form);if(settings.LauncherTheme!=theme)throw new InvalidOperationException("Theme not retained.");
                form.ClientSize=new Size(1180,820);form.AutoScrollPosition=Point.Empty;form.TestPage("Play");Application.DoEvents();
                if(scroller.Slider.Visible||scroller.Canvas.Top!=0)throw new InvalidOperationException("Theme left Play scrolled.");
                using(var bitmap=new Bitmap(form.Width,form.Height)){using(var graphics=Graphics.FromImage(bitmap)){IntPtr dc=graphics.GetHdc();try{if(!PrintWindow(form.Handle,dc,0))throw new InvalidOperationException("Theme capture failed.");}finally{graphics.ReleaseHdc(dc);}}bitmap.Save(Path.Combine(folder,"launcher-play-"+theme.ToLowerInvariant()+".png"));}
                form.TestPage("Help");Application.DoEvents();
            }
            var roundtrip=Store.Json.Deserialize<Settings>(Store.Json.Serialize(settings));if(roundtrip.LauncherTheme!="Daylight"||roundtrip.Profiles.Any(v=>v.RuntimeControls)!=settings.Profiles.Any(v=>v.RuntimeControls))throw new InvalidOperationException("New settings fields did not round-trip.");
            form.Close();
        }
        if(!before.SequenceEqual(File.ReadAllBytes(config)))throw new InvalidOperationException("Layout fixture changed profile settings.");
            string launcher=Path.Combine(Store.Home,"Celesphonia Portable Switcher.exe");
            if(!File.Exists(launcher))launcher=typeof(LauncherLayoutTests).Assembly.Location;
            File.WriteAllText(report,Store.Json.Serialize(new{status="PASS",pages=pages,visibleVolumes=volumeCount,privateProfilesExcluded=privateProfilesExcluded,noDuplicateVolumes=true,buttonOverlapChecks=true,buttonCaptionFits=true,smallScreenScrolling=true,contentScrollbar=true,allPageButtonsReachable=true,navigationStaysVisible=true,settingsBytesPreserved=true,gameLaunches=0,saveWrites=0,fourThemesChecked=true,runtimeCheckboxChecked=true,newSettingsSerialize=true,playFitsWithoutScrolling=true,runtimeToggleProminent=true,nextLaunchPreferenceAvailable=true,checkboxRowClickToggles=true,canvasOffsetResets=true,hoverFadeInAndOut=true,launcherHash=Store.Hash(launcher)}));
    }

    // Independent redesign acceptance: does not call save, game installation,
    // removal or discovery writes. The constructor uses preview=true and a
    // deserialized copy of the supplied settings, never Store.Read/Write.
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    delegate bool WindowVisitor(IntPtr handle,IntPtr state);
    [DllImport("user32.dll")] static extern bool EnumThreadWindows(uint thread,WindowVisitor callback,IntPtr state);
    static ContextMenuStrip VisibleNativeMenu(){
        ContextMenuStrip found=null;
        EnumThreadWindows(GetCurrentThreadId(),delegate(IntPtr handle,IntPtr state){
            var menu=Control.FromHandle(handle) as ContextMenuStrip;
            if(menu!=null&&menu.Visible){found=menu;return false;}
            return true;
        },IntPtr.Zero);
        return found;
    }
    static void NativePng(Control control,string filename){
        if(control==null||!control.IsHandleCreated||control.Width<1||control.Height<1)
            throw new InvalidOperationException("Native screenshot has no valid control handle: "+filename);
        using(var bitmap=new Bitmap(control.Width,control.Height)){
            using(var graphics=Graphics.FromImage(bitmap)){
                IntPtr dc=graphics.GetHdc();
                try{if(!PrintWindow(control.Handle,dc,0))throw new IOException("PrintWindow failed for "+filename);}
                finally{graphics.ReleaseHdc(dc);}
            }
            bitmap.Save(filename,System.Drawing.Imaging.ImageFormat.Png);
        }
    }
    static string CleanFileToken(string name){
        return new string(name.Select(c=>char.IsLetterOrDigit(c)?c:'-').ToArray()).Trim('-').ToLowerInvariant();
    }
    static object InvokePrivate(object owner,string method,params object[] parameters){
        var m=owner.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic);
        if(m==null)throw new InvalidOperationException("UI entrypoint is missing: "+method);
        try{return m.Invoke(owner,parameters);}
        catch(TargetInvocationException e){throw e.InnerException??e;}
    }
    static void RedesignNote(List<string> issues,string category,string detail){
        issues.Add(category+": "+detail);
    }
    static void AuditBounds(Control surface,Control content,string tag,List<string> issues){
        if(content.Width<=0||content.Height<=0){RedesignNote(issues,"CLIP",tag+" zero-size control "+content.Name);return;}
        var rect=surface.RectangleToClient(content.RectangleToScreen(content.ClientRectangle));
        if(rect.Left<0||rect.Right>surface.ClientSize.Width||rect.Top<0||rect.Bottom>surface.ClientSize.Height)
            RedesignNote(issues,"CLIP",tag+" "+content.GetType().Name+" '"+content.Text.Replace("\r"," ").Replace("\n"," ")+"' outside "+surface.ClientSize+": "+rect);
    }
    static void AuditPeerButtons(Control parent,string tag,List<string> issues){
        var buttons=parent.Controls.Cast<Control>().Where(x=>x.Visible&&x is Button).ToArray();
        for(int i=0;i<buttons.Length;i++){
            var button=buttons[i];
            var text=TextRenderer.MeasureText(button.Text,button.Font,new Size(10000,10000),
                TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);
            if(text.Width>button.Width-button.Padding.Horizontal-13)
                RedesignNote(issues,"CAPTION",tag+" '"+button.Text+"' width "+text.Width+" exceeds "+button.Width);
            if(text.Height>button.Height-4)
                RedesignNote(issues,"CAPTION",tag+" '"+button.Text+"' height "+text.Height+" exceeds "+button.Height);
            for(int j=i+1;j<buttons.Length;j++)
                if(button.Bounds.IntersectsWith(buttons[j].Bounds))
                    RedesignNote(issues,"OVERLAP",tag+" buttons '"+button.Text+"' and '"+buttons[j].Text+"'");
        }
        foreach(Control child in parent.Controls)if(child.Visible)AuditPeerButtons(child,tag,issues);
    }
    static void AuditPageControls(PolishedLauncher form,ContentScroller scroller,string page,
                                  string variant,List<string> issues){
        var canvas=scroller.Canvas;
        AuditPeerButtons(canvas,page+" "+variant,issues);
        foreach(var button in Descendants(canvas).OfType<Button>().Where(x=>x.Visible)){
            Rectangle inside=canvas.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
            if(inside.Left<0||inside.Right>canvas.ClientSize.Width)
                RedesignNote(issues,"CLIP",page+" "+variant+" horizontal canvas overflow '"+button.Text+"'");
            if(inside.Top<0||inside.Bottom>canvas.ClientSize.Height)
                RedesignNote(issues,"CLIP",page+" "+variant+" vertical canvas overflow '"+button.Text+"'");
            // Only inspect safe scroll position; never invoke the button.
            int scroll=Math.Max(0,Math.Min(scroller.Limit,inside.Top));
            scroller.Position=scroll;Application.DoEvents();
            Rectangle inView=scroller.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
            if(inView.Top<0||inView.Bottom>scroller.ClientSize.Height+2)
                RedesignNote(issues,"REACH",page+" "+variant+" button not reachable by vertical scroll: '"+button.Text+"'");
        }
        scroller.Position=0;Application.DoEvents();
    }
    static void CheckMenu(ContextMenuStrip menu,string name,string expectedText,string folder,
                          List<string> issues,List<object> menus){
        if(menu==null){RedesignNote(issues,"MENU",name+" did not produce an inspectable native popup");return;}
        Application.DoEvents();
        var labels=menu.Items.Cast<ToolStripItem>().Select(x=>x.Text).ToArray();
        if(!labels.Any(x=>x.IndexOf(expectedText,StringComparison.OrdinalIgnoreCase)>=0))
            RedesignNote(issues,"MENU",name+" missing '"+expectedText+"', got "+string.Join(" | ",labels));
        if(menu.Width<=100||menu.Height<30)
            RedesignNote(issues,"MENU",name+" too narrow/small "+menu.Size);
        int largestOverhang=0;var clippedLabels=new List<string>();
        foreach(ToolStripItem item in menu.Items){
            if(item is ToolStripSeparator)continue;
            largestOverhang=Math.Max(largestOverhang,item.Bounds.Right-menu.ClientSize.Width);
            if(item.Bounds.Top<0||item.Bounds.Bottom>menu.ClientSize.Height)
                RedesignNote(issues,"MENU",name+" vertically clipped option '"+item.Text+"' bounds="+item.Bounds);
            var measured=TextRenderer.MeasureText(item.Text,item.Font,new Size(10000,10000),
                TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);
            // Check glyphs against the *visible native window*, not the item
            // background: WinForms ToolStrip items can be wider than the popup
            // window (e.g. 239px item inside a 224px menu).
            int usable=menu.ClientSize.Width-Math.Max(0,item.Bounds.Left)-13;
            if(measured.Width>usable)
                clippedLabels.Add(item.Text+" measured "+measured.Width+"px of "+usable+"px");
        }
        if(largestOverhang>3)
            RedesignNote(issues,"MENU",name+" native item hitboxes overhang popup by "+
                largestOverhang+"px; inspect click regions (captured text remains separately measurable)");
        foreach(string label in clippedLabels)
            RedesignNote(issues,"MENU",name+" text clipped: "+label);
        string screenshot=Path.Combine(folder,CleanFileToken(name)+".png");
        try{NativePng(menu,screenshot);}catch(Exception e){RedesignNote(issues,"CAPTURE",name+" "+e.Message);}
        menus.Add(new{name=name,items=labels,width=menu.Width,height=menu.Height,screenshot=screenshot});
        menu.Close();Application.DoEvents();
    }
    static void NativePopup(PolishedLauncher form,Control anchor,string name,string expected,
                            string folder,List<string> issues,List<object> menus,Action show){
        try{
            show();Application.DoEvents();
            var menu=VisibleNativeMenu();
            CheckMenu(menu,name,expected,folder,issues,menus);
        }catch(Exception e){RedesignNote(issues,"MENU",name+" "+e.Message);}
    }
    static void NativeModal(PolishedLauncher form,string name,string folder,
                            List<string> issues,List<object> dialogs,Action invoke,
                            string requiredCaption){
        var root=form;
        string saved=Path.Combine(folder,CleanFileToken(name)+".png");
        Form shown=null;
        var started=Stopwatch.StartNew();
        using(var ticker=new System.Windows.Forms.Timer{Interval=60}){
            ticker.Tick+=delegate{
                if(shown!=null)return;
                shown=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f!=root&&f.Visible);
                if(shown==null)return;
                try{
                    foreach(Control button in shown.Controls.OfType<Button>())
                        AuditBounds(shown,button,name,issues);
                    AuditPeerButtons(shown,name,issues);
                    if(!Descendants(shown).Any(c=>c.Text.IndexOf(requiredCaption,StringComparison.OrdinalIgnoreCase)>=0))
                        RedesignNote(issues,"DIALOG",name+" missing '"+requiredCaption+"'");
                    NativePng(shown,saved);
                    dialogs.Add(new{name=name,size=shown.ClientSize.ToString(),screenshot=saved,
                        controls=Descendants(shown).Count()});
                }catch(Exception e){RedesignNote(issues,"DIALOG",name+" "+e.Message);}
                finally{shown.DialogResult=DialogResult.Cancel;shown.Close();}
            };
            ticker.Start();
            try{
                root.BeginInvoke(new Action(invoke));
                while(shown==null&&started.ElapsedMilliseconds<9000){Application.DoEvents();System.Threading.Thread.Sleep(15);}
                if(shown==null)RedesignNote(issues,"DIALOG",name+" did not show before timeout");
            }catch(Exception e){RedesignNote(issues,"DIALOG",name+" "+e.Message);}
            finally{ticker.Stop();}
        }
    }
    // Produces 48 full native window captures plus native menu/modal captures.
    // Buttons/actions in menus and dialogs are never activated.
    public static void RunRedesign(string report,string profileFile){
        byte[] unchanged=File.ReadAllBytes(profileFile);
        var settings=Store.Json.Deserialize<Settings>(Encoding.UTF8.GetString(unchanged).TrimStart('\ufeff'));
        if(settings==null)throw new IOException("QA settings could not be read.");
        if(settings.Profiles==null)settings.Profiles=new List<Profile>();
        string folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"launcher-native-redesign");
        Directory.CreateDirectory(folder);
        var issues=new List<string>();var pageResults=new List<object>();
        var menus=new List<object>();var dialogs=new List<object>();
        int photos=0;
        using(var form=new PolishedLauncher(true,settings)){
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;
            form.Location=new Point(30,30);form.Show();Application.DoEvents();
            var scroller=Descendants(form).OfType<ContentScroller>().Single();
            var sidebar=Descendants(form).OfType<ListBox>().FirstOrDefault();
            string[] pages={"Play","Files","English","Install","Saves","Help"};
            string[] themes={"Midnight","Violet","Ocean","Daylight"};
            foreach(var size in new[]{new Size(1024,768),new Size(1200,800)}){
                form.ClientSize=size;form.AutoScrollPosition=Point.Empty;Application.DoEvents();
                var headers=form.Controls.OfType<Button>().Where(b=>pages.Contains(b.Text)).ToArray();
                if(headers.Length!=6)RedesignNote(issues,"NAV","Expected 6 native nav controls, got "+headers.Length);
                var folders=form.Controls.OfType<Button>().FirstOrDefault(b=>b.Text.StartsWith("Folders"));
                var brand=form.Controls.OfType<Label>().FirstOrDefault(b=>b.Text=="Celesphonia");
                if(folders!=null&&brand!=null&&folders.Bounds.IntersectsWith(brand.Bounds))
                    RedesignNote(issues,"OVERLAP",size.Width+"x"+size.Height+
                        " Celesphonia header overlaps Folders button: "+brand.Bounds+" vs "+folders.Bounds);
                foreach(var nav in headers){
                    Rectangle visible=form.RectangleToClient(nav.RectangleToScreen(nav.ClientRectangle));
                    if(visible.Right>size.Width||visible.Left<0)
                        RedesignNote(issues,"NAV",size.Width+"x"+size.Height+" '"+nav.Text+"' clipped initially at "+visible);
                }
                foreach(string theme in themes){
                    InvokePrivate(form,"ApplyTheme",theme);Application.DoEvents();
                    foreach(string page in pages){
                        form.TestPage(page);Application.DoEvents();
                        scroller.Position=0;Application.DoEvents();
                        string key=size.Width+"x"+size.Height+"-"+theme+"-"+page;
                        var findings=issues.Count;
                        AuditPageControls(form,scroller,page,key,issues);
                        if(page=="Install"&&size.Width==1024){
                            var method=Descendants(scroller.Canvas).OfType<ChoicePicker>()
                                .FirstOrDefault(p=>p.AccessibleName=="Installation method");
                            if(method!=null){
                                Rectangle visible=form.RectangleToClient(method.RectangleToScreen(method.ClientRectangle));
                                if(visible.Right>form.ClientSize.Width-2)
                                    RedesignNote(issues,"REACH",key+" source method dropdown arrow starts beyond "+
                                        "right viewport edge; requires horizontal scrolling: "+visible);
                            }
                        }
                        var image=Path.Combine(folder,CleanFileToken(key)+".png");
                        try{NativePng(form,image);photos++;}
                        catch(Exception e){RedesignNote(issues,"CAPTURE",key+" "+e.Message);}
                        pageResults.Add(new{page=page,theme=theme,width=size.Width,height=size.Height,
                            scrollerLimit=scroller.Limit,issuesAdded=issues.Count-findings,screenshot=image});
                    }
                    form.TestPage("Install");Application.DoEvents();
                    var selectors=Descendants(form).OfType<ChoicePicker>().Where(x=>x.Visible).ToArray();
                    foreach(var picker in selectors.Where(x=>x.AccessibleName=="Version to install"||
                                                             x.AccessibleName=="Installation method")){
                        NativePopup(form,picker,size.Width+"-"+theme+"-"+CleanFileToken(picker.AccessibleName),
                            picker.AccessibleName=="Version to install"?"Three":"Original",
                            folder,issues,menus,()=>picker.PerformClick());
                    }
                    form.TestPage("Play");Application.DoEvents();
                    var managed=sidebar==null?null:sidebar.Items.OfType<Profile>().FirstOrDefault();
                    if(managed!=null){
                        var manage=Descendants(form).OfType<Button>().FirstOrDefault(x=>x.Text.StartsWith("Manage / Uninstall"));
                        if(manage!=null)NativePopup(form,manage,size.Width+"-"+theme+"-management-menu",
                            "Uninstall managed game files",folder,issues,menus,
                            ()=>InvokePrivate(form,"ShowManageMenu",manage,managed,null));
                    }else RedesignNote(issues,"MENU","No registered game available for native management right-click capture");
                }
                if(size.Width==1024){
                    var beforeX=form.AutoScrollPosition.X;
                    form.AutoScrollPosition=new Point(Math.Max(0,1180-size.Width),0);Application.DoEvents();
                    var help=form.Controls.OfType<Button>().FirstOrDefault(b=>b.Text=="Help");
                    if(help!=null){
                        Rectangle now=form.RectangleToClient(help.RectangleToScreen(help.ClientRectangle));
                        if(now.Left<0||now.Right>form.ClientSize.Width)
                            RedesignNote(issues,"NAV","1024px Help remains unreachable after horizontal scroll: "+now);
                    }
                    if(beforeX==form.AutoScrollPosition.X)
                        RedesignNote(issues,"NAV","1024px horizontal viewport did not move to reach nav");
                    form.AutoScrollPosition=Point.Empty;Application.DoEvents();
                }
            }
            form.ClientSize=new Size(1200,800);form.AutoScrollPosition=Point.Empty;
            InvokePrivate(form,"ApplyTheme","Midnight");form.TestPage("Install");Application.DoEvents();
            NativeModal(form,"combine-original-sources",folder,issues,dialogs,
                ()=>InvokePrivate(form,"ChooseOriginalInputs"),"Use these sources");
            NativeModal(form,"select-complete-creator-bundle",folder,issues,dialogs,
                ()=>InvokePrivate(form,"PickBundle",RecipeSupport.Catalog().Recipes.First(x=>UpgradeSupport.IsUpgrade(x.Id))),"Choose archive");
            NativeModal(form,"starting-version-selection",folder,issues,dialogs,
                ()=>InvokePrivate(form,"ChooseStartingVersion"),"Choose full archive");
            var safeGame=sidebar==null?null:sidebar.Items.OfType<Profile>().FirstOrDefault();
            if(safeGame!=null)NativeModal(form,"safe-removal-chooser",folder,issues,dialogs,
                ()=>InvokePrivate(form,"RemoveVersion",safeGame,null),"Review file uninstall");
            else RedesignNote(issues,"DIALOG","No registered game present for removal chooser QA");
            form.Close();
        }
        bool inputUnchanged=unchanged.SequenceEqual(File.ReadAllBytes(profileFile));
        if(!inputUnchanged)RedesignNote(issues,"SOURCE",
            "Profile fixture bytes changed while native QA ran; possible concurrent writer. "+
            "Do not claim input immutability until rerun with an isolated copied fixture.");
        // Preview-only settings may change theme and selection; disk settings
        // and original game/save directories are never written.
        File.WriteAllText(report,Store.Json.Serialize(new{
            status=issues.Count==0?"PASS":"REVIEW",sizes=new[]{"1024x768","1200x800"},
            themes=new[]{"Midnight","Violet","Ocean","Daylight"},pages=pageResults,
            nativePageScreenshots=photos,menus=menus,modals=dialogs,issues=issues,
            settingsBytesPreserved=inputUnchanged,gameFilesWritten=0,userSavesWritten=0,
            installationExecuted=false,removalExecuted=false}),new UTF8Encoding(false));
    }
}
