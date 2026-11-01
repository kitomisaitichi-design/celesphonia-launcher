# Launcher 1.0.32 — Preserve imported translations during automatic refresh

Fixes automatic English refresh reporting failure after a bundled dictionary update when a user has additional imported translations. The overlay was applied successfully, but stale cached counts made it appear outdated again and could block launch. Refresh now rebuilds only those derived counters from the validated merged dictionary. Custom translation files, native data and saves stay unchanged. Includes all 1.0.31 imprint fixes and the native before/after gallery below.


[Full update notes and trace](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.32/LAUNCHER%20CHANGELOG.md)

### Three-in-One: before and after

![Three-in-One imprint menu before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.32/docs/images/imprint-creator-before.png)

![Three-in-One imprint menu after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.32/docs/images/imprint-creator-after.png)

### Maple: before and after

![Maple imprint menu before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.32/docs/images/imprint-maple-before.png)

![Maple imprint menu after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.32/docs/images/imprint-maple-after.png)
