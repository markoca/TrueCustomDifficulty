# Generic Zombie Generator Baseline — corrected v0.2.17

This baseline supersedes the v0.1.83 parent-backed weapon-gap interpretation. The resolved generator catalog is the source of truth.

## Control boundary

| Control concept | Decision | Exact boundary |
|---|---|---|
| **Zombie Grenade Frequency** | **IMPLEMENTED / RUNTIME TEST REQUIRED** | v0.2.17 scales the existing child-local grenade `Chance` on 91/93 roots; two roots have no grenade branch and stay untouched. |
| **Zombie Weapon Condition** | **NO UI / AUDIT ONLY** | Fully mapped over 791 local primary rows + 71 shared-pistol rows, but intentionally not exposed as a difficulty control. |
| **Zombie Weapon Ammo** | **NO UI / AUDIT ONLY** | Fully mapped, but attached-ammo tuning remains documentation-only. |
| Zombie Detector Chance | **NO UI** | Source rows exist but there is no useful observable detector loot effect from zombies. |

## Corrected source classes

| Class | Roots | Meaning |
|---|---:|---|
| Primary + pistol | 75 | local `WeaponPrimary` plus shared pistol link |
| Primary only | 6 | local `WeaponPrimary`; no pistol link |
| Pistol only | 10 | no local primary; weapon comes only through shared pistol pool |
| No weapon source | 2 | do not invent a weapon branch |

## Complete 93-root baseline

