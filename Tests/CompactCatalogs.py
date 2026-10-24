"""Losslessly deduplicate public English catalogs for legacy auto-update limits.

Each original catalog repeats its complete source as both the dict key and the
row's source field. Remove only that redundant copy. The application puts it
back in memory when exporting translations or reading source metadata.
"""
import hashlib
import base64
import gzip
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]


def serialized(data):
    return json.dumps(data, ensure_ascii=False, separators=(",", ":")).encode("utf-8")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    indexes = json.loads((ROOT / "version-fingerprints.json").read_text("utf-8-sig"))
    versions = [x["Id"] for x in indexes["Versions"] if x["Id"] != "ch-current"
                and (ROOT / "English" / x["Id"] / "catalog.json").is_file()]
    assert len(versions) >= 10, "Ten historical/current English packs must remain available"
    prior = ROOT / "Tests/catalog-compact-proof.json"
    previous = json.loads(prior.read_text("utf-8")) if prior.is_file() else {}
    results = {}
    for ident in versions:
        folder = ROOT / "English" / ident
        cat_file = folder / "catalog.json"
        manifest_file = folder / "manifest.json"
        data = cat_file.read_bytes()
        rows = json.loads(data.decode("utf-8-sig"))
        if rows.get('format')=='cph-catalog-gzip-v1':
            decoded=gzip.decompress(base64.b64decode(rows['data'],validate=True));assert sha(decoded)==rows['sha256'];rows=json.loads(decoded)
        manifest = json.loads(manifest_file.read_text("utf-8-sig"))
        assert manifest["TotalTexts"] == len(rows)
        assert all(isinstance(row, dict) and set(row) <= {"source", "kinds", "contexts"}
                   for row in rows.values())
        assert all(row.get("source", key) == key for key, row in rows.items())

        compact = {key: {field: value for field, value in row.items()
                         if field != "source"} for key, row in rows.items()}
        full = {key: {"source": key, **row} for key, row in compact.items()}
        raw_compact = serialized(compact)
        compact_bytes = serialized({'format':'cph-catalog-gzip-v1','sha256':sha(raw_compact),'data':base64.b64encode(gzip.compress(raw_compact,compresslevel=9,mtime=0)).decode('ascii')})
        assert json.loads(gzip.decompress(base64.b64decode(json.loads(compact_bytes)['data'])))==compact
        rebuilt_full = serialized(full)
        original_digest = sha(rebuilt_full)
        if ident in previous:
            assert previous[ident]["fullSha256"] == original_digest, (
                "Full catalog metadata would change: " + ident)
        else:
            assert data == rebuilt_full, "Original catalog is not byte-exact canonical JSON"
        assert len(compact_bytes) < len(rebuilt_full)
        if data != compact_bytes:
            temp = cat_file.with_name(cat_file.name + ".compact-new")
            assert not temp.exists()
            temp.write_bytes(compact_bytes)
            temp.replace(cat_file)
        digest = sha(compact_bytes)
        if manifest["CatalogHash"] != digest:
            manifest["CatalogHash"] = digest
            temp = manifest_file.with_name(manifest_file.name + ".compact-new")
            assert not temp.exists()
            temp.write_bytes(serialized(manifest))
            temp.replace(manifest_file)
        results[ident] = {
            "sourceKeysPreserved": len(rows),
            "fullSha256": original_digest,
            "compactSha256": digest,
            "originalBytes": len(rebuilt_full),
            "portableBytes": len(compact_bytes),
            "losslessRecoveredOriginal": sha(rebuilt_full) == original_digest,
        }
    if prior.is_file():
        assert all(results[ident]['fullSha256']==proof['fullSha256'] and results[ident]['sourceKeysPreserved']==proof['sourceKeysPreserved'] for ident, proof in previous.items()), (
            "Rebuilt catalog deduplication changed previously verified metadata")
    prior.write_bytes(serialized(results))
    print(json.dumps({
        "status": "PASS", "versions": len(results),
        "sourceKeys": sum(x["sourceKeysPreserved"] for x in results.values()),
        "uncompressedBytesSaved": sum(x["originalBytes"] - x["portableBytes"]
                                      for x in results.values()),
        "catalogsLosslesslyReconstructed": True,
    }, indent=2))


if __name__ == "__main__":
    main()
