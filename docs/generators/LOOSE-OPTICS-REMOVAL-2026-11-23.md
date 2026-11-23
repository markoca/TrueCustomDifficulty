# 2026-11-23 — Food/Drink rank selector and Remove Loose Binoculars/NVG

## Food/Drink rank selector — current UI lock

In Loot → Breakable Boxes → FOOD / DRINK BOXES, the four source-selecting chips read **NEWBIE | EXPERIENCED | VETERAN | MASTER** (no EXP/VET abbreviations). **RANK:** is immediately to the left of those chips, and the complete five-element row remains centered. Rank tabs are 94 DIP wide with 5 DIP gaps; the left prefix uses 40 DIP plus 10 DIP gap, giving a 441-DIP selector group. Below a 760-DIP aggregate width, selector and branch tracks move 24 DIP below the separate section heading. The existing two 2×2 Food/Drink knob groups, source semantics and preview bar values remain unchanged.

These four ranks select **player-rank item-generator sources, NOT item weight or character encumbrance**. NEWBIE edits 10 relative `Weight` multipliers; EXPERIENCED/VETERAN/MASTER independently edit 10 direct Chance sources each, 0–100% in 10% increments. The selected rank changes only its own sources. Changed numeric values alone remain yellow; native hover chrome and hit targets remain.

## New REMOVE checkbox — direct binocular and NVG world pickups

`loot.looseOpticsRemoved` is an independent Vanilla-OFF toggle in Loot → World Loot → WORLD ITEM SPAWNS → REMOVE, with compact caption **Binoculars / NVG** and rationale **Remove Loose Binoculars / NVG**. Four REMOVE rows now fit a taller card (155 DIP), without changing WEAPONS and SHUFFLE card contents. For less than 560 DIP of card content width, the cards use the stacked layout so the full word **Binoculars** is not cut off. On narrow layouts the lower SHUFFLE/REMOVE pair remains aligned without clipping.

**Original source:** user-provided `STALKER2-2.1 cfg dump(3).zip` → base `GameData/SpawnActorPrototypes.cfg` and DLC1 `DLCGameData/DLC1/SpawnActorPrototypes.cfg`. The exhaustive `WorldMap_WP` direct Item scan identifies **17** active direct loot actors:

| Scope | Binocular item IDs | NVG IDs | Total |
|---|---|---|---:|
| Base | Binoculars_01 ×4, Binoculars_02 ×5, Binoculars_03 ×3 | NVG_Gen1/Gen2/Gen3 ×1 each | 15 |
| DLC1 | Binoculars_03 ×1 | NVG_Gen2 ×1 | 2 |
| **Total** | **13 binoculars** | **4 NVG** | **17** |

Each actor has a unique 32-digit ActorSID, `LevelName=WorldMap_WP`, `SpawnOnStart=true`, `SpawnType=ESpawnType::Item`, and matching `ItemSID=SpawnedPrototypeSID`. A source text sweep over **all other base/DLC1 CFG files** found no exact reference to any of the 17 actor identifiers. This **does not certify runtime scripts or already-instantiated saved items**.

The exact manifest is `src/TrueCustomDifficulty/Data/loose-optics-direct-manifest.csv`. OFF emits no edits. ON writes **15** direct `<ActorSID> : removenode` output files for base game plus **two** DLC1-only files when DLC1 is installed, using the existing source-scoped per-actor removal writer. Manifest count, content type, source path, unique actor identity and DLC gate are checked before writing. This does **not** suppress binocular/NVG items offered by traders, placed in stashes, used by NPCs or named/quest characters, mod-added `ArGogglesT1/T2/T3`, or any shared item-generator pools; no item prototype is modified.

**Factory role philosophy:** Hardcore, Hardcore+, Outcast = ON, to increase the importance of traders and equipment acquisition. The other nine = OFF, preserving authored world pickups. Older user presets without the ID inherit Vanilla OFF. All twelve factory profiles now persist 497 source values.

**Regression:** `python3 tools/audit_loose_optics.py` checks the source catalog, baseline, manifest, four-row UI and all 12 profile defaults; `tests/LootWorldPlacementContract/LooseOpticsRemoval.cs` exercises the real C# writer and verifies zero edits while OFF, and exactly 15/17 actor-specific removals while ON. Runtime on Windows/Wine and existing-save behavior still require game testing.
