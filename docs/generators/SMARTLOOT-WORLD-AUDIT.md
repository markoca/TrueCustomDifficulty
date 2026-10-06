# World SmartLoot — Deep Audit

This audit deep-maps the world-facing SmartLoot prototypes for medical, food/drink, ammo and grenades. It does **not** add runtime controls or change the CFG writer.

## Evidence boundary

- SmartLoot comes from `StashPrototypes.cfg` and is a separate runtime layer from `ItemGeneratorPrototypes.cfg`.
- Exact prototype/rank/group/item coordinates are taken from `smartloot-inventory.csv`, `smartloot-rank-blocks.csv`, `smartloot-groups.csv` and `smartloot-items.csv`.
- Physical consumers are resolved from `smartloot-consumer-edges.csv` back to SpawnActor item containers/destructibles and enriched with authored map context.
- `MinSpawnChance` / `MaxSpawnChance`, `ItemSetCount`, `MainWeaponAmmoCount`, item `Chance`, item `Weight`, and item count ranges are kept as **different source semantics**. This audit does not collapse them into a synthetic generic probability.
- `NPC_Ammo_Smart`, `NPC_Medicine_Smart` and `NPC_Water_Smart` remain outside this audit because they belong to the separate Difficulty `CorpseSmartLoot` bonus path.
- `StashMedicine_Corpse` and `StashVodka_Corpse` have no resolved external consumer in the supplied graph and are not treated as world SmartLoot.

## Result

| Metric | Count |
| --- | ---: |
| World SmartLoot prototypes deep-audited | 13 |
| Exact group rows | 422 |
| Exact item rows | 991 |
| Direct physical consumer edges | 2170 |
| Production-world consumer edges | 2143 |
| Production item-container edges | 2111 |
| Production destructible edges | 32 |
| Production edges also carrying ItemGenerator | 2105 |
| Production SmartLoot-only edges | 38 |
| Quest/scripted consumer edges | 15 |
| DLC quest-map consumer edges | 6 |
| Test/debug consumer edges | 6 |

The central result is that **there is no single SmartLoot “abundance” field**. The 13 world prototypes contain 422 group rows and 991 item rows, and the source uses several independent mechanics at once. A control named only “World Medical Loot”, “World Food Loot”, “World Ammo Loot”, or “World Grenade Loot” would therefore be broader than any one exact source field.

## Domain coverage

| Domain | Prototypes | All consumers | Production consumers | Group rows | Item rows | Production dual-layer |
| --- | --- | --- | --- | --- | --- | --- |
| MEDICAL | 2 | 1066 | 1054 | 13 | 15 | 1043 |
| FOOD / DRINK | 2 | 16 | 11 | 15 | 29 | 8 |
| GRENADES | 2 | 17 | 16 | 2 | 3 | 5 |
| AMMO | 7 | 1071 | 1062 | 392 | 944 | 1049 |

## Prototype-level structure

