# Standard Human NPC Loose Ammo Generator Audit

**Source:** STALKER 2 runtime/game + DLC CFG dump 2.0.5  
**Scope:** standalone `EItemGenerationCategory::Ammo` reachable from standard-human ItemGenerator loadouts; weapon-entry `AmmoMinCount/AmmoMaxCount` and Difficulty corpse SmartLoot are explicitly separate systems  
**Status:** deep-audited; no generic NPC loose-ammo control approved

## Verdict

There is **no complete isolated generator surface for a generic Standard NPC Loose Ammo Quantity control**.

Across **99 generator SIDs with a direct `human_npc_standard` consumer**, and their **145-generator composition closure**, only **26 locally authored `Ammo` rows across 2 generators** are reachable:

1. **One direct row** exists in `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog`: `A545D`, `Chance=1`, `MinCount=30`, `MaxCount=30`, gated to `ERank::Newbie, ERank::Experienced`.
2. The other **25 rows** are inside `GamePass_Stash_ItemGenerator_Rare`, reached by `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` through one `SubItemGenerator` edge with `Chance=1.0`.

The first row is exact but **Prolog-specific**, not a generic standard-NPC family. The second surface is a **shared world-stash pool**. Editing its ammo count rows for an NPC-only slider would also change world containers. Therefore `loot.looseAmmoQuantity` must remain unsupported as a generic control.

## System boundaries

This audit intentionally keeps three different ammo concepts separate:

| System | CFG surface | Meaning | This audit |
| --- | --- | --- | --- |
| Loose standalone ammo | `EItemGenerationCategory::Ammo` + `MinCount/MaxCount` | separately generated ammo item stack | **yes** |
| Weapon-attached ammo | weapon item row + `AmmoMinCount/AmmoMaxCount` | ammo field attached to the generated weapon entry | no — already audited in `NPC-WEAPONS-AMMO-AUDIT.md` |
| Extra corpse ammo SmartLoot | Difficulty `CorpseSmartLoot` -> `NPC_Ammo_Smart` | difficulty bonus package on human corpses | no — already audited in `SMARTLOOT-CORPSE-BONUS-AUDIT.md` |

## Direct standard-human `Ammo` row

| Generator | Runtime consumer | Gate | Item | Chance | Count | Raw/local line | Control verdict |
| --- | --- | --- | --- | ---: | ---: | ---: | --- |
| `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog` | `GeneralNPC_Neutral_CloseCombat_Prolog` | Newbie + Experienced | `A545D` | 1 | 30–30 | 241956 | exact local row, but Prolog-specific; **not** an all-standard-NPC target |

There is no composition or inheritance indirection behind this row, and the effective table agrees with the raw/local source. Structurally it is patchable, but its gameplay scope is too narrow to justify the existing generic label.

## Nested shared ammo path

The only other standard-human route into a loose-ammo generator is:

`GeneralNPC_Neutral_SniperCustom`  
→ `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom`  
→ `SubItemGenerator` `Chance=1.0`  
→ `GamePass_Stash_ItemGenerator_Rare`

`GamePass_Stash_ItemGenerator_Rare` contains **25 ammo rows** across the four ranks, covering **10 unique ammo SIDs**. Those rows use mixed selection fields (**2 `Chance` rows, 23 `Weight` rows**) plus per-item `MinCount/MaxCount`, so even inside the pool there is not one universal probability field.

More importantly, the pool is cross-context:

- **164 incoming composition references** target it.
- **4 inherited `Stash_Expensive_*` children** inherit it.
- Its reverse dependency graph contains **169 generator nodes**.
- That graph resolves to **348 direct consumer edges: 347 world ItemContainer edges and 1 standard-human prototype edge**.
- Those world edges represent **271 unique physical SpawnActor source IDs** in the mapped data.

Editing `GamePass_Stash_ItemGenerator_Rare` itself to tune NPC ammo would therefore be a world-stash edit as well. It is explicitly excluded from NPC-only target-definition edits.

## Shared pool ammo inventory

| Rank gate | Rows |
| --- | ---: |
| ERank::Experienced | 6 |
| ERank::Master | 8 |
| ERank::Newbie | 4 |
| ERank::Veteran | 7 |

Unique ammo SIDs: `A012A`, `A012E`, `A045A`, `A545A`, `A556A`, `A762SniperA`, `A762SniperD`, `A918A`, `A919A`, `A939A`.

## Control-safety decision

| Candidate | Structural truth | Decision |
| --- | --- | --- |
| Generic `NPC Loose Ammo Quantity` | no isolated all-standard-NPC standalone-ammo surface exists | **DO NOT IMPLEMENT** |
| Prolog neutral close-combat `A545D` count | one exact local `30–30` row | technically 1:1, but only suitable for an explicitly Prolog-specific feature |
| `GamePass_Stash_ItemGenerator_Rare` ammo counts | 25 exact rows, but shared with a large world-stash graph | **EXCLUDE from NPC-only edits** |
| Weapon `AmmoMinCount/AmmoMaxCount` | separate direct weapon-row system | use the weapons/attached-ammo audit, not this domain |
| `NPC_Ammo_Smart` corpse bonus | separate Difficulty SmartLoot package | use the corpse-bonus audit, not this domain |

## Machine-readable evidence

- `npc-loose-ammo-direct-entries.csv` — the single authoritative direct standard-human loose-ammo row.
- `npc-loose-ammo-standard-paths.csv` — the two standard-human paths that reach standalone ammo.
- `npc-loose-ammo-shared-pool-entries.csv` — all 25 exact ammo rows in the shared Rare pool.
- `npc-loose-ammo-shared-dependency-links.csv` — all 164 composition and 4 inheritance links that fan the shared pool out.
- `npc-loose-ammo-shared-blast-radius.csv` — all 348 resolved direct consumer edges in the Rare reverse-dependency closure.
- `npc-loose-ammo-domain-summary.json` — compact counts and final decision.
- `NPC-LOOSE-AMMO-ENTRY-CATALOG.md` — human-readable exact 26-row catalog.

## Final rule

A field being named `Ammo` is not enough to justify an NPC ammo slider. Patch only an explicitly scoped generator surface whose full consumer graph matches the control name. In the current source, **no generic standard-human loose-ammo surface satisfies that rule**.
