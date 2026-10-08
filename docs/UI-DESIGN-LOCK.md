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
7. The build runs `tools/verify_ui_design_lock.py`. A failed UI lock check **must stop publishing**. Never bypass the test simply to get a green build.
8. Tests and docs are additional evidence, not an excuse to alter the visual design. Preserve native gameplay parameters, references and CFG mappings.

## Canonical design references

- [Mixer & responsive grid specification](UI-MIXER-GRID-SPEC.md): strip geometry, per-source values, reference indicators, hit targets, independent board and section grid layout.
- [Visual Control System](VISUAL-CONTROL-SYSTEM.md): non-mixer families, specialized diagrams and interaction semantics.
- [UI design snapshot manifest](UI-DESIGN-LOCK.json): exact Git blob SHA-1 for the canonical specifications and currently approved UI implementation files. It detects file-level changes, not semantic equivalence.
- `tools/verify_mixer_layout.py`, `tools/verify_section_grid.py` and `tools/verify_visual_anchor_triggers.py` verify additional design contracts.

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

### 2026-10-08 — UI-09 — unified handle cues and plot-local instructions

**Explicit user request:** "oba handlea trebaju da imaju isku ikonu, i njihov opis mora da je intuitivnije postavljen"; implement in code and document. Left and bottom handle glyphs remain one identical circular ring-and-dot design (with equal hover feedback). The misleading full-panel-width footer is replaced with two concise plot-centered instructions: `LEFT: DAMAGE · BOTTOM: HEALTH` and `DRAG CENTER: BOTH AXES`. The first is at max(405, plot bottom + 56) and the second 18 DIPs lower. The center remains draggable on both native axes. All original square plot dimensions, controls, source values, attack/accuracy caps, and Mutants pairing remain unchanged. Runtime/Wine acceptance pending.


### 2026-10-08 — UI-09 — runtime caps, not merely catalog caps

**User correction:** Attack Frequency maximum must actually be 2x and Fire Accuracy maximum must actually be 1x; updating the documentation without enforcing runtime behavior is insufficient.

**Implementation:** `ParameterViewModel.Maximum` explicitly caps `combat.npcAttackCooldown` at 2.0x and `combat.npcHipAccuracy` at 1.0x. The existing catalog values match these ceilings. UI faders and normalization take their maximum from this runtime property; dragging, keyboard entry and old preset values cannot exceed it. These are attack *frequency* and human gunfire *accuracy*, not weapon firing rate. No other parameters or UI layout are changed. Static regression checks cover both model-level and catalog-level limits; local native build and Wine QA pending.


### 2026-10-08 — UI-09 — stronger hover feedback on approved handles

**User approval:** Repeated explicit request to implement matching hover glow, interactive X/Y joint, Attack Frequency maximum 2x and Fire Accuracy maximum 1x in code and documentation.

**Implementation:** All three Human Combat rings (X, Y and joint) use the same stronger 20-DIP halo and 12-DIP hover ring. The center joint's hit area is 36×36 DIP; dragging edits both native CFG values, right-click resets both with an accurate status message. Empty plot remains inert. The existing catalog caps are retained at 2x and 1x. No other panels, CFG mappings, preset data, layout dimensions or section pairings are changed. Static tests updated; local Wine visual acceptance pending.


### 2026-10-08 — Canonical UI rules clarified (no visual modification)

The latest approval takes precedence over earlier descriptions when both discuss the same interaction. Human Combat's central joint is **interactive**, notwithstanding the older preview-only statement in the visual specification. Every approved control must preserve its exact native range, step, Base marker, single-setting reset and rationale, and no new pairings, resizes or interaction targets may be inferred from a general instruction to continue. Documentation and regression tests may be corrected without silently changing the approved visuals.

### 2026-10-08 — UI-09 + native parameter caps — explicit approval

User: "ok, nek vertikalni i horizontalni imaju glowup na hoveru i nek budu iste ikone, a njihov presek mora da je isto interactive; attack frequency nek ide do 2x umesto 4x; fire accuracy nek ide max do 1x" and "uradi sve".

Authorized: same glowing ring glyph for both X/Y handles; central joint handle drags both with independent source steps and resets both on right-click; attack cap 2x and accuracy cap 1x. Prior square layout, two right-hand faders, adjacent Mutants section and all unrelated designs remain locked. Runtime QA pending.



### 2026-10-08 — UI-09 — Human Combat readability corrections

**User approval:** "ajde" in direct response to the proposed four fixes: readable Y labels, separated X labels and explanation, vertical alignment of faders, and restored spacing within the existing approved Human Combat layout.

**Scope:** Keep the square health/damage plot, the two right-hand source faders, and the adjacent Mutants section; only increase left margin for the Y labels, move X labels and footer apart, and allow more aggregate vertical height. No CFG/preset or other UI family changes.

**Changed locked files:** `src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs` and `docs/VISUAL-CONTROL-SYSTEM.md`. Test: `tools/verify_human_combat_layout.py`. Runtime visual acceptance pending.


### 2026-10-08 — UI-09, UI-08 — Human Combat Profile redesign

**User approval:** "human combat profile graf bi mogao da bude kvadratan a ne da se prosiruje, a njegovi slajderi desno od toga kao vertikalne kontrole, i onda pored te sekcije ubacimo neku drugu".

**Authorized scope:** Replace the old wide Human Combat Profile plot with a square X/Y plot, move Attack Frequency and Fire Accuracy to two vertical faders at its right, and place the adjacent Combat **Mutants** section alongside **Human Enemies** on sufficiently wide editors. Preserve original axes, source CFG ranges/steps, presets, other panels and all other section pairings.

**Affected lock IDs:** UI-09 (Human Combat Profile visual family) and UI-08 (approved adjacent section pairing); implementation in `AggregateVisualControl.cs`, `ResponsiveSectionGridPanel.cs`; canonical design contracts in `VISUAL-CONTROL-SYSTEM.md` and `UI-MIXER-GRID-SPEC.md`.

**Implementation:** Approved source revision on main branch dated 2026-10-08. Static regression checks required; local .NET build and Wine screenshot acceptance remain pending.

**Lock disposition:** Newly documented layout is locked against unrelated future changes. Refresh exactly the affected tracked files in `UI-DESIGN-LOCK.json` for this explicit authorization; all other baseline snapshots remain unchanged.

When the user later authorizes a specific design change, append a dated entry here stating:
- Exact affected UI registry IDs and relevant source files.
- What the user requested, and what should stay unchanged.
- The implementation commit, tests, and explicit user acceptance status.
- The new locked baseline after acceptance.

To refresh the lock after authorized changes, intentionally run `python3 tools/verify_ui_design_lock.py --update --approval UI-XX` on the changed checkout and commit both the revised manifest and this approval record. **Never** automatically refresh the lock during normal build.
