# World Weapon Shuffle Implementation

Version: 0.4.5

## Contract

The production writer only consumes `Data/world-weapon-shuffle-manifest.csv` and `Data/world-weapon-shuffle-pool.csv`. The manifest is an explicit allowlist. Discovery is never performed at build time.

A slot is runtime-compatible only when the pool contains at least one **different** regular weapon with the same `tier` and `family`. With the current audited data this is 8 slots.

`Weapon Shuffle Amount` samples exactly `round(compatible_slots × amount)` slots without replacement. Identical Base Preset and non-amount parameter values produce one deterministic ordering. Changing only Weapon Shuffle Amount reveals a longer or shorter prefix of that same ordering, so 25% ⊆ 50% ⊆ 100%.

For each selected slot the writer replaces the entire direct `PossibleItems[n]` node. It copies the original neutral spawn scalars when present but does not copy `Upgrades` or `Attaches`, preventing weapon-specific recipe references from leaking to another weapon.

## Intentionally absent

- cross-class replacement
- unique/special weapon replacement
- quest/reward/preset/NPC weapon replacement
- DLC1 world-weapon replacement (no DLC1 SAFE_WORLD surface has been proven)
- armor shuffle
