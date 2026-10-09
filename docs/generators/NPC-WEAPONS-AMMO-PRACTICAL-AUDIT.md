# Standard Human NPC Weapons / Attached Ammo — Practical Production Audit
**Version:** v0.2.15  
**Source:** existing deep audit plus production-only policy filtering  
**Status:** documentation/audit only; no writer or UI behavior added

### 2026-10-09 — Source-verified NPC Ammo / Breakable Box Ammo boundary (current override)

This audit remains correct for the **121 original weapon-attached NPC item rows** and their `AmmoMinCount/AmmoMaxCount` fields. **The generic NPC Ammo UI no longer edits those fields**. The 242 prior attached-ammo edit points were retired in favor of a source-inherited `TCD_NPC_Ammo_Smart` corpse-bonus clone sourced exclusively from `GameData/StashPrototypes.cfg`, with 43 MinCount/MaxCount item edits in 24 SmartLoot groups. The stable preset key `loot.npcWeaponAmmoSupply` is retained for compatibility, but now means generic **extra corpse package ammo quantity**. It is only active with Extra Corpse Loot = Medicine + Ammo. Breakable Box Ammo is a separate `DestructibleStash_AmmoSNG/NATO` ItemGenerator system. Historical notes below describing weapon-attached ammo as the current generic ammo control are superseded. The dropped NPC weapon condition still edits its 121 audited weapon rows. Runtime acceptance remains unverified.

## Verdict
After applying the project hard-exclusion policy, the repeatable standard-human weapon surface is cleaner than the older audit implied. The two Prologue loadouts and four named DLC `GeneralNPC_MIK_*` loadouts are excluded. Every one of the **6 nested shared-pistol links** belonged to those excluded roots. The remaining production surface therefore contains **33 loadout generators**, **36 standard-human archetype consumers**, **121 local weapon rows**, **37 unique weapon SIDs**, and **0 nested/shared weapon dependencies**.
This means the repeatable standard-human weapon **condition** and attached **ammo-count** fields form complete local target surfaces. That does not yet prove the final corpse/drop result in game, so both remain runtime-test candidates rather than automatic UI controls.
## What "most used" means here
CFG provides topology, not playthrough telemetry. The audit therefore uses **static archetype breadth**: how many ordinary standard-human archetypes directly consume each loadout. It does not claim how many of those NPCs a player will actually meet or kill.
The broadest individual roots are the three neutral loadouts, each shared by both the ordinary Neutral and Digger archetypes: `GuardNPC_Neutral_CloseCombat_ItemGenerator`, `GuardNPC_Neutral_Sniper_ItemGenerator`, and `GuardNPC_Neutral_Stormtrooper_ItemGenerator` (**2 consumers each**). All other production roots have one mapped standard-human archetype consumer.
By role breadth: **Sniper 9 consumers**, **CloseCombat 8**, **Stormtrooper 8**, **Recon 7**, **Heavy 4**. These numbers are topology breadth only.
## Production field coverage
| Field | Rows | Meaning / decision |
| --- | ---: | --- |
| Weapon rows | 121 | all locally authored in production loadout roots |
| `Weight` selection | 117 | weapon composition; do not expose as generic difficulty control |
| `Chance` selection | 4 | only custom neutral sniper rows; do not merge with Weight semantics |
| `MinDurability/MaxDurability` | 121 | complete local condition surface |
| `AmmoMinCount` | 121 | complete local attached-ammo lower bound |
| `AmmoMaxCount` | 118 | normal source spelling |
| `AmmoMaxcount` | 3 | exact lowercase-`c` source spelling; writer must preserve it |

**Durability:** 120/121 rows use `0.25-0.50`; the single exception is `GuardGunGrim_ST` in `GuardNPC_Corpus_Stormtrooper_ItemGenerator` at `0.35-0.45`.
**Attached ammo:** 81/121 rows allow a zero lower bound, while 40/121 require positive attached ammo. The ranges are deliberately not flattened.
## Role breadth and offered weapons
| Role | Loadout roots | Archetype consumers | Weapon rows | Unique weapon SIDs |
| --- | ---: | ---: | ---: | ---: |
| Sniper | 8 | 9 | 42 | 16 |
| Stormtrooper | 7 | 8 | 43 | 14 |
| CloseCombat | 7 | 8 | 19 | 6 |
| Recon | 7 | 7 | 12 | 6 |
| Heavy | 4 | 4 | 5 | 1 |

