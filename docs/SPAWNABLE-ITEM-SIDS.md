# Spawn-referenced Item SIDs — GameData + DLC1

Source: GameData and DLC1 CFG dumps (2026-10-17). This inventory means an ItemPrototype definition has at least one configured spawn-related reference; it does **not** prove an item will spawn in a particular playthrough. Sources may be gated by player rank, quest, difficulty, conditions, rarity, or disabled entries. Unreferenced templates and mere upgrade/fitting SIDs are excluded.

Excluded as cut content: **7.62×39** `A762A`, `A762D`, `A762E`. The source includes these definitions even if some have no direct spawn reference. 7.62×51 NATO and 7.62×54 sniper ammunition remain included.

Evidence: `GEN` = item generator, `PACK` = pack-of-items, `STASH` = SmartLoot stash, `WORLD` = world/actor placement (including attachments), `ART` = artifact spawner, `SCRIPT` = script item. `base` and `dlc1` identify the defining ItemPrototypes file, not necessarily the spawning context. Full source CFG paths and native types are retained in `src/TrueCustomDifficulty/Data/spawnable-item-sids.csv`. Extractor: `tools/generate_spawn_item_sids.py`.

Counts: consumables **23**, weapons **115**, ammo **31**, attachments **99**, armor **88**, binoculars **3**, nvg **7**, detectors **5**, artifacts **90** (total **461**).

## Consumables (23)

| SID | Definition | Spawn reference |
|---|---|---|
| `AntiRad` | base | GEN,PACK,WORLD |
| `ArmyMedkit` | base | GEN,PACK,WORLD,STASH |
| `Bandage` | base | GEN,PACK,WORLD,STASH |
| `Beer` | base | GEN,PACK,WORLD,STASH |
| `Bread` | base | GEN,PACK,WORLD,STASH |
| `CannedFood` | base | GEN,PACK,WORLD,STASH |
| `Cinnamon` | base | GEN,PACK,WORLD |
| `DvupalovVodka` | base | GEN,WORLD |
| `EcoMedkit` | base | GEN,PACK,WORLD |
| `Energetic` | base | GEN,PACK,WORLD,STASH |
| `Energetic_Limited` | base | WORLD |
| `EQ08_FreshBread` | base | WORLD |
| `GrenadeF1` | base | GEN,PACK,WORLD,STASH |
| `GrenadeRGD5` | base | GEN,PACK,WORLD,STASH |
| `Hercules` | base | GEN,PACK,WORLD |
| `Medkit` | base | GEN,PACK,WORLD,STASH |
| `Milk` | base | GEN,PACK,WORLD,STASH |
| `MonolithDryRation` | dlc1 | GEN,WORLD |
| `PSYBlocker` | base | GEN,PACK,WORLD |
| `Sausage` | base | GEN,PACK,WORLD,STASH |
| `SpoiledCannedFood` | base | WORLD |
| `Vodka` | base | GEN,PACK,WORLD,STASH |
| `Water` | base | GEN,PACK,WORLD,STASH |

## Weapons (115)

