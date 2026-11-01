# Map027 upgrade blocker — before/after and source trace

## 1.0.36 manual-picker completion

Final review found one additional entrance: a creator archive first chosen through PickBundle after the initial pool check was not included in that proof. The legacy path could then reach BestCompatibleSource and reject the older map. The picker result now joins fresh exact-target checks before that source audit. A path-only reuse shortcut was rejected during review because a user can replace a download under the same filename while the picker is open.

Six compiled checks exercise the actual new planning helper with a source-exact Map027 subset: unsuitable base bytes; the original creator archive; a same-path/same-size/same-timestamp replacement that requires fresh hashing; duplicate Windows paths; cancellation; empty selection. Original source bytes remain unchanged. This is a one-file planner fixture; the installer still requires the complete embedded target inventory and rejects subset proofs, as checked by the full reconstruction tests below. The verified-target installer, save-donor validation and strict native-source verifier are source-identical to 1.0.35. Those 31 checks are reused; the complete game copy is not repeated for this input-event change.

The 1.0.35 tag remains immutable; 1.0.36 is the completed follow-up. Neither release changes translation dictionaries or game data.

Baseline: launcher 1.0.34, source 1b155e81dafb80992a2a1d2f7c5ab7e0072101d8. The reporter's screenshots show a Ver1.052 installed source failing `Source creator file changed or missing: data/Map027.json`, including under creator-bundle recovery. The earlier successful single-file target probe did not cover old-source selection or save migration.

## Root cause

`UpgradeSupport.InstallFromOriginalFiles` first proved every byte of the final target was available, then called `VerifyKnownCreatorSource` on the old save donor. That second full source audit rejected its map despite the independent new target proof. `BestCompatibleSource` raised the same source-integrity error on the conversion path before a clean-source fallback. Adding the latest original creator archive could therefore still leave the user stuck.

Both 1.052 and 1.061 expect Map027 SHA256 `34a1186806b931e247b26e38854a7ce48e4dc77481f3cd7b9fcf2d5aaab7f130` (75,206 bytes). GoRPG 1.23's map is a different 74,486-byte file. A valid base map does not repair a modded source. The reporter's actual mismatched bytes/cause remain unavailable.

## Matching private diagnostic results

The local 1.052 profile has retained state but no playable package/native tree. The fixture therefore uses the complete genuine 1.053 source, the same guard/map hash, a private copy of text/code/data and hardlinked immutable media. No user save is copied into the fixture. Native source identity and intact full verification pass before substituting only its private Map027 with the base version.

| Case | 1.0.34 before | 1.0.35 after |
| --- | --- | --- |
| All 11,738 target files available; older map mismatched | Creator path rejects old Map027 | Independent exact target constructed; compatible synthetic save copied byte-for-byte |
| Conversion source-method decision with complete original proof | Old-source audit stops selection | Uses the same verified target-recovery path |
| Creator-bundle source-method decision | Old-source audit blocks save carry | Uses the same verified target-recovery path |
| Conversion requiring unverified old bytes without complete originals | Rejects source | Still uses strict source validation |
| Wrong/missing final target map, or one-file proof pretending to be complete | Incomplete target | Rejected before destination creation |
| Unknown/different branch, save downgrade, running source, source-overlapping destination | Rejected | Rejected |

The older fixture's strict `VerifyKnownCreatorSource` continues to reject the mismatched map after the patch. This method was not weakened. Source data and user files are never repaired in place by this workflow.

## Implementation and proof

The public original-file installer and the UI share an internal verified-plan completion method. It validates the cached plan's full paths/sizes/hashes against the embedded authoritative target inventory, and recovery rehashes the copied bytes. The compatible save donor must have a recognized version, allowed branch/generation, safe save paths and a closed game, followed by verified backup/copy. It is not labeled an intact native source. Optional legacy control saves/preferences use the existing migration method.

The UI evaluates complete target-original availability for both source methods. If no complete proof exists and the user has not explicitly requested originals, the conversion path retains its prior strict behavior. Explicit original-file requests keep precise missing-target diagnostics. The preflight plan is reused, avoiding a second full source scan/hash pass.

Baseline checks: **9**. Corrected reconstruction/routing checks: **22**. Additional donor/source/destination/runtime-preference boundaries: **9**. The installed private target passes all **11,738 authoritative native file hashes**. Tests load the compiled launcher in a private home, do not register fixture profiles in the user's library, and hash-check the original source map. `Tests/Map027UpgradeRecoveryTests.cs` and `Tests/Map027RecoveryBoundaries.cs` contain the focused harnesses.

The existing **36 forward-route combinations** and **5 source-automation cases** also pass against the candidate, including reverse/cross-branch boundaries. The final release uses the same tested upgrade/routing source; subsequent changes are the release version and documentation resources. The English runtime and dictionaries were not changed or retested as new translation work.

Limits: the full reconstruction/source-copy path is exercised; the two UI routing choices are checked through their production decision helper, not a complete mouse-driven replay of both dialogs. The reporter's exact 1.052 files were not supplied. No gameplay replay, new translation coverage or unrelated game fix is claimed. Existing English runtime/dictionaries remain unchanged. Required packaging/update/restore and installed/public release checks are recorded in the release-specific local receipt.
