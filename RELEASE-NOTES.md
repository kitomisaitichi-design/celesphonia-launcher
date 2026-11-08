# Celesphonia launcher 1.0.44 — Editor repair

Fixes the ContextMenuStrip disposal exception during Export/selection-menu clicks and fully displays all four origin-filter labels. The comparison editor reuses prepared dictionary/index/status data, batches selection work, and avoids repeated full-map construction during drawing. Refresh still picks up imported/model/manual changes; completed translations and authored corrections are retained.

The actual native menu error was reproduced, then selected/filtered exports, cancellation/reopening, all 16 origin combinations, source-bound saves and scaled label layouts were checked. A 35,014-source fixture reduced 60 layer lookups from roughly 1,280 ms to 16 ms; the measured preparation phases dropped from 4.49 s to 2.91 s on this PC. These are targeted timings, not a hardware-wide startup guarantee.

No game files, shared JS adapters, dictionaries, models, saves or paths are rewritten by this UI repair. No model reinstallation or cache reset is required. Full gameplay and physical multi-monitor DPI changes were not retested.

Before/after and trace: https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.44/docs/editor-repair-1.0.44.md
