# Celesphonia launcher 1.0.42 — Local dictionaries, shared adapters

Fixes the local `black → noir` color-parser crash and restores the established Celesphonia renderer/custom adapters in local mode. PLaMo now supplies a separate dictionary only for source strings already translated by the English overlay. Unknown/technical strings stay native. Existing journals, curated English and saves are preserved; unsafe legacy rows remain inspectable but inactive.

Adds per-run CPU/Vulkan GPU/hybrid settings, GPU device/layer/thread controls, visible ETA/progress, a searchable translation editor with durable manual overrides, editable covered scope and separate Resume pending / optional Retry failures actions. Bounded sharing retries address unhandled run.json refresh errors.

Restart the game after updating from 1.0.41. Both native mod variants, the reported parser failure, cache retention, queue/editor behavior and CPU/RTX4070/hybrid samples received focused checks. Full gameplay and all languages/devices are not claimed.

Pictures and trace: https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.42/docs/local-translation-repair-1.0.42.md
