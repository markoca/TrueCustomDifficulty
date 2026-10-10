# Water and Energy Drink — source-exact effect audit (2026-10-10)

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

## Application, writer and UI contract

The `survival.energyDrinkEffectDuration` multiplier changes the
`Duration` of four existing prototypes (45/45/15/5 s). It **does not**
change their -30%/-100% SPDrain strength, their affected action list or
which item receives which effect. `survival.waterEffectDuration` similarly
scales the two Water duration prototypes (5/30 s).

`ConsumableMixer` retains **five real editable drink-duration sources**.
The Water and Energy tiny timeline ticks are passive readings of the same
source; four compact caption rows distinguish Water, ordinary Energy's
5/15/45 s effects and Limited Energy's 5/45 s effects. No new controls or
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
