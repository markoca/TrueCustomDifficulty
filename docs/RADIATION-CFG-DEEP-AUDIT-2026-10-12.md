# Full radiation CFG source audit — 2026-10-12

**Evidence level:** decoded original GameData / DLC1 archives; source-proven values, not an in-game HP timeline. This audit makes no UI or writer changes.

## Source provenance and scope

| Source | Exact archive SHA-256 | Files |
|---|---|---:|
| STALKER2-GameData-CFG(9).zip | 937b3fb4c74d1311b6e052c010aa0795073ccdc60d2ca6d8b5228ee3ce0b6083 | 485 |
| STALKER2-DLC1-CFG(6).zip | 93ccb9bcc9043f7c171d439b2c25c38f61311506bc33c0b926a16f526ccd6114 | 118 |

Archive-wide decoded CFG scan: **603 files, 30,427 radiation-related matching lines across 39 files**. This includes many repeated SpawnActorPrototypes and QuestNodePrototypes references, so the number is *not* an editable-parameter count. Verified direct files: GameData/CoreVariables.cfg, EffectPrototypes.cfg, ObjPrototypes.cfg, DifficultyPrototypes.cfg, ItemPrototypes.cfg, UpgradePrototypes.cfg, ObjEffectMaxParamsPrototypes.cfg, ObjSleepParamsPrototypes.cfg, DamageSourcePrototypes.cfg, PostEffectProcessorPrototypes.cfg, plus DLC1 analogues. Cross-checked with current parameters.json / cfg-writer-baseline.json.

## Confirmed normal player radiation stages

**Source:** GameData/EffectPrototypes.cfg **4298–4595**, RadiationMechanics: VitalBarType Radiation, comparison GreaterOrEqual; Player applies RadiationMechanics at GameData/ObjPrototypes.cfg **736**. These are RAD thresholds, not timestamps. Every damaging stage attaches its own original HP damage prototype and uses RemoveOnTransition=true for its damage effect.

| Stage | Start at RAD ≥ | Damage prototype | Original ValueMin / ValueMax | Other effect |
|---|---:|---|---:|---|
| None | 0 | None | — | — |
| Visual onset | 0.001 | None | — | Screen/post effect only |
| Light HP damage | 1 | LightRadiationDamage | 0.2 | Post effect |
| Medium | 10 | MediumRadiationDamage | 0.3 | DegenStamina1 = −0.5; RadiationNoSleep |
| Strong | 20 | StrongRadiationDamage | 0.48 | DegenStamina2 = −1; NoSleep |
| Level 4 | 30 | RadiationLevel4Damage | 0.816 | DegenStamina2; NoSleep |
| Level 5 | 40 | RadiationLevel5Damage | 1.468 | DegenStamina2; NoSleep |
| Level 6 | 50 | RadiationLevel6Damage | 2.2 | DegenStamina2; NoSleep |
| Level 7 | 60 | RadiationLevel7Damage | 4.18 | DegenStamina2; NoSleep |
| Level 8 | 70 | RadiationLevel8Damage | 8.37 | DegenStamina2; NoSleep |
| Level 9 | 80 | RadiationLevel9Damage | 17.58 | DegenStamina2; NoSleep |
| Level 10 | 90 | RadiationLevel10Damage | 38.67 | DegenStamina2; NoSleep |
| Deadly | 99 | DeadlyRadiationDamage | **20** | DegenStamina3 = −2; NoSleep |

The unusual **Level 10 = 38.67 > Deadly = 20** is *in the original source*. Do not invent monotonic damage curves. There are **11 damaging prototypes**, **22 ValueMin/ValueMax leaves**, which all agree with the existing source-scaling writer's baseline. Original damage prototypes all have Charges=1, bIsPermanent=true, bIsSmooth=false; Strong, levels 4–10 and Deadly author Duration=1. Light and Medium have no explicit Duration. Except Deadly, normal stages say ShouldIgnoreArmor=true; Deadly says false.

**Cadence distinction:** damage prototypes do not individually author TimePerChargeMin/Max, but the **root [0] effect template** in EffectPrototypes.cfg **1–52** sets BOTH to **1.f**. These are *inherited schema defaults*. Without verifying runtime permanence/repeat handling, this does **not** prove one HP hit per second or guaranteed death time.

## Radiation field exposure parameters

**Source:** GameData/CoreVariables.cfg **611–679**. These are field emission presets with authored RadioactivityValue, RadiationPerSecondValue, and Geiger intensity.