| Generator SID | Consumer(s) | Local primary rows | Pistol pool | Link chance | Grenade rows | Weapon source class |
|---|---|---:|---|---:|---:|---|
| `CNPPZombie_Bandit_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Bandit_CloseCombat` | 7 | `—` | — | 1 | PRIMARY_ONLY |
| `CNPPZombie_Bandit_Heavy_ItemGenerator` | `GeneralCNPPZombie_Bandit_Heavy` | 7 | `—` | — | 1 | PRIMARY_ONLY |
| `CNPPZombie_Bandit_Recon_ItemGenerator` | `GeneralCNPPZombie_Bandit_Recon` | 26 | `—` | — | 1 | PRIMARY_ONLY |
| `CNPPZombie_Bandit_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Bandit_Stormtrooper` | 6 | `—` | — | 1 | PRIMARY_ONLY |
| `CNPPZombie_Corpus_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Corpus_CloseCombat` | 0 | `GeneralNPC_Corpus_WeaponPistol` | 0.3 | 1 | PISTOL_ONLY |
| `CNPPZombie_Corpus_Heavy_ItemGenerator` | `GeneralCNPPZombie_Corpus_Heavy` | 0 | `GeneralNPC_Corpus_WeaponPistol` | 0.15 | 1 | PISTOL_ONLY |
| `CNPPZombie_Corpus_Recon_ItemGenerator` | `GeneralCNPPZombie_Corpus_Recon` | 0 | `GeneralNPC_Corpus_WeaponPistol` | 0.25 | 1 | PISTOL_ONLY |
| `CNPPZombie_Corpus_Sniper_ItemGenerator` | `GeneralCNPPZombie_Corpus_Sniper` | 0 | `GeneralNPC_Corpus_WeaponPistol` | 0.5 | 1 | PISTOL_ONLY |
| `CNPPZombie_Corpus_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Corpus_Stormtrooper` | 0 | `GeneralNPC_Corpus_WeaponPistol` | 0.4 | 1 | PISTOL_ONLY |
| `CNPPZombie_Duty_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Duty_CloseCombat` | 5 | `GeneralNPC_Duty_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Duty_Heavy_ItemGenerator` | `GeneralCNPPZombie_Duty_Heavy` | 6 | `GeneralNPC_Duty_WeaponPistol` | 0.1 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Duty_Recon_ItemGenerator` | `GeneralCNPPZombie_Duty_Recon` | 2 | `GeneralNPC_Duty_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Duty_Sniper_ItemGenerator` | `GeneralCNPPZombie_Duty_Sniper` | 8 | `GeneralNPC_Duty_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Duty_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Duty_Stormtrooper` | 10 | `GeneralNPC_Duty_WeaponPistol` | 0.45 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Freedom_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Freedom_CloseCombat` | 8 | `GeneralNPC_Freedom_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Freedom_Recon_ItemGenerator` | `GeneralCNPPZombie_Freedom_Recon` | 8 | `GeneralNPC_Freedom_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Freedom_Sniper_ItemGenerator` | `GeneralCNPPZombie_Freedom_Sniper` | 8 | `GeneralNPC_Freedom_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Freedom_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Freedom_Stormtrooper` | 9 | `GeneralNPC_Freedom_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Mercenaries_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Mercenary_CloseCombat` | 6 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.35 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Mercenaries_Recon_ItemGenerator` | `GeneralCNPPZombie_Mercenary_Recon` | 9 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Mercenaries_Sniper_ItemGenerator` | `GeneralCNPPZombie_Mercenary_Sniper` | 6 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Mercenaries_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Mercenary_Stormtrooper` | 5 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Militaries_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Military_CloseCombat` | 2 | `GeneralNPC_Militaries_WeaponPistol` | 0.35 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Militaries_Heavy_ItemGenerator` | `GeneralCNPPZombie_Military_Heavy` | 12 | `GeneralNPC_Militaries_WeaponPistol` | 0.1 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Militaries_Recon_ItemGenerator` | `GeneralCNPPZombie_Military_Recon` | 10 | `GeneralNPC_Militaries_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Militaries_Sniper_ItemGenerator` | `GeneralCNPPZombie_Military_Sniper` | 4 | `GeneralNPC_Militaries_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Militaries_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Military_Stormtrooper` | 14 | `GeneralNPC_Militaries_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Monolith_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Monolith_CloseCombat` | 10 | `GeneralNPC_Monolith_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Monolith_Recon_ItemGenerator` | `GeneralCNPPZombie_Monolith_Recon` | 5 | `GeneralNPC_Monolith_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Monolith_Sniper_ItemGenerator` | `GeneralCNPPZombie_Monolith_Sniper` | 14 | `GeneralNPC_Monolith_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Monolith_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Monolith_Stormtrooper` | 6 | `GeneralNPC_Monolith_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Neutral_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Neutral_CloseCombat` | 10 | `GeneralNPC_Neutral_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Neutral_Recon_ItemGenerator` | `GeneralCNPPZombie_Neutral_Recon` | 26 | `—` | — | 1 | PRIMARY_ONLY |
| `CNPPZombie_Neutral_Sniper_ItemGenerator` | `GeneralCNPPZombie_Neutral_Sniper` | 17 | `GeneralNPC_Neutral_WeaponPistol` | 1.0 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Neutral_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Neutral_Stormtrooper` | 16 | `—` | — | 1 | PRIMARY_ONLY |
| `CNPPZombie_Noon_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Noon_CloseCombat` | 0 | `GeneralNPC_Noon_WeaponPistol` | 0.3 | 1 | PISTOL_ONLY |
| `CNPPZombie_Noon_Recon_ItemGenerator` | `GeneralCNPPZombie_Noon_Recon` | 0 | `GeneralNPC_Noon_WeaponPistol` | 0.25 | 1 | PISTOL_ONLY |
| `CNPPZombie_Noon_Sniper_ItemGenerator` | `GeneralCNPPZombie_Noon_Sniper` | 0 | `GeneralNPC_Noon_WeaponPistol` | 0.5 | 1 | PISTOL_ONLY |
| `CNPPZombie_Noon_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Noon_Stormtrooper` | 0 | `GeneralNPC_Noon_WeaponPistol` | 0.45 | 1 | PISTOL_ONLY |
| `CNPPZombie_Scientists_Recon_ItemGenerator` | `GeneralCNPPZombie_Scientist_Recon` | 0 | `—` | — | 0 | NO_WEAPON_SOURCE |
| `CNPPZombie_Scientists_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Scientist_Stormtrooper` | 0 | `—` | — | 1 | NO_WEAPON_SOURCE |
| `CNPPZombie_Spark_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Spark_CloseCombat` | 3 | `GeneralNPC_Spark_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Spark_Recon_ItemGenerator` | `GeneralCNPPZombie_Spark_Recon` | 0 | `GeneralNPC_Spark_WeaponPistol` | 0.25 | 1 | PISTOL_ONLY |
| `CNPPZombie_Spark_Sniper_ItemGenerator` | `GeneralCNPPZombie_Spark_Sniper` | 8 | `GeneralNPC_Spark_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Spark_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Spark_Stormtrooper` | 4 | `GeneralNPC_Spark_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Varta_CloseCombat_ItemGenerator` | `GeneralCNPPZombie_Varta_CloseCombat | VartaZombieVeres` | 7 | `GeneralNPC_Varta_WeaponPistol` | 0.35 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Varta_Heavy_ItemGenerator` | `GeneralCNPPZombie_Varta_Heavy` | 5 | `GeneralNPC_Varta_WeaponPistol` | 0.1 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Varta_Recon_ItemGenerator` | `GeneralCNPPZombie_Varta_Recon` | 12 | `GeneralNPC_Varta_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Varta_Sniper_ItemGenerator` | `GeneralCNPPZombie_Varta_Sniper` | 4 | `GeneralNPC_Varta_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `CNPPZombie_Varta_Stormtrooper_ItemGenerator` | `GeneralCNPPZombie_Varta_Stormtrooper` | 18 | `GeneralNPC_Varta_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Bandit_CloseCombat_ItemGenerator` | `GeneralZombie_Bandit_CloseCombat` | 8 | `GeneralNPC_Bandit_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Bandit_Heavy_ItemGenerator` | `GeneralZombie_Bandit_Heavy` | 10 | `GeneralNPC_Bandit_WeaponPistol` | 1.0 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Bandit_Recon_ItemGenerator` | `GeneralZombie_Bandit_Recon` | 28 | `GeneralNPC_Bandit_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Bandit_Stormtrooper_ItemGenerator` | `GeneralZombie_Bandit_Stormtrooper` | 8 | `GeneralNPC_Bandit_WeaponPistol` | 1.0 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Corpus_CloseCombat_ItemGenerator` | `GeneralZombie_Corpus_CloseCombat` | 5 | `GeneralNPC_Corpus_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Corpus_Heavy_ItemGenerator` | `GeneralZombie_Corpus_Heavy` | 3 | `GeneralNPC_Corpus_WeaponPistol` | 0.15 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Corpus_Recon_ItemGenerator` | `GeneralZombie_Corpus_Recon` | 6 | `GeneralNPC_Corpus_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Corpus_Sniper_ItemGenerator` | `GeneralZombie_Corpus_Sniper` | 3 | `GeneralNPC_Corpus_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Corpus_Stormtrooper_ItemGenerator` | `GeneralZombie_Corpus_Stormtrooper` | 5 | `GeneralNPC_Corpus_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Duty_CloseCombat_ItemGenerator` | `GeneralZombie_Duty_CloseCombat` | 8 | `GeneralNPC_Duty_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Duty_Heavy_ItemGenerator` | `GeneralZombie_Duty_Heavy` | 12 | `GeneralNPC_Duty_WeaponPistol` | 0.1 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Duty_Recon_ItemGenerator` | `GeneralZombie_Duty_Recon` | 6 | `GeneralNPC_Duty_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Duty_Sniper_ItemGenerator` | `GeneralZombie_Duty_Sniper` | 13 | `GeneralNPC_Duty_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Duty_Stormtrooper_ItemGenerator` | `GeneralZombie_Duty_Stormtrooper` | 16 | `GeneralNPC_Duty_WeaponPistol` | 0.45 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Freedom_CloseCombat_ItemGenerator` | `FreedomZombieCastro | GeneralZombie_Freedom_CloseCombat` | 11 | `GeneralNPC_Freedom_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Freedom_Recon_ItemGenerator` | `GeneralZombie_Freedom_Recon` | 12 | `GeneralNPC_Freedom_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Freedom_Sniper_ItemGenerator` | `FreedomPiton | GeneralZombie_Freedom_Sniper` | 11 | `GeneralNPC_Freedom_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Freedom_Stormtrooper_ItemGenerator` | `GeneralZombie_Freedom_Stormtrooper` | 14 | `GeneralNPC_Freedom_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Mercenaries_CloseCombat_ItemGenerator` | `GeneralZombie_Mercenary_CloseCombat` | 9 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.35 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Mercenaries_Recon_ItemGenerator` | `GeneralZombie_Mercenary_Recon` | 13 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Mercenaries_Sniper_ItemGenerator` | `GeneralZombie_Mercenary_Sniper` | 10 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Mercenaries_Stormtrooper_ItemGenerator` | `GeneralZombie_Mercenary_Stormtrooper` | 11 | `GeneralNPC_Mercenaries_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Militaries_Sniper_ItemGenerator` | `GeneralZombie_Military_Sniper` | 7 | `GeneralNPC_Militaries_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Monolith_CloseCombat_ItemGenerator` | `CNPP_Archanomaly_PhantomZombieNPC_Monolith_CloseCombat | GeneralZombie_Monolith_CloseCombat` | 13 | `GeneralNPC_Monolith_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Monolith_Recon_ItemGenerator` | `CNPP_Archanomaly_PhantomZombieNPC_Scientist_Recon | GeneralZombie_Monolith_Recon` | 13 | `GeneralNPC_Monolith_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Monolith_Sniper_ItemGenerator` | `CNPP_Archanomaly_PhantomZombieNPC_Military_Sniper | GeneralZombie_Monolith_Sniper` | 20 | `GeneralNPC_Monolith_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Monolith_Stormtrooper_ItemGenerator` | `CNPP_Archanomaly_PhantomZombieNPC_Base | GeneralZombie_Monolith_Stormtrooper` | 13 | `GeneralNPC_Monolith_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Neutral_Sniper_ItemGenerator` | `GeneralZombie_Neutral_Sniper` | 25 | `GeneralNPC_Neutral_WeaponPistol` | 1.0 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Noon_CloseCombat_ItemGenerator` | `GeneralZombie_Noon_CloseCombat` | 4 | `GeneralNPC_Noon_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Noon_Recon_ItemGenerator` | `GeneralZombie_Noon_Recon` | 6 | `GeneralNPC_Noon_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Noon_Sniper_ItemGenerator` | `GeneralZombie_Noon_Sniper` | 8 | `GeneralNPC_Noon_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Noon_Stormtrooper_ItemGenerator` | `GeneralZombie_Noon_Stormtrooper` | 3 | `GeneralNPC_Noon_WeaponPistol` | 0.45 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Scientist_Recon_ItemGenerator` | `GeneralZombie_Scientist_Recon` | 1 | `GeneralNPC_Scientists_WeaponPistol` | 0.25 | 0 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Scientists_Stormtrooper_ItemGenerator` | `GeneralZombie_Scientist_Stormtrooper` | 2 | `GeneralNPC_Scientists_WeaponPistol` | 0.45 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Spark_CloseCombat_ItemGenerator` | `GeneralZombie_Spark_CloseCombat` | 6 | `GeneralNPC_Spark_WeaponPistol` | 0.3 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Spark_Recon_ItemGenerator` | `GeneralZombie_Spark_Recon` | 6 | `GeneralNPC_Spark_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Spark_Sniper_ItemGenerator` | `GeneralZombie_Spark_Sniper` | 16 | `GeneralNPC_Spark_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Spark_Stormtrooper_ItemGenerator` | `GeneralZombie_Spark_Stormtrooper` | 13 | `GeneralNPC_Spark_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Varta_CloseCombat_ItemGenerator` | `GeneralZombie_Varta_CloseCombat` | 12 | `GeneralNPC_Varta_WeaponPistol` | 0.35 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Varta_Heavy_ItemGenerator` | `GeneralZombie_Varta_Heavy` | 9 | `GeneralNPC_Varta_WeaponPistol` | 0.1 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Varta_Recon_ItemGenerator` | `GeneralZombie_Varta_Recon` | 18 | `GeneralNPC_Varta_WeaponPistol` | 0.25 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Varta_Sniper_ItemGenerator` | `GeneralZombie_Varta_Sniper` | 9 | `GeneralNPC_Varta_WeaponPistol` | 0.5 | 1 | PRIMARY_PLUS_PISTOL |
| `GeneralZombie_Varta_Stormtrooper_ItemGenerator` | `GeneralZombie_Varta_Stormtrooper` | 22 | `GeneralNPC_Varta_WeaponPistol` | 0.4 | 1 | PRIMARY_PLUS_PISTOL |

See `GENERIC-ZOMBIE-WEAPON-SOURCE-AUDIT.md` for the correction rationale and machine-readable files.
