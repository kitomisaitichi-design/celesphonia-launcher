using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Collections.Generic;
class LocalUiProbe {
 [STAThread]static int Main(string[] args){try{
  Application.EnableVisualStyles();var asm=Assembly.LoadFrom(Path.GetFullPath(args[0]));var type=asm.GetType("PolishedLauncher",true);var form=(Form)Activator.CreateInstance(type,new object[]{true,null});form.ShowInTaskbar=false;form.Show();type.GetMethod("TestPage").Invoke(form,new object[]{args[1]});Application.DoEvents();
  form.ClientSize=new Size(1180,820);Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(args[2]+".normal.png");}
  if(args[1]=="Local TL"){
   form.ClientSize=new Size(960,640);Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(args[2]+".small.png");}
   var ui=Find(form,"LocalTranslationPage");if(ui==null||form.RectangleToClient(ui.RectangleToScreen(ui.ClientRectangle)).Right>form.ClientSize.Width)throw new Exception("Local page not visible/contained");
   if(form.HorizontalScroll.Visible)throw new Exception("Redundant outer horizontal scroll");
   foreach(Control child in ui.Controls)if(child.Visible&&form.RectangleToClient(child.RectangleToScreen(child.ClientRectangle)).Right>form.ClientSize.Width)throw new Exception("Local action outside viewport: "+child.Text);
   var detail=ui.Controls.OfType<Panel>().FirstOrDefault();if(detail!=null)foreach(Control child in detail.Controls)if(child.Visible&&(child.Bottom>detail.Height||child.Right>detail.Width))throw new Exception("Local control clipped: "+child.GetType().Name+" "+child.Text);
   var navigation=form.Controls.OfType<Button>().Where(b=>new[]{"Play","Files","English","Local TL","Install","Saves","Help"}.Contains(b.Text)).ToArray();if(navigation.Length!=7||navigation.Any(b=>b.Right>form.ClientSize.Width))throw new Exception("Navigation clips");
  }
  if(args.Length>3&&args[3]=="models")using(var models=(Form)Activator.CreateInstance(asm.GetType("LocalModelsDialog",true),new object[]{Color.FromArgb(15,17,25),Color.FromArgb(27,30,42),Color.White,Color.Silver,Color.FromArgb(100,125,245)})){
   models.Show(form);Application.DoEvents();foreach(Control c in models.Controls)if(c.Visible&&(c.Right>models.ClientSize.Width||c.Bottom>models.ClientSize.Height))throw new Exception("Model manager control clips: "+c.Text);
   using(var image=new Bitmap(models.Width,models.Height)){models.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(args[2]+".models.png");}models.Close();
  }
  File.WriteAllText(args[2]+".json","{\"status\":\"PASS\",\"page\":\""+args[1]+"\",\"nativeWindow\":true}");form.Close();form.Dispose();return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static Control Find(Control root,string name){foreach(Control c in root.Controls){if(c.GetType().Name==name)return c;var result=Find(c,name);if(result!=null)return result;}return null;}
}
