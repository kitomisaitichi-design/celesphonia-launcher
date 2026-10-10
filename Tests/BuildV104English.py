"""Version-specific English display pack built from actual v1.04 source text.

Reuses only exact, control-code-compatible English entries from verified prior
translations. All new/changed developer dialogue remains untouched until
reviewed; no game scripts, database or saves are edited.
"""
import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import EnglishCatalog as catalog

ID = "three-in-one-3-3-5-v1-04"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--extracted", type=pathlib.Path, required=True)
    args = parser.parse_args()
    source = args.extracted.resolve()
    assert source.is_relative_to((ROOT / "Staging").resolve())
    markers = next(row["Fingerprints"] for row in catalog.load(ROOT / "version-fingerprints.json")["Versions"] if row["Id"] == ID)
    for name, digest in markers.items():
        assert catalog.sha((source / name).read_bytes()) == digest
    inventory = catalog.inventory(source)
    assert not inventory["errors"], "Cannot release an incomplete translation source inventory"
    entries = {}
    for slot in inventory["slots"].values():
        text = slot["source"]
        if not catalog.CJK.search(text):
            continue
        row = entries.setdefault(text, {"source": text, "kinds": [], "contexts": []})
        if slot["kind"] not in row["kinds"]:
            row["kinds"].append(slot["kind"])
        if len(row["contexts"]) < 4 and slot["context"] not in row["contexts"]:
            row["contexts"].append(slot["context"])

    # Carry forward *all* prior exact-source, compatible translations.
    # v1.03 contains window-title strings missed by v1.01; v1.01 also has
    # some reviewed fields absent from v1.03. Never regress either cohort.
    old = {}
    for version in ("v1-01", "v1-02", "v1-03"):
        old.update(catalog.load(ROOT / f"English/three-in-one-3-3-5-{version}/dictionary.json"))
    reviewed = catalog.load(ROOT / "English/reviewed-translations.json")
    # Version-1.04-only review was done after the original catalog build;
    # recover these exact 18 reviewed strings from the published v1.0.1 pack.
    # Keep their provenance in a separate tracked source so every later
    # regeneration retains the 1.04 dialogue, descriptions and labels.
    specific = catalog.load(ROOT / "English/v104-reviewed-overrides.json")
    assert len(specific) == 18 and all(
        key in entries and catalog.compatible(key, value)
        for key, value in specific.items()
    )
    reviewed.update(specific)
    reviewed["魔法少女苍蓝星★三合一改  禁止未经许可使用相关素材"] = (
        "Magical Girl Celesphonia: Three-in-One Mod | Do not use these assets without permission"
    )
    dictionary = {}
    for key in entries:
        translated = reviewed.get(key, old.get(key))
        if isinstance(translated, str) and catalog.compatible(key, translated):
            dictionary[key] = translated
    for key in entries:
        if key not in dictionary and "\n" in key:
            lines = key.split("\n")
            if all(line in dictionary or not catalog.CJK.search(line) for line in lines):
                value = "\n".join(dictionary.get(line, line) for line in lines)
                if catalog.compatible(key, value):
                    dictionary[key] = value
    # Regression gate: preserve the Three-in-One plugin titles without
    # rewriting the native title IDs used in save-file compatibility logic.
    plugin_titles = [key for key in entries if key.startswith("魔法少女苍蓝星★三合一改")]
    assert len(plugin_titles) == 3 and all(key in dictionary for key in plugin_titles), (
        "One of the v1.04 window/save titles was omitted from the display catalog"
    )
    prior_catalog = catalog.load(ROOT / "English/three-in-one-3-3-5-v1-03/catalog.json")
    new_source_keys = [key for key in entries if key not in prior_catalog]
    assert len(new_source_keys) == 21 and all(key in dictionary for key in new_source_keys), (
        "Previously translated v1.04 new/changed strings were lost"
    )
    target = ROOT / "English" / ID
    target.mkdir(exist_ok=True)
    catalog.dump(target / "dictionary.json", dictionary)
    catalog.dump(target / "catalog.json", entries)
    catalog.dump(target / "remaining.json", {key: key for key in entries if key not in dictionary})
    manifest = {
        "Format": 1, "VersionId": ID, "Name": "Three-in-one 3.3.5 v1.04 English text overlay",
        "Fingerprints": markers, "TotalTexts": len(entries), "TranslatedTexts": len(dictionary),
        "ReviewedTexts": sum(key in reviewed for key in dictionary),
        "ReferenceTexts": sum(key not in reviewed for key in dictionary),
        "Complete": len(entries) == len(dictionary),
        "Mode": "display-only", "SkipNewBaseTranslation": False,
        "DictionaryHash": catalog.sha((target / "dictionary.json").read_bytes()),
        "CatalogHash": catalog.sha((target / "catalog.json").read_bytes()),
    }
    catalog.dump(target / "manifest.json", manifest)
    assert all(k in entries and catalog.compatible(k, v) for k, v in dictionary.items())
    print(json.dumps({"status": "PASS", "version": ID, "totalSourceTexts": len(entries),
                      "verifiedExactTranslations": len(dictionary),
                      "untranslatedNewOrUnreviewed": len(entries) - len(dictionary),
                      "developerFilesModified": 0}, indent=2))


if __name__ == "__main__":
    main()
