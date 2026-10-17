"""Build a byte-hash catalog from a verified standalone Celesphonia installation.

This is an authoring-only operation. It never copies or edits game files.
The catalog contains paths, sizes and hashes, NOT the game itself.
"""
import argparse
import hashlib
import json
import pathlib
import re
import sys

ROOT_FILES = {".exe",".dll",".json",".pak",".bin",".dat",".html",".ico",".ini"}
GAME_FOLDERS = {"www","locales","swiftshader","audio","data","fonts","img","js","movies","css","icon"}
SKIP_FOLDERS = {"save","output","decrypted","celesphonia-english"}
SKIP_FILES = {"manualtransfile.json","debug.log","launcher-base.json","launcher-build.json","launcher-reconstruction.json","launcher-upgrade.json"}
REQUIRED = ("package.json","data/System.json","js/plugins.js","js/rpg_managers.js")

# This is a VERSION-SPECIFIC, path-anchored policy. Do not omit all .bin/.exe,
# arbitrary plugins, or RPG Maker runtime files: NW.js needs its .bin/.dll
# files, and plugins used by the game belong in the complete native catalog.
CH_FINGERPRINTS = {
    "data/System.json": "3b3b13e6a08254d0ff044a0247ae04556d648377f703fcd4192187b118bfb592",
    "js/plugins.js": "a885e9eea036b774adb607a0aa170d60a6ae46a3073e94418f3c69a781ed2458",
    "js/rpg_managers.js": "935c1a4681321d2d0e6ea63f4ea8f8fd8775c614b23528a5472f8f835298df98",
}
CH_NON_RUNTIME_EXACT = frozenset({
    "microsoft.windowsapicodepack.dll",  # .NET decrypter support, not NW.js runtime.
    "microsoft.windowsapicodepack.shell.dll",  # .NET decrypter support.
    "newtonsoft.json.dll",  # .NET decrypter support; GoRPG native runtime has no such assembly.
    "rmdec.exe",  # Standalone decryptor, not the NW.js game executable.
    "www/game.rpgproject",  # RPG Maker editor marker.
    "www/data/stand_base_actor_01.xlsx",  # RPG Maker authoring spreadsheet.
    "www/img/pictures/cloth/desktop.ini",  # Windows folder view metadata.
    "www/img/pictures/under/desktop.ini",
})
CH_TRANSLATION_HELPER_FILES = frozenset({
    "www/js/plugins/clipboard_llule.js",
    "www/js/plugins/cliploger.exe",
})


def enabled_plugins(web):
    """Return enabled RPG Maker plugin names, or None if the list is unreadable.

    The optional clipboard helper is retained when its usage cannot be
    established; we do not discard a dependency on uncertain evidence.
    """
    try:
        source = (web / "js" / "plugins.js").read_text(encoding="utf-8-sig")
        match = re.search(r"\bvar\s+\$plugins\s*=\s*(\[)", source)
        if match is None:
            return None
        plugins, _ = json.JSONDecoder().raw_decode(source[match.start(1):])
        if not isinstance(plugins, list):
            return None
        result = set()
        for item in plugins:
            if not isinstance(item, dict) or not isinstance(item.get("name"), str):
                return None
            if item.get("status") is True:
                result.add(item["name"].casefold())
        return result
    except (OSError, UnicodeError, ValueError):
        return None


def exclusion_reason(version, relative_path, active_plugins=None):
    """Exact CH-only native-catalog exclusion; paths always relative to root."""
    if version != "ch-current":
        return None
    rel = relative_path.replace("\\", "/").casefold()
    if rel in CH_NON_RUNTIME_EXACT:
        return "CH editor/utility artifact"
    if rel.startswith("www/js/.vs/"):
        return "CH Visual Studio workspace cache"
    if rel.startswith("locales/") and rel.count("/") == 1 and rel.endswith(".pak.info"):
        return "CH Chromium locale resource-map sidecar"
    if rel.startswith("www/") and rel.endswith(".txt"):
        return "CH bundled text notes/asset credits (not runtime assets)"
    if "/" not in rel and rel.startswith("trsdata") and rel.endswith(".bin"):
        return "CH root-only MTool translation data"
    if rel.startswith("www/rpg-mv-mz-decrypter") and rel.endswith(".exe") and rel.count("/") == 1:
        return "CH external decrypter"
    if rel in CH_TRANSLATION_HELPER_FILES and active_plugins is not None and "clipboard_llule" not in active_plugins:
        return "CH inactive clipboard/translation helper"
    return None


def require_fingerprints(indexed, version):
    if version != "ch-current":
        return
    for rel, expected in CH_FINGERPRINTS.items():
        entry = indexed.get("www/" + rel) or indexed.get(rel)
        if entry is None:
            raise SystemExit("Missing CH version fingerprint: " + rel)
        if entry["Hash"] != expected:
            raise SystemExit("CH version fingerprint mismatch: " + rel)


def file_sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        while block:=f.read(2*1024*1024):
            h.update(block)
    return h.hexdigest()

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("reference",type=pathlib.Path,help="An intact, verified game directory")
    parser.add_argument("output",type=pathlib.Path)
    parser.add_argument("--version",default="gorpg-1-23")
    args=parser.parse_args()
    root=args.reference.resolve(strict=True)
    if not (root/"package.json").is_file() or not ((root/"Game.exe").is_file() or (root/"nw.exe").is_file()):
        raise SystemExit("Not a standalone game root")
    web=root/"www" if (root/"www").is_dir() else root
    active_plugins=enabled_plugins(web) if args.version=="ch-current" else None
    paths=[]
    for p in root.iterdir():
        if (p.is_file() and (p.suffix.lower() in ROOT_FILES or p.name=="steam_appid.txt")
                and p.name.lower() not in SKIP_FILES
                and exclusion_reason(args.version,p.relative_to(root).as_posix(),active_plugins) is None):
            paths.append(p)
        if p.is_dir() and p.name.lower() in GAME_FOLDERS:
            for f in p.rglob("*"):
                if (f.is_file() and not SKIP_FOLDERS.intersection(x.lower() for x in f.relative_to(root).parts)
                        and f.suffix.lower()!=".docx" and f.name.lower() not in SKIP_FILES
                        and exclusion_reason(args.version,f.relative_to(root).as_posix(),active_plugins) is None):
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
            raise SystemExit("Missing necessary game file: "+rel)
    require_fingerprints(indexed,args.version)
    if "Game.exe" not in indexed and "nw.exe" not in indexed:
        raise SystemExit("Missing native launcher")
    result={"Format":1,"VersionId":args.version,"Files":files}
    args.output.write_text(json.dumps(result,ensure_ascii=False,separators=(",",":"))+"\n",encoding="utf-8")
    print(json.dumps({"status":"PASS","version":result["VersionId"],"files":len(files),"totalBytes":sum(f["Size"] for f in files),"metadataBytes":args.output.stat().st_size,"catalog":str(args.output)},ensure_ascii=True))


if __name__=="__main__":
    main()
