from pathlib import Path
import argparse,json,gzip,base64,hashlib,re,sys
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--source',type=Path,default=ROOT/'Localization');p.add_argument('--partial',action='store_true');p.add_argument('--output',type=Path,default=ROOT/'mod-recipes.json');a=p.parse_args()
codes=['en','zh','es','fr','de','ja','ko','pt','ru','ar','hi','bn','id','it','tr','vi'];packs={};canonical=None;total=0
for code in codes:
 data=json.loads((a.source/(code+'.json')).read_text(encoding='utf8'));strings=data['Strings']
 if code=='en':canonical=set(strings)
 if not a.partial:assert set(strings)==canonical,(code,'incomplete locale keys',len(canonical-set(strings)))
 assert all(isinstance(v,str) and v.strip() for v in strings.values()),'Empty translation'
 for source,target in strings.items():
  for pattern in [r'\{\d+(?:[^{}]*)\}',r'https?://[^\s)<>]+',r'`[^`\r\n]+`']:
   assert sorted(re.findall(pattern,source))==sorted(re.findall(pattern,target)),(code,'protected syntax',source[:80])
 raw=json.dumps(data,ensure_ascii=False,separators=(',',':'),sort_keys=True).encode('utf8');packs[code]={'Sha256':hashlib.sha256(raw).hexdigest(),'Data':base64.b64encode(gzip.compress(raw,9,mtime=0)).decode('ascii'),'Keys':len(strings)};total+=len(strings)
doc=json.loads(a.output.read_text(encoding='utf-8-sig'))
doc['LauncherLocalization']={'Format':1,'SourceHash':packs['en']['Sha256'],'PreparedWith':'PLaMo 3 Translate 0.8B; reviewed navigation/action labels; community corrections welcome','Complete':not a.partial,'Packs':packs}
envelope=doc.pop('LauncherLocalization');core=json.dumps(doc,ensure_ascii=False,separators=(',',':'));a.output.write_text(core[:-1]+',\n  \"LauncherLocalization\":'+json.dumps(envelope,ensure_ascii=False,separators=(',',':'))+'\n}\n',encoding='utf8')
print(json.dumps({'status':'PREVIEW' if a.partial else 'PASS','languages':len(packs),'keys':len(canonical),'localizedEntries':total,'dataBytes':a.output.stat().st_size,'legacyUpdaterPayload':'existing mod-recipes.json; no added ZIP entries'}))
