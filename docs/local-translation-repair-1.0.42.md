# Local dictionary/adapter repair — launcher 1.0.42

## Cause and correction

The 1.0.41 local renderer wrapped Window_Base.convertEscapeCharacters globally. DTextPicture uses that converter for both visible text and technical plugin arguments. A captured `black → noir` entry therefore contaminated a background color and caused CanvasGradient.addColorStop to throw. It also replaced the established renderer with a reduced implementation that omitted prior layout/field fixes.

The local script is now a dictionary provider. Both modes execute the same EnglishRuntime.js adapter source, with a provider boundary. EnglishSupport.PrepareDisplay is shared too: conditional-choice bodies, notification/name aliases, actor/variable grammars, labels, descriptions, enemy names and prefixes are derived using the existing catalog rules. Compiled grammar indices refresh when local batches/overrides arrive. Rendering uses the existing calendar, name/variable, dynamic-picture, notification, objective, memory, title, skill, imprint, enemy, glossary and equipment adapters rather than a second implementation.

Local translation/capture is excluded from non-rendering escape conversion. Technical color, alignment, font and command parsing remain native. Dynamic pictures receive only a temporary display copy. A previously translated saved picture color can be recovered from its original cached color mapping in that copy; native save state is not rewritten.

## Deliberately bounded coverage

EnglishSupport.CoveredLocalSources derives the eligible set from exact source keys already translated by the curated English overlay, with catalog categories. It excludes untranslated catalog material, arbitrary plugin/JSON literals and unknown live strings. A matching English-pack verification toggle is not required: known installed versions use their covered set; unidentified installations use the already-covered set rather than opening a broader scanner. All replacement remains at display boundaries.

Existing results.jsonl files are preserved. Migration prepares shared adapter fields for eligible cached results, maps saved failure identities to the covered queue and stores new execution/scope options separately from worker progress. Other old rows remain in the journal but inactive; the browser exposes them without permitting automatic enrollment. Sources not yet translated stay native. This is not a complete-game/all-language coverage claim.

## Run controls and ownership

options.json owns scope, backend, device, GPU-layer count, CPU threads and optional retry generation. run.json owns worker status/progress. overrides.json owns durable user edits/defer/original choices. Completed model results append to the journal; manual edits never get overwritten by it.

Resume skips successful entries and retains failures for a separate optional retry. Scope changes checkpoint the current entry, re-index the selected covered set and reuse compatible successes. Excluded entries retain their cache. Percentage measures settled selected work, including explicitly deferred/failed entries; separate counters identify those states. ETA uses observed processing speed and is an estimate.

CPU uses zero GPU layers; GPU offloads all supported layers; hybrid uses the chosen GPU layers and CPU remainder. Device detection exposes the backend's own Vulkan device identifiers. Set up GPU downloads only the optional pinned backend when model weights already exist. Startup reports errors rather than silently claiming GPU acceleration. [Upstream server options](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md) document threads, device selection and GPU-layer offload.

Read/replace operations retry bounded Windows sharing conflicts. UI refresh retains its existing view and exposes an actionable status if reads still fail; invalid state is not reported as success. Settings and worker progress are stored independently to avoid clobbering each other.

## Focused evidence and limits

- Supplied failure reproduced in a private native session using a copy of the real French cache: `black` became `noir`, and native gradient creation threw the matching error.
- With local mode active after repair, native plugin parsing returned `black`, a raw converter stayed unchanged, the shared renderer loaded and a manual dictionary override reached it. Three-in-One 1.07 and Maple source hashes stayed unchanged.
- CPU, Vulkan GPU and hybrid each translated the same HP-50 sample successfully. Hardware tested: NVIDIA GeForce RTX 4070 Laptop GPU, Vulkan0; AMD 610M was enumerated, not benchmarked. Both model sizes remain installed; new backend-mode calls used 0.8B. Earlier 2B inference evidence is not a new 2B GPU check.
- A copied 3,007-entry journal remained byte-identical through migration; every newly indexed source was in existing English coverage, and `black`/script text was excluded.
- Queue proof retains completed entries, defers a saved failure during ordinary resume, retries it only after explicit selection and preserves deferred work across category removal/re-addition.
- Shared-file proof covers temporary exclusive locks and concurrent atomic refresh. Editor proof covers search, a durable override and scope changes that retain the journal. Native form checks cover normal/minimum launcher bounds and the settings/editor windows.

Before/after images are actual native captures of the same copied state, with raw model wording left visible. Full source hashes, machine-readable reports and failed-attempt/re-plan records are retained in the task work directory. User saves were not loaded through or written by the real game session for these checks; the private renderer read a copied save. Full gameplay, every language/font/DPI/device and every historic adapter screen were not re-tested. Existing English runtime and release extraction gates remain part of packaging.

[Machine-readable focused evidence](local-translation-repair-1.0.42-evidence.json).
