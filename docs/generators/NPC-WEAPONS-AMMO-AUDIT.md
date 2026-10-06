# NPC Weapons and Attached Ammo Generator Audit

**Source:** STALKER 2 runtime/game + DLC CFG dump 2.0.5  
**Scope:** standard-human NPC loadout weapon rows plus weapon pools directly nested by those loadouts  
**Status:** deep-audited; no runtime writer changes made

## Verdict

The standard-human weapon system is substantially cleaner than consumables. There are **173 direct weapon rows across 39 strict standard-human loadout generators**, covering **51 direct weapon SIDs**. Every one of those direct generators has only `human_npc_standard` impact context and has **zero incoming composition/inheritance references**, so editing those exact rows is structurally isolated from named NPCs, zombies, quests and traders.

However, standard-human loadouts also have **6 direct links to 2 shared pistol pools**. Those pools are reused outside standard human NPCs, so editing their weapon rows would have cross-context effects. Including nested pistol outcomes, standard-human loadouts can reach **54 distinct weapon SIDs**.

This creates a clean boundary: **direct weapon rows are structurally safe allowlist candidates; shared nested pistol pool definitions are not**. A future “all standard NPC weapon condition/ammo” control would therefore be incomplete if it patched only the direct rows, but unsafe if it also edited the shared pistol pools. Completing the latter requires explicit pool cloning/rewiring or a deliberately narrower control definition.

## Field coverage

| Property | Direct standard-human weapon rows | Meaning at CFG level | Audit status |
| --- | ---: | --- | --- |
| Weapon selection via `Weight` | 169 / 173 | weighted candidate value inside that weapon-category branch | source-confirmed; exact probability formula not assumed |
| Weapon selection via `Chance` | 4 / 173 | explicit chance field on four custom-sniper rows | source-confirmed |
| `MinDurability` / `MaxDurability` | 173 / 173 | generated weapon durability range | direct field mapping; final post-death/drop condition still merits runtime test |
| `AmmoMinCount` | 173 / 173 | lower ammo-count field attached to generated weapon entry | direct field mapping |
| `AmmoMaxCount` (case-normal spelling) | 170 / 173 | upper attached-ammo count field | direct field mapping |
| `AmmoMaxcount` (lowercase `c` anomaly) | 3 / 173 | same apparent upper-count role, but exact source key spelling differs | must preserve exact key spelling in any future writer |

All 173 direct weapon rows carry a minimum and maximum durability value and an ammo minimum. All 173 also carry an upper ammo value when `AmmoMaxCount` and the three exact-source `AmmoMaxcount` spellings are treated separately rather than normalized.

## Direct weapon-row distributions

- Categories: **WeaponPrimary 143**, **WeaponPistol 23**, **WeaponSecondary 7**.
- Selection fields: **169 Weight rows**, **4 Chance rows**; no direct row has both or neither.
- Durability: **172 rows use 0.25–0.50**; one `GuardGunGrim_ST` row in `GuardNPC_Corpus_Stormtrooper_ItemGenerator` uses **0.35–0.45**.
- Attached-ammo minimum is zero on **124** rows and positive on **49** rows. The actual ranges are intentionally preserved per entry rather than flattened.
- Only the four DLC1 `GeneralNPC_MIK_*` standard loadouts use explicit Game Difficulty gates on their direct weapon categories; the base-game standard loadouts are primarily rank-gated or ungated.

## Direct standard-human loadout summary

Every row below is externally confined to the `human_npc_standard` context in the supplied graph. `Weapons` lists every direct weapon SID in that loadout; shared nested pistol pools are shown later and are not silently merged into this table.

