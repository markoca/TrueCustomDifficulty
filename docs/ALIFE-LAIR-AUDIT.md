# ALIFE Lair / World-Population Audit

## Scope

This audit covers ALIFE controls outside the already implemented Director group-size radars.

Implemented today:
- **Human Group Size** — rank-specific Human `MaxCount` in `ALifeDirectorScenarioPrototypes.cfg`.
- **Mutant Group Size** — species/rank-specific mutant `MaxCount` in `ALifeDirectorScenarioPrototypes.cfg`.

Runtime source used for this audit:
- `STALKER2-GameData-CFG(3).zip`
- `STALKER2-DLC1-CFG(3).zip`

Verified files:
- `GameData/LairPrototypes.cfg`
- `GameData/ALifePrototypes/ALifeDirectorScenarioPrototypes.cfg`
- `GameData/ALifePrototypes/ALifePopulationManagerFactionPrototypes.cfg`
- `DLCGameData/DLC1/LairPrototypes.cfg`
- `DLCGameData/DLC1/AlifePrototypes/ALifePopulationManagerFactionPrototypes.cfg`

## Core finding

**Director group size and lair population are separate systems.**

The existing ALIFE radars control how many actors the Director may place together in an encounter/group.

`LairPrototypes.cfg` separately defines how many inhabitants a lair can maintain, how full it starts, how quickly it refills, and how its inhabitant archetypes are weighted.

That makes **Lair Population** a valid new difficulty domain rather than a duplicate of Group Size.

---

## Runtime structure

Base game `LairPrototypes.cfg` contains:
- 75 top-level lair prototypes;
- 38 distinct inhabitant-faction keys;
- 788 faction/rank spawn-setting records.

DLC1 `LairPrototypes.cfg` contains:
- 19 top-level lair prototypes;
- 26 distinct inhabitant-faction keys;
- 568 faction/rank spawn-setting records.

Every audited faction/rank spawn setting can contain:
- `MaxSpawnQuantity`
- `InitialSpawnQuantityPercent`
- `InitialSpawnQuantityRespawnTimeSeconds`
- `MaxSpawnQuantityRespawnTimeSeconds`
- `WipeRespawnTimeoutSeconds`
- per-archetype `MinQuantityPerArchetype`
- per-archetype `SpawnWeight`

---

# 1. Lair Population

## Meaning

`MaxSpawnQuantity` is the authored population capacity for one inhabitant faction inside a lair, per player rank.

This is **not** the same field as the Director encounter/group `MaxCount`.

## Representative mutant vanilla capacities

| Mutant | Newbie | Experienced | Veteran | Master |
|---|---:|---:|---:|---:|
| Blind Dog | 9 | 9 | 9 | 9 |
| Bloodsucker | 2 | 2 | 3 | 3 |
| Boar | 5 | 5 | 5 | 5 |
| Flesh | 5 | 5 | 5 | 6 |
| Controller | 1 | 1 | 1 | 2 |
| Poltergeist | 1 | 1 | 1 | 2 |
| Chimera | 1 | 1 | 1 | 2 |
| Burer | 1 | 1 | 1 | 2 |
| Pseudogiant | 1 | 1 | 1 | 2 |
| Snork | 8 | 8 | 9 | 9 |
| Pseudodog | 1 | 1 | 2 | 2 |
| Deer | 1 | 1 | 2 | 2 |
| Bayun | 2 | 2 | 2 | 2 |
| Rat / Rat Swarm lairs | 1 | 1 | 1 | 1 |
| Tushkan | 14 | 16 | 20 | 24 |
| Zombie (generic mutant lairs) | 6 | 8 | 10 | 10 |

DLC1 preserves most of the same authored patterns. Some DLC-specific CNPP lairs differ, for example Bayun can use `2 / 2 / 3 / 4`.

## Human lairs

Human lair capacities also vary by faction and rank rather than using one universal number.

Examples:
- Militaries: `6 / 6 / 6 / 7`
- many ordinary human factions use values around `6 / 6 / 7 / 7` or `6 / 6 / 7 / 8`;
- authored differences must be preserved rather than replaced by one fixed group count.

## UI recommendation

**GOOD CANDIDATE**

