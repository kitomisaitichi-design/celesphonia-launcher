# Launcher updates

Launcher releases change this tool and its reversible display overlays. Game-mod developer announcements are in the separate **Game developer notes** view.

## 1.0.29 — Match Kagura and CH56.6 stat abbreviations

Uses **ATK, DEF, MAT, MD and AGI** when compacting equipment descriptions. Magic defense and its compact stat-down effects now use **MD**, retaining the familiar Kagura/CH56.6 terminology. This changes display labels only; original stat IDs, modifiers, signs and numeric values are unchanged.

Retains all 1.0.28 equipment fixes for Maple and Three-in-One: overflow-aware abbreviations, English composed AGI-down labels, complete wrapped descriptions, readable scrolling help with native arrows, and wheel routing that leaves the item list alone. Already-fitting descriptions keep their full wording. Optional controls are not required. The full overflow before/after gallery follows in the 1.0.28 section below.

The following pair shows the same native Demon King Core panel, at the same scroll position: 1.0.28's MDF labels become MD in 1.0.29. Other effects and values stay intact.

![Equipment abbreviations before the Kagura correction](docs/images/equipment-terms-before.png)

![Equipment abbreviations after the Kagura correction](docs/images/equipment-terms-after.png)

Native equipment rendering and interaction checks were rerun in Maple 1.7.2 and Three-in-One Ver1.061 with the corrected runtime, including all 854 named weapon/armor records. This remains a partial English display overlay; full gameplay routes were not replayed. See the [updated trace](docs/equipment-help-1.0.28-trace.md).

## 1.0.28 — Readable equipment descriptions and English trait labels

- Fits the complete weapon/armor description together, including the formatter's appended stats and traits. Fixes long English lines clipping in Maple 1.7.2 and Three-in-One Ver1.061.
- Uses familiar abbreviations only when full wording does not fit: ATK, DEF, AGI, PHYS-EVA, CRIT-EVA, MAG-EVA, MAT, MDF and related labels. Full wording stays when it fits. Native values, signs, percentages, conditions, icons and control slots remain intact.
- Reuses reviewed whole state labels inside assembled brackets. The reported 敏捷下降（小） / 敏捷下降（大） fields now render English, compacted to AGI Down (S) / AGI Down (L) when needed. S/L retain the distinction between the two native effects.
- Keeps dense equipment help readable at 20px or larger. Hover the help panel and use the mouse wheel to reach the rest; native arrows indicate more text. The equipment list does not scroll while the pointer is over scrollable help.
- Runs as part of the English display overlay with optional controls off. No game-data, save-path, install/upgrade or stat-mechanic changes. Existing enabled English overlays refresh on the next Play as before.

### Three-in-One Ver1.061 — Ninja Hood

Before: the stat/trait line extends beyond the help panel. After: familiar abbreviations expose the English AGI effects; the remaining line is reachable by scrolling.

![Three-in-One Ninja Hood before](docs/images/equipment-creator-before.png)

![Three-in-One Ninja Hood after](docs/images/equipment-creator-after.png)

### Maple 1.7.2 — Ninja Hood

The same source record in Maple's wider native panel now fits with English AGI traits and compact stats. Window dimensions are unchanged between each pair.

![Maple Ninja Hood before](docs/images/equipment-maple-before.png)

![Maple Ninja Hood after](docs/images/equipment-maple-after.png)

### Weapon descriptions — Master Blade

Before: the long generated effect list is cut off. After: the complete description is wrapped and readable; scroll to inspect later effects.

![Three-in-One Master Blade before](docs/images/equipment-weapon-before.png)

![Three-in-One Master Blade after](docs/images/equipment-weapon-after.png)

### Dense descriptions — Demon King Core

Before: even tiny text clips vertically. After: readable text with compact trait labels, with the remaining effects accessible below. The last image shows the bottom of the same help panel after scrolling.

![Demon King Core before](docs/images/equipment-dense-before.png)

![Demon King Core after](docs/images/equipment-dense-after.png)

![Demon King Core after scrolling to the end](docs/images/equipment-dense-end-after.png)