| Domain | Prototype | All consumers | Production surface | Dual / production | Exact structure | Min..Max endpoint pairs | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- |
| MEDICAL | StashMedicine_Cheap | 22 | 18 (10 containers + 8 destructibles) | 8/18 | 2 groups / 2 items / 2 unique | 0.f..1.f | REVIEW — two independent consumable sets; exact fields are mapped, but no one-row medical-abundance scalar |
| FOOD / DRINK | StashFood_Cheap | 11 | 7 (5 containers + 2 destructibles) | 4/7 | 3 groups / 3 items / 3 unique | 0.f..1.f | REVIEW — three independent consumable sets; no one-row food-abundance scalar |
| GRENADES | StashGrenade_Cheap | 12 | 11 (2 containers + 9 destructibles) | 2/11 | 1 groups / 2 items / 2 unique | 0.f..1.f | REVIEW — one group but two weighted grenade items plus ItemSetCount; distinct from Smart grenade prototype |
| AMMO | Stash_AmmoAll_Cheap | 20 | 19 (12 containers + 7 destructibles) | 11/19 | 96 groups / 284 items / 27 unique | 0.f..1.f | REVIEW — 96 rank/slot/caliber groups and 284 item rows; no global ammo scalar |
| GRENADES | StashGrenade_Smart | 5 | 5 (3 containers + 2 destructibles) | 3/5 | 1 groups / 1 items / 1 unique | 0.f..0.02f | NARROW STRUCTURAL CANDIDATE — one group / one item; MaxSpawnChance is a single exact source field, but runtime endpoint semantics still require proof |
| MEDICAL | StashMedicine_Smart | 1044 | 1036 (1035 containers + 1 destructibles) | 1035/1036 | 11 groups / 13 items / 4 unique | 0.1f..0.7f \| 0.1f..0.8f | REVIEW — 11 rank/set groups with endpoint, ItemSetCount, Weight and count semantics; broad production use is mostly dual-layer |
| FOOD / DRINK | StashFood_Smart | 5 | 4 (4 containers + 0 destructibles) | 4/4 | 12 groups / 26 items / 8 unique | 0.f..0.01f \| 0.f..0.05f | REVIEW — 12 rank/set groups and mixed item Chance/Weight semantics; no one-row food-abundance scalar |
| AMMO | Stash_AmmoNATO_Smart | 12 | 9 (6 containers + 3 destructibles) | 6/9 | 67 groups / 151 items / 22 unique | 0.f..0.09f \| 0.f..0.01f | REVIEW — 67 rank/slot/caliber groups; includes one raw MinCount > MaxCount anomaly that must be preserved, not normalized |
| AMMO | Stash_AmmoSNG_Smart | 3 | 3 (3 containers + 0 destructibles) | 2/3 | 45 groups / 104 items / 15 unique | 0.f..0.09f \| 0.f..0.01f | REVIEW — 45 rank/slot/caliber groups with caliber-specific counts/weights |
| AMMO | Stash_AmmoSNG_Smart_MainLoot | 2 | 2 (2 containers + 0 destructibles) | 2/2 | 45 groups / 104 items / 15 unique | 0.f..0.f | SPECIAL / REVIEW — all 45 group endpoint pairs are 0/0 despite live production consumers; do not interpret endpoints as universal spawn chance |
| AMMO | Stash_AmmoNATO_Smart_MainLoot | 3 | 3 (3 containers + 0 destructibles) | 3/3 | 67 groups / 151 items / 22 unique | 0.f..0.f | SPECIAL / REVIEW — all 67 group endpoint pairs are 0/0 despite live production consumers; do not interpret endpoints as universal spawn chance |
| AMMO | Stash_Ammo_Smart_Cheap | 46 | 46 (46 containers + 0 destructibles) | 46/46 | 40 groups / 99 items / 16 unique | 0.1f..0.8f \| 0.f..0.01f | REVIEW — combines 36 groups at 0..0.01 with 4 groups at 0.1..0.8; rank/slot/caliber matrix must be preserved |
| AMMO | Stash_Ammo_Smart_CommonRare | 985 | 980 (980 containers + 0 destructibles) | 979/980 | 32 groups / 51 items / 18 unique | 0.1f..0.8f | REVIEW — 32 groups at 0.1..0.8 with rank/slot/caliber-specific counts and weights; high fan-out and almost entirely dual-layer |

## Medical

`StashMedicine_Cheap` and `StashMedicine_Smart` are **not** interchangeable representations of one chance. The Cheap prototype has two independent Newbie consumable sets (`Medkit`, `Bandage`), each with `MinSpawnChance=0` / `MaxSpawnChance=1`, `ItemSetCount=3`, an item `Weight=5`, and its own count range. The Smart prototype expands to 11 rank/set rows and 13 item rows across all four ranks, using endpoint pairs `0.1..0.7` and `0.1..0.8`, variable `ItemSetCount` values 2–5, item weights, and different count ranges.

`StashMedicine_Smart` alone has **1,036 production consumers**, of which **1,035 also carry ItemGenerator wiring**. A medical SmartLoot control would therefore change only one layer on almost every physical container it reaches. It cannot honestly be labeled as total medical loot for those containers.

## Food / drink

`StashFood_Cheap` contains three independent Newbie sets (`Sausage`, `Bread`, `Energetic`) at `0..1` with `ItemSetCount=3`. `StashFood_Smart` contains 12 rank/set rows and 26 item rows across eight item SIDs. The Smart prototype mixes item-level `Chance` and `Weight` rows and uses `ItemSetCount` values 1–5, so there is no single 1:1 food quantity or chance field.

The world-facing food prototypes also reach quest/test contexts in the raw consumer graph. Any future generic control must use the production consumer allowlist rather than blindly selecting every `StashFood_*` consumer.

