# Launcher 1.0.24 — Actor names and memory descriptions

- Restores Amane/Celesphonia/custom names in nested English notifications.
- Keeps empty locked-memory descriptions blank instead of showing 0; preserves Maple's native question-mark placeholder.
- Fits long English memory titles within their columns without changing native row spacing. The cursor and selected description remain aligned in the last columns.
- Unused final-column cells no longer treat unrelated profile fields as skill IDs. Native unlock requirements, names, values and saves remain intact.

**Works with runtime controls off.** After updating, use **Play** to refresh an already-enabled English overlay automatically. No manual remove/reapply is needed. HDR launch preferences, live bars, custom shortcut and updater safeguards are retained.

## Missing actor name

Before:
![Before: missing actor name](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-names-before.png)

After:
![After: Amane's name retained](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-names-after.png)

## Locked description

Before:
![Before: zero instead of blank](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-locked-before.png)

After:
![After: native empty description stays blank](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-locked-after.png)

## Ver1.053 — same selection

Before:
![Before: rows drift from cursor](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-creator-before.png)

After:
![After: native row spacing retained](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-creator-after.png)

## Maple — same selection

Before:
![Before: English titles overflow the column](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-maple-before.png)

After:
![After: titles fit Maple's native grid](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.24/docs/images/text-memory-maple-after.png)

Comparisons use actual native ticker/picture layers on a plain backdrop and identical temporary test state. They do not use or modify player saves. The illustrated notes are also bundled offline under **Help → Notes & changelogs → Launcher updates**. Native checks cover renamed actors, locked/unlocked entries, the last columns and unused tail cells; full gameplay coverage is not claimed.
