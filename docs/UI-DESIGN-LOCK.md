# UI Design Lock — Approval-Only Contract

**Project:** True Custom Difficulty
**State:** LOCKED
**Owner of design decisions:** User
**Effective baseline:** 2026-10-08
**Purpose:** Approved UI designs must not silently drift during development, bug fixes, cleanup, feature addition, or refactoring.

## Non-negotiable workflow

1. Treat **every implemented UI design and behavior** as locked by default, including visual controls, layout, typography, colors, hit targets, keyboard/mouse interactions, panel rationale and responsive behavior.
2. Do **not** redesign, rearrange, resize, rename, restyle, repurpose, simplify, replace or remove a locked UI element unless the user **explicitly requests that specific design change**.
3. An open-ended "continue"/"ajde", "polish", backend refactor, performance request, or bug fix is **not blanket authorization** to change an existing design. Work on backend, tests, documentation, or previously unimplemented features without altering locked UI.
4. If a necessary bug fix would affect approved UI appearance or behavior, explain the conflict and obtain explicit permission before changing the locked design. A requested UI change may update only the approved scope.
5. When a new visual family is built and user confirms it looks/works correctly, record its exact layout and interactions, then mark that version LOCKED.
6. Never silently update screenshots, UI specs or lock signatures to conceal drift. A new authorized visual design needs an entry in **Change approvals** and updated lock manifest in the *same commit*.
7. Publishing does not execute automated UI/layout tests; this document remains the required design specification.
8. Preserve native gameplay parameters, references and CFG mappings when implementing UI changes.

## Canonical design references

- [Mixer & responsive grid specification](UI-MIXER-GRID-SPEC.md): strip geometry, per-source values, reference indicators, hit targets, independent board and section grid layout.
- [Visual Control System](VISUAL-CONTROL-SYSTEM.md): non-mixer families, specialized diagrams and interaction semantics.

## Baseline design registry

| ID | Component | Contract | Lock state |
| --- | --- | --- | --- |
| UI-01 | Main window | 16:9 defaults/minimum/resize behavior; existing navigation and right-hand rationale | LOCKED |
| UI-02 | Anomaly Effects | Six original independent vertical faders, source order, Base markers, native ranges/steps, responsive strip grid; current source layout is baseline | LOCKED |
| UI-03 | Service Prices | Three independent vertical faders, native units/steps, Base markers and reset | LOCKED |
| UI-04 | Trader Stock | Two independent vertical faders for ammo and medicine | LOCKED |
| UI-05 | Item Weight | Two independent vertical faders for ammo and consumables | LOCKED |
| UI-06 | Breakable Box Yields | Two independent vertical faders for ammo/grenade quantities | LOCKED |
| UI-07 | Stash Reward Rolls | Three independent faders: grenade chance %, grenade quantity (count), coupon chance % | LOCKED |
| UI-08 | Responsive section grid | Existing four opt-in adjacent pairings; 1096-DIP breakpoint, 540-DIP minimum per section; preserve order and headers | LOCKED |
| UI-09 | Remaining specialized controls | Every current radar, timeline, damage/control curve, native input, rationale, modifier and other control as implemented and specified | LOCKED |
| UI-10 | Responsive parameter-card grid | Current semantic rows, controls spacing and responsive behavior | LOCKED |

**Important:** An in-code lock is a precise source snapshot. It is deliberately broad: even a harmless UI code modification will fail the check until the user has authorized it and the baseline is intentionally updated. Non-UI work is not blocked.

## Explicit approval record

The user requested on 2026-10-08: **"nek se zapise svaki UI dizajn kako bi ostao zakucan a menja se samo kad kazem da se menja"**. This authorizes **introducing this lock system** and recording the existing design; it does **not** authorize any visual redesign.

## Change approvals

### 2026-10-08 — UI-09 — screenshot/runtime verification contract

User repeatedly supplied a screenshot with an old square Y handle, `POINT = PREVIEW` footer and 4x/2x fader limits despite newer main-branch source. That screenshot is **evidence of unaccepted runtime UI**, not a new approved design. The model's 2x/1x caps are now asserted directly by the Human Combat drawing path. `run-latest.sh` fetches, publishes, verifies the build receipt against Git HEAD and EXE SHA-256, refuses to run alongside an existing TrueCustomDifficulty.exe, and runs the verified artifact. Never describe UI-09 as visually accepted until the user verifies the running EXE. Approved intended controls remain matching circle handles, interactive center, clear legend and 2x/1x faders. No other UI surfaces are authorized to change.


