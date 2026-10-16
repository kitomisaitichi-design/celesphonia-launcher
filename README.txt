CELESPHONIA MOD LIBRARY

LAUNCHER 1.0.11 -- GLOSSARY ILLUSTRATION LAYOUT
The three illustrated help entries now measure wrapped English text before
placing their images. Illustrations keep their aspect ratio, fit the panel,
and leave most of the space for text. Text-only and image-only pages retain
their appropriate layouts. Original pictures and game records are unchanged.

LAUNCHER 1.0.10 -- RESTORE BOTH BLINKING INTERACTION MARKERS
The complete narrow wardrobe now renders below native event indicators,
so the lower Change Clothes marker blinks normally again. The second
action remains above the wardrobe. Saves and native game data are unchanged.

LAUNCHER 1.0.9 -- SEPARATE BEDROOM INTERACTIONS
Ver1.051 / Ver1.052: the second wardrobe action is above the wardrobe,
separate from Change Clothes. Complete cabinet artwork is fitted into the
existing narrow 1x3 footprint. The transparency/coat menu stays available after the creator
recalculates Shamelessness. This does not grant skills or change its value.
The correction is part of the reversible English overlay. After updating,
apply/update English on the game's English tab and restart the game.
Existing saves, translated text, all five English images and F8 controls
are retained. Runtime controls include resource Set/Lock/Restore repairs
and the startup movement fix (controls version 1.1.4).

LAUNCHER 1.0.8 -- ORIGINAL BEDROOM WARDROBE AND ENGLISH HELP
The home-bedroom wardrobe remains the creator's original NARROW 1x3 tile
column, exactly as in v1.04, Ver1.051 and Ver1.052. Neither the English
runtime nor its image-localization hooks may turn it into a 2x3 cabinet.
Both native interaction points remain at their authored coordinates:
change clothes and the Shamelessness-unlocked transparency/coat choices.
Regression checks verify the native tiles and those event positions.
All five translated glossary/tutorial/help images and the hypnosis captions
remain installed and reversible. Ticker translations, text-fit fixes, all
supported creator versions and save data remain unchanged.
The launcher ZIP keeps the same 35-file auto-update format.

LAUNCHER 1.0.7 -- ENGLISH HELP IMAGES, GROPER TICKER AND TEXT FIT
Reversible English image overlays now localize the native Glossary battle
restraint examples, item/gear examples, Change Color controls and hypnosis
animation captions on exact-verified Ver1.051 and Ver1.052 source images.
Native game pictures, animations, save files and gameplay data stay untouched.
English ON installs replacements inside www/celesphonia-english/images;
English OFF restores the original index.html entry point and native artwork.
The groper/aged-man comment HUD now translates using the exact existing
reviewed Chinese source keys even where the native Ticker plugin bypasses
ordinary English text hooks; game variables and stored text remain original.
Item help and tooltip layouts also wrap adjacent effect tags without clipping
or shrinking every line, and account for inline icon width.
The translated artwork is embedded in the launcher executable, maintaining
the historical 35-file ZIP allowlist for older GitHub auto-updaters.

LAUNCHER 1.0.6 -- THREE-IN-ONE 3.3.5 VER1.051 AND VER1.052 + ENGLISH
Supports verified independent upgrades from v1.04 to Ver1.051 or Ver1.052
using the corresponding creator RAR5 bundle. Installs a NEW independent version, preserves the v1.04 game,
backs up originals, and carries compatible saves forward only.
All 5,163 game overlay files are source-hash checked; exactly 18 data/JS
files change, without any new or deleted game overlay files.
Both Ver1.051 and Ver1.052 English packs preserve all 34,996 reviewed
display strings, including nine Skill Engram descriptions, six conditional
choice labels and historical runtime-display translations.
Original internal game identifiers/scripts, source archives and saves remain
unchanged; rollback and English ON/OFF/restore remain supported.
RAR5 extraction is now bundled in the launcher so long Chinese creator archive
names no longer require a separate 7-Zip installation. 7-Zip extractor © Igor
Pavlov, LGPL/unRAR-restricted components; licensing information and upstream
source are documented in Vendor/SevenZip/License.txt in the GitHub repository.
The launcher automatically checks for updates from the official GitHub
release, then asks for user approval before installing. Prior version
dictionaries and upgrade routes remain supported.

