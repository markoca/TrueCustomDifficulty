# ItemGenerator Usage Footprint — Normal Gameplay

This report ranks **repeatable production gameplay usage**, not literal player encounter telemetry. CFG files tell us how many production consumers reference a generator, but not how often a player actually opens a container or kills a spawned archetype.

Two metrics are used:

- **Direct footprint** — production consumer links that point straight at the generator.
- **Propagated footprint** — production consumer links that depend on the generator through `SubItemGenerator` composition or inheritance. This exposes shared backbone pools hidden under location/faction wrappers.

Excluded from the ranking: test/debug, Prologue-only, one-time/story-scripted generators, named/scripted human NPCs, quest rewards, traders, player gear, templates and demos.

## What matters most first

| Priority | Generator / family | Normal-play footprint | What it can offer | Why it matters |
|---|---|---:|---|---|
| 1 | `GamePass_Stash_ItemGenerator_Cheap` | 1,281 propagated links | 12-gauge / 9×18 / 9×19 ammo; Bandage; Medkit; Bread; Sausage; Canned Food; Milk; Water; Vodka; Energy Drink; RGD-5; Common/Rare/Epic Coupons | Largest hidden stash backbone: many location wrappers inherit/compose it. |
| 2 | `DestructibleStash_Food_Drink*` | 1,415 unique production destructible consumers across the family | Bread; Canned Food; Milk; Sausage; Water; Vodka; Beer; Energy Drink | The most common breakable-container food/drink surface. |
| 3 | `DestructibleStash_AmmoSNG` | 984 direct links | 12-gauge; 5.45×39; 9×18; 9×19 ammo | Huge breakable-crate ammo surface. |
| 4 | `DestructibleStash_Med*` | 876 unique production destructible consumers across the family | Bandage; Medkit; higher-rank Medkit / Army Medkit / Scientific Medkit pools | Huge breakable medical surface. |
| 5 | `GamePass_Stash_ItemGenerator_Common_Var1` | 480 propagated ItemContainer links | 8 ammo families + Bandage, Medkit, Energy Drink, Vodka, Coupons | Common stash backbone under many wrappers. |
| 6 | `GamePass_Stash_ItemGenerator_Rare` | 348 propagated links | higher-grade ammo; Anti-Rad; Army Medkit; Bandage; Canned Food; Energy Drink; RGD-5; Coupons | Rare stash backbone; also reaches one standard-human path, so blast radius is broader. |
| 7 | `Stash_Cheap_*` | 214 unique production consumers across rank variants | same 16-item cheap pool | Most frequently direct-used rank-based stash family. |
| 8 | `GeneralNPC_Consumables_Recon` | 30 standard-human archetype paths; 61 all repeatable propagated paths | Anti-Rad; Bandage; Bread; Canned Food; Energy Drink; Medkit | Most reused human corpse consumable sub-pool. |
| 9 | `GeneralNPC_Consumables_Stormtrooper` | 28 standard-human paths; 60 repeatable propagated paths | Anti-Rad; Bandage; Bread; Medkit; Vodka | Another major corpse/loadout backbone. |
| 10 | Mutant species generators | 1–8 mutant prototype consumers each | one species part per generator; Electrocollar is separate | Simple, isolated species loot; usage depends on encounters, not static actor count. |

**Important:** the four rank variants of a stash family often point at the *same physical containers*. Do not sum Newbie + Experienced + Veteran + Master and call that four times as many stashes. The family-level unique consumer count above de-duplicates those physical roots.

## Breakable crates / destructibles

| Direct links | Generator | Reachable items |
|---:|---|---|
| 1214 | `DestructibleStash_Food_Drink` | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 994 | `DestructibleStash_Food_Drink_Experienced` | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 994 | `DestructibleStash_Food_Drink_Master` | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 994 | `DestructibleStash_Food_Drink_Veteran` | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 984 | `DestructibleStash_AmmoSNG` | 12-gauge ammo, 5.45×39 ammo, 9×18 ammo, 9×19 ammo |
| 870 | `DestructibleStash_Med` | Bandage, Medkit |
| 554 | `DestructibleStash_Med_Experienced` | Army Medkit, Scientific Medkit, Medkit |
| 554 | `DestructibleStash_Med_Master` | Army Medkit, Scientific Medkit, Medkit |
| 554 | `DestructibleStash_Med_Veteran` | Army Medkit, Scientific Medkit, Medkit |
| 253 | `DestructibleStash_AmmoNATO` | 12-gauge ammo, .45 ACP ammo, 5.56×45 ammo, 9×19 ammo |
| 101 | `DestructibleStash_F1` | F1 Grenade |
| 81 | `DestructibleStash_RGD` | RGD-5 Grenade |

