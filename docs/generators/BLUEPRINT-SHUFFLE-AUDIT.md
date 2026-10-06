# Blueprint Shuffle Audit

## Decision

**IMPLEMENTED / RUNTIME TEST REQUIRED** as `loot.blueprintShuffle`.

The shuffle is restricted to a proven one-to-one world-placement surface. It does not use `AllBlueprintsGenerator` directly and it does not touch quest/scripted blueprints.

## Source inventory

`AllBlueprintsGenerator` contains **80 unique blueprint item SIDs**.

- **79** of those 80 also have a dedicated `Stash_Blueprint_*` ItemGenerator.
- **77** dedicated stash generators have exactly **one** resolved physical `item_container_world` consumer each.
- **2** dedicated stash generators have no resolved runtime consumer and are excluded:
  - `Stash_Blueprint_SciSEVA_Scientific_Armor_Upgrade_1`
  - `Stash_Blueprint_SciSEVA_Scientific_Armor_Upgrade_2`
- the one `AllBlueprintsGenerator` item without a `Stash_Blueprint_*` counterpart is `Blueprint_FaustPsyResist_Quest_1_1`; it is quest-specific and is excluded.

The resulting production allowlist is therefore **77 world slots / 77 unique blueprint items**.

## Physical consumer proof

All 77 eligible generators resolve only through:

- context: `item_container_world`
- consumer type: `spawn_item_generator_settings`
- source: `GameData/SpawnActorPrototypes.cfg`

Each eligible generator has exactly one physical consumer and each consumer source actor is unique.

Container-type distribution:

| Container | Slots |
| --- | ---: |
| Bag | 26 |
| Backpack | 16 |
| SmallSafe | 14 |
| BigSafe | 9 |
| BlueBox | 8 |
| BackpackGrave_g | 4 |
| **Total** | **77** |

The complete allowlist is `blueprint-world-placement-manifest.csv` and the embedded production copy is `Data/blueprint-shuffle-manifest.csv`.

## Production behavior

`Blueprint Shuffle` is a boolean control.

### OFF

No blueprint patch is emitted. Vanilla placement is preserved.

### ON

At each Build:

1. load the exact 77-slot allowlist;
2. create a fresh random permutation of the 77 blueprint item SIDs;
3. require a derangement, so no eligible blueprint remains in its vanilla slot;
4. write one replacement `ItemPrototypeSID` into each existing `Stash_Blueprint_*` generator;
5. preserve `Chance=1`.

The shuffle is **without replacement**. No eligible blueprint is duplicated or lost.

## Why generator-content permutation is safe

The production patch changes the blueprint item inside each dedicated `Stash_Blueprint_*` generator instead of changing the physical actor reference.

That is equivalent for this audited surface because every included generator has exactly one world consumer. It also keeps the physical container actors and their placement completely untouched.

## Exclusions

The shuffle intentionally excludes:

- `Blueprint_FaustPsyResist_Quest_1_1`;
- the two `SciSEVA_Scientific` stash generators with no resolved world consumer;
- any blueprint or generator discovered outside the explicit manifest;
- quest/reward/scripted content;
- trader/NPC/player-gear domains.

## Runtime gate

The source and consumer boundary is exact. An in-game smoke test is still required to prove that all 77 generated blueprint substitutions load and unlock normally after repackaging.

Until that smoke test is completed, support status remains `runtime_test_required`.