### 2026-10-08 — UI-09 — runtime screenshot mismatch follow-up

**User report:** A screenshot of the local running EXE still showed the legacy `BOTTOM CIRCLE = HEALTH | LEFT SQUARE = DAMAGE | POINT = PREVIEW` footer and square Y handle, although that text is absent from current main-branch source. This is a source/runtime mismatch, **not** visual acceptance. The lock records implemented source only; the screenshot is explicitly NOT a verified runtime baseline.

**Approved source update:** Center two concise instructions beneath the existing square plot, `HEALTH ← BOTTOM | LEFT → DAMAGE` in the normal foreground, and `CENTER HANDLE: DRAG BOTH` in gold. All three handles use the same ring renderer, native X/Y dragging and center joint remain interactive. The plot dimensions and adjacent faders are unchanged. Publish now emits a BUILD-INFO.txt containing the exact source commit and SHA-256 of the compiled EXE, allowing local verification that the intended binary is launched. Runtime confirmation pending from user.


### 2026-10-08 — UI-09 — unified handle cues and plot-local instructions

**Explicit user request:** "oba handlea trebaju da imaju isku ikonu, i njihov opis mora da je intuitivnije postavljen"; implement in code and document. Left and bottom handle glyphs remain one identical circular ring-and-dot design (with equal hover feedback). The misleading full-panel-width footer is replaced with two concise plot-centered instructions: `LEFT: DAMAGE · BOTTOM: HEALTH` and `DRAG CENTER: BOTH AXES`. The first is at max(405, plot bottom + 56) and the second 18 DIPs lower. The center remains draggable on both native axes. All original square plot dimensions, controls, source values, attack/accuracy caps, and Mutants pairing remain unchanged. Runtime/Wine acceptance pending.


### 2026-10-08 — UI-09 — runtime caps, not merely catalog caps

**User correction:** Attack Frequency maximum must actually be 2x and Fire Accuracy maximum must actually be 1x; updating the documentation without enforcing runtime behavior is insufficient.

**Implementation:** `ParameterViewModel.Maximum` explicitly caps `combat.npcAttackCooldown` at 2.0x and `combat.npcHipAccuracy` at 1.0x. The existing catalog values match these ceilings. UI faders and normalization take their maximum from this runtime property; dragging, keyboard entry and old preset values cannot exceed it. These are attack *frequency* and human gunfire *accuracy*, not weapon firing rate. No other parameters or UI layout are changed. Static regression checks cover both model-level and catalog-level limits; local native build and Wine QA pending.


### 2026-10-08 — UI-09 — stronger hover feedback on approved handles

**User approval:** Repeated explicit request to implement matching hover glow, interactive X/Y joint, Attack Frequency maximum 2x and Fire Accuracy maximum 1x in code and documentation.

**Implementation:** All three Human Combat rings (X, Y and joint) use the same stronger 20-DIP halo and 12-DIP hover ring. The center joint's hit area is 36×36 DIP; dragging edits both native CFG values, right-click resets both with an accurate status message. Empty plot remains inert. The existing catalog caps are retained at 2x and 1x. No other panels, CFG mappings, preset data, layout dimensions or section pairings are changed. local Wine visual acceptance pending.


### 2026-10-08 — Canonical UI rules clarified (no visual modification)

The latest approval takes precedence over earlier descriptions when both discuss the same interaction. Human Combat's central joint is **interactive**, notwithstanding the older preview-only statement in the visual specification. Every approved control must preserve its exact native range, step, Base marker, single-setting reset and rationale, and no new pairings, resizes or interaction targets may be inferred from a general instruction to continue. Documentation and regression tests may be corrected without silently changing the approved visuals.

### 2026-10-08 — UI-09 + native parameter caps — explicit approval

User: "ok, nek vertikalni i horizontalni imaju glowup na hoveru i nek budu iste ikone, a njihov presek mora da je isto interactive; attack frequency nek ide do 2x umesto 4x; fire accuracy nek ide max do 1x" and "uradi sve".

Authorized: same glowing ring glyph for both X/Y handles; central joint handle drags both with independent source steps and resets both on right-click; attack cap 2x and accuracy cap 1x. Prior square layout, two right-hand faders, adjacent Mutants section and all unrelated designs remain locked. Runtime QA pending.



