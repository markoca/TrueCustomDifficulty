# Mutant abilities: catalog of controllable levers

Status 2026-11-18: attacks only. Every attack cooldown of sections 1–2 that a mutant owns as an attack, ability or summon now has its own knob under its slider (see MUTANT-AGGRESSION.md). Roars, growls, howls, dodges, teleports and shields stay vanilla by choice. Windows, area, counts and strength remain a **catalog only.** Nothing in this document is wired to the UI or writer yet,
except what section 1 lists as covered. It is the reference for choosing the next mutant controls.

Sources: `GameData/AbilityPrototypes.cfg` (base), `DLC1/AbilityPrototypes.cfg`,
`EffectPrototypes.cfg`, `DifficultyPrototypes.cfg` (`MutantCombatDifficulty.AgentCooldownMultipliers`),
`ObjPrototypes.cfg` mutant blocks. Times are in seconds. Distances are in cm (1000 = 10 m).

## 0. How the game controls an ability

| Lever | CFG location | What it does |
|---|---|---|
| Cooldown | `AbilityPrototypes` → `<Ability>.TriggeredCooldowns[n].Duration` for the ability's own `CooldownTag` | The minimum time between two uses by one mutant |
| Difficulty cooldown multiplier | `DifficultyPrototypes` → `MutantCombatDifficulty.AgentCooldownMultipliers` (per agent, per `CooldownTag`) | Multiplies the cooldown. Vanilla: only Master has entries, all 0.5 |
| Group token | `CombatSynchronization` token cooldowns (8–12 s; Master 4–6 s) | How often *any* mutant in a group may start a special ability |
| Windows and durations | Per-ability fields: `FocusDuration`, `PreparationTime`, `Lifetime`, fade times | How long the player has to react |
| Area | `Radius`, `EffectsRadius`, `DamageRadius`, `MaxDiscoveryRadius` | Reach of auras, blasts and alerts |
| Counts | `SummonParams.SummonObjects[]` entries, `MaxWeaponsToRise` | How many phantoms, clones, dogs or weapons |
| Strength | Effect values in `EffectPrototypes` (psy points per tick, DPS) | Damage or status build-up per hit or tick |

Rule: a cooldown change goes through the difficulty multiplier, never through the base `Duration`.
That keeps one writer, one per-family route, and a clean reset to vanilla.

## 1. Already covered (docs/combat/MUTANT-AGGRESSION.md)

* **ATTACKS** per family (14 families): claw, bite, run, jump, kick and charge cooldowns.
* **ABILITIES** per family (10 families): psy strike, shields, throws, shockwaves, roars, teleports,
  anomaly spawning, raise dead, zombify, weapon drag and weapon rise.
* **SPECIAL ABILITY RATE**: the group token cooldowns.

Fully covered by these three: Burer, Chimera, Controller, Poltergeist, Pseudogiant.

## 2. Cooldown tags not covered yet

These tags exist with their own cooldown but are in neither the ATTACKS nor the ABILITIES list.
They can join the existing multiplier writer with no new mechanism.

| Family | Tag | Vanilla cooldown | Meaning |
|---|---|---|---|
| Pseudodog | `CombatSummon` | 20 | Summons 5 phantom dogs mid-fight |
| Pseudodog | `RetreatSummon` | 10 | Summons 4 phantoms while fleeing |
| Pseudodog | `Howl` | 30 | Pack alert |
| Bayun | `CombatSummon` | 10 | Summons 5 clones |
| Bayun | `TrickSound.Mimicry` / `.Mockery` | 8 / 8 | Fake sounds that pull the player's attention |
| Bayun | `TrickSound.Ambush` | 20 | Ambush sound trick |
| Deer | `CombatSummon` | 20 | Calls 3 blind dogs |
| Blind dog | `Howl` | 50 | Pack alert |
| Flesh, Tushkan | `Squeek` | 40 | Alert squeal |
| Boar | `Grunt` | 120 | Alert grunt |
| Most families | `Alerted` | 2–3 | Reaction lock after noticing the player. **Do not expose**: it is AI plumbing, not an ability |

## 3. Candidate controls by type

### 3.1 Frequency (cooldowns)

* **SUMMONS** — one multiplier on every `CombatSummon` and `RetreatSummon` tag (Pseudodog, Bayun, Deer).
  This is the most visible missing lever.
* **PACK CALLS** — one multiplier on `Howl`, `Squeek`, `Grunt` and `TrickSound.*`.

### 3.2 Reaction windows

