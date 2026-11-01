# Launcher 1.0.31 — Readable English imprint descriptions

- Fixes the imprint menu splitting original descriptions into fragments before translation. Complete reviewed skill descriptions now translate first, then wrap at word boundaries. This resolves mixed Chinese/English in Cyclone Break, Burst Impact, Mental Focus and the related imprint skill panels in Three-in-One and Maple.
- Fits long skill titles inside the existing panel and retains native skill colors. Full wording remains when it fits; overflowing panels use conventional stat abbreviations. Very long imprints retain their text in a readable scrolling panel: hover the description and use the mouse wheel. Native arrows show when more text is available.
- Corrects four source-exact inherited percentage fields: ATK +10%, ATK +15%, Max HP +10% and Max HP +15%. The English numbers now match each native source field across all 13 compatible packs. Native effects, formulas, IDs and values are unchanged. Other game/version differences remain intact, including Maple's three-hit skill versus the newer Three-in-One two-hit skill.
- Native checks cover all 80 Three-in-One Ver1.061 and 82 Maple imprints, selection/empty/reset behavior and dense-panel scrolling, with runtime controls disabled. Original data and private test state remain unchanged by rendering. This remains a partial English overlay; no full gameplay sweep is claimed.

### Three-in-One Ver1.061 — before and after

These native description-panel captures use the same private four-slot loadout, selection and panel size. They retain the native text pixels and font size. Before: Chinese fragments and clipped English. After: whole descriptions and complete titles fit the panel.

![Three-in-One imprint descriptions before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.31/docs/images/imprint-creator-before.png)

![Three-in-One imprint descriptions after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.31/docs/images/imprint-creator-after.png)

### Maple 1.7.2 — before and after

The same display correction follows Maple's own source wording and effects, preserving its skill differences.

![Maple imprint descriptions before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.31/docs/images/imprint-maple-before.png)

![Maple imprint descriptions after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.31/docs/images/imprint-maple-after.png)

Update the launcher and launch with your enabled English overlay. It refreshes automatically; runtime controls are not required. See the [source and verification trace](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.31/docs/imprint-1.0.31-trace.md).

