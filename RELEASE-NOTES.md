# Celesphonia launcher 1.0.46 — Opaque local-model tokens

Follow-up to the native parameter/line-ending repair: preserve double-escaped actor commands and complete angle-bracket status payloads, including CJK markers, before model translation. This prevents the next repeated structural-failure cluster from being rewritten as new control/tag syntax. Opaque markers may stay original; safety checks remain enabled.

The earlier repair accepted 50 new records before the next cluster reached the checkpoint. Completed entries are retained. The safe matching-envelope test joins the CR, numeric and indexed-placeholder samples. No model reinstall, cache reset or generic prompt is introduced.

Request-format cause and checks: https://github.com/kitomisaitichi-design/celesphonia-launcher/blob/launcher-v1.0.46/docs/model-format-repair-1.0.45.md
