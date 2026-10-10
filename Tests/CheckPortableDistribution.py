"""Verify customer package contents and retain authoring evidence separately."""
import pathlib,json,zipfile,hashlib
HOME=pathlib.Path(__file__).resolve().parents[1];ACTIVE=HOME.parent/'Celesphonia-Delta-Launcher';ZIP=HOME.parent/'Celesphonia-Delta-Launcher.zip'
ids=('gorpg-1-23','maple-1-7-2','three-in-one-3-3-4','three-in-one-3-3-5-trial','three-in-one-3-3-5-v1-01')
import sys
sys.path.insert(0,str(HOME));import EnglishCatalog as c
ids=c.IDS
expected={'Celesphonia Portable Switcher.exe','EnglishRuntime.js','README.txt','mod-recipes.json','version-fingerprints.json'}|{'English/'+ident+'/'+name for ident in ids for name in ('manifest.json','dictionary.json','catalog.json')}
with zipfile.ZipFile(ZIP) as z:
 assert set(z.namelist())==expected and z.testzip() is None
 for name in expected:assert (HOME/name).read_bytes()==z.read(name)==(ACTIVE/name).read_bytes(),name
deployment=json.loads((HOME/'portable-deployment-check.json').read_text('utf8'));assert not deployment['modifiedOldFilesRetained'],deployment['modifiedOldFilesRetained']
obsolete=json.loads((HOME/'Tests/portable-previous-distribution.json').read_text('utf8'))['files']
obsolete=set(obsolete)-expected
for name in obsolete:
 assert not (ACTIVE/name).exists(),name
 original=HOME/pathlib.PurePosixPath(name).name if name.startswith('Source/') else HOME/name
 assert original.is_file(),'Authoring evidence was lost: '+name
assert (HOME/'English/TRANSLATION WORKFLOW.md').exists() and not (ACTIVE/'English/TRANSLATION WORKFLOW.md').exists()
assert not (ACTIVE/'Source').exists() and not (ACTIVE/'Tests').exists()
report={'status':'PASS','runtimeFiles':len(expected),'publicDocuments':['README.txt'],'obsoleteDeveloperFilesAbsent':len(obsolete),'authoringEvidenceRetained':True,'sourceTestsWorkflowReportsExcluded':True,'canonicalArchiveUpdatedInPlace':True}
(HOME/'portable-distribution-check.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report,indent=2))
