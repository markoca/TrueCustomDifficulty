## STASHES — BASIC BREAKABLE-BOX STYLE / RGD-5 PAIR (CURRENT, 2026-10-17)

**Current UI implementation and catalog lock; supersedes older Stash Content Mix and Stash Rewards geometry/descriptions below.** Six unchanged global native `Weight` controls (AMMO yellow, MEDICAL green, FOOD orange, DRINKS blue, GRENADES red, COUPONS purple; 0–3× with original 0.1× steps) in BASIC now use the same compact **270° colored native source knobs** as BREAKABLE BOXES, rather than horizontal tracks. Grid is **3 columns × 2 rows** at width ≥670 DIP and **2 × 3** below; the six parameters still tune all eligible regular stash tiers, not only Basic. Source knob gestures, original-value mark, changed-number emphasis and original preset IDs remain unchanged. No 6-way global drop probability.

**Breakable Boxes-style native composition:** Beneath the knobs the Basic section renders exactly two separate, real source **Weight-only** groups from the embedded `GameData/ItemGeneratorPrototypes.cfg` baseline: BASIC group `[7]` (Easy/Medium + Experienced consumables) and BASIC group `[1]` (Hard consumables). Each individually normalized multicolor bar shows genuine relative Weight shares *within that group*, with exact category legend/percentages. No cross-rank, cross-difficulty, Chance entry, whole-stash or SmartLoot normalization. Every multiplier updates both bars using the existing `StashEffectiveState.TuneSelection` numerical path shared with the emitted CFG writer. The present two groups are examples, not comprehensive stash drop odds.

**Grenades paired:** Under those pool bars, the two compact red rails **RGD-5 CHANCE** (`loot.cheapStashGrenadeChance`: native 0–100% item roll) and **RGD-5 AMOUNT** (`loot.cheapStashGrenadeQuantity`: original integer 0–4 grenades) are side-by-side on both responsive widths. Each retains native snap precision, white clickable/hoverable endpoint labels, gold modified numeric digits, independent mouse/keyboard/reset and source semantics. `loot.cheapStashGrenadeQuantity` is moved from the **StashRewardProfile** visual aggregate to **StashContentMix**; `parameters.json` group is `Stash Content`, no ID, default, writer or preset change. **STASH REWARDS** now contains only its three Basic/Standard/Rare ammo-quantity rows and measures 225 DIP, not the former fourth grenade row.

**Standard and Rare** retain the exact audited native Chance-led *read-only* category tiles. Independent **RARE COUPON CHANCE** remains a source-native rail on Rare, with original 20%/10% native difficulty semantics; do not invent global Chance or change protected actor exclusions.

**Responsive coordinates (DIP) and screenshot contract:** Basic title y37, six native knobs in an outer panel y53 of height 300 (3×2) /454 (2×3), individual knob-card height 146, gap 8; two actual Basic composition bars in a 136-DIP panel y361/y515; RGD-5 paired 56-DIP cards at y505/y659; Standard card at y569/y723 (72/116 high), Rare card at y649/y847 (72/142 high), Rare Coupon rail y729/y997, 56 high. Parent measures 799 DIP at host width ≥700 and 1067 below; all controls are capped to the original 778-DIP content width, not stretched full-screen. `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md`, renderer, catalog, family mapping and static checks MUST match. Build/Wine/game runtime acceptance requires separate execution; do not claim it from static assertions.

## STASHES — BASIC WEIGHT / STANDARD & RARE CHANCE-LED (CURRENT AUTHORITATIVE UI, 2026-10-17)

This section **supersedes all earlier Stash Content Mix visual descriptions below**. The physical actor allowlists, original field semantics, native RNG eligibility, six category multipliers, two real Chance controls, medical/gear controls, all 26 persisted Stashes parameter IDs and source CFG writer rules are unchanged.

**Player-facing hierarchy:** Under **STASH CONTENT MIX**, display **BASIC · WEIGHT MIX** with six independent color-coded 0–3× source Weight rails for AMMO (gold), MEDICAL (green), FOOD (orange), DRINKS (blue), GRENADES (red), COUPONS (purple). All remain editable at 0.1× increments with clickable 0×/1×/3× markers, Stamina Usage number alignment, drag, wheel, keyboard, per-control right click and family R reset. Display exactly one short **WEIGHT APPLIES TO ALL TIERS** note: the Basic-first visualization is **not** a Basic-only backend control and must never advertise a Basic-specific multiplier or erase native Chance-only entries. Retain the compact audited item descriptions within each Weight lane, but remove the duplicate long footer.

**BASIC authored exception:** One narrow **BASIC RGD-5 CHANCE** rail below the Basic mixer controls only original `loot.cheapStashGrenadeChance` (0–100%, source 10%) in its eligible native grenade branch. **STANDARD** and **RARE** then display only compact, read-only, colored **native source field-coverage tiles**, not a second editable Weight mixer, fake percent bars or global Chance sliders. Tiles say **CHANCE ONLY**, **WEIGHT**, or **WEIGHT + CHANCE** (no invented drop odds). Source-audited coverage is Standard AMMO=Weight; MEDICAL, DRINKS, COUPONS=Chance only; FOOD/GRENADES absent. Rare AMMO=Weight+Chance; MEDICAL, FOOD, DRINKS, GRENADES, COUPONS=Chance only. A native Weight presence is still shown in the Chance-led tiers even if global Weight is 0×, because the authored field exists. The separate **RARE COUPON CHANCE** narrow rail is embedded in Rare and edits only original `loot.expensiveStashMoneyChance` (source Novice/Stalker 20%, Veteran/Master 10%). The two Chance controls retain 0%/100% clickable/hoverable landmarks and original source-relative writer semantics; they never promise that a physical stash will include a reward.

