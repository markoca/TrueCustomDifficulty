> **v0.2.16 correction:** the earlier 12-root "parent-backed WeaponPrimary" interpretation was wrong. The resolved child catalog contains no inherited parent `WeaponPrimary` rows. The actual secondary weapon path is 85 local links to 12 shared `*_WeaponPistol` pools. See `GENERIC-ZOMBIE-WEAPON-SOURCE-AUDIT.md`.

# Zombie NPC Loadout Generator Audit
**Source:** STALKER 2 runtime/game + DLC CFG dump 2.0.5  
**Scope:** every generator classified under `ZOMBIE NPC LOADOUTS`, with raw/local source rows checked directly against `GameData/ItemGeneratorPrototypes.cfg` and `DLCGameData/DLC1/ItemGeneratorPrototypes.cfg`  
**Status:** deep-audited; no runtime writer behavior changed
## Verdict
The zombie domain contains **114 mapped generators**, but the earlier structural allowlist was too broad. Only the **93 generic zombie child loadouts** — **43 `GeneralZombie_*` base-game generators** and **50 `CNPPZombie_*` DLC1 generators** — remain generic zombie candidates after this deep audit. Named zombies, scripted mission zombies, the Red Forest special zombie and its helper/key pools are no longer treated as generic tuning candidates.
Those 93 generic child generators contain **1882 locally authored source rows**. Because they are child-local and their observed external impact is zombie-only, exact edits to their own local rows can be structurally isolated. However, many of those child generators call shared `GeneralNPC_*` armor, pistol, NVG and consumable pools. **Editing the shared target pool definitions is not zombie-only** and remains excluded from generic zombie tuning.
The audit also exposed an important data-layer distinction: `generator-item-entries.csv` is an **effective/inheritance-resolved analysis layer**, not a byte-for-byte source-row table. For patch design, the new `generator-local-source-*.csv` files are authoritative because they record exactly what is locally written in the runtime CFG text. Effective/reachable tables remain useful for understanding inheritance and blast radius.
## Family boundary
| Family | Generators | Generic-control policy | Why |
| --- | ---: | --- | --- |
| `GeneralZombie_*` base generic loadouts | 43 | **CANDIDATE** | Zombie-only child generators; use exact child-local rows only. |
| `CNPPZombie_*` DLC1 generic loadouts | 50 | **CANDIDATE WITH SOURCE ANOMALIES** | Zombie-only child generators, but DLC1 source contains mixed-field consumable entries documented below. |
| Named `Zombie*` NPC generators | 5 | **EXCLUDE** | Hand-authored individual zombie NPC inventories; not generic population balance. |
| E03 / DLC01 MQ06 scripted zombie generators | 9 | **EXCLUDE** | Mission/scripted inventory semantics. |
| `RC_Military_Zombie_RedForest_ItemGenerator` | 1 | **EXCLUDE** | Bespoke Red Forest loadout; pulls general ammo/consumable/detector/attachment pools and a quest key. |
| `KeyGeneratorRedForest` | 1 | **EXCLUDE** | Generates `RedForest_Leshyna_Key_DoorNew`; system/quest dependency. |
| `RU*WeaponExperiencedGenerator` helper pools | 5 | **REVIEW / EXCLUDE FROM GENERIC** | Used only by the bespoke Red Forest zombie path; not a general zombie family. |