| ERadiationPreset | RadioactivityValue | RadiationPerSecondValue | Geiger intensity |
|---|---:|---:|---:|
| Light | 15 | **1/s** | 0.4 |
| Medium | 45 | **3/s** | 0.6 |
| Strong | 75 | **6/s** | 0.8 |
| Deadly | 99 | **33/s** | 0.9 |
| RadBlock | 100 | **50/s** | 1.0 |
| Custom | 1 | **1/s** | 0.1 |
| Topaz | 15 | **1/s** | 0.2 |

Separate CoreVariables fields include ObjRadiationSphereMinRadius=1, MaxRadius=500, RadiationInnerOffsetPresetValues Full=0.01 / Custom=1, postprocess/Geiger intensity. Actor instances in GameData/SpawnActorPrototypes.cfg and DLC1 choose individual radiation presets, GeigerTriggerOffset and custom field placements. Do not interpret their display-intensity values as universal RAD/s.

### Exceptional direct-damage hazard: RadBlockFieldDamage

**Source:** GameData/EffectPrototypes.cfg **23359–23375**, referenced by CoreVariables Deadly and RadBlock presets: ValueMin=ValueMax **9000**, Charges=1, explicit **TimePerChargeMin=TimePerChargeMax=2.7 seconds**, bIsPermanent=true, ArmorPiercing=5, ShouldIgnoreArmor=true. This is a separate field-attached damage effect, **not** ordinary DeadlyRadiationDamage=20 and **not** one of the 11 prototypes scaled by the app's radiationDamage setting. Treat as a separate danger category, not level 11 of the normal radiation progression.

## Player values, difficulty, and Master effects

| Exact original field / effect | Original value | Existing UI ownership | Interpretation |
|---|---|---|---|
| Player.VitalParams.MaxRadiation | **100** | None | Max RAD bar; NOT max HP |
| Player.VitalParams.DegenRadiation | **0.05 RAD/s** | survival.radiationRecovery | Independent passive radiation removal |
| Difficulty EnvironmentDifficulty.Radiation_AccumulationSpeed | **Novice 0.6 / Stalker 1.0 / Veteran 1.5 / Master 1.85** | survival.radiationAccumulation | Difficulty scalar, not absolute exposure dose |
| 11 Radiation damage ValueMin/ValueMax pairs | Values in table | survival.radiationDamage | 0–2× multiplier on **22 writer targets**, not threshold/cadence/field editing |
| RadiationNegativeMechanics condition | **50 RAD** | advanced.radiationMechanics | Additional Master-only stamina penalty bundle |
| RadiationNegativeStaminaPenalty ValueMin/ValueMax | **+50% SP drain** | survival.radiationStaminaDrainPenalty | Separate from DegenStamina1/2/3 above |

Player-specific values are in GameData/ObjPrototypes.cfg about **657–737**; difficulty values are in DifficultyPrototypes.cfg, with Master AdditionalMechanicsEffects around **664–680**; Master mechanics and the 50-RAD condition are in EffectPrototypes.cfg **25248–25302**. NPCs and creatures also have MaxRadiation and DegenRadiation fields, often NOT equal to the player's settings; editing those globally would not preserve player-only behavior.

## Treatment, armor, artefacts, and other real radiation fields

**Full top-level EffectPrototypes.cfg type inventory:** **86 EEffectType::Radiation**, **102 EEffectType::ProtectionRadiation**, **28 EEffectType::DegenRadiation**, for **216 separate typed effect definitions**. These are mostly *per-item or per-armor* and should **not** all be merged into a single global difficulty multiplier.

| Treatment prototype | Original radiation delta |
|---|---:|
| Antirad4 (Anti-Rad) | −100 |
| EcoMedkitAntirad3 (Scientific Medkit) | −60 |
| BeerAntirad1 (Beer) | −10 |
| VodkaAntirad3 (Vodka) | −50 |
| SleepRadiationRestore | −100% |
| Artifact_WeirdKettle_BeerAntirad | −20 |
| Artifact_WeirdKettle_VodkaAntirad | −80 |
| Artifact_WeirdKettle_LimitedEnergeticAntirad | −100 |

Most consumable anti-rad prototypes above author Duration=2 and Charges=10; that does NOT imply removal occurs as one instant HP-damage pulse. Weird Kettle versions are **separate** from default beer/vodka effects. Additional original EEffectType::Radiation variants include VolumeRadiationAdd/Remove, quest effects and armor-specific signed radiation absorption.

