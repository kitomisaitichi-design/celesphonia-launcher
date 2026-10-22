# Celesphonia Mod Library

Portable Windows launcher with verified installation and upgrades, separate game profiles, partial English display overlays and optional F8 runtime controls.

## Launcher 1.0.22 — Fewer update writes

In-launcher updates now leave byte-identical installed files untouched and retain rollback copies only for changed files. The verified staged launcher performs the update directly; an extra randomly named copy of the running EXE is no longer created. Full ZIP/payload verification and parent-exit checks remain in place, and rollback failures are reported explicitly.

This addresses unnecessary update behavior investigated after a Bitdefender Advanced Threat Defense report. In the 1.0.19 → 1.0.20 reproduction, only two assets changed but all 38 were previously replaced. **Bitdefender has not validated this revision; the behavior alert's exact scoring cause is not established.**

### If Bitdefender blocked your 1.0.19–1.0.21 updater

The old updater still runs during its first upgrade. Download the current ZIP from Releases and extract it to a separate temporary folder. Close the launcher, back up its existing **Celesphonia Portable Switcher.exe**, **EnglishRuntime.js** and **README.txt**, then copy just those three files from the ZIP into your existing launcher folder. Leave **English**, profiles, settings, Control Sessions and game folders in place. This specific three-file transition applies to 1.0.19–1.0.21, whose other packaged assets are identical. Fresh installations should extract the complete ZIP.

Once installed, subsequent updates use the revised updater. This release does not change antivirus settings or restore quarantined files. If Bitdefender has already removed required files, retain its notification details for review before attempting recovery.

## Launcher 1.0.21 — A clearer status panel

Maple and Three-in-One resistance panels now keep labels and values in separate cells. Compact labels, aligned percentages and a fitted Clothing HP field prevent adjacent columns from overlapping. Native values, negative-value colors and game rules are retained.

### Maple — before and after

![Maple before: English labels overlap the next column](docs/images/stat-layout-maple-before.png)
![Maple after: compact labels and aligned values](docs/images/stat-layout-maple-after.png)

### Three-in-One Ver1.053 — before and after

![Three-in-One before: labels and percentages run together](docs/images/stat-layout-creator-before.png)
![Three-in-One after: readable resistance and clothing values](docs/images/stat-layout-creator-after.png)

These are captures of the native CE806 status pictures, rendered with the same in-memory test values before and after the fix. They are focused layout examples, not full gameplay screenshots.

## Read the release notes in the launcher

Open **Help → Notes & changelogs** for an illustrated, scrollable reader. Switch between **Start here**, **Launcher updates** and **Game developer notes**, or jump directly to a section. The reader follows the launcher theme, can be moved and resized independently, and works offline.

Read the full [launcher changelog](LAUNCHER%20CHANGELOG.md) or the separate [Three-in-One developer announcements](GAME%20MOD%20CHANGELOG.md). Developer announcements currently cover Three-in-One v1.01–Ver1.053; verified Maple developer notes have not been supplied. Launcher and game versions are separate.

## Download and update

Download the ZIP from [GitHub Releases](https://github.com/kitomisaitichi-design/celesphonia-launcher/releases/latest), extract it, and run **Celesphonia Portable Switcher.exe**. Keep the included English folder and program files together. For an existing installation, use **Help → Check launcher updates**.

After updating, close the game and click **Play** or **Switch & Play** once. An already-enabled English overlay refreshes automatically before launch. Games with English disabled stay disabled. Directly launching the game's executable bypasses this refresh.

## Install a game or add an existing one

- Use **Add an installed game** for a game folder you already have.
- For a new Three-in-One Ver1.053 installation, supply your own **Gorpg 1.23 base** and the **creator's Ver1.053 files**, then choose Ver1.053 in Install. Earlier creator versions are not required.
- ZIP, RAR, 7z, renamed archives, extracted folders and individual original files can contribute matching bytes. The launcher reports missing files before creating the installation.
- Existing compatible creator releases can use the forward-upgrade route with verified save carryover. Gorpg, CH, Maple and Three-in-One retain their separate profiles.

No commercial game data, creator archives, user saves or local profiles are included in the launcher download.

## English and runtime controls

English is partial and version-specific. Unmatched text stays in its original language. Enable or restore the reversible display layer from **English**; user translations are retained. The stat-panel and repeated-streaming repairs work with runtime controls off.

The Play page checkbox enables optional runtime controls. **F8** shows or hides that panel. Control Sessions have their own saves, seeded from the game once; later normal and control-session progress can differ. Open the appropriate saves folder from the launcher before moving saves manually.

## Backups and recovery

Back up saves from the **Saves** page. Managed upgrades preserve the source game, and removal keeps saves and unknown files. Restoring original text from **English** restores the backed-up entry page. Keep launcher update backups until the new release works for you.

See [upgrade paths](UPGRADE%20PATHS.md) and [GitHub auto-update details](LAUNCHER%20AUTO-UPDATE.md) for supported routes and rollback behavior.

## Source and verification

The launcher uses C#/.NET Framework, JavaScript and Python packaging tools. Build with `Build.ps1`. Exact localized builds require the private source-language catalogs and reviewed translation snapshots; these are not all in the repository. Focused native layout checks and extracted-package apply/update/restore checks accompany this release. Full gameplay routes are not certified by those checks.

Maintenance prioritizes the latest two creator releases and new Maple releases. Older supported recipes, overlays and upgrade routes remain available; Gorpg and CH remain stable reference sources.

Unofficial third-party tool; not affiliated with or endorsed by the game developer.
