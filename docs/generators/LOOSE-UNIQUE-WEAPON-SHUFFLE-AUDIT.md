# Loose Unique Weapon Shuffle — source/writer audit (2026-10-10)

## Intention and UI

**SHUFFLE UNIQUE WEAPONS** is an independent OFF/ON toggle in the compact **World Placement** panel. It **does not remove weapons**. OFF is vanilla; ON places the nine audited weapon variants at other locations among their nine original loose pickup locations. Ordinary weapon shuffle and density remain separate, as do the five World Loot Depletion controls. Parameter: `loot.looseUniqueWeaponShuffle`.

## Eligible source surface

Proven by original `STALKER2-GameData-CFG(5).zip` and `STALKER2-DLC1-CFG(5).zip` CFG archives. All nine original source actors are:
- exactly in base `GameData/SpawnActorPrototypes.cfg`, `WorldMap_WP`;
- `SpawnType = ESpawnType::Item`, `SpawnOnStart = true`;
- one distinct actor SID per named weapon, with matching `SpawnedPrototypeSID` and `ItemSID`;
- absent by exact literal actor/item ID from both base and DLC1 `QuestNodePrototypes.cfg`;
- disjoint from the 362 regular direct world-weapon shuffle actors.

| Unique ItemSID | Original ActorSID |
| --- | --- |
| `Gun_Tank_MG` | `09EEF8794F23BF1D373B6CA1CCC6F8DF` |
| `Gun_S15_AR` | `13DF06544F7D4401F5B042A01A6BD1E4` |
| `GunArevPrecise_AR` | `61AF1E49404F5E1620F6EE86F5C5F644` |
| `Gun_SOFMOD_AR` | `665FA23B4FC7DE9B6C61BF9497B1C9A5` |
| `Gun_GStreet_HG` | `7ADA44EA4C856FDA87DE298DBF5C7656` |
| `Gun_Texas_SG` | `90A7F3344C766CF84BE73F83A98F7026` |
| `GunNightStalker_HG` | `BEFFC269430B8A7C7CFE8EB2BDC5574A` |
| `Gun_Cavalier_SR` | `CB061545481621331D8258B730D562E1` |
| `Gun_RatKiller_SMG` | `CBE52EF14A9E37B1699E58A51F766F76` |

**Quest exclusion rule:** These are *only* the nine candidates passing the direct QuestNode reference check. Named unique weapons outside this exact allowlist, scripted/deferred spawns and quest/reward/NPC/trader generators are not edited. QuestNode literal references are necessary evidence, **not sufficient to prove no gameplay script depends on those pickups**.

## Exact writer contract

Manifest: `src/TrueCustomDifficulty/Data/loose-unique-weapon-shuffle-manifest.csv`. Alongside source ActorSID/ItemSID, it stores exact original `MeshPath`, `Durability`, `DisablePhysicsAndCollision` so shuffled weapons carry their source presentation/condition rather than the recipient location's.

Writer: `CfgPatchWriterService.WriteLooseUniqueWeaponShuffle`.

- OFF: no modifications.
- ON: one cryptographically chosen **Sattolo 9-cycle**; all nine positions get a different weapon than originally placed; no weapon can disappear or duplicate.
- Each destination retains its actor ID, world position and rotation. Five source fields are patched under `<ActorSID> : struct.begin {bpatch}`: `SpawnedPrototypeSID`, `ItemSID`, `MeshPath`, `Durability`, `DisablePhysicsAndCollision`.
- Exactly **9 per-actor files, 45 assignments, zero removals**.
- Regular World Weapon Shuffle never targets any of these nine locations.

## Gates and local validation

Source ZIP gate:

```bash
python3 tools/audit_loose_unique_weapon_shuffle_source.py \
  --base '/path/to/STALKER2-GameData-CFG(5).zip' \
  --dlc '/path/to/STALKER2-DLC1-CFG(5).zip'
```

Actual production writer test (Windows/.NET):

```powershell
dotnet run --project tests/LootWorldPlacementContract/LootWorldPlacementContract.csproj -c Release
```

The regression suite tests OFF across all four vanilla difficulties and ON in both base/DLC environments, alongside regular weapons and with attachments/detectors/blueprints. It asserts nine exact model and presentation tuples, no same-position items, no `removenode` output, and no unrelated edit points. It now specifies **24 scenarios** total.

**Still NOT RUN here:** successful local .NET/WPF compilation, verified Unreal BPatch application, loose pickup appearance/collision after remapping, save-state effects, and hidden quest/script dependencies. Game acceptance should use a new game or previously unexplored area.
