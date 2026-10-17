# Player-facing gear progression rank catalog

This is a **reference-only**, source-grounded crosswalk of player progression ranks and equipment tiers. It is **not** a region-to-rank mapping, does not claim each map region has a single rank, and changes no loot/shuffle behavior.

| Tier | Rank | Weapons | Armor anchors |
|---|---|---:|---:|
| T1 | Newbie | 8 | 1 |
| T2 | Experienced | 10 | 1 |
| T3 | Veteran | 10 | 1 |
| T4 | Master | 6 | 1 |

The **34** regular weapon SIDs use the existing documented `Trader_T1..T4_Guns_ItemGenerator` evidence in `WORLD-GEAR-WEAPON-TIER-MATRIX.md`. The **4** armor SIDs are explicit `NewbieGearPC`, `ExperiencedGearPC`, `VeteranGearPC`, and `MasterGearPC` anchors from `WORLD-GEAR-ARMOR-TIER-MATRIX.md`. The CSV records source and confidence for every entry.

**Excluded / review:** `GunFora_ST` lacks explicit tier; Gauss, RPG and unique variants are special; other armor models have no validated global tier and must not be assigned based solely on price, NPC faction rank, or trader presence. The existing `WORLD-GEAR-ARMOR-PLAYER-FACING-AUDIT.md` explains this limitation.

**Regional progression:** no verified `region → PlayerRank` contract has been established from the supplied game CFG. Region difficulty, local NPC rank and item tier must remain separate until actual source evidence links them. This CSV must not be consumed as an automatic region-based loot distribution table.

Source file: `src/TrueCustomDifficulty/Data/gear-progression-ranks.csv`. This is an audit artifact only; no UI, runtime or generator changes.
