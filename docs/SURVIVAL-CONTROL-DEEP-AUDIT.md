# Survival Control Deep Audit — v0.2.47

### 2026-10-14 — Equipment groups returned to Equipment (CURRENT)

Survival no longer hosts Equipment Wear or Item Weight. Its native **31 visible controls** (29 confirmed / 2 runtime-test-required) and **82 writer leaves** remain intact; the four equipment source controls and their separate 113 writer leaves now appear on the restored Equipment tab. Bleeding, radiation, hunger, sleep, fatigue and consumable layout and CFG routing are unaffected. The 2026-10-11 Equipment-in-Survival placement below is historical.

### 2026-10-14 — Bleeding removes redundant full-HP tick caption (CURRENT)

Under Survival → BLEEDING SYSTEM → DAMAGE TICK TIMELINE, remove the entire permanent centered line showing `FULL HP N · N HP/TICK · 1 S/TICK EST.` at local preview Y+110. Do not replace it with another status label. Existing unified selectable LOW/MEDIUM/SEVERE cards already show exact HP/TICK, numbered tick markers show timing, and the bottom `DEAD IN m:ss` or `NO HP DAMAGE` HUD is sufficient. Preserve stage selection, read-only timeline, damage calculations, card geometry, host height (665 DIP), original CFG writers and preset behavior. The conditional-stage and unverified one-second-per-tick assumptions remain in the existing hover tooltip; remove the permanent duplicate from the visual only. This entry supersedes the previous same-day instruction requiring a visible `1 S/TICK EST.` caption.

### 2026-10-14 — Bleeding unified stage row and concise death HUD (CURRENT)

Bleeding UI presentation only: three existing PTS-threshold plus HP/TICK cards at Y=405 now also serve as LOW/MEDIUM/SEVERE preview selectors; the redundant separate stage-choice row is removed. The unified centered 618-DIP max row retains live Medium/Severe threshold, Low/Medium/Severe HP/TICK, 44-DIP cards, gold selection outline, white hover, keyboard and right-click LOW reset. Selected stage only changes the read-only damage preview, never the CFG. All seven real Bleeding CFG parameter writers, group R reset, preset IDs, clickable difficulty markers and graph edit handles are unchanged.

Damage tick timeline moves to Y=470; Bleeding host and MainWindow.xaml MinHeight are 665 DIP. Its estimate uses live Max HP and currently selected stage HP/TICK, up to three visual tick markers, with 1 S/TICK EST. Visible bottom HUD is solely DEAD IN m:ss (gold time) or NO HP DAMAGE. The stage-held assumption is shown in the hover explanation; engine cadence is unverified. This replaces earlier same-day duplicate selector/HUD descriptions.


### 2026-10-14 — Bleeding read-only timeline like Hunger (CURRENT)

**Supersedes the 2026-10-14 manual-stage-timeline, WOUND BLEED, HP AT ONSET and draggable tick rail contracts below.** Survival → BLEEDING SYSTEM keeps its original CFG-backed, editable step graph: LOW/MEDIUM/SEVERE damage HP/TICK, Medium/Severe thresholds, Bleeding Inflicted and Natural Recovery. The stage editor still uses three **compact 50-DIP choice chips** at Y=470, centered in a maximum-480-DIP bank, directly above the existing 176-DIP DAMAGE TICK TIMELINE card at Y=531. Click a chip to switch the **single stage being previewed**. Hover is white, selected outline is gold, and only genuinely modified source HP/TICK numerals become gold. Right-click any chip to return to LOW; Left/Right/Up/Down/Home/End work on a selected chip. These choices are local-only preview controls and write no CFG. Source stage graph remains the only editor for actual HP/TICK and threshold values.

**The timeline itself is strictly read-only**, following Hunger/Starvation. No timeline click or hover action, mouse capture, dragging, keyboard focus target, time scrub cursor, user-inserted stage boundary or per-second stage schedule. It draws up to **three sequential numbered source damage ticks** (at 1, 2, 3 modeled seconds) with measured timestamps and a subdued/gold damage window. A lethal tick is clipped to available full HP and no tick is invented after death. If configured stage damage is zero, the rail has no damaging markers and shows NO HP DAMAGE. Compact heading shows **N TICKS / N HP LOST** for visible source charges only. Single bottom 41-DIP HUD shows **IF [STAGE] HOLDS: N TICKS / ~m:ss TO 0 HP** for the selected stage, based on Health-tab Max HP and the current stage's exact source HP/TICK; no extra initial-health slider or invented wound-points formula. Stage changes are global chip selections and immediately recalculate the entire preview. The card stays bounded to 760 DIP, inside the unchanged 726-DIP aggregate host; zero redundant instructions or new footer.

