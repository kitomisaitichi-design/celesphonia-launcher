# Launcher updates

Launcher releases change this tool and its reversible display overlays. Game-mod developer announcements are in the separate **Game developer notes** view.

## 1.0.37 — Read split ZIP volumes correctly

**Before:** a valid `.zip.001` plus `.002` could fail with **End of Central Directory record could not be found** because ZIP header detection selected the single-file reader. **After:** first volumes use the existing volume-capable reader; the verified sample restores all 6,000 bytes exactly. No new archive dependency is required.

Keep every volume together with its original matching name, then select the first `.001`. The continuation `.002` is read automatically; do not extract it separately. Incomplete, corrupt, unsafe or duplicate entries still fail, and final game-file sizes/hashes remain required. Ordinary/mislabelled ZIPs, split 7z, RAR and individual native fragments remain supported.

Grenim's reported 4,455 missing files include creator-mod media. Their exact archive format is unconfirmed: this fixes a reproduced split-ZIP defect, not every missing-source report. An incomplete extracted folder still needs the complete original volumes. Existing games, saves and English overlays are unchanged. [Diagnostic trace](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.37/docs/split-archive-1.0.37-trace.md).

## 1.0.36 — Cover the manual creator-archive picker too

Completes the Map027 upgrade recovery below when the creator bundle is first selected manually after the initial source scan. The chosen archive receives fresh exact-target checks before the older source audit can block recovery, including a download replaced under the same filename. Full target verification, compatible save backup/copy, strict source checks when conversion needs them, and original-file preservation remain unchanged.

**Before:** a manually picked bundle could still fall through to the old-source Map027 error. **After:** a complete exact target is reconstructed through the same verified path as discovered originals. [Diagnostic trace and limits](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.36/docs/map027-upgrade-1.0.35-trace.md).

## 1.0.35 — Recover upgrades blocked by an older source map

Fixes both source-method choices getting stuck on **Source creator file changed or missing: data/Map027.json** in an older installed game, even when the original inputs already supply every correct file for Ver1.061.

**Before:** the creator-bundle path verified the complete new target, then rejected the older game again while carrying its saves. Conversion auto-selection could stop at the same old-source error before reaching a valid original-file reconstruction.

**After:** when every final target file passes the authoritative native inventory, either source method can build that independent target and carry backed-up, compatible saves. The old game's native tree is not blindly copied or marked intact. Each selected input file is still rechecked during recovery. Without complete originals, conversions retain their existing strict source checks; missing target bytes, unknown versions, cross-branch saves and downgrades remain rejected.

The source game/map and saves stay untouched. Normal saves, optional runtime-control progress and custom control preferences keep their existing verified migration behavior. No translation dictionary, game mechanic or native-map edit is part of this update.

Verified against the reproduced shared older-game failure, a full **11,738-file** Ver1.061 reconstruction and focused branch/running-game/destination checks. The private source fixture is Ver1.053, which shares the reported Ver1.052 guard and Map027 hash; this is not an exact replay of the reporter's files. [Before/after diagnostic evidence and limits](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.35/docs/map027-upgrade-1.0.35-trace.md).

## 1.0.34 — Readable magic-skill tooltip details

- Translates the lower skill-tooltip labels and element fragments in Three-in-One Ver1.061 and Maple 1.7.2. Ice is now shown as **HP Damage / Ice**, the formula caption is English, and **Usable: In Battle** replaces **Usable:Fight**. Labels and values have readable spacing.
- Keeps native damage formulas, targets, percentages, MP costs, spell names and variant-specific effects exact. For example, Gefrier retains Three-in-One's 40% freeze chance and Maple's 20% chance. The existing German-style spell names are intentional and remain.
- Measures translated tooltip text before sizing the panel. Oversized skill details wrap at their native readable font size and retain the full text with hover-wheel scrolling. Ordinary state/equipment tooltips retain their previous behavior.
- Fixes Maple displaying **Additional Cost: undefined** when an ordinary skill has no optional custom-cost field. Real custom costs and native visibility settings are respected.
- Works with English enabled and runtime controls off. Existing enabled English overlays refresh through the normal launcher path; no manual remove/reapply is needed.

**Before — native formula caption**

![Formula caption before](docs/images/skill-tooltip-formula-before.png)

**After — English formula caption**

![Formula caption after](docs/images/skill-tooltip-formula-after.png)

These are native text-layer detail crops at the original font size. [Full native Three-in-One and Maple before/after screenshots](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.34/docs/skill-tooltip-1.0.34-gallery.md) · [Source trace and verification limits](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.34/docs/skill-tooltip-1.0.34-trace.md)

## 1.0.33 — Readable enemy stats, states and traits

- Gives stat labels and values separate space in Three-in-One's battle inspector, fixing overlapping evasion, reflection, attack-speed and recovery captions. Native HP, MP, percentages and other values stay exact.
- Wraps long state names and puts permanent/remaining-turn captions on a separate row. Dense state lists retain native selection and scrolling.
- Fits resistance/trait tables inside the screen. Conventional abbreviations are used when required, and the table uses fewer columns when needed. Long tables keep their full content: hover the table and use the mouse wheel; native arrows indicate more rows. The list underneath keeps its selection while you scroll the table.
- Runs with the ordinary English overlay; runtime controls are not required. The adapter checks the native plugin's actual capabilities. Maple's different tooltip has been checked and keeps its existing behavior.

