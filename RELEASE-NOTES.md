# Launcher 1.0.28 — Readable equipment descriptions and English trait labels

- Fits the complete weapon/armor description together, including the formatter's appended stats and traits. Fixes long English lines clipping in Maple 1.7.2 and Three-in-One Ver1.061.
- Uses familiar abbreviations only when full wording does not fit: ATK, DEF, AGI, PHYS-EVA, CRIT-EVA, MAG-EVA, MAT, MDF and related labels. Full wording stays when it fits. Native values, signs, percentages, conditions, icons and control slots remain intact.
- Reuses reviewed whole state labels inside assembled brackets. The reported 敏捷下降（小） / 敏捷下降（大） fields now render English, compacted to AGI Down (S) / AGI Down (L) when needed. S/L retain the distinction between the two native effects.
- Keeps dense equipment help readable at 20px or larger. Hover the help panel and use the mouse wheel to reach the rest; native arrows indicate more text. The equipment list does not scroll while the pointer is over scrollable help.
- Runs as part of the English display overlay with optional controls off. No game-data, save-path, install/upgrade or stat-mechanic changes. Existing enabled English overlays refresh on the next Play as before.

### Three-in-One Ver1.061 — Ninja Hood

Before: the stat/trait line extends beyond the help panel. After: familiar abbreviations expose the English AGI effects; the remaining line is reachable by scrolling.

![Three-in-One Ninja Hood before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-creator-before.png)

![Three-in-One Ninja Hood after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-creator-after.png)

### Maple 1.7.2 — Ninja Hood

The same source record in Maple's wider native panel now fits with English AGI traits and compact stats. Window dimensions are unchanged between each pair.

![Maple Ninja Hood before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-maple-before.png)

![Maple Ninja Hood after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-maple-after.png)

### Weapon descriptions — Master Blade

Before: the long generated effect list is cut off. After: the complete description is wrapped and readable; scroll to inspect later effects.

![Three-in-One Master Blade before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-weapon-before.png)

![Three-in-One Master Blade after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-weapon-after.png)

### Dense descriptions — Demon King Core

Before: even tiny text clips vertically. After: readable text with compact trait labels, with the remaining effects accessible below. The last image shows the bottom of the same help panel after scrolling.

![Demon King Core before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-dense-before.png)

![Demon King Core after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-dense-after.png)

![Demon King Core after scrolling to the end](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.28/docs/images/equipment-dense-end-after.png)

Native rendering checks cover all 854 named weapon/armor records across Maple and Ver1.061, plus wheel routing, empty/repeated selection, refresh, numeric/control-code preservation and unchanged native databases. These are isolated native UI checks, not a full gameplay walkthrough or a claim of complete English coverage. See the [source and validation trace](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.28/docs/equipment-help-1.0.28-trace.md).

