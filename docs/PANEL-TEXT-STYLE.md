# Panel text style — how every control explains itself

Status: CURRENT (2026-11-18). Gate: `tools/audit_panel_text_style.py` (in `tools/pre_release.py`).
History of how we got here: `docs/PANEL-TEXT-GAMER-PASS-2026-11-17.md`.

The right-hand panel is read by a **player**, not by a modder. Every text answers "what does this do to my game?" in the words a STALKER player uses.

## 1. Structure per control

| Section | Field | Shape | Max |
|---|---|---|---|
| Panel title | `label` (or `OwnPanelTitle` for board anchors) | The name the board draws. | — |
| **WHAT IT DOES** | `rationale` | One paragraph: **① what it changes in play** (one sentence, starts with *How …* / *Whether …* / *Which …* when it fits) → **② `Vanilla: …`** → **③ what higher / lower feels like** (optional) → **④ `Only works while X is ON.`** if it has a master switch. | 300 chars |
| **HIGHER / LOWER** | `logic` | Direction only: `Higher = …; lower = …` or `0 = …; 2× = …`. No vanilla values (they live in ①–②). | 200 chars |
| **WORKS WITH** | `interaction` | Which other controls stack, override or stay separate — named in CAPS. | 220 chars |
| **CONTROLS** | `ControlHelp` / NPC LOOT card help | What the hands do, then one reset sentence: `Right-click one <part> to reset it; R resets <scope>.` | — |

Template:

```
How often <thing happens to you>. Vanilla: <value>. Higher = <feel>; lower = <feel>. Only works while <MASTER> is ON.
```

## 2. Format rules

* **Vanilla**: `Vanilla: X.` when equal on every difficulty; otherwise `Vanilla: Novice a, Stalker b, Veteran c, Master d.` with equal neighbours merged (`Novice–Veteran 1×, Master 2×`). Stated once — never repeated in HIGHER / LOWER.
* **Units = the control's readout**: `×` for multipliers (never the letter x), `%` for percentages, plain numbers for raw values; a real-world amount may follow in brackets — `Vanilla: 1× (500 HP)`. Endpoints carry the unit too: `0% removes it`, `0× = never`.
* Space before units: `45 s`, `12 m`, `0.015 pts/s`, `1 HP/s`.
* Ranges with an en dash `9–12`; before → after with `→`; separators ` · `.
* Other controls are always named in **CAPS**, exactly as drawn: GLOBAL ANOMALY DAMAGE, SMART LOOT, RGD-5 AMOUNT.
* Call a control what it is: bar, knob, slider, rail, checkbox, chip, marker.

## 3. Tech speak → player words

| Don't write | Write |
|---|---|
| CFG, SID, prototype, generator, entry, pool, native, authored, baseline | *the game*, *vanilla*, the item's name, *loot*, *offer*, *spot* |
| roll, weighted picks, spawn chance, MinCount/MaxCount | *chance*, *turns up*, *how often*, *stack size* |
| state, bundle, layer, component, actor-level, configured | *while you're severely tired*, *these penalties*, *on top*, *you* |
| coefficient, parameter, value stored / backing | *strength*, *setting*, *saved* |
| internal ids (`BandageHealing2`, `energyFullCostDuration`) | the control's name in CAPS |
| dev notes (*verified*, *acceptance test*, *not adjustable yet*) | nothing — leave them out |

Say what the player **feels** (*less breathing room between attacks*, *you lose 4 HP every 10 s*), not how the system is built (*scales the waits of all the family's attacks*). Don't explain what the card already shows.

## 4. Scope and exceptions

* Checked: `rationale`, `logic`, `interaction` of every catalog row the panel can show; NPC LOOT card texts; CONTROLS help; every string literal in `Controls/`, `ViewModels/` and `PresetService` (boards, panel blocks, dialogs, preset errors).
* Allowed exception: the modder-facing build summary (`BPatch CFG files`, `CFG edits`).
* Skipped: legacy compatibility-only rows with no control (`LEGACY` in the audit) and the legacy BUY restrictions (`Trader Buys`). `description` is a fallback field and not shown when a rationale exists.
* `--report` lists every hit without failing; use it before a copy pass.

## 5. Adding or changing a control

1. Write the four sections with the template above.
2. `python3 tools/audit_panel_text_style.py` must PASS.
3. If the board draws a new name, the label follows it (or add an `OwnPanelTitle` entry for a board anchor).
