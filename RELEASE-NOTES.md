# Celesphonia launcher 1.0.22 — Changed-file updates

The updater now skips installed files whose SHA-256 already matches the release. A reproduced 1.0.19 → 1.0.20 update changed only two files but previously replaced/backed up all 38. Translation catalogs and other identical assets are now left untouched.

The checksum-verified staged launcher performs the update directly, eliminating an extra randomly named helper EXE copy. Full ZIP/payload validation, path restrictions and the parent-exit wait remain. Rollback covers completed replacements, preserves originals and reports restoration failures.

This corrects unnecessary updater behavior investigated after a Bitdefender Advanced Threat Defense report. **Bitdefender has not validated this build, and the exact behavior-scoring trigger remains unconfirmed.** No antivirus settings are changed and no quarantined files are restored.

## Affected 1.0.19–1.0.21 users: install from the ZIP once

Your currently installed updater still runs during its first upgrade. Download/extract this ZIP to a separate temporary folder, close the launcher, and back up these three files in your existing launcher folder before replacing them from this ZIP:

- Celesphonia Portable Switcher.exe
- EnglishRuntime.js
- README.txt

Leave English, profiles, settings, Control Sessions and game directories in place. This specific transition is valid for 1.0.19–1.0.21 because the other packaged files are identical. Fresh installations should use the complete ZIP. Subsequent updates use the revised updater.

If Bitdefender has already removed required files, keep its notification details for vendor review before attempting recovery. Do not treat this build as Bitdefender clearance for a previous quarantine.

All 1.0.21 stat-panel fixes, illustrated notes, chronological developer notes, streaming repairs and automatic enabled-English refresh remain. Game scripts and English packs are byte-identical to 1.0.21; saves, settings and game paths are retained.
