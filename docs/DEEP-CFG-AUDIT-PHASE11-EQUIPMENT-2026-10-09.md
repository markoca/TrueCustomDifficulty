# Deep CFG / BPatch Audit — Phase 11: Equipment (2026-10-09)

## Current scope, not historical eight-control audit

The live Equipment catalog and actual MainViewModel contain exactly four parameters:
`equipment.armorDurability`, `equipment.weaponWearPerShot`,
`equipment.ammoWeight` and `equipment.consumableWeight`.
The historical four equipment bonus-effect cap fields are **audit-only**:
do not restore their sliders or confuse the old 117-point inventory with this phase.

| Parameter | Direct writer points | Vanilla behavior / ownership |
|---|---:|---|
| Armor Durability | 4 base | `EnvironmentDifficulty.Armor_Durability`, one selected difficulty SID, `set`; runtime effect formula unproven |
| Player Weapon Wear Per Shot | 41 base + 11 DLC1 | `DurabilityDamagePerShot` `scaleNonZero`; 40 nonzero base and 11 nonzero DLC1, with `UA_GLaunch_Player = 0.0` preserved |
| Ammo Weight | 34 base | Concrete `Weight` `scale` writes, including effective inherited source weights |
| Consumable Weight | 22 base + 1 DLC1 | Concrete `Weight` `scale` writes; DLC1 `MonolithDryRation` gated by installed-DLC flag |
| **Total** | **101 base + 12 DLC1 = 113** | Only direct manifest points; no special Equipment writer |

## Executable real writer contract (C5)

`tests/EquipmentPatchContract` constructs the production
`MainViewModel` and calls the production `CfgPatchWriterService.Write`.
It verifies each actual generated file path, exact prototype/field identities,
all nested {bpatch} wrappers, transformed numeric values and absence of
collateral edits; it does **not** simulate an independent writer.

The 32 planned/executable scenarios cover:

- 4 vanilla no-change cases across Novice, Stalker, Veteran, Master
- 16 independent single-control cases (all 4 controls × all 4 difficulties)
- 6 boundary cases: 0x and 2x for weapon wear, ammo weight and consumable weight
- 4 DLC1-flagged scenarios: 0.5x/2.0x for wear and consumable weight
- 2 combined controls: armor+weapon wear and ammo+consumables in their shared output file

The suite asserts **one** original zero-valued weapon wear entry, verifies
no patch emitted for that zero, and checks that absent DLC1 never leaks DLC1
patches. For DLC1-flagged cases, the installed flag is simulated with a
`GameInstallInfo`; this does not validate a physical game installation.

Run on Windows / .NET 10, or the CI `equipment-writer-windows` job at the
same exact SHA:

```powershell
dotnet run --project tests/EquipmentPatchContract/EquipmentPatchContract.csproj -c Release -- --out audit/equipment-patch-fixtures
python tools/pre_release.py writer
```

**Verification status at authoring:** registered, Windows execution pending
a green same-SHA CI run. Do not mark PASS from registration alone.

## Independent original decoded source gate (B8)

The existing source comparator checks direct authored values and explicitly
traces an inherited `{refkey=...}` chain when a concrete ammo/consumable
weight is absent on the child prototype. Source inheritance is not proof
of Unreal BPatch behavior.

```bash
python tools/audit_cfg_source_values.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2).zip" \
  --parameter-prefix equipment. --out audit/equipment-source-values.json --strict
```

The `tools/pre_release.py source` runner also executes B8. **Original
archives NOT RUN here** (the named original ZIPs from the earlier conversation
are not mounted in this execution environment). Required acceptance is
113 source identities/raw values, including physically authored and explicitly
verified inherited values, not merely agreement with the writer manifest.

## Manual / in-game acceptance (still NOT RUN)

- Confirm each BPatch loads in game under the intended difficulty baseline;
  switching difficulties must not accidentally apply another difficulty's armor field.
- Verify the armor scalar changes condition wear direction without asserting
  any unproven linear-per-hit formula.
- Measure the change in condition loss per shot on representative base and
  DLC1 weapons; the explicit zero-valued launcher must remain zero.
- Verify that base/DLC1 ammo and consumable weights affect inventory mass
  while preserving relative item differences.
- Confirm that Weapon Jamming stays separate and that adjusting Equipment
  does not change armor protection, weapon damage, consumable effects or UI design.

The UI lock documents remain binding and are unchanged by this audit.
No gameplay balancing or version bump is included.
