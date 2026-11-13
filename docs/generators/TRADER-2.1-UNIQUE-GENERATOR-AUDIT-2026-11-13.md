# S.T.A.L.K.E.R. 2.1 — Unique Trader Generator Isolation Audit (2026-11-13)

## Evidence

Input: user-supplied `STALKER2-2.1 cfg dump.zip`, containing GameData and DLC1 CFG archives. Parsed `ObjPrototypes.cfg`, `TradePrototypes.cfg`, and `ItemGeneratorPrototypes.cfg` in both scopes. This report is an audit, **not a shipped individual-trader override**.

- 74 base + 18 DLC1 TradePrototype definitions = **92** (including generic/test templates, not 92 named traders).
- 3,086 base + 366 DLC1 top-level item generator definitions.
- Source-specific named NPC mapping previously exported: **46 base + 15 DLC1 = 61** ObjPrototype-to-TradePrototype links, all with defined target trade profiles.
- **83** distinct direct root generator SIDs referenced by TradePrototypes; **5** are referenced by more than one TradePrototype. Additional nested generator reuse must also be guarded by copy-on-write.
- Of the 61 specific mappings, **9** use a root SID shared across trade profiles.
- Existing TCD embedded merchant SELL inventory is a **2.0.5 frozen manifest** of **86** trade profiles and **2,939** leaf paths. It must not be treated as complete authoritative 2.1 data.

## Direct 2.1 NPC / trade mappings and generator separation

| NPC ObjPrototype | TradePrototype | Root stock generator | Direct root sharing |
|---|---|---|---|
| RostokBartender (Barkeep) | `Bartender_Rostok_TradePrototype` | `RostokBartender_TradeItemGenerator` | No |
| Rostok Arena bartender (Ganja) | `Bartender_RostokArena_TradePrototype` | `RostokArenaBartender_TradeItemGenerator` | No |
| TraderJunker (Ragman) | `Trader_Soviet_Rostok_TradePrototype` | `RostokTraderSoviet_TradeItemGenerator` | No |

The displayed NPC names are descriptive mappings; exact in-game localization requires matching CharacterPrototypes/localization SIDs. The three stock generator SIDs above are directly present in the 2.1 TradePrototypes.cfg.

## Verified shared-root examples among 61 NPC mappings

| ObjPrototype | Shared stock generator |
|---|---|
| FreedomSkoba | `SkobaMedic_TradeItemGenerator` |
| DutyVeza | `MedicUniversal_TradeItemGenerator` |
| ZalesieMedic | `MedicUniversal_TradeItemGenerator` |
| ZalesieBartender | `BartenderUniversal_TradeItemGenerator` |
| HimzavodMedic | `MedicUniversal_TradeItemGenerator` |
| NeutralNikolaic | `MedicUniversal_TradeItemGenerator` |
| DutyGera | `BartenderUniversal_TradeItemGenerator` |
| VartanikolajBurahovskij | `MedicUniversal_TradeItemGenerator` |
| BanditPonuryj | `BartenderUniversal_TradeItemGenerator` |

## DLC1 source mappings (not all independent merchants)

| ObjPrototype | TradePrototype |
|---|---|
| `FreedomGarpun` | `GarpunTrader_TradePrototype` |
| `ScientistSuslov` | `SuslovTrader_TradePrototype` |
| `FreedomSkoba` | `SkobaMedic_TradePrototype` |
| `FreedomSuslovTraderStage2` | `SuslovTraderStage2_TradePrototype` |
| `DutyGurza` | `GurzaTrader_TradePrototype` |
| `DutyGurzaStage2` | `GurzaTraderStage2_TradePrototype` |
| `DutyVeza` | `VezaMedic_TradePrototype` |
| `FreedomMednyj` | `MednyiTrader_TradePrototype` |
| `DutyGurza_Trader_MQ11` | `GurzaTraderMQ11_TradePrototype` |
| `FreedomSuslov_Trader_MQ11` | `SuslovTraderMQ11_TradePrototype` |
| `NeutralBolshak` | `BolshakTechnician_TradePrototype` |
| `FreedomKolba` | `KolbaBartender_TradePrototype` |
| `DutyGorcak` | `GorcakBartender_TradePrototype` |
| `DutyKapitanKomissarov` | `Trader_CementPlant_TradePrototype` |
| `FreedomDLC1Bereza` | `BerezaDLCTrader_TradePrototype` |

