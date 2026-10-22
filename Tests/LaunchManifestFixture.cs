using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

// Tiny synthetic games only. Never starts an engine or opens the owner's saves.
public static class LaunchManifestFixture {
    static Assembly launcher;
    static JavaScriptSerializer json = new JavaScriptSerializer();
    static List<object> checks = new List<object>();
    static string Hash(string path) { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
    static void Write(string path,object value) { Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,json.Serialize(value),new UTF8Encoding(false)); }
    static object Call(string type,string name,params object[] args) {
        try{return launcher.GetType(type,true).GetMethods().Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(null,args);}
        catch(TargetInvocationException e){throw e.InnerException;}
    }
    static void Check(string name,Action action,bool reject) {
        string error=null;try{action();}catch(Exception e){error=e.Message;}
        bool pass=reject?error!=null:error==null;
        checks.Add(new{name=name,pass=pass,error=error});
        if(!pass)throw new Exception(name+": "+(error??"unexpected acceptance"));
    }
    static object Profile(string root,string id) {
        object p=Activator.CreateInstance(launcher.GetType("Profile",true));
        foreach(var pair in new Dictionary<string,string>{{"Root",root},{"Id",id},{"Name","Manifest fixture"},{"Exe","Game.exe"},{"SavePath",Path.Combine(root,"save")}})p.GetType().GetProperty(pair.Key).SetValue(p,pair.Value,null);
        return p;
    }
    static Dictionary<string,object> Manifest(string main,object args) {
        var m=new Dictionary<string,object>{{"name","fixture"},{"main",main},{"window",new{title="Fixture"}}};
        if(args!=null)m["chromium-args"]=args;return m;
    }
    public static int Main(string[] args) {
        string report=Path.GetFullPath(args[1]);bool baseline=args.Length>2&&args[2]=="baseline";
        try {
            launcher=Assembly.LoadFrom(Path.GetFullPath(args[0]));
            string home=Path.GetDirectoryName(launcher.Location),suite=Path.Combine(home,"manifest-fixtures");
            foreach(string layout in new[]{"root","www"})foreach(string record in new[]{"layer","reconstruction","legacy"}) {
                if(baseline&&record=="legacy")continue;
                string id="hdr-"+layout+"-"+record,root=Path.Combine(suite,id),web=layout=="www"?Path.Combine(root,"www"):root;
                if(Directory.Exists(root))throw new Exception("Use a fresh private fixture directory.");
                Directory.CreateDirectory(Path.Combine(web,"js"));Directory.CreateDirectory(Path.Combine(root,"save"));
                string main=(layout=="www"?"www/":"")+"index.html",entry=Path.Combine(web,"index.html"),package=Path.Combine(root,"package.json"),script=Path.Combine(web,"js","main.js"),save=Path.Combine(root,"save","file1.rpgsave");
                File.WriteAllText(entry,"<html><head></head><body><script src=\"js/main.js\"></script></body></html>");
                File.WriteAllText(script,"// native fixture");File.WriteAllText(save,"save sentinel");File.WriteAllBytes(Path.Combine(root,"Game.exe"),new byte[]{77,90});
                Write(package,Manifest(main,null));
                var hashes=new Dictionary<string,string>{{"package.json",Hash(package)},{main,Hash(entry)},{(layout=="www"?"www/":"")+"js/main.js",Hash(script)}};
                if(record=="layer")Write(Path.Combine(root,"launcher-build.json"),new{BaseFiles=hashes,Layers=new object[0]});
                else if(record=="reconstruction")Write(Path.Combine(root,"launcher-reconstruction.json"),new{format=2,files=hashes.Select(x=>new{path=x.Key,hash=x.Value,size=new FileInfo(Path.Combine(root,x.Key)).Length}).ToArray()});
                else {
                    string basis=root+"-base";Directory.CreateDirectory(Path.Combine(basis,layout=="www"?"www":""));
                    File.Copy(package,Path.Combine(basis,"package.json"));File.Copy(entry,Path.Combine(basis,main));
                    Write(Path.Combine(home,"mod-recipes.json"),new{GorpgFingerprint=new Dictionary<string,string>{{"package.json",Hash(package)},{main,Hash(entry)}},Recipes=new object[0]});
                    Write(Path.Combine(root,"launcher-base.json"),new{BaseRoot=basis,Layers=new object[0]});
                }
                object p=Profile(root,id);Action launch=()=>Call("RecipeSupport","VerifyProfile",p,true);
                Check(id+" original",launch,false);
                Write(package,Manifest(main,"--force-color-profile=srgb"));
                Check(id+" HDR launch",launch,baseline);
                if(baseline)continue;
                if(record!="legacy")Check(id+" exact-source verification",()=>Call("RecipeSupport","VerifyProfile",p,false),true);
                string before=Hash(package),saveBefore=Hash(save),app=(string)Call("RuntimeSupport","Prepare",p);
                var generated=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(app,"package.json")));
                Check(id+" F8 preserves HDR and source",()=>{if((string)generated["chromium-args"]!="--force-color-profile=srgb"||Hash(package)!=before||Hash(save)!=saveBefore)throw new Exception("Configuration or source changed");},false);
                if(record!="legacy"){File.AppendAllText(script," changed");Check(id+" altered native script",launch,true);File.WriteAllText(script,"// native fixture");}
                File.AppendAllText(entry," changed");Check(id+" altered native entry",launch,true);File.WriteAllText(entry,"<html><head></head><body><script src=\"js/main.js\"></script></body></html>");
                Write(package,Manifest(main,42));Check(id+" invalid args type",launch,true);
                Write(package,Manifest("../outside.html",null));Check(id+" external entry",launch,true);
                File.WriteAllText(Path.Combine(web,"unrecorded.html"),"<html></html>");Write(package,Manifest((layout=="www"?"www/":"")+"unrecorded.html",null));Check(id+" unrecorded entry",launch,true);
                Write(package,Manifest("missing.html",null));Check(id+" missing entry",launch,true);
                File.WriteAllText(package,"{ malformed");Check(id+" invalid JSON",launch,true);
                Write(package,Manifest(main,"--force-color-profile=srgb"));Check(id+" restored valid config",launch,false);
                if(record=="reconstruction")Check(id+" direct reconstruction verification",()=>Call("UniversalSupport","VerifyInstalled",p,true),false);
            }
            Write(report,new{status="PASS",baseline=baseline,checks=checks});Console.WriteLine("PASS: "+checks.Count+" manifest checks");return 0;
        }catch(Exception e){Write(report,new{status="FAIL",baseline=baseline,checks=checks,error=e.ToString()});Console.Error.WriteLine(e.Message);return 1;}
    }
}
