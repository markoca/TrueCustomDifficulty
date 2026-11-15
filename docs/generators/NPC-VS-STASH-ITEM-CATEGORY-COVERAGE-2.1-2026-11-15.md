# Generic NPC corpse vs Basic / Standard / Rare stash content — CFG coverage matrix

**Audit date:** 2026-11-15  
**Game source:** pinned S.T.A.L.K.E.R. **2.1** TCD baseline, with original specialist catalogs retained from 2.0.5 where their ItemGenerator domain was verified unchanged in the repository 2.1 migration notes. **Nature:** static generator/consumer coverage, not measured gameplay drop rates.

**Machine-readable source matrices:**
- [33 approved NPC generators and role/item coverage](npc-generic-33-item-category-coverage-2.1.csv)
- [Three ordinary world stash tier parents and exact SID sets](stash-basic-standard-rare-item-category-coverage-2.1.csv)
- [Generator ownership and excluded contexts](GENERIC-CORPSE-STASH-GENERATOR-OWNERSHIP-2.1-2026-11-15.md)
- Check: `python3 tools/audit_generic_npc_stash_category_coverage_21.py`.

The NPC matrix is **one row per exact approved source root** and contains the runtime consumer, role, 121 local weapon rows, possible weapon SIDs, direct grenade fields split into **Chance versus Weight**, direct armor/head and detector rows, exact nested armor/NVG/Rare-stash links, plus Medical/Food/Drinks SID lists from the 104-row source manifest. Its rows are **not** a promise that all possible equipment or consumables appear on the same corpse; difficulty/rank/selection gates and game execution remain independent. The stash matrix lists original **ItemGenerator** tier parent contents only, without wrongly merging the separate SmartLoot overlay or opt-in Stash Gear feature.

## 1. Ordinary-human loadout matrix — 33 approved roots

| Consumable role | Source loadout roots | Direct weapon entries | Direct grenade entries | Direct BodyArmor entries | Linked armor pool edges |
| --- | ---: | ---: | ---: | ---: | ---: |
| CloseCombat | 6 | 17 | 0 | 10 | 5 |
| Heavy | 4 | 5 | 0 | 0 | 4 |
| Noon | 4 | 11 | 6 | 0 | 4 |
| Recon | 6 | 11 | 12 | 1 | 5 |
| Sniper | 7 | 38 | 1 | 20 | 5 |
| Stormtrooper | 6 | 39 | 15 | 10 | 5 |
| **Total** | **33** | **121** | **34** | **41** | **28** |

Those 121 weapon entries contain a matched set of local `MinDurability/MaxDurability` and **weapon-attached** `AmmoMinCount/AmmoMaxCount` fields; three source records spell the upper field `AmmoMaxcount` and must retain that casing. **Weapon condition is already implemented. Weapon-attached ammo quantity is not:** the current **BONUS AMMO QTY** slider instead controls 43 `NPC_Ammo_Smart` bonus-package stacks and only when the MED + AMMO package is selected.

In the approved 33-generator subset, the direct grenade sources split into **32 `Chance` entries plus two `Weight` entries**; the Weight-only sources are `GuardNPC_Noon_Recon_ItemGenerator` and `GuardNPC_Varta_Stormtrooper_ItemGenerator`. All these grenade items are RGD-5/F1 even when the native CFG enum is `EItemGenerationCategory::Artifact`. This is **not** an anomaly-artifact drop surface. **Do not implement one universal NPC grenade Chance slider** from only the 32 rows.

The loadout table contains **41 direct BodyArmor rows**, **five direct Head rows**, and **28 armor-pool link edges**. These linked target pools can serve named, quest or zombie contexts; editing their shared *definitions* is not an NPC-only change. The direct local rows mainly use `Weight`, not uniform drop `Chance`. One approved normal-human generator links to the shared `GeneralNPC_Neutral_NVG` pool. NVG content is a **shared descendant**, not an independently validated generic loot control.

There are also **32 original Echo detector rows** among these 33 roots; the only approved root without such a row is `GuardNPC_Duty_Stormtrooper_ItemGenerator`. **Echo Detector Chance is explicitly excluded from TCD controls by user decision.** This row count is audit/provenance only, not a future UI candidate.

