"""Embed exact native inventories, sharing identical GoRPG file records."""
from pathlib import Path
import gzip
import json

root = Path(__file__).resolve().parents[1]
out = root / 'Staging/native-reference-resources'
out.mkdir(parents=True, exist_ok=True)
base = json.loads((root / 'NativeReferenceFiles.json').read_bytes())
assert base['Format'] == 1 and base['VersionId'] == 'gorpg-1-23'
base_files = {row['Path']: row for row in base['Files']}
assert len(base_files) == len(base['Files'])
saved = 0
count = 0
for source in sorted(root.glob('NativeReferenceFiles*.json')):
    data = source.read_bytes()
    catalog = json.loads(data)
    assert catalog['Format'] == 1 and catalog['Files']
    target = {row['Path']: row for row in catalog['Files']}
    assert len(target) == len(catalog['Files'])
    payload = data
    if catalog['VersionId'] != base['VersionId']:
        delta = {
            'Format': 2, 'VersionId': catalog['VersionId'], 'BaseVersionId': base['VersionId'],
            'RemovedPaths': sorted(base_files.keys() - target.keys()),
            'Files': [row for key, row in target.items() if base_files.get(key) != row],
        }
        recovered = dict(base_files)
        for key in delta['RemovedPaths']:
            del recovered[key]
        recovered.update({row['Path']: row for row in delta['Files']})
        assert recovered == target, source
        payload = json.dumps(delta, ensure_ascii=False, separators=(',', ':')).encode('utf-8')
    packed = gzip.compress(payload, compresslevel=9, mtime=0)
    assert gzip.decompress(packed) == payload
    destination = out / (source.name + '.gz')
    if not destination.exists() or destination.read_bytes() != packed:
        destination.write_bytes(packed)
    saved += len(data) - len(packed)
    count += 1
print('Verified exact embedded native inventories:', count, 'files;', saved, 'bytes saved')
