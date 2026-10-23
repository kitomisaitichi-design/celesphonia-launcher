using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;
class NotesReaderSmoke {
    [STAThread] static void Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));var type=assembly.GetType("NotesReader",true);
        var form=(Form)Activator.CreateInstance(type,new object[]{1});form.ShowInTaskbar=false;form.Opacity=0;
        Action<int> select=index=>type.GetMethod("SelectSource").Invoke(form,new object[]{index});
        var browser=form.Controls.OfType<WebBrowser>().Single();var timer=new Timer{Interval=150};
        int stage=0,ticks=0;var checks=new List<object>();string error=null;
        Action<string,bool> check=(name,ok)=>{checks.Add(new{name,pass=ok});if(!ok)throw new Exception(name);};
        timer.Tick+=delegate{
            try{
                if(++ticks>100)throw new Exception("Reader document timeout at stage "+stage);
                if(browser.ReadyState!=WebBrowserReadyState.Complete||browser.Document==null||browser.Document.Body==null)return;
                if(stage==0){
                    check("launcher history renders",browser.Document.Body.InnerText.Contains("1.0.21")&&browser.Document.Body.InnerText.Contains("1.0.1"));
                    check("current launcher history renders",browser.Document.Body.InnerText.Contains("1.0.24")&&browser.Document.Body.InnerText.Contains("Locked description"));
                    check("all twelve comparison images render",browser.Document.Images.Count==12&&browser.Document.Images.Cast<HtmlElement>().All(x=>x.OffsetRectangle.Width>0&&x.OffsetRectangle.Height>0));
                    check("no scripts in local notes",browser.Document.GetElementsByTagName("script").Count==0);
                    form.ClientSize=new Size(680,500);stage++;ticks=0;return;
                }
                if(stage==1){
                    check("narrow reader has no horizontal overflow",browser.Document.Body.ScrollRectangle.Width<=browser.ClientSize.Width);
                    check("long notes are scrollable",browser.Document.Body.ScrollRectangle.Height>browser.ClientSize.Height);
                    select(2);stage++;ticks=0;return;
                }
                if(stage==2){
                    check("developer notes remain separate",browser.Document.Body.InnerText.Contains("Ver1.053")&&!browser.Document.Body.InnerText.Contains("1.0.21"));
                    var body=browser.Document.Body.InnerText;
                    check("developer releases in chronological order",body.IndexOf("v1.01 —")<body.IndexOf("v1.052 —")&&body.IndexOf("v1.052 —")<body.IndexOf("Ver1.053 —"));
                    check("creator original change numbers retained",browser.Document.GetElementsByTagName("li").Cast<HtmlElement>().Any(x=>x.GetAttribute("value")=="203"));
                    check("developer headings have anchors",browser.Document.GetElementById("section-2")!=null);
                    select(0);stage++;ticks=0;return;
                }
                if(stage==3){
                    check("quick start and images render",browser.Document.Body.InnerText.Contains("Download and update")&&browser.Document.Images.Count==4);
                    select(1);select(1);stage++;ticks=0;return;
                }
                check("repeated source selection stays valid",browser.Document.Body.InnerText.Contains("Launcher updates")&&browser.Document.Images.Count==12);
                timer.Stop();form.Close();
            }catch(Exception e){error=e.ToString();timer.Stop();form.Close();}
        };
        form.Shown+=delegate{timer.Start();};Application.Run(form);
        File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{status=error==null?"PASS":"FAIL",checks,error}));
        if(error!=null)Environment.Exit(1);
    }
}
