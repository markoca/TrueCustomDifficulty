# Medic Stock Quantity — Runtime Proof Contract

**Version:** v0.2.35  
**Status before test:** `STRUCTURALLY_READY_RUNTIME_PROOF`

## What this test proves

This test answers one question only: do `MinCount` / `MaxCount` on the 48 direct rows in `MedicUniversal_TradeItemGenerator` and `MedicNoon_TradeItemGenerator` determine the quantity offered by medics when their stock is generated/refreshed?

It does **not** test or alter item availability, player-rank progression, trader refresh timing, local trader extras, prices, or the general trader inventory.

## Fixed test transforms

Build two diagnostic variants from the same baseline:

| Variant | `MinCount` | `MaxCount` | Purpose |
| --- | ---: | ---: | --- |
| LOW | 1 | 1 | unmistakably compress normal medic quantities |
| HIGH | 20 | 20 | unmistakably exceed normal medic quantities |

Apply the constant to **all 48** proven medic rows only. Preserve every `Chance`, `PlayerRank`, item SID and surrounding generator structure byte-for-byte apart from the two count values. Do not alter TradePrototype refresh settings.

## Primary observations

Use items with `Chance=1` at the player's current rank as the primary evidence. Bandage, Medkit, AntiRad and Water are the preferred probes because their appearance is not confounded by the six sub-1 Chance rows.

Test at least one medic backed by `MedicUniversal_TradeItemGenerator` and the Noon medic path backed by `MedicNoon_TradeItemGenerator`. Force/await a **real stock generation or refresh** between variants; merely reopening the same already-generated inventory is not evidence.

For each tested medic and variant, record the visible quantity of the guaranteed probe items after generation/refresh.

## Safety observations

The six non-guaranteed rows must retain vanilla availability semantics:

- ArmyMedkit / Experienced: `Chance=0.5` in Universal and Noon.
- Hercules / Experienced: `Chance=0.1` in Universal and Noon.
- Hercules / Veteran: `Chance=0.5` in Universal and Noon.

Their presence/absence is **not** a quantity-test failure. The important safety condition is that the test build does not rewrite these Chance values to force them into stock.

## PASS

Promote `Medic Stock Quantity` only if all of the following are observed:

1. LOW generation produces quantity 1 for the guaranteed probe rows that are offered at the tested rank.
2. HIGH generation produces quantity 20 for the same guaranteed probe rows after a genuine regeneration/refresh.
3. The behavior is reproduced on both Universal and Noon medic paths.
4. Item availability/progression remains governed by the unchanged `Chance` and `PlayerRank` fields.
5. No unrelated trader stock or local medic extras are changed by the test build.

If these conditions pass, the runtime meaning matches the static mapping and the control may be promoted as **Medic Stock Quantity**, with the production writer scaling only the actual `MinCount` / `MaxCount` values.

## FAIL / inconclusive

Do not promote the control if count changes do not appear in newly generated stock, if the values affect some other behavior, if only one medic path follows them, or if stock caching/refresh cannot be ruled out. In that case keep the candidate audit-only until the runtime mechanism is understood.