**Responsive locked geometry (DIP):** content width bounded to 778; top header BASIC at y37, mixer at y53 with 262 height for inner width ≥670 or 496 below, unchanged six-lane 3×2/6×1 pitch/marker positions. Basic Chance card: y321/y555, 56 high, max 360 wide. Standard source card: y385/y620, 72/116 high. Rare card: y465/y745, 140/216 high; Rare Chance card: y535/y890, 56 high, aligned right on both widths. Source tiles appear in four/six columns on wide tier cards and two on narrow cards; no horizontal scrolling/full-width controls. Total StashContentMix measured height is **619 DIP for host ≥700**, **975 otherwise**, leaving at least 14 DIP after the lowest panel. Stash Rewards remains a separate compact 310-DIP four-row quantity/amount editor, with no duplicates of Chance controls.

**Evidence boundary:** Tier readouts are the **41 audited native ItemGenerator groups**, **not** all world SmartLoot, physical stash discovery odds, quest/scripted/direct rewards or independent selection pools merged into one probability. Full physical stash containing quest/unique/ambiguous clue rewards stays out of scope. Fail closed if source tier count or exact category field mapping changes. The embedded `parameters.json`, VisualAggregate family ownership, renderer and three Python source audits must match this lock. Local .NET/Wine visual validation and in-game RNG testing remain pending until executed.

## STASHES GLOBAL WEIGHT WITH NATIVE CHANCE EXCEPTIONS (CURRENT) — 2026-10-17

**Current design/behavior:** Exactly six persistent Stash Content Mix category knobs (AMMO, MEDICAL, FOOD, DRINKS, GRENADES, COUPONS) now scale **only native Weight** across all supported eligible ItemGenerator, SmartLoot and guarded direct/DLC1 sources and ranks. The knobs use **0–3× / 0.10×**, with **1× original**, **0.1–0.9× reduced relative Weight**, **1.1–3× increased relative Weight**, and **0× zero Weight entries**. A native Weight is relative *within its source group*; where all rows are one category, multiplying all nonzero weights equally may not change normalized selection frequency. In Weight-only SmartLoot pools, the shared Min/MaxSpawnChance is **not** multiplied by a positive Weight factor; all-zero Weight groups still receive source-native zero-selection/count/spawn fallback guards. No number in this UI is a fabricated whole-stash drop percentage. Source-native rank/difficulty eligibility and protected quest/scripted actors remain unaffected.

**Independent Chance:** Global Weight controls do **not** multiply, clamp, disable, or turn into Weight any Chance-only rows (including MEDICAL when its global Weight is 0×). Existing *real source-exposed* `loot.cheapStashGrenadeChance` (RGD-5) and `loot.expensiveStashMoneyChance` (rare coupon rolls) remain **independent, editable controls in Stash Rewards**; do not manufacture additional global Chance parameters or duplicate persistence/controls. Existing Medical Composition/medical quantity controls retain their genuinely item-specific native Chance and exclusion behavior. Any future relocation of those two native chance controls underneath their category knobs is a **separate UI decision**, not implicit authorisation to add new probability knobs.

**Player-facing Stash Content Mix:** six colored compact knobs on one row; beneath them the always-visible noninteractive **STASH ITEM WEIGHTS** panel continues to show audited item-kind descriptions, but now uses **ORIGINAL / REDUCED / INCREASED / WEIGHT OFF** status, with MEDICAL **PARTIAL / ADJUSTED / EXCLUDED** only for genuine subtype-specific medical overrides. `WEIGHT OFF` explicitly states **independent Chance may still select an item**; do not falsely claim 0× Weight universally excludes the category. Remove native source entry counts / ItemGen / World / W/C debug counters from the game UI. Layout bounds remain 778 DIP wide, y237 panel, 290 DIP two-column or 500 DIP single-column, measured 545/755 DIP. Catalog + implementation + writer + preview/parity tests must agree. Retain the original 26 Stashes parameter IDs and factory preset compatibility; there is no five-phase selection system in the game UI. Older 0–1× global Weight/Chance hybrid and `WHAT CAN APPEAR`/EXCLUDED-for-everything descriptions below are **historical**, superseded here. .NET build/Wine and in-game RNG remain unverified until executed.

## STASHES PLAYER-FACING LOOT STATUS (CURRENT) — 2026-10-17

**The six global item-type knobs remain the only Stash Content Mix edits.** Knob values represent a multiplier applied to the original native *selection* field, NOT an overall chance to find that item in a stash. `1×` = ORIGINAL source selection, `0.1–0.9×` = REDUCED native selection (not a proportional real-world drop-frequency claim), `0×` = EXCLUDED from the mod-controlled eligible generators. The original selection can be a relative Weight or independent Chance, with no fabricated single global probability. Do not claim a positive multiplier guarantees appearance or that `0×` removes items from quest/scripted/protected sources.

**What the player sees:** each original one-row colored knob gains a short neutral `ORIGINAL`, `REDUCED`, `EXCLUDED` (or MEDICAL `PARTIAL`/`ADJUSTED`) status; beneath is one bounded always-visible read-only **WHAT CAN APPEAR** panel with six item-kind descriptions and dynamically recomputed eligibility/status. Descriptions are source-backed: AMMO supported ammunition, MEDICAL bandages/medkits/army medkits/anti-rad, FOOD bread/sausage/canned food, DRINKS water/energy drink/beer/milk/vodka, GRENADES RGD-5/F1, COUPONS native coupon bundles. MEDICAL explicitly lists affected subtypes as `EXCLUDED: ...` when medical composition or MAX=0-items settings disable them; its global knob remains independent, with no new persisted IDs. Status copy states `CAN APPEAR` or `STILL POSSIBLE` only as eligibility, never as promised physical loot. Footer explains possible is not guaranteed and `OFF` applies only to mod-controlled stashes.

