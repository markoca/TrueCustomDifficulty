# World Gear Armor Tier Matrix

## Production-safe progression anchors

Only player gear-rank generators are accepted as world-loot progression evidence:

- **T1 / Newbie:** `Newbee_Neutral_Armor` — `NewbieGearPC_Generator`
- **T2 / Experienced:** `Zorya_Neutral_Armor` — `ExperiencedGearPC_Generator`
- **T3 / Veteran:** `SEVA_Neutral_Armor` — `VeteranGearPC_Generator`
- **T4 / Master:** `Exoskeleton_Neutral_Armor` — `MasterGearPC_Generator`

## Rejected tier signal: faction/NPC PlayerRank

`GeneralNPC_*_Armor` rank branches are retained as NPC loadout metadata but are **not** used to tier world-stash armor. Their semantics are faction/NPC equipment scaling and they produce progression contradictions when interpreted globally. Examples:

- `GeneralNPC_Scientists_Armor`: `Anomaly_Scientific_Armor` is available at `ERank::Newbie`.
- `GeneralNPC_Monolith_Armor`: `Battle_Monolith_Armor` is available at `ERank::Newbie, ERank::Experienced`.
- `GeneralNPC_Freedom_Armor`: a clean faction-local Rook -> Battle -> Heavy/SEVA -> Exoskeleton progression exists, but this still describes Freedom NPC loadouts rather than global stash progression.

Therefore faction rank, price and durability may be used as secondary audit metadata only. They cannot independently admit an armor into a generic world-loot replacement pool.

## Current consequence

The generic armor replacement pool remains intentionally narrow: **4 proven candidates**. Other regular armors stay `REVIEW` until world/trader/location evidence establishes a safe global progression equivalence.
