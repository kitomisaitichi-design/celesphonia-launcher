# Celesphonia: any starting version, original files, and separate saves

## Fresh clean GoRPG 1.23 ZIP + Three-in-One 3.3.5 Ver1.052

Two downloads are sufficient: the complete clean GoRPG 1.23 ZIP or
complete split 7z archive and the creator's full Ver1.052 RAR mod bundle.
Select **Install > Three-in-One 3.3.5 Ver1.052 > Use the original bundle /
game > Install fresh mod + English**. The launcher can use a verified
registered clean GoRPG 1.23, or extract the original full GoRPG archive;
it checks the clean base and complete creator bundle, makes a separate
game, and automatically enables that version's English display overlay.

Verified with a freshly generated pristine GoRPG ZIP containing 7,210
files (3,040,989,656 bytes) and the actual creator Ver1.052 RAR
(547,645,049 bytes; SHA-256
`5dc382a21ed8db8d5597dbabefa6b82ff243937b51199f5b66e45b4854b7e1a2`).
The installed game identified as Ver1.052, all creator files and native
integrity checks passed, the English overlay was enabled (34,996 of
56,080 catalog entries), and no saves were inherited. No preexisting
game or save was overwritten. This establishes the full file-install
chain, not completion of every gameplay route.

Forward upgrades from registered v1.04 or v1.051 remain available to
carry saves; fresh clean-GoRPG installation starts with empty saves.

## Creator v1.03 RAR → v1.04 forward compatibility

The original v1.03 author archive (547,643,719 bytes, SHA-256
`90c6aaad120a695b5dcc7a2fc8ab5e8e1dcb99fae07fb2987a74e60a70214ab7`)
has been detected correctly despite its `r删ar` filename extension, and all
5,163 overlay content file fingerprints passed direct native verification.
The upgrade recipe permits an **installed v1.03** to produce an independent
v1.04 installation with save backup/copy and checks. The developer RAR alone
contains an *overlay*, not the entire playable game. A separate installed
v1.03 profile is required to perform that particular route; v1.04 can also
upgrade directly from registered v1.02, v1.01 and trial installations.

The end-to-end route was exercised against the actual v1.03 and v1.04 author
RARs in independently created temporary installations: all 11 synthetic save
files preserved their SHA-256 hashes, v1.04's separate English overlay was
enabled, developer file hashes passed, and reverse save migration was rejected.
No existing game was registered or changed, and full gameplay was not run.

Renamed ZIP and RAR files can be recognized by their actual signatures and
version-content fingerprints, even with arbitrary `.blob` filenames.
RAR contents are also fingerprinted when the archive has been repacked and
its original whole-archive size/checksum no longer match. Extracted folders
are recognized by their version files. All 5,163 developer overlay files
must still pass full verification before a v1.04 installation.

The archive extractor uses a short, uniquely owned Windows temp directory
when preparing upgrades, fixing extraction failures on legacy `MAX_PATH`
systems. Previous installations and source archives are not overwritten.

## Three-in-one 3.3.5 v1.04 (October 10, 2026)

- **Verified creator release:** the Telegram Desktop RAR whose name ends
  `体验版1_04_r删ar`, 547,633,402 bytes, SHA-256
  `9fd713c92af378134f4b957a38704f7a890d324e83e87bac462e2934a5fdedfd`.
  Recognition uses the actual RAR signature, archive checksum, and five in-game
  marker hashes, not the file extension or filename alone.
- **Safe forward upgrades:** a registered 3.3.5 trial, v1.01, v1.02 or v1.03
  can produce a separately verified v1.04 installation. All 5,163 overlay
  files are checked against the exact creator bundle. Existing originals,
  save folders, English settings and older installations remain intact.
  Selected older saves are backed up, copied and hash-verified before new
  profile registration. This is not a conversion of in-game quest/script state.
- **Preserve developer fixes:** v1.04 changes exactly 12 files relative to
  v1.03 (nine `data/*.json` and three `js/*.js`), covering shops, equipment,
  enemy visibility/skills, experience display, the photography-club route
  and recollection/transform options. Upgrading overlays the *complete*
  verified v1.04 bundle; manually copying old v1.03 JSON/plugins afterward
  would reintroduce previous bugs.
- **Runtime controls:** F8 opens the existing launcher controls in an isolated
  runtime session. Gameplay and saves stay in their original versioned folders;
  no v1.03 hardcoded offsets are reused. The hidden-on-startup controls fix
  remains active.
- **Version-specific English:** the v1.04 display pack includes 34,482 exact,
  control-code-compatible translations. All 20 source keys newly introduced
  since v1.03 are translated (18 newly translated, 2 already covered). Another
  21,079 previously untranslated source strings remain in the original language.
  Game mechanics, event IDs and scripts are never rewritten for translation.
