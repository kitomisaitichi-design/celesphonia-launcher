using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public class DirectFreshInstall1052 {
    public static int Main(string[] args) {
        string report=Path.Combine(Store.Home,"Tests","direct-fresh-1052-install-report.json");
        string stage=args.Length>2?args[2]:Path.Combine(Store.Home,"Tests","fresh-1052-isolated");
        string cleanRoot=null,installedRoot=null;
        try {
            if(args.Length<2)throw new Exception("Expected clean GoRPG ZIP or folder, creator Ver1.052 bundle, optional test install folder.");
            string source=args[0],bundle=args[1];
            if(!File.Exists(bundle))throw new IOException("Missing creator archive: "+bundle);
            Directory.CreateDirectory(stage);
            Profile clean=Directory.Exists(source)?Store.Detect(UniversalSupport.FindGameRoot(source),"GoRPG 1.23 test base"):UniversalSupport.InstallOriginal(source,stage,"gorpg-1-23");
            cleanRoot=clean.Root;RecipeSupport.ValidateBase(cleanRoot);
            string id="three-in-one-3-3-5-v1-052";
            var recipe=RecipeSupport.Catalog().Recipes.First(x=>x.Id==id);
            string creatorHash=Store.Hash(bundle);
            if(creatorHash!=recipe.ArchiveHash)throw new IOException("The creator archive SHA-256 does not match verified Ver1.052.");
            var built=RecipeSupport.Install(clean.Root,stage,"Fresh Ver1.052",new List<LayerInput>{new LayerInput{Source=bundle}});
            installedRoot=built.Root;
            if(UniversalSupport.Identify(built.Root)!=id)throw new IOException("Output version not Ver1.052.");
            if(Store.SaveFiles(built.SavePath).Any())throw new IOException("Fresh game unexpectedly contains prior saves.");
            EnglishSupport.Apply(built);
            var english=EnglishSupport.Status(built);
            if(!english.Enabled||english.Modified||english.Pack==null||english.AppliedTexts<1)throw new IOException("English did not enable correctly: "+english.Detail);
            RecipeSupport.VerifyProfile(built);
            if(UniversalSupport.Identify(clean.Root)!="gorpg-1-23")throw new IOException("Clean GoRPG source modified.");
            var result=new {status="PASS",sourceKind=Directory.Exists(source)?"folder":"archive",baseVersion=UniversalSupport.Identify(clean.Root),targetVersion=id,baseRoot=cleanRoot,installed=installedRoot,bundleHash=creatorHash,englishEnabled=english.Enabled,englishAvailable=english.AvailableTexts,englishApplied=english.AppliedTexts,saves=Store.SaveFiles(built.SavePath).Count(),profileVerified=true,time=DateTimeOffset.Now.ToString("o")};
            File.WriteAllText(report,Store.Json.Serialize(result),new UTF8Encoding(false));
            Console.WriteLine("PASS: "+Store.Json.Serialize(result));return 0;
        }catch(Exception e){var result=new{status="FAIL",baseRoot=cleanRoot,installedRoot=installedRoot,error=e.ToString()};File.WriteAllText(report,Store.Json.Serialize(result),new UTF8Encoding(false));Console.WriteLine("FAIL: "+e);return 1;}
    }
}