**Important limits:** stages are NOT automatically transitioned by this visualization, so "IF [STAGE] HOLDS" is a conditional reference, not a prediction of the player's changing bleed severity. The real CFG's Bleeding Inflicted, Natural Recovery and stage thresholds are preserved but do not feed an unverified wound/decay equation into the read-only tick schedule. Tick spacing remains the **explicit 1 second/tick estimate**, not proven runtime cadence; the UI labels it `1 S/TICK EST.`. Armor, medicine, damage mitigation, new wounds, and active HP regeneration are excluded. The original source IDs, preset/group reset, difficulty markers and writer mappings remain unchanged. Regression tests and consistency audit must reject any return of timeline scrub, per-second stage edits, hypothetical wound input, or source-agnostic HP tick damage.


### 2026-10-14 — Bleeding timeline uses manual source stage schedule (CURRENT)

**Supersedes all previous WOUND BLEED, HP AT ONSET, inferred bleeding-points recovery transitions and hypothetical wound-to-stage timeline descriptions below.** Survival → BLEEDING SYSTEM preserves the original seven CFG writer parameters (Bleeding Inflicted, Natural Recovery, three stage HP/TICK values, Medium/Severe thresholds) and the native step graph with 0–100 PTS thresholds. The graph alone supplies the **actual configured HP damage per stage per tick**, not the duration or occurrence of the stage. The timeline **never derives bleeding points from a fabricated wound or automatic stage decay**.

In the existing compact 726-DIP Bleeding host, a centered, max-480-DIP row of three 50-DIP high **LOW / MEDIUM / SEVERE** stage choices replaces both WOUND BLEED and HP AT ONSET rails. Choices show the exact live corresponding HP/TICK from the graph (changed values gold only if source modified), white hover, gold active outline, clear hit area. The choice sets the selected stage **from the currently inspected tick**; the first tick defaults to LOW. Scrubbing the 176-DIP Starvation-style source-charge rail selects a different second; choosing a stage then creates a persistent local transition at that second, without erasing later different transitions. Timeline bands and actual tick landmarks show the manually authored stage spans and the real configured HP/TICK for each. Adjacent identical stages collapse. Right-clicking a stage choice resets the local schedule to LOW from tick 1; right-clicking the time rail returns inspection to tick 1; arrow/Home/End/Page keys navigate when scrubbing. Stage choices and the schedule are **preview-only**, never CFG or preset writers, and group R resets only original source-backed parameters.

HP starts from live `health.maxHP` with no invented "HP at onset" knob. Each selected stage's configured HP/TICK is applied once per modeled tick; selected stage persists until manually changed. No assumption about how long bleeding remains in Low/Medium/Severe is made. Since exact engine tick scheduler is not independently established, the **1 second per HP damage tick and time-to-death remain estimates** and are labeled accordingly. No guessed Effect_Bleeding wound formula, passive bleed-point recovery, healing/armor/new injuries or extra stage transition formula is applied. Natural Recovery and thresholds still write their genuine CFG values, but do not automatically move the manual timeline. Zero-damage stages remain selectable for at least 120 seconds; model bounded at 3600 seconds; no death is claimed when no lethal result is reached. Preview readout shows inspected STAGE / configured HP/TICK / remaining HP (no speculative BLEED PTS). Update tests for real graph values, stage edits, stage breaks, source HP, HP limits and no-damage behavior.


**2026-10-14 clarification:** `survival.bleedingSeverity` retains its confirmed per-difficulty `EnvironmentDifficulty.Effect_Bleeding` writer (Novice 0.25×, Stalker/Veteran 1.0×, Master 1.5×). The Bleeding timeline now *illustratively* scales the selected base incoming wound PTS by this source value, capped at 100 PTS. The exact runtime relationship between infliction and bleeding points remains unverified; the real writer, per-stage HP/tick effects and passive PTS recovery are not changed. The two simulation inputs are `WOUND BLEED (PTS)` and `HP AT ONSET` (% of selected Max HP).


### 2026-10-11 — Current UI placement, knobs, and alcohol ranges

- The separate **Equipment** tab is retired. Four original equipment source IDs are now presented inside **Survival**, preserving **Equipment Wear** (armor and weapon wear crosshair) and **Item Weight** (ammo and consumables source-native tracks) as independent groups, after the Bleeding controls. Original `equipment.armorDurability`, `equipment.weaponWearPerShot`, `equipment.ammoWeight`, `equipment.consumableWeight` remain unchanged in values, ranges, steps, CFG writer mappings and preset fields. Their `tab` metadata alone switches to `survival`.
- The shared 270° `DrawSourceKnob` renderer no longer draws rectangular background cards around rotary knobs anywhere. Existing dial arc, tick positions, units, hover/drag/wheel/keyboard and source-specific reset remain intact.
- Beer drink strength now has a native UI range **8–40** (1-point steps, vanilla 15); Vodka strength **15–80** (5-point steps, vanilla 30). Both are editable real `BeerDrunkness` / `VodkaDrunkness` effect values via the existing writer, not synthetic stage thresholds. The Drunkenness view takes slider bounds from the catalog.

