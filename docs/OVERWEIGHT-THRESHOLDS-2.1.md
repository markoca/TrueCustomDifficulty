## CURRENT 2026-11-23 — Whole-percent Overweight editing

The STALKER 2.1 source `DefaultWeightParams.WeightEffectParams[i].Threshold`
is a **kilogram float**, not a percentage or integer-only field: Vanilla
is `50.f` / `60.f` / `70.f` / `80.f`. Every manual load-stage
edit now snaps in **1%** steps; all 12 factory role profiles now have
**integer percentage** thresholds, stored as 0.01-grid source ratios.
The native CFG patch still writes precise kg = MAX CARRY WEIGHT × ratio;
85 kg × 73% writes 62.05f, not 62f, to avoid game-balance drift.

The exact off-grid Vanilla source ratios (62.5%, 87.5%) survive reset;
the UI shows ~63% / ~88% rather than pretending they are exact integers.
The source minimum 2.5 percentage-point gap remains for backward
compatibility; edited pairs on the 1% grid effectively need 3 points.
Old user presets may snap to the new 1% grid on load; original Vanilla
reference values are protected. No capacity, native speed/regen penalties,
CFG writer output path or unrelated factory settings change.

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

## Current editor — capacity-scaled interactive graph

STAMINA / **OVERWEIGHT SYSTEM** combines the original MAX CARRY WEIGHT
source and four threshold sources in one interactive Bleeding-style board.
The single native **MAX CARRY WEIGHT** slider (20–160 kg, 5 kg steps) is
located immediately above the LOAD PENALTY PROFILE; it has been removed from
CORE STAMINA to avoid duplicate editors.

The graph's horizontal axis is **kilograms from 0 to the currently selected
MAX CARRY WEIGHT**, not fixed 0–100%. Five dynamically recalculated ticks
show 0, 25%, 50%, 75% and 100% of that weight *in kilograms*. For example:

| MAX CARRY WEIGHT | Kilogram axis ticks | Vanilla stage kilograms (LIGHT / MEDIUM / HEAVY / CRITICAL) |
|---|---|---|
| 50 kg | 0 / 12.5 / 25 / 37.5 / 50 | 31.25 / 37.5 / 43.75 / 50 |
| 80 kg | 0 / 20 / 40 / 60 / 80 | 50 / 60 / 70 / 80 |
| 115 kg | 0 / 28.75 / 57.5 / 86.25 / 115 | 71.875 / 86.25 / 100.625 / 115 |

Threshold values remain ratios in the presets (factory 62.5% / 75% /
87.5% / 100%) and are mapped onto the displayed kilogram axis using the
current carrying limit. Editing carry moves the graph's absolute kilogram
values, but preserves each threshold's original ratio. Dragging a gold
threshold node or its baseline marker edits the corresponding ratio in
native 2.5-percentage-point steps; the original strict-stage constraint
coordinator continues to prevent crossings. Right-click restores one
stage or MAX CARRY WEIGHT; R resets all five source controls on this board.

The profile uses stepped native-effect lines, yellow for speed loss and
white for stamina regeneration loss, plus dashed reference lines (the native
effect strengths remain unchanged). Four read-only stage cards below show
live percent, kilogram trigger, speed/regen penalty and jogging availability.
Derived mass highlights when either its ratio or MAX CARRY WEIGHT changes.
Cards select their rationale without editing it. The responsive board is
612 DIP high at wide widths and 726 DIP when cards form two rows. No source
effects, writer routes or existing preset values are duplicated.

## 2026-11-23 — Overweight stages tuned across all 12 factory roles

**Current source of truth:** `tools/factory_preset_overweight_2026_11_23.json`.
All twelve shipped 2.1 profiles previously used identical Vanilla stage ratios
(62.5 / 75 / 87.5 / 100%); the only difference was MAX CARRY WEIGHT.
Now **48 existing source ratios** have deliberate, distinct role settings, all
on the catalog's 2.5-percentage-point grid with strict ordered stage gaps.
The existing maximum carrying capacities and the other 452 preset values
stay unchanged. No extra fake parameters or effect magnitude controls are added.

| Profile | Max carry | LIGHT | MEDIUM | HEAVY | CRITICAL | Intent |
|---|---:|---:|---:|---:|---:|---|
| Anomaly Runner | 85 kg | 72.5% | 82.5% | 92.5% | 100% | Fast anomaly extraction under valuable cargo; penalties arrive late. |
| Field Medic | 85 kg | 60% | 75% | 87.5% | 97.5% | Medical kit burden is felt earlier; useful emergency carrying margin remains. |
| Hardcore+ | 50 kg | 45% | 60% | 75% | 90% | Unforgiving encumbrance: jogging blocked well before the 50 kg ceiling. |
| Hardcore | 55 kg | 50% | 65% | 80% | 95% | Very demanding but more forgiving than Hardcore+. |
| Immersive | 75 kg | 60% | 72.5% | 85% | 97.5% | Realistic load fatigue with a slightly earlier heavy stage. |
| Mercenary | 80 kg | 62.5% | 77.5% | 90% | 100% | Professional kit management; movement falters near heavy loads. |
| Mutant Hunter | 80 kg | 57.5% | 72.5% | 87.5% | 100% | Weapons, ammo and mutant trophies put strain on the hunt. |
| Outcast | 55 kg | 47.5% | 62.5% | 77.5% | 92.5% | Early burden and reduced escape capability for the isolated survivor. |
| Scavenger | 70 kg | 55% | 70% | 85% | 97.5% | Heavy salvage comes at an early mobility price. |
| Smuggler | 115 kg | 70% | 82.5% | 92.5% | 100% | High-capacity cargo specialist; retains a long mobile load window. |
| Tactical | 70 kg | 65% | 77.5% | 90% | 100% | Disciplined light loadouts preserve battlefield agility. |
| Wanderer | 105 kg | 67.5% | 80% | 90% | 100% | Exploration-focused carrying with longer useful travel range. |