| SID | Definition | Spawn reference |
|---|---|---|
| `GuardGunAK74_ST` | base | GEN |
| `GuardGunAKU_PP` | base | GEN |
| `GuardGunAPB_HG` | base | GEN |
| `GuardGunBucket_PP` | base | GEN |
| `GuardGunD12_SG` | base | GEN |
| `GuardGunDnipro_ST` | base | GEN |
| `GuardGunFora_ST` | base | GEN |
| `GuardGunG37_ST` | base | GEN |
| `GuardGunGP3A_DMR` | base | GEN |
| `GuardGunGrim_ST` | base | GEN |
| `GuardGunGvintar_ST` | base | GEN |
| `GuardGunIntegral_PP` | base | GEN |
| `GuardGunKharod_ST` | base | GEN,WORLD |
| `GuardGunLavina_ST` | base | GEN |
| `GuardGunM10_HG` | base | GEN |
| `GuardGunM16_ST` | base | GEN |
| `GuardGunM701_SP` | base | GEN |
| `GuardGunM860_SG` | base | GEN |
| `GuardGunMark_SP` | base | GEN |
| `GuardGunObrez_SG` | base | GEN |
| `GuardGunPKP_MG` | base | GEN |
| `GuardGunPM_HG` | base | GEN,WORLD |
| `GuardGunRam2_SG` | base | GEN |
| `GuardGunSKP_DMR` | base | GEN |
| `GuardGunSPSA_SG` | base | GEN,WORLD |
| `GuardGunSVDM_SP` | base | GEN,WORLD |
| `GuardGunSVU_SP` | base | GEN |
| `GuardGunTOZ_SG` | base | GEN |
| `GuardGunUDP_HG` | base | GEN |
| `GuardGunViper_PP` | base | GEN |
| `GuardGunZubr_PP` | base | GEN |
| `Gun_Cavalier_SR` | base | WORLD |
| `Gun_Combatant_AR` | base | WORLD |
| `Gun_Decider_AR` | base | WORLD |
| `Gun_Drowned_AR` | base | WORLD |
| `Gun_Encourage_HG` | base | GEN |
| `Gun_GStreet_HG` | base | WORLD |
| `Gun_Lummox_AR` | base | GEN |
| `Gun_Partner_SR` | base | WORLD |
| `Gun_ProjectY_HG` | base | WORLD |
| `Gun_RatKiller_SMG` | base | WORLD |
| `Gun_S15_AR` | base | WORLD |
| `Gun_Sharpshooter_AR` | base | WORLD |
| `Gun_Silence_SMG` | base | GEN |
| `Gun_SkifGun_HG` | base | GEN |
| `Gun_Sledgehammer_SG` | base | GEN |
| `Gun_SOFMOD_AR` | base | WORLD |
| `Gun_Sotnyk_AR` | base | WORLD |
| `Gun_Spitfire_SMG` | base | WORLD |
| `Gun_Star_HG` | base | WORLD |
| `Gun_Tank_MG` | base | WORLD |
| `Gun_Texas_SG` | base | WORLD |
| `Gun_Trophy_AR` | base | GEN |
| `Gun_Unknown_AR` | base | GEN |
| `Gun_Whip_SR` | base | WORLD |
| `GunAK74_ST` | base | GEN,PACK,WORLD |
| `GunAK74_Strelok_ST` | base | GEN |
| `GunAK9_ST` | dlc1 | WORLD |
| `GunAKU_PP` | base | GEN,PACK,WORLD |
| `GunAPB_HG` | base | GEN,PACK,WORLD |
| `GunArev_ST` | base | GEN,WORLD |
| `GunArevLight_AR` | dlc1 | GEN |
| `GunArevPrecise_AR` | base | WORLD |
| `GunBucket_PP` | base | GEN,PACK,WORLD |
| `GunClapper_SG` | dlc1 | GEN |
| `GunD12_SG` | base | GEN,PACK,WORLD |
| `GunDefender_SG` | dlc1 | GEN |
| `GunDnipro_ST` | base | GEN,PACK,WORLD |
| `GunDniproDuty_ST` | dlc1 | WORLD |
| `GunFallschirmjager_DMR` | dlc1 | WORLD |
| `GunFora230_PP` | base | GEN,WORLD |
| `GunFora_ST` | base | GEN,PACK,WORLD |
| `GunG37_ST` | base | GEN,PACK,WORLD |
| `GunG37V2_ST` | base | GEN,WORLD |
| `GunGauss_Scar_SP` | base | GEN |
| `GunGauss_SP` | base | GEN,PACK,WORLD |
| `GunGonta_SP` | base | GEN |
| `GunGP3A_DMR` | base | GEN,WORLD |
| `GunGrim_ST` | base | GEN,PACK,WORLD |
| `GunGvintar_ST` | base | GEN,PACK,WORLD |
| `GunHSG72_DMR` | dlc1 | GEN |
| `GunIntegral_PP` | base | GEN,PACK,WORLD |
| `GunKharod_ST` | base | GEN,PACK,WORLD |
| `GunKharodFreedom_ST` | dlc1 | WORLD |
| `GunKora_HG` | base | GEN,WORLD |
| `GunLavina_ST` | base | GEN,PACK,WORLD |
| `GunM10_HG` | base | GEN,PACK,WORLD |
| `GunM16_ST` | base | GEN,PACK,WORLD |
| `GunM701_SP` | base | GEN,PACK,WORLD |
| `GunM860_SG` | base | GEN,PACK,WORLD |
| `GunMark_SP` | base | GEN,PACK,WORLD |
| `GunNightStalker_HG` | base | WORLD |
| `GunObrez_SG` | base | GEN,PACK,WORLD |
| `GunPKP_Korshunov_MG` | base | GEN |
| `GunPKP_MG` | base | GEN,PACK,WORLD |
| `GunPM_HG` | base | GEN,PACK,WORLD |
| `GunRam2_SG` | base | GEN,PACK,WORLD |
| `GunRhino_HG` | base | GEN,PACK,WORLD |
| `GunRpg7_GL` | base | GEN,PACK,WORLD |
| `GunSKP_DMR` | base | GEN,WORLD |
| `GunSPSA_SG` | base | GEN,PACK,WORLD |
| `GunSPSA_Sniper_SG` | dlc1 | GEN |
| `GunSVDM_SP` | base | GEN,PACK,WORLD |
| `GunSVU_CQB_SP` | dlc1 | WORLD |
| `GunSVU_Sniper_Duga_SP` | base | GEN |
| `GunSVU_SP` | base | GEN,PACK,WORLD |
| `GunThreeLine_SP` | base | GEN,WORLD |
| `GunTOZ_Gonta` | base | WORLD |
| `GunTOZ_SG` | base | GEN,PACK,WORLD |
| `GunTundra_DMR` | dlc1 | WORLD |
| `GunUDP_HG` | base | GEN,PACK,WORLD |
| `GunViper_PP` | base | GEN,PACK,WORLD |
| `GunWarszawa_AR` | dlc1 | GEN |
| `GunZubr_PP` | base | GEN,PACK,WORLD |
| `MeleeShovel` | base | GEN |

