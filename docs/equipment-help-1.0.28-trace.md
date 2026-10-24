# Equipment help display repair, launcher 1.0.28

Follow-up 1.0.29: the user specified the established Kagura/CH56.6 abbreviations ATK, DEF, MAT, MD and AGI while 1.0.28 was already tagged. The corrected immutable follow-up uses MD in compact equipment labels, including existing MDF stat-down captions. No stat IDs or mechanics change. Current native checks use the corrected 1.0.29 runtime; the original 1.0.28 screenshots remain in its gallery, and a separate native before/after pair records the MD correction. Both versions' dictionaries/catalogs remain unchanged.

Reported against Three-in-One Ver1.061 and launcher 1.0.27; checked separately in native Maple 1.7.2. This is a shared display-runtime correction, not a dictionary rewrite or a game-data patch.

## Native source and cause

`js/plugins/ItemDescription.js` supplies `Window_Base.descriotion`, `weaponDescriotion` and `ArmorDescriotion`. The native formatter prefixes the item's original description and appends type, parameter, xparam, element and state fields. States 118/119 are named 敏捷下降（小） / 敏捷下降（大）; their native AGI multipliers are 0.85 / 0.70. Both English dictionaries already contain reviewed whole labels. Previously the composed bracketed string missed that whole-label lookup.

The old help hook depended on translation changing the immediate draw argument. Some assembled or partly translated arguments bypassed fitting. Three-in-One's `LiuYue_WindowHelpEx.refresh` also draws each source line separately, so per-call wrapping cannot lay out the whole panel consistently. Native baseline Ninja Hood (armor 153) reached 1712px in a 988px content area in Ver1.061 and 1814px in a 1244px area in Maple. Ver1.061 Demon King Core (armor 408) still exceeded its 72px height at a 12px font.

## Scoped repair

The runtime recognizes actual weapon/armor selections through `Window_Help.setItem` and confirms the current text is that item's native formatter output. It translates the original complete description before the appended tail, then resolves reviewed whole bracket fields while keeping escape arguments and plugin tags opaque. Native database strings and generated `_text` stay unchanged.

Full wording at the native font is preferred. Only overflowed help receives semantic abbreviations for recognized stat/trait phrases: ATK, DEF, AGI, MAT, MDF, PHYS-EVA, CRIT-EVA, MAG-EVA, MAG-REF, PHYS-HIT, Regen, Res. and Elem. Small/minor and large/major stat-down traits retain S/L labels. Element names stay readable; no icon or elemental meaning is guessed.

Dense descriptions use a larger content bitmap behind the unchanged native help viewport at a readable font of at least 20px. Wheel movement over that panel adjusts only its origin; native up/down arrows show available text. The existing native `Window_Selectable.processWheel` proceeds normally outside scrollable help. Selecting another item resets the help position; empty/non-equipment text restores ordinary help behavior. No activation key is added.

## Evidence and limits

`Tests/NativeEquipmentHelpFixture.js`, run through the existing `Tests/RunNativeTextMemory.py`, uses the real engine, native data/formatter/renderers and isolated synthetic game objects. Optional controls are off. No user save is loaded or written. Before images use the frozen 1.0.27 runtime; after images use the candidate runtime. Each pair keeps the same record, dimensions, font assets and native window skin. Images are rendered native windows, including their clipping and arrows.

- Ver1.061: 36 focused checks; all 431 named weapon/armor records have complete reachable help at 20px or larger. 108 dense records use scrolling.
- Maple: 39 focused checks; all 423 named weapon/armor records have complete reachable help at 20px or larger. 66 dense records use scrolling.
- Interaction checks cover help scrolling, returning to the top, wheel routing to the item list outside help, empty selection, repeated selection and refresh.
- Selected native records check unchanged numeric tokens and display codes. Native database/runtime-state snapshots and the runner's original source hashes remain unchanged.
- The first interaction probe exposed an undefined wheel predecessor. Both native engines were inspected, the guarded hook corrected to `processWheel`, and the native check rerun successfully. No failure was suppressed.

All reviewed dictionary/catalog files remain byte-identical to 1.0.27. Existing partial-English gaps remain; this change does not claim a complete translation. Older supported versions inherit the guarded shared equipment path where their native formatter applies, but only Maple 1.7.2 and Ver1.061 received current native equipment replay. Full gameplay routes and every screen resolution were not tested. Installation and package/publication receipts remain in the task's results record; native UI replay is separate from those checks.