## Generic zombie local-row domains
| Local domain | Rows | Generators | Source field shape | Decision |
| --- | ---: | ---: | --- | --- |
| Primary weapons / attached ammo | 791 | 81 | Every row has `Weight`, `MinDurability`, `MaxDurability`, `AmmoMinCount`, `AmmoMaxCount`; no `Chance`. | **SAFE STRUCTURAL CANDIDATE** for exact child-local durability/ammo edits; selection weights remain a separate mechanic. |
| Detector | 93 | 93 | All generate `Echo`; all use explicit `Chance=0.05` and `MinCount=1`. | **SOURCE-MAPPED / NO UI** — structurally clean, but not a meaningful player-facing zombie loot control. |
| Grenades (CFG `Artifact`) | 91 | 91 | `GrenadeRGD5` / `GrenadeF1`; explicit `Chance` on every row. | **SAFE STRUCTURAL CANDIDATE FOR `Chance` ONLY**; preserve every other source field. |
| Direct body armor | 50 | 5 | All use `Weight`; only Neutral zombie families author direct armor rows. | **REVIEW** — weighted selection, not a drop-probability scalar. |
| Direct head/helmet | 10 | 5 | `Light_Neutral_Helmet`; explicit `Chance` split by rank. | **REVIEW** — Neutral-only helmet branch, not a universal zombie helmet mechanic. |
| Direct consumables | 503 | 50 | DLC1/CNPP only; explicit `Chance`/counts, but many rows contain extra weapon/pool fields. | **REVIEW / SOURCE-ANOMALY BOUNDARY** — cannot be generalized to base zombies. |
| Shared sub-generator links | 344 | 93 | Links to living `GeneralNPC_*` consumable, armor, pistol and NVG pools. | **DO NOT EDIT TARGET DEFINITIONS FOR ZOMBIE-ONLY TUNING**. |

## Direct weapons and attached ammo
Across **81 of the 93 generic zombie roots** there are **791 local `WeaponPrimary` rows**. Separately, **85 roots** carry a local `SubItemGenerator` link to one of **12 shared `GeneralNPC_*_WeaponPistol` pools** containing **71 pistol weapon rows**. The topology is 75 roots with both sources, 6 primary-only, 10 pistol-only and 2 with no weapon source.

All 791 local-primary rows and all 71 pistol-pool rows use `MinDurability=0.25` / `MaxDurability=0.5`; attached ammo ranges vary by weapon/rank/difficulty. The old assumption that 12 CNPP roots inherited their human parent `WeaponPrimary` branches is incorrect: the resolved child catalog contains no such inherited rows. Future zombie-only tuning must therefore patch the 791 local rows and clone/redirect the 12 shared pistol pools through the 85 zombie links, while leaving the two weaponless Scientist roots untouched.
`Weight` is deliberately **not** treated as a generic 'weapon drop chance'. It is selection weight inside a branch. A future weapon rarity/composition control needs its own design and must preserve the existing rank/difficulty branch structure.
## Detectors
All **93 generic zombie detector rows** generate `Echo` with `Chance=0.05` and `MinCount=1`. Structurally this is a clean direct surface: one local row per generic zombie loadout, with no shared detector definition required for the normal generic families. However, the project intentionally does **not** expose or patch it: zombie corpses are not treated as a meaningful detector-loot source in normal gameplay, so the source rows remain documentation-only.
## Grenades
There are **91 child-local grenade rows**: **71 `GrenadeRGD5`** and **20 `GrenadeF1`**. All have explicit `Chance`; the observed chances are `0.2` × 89, `0.4` × 2.
Four source rows also carry durability/ammo-style fields. Those fields are present in the raw CFG and are therefore preserved, but the audit does not infer their runtime meaning for grenades. The v0.2.17 Zombie Grenade Frequency control alters only the explicit `Chance` field and leaves those other fields untouched.
## Consumables: base vs CNPP are different systems
Base-game `GeneralZombie_*` loadouts do **not** locally author consumable item rows. Instead, every base generic zombie calls one of the shared `GeneralNPC_Consumables_*` pools through a local `SubItemGenerator` link. Those same pools are used by living standard NPCs, named NPCs, quests and/or corpse paths, so editing the pool definition is not zombie-only.
DLC1 `CNPPZombie_*` loadouts are different: all 50 author direct `Consumable` branches (**503 local rows total**) **and** most still link shared `GeneralNPC_Consumables_*` pools. This means a single 'Zombie consumables' slider cannot honestly be implemented by one transform shared between base and CNPP.
The CNPP source also contains **216 consumable rows with weapon-like fields** (`Weight`, durability and/or attached-ammo fields), and **11 rows that contain both an `ItemPrototypeSID` and an `ItemGeneratorPrototypeSID` in the same entry**. These are raw CFG facts, not parser artifacts. `zombie-source-anomalies.csv` enumerates every affected row. No future consumable writer should normalize or delete those extra fields unless their engine behavior is separately proven.
## Shared armor / pistol / NVG / consumable pools
The generic zombie child generators reach living-NPC `GeneralNPC_*` pools through local links. The link itself is zombie-local, but the **target generator definition is shared**. Observed shared target families are armor, pistol, NVG and consumables. Their impact graphs include living standard NPCs and commonly named/scripted/quest/corpse contexts as well.
Therefore:

