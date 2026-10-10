# Celesphonia Portable Launcher

Portable Windows launcher for supported Celesphonia mod releases. It detects compatible installed game folders and renamed ZIP/RAR sources using version-specific internal fingerprints; supports verified mod upgrades and save preservation; provides version-specific reversible English overlays and optional F8 runtime controls.

**Launcher version 1.0.1** (orphan-profile repair; released after v1.0.0) and game-mod version 1.04 are independently versioned.

## v1.0.1 fixes\r\nThe library no longer lists orphaned, moved, or incomplete game directories as installed. If an installation disappears while the launcher is open, launching it is blocked with a clear warning. Empty save folders remain valid; stored profiles, private saves and backups are not deleted. Fully independent mod copies tolerate a missing historical base-folder reference for optional runtime assets.\r\n\r\n## Download / update

Download the latest **Celesphonia-Delta-Launcher-CH1.zip** and checksum file from [Releases](https://github.com/kitomisaitichi-design/celesphonia-launcher/releases). Extract into a portable folder outside the game folder and run `Celesphonia Portable Switcher.exe`. Set the GitHub update source to `kitomisaitichi-design/celesphonia-launcher` under Help. Checks can run automatically when opening the launcher, but installation always requires your confirmation. The program validates SHA-256 and retains rollback copies.

The portable release includes supported version-specific English dictionaries. Translation coverage is incomplete for pre-existing dialogue; do not represent the game as fully translated. No original commercial game data, developer RAR/ZIP mod releases, user saves, local profiles, runtime sessions, or personal settings are included.

## Source and reproducibility

The launcher is C#/.NET Framework 4.x with runtime JS and Python packaging scripts. `Build.ps1` compiles the launcher using Windows .NET Framework `csc.exe` and embeds a local generated English release snapshot. Some translation inputs/English catalogs are deliberately excluded from this repository; building the exact localized distribution requires the source language data and reviewed translation snapshots. Run the packaging tests before publishing.

See [GitHub auto-update](LAUNCHER%20AUTO-UPDATE.md) and [upgrade paths](UPGRADE%20PATHS.md) for verification, supported variants and safety details.

*Unofficial third-party tool; not affiliated with or endorsed by the game developer.*
