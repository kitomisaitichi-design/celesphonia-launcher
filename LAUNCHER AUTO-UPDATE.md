# Celesphonia Launcher — GitHub Releases auto-update

This system updates **the Windows launcher**, independently of the
Three-in-one 3.3.5 **game mod v1.04**. Its launcher version is now
**1.0.3**, following the original 1.0.0, orphan-profile-fix 1.0.1,
and v1.04 English-title fix 1.0.2 releases. Nothing about mod releases
is inferred from launcher tags.

## What now works

The Help page has **Check launcher updates** and **GitHub source** controls.
Freshly extracted launchers already use `kitomisaitichi-design/celesphonia-launcher`
with startup update checks enabled; users do not need to enter a source.
Existing settings, including disabled checks or a custom repository, are
respected. The Help page allows manually choosing `owner/repository`.
When startup checks are enabled, the launcher notifies once for each newer
release and asks before installation.
There is no silently initiated update or scheduled background updater.

The updater compares the newest 100 GitHub releases by **numeric semantic
version**, selecting only published, stable `launcher-vMAJOR.MINOR.PATCH`
tags greater than the current launcher version. Publication-date order and
unrelated game-mod release tags do not override launcher versions.
The highest launcher release must contain BOTH:

- `Celesphonia-Delta-Launcher-CH1.zip`
- `Celesphonia-Delta-Launcher-CH1.zip.sha256.txt`

The downloaded ZIP is SHA-256 verified. The ZIP may contain only the EXE,
`EnglishRuntime.js`, `README.txt`, `GAME MOD CHANGELOG.md`, `mod-recipes.json`,
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

1. Use the dedicated public repository
   **kitomisaitichi-design/celesphonia-launcher**. The first stable release
   **launcher-v1.0.0** was published on October 10, 2026, with the verified
   50,550,385-byte portable ZIP and its SHA-256 checksum. This repository is
   separate from the ChatGPT Viewer and Exporter projects.
2. Update the `CurrentVersion` constant in `LauncherUpdate.cs`, rebuild
   the Windows launcher via `Build.ps1`, rerun the native tests and create
   the verified portable ZIP via `BuildPortablePackage.py`.
3. Run `PublishLauncherRelease.ps1 -Repository owner/repository -Version 1.0.3`
   to verify the ZIP and write its checksum asset, then, with GitHub CLI
   installed and authenticated, rerun adding `-Publish` to create
   `launcher-v1.0.3` with both assets. Each future binary must have its
   own higher launcher version and matching release tag.
4. Set **Help → GitHub source** to your repository and leave automatic checks
   enabled. You can also check manually at any time.

GitHub private releases may require authentication and are not guaranteed to
be downloadable through the public release API. Public Releases are the
supported update channel; use only a repository/publisher you trust.

## Validation boundaries

Native offline checks include semantic-version comparisons, an allowlisted
release ZIP, rejected private/traversal paths and an end-to-end test updating
an isolated fake launcher while preserving its private save/config sentinels
and a rollback copy. The installed launcher is now configured to check the
public GitHub repository automatically at startup. Its **real Help → Check
launcher updates** call successfully checked the published v1.0.0 release
and reported the installed v1.0.0 as current. The 1.0.1 release fixes
orphaned and incomplete profile entries: only genuinely playable game
directories appear in the sidebar, stale selections fall back to a valid
profile, and deleting a game folder during an open launcher session produces
a clear warning rather than running or writing a broken session. Old
metadata, saves, and backups remain intact. An independent mod no longer
requires a deleted historical `launcher-base.json` BaseRoot to use the
optional runtime controls. These cases are tested against both working and
portable-profile settings; a real GitHub 1.0.0-to-1.0.1 self-update must
still be checked separately from the isolated rollback tests.

Launcher 1.0.2 fixes the mod v1.04 translated game-window title and the
associated C/A/TAB shortcut captions. The v1.04 catalog now inventories
the three human-facing JavaScript titles from MyselfPlugins.js; exact
English translations from v1.03 and 18 reviewed v1.04-specific additions
are preserved across rebuilds. The window and save-screen translations
are display-only: original plugin logic and save-title matching remain
unchanged. A native JavaScript regression test verifies the real v1.04
window-title translation and unchanged stored save metadata.
