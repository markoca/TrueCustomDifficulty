# NEXT: Authored stash gear + special-weapon randomization backlog

**FROZEN / RESERVED FOR FUTURE FEATURE — DO NOT RETUNE WITH STASH CONTENT OR MEDICAL SLIDERS.**

This is the prominent master queue for a future, explicitly designed **gear and unique/special weapon randomizer**. It contains exactly **45 original authored source entries** (**31 distinct ItemPrototypeSIDs, 41 different physical ordinary WorldMap stash actors**) whose locations, generator source roots, category/item indices, authored chance/weight and stack counts are tracked in the fully source-preserving [CSV backlog](stash-gear-unique-randomization-backlog.csv). The original [80-row combined source inventory](regular-stash-direct-additions-2026-10-16.csv) remains the audit authority.

**Not implemented yet:** no shuffle/pool membership, percentage, weapon swaps, attachment swaps, rarity migration or preset/GUI control is authorized by this catalog. `Gun_Trophy_AR` has a notable name but a special/unique identity must be validated from game source; **do not treat all 14 authored guns as unique**. Current ItemGenerator wrappers must preserve all 45 authored fields unchanged. Upcoming generated-CFG tests explicitly reject changes to any of these row identities when adjusting medical items.

| Reserved slot type | Source entries | Distinct ItemPrototypeSIDs |
| --- | ---: | ---: |
| Weapons (including future unique-weapon candidates) | 14 | 9 |
| Magazine attachments | 9 | 9 |
| Optics | 11 | 5 |
| Suppressors | 3 | 2 |
| Helmets | 7 | 5 |
| Armor | 1 | 1 |
| **TOTAL** | **45** | **31 across all groups** |

## Exact reserved source rows

Keep these rows even if a future list of candidate pools or exclusions is created: the source actor + root + original item index is the stable reference.

