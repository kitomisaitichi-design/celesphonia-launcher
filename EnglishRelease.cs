using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

// One immutable generation: never combine a newer manifest with an older dictionary.
// Loose managed files can be incomplete after extraction. User overrides stay separate.
public static class EnglishRelease {
    static readonly Lazy<Dictionary<string,byte[]>> bundled=new Lazy<Dictionary<string,byte[]>>(Load);
    public static string Hash(byte[] b){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
    static Dictionary<string,byte[]> Load(){
        var result=new Dictionary<string,byte[]>();
        using(var stream=typeof(EnglishRelease).Assembly.GetManifestResourceStream("Celesphonia.EnglishRelease.zip")){
            if(stream==null)throw new IOException("This launcher build has no bundled English release. Extract the complete current launcher ZIP.");
            using(var zip=new ZipArchive(stream,ZipArchiveMode.Read))foreach(var entry in zip.Entries){
                using(var input=entry.Open())using(var output=new MemoryStream()){input.CopyTo(output);result.Add(entry.FullName,output.ToArray());}
            }
        }
        var manifest=Store.Json.Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(result["release.json"]));
        var files=manifest["files"] as Dictionary<string,object>;
        if(Convert.ToInt32(manifest["format"])!=1||files==null||!files.ContainsKey("EnglishRuntime.js"))throw new IOException("Invalid bundled English release.");
        var packs=files.Keys.Where(x=>x.StartsWith("English/")&&x.EndsWith("/manifest.json")&&
            !x.StartsWith("English/ImageLocalization/",StringComparison.Ordinal))
            .Select(x=>x.Split('/')[1]).ToArray();
        var pictures=files.Keys.Where(x=>x.StartsWith("English/ImageLocalization/",StringComparison.Ordinal)).ToArray();
        if(packs.Length==0||files.Count!=1+3*packs.Length+pictures.Length||
           packs.Any(id=>new[]{"manifest.json","dictionary.json","catalog.json"}.Any(name=>!files.ContainsKey("English/"+id+"/"+name)))||
           pictures.Length>0&&!files.ContainsKey("English/ImageLocalization/manifest.json"))
            throw new IOException("Incomplete bundled English release.");
        foreach(var file in files)if(!result.ContainsKey(file.Key)||Hash(result[file.Key])!=Convert.ToString(file.Value))throw new IOException("Bundled English release failed integrity verification: "+file.Key);
        return result;
    }
    public static byte[] Read(string id,string name){return bundled.Value["English/"+id+"/"+name];}
    public static byte[] Runtime(){return bundled.Value["EnglishRuntime.js"];}
    public static byte[] ArtManifest(){byte[] result;return bundled.Value.TryGetValue("English/ImageLocalization/manifest.json",out result)?result:null;}
    public static byte[] Art(string name){
        if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"^[\p{L}\p{N}_-]+\.png$"))
            throw new IOException("Unsupported localized picture name.");
        return bundled.Value["English/ImageLocalization/"+name];
    }
    public static List<string> LooseProblems(string id){
        var result=new List<string>();
        foreach(var name in new[]{"manifest.json","dictionary.json","catalog.json"}){
            string file=Store.Child(Store.Home,Path.Combine("English",id,name));
            if(!File.Exists(file)){result.Add("Missing: "+file);continue;}
            try{if(Store.Hash(file)!=Hash(Read(id,name)))result.Add("Different release or changed: "+file);}catch(IOException){result.Add("Unreadable: "+file);}
        }
        return result;
    }
    public static object Report(){
        var rows=new List<object>();
        foreach(string id in bundled.Value.Keys.Where(x=>x.StartsWith("English/")&&x.EndsWith("/manifest.json")&&
            !x.StartsWith("English/ImageLocalization/",StringComparison.Ordinal))
            .Select(x=>x.Split('/')[1]).OrderBy(x=>x)){
            var pack=Store.Json.Deserialize<EnglishManifest>(Encoding.UTF8.GetString(Read(id,"manifest.json")));
            if(Hash(Read(id,"dictionary.json"))!=pack.DictionaryHash||Hash(Read(id,"catalog.json"))!=pack.CatalogHash)throw new IOException("Bundled manifest does not match its pack: "+id);
            rows.Add(new{version=id,dictionaryHash=pack.DictionaryHash,catalogHash=pack.CatalogHash,availableTexts=pack.TranslatedTexts,looseProblems=LooseProblems(id)});
        }
        return new{status="PASS",packSource="verified embedded release",runtimeHash=Hash(Runtime()),variants=rows};
    }
}