### 2026-10-08 — UI-09 — Human Combat readability corrections

**User approval:** "ajde" in direct response to the proposed four fixes: readable Y labels, separated X labels and explanation, vertical alignment of faders, and restored spacing within the existing approved Human Combat layout.

**Scope:** Keep the square health/damage plot, the two right-hand source faders, and the adjacent Mutants section; only increase left margin for the Y labels, move X labels and footer apart, and allow more aggregate vertical height. No CFG/preset or other UI family changes.

**Changed locked files:** `src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs` and `docs/VISUAL-CONTROL-SYSTEM.md`.  Runtime visual acceptance pending.


### 2026-10-08 — UI-09, UI-08 — Human Combat Profile redesign

**User approval:** "human combat profile graf bi mogao da bude kvadratan a ne da se prosiruje, a njegovi slajderi desno od toga kao vertikalne kontrole, i onda pored te sekcije ubacimo neku drugu".

**Authorized scope:** Replace the old wide Human Combat Profile plot with a square X/Y plot, move Attack Frequency and Fire Accuracy to two vertical faders at its right, and place the adjacent Combat **Mutants** section alongside **Human Enemies** on sufficiently wide editors. Preserve original axes, source CFG ranges/steps, presets, other panels and all other section pairings.

**Affected lock IDs:** UI-09 (Human Combat Profile visual family) and UI-08 (approved adjacent section pairing); implementation in `AggregateVisualControl.cs`, `ResponsiveSectionGridPanel.cs`; canonical design contracts in `VISUAL-CONTROL-SYSTEM.md` and `UI-MIXER-GRID-SPEC.md`.

**Implementation:** Approved source revision on main branch dated 2026-10-08. local .NET build and Wine screenshot acceptance remain pending.

**Lock disposition:** Newly documented layout is locked against unrelated future changes. Update the affected design documentation when a change is explicitly authorized.

When the user later authorizes a specific design change, append a dated entry here stating:
- Exact affected UI registry IDs and relevant source files.
- What the user requested, and what should stay unchanged.
- The implementation commit, tests, and explicit user acceptance status.
- The new locked baseline after acceptance.

When an authorized design change is implemented, update this document and `VISUAL-CONTROL-SYSTEM.md` alongside the actual UI source.

### 2026-10-08 — UI-09 — reject stale Wine process during publish

**User report:** The running application still shows Attack Frequency 4x and Fire Accuracy 2x, contrary to the approved 2x/1x limits. The embedded catalog, `ParameterViewModel.Maximum`, and Human Combat source already enforce 2x/1x on `main`; do not alter unrelated controls or falsely claim these values were missing in the source.

**Implementation:** `publish.sh` now refuses to publish while an existing `TrueCustomDifficulty.exe` Wine process is active, displays the detected process and asks for it to be closed. This prevents testing the pre-existing 4x/2x window while believing a freshly published EXE is running. The existing `run-latest.sh` independently verifies source commit and EXE checksum. UI implementation and UI lock snapshots are unchanged. Actual Wine runtime acceptance still requires the user to close prior windows, pull `main`, publish and launch the new EXE.

### 2026-10-08 — UI-09 — fix stale EXE detection in publish preflight

**Screenshot remains unaccepted:** Attack Frequency shows 4x, Fire Accuracy shows 2x, and the legacy square/preview glyphs remain visible. The approved runtime bounds are 2x/1x. The previous `publish.sh` process check accidentally used a doubled backslash in its `pgrep -f` regular expression, which could miss an already-running Wine executable. Corrected both matches to `[T]rueCustomDifficulty\.exe` (single regex escape before the period). Publishing now fails before replacing output while the named process is present. This is a publish/verification defect correction, not a change to locked UI geometry or to CFG semantics. Visual/runtime acceptance is still pending a genuinely fresh launch; inspect `publish/win-x64/BUILD-INFO.txt` and run `./run-latest.sh` after closing old windows.

### 2026-10-08 — Remove Python publish preflight gates

**User request:** Remove the pre-publish UI/layout tests because they blocked local EXE publishing (notably a stale `human: rationale not updated` text assertion). `publish.sh` no longer runs the six Python preflight scripts. The test scripts are removed from the repository. UI design requirements in `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md` remain binding, and this change does not alter UI rendering, control ranges, or gameplay behavior. Local build and visual verification remain pending.

