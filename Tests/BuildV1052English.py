"""Source-verified Ver1.052 display-only English pack, carrying Ver1.051 fixes.

Ver1.051's 80-Engram audit and conditional-choice aliases remain reviewed
source references, not ad-hoc replacement of the game's scripts or JSON.
"""
import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import EnglishCatalog as c

ID = "three-in-one-3-3-5-v1-052"
PRIOR = "three-in-one-3-3-5-v1-051"
SPECIAL_KINDS = {"native-drawn-engram-description-line",
                 "native-rendered-choice-alias", "legacy-runtime-display"}
SOURCE_KINDS = {"dialogue", "dialogue-block", "database", "glossary-description"}


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--extracted", type=pathlib.Path, required=True)
    p.add_argument("--approved", type=pathlib.Path,
                   default=ROOT / "English/v1052-reviewed-overrides.json")
    args = p.parse_args()
    game = args.extracted.resolve()
    assert game.is_relative_to((ROOT / "Staging").resolve())
    assert (game / "data/System.json").is_file()
    recipe = next(x for x in c.load(ROOT / "mod-recipes.json")["Recipes"]
                  if x["Id"] == ID)
    markers = recipe["Fingerprints"]
    assert all((game / key).is_file() and c.sha((game / key).read_bytes()) == h
               for key, h in markers.items())

    inventory = c.inventory(game)
    assert not inventory["errors"], inventory["errors"]
    entries = {}
    for slot in inventory["slots"].values():
        key = slot["source"]
        if not c.CJK.search(key):
            continue
        row = entries.setdefault(key, {"source": key, "kinds": [], "contexts": []})
        if slot["kind"] not in row["kinds"]:
            row["kinds"].append(slot["kind"])
        if slot["context"] not in row["contexts"] and len(row["contexts"]) < 4:
            row["contexts"].append(slot["context"])

    prior_catalog = c.load(ROOT / "English" / PRIOR / "catalog.json")
    prior = c.load(ROOT / "English" / PRIOR / "dictionary.json")
    reviewed = c.load(args.approved) if args.approved.is_file() else {}
    assert all(key in entries and c.compatible(key, text)
               for key, text in reviewed.items()), "New Ver1.052 review key differs from creator source"
    translated = {}
    for key in entries:
        value = reviewed.get(key, prior.get(key))
        if isinstance(value, str) and c.compatible(key, value):
            translated[key] = value
    for key in entries:
        if key in translated or "\n" not in key:
            continue
        pieces = key.split("\n")
        if all(x in translated or not c.CJK.search(x) for x in pieces):
            value = "\n".join(translated.get(x, x) for x in pieces)
            if c.compatible(key, value):
                translated[key] = value

    weapons = c.load(game / "data/Weapons.json")
    native_engrams = {line for item in weapons if item and item.get("wtypeId") == 20
                      for line in item.get("description", "").split("\n")}
    eng = c.load(ROOT / "English/v1051-engram-overrides.json")
    aliases = c.load(ROOT / "English/v1051-rendered-choice-aliases.json")["derived_aliases"]
    assert len(eng) == 9 and len(aliases) == 6
    assert set(eng).issubset(native_engrams), "Original Ver1.051 Engram lines changed in 1.052"

    retained = {}
    for key, value in prior.items():
        if key in entries or key in reviewed:
            continue
        row = prior_catalog.get(key)
        if not row or not c.compatible(key, value):
            continue
        kinds = set(row["kinds"])
        if kinds & SOURCE_KINDS and not kinds & SPECIAL_KINDS:
            continue
        if not kinds & SPECIAL_KINDS:
            continue
        retained[key] = value
        entries[key] = row
        translated[key] = value

    # Some RPG Maker plugins render database descriptions line by line, and
    # the choice plugin removes condition guards. Preserve both reviewed maps.
    for key, value in dict(aliases, **eng).items():
        assert c.compatible(key, value)
        translated[key] = value
        if key not in entries:
            entries[key] = prior_catalog[key]
    assert all(translated.get(key) == value for key, value in eng.items())
    assert all(translated.get(key) == value for key, value in aliases.items())
    assert all(key in entries and c.compatible(key, value) for key, value in translated.items())
    changed = [key for key in prior if key in translated and
               translated[key] != prior[key] and key not in reviewed]
    assert not changed, "An existing English translation was overwritten without review"

    target = ROOT / "English" / ID
    target.mkdir(parents=True, exist_ok=True)
    c.dump(target / "dictionary.json", translated)
    c.dump(target / "catalog.json", entries)
    c.dump(target / "remaining.json", {k: k for k in entries if k not in translated})
    manifest = {
        "Format": 1,
        "VersionId": ID,
        "Name": "Three-in-one 3.3.5 Ver1.052 English text overlay",
        "Fingerprints": markers,
        "TotalTexts": len(entries),
        "TranslatedTexts": len(translated),
        "ReviewedTexts": 53 + len(reviewed),
        "ReferenceTexts": len(translated) - 53 - len(reviewed),
        "Complete": len(entries) == len(translated),
        "Mode": "display-only",
        "SkipNewBaseTranslation": False,
        "DictionaryHash": c.sha((target / "dictionary.json").read_bytes()),
        "CatalogHash": c.sha((target / "catalog.json").read_bytes()),
    }
    c.dump(target / "manifest.json", manifest)
    print(json.dumps({
        "status": "PASS", "version": ID,
        "nativeDisplayInventory": len(entries) - len(retained),
        "catalogPlusVerifiedRuntimeAliases": len(entries),
        "translated": len(translated),
        "priorTranslationsRetained": sum(key in translated for key in prior),
        "newReviewed": len(reviewed),
        "engramDescriptions": len(eng),
        "guardStrippedChoiceCaptions": len(aliases),
        "untranslated": len(entries) - len(translated),
        "nativeGameplayAndSavesEdited": 0}, indent=2))


if __name__ == "__main__":
    main()
