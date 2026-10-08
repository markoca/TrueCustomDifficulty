# Mixer & Responsive Grid UI — Source of Truth

**Project:** True Custom Difficulty  
**Status:** Anomaly Effects, Service Prices, Trader Stock, Item Weight, Breakable Box Yields and Stash Reward Rolls migrated in source; runtime QA pending for new families.  
**Scope:** Visual layout and interaction only.  
**Canonical document:** This file is the source of truth for the mixer-style parameter controls and the accompanying responsive grid layout.

This specification exists because the current full-width horizontal-track boards consume too much height and repeat slider affordances that are difficult to scan at a glance. The application window uses **16:9**; the editing area must take advantage of that shape without making numeric labels smaller or widening individual tracks indefinitely.

> **Precedence:** For mixer/grid appearance, behavior, breakpoints, and acceptance criteria, follow this file. For individual parameter IDs, current range/step, vanilla difficulty reference, constraints and CFG effects, the live parameter catalog, ParameterViewModel, central constraint coordinator and CFG writer remain authoritative. The existing [Visual Control System](VISUAL-CONTROL-SYSTEM.md) governs non-mixer visual families. Do not silently change this specification while implementing a panel: update the source-of-truth document first.

## 1. Goals and non-goals

**Goals**

1. Replace appropriate groups of repetitive horizontal sliders with compact, visually consistent, **vertical mixer faders**.
2. Fit more useful settings above the fold of a normal 16:9 window while retaining readable values, labels and source-accurate editing.
3. Reflow mixer strips and independent boards into logical columns based on **actual editor width** (not full monitor width).
4. Retain visible **Current vs Base Preset** values and reference markers. Base Preset is the selected *vanilla difficulty*, not an assumed universal 1x/100%.
5. Retain the existing right-hand rationale/context panel, with it following the parameter actually edited.
6. Preserve exact live value, source range, step, constraints, reset semantics, preset persistence and CFG generation.

**Non-goals**

- No gameplay formulas, inferred effective damage/healing probabilities, rebalance, CFG writer changes or new preset fields.
- Do not turn independently editable values into a combined synthetic multiplier.
- Do not make all settings use a universal 0–100% range merely because the UI looks like a mixer.
- Do not replace already-meaningful radars, coordinate plots, timelines, range editors, discrete choices or toggle-led dashboards just for visual uniformity.
- Do not shrink text or interaction targets until everything fits; **wrap** the layout instead.
- Do not change the application window aspect ratio again.

## 2. Window and page composition

- Window default: **1600 × 900**, minimum: **1024 × 576** (both 16:9). The current window implementation fits to the work area, preserves ratio during manual resizing and aspect-maximization.
- Keep the header, tabs, footer and right rationale column. The current rationale column is approximately **320 WPF device-independent pixels (DIPs)**; do not hide it merely to increase mixer strip count.
- The left editor is the responsive viewport; responsive decisions use **measured available content width after tab chrome, margins and rationale are deducted**.
- Retain a maximum content width of approximately **920 DIPs** for large control boards, centered within the editor. A narrow board should not stretch a fader lane to fill a 4K monitor.
- Panel-to-panel spacing: target **12–16 DIPs** vertical; board header/body spacing: **10–14 DIPs**. Use the current application dark background, restrained yellow accent and grey reference palette.
- The editor remains independently vertically scrollable. Do not introduce a nested vertical scroll area inside a normal mixer board.
- At minimum window size, all active tracks, label/value rows and the rationale must remain reachable; horizontal page scrolling is not allowed.

### Conceptual wide-screen arrangement (not pixel-accurate)

