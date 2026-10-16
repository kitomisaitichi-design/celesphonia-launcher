# Celesphonia Portable Launcher

Portable Windows launcher for supported Celesphonia mod releases. It detects compatible installed game folders and renamed ZIP/RAR sources using version-specific internal fingerprints; supports verified mod upgrades and save preservation; provides version-specific reversible English overlays and optional F8 runtime controls.

**Launcher version 1.0.8** supports **creator Ver1.051 and Ver1.052** with RAR5 source detection, verified forward upgrades, reversible English translations, and illustrated glossary/tutorial localization. Launcher and game-mod versions are independently versioned.

## v1.0.8 — Preserve the original bedroom wardrobe

The bedroom wardrobe uses the same native **single-column, three-tile (1×3)** graphic in v1.04, Ver1.051, and Ver1.052. A proposed two-column rendering reconstruction was wrong and has been excluded; the English runtime does not override the native tile renderer or reposition the wardrobe, and both original interaction points remain untouched. Automated source/renderer checks now enforce these invariants. **All five translated help/glossary/hypnosis images stay enabled**, and the publisher's standard 35-file auto-update ZIP layout is unchanged.

## New in launcher v1.0.7 — Illustrated English help and improved text fitting

- Localizes five source-verified native picture/animation assets used in combat-restraint help, glossary gear examples, Change Color control help and hypnosis animation captions. The installed English layer redirects only the verified original asset names, preserving native files and saves and fully reversing when English is disabled.
- Fixes the groper/aged-man dialogue screenshot and matching events: the native **Ticker** plugin draws its speech through a separate variable/escape-code path, which previously bypassed the normal English window translator. v1.0.7 translates reviewed exact source entries temporarily for the ticker's local display only; saved variables, speakers, numbers and gameplay are unchanged.
- Improves help and equipment-description wrapping for adjacent English effect tags (such as `[Effect A][Effect B]`) and native icon widths. The dedicated presentation sweep checked 2,826 cases, improving 328 without changing wording or regressing a previously fitting case.
- The localized image payload is embedded in the launcher executable, rather than added as extra ZIP entries. The public ZIP retains the historical **35-file allowlist** required for older launchers' GitHub auto-update compatibility.

The original game/mod archives are not redistributed. Artwork replacements only activate when their native source hashes match an approved Ver1.051 or Ver1.052 release.

## Current maintenance priorities
Going forward, prioritize **the two most recent Three-in-One 3.3.5 releases (currently Ver1.051 and Ver1.052)** and **new Maple releases** for development, translation maintenance, newly added content, regression tests, and upgrade work. Advance this two-version window as new creator releases arrive. Treat **Gorpg 1.23 and the CH English reference** as stable source/reference installations unless their content actually changes.

Older Three-in-One 3.3.5 trial, v1.01 and v1.02 remain **supported for detection, English overlays, older saves and historical forward-upgrade routes**. This maintenance priority does not remove their code, recipes, dictionaries, developer verification fixtures, or backwards-compatibility tests. The old full game installations may be uninstalled locally to reclaim disk space, without affecting independent v1.03/v1.04 installations. Three-in-One 3.3.4 also remains a supported distinct branch.

## New: Creator Ver1.052 and Ver1.051 (launcher 1.0.6)
Supports installing **either Ver1.051 or Ver1.052 from a verified v1.04 game**, plus a direct forward **Ver1.051 → Ver1.052** upgrade. The launcher verifies the exact creator RAR5 archive and 5,163 gameplay files, creates a separate installation and carries compatible saves forward only. Neither older game nor its original saves are overwritten. The integrated RAR5 extractor handles Chinese filenames without an external utility.

**Ver1.052 fixes boss-unit restraint effects.** Its payload differs from Ver1.051 only in `data/Enemies.json`; the remaining 5,162 files are identical. Both versions include a different enemy-data hash in their native fingerprint, preventing misidentification even though the creator's Ver1.052 RAR still contains a folder labeled Ver1.051.

The dedicated Ver1.051 English pack contains 34,996 translated display entries: 44 newly reviewed creator-source translations, nine repaired Skill Engram description keys, six stripped-condition menu captions, and compatible historical runtime display fallbacks. Its English overlay remains reversible and does not edit native gameplay scripts, source identifiers or saved variables. Original creator game archives and copyrighted assets are **not** included in the GitHub launcher ZIP.