LATEST CREATOR VER1.052 -- OCTOBER 16, 2026
Boss-type enemy restraint effects now take effect. Ver1.052 changes only
data/Enemies.json versus Ver1.051; all 5,162 other overlay files are the
same. The launcher hashes that enemy file to distinguish the two versions,
even though Ver1.052's archive still contains a folder named Ver1.051.
Start from verified v1.04 and choose Ver1.051 or Ver1.052; each installs
independently and preserves original games/saves. Direct Ver1.051 ->
Ver1.052 is also supported as a forward-only upgrade.

LAUNCHER 1.0.5 — V1.04 ENGLISH RUNTIME LABEL PRESERVATION
The game-mod v1.04 English pack restores 472 verified historical render-time
translations omitted from v1.04's fresh source inventory, including native
battle/menu labels, status labels and previously observed dynamic displays.
Changed old dialogue, database values and story descriptions are deliberately
excluded: v1.04 keeps its own 21 newly reviewed source strings.
All retained v1.03 translations remain unchanged. Translation is still a
reversible display overlay; source scripts, game data and saves are untouched.
The 29-file distribution and safe confirmation-based GitHub updater remain
compatible with launcher v1.0.4 and earlier supported updaters.

PREVIOUS LAUNCHER 1.0.4 — BACKWARD-COMPATIBLE GITHUB UPDATES
The game-mod changelog is included in this README and embedded inside the
launcher EXE. Help → Game changelog exports its complete Markdown original
to GAME MOD CHANGELOG.md when needed. No extra ZIP entry is required, so
existing v1.0.2 launchers can safely verify and install this update.
The v1.0.3 ZIP's extra Markdown entry was rejected by older updater
allowlists; 1.0.4 restores the original 29-file distribution structure.

PREVIOUS LAUNCHER 1.0.3 — OFFICIAL AUTOMATIC UPDATE CHECKS
Fresh portable downloads automatically know the official GitHub source:
kitomisaitichi-design/celesphonia-launcher, with startup checks enabled.
The launcher notifies when a newer stable release is available and asks
before downloading/installing it. Help → GitHub source allows opt-out or
a custom repository; existing choices are preserved without overwriting.
Help → Game changelog opens the embedded GAME MOD CHANGELOG.md containing
English creator notes for Three-in-one 3.3.5 v1.01 through v1.04.

PREVIOUS LAUNCHER 1.0.2 — V1.04 WINDOW TITLE RESTORED
The v1.04 English dictionary now includes the translated Three-in-One
window title and C/A/TAB controls plus native save-slot captions,
while retaining every translation from the previous v1.04 package.
The game's save titles and executable identification strings remain native
internally for compatibility: only on-screen presentation is translated.
The v1.0.1 orphan profile and safe launch fixes remain included.
The Help page has Check launcher updates and GitHub source. The official
trusted repository is preconfigured on fresh downloads. New
launcher releases must use launcher-vX.Y.Z tags and include both the portable
ZIP and matching .sha256.txt asset. Installation asks first, verifies every
file and uses a separate launcher helper with retained rollback; games,
saved profiles, English user translations and F8 sessions are untouched.
The public update source is kitomisaitichi-design/celesphonia-launcher.
The Help page offers manual checks and an option to disable startup checks.
Orphan entries for missing/incomplete game installations are now excluded
from the game library. The launcher also rejects an installation that
disappears while it is open. Saved profile records and game saves are not
deleted, so moved folders can reappear when restored. Fully independent
mod installations no longer require a historical deleted base folder for
their optional runtime-control asset lookup.

V1.03 / V1.04 ARCHIVE RECOGNITION
Both creator archives in Telegram Desktop are RAR format, even if their
filenames do not end in conventional .rar. The launcher checks their real
signature and SHA-256, and validates all 5,163 overlay files before upgrading.
v1.03 to v1.04 is an accepted FORWARD upgrade when the v1.03 game is installed.
RARs are overlay archives, not independently playable games. The archive
extractor now uses a short private temporary folder to avoid legacy Windows
260-character path failures. Source game, saves, and archives are preserved.

THREE-IN-ONE 3.3.5 V1.04 (OCTOBER 10, 2026)
The verified creator v1.04 RAR in Telegram Desktop is now supported alongside
trial, v1.01, v1.02 and v1.03. Files / Install offers a separate Upgrade &
carry saves route; it verifies all 5,163 overlay files, creates an independent
game copy, and backs up and copies older saves without overwriting any existing
game. Downgrade saves are blocked. v1.04 has 12 changed files versus v1.03.
Existing F8 runtime controls work in a private per-version session.
The v1.04 English pack preserves all 20 new developer text keys in English
(18 new translations, 2 exact prior matches) and carries forward thousands of
other exact translations. Pre-existing untranslated dialogue stays original.
See UPGRADE PATHS.md for version and hash details.