~~~text
1600 x 900 — left editor (right rationale remains visible)

  ANOMALY EFFECTS                         SELECTED PARAMETER → RIGHT RATIONALE
  ┌────────────────────────────────────────────────────────────────────────┐
  │  DAMAGE    FC MOVE    FC TURN    RAZOR     DIAMOND     PSY DRAIN       │
  │  1.45x     0.50x      0.30x      1.00x     10 pts      1.00x           │
  │  base 1x   base .5x   base .3x   base 1x   base 10     base 1x          │
  │     │         │          │         │          │           │             │
  │     ║         ║          ║         ║          ║           ║             │
  │     ║         ║          ║         ║          ║           ║             │
  │     ━         ━          ━         ━          ━           ━  ← handle   │
  │     │         │          │         │          │           │             │
  │   0–2.5     0–1        0–1       0–2       0–20       0–2            │
  └────────────────────────────────────────────────────────────────────────┘

At narrower editor widths, strips reflow into 4+2, 3+3 or 2+2+2.
The track and labels retain their width; the board grows in height.
~~~

## 3. The reusable MixerStrip contract

One strip edits **exactly one existing ParameterViewModel numeric slider**.

### 3.1 Visual anatomy (top to bottom)

1. **Parameter label**: short, human-readable, unambiguous name. Up to two lines; ellipsis only if necessary. Full catalog label and explanation remain available through hover help/right rationale.
2. **Primary value**: prominent live value in the parameter's appropriate units (x, %, raw points, kg, seconds, etc.).
3. **Reference line**: compact “BASE {value}”, explicitly the currently selected Base Preset difficulty.
4. **Vertical fader track**: 160–184 DIPs nominal editing length, gold handle/current fill, grey reference tick, and a subtle guide rail.
5. **Track endpoints**: show true native minimum and maximum for that specific parameter; the baseline is not automatically the center.
6. **Optional contextual hint**: only where directional semantics are non-obvious, e.g. “higher = less slowdown.” Do not place lengthy rationale text on every strip.

**Suggested dimensions** (WPF DIPs, tune only after visual testing):

| Element | Target | Minimum |
| --- | ---: | ---: |
| Strip width | 132–148 | 128 |
| Between-strip gap | 10–12 | 8 |
| Track visible length | 172 | 150 |
| Track visible width | 5–7 | 4 |
| Handle diameter / grip width | 16–20 | 16 |
| Effective interactive handle/rail hit width | 32 | 28 |
| Total single-row board height (header + faders + footer) | 308–340 | no fixed shrink |
| Board internal horizontal padding | 16 | 12 |
| Main board heading font size | 12–13 | 12 |
| Channel label font size | 10.5–11.5 | 10.5 |
| Current value font size | 13–14 | 12 |
| Base value / min-max font size | 10–11 | 10 |

The stated board height is an **initial design target**, not a clipping limit. Measure the real rendered text and content. Multiple wrapped rows grow board height. Under no circumstances overlap label, value, markers or neighboring strips.

### 3.2 Visual states

| State | Required display |
| --- | --- |
| Vanilla/unmodified | Current handle sits at the grey Base Preset tick; no special modified accent outside existing UI conventions |
| Modified | Gold handle at Current, stable grey tick at Base; clear per-strip modified indication, consistent with existing left-edge/yellow language |
| Selected / keyboard focus | Visible gold focus outline on exactly one strip (persist after pointer leaves) |
| Hover | Subtle track/strip highlight, precise source/units in tooltip; hover alone does not change the stored parameter |
| Disabled by dependency | Muted and non-editable; display remains readable, stored value retained, no false reset |
| Base outside visible span | This must not occur for valid catalog bounds; treat it as an audit failure, not as a reason to invent normalization |
| Missing catalog member | Explicit unavailable state; do not render an active dummy fader |

No decorative meters should suggest that a multiplier is measured damage-per-second, percentage chance or a final gameplay outcome.

## 4. Scale, units and data binding

