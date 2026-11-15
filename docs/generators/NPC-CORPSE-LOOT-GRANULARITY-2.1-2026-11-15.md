# NPC corpse loot — supported granularity and safe next controls (S.T.A.L.K.E.R. 2.1)

**Date:** 2026-11-15  
**Purpose:** enumerate the exact NPC loot writer surfaces that can be tuned without accidentally modifying unrelated world stashes, quest inventories, named NPCs or zombies.

## Evidence boundary

This is a **static source/writer audit**, not an assertion that every configured item visibly drops in the game. Reconciled the current `main` production `cfg-writer-baseline.json` (declares `gameVersion: 2.1`), `parameters.json`, `CfgPatchWriterService.cs`, NPC medical redirect manifest, and retained generator audits:

- `NPC-CONSUMABLES-PRACTICAL-AUDIT.md`, `NPC-EQUIPMENT-ARTIFACTS-AUDIT.md`, `NPC-WEAPONS-AMMO-PRACTICAL-AUDIT.md`, `NPC-LOOSE-AMMO-AUDIT.md`, `NPC-JUNK-MISC-AUDIT.md`, `HUMAN-NPC-GRENADE-SEMANTICS-AUDIT.md`, `CORPSE-OVERRIDE-AUDIT.md`, `SMARTLOOT-CORPSE-BONUS-AUDIT.md`, `NPC-CORPSE-INGAME-LOOT-TYPE-VALIDATION-2026-10-16.md`.
- The retained source-diff in `STALKER2-2.1-CFG-DOMAIN-AUDIT-2026-11-14.md` reports that the native `ItemGeneratorPrototypes.cfg` used by these direct NPC routes did not change between the previous source archive and 2.1. **The 2.1 original ZIP was not newly re-extracted by this audit**; specialist research inventories originate in the older 2.0/2.0.5 dump and therefore remain conditionally 2.1-compatible, not independently re-certified.
- `Chance` (per-entry roll), `Weight` (relative selection), `MinCount/MaxCount` (quantity), `MinDurability/MaxDurability` (weapon condition), `AmmoMinCount/AmmoMaxCount` (weapon-attached ammunition), optional corpse SmartLoot lists and physical `CorpseStashSID` override targets **are not interchangeable**.

## Five separate runtime-producing paths

1. **Standard repeatable-human ItemGenerator weapon/equipment entries** in `GameData/ItemGeneratorPrototypes.cfg`. The well-defined, scoped production subset includes 33 generator roots / 121 weapon item rows. Item identity, relative selection Weight/Chance, per-weapon durability and attached ammunition are independently authored at the row level.
2. **Standard consumables:** six role pools `GeneralNPC_Consumables_*` with 104 item rows; medical `Chance` appears on 58 of those. The pools themselves are *shared across other contexts*; safe edits need TCD private copies and redirection of precisely 33 repeatable-human links (existing mechanism). The other 46 rows cover 26 food + 12 stimulants/utility + 8 alcohol entries. The exact per-role item sets, rank gates and item-level Chance are available in the retained audits.
3. **Optional difficulty CorpseSmartLoot:** one `EconomyDifficulty.CorpseSmartLoot.GeneralNPC` list in the *selected* `DifficultyPrototypes.cfg` profile. Native: Novice = `NPC_Medicine_Smart, NPC_Ammo_Smart`; Stalker = `NPC_Water_Smart`; Veteran/Master = none. The package selection can be deliberately changed on any selected Base Difficulty. These packages carry 13 medicine, 43 bonus-ammo and 4 water row entries across four progression-rank branches. Changing a package does not edit the standard consumables route.
4. **Physical/scripted corpse overrides:** 99 `CorpseStashSID` DeadBody source edges (57 base + 42 DLC1), targeting 39 ItemGenerator roots; 31 of 39 roots are used elsewhere and **must not** be globally altered for an ordinary-corpse rule. Eight are corpse-only, but include scripted/body-specific inventories: they do **not** form a uniform global drop pool.
5. **Audited third-party mod additions:** separate mod-added generator branches controlled by the opt-in corpse MOD ITEMS sidecar, currently only the exact allowlisted PAK families. Not equivalent to original game consumable, weapon or SmartLoot generators.

## Current editor and source exactness

| Current UI | Direct edit scope | Distinct limitation |
| --- | --- | --- |
| Extra Corpse Loot | One selected-difficulty list of optional SmartLoot package SIDs | Package choice, **not** a percentage or guarantee |
| NPC Medical Drops | Six private consumable pool clones, 58 original `Chance` rows, 33 repeatable-human redirects | Affects standard medical Chance; separate optional bonus medicine can still appear |
| Bonus Ammo Drops | One `TCD_NPC_Ammo_Smart` clone, **43** item-count pairs across **24** caliber/rank groups | Enabled only with Medicine + Ammo; not weapon-attached ammo |
| Dropped Weapon Condition | **242** `MinDurability/MaxDurability` edit leaves on **121** local weapon rows in **33** repeatable-human generators | Edits weapon condition, not whether a weapon is generated; one 35–45% source outlier |
| Corpse MOD ITEMS | Allowlisted mod branch Chance reductions only | Disabled by default; no unsupported mod or native item edits |

## Prospective granularity, ranked by safety

