CELESPHONIA MOD LIBRARY

LAUNCHER 1.0.2 — V1.04 WINDOW TITLE RESTORED + GITHUB RELEASES UPDATES
The v1.04 English dictionary now includes the translated Three-in-One
window title and C/A/TAB controls plus native save-slot captions,
while retaining every translation from the previous v1.04 package.
The game's save titles and executable identification strings remain native
internally for compatibility: only on-screen presentation is translated.
The v1.0.1 orphan profile and safe launch fixes remain included.
The Help page now has Check launcher updates and GitHub source. Choose a
trusted dedicated GitHub owner/repository to enable checks on startup. New
launcher releases must use launcher-vX.Y.Z tags and include both the portable
ZIP and matching .sha256.txt asset. Installation asks first, verifies every
file and uses a separate launcher helper with retained rollback; games,
saved profiles, English user translations and F8 sessions are untouched.
The public update source is kitomisaitichi-design/celesphonia-launcher.
The Help page offers version checks once this source is configured.
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