- Every strip binds directly to an existing numeric ParameterViewModel. One live control value = one source parameter ID.
- Map raw values to vertical position with a **linear native-source scale**: bottom = Minimum, top = Maximum; normalized position is (CurrentNumber − Minimum) / (Maximum − Minimum). The visual normalization is for **position only**, never saved as the gameplay setting.
- Reversed *difficulty implications* do **not** reverse the numerical axis. Example: a larger Flycatcher movement-retention coefficient means faster remaining movement and therefore a weaker slowdown. Up still means a larger raw value; explanation handles the gameplay meaning.
- Use the catalog's actual **step**, including absolute step-mode values and fractional values (0.01, 0.25, 0.5 etc.), for dragging, keyboard and snapping. Preserve exact native values and stable floating-point quantization; do not round the stored number to the formatted display.
- Endpoint, live and Base values use a **single source-aware formatter**; “70%” for a 0.70 service coefficient, “10 points” for Diamond bleeding, “1.45x” for the anomaly difficulty coefficient. Do not show a percent sign for a raw effect merely because it has a 0–1 range.
- The Base Preset reference is obtained from the selected difficulty via the existing reference model and refreshes immediately when the selected vanilla baseline changes.
- When several channels happen to use the same native scale they may share understated guide markings, but this never overrides their native source ranges or labels.
- Base Preset/Vanilla markers are **non-destructive snap targets**: clicking an explicit marker selects the corresponding raw reference value, routed through existing constraints. Marker hit areas must not compete with the live handle's drag hit area.
- Read-only comparison visuals must have **no editing hit targets** and must not trap mouse clicks intended for the actual fader.

## 5. Input and reset contract

| Input | Required behavior |
| --- | --- |
| Left-click on track | Select this strip and set only its source parameter to the corresponding snapped native value |
| Drag handle/track vertically | Continuous preview, snapped to the original step, clamped to source min/max; no unnecessary change to neighboring values |
| Left-click Base Preset marker | Snap that same parameter to the current Base Preset reference |
| Right-click strip handle, active rail or marker | Reset **only** that parameter to its Base Preset value |
| Arrow Up / Right | Increment selected parameter by one native tick |
| Arrow Down / Left | Decrement selected parameter by one native tick |
| Page Up / Page Down | Move by ten native ticks (clamped) |
| Home / End | Jump to native min / max |
| R when board has focus | Reset **all mapped members of that visual family**, nothing outside the family |
| Mouse wheel over mixer | Follow the existing aggregate convention: **scroll the surrounding editor**, do not silently change a gameplay value |
| Mouse wheel over ordinary value box outside mixer | Keep its existing local behavior; the two contexts are intentionally different |
| Keyboard focus | Tab into/out of board; selected channel must be visually clear. Do not steal ordinary text-field shortcuts |

- Retain the existing SliderConstraintCoordinator as sole authority for constraints and cascading. The active fader has priority; any linked change is minimized and visibly reflected by the affected strips.
- Right-click must never also perform a left-click edit. Hovering a label must never accidentally set an unrelated strip to max.
- On a focused channel, left/right/up/down remain **value adjustments**, not hidden channel-navigation gestures. A separate explicit keyboard channel-selection mechanism may be added only if it does not conflict with those bindings; document and test it first.
- No background scrollbar or page drag may edit a parameter.
- Undo/redo behavior is unchanged (none is promised here).

## 6. Selection and rationale behavior

- Clicking or dragging a fader activates **its own backing parameter** in the existing right rationale panel, even though the complete board is represented by a single aggregate anchor.
- This is essential: do not leave the rationale describing only the first/anchor member while editing a different strip.
- The rationale displays that parameter's WHAT IT DOES, relevant interaction/side effects and precise unit meaning. Shared mixer controls help may appear in a short CONTROLS block.
- Keep visual selection in sync when switching tab, preset, Base Preset or group. Hover is not selection; a tooltip may preview details without changing the current rationale.
- If selecting a channel requires a new aggregate-to-MainViewModel notification, implement that explicitly rather than inferring selection from the anchor's DataContext.
- Avoid status messages that obscure values; transient reset notices should clear after at most **3 seconds**.

## 7. Responsive mixer arrangement

Column thresholds below apply to **board inner width**, not the whole Window width:

