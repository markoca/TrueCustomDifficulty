# Destructible Food / Drink Production Audit

## Decision

**AUDITED / UI-DEFERRED.** The defined food/drink destructible family is a real production loot surface, but it does not expose one rank-consistent scalar that can honestly be presented as a global Food/Drink Frequency or Quantity control.

Do not add a normal UI control from this family with the current evidence.

`DestructibleStash_Food_Drink_Newbie` remains a separate hard writer boundary documented in `DESTRUCTIBLE-FOOD-DRINK-NEWBIE-BOUNDARY.md`.

## Production wiring

The production wiring catalog contains 4,089 food/drink rank rows across 1,370 physical actors:

| Rank | Generator | Production wiring rows | Definition |
|---|---|---:|---|
| Newbie | `DestructibleStash_Food_Drink` | 1,210 | defined |
| Newbie | `DestructibleStash_Food_Drink_Newbie` | 20 | **missing / unresolved** |
| Experienced | `DestructibleStash_Food_Drink_Experienced` | 953 | defined |
| Veteran | `DestructibleStash_Food_Drink_Veteran` | 953 | defined |
| Master | `DestructibleStash_Food_Drink_Master` | 953 | defined |

The rows are rank wiring, not 4,089 separate physical crates. A physical actor can carry multiple rank-specific generator assignments.

## Wrapper semantics

| Wrapper | Food child | Drink child | Wrapper field |
|---|---|---|---|
| `DestructibleStash_Food_Drink` | `DestructibleStash_Food` | `DestructibleStash_Drink` | `Weight=1` on each child |
| `..._Experienced` | `...Food_Experienced` | `...Drink_Experienced` | `Chance=0.6` on each child |
| `..._Veteran` | `...Food_Veteran` | `...Drink_Veteran` | `Chance=0.7` on each child |
| `..._Master` | `...Food_Master` | `...Drink_Master` | `Chance=0.8` on each child |

The base/Newbie-defined wrapper therefore uses **weighted branch selection**, while Experienced+ use **independent branch chance**. Scaling these as if they were the same field would not be a 1:1 vanilla semantic mapping.

The Experienced → Veteran → Master progression also shows an intentional vanilla increase in the probability of invoking both food and drink child pools: 0.6 → 0.7 → 0.8.

## Reachable item set

The family reaches exactly eight consumable prototypes:

- Food: `Sausage`, `Bread`, `CannedFood`, `Milk`
- Drink: `Water`, `Beer`, `Vodka`, `Energetic` (Energy Drink)

Food and drink use mirrored numeric patterns at every defined rank.

## Base / defined-Newbie child pools

Both child pools use weighted selection and fixed one-item counts.

| Food item | Weight | Count | Drink item | Weight | Count |
|---|---:|---:|---|---:|---:|
| Sausage | 0.3 | 1 | Water | 0.3 | 1 |
| Bread | 0.3 | 1 | Beer | 0.3 | 1 |
| Canned Food | 0.2 | 1 | Vodka | 0.2 | 1 |
| Milk | 0.1 | 1 | Energy Drink | 0.1 | 1 |

These `Weight` values describe relative composition, not an occurrence probability. Treating them as a frequency scalar would change which item is selected.

All counts are fixed at `1..1`, so a quantity control below vanilla cannot reduce quantity without introducing zero and thereby changing the semantic into availability/chance.

## Experienced child pools

| Food item | Chance | Count | Drink item | Chance | Count |
|---|---:|---:|---|---:|---:|
| Sausage | 0.6 | 1..2 | Water | 0.6 | 1..2 |
| Bread | 0.6 | 1..2 | Beer | 0.6 | 1..2 |
| Canned Food | 0.4 | 1..2 | Vodka | 0.4 | 1..2 |
| Milk | 0.3 | 1 | Energy Drink | 0.3 | 1 |

## Veteran child pools

| Food item | Chance | Count | Drink item | Chance | Count |
|---|---:|---:|---|---:|---:|
| Sausage | 0.5 | 1..2 | Water | 0.5 | 1..2 |
| Bread | 0.5 | 1..2 | Beer | 0.5 | 1..2 |
| Canned Food | 0.5 | 1..2 | Vodka | 0.5 | 1..2 |
| Milk | 0.4 | 1 | Energy Drink | 0.4 | 1 |

## Master child pools

| Food item | Chance | Count | Drink item | Chance | Count |
|---|---:|---:|---|---:|---:|
| Sausage | 0.4 | 1..2 | Water | 0.4 | 1..2 |
| Bread | 0.4 | 1..2 | Beer | 0.4 | 1..2 |
| Canned Food | 0.6 | 1..2 | Vodka | 0.6 | 1..2 |
| Milk | 0.5 | 1..2 | Energy Drink | 0.5 | 1..2 |

The higher-rank tables are not merely quantity changes. Their per-item chances alter composition while their counts independently control stack size.

## Why no UI slider is approved

### Food/Drink Quantity

Rejected/deferred because:

1. base child entries are fixed `1..1`;
2. higher-rank entries mix `1..1` and `1..2`;
3. scaling below one would require inventing zero-count behavior or changing `Chance` instead;
4. changing `Chance` would no longer be a quantity control.

### Food/Drink Frequency

Rejected/deferred because:

1. base wrapper uses `Weight`, not `Chance`;
2. Experienced/Veteran/Master wrappers use `Chance`;
3. the 20 production `DestructibleStash_Food_Drink_Newbie` consumers have no supplied definition;
4. therefore no one field/operation covers the full vanilla production surface with one consistent meaning.

### Separate Food Frequency / Drink Frequency

Not approved. At base rank the two child links are a 1:1 weighted composition choice. Scaling one side changes the food-vs-drink ratio. At higher ranks they become independent chances. A single UI concept would therefore change meaning across ranks.

## Writer boundary

Current policy:

- do not mutate the unresolved `DestructibleStash_Food_Drink_Newbie` SID;
- do not synthesize a replacement definition for it;
- do not expose the defined family as a normal slider merely because its local fields are editable;
- preserve the vanilla food/drink composition and rank semantics until a deliberately designed control has one consistent player-facing meaning and a production-safe writer path.

## Revisit conditions

Revisit only if at least one of these becomes available:

1. the actual vanilla definition of `DestructibleStash_Food_Drink_Newbie`;
2. runtime evidence proving how the base weighted wrapper maps to occurrence frequency relative to the higher-rank chance wrappers; or
3. a deliberately narrower control whose label and writer target exactly one proven semantic rather than pretending to scale the whole family uniformly.
