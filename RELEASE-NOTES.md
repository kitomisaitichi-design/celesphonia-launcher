# Celesphonia Launcher v1.0.8 — Original Bedroom Wardrobe Preserved

This release locks the original wardrobe presentation and retains the v1.0.7 English help/glossary artwork, translated animations, Ticker dialogue, and text wrapping.

- **Original 1×3 wardrobe:** Compared the native `Map005.json` from game-mod v1.04, Ver1.051, and Ver1.052. All three define the same single-column wardrobe with native tile IDs 193 / 201 / 209. An experimental, never-published, 2×3 renderer reconstruction was rejected and removed. The launcher does not modify the native map tile renderer, map data, event positions, collision, or saved-game values.
- **Two original interaction points unchanged:** the clothes-change event and the Shamelessness-unlocked trench-coat/transparency-curse event remain at their original coordinates. The creator's in-game unlock conditions remain unchanged.
- **All five English help/tutorial image replacements retained:** the picture and animation redirections remain source-hash verified, reversible, and embedded in the launcher EXE. No creator game graphics or gameplay files are redistributed.
- **Compatibility:** Three-in-One Ver1.051 and Ver1.052 and all ten English version packs remain supported; old update clients receive the same 35-file ZIP layout and can find this release through GitHub auto-update.
- **QA:** Native original map tile IDs and event positions checked for all three creator versions; explicit test guards against reintroducing a Tilemap hook; runtime help-image/Ticker tests; launcher English/upgrade/recipe suites and legacy distribution layout tests. Isolated extracted release verifies English enable/restore across all ten supported versions.

No new Shamelessness rules or clothing assets are injected. Creator installations, personal saves and previous launcher rollback files are not included in this ZIP.