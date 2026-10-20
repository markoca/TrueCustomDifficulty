# Combat Mapping

## 2026-10-20 — CURRENT Combat control retirement (supersedes earlier Attack Frequency / Accuracy UI locks)

**Retired, not replaced:** `combat.npcAttackCooldown` (Enemy Attack Rate / Attack Frequency) and `combat.npcHipAccuracy` (Enemy Shot Accuracy / Fire Accuracy) are no longer part of the playable control catalog, the Human Combat aggregate, any of the twelve shipped presets, or the production CFG writer baseline. These two scalar fields do not represent the full enemy attack-timing and aiming behavior systems; no invented one-knob proxy replaces them. Previously saved presets containing these retired IDs load with those keys ignored and omit them on resave. No generated CFG edit targets `NPCCombatDifficulty.NPC_AttackCooldown` or `NPCCombatDifficulty.HipAccuracyMultiplier` via these retired settings.

**Active Human Enemies editor:** independent X/Y **ENEMY HP** and **ENEMY GUN DAMAGE** plot with exterior axis handles and an interactive joint, plus **one** compact **EXPLOSION DAMAGE** knob. Use the existing 270° knob grammar, neutral references, number-only gold modified values and the same wide/narrow responsive layout. Remove every inactive three-knob/fader/Attack Rate/Accuracy drawing, dependency, help hint and preset migration. Do not stretch the surviving knob to fill the empty bank. All other combat source targets, the Stamina Usage layout conventions, and hover/reset behavior stay unchanged.

**Current authority:** **285** catalog/writer parameters, **180** player-visible controls (**15** Combat), **105** hidden; **12** shipped factory presets with **285** values each; **1230** direct CFG baseline edit points / **215** direct source IDs. The 2026-10-19 copy ledger is reduced to 180 current rows, without rewriting the historical wording decisions for other controls. Previous dated sections below remain design history wherever they conflict with this retirement.


> **Current inventory:** This document describes the original difficulty-layer Combat map. The 2026-10-09 deep audit covers **30 active Combat parameters**, including 14 mutant-health dimensions / 16 nested `MaxHP` edit points. See [Phase 3 Combat audit](DEEP-CFG-AUDIT-PHASE3-COMBAT-2026-10-09.md) for current catalog/writer checks, special-writer boundaries and open runtime tests; the historical sections below remain as source semantics, not current UI counts.


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

`NPC_AttackCooldown` and `HipAccuracyMultiplier` remain documented here as original game-source fields, but neither is an active True Custom Difficulty control as of 2026-10-20. Enemy attack pacing and accuracy depend on deeper gameplay systems and are not represented by these two simple dials.

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

The editable **Your Headshot Damage** control covers **0.5×–4×**, in **0.25×** increments (15 knob ticks), independently of the vanilla values listed above. The maximum 4× is a UI editing limit and writes the existing `NPCCombatDifficulty.PlayerWeapon_HeadshotMultiplier` field without introducing an additional damage layer.

These remain independent controls because vanilla itself treats base player weapon damage, headshot scaling and jamming as separate layers. The UI names `Weapon_JammingMultiplier` **Weapon Jamming Frequency**: higher values mean more frequent jams; weapon condition and per-weapon jam thresholds remain separate mechanics.

## Mutant global combat

`MutantCombatDifficulty` contains:

| Field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `Mutant_BaseDamage` | 0.35 | 1.00 | 1.35 | 1.50 |

Mutant Hit Damage is the only active global mutant-combat scalar. Mutant Health is a separate source-backed 14-species editor; both write existing gameplay fields without affecting human-enemy attack pacing.

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