The separate **Ver1.052 English pack carries forward all 34,996 Ver1.051 translations**, including the nine Skill Engram line fixes and conditional-choice labels. Both English packs use source-verified display-only translations.

The companion [Ver1.04 → Ver1.051 source diff and regression audit](RELEASE-V1051-DIFF-AND-REGRESSION.txt) identifies 18 changed gameplay files, changes #1–8 and #203–206, additional skill/state balance differences and remaining in-game test limitations. The newer creator release cannot be declared gameplay-regression-free solely from static comparison. Historical v1.03 and older support remains available alongside the rolling two-release maintenance focus.

The [Ver1.051 → Ver1.052 full source diff and boss-restraint regression audit](RELEASE-V1052-DIFF-AND-REGRESSION.txt) contains hashes for the exact creator RARs, 22 altered enemy records, and a behavioral regression watchlist. Creator gameplay files and original archives are not redistributed.

The bundled RAR5 helper uses [7-Zip](https://www.7-zip.org/) 26.02 components (Copyright Igor Pavlov); license and notices are included in `Vendor/SevenZip/License.txt`.

## v1.0.5 English regression fix
The game-mod v1.04 English pack restores 472 verified historical runtime-display strings that fell outside a fresh game-source inventory, while excluding 12 changed older dialogue/database/description strings. A full v1.04 pack now includes 34,957 translated entries. Runtime displays, native game scripts and saved-game data remain separate; existing releases and user saves are never redistributed.

## v1.0.1 fixes
The library no longer lists orphaned, moved, or incomplete game directories as installed. If an installation disappears while the launcher is open, launching it is blocked with a clear warning. Empty save folders remain valid; stored profiles, private saves and backups are not deleted. Fully independent mod copies tolerate a missing historical base-folder reference for optional runtime assets.

## v1.0.2 fixes
The Three-in-One v1.04 window title and C/A/TAB controls are translated again. All 20 new/changed v1.04 display strings and 18 reviewed v1.04-specific translation overrides survive rebuilding. No game scripts or saved-game metadata are translated in place; rendering is display-only.

## v1.0.4 compatibility fix
Older launcher v1.0.2 installations rejected the additional changelog file introduced with v1.0.3. v1.0.4 restores the original 29-file ZIP structure for compatibility with existing release validators. The **complete translated changelog is embedded in the launcher EXE** and reproduced in README.txt; Help → Game changelog exports the standalone Markdown on demand. The brief v1.0.3 release requires manual installation of v1.0.4 if its updater rejects the ZIP.

## v1.0.3 improvements
Fresh portable downloads automatically check the official GitHub Releases channel on startup and offer a newer launcher version with a confirmation prompt. No source configuration is required. Existing user opt-outs/custom channels are preserved. The English game-mod v1.01–v1.04 creator changelog is now bundled in README.txt and the EXE resource; Help → Game changelog opens the extracted Markdown copy.

## Download / update

Download the latest **Celesphonia-Delta-Launcher-CH1.zip** and checksum file from [Releases](https://github.com/kitomisaitichi-design/celesphonia-launcher/releases). Extract into a portable folder outside the game folder and run `Celesphonia Portable Switcher.exe`. The official update source `kitomisaitichi-design/celesphonia-launcher` is built in; Help allows turning off startup checks or changing the repository. Checks run automatically when opening the launcher, but installation always requires your confirmation. The program validates SHA-256 and retains rollback copies.

The portable release includes supported version-specific English dictionaries. Translation coverage is incomplete for pre-existing dialogue; do not represent the game as fully translated. No original commercial game data, developer RAR/ZIP mod releases, user saves, local profiles, runtime sessions, or personal settings are included.

## Source and reproducibility

The launcher is C#/.NET Framework 4.x with runtime JS and Python packaging scripts. `Build.ps1` compiles the launcher using Windows .NET Framework `csc.exe` and embeds a local generated English release snapshot. Some translation inputs/English catalogs are deliberately excluded from this repository; building the exact localized distribution requires the source language data and reviewed translation snapshots. Run the packaging tests before publishing.

See [GitHub auto-update](LAUNCHER%20AUTO-UPDATE.md) and [upgrade paths](UPGRADE%20PATHS.md) for verification, supported variants and safety details.

*Unofficial third-party tool; not affiliated with or endorsed by the game developer.*
