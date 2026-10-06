# Human NPC Dropped Ammo Amount — Runtime-Proof Contract

**Version:** v0.2.34  
**Status:** `STRUCTURALLY_READY_RUNTIME_PROOF` — test contract only; no UI/writer promotion

## Proven static boundary

The candidate uses the same repeatable-human production boundary as dropped-weapon condition: **121 direct weapon rows across 33 loadout generators**. The two Prologue and four named MIK/Mykluha roots remain hard-excluded, leaving zero nested/shared weapon dependencies in the production surface.

All **121/121** production weapon rows expose `AmmoMinCount` plus an ammo-max field. The source contains two exact max-key spellings and any future writer must preserve them: **118 `AmmoMaxCount` + 3 `AmmoMaxcount`**. Vanilla lower bounds are zero on **81** rows and positive on **40** rows. Observed vanilla pairs range from `0..1` through `30..45`.

These fields are structurally isolated from the separate loose-ammo generator category. Static topology proves where to edit, but does **not** prove whether the values become loaded/carried weapon ammunition, corpse inventory ammunition, another runtime weapon state, or are post-processed elsewhere.

## A/B test values

Use deliberately non-vanilla, non-zero fixed values across all 121 production weapon rows:

| Test build | `AmmoMinCount` | ammo-max field | Why |
| --- | ---: | ---: | --- |
| LOW | 1 | 1 | non-zero value avoids treating zero as a possible engine fallback/default sentinel |
| HIGH | 77 | 77 | far outside every audited vanilla maximum (45), so the effect is unmistakable |

Do **not** change weapon `Weight`/`Chance`, `MinDurability`/`MaxDurability`, the separate `Ammo`/loose-ammo category, consumables, or corpse SmartLoot during this test.

## Test procedure

1. Build a LOW test mod that changes only the 121 attached-ammo min/max pairs to `1..1`.
2. Start from a clean game state with no other mod changing the same human ItemGenerator weapon rows.
3. Spawn/encounter ordinary repeatable human NPCs from at least **three role families** (recommended: Stormtrooper, Sniper and CloseCombat) and preferably more than one faction/loadout family. Avoid Prologue, named/scripted NPCs and MIK/Mykluha.
4. For each sample, record the generated weapon and every observable ammo manifestation: weapon state while carried if inspectable, dropped weapon state, corpse inventory ammo and any loose ammo stack.
5. Repeat with HIGH `77..77` using equivalent ordinary NPC families.
6. Compare LOW vs HIGH. The important evidence is a deterministic or strongly bounded change tied to the selected weapon entry, not merely “more ammo seemed to drop.”

## PASS / FAIL decision

**PASS — runtime semantics proven:** LOW and HIGH produce clearly different observable ammo state consistent with `1` versus `77` on ordinary repeatable-human weapon generation, across multiple role/loadout families. Record *where* the value manifests. Only then may the candidate be promoted and given a user-facing name matching the observed behavior.

**FAIL — no direct runtime effect:** LOW/HIGH do not produce a repeatable difference attributable to these fields. Keep the control out of the UI and document the fields as runtime-neutral/overridden for the tested path.

**INCONCLUSIVE:** a difference exists but cannot be separated from loose-ammo generation, reload/combat consumption, magazine capacity/clamping, or another post-process. Keep `STRUCTURALLY_READY_RUNTIME_PROOF`; refine the test instead of promoting the control.

## Naming rule

Do **not** assume the final label is `Human NPC Dropped Ammo Amount` or `Ammo Supply`. After PASS, name the control for the observed runtime effect. For example, if the fields affect only ammo attached to the generated weapon and not corpse inventory stacks, the UI must say that rather than implying total NPC loot ammo.

## Writer contract after a future PASS

- target exactly the 121 local production weapon rows / 33 repeatable loadout roots;
- preserve the 2 Prologue + 4 MIK/Mykluha hard exclusions;
- modify only `AmmoMinCount` and the existing exact max-key spelling;
- preserve weapon selection `Weight`/`Chance`, durability, rank gating and all separate loose-ammo generators;
- do not normalize the three `AmmoMaxcount` source keys to `AmmoMaxCount` unless source-format compatibility is separately proven.
