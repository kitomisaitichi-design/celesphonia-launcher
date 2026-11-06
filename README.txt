CELESPHONIA MOD LIBRARY — LAUNCHER 1.0.39

Launcher 1.0.40 — Three-in-One Ver1.07
Direct GoRPG + 1.07 and mixed-original installation; backed-up forward saves from supported earlier 3.3.5 versions. New display text is translated; English remains partial. Separate mod branches retain their own saves. Read the chronological game developer notes and launcher notes in Help.

START HERE
Extract the ZIP and run Celesphonia Portable Switcher.exe.
Keep the program and included English folder together.

WHAT CHANGED
Memory Library prerequisite names now use their existing English translations
after native icon/name expansion. Both variants are checked; native costs,
required levels, repair progress, icons and eligibility are preserved.
Before/after pictures: Help -> Notes & changelogs -> Launcher updates.
Skill buff/debuff and special-effect fields now use English display names.
Full native sweeps cover 2,800 records across Maple and Three-in-One,
including Force Barrier II. Native effects, formulas and values stay exact.
Before/after: Help -> Notes & changelogs -> Launcher updates.
Split ZIP first volumes (.zip.001) now use the volume-capable reader.
Keep every part together and select .001; do not extract .002 separately.
Missing/corrupt parts still fail exact native-file verification.
Includes the Map027 upgrade repair even when the creator archive is chosen
manually after initial discovery. Picker selections receive fresh byte checks.
Fixes upgrades blocked by an older installed game's Map027 mismatch when
the original inputs already contain every exact file for the new target.
Both source methods can reconstruct the verified target and carry compatible,
backed-up saves. Old maps/saves stay untouched. Required conversion-source
checks and final target hashes remain strict. See Help -> Notes & changelogs.
Magic-skill tooltip details now use English labels/elements, readable spacing
and "Usable: In Battle" in Maple and Three-in-One. Missing optional custom
costs no longer display undefined. Native formulas, chances and MP costs
are preserved. Oversized details wrap and scroll. English overlays refresh
normally without runtime controls. Illustrated notes: Help -> Notes & changelogs.
Enemy-inspector stat values and state durations have their own space.
Long state names wrap. Oversized trait tables fit the screen and retain
their full text with hover-wheel scrolling. Maple compatibility is checked.
Also fixes stale imported-translation counts during automatic English refresh,
preserving custom translations and preventing a false update failure.
Imprint skill descriptions translate before wrapping, fixing Chinese fragments
and clipped English titles in Maple and Three-in-One. Long panels support
hover-wheel scrolling with native arrows. Four ATK/Max HP percentage labels
now match their native source text. Native skills, effects and saves stay intact.
Before/after pictures are in Help > Notes & changelogs > Launcher updates.
Enabled English overlays refresh automatically; runtime controls are not required.

ALSO INCLUDED
Fixes unsafe install defaults when the launcher is inside an original game
folder. Automatic and saved defaults move outside source folders. Conflicting
custom choices open a folder picker with folder creation. Existing games,
saves and valid custom locations stay unchanged. No source re-download is
needed for this destination error. Original-file checks remain enabled.
The update notice now shows the catalog's latest game release, Ver1.061,
and follows future supported versions automatically.

Standard overflow abbreviations are ATK, DEF, MAT, MDF, AGI.
Magic defense uses MDF, including compact stat-down traits.

Long equipment help uses standard stat abbreviations when needed. Short help
keeps full wording. Hover dense help and use the mouse wheel to read every
effect; arrows indicate more text. Scrolling help does not move the item list.
Composed AGI-down trait labels now display English. Checked in Maple 1.7.2
and Three-in-One Ver1.061 without optional runtime controls. Stats and saves
are unchanged. Before/after pictures are in Help > Notes & changelogs.

Legacy runtime-save sessions are also carried when shared discovery assigned
a new profile ID. Matching uses the original save path; ambiguous sessions
are preserved and reported instead of being merged or overwritten.

Added Three-in-One Ver1.06 and Ver1.061. Install latest content directly with
GoRPG; no intermediate patches are needed. ZIP/RAR, misleading archive names,
extracted folders and individual exact-hash files can be combined. Identical
bytes from CH/Maple/older versions can fill gaps. Missing files stay explicit.
Forward 3.3.5 upgrades back up/carry saves and keep the old profile. Other mod
save branches remain separate. All 24 new display strings are translated;
the existing English coverage remains partial. Controls are optional.
The newer archive's folder says 1.062; its filename/changelog identify 1.061.
Detection uses native fingerprints and hashes. See Help -> Notes & changelogs.

RETAINED 1.0.25 FIXES
Remaining missing-name cases in Heart/Noicon and other native notification
modes are fixed. A name-only field no longer becomes a completely blank line.
Intentional native newlines and stored text are retained.
Extreme Sensitivity (Intestines) is now Extreme Sensitivity (Anal); the EXP
modifier also says Anal, matching the native effect-to-stat mapping.
Works with controls off. Use Play after updating to refresh enabled English.
Illustrated notes: Help -> Notes & changelogs -> Launcher updates.