### 2026-10-08 — Remove repository test scripts and references

**User instruction:** Remove the tests everywhere, not merely from the publish entry point. Removed Python `verify_*` scripts, runtime test utilities, test-only fixtures, and the unused UI snapshot manifest. Removed active test instructions from documentation. `publish.sh` now performs the .NET publish without any automated Python UI/layout test. UI-DESIGN-LOCK.md and VISUAL-CONTROL-SYSTEM.md remain mandatory UI specifications, not executable tests. Existing gameplay implementation and control limits remain unchanged.


### 2026-10-08 — UI-09 — frameless Human Combat hover and player-readable labels

**Explicit request:** Remove the box on hover (glow only) and arrange the Human Combat control text to be intuitive for players. Human Combat's two fader strips now use an opt-in frameless drawing mode, preserving the interactive hit regions and slider ranges. Hover adds a gold halo to the fader knob rather than an outline around its strip. The second channel is titled SHOT ACCURACY, and the graph footer identifies bottom/left drag actions plus the central dual-value handle. No other mixer family is altered. Runtime visual acceptance pending.


### 2026-10-08 — UI-09 — glow only, no hit box; remove redundant Base 1x captions

**Explicit user approval:** Remove the rectangular selection/hit-target overlay around glowing Human Combat controls, suppress redundant `BASE 1x` captions throughout aggregate controls, and enlarge displayed multiplier values. UI-09 no longer paints the global interactive-target rectangle over its round handles or mixer channels; the mouse hit rectangles remain functional. Mixer captions omit the Base value (grey reference tick remains), and other aggregate text suppresses only unit-valued `BASE 1x` labels; non-unit Base references remain available when informative. Mixer live values increase from 13.2 to 16.2 DIP and endpoint multiplier labels from 9.5 to 11.3 DIP. The frequency/accuracy caps and CFG values are untouched. Wine acceptance pending.


### 2026-10-08 — UI-09 — axis-attached Human Combat labels

**Approved change:** Enemy Health is labeled and valued directly above the horizontal X axis, with a right-arrow cue. Gun Damage is labeled and valued along the left-hand vertical Y axis, rotated for a clear upward-axis cue. The previous split top-row headings have been removed from this control, and the descriptive subtitle beginning “enemy toughness and gun damage” has been removed. The square plot, faders, mouse interaction, values and all other panels remain unchanged. Wine visual acceptance pending.

### 2026-10-08 — UI-09 — axis spacing and missing arrow glyphs

**User correction:** The earlier axis headings were cramped and Wine showed unsupported arrow glyphs as squares. ENEMY HEALTH remains centered above the X-axis, without an arrow; GUN DAMAGE is now a single vertical title in the left gutter and its numeric multiplier appears separately above the left-hand Y scale. The numeric ticks, handles, faders, ranges, and interactions are unchanged. Runtime acceptance pending.

### 2026-10-08 — UI-09 — axis values inline

**User request:** Place each X multiplier on the same line as its axis description, including the vertical axis. The Human Combat X heading is rendered as `ENEMY HEALTH  [value]` on a single centered horizontal row; the Y heading is one jointly rotated `GUN DAMAGE  [value]` row. Both use the existing contrasting text/gold colors and a shared 9-DIP gap, leaving the plot, tick labels and controls unchanged. Runtime visual acceptance pending.

### 2026-10-08 — UI-09 — larger grey fader tick labels

**User request:** Enlarge grey upper/lower multiplier labels for the two Human Combat vertical faders. Both endpoints use 13.0 DIP for frameless Human Combat channels; other mixer strips retain their existing sizes (11.3 DIP maximum, 9.5 DIP minimum). No slider behavior or geometry changes. Runtime visual acceptance pending.

### 2026-10-08 — UI-09 — enemy gun damage label

**User-approved terminology:** Rename the Human Combat vertical axis from `GUN DAMAGE` to `ENEMY GUN DAMAGE`, including its footer legend. Preserve the same axis placement, inline multiplier and gameplay mapping; no other controls change.

### 2026-10-08 — UI-08/UI-09 — Human Combat beside Your Weapons; explosion joins weapon faders

