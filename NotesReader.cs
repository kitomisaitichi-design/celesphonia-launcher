using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

// Local, embedded release notes. No network content, scripts, or game access.
public sealed class NotesReader : Form {
    readonly WebBrowser reader=new WebBrowser();
    readonly ComboBox chapters=new ComboBox();
    readonly List<SleekButton> sources=new List<SleekButton>();
    readonly List<string> anchors=new List<string>();
    bool loading;
    public NotesReader(int source){
        Text="Celesphonia · Notes & changelogs";StartPosition=FormStartPosition.CenterParent;
        ClientSize=new Size(1000,760);MinimumSize=new Size(680,500);AutoScaleMode=AutoScaleMode.Dpi;
        Font=new Font("Segoe UI",10);BackColor=LauncherPaint.Ink;ForeColor=LauncherPaint.Paper;
        var top=new TableLayoutPanel{Dock=DockStyle.Top,Height=116,Padding=new Padding(20,12,20,10),ColumnCount=1,RowCount=2,BackColor=BackColor};
        top.RowStyles.Add(new RowStyle(SizeType.Absolute,49));top.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
        var tabs=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
        string[] titles={"Start here","Launcher updates","Game developer notes"};
        for(int i=0;i<titles.Length;i++){
            int index=i;var b=new SleekButton{Text=titles[i],Navigation=true,Size=new Size(i==2?224:180,40),Margin=new Padding(0,0,10,0),Font=Font,BackColor=LauncherPaint.Surface,ForeColor=ForeColor,Cursor=Cursors.Hand};
            b.Click+=delegate{SelectSource(index);};tabs.Controls.Add(b);sources.Add(b);
        }
        var jump=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        jump.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));jump.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        jump.Controls.Add(new Label{Text="Jump to section",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=LauncherPaint.Muted});
        chapters.Dock=DockStyle.Fill;chapters.DropDownStyle=ComboBoxStyle.DropDownList;chapters.AccessibleName="Notes section";
        chapters.SelectedIndexChanged+=delegate{if(!loading&&chapters.SelectedIndex>=0&&reader.Document!=null){var el=reader.Document.GetElementById(anchors[chapters.SelectedIndex]);if(el!=null)el.ScrollIntoView(true);}};
        jump.Controls.Add(chapters);top.Controls.Add(tabs);top.Controls.Add(jump);
        reader.Dock=DockStyle.Fill;reader.AllowWebBrowserDrop=false;reader.IsWebBrowserContextMenuEnabled=false;reader.WebBrowserShortcutsEnabled=true;
        reader.Navigating+=delegate(object sender,WebBrowserNavigatingEventArgs e){
            if(e.Url.Scheme=="about")return;
            e.Cancel=true;if(e.Url.Scheme=="https")Process.Start(new ProcessStartInfo(e.Url.AbsoluteUri){UseShellExecute=true});
        };
        reader.NewWindow+=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=true;};
        Controls.Add(reader);Controls.Add(top);SelectSource(source);
    }
    static string Embedded(string name){
        using(var stream=typeof(NotesReader).Assembly.GetManifestResourceStream("Celesphonia."+name)){
            if(stream==null)throw new IOException("Bundled notes missing: "+name);
            using(var text=new StreamReader(stream,Encoding.UTF8,true))return text.ReadToEnd();
        }
    }
    public void SelectSource(int index){
        if(index<0||index>2)index=0;
        string text=index==0?Embedded("QuickStart.md"):index==1?Embedded("LauncherChangelog.md"):LauncherUpdate.ChangelogText();
        loading=true;chapters.Items.Clear();anchors.Clear();
        for(int i=0;i<sources.Count;i++){sources[i].Selected=i==index;sources[i].Invalidate();}
        string body=Markdown(text,delegate(string title,string id){chapters.Items.Add(title);anchors.Add(id);});
        if(chapters.Items.Count>0)chapters.SelectedIndex=0;loading=false;
        reader.DocumentText=Document(body);
    }
    static string Html(string text){return System.Net.WebUtility.HtmlEncode(text);}
    static string Image(string path,string alt){
        var match=Regex.Match(path,@"^(?:docs/)?images/((?:stat-layout-(?:maple|creator)|text-memory-(?:names|locked|creator|maple)|notification-(?:names|blank|title)|equipment-(?:creator|maple|weapon|dense|dense-end|terms)|destination)-(?:before|after)\.png)$");
        if(!match.Success)return "<p>"+Html(alt)+"</p>";
        using(var stream=typeof(NotesReader).Assembly.GetManifestResourceStream("Celesphonia.Notes."+match.Groups[1].Value)){
            if(stream==null)throw new IOException("Bundled notes image missing: "+path);
            using(var bytes=new MemoryStream()){stream.CopyTo(bytes);return "<figure><img alt=\""+Html(alt)+"\" src=\"data:image/png;base64,"+Convert.ToBase64String(bytes.ToArray())+"\"><figcaption>"+Html(alt)+"</figcaption></figure>";}
        }
    }
    static string Inline(string text){
        string encoded=Html(text);
        encoded=Regex.Replace(encoded,@"\*\*(.+?)\*\*","<strong>$1</strong>");
        encoded=Regex.Replace(encoded,@"`([^`]+)`","<code>$1</code>");
        return Regex.Replace(encoded,@"\[([^\]]+)\]\(([^)]+)\)",delegate(Match m){
            string href=System.Net.WebUtility.HtmlDecode(m.Groups[2].Value);Uri uri;
            if(Uri.TryCreate(href,UriKind.Absolute,out uri)&&uri.Scheme=="https")return "<a href=\""+Html(uri.AbsoluteUri)+"\">"+m.Groups[1].Value+"</a>";
            return m.Groups[1].Value;
        });
    }
    internal static string Markdown(string text,Action<string,string> section){
        var output=new StringBuilder();var paragraph=new StringBuilder();string list=null;int heading=0;
        Action flush=delegate{if(paragraph.Length>0){output.Append("<p>"+Inline(paragraph.ToString())+"</p>");paragraph.Length=0;}};
        foreach(string raw in text.Replace("\r","").Split('\n')){
            string line=raw.Trim();var h=Regex.Match(line,@"^(#{1,4})\s+(.+)$");
            var img=Regex.Match(line,@"^!\[([^\]]*)\]\(([^)]+)\)$");
            var numbered=Regex.Match(line,@"^(\d+)\.\s+");
            bool bullet=numbered.Success||Regex.IsMatch(line,@"^[-*]\s+");
            string nextList=bullet?(numbered.Success?"ol":"ul"):null;
            if(list!=null&&list!=nextList){output.Append("</"+list+">");list=null;}
            if(h.Success||img.Success||bullet||line.Length==0||line=="---")flush();
            if(h.Success){int level=h.Groups[1].Length;string id="section-"+(heading++);section(h.Groups[2].Value,id);output.Append("<h"+level+" id=\""+id+"\">"+Inline(h.Groups[2].Value)+"</h"+level+">");}
            else if(img.Success)output.Append(Image(img.Groups[2].Value,img.Groups[1].Value));
            else if(bullet){if(list==null){output.Append("<"+nextList+">");list=nextList;}output.Append("<li"+(numbered.Success?" value=\""+numbered.Groups[1].Value+"\"":"")+">"+Inline(Regex.Replace(line,@"^(?:[-*]|\d+\.)\s+",""))+"</li>");}
            else if(line=="---")output.Append("<hr>");
            else if(line.Length>0){if(paragraph.Length>0)paragraph.Append(' ');paragraph.Append(line);}
        }
        flush();if(list!=null)output.Append("</"+list+">");return output.ToString();
    }
    static string Css(Color color){return "#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2");}
    static string Document(string body){
        return "<!doctype html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"><meta charset=\"utf-8\"><style>"+
            "html{background:"+Css(LauncherPaint.Ink)+";color:"+Css(LauncherPaint.Paper)+";}body{font:16px/1.7 'Segoe UI',sans-serif;max-width:860px;margin:auto;padding:22px 36px 60px;word-wrap:break-word;}"+
            "h1{font-size:30px;line-height:1.3;margin:8px 0 24px;}h2{font-size:23px;margin:38px 0 14px;border-bottom:1px solid "+Css(LauncherPaint.Line)+";padding-bottom:10px;}h3{font-size:19px;color:"+Css(LauncherPaint.Gold)+";margin-top:28px;}h4{font-size:17px;}p{margin:12px 0;}li{margin:8px 0;}strong{color:"+Css(LauncherPaint.Gold)+";}a{color:"+Css(LauncherPaint.Gold)+";text-decoration:none;}a:hover{text-decoration:underline;text-shadow:0 0 8px "+Css(LauncherPaint.Gold)+";}"+
            "code{font:14px Consolas,monospace;background:"+Css(LauncherPaint.Surface)+";padding:2px 5px;border-radius:4px;}figure{display:inline-block;vertical-align:top;max-width:100%;margin:12px 18px 16px 0;padding:12px;background:"+Css(LauncherPaint.Surface)+";border:1px solid "+Css(LauncherPaint.Line)+";border-radius:12px;}img{display:block;max-width:100%;height:auto;border-radius:5px;}figcaption{font-size:13px;color:"+Css(LauncherPaint.Muted)+";margin-top:8px;}@media(max-width:720px){body{padding:18px;}figure{margin-right:0;}}"+
            "</style></head><body>"+body+"</body></html>";
    }
}
