# Mutant Loot — Deep Audit

This audit validates the complete mapped mutant ItemGenerator domain and, in particular, the existing standard mutant-part controls. It does not add new runtime controls or change writer behavior.

## Evidence boundary

- Exact patch coordinates come from the authoritative raw/local `generator-local-source-*` tables.
- Consumers come from `ObjPrototypes.cfg` inventory wiring in `generator-consumer-contexts.csv`.
- Composition and inheritance are checked separately; a clean species part generator must not silently fan out through either graph.
- `Chance` and `Weight` are not treated as interchangeable mechanics.
- Human-facing documentation uses **Bayun** while preserving the raw technical CFG identifiers `CatLootGenerator` / `CatLoot`.

## Result

| Metric | Count |
| --- | ---: |
| Mapped mutant-loot generator roots | 19 |
| Exact local source rows in those roots | 28 |
| Direct mutant consumer edges | 48 |
| Existing standard mutant-part sliders | 14 |
| Existing sliders passing strict 1:1 audit | 14 |
| Other special/excluded mutant generators | 5 |

All 14 existing standard mutant-part controls pass the strict source audit: each control writes one locally authored `Chance` field on one single-item generator row, the row is fixed to `MinCount=1 / MaxCount=1`, the writer baseline value matches the raw CFG value, and the generator has no composition or inheritance edges.

## Existing standard mutant-part controls

| UI control | Technical generator | Item | Vanilla Chance | Direct consumers | Verdict |
| --- | --- | --- | ---: | ---: | --- |
| Blind Dog Part Drop Chance | `BlinddogLootGenerator` | `BlinddogLoot` | 15% | 2 | **1:1 confirmed** |
| Bloodsucker Part Drop Chance | `BloodsuckerLootGenerator` | `BloodsuckerLoot` | 50% | 4 | **1:1 confirmed** |
| Boar Part Drop Chance | `BoarLootGenerator` | `BoarLoot` | 20% | 3 | **1:1 confirmed** |
| Burer Part Drop Chance | `BurerLootGenerator` | `BurerLoot` | 100% | 2 | **1:1 confirmed** |
| Bayun Part Drop Chance | `CatLootGenerator` | `CatLoot` | 100% | 2 | **1:1 confirmed** |
| Chimera Part Drop Chance | `ChimeraLootGenerator` | `ChimeraLoot` | 100% | 2 | **1:1 confirmed** |
| Controller Part Drop Chance | `ControllerLootGenerator` | `ControllerLoot` | 100% | 2 | **1:1 confirmed** |
| Deer Part Drop Chance | `DeerLootGenerator` | `DeerLoot` | 100% | 1 | **1:1 confirmed** |
| Flesh Part Drop Chance | `FleshLootGenerator` | `FleshLoot` | 20% | 2 | **1:1 confirmed** |
| Poltergeist Part Drop Chance | `PoltergeistLootGenerator` | `PoltergeistLoot` | 65% | 8 | **1:1 confirmed** |
| Pseudodog Part Drop Chance | `PseudodogLootGenerator` | `PseudodogLoot` | 100% | 2 | **1:1 confirmed** |
| Pseudogiant Part Drop Chance | `PseudogiantLootGenerator` | `PseudogiantLoot` | 100% | 2 | **1:1 confirmed** |
| Tushkan Part Drop Chance | `TushkanLootGenerator` | `TushkanLoot` | 10% | 1 | **1:1 confirmed** |
| Snork Part Drop Chance | `SnorkLootGenerator` | `SnorkLoot` | 20% | 2 | **1:1 confirmed** |

`Bayun Part Drop Chance` intentionally keeps the internal parameter ID `loot.mutant.catChance` and raw CFG identifiers for compatibility, but the player-facing mutant name is Bayun.

## Species / variant blast radius

A 1:1 writer mapping does not mean “only the base prototype consumes it”. The exact species generator is reused by the following mutant prototypes, so the control is correctly interpreted as the species part-drop chance for every eligible consumer of that generator:

| Generator | Player-facing species | Consumers |
| --- | --- | --- |
| `BlinddogLootGenerator` | Blind Dog | Blinddog<br>MoldyBlinddog |
| `BloodsuckerLootGenerator` | Bloodsucker | AshyBloodsucker<br>Bloodsucker<br>MistBloodsucker<br>PrologueBloodsucker |
| `BoarLootGenerator` | Boar | Boar<br>HelmedBoar<br>PorcupineBoar |
| `BurerLootGenerator` | Burer | Burer<br>Burer_ShootingSpecial |
| `CatLootGenerator` | Bayun | Bayun<br>Bayun_E10_MQ01 |
| `ChimeraLootGenerator` | Chimera | Chimera<br>JadedChimeraPhantom |
| `ControllerLootGenerator` | Controller | Controller<br>SpatialBubbleController |
| `DeerLootGenerator` | Deer | Deer |
| `FleshLootGenerator` | Flesh | BoggyFlesh<br>Flesh |
| `PoltergeistLootGenerator` | Poltergeist | Poltergeist<br>Poltergeist_Electro<br>Poltergeist_Toxic<br>Poltergeist_Fire<br>Poltergeist_YanivToxicRozliv<br>Poltergeist_Explosives<br>DLC01_MQ02_Poltergeist<br>DLC01_Pseudobrain_Poltergeist |
| `PseudodogLootGenerator` | Pseudodog | PseudoDogBase<br>PseudoDog |
| `PseudogiantLootGenerator` | Pseudogiant | Pseudogiant<br>Pseudogiant_NoCooldowns |
| `SnorkLootGenerator` | Snork | DLCTestAgent<br>Snork |
| `TushkanLootGenerator` | Tushkan | Tushkan |