**Explicit redesign approval:** Place YOUR WEAPONS immediately to the right of HUMAN ENEMIES / HUMAN COMBAT PROFILE at the existing responsive two-column breakpoint. The prior Human Enemies + Mutants pairing and Your Weapons + Explosives pairing are superseded. Reorder the Combat catalog so Human Enemies and Your Weapons are adjacent and Mutants follows. Consolidate the former separate Explosives section into Your Weapons, retaining `combat.explosionDamage` as its original CFG parameter with unchanged native 0–2x and 0.01 increments. YOUR WEAPONS renders four independent responsive vertical mixer faders: WEAPON DAMAGE, HEADSHOT DAMAGE, WEAPON JAMMING and EXPLOSION DAMAGE. No redundant separate blast impact preview or horizontal explosion slider is shown. Each fader retains a current value, range labels, grey Base marker, independent drag/reset and per-parameter rationale. Human Combat controls, unrelated sections and gameplay writer mappings are unchanged. This is the new locked layout, subject to user's Wine visual acceptance.

### 2026-10-08 — UI-08 — reliable 2x1/1x1 section layout

**User correction:** Human Enemies and Your Weapons were stacking even with the approved responsive pairing. The section grid now resolves names from the actual WPF `GroupItem.Content` (`CollectionViewGroup`) first, falling back to inherited DataContext. This fixes pairing detection without changing the existing 540-DIP minimum per section: 2x1 side-by-side when the usable grid width is at least 1096 DIP; 1x1 stacked below that. Both sections keep independent full-width headers and correct content sizing. Wine acceptance pending.

### 2026-10-08 — UI-08 — fixed Combat two-column placement

**User request:** The Human Enemies and Your Weapons pairing must be a fixed **2x1 row**, not switch automatically to a vertical stack. Their GroupItem pairing is always honored; the section host is at least 1096 DIP wide so each half can retain its 540-DIP minimum plus 16-DIP gutter. On narrower viewports the left content area scrolls horizontally rather than converting these sections into 1x1. Other established section pairs keep their prior responsive behavior. This overrides the earlier responsive Combat stacking approval. Preserve the two panels, all faders and existing CFG mappings. Wine acceptance pending.

### 2026-10-08 — UI-08 — explicit Combat 2x1 section slots

**User correction:** Combat is a fixed design, not an adaptive grid. Earlier grouping by WPF `GroupItem.Content` or DataContext did not consistently produce the required side-by-side layout. The Combat tab now explicitly allocates its first two catalog groups to one 2-column row: left **Human Enemies**, right **Your Weapons**. The layout does not use group-name inference for these slots; section order is defined in the Combat catalog. Only the Combat section panel has a 1096-DIP minimum width; narrower viewports scroll horizontally instead of stacking these panels. All other tabs retain their prior responsive behavior. This is the approved locked UI-08 placement; local visual confirmation remains pending.

### 2026-10-08 — UI-08 — correct fixed Combat section identities

**User screenshot:** Human Enemies was incorrectly paired with Mutants, while Your Weapons appeared below. The WPF grouped collection did not respect the assumed Human Enemies / Your Weapons ordering. The section panel now resolves explicit `GroupItem.Tag` values populated with `{Binding Name}`, positions Human Enemies and Your Weapons first in the visual layout, and pairs those actual two sections in a permanent 2x1 row. Mutants remains in the following row. Do not pair by unchecked first/second original group index. Preserves controls and CFG mappings. Runtime acceptance pending.

### 2026-10-08 — UI-09 — fixed Your Weapons 2x2 fader grid

**User-approved refinement:** The previous generic mixer layout placed three faders across and pushed/clipped the fourth in the fixed-width Combat right column. Your Weapons now always shows a centered 2-column × 2-row grid, source order: Weapon Damage, Headshot Damage / Weapon Jamming, Explosion Damage. Every strip has equal width and spacing; labels, multiplier values, rails, hover, grey reference ticks, native ranges and reset behavior remain intact. A dedicated two-row height accommodates every fader and the footer without overlapping other sections. Other mixer layouts are unchanged. Wine visual acceptance pending.

### 2026-10-08 — All sections fixed 1x1 (supersedes two-column designs)

**Explicit user instruction:** Every top-level section must occupy its own full-width row (1x1). No responsive or fixed side-by-side pairing is permitted, including Human Enemies / Your Weapons, Mutants, trader, equipment, loot and other catalog sections. `ResponsiveSectionGridPanel` now measures and arranges one GroupItem per row, preserving catalog visual order. Combat no longer reserves a 1096-DIP minimum, and horizontal scrolling is disabled again. The four faders **inside** Your Weapons retain their internal 2x2 arrangement; this change concerns top-level sections only. Earlier 2x1 instructions are superseded. No CFG or parameter mappings change. Wine acceptance pending.