**Before — overlapping stat values and state captions**

![Enemy information before](docs/images/enemy-info-stats-before.png)

**After — aligned values and separate duration rows**

![Enemy information after](docs/images/enemy-info-stats-after.png)

[Full native trait-table before/after gallery](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.33/docs/enemy-info-1.0.33-gallery.md) · [Source trace and verification limits](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.33/docs/enemy-info-1.0.33-trace.md)

## 1.0.32 — Preserve imported translations during automatic refresh

Fixes automatic English refresh reporting failure after a bundled dictionary update when a user has additional imported translations. The overlay was applied successfully, but stale cached counts made it appear outdated again and could block launch. Refresh now rebuilds only those derived counters from the validated merged dictionary. Custom translation files, native data and saves stay unchanged. Includes all 1.0.31 imprint fixes and the native before/after gallery below.

## 1.0.31 — Readable English imprint descriptions

- Fixes the imprint menu splitting original descriptions into fragments before translation. Complete reviewed skill descriptions now translate first, then wrap at word boundaries. This resolves mixed Chinese/English in Cyclone Break, Burst Impact, Mental Focus and the related imprint skill panels in Three-in-One and Maple.
- Fits long skill titles inside the existing panel and retains native skill colors. Full wording remains when it fits; overflowing panels use conventional stat abbreviations. Very long imprints retain their text in a readable scrolling panel: hover the description and use the mouse wheel. Native arrows show when more text is available.
- Corrects four source-exact inherited percentage fields: ATK +10%, ATK +15%, Max HP +10% and Max HP +15%. The English numbers now match each native source field across all 13 compatible packs. Native effects, formulas, IDs and values are unchanged. Other game/version differences remain intact, including Maple's three-hit skill versus the newer Three-in-One two-hit skill.
- Native checks cover all 80 Three-in-One Ver1.061 and 82 Maple imprints, selection/empty/reset behavior and dense-panel scrolling, with runtime controls disabled. Original data and private test state remain unchanged by rendering. This remains a partial English overlay; no full gameplay sweep is claimed.

### Three-in-One Ver1.061 — before and after

These native description-panel captures use the same private four-slot loadout, selection and panel size. They retain the native text pixels and font size. Before: Chinese fragments and clipped English. After: whole descriptions and complete titles fit the panel.

![Three-in-One imprint descriptions before](docs/images/imprint-creator-before.png)

![Three-in-One imprint descriptions after](docs/images/imprint-creator-after.png)

### Maple 1.7.2 — before and after

The same display correction follows Maple's own source wording and effects, preserving its skill differences.

![Maple imprint descriptions before](docs/images/imprint-maple-before.png)

![Maple imprint descriptions after](docs/images/imprint-maple-after.png)

Update the launcher and launch with your enabled English overlay. It refreshes automatically; runtime controls are not required. See the [source and verification trace](docs/imprint-1.0.31-trace.md).

## 1.0.30 — Fix original-source destination conflicts

- Fixes the default install location when the portable launcher sits inside GoRPG or another original source folder. A new independent game is placed outside all selected source folders, instead of failing after a successful source audit.
- Repairs older saved automatic defaults too. Valid custom destinations remain unchanged; unsafe custom locations open a validated folder picker with folder creation. Cancelling preserves the previous settings and starts no installation.
- Shares the destination check across direct/combined-original installation, creator upgrades, layered mods and reconstruction. Checks run before creating the new destination/staging parent. Native source containment and exact hashes remain enforced.
- Keeps existing installed game roots, saves, control sessions, selected profiles and translation behavior intact. This is an installation-location fix; it does not add a speed optimization or relax source verification.
- Restores the user's corrected magic-defense abbreviation **MDF**. Compact equipment stats use **ATK, DEF, MAT, MDF and AGI**; the prior MD substitution is removed. Dictionary wording, native values and gameplay mechanics are unchanged.
- Fixes the stale Ver1.053 label in the launcher-update dialog. Its supported game-release name now comes from the same catalog as installation and upgrades, currently **Ver1.061**, and follows future supported releases automatically.

### Before and after — launcher extracted inside an original game

The native Install-page previews below use the same isolated folder layout and source selection. Before: the default sits inside Original GoRPG/Launcher/Game Profiles. After: Celesphonia Game Profiles sits outside the source tree. A minimal exact-byte reconstruction separately reproduces the old failure and succeeds at the corrected location; these previews do not claim a full-game install.

![Unsafe nested install default before](docs/images/destination-before.png)

![Safe external install default after](docs/images/destination-after.png)

### Restored MDF abbreviation

The following native captures compare 1.0.29's MD labels with the exact restored MDF runtime from 1.0.28. Its bytes and unchanged dictionaries match the prior native Maple/Ver1.061 checks; no new gameplay pass is claimed.

![Magic defense before restoring MDF](docs/images/equipment-terms-after.png)

![Magic defense with MDF restored](docs/images/equipment-dense-end-after.png)

If an earlier attempt displayed this specific error, update the launcher and retry with the same original files. You do not need to remove the English overlay or download those files again. For custom destinations, choose or create a folder outside every selected original source folder. See the [source and verification trace](docs/destination-1.0.30-trace.md).

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
