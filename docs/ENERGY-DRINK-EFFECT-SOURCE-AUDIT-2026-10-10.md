# Water and Energy Drink — source-exact effect audit (2026-10-10)

### 2026-10-12 — Instant stamina knobs renamed to STAMINA GAIN (CURRENT)

The two independently writable direct-percent consumable controls are now named **WATER STAMINA GAIN** (38% vanilla) and **ENERGY DRINK STAMINA GAIN** (50% vanilla) in the 270-degree knobs, catalog labels and contextual help. "Gain" means **immediate stamina restored on consumption**, not sustained stamina regeneration; the existing WATER STAMINA BUFF and ENERGY DRINK REGEN knobs remain separately named and operate unchanged. This is nomenclature only: retain parameter IDs `survival.waterInstantStaminaRestore` and `survival.energyDrinkInstantStaminaRestore`, actual 0–100% units, 1-percentage-point steps, exact `ValueMin/ValueMax` direct CFG writes, vanilla levels, Limited Energy Drink unaffected, old-preset migration, override control, and every native knob interaction. Earlier "REFILL" instructions for these two UI labels are superseded.


### 2026-10-12 — Independent direct-percent Water and Energy Drink instant stamina refill (CURRENT)

The former single **WATER + ENERGY DRINK REFILL** 1x control `survival.instantStaminaRestore` is retired from production. **WATER STAMINA GAIN** uses independent `survival.waterInstantStaminaRestore` and exposes the exact **38%** vanilla refill; **ENERGY DRINK STAMINA GAIN** uses `survival.energyDrinkInstantStaminaRestore` and exposes the exact **50%** vanilla refill. Both are real, separate source-backed **0–100%** 270-degree rotary knobs with **1 percentage-point** source steps and clearly centered percent values and 0%/100% ends. Their writer operations **directly set** the corresponding `ValueMin` and `ValueMax` in `WaterStaminaInstant` and `EnergeticStaminaInstant3`, respectively. They must not remain multipliers or share writes. Normal game stamina capacity caps the effect; this is the share of full stamina restored instantly, not regeneration over time.

**Consumable Effects top row** comprises four *bounded* equal-sized rotary controls: WATER STAMINA GAIN (38%), ENERGY DRINK STAMINA GAIN (50%), WATER STAMINA BUFF (1x; preserves the existing coupled stamina regeneration and action cost effects), and ENERGY DRINK REGEN (1x; sustained stamina regeneration). This replaces the obsolete three-knob bank without changing the existing duration timeline / child markers, drink-color visual contract, medicine knobs or their positioning. **Limited Energy Drink's standalone `EnergeticLimitedStaminaInstant4` 100% refill is untouched** because the user requested independent controls for the two regular drinks, not an additional Limited Edition slider.

Old presets containing the original `survival.instantStaminaRestore` multiplier are migrated on load when new source IDs are absent: scalar × 0.38 → Water, scalar × 0.50 → regular Energy Drink, clamped at 100%. The old scale no longer writes to Limited Energy Drink, which stays vanilla. The Drink Duration Override editor-only flag continues under the new Water anchor while retaining the same `drinkDurationOverride` preset property. Native source intervals, right-click reset, selected-source help, wheel, preset roundtrip and source baseline counts and SurvivalPatchContract must agree. No new CFG prototype, runtime effect, synthetic calculation or altered duration behavior is introduced. This entry supersedes historical shared-multiplier wording below. Local build/Wine verification is pending.


## 2026-10-11 — Active live-preview and duration naming

`WaterStaminaPerAction1` remains the real −10% SPDrain modifier for action stamina cost (native 30-second duration). **WATER STAMINA COST CUT** now previews the **10% stamina cost reduction** (rather than a negative amount) with its magnitude bar and grey reference tick filling **left to right**, consistently with positive effects; this preview is not a reverse-oriented control. The original negative gameplay source, numeric scale, knob, writer and item-membership remain untouched.

The three native-seconds child editors are **WATER +25% STAMINA EFFECT DURATION** (`WaterStamina2`, 5 s), **ENERGY -100% STAMINA COST DURATION** (`EnergeticStaminaPerAction2`, 5 s) and **ENERGY -30% STAMINA COST DURATION** (`EnergeticStaminaPerAction1`, 15 s). `WaterStamina2` is type `Stamina` rather than verified `RegenStamina`; no regenerated-SP-per-second inference is added. Changing UI language has no effect on saved/published CFG values.

## Evidence and scope

Verified directly against the user-provided decoded base-game CFG archive
`STALKER2-GameData-CFG(6).zip`, specifically `GameData/EffectPrototypes.cfg`
and `GameData/ItemPrototypes.cfg`. The ZIP is not committed to the repository.
These files establish author-defined prototypes, their explicit item memberships,
inheritance and writer values; **they do not by themselves establish Unreal
Engine stacking or final in-game damage/stamina consumption formulas**.

## Actual effect prototypes