Native rendering checks cover all 854 named weapon/armor records across Maple and Ver1.061, plus wheel routing, empty/repeated selection, refresh, numeric/control-code preservation and unchanged native databases. These are isolated native UI checks, not a full gameplay walkthrough or a claim of complete English coverage. See the [source and validation trace](docs/equipment-help-1.0.28-trace.md).

## 1.0.27 — Carry legacy runtime sessions after profile rediscovery

Retains the Ver1.06/1.061 support and all 24 new translations from 1.0.26. When shared discovery assigned a new profile ID, an older control session can still hold the same game's independent saves and panel settings. Upgrades now locate a unique matching session by its original save path, back it up and carry it independently. Ambiguous matches remain untouched and stop with an explicit message.

## 1.0.26 — Ver1.06 / Ver1.061 originals, upgrades and English

- Registers both complete creator content overlays and their full native target inventories: 5,171 overlay files / 11,738 target files per version. The latest forward-upgrade target is Ver1.061.
- Direct GoRPG plus latest content installation needs no intermediate patch. The existing exact-hash pool accepts archives, misleading extensions, recursive extracted trees, partial sources and individual files. Compatible shared bytes from other bases remain usable.
- Adds strong native fingerprints, including CommonEvents and Map002, so 1.06 and 1.061 are distinguished even though their System.json is identical. The newer archive's misleading 1.062 folder name does not override its verified 1.061 identity.
- Forward Three-in-One 3.3.5 routes preserve backup/old-profile boundaries and carry save files, global/config metadata and the runtime-control checkbox. Reverse or unrelated-branch automatic save transfers remain rejected.
- Translates all 24 newly introduced display strings in both new partial-English packs, preserving formatting/control codes and existing reviewed translations. Shared native artwork hashes are verified before retaining the illustrated fixes; unchanged bedroom data retains its previous display repair.
- Adds the creator's Ver1.06 and Ver1.061 announcements in chronological order. Existing notification names, blank-field, memory-grid, streaming, HDR and updater repairs remain.

See the [version/source trace](docs/v106-support-trace.md). Original native files and gameplay conditions remain unchanged by the English display overlay. Full gameplay compatibility is not implied by native/hash checks.

## 1.0.25 — Remaining notification names and anal terminology

- Fixes the remaining native notification paths, including **Heart (2980)** and **Noicon (2950)**. The previous name repair only handled Talk (2944) and the separate time field.
- The nested message value is resolved during native drawing across the supported TMng modes and alternate notification frames. Amane, Celesphonia and custom names remain visible; a name-only field no longer becomes empty when the renderer consumes its name as an escape code.
- Native notification arguments, stored values, intentional line breaks and event logic are preserved. This does not remove all blank rows indiscriminately or alter user saves. Unknown text remains native.
- **Extreme Sensitivity (Anal)** replaces **Extreme Sensitivity (Intestines)**. Its description now says **Anal development EXP modifier**. Skill 955's tags map to the anal-development cap and EXP stats; no mechanical value changes.
- Works without runtime controls. Play refreshes an already-enabled English overlay after the launcher update. Memory-grid fixes, save paths, HDR options, controls and updater safeguards remain.

### Additional notification path — before and after

![Before: the Noicon field loses its character name](docs/images/notification-names-before.png)
![After: the native ticker retains the name and translates the full field](docs/images/notification-names-after.png)

### A name-only field — before and after

![Before: the actor-name field becomes completely blank](docs/images/notification-blank-before.png)
![After: the same field displays Mira](docs/images/notification-blank-after.png)

### Title terminology — before and after

![Before: Intestines conflicts with the anal-development modifier](docs/images/notification-title-before.png)
![After: title and EXP modifier consistently say Anal](docs/images/notification-title-after.png)

Notification images use neutral test messages with the reproduced native escape structure and the actual native ticker renderer; they are not replacement game dialogue. The title examples use native skill 955. Comparisons use identical temporary state, never player saves. See the [source trace](docs/notification-1.0.25-trace.md). Full gameplay coverage is not claimed.

## 1.0.24 — Actor names and memory descriptions

