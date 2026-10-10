# Celesphonia Launcher — GitHub Releases auto-update

This system updates **the Windows launcher**, independently of the
Three-in-one 3.3.5 **game mod v1.04**. Its launcher version starts at
**1.0.0**. Nothing about mod releases is inferred from launcher tags.

## What now works

The Help page has **Check launcher updates** and **GitHub source** controls.
Set the GitHub repository as `owner/repository`. When a source is configured,
the launcher checks automatically when it opens (unless you disable that
option), notifies you once for each newer release, and asks before installation.
There is no silently initiated update or scheduled background updater.

The updater compares the newest 100 GitHub releases by **numeric semantic
version**, selecting only published, stable `launcher-vMAJOR.MINOR.PATCH`
tags greater than the current launcher version. Publication-date order and
unrelated game-mod release tags do not override launcher versions.
The highest launcher release must contain BOTH:

- `Celesphonia-Delta-Launcher-CH1.zip`
- `Celesphonia-Delta-Launcher-CH1.zip.sha256.txt`

The downloaded ZIP is SHA-256 verified. The ZIP may contain only the EXE,
`EnglishRuntime.js`, `README.txt`, `mod-recipes.json`,
`version-fingerprints.json`, and the explicit English `manifest.json`,
`catalog.json`, `dictionary.json` files. Unsafe paths, symlinks, unknown
files, oversized ZIP entries, and missing assets are refused. The validated
payload is staged under `Staging/LauncherUpdates`; a copy of the launcher
waits for the original to exit, then swaps files with rollback copies and
restarts the launcher.

The following are NEVER included in release ZIPs or overwritten by the
updater: `profiles.json`, `profiles.json.previous`, `launcher-update.json`,
`Game Profiles`, `Save Backups`, `Control Sessions`, `Runtime`,
`user-translations.json`, mod archives and installed game folders.
English release dictionaries may change; user-added translations remain in
their separate user file. If installation fails, rollback copies are retained.

## Publish a future launcher version

1. Create or choose a dedicated GitHub repository under your account.
   There is currently **no connected Celesphonia repository**, and the
   launcher will not reuse your unrelated ChatGPT Viewer/Exporter repository.
   Choose private/public visibility with regard to rights to game-derived text.
2. Update the `CurrentVersion` constant in `LauncherUpdate.cs`, rebuild
   the Windows launcher via `Build.ps1`, rerun the native tests and create
   the verified portable ZIP via `BuildPortablePackage.py`.
3. Run `PublishLauncherRelease.ps1 -Repository owner/repository -Version 1.0.0`
   to verify the ZIP and write its checksum asset, then, with GitHub CLI
   installed and authenticated, rerun adding `-Publish` to create
   `launcher-v1.0.0` with both assets. Publish a larger tag, such as
   `launcher-v1.0.1`, to exercise an update from launcher v1.0.0.
4. Set **Help → GitHub source** to your repository and leave automatic checks
   enabled. You can also check manually at any time.

GitHub private releases may require authentication and are not guaranteed to
be downloadable through the public release API. Public Releases are the
supported update channel; use only a repository/publisher you trust.

## Validation boundaries

Native offline checks include semantic-version comparisons, an allowlisted
release ZIP, rejected private/traversal paths and an end-to-end test updating
an isolated fake launcher while preserving its private save/config sentinels
and a rollback copy. A **live download/install from GitHub cannot be verified
until a real Celesphonia GitHub release and checksum exist**.
