# Combat Mapping

Source of truth: runtime `Stalker2/Content/GameLite/GameData/DifficultyPrototypes.cfg` from the supplied STALKER 2 2.0.5 CFG dump.

This document records only combat controls that map to fields already present in the vanilla difficulty structures. It deliberately does not pull low-level weapon, perception, recoil, dispersion or AI-behavior tuning from unrelated CFGs into the difficulty editor.

## Game Difficulty mapping

| Internal SID | UI name |
|---|---|
| `Easy` | Novice |
| `Medium` | Stalker |
| `Hard` | Veteran |
| `Stalker` | Master |

## Human enemies

`NPCCombatDifficulty` contains the principal human-combat difficulty layer.

| Field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `NPC_HP` | 1.00 | 1.20 | 1.00 | 1.50 |
| `NPC_Weapon_BaseDamage` | 0.17 | 0.85 | 1.02 | 1.35 |
| `NPC_AttackCooldown` | 1.00 | 1.00 | 1.00 | 1.00 |
| `HipAccuracyMultiplier` | 1.00 | 1.00 | 1.00 | 1.00 |

`NPC_AttackCooldown` and `HipAccuracyMultiplier` are valid fields in every vanilla profile even though vanilla leaves them neutral. They are therefore useful custom-difficulty controls, but they should not be described as vanilla difficulty differences. The UI calls `HipAccuracyMultiplier` **Human Enemy Fire Accuracy** for readability. Starting with v0.1.96, the cooldown field is presented as **Human Attack Frequency**: the UI frequency multiplier is inverted when written (`raw cooldown = vanilla cooldown / UI frequency`) so moving the slider upward always means more frequent attacks.

## Accumulated incoming-damage handling

The same `NPCCombatDifficulty` block contains:

| Field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `AccumulatedDamageReductionCurveWeightMax` | 1.00 | 0.75 | 0.50 | 0.40 |
| `AccumulatedDamageReductionCurveWeightMin` | 1.00 | 0.33 | 0.15 | 0.12 |
| `AccumulatedDamageReductionIncludesHealedHealth` | false | false | true | true |
| `AccumulatedDamageReductionCurveWeightMaxDistance` | 1600 | 1600 | 1600 | 1600 |

The CFG proves the weights and their vanilla direction. It does **not** contain the engine formula for interpolation/evaluation of the curve. The application therefore exposes the two weights and healed-health flag, but does not claim that either number is a direct final-damage percentage.

Vanilla always keeps `CurveWeightMin <= CurveWeightMax`. The current UI exposes them as separate controls; users should preserve that relationship unless deliberately testing non-vanilla curve shapes.

`AccumulatedDamageReductionCurveWeightMaxDistance` remains omitted because it is identical on every difficulty and its exact curve-distance semantics are not documented by the supplied CFG.

## Player lethality

The player-facing combat fields used by vanilla difficulty are split across the NPC and environment difficulty structs:

| Field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `EnvironmentDifficulty.Weapon_BaseDamage` | 1.30 | 1.00 | 0.75 | 0.75 |
| `NPCCombatDifficulty.PlayerWeapon_HeadshotMultiplier` | 1.00 | 1.00 | 1.00 | 2.00 |
| `NPCCombatDifficulty.Weapon_JammingMultiplier` | 1.00 | 1.00 | 1.00 | 1.50 |

These remain independent controls because vanilla itself treats base player weapon damage, headshot scaling and jamming as separate layers. The UI names `Weapon_JammingMultiplier` **Weapon Jamming Frequency**: higher values mean more frequent jams; weapon condition and per-weapon jam thresholds remain separate mechanics.

## Mutant global combat

`MutantCombatDifficulty` contains:

| Field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `Mutant_BaseDamage` | 0.35 | 1.00 | 1.35 | 1.50 |
| `Mutant_AttackCooldown` | 1.00 | 1.00 | 1.00 | 1.00 |