RETAINED 1.0.24 FIXES
Fixed missing Amane/Celesphonia/custom names in nested English notifications.
Empty locked-memory descriptions stay blank rather than showing 0.
Memory title columns keep native row spacing and fit long English labels;
the selected description matches the visible row. Empty cells do not read
unrelated profile fields. Maple keeps its native question-mark placeholders.
Works without runtime controls. Use Play after updating to refresh an already
enabled English overlay automatically. Saves/unlock conditions are retained.
Before/after pictures: Help -> Notes & changelogs -> Launcher updates.

RETAINED CONTROLS
Live HP/MP bars show current/maximum values across runtime-control tabs.
Navigation and controls have clearer spacing, rounded edges and hover highlights.
Ctrl+Shift+G opens/closes the panel by default; click the shortcut button in its
header to choose another combination. Your choice also works detached and is
saved per control session. The on-screen Controls button is always available.

HDR MONITORS / LOCAL LAUNCH OPTIONS
Close the game and add this top-level property to its root package.json:
  "chromium-args": "--force-color-profile=srgb"
Use valid JSON, with a comma after the preceding property. If chromium-args
already exists, append the option to that string. Keep the original main entry.
Normal and control-session launches accept local package.json preferences while
still verifying the native entry and game code/data. Original-source matching,
installation and reconstruction retain exact hash checks; keep original archives
for those operations. The launcher does not force sRGB on anyone else.

RETAINED UPDATER SAFEGUARDS
Updater: identical installed files are skipped; rollback copies cover only
changes. The verified staged EXE handles the update without an extra random
helper EXE. ZIP/payload verification is retained; rollback errors are reported.
This reduces unnecessary behavior associated with an Advanced Threat Defense
report. Bitdefender has not validated this revision; a vendor retest is needed.

ONE-TIME TRANSITION FOR BITDEFENDER-AFFECTED 1.0.19–1.0.21 USERS
The old updater still handles its first upgrade. Download the current ZIP
and extract it elsewhere. Close the launcher. Back up and replace ONLY these
three files in your existing launcher folder:
  Celesphonia Portable Switcher.exe
  EnglishRuntime.js
  README.txt
Keep English, profiles, settings, Control Sessions and game folders in place.
Other packaged assets are identical for this specific transition. Fresh
installs should extract the complete ZIP. Subsequent updates use the revised
updater. No AV settings are changed and quarantined files are not restored.
If required files were already removed, retain the alert for vendor review.

RETAINED IN 1.0.22
Maple and Three-in-One status panels have compact labels, aligned values
and fitted Clothing HP text. Native values, colors and mechanics are retained.
Help -> Notes & changelogs opens an illustrated, formatted reader with
before/after images, launcher history and separate game-developer notes.
Use the section list, mouse wheel or Page Up / Page Down; resize the reader
without changing the main launcher layout. All reading works offline.

UPDATING
Use Help -> Check launcher updates, or get the current ZIP from:
https://github.com/kitomisaitichi-design/celesphonia-launcher/releases/latest
After updating, close the game and use Play / Switch & Play once. An enabled
English overlay refreshes automatically; English-off games remain off.
Directly opening Game.exe / nw.exe bypasses the launcher refresh.

INSTALLING
Use Add an installed game for an existing game folder.
For a fresh Three-in-One Ver1.053 game, provide your own Gorpg 1.23 base and
creator Ver1.053 files in Install. Intermediate creator versions are not
required. ZIP/RAR/7z, renamed archives, extracted folders and individual
original files can contribute matching bytes. Missing files are reported.
Use the compatible forward-upgrade route to carry saves from an older
creator release. Gorpg, CH, Maple and Three-in-One have separate profiles.

ENGLISH AND OPTIONAL CONTROLS
English is partial and version-specific. Untranslated text remains visible.
Enable or restore English from the English page. Custom labels are retained.
The status layout and repeated-streaming repairs do not require cheats.
The Play-page checkbox enables runtime controls; Ctrl+Shift+G shows/hides the
panel by default. Change that shortcut using the button in the panel header.
Control Sessions use separate saves, copied from the game only on first use.
Later control-session and normal-game progress can differ. Use the matching
saves-folder button before manually transferring any save files.

BACKUPS / RECOVERY
Use Saves to back up progress. Managed upgrades keep the original game;
removal preserves saves and unknown files. English -> Restore original text
restores the backed-up entry page. Keep update backups until satisfied.

READING THE NOTES
Help -> Quick start guide: illustrated guide and this release's comparison.
Help -> Notes & changelogs -> Launcher updates: launcher patch history.
Game developer notes: separate Three-in-One announcements for v1.01, v1.02,
v1.03, v1.04, v1.051, v1.052 and v1.053, oldest to newest.
Verified Maple developer announcements are not currently bundled.
Launcher and game-mod versions are independently numbered.

Source / docs: https://github.com/kitomisaitichi-design/celesphonia-launcher
The ZIP contains no commercial game files or user saves. Exact localized
source builds require the private catalogs and reviewed translation inputs.
Unofficial tool, not affiliated with or endorsed by the game developer.
