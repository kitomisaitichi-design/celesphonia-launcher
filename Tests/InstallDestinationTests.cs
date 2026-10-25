using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Security.Cryptography;

// Real recovery and launcher controls; tiny synthetic originals, no user games/saves.
class InstallDestinationTests {
    static Assembly assembly;static JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=32*1024*1024};
    static List<object> checks=new List<object>();static BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static object Get(object value,string name){var type=value is Type?(Type)value:value.GetType();var p=type.GetProperty(name,flags);return p!=null?p.GetValue(value is Type?null:value,null):type.GetField(name,flags).GetValue(value is Type?null:value);}
    static object Call(string type,string name,params object[] values){return assembly.GetType(type,true).GetMethods(flags).Single(x=>x.Name==name&&x.GetParameters().Length==values.Length&&x.GetParameters().Select((p,i)=>values[i]==null||p.ParameterType.IsInstanceOfType(values[i])).All(ok=>ok)).Invoke(null,values);}
    static void Check(string name,bool ok){checks.Add(new{name,pass=ok});if(!ok)throw new Exception(name);}
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static object Settings(string location,bool? manual=null){return json.Deserialize(json.Serialize(new{InstallRoot=location,InstallRootUserSelected=manual,Profiles=new object[0]}),assembly.GetType("Settings",true));}
    [STAThread] static int Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        string exe=Path.GetFullPath(args[0]),outFile=Path.GetFullPath(args[1]),root=Path.GetFullPath(args[2]);bool fixedBuild=args[3]=="after";
        var evidence=new Dictionary<string,object>();string error=null;
        try{
            assembly=Assembly.LoadFrom(exe);string home=(string)Get(assembly.GetType("Store"),"Home");Check("fixture binds the exact launcher home",home==Path.GetDirectoryName(exe));
            string data=Path.Combine(root,"www/data/System.json"),plugin=Path.Combine(root,"www/js/plugins/fixture.js");
            Directory.CreateDirectory(Path.GetDirectoryName(data));Directory.CreateDirectory(Path.GetDirectoryName(plugin));
            if(!File.Exists(data))File.WriteAllText(data,"{\"gameTitle\":\"Destination fixture\",\"variables\":[],\"switches\":[]}",new UTF8Encoding(false));
            if(!File.Exists(plugin))File.WriteAllText(plugin,"// original source fixture\n",new UTF8Encoding(false));
            var originals=new[]{data,plugin}.ToDictionary(x=>x,Hash);
            string unsafeRoot=Path.Combine(home,"Game Profiles");Check("unsafe fixture is not already created",!Directory.Exists(unsafeRoot));
            var files=new[]{new{Path="www/data/System.json",Hash=Hash(data),Size=new FileInfo(data).Length},new{Path="www/js/plugins/fixture.js",Hash=Hash(plugin),Size=new FileInfo(plugin).Length}};
            var reference=json.Deserialize(json.Serialize(new{Format=1,VersionId="destination-fixture",Files=files}),assembly.GetType("MixedNativeReference"));
            var probe=Call("MixedSourceRecovery","Probe",reference,new[]{root},CancellationToken.None,false);
            Check("complete exact-byte source report",(bool)Get(probe,"Complete")&&(int)Get(probe,"FoundCount")==2);
            var method=assembly.GetType("PolishedLauncher").GetMethods(flags).Single(x=>x.Name=="InstallRecoveredVersion"&&x.GetParameters().Length==3&&x.GetParameters()[1].ParameterType.Name=="MixedRecoveryReport");
            string guard=null;try{method.Invoke(null,new[]{(object)"destination-fixture",probe,unsafeRoot});}catch(TargetInvocationException e){guard=e.GetBaseException().Message;}
            Check("nested source destination is rejected",guard!=null&&guard.IndexOf("source",StringComparison.OrdinalIgnoreCase)>=0);
            evidence["guardError"]=guard;evidence["createdUnsafeParentBeforeError"]=Directory.Exists(unsafeRoot);
            if(fixedBuild)Check("reject before creating a source-tree install folder",!Directory.Exists(unsafeRoot));
            else Check("baseline reproduces premature empty-folder creation",Directory.Exists(unsafeRoot));
            if(Directory.Exists(unsafeRoot))Directory.Delete(unsafeRoot,false);
            object settings=Settings(unsafeRoot);
            var directoriesBefore=Directory.GetDirectories(Path.GetDirectoryName(root)).OrderBy(x=>x).ToArray();
            using(var form=(Form)Activator.CreateInstance(assembly.GetType("PolishedLauncher"),new[]{(object)true,settings})){
                form.ShowInTaskbar=false;form.Opacity=0;
                var targets=Get(form,"targets");var items=(IEnumerable)Get(targets,"Items");object target=items.Cast<object>().First(x=>(string)Get(x,"Id")=="three-in-one-3-3-5-v1-061");
                targets.GetType().GetProperty("SelectedItem").SetValue(targets,target,null);
                var methods=Get(form,"methods");methods.GetType().GetProperty("SelectedIndex").SetValue(methods,1,null);
                form.GetType().GetMethod("AddOriginalInput",flags).Invoke(form,new object[]{root});form.GetType().GetMethod("UpdateInstall",flags).Invoke(form,null);form.GetType().GetMethod("ShowPage",flags).Invoke(form,new object[]{"Install"});
                form.Show();var until=DateTime.UtcNow.AddMilliseconds(300);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}
                var panel=(Control)Get(form,"main");string image=Path.ChangeExtension(outFile,"png");using(var bitmap=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(bitmap,panel.ClientRectangle);bitmap.Save(image);}
                evidence["image"]=image;evidence["installRoot"]=(string)Get(settings,"InstallRoot");evidence["destinationText"]=((Label)Get(form,"installLocation")).Text;
                Check("native Install page has destination text",!string.IsNullOrEmpty((string)evidence["destinationText"]));form.Close();
            }
            if(fixedBuild){
                string resolved=(string)Get(settings,"InstallRoot");
                Check("default moves outside source folder",!(bool)Call("InstallDestination","Within",resolved,root));
                Check("safe suggestion creates no directory",directoriesBefore.SequenceEqual(Directory.GetDirectories(Path.GetDirectoryName(root)).OrderBy(x=>x)));
                Check("profile list remains empty",((IList)Get(settings,"Profiles")).Count==0);
                int picker=0;Func<string,string> cancel=p=>{picker++;return null;};
                Check("safe default resolves without a picker",(string)Call("InstallDestination","Resolve",settings,new[]{root},home,cancel)==resolved&&picker==0);
                Check("safe root remains unchanged on repeat",(string)Call("InstallDestination","Resolve",settings,new[]{root},home,cancel)==resolved);
                var stale=Settings(unsafeRoot);Call("InstallDestination","Resolve",stale,new[]{root},home,cancel);Check("previously saved automatic default repairs",(string)Get(stale,"InstallRoot")==resolved);
                var manual=Settings(Path.Combine(root,"Custom destination"),true);string before=json.Serialize(manual);
                object canceled=Call("InstallDestination","Resolve",manual,new[]{root},home,cancel);evidence["cancelBefore"]=before;evidence["cancelAfter"]=json.Serialize(manual);evidence["cancelResult"]=canceled;
                var oldState=json.Deserialize<Dictionary<string,object>>(before);var newState=json.Deserialize<Dictionary<string,object>>(json.Serialize(manual));
                Check("cancel custom destination selection",canceled==null&&oldState.Count==newState.Count&&oldState.All(x=>newState.ContainsKey(x.Key)&&json.Serialize(x.Value)==json.Serialize(newState[x.Key])));
                string custom=Path.Combine(Path.GetDirectoryName(root),"Chosen profiles");Func<string,string> choose=p=>custom;
                Check("manual choice selects safe location",(string)Call("InstallDestination","Resolve",manual,new[]{root},home,choose)==custom&&(bool)Get(manual,"InstallRootUserSelected"));
                Check("existing valid custom location is retained",(string)Call("InstallDestination","Resolve",manual,new[]{root},home,cancel)==custom);
                var legacy=Settings(custom);Check("legacy custom preference stays valid",(string)Call("InstallDestination","Resolve",legacy,new[]{root},home,cancel)==custom);
                var existingProfile=json.Deserialize(json.Serialize(new{InstallRoot=unsafeRoot,Profiles=new[]{new{Id="retained",Name="Existing game",Root=root,SavePath=Path.Combine(root,"save"),Exe="Game.exe",RuntimeControls=true}},SelectedId="retained",LauncherTheme="Midnight"}),assembly.GetType("Settings"));
                Call("InstallDestination","Resolve",existingProfile,new[]{root},home,cancel);object retained=((IList)Get(existingProfile,"Profiles"))[0];
                Check("existing game and save paths stay unchanged",(string)Get(retained,"Root")==root&&(string)Get(retained,"SavePath")==Path.Combine(root,"save")&&(string)Get(existingProfile,"SelectedId")=="retained"&&(bool)Get(retained,"RuntimeControls"));
                Check("case and trailing slash containment",(bool)Call("InstallDestination","Conflicts",unsafeRoot.ToUpperInvariant()+"\\",new[]{root.ToLowerInvariant()+"\\"}));
                Check("prefix sibling is independent",(bool)Call("InstallDestination","Safe",root+"-sibling\\profiles",new[]{root}));
                Check("parent profile library allows sibling installs",(bool)Call("InstallDestination","Safe",Path.GetDirectoryName(root),new[]{root}));
                Check("drive-root source protects entire drive",!(bool)Call("InstallDestination","Safe",unsafeRoot,new[]{Path.GetPathRoot(root)}));
                string archive=Path.Combine(Path.GetDirectoryName(root),"original.rar");File.WriteAllText(archive,"archive fixture");
                Check("archive file is not a source folder",(bool)Call("InstallDestination","Safe",Path.GetDirectoryName(archive),new[]{archive}));
                Check("file cannot be an install folder",!(bool)Call("InstallDestination","Safe",archive,new string[0]));
                string other=Path.Combine(Path.GetDirectoryName(root),"Creator source");Directory.CreateDirectory(other);
                Check("all folder contributors are protected",!(bool)Call("InstallDestination","Safe",Path.Combine(other,"profiles"),new[]{root,other}));
                string broad=Path.GetDirectoryName(root);string outside=(string)Call("InstallDestination","Suggest",unsafeRoot,new[]{broad});
                Check("broad recursive source chooses outside it",outside!=null&&!(bool)Call("InstallDestination","Within",outside,broad));
                var invalid=Settings(Path.Combine(root,"Manual"),true);Func<string,string> bad=p=>unsafeRoot;bool rejected=false;try{Call("InstallDestination","Resolve",invalid,new[]{root},home,bad);}catch(TargetInvocationException e){rejected=e.InnerException is IOException;}
                Check("unsafe picker result cannot be accepted",rejected&&!Directory.Exists(unsafeRoot));
                Directory.CreateDirectory(resolved);string destination=Path.Combine(resolved,"Recovered-"+Guid.NewGuid().ToString("N").Substring(0,8));
                Call("MixedSourceRecovery","Recover",probe,destination,CancellationToken.None);
                Check("independent recovery succeeds with exact hashes",files.All(x=>Hash(Path.Combine(destination,x.Path))==x.Hash));
                bool existing=false;try{Call("MixedSourceRecovery","Recover",probe,destination,CancellationToken.None);}catch(TargetInvocationException e){existing=e.InnerException is IOException;}
                Check("existing game destination remains protected",existing);
                evidence["recoveryDestination"]=destination;
            }
            Check("original source bytes unchanged",originals.All(x=>Hash(x.Key)==x.Value));evidence["sourceNativeHashesUnchanged"]=true;
        }catch(Exception e){error=e.ToString();}
        evidence["status"]=error==null?"PASS":"FAIL";evidence["checks"]=checks;evidence["error"]=error;evidence["userSavesLoaded"]=false;evidence["userSavesWritten"]=false;evidence["fullGameInstallVerified"]=false;
        File.WriteAllText(outFile,json.Serialize(evidence),new UTF8Encoding(false));return error==null?0:1;
    }
}
