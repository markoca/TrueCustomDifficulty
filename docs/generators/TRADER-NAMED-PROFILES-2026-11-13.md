# Source trade profiles: named and location-specific (S.T.A.L.K.E.R. 2 2.0.5)

## 2026-11-14 — Verified 2.1 named NPC stock categories (UI rationale reference)

This supplemental table is derived exclusively from `src/TrueCustomDifficulty/Data/trader-sells-2.1-runtime.csv` (the active production SELL inventory), with specific NPC/TradePrototype identities from `TRADER-2.1-UNIQUE-GENERATOR-AUDIT-2026-11-13.md`. These are categories of reachable *candidate stock*, not an exact claim about what is in a live shop; rank, Chance, refresh and progression still apply. The named mappings are source descriptions, not a verified localized name string. Source TradePrototypes for a character's different DLC1 stages are **not** independent NPCs. The list includes the informational Bolshak technician source, but does not re-enable a Technician role tab.

| Source-linked named trader / stage | Scope | TradePrototype SID | 2.1 candidate stock categories |
|---|---|---|---|
| Barkeep (Rostok) | base | `Bartender_Rostok_TradePrototype` | Weapons, Food, Drinks, Artifacts, Detectors |
| Ganja (Rostok Arena) | base | `Bartender_RostokArena_TradePrototype` | Weapons, Ammo, Food, Drinks |
| Ragman (Rostok) | base | `Trader_Soviet_Rostok_TradePrototype` | Weapons, Attachments, Ammo, Medical, Food, Drinks, Artifacts, Grenades |
| Garpun (DLC1) | dlc1 | `GarpunTrader_TradePrototype` | Weapons, Ammo, Medical, Food, Drinks |
| Suslov (DLC1) | dlc1 | `SuslovTrader_TradePrototype` | Weapons, Ammo, Medical, Food, Drinks |
| Suslov (DLC1, MQ11) | dlc1 | `SuslovTraderMQ11_TradePrototype` | Ammo, Medical, Food, Drinks |
| Suslov (DLC1, SQ14) | dlc1 | `SuslovTraderSQ14_TradePrototype` | Weapons, Armor, Ammo, Medical, Food, Drinks |
| Suslov (DLC1, Stage 2) | dlc1 | `SuslovTraderStage2_TradePrototype` | Weapons, Ammo, Medical, Food, Drinks |
| Gurza (DLC1) | dlc1 | `GurzaTrader_TradePrototype` | Weapons, Ammo, Medical, Food, Drinks |
| Gurza (DLC1, MQ11) | dlc1 | `GurzaTraderMQ11_TradePrototype` | Ammo, Medical, Food, Drinks |
| Gurza (DLC1, SQ14) | dlc1 | `GurzaTraderSQ14_TradePrototype` | Weapons, Armor, Ammo, Medical, Food, Drinks |
| Gurza (DLC1, Stage 2) | dlc1 | `GurzaTraderStage2_TradePrototype` | Weapons, Ammo, Medical, Food, Drinks |
| Skoba (DLC1) | dlc1 | `SkobaMedic_TradePrototype` | Medical, Drinks |
| Skoba (DLC1, Stage 2) | dlc1 | `SkobaMedicStage2_TradePrototype` | Medical, Drinks |
| Veza (DLC1) | dlc1 | `VezaMedic_TradePrototype` | Medical, Drinks |
| Veza (DLC1, Stage 2) | dlc1 | `VezaMedicStage2_TradePrototype` | Medical, Drinks |
| Mednyi (DLC1) | dlc1 | `MednyiTrader_TradePrototype` | Weapons, Ammo, Medical, Food, Drinks |
| Gorcak (DLC1) | dlc1 | `GorcakBartender_TradePrototype` | Food, Drinks |
| Kolba (DLC1) | dlc1 | `KolbaBartender_TradePrototype` | Food, Drinks |
| Bereza (DLC1) | dlc1 | `BerezaDLCTrader_TradePrototype` | Weapons, Armor, Attachments, Ammo, Medical, Food, Drinks, Binoculars |
| Bolshak (DLC1, technician) | dlc1 | `BolshakTechnician_TradePrototype` | Weapons, Ammo, Medical, Food, Drinks |

**Category editor scope:** the actual Trader Stock UI only toggles categories by the four player-facing merchant roles (General, Medic, Bartender, Generic NPC), not by the specific named NPC. The Technician profiles remain in source data/presets/writer but are not represented by a technician UI tab. **Binoculars** is a real 2.1 source category but has no dedicated category checkbox. For source-backed but unnamed/location-specific TradePrototype SIDs, refer to the remaining inventory below; never turn a TradePrototype SID into a claimed NPC name without an exact actor mapping.

Source: docs/generators/trader-buy-limits-86.csv. These are TradePrototype identifiers, NOT confirmed display names or actor NPC IDs. There are 86 source trade profiles, including 18 DLC1 profiles. Some DLC1 entries are stages of the same trader, not independent merchants. In particular, Barkeep is not explicitly present as a named source SID; Bartender_Rostok_TradePrototype is a candidate and requires an NPC-to-TradePrototype reference check.

