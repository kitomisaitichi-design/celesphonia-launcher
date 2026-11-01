# Launcher 1.0.35 — Recover upgrades blocked by an older source map

Fixes Source creator file changed or missing: data/Map027.json blocking a new Ver1.061 reconstruction even when every correct target file is available. Both source-method buttons can use the complete verified target and carry compatible backed-up saves independently of the older map mismatch. Source games/saves are preserved; no map is silently accepted or edited.

## Before and after

| Case | Before | After |
| --- | --- | --- |
| Complete originals plus older source with mismatched Map027 | Upgrade rejected on old source | Reconstructs all 11,738 exact target files and carries compatible saves |
| Conversion without complete originals and with damaged required source bytes | Rejects unsafe source | Still rejects; strict checks retained |
| Missing target bytes, unknown/cross-branch saves, downgrade, running source, unsafe destination | Invalid | Rejected |

Verified with 9 baseline reproduction checks, 22 corrected reconstruction/routing checks, 9 additional boundaries and required package/update/restore checks. The genuine private older-game fixture is 1.053, sharing the reported 1.052 guard/map hash; the reporter's exact files and a full gameplay replay are not claimed. Runtime controls are optional; translation runtime/dictionaries are unchanged.

[Source trace and diagnostic evidence](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.35/docs/map027-upgrade-1.0.35-trace.md)