**Retire all developer/source counters from player UI:** `GLOBAL SOURCE COVERAGE`, `ITEMGEN`, `WORLD`, `WEIGHT`, `CHANCE`, entry counts and W/C abbreviations; keep original 463-group/1,124-row inventories only for source validation and executable CFG parity. Panel shows two columns of three source-aligned categories when outer width ≥670 DIP (panel height 290 DIP; family measured 545 DIP for width ≥700), otherwise one column of six (panel height 500 DIP; family measured 755 DIP). Starts y237 and remains ≤778 DIP wide, with small category swatches, compact neutral typography, no extra editing dimension, no extra rank/branch/drop-rate controls. All original 26 Stashes IDs, 0–1× / 0.10× global snapping, native eligibility/quest safety, Weight/Chance writer transforms and catalog/UI synchronization are preserved. Runtime/Wine and game acceptance are not claimed. Older global-source-coverage proposals below are historical, **not current UI**.

## STASHES GLOBAL ITEM-CATEGORY SOURCE COVERAGE (CURRENT) — 2026-10-17

**One global knob per item type, never per generator branch or rank.** The six persisted Stash Content Mix controls (AMMO, MEDICAL, FOOD, DRINKS, GRENADES, COUPONS) each apply a single identical 0–1× multiplier in 0.10× increments to *all eligible matching source item SIDs*, irrespective of Basic/Standard/Rare ItemGenerator tier, SmartLoot prototype, native branch, player rank or difficulty. Supported guarded direct/DLC1 stash paths share that category mapping. Existing native rank/difficulty eligibility gates remain, as do protected actor/quest constraints; the global knob does **not** create absent items or guarantee physical spawns. Original Weight and Chance fields keep distinct native semantics. Medical detail effects remain MEDICAL-only and there is no global combined loot probability.

**Visible compact UI:** six unboxed knobs on one row, then an always-visible noninteractive **GLOBAL SOURCE COVERAGE** panel, **not** a Native Branch browser. Six category readouts each show original authored Weight/Chance item-row counts and ItemGenerator/SmartLoot counts **across every native rank/group/prototype**, with no generator, branch, rank, difficulty, prototype, group or disclosure selectors. Counts are from the audited 463 original groups / 1,124 item rows; supported guarded direct/DLC1 overrides are **not included in these inventory counts**. No normalized six-way percentages or invented drop rates. Bounded 778-DIP width, summary starts at y237; total measured family height = **520 DIP for widths ≥700 DIP**, **665 DIP narrower**. Source keys and 26 persisted Stashes IDs remain unchanged. Color/size hierarchy, white neutral copy and gold changed *digits only* follow the locked visual system. The old Native Branch UI below in historical sections is retired from the player-facing screen; its source catalog is kept for backend CFG parity only. Real .NET/Wine/gameplay acceptance remains pending.

# Mixer & Responsive Grid UI — Source of Truth

**Project:** True Custom Difficulty  
**Status:** Anomalies now uses four semantic source-bound panels; its six-fader mixer is retired. Item Weight, Loot Breakable Boxes, Stash Rewards, Economy Service Prices and Trader Stock use compact native horizontal editors (2026-10-09). Earlier mixer examples for those families are historical only. Runtime/Wine QA pending.  
**Scope:** Visual layout and interaction only.  
**Canonical document:** This file is the source of truth for the mixer-style parameter controls and the accompanying responsive grid layout.

This specification exists because the current full-width horizontal-track boards consume too much height and repeat slider affordances that are difficult to scan at a glance. The application window uses **16:9**; the editing area must take advantage of that shape without making numeric labels smaller or widening individual tracks indefinitely.

> **Precedence:** For mixer/grid appearance, behavior, breakpoints, and acceptance criteria, follow this file. For individual parameter IDs, current range/step, vanilla difficulty reference, constraints and CFG effects, the live parameter catalog, ParameterViewModel, central constraint coordinator and CFG writer remain authoritative. The existing [Visual Control System](VISUAL-CONTROL-SYSTEM.md) governs non-mixer visual families. Do not silently change this specification while implementing a panel: update the source-of-truth document first.


## Latest Anomalies override — replaces six vertical mixer strips (2026-10-09)

The user approved the **four-section Anomalies layout**: Global Anomaly Damage (one compact native horizontal track), Individual Anomaly Damage (all eleven actual sources in a 2×6 bounded radar), Flycatcher Slowdown (Combat-style square with three identical interactive circular handles, bottom X / left Y / center both), and Special Anomaly Effects (three compact horizontal tracks). The historical Anomaly Effects `DrawMixerStrip` family and its 6-fader 6/4/3/2 wrapping acceptance requirements **no longer apply to Anomalies**. The mixer remains a reusable component elsewhere where still authorized. Existing base damage + selected-difficulty global multiplier remains the mathematical layering model before player protection; the radar's normalized rail is not a synthetic effective-output preview. Do not change source CFG/preset bindings or convert Diamond's raw bleeding points into x. Full geometry, native limits, selected-reference and hover/reset semantics are locked in `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md`.

## Latest Loot override — six compact semantic panels (2026-10-09)

User-approved Loot redraw replaces the old separate vertical `BreakableBoxYields` and `StashRewardProfile` faders, the standalone `StashAmmoRadar` preview, the former 750-DIP `AmmoDistribution` graph and separate giant NPC/World Loot boards. **This override is authoritative for Loot** over earlier generic mixer examples in this document; other tabs' mixers and responsive rules remain locked.

Exactly six visible Loot panels in order: **World Loot Depletion (5 sources), World Placement (5), NPC Loot & Equipment (4), Stash Rewards (6), Breakable Boxes (6), Mutant Part Drops (14)**. Stash Rewards uses three columns at >=750 DIP and two below, with measured 263/418 DIP heights; the two World depletion columns stay readable and the mutant-drop bank is 2×7. All Loot controls follow Stamina Usage label/current/rail hierarchy, with native ranges, reference ticks and hover-only gold handles. See `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md` for the authoritative exact source-to-panel mapping and mouse/keyboard/reset behavior.

