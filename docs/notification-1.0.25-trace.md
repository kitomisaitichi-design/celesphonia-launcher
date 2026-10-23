# Notification and skill-label trace — launcher 1.0.25

Baseline: launcher 1.0.24, source `6760cbe`. Scope: shared render-time overlay, native Maple 1.7.2 and Three-in-One Ver1.053, controls disabled. Native gameplay data and player saves are not rewritten by these repairs.

## Remaining missing names

`OriginalCommands.js` routes TMng commands to distinct message variables. CE97 eventually draws the selected value through `V[V[2995]]`; several frames prepend icons, colors, speaker labels or a comment prefix. Ticker's private escape converter runs its initial slash pass before that nested insertion. An extra actor escape before a translated Latin name can then consume the name as a formatting command.

The 1.0.24 wrapper recognized the plain frame and only Talk (2944), with a separate guarded time (2974) repair. The reported records use other modes:

| Native source key SHA256 prefix | Common event / command index | Mode | Message variable |
| --- | --- | --- | --- |
| c2c8df196340 | CE364 / 6 | Heart | 2980 |
| 2aa29223c4f7 | CE377 / 34 | Noicon | 2950 |
| 58ec48b3071c | CE377 / 37 | Noicon | 2950 |

The repair recognizes the nested native TMng field across its supported modes and alternate frames. It resolves numeric actor slots in the selected display value before insertion, restoring the original variable reader in `finally`. Native arguments/log inputs and stored fields remain unchanged. The existing exact time-field handling remains separate. Ordinary direct messages still use the existing converter hooks.

## Blank output

A field containing only a doubled actor-name escape reproduces a fully empty rendered ticker under 1.0.24. The corrected display contains that actor's name. This fixes a demonstrated translation-created gap; intentional native blank lines are retained, and the repair does not suppress arbitrary empty notifications. The reported real records and a neutral escape-equivalent fixture are tested separately.

## Skill 955: why Anal is the correct label

Native skill 955 contains `<Skill01;[110,1]>` and `<Skill02;[95,40]>`. `CulSkillEffect.js` routes these through the additive slots and `TagSkillList.json` to their base stats:

| Effect tag | Additive variable | Base stat variable | Native category | Skill contribution |
| --- | --- | --- | --- | --- |
| 110 | 4310 | 1270 | Anal development cap | +1 |
| 95 | 4295 | 1255 | Anal development EXP modifier | +40 |

The source description uses two different anatomical terms, but both effect mappings are anal-related. The display corrections are limited to the reviewed English title `Extreme Sensitivity (Intestines)` and bracketed `Intestinal development EXP modifier` label. Other anatomical text and every native mechanical value remain unchanged. Maple's already-consistent labels are retained.

## Validation and limits

Native checks cover all three reported records with Amane/Celesphonia/custom names, supported non-time TMng mode IDs with an alternate frame, the name-only gap, an unknown native field, stored reader/value restoration, intentional newlines, the skill mapping and database immutability. The existing suite covers the Heart streak/time conversion and other display hooks. Neutral before/after images use the actual native renderer, not edited game screenshots. Full gameplay coverage is not claimed.
