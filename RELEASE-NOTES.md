# Launcher 1.0.37 — Correct split-ZIP installation

Fixes `.zip.001` dispatching to single-file ZIP parsing even when all continuation volumes are present. Keep every part together with its original matching name and select `.001`; `.002` is consumed automatically. Do not extract `.002` separately. Incomplete/corrupt inputs still fail; no missing game files are accepted or fabricated.

| Case | Before | After |
| --- | --- | --- |
| Valid sample.zip.001 + .002 | End of Central Directory record could not be found | Exact 6,000-byte payload recovered |
| Missing, corrupt or truncated second volume | Cannot build complete target | Still rejected, with first-volume guidance on extractor failure |
| Ordinary/mislabelled ZIP, split 7z, RAR, loose native fragment | Supported | Preserved |
| Unsafe paths, case-colliding entries, symbolic links | Rejected | Rejected on the new route too |

Grenim's exact volume names and launcher version are unknown. Their missing-media report does not by itself prove split ZIP was the cause. This release fixes an independently reproduced current bug. Existing games, saves, translation dictionaries and runtime controls are unchanged.

[Source trace and verification limits](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.37/docs/split-archive-1.0.37-trace.md)
