# Linked Slider Constraints

True Custom Difficulty keeps semantically ordered slider groups valid while the user edits them.

## Interaction rule

All linked-slider constraints compare the **actual parameter / CFG value**. They do not compare each slider's percentage of its own vanilla baseline.

The slider being moved is authoritative. Other sliders move only when its actual value would violate the configured order, and only by the minimum amount required to restore it. Existing spacing is otherwise preserved.

Example for `Low <= Medium <= Severe`:

- `1.0 / 1.5 / 2.0`, move Low to `1.2` -> `1.2 / 1.5 / 2.0`
- `1.0 / 1.5 / 2.0`, move Low to `1.5` -> `1.5 / 1.5 / 2.0`
- `1.0 / 1.5 / 2.0`, move Low to `1.7` -> `1.7 / 1.7 / 2.0`
- `1.0 / 1.5 / 2.0`, move Low to `2.2` -> `2.2 / 2.2 / 2.2` when all three ranges allow it
- `1.0 / 1.5 / 2.0`, move Severe to `1.3` -> `1.0 / 1.3 / 1.3`

A slider may still be *displayed* as `% of vanilla`; that is presentation only. Cascading uses the underlying numeric value.

## Active constraint groups

| Group | Required raw-value relation | Parameters |
|---|---|---|
| Bleeding damage severity | nondecreasing | Low Bleeding Damage <= Medium Bleeding Damage <= Severe Bleeding Damage |
| Fatigue aim penalty severity | nondecreasing | Low <= Medium <= Severe Fatigue Aim-Time Penalty |
| Fatigue reload penalty severity | nondecreasing | Medium <= Severe Fatigue Reload-Time Penalty |
| Water stamina drain depth | nondecreasing | Medium-Water Stamina Drain <= Deep-Water Stamina Drain |
| Hunger stamina recovery severity | **nonincreasing** | Hungry value >= Severely Hungry value |
| Repeated-hit protection range | nondecreasing | Minimum <= Maximum |
| Repair price reputation | nondecreasing | Friendly <= Neutral <= Disliked <= Hostile Repair Price |
| Melee stamina cost strength | nondecreasing | Normal Melee Cost <= Strong Melee Cost |

### Negative-value rule: Hunger

Hunger recovery penalties are negative raw values. Vanilla is approximately `-0.33` while Hungry and `-0.70` while Severely Hungry. A stronger penalty is therefore a **smaller / more negative** number, so the valid raw relation is:

`Hungry >= Severely Hungry`

For example, `-0.33 / -0.70`; moving Hungry down to `-0.80` cascades Severe Hunger to `-0.80`. Moving Severe Hunger up to `-0.20` cascades Hungry to `-0.20`.


## Clickable vanilla/reference markers

The visible reference markers underneath sliders are interactive. Clicking a marker snaps that control to the marker's exact source value. This applies to normal numeric sliders, choice sliders, and both MIN/MAX tracks of range controls.

Marker snapping deliberately bypasses ordinary drag/tick rounding so, for example, clicking another difficulty's vanilla point cannot be rounded to the current Base Preset's `% of vanilla` tick grid. The resulting `CurrentNumber` property change still goes through `SliderConstraintCoordinator`, so linked sliders preserve the same value-based cascade rules as manual dragging.

Only the visible marker dot/label hit area is active; empty space underneath the slider is not a second click-to-position track.

## Preset behavior

Preset/reference bulk loads temporarily suspend live propagation so JSON property ordering cannot affect the outcome. After loading, a deterministic normalization pass preserves the first member of each constraint group and minimally moves later members until the raw-value relation is valid. Save and build also run a final normalization pass as a safety check.

## Architecture

The groups are declared in `Data/parameters.json` under `sliderConstraints`. A constraint contains only an ID, order (`nondecreasing` or `nonincreasing`) and the ordered parameter IDs. `ParameterCatalogService` validates membership, slider control type, uniqueness and every vanilla difficulty baseline. `SliderConstraintCoordinator` owns runtime propagation. CFG writer behavior is unchanged.

There is intentionally no `relativeToVanilla` comparison mode. Percentage-of-vanilla is a UI display concept only and does not affect linked-slider ordering.

## Runtime catalog source

Release builds use the `parameters.json` embedded into the EXE. A loose `Data/parameters.json` beside the executable is intentionally ignored so an old development/runtime file cannot silently disable newer constraint definitions. For deliberate development testing only, set `STALKER2_TCD_PARAMETERS_PATH` to the exact external JSON file to load.
