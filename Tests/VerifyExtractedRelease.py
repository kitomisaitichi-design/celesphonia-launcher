"""Real extracted EXE tests, all versions; private synthetic games, no user saves."""
import pathlib,sys,json,zipfile,subprocess,tempfile,shutil,hashlib,re,argparse
HOME=pathlib.Path(__file__).resolve().parents[1];sys.path.insert(0,str(HOME));import EnglishCatalog as c
def run(exe,*args):subprocess.run([str(exe),*map(str,args)],cwd=str(HOME.parent),check=True,timeout=90)
def remove_private_tree(root):
    assert root.resolve().parent== (HOME/'Tests').resolve() and root.name.startswith('extraction-fixture-')
    assert not root.is_symlink() and not root.is_junction()
    for p in root.rglob('*'):assert not p.is_symlink() and not p.is_junction(),p
    shutil.rmtree(root)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--archive',type=pathlib.Path,default=HOME.parent/'Celesphonia-Delta-Launcher.zip');parser.add_argument('--published-reference',action='store_true');parser.add_argument('--version',action='append',choices=c.IDS);args=parser.parse_args()
    selected=args.version or c.IDS
    rows=[];fixture=pathlib.Path(tempfile.mkdtemp(prefix='extraction-fixture-',dir=HOME/'Tests'))
    portable=fixture/'existing portable 原始文件夹';portable.mkdir();exe=portable/'Celesphonia Portable Switcher.exe'
    original_config=b'{"Profiles":[],"SelectedId":null,"InstallRoot":"private fixture","SearchRoots":null}'
    (portable/'profiles.json').write_bytes(original_config);(portable/'private-user-note.txt').write_bytes(b'keep this unrelated file')
    try:
        with zipfile.ZipFile(args.archive) as z:
            assert z.testzip() is None
            changelog=z.read('README.txt').decode('utf8')
            assert all('v1.0'+str(i) in changelog for i in range(1,5)), 'The English creator changelog is missing a version.'
            assert 'GAME MOD CHANGELOG.md' not in z.namelist(), 'Extra ZIP entry would break older auto-updaters.'
            for name in z.namelist():
                p=portable/pathlib.PurePosixPath(name);p.resolve().relative_to(portable.resolve());p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(z.read(name))
            assert (portable/'profiles.json').read_bytes()==original_config
            for name in z.namelist():assert (portable/name).read_bytes()==z.read(name)
            assert not (portable/'launcher-update.json').exists(), 'Portable distribution leaked private updater preferences.'
            extracted_changelog=portable/'GAME MOD CHANGELOG.md'
            run(exe,'--export-game-changelog',extracted_changelog)
            assert extracted_changelog.read_text('utf8')==(HOME/'GAME MOD CHANGELOG.md').read_text('utf8')
            assert all('v1.0'+str(i) in extracted_changelog.read_text('utf8') for i in range(1,5))
            profiles=c.load(HOME/'profiles.json')['Profiles']
            for profile in profiles:
                if profile['Root'].startswith('@launcher/'):
                    resolved=(HOME/profile['Root'][10:]).resolve();resolved.relative_to(HOME.resolve());profile['Root']=str(resolved)
            profiles=[p for p in profiles if (pathlib.Path(p['Root'])/'package.json').is_file()]
            versions=c.load(HOME/'version-fingerprints.json')['Versions']
            for ident in selected:
                marks=next(v['Fingerprints'] for v in versions if v['Id']==ident)
                matched=[c.webroot(pathlib.Path(p['Root'])) for p in profiles if all(c.sha((c.webroot(pathlib.Path(p['Root']))/n).read_bytes())==h for n,h in marks.items())]
                # Test-only marker fixtures are intentionally NOT registered in
                # the user's active game library. Retain historical support
                # tests without cluttering or launching any private QA games.
                if not matched:
                    options=(HOME/'Tests/native-game-fixtures'/ident/'www',
                             HOME/'Tests/native-game-fixtures'/(ident+'-reference')/'www')
                    matched=[candidate for candidate in options if
                             all((candidate/n).is_file() and c.sha((candidate/n).read_bytes())==h
                                 for n,h in marks.items())]
                    assert matched, 'No source-verifiable fixture for '+ident
                source=matched[0]
                game=fixture/'synthetic game';web=game/'www';web.mkdir(parents=True)
                (game/'Game.exe').write_bytes(b'fixture sentinel: never execute')
                (game/'package.json').write_text('{"main":"www/index.html"}',encoding='utf8')
                original=b'\xef\xbb\xbf<!DOCTYPE html>\r\n<script src="js/main.js"></script>\r\n';(web/'index.html').write_bytes(original)
                (web/'save').mkdir();(web/'save/file1.rpgsave').write_bytes(b'isolated synthetic save sentinel')
                for name in marks:
                    dest=web/name;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes((source/name).read_bytes())
                folder=portable/'English'/ident;expected=json.loads(z.read('English/'+ident+'/manifest.json')) if args.published_reference else c.load(HOME/'English'/ident/'manifest.json');dictionary=json.loads(z.read('English/'+ident+'/dictionary.json')) if args.published_reference else c.load(HOME/'English'/ident/'dictionary.json')
                key=next(k for k in dictionary if c.CJK.search(k) and len(k)<15 and not c.CONTROL.search(k) and '\n' not in k)
                custom=json.dumps({key:'Private custom label'},ensure_ascii=False).encode('utf8');(folder/'user-translations.json').write_bytes(custom)
                for case in ('clean-extraction','existing-folder-overwrite','missing-dictionary','truncated-mixed-pack','coherent-stale-pack','missing-catalog-and-runtime'):
                    for name in ('manifest.json','dictionary.json','catalog.json'):(folder/name).write_bytes(z.read('English/'+ident+'/'+name))
                    (portable/'EnglishRuntime.js').write_bytes(z.read('EnglishRuntime.js'))
                    if case=='existing-folder-overwrite':
                        (folder/'dictionary.json').write_bytes(b'old truncated content')
                        for name in z.namelist():
                            if name.startswith('English/'+ident+'/'):(portable/name).write_bytes(z.read(name))
                    elif case=='missing-dictionary':(folder/'dictionary.json').unlink()
                    elif case=='truncated-mixed-pack':
                        (folder/'dictionary.json').write_bytes((folder/'dictionary.json').read_bytes()[:97]);(folder/'catalog.json').write_bytes(b'{')
                    elif case=='coherent-stale-pack':
                        (folder/'dictionary.json').write_bytes(b'{}');m=dict(expected);m['DictionaryHash']=c.sha(b'{}');m['TranslatedTexts']=0;c.dump(folder/'manifest.json',m)
                    elif case=='missing-catalog-and-runtime':(folder/'catalog.json').unlink();(portable/'EnglishRuntime.js').unlink()
                    run(exe,'--english-apply',game)
                    statusfile=fixture/'status.json';run(exe,'--english-status',game,statusfile);status=c.load(statusfile)
                    assert status['Enabled'] and not status['Modified'] and not status['UpdateAvailable'],(ident,case,status)
                    assert status['Pack']['DictionaryHash']==expected['DictionaryHash'] and status['Pack']['TranslatedTexts']==expected['TranslatedTexts']
                    state=c.load(web/'celesphonia-english/state.json');assert state['DictionaryHash']==expected['DictionaryHash']
                    assert c.sha((web/'celesphonia-english/runtime.js').read_bytes())==c.sha(z.read('EnglishRuntime.js'))
                    assert 'Private custom label' in (web/'celesphonia-english/dictionary.js').read_text('utf8')
                    assert (folder/'user-translations.json').read_bytes()==custom
                    assert (portable/'profiles.json').read_bytes()==original_config and (portable/'private-user-note.txt').read_bytes()==b'keep this unrelated file'
                    assert (web/'save/file1.rpgsave').read_bytes()==b'isolated synthetic save sentinel'
                    assert all(c.sha((web/n).read_bytes())==h for n,h in marks.items())
                    run(exe,'--english-restore',game);assert (web/'index.html').read_bytes()==original
                    rows.append({'version':ident,'case':case,'status':'PASS'})
                # All game files are synthetic/copied immutable fingerprints in this private root.
                for p in sorted(game.rglob('*'),key=lambda p:len(p.parts),reverse=True):
                    assert not p.is_symlink() and not p.is_junction()
                    if p.is_file():p.unlink()
                    else:p.rmdir()
                game.rmdir();(folder/'user-translations.json').unlink()
                for name in ('manifest.json','dictionary.json','catalog.json'):(folder/name).write_bytes(z.read('English/'+ident+'/'+name))
                print(ident,'6 extraction/apply/restore cases passed',flush=True)
        report={'status':'PASS','archiveHash':c.sha(args.archive.read_bytes()),'cases':rows,'versions':len(c.IDS),'extractionCases':len(rows),'fullArchiveRoundTrip':True,'unrelatedWorkingDirectory':True,'spacesAndChinesePath':True,'userFilesAndSettingsPreserved':True,'customTranslationsPreserved':True,'originalEntryBytesRestored':True,'userGamesLaunched':0,'userSavesRead':False,'userSavesWritten':False,'fullGameplayVerified':False,'launcherHash':c.sha((HOME/'Celesphonia Portable Switcher.exe').read_bytes())}
        report['versions']=len(selected)
        report['selectedVersions']=list(selected)
        c.dump(HOME/'Tests/extracted-release-check.json',report)
    finally:remove_private_tree(fixture)
if __name__=='__main__':main()
