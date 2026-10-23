# Launcher 1.0.25 — Remaining notification names and anal terminology

- Fixes the remaining native notification paths, including **Heart (2980)** and **Noicon (2950)**. The previous name repair only handled Talk (2944) and the separate time field.
- The nested message value is resolved during native drawing across the supported TMng modes and alternate notification frames. Amane, Celesphonia and custom names remain visible; a name-only field no longer becomes empty when the renderer consumes its name as an escape code.
- Native notification arguments, stored values, intentional line breaks and event logic are preserved. This does not remove all blank rows indiscriminately or alter user saves. Unknown text remains native.
- **Extreme Sensitivity (Anal)** replaces **Extreme Sensitivity (Intestines)**. Its description now says **Anal development EXP modifier**. Skill 955's tags map to the anal-development cap and EXP stats; no mechanical value changes.
- Works without runtime controls. Play refreshes an already-enabled English overlay after the launcher update. Memory-grid fixes, save paths, HDR options, controls and updater safeguards remain.

### Additional notification path — before and after

![Before: the Noicon field loses its character name](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.25/docs/images/notification-names-before.png)
![After: the native ticker retains the name and translates the full field](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.25/docs/images/notification-names-after.png)

### A name-only field — before and after

![Before: the actor-name field becomes completely blank](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.25/docs/images/notification-blank-before.png)
![After: the same field displays Mira](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.25/docs/images/notification-blank-after.png)

### Title terminology — before and after

![Before: Intestines conflicts with the anal-development modifier](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.25/docs/images/notification-title-before.png)
![After: title and EXP modifier consistently say Anal](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.25/docs/images/notification-title-after.png)

Notification images use neutral test messages with the reproduced native escape structure and the actual native ticker renderer; they are not replacement game dialogue. The title examples use native skill 955. Comparisons use identical temporary state, never player saves. See the [source trace](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.25/docs/notification-1.0.25-trace.md). Full gameplay coverage is not claimed.

