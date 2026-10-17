"""Build a byte-hash catalog from a verified clean GoRPG installation.

This is an authoring-only operation. It never copies or edits game files.
The catalog contains paths, sizes and hashes, NOT the game itself.
"""
import argparse
import hashlib
import json
import pathlib
import sys

ROOT_FILES = {".exe",".dll",".json",".pak",".bin",".dat",".html",".ico",".ini"}
GAME_FOLDERS = {"www","locales","swiftshader","audio","data","fonts","img","js","movies","css","icon"}
SKIP_FOLDERS = {"save","output","decrypted","celesphonia-english"}
SKIP_FILES = {"manualtransfile.json","debug.log","launcher-base.json","launcher-build.json","launcher-reconstruction.json","launcher-upgrade.json"}
REQUIRED = ("package.json","data/System.json","js/plugins.js","js/rpg_managers.js")


def file_sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        while block:=f.read(2*1024*1024):
            h.update(block)
    return h.hexdigest()

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("reference",type=pathlib.Path,help="An intact, verified GoRPG 1.23 game directory")
    parser.add_argument("output",type=pathlib.Path)
    parser.add_argument("--version",default="gorpg-1-23")
    args=parser.parse_args()
    root=args.reference.resolve(strict=True)
    if not (root/"package.json").is_file() or not ((root/"Game.exe").is_file() or (root/"nw.exe").is_file()):
        raise SystemExit("Not a standalone game root")
    web=root/"www" if (root/"www").is_dir() else root
    paths=[]
    for p in root.iterdir():
        if p.is_file() and (p.suffix.lower() in ROOT_FILES or p.name=="steam_appid.txt") and p.name.lower() not in SKIP_FILES and not p.name.lower().startswith("trsdata"):
            paths.append(p)
        if p.is_dir() and p.name.lower() in GAME_FOLDERS:
            for f in p.rglob("*"):
                if f.is_file() and not SKIP_FOLDERS.intersection(x.lower() for x in f.relative_to(root).parts) and f.suffix.lower()!=".docx" and f.name.lower() not in SKIP_FILES and not f.name.lower().startswith("trsdata"):
                    paths.append(f)
    originals=web/"celesphonia-english"/"original-entry.bin"
    from_index=web/"index.html"
    assert from_index.is_file()
    if originals.is_file():
        # Check an English sidecar exists rather than treating a random backup as a reference.
        assert (web/"celesphonia-english"/"state.json").is_file(), "Unverified English backup"
    files=[]
    for path in sorted(set(paths),key=lambda p:p.relative_to(root).as_posix().lower()):
        rel=path.relative_to(root).as_posix()
        actual=originals if path==from_index and originals.is_file() else path
        if path.is_symlink() or actual.is_symlink():
            raise SystemExit("Linked file refused: "+rel)
        files.append({"Path":rel,"Size":actual.stat().st_size,"Hash":file_sha256(actual)})
    indexed={x["Path"]:x for x in files}
    for rel in REQUIRED:
        if rel not in indexed and "www/"+rel not in indexed:
            raise SystemExit("Missing necessary GoRPG file: "+rel)
    if "Game.exe" not in indexed and "nw.exe" not in indexed:
        raise SystemExit("Missing native launcher")
    result={"Format":1,"VersionId":args.version,"Files":files}
    args.output.write_text(json.dumps(result,ensure_ascii=False,separators=(",",":"))+"\n",encoding="utf-8")
    print(json.dumps({"status":"PASS","version":result["VersionId"],"files":len(files),"totalBytes":sum(f["Size"] for f in files),"metadataBytes":args.output.stat().st_size,"catalog":str(args.output)},ensure_ascii=True))


if __name__=="__main__":
    main()