**Ammo family bar semantics:** The SNG chart has three segments `9×18/9×19`, `5.45`, `12 Gauge`; NATO chart has four `.45 ACP`, `9×19`, `5.56`, `12 Gauge`. Every segment is permanently keyed by a matching swatch, caliber text and share percent inside its own chart card. The same underlying family weight MUST use the same bar/swatch color in both pools regardless of segment ordering; zero-width segments must preserve a labeled 0% entry. Numeric legends show composition *within that pool*, not global drop probability. The two yield controls are independent of weight composition and remain native source-bound sliders. No duplicated preview lines, phantom selectors or new gameplay values.

## Latest Equipment override — two compact sections (2026-10-09)

**Item Weight is NO LONGER a vertical mixer.** The prior status and pilot design sections regarding two Item Weight strips are historical. Equipment now shows two full-width top-level sections; `EquipmentWearBalance` adopts the **same three interactive gold circular handles** as Human Combat (bottom edits Armor Toughness, left edits Weapon Wear / Shot, center jointly edits both), with Combat-style axis heading/current-value text, native square grid and muted reference ghosts. The plot stays centered at 200–240 DIP in a measured 347–387-DIP board; there are no duplicate side readout cards or bottom instructions. `ItemWeightBoard` retains two ordinary compact **horizontal** source cards (Ammo Weight, Food & Medicine Weight). Render weights using the same native horizontal `DrawEconomyCard` rail grammar as Stamina Usage, max 370 DIP per 2-column card >=672 editor width and max 440 DIP in a narrow single column. Measured height 154 DIP or 248 DIP. Two independent 0–2x source multipliers retain 0.01x increments, native ticks, muted reference and handle-only glow. The centered Equipment graph is width-bounded to prevent stretching, with its panel height measured from the real plot side. Its center is draggable for both native source values, matching Human Combat. Do not use vertical Item Weight `DrawMixerStrip` again without explicit approval. The four historical Equipment Bonus Caps are **audit-only** and do not appear in the active four-source Equipment UI; their real effects beyond maximum-value upper bounds have not been proven. Source/preset/writer mappings are untouched. See the latest UI lock and visual-control catalog.

## Current Economy override — supersedes vertical mixer examples (2026-10-09)

The user explicitly authorized a tab-wide Economy redesign. **Only for Economy**, override the earlier Service Prices / Trader Stock mixer-strip rules and the old section-grid Economy pairing examples: all top-level Economy sections are now individually full-width (1×1), and internal controls use compact horizontal native-range cards. The Services and Trader Stock panels are no longer rendered by `DrawMixerStrip`; other tabs' mixers remain unchanged.

- `CouponFlow` — 3 native controls, two columns plus centered final row at editor widths >=672 DIP; one column below.
- `TraderStockBoard` — 2 horizontal native supply scalers, original independent 0.25× increments.
- `ServiceCostsBoard` — 3 horizontal native price controls; base repair source shown as a percentage.
- `ItemPriceScalersRadar` — 6 compact horizontal source cards in 2×3 layout, covering all five category layers plus actual mutant-part Cost scaling.
- Trading and Reputation Repair retain their previous user-approved compact horizontal designs.

Each new shared Economy card is 80 DIP tall, 94-DIP pitch, 12-DIP intercolumn gap, with a centered gold title and no nonessential subtitles or legends. Breakpoint: 672-DIP editor width for two columns. Last short row remains centered. Item Price uses a dedicated 326-DIP canvas and 78-DIP cards with 90-DIP pitch. Current numeric tokens alone turn gold and grow 1.2 DIP when changed against the selected Base difficulty; grey reference ticks remain. Only circular handles glow on hover. Controls retain their original native parameter IDs, ranges, keyboard/drag/right-click/R semantics and right-panel rationale. See `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md` for the exact current designs; older notes on Economy strip faders are superseded, not live acceptance criteria.

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
- Prefer rows of 6/4/3/2 over artificially narrow strips. For the six-channel **Anomaly Effects** board, continuation rows stay **left aligned** in source order. For compact two/three-channel banks (Services, Trader Stock, Item Weight, Breakable Box Yields, Stash Rewards), **center each complete or incomplete row** within the board without stretching strips beyond 148 DIPs. Both choices preserve source order and per-strip minimum 128 DIPs.
- **Responsive width polish:** both the six-channel Anomaly Effects board and smaller mixer banks cap strip width at **148 DIPs** while maintaining **128 DIPs** minimum; surplus horizontal width stays available as whitespace rather than stretching sliders. Anomaly continuation rows remain left aligned, compact banks are centered.
- Row height is determined by the tallest label/legend in that row; each track must retain the same vertical alignment within its row.
- Preserve authored semantic order on wrap: global effect first, then related local effects. No dynamic reorder based on current numeric values.
- Board header and a short source-accurate guidance/footer line can span all columns. Do not repeat a long “Current/Base Preset” explanation in each strip.
- Avoid mini-horizontal sliders inside each mixer strip. A mixer strip must read as a **vertical control** even in the narrow layout.
- Recompute measure and arrange from the same breakpoints and actual row heights to avoid clipped bottom content or stale hit rectangles when resizing.

### Tab-level grid composition (independent boards)

- Introduce an **opt-in semantic board grid**, separate from the strip grid.
- **Implemented section-grid pairs:** adjacent **Stashes + Breakable Containers** (Loot), **Human Enemies + Mutants** (approved square graph with two faders), **Your Weapons + Explosives** (Combat), **Trader Stock + Item Price Scalers** (Economy), and **Equipment Wear + Item Weight** (Equipment) may each share one row when the left-editor width is at least **1096 DIPs**. Each section keeps its own heading and source-order membership, with at least **540 DIPs** per section and a 16-DIP gap. Other sections remain full-width; narrower editors stack everything.
- These are strictly **adjacent catalog section** pairings: never reorder the sections, move controls to another tab, or merge source families merely to fill a grid. The Economy **Services** section is not adjacent to Trader Stock and is deliberately not paired with it.
- Static regression: `python3 tools/verify_section_grid.py`. Actual WPF/Wine sizing, clipping and section header presentation remain to be checked.
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

