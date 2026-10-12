# Visual Control System

### 2026-10-12 — Starvation source-native knob trio and dual lower faders (CURRENT)

`StarvationTimeline` keeps its single toggle, five source-backed values and unchanged read-only tick/HUD preview. Under the checkbox (Y=52..91), **three independent native 270° knobs** at Y=106..287 display `PENALTIES START` (seconds), `HP DAMAGE PER TICK` (actual HP) and `DAMAGE EVERY` (seconds). Source IDs respectively `survival.starvationPenaltyDelay`, `survival.starvationDamage`, `survival.starvationDamageInterval`; 12-DIP gaps and bounded **606-DIP** total group. Use `DrawSourceKnob` in raw-unit mode **without the positive-prefix normally applied to consumable medicine**. Its original native step/range, subdued Base reference tick, changed-only gold current digits, angular drag/wheel/keyboard/right-click and Master-disabled behavior must all apply. No ghost slider for HP damage remains below.

At Y=299 starts the unchanged **176-DIP** source-derived 3-tick timeline + `STARVATION KILLS IN` display. At Y=487 are the two remaining real independent Master sliders, **EXTRA STAMINA COST** and **HEALTH RECOVERY BLOCKED**, each in its own 80-DIP compact horizontal card, centered, capped at 370 DIP, separated by 12 DIP. They always sit side by side at the minimum actual host width 520 (ignore general MasterCardBank's 672-DIP breakpoint). Shared render/measurement constants yield total height 581 DIP, leaving sufficient bottom space. No CFG manipulation, new sources, dependency bypass or changes to source step/presets. This is the CURRENT Starvation composition and supersedes the obsolete timing-card/effect-card arrangement.

### 2026-10-12 — Starvation Kills In read-only HUD (CURRENT HUD semantics; historical control placement)

`StarvationTimeline` retains three passive dot markers (numbers ABOVE the rail), phase names, timestamps, source-accurate `3 TICKS / N RAW HP` heading and its five native Starvation controls. The previous footer sentence becomes a **41-DIP bounded two-row read-only HUD** inside a **176-DIP** preview. Left label `STARVATION KILLS IN` above `ESTIMATE · NO HEALING`; bold middle `~48s` at 16 DIP; right `15 TICKS` over `FULL 100 HP` (dynamic values). Bounded timer text avoids collision with the right values on the minimum-width host. A thin accent edge and neutral divider organize the row without adding redundant prose or clickable elements. Time is neutral at reference, gold only when a contributing CFG/Health Max HP value is modified, and muted when OFF with `IF ENABLED · ESTIMATE` sublabel. Zero-damage and unavailable inputs show short appropriate non-countdown status strings.

The same **raw, full-health, no-healing** formula remains: `K=ceil(health.maxHP/starvationDamage)`, `T=starvationPenaltyDelay + K*starvationDamageInterval` measured from underlying starvation-bundle activation. This is not a simulation or guarantee of actual death; other active mechanics, HP protections and regeneration are excluded. Observe health.maxHP read-only and never reset it with Starvation. The effect cards shift down by the same `StarvationPreviewHeight=176` measurement used for rendering. All five source parameters and toggle dependency/CFG/preset inputs remain identical. Supersedes the prior 152-DIP one-line readout, not the original three-tick source timeline.

### 2026-10-12 — Starvation full-HP preview and uncluttered tick markers (HISTORICAL; footer superseded)

`StarvationTimeline` keeps the same parent switch, **2 native timing sliders**, **3 native effect sliders**, fixed three-tick read-only visualization and the `3 TICKS / N RAW HP` header. Timeline frame is **152 DIP** with its native markers at +75 DIP. Tick numbers are **above** the rail (+49 DIP), never inside the dots; tight spacing merges ordinal text to `1–3` without changing the source times. Times sit below the rail; 0s and START occupy their own lower band. The final +130 DIP line states **FULL X HP → 0 IN ~T · K TICKS (RAW, NO HEALING)**, or its zero-damage/unavailable fallback. No slider state or tick labels overlap this line; the effect bank follows the common `StarvationPreviewHeight` layout value.

`X` comes from the independent live `health.maxHP` source (25–200 HP), **read-only and subscribed** to rerender on changes; it is not a Starvation family member, cannot receive tick-marker interaction, and is excluded from Starvation R/reset. `K = ceil(X / starvationDamage)`, `T = starvationPenaltyDelay + K × starvationDamageInterval` from the moment the underlying bundle triggers. This is **hypothetical unmitigated full-HP damage** only, not an assertion about runtime health loss, healing, resistance, indefinite hunger status, or an actual death event. If parent mechanics are OFF, the readout is neutral/muted and prefixed `IF ENABLED`; if tick damage is zero it shows no lethal countdown; zero interval or missing health capacity does not manufacture an estimate. All original source parameters, presets, dependency guards and writer operations are untouched. Previous 124-DIP strip sizing and no-estimate restriction are superseded by this specifically approved arithmetic projection.

### 2026-10-12 — Hero Headshot Damage knob and Starvation Timeline v2 (CURRENT)

**Your Weapons / Headshot Damage:** the central **HEADSHOT DAMAGE** source-native 270-degree knob is the group's visual hero: its circular radius and native rail are **18% larger** than the two neighboring WEAPON DAMAGE and JAM FREQUENCY knobs, its current value is slightly larger, and its own label has modest emphasis. The group stays compact, centered and bounded; two neighbors remain their previous size. The shared `DrawSourceKnob` renderer has an optional `prominent` presentation flag used **only** when `combat.playerHeadshotMultiplier` is drawn by `DrawYourWeaponsKnobs`. Arc input range, real tick marks, native source step, selected-difficulty reference indicator, position/value mapping, hit target, edit gesture, wheel and keyboard precision, right-click reset, presets and CFG patch writer are unchanged. Do not increase the width of all three knobs or create a new master slider.

**Master / Starvation Penalties:** the read-only DAMAGE TICK TIMELINE becomes an informative **124-DIP phase strip** without turning tick markers into editable sources. Keep its parent STARVATION MECHANICS toggle, two editable timing cards (**PENALTIES START**, **DAMAGE EVERY**) and three editable effect cards (**EXTRA STAMINA COST**, **HP LOST PER TICK**, **HEALTH RECOVERY BLOCKED**) intact. A single compact header at top right reads **3 TICKS / N RAW HP**, with N = 3 × the genuine HP LOST PER TICK value (unmitigated source-event sum, not predicted effective health loss). A neutral **DELAY** phase is shown before the configurable start marker where there is space; a softly highlighted **DAMAGE WINDOW** and three numbered circular ticks follow it. Display source-correct elapsed timestamps below the three ticks when they can fit, retaining the final timestamp even when intermediate ones would overlap, and **START Ns** beneath the actual delay marker. The background fades to the same neutral OFF styling as other Master controls whenever the mechanics toggle is off; no fake live-game simulation, SAFE-window claim, invented resistance/HPS or extra descriptions. Vertical layout measurement and rendering share the `StarvationPreviewHeight=124` constant, with the three native effect cards moving down 44 DIP to avoid collision. Keep read-only markers and all original underlying CFG sources. This supersedes the former 80-DIP single-rail, dot-only Starvation preview. Local publish/Wine acceptance pending.


### 2026-10-12 — Water/Energy Drink accents, centered medicine trio and uniform Master OFF styling (CURRENT)

**Energy Drink Fatigue Relief has no blue secondary line.** It uses only the normal gold/neutral primary ruler, matching other uncolored fatigue controls. Blue underlines remain reserved for the distinct Energy Drink stamina-cost duration effects (light blue for -100%, dark blue for -30%); Water's own duration marker remains green. The Fatigue Relief control retains its exact slider range, value, native CFG source and mouse/keyboard/reset behavior.

**Vinca Bleed Regen, Hercules Carry and Psi Block Protection** are exactly three separate source-native 270-degree knobs **centered as a trio** beneath the duration/short-effect timeline. Compute a bounded medicine knob width from the existing four-drink row, derive the combined width of just three medicine knobs plus two gaps, and center that group on the card. Do not left-align them to the four-knob drink row or stretch them to full width. The center values, units (bleed points / kg / psi points), references, native ranges, wheel/drags/resets and CFG mappings are unchanged.

**Master OFF-state uniformity:** `ParameterCard`'s XAML whole-card opacity=0.48 applies only to ordinary non-visual rows whose `IsControlEnabled` is false. A composite visual anchor (especially the dependency-disabled StarvationTimeline) must **not** be faded again as a complete card because its native drawing already renders each child in its disabled state. All native Master slider cards and severe-fatigue native gauges share `MasterDisabledInk` (70% main text opacity), muted inactive handles and rails with no gold change accent while off. Starvation and Severe Fatigue therefore follow the same source-disabled contrast rule. The toggle remains readable and clickable so the player can enable the bundle. No disabled child becomes editable, no game mechanics or dependency wiring change. Previous family-specific Starvation text-opacity special case is superseded. Local publish/Wine visual test pending.


### 2026-10-12 — Instant stamina knobs renamed to STAMINA GAIN (CURRENT)

The two independently writable direct-percent consumable controls are now named **WATER STAMINA GAIN** (38% vanilla) and **ENERGY DRINK STAMINA GAIN** (50% vanilla) in the 270-degree knobs, catalog labels and contextual help. "Gain" means **immediate stamina restored on consumption**, not sustained stamina regeneration; the existing WATER STAMINA BUFF and ENERGY DRINK REGEN knobs remain separately named and operate unchanged. This is nomenclature only: retain parameter IDs `survival.waterInstantStaminaRestore` and `survival.energyDrinkInstantStaminaRestore`, actual 0–100% units, 1-percentage-point steps, exact `ValueMin/ValueMax` direct CFG writes, vanilla levels, Limited Energy Drink unaffected, old-preset migration, override control, and every native knob interaction. Earlier "REFILL" instructions for these two UI labels are superseded.


### 2026-10-12 — Independent direct-percent Water and Energy Drink instant stamina refill (CURRENT)

The former single **WATER + ENERGY DRINK REFILL** 1x control `survival.instantStaminaRestore` is retired from production. **WATER STAMINA GAIN** uses independent `survival.waterInstantStaminaRestore` and exposes the exact **38%** vanilla refill; **ENERGY DRINK STAMINA GAIN** uses `survival.energyDrinkInstantStaminaRestore` and exposes the exact **50%** vanilla refill. Both are real, separate source-backed **0–100%** 270-degree rotary knobs with **1 percentage-point** source steps and clearly centered percent values and 0%/100% ends. Their writer operations **directly set** the corresponding `ValueMin` and `ValueMax` in `WaterStaminaInstant` and `EnergeticStaminaInstant3`, respectively. They must not remain multipliers or share writes. Normal game stamina capacity caps the effect; this is the share of full stamina restored instantly, not regeneration over time.

**Consumable Effects top row** comprises four *bounded* equal-sized rotary controls: WATER STAMINA GAIN (38%), ENERGY DRINK STAMINA GAIN (50%), WATER STAMINA BUFF (1x; preserves the existing coupled stamina regeneration and action cost effects), and ENERGY DRINK REGEN (1x; sustained stamina regeneration). This replaces the obsolete three-knob bank without changing the existing duration timeline / child markers, drink-color visual contract, medicine knobs or their positioning. **Limited Energy Drink's standalone `EnergeticLimitedStaminaInstant4` 100% refill is untouched** because the user requested independent controls for the two regular drinks, not an additional Limited Edition slider.

Old presets containing the original `survival.instantStaminaRestore` multiplier are migrated on load when new source IDs are absent: scalar × 0.38 → Water, scalar × 0.50 → regular Energy Drink, clamped at 100%. The old scale no longer writes to Limited Energy Drink, which stays vanilla. The Drink Duration Override editor-only flag continues under the new Water anchor while retaining the same `drinkDurationOverride` preset property. Native source intervals, right-click reset, selected-source help, wheel, preset roundtrip and source baseline counts and SurvivalPatchContract must agree. No new CFG prototype, runtime effect, synthetic calculation or altered duration behavior is introduced. This entry supersedes historical shared-multiplier wording below. Local build/Wine verification is pending.


### 2026-10-12 — Two blue shades for Energy Drink effect markers (CURRENT)

The previously approved Water / Energy Drink underlines remain strictly visual and are still bounded by **each real child-effect marker** rather than the parent drink duration handle. **Water stays green (RGB 91,173,98)**. For Energy Drink, the independently editable **−100% stamina cost** effect (`survival.energyFullCostDuration`) uses **light blue (RGB 92,186,243)**, while the **−30% stamina cost** effect (`survival.energyPartialCostDuration`) uses **deeper blue (RGB 58,111,180)**. The two separate colored lines on the main Energy Drink timeline therefore communicate which cost-cut duration belongs to which marker, and the two matching small effect-duration tracks below use the exact same source-specific colors. The general Energy Drink duration identity and Energy Drink fatigue-relief underline continue to use the light blue, with no third Energy Drink color. Keep both child marker positions, their +7/+11 DIP line separation, actual seconds coordinates, handles, neutral/gold base rails, source values and interaction regions unchanged. This is presentation-only: no CFG change, source additions, altered snap/ranges, preset change, or color for alcohol/medicine/mixed refill controls. This subsection supersedes the earlier single-blue Energy Drink visual rule. Local build/Wine visual acceptance is pending.


### 2026-10-12 — Water / Energy Drink effect-marker stripes: green and blue (CURRENT)

This supersedes the earlier **blue Water / orange Energy Drink** colored-line rule. **Water = subdued green** (RGB **91,173,98**); **Energy Drink = subdued blue** (RGB **76,149,215**). Do not change the shared gold main slider track, numeric highlighting, inactive rail or native handles. Colored underlines remain purely visual, thin (**2.4 DIP**) and exclusive to Water and Energy Drink effects; no new CFG source, interactivity or tutorial legend.

**Critical timeline semantics:** in `ConsumableMixer` main WATER / ENERGY DRINK rows, the colored line(s) terminate at the *independent child-effect marker*, **never at the main drink duration handle**. WATER draws **one green stripe** from the 0-second baseline to `survival.waterRegenDuration` (+25% stamina effect marker). ENERGY DRINK draws **two blue stripes** from the baseline to `survival.energyFullCostDuration` (−100% stamina cost) and `survival.energyPartialCostDuration` (−30% stamina cost), respectively. Position Energy Drink's two stripes at **+7 DIP / +11 DIP** below its main rail so they remain distinguishable; Water uses **+9 DIP**. All X coordinates come from the same source-native `SecondsX` mapping already used to render the short-effect markers. The muted marker ticks, editable parent gold duration bars, min/max, baseline references and shared seconds axes remain unchanged.

The three compact short-effect cards keep their colored underlines ending at **their own editable handles**, and the Energy Drink Fatigue Relief ruler keeps its own blue line ending at its current handle. Alcohol, medicine, generic stamina and mixed Water+Energy refill knobs remain standard gold/neutral. Preserve native clamps/linked duration constraints, range override, pointer/keyboard edit, right-click/R reset, presets, and CFG writer. This is presentation-only; local publish/Wine acceptance is pending.


### 2026-10-12 — Water / Energy Drink slider underlines (CURRENT)

Only **Water** and **Energy Drink** source-backed horizontal effect sliders receive a **second thin, colored line just below their existing gold slider rail**. Water uses subdued **blue** (RGB 72,159,190); Energy Drink uses subdued **orange** (RGB 215,139,62). The line is **2.4 DIP thick**, follows the real current slider value from the track's left position to its current handle, and is non-interactive. When a value is at the left endpoint or the source is disabled, no colored fill is drawn. The main track, gold handle, reference tick, actual scale, number highlighting, snapping, keyboard/mouse behavior and reset remain unchanged.

The rule applies **only** to `survival.waterEffectDuration` and `survival.waterRegenDuration` (blue), plus `survival.energyDrinkEffectDuration`, `survival.energyFullCostDuration`, `survival.energyPartialCostDuration` and `survival.energyDrinkFatigueRelief` (orange). The five-drink duration timeline colors its first two lanes; the three compact short-effect cards use the same underline and move their existing rails up 4 DIP solely to avoid collisions with min/max captions. The Energy Drink Fatigue Relief native ruler in Survival uses the same orange underline. **Other drink sliders (Vodka, Beer, Magic Vodka), medical duration sliders, generic stamina controls and mixed Water + Energy Drink refill knobs remain standard gold/neutral.** No colored ring is added to the separate strength knobs, no new indicator/legend or redundant descriptive text, no second source/control, and no CFG or preset change. The original slider interaction rectangle follows any shifted compact rail exactly. This rule supersedes any suggestion to color every consumable category.


### 2026-10-12 — Human Combat: Tactical Control Console (CURRENT)

Approved **solution 1 / Tactical Control Console** replaces the earlier Squad Pressure Dashboard layouts. The Human Combat card remains one native five-source `HumanCombatProfile` WPF aggregate, but its width is **bounded to 802 DIP** rather than stretching across the entire available host. On wide hosts (at least **790 DIP**), a **300-DIP left enemy-profile column** and a **three-knob offensive-pressure row** are paired; total measured height is **366 DIP**. For narrower hosts, the 2D profile is centered above the same three-knob horizontal row; measured height is **589 DIP** and controls remain inside the panel. Rendering and height use the exact same breakpoint.

**ENEMY PROFILE:** compact inline **HEALTH** and **GUN DAMAGE** readouts replace the two boxed stat cards. They are read-only live values; only digits change to gold when modified. Below, the **BALANCE EDITOR** is a 191-DIP source-exact X/Y graph with visible **ENEMY HEALTH** horizontal axis, **ENEMY GUN DAMAGE** vertical axis and real native tick values (including selected Base reference crosshair). The three original interactive rings remain: bottom edits health, left edits gun damage, center edits both. No duplicate editing knobs for these two parameters, no numeric invention or changes to source ranges, steps or value-based snapping.

**OFFENSIVE PRESSURE:** the three long horizontal slider cards are removed. **ATTACK FREQUENCY** (`combat.npcAttackCooldown`), **FIRE ACCURACY** (`combat.npcHipAccuracy`) and **EXPLOSION DAMAGE** (`combat.explosionDamage`) are three independent 270-degree rotary knobs in a single tight row, using the existing `DrawSourceKnob` native-source renderer, subdued Base tick and modified value coloring. The `HumanCombatProfile` family is included in the shared knob mouse-wheel dispatch so drag, wheel, keyboard, right-click per-control reset and R reset-group retain their standard behavior. No additional gameplay or CFG controls are created. Preserve exact parameter IDs, writer/preset roundtrips and source selection. Old oversized offensive slider or two-value-card contracts are superseded. Local build/Wine visual approval remains pending.


### 2026-10-12 — Human Combat: balanced dashboard spacing with named axes (CURRENT)

The approved Squad Pressure Dashboard keeps the **two current-value stat cards** above the left **BALANCE EDITOR** and the **three independent OFFENSIVE PRESSURE sliders** on the right. Adjust both column headings to the same baseline, use matching top alignment between the stat-card row and the offensive slider stack, and give the slider cards consistent height and spacing. The left graph is slightly larger where width permits and is centered within a bounded left column while reserving room for its two axis titles, ticks and the existing bottom/left handles.

**Explicit requirement: retain axis text.** Show **ENEMY HEALTH** beneath the horizontal native X axis and **ENEMY GUN DAMAGE** vertically beside the native Y axis; show the real minimum/midpoint/maximum tick labels for both. Do not hide, abbreviate or replace axis names with a tooltip, remove the stat cards, or repeat instructional paragraphs. The current values remain in the stat cards (gold only when modified), so axis titles need no extra duplicate values. Keep the centered joint handle (both sources), bottom handle (health only), left handle (gun damage only), subdued per-difficulty reference crosshair, and all original values and native input/CFG/reset/preset behavior. The measured board height is **449 DIP** so the bottom-axis labels fit *inside* the industrial frame; do not clip the labels below the 2D plot. This is presentation-only and supersedes prior axis-text removal in the earlier solution 2 description. Local publish/Wine visual acceptance is pending.


### 2026-10-12 — Human Combat: Squad Pressure Dashboard (CURRENT)

The approved **solution 2 / Squad Pressure Dashboard** supersedes the previous oversized Human Combat Profile graph-first layout. `HumanCombatProfile` remains one bounded **414-DIP** editor with all five native independent CFG sources. On the LEFT, two compact, read-only live stat cards show **ENEMY HEALTH** (`combat.npcHealth`) and **GUN DAMAGE** (`combat.npcWeaponDamage`) above a smaller **BALANCE EDITOR** 2D graph; values use selected-reference-aware gold only when modified. The graph retains the original source X/Y ranges, exact ticks/steps, grey selected-difficulty reference crosshair, and three actual edit handles: bottom = health only, left = gun damage only, joint center = both. The two stat cards do not introduce duplicate editable controls. The obsolete lengthy vertical gun-damage caption and redundant graph-top health caption are removed.

On the RIGHT, **OFFENSIVE PRESSURE** groups three compact real-source slider cards: **ATTACK FREQUENCY** (`combat.npcAttackCooldown`), **FIRE ACCURACY** (`combat.npcHipAccuracy`) and **EXPLOSION DAMAGE** (`combat.explosionDamage`). Existing native slider hit regions, right-click/R resets, mouse and keyboard interactions, reference ticks, selection/help routing, and preset/CFG writer mappings must stay intact. Preserve the common Stamina Usage value/label alignment and subdued reference marks; do not display redundant base labels or explanatory paragraphs. This is a layout-only change. Local compile and Wine visual acceptance remain pending.


### 2026-10-12 — Quick Melee terminology (CURRENT)

The Stamina Usage melee action with native parameter ID `stamina.meleeNormalCost` must be labeled **QUICK MELEE**, never **NORMAL MELEE**, in the visible radar. Its catalog label, description, rationale and value label use **Quick Melee** as well. The underlying source ID intentionally stays `stamina.meleeNormalCost` for preset/CFG compatibility. This is a terminology-only change: no source values, sliders, steps, interactions, or writer semantics change.


### 2026-10-12 — Energy Drink terminology (CURRENT)

Every visible control label for the energy-drink consumable must say **ENERGY DRINK**, not bare **ENERGY**, to avoid confusing the item with stamina or energy. In Survival > Fatigue, the source ruler reads **ENERGY DRINK FATIGUE RELIEF**. In Consumables this applies to the Water + Energy Drink refill knob, Energy Drink stamina regeneration knob, Energy Drink timeline name, and the two Energy Drink stamina-cost-duration rows. Matching player-facing parameter-catalog titles and help use the same naming. Native parameter IDs, effect-prototype symbols, exact values, ranges, presets, engine references, interactions and CFG writer are unchanged. Do not rename generic player energy/stamina concepts or technical game identifiers to the consumable label.


### 2026-10-12 — Combined Flycatcher / Special Anomaly Effects (CURRENT)

The approved Flycatcher **Movement Profile (option C)** and **Special Anomaly Effects group layout (option C)** now share **one visible compact editor** under the original Flycatcher anchor. The 2D Flycatcher movement/turn-speed plot is positioned on the LEFT, preserving the native independently draggable X/Y axis handles and the joint center handle; the RIGHT area groups **BLEEDING** (Razor and Diamond source sliders in two small side-by-side cards) above a full-width **PSY DRAIN / INTENSITY** source slider. This eliminates the formerly independent Special Anomaly Effects full-width panel and its uneven 2+1 layout. Each of the five CFG sources remains independent, writable and selectable for its own help, with native source ranges, steps, reset, keyboard and preset semantics. The combined control measures dynamically to a compact minimum 348 DIP; controls are bounded, never stretched into another full-width section. The effect sliders retain their existing native `DrawEconomyCard` interaction renderer. Changes are presentation/grouping only, not new game mechanics.

In Survival > Hunger, the upper Well-Fed ruler's visible label is now **WELL-FED STAMINA RECOVERY**, explicitly identifying the stamina effect. Source ID, percentage, reference, native step and gameplay behavior are unchanged.


### 2026-10-12 — Anomaly Damage: gamer-first FINAL / BASE presentation (CURRENT)

The one-card `AnomalyDamageRadar` keeps its centered native GLOBAL 270° knob and eleven independent native vertical anomaly sliders. Supersede the old three-line technical footer (`RESULT = SOURCE × GLOBAL`, bar-scale explanation, top/lower-number instructions) with three compact in-context headings: **ALL ANOMALIES**, **Scales damage across every anomaly**, and **INDIVIDUAL DAMAGE · BARS VS VANILLA**. No redundant help paragraphs or additional controls are added.

Every individual column explicitly labels its top computed number **FINAL** and lower editable source number **BASE**, directly adjacent to the respective numbers. Here BASE means the *current, editable individual source strength*, **not** the unchanged selected-difficulty vanilla/reference value. FINAL is the existing individual source × global multiplier *before protection*, not a second writable source and not a guaranteed in-game damage number after protection. The vertical bar and grey reference marker still express the individual source as % of its own selected-difficulty vanilla value, excluding global. Razor keeps its genuine multiplier unit. Keep the previous 606-DIP board, native bounds/steps, value-based accent rules, selection, drag, keyboard, wheel, right-click and R resets, exact parameter IDs, preset round trips, and CFG writing unchanged. The richer formula explanation is available on hover and in context help rather than as constant on-screen prose. This is UI-only.


### 2026-10-12 — Exhaustion & Recovery: gamer-first Stamina States panel (CURRENT)

**Build regression guard (2026-10-12):** The Stamina States help tooltip in `ParameterViewModel.cs` is one C# `return` expression with its optional `Environment.NewLine` continuation before the final semicolon. Do not leave the retired recovery-group tooltip as a standalone `+ Environment.NewLine` statement after that semicolon: it causes `CS0023` and `CS0201`. The static consistency audit checks this exact invalid pattern. This is a compile-only fix; no on-screen placement, input, preset, CFG or gameplay behavior changes.

The approved **solution 1 / STAMINA STATES** supersedes ALL previous six-identical-card and generic slider-bank layouts. `ActionCostsRadar` with `Mode="Exhaustion"` displays **one STAMINA ZONES card on the left** and **three gameplay-context groups on the right**; there are still exactly six writable CFG source parameters. Stamina Usage (`Mode="Usage"`) and Water Drain are untouched.

**STAMINA ZONES:** one real common **0–60 SP** horizontal ruler, without duplicate horizontal full-width tracks. Two vertically separated, independently draggable source handles straddle the same line. Above: **SPRINT LOCKS** (`stamina.sprintDisableThreshold`, native 0–32.2 SP, original 16.1); below: **RECOVERY KICKS IN** (`stamina.lowRecoveryThreshold`, native 1–60 SP, original 30). Each has a clearly associated label and actual SP value, distinct above/below hit lane and native-range bound. Top-side input beyond 32.2 SP and bottom-side input below 1 SP remain passive. The handles may occupy the same horizontal X without merging, cascading or coupling their actual values. Show only shared subdued `0 SP` / `60 SP` scale endpoints, real muted reference ticks and tasteful distinct upper/lower gold traces. No simulated stamina dynamics or claims about the exact order in which thresholds fire.

**Context groups:** to the right place **AIMING** (one **REGEN PENALTY** %-source `stamina.aimRegenCoef`), **AFTER EXHAUSTION** (one **REGEN DELAY** s-source `stamina.sprintRecoveryDelay`), and **LOW-STAMINA BOOST** (two compact side-by-side **STRENGTH** `stamina.lowRecoveryStrength` in SP/s and **DURATION** `stamina.lowRecoveryDuration` in seconds). Every independent value has its native editable slider, 1:1 parameter ID, original steps/ranges, gold-only-changed numeric readout, muted exact reference tick and small real-unit min/max endpoints. Group headings supply context instead of verbose repeated control labels or instructional paragraphs; only these four simple source tracks remain, not another six identical boxes.

**Measured layout:** for control widths **≥780 DIP**, one **268-DIP tall** left Stamina Zones board is paired with a vertically arranged right group (70-DIP AIMING, 70-DIP AFTER EXHAUSTION, 108-DIP LOW-STAMINA BOOST, 10-DIP gaps), all inside a **330-DIP** outer panel. The left area is at most 340 DIP and 40% of available width; the right gets the rest. Below 780 DIP, stack a **192-DIP** full-width Zones card above two **82-DIP** half-width Aiming/After Exhaustion cards and a **94-DIP** full-width Boost card; measured total panel height **450 DIP**. The main heading stays centered. The renderer must not stretch cards or use duplicate filler text.

**Interaction invariant:** keep native snap/keyboard arrow keys, exact selected-parameter ownership, per-handle right-click reset, family-scoped R reset, preset persistence and original CFG writer transforms. The selected source stays independent of the other five, regardless of coincident zone marker positions. UI description/catalog labels for the two zones now read **Sprint Locks** and **Recovery Kicks In**, and the boost members are **Boost Strength** and **Boost Duration**. The aiming/exhaustion catalog IDs and source definitions retain their original meaning. No gameplay constants or CFG output logic change.

### 2026-10-12 — Beer radiation removal 3x and Exhaustion naming (historical; Stamina States supersedes layout)

**MEDICAL ITEMS / RADIATION REMOVAL:** Beer now allows **0–30 RAD (0x–3x of 10 RAD vanilla)** at the unchanged 5-RAD step. The real `BeerAntirad1.ValueMin/ValueMax` still receives **-30/-30** via `negateSet` at max. Preserve the four-column 0–200 RAD ruler, handling, highlighting and independent Vodka 0–100 RAD range.

**EXHAUSTION & RECOVERY:** Six real CFG controls keep their established 94-DIP cards, 2+4 layout, responsive alignment, neutral source reference marks and raw ranges. Their on-screen/parameter-catalog titles are **SPRINT LOCK BELOW**, **BOOST ACTIVATES BELOW**, **AIMING REGEN PENALTY**, **EXHAUSTION REGEN DELAY**, **LOW-SP BOOST RATE** and **LOW-SP BOOST DURATION**. The first threshold disables sprint when stamina falls below it; the second threshold controls the separate temporary low-SP `RegenStamina` effect. Aiming penalty reduces ongoing regeneration while aiming. Exhaustion delay affects time before regeneration after sprint lockout; the last two knobs edit the conditional effect's strength (SP/s) and duration (s). No new derived values, extra labels, source changes, larger cards or simulated stamina model. This semantic revision supersedes the 2026-10-11 titles and the earlier Beer 2x ceiling.

### 2026-10-12 — Sleep Health Restored default and range (current)

**Survival → Sleep System → Recovery When You Wake Up:** HEALTH RESTORED starts and resets at **100 HP (1x tool default)**. The compact slider has real HP bounds **0–200 HP** in **5-HP increments**; only its two endpoint labels read **0x** and **2x** (relative to 100 HP). A 0x setting writes zero HP restoration when waking up, while the other sleep effects remain independently controlled. Its current readout shows actual **+HP**, not a fictitious effect multiplier. Original `SleepHealthRestore.ValueMin/ValueMax = 300` remains in the immutable vanilla catalog/source manifest for correct BPatch diffing. Because the tool deliberately starts at 100 HP, **both source Value fields must be emitted as 100** for an untouched new preset. Original 300 HP is off-scale; do not draw a misleading gray reference tick at 200 HP. Reset returns to the tool's 100-HP default, and old presets above 200 HP clamp on import. The adjacent stamina strength slider, UI geometry and all other gameplay parameters remain unchanged. This rule supersedes older 0–600 HP editor descriptions.

### 2026-10-12 — All rotary knob readouts vertically centered (current)

**Project-wide 270° knob grammar:** all one-line numeric center readouts in the shared `AggregateVisualControl.DrawSourceKnob` renderer use the WPF text top-edge position **`center.Y - 8 DIP`**, replacing the legacy **`center.Y + 3 DIP`** (an 11-DIP lift). This applies to all native multiplier/percentage/signed-percentage knobs across Combat (Your Weapons, Psy), Survival (Equipment Wear, Radiation, Consumables), Loot (Stash and Ammo Family Weights), Economy (Service Prices, Trader Stock), Master penalties and any other call site. **Two-line center readouts** (Vinca/Hercules/Psi Block amount + unit and Radiation Recovery value + `PTS/S`) use a shared **number top `center.Y - 15 DIP` / unit top `center.Y + 5 DIP`**. No per-consumer readout-offset argument or duplicate renderer is needed. The separate `AnomalyDamageRadar.DrawGlobalDamageKnob` uses a truly vertically centering `DrawCenteredText` helper, so its text must be anchored at **`(center.X, center.Y)`**, without the old extra +3 DIP. Keep horizontal centering, ring/arc geometry, tick marks, source values, highlighting, unit styling, card sizes, hit regions and all CFG/preset behavior unchanged. Do not move title text or min/max footer marks.

### 2026-10-11 — Matched Hunger/Fatigue primary resource rulers (current)

The **first two controls** in both **Survival → Hunger** and **Survival → Fatigue / Sleepiness** must reuse the *actual* `DrawCoreResourceRuler` from **Core Health / Core Stamina**, not smaller generic `DrawCompactTrack` cards or lookalikes. Each pair consists of two equal 105-DIP native-resource cards on a centered row (margin 16 DIP, 12-DIP gap, top 51 DIP), using the same 19-DIP primary readout (16.5 DIP only where native 0.001 rate precision requires space), 12-DIP label, same rail/handle geometry, native min/max endpoints, subdued reference tick and **only changed digits yellow**. No highlighted Vanilla/Base label, extra explanatory lines or full-width cards. At minimum window width, abbreviate the titles rather than let label and value overlap.

**HUNGER:** `HUNGER RATE` is `survival.hungerRate`, real **0.000–0.030 pts/s** (original 0.015, step 0.001); `WELL-FED RECOVERY` is `survival.wellFedStaminaRegen`, displayed as **+20%** of the original Well-Fed stamina-recovery bonus (0–40%, step 1 percentage point). Its raw `0.2` must **never** appear as “0.2%”; endpoint labels remain 0%/40%. **FATIGUE:** `FATIGUE RATE` is `survival.sleepinessRate`, **0.000–0.020 pts/s** (original 0.010, step 0.001); `ENERGY FATIGUE RELIEF` is `survival.energyDrinkFatigueRelief`, genuine **0–2x** multiplier (original 1x, step 0.01). These controls keep their independent CFG/preset mappings and each slider's original native snapping, wheel/keyboard, reset and hit targets.

The existing Hunger stage preview, Hunger Survival Profile, Fatigue stage preview and Fatigue Combat Profile stay intact. Their top coordinates and the aggregate component's measured heights must be shifted together to begin below the new 105-DIP ruler row (shared preview top `51 + 105 + 12 = 168 DIP`), without blank gap or overlapping captions. The two top rows may show different units because the underlying CFG source types genuinely differ, but their **visual grammar and placement must be identical to Core Health/Core Stamina**. This is a presentation change, not a difficulty/CFG transformation.


### 2026-10-11 — Consumable strength labels and shared Override Limits (current)

**Survival → Consumable Effects:** The three top 0–2x source-backed knobs must be self-explanatory without additional filler text. Label them **WATER + ENERGY REFILL** (immediate SP refill by Water, Energy Drink and Limited Energy Drink), **WATER REGEN + COST CUT** (Water's *two linked* sustained stamina regeneration and lower per-action stamina drain), and **ENERGY STAMINA REGEN** (normal Energy Drink's sustained stamina regeneration). Their 1x numeric readouts follow the **project-wide single-line center placement** (`center.Y - 8 DIP`), also used by every other shared rotary source knob; no special drink-only offset. Keep horizontal centering, knob circles, title positions, reference ticks and limits unchanged. Their 0.05x edit steps, real `scale` CFG operations, individual preset IDs, compact aligned 3-knob layout, neutral base ticks and changed-only yellow values remain unchanged. `ControlHelp` and catalog labels/rationale must agree. These **are** multipliers, unlike the three source-native point/kg medicine knobs below the duration graph.

In the existing single **EFFECT DURATION / SECONDS** frame, remove the gray `MEDICINE` section caption (not the three medicine lane names or the second calibrated seconds axis). `OVERRIDE LIMITS` applies to **all eleven** real existing duration sources through the same backward-compatible UI-only preset flag `drinkDurationOverride`, with no new CFG field. OFF preserves the five original per-drink main maxima and the three short Water/Energy child maxima linked to their parents, plus medicine maxima **Vinca 540s / Hercules 900s / Psi Block 300s**. ON keeps the drink axis at **0–270s** and allows all three medicine durations to reach **900s (15 minutes)** independently (Hercules already has that native limit); the medicine axis remains **0/300/600/900s** in both modes. Turning ON does not change current durations; turning OFF clamps only values above the appropriate native maximum and respects child≤parent. Medicine durations retain 5-second steps and independently writable original `Duration` CFG targets; Hercules' two effects always move together. Do not introduce a second checkbox, duration panel, fake duration multiplier, unitless source label or full-width control. These are editor ranges, not a newly verified in-game duration cap.

This supersedes historical statements that override is drink-only or that medicine override reaches 1800 seconds.


### 2026-10-11 — Medicine knobs show actual game-effect units (current)

In **Survival → Consumable Effects**, the three bottom medicine strength knobs must display concrete **source-native bonuses**, never `x`, vanilla percentages or implied regeneration per-second rates. `VINCA BLEED REGEN` displays **+10 BLEED PTS** by default (native source 10, range 0–30, step 1); `HERCULES CARRY` displays **+20 KG** (native 20, range 0–60, step 1); `PSI BLOCK PROTECTION` displays **+10 PSI PTS** (native 10, range 0–30, step 1). Vertically center the signed number and muted, smaller unit **as one two-line block** inside each existing 270-degree dial: WPF text tops are `center.Y − 15 DIP` for the number and `center.Y + 5 DIP` for the unit, lifting both 11 DIP compared with the previous low alignment. Horizontal centering, line spacing, ring geometry and non-medicine knob readouts do not change. Only modified **numbers** become yellow; units and reference ticks remain neutral. The existing min/max scale endpoints stay compact. Do not add text cards, second controls or alter the graph placement.

These are the effect **amounts** exposed by the game's consumable descriptions: Vinca's bleeding regeneration, Hercules' additional carry kilograms and Psi Block's psi protection. Source IDs remain `CinnamonDegenBleeding`, both paired `HerculesWeight` effects and `PSYBlockerIncreaseRegen`. Setting +20 KG writes 20 to **both** Hercules effects, not 40 or a percentage. For the bleeding and psi effect, game-facing +10 points does **not** assert a particular damage/healing tick frequency or rate per second; that runtime detail remains unverified. Preserve all original `ValueMin/ValueMax`, duration, `KeepNew`, preset and right-click reset semantics. `CurrentDisplay`, catalog rationale, visual controls and production regression contracts must agree on the units.


### 2026-10-11 — Consumable Effects: compact knobs below duration graph (current)

**Survival → Consumable Effects:** Delete the entire passive `LIVE EFFECT PREVIEW` stamina graph and its obsolete renderer. Keep the existing three native drink-strength knobs (Instant Stamina, Water Buff, Energy Regen) together at Y=53 DIP, preserving their CFG/preset mappings and interactions. Put the **single shared `EFFECT DURATION / SECONDS` frame immediately below** at Y=250 DIP, with the five existing drink duration lanes on their 0–270 s clock, three real medicine lanes (Vinca, Hercules, Psi Block) on an independent, labeled 0–900 s clock **within the same frame**, and all three original short Water/Energy duration tracks at the foot of that frame. Existing duration step/limit rules and draggable targets remain unchanged; the shared `OVERRIDE LIMITS` checkbox also controls medicine caps (native OFF, 900s ON).

**Medicine effect strengths below durations:** render exactly three source-backed frameless 270° knobs in one compact centered row, **below the full duration frame**, not between the drinks and the frame. Labels are `VINCA BLEED`, `HERCULES CARRY` and `PSI BLOCK`; their real 1-point native steps, max-620-DIP knob bank, 181-DIP knob card height, neutral vanilla tick, gold-only-when-changed current values and per-knob resets are unchanged. The medicine row's position is computed from the responsive duration chart's bottom: `ConsumableDurationTop + ConsumableTimelineHeight(width) + 14 DIP`. MeasureOverride must return that top + 181 + 14 DIP; never clip the controls or reserve the now-removed preview's empty vertical space. Vinca/Hercules/Psi Block magnitudes, Hercules dual-CFG coupling, six preset IDs and eight source-backed duration lanes remain intact. No duplicate medicine sliders, separate duration cards, redundant text, or full-width controls.

**Supersedes:** previous live stamina preview and medicine-knobs-above-duration layout. This is a UI-only rearrangement; no CFG gameplay parameter, effect duration, or output mapping is altered.


### Superseded: medicine knobs above chart with passive stamina preview

**Resolved missing controls:** the six medicine IDs must be instantiated by `MainViewModel.consumableAggregateParameterIds`, not merely listed in the catalog, visual family dictionary, or CFG writer. The visible `ConsumableMixer` anchor binds all six hidden-but-editable source VMs. Missing one is a failed runtime UI contract, not permission to show an empty board.

**One combined editor:** preserve the original three drink strength knobs at Y=53, existing six-bar live drink preview at Y=250 and all original drink-duration sources. Directly below preview, put **three frameless 270° raw-strength knobs on ONE centered row** (medicine top Y=474; shared maximum bank width 620 DIP): **VINCA BLEED** (raw 10, 0–30/1), **HERCULES CARRY** (raw 20, 0–60/1) and **PSI BLOCK** (raw 10, 0–30/1). These edit `ValueMin/ValueMax`, and Hercules updates both existing carry/weight-penalty prototypes. Only changed numbers go gold; no “x”, invented percentages, large medicine card, redundant value labels or additional standalone medicine panel.

**Effect Duration:** the **existing SINGLE** `EFFECT DURATION / SECONDS` frame now starts at Y=670 and contains **eight direct draggable lanes**: first the original Water, Energy, Vodka, Beer and Magic Vodka rows on the unchanged **0–270s** clock; then Vinca (180s default, 0–540s native), Hercules (300s, 0–900s) and Psi Block (60s, 0–300s) on a visibly separate **0–900s MEDICINE ruler inside the same frame**. The second scale is necessary for honest source seconds without compressing the drink handles; there is no second boxed medicine graph and no duplicated duration slider. Every medicine lane has left item label, right-aligned actual seconds, current editable gold rail/handle, neutral reference tick, own native maximum, mouse drag, arrow-key step 5s, right-click reset and family R reset. Keep the original three Water/Energy short-effect duration cards **inside the same frame**, shifted below the new medicine lanes, still bound to their own drink parent as before. Frame measurement and renderer agree at narrow widths; all paths remain independently owned.

`OVERRIDE LIMITS` remains a **drink-only** editor mode, never caps medicine to 270s. Preserve presets, source magnitudes, linked Hercules effects, original `KeepNew` behavior and existing CFG writes. This supersedes the earlier detached three medicine cards and the old requirement that the duration renderer remain literally unchanged.


### Superseded medicine layout (historical)

The previous detached Medicine Effects panel and three extra duration sliders were replaced by the current combined Consumable Effects design specified above.

### 2026-10-11 — Starvation inactive-state readability; medicine candidates (current)

**Master → Starvation Penalties:** when the authored **Starvation Mechanics** parent is OFF, all five child source values **remain disabled/non-editable** and receive no editable gold track/handle or hover hit targets. Their **card captions and current numerical readouts** must nevertheless use neutral high-legibility text (`CloneWithOpacity(text, 0.75)`), not the much dimmer common `muted` brush that made the entire Starvation section appear blacked out compared with the other Master panels. Source rails, grey native references, parent toggle behavior, preset and CFG writes remain unchanged. Enabled and modified states continue to use the existing normal/changed-only accent convention. This is a presentation-only correction to `DrawMasterNativeCard` scoped to `StarvationTimeline`, not a forced activation of Master-only gameplay mechanics.

**Medicine effects are now implemented:** the six source-backed Vinca, Hercules and Psi Block magnitude/duration controls live inside Consumable Effects. See the current medicine UI contract at the top of this catalog and the CFG mapping audit.

### 2026-10-11 — Paired Master Radiation / Low Health penalty knobs (latest authoritative layout)

The **RADIATION PENALTY** (`MasterRadiationPenalty`, group `Radiation`) and **LOW HEALTH PENALTY** (`MasterLowHealth`, group `Low Health`) sections retain their **separate** original parent mechanics toggles, two independent source parameters and normal Master-section heading style. Their former horizontal penalty sliders are replaced by the **same frameless 270° `DrawSourceKnob` family** used throughout the application (native-step-aligned graduated arc, yellow current arc, muted source/reference tick, interactive angular dragging, wheel/keyboard and right-click per-source reset). Both knobs use a centered **204 × 181 DIP** maximum local footprint and source-native **0.01 steps over 0–1**; their visual endpoints and center readout use explicit **signed percentages**:

- **ACTION STAMINA COST** (`survival.radiationStaminaDrainPenalty`) displays **+0…+100%** while editing the exact native 0…1 modifier (original Master reference 0.5 = +50%).
- **HEALTH RECOVERY LOSS** (`health.masterLowHealthPenalty`) displays **−0…−100%** while editing the exact native 0…1 modifier (original Master reference 0.7 = −70%). The original 30-HP activation threshold is not altered.

These **two independent Master group sections sit beside each other in one responsive row** (Radiation left, Low Health right) when the section host is at least **740 DIP** wide, with **12 DIP** separation. Below 740 DIP they remain full-width **stacked in the same order** to prevent clipping. This pairing is restricted to those two adjacent group identities in `ResponsiveSectionGridPanel`; all unrelated top-level sections still occupy their own rows. Each conditional group's native height is **298 DIP**, including the centered section heading, unchanged **39-DIP** mechanics toggle at Y=52, and centered dial starting at Y=102. The pair is not one new CFG field or combined switch, and it must never occupy the entire row per knob when there is enough room.

**Behavior invariant:** Both existing toggles still gate their own knob's enabled styling and interaction (`IsControlEnabled`); their Source ID / `AdditionalMechanicsEffects` membership, selected difficulty default, native min/max/step, current/reference values, preset serialization and BPatch writer operations are unchanged. Only source-value **numbers** turn yellow on modification; static `+`/`−` and % units and subdued Base tick do not need separate emphasis. No redundant explanation/Vanilla value below the controls. This specification supersedes older 204-DIP standalone horizontal penalty sections and 440-DIP slider-bank directions.

### 2026-10-11 — Compact Movement Limits and gamer-facing Exhaustion labels (historical; later Stamina States supersedes Exhaustion layout)

**MOVEMENT LIMITS** in `StaminaCore` is now **two matching compact horizontal 116-DIP cards**, not a tall vertical vault bar paired with an oversized carry chart. **MAXIMUM VAULT HEIGHT** (`stamina.vaultMaxHeight`, native **70–300 uu**, 1-uu steps, vanilla **130 uu**) and **MAXIMUM CARRY WEIGHT** (`stamina.maxCarryWeight`, native **20–160 KG**, 5-KG steps, vanilla **80 KG**) share the **same title-left/value-right, rail, source-reference, handle and endpoint geometry**. Each card starts at **Y=191**, with its rail centered at card top +51, a 12-DIP sibling gap, and native min/max units in the bottom footer. The vault rail retains three **source-authored, clickable exact-value landmark captions**: **WAIST=130**, **EYES=225**, **ABOVE EYES=300**. At narrow widths ABOVE EYES drops one short label line to prevent overlap; clicking captions snaps the unchanged underlying source value exactly, not an approximation. The carry rail has only three intermediate kg calibration ticks (native quarter increments); it **does not** duplicate a separate “LOAD CAPACITY / KG” caption or suggest fabricated fatigue thresholds. Only a modified numeric token is highlighted, never its `uu`/`KG` unit or Vanilla/Base tick. Board height is **323 DIP** rather than 382, retaining the two top CORE STAMINA 105-DIP cards unchanged. Existing pointer, keyboard, right-click per-source reset, `R` family reset and original four CFG/preset mappings are preserved.

**EXHAUSTION & RECOVERY** retains its six independent source-native sliders and the 2+4 arrangement, 94-DIP card grammar, shared 0–60 SP *visual ruler* for the thresholds, real Sprint Lock **0–32.2 SP** interactive range, and compact responsive wrapping. The six current visible names, and corresponding catalog `label` metadata, are:
- **SPRINT LOCK BELOW** — `stamina.sprintDisableThreshold`: below this SP level sprint locks
- **BOOST ACTIVATES BELOW** — `stamina.lowRecoveryThreshold`: below this SP level the separate stamina boost can start
- **AIMING REGEN PENALTY** — `stamina.aimRegenCoef`: percent stamina regeneration penalty specifically while aiming
- **EXHAUSTION REGEN DELAY** — `stamina.sprintRecoveryDelay`: seconds before regeneration after sprint exhaustion
- **LOW-SP BOOST RATE** — `stamina.lowRecoveryStrength`: independent low-SP regeneration boost strength (SP/s)
- **LOW-SP BOOST DURATION** — `stamina.lowRecoveryDuration`: time the low-SP boost lasts (seconds)

The names are user-facing wording only: no source range, step, transformation, unit, writer, constraint or save format is modified. Supersede the older vertically oriented Vault Height design, 175-DIP Movement card, 382-DIP StaminaCore height, and technical Exhaustion captions in dated catalog sections below.

### 2026-10-11 — Frameless knobs, Equipment in Survival, expanded drink strengths (current)

**All rotary knobs:** The common `AggregateVisualControl.DrawSourceKnob` renders every native 270° knob without a rectangular card fill or border. Preserve dial face, selected/hover highlighting, native source-grid ticks, left/right limits, current value, neutral reference marker, pointer/wheel/keyboard interaction and right-click reset. Applies to Your Weapons, Radiation System, Consumable Effects, Ammo Family Weights, Economy, Trader Stock and all other consumers of this renderer. Do not add extra frames, filler text or full-width control backgrounds.

**Equipment now lives in Survival:** Remove the separate Equipment navigation tab. Under Survival place the two original groups after Bleeding: **Equipment Wear** keeps its compact two-axis original armor durability (`equipment.armorDurability`) and player weapon wear per shot (`equipment.weaponWearPerShot`) editor; **Item Weight** keeps the two original source-native weight controls (`equipment.ammoWeight`, `equipment.consumableWeight`). Only the catalog UI tab assignment for those four changes from `equipment` to `survival`. Preserve source IDs, values, order within each group, steps, reference handling, CFG targets, writer operations and preset compatibility. No duplicate equipment tab and no duplicate controls.

**Alcohol Tolerance:** Beer Strength (`survival.beerDrunknessStrength`) now spans **8–40** in **1** point increments (vanilla **15**); Vodka Strength (`survival.vodkaDrunknessStrength`) spans **15–80** in **5** point increments (vanilla **30**). The native catalog controls both real editing bounds and the DrunkennessControl preview/labels. These maxima are user tuning limits only; original DrunknessMechanics stage thresholds and the beer/vodka CFG effect targets remain unchanged.

This active design supersedes historical rectangular knob banks, Equipment tab navigation and 30/60 drink strength ceilings.

### 2026-10-11 — Exhaustion and water native-card redesign (historical; Exhaustion board superseded)

**EXHAUSTION & RECOVERY:** The six current, independently writable `ActionCostsRadar` / `Mode=Exhaustion` sources are shown in **six uniformly formatted 94-DIP native-track cards**. The two left-hand stacked cards are **SPRINT LOCK** (`stamina.sprintDisableThreshold`) and **RECOVERY BELOW** (`stamina.lowRecoveryThreshold`), each with its current value aligned to the right, its own 0–60 SP physical ruler, subdued source-reference tick, and explicit 0 SP / 60 SP footer. The Sprint Lock portion beyond its native **32.2 SP** maximum is passive/non-editable, and Recovery Below retains its native **1–60 SP** active span. The right-hand 2×2 cards are **AIM RECOVERY PENALTY** (`stamina.aimRegenCoef`, %), **RECOVERY RESTART DELAY** (`stamina.sprintRecoveryDelay`, s), **LOW-SP RECOVERY BOOST** (`stamina.lowRecoveryStrength`, SP/s), and **LOW-SP RECOVERY DURATION** (`stamina.lowRecoveryDuration`, s). Each uses the same header/readout, horizontal source-native rail/handle and own min/max footers. Numeric highlights remain limited to values modified from their reference, titles and units stay neutral, and long titles are locally ellipsized without crossing the value column. The redundant STAMINA THRESHOLDS section subtitle is removed. At viewport width >=780 DIP, thresholds occupy a bounded left column (38% up to 340 DIP) and the four right cards share the wider remaining area; height **265 DIP**. Below 780 DIP, the threshold pair stacks above a two-column recovery grid; height **475 DIP**. The six original source ranges, steps, click/drag, keyboard and per-source reset semantics are unchanged. The separate STAMINA USAGE board is untouched.

**WATER STAMINA DRAIN:** Preserve the two independent source editors `stamina.waterMediumDrain` (native **0–2×**) and `stamina.waterStrongDrain` (native **0–3×**) with the existing **Medium <= Deep** value-based cascade. Both are now compact **105-DIP primary resource rulers** rendered through the exact `DrawCoreResourceRuler` grammar used by CORE HEALTH and CORE STAMINA: same title/value row, 19-DIP current numeric, 5-DIP filled track, 7-DIP handles, muted reference tick and 10.4-DIP native min/max labels. Readouts and endpoint units are **1x / 2x** (not “x drain” or “x x”), and only modified numeric tokens turn gold. Two equal cards start at Y=51 with a 12-DIP gap in a **171-DIP** board. No extra instruction labels, full-width controls or synthetic percentages. All native edit/reset and linked-source mapping behavior remains unchanged.

This specification supersedes historical 240/419-DIP Exhaustion and 149-DIP Water card layouts in older dated passages below; the underlying writer/preset/CFG contract is identical.

### 2026-10-11 — Core Stamina matches Core Health (current authoritative layout)

The existing `StaminaCore` family is rendered under **CORE STAMINA** (replacing the former **STAMINA & MOVEMENT** outer title and redundant **STAMINA CORE** subheading). The top two 105-DIP primary resource cards are **MAX STAMINA** (`stamina.maxSP`, native **SP**) and **BASE RECOVERY** (`stamina.regeneration`, native **SP/s**). They share the **exact** `DrawCoreResourceRuler` renderer also used by the Core Health MAX HEALTH / PASSIVE RECOVERY cards: the same 12-DIP heading at +15/+12, 19-DIP right-aligned current value at -15/+6, 5-DIP horizontal rail and 7-DIP handle at card top +65, muted reference marker, and 10.4-DIP native min/max labels at bottom -20. Only edited **numeric tokens** become gold; neutral units, scale ticks and baseline markers remain subdued. Native SP-capacity and regeneration values/ranges/steps are not interchanged.

The new layout uses the Core Health header position (Y=21) and identical two-card geometry (Y=51, H=105, gap=12). The next line is **MOVEMENT LIMITS** (Y=173), followed by the **unchanged native vault-height vertical ruler** and **calibrated maximum carry-weight rail** (Y=191, H=175, gap=12). These lower controls retain their exact clickable landmarks, underlying `stamina.vaultMaxHeight` / `stamina.maxCarryWeight` values, drag/keyboard/reset and reference semantics. Board height is **382 DIP** (366-DIP bottom plus 16-DIP padding). There is **no duplicate heading**, no new gameplay control, no full-width slider and no change to the separate Stamina Usage panel.

All four parameters keep their original per-source mappings, preset serialization, validation, selection, reset, and CFG writer operations. This overrides older 351/336-DIP and 72-DIP StaminaCore visual descriptions without changing their gameplay audits.

### 2026-10-11 — Mutant, consumable and sleep labels / preview direction (current)

- The single `MutantHealthRadar` panel is titled **MUTANT DAMAGE AND HEALTH** instead of Mutant Properties. The original mutant hit-damage track and fourteen independently editable health tracks remain intact.
- The `ConsumableMixer` read-only LIVE EFFECT PREVIEW uses **WATER STAMINA COST CUT** for the underlying `WaterStaminaPerAction1` action stamina-drain modifier: original native **−10% SPDrain**, displayed as **10%** reduction. Like all other preview bars, this bar and its reference tick fill **left to right** according to effect magnitude. It is not a draggable slider. Only the sign/reading of the preview changes, not the negative source value, shared water knob, writer or effects. Preview captions are locally width-bounded next to their right-aligned percent readout.
- The three compact sliders directly below the duration ruler use exact, source-distinct descriptions: **WATER +25% STAMINA EFFECT DURATION** (`survival.waterRegenDuration`, `WaterStamina2`, original 5s), **ENERGY -100% STAMINA COST DURATION** (`survival.energyFullCostDuration`, `EnergeticStaminaPerAction2`, 5s) and **ENERGY -30% STAMINA COST DURATION** (`survival.energyPartialCostDuration`, `EnergeticStaminaPerAction1`, 15s). `WaterStamina2` is natively `EEffectType::Stamina`, not `RegenStamina`: do not describe it as a verified regeneration-rate effect. The original seconds-range/steps, interactive behavior, reference markers, and preset/CFG routes are unchanged.
- Sleep's wake-up-percent control is **STAMINA RESTORE STRENGTH**: `SleepStaminaRestore` is an authored stamina-effect strength, vanilla **+100%** (raw `1.0`), not 100 additional stamina points, max SP or SP/s. **HEALTH RESTORED** is the original `SleepHealthRestore` **300 HP per sleep completion** (not HP/s); actual recovered HP is bounded by maximum HP. No new sleep simulation or CFG parameter.

This current layout specification supersedes old captions, the inverted negative-preview direction and alternate terminology in earlier dated sections. All controls remain compact; visual and gameplay-value models are not conflated.

### 2026-10-11 — Radiation, Core Health, Stamina & Movement compact pass (current)

**Radiation System:** three independently editable **native 270° rotary knobs on one row**, labeled BUILD-UP (`survival.radiationAccumulation`, 0–2.50×, native 0.01×), RECOVERY (`survival.radiationRecovery`, 0–0.10 **PTS/S**, native 0.01 points per second), and HP DAMAGE (`survival.radiationDamage`, 0–2.00×, native 0.01×). No invented radiation net-dose preview or unified artificial scale. Each knob retains source-derived native tick marks, selection/rationale, mouse drag, wheel, keyboard and independent Base Preset right-click reset. Three 181-DIP dials are centered in a 606-DIP bank in the 242-DIP RadiationBalance card. RECOVERY must not be rendered as a multiplier. This **supersedes** all earlier audit/catalog requirements to render three `DrawNativeGauge` vertical controls or a 308-DIP RadiationBalance board.

**Core Health:** a compact source-specific board. Top row pairs an interactive, native MAX HEALTH HP ruler (`health.maxHP`, no synthetic current-health reading) with PASSIVE RECOVERY (`health.baseRegen`). Both upper 105-DIP cards must use the **same shared primary-ruler renderer**: 12-DIP bold title at +15/+12, 19-DIP right-aligned live value at -15/+6, a source-native 5-DIP rail centered at card top +65 with matching 7-DIP handles and muted reference ticks, and 10.4-DIP min/max labels at bottom -20. Current changed **numbers** alone turn gold (units stay neutral); values retain their own HP or HP/s units, including the native-range endpoints. Card boundaries, margins and heights align at all supported widths; do not render PASSIVE RECOVERY using the small 72-DIP generic horizontal-track grammar. No CFG, editing or reset semantics change. Below, the RECOVERY RESPONSE band has only three source-native independent tracks: RECOVERY DELAY (`health.regenDelay`), RECOVERY MULTIPLIER (`health.regenerationScalar`) and LOW-HP RECOVERY RATE (`health.lowHealthRegen`). These are actual existing CFG parameters. The lower row fits three columns from 700 DIP onward, otherwise wraps 2+1. Height 282 / 365 DIP respectively. **No Medkit Power control** and no fictitious Low-HP threshold or player-body health simulation: medkit efficacy remains solely in existing Medical Items UI.

**Core Stamina:** use the exact CORE HEALTH primary-ruler renderer for both independent MAX STAMINA (SP) and BASE RECOVERY (SP/s) source cards, with the shared 105-DIP height, title/number/rail/endpoints alignment and changed-numeric-only gold styling. Centered title CORE STAMINA replaces the old STAMINA & MOVEMENT title and redundant STAMINA CORE subheader. MOVEMENT LIMITS remains a separate heading below. Put the original native MAXIMUM VAULT HEIGHT vertical ruler on the left of MOVEMENT LIMITS, with existing exact WAIST/EYES/ABOVE EYES landmarks and click/reset mapping unchanged. Replace the right-hand floating carry slider with a same-height **MAXIMUM CARRY WEIGHT calibrated kg bar**: native 20–160 KG, 5 KG steps; live value, selected difficulty reference tick and five evenly-spaced source-based guide ticks are rendered in one 175-DIP card. These ticks communicate source limits, **not** newly invented weight/fatigue/sprint game thresholds. The two lower cards align top/bottom in one 382-DIP board with no large dead space around carry weight. The remaining Stamina Usage board still has exactly six native-source movement/melee sliders in the original two-column MOVEMENT / MELEE groups. It is visually tightened from 207 to 187 DIP with 124-DIP group cards and 31-DIP internal row spacing. The six individual costs, 2× reference ruler and writer/preset mappings are unchanged. All modifications are UI-only; no CFG IDs, values, step, constraints or writer operations have changed.

Validation contract: verify the three active Radiation knobs and native rate units, both source-specific Core Health rows and responsive height, the true vault ruler + calibrated kilogram card with matching measured frame, and all six retained Stamina Usage editable channels. Update the old "Seven source-native visual controls" audit to test the actual current renderer rather than outdated vertical gauges and 336/308-DIP board heights. No existing unrelated sliders are converted.


### 2026-10-11 — Economy alignment and native Starting Coupons rail (latest)

The previous 2026-10-11 **plus/minus Starting Coupons stepper is retired**. A direct, source-native **0–100,000** horizontal amount rail replaces it, with live coupon amount, 0K/25K/50K/75K/100K milestones, selected-difficulty reference tick, pointer drag-to-value and exact **10-coupon snapping** using the generic WPF `HitKind.Horizontal` path. Existing Home/End, arrows, PageUp/PageDown and right-click reset remain available; there are no thousands of required +/- clicks and no special coupon mouse-wheel branch. Main/Side Quest reward meters remain independent.

Economy section captions are now **MONEY FLOW**, **MARKET**, **EXPENSES** — no 01/02/03 numbering. Pairable Money Flow panels and Expenses panels use the existing responsive grid with centered, bounded native controls. Compact CouponFlow two-meter layout begins at 390 DIP, allowing its 274-DIP board to align with the 273-DIP Trading board. Service Costs switches to a single 3-knob row at 390 DIP (242 DIP board), with 2+1 only on smaller widths. The compact Service and Repair content is vertically centered when its section row is taller than its native frame; headers remain aligned at the top. Original source, preset and CFG contracts are unchanged.

### 2026-10-11 — Combined global and individual anomaly damage (latest)

**One visible Anomaly Damage card**, not separate Global and Individual sections: `AnomalyDamageRadar` renders the existing eleven independent source sliders plus a native 270° GLOBAL damage knob (`environment.anomalyDamage`, exact 0–2.5× with 0.01× increments). The global source joins the Individual Anomaly Damage UI grouping but its old stand-alone aggregate anchor is hidden; no CFG ID, writer or modifier is removed.

A **direct explanation in the panel** separates the three values: (1) GLOBAL multiplies all eleven individual CFG sources, (2) upper number `RESULT = SOURCE × GLOBAL` is the computed pre-protection effect (not another writable CFG value), and (3) lower number is the individual editable native SOURCE. The shared chart axis `% VANILLA` represents only each individual source relative to its selected Base value: **100% = original individual source; 200% = twice that source; it does NOT include the GLOBAL knob**. The same chart is intentionally not labeled as raw damage points. Razor's source is a multiplier, all other columns use their actual authored units. Direct individual vertical dragging edits native steps; the knob preserves native ticks, angular drag, mouse wheel, selected keyboard arrows/Home/End/Page keys, independent right-click reset, and grey per-difficulty Base marker. Clicking a control selects its source rationale; `R` resets all twelve sources in the unified card. The combined card is 606 DIP high. Existing effective-value provider drives live upper readouts; runtime engine protection and other effects are not simulated.

This is the current approval contract. Any older catalog instructions demanding two independent Anomaly Damage cards, an Economy numbered heading, or CouponFlow +/- buttons are superseded.


### 2026-10-11 — Economy audit initialization-order fix

The CouponFlow mouse-wheel regression check executes during the Economy audit, **before** the generic rotary audit initializes `_knob_source`. It therefore reads the already-initialized `_economy_aggregate` source text. Do not reference late-initialized audit globals from an earlier check: Python `py_compile` detects syntax errors but not `NameError` caused by execution order. Run the full `python tools/audit_project_consistency.py` after the syntax check. Runtime UI, source mappings, and CFG writers are unchanged.


### 2026-10-11 — CouponFlow wheel-handler compile invariant (CS0136)

`AggregateVisualControl.OnMouseWheel` retains separate input paths for the native 10-coupon counter and the shared WPF rotary knobs. The CouponFlow nested branch uses `couponDirection`; the later generic rotary branch retains `direction`. C# disallows reusing the later enclosing-scope local name inside that preceding nested block (CS0136). Keep the variables distinct. `tools/audit_project_consistency.py` asserts both wheel paths and their native step multiplication, so this compile regression cannot recur unnoticed. No visual geometry, CFG bindings, preset behavior or writer contract is changed.


### 2026-10-11 — Economy three-section dashboard (authoritative layout)

**Approved game-centric Economy layout supersedes older six-full-width section instructions, historical horizontal CouponFlow/ServiceCostsBoard descriptions, and older trader-stock descriptions.** The *UI grouping* is implemented in `ParameterViewModel.Group`, not by altering the source `parameters.json` catalog: all original CFG IDs, difficulty baselines, constraints, native min/max/step and BPatch writers remain unchanged. Only these three Economy group headers are visible:

1. **01 / MONEY FLOW**: compact paired `CouponFlow` and `TradingDashboard` panels. `CouponFlow` uses a real **0–100,000 coupons native 10-coupon stepper** (`economy.startingMoney`); minus/plus buttons, keyboard Home/End/arrows/PageUp/PageDown and mouse wheel operate on `CurrentNumber` with its native `TickFrequency`. The center number is selectable; right-click resets only the selected parameter. There is no fictitious money-progress bar. The two independently editable 0–2×/0.01× MAIN QUEST and SIDE QUEST meters are source-bound to `economy.mainQuestReward` and `economy.sideQuestReward`, not a calculated earning projection. `CouponFlow` measures 274 DIP when width ≥470 DIP, 367 DIP otherwise; current-value text uses gold only if modified. `TradingDashboard` retains the direction-correct opposite BUY/SELL meters and independent minimum accepted condition/broken item value sources, with no invented spread or profit calculation.
2. **02 / MARKET**: existing `ItemPriceScalersRadar` with six independent native 0–2× columns (Mutant Parts writes its own item prototypes), and existing `TraderStockBoard` with two 270° native source knobs (Ammo/Medic, 0.25× step). These larger source graphs remain centrally bounded rather than cramped into half-size editors. No inventing inventory slots/stock odds.
3. **03 / EXPENSES**: compact paired `ServiceCostsBoard` and existing `RepairCurve`. Service Costs are **three 270° native WPF knobs** FAST TRAVEL (`economy.fastTravelCost`), UPGRADES (`economy.upgradeCost`), BASE REPAIR (`economy.repairCostBase`); each uses native 0–2×/0.01× and the existing `DrawSourceKnob` snap/tick/drag/wheel/keyboard/reset infrastructure. **BASE REPAIR displays source multiplier as a percent** (0.70x => 70%), without composing a fictional final repair price. At ≥418 DIP the knobs form one bounded three-column bank; below that they wrap 2+1, centered and height 438 DIP instead of 242 DIP. Repair Curve stays the editable four-point FRIENDLY → NEUTRAL → DISLIKED → HOSTILE source graph with native maximums and existing value-based ordering.

`ResponsiveParameterGridPanel` pairs only the small Money Flow and Expenses visual anchors when the Economy content width is ≥700 DIP; otherwise they stack. The Market six-bar graph and Trader Stock remain intact and centered. Cards do not stretch indefinitely; responsive `AggregateVisualControl.MeasureOverride` uses the actual available width for these four paired Economy families, while leaving other families' established measurement contracts untouched. The group header template in `MainWindow.xaml` reveals only the three Economy group labels and keeps headings for all other tabs unchanged. No redundant explanation is shown inside controls. The rationale panel retains detailed descriptions. Audit must verify both catalog names and active implementation, not historical renderers.


### 2026-10-11 — Seven-controller source audit after Stash Tier knobs

The historical seven-source-visual audit must **not** require `DrawNativeGauge(dc, P(tier.Id), ...)` for Stash Rewards: BASIC / STANDARD / RARE Ammo by Stash Tier were intentionally converted to three native `DrawSourceKnob` WPF controls. The audit now checks all three literal CFG parameter IDs inside `DrawCompactStashRewards`, native knob dispatch and bounded 606-DIP bank, plus the three separate BONUS REWARDS horizontal source controls. Radiation Balance **still** uses `DrawNativeGauge` for its three independent native sources; Repair per Reputation **still** uses its four source-point graph and editable vertical hit regions. Guard the correct family in the shared rotary input gate. The CFG writers, ranges and the UI's existing geometry are not modified by this audit correction.


### 2026-10-11 — Shared rotary input audit validation

A single WPF mouse-wheel gate dispatches the shared `HitKind.Knob` renderer across approved source families: PlayerWeaponProfile, AnomalyGlobalDamage, AmmoDistribution, ConsumableMixer, StashRewardProfile, TraderStockBoard, PsyPhantomPressure, EquipmentWearBalance. The static audit validates each required family by independent membership, so extension does not break existing controller contracts. Check Python syntax (`python -m py_compile tools/audit_project_consistency.py`) before running the consistency audit. A syntax fix to the Psy Combat / Equipment Wear audit does not modify WPF control geometry or native CFG mappings.


### 2026-10-11 — Psy Combat and Equipment Wear rotary banks

Psy Combat replaces the former two-axis square plot and side fader with three independent 194×181 DIP native WPF knobs: spawn-cycle delay, phantom count and gear durability damage. Its bleeding toggle remains independently interactive. The small bar preview is **relative only**, not a calculated spawn timeline or deterministic spawn frequency; no inferred AI scheduling rules. Height 378 DIP. Equipment Wear replaces the combined 2D chart with two independent 194×181 DIP native knobs, armor durability and weapon wear per shot, height 242 DIP. Shared native ticks, snapping, drag, wheel and keyboard handling apply to both families. Original source IDs and writers are unchanged.


### 2026-10-11 — Trader Stock native knobs

Trader Stock is a compact two-column 194×181 DIP pair of WPF 270° source rotary knobs, AMMO STOCK (`economy.traderAmmoStock`) and MEDIC STOCK (`economy.medicStock`). The existing `DrawSourceKnob` native grid, snapping, drag, wheel, keyboard and reference reset apply unchanged. Board height is 242 DIP. These are quantity multipliers, not claims about slot count or restocking. Other economy surfaces are unchanged.


### 2026-10-11 — Ammo by Stash Tier rotary

Stash Rewards uses **three independent, compact native-source 270° WPF knobs** for Ammo by Stash Tier: BASIC (`loot.cheapStashAmmoQuantity`), STANDARD (`loot.mediumStashAmmoQuantity`) and RARE (`loot.expensiveStashAmmoQuantity`). Reuse `DrawSourceKnob` and its native parameter range/step, mouse drag, wheel, keyboard and reference-reset behavior, in a centered bank capped at 606 DIP with 194 × 181 DIP cards. No duplicated informational labels. The separate BONUS REWARDS chance/count horizontal rails and their CFG bindings stay unchanged. The family `StashRewardProfile` must be enabled in the shared knob wheel gate.


### 2026-10-11 — Consumable Effects: three source knobs and six live preview bars (latest authoritative UI)

**User-approved replacement applies ONLY above `EFFECT DURATION / SECONDS` in `ConsumableMixer`.** Remove the three horizontal strength sliders and two passive instant/sustained text-readout cards. Their replacement is **three compact, independently editable 270° WPF rotary knobs in one centered row** (max bank width 620 DIP, shared `DrawSourceKnob`/`HitKind.Knob`) above **one source-bound LIVE EFFECT PREVIEW graph containing six read-only bars**. The knob names and real `GameData/EffectPrototypes.cfg` sources are **INSTANT STAMINA** = `survival.instantStaminaRestore`, **WATER BUFF** = `survival.waterStaminaEffectStrength`, **ENERGY REGEN** = `survival.energyDrinkStaminaRecoveryStrength`. All retain their exact **0–2x / 0.05x native range and steps, Vanilla/Base 1x**, reference tick, centered value, gold-only-when-modified number, correct tick graduation, mouse angular drag/wheel, keyboard, one-source right-click reset and family `R` reset. No new source, writer field or second slider exists.

**LIVE EFFECT PREVIEW** is one frame with two side-by-side subgraphs. **INSTANT STAMINA**: WATER = `+38% × instant multiplier` (scale 0–76%), ENERGY = `+50% × instant multiplier` (0–100%), LIMITED ENERGY = `+100% × instant multiplier` (0–200%). **SUSTAINED EFFECTS**: WATER REGEN = `+25% × water buff` (0–50%), WATER STAMINA COST CUT = magnitude of `−10% × water buff` (0–20% cost reduction, with bar and reference growing *right* from zero while native SPDrain stays negative), ENERGY REGEN = `+200% × energy regen` (0–400%). These are the **exact six preexisting authored derived values**, not DPS estimates, extra effects or inferred effectiveness. Each bar has its own correct scale and muted native reference tick. Preview numbers are gold only when their controlling source is changed; captions remain neutral. Hovering/choosing a knob highlights exclusively its one, two or three linked bars, respectively; there is no preview drag target and no separate persisted selection.

Layout positions: knob cards from Y=53 DIP (height 181); live preview from Y=250 (height 211); the previous entire duration frame starts at the shared **`ConsumableDurationTop = 475` DIP**. `CompactConsumableHeight` and the renderer both use this same constant to avoid cut-off. **The full preexisting duration rendering block after `// A true seconds-space editor` remains literally unchanged**: all five main real-seconds timeline lanes, three independent short-duration sliders, visible second ticks and vanilla markers, 270s optional limits, universal 20-DIP `OVERRIDE LIMITS` checkbox, original snap/dependency logic, CFG writer, and native presets remain exactly as before. The duration section is translated downward only; its internal layout, source IDs and behavior must not be redesigned in this change. Earlier horizontal-strength/text-readout guidance and earlier duration redesign history are superseded ONLY for the upper strength/summary area. Local .NET/Wine and actual-game acceptance pending.


### 2026-10-11 — Seven gamer-centric native visual controllers (latest authoritative UI contract)

Implemented directly in `AggregateVisualControl` as source-backed WPF drawing and hit targets, not fake previews or new gameplay parameters. This latest user-authorized design supersedes earlier slider/card geometry for the exact seven families below; unrelated controls keep their prior design.

1. **Economy / REPAIR PRICE BY REPUTATION** (`RepairCurve`, **324 DIP**): one bounded, interactive **4-point reputation curve**: FRIENDLY → NEUTRAL → DISLIKED → HOSTILE. Points are individually vertical-drag editable on a shared **0–3x display Y-axis**, but remain restricted to each native domain: Friendly/Neutral/Disliked 0–2x, Hostile 0–3x, all native 0.01x steps. Small per-point ceiling notches indicate source-specific maxima; the connecting current polyline and grey dashed actual vanilla reference line are read-only, not separate controls. True current numbers are below each label, gold only if source-modified. Each point has its own source rationale/right-click reset; existing real-value source ordering/cascade in the coordinator is preserved. No fabricated final repair bill.
2. **Economy / TRADING** (`TradingDashboard`, **273 DIP**): two independent **opposed horizontal price meters** in the top row: YOU SELL TO TRADERS (`economy.buyCondition`, 0–2x/0.01) increases rightward, YOU BUY FROM TRADERS (`economy.sellCondition`, 0–2x/0.01) increases leftward. Only the *visual orientation* is mirrored (mouse mapping reverses with it); native CFG values and writer semantics are NOT inverted. Each has an exact per-difficulty Base tick and current number. MINIMUM ACCEPTED CONDITION (`economy.traderMinBuyCondition`, 0–100%) and BROKEN ITEM VALUE (`economy.itemValueFloor`, 0–20%) stay separate native horizontal cards beneath. This is not a simulated bid-ask spread or trade profit estimate.
3. **Loot / STASH REWARDS** (`StashRewardProfile`, **382 DIP wide / 465 DIP narrow**): three equal independent **tier reward rack vertical gauges**, BASIC, STANDARD, RARE, centered as a bounded row; each edits its exact `loot.cheap/medium/expensiveStashAmmoQuantity` original 0.25–2x, 0.25x source, with its native reference marker. The three separate actual generator reward editors stay beneath as compact horizontal controls: RGD-5 CHANCE (0–100%, 0.05 native), RGD-5 AMOUNT (0–4 integer), COUPON CHANCE (0–100%, 0.01). Never infer a guaranteed grenade from a successful-roll amount. No extra tier probabilities or duplicated anchors.
4. **Survival / SLEEP SYSTEM** (`SleepingDashboard`, **370 DIP**): `survival.minimumSleepHours` now uses a compact **14-position stepped hour selector**, positions 1–14 in the native source's exact 1-hour increments; each clickable cell directly edits the same numeric CFG value. Sliding the pointer along the selected row, keyboard arrows/Home/End, exact reference reset, and family R continue to operate. The Vanilla **7 h** cell has a muted reference underline; only modified current values use gold. The other three original controls retain native independent horizontal tracks for sleep eligibility (fatigue points), wake-up HP restored and wake-up stamina restored. No invented sleep quality/HP-rate computation.
5. **Master / SEVERE FATIGUE** (`SevereFatiguePenaltyBoard`, **376 DIP**): five compact native-source **vertical penalty equalizer gauges** in one centered row after the existing universal bundle switch: STAMINA LOST (0–40%, step 1 percentage point), CARRY LOST (0–40%, same), RECOIL (0–110%, same), AIM SWAY (0–0.8x/0.01) and SHOT SPREAD (0–1x/0.01). Each gauge has its own independent normalized 0–100% *track position* representing its personal min/max, with displayed **true source value and unit**, not a shared scalar. Disabled-source handling, individual right-click reset, whole-group R reset, tooltip, writer and dependency toggle behavior are retained.
6. **Survival / RADIATION SYSTEM** (`RadiationBalance`, **308 DIP**): three compact independent **vertical radiation gauges**: BUILD-UP (`survival.radiationAccumulation`, 0–2.5x/0.01), RECOVERY (`survival.radiationRecovery`, 0–0.10 PTS/S/0.01), HP DAMAGE (`survival.radiationDamage`, 0–2x/0.01). Each gauge shows its own native min, max, base tick and live actual unit. No synthetic net-dose or combined health-damage calculation. Full pointer/touch-equivalent mouse editing, keyboard and original per-source CFG writers remain intact.
7. **Stamina / STAMINA & MOVEMENT** (`StaminaCore`, **336 DIP**): Maximum Vault Height becomes one **vertical native Unreal-unit (`uu`) ruler** on the left, editing the original `stamina.vaultMaxHeight` 70–300 uu in exact 1-uu steps. The existing source-marker points WAIST=130, EYES=225, ABOVE EYES=300 are labelled, clickable and separate from the real vanilla Base marker (130). A vertical current cap, 50-uu landmark grid and precise integer current value replace only the old vault slider. Other three independent Stamina Capacity, Base Recovery and Maximum Carry Weight controls remain native horizontal tracks, with Maximum Carry Weight centered vertically beside the taller vault card. No metre conversion or new vaulting capability is invented.

**Cross-family interaction contract:** actual stored `ParameterViewModel` sources own each hit target. Click/drag, native step snapping and arrow/Page/Home/End input, hover only on the real editable handle, source selection/right rationale, right-click one-source reference reset and `R` all-family reference reset must remain functional and accurately reflect dependency-disabled states. Never recolor neutral labels/scale/reference numbers just because a section contains modified values. The original CFG/BPatch writer, source membership, counts, per-difficulty Vanilla/Base presets and saved preset format are unchanged. Local .NET publish, Wine rendering and in-game evaluation are pending.


### 2026-10-11 — Rotary tick marks follow native CFG editing steps (current authoritative rule)

**Applies to all 8 source-backed WPF rotary controls**: Your Weapons (Weapon Damage / Headshot Damage / Jam Frequency), Global Anomaly Damage (Overall Damage Multiplier), and Loot > Breakable Boxes (four Ammo Family Weights). The former hard-coded eleven graduation lines (`tick=0..10`) were inaccurate and are removed. The shared `DrawSourceKnob` computes tick positions as **`Minimum + nativeStepIndex * TickFrequency`**, then uses precisely the same native interval / 270° scale as mouse angular editing and `Snap` (whose origin is the parameter's native Minimum). First and last native boundaries are always drawn; every intermediate drawn line is a real reachable edit step, never an invented 10% slice of the dial. Muted marker positions for the selected vanilla Base Preset are **independent of the editable grid** and are not silently quantized.

Native marker counts: **Weapon Damage** 0.25–2x / 0.25x = **8** marks; **Headshot Damage** 0.5–3x / 0.25x = **11**; **Jam Frequency** 0–2x / 0.25x = **9**; each of the **four Ammo Family Weights** 0–3x / 0.25x = **13**. Every original 0.25x step has one visual mark. **Global Anomaly Damage** 0–2.5x / **0.01x** has **251 reachable values (250 native intervals)** but its ~47-DIP-radius ring cannot fit 251 legible strokes; the shared renderer draws **every tenth native step** (**26 marks at 0.10x intervals**), with major marks at 0.5x, while pointer, wheel, arrow keys and preset values still retain **full 0.01x precision**. The legibility sampling always chooses `1/2/5/10/...` **integer native step strides**, never arbitrary fractional intervals, and ends on real bounds. The outer tick lengths are bounded to prevent clipping of the four narrower Loot cards.

**Important reference exception:** the Novice Base Preset for Weapon Damage is **1.30x**, which is *not* on its user-editable 0.25x grid. Its separate grey reference notch must stay at exactly 1.30x, between neighboring native marks; Reset returns to 1.30x, but ordinary manual click/scroll stays on 0.25x step values. This is deliberate and not a wrongly placed graduation. All CFG writers, per-source ranges, native snapping, the SNG nonempty-pool guard, preview graphs, preset keys and existing input/reset behavior remain unchanged. The static consistency audit now checks all eight native tick grids and prohibits a return to the fixed eleven-line visual scheme. Local .NET/Wine visual acceptance remains required.


### 2026-10-11 — Ammo Family Weights: four native rotary knobs (authoritative)

Inside Loot > **BREAKABLE BOXES**, only the **AMMO FAMILY WEIGHTS** subsection changes presentation: its four independent source-backed horizontal sliders are replaced by **four compact WPF rotary knobs in one centered row**. Labels and order: **9×18 / 9×19**, **.45 ACP**, **5.45 / 5.56**, **12 GAUGE**. Each uses the canonical shared `DrawSourceKnob` renderer and `HitKind.Knob`, the same 270° circular dark face, gold current arc/indicator, muted per-source baseline tick, centered current `x` value (gold only when modified), and muted native endpoints as Your Weapons / Global Anomaly Damage. No additional labels, synthetic odds, or full-width individual controls. The bank is bounded to **780 DIP**, with four equal-width 181-DIP cards and 10-DIP gaps. The **Breakable Boxes board measures 715 DIP** (previous 680) to avoid overlap with the two pool previews.

Original CFG parameters stay **independent, 0–3x, 0.25x native steps**, with authored vanilla 1x: `loot.ammoBoxWeight918`, `loot.ammoBoxWeight045`, `loot.ammoBoxWeight545`, `loot.ammoBoxWeight012`. Mouse angular click/drag, mouse wheel, keyboard arrows/Page/Home/End, right-click one-source reset and R family reset retain existing native snapping. Editing the **last positive SNG member** below one tick must still be corrected via `EnforceAmmoPoolValidity`: SNG cannot become all-zero; this applies to pointer AND wheel edits. The two original **AMMO YIELD** and **GRENADE YIELD** controls remain horizontal and unchanged, as does the read-only eight-caliber ammo count preview. The two **SNG/NATO POOL** normalized composition bars remain read-only with permanent source-color swatches, caliber names, and exact percentages; zero-weight families do not silently gain generator rows. Original `GameData/ItemGeneratorPrototypes.cfg` weight/count writer, baseline manifest and preset fields are unchanged. This 2026-10-11 authorization supersedes older Loot text prescribing four horizontal family sliders, but no other Loot family redesign is authorized. Build/Wine and in-game confirmation pending.


### 2026-10-11 — Global Anomaly Damage single knob (current authoritative contract)

**GLOBAL ANOMALY DAMAGE** (`AnomalyGlobalDamage`) now contains **one centered compact WPF rotary knob**, labeled **OVERALL DAMAGE MULTIPLIER**. It replaces the former `DrawEconomyCard` horizontal slider; all prior global-anomaly horizontal-slider descriptions are historical and superseded by this section. It shares **the exact same `DrawSourceKnob` / `HitKind.Knob` renderer, 270° arc, native source-selection semantics and input handlers as Your Weapons**, with one 194×181 DIP inner card and a **242-DIP measured top-level section**. There is no second slider or preview, and no full-width knob stretching.

Native source: `environment.anomalyDamage`, multiplier **0–2.5x**, **0.01x native step**. The real Base reference marker changes with selected difficulty (Novice **0.30x**, Stalker **1.00x**, Veteran **1.45x**, Master **1.75x**). Center current value and its `x` suffix turn yellow only when different from the selected reference; a muted reference tick and neutral endpoints do not become gold. Clockwise angular click/drag and scroll wheel adjust only this CFG value; the center is selection-only, the unused lower quadrant clamps to its nearest endpoint. Keyboard arrows, Page Up/Down, Home/End, right-click reset and R reset follow the existing shared knob contract. `ParameterSelected` continues to select the actual source rationale.

**No CFG/logic changes:** the source keeps the same `GameData/DifficultyPrototypes.cfg` / `EnvironmentDifficulty.Anomaly_Damage` writer target, preset key, selected-difficulty baseline, and layered multiplication with the 11 independent individual anomaly values. This is a WPF presentation/interaction change, not a new synthetic source or a WinForms dependency. Local dotnet/Wine visual and in-game behavior still require verification.


### 2026-10-11 — YOUR WEAPONS rotary knobs (current authoritative contract)

The **YOUR WEAPONS** section uses exactly **three compact, independent WPF rotary knobs in one row**, ordered **WEAPON DAMAGE**, **HEADSHOT DAMAGE**, **JAM FREQUENCY**. This replaces and supersedes all earlier Your Weapons horizontal/vertical slider specifications. The board is a single full-width Combat section with **242-DIP** measured content height and **three equal inner cells centered inside a maximum-620-DIP group** (18-DIP margins, 12-DIP gaps), not a full-width control per source. Old inactive horizontal weapon-lane methods are removed.

Each knob has a dark circular face, 270° muted track, a gold live arc/indicator, a small grey **selected Base Preset reference tick**, an in-center current multiplier (neutral until modified, then gold), and neutral native min/max under the arc; labels sit above. No duplicate BASE text, fake DPS/TTK, invented jam chance, or explanatory footer. Use shared theme brushes, industrial frame, centered heading and hover-only white indicator. **Only current modified values turn gold**, including their x unit.

All three knobs edit the same independent real CFG properties as before: `combat.playerWeaponDamage` **0.25–2x**, `combat.playerHeadshotMultiplier` **0.5–3x**, `combat.weaponJamming` **0–2x**, each with **0.25x snapping** and independent preset persistence. Clockwise angular dragging/clicking chooses a source value; the bottom gap clamps to the closest end, clicking the center selects without editing. Mouse wheel, arrow keys and Page Up/Down adjust by native steps; Home/End snap to limits; right-click resets one source, R resets all three. The source selection/rationale, CFG/BPatch writer and preset round-trip are unchanged. This is **WPF-native**, not a WinForms dependency. Local publish/Wine visual and actual-game confirmation remain pending.


**2026-10-10 — Human Squad Size warning cleanup:** removed the unused `minAxis` constant from the vertical-bar renderer (`CS0219`). The 0–3x ruler, 0.25–3x source range, slider behavior, CFG/ BPATCH mapping and preset semantics are unchanged. Local warning-free publish is not yet verified.


### 2026-10-10 — Individual anomalies inherit Global Anomaly Damage

All eleven **INDIVIDUAL ANOMALY DAMAGE** vertical editors now show their **effective strength** above the native authored source value, derived live as `individual source × environment.anomalyDamage`. The selected vanilla difficulty supplies the global default (Novice 0.30x, Stalker 1x, Veteran 1.45x, Master 1.75x) and moving the global slider changes the combined effect without overwriting individual settings. The upper readout is the combined value, the muted secondary readout is the editable native CFG value; the vertical handle, baseline-relative 0–200% Y ruler and original source range continue to edit only the native per-anomaly field. Razor displays its combined coefficient as x; its existing low/high effect breakdown remains in the parameter detail. The actual writer retains one global DifficultyPrototypes `EnvironmentDifficulty.Anomaly_Damage` source plus eleven individual anomaly sources, with no additional baked-in multiplier and no double scaling. Preset formats and existing mappings are unchanged. Local publish/Wine and runtime validation pending.


### 2026-10-10 — Human Squad Size and Individual Anomaly Damage vertical editors

**HUMAN SQUAD SIZE** is now four independent one-row Medical Items-style vertical bars (NEWBIE, EXPERIENCED, VETERAN, MASTER) on a shared **0–3x SQUAD SIZE** ruler. The native source domain remains **0.25–3x**, step **0.05x**, with each bar showing its *effective integer NPC cap* using the existing round-half-up rule and its multiplier in a muted secondary line. The actual CFG multiplier, rank-specific selection callback, muted reference marker, individual right-click reset, keyboard adjustment and R reset are preserved. No new domain or control is created.

**INDIVIDUAL ANOMALY DAMAGE** is now eleven independent vertical bars in one compact row (Electro, Chemical, Fire, Carousel, Toxic Cloud, Steam, Clicker, Razor, Soap Bubble, Fireball, Diamond). Their individual source values are shown above the handles; a common **0–200% DAMAGE** ruler measures each source relative to its own audited 1x reference, not absolute HP across unlike anomalies. All eleven native sources have a 0–2x reference-relative limit and preserve their different real CFG numbers and native steps (Razor is itself an x multiplier). Only changed current values turn gold; baseline ticks are muted. Exact source writer mapping, individual selection/help callback, right-click source reset, keyboard input and R reset remain unchanged. The anomaly panel and XAML host now measure **360 DIP**, human panel **350 DIP**, with compact one-row controls and no redundant text. Local build/Wine visual and in-game checks remain pending.


### 2026-10-10 — World Item Spawns heading

The former **WORLD PLACEMENT** panel heading is now **WORLD ITEM SPAWNS**. This is a display-title change only: existing placement sources, native controls, internal identifiers, presets and CFG/BPatch writers remain unchanged.


### 2026-10-10 — World Loot Depletion vertical bank (authoritative)

The **WORLD LOOT DEPLETION** editor uses five independent, directly editable vertical removal bars in one compact row: AMMO BOXES, F1 GRENADE BOXES, FOOD & DRINK BOXES, MEDICAL BOXES, and LOOSE WORLD LOOT. A single truthful **0–100% REMOVED** Y ruler, current percent above each bar, discreet native reference ticks, and category names below follow the Medical Items / Item Value Layers source-bar grammar. Each control retains its real 0–1 CFG range, 0.01 increment, distinct source writer, reset-on-right-click, keyboard arrow editing and panel-wide R reset. Only modified values are gold. The frame and XAML host both measure 330 DIP, minimum renderer width 600 DIP, with no second row or redundant legend. No loot-domain merge, unique-weapon override, preset or BPatch mapping change. Base/DLC1 population evidence remains in the source audit and parameter definitions rather than adding a noisy legend to the direct editor. Local build/Wine visual verification remains pending.


### 2026-10-10 — Item Value Layers vertical controls (authoritative)

The **ITEM VALUE LAYERS** editor is now a compact, one-row six-column vertical source bank, following the Medical Items direct-edit grammar. CONSUMABLES, AMMO, ARMOR, WEAPONS, ARTIFACTS and MUTANT PARTS each have an independent native vertical drag handle, a current `x` multiplier above the bar, a muted reference tick, and a short item caption below. All six audited CFG values range from **0.00x to 2.00x**, with **0.01x** steps, so the editor uses one truthful **0–2x VALUE** ruler at left rather than unrelated per-card scales. Only modified current numbers turn gold; no Base/Vanilla legend or duplicate bounds are shown. The frame measures 330 DIP high at a minimum 560-DIP width; the original six real parameters, distinct mutant-parts ItemPrototypes Cost writer, keyboard arrows, right-click individual reset and R all-reset remain intact. This visual change does not modify CFG targets, preset format or writer rules. Local publish/Wine and game validation remain pending.
Build fix (2026-10-10): `ItemPriceScalersRadar.OnKeyDown` uses a distinct loop-local name for the six-source R reset to avoid C# CS0136 shadowing of the selected key-control pattern variable. UI behavior and CFG mappings are unchanged; local build verification pending.



### 2026-10-10 — Medical Items shared Override Limits (current contract)

The 370-DIP Medical Items frame places **FOOD HEALING at top-left** and one canonical **OVERRIDE LIMITS checkbox at top-right**. This is a UI-only `medicalOverride` preset flag owned by `health.bandageHealing`; no synthetic CFG boolean or new writer route. OFF (default, including legacy presets) keeps all twelve sources' independent catalog maxima. ON expands their independent caps to the existing physical axes: **HEAL 200 HP, BLEED 100 PTS, RAD 200 RAD**. Individual sliders are never linked or synchronized. Disabling Override clamps only values beyond their respective original source caps; source edits persist via their existing CFG/BPatch writers. Checkbox retains canonical 20-DIP appearance, click/hover, right-click reset, whole-panel R reset, tab reset, and baseline reset. Numeric labels, compact row, reference ticks, and independent source writers remain unchanged. Local Wine visual and in-game confirmation pending.


### 2026-10-10 — Beer and Vodka independent RAD removal (latest binding contract)

Within the **single MEDICAL ITEMS card** the right-hand RADIATION REMOVAL bank now has **four compact vertical sliders on one row**: **SCIENTIFIC MEDKIT**, **ANTI-RAD**, **BEER** and **VODKA**. There is no second-row, duplicate standalone Radiation Treatment card, or separate consumable alcohol antirad slider. The RAD bank is given additional horizontal space by tightening the four gold HEAL / copper BLEED pairs on the left; at least 820 DIP board width is reserved so all 12 numeric columns remain one row without clipping. Each column uses the shared horizontal cap/white-on-hover and independent source-native drag/keyboard/right-click/R reset; the RAD bank keeps a separate teal 0–200 vertical scale, with numbers not conflated with HP or bleeding points.

**Actual original CFG prototypes:** `BeerAntirad1.ValueMin/ValueMax=-10/-10`; `VodkaAntirad3.ValueMin/ValueMax=-50/-50`. New independent positive-unit control IDs: `health.beerRadiationRemoval` (vanilla **10 RAD**, **0–20 RAD**, 5-RAD native steps) and `health.vodkaRadiationRemoval` (vanilla **50 RAD**, **0–100 RAD**, 5-RAD native steps). Scientific Medkit remains 60 RAD/120 max; Anti-Rad 100 RAD/200 max. Both new controls write exclusively to their own source prototypes via the existing positive-to-negative `negateSet` operation; neither affects intoxication, drink duration, alcohol tolerance or the other drink. The previous `survival.alcoholRadiationRemoval` shared 0–2x parameter and its common Beer/Vodka CFG ownership have been retired from the catalog/Consumable Effects UI. **Preset compatibility:** when an old preset still contains the shared multiplier and lacks new individual drink values, load converts it to Beer `10 × old multiplier` and Vodka `50 × old multiplier`, retaining even fractional authored RAD amounts; new presets persist the two independent positive-unit values. Thus default 1x restores 10/50 without changing how the original game works.

The existing Medical Items anchor `health.bandageHealing` reflects changes in every HEAL/BLEED/RAD source plus Food Healing, but production CFG writing remains strictly per-source. No new effect, source ID collision or duplicate writer output is allowed. This current specification overrides historical shared-alcohol and two-RAD-bank UI descriptions.

### 2026-10-10 — Medical Items: compact HEAL/BLEED row plus RAD removal bank (current authoritative layout)

**MEDICAL ITEMS** is a single 370-DIP-high frame containing **four closely paired HEAL/BLEED item clusters in one horizontal row on the left** (Bandage, Medkit, Army Medkit, Scientific Medkit) and **a fifth, dedicated RADIATION REMOVAL group on the right** (Scientific Medkit and Anti-Rad). Do **not** create or revive a separate Radiation Treatment card, aggregate anchor, or an additional row. The HEAL and BLEED columns keep their two real independent physical axes: vertical **HP** in gold (0–200) on the far left and vertical **PTS** in copper (0–100) between the medical pairs and the RAD bank. The narrower right-hand pair uses **teal** (`RadiationAccent`) for both bar-top handles and the **vertical RAD Y-axis title**, with its **own 0–200 RAD scale** (0/50/100/150/200). Units are not converted or normalized across these distinct physical axes. Food Healing stays an independent checkbox at upper right.

In each left item pair, the **HEAL caption below its vertical bar is now gold** to match the gold healing bar (the BLEED caption remains copper to match its secondary bar). Shrink medical pair spacing to `Min(23 DIP, laneWidth × 0.22)`, medical bar widths to `Min(39 DIP, laneWidth × 0.31)` HEAL and `Min(27 DIP, laneWidth × 0.21)` BLEED, and reserve `ActualWidth − 334 DIP` (minimum 270 DIP) for the four-item medical chart and a **compact right bank starting 55 DIP beyond that chart**. The existing four-item card order and on-card labels remain; on very narrow charts, stagger the BLEED numeric label down enough not to collide with HEAL numeric labels. Keep a single horizontal row throughout rather than stacking or spreading into a second large panel.

The radiation bank has **two full native vertical handles** with the same `VerticalBarHandleChrome` current-value cap / white-hover behavior as the medical bars: **Scientific Medkit 60 RAD reference, 0–120 RAD editable** and **Anti-Rad 100 RAD reference, 0–200 RAD editable**, both in **5-point steps**. They have separate actual numeric readouts and muted native reference/max ticks. Drag, keyboard arrows and right-click reset target only the selected source; `R` resets all eight HEAL/BLEED bars, both RAD bars and Food Healing. All **11 source parameters** (4 healing + 4 bleeding + 2 radiation removal + 1 Food Healing checkbox) live under the existing visible `health.bandageHealing` UI anchor. They remain **11 independently persisted CFG-backed controls**, with the original sign-converting `negateSet` radiation/bleeding patch writers unchanged. Modified-state tracking includes both radiation children. Supersede historical 4x2-only and detached Radiation Treatment UI contracts; this is visual regrouping, not gameplay coupling.

### 2026-10-10 — Medical Items colored vertical Y-axis titles (current binding refinement)

In the existing **MEDICAL ITEMS** paired vertical-bar graph, the **left HP axis title** is **vertical (−90°)**, centered beside its 0–200 HP ruler and drawn in the **same gold AccentBrush as the HEAL columns**. The **right PTS axis title** is **vertical (+90°)**, centered beside its 0–100 PTS ruler and drawn in the **same copper BleedingAccent as the BLEED columns**. The titles use the reserved side gutters (left `chart.Left − 38 DIP`, right `chart.Right + 44 DIP`) and do not interfere with horizontal muted axis tick values or the chart/column hit areas. Both must match the respective column colors, not swap sides or inherit neutral gray. No extra legend, axis scaling change, increased chart size, preset or CFG changes are introduced.

### 2026-10-10 — Medical Items final spacing and contrast refinement (latest binding design)

Keep the **four HEAL/BLEED vertical pairs in one MEDICAL ITEMS row**, but tighten each pair with `spread = Min(27 DIP, laneWidth × 0.235)` rather than the former 35-DIP spread. The gold HEAL bar stays wider and on the left; the copper BLEED sub-bar stays narrower and on the right. This must not merge the independently editable hit areas or overlap the HP/PTS readouts at the existing 560-DIP minimum board width. Tone down the *native maximum* gray cap to muted opacity **0.38**, stroke **0.8 DIP**, and the separate muted reference tick to opacity **0.68**, stroke **1.35 DIP**, retaining the brighter current-value top caps. The **Food Healing OFF checkbox** retains `BooleanCheckboxChrome` universal 20-DIP shape, checkmark and hover behavior, but receives a clearer muted border (opacity **0.75**) in the Medical Items board. No extra UI framing, text, enlarged board, CFG/preset changes or modifications to other checkboxes.

### 2026-10-10 — MEDICAL ITEMS: four HEAL/BLEED paired vertical controls (authoritative UI)

The Health tab shows **one full-width MEDICAL ITEMS frame**, replacing separate MEDICAL HEALING and BLEEDING TREATMENT cards. **Exactly four item clusters in one horizontal row**: BANDAGE, MEDKIT, ARMY MEDKIT, SCIENTIFIC MEDKIT. Every cluster contains **two independently editable vertical columns**: a **wider, gold HEAL** bar on the left (HP restored) and a **narrower, copper-colored BLEED** bar immediately to its right (bleeding points removed). Their **separate actual HP and PTS readouts** sit directly above the bars; small HEAL/BLEED captions distinguish the controls, and the item name sits underneath its pair. The two effects are visually grouped only; there is **no value linking or dependency**.

The compact board is **370 DIP high**, remains one row across all window widths, uses four equal item cells inside the existing max-920-DIP renderer, and has two clearly labeled simultaneous physical rulers: **HP 0–200 at the left** and **PTS 0–100 at the right**, marked in 50-HP / 25-PTS steps. These aligned Y coordinates do not imply the two measures are interchangeable. Healing true native maxima remain **40/140/170/200 HP** respectively; Bleeding true native maxima remain **100/30/70/50 PTS** respectively, with Bandage's max **1x (100 PTS)**. Both use native 5-point increments and **independent native-bounded hit zones**. Reference/native-max ticks remain muted gray; only genuinely changed numeric readouts receive the relevant color. Both bar types share `VerticalBarHandleChrome`: their horizontal top caps become white with subtle glow on hover and track their exact own value during dragging. Right-click resets the selected HEAL or BLEED source, arrow keys edit the last-selected bar, and `R` resets all four pairs plus Food Healing. The existing **Food Healing checkbox** is retained compactly at the upper-right inside this same frame.

The **single visible UI anchor is `health.bandageHealing`**. All eight original numeric backing parameters plus `health.foodHealing` stay live, individually persisted and handled by the existing CFG writer (including bleeding `negateSet` paths), and changes to any of them count as modification of the Medical Items card. The old `BleedingTreatmentMixer` UI family, duplicated standalone card and source renderer are retired. This supersedes older standalone Bleeding Treatment and Medkit Healing layouts elsewhere in the historical design documentation; preserve all source mappings and do not introduce eight disconnected or horizontally stacked sliders.

### 2026-10-10 — Bleeding Treatment: 100-PTS Y axis, Bandage maximum 1x (current)

The **BLEEDING TREATMENT** vertical-bar editor uses a common **Y axis 0–100 bleeding points**, with labeled steps **0 / 25 / 50 / 75 / 100**. The independent **Bandage** `health.bandageBleeding` slider is now limited to **0–100 PTS** in 5-PTS increments: 100 PTS is both its original reference and its hard upper editing limit (**1x**, not 2x). Medkit, Army Medkit and Scientific Medkit retain independent native **0–30 / 0–70 / 0–50 PTS** ranges and 5-PTS steps, including matching bounded hit maps. On the common Y axis the Bandage original 100-PTS bar reaches the top; gray native maximum and reference ticks retain their meanings and the gold/white horizontal hover cap stays aligned to the editable current amount. The `negateSet` CFG writer and source reference values remain untouched. Old presets containing Bandage values above 100 are clamped through the existing ViewModel setter. This supersedes previous Bleeding Treatment 200-PTS-ruler / 200-PTS Bandage UI contracts, not historical source evidence.

### 2026-10-10 — Medical and Bleeding vertical-bar hover handles (current authoritative contract)

**MEDKIT HEALING** (Medkit, Army Medkit, Scientific Medkit) and **BLEEDING TREATMENT** (Bandage, Medkit, Army Medkit, Scientific Medkit) use one common `VerticalBarHandleChrome` design instead of a circular handle / hover halo. Each editable vertical bar has a **short horizontal gold cap across its current-value top edge**. Hovering that bar produces a **white cap with subtle horizontal glow**, following the actual current HP/PTS height as the value is dragged; moving off restores the gold cap. Native per-item maximum bounds, gray reference ticks, chart Y axis, number formatting, direct source-value editing, presets, mouse/keyboard inputs and CFG writer are unchanged. The ordinary circular generic `AggregateVisualControl.DrawHoveredHandle` overlay is disabled **only** for `BleedingTreatmentMixer` vertical targets; `MedicalHealingRadar` also stops drawing its circular medkit-chart ring. Bandage's **separate horizontal slider** retains the usual circular handle/hover. No extra floating circle, bogus marker, explanatory text or expanded layout is allowed.

### 2026-10-10 — Hunger LOW single-halo correction (current binding UI contract)

In `AggregateVisualControl / HungerLadder`, the interactive **LOW** threshold has exactly **one** yellow hover glow, centered on its actual circular handle at `(lowX, Y(0))` on the top edge of the Stamina Recovery Loss plot. That handle already uses `DrawHumanAxisHandle`; suppress the generic `DrawHoveredHandle` overlay **only** for the `survival.hungerLowThreshold` horizontal target in the HungerLadder family, because its tall 2D hit-map would incorrectly draw a second glow at the plot's vertical midpoint. Other hunger handles (MEDIUM/SEVERE), graph geometry, values, selected ring, hover interaction and editing remain unchanged. Do not add a second marker, duplicate legend or unused visual spacing.

### 2026-10-10 — Mutant Properties difficulty marker interaction (current authoritative contract)

Inside the single **MUTANT PROPERTIES** card, the **MUTANT HIT DAMAGE** track displays complete **NOVICE**, **STALKER**, **VETERAN** and **MASTER** labels in uppercase below their native difficulty reference dots. Labels and dots are independent click/hover targets. Default labels remain muted; hovering either the dot or its text changes that marker's dot and **label to white** and shows a hand cursor. Clicking either one calls `SnapCurrentNumberToMarker` on the existing `combat.mutantDamage` ViewModel and sets its real value to the selected difficulty's **exact authored reference** (Novice 0.35x, Stalker 1.00x, Veteran 1.35x, Master 1.50x). Marker clicks have priority over the rail's overlapping hit rectangle, are single-click (no dragging) and bypass 0.10x mouse/keyboard step quantization while keeping all standard preset/CFG edits and modification highlighting. Mouse-leave restores muted color. Right-clicking a marker resets the underlying damage parameter like right-clicking the rail. This is a focused visual/interaction refinement: do not add a separate card, buttons, extra labels or highlight vanilla by default.

### 2026-10-10 — Mutant Properties unified card (current authoritative layout)

The existing Mutant Health radar frame is now a **single MUTANT PROPERTIES card**. Directly below the header, the full-content-width **Mutant Hit Damage** rail is rendered *inside the same frame*, showing complete Novice / Stalker / Veteran / Master markers; below it stays the two-column, fourteen-mutant health grid. The old separate `combat.mutantDamage` parameter card is hidden only in UI; its actual ViewModel, presets and CFG writer remain intact and independent. Mouse drag and arrow editing, right-click reset and group R reset operate on the embedded damage slider as well. Layout height is measured at 605 DIP and its member cards are shifted down; no disconnected standalone row or duplicate control. The explicit full-width exception for the former standalone row is removed.

### 2026-10-10 — Mutant Hit Damage top full-width layout (current)

Within **Mutant Health**, `combat.mutantDamage` is the first full-width row, above the Mutant Health radar. Only this one slider receives the full section width to clearly display complete reference difficulty names **Novice, Stalker, Veteran, Master** instead of N/S/V/M. Its ordinary compact card/sliding behavior, marker hit targets, source ID, values and writer remain unchanged. The full-width exception is specific to this section; other independent sliders keep the compact responsive grid. Do not restore the earlier below-radar or orphaned placement.

### 2026-10-10 — Mutant Hit Damage section placement

The existing **Mutant Hit Damage** slider is a member of the **Mutant Health** Combat section, immediately after the Mutant Health radar, not an orphaned standalone **Mutant Combat** section between framed panels. It remains a compact independent slider with unchanged ID, value, preset, vanilla markers, and original CFG writer. Parameter catalog grouping and explicit Combat sort order must match the rendered layout; no duplicate text or new control is introduced.

### 2026-10-10 — Bleeding Damage Profile vertical Y-axis caption

The **HP / TICK** Y-axis title in BleedingCurveControl is rotated 90 degrees counterclockwise and centered next to the chart vertically, matching other profile graph axis labels. The plot geometry, tick numbers, hit targets and compact overall dimensions remain unchanged; do not display the title horizontally at the upper-left.

### 2026-10-10 — Secondary effects never exceed their drink; exact seconds stepping

The three independently editable secondary Effect Duration controls (Water Regen, Energy −100% Cost, Energy −30% Cost) have a **live maximum equal to their matching main drink duration, in both Override Limits modes**. Override Limits changes only the main source's allowable maximum (native OFF / 270s ON); it never decouples the secondary source from its parent. Lowering the main duration immediately caps any longer secondary duration. Each of the eight duration controls enforces a whole-second source-aligned grid in the ViewModel itself: OFF = 1 second; ON = 5 seconds. All main multiplier-backed durations convert this seconds step to their native units; short effects use native seconds. GUI mouse and keyboard edits share the same normalization. This supersedes earlier documentation permitting 270s independent secondary caps in Override ON or saying 0.5s steps remain.

### 2026-10-10 — Drink duration slider increments (authoritative)

All eight independent drink duration sliders snap in real seconds: **Override Limits OFF = 1-second steps; ON = 5-second steps**. Mouse dragging/clicking and keyboard arrows use the same increment; Page Up/Down use ten increments. Five multiplier-backed main controls convert seconds to the correct source multiplier (30/45/20/20/90 seconds per 1x); three secondary tracks use seconds directly. Existing original and 270s override caps and parent constraints continue to apply. Mode changes alone do not alter the current values, and no new CFG or preset fields, UI text or controls are added. The generated BPatch retains the source duration fields.

### 2026-10-10 — OVERRIDE LIMITS: all eight drink duration editors (current authoritative contract)

**One compact global checkbox labeled `OVERRIDE LIMITS`** appears inside the `EFFECT DURATION / SECONDS` frame's top-right header, using the *same* 20-DIP `BooleanCheckboxChrome` as all other booleans. It is stored as preset metadata `drinkDurationOverride` (default false; absent in older presets means OFF), NOT a fictitious CFG source. A hit target `DrinkDurationOverride` supports mouse click, Space on focus, right-click OFF/reset and R on the Consumable group; hover provides the short explanation. The timeline axis labels are shifted down to leave the checkbox/header row clear; the three source-backed short duration cards also move down by 40 DIP from the previous layout. Renderer and measurement both use `ConsumableTimelineHeight(width) = 200 + 82*rows + 8*(rows-1) + 12` with the existing responsive three/two/one-column behavior.

**OFF = original separate limits, NOT shared values**: WATER main 30×3 = 90s, ENERGY main 45×3 = 135s, VODKA and BEER 20×3 = 60s each, MAGIC VODKA 90×3 = 270s. The three extra Water/Energy source durations remain bounded by their corresponding live main drink duration, with 5/5/15s vanilla minimum. This precisely preserves the previous behavior.

**ON = identical allowable 270-second maximum for *all eight* independent durations**, not one shared multiplier. Existing authored 0–270s ruler is unchanged; the five primary source multipliers gain source-aware dynamic ceilings: Water `270/30=9x`, Energy `270/45=6x`, Vodka and Beer `270/20=13.5x`, Magic Vodka `270/90=3x`. The three true-seconds secondary controls get a dynamic 270s cap, removing the parent-duration dependency *only while ON*. Every effect can be set to 180s simultaneously or to completely different values. Vanilla/ref positions, primary source minima, unchanged steps (0.05x primaries / 0.5s secondaries), source `Duration` writer operations, UI highlight rules and independent hit handles remain intact. The checkbox alone NEVER changes any CFG duration; it changes only the allowed maximum.

**Transitions:** OFF→ON must not edit any source number. ON→OFF must restore all native limits and clamp *only out-of-bounds* parent/child durations to their now-allowed maxima (without synchronizing the valid remaining values); main sources clamp before secondary. Switching ON again does not resurrect clamped values, and users should save a preset before disabling if they want to keep extreme settings. Resetting the Survival group/all returns checkbox to OFF. Saving/loading a preset records/restores `drinkDurationOverride` before applying numeric values so 270-second custom settings round-trip losslessly. Legacy presets default OFF. **NO fake new CFG boolean, NO eight synchronized sliders, NO additional catalog parameter**; the same 239 source-backed catalog dimensions and 1,220 direct edit points remain valid. Static audits and production writer tests verify both modes and source-only BPatch output.

### 2026-10-10 — Independently editable short Water/Energy durations (active UI lock)

**Authoritative supersession of every earlier passive-only Water/Energy tick instruction.** The original five primary drink-duration controls remain in one compact 0–270s real-time ruler. Under this ruler, add **exactly three** small independent source-native duration sliders, inside the SAME measured `ConsumableMixer` board: **WATER +25% STAMINA EFFECT DURATION** (`survival.waterRegenDuration`, source `WaterStamina2.Duration`, vanilla 5s), **ENERGY -100% STAMINA COST DURATION** (`survival.energyFullCostDuration`, source `EnergeticStaminaPerAction2.Duration`, vanilla 5s, shared with Limited Energy), and **ENERGY -30% STAMINA COST DURATION** (`survival.energyPartialCostDuration`, source `EnergeticStaminaPerAction1.Duration`, vanilla 15s, normal Energy + Limited Weird Kettle alternative). Each is a genuine separate mouse/keyboard/right-click/R editable parameter, stored in presets and written to its **own** `GameData/EffectPrototypes.cfg` `Duration` field via `set`, without a second CFG owner.

**Actual-value linked bounds:** every short track starts at its exact authored vanilla minimum (5/5/15 s, never below it), and its live maximum equals its linked main drink's **current authored effect duration**: Water `30 × survival.waterEffectDuration` seconds, Energy `45 × survival.energyDrinkEffectDuration` seconds. The source parameter has a static ceiling of 90/135/135 seconds to cover the parent's native 3× maximum, and the UI reads a dynamically lower maximum from the parent. Shrinking the parent automatically clamps only children above its new duration; expanding never moves them without direct input. To prevent impossible bounds (the main shorter than a mandatory vanilla child), the two parent sliders' lower endpoints are **5/30×** and **15/45×** respectively, on the familiar original 1×-anchored 0.05× step grid. Their other behavior stays unchanged. This slight main-slider restriction is mandatory to preserve both requested child invariants simultaneously.

**Compact layout:** keep the five 23-DIP-separated primary source lanes, small subdued reference ticks and their short-effect duration ticks, now positioned from the **three independent child source values** rather than multiplying the main value. Place the three source-native **82-DIP** slider cards in a responsive 3/2/1 column bank at local Y=160 under the timeline, with a compact header, numeric seconds and native minimum/maximum endpoints. Use exactly the same rendering/handle grammar as Stamina Usage. Main duration rail hit maps begin at the actual new native minimum position, not zero. `ConsumableTimelineHeight(width)` computes the real content size at every column breakpoint and `CompactConsumableHeight(width)` uses the same value. Remove the former four passive explanatory footer lines, which become redundant with the editable labels and on-rail gray marks. At baseline, all new controls retain native 5/5/15s with neutral numeric styling; only changed numeric tokens turn gold and grow +1.2 DIP. Do not highlight Vanilla/Base values, add full-width cards or invent effects/engine SPDrain stacking behavior.

**Compatibility:** previous custom presets missing short-duration keys migrate their earlier scaled short effect times where possible without going under vanilla or above the main; all twelve untouched factory presets are upgraded by content-identical SHA migration and include the three vanilla short values. Total source dimensions become **239** (150 visible anchors, 89 hidden), of which **226** are player-editable (including 76 hidden aggregate backings); Survival is now 57 catalog dimensions / 56 live VM dimensions (one deliberately internal). The physical writer still owns exactly **1,220 direct edit points** overall, **139 Survival points**, because three existing points are *reassigned* rather than duplicated. Full local Windows/Wine acceptance and actual UE mixing remain separate.

### 2026-10-10 — CFG-confirmed Energy Drink variants (current source-exact UI contract)

Decoded `EffectPrototypes.cfg` and `ItemPrototypes.cfg` prove that ordinary Energy has **two SPDrain effects**: -100% for 5 s and -30% for 15 s, both targeting eight named actions. Limited Energy's normal effect list has **only -100% for 5 s**; its Weird Kettle alternative includes -30% for 15 s. Their respective `RegenStamina` effects are +200% and +500% for 45 s. `EnergeticStaminaPerAction2` inherits the SPDrain type/actions from PerAction1. Keep four clear, read-only timeline captions: **WATER** regen/stamina effect and lower cost; **ENERGY** -100%/-30% cost timing; **ENERGY** stamina regeneration; **LIMITED** -100% cost and regeneration. Percentage tokens are fixed effect strengths (neutral even when duration changes); only duration `s` numerals change and get gold/+1.2 when the corresponding time source differs from its selected baseline. No claim that simultaneous SPDrain effects add or override. The renderer and measurement share `ConsumableTimelineHeight = 240.0` DIP, caption Y offsets 161/178/195/212, leaving the five original source lanes and exactly nine Consumable Effects inputs unchanged. See [verified source audit](ENERGY-DRINK-EFFECT-SOURCE-AUDIT-2026-10-10.md).

### 2026-10-10 — Active Radiation System caption and compact header sizing

Radiation System uses **RADIATION BUILD-UP / RADIATION RECOVERY / RADIATION HP DAMAGE**. Recovery is the real `survival.radiationRecovery` / `VitalParams.DegenRadiation` source measured in **PTS/S**; it reduces radiation accumulated on the player and never represents HP regeneration. The `DrawCompactTrack` renderer must measure the fully styled current numeric value before allocating the label's remaining width, leaving at least 10 DIP between the two. This supersedes the arbitrary fixed 155-DIP number lane responsible for the ellipsis. Preserve original compact three-column geometry, Stamina Usage ordering, modifier-number highlighting, base ticks and independent CFG controls.

### 2026-10-10 — Active Mutant Combat contract

The Combat tab retains one **Mutant Hit Damage** scalar (`combat.mutantDamage`) as a compact normal horizontal slider; Mutant Health remains its existing fourteen-species grouped control. No additional mutant attack-pacing controls or species ability editor exist. Use Stamina Usage alignment: label left, current numeric value adjacent and clear, rail directly beneath; no full-width padded dashboard, no redundant instruction or Base/Vanilla emphasis. **Human Attack Frequency** is unaffected. This active specification supersedes all earlier Combat bank/board proposals.


True Custom Difficulty keeps every gameplay setting as its original parameter in the preset and CFG-writer graph, while related parameters may be edited through one aggregate visual surface.

**Mixer and responsive grid implementation must follow [UI-MIXER-GRID-SPEC.md](UI-MIXER-GRID-SPEC.md).** This document continues to describe existing non-mixer visual families and their established interaction contracts.

The visual layer never invents replacement gameplay values. It edits the same live `ParameterViewModel` instances used by the ordinary parameter system.

## Compact readability contract

Semantic controls follow one density/readability baseline:

- The default application window is **1600×900**, minimum **1024×576**, preserving **16:9**. The window fits itself to the available work area. Mixer/grid layout follows the authoritative [Mixer + Responsive Grid UI specification](UI-MIXER-GRID-SPEC.md).
- Large semantic controls are centered and capped at 920 px so maximizing the window does not stretch tracks across the whole screen.
- Custom-rendered text has a 9.0 minimum FontSize. Important values and labels use larger sizes.
- Shared aggregate title/subtitle typography uses 11.5/9.5 px; the shared renderer confines labels to the available horizontal viewport, **trims long labels on one line with an ellipsis**, and shifts centered labels away from clipped panel edges. This does not shrink below the 9 px minimum or change any numeric values.
- Shared Base Preset reference ticks are visually thicker and taller for legibility.
- Paired **72 px** source-value cards (Core Health, Human Combat, Psy Phantom Pressure) have a fixed hierarchy: title, current/Base Preset value, native-range track, and legible min/max. Titles and values occupy separate rows and are clipped **inside their own card**, not to the right edge of the full panel.
- Boolean editors use the single 20-DIP square checkbox with a gold checkmark; a neutral label remains beside it. Earlier toggle ON/OFF lanes are historical and no longer rendered.
- Equipment Wear and Human Combat crosshair panels show both independent axis captions and the current/Base Preset multipliers on separate, half-width rows above the plot. No crosshair drag semantics, sliders, or CFG values change.
- Detailed implementation caveats belong in the rationale/help panel; the control surface itself should show only the value, Base Preset reference and immediate gameplay meaning.
- Large entity families use multiple columns when that improves scanability instead of growing into very tall single-column dashboards.
- The Fatigue, Consumables and Hunger aggregate panels use a shared 82 px source-value card, with native-range sliders, 9.2+ px axis labels, 10+ px headings and a grey Base Preset reference tick. The heading and current/base values have separate lines and are clipped to their own card, not across neighboring columns.
- These panels dynamically reflow: ordinary editing cards use two columns at **700 px** aggregate width; Hunger and fatigue stage-threshold cards use up to three columns at **850 px** aggregate width. Narrower containers use fewer columns while retaining the original font sizes and hit targets.
- Their measured heights are calculated from the same wrapped row counts used by the renderer; no fixed 975/1080/748 px boards or bottom clipping at breakpoint changes.
- Grey ticks/markers carry Base Preset reference information so repeated micro-labels such as `VANILLA`, `0%` and `200%` do not need to be printed on every row.
- Compactness must never change backing parameter values, constraints, preset serialization or CFG-writer behavior.

### 2026-10-09 — Anomalies actual grouped rendering / CS8510 follow-up

`TabViewModel.SliderGuide` must contain **only one** `"environment"` arm (the four-panel-specific guide). The previous duplicate caused CS8510 and prevented publishing. `environment` must also appear in the explicit `ParametersView.GroupDescriptions` tab whitelist; without it the named families would not render as four independent top-level group cards, contrary to the Anomalies UI contract. Source sort order 0/100/200/300 for the four family names is unchanged. Compile and Wine smoke test remain pending on the user's local setup.

## Master tab — five compact toggle-gated panels (2026-10-09, latest)

The final tab-by-tab UI pass replaces the five previously oversized Master renderers' inconsistent full-width rails, tiny captions and repeating status footers with one dense **toggle + native compact card bank** grammar. Five independent Master families preserve **five boolean AdditionalMechanicsEffects toggles + fifteen numerical source values** and their fifteen `requiresToggle` edges; no effective gameplay outcomes are fabricated. Each parent remains a universal 20-DIP checkbox in the centered toggle pill immediately under one 12.5-DIP centered heading.

**Shared `DrawMasterNativeCard` contract**: source label left, native current number right, 80-DIP card / 94-DIP pitch, native min/max and selected difficulty reference tick, 0.01/0.5/1.0 step as specified by catalog rather than a fabricated common slider step. At editor width >=672 DIP, max 370-DIP cards arranged in two centered columns; below 672 use max 440-DIP single column. Gold and +1.2 DIP only for modified current numeric token and suffix `%/x/s/HP` when enabled; disabled children retain values in muted text and cannot be edited until the parent's toggle is enabled. The genuine saved custom child value remains intact across toggle changes. Handle-only common `DrawHoveredHandle` glow, native mouse/keyboard/right-click/R scope and parameter-specific rationale selection are mandatory. Original range, difficulty reference and writer mapping are authoritative.

- **StarvationTimeline**, 496/684 DIP: centered toggle, two editable timing cards, native read-only start-delay + three HP damage-tick timeline, followed by three effect cards. No damage-pressure HP/s simulation, no claim the time axis changes the authored severe-hunger activation threshold.
- **SevereFatiguePenaltyBoard**, 401/589 DIP: toggle and five independent positive player-facing magnitude sliders (max stamina loss, carry loss, recoil increase, native aim-sway scalar, native dispersion scalar); 2-column rows with final 5th card centered.
- **MasterRadiationPenalty**, 204 DIP: toggle and one bounded +0–100% additional stamina-action cost source. Does not change radiation buildup or injury.
- **MasterLowHealth**, 204 DIP: toggle and one bounded −0–100% HP recovery-loss source. Authored 30 HP low-health state threshold remains fixed.
- **MasterMedicalHealing**, 307/401 DIP: toggle and three real 0–200% Bandage/Medkit/Army Medkit healing modifier `ValueMultiplier` source cards with 25%, 71%, 82% authored vanilla references; subdued dashed 100%-original-healing guideline remains separate from the selected-difficulty reference. Toggle also affects other engine Master consumable modifiers not modeled as healing controls.

Earlier Master UI sections describing separate full-width effect rows, oversized fixed 390/532-DIP canvases, bright ACTIVE status badges or a 3-fader Starvation lower lane are superseded. Do not change their field semantics, source-specific reference, dependency behavior or preset/writer scope. This completes the UI redesign pass before deep CFG audit; local WPF/Wine visual QA is pending.

## A-Life — current Human and Mutant group-cap editors (2026-10-09)

**Active, approved design.** Two full-width A-Life sections expose **18** native Director `MaxCount` scalers: four human player-rank settings and fourteen mutant-species settings. The screen edits **group-size caps**, not A-Life encounter frequency, lair population, lair respawn or scenario weighting. Current source `x` values and derived integer caps obey the source's existing rank-specific authored cap and half-up integer rounding.

**HUMAN SQUAD SIZE / HumanGroupSizeRadar** is a 350-DIP section comprising four compact full-bank cards (max 760 DIP wide, 66 DIP high, 72 DIP pitch, start Y=50). Each shows its actual rank name left, neutral MAX SQUAD, a **large actual integer cap** and a native multiplier `x` on the same header. Compact horizontal rail with native 0.25–3x source / 0.05x increment, one grey reference tick and quiet bounds 0.25x/3x below. Only cap changes relative to the reference resulting cap and multiplier changes relative to reference source appear yellow, each with +1.2-DIP growth. User may drag/click one rank track, keyboard-adjust the source, right-click one to reset or press `R` to reset all four.

**MUTANT GROUP SIZE / MutantGroupSizeRadar** is a 546-DIP section. All fourteen concrete species appear in **alphabetical order**, in two equal columns by seven rows, with a joint content width cap 784 DIP and 12-DIP gap. Each card height 61 DIP, row pitch 68 DIP, start Y=64. Line 1 = species name left + actual multiplier right. Line 2 = muted `N / E / V / M` (player rank) label + the four **resulting integer max caps**, formatted one token at a time; each numeric token becomes accent-colored and +1.2 DIP only if THAT rank's result differs from reference. Authored zeroes remain zero and neutral. A compact horizontal actual 0.25–3x, 0.05x-step source track (gold fill, muted selected-reference tick and handle-only common `ControlHoverGlow`) occupies the bottom. Each row selects its source rationale and supports native drag/click, right-click-one and `R` reset-all-species. The formerly broad per-card captions and under-9-DIP result labels are not current UI.

Both custom controls raise the existing `AggregateParameterSelectedEventArgs` via their new `ParameterSelected` event to the same `MainWindow` handler already used for aggregate editors; selecting a source no longer strands the rationale at the group anchor. The H/M control's WPF `MinHeight` agrees with its renderer's actual measured height, preventing clipped bottom rows. `alife.mutantSpawn.generic` stays hidden. Lair/World Population audit-only proposals are not implemented. Reference, native tick, CFG writer and preset contracts unchanged.

## Anomalies — four distinct source-bound boards (2026-10-09, latest)

**This explicitly supersedes the earlier six-column `AnomalyEffectsBoard` mixer and older one-section Anomalies presentation.** The `environment` tab's four top-level sections contain 1+11+2+3 = **17** existing gameplay settings. Group names are exact: `Global Anomaly Damage`, `Individual Anomaly Damage`, `Flycatcher Slowdown`, `Special Anomaly Effects`, in that order; each occupies a full-width section.

- **Global**: `AnomalyGlobalDamage` renders one centered **OVERALL DAMAGE MULTIPLIER** 270° knob (194×181 DIP inner card, **242-DIP board**), retaining its native 0–2.5x, 0.01x step and selected-difficulty grey reference tick. The central current number and `x` suffix turn gold only when modified. Interaction is the same WPF dial grammar as Your Weapons, not a separate horizontal slider.
- **Individual damage**: `AnomalyDamageRadar` retains all eleven independent 0–200%-of-own-vanilla source rows in 2×6 grid (756-DIP content cap, 52-DIP row pitch, 420-DIP board), enlarged readable labels, native raw source values and steps. The edited row now raises `ParameterSelected` for exact right rationale selection; its track position is a *relative editing aid*, NOT effective post-protection damage. There is no synthetic merged health-damage control. Razor Health Damage is independently backed by its real 0–2x multiplier.
- **Flycatcher**: `FlycatcherSlowdown` reuses Combat/Equipment three matching circular rings and `HitKind.HumanJoint`. Bottom horizontal ring edits only `flycatcherMovementSpeed` retained speed; left vertical ring only `flycatcherTurnSpeed`; center edits **both** from X/Y position. Source range 0–1, tick 0.01; UI axis/live unit percent 0–100%. Higher means less slowdown. Current 50%/30% vanilla ghost, neutral grid/ticks and gold guides. Above-X and rotated-Y labels carry only modified-current gold numerics. Hover-only circle glow, right-click single vs dual reset, keyboard step and R group reset precisely follow Combat.
- **Special effects**: `AnomalySpecialEffects` is three `DrawEconomyCard` rails (Razor Bleeding 0–2x/0.01; Diamond Bleeding 0–20 points/0.5; Psy Drain Intensity 0–2x/0.01), native suffixes, no unrelated damage or duration simulation. 2-column cap 370 DIP and centered final card (248-DIP board) or 1-column 440 DIP (342-DIP board).
- No changes to authored CFG transforms (Flycatcher retained speed converts via UI−1, yaw and pitch together; Razor two bleeding tiers; Psy native negative drain) or preset/writer routes. Bonus cap work in Equipment and finished Loot layout are unaffected.

## Equipment UI — authoritative compact two-panel contract (2026-10-09)

This latest approved Equipment contract **supersedes the historical Equipment Wear Balance graph layout and Item Weight vertical-fader descriptions below**. Equipment currently has *four* player-facing sources and two full-width section rows, not the eight controls counted in the older v0.2.49 audit.

**Equipment Wear** (`EquipmentWearBalance`): now draws and behaves like the real **Human Combat Profile** 2D editor. A centrally placed square native CFG plot has side `min(240, max(200, ActualWidth − 360))` DIP, top 86, and board height `147 + side`. The graph uses Combat's upper X-axis title **ARMOR TOUGHNESS** and live `x` value, rotated Y-axis title **WEAPON WEAR / SHOT** with its live `x` value, five neutral grid lines per axis with min/middle/max captions, grey dashed selected-difficulty ghosts and gold dotted current guides. Titles and source values are attached directly to the axes; former detached readout cards are removed. Three matching circles use `DrawHumanAxisHandle`, with universal halo only on hover: **bottom ring** = `equipment.armorDurability` X (0.10–2x, 0.01 step), **left ring** = `equipment.weaponWearPerShot` Y (0–2x, 0.01 step), **center ring** = **both sources**, each independently mapped to its native axis. Unlike the previous read-only intersection, center has the same 36×36 interactive joint target and drag routing as Combat; outside X/Y targets are 34×22/27×28 DIP. The inactive plot background is not a target. Right-click either outer handle resets its own source; right-click the center resets both; `R` resets both. Current values are accent +1.2 DIP only when source-modified. Hover-only glow, native source snapping/keyboard steps, selected-difficulty reference and right rationale follow the shared Combat controls. **Do not infer armor condition-loss percentages** from `Armor_Durability`: its exact engine conversion remains runtime-test-required. Weapon wear continues to scale 41 base + 11 DLC1 per-shot records, preserving authored differences and jamming separation.

**Item Weight** (`ItemWeightBoard`): exactly two independent source-bound horizontal `DrawEconomyCard` controls, AMMO WEIGHT and FOOD & MEDICINE, both in 0–2x with 0.01x catalog steps, 1x fixed reference, neutral endpoints, centered bank and bounded text. Cards: 80 DIP each, gap 12 DIP, native slider hit rail; >=672 DIP gives two columns with each card capped to 370 DIP (154-DIP board height), below 672 gives one column with 440-DIP cap (248-DIP height). The source reference tick stays muted. Handle-only glow, one-line value alignment, right-click-one and R reset-both match Stamina Usage. These are item-mass scalers, not carry-capacity controls.

**Bonus caps excluded:** historical `RegenStamina.MaxValue`, `DegenBleeding.MaxValue`, `AdditionalInventoryWeight.MaxValue`, `PenaltyLessWeight.MaxValue` represent upper limits only; the actual gameplay composition is not yet source/runtime-verified. Do not render them. In the **current** live parameter catalog only the above four Equipment sources exist. Do not infer that the older eight-control audit describes eight still-visible editors. Preserve all existing current writer/preset semantics; do not fabricate cap parameters.

## Shared interaction contract

- Left-click or drag a visible handle, point, meter or track to edit its backing value.
- Mouse wheel is not consumed by aggregate visual controls; the surrounding page continues to scroll.
- Hovered slider tracks show a subtle gold outline and a short source-parameter tooltip; draggable controls use horizontal/vertical resize cursors, while toggles use a hand cursor. Read-only graphs have no clickable hit target.
- A keyboard-selected slider retains its outline while the aggregate control has focus, even after the pointer leaves its track.
- Left/Right and Up/Down adjust the selected slider by its **catalog tick**. PageUp/PageDown move 10 ticks; Home/End select the source minimum/maximum. All keyboard edits use the same source-value snapping as dragging.
- Right-click an individual interactive element to restore only that backing parameter to the current Base Preset.
- Press `R` while a visual control has focus to restore the whole visual family to the current Base Preset.
- Vanilla/Base Preset values are shown with grey ticks, dashed curves, ghost envelopes or labelled reference markers.
- Preset serialization and CFG writer routes remain unchanged.

### WPF template visibility guard

When an aggregate family is anchored on a toggle (currently `advanced.fatigueMechanics`), the template must keep the `ControlRow` visible for its `AggregateVisualControl`. Ordinary toggle styling is therefore conditioned on **both** `IsToggle=True` and `IsVisualAggregateAnchor=False`. This prevents the later toggle trigger from hiding the entire visual board. The board itself contains the bundle toggle; no second ordinary checkbox should appear.

The WPF visual-anchor contract is maintained in the source and the UI design documentation.

### Constraint authority

Ordered gameplay relationships are owned by the central `SliderConstraintCoordinator`, not by individual visual controls.

The coordinator receives every `CurrentNumber` change, including changes made through aggregate visuals, and applies constraints using actual parameter/CFG values. The actively edited value has priority; neighbouring values move only as far as required.

Current ordered families include:

- Bleeding damage: `LOW <= MEDIUM <= SEVERE`.
- Bleeding stage thresholds: `MEDIUM <= SEVERE`; LOW stays fixed at the authored 0.1 boundary.
- Fatigue stage thresholds: `LOW <= MEDIUM <= SEVERE`.
- Fatigue aim penalty: `LOW <= MEDIUM <= SEVERE`.
- Fatigue reload penalty: `MEDIUM <= SEVERE`.
- Hunger stage thresholds: `LOW <= MEDIUM <= SEVERE`.
- Hunger stamina-recovery penalty: `HUNGRY <= SEVERE` by penalty magnitude.
- Repeated-hit protection: `MIN <= MAX`.
- Repair pricing: `FRIENDLY <= NEUTRAL <= DISLIKED <= HOSTILE`.
- Water stamina drain and melee stamina cost relationships.

Ammo-box pool validity is a separate family invariant rather than an ordering constraint. The UI prevents the SNG pool from reaching all-zero weights, and the CFG writer independently rejects any destructible ammo pool whose effective weights are all zero.

## Aggregate-family audit

| Family | Semantic visualization | Status |
| --- | --- | --- |
| Fatigue | Read-only LOW / MEDIUM / SEVERE stage preview and compact native-range combat-penalty/source tracks | Semantic |
| Equipment Wear | Two independent native CFG axes: armor toughness and player-weapon wear per shot, with current/base comparison | Semantic |
| Psy Phantom Pressure | Two independent phantom-cycle timing/count axes, plus gear damage and bleeding toggle | Semantic |
| Human Combat Profile | Native human health and gun-damage axes, plus independent attack-frequency and accuracy tracks | Semantic |
| Your Weapons | Three native 270° rotary knobs for weapon damage, headshot multiplier and weapon jamming | Semantic |
| Core Health | Max-HP rail, recovery-delay ruler, passive regen, difficulty effect scaling and a separate low-health recovery track | Semantic |
| Master Severe Fatigue | On/off bundle plus five independently editable severe-fatigue penalties; inactive tracks preserve stored values | Semantic |
| Master Medical Healing | Master consumable effect bundle toggle plus three source-exact healing-retention multipliers, on a common 0–200% axis | Semantic |
| Master Low Health | Compact bundle toggle and linked conditional health recovery loss, on a 0–100% native scale | Semantic |
| Master Radiation | Compact bundle toggle and linked additional action stamina cost, on a 0–100% native scale | Semantic |
| Bleeding Treatment | Shared normalized treatment-strength mixer against each item's own vanilla value | Semantic |
| Consumables | Authored instant-effect previews plus one real-seconds duration timeline | Semantic |
| Sleeping | Sleep gate, minimum sleep window and wake-up recovery | Semantic |
| Hunger | Shared stamina-recovery axis from penalty through neutral to bonus | Semantic |
| Starvation | Real-time grace period, repeating damage cadence and active penalty meters | Semantic |
| Repeated Hits | Current and vanilla protection envelopes on one curve-weight scale | Semantic |
| Radiation | Separate exposure/clearance controls, normalized tuning bias and downstream damage | Semantic |
| Coupons & Rewards | Three independent native horizontal source cards: starting coupons, main and side quest reward multipliers | Semantic |
| Trading | Four independent compact horizontal cards for buy/sell scalers, acceptance condition and value floor | Semantic |
| Repair | Four independent compact native horizontal reputation modifiers, actual-value-linked ordering | Semantic |
| Ammo Distribution | Separate SNG/NATO pool composition previews plus family-weight editors | Semantic |
| Stash Reward Rolls | Three source-native vertical faders: Basic grenade chance, Basic grenade count, Rare coupon chance; Wine QA pending | Mixer pilot |
| Breakable Box Yields | Two vertical mixer faders for ammo and grenade counts; original 0.25x / 0.5x steps retained; Wine QA pending | Mixer pilot |
| World Loot Layout | Four independent reshuffle/removal toggles and a weapon-shuffle-dependent density track | Semantic |
| NPC Loot & Equipment | Discrete corpse-loot selector, independent medical/ammo tracks and MIN/MAX dropped-weapon condition | Semantic |
| Explosion Damage | Direct blast-damage multiplier and read-only Base Preset comparison band | Semantic |
| Physical Limits | Independent vault height (uu) / carry mass (kg) source tracks with original vault landmarks | Semantic |
| Trader Stock | Two compact native horizontal source cards for ammo and medic supply (0.25x steps), 2-column/single-column responsive | Semantic |
| Service Prices | Three compact horizontal source cards for fast travel, upgrade cost and base repair %; 2-column/single-column responsive | Semantic |
| Item Weight | Two independent native-value vertical weight faders; Wine QA pending | Mixer pilot |
| Anomaly Effects | Six native-value vertical mixer faders with responsive rows, clickable Base marks and per-channel rationale selection; Wine QA pending | Pilot |

The old generic duration fader-bank helper is no longer used.

## Family mapping

### Fatigue Response

`AggregateVisualControl / FatigueCurve`

The redesigned **Fatigue Penalties** panel separates actual SleepinessPoints progression, the three authored stage thresholds and the five combat penalties. The previous combined X/Y graph with overlapping drag targets is replaced by a **read-only stage preview** and ten native-range, independently editable sliders. The compact panel uses **two columns for build-up and other ordinary controls** (at least 700 px wide), with **up to three threshold and combat-penalty cards per row** at 850 px. On narrower screens, all controls wrap without shrinking text or editing targets.

- **Fatigue Build-up Rate**: `survival.sleepinessRate`, raw `VitalParams.RegenSleepinessPoints` (0–0.020 points/s, 0.001 step; vanilla 0.010).
- **Energy Drink Fatigue Relief**: `survival.energyDrinkFatigueRelief`, a 0–2x scaler (0.01 step, vanilla 1x) on the four already-authored energy-drink fatigue-removal effects. This changes their effect strength, not the effect duration.
- **Low / Medium / Severe Fatigue Thresholds**: `survival.sleepLowThreshold`, `survival.sleepMediumThreshold`, `survival.sleepStrongThreshold`. Each uses the real 0–100 SleepinessPoints scale in whole points; vanilla 50 / 60 / 80. The noninteractive stage preview shows colored progression, gold current threshold marks, grey Base Preset marks and three fixed-width live/base labels to avoid collisions.
- **Aim-Time Penalties**: Low `survival.fatigueLowAimPenalty` (0–30%, vanilla +15%); Medium `survival.fatigueMediumAimPenalty` (0–50%, vanilla +25%); Severe `survival.fatigueStrongAimPenalty` (0–80%, vanilla +40%). All three are independent 0.01-raw-step editors with their own CFG effect prototypes.
- **Reload-Time Penalties**: Medium `survival.fatigueMediumReloadPenalty` (0–30%, vanilla +15%, 0.01 step); Severe `survival.fatigueStrongReloadPenalty` (0–37.5%, vanilla +18.75%, 0.0025 raw step). The fractional vanilla value remains fully visible; it is not rounded to 19%.
  
There is **no Low-stage reload effect** in the authored CFG, so no fabricated sixth combat slider is exposed. The stage thresholds and combat penalty intensities remain separate independent controls. Base Preset reference values appear in grey behind gold current handles, with original source bounds at track endpoints.

The centralized value-based `LOW <= MEDIUM <= SEVERE` threshold constraint remains active, prioritizing the currently moved slider and adjusting neighbors minimally. Right-click resets one source setting, while `R` resets all ten family parameters. No new CFG writer mappings, preset fields, inferred formulas or gameplay semantics were introduced.

> **Historical renderer description only:** The 2026-10-09 Combat-parity override above supersedes the old square Y-handle / read-only center / detached text layout for Equipment Wear. The current center is a genuine two-source draggable Combat-style circle.

### Equipment Wear Balance

`AggregateVisualControl / EquipmentWearBalance`

- Horizontal axis = `equipment.armorDurability` (`EnvironmentDifficulty.Armor_Durability`); higher means tougher armor, but is **not** a proven inverse wear percentage.
- Vertical axis = `equipment.weaponWearPerShot`, scaling each nonzero `DurabilityDamagePerShot` in the existing 41 base + 11 DLC player-weapon routes; higher means faster weapon condition loss.
- Grey dashed lines/ghost point = the selected Base Preset; yellow lines/point = the current values.
- Drag the **bottom gold circle** to edit armor toughness or the **left square** to edit weapon wear. The gold crosshair intersection is a read-only summary; clicking it does not modify either CFG value. The on-panel legend now identifies these two separate handles.
- Right-click either handle to reset just that parameter; R resets the pair.
- Both axis headings and live/Base Preset values now occupy separate, width-bounded text lines, eliminating the left/right label collision at the minimum window width.
- Both parameters retain their distinct native ranges/steps, per-difficulty baselines, preset serialization and writer routes. The two item-weight sliders remain separate and unchanged.
- The visual does **not** claim a specific armor durability-to-wear formula and does **not** alter weapon jamming.

### Human Combat Profile

`AggregateVisualControl / HumanCombatProfile`

- X axis: `combat.npcHealth` (the selected difficulty's `NPC_HP` multiplier); farther right = tougher human enemies.
- Y axis: `combat.npcWeaponDamage` (the selected difficulty's `NPC_Weapon_BaseDamage` multiplier); higher = stronger firearm hits from human enemies.
- A grey ghost marker is the selected Base Preset; the yellow crosshair is the current pair. The crosshair is display-only; the **bottom circle** adjusts enemy health and the **left square** adjusts gun damage. The on-panel legend now names both handles explicitly.
- Below the plot, `combat.npcAttackCooldown` edits player-facing attack **frequency**, preserving the existing writer's reciprocal `NPC_AttackCooldown` conversion. `combat.npcHipAccuracy` edits the native accuracy multiplier.
- Each axis has its own two-line title and exact current/Base Preset value, with a strict left/right half-width text boundary so the two headings do not overlap.
- Both lower tracks use separate title/value lines, a gold editable handle, grey Base Preset marker, native range labels and the original step.
- These four independent controls do **not** produce a computed hit chance, attack DPS, or time-to-kill; the engine's complete combat model is not claimed.

### Psy Phantom Pressure

`AggregateVisualControl / PsyPhantomPressure`

- **Phantom Cycle Wait** (`combat.psySpawnCycleDelay`) is the horizontal source multiplier, edited only with the **bottom circular handle**.
- **Phantom Count** (`combat.psyPhantomCount`) is the vertical source multiplier, edited only with the **left square handle**.
- Both headings and current/Base Preset numbers occupy independent, width-bounded rows, matching the Human Combat and Equipment Wear graph style. The interior crosshair point and its guideline intersections are **read-only**, even when the current and base points overlap.
- **Psy Gear Damage** (`combat.psyDurabilityDamage`) remains its own native-range slider and **Psy Bleeding** (`combat.psyBleeding`) remains a separate ON/OFF bundle control. No artificial phantoms-per-minute estimate or joint-axis editing has been introduced.

### Your Weapons

`AggregateVisualControl / PlayerWeaponProfile`

Three directly adjustable and independent rotary knobs in one compact row:

- `combat.playerWeaponDamage` tunes the selected difficulty's `EnvironmentDifficulty.Weapon_BaseDamage` (0.25x–2x). Its Base Preset reference can be 1.30x, 1x or 0.75x depending on the selected difficulty.
- `combat.playerHeadshotMultiplier` tunes `NPCCombatDifficulty.PlayerWeapon_HeadshotMultiplier` (0.5x–3x). The selected baseline is 1x for Novice/Stalker/Veteran and 2x for Master.
- `combat.weaponJamming` tunes `NPCCombatDifficulty.Weapon_JammingMultiplier` (0x–2x). The selected baseline is 1x for Novice/Stalker/Veteran and 1.5x for Master.

Each knob uses its own native absolute multiplier range and **0.25x step**. A grey arc tick denotes the selected Base Preset, not a universal 1x. Gold arc and notch adjust only that CFG setting. Mouse angular drag, wheel, keyboard, right-click and group R reset remain source-specific. No damage-per-second, hit probability or headshot final-damage calculation is invented. The older horizontal-lane section is historical and superseded by the 2026-10-11 contract.

### Core Health

`AggregateVisualControl / CoreHealthDashboard`

- MAX HEALTH edits `health.maxHP` on its real 25–200 HP range. This updates both the player `VitalParams.MaxHP` and the `HealthMechanics.FullHP.ConditionValue` value, but other 100-HP gameplay assumptions remain runtime-test required.
- CONFIGURED RECOVERY DELAY edits `health.regenDelay` in the raw 0–10 second range. The shaded section represents the *configured delay*, not a proven HP recovery timeline: not every recovery source is confirmed to honor it.
- PASSIVE REGEN edits `health.baseRegen` (0–5 native `VitalParams.RegenHP`, 0.5 step). The source field's actual tick cadence and effect layering still need a runtime test, so it is not labeled as HP/s.
- RECOVERY EFFECT SCALE edits `health.regenerationScalar` (0x–2x, 0.01 step), the selected difficulty's `EnvironmentDifficulty.Regen_HP` multiplier. This changes compatible regeneration effects but does not create passive healing by itself.
- The paired PASSIVE REGEN and RECOVERY EFFECT SCALE cards now put the heading above the exact current/Base Preset value instead of trying to fit both on a single line. Both keep native ranges, grey reference markers, and independent draggable hit targets.
- Four independent controls preserve their own ranges, steps, presets, and CFG routes. Gold handles are editable, grey ticks are Base Preset references. Right-click restores a single setting; R restores all four. No fabricated healing-rate curve or effective HP-per-second arithmetic is shown.

### Master Severe Fatigue

`AggregateVisualControl / SevereFatiguePenaltyBoard`

- Toggle: `advanced.fatigueMechanics`, the `SleepinessNegativeMechanics` membership of the selected difficulty's `EnvironmentDifficulty.AdditionalMechanicsEffects`. Master defaults ON; the other vanilla difficulties default OFF.
- MAX STAMINA LOSS: `survival.severeFatigueMaxStamina`, a 0–40% loss (vanilla 20%).
- CARRY CAPACITY LOSS: `survival.severeFatigueCarryWeight`, a 0–40% penalty applied to both existing carry-related effects (vanilla 20%).
- RECOIL INCREASE: `survival.severeFatigueRecoil`, a 0–110% increase (vanilla 55%).
- AIM SWAY: `survival.severeFatigueAimSway`, a 0x–0.8x source multiplier (vanilla 0.4x) mapped to both X and Y sway effects.
- SHOT SPREAD: `survival.severeFatigueDispersion`, a 0x–1x source multiplier (vanilla 0.5x).
- The compact panel uses two capacity tracks, two handling tracks, and a centered final Shot Spread track. Each active track displays a gold fill from its actual minimum to the current value; the grey Base Preset tick remains visible even when current and reference values coincide. The five numeric values and axis endpoints use larger text to remain legible.
- When OFF, the five penalty sliders retain their stored values but cannot be edited until the bundle is switched ON. Base Preset references remain visible in grey. The activation point is unchanged and ordinary Low/Medium fatigue mechanics are separate. Right-click restores a single enabled setting; R restores all six source values and the toggle.
- The panel edits these six existing CFG paths exclusively. It does not infer compounded spread/recoil formulas or introduce a new in-game difficulty.

### Master Medical Healing

`AggregateVisualControl / MasterMedicalHealing`

- `advanced.masterEffectModifierMechanics` controls membership of `MasterEffectModifierNegativeMechanics` in the selected vanilla difficulty's `AdditionalMechanicsEffects`. It activates **more than just medical effects**, including bundled food, energy-drink and artifact behavior. The panel must never imply the toggle affects medkits alone.
- Bandage, Medkit and Army Medkit are direct `MasterEffectModifier.EffectModifiers[*].ValueMultiplier` scalars with distinct fixed vanilla references: 25%, 71% and 82%, respectively. They share a real 0–200% of **each item's normal healing** axis; 100% means the unmodified item healing strength, not the vanilla Master value.
- Golden fills show each currently configured healing-retention multiplier. The grey tick shows the item's vanilla source multiplier. The subtle dashed center marker shows 100% original healing for reference; it is not a separate parameter.
- Child tracks are disabled while the bundle toggle is off without discarding preset values. There is no invented final HP-restored calculation since base healing can be changed independently on the Health tab.
- Right-click an individual slider or switch resets that backing parameter; R resets the entire four-parameter family. No CFG writer or preset schema changes.

### Master Low Health and Radiation

`AggregateVisualControl / MasterLowHealth`

- Toggle `advanced.lowHealthMechanics` adds/removes `RegenHealthNegativeMechanics` on the selected Base Preset.
- Child slider `health.masterLowHealthPenalty` edits the source `RegenHealthNegativeHealthRegeneration.ValueMin/ValueMax` as a 0–100% recovery loss, with the vanilla negative 70% effect encoded by the writer.
- The authored Master activation threshold remains 30 HP. This control never changes ordinary low-health behavior or the selected Health-tab regeneration multiplier.
- OFF locks the slider without discarding its setting. Master defaults ON; lower vanilla difficulties default OFF.

`AggregateVisualControl / MasterRadiationPenalty`

- Toggle `advanced.radiationMechanics` adds/removes `RadiationNegativeMechanics` on the selected Base Preset.
- Child slider `survival.radiationStaminaDrainPenalty` edits `RadiationNegativeStaminaPenalty.ValueMin/ValueMax`, adding 0–100% to action stamina costs when the existing heavy-radiation condition applies. Vanilla is +50%.
- The radiation accumulation, passive removal, health damage and authored state thresholds do not change.
- OFF locks the slider without discarding its setting. Master defaults ON; lower vanilla difficulties default OFF.

Both panels use 235px height rather than the global 320px minimum: this is possible because the common WPF control and its XAML declaration no longer impose the old minimum. The other existing aggregate families retain their explicit measured heights (320px and above). Each toggle and its source-true child share one visual card; grey ticks show Base Preset reference values. Right-click one handle to reset it, and R resets its two-member family.

### Bleeding Treatment

`AggregateVisualControl / BleedingTreatmentMixer`

The redesigned board separates **read-only relative strength comparison** from the four **independent, full-width, raw bleeding-removal controls**. The previous narrow vertical faders are removed.

The upper comparison has a small, fixed-width column for **Bandage / Medkit / Army Medkit / Scientific Medkit**. Each column normalizes its current authored removal to that **specific item's own Base Preset value**, on a shared 0–200% relative scale. The grey midpoint tick marks 100% (the item's original strength). Current percentage and raw bleeding-removal points appear below each mini-bar without floating labels or draggable hit targets. **100% does not mean 100 bleeding points for every item.**

The four independently editable source values are:

- **Bandage**: `health.bandageBleeding`, 0–200 bleeding points in 5-point steps; vanilla 100. Writes `BandageBleeding4.ValueMin|ValueMax` with the original `negateSet` operation.
- **Medkit**: `health.medkitBleeding`, 0–30 points in 5-point steps; vanilla 15, `MedkitBleeding2`.
- **Army Medkit**: `health.armyMedkitBleeding`, 0–70 points in 5-point steps; vanilla 35, `ArmyMedkitBleeding3`.
- **Scientific Medkit**: `health.ecoMedkitBleeding`, 0–50 points in 5-point steps; vanilla 25, `EcoMedkitBleeding2`.

Each horizontal row uses the real raw slider min/max, current bleeding points, Base Preset bleeding points, and a percentage of that item's own vanilla reference. Gold handles and grey baseline ticks can be read independently. The stored numeric values remain **positive bleeding-removal magnitudes**; the existing CFG writer continues applying `negateSet`. This panel does **not** control HP healing or overwrite any medical-health-restore effect.

Right-click resets an individual slider; `R` resets the family of four. No changes to CFG bindings, preset schemas, original step frequency or healing/gameplay semantics.

### Consumable Effect Board

`AggregateVisualControl / ConsumableMixer`

The compact panel retains **nine independent native-range horizontal sliders**, reflowed into two source-value columns when at least 700 px wide (one column otherwise), while the five timed-effect editors use up to three columns at 850 px. It retains the **read-only instant-stamina/radiation previews** and the **real-seconds duration preview**. Every value remains selectable independently; the two-line compact cards prevent label/value collisions and do not shrink text or hit areas. Actual CFG targets and original slider steps remain unchanged.

**Immediate-effect preview:** current authored stamina refill shown separately for Water (vanilla 38%), Energy Drink (50%) and Limited Energy Drink (100%). The `survival.instantStaminaRestore` multiplier scales all three existing effects together (0–2x, step 0.05) without changing their proportions or effect durations. The adjacent alcohol preview displays Beer (10 radiation points removed) and ordinary Vodka (50 radiation points removed), controlled together by `survival.alcoholRadiationRemoval` (0–2x, step 0.05). Magic Vodka remains separate.

**Sustained stamina strength:** `survival.waterStaminaEffectStrength` scales Water's existing +25% stamina regeneration and −10% action-stamina cost effects together; `survival.energyDrinkStaminaRecoveryStrength` independently scales the ordinary Energy Drink's +200% sustained stamina regeneration. Both range from 0–2x in 0.05x steps. Neither strength control changes a timed effect's duration.

**Duration preview:** a five-row **read-only real-seconds overview (0–270 seconds)** shows the longest current effect for each drink as a gold line and its Base Preset reference as a grey tick. Its endpoints cannot be dragged; each actual duration is edited using a separate source-range slider in the responsive card grid below. Five authored duration families (0–3x, 0.05x step) are:

- **Water** `survival.waterEffectDuration`: 5-second stamina regeneration + 30-second stamina-cost effect.
- **Energy Drink** `survival.energyDrinkEffectDuration`: the original 45 / 15 / 5-second timed stamina-effect set, including the normal/Limited and per-action effects.
- **Vodka** `survival.vodkaEffectDuration`: original 20-second stamina-drain penalty.
- **Beer** `survival.beerEffectDuration`: original 20-second stamina-drain penalty.
- **Magic Vodka** `survival.magicVodkaEffectDuration`: original 90-second psi protection.

Each duration slider keeps the original relative durations **inside its own source family**, with the live scaled seconds visible in the summary below the grid (all five families) and in the preview for the longest effect. The control never invents new effect prototypes or silently combines unrelated modifiers. Gold handles show current multipliers; grey ticks and explicit text display the Base Preset reference, and the slider endpoints use the original catalog limits. Right-click resets the one selected slider; `R` resets all nine source settings.

**Drunkenness and OverDrunkenness are separate parameters and stay outside this board.** Neither alcoholic duration control changes how quickly the player becomes drunk; duration in this panel refers to its specifically authored stamina or psi effect.

### Sleeping System

`AggregateVisualControl / SleepingDashboard`

The redesigned panel separates **sleep access** from **post-sleep recovery** with four full-width horizontal tracks. All settings retain their existing catalog ranges, steps, Base Preset references and CFG target routes:

- **Sleep Becomes Available At** edits `survival.sleepEligibilityThreshold`, the actual `DefaultSleepParams.AllowSleepThreshold` in **sleepiness points** (0–100, 5-point steps, vanilla 50). This is a rule for when the player *may sleep*, not a live meter of the player's current sleepiness.
- **Minimum Sleep** edits `survival.minimumSleepHours`, the actual `DefaultSleepParams.MinSleepHours` in **hours** (1–14, 1-hour steps, vanilla 7).
- **Post-Sleep Health Restore** edits `survival.sleepHealthRestore`, the authored `SleepHealthRestore` effect's **HP amount** (0–600 HP, 5-HP steps, vanilla 300 HP). The amount is not an HP-per-second rate; restored health cannot exceed the player's health cap.
- **Post-Sleep Stamina Effect** edits `survival.sleepStaminaRestore`, the authored `SleepStaminaRestore` **Stamina effect strength** (0–2 raw effect units, displayed as 0–200%, 0.01 raw increments, vanilla +100%). This does not change the player's maximum stamina or stamina regeneration rules.

Each wide row displays the current value and Base Preset value in the appropriate units, with a gold handle and grey reference tick. Right-click an individual slider to reset only that source setting; `R` resets the four-member family. The original two narrow half-width recovery meters have been removed in favor of separate, readable recovery editors. No in-game sleep timing simulation, additional CFG fields or changed writer semantics have been introduced.

### Hunger State Ladder

`AggregateVisualControl / HungerLadder`

This panel now separates the **actual hunger build-up source**, a **read-only 0–100 HungerPoints stage preview**, three **native-range stage-threshold editors** and three **independent stamina-recovery effects**. No overlapping threshold handles remain on the overview: even when two thresholds have the same value, all three remain editable on distinct native-range sliders. When at least 850 px wide the three threshold cards sit in one row; the three recovery cards likewise share one row. At narrower widths, these cards wrap to two or one column.

- **Hunger Build-up**: `survival.hungerRate`, raw `VitalParams.RegenHungerPoints` (0–0.030 points/s, 0.001 step; vanilla 0.015).
- **Low / Medium / Severe Hunger Thresholds**: `survival.hungerLowThreshold`, `survival.hungerMediumThreshold`, `survival.hungerStrongThreshold`. Each uses the true 0–100 HungerPoints range, in whole points; vanilla 17 / 33 / 83. The read-only overview colors the four consecutive regions and shows fixed-position live/base legends, so labels never collide when thresholds converge.
- **Well-Fed Stamina Recovery Bonus**: `survival.wellFedStaminaRegen`, 0–40% bonus (1% step; vanilla +20%). The direct `VeryLowHungerStaminaRegeneration` effect remains a positive value.
- **Medium Hunger Recovery Loss**: `survival.hungerMediumStaminaRegen`, 0–66% loss (1% step; vanilla −33%).
- **Severe Hunger Recovery Loss**: `survival.hungerStrongStaminaRegen`, 0–100% loss (5% step; vanilla −70%).

The Medium and Severe effect values remain stored as **positive magnitudes** while their CFG writer uses the original `negateSet` operation; the UI explicitly displays them as negative stamina-recovery changes. The three recovery effects are independent of the threshold positions. Medium and Severe hunger retain their separate authored **no-sleep** state without creating any new sleep control.

Every slider displays its current value and actual Base Preset reference, with the source min/max, a gold editable handle and a grey Base Preset tick. The existing value-based `LOW <= MEDIUM <= SEVERE` constraint still prioritizes the actively edited threshold and minimally moves neighbors. Right-click resets the targeted slider; `R` resets all seven source parameters. No new CFG mapping, preset field, simulated time-to-starvation result or writer behavior has been introduced.

### Starvation Timeline

`AggregateVisualControl / StarvationTimeline`

- Master Starvation Penalties.
- Starvation Penalty Delay.
- Starvation Damage Interval.
- Starvation Stamina Drain.
- Starvation Damage.
- Starvation Health Recovery Penalty.

The Master-only bundle toggle gates all five numeric settings without discarding their staged values. Two separate, native-range horizontal timing sliders control the penalty delay (0–6s) and repeat interval (1–20s); their grey ticks show the Base Preset. The charge timeline is **read-only** and expands its seconds scale to keep repeated damage ticks visible without overlapping timing hit targets. Three full-width, horizontal effect strips edit extra stamina cost (0–400%), direct HP lost per tick (0–8 HP) and health recovery blocked (0–100%). Each preserves the original CFG target and shows the Base Preset mark. Right-click an active track to reset just that parameter; press R to reset the family.

`DAMAGE PRESSURE` is derived as:

`HP per tick / seconds per tick`

and is displayed only as an explanatory average health-pressure metric.

### Repeated-Hit Protection

`AggregateVisualControl / RepeatedHitBand`

- **Minimum Protection / Weaker End** edits `combat.accumulatedDamageWeightMin`.
- **Maximum Protection / Stronger End** edits `combat.accumulatedDamageWeightMax`.
- **Healed Damage Still Counts** edits `combat.accumulatedDamageIncludesHealedHealth`.

The wide, read-only **Protection Window** previews the two current golden endpoints and the two grey Base Preset endpoints on the same 0.00-1.00 source curve-weight axis. The title and live/Base Preset range now appear on **separate, panel-bounded text lines**, so they cannot collide at the minimum width. It is intentionally not draggable: when MIN and MAX are equal, there are no overlapping drag handles or ambiguous click targets. The two full-width horizontal sliders below it are independent editors, with current values, Base Preset numbers and grey reference ticks.

These source values are hidden protection-strength weights, **not literal damage-reduction percentages**. The catalog's value-based `MIN <= MAX` slider constraint is preserved, with the actively dragged value taking priority. The switch changes only whether healed damage remains in the repeated-hit tracker; it does not change the actual amount of healing.

Right-click on either slider or the switch resets that single source value to Base Preset. `R` resets all three settings. All three CFG mappings and preset schema remain unchanged.

### Radiation Pressure

`AggregateVisualControl / RadiationBalance`

Three independent, **full-width horizontal tracks** replace the old dual half-width meters and inferred relative-bias display:

- **Exposure Accumulation** edits `survival.radiationAccumulation` as the selected difficulty's incoming radiation **multiplier** (0–2.5x; base differs by difficulty).
- **Radiation Recovery** edits `survival.radiationRecovery` as the Player's absolute passive radiation removal (0.00–0.10 points/s; base 0.05 points/s). Higher recovery removes accumulated radiation faster.
- **Health Damage from Radiation** edits `survival.radiationDamage` as a 0–2x scaler on the eleven existing harmful-radiation effect prototypes, without changing their relative strengths or state thresholds.

A compact, read-only **Exposure / Recovery / HP Damage** overview explains the three distinct game systems without pretending that the gain multiplier and the absolute removal rate can be subtracted into a net rate. Each overview column now clips its own caption and explanation before the next column begins. The former `RELATIVE TUNING BIAS` indicator is removed to avoid suggesting such a calculation.

Each editable slider uses its real catalog limits and step, shows current and Base Preset values on a **separate, wider value line below its title**, and keeps a grey reference tick behind the gold handle. The gameplay explanation remains on its own line; the lower editor track is moved slightly to maintain spacing within the existing card height. Right-click resets one slider; `R` resets all three. There are no new parameters or CFG-writer changes.

### Trading Flow

`AggregateVisualControl / TradingDashboard`

The redesigned Trading Flow surface separates four independently authored economy settings:

- **You Sell / Trader Payout** edits `economy.buyCondition` on its real 0–2x axis. This is the difficulty-profile `BuyCondition` used when a trader **buys from the player**, not an exact final payout.
- **You Buy / Trader Shop Price** edits `economy.sellCondition` on its real 0–2x axis. This is the difficulty-profile `SellCondition` used when a trader **sells to the player**, not the full shop-price formula.
- **Minimum Condition Traders Accept** edits `economy.traderMinBuyCondition` on a 0–100% durability gate: gear below the chosen threshold is refused. The source maps to the weapon and armor buy-eligibility gates of 86 audited production trading profiles, including DLC1 when installed. Vanilla is 40%.
- **Item Value at 0% Durability** edits `economy.itemValueFloor` on a 0–20% base-item-value axis. Vanilla is 10%. This changes the value retained by fully broken items independently from whether a trader agrees to buy them.

A compact read-only **You Sell → Trader Buys / Trader Sells → You Buy** overview establishes the direction of each transaction. Its two captions are bounded inside separate halves; even at the minimum width, one side cannot overwrite the other. All four values have their own full-width horizontal editor, with **distinct title, live/Base Preset value and gameplay-explanation rows** rather than overlapping labels at the top. The gold current handle, grey Base Preset tick and labelled source-range endpoints remain independent. The acceptance threshold uses neutral grey for refused condition and gold for accepted condition.

The former `DIFFICULTY PRICING GAP` display is removed. Subtracting two difficulty settings is not the player's actual shop markup; trader-specific modifiers, item condition and other vanilla trade rules affect the result. Right-click an editor to reset that parameter; `R` resets the four-member family. All existing CFG mappings, presets, steps and writer behavior remain unchanged.

### Repair Price Curve

`AggregateVisualControl / RepairCurve`

The panel combines a **read-only reputation curve** with four independently editable full-width horizontal sliders:

- **Hostile / Enemy** edits `economy.repairEnemy` (0–3x, vanilla 2x).
- **Disliked** edits `economy.repairDisaffection` (0–2x, vanilla 1.5x).
- **Neutral** edits `economy.repairNeutral` (0–2x, vanilla 1x).
- **Friendly** edits `economy.repairFriend` (0–2x, vanilla 0.75x).

The chart connects current values in gold and Base Preset values with a grey dashed line. Its title and legend have separate bounded half-width captions, and the 1x reference explanation occupies its own line. It has **no draggable hit targets**: the live handles are on the four wide tracks underneath, whose labels, exact live/Base Preset numbers and 1x-relative surcharge/discount hints have **three distinct clipped text lines**. Native bounds, grey Base Preset ticks and the separate **1x unmodified-price** reference remain visible. The chart uses a common 0–3x axis for accurate visual comparison; each slider retains its own original source range and 0.01 step.

`1x` means no reputation-specific change to the underlying repair price; values above 1x are surcharges and values below 1x are discounts. These are reputation modifiers, **not final repair bills**: base repair price and other underlying repair settings are unchanged.

The centralized value-based slider constraint remains `HOSTILE >= DISLIKED >= NEUTRAL >= FRIENDLY`. It prioritizes the actively edited setting and adjusts neighbors minimally. Right-click on a slider resets only its parameter to Base Preset; `R` resets the full four-member family. The control continues writing the four existing `ReputationRepairCostModifiers` entries without changing the CFG or preset schema.

### Breakable Box Ammo Mixer

`AggregateVisualControl / AmmoDistribution`

The redesigned panel has two **read-only, full-width distribution previews** and four **full-width editable family-weight tracks**. It retains the two real authored breakable-box ammo generators rather than simulating a single merged ammunition pool.

**SNG pool:** `A918D`, `A919D`, `A545D`, `A012D`. The first two entries are controlled together by the 9x18/9x19 slider, so the authored vanilla weights of 2 each produce normalized shares of **50% 9x18/9x19, 25% 5.45, 25% 12 Gauge**.

**NATO pool:** `A045D`, `A919D`, `A556D`, `A012D`. Their authored weights are all 2, so the vanilla normalized shares are **25% .45 ACP, 25% 9x19, 25% 5.56, 25% 12 Gauge**.

Each preview renders a proportional stacked bar and a separate, always-visible legend beneath it. Pool title and original generator-entry IDs have separate lines; every legend caption is clipped within its own family column rather than painting over adjacent labels. The legend includes the **current normalized percentage and vanilla percentage** for every authored candidate family, including families currently at 0%, without trying to squeeze text inside narrow bar segments. Pool order and source entry membership are preserved. **The percentage is a selection-weight share within that pool, not a global box spawn chance and not the number of bullets generated.**

The independent family editors are:

- **9x18 / 9x19 mm:** influences both SNG candidates (`A918D` and `A919D`) and the NATO `A919D` candidate.
- **.45 ACP:** changes NATO `A045D` only; does not add a .45 candidate to SNG.
- **5.45 / 5.56 mm:** controls existing SNG `A545D` and NATO `A556D` entries.
- **12 Gauge:** controls existing `A012D` entries in both pools.

All four tracks use their existing **0x–3x range in 0.25x steps**. At `1x` an authored `Weight = 2` entry remains unchanged; at `0x` that family's existing entries receive no positive selection weight. Each track shows its exact current multiplier, a grey Base Preset marker (`1x` in the unmodified source), its slider bounds and the current SNG/NATO shares. The **family title, current/Base Preset multiplier and per-pool share** now occupy separate, card-bounded rows; the horizontal track is kept below them.

The live SNG guard remains mandatory: at least one of the **9x18/9x19, 5.45/5.56 or 12 Gauge** family knobs must stay above `0x`. The editor corrects an attempted all-zero SNG selection, and the CFG writer retains its own non-empty-pool validation. The previews are not draggable; only the four family knobs create edit hit targets. Right-click resets one track, while `R` resets all four. No extra candidate entries, CFG routes, preset fields or spawn/quantity controls have been introduced.

## Non-aggregate visual controls

Dedicated semantic controls remain appropriate where a parameter family has its own gameplay model rather than belonging in the shared aggregate renderer.

Current specialized surfaces include:

- Bleeding damage curve with real Medium/Severe stage-threshold X handles (Low stays fixed at 0.1).
- Drunkenness threshold control.
- Stamina pressure board for action costs, aiming recovery, sprint lockout and emergency low-SP recovery.
- Medical healing output board.
- Item value layers board.
- Mutant health roster.
- Mutant special-ability pacing board.
- Human squad-cap ladder.
- Mutant encounter-cap matrix.
- Stash ammo tier/stack board.
- World-loot depletion board.
- Mutant part drop probability board.
- Individual anomaly damage table.

The semantic-control audit is complete: no polygon/ring radar renderer remains in `Controls/`. Several C# type names still end in `Radar` only as historical internal names used by existing XAML declarations; they no longer describe the rendered UI or interaction model. `RadarMutedTextBrush` is likewise only a legacy theme-resource key.

## Architecture

`ParameterViewModel.VisualAggregateFamily` maps backing parameter IDs to visual families and exposes exactly one visible anchor per family.

`AggregateVisualControl` receives the full live parameter collection plus the anchor's family name, binds only the required backing parameters and edits those instances directly.

`MainViewModel` assigns aggregate modified-state providers so the visible family is considered modified when any backing member differs from the current Base Preset.

`SliderConstraintCoordinator` subscribes to every numeric parameter change and remains the single authority for ordered slider relationships.

The CFG writer remains the final safety boundary for writer-specific invariants such as destructible-ammo pools requiring at least one positive effective weight.

No aggregate visual control changes the parameter catalog target, preset serialization format or CFG writer contract.

## Approved Human Combat UI-09 revision (2026-10-08)

- Both independent axis handles use one ring-and-dot glyph, matching hover/selection glow.
- Center crosshair is interactive: drag edits Health X and Gun Damage Y simultaneously, each using its native range and step; right-click center resets both to Base. Axis rings remain independently draggable.
- Attack Frequency capped at 2x; Fire Accuracy capped at 1x, with existing minimums and 0.01 steps retained.
- User explicitly approved these changes; other UI components remain locked.

## Approved Human Combat Profile layout (2026-10-08, UI-09)

**Status: LOCKED after implementation; runtime visual acceptance pending.** The user explicitly authorized this layout revision.

- Human Enemies uses a **square 2D plot** (width equals height), never the previous wide, shallow rectangle. The plot measures 180–280 DIPs per side based on the actual aggregate width. Position: 64 DIPs from the left, 91 DIPs from the top (extra left margin for unclipped Y-axis labels).
- The two editable graph axes remain **Enemy Health** (bottom circle / horizontal X) and **Gun Damage** (left square / vertical Y). The central crosshair is an **interactive joint handle**: drag to adjust both Enemy Health (X) and Gun Damage (Y), preserving each source's native bounds and steps; right-click it to reset both. The axis rings are independently draggable. Base indicators and empty plot background are noninteractive.
- The **Attack Frequency** and **Fire Accuracy** source controls appear as **two separate vertical mixer strips immediately to the right of the square graph**, each 128–140 DIPs wide; they use `DrawMixerStrip` with native ranges, steps, current/Base markers and source-specific rationale.
- The aggregate is **465 DIPs tall**. X-axis ticks sit 32 DIPs below the plot; Y-axis tick labels are right-aligned 12 DIPs to its left. Two concise explanation lines sit directly below the square plot, centered on the plot rather than the entire aggregate: `LEFT: DAMAGE · BOTTOM: HEALTH` at max(405, plot bottom + 56), and `DRAG CENTER: BOTH AXES` 18 DIPs lower. The left and bottom controls use the exact same ring-and-dot icon; no shape-specific legend wording is permitted. At the two-section grid breakpoint, a 540-DIP section can fit the 180-DIP square plot and both ≥128-DIP faders without horizontal overflow.
- In Combat, **Human Enemies + Mutants** are an explicitly approved adjacent two-column section pair. The existing global threshold remains 1096 DIPs of editor width; below that, section order is unchanged and they stack.
- Do not alter these sizes, placement, styling, interaction mechanics or pairing without fresh explicit user approval and UI Design Lock update. This arrangement changes **no gameplay semantics or source CFG values**.

### Human Combat hover visibility clarification — 2026-10-08

All three edit handles (bottom Health X, left Gun Damage Y, central joint X/Y) use the same gold ring/dot glyph. Hovering any draws an outer 20-DIP-radius translucent gold halo and a 12-DIP-radius emphasized ring; the central joint has a 36×36 DIP mouse target for more reliable interaction. Dragging the joint changes both native source parameters; right-click resets both with a dual-parameter status message. Empty plot background has no edit action. Attack Frequency remains limited to 2x and Fire Accuracy to 1x in `parameters.json`. This is explicitly approved UI-09 behavior and locked after implementation.

### Human Combat native runtime ceilings — 2026-10-08

Human Attack Frequency (`combat.npcAttackCooldown`) is limited to **2.00x**; Human Fire Accuracy (`combat.npcHipAccuracy`) is limited to **1.00x**. These bounds are enforced by both the embedded `parameters.json` catalog and `ParameterViewModel.Maximum` at runtime. The vertical faders, drag/key steps, normalized preset imports and CFG writer source values obey these runtime ceilings. This changes the frequency of attacks and shot accuracy respectively, **not the weapon's mechanical rate of fire**. No other sliders inherit either cap.

### Human Combat implementation vs. runtime screenshot (2026-10-08)

The intended source design has three identical gold circular ring-and-dot controls (Health bottom, Damage left, combined center), with a plot-centered two-line footer: `HEALTH ← BOTTOM | LEFT → DAMAGE`, then an accented `CENTER HANDLE: DRAG BOTH`. The user's reported screenshot displaying square/circle shapes and `POINT = PREVIEW` is **not** an approved baseline; it represents an older running artifact. A publish receipt `publish/win-x64/BUILD-INFO.txt` records source commit and binary SHA-256. This does not establish visual acceptance until confirmed in Wine.

### Human Combat runtime-verification requirement

The UI guide must reflect the **user-confirmed running UI**, not substitute source inspection for visible acceptance. A reported runtime showing 4x Attack Frequency, 2x Fire Accuracy, square Y glyph and `POINT = PREVIEW` is an **unaccepted stale UI**. `AggregateVisualControl.DrawHumanCombatProfile` explicitly asserts the 2x and 1x runtime maxima. To prove the actual EXE matches source, close all old TrueCustomDifficulty windows and invoke `./run-latest.sh`, which checks the publish receipt and starts the verified executable. Visual acceptance remains pending.


### Human Combat hover and labels (2026-10-08)

The two right-hand Human Combat strips have no surrounding rectangular frame in default, selected, or hovered states. Hover only adds a soft gold halo around the active fader handle; the rail, current gold value, grey Base marker, and actual parameter hit targets remain intact. Their headings are **ATTACK FREQUENCY** (NPC attack tempo, maximum 2x) and **SHOT ACCURACY** (human shot accuracy, maximum 1x). Below the plot, read **BOTTOM: ENEMY HEALTH | LEFT: GUN DAMAGE**, followed by **DRAG CENTER: BOTH VALUES**. The shared circular handle glyphs and their hover glow are preserved. Other mixer families retain their borders and labels.


### Human Combat — selection overlay and multiplier typography (2026-10-08)

No yellow rectangular hit-target/selection outline may be drawn around Human Combat's glowing handles or their two fader channels. Selection is communicated by the ring and halo only; hit testing and dragging remain active. The mixer-family current multiplier is set in 16.2 DIP type and scale endpoint multipliers in 11.3 DIP type. Mixer strips no longer print an explicit BASE multiplier caption; the grey reference tick remains available and clickable. Other aggregate labels omit redundant `BASE 1x` text but keep non-unit Base values. These are presentation-only changes and do not normalize the actual chosen difficulty reference to 1x.


### Human Combat axis labels (2026-10-08)

The Human Combat plot identifies its X axis as **ENEMY HEALTH →** centered immediately above the square plot, with the current multiplier beneath that title. The Y axis is identified by a rotated **GUN DAMAGE ↑** caption and its current multiplier in the left margin, physically attached to the vertical axis. Do not put these labels into independent left/right top panel cells. The panel no longer uses the “enemy toughness and gun damage, plus attack tempo and shot accuracy” subtitle. Preserve native parameters and faders.

### Human Combat axis headings — 2026-10-08

Keep ENEMY HEALTH and the current multiplier centered above the chart. The GUN DAMAGE caption alone is rotated vertically in the left outer margin; its current numeric multiplier is shown above the top-left Y-axis scale, separated from the rotated caption. Never use Unicode arrow glyphs in these labels because they can render as missing-glyph boxes in Wine. The interactive chart geometry is unchanged.

### Human Combat inline axis values (2026-10-08)

Draw the selected multiplier beside its axis title rather than on a different row: `ENEMY HEALTH` and its value share one horizontal line centered above the square; `GUN DAMAGE` and its value share one line that is rotated together along the left side. Use a small fixed gap, maintain title and value colors, and preserve the native axes and edit interactions.

### Human Combat grey fader endpoint typography (2026-10-08)

The Attack Frequency and Shot Accuracy grey minimum/maximum multiplier captions are 13.0 DIP. This applies only to Human Combat's frameless vertical mixer strips; other control families retain their established font sizes.

### Human Combat terminology — 2026-10-08

The vertical Human Combat axis label is **ENEMY GUN DAMAGE** (not GUN DAMAGE). Its multiplier remains inline with the rotated label; the footer names the left handle ENEMY GUN DAMAGE.

### Your Weapons + Human Combat layout (2026-10-08)

At widths supporting the existing 540-DIP-per-section responsive pairing, **Human Enemies** is directly left of **Your Weapons**. When narrower, they stack in that order. Your Weapons consists of four independent vertical source faders, displayed in responsive rows: WEAPON DAMAGE (`combat.playerWeaponDamage`), HEADSHOT DAMAGE (`combat.playerHeadshotMultiplier`), WEAPON JAMMING (`combat.weaponJamming`), EXPLOSION DAMAGE (`combat.explosionDamage`). The former explosion damage graph and redundant separate horizontal slider are retired from the active UI, not from catalog/CFG writing. Base reference ticks, numeric ranges, source steps and individual rationale remain available. The Mutants section follows the paired controls. This redesign supersedes the previous Human Enemies+Mutants and Your Weapons+Explosives pairing descriptions.

### Responsive combat section placement (2026-10-08)

The Human Enemies and Your Weapons sections form one 2x1 responsive row when usable grid width permits two 540-DIP-wide sections plus the 16-DIP gap (at least 1096 DIP). At smaller widths they are two stacked 1x1 sections. WPF section pairing must identify neighboring `GroupItem` entries using their `CollectionViewGroup` content, rather than relying solely on inherited DataContext.

### Fixed Combat columns (2026-10-08)

Human Enemies occupies the left column and Your Weapons the right column of a permanently paired 2x1 row, including when the viewport is narrower than the combined minimum. The section content maintains a minimum of 1096 DIP and offers horizontal scrolling when necessary; it does not reflow this particular pair into 1x1. Existing unrelated section pairings remain responsive.

### Combat fixed layout slots (2026-10-08)

Combat section row 1 is always **Human Enemies | Your Weapons**, two equal columns separated by 16 DIP. This relationship is defined by fixed first/second Combat catalog group positions, not automatic pairing or WPF group-name extraction. The Combat section panel reserves at least 1096 DIP, preserving each section's usable width; horizontal scrolling handles smaller viewports. The other Combat sections follow underneath, and other tabs do not inherit this fixed minimum.

### Combat group identity resolution (2026-10-08)

The actual WPF group enumeration may put Mutants between Human Enemies and Your Weapons. Read the group name from each GroupItem's bound Tag; for the Combat layout, arrange the Human Enemies and Your Weapons group elements first and together in a fixed 2-column row, with Mutants following. Never pair the first two raw WPF elements merely by index.

### Your Weapons fader alignment — 2026-10-08

The Combat Your Weapons section uses a fixed **2×2 mixer layout** (two columns, two rows), not generic automatic 3-column/4-column packing. The order is Weapon Damage | Headshot Damage on row one and Weapon Jamming | Explosion Damage on row two. All four faders share equal widths, centered within the section, with a consistent row gap and sufficient panel height. They retain their independent actual CFG ranges, steps and markers.

### 2026-10-08 — All sections fixed 1x1 (supersedes two-column designs)

**Explicit user instruction:** Every top-level section must occupy its own full-width row (1x1). No responsive or fixed side-by-side pairing is permitted, including Human Enemies / Your Weapons, Mutants, trader, equipment, loot and other catalog sections. `ResponsiveSectionGridPanel` now measures and arranges one GroupItem per row, preserving catalog visual order. Combat no longer reserves a 1096-DIP minimum, and horizontal scrolling is disabled again. The four faders **inside** Your Weapons retain their internal 2x2 arrangement; this change concerns top-level sections only. Earlier 2x1 instructions are superseded. No CFG or parameter mappings change. Wine acceptance pending.

### Combat visual update — 2026-10-08

HUMAN COMBAT PROFILE combines the health/damage X/Y graph and three frameless right-hand vertical faders: Attack Frequency, Shot Accuracy, and Explosion Damage. Explosion Damage is the original difficulty blast multiplier, **not** radius and not ordinary gun hit damage. YOUR WEAPONS now has three source-exact faders, excluding Explosion Damage. MUTANT HEALTH displays all eight mutant health multipliers as identical gold-ring vertical faders arranged across four columns and two rows, with grey clickable Base indicators, source-native min/max, 0.05 steps and derived HP below each name. No rectangular hover highlight. Preserve full-width, 1x1 top-level sections.

### Single-row faders — Mutant Health and Your Weapons (2026-10-08)

Mutant Health renders all eight equal-width vertical controls in **one row**, with 8-DIP gaps and compact long labels broken into two lines. Its minimum measured width is 760 DIP; it maintains vertical source ranges and ring-only hover feedback. Your Weapons renders its three independent source faders in one centered row (no second row), with the common mixer geometry. Each stays within its full-width top-level section, and the shorter content determines section height.

### Combined Mutant Combat board (2026-10-08)


### Mutant Combat orthogonal slider directions (2026-10-08)


### Corrected Global Combat positioning — 2026-10-08

The two horizontal sliders are **side by side**, not stacked: Damage Per Hit on the left, Attack Frequency on the right, equal-width columns separated by 24 DIP. The eight vertical cooldown faders remain underneath. Only their placement changes; mouse mapping and range semantics stay as before.

### Combat UI density and Psy graph — 2026-10-08


### Locked Combat section order — 2026-10-08

The four visible Combat groups are ordered **Your Weapons → Human Enemies → Mutant Combat → Mutant Health**, one full-width card per row. The Psy Combat section and Repeated Hit Protection section are completely absent from the Combat view, including their headers; source CFG definitions remain available internally. Your Weapons uses one centered row of three equal-width faders, with two equal gaps rather than the former off-center bank calculation. Combat explanatory text must not mention repeated-hit protection as a visible control.

### Centered card title system (2026-10-08)


### Unified heading system across all tabs (2026-10-08)

Full-width 1x1 section cards and removal of duplicate section separators apply globally. Dedicated visual control titles are centered, bold, gold and 12.5 DIP, with centered 9.5-DIP grey explanatory subtitles, matching the Combat aggregate family. Gameplay ranges, interactions, visuals inside each specialized chart and CFG mapping stay unchanged. The beer/vodka drunkenness editor has no standalone top heading and retains its custom compact internal arrangement until its tab's individual redesign.

### Your Weapons historical horizontal layout (2026-10-08; superseded by 2026-10-11 knobs)

Three compact horizontal tracks occupy one row, left to right: Weapon Damage, Headshot Damage, Weapon Jamming. Each track controls the native parameter by dragging horizontally, with its own Base tick and right-click reset. Equal-width cells separated by 12 DIP, centered card heading, and 169-DIP overall height. Vertical faders are no longer used in Your Weapons.

### Hunger graph interaction and Your Weapons labels (2026-10-08)

Hunger State Ladder: keep the top Hunger Build-up slider and the existing HUNGER POINTS/STAGE PREVIEW. Replace both three-slider arrays with connected three-handle graphs. Stage thresholds use X=the native hunger points; their three levels are visual categories only, not extra config variables. Recovery handles use Y with an actual signed recovery axis: Well-Fed positive bonus versus Medium/Severe negative loss. Click or drag only the rings to edit one real parameter at a time; preserve source ordering constraints, native snapping, reference ghosts and reset behavior. The three Your Weapons horizontal sliders show live value only, not redundant `BASE 1` caption; reference ticks remain.

### Unified Hunger Effect Curve (2026-10-08)

The Hunger State Ladder has one interactive 2D graph beneath the existing Hunger Points preview. X maps thresholds in hunger points; Y maps signed stamina recovery (+ bonus, - penalty). LOW edits X only; MEDIUM and SEVERE are true 2D controls, each bound to its own native threshold and recovery-loss CFG values; WELL-FED is a separate Y-only bonus ring at zero hunger. Current connected effect curve in gold, Base in dashed grey. Right-click a MEDIUM/SEVERE ring resets both underlying values, with native snapping and existing low <= medium <= severe constraints. The previous separate Threshold and Recovery plots are superseded.

### Gamer-style hunger stage graph (2026-10-08)

Survival's Hunger State Ladder retains build-up control and threshold preview, with an updated **HUNGER SURVIVAL PROFILE**. Inside the profile the WELL-FED bonus has its own dedicated vertical channel, visually separate from the thresholds. A stepped, not diagonal, curve depicts discrete LOW, MEDIUM and SEVERE effects. LOW moves horizontally only; MEDIUM/SEVERE gold rings move in two dimensions (hunger onset X / negative stamina regeneration effect Y), with grey baseline markers and dashed baseline steps. Persistent state captions are in separate slots beneath the graph, not floating above handles. Intuitive instructions use WHEN and HOW MUCH. Keep original vanilla CFG semantics and threshold ordering.

### Interaction polish (2026-10-08)

The reference is Human Combat: no rectangular bounding-box selection overlays and no gold transient reset confirmation text. Gold ring handles and hover glow remain; underlying source reset behavior is unchanged. Shared mixer faders no longer draw selection card frames. Dedicated radar control reset messages are never rendered. Hunger Survival Profile removes the word BASE from its live value/preview/footer; grey reference ticks and dashed reference step profile stay visible.

### Hunger visual axis labeling (2026-10-08)

Show WELL-FED / STAMINA RECOVERY / BONUS directly above its vertical positive bonus rail, with UP = MORE RECOVERY below. Main chart captions: horizontal HUNGER POINTS (0–100) and vertical STAMINA RECOVERY LOSS in the dedicated left gutter. Avoid arrows that can render as missing glyphs in Wine; keep axis tick labels apart from the vertical caption. Native parameters and handle interactions remain untouched.

### Hunger axis tick typography (2026-10-08)

The Hunger Survival Profile uses 10-DIP semibold X/Y tick labels, emphasizes the 0/100 endpoints, and labels the Y-axis neutral state `0% NORMAL`. Keep accurate values on the axes (do not replace 100 with MAX). Remove the two redundant bottom instructional captions. Preserve handle behavior, stage markers, axes and game configuration mapping.

### Hunger Survival Profile — player-facing axis and stage captions (2026-10-08)

The Well-Fed bonus fader shows its live `+N% RECOVERY` value without the redundant `UP = MORE RECOVERY` explanation. Keep adequate physical separation between the vertically rotated `STAMINA RECOVERY LOSS` axis title and its numeric percent ticks; the profile plot starts 20 DIPs farther right, with its title 15 DIPs farther left relative to the plot. The vertical zero tick is simply `0%` rather than `0% NORMAL`. Stage summaries distinguish the onset point from the actual recovery reduction on two lines: `LOW AT N PTS` (no invented LOW penalty), `MEDIUM AT N PTS` / `-N% RECOVERY`, and `SEVERE AT N PTS` / `-N% RECOVERY`. This is labeling and spacing only: the native X/Y edit mapping, baseline ghosts, snapped source values and reset actions stay unchanged.

### Hunger stage footer — single-line readability update (2026-10-08)

The interactive Hunger Survival Profile retains its plot and Well-Fed bonus fader. Place the three stage readouts in separate left-aligned, full-width, single-line rows, at offsets +266, +286, and +306 DIPs below the 334-DIP frame top. All three rows are below X-axis ticks (+243 DIPs); none follows its handle's X coordinate. LOW displays `LOW AT N PTS`; MEDIUM and SEVERE explicitly display `MEDIUM AT N PTS | -N% STAMINA RECOVERY` and `SEVERE AT N PTS | -N% STAMINA RECOVERY`. The frame and measured aggregate height gain 40 DIPs. Preserve native parameter editing and other locked presentation unchanged.

### Hunger State Ladder — top-row Well-Fed and aligned full-width plot (2026-10-08)

HUNGER BUILD-UP (`survival.hungerRate`) and WELL-FED STAMINA RECOVERY (`survival.wellFedStaminaRegen`) are independent horizontal `DrawCompactTrack` editors in a shared two-column top row, each preserving its source range, Base tick, and drag/reset mapping. Well-Fed has no vertical slider within the graph. The HUNGER POINTS / STAGE PREVIEW remains an unchanged full-width read-only overview. HUNGER SURVIVAL PROFILE occupies the same outer panel width, with only a left gutter for recovery-loss ticks/title; it retains discrete gold steps and 2D MEDIUM/SEVERE drag rings. Its 294-DIP frame has a one-line stage footer at +259 DIPs, split into three independently clipped, evenly allocated horizontal zones: LOW left, MEDIUM center, SEVERE right; each caption updates with actual current threshold, and MEDIUM/SEVERE captions name the associated `-N% STAMINA RECOVERY`. No back-end or other UI contracts changed.

### Hunger preview/profile X-axis alignment (2026-10-08)

The preview and profile share identical 0–100 hunger-point X coordinates, not merely the same outer frame width. Preview axis starts at `preview.Left + 81` and spans `preview.Width - 111`; the lower profile derives its plot from that rectangle (`plot.Left = axis.Left - 2`, `plot.Width = axis.Width + 12`; internal profile `area` restores the same `axis.Left`/`axis.Width`). This deliberately reserves the Y-axis caption/tick gutter without shifting only the lower graph. All threshold markers, step transitions and ghost references therefore align vertically with the preview's threshold markers at any supported panel width. No gameplay or other controls are changed.

### Hunger header and Y-axis caption correction (2026-10-08)

The HUNGER STATE LADDER header has no explanatory subtitle. The rotated STAMINA RECOVERY LOSS text is aligned to the exact middle of the interior charting area, not the surrounding plot padding; its horizontal location is unchanged, preserving the gap to percentage tick labels. Preview/profile X-axis alignment, existing layout and native editing semantics remain intact.

### Hunger stage labels — consistent AT wording (2026-10-08)

All three stage footer captions include `AT` before the threshold value. LOW: `LOW AT N PTS`; MEDIUM: `MEDIUM AT N PTS | -N% STAMINA RECOVERY`; SEVERE: `SEVERE AT N PTS | -N% STAMINA RECOVERY`. This wording update does not affect the fixed three-zone layout or any interactions.

### Locked graph design archetype: Hunger Survival Profile (2026-10-08)

The confirmed Hunger graph is the permanent design reference for similar interactive stage-effect profiles. Its stable pattern is two native horizontal control tracks above a full-width read-only stage preview, exactly X-aligned with a full-width stepped graph; independent interactive gold rings, grey Base ghosts, clearly titled native X/Y axes with physically separate Y text/ticks, and LOW/MEDIUM/SEVERE one-line three-column dynamic captions. Preserve actual CFG values, snap, ordering, player-facing labels, reset behavior, and discrete authored effects. Different gameplay families must only reuse the visual grammar; their gameplay parameters/step heights must be audited separately.

### Fatigue profile: two native combat penalty plots (2026-10-08)

The FatigueCurve uses ten original parameters: `survival.sleepinessRate` build-up and `survival.energyDrinkFatigueRelief` as two top-row horizontal source tracks; `survival.sleepLowThreshold`, `survival.sleepMediumThreshold`, `survival.sleepStrongThreshold` for X thresholds (0–100 sleepiness points); `survival.fatigueLowAimPenalty`, `survival.fatigueMediumAimPenalty`, `survival.fatigueStrongAimPenalty` for native increased aim time, and `survival.fatigueMediumReloadPenalty`, `survival.fatigueStrongReloadPenalty` for native increased reload time. No LOW reload effect exists in CFG. The stage preview is read-only and shows gold current/grey base marker positions. The two curves render **discrete stairs** (not interpolations) on the shared sleepiness X axis, with actual source-family maxima setting each Y scale. Gold ring joints edit their own source threshold and one corresponding penalty; the two curves share thresholds. Right-click a joint resets those two values, R resets the entire visual family; low reload marker is threshold-only. The graphs reserve Y-title clearance, show real percentage labels and clipped one-line player-facing LOW/MEDIUM/SEVERE captions. These are distinct gameplay axes, not a combined/average fatigue score. Pending local Wine verification.

### Unified Fatigue Combat Profile — independent Y axes (2026-10-08)

The former two separate penalty graphs are superseded by a **single plot** with a shared 0–100 fatigue-point X axis and two separate **native independent Y scales**: gold AIM TIME on the left (0 to the maximum of the three original aim penalties) and white RELOAD TIME on the right (0 to the maximum of the two existing reload penalties). Both appear as discrete step curves, with matching Base ghosts. At LOW, only the gold aim penalty is authored; white reload stays zero until MEDIUM. Gold LOW/MEDIUM/SEVERE and white MEDIUM/SEVERE rings each edit the existing threshold X and corresponding native effect Y; the white and gold endpoints never represent a mathematically combined fatigue penalty. Pointer kind distinguishes two joint families even within the same plot, replacing fragile Y-position detection. Right-click each joint resets its own threshold and penalty, R resets the complete family. Top build-up and energy-drink tracks plus full-width stage preview remain as the approved Hunger-derived design. Graph height and control measurement have been shortened for the single panel. Pending Wine visual review.

### Fatigue combined graph — reload-only domain + correct dragging (2026-10-08)

The combined chart keeps the gold full-stage AIM curve. White RELOAD has no LOW effect: its visible step curve starts at MEDIUM's current native threshold and its grey ghost at Base MEDIUM. White percent tick marks use their own shortened reload vertical mapping (18 DIPs below the aim chart's top, 12 DIPs above its bottom), and a small right-inner secondary rail rather than a duplicate full-height Y caption. Its 0% is bottom and highest native reload slowdown is top. A reload joint maps pointer Y from its own reloadArea; both types of fatigue joint use `effect = ratioFromBottom * nativeFamilyMax` for intuitive upward-increases-penalty semantics. 34×34 hit rectangles and nearest-center arbitration resolve overlapping white/gold handles. The ring kind continues to distinguish aim versus reload effects and preserve targeted reset semantics. The Hunger design lock and all other source parameter mappings stay intact.

### Fatigue white reload scope: independent penalty controls (2026-10-08)

In the unified Fatigue graph, the gold aim handles own all LOW/MEDIUM/SEVERE shared fatigue thresholds (X) and aim slow-down values (Y). White reload handles are **Y-only** native penalty controls: MEDIUM maps to `survival.fatigueMediumReloadPenalty`, SEVERE maps to `survival.fatigueStrongReloadPenalty`; horizontal drag must not edit `survival.sleep*Threshold`, and selected-key adjustments also target the reload effect. White right-click resets only its own effect. The white step preview (and Base dashed ghost) is drawn **only from its MEDIUM threshold to its SEVERE threshold**, starting at MEDIUM's actual reload penalty and ending at the SEVERE step, without a fabricated 0–MEDIUM base segment or any line beyond SEVERE. The right-hand independent reload percentage ticks remain a guide to the white Y values. Gold stage thresholds and all original CFG mappings are preserved.

### Fatigue stage captions and vertical reload title (2026-10-08)

Fatigue's dynamic three-zone row uses `LOW AT N PTS | AIM +N%`, `MEDIUM AT N PTS | AIM +N% | RELOAD +N%`, and `SEVERE AT N PTS | AIM +N% | RELOAD +N%`, with a compact legend explicitly defining positive values as longer/slower aim or reload actions. The white axis title `RELOAD TIME` is rotated vertically at the **outer right margin**, centered against the shorter independent reload Y range. Its ticks, white curve and Y-only handles retain their native independent scale, restricted MEDIUM-to-SEVERE response and white reset behavior. The main plot rectangle, horizontal X values, preview alignment and locked Hunger archetype remain unchanged.

### Fatigue — final approved and locked (2026-10-08)

The user accepted the unified Fatigue Combat Profile: gold AIM stage graph and white independent, vertically titled right-hand RELOAD scale; gold handles control stage X thresholds while white handles change only native reload penalty Y. White response exists only MEDIUM–SEVERE; no unsupported LOW reload effect or additional white segments. Keep preview X alignment, separate native Y ranges, nearest-ring pointer behavior, grey Base references and concise gamer-facing percentages unchanged unless explicitly authorized. This is a design reference, not a required layout for other control families.

### Consumables Effect Board — gamer-facing effects (2026-10-08)

All nine independent source sliders survive. Four 0–2x strength tracks appear first: shared instant stamina refill (Water 38%, Energy 50%, Limited Energy 100% at 1x), Beer/Vodka radiation removed (10/50 points at 1x), Water sustained regen +25% and stamina action cost -10% at 1x, Energy sustained regen +200% at 1x. Three responsive read-only cards immediately explain the current resulting effects; these do not introduce extra parameter coupling or edit targets. A real-seconds timeline previews each drink's longest authored timed effect, with native grey Base markers. Five 0–3x duration tracks then edit Water's 5/30s regen/cost effects, Energy's 45/15/5s effect set, Vodka and Beer's 20s stamina penalties, and Magic Vodka's 90s psi protection separately. The panel explicitly distinguishes duration from strength and drunkenness, which is controlled elsewhere. Width-driven layout measurement now includes both strength rows, three preview cards and five duration controls. Keep native catalog snapping, Base resets, preset serialization and CFG mappings unchanged. Wine visual approval pending.

### 2026-10-08 — Consumables preview compile correction

Removed the duplicate local `previewTop` declaration in `DrawConsumableMixer` (CS0128). The seconds timeline now uses the single preview position derived from its redesigned title (`previewTitle + 23`), consistent with `CompactConsumableHeight`. No parameter, interaction, or approved UI layout was intentionally changed. Local publish/Wine verification pending.

### Consumables vertical compact fader layout (2026-10-08)

The nine independent editable ConsumableMixer tracks use a reusable compact 179-DIP vertical fader: two-line effect title, live native multiplier, gold vertical rail and knob, top max / bottom min labels, and side grey Base marker. Four strength faders precede the read-only instant/sustained/radiation statistics; five duration faders follow the retained read-only five-row real-seconds preview. Layout uses 3/4/5 columns by available width with measured height matched to the number of rows. Original source IDs, value bounds, ticks, reset semantics and all displayed effects remain. No Hunger/Fatigue modifications. Wine acceptance pending.

### Consumables approved revision: horizontal strengths, vertical duration faders (2026-10-08)

Supersedes the previous all-nine-faders arrangement. Four native 0–2x strength editors are compact horizontal source tracks; the five independent 0–3x Water/Energy/Vodka/Beer/Magic-Vodka duration editors remain compact vertical faders. The three effect-statistics cards and longest-effect seconds preview stay intact. Numeric value highlighting is conditional on `IsValueModifiedFromReference()` relative to the selected vanilla Base Preset: changed consumables values are yellow, unchanged values normal text; the color signal extends to effect readouts and current seconds. Remove the three bottom prose/explanation lines; explanation belongs to the existing rationale pane. Board height follows actual responsive rows and ends just below the five faders. No changes to CFG bindings, range/snapping, or other locked panels.

### 2026-10-08 — Precise highlighting inside Consumables readouts

User correction: in the three live Consumables statistics cards, highlight **only individual numerical values** that differ from their corresponding selected Base/Vanilla source values. Never color the whole statistics line: drink/effect names, separators, percentage signs, RAD units and unchanged numbers keep ordinary foreground color. For a mixed-effect line, Water's source modification and Energy's source modification are evaluated separately; one must not highlight the other's unaffected number. Upper horizontal strength sliders, lower vertical duration faders, effect values, and rationale remain otherwise unchanged. Local Wine QA pending.

### 2026-10-08 — Consumables warning cleanup

After the upper four controls returned to horizontal sliders, the local `FaderEnd` calculation in `DrawConsumableMixer` became unused (CS8321). Removed only that dead local helper. Duration faders, layout and all source/editor behavior remain unchanged. The user's previous publish succeeded with this warning; a fresh post-fix publish remains to be run locally.

### Locked Consumables and gamer-facing Drunk stages (2026-10-08)

Final Consumables design is **locked by user instruction**: top four compact horizontal effect-strength sliders; bottom five compact vertical effect-duration faders; three read-only live gameplay-statistic cards whose only modified **numeric substrings** turn yellow, not their labels/units; retained five-drink seconds timeline; no extra footer prose. The nine native parameter IDs, ranges, reset/CFG behavior are preserved.

DrunkennessControl keeps vanilla stage thresholds and existing beer/vodka strength controls. Visible stages use SOBER (<0.001), TIPSY (LOW 0.001), DRUNK (MEDIUM 30), WASTED (STRONG 80), with progression tiles, threshold reference cards and headline vocabulary updated consistently. This does not change intoxication math, the separate OverDrunkness 89 reference, or any source CFG mechanics.

### Sleep System simplified source dashboard (2026-10-08)

SleepingDashboard now reuses compact horizontal native tracks in two logical groups: access rules (`SLEEP UNLOCKS AT`, required fatigue points; `MINIMUM TIME ASLEEP`, minimum configured hours) and wake-up recovery (`HEALTH RESTORED` in HP, `STAMINA RESTORE STRENGTH` as authored effect percentage). Source identifiers remain `survival.sleepEligibilityThreshold`, `survival.minimumSleepHours`, `survival.sleepHealthRestore`, and `survival.sleepStaminaRestore`. Player-facing value text and range endpoints use meaningful points/hours/HP/% units; no BASE wording, redundant descriptions, or bottom legend. Grey source-reference ticks remain for orientation and right-click/R reset continues through the existing hit-target system. Grid is two columns at widths 700+ and one at narrower widths, with runtime height matching rendered rows. This is a UI-only redesign, with no changes to sleeping CFG effects or other locked boards. Local Wine validation pending.

### Radiation System — compact gameplay controls (2026-10-08)

RadiationBalance now presents three native horizontal sliders without the former 3-column prose overview, descriptive sentence under each value, or bottom control legend. The labels are RADIATION BUILD-UP (source multiplier, 0..native limits), RADIATION RECOVERY (native radiation points per second), and RADIATION HP DAMAGE (native damage multiplier). The three use the original `survival.radiationAccumulation`, `survival.radiationRecovery`, and `survival.radiationDamage` parameters and their actual catalog ranges and steps. Values show x or PTS/S appropriately; no BASE text, while grey Base reference ticks are retained for visual reference. The controls reflow with CompactStageColumns and measured height uses the same CompactGridEnd layout. No fake aggregate graph and no modification to threshold states or to any other approved board. Local runtime test pending.

### 2026-10-08 — Bleeding damage graph richer, native editing preserved

User-authorized BleedingCurveControl visual revamp: keep the two independent top bleeding-strength and natural-recovery sliders, and the existing native three damage plus two onset threshold parameters. The graph is taller (458-DIP control; plot y=185 height=185), with readable BLEEDING POINTS 0–100 X and HP/TICK Y captions, numeric X grid ticks, three gently shaded low/medium/severe exposure domains derived live from the editable medium/severe thresholds, a stronger gold live damage curve/fill, dashed vanilla reference, dotted live-value guides and a compact three-column stage readout at the bottom showing `STAGE AT N PTS` and `N HP / TICK`. Only damage readout numbers that differ from their source references are accented; current stage names remain unchanged. Stages have independent caption cells to avoid overlap even if thresholds converge. The LOW threshold remains fixed at 0.1; interactive damage drag, threshold sliders, threshold ordering, values and original CFG mappings are untouched. Existing other panels remain design-locked. Pending local build/Wine validation.

### 2026-10-08 — Core Health native controls with mixed visual grammar

User-authorized redesign of CoreHealthDashboard only, to avoid a panel of five similar sliders. The maximum HP parameter remains an interactive health-pool meter with source-exact native range and grey reference marker; recovery delay remains a separate interactive wait meter in real seconds. The three independent recovery sources (`health.baseRegen`, `health.regenerationScalar`, `health.lowHealthRegen`) are now compact **vertical faders** in a 3-column bank, showing current source value, its original min/max, a grey reference tick and a gold vertical handle. Do not combine or sum the three recovery mechanisms or suggest a fabricated HP/s formula. Recovery effects remain separate from passive and low-health rates. All five source IDs, native ranges, snapping, mouse/keyboard/reset routing, and CFG writer semantics remain intact. The gratuitous BASE string on both upper meters and the bottom tutorial line are removed. Panel height is adjusted for the new fader bank. Other approved panels, including locked Hunger, Fatigue and Consumables, remain unchanged. Local build/Wine inspection pending.

### 2026-10-08 — Core Health recovery channels horizontal

Explicit user correction supersedes the previous vertical recovery fader bank. In CoreHealthDashboard, all three independent recovery channels (passive healing, healing effects, low-HP recovery) use compact **horizontal** native source tracks in the existing three-column row, with Base text suppressed while reference ticks and native source values/reset behavior remain. Maximum HP and recovery-delay meters are unchanged. Removed vertical fader hit areas and updated panel height to 395 DIP. No other panels are affected. Local build/Wine visual verification pending.

### Medical Healing Output — interactive dosage panel, no preview (2026-10-08)

MedicalHealingRadar (legacy WPF control name) has a 390-DIP interactive direct-edit board. Top: `health.foodHealing` ON/OFF switch. The former standalone Food Restores Health row is collapsed using `IsMedicalHealingMember` and the existing card visibility contract, while `AllParameters` still supplies the real toggle to the integrated board. The medical aggregate-modified provider now includes the food Boolean and subscribes to `CurrentBoolean` changes.

Below: four direct source editor cards (Bandage, Medkit, Army Medkit and Scientific Medkit) in 2x2 layout, each showing current real HP, a relative healing percentage label, and exactly five clickable controls: minus / HALF / NORMAL / MAX / plus. Numeric HP changes accent yellow, static labels do not. The clicked actions change actual source values with native snapping, native range, and current-difficulty reference: HALF=0.5 reference, NORMAL=reference, MAX=native catalog maximum; plus/minus=one native tick. Keyboard arrow control and mouse right-click reset remain available. No separate read-only medical strength visualization, HP comparison bars, outcome simulation or preview is rendered. The food switch only changes the original food-healing Boolean; satiety and Weird Kettle are unaffected, and Scientific Medkit remains outside the separate Master medical penalty subset. Preserve the CFG writer, presets, other boards and separate Master parameters.

### 2026-10-08 — MedicalHealingRadar CS0136 compile correction

Resolved the C# CS0136 name-shadowing error in `OnKeyDown` by renaming the R-reset foreach variable from `p` to `medicalItem`, preserving all reset behavior and the separately declared selected parameter `p`. No UI, source mapping or editor behavior change. Fresh publish/Wine check pending.

### 2026-10-08 — Medical Healing UI correction: native horizontal sliders and tiny Food switch

The user rejected the five-step/button strip (-, HALF, NORMAL, MAX, +) and oversized Food toggle. That layout is **superseded**. Each of the four medical HP cards now has just its item label, live HP amount, one slim **horizontal** native-value draggable slider with a grey difficulty reference tick, and native endpoint labels; no step button presets, no second preview/comparison bar. Click and drag maps the real source parameter min/max with native snapping; mouse right-click resets one medicine, keyboard arrows change its native tick, and R resets the board. Modified HP values alone are accented. The Food Restores Health switch is now a small 52×23 DIP ON/OFF pill on the right of a 39-DIP header row; only the pill is clickable, not the entire large header. Food keeps its real CFG-backed Boolean, unchanged semantics and right-click reset. Overall MedicalHealingRadar board and XAML minimum height reduced from 390 to 304 DIP. Preserve the four native medicine parameters, presets and CFG writer, and all unrelated locked UI panels. Build/Wine review pending.

### 2026-10-08 — Final requested Medical Healing layout: four horizontal faders

User explicitly chose the simple alternative instead of another combined graph. Replace the 2x2 medical card grid with **four independent full-width horizontal fader lanes**, one each for Bandage, Medkit, Army Medkit and Scientific Medkit. Each lane contains a terse item name, its real HP value (yellow only if modified), a slim gold source-value rail/handle and an understated grey reference tick. No separate preview, graph, large boxed cards, preset buttons, prose, BASE strings or additional slider endpoints. A single compact Food Restores Health ON/OFF switch remains at the top. Preserve the native source parameters, snapping, pointer dragging, keyboard nudging, right-click/R reset, preset state and CFG writer. The horizontal rails span nearly the full board width; drag mapping uses the same geometry as rendering, not the earlier two-column mapping. MedicalHealingRadar measures 305 DIP; its XAML min-height matches. Other locked boards untouched. Local build/Wine verification pending.

### 2026-10-08 — Medical Healing compact 2×2 dosage layout (latest user decision)

Supersedes the earlier full-width four-lane Medical Healing design. Four independent native HP controls now occupy a compact, legible **2×2 grid**: Bandage / Medkit on the first row, Army Medkit / Scientific Medkit on the second. Each small medical cell presents a strong item label, prominent current HP readout (yellow only for modified values), a single native draggable horizontal HP fader, subtle five-part rail graduation for orientation, a grey selected-difficulty reference tick, and minimal actual native min/max markers. No extra comparison/preview bars, no preset buttons, no aggregate graph and no BASE prose. Hover/selection emphasize the directly editable rail, rather than introducing additional controls. Drag mapping is now derived from exactly the hit rectangle created during rendering, preserving accuracy across both columns and widths. A compact Food Restores Health ON/OFF pill remains at the top as the same real boolean. Native HP bounds/ticks, preset/CFG mapping, per-item right-click reset and keyboard editing remain unchanged. Medical control and XAML height set to 297 DIP. Other locked designs remain unchanged. User local build/Wine visual review pending.

### 2026-10-08 — Medical selection highlight and Bleeding Treatment copy refinement

Per explicit user request, Medical Healing's selected/hovered slider **does not acquire a yellow card outline, gold selection halo or thicker gold handle**. The normal active gold rail and gold numeric value only when modified remain; source interaction, keyboard focus and CFG behavior are unchanged. Bleeding Treatment no longer prints any `BASE` / `BASE PRESET` text, eliminates explanatory and reset footer prose, and shortens the live labels to treatment strength %, absolute `PTS`, and `BLEEDING REMOVED PER USE`. The reference markers remain visually present (subtle grey), and the read-only comparison, four native bleeding-removal tracks and actual source values stay intact. Pending local build/Wine review.

### Mutant Health — audited complete roster (2026-10-09)


### 2026-10-09 — Source-aware numeric-only highlighting in compound captions

**General design rule:** where gamer-facing labels combine dynamic CFG numbers with explanatory words/units, never color the entire caption yellow simply because one source differs from its reference. Use a reusable `DrawCenteredNumericChanges` text-rendering helper that takes the ordered numeric tokens' individual modified-from-reference flags. Only the digits of each modified numeric token are accented; other digits, `LOW/MEDIUM/SEVERE`, `AT`, `PTS`, `STAMINA RECOVERY`, delimiters, minus and percent symbols remain normal. Do not automatically highlight numbers in static scales, axis limits or labels without a parameter association. Applied now to Hunger State Ladder's three stage-preview readouts and its three bottom Hunger Survival Profile stage captions, with independent change flags for each stage's threshold and recovery loss. Never infer modification from a combined sentence-wide Boolean. Existing Hunger geometry, lock, hit areas and native CFG values are unchanged. This helper is the standard for other affected compound text when updating those renderers, rather than indiscriminately recoloring literals. Pending local build/Wine verification.

### 2026-10-09 — Unified source-aware numeric highlighting across value captions

User requested that **all dynamic CFG-value captions** follow the Hunger rule: label words, units, signs, `%`, `x`, `HP`, `PTS`, separators, `BASE` and unmodified/reference numbers remain in their original neutral text colors. Only the digits of a value that differs from the parameter's selected reference receive accent yellow. Source changes must be checked **per parameter**, never by painting an entire mixed sentence. Static axes, scale limits, section headers and explanatory text do not represent changed source values and are not highlighted. Implemented a reusable formatted-text source-number renderer in `AggregateVisualControl` and applied it to the common compact/horizontal track components, Hunger stage preview/footers, Master medical and penalty readouts, Bleeding Treatment, Player Weapons, Bleeding curve stage captions, and Mutant Health live value/HP readouts. Existing custom Consumables statistic lines already color independent numeric values; no geometry, native CFG values, or locked board layouts were changed. Other specialized renderer captions should follow the same established pattern when touched; they must not be blanket-colored or falsely presented as changed without a source comparison. Local build/Wine verification pending.

### 2026-10-09 — Changed numeric value highlights include adjacent gameplay notation

Clarification from user: **when a dynamic numeric value differs from its selected Vanilla/Base reference, color the whole compact numeric token yellow**, including any immediately attached leading `+`, `-` (hyphen or mathematical minus `−`) and trailing `%`, `x` or `×`. Examples: `+20%`, `-33%`, `2x`, `1.5×`; keep `HP`, `PTS`, `RAD`, `STAMINA RECOVERY`, other words, separators and all unchanged/reference tokens in their normal color. This supersedes the earlier digits-only rule. Updated shared numeric-token regex renderers in AggregateVisualControl (Hunger captions and stage labels, compact/value tracks, Consumables readouts and other mapped usage), BleedingCurveControl and MutantHealthRadar. Compare each value with **its own** reference; no blanket line-wide gold coloring. No CFG source or layout changes. Compile/Wine validation pending.

### 2026-10-09 — Your Weapons 0.25× increments

The three existing Your Weapons source sliders — Base Hit Damage (`combat.playerWeaponDamage`), Headshot Multiplier (`combat.playerHeadshotMultiplier`) and Jam Frequency (`combat.weaponJamming`) — now have a native editor snap/tick of **0.25×** instead of 0.01×. Original min/max, CFG targets, reference markers and reset behavior are unchanged. This is a user-requested tuning precision change, not a layout redesign. Local compilation and Wine verification pending.

### 2026-10-09 — Combat numeric multiplier steps: 0.10x except Your Weapons


### 2026-10-09 — Mutant Health 0.05× step restored

User explicitly reverted the recent Combat-wide 0.10× step **only for all 14 Mutant Health faders**. Their catalog sliders again use **0.05× increments** with original 0.25× minimum and 2× maximum. Native drag and keyboard adjustments read the catalog tick dynamically; the existing 1.00×-centered snap grid supports the full 0.25×–2× range in 0.05× increments. Other Combat multiplier controls retain 0.10×; the three Your Weapons controls retain 0.25×. No changes to layout, CFG mappings, presets, or gameplay values. Local build/Wine verification pending.

### 2026-10-09 — Footer removal and numeric emphasis refinement


### 2026-10-09 — Mutant Combat wording cleanup


### 2026-10-09 — Mutant Combat label implementation verified

The active `MutantCombatPressure` source control's `FirstLabel` is **DAMAGE FROM MUTANTS**, matching the previously authorized Mutant Combat wording contract. Only the displayed caption changed; `combat.mutantDamage`, slider geometry, ranges, editing, and reset semantics are unchanged.


### 2026-10-09 — Mutant Health subtitle removed

The Mutant Health card displays only its centered **MUTANT HEALTH** title: remove the `live MaxHP + direct multiplier` subtitle directly beneath it. Preserve the 14 editable faders, positions, source parameters, HP readouts, ranges and reset interactions; no gameplay change.


### 2026-10-09 — Mutant Health lower bound 0.05×

All fourteen Mutant Health multiplier controls now range from **0.05× to 2.00×**, with the existing **0.05× step**, 1.00× vanilla reference, and unchanged CFG mappings. The catalog slider minimum, runtime lower label and drag/keyboard bounds must agree; 0.25× is no longer the minimum. Existing values remain unchanged unless outside the new range.


### 2026-10-09 — Mutant Health two-row spacing

The seven-fader top row and seven-fader bottom row have a subtle **12-DIP vertical gap** instead of a 1-DIP gap. This changes only the second row's Y placement; individual fader height, width, order, hit targets, slider range/step, and outer card height stay unchanged.


### 2026-10-09 — Fatigue Combat Profile source-aware numeric emphasis and footer removal

The three stage captions (LOW, MEDIUM, SEVERE) are source-aware composite labels: fatigue-point threshold, aim penalty and (for MEDIUM/SEVERE) reload penalty each show the shared numeric-only gold emphasis and +1.2 DIP numeric size **only if that specific value differs from its selected Base reference**. Attached signs and % symbols follow their number; unchanged numbers, stage names and descriptive words stay neutral. The lower static legend (`GOLD = SLOWER AIM | WHITE = SLOWER RELOAD | HIGHER % = LONGER ACTION`) is removed. The X/Y axis scales, original aim/reload line colors, hit targets, drag geometry, source mappings and effect values are unchanged.


### 2026-10-09 — Bleeding System difficulty baseline markers

The **Bleeding Strength** slider displays labeled grey ticks at its actual vanilla values: **N** at **0.25×**, **S/V** at **1.00×** (shared by Stalker and Veteran), and **M** at **1.50×**. Labels are drawn below the rail instead of the min/max captions to avoid overlaps in the compact card. These correspond to `ParameterViewModel.VanillaMarkers`, not manufactured levels. **Natural Recovery** and all Bleeding Damage Profile stages have shared `All` references, so they do not receive separate per-difficulty labels. Existing active handles, drag hit boxes, controls, values and reset-to-base behavior remain unchanged.


### 2026-10-09 — Stamina Core and Movement Limits unified

The first Stamina card is **STAMINA & MOVEMENT** and combines four independent full-width horizontal sliders: **Stamina Capacity**, **Base Recovery Rate**, **Maximum Vault Height**, **Maximum Carry Weight**. The old separate Movement Limits card is hidden through shared `StaminaCore` aggregate membership and the original `stamina.maxSP` anchor remains first in the Stamina tab. Vault-height landmark labels (WAIST/EYES etc.) keep their exact values and clickable reset-to-landmark behavior. No subtitle or bottom explanatory footer. Numeric current/Base readouts use the common source-aware gold-only modified-number typography, with units and static words neutral. The existing four CFG bindings, reference markers, native bounds, edits, drag/right-click behavior, and preset serialization remain independent and unchanged.


### 2026-10-09 — Vault-height landmarks use body references only

In **Stamina & Movement → Maximum Vault Height**, show only authored body-reference labels (for example **WAIST**, **EYES**, **ABOVE EYES**) beneath the rail. Do **not** append raw Unreal-unit numbers or `uu` to those labels. Landmark positions and clickable snap targets retain the original source values; only the visible label text is simplified. The retired PhysicalLimits renderer follows the same caption rule for consistency.


### 2026-10-09 — Amok DLC1 boss mapping verification gate

Amok is excluded from the current fourteen Mutant Health faders. Do not invent a MaxHP override or add a UI channel until the DLC1 boss prototype, health path and three-phase/weak-point mechanics are verified against original CFG and runtime. See docs/combat/MUTANT-HEALTH-AUDIT.md. Existing 2 × 7 layout and gameplay remain unchanged.


### 2026-10-09 — Medical Healing combined card

Medical Healing uses one 400-DIP board: top row has the editable Bandage Healing horizontal rail left and Food Healing checkbox right; below, an interactive three-column shared 0–200 HP graph compares Medkit, Army Medkit and Scientific Medkit. Each bar is draggable vertically, with a small grey line at that item's actual vanilla reference (70/85/100 HP respectively). Current numeric HP values turn yellow and grow by 1.2 DIP only if changed from the selected Base preset; static HP and names remain neutral. The Bandage readout uses the same changed-numeric-only rule. No bottom explanatory text, duplicated per-medkit sliders or extra subtitle. Five original CFG-backed parameters, references, native discrete 5 HP steps, and reset interactions remain preserved. No game values are modified by this layout change.


### 2026-10-09 — Bleeding Treatment compact shared comparison

The Bleeding Treatment card is now 407 DIP tall. Top: a full-width editable Bandage bleeding-removal slider. Below: one interactive three-column chart for Medkit, Army Medkit, and Scientific Medkit on the same 0–70 bleeding-points display axis. Vanilla grey reference ticks are specific to each source (15, 35 and 25 pts), rather than normalized to 100%; the shared chart shows their real relative bleeding removal. Each column edits its original native CFG parameter with existing 5-point snapping and its own legal bounds. Only numeric values changed from Base turn gold and grow slightly, including associated +/-/%/x when present. No redundant preview, slider duplicates, explanatory footer or fabricated healing/HP units; this card is about bleeding removal, not health restored. Existing bandage and medkit CFG semantics remain unchanged.


### 2026-10-09 — Bandage joins the Bleeding Treatment chart

Bleeding Treatment now uses one interactive four-column chart for Bandage, Medkit, Army Medkit, and Scientific Medkit. A shared 0–200 bleeding-points scale compares actual values; each column has a grey vanilla/Base tick (100, 15, 35, 25 respectively), independently editable underlying CFG value, original legal range, and 5-point snapping. Remove the standalone Bandage slider, explanatory footer and duplicate preview. Numeric values follow changed-only yellow/enlarged styling; all other text stays neutral. Board height: 376 DIP.


### 2026-10-09 — Factory presets: role-first balancing



### 2026-10-09 — Three additional factory role profiles

Add Mercenary, Wanderer and Outcast to the shipped Presets list (ten total). These are preset-data additions only: no new slider, UI card, difficulty mode or input behavior. All numeric changes remain subject to the existing yellow-only-when-modified number rule; baseline references are Veteran, Stalker and Veteran, respectively. See docs/FACTORY-PRESETS.md for authored playstyles.


### 2026-10-09 — Factory preset embedding fix

The three new JSON preset files (Mercenary, Wanderer, Outcast) must be explicitly included in `TrueCustomDifficulty.csproj` as embedded resources with `TrueCustomDifficulty.Presets.<Name>.json` logical names. `PresetDirectoryService.WriteEmbedded()` uses those exact manifest names during first-run factory seeding; a file present in the repository but absent from the embedded manifest causes startup failure. All ten factory JSON resources now have explicit entries. No gameplay parameters or UI layout changed.


### 2026-10-09 — Alphabetical profiles and two new roles

Twelve factory presets are ordered alphabetically, adding full 201-value Field Medic (Stalker Base) and Smuggler (Veteran Base) roles. Both are bundled as explicit .NET embedded resources and seeded on first run only when missing. UI and CFG editing rules, including yellow-only-changed-numbers, are unchanged. See docs/FACTORY-PRESETS.md.


### 2026-10-09 — A-Life factory preset depth

All twelve shipped factory presets now include the four human Director group size values and fourteen species-specific mutant Director group size values (219 entries total). The hidden generic mutant scaler remains excluded. Profiles preserve existing visual group radar controls and yellow-number-on-change behavior; A-Life Director group sizes are not conflated with lair population, encounter cadence or respawn. See docs/FACTORY-PRESETS.md.


### 2026-10-09 — Health tab compact boards

**Core Health** uses a compact two-column, three-row layout (302 DIP): Max Health, Recovery Delay, Passive Healing, Healing Effects and Low-HP Recovery. All five original CFG-backed native-range sliders remain editable, with subdued vanilla tick marks. Remove the old giant full-width HP/recovery-delay lanes, their min/max captions and redundant recovery heading. **Radiation Treatment** uses two side-by-side native-range sliders (Scientific Medkit and Anti-Rad) in a 149-DIP card, with no subtitle/footer. Preserve source values and changed-numeric-only gold highlight, neutral label/unit text, slider drag and resets. No catalog or gameplay values changed.


### 2026-10-09 — Compact Stamina & Movement

Unified Stamina & Movement is a 259-DIP two-column/two-row board: Stamina Capacity and Base Recovery on top, Maximum Vault Height and Maximum Carry Weight below. No repeated Base values in text: native horizontal sliders retain a grey vanilla/Base reference tick and modified numeric tokens alone turn gold and slightly larger. Vault retains compact, clickable body-height landmarks (WAIST / EYES / ABOVE EYES); no numeric UU landmark captions. All four authored catalog vanilla values are identical across difficulty presets (100 stamina, 5 regeneration, 130 vault height, 80 carry capacity), so do not invent or display per-difficulty differences. This is layout-only; native CFG values and sliders' independent reset/edit behavior do not change.


### 2026-10-09 — Vault landmark alignment correction

Within the two-column Stamina & Movement card, WAIST / EYES / ABOVE EYES captions must be positioned by measuring actual glyph width and centering on their respective track marker X positions. Clamp labels inside the card only at its edges (especially ABOVE EYES); hit targets follow the visible caption bounds. Never left-align caption glyphs inside a fixed-width box, as this visually misaligns labels and ticks. Preserve body-part-only captions, slider values and no redundant explanatory text.


### 2026-10-09 — Radiation Treatment binding fix

Scientific Medkit (`health.ecoMedkitAntirad`) and Anti-Rad (`health.antiRadRadiation`) are not individually exposed catalog rows. Both must be included in `MainViewModel`'s always-instantiated composite parameter ID set; otherwise `AllParameters` omits them and Radiation Treatment renders `CFG PARAMETER UNAVAILABLE`. Maintain both live backing VMs even when only one aggregate anchor is visible. The existing side-by-side 149-DIP layout, raw CFG semantics, vanilla ticks, independent interaction and modified-number highlighting remain unchanged.


### 2026-10-09 — Radiation Treatment delayed binding recovery

The compact Radiation Treatment board requires live `health.ecoMedkitAntirad` and `health.antiRadRadiation` VMs. AggregateVisualControl now rebinds on WPF Loaded and resolves a missing family member from the bound `Parameters` collection on demand; it caches and subscribes to the actual source VM only if that ID belongs to the active family. This addresses template/Window-relative binding timing without inventing CFG values or substituting fallback placeholders. Verify Scientific Medkit and Anti-Rad render editable values after fresh publish. If unavailable persists, inspect runtime binding diagnostics; do not claim gameplay verified.


### 2026-10-09 — Stamina Usage wording and clean presentation

Rename the Action Costs radar panel title from **STAMINA PRESSURE** to **STAMINA USAGE**. Keep the six independent movement/melee costs, aiming recovery control, sprint lock, low-SP recovery threshold, restart delay, recovery boost and boost duration. Omit the explanatory panel subtitle, repeated gold/Base legend, 'continuous stamina-regeneration slowdown', 'absolute stamina values' and duplicated percentages beside direct SP costs. Static action labels remain neutral; modified numeric values alone use the gold highlight; vanilla reference ticks stay visible on tracks. Bring rows closer together and size the board at 393 DIP, without changing backing source values, reference baselines or interaction mappings.


### 2026-10-09 — Aim stamina terminology

In Stamina Usage, display **AIM REGEN SLOWDOWN** instead of `AIMING RECOVERY PENALTY`. This names the gameplay effect precisely: aiming slows stamina regeneration, rather than directly spending stamina. Keep the original CFG setting, percent readout, reference tick, and modified-value coloring.


### 2026-10-09 — Cross-panel Stamina Usage visual consistency pass

Adopt Stamina Usage text economy for specialized slider dashboards without replacing meaning-bearing diagrams: short titles, no generic explanatory subtitle, no duplicated Base-vs-current prose where grey source ticks already show the reference, neutral static labels, and gold numeric readouts only when their parameter differs from its Base. Applied to Anomaly Damage, Human Group Size, Item Value Layers, Mutant Part Drops, Mutant Group Size, Stash Ammo, World Loot Depletion and Mutant Special Ability controls; removed redundant generic subtitles in aggregate editors. Preserve native control maps, original interactive charts, hit targets, baseline semantics and CFG values. Further per-panel geometry tuning must be tested in Wine; do not assume every specialized slider can be replaced by an ordinary horizontal track.


### 2026-10-09 — Specialized slider geometry pass (Stamina Usage alignment)

Retain original interactive ranges, native CFG backing IDs, vanilla/base ticks and panel-specific charts. Across Anomaly Damage, Mutant Part Drops, Stash Ammo, Human Group Size, Mutant Group Size, Item Value Layers and World Loot Depletion, use compact horizontal tracks with consistent approximately 10–12 DIP rails, 10 DIP side padding where applicable, neutral static names, and right-aligned current values that turn gold only when modified from Base. Remove duplicated conversion percentages on Anomaly Damage where the source value already labels the rail; show one chance percentage rather than repeating expected rolls in Mutant Drops. Stash Ammo keeps distinct sample-stack values alongside its shortened native slider, with no redundant tier subtitle. Human and Mutant Group Size preserve authored effective-cap references and species/rank breakdowns. World Loot preserves base/DLC1 population estimates and removal semantics. Do not flatten specialized radar/fader interactions into ordinary sliders; further per-panel geometry changes require local Wine visual verification. No source CFG values, preset values, or mappings changed.


### 2026-10-09 — Shared hover feedback alignment

Interactive hover styling is standardized across the Stamina Usage slider rail/handle controls, shared aggregate horizontal-track cards, aggregate toggle pills and specialized horizontal-row radar panels (anomaly damage, mutant drops, mutant group sizes, world loot, human squad caps, stash ammo and item prices). Hover uses subtle gold/Accent tinted card surface and/or name accent, with consistent stronger interactive handle outline; current numeric value stays gold **only** when modified from the selected Base Preset. Keep hovered styles transient and independent of value changes; keep clickable target geometry, keyboard and right-click reset behavior unchanged. Non-interactive explanations remain neutral. The existing specialized curved plots and vertical faders retain their interaction-specific hover and must not gain rectangular fake buttons. Local Windows/Wine visual testing remains required.


### 2026-10-09 — Hover target-only rule (user-approved correction)

For **all sliders**, hovering must not turn the whole card, track label, helper text or value text gold. Only the interactive slider **handle/control point** receives hover glow/outline. Current numeric text becomes gold only for an actual change from the selected Base Preset, never due solely to hover or selection. Specifically, the three bottom Stamina Usage mini-sliders keep their labels neutral while hovering; Radiation Treatment's two horizontal cards keep neutral panel/labels but their handles visibly respond to hover. Standard aggregate tracks, world-loot, anomaly, stash-ammo, mutant-drop, economy and A-Life slider row cards must not tint their entire surface gold on hover. Standalone toggle/buttons remain buttons and may retain their normal surface-hover feedback. Do not alter CFG mappings, native slider ranges, baseline markers or input hit targets.


### 2026-10-09 — Anomaly hover syntax correction

The Anomaly Damage row fill must use a syntactically valid two-branch conditional (`selected ? accent-tinted : neutral panel`). A prior hover-only cleanup accidentally left a third `:` branch and broke compilation (CS1003, lines 182–183); this has been corrected without changing slider targets, source CFG parameters, reset behavior or the handle-only hover policy.


### 2026-10-09 — Mutant Health grouped horizontal redesign

Mutant Health now uses the approved Stamina Usage presentation: fourteen independent, alphabetically ordered species multipliers in **two columns of seven compact horizontal cards** (58 DIP each, 65 DIP row pitch, 12 DIP column gap) within a 526-DIP board. Each card shows one neutral species label, current multiplier in a compact top-right value, small calculated HP readout and one editable horizontal rail. The source/base tick stays grey; only a modified multiplier turns gold (+1.2 DIP), and hover glow belongs **only to the handle**, not the entire card or its text. Mouse drag mapping is now horizontal, and original source IDs, exact 0.05x lower limits, slider tick snapping anchored at 1.00x, keyboard input, per-species right-click reset, group R reset and source CFG mappings remain intact. No large vertical faders or redundant explanatory text.

### 2026-10-09 — Mutant Health compact row value alignment

User-approved change for the fourteen grouped horizontal Mutant Health cards: display the neutral mutant name on the left, the editable source multiplier centered horizontally in the card's top row, and the derived actual HP right-aligned in that same top row. Compare both dynamic values to the selected Base reference using the source parameter's modification state. When modified, the complete multiplier token (including x) and **only the numeric HP amount** turn accent yellow and increase by 1.2 DIP; the HP unit, mutant label, card background and slider remain neutral. Keep the two-column/seven-row Stamina-style layout, horizontal rail, handle-only hover glow, 0.05x–2.00x range, 0.05x editing step, reference ticks, original CFG mappings and reset behavior. No extra subtitle/footer or UI family redesign.

### Economy Trading — compact direct editing (2026-10-09)

The four source-bound trading editors are displayed as a two-column/two-row bank of horizontal cards when available width is at least 672 DIP, falling back to one column without squeezing labels. Their independent semantics are: player selling multiplier (`economy.buyCondition`), player buying multiplier (`economy.sellCondition`), minimum accepted gear durability (`economy.traderMinBuyCondition`), and zero-durability retained item value (`economy.itemValueFloor`). The first pair displays x multipliers; the second pair percentages. Every card uses Stamina-style neutral labels, current numeric gold only when source-modified, a gold circular active handle, grey Base tick, native endpoints, and direct horizontal editing. Original right-click reset and keyboard handling are retained. This supersedes the former long-form Trading Flow diagram and four stacked controls, **not** the remaining Economy visual families.

### 2026-10-09 — Economy reputation repair compact controls

User-authorized Economy redesign: replace the former read-only repair reputation curve plus four long source sliders with a compact two-column/two-row group of four **independently editable, horizontal Stamina-style cards** (single-column fallback below 672 DIP). The cards are FRIENDLY, NEUTRAL, DISLIKED and HOSTILE, preserving their original `economy.repairFriend`, `economy.repairNeutral`, `economy.repairDisaffection`, `economy.repairEnemy` source bindings. Show the current multiplier with neutral-at-Base/yellow-and-1.2-DIP-larger-when-modified formatting, a neutral one-line surcharge/discount cue compared with 1x, grey selected-Base marker, separate subtle 1x mark, original endpoint bounds and circular handle. Hover must highlight the handle only. Keep original direct drag, selected-slider keyboard edits and resets. The global linked constraint continues to enforce actual source-value ordering FRIENDLY <= NEUTRAL <= DISLIKED <= HOSTILE with priority given to the actively moved slider. No CFG writer/preset changes; 300 DIP wide and 526 DIP narrow board heights. This replaces **RepairCurve only**; the other Economy sections keep their recorded layout until individually converted.

### 2026-10-09 — Global concise-control rule (user-authorized)

Apply across all tabs and all reusable visual-control families: remove redundant instructional footer legends (gold=current, grey=Base, right-click/R reset), duplicate explicit Base reference prose when a reference tick already communicates the same information, and explanatory captions that do not add gameplay meaning. Retain unique game-mechanic explanations, real units, essential source semantics, value labels, state-dependent warnings, accessibility through tooltips, and native reference ticks. Preserve the existing visual family shape, draggable handles, source bindings, keyboard interactions, reset, preset persistence and CFG output. Render modified source numbers gold and +1.2 DIP larger only when genuinely different from the selected vanilla reference; all static text remains neutral. Do not globally strip meaningful subtitles/axis labels or change unrelated locked geometry to fill the vacated space without a specific redesign approval. First implementation pass removes repeated footers and Base prose from shared AggregateVisualControl families, including mixer, explosion, NPC equipment, world loot and toggle-led dashboards. This is a progressive cleanup contract; other dedicated custom renderers must be audited individually rather than assumed to have been changed.

### 2026-10-09 — Global compact-text cleanup: dedicated visual families


### 2026-10-09 — Trading source-card anatomy (current)

`TradingDashboard` now invokes the identical 80-DIP `DrawEconomyCard` renderer used by `CouponFlow`. Its original four sliders remain independently editable on a compact 2x2 grid, with one-column fallback, 94-DIP row pitch, 12-DIP inter-card gap, neutral labels/limits and one right-aligned live source value per card. The two transaction-direction scalers have `x` units; accepted-gear condition and broken-item retained value use `%` units (value * 100). Redundant explanatory rows were removed; the direction remains explicit in YOU SELL / YOU BUY labels. Hover is handle-only, reference markers are unobtrusive, and current numbers become gold only when source-modified. The new board measurement is 248 DIP (wide) or 436 DIP (narrow). The older Trading descriptions using 106-DIP cards, long meaning rows, or 300-/536-DIP board heights are superseded. Coupon cards are unchanged and remain the common Stamina Usage layout reference. No change to native CFG semantics, values, stepping, reset or presets; Wine check pending.


### Economy — six complete compact source-bound boards (2026-10-09)

This entry **supersedes** the historical Trading Flow/Repair Curve diagrams and the former Services/Trader Stock vertical-mixer examples in this document. The earlier sections remain as development history; the current source renderer contracts below are authoritative alongside `docs/UI-DESIGN-LOCK.md`:

| Board | Source VMs (all independent) | Layout |
| --- | --- | --- |
| `CouponFlow` | `startingMoney`, `mainQuestReward`, `sideQuestReward` (economy namespace) | 3 cards, two columns then one centered; one column at narrow widths |
| `TradingDashboard` | `buyCondition`, `sellCondition`, `traderMinBuyCondition`, `itemValueFloor` | Existing compact four-card layout |
| `TraderStockBoard` | `traderAmmoStock`, `medicStock` | 2 compact horizontal cards |
| `ItemPriceScalersRadar` | `consumableCost`, `ammoCost`, `armorCost`, `weaponCost`, `artifactCost`, `mutantPartValue` | 2×3 compact horizontal source cards |
| `ServiceCostsBoard` | `fastTravelCost`, `upgradeCost`, `repairCostBase` | 3 compact horizontal cards, 2+1 centered |
| `RepairCurve` | `repairFriend`, `repairNeutral`, `repairDisaffection`, `repairEnemy` | Existing four-card horizontal layout, real CFG-value linked ordering |

All IDs above have prefix `economy.`. There are **22** source-editable Economy values. `economy.repairDifficultyCost` stays audit-only and nonvisible. Service price values are not collapsed into reputation price, trader payout formulae are not invented, starting coupons affect only new-game initialization, and Mutant Parts retains its actual per-item `Cost` scaling.

The reusable `DrawEconomyCard` grammar and Item Value Layers renderer preserve one true numeric value, native endpoints and steps, grey Base tick, gold handle and handle-only hover glow. Only the number changed from selected Base is accent-colored and +1.2 DIP larger. No Base prose, bottom legend or long footnote is rendered. Right-side parameter rationale remains available for fuller technical context. These are source-value editors, not fake previews.

### 2026-10-09 — Economy latest UI grammar: bounded cards and repair handles

This **supersedes** the earlier Economy examples that depict overly long horizontal cards, 106/104-DIP Trading/Repair editors, 300/536/526-DIP Trading/Repair boards or redundant reputation surcharge text.

- **CouponFlow** (3), **TradingDashboard** (4), **TraderStockBoard** (2), **ServiceCostsBoard** (3) and **RepairCurve** (4) use the **same real source editor** `AggregateVisualControl.DrawEconomyCard`, with 80-DIP cards, 94-DIP row pitch, title left/current editable value right, gold track/handle and muted min/max and reference tick. Hover shows the exact common `ControlHoverGlow` halo **on the handle alone**, never gold card backgrounds/text. Current numeric glyphs and attached `x`/`%` go gold and +1.2 DIP only if source differs from reference. Title width is based on the measured live number, clipped to one line.
- **Internal card width max = 370 DIP** with two columns at editor width >=672 DIP, **440 DIP** with one column below that. The complete cards are centered inside their respective full-width top-level section. Columns have 12-DIP spacing, short final Coupon/Service rows have centered cards. Board height is calculated with the real row count: one row 154 DIP, two rows 248 DIP, three rows 342 DIP, four rows 436 DIP. All source hit rectangles and slider tracks follow the centered cards.
- **RepairCurve** retains distinct FRIENDLY/NEUTRAL/DISLIKED/HOSTILE sliders and the global source-value ordering/cascade. It now uses the shared compact 248-/436-DIP bank without the redundant per-row surcharge/discount caption. Its subtle extra dashed 1x tick indicates **no reputation modifier** only when separate from the grey selected-reference tick; it does not change the reference or represent a completed repair-cost calculation.
- **ItemPriceScalersRadar** keeps six independent 0–2x item-category multipliers, including independently sourced mutant-part `Cost`; its 78-DIP cards remain in a 2×3 grid, 90-DIP pitch, 326-DIP board, and are capped at **370 DIP** per card and centered together. Labels reserve measured live-value width and ellipsize without overlapping the number. The existing global handle-only glow, native ticks, hit targets, keyboard/drag and reset behavior remain.
- Trader Stock is **supply multiplier**, not an invented visible item count; Guides/Upgrades are `x` and repair base price uses the genuine source `%`; Buying/Selling retain direction-specific `x` values, while the condition gate and broken value use genuine `%`. Starting Coupons remain raw coupons, advancing in 10-coupon ticks. No other source, CFG-writer or preset behavior changes.

### Economy QA requirements

At full and minimum 16:9 window sizes, verify that all 22 source controls appear once, within their intended six boards, with no missing parameter placeholders. Confirm drag and keyboard steps preserve the native slider limits, coupon amounts snap to 10, Trader/Medic stock snaps to 0.25x, single-source right-click reset, R reset-all per family, source-modified highlighting, correct selected-difficulty reference ticks and preset-save round trip. Build and Wine visual acceptance is still pending.

### 2026-10-09 — Global Stamina Usage slider anatomy and neutral references (latest user-approved rule)

**This entry supersedes conflicting older slider-value positions and any requirement to visually emphasize Vanilla/Base values across ALL tabs.** For every ordinary horizontally editable source slider, use one coherent, non-overlapping top line: **source name on the left, editable CURRENT value right-aligned** (shared card left/right padding and reserved space so the label cannot run beneath the current value); place the continuous interactive rail directly below that line, and display any native min/max labels underneath the rail in the same neutral small typography. Do not alternate between left-justified current values on the next line, right-justified current values in the same family, or side-by-side current/Base figures. When a separate preview/semantic graph needs read-only numeric results, keep those unambiguously labeled as derived results rather than a second slider value.

Current source numbers remain text-colored at the selected reference and turn gold **only after a real source deviation**; modified current numbers grow by +1.2 DIP while all units, bounds, captions and explanatory text stay neutral. The interactive rail/handle can retain its gold affordance, but on hover only the **handle** glows. Never color a full card or the displayed value merely because the mouse hovers. **Vanilla/Base references are not highlighted**: retain a thin, unobtrusive grey tick for source comparison and native snapping/reset where already supported; where standard `VanillaMarkerBar` displays clickable reference labels, keep the labels/dots muted even during hover. Do not place gold/white duplicate Base numbers or instructive legends in card headers or footers.

The reusable `AggregateVisualControl.DrawHorizontalTrackCard` and `DrawCompactTrack` now follow this exact hierarchy. This affects shared health, stamina, fatigue, survival, loot, equipment and economy layouts without changing source bindings or hit-target semantics. `DrawDualParameterHeading` uses independent left-title/right-current pairs even for the two-axis graph. Existing Trading, Reputation Repair, Coupon, Service, Stock and Item Value cards already place the live value on the right. The compact vertical mixers, native semantic curves, separate multi-axis controls and Mutant Health's previously approved **species left / multiplier centered / actual HP right** remain differentiated where the meaningful control structure requires it; their **reference readouts still stay muted**, and vertical mixer editable numbers are no longer yellow at Base. Dedicated Action Costs, Medical Healing, Special Ability, Human/Mutant group, Anomaly Damage, Mutant Drops and World Loot horizontal rows use the same changed-number emphasis rule.

This is a visual presentation change, not a global rescale: preserve sliders' native min/max, source/CFG stepping and mapping, actual Base reference values, value-based cascades, right-click/R reset, selected-parameter rationale, preset round trips and writer output. Do not substitute a universal 1x or mask Base values in internal calculations. Local publish and Wine inspection are required to verify text widths and min-width layouts; do not claim they passed without running them.

### 2026-10-09 — One universal hover glow across every interactive control

**User-approved global visual change; supersedes all earlier per-family hover halo sizes, opacities, double-rings, whole-card hover backgrounds and selection-only glow.** All hand-drawn editor handles use the single helper `Controls/ControlHoverGlow.cs` with **halo radius 15 DIP / opacity 0.17** and **outline ring radius 11 DIP / opacity 0.67 / 1.6-DIP stroke**, drawn using the existing gold AccentBrush. Hover state alone enables it. Selected/focused/modified-but-not-hovered controls do not manufacture this glow.

- Shared aggregate renderers determine the current editable handle position from the actual source mapping and show the halo **only on the hovered active hit target**. Combined graph rings reuse the same helper at their actual drawn point. Toggles receive the same indicator-sized halo, not a glowing entire panel. Hovering a Base/Vanilla reference tick, selection-only area, static preview or noninteractive legend does not glow or edit values.
- Ordinary WPF controls use `UniversalHoverGlowEffect` in `App.xaml`, matching gold, radius 15 and opacity 0.67 with zero shadow depth. The same resource drives Button/PrimaryButton/window buttons, preset ComboBoxes and their options, TabItem, TextBox, CheckBox switch **thumb**, native Slider **thumb** and wheel-editable value box. On slider mouseover the track, label and current numeric text stay neutral; only the thumb gets hover effect. The ordinary parameter-card background must not change because a mouse is merely over the card.
- Disabled controls remain non-editable and do not receive active handle halos. Preserve focus styling, keyboard/touch/mouse behavior, native snapping, preset reset, CFG and value-reference calculations. Handle-only hover is strictly visual; no new gameplay parameter or hit surface.

The canonical drawing parameters are deliberately fixed in one class and the WPF equivalent is a single named effect resource. Do not introduce another custom blur, radius, selector-opacity or full-card hover wash in later UI work without an explicit user-approved change. Local publish + Wine acceptance is still required.

### Hunger Survival Profile — fixed stage caption clearance (2026-10-09)

**Current authoritative override** to the 2026-10-08 one-line stage-footer notes: real Wine view showed long `MEDIUM AT N PTS | -N% STAMINA RECOVERY` and `SEVERE AT N PTS | -N% STAMINA RECOVERY` sentences truncated in three narrow footer columns. Keep the profile's plotting/interactive geometry and aligned read-only preview, but switch to separate short centered footer lines within three fixed plot-aligned columns:

| Stage | Line 1 | Line 2 | Line 3 |
| --- | --- | --- | --- |
| LOW | LOW | AT N PTS | — |
| MEDIUM | MEDIUM | AT N PTS | -N% RECOVERY |
| SEVERE | SEVERE | AT N PTS | -N% RECOVERY |

The editable threshold and penalty numbers color independently and grow +1.2 DIP **only if source-modified**. The chart Y-axis already defines the meaning of RECOVERY. Footer begins at frame offset +269 DIP, below X-tick text; stage names start +270, threshold centers +296, penalty centers +316. Stage zones are plot-aligned thirds and clipped only as a safety boundary, not to hide long text. Increase the chart frame from 294 to 344 DIP, using the same `HungerProfileHeight` constant in renderer and measured height `CompactHungerHeight` so WPF scroll layout reserves sufficient space. Redundant drag-help subtitle is removed. The 0–100 axes, horizontal alignment, handles, native parameter bounds, reference ghosts, hover glow and cfg writer are unchanged.

### 2026-10-09 — Universal minimal checkbox design (explicit approval; latest rule)

**Supersedes the earlier WPF pill switch, per-panel checkbox shapes, ON/OFF status-lane pills and the 2026-10-09 global hover-halo requirement specifically for boolean checkboxes.** Across all tabs, every ordinary boolean input renders as the same compact **20 × 20 DIP square**, 3-DIP corner radius, 1-DIP neutral border and dark `PanelAltBrush` face. The checked state is communicated **only by the gold checkmark** (two rounded 2.2-DIP strokes), not by a yellow card, yellow text, yellow track, extra ON/OFF caption or a glowing outline. The unchecked state has an empty square. Static control names remain neutral and adjacent to the box; long names fit in the remaining width without overlapping it.

**Hover policy for boolean controls only:** a slightly lighter neutral border on the square itself (`RadarMutedTextBrush`), no drop shadow, no halo, no animation, no changed font or card background. No ON/OFF label highlighting. Keyboard focus and checked state remain distinct from hover; disabled checkboxes are not editable. The broader universal `ControlHoverGlow` continues to govern sliders and graph handles; **checkboxes are deliberately exempt**.

**Single implementation contract:** WPF's global `CheckBox` template in `App.xaml` and the drawn `BooleanCheckboxChrome.Draw` in `Controls/BooleanCheckboxChrome.cs` share exactly the same size, corner radius, border, stroke, color and hover behavior. `AggregateVisualControl.DrawTogglePill` uses this chrome for all toggle-led panels (World Loot Layout, bundle toggles, healing/radiation/fatigue, starvation, equipment mechanics); `MedicalHealingRadar` uses it for Food Healing. No old slider-style toggle halo overlay may activate for `HitKind.Toggle`. Regular WPF parameter checkbox rows keep one label at left and the same square at right, without repeating ON/OFF.

**Nonvisual behavior unchanged:** each boolean retains its real `ParameterViewModel.CurrentBoolean`, enabled state, existing clickable row/target, click/Space/keyboard selection, right-click reference reset, visual-family R reset, preset persistence, source catalog and CFG writing. The Food Healing card remains clickable in its existing hit area, but only hovering the 20-DIP checkbox brightens its border. No changes to sliders, graphs, native step limits or gameplay values. Inspect the WPF template and draw-based panels under Wine; source/UI geometry checks are not a substitute for a runtime build.

### Health panel font scale — Core Health and Medical Healing (2026-10-09)

**Current presentation override for these two panels only:** Core Health and Medical Healing share `HealthTypography` values of 12.5-DIP panel titles, 11.4-DIP bold control labels, 12.8-DIP live current/HP figures, and 10.4-DIP neutral scale/endpoints. Edited numeric readouts grow by the existing +1.2 DIP to 14.0 DIP with gold numeric highlighting. The `MEDKIT HEALING` section label is 11.4 DIP; `HP RESTORED` and numeric chart Y ticks are 10.4 DIP. Medkit labels also use 11.4 DIP. Earlier 8.4-DIP graph ticks and 9.0-DIP health-card endpoints are superseded; these sizes are not to be used in these two health panels.

The reusable `AggregateVisualControl.DrawHorizontalTrackCard` receives `healthTypography: true` only from `DrawCoreHealthDashboard`. It measures the current output's actual width to reserve space for the source name without truncation at smaller layouts. Other consumers of the same helper preserve their earlier font scale and appearance. No changed position, native control count (Core Health five; Medical Healing Bandage/three medkits/Food checkbox), graphics, reference behavior, focus, keyboard or mouse input is authorized. Use the shared `HealthTypography.cs` values rather than reintroducing independent tiny literal font sizes. Build/visual verification under Wine is still required.

### 2026-10-09 — Combat tab gamer-centric redesign (explicitly approved; authoritative)

The user approved the four-part Combat wireframe and specifically requested source+docs deployment on `main`. **This is the latest authoritative Combat UI design and supersedes conflicting 2026-10-08/09 vertical cooldown-fader, Human Combat side-fader, and oversize Mutant Health min-height notes.** Keep four separate **full-width (1×1)** sections in existing order, with only one in-card centered gold heading per section:

1. **YOUR WEAPONS** (`PlayerWeaponProfile`): three independent compact **270° rotary knobs** in one row — WEAPON DAMAGE, HEADSHOT DAMAGE, JAM FREQUENCY — with centered current multiplier, gold arc, muted Base tick, native endpoints and **0.25x** steps. The active measured board is **242 DIP**, not the superseded 169-DIP horizontal layout. Preserve the three actual CFG fields and per-source resets; no DPS/jam-probability simulation. The 2026-10-11 knob contract is authoritative.
2. **HUMAN ENEMIES** (`HumanCombatProfile`): keep the square **180–280 DIP 2D graph** with X=enemy health and Y=enemy gun damage, independent gold exterior drag handles and an interactive central joint. **Replace the three right-hand vertical faders** with three **90-DIP horizontal cards stacked vertically**, ATTACK FREQUENCY, FIRE ACCURACY, EXPLOSION DAMAGE, with current values right-aligned and native min/max below. Fixed graph start (64,91), cards start X=plot.Right+16, Y=91/192/293, equal measured right-lane width; unchanged 465-DIP frame. The frame no longer renders the redundant bottom drag/axis legend: the axes and joint tooltip already explain interaction. Graph axis current values are neutral at source reference and gold/+1.2 only if modified. Attack Frequency has the verified 2x maximum and Fire Accuracy 1x maximum; native CFG steps and the explosion source remain unchanged.
3. **MUTANT COMBAT**: a compact standalone `combat.mutantDamage` slider using the standard Stamina-style label / current value / rail layout. Keep source-accurate endpoints and subdued reference ticks; no aggregate board or duplicate instructions.
4. **MUTANT HEALTH** (`MutantHealthRadar`): preserve the approved two-column seven-row Stamina-style card layout, alphabetical order of **14** independently editable mutants, native **0.05x–2x** source range and **0.05x source step**. Keep species name on the left, editable multiplier **centered**, derived actual `HP` **right-aligned**, single rail below, grey reference tick and handle-only glow. Improve tiny numbers without moving handles: species name **10.8 DIP**, multiplier **11.2 DIP**, actual HP **10.5 DIP**, and numeric change highlighting still +1.2 DIP. WPF minimum is brought into alignment with the unchanged renderer's **526 DIP** measured height (instead of the stale 672-DIP blank-space minimum). The target values `VanillaHp × sourceMultiplier` remain unchanged; no fake boss/Amok control.

**Global rules:** all displayed current numerics are neutral if equal to the selected difficulty reference; on change, only the numeric token (and attached ×/x) turns gold and grows 1.2 DIP; static labels and native endpoints remain neutral. No full-card hover, no hover on Base reference, no repeated section title, no instruction footer, no checkbox changes. Preserve existing same-source hit targets, source range/bounds, catalog steps, reference reset, keyboard drag semantics and preset/CFG writer. Psy/Repeated-Hit family mappings remain available in the catalog but are not added as new top-level Combat cards. Do not change the gameplay parameters, source ranges or writer from this UI redesign.

**Acceptance after publish:** verify that all visible Combat families render once and their full labels/values remain readable at normal and minimum WPF sizes; hover is identical. A local Wine build and visual test is still required.

### Global CFG-to-visual selection grammar (approved 2026-10-09)

The visual catalog is a **selection system, not a slider-only library**. Before redesigning a tab, audit every relevant catalog source (`parameters.json`, constraints, native steps/ranges, authored relations and CFG writer) and make an explicit decision for each semantic group:

| CFG relationship | Preferred editable representation | Reject when |
| --- | --- | --- |
| One scalar source | Compact slider/ruler | A decorative chart would obscure the real current value |
| Two meaningful independent scalar axes | X/Y interactive graph with source-accurate axis and optional joint handle | Either axis is synthetic or the joint cannot write both |
| Discrete threshold/effect stages | Editable stepped profile or threshold band | A smooth line suggests nonexistent interpolation |
| Real timing windows/intervals | Timeline with editable native event points | The depicted cadence is stochastic or inferred |
| Source-backed min/max / ordering | Dual-handle range band, native-value cascade | Relative baseline percentages hide required absolute ordering |
| Comparable category channels | Compact grouped bank, proportional bar or meaningful radar | Units/scales are incompatible or normalization is unexplained |
| Boolean | Locked minimal 20-DIP checkbox | Full-card hover or glowing switch competes with checked state |
| Enum/discrete option | Segmented choice / native selector | A continuous slider would invent intermediate states |
| Actual derivable result | Read-only preview with explicit unit and proven formula | Game/runtime behavior is not deterministically supported by CFG |

**Interaction invariants:** no synthetic gameplay knobs; preserve native source IDs, limits, tick snapping, constraints, CFG/preset round-tripping, selected vanilla-difficulty reference and individual right-click/R resets. Current editable numbers are neutral at reference, yellow and +1.2 DIP on genuine change. The canonical hover halo belongs only to interactive handles; checkbox hover is deliberately restrained. Graph axes must name real units, handle geometry must match the CFG transform (including inverses), and labels/values must not overlap at minimum editor width. Every chart must provide equivalent discrete keyboard access through its real underlying source parameters. Avoid extra descriptive footer copy when labels and controls already communicate meaning.

**Process:** propose a compact wireframe and map each visual back to source parameters; seek user approval for a tab redesign; then update the implementation, `UI-DESIGN-LOCK.md` and this file together in the GitHub `main` branch. This approval broadens the available control vocabulary but **does not silently unlock existing layouts** or authorize replacing a working control without a specific request.

### 2026-10-09 — Survival tab gamer-centric CFG-first redesign (approved; current)

**This is the authoritative current Survival UI contract.** It supersedes the older "read-only duration preview + five vertical duration faders", "smooth bleeding-damage curve", "long one-line fatigue captions", "three intoxication tiles", and overly small stage/axis typography. The user explicitly approved the seven-section Survival redesign based on the catalog's **43 Survival definitions**. All real editable source controls remain available: 7 Hunger, 10 Fatigue, 9 Consumable Effects, 2 Alcohol Strength, 4 Sleep, 3 Radiation, 7 Bleeding, with one additional audit/hidden drunkness-threshold scaler not made into a fictitious new editor. No change to source IDs, CFG write paths, native min/max/steps, reset/presets, select-difficulty reference numbers, gameplay calculations, or established constraint cascades.

1. **Hunger State Ladder** (`HungerLadder`): preserve the accepted synchronized 0–100 HungerPoints preview and threshold-linked *discrete* stamina-recovery loss profile. The three thresholds stay native editing sources and Medium/Severe loss remains true positive stored magnitude / negative UI effect. Keep live stage labels in fixed cells and avoid smoothing authored step changes. Improve the stage preview heading to **11.0 DIP**. No fabricated elapsed-time-to-hunger simulation.
2. **Fatigue Response** (`FatigueCurve`): keep the two independent, real-Y-scale gold AIM TIME and pale RELOAD TIME stepped profiles with shared real 0–100 SleepinessPoints thresholds; LOW has no reload source. Preserve the separate white Y-only reload handles. Widen the footer vertically from one clipped sentence to one fixed **four-line-capable** stage cell in each third: LOW/MEDIUM/SEVERE label; `AT N PTS`; `AIM +N%`; where authored, `RELOAD +N%`. No forced LOW reload. The actual frame and `CompactFatigueHeight` must both use **365 DIP** instead of 320; lines at frame Y+262 / +282 / +304 / +326 and cells Y+260 height93, safely below X-axis ticks. Use 11-DIP stage preview headings and 10.2-DIP real axis captions. Golden modified numbers only when the corresponding source differs from the difficulty reference.
3. **Consumable Effects** (`ConsumableMixer`): retain four independent 0–2x top strength sliders with original native 0.05 steps, and three concise *read-only* effect readouts for Water/Energy/Limited Energy instant stamina, Water/Energy sustained buffs and Beer/Vodka radiation removal. The effect readouts now place **one effect per line** rather than clipping pipe-separated sentences; up to three 9.8-DIP effect lines under a 10.4-DIP title, highlighting/enlarging only changed numeric tokens by +1.2 DIP. No extra duplicate readout headings.
   
   **Replace all five vertical duration faders and the read-only longest-effect preview with one directly editable common-seconds timeline.** Five horizontally independent native source lanes are labeled WATER, ENERGY, VODKA, BEER and MAGIC VODKA. Its common **0–270 real-second** ruler shows 0/90/180/270 seconds. Proven longest authored duration prototypes are **30 / 45 / 20 / 20 / 90 seconds**, respectively, scaled by the same original native **0–3x multiplier** and **0.05x step**. Each lane renders an interactive gold handle at `SecondsX(authoredSeconds × currentMultiplier)`, a thin neutral selected-reference tick, a subdued uneditable tail *beyond that lane's own native maximum*, and a right-aligned exact current longest duration in seconds. WATER's shorter 5-second effect and ENERGY's shorter 5/15-second effects appear only as passive minor ticks at their scaled times, **not as independent controls**. Do not imply all five families can last 270 seconds. Native hit mapping uses the lane's physically shorter `0..authoredSeconds*3` track, rather than stretching the source multiplier across the entire 270s axis; any grey tail cannot receive hover/edit. `AddHorizontalTarget` retains source-native snapping, track selection, focused arrows and right-click/R reset. There must be **exactly nine editable sources** (four strengths + five durations), not duplicated read-only and interactive duration handles. Remove unused `DrawConsumableFader` and `ConsumableFaderColumns`, and measure actual new panel bottom as `previewTop + ConsumableTimelineHeight + 14` rather than retaining the stale 179-DIP fader bank.
4. **Alcohol Tolerance / Drunkenness** (`DrunkennessControl`): keep the two native Beer/Vodka strength sliders and their deterministic already-audited number-of-drinks stage previews. The progression captions use legible ~10-DIP type. Four *read-only* fixed stage tiles now represent **SOBER (below 0.001), TIPSY (0.001+), DRUNK (30+) and WASTED (80+)**, with two separate lines per tile at minimum width and no fake thresholds. The existing hidden `survival.drunknessThresholdScale` stays hidden and OverDrunkness is not folded into the graph. Static stage boundaries and labels are muted, the source value and derived WASTED-after-N count are gold and +1.2 DIP **only if the corresponding Beer/Vodka source is modified**, and the old yellow VANILLA baseline caption disappears; its muted source-reference tick remains. All native source steps, mouse, keyboard, reset and saved preset semantics are unchanged.
5. **Sleeping System** (`SleepingDashboard`): preserve two distinct native access controls (required fatigue points / minimum hours) and two distinct true wake-up effects (HP / stamina effect), with no invented overnight recovery timeline or simulated sleep mechanics. Increase the RECOVERY WHEN YOU WAKE UP section heading to **11.4 DIP**; current source-value cards, reference markers and original editable mappings stay unchanged.
6. **Radiation System** (`RadiationBalance`): intentionally preserve **three independent editable sliders** for difficulty-scoped exposure multiplier, absolute passive points-per-second radiation recovery and harmful-radiation health-damage scalar. A combined exposure-minus-clearance graph or predicted HP/s curve would mix incompatible native units and pretend unverified runtime math. No composite "radiation balance" scalar is added. Existing native source/units, selection and resets remain.
7. **Bleeding** (`BleedingCurveControl`): retain two independent top sources, Bleeding Effect Strength and Natural Recovery, and all five lower native editable stage damage/threshold sources. Render the *current and dashed reference damage path as true LOW→MEDIUM→SEVERE **horizontal steps with vertical transitions*** rather than lines interpolating between discrete authored states. The accent fill follows the same stepped polygon to the chart's right edge. The existing native damage handles and separate Medium/Severe threshold handles stay at the true authored points, including ordered thresholds. Raise chart and native axis typography to ~10–11.4 DIP, and enlarge each independent stage caption zone to **44 DIP** with `STAGE AT N PTS` and `N HP/TICK` as distinct rows; no longer clip 35-DIP footer cells. The 458-DIP frame and existing pointer/CFG mappings remain unchanged.

**Shared invariants:** current values remain neutral at the selected vanilla difficulty reference; yellow and +1.2-DIP size apply only to source-modified numeric tokens; thin reference marks never receive highlight; labels do not overlap or turn gold solely on hover; all handle halos use `ControlHoverGlow` and boolean editors remain universal minimal checkboxes. Do not add redundant on-panel instructions or unrelated simulated gameplay. This contract applies only to Survival and does **not** unlock Combat or any other established tab. Full compile and Wine appearance checks remain the local acceptance step; source-level verification is not equivalent to having run them.

### Health — native-value graphic editors (approved 2026-10-09)

**Supersedes the 2026-10-09 Health compact-card snapshot.** Each of the 16 source controls remains editable once in four full-width groups. Core Health (5) has one expanded MAX HEALTH ruler and four 2×2 recovery lanes; Medical Healing (5) retains Bandage, Food checkbox and three directly draggable HP bars; Bleeding Treatment (4) presents four directly draggable shared-unit columns; Radiation Treatment (2) presents two source-clipped horizontal common-unit lanes. Their measured heights are **348 / 400 / 376 / 250 DIP** in section order. No additional preview-only replicas or invented HP/s/radiation-balance formulae.

For medical bars the plotted shared HP axis is 0–200; native editable ceilings are 140, 170 and 200 for Medkit, Army and Scientific. For bleeding-removal bars the same 0–200 visual-unit principle applies with native editable ceilings 200, 30, 70 and 50. Each vertical target's `MapRect` begins at the plotted native ceiling, and its `VisualMaximum` is the real source maximum, preserving exact native chart Y mapping and hit testing; data remain in 5-point steps. Radiation's two lanes share a fixed visual 0–200 scale, but the Scientific lane's editable `MapRect` physically stops at 120 while Anti-Rad continues to 200. Grey beyond-native tails are never hit targets. The HP ruler reads real 25–200 MaxHealth in 5-HP steps and does not depict current in-game HP damage.

All charts use true CFG source points, the existing `ControlHoverGlow`, a tiny muted per-source reference tick, current-value-only gold highlighting (+1.2 DIP when changed), neutral labels and accessible direct mouse/keyboard source edits. Food Healing retains its neutral square checkbox with minimal-border hover. Existing `HealthTypography` stays in force and no other tab's controls are restyled.

### 2026-10-09 — Approved Stamina four-panel wireframe (implemented on main)

The approved four-card Stamina arrangement is implemented with **two visual families and one split editor family**, not an aggregate all-in-one knob.

| Tab order | Source-bound UI family | Editable sources | Actual interface |
| --- | --- | ---: | --- |
| 1. Stamina & Movement | `AggregateVisualControl / StaminaCore` | 4 | Compact 2×2 horizontal native sliders; Vault body-reference landmark snapping |
| 2. Stamina Usage | `ActionCostsRadar / Mode=Usage` | 6 | Two columns of three action SP costs, Movement and Melee |
| 3. Exhaustion & Recovery | `ActionCostsRadar / Mode=Exhaustion` | 6 | Independent Sprint Lock/Recovery Below on common 0–60 SP axis; four native recovery/slowing/time source cards |
| 4. Water Stamina Drain | `AggregateVisualControl / WaterStaminaPressure` | 2 | Two compact native multiplier sliders in one row |

**Actual Stamina source map (18 visible controls, no synthetic parameters):**
- Core: `stamina.maxSP`, `stamina.regeneration`, `stamina.vaultMaxHeight`, `stamina.maxCarryWeight`.
- Usage: `stamina.sprintCost`, `stamina.jumpCost`, `stamina.vaultCost`, `stamina.meleeNormalCost`, `stamina.meleeStrongCost`, `stamina.butstockCost`.
- Exhaustion: `stamina.sprintDisableThreshold`, `stamina.lowRecoveryThreshold`, `stamina.aimRegenCoef`, `stamina.sprintRecoveryDelay`, `stamina.lowRecoveryStrength`, `stamina.lowRecoveryDuration`.
- Water: `stamina.waterMediumDrain`, `stamina.waterStrongDrain`.

The two `ActionCostsRadar` instances bind to the same live `AllParameters` store but each mode subscribes to, paints and resets **only its own six source values**. Their anchors are `stamina.sprintCost` and `stamina.sprintDisableThreshold` respectively; `ParameterViewModel.IsControlVisible` hides other family members as standalone rows. Their modified-state provider scopes are likewise independent. The explicitly configured Stamina sort places the anchor rows in approved order. The water family uses `DrawWaterStaminaBoard` rather than reusing the unrelated radiation treatment renderer.

Exhaustion threshold tracks have a *common visual domain* of 0–60 actual SP with independently native-limited hit mapping (Sprint Lock 0..32.2, Recovery Below 1..60). The right recovery cards do not normalize different units onto the threshold axis. Usage's native raw SP values are editable via existing relative-to-reference two-times rails and retain original snapping/constraints. Water retains source-native 0–2x and 0–3x domains and the native Medium ≤ Deep ordering. All 18 source parameters remain part of the original catalog/preset/writer graph. The five internal movement-state coefficient definitions stay unexposed.

Visual contract: compact dark panels, one centered gold section title, neutral source labels and units, value above rail, only genuinely modified numeric tokens highlighted +1.2 DIP, understated grey reference marks, handle-only gold hover. No redundant explanatory text, current-vs-Base tables or made-up simulation. Wide mode height 240 DIP for Exhaustion with threshold and 2×2 cards side-by-side; narrow mode 419 DIP with thresholds stacked above the 2×2 bank. Confirm the WPF live result in Wine; source checks alone do not establish visual acceptance.


### 2026-10-09 — Loot tab six-section gamer-centric redesign (approved)

| Display order | Family renderer | Normal editable sources | Display rule |
| --- | --- | ---: | --- |
| 1. World Loot Depletion | `WorldLootRemovalRadar` | 5 | 2-column/3-row compact percentage cards; dedicated F1 boxes independent |
| 2. World Placement | `AggregateVisualControl / WorldLootLayout` | 5 | 4 universal small checkboxes, one dependent weapon-density rail |
| 3. NPC Loot & Equipment | `AggregateVisualControl / NpcLootLoadout` | 4 | discrete 3-option selector, 2 scalar sliders, 1 two-endpoint MIN/MAX source range |
| 4. Stash Rewards | `AggregateVisualControl / StashRewardProfile` | 6 | three tier ammo quantities and three independent bonus-roll chances/counts |
| 5. Breakable Boxes | `AggregateVisualControl / AmmoDistribution` | 6 | 2 yield sliders, 4 source-backed rotary knobs, two linked *read-only* pool composition bars |
| 6. Mutant Part Drops | `MutantDropRadar` | 14 | 2 × 7 alphabetized independent 0–100% sliders |
| **Total** | | **40** | No additional source, generic difficulty, invented loot event or hidden duplicate editor |

### Breakable Boxes — caliber identity is a first-class chart requirement

`AmmoDistribution` contains the four existing independent **relative weight** sources:
- `loot.ammoBoxWeight918` — label **9×18 / 9×19**. SNG has two underlying entries using this weight, so SNG numerator is `2 × weight918`; NATO has just one 9×19 entry, numerator `weight918`.
- `loot.ammoBoxWeight045` — **.45 ACP**, NATO only.
- `loot.ammoBoxWeight545` — **5.45 / 5.56**, SNG shows **5.45**, NATO shows **5.56**.
- `loot.ammoBoxWeight012` — **12 Gauge**, both pools.

For the **SNG POOL** stacked bar the normalized current segments are `(2*w918, w545, w012)/sum` in that order. The **NATO POOL** segments are `(w045, w918, w545, w012)/sum`. Each bar displays a permanent **swatch + caliber name + exact share percentage** for *every* segment immediately below the drawn bar **inside the same bordered chart card**, regardless of whether a 0% segment becomes invisible. Swatch and bar segment draw from identical source-color family indices `918=0, 045=1, 545=2, 012=3`, opacities `[.95,.72,.50,.30]`. **Do not assign palette by segment index** (because SNG and NATO orders differ); this would falsely imply different meanings for the same source weight. `Source weight=0` retains the grey/zero numerical current value as required while the read-only legend remains visible. Changes to relative weights do not affect which ammo membership entries exist or how many rounds each box grants.

Both yield sliders and all four rotary knobs retain their individual source range/step and right-click/R semantics. The two additional `loot.destructibleAmmoQuantity` and `loot.destructibleGrenadeQuantity` sources are now members of `AmmoDistribution`; the former standalone `BreakableBoxYields` has no visible anchor. The previous vertically extended read-only per-pool Base percentages and long prose are intentionally removed. The parameter catalog/CFG writer remains authority for raw input.

Other Loot panels follow the approved `UI-DESIGN-LOCK.md` mapping. `StashAmmoRadar` is retired from the visible tab without deleting its existing source values; `StashRewardProfile` contains all six related sources and owns the `loot.cheapStashAmmoQuantity` anchor. Minor source and font changes in `WorldLootRemovalRadar` and `MutantDropRadar` only affect the Loot tab. No other tab UI is reauthorized. Wine acceptance pending.

### 2026-10-09 — Source-verified NPC Ammo / Breakable Box Ammo boundary

**Control membership and hit architecture:**
- `NpcLootLoadout` remains one aggregate anchor `loot.npcCorpseBonus`; it owns four independent source IDs: `loot.npcCorpseBonus`, `loot.npcMedicalChance`, `loot.npcWeaponAmmoSupply`, `loot.droppedWeaponCondition`. Its current 496-DIP layout supersedes the prior 354/401/463-DIP panels.
- `EXTRA CORPSE LOOT` draws three compact capped-width **chips** centered within the option frame. Each chip has a distinct full-rectangle `HitKind.Choice` hit area and a fixed `VisualMinimum=VisualMaximum=choiceIndex`, so click and drag cannot accidentally select the next tile. Hover is shown by the chip's border; keyboard arrows navigate the three option indices, Enter/Space preserve the selected choice. No gap click and no range-like choice bar.
- `NPC AMMO` is **`loot.npcWeaponAmmoSupply` in the persistent preset**. It is now a *source-derived generic corpse SmartLoot multiplier*, not weapon-attached NPC spare ammunition. The normal 0–3x / 0.25x native CFG slider remains and shows compact source-grounded Newbie `NPC_Ammo_Smart` examples: 12 GA 2–6, 5.45 15–20, .45 ACP 8–12 at 1x, including zero and half-up rounding at other values. The sample panel explicitly notes when `Medicine + Ammo` is required for activation. No other NPC source or Breakable Box count is used for these examples.
- `DROPPED WEAPON CONDITION` is still one source with two separately editable values; `RangeMin/RangeMax` now map *horizontally* on the native 0–100% rail and `FindTarget` chooses by nearest current X/2D handle distance rather than top/bottom-only lane or last target in list. 38-DIP high clickable surface, stable drag capture, separately staggered visible handles, hover centered at each actual handle, native 1% snap and automatic minimal boundary cascade. Clicks on empty rail choose nearest endpoint. Right-click resets whole range; R resets the source family.

**CFG binding — three strictly distinct ammo domains:**
1. `NPC_Ammo_Smart` / `GameData/StashPrototypes.cfg`: **43 item MinCount/MaxCount** across **24 rank-specific groups**; linked only through the selected difficulty's `GeneralNPC CorpseSmartLoot` `Medicine + Ammo` package. Writer creates `TCD_NPC_Ammo_Smart` referenced clone when this NPC source differs from reference and is activated.
2. `DestructibleStash_AmmoSNG/NATO` / `GameData/ItemGeneratorPrototypes.cfg`: eight item `MinCount/MaxCount` rows with independent Weight=2 originals, used by physical breakable ammo boxes. `Ammo Yield` and four `Ammo Family Weights` remain their own six-control editor; UI-colored pool legends are not NPC sample indicators.
3. NPC weapon-attached 121 standard NPC weapon rows / `ItemGeneratorPrototypes.cfg`: **old ammo scalar route retired**, only weapon condition still applies to weapon rows. No old 242 ammo editpoints in the writer baseline.

Evidence: upload-normalized NPC source inventory checksum `0x61073f67` matches `SMARTLOOT-CORPSE-BONUS-ENTRY-CATALOG.md` on all 43 rank/group/item/SID/min/max rows. The DLC1 dump has no parallel `StashPrototypes.cfg`; it reuses base SNG/NATO breakable generator references through DLC1 SpawnActor. All runtime behavior and Windows/Wine build remain unverified.


### 2026-10-09 — NPC Ammo Preview: 3 mini-card lock

Current `NpcLootLoadout` desired height is **463 DIP**, matching `AggregateVisualControl.GetDesiredHeight`. The preview is **read-only**; do not add `HitTarget` records, hover handles, focusable controls, fake select zones or a second ammo editor.

The `NPC AMMO PREVIEW` outer rect is local **Y=238 / H=104**, with heading at Y+7 and muted **NEWBIE SOURCE** at top right. At Y+29 draw 3 noninteractive tile rects (H=52; horizontal inset 11, gaps 8). Tile top line: neutral caliber (`12 GA`, `5.45`, `.45 ACP`), 10.0 DIP. Tile bottom line: a centered `MIN–MAX` numeric string in the original integer-half-up scaled source range, **13.0 DIP** when `ammo.IsValueModifiedFromReference()==false`, **14.2 DIP gold** when true. The **complete count range including the en dash** changes color and size together, and caliber label digits are never gold. Do not bind the highlight to group-level `IsModified`.

When the Extra Corpse Loot choice lacks `NPC_Ammo_Smart`, retain three subdued cards and show the muted line **Inactive until Medicine + Ammo is selected** at Y+86; this does not remove or reset the NPC Ammo multiplier. Count values remain gold if this source is independently modified, even while the inactive hint is visible. Following the increased preview, the interactive Dropped Weapon Condition range moves to Y=352 (H=97) and must retain its native physical click map and keyboard support. **No change** to source CSVs, item counts, selected difficulty isolation or Breakable Box Ammo mapping. Local Wine proof pending.

### 2026-10-09 — NPC condition dual-ended slider: central readout lock

This is the latest `NpcLootLoadout` rendering contract and **supersedes** the 463-DIP NPC layout's lower range details. The card is 130 DIP high at local Y=352; parent measured height **496 DIP**.

**READOUT:** keep the title on the left top line, and move both actual numeric values to **two fixed, centrally balanced positions** (centerX−73, centerX+73). Muted small `MIN`/`MAX` captions above their larger respective `N%` values; no overlapping digits or moving labels. Use per-endpoint changed-state values against authored baseline 0.25/0.50, giving neutral **13.0 DIP** at base or accent **14.2 DIP** for modified tokens, `%` included. The rest of the card is neutral; divider between centered readouts is subtle.

**TRACK & PREVIEW:** one fully horizontal selected MIN→MAX gold range at local card Y+81, with a faint grey **solid** authored 25–50% reference and tiny grey reference ticks. Remove the previous eye-catching dashed ghost. Two identical 6.5-DIP circular handles are centered directly on the gold rail. Only when their X values are less than 16 DIP apart, separate them vertically by ±7 DIP with minimal connector stems. No permanent displaced handles. Grey 0/100 endpoints remain beneath the rail.

**INTERACTION LOCK:** preserve the 38-DIP full-length shared clickable rail; nearest-X active endpoint selection, geometric 2D disambiguation near the handles, automatic native min/max ordering, keyboard step 1%, individual right-click reference reset and family-wide R reset. `OnMouseMove` holds visual hover on `_dragTarget` for range movements under active mouse capture, so the other endpoint never appears to hijack the drag. Halo follows actual rendered centers ±7 DIP only when overlapping. Reference marks and numeric readouts are read-only, so they cannot steal pointer events. The `NPC AMMO PREVIEW` mini-cards and both ammo/medical sources are unchanged.

### 2026-10-09 — AmmoDistribution physical breakable-box yield preview

**Latest locked visual:** `AggregateVisualControl / AmmoDistribution` is now **680 DIP** tall (previously 536) because native **AMMO YIELD** has a read-only per-caliber quantity preview underneath the yield scalers. The six independent editable Loot sources stay exactly the same. Yield and Grenade horizontal cards start at Y=66 (H=72). Add a bordered, read-only `AMMO YIELD PREVIEW` frame at Y=147 H=121. Heading on left; `ROUNDS IF ROLLED` on right, explicitly not guaranteeing any drop. Two miniature source-pool rows at Y=173 and Y=219, each with four centered fixed-label/quantity cards. At full width cap tile width to 143 DIP; at minimum supported control width dynamically fit all four with 7-DIP gaps and a left 42-DIP neutral SNG/NATO identifier. Rows are not editable and register **no hit target**.

SNG source item entries (actual `DestructibleStash_AmmoSNG`): `A918D 9×18 = 3–6`, `A919D 9×19 = 5–10`, `A545D 5.45 = 5–10`, `A012D 12 GA = 2–4`. NATO (`DestructibleStash_AmmoNATO`): `A045D .45 ACP = 5–10`, `A919D 9×19 = 5–10`, `A556D 5.56 = 5–10`, `A012D 12 GA = 5–10`. These are **eight distinct generator entries** read from the verified writer baseline, not six unique caliber-weight sliders or any `NPC_Ammo_Smart` values. `loot.destructibleAmmoQuantity` alone scales both source `MinCount/MaxCount` values using the existing `ScalePositiveInteger = max(1,floor(source*multiplier+0.5))`, native **0.5–2x**, **0.25x steps**, and no chance manipulation.

For each read-only sample tile, print neutral caliber name on the top line at 9.7 DIP. On its second line, center the formatted whole **MIN–MAX** count. Normal numeric font 11.6 DIP and neutral foreground; only when the **amount source itself** differs from selected Base, display the **whole numeric range including en dash** in gold at 12.8 DIP (+1.2). Never highlight the numeric substring in a caliber label. If that caliber's weight is currently zero, retain its range as an appropriately muted read-only sample, without implying it is currently eligible; an independently modified amount retains gold digits according to universal rule.

After the sample preview, weights heading Y=279; first and second weight-card rows Y=297/377; composition heading Y=459; unmodified live SNG/NATO share graphs at Y=481/575 with exactly the same permanent color-keyed percent legend semantics. NATO card bottom is 664 and parent height is 680. Right-click/R, native mouse/keyboard and preset writer paths are unchanged. The preview is derived *only* from the breakable box base SNG/NATO generator inventory, not world containers, named NPC corpses or all ammo-generating game systems. Runtime/Wine verification pending.


### 2026-10-10 — Nine NPC Ammo caliber previews (supersedes three-tile lock)

`NpcLootLoadout` is 564 DIP high. The read-only NPC Ammo preview is local Y=238/H=172 and uses a compact 3×3 grid (40-DIP-high tiles, 6-DIP gaps); the dropped-weapon condition range starts at Y=420. Each tile shows its caliber, earliest source rank and live Min–Max counts. Source tuples: Newbie 12 GA 2–6, 5.45 15–20, 9×19 15–20, .45 ACP 8–12; Experienced 5.56 15–20; Veteran 9×18 15–20, 7.62 sniper 25–30, 7.62 25–30; Master 9×39 15–20. These are illustrative source entries, not guaranteed drops and not an aggregate across ranks. Counts follow exactly `floor(source * multiplier + 0.5)`; only modified numeric ranges (including dash) grow and turn gold. Caliber/rank text stays neutral. Inactive `Medicine + Ammo` continues to dim the previews and show the concise activation hint. No CFG/writer/multiplier/preset change. Prior Newbie-only 3-mini-card geometry in this document is historical. Build/Wine and in-game acceptance pending.

### 2026-10-10 — Source-backed loose unique weapon shuffle (replaces removal control)

**Final approved behavior:** `SHUFFLE UNIQUE WEAPONS` is a **boolean toggle in WORLD PLACEMENT**, not a depletion slider. Parameter `loot.looseUniqueWeaponShuffle` (default OFF) is separate from ordinary `loot.worldWeaponShuffle`, weapon density, and `loot.looseWorldLootRemoved`. The World Loot Depletion panel remains the original **five** sliders; no unique-weapon percentage, removal path, or obsolete preset parameter is active.

**Compact UI contract:** World Placement aggregate has **six** settings (five independent toggles and one dependent weapon-density slider). Height 275 DIP. The left compact column contains regular weapon shuffle at local top=65/H44, its dependent density card at 122/H72, and unique weapon shuffle at 208/H44. Three other toggle pills remain on the right, 46 DIP high, 57 DIP apart. Reuse the established compact checkbox/toggle chrome, hover highlight, independent right-click reference reset and family R reset. No redundant Vanilla/Base labels or helper paragraphs inside the control.

**Original CFG evidence:** GameData `SpawnActorPrototypes.cfg`, exact `WorldMap_WP` actors with `SpawnOnStart=true`, `SpawnType=Item`, matching `ItemSID`/`SpawnedPrototypeSID`. Nine included ItemSIDs: `Gun_Tank_MG`, `Gun_S15_AR`, `GunArevPrecise_AR`, `Gun_SOFMOD_AR`, `Gun_GStreet_HG`, `Gun_Texas_SG`, `GunNightStalker_HG`, `Gun_Cavalier_SR`, `Gun_RatKiller_SMG`. Exact actor IDs and original presentation tuple are in `src/TrueCustomDifficulty/Data/loose-unique-weapon-shuffle-manifest.csv`, kept separate from regular weapon pools.

**Writer contract:** OFF emits no patch. ON uses a cryptographically seeded Sattolo cycle over the nine original weapon tuples, meaning **every item changes location and each original weapon exists exactly once**. Each original location keeps its actor SID, coordinates and rotation; only the `SpawnedPrototypeSID`, `ItemSID`, `MeshPath`, `Durability`, and `DisablePhysicsAndCollision` tuple is redirected together by exact per-actor `{bpatch}`. Nine output actor files, 45 exact assignment fields, **zero** `removenode` operations. Neither the ordinary weapon shuffle nor its density can edit any of these actor IDs. Quest/deferred pickup locations, trader rewards, NPC inventories and global item definitions are never targeted.

**Quest limits:** The nine actor/item SIDs were not directly referenced in the base or DLC1 `QuestNodePrototypes.cfg` searches; this does not certify every possible script/reward linkage. Do not expand the actor pool without separate source/quest audit. Production writer regression now asserts permutation conservation, no fixed points, original mesh/condition tuple fidelity and coexistence with other World Placement edits. Local Windows build, BPatch resolution and actual in-game placement/pickup remain **NOT RUN** until executed.

### 2026-10-10 — World Placement blank-panel regression fix

The `SHUFFLE UNIQUE WEAPONS` setting remains **aggregate-only** (`exposeInUi=false`), so `MainViewModel` MUST materialize it as a live `ParameterViewModel` before passing `AllParameters` to `AggregateVisualControl`. The World Placement family has exactly six live IDs: `loot.worldWeaponShuffle`, `loot.worldWeaponDensity`, `loot.looseUniqueWeaponShuffle`, `loot.worldAttachmentShuffle`, `loot.worldDetectors`, `loot.blueprintShuffle`. All six must be present even though only `loot.worldWeaponShuffle` is the **visible aggregate anchor**. Otherwise the entire panel incorrectly renders `World placement data unavailable`.

`MainViewModel` now explicitly includes all six IDs in its materialization filter, includes Unique Weapon Shuffle in the aggregate modified-state wiring, and keeps it in World Placement sort order. `tests/LootWorldPlacementContract` checks the actual `MainViewModel.AllParameters`, anchor/visibility mapping, and `AggregateVisualControl`'s six bound sources; this guards the exact user-observed failure. Existing World Placement compact height and checkbox design, all real CFG mappings, writer logic and original-world eligibility remain unchanged. Windows/.NET publish and Wine screenshot acceptance still require execution.

### 2026-10-10 — Global right-hand rationale and CONTROLS pass (authoritative)

**Source of truth:** `MainWindow.xaml`, `ParameterViewModel.RationaleDisplay` and `ParameterViewModel.ControlHelp`. This updates the presentation of every gameplay tab without changing any CFG/preset parameter, values, calculations, writer route, sliders, hit targets, or actual reset behavior.

**Required reading order from top to bottom:** selected control name; WHAT IT DOES (**gameplay explanation only**); conditionally WHAT THIS VALUE MEANS; HIGHER / LOWER; WORKS WITH; CURRENT RESULT; one **CONTROLS** section; then the existing muted `TabSummary` context at the very bottom. All conditional sections retain their previous behavior. The interaction instructions **must not** be appended inside `RationaleDisplay`, and the title `CONTROLS` is authored **exactly once** in XAML, never emitted by a view-model string.

**Player-friendly controls grammar:** keep CONTROLS compact and plainly actionable (what to click/drag, what the control changes, how to reset); avoid CFG identifiers, explanation repetition, two full help paragraphs, generic repeated Vanilla/Base blurbs, duplicated headings and fake UI interactions. Use concise specialized guidance for **all 33** current `VisualAggregateFamily` types, including when the selected item is a child inside its group; use equivalent semantic guidance for specialized editors (mutant health/drop/group, stamina costs/recovery, medical healing, anomalies, world loot, etc.). Generic slider/range/choice/toggle fallbacks apply to ordinary controls. The guide is normal foreground text, not a bright callout, with a small single accent heading and no extra large card. Reuse existing right-column scrolling and responsive layout.

**Muted bottom text:** keep the short `TabSummary` only. Remove the old `SliderGuide` from the visible footer; it repeated instructions already covered by CONTROLS and conflicted with individual semantic controls. Do not put it back as a redundant long paragraph.

**Regression:** `python3 tools/audit_rationale_ui.py` asserts one heading, ordering ahead of the grey footer, all 33 family guides, generic fallbacks, preserved conditional meaning/results, and absence of duplicated old control text. It is static source verification only; local .NET publish, Wine visual layout and interaction remain unverified until run.

### 2026-10-10 — Rationale CONTROLS compile syntax correction

Fixed a C# switch-expression punctuation typo in `ParameterViewModel.ControlHelp`: the `RadiationTreatment` arm ended in a semicolon rather than the required comma, causing `CS1003` at `ParameterViewModel.cs(66,134)`. The player-facing wording, right-panel layout and writer mappings are unchanged. `tools/audit_rationale_ui.py` now verifies that every named switch-expression instruction arm ends with a comma so this error cannot pass the source audit again. **Static arm review passes; an actual .NET 10 Windows publish and Wine UI acceptance remain to be run locally.**

### 2026-10-10 — Starvation OFF must disable its visual-anchor slider

**Bug observed in Wine UI:** Under MASTER → STARVATION PENALTIES, the EXTRA STAMINA COST slider stayed gold and interactive even with STARVATION MECHANICS OFF. Unlike the four other child sliders, the stamina-cost parameter (`survival.starvationStaminaDrain`) is also the visible `StarvationTimeline` **visual aggregate anchor**. The earlier `ParameterViewModel.IsControlEnabled` expression bypassed every dependency check for aggregate anchors, including the anchor's own `requiresToggle: advanced.starvationMechanics`.

**Correct contract:** `IsControlEnabled` always honors the real `requiresToggle` provider; being a visual anchor grants visibility, **never edit permission**. `IsControlVisible` separately preserves the Starvation composite row while the parent is OFF. OFF leaves **all five child values visible, grey and non-interactive** (no gold accent, mouse drag, keyboard edit, or child CFG output). Switching ON reactivates all five using their preserved settings. The independent parent toggle remains clickable. This is also the correct pattern for future dependency-gated visual anchors.

**Implementation:** `ParameterViewModel.IsControlEnabled` no longer ORs `IsVisualAggregateAnchor`. Existing `DrawMasterNativeCard` already uses `IsControlEnabled` to draw the active accent and handles, and `AddHorizontalTarget` rejects disabled hit areas. Added safety guards for stale keyboard selection, mouse hit targets and captured drags that could survive a parent toggle transition. No gameplay number, difficulty baseline, source CFG target, writer algorithm, control geometry or formatting has changed.

**Regression:** `tests/SurvivalPatchContract` now includes the Starvation anchor in its Veteran-disabled and Master-OFF/re-enable checks; the entire five-child dependency set must disable/re-enable together, keep its anchor row visible, preserve modified values and produce no child writer edit while OFF. The production suite adds two scenarios (from 85 to 87 based on the prior inventory). Static audit: `python3 tools/audit_master_dependency_ui.py`; both this source gate and actual Windows .NET suite should be run before release. The source inspection alone is **not** a successful WPF build or Wine/in-game visual acceptance.

### 2026-10-10 — Unique Weapon Shuffle CFG writer baseline / audit reconciliation

The dedicated `loot.looseUniqueWeaponShuffle` control was already implemented in the World Placement UI and in `CfgPatchWriterService.WriteLooseUniqueWeaponShuffle`, but the central `cfg-writer-baseline.json` still declared **244** supported parameters and lacked its manifest-backed production route. This made the actual C# `ValidateBaseline` fail before export, and the Python audits report the parameter as unsupported. **Do not remove, defer or fake a single-field CFG edit for this control.**

**Correct current inventory:** **245** catalog entries, **152 visible aggregate anchors/standalone controls**, **93 hidden backing IDs**, **232 effective editable source dimensions**, **13 intentionally non-UI source IDs**, **80 hidden-but-editable backings**. The source-backed direct/special writer route union covers **245/245**, including the new nine-actor shuffle.

**Canonical production route:** `cfg-writer-baseline.json.special.looseUniqueWeaponShuffle` contains the real `parameterId`, embedded `manifestResource` and `expectedActorCount=9`. `CfgWriterBaseline` deserializes this exact route and `CfgPatchWriterService` validates it, includes it in supported count and uses its embedded manifest in the actual writer. The writer remains a real per-actor `{bpatch}` derangement of nine physical world pickups (five item/presentation fields per actor, no `removenode`), separate from regular World Weapon Shuffle.

**Regression gates:** `tools/audit_ui_cfg_contract.py` and `tools/audit_project_consistency.py` now agree on the new totals and discover the new special route. The project audit additionally verifies route identity, manifest resource, exactly nine distinct actor/item pairs and linkage to the real C# writer. `tests/LootWorldPlacementContract` checks that the loaded production baseline contains the precise route and matches the live catalog count; its existing **24 writer scenarios** already cover unique-only, DLC, mixed weapon and other placements. Run the two Python audits, then the Windows `.NET` writer tests and Wine application. Source/structure verification alone does not prove local publish or UE runtime acceptance.


### 2026-10-11 — Audit reconciliation: native UI geometry and consumable sources

- Mutant Health remains a 14-mutant, two-column, seven-row layout at 605 DIP; the seventh row ends at 573 DIP, so a 526-DIP assertion would clip live controls. Preserve the shared 0–7500 HP scale and current/reference sources.
- The shared WPF rotary mouse-wheel path supports `PlayerWeaponProfile`, `AnomalyGlobalDamage`, `AmmoDistribution` and the newly introduced `ConsumableMixer`; do not remove a family while updating another.
- Medical Items binds four HEAL, four BLEED and four RAD bars (`_medical`, `_bleeding`, `_radiation`) plus Food Healing. Keyboard reset must traverse all three groups.
- Writer baseline currently contains 42 Health and 86 *exposed Survival* edit points in `cfg-writer-baseline.json` (the audit's exposed-parameter filter, not all Survival points). Future CFG additions must update the baseline and audit expectations together after verifying native prototype/path ownership; do not invent edit points to satisfy historical counts.
- Energy Drink source facts: `EnergeticStaminaPerAction1` −30% for 15 s; `EnergeticStaminaPerAction2` −100% for 5 s; `Energetic_Limited`, `MeleeButstock`, and **Engine stacking** remain explicit source audit facts. These short effects stay independently editable from the main Energy regeneration duration.