## World stashes / ItemContainers

| Direct links | Generator | Reachable items |
|---:|---|---|
| 213 | `Stash_Cheap_Newbie` | 12-gauge ammo, 9×18 ammo, 9×19 ammo, Bandage, Bread, CannedFood, Energy Drink, RGD-5 Grenade, Medkit, Milk, Coupons (Common), Coupons (Epic), Coupons (Rare), Sausage, Vodka, Water |
| 212 | `Stash_Cheap_Experienced` | 12-gauge ammo, 9×18 ammo, 9×19 ammo, Bandage, Bread, CannedFood, Energy Drink, RGD-5 Grenade, Medkit, Milk, Coupons (Common), Coupons (Epic), Coupons (Rare), Sausage, Vodka, Water |
| 212 | `Stash_Cheap_Master` | 12-gauge ammo, 9×18 ammo, 9×19 ammo, Bandage, Bread, CannedFood, Energy Drink, RGD-5 Grenade, Medkit, Milk, Coupons (Common), Coupons (Epic), Coupons (Rare), Sausage, Vodka, Water |
| 212 | `Stash_Cheap_Veteran` | 12-gauge ammo, 9×18 ammo, 9×19 ammo, Bandage, Bread, CannedFood, Energy Drink, RGD-5 Grenade, Medkit, Milk, Coupons (Common), Coupons (Epic), Coupons (Rare), Sausage, Vodka, Water |
| 128 | `GamePass_Stash_ItemGenerator_Cheap` | 12-gauge ammo, 9×18 ammo, 9×19 ammo, Bandage, Bread, CannedFood, Energy Drink, RGD-5 Grenade, Medkit, Milk, Coupons (Common), Coupons (Epic), Coupons (Rare), Sausage, Vodka, Water |
| 99 | `GamePass_Stash_ItemGenerator_Common_Var1` | 12-gauge ammo, .45 ACP ammo, 5.45×39 ammo, 5.56×45 ammo, 7.62×54R ammo, 7.62×54R ammo (alt.), 9×19 ammo, 9×39 ammo, Bandage, Energy Drink, Medkit, Coupons (Common), Coupons (Epic), Coupons (Rare), Vodka |
| 82 | `GamePass_Stash_ItemGenerator_Rare` | 12-gauge ammo (alt.), 12-gauge ammo (alt.), .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R ammo (alt.), 7.62×54R ammo, 9×18 ammo (alt.), 9×19 ammo (alt.), 9×39 ammo (alt.), Anti-Rad, Army Medkit, Bandage, CannedFood, Energy Drink, R… |
| 37 | `Stash_Medium_Experienced` | 12-gauge ammo, .45 ACP ammo, 5.45×39 ammo, 5.56×45 ammo, 7.62×54R ammo, 7.62×54R ammo (alt.), 9×19 ammo, 9×39 ammo, Bandage, Energy Drink, Medkit, Coupons (Common), Coupons (Epic), Coupons (Rare), Vodka |
| 37 | `Stash_Medium_Master` | 12-gauge ammo, .45 ACP ammo, 5.45×39 ammo, 5.56×45 ammo, 7.62×54R ammo, 7.62×54R ammo (alt.), 9×19 ammo, 9×39 ammo, Bandage, Energy Drink, Medkit, Coupons (Common), Coupons (Epic), Coupons (Rare), Vodka |
| 37 | `Stash_Medium_Newbie` | 12-gauge ammo, .45 ACP ammo, 5.45×39 ammo, 5.56×45 ammo, 7.62×54R ammo, 7.62×54R ammo (alt.), 9×19 ammo, 9×39 ammo, Bandage, Energy Drink, Medkit, Coupons (Common), Coupons (Epic), Coupons (Rare), Vodka |
| 37 | `Stash_Medium_Veteran` | 12-gauge ammo, .45 ACP ammo, 5.45×39 ammo, 5.56×45 ammo, 7.62×54R ammo, 7.62×54R ammo (alt.), 9×19 ammo, 9×39 ammo, Bandage, Energy Drink, Medkit, Coupons (Common), Coupons (Epic), Coupons (Rare), Vodka |
| 27 | `Stash_Expensive_Experienced` | 12-gauge ammo (alt.), 12-gauge ammo (alt.), .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R ammo (alt.), 7.62×54R ammo, 9×18 ammo (alt.), 9×19 ammo (alt.), 9×39 ammo (alt.), Anti-Rad, Army Medkit, Bandage, CannedFood, Energy Drink, R… |