### Normal NPC consumables

Exactly six role pools feed the 33 approved roots. All **104 native Chance rows** belong to the ten SID types below; only the matching item/role/difficulty/rank combinations are eligible.

| Category | Vanilla possible SIDs | Source Chance entries | TCD coverage |
| --- | --- | ---: | --- |
| Medical | `Bandage`, `Medkit`, `ArmyMedkit`, `AntiRad` | **58** | Global STANDARD MED 0–2× source scalar and four opt-in individual absolute 0–100% SID Chance rails |
| Food | `Bread`, `Sausage`, `CannedFood` | **26** | Three individual opt-in absolute Chance rails |
| Drinks / stimulants | `Energetic`, `Beer`, `Vodka` | **20** | Three individual opt-in absolute Chance rails |

Medical/Food/Drinks **all have usable audited NPC Chance controls today**. The six vanilla consumable pool definitions are **shared** and must be copied into TCD private variants, then reached via only the approved 33 source redirects. An enabled item's 100% writes `Chance = 1` *at an eligible row*, not a blanket guarantee for every corpse.

**Water and Milk are not part of the six standard-human consumable pools**. Water in human corpses instead has a distinct **optional `NPC_Water_Smart` package**. The other two exact package SIDs are `NPC_Medicine_Smart` and `NPC_Ammo_Smart`; this selected-difficulty list is not the same as direct NPC loadout generation.

## 2. Original ordinary-world stash tiers — exact source differences

These three sources contain **41** rank/difficulty-gated ItemGenerator groups and **52 / 39 / 42 authored item rows** in Cheap / Medium / Rare respectively (source inventory), with **16 / 15 / 21 distinct collectible SIDs**. No original parent tier in the classified table directly authors weapons or body armor; optional TCD **Extra Stash Gear** is a separately controlled feature and is not native parent-tier content.

| Native content category | Basic / Cheap | Standard / Medium | Rare / Expensive |
| --- | --- | --- | --- |
| Ammunition | Three basic SIDs: `A012D`, `A918D`, `A919D` | Eight ammo SIDs (including `A545D`, `A556D`, `A762Sniper*`) | Ten higher-grade ammo SIDs |
| Medical | Bandage, Medkit | Bandage, Medkit | Bandage, Medkit, Army Medkit, AntiRad |
| Food | Bread, Sausage, Canned Food | **None in this original parent** | Canned Food |
| Drinks/stimulants | Water, Milk, Vodka, Energy Drink | Vodka, Energy Drink | Energy Drink |
| Grenades | RGD-5 | **None in this original parent** | RGD-5 |
| Coupons | Common, Rare, Epic | Common, Rare, Epic | Common, Rare, Epic, Legendary |
| Native weapons, armor | None | None | None |

The **absence** of food/grenades in the Standard parent does **not** mean a particular physical Standard stash cannot acquire those items through **independent `StashPrototypeSID` SmartLoot, direct authored entries, mission/quest logic or another source**. Never treat that cell as a total physical-container exclusion. `Weight` selection, `Chance` rolls and count fields have separate semantics. An eligible source row is not an unconditional award.

Stash controls have an existing six-category **native Weight mixer** (Ammo, Medical, Food, Drinks, Grenades, Coupons), plus existing tier-native Chance and medical/quantity controls where source fields justify them. Positive multipliers on all competing `Weight` entries do not necessarily alter selection probability; reducing `Weight` to zero does not kill independent positive `Chance` entries.

### World SmartLoot remains a separate layer

The 13 world stash prototypes (seven ammo, two medical, two food/drinks, two grenade) hold **422 group rows / 991 item rows**. Their production source graph has **2,143** SmartLoot consumer edges, with **2,105** of those also carrying ItemGenerator wiring. Treat this as a parallel source/overlay, **not an extra 422 normal ItemGenerator groups**. A correct world-stash category control must preserve rank, player weapon/caliber and the independent group `MinSpawnChance/MaxSpawnChance`, `ItemSetCount`, item `Weight/Chance` and quantity behavior.

**Shared contamination edge:**

