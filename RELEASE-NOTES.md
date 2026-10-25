# Launcher 1.0.30 — Fix original-source destination conflicts

- Fixes the default install location when the portable launcher sits inside GoRPG or another original source folder. A new independent game is placed outside all selected source folders, instead of failing after a successful source audit.
- Repairs older saved automatic defaults too. Valid custom destinations remain unchanged; unsafe custom locations open a validated folder picker with folder creation. Cancelling preserves the previous settings and starts no installation.
- Shares the destination check across direct/combined-original installation, creator upgrades, layered mods and reconstruction. Checks run before creating the new destination/staging parent. Native source containment and exact hashes remain enforced.
- Keeps existing installed game roots, saves, control sessions, selected profiles and translation behavior intact. This is an installation-location fix; it does not add a speed optimization or relax source verification.
- Restores the user's corrected magic-defense abbreviation **MDF**. Compact equipment stats use **ATK, DEF, MAT, MDF and AGI**; the prior MD substitution is removed. Dictionary wording, native values and gameplay mechanics are unchanged.
- Fixes the stale Ver1.053 label in the launcher-update dialog. Its supported game-release name now comes from the same catalog as installation and upgrades, currently **Ver1.061**, and follows future supported releases automatically.

### Before and after — launcher extracted inside an original game

The native Install-page previews below use the same isolated folder layout and source selection. Before: the default sits inside Original GoRPG/Launcher/Game Profiles. After: Celesphonia Game Profiles sits outside the source tree. A minimal exact-byte reconstruction separately reproduces the old failure and succeeds at the corrected location; these previews do not claim a full-game install.

![Unsafe nested install default before](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.30/docs/images/destination-before.png)

![Safe external install default after](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.30/docs/images/destination-after.png)

### Restored MDF abbreviation

The following native captures compare 1.0.29's MD labels with the exact restored MDF runtime from 1.0.28. Its bytes and unchanged dictionaries match the prior native Maple/Ver1.061 checks; no new gameplay pass is claimed.

![Magic defense before restoring MDF](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.30/docs/images/equipment-terms-after.png)

![Magic defense with MDF restored](https://raw.githubusercontent.com/kitomisaitichi-design/celesphonia-launcher/launcher-v1.0.30/docs/images/equipment-dense-end-after.png)

If an earlier attempt displayed this specific error, update the launcher and retry with the same original files. You do not need to remove the English overlay or download those files again. For custom destinations, choose or create a folder outside every selected original source folder. See the [source and verification trace](https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.30/docs/destination-1.0.30-trace.md).