Run Celesphonia Portable Switcher.exe. No launcher installation is needed.
This package includes the launcher and English display packs; game files and
mod installation archives must be supplied separately.

FINDING INSTALLATIONS FROM ANOTHER LAUNCHER COPY
All supported versions are remembered for the current Windows user. A fresh
launcher extracted elsewhere checks those verified locations before searching
other folders, and uses the existing game's files and save folder.
The location index lives in %LOCALAPPDATA%\Celesphonia\Launcher\installations.json;
it is created on your machine and is not included in this ZIP. Missing or
changed installations are checked again before they are added to the library.
Each launcher keeps its own selection, preferences and runtime-control checkbox.
Control session saves remain in the launcher copy that created that session.

RUNTIME CONTROLS (SOURCE GAME UNTOUCHED)
Choose a registered game, open Play, tick Enable runtime controls for this
game, then Switch & Play. Untick it for a regular launch. Press F8
or click Controls in the game. The source game entry/data/scripts are only
read; panel files and settings live in Control Sessions/<profile>. The game
runs its native scripts with a launcher-owned entry page and original assets.
Your saves are copied once into this session's own save folder. Saving edited
values writes there; the source save folder is left alone. Use Control session
folder to find these saves. The checkbox is saved separately for each registered game.

VALUE EDITING AND RESTORATION FIX (RUNTIME CONTROLS 1.1.3)
Typed values remain in the field when clicking Set; live refresh resumes after
a successful edit. Set HP/MP/SP and Restore bypass native resource limits for
the edited resource during this session. Damage and skill costs still apply.
Lock holds a value. Release all locks, load a save or restart to resume native
resource limits. Quick has separate Restore HP, Restore MP / EP and Restore SP
party buttons; Actors has Restore beside each resource for the selected actor.
Full restore fills all three resources and clears temporary effects and buffs
while retaining permanent states. Native variable names/formulas remain intact.

ENGLISH AND SAVE ACCESS FIX (LAUNCHER 1.1.2)
Runtime controls now preserve the game's enabled English display overlay.
The private launch page uses the verified active entry instead of the
pre-translation backup. English stays off when disabled. Existing English
packs, original game files and both save sets are retained.

SAVE ACCESS FIX (RUNTIME CONTROLS 1.1.1)
This release fixes the MV save-directory redirection used by native readers,
writers and the save-slot index. Existing copied saves are recognized when
runtime controls are enabled. Close the game and launcher before updating;
extract into the same portable folder and keep Control Sessions and profiles.json.
Original saves and copied session saves are retained. No save conversion is needed.
Saves created under an earlier session's app/save folder are also retained;
do not delete that folder if it contains progress. Keep a backup when importing
those legacy saves into the session's save folder to avoid replacing another slot.

Quick offers money and locks, miasma, sensitivity, souls, domination, lust,
corruption, fame, public/school reputation and hypnosis.
Each quick control has Choose ID for a mod-specific mapping. Where a version
has duplicate named variables, choose the correct ID. Variables
and Switches show searchable English/original names, current values, Set/Lock
and per-ID display aliases. Inventory has items, weapons and armor quantities,
add/remove and locks. Actors has HP, EP/MP, SP/TP and maximum/value locks, level,
runtime maximum HP/MP/SP overrides, permanent parameter bonuses and equipment.
Skills learns/forgets skills. Experience lists sexual experience, development
and level variables with Set/Lock controls. States applies/removes effects or
holds them present/absent. Settings can remember state Keep rules per profile.
These rules reactivate on load/new game only when explicitly enabled. Maps
offers a searchable map dropdown with parent hierarchy, current-map highlight,
selected destination and tile coordinates. Go to map queues the native transfer.
Quick also has noclip, click-to-cursor teleport, enemy HP held at 1, win/lose
battle, save/load menus and full party restoration. Restoration removes buffs
and temporary states, preserves permanent states and fills HP/EP/SP. Native
resource caps can limit one-time restoration; resource locks override those
caps while enabled. Arbitrary map transfers may skip native entrance events.

