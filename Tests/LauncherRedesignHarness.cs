using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

// Test-only entry point. Compile with /main:LauncherRedesignHarness and
// -- never -- against the user's installed games as a build destination.
// The report argument must point to a disposable QA-only output directory.
public static class LauncherRedesignHarness {
    delegate bool Visitor(IntPtr window,IntPtr extra);
    [DllImport("user32.dll")] static extern bool EnumWindows(Visitor callback,IntPtr extra);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,Visitor callback,IntPtr extra);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr window,StringBuilder buffer,int limit);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window,uint message,IntPtr wparam,IntPtr lparam);
    static string WindowText(IntPtr handle){
        var buffer=new StringBuilder(2048);GetWindowText(handle,buffer,buffer.Capacity);return buffer.ToString();
    }
    static readonly HashSet<long> dismissed=new HashSet<long>();
    static void DismissOnlyErrorOK(string log){
        int current=Process.GetCurrentProcess().Id;
        EnumWindows(delegate(IntPtr window,IntPtr extra){
            uint pid;
            GetWindowThreadProcessId(window,out pid);
            if(pid!=current||WindowText(window)!="Celesphonia Launcher")return true;
            var children=new List<Tuple<IntPtr,string>>();
            EnumChildWindows(window,delegate(IntPtr control,IntPtr ignored){
                children.Add(Tuple.Create(control,WindowText(control)));
                return true;
            },IntPtr.Zero);
            var ok=children.Where(x=>x.Item2=="OK").ToArray();
            var destructive=children.Where(x=>x.Item2=="Yes"||x.Item2=="No"||x.Item2=="Uninstall").ToArray();
            if(ok.Length!=1||destructive.Length!=0)return true;
            lock(dismissed){
                if(!dismissed.Add(window.ToInt64()))return true;
                string message=string.Join(" | ",children.Select(x=>x.Item2).Where(x=>x.Length>3).ToArray());
                File.AppendAllText(log,DateTime.UtcNow.ToString("o")+" "+message+"\r\n");
                Console.WriteLine("STARTUP_WARNING: "+message);
            }
            // OK only: never automate a Yes/No confirmation or any destructive action.
            PostMessage(ok[0].Item1,0x00f5,IntPtr.Zero,IntPtr.Zero);
            return true;
        },IntPtr.Zero);
    }
    [STAThread]
    public static int Main(string[] args){
        if(args.Length<2||args.Length>3){
            Console.Error.WriteLine("Usage: LauncherRedesignHarness <qa-report.json> <existing-profiles.json> [legacy]");
            return 2;
        }
        string log=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])),"startup-modal-errors.txt");
        using(var autoDismiss=new System.Threading.Timer(_=>{
            try{DismissOnlyErrorOK(log);}catch(Exception e){Console.Error.WriteLine(e.Message);}
        },null,250,300)){
        try{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length>=3&&args[2]=="legacy")LauncherLayoutTests.Run(args[0],args[1]);
            else LauncherLayoutTests.RunRedesign(args[0],args[1]);
            Console.WriteLine("Native UI redesign QA report: "+args[0]);
            return 0;
        }catch(Exception e){
            Console.Error.WriteLine(e);
            return 1;
        }
        }
    }
}
