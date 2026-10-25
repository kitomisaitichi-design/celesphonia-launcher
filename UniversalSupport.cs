using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public class VersionIndex {
    public int Format {get;set;}
    public string Name {get;set;}
    public List<VersionBuild> Versions {get;set;}
}
public class VersionBuild {
    public string Id {get;set;}
    public string Name {get;set;}
    public Dictionary<string,string> Fingerprints {get;set;}
    public List<VersionFile> Files {get;set;}
}
public class VersionFile {
    public string Path {get;set;}
    public string Hash {get;set;}
    public long Size {get;set;}
    public Dictionary<string,VersionPlan> Plans {get;set;}
}
public class VersionPlan {
    public string Source {get;set;}
    public string SourceHash {get;set;}
    public long Prefix {get;set;}
    public long Suffix {get;set;}
    public string Payload {get;set;}
}
public class ReconstructionState {public int format {get;set;}public List<ReconstructionFile> files {get;set;}}
public class ReconstructionFile {public string path {get;set;}public string hash {get;set;}public long size {get;set;}}
public static class UniversalSupport {
    static readonly string[] markers={"data/System.json","js/plugins.js","js/rpg_managers.js"};
    public const string PackName="Celesphonia-Any-Version.cgdelta.zip";
    static readonly object indexGate=new object();static VersionIndex cachedIndex;static string indexStamp;
    public static VersionIndex ReadIndex(){string file=Path.Combine(Store.Home,"version-fingerprints.json"),stamp=DetectionCache.Stamp(file);lock(indexGate){if(cachedIndex==null||stamp!=indexStamp){cachedIndex=File.Exists(file)?Store.Json.Deserialize<VersionIndex>(File.ReadAllText(file,Encoding.UTF8)):new VersionIndex{Versions=new List<VersionBuild>()};indexStamp=stamp;}return cachedIndex;}}
    public static string IdentifyWeb(string web){
        var index=ReadIndex();var hashes=new Dictionary<string,string>();foreach(string mark in index.Versions.SelectMany(x=>x.Fingerprints.Keys).Distinct()){string file=Store.Child(web,mark.Replace('/','\\'));hashes[mark]=DetectionCache.Hash(file);}return index.Versions.OrderByDescending(x=>x.Fingerprints.Count).Where(x=>x.Fingerprints.All(m=>hashes[m.Key]==m.Value)).Select(x=>x.Id).FirstOrDefault();
    }
    public static string Identify(string root){try{return IdentifyWeb(Store.WebRoot(root));}catch{return null;}}
    public static string DetectedName(string root){var id=Identify(root);return ReadIndex().Versions.Where(x=>x.Id==id).Select(x=>x.Name).FirstOrDefault();}
    public static bool IsPack(string file){try{using(var zip=ZipFile.OpenRead(file)){var entry=zip.GetEntry("index.json");if(entry==null)return false;using(var reader=new StreamReader(entry.Open()))return Store.Json.Deserialize<VersionIndex>(reader.ReadToEnd()).Format==2;}}catch{return false;}}
    public static VersionIndex PackIndex(ZipArchive zip){var entry=zip.GetEntry("index.json");if(entry==null)throw new IOException("This is not an any-version pack.");using(var reader=new StreamReader(entry.Open(),Encoding.UTF8)){var index=Store.Json.Deserialize<VersionIndex>(reader.ReadToEnd());if(index.Format!=2||index.Versions==null||index.Versions.Count==0)throw new IOException("Unsupported any-version pack.");return index;}}
    static VersionIndex Manifest(ZipArchive zip){var entry=zip.GetEntry("routes.json")??zip.GetEntry("manifest.json");if(entry==null)throw new IOException("Missing any-version manifest.");using(var reader=new StreamReader(entry.Open(),Encoding.UTF8)){var m=Store.Json.Deserialize<VersionIndex>(reader.ReadToEnd());if(m.Format!=2||m.Versions==null)throw new IOException("Unsupported any-version manifest.");return m;}}
    static string Hash(string file,Dictionary<string,string> cache){string hash;if(!cache.TryGetValue(file,out hash)){hash=Store.Hash(file);cache[file]=hash;}return hash;}
    static string Canonical(Profile p){if(FileDiscovery.IsCH(p.Root))return "ch-current";if(RecipeSupport.IsCleanBase(p.Root))return "gorpg-1-23";var recipe=RecipeSupport.Identify(Store.WebRoot(p.Root));return recipe==null?null:recipe.Id;}
    static string Name(string id){if(id=="ch-current")return "CH";if(id=="gorpg-1-23")return "Gorpg 1.23 - clean base";return RecipeSupport.Catalog().Recipes.First(x=>x.Id==id).Name;}
    public static void Build(string config,string output){
        if(File.Exists(output))throw new IOException("Pack already exists; choose a new output file.");
        var settings=Store.Json.Deserialize<Settings>(File.ReadAllText(config,Encoding.UTF8));
        var profiles=settings.Profiles.Select(x=>new {Profile=x,Id=Canonical(x)}).Where(x=>x.Id!=null&&x.Id!="abu-51-01").GroupBy(x=>x.Id).Select(x=>x.First()).ToList();
        if(profiles.Count!=5)throw new IOException("Build requires the five reference versions: CH, Gorpg 1.23, Maple 1.7.2, 3.3.4, and 3.3.5 trial.");
        var cache=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var commonCache=new Dictionary<string,long[]>();var payloads=new HashSet<string>();var sliceCache=new Dictionary<string,string>();
        var manifest=new VersionIndex{Format=2,Name="Celesphonia · Any starting version",Versions=new List<VersionBuild>()};
        foreach(var p in profiles){string web=Store.WebRoot(p.Profile.Root);var build=new VersionBuild{Id=p.Id,Name=Name(p.Id),Fingerprints=new Dictionary<string,string>(),Files=new List<VersionFile>()};foreach(string mark in markers)build.Fingerprints[mark]=Hash(Store.Child(web,mark.Replace('/','\\')),cache);manifest.Versions.Add(build);}
        var index=new VersionIndex{Format=2,Name=manifest.Name,Versions=manifest.Versions.Select(x=>new VersionBuild{Id=x.Id,Name=x.Name,Fingerprints=x.Fingerprints}).ToList()};
        File.WriteAllText(Path.Combine(Store.Home,"version-fingerprints.json"),Store.Json.Serialize(index),new UTF8Encoding(false));
        string partial=output+".building";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=new List<object>();
        using(var zip=new ZipArchive(File.Create(partial),ZipArchiveMode.Create)){
            foreach(var target in profiles){
                var build=manifest.Versions.First(x=>x.Id==target.Id);string targetRoot=target.Profile.Root.TrimEnd('\\'),web=Store.WebRoot(targetRoot),webRelative=web.Equals(targetRoot,StringComparison.OrdinalIgnoreCase)?"":web.Substring(targetRoot.Length+1).Replace('\\','/');
                foreach(string file in DeltaSupport.GameFiles(targetRoot).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x)){
                    string relative=file.Substring(targetRoot.Length+1).Replace('\\','/');long size=new FileInfo(file).Length;string targetHash=Hash(file,cache);
                    var record=new VersionFile{Path=relative,Hash=targetHash,Size=size,Plans=new Dictionary<string,VersionPlan>()};
                    foreach(var source in profiles.Where(x=>x.Id!=target.Id)){
                        var plan=new VersionPlan();long best=size;
                        foreach(string token in DeltaSupport.Candidates(source.Profile.Root,relative,webRelative)){
                            string candidate=DeltaSupport.Resolve(source.Profile.Root,token);long sourceSize=new FileInfo(candidate).Length;string sourceHash=Hash(candidate,cache);
                            if(sourceSize==size&&sourceHash==targetHash){plan.Source=token;plan.SourceHash=sourceHash;plan.Prefix=size;plan.Suffix=0;best=0;break;}
                            if(size<65536||sourceSize<65536)continue;
                            string commonKey=sourceHash+":"+targetHash;long[] common;if(!commonCache.TryGetValue(commonKey,out common)){common=DeltaSupport.Common(candidate,file);commonCache[commonKey]=common;}
                            long middle=size-common[0]-common[1];if(middle<best&&middle<size/2){best=middle;plan.Source=token;plan.SourceHash=sourceHash;plan.Prefix=common[0];plan.Suffix=common[1];}
                        }
                        long literal=size-plan.Prefix-plan.Suffix;
                        if(literal>0){
                            string key=targetHash+":"+plan.Prefix+":"+plan.Suffix,sliceHash;
                            if(!sliceCache.TryGetValue(key,out sliceHash)){if(literal==size)sliceHash=targetHash;else using(var sink=new DigestSink())using(var input=File.OpenRead(file)){DeltaSupport.CopyPart(input,sink,plan.Prefix,literal);sliceHash=sink.Finish();}sliceCache[key]=sliceHash;}
                            plan.Payload="payload/"+sliceHash;
                            if(payloads.Add(plan.Payload)){var entry=zip.CreateEntry(plan.Payload,CompressionLevel.Optimal);using(var input=File.OpenRead(file))using(var stream=entry.Open())DeltaSupport.CopyPart(input,stream,plan.Prefix,literal);}
                        }
                        record.Plans[source.Id]=plan;
                    }
                    build.Files.Add(record);
                }
                report.Add(new {target=build.Name,id=build.Id,files=build.Files.Count,sourceVersions=profiles.Count-1,uniquePayloadsSoFar=payloads.Count});
                File.WriteAllText(Path.Combine(Store.Home,"any-version-build-progress.json"),Store.Json.Serialize(report),Encoding.UTF8);
            }
            foreach(var item in new[]{Tuple.Create("index.json",index),Tuple.Create("routes.json",manifest)}){var entry=zip.CreateEntry(item.Item1,CompressionLevel.Optimal);using(var writer=new StreamWriter(entry.Open(),new UTF8Encoding(false)))writer.Write(Store.Json.Serialize(item.Item2));}
        }
        File.Move(partial,output);File.WriteAllText(Path.Combine(Store.Home,"any-version-build-report.json"),Store.Json.Serialize(new {pack=Path.GetFileName(output),downloadBytes=new FileInfo(output).Length,versions=report,uniquePayloads=payloads.Count,routes=20}),Encoding.UTF8);
    }
    static void Validate(VersionBuild target,string sourceId,string source,ZipArchive zip,Dictionary<string,string> cache){
        var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var file in target.Files){
            Store.Child(Store.Home,file.Path.Replace('/','\\'));if(!paths.Add(file.Path)||Store.IsSave(file.Path)||file.Path.Split('/').Any(x=>x.Equals("save",StringComparison.OrdinalIgnoreCase)))throw new IOException("Invalid or duplicate target path.");
            VersionPlan plan;if(file.Plans==null||!file.Plans.TryGetValue(sourceId,out plan))throw new IOException("This pack has no route from the selected source version.");
            if(file.Size<0||plan.Prefix<0||plan.Suffix<0||plan.Prefix>file.Size-plan.Suffix)throw new IOException("Invalid patch ranges.");
            if(plan.Source!=null){string path=DeltaSupport.Resolve(source,plan.Source);if(!File.Exists(path)||Hash(path,cache)!=plan.SourceHash)throw new IOException("The detected version has a modified or missing required file: "+plan.Source+". Choose another intact installation. Existing games and saves were not changed.");if(plan.Prefix>new FileInfo(path).Length-plan.Suffix)throw new IOException("Invalid source ranges.");}
            else if(plan.Prefix!=0||plan.Suffix!=0)throw new IOException("Source required for patch ranges.");
            if(plan.Payload!=null){if(!plan.Payload.StartsWith("payload/",StringComparison.Ordinal)||plan.Payload.Length!=72||plan.Payload.Substring(8).Any(x=>!Uri.IsHexDigit(x)))throw new IOException("Invalid payload reference.");var entry=zip.GetEntry(plan.Payload);if(entry==null||entry.Length!=file.Size-plan.Prefix-plan.Suffix)throw new IOException("Missing or invalid patch payload.");}
            else if(plan.Prefix+plan.Suffix!=file.Size)throw new IOException("Incomplete patch file.");
        }
    }
    static void Reconstruct(ZipArchive zip,string source,VersionFile file,string sourceId,Stream output){
        var plan=file.Plans[sourceId];if(plan.Source!=null)using(var input=File.OpenRead(DeltaSupport.Resolve(source,plan.Source)))DeltaSupport.CopyPart(input,output,0,plan.Prefix);
        if(plan.Payload!=null)using(var input=zip.GetEntry(plan.Payload).Open())input.CopyTo(output);
        if(plan.Source!=null&&plan.Suffix>0)using(var input=File.OpenRead(DeltaSupport.Resolve(source,plan.Source)))DeltaSupport.CopyPart(input,output,input.Length-plan.Suffix,plan.Suffix);
    }
    static string SourceId(VersionIndex manifest,string source){
        string web=Store.WebRoot(source);foreach(var version in manifest.Versions){if(version.Fingerprints.All(x=>File.Exists(Store.Child(web,x.Key.Replace('/','\\')))&&Store.Hash(Store.Child(web,x.Key.Replace('/','\\')))==x.Value))return version.Id;}
        throw new IOException("This game folder does not match a supported reference build. Select an intact CH, Gorpg 1.23, Maple 1.7.2, 3.3.4, or 3.3.5 trial installation. Folder names alone are not enough.");
    }
    public static Profile Install(string pack,string source,string installRoot,string targetId){
        InstallDestination.RequireSafe(installRoot,new[]{source,pack});
        UpgradeSupport.RequireRoute(Identify(source),targetId);
        using(var zip=ZipFile.OpenRead(pack)){
            var m=Manifest(zip);string sourceId=SourceId(m,source);var target=m.Versions.FirstOrDefault(x=>x.Id==targetId);if(target==null)throw new IOException("The selected version is not present in this pack.");if(sourceId==targetId)throw new IOException("You already have this version. Register its folder and switch to it directly.");
            var cache=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);Validate(target,sourceId,source,zip,cache);
            string full=Path.GetFullPath(installRoot);if((full.TrimEnd('\\')+"\\").StartsWith(Path.GetFullPath(source).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new IOException("Install new versions outside the source game folder.");
            long need=target.Files.Sum(x=>x.Size)+256L*1024*1024;Directory.CreateDirectory(full);if(new DriveInfo(Path.GetPathRoot(full)).AvailableFreeSpace<need)throw new IOException("Need approximately "+(need/(1024.0*1024*1024)).ToString("0.0")+" GB free for the new game.");
            string root=Store.Child(full,"Version-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(root);
            foreach(var file in target.Files){string destination=Store.Child(root,file.Path.Replace('/','\\'));Directory.CreateDirectory(Path.GetDirectoryName(destination));using(var output=File.Create(destination))Reconstruct(zip,source,file,sourceId,output);if(new FileInfo(destination).Length!=file.Size||Store.Hash(destination)!=file.Hash)throw new IOException("Reconstructed file failed verification: "+file.Path+". The incomplete profile was not registered.");}
            var p=Store.Detect(root,target.Name);Directory.CreateDirectory(p.SavePath);p.Note="Built from "+m.Versions.First(x=>x.Id==sourceId).Name+"; independent files and saves";
            File.WriteAllText(Path.Combine(root,"launcher-reconstruction.json"),Store.Json.Serialize(new {format=2,sourceVersion=sourceId,targetVersion=target.Id,files=target.Files.Select(x=>new {path=x.Path,hash=x.Hash,size=x.Size}).ToArray()}),Encoding.UTF8);return p;
        }
    }
    public static void VerifyAll(string pack,string config){
        var settings=Store.Json.Deserialize<Settings>(File.ReadAllText(config,Encoding.UTF8));var report=new List<object>();var cache=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        using(var zip=ZipFile.OpenRead(pack)){
            var m=Manifest(zip);var sources=settings.Profiles.Select(x=>new {Profile=x,Id=Identify(x.Root)}).Where(x=>x.Id!=null).GroupBy(x=>x.Id).Select(x=>x.First()).ToList();
            foreach(var target in m.Versions)foreach(var source in sources.Where(x=>x.Id!=target.Id)){
                Validate(target,source.Id,source.Profile.Root,zip,cache);
                foreach(var file in target.Files)using(var sink=new DigestSink()){Reconstruct(zip,source.Profile.Root,file,source.Id,sink);if(sink.Count!=file.Size||sink.Finish()!=file.Hash)throw new IOException("Verification failed: "+source.Id+" → "+target.Id+" / "+file.Path);}
                report.Add(new {source=source.Id,target=target.Id,verifiedFiles=target.Files.Count,status="PASS"});File.WriteAllText(Path.Combine(Store.Home,"any-version-verification.json"),Store.Json.Serialize(report),Encoding.UTF8);
            }
        }
        if(report.Count!=20)throw new IOException("Expected all twenty source-to-target routes.");
    }
    public static void VerifyInstalled(Profile profile){VerifyInstalled(profile,false);}
    public static void VerifyInstalled(Profile profile,bool launchOnly){
        string record=Path.Combine(profile.Root,"launcher-reconstruction.json");if(!File.Exists(record))return;var state=Store.Json.Deserialize<ReconstructionState>(File.ReadAllText(record,Encoding.UTF8));if(state.format!=2||state.files==null)throw new IOException("Invalid reconstructed profile record.");
        string web=Store.WebRoot(profile.Root);
        if(launchOnly)RecipeSupport.RequireRecordedEntry(RecipeSupport.ReadLaunchEntry(profile.Root),state.files.Select(file=>Store.Child(profile.Root,file.path.Replace('/','\\'))));
        foreach(var file in state.files){string recorded=Store.Child(profile.Root,file.path.Replace('/','\\'));if(RecipeSupport.IsMutableAuxiliary(profile.Root,web,recorded)||(launchOnly&&!RecipeSupport.IsLaunchDependency(profile.Root,web,recorded)))continue;string path=EnglishSupport.OriginalFile(profile.Root,recorded);if(!File.Exists(path)||new FileInfo(path).Length!=file.size||Store.Hash(path)!=file.hash)throw new IOException("Reconstructed profile file changed or is missing: "+file.path);}
    }
    public static string FindGameRoot(string folder){
        if(File.Exists(Path.Combine(folder,"package.json"))&&(File.Exists(Path.Combine(folder,"Game.exe"))||File.Exists(Path.Combine(folder,"nw.exe")))){Store.Detect(folder,"Game");return folder;}
        var possible=new List<string>();var pending=new Queue<Tuple<string,int>>();pending.Enqueue(Tuple.Create(folder,0));int budget=3500;
        while(pending.Count>0&&budget-->0){var item=pending.Dequeue();if((new DirectoryInfo(item.Item1).Attributes&FileAttributes.ReparsePoint)!=0)continue;if(File.Exists(Path.Combine(item.Item1,"package.json")))possible.Add(item.Item1);if(item.Item2<8)foreach(string sub in Directory.GetDirectories(item.Item1))pending.Enqueue(Tuple.Create(sub,item.Item2+1));}
        var games=possible.Distinct(StringComparer.OrdinalIgnoreCase).Where(x=>File.Exists(Path.Combine(x,"Game.exe"))||File.Exists(Path.Combine(x,"nw.exe"))).ToArray();
        if(games.Length!=1)throw new IOException("Archive/folder must contain one complete game. Choose the specific game folder.");Store.Detect(games[0],"Game");return games[0];
    }
    public static Profile InstallOriginal(string archive,string installRoot,string targetId){
        Directory.CreateDirectory(installRoot);string root=Store.Child(installRoot,"Original-"+Guid.NewGuid().ToString("N").Substring(0,8));
        if(archive.EndsWith(".zip",StringComparison.OrdinalIgnoreCase))using(var zip=ZipFile.OpenRead(archive)){long need=zip.Entries.Sum(x=>x.Length)+256L*1024*1024;if(new DriveInfo(Path.GetPathRoot(installRoot)).AvailableFreeSpace<need)throw new IOException("Insufficient space to extract the full game archive.");}
        Store.Extract(archive,root,false,true);string game=FindGameRoot(root),detected=Identify(game);
        if(detected==null||targetId!=null&&detected!=targetId)throw new IOException("The extracted game does not match the selected reference version. It was not registered; existing games are unchanged.");
        var p=Store.Detect(game,DetectedName(game));p.Note="Installed from original archive; own game files and saves";Directory.CreateDirectory(p.SavePath);return p;
    }
}
