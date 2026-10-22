"""Focused native-engine panel checks; no real saves are seeded or loaded."""
import argparse, hashlib, json, os, pathlib, shutil, subprocess, time

p = argparse.ArgumentParser()
p.add_argument('--launcher', required=True, type=pathlib.Path)
p.add_argument('--root', required=True, type=pathlib.Path)
p.add_argument('--helper', required=True, type=pathlib.Path)
p.add_argument('--report', required=True, type=pathlib.Path)
p.add_argument('--review', action='store_true')
a = p.parse_args()
launcher, root, report = a.launcher.resolve(), a.root.resolve(), a.report.resolve()
home = launcher.parent
assert (home / 'launcher.exe').is_file() and 'work' in home.parts
config = json.loads((root / 'package.json').read_text('utf-8-sig'))
web = (root / config['main']).parent
tracked = [root / 'package.json', root / config['main']]
tracked += [f for folder in (web/'data', web/'js', web/'celesphonia-english') if folder.exists() for f in folder.rglob('*') if f.is_file()]
sha = lambda f: hashlib.sha256(f.read_bytes()).hexdigest()
before = {f: sha(f) for f in tracked}
exe = 'Game.exe' if (root/'Game.exe').is_file() else 'nw.exe'
marker = home/'panel-path.txt'
prefs = home/'Control Sessions/panel-native-fixture/panel-settings.json'
if prefs.exists(): prefs.rename(home/('qa-panel-settings-'+str(time.time_ns())+'.json'))
subprocess.run([str(a.helper.resolve()), str(launcher), str(root), exe, str(marker)], check=True)
app = pathlib.Path(marker.read_text('utf-8-sig'))
script = (pathlib.Path(__file__).parent/'RuntimeVitalsFixture.js').read_text('utf-8')
script = script.replace('__OUTPUT__', json.dumps(str(report))).replace('__REVIEW__', 'true' if a.review else 'false')
with (app/'controls.js').open('a', encoding='utf-8') as f: f.write('\n'+script)
proc = subprocess.Popen([str(app/exe), '--user-data-dir='+str(home/'native-ui-browser'), '--nwapp='+str(app)], cwd=app, creationflags=subprocess.CREATE_NO_WINDOW)
deadline = time.monotonic()+(240 if a.review else 65)
while proc.poll() is None and time.monotonic()<deadline:
    if a.review and pathlib.Path(str(report)+'.ready').exists():
        print('READY: actual native panel; Ctrl+Alt+Q completes visual review.', flush=True)
        a.review=False
    time.sleep(.3)
assert proc.poll() is not None, 'Native fixture did not exit; preserve it for inspection.'
r = json.loads(report.read_text('utf-8-sig'))
assert r['status']=='PASS', r
assert all(sha(f)==h for f,h in before.items()), 'Native source file changed'
r['sourceNativeHashesUnchanged']=True
r['sourceFilesChecked']=len(before)
r['launcherHash']=sha(launcher)
report.write_text(json.dumps(r, indent=2), encoding='utf-8')
print(json.dumps({'status':'PASS','checks':len(r['checks']),'sourceFilesUnchanged':len(before)}))
