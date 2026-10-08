# Remaining standalone UI controls (2026-10-08)

This is a **source-generated snapshot** of catalog controls still rendered by their
default XAML type (slider, range, choice slider or toggle) rather than a specialized
semantic visual family. It is a redesign queue, **not** an indication of broken CFG mappings.

- Catalog: 238 parameters
- Default-control entries remaining: **21**
- Completed prior pass: MutantCombatPressure, StaminaCore, WaterStaminaPressure, RadiationTreatment (eight sliders)
- Completed prior loot pass: StashRewardProfile, BreakableBoxYields, WorldLootLayout (ten controls)
- Completed current pass: NpcLootLoadout (one choice slider, two numeric sliders and one two-bound range)
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

## NPC Loot & Equipment pass (2026-10-08)

- **NpcLootLoadout** contains one discrete, three-option smart-loot selection (Medicine + Ammo / Water / None), two independent native-range multipliers for normal medical chance and attached weapon ammo, and an absolute weapon-condition range with distinct MIN and MAX editors.
- The selected difficulty's smart-loot vanilla option is shown separately from the selected option, and N/S/V/M markers are retained on option tiles.
- Condition's top 0–100% band is read-only; the two lower native tracks edit 1%-step MIN and MAX, retaining existing automatic bound ordering. The 25–50% reference preserves the authored 35–45% exception as long as the range is unchanged; changed ranges apply to all 121 audited direct weapon rows.
- Range and choice retain their own preset serialization and original CFG writers, keyboard controls, right-click per-setting resets, and R family reset.
- Native WPF choice and range editors are explicitly hidden under an aggregate anchor to avoid duplicate controls.

## Loot pass completed (2026-10-08)

- StashRewardProfile: three independent existing source values in one panel, including two separate probability previews.
- BreakableBoxYields: two independent source multipliers and read-only comparison lanes.
- WorldLootLayout: four source-exact booleans and one conditional density slider. Toggle-anchored board is compatible with the WPF toggle visibility guard; density stays disabled with weapon shuffle OFF.
- Underlying CFG patching and presets remain untouched. Source ranges, increments, reference values and reset behavior are preserved.

## UX follow-up checks

- Do not turn independent settings into linked values unless CFG-backed constraints require it.
- Keep Base Preset / Vanilla reference legible even on compact visual controls.
- For `choiceSlider`, `range` and `toggle`, preserve their native input semantics.
- Track hidden/advanced and pre-existing specialized families separately; these are **not** standalone-UI leftovers.
- Check WPF resizing, 16:10 window, wheel changes, keyboard focus, tooltip, and reset behavior.
- Complete the remaining panels before global UI polish.