| Available board inner width | Faders per row |
| --- | ---: |
| 852 DIPs or more | 6 |
| 596–851 DIPs | 4 |
| 420–595 DIPs | 3 |
| 272–419 DIPs | 2 |
| Less than 272 DIPs | 1 |

- The actual implementation must also enforce **strip width ≥ 128 DIPs**, minimum gap and padding. If a threshold and true measured width disagree due to fonts/DPI, use fewer columns.
- Prefer rows of 6/4/3/2 over artificially narrow strips. Remaining strips start a **new row aligned from the left**; do not stretch two remaining strips to match four.
- Row height is determined by the tallest label/legend in that row; each track must retain the same vertical alignment within its row.
- Preserve authored semantic order on wrap: global effect first, then related local effects. No dynamic reorder based on current numeric values.
- Board header and a short source-accurate guidance/footer line can span all columns. Do not repeat a long “Current/Base Preset” explanation in each strip.
- Avoid mini-horizontal sliders inside each mixer strip. A mixer strip must read as a **vertical control** even in the narrow layout.
- Recompute measure and arrange from the same breakpoints and actual row heights to avoid clipped bottom content or stale hit rectangles when resizing.

### Tab-level grid composition (independent boards)

- Introduce an **opt-in semantic board grid**, separate from the strip grid.
- At sufficiently wide left-editor widths (initial target **≥ 900 DIPs usable**), two **compact, independent** boards may share a row; aim for at least **420 DIPs per board** including internal padding.
- At narrower widths, or if a board declares a wide/minimum layout requirement, stack it full width. Never put a large radar, complex timeline or six-channel mixer into an unusably narrow two-column slot.
- Recommended side-by-side candidate within **Economy**: **Trader Stock + Service Prices**, provided their boards remain comfortably readable. Do not pair Trader Stock (Economy) with Item Weight (Equipment): they belong to different tabs. Independently group smaller boards within each tab.
- Tab grouping, natural reading order, section headings and right rationale panel stay intact.
- Reflow must not change source parameter order, family membership or which settings the Reset Tab button affects.

## 8. Approved migration matrix

| Visual family | Treatment | Reason / constraints |
| --- | --- | --- |
| **AnomalyEffectsBoard** | **First mixer prototype (6 strips)** | Six independent source sliders, current tall stacked board; exercises mixed raw units and selected-difficulty vanilla references |
| **ServiceCostsBoard** | **Implemented in source (3 strips); runtime QA pending** | Different price domains; keep base repair price separate from reputation repair curve |
| **TraderStockBoard** | **Implemented in source (2 strips); runtime QA pending** | Equal numeric ranges, independent inventories; preserve 0.25x absolute steps |
| **ItemWeightBoard** | **Implemented in source (2 strips); runtime QA pending** | Equal ranges; item weight remains separate from carry limit |
| **BreakableBoxYields** | **Implemented in source (2 strips); runtime QA pending** | Distinct absolute step sizes; no claim that box quantity changes box occurrence |
| **StashRewardProfile** | **Implemented in source (3 strips); runtime QA pending** | Chance vs quantity vs chance; preserve different native units and chance semantics |
| **CoreHealthDashboard** | **Keep hybrid initially; evaluate later** | Max HP, delay, passive regen, effect scale and low-health regen are semantically distinct; meaningful widgets may remain preferable |
| **HumanCombatProfile, EquipmentWearBalance, PsyPhantomPressure** | Retain existing 2D/semantic visuals | Spatial relationship is informative; do not replace with faders merely for visual uniformity |
| **RepairCurve, RadiationBalance, StarvationTimeline** | Retain semantic graphics | Actual reputation, exposure/recovery or timing relationships are the purpose of these boards |
| **NPC loot condition, choice, toggle-led boards** | Retain existing special editors | Paired MIN/MAX, discrete choice and on/off dependencies must remain obvious |
| **Existing radar families** | Keep radars | Axes compare coherent parameter families; screen space managed at outer-grid level |

These are **migration priorities**, not claims that the code is already converted.