| Mutant | Field | Vanilla | Effect |
|---|---|---|---|
| Controller | `Controller_PSYStrike.FocusDuration` | 1.5 | Time to break line of sight before the psy strike lands |
| Bloodsucker | invisibility fade to visible / to invisible | 0.4 / 1.8 | How fast it cloaks and uncloaks |
| Bloodsucker | invisibility loss per damage | 0.01 | How much a hit reveals it |
| Bloodsucker, Poltergeist | anomaly `PreparationTime` / `Lifetime` | 5 / 7 | Warning before an anomaly arms and how long it lasts |
| Burer | `WeaponDrag` delay | 1 | Time before the disarm pulls the weapon |
| Bayun | `BlinkTeleport` delay | 0.7 | Teleport wind-up |
| Bayun | ambush end | 5–10 | How long the ambush waits |

### 3.3 Area

| Mutant | Field | Vanilla |
|---|---|---|
| Controller | `Controller_PSYAura.Radius` | 4000 (40 m) |
| Controller | `RaiseDeadBody.MaxDiscoveryRadius` / `ZombifyNPC` | 5000 / 4000 |
| Bayun | `Cat_SleepinessAura.Radius` | 4000 |
| Pseudogiant | shockwave radius | 1000 |
| Poltergeist | AOE `DamageRadius` | 300 |
| Bloodsucker | escape radius | 1500 |
| Roars and howls | `EffectsRadius` (alert reach) | 1000–3000 |

### 3.4 Counts

| Mutant | Field | Vanilla | Note |
|---|---|---|---|
| Pseudodog | combat summon phantoms | 5 | Array entries in `SummonParams.SummonObjects` |
| Pseudodog | retreat summon phantoms | 4 | Same |
| Bayun | clones | 5 | Same |
| Deer | blind dogs | 3 | Same |
| Burer | `Burer_WeaponRiseAndShoot.MaxWeaponsToRise` | 3 | Plain number |
| Poltergeist | throw shots | 1–2 | Plain numbers |

Fewer summons means removing array entries; more means cloning the last entry. Both are BPatch
array edits, so they carry more risk than a number edit.

### 3.5 Strength

| Mutant | Field | Vanilla |
|---|---|---|
| Controller | aura psy points per tick | +3 every 0.2 s |
| Controller | roar psy damage | 30 |
| Bayun | sleepiness per tick | 0.7–0.9 every 0.2 s |
| Poltergeist | AOE burst | 30 DPS |

Strength overlaps with the existing damage and status controls. Prefer the reaction-window and
area levers first.

## 4. Dependencies and risks

* **Shared tags.** `CombatSummon` is used by Pseudodog, Bayun, Deer and story agents (`Faust_CombatSummonAbility`).
  The difficulty multiplier is set per agent, so the story agent must be left out by name.
* **Stacking.** A summon or pack-call multiplier stacks with SPECIAL ABILITY RATE: the token decides
  *whether* the group may act, the cooldown decides *how often* one mutant may. Board text must say both apply.
* **Master already halves cooldowns.** Writing new tags into `AgentCooldownMultipliers` must keep
  Master's existing 0.5 entries and multiply them, the same way ATTACKS and ABILITIES do.
* **Excluded agents.** DLC1 bosses and quest variants: JadedChimera, Object68, the DLC01 Poltergeists,
  `Burer_WeaponRiseAndShoot_ShootingSpecial` (12 weapons), `Faust_*`, `Prologue*`.
* **Phantoms are separate prototypes.** `PseudoDogCombatSummon` / `PseudoDogRetreatSummon` have their own
  attacks (`PseudoDogSummon_*`). Mutant health and attack controls must not count them twice.
* **Radius limits.** Aura radii far above vanilla reach through walls and across a whole lair.
  Cap at 2× vanilla.
* **Reaction windows have a floor.** `FocusDuration` below 0.5 s or anomaly preparation below 2 s
  leave no time to react. Cap the minimum.

## 5. Recommended shortlist

1. **SUMMONS** (frequency): done 2026-11-18 as per-mutant knobs (PHANTOMS, RETREAT, CLONES, DOGS).
2. **PACK CALLS** (frequency): 0–3× multiplier on howl, squeal, grunt and sound tricks. Reuses the existing writer.
3. **PSY WARNING** (window): Controller `FocusDuration`, 0.5–3 s.
4. **BLOODSUCKER CLOAK** (window): fade-in and fade-out times as one 0.5–2× scale.
5. **ANOMALY WARNING** (window): Bloodsucker and Poltergeist anomaly preparation time, 2–10 s.
6. **AURA REACH** (area): Controller psy aura and Bayun sleepiness radius, 0.5–2×.
7. **SUMMON COUNT** (count): array edit; last, after 1–6 are stable.