## Grenades

The grenade domain is the smallest, but it still contains two different structures:

- `StashGrenade_Cheap`: one group, **two weighted items** (`GrenadeRGD5`, `GrenadeF1`), `ItemSetCount=5`, count range 1–3, endpoints `0..1`.
- `StashGrenade_Smart`: one group, **one item** (`GrenadeRGD5`), item `Chance=1`, fixed count 1, `ItemSetCount=1`, endpoints `0..0.02`.

`StashGrenade_Smart.MaxSpawnChance` is therefore the only world SmartLoot field in this audit that is a **narrow one-group / one-item structural candidate**. That does **not** make it a global grenade-loot slider: it affects only that prototype's five consumers, and the exact runtime interpretation of the Min/Max endpoint pair is not proven by the CFG graph alone.

## Ammo

Ammo is a matrix, not a scalar. Seven world ammo prototypes contain **392 group rows and 944 item rows**. Their groups are selected across player rank, primary/secondary/pistol weapon context, `PriorityCaliber`, `MainWeaponAmmoCount`, group endpoint pairs, item weights/chances and item count ranges.

Important source facts that prevent a generic chance interpretation:

- `Stash_AmmoAll_Cheap`: 96 groups, all `0..1`, 284 item rows.
- `Stash_AmmoNATO_Smart`: 67 groups using `0..0.01` or `0..0.09`.
- `Stash_AmmoSNG_Smart`: 45 groups using `0..0.01` or `0..0.09`.
- `Stash_AmmoSNG_Smart_MainLoot`: **all 45 groups are `0..0`**, yet the prototype has two live production consumers.
- `Stash_AmmoNATO_Smart_MainLoot`: **all 67 groups are `0..0`**, yet the prototype has three live production consumers.
- `Stash_Ammo_Smart_Cheap`: 36 groups use `0..0.01`, while four use `0.1..0.8`.
- `Stash_Ammo_Smart_CommonRare`: 32 groups use `0.1..0.8` and fan out to 980 production consumers.

The two `*_MainLoot` cases are decisive evidence that `MaxSpawnChance` cannot be promoted into a universal “ammo spawn chance” slider without understanding the engine's SmartLoot selection path.

### Preserved raw anomaly

`Stash_AmmoNATO_Smart`, Master / `SecondaryWeaponParams` set `[7]`, item `A762NATOD`, source line **4695**, is authored as `MinCount=25` / `MaxCount=15`. This is preserved exactly in the audit. It is not silently corrected because the runtime source is the authority and the intended engine handling is not proven here.

## Physical layer relationship

Of 2143 production-world SmartLoot edges, **2105** also have ItemGenerator wiring and only **38** are SmartLoot-only. This confirms the earlier world-container/destructible audits: SmartLoot is normally an **overlay**, not a complete description of what the container can generate.

Therefore:

- do not combine ItemGenerator and SmartLoot into one hidden multiplier;
- do not call a SmartLoot-only control “total stash loot”;
- preserve exact prototype/rank/group relationships;
- use production actor allowlists when a control intends world-only scope;
- keep Difficulty `CorpseSmartLoot` packages separate.

## Control-safety conclusion

No broad medical, food/drink, ammo, or grenade SmartLoot slider is approved as a strict one-field 1:1 mapping.

`StashGrenade_Smart.MaxSpawnChance` is the sole **narrow structural candidate** because it is one authored group feeding one fixed-count item. It remains deferred until runtime behavior proves what the endpoint pair means in practice. Everything else requires either multiple exact coordinated edits or a more narrowly named control that exposes the actual source mechanic instead of inventing a generic “loot %”.

## Machine-readable artifacts

- `smartloot-world-prototype-inventory.csv` — all 13 audited world prototypes with exact structure, fan-out, context and decision.
- `smartloot-world-group-rows.csv` — all 422 exact group rows.
- `smartloot-world-item-rows.csv` — all 991 exact item rows.
- `smartloot-world-consumer-edges.csv` — all 2,170 physical consumer edges enriched with context and dual-layer status.
- `smartloot-world-domain-summary.json` — compact counts and control-safety conclusions.
- `SMARTLOOT-WORLD-ENTRY-CATALOG.md` — complete human-readable group/item catalog.
- `SMARTLOOT-WORLD-WIRING-CATALOG.md` — complete physical consumer wiring catalog.