| Scope | Source role | TradePrototype SID |
|---|---|---|
| base | GENERAL_TRADER | `Trader_Zalesie_TradePrototype` |
| base | GENERAL_TRADER | `Trader_ChemicalPlant_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Terikon_TradePrototype` |
| base | BARTENDER | `Bartender_Zalesie_TradePrototype` |
| base | MEDIC | `Medic_Zalesie_TradePrototype` |
| base | MEDIC | `Medic_ChemicalPlant_TradePrototype` |
| base | BARTENDER | `Bartender_ChemicalPlant_TradePrototype` |
| base | TECHNICIAN | `Technician_ChemicalPlant_TradePrototype` |
| base | MEDIC | `Medic_Terricon_TradePrototype` |
| base | BARTENDER | `Bartender_Terricon_TradePrototype` |
| base | GENERAL_TRADER | `Asylum_Trader_TradePrototype` |
| base | TECHNICIAN | `Asylum_Technician_TradePrototype` |
| base | MEDIC | `Asylum_Medic_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Ikar_TradePrototype` |
| base | TECHNICIAN | `Ikar_Technician_TradePrototype` |
| base | MEDIC | `Ikar_Medic_TradePrototype` |
| base | TECHNICIAN | `Backwater_Technician_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Sultansk_TradePrototype` |
| base | BARTENDER | `Bartender_Sultansk_TradePrototype` |
| base | BARTENDER | `BartenderBanditSultansk_TradePrototype` |
| base | TECHNICIAN | `Technician_Sultansk_TradePrototype` |
| base | MEDIC | `Sultansk_Medic_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Shevchenko_TradePrototype` |
| base | BARTENDER | `Bartender_Shevchenko_TradePrototype` |
| base | GENERAL_TRADER | `Trader_NewbeeVillage_TradePrototype` |
| base | TECHNICIAN | `Technician_NewbieVillage_TradePrototype` |
| base | MEDIC | `NewbieVillage_Medic_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Malakhit_TradePrototype` |
| base | BARTENDER | `Bartender_Malakhit_TradePrototype` |
| base | TECHNICIAN | `Technician_Malakhit_TradePrototype` |
| base | MEDIC | `Malakhit_Medic_TradePrototype` |
| base | GENERAL_TRADER | `Trader_CementPlant_TradePrototype` |
| base | BARTENDER | `Bartender_CementPlant_TradePrototype` |
| base | TECHNICIAN | `Technician_CementPlant_TradePrototype` |
| base | MEDIC | `CementPlant_Medic_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Armor_Rostok_TradePrototype` |
| base | GENERAL_TRADER | `Trader_NATO_Rostok_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Soviet_Rostok_TradePrototype` |
| base | BARTENDER | `Bartender_Rostok_TradePrototype` |
| base | BARTENDER | `Bartender_RostokArena_TradePrototype` |
| base | TECHNICIAN | `Technician_Rostok_TradePrototype` |
| base | MEDIC | `Rostok_Medic_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Yanov_TradePrototype` |
| base | BARTENDER | `Bartender_Yanov_TradePrototype` |
| base | TECHNICIAN | `Technician_Yanov_TradePrototype` |
| base | MEDIC | `Yanov_Medic_TradePrototype` |
| base | GENERAL_TRADER | `Trader_Pripyat_TradePrototype` |
| base | TECHNICIAN | `Technician_Pripyat_TradePrototype` |
| base | GENERAL_TRADER | `Trader_RedForest_TradePrototype` |
| base | GENERAL_TRADER | `EgerTrader_TradePrototype` |
| base | TECHNICIAN | `ZalesieTechnician_TradePrototype` |
| base | TECHNICIAN | `TerriconTechnician_TradePrototype` |
| base | TECHNICIAN | `PowerPlugTechnician_TradePrototype` |
| base | TECHNICIAN | `VartaTechnician_TradePrototype` |
| base | GENERAL_TRADER | `TraderSuska_TradePrototype` |
| base | GENERAL_TRADER | `VartaTrader_TradePrototype` |
| base | GENERAL_TRADER | `SelmaTrader_TradePrototype` |
| dlc1 | GENERAL_TRADER | `GarpunTrader_TradePrototype` |
| dlc1 | MEDIC | `VezaMedic_TradePrototype` |
| dlc1 | GENERAL_TRADER | `SuslovTrader_TradePrototype` |
| dlc1 | GENERAL_TRADER | `GurzaTrader_TradePrototype` |
| dlc1 | GENERAL_TRADER | `SuslovTraderSQ14_TradePrototype` |
| dlc1 | GENERAL_TRADER | `GurzaTraderSQ14_TradePrototype` |
| dlc1 | TECHNICIAN | `BolshakTechnician_TradePrototype` |
| dlc1 | MEDIC | `SkobaMedic_TradePrototype` |
| dlc1 | GENERAL_TRADER | `MednyiTrader_TradePrototype` |
| dlc1 | BARTENDER | `GorcakBartender_TradePrototype` |
| dlc1 | BARTENDER | `KolbaBartender_TradePrototype` |
| dlc1 | GENERAL_TRADER | `SuslovTraderMQ11_TradePrototype` |
| dlc1 | GENERAL_TRADER | `GurzaTraderMQ11_TradePrototype` |
| dlc1 | GENERAL_TRADER | `SuslovTraderStage2_TradePrototype` |
| dlc1 | GENERAL_TRADER | `GurzaTraderStage2_TradePrototype` |
| dlc1 | MEDIC | `VezaMedicStage2_TradePrototype` |
| dlc1 | MEDIC | `SkobaMedicStage2_TradePrototype` |
| dlc1 | GENERAL_TRADER | `BerezaDLCTrader_TradePrototype` |

The role-based category editor edits all trade profiles in each source role. This list is an inventory for future individual-trader overrides, not proof that each profile corresponds to a separate named character. No Barkeep-specific control is implemented without an authoritative actor mapping.