## Ammo (31)

| SID | Definition | Spawn reference |
|---|---|---|
| `A012A` | base | GEN,PACK,WORLD,STASH |
| `A012D` | base | GEN,PACK,WORLD,STASH |
| `A012E` | base | GEN,PACK,WORLD,STASH |
| `A045A` | base | GEN,PACK,WORLD,STASH |
| `A045D` | base | GEN,PACK,WORLD,STASH |
| `A045E` | base | GEN,PACK,STASH |
| `A545A` | base | GEN,PACK,WORLD,STASH |
| `A545D` | base | GEN,PACK,WORLD,STASH |
| `A545E` | base | GEN,PACK,WORLD,STASH |
| `A556A` | base | GEN,PACK,WORLD,STASH |
| `A556D` | base | GEN,PACK,WORLD,STASH |
| `A556E` | base | GEN,PACK,WORLD,STASH |
| `A556S` | base | GEN,PACK,WORLD,STASH |
| `A762NATOA` | base | GEN,PACK,WORLD,STASH |
| `A762NATOD` | base | GEN,PACK,WORLD,STASH |
| `A762NATOS` | base | GEN,PACK,WORLD,STASH |
| `A762SniperA` | base | GEN,PACK,WORLD,STASH |
| `A762SniperD` | base | GEN,PACK,WORLD,STASH |
| `A762SniperS` | base | GEN,PACK,WORLD,STASH |
| `A918A` | base | GEN,PACK,WORLD,STASH |
| `A918D` | base | GEN,PACK,WORLD,STASH |
| `A919A` | base | GEN,PACK,WORLD,STASH |
| `A919D` | base | GEN,PACK,WORLD,STASH |
| `A939A` | base | GEN,PACK,WORLD,STASH |
| `A939D` | base | GEN,PACK,WORLD,STASH |
| `A939E` | base | GEN,PACK,WORLD,STASH |
| `A939S` | base | GEN,PACK,STASH |
| `AGA` | base | GEN,PACK,WORLD |
| `AHEDP` | base | GEN,PACK,WORLD |
| `APG7V` | base | GEN,PACK,WORLD |
| `AVOG` | base | GEN,PACK,WORLD |

## Attachments (99)

