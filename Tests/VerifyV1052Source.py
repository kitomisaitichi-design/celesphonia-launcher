"""Read-only creator Ver1.051 / Ver1.052 source regression audit."""
import argparse
import hashlib
import json
import pathlib

HOME = pathlib.Path(__file__).resolve().parents[1]
ALLOWED = {"audio", "data", "fonts", "img", "js", "movies", "css", "icon"}
IDS = ("three-in-one-3-3-5-v1-051", "three-in-one-3-3-5-v1-052")
LATEST_SHA = "5dc382a21ed8db8d5597dbabefa6b82ff243937b51199f5b66e45b4854b7e1a2"


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def overlay_files(root):
    assert root.is_dir()
    files = {}
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        assert not path.is_symlink(), path
        name = path.relative_to(root).as_posix()
        if name.split("/")[0] in ALLOWED:
            assert name not in files
            files[name] = sha(path)
    return files


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--prior", type=pathlib.Path, required=True)
    p.add_argument("--new", type=pathlib.Path, required=True)
    p.add_argument("--creator-archive", type=pathlib.Path, required=True)
    args = p.parse_args()
    recipes = json.loads((HOME / "mod-recipes.json").read_text("utf-8-sig"))["Recipes"]
    indexed = {x["Id"]: x for x in recipes}
    archive = args.creator_archive.resolve()
    assert archive.is_file() and archive.stat().st_size == 547645049
    assert sha(archive) == LATEST_SHA

    checks = {}
    for ident, root in zip(IDS, (args.prior.resolve(), args.new.resolve())):
        assert root.is_relative_to((HOME / "Staging").resolve())
        files = overlay_files(root)
        recipe = indexed[ident]
        assert len(files) == 5163 and files == recipe["Files"]
        assert all(files[key] == digest for key, digest in recipe["Fingerprints"].items())
        checks[ident] = files
    a, b = map(checks.get, IDS)
    assert set(a) == set(b)
    modified = sorted(key for key in a if a[key] != b[key])
    assert modified == ["data/Enemies.json"], modified
    assert indexed[IDS[1]]["UpgradeFrom"] == ["three-in-one-3-3-5-v1-04", IDS[0]]
    print(json.dumps({
        "status": "PASS", "creatorArchiveSHA256": LATEST_SHA,
        "ver1051Files": len(a), "ver1052Files": len(b),
        "newFiles": 0, "deletedFiles": 0, "modifiedPaths": modified,
        "identicalFiles": 5162,
        "supportsDirect104To1051": "three-in-one-3-3-5-v1-04" in indexed[IDS[0]]["UpgradeFrom"],
        "supportsDirect104To1052": "three-in-one-3-3-5-v1-04" in indexed[IDS[1]]["UpgradeFrom"],
        "nativeSourceEdits": 0,
    }, indent=2))


if __name__ == "__main__":
    main()
