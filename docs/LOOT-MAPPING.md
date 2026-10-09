> **Phase 14B2 (2026-10-10):** Ten remaining world Loot writer inputs now have live production C# regression suites: randomized physical actor removal of five box/loose categories, and world weapons/density, attachments, blueprints and detectors. Invariants validate source-manifest membership, DLC1 eligibility, category/tier constraints and absence of collateral CFG output. See [World Loot Phase 14B2](DEEP-CFG-AUDIT-PHASE14B2-WORLD-LOOT-2026-10-10.md). Original archive and in-game gates remain separate.

> **Phase 14A direct-source audit (2026-10-10):** The current Loot catalog has 40 parameter IDs. Exactly **16 IDs / 320 direct CFG points** have been independently checked against the original decoded ZIPs: 14 mutant-part chances, 242 NPC weapon durability bounds, and 64 Rare Stash coupon routes. The 64 coupon mappings are **physically inherited** from `GamePass_Stash_ItemGenerator_Rare` through explicit refurl/refkey wrappers; they are not directly authored under the four child roots. **24 other Loot IDs are implemented via specialized writers** and require separate Phase 14B coverage. See [Phase 14A source and writer report](DEEP-CFG-AUDIT-PHASE14A-LOOT-DIRECT-2026-10-10.md). No in-game or UE merge acceptance is implied.

# Loot mapping — strict source model

## Scope

This document describes only Loot controls that are currently safe to expose plus the audited deferred research areas. The source of truth is the current extracted base `GameData` and `DLC1` CFG set.

## Rule

The tool does not expose a Loot control merely because related fields exist. A visible control needs an explicit writer path with known targets and one well-defined operation. Aggregate selectors based on phrases such as “normal NPC”, “loose ammo”, or “stash medical loot” stay deferred until their full target set is enumerated.

## Current visible Loot controls

### Extra NPC Corpse Loot

This is backed by the explicit difficulty smart-loot list under `DifficultyPrototypes.cfg`. The writer edits the selected vanilla difficulty's `EconomyDifficulty.CorpseSmartLoot.GeneralNPC` list. The v0.1.78 deep audit confirms this is an **exact 1:1 set-list control** with only three source-aligned states: Novice = Medicine + Ammo, Stalker = Water, Veteran/Master = None. The three referenced SmartLoot packages have no mapped non-difficulty consumers, and their internal rank/group/item rows are not modified. See `docs/generators/SMARTLOOT-CORPSE-BONUS-AUDIT.md`.

### Rare Stash Coupon Chance

This uses explicitly enumerated `Chance` edit points for the rare-stash money entries and preserves the vanilla difficulty split. The writer baseline contains the exact target nodes rather than a wildcard stash selector.

### Individual mutant part drop chances

Each base-game mutant slider maps to one concrete loot generator and its direct `Chance` property:

| UI mutant | Runtime generator | Property |
| --- | --- | --- |
| Blind Dog | `BlinddogLootGenerator` | `ItemGenerator[0].PossibleItems[0].Chance` |
| Bloodsucker | `BloodsuckerLootGenerator` | same |
| Boar | `BoarLootGenerator` | same |
| Burer | `BurerLootGenerator` | same |
| Bayun | `CatLootGenerator` | same |
| Chimera | `ChimeraLootGenerator` | same |
| Controller | `ControllerLootGenerator` | same |
| Deer | `DeerLootGenerator` | same |
| Flesh | `FleshLootGenerator` | same |
| Poltergeist | `PoltergeistLootGenerator` | same |
| Pseudodog | `PseudodogLootGenerator` | same |
| Pseudogiant | `PseudogiantLootGenerator` | same |
| Tushkan | `TushkanLootGenerator` | same |
| Snork | `SnorkLootGenerator` | same |

The user-facing name is **Bayun**; `CatLootGenerator` is retained only because it is the game's internal CFG SID.

## Corrected NPC consumable structure

The current base CFG contains both of these active structures:

1. four rank pools: `ConsumableNewbieGenerator`, `ConsumableExperiencedGenerator`, `ConsumableVeteranGenerator`, `ConsumableMasterGenerator`;
2. eight `GeneralNPC_Consumables*` role-oriented prototypes, including `GeneralNPC_Consumables_Recon`, `..._CloseCombat`, `..._Sniper`, `..._Stormtrooper`, `..._Heavy`, and Noon/armored variants.

Both families are referenced by the runtime item-generator graph. Therefore a single `NPC Medical/Food/Utility` slider cannot be treated as 1:1 until every intended prototype/entry is explicitly listed and the exclusions are equally explicit.

## Corrected NPC weapon structure

The base CFG contains the 60 EN/RU/ALL role/rank weapon-generator SIDs, and the modern `GeneralNPC_*` item generators also contain direct weapon entries. DLC1 adds its own `GeneralNPC_*` weapon families. `MinDurability/MaxDurability` and `AmmoMinCount/AmmoMaxCount` are real fields, but one global Dropped Weapon Condition or Dropped Weapon Ammo slider is an aggregate policy decision unless all targets are explicitly enumerated.

## Corrected stash structure

`Stash_Cheap_*`, `Stash_Medium_*`, and `Stash_Expensive_*` commonly inherit shared `GamePass_Stash_ItemGenerator_*` prototypes. The actual medical, food/utility and ammo entries live in those shared bases and in additional standalone stash generators. Some branches use `Chance`, while others use relative `Weight`.

For that reason there is currently no generic stash medical/food/ammo abundance slider in the UI.


## Full generator topology

