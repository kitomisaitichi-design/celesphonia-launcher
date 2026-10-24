# Launcher 1.0.29 — Match Kagura and CH56.6 stat abbreviations

Uses **ATK, DEF, MAT, MD and AGI** when compacting equipment descriptions. Magic defense and its compact stat-down effects now use **MD**, retaining the familiar Kagura/CH56.6 terminology. This changes display labels only; original stat IDs, modifiers, signs and numeric values are unchanged.

Retains all 1.0.28 equipment fixes for Maple and Three-in-One: overflow-aware abbreviations, English composed AGI-down labels, complete wrapped descriptions, readable scrolling help with native arrows, and wheel routing that leaves the item list alone. Already-fitting descriptions keep their full wording. Optional controls are not required. The full overflow before/after gallery follows in the 1.0.28 section below.

The following pair shows the same native Demon King Core panel, at the same scroll position: 1.0.28's MDF labels become MD in 1.0.29. Other effects and values stay intact.

![Equipment abbreviations before the Kagura correction](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.29/docs/images/equipment-terms-before.png)

![Equipment abbreviations after the Kagura correction](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.29/docs/images/equipment-terms-after.png)

Native equipment rendering and interaction checks were rerun in Maple 1.7.2 and Three-in-One Ver1.061 with the corrected runtime, including all 854 named weapon/armor records. This remains a partial English display overlay; full gameplay routes were not replayed. See the [updated trace](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.29/docs/equipment-help-1.0.28-trace.md).

