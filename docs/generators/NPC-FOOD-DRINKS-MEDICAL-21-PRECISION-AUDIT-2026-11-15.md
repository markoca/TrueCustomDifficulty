# NPC food, drinks and medical — precise source audit (S.T.A.L.K.E.R. 2.1)

**Date:** 2026-11-15  
**Status:** historical source audit and original design proposal. **Superseded for implemented UI semantics** by [NPC Consumable Chance — Literal CFG Chance](NPC-CONSUMABLE-CHANCE-FIRST-PASS-2026-11-15.md). Its earlier 0–200% scaling design is kept below as an audit record, not as the current UI contract.

## Evidence

Inspected `src/TrueCustomDifficulty/Data/cfg-writer-baseline.json` (`gameVersion: 2.1`), its `special.npcMedicalLootProduction` six source-backed pool templates, 33-row `npc-medical-production-redirect-manifest.csv`, `WriteNpcMedicalLootProduction` and `WriteNpcMedicalPoolTemplate` in `CfgPatchWriterService.cs`, `parameters.json`, and the existing `NpcLootLoadout` aggregate UI. This is repository-source analysis, not a fresh original 2.1 game dump extraction or a gameplay drop-rate measurement.

Six `GeneralNPC_Consumables_*` role pools have four authored `Diff × PlayerRank` gate blocks each, **104 item rows**, every row with a source `Chance`, and item-level counts. The **58 medical source Chance** values in the existing manifest match their associated source template rows exactly. The writer already clones all six pools and source-redirects **33 standard-human generator links** to private `TCD_GeneralNPC_Consumables_*` roots when standard Medical Drops changes. This is the safe scope for prospective additional item Chance edits.

### Exact per-item inventory

| Group | Item SID | Authored item rows | Role-pool coverage | Native Chance range |
| --- | --- | ---: | --- | --- |
| Food | `Bread` | 8 | Recon, Sniper, Stormtrooper | 0.60–1.00 |
| Food | `Sausage` | 8 | CloseCombat, Heavy, Noon | 0.60 |
| Food | `CannedFood` | 10 | Heavy, Noon, Recon, Sniper | 0.50–1.00 |
| Drinks | `Energetic` | 12 | Noon, Recon, Sniper | 0.15–1.00 |
| Drinks | `Beer` | 4 | CloseCombat | 0.40–1.00 |
| Drinks | `Vodka` | 4 | Stormtrooper | 0.10–0.50 |
| Medical | `Bandage` | 24 | All six | 0.05–1.00 |
| Medical | `Medkit` | 20 | All except Sniper | 0.05–1.00 |
| Medical | `ArmyMedkit` | 6 | Heavy, Noon, Sniper | 0.20–0.30 |
| Medical | `AntiRad` | 8 | Heavy, Noon, Recon, Stormtrooper | 0.30–0.50 |
| **TOTAL** | **10 SIDs** | **104** | **6 role pools** | |

**Important Water exception:** `Water` does **not** occur in these 104 standard NPC consumable template rows. The native Water source is the separate optional `NPC_Water_Smart` corpse bonus package and must not appear as an ordinary Drinks item slider. `NPC_Medicine_Smart` likewise remains an independent extra bonus source; a standard-medical 0% cannot imply zero medical items from all corpse sources.

### Source controls available without touching item counts

- For any of the ten SIDs, scale each authored `Chance` proportionally: `newChance = clamp(originalChance × multiplier, 0, 1)`; preserve original `Diff`, `PlayerRank`, `ItemPrototypeSID`, `MinCount`, `MaxCount`, category branches, role-specific memberships and other fields. No global flat percentage: 50% of a 20% original roll is 10%, not 50%.
- The 10 SID rows are grouped exactly: Food **26** (Bread 8, Sausage 8, CannedFood 10), Drinks **20** (Energetic 12, Beer 4, Vodka 4), Medical **58** (Bandage 24, Medkit 20, ArmyMedkit 6, AntiRad 8).
- The original `loot.npcMedicalChance` global medical slider already scales all 58 medical Chance rows. Introducing per-item medical controls without a precedence contract could double-scale or silently replace that slider. Recommended behavior: leave global Medical Drops at its existing stable preset ID and allow an **optional per-item override** (unchecked by default) that replaces the global multiplier only for its SID, while unchecked SIDs keep the global multiplier. Never multiply a per-item setting by the global setting by accident. Persist separate opt-in flags and values.
- Food and Drinks have no pre-existing independent corpse-category Chance sliders; a compact 3+3 item layout could safely use a neutral 100%-of-native starting value for each. If controls are added, disable their override by default, so an untouched preset produces **no new CFG clones**.
- All 84 fixed one-item stacks and the remaining 20 variable-count rows should retain their authored Min/Max. Do not introduce an undifferentiated consumable-quantity slider.

### Production writer requirements for future implementation

1. Reuse the *single existing six-pool-clone fanout* and *single 33-link redirect set* whenever **any** food, drinks or medical sidecar is active. Never emit separate competing clones per SID/category.
2. Extend source-backed template generation to select a multiplier by `ItemPrototypeSID`. Retain original source `Chance` as the scaling base. If a medical row has its per-item override enabled, it uses that multiplier; otherwise it uses `loot.npcMedicalChance`. Food and drinks use override multiplier when active, otherwise exactly original Chance. Treat all ten SIDs as a closed allowlist, reject other SIDs.
3. Protect every non-target original field; reject changed template/redirect counts, missing original SID/Chance fields and scope drift before writing. Only 33 repeatable human links; never change shared vanilla pools, zombies, named NPCs, Prologue, traders, world stashes or scripted corpses.
4. Preserve the existing rank/difficulty gates: four native templates per pool represent (Easy/Medium/Hard vs internal Stalker) × (Newbie/Experienced vs Veteran/Master progression ranks). This does not equate to 16 fully independent authored combinations.
5. Add production writer assertions for each SID individually at 0%, 50%, 100%, 200% and all-default; verify emitted 104 entries, medical/food/drink independence, 33 redirect identity, source-relative values and unchanged Min/Max, 2.1 fallback, preset load/save/disabled states, and no duplicated source clone/redirect blocks when multiple items change.
6. Actual in-game corpse tests must use newly generated ordinary human actors; separately track `NPC_Medicine_Smart`, `NPC_Water_Smart`, standard consumables, and mod-added items.

### Compact UI proposal — not yet implemented

Keep **NPC LOOT & EQUIPMENT** as the owning board; integrate a single compact optional editor area with three simple item groups (Food 3, Drinks 3, Medical 4), visually matching the existing controls and Stamina Usage slider alignment. One item row = concise label, small checkbox, proportionate 0–200% track and aligned changed-only yellow percentage at right. An unchecked row stays grey and leaves that SID at its existing effective base. Avoid redundant `BASE`/`VANILLA` labels, long explanations inside rows, separate edge strips, or a full-window board. Retain the global Medical Drops control above the optional Medical row overrides and explain override precedence only in the rationale.

This audit **does not implement** the ten optional sliders, add catalog IDs or modify gameplay writer behavior. It records the exact source boundary and safe interaction contract for a subsequent explicit implementation.
