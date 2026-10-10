using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
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
                    var launch=Descendants(form).OfType<Button>().Single(b=>b.Text=="Switch & Play");
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
            var hovered=Descendants(form).OfType<SleekButton>().First(b=>b.Text==(volumeCount>0?"Switch & Play":"Play"));
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
        File.WriteAllText(report,Store.Json.Serialize(new{status="PASS",pages=pages,visibleVolumes=volumeCount,privateProfilesExcluded=privateProfilesExcluded,noDuplicateVolumes=true,buttonOverlapChecks=true,buttonCaptionFits=true,smallScreenScrolling=true,contentScrollbar=true,allPageButtonsReachable=true,navigationStaysVisible=true,settingsBytesPreserved=true,gameLaunches=0,saveWrites=0,fourThemesChecked=true,runtimeCheckboxChecked=true,newSettingsSerialize=true,playFitsWithoutScrolling=true,runtimeToggleProminent=true,nextLaunchPreferenceAvailable=true,checkboxRowClickToggles=true,canvasOffsetResets=true,hoverFadeInAndOut=true,launcherHash=Store.Hash(Path.Combine(Store.Home,"Celesphonia Portable Switcher.exe"))}));
    }
}