## Standard human NPC loadouts / normal corpse loot

| Direct links | Generator | Reachable items |
|---:|---|---|
| 3 | `GeneralNPC_Bandit_CloseCombat_ItemGenerator` | Bandage, Beer, Echo, RGD-5 Grenade, GunM10_HG, GunM860_SG, GunObrez_SG, GunPM_HG, GunSPSA_SG, GunTOZ_SG, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_NPC_Gen2, Sausage, SkinJacket_Bandi… |
| 3 | `GeneralNPC_Bandit_Heavy_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, CannedFood, Echo, RGD-5 Grenade, GunAK74_ST, GunAKU_PP, GunG37_ST, GunPM_HG, GunThreeLine_SP, GunViper_PP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_N… |
| 3 | `GeneralNPC_Bandit_Recon_ItemGenerator` | Anti-Rad, Bandage, Bread, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunAKU_PP, GunBucket_PP, GunFora230_PP, GunM10_HG, GunPM_HG, GunViper_PP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen… |
| 3 | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | Anti-Rad, Bandage, Bread, Echo, RGD-5 Grenade, GunAK74_ST, GunAKU_PP, GunG37_ST, GunPM_HG, GunViper_PP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_NPC_Gen2, SkinJacket_Bandit_Armor, V… |
| 3 | `GeneralNPC_Varta_CloseCombat_ItemGenerator` | Bandage, BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, Beer, Echo, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunM860_SG, GunPM_HG, GunSPSA_SG, GunUDP_HG, GunViper_PP, Heavy_Varta_Helmet, Medkit, NVG_NPC_Gen1, NVG_NPC_Gen2, NVG_NPC_Gen3, Sausage |
| 3 | `GeneralNPC_Varta_Heavy_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, CannedFood, Echo, RGD-5 Grenade, GunAK74_ST, GunAPB_HG, GunFora_ST, GunG37_ST, GunM16_ST, GunPKP_MG, GunPM_HG, GunUDP_HG, Heavy_Varta_Helmet, Medkit, NVG_NPC_Gen1, NVG_NPC_Ge… |
| 3 | `GeneralNPC_Varta_Recon_ItemGenerator` | Anti-Rad, Bandage, BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, Bread, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunBucket_PP, GunFora230_PP, GunIntegral_PP, GunPM_HG, GunUDP_HG, GunViper_PP, GunZubr_PP, Heavy_Varta_Helmet, … |
| 3 | `GeneralNPC_Varta_Sniper_ItemGenerator` | Army Medkit, Bandage, BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, Bread, CannedFood, Echo, Energy Drink, F1 Grenade, GunAK74_ST, GunAPB_HG, GunFora_ST, GunM16_ST, GunM701_SP, GunMark_SP, GunPM_HG, GunUDP_HG, Heavy_Varta_Helmet, NVG_NPC_Gen1, NVG_NPC_… |
| 3 | `GeneralNPC_Varta_Stormtrooper_ItemGenerator` | Anti-Rad, Bandage, BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, Bread, Echo, RGD-5 Grenade, GunAK74_ST, GunAPB_HG, GunArev_ST, GunDnipro_ST, GunFora_ST, GunG37_ST, GunKharod_ST, GunM16_ST, GunPM_HG, GunUDP_HG, Heavy_Varta_Helmet, Medkit, NVG_NPC_Gen1,… |
| 2 | `GeneralNPC_Duty_CloseCombat_ItemGenerator` | Bandage, Battle_Dolg_Armor, Beer, Echo, Exoskeleton_Dolg_Armor, RGD-5 Grenade, GunAPB_HG, GunD12_SG, GunM860_SG, GunPM_HG, GunTOZ_SG, HeavyExoskeleton_Dolg_Armor, Heavy_Dolg_Armor, Heavy_Duty_Helmet, Light_Duty_Helmet, Medkit, NVG_NPC_Gen1, NVG_NPC_Gen2, NV… |
| 2 | `GeneralNPC_Duty_Heavy_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Battle_Dolg_Armor, CannedFood, Echo, Exoskeleton_Dolg_Armor, RGD-5 Grenade, GunAK74_ST, GunAPB_HG, GunGrim_ST, GunGvintar_ST, GunLavina_ST, GunPKP_MG, GunPM_HG, HeavyExoskeleton_Dolg_Armor, Heavy_Dolg_Armor, Heavy_Duty_Helmet… |
| 2 | `GeneralNPC_Duty_Recon_ItemGenerator` | Anti-Rad, Bandage, Battle_Dolg_Armor, Bread, CannedFood, Echo, Energy Drink, Exoskeleton_Dolg_Armor, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunBucket_PP, GunPM_HG, GunZubr_PP, HeavyExoskeleton_Dolg_Armor, Heavy_Dolg_Armor, Heavy_Duty_Helmet, Light_Duty_Helmet… |

Human corpses do **not** use one universal ItemGenerator. The directly referenced roots are faction × role loadouts. Their most reused nested consumable pools are:

| Standard-human paths | Shared sub-pool | Items |
|---:|---|---|
| 30 | `GeneralNPC_Consumables_Recon` | Anti-Rad, Bandage, Bread, CannedFood, Energy Drink, Medkit |
| 28 | `GeneralNPC_Consumables_Stormtrooper` | Anti-Rad, Bandage, Bread, Medkit, Vodka |
| 27 | `GeneralNPC_Consumables_CloseCombat` | Bandage, Beer, Medkit, Sausage |
| 24 | `GeneralNPC_Consumables_Sniper` | Army Medkit, Bandage, Bread, CannedFood, Energy Drink |
| 20 | `GeneralNPC_Varta_Armor` | BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, Heavy_Varta_Helmet |
| 16 | `GeneralNPC_Bandit_Armor` | Jacket_Bandit_Armor, Light_Bandit_Helmet, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, SkinJacket_Bandit_Armor |
| 15 | `GeneralNPC_Varta_WeaponPistol` | GunAPB_HG, GunPM_HG, GunUDP_HG |
| 15 | `GeneralNPC_Varta_NVG` | NVG_NPC_Gen1, NVG_NPC_Gen2, NVG_NPC_Gen3 |

## Zombie NPC loadouts

| Direct links | Generator | Reachable items |
|---:|---|---|
| 2 | `CNPPZombie_Varta_CloseCombat_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, Beer, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunM860_SG, GunPM_HG, GunSPSA_SG, GunUDP_HG, GunViper_PP, Heavy_Varta_Helmet, Medkit, NVG_NPC_Gen1,… |
| 2 | `GeneralNPC_Neutral_Recon_ItemGenerator` | Anti-Rad, Bandage, Bread, CannedFood, Echo, Energy Drink, Exoskeleton_Neutral_Armor, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunBucket_PP, GunFora230_PP, GunIntegral_PP, GunKora_HG, GunM10_HG, GunPM_HG, GunUDP_HG, GunViper_PP, Jemmy_Neutral_Armor, Light_Neutra… |
| 2 | `GeneralZombie_Freedom_CloseCombat_ItemGenerator` | Bandage, Battle_Svoboda_Armor, Beer, Echo, Exoskeleton_Svoboda_Armor, RGD-5 Grenade, GunKora_HG, GunM10_HG, GunM860_SG, GunRam2_SG, GunSPSA_SG, GunUDP_HG, HeavyExoskeleton_Svoboda_Armor, Heavy_Svoboda_Armor, Heavy_Svoboda_Helmet, Medkit, NVG_NPC_Gen1, NVG_N… |
| 2 | `GeneralZombie_Freedom_Sniper_ItemGenerator` | Army Medkit, Bandage, Battle_Svoboda_Armor, Bread, CannedFood, Echo, Energy Drink, Exoskeleton_Svoboda_Armor, F1 Grenade, GunGP3A_DMR, GunKora_HG, GunM10_HG, GunM701_SP, GunMark_SP, GunUDP_HG, HeavyExoskeleton_Svoboda_Armor, Heavy_Svoboda_Armor, Heavy_Svobo… |
| 2 | `GeneralZombie_Monolith_CloseCombat_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Battle_Military_Helmet, Battle_Monolith_Armor, CannedFood, Echo, Energy Drink, Exoskeleton_Monolith_Armor, RGD-5 Grenade, GunAPB_HG, GunD12_SG, GunM860_SG, GunRam2_SG, GunSPSA_SG, GunUDP_HG, HeavyAnomaly_Monolith_Armor, Heavy… |
| 2 | `GeneralZombie_Monolith_Recon_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Battle_Military_Helmet, Battle_Monolith_Armor, CannedFood, Echo, Energy Drink, Exoskeleton_Monolith_Armor, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunBucket_PP, GunFora230_PP, GunGauss_SP, GunIntegral_PP, GunUDP_HG, GunViper_PP,… |
| 2 | `GeneralZombie_Monolith_Sniper_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Battle_Military_Helmet, Battle_Monolith_Armor, CannedFood, Echo, Energy Drink, Exoskeleton_Monolith_Armor, F1 Grenade, GunAPB_HG, GunGP3A_DMR, GunGauss_SP, GunM701_SP, GunMark_SP, GunSKP_DMR, GunSVDM_SP, GunSVU_SP, GunUDP_HG,… |
| 2 | `GeneralZombie_Monolith_Stormtrooper_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Battle_Military_Helmet, Battle_Monolith_Armor, CannedFood, Echo, Energy Drink, Exoskeleton_Monolith_Armor, RGD-5 Grenade, GunAPB_HG, GunFora_ST, GunG37_ST, GunGauss_SP, GunGrim_ST, GunKharod_ST, GunLavina_ST, GunM16_ST, GunUD… |
| 1 | `CNPPZombie_Bandit_CloseCombat_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Beer, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunM10_HG, GunM860_SG, GunPM_HG, GunSPSA_SG, GunTOZ_SG, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG… |
| 1 | `CNPPZombie_Bandit_Heavy_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunAK74_ST, GunG37_ST, GunPM_HG, GunThreeLine_SP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_NPC_Gen2, S… |
| 1 | `CNPPZombie_Bandit_Recon_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Bread, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunAKU_PP, GunBucket_PP, GunFora230_PP, GunM10_HG, GunPM_HG, GunViper_PP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor… |
| 1 | `CNPPZombie_Bandit_Stormtrooper_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Bread, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunAK74_ST, GunG37_ST, GunPM_HG, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_NPC_Gen2, SkinJacket_… |