DLC1 has multiple stage-specific prototypes for the same characters. A unique-trader UI must group story stages intentionally while preserving each source TradePrototype SID.

## Required implementation gate

1. Re-extract the full 2.1 source-backed SELL offer leaf graph (base and DLC1), compare category coverage and generator paths with the frozen 2.0.5 manifest, and update integrity hashes/count assertions in a coordinated migration.
2. Introduce per-TradePrototype optional category overrides with tri-state inheritance: INHERIT / ON / OFF. Existing five role-level category settings remain the fallback.
3. Resolve each NPC to the exact trade SID from ObjPrototypes, not a guessed localized name; stage variants must be explicit.
4. For every edited trade profile, clone its root and all affected nested shared generators privately; never mutate source stock used by other traders or NPC/quest consumers.
5. Add writer audits for no-op defaults, override precedence, source scope and DLC gating, shared nested pools, unrelated trader stock, and missing category combinations.
6. Update the UI catalog/design documentation and ship code+tests+docs together on main. Compilation, Wine and gameplay acceptance remain outstanding.

**Decision:** Individual-trader controls are not safe to ship against the old 2.0.5 SELL manifest until the 2.1 re-extraction is complete.

## Direct recheck against uploaded S.T.A.L.K.E.R. 2.1 CFG archives (2026-11-13)

Input ZIP: `STALKER2-2.1 cfg dump.zip` (nested base and DLC1 CFG archives). Re-parsed both `TradePrototypes.cfg` and `ItemGeneratorPrototypes.cfg`, and joined the previously extracted 61 specific `ObjPrototypes.cfg` mappings to their stock roots.

- 92 TradePrototype definitions, of which **90 explicitly reference a stock generator**.
- **3,449** ItemGeneratorPrototype definitions (base + DLC1; keyed by scope/SID).
- **83 distinct stock root generator SIDs**, with **5 shared** across TradePrototype definitions.
- **0 missing direct root definitions**.
- **61 specific NPC mappings**; **9** use shared direct stock roots.
- Shared root SIDs: `MedicUniversal_TradeItemGenerator` (4 trade profiles), `SkobaMedic_TradeItemGenerator` (2), `GeneralNPC_TradeItemGenerator` (2), `SimpleFoodGenerator` (2), `BartenderUniversal_TradeItemGenerator` (3).
- The 2.1 base+DLC1 generator files contain **5,776 nested ItemGeneratorPrototypeSID reference occurrences**. Root-only isolation is therefore insufficient; copy-on-write must also isolate each affected nested generator edge.

Important: this verifies root references and named-NPC mapping, **not** the complete source-exact 2.1 per-item SELL leaf census. The current executable still embeds a frozen **2.0.5** 86-profile/2,939-leaf CSV. Do **not** change its integrity hash, source version label, or promote individual NPC category controls until all 2.1 leaf paths, category classification, DLC scopes and generated CFG patches have been validated.

Verified local export: `STALKER2_2.1_unique_trader_generator_audit.csv` with columns scope, ObjPrototype, NPCPrototype, TradePrototype, root generator, shared flag, consumer count and consumer SID list. The CSV is a user conversation artifact; it is not an embedded application resource.

**Next implementation gate:** re-extract source-exact 2.1 SELL leaf manifest; reconcile 2.0.5/2.1 profile differences; audit leaf path structure, zero-chance, rank/quantity/weight, nested edges; then implement per-trade tri-state category overrides with source-backed writer tests. No UI or writer change is asserted in this audit-only commit.

