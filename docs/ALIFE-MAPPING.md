# ALIFE Group Size Mapping

## Scope

The ALIFE tab intentionally exposes only the general encounter-group caps used by the ALife Director. It does not change lair capacity, lair refill/respawn, encounter frequency, faction population, expansion timing, or scenario weights.

Source CFG:

`GameData/ALifePrototypes/ALifeDirectorScenarioPrototypes.cfg`

Source structure:

`ALifeDirectorPreset.ALifeScenarioNPCArchetypesLimitsPerPlayerRank[*].Restrictions[*].MaxCount`

The actual source node is named `ALifeScenarioNPCArchetypesLimitsPerPlayerRank` (without the extra `FE`); the spelling above is descriptive only. Writer paths use the exact source node name.

## Human Group Size

Four radar axes follow the Director player-rank rows:

| Axis | Vanilla Human `MaxCount` |
|---|---:|
| Newbie | 3 |
| Experienced | 4 |
| Veteran | 5 |
| Master | 6 |

Each axis is a multiplier. `1x` is vanilla. Values are rounded half-up to an integer with a minimum of one.

There is no faction-specific Human `MaxCount` table in this Director restriction block, so the UI deliberately does not pretend to offer separate Duty/Freedom/Bandit/etc. encounter-size controls.

## Mutant Group Size

Four radar axes use the same player-rank rows. Each axis scales every non-Human restriction in that rank together:

`MutantGeneric`, `Bloodsucker`, `Blinddog`, `Flesh`, `Controller`, `Poltergeist`, `Snork`, `Boar`, `Chimera`, `Cat` (Bayun), `Tushkan`, `RatSwarm`, `Burer`, `PseudoDog`, and `Deer`.

The multiplier is applied independently to each authored `MaxCount`, preserving the vanilla species balance. Integer rounding is half-up. Authored zero limits remain zero, so increasing group size does not unlock a mutant type that the source disables for that rank.

Examples:

- Newbie Blinddog: `4` at `1x`, `6` at `1.5x`, `8` at `2x`.
- Master Tushkan: `16` at `1x`, `24` at `1.5x`, `32` at `2x`.
- Newbie Bloodsucker: `0` at every multiplier because the source restriction is zero.

## Deliberately untouched

- `GameData/LairPrototypes.cfg / MaxSpawnQuantity`
- `InitialSpawnQuantityPercent`
- all refill / wipe-respawn timers
- `DefaultSpawnDelayMin/Max`
- `DefaultPostSpawnDirectorTimeoutMin/Max`
- `DefaultExpansionSquadNumMin/Max`
- faction `MinLairs` / `MaxLairs`

Those controls affect world population, lair lifecycle, or Director timing rather than the general encounter-group size requested for this tab.
