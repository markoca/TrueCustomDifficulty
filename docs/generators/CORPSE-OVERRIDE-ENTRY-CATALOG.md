# Corpse Override — Exact Local Source Entry Catalog

Every row below is physically authored in one of the 39 ItemGenerator roots directly referenced by `SpawnActorPrototypes.CorpseStashSID`. Use this catalog for exact source structure; do not infer engine probability semantics from `Weight` or `Chance`.

| Scope | Generator | Category | Diff | Rank | Item / nested generator | Weight | Chance | Count | Durability | Ammo | Source line |
| --- | --- | --- | --- | --- | --- | ---: | ---: | --- | --- | --- | ---: |
| base | `ARG01_QSBIG_Body1` | Ammo | — | ERank::Newbie, ERank::Experienced, ERank::Veteran, ERank::Master | `A545E` | — | 1 | 90–90 | — | — | 265040 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | — | ERank::Newbie | `GunViper_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 265054 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | — | ERank::Newbie | `GunAKU_PP` | 100 | — | — | 0.25–0.5 | 0–5 | 265062 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | — | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 265076 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | — | ERank::Experienced | `GunG37_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 265084 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | — | ERank::Veteran | `GunAK74_ST` | 500 | — | — | 0.25–0.5 | 0–5 | 265098 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | — | ERank::Veteran | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 265106 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 265121 |
| base | `ARG01_QSBIG_Body1` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 265136 |
| base | `ARG01_QSBIG_Body1` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_Armor` | 1 | — | — | — | — | 265149 |
| base | `ARG01_QSBIG_Body1` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | 1 | — | — | — | — | 265158 |
| base | `ARG01_QSBIG_Body1` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 265167 |
| base | `ARG01_QSBIG_Body1` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 265178 |
| base | `ARG01_QSBIG_Body1` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_NVG` | — | 1 | — | — | — | 265188 |
| base | `BanditChernozem_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_WeaponPistol` | — | 0.3 | — | — | — | 143703 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunObrez_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 143713 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunTOZ_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 143727 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunM860_SG` | 100 | — | — | 0.25–0.5 | 0–2 | 143735 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunM10_HG` | 300 | — | — | 0.25–0.5 | 0–7 | 143743 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran, ERank::Master | `GunSPSA_SG` | 120 | — | — | 0.25–0.5 | 2–4 | 143758 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran, ERank::Master | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 143766 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran, ERank::Master | `GunSPSA_SG` | 120 | — | — | 0.25–0.5 | 1–4 | 143781 |
| base | `BanditChernozem_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran, ERank::Master | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 143789 |
| base | `BanditChernozem_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_Armor` | 1 | — | — | — | — | 143802 |
| base | `BanditChernozem_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_CloseCombat` | 1 | — | — | — | — | 143811 |
| base | `BanditChernozem_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 143820 |
| base | `BanditChernozem_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 143830 |
| base | `BanditChernozem_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_NVG` | — | 1 | — | — | — | 143841 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 2185 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ RUCloseCombatWeaponNewbieGenerator` | 20.0 | — | — | — | — | 2194 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ RUScoutWeaponNewbieGenerator` | 20.0 | — | — | — | — | 2198 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ RUStormTrooperWeaponNewbieGenerator` | 40.0 | — | — | — | — | 2202 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ RUMachineGunnerWeaponNewbieGenerator` | 10.0 | — | — | — | — | 2206 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ RUSniperWeaponNewbieGenerator` | 10.0 | — | — | — | — | 2210 |
| base | `DutyNewbieItemGenerator` | BodyArmor | — | — | `Rook_Dolg_Armor` | 100.0 | — | — | 1.0–1.0 | — | 2219 |
| base | `DutyNewbieItemGenerator` | Head | — | — | `Light_Duty_Helmet` | 50.0 | — | — | 1.0–1.0 | — | 2230 |
| base | `DutyNewbieItemGenerator` | Head | — | — | `Heavy_Duty_Helmet` | 50.0 | — | — | 1.0–1.0 | — | 2236 |
| base | `DutyNewbieItemGenerator` | Head | — | — | `empty` | 50.0 | — | — | 1.0–1.0 | — | 2242 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ ConsumableNewbieGenerator` | — | 1.0 | — | — | — | 2257 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ DetectorNewbieGenerator` | — | 0.05 | — | — | — | 2261 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ ArtifactNewbieGenerator` | — | 0.01 | — | — | — | 2265 |
| base | `DutyNewbieItemGenerator` | SubItemGenerator | — | — | `→ AttachNewbieGenerator` | — | 0.01 | — | — | — | 2269 |
| base | `EmptyQuest` | None | — | — | `empty` | 4.f | — | 0–0 | 0.0–0.0 | 0–0 | 80184 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 2638 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ENCloseCombatWeaponExperiencedGenerator` | 20.0 | — | — | — | — | 2647 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ENScoutWeaponExperiencedGenerator` | 20.0 | — | — | — | — | 2651 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ENStormTrooperWeaponExperiencedGenerator` | 40.0 | — | — | — | — | 2655 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ENMachineGunnerWeaponExperiencedGenerator` | 10.0 | — | — | — | — | 2659 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ENSniperWeaponExperiencedGenerator` | 10.0 | — | — | — | — | 2663 |
| base | `FreedomExperiencedItemGenerator` | BodyArmor | — | — | `Battle_Svoboda_Armor` | 100.0 | — | — | 1.0–1.0 | — | 2672 |
| base | `FreedomExperiencedItemGenerator` | Head | — | — | `Heavy_Varta_Helmet` | 50.0 | — | — | 1.0–1.0 | — | 2683 |
| base | `FreedomExperiencedItemGenerator` | Head | — | — | `Heavy_Svoboda_Helmet` | 50.0 | — | — | 1.0–1.0 | — | 2689 |
| base | `FreedomExperiencedItemGenerator` | Head | — | — | `empty` | 50.0 | — | — | 1.0–1.0 | — | 2695 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ConsumableExperiencedGenerator` | — | 1.0 | — | — | — | 2710 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ DetectorExperiencedGenerator` | — | 0.1 | — | — | — | 2714 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ArtifactExperiencedGenerator` | — | 0.05 | — | — | — | 2718 |
| base | `FreedomExperiencedItemGenerator` | SubItemGenerator | — | — | `→ AttachExperiencedGenerator` | — | 0.05 | — | — | — | 2722 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 2736 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENCloseCombatWeaponVeteranGenerator` | 20.0 | — | — | — | — | 2745 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENScoutWeaponVeteranGenerator` | 20.0 | — | — | — | — | 2749 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENStormTrooperWeaponVeteranGenerator` | 40.0 | — | — | — | — | 2753 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENMachineGunnerWeaponVeteranGenerator` | 10.0 | — | — | — | — | 2757 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENSniperWeaponVeteranGenerator` | 10.0 | — | — | — | — | 2761 |
| base | `FreedomVeteranItemGenerator` | BodyArmor | — | — | `SEVA_Svoboda_Armor` | 100.0 | — | — | 1.0–1.0 | — | 2770 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ ConsumableVeteranGenerator` | — | 1.0 | — | — | — | 2789 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ DetectorVeteranGenerator` | — | 0.25 | — | — | — | 2793 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ ArtifactVeteranGenerator` | — | 0.07 | — | — | — | 2797 |
| base | `FreedomVeteranItemGenerator` | SubItemGenerator | — | — | `→ AttachVeteranGenerator` | — | 0.07 | — | — | — | 2801 |
| base | `GarbageDetCenterCorpseItemGenerator` | Junk | — | — | `Garbage_DetentionCenter_Key_Padlock` | — | 1.0 | 1–1 | — | — | 22514 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_WeaponPistol` | — | 1.0 | — | — | — | 33800 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunViper_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 33810 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAKU_PP` | 100 | — | — | 0.25–0.5 | 0–5 | 33818 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunThreeLine_SP` | 1000 | — | — | 0.25–0.5 | 0–7 | 33826 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 33840 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunG37_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 33848 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunThreeLine_SP` | 100 | — | — | 0.25–0.5 | 0–7 | 33856 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunAK74_ST` | 500 | — | — | 0.25–0.5 | 0–5 | 33870 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 33878 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 33893 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 33908 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_Armor` | 1 | — | — | — | — | 33921 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 33930 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Heavy` | 1 | — | — | — | — | 33941 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 33950 |
| base | `GeneralNPC_Bandit_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_NVG` | — | 1 | — | — | — | 33960 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_WeaponPistol` | — | 1.0 | — | — | — | 33641 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunViper_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 33651 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAKU_PP` | 100 | — | — | 0.25–0.5 | 0–5 | 33659 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 33673 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunG37_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 33681 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunAK74_ST` | 500 | — | — | 0.25–0.5 | 0–5 | 33695 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 33703 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 33718 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 33733 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_Armor` | 1 | — | — | — | — | 33746 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | 1 | — | — | — | — | 33755 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 33764 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 33775 |
| base | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Bandit_NVG` | — | 1 | — | — | — | 33785 |
| base | `GeneralNPC_MALACHITE_Scientist_Recon_ItemGenerator` | WeaponPistol | — | ERank::Newbie, ERank::Experienced, ERank::Veteran,  ERank::Master | `GunUDP_HG` | 1000 | — | — | 0.25–0.5 | 0–3 | 49273 |
| base | `GeneralNPC_MALACHITE_Scientist_Recon_ItemGenerator` | BodyArmor | — | ERank::Newbie, ERank::Experienced, ERank::Veteran,  ERank::Master | `NPC_Sci_Armor` | 1000 | — | — | 0.35–0.55 | — | 49287 |
| base | `GeneralNPC_MALACHITE_Scientist_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Recon` | 1 | — | — | — | — | 49298 |
| base | `GeneralNPC_MALACHITE_Scientist_Recon_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 49307 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 34039 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunSPSA_SG` | 200 | — | — | 0.25–0.5 | 2–4 | 34054 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 34062 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunSPSA_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 34077 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunRam2_SG` | 100 | — | — | 0.25–0.5 | 2–4 | 34085 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunSPSA_SG` | 200 | — | — | 0.25–0.5 | 1–4 | 34100 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 1–4 | 34108 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunSPSA_SG` | 1000 | — | — | 0.25–0.5 | 1–4 | 34123 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunRam2_SG` | 100 | — | — | 0.25–0.5 | 1–4 | 34131 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Mercenaries_Armor` | 1 | — | — | — | — | 34144 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_CloseCombat` | 1 | — | — | — | — | 34153 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Mercenaries_WeaponPistol` | — | 0.35 | — | — | — | 34162 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 34171 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 34181 |
| base | `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Mercenaries_NVG` | — | 1 | — | — | — | 34192 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 34970 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Experienced, ERank::Veteran | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 0–5 | 34984 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunD12_SG` | 500 | — | — | 0.25–0.5 | 2–4 | 34998 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 0–5 | 35006 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_CloseCombat` | — | 1 | — | — | — | 35019 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.35 | — | — | — | 35023 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | ERank::Newbie, ERank::Experienced, ERank::Veteran, ERank::Master | `→ GeneralNPC_Militaries_Armor` | 1 | — | — | — | — | 35033 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 35042 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 35053 |
| base | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 35063 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 35659 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunThreeLine_SP` | 1000 | — | — | 0.25–0.5 | 0–5 | 35667 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 35682 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunGvintar_ST` | 200 | — | — | 0.25–0.5 | 4–6 | 35690 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunPKP_MG` | 100 | — | — | 0.25–0.5 | 15–25 | 35705 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 35713 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 4–6 | 35721 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 35729 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunPKP_MG` | 1000 | — | — | 0.25–0.5 | 15–25 | 35744 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 35759 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunGvintar_ST` | 200 | — | — | 0.25–0.5 | 0–6 | 35767 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunPKP_MG` | 100 | — | — | 0.25–0.5 | 15–25 | 35782 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 35790 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 35798 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 35806 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunPKP_MG` | 1000 | — | — | 0.25–0.5 | 15–25 | 35821 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_Armor` | — | 1 | — | — | — | 35834 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.1 | — | — | — | 35838 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Heavy` | 1 | — | — | — | — | 35847 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 35856 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 35866 |
| base | `GeneralNPC_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 35877 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 35079 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 35093 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunBucket_PP` | 200 | — | — | 0.25–0.5 | 0–5 | 35101 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunFora230_PP` | 100 | — | — | 0.25–0.5 | 0–7 | 35109 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunZubr_PP` | 100 | — | — | 0.25–0.5 | 0–7 | 35123 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunAKU_PP` | 800 | — | — | 0.25–0.5 | 0–5 | 35131 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunBucket_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 35139 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunFora230_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 35147 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunZubr_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 35162 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunBucket_PP` | 500 | — | — | 0.25–0.5 | 0–5 | 35170 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunFora230_PP` | 500 | — | — | 0.25–0.5 | 0–7 | 35178 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunZubr_PP` | 1000 | — | — | 0.25–0.5 | 0–6 | 35193 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunBucket_PP` | 500 | — | — | 0.25–0.5 | 0–5 | 35201 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunFora230_PP` | 500 | — | — | 0.25–0.5 | 0–5 | 35209 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_Armor` | — | 1 | — | — | — | 35222 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.25 | — | — | — | 35226 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Recon` | 1 | — | — | — | — | 35235 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | — | 0.45–0.45 | 1–1 | 35244 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 35257 |
| base | `GeneralNPC_Militaries_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 35267 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunThreeLine_SP` | 1000 | — | — | 0.25–0.5 | 0–2 | 35519 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–2 | 35527 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced, ERank::Veteran | `GunSVDM_SP` | 1000 | — | — | 0.25–0.5 | 0–2 | 35541 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunSVU_SP` | 500 | — | — | 0.25–0.5 | 3–5 | 35556 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunSVDM_SP` | 1000 | — | — | 0.25–0.5 | 0–2 | 35564 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunSVU_SP` | 500 | — | — | 0.25–0.5 | 0–5 | 35579 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunSVDM_SP` | 1000 | — | — | 0.25–0.5 | 0–2 | 35587 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_Armor` | — | 1 | — | — | — | 35600 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.5 | — | — | — | 35604 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Sniper` | 1 | — | — | — | — | 35613 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | Artifact | — | — | `GrenadeF1` | — | 0.2 | 1–1 | — | — | 35622 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 35633 |
| base | `GeneralNPC_Militaries_Sniper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 35643 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 35283 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 35297 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunGvintar_ST` | 200 | — | — | 0.25–0.5 | 4–6 | 35305 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 35320 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 4–6 | 35328 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 35336 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunDnipro_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 35351 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunLavina_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 35359 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunGvintar_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 35367 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunGrim_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 35375 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 35390 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 35398 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 35406 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunDnipro_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 35421 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunLavina_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 35429 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunGvintar_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 35437 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunGrim_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 35445 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_Armor` | — | 1 | — | — | — | 35458 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.4 | — | — | — | 35462 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | 1 | — | — | — | — | 35471 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | — | 0.45–0.45 | 1–1 | 35480 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 35493 |
| base | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 35503 |
| base | `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | BodyArmor | — | ERank::Newbie | `Jemmy_Neutral_Armor` | 1.0 | — | — | — | — | 247599 |
| base | `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | BodyArmor | — | ERank::Newbie | `Newbee_Neutral_Armor` | 1.0 | — | — | — | — | 247603 |
| base | `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | Head | — | — | `Light_Neutral_Helmet` | 1.0 | — | — | — | — | 247612 |
| base | `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | Consumable | — | — | `Medkit` | — | 1 | 3–3 | — | — | 247621 |
| base | `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | Consumable | — | — | `Bandage` | — | 1 | 2–2 | — | — | 247627 |
| base | `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.8–0.9 | 60–60 | 247639 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 38818 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunViper_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 38826 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 38840 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunViper_PP` | 800 | — | — | 0.25–0.5 | 0–5 | 38848 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunBucket_PP` | 100 | — | — | 0.25–0.5 | 0–5 | 38856 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunFora230_PP` | 100 | — | — | 0.25–0.5 | 0–7 | 38864 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunIntegral_PP` | 100 | — | — | 0.25–0.5 | 0–5 | 38878 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunAKU_PP` | 500 | — | — | 0.25–0.5 | 0–5 | 38886 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunBucket_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 38894 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunFora230_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 38902 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunIntegral_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 38917 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunBucket_PP` | 500 | — | — | 0.25–0.5 | 0–7 | 38925 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunZubr_PP` | 500 | — | — | 0.25–0.5 | 0–7 | 38933 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunFora230_PP` | 500 | — | — | 0.25–0.5 | 0–7 | 38941 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunIntegral_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 38956 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunBucket_PP` | 500 | — | — | 0.25–0.5 | 0–5 | 38964 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunZubr_PP` | 500 | — | — | 0.25–0.5 | 0–5 | 38972 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunFora230_PP` | 500 | — | — | 0.25–0.5 | 0–5 | 38980 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Varta_Armor` | — | 1 | — | — | — | 38993 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Varta_WeaponPistol` | — | 0.25 | — | — | — | 38997 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Recon` | 1 | — | — | — | — | 39006 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 39015 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 39026 |
| base | `GeneralNPC_Varta_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Varta_NVG` | — | 1 | — | — | — | 39036 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 56531 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Experienced, ERank::Veteran | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 0–5 | 56545 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunD12_SG` | 500 | — | — | 0.25–0.5 | 2–4 | 56559 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 0–5 | 56567 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_CloseCombat` | — | 1 | — | — | — | 56580 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.35 | — | — | — | 56584 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | ERank::Newbie, ERank::Experienced, ERank::Veteran, ERank::Master | `→ GeneralNPC_Militaries_Armor` | 1 | — | — | — | — | 56594 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 56603 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 56614 |
| base | `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 56624 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 57260 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunThreeLine_SP` | 1000 | — | — | 0.25–0.5 | 0–5 | 57268 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 57283 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunGvintar_ST` | 200 | — | — | 0.25–0.5 | 4–6 | 57291 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunPKP_MG` | 100 | — | — | 0.25–0.5 | 15–25 | 57306 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 57314 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 4–6 | 57322 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 57330 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunPKP_MG` | 1000 | — | — | 0.25–0.5 | 15–25 | 57345 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 57360 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunGvintar_ST` | 200 | — | — | 0.25–0.5 | 0–6 | 57368 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunPKP_MG` | 100 | — | — | 0.25–0.5 | 15–25 | 57383 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 57391 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 57399 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 57407 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunPKP_MG` | 1000 | — | — | 0.25–0.5 | 15–25 | 57422 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_Armor` | — | 1 | — | — | — | 57435 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.1 | — | — | — | 57439 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Heavy` | 1 | — | — | — | — | 57448 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 57457 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 57467 |
| base | `GeneralZombie_Militaries_Heavy_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 57478 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 56864 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 56878 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunGvintar_ST` | 200 | — | — | 0.25–0.5 | 4–6 | 56886 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 56901 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 4–6 | 56909 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 56917 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunDnipro_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 56932 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunLavina_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 56940 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunGvintar_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 56948 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunGrim_ST` | 400 | — | — | 0.25–0.5 | 4–6 | 56956 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 56971 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 56979 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 56987 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunDnipro_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 57002 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunLavina_ST` | 1000 | — | — | 0.25–0.5 | 0–6 | 57010 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunGvintar_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 57018 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunGrim_ST` | 400 | — | — | 0.25–0.5 | 0–6 | 57026 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_Armor` | — | 1 | — | — | — | 57039 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_WeaponPistol` | — | 0.4 | — | — | — | 57043 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | 1 | — | — | — | — | 57052 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | — | 0.45–0.45 | 1–1 | 57061 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 57074 |
| base | `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Militaries_NVG` | — | 1 | — | — | — | 57084 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Newbie | `GunObrez_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 58513 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Newbie | `GunTOZ_SG` | 100 | — | — | 0.25–0.5 | 0–2 | 58521 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunM860_SG` | 100 | — | — | 0.25–0.5 | 2–4 | 58536 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunTOZ_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 58544 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Newbie | `GunObrez_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 58559 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Newbie | `GunTOZ_SG` | 100 | — | — | 0.25–0.5 | 0–2 | 58567 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunM860_SG` | 100 | — | — | 0.25–0.5 | 1–4 | 58582 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunTOZ_SG` | 1000 | — | — | 0.25–0.5 | 1–4 | 58590 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Neutral_WeaponPistol` | — | 0.3 | — | — | — | 58603 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 58614 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunSPSA_SG` | 100 | — | — | 0.25–0.5 | 2–4 | 58622 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunSPSA_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 58637 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 1–4 | 58652 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunSPSA_SG` | 100 | — | — | 0.25–0.5 | 1–4 | 58660 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunSPSA_SG` | 1000 | — | — | 0.25–0.5 | 1–4 | 58675 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Jemmy_Neutral_Armor` | 15.0 | — | — | — | — | 58689 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Newbee_Neutral_Armor` | 1.0 | — | — | — | — | 58693 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Newbie | `NPC_Sel_Neutral_Armor` | 4.0 | — | — | — | — | 58697 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Nasos_Neutral_Armor` | 1.0 | — | — | — | — | 58707 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Zorya_Neutral_Armor` | 1.0 | — | — | — | — | 58711 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Veteran | `SEVA_Neutral_Armor` | 2.0 | — | — | — | — | 58721 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Veteran | `NPC_Cloak_Heavy_Neutral_Armor` | 3.0 | — | — | — | — | 58725 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Master | `Exoskeleton_Neutral_Armor` | 2.0 | — | — | — | — | 58735 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Master | `SEVA_Neutral_Armor` | 1.0 | — | — | — | — | 58739 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Master | `NPC_Cloak_Heavy_Neutral_Armor` | 2.0 | — | — | — | — | 58743 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | Head | — | ERank::Newbie, ERank::Experienced | `Light_Neutral_Helmet` | — | 0.3 | — | — | — | 58753 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | Head | — | ERank::Veteran, ERank::Master | `Light_Neutral_Helmet` | — | 0.7 | — | — | — | 58763 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_CloseCombat` | 1 | — | — | — | — | 58772 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 58781 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 58791 |
| base | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Neutral_NVG` | — | 1 | — | — | — | 58802 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunViper_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 58828 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAKU_PP` | 100 | — | — | 0.25–0.5 | 0–5 | 58836 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Neutral_WeaponPistol` | — | 0.25 | — | — | — | 58849 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunViper_PP` | 800 | — | — | 0.25–0.5 | 0–7 | 58860 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 58868 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunBucket_PP` | 100 | — | — | 0.25–0.5 | 0–7 | 58876 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Experienced | `GunFora230_PP` | 100 | — | — | 0.25–0.5 | 0–7 | 58884 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunViper_PP` | 400 | — | — | 0.25–0.5 | 0–7 | 58899 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunAKU_PP` | 500 | — | — | 0.25–0.5 | 0–7 | 58907 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunBucket_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 58915 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunIntegral_PP` | 100 | — | — | 0.25–0.5 | 0–7 | 58923 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran | `GunFora230_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 58931 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunAKU_PP` | 500 | — | — | 0.25–0.5 | 0–7 | 58946 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunBucket_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 58954 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunIntegral_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 58962 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Master | `GunFora230_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 58970 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunViper_PP` | 800 | — | — | 0.25–0.5 | 0–6 | 58985 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunAKU_PP` | 1000 | — | — | 0.25–0.5 | 0–6 | 58993 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunBucket_PP` | 100 | — | — | 0.25–0.5 | 0–6 | 59001 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Experienced | `GunFora230_PP` | 100 | — | — | 0.25–0.5 | 0–6 | 59009 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunViper_PP` | 400 | — | — | 0.25–0.5 | 0–6 | 59024 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunAKU_PP` | 500 | — | — | 0.25–0.5 | 0–6 | 59032 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunBucket_PP` | 1000 | — | — | 0.25–0.5 | 0–6 | 59040 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunIntegral_PP` | 100 | — | — | 0.25–0.5 | 0–6 | 59048 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Veteran | `GunFora230_PP` | 1000 | — | — | 0.25–0.5 | 0–6 | 59056 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunAKU_PP` | 500 | — | — | 0.25–0.5 | 0–6 | 59071 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunBucket_PP` | 1000 | — | — | 0.25–0.5 | 0–6 | 59079 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunIntegral_PP` | 1000 | — | — | 0.25–0.5 | 0–6 | 59087 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | WeaponPrimary | EGameDifficulty::Stalker | ERank::Master | `GunFora230_PP` | 1000 | — | — | 0.25–0.5 | 0–6 | 59095 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Jemmy_Neutral_Armor` | 15.0 | — | — | — | — | 59109 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Newbee_Neutral_Armor` | 1.0 | — | — | — | — | 59113 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Newbie | `NPC_Sel_Neutral_Armor` | 4.0 | — | — | — | — | 59117 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Nasos_Neutral_Armor` | 1.0 | — | — | — | — | 59127 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Zorya_Neutral_Armor` | 1.0 | — | — | — | — | 59131 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Veteran | `SEVA_Neutral_Armor` | 2.0 | — | — | — | — | 59141 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Veteran | `NPC_Cloak_Heavy_Neutral_Armor` | 2.0 | — | — | — | — | 59145 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Master | `Exoskeleton_Neutral_Armor` | 2.0 | — | — | — | — | 59155 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Master | `SEVA_Neutral_Armor` | 1.0 | — | — | — | — | 59159 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Master | `NPC_Cloak_Heavy_Neutral_Armor` | 2.0 | — | — | — | — | 59163 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | Head | — | ERank::Newbie, ERank::Experienced | `Light_Neutral_Helmet` | — | 0.3 | — | — | — | 59173 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | Head | — | ERank::Veteran, ERank::Master | `Light_Neutral_Helmet` | — | 0.7 | — | — | — | 59183 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Recon` | 1 | — | — | — | — | 59192 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 59201 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 59212 |
| base | `GeneralZombie_Neutral_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Neutral_NVG` | — | 1 | — | — | — | 59222 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Neutral_WeaponPistol` | — | 0.4 | — | — | — | 59651 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | — | 1 | — | — | — | 59655 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 59665 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunM16_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 59673 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunAK74_ST` | 500 | — | — | 0.25–0.5 | 0–7 | 59687 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunM16_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 59695 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunFora_ST` | 100 | — | — | 0.25–0.5 | 0–7 | 59703 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunGvintar_ST` | 100 | — | — | 0.25–0.5 | 0–7 | 59711 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunM16_ST` | 500 | — | — | 0.25–0.5 | 0–7 | 59725 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunG37_ST` | 100 | — | — | 0.25–0.5 | 0–7 | 59733 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunFora_ST` | 500 | — | — | 0.25–0.5 | 0–7 | 59741 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunGrim_ST` | 100 | — | — | 0.25–0.5 | 0–7 | 59749 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 59757 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunArev_ST` | 100 | — | — | 0.25–0.5 | 0–7 | 59765 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunG37_ST` | 500 | — | — | 0.25–0.5 | 0–7 | 59779 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunFora_ST` | 500 | — | — | 0.25–0.5 | 0–7 | 59787 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 59795 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunGvintar_ST` | 500 | — | — | 0.25–0.5 | 0–7 | 59803 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunLavina_ST` | 100 | — | — | 0.25–0.5 | 0–7 | 59811 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunArev_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 59819 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Jemmy_Neutral_Armor` | 15.0 | — | — | — | — | 59833 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Newbee_Neutral_Armor` | 1.0 | — | — | — | — | 59837 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Newbie | `NPC_Sel_Neutral_Armor` | 4.0 | — | — | — | — | 59841 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Nasos_Neutral_Armor` | 1.0 | — | — | — | — | 59851 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Zorya_Neutral_Armor` | 1.0 | — | — | — | — | 59855 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Veteran | `SEVA_Neutral_Armor` | 2.0 | — | — | — | — | 59865 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Veteran | `NPC_Cloak_Heavy_Neutral_Armor` | 3.0 | — | — | — | — | 59869 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Master | `Exoskeleton_Neutral_Armor` | 2.0 | — | — | — | — | 59879 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Master | `SEVA_Neutral_Armor` | 1.0 | — | — | — | — | 59883 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | BodyArmor | — | ERank::Master | `NPC_Cloak_Heavy_Neutral_Armor` | 2.0 | — | — | — | — | 59887 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | Head | — | ERank::Newbie, ERank::Experienced | `Light_Neutral_Helmet` | — | 0.3 | — | — | — | 59897 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | Head | — | ERank::Veteran, ERank::Master | `Light_Neutral_Helmet` | — | 0.7 | — | — | — | 59907 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 59916 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 59927 |
| base | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Neutral_NVG` | — | 1 | — | — | — | 59937 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GuardGunViper_PP` | 1000 | — | — | 0.25–0.5 | 0–5 | 41589 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GuardGunViper_PP` | 1000 | — | — | 0.25–0.5 | 30–45 | 41603 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GuardGunAKU_PP` | 1000 | — | — | 0.25–0.5 | 30–45 | 41611 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran, ERank::Master | `GuardGunViper_PP` | 1000 | — | — | 0.25–0.5 | 30–45 | 41625 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran, ERank::Master | `GuardGunAKU_PP` | 1000 | — | — | 0.25–0.5 | 30–45 | 41633 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | WeaponPrimary | — | ERank::Veteran, ERank::Master | `GuardGunBucket_PP` | 1000 | — | — | 0.25–0.5 | 30–45 | 41641 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Jemmy_Neutral_Armor` | 15.0 | — | — | — | — | 41655 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Newbee_Neutral_Armor` | 1.0 | — | — | — | — | 41659 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Newbie | `NPC_Sel_Neutral_Armor` | 4.0 | — | — | — | — | 41663 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Nasos_Neutral_Armor` | 1.0 | — | — | — | — | 41673 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Zorya_Neutral_Armor` | 1.0 | — | — | — | — | 41677 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Veteran | `SEVA_Neutral_Armor` | 4.0 | — | — | — | — | 41687 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Veteran | `NPC_Cloak_Heavy_Neutral_Armor` | 1.0 | — | — | — | — | 41691 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Master | `Exoskeleton_Neutral_Armor` | 2.0 | — | — | — | — | 41701 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Master | `SEVA_Neutral_Armor` | 1.0 | — | — | — | — | 41705 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | BodyArmor | — | ERank::Master | `NPC_Cloak_Heavy_Neutral_Armor` | 2.0 | — | — | — | — | 41709 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | Head | — | ERank::Newbie, ERank::Experienced | `Light_Neutral_Helmet` | — | 0.3 | — | — | — | 41719 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Recon` | 1 | — | — | — | — | 41728 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | Artifact | — | ERank::Experienced | `GrenadeRGD5` | — | 0.3 | 1–1 | — | — | 41738 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | Artifact | — | ERank::Veteran, ERank::Master | `GrenadeRGD5` | — | 0.3 | 1–1 | — | — | 41750 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | Artifact | — | ERank::Veteran, ERank::Master | `GrenadeF1` | — | 0.3 | 1–1 | — | — | 41756 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | Detector | — | — | `Echo` | — | 0.2 | 1–— | — | — | 41767 |
| base | `GuardNPC_Neutral_Recon_ItemGenerator` | Junk | — | — | `GuardQuestItem` | — | 1.0 | 1–1 | — | — | 41777 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 3065 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ RUCloseCombatWeaponVeteranGenerator` | 20.0 | — | — | — | — | 3074 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ RUScoutWeaponVeteranGenerator` | 20.0 | — | — | — | — | 3078 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ RUStormTrooperWeaponVeteranGenerator` | 40.0 | — | — | — | — | 3082 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ RUMachineGunnerWeaponVeteranGenerator` | 10.0 | — | — | — | — | 3086 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ RUSniperWeaponVeteranGenerator` | 10.0 | — | — | — | — | 3090 |
| base | `MonolithVeteranItemGenerator` | BodyArmor | — | — | `HeavyExoskeleton_Monolith_Armor` | 100.0 | — | — | 1.0–1.0 | — | 3099 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ ConsumableVeteranGenerator` | — | 1.0 | — | — | — | 3118 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ DetectorVeteranGenerator` | — | 0.25 | — | — | — | 3122 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ ArtifactVeteranGenerator` | — | 0.07 | — | — | — | 3126 |
| base | `MonolithVeteranItemGenerator` | SubItemGenerator | — | — | `→ AttachVeteranGenerator` | — | 0.07 | — | — | — | 3130 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 4717 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLCloseCombatWeaponExperiencedGenerator` | 20.0 | — | — | — | — | 4726 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLScoutWeaponExperiencedGenerator` | 20.0 | — | — | — | — | 4730 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLStormTrooperWeaponExperiencedGenerator` | 40.0 | — | — | — | — | 4734 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLMachineGunnerWeaponExperiencedGenerator` | 10.0 | — | — | — | — | 4738 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLSniperWeaponExperiencedGenerator` | 10.0 | — | — | — | — | 4742 |
| base | `NeutralExperiencedItemGenerator` | BodyArmor | — | — | `Nasos_Neutral_Armor` | 100.0 | — | — | 1.0–1.0 | — | 4751 |
| base | `NeutralExperiencedItemGenerator` | BodyArmor | — | — | `Zorya_Neutral_Armor` | 100.0 | — | — | 1.0–1.0 | — | 4757 |
| base | `NeutralExperiencedItemGenerator` | BodyArmor | — | — | `SEVA_Neutral_Armor` | 100.0 | — | — | 1.0–1.0 | — | 4763 |
| base | `NeutralExperiencedItemGenerator` | Head | — | — | `Light_Neutral_Helmet` | 50.0 | — | — | 1.0–1.0 | — | 4774 |
| base | `NeutralExperiencedItemGenerator` | Head | — | — | `empty` | 150.0 | — | — | 1.0–1.0 | — | 4780 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ConsumableExperiencedGenerator` | — | 1.0 | — | — | — | 4795 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ DetectorExperiencedGenerator` | — | 0.1 | — | — | — | 4799 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ArtifactExperiencedGenerator` | — | 0.05 | — | — | — | 4803 |
| base | `NeutralExperiencedItemGenerator` | SubItemGenerator | — | — | `→ AttachExperiencedGenerator` | — | 0.05 | — | — | — | 4807 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 4914 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ ALLCloseCombatWeaponMasterGenerator` | 20.0 | — | — | — | — | 4923 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ ALLScoutWeaponMasterGenerator` | 20.0 | — | — | — | — | 4927 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ ALLStormTrooperWeaponMasterGenerator` | 40.0 | — | — | — | — | 4931 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ ALLMachineGunnerWeaponMasterGenerator` | 10.0 | — | — | — | — | 4935 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ ALLSniperWeaponMasterGenerator` | 10.0 | — | — | — | — | 4939 |
| base | `NeutralMasterItemGenerator` | BodyArmor | — | — | `Exoskeleton_Neutral_Armor` | 100.0 | — | — | 1.0–1.0 | — | 4948 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ ConsumableMasterGenerator` | — | 1.0 | — | — | — | 4967 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ DetectorMasterGenerator` | — | 0.5 | — | — | — | 4971 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ ArtifactMasterGenerator` | — | 0.1 | — | — | — | 4975 |
| base | `NeutralMasterItemGenerator` | SubItemGenerator | — | — | `→ AttachMasterGenerator` | — | 0.1 | — | — | — | 4979 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 4619 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ ALLCloseCombatWeaponNewbieGenerator` | 20.0 | — | — | — | — | 4628 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ ALLScoutWeaponNewbieGenerator` | 20.0 | — | — | — | — | 4632 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ ALLStormTrooperWeaponNewbieGenerator` | 40.0 | — | — | — | — | 4636 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ ALLMachineGunnerWeaponNewbieGenerator` | 10.0 | — | — | — | — | 4640 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ ALLSniperWeaponNewbieGenerator` | 10.0 | — | — | — | — | 4644 |
| base | `NeutralNewbieItemGenerator` | BodyArmor | — | — | `Jemmy_Neutral_Armor` | 100.0 | — | — | 1.0–1.0 | — | 4653 |
| base | `NeutralNewbieItemGenerator` | BodyArmor | — | — | `Newbee_Neutral_Armor` | 100.0 | — | — | 1.0–1.0 | — | 4659 |
| base | `NeutralNewbieItemGenerator` | Head | — | — | `Light_Neutral_Helmet` | 50.0 | — | — | 1.0–1.0 | — | 4670 |
| base | `NeutralNewbieItemGenerator` | Head | — | — | `empty` | 150.0 | — | — | 1.0–1.0 | — | 4676 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ ConsumableNewbieGenerator` | — | 1.0 | — | — | — | 4691 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ DetectorNewbieGenerator` | — | 0.05 | — | — | — | 4695 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ ArtifactNewbieGenerator` | — | 0.01 | — | — | — | 4699 |
| base | `NeutralNewbieItemGenerator` | SubItemGenerator | — | — | `→ AttachNewbieGenerator` | — | 0.01 | — | — | — | 4703 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 4382 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLCloseCombatWeaponExperiencedGenerator` | 20.0 | — | — | — | — | 4391 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLScoutWeaponExperiencedGenerator` | 20.0 | — | — | — | — | 4395 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLStormTrooperWeaponExperiencedGenerator` | 40.0 | — | — | — | — | 4399 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLMachineGunnerWeaponExperiencedGenerator` | 10.0 | — | — | — | — | 4403 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ALLSniperWeaponExperiencedGenerator` | 10.0 | — | — | — | — | 4407 |
| base | `ScientistExperiencedItemGenerator` | BodyArmor | — | — | `HeavyAnomaly_Scientific_Armor` | 100.0 | — | — | 1.0–1.0 | — | 4416 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ConsumableExperiencedGenerator` | — | 1.0 | — | — | — | 4435 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ DetectorExperiencedGenerator` | — | 0.1 | — | — | — | 4439 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ ArtifactExperiencedGenerator` | — | 0.05 | — | — | — | 4443 |
| base | `ScientistExperiencedItemGenerator` | SubItemGenerator | — | — | `→ AttachExperiencedGenerator` | — | 0.05 | — | — | — | 4447 |
| base | `ScientistsZitnik_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced, ERank::Veteran, ERank::Master | `GunViper_PP` | 1000 | — | — | 0.25–0.5 | 0–7 | 196246 |
| base | `ScientistsZitnik_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Scientists_Armor` | — | 1 | — | — | — | 196259 |
| base | `ScientistsZitnik_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Scientists_WeaponPistol` | — | 0.25 | — | — | — | 196263 |
| base | `ScientistsZitnik_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Recon` | 1 | — | — | — | — | 196272 |
| base | `ScientistsZitnik_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 196281 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 6416 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENCloseCombatWeaponVeteranGenerator` | 20.0 | — | — | — | — | 6425 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENScoutWeaponVeteranGenerator` | 20.0 | — | — | — | — | 6429 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENStormTrooperWeaponVeteranGenerator` | 40.0 | — | — | — | — | 6433 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENMachineGunnerWeaponVeteranGenerator` | 10.0 | — | — | — | — | 6437 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ ENSniperWeaponVeteranGenerator` | 10.0 | — | — | — | — | 6441 |
| base | `SparkVeteranItemGenerator` | BodyArmor | — | — | `HeavyAnomaly_Spark_Armor` | 100.0 | — | — | 1.0–1.0 | — | 6450 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ ConsumableVeteranGenerator` | — | 1.0 | — | — | — | 6469 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ DetectorVeteranGenerator` | — | 0.25 | — | — | — | 6473 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ ArtifactVeteranGenerator` | — | 0.07 | — | — | — | 6477 |
| base | `SparkVeteranItemGenerator` | SubItemGenerator | — | — | `→ AttachVeteranGenerator` | — | 0.07 | — | — | — | 6481 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 5546 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ RUCloseCombatWeaponMasterGenerator` | 20.0 | — | — | — | — | 5555 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ RUScoutWeaponMasterGenerator` | 20.0 | — | — | — | — | 5559 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ RUStormTrooperWeaponMasterGenerator` | 40.0 | — | — | — | — | 5563 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ RUMachineGunnerWeaponMasterGenerator` | 10.0 | — | — | — | — | 5567 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ RUSniperWeaponMasterGenerator` | 10.0 | — | — | — | — | 5571 |
| base | `VartaMasterItemGenerator` | BodyArmor | — | — | `BattleExoskeleton_Varta_Armor` | 100.0 | — | — | 1.0–1.0 | — | 5580 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ ConsumableMasterGenerator` | — | 1.0 | — | — | — | 5599 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ DetectorMasterGenerator` | — | 0.5 | — | — | — | 5603 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ ArtifactMasterGenerator` | — | 0.1 | — | — | — | 5607 |
| base | `VartaMasterItemGenerator` | SubItemGenerator | — | — | `→ AttachMasterGenerator` | — | 0.1 | — | — | — | 5611 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunAK74_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 63872 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunM16_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 63880 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunFora_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 63888 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunGvintar_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 63896 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunAK74_ST` | 400 | — | — | 0.25–0.5 | 0–5 | 63910 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunM16_ST` | 500 | — | — | 0.25–0.5 | 0–5 | 63918 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunGrim_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 63926 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunGvintar_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 63934 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunG37_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 63942 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunDnipro_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 63956 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunGrim_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 63964 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunLavina_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 63972 |
| base | `YantarDeadBody_Spark_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunG37_ST` | 500 | — | — | 0.25–0.5 | 0–5 | 63980 |
| base | `YantarDeadBody_Spark_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Spark_WeaponPistol` | — | 0.4 | — | — | — | 63993 |
| base | `YantarDeadBody_Spark_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Spark_Armor` | — | 1 | — | — | — | 63997 |
| base | `YantarDeadBody_Spark_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | 1 | — | — | — | — | 64006 |
| base | `YantarDeadBody_Spark_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 64015 |
| base | `YantarDeadBody_Spark_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 64026 |
| base | `YantarDeadBody_Spark_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Spark_NVG` | — | 1 | — | — | — | 64036 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunM16_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 251865 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Newbie, ERank::Experienced | `GunG37_ST` | 100 | — | — | 0.25–0.5 | 0–5 | 251873 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunFora_ST` | 100 | — | — | 0.25–0.5 | 0–7 | 251887 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunM16_ST` | 400 | — | — | 0.25–0.5 | 0–5 | 251895 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 251903 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunArev_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 251911 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunFora_ST` | 500 | — | — | 0.25–0.5 | 0–7 | 251925 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunM16_ST` | 500 | — | — | 0.25–0.5 | 0–5 | 251933 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunG37_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 251941 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunKharod_ST` | 1000 | — | — | 0.25–0.5 | 0–5 | 251949 |
| base | `cerep_0_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunArev_ST` | 1000 | — | — | 0.25–0.5 | 0–7 | 251957 |
| base | `cerep_0_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Mercenaries_Armor` | — | 1 | — | — | — | 251970 |
| base | `cerep_0_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Mercenaries_WeaponPistol` | — | 0.4 | — | — | — | 251974 |
| base | `cerep_0_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | 1 | — | — | — | — | 251983 |
| base | `cerep_0_ItemGenerator` | Artifact | — | — | `GrenadeRGD5` | — | 0.2 | 1–1 | — | — | 251992 |
| base | `cerep_0_ItemGenerator` | Detector | — | — | `Echo` | — | 0.05 | 1–— | — | — | 252003 |
| base | `cerep_0_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Mercenaries_NVG` | — | 1 | — | — | — | 252013 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Newbie, ERank::Experienced | `Bandage` | — | 0.2 | 1–1 | — | — | 4428 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Newbie, ERank::Experienced | `Medkit` | — | 0.2 | 1–1 | — | — | 4434 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran, ERank::Master | `Bandage` | — | 1 | 1–2 | — | — | 4448 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Easy, EGameDifficulty::Medium, EGameDifficulty::Hard | ERank::Veteran, ERank::Master | `Medkit` | — | 0.4 | 1–1 | — | — | 4454 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Stalker | ERank::Newbie, ERank::Experienced | `Bandage` | — | 0.15 | 1–1 | — | — | 4468 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Stalker | ERank::Newbie, ERank::Experienced | `Medkit` | — | 0.15 | 1–1 | — | — | 4474 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Stalker | ERank::Veteran, ERank::Master | `Bandage` | — | 0.7 | 1–2 | — | — | 4488 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Consumable | EGameDifficulty::Stalker | ERank::Veteran, ERank::Master | `Medkit` | — | 0.3 | 1–1 | — | — | 4494 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunObrez_SG` | 1000 | — | — | 0.25–0.5 | 0–2 | 4506 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Newbie | `GunTOZ_SG` | 100 | — | — | 0.25–0.5 | 0–2 | 4514 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunM860_SG` | 100 | — | — | 0.25–0.5 | 2–4 | 4528 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Experienced | `GunTOZ_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 4536 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Neutral_WeaponPistol` | — | 0.3 | — | — | — | 4549 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunM860_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 4559 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Veteran | `GunSPSA_SG` | 100 | — | — | 0.25–0.5 | 2–4 | 4567 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | WeaponPrimary | — | ERank::Master | `GunSPSA_SG` | 1000 | — | — | 0.25–0.5 | 2–4 | 4581 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Jemmy_Neutral_Armor` | 15.0 | — | — | — | — | 4595 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Newbie | `Newbee_Neutral_Armor` | 1.0 | — | — | — | — | 4599 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Newbie | `NPC_Sel_Neutral_Armor` | 4.0 | — | — | — | — | 4603 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Nasos_Neutral_Armor` | 1.0 | — | — | — | — | 4613 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Experienced | `Zorya_Neutral_Armor` | 1.0 | — | — | — | — | 4617 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Veteran | `SEVA_Neutral_Armor` | 2.0 | — | — | — | — | 4627 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Veteran | `NPC_Cloak_Heavy_Neutral_Armor` | 3.0 | — | — | — | — | 4631 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Master | `Exoskeleton_Neutral_Armor` | 2.0 | — | — | — | — | 4641 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Master | `SEVA_Neutral_Armor` | 1.0 | — | — | — | — | 4645 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | BodyArmor | — | ERank::Master | `NPC_Cloak_Heavy_Neutral_Armor` | 2.0 | — | — | — | — | 4649 |
| dlc1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | None | — | — | `empty` | 4.f | — | 0–0 | 0.0–0.0 | 0–0 | 4658 |
| dlc1 | `MercTesak_ItemGenerator` | Artifact | — | — | `GrenadeF1` | 100.0 | 1 | 1–1 | 1.0–1.0 | — | 26421 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ GeneralNPC_Consumables_Stormtrooper` | — | 1 | — | — | — | 26435 |
| dlc1 | `MercTesak_ItemGenerator` | WeaponPrimary | — | — | `GunKharod_ST` | 1000 | — | — | 0.55–0.65 | 7–9 | 26444 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 26457 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ ENCloseCombatWeaponMasterGenerator` | 20.0 | — | — | — | — | 26466 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ ENScoutWeaponMasterGenerator` | 20.0 | — | — | — | — | 26470 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ ENStormTrooperWeaponMasterGenerator` | 40.0 | — | — | — | — | 26474 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ ENMachineGunnerWeaponMasterGenerator` | 10.0 | — | — | — | — | 26478 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ ENSniperWeaponMasterGenerator` | 10.0 | — | — | — | — | 26482 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ ConsumableMasterGenerator` | — | 1.0 | — | — | — | 26491 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ DetectorMasterGenerator` | — | 0.5 | — | — | — | 26495 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ ArtifactMasterGenerator` | — | 0.1 | — | — | — | 26499 |
| dlc1 | `MercTesak_ItemGenerator` | SubItemGenerator | — | — | `→ AttachMasterGenerator` | — | 0.1 | — | — | — | 26503 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `SciSEVA_Scientific_Armor` | 100.0 | 1 | — | 1.0–1.0 | — | 50041 |
| dlc1 | `ScientistTaumin_ItemGenerator` | WeaponPrimary | — | — | `GunM16_ST` | 1000 | — | — | 0.4–0.5 | 7–9 | 50054 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ AllAmmosNPCGenerator` | — | 1.0 | — | — | — | 50067 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ ALLCloseCombatWeaponMasterGenerator` | 20.0 | — | — | — | — | 50076 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ ALLScoutWeaponMasterGenerator` | 20.0 | — | — | — | — | 50080 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ ALLStormTrooperWeaponMasterGenerator` | 40.0 | — | — | — | — | 50084 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ ALLMachineGunnerWeaponMasterGenerator` | 10.0 | — | — | — | — | 50088 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ ALLSniperWeaponMasterGenerator` | 10.0 | — | — | — | — | 50092 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ ConsumableMasterGenerator` | — | 1.0 | — | — | — | 50105 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ DetectorMasterGenerator` | — | 0.5 | — | — | — | 50109 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ ArtifactMasterGenerator` | — | 0.1 | — | — | — | 50113 |
| dlc1 | `ScientistTaumin_ItemGenerator` | SubItemGenerator | — | — | `→ AttachMasterGenerator` | — | 0.1 | — | — | — | 50117 |
