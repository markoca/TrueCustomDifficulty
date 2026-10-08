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


## Production UI extension — 2026-10-09

The earlier eight-family-only UI restriction is superseded by the user's explicit approval to add **all audited mutant MaxHP families**, even where no separately verifiable Ability Cooldown editor exists. Added standalone health multiplier mapping for Blinddog (63 HP), Boar (300 HP), Flesh (280 HP), Poltergeist (400 HP), Snork (150 HP) and Tushkan (19 HP). The existing eight mapped families are retained. The combined Mutant Health board is now a two-row, seven-column editor. These remain **direct prototype MaxHP scalars**, not difficulty-specific health settings. No additional cooldown mechanism is introduced. The previous eight-only notes above remain as historical release decisions, not as current UI scope. Compile, runtime and generated CFG validation still require local verification.


## Alphabetical HP and special-ability cooldown verification — 2026-10-09

The production Mutant Health editor now lists all fourteen supported families alphabetically and caps each HP multiplier at **2×** instead of 3× (0.25× minimum, 0.05× tick). The catalog and ViewModel both enforce the maximum.

Only eight family-specific ability cooldown mappings are currently verified and implemented through `MutantCombatDifficulty.AgentCooldownMultipliers` in selected difficulty profiles: Bloodsucker, Chimera, Deer, Pseudogiant, Pseudodog, Burer, Bayun, Controller (with authored related variants). Blind Dog, Boar, Flesh, Poltergeist, Snork and Tushkan have audited MaxHP but **no verified equivalent special ability cooldown entry**. This does not prove those mutants have no cooldown of any kind; it prohibits representing unverified special ability controls as genuine CFG mappings. Global ordinary attack frequency is a separate control. No new cooldown CFG mappings were fabricated.


## DLC1 Amok — pending original CFG verification (2026-10-09)

- Amok is a separate Cost of Hope boss. Do not combine with regular Bloodsucker HP scaling or automatically include in the fourteen Mutant Health channels.
- EXTERNAL MODDER REPORT (not verified from CFG): Grounded Combat reports a separate 24,000 HP three-phase boss encounter and deliberately leaves Amok unmodified pending encounter-specific verification. Source: https://www.nexusmods.com/stalker2heartofchornobyl/mods/2396
- The existing parameter catalog, CFG writer baseline and mutant health/entry audits have no confirmed Amok source mapping. A prior DLC1 file inventory references DLCGameData/DLC1/NPCPrototypes.cfg.bin, but the file name alone does not establish an Amok prototype ID or HP field.
- Next evidence: extracted DLC1 runtime NPCPrototypes.cfg and related files, resolved prototype/inheritance chain, actual MaxHP, phase/weak-point gating and quest completion conditions.
- No user-facing slider, hardcoded health default, or CFG writer mapping until the source target and encounter behavior are verified. If supported, use an independent boss-specific control, not the shared ordinary mutant health family.
- STATUS: CFG unconfirmed; runtime testing required. No gameplay override implemented.
