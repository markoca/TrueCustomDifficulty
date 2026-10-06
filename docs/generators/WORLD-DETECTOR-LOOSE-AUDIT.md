# Loose World Detector Audit

Version: 0.9.26.44 audit-only

## Result

The loose detector domain is much smaller and cleaner than weapons or attachments.

There are **7 active safe direct `WorldMap_WP` detector pickups**:

- **base game: 4**
  - `Echo` ×2
  - `Gilka` ×2
- **with DLC1: 7**
  - the same 4 base pickups
  - `Gilka` ×3 additional DLC1 pickups

The safe loose-world model set therefore contains only:

- `Echo`
- `Gilka`

## Excluded direct detector actor

One additional base `Gilka` exists on `WorldMap_WP`:

- actor `19FB54914B80A8E293E6F2AA84A56AAB`
- `SpawnOnStart = false`
- no external CFG reference was found outside `SpawnActorPrototypes.cfg`

It is conservatively classified **DEFERRED_EXCLUDED**. It is not part of the safe loose-detector surface.

## Other regular detector models

### Bear

`Bear` is a normal detector prototype (`Cost = 6000`) but has **no active direct WorldMap pickup**.

It appears in other domains such as traders, rewards, stash/loadout generators and player gear presets. Those are not loose world placements.

### Veles

`Veles` (`Cost = 10000`) has **no active direct WorldMap pickup**.

Its visible authored distribution is trader/reward oriented, including the explicit `E08_MQ03_reward_var5_Veles` generator.

### Svarog

DLC1 defines `Svarog` as a `Veles` derivative.

A direct `Svarog` item actor exists, but only on `DestructibleTestMap_2`. It is therefore test content, not world loot.

### EchoE01

`EchoE01` derives from `Echo` and explicitly sets:

- `IsQuestItem = true`
- `IsQuestItemPrototype = true`

It is excluded completely from loose detector logic.

## Generator audit

`ItemGeneratorPrototypes.cfg` contains many detector references, especially `Echo`, but these belong to distinct domains:

- NPC / named-character loadouts
- trader stock
- quest/reward generators
- player gear presets
- a small number of non-WorldMap stash contexts

No generator-backed **active physical WorldMap loose-detector surface** was proven.

Therefore the conservative loose-detector surface is **direct SpawnActor only**.

## Progression identity

The regular detector prototype progression is strongly reflected by cost and functionality:

| Detector | Cost | Loose World |
|---|---:|---:|
| Echo | 2000 | 2 base |
| Gilka | 4000 | 2 base + 3 DLC1 |
| Bear | 6000 | 0 |
| Veles | 10000 | 0 |
| Svarog | 10000 | 0 |

For a future **Loose Detector Shuffle**, the safest implementation is to preserve the exact authored loose multiset:

- base: `Echo ×2 + Gilka ×2`
- DLC1-inclusive: `Echo ×2 + Gilka ×5`

and shuffle only those items among the 4/7 proven active locations.

This would refresh detector locations without injecting Bear, Veles or Svarog into a domain where vanilla does not place them loosely.

## Candidate control

A future standalone toggle could be:

**World Detector Shuffle**

Helper:

`Randomize Echo and Gilka across their existing loose world locations.`

No density slider is recommended from this audit: the surface contains only 4/7 pickups and removing detector spawns would directly reduce an already sparse progression resource.

## Evidence files

- `world-detector-loose-placement-audit.csv`
- `world-detector-master-domain-audit.csv`
