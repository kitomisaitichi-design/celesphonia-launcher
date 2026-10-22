"""Render native CE806 against the installed or candidate display hook; games are read-only."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess
p=argparse.ArgumentParser()
for name in ('game','output','runtime'):p.add_argument('--'+name,type=Path,required=True)
p.add_argument('--fixed',action='store_true');args=p.parse_args()
root=args.game.resolve();manifest=json.loads((root/'package.json').read_text('utf-8-sig'))
entry=root/manifest['main'];web=entry.parent
sources=[entry,web/'data/CommonEvents.json',web/'js/plugins/DTextPicture.js']+list((root/'save').glob('*.rpgsave'))
before={f:hashlib.sha256(f.read_bytes()).hexdigest() for f in sources}
args.output.parent.mkdir(parents=True,exist_ok=True)
engine=next(root/n for n in ('nw.exe','Game.exe') if (root/n).exists())
app=args.output.parent/('engine-'+engine.stem)/'app';app.mkdir(parents=True,exist_ok=True)
for f in root.iterdir():
    if f.is_file() and (f.name==engine.name or f.suffix.lower() in ('.dll','.pak') or f.name in ('icudtl.dat','snapshot_blob.bin','natives_blob.bin','v8_context_snapshot.bin')):
        target=app/f.name
        if not target.exists():shutil.copy2(f,target)
for folder in ('locales','swiftshader'):
    if (root/folder).exists():shutil.copytree(root/folder,app/folder,dirs_exist_ok=True)
html=entry.read_text('utf-8-sig')
html=re.sub(r'<head\b[^>]*>',lambda m:m[0]+'\n<base href="'+web.as_uri()+'/">',html,count=1,flags=re.I)
html=re.sub(r'celesphonia-english/runtime\.js',lambda m:args.runtime.resolve().as_uri(),html)
source=(Path(__file__).parent/'NativeResistanceLayoutFixture.js').read_text('utf8').replace('__OUTPUT__',json.dumps(str(args.output.resolve()))).replace('__FIXED__',str(args.fixed).lower())
(app/'resistance-probe.js').write_text(source,encoding='utf8')
main=re.search(r'<script\b[^>]*src=["\']js/main\.js["\'][^>]*>',html,re.I);assert main
html=html[:main.start()]+'<script src="'+(app/'resistance-probe.js').as_uri()+'"></script>\n'+html[main.start():]
(app/'index.html').write_text(html,encoding='utf8')
manifest.update(name='resistance-layout-check',main='index.html');manifest['window']=dict(manifest.get('window',{}),show=False)
for key in ('node-main','inject_js_start','inject_js_end','inject-js-start','inject-js-end'):manifest.pop(key,None)
(app/'package.json').write_text(json.dumps(manifest),encoding='utf8')
process=subprocess.Popen([str(app/engine.name),'--user-data-dir='+str(app.parent/'browser'),'--nwapp='+str(app)],cwd=app,creationflags=subprocess.CREATE_NO_WINDOW)
try:process.wait(timeout=40)
except subprocess.TimeoutExpired:process.terminate();raise
report=json.loads(args.output.read_text('utf8'));report['sourceNativeHashesUnchanged']=all(hashlib.sha256(f.read_bytes()).hexdigest()==h for f,h in before.items())
report['runtimeHash']=hashlib.sha256(args.runtime.read_bytes()).hexdigest()
args.output.write_text(json.dumps(report,indent=2),encoding='utf8')
assert report['status']=='PASS' and report['sourceNativeHashesUnchanged'],report.get('error')
print(json.dumps({'status':report['status'],'checks':len(report['checks']),'cases':len(report['cases']),'sourceFilesUnchanged':len(before)}))
