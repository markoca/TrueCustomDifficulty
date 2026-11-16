# NPC loot — ordinary-human scope expansion (S.T.A.L.K.E.R. 2.1)

**Date:** 2026-11-16
**Evidence:** the original `STALKER2-GameData-CFG.zip` / `STALKER2-DLC1-CFG.zip` 2.1 dumps, re-parsed directly (not the older 2.0/2.0.5 research inventories).
**Supersedes the scope statements** in `NPC-LOOT-2.1-END-TO-END-AUDIT-2026-11-15.md`, `NPC-CORPSE-LOOT-GRANULARITY-2.1-2026-11-15.md` and `GENERIC-CORPSE-STASH-GENERATOR-OWNERSHIP-2.1-2026-11-15.md` that call the 33-root set "the ordinary human corpse path".

## Finding

The previous production scope (33 roots) contained **31 `GuardNPC_*` settlement-guard loadouts and 2 custom `GeneralNPC_*` loadouts**. It did **not** contain the roaming `GeneralNPC_<Faction>_<Role>_ItemGenerator` loadouts that ALife actually spawns:

| Spawner source (2.1) | `GeneralNPC_<Faction>_<Role>` refs | `GuardNPC_*` refs |
| --- | ---: | ---: |
| base `SpawnActorPrototypes.cfg` | 8305 | 205 |
| base `LairPrototypes.cfg` | 1457 | 152 |
| DLC1 `SpawnActorPrototypes.cfg` | 2956 | 169 |
| DLC1 `LairPrototypes.cfg` | 965 | 0 |

Example chain: `GeneralNPC_Bandit_Recon` (ObjPrototype, `refkey=NPCBase`) → `GeneralNPC_Bandit_Recon_ItemGenerator` → `[9]` SubItemGenerator → `GeneralNPC_Consumables_Recon`. That link was never redirected, so **MED DROPS, the ten per-item Chance rows, the consumable half of FEWER SUPPLIES and DROPPED WEAPON CONDITION did not affect most NPCs you actually kill.** CORPSE SMART LOOT, BONUS AMMO, the SmartLoot half of FEWER SUPPLIES and FEWER MOD ITEMS were already global and are unchanged.

## Why the roots were safe to add

* **Zombies:** every refkey descendant (`GeneralZombie_*`, `CNPPZombie_*`, custom children) **re-declares** the consumable category locally. A patch on the parent root does not leak into them. Verified for all 59 new roots.
* **Quest NPCs** that reuse a generic loadout (e.g. `BanditKesaOtbitok`, Zalesie trader/technician on `GeneralNPC_Neutral_Recon_ItemGenerator`, several DLC1 quest Freedom/Duty members) now follow the same generic tuning. Their loot is the generic faction loadout by design; no unique/quest item rows are touched.
* The redirect only swaps the pool **SID**; the native gate is preserved (Weight=1 ×89, Chance=1 ×2, sole-entry Weight=0.5 ×1). The shared vanilla `GeneralNPC_Consumables_*` pools are never patched.

## New production scope

| Surface | Before | Now |
| --- | ---: | ---: |
| Consumable pool redirects | 33 | **92** (88 base + 4 DLC1 NORD) |
| Weapon-condition roots | 33 | **92** |
| Weapon rows (Min/Max pairs) | 121 | **738** (40 DLC1) |
| Durability edit leaves | 242 | **1476** (80 DLC1) |
| TCD consumable pool clones | 6 | 6 (Armored pool has no eligible link) |

**Exclusions (unchanged policy, now enforced in code):** named `GeneralNPC_MIK_*` (Mykluha squad, 4), disabled Prologue edges with `Chance=0` (2), and the three `*_No_Armor_ItemGenerator` roots no spawner or ObjPrototype uses.

**Weapon condition** edits only direct `WeaponPrimary` / `WeaponSecondary` / `WeaponPistol` rows. Rows under the `Artifact` enum (RGD-5/F1 grenades, 45–45%) and one `BodyArmor` row (35–55%) are not weapons and stay untouched. Authored weapon outliers: `GuardNPC_Corpus_Stormtrooper` 35–45% and `GuardNPC_Freedom_Stormtrooper` 45–45%; all other 736 rows are 25–50%. Pistol sub-generators (`GeneralNPC_<Faction>_WeaponPistol`) remain out of scope.

## How the vanilla corpse path works (reference)

1. **Loadout ItemGenerator:** primary weapon chosen by `Weight` per player rank (and a separate Stalker-difficulty block), pistol sub-generator (~25–40%), armor/NVG sub-generators, RGD-5 ~20%, Echo ~5%, and one consumable pool link.
2. **Consumable pools:** every item rolls its own `Chance` independently; four branches = difficulty (Easy/Medium/Hard vs Stalker) × player rank (Newbie/Experienced vs Veteran/Master), so e.g. Recon Bandage rises 0.3 → 1.0 as the player progresses.
3. **CorpseSmartLoot (difficulty):** `NPC_Medicine_Smart` / `NPC_Ammo_Smart` / `NPC_Water_Smart` with `MinSpawnChance`/`MaxSpawnChance`, `ItemSetCount` / `MainWeaponAmmoCount` and `PriorityCaliber` — by its structure a need-based roll that favours what the player lacks (bonus ammo follows the player's weapon caliber). Engine code is not visible; treat as strongly indicated, not proven.

## Code and audits

* `npc-medical-production-redirect-manifest.csv`: 59 rows appended (original 33 kept first, unchanged, CRLF).
* `cfg-writer-baseline.json`: 1234 `loot.droppedWeaponCondition` points appended after the original 242.
* `CfgPatchWriterService`: redirect scope constant 92 (4 DLC1) and a hard guard against MIK / Prologue / No_Armor rows. DLC1 rows only emit on DLC1 builds.
* New `tools/audit_npc_generalnpc_scope_21.py --base-zip … --dlc1-zip …` re-derives the whole scope from the original archives (links, gates, descendants, consumers, weapon leaves and source values). `tools/audit_loot_direct_source.py` now also reads the DLC1 archive: **PASS 1554/1554** fields, 92 roots / 738 rows.
* The doc-coverage audits (`audit_generic_npc_stash_category_coverage_21.py`, `audit_generic_corpse_stash_ownership_21.py`, `audit_generic_loot_scarcity_gates_21.py`) still describe the original 33 roots and check those as the first 33 manifest rows.

## In-game acceptance checklist

Use a **fresh save or a new zone visit** — already rolled corpses keep their loot. Kill ordinary roaming NPCs (not settlement guards), e.g. bandits around Zalesie / Lesser Zone.

1. **MED DROPS 0×**, CORPSE SMART LOOT = NONE → roaming bandits/stalkers should carry **no** Bandage/Medkit/Army Medkit/Anti-Rad (food and drinks still appear).
2. **MED DROPS 2×**, NONE → noticeably more medical items than vanilla on the same NPC types.
3. **FEWER SUPPLIES 100%**, NONE → no food, drinks or medicine on roaming NPC corpses; weapons/armor unchanged.
4. **Per-item row:** check *Bandage* at 3× on Veteran → early ranks rise from ~25% toward ~75%, late ranks stay capped at 100%; the N/E/V/M bars in the row show the same shape.
5. **DROPPED WEAPON CONDITION 90–100%** → primary weapons and pistols dropped by roaming NPCs show 90–100% condition.
6. **DLC1 (if owned):** repeat 1 and 5 on NORD mercenaries.
7. **Zombies:** with the settings from 1 and 5, zombie corpses keep vanilla consumables and weapon condition.