**Radiation-protection families:** ProtectionRadiation1–9 = **5–45%**; ProtectionAbsoluteRadiation1–9 = **5–45 absolute source values**, not the same unit; armor and helmet-specific protection/absorption values; ObjEffectMaxParamsPrototypes.cfg ProtectionRadiation MaxValue=**85**. EEffectType::DegenRadiation includes AddRadiation1–9, DegenRadiation1–9, ArtifactProtectionRadiation1–4, ArtifactAddRadiation1–4, AddDegenRadiation=**1.5** and further effects. Source prefixes are sometimes counterintuitive; verify signs and actual item effect ownership rather than claiming every "Add" increases RAD.

**Items/equipment:** base GameData/ItemPrototypes.cfg has **170 literal Radiation= fields** and DLC1 ItemPrototypes.cfg has **18**; these are attached to individual item/armor prototypes. GameData/UpgradePrototypes.cfg has many radiation-absorption and protection upgrade variants. Player and NPC values are distinct (base ObjPrototypes.cfg 98 MaxRadiation and 98 DegenRadiation fields; DLC1 90 each), so no bulk replacement without domain scoping. RadiationNoSleep and sleep restore also occur in ObjSleepParamsPrototypes.cfg.

## DLC1, world, quests and audiovisual references

- DLC1/EffectPrototypes.cfg **does not override** generic RadiationMechanics or DeadlyRadiationDamage (its other DLC effects may inherit the base prototypes). DLC1 world spawns, item and armor radiation values and quest scripts nevertheless exist.
- Base and DLC1 SpawnActorPrototypes.cfg: thousands of placed radiation fields using ERadiationPreset, GeigerTriggerOffset, environmental effects and unique actors. These are *instances*; don't expose each as a generic difficulty slider.
- Base and DLC1 QuestNodePrototypes.cfg: many IgnoreRadiationFeilds flags (original typo), quest-specific fields and script conditions. Dialogue also tests radiation. These must stay separate from global stages.
- PostEffectProcessorPrototypes.cfg, PostProcessParamPrototypes.cfg, FloatProviderPrototypes.cfg, DamageSourcePrototypes.cfg and Wwise fields control Geiger, display intensity, audible/visual radiation response—not HP tick rates.

## Can we now provide an accurate "RADIATION KILLS IN" timeline?

**Only an explicitly conditional source-model estimate, not a verified in-game countdown.** We now know state thresholds, field presets, eleven amounts, inherited 1-second charge configuration and RadBlock's explicit 2.7s hazard. Still unverified:

1. Real effective exposure rate = field rate × difficulty scalar after player protection, distance/falloff, resistance, inner offset and active items; no proven direct net RAD/s formula for every situation.
2. Whether permanent one-charge damage effects actually repeat each second in the engine, their first-charge offset and transitions / simultaneous effects.
3. HP regeneration, radiation removal, armor damage exceptions (Deadly ShouldIgnoreArmor=false), additional Master stamina mechanics, and RadBlock's 9000 direct damage overlapping other effects.
4. Effective death timing under player motion/armor/cure usage; a status threshold is not a clock tick.

**UI decision for this audit:** keep the current three source-native radiation knobs, plus the eleven-stage read-only damage-values graph. The audited stage thresholds could later be added as source-exact annotations, with user approval, but do not silently introduce a "time to die" formula or new editable radiation sliders without real gameplay verification. No app UI, CFG writer, source IDs or preset behavior changed in this audit.

## Direct source line index

| Original CFG evidence | Lines |
|---|---|
| EffectPrototypes.cfg RadiationMechanics thresholds / attached effects | 4298–4595 |
| EffectPrototypes.cfg Light / Medium / Strong / Deadly damage | 4596–4657 |
| EffectPrototypes.cfg stages 4–10 | 24334–24438 |
| EffectPrototypes.cfg root [0] TimePerCharge defaults | 1–52 |
| CoreVariables.cfg RadiationPresetValues | 611–679 |
| EffectPrototypes.cfg RadBlockFieldDamage | 23359–23375 |
| ObjPrototypes.cfg Player vitals & mechanics membership | 657–737 |
| DifficultyPrototypes.cfg Radiation accumulation scalar | 45,117,182,240,664 |
| EffectPrototypes.cfg RadiationNegativeMechanics | 25248–25302 |
| EffectPrototypes.cfg Anti-Rad / Beer / Vodka / EcoMedkit | 558–570;5820–5845;8094–8110 |

**Limit:** decoded CFG content is proven. Unreal's effect-system execution, exact source stacking, runtime tick semantics and final time-to-death remain **UNVERIFIED**.