- **Downgrades:** older installations can still be selected and played with
  their own saves. Newer saves cannot be copied or restored *backward* into
  older 3.3.5 versions. Keep a backup before testing new dialogue or events.
- **Future developer releases:** an unknown v1.05+ bundle stays unverified
  rather than being misidentified as trial or an older update. Its files and
  checksum must be registered before it is offered as an upgrade.

## 3.3.5 v1.01: forward upgrade with saves

Select **Three-in-one 3.3.5 v1.01** in Files or Install. The launcher recognizes the
creator's complete update archive by its byte signature and SHA-256 hash, including
the downloaded filename ending in `r删ar`. Renamed ZIP overlays and extracted
folders are checked by their structure and version markers; every update content
file is checked again before installation.

The upgrade starts from the registered **3.3.5 trial** and creates a separate verified
game folder. Trial saves are backed up, copied and verified byte for byte. The original
trial and its saves stay intact. Enabled English carries forward using the v1.01 pack's
exact source-key matches. Save copying is not a conversion of the game's internal state.

Migration into v1.01 accepts the trial or the same version's backups. Newer saves
cannot be copied/restored into older versions or a separate branch. Backups now
record their source version so the restore path follows the same rule. The launcher
also blocks using v1.01 as a reconstruction source for older targets. Existing older
installations remain independently playable with their own saves.

**3.3.4 stays separate on clean Gorpg** with its Abu 56.5, hypnosis 2.4.12 and skills
3.3.4 components. It is not mixed into the 3.3.5 story branch. The existing any-version
pack retains its original five-version routes; v1.01 uses the verified creator update.

The new **Celesphonia-Any-Version.cgdelta.zip** supports all 20 directed routes among
the five reference builds. Any row below can be your only installed starting game.
Each reconstruction produces a complete independent target; existing versions can
be selected directly. The four older single-target packs still require matching CH.

| Starting installation | CH | Gorpg 1.23 | Maple 1.7.2 | 3.3.4 | 3.3.5 trial |
|---|---|---|---|---|---|
| CH | Use existing | Reconstruct | Reconstruct | Reconstruct | Reconstruct |
| Gorpg 1.23 | Reconstruct | Use existing | Reconstruct | Reconstruct | Reconstruct |
| Maple 1.7.2 | Reconstruct | Reconstruct | Use existing | Reconstruct | Reconstruct |
| Three-in-one 3.3.4 | Reconstruct | Reconstruct | Reconstruct | Use existing | Reconstruct |
| Three-in-one 3.3.5 trial | Reconstruct | Reconstruct | Reconstruct | Reconstruct | Use existing |

Recognition combines `package.json`, game executable/layout, and SHA-256 fingerprints
of `data/System.json`, `js/plugins.js`, and `js/rpg_managers.js`. The reconstruction
preflight separately checks every source file reused by the selected route.
Folder names alone do not establish compatibility. Builds with different marker
files are not assumed equivalent, even if they use the same public version label.
Every reconstructed file must match its recorded target hash before registration.
The pack's authoritative per-file plans are in `routes.json`; `index.json` is the
small five-version discovery index. No save files or absolute user paths are included.

## Original archives and overlay ordering

| Available original files | Correct action |
|---|---|
| A complete CH / Gorpg / Maple / three-in-one game folder | Register the game or its wrapper folder; keep existing saves |
| A complete game's ZIP, RAR, or 7z archive | Install/extract and register its actual detected version |
| Gorpg split `.7z.001` and `.002` | Keep both together; extract `.001` |
| Maple 1.7.2 full archive | Install as a standalone game; never apply as a Gorpg overlay |
| Three-in-one 3.3.4 content bundle | Private copy of clean Gorpg + Abu 56.5 / hypnosis 2.4.12 / skills 3.3.4 bundle |
| Three-in-one 3.3.5 trial content bundle | Private copy of clean Gorpg + Abu 56.6 / story 1.0 / skills 3.3.5 trial bundle |
| Separate author-supplied hotfixes | Reviewed complete bundle first, relevant hotfixes afterward |

The downloaded 3.3.5 archive is a content overlay, not a full game. Its bundled
components are already combined. Do not apply it over CH, Maple, 3.3.4, or standalone
Abu. If clean Gorpg is missing, reconstruct it from any supported starting game or
install its original archive first. `1.23`, `56.6`, `1.0`, and `3.3.5` name different
components; they are not one incremental upgrade sequence.