### Combat: Your Weapons mixer (2026-10-08)

The four vertical faders in **Your Weapons** are Weapon Damage, Headshot Damage, Weapon Jamming, Explosion Damage (catalog-native ranges and steps). Their section sits to the right of Human Enemies when the two-column section breakpoint is met; otherwise sections stack. There is no separate Explosives section or blast-impact preview in the player-facing UI. Each fader uses the same source-exact mixer interaction, grey reference marker and right-click reset as existing mixers. This is an explicitly user-approved locked UI redesign.

### GroupItem pairing reliability (2026-10-08)

The responsive section grid uses the actual WPF GroupItem.Content to resolve group names, with DataContext as fallback. Human Enemies + Your Weapons therefore pair at at least 1096 DIP available content width, and stack below that. Do not force narrow side-by-side panels that clip the square human plot or four player-weapon faders.

### Fixed 2x1 Combat pairing (2026-10-08)

Human Enemies and Your Weapons are a permanently fixed side-by-side pair (left/right), not an adaptive 2x1/1x1 switch. The group host reserves at least 1096 DIP, horizontal overflow is scrollable, and each child receives at least 540 DIP. Other mixer groups remain unchanged.

### Locked Combat row (2026-10-08)

The first Combat row is a fixed 2x1 section pair: Human Enemies on the left, Your Weapons on the right. Pairing is driven by explicitly known Combat catalog order and active-tab ID, not responsive group-name inference. Only Combat guarantees the 1096-DIP minimum; other tab layouts remain responsive. Keep this arrangement until the user explicitly changes it.

### Fixed Combat row identity (2026-10-08)

The locked 2x1 row always contains **Human Enemies | Your Weapons**, identified from each GroupItem's bound group name, regardless of WPF enumeration order. Mutants is not an approved partner and must remain below that row. Keep the four player-weapon faders under Your Weapons.

### Locked Your Weapons internal 2×2 grid — 2026-10-08

Inside the fixed right-hand Combat section, four faders must be a centered 2-column, 2-row bank with equal cell widths and consistent spacing. Do not reuse the generic mixer column-count algorithm for this family: it produced an overflowing 3+1 arrangement. The fourth Explosion Damage fader must always be visible beneath Headshot Damage. The outer Combat section arrangement remains Human Enemies | Your Weapons.

### 2026-10-08 — All sections fixed 1x1 (supersedes two-column designs)

**Explicit user instruction:** Every top-level section must occupy its own full-width row (1x1). No responsive or fixed side-by-side pairing is permitted, including Human Enemies / Your Weapons, Mutants, trader, equipment, loot and other catalog sections. `ResponsiveSectionGridPanel` now measures and arranges one GroupItem per row, preserving catalog visual order. Combat no longer reserves a 1096-DIP minimum, and horizontal scrolling is disabled again. The four faders **inside** Your Weapons retain their internal 2x2 arrangement; this change concerns top-level sections only. Earlier 2x1 instructions are superseded. No CFG or parameter mappings change. Wine acceptance pending.

### Human / Mutant Combat mixer contract — 2026-10-08

Human Combat has three vertical tracks next to its graph: Attack Frequency 0.5–2x, Shot Accuracy 0–1x, Explosion Damage 0–2x. Your Weapons excludes explosions. Mutant Health has 8 matching vertical faders in a fixed 4×2 bank on its own full-width section, each with the corresponding native catalog minimum, maximum, Base value marker, gold current handle and circular hover glow; no redundant horizontal HP slider or box. Effective health is displayed in HP as supplementary information, not another draggable control. Do not apply the source maxima of Human Combat to mutant health.

### Mutant Health / Your Weapons row lock (2026-10-08)

Inside the fixed 1x1 full-width section layout, Mutant Health has eight source-native vertical faders in a single row with equal spacing. Your Weapons has three vertical faders in a single centered row. The old Mutant Health 4x2 and Your Weapons 2x2 layout descriptions are superseded. Do not auto-wrap either bank; preserve their native source semantics and grey reference ticks.

### Mutant Combat: two-tier fixed composition (2026-10-08)


### Mutant Combat horizontal/vertical split (2026-10-08)


### Locked Mutant Combat horizontal pair — 2026-10-08

Global Hit Damage and Attack Frequency must sit in one 2-column row (left/right), each featuring a horizontal rail. They are not vertically stacked. Their cooldown bank is a separate eight-fader row below.

### Compact Mutant Combat and Psy axis styling — 2026-10-08

Mutant Combat's global side-by-side horizontal controls occupy the top row without a tall reserved vertical lane. Eight vertical ability faders occupy the next band, preserving exact native wait semantics. Psy Phantom Pressure adopts a square Human Combat-style interactive crosshair, circular glowing X/Y/center handles, and a right-side vertical Gear Damage mixer plus Bleed toggle. Repeated-hit controls are internal-only; their source CFG fields persist. Top-level sections stay 1x1.

### Combat view grouping lock (2026-10-08)

Combat section order is fixed: Your Weapons, Human Enemies, Mutant Combat, Mutant Health. Do not depend on alphabetical or incidental raw catalogue ordering; explicitly order before WPF group formation. The source-only Psy Combat and Repeated Hit Protection domains must not form visible group headers. Your Weapons: exactly three centered, equal-size vertical faders in a single line with correctly accounted two gaps. Top-level sections remain 1x1.

### Single in-card heading contract (2026-10-08)

All visual section controls own one centered accented top title; optional concise description below is also centered. WPF group headers and their decorative separator bars have no visible output or reserved height. Preserve fixed 1x1 external layout; changes to titles are styling only.

### Shared baseline for next-tab passes (2026-10-08)

All tabs use fixed full-width 1x1 sections without duplicate external headings or separator lines. Dedicated radar cards with top headings now use centered 12.5-DIP gold titles and optional centered 9.5-DIP grey subtitles, matching Combat. Specialized control interactions and internal layouts remain for subsequent tab passes. Embedded Drunkenness has no separate top title pending redesign.

