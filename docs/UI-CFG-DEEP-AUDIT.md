# UI → Real CFG Deep Audit

> **HISTORICAL SNAPSHOT ONLY — v0.9.26.51.** The counts below (199 catalog / 188 editable) and its PASS apply **only to that earlier snapshot**. They do NOT describe the current source tree. The active catalog is 236 parameters / 223 editable dimensions, and this earlier audit missed six subsequently introduced mutant MaxHP writer routes. See [Deep CFG audit phase 1 (2026-10-09)](DEEP-CFG-AUDIT-2026-10-09.md) for the source-verified correction and current counts. Retained as an archived comparison, not as the current release approval.


**Project:** True Custom Difficulty  
**Audit target:** v0.9.26.51  
**Scope:** every player-editable UI dimension, including every backing axis hidden behind aggregate radar cards.
## Result
**PASS — no new UI/CFG mapping defect was found.** The current UI exposes **128 visible cards**, but those cards represent **188 independent player-editable parameter dimensions** because 10 aggregate radar/cards contain 70 backing parameters. All **188/188** editable dimensions resolve to an active writer route.
The catalog contains **199** parameters in total. The remaining **11** are intentionally hidden implementation/audit controls: 10 internal stamina helpers plus `economy.repairDifficultyCost`. They are not silently presented through the normal UI.
Across player-editable dimensions, **147** use the normal direct writer path and **41** use explicit special writers. The direct UI surface expands to **1,342 concrete CFG edit points**. Special writers are additionally protected by their production manifests/structural contracts. No direct concrete target is owned by two different parameters.
Support status is also explicit: **142** editable dimensions are `confirmed`; **46** remain `runtime_test_required`. `runtime_test_required` does **not** mean that the UI is currently unrouted—the writer mapping exists and is audited—but it still lacks the same level of in-game runtime confirmation as `confirmed` controls.
## Per-tab contract
| Tab | Editable dimensions | Confirmed | Runtime test required | Direct routes | Special routes | Direct CFG edit points |
|---|---:|---:|---:|---:|---:|---:|
| Combat | 33 | 21 | 12 | 21 | 12 | 62 |
| Health | 16 | 13 | 3 | 16 | 0 | 38 |
| Stamina | 13 | 12 | 1 | 13 | 0 | 15 |
| Survival | 23 | 23 | 0 | 23 | 0 | 70 |
| Economy | 22 | 17 | 5 | 21 | 1 | 405 |
| Equipment | 4 | 3 | 1 | 4 | 0 | 113 |
| Environment | 17 | 17 | 0 | 17 | 0 | 42 |
| Loot | 40 | 16 | 24 | 17 | 23 | 562 |
| Master | 20 | 20 | 0 | 15 | 5 | 35 |

## Aggregate UI controls
| UI card / radar | Backing dimensions | Route split | Real CFG surface | Direct points |
|---|---:|---|---|---:|
| Mutant Health | 8 | 8 direct / 0 special | `GameData/ObjPrototypes.cfg` | 10 |
| Healing Amount | 4 | 4 direct / 0 special | `GameData/EffectPrototypes.cfg` | 8 |
| Action Costs | 7 | 7 direct / 0 special | `GameData/CoreVariables.cfg, GameData/ObjPrototypes.cfg` | 7 |
| Item Price Scalers | 6 | 6 direct / 0 special | `DLCGameData/DLC1/ItemPrototypes.cfg, GameData/DifficultyPrototypes.cfg, GameData/ItemPrototypes.cfg` | 35 |
| Individual Anomaly Damage | 11 | 11 direct / 0 special | `GameData/EffectPrototypes.cfg` | 24 |
| World Loot Removal | 5 | 0 direct / 5 special | `GameData/SpawnActorPrototypes.cfg` | 0 |
| Breakable Box Ammo Distribution | 4 | 0 direct / 4 special | `GameData/ItemGeneratorPrototypes.cfg` | 0 |
| Stash Ammo Amounts | 3 | 0 direct / 3 special | `GameData/ItemGeneratorPrototypes.cfg` | 0 |
| Mutant Part Drop Chances | 14 | 14 direct / 0 special | `GameData/ItemGeneratorPrototypes.cfg` | 14 |

### Mutant Health
All eight axes are direct `scale` operations against `GameData/ObjPrototypes.cfg -> MaxHP`. There are **10 concrete MaxHP edits** because Bloodsucker also controls `MistBloodsucker`, while Burer also controls `Burer_ShootingSpecial`. Bloodsucker and MistBloodsucker receive the same family multiplier; the radar deliberately displays only the standard Bloodsucker reference HP (500 at 1x) instead of exposing the internal 500/1000 pair. Other displayed references remain Chimera 1400, Deer 600, Pseudogiant 2500, Pseudodog 300, Burer 400, Bayun 400, Controller 500.

