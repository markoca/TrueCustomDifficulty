# Generic corpse and stash optional-loot gating — verified 2.1 source structure

**Date:** 2026-11-15. **Scope:** read-only source/CFG audit, with one source-derived regression script and one 463-group catalog. **No player-facing reduction sliders or game writer changes are approved by this audit alone.**

## Main finding

Native `Weight` governs **selection between available candidates**, not whether a group gets a chance to execute. It is never a substitute for genuine activation `Chance`, `MinSpawnChance`, `MaxSpawnChance` or item set count. This remains true even when the user interface can display a useful native relative-weight distribution.

The source catalog [generic-loot-weight-group-gates-2.1.csv](generic-loot-weight-group-gates-2.1.csv) classifies **all 463 audited native groups** by the exact generator SID, rank/difficulty and group, selection field, item SIDs and original group activation fields. The baseline is `src/TrueCustomDifficulty/Data/cfg-writer-baseline.json` pinned to gameVersion 2.1, plus `src/TrueCustomDifficulty/Data/smartloot-world-source-entries.csv`.

### 1. Basic / Standard / Rare original ItemGenerator

| Tier | Original groups | Native Weight groups | Native item Chance groups | Explicit group activation Chance/SpawnChance |
| --- | ---: | ---: | ---: | --- |
| Basic / Cheap | 12 | 8 | 4 | None authored |
| Standard / Medium | 10 | 4 | 6 | None authored |
| Rare / Expensive | 19 | 7 | 12 | None authored |
| **Total** | **41** | **19** | **22** | **0** |

All 41 original group headers contain only the relevant combination of `Category`, `Diff`, `PlayerRank` and `bAllowSameCategoryGeneration`. Their `PossibleItems` contain source `Chance` or `Weight` (not a hybrid within the same audited group), plus `MinCount/MaxCount`. In all **86** authored weighted item rows (38 Cheap + 25 Standard + 23 Rare), `MinCount` and `MaxCount` are positive. No dedicated "group may be empty" or `GroupSpawnChance` field is present in these original groups.

**Important:** A positive weighted category is not an optional roll merely because its `Weight` can be reduced. Halving all competing weights preserves normalized selection, as the existing Basic preview already demonstrates. Setting all weights to zero may use unspecified engine fallback rather than produce an empty reward; source-only audits cannot prove an empty result. We cannot truthfully claim that 100% reduction removes the entire basic/standard/rare stash content through existing native Weight scaling.

**Immediately source-feasible part:** the **22 native `Chance` groups** can have their per-item `Chance` scaled after existing TCD detailed edits, while preserving native rank/difficulty and source 0 values. But this produces *partial* reduction only; it cannot be shipped as a full-tiers "Stash Loot Reduction" slider until the 19 Weight-only groups also have confirmed omission semantics. Do not silently conflate a per-item 100% `Chance` with a physical stash guarantee.

### 2. World Stash SmartLoot

| Native group type | Total groups | Positive `MaxSpawnChance` | Source `MaxSpawnChance=0` |
| --- | ---: | ---: | ---: |
| Weight-only item selection | 410 | **300** | **110** |
| Chance-only item selection | 12 | **10** | **2** |
| **Total** | **422** | **310** | **112** |

All 422 audited world SmartLoot groups define both `MinSpawnChance` and `MaxSpawnChance`. **30** define an authored `ItemSetCount`; the other 392 leave it empty/not authored. Native minima: `0.f` in 375 groups, `0.1f` in 47; no group has `MinSpawnChance > MaxSpawnChance`. Some groups intentionally start **0.f / 0.f**. Do not infer that a zero-max group is activated solely because its actor is production-classified, and do not replace a zero authored gate with a positive tuning value.

**Candidate after engine acceptance:** Within the already approved 803 base and eligible DLC1 physical stash actor redirects, privately clone the existing SmartLoot prototype and apply a common relative scarcity factor `q` to *both* group spawn endpoints **after** existing independent category-level logic. Keep original per-item `Weight` ratios, item `Chance`, quantity and set count unchanged for a positive factor:

```text
q = 1 - reduction / 100
MinSpawnChance_final = source/effective_MinSpawnChance × q
MaxSpawnChance_final = source/effective_MaxSpawnChance × q
```

