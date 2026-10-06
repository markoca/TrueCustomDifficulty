# Trader Ammo Stock Quantity Audit

## Decision

**NO UI — incomplete quantity surface.**

A trader-ammo quantity control looks structurally attractive because the reusable ammo pools are highly regular, but it fails the project's 1:1 completeness rule. The audited reusable surface contains **52 ammo rows**. **51** expose vanilla `MinCount`/`MaxCount`; `Trader_SovietAmmo_ItemGenerator -> AVOG` is a legitimate trader ammo row with `Chance=1` and all-rank availability but has **no count fields**. Adding count fields would invent semantics rather than scale an existing vanilla parameter.

## Reusable ammo pools

| Pool | Rows | Count-bearing | Context |
|---|---:|---:|---|
| `Trader_SovietAmmo_ItemGenerator` | 10 | 9 | trader_stock |
| `Trader_T1_Ammo_ItemGenerator` | 7 | 7 | trader_stock |
| `Trader_T2_Ammo_ItemGenerator` | 7 | 7 | trader_stock |
| `Trader_T3_Ammo_ItemGenerator` | 11 | 11 | trader_stock |
| `Trader_T4_Ammo_ItemGenerator` | 7 | 7 | trader_stock |
| `Trader_NatoAmmo_ItemGenerator` | 10 | 10 | quest_scripted + trader_stock |

## Semantics

- All 52 audited rows use `Chance=1` and are available across Newbie/Experienced/Veteran/Master.
- 51 rows have explicit `MinCount`/`MaxCount`, so those rows are structurally quantity-scalable.
- `AVOG` in the Soviet pool is the sole no-count exception. It must remain vanilla; TCD must not synthesize missing count fields.
- `Trader_NatoAmmo_ItemGenerator` is shared by trader stock and quest/scripted context. A trader-only edit would require clone/rewire of trader links rather than editing that vanilla pool globally.

## UI consequence

Do **not** expose `Trader Ammo Stock Quantity`. A control with that label would imply complete trader-ammunition coverage while leaving AVOG untouched. A narrower “count-bearing ammo only” slider is technically possible but is not a useful or honest normal-UI difficulty control.

The existing aggregate `Trader Stock` NO-UI decision and the separate `Medic Stock Quantity` runtime-proof candidate are unchanged.