Choose Midnight, Violet, Ocean or Daylight from the panel top bar. The visible
Opacity slider controls transparency. Hold the left mouse button on the header
to drag; releasing it stops movement. Resize using the bottom-right corner.
Detach opens a separate controls window; Dock back returns it to the game.
Closing the detached window docks the panel without closing the game.
Catalogue lists automatically append the next 40 matches as you scroll.
Inventory/equipment Details and hover cards show descriptions, bonuses and traits. Settings offers corner placement and reset size/position. At narrow
widths navigation becomes a scrolling horizontal strip. Launcher themes are
under Help > Launcher appearance and are saved independently. Locks/toggles start OFF every launch and clear
when loading or starting a new game, except explicitly remembered state rules.
Maximum overrides are session-only; native levels, parameter bonuses, skills
and variable/state edits persist when saved. English display labels use the
matching offline dictionary, local label cache and bundled terminology. The
launcher builds a separate index for variables, switches and maps, with original
names and IDs retained for reference. It makes no online translation requests.
Settings shows coverage and allows importing offline source-name/English-name
JSON dictionaries. Unknown text remains native until a matching entry is supplied.
String values may show a separate English preview; the editable value stays native.
Aliases change only the panel. Equipment plugin metadata is shown as supplied. No complete translation or unknown-version compatibility is
guaranteed. Add an extracted RPG Maker MV Celesphonia installation for new
versions; encrypted/packed entry pages and unusual custom loaders may differ.

PLAY
Choose an installed version on the left, then use Play > Switch & Play.
Use + Add an installed game to register an existing game folder.
Folders opens that version's game, saves, plugins and other available folders.

ENGLISH
Select your version, open English, and choose Enable / update English.
The translation is partial. Unmatched text stays in its original language.
Use original text restores the backed-up entry page. Game data and saves retain
their native IDs. You can import translated JSON or export remaining text.

INSTALL AND SAVES
Files shows installed versions and detected archives. Install explains which
original bundle or patch pack is needed. Each version keeps its own saves.
Save and close the game before switching, updating English or transferring
saves. Cross-version saves are copied, not converted. Keep your originals.
The 3.3.5 v1.03 upgrade accepts v1.02, v1.01 or trial and copies saves into a separate
game folder after making a backup. Newer saves cannot be moved backward.

UPDATING THIS LAUNCHER
Close the launcher, then extract the updated ZIP into the same portable folder.
Keep profiles.json, custom English translations, Runtime, Game Profiles and
Save Backups and Control Sessions. The distributed ZIP contains no personal settings or save files.
The launcher carries its matching English packs and display runtime internally,
so missing or stale loose pack files cannot mix dictionary releases. Custom
translations remain separate. After extracting, use Enable / update English
to refresh an enabled game's overlay; the launcher refreshes its status when
you return to the English page.
On smaller screens, scroll the launcher to reach all controls.

PORTABILITY AND LAUNCH
The library opens immediately while discovery runs in the background. Play
remains available for installed games during discovery. Search includes the
current Windows user's download folders, Steam libraries and common local
game folders, to eight nested levels within the bounded scan. Add a search
folder for games elsewhere. No personal profiles are included in this ZIP.
Games, saves, runtime and search folders inside this portable folder are
stored relative to it when settings are saved, so they survive moving it.
Games outside the portable folder remain local installations; rediscover or
add them on another PC. Launch shows verification and backup progress, and
records startup failures in launch-status.json. Full asset checks run during
installation; Play verifies native code/data and runtime dependencies.

NEARBY FILE DETECTION
Direct files in the launcher and search folders are checked before deeper
folders. Results appear as discovery runs, and remain available if stopped.
ZIP/RAR/7z and split archives use signatures, normalized filename/version
hints and known byte sizes. Large known archives are fully verified when
installing, rather than hashed before appearing in the list. View found files
shows the actual filename/path, size and matching evidence.

CREATOR UPDATE 1.03
Three-in-one 3.3.5 v1.03 is recognized by archive signature, creator filename,
byte size and native version fingerprints. The author reports a fix for
monster births. Upgrade installs an independent copy; the previous game and
saves are retained. Existing exact English text and runtime controls carry
forward. Choose v1.03 in Install and use the detected archive or extracted
bundle. Saves migrate forward from trial, v1.01 or v1.02.

The latest uninstalled update appears at the top of the left game list. Click
its Update available row to open the matching detected-bundle upgrade route.
After installation the row becomes a normal playable game with its own saves.

CREATOR CHANGELOG — ENGLISH TRANSLATION
See GAME MOD CHANGELOG.md beside this README for the complete English
developer notes covering Three-in-one 3.3.5 v1.01 through Ver1.052,
including full-release synchronized changes #200, #201 and #203-206.
These game-mod releases are separate from launcher v1.0.7.