At source 0/0 keep 0/0 exactly. At 100% target source 0/0 as well, but **do not assert the engine yields no loot** until the actual actor inventory and SmartLoot engine handling of a zero endpoint are observed, including cases with nonempty `ItemSetCount`. Do not automatically write `ItemSetCount=0` for positive `q`, since the field is independent and absent in 392 groups. The current `CfgPatchWriterService.StashSmartLoot.cs` already has a helper `StashEffectiveState.SmartSpawnEndpoint`, but it is invoked for **existing category-specific UniformSmartFactor rules**, not proof of a global stash-abundance writer; blind reuse may double-scale endpoints or conflict with two independently modified item categories.

**Native distribution remains:** For an active group with `Weight`, compute normalized selection among its own original eligible members using the existing relative category mixer; changing `q` alone should not modify those shares. `MinSpawnChance`, `MaxSpawnChance`, `ItemSetCount` and normalized `Weight` represent different layers.

### 3. Repeatable human NPCs

The **six private-cloned** `GeneralNPC_Consumables_*` pools have **104** authored item `Chance` rows (**0** item `Weight` entries) and **33 exact ordinary-human loadout redirect edges**. The external NPC-to-pool `SubItemGenerator` links each use **`Weight=1`**, not a source-authored independent bundle `Chance` gate. This is the same source-semantic boundary as the stash Weight-only groups: changing `Weight=1` to 0 or halving it cannot establish a clean scalar probability of "this NPC receives consumables", especially if other nested groups compete.

**Directly source-feasible:** A final `q_corpse` after the current per-item absolute Chance overrides and Standard Med factor can scale all **104 optional item Chance rows** while retaining the existing six private pools, all 33 redirects and unchanged NPC loadout equipment. That affects standard food/drink/medical only. Extra Corpse SmartLoot remains its own package system with **three** selectable values: Medicine+Ammo, Water and None, and its native package/group gating is not yet validated for partial suppression. A master must not claim to affect all corpse optional loot until that bonus path is also covered. Weapon-attached ammo and Echo detector Chance stay explicitly excluded from every writer/control design.

### 4. Recommended next implementation boundary (not yet shipped)

Keep the original plan of **two** independent compact masters (**CORPSE LOOT REDUCTION** and **STASH LOOT REDUCTION**), both default 0% and compatible with old saved presets, and reuse the existing native-weight composition mixer. Implement **one shared source-aware scarcity calculator** with *separate* methods for an item `Chance` roll and a verified SmartLoot group spawn endpoint; do not use one overloaded multiplier that can silently write `Weight`.

Block shipping the two masters as complete until all 19 ItemGenerator Weight-only group cases and optional NPC corpse SmartLoot package gating are proven with engine/source tests. If these cannot be gated without new semantics, expose only **truthfully narrower** masters or keep current native controls—do not introduce fake generic emptiness or new item SIDs.

**Potential alternative requiring explicit approval:** deterministic per-physical-actor selection of whole unprotected stash sources (coverage style, like existing Extra Stash Gear coverage), with a stable hash and no special actors; this would be a *container distribution policy*, not the same as native per-opening loot chance. It must not rewrite basic gameplay stash contents without a verified optional path.

### 5. Tests and acceptance evidence

Run:

```bash
python3 tools/audit_generic_loot_scarcity_gates_21.py
```

The static contract checks source and catalog row parity, **19/22** native ItemGenerator Weight/Chance split, **410/12** World SmartLoot split, **310/112** original spawn endpoint split, 30 ItemSetCount groups, 104 NPC source Chance entries and 33 existing NPC `Weight=1` pool links. It also asserts the exact Basic source-weight composition, including the invariant that uniform scaling leaves normalized shares intact.

The following are **not** established by source audit: whether a `MinSpawnChance=0,MaxSpawnChance=0` SmartLoot group really never contributes loot; whether `ItemSetCount=0` is honored universally; how the engine chooses/executes multiple ItemGenerator category groups; whether item `Chance=0` always suppresses a child and how loaded/initialized containers cache drops.

For a gameplay test, compare uninitialized ordinary ItemContainers and newly generated repeatable humans under native, 50% and 100% target profiles. Confirm same distribution *conditional on group output*, no equipped-weapon/quest side effects, no protected actor patches, and no surprise base/DLC1 source overlap. The 2.1 archive is not newly unpacked in this GitHub-only pass; counts come from the pinned tracked source manifests. This is evidence classification, not a successful .NET build or gameplay proof.
