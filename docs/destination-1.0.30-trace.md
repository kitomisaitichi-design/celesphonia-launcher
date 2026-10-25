# Original-source destination conflict, launcher 1.0.30

The recipient's Ver1.061/GoRPG error was reached after the complete source-file audit. `Store.Home` is the launcher's executable directory. `Store.Read` supplied `Home/Game Profiles` when no install root was configured. If the launcher was extracted inside an original game, this new destination also sat inside that source. `MixedSourceRecovery.Recover` correctly rejected it. Adding the same source files again could not resolve the conflict.

## Shared policy and scope

`InstallDestination` in `MixedSourceRecovery.cs` normalizes paths, checks containment with separator boundaries and compares all existing source folders. Archive/individual file inputs remain files, not fabricated folder containers. Linked destination ancestors and file-valued destinations are rejected. Drive-root source containment is retained.

Legacy settings with no selection flag treat the original beside-launcher `Game Profiles` value as automatic. A nullable `InstallRootUserSelected` field records future choices. Unsafe automatic defaults are replaced with a validated external suggestion, including an already saved default from an older release. Other legacy custom paths remain custom. Suggestions create no directories and do not move games or saves.

The Install page repairs the automatic preview when sources become known and repeats preflight after route-specific source selection. An unsafe custom choice opens the dedicated folder picker, which allows creating a folder and validates before acceptance. Cancellation leaves the settings intact and starts no install. Valid explicit locations remain unchanged.

Before this release was tagged, the user corrected MD back to MDF. `EnglishRuntime.js` is restored byte-for-byte to the 1.0.28 runtime (SHA256 2b93c9e081e7d4131e6b1e08008e08a499afec9228dcb7f2aca92b2f8df10876), retaining all equipment layout fixes. Both prior native equipment reports match this exact runtime, and every dictionary/catalog remains unchanged. The MDF comparison reuses those exact native runtime captures; the native gameplay/equipment sweep was not repeated for identical bytes. Runtime-control code and native game mechanics are unchanged.

The supplied follow-up screenshot identified a separate stale Ver1.053 literal in `PolishedLauncher.CheckLauncherUpdate`. The native notice now calls `LauncherUpdate.CurrentVersionMessage`, which reads `UpgradeSupport.LatestName` from the same recipe catalog as the upgrade target. Focused checks confirm Ver1.061 and temporarily add/remove a higher-generation in-memory fixture recipe to prove future catalog updates change the notice. No historical changelog/version IDs were rewritten.

Independent recovery, creator upgrades, mixed GoRPG recovery, layered mods and legacy reconstruction also enforce the shared source check before creating their installation parent. The original lexical recovery guard remains mandatory even if a source disappears after probing. Original source/hash rules, version detection, reconstruction data, save migration, runtime controls and English dictionaries are unchanged. This does not relocate existing profile roots or save paths. Launcher-owned configuration/cache activity remains separate from installation into source game data.

## Reproduction and evidence

`Tests/InstallDestinationTests.cs` loads the exact supplied assembly from a private launcher directory inside a synthetic Original GoRPG folder. It uses the real native recovery implementation with a complete two-file SHA-256 reference and the real `PolishedLauncher` Install page in preview mode. No user game/save is loaded or written, and no full game tree is copied.

The frozen 1.0.29 baseline reaches the reported guard despite the complete report and creates an empty Game Profiles parent before rejection. The new build rejects an unsafe destination before that directory is created. Its Install-page default moves outside the source and the tiny reconstruction succeeds with identical output hashes. Before/after PNGs are actual native control captures using the same source/layout.

Focused cases cover old saved defaults, repeat resolution, valid custom destinations, custom-choice cancellation, existing profile/save paths, case/trailing separators, prefix siblings, parent library folders, whole-drive sources, broad recursive folders, multiple contributors, file/archive distinctions and existing destination rejection. Source bytes are unchanged.

One reflection harness attempt selected an ambiguous overload and was corrected by parameter-type matching. The cancellation assertion then failed because JSON property ordering differed while decoded settings were identical; the diagnostic was retained and the re-plan compares every decoded key/value. No product error was hidden. Final receipts distinguish these path/recovery/UI checks from full-game installation and gameplay. Performance work is separate and is not claimed by this release.
