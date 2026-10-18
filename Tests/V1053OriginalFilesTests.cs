using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

// Explicit caller-owned test workspace. Original game folders/archives are read-only.
public static class V1053OriginalFilesTests {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    public static int Main(string[] args){
        try{
            string gorpg=args[0],archive=args[1],old=args[2],web=args[3],qa=Path.GetFullPath(args[4]);
            Directory.CreateDirectory(qa);string id="three-in-one-3-3-5-v1-053";
            var current=RecipeSupport.Catalog().Recipes.Single(x=>x.Id==id);
            var previous=RecipeSupport.Catalog().Recipes.Single(x=>x.Id=="three-in-one-3-3-5-v1-052");
            var native=MixedSourceRecovery.LoadEmbeddedReference(id);
            Check(native.Files.Count==11730&&current.Files.Count==5163,"complete final target inventory");
            Check(UpgradeTests.ForwardCreatorRoutes()==21,"all registered forward routes, reverse and branch boundaries");
            var detected=FileDiscovery.ProbeRar(archive,CancellationToken.None);
            Check(detected.Any(x=>x.RecipeId==id),"mislabeled creator RAR identified from signature and native fingerprints");
            var direct=MixedSourceRecovery.Probe(native,new[]{gorpg,archive},CancellationToken.None);
            Check(direct.Complete,"GoRPG plus creator 1.053 supplies every final target file without an intermediate version");
            var changed=current.Files.Where(x=>previous.Files[x.Key]!=x.Value).Select(x=>x.Key).OrderBy(x=>x).ToArray();
            Check(changed.Length==6,"six changed files are sufficient alongside exact 1.052 originals");
            string flat=Path.Combine(qa,"partial-extracted"),zipPath=Path.Combine(qa,"partial-zip-renamed.update");Directory.CreateDirectory(flat);
            var inputs=new List<string>{old,flat,zipPath};
            using(var zip=new ZipArchive(File.Create(zipPath),ZipArchiveMode.Create))for(int i=0;i<changed.Length;i++){
                string source=Path.Combine(web,changed[i].Replace('/',Path.DirectorySeparatorChar));
                if(i<3){var e=zip.CreateEntry("wrapper/"+Path.GetFileName(source));using(var o=e.Open())using(var s=File.OpenRead(source))s.CopyTo(o);}
                else{string target=Path.Combine(flat,Path.GetFileName(source));File.Copy(source,target,true);if(i==changed.Length-1)inputs.Add(target);}
            }
            var partial=MixedSourceRecovery.Probe(native,inputs,CancellationToken.None);
            Check(partial.Complete&&partial.Selections.Any(x=>x.Kind=="zip"),"partial renamed ZIP plus flat extracted files and an installed base reconstruct the exact target");
            string onlyFiles=Path.Combine(qa,"individuals");Directory.CreateDirectory(onlyFiles);
            var individual=new List<string>{old};for(int i=0;i<changed.Length;i++){
                string f=Path.Combine(onlyFiles,"original-"+i+".fragment");File.Copy(Path.Combine(web,changed[i].Replace('/',Path.DirectorySeparatorChar)),f,true);individual.Add(f);
            }
            Check(MixedSourceRecovery.Probe(native,individual,CancellationToken.None).Complete,"six individual files work without their original filenames or folder layout");
            var missing=MixedSourceRecovery.Probe(native,individual.Take(individual.Count-1),CancellationToken.None);
            Check(!missing.Complete&&missing.Missing.Count==1,"missing file reported precisely instead of accepting a mixed version");
            string forbidden=Path.Combine(qa,"must-not-install");bool refused=false;
            try{UpgradeSupport.InstallFromOriginalFiles(individual.Take(individual.Count-1),forbidden,id,null,CancellationToken.None);}catch(IOException){refused=true;}
            Check(refused&&!Directory.Exists(forbidden),"incomplete final target fails before creating installation output");
            foreach(string branch in new[]{"gorpg-1-23","ch-current","maple-1-7-2","three-in-one-3-3-4"})
                Check(!UpgradeSupport.CanTransfer(branch,id),"cross-branch save isolation: "+branch);
            var game=UpgradeSupport.InstallFromOriginalFiles(new[]{gorpg,archive},Path.Combine(qa,"installed"),id,null,CancellationToken.None);
            Check(UniversalSupport.Identify(game.Root)==id,"real independent direct GoRPG installation identifies as Ver1.053");
            Check(Store.SaveFiles(game.SavePath).Length==0,"fresh install does not copy unrelated branch saves");
            File.WriteAllText(Path.Combine(game.Root,".celesphonia-native-probe"),"Owned Ver1.053 QA only");
            RecipeSupport.VerifyProfile(game,false,true);
            Check(File.Exists(Path.Combine(game.Root,"launcher-reconstruction.json")),"complete verified reconstruction ledger retained for launch, repair and managed uninstall");
            Check(Store.Hash(archive)==current.ArchiveHash,"creator archive preserved byte-for-byte");
            File.WriteAllText(Path.Combine(qa,"result.json"),Store.Json.Serialize(new{status="PASS",checks=checks,gameRoot=game.Root,target=id,directGoRpg=true,partialOriginalFiles=true,userSavesWritten=false}));
            return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
