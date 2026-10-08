# Remaining standalone UI controls (2026-10-08)

This is a **source-generated snapshot** of catalog controls still rendered by their
default XAML type (slider, range, choice slider or toggle) rather than a specialized
semantic visual family. It is a redesign queue, **not** an indication of broken CFG mappings.

- Catalog: 238 parameters
- Default-control entries remaining: **30**
- Just converted (commit `6f4dd55`): MutantCombatPressure, StaminaCore, WaterStaminaPressure, RadiationTreatment (eight sliders)
- Scope: visual reorganization only; preserve exact source values, min/max, steps,
  vanilla markers, reset/right-click semantics, dependencies and preset serialization
- Do not merge sliders across different CFG mappings into synthetic gameplay formulas
- Groups should be solved individually before any global typography/spacing polish

## Remaining by tab and section

### combat / Explosives (1)

- `combat.explosionDamage` — Explosion Damage (`slider`)

### health / Low Health (1)

- `health.lowHealthRegen` — Low-Health Recovery Rate (`slider`)

### health / Healing Amount (1)

- `health.foodHealing` — Food Restores Health (`toggle`)

### stamina / Traversal (1)

- `stamina.vaultMaxHeight` — Maximum Vault Height (`slider`)

### stamina / Encumbrance (1)

- `stamina.maxCarryWeight` — Maximum Carry Weight (`slider`)

### loot / NPC Loot & Equipment (4)

- `loot.npcCorpseBonus` — Extra NPC Corpse Loot (`choiceSlider`)
- `loot.npcMedicalChance` — NPC Medical Drop (`slider`)
- `loot.npcWeaponAmmoSupply` — NPC Dropped Ammo Amount (`slider`)
- `loot.droppedWeaponCondition` — NPC Weapon Condition (`range`)

### loot / Stashes (0 — visual family complete)

- `loot.cheapStashGrenadeChance` — Basic Stash RGD-5 Grenade Chance (`slider`)
- `loot.cheapStashGrenadeQuantity` — Basic Stash RGD-5 Grenade Amount (`slider`)
- `loot.expensiveStashMoneyChance` — Rare Stash Coupon Rewards (`slider`)

### loot / Breakable Containers (0 — visual family complete)

- `loot.destructibleAmmoQuantity` — Ammo from Breakable Boxes (`slider`)
- `loot.destructibleGrenadeQuantity` — Grenades from Breakable Boxes (`slider`)

### economy / Starting Coupons (1)

- `economy.startingMoney` — Starting Coupons (`slider`)

### economy / Trader Stock (2)

- `economy.traderAmmoStock` — Trader Ammo Stock Amount (`slider`)
- `economy.medicStock` — Medic Stock Amount (`slider`)

### economy / Services (3)

- `economy.fastTravelCost` — Fast Travel Cost (`slider`)
- `economy.upgradeCost` — Upgrade Cost (`slider`)
- `economy.repairCostBase` — Base Repair Price (`slider`)

### economy / Coupon Rewards (2)

- `economy.mainQuestReward` — Main Quest Coupon Rewards (`slider`)
- `economy.sideQuestReward` — Side Quest Coupon Rewards (`slider`)

### equipment / Item Weight (2)

- `equipment.ammoWeight` — Ammo Weight (`slider`)
- `equipment.consumableWeight` — Consumable Weight (`slider`)

### environment / Anomalies (6)

- `environment.anomalyDamage` — Global Anomaly Damage (`slider`)
- `environment.flycatcherMovementSpeed` — Flycatcher Movement Speed (`slider`)
- `environment.flycatcherTurnSpeed` — Flycatcher Turn Speed (`slider`)
- `environment.razorBleeding` — Razor Bleeding (`slider`)
- `environment.diamondBleeding` — Diamond Bleeding (`slider`)
- `environment.psyAnomalyIntensity` — Psy Anomaly Intensity (`slider`)

### loot / World Loot (5)

- `loot.worldWeaponShuffle` — World Weapon Shuffle (`toggle`)
- `loot.worldWeaponDensity` — World Weapon Density (`slider`)
- `loot.worldAttachmentShuffle` — World Attachment Shuffle (`toggle`)
- `loot.worldDetectors` — Remove World Detectors (`toggle`)
- `loot.blueprintShuffle` — Blueprint Shuffle (`toggle`)

## UX follow-up checks

- Do not turn independent settings into linked values unless CFG-backed constraints require it.
- Keep Base Preset / Vanilla reference legible even on compact visual controls.
- For `choiceSlider`, `range` and `toggle`, preserve their native input semantics.
- Track hidden/advanced and pre-existing specialized families separately; these are **not** standalone-UI leftovers.
- Check WPF resizing, 16:10 window, wheel changes, keyboard focus, tooltip, and reset behavior.
- Complete the remaining panels before global UI polish.
