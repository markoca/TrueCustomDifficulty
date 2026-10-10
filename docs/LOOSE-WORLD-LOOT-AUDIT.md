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

### 2026-10-10 — Source-backed loose unique weapon shuffle (replaces removal control)

**Final approved behavior:** `SHUFFLE UNIQUE WEAPONS` is a **boolean toggle in WORLD PLACEMENT**, not a depletion slider. Parameter `loot.looseUniqueWeaponShuffle` (default OFF) is separate from ordinary `loot.worldWeaponShuffle`, weapon density, and `loot.looseWorldLootRemoved`. The World Loot Depletion panel remains the original **five** sliders; no unique-weapon percentage, removal path, or obsolete preset parameter is active.

**Compact UI contract:** World Placement aggregate has **six** settings (five independent toggles and one dependent weapon-density slider). Height 275 DIP. The left compact column contains regular weapon shuffle at local top=65/H44, its dependent density card at 122/H72, and unique weapon shuffle at 208/H44. Three other toggle pills remain on the right, 46 DIP high, 57 DIP apart. Reuse the established compact checkbox/toggle chrome, hover highlight, independent right-click reference reset and family R reset. No redundant Vanilla/Base labels or helper paragraphs inside the control.

**Original CFG evidence:** GameData `SpawnActorPrototypes.cfg`, exact `WorldMap_WP` actors with `SpawnOnStart=true`, `SpawnType=Item`, matching `ItemSID`/`SpawnedPrototypeSID`. Nine included ItemSIDs: `Gun_Tank_MG`, `Gun_S15_AR`, `GunArevPrecise_AR`, `Gun_SOFMOD_AR`, `Gun_GStreet_HG`, `Gun_Texas_SG`, `GunNightStalker_HG`, `Gun_Cavalier_SR`, `Gun_RatKiller_SMG`. Exact actor IDs and original presentation tuple are in `src/TrueCustomDifficulty/Data/loose-unique-weapon-shuffle-manifest.csv`, kept separate from regular weapon pools.

**Writer contract:** OFF emits no patch. ON uses a cryptographically seeded Sattolo cycle over the nine original weapon tuples, meaning **every item changes location and each original weapon exists exactly once**. Each original location keeps its actor SID, coordinates and rotation; only the `SpawnedPrototypeSID`, `ItemSID`, `MeshPath`, `Durability`, and `DisablePhysicsAndCollision` tuple is redirected together by exact per-actor `{bpatch}`. Nine output actor files, 45 exact assignment fields, **zero** `removenode` operations. Neither the ordinary weapon shuffle nor its density can edit any of these actor IDs. Quest/deferred pickup locations, trader rewards, NPC inventories and global item definitions are never targeted.

**Quest limits:** The nine actor/item SIDs were not directly referenced in the base or DLC1 `QuestNodePrototypes.cfg` searches; this does not certify every possible script/reward linkage. Do not expand the actor pool without separate source/quest audit. Production writer regression now asserts permutation conservation, no fixed points, original mesh/condition tuple fidelity and coexistence with other World Placement edits. Local Windows build, BPatch resolution and actual in-game placement/pickup remain **NOT RUN** until executed.

