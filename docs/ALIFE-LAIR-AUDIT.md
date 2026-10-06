# ALIFE Lair / World-Population Audit

## Scope

This audit covers ALIFE controls outside the already implemented Director group-size radars.

Implemented today:
- **Human Group Size** — rank-specific Human `MaxCount` in `ALifeDirectorScenarioPrototypes.cfg`.
- **Mutant Group Size** — species/rank-specific mutant `MaxCount` in `ALifeDirectorScenarioPrototypes.cfg`.

The fields below are **not ready for UI yet**. Their raw runtime values and relationships must be verified first.

## Known source files

- `GameData/LairPrototypes.cfg`
- `GameData/LairPrototypes/GenericLairPrototypes.cfg`
- `DLCGameData/DLC1/LairPrototypes.cfg`
- ALIFE Director configuration containing the timing/expansion defaults.

The repository currently contains the field names below, but not the raw current runtime contents of those Lair CFGs and not a writer baseline for them.

## Candidate controls

| Raw CFG field / family | Likely gameplay domain | Possible player-facing control | Status |
|---|---|---|---|
| `MaxSpawnQuantity` | Lair population capacity | **Lair Population** | AUDIT REQUIRED |
| `InitialSpawnQuantityPercent` | Initial lair fill | **Starting Lair Population** | AUDIT REQUIRED |
| refill / wipe-respawn timers | Lair recovery after losses | **Lair Respawn Speed** | AUDIT REQUIRED |
| `DefaultSpawnDelayMin/Max` | Director spawn timing | **ALIFE Encounter Frequency** | AUDIT REQUIRED |
| `DefaultPostSpawnDirectorTimeoutMin/Max` | Director post-spawn cooldown | **Time Between ALIFE Encounters** | AUDIT REQUIRED |
| `DefaultExpansionSquadNumMin/Max` | ALIFE expansion | **ALIFE Expansion Strength** | HIGH RISK |
| faction `MinLairs/MaxLairs` | Persistent faction presence | **Faction Presence** | HIGH RISK |

## Relationships that must be proven

### Lair population
For `MaxSpawnQuantity`, determine:
- actor count vs squad count;
- inheritance through `refkey`;
- human vs mutant lair differences;
- species-specific or faction-specific prototypes;
- story/quest prototypes that must be excluded;
- DLC1 additions/overrides.

A future slider must scale proven eligible source values independently. Do not invent one fake global variable.

### Starting population
Do not assume:

`starting population = MaxSpawnQuantity × InitialSpawnQuantityPercent`

until the CFG/runtime relationship is proven.

### Respawn lifecycle
Map the complete chain:
1. actors are lost;
2. refill becomes eligible;
3. timer starts;
4. replacements spawn;
5. full-wipe behavior, if different.

Partial refill and wiped-lair respawn should remain separate until proven to be one player-facing system.

### Director cadence
Audit `DefaultSpawnDelayMin/Max` together with `DefaultPostSpawnDirectorTimeoutMin/Max`.

We need to know whether these are:
- pre-spawn delay plus post-spawn cooldown;
- independent Director states;
- defaults overridden per scenario;
- actually related to encounter frequency perceived by the player.

Do not label either one "Encounter Frequency" in production UI until this is established.

### Expansion
For `DefaultExpansionSquadNumMin/Max`, prove:
- what an expansion squad is;
- what triggers expansion;
- where squads move;
- whether humans and mutants share the value;
- whether it changes persistent world population;
- whether quest-controlled ALIFE consumes the same default.

### Faction lair limits
Treat `MinLairs/MaxLairs` as world-state tuning, not ordinary combat difficulty.

Required checks:
- quest factions;
- story hubs;
- scripted/locked lairs;
- progression-gated regions;
- DLC1 factions/regions;
- save persistence.

## Recommended order

1. **Lair Population**
2. **Lair Respawn Speed**
3. **Starting Lair Population**
4. **ALIFE Encounter Frequency**
5. **ALIFE Expansion Strength**
6. **Faction Presence**

The first four are the most plausible player-facing difficulty controls. Expansion and faction presence are much more invasive.

## Current decision

Do not add these controls to `parameters.json` or the CFG writer yet.

Next evidence required:
- current runtime `GameData/LairPrototypes.cfg`;
- `GameData/LairPrototypes/GenericLairPrototypes.cfg`;
- `DLCGameData/DLC1/LairPrototypes.cfg`;
- the Director prototype containing the spawn-delay, post-spawn-timeout and expansion defaults.

For each field record:
- prototype SID;
- inheritance/refkey;
- raw vanilla value;
- unit/type;
- base vs DLC source;
- story/quest status;
- overrides;
- safe transformation;
- runtime-test requirement.

Only then promote a candidate from **AUDIT REQUIRED** to **CONFIRMED**.