### 2026-10-08 — Combat: Explosion Damage in Human Combat; Mutant Health vertical faders

**User-approved redesign:** Move `combat.explosionDamage` out of Your Weapons into Human Enemies / HUMAN COMBAT PROFILE. Add it as the third independent frameless vertical mixer beside Attack Frequency and Shot Accuracy (source range 0x–2x; native CFG mapping unchanged). Your Weapons now retains only Weapon Damage, Headshot Damage, and Weapon Jamming. The separate Explosives group is not revived. **MUTANT HEALTH** now uses eight source-exact vertical faders in a centered 4-column, 2-row layout: Bloodsucker, Chimera, Deer, Pseudogiant, Pseudodog, Burer, Bayun, Controller. Each fader preserves its native multiplier range, 0.05 step, Base marker, current multiplier and derived HP readout; hover uses a circular gold glow without selection boxes. Mouse dragging uses the vertical source axis; right-click restores an individual baseline, and R restores all. All top-level sections remain fixed 1x1. Runtime Wine acceptance pending.

### 2026-10-08 — Single-row Mutant Health and Your Weapons faders

**Explicit user approval:** Mutant Health must put all eight compact vertical faders on **one horizontal row**, without auto-wrapping into four columns and two rows. Use equal cell widths, compact heading typography and split long mutant names onto two lines to prevent collisions. Its section height is reduced accordingly. **Your Weapons** likewise uses one centered row of its three vertical faders (Weapon Damage, Headshot Damage, Weapon Jamming), not the former 2-column layout. Full-width 1x1 top-level sections remain locked. Native CFG ranges, 0.05 mutant steps, per-fader base ticks, right-click reset, gold handle/glow and current HP labels remain unchanged. Local Wine acceptance pending.

### 2026-10-08 — Special Ability Cooldowns: one row of eight vertical faders

**User-approved change:** The eight mutant Special Ability Cooldown controls use the same compact one-row vertical-fader layout as Mutant Health. Retain original native cooldown/wait multipliers and steps, return-rate readout, grey reference tick, gold current handle, circular hover glow without box, vertical dragging, single-control right-click reset and R reset all. Split long mutant names on two lines. Keep all top-level sections full width (1x1); no unrelated UI changes. Wine visual acceptance pending.

### 2026-10-08 — Special Ability Cooldowns single-row layout

User-approved change: eight mutant cooldown controls now use one compact horizontal row of vertical faders, matching Mutant Health. Their original source values, steps, grey Base ticks, return-rate readouts, glow-only handles, vertical dragging, individual right-click reset and R reset-all are preserved. Long labels are split across two lines. Top-level sections remain 1x1. Runtime acceptance pending.

### 2026-10-08 — Mutant Combat and ability pacing combined

User-authorized consolidation: The former Mutants and Mutant Special Attack Cooldown sections are now one full-width **Mutant Combat** section. The upper **GLOBAL COMBAT** zone contains two independent vertical controls: DAMAGE PER HIT and ATTACK FREQUENCY (the normal mutant attack tempo). A distinct lower **SPECIAL ABILITY COOLDOWNS** zone contains eight compact wait-multiplier faders, one per mutant, with inverse `x RETURN` readouts. The source parameters, native ranges/steps and per-mutant variants are unchanged. Separating normal attack frequency from special cooldown avoids conflating those mechanics. Both zones retain circular glowing handles, grey reference ticks and right-click reset. The top-level layout remains fixed 1x1. The old separate mutant combat aggregate is no longer shown. Local build and Wine acceptance pending.

### 2026-10-08 — Mutant Combat global controls horizontal

User-approved refinement: In the combined Mutant Combat section, **DAMAGE PER HIT** and **ATTACK FREQUENCY** are now two wide horizontal sliders stacked vertically, using glowing gold circular handles and the original CFG min/max, steps and Base reference. The eight SPECIAL ABILITY COOLDOWNS underneath remain compact vertical faders in one row. Dragging of the two upper controls uses horizontal X coordinates; dragging the eight timers continues to use vertical Y coordinates. This explicit two-tier direction contrast is locked. No gameplay or catalog change. Local Wine verification pending.
