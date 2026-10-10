"""Rebuild the portable distribution; omit private profiles, saves and development probes."""
import argparse,hashlib,json,os,pathlib,zipfile,shutil,sys
HOME=pathlib.Path(__file__).resolve().parent
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--archive',type=pathlib.Path)
 mode=parser.add_mutually_exclusive_group();mode.add_argument('--stage-only',action='store_true');mode.add_argument('--publish-verified-stage',action='store_true')
 args=parser.parse_args();target=(args.archive or HOME.parent/'Celesphonia-Delta-Launcher.zip').resolve()
 assert target.parent==HOME.parent.resolve() and target.suffix=='.zip'
 backlog=json.loads((HOME/'English/dialogue-backlog-summary.json').read_text('utf8'))
 bankhash=hashlib.sha256((HOME/'English/reviewed-translations.json').read_bytes()).hexdigest()
 progress=json.loads((HOME/'English/current-progress.json').read_text('utf8'));coverage=json.loads((HOME/'English/all-routes-coverage-summary.json').read_text('utf8'));native=json.loads((HOME/'English/native-variants-check.json').read_text('utf8'));queue=json.loads((HOME/'English/all-routes-review-queue-index.json').read_text('utf8'))
 bankhash=hashlib.sha256((HOME/'English/reviewed-translations.json').read_bytes()).hexdigest()
 # Historical, broader translation reports may describe a different review
 # pass. Never silently present them as current release verification. Check
 # the exact current English manifests and the new EXE's embedded bundles
 # below, then perform native extracted-package round-trip tests.
 evidenceCurrent=all(
  evidence.get('reviewedTranslationsHash')==bankhash and
  all(hashlib.sha256((HOME/'English'/ident/'dictionary.json').read_bytes()).hexdigest()==digest
      for ident,digest in evidence.get('dictionaryHashes',{}).items())
  for evidence in (backlog,progress,coverage,native,queue)
 )
 if not evidenceCurrent:print('Historical broad-coverage reports are stale; current package is verified by its own manifests, embedded EXE and extracted release checks.')
 # Distribution is a runtime allowlist. Authoring reports, source, tests and
 # workflow documents stay here even when future passes create new files.
 top=['Celesphonia Portable Switcher.exe','EnglishRuntime.js','README.txt','GAME MOD CHANGELOG.md','mod-recipes.json','version-fingerprints.json']
 ids=('gorpg-1-23','maple-1-7-2','three-in-one-3-3-4','three-in-one-3-3-5-trial','three-in-one-3-3-5-v1-01')
 import EnglishCatalog as c
 ids=c.IDS
 # Translation changes must rebuild the EXE snapshot too. Refuse a mixed release.
 import subprocess
 embeddedReport=HOME/'Tests/embedded-pack-check.json'
 subprocess.run([str(HOME/top[0]),'--english-pack-report',str(embeddedReport)],check=True)
 embedded=json.loads(embeddedReport.read_text('utf8'));assert embedded['status']=='PASS'
 assert embedded['runtimeHash']==hashlib.sha256((HOME/'EnglishRuntime.js').read_bytes()).hexdigest(),'Rebuild launcher after runtime changes.'
 assert {r['version'] for r in embedded['variants']}==set(ids)
 for row in embedded['variants']:
  ident=row['version']
  assert not row['looseProblems'],'Rebuild launcher after pack changes.'
  assert row['dictionaryHash']==hashlib.sha256((HOME/'English'/ident/'dictionary.json').read_bytes()).hexdigest()
  assert row['catalogHash']==hashlib.sha256((HOME/'English'/ident/'catalog.json').read_bytes()).hexdigest()
 files=[(HOME/name,name) for name in top]
 files.extend((HOME/'English'/ident/name,'English/'+ident+'/'+name) for ident in ids for name in ('manifest.json','dictionary.json','catalog.json'))
 assert len(files)==len({name for _,name in files})==6+3*len(ids)
 assert b'Celesphonia.RuntimePanel.js' in (HOME/top[0]).read_bytes(),'Runtime controls must be embedded in the EXE.'
 ui=json.loads((HOME/'Tests/launcher-ui-review/layout-check.json').read_text('utf8'))
 assert ui['status']=='PASS' and ui['launcherHash']==hashlib.sha256((HOME/top[0]).read_bytes()).hexdigest(),'Stale launcher UI verification.'
 first=json.loads((HOME/'Tests/launcher-first-run-review/layout-check.json').read_text('utf8'))
 assert first['status']=='PASS' and first['launcherHash']==ui['launcherHash'],'Stale first-run UI verification.'
 # Retain ownership evidence for removing obsolete files from the previous
 # distribution. Never treat untracked/private files as launcher assets.
 previous=HOME/'Tests/portable-previous-distribution.json'
 if target.exists():
  with zipfile.ZipFile(target) as old:
   if set(old.namelist())!={name for _,name in files}:
    previous.write_text(json.dumps({'archive':str(target),'files':{name:hashlib.sha256(old.read(name)).hexdigest() for name in old.namelist() if not name.endswith('/')}},indent=2),encoding='utf8')
 staging=target.with_suffix('.zip.new')
 if not args.publish_verified_stage:
  assert shutil.disk_usage(target.parent).free>sum(p.stat().st_size for p,_ in files)+8*1024*1024,'Not enough free space to stage a complete release; preserve the canonical ZIP.'
  with zipfile.ZipFile(staging,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
   for p,name in files:archive.write(p,name)
 with zipfile.ZipFile(staging) as archive:
  assert archive.testzip() is None
  names=archive.namelist()
  assert not any(x in names for x in ['profiles.json','profiles.json.previous','English-build-check.exe','delta-error.txt'])
  assert set(names)=={name for _,name in files}
  for p,name in files:assert archive.read(name)==p.read_bytes(),'Staged release changed since review: '+name
  assert not any((name.endswith('.md') and name!='GAME MOD CHANGELOG.md') or name.startswith(('Source/','Tests/')) for name in names)
  for ident in ids:
   manifest=json.loads(archive.read('English/'+ident+'/manifest.json'));assert hashlib.sha256(archive.read('English/'+ident+'/dictionary.json')).hexdigest()==manifest['DictionaryHash'];assert hashlib.sha256(archive.read('English/'+ident+'/catalog.json')).hexdigest()==manifest['CatalogHash']
 if args.stage_only:
  print(json.dumps({'status':'STAGED','archive':str(staging),'canonicalArchivePreserved':True}));return
 if not args.publish_verified_stage:
  subprocess.run([sys.executable,str(HOME/'Tests/VerifyExtractedRelease.py'),'--archive',str(staging)],check=True)
 extracted=json.loads((HOME/'Tests/extracted-release-check.json').read_text('utf8'))
 assert extracted['status']=='PASS' and extracted['archiveHash']==hashlib.sha256(staging.read_bytes()).hexdigest(),'Run extraction tests on this exact stage before publishing.'
 assert extracted['launcherHash']==ui['launcherHash'],'Stale extracted launcher verification.'
 os.replace(staging,target)
 reviewed=json.loads((HOME/'English/reviewed-translations.json').read_text('utf8'))
 report={'status':'PASS','portableArchive':target.name,'bytes':target.stat().st_size,'files':len(files),'runtimeAllowlist':True,'developmentDocumentsIncluded':False,'privateProfilesAndSavesIncluded':False,'unusedTestProbesIncluded':False,'translationComplete':False,'ReviewedBatchEntries':len(reviewed),'historicalBroadCoverageReportsCurrent':evidenceCurrent,'currentEmbeddedPackVerification':'PASS','currentExtractedReleaseVerification':'PASS'}
 (HOME/'portable-package-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