| SID | Definition | Spawn reference |
|---|---|---|
| `EN_AutMuz_1` | base | GEN,WORLD |
| `EN_AutMuz_2` | base | GEN,WORLD |
| `EN_BuckLaunch_1` | base | GEN,PACK,WORLD |
| `EN_ColimScope_1` | base | GEN,PACK,WORLD |
| `EN_GLaunch_1` | base | GEN,PACK,WORLD |
| `EN_GoloScope_1` | base | GEN,PACK,WORLD |
| `EN_Grip_1` | base | GEN,WORLD |
| `EN_Laser_1` | base | GEN,WORLD |
| `EN_Silen_1` | base | GEN,PACK,WORLD |
| `EN_Silen_2` | base | GEN,PACK,WORLD |
| `EN_Silen_3` | base | GEN,PACK,WORLD |
| `EN_Silen_4` | base | GEN,PACK,WORLD |
| `EN_SinMuz_1` | base | GEN |
| `EN_SinMuz_2` | base | GEN,WORLD |
| `EN_SinMuz_3` | base | GEN,WORLD |
| `EN_SinMuz_4` | base | GEN,WORLD |
| `EN_SinMuz_5` | base | GEN,WORLD |
| `EN_X2Scope_1` | base | GEN,PACK,WORLD |
| `EN_X3Scope_1` | base | GEN |
| `EN_X4Scope_1` | base | GEN,PACK,WORLD |
| `EN_X8Scope_1` | base | GEN,PACK,WORLD |
| `GunAK74_MagDefault` | base | WORLD |
| `GunAK74_MagIncreased` | base | GEN,PACK,WORLD |
| `GunAK_MagPaired` | base | GEN,PACK,WORLD |
| `GunAKU_MagDefault` | base | WORLD |
| `GunAPB_MagDefault` | base | WORLD |
| `GunAPB_MagIncreased` | base | GEN,PACK,WORLD |
| `GunBucket_MagDefault` | base | WORLD |
| `GunBucket_MagIncreased` | base | GEN,PACK,WORLD |
| `GunD12_MagDefault` | base | WORLD |
| `GunD12_MagIncreased` | base | GEN,PACK,WORLD |
| `GunD12_MagLarge` | base | PACK,WORLD |
| `GunD12_MagPaired` | base | GEN,PACK,WORLD |
| `GunDnipro_MagPaired` | base | GEN,PACK,WORLD |
| `GunDrowned_GLaunch` | base | WORLD |
| `GunFora230_MagIncreased` | base | GEN |
| `GunFora_MagDefault` | base | WORLD |
| `GunFora_MagIncreased` | base | GEN,PACK,WORLD |
| `GunGauss_MagDefault` | base | WORLD |
| `GunGauss_MagIncreased` | base | GEN,WORLD |
| `GunGP37_MagDefault` | base | WORLD |
| `GunGP37_MagLarge` | base | GEN,PACK |
| `GunGP37_MagPaired` | base | GEN,PACK,WORLD |
| `GunGP3A_MagIncreased` | base | GEN |
| `GunGP3A_MagLarge` | base | GEN |
| `GunGrim_MagDefault` | base | WORLD |
| `GunGrim_MagIncreased` | base | GEN,PACK,WORLD |
| `GunGrim_MagLarge` | base | GEN,PACK,WORLD |
| `GunGvintar_MagDefault` | base | WORLD |
| `GunGvintar_MagIncreased` | base | GEN,PACK,WORLD |
| `GunIntegral_MagDefault` | base | WORLD |
| `GunIntegral_MagIncreased` | base | GEN,PACK,WORLD |
| `GunKharod_MagPaired` | base | GEN,PACK |
| `GunKora_MagIncreased` | base | WORLD |
| `GunLavina_MagDefault` | base | WORLD |
| `GunLavina_MagIncreased` | base | GEN,PACK,WORLD |
| `GunM10_MagDefault` | base | WORLD |
| `GunM10_MagIncreased` | base | GEN,PACK,WORLD |
| `GunM16_MagDefault` | base | WORLD |
| `GunM16_MagIncreased` | base | GEN,PACK,WORLD |
| `GunMark_MagDefault` | base | WORLD |
| `GunMark_MagIncreased` | base | GEN,PACK,WORLD |
| `GunPKP_MagDefault` | base | WORLD |
| `GunPKP_MagIncreased` | base | GEN,PACK,WORLD |
| `GunPKP_MagLarge` | base | GEN,PACK,WORLD |
| `GunPM_MagDefault` | base | WORLD |
| `GunPM_MagIncreased` | base | GEN,PACK,WORLD |
| `GunSKP_MagIncreased` | base | GEN |
| `GunSVU_MagDefault` | base | WORLD |
| `GunSVU_MagIncreased` | base | GEN,PACK,WORLD |
| `GunUDP_MagDefault` | base | WORLD |
| `GunUDP_MagIncreased` | base | GEN,PACK,WORLD |
| `GunViper_MagDefault` | base | WORLD |
| `GunViper_MagIncreased` | base | GEN,PACK,WORLD |
| `GunZubr_MagDefault` | base | WORLD |
| `GunZubr_MagIncreased` | base | GEN,PACK,WORLD |
| `RailPicatiniGP37` | base | WORLD |
| `RU_AutMuz_1` | base | GEN,WORLD |
| `RU_AutMuz_2` | base | GEN,WORLD |
| `RU_AutMuz_3` | base | GEN,WORLD |
| `RU_ColimScope_1` | base | GEN,PACK,WORLD |
| `RU_ColimScope_2` | base | GEN |
| `RU_GLaunch_1` | base | GEN,PACK,WORLD |
| `RU_Grip_1` | base | GEN,WORLD |
| `RU_Laser_1` | base | GEN,WORLD |
| `RU_Silen_1` | base | GEN,PACK,WORLD |
| `RU_Silen_2` | base | GEN,PACK,WORLD |
| `RU_Silen_3` | base | GEN,PACK,WORLD |
| `RU_SinMuz_1` | base | GEN,WORLD |
| `RU_SinMuz_2` | base | GEN,WORLD |
| `RU_SinMuz_3` | base | GEN,WORLD |
| `RU_SinMuz_4` | base | GEN,WORLD |
| `RU_SinMuz_5` | base | GEN,WORLD |
| `RU_X2Scope_1` | base | GEN,PACK,WORLD |
| `RU_X4Scope_1` | base | GEN,PACK,WORLD |
| `RU_X8Scope_1` | base | GEN,PACK,WORLD |
| `TopRailGrim` | base | WORLD |
| `TopRailViper` | base | WORLD |
| `TopRailZubr` | base | WORLD |

