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

### 2026-10-10 — Unique Weapon Shuffle CFG writer baseline / audit reconciliation

The dedicated `loot.looseUniqueWeaponShuffle` control was already implemented in the World Placement UI and in `CfgPatchWriterService.WriteLooseUniqueWeaponShuffle`, but the central `cfg-writer-baseline.json` still declared **244** supported parameters and lacked its manifest-backed production route. This made the actual C# `ValidateBaseline` fail before export, and the Python audits report the parameter as unsupported. **Do not remove, defer or fake a single-field CFG edit for this control.**

**Correct current inventory:** **245** catalog entries, **152 visible aggregate anchors/standalone controls**, **93 hidden backing IDs**, **232 effective editable source dimensions**, **13 intentionally non-UI source IDs**, **80 hidden-but-editable backings**. The source-backed direct/special writer route union covers **245/245**, including the new nine-actor shuffle.

**Canonical production route:** `cfg-writer-baseline.json.special.looseUniqueWeaponShuffle` contains the real `parameterId`, embedded `manifestResource` and `expectedActorCount=9`. `CfgWriterBaseline` deserializes this exact route and `CfgPatchWriterService` validates it, includes it in supported count and uses its embedded manifest in the actual writer. The writer remains a real per-actor `{bpatch}` derangement of nine physical world pickups (five item/presentation fields per actor, no `removenode`), separate from regular World Weapon Shuffle.

**Regression gates:** `tools/audit_ui_cfg_contract.py` and `tools/audit_project_consistency.py` now agree on the new totals and discover the new special route. The project audit additionally verifies route identity, manifest resource, exactly nine distinct actor/item pairs and linkage to the real C# writer. `tests/LootWorldPlacementContract` checks that the loaded production baseline contains the precise route and matches the live catalog count; its existing **24 writer scenarios** already cover unique-only, DLC, mixed weapon and other placements. Run the two Python audits, then the Windows `.NET` writer tests and Wine application. Source/structure verification alone does not prove local publish or UE runtime acceptance.