| Loadout generator | Runtime consumer(s) | Weapon rows | Categories | Gate patterns | Selection fields | Durability | Attached ammo ranges | Weapons |
| --- | --- | ---: | --- | --- | --- | --- | --- | --- |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` | GeneralNPC_Bandit_Recon_Prolog_AKU | 2 | WeaponPrimary | All / ungated × Newbie \| All / ungated × Experienced / Veteran / Master | Weight: 3, 1000 | 0.25-0.5 | 0-5 \| 0-7 | GunAKU_PP |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` | GeneralNPC_Bandit_Recon_Prolog_Obrez | 2 | WeaponPrimary | All / ungated × Newbie \| All / ungated × Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-2 \| 0-7 | GunObrez_SG |
| `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` | GeneralNPC_Neutral_SniperCustom | 4 | WeaponPrimary | All / ungated × Newbie \| All / ungated × Experienced \| All / ungated × Veteran \| All / ungated × Master | Chance: 1 | 0.25-0.5 | 0-2 \| 0-7 \| 3-5 | GunSKP_DMR \| GunSVDM_SP \| GunThreeLine_SP |
| `GeneralNPC_SIRCAA_Scientist_Recon_ItemGenerator` | GeneralNPC_SIRCAA_Scientist_Recon | 1 | WeaponPistol | All / ungated × Newbie / Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-3 | GunUDP_HG |
| `GuardNPC_Bandit_CloseCombat_ItemGenerator` | GuardNPC_SultanBandit_CloseCombat | 2 | WeaponPrimary | All / ungated × Newbie / Experienced \| All / ungated × Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-2 \| 0-7 | GuardGunObrez_SG \| GuardGunSPSA_SG |
| `GuardNPC_Bandit_Heavy_ItemGenerator` | GuardNPC_SultanBandit_Heavy | 1 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-20 | GuardGunPKP_MG |
| `GuardNPC_Bandit_Recon_ItemGenerator` | GuardNPC_SultanBandit_Recon | 2 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-5 | GuardGunAKU_PP \| GuardGunViper_PP |
| `GuardNPC_Bandit_Stormtrooper_ItemGenerator` | GuardNPC_SultanBandit_Stormtrooper | 1 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-5 | GuardGunAK74_ST |
| `GuardNPC_Corpus_CloseCombat_ItemGenerator` | GuardNPC_Corpus_CloseCombat | 2 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master | Weight: 1000 | 0.25-0.5 | 0-2 \| 4-6 | GuardGunD12_SG \| GuardGunRam2_SG |
| `GuardNPC_Corpus_Heavy_ItemGenerator` | GuardNPC_Corpus_Heavy | 2 | WeaponPrimary | All / ungated × Newbie / Experienced \| All / ungated × Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-20 \| 25-35 | GuardGunPKP_MG |
| `GuardNPC_Corpus_Recon_ItemGenerator` | GuardNPC_Corpus_Recon | 3 | WeaponPrimary | All / ungated × Newbie \| All / ungated × Experienced \| All / ungated × Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-5 \| 30-45 | GuardGunAKU_PP \| GuardGunBucket_PP \| GuardGunZubr_PP |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | 6 | WeaponPistol \| WeaponPrimary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master | Weight: 1, 1000 | 0.25-0.5 | 0-2 \| 0-3 \| 0-7 | GuardGunAPB_HG \| GuardGunGP3A_DMR \| GuardGunPM_HG \| GuardGunSVU_SP |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | 8 | WeaponPistol \| WeaponPrimary \| WeaponSecondary | All / ungated × Newbie / Experienced \| All / ungated × Veteran \| All / ungated × Master \| All / ungated × Veteran / Master | Weight: 1, 1000 | 0.25-0.5 \| 0.35-0.45 | 0-1 \| 0-3 \| 0-5 \| 0-7 \| 30-45 | GuardGunAPB_HG \| GuardGunDnipro_ST \| GuardGunFora_ST \| GuardGunGrim_ST \| GuardGunGvintar_ST \| GuardGunPM_HG \| GuardGunRpg7_GL |
| `GuardNPC_Duty_CloseCombat_ItemGenerator` | GuardNPC_Duty_CloseCombat | 2 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master | Weight: 1000 | 0.25-0.5 | 0-2 \| 0-7 | GuardGunD12_SG \| GuardGunM860_SG |
| `GuardNPC_Duty_Heavy_ItemGenerator` | GuardNPC_Duty_Heavy | 1 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-20 | GuardGunPKP_MG |
| `GuardNPC_Duty_Recon_ItemGenerator` | GuardNPC_Duty_Recon | 1 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-10 | GuardGunZubr_PP |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | 6 | WeaponPistol \| WeaponPrimary | All / ungated × Newbie / Experienced \| All / ungated × Veteran / Master \| All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master | Weight: 1, 500, 1000 | 0.25-0.5 | 0-2 \| 0-3 \| 0-7 | GuardGunAPB_HG \| GuardGunPM_HG \| GuardGunSKP_DMR \| GuardGunSVDM_SP |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | 9 | WeaponPistol \| WeaponPrimary | All / ungated × Newbie / Experienced \| All / ungated × Veteran \| All / ungated × Master \| All / ungated × Newbie / Experienced / Veteran | Weight: 1, 100, 1000 | 0.25-0.5 | 0-3 \| 0-5 \| 0-7 \| 30-45 | GuardGunAK74_ST \| GuardGunAPB_HG \| GuardGunGrim_ST \| GuardGunGvintar_ST \| GuardGunLavina_ST \| GuardGunPM_HG |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | 8 | WeaponPistol \| WeaponPrimary \| WeaponSecondary | All / ungated × Newbie / Experienced \| All / ungated × Veteran \| All / ungated × Master \| All / ungated × All / ungated \| All / ungated × Newbie \| All / ungated × Experienced / Veteran / Master | Weight: 1, 1000 | 0.25-0.5 | 0-2 \| 0-7 \| 2-4 \| 30-45 | GuardGunGP3A_DMR \| GuardGunM10_HG \| GuardGunM701_SP \| GuardGunSVDM_SP \| GuardGunUDP_HG \| GuardGunViper_PP |
| `GuardNPC_Monolith_CloseCombat_ItemGenerator` | GuardNPC_Monolith_CloseCombat | 4 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master | Weight: 1000 | 0.25-0.5 | 0-2 \| 4-6 \| 15-20 | GuardGunD12_SG \| GuardGunM860_SG \| GuardGunRam2_SG \| GuardGunSPSA_SG |
| `GuardNPC_Monolith_Recon_ItemGenerator` | GuardNPC_Monolith_Recon | 2 | WeaponPrimary | All / ungated × Newbie / Experienced \| All / ungated × Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-5 \| 30-45 | GuardGunIntegral_PP \| GuardGunViper_PP |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | 6 | WeaponPistol \| WeaponPrimary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master \| All / ungated × All / ungated | Weight: 1, 100, 300, 1000 | 0.25-0.5 | 0-2 \| 2-4 | GuardGunGP3A_DMR \| GuardGunMark_SP \| GuardGunSKP_DMR \| GuardGunSVDM_SP \| GuardGunUDP_HG |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | 12 | WeaponPistol \| WeaponPrimary \| WeaponSecondary | All / ungated × Newbie \| All / ungated × Experienced \| All / ungated × Veteran \| All / ungated × Master \| All / ungated × All / ungated | Weight: 1, 5, 10, 1000 | 0.25-0.5 | 0-2 \| 0-5 \| 0-7 \| 2-4 \| 8-12 \| 10-15 \| 30-45 | GuardGunAK74_ST \| GuardGunG37_ST \| GuardGunGauss_SP \| GuardGunGrim_ST \| GuardGunGvintar_ST \| GuardGunKharod_ST \| GuardGunLavina_ST \| GuardGunM16_ST \| GuardGunRpg7_GL \| GuardGunUDP_HG |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | GuardNPC_Digger_CloseCombat<br>GuardNPC_Neutral_CloseCombat | 5 | WeaponPrimary | All / ungated × Newbie / Experienced \| All / ungated × Veteran \| All / ungated × Master | Weight: 1000 | 0.25-0.5 | 0-2 \| 0-7 | GuardGunM860_SG \| GuardGunObrez_SG \| GuardGunSPSA_SG \| GuardGunTOZ_SG |
| `GuardNPC_Neutral_Sniper_ItemGenerator` | GuardNPC_Digger_Sniper<br>GuardNPC_Neutral_Sniper | 3 | WeaponPistol \| WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master \| All / ungated × All / ungated | Weight: 1, 1000 | 0.25-0.5 | 0-2 \| 2-4 | GuardGunPM_HG \| GuardGunSVDM_SP \| GuardGunUDP_HG |
| `GuardNPC_Neutral_Stormtrooper_ItemGenerator` | GuardNPC_Digger_Stormtrooper<br>GuardNPC_Neutral_Stormtrooper | 4 | WeaponPrimary | All / ungated × Newbie / Experienced \| All / ungated × Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-5 \| 30-45 | GuardGunAK74_ST \| GuardGunFora_ST \| GuardGunG37_ST \| GuardGunM16_ST |
| `GuardNPC_Noon_CloseCombat_ItemGenerator` | GuardNPC_Noon_CloseCombat | 2 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-2 | GuardGunM860_SG \| GuardGunSPSA_SG |
| `GuardNPC_Noon_Recon_ItemGenerator` | GuardNPC_Noon_Recon | 1 | WeaponPrimary | All / ungated × All / ungated | Weight: 1000 | 0.25-0.5 | 30-45 | GuardGunViper_PP |
| `GuardNPC_Noon_Sniper_ItemGenerator` | GuardNPC_Noon_Sniper | 4 | WeaponPistol \| WeaponPrimary | All / ungated × Newbie / Experienced / Veteran / Master \| All / ungated × All / ungated | Weight: 1, 500, 1000 | 0.25-0.5 | 0-2 \| 2-4 | GuardGunMark_SP \| GuardGunSKP_DMR \| GuardGunSVDM_SP \| GuardGunUDP_HG |
| `GuardNPC_Noon_Stormtrooper_ItemGenerator` | GuardNPC_Noon_Stormtrooper | 4 | WeaponPrimary | All / ungated × Newbie \| All / ungated × Experienced / Veteran / Master | Weight: 1000 | 0.25-0.5 | 0-5 \| 30-45 | GuardGunAK74_ST \| GuardGunG37_ST \| GuardGunGrim_ST \| GuardGunM16_ST |
| `GuardNPC_Varta_CloseCombat_ItemGenerator` | GuardNPC_Varta_CloseCombat | 2 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master | Weight: 1000 | 0.25-0.5 | 0-2 \| 15-20 | GuardGunM860_SG \| GuardGunRam2_SG |
| `GuardNPC_Varta_Heavy_ItemGenerator` | GuardNPC_Varta_Heavy | 1 | WeaponPrimary | All / ungated × All / ungated | Weight: 1000 | 0.25-0.5 | 25-35 | GuardGunPKP_MG |
| `GuardNPC_Varta_Recon_ItemGenerator` | GuardNPC_Varta_Recon | 2 | WeaponPrimary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master | Weight: 1000 | 0.25-0.5 | 0-5 \| 30-45 | GuardGunAKU_PP \| GuardGunIntegral_PP |
| `GuardNPC_Varta_Sniper_ItemGenerator` | GuardNPC_Varta_Sniper | 5 | WeaponPistol \| WeaponPrimary \| WeaponSecondary | All / ungated × Newbie / Experienced / Veteran \| All / ungated × Master \| All / ungated × All / ungated | Weight: 1, 1000 | 0.25-0.5 | 0-2 \| 0-5 \| 2-4 \| 10-15 \| 30-45 | GuardGunAKU_PP \| GuardGunIntegral_PP \| GuardGunM701_SP \| GuardGunMark_SP \| GuardGunUDP_HG |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | GuardNPC_Varta_Stormtrooper | 5 | WeaponPistol \| WeaponPrimary \| WeaponSecondary | All / ungated × Newbie / Experienced \| All / ungated × Veteran \| All / ungated × Master \| All / ungated × All / ungated | Weight: 1, 1000 | 0.25-0.5 | 0-1 \| 0-5 \| 2-4 \| 30-45 | GuardGunAK74_ST \| GuardGunFora_ST \| GuardGunKharod_ST \| GuardGunRpg7_GL \| GuardGunUDP_HG |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | 11 | WeaponPrimary | All / ungated × Newbie / Experienced \| Novice / Stalker / Veteran × Veteran \| Novice / Stalker / Veteran × Master \| Master × Veteran \| Master × Master | Weight: 100, 300, 500, 1000 | 0.25-0.5 | 0-5 \| 1-4 \| 2-4 | GunM860_SG \| GunRam2_SG \| GunSPSA_SG |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | 10 | WeaponPrimary | Novice / Stalker / Veteran × Newbie / Experienced \| Novice / Stalker / Veteran × Veteran \| Novice / Stalker / Veteran × Master \| Master × Newbie / Experienced \| Master × Veteran \| Master × Master | Weight: 200, 600, 1000 | 0.25-0.5 | 0-6 \| 0-7 | GunIntegral_PP \| GunViper_PP |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | 11 | WeaponPrimary | All / ungated × Newbie / Experienced \| Novice / Stalker / Veteran × Veteran \| Novice / Stalker / Veteran × Master \| Master × Veteran \| Master × Master | Weight: 600, 1000 | 0.25-0.5 | 0-2 \| 0-5 \| 3-5 | GunGP3A_DMR \| GunM701_SP \| GunMark_SP |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | 16 | WeaponPrimary | All / ungated × Newbie / Experienced \| Novice / Stalker / Veteran × Veteran \| Novice / Stalker / Veteran × Master \| Master × Veteran \| Master × Master | Weight: 100, 400, 500, 1000 | 0.25-0.5 | 0-5 \| 0-6 \| 0-7 | GunArev_ST \| GunG37_ST \| GunKharod_ST \| GunM16_ST |

