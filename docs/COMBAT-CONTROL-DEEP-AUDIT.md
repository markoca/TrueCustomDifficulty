# Combat Control Deep Audit

> **Current deep CFG audit (2026-10-09):** This document is a historical v0.2.44 25-control snapshot, not the current Combat inventory. The active catalog has **39 Combat dimensions: 27 direct controls (68 source edit points) and 12 special runtime-gated controls**. The current source-to-writer findings, safety boundaries and reproduction commands are in [Phase 3 Combat audit](DEEP-CFG-AUDIT-PHASE3-COMBAT-2026-10-09.md). The 16 direct mutant HP routes were repaired in [Phase 2](DEEP-CFG-AUDIT-PHASE2-2026-10-09.md). Do not use the old visible-count or route-quality claims below as a current production test result.


Release: v0.2.44

Scope: all 25 normal UI controls in the `combat` tab. This audit traces each control from player-facing semantics through catalog metadata to the concrete `DifficultyPrototypes.cfg` target/operator. It does not promote new controls.

## Result

- 25/25 visible Combat controls audited.
- 12 controls are direct/confirmed difficulty-field mappings.
- 13 controls are structurally mapped but retain `runtime_test_required` because they create/use Master-only nested override structures outside their vanilla-present baseline.
- 0 controls removed.
- 0 writer-route defects found.
- 1 wording defect corrected: `combat.playerHeadshotMultiplier` no longer describes the whole multiplier as "extra damage"; `1.0` is explicitly neutral and the CFG field is a headshot damage multiplier.
- `combat.accumulatedDamageWeightMin <= combat.accumulatedDamageWeightMax` remains enforced on actual CFG values.

## Direct / confirmed controls

| ID | UI label | CFG target | Operator | Vanilla | Audit |
|---|---|---|---|---|---|
| `combat.npcHealth` | Human Enemy Health | `NPCCombatDifficulty.NPC_HP` | set | 1.00 / 1.20 / 1.00 / 1.50 | 1:1 |
| `combat.npcWeaponDamage` | Human Enemy Gun Damage | `NPCCombatDifficulty.NPC_Weapon_BaseDamage` | set | 0.17 / 0.85 / 1.02 / 1.35 | 1:1 |
| `combat.mutantDamage` | Mutant Hit Damage | `MutantCombatDifficulty.Mutant_BaseDamage` | set | 0.35 / 1.00 / 1.35 / 1.50 | 1:1 |
| `combat.playerWeaponDamage` | Your Weapon Damage | `EnvironmentDifficulty.Weapon_BaseDamage` | set | 1.30 / 1.00 / 0.75 / 0.75 | 1:1 |
| `combat.playerHeadshotMultiplier` | Your Headshot Damage | `NPCCombatDifficulty.PlayerWeapon_HeadshotMultiplier` | set | 1.00 / 1.00 / 1.00 / 2.00 | 1:1; wording fixed |
| `combat.weaponJamming` | Weapon Jamming Frequency | `NPCCombatDifficulty.Weapon_JammingMultiplier` | set | 1.00 / 1.00 / 1.00 / 1.50 | 1:1 difficulty multiplier |
| `combat.explosionDamage` | Explosion Damage | `EnvironmentDifficulty.Explosion_BaseDamage` | set | 0.25 / 1.00 / 1.00 / 1.20 | 1:1 |
| `combat.npcAttackCooldown` | Human Attack Frequency | `NPCCombatDifficulty.NPC_AttackCooldown` | inverseScale | 1.00 all | 1:1 field; reciprocal UI semantic |
| `combat.npcHipAccuracy` | Human Enemy Fire Accuracy | `NPCCombatDifficulty.HipAccuracyMultiplier` | set | 1.00 all | 1:1 exposed accuracy multiplier |
| `combat.accumulatedDamageWeightMax` | Repeated-Hit Protection — Maximum | `NPCCombatDifficulty.AccumulatedDamageReductionCurveWeightMax` | set | 1.00 / 0.75 / 0.50 / 0.40 | direct curve-weight control; formula not claimed |
| `combat.accumulatedDamageWeightMin` | Repeated-Hit Protection — Minimum | `NPCCombatDifficulty.AccumulatedDamageReductionCurveWeightMin` | set | 1.00 / 0.33 / 0.15 / 0.12 | direct curve-weight control; formula not claimed |
| `combat.accumulatedDamageIncludesHealedHealth` | Healed Damage Still Counts | `NPCCombatDifficulty.AccumulatedDamageReductionIncludesHealedHealth` | set | false / false / true / true | 1:1 boolean |
| `combat.mutantAttackCooldown` | Mutant Attack Frequency | `MutantCombatDifficulty.Mutant_AttackCooldown` | inverseScale | 1.00 all | 1:1 field; reciprocal UI semantic |

