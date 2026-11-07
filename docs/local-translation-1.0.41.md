# Local PLaMo translation — launcher 1.0.41

## User workflow

1. Select an installed game and open **Local TL**. No curated English pack is needed.
2. **Manage models** installs/repairs 0.8B or 2B. CPU works without CUDA. Optional Vulkan requires a compatible card/driver and was not tested in this release.
3. **New run** chooses a name, target language and categories. Names default to original. The model detects source language; only the target is selected.
4. Start processing. Indexing reads native game JSON and supported plugin/event text, then an owned worker translates off the launcher/game thread. Progress counts source/context records, not unique visible sentences or a guaranteed percentage of the entire game.
5. Enable **Use this run in game** and launch normally. Partial, paused and completed caches are usable. Local selection takes precedence over curated English without changing its saved setting. Runtime controls are not required.
6. Pause/resume processing separately from display. Switch local runs live; switching between local and curated English mode requires relaunch. If disabled during a local session, text falls back to the original until the next launch restores English. Existing open windows other than help may need reopening to redraw.
7. **Details** exposes processing errors, unreadable data files and a measured average/remaining estimate. Retry is bounded; failed text remains original. **Delete run** recycles only that run after its worker stops. **Remove model** keeps caches and the shared backend.

## Architecture and safety

This is a hybrid using the launcher's Celesphonia adapters. Harness 0.16.1 was inspected as reference; it is not vendored and its Python/manual-review pipeline is not required. No new package manager or service is required.

`LocalTranslation.cs` owns optional model/runtime setup, atomic state, entry composition, run binding and guarded removal. `LocalTranslationWorker.cs` indexes supported display fields, reuses the existing Maple decoder and calls the owned localhost inference server. `LocalTranslationRuntime.js` renders translated copies at native display boundaries. `LocalTranslationPage.cs` supplies the themed workspace and dialogs.

Run IDs are immutable. Cache reuse requires the same game, source text/control/context key, language, model hash and prompt version. Scope affects indexing and display. Journals append completed work; checkpoint manifests are constant-size and the game tails bounded UTF-8 chunks. Interrupted final journal records are recovered before appending. Rendering never waits for inference or alters native database values.

Native event conditions and choices are evaluated before display substitution. Escape/control tokens and numeric sequences are automatically checked; incompatible results are retried within a fixed budget, then left original. This protects machine-readable structure, not semantic accuracy. Ambiguous contextless matches stay original; unseen supported display strings are queued for later processing.

Entry changes are reversible and coordinated with English overlay ownership. The exact underlying entry is retained and hash-checked. Unknown external edits are preserved with an error. English refresh, removal and reapplication compose the local layer instead of overwriting it. Saves, profiles, creator files, English dictionaries and imported corrections are not local translation storage.

Storage: `%LOCALAPPDATA%\Celesphonia\LocalTranslation` contains Models, Runtime, Runs, Bindings, Incoming and Status. The public ZIP contains none of these personal files or weights. Captured display text stays local; model/runtime downloads contact official hosts. Runs belong to a particular installed game path; they are not automatically carried to another installation.

## Pinned optional sources

- [PFN 0.8B GGUF](https://huggingface.co/pfnet/plamo-3-translate-0.8b-GGUF), Q4_K_M, 573125952 bytes, revision `0c4af2b003fdd94b6868aa955414c1c86ee96997`, SHA-256 `73a1d942d8ab1e654d83898d997b8b4808c32ba669b378df612230989d18f079`.
- [PFN 2B GGUF](https://huggingface.co/pfnet/plamo-3-translate-2b-GGUF), Q4_K_M, 1807202144 bytes, revision `d44589ecd5287cd64452e9cd1e2cfdad904fe216`, SHA-256 `2189a8a0b4b359f59443ec36cfb9c9c3f603a678c00dbcee8598b3f7ea7c12fb`.
- [llama.cpp b12189](https://github.com/ggml-org/llama.cpp/releases/tag/b12189), official Windows x64 CPU/Vulkan packages. CPU ZIP SHA-256 `f912ea5e3e397c63405c81e732131ab0fa6e72411c80546bb5ad643f7dd82aac`; Vulkan `9d56405579492423833cdde94cf46cae15d38333f585f3957cafebfc6fb0e182`.

Model use is subject to the linked PLaMo Community License; llama.cpp has its own upstream license. Downloads are optional, pinned and resumable. Setup and removal share a cross-process lease. Weights stay installed after this release's tests; disposable test copies and proof runs are removed.

## Evidence and limits

Real CPU inference: both models, three calls each, English/French including protected HP values and game controls. Lifecycle: 13 focused checks for pause, resume, compatible reuse, language separation, partial selection, journal recovery and run deletion. Real 0.8B uninstall retained caches and restored the verified model without redownloading. Native latest Three-in-One 1.07 and Maple 1.7.2: ten checks each, including partial French display with runtime controls off, English renderer exclusion, original fallback, unchanged databases, fitting and scrolling to the final line. English off/on and local off/on also passed against both installed games.

Native UI checks cover normal/minimum window sizes, action bounds, no redundant horizontal scroll and the model manager. The unmodified English layout/native extraction checks are retained for release packaging. Native test fixtures and reports identify their source/runtime hashes; images in launcher notes are actual captures.

The automatic models are not the curated English translation. The French sample visibly contains imperfect terminology. Images/OCR, all dynamically generated/plugin-specific surfaces, every language's quality, every DPI/font/RTL case and Vulkan are not proven. Unreadable files are listed in Details, and uncached/unsupported material remains original. No full gameplay or complete-game translation claim is made. Network-interruption/occupied-port/low-disk/error paths are implemented but not all fault-injected on this PC.
