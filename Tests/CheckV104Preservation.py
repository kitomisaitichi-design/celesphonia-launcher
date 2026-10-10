"""Guard against losing verified v1.03 display strings on v1.04 reindexing.

This is a read-only audit of packaged dictionaries and (optionally) the actual
creator's extracted v1.04 sources. It never patches installed game files.
"""
import argparse
import hashlib
import json
import pathlib

HOME = pathlib.Path(__file__).resolve().parents[1]
ENGLISH = HOME / "English"
PRIOR = ENGLISH / "three-in-one-3-3-5-v1-03"
LATEST = ENGLISH / "three-in-one-3-3-5-v1-04"
SOURCE_SPECIFIC = {"dialogue", "dialogue-block", "database", "glossary-description"}


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=pathlib.Path)
    args = parser.parse_args()

    old_catalog = load(PRIOR / "catalog.json")
    old_dictionary = load(PRIOR / "dictionary.json")
    new_catalog = load(LATEST / "catalog.json")
    new_dictionary = load(LATEST / "dictionary.json")
    manifest = load(LATEST / "manifest.json")

    native_keys = {k for k, row in new_catalog.items()
                   if "legacy-runtime-display" not in row["kinds"]}
    new_source_keys = native_keys - set(old_catalog)
    removed = set(old_dictionary) - set(new_dictionary)
    legacy = {k for k, row in new_catalog.items()
              if "legacy-runtime-display" in row["kinds"]}

    assert len(old_dictionary) == 34948
    assert len(new_dictionary) == 34957
    assert len(legacy) == 472
    assert len(new_source_keys) == 21
    assert not (set(new_source_keys) - set(new_dictionary))
    assert len(removed) == 12, sorted(removed)
    assert all(SOURCE_SPECIFIC.intersection(old_catalog[k]["kinds"]) for k in removed)
    assert all(new_dictionary[k] == value for k, value in old_dictionary.items()
               if k in new_dictionary), "A previously retained translation changed"
    assert all(k in old_catalog and k not in removed and
               not SOURCE_SPECIFIC.intersection(old_catalog[k]["kinds"])
               for k in legacy)
    assert all(new_catalog[k]["contexts"][-1] == "v1.03 verified runtime fallback"
               for k in legacy)
    assert manifest["TranslatedTexts"] == len(new_dictionary)
    assert manifest["TotalTexts"] == len(new_catalog)
    for name, field in (("dictionary.json", "DictionaryHash"),
                        ("catalog.json", "CatalogHash")):
        assert hashlib.sha256((LATEST / name).read_bytes()).hexdigest() == manifest[field]

    source_hits = 0
    if args.source:
        source = args.source.resolve()
        markers = load(HOME / "version-fingerprints.json")["Versions"]
        fingerprints = next(x["Fingerprints"] for x in markers if x["Id"] ==
                            "three-in-one-3-3-5-v1-04")
        for name, expected in fingerprints.items():
            assert hashlib.sha256((source / name).read_bytes()).hexdigest() == expected
        for key in legacy:
            contexts = old_catalog[key].get("contexts", [])
            scripts = {"/".join(x.split("/")[:3]) for x in contexts
                       if x.startswith("js/plugins/")}
            if any((source / name).is_file() and
                   key in (source / name).read_text(encoding="utf-8-sig",
                                                     errors="replace")
                   for name in scripts):
                source_hits += 1
        assert source_hits >= 30, ("Missing translated native JS labels", source_hits)

    print(json.dumps({"status": "PASS", "v104Translated": len(new_dictionary),
                      "v104SourceKeysAdded": len(new_source_keys),
                      "legacyDisplaysRecovered": len(legacy),
                      "oldChangedTextsDeliberatelyExcluded": len(removed),
                      "unchangedLegacyKeysRegressed": 0,
                      "actualV104NativeScriptHits": source_hits,
                      "gameFilesModified": 0}, indent=2))


if __name__ == "__main__":
    main()
