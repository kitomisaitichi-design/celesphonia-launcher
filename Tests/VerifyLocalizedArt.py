"""Native, isolated, reversible help-image integration QA.

Copies creator marker files and graphics to a private synthetic NW.js game,
then exercises the REAL launcher English ON/OFF/ON CLI. Never touches the
native source game, its save directory, or any installed game state.
"""
import argparse
import hashlib
import json
import pathlib
import shutil
import subprocess
import tempfile
from PIL import Image

HOME = pathlib.Path(__file__).resolve().parents[1]
ART = HOME / "English/ImageLocalization"
IMAGES = json.loads((ART / "manifest.json").read_text("utf8"))["images"]
VERSIONS = ("three-in-one-3-3-5-v1-051", "three-in-one-3-3-5-v1-052")


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(exe, *args):
    subprocess.run([str(exe), *(str(a) for a in args)], check=True, timeout=90)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=pathlib.Path, required=True)
    parser.add_argument("--creator-1051", type=pathlib.Path, required=True)
    parser.add_argument("--creator-1052", type=pathlib.Path, required=True)
    args = parser.parse_args()
    assert args.exe.is_file()
    marker_index = json.loads((HOME / "version-fingerprints.json").read_text("utf8"))
    results = []
    # The private fixture is *within this workspace*, not the user's games.
    base = HOME / "Staging/continuation-v1061-prime/native-qa-release-v107"
    base.mkdir(parents=True, exist_ok=True)
    settings = args.exe.parent / "profiles.json"
    if not settings.exists():
        settings.write_text(json.dumps({
            "Profiles": [], "SelectedId": None, "InstallRoot": str(base),
            "SearchRoots": None,
        }, separators=(",", ":")), encoding="utf8")
    for ident, source_root in zip(VERSIONS, (args.creator_1051, args.creator_1052)):
        source_root = source_root.resolve()
        assert source_root.is_dir()
        game = base / ident
        assert not game.exists(), "Refuse to overwrite a previous native art QA fixture"
        web = game / "www"
        web.mkdir(parents=True)
        (game / "Game.exe").write_bytes(b"synthetic game fixture: never run")
        (game / "package.json").write_text('{"main":"www/index.html"}', "utf8")
        original_html = b'\xef\xbb\xbf<!DOCTYPE html>\r\n<script src="js/main.js"></script>\r\n'
        (web / "index.html").write_bytes(original_html)
        (web / "save").mkdir()
        (web / "save/qa.rpgsave").write_bytes(b"isolated dummy save")
        version = next(x for x in marker_index["Versions"] if x["Id"] == ident)
        hashes = {}
        for name, digest in version["Fingerprints"].items():
            src = source_root / name
            assert src.is_file() and sha(src) == digest
            dest = web / name
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(src, dest)
            hashes[name] = sha(dest)
        for name, meta in IMAGES.items():
            src = source_root / meta["nativePath"]
            assert src.is_file() and sha(src) == meta["originalSHA256"], name
            dst = web / meta["nativePath"]
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(src, dst)
            original = Image.open(src)
            replacement = Image.open(ART / name)
            assert original.size == replacement.size == (meta["width"], meta["height"])
            assert sha(ART / name) == meta["localizedSHA256"]
            hashes[meta["nativePath"]] = sha(dst)
        run(args.exe, "--english-apply", game)
        dictionary = (web / "celesphonia-english/dictionary.js").read_text("utf8")
        assert '"Glossary_2":"Glossary_2"' in dictionary
        assert '"催眠用语":"HypnosisWords"' in dictionary
        english_folder = web / "celesphonia-english/images"
        state = json.loads((web / "celesphonia-english/state.json").read_text("utf8"))
        assert len([x for x in state["Assets"] if x.startswith("images/")]) == len(IMAGES)
        for name, meta in IMAGES.items():
            dest = english_folder / name
            assert dest.is_file() and sha(dest) == meta["localizedSHA256"]
        status = base / (ident + "-status.json")
        run(args.exe, "--english-status", game, status)
        reading = json.loads(status.read_text("utf8"))
        assert reading["Enabled"] and not reading["UpdateAvailable"], reading
        assert reading["VersionId"] == ident
        for name, digest in hashes.items():
            assert sha(web / name) == digest, "Source image or gameplay data was changed"
        assert (web / "save/qa.rpgsave").read_bytes() == b"isolated dummy save"
        run(args.exe, "--english-restore", game)
        assert (web / "index.html").read_bytes() == original_html
        for name, digest in hashes.items():
            assert sha(web / name) == digest
        run(args.exe, "--english-apply", game)
        assert (web / "celesphonia-english/images/Glossary_2.png").is_file()
        result = {"version": ident, "status": "PASS",
                  "sourceImagesUnchanged": len(IMAGES),
                  "originalGameplayMarkersUnchanged": len(version["Fingerprints"]),
                  "saveUnchanged": True, "englishOnOffOn": True,
                  "picturesInstalled": len(IMAGES)}
        results.append(result)
        print(json.dumps(result))
    output = HOME / "Tests/verified-art-install-report.json"
    output.write_text(json.dumps({"status": "PASS", "versions": results},
                                 indent=2), encoding="utf8")
    print("ALL_LOCALIZED_IMAGE_INSTALLATION_CHECKS_PASS")


if __name__ == "__main__":
    main()
