# A-Life Spawn Mapping

## Current A-Life UI layout (2026-10-09)

The tab retains **two independent full-width sections** backed by **18 active source values**. **Human Squad Size** has four ranked compact horizontal cards (Newbie, Experienced, Veteran, Master) showing the **calculated maximum squad members** and each rank's 0.25–3x native, 0.05-step multiplier; reference caps come from the source's 3/4/5/6 maxima. **Mutant Group Size** has all fourteen original species in a compact **2-column × 7-row** alphabetical card bank. Each species card shows its own `x` setting and four current `N / E / V / M` ranked maxima; authored zero limits stay zero and are never highlighted if unchanged. The source cap computation retains existing half-up rounding, native limits, selected-difficulty base markers and writer behavior.

The two control classes remain named `HumanGroupSizeRadar` and `MutantGroupSizeRadar` solely for XAML/preset compatibility; their latest renderings are compact native source-card panels, not polar statistical charts. The click/right-click of any row updates the right rationale to the selected **rank/species source**; resetting `R` is per family. Source-only numbers or results are yellow (+1.2 DIP) if and only if they change relative to the selected reference; hovering lights up the handle alone. Board heights are 350 DIP and 546 DIP, respectively. The normal `alife.mutantSpawn.generic` control remains hidden.

**Scope exclusion**: group-size caps are not an encounter-frequency or world-population change. Separate Lair Population, refill and scenario-frequency candidates in `ALIFE-LAIR-AUDIT.md` remain audit-only until eligible generic (not quest/story/test) lairs and runtime effects are sufficiently verified. No new sources, altered CFG targets, or new preset IDs were introduced in this visual pass.

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