### Your Weapons 3x1 horizontal track lock (2026-10-08)

The Your Weapons card has three equal-width horizontal sliders in one line, with native source min/max, current multiplier, grey Base marker and gold draggable handle. Cell gap 12 DIP, compact card height 169 DIP. This supersedes all earlier vertical Your Weapons mixer layouts.

### Hunger triple graphs and clean weapon values (2026-10-08)

Survival > Hunger State Ladder keeps its existing stage preview. The three stage threshold sliders become a connected horizontal-coordinate graph with LOW/MEDIUM/SEVERE node handles and actual 0–100 point mapping. The three recovery sliders become a separate three-node signed-effect graph with vertically dragged Well-Fed (+) / Medium (-) / Severe (-) handles. No fake combined difficulty or percent formula is introduced; curves connect independent native CFG controls. Preserve Base ghost ticks, snapping, resets and native constraints. Your Weapons horizontal tracks suppress the literal BASE value text while keeping the visual grey marker.

### Hunger Ladder: single 2D effect graph (2026-10-08)

After Hunger Build-Up and the retained read-only stage preview, show one full-width HUNGER EFFECT CURVE. Horizontal axis = hunger points 0–100. Vertical axis = signed stamina regen effect +40% to -100%. LOW has a horizontal threshold ring only. MEDIUM and SEVERE are draggable 2D pairs (threshold X; recovery penalty Y); WELL-FED is a standalone vertical ring on the left of the graph. Draw gold current links and grey Base ghost links. Preserve independent CFG parameters and tick snapping. Compact height is derived from one plot plus footer; never render the previous two graph cards.

### Hunger Survival Profile: intuitive discrete stages (2026-10-08)

Replace sloping Hunger Effect Curve with a discrete step graph to avoid falsely implying continuous effect interpolation. Preserve Hunger Build-Up and stage preview. In one bordered profile, allocate a distinct Well-Fed vertical bonus rail at left and a right-hand 0–100 hunger point / 0..-100% recovery loss chart. LOW threshold ring edits X only. MEDIUM and SEVERE 2D rings edit native threshold and penalty. Highlight gold current stages and dashed grey Base state, with labeled value slots below; keep hover/glow rings, source steps, right-click reset and group-wide R reset. Do not relabel penalties as direct HP damage or fabricate low-state recovery loss.

### Hover/reset and reference-label lock (2026-10-08)

Across the visual control system: do not render interactive hit regions as rectangular outlines; emphasize the active ring handle instead. Reset gestures stay active but are visually silent (no ephemeral gold text). Remove textual BASE from Hunger State Ladder while retaining its grey reference geometry. Mixer strips should be frameless.

### Hunger fader and chart axes (2026-10-08)

Left Well-Fed slider is explicitly labeled STAMINA RECOVERY BONUS; upward drag increases the bonus. The main staged loss plot has X = HUNGER POINTS (0–100) and a rotated Y = STAMINA RECOVERY LOSS label in its own left margin, separated from numeric percent ticks. Keep labels legible without colliding with handles or cards.

### Hunger graph compact HUD scale (2026-10-08)

Hunger X and Y axis numbers use stronger 10-DIP semibold HUD labels, with a `0% NORMAL` neutral recovery tick. The two helper lines below the graph are eliminated and the widget's desired height shortened accordingly. Keep the in-graph state labels, Well-Fed bonus rail and native CFG semantics.

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

### 2026-10-09 — Universal minimal checkbox design (explicit approval; latest rule)

**Supersedes the earlier WPF pill switch, per-panel checkbox shapes, ON/OFF status-lane pills and the 2026-10-09 global hover-halo requirement specifically for boolean checkboxes.** Across all tabs, every ordinary boolean input renders as the same compact **20 × 20 DIP square**, 3-DIP corner radius, 1-DIP neutral border and dark `PanelAltBrush` face. The checked state is communicated **only by the gold checkmark** (two rounded 2.2-DIP strokes), not by a yellow card, yellow text, yellow track, extra ON/OFF caption or a glowing outline. The unchecked state has an empty square. Static control names remain neutral and adjacent to the box; long names fit in the remaining width without overlapping it.

**Hover policy for boolean controls only:** a slightly lighter neutral border on the square itself (`RadarMutedTextBrush`), no drop shadow, no halo, no animation, no changed font or card background. No ON/OFF label highlighting. Keyboard focus and checked state remain distinct from hover; disabled checkboxes are not editable. The broader universal `ControlHoverGlow` continues to govern sliders and graph handles; **checkboxes are deliberately exempt**.

**Single implementation contract:** WPF's global `CheckBox` template in `App.xaml` and the drawn `BooleanCheckboxChrome.Draw` in `Controls/BooleanCheckboxChrome.cs` share exactly the same size, corner radius, border, stroke, color and hover behavior. `AggregateVisualControl.DrawTogglePill` uses this chrome for all toggle-led panels (World Loot Layout, bundle toggles, healing/radiation/fatigue, starvation, equipment mechanics); `MedicalHealingRadar` uses it for Food Healing. No old slider-style toggle halo overlay may activate for `HitKind.Toggle`. Regular WPF parameter checkbox rows keep one label at left and the same square at right, without repeating ON/OFF.

**Nonvisual behavior unchanged:** each boolean retains its real `ParameterViewModel.CurrentBoolean`, enabled state, existing clickable row/target, click/Space/keyboard selection, right-click reference reset, visual-family R reset, preset persistence, source catalog and CFG writing. The Food Healing card remains clickable in its existing hit area, but only hovering the 20-DIP checkbox brightens its border. No changes to sliders, graphs, native step limits or gameplay values. Inspect the WPF template and draw-based panels under Wine; source/UI geometry checks are not a substitute for a runtime build.

### 2026-10-09 — Survival tab gamer-centric CFG-first redesign (approved; current)

