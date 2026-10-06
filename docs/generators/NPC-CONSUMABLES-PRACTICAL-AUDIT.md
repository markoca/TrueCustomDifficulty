# Standard Human NPC Consumables — Practical Control Audit

**Source baseline:** STALKER 2 runtime/game + DLC CFG dump 2.0.5  
**Project release:** v0.2.25  
**Scope:** repeatable/standard-human use of the six active `GeneralNPC_Consumables_*` pools  
**Status:** control-surface decision layer; no runtime writer change in this release

## Executive verdict

The existing deep audit proves that the six active consumable pools are shared by standard humans, named/scripted NPCs, zombies and other contexts, so the vanilla pools must **never** be globally patched for a standard-human-only control. However, the repeatable production source links are individually known. A future control can therefore be scope-isolated by cloning the six pools into TCD-owned roots and redirecting only the 33 repeatable links.

The important design result is that **quantity is not a useful generic lever** while **medical Chance is**:

- 104 active item rows exist across the six pools.
- 84/104 are fixed `Count=1-1`; only 20 rows have any count range at all.
- all 104 rows expose an explicit item-level `Chance`.
- Medical is the only category whose vanilla Chance materially changes with difficulty; food, Energy Drink and alcohol retain the same source chances on Master.

The only new control worth promoting is **Human NPC Medical Loot Frequency**. It is implemented in v0.2.14 through six TCD-owned pool clones and the exact 33-link repeatable standard-human redirect boundary documented below.

**v0.2.25 scope correction:** the older 37-link manifest included four named DLC `GeneralNPC_MIK_*` / `GeneralNPC_MykluhaFreedom_*` paths. Those are not repeatable generic humans and are now hard-excluded, leaving **33** runtime redirects. The six pool clones and all 58 medical Chance transforms are otherwise unchanged.

## Practical pool usage

`Propagated standard-human paths` is a static graph-breadth metric, not gameplay telemetry; it shows how broadly the pool sits under standard-human generator paths. Counts may overlap between pools and must not be summed as unique NPCs.

| Pool | Active standard source links | Direct standard consumers | Propagated standard-human paths | Possible items |
| --- | ---: | ---: | ---: | --- |
| `CloseCombat` | 7 | 8 | 27 | Bandage, Beer, Medkit, Sausage |
| `Heavy` | 4 | 4 | 15 | AntiRad, ArmyMedkit, Bandage, CannedFood, Medkit, Sausage |
| `Noon` | 4 | 4 | 12 | AntiRad, ArmyMedkit, Bandage, CannedFood, Energetic, Medkit, Sausage |
| `Recon` | 7 | 7 | 30 | AntiRad, Bandage, Bread, CannedFood, Energetic, Medkit |
| `Sniper` | 8 | 9 | 24 | ArmyMedkit, Bandage, Bread, CannedFood, Energetic |
| `Stormtrooper` | 7 | 8 | 28 | AntiRad, Bandage, Bread, Medkit, Vodka |

Static breadth ranks the shared pools roughly as **Recon (30) > Stormtrooper (28) > CloseCombat (27) > Sniper (24) > Heavy (15) > Noon (12)** for standard-human propagated paths. This is not a claim about actual kill frequency in a playthrough.

## What the standard pools can drop

Across the six active pools the possible item set is: **Bandage, Medkit, Army Medkit, Anti-Rad, Bread, Canned Food, Sausage, Energy Drink, Beer and Vodka**. Different combat roles use different subsets, and the source keeps separate branches for `(Novice/Stalker/Veteran vs Master) × (Newbie/Experienced vs Veteran/Master player rank)`.

| Role pool | Item set |
| --- | --- |
| `CloseCombat` | Bandage, Beer, Medkit, Sausage |
| `Heavy` | AntiRad, ArmyMedkit, Bandage, CannedFood, Medkit, Sausage |
| `Noon` | AntiRad, ArmyMedkit, Bandage, CannedFood, Energetic, Medkit, Sausage |
| `Recon` | AntiRad, Bandage, Bread, CannedFood, Energetic, Medkit |
| `Sniper` | ArmyMedkit, Bandage, Bread, CannedFood, Energetic |
| `Stormtrooper` | AntiRad, Bandage, Bread, Medkit, Vodka |

## Why Medical is the only strong difficulty candidate

Average item-row `Chance` values below are descriptive source summaries, not an engine probability formula. They make the vanilla design intent visible:

| Category | Rows | Fixed 1-1 counts | Chance=100% rows | Lower-rank Novice/Stalker/Veteran | Lower-rank Master | Higher-rank Novice/Stalker/Veteran | Higher-rank Master |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Medical | 58 | 42 | 7 | 20.4% | 9.0% | 58.6% | 29.3% |
| Food | 26 | 26 | 6 | 63.3% | 63.3% | 74.3% | 74.3% |
| Utility / stimulant | 12 | 10 | 4 | 18.3% | 18.3% | 83.3% | 83.3% |
| Utility / alcohol | 8 | 6 | 2 | 25.0% | 25.0% | 75.0% | 75.0% |

Medical drops are deliberately suppressed on Master: average medical row Chance falls from **20.4% to 9.0%** in lower-rank branches and from **58.6% to 29.3%** in higher-rank branches. Food, stimulant and alcohol chances do not show that difficulty split in the source.

## Quantity is not a coherent generic control

Across the 104 active rows the count patterns are:

- `0-1`: **4 rows**
- `0-2`: **2 rows**
- `1-1`: **84 rows**
- `1-2`: **13 rows**
- `1-3`: **1 rows**

Because **84 rows are already `1-1`**, a percentage-based NPC Consumable Quantity control would either do nothing over large slider ranges or start inventing extra items on rows that vanilla treats as a single-item drop. That is not a clean system-level control.

## Control-surface decisions

| Control | Decision | Rationale |
| --- | --- | --- |
| **Human NPC Medical Loot Frequency** | **IMPLEMENTED / RUNTIME TEST REQUIRED** | Only consumable category with a strong vanilla difficulty signal. All 58 medical item `Chance` rows are scaled relative to their own vanilla chance; counts remain untouched. |
| **Standard NPC Consumable Frequency** | NO UI | Mixes difficulty-sensitive medical loot with difficulty-neutral food/utility loot and creates a broad economy control with unclear player intent. |
| **Standard NPC Consumable Quantity** | NO UI | 84/104 rows are fixed 1-1; a quantity slider would affect only a minority of high-rank rows and would provide inconsistent feedback. |
| **Standard NPC Medical Quantity** | NO UI / DEFER | 42/58 medical rows are fixed 1-1. Quantity is not a uniform system-level lever. |
| **Standard NPC Food Loot Frequency** | NO UI | All 26 food rows are fixed quantity 1 and the source chances are difficulty-neutral between Master and the other vanilla difficulty family. |
| **Standard NPC Utility Loot Frequency** | NO UI | Technically patchable but narrow economy/flavor tuning, not a core difficulty control. |

## Implemented safe boundary for Medical Frequency

The implemented **Human NPC Medical Loot Frequency** writer contract is:

1. Materialize six TCD-owned clones: `TCD_GeneralNPC_Consumables_CloseCombat`, `...Heavy`, `...Noon`, `...Recon`, `...Sniper`, and `...Stormtrooper`.
2. Preserve all four vanilla difficulty × player-rank branches and copy every non-medical row byte-for-byte/field-for-field.
3. Modify only the **58 Medical item `Chance` fields** (`Bandage`, `Medkit`, `ArmyMedkit`, `AntiRad`); never touch their count ranges.
4. Redirect only the **33 active repeatable standard-human `SubItemGenerator` links** listed in `npc-consumable-standard-redirect-manifest.csv`.
5. Leave the two Prologue links with `Chance=0` untouched, consistent with the project-wide Prologue exclusion policy.
6. Leave named/scripted NPCs, zombies, corpse overrides, shared parents and trader consumers on the vanilla pools.
7. Scale each medical row relative to its own vanilla Chance using **0–200% in 5% steps**. `scaled = clamp(vanillaChance × frequency, 0, 1)`. The **7/58** rows already at `Chance=1.0` stay capped at 100% above vanilla.

## Files

- `npc-consumable-practical-pools.csv` — six active pools with standard-human breadth and future TCD clone SID.
- `npc-consumable-practical-category-summary.csv` — category-level row/count/chance/difficulty summary.
- `npc-consumable-standard-redirect-manifest.csv` — exact 33-row repeatable standard-human-only redirect boundary.
- `npc-consumable-control-surfaces-v0.2.14.csv` — keep/defer/no-UI decision table.

v0.2.14 introduced this control; v0.2.25 narrows its runtime redirect boundary from 37 to 33 by excluding the four named DLC `GeneralNPC_MIK_*` / Mykluha paths, matching the later project-wide hard-exclusion policy. See `npc-medical-frequency-scaling.csv` for the audited row-by-row scaling examples.