Important examples: `BloodsuckerLootGenerator` also feeds Ashy, Mist and Prologue Bloodsuckers; `CatLootGenerator` feeds Bayun and `Bayun_E10_MQ01`; `ChimeraLootGenerator` also feeds `JadedChimeraPhantom`; and `PoltergeistLootGenerator` is shared by several elemental/DLC Poltergeist variants. This is intended species-level blast radius, not an accidental shared-pool dependency.

## Non-standard mutant generators

| Generator | Exact local structure | Consumers | Decision |
| --- | --- | --- | --- |
| `DefaultMutantLootGenerator` | `Sausage` (Weight 2) | — | EXCLUDE — no resolved runtime consumer; Weight-based Sausage fallback/template |
| `MutantElectrocollarGenerator` | `Electrocollar` (Weight 1) | Blinddog_Collar<br>Bloodsucker_Collar<br>Bayun_Collar<br>Controller_Collar<br>Controller_Deer<br>Pseudogiant_Collar<br>Snork_Collar | SEPARATE DOMAIN — direct Electrocollar Weight, not mutant-part Chance |
| `RC_Faust_Mutant_ItemGenerator` | → `AllAmmosNPCGenerator` (Chance 1.0); `GunAK74_ST` (Weight 50.0); `empty` (Weight 50.0); `GunPM_HG` (Weight 50.0); `Jacket_Bandit_Armor` (Weight 100.0); `empty` (Weight 50.0); → `ConsumableNewbieGenerator` (Chance 1.0); → `DetectorNewbieGenerator` (Chance 0.05); → `ArtifactNewbieGenerator` (Chance 0.01); → `AttachNewbieGenerator` (Chance 0.01) | FaustBase<br>FaustBoss<br>FaustSummon<br>FaustPsyMutant | EXCLUDE FROM GENERIC MUTANT DROPS — bespoke boss/loadout generator with shared nested pools |
| `DLC01_Object68LootGenerator` | `DLC01_Object68Loot` (Chance 1) | Object68 | DEFER SPECIAL/DLC — isolated direct Chance, but not part of standard mutant-part control set |
| `JadedChimeraLootGenerator` | `DLC01_ArtifactHeartofChimera` (Chance 1) | JadedChimera | DEFER SPECIAL/DLC — isolated direct Chance, but not part of standard mutant-part control set |

### Why these remain separate

- `MutantElectrocollarGenerator` contains `Electrocollar` with `Weight=1`, not a direct `Chance`; calling it a percentage drop chance would invent semantics.
- `RC_Faust_Mutant_ItemGenerator` is a bespoke boss/loadout generator with weapons, armor and nested ammo/consumable/detector/artifact/attachment pools. It is not a mutant-trophy generator.
- `DefaultMutantLootGenerator` has no resolved runtime consumer and yields `Sausage` through `Weight=2`; it is not used as the base for the species part generators.
- `DLC01_Object68LootGenerator` and `JadedChimeraLootGenerator` each have a clean direct `Chance=1` row, but they represent special DLC semantics and are deliberately not merged into the standard species control set.

## Control-safety conclusion

The current 14 standard mutant-part sliders are approved as **exact 1:1 source-backed controls**. No clone/rewire work is required for them. Their honest scope is “all eligible consumers wired to this exact species generator”, not “only the base creature prototype”.

No additional mutant generator should be promoted into the same UI group merely because its consumer is a mutant. The five non-standard roots use different semantics or special content and remain separate/deferred.

## Machine-readable artifacts

- `mutant-generator-inventory.csv` — one row per mapped mutant-domain generator with consumers, graph edges, fields and decision.
- `mutant-local-source-entries.csv` — all exact locally authored rows for the 19 mapped mutant generators.
- `mutant-consumer-edges.csv` — all direct runtime consumer edges for those generators.
- `mutant-control-audit.csv` — exact cross-check of the 14 visible mutant-part controls against raw/local CFG rows and writer baseline edit points.
- `mutant-domain-summary.json` — compact counts and audit conclusions.