## 9. First implementation pilot: Anomaly Effects

Family: **AnomalyEffectsBoard** (Anomalies tab → Anomalies section). Six strips in this fixed source order:

| Strip label | Source parameter ID | Native min → max | Native step | Base reference |
| --- | --- | ---: | ---: | --- |
| ANOMALY DAMAGE | environment.anomalyDamage | 0–2.50x | 0.01 | Novice 0.30x; Stalker 1.00x; Veteran 1.45x; Master 1.75x |
| FLYCATCHER MOVE | environment.flycatcherMovementSpeed | 0–1.00x remaining movement | 0.01 | 0.50x, all difficulties |
| FLYCATCHER TURN | environment.flycatcherTurnSpeed | 0–1.00x remaining turn speed | 0.01 | 0.30x, all difficulties |
| RAZOR BLEED | environment.razorBleeding | 0–2.00x | 0.01 | 1.00x, all difficulties |
| DIAMOND BLEED | environment.diamondBleeding | 0–20 raw bleeding points | 0.5 absolute | 10 points, all difficulties |
| PSY DRAIN | environment.psyAnomalyIntensity | 0–2.00x | 0.01 | 1.00x, all difficulties |

**Required pilot checks**

- The top/global anomaly setting is **not** computed by multiplying local Razor/Diamond/Psy or Flycatcher effects. Six independently editable source values, six independently stored values.
- Flycatcher faders must state **speed remaining**, not imply that larger source values always mean stronger slow.
- Diamond shows **raw bleeding points**, not 0–2000% or “10x.” Its native 0.5-point step is visible in editing behavior.
- Base reference for global anomaly damage changes as the user selects Novice, Stalker, Veteran or Master.
- In a wide editor, the six channels form one mixer row; narrow editor widths wrap 4+2, 3+3 or 2+2+2, maintaining alignment.
- Clicking the fifth fader must update right rationale to **Diamond Bleeding**, not leave it at the Anomaly Damage aggregate anchor.
- Reset one channel (right-click), then reset all six (R); verify no unrelated anomaly/radar parameters change.
- Dragging does not make a hidden original horizontal slider appear underneath the mixer.
- Test manual resize, aspect-maximize/restore and interaction after relayout.

## 10. Technical integration boundaries

- Reuse the live ParameterViewModel mapping and aggregate family anchor model; do **not** add a new parameter catalog or parallel stored fader values.
- A reusable **MixerStrip renderer/control** should be parameter-driven. A **MixerBoard host** owns family header, wrapping, selection and group reset. The first implementation may integrate this as shared helpers in AggregateVisualControl if extracting new WPF types would duplicate state or interaction logic.
- The existing MainWindow parameter-card template must show **one visible board per family**, without also showing the native per-member slider controls.
- Existing visual-family registrations, modified-state wiring, IsVisualAggregateAnchor hiding, GetFamilyIds, draw dispatch and desired/arranged height contracts must remain consistent.
- Reuse source-aware labels/formatters, selected/Base reference values, hit-target registration, native-step snapping and current keyboard shortcuts. A new vertical hit target uses top-to-max and bottom-to-min.
- The generic semantic layout system may place independent **boards** side-by-side where safe, but must not alter the original catalog ordering or silently merge unrelated control families.
- **No CFG writer, saved preset JSON, default reference or runtime gameplay change is allowed** in the mixer/grid passes.
- If an implementation proposes a behavior not specified here, update this document as part of the same reviewed design change **before** implementing that behavior.

### Implementation notes for the first pilot

