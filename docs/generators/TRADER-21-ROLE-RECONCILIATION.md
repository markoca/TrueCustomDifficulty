# 2.1 Trader Role Reconciliation — Source-backed gate

Compared the 90 stocked 2.1 TradePrototype SIDs against the prior 86-profile role list by exact `(scope, trade_sid)`. **All 86 legacy SIDs remain present** in 2.1. **Four additional stocked profiles** were found: `AllTraderNPC`, `BaseTraderNPC_Template`, `BasicTrader`, `TradeTest` (all base scope).

The accompanying `trader-21-role-reconciliation.csv` contains all 90 rows. The 86 matching roles are marked **legacy_2.0.5_role_pending_2.1_review**: a matching SID alone does not prove unchanged role semantics. The four new profiles are explicitly **UNRESOLVED**, rather than guessed into the general trader category. All are marked not eligible for the unique-trader UI pending NPC confirmation.

The remaining two of 92 TradePrototype definitions have no reachable stock leaves. Do not invent rows for them in the SELL offer manifest.

**Gate:** Reconcile the four new profiles' intended role or exclude templates/test profiles explicitly, validate all 86 inherited roles against 2.1 source behavior, then migrate the 3,150-leaf manifest and writer together. Never silently fall back to 2.0.5. No executable 2.1 activation is claimed by this audit.

## 2.1 direct ObjPrototypes references — verified

Examined the uploaded base and DLC1 `ObjPrototypes.cfg` for exact `TradePrototypeSID` assignments:

| 2.1 profile | Direct ObjPrototype references | Assessment |
|---|---|---|
| `AllTraderNPC` | `AllTraderNPC` | Generic NPC template, not a named unique merchant |
| `BaseTraderNPC_Template` | none | Template; exclude from unique-trader UI |
| `BasicTrader` | `FaustBase`, `FaustBoss`, `FaustSummon`, `NPCBase`, `ScarBoss` | Shared base/boss fallback; not a merchant to expose individually |
| `TradeTest` | none | Test profile; exclude from unique-trader UI |

This is an **exact direct reference audit of ObjPrototypes**, not an exhaustive audit of indirect inheritance or all other game CFG references. Keep all four profiles in the 2.1 source graph for accurate census; do not surface them as individually named traders. `AllTraderNPC` and `BasicTrader` are referenced and must not be discarded from source processing just because they are generic. No runtime role was guessed: role classification still requires a reviewed source-backed rule before activating category switches.

## Source-exact 2.1 TradeGenerators verification

Re-read `GameData/TradePrototypes.cfg` for the four additional source profiles:

| SID | Stock generator(s) | Observed trade configuration | Decision |
|---|---|---|---|
| `AllTraderNPC` | `AllTraderItemGenerator` | single ConstTrue entry; buy 0.5, sell 1.0 | generic template, not a unique trader |
| `BaseTraderNPC_Template` | `GeneralNPC_TradeItemGenerator` | single ConstTrue entry; buy 0.3, sell 1.0; durability thresholds | generic template, not a unique trader |
| `BasicTrader` | `SimpleFoodGenerator` | single ConstTrue entry; buy 0.5, sell 1.0 | generic/base actor stock, not a unique trader |
| `TradeTest` | `SimpleFoodGenerator` (slot 0), `TraderItemGenerator` (slot 1) | second stock slot gated by `PlayerRankExperienced`; different modifiers and buy limitations | test-only, not a unique trader |

**Critical writer invariant:** `TradeTest` has **two** `TradeGenerators` entries, while the existing SELL writer hardcodes `TradeGenerators/[0]` when relinking. A 2.1 writer must retain each source slot index and condition, clone/relink only its targeted slot, and never rewrite slot 1 through slot 0. The earlier 3,150-leaf graph can include stock from multiple slots of the same trade; it must be augmented with `trade_generator_slot` before becoming the production manifest.

The four profiles remain in the 2.1 source inventory but are not exposed in the unique-trader UI. This verifies their direct source configuration, **not** their exact game runtime reachability. The production SELL writer is still 2.0.5 until slot-aware migration and CFG output tests pass.

## Writer: slot-aware clone/relink groundwork (2026-11-13)

The production SELL writer's internal offer record now has `TradeGeneratorSlot`. It groups source rows by `(trade scope, trade SID, slot)`, includes nonzero slot in private clone identity, and targets `TradeGenerators/[slot]` rather than hard-coded `[0]`. For slot 0, clone SID hashing is intentionally unchanged, preserving existing 2.0.5 output and preset behavior. The currently embedded legacy CSV has no slot column, so its loader explicitly assigns slot 0; **this change does not activate 2.1**.

Static guard: `python3 tools/audit_trader_sell_slots.py`. The new 2.1 manifest must supply the actual per-leaf slot, including multiple stock roots for `TradeTest`, and writer source integrity checks must be migrated before enabling it. Static inspection is not compiled/runtime CFG validation.
