# NPC Loose Ammo Entry Catalog

**Authoritative source:** `generator-local-source-item-entries.csv`  
**Scope:** every locally authored `EItemGenerationCategory::Ammo` row reachable from a direct `human_npc_standard` generator through generator composition  
**Total:** 26 rows across 2 generators

## Direct standard-human row

| Generator | Consumer | Rank | Item | Chance | Weight | MinCount | MaxCount | Source line | Decision |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog` | GeneralNPC_Neutral_CloseCombat_Prolog | ERank::Newbie, ERank::Experienced | `A545D` | 1 | — | 30 | 30 | 241956 | EXACT BUT PROLOG-SPECIFIC — not a generic standard-NPC surface |

## Shared nested `GamePass_Stash_ItemGenerator_Rare` ammo rows

These rows are reachable by `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom`, but they belong to a shared world-stash parent. They are cataloged for blast-radius visibility and are **not** NPC-only patch targets.

| Rank | Item | Chance | Weight | MinCount | MaxCount | Source line |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ERank::Newbie | `A918A` | 1 | — | 20 | 25 | 65147 |
| ERank::Newbie | `A012A` | 0.5 | — | 8 | 12 | 65153 |
| ERank::Newbie | `A545A` | — | 1 | 55 | 65 | 65166 |
| ERank::Newbie | `A919A` | — | 2 | 55 | 65 | 65172 |
| ERank::Experienced | `A918A` | — | 5 | 20 | 25 | 65221 |
| ERank::Experienced | `A012A` | — | 3 | 8 | 12 | 65227 |
| ERank::Experienced | `A045A` | — | 2 | 30 | 40 | 65233 |
| ERank::Experienced | `A545A` | — | 2 | 55 | 65 | 65246 |
| ERank::Experienced | `A919A` | — | 1 | 55 | 65 | 65252 |
| ERank::Experienced | `A556A` | — | 2 | 55 | 65 | 65258 |
| ERank::Veteran | `A762SniperA` | — | 2 | 10 | 15 | 65271 |
| ERank::Veteran | `A012E` | — | 3 | 8 | 12 | 65277 |
| ERank::Veteran | `A045A` | — | 2 | 30 | 40 | 65283 |
| ERank::Veteran | `A545A` | — | 2 | 55 | 65 | 65296 |
| ERank::Veteran | `A919A` | — | 1 | 55 | 65 | 65302 |
| ERank::Veteran | `A556A` | — | 2 | 55 | 65 | 65308 |
| ERank::Veteran | `A762SniperD` | — | 2 | 55 | 65 | 65314 |
| ERank::Master | `A762SniperA` | — | 2 | 10 | 15 | 65327 |
| ERank::Master | `A012E` | — | 3 | 8 | 12 | 65333 |
| ERank::Master | `A045A` | — | 2 | 30 | 40 | 65339 |
| ERank::Master | `A939A` | — | 2 | 30 | 40 | 65345 |
| ERank::Master | `A545A` | — | 2 | 55 | 65 | 65358 |
| ERank::Master | `A919A` | — | 1 | 55 | 65 | 65364 |
| ERank::Master | `A556A` | — | 2 | 55 | 65 | 65370 |
| ERank::Master | `A762SniperD` | — | 2 | 55 | 65 | 65376 |
