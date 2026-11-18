# MUTANT AGGRESSION — attack and ability knobs, SPECIAL ABILITY RATE (2026-11-16, knobs 2026-11-18)

Two control groups on the Mutant board (*MUTANT DAMAGE, HEALTH AND AGGRESSION*):

| Control | Ids | Range | Vanilla |
|---|---|---|---|
| One knob per move (53: 37 attacks, 16 abilities) | `combat.mutantMoveRate.{family}.{move}` | 0.25×–3×, step 0.25 | 1×; **2× on Master** for every move the game halves there (e.g. Bayun claw, Controller psy, Pseudogiant shockwave) |
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
  ThrowGrenade, WeaponDrag, WeaponRiseAndShoot, ActivateAnomaly, ShockwaveAttack (Pseudogiant); Pseudodog: Howl,
  CombatSummon, RetreatSummon (vanilla 1× on every difficulty; 2026-11-18 fix, the old psi-wave tag did nothing).
  Summons belong to their mutant's ABILITIES: Bayun clones, Deer blind dogs (new Deer ABILITIES slider) and the
  Deer controller's dogs (`Controller_Deer` uses `CombatSummonAbility_Deer`). Boar, Deer, Flesh and Tushkan
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

## CFG writer check (2026-11-18)

`tools/audit_mutant_cooldown_cfg.py` (pre-release gate; with `--game <GameData>` or `TCD_BASE_DUMP` it also reads
the game files) re-implements the knob writer and renders the same PatchNode shape:

```
Stalker : struct.begin {bpatch}
    MutantCombatDifficulty : struct.begin {bpatch}
        AgentCooldownMultipliers : struct.begin          // no {bpatch}: the whole list is replaced
            [0] : struct.begin
                AgentPrototypeSID = Bayun
                CooldownMultipliers : struct.begin
                    [0] : struct.begin
                        CooldownMultiplier = 0.5
                        CooldownTag = Ability.Cooldown.ClawAttack
                    struct.end
                    ...
```

* Master, every knob at vanilla: our list equals the game's own 14-agent list (same tags, same 0.5), so the
  full-list replacement loses nothing (roars, growls, shields keep their 0.5).
* One changed knob changes only its tag, on every agent of the family (e.g. Controller PSY 3× → 1/3 on Controller,
  Controller_Collar, Controller_Deer); Novice–Veteran at vanilla write nothing.
* All 38 agent SIDs exist in ObjPrototypes and all 27 tags in AbilityPrototypes; the render parses back balanced
  with `[0..n]` indices. DLC1 has no DifficultyPrototypes override.
* In-game effect is still `runtime_test_required` (the engine merge of an inherited `AgentCooldownMultipliers =`
  on Easy / Medium / Hard follows the same pattern as the existing Mutant_BaseDamage edits there).

## Gamer rationales; Controller DOGS removed (2026-11-18, fifth pass)

* Every knob's panel rationale says in player words what the move is and how to read it (e.g. psy strike: stay in
  its view and it fries your head), then its vanilla seconds and Master value.
* `Controller_Deer` is a one-off map variant (no CFG spawns it; only `Statistics.cfg` maps it to Controller) that
  carries the Deer dog summon. Regular Controllers never call dogs, so the Controller DOGS knob is gone; that
  summon stays vanilla. 53 knobs remain.

## No group sliders (2026-11-18, fourth pass)

