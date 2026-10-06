# F1 Grenade Boxes

Version: 0.9.26.44

## Control

**F1 Grenade Boxes**

This World Loot slider controls the percentage of dedicated physical F1 grenade boxes that remain in the world.

- **0%** = vanilla; keep every audited F1 box.
- **50%** = remove roughly half of the audited F1 boxes.
- **100%** = remove every audited F1 box.

The removed subset is randomized without replacement on every Build.

## Audited surface

The production surface contains exactly **101** active non-quest `WorldMap_WP` actors:

- **61 base-game**
- **40 DLC**

Every actor is:

- `SpawnedPrototypeSID = D_WoodenAmmoCrate_01`
- linked to `DestructibleStash_F1`
- active with `SpawnOnStart = true`
- outside the quest/deferred exclusion set

`DestructibleStash_F1` yields `GrenadeF1` with `Weight = 1`, `MinCount = 1`, `MaxCount = 2`.

## Why this is a placement slider, not a Chance slider

The dedicated F1 box generator does not expose a standalone `Chance` field for F1. The F1 grenade is the content of the dedicated generator.

Other F1 `Chance` fields do exist in shared consumable generators, but those are reused by NPC/loadout and other domains. Editing them would not be a clean world-stash control.

Therefore this control changes the number of **physical dedicated F1 box actors**, not an internal shared F1 chance.

## Interaction with World Ammo Boxes
Dedicated F1 boxes are a **separate physical population** from the broad World Ammo Boxes removal surface.

`World Ammo Boxes` now targets only audited `DestructibleStash_AmmoSNG`, `DestructibleStash_AmmoNATO`, and `DestructibleStash_RGD` actors. All `DestructibleStash_F1` actors are excluded from that manifest.

Therefore the two controls are independent: changing `World Ammo Boxes` cannot remove a dedicated F1 box, and changing `F1 Grenade Boxes` cannot remove an ordinary ammo/RGD box.

## Runtime status

`runtime_test_required`

The writer emits `removenode` files for the randomly selected dedicated F1 box actors.
