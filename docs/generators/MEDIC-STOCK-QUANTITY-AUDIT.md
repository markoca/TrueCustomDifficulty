# Medic Stock Quantity Audit

**Baseline:** v0.2.35 / runtime CFG dump 2.0.5  
**Decision:** `STRUCTURALLY_READY_RUNTIME_PROOF` — candidate `Medic Stock Quantity`

## Proven surface

| Pool | Direct rows | Items |
| --- | ---: | --- |
| `MedicUniversal_TradeItemGenerator` | 25 | Bandage, Medkit, AntiRad, Water, ArmyMedkit, Vodka, Hercules, EcoMedkit |
| `MedicNoon_TradeItemGenerator` | 23 | Bandage, Medkit, AntiRad, Water, ArmyMedkit, Hercules, EcoMedkit |
| **Total** | **48** | medic consumable stock only |

Concrete medic wrappers consume these pools. Local extras such as Rostok `Echo` and Yanov artifacts/detector are outside this control.

## Exact semantics

`PlayerRank` is progression gating. `Chance` is availability for selected higher-tier items. `MinCount` / `MaxCount` consistently encode the candidate stock-quantity surface and already carry vanilla rank progression.

All 48 direct rows have both count fields. **42/48** have `Chance=1`; the remaining **6/48** are progression/rarity rows: ArmyMedkit at Experienced is `Chance=0.5` in both pools, Hercules is `0.1` at Experienced and `0.5` at Veteran in both pools. These six Chance values are explicitly outside the quantity control.

Examples from `MedicUniversal_TradeItemGenerator`: Bandage 5–8 → 6–9 → 8–10 → 10–12; Medkit 4–6 → 6–8 → 8–10 → 10–12; AntiRad 1–2 → 2–4 → 4–6 → 6–10. ArmyMedkit appears at Experienced as 1–2 with Chance 0.5, then 3–5 and 4–6 at higher ranks.

The candidate therefore changes **only `MinCount` / `MaxCount`** from their actual CFG values. It preserves `Chance`, `PlayerRank`, item SIDs, wrapper links and TradePrototype refresh.

## Runtime gate

Static mapping is 1:1 and structurally isolated, but the UI/writer is not promoted until the in-game contract in `MEDIC-STOCK-QUANTITY-RUNTIME-TEST.md` proves that these count fields directly control offered medic stock quantity.

There is no `Medic Stock Chance` candidate and no aggregate all-trader stock scalar.
