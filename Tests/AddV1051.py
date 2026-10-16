"""Register the exact creator Ver1.051 RAR5 overlay as a forward v1.04 upgrade.

Read-only on the Telegram creator archive, extracted game data and native user
installations. Writes only the launcher recipe and fingerprint indexes.
"""
import argparse
import copy
import hashlib
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
ID = "three-in-one-3-3-5-v1-051"
PRIOR = "three-in-one-3-3-5-v1-04"
ARCHIVE_SHA = "feda86817320cd548a56fc17afa41cc8e345d93b3dae3d44f7535538c9a5d8ac"
ARCHIVE_BYTES = 547645039
FILE_COUNT = 5163
CHANGED_COUNT = 18
MARKERS = ("data/System.json", "js/plugins.js", "js/rpg_managers.js",
           "data/Skills.json", "data/CommonEvents.json", "data/Enemies.json")
ALLOWED = {"audio", "data", "fonts", "img", "js", "movies", "css", "icon"}


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for data in iter(lambda: f.read(1 << 20), b""):
            h.update(data)
    return h.hexdigest()


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def dump(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")),
                    encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", type=pathlib.Path, required=True)
    parser.add_argument("--extracted", type=pathlib.Path, required=True)
    args = parser.parse_args()
    source, archive = args.extracted.resolve(), args.archive.resolve()
    assert source.is_relative_to((ROOT / "Staging").resolve())
    assert source.is_dir() and (source / "data/System.json").is_file()
    assert archive.is_file() and archive.stat().st_size == ARCHIVE_BYTES
    assert digest(archive) == ARCHIVE_SHA, "Not the exact creator 1.051 release"

    files = {}
    for p in source.rglob("*"):
        if not p.is_file():
            continue
        assert not p.is_symlink(), "Linked content is not allowed"
        rel = p.relative_to(source).as_posix()
        if rel.split("/")[0] in ALLOWED:
            assert rel not in files
            files[rel] = digest(p)
    assert len(files) == FILE_COUNT

    rp, fp = ROOT / "mod-recipes.json", ROOT / "version-fingerprints.json"
    recipes, index = load(rp), load(fp)
    previous = next(x for x in recipes["Recipes"] if x["Id"] == PRIOR)
    old = previous["Files"]
    assert set(old) == set(files), "An added or removed gameplay file needs separate review"
    changed = sorted(p for p in files if old[p] != files[p])
    assert len(changed) == CHANGED_COUNT
    assert all(n in changed for n in ("data/System.json", "js/plugins.js",
                                      "data/CommonEvents.json", "data/Skills.json",
                                      "js/rpg_managers.js"))

    recipe = copy.deepcopy(previous)
    recipe.update(
        Id=ID, Name="Three-in-one 3.3.5 v1.051",
        SaveGeneration=1051,
        Components=["Abu 56.6", "Story 1.0", "Skills 3.3.5 v1.051"],
        UpgradeFrom=[PRIOR],
        SystemHash=files["data/System.json"],
        PluginsHash=files["js/plugins.js"],
        Fingerprints={key: files[key] for key in MARKERS},
        ArchiveHash=ARCHIVE_SHA,
        ArchiveName=archive.name,
        ArchiveBytes=archive.stat().st_size,
        Files=files,
    )
    known = next((x for x in recipes["Recipes"] if x["Id"] == ID), None)
    if known is None:
        recipes["Recipes"].insert(0, recipe)
        dump(rp, recipes)
    else:
        assert known == recipe, "Existing creator 1.051 recipe differs"
    expected = {"Id": ID, "Name": recipe["Name"],
                "Fingerprints": recipe["Fingerprints"], "Files": None}
    existing = next((x for x in index["Versions"] if x["Id"] == ID), None)
    if existing is None:
        index["Versions"].insert(0, expected)
        dump(fp, index)
    else:
        assert existing == expected, "Existing 1.051 fingerprints differ"
    print(json.dumps({"status": "PASS", "id": ID, "archiveSHA256": ARCHIVE_SHA,
                      "files": len(files), "newPaths": 0, "removedPaths": 0,
                      "changedPaths": changed, "forwardFrom": PRIOR,
                      "savesCopiedOnlyForward": True, "gameFilesModified": False},
                     indent=2))


if __name__ == "__main__":
    main()