======== ORIGINAL ENGLISH CREATOR CHANGELOG ========
# Magical Girl Celesphonia — Three-in-One 3.3.5

## Creator's English changelog (releases v1.01–v1.052)

These are English translations of developer **Tian Ying (天影)**'s announcements
dated October 3–16, 2026. They describe **game-mod versions**, not the Windows
launcher versions. The October 3 announcement repeated the first v1.01 change
in consecutive messages; it is listed once below.

### v1.01 — October 3, 2026 (11:19–11:20 PM)

1. Removed placeholder entries for outfits and hairstyles unavailable in the trial version.
2. Fixed the equipped weapon reverting to the starting sword when interrupted during the fight against Tatar.
3. Fixed defeating the Demon Womb not unlocking the complete Recollection Gallery.

### v1.02 — October 5, 2026 (1:13–1:14 AM)

**Synchronized with the full release — change #200**

- Added a **Storage Chest** at home for depositing and withdrawing items.
- When carrying the **Gathering Drone**, collected items exceeding the inventory limit are automatically transferred to the Storage Chest.

### v1.03 — October 5, 2026 (10:59–11:02 PM)

1. Fixed a bug that prevented the player from properly giving birth to Monstrums.

### v1.04 — October 10, 2026 (11:23 AM)

1. Fixed dialogue errors involving the pantyhose-buying pervert in the Downtown district.
2. Clarified the item description for the **Offensive Magic Barrier**.
3. Fixed the display of certain enemies when the **Invisibility Ring** is equipped.
4. Corrected the battle-end EXP display to show the experience **Amane actually receives**.
5. Made the **Maid Headband** obtainable.
6. Fixed a character-position conflict involving Photography Club members during dialogue between Mashiro's storyline and the main storyline.
7. Weakened the skills used by **Cursed Monster: Tatar** and **Regenerated Monster: Tatar**.

**Synchronized with the full release — change #201**

8. In the **Recollection Room**, automatic transformations during recollection scenes no longer incur the special transformation cost. Players can also choose the degree of outfit damage.

### v1.051 — October 16, 2026

1. Fixed **Succubus's Contract Kiss** sometimes having no effect, and substantially increased kissing priority.
2. Greatly increased the speed at which **monster pregnancies** progress to giving birth.
3. The **transparency effect** now works both while transformed and while inside dungeons.
4. Fixed the first conditional event after sleeping sometimes incorrectly requiring **Heat 100**.
5. Fixed the older client sometimes failing to show up when the **Prostitution App** had been unlocked through a special method.
6. Fixed incorrectly displayed **nipple rings** when the battle outfit was damaged.
7. **Permanent Slave Contract** and **Fixed Lewd Crest** are now displayed in the menu.
8. Revised several descriptions in the controller button settings and added controller assignments for **Detailed Information**, **Transformation** and **Smartphone**. The creator does not have a controller to test these bindings and cannot guarantee that they work correctly.

**Synchronized with the full release — changes #203–206**

203. Crafting items at **Yarus** no longer costs money.
204. Added **Tainted Orbs** and **Evil Orbs**, craftable at the **Inner Forge** in the Forbidden Library.
205. All boss-type enemies gain an effect that prevents their restraint preparation from being interrupted.
206. Added the **Shamelessness** concept. It increases only through erotic achievements. Above 80, the player can choose at home to go without a trench coat or stop suppressing the transparency lewd curse.

### v1.052 — October 16, 2026

1. Fixed an issue that prevented **restraint effects from taking effect on boss-type enemies**.

---

## Source and terminology

Translated from the author's Telegram messages supplied October 10 and 16, 2026,
and cross-checked against the original Chinese `三合一体验版修复日志.txt`
included in the creator archives. The developer's original numbering
**#200**, **#201** and **#203–206** denotes changes inherited from the full release; these
are not extra game-mod versions.

Game terms follow the existing English translation where available:
**Amane** (天音), **Mashiro** (真白), **Demon Womb** (恶魔母胎),
**Gathering Drone** (采集无人机), **Storage Chest** (储物箱),
and **Maid Headband** (女仆发箍).

The source's `繁华街连裤袜售卖变态` refers to the pervert who asks
to **buy the player's pantyhose**; hence “pantyhose-buying pervert.”

This is an unofficial English translation of the creator's game-mod notes.