**This is the authoritative current Survival UI contract.** It supersedes the older "read-only duration preview + five vertical duration faders", "smooth bleeding-damage curve", "long one-line fatigue captions", "three intoxication tiles", and overly small stage/axis typography. The user explicitly approved the seven-section Survival redesign based on the catalog's **43 Survival definitions**. All real editable source controls remain available: 7 Hunger, 10 Fatigue, 9 Consumable Effects, 2 Alcohol Strength, 4 Sleep, 3 Radiation, 7 Bleeding, with one additional audit/hidden drunkness-threshold scaler not made into a fictitious new editor. No change to source IDs, CFG write paths, native min/max/steps, reset/presets, select-difficulty reference numbers, gameplay calculations, or established constraint cascades.

1. **Hunger State Ladder** (`HungerLadder`): preserve the accepted synchronized 0–100 HungerPoints preview and threshold-linked *discrete* stamina-recovery loss profile. The three thresholds stay native editing sources and Medium/Severe loss remains true positive stored magnitude / negative UI effect. Keep live stage labels in fixed cells and avoid smoothing authored step changes. Improve the stage preview heading to **11.0 DIP**. No fabricated elapsed-time-to-hunger simulation.
2. **Fatigue Response** (`FatigueCurve`): keep the two independent, real-Y-scale gold AIM TIME and pale RELOAD TIME stepped profiles with shared real 0–100 SleepinessPoints thresholds; LOW has no reload source. Preserve the separate white Y-only reload handles. Widen the footer vertically from one clipped sentence to one fixed **four-line-capable** stage cell in each third: LOW/MEDIUM/SEVERE label; `AT N PTS`; `AIM +N%`; where authored, `RELOAD +N%`. No forced LOW reload. The actual frame and `CompactFatigueHeight` must both use **365 DIP** instead of 320; lines at frame Y+262 / +282 / +304 / +326 and cells Y+260 height93, safely below X-axis ticks. Use 11-DIP stage preview headings and 10.2-DIP real axis captions. Golden modified numbers only when the corresponding source differs from the difficulty reference.
3. **Consumable Effects** (`ConsumableMixer`): retain four independent 0–2x top strength sliders with original native 0.05 steps, and three concise *read-only* effect readouts for Water/Energy/Limited Energy instant stamina, Water/Energy sustained buffs and Beer/Vodka radiation removal. The effect readouts now place **one effect per line** rather than clipping pipe-separated sentences; up to three 9.8-DIP effect lines under a 10.4-DIP title, highlighting/enlarging only changed numeric tokens by +1.2 DIP. No extra duplicate readout headings.
   
   **Replace all five vertical duration faders and the read-only longest-effect preview with one directly editable common-seconds timeline.** Five horizontally independent native source lanes are labeled WATER, ENERGY, VODKA, BEER and MAGIC VODKA. Its common **0–270 real-second** ruler shows 0/90/180/270 seconds. Proven longest authored duration prototypes are **30 / 45 / 20 / 20 / 90 seconds**, respectively, scaled by the same original native **0–3x multiplier** and **0.05x step**. Each lane renders an interactive gold handle at `SecondsX(authoredSeconds × currentMultiplier)`, a thin neutral selected-reference tick, a subdued uneditable tail *beyond that lane's own native maximum*, and a right-aligned exact current longest duration in seconds. WATER's shorter 5-second effect and ENERGY's shorter 5/15-second effects appear only as passive minor ticks at their scaled times, **not as independent controls**. Do not imply all five families can last 270 seconds. Native hit mapping uses the lane's physically shorter `0..authoredSeconds*3` track, rather than stretching the source multiplier across the entire 270s axis; any grey tail cannot receive hover/edit. `AddHorizontalTarget` retains source-native snapping, track selection, focused arrows and right-click/R reset. There must be **exactly nine editable sources** (four strengths + five durations), not duplicated read-only and interactive duration handles. Remove unused `DrawConsumableFader` and `ConsumableFaderColumns`, and measure actual new panel bottom as `previewTop + 181 + 14` rather than retaining the stale 179-DIP fader bank.
4. **Alcohol Tolerance / Drunkenness** (`DrunkennessControl`): keep the two native Beer/Vodka strength sliders and their deterministic already-audited number-of-drinks stage previews. The progression captions use legible ~10-DIP type. Four *read-only* fixed stage tiles now represent **SOBER (below 0.001), TIPSY (0.001+), DRUNK (30+) and WASTED (80+)**, with two separate lines per tile at minimum width and no fake thresholds. The existing hidden `survival.drunknessThresholdScale` stays hidden and OverDrunkness is not folded into the graph. Static stage boundaries and labels are muted, the source value and derived WASTED-after-N count are gold and +1.2 DIP **only if the corresponding Beer/Vodka source is modified**, and the old yellow VANILLA baseline caption disappears; its muted source-reference tick remains. All native source steps, mouse, keyboard, reset and saved preset semantics are unchanged.
5. **Sleeping System** (`SleepingDashboard`): preserve two distinct native access controls (required fatigue points / minimum hours) and two distinct true wake-up effects (HP / stamina effect), with no invented overnight recovery timeline or simulated sleep mechanics. Increase the RECOVERY WHEN YOU WAKE UP section heading to **11.4 DIP**; current source-value cards, reference markers and original editable mappings stay unchanged.
6. **Radiation System** (`RadiationBalance`): intentionally preserve **three independent editable sliders** for difficulty-scoped exposure multiplier, absolute passive points-per-second radiation clearance and harmful-radiation health-damage scalar. A combined exposure-minus-clearance graph or predicted HP/s curve would mix incompatible native units and pretend unverified runtime math. No composite "radiation balance" scalar is added. Existing native source/units, selection and resets remain.
7. **Bleeding** (`BleedingCurveControl`): retain two independent top sources, Bleeding Effect Strength and Natural Recovery, and all five lower native editable stage damage/threshold sources. Render the *current and dashed reference damage path as true LOW→MEDIUM→SEVERE **horizontal steps with vertical transitions*** rather than lines interpolating between discrete authored states. The accent fill follows the same stepped polygon to the chart's right edge. The existing native damage handles and separate Medium/Severe threshold handles stay at the true authored points, including ordered thresholds. Raise chart and native axis typography to ~10–11.4 DIP, and enlarge each independent stage caption zone to **44 DIP** with `STAGE AT N PTS` and `N HP/TICK` as distinct rows; no longer clip 35-DIP footer cells. The 458-DIP frame and existing pointer/CFG mappings remain unchanged.