- The current implementation uses a **shared parameter-driven DrawMixerStrip renderer** inside AggregateVisualControl, rather than six duplicated custom WPF elements.
- The AnomalyEffectsBoard renders exactly six columns at a wide 920-DIP board and reflows from the same source-defined column threshold and row count used during measuring.
- The selected parameter is reported through AggregateVisualControl.ParameterSelected to MainWindow, which selects the actual source ParameterViewModel for the rationale panel. This fixes the old anchor-only rationale behavior.
- A distinct grey Base-marker hit target is positioned beside (not directly over) the live fader rail.
- The original source sliders remain hidden as aggregate members; CFG writer and preset serialization remain untouched.
- **Static safeguard:** `python3 tools/verify_mixer_layout.py` (also run by `publish.sh`) checks six IDs, native ranges, references, layout widths, XAML binding and per-member selection.
- **Not yet accepted:** WPF compilation, Wine runtime interaction, clipping on narrow screens, and screenshots at both target sizes. Checkboxes below remain unchecked until those are actually tested.

### Second-wave implementation

- Service Costs, Trader Stock and Item Weight now use the same source-bound `DrawMixerStrip` and responsive row-height calculation as the Anomaly Effects pilot.
- Service Costs retains selected-difficulty Upgrade references and shows Base Repair Cost as a percentage of its raw pricing factor (e.g. 0.70 = 70%).
- Trader Stock retains separate 0.25x absolute steps; Item Weight retains independent 0.01 steps.
- Clicking a strip selects its individual backing parameter for the rationale pane; no CFG writer, serialisation, parameter IDs or base values changed.
- Full 16:9 runtime QA, keyboard/drag interaction and screenshots are still required before marking the second wave accepted.

## 11. Acceptance checklist (all required)

- [ ] All six pilot faders render at default 1600×900 and minimum 1024×576 without overlap, clipped labels or horizontal scrolling.
- [ ] At all breakpoints, board height matches wrapped rows and hit targets follow the visible fader positions.
- [ ] Font sizes stay at/above the stated minima; current/Base values and native min/max remain legible.
- [ ] Each fader edits only its correct backing ID unless the **existing** constraint coordinator explicitly requires dependent changes.
- [ ] Native min/max/step/stepMode and selected vanilla difficulty references are exact; no normalization leaks into stored values.
- [ ] Click, drag, Base-marker snap, right-click one reset, R family reset and all existing keyboard commands work.
- [ ] Mouse wheel scrolls the editor; it does not unintentionally change fader values.
- [ ] Focus, hover, disabled states, right rationale selection and modified indicators are correct.
- [ ] Existing semantic radars/timelines/ranges/toggles and all their interactions remain unchanged.
- [ ] Selection of Veteran vs Stalker updates Base markers while preserving current preset state according to the existing parameter model.
- [ ] All relevant static visual mapping/visibility checks pass; the WPF project **actually builds** under net10.0-windows / win-x64.
- [ ] Wine runtime is checked for text alignment, scaling, resizing, interaction, keyboard and persistent preset round-trip.
- [ ] Before calling the pilot complete, capture before/after screenshots at 1600×900 and 1024×576.

## 12. Phased execution and change discipline

1. **Specification (completed):** establish this canonical contract and cross-link it from the UI documentation.
2. **Reusable fader foundation (coded, awaiting runtime verification):** shared DrawMixerStrip and responsive wrap/height helpers within AggregateVisualControl, with new selection notification and Base-marker snap hit zones.
3. **Anomaly Effects pilot (coded, awaiting runtime verification):** six source-exact vertical strips, one/member reset and explicit rationale activation.
4. **Pilot verification (pending):** build + Wine visual pass; fix readability/hit targets/breakpoints before expanding.
5. **Additional eligible families:** Services, Trader Stock, Item Weight, then selectively Box Yields/Stash Rewards.
6. **Outer semantic grid:** arrange compatible small boards side-by-side within their existing tab and section, keep complex full-width boards full-width.
7. **Final polish:** typography consistency, whitespace, section rhythm, clipping/DPI audit, and documented acceptance pass.

**Repository synchronization rule:** Always build and test against the latest fetched remote main commit. A failed pull or diverging/dirty local checkout is a stop condition, not permission to build an older source tree. Never discard local changes implicitly; save/reconcile them before syncing. Every GitHub implementation update must include the latest commit SHA and a complete copy-pasteable local fetch/verify/publish/run sequence.