```text
GeneralNPC_Neutral_SniperCustom
  -> GeneralNPC_Neutral_Sniper_ItemGeneratorCustom
     -> GamePass_Stash_ItemGenerator_Rare  <-- shared with world containers
```

This creates one narrow NPC-to-stash cross-reference. The source parent has many world consumers, so an NPC-specific or stash-specific change must patch a private clone and only appropriately allowed consumer edges; **never mutate the shared original parent as though it belonged solely to one domain**.

## 3. What existing TCD controls already cover, and the actual gaps

| Domain | Existing, source-backed TCD control | Remaining behavior / recommended decision |
| --- | --- | --- |
| NPC weapon condition | DROPPED WEAPON CONDITION MIN/MAX, 121 local rows | Implemented; no new UI needed |
| NPC ammunition | BONUS AMMO QTY, optional 43-row corpse SmartLoot clone | **Weapon-attached ammo counts** on 121 weapon rows are a real separate uncovered domain; precise writer is feasible, in-game result unverified; **test before implementation** |
| NPC food, drinks, medical | Ten opt-in SID-specific literal-Chance rails, plus STANDARD MED | Already covered; don't invent additional global corpse drop chance |
| NPC weapon composition | None (117 Weight + 4 Chance authored rows) | Mixed native fields; defer, not an absolute weapon-drop chance |
| NPC grenades | No generic Chance editor | 32 Chance + 2 Weight; **do not implement a misleading universal percentage** |
| NPC armor/helmet | No simple generic armor-drop chance | 41 direct armor + 5 head rows and 28 shared armor links; requires scoped clones or carefully defined local policies |
| NPC NVG / detectors | No generic NVG control; Echo explicitly excluded | Shared NVG pool requires new scoped runtime evidence; **never introduce Echo Chance** |
| NPC standalone loose ammo | No all-human native isolated amount surface | One Prologue-only source plus 25 rows in the shared Rare stash pool; **do not implement generic loose NPC ammo** |
| Basic/Standard/Rare stash categories | Six-category Weight mixer, independent reward and medical/quantity controls, guarded ItemGenerator/SmartLoot edits | Covered only on eligible, source-audited consumers; native Chance is not necessarily removed by zero Weight |
| Stash weapons/armor/artifacts | Extra Stash Gear opt-in, separately protected world/quest scope | These are **not** original Basic/Standard/Rare tier parent content; don't misattribute bonus slots to normal stash parents |
| Mod-added items | Separate opt-in corpse/world MOD ITEMS audited PAK controls | Only recognized source-hash allowlisted mod generator branches; no automatic coverage of unknown mods |
| Quest/named/physical scripted corpses | Not a generic editing surface | Keep excluded, including 99 separate `CorpseStashSID` source edges into 39 direct roots |

### Priority and scope lock

The **strongest technically coherent next investigation** is the 121-row **weapon-attached ammunition amount**, with its own label clearly distinguished from **BONUS AMMO QTY**. If confirmed through fresh NPC spawning and corpse/weapon inventory inspection, it can be added as an independent compact control. Its writer must retain the original `AmmoMaxcount` spelling on three rows, preserve original zero bounds and integer rounding, and never change SmartLoot or world stash ammo.

NPC grenade frequency, armor drop probability, universal NPC loose ammo, Echo detectors and a broad whole-corpse abundance percentage are **not approved** by this source inventory. The existing 33-root ordinary-human boundary and protected stash actor allowlists remain unchanged.

## 4. Static verification and limitations

Run `python3 tools/audit_generic_npc_stash_category_coverage_21.py` after a pull. The checker verifies all 33 source roots, each root's original weapon rows and consumable SID sets, the 34 native grenade field kinds, direct equipment and nested source edges, and the three exact stash parent SID sets against the retained original inventories.

**Evidence limitation:** parts of the specialist NPC generator catalogs were originally extracted from the 2.0.5 dump and carried forward because the repository's 2.1 domain audit found those original `ItemGeneratorPrototypes.cfg` routes unchanged. This is not fresh 2.1 binary re-extraction or engine runtime acceptance. New local builds, Wine display and controlled in-game loot trials are still needed for any new gameplay slider.
