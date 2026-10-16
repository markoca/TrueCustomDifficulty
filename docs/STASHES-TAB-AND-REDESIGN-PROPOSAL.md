# Stashes tab — verified source meanings and UI redesign proposal

Date: 2026-10-16. Navigation relocation and five-section graphical redesign are IMPLEMENTED on main.

## Implemented: which controls move

Place Stashes between Loot and Economy. All 26 existing stash parameters use their original loot.* IDs; 24 are player-visible and two are hidden aggregate backings. Their presets, output CFG destinations and calculations are untouched. The five source families render in the order Rewards, Content, Medical Composition, Medkit Quantity and Extra Gear. World Loot, destructible crates, NPC loot and mutant part drops remain in Loot. Both catalog locks are authoritative for existing native visuals.

## What every existing stash parameter does

| Group | Stable source IDs | Actual source effect |
| --- | --- | --- |
| Rewards: Basic/Standard/Rare Ammo | loot.cheapStashAmmoQuantity, loot.mediumStashAmmoQuantity, loot.expensiveStashAmmoQuantity | Three independent stack multipliers change the number of rounds in eligible stash entries. They do not change ammo caliber pools, entry selection chances or world stash occurrence. |
| Rewards: Basic RGD-5 Chance | loot.cheapStashGrenadeChance | Edits the entry-level RGD-5 roll chance in Basic stash rewards. It does not change successful drop quantity. |
| Rewards: Basic RGD-5 Amount | loot.cheapStashGrenadeQuantity | Changes RGD-5 quantity when that entry is generated; independent of chance. |
| Rewards: Rare Coupons | loot.expensiveStashMoneyChance | Alters eligible Rare stash coupon selection chance; does not rewrite coupon amount, item tier or authored quest reward. |
| Content | loot.stashMixAmmo, loot.stashMixMedical, loot.stashMixFood, loot.stashMixDrinks, loot.stashMixGrenades, loot.stashMixCoupons | Six native 0..3x category selection biases (0.25x steps). They multiply existing entry Weight or Chance; Chance is capped at 1. Neither overall stash inventory share nor item count is guaranteed by these multipliers. |
| Medical composition | loot.stashMedicalBandageBias, loot.stashMedicalMedkitBias, loot.stashMedicalArmyMedkitBias, loot.stashMedicalAntiRadBias | Four native 0..3x (0.25x step) item-specific selection biases. Each combines with general MEDICAL: effective = general x specific. An already-100%-chance entry cannot go above 100%. |
| Medkit stack size | loot.stashMedkitMinCount, loot.stashMedkitMaxCount | Native 0 AUTO/1..6 lower floor and upper cap for Medkit stack size. AUTO retains individual original source bound, not zero items; conflicting MAX CAP wins. Does not force any selection. |
| Army Medkit stack size | loot.stashArmyMedkitMinCount, loot.stashArmyMedkitMaxCount | The same independent native floor/cap logic for eligible ArmyMedkit entries, not every Medkit or a guaranteed Army Medkit reward. |
| Extra Weapons | loot.stashGearCheapWeapon, loot.stashGearMediumWeapon, loot.stashGearExpensiveWeapon | Each Basic/Standard/Rare mode controls additional rank-gated regular weapon selection: 0 OFF, 1 RARE, 2 MODERATE, 3 FREQUENT. Source chance modes are not verified whole-stash odds. |
| Extra Armor | loot.stashGearCheapArmor, loot.stashGearMediumArmor, loot.stashGearExpensiveArmor | Identical independent modes for extra rank-gated regular armor. OFF stops only TCD added gear, never existing authored reward. |

Only the audited production-only TCD generator clones/redirects are changed: some scripted, quest and standalone/unredirected stashes remain untouched. Weight is a relative selection input, while Chance is an entry field, not measured per-stash occurrence. In-game testing remains required.

## IMPLEMENTED — compact graphical design

1. STASH REWARDS: six bar-graph rows in two banks: three BASIC/STANDARD/RARE ammo quantity tracks on one multiplier axis, followed by three separate plotted RGD-5 CHANCE, RGD-5 AMOUNT and RARE COUPONS tracks on source-native axes. No knobs or ordinary slider tiles.
2. STASH CONTENT: six aligned horizontal tracks sharing a single scale. Labels left; current value numbers consistently right like Stamina Usage. One shared muted reference 1x marker, no repeated redundant scale text or pie-chart pseudo-probability.
3. MEDICAL COMPOSITION: four short tracks in the same grammar as Content; a minimal group hint explains that item bias multiplies general MEDICAL. Separate item rows should never falsely suggest stack count.
4. MEDKIT QUANTITY: two rows by two columns of selectable dot plots. MIN FLOOR 0 AUTO/1..6; MAX CAP 0 ITEMS (stored -1), AUTO (stored 0), and 1..6. Explicit zero also sets native Chance/Weight to zero for that Medkit or ArmyMedkit entry; existing presets with stored 0 remain AUTO.
5. EXTRA STASH GEAR: a 3x2 matrix Basic/Standard/Rare by Weapons/Armor. Each cell gets an OFF/RARE/MODERATE/FREQUENT native discrete selector; do not stretch cells across full width or duplicate their explanations.

All five charts are centered, bounded, source-editable, and have right-aligned live values and changed-number-only highlights. The new explicit -1 MAX stop is the only catalog range expansion. Both authoritative visual catalogs were updated as part of the implementation. No separate redundant editors appear.

## Local acceptance after pull/build

Verify tab order Loot > Stashes > Economy; five Stashes groups and 26 IDs; no stash group left under Loot; group Reset Tab limited to Stashes; Save/Load round-trips existing loot.* preset IDs; CFG writer emits same source paths; no clipped WPF tabs/cards at narrow width. No WPF/Wine or game runtime claim is made from connector/static checks.
