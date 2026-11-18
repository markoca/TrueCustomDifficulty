# MUTANT AGGRESSION — ATTACKS, ABILITIES and SPECIAL ABILITY RATE (2026-11-16)

Two control groups on the Mutant board (*MUTANT DAMAGE, HEALTH AND AGGRESSION*):

| Control | Ids | Range | Vanilla |
|---|---|---|---|
| ATTACKS per family (14) | `combat.mutantAttackRate.{family}` | 0.25×–3×, step 0.25 | 1×; **2× on Master** for Bayun, Bloodsucker, Burer, Chimera, Controller, Deer, Pseudodog, Pseudogiant |
| ABILITIES per family (10) | `combat.mutantAbilityRate.{family}` | 0.25×–3×, step 0.25 | 1×; **2× on Master** for Bayun, Bloodsucker, Burer, Chimera, Controller, Pseudogiant |
| SUMMON RATE (global) | `combat.mutantSummonRate` | 0.25×–3×, step 0.25 | 1× on every difficulty |
| SPECIAL ABILITY RATE (global) | `combat.mutantSpecialAbilityRate` | 0.25×–3×, step 0.25 | 1× Novice / Stalker / Veteran, 2× Master |

All are `runtime_test_required`. Source: S.T.A.L.K.E.R. 2 2.1 base dump.

## Where the cooldowns are

* Real cooldowns: `AbilityPrototypes.cfg` → each ability's `TriggeredCooldowns` (`CooldownTag`, `Duration` in seconds),
  e.g. Controller psy attack 12 s, Burer shield 17 s, Pseudogiant shockwave 8 s, Bloodsucker claw 6 s.
* The difficulty multiplies them per agent and per tag:
  `DifficultyPrototypes.cfg → [difficulty].MutantCombatDifficulty.AgentCooldownMultipliers`
  (`{ AgentPrototypeSID, CooldownMultipliers[{ CooldownTag, CooldownMultiplier }] }`).
* Vanilla: only Master (SID `Stalker`) has entries — 14 agents at **0.5**, but each lists only part of its tags
  (e.g. Pseudogiant: shockwave, run, charge; not claw, roar or throw).

## ATTACKS / ABILITIES

* Each family's tags are collected from its abilities in `AbilityPrototypes.cfg` (own and triggered cooldowns, all
  variant prefixes such as `Cat_` for Bayun, `AshyBloodsucker_`, `HelmedBoar_`, `BoggyFlesh_`), plus the vanilla
  Master tags. Navigation/passive tags (`Jump`, `EscapeIsolatedNavmesh`, `Abilities.Passive.Aura`) and the boar
  `Grunt` sound are left out.
* **ATTACKS**: ClawAttack, BiteAttack, BiteAttackAnyAngle, RunAttack, JumpAttack, Short/LongJumpAttack, KickAttack,
  TurnAttack, Charge, JumpToEnemy, FlyThrough, Evasion, AOEAttack (Poltergeist burst). Pseudodog ATTACKS also carry
  `ShockwaveAttack`: the game's Master list names it for PseudoDog, but no pseudodog ability uses it (no-op).
* **ABILITIES**: Abilities.PSYAttack / RaiseDeadBody / ZombifyNPC, Roar, Growl, BlinkTeleport, Shield, Throw,
  ThrowGrenade, WeaponDrag, WeaponRiseAndShoot, ActivateAnomaly, ShockwaveAttack (Pseudogiant); Pseudodog: Howl
  (vanilla 1× on every difficulty; 2026-11-18 fix, the old psi-wave tag did nothing; its phantoms are SUMMON RATE). Boar, Deer, Flesh and Tushkan
  have none, so they have no ABILITIES track.
* Agents (exact SIDs, 38): every base-game variant of the family — e.g. Bloodsucker, MistBloodsucker,
  Bloodsucker_Collar, AshyBloodsucker, PrologueBloodsucker; Poltergeist + Electro/Toxic/Fire/YanivToxicRozliv/
  Explosives; PseudoDog + its two phantom summons; HelmedBoar, PorcupineBoar, MoldyBlinddog, BoggyFlesh.
  DLC1 quest/boss agents (`Pseudogiant_NoCooldowns`, `DLC01_*_Poltergeist`, `JadedChimera`) are not listed.
* Rate *r* writes multiplier **1/r** on every tag of the group for every agent. The writer replaces the whole list
  (non-bpatch) as soon as any rate differs from its vanilla: a group at its vanilla keeps the game's own Master
  entries (0.5, same order); a group at 1× writes nothing.
* The card shows one real cooldown per group (`2x · psy 6 s`): claw/bite/kick/leap/stomp/burst for ATTACKS,
  psy/shield/teleport/anomaly/growl/shockwave/summon for ABILITIES. Chimera growl is 60 s and Snork growl 120 s
  (2026-11-18 fix; the card used to say 30 s and 20 s). At the vanilla value it shows the game's effective time (e.g. Master Controller psy 6 s).
* `Mutant_AttackCooldown` (1.0 on all difficulties) is not touched.

## SUMMON RATE → the same `AgentCooldownMultipliers` list (2026-11-18)

* Rate *r* writes **1/r** on the summon tags of the summoning agents, next to their ATTACKS / ABILITIES entries:
  `PseudoDog` (CombatSummon 20 s: 5 phantoms; RetreatSummon 10 s: 4 phantoms), `Bayun`, `Bayun_Collar`,
  `Bayun_E10_MQ01` (CombatSummon 10 s: 5 clones), `Deer`, `Controller_Deer` (CombatSummon 20 s: 3 blind dogs).
* No difficulty lists a summon tag, so vanilla is 1× everywhere and 1× writes nothing.
* Story `Faust_CombatSummonAbility` and the DLC1 Jaded Chimera summon are not touched. Counts per summon stay vanilla.
* Board: the second global row is split, SPECIAL ABILITY RATE left and SUMMON RATE right
  (`phantoms 20 s · clones 10 s · dogs 20 s`).

## Real seconds on the board and in the panel (2026-11-18)

* Card halves and panel values show the time between two uses in seconds (`claw every 2.5 s`), never a × factor.
* The panel's **COOLDOWNS** block lists every move the selected knob reaches, in seconds at its current value
  (`MutantAggressionPolicy.Moves`, `SummonTimes`). Only changed numbers turn gold.
* Values come from `MUTANT-COOLDOWNS.md` (main agent, base CFG). With `TCD_BASE_DUMP` set, the audit checks each
  second against `AbilityPrototypes.cfg`.

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
* UI value: panel `2x · special move every 4–6 s`; card `special move every 4–6 s`.
* Since the HUMAN TACTICS controls, one writer (`WriteCombatSynchronization`) writes the rank tables for SPECIAL ABILITY RATE and the five tactics together (source × reference / rate); see `HUMAN-TACTICS-AND-AWARENESS.md`.

## Not done

* PACK PRESSURE (how many mutants attack at once — other FilterGroups / token counts) is not
  exposed; optional follow-up.
* Families, agents and tags vanilla never lists on Master need the in-game check in `RELEASE-TESTING.md`.

## Checks

* `tools/audit_mutant_aggression.py` — policy ↔ catalog ↔ writer ↔ embedded tables ↔ baseline ↔ UI.
  `TCD_BASE_DUMP=<dump>/base` also checks the vanilla Master lists (order), every agent SID and every tag against the dump.
* Factory presets do not store these ids; they load at the difficulty's vanilla.

**Readout (2026-11-17):** species card = wait between two uses of the headline move (`gore every 10 s`); panel = rate + wait (`1x · gore every 10 s`).
