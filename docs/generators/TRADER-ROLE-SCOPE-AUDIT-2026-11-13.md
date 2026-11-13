# Trader Types and Consumer Scope — Source Audit (2026-11-13)

**Source-classified, not gameplay-implemented.** These are STALKER 2 CFG 2.0.5 extracted catalog identities, not a fresh original archive parity run. No UI, preset, CFG writer or game changes are made in this commit.

## Five merchant types and internal pools

| Role / type | Total roots | Mapped merchant-facing roots | Internal pools | Unresolved | Base / DLC1 |
| --- | ---: | ---: | ---: | ---: | --- |
| GENERAL_TRADER | 35 | **32** | 0 | 3 | 25 / 10 |
| MEDIC | 13 | **11** | 1 | 1 | 11 / 2 |
| TECHNICIAN | 16 | **16** | 0 | 0 | 15 / 1 |
| BARTENDER | 11 | **10** | 1 | 0 | 8 / 3 |
| GENERIC_NPC_TRADER | 9 | **9** | 0 | 0 | 9 / 0 |
| SHARED_STOCK_POOL (not a trader) | 12 | 0 | 12 | 0 | 12 / 0 |
| **Total** | **96** | **78** | **14** | **4** | **80 / 16** |

These are **source generator roots, not unique NPC instances**. Audit references: [834-row item source catalog](TRADER-ENTRY-CATALOG.md), [96-root scope/consumer survey](TRADER-STOCK-AUDIT.md), and machine-readable [96-root role ledger](trader-role-scope-catalog.csv). Cross-check via `python3 tools/audit_trader_roles.py`.

### General Traders

32 mapped stock roots. They include **21** named/location models, **3** specialized Rostok armor/NATO/Soviet models, **6** DLC1 Gurza and Suslov MQ11/SQ14/Stage2 models, **1** `DynamicTraderItemGenerator` and **1** `RC_TraderNPC_ItemGenerator`. The two special NPC generator forms belong here, not under generic faction NPC trader stock. Six quest/stage variants are not approved for any blanket edit until quest consequences are checked.

Three roots remain unresolved: `HimzavodTrader_ItemGenerator`, `Trader_Ammo_Experienced_ItemGenerator`, `Trader_Ammo_ItemGenerator`.

### Medics

11 mapped merchant-facing roots, one `MedicNoon_TradeItemGenerator` internal pool, and one unresolved DLC1 `VezaMedic_TradeItemGenerator`. `MedicUniversal_TradeItemGenerator` is both a direct TradePrototype consumer and reused as a target pool; never double-count it as an independent merchant instance. Medic wrappers sell chiefly medical supplies but some have their own direct artifacts/detectors. The existing `economy.medicStock` amount control is separate from the merchant role classification.

### Technicians

16 concrete roots, many with **only nested links** to T2–T4 attachment pools; several also point to ammunition, weapon or consumable pools. A type name alone cannot prove exclusive stock categories. Technician **repair and upgrade** actions are separate from their trade inventory or what they will buy.

### Bartenders

10 merchant-facing roots and one DLC1 internal `BartenderCNPP_TradeItemGenerator` pool. They mostly reference the universal drinks/food pool, but some also have artifacts, detectors, or even T1 gun/ammo pool links. `BartenderUniversal_TradeItemGenerator` is a cross-context target shared with named NPC inventories and must never be patched globally for bartender-only stock.

### Generic NPC Trader

Exactly nine roots: `GeneralNPC_TradeItemGenerator` plus Bandit, Corpus, Duty, Freedom, Mercenary, Militaries, Scientists and Spark variants. All nine are base-game direct-item catalogs; none is the `DynamicTraderItemGenerator` or `RC_TraderNPC_ItemGenerator`. Distinguish *trading profiles* from ordinary NPC loadout generators and NPC corpse loot.

### Shared Stock Pools (not a seller type)

12 root generators: Trader T1–T4 weapons, T1–T4 ammo, Soviet ammo and T2–T4 attachments. Together with `MedicNoon` and `BartenderCNPP` they make the 14 internal pool roots. Do not expose shared pools as separate traders.

## Cross-role source links: current direct evidence

```text
Trader_T1_Ammo_ItemGenerator        → General + Technician + Bartender
Trader_T1_Guns_ItemGenerator        → General + Bartender
Trader_T2_Ammo_ItemGenerator        → General + Technician
Trader_Attachments_T2_ItemGenerator → General + Technician
Trader_Attachments_T3_ItemGenerator → General + Technician
Trader_Attachments_T4_ItemGenerator → General + Technician
Trader_T3_Guns_ItemGenerator        → General + Technician
Trader_Cosnsumables_ItemGenerator   → General + Technician (also quest/script contexts)
BartenderUniversal_TradeItemGenerator → General + Bartender (also named NPC contexts)
```

Every entry is source-linked, but the pairings do not establish a safe edit boundary. **Per-role stock editing must patch each concrete trader's link, or clone/rewire common pools when distinct item selection is needed**, never remove a shared target used by other roles or scripted/quest contexts. A slider for one role must never silently alter another role.

## Separate consumer inventory for buy-acceptance audit

The 96-root stock inventory resolves **83 external TradePrototype consumer references**; the existing `trader-min-buy-condition-manifest.csv` contains **86 production trade profiles (68 base, 18 DLC1)**. These are not identical inventory universes. Examples of stock-graph consumer gaps: `BaseTraderNPC_Template` is a template excluded from production; the Veza Medic cross-scope consumers are DLC1 trade profiles even though the common medic stock pool is base-game. Several profile SIDs (e.g. Bereza DLC1 and bartender profiles) appear in the 86-profile allowlist without their own unique stock generator in the root census. A future **buyer-role manifest must be keyed by TradePrototype SID and actual file scope**, not assume that 78 shop-facing stock roots equals 78 purchase policies or infer buyer role solely from a generator name.

## Authoritative guardrails and next gates

1. The committed 96-root CSV is a static source-identity ledger. It is **not** a production writer allowlist.
2. Verify the catalog against original decoded version-matching base + DLC1 CFGs independently; current source audits were extracted earlier.
3. Trace each root's real `TradePrototype` (including DLC1 stage/quest variants and template inheritance). Resolve orphan/unknown profiles separately.
4. For buying restrictions, use `TradePrototypes.cfg` acceptance rules rather than `ItemGeneratorPrototypes.cfg` sell-stock definitions.
5. Validate actual BPatch output, preserved parallel edits (minimum durability / pricing), native item-type taxonomy and buy/sell behavior in-game before any new control is exposed.