## Complete direct weapon-entry table

This is the exact direct target surface. No prefix matching is needed: every source row that a future allowlisted standard-human weapon transform could touch is enumerated here.

| Generator | Consumer(s) | Cat | Diff | Rank | Weapon | Selection | Durability | Attached ammo | Exact max key | CFG line |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | ---: |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` | GeneralNPC_Bandit_Recon_Prolog_AKU | WeaponPrimary | All / ungated | Newbie | `GunAKU_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 53132 |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` | GeneralNPC_Bandit_Recon_Prolog_AKU | WeaponPrimary | All / ungated | Experienced / Veteran / Master | `GunAKU_PP` | Weight=3 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 53146 |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` | GeneralNPC_Bandit_Recon_Prolog_Obrez | WeaponPrimary | All / ungated | Newbie | `GunObrez_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 53034 |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` | GeneralNPC_Bandit_Recon_Prolog_Obrez | WeaponPrimary | All / ungated | Experienced / Veteran / Master | `GunObrez_SG` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 53048 |
| `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` | GeneralNPC_Neutral_SniperCustom | WeaponPrimary | All / ungated | Newbie | `GunThreeLine_SP` | Chance=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 68853 |
| `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` | GeneralNPC_Neutral_SniperCustom | WeaponPrimary | All / ungated | Experienced | `GunSVDM_SP` | Chance=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 68867 |
| `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` | GeneralNPC_Neutral_SniperCustom | WeaponPrimary | All / ungated | Veteran | `GunSKP_DMR` | Chance=1 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 68881 |
| `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` | GeneralNPC_Neutral_SniperCustom | WeaponPrimary | All / ungated | Master | `GunSKP_DMR` | Chance=1 | 0.25–0.5 | 3–5 | `AmmoMaxCount` | 68895 |
| `GeneralNPC_SIRCAA_Scientist_Recon_ItemGenerator` | GeneralNPC_SIRCAA_Scientist_Recon | WeaponPistol | All / ungated | Newbie / Experienced / Veteran / Master | `GunUDP_HG` | Weight=1000 | 0.25–0.5 | 0–3 | `AmmoMaxCount` | 53783 |
| `GuardNPC_Bandit_CloseCombat_ItemGenerator` | GuardNPC_SultanBandit_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunObrez_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 46603 |
| `GuardNPC_Bandit_CloseCombat_ItemGenerator` | GuardNPC_SultanBandit_CloseCombat | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunSPSA_SG` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 46617 |
| `GuardNPC_Bandit_Heavy_ItemGenerator` | GuardNPC_SultanBandit_Heavy | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunPKP_MG` | Weight=1000 | 0.25–0.5 | 0–20 | `AmmoMaxCount` | 46877 |
| `GuardNPC_Bandit_Recon_ItemGenerator` | GuardNPC_SultanBandit_Recon | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunViper_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 46677 |
| `GuardNPC_Bandit_Recon_ItemGenerator` | GuardNPC_SultanBandit_Recon | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunAKU_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 46685 |
| `GuardNPC_Bandit_Stormtrooper_ItemGenerator` | GuardNPC_SultanBandit_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunAK74_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 46781 |
| `GuardNPC_Corpus_CloseCombat_ItemGenerator` | GuardNPC_Corpus_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunD12_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 49327 |
| `GuardNPC_Corpus_CloseCombat_ItemGenerator` | GuardNPC_Corpus_CloseCombat | WeaponPrimary | All / ungated | Master | `GuardGunRam2_SG` | Weight=1000 | 0.25–0.5 | 4–6 | `AmmoMaxCount` | 49341 |
| `GuardNPC_Corpus_Heavy_ItemGenerator` | GuardNPC_Corpus_Heavy | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunPKP_MG` | Weight=1000 | 0.25–0.5 | 0–20 | `AmmoMaxCount` | 49797 |
| `GuardNPC_Corpus_Heavy_ItemGenerator` | GuardNPC_Corpus_Heavy | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunPKP_MG` | Weight=1000 | 0.25–0.5 | 25–35 | `AmmoMaxCount` | 49811 |
| `GuardNPC_Corpus_Recon_ItemGenerator` | GuardNPC_Corpus_Recon | WeaponPrimary | All / ungated | Newbie | `GuardGunAKU_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 49401 |
| `GuardNPC_Corpus_Recon_ItemGenerator` | GuardNPC_Corpus_Recon | WeaponPrimary | All / ungated | Experienced | `GuardGunBucket_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 49415 |
| `GuardNPC_Corpus_Recon_ItemGenerator` | GuardNPC_Corpus_Recon | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunZubr_PP` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 49429 |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunSVU_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 49679 |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | WeaponPrimary | All / ungated | Master | `GuardGunSVU_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 49693 |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | WeaponPrimary | All / ungated | Master | `GuardGunGP3A_DMR` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 49701 |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | WeaponPistol | All / ungated | Newbie / Experienced / Veteran | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–3 | `AmmoMaxCount` | 49715 |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | WeaponPistol | All / ungated | Master | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 49729 |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | WeaponPistol | All / ungated | Master | `GuardGunAPB_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 49737 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunFora_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 49511 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunGrim_ST` | Weight=1000 | 0.35–0.45 | 0–5 | `AmmoMaxCount` | 49519 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponPrimary | All / ungated | Veteran | `GuardGunGvintar_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 49533 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponPrimary | All / ungated | Master | `GuardGunDnipro_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 49547 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponSecondary | All / ungated | Master | `GuardGunRpg7_GL` | Weight=1000 | 0.25–0.5 | 0–1 | `AmmoMaxCount` | 49561 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponPistol | All / ungated | Newbie / Experienced | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–3 | `AmmoMaxCount` | 49575 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponPistol | All / ungated | Veteran / Master | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 49589 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | WeaponPistol | All / ungated | Veteran / Master | `GuardGunAPB_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 49597 |
| `GuardNPC_Duty_CloseCombat_ItemGenerator` | GuardNPC_Duty_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunM860_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47461 |
| `GuardNPC_Duty_CloseCombat_ItemGenerator` | GuardNPC_Duty_CloseCombat | WeaponPrimary | All / ungated | Master | `GuardGunD12_SG` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 47475 |
| `GuardNPC_Duty_Heavy_ItemGenerator` | GuardNPC_Duty_Heavy | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunPKP_MG` | Weight=1000 | 0.25–0.5 | 0–20 | `AmmoMaxCount` | 47877 |
| `GuardNPC_Duty_Recon_ItemGenerator` | GuardNPC_Duty_Recon | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunZubr_PP` | Weight=1000 | 0.25–0.5 | 0–10 | `AmmoMaxCount` | 47535 |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunSVDM_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47759 |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunSVDM_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47773 |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunSKP_DMR` | Weight=500 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47781 |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | WeaponPistol | All / ungated | Newbie / Experienced / Veteran | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–3 | `AmmoMaxCount` | 47795 |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | WeaponPistol | All / ungated | Master | `GuardGunAPB_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 47809 |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | WeaponPistol | All / ungated | Master | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 47817 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunAK74_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 47608 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunGrim_ST` | Weight=100 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 47616 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPrimary | All / ungated | Veteran | `GuardGunGrim_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 47630 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPrimary | All / ungated | Veteran | `GuardGunGvintar_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 47638 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPrimary | All / ungated | Master | `GuardGunLavina_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 47652 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPrimary | All / ungated | Master | `GuardGunGrim_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 47660 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPistol | All / ungated | Newbie / Experienced / Veteran | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–3 | `AmmoMaxCount` | 47674 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPistol | All / ungated | Master | `GuardGunAPB_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 47688 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | WeaponPistol | All / ungated | Master | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 47696 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunSVDM_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 48295 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponPrimary | All / ungated | Veteran | `GuardGunM701_SP` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 48309 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponPrimary | All / ungated | Master | `GuardGunM701_SP` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 48323 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponPrimary | All / ungated | Master | `GuardGunGP3A_DMR` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 48331 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponSecondary | All / ungated | All / ungated | `GuardGunViper_PP` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 48344 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponPistol | All / ungated | Newbie | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 48358 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponPistol | All / ungated | Experienced / Veteran / Master | `GuardGunM10_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 48372 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | WeaponPistol | All / ungated | Experienced / Veteran / Master | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 48380 |
| `GuardNPC_Monolith_CloseCombat_ItemGenerator` | GuardNPC_Monolith_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunM860_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 46937 |
| `GuardNPC_Monolith_CloseCombat_ItemGenerator` | GuardNPC_Monolith_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunSPSA_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 46945 |
| `GuardNPC_Monolith_CloseCombat_ItemGenerator` | GuardNPC_Monolith_CloseCombat | WeaponPrimary | All / ungated | Master | `GuardGunD12_SG` | Weight=1000 | 0.25–0.5 | 15–20 | `AmmoMaxCount` | 46959 |
| `GuardNPC_Monolith_CloseCombat_ItemGenerator` | GuardNPC_Monolith_CloseCombat | WeaponPrimary | All / ungated | Master | `GuardGunRam2_SG` | Weight=1000 | 0.25–0.5 | 4–6 | `AmmoMaxCount` | 46967 |
| `GuardNPC_Monolith_Recon_ItemGenerator` | GuardNPC_Monolith_Recon | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunViper_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 47027 |
| `GuardNPC_Monolith_Recon_ItemGenerator` | GuardNPC_Monolith_Recon | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunIntegral_PP` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 47041 |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunSVDM_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47350 |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunMark_SP` | Weight=300 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47358 |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunSKP_DMR` | Weight=100 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47366 |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | WeaponPrimary | All / ungated | Master | `GuardGunMark_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47380 |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | WeaponPrimary | All / ungated | Master | `GuardGunGP3A_DMR` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47388 |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | WeaponPistol | All / ungated | All / ungated | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 47401 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Newbie | `GuardGunAK74_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 47137 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Newbie | `GuardGunM16_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 47145 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Experienced | `GuardGunG37_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 47159 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Experienced | `GuardGunGrim_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxcount` | 47167 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Veteran | `GuardGunG37_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxcount` | 47181 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Veteran | `GuardGunGrim_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxcount` | 47189 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Veteran | `GuardGunGvintar_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 47197 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Master | `GuardGunKharod_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 47211 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPrimary | All / ungated | Master | `GuardGunLavina_ST` | Weight=1000 | 0.25–0.5 | 8–12 | `AmmoMaxCount` | 47219 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponSecondary | All / ungated | Master | `GuardGunRpg7_GL` | Weight=10 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 47233 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponSecondary | All / ungated | Master | `GuardGunGauss_SP` | Weight=5 | 0.25–0.5 | 10–15 | `AmmoMaxCount` | 47241 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | WeaponPistol | All / ungated | All / ungated | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 47254 |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | GuardNPC_Digger_CloseCombat<br>GuardNPC_Neutral_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunObrez_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 45896 |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | GuardNPC_Digger_CloseCombat<br>GuardNPC_Neutral_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunTOZ_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 45904 |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | GuardNPC_Digger_CloseCombat<br>GuardNPC_Neutral_CloseCombat | WeaponPrimary | All / ungated | Veteran | `GuardGunM860_SG` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 45918 |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | GuardNPC_Digger_CloseCombat<br>GuardNPC_Neutral_CloseCombat | WeaponPrimary | All / ungated | Master | `GuardGunM860_SG` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 45932 |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | GuardNPC_Digger_CloseCombat<br>GuardNPC_Neutral_CloseCombat | WeaponPrimary | All / ungated | Master | `GuardGunSPSA_SG` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 45940 |
| `GuardNPC_Neutral_Sniper_ItemGenerator` | GuardNPC_Digger_Sniper<br>GuardNPC_Neutral_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunSVDM_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 46272 |
| `GuardNPC_Neutral_Sniper_ItemGenerator` | GuardNPC_Digger_Sniper<br>GuardNPC_Neutral_Sniper | WeaponPistol | All / ungated | All / ungated | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 46285 |
| `GuardNPC_Neutral_Sniper_ItemGenerator` | GuardNPC_Digger_Sniper<br>GuardNPC_Neutral_Sniper | WeaponPistol | All / ungated | All / ungated | `GuardGunPM_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 46293 |
| `GuardNPC_Neutral_Stormtrooper_ItemGenerator` | GuardNPC_Digger_Stormtrooper<br>GuardNPC_Neutral_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunAK74_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 46418 |
| `GuardNPC_Neutral_Stormtrooper_ItemGenerator` | GuardNPC_Digger_Stormtrooper<br>GuardNPC_Neutral_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunM16_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 46426 |
| `GuardNPC_Neutral_Stormtrooper_ItemGenerator` | GuardNPC_Digger_Stormtrooper<br>GuardNPC_Neutral_Stormtrooper | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunG37_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 46440 |
| `GuardNPC_Neutral_Stormtrooper_ItemGenerator` | GuardNPC_Digger_Stormtrooper<br>GuardNPC_Neutral_Stormtrooper | WeaponPrimary | All / ungated | Veteran / Master | `GuardGunFora_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 46448 |
| `GuardNPC_Noon_CloseCombat_ItemGenerator` | GuardNPC_Noon_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunM860_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 48949 |
| `GuardNPC_Noon_CloseCombat_ItemGenerator` | GuardNPC_Noon_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunSPSA_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 48957 |
| `GuardNPC_Noon_Recon_ItemGenerator` | GuardNPC_Noon_Recon | WeaponPrimary | All / ungated | All / ungated | `GuardGunViper_PP` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 49016 |
| `GuardNPC_Noon_Sniper_ItemGenerator` | GuardNPC_Noon_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunSVDM_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 49238 |
| `GuardNPC_Noon_Sniper_ItemGenerator` | GuardNPC_Noon_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunMark_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 49246 |
| `GuardNPC_Noon_Sniper_ItemGenerator` | GuardNPC_Noon_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran / Master | `GuardGunSKP_DMR` | Weight=500 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 49254 |
| `GuardNPC_Noon_Sniper_ItemGenerator` | GuardNPC_Noon_Sniper | WeaponPistol | All / ungated | All / ungated | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 49267 |
| `GuardNPC_Noon_Stormtrooper_ItemGenerator` | GuardNPC_Noon_Stormtrooper | WeaponPrimary | All / ungated | Newbie | `GuardGunAK74_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 49112 |
| `GuardNPC_Noon_Stormtrooper_ItemGenerator` | GuardNPC_Noon_Stormtrooper | WeaponPrimary | All / ungated | Newbie | `GuardGunM16_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 49120 |
| `GuardNPC_Noon_Stormtrooper_ItemGenerator` | GuardNPC_Noon_Stormtrooper | WeaponPrimary | All / ungated | Experienced / Veteran / Master | `GuardGunG37_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 49134 |
| `GuardNPC_Noon_Stormtrooper_ItemGenerator` | GuardNPC_Noon_Stormtrooper | WeaponPrimary | All / ungated | Experienced / Veteran / Master | `GuardGunGrim_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 49142 |
| `GuardNPC_Varta_CloseCombat_ItemGenerator` | GuardNPC_Varta_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunM860_SG` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 48440 |
| `GuardNPC_Varta_CloseCombat_ItemGenerator` | GuardNPC_Varta_CloseCombat | WeaponPrimary | All / ungated | Master | `GuardGunRam2_SG` | Weight=1000 | 0.25–0.5 | 15–20 | `AmmoMaxCount` | 48454 |
| `GuardNPC_Varta_Heavy_ItemGenerator` | GuardNPC_Varta_Heavy | WeaponPrimary | All / ungated | All / ungated | `GuardGunPKP_MG` | Weight=1000 | 0.25–0.5 | 25–35 | `AmmoMaxCount` | 48889 |
| `GuardNPC_Varta_Recon_ItemGenerator` | GuardNPC_Varta_Recon | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunAKU_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 48514 |
| `GuardNPC_Varta_Recon_ItemGenerator` | GuardNPC_Varta_Recon | WeaponPrimary | All / ungated | Master | `GuardGunIntegral_PP` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 48528 |
| `GuardNPC_Varta_Sniper_ItemGenerator` | GuardNPC_Varta_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced / Veteran | `GuardGunMark_SP` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 48775 |
| `GuardNPC_Varta_Sniper_ItemGenerator` | GuardNPC_Varta_Sniper | WeaponPrimary | All / ungated | Master | `GuardGunM701_SP` | Weight=1000 | 0.25–0.5 | 10–15 | `AmmoMaxCount` | 48789 |
| `GuardNPC_Varta_Sniper_ItemGenerator` | GuardNPC_Varta_Sniper | WeaponSecondary | All / ungated | Newbie / Experienced / Veteran | `GuardGunAKU_PP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 48803 |
| `GuardNPC_Varta_Sniper_ItemGenerator` | GuardNPC_Varta_Sniper | WeaponSecondary | All / ungated | Master | `GuardGunIntegral_PP` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 48817 |
| `GuardNPC_Varta_Sniper_ItemGenerator` | GuardNPC_Varta_Sniper | WeaponPistol | All / ungated | All / ungated | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 48830 |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | GuardNPC_Varta_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GuardGunAK74_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 48624 |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | GuardNPC_Varta_Stormtrooper | WeaponPrimary | All / ungated | Veteran | `GuardGunFora_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 48638 |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | GuardNPC_Varta_Stormtrooper | WeaponPrimary | All / ungated | Master | `GuardGunKharod_ST` | Weight=1000 | 0.25–0.5 | 30–45 | `AmmoMaxCount` | 48652 |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | GuardNPC_Varta_Stormtrooper | WeaponSecondary | All / ungated | Master | `GuardGunRpg7_GL` | Weight=1000 | 0.25–0.5 | 0–1 | `AmmoMaxCount` | 48666 |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | GuardNPC_Varta_Stormtrooper | WeaponPistol | All / ungated | All / ungated | `GuardGunUDP_HG` | Weight=1 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 48679 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | All / ungated | Newbie / Experienced | `GunM860_SG` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 17861 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunSPSA_SG` | Weight=100 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 17880 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunM860_SG` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 17888 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunRam2_SG` | Weight=500 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 17903 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunSPSA_SG` | Weight=1000 | 0.25–0.5 | 2–4 | `AmmoMaxCount` | 17911 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunM860_SG` | Weight=300 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 17919 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Master | Veteran | `GunSPSA_SG` | Weight=100 | 0.25–0.5 | 1–4 | `AmmoMaxCount` | 17934 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Master | Veteran | `GunM860_SG` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 17942 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Master | Master | `GunRam2_SG` | Weight=500 | 0.25–0.5 | 1–4 | `AmmoMaxCount` | 17957 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Master | Master | `GunSPSA_SG` | Weight=1000 | 0.25–0.5 | 1–4 | `AmmoMaxCount` | 17965 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | WeaponPrimary | Master | Master | `GunM860_SG` | Weight=300 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 17973 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Novice / Stalker / Veteran | Newbie / Experienced | `GunViper_PP` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18047 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunViper_PP` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18066 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunIntegral_PP` | Weight=200 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18074 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunViper_PP` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18089 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunIntegral_PP` | Weight=600 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18097 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Master | Newbie / Experienced | `GunViper_PP` | Weight=1000 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18112 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Master | Veteran | `GunViper_PP` | Weight=1000 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18127 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Master | Veteran | `GunIntegral_PP` | Weight=200 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18135 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Master | Master | `GunViper_PP` | Weight=1000 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18150 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | WeaponPrimary | Master | Master | `GunIntegral_PP` | Weight=600 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18158 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | All / ungated | Newbie / Experienced | `GunMark_SP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18456 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunM701_SP` | Weight=600 | 0.25–0.5 | 3–5 | `AmmoMaxCount` | 18475 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunMark_SP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18483 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunM701_SP` | Weight=600 | 0.25–0.5 | 3–5 | `AmmoMaxCount` | 18498 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunMark_SP` | Weight=1000 | 0.25–0.5 | 3–5 | `AmmoMaxCount` | 18506 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunGP3A_DMR` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 18514 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Master | Veteran | `GunM701_SP` | Weight=600 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18529 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Master | Veteran | `GunMark_SP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18537 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Master | Master | `GunM701_SP` | Weight=600 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18552 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Master | Master | `GunMark_SP` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18560 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | WeaponPrimary | Master | Master | `GunGP3A_DMR` | Weight=1000 | 0.25–0.5 | 0–2 | `AmmoMaxCount` | 18568 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GunM16_ST` | Weight=1000 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18231 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | All / ungated | Newbie / Experienced | `GunG37_ST` | Weight=100 | 0.25–0.5 | 0–5 | `AmmoMaxCount` | 18243 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunM16_ST` | Weight=500 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18258 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunG37_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18266 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Novice / Stalker / Veteran | Veteran | `GunArev_ST` | Weight=500 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18274 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunKharod_ST` | Weight=500 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18289 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunM16_ST` | Weight=400 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18297 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunG37_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18305 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Novice / Stalker / Veteran | Master | `GunArev_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18313 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Master | Veteran | `GunM16_ST` | Weight=500 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18328 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Master | Veteran | `GunG37_ST` | Weight=1000 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18336 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Master | Veteran | `GunArev_ST` | Weight=500 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18344 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Master | Master | `GunKharod_ST` | Weight=500 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18359 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Master | Master | `GunM16_ST` | Weight=400 | 0.25–0.5 | 0–6 | `AmmoMaxCount` | 18367 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Master | Master | `GunG37_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18375 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | WeaponPrimary | Master | Master | `GunArev_ST` | Weight=1000 | 0.25–0.5 | 0–7 | `AmmoMaxCount` | 18383 |

## Shared nested pistol pools reached by standard-human loadouts

These are real weapon pools in the standard-human reachability graph, but they are **not safe standard-NPC-only edit points** because many other generator families also reference them.

| Pool | Standard-human links | All incoming refs | Named/scripted | Zombies | Quest/scripted | Shared parents | Weapons | Decision |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
| `GeneralNPC_Bandit_WeaponPistol` | 2 | 160 | 136 | 12 | 1 | 9 | GunPM_HG | **EXCLUDE direct pool edits** |
| `GeneralNPC_Freedom_WeaponPistol` | 4 | 82 | 64 | 9 | 0 | 5 | GunKora_HG \| GunM10_HG \| GunUDP_HG | **EXCLUDE direct pool edits** |

### Standard-human links into those pools

| Standard loadout | Consumer(s) | Shared pistol pool | Edge field | CFG line |
| --- | --- | --- | --- | ---: |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` | GeneralNPC_Bandit_Recon_Prolog_AKU | `GeneralNPC_Bandit_WeaponPistol` | Weight=1 | 53122 |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` | GeneralNPC_Bandit_Recon_Prolog_Obrez | `GeneralNPC_Bandit_WeaponPistol` | Weight=1 | 53024 |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | `GeneralNPC_Freedom_WeaponPistol` | Chance=0.3 | 17990 |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | `GeneralNPC_Freedom_WeaponPistol` | Chance=0.25 | 18175 |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | `GeneralNPC_Freedom_WeaponPistol` | Chance=0.5 | 18585 |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | `GeneralNPC_Freedom_WeaponPistol` | Chance=0.4 | 18400 |

### Complete shared-pool weapon rows

| Pool | Diff | Rank | Weapon | Selection | Durability | Attached ammo | CFG line |
| --- | --- | --- | --- | --- | --- | --- | ---: |
| `GeneralNPC_Bandit_WeaponPistol` | All / ungated | Newbie | `GunPM_HG` | Weight=1 | 0.25–0.5 | 0–3 | 50079 |
| `GeneralNPC_Bandit_WeaponPistol` | All / ungated | Experienced | `GunPM_HG` | Weight=1 | 0.25–0.5 | 0–7 | 50093 |
| `GeneralNPC_Bandit_WeaponPistol` | All / ungated | Veteran | `GunPM_HG` | Weight=1 | 0.25–0.5 | 0–7 | 50107 |
| `GeneralNPC_Freedom_WeaponPistol` | All / ungated | Newbie / Experienced | `GunUDP_HG` | Weight=1 | 0.25–0.5 | 0–3 | 50433 |
| `GeneralNPC_Freedom_WeaponPistol` | All / ungated | Veteran | `GunUDP_HG` | Weight=1 | 0.25–0.5 | 0–3 | 50447 |
| `GeneralNPC_Freedom_WeaponPistol` | All / ungated | Veteran | `GunM10_HG` | Weight=0.1 | 0.25–0.5 | 0–3 | 50455 |
| `GeneralNPC_Freedom_WeaponPistol` | All / ungated | Veteran | `GunKora_HG` | Weight=0.1 | 0.25–0.5 | 0–3 | 50463 |
| `GeneralNPC_Freedom_WeaponPistol` | All / ungated | Master | `GunUDP_HG` | Weight=1 | 0.25–0.5 | 0–3 | 50477 |
| `GeneralNPC_Freedom_WeaponPistol` | All / ungated | Master | `GunM10_HG` | Weight=1 | 0.25–0.5 | 0–3 | 50485 |
| `GeneralNPC_Freedom_WeaponPistol` | All / ungated | Master | `GunKora_HG` | Weight=1 | 0.25–0.5 | 0–3 | 50493 |

## Separate loose-ammo note

The standard-human group also contains **1 explicit `Ammo` category row**, separate from attached-ammo fields: `A545D` in `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog` at CFG line 248818, `Chance=1`, `MinCount=30`, `MaxCount=30`. This is **not** part of the attached-ammo audit and is tracked as a separate domain so the two mechanics are never conflated.

## What future controls would actually mean

| Candidate | Exact target surface | Standard-NPC-only? | Source relationship | Decision |
| --- | --- | --- | --- | --- |
| Direct Standard NPC Weapon Condition | `MinDurability/MaxDurability` on the 173 enumerated direct weapon rows | Yes, for those direct rows | direct per-entry fields, but aggregate UI scaling is our transform | **Good structural candidate**, runtime-test final dropped-weapon behavior before calling it confirmed. Does not cover shared nested-pool pistols. |
| Direct Standard NPC Attached Ammo | `AmmoMinCount` + exact max-key spelling on the 173 direct rows | Yes, for those direct rows | direct per-entry fields, but aggregate UI scaling is our transform | **Good structural candidate**, runtime-test gameplay semantics. Does not cover shared nested-pool pistols. |
| Standard NPC Weapon Selection / rarity | `Weight` on 169 rows plus `Chance` on 4 custom-sniper rows | Yes for direct rows | mixed selection mechanisms and rank/difficulty branches | **Do not collapse to one percentage yet.** Requires an explicit selection-policy design. |
| All Standard NPC Weapon Condition / Ammo including nested pistols | direct rows + shared pistol-pool rows | No if pools are edited in place | nested pools are reused by named NPCs, zombies and quests | **Not safe as an in-place global edit.** Clone/rewire or explicitly narrow the control. |
| Clone shared pistol pools and rewire six standard-human links | new custom pool definitions + six source-link rewires | Yes | authored generator redesign | Technically feasible later, but not a source-1:1 control. |

## Audit decision

- **173 direct weapon rows / 39 loadout generators:** structurally safe standard-human-only allowlist surface.
- **6 links to 2 shared pistol pools:** pool definitions excluded from standard-NPC-only direct edits.
- **51 direct weapon SIDs; 54 reachable weapon SIDs when shared nested pistols are included.**
- **Durability and attached-ammo fields are direct source fields**, but a single UI slider over many entries is still an aggregate transform and needs runtime validation of the final in-game effect.
- **Weapon selection is not one uniform mechanic:** most direct rows use `Weight`, four use `Chance`; no generic selection slider is approved.
- The three `AmmoMaxcount` source-key anomalies must be addressed by exact key, not silently normalized in a future writer.

Machine-readable companions: `npc-weapon-loadouts.csv`, `npc-weapon-direct-entries.csv`, `npc-weapon-nested-links.csv`, `npc-weapon-nested-pools.csv`, `npc-weapon-nested-pool-entries.csv`, `npc-weapon-item-matrix.csv`, and `npc-loose-ammo-direct-entries.csv`.
