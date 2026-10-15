# Loot UI Review

Version: 0.9.26.44

## Goal

Reduce Loot-tab slider clutter without collapsing controls that represent different source semantics.

## Decisions

| Surface | UI decision | Reason |
|---|---|---|
| Breakable Box Ammo Distribution | Keep radar | Relative distribution across a shared selection system |
| Basic / Standard / Rare Stash Ammo | **Radar implemented** | Three parallel quantity multipliers with identical semantics |
| World Ammo / Food & Drink / Medical / Loose Loot removal | Radar candidate | Four parallel `% removed` controls with the same interpretation |
| F1 Grenade Boxes | Include as World Loot Removal radar axis | Dedicated F1 boxes are a disjoint physical population from ordinary Ammo/RGD boxes |
| Mutant Part Drop Chance | Do not use radar | Fourteen axes would be harder to read than a compact grid |
| NPC loot / equipment | **Mixed-input visual board implemented** | Retains real discrete choice, two independent multipliers, and absolute MIN/MAX condition |
| Stash grenade / coupon controls | **Independent source tracks in one board** | Chance and amount remain distinct source semantics |

## Stash food / medical review

Do not promote a generic stash food/medical quantity radar yet.

Cheap stash evidence is especially weak for a meaningful quantity multiplier:

- medical: 12 rows, 8 already fixed at `1–1`;
- food/drink: 33 rows, 28 fixed at `1–1`;
- selection mixes `Chance` and `Weight`;
- lowering a quantity multiplier often becomes a no-op unless zero-count behavior is introduced.

Medium/Rare also contain overlapping difficulty/rank blocks, so a single clean quantity scalar needs a more careful production design before normal UI exposure.

## Implemented in v0.9.26.44

`Stash Ammo Amounts` is a three-axis radar:

- Basic
- Standard
- Rare

Each axis controls only the existing stash-ammo amount multiplier. Writer routes are unchanged.


## Stash Ammo live preview — v0.9.26.44

The aggregate radar now owns the practical quantity display. The former single `PracticalQuantityDisplay` attached to the visible Basic backing parameter is hidden for this radar card.

Below the graph, three fixed columns show the current concrete ammo count ranges for **Basic**, **Standard**, and **Rare**. Values are recalculated from each tier's audited `lootHint.scaledCountEntries` using the same positive round-half-up rule as the production writer.

## 2026-10-10 — Unique weapons shuffle correction

`SHUFFLE UNIQUE WEAPONS` is the sixth compact `WorldLootLayout` input (toggle), not a sixth world-depletion slider. The normal five removal controls remain untouched. The nine verified standalone unique weapon identities move between their nine original world pickup locations with no removals; the existing regular weapon shuffle remains independent. See [source and production contract](generators/LOOSE-UNIQUE-WEAPON-SHUFFLE-AUDIT.md).

## 2026-10-15 — Loot retired mixer cleanup

The actual destructible ammo/grenade yield sliders continue to be edited through `AmmoDistribution`. The inert `BreakableBoxYields` aggregate case (empty member list and obsolete height) and the entire unreachable vertical-strip mixer chain were removed, including their dedicated `MixerSelect` / `MixerBaseMarker` input dispatch and dead helper formatters. No Loot values, native CFG targets, presets, or visible controls changed. Existing `StashRewardProfile`, `NpcLootLoadout`, `WorldLootLayout`, the ammo-family weight editor and the shared live drawing helpers remain intact. `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md` record the current active UI contract; `audit_project_consistency.py` guards against restoring retired paths. Static source contract verified; local WPF build/Wine visual test remain pending.
