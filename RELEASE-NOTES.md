# Celesphonia Delta Launcher v1.0.15 — CH native-only reference fix

This update cleans the **CH native-file recovery and uninstall inventory**. No installed game files, save data, Telegram archives or translation exports were modified.

## Fixed

- CH now requires exactly **13,536 verified game/runtime files**, down from 13,654.
- **118 non-runtime entries are excluded** (73,688,059 bytes of previously indexed utilities/metadata): MTool's TrsData blobs, RMDEC and RPG-MV-MZ decrypters, three .NET utility DLLs, inactive clipboard/text-extraction helpers, Visual Studio cache files, RPG Maker authoring files, Windows desktop.ini, documentation/credit text and Chromium locales/*.pak.info developer maps.
- The actual runtime components remain required: Game.exe, NW.js engine DLLs and bin/pak assets, all 53 locales/*.pak, data/System.json, plugins.js, rpg_managers.js, active scripts, images, audio, encrypted game resources.
- CH **identity detection still uses the same three SHA-256 version markers**. Incomplete original-file sources will not falsely fail because MTool exports or decryption helpers are absent.
- Managed-uninstall verification uses this cleaned embedded CH inventory, so excluded tools and personal/foreign files remain outside native deletion authority.
- All other native reference inventories and the direct Telegram creator upgrade graph are unchanged.

## Verified

- Read-only real CH source preflight: **13,536/13,536 SHA-256 matches**, zero missing/changed; **4,502,481,560 bytes** verified against the actual CH installed files.
- Independent strict provenance comparison: **118 removals, zero additions or changed hashes** among retained game assets.
- CH-specific source builder policy and test suite, launcher build, embedded native resource inspection, upgrade/English/self-tests, Windows native UI layout tests, and targeted extracted-release/English archive roundtrips.
- No game installations or save data created, modified or removed during this update.

## Installation

Close Celesphonia Delta Launcher and extract the new 35-file ZIP into the launcher folder, replacing the runtime files. The game's original ZIP/RAR/7z, profiles.json, Game Profiles, Save Backups and installed game directories are separate and should remain unchanged.