This 2026-10-11 UI contract supersedes historical references to a separate Equipment tab and the prior Beer 30/Vodka 60 ceiling. The original mechanical/source audit remains valid.

### 2026-10-10 — Beer and Vodka independent RAD removal (latest binding contract)

Within the **single MEDICAL ITEMS card** the right-hand RADIATION REMOVAL bank now has **four compact vertical sliders on one row**: **SCIENTIFIC MEDKIT**, **ANTI-RAD**, **BEER** and **VODKA**. There is no second-row, duplicate standalone Radiation Treatment card, or separate consumable alcohol antirad slider. The RAD bank is given additional horizontal space by tightening the four gold HEAL / copper BLEED pairs on the left; at least 820 DIP board width is reserved so all 12 numeric columns remain one row without clipping. Each column uses the shared horizontal cap/white-on-hover and independent source-native drag/keyboard/right-click/R reset; the RAD bank keeps a separate teal 0–200 vertical scale, with numbers not conflated with HP or bleeding points.

**Actual original CFG prototypes:** `BeerAntirad1.ValueMin/ValueMax=-10/-10`; `VodkaAntirad3.ValueMin/ValueMax=-50/-50`. New independent positive-unit control IDs: `health.beerRadiationRemoval` (vanilla **10 RAD**, **0–20 RAD**, 5-RAD native steps) and `health.vodkaRadiationRemoval` (vanilla **50 RAD**, **0–100 RAD**, 5-RAD native steps). Scientific Medkit remains 60 RAD/120 max; Anti-Rad 100 RAD/200 max. Both new controls write exclusively to their own source prototypes via the existing positive-to-negative `negateSet` operation; neither affects intoxication, drink duration, alcohol tolerance or the other drink. The previous `survival.alcoholRadiationRemoval` shared 0–2x parameter and its common Beer/Vodka CFG ownership have been retired from the catalog/Consumable Effects UI. **Preset compatibility:** when an old preset still contains the shared multiplier and lacks new individual drink values, load converts it to Beer `10 × old multiplier` and Vodka `50 × old multiplier`, retaining even fractional authored RAD amounts; new presets persist the two independent positive-unit values. Thus default 1x restores 10/50 without changing how the original game works.

The existing Medical Items anchor `health.bandageHealing` reflects changes in every HEAL/BLEED/RAD source plus Food Healing, but production CFG writing remains strictly per-source. No new effect, source ID collision or duplicate writer output is allowed. This current specification overrides historical shared-alcohol and two-RAD-bank UI descriptions.

This release revalidates every normal Survival-tab control against the locked runtime CFG/writer baseline. The audit follows the same chain used for Combat, Health and Stamina:

`UI meaning -> vanilla value -> slider range/direction -> writer operation -> concrete CFG edit points`

## Result

- **23** normal Survival controls audited.
- **23 confirmed**, **0 runtime-test-required**, **0 invalid/partial**.
- **70** concrete writer edit points.
- No new Survival control was promoted and no existing control was removed.
- Two hunger stamina-recovery controls were normalized from engine-signed values to positive player-facing penalty magnitudes.
- One stale documentation value (`LowBleedingDamage = 0.33`) was corrected to the already-audited runtime value `1.0`; catalog/writer gameplay output was already correct.

## Control matrix