**Bundle + hotfixes** copies the clean base into a private folder, then applies
reviewed layers in order. Later files overwrite earlier files, without automatic
JSON/JavaScript merging. Known incompatible bases, multiple complete bundles, and
bundles after hotfixes are rejected. Unknown hotfixes remain custom/unverified.
`launcher-build.json` records layer order and final hashes. `launcher-reconstruction.json`
records source/target version IDs and output hashes for any-version reconstructions.
Completed copies do not depend on the original base path being available.

## Files view and automatic discovery

**Files** compares all five versions with available installed games, original files,
and patch packs. Green rows can switch now; lavender rows have a preparation route;
amber rows still need files. Selecting a row opens the existing game in Play or the
appropriate guide in Install. The guide updates its diagram and checklist dynamically.
**View detected files** lists source paths and missing split volumes. **Search another
folder** remembers another scan root; **Find files** rescans after moving files.

Searches cover the launcher directory and subfolders, registered game locations and
their parents, installation location, Windows Documents/Desktop/Downloads, Telegram
Desktop, and local Chrome/Edge/Brave/Chromium/Vivaldi/Opera/Firefox download settings.
Browser history and credentials are not read. More specific roots are searched first
to avoid losing a direct location to an ancestor's depth limit. Each root has a
five-level / 3,500-folder scan limit. Linked folders, game assets, saves, backups and
runtime caches are skipped; cloud-only archives must be made local first.

ZIP files can be recognized from contents even after renaming. Full-game ZIP discovery
checks the wrapper/runtime structure and three version files. That match identifies a
version, not the completeness of every asset. Named RAR/7z files are suggestions until
their known hash or extracted contents are checked. Split volumes are grouped and
missing parts displayed. Corrupt ZIPs are skipped. Unknown related sources stay
unverified. Original archives/folders are usable without delta packs; a partial
overlay still needs its correct base. Detection never patches files or imports saves.

## Switching back and saves

Close the active game, select CH or another version, and press **Switch & Play**.
Each returns to its own game folder, runtime data and saves. Launches create a save
backup. **Copy saves from...** preserves the source and backs up the destination.
**Restore backup** verifies its hashes and backs up current progress before restoring.
These operations do not convert save contents or guarantee gameplay compatibility.

The supplied story 1.0 changelog removes the hypnosis storyline. A 3.3.4 save on that
route may behave differently in 3.3.5. Newer saves can reference state absent from
older versions. Keep each branch's original saves. The skills changelog mentions
automatic saving to slot 1, so maintain backups when experimenting with transfers.

## Portable distribution and verification

Share the clean **Celesphonia-Delta-Launcher.zip** and the separate approximately
7.00 GB any-version pack, or use original game files. The launcher ZIP excludes
game assets, personal saves, local profile configuration and runtime caches.
The all-version pack needs more data than a CH-only pack because it also reconstructs
CH and supports different encrypted/plain asset sets in every direction.

Native ZIP reconstruction/extraction requires no separate extractor. RAR uses Windows
tar where available. Split 7z or other unsupported archives can be extracted manually
and their folders selected. The local 7z helper is not included in the clean tool ZIP.
Future installation location can be changed without moving existing profiles.

All 20 directions passed **207,196 target-file SHA-256 checks**. A fresh 3.3.5 trial
reconstructed from Maple reached `Scene_Title` with no recorded startup errors and
its own save folder. Detection, modified-source rejection, path checks, empty files,
root/www layout reconstruction, and four non-CH starting-version UI cases passed.
See `TEST RESULTS.txt` and the route verification report. These checks do not establish
full gameplay or automatic cross-version save migration.

## Evidence used

These are findings from supplied documents and local archive contents. The messages were treated as evidence for dependencies, not as commands to run.

- Supplied Discord excerpt, lines 42–44 and 72–76: Chinese 1.23 base, both volumes together, then the approximately 500 MB combined trial; matching folders are copied and overwritten. Earlier messages distinguish Maple and three-in-one developers.
- Supplied Telegram excerpt dated October 1–2, 2026, lines 8–9: 1.23 is the game version and 56.6 is the Abu component. Lines 78–82 distinguish the two 56.6 packages; lines 102–105 specify overwriting inside `www`.
- The overlapping Telegram excerpt and bundled `剧情增强更新日志Ver1.0.txt` agree that the hypnosis storyline was removed.
- The downloaded archive contains `此版本只能在Gorpg本体用.txt` and content folders, without a game executable. Its System file has Gorpg's encryption settings. Its enabled SavePathRelocation plugin places saves beside the executable.
- Archive SHA-256: `90622feb8376e336becae31d793ed84fd8f39509c4a936382447e2663269e24e`.

The supplied trial is distinct from the fuller testing release mentioned in Telegram. This launcher keeps the trial label. Title-screen startup and file reconstruction checks do not establish complete gameplay or save migration compatibility.
