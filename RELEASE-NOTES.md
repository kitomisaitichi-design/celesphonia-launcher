# Celesphonia Delta Launcher v1.0.13 — Original-file interoperability

The installer now supports **direct verified creator updates** and **multi-source original-file recovery** without requiring the 7 GB Any-Version delta pack.

## What changed

- **Direct 3.3.5 upgrades.** An intact v1.01, v1.02, v1.03, v1.04 or v1.051 installation can be upgraded straight to a later registered creator release, including v1.051 and v1.052, using the complete corresponding Telegram archive or extracted folder. Fifteen forward pairs are checked. Intermediate releases are not required.
- **Recover originals from the files already on disk.** The **Combine original sources...** dialog accepts ZIP/RAR/7z/split archives, extracted folders and registered installed games together. Filename and extension may be wrong. Only exact matching bytes from the authoritative native inventories are used; missing required originals are reported.
- **Four embedded native inventories:** 7,208 GoRPG 1.23; 13,654 CH; 9,727 Maple 1.7.2; 9,474 hypnosis/Three-in-One 3.3.4 files. The manifests contain only filenames, sizes and SHA-256 digests—not game assets.
- **Improved mixed-file and English handling.** Previously reversible English-modified entry pages can supply their authenticated original bytes. Tampered native files, unsafe archive paths, misleading partial archives and mismatched version markers are not accepted as a complete game.
- **Save routing corrected.** The launcher keeps installed branches and their saves separate, allows same-version restores and registered forward 3.3.5 creator transitions, and blocks cross-branch or newer-to-older saves. It copies actual save bytes rather than rewriting game state.
- **Original hypnosis/3.3.4 verification.** A completed hypnosis game is validated against its full native target inventory rather than treated as complete because of a few version-marker files.

## Installing v1.052 with English

With a complete clean GoRPG ZIP, two-part original 7z, or recoverable native files **and the original Telegram Ver1.052 RAR/folder**, select **Install → Three-in-One Ver1.052 → Use original archives / folders**. Choose **Combine original sources...** if your native files are spread across multiple places. English is a reversible, version-specific display layer and remains partial. A fresh install starts without saves; a verified forward creator-version upgrade can copy backed-up saves.

The historical **v1.05** is not separately registered. It is not invented or misidentified as v1.051 or v1.052.


## Improved source discovery, installation and removal

- **More automatic installation.** Ranks compatible installed games with progress saves before archive candidates; can discover and verify an older full-game archive, migrate its saves, or build fresh directly from multiple original folders and Telegram files. Missing required files are named instead of silently accepting mixed versions.
- **Faster, broader source scanning.** Filters unlikely archive files before reading their headers, avoids redundant archive inspections, identifies partial ZIP/RAR/7z/folder sources as candidates, and distinguishes partial sources from complete target versions. Nearby parent/child game, downloads and Telegram locations are scanned with a bounded budget.
- **Clearer six-page interface.** Install and Files explain the next step and why a source cannot be installed; Saves makes each version's isolated backups and cross-version transfer behavior explicit.
- **Remove only what you choose.** The *Remove this version...* action distinguishes unregister-only from uninstalling verified launcher-managed game files. Both require confirmation. Exact native hashes and a managed-directory ownership check gate file deletion; original ZIP/RAR/7z/split volumes, game saves, backups, and unexpected/custom files are retained. Removed registrations are suppressed from automatic re-import, but can be added manually again.

## Verification scope

Batched, bounded mixed-source ZIP/7z/folder fixtures, 35 exact integrity/security assertions, four compiled native inventory validations, real no-copy hash preflights on registered installations, direct creator route regressions and English install/apply/restore tests passed. The previous clean GoRPG + Telegram Ver1.052 + English end-to-end installation was verified separately. This update was validated without creating another collection of multi-gigabyte QA game installations. Full gameplay-route/save-state compatibility is not claimed.

The ZIP contains **only** the launcher program and matching English packs, not GoRPG, Maple, CH, hypnosis creator content, personal profiles or saves. Keep your original archives and game folders. Close the launcher before extracting this update into its portable directory.
