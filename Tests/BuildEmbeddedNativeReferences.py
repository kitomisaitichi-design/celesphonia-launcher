"""Losslessly compress hash-only native inventories for the portable EXE."""
from pathlib import Path
import gzip,json,hashlib
root=Path(__file__).resolve().parents[1]
out=root/'Staging/native-reference-resources';out.mkdir(parents=True,exist_ok=True)
saved=0;count=0
for source in sorted(root.glob('NativeReferenceFiles*.json')):
 data=source.read_bytes();catalog=json.loads(data);assert catalog['Format']==1 and catalog['Files']
 packed=gzip.compress(data,compresslevel=9,mtime=0);assert gzip.decompress(packed)==data
 destination=out/(source.name+'.gz')
 if not destination.exists() or destination.read_bytes()!=packed:destination.write_bytes(packed)
 assert hashlib.sha256(gzip.decompress(destination.read_bytes())).digest()==hashlib.sha256(data).digest()
 saved+=len(data)-len(packed);count+=1
print('Verified lossless embedded native inventories:',count,'files;',saved,'bytes saved')