### Healing Amount
The four axes directly write `GameData/EffectPrototypes.cfg` healing `ValueMin/ValueMax` pairs: **8 concrete edits**. The HP shown by the radar is therefore the actual value written to those healing effects, not a cosmetic normalized number.


### Food Restores Health
This confirmed direct toggle controls only the four ordinary food healing effects in `GameData/EffectPrototypes.cfg`: `BreadHealing1` (8 HP), `FreshBreadHealing2` (15 HP), `CannedHealing2` (18 HP), and `SausageHealing2` (12 HP). OFF writes zero to each effect's `ValueMin` and `ValueMax`; ON leaves vanilla values in force. Hunger/satiety effects are separate, and all `Artifact_WeirdKettle_*Healing` alternative effects are intentionally excluded.

### Action Costs
All seven axes are direct routes. Jump/vault/melee/buttstock write the player's stamina action values in `GameData/ObjPrototypes.cfg`; Sprint and Aiming use the audited stamina variables in `GameData/CoreVariables.cfg`. Aiming retains the positive player-facing penalty with the existing signed `negateSet` transformation.

### Item Price Scalers
The six radar axes produce **35 direct edits**. Consumables, ammo, armor, weapons and artifacts write difficulty economy/item cost multipliers; Mutant Parts scales 15 concrete `Cost` values (14 base + 1 DLC). The radar's 0.25x interaction step is UI-only quantization over the same existing multiplier writer semantics.

### Individual Anomaly Damage
All 11 axes are direct routes into `GameData/EffectPrototypes.cfg` (**24 concrete edits**). The separate Global Anomaly Damage control remains a second layer; the radar's Effective Damage display is computed from the same individual raw value plus that global multiplier.

### World Loot Removal
All five axes are manifest-driven special writers against audited world placements. Ordinary Ammo/RGD and dedicated F1 boxes are disjoint. Current audited populations are Ammo/RGD **1,315**, F1 **101**, Food & Drink **1,208**, Medical **868**, and Loose Loot **7,233**. The Base / With DLC counts shown in the radar come from those production surfaces.

### Breakable Box Ammo Distribution
The four axes are a special `ItemGeneratorPrototypes.cfg` writer. They alter only the Weight fields of the six already-supported regular-ammo entries and preserve `PossibleItems` membership. The production redirect surface remains **1,235** physical ammo boxes.

### Stash Ammo Amounts
Basic, Standard and Rare use three separate special generator writers in `ItemGeneratorPrototypes.cfg`, with production redirect manifests of **1,271**, **450**, and **305** rows respectively. Their live ammo-stack preview is derived from the same source MinCount/MaxCount entries used by those writers.

### Mutant Part Drop Chances
All 14 axes are direct `ItemGeneratorPrototypes.cfg` routes, one concrete generator chance edit per mutant family. The radar changes the existing per-mutant drop chance only; it does not change spawn counts or successful-drop quantity.

## Runtime-test-required dimensions
- **World Loot Removal**: 5
- **Ammo Box Distribution**: 4
- **Stash Ammo Amounts**: 3
- **Psy Phantoms Cause Bleeding**: 1
- **Psy Equipment Durability Damage**: 1
- **Psy Phantom Count**: 1
- **Psy Phantom Spawn-Cycle Delay**: 1
- **Medic Stock Amount**: 1
- **Trader Ammo Stock Amount**: 1
- **Trader Buy-From-You Pricing**: 1
- **Trader Sell-To-You Pricing**: 1
- **Trader Minimum Buy Condition**: 1
- **Armor Durability**: 1
- **Passive Health Recovery**: 1
- **Player Max Health**: 1
- **Health Recovery Delay**: 1
- **Ammo from Breakable Boxes**: 1
- **Grenades from Breakable Boxes**: 1
- **NPC Medical Loot Chance**: 1
- **NPC Weapon Condition**: 1
- **NPC Dropped Ammo Amount**: 1
- **Basic Stash RGD-5 Grenade Chance**: 1
- **Basic Stash RGD-5 Grenade Amount**: 1
- **Blueprint Shuffle**: 1
- **World Attachment Shuffle**: 1
- **Remove World Detectors**: 1
- **World Weapon Density**: 1
- **World Weapon Shuffle**: 1
- **Maximum Vault Height**: 1

## Machine-readable evidence
- `docs/ui-cfg-deep-audit.csv` contains one row for each of the **187 effective UI dimensions**, including aggregate radar backings, route type, CFG files, operation, concrete edit count, target samples, special writer group and manifest evidence.
- `tools/audit_ui_cfg_contract.py` locks the 127-card / 187-dimension UI contract, the 11 intentionally hidden settings, complete route coverage, aggregate membership, concrete-target collision protection, explicit support state, and presence of all special-writer resources.
- Existing `audit_project_consistency.py` and `audit_production_integration.py` remain the canonical broad project gates and are run together with this audit.