| Pool | Physical stash (source root) | ItemPrototypeSID | ActorSID | Category / item |
| --- | --- | --- | --- | --- |
| OPTIC | `Wild_Island_AzimuthStation_StashGenerator4` | `EN_GoloScope_1` | `0363359F46F9B92DBB1B709FCFA23C28` | `[1]/[0]` |
| OPTIC | `CoolingTowers_BurningFireDepartment_StashGenerator1` | `EN_X3Scope_1` | `05335F754265D92E87B9CC9144578B22` | `[1]/[0]` |
| WEAPON | `CityCenter_PolissyaHotel_StashGenerator3` | `GunRam2_SG` | `07727ED446A8EB9D0F56B3ABB9542CAD` | `[1]/[0]` |
| WEAPON | `Wild_Island_WasteProcessingStation_StashGenerator3` | `GunSVDM_SP` | `0CC8DAA64C554EF5C010439FE1CC448B` | `[1]/[0]` |
| OPTIC | `CementFactory_IntercomTower_StashGenerator3` | `EN_X2Scope_1` | `0D4342DC44D4FB5A0824D8B70DDCA75D` | `[1]/[0]` |
| MAGAZINE | `CementFactory_IntercomTower_StashGenerator3` | `GunIntegral_MagIncreased` | `0D4342DC44D4FB5A0824D8B70DDCA75D` | `[1]/[1]` |
| WEAPON | `Promzona_StorageontheHill_StashGenerator2` | `Gun_Trophy_AR` | `1CBBBE27428A1766B7F911863C044833` | `[1]/[0]` |
| HELMET | `Promzona_AhropromResearchInstitute_StashGenerator2` | `Light_Military_Helmet` | `209A672C41CCF8C6F5DFA4810949D5B3` | `[1]/[0]` |
| OPTIC | `Wild_Island_FloodedPumpingStation_StashGenerator3` | `EN_GoloScope_1` | `2277ADFB43399E29A1220F9ED7D27C47` | `[1]/[0]` |
| WEAPON | `Garbage_VehicleGraveyard_StashGenerator1` | `GunM860_SG` | `393A3DB04B0C6906EBF05299C8E3DEEE` | `[1]/[0]` |
| SUPPRESSOR | `LesserZone_PoppyField_StashGenerator2` | `EN_Silen_1` | `42DF8E684B055AF1A49F94978BE8F421` | `[1]/[0]` |
| MAGAZINE | `Yantar_RozsokhaRWD_StashGenerator7` | `GunKharod_MagPaired` | `4A286A304F30AA378F0504B74E5E0F23` | `[1]/[0]` |
| WEAPON | `Garbage_Plant_StashGenerator7` | `GunBucket_PP` | `4AB2B2CF48D0FC16B2E853BF27922273` | `[1]/[0]` |
| MAGAZINE | `BurntForest_FairyTalePioneerCamp_StashGenerator4` | `GunGP3A_MagIncreased` | `5C38F2F64D86FB1F126BCFA5ED94088D` | `[1]/[0]` |
| OPTIC | `CementFactory_ConcreteForest_StashGenerator1` | `EN_GoloScope_1` | `5E46BB184CC7F072850FF49355A79793` | `[1]/[0]` |
| MAGAZINE | `STCMalachite_BrainScorcher_StashGenerator5` | `GunZubr_MagIncreased` | `63BA520740F12C6B265A0399997684B0` | `[1]/[0]` |
| HELMET | `BurntForest_RepairStation_StashGenerator1` | `Light_Bandit_Helmet` | `6817326B419511D69454EBB42F23C84A` | `[1]/[0]` |
| WEAPON | `Promzona_Х5Lab_StashGenerator3` | `GunAPB_HG` | `837831724DFAE2B9000E369F7D767BA3` | `[1]/[0]` |
| OPTIC | `Rostok_VolkhovSAM_StashGenerator2` | `EN_X4Scope_1` | `88D34BFA4BE526FEBBF2B4ABC6B38AE2` | `[1]/[0]` |
| WEAPON | `ChemicalPlant_ChemicalTanks_StashGenerator6` | `GunUDP_HG` | `8E3157704F88075AAB9F519551CDA57D` | `[1]/[0]` |
| SUPPRESSOR | `ChemicalPlant_ChemicalTanks_StashGenerator6` | `EN_Silen_1` | `8E3157704F88075AAB9F519551CDA57D` | `[2]/[0]` |
| OPTIC | `Garbage_Depot_StashGenerator1` | `EN_GoloScope_1` | `8E67770847649D9D16DF58AB190270B4` | `[1]/[0]` |
| HELMET | `Garbage_TheWardsOutpost_StashGenerator2` | `Light_Bandit_Helmet` | `8EDCE5744E3A84ADBD2D87AAB8FD44FA` | `[1]/[0]` |
| WEAPON | `LesserZone_WastewaterTreatmentPlant_StashGenerator1` | `GunUDP_HG` | `91D632284EAEDA16F6CD7098A676611F` | `[1]/[0]` |
| HELMET | `LesserZone_Cowsheds_StashGenerator1` | `Light_Neutral_Helmet` | `924466E6468A0A09842CBCA865793D57` | `[1]/[0]` |
| OPTIC | `BurntForest_Cherevach_StashGenerator3` | `EN_X4Scope_1` | `9B1C9128455B34ABC8449EB52F25890C` | `[1]/[0]` |
| MAGAZINE | `BurntForest_Cherevach_StashGenerator3` | `GunLavina_MagIncreased` | `9B1C9128455B34ABC8449EB52F25890C` | `[1]/[1]` |
| SUPPRESSOR | `LesserZone_BoilerHouse_StashGenerator1` | `RU_Silen_1` | `9B388D9C4782AA6D733A3BB4877769B8` | `[1]/[0]` |
| WEAPON | `Jupiter_Market_StashGenerator2` | `GunMark_SP` | `A3B04A9949CA27AA3E62FD9F3CD08ECD` | `[1]/[0]` |
| WEAPON | `LesserZone_ARMS_StashGenerator2` | `GunObrez_SG` | `A4D6A1EA48DDDEE35883379DFFD93F81` | `[1]/[0]` |
| HELMET | `Backwater_CanningFactory_StashGenerator2` | `Light_Neutral_Helmet` | `B233B08F402FC792B705B8873A43B15A` | `[1]/[0]` |
| WEAPON | `Garbage_Factory_StashGenerator4` | `GunM860_SG` | `B3F81A794CB50D8DD300F794E6FDAF25` | `[1]/[0]` |
| ARMOR | `Promzona_StorageontheHill_StashGenerator6` | `SEVA_Neutral_Armor` | `B7D42B0E4B38CF5EA07443A6F7B576A3` | `[1]/[0]` |
| WEAPON | `Promzona_StorageontheHill_StashGenerator2` | `GunSVDM_SP` | `BB1EF91D4CEC1424FF522FBD6DCA3148` | `[1]/[0]` |
| HELMET | `Yantar_TunnelunderLake_StashGenerator3` | `Heavy_Duty_Helmet` | `BD07510A41003ACB169B1889BC08A29E` | `[1]/[0]` |
| OPTIC | `Promzona_ArmyWarehouses_StashGenerator1` | `EN_X2Scope_1` | `BFD606894964956318B5519609B326B0` | `[2]/[0]` |
| MAGAZINE | `Promzona_ArmyWarehouses_StashGenerator1` | `GunMark_MagIncreased` | `BFD606894964956318B5519609B326B0` | `[2]/[1]` |
| MAGAZINE | `CoolingTowers_SouthCoolingTower_StashGenerator1` | `GunAK74_MagIncreased` | `C0738FF24DCA4386C382EA9FB6274248` | `[1]/[0]` |
| WEAPON | `Garbage_DigSite_StashGenerator2` | `GunBucket_PP` | `C59BE52D481BCA2E1EB1CE816B67EF71` | `[1]/[0]` |
| MAGAZINE | `Garbage_DetentionCenter_StashGenerator3` | `GunViper_MagIncreased` | `CC6A6867427289A4DED2099C745EEE90` | `[1]/[0]` |
| OPTIC | `LesserZone_NorthernCheckpoint_StashGenerator1` | `RU_X4Scope_1` | `CE3C517D422A62D2F61ECA9A396D06C9` | `[1]/[0]` |
| HELMET | `Rostok` | `Heavy_Varta_Helmet` | `D0D974B342233E90E494C1BE1CB21240` | `[1]/[0]` |
| MAGAZINE | `CementFactory_Cage_StashGenerator3` | `GunFora230_MagIncreased` | `D996FC754ED61F566A3B0BB1EC9AE50F` | `[1]/[0]` |
| WEAPON | `Promzona_AhropromUnderground_StashGenerator3` | `GunMark_SP` | `E67E4BD743B8868C0D9715826155DA1D` | `[1]/[0]` |
| OPTIC | `Garbage_Terikon_StashGenerator1_Scope` | `RU_X4Scope_1` | `FAFCEB6246023B52AB4C0E9D4AC7A3AF` | `[1]/[0]` |

## Design steps when resumed

1. Audit which weapon SIDs are genuinely unique, which are ordinary, and which slots are weapon, armor or attachment-specific. Preserve source rank/tier and stash quality.
2. Define separate compatible replacement pools for weapon type, optic mounting, magazines, suppressors, helmet and suit; specify whether truly unique gear can be in a global shuffle at all.
3. Avoid spawning impossible weapon/attachment combinations, duplicate quest rewards or changing 106 protected blueprint/named-clue actors.
4. Create precise per-item BPatch assignments only for source-approved ordinary actors. Preserve existing native `Chance`, count and authored source until explicitly directed otherwise.
5. Add generated-CFG no-loss tests for all 45 originals and reverse/seed repeatability before adding UI controls.

**Source verification:** `python tools/audit_regular_stash_consumables.py` confirms this separate queue still equals the 45 `OTHER_AUTHORED` rows from the source inventory. The current 10-row direct medical extension touches **none** of these 45 gear records.
