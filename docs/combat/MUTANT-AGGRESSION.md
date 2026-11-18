# MUTANT AGGRESSION — move knobs and SPECIAL MOVES (current state, 2026-11-18)

Controls on the Mutant board (*MUTANT DAMAGE, HEALTH AND AGGRESSION*):

| Control | Ids | Range | Vanilla |
|---|---|---|---|
| One knob per move (53: 37 attacks, 16 abilities) | `combat.mutantMoveRate.{family}.{move}` | 0.25×–3×, step 0.25 | 1×; **2× on Master** for every move the game halves there (e.g. Bayun claw, Controller psy, Pseudogiant shockwave) |
| SPECIAL MOVES (global) | `combat.mutantSpecialAbilityRate` | 0.25×–3×, step 0.25 | 1× Novice / Stalker / Veteran, 2× Master |

All are `runtime_test_required`. Source: S.T.A.L.K.E.R. 2 2.1 base + DLC1 dumps. Real seconds per move:
`MUTANT-COOLDOWNS.md` (generated). Lever catalog and history of choices: `MUTANT-ABILITIES-CATALOG.md`.

## Where the cooldowns are

* `AbilityPrototypes.cfg` → each ability's own `CooldownTag` and `TriggeredCooldowns.Duration` (seconds).
* `DifficultyPrototypes.cfg → [difficulty].MutantCombatDifficulty.AgentCooldownMultipliers`
  (`{ AgentPrototypeSID, CooldownMultipliers[{ CooldownTag, CooldownMultiplier }] }`) multiplies them per agent and
  tag. Vanilla: only Master (SID `Stalker`) has entries, 14 agents at **0.5** on part of their tags.

## Knobs (`MutantAggressionPolicy.Moves`)

* One knob per attack or ability that hurts or brings enemies, one per cooldown tag of the family, for all
  variants of the family (38 agent SIDs; DLC1 bosses and quest-only agents are not listed).
* Not controlled, kept exactly vanilla: roars, growls, howls, alert sounds, dodges, Bayun teleport, shields,
  Chimera short leap (`ShortJumpAttackData Enabled = false`), moves at 0 s (Chimera claw), and the one-off
  `Controller_Deer` dog summon. The writer writes their Master 0.5 back.
* Summons are knobs of their own mutant: Bayun CLONES, Deer DOGS, Pseudodog PHANTOMS and RETREAT.
* Writer: knob *k* is a one-tag group with its own vanilla (`KnobReference`: 2× on Master where any agent of the
  family lists the tag): at its vanilla the game's entry stays, else `1/k` on that tag for every agent of the
  family. The whole list is replaced (non-bpatch); tags that end at 1 are not written.
* Board: species card 140 px, `ATTACKS` / `ABILITIES` captions, one five-column knob grid (abilities right-aligned,
  thin divider), 18 px knobs, CAPS caption and the real wait in seconds. Panel COOLDOWNS block shows the move.

## SPECIAL MOVES → `CombatSynchronizationPrototypes`

* `FilterGroups[3]` (`TokenTag.Ability`) and `[4]` (`TokenTag.Ability.Knockdown`): one special move at a time for the
  whole fight, then **8–12 s** (Master **4–6 s**). Growls / roars, shields and knockdown attacks use them.
* Rate *r* writes `CooldownMin = 8/r`, `CooldownMax = 12/r`. Easy / Medium / Hard inherit `Default`, so the writer
  copies the full rank tables (`Data/combat-synchronization-2.1.json`) and changes only these groups; one writer
  (`WriteCombatSynchronization`) also serves the five HUMAN TACTICS controls.
* Knockdown knobs (ObjPrototypes action synced on `TokenTag.Ability.Knockdown`: Bloodsucker LEAP, Boar / Deer /
  Pseudogiant CHARGE, Pseudogiant WAVE) may not beat that pause: `KnobCap` = ⌊seconds × SPECIAL ÷ 8 s⌋ on the 0.25
  grid, never below the knob's vanilla. MainViewModel clamps, the writer caps, the board draws a stop tick.

## Presets

Every profile stores all 53 knobs and SPECIAL MOVES. Earlier group sliders (ATTACKS / ABILITIES, SUMMON RATE)
were folded into the knobs (`tools/factory_preset_*_2026_11_18.json`); untouched older stock files upgrade by
blob hash in `PresetDirectoryService`.

## Checks

* `tools/audit_mutant_aggression.py` — policy ↔ catalog ↔ writer ↔ embedded tables ↔ baseline ↔ UI; with
  `TCD_BASE_DUMP` (+ `TCD_DLC_DUMP`) also the vanilla Master lists, every SID and tag, every knob's seconds, that each
  knob's move exists on an agent of its family, and the knockdown set.
* `tools/audit_mutant_cooldown_cfg.py` — re-derives the written list: Master at vanilla equals the game's own list,
  one knob changes only its tag, SIDs and tags exist, the render parses back.

## Not done

* Pack size / how many mutants attack at once (other token groups) is not exposed.
* In-game check of tags the game never lists on Master: `RELEASE-TESTING.md`.
