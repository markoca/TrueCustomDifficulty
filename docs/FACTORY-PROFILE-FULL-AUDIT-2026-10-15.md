# Full factory-profile source and decision audit — 2026-10-15

## Contract and scope

- **12 stock roles × 248 exact source-backed catalog IDs = 2,976 recorded setting decisions.** Every decision records the configured value, its `referenceDifficulty`-dependent vanilla comparator, role intent and source-specific reason in [`tools/factory_preset_full_source_audit.json`](../tools/factory_preset_full_source_audit.json).
- **197 confirmed source mappings; 51 marked `runtime_test_required`.** A known CFG edit path is not proof of measured live spawn frequency, duration stacking, treatment effectiveness, win rate or game balance. In-game validation remains separate.
- No made-up settings, no UI geometry/controls/CFG writer modifications, no changes to existing catalog semantics. Each profile keeps 248 native entries; duration-override root metadata is not a CFG parameter.
- The earlier source grids contained values that the VM silently normalizes on load. This pass reauthors factory stored values onto **the actual ParameterViewModel grid** (5pp percent controls, native absolute-step lanes, reference-relative steps and Override's five-second drink/medicine timing), and differentiates eight major mutant HP classes per role instead of copying one universal class scalar. Rare positive mutant-part chances never round down to zero solely from UI snapping.
- No automatic overwrite of customized presets. Only byte-identical pre-audit factory JSONs are reseeded via `PreFullDeepAuditFactoryBlobs`.

## Role objectives and tradeoffs

| Role | Reference | Survival and resource purpose | Combat / economy identity |
|---|---|---|---|
| Immersive | Stalker | believable food, sleep, drink and radiation pressure without repeated micromanagement | credible lethal gunfights, restrained health padding and recoverable mutant risk; measured income, meaningful repairs and consumable cost |
| Tactical | Veteran | quick tactical buffs but regular fatigue consequences | precise high-lethality human gunplay with demanding ammunition decisions; ammo, weapon upkeep and precision have an explicit financial cost |
| Field Medic | Stalker | strong recovery tools and extended medicine coverage | survivability through treatment rather than superior weapons; medicine is comparatively accessible but weapons costly |
| Mutant Hunter | Veteran | long specialist medicine helps counter hunt injuries | mutant hunts dangerous but worth preparing ammunition for; trophy coefficient funds real hunting preparation |
| Anomaly Runner | Stalker | water, energy and psi medicine support long exposure expeditions | avoid wasting supplies in combat before dangerous anomaly routes; artifact values justify high-risk exposure |
| Wanderer | Stalker | long food intervals and sustained traversal effects without continuous redosing | travel encounters risky without turning the trip into constant fights; expedition sales and travel expenses make logistics important |
| Mercenary | Veteran | contract mobility and useful stamina recovery | highly lethal contract engagements paid for by income; contracts and ammo stock are the income/supply center |
| Smuggler | Veteran | extended carry and water preparation for cargo runs | dangerous close-range trading routes and fragile acquired weapons; cargo and black-market arbitrage fund progression |
| Scavenger | Veteran | scarce food and restorative power make scavenging decisions meaningful | fight selectively because salvage equipment is weak; repairs and damaged resale dominate earnings |
| Outcast | Veteran | low-access survival pressures but avoid hourly forced meals | survive by evasion rather than overwhelming enemies; very weak access makes every transaction consequential |
| Hardcore | Master | scarcity raises the cost of mistakes, not food use frequency | punish errors but leave viable gunfights; expensive repairs and scarce rewards increase stakes |
| Hardcore+ | Master | harshest hunger and recovery tradeoffs, but no unplayable constant redosing | maximum danger with finite resources; no unlimited damage sponges; weakest purchasing power with finite but costly recovery routes |

## Every source setting, with the 12 committed role values

Abbreviations: IMM = Immersive; TAC = Tactical; MED = Field Medic; HUN = Mutant Hunter; ANO = Anomaly Runner; WAN = Wanderer; MER = Mercenary; SMU = Smuggler; SCA = Scavenger; OUT = Outcast; HC = Hardcore; HC+ = Hardcore+. Parent drink multipliers below are displayed as resulting CFG seconds for readability. All other values are stored source units, except mutant-part probability rows displayed as percentages. For **each cell**, see the matching `settings[id].roleDecisions[role].reason` and reference value in the machine-auditable decision ledger. `?` marks a source requiring runtime validation, not a broken setting.

### Combat

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `combat.npcHealth` | Changes how much damage human enemies can take before they die. | CFG | 0.85 | 0.65 | 0.95 | 0.95 | 0.85 | 0.95 | 0.75 | 0.85 | 0.85 | 0.95 | 0.95 | 0.85 |
| `combat.npcWeaponDamage` | Changes how much damage you take from firearm hits by human enemies. | CFG | 1.2 | 1.6 | 1.2 | 1.2 | 1.2 | 1.1 | 1.5 | 1.3 | 1.1 | 1.3 | 1.5 | 1.6 |
| `combat.npcAttackCooldown` | Controls how frequently human enemies can make another attack. | CFG | 1.1 | 1.4 | 1.1 | 1.1 | 1 | 1 | 1.5 | 1.3 | 1.1 | 1.3 | 1.5 | 1.6 |
| `combat.npcHipAccuracy` | Changes how accurate human enemy gunfire is. | CFG | 0.7 | 0.9 | 0.7 | 0.7 | 0.7 | 0.6 | 0.9 | 0.7 | 0.7 | 0.8 | 0.9 | 1 |
| `combat.explosionDamage` | Changes damage from explosions such as grenades and other explosive effects. | CFG | 1.1 | 1.3 | 1.1 | 1.1 | 1.2 | 1.2 | 1.3 | 1.1 | 1.1 | 1.1 | 1.4 | 1.7 |
| `combat.playerWeaponDamage` | Changes the base damage dealt by your weapons. | CFG | 1.25 | 1.75 | 1 | 1.25 | 1 | 1 | 1.5 | 1 | 1 | 1 | 1.25 | 1.25 |
| `combat.playerHeadshotMultiplier` | Controls your headshot damage multiplier, not a separate bonus added to the hit. | CFG | 2 | 3.5 | 1.75 | 2.5 | 2 | 1.75 | 3 | 2 | 2 | 2.25 | 2.5 | 2.75 |
| `combat.weaponJamming` | Changes how often worn weapons jam. | CFG | 1 | 1 | 1 | 1.25 | 1.25 | 1 | 1 | 1.5 | 1.5 | 1.5 | 1.5 | 1.75 |
| `combat.mutantDamage` | Changes how much damage mutant attacks deal when they connect. | CFG | 1.2 | 1.1 | 1.2 | 1.5 | 1.3 | 1.3 | 1.1 | 1.3 | 1.2 | 1.4 | 1.5 | 1.6 |
| `combat.accumulatedDamageWeightMax` | Sets the stronger end of the hidden protection the game can apply when several hits land close together. | CFG | 0.6 | 0.3 | 0.6 | 0.3 | 0.5 | 0.5 | 0.3 | 0.4 | 0.4 | 0.4 | 0.2 | 0.1 |
| `combat.accumulatedDamageWeightMin` | Sets the weaker end of the hidden protection the game can apply when several hits land close together. | CFG | 0.2 | 0.1 | 0.2 | 0.1 | 0.1 | 0.1 | 0.1 | 0.1 | 0.1 | 0.1 | 0.1 | 0 |
| `combat.accumulatedDamageIncludesHealedHealth` | Repeated-hit protection tracks damage taken over time. When this is ON, healing restores your HP but does not  | CFG | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |
| `combat.mutantHealth.bloodsucker` | Set Bloodsucker-family HP with one multiplier. Standard Bloodsucker is the displayed reference; Mist Bloodsuck | CFG | 1.05 | 1 | 1.1 | 1.45 | 1.05 | 1.05 | 1.05 | 1.15 | 1.05 | 1.1 | 1.3 | 1.5 |
| `combat.mutantHealth.chimera` | Increase or decrease Chimera HP. | CFG | 1.1 | 1.05 | 1.15 | 1.6 | 1.15 | 1.2 | 1.2 | 1.25 | 1.2 | 1.4 | 1.4 | 1.65 |
| `combat.mutantHealth.deer` | Increase or decrease Deer HP. | CFG | 1 | 0.95 | 1 | 1.15 | 1 | 1 | 1 | 1.05 | 1 | 1.05 | 1.2 | 1.3 |
| `combat.mutantHealth.pseudogiant` | Increase or decrease Pseudogiant HP. | CFG | 1.25 | 1.2 | 1.25 | 1.65 | 1.2 | 1.3 | 1.3 | 1.35 | 1.3 | 1.5 | 1.65 | 1.8 |
| `combat.mutantHealth.pseudodog` | Increase or decrease Pseudodog HP. | CFG | 1.05 | 1.05 | 1.05 | 1.2 | 1 | 1.1 | 1.1 | 1.1 | 1.15 | 1.2 | 1.3 | 1.45 |
| `combat.mutantHealth.burer` | Increase or decrease Burer HP. | CFG | 1.15 | 1.1 | 1.1 | 1.4 | 1.15 | 1.15 | 1.15 | 1.2 | 1.1 | 1.3 | 1.35 | 1.55 |
| `combat.mutantHealth.bayun` | Increase or decrease Bayun HP. | CFG | 1.05 | 1 | 1 | 1.25 | 1.1 | 1 | 1.05 | 1.1 | 1.1 | 1.2 | 1.25 | 1.45 |
| `combat.mutantHealth.blinddog` | Scales Blind Dog maximum HP. Vanilla: 63 HP. | CFG | 1.05 | 1 | 1.05 | 1.45 | 1 | 1 | 1.05 | 1.2 | 1.15 | 1.3 | 1.4 | 1.6 |
| `combat.mutantHealth.boar` | Scales Boar maximum HP. Vanilla: 300 HP. | CFG | 1.1 | 1.05 | 1.1 | 1.55 | 1.05 | 1.05 | 1.1 | 1.25 | 1.15 | 1.35 | 1.5 | 1.7 |
| `combat.mutantHealth.flesh` | Scales Flesh maximum HP. Vanilla: 280 HP. | CFG | 1 | 0.95 | 1 | 1.3 | 1 | 1 | 1 | 1.15 | 1.1 | 1.2 | 1.35 | 1.55 |
| `combat.mutantHealth.poltergeist` | Scales Poltergeist maximum HP. Vanilla: 400 HP. | CFG | 1.05 | 1.1 | 1.1 | 1.55 | 1.3 | 1.15 | 1.1 | 1.3 | 1.1 | 1.35 | 1.5 | 1.75 |
| `combat.mutantHealth.snork` | Scales Snork maximum HP. Vanilla: 150 HP. | CFG | 1.05 | 1.05 | 1.05 | 1.45 | 1 | 1.05 | 1.1 | 1.2 | 1.2 | 1.3 | 1.45 | 1.65 |
| `combat.mutantHealth.tushkan` | Scales Tushkan maximum HP. Vanilla: 19 HP. | CFG | 1 | 0.95 | 1 | 1.3 | 0.95 | 1 | 1 | 1.1 | 1.1 | 1.2 | 1.35 | 1.55 |
| `combat.mutantHealth.controller` | Increase or decrease Controller HP. | CFG | 1.2 | 1.15 | 1.2 | 1.45 | 1.3 | 1.2 | 1.2 | 1.3 | 1.2 | 1.35 | 1.45 | 1.6 |
| `combat.psySpawnCycleDelay` | Controls how long Psy encounters using the Master override wait before another phantom spawn cycle can begin. | ? | 0.95 | 0.85 | 0.95 | 0.65 | 0.85 | 0.75 | 0.85 | 0.85 | 0.85 | 0.55 | 0.65 | 0.55 |
| `combat.psyPhantomCount` | Controls how many Psy phantoms the Master override adds to the affected encounters. | ? | 1.1 | 1.1 | 1.1 | 1.6 | 1.2 | 1.4 | 1.3 | 1.1 | 1.1 | 1.9 | 1.6 | 1.9 |
| `combat.psyBleeding` | Lets the affected Master Psy phantoms inflict bleeding in addition to their normal threat. | ? | OFF | ON | OFF | ON | OFF | OFF | ON | ON | ON | ON | ON | ON |
| `combat.psyDurabilityDamage` | Controls how much equipment durability the affected Master Psy enemies damage. | ? | 1.1 | 1.2 | 1.1 | 1.3 | 1.2 | 1.1 | 1.2 | 1.4 | 1.4 | 1.6 | 1.6 | 1.9 |

### Survival

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `survival.hungerRate` | Changes how quickly you become hungry. | CFG | 0.009 | 0.01 | 0.008 | 0.01 | 0.009 | 0.007 | 0.01 | 0.01 | 0.011 | 0.012 | 0.013 | 0.014 |
| `survival.hungerLowThreshold` | Controls when the Low hunger state begins on the 0-100 HungerPoints scale. | CFG | 17 | 17 | 17 | 17 | 17 | 17 | 17 | 17 | 17 | 17 | 17 | 17 |
| `survival.hungerMediumThreshold` | Controls when the standard Medium hunger state begins. | CFG | 33 | 33 | 33 | 33 | 33 | 33 | 33 | 33 | 33 | 33 | 33 | 33 |
| `survival.hungerStrongThreshold` | Controls when the standard Severe hunger state begins. | CFG | 83 | 83 | 83 | 83 | 83 | 83 | 83 | 83 | 83 | 83 | 83 | 83 |
| `survival.hungerMediumStaminaRegen` | Controls how strongly medium hunger slows stamina recovery. | CFG | 0.4 | 0.35 | 0.4 | 0.4 | 0.3 | 0.3 | 0.35 | 0.45 | 0.45 | 0.45 | 0.5 | 0.6 |
| `survival.hungerStrongStaminaRegen` | Controls how strongly severe hunger slows stamina recovery. | CFG | 0.8 | 0.75 | 0.8 | 0.8 | 0.65 | 0.65 | 0.75 | 0.85 | 0.85 | 0.85 | 0.9 | 1 |
| `survival.sleepinessRate` | Changes how quickly you become tired and need sleep. | CFG | 0.012 | 0.014 | 0.012 | 0.014 | 0.013 | 0.012 | 0.014 | 0.015 | 0.016 | 0.017 | 0.017 | 0.019 |
| `survival.sleepLowThreshold` | Controls where the Low fatigue stage begins on the real 0-100 SleepinessPoints axis. | CFG | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 |
| `survival.sleepMediumThreshold` | Controls where Medium fatigue begins. | CFG | 60 | 60 | 60 | 60 | 60 | 60 | 60 | 60 | 60 | 60 | 60 | 60 |
| `survival.sleepStrongThreshold` | Controls where Severe fatigue begins. | CFG | 80 | 80 | 80 | 80 | 80 | 80 | 80 | 80 | 80 | 80 | 80 | 80 |
| `survival.energyDrinkFatigueRelief` | Lets the player tune how strongly energy drinks counter fatigue without changing how quickly fatigue naturally | CFG | 1.1 | 1.45 | 1.3 | 1.4 | 1.65 | 1.8 | 1.6 | 1.2 | 0.85 | 0.7 | 0.65 | 0.55 |
| `survival.waterInstantStaminaRestore` | Sets the exact instant stamina refill from Water independently; sustained buffs and durations remain separate. | CFG | 0.55 | 0.5 | 0.65 | 0.55 | 0.75 | 0.85 | 0.62 | 0.6 | 0.48 | 0.4 | 0.35 | 0.28 |
| `survival.energyDrinkInstantStaminaRestore` | Sets the exact instant stamina refill from regular Energy Drink independently; sustained buffs and durations r | CFG | 0.65 | 0.8 | 0.65 | 0.75 | 0.9 | 0.95 | 0.85 | 0.75 | 0.6 | 0.48 | 0.45 | 0.38 |
| `survival.waterStaminaEffectStrength` | Scales only WaterStamina2's +25% timed Stamina effect. | CFG | 1.15 | 1 | 1.25 | 1.2 | 1.7 | 2 | 1.35 | 1.25 | 1.1 | 0.9 | 0.8 | 0.7 |
| `survival.waterStaminaCostReduction` | Controls Water's native stamina drain reduction independently of its +25% Stamina effect and 38% instant stami | CFG | 1.1 | 1.1 | 1.2 | 1.1 | 1.65 | 2 | 1.4 | 1.3 | 1 | 0.85 | 0.75 | 0.6 |

### Health

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `health.beerRadiationRemoval` | Independent Beer radiation-removal strength; keeps the vanilla alcohol cure mechanic. | CFG | 10 | 10 | 15 | 10 | 15 | 15 | 10 | 15 | 10 | 5 | 5 | 5 |
| `health.vodkaRadiationRemoval` | Independent Vodka radiation-removal strength; keeps the vanilla alcohol cure mechanic. | CFG | 50 | 50 | 65 | 55 | 65 | 65 | 55 | 70 | 45 | 40 | 35 | 25 |

### Survival

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `survival.waterEffectDuration` | Controls the main Water stamina-cost reduction duration, which also caps the shorter regeneration effect. | CFG | 120s | 90s | 105s | 120s | 180s | 240s | 105s | 150s | 90s | 100s | 120s | 90s |
| `survival.energyDrinkStaminaRecoveryStrength` | Scales sustained stamina regeneration from normal Energy Drink, not its instant refill or duration. | CFG | 1.2 | 1.4 | 1.15 | 1.4 | 1.8 | 2 | 1.65 | 1.45 | 1.1 | 0.95 | 0.85 | 0.75 |
| `survival.energyDrinkEffectDuration` | Controls how long Energy Drink stamina buffs last. | CFG | 180s | 180s | 135s | 180s | 225s | 270s | 180s | 195s | 150s | 135s | 180s | 150s |
| `survival.waterRegenDuration` | Controls how long Water's +25% Max Stamina buff lasts. | CFG | 45 | 25 | 30 | 40 | 75 | 100 | 30 | 55 | 25 | 20 | 25 | 15 |
| `survival.energyFullCostDuration` | Controls how long Energy Drink removes stamina costs for affected actions. | CFG | 25 | 35 | 25 | 30 | 45 | 60 | 35 | 35 | 20 | 15 | 15 | 10 |
| `survival.energyPartialCostDuration` | Controls how long Energy Drink reduces stamina costs for affected actions by 30%. | CFG | 80 | 90 | 60 | 85 | 120 | 160 | 95 | 100 | 65 | 50 | 50 | 40 |
| `survival.vodkaEffectDuration` | Controls how long Vodka's stamina penalty lasts. | CFG | 20s | 20s | 20s | 25s | 20s | 15s | 20s | 20s | 25s | 25s | 30s | 35s |
| `survival.beerEffectDuration` | Controls how long Beer's stamina penalty lasts. | CFG | 20s | 20s | 20s | 20s | 20s | 15s | 20s | 20s | 20s | 25s | 25s | 30s |
| `survival.magicVodkaEffectDuration` | Controls how long Magic Vodka's psi protection lasts. | CFG | 120s | 90s | 145s | 160s | 210s | 230s | 125s | 180s | 100s | 90s | 90s | 75s |
| `survival.vincaBleedRecovery` | Vinca's game-facing bleeding regeneration bonus, +10 bleed points by default. The exact effect tick/rate is no | ? | 12 | 9 | 30 | 25 | 18 | 16 | 20 | 12 | 8 | 7 | 6 | 5 |
| `survival.vincaDuration` | Duration of Vinca's ongoing bleeding recovery | ? | 600 | 540 | 750 | 650 | 650 | 700 | 650 | 600 | 500 | 450 | 425 | 350 |
| `survival.herculesCarryBonus` | Hercules grants +20 kg of carry capacity by default. Its two linked game effects share one +kg setting; they a | ? | 22 | 22 | 30 | 32 | 35 | 50 | 30 | 55 | 25 | 18 | 14 | 10 |
| `survival.herculesDuration` | Duration of both linked Hercules effects | ? | 420 | 350 | 480 | 500 | 650 | 800 | 500 | 750 | 480 | 360 | 310 | 270 |
| `survival.psiBlockRecovery` | Psi Block has a +10 psi-protection point game-facing bonus by default (CFG DegenPsyPoints). This is not a perc | ? | 12 | 8 | 30 | 24 | 28 | 24 | 12 | 18 | 7 | 6 | 5 | 4 |
| `survival.psiBlockDuration` | Duration of Psi Block psi-point recovery | ? | 450 | 300 | 750 | 700 | 850 | 800 | 450 | 500 | 350 | 300 | 250 | 200 |
| `survival.drunknessThresholdScale` | Retained as a source-correct internal mapping while OverDrunkness remains separately audited. | CFG | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| `survival.beerDrunknessStrength` | Controls how strongly one Beer pushes the drunkenness meter. | ? | 15 | 15 | 14 | 15 | 15 | 15 | 15 | 15 | 16 | 18 | 20 | 22 |
| `survival.vodkaDrunknessStrength` | Controls how strongly one Vodka pushes the drunkenness meter. | ? | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 35 | 40 | 45 |
| `survival.sleepEligibilityThreshold` | Controls how tired the player must be before normal sleeping is allowed. | CFG | 45 | 50 | 45 | 45 | 50 | 50 | 50 | 45 | 45 | 45 | 40 | 35 |
| `survival.minimumSleepHours` | Sets the minimum sleep duration used by the default sleep settings. | CFG | 6 | 6 | 6 | 6 | 5 | 5 | 6 | 7 | 7 | 7 | 7 | 8 |
| `survival.radiationAccumulation` | Scales the rate at which radiation is accumulated through the difficulty preset. | CFG | 1.16 | 1.15 | 1.1 | 1.18 | 1.55 | 1.45 | 1.18 | 1.45 | 1.25 | 1.38 | 1.42 | 1.6 |
| `survival.radiationRecovery` | Controls how quickly accumulated radiation decreases naturally, in points per second. | CFG | 0.04 | 0.04 | 0.05 | 0.03 | 0.05 | 0.05 | 0.04 | 0.03 | 0.03 | 0.02 | 0.02 | 0.01 |
| `survival.radiationDamage` | Controls the health damage caused by radiation after the game has already decided that your current radiation  | CFG | 1.16 | 1.15 | 1.1 | 1.18 | 1.55 | 1.45 | 1.18 | 1.45 | 1.25 | 1.38 | 1.42 | 1.6 |
| `survival.bleedingSeverity` | Scales the difficulty-wide bleeding effect applied by sources that cause bleeding. | CFG | 1.2 | 1.4 | 1.3 | 1.5 | 1.4 | 1.2 | 1.5 | 1.3 | 1.3 | 1.5 | 1.7 | 1.8 |
| `survival.bleedingRecovery` | Controls the player's base natural bleeding reduction rate. | CFG | 0.3 | 0.2 | 0.3 | 0.2 | 0.3 | 0.25 | 0.25 | 0.2 | 0.2 | 0.1 | 0.1 | 0.05 |

### Health

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `health.maxHP` | Changes your maximum health. The game's full-health feedback stays in sync with the new maximum. | ? | 100 | 90 | 100 | 100 | 100 | 105 | 95 | 90 | 90 | 85 | 85 | 75 |
| `health.baseRegen` | Controls ordinary passive health recovery on the player. Vanilla sets this to zero, so by default you do not s | ? | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| `health.regenDelay` | Sets how many seconds must pass before health recovery can begin. Vanilla is 5 seconds. | ? | 6 | 7 | 5 | 6 | 7 | 6 | 7 | 8 | 8 | 9 | 9 | 10 |
| `health.regenerationScalar` | Changes the strength of health-recovery effects that are already active. It does not create passive healing by | CFG | 0.95 | 0.9 | 1.1 | 0.9 | 1.05 | 1.05 | 0.9 | 0.75 | 0.75 | 0.75 | 0.6 | 0.4 |
| `health.lowHealthRegen` | Sets the recovery speed used by the normal low-health recovery effect. Vanilla is 1 HP/s. | CFG | 0.8 | 0.6 | 0.8 | 0.5 | 0.75 | 0.75 | 0.6 | 0.55 | 0.55 | 0.55 | 0.5 | 0.2 |

### Master

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `advanced.lowHealthMechanics` | Turns on the extra low-health punishment used by vanilla Master. | CFG | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |
| `health.masterLowHealthPenalty` | Controls how strongly Master difficulty slows health recovery when you are badly wounded. | CFG | 0.55 | 0.6 | 0.55 | 0.6 | 0.5 | 0.5 | 0.6 | 0.5 | 0.5 | 0.5 | 0.75 | 0.9 |

### Health

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `health.bandageHealing` | Set HP restored by Bandages, Medkits, Army Medkits, and Scientific Medkits. Bleeding and radiation removal are | CFG | 0 | 0 | 10 | 0 | 0 | 0 | 0 | 0 | 10 | 0 | 0 | 0 |
| `health.medkitHealing` | Controls how much health a standard Medkit restores before any Master-only healing penalty is applied. | CFG | 60 | 45 | 105 | 65 | 60 | 60 | 65 | 55 | 50 | 40 | 45 | 35 |
| `health.armyMedkitHealing` | Controls how much health an Army Medkit restores before any Master-only healing penalty is applied. | CFG | 80 | 60 | 125 | 85 | 85 | 80 | 85 | 70 | 60 | 50 | 60 | 50 |
| `health.ecoMedkitHealing` | Controls how much health a Scientific Medkit restores. Vanilla restores 100 HP. | CFG | 95 | 75 | 140 | 100 | 105 | 105 | 95 | 90 | 80 | 65 | 70 | 55 |
| `health.foodHealing` | Separates food from medical healing. OFF removes the normal HP restoration from the four ordinary healing-food | CFG | OFF | OFF | OFF | OFF | OFF | OFF | OFF | OFF | OFF | OFF | OFF | OFF |

### Master

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `health.masterBandageModifier` | Controls how much of the normal Bandage healing remains while Master Consumable Effect Penalties are active. V | CFG | 0.7 | 0.6 | 0.7 | 0.5 | 0.65 | 0.65 | 0.6 | 0.5 | 0.5 | 0.5 | 0.35 | 0.15 |
| `health.masterMedkitModifier` | Controls how much of the normal Medkit healing remains while Master Consumable Effect Penalties are active. Va | CFG | 0.85 | 0.8 | 0.85 | 0.75 | 0.8 | 0.8 | 0.8 | 0.72 | 0.72 | 0.72 | 0.6 | 0.5 |
| `health.masterArmyMedkitModifier` | Controls how much of the normal Army Medkit healing remains while Master Consumable Effect Penalties are activ | CFG | 0.9 | 0.88 | 0.9 | 0.85 | 0.9 | 0.9 | 0.88 | 0.82 | 0.82 | 0.82 | 0.72 | 0.65 |

### Health

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `health.bandageBleeding` | Controls how much bleeding the Bandage removes when used. | CFG | 75 | 80 | 100 | 85 | 80 | 80 | 80 | 70 | 70 | 60 | 65 | 50 |
| `health.medkitBleeding` | Controls how much bleeding the Medkit removes when used. | CFG | 10 | 10 | 30 | 10 | 10 | 10 | 10 | 10 | 10 | 5 | 5 | 5 |
| `health.armyMedkitBleeding` | Controls how much bleeding the Army Medkit removes when used. | CFG | 25 | 25 | 45 | 25 | 25 | 25 | 25 | 20 | 20 | 10 | 15 | 10 |
| `health.ecoMedkitBleeding` | Controls how much bleeding the Scientific Medkit removes when used. | CFG | 20 | 20 | 50 | 20 | 20 | 20 | 20 | 15 | 15 | 10 | 10 | 10 |
| `health.ecoMedkitAntirad` | Controls how much radiation the Scientific Medkit removes when used. | CFG | 55 | 45 | 105 | 50 | 75 | 60 | 55 | 55 | 45 | 45 | 45 | 35 |
| `health.antiRadRadiation` | Controls how much radiation the Anti-Rad removes when used. | CFG | 85 | 75 | 120 | 80 | 130 | 100 | 85 | 85 | 70 | 65 | 65 | 55 |

### Stamina

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `stamina.maxSP` | Changes your maximum stamina pool. Vanilla is 100. | CFG | 105 | 115 | 105 | 120 | 150 | 155 | 120 | 120 | 90 | 90 | 80 | 75 |
| `stamina.sprintDisableThreshold` | Sets how low your stamina can fall before sprinting is forced to stop. Vanilla is 16.1 stamina. | CFG | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 | 16.1 |
| `stamina.sprintRecoveryDelay` | Changes how long you must wait after running out of sprint stamina before recovery can begin. | CFG | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| `stamina.lowRecoveryThreshold` | Sets the stamina level that triggers emergency low-stamina recovery. Vanilla is 30 stamina. | CFG | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30 |
| `stamina.lowRecoveryStrength` | Sets how strong emergency stamina recovery is while it is active. Vanilla is 10 stamina/s. | CFG | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 |
| `stamina.lowRecoveryDuration` | Sets how long emergency stamina recovery lasts once it is triggered. Vanilla is 4 seconds. | CFG | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 |
| `stamina.sprintUnderRunSpeedRegenCoef` | Changes stamina recovery during speed-limited sprinting. | ? | -0.65 | -0.5 | -0.55 | -0.55 | -0.3 | -0.25 | -0.5 | -0.6 | -0.85 | -0.9 | -0.95 | -1.1 |
| `stamina.joggingRegenCoef` | Changes stamina recovery while the game treats your movement as jogging. | ? | 0 | 0.2 | 0.05 | 0.1 | 0.3 | 0.35 | 0.15 | 0.05 | -0.05 | -0.1 | -0.15 | -0.2 |
| `stamina.slowRunRegenCoef` | Changes stamina recovery while the game treats your movement as slow running. | ? | 0 | 0.1 | 0.05 | 0.05 | 0.2 | 0.25 | 0.1 | 0 | -0.1 | -0.1 | -0.2 | -0.25 |
| `stamina.vaultMaxHeight` | Changes the tallest obstacle you can vault over. The WAIST / EYES / ABOVE EYES markers are simple player-frien | ? | 135 | 145 | 145 | 145 | 180 | 175 | 150 | 160 | 115 | 115 | 105 | 95 |
| `stamina.waterMediumDrain` | Changes the extra stamina drain while moving through medium-depth water. | CFG | 1.1 | 1.15 | 1.1 | 1.25 | 1.1 | 1.1 | 1.15 | 1.25 | 1.25 | 1.25 | 1.5 | 2 |
| `stamina.waterStrongDrain` | Changes the extra stamina drain while moving through deep water. | CFG | 2.2 | 2.25 | 2.2 | 2.5 | 2.1 | 2.1 | 2.25 | 2.5 | 2.5 | 2.5 | 2.75 | 3 |
| `stamina.regeneration` | Controls the player's base stamina regeneration rate. | CFG | 7 | 8 | 7 | 8 | 9 | 10 | 8 | 7 | 6 | 6 | 5 | 5 |
| `stamina.sprintCost` | Set how tiring sprinting, jumping, vaulting, and melee attacks are. The Aiming point changes stamina recovery  | CFG | 5.25 | 3.5 | 5.25 | 5 | 2.75 | 2.5 | 3.25 | 5.5 | 6.25 | 7 | 8 | 8.75 |
| `stamina.jumpCost` | Controls stamina consumed by a jump. | CFG | 16 | 12 | 14 | 13 | 8 | 7 | 10 | 16 | 19 | 22 | 26 | 30 |
| `stamina.vaultCost` | Controls stamina consumed by vaulting. | CFG | 13 | 8 | 12 | 10 | 6 | 5 | 8 | 13 | 16 | 19 | 21 | 23 |
| `stamina.meleeNormalCost` | Controls stamina consumed by a quick melee attack. | CFG | 16 | 12 | 15 | 16 | 10 | 7 | 10 | 17 | 21 | 23 | 26 | 32 |
| `stamina.meleeStrongCost` | Controls stamina consumed by a strong melee attack. | CFG | 26 | 19 | 24 | 20 | 14 | 12 | 17 | 27 | 31 | 38 | 42 | 48 |
| `stamina.butstockCost` | Controls stamina consumed by a buttstock melee attack. | CFG | 13 | 10 | 13 | 14 | 10 | 10 | 10 | 14 | 14 | 14 | 15 | 18 |
| `stamina.sprintRegenCoef` | Changes stamina recovery while sprinting. | CFG | -1.3 | -1.1 | -1.35 | -1.15 | -0.8 | -0.7 | -1.15 | -1.25 | -1.6 | -1.85 | -2 | -2.3 |
| `stamina.runRegenCoef` | Changes stamina recovery during normal running. | ? | -0.65 | -0.5 | -0.55 | -0.55 | -0.35 | -0.3 | -0.5 | -0.65 | -0.8 | -0.9 | -0.9 | -1.05 |
| `stamina.aimRegenCoef` | Controls the aiming-only regeneration penalty independently from low-stamina recovery effects. | CFG | 0.25 | 0.15 | 0.2 | 0.2 | 0.1 | 0.1 | 0.15 | 0.25 | 0.3 | 0.35 | 0.4 | 0.45 |
| `stamina.maxCarryWeight` | Sets the player's default maximum inventory mass. | CFG | 75 | 70 | 85 | 80 | 85 | 100 | 80 | 110 | 70 | 55 | 55 | 50 |

### Loot

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `loot.npcCorpseBonus` | Chooses which extra smart-loot package is added to human NPC corpses on top of their normal generated inventor | CFG | empty | empty | empty | empty | empty | empty | empty | empty | empty | empty | empty | empty |
| `loot.npcMedicalChance` | Changes medical-item drop frequency on ordinary human enemies without changing food, drinks, stimulants, alcoh | ? | 0.6 | 0.5 | 0.95 | 0.5 | 0.55 | 0.5 | 0.5 | 0.4 | 0.35 | 0.3 | 0.3 | 0.25 |
| `loot.npcWeaponAmmoSupply` | Controls generic bonus ammo counts on ordinary human NPC corpses when the Extra Corpse Loot choice enables Med | ? | 0.5 | 0.75 | 0.5 | 0.5 | 0.5 | 0.5 | 1 | 0.25 | 0.5 | 0.25 | 0.25 | 0.25 |
| `loot.droppedWeaponCondition` | Sets the condition range of weapons dropped by ordinary human enemies. | ? | 0.25–0.55 | 0.3–0.6 | 0.25–0.55 | 0.2–0.5 | 0.25–0.55 | 0.25–0.55 | 0.3–0.6 | 0.15–0.45 | 0.15–0.45 | 0.15–0.45 | 0.1–0.3 | 0.05–0.2 |
| `loot.cheapStashAmmoQuantity` | Set how much ammo Basic, Standard, and Rare stashes contain. The ammo types available in each stash tier stay  | ? | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.5 | 0.5 | 0.25 | 0.25 | 0.25 |
| `loot.cheapStashGrenadeChance` | Controls how often an RGD-5 grenade appears in a Basic stash. It does not change the grenade amount or other s | ? | 0.1 | 0.1 | 0.1 | 0.05 | 0.05 | 0.05 | 0.1 | 0.05 | 0.05 | 0.05 | 0.05 | 0.05 |
| `loot.cheapStashGrenadeQuantity` | Controls how many RGD-5 grenades you get when the Basic-stash grenade roll succeeds. | ? | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| `loot.mediumStashAmmoQuantity` | Changes only the number of rounds found in Standard stashes, not which ammo types can appear. | ? | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.5 | 0.5 | 0.25 | 0.25 | 0.25 |
| `loot.destructibleAmmoQuantity` | Changes how many rounds breakable ammo boxes give you without changing which ammo families they favor. | ? | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 |
| `loot.ammoBoxWeight918` | Choose which ammo families are more or less common in breakable ammo boxes. This changes the ammo mix, not the | ? | 1 | 1 | 1 | 0.75 | 1 | 1 | 1 | 0.75 | 0.75 | 0.75 | 0.75 | 0.5 |
| `loot.ammoBoxWeight045` | Changes how common .45 ACP ammo is in breakable ammo boxes. | ? | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1.25 |
| `loot.ammoBoxWeight545` | Changes how common 5.45 / 5.56 mm ammo is in breakable ammo boxes. | ? | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1.25 | 1.25 | 1.25 | 1.25 | 1.25 |
| `loot.ammoBoxWeight012` | Changes how common 12 Gauge ammo is in breakable ammo boxes. | ? | 1 | 1 | 1 | 0.75 | 1 | 1 | 1 | 0.75 | 0.75 | 0.75 | 0.75 | 0.5 |
| `loot.destructibleGrenadeQuantity` | Changes how many grenades breakable grenade boxes give you without changing which grenade type appears. | ? | 1 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 | 0.5 |
| `loot.expensiveStashAmmoQuantity` | Changes only the number of rounds found in Rare stashes, not which ammo types can appear. | ? | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.75 | 0.5 | 0.5 | 0.25 | 0.25 | 0.25 |
| `loot.expensiveStashMoneyChance` | Changes how often Rare stashes include coupon rewards. Your progression still decides how large that reward ca | CFG | 0.1 | 0.05 | 0.1 | 0.05 | 0.05 | 0.05 | 0.05 | 0.05 | 0.05 | 0.05 | 0.05 | 0 |

### Economy

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `economy.startingMoney` | Sets how many coupons a new game starts with. It does not change quest rewards, trader prices, stash coupons,  | CFG | 500 | 300 | 700 | 250 | 250 | 250 | 2000 | 2500 | 50 | 0 | 0 | 0 |
| `economy.buyCondition` | Changes how much traders pay when buying items from you. Harder vanilla presets pay less. | ? | 0.68 | 0.65 | 0.68 | 0.6 | 0.64 | 0.65 | 0.72 | 0.5 | 0.48 | 0.4 | 0.45 | 0.36 |
| `economy.sellCondition` | Changes how much traders charge when selling items to you. Harder vanilla presets charge more. | ? | 1.17 | 1.27 | 1.16 | 1.3 | 1.22 | 1.2 | 1.24 | 1.55 | 1.5 | 1.66 | 1.75 | 1.85 |
| `economy.traderMinBuyCondition` | This is the dedicated sell-condition gate for damaged weapons and armor, separate from Trader Buy-From-You Pri | ? | 0.37 | 0.42 | 0.37 | 0.43 | 0.41 | 0.41 | 0.42 | 0.51 | 0.51 | 0.51 | 0.58 | 0.62 |
| `economy.itemValueFloor` | ItemCostMinPercent directly defines the fraction of base item value retained at zero durability. Vanilla is 0. | CFG | 0.1 | 0.1 | 0.1 | 0.05 | 0.1 | 0.1 | 0.1 | 0.05 | 0.05 | 0.05 | 0.05 | 0.05 |
| `economy.traderAmmoStock` | Changes trader ammo stock without changing what kinds of ammo each trader is meant to sell. | ? | 0.75 | 1 | 0.5 | 0.75 | 0.5 | 0.5 | 1.25 | 0.5 | 0.25 | 0.25 | 0.25 | 0.25 |
| `economy.medicStock` | Changes how many medical supplies medics carry without changing the rest of trader stock. | ? | 0.75 | 0.5 | 1.25 | 0.75 | 0.5 | 0.5 | 0.75 | 0.5 | 0.5 | 0.25 | 0.25 | 0.25 |
| `economy.consumableCost` | Set the base value of consumables, ammo, armor, weapons, artifacts, and mutant parts. Trader buy/sell pricing  | CFG | 1.2 | 1.3 | 1.12 | 1.22 | 1.28 | 1.25 | 1.25 | 1.42 | 1.35 | 1.5 | 1.48 | 1.65 |
| `economy.ammoCost` | The in-game difficulty presets expose Ammo_Cost directly and keep it at 1.00 on all four vanilla difficulties. | CFG | 1.2 | 1.35 | 1.25 | 1.28 | 1.28 | 1.3 | 1.18 | 1.45 | 1.45 | 1.58 | 1.55 | 1.65 |
| `economy.armorCost` | The in-game difficulty presets expose Armor_Cost directly and keep it at 1.00 on all four vanilla difficulties | CFG | 1.14 | 1.18 | 1.14 | 1.18 | 1.18 | 1.17 | 1.2 | 1.3 | 1.28 | 1.45 | 1.45 | 1.55 |
| `economy.weaponCost` | The in-game difficulty presets expose Weapon_Cost directly and keep it at 1.00 on all four vanilla difficultie | CFG | 1.15 | 1.2 | 1.15 | 1.2 | 1.16 | 1.16 | 1.2 | 1.35 | 1.28 | 1.45 | 1.45 | 1.6 |
| `economy.artifactCost` | The in-game difficulty presets expose Artifact_Cost directly and keep it at 1.00 on all four vanilla difficult | CFG | 1.2 | 1.1 | 1 | 1.1 | 1.85 | 1.65 | 1.15 | 1.95 | 1.15 | 1.1 | 1.2 | 1.2 |
| `economy.mutantPartValue` | Controls how profitable mutant-part hunting is before normal trader pricing is applied. | CFG | 1.1 | 1.05 | 1.05 | 1.95 | 1.15 | 1.2 | 1.1 | 1.5 | 1.4 | 1.3 | 1.35 | 1.4 |
| `economy.fastTravelCost` | Provides one clear economy control for guide travel without flattening the different vanilla route prices. | CFG | 1.12 | 1.12 | 1.05 | 1.25 | 1.45 | 1.6 | 1.2 | 1.65 | 1.4 | 1.55 | 1.6 | 1.65 |
| `economy.upgradeCost` | Changes upgrade prices. Vanilla is 1x on Novice/Stalker, 1.25x on Veteran, and 1.5x on Master. | CFG | 1.12 | 1.16 | 1.12 | 1.24 | 1.18 | 1.17 | 1.2 | 1.4 | 1.34 | 1.48 | 1.45 | 1.65 |
| `economy.repairDifficultyCost` | Kept fixed to avoid giving you two different global sliders that both do the same job as Base Repair Price. | CFG | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| `economy.repairCostBase` | The main global setting for making repair services cheaper or more expensive. | CFG | 1.08 | 1.25 | 0.85 | 1.08 | 1.2 | 1.12 | 1.15 | 1.38 | 1.18 | 1.45 | 1.5 | 1.55 |
| `economy.repairEnemy` | Changes repair prices for the Enemy reputation tier. | CFG | 2.25 | 2.2 | 2.25 | 2.4 | 2.2 | 2.2 | 2.2 | 2.4 | 2.4 | 2.4 | 2.5 | 3 |
| `economy.repairDisaffection` | Changes repair prices for the Disliked reputation tier. | CFG | 1.6 | 1.6 | 1.6 | 1.7 | 1.55 | 1.55 | 1.6 | 1.7 | 1.7 | 1.7 | 1.75 | 2 |
| `economy.repairNeutral` | Changes repair prices for the Neutral reputation tier. | CFG | 1 | 1.05 | 1 | 1.1 | 1 | 1 | 1.05 | 1.15 | 1.15 | 1.15 | 1.2 | 1.5 |
| `economy.repairFriend` | Changes repair prices for the Friend reputation tier. | CFG | 0.7 | 0.75 | 0.7 | 0.75 | 0.7 | 0.7 | 0.75 | 0.8 | 0.8 | 0.8 | 0.85 | 1 |
| `economy.mainQuestReward` | Reward_MainLine_Coupon is a direct difficulty setting: 135% on Novice, 100% on Stalker, 75% on Veteran and 55% | CFG | 0.85 | 0.8 | 0.9 | 0.82 | 0.8 | 0.85 | 1.12 | 1.02 | 0.6 | 0.55 | 0.45 | 0.35 |
| `economy.sideQuestReward` | Reward_SideLine_Coupon is a direct difficulty setting: 135% on Novice, 100% on Stalker, 75% on Veteran and 55% | CFG | 0.85 | 0.92 | 0.9 | 1.05 | 0.95 | 0.95 | 1.4 | 1.25 | 0.55 | 0.55 | 0.5 | 0.42 |

### Equipment

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `equipment.armorDurability` | Changes how quickly armor loses condition across the selected Base Preset. | ? | 0.9 | 0.78 | 1.05 | 0.85 | 0.92 | 0.95 | 0.9 | 0.8 | 0.85 | 0.8 | 0.78 | 0.75 |
| `equipment.ammoWeight` | Provides one carry-weight control for ammunition without flattening the different vanilla weights of bullets,  | CFG | 1.18 | 1.23 | 1.08 | 1.25 | 1.12 | 1.1 | 1.15 | 1.35 | 1.3 | 1.35 | 1.38 | 1.45 |
| `equipment.consumableWeight` | Lets consumables contribute more or less to inventory weight without changing their healing, hunger, radiation | CFG | 1.1 | 1.12 | 1.08 | 1.18 | 1.1 | 1.08 | 1.14 | 1.24 | 1.25 | 1.35 | 1.4 | 1.5 |

### Environment

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `environment.anomalyDamage` | This is the broad anomaly difficulty setting. It scales damaging anomaly output as a group while each anomaly  | CFG | 1.15 | 1.15 | 1.1 | 1.35 | 1.55 | 1.35 | 1.15 | 1.45 | 1.3 | 1.5 | 1.6 | 1.75 |

### Master

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `advanced.starvationMechanics` | Enables the extra starvation punishment bundle that vanilla Master adds on top of the normal hunger system. It | CFG | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |
| `advanced.radiationMechanics` | Enables the additional Master radiation penalty rather than changing radiation accumulation or radiation damag | CFG | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |
| `advanced.fatigueMechanics` | Enables the Master-only severe-fatigue penalty bundle. This is separate from ordinary sleepiness build-up, aim | CFG | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |

### Survival

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `survival.wellFedStaminaRegen` | Changes the stamina-recovery bonus you get while well fed. Vanilla gives +20%. | CFG | 0.29 | 0.26 | 0.29 | 0.27 | 0.22 | 0.22 | 0.26 | 0.34 | 0.34 | 0.34 | 0.39 | 0.4 |

### Master

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `survival.starvationStaminaDrain` | Adds extra stamina cost to stamina-consuming actions while the Master starvation penalty is active. Vanilla ad | CFG | 1.1 | 1.15 | 1.1 | 1.25 | 1.2 | 1.45 | 1.15 | 1.35 | 1.35 | 1.45 | 1.65 | 1.85 |
| `survival.starvationDamage` | The Master starvation bundle applies a dedicated Hunger damage effect with a vanilla value of 4 per charge. | CFG | 2 | 2 | 2 | 3 | 2 | 2 | 2 | 3 | 3 | 3 | 5 | 6 |
| `survival.starvationDamageInterval` | Vanilla schedules the starvation damage charge every 10 seconds while the effect remains active. | CFG | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 10 | 7 | 5 |
| `survival.starvationPenaltyDelay` | All three vanilla starvation penalties use a 3-second delay. This control keeps those delay values synchronize | CFG | 4 | 4 | 4 | 3 | 4 | 4 | 4 | 3 | 3 | 3 | 2 | 1 |
| `survival.starvationHealthRegenPenalty` | Vanilla Master applies a 100% health-recovery penalty while the starvation bundle is active. | CFG | 0.65 | 0.7 | 0.65 | 0.7 | 0.7 | 0.6 | 0.6 | 0.8 | 0.8 | 0.85 | 0.9 | 0.95 |

### Survival

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `survival.fatigueLowAimPenalty` | At the Low Sleepiness state, vanilla increases aiming time by 15%. | CFG | 0.15 | 0.2 | 0.15 | 0.2 | 0.15 | 0.15 | 0.2 | 0.25 | 0.2 | 0.25 | 0.2 | 0.25 |
| `survival.fatigueMediumAimPenalty` | Vanilla raises the aiming-time penalty to +25% in the medium fatigue state. | CFG | 0.3 | 0.3 | 0.25 | 0.3 | 0.25 | 0.25 | 0.3 | 0.4 | 0.35 | 0.45 | 0.4 | 0.45 |
| `survival.fatigueStrongAimPenalty` | Vanilla increases aiming time by +40% in the severe fatigue state. | CFG | 0.5 | 0.5 | 0.45 | 0.5 | 0.4 | 0.35 | 0.5 | 0.6 | 0.55 | 0.7 | 0.6 | 0.75 |
| `survival.fatigueMediumReloadPenalty` | Medium fatigue adds +15% to the reload-time effect in vanilla. | CFG | 0.2 | 0.15 | 0.15 | 0.2 | 0.15 | 0.15 | 0.2 | 0.25 | 0.2 | 0.25 | 0.2 | 0.25 |
| `survival.fatigueStrongReloadPenalty` | Controls how much severe fatigue slows your reloads. Vanilla severe fatigue adds 18.75% reload time. | CFG | 0.25 | 0.25 | 0.2 | 0.25 | 0.2 | 0.2 | 0.25 | 0.3 | 0.25 | 0.35 | 0.3 | 0.35 |
| `survival.sleepHealthRestore` | Controls the direct health restore you receive after normal sleep. The tool starts at 100 HP (1x), adjustable  | CFG | 50 | 40 | 75 | 45 | 60 | 70 | 45 | 40 | 35 | 30 | 30 | 20 |
| `survival.sleepStaminaRestore` | Normal sleep restores stamina with a vanilla +100% Stamina effect. | CFG | 0.9 | 0.9 | 0.9 | 0.85 | 1 | 1 | 0.9 | 0.8 | 0.8 | 0.8 | 0.7 | 0.6 |

### Master

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `survival.severeFatigueMaxStamina` | Vanilla Master reduces maximum stamina by 20% while its severe-fatigue bundle is active. | CFG | 0.1 | 0.1 | 0.1 | 0.15 | 0.1 | 0.1 | 0.1 | 0.2 | 0.2 | 0.2 | 0.3 | 0.35 |
| `survival.severeFatigueCarryWeight` | Vanilla Master applies a 20% loss to both carry-related fatigue effects. | CFG | 0.1 | 0.1 | 0.1 | 0.15 | 0.1 | 0.1 | 0.1 | 0.2 | 0.2 | 0.2 | 0.25 | 0.35 |
| `survival.severeFatigueRecoil` | Vanilla Master adds a +55% recoil effect while the severe-fatigue bundle is active. | CFG | 0.35 | 0.4 | 0.35 | 0.45 | 0.3 | 0.3 | 0.4 | 0.5 | 0.5 | 0.5 | 0.75 | 0.9 |
| `survival.severeFatigueAimSway` | Controls how much extra weapon sway Master severe fatigue adds while aiming. | CFG | 0.25 | 0.3 | 0.25 | 0.35 | 0.25 | 0.25 | 0.3 | 0.38 | 0.38 | 0.38 | 0.55 | 0.7 |
| `survival.severeFatigueDispersion` | Changes how much severe fatigue increases weapon spread. | CFG | 0.3 | 0.35 | 0.3 | 0.4 | 0.3 | 0.3 | 0.35 | 0.45 | 0.45 | 0.45 | 0.65 | 0.8 |
| `survival.radiationStaminaDrainPenalty` | Adds extra stamina cost to stamina-consuming actions during the Master heavy-radiation penalty. Vanilla adds + | CFG | 0.3 | 0.35 | 0.3 | 0.4 | 0.55 | 0.55 | 0.35 | 0.45 | 0.45 | 0.45 | 0.65 | 0.85 |

### Survival

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `survival.bleedingMediumThreshold` | Controls when Medium bleeding begins. | CFG | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 | 50 |
| `survival.bleedingStrongThreshold` | Controls when Severe bleeding begins. | CFG | 75 | 75 | 75 | 75 | 75 | 75 | 75 | 75 | 75 | 75 | 75 | 75 |
| `survival.bleedingLowDamage` | Changes HP loss while low bleeding is active. | CFG | 1.1 | 1.2 | 1.1 | 1.2 | 1 | 1 | 1.2 | 1.2 | 1.2 | 1.2 | 1.3 | 1.5 |
| `survival.bleedingMediumDamage` | Controls the health damage dealt by medium bleeding each time its damage tick occurs. Vanilla uses 1.5 damage. | CFG | 1.7 | 1.9 | 1.7 | 2 | 1.7 | 1.7 | 1.9 | 2 | 2 | 2 | 2.2 | 2.5 |
| `survival.bleedingStrongDamage` | Controls the health damage dealt by severe bleeding each time its damage tick occurs. Vanilla uses 2 damage. | CFG | 2.3 | 2.6 | 2.3 | 2.8 | 2.4 | 2.4 | 2.6 | 2.7 | 2.7 | 2.7 | 3 | 3.5 |

### Master

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `advanced.masterEffectModifierMechanics` | Turns on the extra consumable penalties used by vanilla Master. This includes the separate Bandage, Medkit and | CFG | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |

### Environment

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `environment.electroAnomalyDamage` | Set the damage of each supported anomaly independently. Global Anomaly Damage still scales all damaging anomal | CFG | 72 | 72 | 80 | 68 | 81 | 72 | 72 | 88 | 75 | 89 | 72 | 78 |
| `environment.chemicalAnomalyDamage` | Fine-tunes Chemical damage without changing the other anomalies. Global Anomaly Damage still affects it too. | CFG | 33 | 33 | 37 | 32 | 38 | 34 | 33 | 40 | 34 | 41 | 33 | 36 |
| `environment.fireAnomalyDamage` | Fine-tunes Fire damage without changing the other anomalies. Global Anomaly Damage still affects it too. | CFG | 66 | 66 | 73 | 63 | 75 | 66 | 66 | 81 | 69 | 82 | 66 | 72 |
| `environment.carouselAnomalyDamage` | Fine-tunes Carousel damage without changing the other anomalies. Global Anomaly Damage still affects it too. | CFG | 116 | 116 | 129 | 110 | 131 | 116 | 116 | 142 | 121 | 144 | 116 | 126 |
| `environment.toxicCloudDamage` | Fine-tunes Toxic Cloud damage without changing the other anomalies. Global Anomaly Damage still affects it too | CFG | 28 | 28 | 31 | 26 | 31 | 27 | 28 | 34 | 29 | 35 | 28 | 30 |
| `environment.steamAnomalyDamage` | Fine-tunes Steam damage without changing the other anomalies. Global Anomaly Damage still affects it too. | CFG | 66 | 66 | 73 | 63 | 75 | 66 | 66 | 81 | 69 | 82 | 66 | 72 |
| `environment.flycatcherMovementSpeed` | Shows the Flycatcher movement effect as the speed you keep, rather than exposing the game's negative velocity- | CFG | 0.55 | 0.65 | 0.55 | 0.65 | 0.85 | 0.85 | 0.65 | 0.65 | 0.65 | 0.65 | 0.75 | 0.85 |
| `environment.flycatcherTurnSpeed` | Changes how much turning speed you keep while Flycatcher slows you down. | CFG | 0.35 | 0.45 | 0.35 | 0.45 | 0.65 | 0.65 | 0.45 | 0.45 | 0.45 | 0.45 | 0.55 | 0.65 |
| `environment.clickerHealthDamage` | Exposes only the Clicker's clean direct-damage effect. Equipment corrosion, flash behavior, radius, charge tim | CFG | 77 | 77 | 85 | 74 | 88 | 78 | 77 | 94 | 80 | 95 | 77 | 84 |
| `environment.razorHealthDamage` | Changes Glass Shards health damage while preserving its different low-speed and high-speed hit strengths. | CFG | 1.1 | 1.1 | 1.22 | 1.05 | 1.25 | 1.11 | 1.1 | 1.35 | 1.15 | 1.37 | 1.1 | 1.2 |
| `environment.razorBleeding` | Keeps Glass Shards bleeding independent from direct health damage and equipment corrosion. | CFG | 1.2 | 1.3 | 1.33 | 1.4 | 1.6 | 1.42 | 1.3 | 1.64 | 1.4 | 1.67 | 1.55 | 1.8 |
| `environment.soapBubbleHealthDamage` | Exposes the clearly separated direct health-damage effect without turning the full Soap Bubble behavior into o | CFG | 110 | 110 | 122 | 105 | 125 | 111 | 110 | 135 | 115 | 137 | 110 | 120 |
| `environment.fireballHealthDamage` | Exposes only the confirmed direct health-damage effect. | CFG | 110 | 110 | 122 | 105 | 125 | 111 | 110 | 135 | 115 | 137 | 110 | 120 |
| `environment.diamondHealthDamage` | Keeps Diamond direct damage separate from its bleeding effect. | CFG | 16 | 16 | 18 | 16 | 19 | 17 | 16 | 20 | 17 | 20 | 16 | 18 |
| `environment.diamondBleeding` | Exposes the confirmed bleeding layer independently from direct Diamond damage. | CFG | 12 | 13 | 13.5 | 14 | 16 | 14 | 13 | 16.5 | 14 | 16.5 | 15 | 18 |
| `environment.psyAnomalyIntensity` | Controls how strongly Psy anomalies affect you without exposing confusing behind-the-scenes numbers. | CFG | 1.2 | 1.15 | 1.1 | 1.45 | 1.65 | 1.5 | 1.2 | 1.4 | 1.3 | 1.55 | 1.65 | 1.8 |

### Loot

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `loot.mutant.blindDogChance` | Set the part-drop chance for each mutant family. A successful drop gives one part; this does not change mutant | CFG | 10% | 10% | 10% | 20% | 10% | 10% | 5% | 10% | 5% | 5% | 5% | 5% |
| `loot.mutant.bloodsuckerChance` | Controls how often an eligible Bloodsucker kill drops its mutant part. When it drops, you get 1 part. | CFG | 45% | 40% | 45% | 75% | 45% | 45% | 40% | 50% | 45% | 30% | 25% | 20% |
| `loot.mutant.boarChance` | Controls how often an eligible Boar kill drops its mutant part. When it drops, you get 1 part. | CFG | 15% | 15% | 15% | 25% | 15% | 15% | 10% | 10% | 10% | 5% | 5% | 5% |
| `loot.mutant.burerChance` | Controls how often an eligible Burer kill drops its mutant part. When it drops, you get 1 part. | CFG | 70% | 55% | 65% | 85% | 55% | 55% | 50% | 50% | 40% | 25% | 25% | 10% |
| `loot.mutant.catChance` | Controls how often an eligible Bayun kill drops its mutant part. When it drops, you get 1 part. | CFG | 65% | 55% | 60% | 80% | 50% | 50% | 45% | 45% | 35% | 20% | 20% | 10% |
| `loot.mutant.chimeraChance` | Controls how often an eligible Chimera kill drops its mutant part. When it drops, you get 1 part. | CFG | 60% | 55% | 50% | 90% | 50% | 55% | 60% | 75% | 60% | 35% | 30% | 25% |
| `loot.mutant.controllerChance` | Controls how often an eligible Controller kill drops its mutant part. When it drops, you get 1 part. | CFG | 60% | 55% | 55% | 80% | 50% | 50% | 45% | 45% | 35% | 20% | 20% | 10% |
| `loot.mutant.deerChance` | Controls how often an eligible Deer kill drops its mutant part. When it drops, you get 1 part. | CFG | 65% | 55% | 60% | 80% | 55% | 55% | 45% | 50% | 40% | 25% | 25% | 10% |
| `loot.mutant.fleshChance` | Controls how often an eligible Flesh kill drops its mutant part. When it drops, you get 1 part. | CFG | 15% | 15% | 15% | 25% | 15% | 15% | 10% | 10% | 10% | 5% | 5% | 5% |
| `loot.mutant.poltergeistChance` | Controls how often an eligible Poltergeist kill drops its mutant part. When it drops, you get 1 part. | CFG | 45% | 40% | 40% | 65% | 35% | 40% | 35% | 35% | 25% | 15% | 15% | 5% |
| `loot.mutant.pseudodogChance` | Controls how often an eligible Pseudodog kill drops its mutant part. When it drops, you get 1 part. | CFG | 65% | 55% | 60% | 80% | 50% | 50% | 45% | 45% | 35% | 20% | 20% | 10% |
| `loot.mutant.pseudogiantChance` | Controls how often an eligible Pseudogiant kill drops its mutant part. When it drops, you get 1 part. | CFG | 70% | 55% | 65% | 85% | 55% | 55% | 50% | 50% | 40% | 25% | 25% | 10% |
| `loot.mutant.tushkanChance` | Controls how often an eligible Tushkan kill drops its mutant part. When it drops, you get 1 part. | CFG | 15% | 15% | 15% | 25% | 15% | 15% | 10% | 10% | 10% | 5% | 5% | 5% |
| `loot.mutant.snorkChance` | Controls how often an eligible Snork kill drops its mutant part. When it drops, you get 1 part. | CFG | 10% | 5% | 5% | 10% | 5% | 5% | 5% | 5% | 5% | 5% | 5% | 5% |

### Equipment

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `equipment.weaponWearPerShot` | Changes how quickly player weapons lose condition when fired, while keeping each weapon's own vanilla wear rat | CFG | 1.15 | 1.35 | 1.05 | 1.3 | 1.22 | 1.13 | 1 | 1.25 | 1.28 | 1.3 | 1.35 | 1.45 |

### Loot

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `loot.worldAmmoCratesRemoved` | Choose how much ordinary world loot to remove from each category when the mod is built. 0% keeps vanilla place | ? | 0.55 | 0.62 | 0.65 | 0.6 | 0.66 | 0.66 | 0.52 | 0.7 | 0.68 | 0.8 | 0.79 | 0.83 |
| `loot.worldF1GrenadeBoxes` | Controls physical F1 box abundance directly using the same removed-percentage convention as the other world bo | ? | 0.35 | 0.42 | 0.4 | 0.38 | 0.42 | 0.42 | 0.42 | 0.5 | 0.55 | 0.64 | 0.62 | 0.78 |
| `loot.worldFoodDrinkCratesRemoved` | Controls physical food/drink box presence directly without a separate enable checkbox or shared percentage con | ? | 0.47 | 0.55 | 0.5 | 0.52 | 0.53 | 0.46 | 0.64 | 0.58 | 0.58 | 0.7 | 0.69 | 0.74 |
| `loot.worldMedicalCratesRemoved` | Controls physical medical box presence directly without a separate enable checkbox or shared percentage contro | ? | 0.56 | 0.62 | 0.25 | 0.58 | 0.62 | 0.56 | 0.61 | 0.68 | 0.71 | 0.76 | 0.75 | 0.82 |
| `loot.looseWorldLootRemoved` | Reduces vanilla world generosity without changing stashes, boxes, NPC loot, traders, weapons/armor or scripted | ? | 0.56 | 0.58 | 0.5 | 0.62 | 0.61 | 0.58 | 0.61 | 0.7 | 0.7 | 0.76 | 0.76 | 0.81 |
| `loot.looseUniqueWeaponShuffle` | Move source-verified loose unique weapons between known pickup locations without removing them. | ? | OFF | OFF | OFF | OFF | ON | ON | OFF | ON | ON | OFF | OFF | OFF |
| `loot.worldWeaponShuffle` | Allows weapon locations to be refreshed and balanced without mixing pistols with primary slots or jumping more | ? | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |
| `loot.worldWeaponDensity` | The slider controls actual world weapon density while keeping all 36 regular models represented and progressiv | ? | 0.6 | 0.55 | 0.6 | 0.5 | 0.6 | 0.6 | 0.6 | 0.45 | 0.4 | 0.3 | 0.35 | 0.25 |
| `loot.worldAttachmentShuffle` | Refreshes world attachment locations without changing the total safe-world attachment population or mixing inc | ? | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |
| `loot.worldDetectors` | Provides an opt-in removal switch for world detectors without touching detector progression domains that are q | ? | OFF | OFF | OFF | OFF | OFF | OFF | OFF | ON | ON | ON | ON | ON |
| `loot.blueprintShuffle` | Refreshes blueprint discovery without changing blueprint count, container count, blueprint definitions, quest  | ? | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON | ON |

### Alife

| Source ID | Native setting intent | Evidence | IMM | TAC | MED | HUN | ANO | WAN | MER | SMU | SCA | OUT | HC | HC+ |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `alife.humanGroupSize.newbie` | Set the maximum human squad size separately for Newbie, Experienced, Veteran, and Master player ranks. This do | CFG | 1 | 1 | 1 | 0.85 | 0.8 | 0.8 | 1.1 | 0.85 | 0.7 | 1 | 1.2 | 1.4 |
| `alife.humanGroupSize.experienced` | Use the radar to tune human squad size separately for each player rank. | CFG | 1 | 1.15 | 1 | 0.9 | 0.85 | 0.9 | 1.2 | 0.95 | 0.8 | 1.1 | 1.25 | 1.5 |
| `alife.humanGroupSize.veteran` | Use the radar to tune human squad size separately for each player rank. | CFG | 1 | 1.45 | 1 | 1 | 0.85 | 0.75 | 1.45 | 1.05 | 0.85 | 1.1 | 1.35 | 1.5 |
| `alife.humanGroupSize.master` | Use the radar to tune human squad size separately for each player rank. | CFG | 1 | 1.5 | 1.05 | 1 | 0.95 | 1 | 1.4 | 1.1 | 0.9 | 1.25 | 1.4 | 1.75 |
| `alife.mutantSpawn.generic` | Kept hidden so the tool does not pretend there is one proven master slider for every mutant group. | CFG | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 |
| `alife.mutantSpawn.bloodsucker` | Set how large each mutant species group can become while keeping its vanilla rank progression. This does not c | CFG | 0.95 | 0.9 | 0.85 | 1.15 | 0.7 | 0.8 | 0.95 | 0.9 | 0.85 | 1.1 | 1.2 | 1.4 |
| `alife.mutantSpawn.blindDog` | Change how large Blind Dog groups can become while keeping the vanilla rank progression. | CFG | 1 | 0.85 | 0.85 | 1.25 | 1.15 | 1.15 | 1 | 0.95 | 1.1 | 1.2 | 1.25 | 1.3 |
| `alife.mutantSpawn.flesh` | Change how large Flesh groups can become while keeping the vanilla rank progression. | CFG | 1.05 | 1.05 | 1.05 | 1.35 | 0.9 | 1.1 | 1 | 1.1 | 1.1 | 1.35 | 1.35 | 1.65 |
| `alife.mutantSpawn.controller` | Change how large Controller groups can become while keeping the vanilla rank progression. | CFG | 1 | 0.95 | 0.9 | 1.15 | 0.7 | 0.9 | 1 | 0.95 | 0.85 | 1.2 | 1.2 | 1.45 |
| `alife.mutantSpawn.poltergeist` | Change how large Poltergeist groups can become while keeping the vanilla rank progression. | CFG | 1 | 0.95 | 0.9 | 1.15 | 0.7 | 0.9 | 1 | 0.95 | 0.85 | 1.2 | 1.2 | 1.45 |
| `alife.mutantSpawn.snork` | Change how large Snork groups can become while keeping the vanilla rank progression. | CFG | 1.05 | 1.05 | 1.05 | 1.35 | 0.9 | 1.1 | 1 | 1.1 | 1.1 | 1.35 | 1.35 | 1.65 |
| `alife.mutantSpawn.boar` | Change how large Boar groups can become while keeping the vanilla rank progression. | CFG | 1.05 | 1.05 | 1.05 | 1.35 | 0.9 | 1.1 | 1 | 1.1 | 1.1 | 1.35 | 1.35 | 1.65 |
| `alife.mutantSpawn.chimera` | Change how large Chimera groups can become while keeping the vanilla rank progression. | CFG | 0.95 | 0.85 | 0.85 | 1.15 | 0.8 | 0.75 | 0.9 | 0.9 | 1 | 1.1 | 1.1 | 1.05 |
| `alife.mutantSpawn.bayun` | Change how large Bayun groups can become while keeping the vanilla rank progression. | CFG | 1 | 0.95 | 0.9 | 1.15 | 0.7 | 0.9 | 1 | 0.95 | 0.85 | 1.2 | 1.2 | 1.45 |
| `alife.mutantSpawn.tushkan` | Change how large Tushkan groups can become while keeping the vanilla rank progression. | CFG | 1.05 | 1.05 | 1.05 | 1.35 | 0.9 | 1.1 | 1 | 1.1 | 1.1 | 1.35 | 1.35 | 1.65 |
| `alife.mutantSpawn.ratSwarm` | Change how large Rat Swarm groups can become while keeping the vanilla rank progression. | CFG | 1.05 | 1.05 | 1.05 | 1.35 | 0.9 | 1.1 | 1 | 1.1 | 1.1 | 1.35 | 1.35 | 1.65 |
| `alife.mutantSpawn.burer` | Change how large Burer groups can become while keeping the vanilla rank progression. | CFG | 0.95 | 0.9 | 0.85 | 1.15 | 0.7 | 0.8 | 0.95 | 0.9 | 0.85 | 1.1 | 1.2 | 1.4 |
| `alife.mutantSpawn.pseudodog` | Change how large Pseudodog groups can become while keeping the vanilla rank progression. | CFG | 1.05 | 1.05 | 1.05 | 1.35 | 0.9 | 1.1 | 1 | 1.1 | 1.1 | 1.35 | 1.35 | 1.65 |
| `alife.mutantSpawn.deer` | Change how large Deer groups can become while keeping the vanilla rank progression. | CFG | 1.05 | 1.05 | 1.05 | 1.35 | 0.9 | 1.1 | 1 | 1.1 | 1.1 | 1.35 | 1.35 | 1.65 |

## Recorded reasoning for all 2,976 decisions

The human-readable matrix above is an index, not an invented aggregate difficulty rating. Each native setting has (1) an explicit source-specific purpose and target prototypes/operations, (2) the actual value and active base-game comparison for each of the 12 roles, (3) a role-aware reason that ties that exact source to one of ten authored role strategies, and (4) an evidence status. The complete per-setting/per-role ledger is `tools/factory_preset_full_source_audit.json`, checked by `tools/audit_project_consistency.py` against the embedded presets and the unchanged 248-ID catalog.

### Fixed shared values are deliberate

Shared hunger thresholds **17 / 33 / 83**, fatigue thresholds **50 / 60 / 80**, bleeding thresholds **50 / 75**, sprint disable/recovery bounds and equipment-shuffle switches are held constant to avoid incoherent ladders or quest-sensitive changes. `health.baseRegen=0` and food HP healing OFF preserve finite medicine. The optional NPC bonus loot source stays empty. Master survival/fatigue/medical bundles stay enabled and their separately mapped penalties remain role-defined. Hidden/unverified `survival.drunknessThresholdScale`, `economy.repairDifficultyCost` and generic A-Life mutant spawn remain neutral at **1×**; claiming they work as additional role knobs would be unsupported.

### Coupled-source audit boundaries

- **Food cadence vs scarcity:** slower hunger buildup is retained; low world food supply does not imply frequent eating. Food remains nourishment, never free medical recovery.
- **Medicine vs infection/exposure:** treat the known CFG effect duration separately from its magnitude and item availability. Field Medic's distinct `medicalOverride` must be restored before bleeding treatment values.
- **Water/Energy:** main durations must include every shorter child effect; preset override is restored before importing extended values. CFG prototype effect types and unknown stacking are unchanged.
- **Rare mutant trophies:** each `loot.mutant.*Chance` uses the UI's 5-percentage-point snap behavior; values that formerly rounded to zero now have a positive, source-backed minimum where a nonzero drop is intended. This does not imply every corpse drops a trophy.
- **Resource acquisition:** world placements, stashes, containers, NPC drops and trader stock are independent proven target surfaces; no single slider claims a whole-world loot percentage.
- **Hardcore recovery:** even Hardcore+ retains positive health treatment, sleep HP and a nonzero trader ammunition option; severe injury cannot be equated to a guaranteed hard lock.
- **Survival/physics:** values marked runtime-pending require live observation before statements about precise effectiveness, encounter counts or buffs.

## Acceptance criteria

1. Run `python tools/audit_project_consistency.py` and all existing source/native contract checks.
2. `./publish.sh` and Wine-load every factory role, inspect modified amounts, 5-second drink ticks and preserved ON override controls; save and reload a profile to confirm no numeric drift.
3. Confirm only byte-identical previous factory files migrate, while a modified `Hardcore+.json` and custom filename remain unchanged.
4. In game, benchmark hunger/fatigue cadence, item use per expedition, medicine and radiation recovery, species encounter survivability, trophy availability and resource loops for each role.

**This audit verifies source contracts and proposed authoring. It is not a claim that .NET/Wine or STALKER 2 runtime tests have been performed.**
