Celesphonia Launcher v1.0.4 — backward-compatible official auto-updates and English game changelog.

- New portable downloads know the official kitomisaitichi-design/celesphonia-launcher GitHub update source and automatically check at startup. The user approves any installation, and can disable automatic checks or choose a custom source.
- The full English creator changelog for game-mod versions v1.01–v1.04 is bundled in README.txt and embedded inside Celesphonia Portable Switcher.exe. Help -> Game changelog exports and opens the standalone GAME MOD CHANGELOG.md on demand.
- Restores the original 29-file release allowlist for compatibility with the v1.0.2 updater. GitHub-release package checks passed with BOTH v1.0.2 and v1.0.4 launcher binaries; isolated replacement and rollback preserved saves and profiles.
- Also preserves v1.0.2 English title fixes, v1.0.1 orphan-profile protection, F8 runtime controls and reversible English overlays.
- All 48 extracted-release cases across eight supported game variants passed, along with native UI and first-run tests.

Important: the brief v1.0.3 release required the additional Markdown ZIP entry and its updater rejects v1.0.4's compatibility-format archive; anyone already running v1.0.3 should install v1.0.4 manually once. v1.0.2 with an already configured update source can update directly. Future fresh v1.0.4 downloads need no source setup.

Launcher version v1.0.4 is independent of game-mod version v1.04. Game archives, saved games and private profiles are not included; English dialogue coverage remains partial.