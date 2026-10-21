"""Exercise the real native message handoff; source games and saves are read-only."""
from pathlib import Path
import argparse, hashlib, json, re, shutil, subprocess, tempfile

parser = argparse.ArgumentParser()
for name in ('launcher', 'game', 'save', 'output'):
    parser.add_argument('--' + name, type=Path, required=True)
parser.add_argument('--mode', choices=('baseline', 'normal', 'controls'), required=True)
args = parser.parse_args()
root = args.game.resolve()
manifest = json.loads((root / 'package.json').read_text('utf-8-sig'))
entry = root / manifest['main']; web = entry.parent
before = {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in [entry, args.save, web / 'data/CommonEvents.json']}
args.output.parent.mkdir(parents=True, exist_ok=True)
engine = next(root / n for n in ('nw.exe', 'Game.exe') if (root / n).exists())
if args.mode == 'controls':
    route = args.output.with_suffix('.app-path.txt')
    subprocess.run([str(args.launcher), '--prepare-control-session', str(root), str(route)], check=True)
    app = Path(route.read_text('utf-8-sig'))
    html = (app / 'index.html').read_text('utf-8-sig')
else:
    app = args.output.parent / ('native-stream-engine-' + engine.name.replace('.exe', '')) / 'app'
    app.mkdir(parents=True, exist_ok=True)
    for file in root.iterdir():
        if file.is_file() and (file.name == engine.name or file.suffix.lower() in ('.dll', '.pak') or file.name in ('icudtl.dat', 'snapshot_blob.bin', 'natives_blob.bin', 'v8_context_snapshot.bin')):
            target = app / file.name
            if not target.exists() or target.stat().st_size != file.stat().st_size or target.stat().st_mtime_ns != file.stat().st_mtime_ns:
                shutil.copy2(file, target)
    for folder in ('locales', 'swiftshader'):
        if (root / folder).exists(): shutil.copytree(root / folder, app / folder, dirs_exist_ok=True)
    html = entry.read_text('utf-8-sig')
    html = re.sub(r'<head\b[^>]*>', lambda m: m[0] + '\n<base href="' + web.as_uri() + '/">', html, count=1, flags=re.I)
    if args.mode == 'baseline':
        html = re.sub(r'<script\b[^>]*src=["\']celesphonia-english/stream-repeat-rescue\.js["\'][^>]*>\s*</script>', '', html, flags=re.I)
    manifest['main'] = 'index.html'; manifest['name'] = 'native-stream-handoff-' + args.mode
    manifest['window'] = dict(manifest.get('window', {}), show=False)
    for key in ('node-main', 'inject_js_start', 'inject_js_end', 'inject-js-start', 'inject-js-end'): manifest.pop(key, None)
    (app / 'package.json').write_text(json.dumps(manifest), encoding='utf8')
source = (Path(__file__).parent / 'NativeStreamChoiceRescueFixture.js').read_text('utf8')
for key, value in {'__OUTPUT__': str(args.output.resolve()), '__SAVE__': str(args.save.resolve()), '__WEB__': str(web), '__REPAIR__': args.mode != 'baseline', '__CONTROLS__': args.mode == 'controls'}.items():
    source = source.replace(key, json.dumps(value))
(app / 'native-stream-probe.js').write_text(source, encoding='utf8')
main = re.search(r'<script\b[^>]*src=["\']js/main\.js["\'][^>]*>', html, re.I)
assert main, 'Native entry script missing'
html = html[:main.start()] + '<script src="' + (app / 'native-stream-probe.js').as_uri() + '"></script>\n' + html[main.start():]
(app / 'index.html').write_text(html, encoding='utf8')
process = subprocess.Popen([str(app / engine.name), '--user-data-dir=' + str(app.parent / ('browser-' + args.mode)), '--nwapp=' + str(app)], cwd=app, creationflags=subprocess.CREATE_NO_WINDOW)
try: process.wait(timeout=40)
except subprocess.TimeoutExpired:
    process.terminate(); raise
assert args.output.exists(), 'Native fixture returned no report'
report = json.loads(args.output.read_text('utf8'))
report['sourceNativeHashesUnchanged'] = all(hashlib.sha256(p.read_bytes()).hexdigest() == value for p, value in before.items())
report['mode'] = args.mode
args.output.write_text(json.dumps(report, indent=2), encoding='utf8')
assert report['status'] == 'PASS' and report['sourceNativeHashesUnchanged'], report
print(json.dumps({'status': report['status'], 'mode': args.mode, 'checks': len(report['checks']), 'menu': report['menu']}))