| Effect SID | Type | ValueMin / ValueMax | Duration | Item/meaning |
|---|---|---:|---:|---|
| `WaterStamina2` | `EEffectType::Stamina` | +25% / +25% | 5 s | `Text = Regen Stamina`, but native Type is **Stamina**, not RegenStamina |
| `WaterStaminaPerAction1` | `EEffectType::SPDrain` | -10% / -10% | 30 s | Water action stamina drain |
| `EnergeticStamina` | `EEffectType::RegenStamina` | +200% / +200% | 45 s | Ordinary Energy regeneration |
| `EnergeticLimitedStamina` | `EEffectType::RegenStamina` | +500% / +500% | 45 s | Limited Energy regeneration |
| `EnergeticStaminaPerAction1` | `EEffectType::SPDrain` | -30% / -30% | 15 s | Ordinary Energy action stamina drain |
| `EnergeticStaminaPerAction2` | inherits `SPDrain` | -100% / -100% | 5 s | Action drain inherited from PerAction1; stronger but shorter |

The `EnergeticStaminaPerAction2` structure declares
`{refkey=EnergeticStaminaPerAction1}` and overrides **only SID, Duration,
ValueMin and ValueMax**. It inherits the parent `SPDrain` type and the
eight-element `AffectedStaminaActionTypes` array. The authored actions
are Walk, Run, Sprint, Jump, Vault, MeleeNormal, MeleeStrong and
MeleeButstock. Water's action-drain structure has the same eight actions.

### Membership matters: regular vs Limited

`ItemPrototypes.cfg` declares:

- `Energetic.EffectPrototypeSIDs[6] = EnergeticStaminaPerAction1`
  (**15 s / -30%**) and `[9] = EnergeticStaminaPerAction2`
  (**5 s / -100%**). Both are present in the ordinary drink's list.
- `Energetic_Limited.EffectPrototypeSIDs[9] =
  EnergeticStaminaPerAction2`. Its explicit regular list **does not contain
  PerAction1**.
- `Energetic_Limited.AlternativeEffectPrototypeSIDs[6] =
  EnergeticStaminaPerAction1`, the alternative Weird Kettle path.
- `Energetic` points to `EnergeticStamina` (45 s / +200%);
  `Energetic_Limited` points to `EnergeticLimitedStamina`
  (45 s / +500%).

Although ordinary Energy lists both SPDrain SIDs, the effect manager's
runtime combination rule is not established by CFG inspection. Do **not**
assume that the two percentages are additive, that one overrides the
other or that the rendered stamina cost equals exactly zero without an
in-game acceptance test.

## Global Override Limits (2026-10-10)

The eight existing drink duration fields remain independent, but the new single
`OVERRIDE LIMITS` UI-only checkbox can widen their **maximum** to 270 seconds
(the existing Magic Vodka 3× common ruler). This includes both Energy `SPDrain`
children, so neither is parent-capped in Override ON mode. In OFF mode their
separate source-exact maxima and their prior parent caps remain unchanged.
The checkbox has no CFG SID or `Duration` writer target, and it never changes
effect magnitudes, prototype membership or stacking semantics.

## Update 2026-10-10 — independent short-effect duration tuning

The earlier one-multiplier/four-target duration mapping below is now **historical**. The current `survival.energyDrinkEffectDuration` controls only `EnergeticStamina` and `EnergeticLimitedStamina` (both 45s base), while `survival.energyFullCostDuration` writes the original 5s `EnergeticStaminaPerAction2` `Duration` and `survival.energyPartialCostDuration` writes the original 15s `EnergeticStaminaPerAction1` `Duration`. Water's 30s action-cost duration remains on `survival.waterEffectDuration` and its 5s regeneration duration is controlled separately by `survival.waterRegenDuration`. There are **no duplicated targets**. The first three child controls may extend their seconds only as far as the live current main duration; authored vanilla remains the floor. The same PerAction2 prototype is used by normal Energy and Limited Energy, and PerAction1 remains available in Limited's alternate Weird Kettle path.

## Application, writer and UI contract

The original Energy duration control formerly changed the durations of four prototypes (45/45/15/5 s). The **current** main multiplier changes only the two 45s prototypes; the new 5s/15s child controls edit their own real second values. This arrangement **does not**
change their -30%/-100% SPDrain strength, their affected action list or
which item receives which effect. `survival.waterEffectDuration` similarly
scales the two Water duration prototypes (5/30 s).

`ConsumableMixer` retains **five real editable drink-duration sources**.
The Water and Energy tiny timeline ticks are passive readings of the three independently editable child durations; three compact sliders directly beneath the timeline give each its own source-backed editing track. No new controls or
synthetic sums are permitted. Duration numerals change with the existing
native 0–3x / 0.05x duration multipliers. Read-only percentage labels
remain neutral even when their duration changes.

## Reproduce from the original CFG ZIP

```bash
python3 tools/audit_consumable_effect_source.py \
  --base-zip "/path/to/STALKER2-GameData-CFG.zip" \
  --out /tmp/tcd-consumable-original-source.json
```

The audit checks native Type, ValueMin/Max, Duration, PerAction2 inheritance,
all eight action types, ordinary/Limited/alternative item membership and
parity with the production `cfg-writer-baseline.json` edit points.
Actual Wine WPF layout and game-runtime stacking are separate checks.