* The ATTACKS sliders are gone as well: slider and knobs did the same thing. Every attack and ability is one knob,
  a one-tag group in the writer with its own Master vanilla (`KnobReference`: 2× on Master where any agent of the
  family has the tag in the game's Master list).
* Presets folded each ATTACKS slider into its attack knobs the same way as the ABILITIES pass
  (`tools/factory_preset_attack_knobs_2026_11_18.json`).
* Attack tags that never had a knob (e.g. Bayun short / long leap, Deer run, Mist Bloodsucker bite, the Pseudodog
  no-op shockwave) keep the game's own entry.
* Board: under the health track, `ATTACKS` and `ABILITIES` captions over one five-column knob grid; card 140 px.

## No ABILITIES sliders (2026-11-18, third pass)

* The ABILITIES sliders are gone: each ability is its own knob and a one-tag group in the writer
  (`Multiplier(agent, tag, knob, KnobReference)`), so its knob carries the Master vanilla (2× where the game
  lists the tag on Master). Attacks keep the ATTACKS slider × knob.
* Presets folded the old slider into each ability knob: knob × slider, or knob × its Master 2× when the slider sat
  at its vanilla (`tools/factory_preset_ability_knobs_2026_11_18.json`).
* Board: ATTACKS caption and slider across the card; one five-column knob grid below (attacks left, abilities
  right-aligned under an ABILITIES header with a divider). Knobs 18 px radius; caption 9.4 pt, seconds 11 pt bold.

## Attacks only (2026-11-18, second pass)

* Only moves that hurt or bring enemies have controls. Roars, growls, howls, dodges, Bayun teleports and shields
  are in no slider and have no knob; the writer writes the game's own Master 0.5 back for them, so they stay vanilla.
* Checked against the CFG: the regular Blind Dog never shields (`ShieldActionData Enabled = false`; only
  `Blinddog_Collar` has it), Chimera never short-leaps (`ShortJumpAttackData Enabled = false`), and Pseudogiant
  throws only as the electrocollar variant (`ThrowActionData` enabled on `Pseudogiant_Collar` only).
* Removed: Blind Dog, Chimera and Snork ABILITIES sliders (nothing left); 14 knobs. 54 attack knobs remain:
  ATTACKS 37, ABILITIES 17 (Bayun clones; Bloodsucker anomaly; Burer grenades, disarm, throw, weapons;
  Controller psy, corpse, zombify, dogs; Deer dogs; Poltergeist anomaly, throw; Pseudodog phantoms, retreat;
  Pseudogiant shockwave, throw).
* Bayun and Bloodsucker ABILITIES are now 1× on Master too (their halved Master moves were a growl,
  teleport or roar). Stock Hardcore stores them at 1×.
* With `TCD_BASE_DUMP`, the audit checks every knob's move is in an agent's ability list of its family.

## One knob per move (2026-11-18)

* Under each ATTACKS / ABILITIES slider sits one knob per move (one per cooldown tag of the family), 68 in all:
  `combat.mutantMoveRate.{family}.{move}`, 0.25–3× step 0.25, vanilla 1×. The list, captions and vanilla seconds
  are `MutantAggressionPolicy.Moves` (main agent, base CFG; checked against `AbilityPrototypes.cfg` by the audit).
* Writer: per agent and tag the multiplier is **group × 1/knob** (group = the game's Master 0.5 at its vanilla,
  else 1/rate). Tags that end at exactly 1 are not written. Same full `AgentCooldownMultipliers` list.
* Moves with no cooldown in the CFG (Chimera claw and dodge, 0 s) get no knob.
* The global SUMMON RATE of the same morning is retired: each summon is a knob of its own mutant
  (Pseudodog PHANTOMS 20 s and RETREAT 10 s, Bayun CLONES 10 s, Deer DOGS 20 s, Controller DOGS 20 s).
  The preset values it had moved onto those knobs.
* Board: species cards are 146 px with a knob row (54 px) under each half. A knob shows a short CAPS caption and
  the move's real wait in seconds (`2.5 s`); drag up / right, wheel, arrow keys, right-click or R reset.

## Real seconds on the board and in the panel (2026-11-18)

* Slider halves carry no value text (the knobs show every move); panel values are seconds (`claw every 2.5 s`), never a × factor.
* The panel's **COOLDOWNS** block lists every move the selected knob reaches, in seconds at its current value
  (`MutantAggressionPolicy.Moves`: slider × knob). Only changed numbers turn gold.
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
