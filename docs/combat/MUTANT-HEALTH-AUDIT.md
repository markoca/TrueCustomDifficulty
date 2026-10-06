# Mutant Health Audit

Version: 0.9.26.44 — audit-only

## Result

Mutant health is directly configurable, but the audited vanilla CFG does **not** expose mutant health as a difficulty-specific scalar.

Regular mutant prototypes define `MaxHP` directly in:

`GameData/ObjPrototypes.cfg`

Examples:

| Mutant | Prototype | Vanilla MaxHP |
|---|---|---:|
| BlindDog | `Blinddog` | 63 |
| Bloodsucker | `Bloodsucker` | 500 |
| Boar | `Boar` | 300 |
| Burer | `Burer` | 400 |
| Bayun | `Bayun` | 400 |
| Chimera | `Chimera` | 1400 |
| Controller | `Controller` | 500 |
| Deer | `Deer` | 600 |
| Flesh | `Flesh` | 280 |
| Poltergeist | `Poltergeist` | 400 |
| Pseudodog | `PseudoDogBase` | 300 |
| Pseudogiant | `Pseudogiant` | 2500 |
| Snork | `Snork` | 150 |
| Tushkan | `Tushkan` | 19 |

## Difficulty architecture

`GameData/DifficultyPrototypes.cfg` contains mutant combat difficulty controls such as:

- `Mutant_BaseDamage`
- `Mutant_AttackCooldown`

and some difficulty profiles contain mutant-specific attack cooldown tables.

However, the audited file contains **no**:

- `Mutant_HP`
- `MutantHP`
- `MaxHP`

difficulty field.

The mutant prototype blocks in `ObjPrototypes.cfg` also contain no `EGameDifficulty` branches around `MaxHP`.

Therefore:

- **Yes:** we can safely change mutant health by editing each mutant's `MaxHP`.
- **No, not 1:1 from the audited vanilla architecture:** we cannot claim a native per-difficulty mutant-health multiplier comparable to `Mutant_BaseDamage`.
- A global/per-mutant health multiplier would affect that mutant regardless of the currently selected in-game difficulty unless a separate runtime mechanism is found.

## Recommended control design

If added now, the evidence-supported design is one health multiplier per mutant, for example:

- Blind Dog Health
- Bloodsucker Health
- Boar Health
- Burer Health
- Bayun Health
- Chimera Health
- Controller Health
- Deer Health
- Flesh Health
- Poltergeist Health
- Pseudodog Health
- Pseudogiant Health
- Snork Health
- Tushkan Health

Each would scale that prototype's authored `MaxHP`.

Do **not** expose these as Novice / Stalker / Veteran / Master health values unless another game-data path is found that actually selects mutant HP by difficulty.

Status: audit-only. No mutant-health UI controls are enabled in v0.9.26.44.


## Production decision — v0.9.26.44

Only mutant families that already have a verified **Ability Cooldown** control are promoted to normal Combat UI in this release.

Eight paired health controls are enabled:

- Bloodsucker / Mist Bloodsucker
- Chimera
- Deer
- Pseudogiant
- Pseudodog
- Burer
- Bayun
- Controller

The remaining audited MaxHP values stay documented but are not exposed as standalone health sliders yet, because there is no corresponding verified ability-cooldown control to pair them with.
