# World Weapon Shuffle / Density

## Compatibility model

The world-weapon system uses **36 regular weapon models** and the audited safe placement surface:

- **296 base-game locations**
- **376 locations with DLC1**

A weapon is no longer restricted to locations where that exact SID originally appeared. It may move only to a proven compatible weapon slot:

1. pistols -> pistol slots only;
2. every other regular weapon -> primary-weapon slots only;
3. progression tier may differ by at most one step (`T1 <-> T2`, `T2 <-> T1/T2/T3`, etc.).

This is the narrowest audited rule that removes the one-location bottleneck while still preserving slot role and progression.

## Why this is needed

Several regular models have only one authored exact-SID location. Exact-SID-only placement could therefore never guarantee more than one copy of every weapon.

The compatibility audit proves that the safe topology can guarantee:

- **base game: up to 6 copies of every regular weapon**
- **with DLC1: up to 8 copies of every regular weapon**

The writer verifies the guarantee using an exact max-flow allocation before emitting any files.

## World Weapon Density

The slider remains 0-100% in 5% steps.

Occupied locations:

`36 + round((available_locations - 36) * density)`

The writer derives the guaranteed minimum:

`min(floor(occupied_locations / 36), topology_cap)`

where the cap is 6 for base game and 8 with DLC1.

| Density | Base occupied | Base minimum each | DLC1 occupied | DLC1 minimum each |
|---:|---:|---:|---:|---:|
| 0% | 36 | once | 36 | once |
| 15% | 75 | twice | 87 | twice |
| 25% | 101 | twice | 121 | 3 times |
| 50% | 166 | 4 times | 206 | 5 times |
| 75% | 231 | 6 times | 291 | 8 times |
| 100% | 296 | 6 times | 376 | 8 times |

The live UI helper reports both the occupied-location count and this exact guaranteed minimum.

## Allocation order

Every Build performs:

1. calculate the target occupied-location count;
2. calculate the guaranteed minimum copies of every weapon;
3. solve weapon -> compatible tier/slot buckets using max-flow;
4. randomly choose concrete locations inside the assigned buckets;
5. fill remaining density locations with random compatible weapons;
6. remove every unselected eligible location.

The final distribution is then checked again to ensure every model met the promised minimum.

## Direct world actors

A direct weapon actor uses both:

- `SpawnedPrototypeSID`
- `ItemSID`

When a selected compatible location receives a different weapon, both fields are patched together.

`MeshPath` is intentionally not rewritten. Raw vanilla data contains multiple cases where `MeshPath` does not correspond to the actor's own weapon SID, demonstrating that this field is not a reliable runtime item identity for this surface.

## Generator weapon slots

The 14 proven `ItemGenerator` slots use the same compatibility/density allocation. Selected replacements preserve the authored Chance, Weight, durability, ammo-count and item-count fields.

## Exclusions

The system does not touch:

- NPC weapons;
- trader stock;
- quest/scripted rewards;
- named unique weapons;
- Guard variants;
- Gauss Rifle;
- RPG-7u;
- unresolved/special placements.

Status remains `runtime_test_required` until the compatible redistribution is validated in-game.
