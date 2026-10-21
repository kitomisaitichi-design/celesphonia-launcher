using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EnglishManifest {
    public int Format {get;set;} public string VersionId {get;set;} public string Name {get;set;}
    public string Mode {get;set;} public Dictionary<string,string> Fingerprints {get;set;}
    public int TotalTexts {get;set;} public int TranslatedTexts {get;set;} public int ReviewedTexts {get;set;}
    public int ReferenceTexts {get;set;} public bool Complete {get;set;}
    public int DerivedTexts {get;set;}
    public int? IndexedEventFieldsPending {get;set;}
    public int? OtherDisplayReviewFields {get;set;}
    public int? DeveloperVariableLabels {get;set;}
    public string DictionaryHash {get;set;} public string CatalogHash {get;set;}
}
public class LocalizedImageInfo {
    public string originalSHA256 {get;set;}
    public string localizedSHA256 {get;set;}
    public int width {get;set;}
    public int height {get;set;}
    public string kind {get;set;}
    public string nativePath {get;set;}
    public string nativeName {get;set;}
    public string localizedName {get;set;}
    public bool renderOnly {get;set;}
    public bool nativePictureUnchanged {get;set;}
}
public class LocalizedImageManifest {
    public int format {get;set;}
    public string[] versionIds {get;set;}
    public Dictionary<string,LocalizedImageInfo> images {get;set;}
}
public class EnglishState {
    public int Format {get;set;} public string VersionId {get;set;} public string Entry {get;set;}
    public string OriginalHash {get;set;} public string PatchedHash {get;set;}
    public Dictionary<string,string> Assets {get;set;} public int TotalTexts {get;set;}
    public int TranslatedTexts {get;set;} public string Updated {get;set;} public string DictionaryHash {get;set;}
}
public class EnglishStatus {
    public string VersionId; public bool Supported,Enabled,Modified,UpdateAvailable; public string Detail;
    public EnglishManifest Pack; public int AppliedTexts,AvailableTexts,TotalTexts;
    public string Short {get{return VersionId=="ch-current"?"English reference":!Supported?"No overlay":Modified?"Check files":Enabled?UpdateAvailable?"EN update ready":"EN partial · "+AppliedTexts.ToString("N0"):Pack==null?"Pack needed":"Original · EN ready";}}
}
public static class EnglishSupport {
    const string FolderName="celesphonia-english";
    static readonly Regex Controls=new Regex(@"(?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|%[1-9]|<(?:/?[A-Za-z][^>\r\n]*)>|(?:if|en|cmpd|cmxs)\([^\r\n]*\)");
    static readonly Regex Cjk=new Regex(@"[\u3040-\u30ff\u3400-\u9fff]");
    static string Folder(string root){return Path.Combine(Store.WebRoot(root),FolderName);}
    static string StateFile(string root){return Path.Combine(Folder(root),"state.json");}
    static EnglishState ReadState(string root){string file=StateFile(root);return File.Exists(file)?Store.Json.Deserialize<EnglishState>(File.ReadAllText(file,Encoding.UTF8)):null;}
    static string PackFolder(string id){return Store.Child(Path.Combine(Store.Home,"English"),id);}
    static LocalizedImageManifest ImagesFor(string id){
        byte[] bytes=EnglishRelease.ArtManifest();
        if(bytes==null)return null;
        var result=Store.Json.Deserialize<LocalizedImageManifest>(Encoding.UTF8.GetString(bytes));
        if(result==null||result.format!=1||result.versionIds==null||result.images==null)
            throw new IOException("Invalid localized help-art manifest.");
        return result.versionIds.Contains(id,StringComparer.Ordinal)?result:null;
    }
    static EnglishManifest ReadPack(string id){return Store.Json.Deserialize<EnglishManifest>(Encoding.UTF8.GetString(EnglishRelease.Read(id,"manifest.json")));}
    static string Entry(string root){var package=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"package.json"),Encoding.UTF8));return Store.Child(root,Convert.ToString(package["main"]));}
    static void Atomic(string file,byte[] bytes){string temp=file+".new";File.WriteAllBytes(temp,bytes);if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}
    static void JsonWrite(string file,object value){Atomic(file,new UTF8Encoding(false).GetBytes(Store.Json.Serialize(value)));}
    public static bool Compatible(string source,string target){return source.Count(c=>c=='\n')==target.Count(c=>c=='\n')&&Controls.Matches(source).Cast<Match>().Select(x=>x.Value).SequenceEqual(Controls.Matches(target).Cast<Match>().Select(x=>x.Value));}
    static void ValidateState(string root,EnglishState state,bool assets){
        if(state.Format!=1||state.Assets==null||state.VersionId=="ch-current")throw new IOException("Invalid English overlay record.");
        string actualEntry=Entry(root),recordedEntry=Store.Child(root,state.Entry.Replace('/','\\'));
        if(!actualEntry.Equals(recordedEntry,StringComparison.OrdinalIgnoreCase))throw new IOException("The game entry page changed since this overlay was applied.");
        string backup=Path.Combine(Folder(root),"original-entry.bin");
        if(!File.Exists(backup)||Store.Hash(backup)!=state.OriginalHash)throw new IOException("English overlay's original-entry backup is missing or changed. It cannot be safely restored.");
        string hash=Store.Hash(actualEntry);
        if(hash!=state.OriginalHash&&hash!=state.PatchedHash)throw new IOException("The game entry page was changed by another tool. Restore that change first; the launcher will preserve it.");
        if(assets&&hash==state.PatchedHash)foreach(var item in state.Assets){string file=Store.Child(Folder(root),item.Key);if(!File.Exists(file)||Store.Hash(file)!=item.Value)throw new IOException("English overlay file is missing or changed: "+item.Key);}
    }
    // This markup is byte-for-byte reconstructible from the original entry.
    // Stale pre-state English installs can be recovered only when that exact
    // reversible injection and a matching original-entry.bin are both present.
    internal const string LegacyInjection="<!-- Celesphonia reversible English display overlay -->\r\n        <script src=\"celesphonia-english/dictionary.js\"></script>\r\n        <script src=\"celesphonia-english/runtime.js\"></script>\r\n        ";
    internal const string Injection=LegacyInjection+"<script src=\"celesphonia-english/stream-repeat-rescue.js\"></script>\r\n        ";
    static byte[] StreamRepair(){
        using(var source=typeof(EnglishSupport).Assembly.GetManifestResourceStream("Celesphonia.StreamRepeatChoiceRescue.js")){
            if(source==null)throw new IOException("The streaming compatibility repair is missing from this launcher.");
            using(var output=new MemoryStream()){source.CopyTo(output);return output.ToArray();}
        }
    }
    public static string OriginalFile(string root,string file){
        if(!Path.GetExtension(file).Equals(".html",StringComparison.OrdinalIgnoreCase)||
            !file.Equals(Entry(root),StringComparison.OrdinalIgnoreCase))return file;
        var state=ReadState(root);
        if(state!=null){
            ValidateState(root,state,false);
            return Path.Combine(Folder(root),"original-entry.bin");
        }
        // A missing state file must not make copied games inherit an injected
        // index.html pointing at discarded old English runtime assets.
        string modified=File.ReadAllText(file,Encoding.UTF8);
        if(modified.IndexOf("celesphonia-english/",StringComparison.OrdinalIgnoreCase)<0)return file;
        string original=Path.Combine(Folder(root),"original-entry.bin");
        if(!File.Exists(original))throw new IOException(
            "This game has an unmanaged English-modified index.html and no original-entry.bin. Choose its untouched original game archive or restore its entry page before installing; no files were deleted.");
        byte[] source=File.ReadAllBytes(original);
        string html=Encoding.UTF8.GetString(source);
        Match script=Regex.Match(html,@"<script\b[^>]*\bsrc\s*=\s*['""]js/main\.js['""][^>]*>",RegexOptions.IgnoreCase);
        if(!script.Success||html.IndexOf("celesphonia-english/",StringComparison.OrdinalIgnoreCase)>=0)
            throw new IOException("The English index backup is not an intact original page.");
        byte[] exact=new UTF8Encoding(false).GetBytes(html.Insert(script.Index,Injection));
        if(!File.ReadAllBytes(file).SequenceEqual(exact)&&!File.ReadAllBytes(file).SequenceEqual(new UTF8Encoding(false).GetBytes(html.Insert(script.Index,LegacyInjection))))
            throw new IOException("The English index.html differs from the known reversible injection. Keep its custom edits and supply an intact original game/entry page; automatic replacement is unsafe.");
        return original;
    }
    // Source-copy validation intentionally disregards missing *English-only*
    // runtime files, but never ignores a changed index page or game scripts.
    public static void VerifyForCopy(string root){
        var state=ReadState(root);
        if(state!=null){
            ValidateState(root,state,false);
            if(UniversalSupport.Identify(root)!=state.VersionId)
                throw new IOException("The English overlay record belongs to a different game version.");
        }
        OriginalFile(root,Entry(root));
    }
    public static void Verify(string root){var state=ReadState(root);if(state!=null){ValidateState(root,state,true);if(UniversalSupport.Identify(root)!=state.VersionId)throw new IOException("The English overlay belongs to a different game version. Restore Original text before changing this installation.");}}
    public static EnglishStatus Status(Profile profile){
        var result=new EnglishStatus();if(profile==null)return result;
        result.VersionId=UniversalSupport.Identify(profile.Root);
        if(result.VersionId==null){result.Detail="This installation does not match a supported version.";return result;}
        if(result.VersionId=="ch-current"){result.Detail="Existing English CH reference. Overlays apply only to Gorpg, Maple, 3.3.4 and 3.3.5 trial.";return result;}
        result.Supported=true;
        try{result.Pack=ReadPack(result.VersionId);if(result.Pack!=null){result.AvailableTexts=result.Pack.TranslatedTexts;result.TotalTexts=result.Pack.TotalTexts;string statistics=Path.Combine(PackFolder(result.VersionId),"user-statistics.json");if(File.Exists(statistics)){var stats=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(statistics,Encoding.UTF8));if(Convert.ToString(stats["DictionaryHash"])==result.Pack.DictionaryHash){result.AvailableTexts+=Convert.ToInt32(stats["AdditionalTexts"]);result.TotalTexts+=Convert.ToInt32(stats["ExtraCatalogTexts"]);}}}var state=ReadState(profile.Root);if(state!=null){Verify(profile.Root);result.Enabled=Store.Hash(Entry(profile.Root))==state.PatchedHash;result.AppliedTexts=state.TranslatedTexts;if(result.Enabled)result.TotalTexts=state.TotalTexts;}result.Detail=result.Enabled?"English display overlay is enabled. Untranslated lines remain in their original language.":"Original text is active. Enable this version's English overlay to use the available translations.";}catch(Exception e){result.Modified=true;result.Detail=e.Message;}
        if(result.Enabled&&result.Pack!=null&&!result.Modified){
            var state=ReadState(profile.Root);
            result.UpdateAvailable=state.DictionaryHash!=result.Pack.DictionaryHash||
                result.AppliedTexts!=result.AvailableTexts||
                state.Assets["runtime.js"]!=EnglishRelease.Hash(EnglishRelease.Runtime());
            string repairHash;
            if(!state.Assets.TryGetValue("stream-repeat-rescue.js",out repairHash)||repairHash!=EnglishRelease.Hash(StreamRepair()))result.UpdateAvailable=true;
            var pictures=ImagesFor(result.VersionId);
            if(pictures!=null)foreach(var image in pictures.images){
                var originalAsset=Store.Child(Store.WebRoot(profile.Root),
                    image.Value.nativePath.Replace('/','\\'));
                // Incomplete or reduced game copies might not contain this
                // optional tutorial asset. The English *text* pack still
                // applies; no visual override is installed for a missing image.
                if(!File.Exists(originalAsset))continue;
                if(Store.Hash(originalAsset)!=image.Value.originalSHA256)continue;
                string recorded;
                if(!state.Assets.TryGetValue("images/"+image.Key,out recorded)||
                   recorded!=EnglishRelease.Hash(EnglishRelease.Art(image.Key)))
                    result.UpdateAvailable=true;
            }
        }
        if(result.Pack!=null&&!result.Modified){var problems=EnglishRelease.LooseProblems(result.VersionId);if(problems.Count>0)result.Detail+="\r\nUsing the verified built-in English pack; "+problems.Count+" loose pack files are missing or from a different release. Extract the complete ZIP into "+Store.Home+" to refresh them.";}
        return result;
    }
    static Dictionary<string,object> Catalog(string id,EnglishManifest pack){byte[] bytes=EnglishRelease.Read(id,"catalog.json");if(EnglishRelease.Hash(bytes)!=pack.CatalogHash)throw new IOException("Bundled English catalog does not match its manifest: "+id);var result=Store.Json.Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(bytes));string observed=Path.Combine(PackFolder(id),"observed-catalog.json");if(File.Exists(observed))foreach(var item in Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(observed,Encoding.UTF8)))if(Cjk.IsMatch(item.Key)&&item.Key.Length<6000&&!result.ContainsKey(item.Key))result[item.Key]=item.Value;
        // Portable catalogs deduplicate an identical source value: the JSON
        // object key is itself the complete native source text. Restore the
        // redundant field in memory for existing export/MTool consumers.
        foreach(var item in result){
            var row=item.Value as Dictionary<string,object>;
            if(row!=null){object source;if(row.TryGetValue("source",out source)){if(Convert.ToString(source)!=item.Key)throw new IOException("Catalog source key does not match its metadata.");}
                else row["source"]=item.Key;}
        }
        return result;}
    static Dictionary<string,string> Dictionary(string id,EnglishManifest pack){
        byte[] bytes=EnglishRelease.Read(id,"dictionary.json");if(EnglishRelease.Hash(bytes)!=pack.DictionaryHash)throw new IOException("Bundled English dictionary does not match its manifest: "+id);
        var catalog=Catalog(id,pack);var data=Store.Json.Deserialize<Dictionary<string,string>>(Encoding.UTF8.GetString(bytes));
        foreach(var item in data)if(!catalog.ContainsKey(item.Key)||item.Value==null||!Compatible(item.Key,item.Value))throw new IOException("Unsafe or unknown dictionary entry: "+item.Key.Substring(0,Math.Min(80,item.Key.Length)));
        string user=Path.Combine(PackFolder(id),"user-translations.json");
        if(File.Exists(user))foreach(var item in Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(user,Encoding.UTF8)))if(catalog.ContainsKey(item.Key)&&item.Value!=null&&item.Key!=item.Value&&Compatible(item.Key,item.Value))data[item.Key]=item.Value;
        return data;
    }
    static void RequireClosed(Profile profile){var settings=Store.Read();if(!settings.Profiles.Any(x=>x.Root.Equals(profile.Root,StringComparison.OrdinalIgnoreCase)))settings.Profiles.Add(profile);if(Store.Running(settings))throw new IOException("Save and close the game before changing its English overlay.");}
    public static void Apply(Profile profile){RequireClosed(profile);ApplyCore(profile,null);}
    internal static void ApplyCore(Profile profile,string runtimeOverride){
        string id=UniversalSupport.Identify(profile.Root);if(id==null||id=="ch-current")throw new IOException("Choose Gorpg 1.23, Maple 1.7.2, 3.3.4, or 3.3.5 trial. CH is an English reference only.");
        var pack=ReadPack(id);if(pack==null||pack.Format!=1||pack.VersionId!=id||pack.Mode!="display-only")throw new IOException("This version's English overlay pack is missing or unsupported.");
        string web=Store.WebRoot(profile.Root);foreach(var item in pack.Fingerprints)if(Store.Hash(Store.Child(web,item.Key))!=item.Value)throw new IOException("Game version does not match the English overlay.");
        var data=Dictionary(id,pack);var old=ReadState(profile.Root);string folder=Folder(profile.Root),entry=Entry(profile.Root);byte[] original;
        if(old!=null){ValidateState(profile.Root,old,true);original=File.ReadAllBytes(Path.Combine(folder,"original-entry.bin"));}
        else{if(Directory.Exists(folder)&&Directory.EnumerateFileSystemEntries(folder).Any())throw new IOException("An unrecognized celesphonia-english folder already exists. Choose a clean game entry before applying this overlay.");original=File.ReadAllBytes(entry);}
        string html=Encoding.UTF8.GetString(original);var main=Regex.Match(html,@"<script\b[^>]*\bsrc\s*=\s*['""]js/main\.js['""][^>]*>",RegexOptions.IgnoreCase);
        if(!main.Success||html.Contains("celesphonia-english/"))throw new IOException("Unsupported or already modified game entry page.");
        string markup=Injection;
        byte[] patched=new UTF8Encoding(false).GetBytes(html.Insert(main.Index,markup));
        byte[] runtimeBytes=runtimeOverride==null?EnglishRelease.Runtime():File.ReadAllBytes(runtimeOverride);
        Directory.CreateDirectory(folder);if(old==null)Atomic(Path.Combine(folder,"original-entry.bin"),original);
        // Import conditional-choice labels only for rendering; their original expression is preserved.
        var display=new Dictionary<string,string>(data);foreach(var item in data){var suffix=Regex.Match(item.Key,@"(?:if|en|cmpd|cmxs)\([^\r\n]*\)$");if(suffix.Success&&item.Value.EndsWith(suffix.Value,StringComparison.Ordinal)){string a=item.Key.Substring(0,suffix.Index).TrimEnd(),b=item.Value.Substring(0,item.Value.Length-suffix.Length).TrimEnd();if(a.Length>0&&!display.ContainsKey(a))display[a]=b;}}
        foreach(var item in data){var prefix=Regex.Match(item.Key,@"^\\[Nn]<[^\r\n]*?>");if(prefix.Success&&item.Value.StartsWith(prefix.Value,StringComparison.Ordinal)){string a=item.Key.Substring(prefix.Length),b=item.Value.Substring(prefix.Length);if(a.Length>0&&!display.ContainsKey(a))display[a]=b;}}
        // Notification plugins prepend their own icons/colors and sometimes spaces.
        // Derive an exact body alias only when source and target share the same
        // leading/trailing cosmetic codes; internal escapes remain in place.
        foreach(var item in data){
            var leading=Regex.Match(item.Key,@"^(?:\\[cCiI]\[\d+\]\s*)+");var trailing=Regex.Match(item.Key,@"(?:\s*\\[cCiI]\[\d+\])+$");
            if(!leading.Success&&!trailing.Success)continue;
            string prefix=leading.Value,suffix=trailing.Value;
            if(!item.Value.StartsWith(prefix,StringComparison.Ordinal)||!item.Value.EndsWith(suffix,StringComparison.Ordinal)||prefix.Length+suffix.Length>=item.Key.Length)continue;
            string a=item.Key.Substring(prefix.Length,item.Key.Length-prefix.Length-suffix.Length).Trim(),b=item.Value.Substring(prefix.Length,item.Value.Length-prefix.Length-suffix.Length).Trim();
            if(a.Length>0&&Compatible(a,b)&&!display.ContainsKey(a))display[a]=b;
        }
        string webPath=web.Equals(profile.Root.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)?"":web.Substring(profile.Root.TrimEnd('\\').Length+1).Replace('\\','/');
        // Only cataloged short UI/name labels participate in dynamic segment matching.
        // Full dialogue still requires an exact message or complete line match.
        var labels=new Dictionary<string,string>();var descriptions=new Dictionary<string,string>();var humanLabels=new Dictionary<string,string>();var catalog=Catalog(id,pack);
        foreach(var item in data){var row=catalog[item.Key] as Dictionary<string,object>;if(row==null||!row.ContainsKey("kinds"))continue;var kinds=row["kinds"] as System.Collections.IEnumerable;if(kinds!=null&&kinds.Cast<object>().Select(Convert.ToString).Any(x=>x=="picture-label")&&Compatible(item.Key,item.Value))humanLabels[item.Key]=item.Value;}
        // Native state notifications concatenate an actor name and a suffix.
        // Build display templates from cataloged state messages, without changing
        // the database messages or the log strings saved by the game.
        foreach(var item in data){
            var row=catalog[item.Key] as Dictionary<string,object>;if(row==null||!row.ContainsKey("contexts"))continue;
            var contexts=row["contexts"] as System.Collections.IEnumerable;
            // Ambient NPC sentences are stored with actor-name commands, then
            // composed as "Speaker : sentence" by the native ticker. Translate
            // that complete field after composition without rewriting actors.
            var kinds=row.ContainsKey("kinds")?row["kinds"] as System.Collections.IEnumerable:null;
            bool battleLine=contexts!=null&&contexts.Cast<object>().Select(Convert.ToString).Any(x=>Regex.IsMatch(x,@"^(?:New)?BattleMessage\.json/"));
            if(kinds!=null&&(battleLine||kinds.Cast<object>().Select(Convert.ToString).Any(x=>x=="character"||x=="display-value"||x=="notification"||x=="script-display"))){
                foreach(string separator in new[]{" : ",": ","："}){
                    string source="%9"+separator+item.Key,target="%9"+separator+item.Value;
                    if(!display.ContainsKey(source)&&Compatible(source,target))display[source]=target;
                }
            }
            if(contexts!=null&&contexts.Cast<object>().Select(Convert.ToString).Any(x=>x.EndsWith("/description-first-line",StringComparison.Ordinal))&&Compatible(item.Key,item.Value)&&!Controls.IsMatch(item.Key))descriptions[item.Key]=item.Value;
            if(contexts==null||!contexts.Cast<object>().Select(Convert.ToString).Any(x=>Regex.IsMatch(x,@"^States\.json/\d+/message[1-4]$")))continue;
            if(item.Key.Contains("%9")||!Cjk.IsMatch(item.Key)||item.Key.StartsWith("\\",StringComparison.Ordinal))continue;
            string a="%9"+item.Key,b="%9 "+item.Value.TrimStart();if(!display.ContainsKey(a))display[a]=b;
        }
        foreach(var item in data){
            if(item.Key.Length>24||!Regex.IsMatch(item.Key,@"^(?:HP|MP|SP|TP)?[\u3040-\u30ff\u3400-\u9fff]+$")||item.Value==item.Key)continue;
            var row=catalog[item.Key] as Dictionary<string,object>;if(row==null||!row.ContainsKey("kinds"))continue;
            var kinds=row["kinds"] as System.Collections.IEnumerable;
            var contexts=row.ContainsKey("contexts")?row["contexts"] as System.Collections.IEnumerable:null;
            bool name=contexts!=null&&contexts.Cast<object>().Select(Convert.ToString).Any(x=>x.EndsWith("/name",StringComparison.Ordinal)||x.EndsWith("/ParaName",StringComparison.Ordinal));
            if(name||kinds!=null&&kinds.Cast<object>().Select(Convert.ToString).Any(x=>x=="plugin-ui"||x=="ui-label"||x=="menu"||x=="character"||x=="map-name"||x=="display-table"||x=="display-value"||x=="plugin-display"||x=="picture-label"))labels[item.Key]=item.Value;
        }
        // Prefix states decorate enemy names; keep their provenance separate
        // so only a complete recognized enemy caption can be composed at render time.
        var enemyPrefixes=new Dictionary<string,string>();var enemyNames=new Dictionary<string,string>();
        foreach(var item in data){
            var row=catalog[item.Key] as Dictionary<string,object>;if(row==null)continue;
            var kinds=row.ContainsKey("kinds")?row["kinds"] as System.Collections.IEnumerable:null;
            var contexts=row.ContainsKey("contexts")?row["contexts"] as System.Collections.IEnumerable:null;
            if(kinds!=null&&kinds.Cast<object>().Select(Convert.ToString).Any(x=>x=="enemy-prefix"))enemyPrefixes[item.Key]=item.Value;
            if(contexts!=null&&contexts.Cast<object>().Select(Convert.ToString).Any(x=>Regex.IsMatch(x,@"^Enemies\.json/\d+/name$"))){enemyNames[item.Key]=item.Value;enemyNames[item.Value]=item.Value;}
            // Native battle logs first format translated system messages around
            // untranslated battler captions. Recognize that complete grammar,
            // then translate its referenced name slots at render time.
            if(contexts!=null&&contexts.Cast<object>().Select(Convert.ToString).Any(x=>x.StartsWith("System.json/terms/messages/",StringComparison.Ordinal)||x.StartsWith("SystemMessage.json/",StringComparison.Ordinal))&&Regex.IsMatch(item.Value,@"%[1-9]"))display[item.Value]=item.Value;
            if(contexts!=null&&contexts.Cast<object>().Select(Convert.ToString).Any(x=>Regex.IsMatch(x,@"^Skills\.json/\d+/message1$"))){
                string source="%9"+item.Key,target="%9 "+item.Value.TrimStart();
                if(!display.ContainsKey(source)&&Compatible(source,target))display[source]=target;
            }
        }
        var pictureRedirects=new Dictionary<string,string>();
        var animationRedirects=new Dictionary<string,string>();
        var artwork=ImagesFor(id);
        if(artwork!=null)foreach(var image in artwork.images){
            string name=image.Key;var info=image.Value;
            if(info==null||!Regex.IsMatch(name,@"^[\p{L}\p{N}_-]+\.png$")||
               info.nativePath==null||!Regex.IsMatch(info.nativePath,@"^img/(?:pictures|animations)/[^/\\]+\.png$")||
               string.IsNullOrWhiteSpace(info.nativeName)||string.IsNullOrWhiteSpace(info.localizedName)||
               !(info.kind=="picture"||info.kind=="animation")||
               !info.renderOnly||!info.nativePictureUnchanged)
                throw new IOException("Unsupported localized artwork mapping.");
            string originalAsset=Store.Child(web,info.nativePath.Replace('/','\\'));
            if(!File.Exists(originalAsset))continue;
            if(Store.Hash(originalAsset)!=info.originalSHA256)
                throw new IOException("Creator help/animation image changed: "+info.nativePath);
            var bytes=EnglishRelease.Art(name);
            if(EnglishRelease.Hash(bytes)!=info.localizedSHA256)
                throw new IOException("Localized English artwork was altered: "+name);
            if(info.kind=="picture")pictureRedirects.Add(info.nativeName,info.localizedName);
            else animationRedirects.Add(info.nativeName,info.localizedName);
        }
        var payload=new UTF8Encoding(false).GetBytes("window.CelesphoniaEnglishMeta="+Store.Json.Serialize(new{version=id,texts=data.Count,mode="display-only",webPath=webPath})+";\nwindow.CelesphoniaEnglishImageOverrides="+Store.Json.Serialize(new{picture=pictureRedirects,animation=animationRedirects})+";\nwindow.CelesphoniaEnglishDictionary="+Store.Json.Serialize(display).Replace("\u2028","\\u2028").Replace("\u2029","\\u2029")+";\nwindow.CelesphoniaEnglishLabels="+Store.Json.Serialize(labels).Replace("\u2028","\\u2028").Replace("\u2029","\\u2029")+";\nwindow.CelesphoniaEnglishHumanLabels="+Store.Json.Serialize(humanLabels).Replace("\u2028","\\u2028").Replace("\u2029","\\u2029")+";\nwindow.CelesphoniaEnglishDescriptions="+Store.Json.Serialize(descriptions).Replace("\u2028","\\u2028").Replace("\u2029","\\u2029")+";\nwindow.CelesphoniaEnglishEnemyPrefixes="+Store.Json.Serialize(enemyPrefixes)+";\nwindow.CelesphoniaEnglishEnemyNames="+Store.Json.Serialize(enemyNames)+";\n");
        var state=new EnglishState{Format=1,VersionId=id,DictionaryHash=pack.DictionaryHash,Entry=entry.Substring(profile.Root.TrimEnd('\\').Length+1).Replace('\\','/'),OriginalHash=Store.Hash(Path.Combine(folder,"original-entry.bin")),TotalTexts=Catalog(id,pack).Count,TranslatedTexts=data.Count,Updated=DateTimeOffset.Now.ToString("o"),Assets=new Dictionary<string,string>()};
        // Restore entry before updating assets: a failed update leaves the original game launchable.
        if(old!=null)Atomic(entry,original);
        Atomic(Path.Combine(folder,"dictionary.js"),payload);Atomic(Path.Combine(folder,"runtime.js"),runtimeBytes);
        Atomic(Path.Combine(folder,"stream-repeat-rescue.js"),StreamRepair());
        foreach(string name in new[]{"dictionary.js","runtime.js","stream-repeat-rescue.js"})state.Assets[name]=Store.Hash(Path.Combine(folder,name));
        if(artwork!=null&&artwork.images.Count>0){
            Directory.CreateDirectory(Path.Combine(folder,"images"));
            foreach(var image in artwork.images){
                string originalAsset=Store.Child(web,image.Value.nativePath.Replace('/','\\'));
                if(!File.Exists(originalAsset))continue;
                string destination=Path.Combine(folder,"images",image.Key);
                Atomic(destination,EnglishRelease.Art(image.Key));
                state.Assets["images/"+image.Key]=Store.Hash(destination);
            }
        }
        string digest;using(var sha=System.Security.Cryptography.SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(patched)).Replace("-","").ToLowerInvariant();state.PatchedHash=digest;
        JsonWrite(Path.Combine(folder,"state.json"),state);Atomic(entry,patched);Verify(profile.Root);
    }
    public static void Restore(Profile profile){RequireClosed(profile);var state=ReadState(profile.Root);if(state==null)return;ValidateState(profile.Root,state,false);Atomic(Entry(profile.Root),File.ReadAllBytes(Path.Combine(Folder(profile.Root),"original-entry.bin")));}
    public static string Import(Profile profile,string input){
        RequireClosed(profile);var status=Status(profile);if(!status.Supported||status.Pack==null||status.Modified)throw new IOException("Select an intact supported version with an English pack.");
        var catalog=Catalog(status.VersionId,status.Pack);var observed=new Dictionary<string,object>();string observedFile=Path.Combine(PackFolder(status.VersionId),"observed-catalog.json");if(File.Exists(observedFile))observed=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(observedFile,Encoding.UTF8));
        string seen=Path.Combine(Folder(profile.Root),"seen-untranslated.json");var state=ReadState(profile.Root);if(state!=null&&state.VersionId==status.VersionId&&File.Exists(seen)){ValidateState(profile.Root,state,false);foreach(var item in Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(seen,Encoding.UTF8)))if(item.Key==item.Value&&Cjk.IsMatch(item.Key)&&item.Key.Length<6000&&!catalog.ContainsKey(item.Key))catalog[item.Key]=new{source=item.Key,kinds=new[]{"seen-display"},contexts=new[]{"Observed during render in this version"}};}
        var supplied=Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(input,Encoding.UTF8));var accepted=new Dictionary<string,string>();int ignored=0,unchanged=0;
        foreach(var item in supplied){if(string.IsNullOrWhiteSpace(item.Value)||!catalog.ContainsKey(item.Key)){ignored++;continue;}if(item.Key==item.Value){unchanged++;continue;}if(!Compatible(item.Key,item.Value))throw new IOException("Import rejected: control codes, plugin conditions or line count changed in: "+item.Key.Substring(0,Math.Min(100,item.Key.Length)));accepted[item.Key]=item.Value;}
        if(accepted.Count==0)return "No translated values found. "+unchanged.ToString("N0")+" unchanged template entries; "+ignored.ToString("N0")+" outside this version's display catalog.";
        string file=Path.Combine(PackFolder(status.VersionId),"user-translations.json");var merged=File.Exists(file)?Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(file,Encoding.UTF8)):new Dictionary<string,string>();foreach(var item in accepted)merged[item.Key]=item.Value;JsonWrite(file,merged);
        var built=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(PackFolder(status.VersionId),"catalog.json"),Encoding.UTF8));foreach(var item in accepted)if(!built.ContainsKey(item.Key))observed[item.Key]=catalog[item.Key];JsonWrite(observedFile,observed);
        var defaults=Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(PackFolder(status.VersionId),"dictionary.json"),Encoding.UTF8));JsonWrite(Path.Combine(PackFolder(status.VersionId),"user-statistics.json"),new{DictionaryHash=status.Pack.DictionaryHash,AdditionalTexts=merged.Keys.Count(x=>!defaults.ContainsKey(x)),ExtraCatalogTexts=observed.Keys.Count(x=>!built.ContainsKey(x))});
        if(status.Enabled)Apply(profile);return "Imported "+accepted.Count.ToString("N0")+" translations for "+status.Pack.Name+"; ignored "+ignored.ToString("N0")+" unrelated entries.";
    }
    public static void ExportRemaining(Profile profile,string target){var status=Status(profile);if(status.Pack==null)throw new IOException("Choose a version with an English overlay pack.");var known=Dictionary(status.VersionId,status.Pack);var catalog=Catalog(status.VersionId,status.Pack);JsonWrite(target,catalog.Keys.Where(x=>!known.ContainsKey(x)).ToDictionary(x=>x,x=>x));}
    public static int SeenCount(Profile profile){try{string file=Path.Combine(Folder(profile.Root),"seen-untranslated.json");return File.Exists(file)?Store.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(file,Encoding.UTF8)).Count:0;}catch{return 0;}}
}
