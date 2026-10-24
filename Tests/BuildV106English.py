"""Source-verified 1.06/1.061 packs using the existing reviewed pack builder."""
import argparse,importlib.util,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT));import EnglishCatalog as c
p=argparse.ArgumentParser();p.add_argument('--version',choices=['06','061'],required=True);p.add_argument('--extracted',required=True);args=p.parse_args()
spec=importlib.util.spec_from_file_location('reviewed_pack_builder',ROOT/'Tests/BuildV1052English.py');builder=importlib.util.module_from_spec(spec);spec.loader.exec_module(builder)
builder.ID='three-in-one-3-3-5-v1-'+args.version;builder.PRIOR='three-in-one-3-3-5-v1-'+('053' if args.version=='06' else '06')
sys.argv=[sys.argv[0],'--extracted',args.extracted,'--approved',str(ROOT/'English/v106-reviewed-overrides.json')];builder.main()
folder=ROOT/'English'/builder.ID;catalog=c.load(folder/'catalog.json');c.dump(folder/'catalog.json',{key:{'source':key,**{k:v for k,v in row.items() if k!='source'}} for key,row in catalog.items()})
m=c.load(folder/'manifest.json');m['Name']='Three-in-one 3.3.5 Ver1.'+args.version+' English text overlay';m['CatalogHash']=c.sha((folder/'catalog.json').read_bytes());c.dump(folder/'manifest.json',m)
