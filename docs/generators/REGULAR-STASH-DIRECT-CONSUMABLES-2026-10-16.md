# Direct 100%-chance consumables on ordinary stash wrappers (2026-10-16)

## Production outcome

A complete source audit found **22 extra consumable entries** outside the ordinary `GamePass_Stash` core pools and the independently authored SmartLoot layers in regular WorldMap no-clue containers:

- **12 canonical medical source rows** in 8 actors, including two already handled at Richter's Cabin. The **remaining 10 rows in six actors** now follow the existing `Stash Content > MEDICAL` and item-specific Bandage/Medkit/ArmyMedkit/count controls via a narrowly scoped CFG writer.
- **9 booster rows** on 7 actors, consisting only of `Hercules` and `PSYBlocker`. These are not Bandage/Medkit medicine and have **no source-backed dedicated existing stash slider**. Preserve them untouched pending a deliberate control mapping (do not silently classify them as drinks or medicine).
- **One original lowercase `medkit`** in the Promzona Army Warehouses wrapper, `Chance=1`, `MinCount=MaxCount=3`. The exact lowercase SID is **not declared as a root** in supplied `GameData/ItemPrototypes.cfg`, unlike `Medkit`. Keep source case untouched; consider this an unresolved source/wiring issue, not an authorized alias.

`Chance=1` is an **item-level native generator field**. It does **not** prove a 100% guaranteed appearance of the full stash or that category/gen activation always occurs. Zeroing a safe canonical row means its Chance and counts are zeroed (to avoid fallback in all-zero groups).

## Canonical medical rows — 12 source entries, 10 newly supported

| Stash generator root | ItemPrototypeSID | Original count | Category/item | Status |
| --- | --- | ---: | --- | --- |
| `Rostok` | `ArmyMedkit` | 5–5 | `[1]/[0]` | New medical coverage |
| `LesserZone_Cabin_StashGenerator1` | `Medkit` | 1–1 | `[1]/[0]` | Cabin existing |
| `LesserZone_Cabin_StashGenerator3` | `Medkit` | 1–1 | `[1]/[0]` | Cabin existing |
| `Wild_Island_AzimuthStation_StashGenerator1` | `EcoMedkit` | 1–1 | `[1]/[0]` | New medical coverage |
| `Wild_Island_AzimuthStation_StashGenerator1` | `ArmyMedkit` | 3–3 | `[1]/[1]` | New medical coverage |
| `Wild_Island_AzimuthStation_StashGenerator1` | `Medkit` | 3–3 | `[1]/[2]` | New medical coverage |
| `Wild_Island_AzimuthStation_StashGenerator1` | `Bandage` | 5–5 | `[1]/[3]` | New medical coverage |
| `Garbage_Factory_StashGenerator3` | `ArmyMedkit` | 5–5 | `[1]/[0]` | New medical coverage |
| `Promzona_ProminCMDFactory_StashGenerator6` | `EcoMedkit` | 2–2 | `[1]/[0]` | New medical coverage |
| `Garbage_HydroelectricStation_StashGenerator2` | `ArmyMedkit` | 3–3 | `[1]/[0]` | New medical coverage |
| `Promzona_ArmyWarehouses_StashGenerator1` | `EcoMedkit` | 3–3 | `[1]/[0]` | New medical coverage |
| `Promzona_ArmyWarehouses_StashGenerator1` | `ArmyMedkit` | 3–3 | `[1]/[1]` | New medical coverage |

The six newly touched ordinary actors are `12AA126A43D685C7CD7179854E40C5E9`, `717745A84684B392BF29D5B6C157B489`, `89A6611B4CA53AFD52E20CBAC749B162`, `A5C617CC4B4C04CC52DAAA86A6AAFDBF`, `A781417E408DCEDA7F96D48D07C27D52`, and `BFD606894964956318B5519609B326B0`. All are individually cross-checked against the existing 803-actor no-clue SmartLoot manifest before authoring.

