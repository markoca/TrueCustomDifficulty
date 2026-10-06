# World Attachment Placement Audit

Version: 0.9.26.44 audit-only

## Result

A separate world-attachment shuffle is technically viable, but the attachment domain must remain independent from the weapon shuffle.

The current base data contains **217** descendants of `TemplateAttach`. Most of those are not player-loot items: default magazines, rails, iron sights, internal muzzle pieces, lasers/grips used as weapon components, and other implementation data.

A conservative inventory-item filter requires all three of:

- positive `Cost`;
- inventory `Icon`;
- `FittingWeaponsSIDs`.

That leaves **54 player-loot attachment prototypes**:

| Category | Lootable prototypes | Conservative safe-world pool |
|---|---:|---:|
| Magazine | 33 | 28 |
| Optic | 11 | 5 |
| Suppressor | 7 | 4 |
| Underbarrel | 3 | 2 |
| **Total** | **54** | **39** |

The conservative **safe-world pool is 39 attachment models**.

## Proven world placement surface

### Direct pickups

There are **7** safe direct `WorldMap_WP` attachment pickups:

- base: 5
- DLC1: 2

### Stash / generator placements

After resolving `ItemGeneratorPrototypes.cfg` into actual `SpawnActorPrototypes.cfg` consumers and excluding quest-referenced/deferred WorldMap usages:

- **52 safe generator definitions**
- **57 attachment entries** inside those definitions
- **55 physical generator usages**
- **53 unique physical stash/container actors**
- **60 physical attachment-entry usages**

Adding the direct pickups gives **67 conservative physical attachment placement slots/usages**.

This number is deliberately described as placement **usages**, not yet as independently writable slots, because two generator definitions are shared by multiple physical actors.

## Shared-generator caveat

Most safe attachment generators are one-to-one with a physical world actor. Two are reused:

- `Stash_GunAK74_MagIncreased` — 2 physical WorldMap consumers
- `Stash_RU_Silen_3` — 3 physical WorldMap consumers

If we want every physical attachment location to shuffle independently, these shared definitions need the same clone-and-redirect treatment used elsewhere in the project. Otherwise several locations would receive the same shuffled result together.

## Exclusions

**8 WorldMap generator usages** are conservatively excluded because they are quest-referenced or otherwise deferred by the safe filter.

There are also **15 lootable attachment prototypes** that do not currently belong to the conservative safe-world pool. They may exist only in traders, benches, rewards, non-WorldMap contexts, or currently unproven generator paths.

They must not be pulled into the world shuffle merely because they are valid inventory items.

## Internal attachment components are not loot

Examples such as:

- `EN_Laser_1`
- `RU_Laser_1`
- `EN_Grip_1`
- `RU_Grip_1`
- `EN_SinMuz_*`
- `RU_SinMuz_*`
- default weapon magazines and rails

appear under the `TemplateAttach` inheritance tree, but many have no inventory cost/icon/fitting contract. They are implementation components, not part of the world-loot attachment pool.

## Compatibility model for a future shuffle

A world attachment shuffle should be separate from weapon shuffle and should not mix arbitrary attachment classes.

The natural top-level buckets are:

1. **Optics**
2. **Suppressors**
3. **Magazines**
4. **Underbarrel attachments**

`FittingWeaponsSIDs` is the authoritative compatibility metadata for the item itself. For magazines this is especially important because most are platform-specific.

For *world-location* shuffling, weapon fitting does not mean an attachment can only be found beside a matching gun. However, it gives us a reliable identity/compatibility contract and allows us to prevent nonsensical category mixing.

Before implementation, the remaining required audit is **progression tier mapping** for the 39-model safe pool. The strongest candidate sources are trader attachment tiers, cost bands, and existing stash placement progression.

## Proposed eventual controls

Do **not** add these to production yet; this audit only establishes the surface.

- `World Attachment Shuffle`
- optionally `World Attachment Density`

The shuffle should operate only over the proven safe-world surface and preserve category + progression constraints. Trader stock, quest rewards, benches, unique/special attachment placements and internal weapon components remain separate domains.

## Evidence files

- `world-attachment-master-pool-audit.csv`
- `world-attachment-safe-generator-usages.csv`
- `world-attachment-safe-direct-pickups.csv`
- `world-attachment-excluded-world-usages.csv`