Possible player-facing section:

### WORLD POPULATION
**Human Lair Population**
**Mutant Lair Population**

Preferred transformation:
- multiplier over each eligible authored `MaxSpawnQuantity`;
- preserve zero values;
- integer half-up rounding;
- preserve faction/species/rank differences;
- base game + DLC1 together;
- exclude special/test/story prototypes after final eligibility audit.

Do **not** replace all values with one absolute count.

---

# 2. Starting Lair Population

## Runtime values

`InitialSpawnQuantityPercent` appears in every audited faction/rank spawn-setting record.

Base-game distribution:
- `0.5`: 688 records
- `1.0`: 100 records

So most lairs start at roughly half of their configured maximum, while a significant authored subset starts fully populated.

## UI recommendation

**POSSIBLE, BUT LOWER PRIORITY**

Possible label:
**Starting Lair Population**

Do not expose this until runtime behavior confirms whether the field directly means:

`initial count = MaxSpawnQuantity × InitialSpawnQuantityPercent`

The raw configuration strongly suggests that relationship, but CFG structure alone is not enough to claim exact runtime rounding/initialization semantics.

---

# 3. Lair Respawn / Refill

Three separate fields exist per faction/rank entry:

- `InitialSpawnQuantityRespawnTimeSeconds`
- `MaxSpawnQuantityRespawnTimeSeconds`
- `WipeRespawnTimeoutSeconds`

## Dominant vanilla values

Across the 788 base-game faction/rank records:

- `InitialSpawnQuantityRespawnTimeSeconds = 180` in 774 records
- `MaxSpawnQuantityRespawnTimeSeconds = 480` in 774 records
- `WipeRespawnTimeoutSeconds = 480` in 774 records

There are deliberate exceptions, including very short/test/special values and the initial empty prototype.

This is strong evidence that ordinary lairs commonly use:
- 3 minutes for the initial population refill stage;
- 8 minutes for maximum-population refill;
- 8 minutes after a full wipe.

## UI recommendation

**GOOD CANDIDATE AFTER LIFECYCLE TEST**

Possible player-facing control:
**Lair Respawn Speed**

But one slider should scale all three eligible timers only if runtime testing confirms that doing so produces one coherent perceived respawn-speed mechanic.

Preferred user-facing direction:
- lower slider = slower repopulation;
- higher slider = faster repopulation.

Implementation therefore likely requires inverse scaling of timer seconds.

Do not include special/test prototypes in the aggregate transform.

---

# 4. Lair composition

Each lair/rank can also define:

- `MinQuantityPerArchetype`
- `SpawnWeight`

Base game contains 2709 occurrences of each.

Example human lairs distinguish CloseCombat, Recon, Stormtrooper, Sniper and Heavy archetypes with different minimum quantities and weights.

## UI recommendation

**NOT A NORMAL DIFFICULTY CONTROL YET**

This could eventually support:
- more/less elite NPCs;
- more snipers/heavies;
- different mutant composition.

However this changes composition rather than simple difficulty magnitude and is highly faction/prototype specific.

Keep as a separate future audit.

---

# 5. Resting Lair system

The runtime contains a dedicated `RestingLairDefault` prototype:

- `RestingLairInstantSpawnScenarioChance = 0.4`
- `RestingLairShortDelayedSpawnScenarioChance = 0.4`
- `RestingLairMinNPCCount = 1`
- `RestingLairMaxNPCCount = 5`
- `RestingLairShortDelaySpawnMin = 60`
- `RestingLairShortDelaySpawnMax = 120`
- `RestingLairLongDelaySpawnMin = 600`
- `RestingLairLongDelaySpawnMax = 1200`
- `ALifeLairsSearchRadius = 65000`
- `GameTimeOfflineToRerollLairData = 14400`

## UI recommendation

**INTERESTING BUT ADVANCED**

Potential future controls:
- Resting Lair NPC Count
- Resting Lair Spawn Delay
- Resting Lair Immediate-Encounter Chance

Do not expose search radius or offline reroll timing as ordinary difficulty controls without runtime testing.

---

# 6. ALIFE Director timing

Global Director defaults:

- `DefaultSpawnDelayMin = 100`
- `DefaultSpawnDelayMax = 180`
- `DefaultPostSpawnDirectorTimeoutMin = 150`
- `DefaultPostSpawnDirectorTimeoutMax = 300`
- `DefaultALifeLairExpansionToPlayerTimeMin = 120`
- `DefaultALifeLairExpansionToPlayerTimeMax = 180`
- `DefaultExpansionSquadNumMin = 4`
- `DefaultExpansionSquadNumMax = 7`

However the same Director CFG contains many authored scenario-specific overrides.

Examples include:
- spawn delays as low as `5–30` seconds;
- several `60–90`, `120–150`, `120–180`, and `150–240` ranges;
- post-spawn timeouts ranging from roughly `60–120` to `300–420` seconds;
- expansion squad overrides such as `1–1` and `4–7`.

## Key conclusion

Changing only the global defaults would **not** reliably change all ALIFE encounter cadence because many scenarios override them.

A real **ALIFE Encounter Frequency** control would need an eligibility audit over the default values plus scenario-level overrides, preserving their authored ratios and excluding story/special scenarios where appropriate.

## UI recommendation

**GOOD CONCEPT, NOT YET SAFE**

Do not implement as one naive global slider.

---

# 7. Faction / species ALIFE state thresholds

`ALifePopulationManagerFactionPrototypes.cfg` uses `MinLairs/MaxLairs` thresholds to select faction goals such as:

- Aggressive
- Normal
- Defensive

Examples:

### Bandits
- Aggressive: 1–5 lairs
- Normal: 6–20
- Defensive: 21–900

### Militaries
- Aggressive: 1–3
- Normal: 4–9
- Defensive: 10–900

### Bloodsucker
- Aggressive: 1–7
- Normal: 8–31
- Defensive: 32–900

### Pseudogiant
- Aggressive: 1–7
- Normal: 8–10
- Defensive: 11–900

DLC1 adds additional faction definitions, including MoldyBlinddog and IkarVarta.

## Key conclusion

These values are **behavior-state thresholds based on current lair count**, not simply "maximum faction population".

Changing them can alter whether a faction/species behaves aggressively, normally or defensively as its number of controlled lairs changes.

## UI recommendation

**DO NOT expose as simple Faction Presence sliders.**

This is a strategic ALIFE behavior system and requires a dedicated audit before any tuning.

---

# Recommended implementation order

## Tier 1

### 1. Human Lair Population
Scale eligible human `MaxSpawnQuantity` values.

### 2. Mutant Lair Population
Scale eligible mutant `MaxSpawnQuantity` values.

These are now the strongest next ALIFE candidates.

## Tier 2

### 3. Lair Respawn Speed
Scale eligible refill/wipe timer families after runtime validation.

## Tier 3

### 4. Starting Lair Population
Only after exact initialization semantics are validated.

### 5. ALIFE Encounter Frequency
Requires Director scenario-override eligibility mapping.

## Separate advanced audits

- Lair archetype composition
- Resting Lair behavior
- Expansion squad behavior
- Faction/species Aggressive/Normal/Defensive thresholds

---

# Proposed next implementation

The safest next production feature is:

## WORLD POPULATION

- **Human Lair Population**
- **Mutant Lair Population**

These should be separate from the existing:

## GROUP SIZE

- Human Group Size
- Mutant Group Size

That distinction is player-facing and matches the CFG architecture:

- **Group Size** = how many can be placed together in an encounter.
- **Lair Population** = how many inhabitants a world lair can maintain.

Before coding, perform one final eligibility pass over the 75 base and 19 DLC1 lair prototypes to classify:
- generic gameplay lairs;
- quest/story/special lairs;
- test/empty prototypes;
- DLC-only prototypes.

Only the generic gameplay population should be scaled.

## 2026-10-15 — A-Life UI cleanup

Removed the unreachable `HumanGroupSizeRadar.ValueToX` helper after source-level call-site verification. The editor's four rank-specific human group-size targets and its native pointer/keyboard reset flow are untouched; mutant group-size controls are unchanged. No CFG edits or presets changed. Both UI catalogs and the project consistency audit track this cleanup. Source inspection passes, but local WPF build/Wine test still need execution.
