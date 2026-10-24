# Ver1.06 / Ver1.061 source and compatibility trace

Both supplied files have RAR5 signatures despite their unusual suffixes. Each is a content-only overlay with 5,171 files and no standalone package.json/game executable. Both contain every path from the previously supported 1.053 overlay, plus eight additional files. The native target inventory contains 11,738 files.

## Identity

The newer download is named 1.061 and its bundled developer changelog ends at Ver1.061. Its internal folder is labeled 1.062. The folder label is retained as source evidence and is not treated as another supported release. Both new downloads share System.json, so System.json alone cannot identify them. Complete recipe hashes and fingerprints including CommonEvents.json and Map002.json distinguish them.

Between the two downloads, only CommonEvents.json, Map002.json and the creator changelog differ. The 24 newly introduced display strings compared with 1.053 are identical between 1.06 and 1.061.

## Reconstruction and upgrades

The native target extends the verified previous inventory with the complete supplied overlay. Every non-overlay dependency matches the original GoRPG inventory, so reconstruction does not require private bytes from an old Three-in-One installation. The normal shared pool and exact size/SHA-256 checks handle direct base-plus-latest installation and mixed archives/folders/individual files.

Save generations 1060 and 1061 follow 1053. Registered forward Three-in-One 3.3.5 routes remain explicit; reverse and unrelated-branch save transfers stay closed. CH/Maple/GoRPG files may contribute identical game bytes without importing their saves into another branch. Upgrade backups and original profiles are retained.

Loose original files are checked as complete expected size/hash matches before interpreting archive signatures. This matters for native DOCX files: they are ZIP containers internally, but the full document is also a required native file. Unmatched archives still follow the existing bounded inventory/extraction path.

The native and optional runtime-control save branches migrate independently, with verified backups. Existing control saves are not overwritten with normal saves. The control checkbox and panel-settings.json are retained; runtime caches and engine copies are regenerated for the new profile.

Launcher 1.0.27 also resolves legacy control-session IDs when shared discovery assigned a new profile ID. A unique seed source path may identify the existing session; multiple matches fail explicitly. The normal-save set and each launcher's independent control-save set remain separate.

## English

The new source inventory contains 24 new display keys, including choice help, status labels/descriptions and two script-display notifications. Their reviewed translations are in English/v106-reviewed-overrides.json and are checked against the native source/control codes. Existing reviewed translations and runtime aliases are reused by the established pack builder; the overall English coverage remains partial.

The five localized native artwork sources and unchanged Map005 data retain their previous hashes. No translated name or description is written into native gameplay data. The existing normal-launch English refresh applies the new version-specific pack without requiring runtime controls.

All 13 portable text catalogs use lossless gzip/base64 JSON envelopes to stay below existing updaters' 180 MiB expanded-package limit. Decompression restores identical source keys, kinds and contexts, with a 32 MiB per-catalog expansion bound and SHA-256 verification. Plain catalogs remain supported. Existing dictionary bytes are unchanged; only catalog storage and manifest hashes change. Translation export/import still works with full reconstructed metadata.

## Validation scope

Acceptance requires exact native reconstruction, forward route and branch-boundary checks, source discovery, partial/individual source recovery and missing-file rejection, save hash preservation, uninstall preview/rollback boundaries, normal/control save routing, native English startup/render checks and final package/public ZIP verification. These checks do not establish an exhaustive playthrough of every new event or cross-version save state.