## Armor (88)

| SID | Definition | Spawn reference |
|---|---|---|
| `Anomaly_Scientific_Armor` | base | GEN,WORLD |
| `Anomaly_Scientific_Armor_PSY_preinstalled` | base | GEN,WORLD |
| `Battle_Dolg_Armor` | base | GEN,WORLD |
| `Battle_Dolg_End_Armor` | base | WORLD |
| `Battle_Military_Helmet` | base | GEN,WORLD |
| `Battle_Monolith_Armor` | base | GEN,WORLD |
| `Battle_Spark_Armor` | base | GEN,WORLD |
| `Battle_Svoboda_Armor` | base | GEN,WORLD |
| `Battle_Varta_Armor` | base | GEN,WORLD |
| `BattleExoskeleton_Varta_Armor` | base | GEN,WORLD |
| `Default_Military_Armor` | base | GEN,WORLD |
| `Exoskeleton_Dolg_Armor` | base | GEN,WORLD |
| `Exoskeleton_Mercenaries_Armor` | base | GEN,WORLD |
| `Exoskeleton_Monolith_Armor` | base | GEN,WORLD |
| `Exoskeleton_Neutral_Armor` | base | GEN,WORLD |
| `Exoskeleton_Svoboda_Armor` | base | GEN,WORLD |
| `Heavy2_Military_Armor` | base | GEN,WORLD |
| `Heavy_Dolg_Armor` | base | GEN,WORLD |
| `Heavy_Duty_Helmet` | base | GEN,WORLD |
| `Heavy_Mercenaries_Armor` | base | GEN |
| `Heavy_Military_Helmet` | base | GEN,WORLD |
| `Heavy_Svoboda_Armor` | base | GEN,WORLD |
| `Heavy_Svoboda_Helmet` | base | GEN,WORLD |
| `Heavy_Varta_Helmet` | base | GEN,WORLD |
| `HeavyAnomaly_Monolith_Armor` | base | GEN,WORLD |
| `HeavyAnomaly_Scientific_Armor` | base | GEN,WORLD |
| `HeavyAnomaly_Spark_Armor` | base | GEN |
| `HeavyBattle_Spark_Armor` | base | GEN,WORLD |
| `HeavyExoskeleton_Dolg_Armor` | base | GEN |
| `HeavyExoskeleton_Monolith_Armor` | base | GEN,WORLD |
| `HeavyExoskeleton_Svoboda_Armor` | base | GEN,WORLD |
| `Jacket_Bandit_Armor` | base | GEN,WORLD |
| `Jemmy_Neutral_Armor` | base | GEN |
| `Light_Bandit_Helmet` | base | GEN,WORLD |
| `Light_Duty_Helmet` | base | GEN,WORLD |
| `Light_Mercenaries_Armor` | base | GEN,WORLD |
| `Light_Mercenaries_Helmet` | base | GEN,WORLD |
| `Light_Military_Helmet` | base | GEN,WORLD |
| `Light_Neutral_Helmet` | base | GEN |
| `Middle_Bandit_Armor` | base | GEN,WORLD |
| `Nasos_Neutral_Armor` | base | GEN,WORLD |
| `Newbee_Neutral_Armor` | base | GEN,WORLD |
| `NPC_Agata_Armor` | base | GEN |
| `NPC_Anomaly_Spark_Armor` | base | GEN |
| `NPC_Battle_Noon_Armor` | base | GEN |
| `NPC_Cloak_Heavy_Military_Armor` | base | GEN |
| `NPC_Cloak_Heavy_Neutral_Armor` | base | GEN |
| `NPC_Dalin_Armor` | base | GEN |
| `NPC_Dekhtyarev_Armor` | base | GEN |
| `NPC_Exoskeleton_Coprs_Armor` | base | GEN |
| `NPC_Exoskeleton_Mercenaries_NORD_Armor` | dlc1 | GEN |
| `NPC_Exoskeleton_MIK_Armor` | dlc1 | GEN |
| `NPC_Exoskeleton_Noon_Armor` | base | GEN |
| `NPC_Faust_Armor` | base | GEN |
| `NPC_Heavy2_Coprs_Armor` | base | GEN |
| `NPC_Heavy3_Corps_Armor` | base | GEN |
| `NPC_Heavy3Exoskeleton_Coprs_Armor` | base | GEN |
| `NPC_Heavy_Corps_Armor` | base | GEN |
| `NPC_Heavy_Mercenaries_NORD_Armor` | dlc1 | GEN |
| `NPC_Heavy_MIK_Armor` | dlc1 | GEN |
| `NPC_Heavy_Military_Armor` | base | GEN |
| `NPC_HeavyAnomaly_Noon_Armor` | base | GEN |
| `NPC_HeavyExoskeleton_Mercenaries_Armor` | base | GEN |
| `NPC_HeavyExoskeleton_Mercenaries_NORD_Armor` | dlc1 | GEN |
| `NPC_HeavyExoskeleton_MIK_Armor` | dlc1 | GEN |
| `NPC_HeavyExoskeleton_Noon_Armor` | base | GEN |
| `NPC_HeavyExoskeleton_Spark_Armor` | base | GEN |
| `NPC_Kaymanov_Armor` | base | GEN |
| `NPC_Korshunov_Armor` | base | GEN |
| `NPC_Korshunov_Armor_2` | base | GEN |
| `NPC_Light_Mercenaries_NORD_Armor` | dlc1 | GEN |
| `NPC_Richter_Armor` | base | GEN |
| `NPC_Sci_Armor` | base | GEN |
| `NPC_Sel_Armor` | base | GEN |
| `NPC_Sel_Neutral_Armor` | base | GEN |
| `NPC_SEVA_MIK_Armor` | dlc1 | GEN |
| `NPC_SkinCloak_Bandit_Armor` | base | GEN |
| `NPC_Spark_Armor` | base | GEN |
| `NPC_Tec_Armor` | base | GEN |
| `Rook_Dolg_Armor` | base | GEN,WORLD |
| `Rook_Svoboda_Armor` | base | GEN,WORLD |
| `SciSEVA_Scientific_Armor` | base | GEN |
| `SEVA_Dolg_Armor` | base | GEN,WORLD |
| `SEVA_Neutral_Armor` | base | GEN,WORLD |
| `SEVA_Spark_Armor` | base | GEN,WORLD |
| `SEVA_Svoboda_Armor` | base | GEN,WORLD |
| `SkinJacket_Bandit_Armor` | base | GEN,WORLD |
| `Zorya_Neutral_Armor` | base | GEN,WORLD |

