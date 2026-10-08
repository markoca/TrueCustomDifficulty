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

No post-baseline UI design changes approved yet.

When the user later authorizes a specific design change, append a dated entry here stating:
- Exact affected UI registry IDs and relevant source files.
- What the user requested, and what should stay unchanged.
- The implementation commit, tests, and explicit user acceptance status.
- The new locked baseline after acceptance.

To refresh the lock after authorized changes, intentionally run `python3 tools/verify_ui_design_lock.py --update --approval UI-XX` on the changed checkout and commit both the revised manifest and this approval record. **Never** automatically refresh the lock during normal build.
