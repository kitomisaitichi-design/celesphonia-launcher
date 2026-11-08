# Local model formatting repair — 1.0.45

The user reported hundreds of failures in a running Interface translation run and asked whether native PLaMo parameters were being used. The backend was generating output; transport was working. At diagnosis, 863 failure records matched the selected index: 850 structural/value checks, eight output limits and five protected-slot checks. 802 sources ended in CR. The run was paused at its checkpoint with 3,114 completed entries kept. Historical failure records outside that index were not blindly deleted.

## Actual request/response cause

The current request already used `chat_template_kwargs.target_lang`, `glossary` and `context`, sending matching domain terms rather than the full glossary. However, the context included generic instructions, a literal dummy slot and raw English reference controls. PFN documents context as surrounding text/situation. The new defaults are descriptive, and reference controls are removed from the meaning hint.

A safe source `这是一个测试。` with trailing CR returned `C'est un test.<br />` in both the old-context and minimal native request. The added HTML tag correctly failed validation. A Japanese window-height help line dropped literal `1`; a French format message naturally moved `%2` before `%1`, which the previous sequence check incorrectly rejected. These raw pairs, rather than an HTTP success or a single terminology label, identified the fixes.

## Repair

- Preserve outer whitespace/CR separately, translate the actual line body, then reattach the exact source shell. The worker retains its original line delimiters and blank lines.
- Use the same protected-token grammar as the existing English adapters, plus literal numeric tokens, with native glossary identity mappings. Only matched optional domain terms are added. No structural/numeric slots are translated as prose. Default context describes page type/situation; English reference text remains optional and contains no engine escape codes.
- Local compatibility accepts only permutations of the same indexed `%1`…`%9` placeholders. Every other engine control/tag/condition stays in the same identity/order. Values, including signs, remain checked. Shared JS already maps placeholders by identifier; no native data/name/value mutation is introduced. The curated-English path keeps its prior strict validation.
- A new comparison snapshot/current shared preparation derives the local display fields. Eight identical consecutive failures pause the worker at a checkpoint with an actionable reason instead of silently spending the whole queue on the same failure. Successful work and optional retry history remain.

## Focused checks

The same safe UI/CR/placeholder/help/state/conditional samples are run through the corrected inference path with the installed 0.8B model on the user's existing CPU configuration. A bounded real-worker fixture forces six already-covered sources, bypassing reuse only for that private fixture, then pauses. The cases include plain and icon/color-prefixed CR lines, the actual LF-ended help source, an indexed format message and the glossary state label. All six were accepted in the initial worker check; the existing user's successful journal was unchanged. The existing shared renderer separately substitutes the reordered format slots by ID.

Negative cases still reject changed color order, literal values, missing/duplicated/invented format IDs, changed conditions and added tags/newlines. The existing English regression suite is retained. Final packaging/installed-cache/live-run receipts are kept with the coordination results. No model weights are downloaded, no whole corpus is restarted, and no authored/imported target is overwritten to force a successful counter.

A first compile stopped because one local helper retained a renamed delegate outside its scope; it was corrected before any candidate was installed. An initial JS mock used the wrong dictionary global and therefore exercised an empty lookup; the corrected mock uses the real shared renderer's dictionary global. These failed checks are retained rather than counted as passes.

## Native API reference

PFN documentation: https://huggingface.co/pfnet/plamo-3-translate-0.8b-GGUF#translation-options

The source text is the last user message; native options are fields of `chat_template_kwargs`. `glossary` is a list of source/target pairs, `context` is reference context, and `source_lang` is optional. The launcher allows source-language detection. The installed GGUF templates were already verified to support glossary/context. A glossary can improve consistency without guaranteeing reviewed French or acceptance of every string. Invalid targets remain uninstalled and the original text stays available.