| Potential future control | CFG evidence / precise breadth | Verdict |
| --- | --- | --- |
| **NPC Detector Drops** (only Echo) | 36 isolated direct `Echo` rows; 30 original Chance=0.20 and 6 Chance=0.05, MinCount=1 | **Excluded from the UI by explicit user decision (2026-11-15).** Retain as historical CFG research only; no control or writer |
| **Standard medical by item** (Bandage / Medkit / ArmyMedkit / AntiRad) | Split existing 58 medical Chance rows by exact SID inside six private source-copied pools | **Feasible extension** of current audited clone/redirection route; one independent percentage per medical item (not global corpse medical ban) |
| **Standard consumables by item/category** | 26 food, 12 stimulants/utility, 8 alcohol, all with item-level Chance; 104 overall, 84 fixed 1–1 counts | **Technically feasible** with existing six private pools + 33 redirects; prefer Chance, not quantity. More detailed faction/role gates need explicit consumer validation |
| **Medical Chance by NPC role** | Six role pools: CloseCombat, Heavy, Noon, Recon, Sniper, Stormtrooper | **Feasible scoped prototype granularity**: six independently changed clone groups, not global faction frequency |
| **Medical Chance by difficulty × player rank** | Four native pool branches: non-Master lower rank, non-Master higher rank, Master lower rank, Master higher rank | **Possible** via branch-aware Chance edits while retaining every original `Diff`/rank guard. Two axes; do not assume 4 difficulties × 4 independent ranks are authored as 16 distinct blocks |
| **NPC weapon-attached ammo counts** | 121 original local weapon-row `AmmoMinCount` and `AmmoMaxCount` leaves, including 3 source `AmmoMaxcount` spelling exceptions | **Structurally feasible** separate control; never conflate with optional SmartLoot or loose ammo. Need gameplay proof of condition/weapon inventory outcome |
| **Individual NPC weapon loadout composition** | 121 local weapon rows / 37 weapon SIDs / 33 generator roots; 117 source Weight rows and 4 Chance rows | **Advanced, conditional**; exact local Weight/Chance transformations possible, but “weapon drop probability” is NOT one universal scalar |
| **NPC grenade frequency** | 44 direct rows: 36 active Chance, six disabled Prologue Chance=0, two legitimate repeatable Weight-only sources | **Do not offer one generic Chance slider**; mixed semantics and hard Prologue exclusions |
| **Armor and helmet drops** | 43 direct BodyArmor and six Head rows, mostly relative Weight; many nested faction pools shared with named NPCs, zombies and quests | **Not safe as a global Chance slider**. Direct weight tuning or privately cloned faction pools require deliberate scope-and-consumer design |
| **NPC NVG frequency** | Five links to two nested NVG pools shared across multiple contexts | **Requires private clone and source-edge redirects**, and evidence that changing the inclusion edge affects player-lootable NVGs |
| **Anomaly artifacts on ordinary NPCs** | No direct anomaly-artifact item rows in the audited normal human surface; the misleading CFG `Artifact` enum here denotes RGD-5/F1 grenades | **Not a native scalar**; would require optional explicitly new private generator with verified actor scope |
| **Generic loose NPC ammo** | One Prologue-only `A545D` row + 25 rows in `GamePass_Stash_ItemGenerator_Rare`, shared with 347 world-container consumer edges | **Do not edit shared pool**; no complete isolated NPC-only loose-ammo scalar |
| **NPC Junk/Misc** | 35 standard-human `GuardQuestItem` rows, invisible context-action marker, not loot | **Exclude completely** |
| **All-corpse generic loot abundance** | 99 separate physical corpse override edges / 39 roots, 31 cross-context | **Not supported** as one truthful percentage |

## Practical proposed design (not implemented by this audit)

The current UI consolidates SmartLoot package, Bonus Ammo Qty, Standard Med and Mod Items into a single horizontal strip (two pairs when the host is narrow); no separate Echo slider is allowed. If new controls are desired, keep the existing compact **NPC LOOT & EQUIPMENT** board rather than introducing a wall of controls:

1. **No NPC DETECTORS / Echo Chance control.** The user explicitly excluded it; the 36 source rows remain audit evidence only.
2. An optional compact **STANDARD MEDICAL MIX** (Bandage / Medkit / Army Medkit / Anti-Rad) using exact Chance scaling, not a new global “corpse has medical item” probability. This would refine (not double-scale) the existing NPC Medical Drops control, so its precedence/preset migration must be explicitly defined before implementation.
3. If needed, **WEAPON AMMO** as a distinctly named *attached ammo* quantity editor, not another bonus-ammo slider.

Preserve the original `EXTRA CORPSE LOOT` package chooser on **all** Base Difficulties: it writes only the *currently selected* difficulty's subtree, and changing Veteran's native None to Medicine+Ammo is an existing valid custom-difficulty workflow. A difference in native default value is **not** proof that the editor itself is allowed on one Base Difficulty only. Only mechanics specifically authored as Master-exclusive are subject to the new difficulty-dependent UI visibility rule.

## Tests still required

The source-driven scopes establish plausible CFG patch targets, **not guaranteed on-screen loot**. Validate on newly generated ordinary human bodies, separately identifying standard ItemGenerator consumables, carried weapons/attached ammo, optional SmartLoot, mod-added items and named/scripted corpse overrides. Keep the current 32-case `npc-corpse-ingame-loot-test-matrix.csv` as the base and add separate cases for each new candidate before shipping it.
