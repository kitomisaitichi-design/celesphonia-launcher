"""Read-only acceptance audit for the CH native recovery catalog.

The original CH reference had 13,654 entries. A safe, bounded cleanup removes
exactly 118 extra tool/editor/authoring files and Chromium *.pak.info source
metadata records; the other 13,536 native records, paths, sizes and SHA-256
digests must remain byte-identical to the verified historical manifest.

This test NEVER writes a manifest, game file, source archive or user save.
Run with --require-clean after the prime agent regenerates the CH catalog.
The default --audit accepts the historical pre-clean catalog for diagnostics.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import sys


PROJECT = pathlib.Path(__file__).resolve().parent.parent
CH_MANIFEST = "NativeReferenceFiles-ch-current.json"

# Fixed observed SHA-256 of the other original native reference manifests.
# No regeneration of GoRPG, Maple or hypnosis is authorized for CH cleanup.
OTHER_REFERENCE_SHA256 = {
    "NativeReferenceFiles.json":
        "bd89166a570bc129c3125dee90dc3d428ff68f3c61f9681cf2d8b45023550800",
    "NativeReferenceFiles-maple-1-7-2.json":
        "b85df08a11f11c50e8b76459a71cd366a9d11b03cc0b258e57fe398ef12aa1b0",
    "NativeReferenceFiles-three-in-one-3-3-4.json":
        "5cf9d6938e35a22942d82e3f2dc7a1600b7758fe2ef7c0a070194b8e74f7503f",
}
ORIGINAL_CH_COUNT = 13_654
EXPECTED_CLEAN_COUNT = 13_536
PROVISIONAL_COUNT = 13_539
ORIGINAL_CH_ROWS_SHA256 = (
    "fc1961b5ef4736c9e968786dc0057cc6e5e5d7166f2947ed4a9f4ea1e0bf4b50"
)
PROVISIONAL_ROWS_SHA256 = (
    "9385a91dbcd50c14ebcff81fc17bb5b9d881e98610d9c3b0417651d727e9b950"
)
EXPECTED_CLEAN_ROWS_SHA256 = (
    "56fd83c3a8e0597cae6ef30c698f3979b16c3e37a281364ee116660c0f8f9133"
)
EXPECTED_EXCLUDED_ROWS_SHA256 = (
    "14232d78f9ad50de9a6e9e8b3f9a860c37df1dc5851d077f0e1ffddd16dd70d9"
)
EXPECTED_CLEANUP_BYTES = 73_688_059

# Identifiable external tools, not the game's Game.exe, NW.js runtime or RPG
# Maker plugins. Game.exe is SHA-identical to GoRPG, which lacks these six.
EXCLUDE_FILES = frozenset({
    "RMDEC.exe",
    "Microsoft.WindowsAPICodePack.dll",
    "Microsoft.WindowsAPICodePack.Shell.dll",
    "Newtonsoft.Json.dll",
    "TrsData.bin",
    "TrsData_Bing_842023, 102632 PMP.bin",
    "www/RPG-MV-MZ-Decrypter-1.3.3-win.exe",
    "www/Game.rpgproject",
    "www/data/stand_base_actor_01.xlsx",
    "www/img/pictures/cloth/desktop.ini",
    "www/img/pictures/under/desktop.ini",
    "www/js/plugins/Clipboard_llule.js",
    "www/js/plugins/ClipLoger.exe",
})
EXCLUDE_LOCALE_INFO = re.compile(r"^locales/[^/]+\.pak\.info$", re.I)
EXCLUDE_WEB_ASSET_NOTES = re.compile(
    r"^www/(?:img/[^/]+/(?:[^/]+/)*[^/]+|[^/]+)\.txt$", re.I
)
EXCLUDE_EDITOR_CACHE = re.compile(r"^www/js/\.vs/[^/]+(?:/[^/]+)*$")
REQUIRED_NATIVE = {
    "Game.exe", "package.json", "resources.pak", "nw.dll",
    "www/package.json", "www/index.html", "www/data/System.json",
    "www/js/plugins.js", "www/js/rpg_core.js", "www/js/rpg_managers.js",
}
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")


def sha_file(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(2 * 1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def row_digest(files: list[dict]) -> str:
    """Canonical record fingerprint, independent of JSON whitespace/order."""
    digest = hashlib.sha256()
    for row in sorted(files, key=lambda x: x["Path"].casefold()):
        digest.update(row["Path"].encode("utf-8"))
        digest.update(b"\0")
        digest.update(str(row["Size"]).encode("ascii"))
        digest.update(b"\0")
        digest.update(row["Hash"].lower().encode("ascii"))
        digest.update(b"\n")
    return digest.hexdigest()


def fail(message: str) -> None:
    raise AssertionError(message)


def validate_manifest(path: pathlib.Path, expected_version: str) -> list[dict]:
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if data.get("Format") != 1 or data.get("VersionId") != expected_version:
        fail(f"{path}: unexpected format/version (expected {expected_version})")
    files = data.get("Files")
    if not isinstance(files, list) or not files:
        fail(f"{path}: empty or malformed native inventory")
    found = set()
    for index, row in enumerate(files):
        if not isinstance(row, dict) or set(row) != {"Path", "Size", "Hash"}:
            fail(f"{path}: malformed record {index}")
        relative = row["Path"]
        size, digest = row["Size"], row["Hash"]
        if (not isinstance(relative, str) or not relative
                or relative.startswith(("/", "\\"))
                or "\\" in relative or ":" in relative
                or any(p in ("", ".", "..") for p in relative.split("/"))):
            fail(f"{path}: unsafe relative path {relative!r}")
        if not isinstance(size, int) or isinstance(size, bool) or size < 0:
            fail(f"{path}: invalid size {relative}")
        if not isinstance(digest, str) or not SHA256_PATTERN.fullmatch(digest):
            fail(f"{path}: invalid SHA-256 {relative}")
        key = relative.casefold()
        if key in found:
            fail(f"{path}: duplicate case-insensitive native path {relative}")
        found.add(key)
        parts = relative.casefold().split("/")
        if ("save" in parts or "output" in parts or "decrypted" in parts
                or "celesphonia-english" in parts
                or relative.lower().endswith(".rpgsave")):
            fail(f"{path}: non-native save/English/output directory included: {relative}")
    return files


def is_external_tool_or_metadata(path: str) -> bool:
    return (path in EXCLUDE_FILES
            or bool(EXCLUDE_LOCALE_INFO.fullmatch(path))
            or bool(EXCLUDE_WEB_ASSET_NOTES.fullmatch(path))
            or bool(EXCLUDE_EDITOR_CACHE.fullmatch(path)))


def check_unmodified_other_manifests(root: pathlib.Path) -> None:
    for filename, fixed_hash in OTHER_REFERENCE_SHA256.items():
        catalog = root / filename
        actual_hash = sha_file(catalog)
        if actual_hash != fixed_hash:
            fail(f"Unrelated {filename} changed: SHA-256 {actual_hash} != {fixed_hash}")
        # Validate semantic internal structure too, not just the file checksum.
        version = {
            "NativeReferenceFiles.json": "gorpg-1-23",
            "NativeReferenceFiles-maple-1-7-2.json": "maple-1-7-2",
            "NativeReferenceFiles-three-in-one-3-3-4.json": "three-in-one-3-3-4",
        }[filename]
        validate_manifest(catalog, version)


def selected_ch_root(config_path: pathlib.Path) -> pathlib.Path:
    settings = json.loads(config_path.read_text(encoding="utf-8-sig"))
    profiles = settings.get("Profiles") or []
    matches = [p for p in profiles if isinstance(p, dict) and
               p.get("Name", "").startswith("CH -") and p.get("Root")]
    if len(matches) != 1:
        fail("Expected exactly one registered CH original root in profiles.json; "
             "pass --ch-root explicitly if the local machine differs")
    return pathlib.Path(matches[0]["Root"])


def check_ch_layout(folder: pathlib.Path, present: dict[str, dict]) -> None:
    package = json.loads((folder / "package.json").read_text(encoding="utf-8-sig"))
    if package.get("main") != "www/index.html":
        fail("CH package.json does not point to www/index.html")
    web_package = json.loads((folder / "www" / "package.json").read_text(encoding="utf-8-sig"))
    if web_package.get("main") != "index.html":
        fail("CH www/package.json does not point to index.html")
    for relative in REQUIRED_NATIVE:
        if relative not in present:
            fail("CH recovery manifest lacks a launch-critical file: " + relative)
        if not (folder / relative).is_file():
            fail("CH original source lacks a launch-critical file: " + relative)
    # Every indexed Chromium *.pak.info file is auxiliary human-readable
    # build metadata; DO NOT drop actual locales/*.pak needed by Chromium.
    for relative in present:
        if EXCLUDE_LOCALE_INFO.fullmatch(relative):
            corresponding = relative[:-len(".info")]
            if corresponding not in present:
                fail("Locale .pak.info cannot be classified without matching runtime .pak: " + relative)
    packs = [key for key in present if key.startswith("locales/") and key.endswith(".pak")]
    if len(packs) != 53:
        fail(f"CH Chromium runtime locales/*.pak unexpectedly absent: {len(packs)}/53")


def check_file_records(
        folder: pathlib.Path, files: list[dict], full_hash: bool
) -> tuple[int, int]:
    """No directory copy/extraction. Read each recorded source file in-place."""
    missing = []
    wrong_sizes = []
    wrong_hashes = []
    verified = 0
    checked_bytes = 0
    for row in files:
        relative = row["Path"]
        original = folder / relative
        if not original.is_file():
            missing.append(relative)
            continue
        if original.is_symlink():
            fail("Linked CH original native source refused: " + relative)
        actual_size = original.stat().st_size
        if actual_size != row["Size"]:
            wrong_sizes.append(f"{relative} expected={row['Size']} actual={actual_size}")
            continue
        if full_hash:
            actual_hash = sha_file(original)
            if actual_hash != row["Hash"]:
                wrong_hashes.append(relative)
                continue
            checked_bytes += actual_size
        verified += 1
    if missing or wrong_sizes or wrong_hashes:
        fail("CH source incomplete or modified: "
             f"missing={len(missing)} size-mismatch={len(wrong_sizes)} "
             f"SHA256-mismatch={len(wrong_hashes)}. "
             f"First missing={missing[:6]}, wrong-size={wrong_sizes[:6]}, "
             f"wrong-hash={wrong_hashes[:6]}. "
             "Check false missing against removed tool/metadata records before release.")
    return verified, checked_bytes


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=pathlib.Path, default=PROJECT,
                        help="Launcher project holding 4 reference manifests")
    parser.add_argument("--ch-root", type=pathlib.Path, default=None,
                        help="Existing CH installation; never modified")
    parser.add_argument("--ch-manifest", type=pathlib.Path, default=None,
                        help="Regenerated CH manifest candidate; defaults to project CH catalog")
    parser.add_argument("--require-clean", action="store_true",
                        help="Fail if any of the 118 auxiliary/authoring records remain")
    parser.add_argument("--audit", action="store_true",
                        help="Show candidate cleanup against pre-clean original catalog")
    parser.add_argument("--quick", action="store_true",
                        help="Check every path/size but skip full multi-GB source hashing")
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    manifest = (args.ch_manifest or root / CH_MANIFEST).resolve(strict=True)
    config = root / "profiles.json"
    source = (args.ch_root or selected_ch_root(config)).resolve(strict=True)
    check_unmodified_other_manifests(root)
    records = validate_manifest(manifest, "ch-current")
    kept = [row for row in records if not is_external_tool_or_metadata(row["Path"])]
    excluded_now = [row for row in records if is_external_tool_or_metadata(row["Path"])]
    indexed = {row["Path"]: row for row in records}
    check_ch_layout(source, indexed)

    kept_digest = row_digest(kept)
    if kept_digest != EXPECTED_CLEAN_ROWS_SHA256 or len(kept) != EXPECTED_CLEAN_COUNT:
        fail("Regeneration altered native CH path/size/SHA-256 records beyond the 118 "
             f"allowlisted helper/metadata exclusions: retained={len(kept)}, "
             f"digest={kept_digest} expected={EXPECTED_CLEAN_ROWS_SHA256}")
    if len(records) == ORIGINAL_CH_COUNT:
        if (row_digest(records) != ORIGINAL_CH_ROWS_SHA256
                or len(excluded_now) != 118
                or row_digest(excluded_now) != EXPECTED_EXCLUDED_ROWS_SHA256):
            fail("Historical CH manifest no longer matches trusted pre-clean audit")
        phase = "PRE_CLEAN"
    elif len(records) == PROVISIONAL_COUNT and len(excluded_now) == 3:
        # Worker-1's first regeneration removed 115 records, with three
        # decompiler/MTool support DLLs still mistakenly required for
        # reconstructing CH. The exact digest prevents normalizing a modified
        # game catalog to this temporary intermediate state.
        if row_digest(records) != PROVISIONAL_ROWS_SHA256 or {
            x["Path"] for x in excluded_now
        } != {
            "Microsoft.WindowsAPICodePack.dll",
            "Microsoft.WindowsAPICodePack.Shell.dll",
            "Newtonsoft.Json.dll",
        }:
            fail("Provisional CH manifest differs from independently audited regeneration")
        phase = "PARTIAL_CLEAN"
    elif len(records) == EXPECTED_CLEAN_COUNT and not excluded_now:
        if row_digest(records) != EXPECTED_CLEAN_ROWS_SHA256:
            fail("Clean CH manifest content differs from approved retained records")
        phase = "CLEAN"
    else:
        fail("Partial/unreviewed CH cleanup: "
             f"{len(records)} entries and {len(excluded_now)} unexpected old tool/meta records; "
             "require exact historical, reviewed provisional or fully cleaned reference")
    if args.require_clean and phase != "CLEAN":
        fail(f"CH catalog still includes {len(excluded_now)} non-runtime records "
             f"(e.g. {[x['Path'] for x in excluded_now[:3]]}); "
             "Prime must remove these helper/authoring files first.")

    checked, amount = check_file_records(source, kept, full_hash=not args.quick)
    print(json.dumps({
        "status": "PASS_CLEAN" if phase == "CLEAN" else "AUDIT_PENDING_CLEANUP",
        "phase": phase,
        "version": "ch-current", "source_read_only": str(source),
        "manifest": str(manifest), "original_records": ORIGINAL_CH_COUNT,
        "retained_runtime_records": len(kept), "currently_excluded_from_manifest": 118-len(excluded_now),
        "non_runtime_still_indexed": len(excluded_now),
        "proposed_cleanup": 118, "proposed_cleanup_bytes": EXPECTED_CLEANUP_BYTES,
        "runtime_rows_sha256": kept_digest,
        "source_checked_records": checked,
        "source_sha256_hashed_bytes": amount,
        "source_hashes_checked": not args.quick,
        "other_reference_catalogs_unchanged": list(OTHER_REFERENCE_SHA256),
        "hard_requirements_present": sorted(REQUIRED_NATIVE),
        "remaining_false_missing_candidates": [row["Path"] for row in excluded_now],
        "note": "Non-native sidecars, MTool translation/decryption helper files, "
                "and Chrome *.pak.info source metadata are optional. "
                "Keep NW.js runtime, locale *.pak and www game/data/js/media unchanged.",
    }, ensure_ascii=True, indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (AssertionError, OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        print("CH_MANIFEST_QA_FAIL:", error, file=sys.stderr)
        sys.exit(1)