The global attack-cooldown field is neutral on all vanilla profiles. The UI presents it as **Mutant Attack Frequency** and writes the reciprocal multiplier so higher UI values mean more frequent general attacks. Master separately adds species/agent-specific ability cooldown overrides.

## Master mutant ability cooldown overrides

Only Master contains a populated `MutantCombatDifficulty.AgentCooldownMultipliers` block. All mapped entries use `CooldownMultiplier = 0.5` for their listed abilities.

The tool groups related agent variants under one player-facing control while retaining the actual target SIDs:

| UI control | Master agent SIDs | Mapped ability tags |
|---|---|---|
| Bloodsucker / Mist Bloodsucker | `Bloodsucker`, `MistBloodsucker` | RunAttack, JumpAttack, ClawAttack, Roar; Mist variant also BiteAttack |
| Chimera | `Chimera` | RunAttack, Growl, ClawAttack, ShortJumpAttack, LongJumpAttack, FlyThrough |
| Deer | `Deer` | RunAttack, JumpAttack, Charge |
| Pseudogiant | `Pseudogiant` | ShockwaveAttack, RunAttack, Charge |
| Pseudodog | `PseudoDog` | ShockwaveAttack, RunAttack, BiteAttack |
| Burer | `Burer`, `Burer_ShootingSpecial` | WeaponDrag, WeaponRiseAndShoot, Shield, Throw, ClawAttack |
| Bayun | `Bayun`, `Bayun_Collar`, `Bayun_Collar_Clone`, `Bayun_E10_MQ01` | ClawAttack, JumpAttack, Growl, JumpToEnemy, BlinkTeleport |
| Controller | `Controller`, `Controller_Collar` | ClawAttack, PSYAttack, Roar, RaiseDeadBody, ZombifyNPC |

Novice/Stalker/Veteran do not contain matching `AgentCooldownMultipliers` entries. The UI represents absence as neutral `100%`, while Master is `50%`.

**Runtime-test requirement:** final CFG generation must verify that creating these override entries for a non-Master base preset behaves exactly like the vanilla Master block. The existence and Master values are confirmed; non-Master insertion behavior is not.

## PSY combat overrides

Master also populates `NPCCombatDifficulty.PsyPhantomNPCOverrides` for both:

- `OverrideEffectPrototypeSID = SpawnPSYPhantoms`
- `OverrideEffectPrototypeSID = SpawnPSYNPC`

Both entries use:

| Field | Novice / Stalker / Veteran | Master |
|---|---:|---:|
| `PSYDelayBeforeNewSpawnCycleModifier` | no override → neutral 1.0 in UI | 0.5 |
| `PsyPhantomNPCCountMultiplier` | no override → neutral 1.0 in UI | 2.0 |
| `bPsyNPCApplyBleeding` | no override → Off in UI | true |
| `PsyNPCDurabilityDamageMultiplier` | no override → neutral 1.0 in UI | 2.0 |

The neutral lower-difficulty values are an explicit UI representation of **absence of an override**, not literal fields written in those vanilla profiles.

**Runtime-test requirement:** creating equivalent PSY override entries for Novice/Stalker/Veteran must be validated in game before those lower-baseline overrides can be called runtime-confirmed. Master uses the vanilla-provided PSY override array as its source structure.

## Explosion damage

`EnvironmentDifficulty.Explosion_BaseDamage` remains displayed in Combat because it is combat-facing damage even though the field physically resides in `EnvironmentDifficulty`:

- Novice: 0.25
- Stalker: 1.00
- Veteran: 1.00
- Master: 1.20

## Intentionally omitted

The following fields are not exposed in this pass:

- `NPC_Armor_Strike_Add`
- `NPC_Weapon_Rank_Add`
- other `Armor_Strike_Add`-style zeroed fields
- low-level weapon recoil/dispersion parameters
- AI perception/vision/hearing parameters
- ability damage values outside the difficulty struct

They either remain zero across vanilla profiles, have unclear semantics in the supplied CFG, or belong to a broader combat-overhaul editor rather than a difficulty-layer editor.