- editing a zombie child generator's own link field can be considered only after `Chance`/`Weight` semantics are proven;
- editing the referenced `GeneralNPC_*` pool definition is **not allowed for a zombie-only control**;
- if a future control must alter item-level contents of those pools only for zombies, the safe architecture is clone + rewire, not shared-pool mutation.
## Special and scripted zombie paths
The 5 named `Zombie*` inventories and the 9 E03/DLC01 mission zombie generators are now explicitly excluded from generic zombie tuning. `RC_Military_Zombie_RedForest_ItemGenerator` is also excluded: it composes general ammo, five RU weapon pools, consumables, detector, attachments and `KeyGeneratorRedForest`; the latter emits `RedForest_Leshyna_Key_DoorNew`. This is a bespoke authored path, not a generic zombie loot family.
## Control-safety conclusions
| Possible future control | Status after audit | Exact boundary |
| --- | --- | --- |
| Generic Zombie Weapon Condition | **NO UI / AUDIT ONLY** | Fully mapped source boundary retained for documentation; not exposed because it tunes loot/loadout outcomes rather than core difficulty. |
| Generic Zombie Attached Ammo | **NO UI / AUDIT ONLY** | Fully mapped source boundary retained for documentation; not exposed as a normal difficulty control. |
| Generic Zombie Detector Chance | **NO UI — NO OBSERVABLE LOOT VALUE** | 93 child-local `Echo` rows exist and are structurally clean, but detectors are not a meaningful zombie-corpse loot outcome; keep source-only. |
| Generic Zombie Grenade Frequency | **IMPLEMENTED / RUNTIME TEST REQUIRED** | v0.2.17 scales the 91 child-local grenade `Chance` rows only, preserves every other field, and does not synthesize grenade branches for the two roots that have none. |
| Generic Zombie Weapon Selection / Rarity | **DESIGN REQUIRED** | Uses branch `Weight`; exact probability semantics not assumed. |
| Generic Zombie Armor Chance | **NOT A 1:1 CONTROL** | Armor is mostly shared-pool selection; direct Neutral rows use `Weight`. |
| Generic Zombie Pistol/NVG contents | **NOT SAFE VIA SHARED POOL EDITS** | Referenced `GeneralNPC_*` pools are cross-context. |
| Generic Zombie Consumable Chance/Count | **NOT YET ONE CONTROL** | Base uses shared pools; CNPP has direct rows + shared links + source anomalies. |
| Named/scripted/special zombie loot | **EXCLUDED FROM GENERIC TUNING** | Kept authored unless a dedicated feature is designed. |

## Raw/local versus effective tables
Use the tables as follows:

