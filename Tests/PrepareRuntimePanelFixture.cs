using System;
using System.IO;
using System.Reflection;
public static class PrepareRuntimePanelFixture {
    public static int Main(string[] args){try{
        var a=Assembly.LoadFrom(Path.GetFullPath(args[0]));var t=a.GetType("Profile",true);var p=Activator.CreateInstance(t);
        string home=Path.GetDirectoryName(a.Location),empty=Path.Combine(home,"empty-test-saves");Directory.CreateDirectory(empty);
        foreach(var pair in new[]{new[]{"Root",Path.GetFullPath(args[1])},new[]{"Id","panel-native-fixture"},new[]{"Name","Celesphonia UI check"},new[]{"Exe",args[2]},new[]{"SavePath",empty}})t.GetProperty(pair[0]).SetValue(p,pair[1],null);
        string app=(string)a.GetType("RuntimeSupport",true).GetMethod("Prepare").Invoke(null,new[]{p});File.WriteAllText(args[3],app);Console.WriteLine(app);return 0;
    }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
