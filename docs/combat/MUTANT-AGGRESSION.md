# MUTANT AGGRESSION — ATTACK RATE and SPECIAL ABILITY RATE (2026-11-16)

Two control groups on the Mutant board (*MUTANT DAMAGE, HEALTH AND AGGRESSION*):

| Control | Ids | Range | Vanilla |
|---|---|---|---|
| ATTACK RATE per family (14) | `combat.mutantAttackRate.{family}` | 0.25×–3×, step 0.25 | 1× everywhere; **2× on Master** for Bayun, Bloodsucker, Burer, Chimera, Controller, Deer, Pseudodog, Pseudogiant |
| SPECIAL ABILITY RATE (global) | `combat.mutantSpecialAbilityRate` | 0.5×–3×, step 0.25 | 1× Novice / Stalker / Veteran, 2× Master |

Both are `runtime_test_required`. Source: S.T.A.L.K.E.R. 2 2.1 base dump.

## ATTACK RATE → `AgentCooldownMultipliers`

`GameData/DifficultyPrototypes.cfg → [difficulty].MutantCombatDifficulty.AgentCooldownMultipliers`
is a list of `{ AgentPrototypeSID, CooldownMultipliers[{ CooldownTag, CooldownMultiplier }] }`.

* Vanilla: only Master (SID `Stalker`) has entries — 14 agent SIDs at **0.5** (Bayun, Bayun_Collar,
  Bayun_Collar_Clone, Bayun_E10_MQ01, Bloodsucker, MistBloodsucker, Burer, Burer_ShootingSpecial,
  Chimera, Controller, Controller_Collar, Deer, PseudoDog, Pseudogiant). Novice / Stalker / Veteran
  have none.
* Matching is by **exact agent SID**, so `MutantAggressionPolicy` lists every variant of a family
  (25 SIDs; e.g. `Bloodsucker_Collar`, `Controller_Deer`, `Pseudogiant_Collar`, `Snork_Collar`,
  `Blinddog_Collar`). Tags are the family's `Ability.Cooldown.*` / `Abilities.*` tags from
  `AbilityPrototypes.cfg` (vanilla Burer lists `WeaponDrag` twice; kept as authored).
* Rate *r* writes multiplier **1/r** on every tag of every variant (2× = half the cooldown).
* The writer replaces the whole list (non-bpatch) as soon as any family differs from its vanilla:
  families still at vanilla keep their exact vanilla entries (Master 0.5, otherwise nothing);
  a family at 1× writes nothing.
* `Mutant_AttackCooldown` (1.0 on all difficulties) is not touched.

## SPECIAL ABILITY RATE → `CombatSynchronizationPrototypes`

`GameData/CombatSynchronizationPrototypes.cfg`: `CombatSynchronizerEasy/Medium/Hard` inherit
`Default` (no own ranks); `CombatSynchronizerStalker` owns its ranks. In every rank
(Newbie / Experienced / Veteran / Master) the group-token filters
`FilterGroups[3]` (`TokenTag.Ability`) and `FilterGroups[4]` (`TokenTag.Ability.Knockdown`) set how
long the fight waits before another mutant may use a special / knockdown move:
Default **8–12 s**, Master **4–6 s** (= 2×).

* Rate *r* writes `CooldownMin = 8/r`, `CooldownMax = 12/r` for [3] and [4].
* Because Easy/Medium/Hard have no own ranks, the writer copies the full rank tables
  (embedded `Data/combat-synchronization-2.1.json`: Default and CombatSynchronizerStalker) into
  `CombatSynchronizer{difficulty}` and changes only those two groups. The loader guards the tags.
* UI value: `2x · pause 4–6 s`.

## Not done

* PACK PRESSURE (how many mutants attack at once — other FilterGroups / token counts) is not
  exposed; optional follow-up.
* Six families (Blind Dog, Boar, Flesh, Poltergeist, Snork, Tushkan) are never listed by vanilla;
  that the engine honours their entries needs the in-game check in `RELEASE-TESTING.md`.

## Checks

* `tools/audit_mutant_aggression.py` — policy ↔ catalog ↔ writer ↔ embedded tables ↔ baseline ↔ UI.
  `TCD_BASE_DUMP=<dump>/base` also compares the vanilla agent list with the dump.
* Factory presets do not store these ids; they load at the difficulty's vanilla.