The ten rows are extracted verbatim into embedded `src/TrueCustomDifficulty/Data/regular-stash-direct-medical-entries.csv`. The patch uses the exact source root and index in `GameData/ItemGeneratorPrototypes.cfg`, not the global core prototype. **MEDICAL=0x** forces `Chance=0`, `MinCount=0`, and `MaxCount=0` on all ten. **MEDICAL=0.5x** writes `Chance=0.5` and preserves native counts. Individual zero selection suppresses matching Bandage, Medkit or ArmyMedkit; `Medkit MAX=0 ITEMS` and `ArmyMedkit MAX=0 ITEMS` use the original zero-count behavior. `EcoMedkit` is controlled by global MEDICAL only because there is no separately supported EcoMedkit selection slider.

The separately implemented Cabin fix remains exactly two source rows; the full canonical direct-medical coverage is therefore **12 rows across 8 actors**.

## Boosters — nine rows intentionally not modified

| Stash generator root | ItemPrototypeSID | Original count | Category/item |
| --- | --- | ---: | --- |
| `Rostok` | `PSYBlocker` | 1–2 | `[1]/[1]` |
| `Swamp_SecondVanguard_StashGenerator1` | `PSYBlocker` | 1–1 | `[1]/[0]` |
| `Wild_Island_QuietsCamp_StashGenerator2` | `Hercules` | 1–1 | `[1]/[0]` |
| `CementFactory_ConcreteForest_StashGenerator1` | `PSYBlocker` | 1–1 | `[3]/[0]` |
| `CementFactory_ConcreteForest_StashGenerator1` | `Hercules` | 2–2 | `[3]/[1]` |
| `Garbage_Factory_StashGenerator3` | `Hercules` | 2–2 | `[1]/[1]` |
| `Promzona_ProminCMDFactory_StashGenerator6` | `PSYBlocker` | 1–1 | `[1]/[1]` |
| `Promzona_ProminCMDFactory_StashGenerator6` | `Hercules` | 1–1 | `[1]/[2]` |
| `Promzona_ArmyWarehouses_StashGenerator1` | `Hercules` | 2–2 | `[1]/[3]` |

These items have `Chance=1` but are **not automatically suppressed by MEDICAL=0x**. A future booster-specific UI decision could change that policy without touching the 45 reserved gear entries, but adding a new slider or repurposing Drinks/Medical today would be ungrounded.

## Case-sensitive original outside canonical medical

| Stash | ItemSID (exact) | Original chance | Count | Status |
| --- | --- | ---: | ---: | --- |
| `Promzona_ArmyWarehouses_StashGenerator1_BFD606894964956318B5519609B326B0` | `medkit` | 1 | 3–3 | **UNRESOLVED: do not rewrite** |

## Verification and safeguards

- `python tools/audit_regular_stash_direct_additions.py --source-zip "STALKER2-GameData-CFG(20261016-152136).zip"` re-extracts all 80 original items without assumption about physical loot rolls.
- `python tools/audit_regular_stash_consumables.py --source-zip "STALKER2-GameData-CFG(20261016-152136).zip"` verifies 45 gear reservations, the exact 10-row medical embedded source, nine booster sources and lowercase `medkit` ItemPrototype absence.
- `./tests/run-wine.sh LootSpecialGeneratorsContract` runs 7 generated-CFG medical cases (global 0/0.5, Bandage 0, Medkit 0, Medkit MAX 0, ArmyMedkit 0, ArmyMedkit MAX 0). Every case also rejects any patch to the 45 authored gear rows, nine booster rows and lowercase medkit entry. The production writer reuses `TuneStashSelectionField` and `TuneStashMedicalCountField`, with no new UI, preset, category-level deletion or root clone.
- Gameplay requires a fresh, not-yet-generated physical container. Already generated contents in a save will not be retroactively removed. Correct source-level CFG does not establish packed Unreal patch loading.

**Future gear feature:** [45 reserved authored rows, prominent randomization queue](STASH-GEAR-UNIQUE-RANDOMIZATION-BACKLOG.md).
