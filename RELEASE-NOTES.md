# Celesphonia Launcher v1.0.6 — Three-in-One Ver1.051 + Ver1.052

**Latest creator mod: Ver1.052.** This launcher update does **not** bundle the creator's game or mod files. Select your own original RAR/ZIP or extracted folder in the launcher.

- Adds independent, verified upgrade paths **v1.04 → Ver1.051**, **v1.04 → Ver1.052**, and **Ver1.051 → Ver1.052**. Each new game is installed alongside the older version and existing saves are copied forward, never overwritten at the source.
- Adds exact archive/content fingerprints for both releases. The creator's Ver1.052 archive internally uses a Ver1.051 folder name, so detection additionally checks `data/Enemies.json` to prevent confusion. Supports Chinese-named RAR5 archives, ZIPs and extracted game folders without requiring external 7-Zip.
- Ver1.052 fixes boss-class enemy restraint effects not activating. The verified Ver1.051 → Ver1.052 overlay changes only `data/Enemies.json` (22 enemy records); the remaining 5,162 gameplay files have identical hashes. Developer changelog and source/regression audit are included in the GitHub source repository.
- Provides **34,996** source-exact English text entries for **each** release, including nine repaired Skill Engram description lines, six native choice captions whose condition guards are stripped before rendering, controller menu labels, Tainted/Wicked Orbs, and Shamelessness text.
- Fixes historical v1.04 English runtime-label preservation. Retains all previous supported creator versions, Maple 1.7.2, Gorpg 1.23, CH reference, and prior-version saves/rollback support.
- Reduces total package size without losing source catalog content: 550,547 catalog keys across ten English packs are preserved, with redundant source strings reconstructed in-memory. Package remains compatible with the older v1.0.2 auto-updater's uncompressed file-size ceiling.
- Updates Help to latest **Ver1.052** and clarifies that game-mod versions are independent of launcher version 1.0.6. No private game data, saves, local QA copies or developer creator archives are shipped.

**QA:** verified 5,163 creator overlay file hashes, both upgrade routes and copied saves; native English apply/restore; source-only boss change comparison; launcher Play to title/real-save item menu; full ten-pack portable release ZIP extraction, application and restore checks; old launcher auto-update compatibility. Newly gated late-game mechanics (boss restraint in a live battle and Shamelessness >80 home menu) require appropriate game progress for full live-path validation and are not claimed independently tested.

**Update delivery:** existing launchers with auto-update checks will discover v1.0.6 on GitHub. Installing an update still requires user confirmation; the launcher does not silently replace its executable.

**Known history:** launcher v1.0.3 has a historical updater packaging incompatibility and needs a manual update once. Launcher v1.0.2 and v1.0.4/v1.0.5 use the validated format.