The exhaustive topology is in [`GENERATOR-MAP.md`](GENERATOR-MAP.md). v0.1.65 adds the human-readable [`GENERATOR-CATALOG.md`](GENERATOR-CATALOG.md) and [`SMARTLOOT-CATALOG.md`](SMARTLOOT-CATALOG.md), with exact branch/entry tables and CSV data under [`generators/`](generators/README.md).

The audit establishes a hard boundary between two systems:

1. **Item Generator** — `ItemGeneratorPrototypes.cfg`, including inheritance, nested `SubItemGenerator` composition and consumers such as NPC inventory, traders, quests and SpawnActor `ItemGeneratorSettings`;
2. **SmartLoot** — `StashPrototypes.cfg`, consumed through SpawnActor `StashPrototypeSID` and `DifficultyPrototypes.CorpseSmartLoot`.

A physical container can use both systems at once. Therefore future aggregate Loot controls must name the system and exact target set instead of treating all stash/NPC loot as one namespace.

## Post-audit UI state (v0.2.17)

**Zombie Grenade Frequency** is implemented as a relative 0-250% control over the exact 91 child-local grenade `Chance` rows across 91/93 generic zombie roots. 100% preserves vanilla; the two roots without a grenade branch stay untouched. See `generators/ZOMBIE-GRENADE-FREQUENCY.md`.

**Zombie Weapon Condition** and **Zombie Weapon Ammo** are fully mapped over the complete existing generic-zombie weapon boundary, but are intentionally **audit-only / no-UI** because they tune loot/loadout outcomes rather than core difficulty. Human NPC Detector Chance and Zombie Detector Chance are also documentation-only / no-UI. See `docs/generators/GENERIC-ZOMBIE-WEAPON-SOURCE-AUDIT.md`.

## Deferred research items

The following former controls are intentionally not visible:

- Dropped Weapon Condition
- NPC Grenade Drop Chance
- Loose Ammo Stack Size
- Novice Stash Medical Bonus
- NPC Food Drop Chance
- NPC Medical Drop Chance
- NPC Medical Stack Size
- NPC Utility Drop Chance
- Dropped Weapon Ammo
- Stash Ammo Stack Size
- Stash Food & Utility Drop Chance
- Stash Medical Drop Chance
- Stash Medical Stack Size

Their CFG ingredients are real, but their UI semantics require grouping, filtering, inheritance resolution, or multi-entry scaling that is not itself a direct CFG setting. Details are recorded in `docs/AUDIT.md` and `parameters.json -> deferred`.

## v0.3.2 player-facing contents/quantity contract

Loot controls now keep three concepts separate:

- **Chance** is shown only when the source generator has a real `Chance` field.
- **Weight** is reported as relative selection weight and is never converted into a fake percentage.
- **Quantity** is derived from authored `MinCount/MaxCount`; the UI shows the concrete ammo/grenade type and the vanilla → current count range.

### Basic stash

- Ammo: 9×18 FMJ `2–3` (Weight 40), 9×19 FMJ `5–7` (Weight 30), 12/70 Default `2–3` (Weight 30).
- RGD-5: separate **Chance** (vanilla 10%) and **Quantity** (vanilla 1, selectable 0–4). Quantity writes both `MinCount` and `MaxCount` to the selected exact integer.

### Standard stash ammo

9×19 FMJ, 5.45×39 FMJ, 12/70 Default, .45 ACP FMJ; higher progression ranks additionally introduce 5.56×45 FMJ, 7.62×54 Supersonic/FMJ and 9×39 FMJ. Selection remains Weight/rank-gated.

### Rare stash ammo

Primarily Armor-Piercing/special ammunition: 9×18 AP, 12/70 AP, 5.45×39 AP, 9×19 AP, .45 ACP AP, 5.56×45 AP, 7.62×54 AP/FMJ, 12/70 Expanding and 9×39 AP. Selection uses the authored mixture of Chance/Weight/rank gates; there is no single honest Ammo Chance slider.

### Destructible ammo crates

- SNG: 9×18 FMJ `3–6`, 9×19 FMJ `5–10`, 5.45×39 FMJ `5–10`, 12/70 Default `2–4`; Weight 2 each.
- NATO: .45 ACP FMJ, 9×19 FMJ, 5.56×45 FMJ, 12/70 Default, all `5–10`; Weight 2 each.

### Destructible grenade crates

Dedicated F1 or RGD-5 pool depending on crate type; vanilla `1–2`, Weight 1.

### Human NPC weapon ammo

There is no fixed ammo pool. The control scales the `AmmoMinCount/AmmoMaxCount` payload attached to the NPC's selected weapon row. Authored zero minima remain zero.


### Build-time physical crate reduction

Physical crate presence is controlled separately from loot-generator contents. The current reduction surface is deliberately limited to the two supplied reference populations:

- **Ammo crates:** toggle `loot.reduceAmmoCrates`; canonical population **1,042** base-game `D_WoodenAmmoCrate_01` actors from the supplied FULL pak. This includes SNG/NATO ammo pools plus the F1/RGD grenade-pool ammo crates present in that reference.
- **Food & Drink crates:** toggle `loot.reduceFoodDrinkCrates`; canonical population **1,100** base-game `D_WoodenDSPCrate_01/02/03` actors from the supplied FULL pak.
- **Boxes Removed:** `loot.crateRemovalPercent`, shared 0–100% slider. On each Build, the writer chooses exactly `round(N × percentage)` unique actors independently from each enabled category and emits `<ActorSID> : removenode`.

The random removal surface does not alter the item generators inside surviving crates. Medical, weapon, armor and DLC1 box populations remain outside the current control surface until separately supplied/audited. See `CRATE-SPAWN-AUDIT.md`.