**Shared invariants:** current values remain neutral at the selected vanilla difficulty reference; yellow and +1.2-DIP size apply only to source-modified numeric tokens; thin reference marks never receive highlight; labels do not overlap or turn gold solely on hover; all handle halos use `ControlHoverGlow` and boolean editors remain universal minimal checkboxes. Do not add redundant on-panel instructions or unrelated simulated gameplay. This contract applies only to Survival and does **not** unlock Combat or any other established tab. Full compile and Wine appearance checks remain the local acceptance step; source-level verification is not equivalent to having run them.


### 2026-10-09 — Source-verified NPC Ammo / Breakable Box Ammo boundary

The current NPC panel is a 496-DIP compact, source-bound four-control family: three centered, full-surface clickable discrete corpse-bonus choice tiles; independent NPC medical and generic NPC bonus-ammo sliders; and native two-handle dropped weapon condition range. The range picks the nearer MIN/MAX endpoint on any rail click with actual 2D handle distances if close, stays captured during drag, and uses 1% native steps. The NPC ammo multiplier updates read-only, real Newbie SmartLoot stack-count examples, not ammo attached to a weapon. Its item rows are from `NPC_Ammo_Smart` in `GameData/StashPrototypes.cfg` and only active with the Medicine + Ammo corpse package.

**Do not conflate it with** `DestructibleStash_AmmoSNG/NATO` breakable ammo box pools in `GameData/ItemGeneratorPrototypes.cfg`. The latter independently retain Ammo Yield and four Ammo Family Weights, with segment-identifying SNG/NATO legends. The former 242 weapon-attached NPC Ammo editor points are retired; older Loot notes about a weapon-attached generic ammo slider are historical. The 43 NPC SmartLoot item entries and 8 breakable pool entries were directly checked against the uploaded GameData/DLC1 CFG dumps. See `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md` for source mappings and tested/unverified boundaries.


### 2026-10-09 — NPC Ammo source-sample compact tile override

For the `NpcLootLoadout` family (current **463 DIP** measurement), the former one-line Newbie ammo footer was replaced by three compact **read-only** sample tiles beneath the native `NPC AMMO` slider. In each: neutral caliber caption (digits in `5.45` and `12 GA` are labels, not changed values), **13.0 DIP** neutral source-count range at the selected Base value, or **14.2 DIP gold** for the **entire MIN–MAX numeric token** only if `ammo.IsValueModifiedFromReference()`. Source counts/rounding from the 43-row `NPC_Ammo_Smart` inventory are unchanged. With inactive `Water`/`None`, preview tiles are subdued and show a concise activation hint; any separately customized ammo count remains gold as stored configuration. Outer preview Y=238/H=104, three inner cards H=52 with 8-DIP gaps, native Dropped Weapon Condition Y=352/H=97. These tiles have no `HitTarget` and must not be mistaken for Breakable Box Ammo or weapon-attached ammo controls.

### 2026-10-09 — Final NPC Weapon Condition range override

The current `NpcLootLoadout` is **496 DIP** high; its `Dropped Weapon Condition` range card is **130 DIP** high at local Y=352. Render both MIN/MAX values in **fixed centered dual readouts** above the single rail, MIN on the left of center and MAX right of center, each with neutral label and gold **N%** only when *that endpoint* differs from the real 25/50% reference. No right-corner values or permanently vertically displaced handles. The rail is gold for selected min-to-max, otherwise muted; vanilla ghost is a very faint **solid** 25–50% segment with tiny neutral reference ticks, not dashed. Both equal circular handles lie directly on the shared rail, with ±7 DIP vertical temporary separation only for X proximity under 16 DIP. Preserve 38-DIP easy-to-click track, nearest-X/2D hit testing, drag hover lock, original 1% step/cascading constraints/reset. The three noninteractive NPC Ammo sample cards remain exactly as designed. This UI override supersedes older special-editor sketches; it changes no Loot CFG controls or writer outputs.

### 2026-10-09 — Breakable Boxes conditional rounds-per-box samples

The newest `AmmoDistribution` board grows from 536 to **680 DIP** to place **AMMO YIELD PREVIEW** directly below the native Ammo Yield and Grenade Yield controls. Its two noninteractive source-pool rows display exactly eight **live generator-native stack-count ranges**, four SNG (`9×18 3–6`, `9×19 5–10`, `5.45 5–10`, `12 GA 2–4`) and four NATO (`.45 ACP 5–10`, `9×19 5–10`, `5.56 5–10`, `12 GA 5–10`) at 1x. These use the **real current Ammo Yield 0.5–2x** and the writer's positive-integer-half-up count scaling, not the four relative family-weight scalers. The permanent weight-composition percent bars remain below at Y=481 (SNG) and Y=575 (NATO). Preview units are ROUNDS IF ROLLED: a range is not an expected or guaranteed drop.

Each source mini-card (39 DIP tall, capped 143 DIP wide) has a neutral caliber label and a centered whole numeric `MIN–MAX` range, default **11.6 DIP**, highlighted **gold at 12.8 DIP** only when `ammoYield.IsValueModifiedFromReference()` is true. Numeric punctuation changes together, calibers never get colored for their digits, and the preview is **read-only** without any pointer hit zones. With a 0x family weight the corresponding source quantity stays visible but subdued; a separately modified Ammo Yield remains highlighted because its selected value is preserved. The actual writer and independent NPC corpse SmartLoot remain untouched.
