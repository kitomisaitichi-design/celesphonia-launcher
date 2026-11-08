using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
public static class ModelSafetyProof {
 static string live;
 public static int Main(string[] args){live=Path.GetFullPath(args[0]);AppDomain.CurrentDomain.AssemblyResolve+=delegate(object s,ResolveEventArgs e){return e.Name.StartsWith("Celesphonia Portable Switcher,")?Assembly.LoadFrom(live):null;};Assembly.LoadFrom(live);return Run(args);}
 static int Run(string[] args){var cases=new[]{new[]{"%1的%2 上升到了%3！","Le %2 de %1 est monté à %3 !","yes"},new[]{"\\c[4]HP 50\\c[0]","\\c[0]PV 50\\c[4]","no"},new[]{"HP 50","PV 60","no"},new[]{"HP+50","PV-50","no"},new[]{"HP 1,000","PV 1,000","yes"},new[]{"%1获得%2","%1 obtient %3","no"},new[]{"%1获得%2","%1 obtient %2 %2","no"},new[]{"Text en(v(1)>30)","Texte en(v(1)>40)","no"},new[]{"Text\r","Texte<br />","no"},new[]{"A\nB","Texte","no"},new[]{"\\v[1] \\v[2]","\\v[2] \\v[1]","no"}};var rows=new List<object>();foreach(var c in cases){bool value=LocalLibrary.Valid(c[0],c[1]),expected=c[2]=="yes";rows.Add(new{source=c[0],target=c[1],accepted=value,pass=value==expected});if(value!=expected)throw new Exception("Guard mismatch: "+c[0]);}if(EnglishSupport.Compatible(cases[0][0],cases[0][1]))throw new Exception("Curated English validation changed");LocalTranslation.Write(args[1],new{status="PASS",cases=rows,curatedEnglishStillStrict=true});Console.WriteLine("PASS: "+rows.Count+" control, value, sign, tag, condition and placeholder checks");return 0;}
}
