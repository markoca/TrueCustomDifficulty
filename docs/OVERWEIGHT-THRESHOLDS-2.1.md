# Overweight Thresholds — S.T.A.L.K.E.R. 2.1

## Source and vanilla values

`GameData/ObjWeightParamsPrototypes.cfg` defines `DefaultWeightParams`, where
`MaxInventoryMass = 80`, `InventoryPenaltyLessWeight = 49.99`,
and `WeightEffectParams` is indexed in descending threshold order:

| Index | Threshold | Original effects |
|---|---:|---|
| [3] | 50 kg | VelocityChangeNoCap -15%, stamina regeneration -10% |
| [2] | 60 kg | VelocityChangeNoCap -25%, stamina regeneration -20% |
| [1] | 70 kg | VelocityChangeNoCap -50%, stamina regeneration -35%, jogging blocked |
| [0] | 80 kg | VelocityChangeNoCap -50%, stamina regeneration -50%, jogging blocked |

These are activation *thresholds*, not standalone sprint-speed controls.
`EffectPrototypes.cfg` owns the penalty strengths and `EffectPrototypeSIDs`
remain untouched. The engine's runtime effect-removal behavior is not encoded
in these CFGs and needs an in-game test.

`CoreVariables.cfg` / `DefaultConfig` also declares
`InventoryPenaltyLessWeight = 50.0`, `MediumEffectStartUI = 50`,
`CriticalEffectStartUI = 70`. Synchronize these with edited thresholds.

## User-facing editor

STAMINA / **OVERWEIGHT THRESHOLDS** is one compact 2×2 board:
**LIGHT LOAD**, **MEDIUM LOAD**, **HEAVY LOAD**, **CRITICAL LOAD**.
Each native Core Stamina-style horizontal rail edits a **fraction of maximum
base carry weight**, with its live KG load preview below the track.

The factory settings are 62.5%, 75%, 87.5%, 100%. All four stages use
2.5-percentage-point steps, with a minimum 2.5-point gap between adjacent
stages. Existing SliderConstraintCoordinator cascades changes to preserve
strict stage order. One handle resets on right-click; R resets the board.
Original native rail, reference tick, hover glow, changed-only yellow numeric
token and compact card spacing are preserved. The displayed KG load is
read-only, not a second source.

Maximum Carry Weight remains an independent 20–160 kg editor. At 50 kg
the thresholds default to 31.25 / 37.5 / 43.75 / 50 kg; at 115 kg they
default to 71.875 / 86.25 / 100.625 / 115 kg. This preserves old profiles,
including Hardcore+ and Smuggler, without requiring mass preset rewrites.
Older preset JSON files that do not contain the four new IDs inherit the
vanilla ratios when loaded.

## Production BPatch contract

The four percentage controls are stored as `stamina.overweight*Threshold`
source-relative ratios in preset files. They are special writer routes,
**not** fictitious direct CFG percentages. Whenever Max Carry Weight or
any stage changes, the writer emits:

- `DefaultWeightParams.WeightEffectParams[3].Threshold = carry × light`
- `DefaultWeightParams.WeightEffectParams[2].Threshold = carry × medium`
- `DefaultWeightParams.WeightEffectParams[1].Threshold = carry × heavy`
- `DefaultWeightParams.WeightEffectParams[0].Threshold = carry × critical`
- `DefaultWeightParams.InventoryPenaltyLessWeight = lightKg − 0.01`
- `DefaultConfig.InventoryPenaltyLessWeight = lightKg`
- `DefaultConfig.MediumEffectStartUI = lightKg`
- `DefaultConfig.CriticalEffectStartUI = heavyKg`

`DefaultWeightParams.MaxInventoryMass` continues through its original
exact direct CFG edit point. The writer emits the whole linked subtree
atomically, keeping native effects, item weights, perks, artifacts, armor
bonuses, DLC1 and sprint/stamina effect definitions unchanged.

All output uses BPatch nested structures under the original indexed effect
array, avoiding replacement of original `EffectPrototypeSIDs`.

## Test coverage and limitations

`tests/HealthStaminaPatchContract/Program.cs` checks source ratios,
capacity-only relocation, nested BPatch paths, inventory UI markers,
strict-order cascades, and joint capacity + threshold output. Static source
checks inspect the 2.1 CFG. Full .NET/Wine compilation and in-game behavior
must still be verified on the user's machine; offline source review cannot
prove the runtime event sequence when effects are added or removed.

Suggested game test: 55 → 65 → 75 → 65 → 55 → 45 kg with the default
80 kg capacity, plus scaled weights using 50 kg and 115 kg profiles.
Verify sprint speed, stamina regen, inventory load indication and effect
removal after dropping/selling items. Bonus carry from armor/artifacts
may be computed separately by the game and must be tested.

## 2026-11-23 — unified interactive graph

The former four 2×2 slider rails are replaced by a single Bleeding-style
four-node stage graph. Each node is on its own labeled horizontal lane, while
all four share a 0–125% maximum-carry-weight axis. A connected gold line
shows the current ordered activation thresholds; the dashed gray reference
line and rings show the unchanged vanilla ratios. Each lane exposes the
editable ratio and a read-only kilogram threshold at the current Max Carry
Weight. Drag/click a lane to change that stage. Its entire lane acts as the
native hit target; neighboring lanes do not steal the pointer.

The current source IDs, stepping (2.5 percentage points), strict 2.5-point
stage gaps, reference markers, edited-only yellow digits, hover glow,
right-click stage reset, R board reset, linked capacity preview, preset
compatibility and BPatch mapping are unchanged. This graph represents the
*activation order* of existing native load effects; the segments are not
an interpolated velocity or stamina curve. Runtime removal and sprint-speed
response still require the in-game regression test described above.

## 2026-11-23 — build fix

The four-node graph calls the shared `ValueToX(value, min, max, rect, reverse)` helper with explicit `reverse: false` for both live and reference points. This preserves the left-to-right percentage axis and fixes the missing-argument CS7036 build failure; no game balance, preset or BPatch semantics change.