## Mutant loot

| Direct links | Generator | Reachable items |
|---:|---|---|
| 8 | `PoltergeistLootGenerator` | Poltergeist Part |
| 7 | `MutantElectrocollarGenerator` | Electrocollar |
| 4 | `BloodsuckerLootGenerator` | Bloodsucker Part |
| 3 | `BoarLootGenerator` | Boar Part |
| 2 | `BlinddogLootGenerator` | Blinddog Part |
| 2 | `BurerLootGenerator` | Burer Part |
| 2 | `CatLootGenerator` | Bayun Part |
| 2 | `ChimeraLootGenerator` | Chimera Part |
| 2 | `ControllerLootGenerator` | Controller Part |
| 2 | `FleshLootGenerator` | Flesh Part |
| 2 | `PseudodogLootGenerator` | Pseudodog Part |
| 2 | `PseudogiantLootGenerator` | Pseudogiant Part |
| 2 | `SnorkLootGenerator` | Snork Part |
| 1 | `DLC01_Zulus_ItemGenerator` | Heavy_Dolg_Armor, MeleeShovel |
| 1 | `DeerLootGenerator` | Deer Part |
| 1 | `MavkaMain_ItemGenerator` | Anti-Rad, Bandage, Bread, F1 Grenade, GunHSG72_DMR, Medkit, Vodka |
| 1 | `TushkanLootGenerator` | Tushkan Part |

