"""Creator-source-exact 1.053 pack, using the existing reviewed pack builder."""
import importlib.util,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT));import EnglishCatalog as c
spec=importlib.util.spec_from_file_location('previous_pack_builder',ROOT/'Tests/BuildV1052English.py')
builder=importlib.util.module_from_spec(spec);spec.loader.exec_module(builder)
builder.ID='three-in-one-3-3-5-v1-053';builder.PRIOR='three-in-one-3-3-5-v1-052'
builder.main()
folder=ROOT/'English'/builder.ID
# Historical packs are stored without redundant source fields. Expand the new
# pack once so the existing lossless compactor can establish its own baseline.
catalog=c.load(folder/'catalog.json')
catalog={key:{'source':key,**{k:v for k,v in row.items() if k!='source'}} for key,row in catalog.items()}
c.dump(folder/'catalog.json',catalog)
p=folder/'manifest.json';m=c.load(p);m['Name']='Three-in-one 3.3.5 Ver1.053 English text overlay';m['CatalogHash']=c.sha((folder/'catalog.json').read_bytes());c.dump(p,m)
