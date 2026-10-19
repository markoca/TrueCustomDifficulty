# Native Food Hunger Relief — targeted audit (2026-10-19)

## Scope and verdict

Four **per-use hunger-relief magnitude** sliders, not "satiety rate" / passive regeneration: `survival.breadSatiety`, `survival.sausageSatiety`, `survival.cannedFoodSatiety`, `survival.milkSatiety`. The only passive hunger-rate control is `survival.hungerRate` and the unknown runtime application of `EnvironmentDifficulty.Effect_Satiety` remains outside editor scope.

**Static result:** catalog/VM/source mapping and negative writer operation align for the four normal food SIDs; targeted audit corrects two issues found after the original implementation: Milk's redundant upper range (200 → **100**) and the absence of explicitly locked ordering in `MainViewModel.ParameterSortOrder`. Other food families and effect copies are deliberately not changed.

| Item | Base normal effect (2.0.5 project provenance) | Writer | Slider/reference | Gameplay constraint |
|---|---|---|---|---|
| Bread | `BreadSatiety2.ValueMin/Max = -20` | both via `negateSet` | 0–40 / 20, step 5 | no cross-edit |
| Sausage | `SausageSatiety2.ValueMin/Max = -30` | both via `negateSet` | 0–60 / 30, step 5 | no cross-edit |
| Canned Food | `CannedSatiety3.ValueMin/Max = -50` | both via `negateSet` | 0–100 / 50, step 5 | no cross-edit |
| Condensed Milk (item `Milk`) | `MilkSatiety4.ValueMin/Max = -100` | both via `negateSet` | 0–100 / 100, step 5 | `Player.VitalParams.MaxHungerPoints = 100` |

Player-facing numbers are **positive amounts of hunger removed**. The authoring pipeline makes `ValueMin` and `ValueMax` negative. Therefore 0 points is no relief; greater positive magnitude reduces more hunger. With a 0–100 HungerPoints meter, vanilla Milk already removes up to the entire bar. Exposing 101–200 would falsely suggest extra meaningful benefit. Existing presets or user inputs above 100 are now clamped by the shared VM range normalization, not allowed to write oversized Milk values.

## Verification layers and status

- **Source mapping:** specific normal consumable `EffectPrototypeSIDs` and the four source values are recorded in `docs/CONFIG-BASELINE.md` and `docs/CONSUMABLE-MEDICINE-CFG-AUDIT-2026-10-11.md`. The 2026-10-11 audit cites the supplied `STALKER2-GameData-CFG(8).zip`; the underlying raw 2.0.5 ZIP was **not available for a fresh byte-by-byte comparison in this audit**. An older [1.5.2 effect/item inventory](https://gist.github.com/sdwvit/93bb5e0e877c9777e5101cb6864fabfa) independently supports the SID identities but cannot establish 2.0.5 exact values. Source classifications remain `runtime_test_required`.
- **UI mapping:** all four are normal source sliders, not part of the `HungerLadder` aggregate. `ParameterSortOrder` now fixes Bread → Sausage → Canned Food → Condensed Milk directly after the Hunger Ladder. The responsive panel places them in **two adjacent rows of two** when ≥700 DIP, stacking below; no independent giant visual or duplicated hint row. Current numeric text, native markers, reset and modified-only yellow styling use standard WPF card contracts.
- **Writer:** exact `GameData/EffectPrototypes.cfg` base-game routes; eight separate leaves, no shared prototype ownership. Real writer uses `negateSet`, `FormatLikeSource` and numeric equivalent suppression, so unchanged vanilla source rows produce no food patch. Existing intentional sleep 100 HP default remains separate.
- **Factory presets:** twelve shipped profiles retain the four unchanged vanilla values; the existing exact-blob pristine-upgrade path remains intact and does not overwrite modified user files.
- **New gates:** `tools/audit_food_satiety_source.py` checks each catalog/writer/UI boundary and all twelve profiles; added to GitHub static CI and `tools/pre_release.py static`. `SurvivalPatchContract` now checks exact order and bounds, clamp/reset, individual 0/high writer cases, and four-simultaneous BPatch leaf isolation. These checks are committed for execution, **not reported as executed** in this environment.

## Intentional exclusions

`FreshBreadSatiety3` (Fresh Bread), `SpoiledCannedSatiety1` (spoiled canned food), Water, Energy Drink, Vodka, Beer, alternative Weird Kettle satiety effects, food HP healing, nausea/drunkness relief and the global difficulty `Effect_Satiety` scalar are **not controlled by this set**. These are distinct effect sources with different side effects/semantics, not undisclosed portions of Bread/Canned controls. Implementing new controls for them requires its own grounded source/range and UX decision.

## Local acceptance checklist (pending)

1. Pull `main`, build, open Survival → Hunger; check compact Bread/Sausage row then Canned/Condensed Milk row under Hunger Ladder, with no yellow on unchanged numbers.
2. Set Bread to 0, Sausage to 60, Canned Food to 100 and Milk to 0; verify each UI value has exactly 5-point stops and its own right-click reset. Milk cannot exceed 100; vanilla Milk resets to 100.
3. Build and inspect generated `EffectPrototypes_patch_TrueCustomDifficulty.cfg`: expect exactly `ValueMin`/`ValueMax` pairs for edited food prototypes, signed `0`, `-60`, `-100`, `0`; no Fresh Bread, alcohol or Weird Kettle prototype modifications. An unchanged Milk 100 must not create a Milk patch.
4. In S.T.A.L.K.E.R. 2 2.0.5 without an active Weird Kettle alternative, measure HungerPoints before/after consuming **each** normal item at two distinguishable settings. Verify no changes to separate HP healing, alcohol or other item effects. Repeat with the alternate effect to ensure scope is accurately represented.

The workflow run on the original implementation commit `34d62adc` had failed jobs without reported steps/logs in the accessible GitHub results; this is **not evidence of passing or failing these gameplay checks**. Do not mark `.NET`, Wine or in-game work PASS until actual output is available.
