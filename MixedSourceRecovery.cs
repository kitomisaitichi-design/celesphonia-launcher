using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Web.Script.Serialization;

// Restores a target whose COMPLETE file hashes are independently known, by
// selecting exact bytes from any combination of original folders and archives.
// This code has no delta-pack dependency and never writes to a source or saves.
public sealed class MixedNativeFile {
    public string Path {get;set;}
    public string Hash {get;set;}
    public long Size {get;set;}
}
public sealed class MixedNativeReference {
    public int Format {get;set;}
    public string VersionId {get;set;}
    public List<MixedNativeFile> Files {get;set;}
}
public sealed class MixedRecoverySelection {
    public string Path {get;internal set;}
    public string Source {get;internal set;}
    public string Entry {get;internal set;}
    public string Kind {get;internal set;}
    public string Hash {get;internal set;}
    public long Size {get;internal set;}
}
public sealed class MixedRecoveryReport {
    public string VersionId {get;internal set;}
    public int RequiredCount {get;internal set;}
    public int FoundCount {get {return Selections.Count;}}
    public bool Complete {get {return Missing.Count==0 && RequiredCount>0;}}
    public List<string> Missing {get;internal set;}
    public List<MixedRecoverySelection> Selections {get;internal set;}
    internal List<MixedNativeFile> Expected;
}
public static class MixedSourceRecovery {
    const string ReferenceResource = "Celesphonia.NativeReferenceFiles.json";
    const string SevenZipExeResource = "Celesphonia.Rar7z.exe";
    const string SevenZipDllResource = "Celesphonia.Rar7z.dll";
    const int MaxInventory = 250000;
    const long MaxArchiveScratch = 96L*1024*1024;
    const int MaxArchiveBatch = 64;
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength=32*1024*1024 };
    static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    // The resource MUST be compiled into the launcher with its complete native
    // inventory. Sparse version-fingerprints.json is not a replacement.
    public static MixedNativeReference LoadEmbeddedReference(string versionId) {
        if(string.IsNullOrWhiteSpace(versionId) || versionId.Length>100 ||
           versionId.Any(c=>!((c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='-'||c=='_')))
            throw new IOException("Invalid requested native build identifier.");
        var assembly=typeof(MixedSourceRecovery).Assembly;
        string perVersionResource="Celesphonia.NativeReferenceFiles."+versionId+".json";
        Stream stream=assembly.GetManifestResourceStream(perVersionResource);
        if(stream==null&&versionId=="gorpg-1-23")stream=assembly.GetManifestResourceStream(ReferenceResource);
        using(Stream source=stream) {
            if(source==null)throw new IOException("The launcher has no embedded complete native file inventory.");
            using(var reader=new StreamReader(source,Encoding.UTF8)) {
                var reference=Json.Deserialize<MixedNativeReference>(reader.ReadToEnd());
                if(reference==null || reference.Format!=1 || reference.VersionId!=versionId)
                    throw new IOException("Embedded native inventory does not match the requested build.");
                ValidateTargets(reference.Files);
                return reference;
            }
        }
    }
    public static MixedRecoveryReport Probe(MixedNativeReference reference,IEnumerable<string> sources,CancellationToken cancellation) {
        if(reference==null || reference.Format!=1 || string.IsNullOrEmpty(reference.VersionId))
            throw new IOException("An explicit trusted target version is required.");
        return Probe(reference.VersionId,reference.Files,sources,cancellation);
    }
    public static MixedRecoveryReport Probe(string versionId,IEnumerable<MixedNativeFile> expected,IEnumerable<string> sources,CancellationToken cancellation) {
        var targets=ValidateTargets(expected);
        var inputs=sources==null?new string[0]:sources.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(System.IO.Path.GetFullPath)
            .Distinct(PathComparer).ToArray();
        if(inputs.Length==0)throw new IOException("Select one or more original game folders or archives.");
        var report=new MixedRecoveryReport{VersionId=versionId,RequiredCount=targets.Count,Expected=targets,
            Selections=new List<MixedRecoverySelection>(),Missing=new List<string>()};
        var remaining=targets.ToDictionary(x=>x.Path,x=>x,PathComparer);
        var byName=targets.GroupBy(x=>System.IO.Path.GetFileName(x.Path),PathComparer)
            .ToDictionary(x=>x.Key,x=>x.ToList(),PathComparer);
        using(var sevenZip=new SevenZipSession()) {
            // Cheap random-access folders/ZIPs first, regardless of ordering.
            // A solid 7z/RAR should only be opened for files still missing.
            foreach(string input in inputs.OrderBy(x=>File.Exists(x)&&ArchiveFormat(x)=="7z"?1:0)) {
                cancellation.ThrowIfCancellationRequested();
                if(remaining.Count==0)break;
                if(Directory.Exists(input))ProbeFolder(input,remaining,byName,report,cancellation);
                else if(File.Exists(input)) {
                    string format=ArchiveFormat(input);
                    if(format=="zip")ProbeZip(input,remaining,byName,report,cancellation);
                    else if(format=="7z")Probe7z(input,remaining,byName,report,sevenZip,cancellation);
                    else throw new IOException("Unsupported original source archive: "+input);
                } else throw new IOException("Original source is missing: "+input);
            }
        }
        report.Missing=remaining.Keys.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
        return report;
    }
    static List<MixedNativeFile> ValidateTargets(IEnumerable<MixedNativeFile> files) {
        if(files==null)throw new IOException("No complete native file inventory was supplied.");
        var result=new List<MixedNativeFile>();
        var paths=new HashSet<string>(PathComparer);
        foreach(var file in files) {
            if(file==null || !SafeRelative(file.Path) || file.Size<0 || !Sha256(file.Hash)
               || !paths.Add(file.Path.Replace('\\','/')))
                throw new IOException("Untrusted, duplicate or incomplete native file inventory entry.");
            if(file.Path.Split('/','\\').Any(p=>p.Equals("save",StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("celesphonia-english",StringComparison.OrdinalIgnoreCase)) ||
               file.Path.EndsWith(".rpgsave",StringComparison.OrdinalIgnoreCase))
                throw new IOException("Save or English sidecar path in native inventory: "+file.Path);
            result.Add(new MixedNativeFile{Path=file.Path.Replace('\\','/'),Hash=file.Hash.ToLowerInvariant(),Size=file.Size});
            if(result.Count>MaxInventory)throw new IOException("Native inventory is unexpectedly large.");
        }
        if(result.Count==0)throw new IOException("Native file inventory is empty.");
        return result;
    }
    static bool Sha256(string hash) {return hash!=null && hash.Length==64 && hash.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F'));}
    static bool SafeRelative(string path) {
        if(string.IsNullOrEmpty(path) || path.Length>1024 || path.IndexOfAny(new[]{'\0','\r','\n','"',':','<','>','|','?','*'})>=0 ||
           path.StartsWith("/",StringComparison.Ordinal)||path.StartsWith("\\",StringComparison.Ordinal) ||
           path.StartsWith("-",StringComparison.Ordinal) || System.IO.Path.IsPathRooted(path))return false;
        var segments=path.Replace('\\','/').Split('/');
        foreach(string part in segments) {
            if(part.Length==0||part=="."||part==".."||part.EndsWith(".",StringComparison.Ordinal)||part.EndsWith(" ",StringComparison.Ordinal))return false;
            string stem=part.Split('.')[0].ToUpperInvariant();
            if(new[]{"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
                     "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"}.Contains(stem))return false;
        }
        return true;
    }
    static bool MatchPath(string candidate,string required) {
        if(candidate.Equals(required,StringComparison.OrdinalIgnoreCase) ||
           candidate.EndsWith("/"+required,StringComparison.OrdinalIgnoreCase))return true;
        // An extracted www content directory may have been supplied directly.
        if(required.StartsWith("www/",StringComparison.OrdinalIgnoreCase)) {
            string content=required.Substring(4);
            return candidate.Equals(content,StringComparison.OrdinalIgnoreCase) ||
                   candidate.EndsWith("/"+content,StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
    static IEnumerable<MixedNativeFile> Matches(string relative,Dictionary<string,List<MixedNativeFile>> byName,
                                                 Dictionary<string,MixedNativeFile> remaining) {
        if(relative.Equals("celesphonia-english/original-entry.bin",StringComparison.OrdinalIgnoreCase) ||
           relative.EndsWith("/celesphonia-english/original-entry.bin",StringComparison.OrdinalIgnoreCase)) {
            // A reversible English installation holds pristine entry bytes here.
            // Only the target's authoritative SHA256 can approve this alias.
            MixedNativeFile original;
            if(remaining.TryGetValue("www/index.html",out original) ||
               remaining.TryGetValue("index.html",out original))yield return original;
        }
        List<MixedNativeFile> candidates;
        if(!byName.TryGetValue(System.IO.Path.GetFileName(relative),out candidates))yield break;
        foreach(var candidate in candidates)
            if(remaining.ContainsKey(candidate.Path)&&MatchPath(relative,candidate.Path))yield return candidate;
    }
    static void AddMatch(string source,string entry,string kind,MixedNativeFile file,
                         Dictionary<string,MixedNativeFile> remaining,MixedRecoveryReport report) {
        if(!remaining.Remove(file.Path))return;
        report.Selections.Add(new MixedRecoverySelection {
            Path=file.Path,Source=source,Entry=entry,Kind=kind,Size=file.Size,Hash=file.Hash
        });
    }
    static bool IsLink(string path) {
        return (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;
    }
    static void ProbeFolder(string root,Dictionary<string,MixedNativeFile> remaining,
                            Dictionary<string,List<MixedNativeFile>> byName,MixedRecoveryReport report,
                            CancellationToken cancellation) {
        if(IsLink(root))throw new IOException("Linked source folder is not supported: "+root);
        var pending=new Stack<string>();pending.Push(root);int count=0;
        while(pending.Count>0 && remaining.Count>0) {
            cancellation.ThrowIfCancellationRequested();
            string current=pending.Pop();
            foreach(string entry in Directory.GetFileSystemEntries(current)) {
                cancellation.ThrowIfCancellationRequested();
                if(++count>MaxInventory)throw new IOException("Source folder inventory is unexpectedly large.");
                if(IsLink(entry))throw new IOException("Linked file or directory in original source: "+entry);
                if(Directory.Exists(entry)){pending.Push(entry);continue;}
                string relative=entry.Substring(root.TrimEnd('\\','/').Length+1).Replace('\\','/');
                if(!SafeRelative(relative))continue;
                var size=new FileInfo(entry).Length;
                foreach(var candidate in Matches(relative,byName,remaining).Where(x=>x.Size==size).ToArray()) {
                    if(HashFile(entry,cancellation)==candidate.Hash)AddMatch(root,entry,"file",candidate,remaining,report);
                }
            }
        }
    }
    static string ArchiveFormat(string file) {
        byte[] bytes=new byte[8];int n;
        using(var s=File.OpenRead(file))n=s.Read(bytes,0,bytes.Length);
        if(n>=4&&bytes[0]==0x50&&bytes[1]==0x4b &&
           ((bytes[2]==3&&bytes[3]==4)||(bytes[2]==5&&bytes[3]==6)||(bytes[2]==7&&bytes[3]==8)))return "zip";
        if(n>=6&&bytes[0]==0x37&&bytes[1]==0x7a&&bytes[2]==0xbc&&bytes[3]==0xaf&&bytes[4]==0x27&&bytes[5]==0x1c)return "7z";
        if(n>=7&&bytes[0]==0x52&&bytes[1]==0x61&&bytes[2]==0x72&&bytes[3]==0x21&&bytes[4]==0x1a&&bytes[5]==0x07)return "7z";
        if(file.EndsWith(".001",StringComparison.OrdinalIgnoreCase))return "7z";
        return null;
    }
    static void ProbeZip(string source,Dictionary<string,MixedNativeFile> remaining,
                         Dictionary<string,List<MixedNativeFile>> byName,MixedRecoveryReport report,
                         CancellationToken cancellation) {
        using(var zip=ZipFile.OpenRead(source)) {
            if(zip.Entries.Count>MaxInventory)throw new IOException("ZIP inventory is unexpectedly large.");
            var names=new HashSet<string>(PathComparer);
            foreach(var entry in zip.Entries) {
                cancellation.ThrowIfCancellationRequested();
                string relative=entry.FullName.Replace('\\','/').TrimEnd('/');
                if(!SafeRelative(relative))throw new IOException("Unsafe ZIP entry: "+entry.FullName);
                if(!names.Add(relative))throw new IOException("ZIP entries collide on Windows: "+relative);
                int unixMode=(entry.ExternalAttributes>>16)&0xf000;
                if(unixMode==0xa000 || (entry.ExternalAttributes&0x400)!=0)
                    throw new IOException("ZIP contains a linked file: "+relative);
                if(entry.FullName.EndsWith("/",StringComparison.Ordinal))continue;
                foreach(var candidate in Matches(relative,byName,remaining).Where(x=>x.Size==entry.Length).ToArray()) {
                    using(var input=entry.Open()) {
                        if(HashStream(input,cancellation,candidate.Size)==candidate.Hash)
                            AddMatch(source,entry.FullName,"zip",candidate,remaining,report);
                    }
                }
            }
        }
    }
    static void Probe7z(string source,Dictionary<string,MixedNativeFile> remaining,
                        Dictionary<string,List<MixedNativeFile>> byName,MixedRecoveryReport report,
                        SevenZipSession tool,CancellationToken cancellation) {
        var entries=tool.List(source,cancellation);
        var candidates=entries.SelectMany(entry=>Matches(entry.Item1,byName,remaining)
            .Where(x=>x.Size==entry.Item2).Select(x=>Tuple.Create(entry.Item1,entry.Item2,x))).ToList();
        foreach(var batch in Batches(candidates,x=>x.Item2,x=>x.Item1.Length)) {
            cancellation.ThrowIfCancellationRequested();
            if(batch.Count==1&&batch[0].Item2>MaxArchiveScratch) {
                var item=batch[0];
                if(remaining.ContainsKey(item.Item3.Path) &&
                   tool.HashEntry(source,item.Item1,item.Item2,cancellation)==item.Item3.Hash)
                    AddMatch(source,item.Item1,"7z",item.Item3,remaining,report);
                continue;
            }
            tool.WithExtracted(source,batch.Select(x=>x.Item1),folder=>{
                foreach(var item in batch) {
                    if(!remaining.ContainsKey(item.Item3.Path))continue;
                    string native=Child(folder,item.Item1.Replace('/','\\'));
                    if(File.Exists(native)&&new FileInfo(native).Length==item.Item2 &&
                       HashFile(native,cancellation)==item.Item3.Hash)
                        AddMatch(source,item.Item1,"7z",item.Item3,remaining,report);
                }
            },cancellation);
        }
    }
    static IEnumerable<List<T>> Batches<T>(IEnumerable<T> items,Func<T,long> size,Func<T,int> nameLength) {
        var batch=new List<T>();long bytes=0;int chars=0;
        foreach(T item in items) {
            long n=size(item);int c=nameLength(item)+4;
            if(batch.Count>0&&(batch.Count>=MaxArchiveBatch||bytes+n>MaxArchiveScratch||chars+c>14000)) {
                yield return batch;batch=new List<T>();bytes=0;chars=0;
            }
            batch.Add(item);bytes+=n;chars+=c;
            if(n>MaxArchiveScratch){yield return batch;batch=new List<T>();bytes=0;chars=0;}
        }
        if(batch.Count>0)yield return batch;
    }
    static string HashFile(string path,CancellationToken cancellation) {
        using(var stream=File.OpenRead(path))return HashStream(stream,cancellation);
    }
    static string HashStream(Stream stream,CancellationToken cancellation,long maxBytes=long.MaxValue) {
        using(var sha=SHA256.Create()) {
            var buffer=new byte[65536];int n;long count=0;
            while((n=stream.Read(buffer,0,buffer.Length))>0) {
                cancellation.ThrowIfCancellationRequested();
                count+=n;
                if(count>maxBytes)throw new IOException("Original source entry exceeds its declared expected size.");
                sha.TransformBlock(buffer,0,n,buffer,0);
            }
            sha.TransformFinalBlock(new byte[0],0,0);
            return BitConverter.ToString(sha.Hash).Replace("-","").ToLowerInvariant();
        }
    }
    static void CopyAndCheck(Stream source,string target,MixedRecoverySelection expected,CancellationToken cancellation) {
        using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        using(var sha=SHA256.Create()) {
            var buffer=new byte[65536];int n;long count=0;
            while((n=source.Read(buffer,0,buffer.Length))>0) {
                cancellation.ThrowIfCancellationRequested();
                count+=n;
                if(count>expected.Size)throw new IOException("Source expanded beyond target size: "+expected.Path);
                sha.TransformBlock(buffer,0,n,buffer,0);output.Write(buffer,0,n);
            }
            sha.TransformFinalBlock(new byte[0],0,0);
            string digest=BitConverter.ToString(sha.Hash).Replace("-","").ToLowerInvariant();
            if(count!=expected.Size || digest!=expected.Hash)throw new IOException("Source changed or failed hash verification: "+expected.Path);
            output.Flush(true);
        }
    }
    // Creates a NEW independent game tree. Existing destinations are rejected;
    // staging is a sibling and is removed on error. Source trees are read-only.
    public static void Recover(MixedRecoveryReport report,string destination,CancellationToken cancellation) {
        if(report==null || !report.Complete || report.Expected==null ||
           report.Selections.Count!=report.Expected.Count)throw new IOException("Missing original native files. Recovery cannot start.");
        var expected=ValidateTargets(report.Expected);
        string target=System.IO.Path.GetFullPath(destination).TrimEnd('\\', '/');
        if(Directory.Exists(target)||File.Exists(target))throw new IOException("Recovery destination already exists.");
        if(expected.Count!=report.Selections.Count ||
           expected.Any(x=>!report.Selections.Any(y=>PathComparer.Equals(x.Path,y.Path)&&x.Hash==y.Hash&&x.Size==y.Size)))
            throw new IOException("Recovery report is incomplete or was changed.");
        foreach(var selection in report.Selections) {
            string src=System.IO.Path.GetFullPath(selection.Source).TrimEnd('\\','/');
            if(target.StartsWith(src+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new IOException("Select a destination outside original source folders.");
        }
        string parent=System.IO.Path.GetDirectoryName(target);
        if(!Directory.Exists(parent))throw new IOException("Recovery destination parent does not exist.");
        string stage=System.IO.Path.Combine(parent,".cph-mixed-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        try {
            using(var sevenZip=new SevenZipSession()) {
                foreach(var selection in report.Selections.OrderBy(x=>x.Path,StringComparer.OrdinalIgnoreCase)) {
                    cancellation.ThrowIfCancellationRequested();
                    string relative=selection.Path.Replace('/','\\');
                    string output=Child(stage,relative);
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
                    if(selection.Kind=="file") {
                        if(IsLink(selection.Entry))throw new IOException("Source file became linked: "+selection.Entry);
                        using(var stream=File.OpenRead(selection.Entry))CopyAndCheck(stream,output,selection,cancellation);
                    } else if(selection.Kind=="zip") {
                        using(var zip=ZipFile.OpenRead(selection.Source)) {
                            var entry=zip.GetEntry(selection.Entry);
                            if(entry==null||entry.Length!=selection.Size||((entry.ExternalAttributes>>16)&0xf000)==0xa000)
                                throw new IOException("ZIP entry changed: "+selection.Entry);
                            using(var stream=entry.Open())CopyAndCheck(stream,output,selection,cancellation);
                        }
                    } else if(selection.Kind=="7z")continue; // copied in bounded source batches below
                    else throw new IOException("Unknown source kind in recovery plan.");
                }
                foreach(var sourceGroup in report.Selections.Where(x=>x.Kind=="7z").GroupBy(x=>x.Source,PathComparer)) {
                    foreach(var batch in Batches(sourceGroup,x=>x.Size,x=>x.Entry.Length)) {
                        cancellation.ThrowIfCancellationRequested();
                        if(batch.Count==1&&batch[0].Size>MaxArchiveScratch) {
                            var entry=batch[0];string output=Child(stage,entry.Path.Replace('/','\\'));
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
                            sevenZip.CopyEntry(sourceGroup.Key,entry.Entry,output,entry,cancellation);
                            continue;
                        }
                        sevenZip.WithExtracted(sourceGroup.Key,batch.Select(x=>x.Entry),scratch=>{
                            foreach(var entry in batch) {
                                string native=Child(scratch,entry.Entry.Replace('/','\\'));
                                if(!File.Exists(native))throw new IOException("Missing extracted archive member: "+entry.Entry);
                                string output=Child(stage,entry.Path.Replace('/','\\'));
                                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
                                using(var input=File.OpenRead(native))CopyAndCheck(input,output,entry,cancellation);
                            }
                        },cancellation);
                    }
                }
            }
            if(File.Exists(target)||Directory.Exists(target))throw new IOException("Recovery destination appeared while building.");
            Directory.Move(stage,target);
        } finally {
            // Only remove staging created in this call; never remove a source.
            if(Directory.Exists(stage))Directory.Delete(stage,true);
        }
    }
    static string Child(string root,string relative) {
        if(!SafeRelative(relative))throw new IOException("Unsafe recovery output path.");
        string prefix=System.IO.Path.GetFullPath(root).TrimEnd('\\','/')+System.IO.Path.DirectorySeparatorChar;
        string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,relative));
        if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new IOException("Recovery output escapes staging.");
        return path;
    }
    sealed class SevenZipSession : IDisposable {
        string owned;
        string exe;
        public SevenZipSession() {}
        static string Quote(string s) {
            if(s.IndexOfAny(new[]{'"','\r','\n','\0'})>=0)throw new IOException("Invalid archive argument.");
            return "\""+s+"\"";
        }
        void Ensure() {
            if(exe!=null)return;
            owned=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CphMixed7z-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(owned);
            exe=System.IO.Path.Combine(owned,"7z.exe");
            foreach(var item in new[]{Tuple.Create(SevenZipExeResource,"7z.exe"),Tuple.Create(SevenZipDllResource,"7z.dll")}) {
                using(var stream=typeof(MixedSourceRecovery).Assembly.GetManifestResourceStream(item.Item1)) {
                    if(stream==null)throw new IOException("Embedded 7z extractor resource missing: "+item.Item1);
                    using(var output=File.Create(System.IO.Path.Combine(owned,item.Item2)))stream.CopyTo(output);
                }
            }
        }
        Process Start(string command,string source,string entry) {
            Ensure();
            string arguments=command+" -p- -sccUTF-8 "+Quote(source)+(entry==null?"":" "+Quote(entry));
            var process=Process.Start(new ProcessStartInfo(exe,arguments) {
                UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,
                StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false)
            });
            if(process==null)throw new IOException("Could not start embedded archive extractor.");
            return process;
        }
        public void WithExtracted(string archive,IEnumerable<string> entries,Action<string> inspect,CancellationToken cancellation) {
            Ensure();
            string stage=System.IO.Path.Combine(owned,"chunk-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            try {
                string[] names=entries.Distinct(PathComparer).ToArray();
                if(names.Length==0 || names.Length>MaxArchiveBatch || names.Any(x=>!SafeRelative(x)))
                    throw new IOException("Invalid archive extraction batch.");
                string arguments="x -y -p- -sccUTF-8 -bso0 -bsp0 -o"+Quote(stage)+" "+Quote(archive)+" "+
                    string.Join(" ",names.Select(Quote).ToArray());
                using(var process=Process.Start(new ProcessStartInfo(exe,arguments) {
                    UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,
                    StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false)
                })) {
                    if(process==null)throw new IOException("Could not extract original archive batch.");
                    var error=process.StandardError.ReadToEndAsync();
                    process.StandardOutput.ReadToEnd();
                    Wait(process,cancellation);
                }
                var paths=new Stack<string>();paths.Push(stage);int count=0;long bytes=0;
                while(paths.Count>0) {
                    cancellation.ThrowIfCancellationRequested();
                    foreach(string path in Directory.GetFileSystemEntries(paths.Pop())) {
                        if(IsLink(path))throw new IOException("Archive extraction created a linked path.");
                        if(Directory.Exists(path))paths.Push(path);
                        else {
                            count++;bytes+=new FileInfo(path).Length;
                            if(count>names.Length||bytes>MaxArchiveScratch)
                                throw new IOException("Archive extraction exceeds its bounded scratch allowance.");
                        }
                    }
                }
                inspect(stage);
            } finally {
                if(Directory.Exists(stage))Directory.Delete(stage,true);
            }
        }
        static void Wait(Process process,CancellationToken cancellation) {
            while(!process.WaitForExit(150)) {
                if(cancellation.IsCancellationRequested) {
                    try{process.Kill();}catch(InvalidOperationException){}
                    cancellation.ThrowIfCancellationRequested();
                }
            }
            cancellation.ThrowIfCancellationRequested();
            if(process.ExitCode!=0)throw new IOException("Original archive entry is unavailable or archive validation failed (7z exit "+process.ExitCode+").");
        }
        public List<Tuple<string,long>> List(string source,CancellationToken cancellation) {
            using(var process=Start("l -slt",source,null)) {
                var error=process.StandardError.ReadToEndAsync();
                string text=process.StandardOutput.ReadToEnd();
                Wait(process,cancellation);
                var found=new List<Tuple<string,long>>();
                var unique=new HashSet<string>(PathComparer);
                int start=text.IndexOf("----------",StringComparison.Ordinal);
                if(start<0)throw new IOException("Original archive has no file inventory.");
                foreach(string group in text.Substring(start+10).Split(new[]{"\r\n\r\n","\n\n"},StringSplitOptions.RemoveEmptyEntries)) {
                    string path=null,size=null;bool folder=false,link=false;
                    foreach(string row in group.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)) {
                        if(row.StartsWith("Path = ",StringComparison.Ordinal))path=row.Substring(7).Replace('\\','/');
                        else if(row.StartsWith("Size = ",StringComparison.Ordinal))size=row.Substring(7);
                        else if(row=="Folder = +")folder=true;
                        else if(row.StartsWith("Symbolic Link = ",StringComparison.Ordinal)||
                                row.StartsWith("Hard Link = ",StringComparison.Ordinal)||
                                row.StartsWith("Copy Link = ",StringComparison.Ordinal))link=true;
                    }
                    if(path==null)continue;
                    if(!SafeRelative(path) || !unique.Add(path) || link)
                        throw new IOException("Archive contains an unsafe or duplicate path: "+path);
                    if(folder)continue;
                    long length;
                    if(!long.TryParse(size,out length)||length<0)throw new IOException("Archive has an invalid entry size.");
                    found.Add(Tuple.Create(path,length));
                    if(found.Count>MaxInventory)throw new IOException("Original archive inventory is unexpectedly large.");
                }
                return found;
            }
        }
        public string HashEntry(string archive,string entry,long maxBytes,CancellationToken cancellation) {
            using(var process=Start("x -so",archive,entry)) {
                var error=process.StandardError.ReadToEndAsync();
                string hash;
                try{hash=HashStream(process.StandardOutput.BaseStream,cancellation,maxBytes);}
                catch{try{process.Kill();}catch{}throw;}
                Wait(process,cancellation);
                return hash;
            }
        }
        public void CopyEntry(string archive,string entry,string target,MixedRecoverySelection selection,CancellationToken cancellation) {
            using(var process=Start("x -so",archive,entry)) {
                var error=process.StandardError.ReadToEndAsync();
                try{CopyAndCheck(process.StandardOutput.BaseStream,target,selection,cancellation);}
                catch{try{process.Kill();}catch{}throw;}
                Wait(process,cancellation);
            }
        }
        public void Dispose() {
            if(owned!=null&&Directory.Exists(owned) &&
               System.IO.Path.GetFileName(owned).StartsWith("CphMixed7z-",StringComparison.Ordinal))
                try{Directory.Delete(owned,true);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
}