| Control ID | Player-facing control | Vanilla/reference | Writer operation | Edit points | Verdict |
|---|---|---:|---|---:|---|
| `survival.hungerRate` | Hunger Build-up Rate | `0.015` | `set` | 1 | Confirmed |
| `survival.hungerMediumStaminaRegen` | Hunger Stamina Recovery Penalty | `33%` | `negateSet` | 2 | Confirmed |
| `survival.hungerStrongStaminaRegen` | Severe Hunger Stamina Recovery Penalty | `70%` | `negateSet` | 2 | Confirmed |
| `survival.sleepinessRate` | Fatigue Build-up Rate | `0.01` | `set` | 1 | Confirmed |
| `survival.energyDrinkFatigueRelief` | Energy Drink Fatigue Relief | `100%` scale | `scale` | 8 | Confirmed |
| `survival.sleepEligibilityThreshold` | Sleep Becomes Available At | `50` | `set` | 1 | Confirmed |
| `survival.minimumSleepHours` | Minimum Sleep | `7 h` | `set` | 1 | Confirmed |
| `survival.radiationAccumulation` | Radiation Accumulation | `0.60 / 1.00 / 1.50 / 1.85` | `set` | 4 | Confirmed |
| `survival.radiationRecovery` | Radiation Recovery | `0.05` | `set` | 1 | Confirmed |
| `survival.radiationDamage` | Radiation Damage | `100%` scale | `scale` | 22 | Confirmed |
| `survival.bleedingSeverity` | Bleeding Inflicted | `0.25 / 1.00 / 1.00 / 1.50` | `set` | 4 | Confirmed |
| `survival.bleedingRecovery` | Natural Bleeding Recovery | `0.3` | `set` | 1 | Confirmed |
| `survival.wellFedStaminaRegen` | Well-Fed Stamina Recovery Bonus | `20%` | `set` | 2 | Confirmed |
| `survival.fatigueLowAimPenalty` | Low Fatigue Aim-Time Penalty | `15%` | `set` | 2 | Confirmed |
| `survival.fatigueMediumAimPenalty` | Medium Fatigue Aim-Time Penalty | `25%` | `set` | 2 | Confirmed |
| `survival.fatigueStrongAimPenalty` | Severe Fatigue Aim-Time Penalty | `40%` | `set` | 2 | Confirmed |
| `survival.fatigueMediumReloadPenalty` | Medium Fatigue Reload-Time Penalty | `15%` | `set` | 2 | Confirmed |
| `survival.fatigueStrongReloadPenalty` | Severe Fatigue Reload-Time Penalty | `18.75%` | `set` | 2 | Confirmed |
| `survival.sleepHealthRestore` | Post-Sleep Health Restore | `300` | `set` | 2 | Confirmed |
| `survival.sleepStaminaRestore` | Post-Sleep Stamina Restore | `100%` | `set` | 2 | Confirmed |
| `survival.bleedingLowDamage` | Low Bleeding Damage | `1.0` | `set` | 2 | Confirmed |
| `survival.bleedingMediumDamage` | Medium Bleeding Damage | `1.5` | `set` | 2 | Confirmed |
| `survival.bleedingStrongDamage` | Severe Bleeding Damage | `2.0` | `set` | 2 | Confirmed |

The four values shown for difficulty-dependent controls are Novice / Stalker / Veteran / Master.

## Signed hunger-recovery boundary

Runtime `EffectPrototypes.cfg` stores the standard hunger recovery modifiers as negative deltas:

- `MediumHungerStaminaRegeneration.ValueMin/ValueMax = -33%`
- `StrongHungerStaminaRegeneration.ValueMin/ValueMax = -70%`

Exposing those raw signs made the old sliders run backwards from a player's point of view: moving right toward zero weakened the penalty even though the control described hunger recovery impact.

v0.2.47 uses positive penalty magnitudes instead:

- **Hunger Stamina Recovery Penalty**: vanilla `33%`, range `0..66%`.
- **Severe Hunger Stamina Recovery Penalty**: vanilla `70%`, range `0..100%`.
- Writer operation: `negateSet`, so `33% -> -33%` and `70% -> -70%` in the CFG.
- Pre-v0.2.47 presets containing negative values are migrated with `abs(value)` on load, preserving the same generated gameplay value.

The linked constraint is now expressed in the same player-facing value space:

`Hunger Stamina Recovery Penalty <= Severe Hunger Stamina Recovery Penalty`

The actively moved slider remains authoritative and the neighboring slider only moves as much as necessary to preserve that relation.

## Other linked Survival controls

The existing value-based constraints remain valid:

- `Low Bleeding Damage <= Medium Bleeding Damage <= Severe Bleeding Damage`
- `Low Fatigue Aim-Time Penalty <= Medium Fatigue Aim-Time Penalty <= Severe Fatigue Aim-Time Penalty`
- `Medium Fatigue Reload-Time Penalty <= Severe Fatigue Reload-Time Penalty`

These relationships use actual control/CFG values rather than percentages of each slider's vanilla baseline.

## Low Bleeding documentation correction

`CONFIG-BASELINE.md` already records the v0.1.63 runtime re-audit proving:

`LowBleedingDamage.ValueMin = 1.0`

`LowBleedingDamage.ValueMax = 1.0`

The catalog and writer baseline have used `1.0` since that correction. `SURVIVAL-MAPPING.md` still contained an older `0.33` line; v0.2.47 corrects that documentation-only drift. No gameplay writer value changed for Low Bleeding Damage in this release.

## Deliberate Survival boundaries

The deep audit does not turn every survival-related CFG field into a slider. These remain outside normal Survival UI:

- thirst, because the supplied CFG evidence still does not establish a complete active thirst mechanics chain;
- hunger/fatigue/bleeding/radiation state activation thresholds;
- `SleepHunger`, whose player-facing runtime semantics remain insufficiently proven;
- `bAllowEmissionSleep`;
- ambiguous bleeding movement-speed effects;
- Master-only starvation, severe-fatigue and radiation bundles, which remain in the Master tab.

This is intentional scope control, not missing writer coverage.
