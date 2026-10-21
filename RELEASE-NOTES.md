# Celesphonia Portable Launcher v1.0.20

**Updated the launcher but streaming still fails? Click Play or Switch & Play once in this version. The launcher now refreshes an outdated enabled English patch automatically before starting either a normal game or an F8 Control Session. You no longer need to remove and reapply English.**

Version 1.0.19 bundled the normal-mode streaming repair, but Play did not invoke the existing overlay update detector. This release connects both launch paths to one shared pre-launch refresh. Older enabled patches receive the current runtime and compatibility assets; current patches are left untouched. Games with English disabled remain disabled. Custom translations, native game files, save folders and save slots are preserved. Modified or unverifiable patches still produce an explicit error instead of being silently replaced.

This changes the launcher's automatic update behavior. The existing first/repeat-stream prerequisites, streaming repair, day/time fix, partial-English dictionaries and runtime controls are retained. Directly opening Game.exe bypasses launcher checks; use the launcher once after an update to refresh the installed patch.

Validation covers automatic migration of stale enabled overlays, no-op checks for current overlays, preservation of disabled overlays, custom labels and saves on Maple 1.7.2 and Three-in-One Ver1.053, plus the existing extracted-package apply/restore cases. The game/runtime JavaScript and English packs are unchanged from v1.0.19.
