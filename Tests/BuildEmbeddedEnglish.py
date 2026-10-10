"""Deterministic, complete English release snapshot compiled into the launcher."""
import pathlib,json,zipfile,hashlib
HOME=pathlib.Path(__file__).resolve().parents[1]
sha=lambda b:hashlib.sha256(b).hexdigest()
ids=('gorpg-1-23','maple-1-7-2','three-in-one-3-3-4','three-in-one-3-3-5-trial','three-in-one-3-3-5-v1-01')
import sys
sys.path.insert(0,str(HOME));import EnglishCatalog as c
ids=c.IDS
payload={'EnglishRuntime.js':(HOME/'EnglishRuntime.js').read_bytes()}
for ident in ids:
    m=json.loads((HOME/'English'/ident/'manifest.json').read_text('utf8'))
    assert m['VersionId']==ident and m['Format']==1 and m['Mode']=='display-only'
    for name in ('manifest.json','dictionary.json','catalog.json'):
        payload['English/'+ident+'/'+name]=(HOME/'English'/ident/name).read_bytes()
    for name,key in [('dictionary.json','DictionaryHash'),('catalog.json','CatalogHash')]:
        assert sha(payload['English/'+ident+'/'+name])==m[key],(ident,name)
report={'format':1,'files':{name:sha(b) for name,b in sorted(payload.items())}}
payload['release.json']=json.dumps(report,sort_keys=True,separators=(',',':')).encode()
dest=HOME/'Tests/embedded-english-release.zip'
with zipfile.ZipFile(dest,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for name,b in sorted(payload.items()):
        info=zipfile.ZipInfo(name,(2000,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
        z.writestr(info,b,compresslevel=9)
print('Verified embedded English snapshot:',len(report['files']),'files;',dest.stat().st_size,'bytes')
