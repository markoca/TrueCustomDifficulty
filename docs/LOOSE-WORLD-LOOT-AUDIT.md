# Loose World Loot Audit

## Decision

True Custom Difficulty exposes one standalone **Loose World Loot** slider.

- `0%` = Vanilla; no loose-loot actor removal patch is emitted.
- `100%` = remove every actor in the audited allowlist.
- Intermediate values remove `round(eligible_count × fraction)` unique actors at build time.
- Selection is random without replacement for each build.

## Audited surface

Source data: base-game and DLC1 `SpawnActorPrototypes.cfg`.

| Domain | Base | DLC1 | Total |
|---|---:|---:|---:|
| Dynamic `PackOfItems` | 4,588 | 807 | 5,395 |
| Static `Item` | 1,408 | 430 | 1,838 |
| **Eligible loose world loot** | **5,996** | **1,237** | **7,233** |

Dynamic allowlisting is limited to ordinary ammo, food/drink, medical/stimulant and grenade PackOfItems groups. Static allowlisting is limited to ordinary item SIDs proven through those vanilla pools.

## Explicit exclusions

The slider does **not** target:

- weapons or armor;
- artifacts / Arch artifacts;
- detectors or magazines;
- cabinets / item containers;
- boxes/crates (controlled by separate World Loot sliders);
- stashes;
- NPC inventories or corpse generators;
- traders;
- quest/unique item SIDs;
- quest logic levels and test maps.

World Weapon Shuffle remains independent.

## Writer contract

Selected actors are emitted as per-actor SpawnActor override files containing:

```cfg
<ActorSID> : removenode
```

This is the same removal mechanism used by the audited World Ammo/Food/Medical Box controls. The control remains `runtime_test_required` until an in-game build confirms the expected behavior. Existing/explored save state may already contain instantiated actors, so a new game or unexplored area is the correct validation target.

### 2026-10-10 — Loose Unique Weapons source-backed depletion control

The World Loot Depletion board now includes a sixth compact native 0–100% slider, **LOOSE UNIQUE WEAPONS**, in the previously unused sixth 2-column cell. The parameter `loot.looseUniqueWeaponsRemoved` is independent of `loot.looseWorldLootRemoved` and World Weapon Shuffle. It removes a build-time rounded random subset of **nine exact SpawnActor IDs** (not every occurrence of the ItemSID). At 0%, no files are emitted; at 100%, all nine listed actors are removed. Original source: GameData SpawnActorPrototypes, WorldMap_WP, true start, Item spawn, matching ItemSID/SpawnedPrototypeSID. Both base and DLC1 QuestNodePrototypes contain zero literal references to all nine Actor SIDs or Item SIDs. **This is a direct-reference safety check, not proof of complete quest independence.** Excluded from this control: all unlisted weapons, quest-scripted/deferred actors, NPC inventories, trader/generator rewards, DLC1 unique variants, and regular world weapon placements. In-game runtime and local .NET build remain NOT RUN.

The exact allowlist is `src/TrueCustomDifficulty/Data/loose-unique-weapons-manifest.csv`; the authored item list is: `Gun_Tank_MG`, `Gun_S15_AR`, `GunArevPrecise_AR`, `Gun_SOFMOD_AR`, `Gun_GStreet_HG`, `Gun_Texas_SG`, `GunNightStalker_HG`, `Gun_Cavalier_SR`, `Gun_RatKiller_SMG`. The existing gold-only-modified-number and compact rail interaction rules apply. No standalone large-width card or redundant hint is added.
