# Celesphonia Portable Launcher v1.0.19

Repeated streaming now works with the runtime-controls checkbox OFF. Version 1.0.18 installed the repair only in F8 Control Sessions; this update also installs the same shared repair in the reversible partial-English overlay used by ordinary game launches. Existing enabled overlays refresh when using Switch & Play.

The repair releases the stale message pause left by MPP_ChoiceEX.forceClear() only when Map005's repeat-stream prompt and choices are waiting and the first-stream flag is set. Native unlock conditions, webcam requirements, first-stream events, repeat-stream branches, saves, and game data remain intact. Loading from both the overlay and an F8 session is harmless and applies the repair once.

Validation: reproduced the stuck native choice window without the repair; verified native choice-window activation and branch callbacks on Maple 1.7.2 and Three-in-One Ver1.053 with F8 controls disabled and enabled. Focused checks also cover first-stream and unrelated-message guards, duplicate loading, legacy-overlay refresh, package extraction and exact restoration. This is a native message-handoff regression check, not a full new-game playthrough.

The existing day/time display fix and all partial-English packs are retained. The download contains the launcher and translation packs; personal saves and profiles are excluded.