- Nested English notifications retain Amane/Celesphonia and custom actor names. The display hook resolves the native name escape before the ticker inserts the message; it does not rename game actors.
- Empty locked-memory descriptions remain blank rather than becoming numeric zero. Maple's native question-mark placeholder is preserved.
- Long memory titles fit the available column width while retaining each variant's native row spacing (24 px in Ver1.053, 29 px in Maple). The last columns no longer drift away from their selection cursor.
- Selecting an unused cell in the final column uses the native locked-description sentinel for that display read, instead of treating an unrelated profile variable as a skill ID. Stored profile values and unlock prerequisites remain intact.
- Normal English launches receive these fixes with runtime controls disabled. After updating, use Play to refresh an already-enabled overlay automatically. Existing save paths, HDR options, controls and updater safeguards are retained.

### Missing actor name — before and after

![Before: the nested notification loses Amane's name](docs/images/text-memory-names-before.png)
![After: the same notification retains Amane's name](docs/images/text-memory-names-after.png)

### Locked description — before and after

![Before: a native empty description displays zero](docs/images/text-memory-locked-before.png)
![After: the same locked description stays blank](docs/images/text-memory-locked-after.png)

### Ver1.053 memory grid — same selection, before and after

![Before: shrinking an entire column moves its rows away from the cursor](docs/images/text-memory-creator-before.png)
![After: fitted titles keep the native row positions and selected description](docs/images/text-memory-creator-after.png)

### Maple memory grid — same selection, before and after

![Before: long English titles extend beyond their column](docs/images/text-memory-maple-before.png)
![After: titles fit Maple's column width with native row spacing retained](docs/images/text-memory-maple-after.png)

These comparisons show actual native ticker/picture layers on a plain backdrop, with identical temporary test state before and after. They do not use or modify player saves. Native checks cover locked/unlocked entries, the last columns, unused tail cells and renamed actors; full gameplay coverage is not claimed.

## 1.0.23 — HDR compatibility and clearer runtime controls

- Root package.json launch preferences such as chromium-args are accepted in normal and control-session launches. The selected native entry remains verified; deep installation/source hash checks stay exact.
- Live HP/MP bars show the active party leader's current/maximum values across tabs, with clear empty/no-character states.
- Rounded controls, smooth hover highlights and a more compact navigation strip retain themes, opacity, resizing and header-only dragging.
- Click the shortcut in the header to change it. Ctrl+Shift+G replaces F8 as the default to avoid laptop airplane-mode keys; the Controls button remains available. The saved shortcut also works detached.
- Game data, translations, save paths and 1.0.22 updater safeguards are retained.
- English application retries brief Windows file-lock/remove errors during atomic replacement, with hash guards. Persistent locks or externally changed files still stop with an explicit path; there is no delete/copy fallback.

## 1.0.22 — Changed-file updates and explicit rollback results

- Identical installed assets are no longer replaced or copied to rollback. A two-file update no longer rewrites all 38 packaged files.
- Launches the checksum-verified staged launcher for the existing update command, removing the extra random helper EXE copy.
- Full ZIP/payload checks, path restrictions, parent-exit wait and backups remain. Rollback restores only completed replacements and reports failures explicitly.
- Investigated after a Bitdefender Advanced Threat Defense behavior report. The redundant writes are reproduced and corrected; Bitdefender validation remains pending.
- Affected 1.0.19–1.0.21 users need the README's one-time three-file ZIP transition because their installed updater still runs during its first upgrade. Antivirus settings and quarantine are not changed.

All 1.0.21 game-display fixes, illustrations and chronological developer notes are retained. Game JavaScript and English packs are unchanged in this release.

## 1.0.21 — Readable status panels and illustrated notes

- Maple and Three-in-One: resistance labels and values have separate bounded cells, compact names and aligned values. Clothing HP fits both percentage and current/maximum displays.
- Native numbers, signs, colors, picture text and game logic remain intact. These presentation fixes work with optional F8 controls disabled.
- Help opens a themed, resizable notes reader with section navigation, offline images and separate launcher/developer histories. README is a concise starting guide.

### Maple — same values, before and after

![Maple before: overlapping English labels](docs/images/stat-layout-maple-before.png)
![Maple after: separated labels and values](docs/images/stat-layout-maple-after.png)

### Three-in-One Ver1.053 — same values, before and after

![Three-in-One before: overlapping columns](docs/images/stat-layout-creator-before.png)
![Three-in-One after: fitted resistance and clothing fields](docs/images/stat-layout-creator-after.png)

These images show the actual native CE806 picture renderer with controlled in-memory values. Source game records and saves are not edited for the comparison.

## 1.0.20 — Automatic English refresh on Play

Normal and F8 launches refresh outdated, already-enabled English overlays before starting. Recipients no longer need to remove/reapply English after updating. Current overlays are a no-op; disabled overlays remain disabled. Direct game-executable launches bypass launcher checks.

## 1.0.19 — Repeat streaming without runtime controls

The narrow stale-message-pause repair is included in the regular English overlay. F8 controls are optional. Native first-stream and repeat-stream conditions remain in effect, and duplicate repair loading is harmless.

## 1.0.18 — Repeat-stream choice handoff

Repairs the stale message pause left by the bedroom PC menu when the repeat-stream prompt is queued. The initial release applied this through Control Sessions; 1.0.19 extended it to normal English launches.

## 1.0.17 — Time-of-day display

Resolves the time name in the native notification's display path so translated time labels are not swallowed as escape codes. Stored actor/variable names remain original.

## 1.0.16 — Direct Ver1.053 installation

Install creator Ver1.053 from Gorpg plus matching creator content without Ver1.051 or Ver1.052. Recursive discovery combines original files from ZIP/RAR/7z, renamed archives, extracted folders and partial sources using verified hashes. The separate partial English pack retains reviewed labels and adds four changed display strings.

## 1.0.15 — Clean CH reference

Uses clean native CH hashes for source compatibility; English-patched metadata does not become the canonical native reference. Source profiles and saves stay separate.

## 1.0.14 — Library navigation

Improved compact launcher navigation, visible runtime-control selection and theme consistency. Main Play controls remain directly accessible.

## 1.0.13 — Original-file interoperability

Finds matching native bytes across supported sources rather than requiring a specific intermediate version or archive name. Unknown and missing files are reported before managed installation.

## 1.0.12 — Fresh creator installation

Added direct installation from a clean base and complete creator overlay while retaining forward upgrades and source-save safeguards.

## 1.0.11 — Glossary illustration layout

Measures wrapped English text before positioning illustrations. Images preserve aspect ratio and fit the remaining panel; text-only and image-only entries retain their layouts.

## 1.0.10 — Wardrobe interaction indicators

Renders the complete narrow wardrobe below native indicators so the lower interaction marker blinks again.

## 1.0.9 — Separate bedroom actions

Fits complete wardrobe artwork into its narrow footprint and places the second interaction above it. Preserves native unlock conditions and the transparency/coat menu after native recalculation.

## 1.0.8 — Native wardrobe footprint

Preserves the narrow cabinet footprint and native map data while retaining translated help images, glossary fitting and controls.

## 1.0.7 — English help images and text fitting

Adds reversible translated tutorial/glossary images and hypnosis captions, native ticker translation, and item-help wrapping with icon-width awareness. Assets are embedded to preserve older updater ZIP compatibility.

## 1.0.6 — Creator Ver1.051 / Ver1.052

Adds creator-version detection, verified upgrades and separate partial English packs. Original sources and saves are preserved.

## 1.0.5 — English runtime labels

Restores reviewed display labels omitted from the fresh v1.04 source inventory while excluding changed dialogue and database content.

## 1.0.4 — Backward-compatible update package

Embeds developer notes in the executable instead of adding an unsupported ZIP entry. Older launchers can verify and install the release.

## 1.0.3 — Official update source

Fresh downloads know the official GitHub repository and check for stable releases. Existing opt-out/custom repository settings are retained. Installs verify checksums and keep rollback data.

## 1.0.2 — Window-title translations

Restores v1.04 on-screen title, controls and save-caption translations while preserving internal identification strings.

## 1.0.1 — Existing game profiles

Hides unavailable/incomplete profile entries without deleting records or saves. Independent game folders no longer require a removed historical base for optional runtime asset lookup.