**Semantics:** Each percentage is the share of the role's existing MAX CARRY
WEIGHT at which that load stage begins; the edited parameter is not a sprint
speed multiplier. The four source 2.1 stage effects remain -15/-25/-50/-50%
velocity and -10/-20/-35/-50% stamina regeneration, with jogging blocked at
HEAVY and CRITICAL. Effects and exact runtime stacking/removal still require
in-game verification. Profiles with earlier thresholds carry equipment less
comfortably even if their MAX CARRY WEIGHT value is unchanged. In the graph,
all four kg thresholds are recalculated live from that role's capacity.

**Tests:** `python tools/audit_factory_overweight_profiles.py` checks all
12 JSONs, 48 ratios, exact 2.5% snapping, ascending minimum gaps, role
diversification, unchanged carried capacities and writer references.
The production Windows Health/Stamina contract now imports each actual
embedded factory profile and writes the five source parameters into two real
CFG patch files, checking four thresholds, MaxInventoryMass when changed,
InventoryPenaltyLessWeight and both inventory HUD markers. Both existing
factory coverage audits also enforce the new role matrix; historical balance
ledgers remain untouched. A successful local `./publish.sh` refreshes the
tracked twelve role presets; user-named custom presets are not changed.

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

## 2026-11-23 — Bleeding-profile graph and four dynamic cards

The Overweight System now uses the same interaction/reading hierarchy as
BLEEDING DAMAGE PROFILE: a framed X/Y threshold plot with discrete **stepped**
profiles, draggable gold stage nodes, separate baseline markers, dotted
projections, neutral dashed factory references and read-only cards beneath.

The X axis is **load as percent of MAX CARRY WEIGHT (0–100%)**. The Y axis is
the fixed native **stage penalty (0–50%)**. A gold stepped curve shows the
native movement-speed penalties (15/25/50/50% loss) and a white stepped curve
shows stamina-regeneration penalties (10/20/35/50% loss). Both use the same
four source activation thresholds; neither penalty magnitude is editable.
The heavy and critical speed penalty is equal, but critical raises the stamina
regen penalty from 35% to 50%, explaining the final horizontal gold step.
Dashed gray outlines/ticks show reference thresholds. These represent
stage-associated native effects, not proven cumulative game runtime behavior.

Four cards **LIGHT / MEDIUM / HEAVY / CRITICAL** show live STARTS AT %, derived
carried kilograms, both fixed effect penalties, and whether jogging is
blocked. Threshold percentages become yellow only when edited; derived
kilograms become yellow if either that stage or MAX CARRY WEIGHT is modified.
All units, stage labels and fixed penalty values stay neutral. Cards select
their corresponding rationale but are read-only, as in Bleeding; only plot
nodes/axis markers edit. Stage dragging, 2.5-point snapping, ordering cascade,
per-stage right-click and R all-stage reset are unchanged. On narrow hosts,
cards form a 2×2 bank instead of squeezing text into four narrow columns.
The control measures to 490 DIP wide or 604 DIP narrow so no card is clipped.

Only presentation and source selection are changed. The BPatch output, stage
ratios, native effect strength and 2.1 CFG paths are unchanged. The visual
stage curve is not an engine-runtime simulator: validate effect stacking and
removal in-game, especially when hovering around a boundary.

## 2026-11-23 — dynamic kilogram axis and integrated carry slider

A single existing `stamina.maxCarryWeight` source has moved from the
`StaminaCore` aggregate to `OverweightThresholds`; `GetFamilyIds` and
`ParameterViewModel.VisualAggregateFamily` agree. The graph now owns and
edits this parameter, rather than observing it as a read-only linked source.
Its source-native ruler sits at the top of the same board. Core Stamina keeps
MAX STAMINA, REGEN and a full-width VAULT HEIGHT control, with no duplicate
carry editor.

The graph axis reads kilogram quantities 0..MAX CARRY WEIGHT. Both the
yellow/white stage profiles and their dashed reference positions use
`carryKg × ratio`. Graph drag maps back to the unchanged source ratio
0..1, so loading old presets, strict ordering and the atomic writer route
are unaffected. Reset and keyboard interactions use the existing native
`HitTarget`/family code; R now resets carry and all four thresholds together.
Five axis ticks and every stage-card kg readout update on any carry change.
