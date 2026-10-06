# Destructible Ammo Weight Audit

Version: 0.9.26.44 audit-only

## Result

The dedicated breakable ammo-box generators are a good candidate for **relative ammo distribution controls**.

There are two internal vanilla pools. Player-facing UI should **not** use the internal `SNG` / `NATO` names.

### Physical world surface

The existing production redirect manifest proves **1,235** physical destructible ammo-box generator usages:

| Internal pool | Base | DLC | Total |
|---|---:|---:|---:|
| `DestructibleStash_AmmoSNG` | 832 | 151 | 983 |
| `DestructibleStash_AmmoNATO` | 102 | 150 | 252 |
| **Total** | **934** | **301** | **1,235** |

All 1,235 audited rows are active production wooden ammo-box routes already used by the existing `Ammo from Breakable Boxes` production architecture.

## Vanilla weighted pools

### Eastern-caliber pool — internal `DestructibleStash_AmmoSNG`

| Ammo | SID | Weight | Relative share per weighted draw | Min–Max |
|---|---|---:|---:|---:|
| 9×18 mm | `A918D` | 2 | 25% | 3–6 |
| 9×19 mm | `A919D` | 2 | 25% | 5–10 |
| 5.45×39 mm | `A545D` | 2 | 25% | 5–10 |
| 12×70 buckshot | `A012D` | 2 | 25% | 2–4 |

### Western-caliber pool — internal `DestructibleStash_AmmoNATO`

| Ammo | SID | Weight | Relative share per weighted draw | Min–Max |
|---|---|---:|---:|---:|
| .45 ACP | `A045D` | 2 | 25% | 5–10 |
| 9×19 mm | `A919D` | 2 | 25% | 5–10 |
| 5.56×45 mm | `A556D` | 2 | 25% | 5–10 |
| 12×70 buckshot | `A012D` | 2 | 25% | 5–10 |

`9×19 mm` and `12×70 buckshot` exist in both pools.

## What Weight means here

All four candidates inside each pool have the same authored `Weight = 2`.

For a weighted selection **within that pool**, the normalized weight share is therefore:

`2 / (2 + 2 + 2 + 2) = 25%`

This is a **relative weighted share**, not a separate `Chance` gate.

Changing one item's weight changes its share relative to the other items in the same pool. For example, weights `4 / 2 / 2 / 2` normalize to `40% / 20% / 20% / 20%`.

`MinCount / MaxCount` is a separate quantity layer and must remain independent from weight.

## Important distinction from Chance

The ammo pool entries themselves do **not** have `Chance`. They have `Weight`.

The game also contains rank wrapper generators with `Chance = 0.5 / 0.6 / 0.7 / 0.8`, but the current proven physical production surface is redirected directly from the two base destructible ammo pools. Those shared wrapper Chance values should not be exposed as the caliber-distribution control.

Therefore the clean control point is the **Weight field of the six ammo models in the two TCD-owned destructible ammo pool clones**.

## Candidate UI design

Do not expose internal pool names. A clean player-facing design is six relative-weight sliders:

- **9×18 mm Ammo Box Weight**
- **9×19 mm Ammo Box Weight**
- **.45 ACP Ammo Box Weight**
- **5.45×39 mm Ammo Box Weight**
- **5.56×45 mm Ammo Box Weight**
- **12×70 Buckshot Ammo Box Weight**

Suggested semantics:

- vanilla = `1.0x`
- slider changes the authored weight multiplier, not quantity
- `0x` = caliber cannot be selected from any destructible ammo pool where it exists
- `1x` = vanilla relative weight
- values above `1x` make the caliber proportionally more common
- all effective weights in a pool must never be zero simultaneously

For duplicated calibers (`9×19` and `12×70`), one player-facing slider should modify both internal pool entries so the UI describes the caliber rather than the implementation.

## Interaction with existing Ammo Quantity

The existing `Grenades from Breakable Boxes` control is separate. More importantly, the existing destructible **Ammo Quantity** writer already creates TCD-owned clones of both ammo pools and preserves their vanilla `Weight = 2` fields.

A future weight implementation should extend those same TCD-owned pool clones so:

- **Weight controls caliber selection**
- **Ammo Quantity controls MinCount / MaxCount**
- both settings compose in one generated pool instead of competing writers

## Evidence

- `destructible-ammo-weight-audit.csv`
- `destructible-ammo-pool-usage-audit.csv`
- existing production `destructible-ammo-production-redirect-manifest.csv`

Status: **audit-only**. No ammo-weight gameplay controls are enabled in v0.9.26.44.
