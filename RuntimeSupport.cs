using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

public static class RuntimeSupport {
    static byte[] Resource(string name) {
        using(var s=typeof(RuntimeSupport).Assembly.GetManifestResourceStream("Celesphonia."+name)) {
            if(s==null)throw new IOException("Runtime controls are missing from this launcher.");
            using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}
        }
    }
    public static void CarryForward(Profile source,Profile target,bool carrySaves) {
        string parent=Path.Combine(Store.Home,"Control Sessions"),from=Store.Child(parent,source.Id),to=Store.Child(parent,target.Id);
        string sourceSaves=Path.Combine(from,"save"),preferences=Path.Combine(from,"panel-settings.json");
        bool saves=carrySaves&&Directory.Exists(sourceSaves)&&Store.SaveFiles(sourceSaves).Length>0;
        if(!saves&&!File.Exists(preferences))return;
        if(Directory.Exists(to))throw new IOException("The upgrade's control session already exists; preserve it and choose a new destination.");
        string stage=Store.Child(parent,target.Id+"-upgrade-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        try {
            if(File.Exists(preferences)) {
                if((File.GetAttributes(preferences)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked control preferences cannot be migrated.");
                File.Copy(preferences,Path.Combine(stage,"panel-settings.json"));
            }
            if(saves) {
                Store.CheckLinks(sourceSaves);
                var branch=new Profile{Id=source.Id+"-controls",Name=source.Name+" runtime controls",Root=source.Root,SavePath=sourceSaves};
                string backup=Store.Backup(branch);Store.VerifyBackup(backup);
                string destination=Path.Combine(stage,"save");Directory.CreateDirectory(destination);
                foreach(string f in Store.SaveFiles(backup)) {
                    string targetFile=Path.Combine(destination,Path.GetFileName(f));File.Copy(f,targetFile);
                    if(Store.Hash(f)!=Store.Hash(targetFile))throw new IOException("Runtime-save migration verification failed.");
                }
                File.WriteAllText(Path.Combine(stage,"seed.json"),Store.Json.Serialize(new{source=target.SavePath,upgradedFrom=source.Id,sourceBackup=backup}),new UTF8Encoding(false));
            }
            Directory.Move(stage,to);
        } finally {if(Directory.Exists(stage))ExtendedSafeCleanup.RemoveTreeSafely(stage);}
    }
    public static string Prepare(Profile p) {
        string home=Store.Child(Path.Combine(Store.Home,"Control Sessions"),p.Id);
        string app=Path.Combine(home,"app"),saves=Path.Combine(home,"save");
        Directory.CreateDirectory(app);Directory.CreateDirectory(saves);
        // Some shipped NW.js executables prioritize their own package.json even
        // over --nwapp. Run a private copy of the engine beside our own manifest.
        foreach(string f in Directory.GetFiles(p.Root)) {
            string ext=Path.GetExtension(f).ToLowerInvariant();
            if(Path.GetFileName(f).Equals(p.Exe,StringComparison.OrdinalIgnoreCase)||new[]{".dll",".pak"}.Contains(ext)||new[]{"icudtl.dat","snapshot_blob.bin","natives_blob.bin","v8_context_snapshot.bin"}.Contains(Path.GetFileName(f).ToLowerInvariant())) {
                string target=Path.Combine(app,Path.GetFileName(f));
                if(!File.Exists(target)||new FileInfo(f).Length!=new FileInfo(target).Length||File.GetLastWriteTimeUtc(f)!=File.GetLastWriteTimeUtc(target))File.Copy(f,target,true);
            }
        }
        foreach(string sub in new[]{"locales","swiftshader"}) {
            string from=Path.Combine(p.Root,sub);if(Directory.Exists(from))Store.CopyTree(from,Path.Combine(app,sub),false);
        }
        // Seed once. Later launches keep their independent edited save set.
        string seed=Path.Combine(home,"seed.json");
        if(!File.Exists(seed)) {
            foreach(string f in Store.SaveFiles(p.SavePath))File.Copy(f,Path.Combine(saves,Path.GetFileName(f)),false);
            File.WriteAllText(seed,Store.Json.Serialize(new{source=p.SavePath,created=DateTimeOffset.Now.ToString("o")}),new UTF8Encoding(false));
        }
        var manifest=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(p.Root,"package.json"),Encoding.UTF8));
        string web=Store.WebRoot(p.Root),entry=Store.Child(p.Root,Convert.ToString(manifest["main"]));
        // Launch the current verified entry, including its enabled English overlay.
        // OriginalFile is only for reconstruction; using it here silently turns English off.
        EnglishSupport.Verify(p.Root);
        string html=File.ReadAllText(entry,Encoding.UTF8);
        if(Regex.IsMatch(html,@"<base\b",RegexOptions.IgnoreCase))throw new IOException("This game entry already defines a base URL; a private session cannot safely remount it.");
        var head=Regex.Match(html,@"<head\b[^>]*>",RegexOptions.IgnoreCase);
        if(!head.Success)throw new IOException("This game needs a standard RPG Maker HTML entry.");
        string baseUrl=new Uri(web.TrimEnd('\\','/')+Path.DirectorySeparatorChar).AbsoluteUri;
        html=html.Insert(head.Index+head.Length,"\n<base href=\""+baseUrl+"\">\n");
        var mainScript=Regex.Match(html,@"<script\b[^>]*\bsrc\s*=\s*['""]js/main\.js['""][^>]*>",RegexOptions.IgnoreCase);
        if(!mainScript.Success)throw new IOException("Runtime controls need a standard js/main.js entry.");
        html=html.Insert(mainScript.Index,"<script src=\""+new Uri(Path.Combine(app,"controls.js")).AbsoluteUri+"\"></script>\n"+
            "<script src=\""+new Uri(Path.Combine(app,"stream-repeat-rescue.js")).AbsoluteUri+"\"></script>\n");
        // Resolve relative data and plugin requests against the original web root.
        // No native data, scripts, entry pages or source saves are written here.
        File.WriteAllText(Path.Combine(app,"index.html"),html,new UTF8Encoding(false));
        string id=UniversalSupport.Identify(p.Root);
        var dictionary=new Dictionary<string,string>();
        if(id!=null&&id!="ch-current")try{dictionary=Store.Json.Deserialize<Dictionary<string,string>>(Encoding.UTF8.GetString(EnglishRelease.Read(id,"dictionary.json")));}catch(IOException){}
        string aliases=Path.Combine(Store.Home,"English",id??"runtime-generic","user-translations.json");
        if(File.Exists(aliases))foreach(var v in Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(aliases,Encoding.UTF8)))dictionary[v.Key]=v.Value;
        // Independent mod copies may retain a historical BaseRoot that was
        // deleted after installation. It is an optional resource fallback,
        // never a requirement for starting the fully installed game.
        string baseWeb=OptionalBaseWeb(p.Root);
        string panel=Path.Combine(app,"panel.html");File.WriteAllText(panel,"<!doctype html><html><head><meta charset=\"utf-8\"><title>Runtime controls</title></head><body></body></html>",new UTF8Encoding(false));
        var config=new{panel=new Uri(panel).AbsoluteUri,root=p.Root,web=web,baseWeb=baseWeb,home=home,save=saves,profile=p.Name,version=id??"unrecognized",dictionary=dictionary};
        string prefix="window.CelesphoniaControlConfig="+Store.Json.Serialize(config).Replace("\u2028","\\u2028").Replace("\u2029","\\u2029")+";\n";
        File.WriteAllText(Path.Combine(app,"controls.js"),prefix+Encoding.UTF8.GetString(Resource("RuntimeOfflineLabels.js"))+"\n"+Encoding.UTF8.GetString(Resource("RuntimePanel.js")),new UTF8Encoding(false));
        // The RPG Maker MPP_ChoiceEX PC menu can leave Window_Message paused
        // after clearing a choice. Restore only the pending repeat-stream menu.
        File.WriteAllBytes(Path.Combine(app,"stream-repeat-rescue.js"),Resource("StreamRepeatChoiceRescue.js"));
        manifest["name"]="celesphonia-controls-"+p.Id;manifest["main"]="index.html";
        manifest.Remove("node-main");manifest.Remove("inject_js_start");manifest.Remove("inject-js-start");manifest.Remove("inject-js-end");
        manifest.Remove("inject_js_end");
        if(manifest.ContainsKey("window")) {
            var w=manifest["window"] as Dictionary<string,object>;
            if(w!=null){w["title"]=p.Name+" · Runtime controls";if(w.ContainsKey("icon"))w["icon"]=new Uri(Path.Combine(p.Root,Convert.ToString(w["icon"]))).AbsoluteUri;}
        }
        File.WriteAllText(Path.Combine(app,"package.json"),Store.Json.Serialize(manifest),new UTF8Encoding(false));
        return app;
    }
    public static string OptionalBaseWeb(string root){
        string record=Path.Combine(root,"launcher-base.json");
        if(!File.Exists(record))return null;
        try{
            var layer=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(record,Encoding.UTF8));
            object raw;
            if(layer==null||!layer.TryGetValue("BaseRoot",out raw)||raw==null)return null;
            string basis=Convert.ToString(raw);
            return !string.IsNullOrWhiteSpace(basis)&&Directory.Exists(basis)?Store.WebRoot(basis):null;
        }catch(Exception e){
            if(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||
               e is InvalidOperationException||e is NotSupportedException||
               e is System.Security.SecurityException)return null;
            throw;
        }
    }
    public static Process Launch(Profile p,Action<string> progress) {
        var detected=Store.Detect(p.Root,p.Name);
        if(!File.Exists(Store.Child(p.Root,p.Exe)))p.Exe=detected.Exe;
        if(progress!=null)progress("Preparing untouched-game runtime controls…");
        EnglishSupport.RefreshEnabled(p,progress);
        RecipeSupport.VerifyProfile(p,true);
        string app=Prepare(p),home=Path.GetDirectoryName(app);
        var started=Process.Start(new ProcessStartInfo(Store.Child(app,detected.Exe),"--user-data-dir="+Store.Q(Path.Combine(home,"browser"))+" --nwapp="+Store.Q(app)){WorkingDirectory=app,UseShellExecute=false});
        if(started==null||started.WaitForExit(1500))throw new IOException("The private runtime session could not start. The original game has not been edited.");
        return started;
    }
}
