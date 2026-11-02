"""Keep embedded notes byte-exact while preserving the legacy EXE size limit."""
from pathlib import Path
import gzip,hashlib,json
root=Path(__file__).resolve().parent.parent;out=root/'Staging/note-resources';out.mkdir(parents=True,exist_ok=True)
rows=[]
for name in ['README.md','LAUNCHER CHANGELOG.md']:
 data=(root/name).read_bytes();packed=gzip.compress(data,compresslevel=9,mtime=0)
 assert gzip.decompress(packed)==data
 (out/(name+'.gz')).write_bytes(packed)
 rows.append({'name':name,'sourceBytes':len(data),'packedBytes':len(packed),'sourceSha256':hashlib.sha256(data).hexdigest(),'lossless':True})
print(json.dumps({'status':'PASS','notes':rows,'bytesSaved':sum(x['sourceBytes']-x['packedBytes'] for x in rows)}))