## Explicit DeadBody CorpseStash overrides

| Direct links | Generator | Reachable items |
|---:|---|---|
| 12 | `NeutralMasterItemGenerator` | 12-gauge ammo (alt.), 12-gauge ammo, 12-gauge ammo (alt.), .45 ACP ammo (alt.), .45 ACP ammo, .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.45×39 ammo, 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 5.56×45 ammo, 5.56×45 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R … |
| 4 | `GeneralNPC_Militaries_Heavy_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, Battle_Military_Helmet, CannedFood, Default_Military_Armor, Echo, RGD-5 Grenade, GunAK74_ST, GunAPB_HG, GunGrim_ST, GunGvintar_ST, GunPKP_MG, GunPM_HG, GunThreeLine_SP, Heavy2_Military_Armor, Heavy_Military_Helmet, Light_Mili… |
| 4 | `GeneralNPC_Militaries_Sniper_ItemGenerator` | Army Medkit, Bandage, Battle_Military_Helmet, Bread, CannedFood, Default_Military_Armor, Echo, Energy Drink, F1 Grenade, GunAK74_ST, GunAPB_HG, GunPM_HG, GunSVDM_SP, GunSVU_SP, GunThreeLine_SP, Heavy2_Military_Armor, Heavy_Military_Helmet, Light_Military_He… |
| 4 | `MonolithVeteranItemGenerator` | 12-gauge ammo (alt.), 12-gauge ammo, 12-gauge ammo (alt.), .45 ACP ammo (alt.), .45 ACP ammo, .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.45×39 ammo, 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 5.56×45 ammo, 5.56×45 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R … |
| 3 | `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | Bandage, Battle_Military_Helmet, Beer, Default_Military_Armor, Echo, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunD12_SG, GunM860_SG, GunPM_HG, Heavy2_Military_Armor, Heavy_Military_Helmet, Light_Military_Helmet, Medkit, NPC_Cloak_Heavy_Military_Armor, NPC_Heavy… |
| 3 | `GeneralNPC_Militaries_Recon_ItemGenerator` | Anti-Rad, Bandage, Battle_Military_Helmet, Bread, CannedFood, Default_Military_Armor, Echo, Energy Drink, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunBucket_PP, GunFora230_PP, GunPM_HG, GunZubr_PP, Heavy2_Military_Armor, Heavy_Military_Helmet, Light_Military_He… |
| 3 | `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | Anti-Rad, Bandage, Bread, Echo, Exoskeleton_Neutral_Armor, RGD-5 Grenade, GunAK74_ST, GunAPB_HG, GunArev_ST, GunFora_ST, GunG37_ST, GunGrim_ST, GunGvintar_ST, GunKora_HG, GunLavina_ST, GunM10_HG, GunM16_ST, GunPM_HG, GunUDP_HG, Jemmy_Neutral_Armor, Light_Ne… |
| 2 | `FreedomVeteranItemGenerator` | 12-gauge ammo (alt.), 12-gauge ammo, 12-gauge ammo (alt.), .45 ACP ammo (alt.), .45 ACP ammo, .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.45×39 ammo, 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 5.56×45 ammo, 5.56×45 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R … |
| 2 | `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | Anti-Rad, Bandage, Battle_Military_Helmet, Bread, Default_Military_Armor, Echo, RGD-5 Grenade, GunAK74_ST, GunAPB_HG, GunDnipro_ST, GunGrim_ST, GunGvintar_ST, GunLavina_ST, GunPM_HG, Heavy2_Military_Armor, Heavy_Military_Helmet, Light_Military_Helmet, Medki… |
| 2 | `GeneralNPC_Varta_Recon_ItemGenerator` | Anti-Rad, Bandage, BattleExoskeleton_Varta_Armor, Battle_Varta_Armor, Bread, CannedFood, Echo, Energy Drink, RGD-5 Grenade, GunAKU_PP, GunAPB_HG, GunBucket_PP, GunFora230_PP, GunIntegral_PP, GunPM_HG, GunUDP_HG, GunViper_PP, GunZubr_PP, Heavy_Varta_Helmet, … |
| 2 | `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | Bandage, Beer, Echo, Exoskeleton_Neutral_Armor, RGD-5 Grenade, GunAPB_HG, GunKora_HG, GunM10_HG, GunM860_SG, GunObrez_SG, GunPM_HG, GunSPSA_SG, GunTOZ_SG, GunUDP_HG, Jemmy_Neutral_Armor, Light_Neutral_Helmet, Medkit, NPC_Cloak_Heavy_Neutral_Armor, NPC_Sel_N… |
| 2 | `NeutralExperiencedItemGenerator` | 12-gauge ammo (alt.), 12-gauge ammo, 12-gauge ammo (alt.), .45 ACP ammo (alt.), .45 ACP ammo, .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.45×39 ammo, 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 5.56×45 ammo, 5.56×45 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R … |
| 1 | `ARG01_QSBIG_Body1` | 5.45×39 ammo (alt.), Anti-Rad, Bandage, Bread, Echo, RGD-5 Grenade, GunAK74_ST, GunAKU_PP, GunG37_ST, GunPM_HG, GunViper_PP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_NPC_Gen2, SkinJ… |
| 1 | `BanditChernozem_ItemGenerator` | Bandage, Beer, Echo, RGD-5 Grenade, GunM10_HG, GunM860_SG, GunObrez_SG, GunPM_HG, GunSPSA_SG, GunTOZ_SG, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_NPC_Gen2, Sausage, SkinJacket_Bandi… |
| 1 | `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | Bandage, Exoskeleton_Neutral_Armor, GunAPB_HG, GunKora_HG, GunM10_HG, GunM860_SG, GunObrez_SG, GunPM_HG, GunSPSA_SG, GunTOZ_SG, GunUDP_HG, Jemmy_Neutral_Armor, Medkit, NPC_Cloak_Heavy_Neutral_Armor, NPC_Sel_Neutral_Armor, Nasos_Neutral_Armor, Newbee_Neutral… |
| 1 | `DutyNewbieItemGenerator` | 12-gauge ammo (alt.), 12-gauge ammo, 12-gauge ammo (alt.), .45 ACP ammo (alt.), .45 ACP ammo, .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.45×39 ammo, 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 5.56×45 ammo, 5.56×45 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R … |
| 1 | `FreedomExperiencedItemGenerator` | 12-gauge ammo (alt.), 12-gauge ammo, 12-gauge ammo (alt.), .45 ACP ammo (alt.), .45 ACP ammo, .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.45×39 ammo, 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 5.56×45 ammo, 5.56×45 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R … |
| 1 | `GeneralNPC_Bandit_Heavy_ItemGenerator` | Anti-Rad, Army Medkit, Bandage, CannedFood, Echo, RGD-5 Grenade, GunAK74_ST, GunAKU_PP, GunG37_ST, GunPM_HG, GunThreeLine_SP, GunViper_PP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_N… |
| 1 | `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | Anti-Rad, Bandage, Bread, Echo, RGD-5 Grenade, GunAK74_ST, GunAKU_PP, GunG37_ST, GunPM_HG, GunViper_PP, Jacket_Bandit_Armor, Light_Bandit_Helmet, Medkit, Middle_Bandit_Armor, NPC_SkinCloak_Bandit_Armor, NVG_NPC_Gen1, NVG_NPC_Gen2, SkinJacket_Bandit_Armor, V… |
| 1 | `GeneralNPC_MALACHITE_Scientist_Recon_ItemGenerator` | Anti-Rad, Bandage, Bread, CannedFood, Echo, Energy Drink, GunUDP_HG, Medkit, NPC_Sci_Armor |

## Highest propagated backbone generators

These are the generators where editing one pool can affect the largest number of repeatable production loot paths. This is the more useful table when deciding future global controls.

| Propagated links | Direct links | Generator | Context mix | Items |
|---:|---:|---|---|---|
| 1281 | 128 | `GamePass_Stash_ItemGenerator_Cheap` | item container world ×1279, destructible world ×2 | 12-gauge ammo, 9×18 ammo, 9×19 ammo, Bandage, Bread, CannedFood, Energy Drink, RGD-5 Grenade, Medkit, Milk, Coupons (Common), Coupons (Epic), Coupons (Rare), Sausage, Vodka, Water |
| 1214 | 0 | `DestructibleStash_Drink` | destructible world ×1214 | Beer, Energy Drink, Vodka, Water |
| 1214 | 1214 | `DestructibleStash_Food_Drink` | destructible world ×1214 | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 1214 | 0 | `DestructibleStash_Food` | destructible world ×1214 | Bread, CannedFood, Milk, Sausage |
| 994 | 0 | `DestructibleStash_Food_Experienced` | destructible world ×994 | Bread, CannedFood, Milk, Sausage |
| 994 | 0 | `DestructibleStash_Drink_Experienced` | destructible world ×994 | Beer, Energy Drink, Vodka, Water |
| 994 | 994 | `DestructibleStash_Food_Drink_Experienced` | destructible world ×994 | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 994 | 994 | `DestructibleStash_Food_Drink_Veteran` | destructible world ×994 | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 994 | 0 | `DestructibleStash_Drink_Veteran` | destructible world ×994 | Beer, Energy Drink, Vodka, Water |
| 994 | 0 | `DestructibleStash_Food_Veteran` | destructible world ×994 | Bread, CannedFood, Milk, Sausage |
| 994 | 994 | `DestructibleStash_Food_Drink_Master` | destructible world ×994 | Beer, Bread, CannedFood, Energy Drink, Milk, Sausage, Vodka, Water |
| 994 | 0 | `DestructibleStash_Food_Master` | destructible world ×994 | Bread, CannedFood, Milk, Sausage |
| 994 | 0 | `DestructibleStash_Drink_Master` | destructible world ×994 | Beer, Energy Drink, Vodka, Water |
| 984 | 984 | `DestructibleStash_AmmoSNG` | destructible world ×984 | 12-gauge ammo, 5.45×39 ammo, 9×18 ammo, 9×19 ammo |
| 870 | 870 | `DestructibleStash_Med` | destructible world ×870 | Bandage, Medkit |
| 555 | 1 | `DestructibleStash_Medkit_Master` | destructible world ×554, item container world ×1 | Army Medkit, Scientific Medkit, Medkit |
| 554 | 554 | `DestructibleStash_Med_Experienced` | destructible world ×554 | Army Medkit, Scientific Medkit, Medkit |
| 554 | 0 | `DestructibleStash_Medkit_Experienced` | destructible world ×554 | Army Medkit, Scientific Medkit, Medkit |
| 554 | 0 | `DestructibleStash_Medkit_Veteran` | destructible world ×554 | Army Medkit, Scientific Medkit, Medkit |
| 554 | 554 | `DestructibleStash_Med_Veteran` | destructible world ×554 | Army Medkit, Scientific Medkit, Medkit |
| 554 | 554 | `DestructibleStash_Med_Master` | destructible world ×554 | Army Medkit, Scientific Medkit, Medkit |
| 480 | 99 | `GamePass_Stash_ItemGenerator_Common_Var1` | item container world ×480 | 12-gauge ammo, .45 ACP ammo, 5.45×39 ammo, 5.56×45 ammo, 7.62×54R ammo, 7.62×54R ammo (alt.), 9×19 ammo, 9×39 ammo, Bandage, Energy Drink, Medkit, Coupons (Common), Coupons (Epic), Coupons (Rare), Vodka |
| 348 | 82 | `GamePass_Stash_ItemGenerator_Rare` | item container world ×347, human npc standard ×1 | 12-gauge ammo (alt.), 12-gauge ammo (alt.), .45 ACP ammo (alt.), 5.45×39 ammo (alt.), 5.56×45 ammo (alt.), 7.62×54R ammo (alt.), 7.62×54R ammo, 9×18 ammo (alt.), 9×19 ammo (alt.), 9×39 ammo (alt.), Anti-Rad,… |
| 253 | 253 | `DestructibleStash_AmmoNATO` | destructible world ×253 | 12-gauge ammo, .45 ACP ammo, 5.56×45 ammo, 9×19 ammo |
| 214 | 214 | `Stash_Cheap_Newbie` | item container world ×213, destructible world ×1 | 12-gauge ammo, 9×18 ammo, 9×19 ammo, Bandage, Bread, CannedFood, Energy Drink, RGD-5 Grenade, Medkit, Milk, Coupons (Common), Coupons (Epic), Coupons (Rare), Sausage, Vodka, Water |

## Improving Stashes Loot mod — cross-check

The supplied mod contains overrides for **18 vanilla ItemGenerator SIDs** from our catalog. They are exactly concentrated on the core stash tiers/backbone rather than random location-specific one-off generators:

- `Stash_Cheap_Newbie`
- `Stash_Cheap_Experienced`
- `Stash_Cheap_Veteran`
- `Stash_Cheap_Master`
- `Stash_Medium_Newbie`
- `Stash_Medium_Experienced`
- `Stash_Medium_Veteran`
- `Stash_Medium_Master`
- `Stash_Expensive_Newbie`
- `Stash_Expensive_Experienced`
- `Stash_Expensive_Veteran`
- `Stash_Expensive_Master`
- `GamePass_Stash_ItemGenerator_Cheap`
- `GamePass_Stash_ItemGenerator_Common_Var1`
- `GamePass_Stash_ItemGenerator_Rare`
- `CNPPGamePass_Stash_ItemGenerator_Cheap`
- `GamePass_Stash_ItemGenerator_CheapNoFresh`
- `CNPPGamePass_Stash_ItemGenerator_Rare`

That independently supports prioritizing the core stash pools first. The mod is reference evidence only; its balance/content choices are **not** treated as vanilla truth.

## Practical order for future generator controls

1. **Core world stash pools** — `Stash_Cheap_*`, `Stash_Medium_*`, `Stash_Expensive_*`, `GamePass_Stash_ItemGenerator_*`.
2. **Destructible crate pools** — food/drink, medical, SNG/NATO ammo, grenade pools.
3. **Standard-human shared sub-pools** — consumables first; weapon/armor pools only after we decide what should be player-facing.
4. **Mutant species loot** — already structurally isolated and easy to reason about.
5. **Zombie loadouts** — after generic zombie generator normalization is complete.
6. **CorpseStash overrides** — last; these are explicit physical dead-body overrides and are far less uniform than normal NPC corpse loot.
