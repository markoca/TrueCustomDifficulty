## CURRENT 2026-11-18 — Weather knob order and readable percentages

The ZONE WEATHER knob row is STORMS → EMISSIONS → TIME TO FIND COVER.
The first two show their source-relative weather weights as percentages
(100% = numeric weight 1×, 125% = numeric weight 1.25×); these are not
absolute weather occurrence probabilities. A single neutral `FREQUENCY`
caption sits below the number. The warning dial still uses raw seconds
(10–60 s, with 5 s native snapping). The existing emission-stage timeline
and CFG writer are not modified by this presentation change.

## 2026-11-18 — CURRENT: Time to Find Cover limited to 10–60 seconds

* TIME TO FIND COVER uses 10–60 s in 5 s steps, with 60 s as the unmodified game default. The knob, timeline preview and CFG patch validation use the same limits.
* The full ONE EMISSION timeline lasts 90–140 s, NPCs leave cover at 80–130 s, and story emission timing stays unchanged. A warning of 10 s still places the shared NPC leave-cover event after the latest 61 s story emission.
* The Wanderer factory preset's old 75 s is now 60 s, and the exact old stock file can migrate without replacing user edits. Custom old presets are clamped to the new valid control range when loaded.

## 2026-11-18 — CURRENT: Frameless ZONE WEATHER dials and actual emission phases

* The three source-backed dials keep the same compact `ArtifactKnobBank` positions and native ranges, but **no individual rounded-rectangle frames** are painted. Captions are **RANDOM EMISSIONS**, **STORMS**, and **TIME TO FIND COVER**. Both frequency dials show only a short second line (`OFF`, `NORMAL`, `LESS OFTEN`, `MORE OFTEN`) beneath their multiplier. The warning dial shows only its unsigned seconds value and `SECONDS` unit: no secondary `TO FIND COVER` drawn over it. Hover, 270° arcs, keyboard/wheel editing, disabled appearance, native ticks and reference markers stay unchanged.
* The **ONE EMISSION** rail is **not** a 0–300 s representation of the independent weather window. Its full width now covers the exact Default emission stage sequence from 0 to `ZoneTimersPolicy.EmissionEnd(warning)` = warning + 80 s (90–140 s over the configured 10–60 s warning range), and three distinct continuous segments fill it: **FIND COVER** from 0 to warning, **EMISSION** from warning through warning + 60 s (the Active stage), **AFTERMATH** for the final 20 s. The separately sourced NPC AI `EmissionEnd` at warning + 70 s remains a contrasting white marker inside AFTERMATH. The stage values and 300 s weather selection duration in the CFG are **not changed**.
* Below the rail, compact gamer-facing readouts report time to hide, the real active-emission interval, when its stages end, and when NPCs leave shelter. No fake five-minute-empty-tail or misleading `CALM` phase is drawn. Only edited numbers are accented. All changes are presentation-only; original source edits, factory presets, native step sizes, and save compatibility remain intact.
# ZONE WEATHER and TRADER RESTOCK (2026-11-18)

Two new boards with four controls. Before building them, each source was checked for what else depends on it.

## ZONE WEATHER (Environment tab)

| Card | ID | Source | Vanilla |
|---|---|---|---|
| EMISSIONS | `environment.emissionFrequency` | `DifficultyPrototypes.cfg <difficulty>.EnvironmentDifficulty.Weather_Emission_Weight` | Novice–Veteran 1×, Master 1.25× |
| STORMS | `environment.stormFrequency` | `… .Weather_Storm_Weight` | Novice–Veteran 1×, Master 1.25× |
| EMISSION WARNING | `environment.emissionWarning` | `EmissionPrototypes.cfg [0]` (SID `Default`) stage timing | 60 s |

### EMISSIONS / STORMS dependencies

- These are weights in the weather pick of the selected difficulty, not fixed timers. 2× is roughly twice as often, and 0 turns random emissions or storms off. The Xbox difficulty variants are not edited.
- The emission weather has its own pacing in `WeatherSelectionPrototypes.cfg`: weight 0 rising by 100 per change, at least 18 weather changes between emissions, and a 300 s window. These pacing values are not edited, so the weight changes how likely an emission is once it is allowed.
- **Artifacts:** anomaly fields refill after an emission. More emissions also mean more artifacts, which ties this to ARTIFACT HUNTING (said in WORKS WITH).
- **A-Life:** an emission kills NPCs and mutants caught outside, and LAIR RESPAWN TIME sets how fast lairs fill back up (said in WORKS WITH).
- **Story emissions** are started by quest nodes (`EmissionStart` with their own prototypes) and do not depend on the weight.
- **Storms** are where RAIN / THUNDER NOISE (HUMAN AWARENESS) hide your noise. More storms also leave less room for clear and cloudy weather, because the weights are relative.

### EMISSION WARNING

`BeforeTheStorm` is the time between the warning and the shock wave. Changing it shifts everything after it:

