using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class LocalRunSettings:Form {
 readonly CheckedListBox scope=new CheckedListBox();readonly ComboBox backend=new ComboBox(),device=new ComboBox();readonly NumericUpDown layers=new NumericUpDown(),threads=new NumericUpDown();readonly LocalRun run;readonly Label note=new Label();readonly SleekButton save=new SleekButton(),setup=new SleekButton();bool busy;
 public LocalRunSettings(LocalRun value,Color bg,Color card,Color text,Color accent){run=value;Text="Run settings · "+run.Name;ClientSize=new Size(580,560);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=bg;ForeColor=text;Font=new Font("Segoe UI",10);
  var options=LocalTranslation.Options(run);Controls.Add(new Label{Text="Covered text & execution",Location=new Point(24,20),Size=new Size(530,35),Font=new Font(Font,FontStyle.Bold)});
  Controls.Add(new Label{Text="Only sources already covered by our English overlay are eligible.",Location=new Point(24,61),Size=new Size(530,25)});
  scope.SetBounds(24,95,530,116);scope.BackColor=card;scope.ForeColor=text;scope.BorderStyle=BorderStyle.None;scope.CheckOnClick=true;foreach(string kind in LocalTranslation.Categories)scope.Items.Add(kind,options.Scope.Contains(kind));Controls.Add(scope);
  backend.DropDownStyle=ComboBoxStyle.DropDownList;backend.Items.AddRange(new object[]{"CPU","GPU · Vulkan","Hybrid · GPU layers + CPU remainder"});backend.SelectedIndex=options.Backend=="gpu"?1:options.Backend=="hybrid"?2:0;backend.SetBounds(24,243,530,30);Controls.Add(backend);
  device.DropDownStyle=ComboBoxStyle.DropDownList;device.Items.Add(new KeyValuePair<string,string>("","Automatic device"));device.DisplayMember="Value";device.SetBounds(24,288,530,30);Controls.Add(device);
  Controls.Add(new Label{Text="GPU layers (hybrid)",Location=new Point(24,335),Size=new Size(220,24)});layers.SetBounds(260,332,90,28);layers.Minimum=1;layers.Maximum=200;layers.Value=Math.Max(1,options.GpuLayers);Controls.Add(layers);
  Controls.Add(new Label{Text="CPU threads",Location=new Point(24,376),Size=new Size(220,24)});threads.SetBounds(260,372,90,28);threads.Minimum=1;threads.Maximum=Environment.ProcessorCount;threads.Value=Math.Max(1,Math.Min(Environment.ProcessorCount,options.Threads));Controls.Add(threads);
  note.SetBounds(24,416,530,70);note.Text="Completed work is kept. Changes apply after the current entry checkpoints. GPU/hybrid requires the optional Vulkan backend.";Controls.Add(note);
  setup.Text="Set up GPU backend";setup.SetBounds(24,503,200,36);setup.BackColor=card;setup.ForeColor=text;setup.Click+=async delegate{if(busy)return;busy=true;setup.Enabled=save.Enabled=false;try{await Task.Run(()=>LocalTranslation.Install(run.Model,true,CancellationToken.None));LoadDevices(options.Device);note.Text="Vulkan backend ready. Model weights were reused.";}catch(Exception e){note.Text=e.Message;}finally{busy=false;setup.Enabled=save.Enabled=true;}};Controls.Add(setup);
  save.Text="Save settings";save.Primary=true;save.BackColor=accent;save.ForeColor=text;save.SetBounds(374,503,180,36);save.Click+=delegate{try{var next=LocalTranslation.Options(run);next.Scope=scope.CheckedItems.Cast<string>().ToArray();next.Backend=backend.SelectedIndex==0?"cpu":backend.SelectedIndex==1?"gpu":"hybrid";next.GpuLayers=(int)layers.Value;next.Threads=(int)threads.Value;next.Device=((KeyValuePair<string,string>)device.SelectedItem).Key;LocalTranslation.SetOptions(run.Id,next);LocalLibrary.Publish(run);DialogResult=DialogResult.OK;}catch(Exception e){note.Text=e.Message;}};Controls.Add(save);AcceptButton=save;
  backend.SelectedIndexChanged+=delegate{layers.Enabled=backend.SelectedIndex==2;device.Enabled=backend.SelectedIndex>0;};layers.Enabled=backend.SelectedIndex==2;device.Enabled=backend.SelectedIndex>0;
  Shown+=async delegate{try{var rows=await Task.Run(()=>LocalBackend.Devices());if(IsDisposed)return;foreach(var row in rows)device.Items.Add(row);SelectDevice(options.Device);}catch(Exception e){note.Text="Device detection: "+e.Message;}};FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){e.Cancel=true;note.Text="Finish GPU backend setup before closing this window.";}};
 }
 void SelectDevice(string wanted){foreach(var item in device.Items.Cast<KeyValuePair<string,string>>())if(item.Key==wanted){device.SelectedItem=item;return;}device.SelectedIndex=0;}
 void LoadDevices(string wanted){foreach(var item in LocalBackend.Devices())if(!device.Items.Cast<KeyValuePair<string,string>>().Any(x=>x.Key==item.Key))device.Items.Add(item);SelectDevice(wanted);}
}
public static class LocalBackend {
 public static List<KeyValuePair<string,string>> Devices(){var result=new List<KeyValuePair<string,string>>();string exe=LocalTranslation.RuntimeExe(true);if(!File.Exists(exe))return result;var start=new ProcessStartInfo(exe,"--list-devices"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(exe)};using(var process=Process.Start(start)){var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(12000)){process.Kill();throw new IOException("GPU device enumeration timed out.");}string text=output.Result+"\n"+error.Result;foreach(Match m in Regex.Matches(text,@"(?m)^\s*(Vulkan\d+):\s*(.+)$"))result.Add(new KeyValuePair<string,string>(m.Groups[1].Value,m.Groups[2].Value.Trim()));}return result;}
}

public static class LocalTextBoxHint {
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr parameter,string text);
 public static void PlaceholderTextCompat(this TextBox box,string text){box.AccessibleName=text;box.HandleCreated+=delegate{SendMessage(box.Handle,0x1501,new IntPtr(1),text);};}
}
