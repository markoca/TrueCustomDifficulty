# Destructible Generator Raw/Local Entry Catalog

Authoritative patch-coordinate view for every locally authored `DestructibleStash*` generator row in the supplied runtime CFG dump. These are raw/local rows, not inheritance-expanded rows.

**40 defined generator roots / 91 local rows / 24 direct item SIDs / 13 nested target SIDs.**

| scope | sid | category | category_node | entry_node | Diff | PlayerRank | ItemPrototypeSID | ItemGeneratorPrototypeSID | Weight | Chance | MinCount | MaxCount | MinDurability | MaxDurability | AmmoMinCount | AmmoMaxCount | AmmoMaxcount | bRequireWeapon | bRequireAmmo | line |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| base | DestructibleStashProlog_AmmoA545D | EItemGenerationCategory::Ammo | [0] | [0] |  |  | A545D |  | 1 |  | 20.0 | 20.0 |  |  |  |  |  |  |  | 22399 |
| base | DestructibleStashProlog_Drink | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Vodka |  |  | 1 | 1.0 | 1.0 |  |  |  |  |  |  |  | 22353 |
| base | DestructibleStashProlog_Drink | EItemGenerationCategory::Consumable | [0] | [1] |  |  | CannedFood |  |  | 1 | 1.0 | 1.0 |  |  |  |  |  |  |  | 22359 |
| base | DestructibleStashProlog_Med | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Medkit |  |  | 1 | 1.0 | 1.0 |  |  |  |  |  |  |  | 22376 |
| base | DestructibleStashProlog_Med | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Bandage |  |  | 1 | 1.0 | 1.0 |  |  |  |  |  |  |  | 22382 |
| base | DestructibleStash_AmmoNATO | EItemGenerationCategory::Ammo | [0] | [0] |  |  | A045D |  | 2 |  | 5.0 | 10.0 |  |  |  |  |  |  |  | 20362 |
| base | DestructibleStash_AmmoNATO | EItemGenerationCategory::Ammo | [0] | [1] |  |  | A919D |  | 2 |  | 5.0 | 10.0 |  |  |  |  |  |  |  | 20368 |
| base | DestructibleStash_AmmoNATO | EItemGenerationCategory::Ammo | [0] | [2] |  |  | A556D |  | 2 |  | 5.0 | 10.0 |  |  |  |  |  |  |  | 20374 |
| base | DestructibleStash_AmmoNATO | EItemGenerationCategory::Ammo | [0] | [3] |  |  | A012D |  | 2 |  | 5.0 | 10.0 |  |  |  |  |  |  |  | 20380 |
| base | DestructibleStash_AmmoSNG | EItemGenerationCategory::Ammo | [0] | [0] |  |  | A918D |  | 2 |  | 3.0 | 6.0 |  |  |  |  |  |  |  | 20507 |
| base | DestructibleStash_AmmoSNG | EItemGenerationCategory::Ammo | [0] | [1] |  |  | A919D |  | 2 |  | 5.0 | 10.0 |  |  |  |  |  |  |  | 20513 |
| base | DestructibleStash_AmmoSNG | EItemGenerationCategory::Ammo | [0] | [2] |  |  | A545D |  | 2 |  | 5.0 | 10.0 |  |  |  |  |  |  |  | 20519 |
| base | DestructibleStash_AmmoSNG | EItemGenerationCategory::Ammo | [0] | [3] |  |  | A012D |  | 2 |  | 2.0 | 4.0 |  |  |  |  |  |  |  | 20525 |
| base | DestructibleStash_Ammo_NATO_Experienced | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoNATO |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 20458 |
| base | DestructibleStash_Ammo_NATO_Master | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoNATO |  | 0.8 | 2.0 | 3.0 |  |  |  |  |  |  |  | 20490 |
| base | DestructibleStash_Ammo_NATO_Newbie | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoNATO |  | 0.5 | 1.0 | 1.0 |  |  |  |  |  |  |  | 20396 |
| base | DestructibleStash_Ammo_NATO_Veteran | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoNATO |  | 0.7 | 2.0 | 2.0 |  |  |  |  |  |  |  | 20474 |
| base | DestructibleStash_Ammo_SNG_Experienced | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoSNG |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 20557 |
| base | DestructibleStash_Ammo_SNG_Master | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoSNG |  | 0.8 | 2.0 | 3.0 |  |  |  |  |  |  |  | 20589 |
| base | DestructibleStash_Ammo_SNG_Newbie | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoSNG |  | 0.5 | 1.0 | 1.0 |  |  |  |  |  |  |  | 20541 |
| base | DestructibleStash_Ammo_SNG_Veteran | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_AmmoSNG |  | 0.7 | 2.0 | 2.0 |  |  |  |  |  |  |  | 20573 |
| base | DestructibleStash_Drink | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Water |  | 0.3 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18594 |
| base | DestructibleStash_Drink | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Beer |  | 0.3 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18600 |
| base | DestructibleStash_Drink | EItemGenerationCategory::Consumable | [0] | [2] |  |  | Vodka |  | 0.2 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18606 |
| base | DestructibleStash_Drink | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Energetic |  | 0.1 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18612 |
| base | DestructibleStash_Drink_Experienced | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Water |  |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18630 |
| base | DestructibleStash_Drink_Experienced | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Beer |  |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18636 |
| base | DestructibleStash_Drink_Experienced | EItemGenerationCategory::Consumable | [0] | [2] |  |  | Vodka |  |  | 0.4 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18642 |
| base | DestructibleStash_Drink_Experienced | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Energetic |  |  | 0.3 | 1.0 | 1.0 |  |  |  |  |  |  |  | 18648 |
| base | DestructibleStash_Drink_Master | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Water |  |  | 0.4 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18700 |
| base | DestructibleStash_Drink_Master | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Beer |  |  | 0.4 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18706 |
| base | DestructibleStash_Drink_Master | EItemGenerationCategory::Consumable | [0] | [2] |  |  | Vodka |  |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18712 |
| base | DestructibleStash_Drink_Master | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Energetic |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18718 |
| base | DestructibleStash_Drink_Veteran | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Water |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18665 |
| base | DestructibleStash_Drink_Veteran | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Beer |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18671 |
| base | DestructibleStash_Drink_Veteran | EItemGenerationCategory::Consumable | [0] | [2] |  |  | Vodka |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18677 |
| base | DestructibleStash_Drink_Veteran | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Energetic |  |  | 0.4 | 1.0 | 1.0 |  |  |  |  |  |  |  | 18683 |
| base | DestructibleStash_F1 | EItemGenerationCategory::Ammo | [0] | [0] |  |  | GrenadeF1 |  | 1 |  | 1.0 | 2.0 |  |  |  |  |  |  |  | 18420 |
| base | DestructibleStash_Food | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Sausage |  | 0.3 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18454 |
| base | DestructibleStash_Food | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Bread |  | 0.3 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18460 |
| base | DestructibleStash_Food | EItemGenerationCategory::Consumable | [0] | [2] |  |  | CannedFood |  | 0.2 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18466 |
| base | DestructibleStash_Food | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Milk |  | 0.1 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18472 |
| base | DestructibleStash_Food_Drink | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_Food | 1 |  |  |  |  |  |  |  |  |  |  | 18734 |
| base | DestructibleStash_Food_Drink | EItemGenerationCategory::SubItemGenerator | [0] | [1] |  |  |  | DestructibleStash_Drink | 1 |  |  |  |  |  |  |  |  |  |  | 18738 |
| base | DestructibleStash_Food_Drink_Experienced | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_Food_Experienced |  | 0.6 |  |  |  |  |  |  |  |  |  | 18752 |
| base | DestructibleStash_Food_Drink_Experienced | EItemGenerationCategory::SubItemGenerator | [0] | [1] |  |  |  | DestructibleStash_Drink_Experienced |  | 0.6 |  |  |  |  |  |  |  |  |  | 18756 |
| base | DestructibleStash_Food_Drink_Master | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_Food_Master |  | 0.8 |  |  |  |  |  |  |  |  |  | 18788 |
| base | DestructibleStash_Food_Drink_Master | EItemGenerationCategory::SubItemGenerator | [0] | [1] |  |  |  | DestructibleStash_Drink_Master |  | 0.8 |  |  |  |  |  |  |  |  |  | 18792 |
| base | DestructibleStash_Food_Drink_Veteran | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_Food_Veteran |  | 0.7 |  |  |  |  |  |  |  |  |  | 18770 |
| base | DestructibleStash_Food_Drink_Veteran | EItemGenerationCategory::SubItemGenerator | [0] | [1] |  |  |  | DestructibleStash_Drink_Veteran |  | 0.7 |  |  |  |  |  |  |  |  |  | 18774 |
| base | DestructibleStash_Food_Experienced | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Sausage |  |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18489 |
| base | DestructibleStash_Food_Experienced | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Bread |  |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18495 |
| base | DestructibleStash_Food_Experienced | EItemGenerationCategory::Consumable | [0] | [2] |  |  | CannedFood |  |  | 0.4 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18501 |
| base | DestructibleStash_Food_Experienced | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Milk |  |  | 0.3 | 1.0 | 1.0 |  |  |  |  |  |  |  | 18507 |
| base | DestructibleStash_Food_Master | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Sausage |  |  | 0.4 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18559 |
| base | DestructibleStash_Food_Master | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Bread |  |  | 0.4 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18565 |
| base | DestructibleStash_Food_Master | EItemGenerationCategory::Consumable | [0] | [2] |  |  | CannedFood |  |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18571 |
| base | DestructibleStash_Food_Master | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Milk |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18577 |
| base | DestructibleStash_Food_Veteran | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Sausage |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18524 |
| base | DestructibleStash_Food_Veteran | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Bread |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18530 |
| base | DestructibleStash_Food_Veteran | EItemGenerationCategory::Consumable | [0] | [2] |  |  | CannedFood |  |  | 0.5 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18536 |
| base | DestructibleStash_Food_Veteran | EItemGenerationCategory::Consumable | [0] | [3] |  |  | Milk |  |  | 0.4 | 1.0 | 1.0 |  |  |  |  |  |  |  | 18542 |
| base | DestructibleStash_Med | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Medkit |  | 1 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 19051 |
| base | DestructibleStash_Med | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Bandage |  | 2 |  | 1.0 | 2.0 |  |  |  |  |  |  |  | 19057 |
| base | DestructibleStash_Med_Experienced | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_Medkit_Experienced |  | 1 |  |  |  |  |  |  |  |  |  | 19074 |
| base | DestructibleStash_Med_Master | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_Medkit_Master |  | 1 |  |  |  |  |  |  |  |  |  | 19104 |
| base | DestructibleStash_Med_Veteran | EItemGenerationCategory::SubItemGenerator | [0] | [0] |  |  |  | DestructibleStash_Medkit_Veteran |  | 1 |  |  |  |  |  |  |  |  |  | 19089 |
| base | DestructibleStash_Medkit_Experienced | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Medkit |  | 20 |  | 1.0 | 2.0 |  |  |  |  |  |  |  | 18836 |
| base | DestructibleStash_Medkit_Experienced | EItemGenerationCategory::Consumable | [0] | [1] |  |  | ArmyMedkit |  | 4 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18842 |
| base | DestructibleStash_Medkit_Experienced | EItemGenerationCategory::Consumable | [0] | [2] |  |  | EcoMedkit |  | 1 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18848 |
| base | DestructibleStash_Medkit_Master | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Medkit |  | 10 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18895 |
| base | DestructibleStash_Medkit_Master | EItemGenerationCategory::Consumable | [0] | [1] |  |  | ArmyMedkit |  | 10 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18901 |
| base | DestructibleStash_Medkit_Master | EItemGenerationCategory::Consumable | [0] | [2] |  |  | EcoMedkit |  | 10 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18907 |
| base | DestructibleStash_Medkit_Newbie | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Medkit |  | 1 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18807 |
| base | DestructibleStash_Medkit_Newbie | EItemGenerationCategory::Consumable | [0] | [1] |  |  | ArmyMedkit |  | 0 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18813 |
| base | DestructibleStash_Medkit_Newbie | EItemGenerationCategory::Consumable | [0] | [2] |  |  | EcoMedkit |  | 0 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18819 |
| base | DestructibleStash_Medkit_Veteran | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Medkit |  | 20 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18865 |
| base | DestructibleStash_Medkit_Veteran | EItemGenerationCategory::Consumable | [0] | [1] |  |  | ArmyMedkit |  | 10 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18871 |
| base | DestructibleStash_Medkit_Veteran | EItemGenerationCategory::Consumable | [0] | [2] |  |  | EcoMedkit |  | 5 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18877 |
| base | DestructibleStash_RGD | EItemGenerationCategory::Ammo | [0] | [0] |  |  | GrenadeRGD5 |  | 1 |  | 1.0 | 2.0 |  |  |  |  |  |  |  | 18437 |
| base | DestructibleStash_Stimulator | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Hercules |  | 2 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18925 |
| base | DestructibleStash_Stimulator | EItemGenerationCategory::Consumable | [0] | [1] |  |  | Cinnamon |  | 3 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18931 |
| base | DestructibleStash_Stimulator | EItemGenerationCategory::Consumable | [0] | [2] |  |  | PSYBlocker |  | 1 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18937 |
| base | DestructibleStash_SupMed_Experienced | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Bandage |  |  | 0.6 | 1.0 | 2.0 |  |  |  |  |  |  |  | 18979 |
| base | DestructibleStash_SupMed_Experienced | EItemGenerationCategory::Consumable | [0] | [1] |  |  | AntiRad |  |  | 0.4 | 1.0 | 1.0 |  |  |  |  |  |  |  | 18985 |
| base | DestructibleStash_SupMed_Master | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Bandage |  |  | 0.8 | 1.0 | 2.0 |  |  |  |  |  |  |  | 19027 |
| base | DestructibleStash_SupMed_Master | EItemGenerationCategory::Consumable | [0] | [1] |  |  | AntiRad |  |  | 0.6 | 1.0 | 1.0 |  |  |  |  |  |  |  | 19033 |
| base | DestructibleStash_SupMed_Newbie | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Bandage |  | 1 |  | 1.0 | 2.0 |  |  |  |  |  |  |  | 18955 |
| base | DestructibleStash_SupMed_Newbie | EItemGenerationCategory::Consumable | [0] | [1] |  |  | AntiRad |  | 0 |  | 1.0 | 1.0 |  |  |  |  |  |  |  | 18961 |
| base | DestructibleStash_SupMed_Veteran | EItemGenerationCategory::Consumable | [0] | [0] |  |  | Bandage |  |  | 0.7 | 1.0 | 2.0 |  |  |  |  |  |  |  | 19003 |
| base | DestructibleStash_SupMed_Veteran | EItemGenerationCategory::Consumable | [0] | [1] |  |  | AntiRad |  |  | 0.5 | 1.0 | 1.0 |  |  |  |  |  |  |  | 19009 |
