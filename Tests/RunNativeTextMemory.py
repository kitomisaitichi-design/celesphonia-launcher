"""Read-only native game renderer reproduction; private engine and empty saves."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess
p=argparse.ArgumentParser()
for name in ('game','output','runtime'):p.add_argument('--'+name,type=Path,required=True)
p.add_argument('--fixed',action='store_true');args=p.parse_args()
args.output=args.output.resolve()
root=args.game.resolve();manifest=json.loads((root/'package.json').read_text('utf-8-sig'));entry=root/manifest['main'];web=entry.parent
sources=[entry,root/'package.json',web/'data/CommonEvents.json',web/'data/Skills.json',web/'js/plugins/DTextPicture.js']
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest();before={f:sha(f) for f in sources}
args.output.parent.mkdir(parents=True,exist_ok=True)
engine=next(root/n for n in ('nw.exe','Game.exe') if (root/n).exists());app=args.output.parent/'engine/app';app.mkdir(parents=True,exist_ok=True)
for f in root.iterdir():
    if f.is_file() and (f.name==engine.name or f.suffix.lower() in ('.dll','.pak') or f.name in ('icudtl.dat','snapshot_blob.bin','natives_blob.bin','v8_context_snapshot.bin')):
        target=app/f.name
        if not target.exists():shutil.copy2(f,target)
for name in ('locales','swiftshader'):
    if (root/name).exists():shutil.copytree(root/name,app/name,dirs_exist_ok=True)
html=entry.read_text('utf-8-sig');assert 'celesphonia-english/runtime.js' in html
html=re.sub(r'<head\b[^>]*>',lambda m:m[0]+'\n<base href="'+web.as_uri()+'/">',html,count=1,flags=re.I)
html=html.replace('celesphonia-english/runtime.js',args.runtime.resolve().as_uri())
source=(Path(__file__).parent/'NativeTextMemoryFixture.js').read_text('utf8').replace('__OUTPUT__',json.dumps(str(args.output.resolve()))).replace('__FIXED__',str(args.fixed).lower())
(app/'text-memory-probe.js').write_text(source,encoding='utf8')
main=re.search(r'<script\b[^>]*src=["\']js/main\.js["\'][^>]*>',html,re.I);assert main
html=html[:main.start()]+'<script src="'+(app/'text-memory-probe.js').as_uri()+'"></script>\n'+html[main.start():]
(app/'index.html').write_text(html,encoding='utf8')
manifest.update(name='native-text-memory-check',main='index.html');manifest['window']=dict(manifest.get('window',{}),show=False)
for key in ('node-main','inject_js_start','inject_js_end','inject-js-start','inject-js-end'):manifest.pop(key,None)
(app/'package.json').write_text(json.dumps(manifest),encoding='utf8')
process=subprocess.Popen([str(app/engine.name),'--user-data-dir='+str(app.parent/'browser'),'--nwapp='+str(app)],cwd=app,creationflags=subprocess.CREATE_NO_WINDOW)
try:process.wait(timeout=55)
except subprocess.TimeoutExpired:process.terminate();raise
report=json.loads(args.output.read_text('utf8'));report['sourceNativeHashesUnchanged']=all(sha(f)==h for f,h in before.items());report['runtimeHash']=sha(args.runtime)
args.output.write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf8')
assert report['status']=='PASS' and report['sourceNativeHashesUnchanged'],report.get('error')
print(json.dumps({'status':report['status'],'checks':len(report['checks']),'names':report['names'],'columns':report['columns'],'locked':report['locked']},ensure_ascii=True))
