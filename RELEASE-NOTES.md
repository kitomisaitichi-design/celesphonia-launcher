# Celesphonia Launcher v1.0.12 — Direct Ver1.052 install and English recovery

This launcher release adds an independent fresh install of the Chinese Three-in-One 3.3.5 Ver1.052 mod using an intact **GoRPG 1.23 game ZIP/folder** plus the author's **Ver1.052 mod bundle**. The Install page now offers **Install fresh mod + English**. An existing v1.04 or v1.051 installation is no longer required for this fresh-install route.

- Finds supported ZIP/RAR/7z archives (including renamed extensions) and extracted folders using signatures, content verification and discovered locations. Supports the game base and creator update in different locations.
- Applies the matching version-specific **reversible English display overlay** as part of installation. The English translation remains partial; unmatched text stays in its native language.
- Recovers a previously English-modified source index only when an authentic `original-entry.bin` can reconstruct the injected `index.html` exactly; unknown edits and modified native game scripts are rejected without replacing them.
- Keeps the new game installation separate. Existing originals and personal saves are not packaged or deleted. Forward-compatible save migrations use validated routes and preserve backups; different mod branches are not silently mixed.
- Reconciles old installation-index entries only when the referenced folder is absent and its parent still exists, retaining the prior index backup.

**Inputs:** This release contains the launcher, bundled English display packs, and version recipes. The GoRPG game and creator Ver1.052 archive must be supplied separately. It contains no game distribution, user profiles or save data.

**Verification:** Fresh GoRPG ZIP → Ver1.052 fixture install, enabled English overlay, empty-save isolation and target file verification; launcher upgrade/English regressions and reversible restoration. No complete gameplay playthrough is claimed.

After closing the game and old launcher, extract the new ZIP over the portable launcher files, leaving `profiles.json`, games, runtime, and save folders in place. For a fresh install, open **Install → Three-in-One 3.3.5 Ver1.052 → Install → Use the original bundle / game → Install fresh mod + English**.