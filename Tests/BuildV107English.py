"""Build Ver1.07 display copies from exact native fields and reviewed changes."""
from pathlib import Path
import argparse,importlib.util,sys,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--extracted',required=True);args=p.parse_args()
spec=importlib.util.spec_from_file_location('reviewed_pack_builder',ROOT/'Tests/BuildV1052English.py');builder=importlib.util.module_from_spec(spec);spec.loader.exec_module(builder)
builder.ID='three-in-one-3-3-5-v1-07';builder.PRIOR='three-in-one-3-3-5-v1-061'
sys.argv=[sys.argv[0],'--extracted',args.extracted,'--approved',str(ROOT/'English/v107-reviewed-overrides.json')];builder.main()
folder=ROOT/'English'/builder.ID;m=folder/'manifest.json';d=json.loads(m.read_text('utf8'));dictionary=json.loads((folder/'dictionary.json').read_text('utf8'));catalog=json.loads((folder/'catalog.json').read_text('utf8'))
for native,english in [('伤痕:开','Scars: On'),('伤痕:关','Scars: Off')]:
    dictionary[native]=english;catalog[native]={'source':native,'kinds':['native-rendered-choice-alias'],'contexts':['MPP_ChoiceEX strips native if(ConfigManager.noScar) guard from the verified 1.07 scar choices']}
catalog={key:{'source':key,**{field:value for field,value in row.items() if field!='source'}} for key,row in catalog.items()}
for name,data in [('dictionary.json',dictionary),('catalog.json',catalog)]:
    (folder/name).write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')),encoding='utf8')
d.update(Name='Three-in-one 3.3.5 Ver1.07 English text overlay',TotalTexts=len(catalog),TranslatedTexts=len(dictionary),ReviewedTexts=d['ReviewedTexts']+2,DictionaryHash=hashlib.sha256((folder/'dictionary.json').read_bytes()).hexdigest(),CatalogHash=hashlib.sha256((folder/'catalog.json').read_bytes()).hexdigest())
m.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':')),encoding='utf8')