- `generator-local-source-inventory.csv` / `generator-local-source-category-blocks.csv` / `generator-local-source-item-entries.csv`: **exact locally authored CFG rows**; authoritative for patch targets.
- `generator-inventory.csv` / `generator-category-blocks.csv` / `generator-item-entries.csv`: effective/inheritance-resolved analysis layer; useful for understanding what a generator may inherit/reach, but not sufficient alone to prove which fields are physically authored in the child block.
- `GENERATOR-CATALOG.md` and `GENERATOR-BRANCH-CATALOG.md`: readable graph/reachability views.
- `ZOMBIE-ENTRY-CATALOG.md`: complete zombie-domain raw/local row table.
## Generator family table
| Generator | Scope | Family | Consumer(s) | Parent | Local rows | Reachable items | Final policy |
| --- | --- | --- | --- | --- | ---: | ---: | --- |
| `CNPPZombie_Bandit_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Bandit_CloseCombat | `GeneralNPC_Bandit_CloseCombat_ItemGenerator` | 22 | 22 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Bandit_Heavy_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Bandit_Heavy | `GeneralNPC_Bandit_Heavy_ItemGenerator` | 22 | 20 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Bandit_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Bandit_Recon | `GeneralNPC_Bandit_Recon_ItemGenerator` | 41 | 22 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Bandit_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Bandit_Stormtrooper | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | 21 | 20 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Corpus_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Corpus_CloseCombat | `GeneralNPC_Corpus_CloseCombat_ItemGenerator` | 16 | 22 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Corpus_Heavy_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Corpus_Heavy | `GeneralNPC_Corpus_Heavy_ItemGenerator` | 16 | 21 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Corpus_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Corpus_Recon | `GeneralNPC_Corpus_Recon_ItemGenerator` | 16 | 21 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Corpus_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Corpus_Sniper | `GeneralNPC_Corpus_Sniper_ItemGenerator` | 16 | 21 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Corpus_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Corpus_Stormtrooper | `GeneralNPC_Corpus_Stormtrooper_ItemGenerator` | 16 | 22 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Duty_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Duty_CloseCombat | `GeneralNPC_Duty_CloseCombat_ItemGenerator` | 21 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Duty_Heavy_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Duty_Heavy | `GeneralNPC_Duty_Heavy_ItemGenerator` | 22 | 27 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Duty_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Duty_Recon | `GeneralNPC_Duty_Recon_ItemGenerator` | 18 | 24 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Duty_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Duty_Sniper | `GeneralNPC_Duty_Sniper_ItemGenerator` | 24 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Duty_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Duty_Stormtrooper | `GeneralNPC_Duty_Stormtrooper_ItemGenerator` | 26 | 27 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Freedom_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Freedom_CloseCombat | `GeneralNPC_Freedom_CloseCombat_ItemGenerator` | 24 | 26 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Freedom_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Freedom_Recon | `GeneralNPC_Freedom_Recon_ItemGenerator` | 24 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Freedom_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Freedom_Sniper | `GeneralNPC_Freedom_Sniper_ItemGenerator` | 24 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Freedom_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Freedom_Stormtrooper | `GeneralNPC_Freedom_Stormtrooper_ItemGenerator` | 25 | 27 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Mercenaries_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Mercenary_CloseCombat | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | 22 | 24 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Mercenaries_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Mercenary_Recon | `GeneralNPC_Mercenaries_Recon_ItemGenerator` | 25 | 23 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Mercenaries_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Mercenary_Sniper | `GeneralNPC_Mercenaries_Sniper_ItemGenerator` | 22 | 23 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Mercenaries_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Mercenary_Stormtrooper | `GeneralNPC_Mercenaries_Stormtrooper_ItemGenerator` | 21 | 26 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Militaries_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Military_CloseCombat | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | 18 | 24 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Militaries_Heavy_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Military_Heavy | `GeneralNPC_Militaries_Heavy_ItemGenerator` | 28 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Militaries_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Military_Recon | `GeneralNPC_Militaries_Recon_ItemGenerator` | 26 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Militaries_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Military_Sniper | `GeneralNPC_Militaries_Sniper_ItemGenerator` | 20 | 23 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Militaries_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Military_Stormtrooper | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | 30 | 27 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Monolith_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Monolith_CloseCombat | `GeneralNPC_Monolith_CloseCombat_ItemGenerator` | 26 | 23 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Monolith_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Monolith_Recon | `GeneralNPC_Monolith_Recon_ItemGenerator` | 21 | 24 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Monolith_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Monolith_Sniper | `GeneralNPC_Monolith_Sniper_ItemGenerator` | 30 | 26 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Monolith_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Monolith_Stormtrooper | `GeneralNPC_Monolith_Stormtrooper_ItemGenerator` | 22 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Neutral_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Neutral_CloseCombat | `GeneralNPC_Neutral_CloseCombat_ItemGenerator` | 37 | 30 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Neutral_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Neutral_Recon | `GeneralNPC_Neutral_Recon_ItemGenerator` | 52 | 30 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Neutral_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Neutral_Sniper | `GeneralNPC_Neutral_Sniper_ItemGenerator` | 45 | 33 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Neutral_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Neutral_Stormtrooper | `GeneralNPC_Neutral_Stormtrooper_ItemGenerator` | 41 | 34 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Noon_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Noon_CloseCombat | `GeneralNPC_Noon_CloseCombat_ItemGenerator` | 15 | 18 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Noon_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Noon_Recon | `GeneralNPC_Noon_Recon_ItemGenerator` | 15 | 18 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Noon_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Noon_Sniper | `GeneralNPC_Noon_Sniper_ItemGenerator` | 15 | 18 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Noon_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Noon_Stormtrooper | `GeneralNPC_Noon_Stormtrooper_ItemGenerator` | 15 | 18 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Scientists_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Scientist_Recon | `GeneralNPC_Scientists_Recon_ItemGenerator` | 12 | 13 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Scientists_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Scientist_Stormtrooper | `GeneralNPC_Scientists_Stormtrooper_ItemGenerator` | 13 | 15 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Spark_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Spark_CloseCombat | `GeneralNPC_Spark_CloseCombat_ItemGenerator` | 19 | 27 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Spark_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Spark_Recon | `GeneralNPC_Spark_Recon_ItemGenerator` | 16 | 23 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Spark_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Spark_Sniper | `GeneralNPC_Spark_Sniper_ItemGenerator` | 26 | 27 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Spark_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Spark_Stormtrooper | `GeneralNPC_Spark_Stormtrooper_ItemGenerator` | 20 | 28 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Varta_CloseCombat_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Varta_CloseCombat \| VartaZombieVeres | `GeneralNPC_Varta_CloseCombat_ItemGenerator` | 23 | 23 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Varta_Heavy_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Varta_Heavy | `GeneralNPC_Varta_Heavy_ItemGenerator` | 21 | 22 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Varta_Recon_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Varta_Recon | `GeneralNPC_Varta_Recon_ItemGenerator` | 28 | 23 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Varta_Sniper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Varta_Sniper | `GeneralNPC_Varta_Sniper_ItemGenerator` | 20 | 20 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `CNPPZombie_Varta_Stormtrooper_ItemGenerator` | dlc1 | CNPPZombie | GeneralCNPPZombie_Varta_Stormtrooper | `GeneralNPC_Varta_Stormtrooper_ItemGenerator` | 34 | 25 | CANDIDATE — generic DLC1/CNPP zombie child-local rows; source anomalies documented |
| `DLC01_MQ06_Zombie_Bandit_CloseCombat_ItemGenerator` | dlc1 | DLC01_MQ06 scripted | DLC01_MQ06_Zombie_Bandit_CloseCombat | `DLC01_DynamicItemGenerator` | 19 | 16 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `DLC01_MQ06_Zombie_Bandit_Heavy_ItemGenerator` | dlc1 | DLC01_MQ06 scripted | DLC01_MQ06_Zombie_Bandit_Heavy | `DLC01_DynamicItemGenerator` | 20 | 13 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `DLC01_MQ06_Zombie_Bandit_Recon_ItemGenerator` | dlc1 | DLC01_MQ06 scripted | DLC01_MQ06_Zombie_Bandit_Recon | `DLC01_DynamicItemGenerator` | 24 | 12 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `DLC01_MQ06_Zombie_Bandit_Stormtrooper_ItemGenerator` | dlc1 | DLC01_MQ06 scripted | DLC01_MQ06_Zombie_Bandit_Stormtrooper | `DLC01_DynamicItemGenerator` | 18 | 12 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `DLC01_MQ06_Zombie_Neutral_Recon_ItemGenerator` | dlc1 | DLC01_MQ06 scripted | DLC01_MQ06_Zombie_Neutral_Recon | `DLC01_DynamicItemGenerator` | 32 | 19 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `DLC01_MQ06_Zombie_Neutral_Sniper_ItemGenerator` | dlc1 | DLC01_MQ06 scripted | DLC01_MQ06_Zombie_Neutral_Sniper | `DLC01_DynamicItemGenerator` | 30 | 22 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `DLC01_MQ06_Zombie_Neutral_Stormtrooper_ItemGenerator` | dlc1 | DLC01_MQ06 scripted | DLC01_MQ06_Zombie_Neutral_Stormtrooper | `DLC01_DynamicItemGenerator` | 36 | 22 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `C_E03_MQ06N_MeetingScar_cha_Zombie_01_ItemGenerator` | base | E03 scripted | C_E03_MQ06N_MeetingScar_cha_Zombie_01 | — | 17 | 13 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `C_E03_MQ06N_MeetingScar_cha_Zombie_02_ItemGenerator` | base | E03 scripted | C_E03_MQ06N_MeetingScar_cha_Zombie_02 | — | 17 | 12 | EXCLUDE FROM GENERIC TUNING — scripted/quest zombie |
| `GeneralZombie_Bandit_CloseCombat_ItemGenerator` | base | GeneralZombie | GeneralZombie_Bandit_CloseCombat | `GeneralNPC_Bandit_CloseCombat_ItemGenerator` | 14 | 19 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Bandit_Heavy_ItemGenerator` | base | GeneralZombie | GeneralZombie_Bandit_Heavy | `GeneralNPC_Bandit_Heavy_ItemGenerator` | 16 | 21 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Bandit_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Bandit_Recon | `GeneralNPC_Bandit_Recon_ItemGenerator` | 34 | 21 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Bandit_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Bandit_Stormtrooper | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | 14 | 19 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Corpus_CloseCombat_ItemGenerator` | base | GeneralZombie | GeneralZombie_Corpus_CloseCombat | `GeneralNPC_Corpus_CloseCombat_ItemGenerator` | 11 | 21 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Corpus_Heavy_ItemGenerator` | base | GeneralZombie | GeneralZombie_Corpus_Heavy | `GeneralNPC_Corpus_Heavy_ItemGenerator` | 9 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Corpus_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Corpus_Recon | `GeneralNPC_Corpus_Recon_ItemGenerator` | 12 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Corpus_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Corpus_Sniper | `GeneralNPC_Corpus_Sniper_ItemGenerator` | 9 | 22 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Corpus_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Corpus_Stormtrooper | `GeneralNPC_Corpus_Stormtrooper_ItemGenerator` | 11 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Duty_CloseCombat_ItemGenerator` | base | GeneralZombie | GeneralZombie_Duty_CloseCombat | `GeneralNPC_Duty_CloseCombat_ItemGenerator` | 14 | 22 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Duty_Heavy_ItemGenerator` | base | GeneralZombie | GeneralZombie_Duty_Heavy | `GeneralNPC_Duty_Heavy_ItemGenerator` | 18 | 26 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Duty_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Duty_Recon | `GeneralNPC_Duty_Recon_ItemGenerator` | 12 | 24 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Duty_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Duty_Sniper | `GeneralNPC_Duty_Sniper_ItemGenerator` | 19 | 25 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Duty_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Duty_Stormtrooper | `GeneralNPC_Duty_Stormtrooper_ItemGenerator` | 22 | 24 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Freedom_CloseCombat_ItemGenerator` | base | GeneralZombie | FreedomZombieCastro \| GeneralZombie_Freedom_CloseCombat | `GeneralNPC_Freedom_CloseCombat_ItemGenerator` | 17 | 22 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Freedom_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Freedom_Recon | `GeneralNPC_Freedom_Recon_ItemGenerator` | 18 | 24 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Freedom_Sniper_ItemGenerator` | base | GeneralZombie | FreedomPiton \| GeneralZombie_Freedom_Sniper | `GeneralNPC_Freedom_Sniper_ItemGenerator` | 17 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Freedom_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Freedom_Stormtrooper | `GeneralNPC_Freedom_Stormtrooper_ItemGenerator` | 20 | 24 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Mercenaries_CloseCombat_ItemGenerator` | base | GeneralZombie | GeneralZombie_Mercenary_CloseCombat | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | 15 | 20 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Mercenaries_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Mercenary_Recon | `GeneralNPC_Mercenaries_Recon_ItemGenerator` | 19 | 22 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Mercenaries_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Mercenary_Sniper | `GeneralNPC_Mercenaries_Sniper_ItemGenerator` | 16 | 21 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Mercenaries_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Mercenary_Stormtrooper | `GeneralNPC_Mercenaries_Stormtrooper_ItemGenerator` | 17 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Militaries_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Military_Sniper | `GeneralNPC_Militaries_Sniper_ItemGenerator` | 13 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Monolith_CloseCombat_ItemGenerator` | base | GeneralZombie | CNPP_Archanomaly_PhantomZombieNPC_Monolith_CloseCombat \| GeneralZombie_Monolith_CloseCombat | `GeneralNPC_Monolith_CloseCombat_ItemGenerator` | 19 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Monolith_Recon_ItemGenerator` | base | GeneralZombie | CNPP_Archanomaly_PhantomZombieNPC_Scientist_Recon \| GeneralZombie_Monolith_Recon | `GeneralNPC_Monolith_Recon_ItemGenerator` | 19 | 26 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Monolith_Sniper_ItemGenerator` | base | GeneralZombie | CNPP_Archanomaly_PhantomZombieNPC_Military_Sniper \| GeneralZombie_Monolith_Sniper | `GeneralNPC_Monolith_Sniper_ItemGenerator` | 26 | 26 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Monolith_Stormtrooper_ItemGenerator` | base | GeneralZombie | CNPP_Archanomaly_PhantomZombieNPC_Base \| GeneralZombie_Monolith_Stormtrooper | `GeneralNPC_Monolith_Stormtrooper_ItemGenerator` | 19 | 26 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Neutral_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Neutral_Sniper | `GeneralNPC_Neutral_Sniper_ItemGenerator` | 42 | 31 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Noon_CloseCombat_ItemGenerator` | base | GeneralZombie | GeneralZombie_Noon_CloseCombat | `GeneralNPC_Noon_CloseCombat_ItemGenerator` | 9 | 21 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Noon_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Noon_Recon | `GeneralNPC_Noon_Recon_ItemGenerator` | 11 | 22 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Noon_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Noon_Sniper | `GeneralNPC_Noon_Sniper_ItemGenerator` | 13 | 24 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Noon_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Noon_Stormtrooper | `GeneralNPC_Noon_Stormtrooper_ItemGenerator` | 8 | 20 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Scientist_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Scientist_Recon | `GeneralNPC_Scientists_Recon_ItemGenerator` | 5 | 13 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Scientists_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Scientist_Stormtrooper | `GeneralNPC_Scientists_Stormtrooper_ItemGenerator` | 7 | 14 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Spark_CloseCombat_ItemGenerator` | base | GeneralZombie | GeneralZombie_Spark_CloseCombat | `GeneralNPC_Spark_CloseCombat_ItemGenerator` | 12 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Spark_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Spark_Recon | `GeneralNPC_Spark_Recon_ItemGenerator` | 12 | 25 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Spark_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Spark_Sniper | `GeneralNPC_Spark_Sniper_ItemGenerator` | 22 | 29 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Spark_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Spark_Stormtrooper | `GeneralNPC_Spark_Stormtrooper_ItemGenerator` | 19 | 29 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Varta_CloseCombat_ItemGenerator` | base | GeneralZombie | GeneralZombie_Varta_CloseCombat | `GeneralNPC_Varta_CloseCombat_ItemGenerator` | 18 | 19 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Varta_Heavy_ItemGenerator` | base | GeneralZombie | GeneralZombie_Varta_Heavy | `GeneralNPC_Varta_Heavy_ItemGenerator` | 15 | 22 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Varta_Recon_ItemGenerator` | base | GeneralZombie | GeneralZombie_Varta_Recon | `GeneralNPC_Varta_Recon_ItemGenerator` | 24 | 23 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Varta_Sniper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Varta_Sniper | `GeneralNPC_Varta_Sniper_ItemGenerator` | 15 | 21 | CANDIDATE — generic base zombie child-local rows |
| `GeneralZombie_Varta_Stormtrooper_ItemGenerator` | base | GeneralZombie | GeneralZombie_Varta_Stormtrooper | `GeneralNPC_Varta_Stormtrooper_ItemGenerator` | 28 | 23 | CANDIDATE — generic base zombie child-local rows |
| `ZombieFoster_ItemGenerator` | base | Named Zombie | ZombieFoster | `GeneralNPC_Freedom_Recon_ItemGenerator` | 18 | 25 | EXCLUDE FROM GENERIC TUNING — named zombie NPC |
| `ZombieIgor_petrusko_ItemGenerator` | base | Named Zombie | ZombieIgor_petrusko | `GeneralNPC_Scientists_Recon_ItemGenerator` | 5 | 13 | EXCLUDE FROM GENERIC TUNING — named zombie NPC |
| `ZombieLevsa_ItemGenerator` | base | Named Zombie | ZombieLevsa | `GeneralNPC_Neutral_Stormtrooper_ItemGenerator` | 35 | 31 | EXCLUDE FROM GENERIC TUNING — named zombie NPC |
| `ZombiePetkaBelak_ItemGenerator` | base | Named Zombie | ZombiePetkaBelak | `GeneralNPC_Neutral_Recon_ItemGenerator` | 45 | 30 | EXCLUDE FROM GENERIC TUNING — named zombie NPC |
| `ZombieZombirovannyj_1_ItemGenerator` | base | Named Zombie | ZombieZombirovannyj_1 | `GeneralNPC_Scientists_Recon_ItemGenerator` | 5 | 13 | EXCLUDE FROM GENERIC TUNING — named zombie NPC |
| `RUCloseCombatWeaponExperiencedGenerator` | base | RU weapon pool | — | — | 4 | 3 | REVIEW/EXCLUDE — dependency of bespoke RedForest zombie |
| `RUMachineGunnerWeaponExperiencedGenerator` | base | RU weapon pool | — | — | 4 | 4 | REVIEW/EXCLUDE — dependency of bespoke RedForest zombie |
| `RUScoutWeaponExperiencedGenerator` | base | RU weapon pool | — | — | 4 | 3 | REVIEW/EXCLUDE — dependency of bespoke RedForest zombie |
| `RUSniperWeaponExperiencedGenerator` | base | RU weapon pool | — | — | 6 | 6 | REVIEW/EXCLUDE — dependency of bespoke RedForest zombie |
| `RUStormTrooperWeaponExperiencedGenerator` | base | RU weapon pool | — | — | 8 | 8 | REVIEW/EXCLUDE — dependency of bespoke RedForest zombie |
| `KeyGeneratorRedForest` | base | RedForest key pool | — | — | 1 | 1 | EXCLUDE FROM GENERIC TUNING — quest/system key dependency |
| `RC_Military_Zombie_RedForest_ItemGenerator` | base | RedForest zombie | GeneralZombieMilitaryReconRedForest | — | 14 | 90 | EXCLUDE FROM GENERIC TUNING — bespoke RedForest zombie loadout |