Exact weapon membership and ammo/durability ranges are in `npc-weapon-production-weapon-frequency.csv`. The most broadly represented weapon SIDs by distinct production loadout roots are:
- `GuardGunUDP_HG` — 7 loadout roots, 8 source rows; roles: Sniper | Stormtrooper; ammo ranges: 0-2 | 2-4.
- `GuardGunAK74_ST` — 6 loadout roots, 6 source rows; roles: Stormtrooper; ammo ranges: 0-5.
- `GuardGunPM_HG` — 5 loadout roots, 9 source rows; roles: Sniper | Stormtrooper; ammo ranges: 0-3 | 0-7 | 2-4.
- `GuardGunM860_SG` — 5 loadout roots, 6 source rows; roles: CloseCombat; ammo ranges: 0-2 | 0-7.
- `GuardGunSVDM_SP` — 5 loadout roots, 6 source rows; roles: Sniper; ammo ranges: 0-2.
- `GuardGunGrim_ST` — 4 loadout roots, 7 source rows; roles: Stormtrooper; ammo ranges: 0-5 | 30-45.
- `GuardGunPKP_MG` — 4 loadout roots, 5 source rows; roles: Heavy; ammo ranges: 0-20 | 25-35.
- `GuardGunAKU_PP` — 4 loadout roots, 4 source rows; roles: Recon | Sniper; ammo ranges: 0-5.
- `GuardGunAPB_HG` — 4 loadout roots, 4 source rows; roles: Sniper | Stormtrooper; ammo ranges: 0-7.
- `GuardGunSPSA_SG` — 4 loadout roots, 4 source rows; roles: CloseCombat; ammo ranges: 0-2 | 0-7.
- `GuardGunViper_PP` — 4 loadout roots, 4 source rows; roles: Recon | Sniper; ammo ranges: 0-5 | 30-45.
- `GuardGunG37_ST` — 3 loadout roots, 4 source rows; roles: Stormtrooper; ammo ranges: 30-45.

## Exclusions that remove the old shared-pool gap
- `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` — Prologue.
- `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` — Prologue.
- `GeneralNPC_MIK_CloseCombat_ItemGenerator` — named DLC Mykluha path.
- `GeneralNPC_MIK_Recon_ItemGenerator` — named DLC Mykluha path.
- `GeneralNPC_MIK_Sniper_ItemGenerator` — named DLC Mykluha path.
- `GeneralNPC_MIK_Stormtrooper_ItemGenerator` — named DLC Mykluha path.

Those six roots accounted for **all 6** nested links to `GeneralNPC_Bandit_WeaponPistol` / `GeneralNPC_Freedom_WeaponPistol`. Once excluded, the repeatable production surface has **zero nested weapon pool links**.
## Control-surface verdict
### Standard Human NPC Weapon Condition — structurally ready, runtime proof still required
A future control can target all 121 local `MinDurability/MaxDurability` pairs without touching named NPCs, zombies, quests, traders or shared weapon pools. Before UI promotion, confirm in game that the generated durability range corresponds to the observable carried/dropped weapon condition after NPC death.
### Standard Human NPC Dropped Ammo Amount — structurally ready, runtime proof still required
All 121 production weapon rows expose local attached-ammo min/max fields. A future control can scale only those integer counts while preserving weapon-selection Weight/Chance values. Runtime testing is still required to prove how these counts surface on the living NPC and corpse/weapon loot path. Preserve the three exact-source `AmmoMaxcount` spellings.
### Weapon selection / frequency — no UI
Do not expose a generic weapon-frequency or weapon-selection slider. Production source mixes 117 `Weight` rows with 4 `Chance` rows, and changing those fields would rebalance weapon composition rather than tune a clear difficulty mechanic.
## Machine-readable outputs
- `npc-weapon-production-loadouts.csv` — 33 production loadout roots with consumers, role breadth and reachable items.
- `npc-weapon-production-entries.csv` — all 121 exact local production weapon rows.
- `npc-weapon-production-role-summary.csv` — role-level static breadth and offered weapon lists.
- `npc-weapon-production-weapon-frequency.csv` — 37 weapon SIDs ranked by production loadout coverage.
- `npc-weapon-production-exclusions.csv` — the six hard-excluded loadout roots and reasons.
- `npc-weapon-production-control-surfaces.csv` — condition/ammo/selection control verdicts.
- `npc-weapon-production-summary.json` — compact production-only counts and sanity facts.
