# Celesphonia Portable Launcher v1.0.18

Fixes the missing second-livestream menu in Three-in-One Ver1.053 when running through the launcher's F8 Control Session. Previously the bedroom PC accepted **Stream**, then showed a blank laptop and advanced time without opening the stream-type choices or playing the event.

Native desktop reproduction traced the cause to a paused RPG Maker `Window_Message` left by `MPP_ChoiceEX.forceClear()` after selecting Stream from the PC's help-enabled menu. The next common event (CE69) correctly queued Chat, Game, Singing, and other stream-type choices, but the old pause prevented the new choice window from starting. The narrow runtime repair releases that stale pause only for the pending Map005 repeat-stream selection, allowing the native event to continue. No event database, gameplay item, quest flag, or saved game is modified.

Verified against the actual Ver1.053 Control Session test slot 13: before the fix the second-stream menu was invisible; after the fix it appeared with its five choices. The stream event had already been independently tested with native frame-driven chat/comments and game-picture sequences. Tests also cover seven conditions to ensure first-stream, other-map, and unrelated message windows are not changed.

The fix is now embedded in the portable EXE and regenerated Control Sessions. Both supported game profiles, English packs, and existing F8 controls remain present. The v1.0.18 portable package passed 12 extracted ZIP install/repair/restore checks for Maple 1.7.2 and Three-in-One Ver1.053, plus the existing 11-pack and launcher UI validations. Language translation coverage remains partial.

Download includes the launcher and English packs only. Personal save files and launcher profiles are not distributed.
