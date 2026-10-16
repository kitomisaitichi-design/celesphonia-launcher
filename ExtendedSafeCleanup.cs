using System;
using System.IO;
using System.Runtime.InteropServices;

// Secure, long-path-aware staging cleanup with link checks before deletion.
public static class ExtendedSafeCleanup {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    struct FileTime { public uint Low, High; }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    struct FindData {
        public uint Attributes;
        public FileTime CreationTime, AccessTime, WriteTime;
        public uint FileSizeHigh, FileSizeLow, Reserved0, Reserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)] public string FileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)] public string AlternateFileName;
    }
    [DllImport("kernel32.dll", EntryPoint="GetFileAttributesW", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern uint GetAttrs(string path);
    [DllImport("kernel32.dll", EntryPoint="FindFirstFileW", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern IntPtr First(string path, out FindData data);
    [DllImport("kernel32.dll", EntryPoint="FindNextFileW", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool Next(IntPtr handle, out FindData data);
    [DllImport("kernel32.dll", EntryPoint="FindClose", SetLastError=true)]
    static extern bool Close(IntPtr handle);
    [DllImport("kernel32.dll", EntryPoint="DeleteFileW", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool DeleteNativeFile(string path);
    [DllImport("kernel32.dll", EntryPoint="RemoveDirectoryW", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool RemoveNativeDirectory(string path);
    [DllImport("kernel32.dll", EntryPoint="SetFileAttributesW", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern bool SetAttrs(string path, uint attributes);
    const uint Reparse=0x400, DirectoryAttr=0x10, ReadOnly=0x1, InvalidAttrs=0xffffffff;
    static string NativePath(string path) {
        string full=Path.GetFullPath(path);
        return full.StartsWith(@"\\",StringComparison.Ordinal)?@"\\?\UNC\"+full.Substring(2):@"\\?\"+full;
    }
    static void Guard(uint attrs, string path) {
        if((attrs&Reparse)!=0)throw new IOException("Linked archive staging path refused: "+path);
    }
    static void RemoveDir(string path, uint attrs) {
        if((attrs&ReadOnly)!=0 && !SetAttrs(path,attrs&~ReadOnly))
            throw new IOException("Cannot clear read-only directory attribute: "+path+" (win32="+Marshal.GetLastWin32Error()+")");
        if(!RemoveNativeDirectory(path))throw new IOException("Cannot remove archive staging directory: "+path+" (win32="+Marshal.GetLastWin32Error()+")");
    }
    static void Visit(string path, int depth=0, bool remove=false) {
        if(depth>256)throw new IOException("Excessive archive staging folder depth: "+path);
        uint root=GetAttrs(path);
        if(root==InvalidAttrs)throw new IOException("Cannot inspect archive staging path: "+path+" (win32="+Marshal.GetLastWin32Error()+")");
        Guard(root,path);
        if((root&DirectoryAttr)==0) {
            if(remove) {
                if((root&ReadOnly)!=0 && !SetAttrs(path,root&~ReadOnly))throw new IOException("Cannot make archive staging file writable: "+path);
                if(!DeleteNativeFile(path))throw new IOException("Cannot remove archive staging file: "+path+" (win32="+Marshal.GetLastWin32Error()+")");
            }
            return;
        }
        FindData data;
        IntPtr h=First(path.TrimEnd('\\')+@"\*",out data);
        if(h==new IntPtr(-1)) {
            int err=Marshal.GetLastWin32Error();
            if(err==2) {
                if(remove)RemoveDir(path,root);
                return; // ERROR_FILE_NOT_FOUND: empty directory
            }
            throw new IOException("Cannot enumerate archive staging path: "+path+" (win32="+err+")");
        }
        try {
            while(true) {
                if(data.FileName!="." && data.FileName!="..") {
                    string child=path.TrimEnd('\\')+"\\"+data.FileName;
                    Guard(data.Attributes,child);
                    Visit(child,depth+1,remove);
                }
                if(!Next(h,out data)) {
                    int err=Marshal.GetLastWin32Error();
                    if(err!=18)throw new IOException("Archive staging scan stopped: "+path+" (win32="+err+")");
                    break;
                }
            }
        } finally { Close(h); }
        if(remove)RemoveDir(path,root);
    }
    public static void AssertNoLinks(string root) {
        Visit(NativePath(root));
    }
    public static void RemoveTreeSafely(string root) {
        string path=NativePath(root);
        Visit(path); // verify full tree BEFORE deleting anything
        Visit(path,0,true); // recheck every node immediately before native deletion
    }
}
