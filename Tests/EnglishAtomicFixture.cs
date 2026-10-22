using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Web.Script.Serialization;
public static class EnglishAtomicFixture {
 public static int Main(string[] args){try{
  var a=Assembly.LoadFrom(Path.GetFullPath(args[0]));var atomic=a.GetType("EnglishSupport").GetMethod("Atomic",BindingFlags.Static|BindingFlags.NonPublic);
  string root=Path.Combine(Path.GetDirectoryName(a.Location),"atomic-fixture");Directory.CreateDirectory(root);string file=Path.Combine(root,"index.html");File.WriteAllText(file,"original");
  var hold=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);var release=new Thread(()=>{Thread.Sleep(250);hold.Dispose();});release.Start();var elapsed=Stopwatch.StartNew();atomic.Invoke(null,new object[]{file,System.Text.Encoding.UTF8.GetBytes("replacement")});release.Join();
  if(File.ReadAllText(file)!="replacement"||elapsed.ElapsedMilliseconds<200)throw new Exception("Transient replacement check failed");
  File.WriteAllText(file,"keep");bool failed=false;
  using(var permanent=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read))try{atomic.Invoke(null,new object[]{file,System.Text.Encoding.UTF8.GetBytes("blocked")});}catch(TargetInvocationException e){failed=e.InnerException is IOException&&e.InnerException.Message.Contains(file);}
  if(!failed||File.ReadAllText(file)!="keep"||!File.Exists(file+".new"))throw new Exception("Locked original was not preserved with an explicit error");
  File.WriteAllText(file,"before external change");var shared=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);var change=new Thread(()=>{Thread.Sleep(60);using(var edit=new FileStream(file,FileMode.Truncate,FileAccess.Write,FileShare.ReadWrite)){byte[] text=System.Text.Encoding.UTF8.GetBytes("external edit");edit.Write(text,0,text.Length);}Thread.Sleep(250);shared.Dispose();});change.Start();bool changed=false;
  try{atomic.Invoke(null,new object[]{file,System.Text.Encoding.UTF8.GetBytes("must not overwrite")});}catch(TargetInvocationException e){changed=e.InnerException is IOException&&e.InnerException.Message.Contains("changed");}change.Join();
  if(!changed||File.ReadAllText(file)!="external edit")throw new Exception("External edit was not preserved");
  File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{status="PASS",transientLockRecovered=true,permanentLockReported=true,originalPreserved=true,externalEditPreserved=true}));Console.WriteLine("PASS: bounded atomic replacement checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
