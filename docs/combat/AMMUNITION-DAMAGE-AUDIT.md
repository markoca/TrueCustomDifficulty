# Ammunition Damage — CFG audit (2.1)

Date: 2026-11-24. Status: CFG-confirmed, implementation deferred, runtime test required.

Source: STALKER2-2.1 cfg dump(3).zip, nested STALKER2-GameData-CFG.zip (487 entries) and STALKER2-DLC1-CFG.zip (118 entries). Scanned the base ItemPrototypes.cfg top-level roots and resolved refkey inheritance. DLC1 ItemPrototypes.cfg has no AmmoType, TemplateAmmo or DamageMod declarations. DamageMod assignments across the supplied dump occur only in base ItemPrototypes.cfg (21 assignments, including non-ammo occurrences if present). No changes to shipped controls, presets or writers.

## Exact targets

GameLite/GameData/ItemPrototypes.cfg, top-level concrete SID, DamageMod. TemplateAmmo defines DamageMod = 1.0. I = inherited, E = explicitly authored. Do not confuse effective value with presence of a field in the concrete root.

| SID | Parent | Effective DamageMod | Source |
|---|---|---:|---|
| A918D | TemplateAmmo | 1.0 | I |
| A918A | A918D | 1.0 | I |
| A919D | TemplateAmmo | 1.0 | I |
| A919A | A919D | 0.98 | E |
| A045D | TemplateAmmo | 1.0 | I |
| A045A | A045D | 0.98 | E |
| A045E | A045D | 1.9 | E |
| A545D | TemplateAmmo | 1.0 | I |
| A545A | A545D | 0.98 | E |
| A545E | A545D | 1.9 | E |
| A556D | TemplateAmmo | 1.0 | I |
| A556A | A556D | 0.98 | E |
| A556E | A556D | 1.9 | E |
| A556S | A556D | 1.0 | I |
| A939D | TemplateAmmo | 1.0 | I |
| A939A | A939D | 0.98 | E |
| A939E | A939D | 1.9 | E |
| A939S | A939D | 1.0 | I |
| A762D | TemplateAmmo | 1.0 | I |
| A762A | A762D | 0.98 | E |
| A762E | A762D | 1.9 | E |
| A762NATOD | TemplateAmmo | 1.0 | I |
| A762NATOA | A762NATOD | 0.98 | E |
| A762NATOS | A762NATOD | 1.0 | I |
| A762SniperD | TemplateAmmo | 1.0 | I |
| A762SniperA | A762SniperD | 0.98 | E |
| A762SniperS | A762SniperD | 1.0 | I |
| A012D | TemplateAmmo | 0.084 | E |
| A012A | A012D | 0.72 | E |
| A012E | A012D | 1.42 | E |
| AGA | TemplateAmmo | 1.0 | I |
| AVOG | TemplateAmmo | 1.0 | I |
| AHEDP | TemplateAmmo | 1.0 | I |
| APG7V | TemplateAmmo | 1.0 | I |

## Related fields and boundaries

TemplateAmmo: ArmorPiercingMod=0.0, CoverPiercingMod=0.0, ArmorDamageMod=1.0, BleedingMod=1.0, WeaponExhaustionMod=1.0, FractionCount=1, FlatnessMod=1.0, RecoilMod=1.0, DispersionMod=1.0, AimDispersionMod=1.0, OffsetAimDispersionMod=1.0, ImpulseToObjectsMod=1.0. Keep all unchanged in a damage-only implementation. AP variants have ArmorPiercingMod=0.5; expanding variants=-0.7 in the resolved source. A012D has FractionCount=12 and ArmorDamageMod=0.084; A012A/E each have FractionCount=1 and ArmorDamageMod=1.0. Per-projectile interpretation and total damage require runtime validation; never equate DamageMod alone to HP or total shell damage.

A762 family exists but prior regular-caliber audit found no vanilla weapon setup consuming it. Keep it in the research inventory, not automatically in the visible controls. AGA is BulletHeavy; AVOG/AHEDP/APG7V are Explosion; separate special-ammo candidates.

WeaponData/CharacterWeaponSettingsPrototypes.cfg defines BaseDamage independently. DifficultyPrototypes.cfg defines EnvironmentDifficulty.Weapon_BaseDamage and NPCCombatDifficulty.NPC_Weapon_BaseDamage; PC vanilla values respectively 1.3/1.0/0.75/0.75 and 0.17/0.85/1.02/1.35 (Novice/Stalker/Veteran/Master). CFG confirms separate fields, not the engine calculation or player/NPC applicability of DamageMod.

## Deferred implementation contract

- Proposed relative adjustment: effective vanilla DamageMod times user factor; 1x must preserve vanilla, including non-1.0 values. No UI or ranges selected yet.
- Preserve inheritance: never edit TemplateAmmo globally as a shortcut for a selective change. Verify concrete overrides for inherited fields, including children inheriting from default ammo. Ensure unselected descendants retain their effective vanilla values; verify patch insertion/precedence before implementation.
- Do not modify weapon BaseDamage, difficulty multipliers, penetration, pellet count, or loot distribution as a side effect.
- Test no-op output, explicit and inherited targets, independent parent/child adjustments, all four PC difficulties, unchanged siblings, and in-game player/NPC applicability, armor and shotgun behavior before promotion to production.
- No runtime tests performed; no catalog, writer, UI or factory preset changes authorized in this audit.

Related: [Combat mapping](../COMBAT-MAPPING.md), [regular ammo caliber audit](../generators/FULL-REGULAR-AMMO-CALIBER-AUDIT.md).