The count above is 13 direct controls. The release guard treats these as the confirmed/direct set; the remaining 12 are nested Master-derived override controls.

## Runtime-gated nested overrides

Eight mutant-family controls map to the exact Master `AgentCooldownMultipliers` agent/ability families: Bloodsucker/Mist Bloodsucker, Chimera, Deer, Pseudogiant, Pseudodog, Burer, Bayun and Controller. Master supplies `0.5`; lower vanilla profiles have no corresponding nested entries and are represented as neutral `1.0` in the UI. Their target operation remains `setAgentAbilityCooldownFamily` and their status remains `runtime_test_required`.

Four PSY controls map to both Master override entries (`SpawnPSYPhantoms` and `SpawnPSYNPC`): spawn-cycle delay, phantom count, bleeding and equipment durability damage. Master values are respectively `0.5`, `2.0`, `true`, `2.0`; lower profiles have no override and are represented by neutral UI values. Their operation remains `setOrCreateOverride` and status remains `runtime_test_required`.

No runtime-gated control was promoted to confirmed by this static audit.

## Direction and range review

- Direct damage/health/accuracy/jamming multipliers preserve raw CFG direction: larger UI value writes a larger CFG multiplier.
- Human and mutant attack-frequency controls intentionally invert the raw cooldown field. `100%` writes vanilla `1.0`; increasing frequency writes a smaller cooldown. The label therefore describes the player-facing effect rather than exposing a backwards cooldown slider.
- Species ability controls deliberately expose **cooldown**, not frequency: smaller percentage means faster ability reuse. No inversion is applied.
- Repeated-hit Minimum and Maximum are raw curve weights, displayed as `% of full scale`, not `% damage reduction`. Their exact engine interpolation formula remains outside the supplied CFG evidence.
- Current ranges include every vanilla value. Wider custom ranges are policy bounds, not claims about additional vanilla difficulty states.

## Hard boundaries retained

The audit does not promote `AccumulatedDamageReductionCurveWeightMaxDistance`, zeroed `*_Armor_Strike_Add` / `NPC_Weapon_Rank_Add` fields, low-level recoil/dispersion, perception, or ability-specific damage. Their semantics/scope remain insufficient for normal difficulty UI.


## Mutant Health + Ability Cooldown pairing (v0.9.26.44)

Eight confirmed mutant-health multipliers are now exposed in Combat and paired immediately with their existing ability-cooldown controls:

| Family | Health source | Paired cooldown |
|---|---|---|
| Bloodsucker / Mist Bloodsucker | `Bloodsucker.MaxHP`, `MistBloodsucker.MaxHP` | Bloodsucker Special Ability Cooldown |
| Chimera | `Chimera.MaxHP` | Chimera Special Ability Cooldown |
| Deer | `Deer.MaxHP` | Deer Special Ability Cooldown |
| Pseudogiant | `Pseudogiant.MaxHP` | Pseudogiant Special Ability Cooldown |
| Pseudodog | `PseudoDog.MaxHP` | Pseudodog Special Ability Cooldown |
| Burer | `Burer.MaxHP`, `Burer_ShootingSpecial.MaxHP` | Burer Special Ability Cooldown |
| Bayun | `Bayun.MaxHP` | Bayun Special Ability Cooldown |
| Controller | `Controller.MaxHP` | Controller Special Ability Cooldown |

Health is a direct multiplier of the authored `MaxHP`; it is not difficulty-scoped. Ability cooldowns keep their existing difficulty-aware behavior.
