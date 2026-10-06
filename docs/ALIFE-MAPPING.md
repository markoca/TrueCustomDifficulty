# A-Life Spawn Mapping

## Scope

The **A-Life** tab exposes ALife Director spawn-count caps from:

`GameData/ALifePrototypes/ALifeDirectorScenarioPrototypes.cfg`

Source structure:

`ALifeDirectorPreset.ALifeScenarioNPCArchetypesLimitsPerPlayerRank[*].Restrictions[*].MaxCount`

These controls intentionally do **not** modify lair capacity, lair refill/respawn, encounter frequency, faction population, expansion timing, or scenario weights.

## Human Spawn Count per Rank

The human radar follows the Director player-rank rows:

| Axis | Vanilla Human `MaxCount` |
|---|---:|
| Newbie | 3 |
| Experienced | 4 |
| Veteran | 5 |
| Master | 6 |

Each axis is an independent multiplier. `1x` is vanilla.

## MutantGeneric restriction

The Director also contains a separate `MutantGeneric` restriction with vanilla `MaxCount` values:

`3 / 4 / 4 / 4`

It is a sibling entry beside the concrete mutant restrictions in each player-rank `Restrictions[]` list. The CFG structure does not establish it as a parent ceiling for Bloodsucker, Blind Dog, Tushkan, or the other concrete species, so the tool does **not** compose it with species `MaxCount` values and does not expose it as a player-facing global slider.

## Mutant Spawn per Species

The mutant radar contains only concrete mutant types. Each point changes one species across all four player-rank rows while preserving the vanilla rank progression.

The legend is intentionally player-facing: it explains whether the mutant is normally solo, pack-oriented, swarm-oriented, rare, or disabled at a lower rank, while still showing the resulting group cap.

| Radar axis | N / E / V / M vanilla `MaxCount` |
|---|---|
| Bloodsucker | 0 / 1 / 1 / 1 |
| Blind Dog | 4 / 6 / 8 / 12 |
| Flesh | 2 / 3 / 5 / 6 |
| Controller | 0 / 1 / 1 / 1 |
| Poltergeist | 1 / 1 / 2 / 2 |
| Snork | 1 / 3 / 4 / 6 |
| Boar | 2 / 3 / 5 / 6 |
| Chimera | 1 / 1 / 1 / 1 |
| Bayun | 1 / 1 / 1 / 2 |
| Tushkan | 6 / 8 / 12 / 16 |
| Rat Swarm | 1 / 1 / 1 / 1 |
| Burer | 1 / 1 / 1 / 2 |
| Pseudodog | 1 / 1 / 1 / 1 |
| Deer | 1 / 1 / 1 / 1 |

The multiplier is applied independently to each rank-specific `MaxCount`. Integer rounding is half-up. Authored zero caps remain zero, so increasing a species does not unlock that species at a player rank where the Director explicitly disables it.


There is no mapped Pseudogiant restriction in this Director `Restrictions[].MaxCount` table, so the UI does not invent a Pseudogiant spawn control.

## Deliberately untouched

- `GameData/LairPrototypes.cfg / MaxSpawnQuantity`
- `InitialSpawnQuantityPercent`
- all refill / wipe-respawn timers
- `DefaultSpawnDelayMin/Max`
- `DefaultPostSpawnDirectorTimeoutMin/Max`
- `DefaultExpansionSquadNumMin/Max`
- faction `MinLairs` / `MaxLairs`

Those values belong to world population, lair lifecycle, or Director timing rather than the spawn-count caps represented by these radars.
