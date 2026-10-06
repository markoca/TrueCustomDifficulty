# Mutant Loot — Exact Local Entry Catalog

This catalog records the locally authored rows for every generator classified as `MUTANT LOOT` in the generator gameplay map. Raw technical SIDs are preserved exactly; player-facing naming uses Bayun for `CatLootGenerator`.

| Scope | Generator | Category | Item | Nested generator | Chance | Weight | Count | Durability | Ammo | Source line |
| --- | --- | --- | --- | --- | ---: | ---: | --- | --- | --- | ---: |
| base | `MutantElectrocollarGenerator` | `EItemGenerationCategory::Head` | `Electrocollar` | `—` | — | 1 | 1–1 | —–— | —–— | 21931 |
| base | `DefaultMutantLootGenerator` | `EItemGenerationCategory::MutantLoot` | `Sausage` | `—` | — | 2 | 1–1 | —–— | —–— | 23118 |
| base | `BlinddogLootGenerator` | `EItemGenerationCategory::MutantLoot` | `BlinddogLoot` | `—` | 0.15 | — | 1–1 | —–— | —–— | 23134 |
| base | `BloodsuckerLootGenerator` | `EItemGenerationCategory::MutantLoot` | `BloodsuckerLoot` | `—` | 0.5 | — | 1–1 | —–— | —–— | 23150 |
| base | `BoarLootGenerator` | `EItemGenerationCategory::MutantLoot` | `BoarLoot` | `—` | 0.2 | — | 1–1 | —–— | —–— | 23166 |
| base | `BurerLootGenerator` | `EItemGenerationCategory::MutantLoot` | `BurerLoot` | `—` | 1 | — | 1–1 | —–— | —–— | 23182 |
| base | `CatLootGenerator` | `EItemGenerationCategory::MutantLoot` | `CatLoot` | `—` | 1 | — | 1–1 | —–— | —–— | 23198 |
| base | `ChimeraLootGenerator` | `EItemGenerationCategory::MutantLoot` | `ChimeraLoot` | `—` | 1 | — | 1–1 | —–— | —–— | 23214 |
| base | `ControllerLootGenerator` | `EItemGenerationCategory::MutantLoot` | `ControllerLoot` | `—` | 1 | — | 1–1 | —–— | —–— | 23230 |
| base | `DeerLootGenerator` | `EItemGenerationCategory::MutantLoot` | `DeerLoot` | `—` | 1 | — | 1–1 | —–— | —–— | 23246 |
| base | `FleshLootGenerator` | `EItemGenerationCategory::MutantLoot` | `FleshLoot` | `—` | 0.2 | — | 1–1 | —–— | —–— | 23262 |
| base | `PoltergeistLootGenerator` | `EItemGenerationCategory::MutantLoot` | `PoltergeistLoot` | `—` | 0.65 | — | 1–1 | —–— | —–— | 23278 |
| base | `PseudodogLootGenerator` | `EItemGenerationCategory::MutantLoot` | `PseudodogLoot` | `—` | 1.0 | — | 1–1 | —–— | —–— | 23294 |
| base | `PseudogiantLootGenerator` | `EItemGenerationCategory::MutantLoot` | `PseudogiantLoot` | `—` | 1 | — | 1–1 | —–— | —–— | 23310 |
| base | `TushkanLootGenerator` | `EItemGenerationCategory::MutantLoot` | `TushkanLoot` | `—` | 0.1 | — | 1–1 | —–— | —–— | 23326 |
| base | `SnorkLootGenerator` | `EItemGenerationCategory::MutantLoot` | `SnorkLoot` | `—` | 0.2 | — | 1–1 | —–— | —–— | 23342 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::SubItemGenerator` | `—` | `AllAmmosNPCGenerator` | 1.0 | — | —–— | —–— | —–— | 136710 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::WeaponPrimary` | `GunAK74_ST` | `—` | — | 50.0 | —–— | 0.8–1.0 | 75–125 | 136719 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::WeaponSecondary` | `empty` | `—` | — | 50.0 | —–— | 0.4–0.9 | 25–75 | 136732 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::WeaponPistol` | `GunPM_HG` | `—` | — | 50.0 | —–— | 0.8–0.9 | 25–75 | 136745 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::BodyArmor` | `Jacket_Bandit_Armor` | `—` | — | 100.0 | —–— | 1.0–1.0 | —–— | 136758 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::Head` | `empty` | `—` | — | 50.0 | —–— | 1.0–1.0 | —–— | 136769 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::SubItemGenerator` | `—` | `ConsumableNewbieGenerator` | 1.0 | — | —–— | —–— | —–— | 136784 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::SubItemGenerator` | `—` | `DetectorNewbieGenerator` | 0.05 | — | —–— | —–— | —–— | 136788 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::SubItemGenerator` | `—` | `ArtifactNewbieGenerator` | 0.01 | — | —–— | —–— | —–— | 136792 |
| base | `RC_Faust_Mutant_ItemGenerator` | `EItemGenerationCategory::SubItemGenerator` | `—` | `AttachNewbieGenerator` | 0.01 | — | —–— | —–— | —–— | 136796 |
| dlc1 | `JadedChimeraLootGenerator` | `EItemGenerationCategory::MutantLoot` | `DLC01_ArtifactHeartofChimera` | `—` | 1 | — | 1–1 | —–— | —–— | 31 |
| dlc1 | `DLC01_Object68LootGenerator` | `EItemGenerationCategory::MutantLoot` | `DLC01_Object68Loot` | `—` | 1 | — | 1–1 | —–— | —–— | 47 |

The standard species part generators are single-row `MutantLoot` roots. Faust is the deliberate multi-row exception; its nested generators and equipment rows are why it is excluded from generic mutant-part tuning.
