# Memory Library prerequisite localization — launcher 1.0.39

## Cause

The reported Lightning Magic III engram is weapon 161. It requires weapon 160 (电击魔法Ⅱ) and level 23. The dictionary already maps 160 to Lightning Magic Ⅱ. Native Map119 bookshelf events use TemplateEvent EngramRelease, Map002 event 183. Its page-0 command 191 sends D_TEXT a prerequisite containing iw[v[597]]. Native escape expansion inserts the original weapon name after the surrounding sentence has translated. The older converter's post-expansion repair handled ii/item references only; the skill-tooltip fix in 1.0.38 was a separate Window_ObjectInfo path.

## Small shared correction

The overlay now wraps the native escapeIconItem display helper. It calls the native method first, retaining variable resolution and invalid-ID errors, and translates only the returned record-name suffix. Icon prefixes and all source records remain untouched. This applies to ii/iw/ia/is/it references without manually evaluating variable escapes. Existing item-reference handling remains as a compatibility fallback. Unknown names preserve existing translation behavior; no new dictionary entries, dependencies, game flags or calculations are introduced.

## Native before/after evidence

These are actual native DTextPicture glyph/icon bitmaps from the unchanged template command, shown on a plain blue backing for readability. They are not a replay of the whole library map or a purchase action. Re-encoding is lossless and checked by pixel hashes.

### Three-in-One Ver1.061

Before:

![Three-in-One prerequisite before](images/memory-library-creator-before.png)

After:

![Three-in-One prerequisite after](images/memory-library-creator-after.png)

### Maple 1.7.2

Before:

![Maple prerequisite before](images/memory-library-maple-before.png)

After:

![Maple prerequisite after](images/memory-library-maple-after.png)

## Coverage and preservation

Tests/NativeMemoryLibraryFixture.js enumerates native metadata after each variant loads: Three-in-One has 46 prerequisite records/45 prior-engram name rows; Maple has 47/47. All 92 prior-engram rows render without CJK residuals and fit the native viewport. The longest are 686/617 pixels respectively. Both variants also render all six native cost, level and repair-percentage command branches, retaining numbers and color escapes. Eight owned/unowned, level and currency input combinations per variant retain the same prerequisite/level/cost/repair outcomes. No purchase is performed.

The native converter is checked against 2,375 Three-in-One and 2,200 Maple named item/weapon/armor/skill/state references (4,575 total), direct/variable/nested-variable IDs, exact icons and unchanged native errors for invalid IDs. Existing direct item-reference results are unchanged. All generated prerequisite text and branch text is English. Native database/template snapshots, metadata, inventory/variable/switch render-state checks and source hashes remain exact. English is on and runtime controls are absent.

There are 81 Three-in-One and 83 Maple assertions (164 total); these are assertions, not counts of different books. Before runs reproduce the untranslated text in both variants. The earlier 2,800-skill tooltip proof is not presented as Memory Library coverage or a fresh gameplay replay. Full library-map navigation and actual engram purchase gameplay are not claimed.

## Release

Both local launcher installations and both actual English overlays receive the update. Existing updater size/entry limits, the 44-entry portable layout, installation/archive/save logic and dictionary packs remain unchanged. The offline notes link these full native picture pairs; the existing 45 embedded images are retained within the legacy EXE budget. Required notes/layout and extracted update/apply/restore checks are run on the final package. Public release/download and installed hashes are independently compared.
