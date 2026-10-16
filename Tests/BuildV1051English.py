"""Build the creator-exact Ver1.051 English display overlay without touching games.

Only original matching Chinese source strings are substituted at render time.
Retain v1.04 translations and runtime-only fallbacks; exclude source-specific
old strings that no longer exist in the v1.051 creator source inventory.
"""
import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import EnglishCatalog as catalog

ID = "three-in-one-3-3-5-v1-051"
PRIOR = "three-in-one-3-3-5-v1-04"
PACKET = ROOT / "English/v1051-reviewed-overrides.json"
ALIASES = ROOT / "English/v1051-rendered-choice-aliases.json"
ENGRAMS = ROOT / "English/v1051-engram-overrides.json"
SOURCE_SPECIFIC = {"dialogue", "dialogue-block", "database", "glossary-description"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--extracted", type=pathlib.Path, required=True)
    args = parser.parse_args()
    source = args.extracted.resolve()
    assert source.is_relative_to((ROOT / "Staging").resolve())
    markers = next(v["Fingerprints"] for v in catalog.load(ROOT / "version-fingerprints.json")["Versions"]
                   if v["Id"] == ID)
    assert all(catalog.sha((source / key).read_bytes()) == value for key, value in markers.items())

    inventory = catalog.inventory(source)
    assert not inventory["errors"], inventory["errors"]
    entries = {}
    for slot in inventory["slots"].values():
        key = slot["source"]
        if not catalog.CJK.search(key):
            continue
        row = entries.setdefault(key, {"source": key, "kinds": [], "contexts": []})
        if slot["kind"] not in row["kinds"]:
            row["kinds"].append(slot["kind"])
        if slot["context"] not in row["contexts"] and len(row["contexts"]) < 4:
            row["contexts"].append(slot["context"])

    prior_catalog = catalog.load(ROOT / "English" / PRIOR / "catalog.json")
    prior_dictionary = catalog.load(ROOT / "English" / PRIOR / "dictionary.json")
    overrides = catalog.load(PACKET)
    aliases = catalog.load(ALIASES)["derived_aliases"]
    engrams = catalog.load(ENGRAMS)
    assert len(overrides) == 44
    assert len(aliases) == 6
    assert len(engrams) == 9
    assert not (set(overrides) & set(prior_dictionary)), "Reviewed new source keys collided with old v1.04"
    assert all(key in entries for key in overrides), "New creator display key missing from source inventory"
    assert all(catalog.compatible(key, value) for key, value in overrides.items()), "Changed control token"
    assert not (set(aliases) & set(overrides)), "Renderer aliases overlap authored source strings"
    assert all(catalog.compatible(key, value) for key, value in aliases.items())
    assert not set(engrams) & set(overrides), "Engram review conflicts with new creator content"
    assert all(catalog.compatible(key, value) for key, value in engrams.items()), "Engram control codes changed"
    # RPG Maker draws multiline database descriptions one line at a time.
    # A translated full description cannot translate an unmatched second line.
    weapons = catalog.load(source / "data/Weapons.json")
    native_engrams = {line for weapon in weapons if weapon and
                      weapon.get("wtypeId") == 20
                      for line in weapon.get("description", "").split("\n")}
    assert set(engrams) <= native_engrams, "Engram key is not an exact native description line"

    dictionary = {}
    for key in entries:
        translation = engrams.get(key, overrides.get(key, prior_dictionary.get(key)))
        if isinstance(translation, str) and catalog.compatible(key, translation):
            dictionary[key] = translation

    # Whole dialogue-block keys can be rebuilt from their exact English lines.
    for key in entries:
        if key not in dictionary and "\n" in key:
            lines = key.split("\n")
            if all(line in dictionary or not catalog.CJK.search(line) for line in lines):
                value = "\n".join(dictionary.get(line, line) for line in lines)
                if catalog.compatible(key, value):
                    dictionary[key] = value

    # Dynamic plugin UI strings are not necessarily present as literal current
    # source records. Keep them as verified backwards-compatible fallbacks.
    recovered = {}
    for key, value in prior_dictionary.items():
        if key in entries:
            continue
        row = prior_catalog.get(key)
        if not row or SOURCE_SPECIFIC.intersection(row["kinds"]):
            continue
        if not catalog.compatible(key, value):
            continue
        recovered[key] = value
        entries[key] = {"source": key,
                        "kinds": list(row["kinds"]) if "legacy-runtime-display" in row["kinds"]
                                 else list(row["kinds"]) + ["legacy-runtime-display"],
                        "contexts": list(row["contexts"]) + ["v1.04 verified runtime fallback"]}
        dictionary[key] = value

    # MPP_ChoiceEX removes the conditional if(...) fragment before drawText.
    # Source-key translations alone cannot translate the six displayed
    # captions. Preserve the original guarded choice for gameplay control.
    for key, value in aliases.items():
        if key in dictionary:
            assert dictionary[key] == value, "A renderer alias conflicts with an older translation"
        dictionary[key] = value
        if key not in entries:
            entries[key] = {"source": key, "kinds": ["native-rendered-choice-alias"],
                            "contexts": ["MPP_ChoiceEX.js: conditional if(...) stripped before rendering"]}

    for key, value in engrams.items():
        dictionary[key] = value
        if key not in entries:
            entries[key] = {"source": key, "kinds": ["native-drawn-engram-description-line"],
                            "contexts": ["Weapons.json: wtypeId=20 / description split by newline"]}

    assert all(dictionary.get(key) == value for key, value in prior_dictionary.items()
               if key in dictionary and key not in engrams), "An unreviewed translation changed"
    assert set(overrides) <= set(dictionary)
    assert all(key in entries and catalog.compatible(key, value) for key, value in dictionary.items())
    prior_titles = [key for key in prior_dictionary if key.startswith("魔法少女苍蓝星★三合一改")]
    assert len(prior_titles) == 3 and all(dictionary.get(key) == prior_dictionary[key] for key in prior_titles)

    target = ROOT / "English" / ID
    target.mkdir(exist_ok=True)
    catalog.dump(target / "dictionary.json", dictionary)
    catalog.dump(target / "catalog.json", entries)
    catalog.dump(target / "remaining.json", {key: key for key in entries if key not in dictionary})
    manifest = {
        "Format": 1, "VersionId": ID,
        "Name": "Three-in-one 3.3.5 v1.051 English text overlay",
        "Fingerprints": markers,
        "TotalTexts": len(entries), "TranslatedTexts": len(dictionary),
        "ReviewedTexts": len(overrides) + len(engrams),
        "ReferenceTexts": len(dictionary) - len(overrides) - len(engrams),
        "Complete": len(entries) == len(dictionary),
        "Mode": "display-only", "SkipNewBaseTranslation": False,
        "DictionaryHash": catalog.sha((target / "dictionary.json").read_bytes()),
        "CatalogHash": catalog.sha((target / "catalog.json").read_bytes())
    }
    catalog.dump(target / "manifest.json", manifest)
    print(json.dumps({"status": "PASS", "version": ID,
                      "catalogedSourceTexts": len(entries) - len(recovered),
                      "catalogedTextsAndRuntimeFallbacks": len(entries),
                      "verifiedEnglishTranslations": len(dictionary),
                      "creator151NewReviewedTranslations": len(overrides),
                      "reviewedEngramLineTranslations": len(engrams),
                      "nativeRenderedChoiceAliases": len(aliases),
                      "priorRuntimeFallbacksRetained": len(recovered),
                      "untranslatedNewOrUnreviewed": len(entries) - len(dictionary),
                      "sourceOriginalsAndSavesEdited": 0}, indent=2))


if __name__ == "__main__":
    main()