## Binoculars (3)

| SID | Definition | Spawn reference |
|---|---|---|
| `Binoculars_01` | base | GEN,WORLD |
| `Binoculars_02` | base | GEN,WORLD |
| `Binoculars_03` | base | GEN,WORLD |

## Nvg (7)

| SID | Definition | Spawn reference |
|---|---|---|
| `NVG_Gen1` | base | GEN,WORLD |
| `NVG_Gen2` | base | GEN,WORLD |
| `NVG_Gen3` | base | GEN,WORLD |
| `NVG_Gen3_WP` | base | WORLD |
| `NVG_NPC_Gen1` | base | GEN |
| `NVG_NPC_Gen2` | base | GEN |
| `NVG_NPC_Gen3` | base | GEN |

## Detectors (5)

| SID | Definition | Spawn reference |
|---|---|---|
| `Bear` | base | GEN,PACK |
| `Echo` | base | GEN,PACK,WORLD |
| `Gilka` | base | GEN,PACK,WORLD |
| `Svarog` | dlc1 | WORLD |
| `Veles` | base | GEN,PACK |

## Artifacts (90)

| SID | Definition | Spawn reference |
|---|---|---|
| `AArtifactCNPP_MainFinal` | dlc1 | WORLD |
| `AArtifactCNPP_SmallShard_2` | dlc1 | WORLD |
| `AArtifactCNPP_SmallShard_3` | dlc1 | WORLD |
| `AArtifactCNPP_SmallShard_4` | dlc1 | WORLD |
| `AArtifactCNPP_SmallShard_5` | dlc1 | WORLD |
| `AArtifactCNPP_SmallShard_6` | dlc1 | WORLD |
| `AArtifactCNPPShard` | dlc1 | WORLD |
| `AArtifactWeirdBall` | base | ART,GEN |
| `AArtifactWeirdBolt` | base | ART,GEN |
| `AArtifactWeirdFlower` | base | ART,GEN |
| `AArtifactWeirdKettle` | base | ART,GEN |
| `AArtifactWeirdNut` | base | ART,GEN |
| `AArtifactWeirdWater` | base | ART,GEN |
| `Artifact_WaterDrip` | dlc1 | ART,WORLD |
| `CArtifactBouncyBall` | base | ART,GEN,PACK |
| `CArtifactBubble` | base | ART,GEN,PACK |
| `CArtifactBun` | base | ART,GEN,PACK |
| `CArtifactBung` | base | ART,GEN,PACK |
| `CArtifactChunkMeat` | base | ART,GEN,PACK |
| `CArtifactCottonWool` | base | ART,GEN,PACK,WORLD |
| `CArtifactCrystalThorn` | base | ART,GEN,PACK |
| `CArtifactDevilsMushroom` | base | ART,GEN,PACK |
| `CArtifactEchinus` | base | ART,GEN,PACK |
| `CArtifactKryptonite` | base | ART,GEN,PACK |
| `CArtifactLiquidStone` | base | ART,GEN |
| `CArtifactMica` | base | ART,GEN,PACK,WORLD |
| `CArtifactPellicle` | base | ART,GEN,PACK |
| `CArtifactPlasticine` | base | ART,GEN,PACK |
| `CArtifactRosin` | base | ART,GEN,PACK |
| `CArtifactSlime` | base | ART,GEN,PACK,WORLD |
| `CArtifactSlug` | base | ART,GEN,PACK,WORLD |
| `CArtifactThorn` | base | ART,GEN,PACK |
| `CPrologArtifactSlug` | base | ART |
| `DLC01_ArtifactHeartofChimera` | dlc1 | GEN,WORLD |
| `EArtifactAtom` | base | ART,GEN,PACK |
| `EArtifactBattery` | base | ART,GEN,PACK,WORLD |
| `EArtifactChocolate` | base | ART,GEN,PACK |
| `EArtifactCloud` | base | ART,GEN,PACK |
| `EArtifactCrystalGlass` | base | ART,GEN,PACK |
| `EArtifactDope` | base | ART,GEN |
| `EArtifactDummy` | base | ART,GEN,PACK |
| `EArtifactFlash` | base | ART,GEN,PACK,WORLD |
| `EArtifactJellyFish` | base | ART,GEN,PACK |
| `EArtifactMoonlight` | base | ART,GEN,PACK |
| `EArtifactRazor` | base | ART,GEN,PACK |
| `EArtifactSnowflake` | base | ART,GEN,PACK |
| `EArtifactSoul` | base | ART,GEN,PACK |
| `EArtifactSoul_Fake` | base | WORLD |
| `EArtifactSparkler` | base | ART,GEN,PACK |
| `EArtifactThunderHedgehog` | base | ART,GEN,PACK,WORLD |
| `EArtifactTow` | base | ART,GEN,PACK |
| `EArtifactWorm` | base | ART,GEN,PACK |
| `FArtifactBakedBolts` | base | ART,GEN,PACK |
| `FArtifactBurntHunk` | base | ART,GEN,PACK |
| `FArtifactCandle` | base | ART,GEN,PACK |
| `FArtifactCore` | base | ART,GEN,PACK |
| `FArtifactCrystal` | base | ART,GEN,PACK |
| `FArtifactDeadSponge` | base | ART,GEN,PACK |
| `FArtifactDrops` | base | ART,GEN,PACK |
| `FArtifactEye` | base | ART,GEN,PACK,WORLD |
| `FArtifactFireBall` | base | ART,GEN,PACK,WORLD |
| `FArtifactFireworks` | base | ART,GEN,PACK |
| `FArtifactGlass` | base | ART,GEN,PACK |
| `FArtifactHellishHedgehog` | base | ART,GEN,PACK |
| `FArtifactMomsBeads` | base | ART,GEN,PACK |
| `FArtifactPlasma` | base | ART,GEN,PACK |
| `FArtifactResin` | base | ART,GEN,PACK |
| `FArtifactRingOmnipotence` | base | ART,GEN |
| `FArtifactSteak` | base | ART,GEN,PACK,WORLD |
| `GArtifactBloodStone` | base | ART,GEN,PACK |
| `GArtifactBud` | base | ART,GEN,PACK |
| `GArtifactCompass` | base | ART,GEN |
| `GArtifactGoldFish` | base | ART,GEN,PACK |
| `GArtifactGraphiteBlock` | base | ART,GEN,PACK |
| `GArtifactGravy` | base | ART,GEN,PACK |
| `GArtifactHedgehog` | base | ART,GEN,PACK |
| `GArtifactLandSlug` | base | ART,GEN,PACK |
| `GArtifactNightStar` | base | ART,GEN,PACK |
| `GArtifactPlane` | base | ART,GEN,PACK |
| `GArtifactRubiksCube` | base | ART,GEN,PACK |
| `GArtifactSplitStone` | base | ART,GEN,PACK |
| `GArtifactSponge` | base | ART,GEN,PACK |
| `GArtifactSpring` | base | ART,GEN,PACK |
| `GArtifactStoneDrop` | base | ART,GEN,PACK |
| `GArtifactTrunk` | base | ART,GEN,PACK |
| `GArtifactWrenched` | base | ART,GEN,PACK |
| `PArtifactBrain` | base | ART |
| `QuestArtifactCrystalThorn` | base | ART |
| `QuestArtifactHeartofChornobyl` | base | GEN |
| `SQ13_Soul` | base | ART,WORLD |