| Field | Vanilla | Written |
|---|---|---|
| `Stages.[0].PhaseDuration` (BeforeTheStorm) | 60 | warning |
| `Stages.[2].PhaseStartTime` (ShockWave) | 60 | 60 + Δ |
| `Stages.[3].PhaseStartTime` (Active) | 60 | 60 + Δ |
| `Stages.[4].PhaseStartTime` (AfterTheStorm) | 120 | 120 + Δ |
| `AIEvents.[3].AIEventStartTime` (EmissionEnd, NPCs leave cover) | 130 | 130 + Δ |

Here Δ = warning − 60. ActivateQuest and the stage durations stay.

- **Range 10–60 s, step 5 s; vanilla 60 s.** At 10 s the emission ends at 90 s; at 60 s it ends at 140 s, both inside the 300 s emission weather.
- **Story emissions** (`[1]`–`[5]`) have their own Stages and are not retimed. They inherit Default's AIEvents, but the moved EmissionEnd (80–130 s) is still after every story emission ends (≤ 61 s). NPCs never leave cover in the middle of one.
- Default is also used by the A-Life emission trigger and the EQ29 side quest. Both get the same warning.
- **Not changed:**
  - the red-sky calm before the emission (`CalmBeforeEmission`, 120 s);
  - NPC reaction delays (7–10 s);
  - kill delays;
  - `EmissionNeutralityTimer`.

## TRADER RESTOCK (Economy tab)

| Card | ID | Source | Vanilla |
|---|---|---|---|
| RESTOCK EVERY | `economy.traderRestockHours` | `BoolProviderPrototypes.cfg TradeRegenHoursPassed8.Hours` (+ `TradeRegenDaysPassed7.Days`) | 8 h (7 days) |

### Dependencies

- `TradeRegenHoursPassed8` is the refresh condition of **84 named traders** (base + DLC1). `TradeRegenDaysPassed7` is the condition of the 4 generic stalker trade profiles (BasicTrader, TraderNPC, AllTraderNPC, RC_TraderNPC). Both are used **only** as `RefreshConditionSID` in `TradePrototypes.cfg`, and the gate checks that across every base and DLC1 CFG.
- Generic traders follow the same ratio: days = 7 × hours / 8, at least 1. The line under the card shows it.
- `TradeRegenDaysPassed1` (the trader template and TradeTest) is not changed. DLC1 traders that inherit the template set their own 8 h condition.
- These are game hours, so sleeping and waiting count.
- What comes back is decided by TRADER STOCK and AMMO / MEDICAL SUPPLY (said in WORKS WITH). Prices are separate.

## Code

- `Services/ZoneTimersPolicy.cs` holds the IDs, vanilla stage table, limits and the restock ratio.
- `Services/CfgPatchWriterService.ZoneTimers.cs` writes EMISSION WARNING and TRADER RESTOCK. EMISSIONS and STORMS are direct edit points (4 difficulties each).
- Boards `ZoneWeather` and `TraderRestock` live in `AggregateVisualControl`, with family wiring in `ParameterViewModel` and `MainViewModel`.
- Baseline has `special.zoneTimers`. The census is now 422 controls (213 visible) and 2830 edit points.

## Factory presets

Absent from the table = the preset's reference-difficulty vanilla.

| Profile | EMISSIONS | STORMS | WARNING | RESTOCK |
|---|---|---|---|---|
| Hardcore+ | 1.5× | 1.5× | 40 s | 16 h |
| Hardcore | – | – | 50 s | 12 h |
| Anomaly Runner | 1.25× | – | – | – |
| Immersive | – | 1.25× | – | – |
| Wanderer | 1.15× | – | 60 s | – |
| Tactical | – | 1.15× | – | – |
| Scavenger | – | – | – | 16 h |
| Outcast | – | – | – | 12 h |
| Smuggler | – | – | – | 6 h |

Older stock files upgrade only when byte-identical (`PreZoneWeatherFactoryBlobs`, `PreCoverRangeFactoryBlobs`, `tools/factory_preset_zone_weather_2026_11_18.json`). Original Wanderer 75 s is capped at 60 s in the new factory profile; user-edited profiles are never overwritten (out-of-range values clamp on load).

## Gate

`tools/audit_zone_timers.py` is in `pre_release.py`. With `--game` / `--dlc` it re-reads every vanilla value and dependency above from the game CFG. That has passed against the 2.1 dumps.

## Not yet verified

The .NET build, a Wine run and in-game behaviour have not been checked: how often emissions and storms actually come, the timing of the warning and the cover AI, and whether traders actually restock at the new interval.

## 2026-11-18 — Board as knobs

ZONE WEATHER uses three knobs (ARTIFACT HUNTING geometry) with short in-dial lines (`ZoneTimersPolicy.KnobText`). Below them, ONE EMISSION draws a dynamic 0–(warning + 80) s bar: warning (light), storm = shock wave + active (gold), calm after it (grey), and a white tick for the NPC leave-cover moment. Two sentences explain it.
