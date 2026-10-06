# World Gear Armor Player-Facing Progression Audit

## Conclusion

Trader, stash and location occurrence data is useful as **availability evidence**, but the supplied GameData does not expose a universal armor tier contract equivalent to `Trader_T1..T4_Guns_ItemGenerator`.

Therefore these signals must **not** be converted mechanically into T1-T4 armor tiers.

## Production-safe armor tier source

Only the explicit player gear progression anchors are tier-locked:

| Tier | Player progression anchor | Armor |
|---|---|---|
| T1 | `NewbieGearPC_Generator` | `Newbee_Neutral_Armor` |
| T2 | `ExperiencedGearPC_Generator` | `Zorya_Neutral_Armor` |
| T3 | `VeteranGearPC_Generator` | `SEVA_Neutral_Armor` |
| T4 | `MasterGearPC_Generator` | `Exoskeleton_Neutral_Armor` |

## Rejected tier signals

- NPC/faction `PlayerRank` is NPC loadout scaling, not world-loot progression.
- Item `Cost` is metadata, not a tier selector.
- `BaseDurability` is metadata, not a tier selector.
- Trader presence proves that an item can enter player-facing circulation at that trader, but trader names do not provide a common numeric tier axis.
- Stash/location presence is valuable contextual evidence, but named locations likewise do not define a universal T1-T4 contract.

## Evidence artifact

`world-gear-armor-player-facing-evidence.csv` records trader, stash and player-generator occurrences for every currently reviewed direct `TemplateArmor` item. It is audit evidence only.

## Implementation gate

The generic armor shuffle must remain disabled until either:

1. a real universal armor progression mapping is found in GameData, or
2. the project explicitly adopts a curated/manual armor-tier policy as a design decision rather than claiming it is vanilla-derived.

The weapon shuffle is not blocked by this armor limitation because weapon tiers have explicit `Trader_T1..T4` evidence.
