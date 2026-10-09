# World Destructibles — Loot Generator Deep Audit

This audit separates physical destructible objects, ItemGenerator wiring, internal generator composition, and the small independent SmartLoot overlay. It does not add or enable any runtime control.

## Evidence boundary

- Patch coordinates come from `generator-local-source-*` raw/local tables.
- Physical consumers come from `SpawnActorPrototypes.cfg` and are classified by authored `LevelName` / map path.
- `WorldMap_WP` is the strict production-world surface. Quest logic levels and test/blockout maps are excluded from generic tuning.
- `Chance` and `Weight` are kept as distinct source mechanics; this audit does not assume their runtime probability formula.

## Physical topology

| Metric | Count |
| --- | ---: |
| All physical destructible actors in base + DLC1 SpawnActor CFGs | 79,602 |
| All loot-relevant destructible actors | 3,748 |
| Strict production `WorldMap_WP` destructible actors | 46,424 |
| Strict production loot-relevant destructibles | 3,685 |
| Production ItemGenerator-only destructibles | 3,653 |
| Production SmartLoot-only destructibles | 28 |
| Production dual ItemGenerator + SmartLoot destructibles | 4 |
| Production destructibles with ItemGenerator wiring | 3,657 |
| Production rank-specific ItemGenerator wiring rows | 8,024 |
| Distinct production generator SIDs on destructibles | 15 |
| Defined `DestructibleStash*` generator roots | 40 |
| Exact local rows in those roots | 91 |

The production layer is therefore mostly ItemGenerator-only: only 32 production destructibles carry SmartLoot at all, and only 4 carry both layers. This differs sharply from normal world item containers, where dual wiring is common.

## Production physical families

| Family | Generator SID | Production wiring rows | Defined? |
| --- | --- | ---: | --- |
| NATO ammo pool | `DestructibleStash_AmmoNATO` | 252 | yes |
| SNG ammo pool | `DestructibleStash_AmmoSNG` | 983 | yes |
| cross-system normal stash wrapper | `Stash_Cheap_Experienced` | 1 | yes |
| cross-system normal stash wrapper | `Stash_Cheap_Newbie` | 1 | yes |
| food+drink wrapper | `DestructibleStash_Food_Drink` | 1,210 | yes |
| food+drink wrapper | `DestructibleStash_Food_Drink_Experienced` | 953 | yes |
| food+drink wrapper | `DestructibleStash_Food_Drink_Master` | 953 | yes |
| food+drink wrapper | `DestructibleStash_Food_Drink_Newbie` | 20 | **NO** |
| food+drink wrapper | `DestructibleStash_Food_Drink_Veteran` | 953 | yes |
| grenade pool | `DestructibleStash_F1` | 101 | yes |
| grenade pool | `DestructibleStash_RGD` | 81 | yes |
| medical base pool | `DestructibleStash_Med` | 869 | yes |
| medical wrapper | `DestructibleStash_Med_Experienced` | 549 | yes |
| medical wrapper | `DestructibleStash_Med_Master` | 549 | yes |
| medical wrapper | `DestructibleStash_Med_Veteran` | 549 | yes |

Two production rows use normal world-stash wrappers (`Stash_Cheap_Newbie` / `Stash_Cheap_Experienced`) on one destructible actor. They are cross-system exceptions, not members of the destructible family.

## Generator role / blast-radius table

| generator_sid | family | role | production_wiring_refs | all_destructible_wiring_refs | incoming_composition_refs | impact_contexts | fields_present | direct_items | nested_generators | reachable_items | decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DestructibleStash_AmmoNATO | NATO ammo pool | production root; cross-context dependency | 252 | 253 | 4 | destructible_world, item_container_world | Weight \| MinCount \| MaxCount | A012D \| A045D \| A556D \| A919D |  | A012D \| A045D \| A556D \| A919D | REVIEW — production destructible use but edits also reach ItemContainer path |
| DestructibleStash_Ammo_NATO_Experienced | NATO ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoNATO | A012D \| A045D \| A556D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| DestructibleStash_Ammo_NATO_Master | NATO ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world, item_container_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoNATO | A012D \| A045D \| A556D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| DestructibleStash_Ammo_NATO_Newbie | NATO ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoNATO | A012D \| A045D \| A556D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| DestructibleStash_Ammo_NATO_Veteran | NATO ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoNATO | A012D \| A045D \| A556D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| DestructibleStash_AmmoSNG | SNG ammo pool | production root; cross-context dependency | 983 | 984 | 4 | destructible_world, item_container_world | Weight \| MinCount \| MaxCount | A012D \| A545D \| A918D \| A919D |  | A012D \| A545D \| A918D \| A919D | REVIEW — production destructible use but edits also reach ItemContainer path |
| DestructibleStash_Ammo_SNG_Experienced | SNG ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoSNG | A012D \| A545D \| A918D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| DestructibleStash_Ammo_SNG_Master | SNG ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world, item_container_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoSNG | A012D \| A545D \| A918D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| DestructibleStash_Ammo_SNG_Newbie | SNG ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoSNG | A012D \| A545D \| A918D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| DestructibleStash_Ammo_SNG_Veteran | SNG ammo wrapper | test/debug physical root | 0 | 5 | 0 | destructible_world | Chance \| MinCount \| MaxCount |  | DestructibleStash_AmmoSNG | A012D \| A545D \| A918D \| A919D | EXCLUDE — no confirmed production destructible consumer |
| Stash_Cheap_Experienced | cross-system normal stash wrapper | cross-system physical exception | 1 | 1 | 0 | destructible_world, item_container_world | Chance, Weight, Count |  | 0 | A012D \| A918D \| A919D \| Bandage \| Bread \| CannedFood \| Energetic \| GrenadeRGD5 \| Medkit \| Milk \| MoneyCommon \| MoneyEpic \| MoneyRare \| Sausage \| Vodka \| Water | EXCLUDE — normal world-stash wrapper used by one destructible exception; do not fold into destructible family |
| Stash_Cheap_Newbie | cross-system normal stash wrapper | cross-system physical exception | 1 | 1 | 0 | destructible_world, item_container_world | Chance, Weight, Count |  | 0 | A012D \| A918D \| A919D \| Bandage \| Bread \| CannedFood \| Energetic \| GrenadeRGD5 \| Medkit \| Milk \| MoneyCommon \| MoneyEpic \| MoneyRare \| Sausage \| Vodka \| Water | EXCLUDE — normal world-stash wrapper used by one destructible exception; do not fold into destructible family |
| DestructibleStash_Drink | drink pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Weight \| MinCount \| MaxCount | Beer \| Energetic \| Vodka \| Water |  | Beer \| Energetic \| Vodka \| Water | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Drink_Experienced | drink pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Chance \| MinCount \| MaxCount | Beer \| Energetic \| Vodka \| Water |  | Beer \| Energetic \| Vodka \| Water | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Drink_Master | drink pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Chance \| MinCount \| MaxCount | Beer \| Energetic \| Vodka \| Water |  | Beer \| Energetic \| Vodka \| Water | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Drink_Veteran | drink pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Chance \| MinCount \| MaxCount | Beer \| Energetic \| Vodka \| Water |  | Beer \| Energetic \| Vodka \| Water | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Food | food pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Weight \| MinCount \| MaxCount | Bread \| CannedFood \| Milk \| Sausage |  | Bread \| CannedFood \| Milk \| Sausage | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Food_Experienced | food pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Chance \| MinCount \| MaxCount | Bread \| CannedFood \| Milk \| Sausage |  | Bread \| CannedFood \| Milk \| Sausage | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Food_Master | food pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Chance \| MinCount \| MaxCount | Bread \| CannedFood \| Milk \| Sausage |  | Bread \| CannedFood \| Milk \| Sausage | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Food_Veteran | food pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Chance \| MinCount \| MaxCount | Bread \| CannedFood \| Milk \| Sausage |  | Bread \| CannedFood \| Milk \| Sausage | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Food_Drink | food+drink wrapper | production destructible root | 1210 | 1214 | 0 | destructible_world | Weight |  | DestructibleStash_Drink \| DestructibleStash_Food | Beer \| Bread \| CannedFood \| Energetic \| Milk \| Sausage \| Vodka \| Water | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Food_Drink_Experienced | food+drink wrapper | production destructible root | 953 | 994 | 0 | destructible_world | Chance |  | DestructibleStash_Drink_Experienced \| DestructibleStash_Food_Experienced | Beer \| Bread \| CannedFood \| Energetic \| Milk \| Sausage \| Vodka \| Water | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Food_Drink_Master | food+drink wrapper | production destructible root | 953 | 994 | 0 | destructible_world | Chance |  | DestructibleStash_Drink_Master \| DestructibleStash_Food_Master | Beer \| Bread \| CannedFood \| Energetic \| Milk \| Sausage \| Vodka \| Water | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Food_Drink_Newbie | food+drink wrapper | referenced but undefined | 20 | 59 | 0 | unknown |  |  |  |  | REVIEW/EXCLUDE — physical references exist but no supplied generator definition |
| DestructibleStash_Food_Drink_Veteran | food+drink wrapper | production destructible root | 953 | 994 | 0 | destructible_world | Chance |  | DestructibleStash_Drink_Veteran \| DestructibleStash_Food_Veteran | Beer \| Bread \| CannedFood \| Energetic \| Milk \| Sausage \| Vodka \| Water | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_F1 | grenade pool | production destructible root | 101 | 101 | 0 | destructible_world | Weight \| MinCount \| MaxCount | GrenadeF1 |  | GrenadeF1 | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_RGD | grenade pool | production destructible root | 81 | 81 | 0 | destructible_world | Weight \| MinCount \| MaxCount | GrenadeRGD5 |  | GrenadeRGD5 | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Med | medical base pool | production destructible root | 869 | 870 | 0 | destructible_world | Weight \| MinCount \| MaxCount | Bandage \| Medkit |  | Bandage \| Medkit | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Med_Experienced | medical wrapper | production destructible root | 549 | 554 | 0 | destructible_world | Chance |  | DestructibleStash_Medkit_Experienced | ArmyMedkit \| EcoMedkit \| Medkit | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Med_Master | medical wrapper | production destructible root | 549 | 554 | 0 | destructible_world | Chance |  | DestructibleStash_Medkit_Master | ArmyMedkit \| EcoMedkit \| Medkit | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Med_Newbie | medical wrapper | referenced but undefined | 0 | 5 | 0 | unknown |  |  |  |  | REVIEW/EXCLUDE — physical references exist but no supplied generator definition |
| DestructibleStash_Med_Veteran | medical wrapper | production destructible root | 549 | 554 | 0 | destructible_world | Chance |  | DestructibleStash_Medkit_Veteran | ArmyMedkit \| EcoMedkit \| Medkit | CANDIDATE — exact raw/local fields only; preserve family semantics |
| DestructibleStash_Medkit_Experienced | medkit pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Weight \| MinCount \| MaxCount | ArmyMedkit \| EcoMedkit \| Medkit |  | ArmyMedkit \| EcoMedkit \| Medkit | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStash_Medkit_Master | medkit pool | internal dependency; cross-context | 0 | 0 | 1 | destructible_world, item_container_world | Weight \| MinCount \| MaxCount | ArmyMedkit \| EcoMedkit \| Medkit |  | ArmyMedkit \| EcoMedkit \| Medkit | REVIEW — leaf/shared dependency reaches non-destructible content |
| DestructibleStash_Medkit_Newbie | medkit pool | orphan/unreferenced definition | 0 | 0 | 0 | — | Weight \| MinCount \| MaxCount | ArmyMedkit \| EcoMedkit \| Medkit |  | ArmyMedkit \| EcoMedkit \| Medkit | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStash_Medkit_Veteran | medkit pool | internal destructible dependency | 0 | 0 | 1 | destructible_world | Weight \| MinCount \| MaxCount | ArmyMedkit \| EcoMedkit \| Medkit |  | ArmyMedkit \| EcoMedkit \| Medkit | CANDIDATE DEPENDENCY — only through approved production wrappers |
| DestructibleStashProlog_AmmoA545D | prolog residue | orphan/unreferenced definition | 0 | 0 | 0 | — | Weight \| MinCount \| MaxCount | A545D |  | A545D | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStashProlog_Drink | prolog residue | orphan/unreferenced definition | 0 | 0 | 0 | — | Chance \| MinCount \| MaxCount | CannedFood \| Vodka |  | CannedFood \| Vodka | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStashProlog_Med | prolog residue | orphan/unreferenced definition | 0 | 0 | 0 | — | Chance \| MinCount \| MaxCount | Bandage \| Medkit |  | Bandage \| Medkit | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStash_Stimulator | stimulator pool | orphan/unreferenced definition | 0 | 0 | 0 | — | Weight \| MinCount \| MaxCount | Cinnamon \| Hercules \| PSYBlocker |  | Cinnamon \| Hercules \| PSYBlocker | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStash_SupMed_Experienced | support-med pool | orphan/unreferenced definition | 0 | 0 | 0 | — | Chance \| MinCount \| MaxCount | AntiRad \| Bandage |  | AntiRad \| Bandage | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStash_SupMed_Master | support-med pool | orphan/unreferenced definition | 0 | 0 | 0 | — | Chance \| MinCount \| MaxCount | AntiRad \| Bandage |  | AntiRad \| Bandage | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStash_SupMed_Newbie | support-med pool | orphan/unreferenced definition | 0 | 0 | 0 | — | Weight \| MinCount \| MaxCount | AntiRad \| Bandage |  | AntiRad \| Bandage | EXCLUDE — no resolved consumer in supplied graph |
| DestructibleStash_SupMed_Veteran | support-med pool | orphan/unreferenced definition | 0 | 0 | 0 | — | Chance \| MinCount \| MaxCount | AntiRad \| Bandage |  | AntiRad \| Bandage | EXCLUDE — no resolved consumer in supplied graph |

## Family conclusions

### Food + drink crates

- Production Newbie wiring mostly uses `DestructibleStash_Food_Drink`, while Experienced/Veteran/Master use explicit rank wrappers.
- **20 production WorldMap rows reference `DestructibleStash_Food_Drink_Newbie`, but that generator definition is absent from the supplied CFG dump.** These rows are excluded from any future writer until the missing definition is sourced or runtime behavior is proven.
- The base wrapper delegates with `Weight=1` to separate food and drink pools. Experienced/Veteran/Master wrappers delegate with `Chance=0.6/0.7/0.8` to rank-specific food and drink pools. Leaf pools then contain their own item `Chance` or `Weight` values. A single percentage scalar must not flatten these layers.

### Medical crates

- Production Newbie uses `DestructibleStash_Med` directly. Experienced/Veteran/Master use wrappers that delegate to corresponding medkit pools.
- `DestructibleStash_Med_Newbie` is referenced only by test/quest wiring and has no definition in this dump; it is not part of the strict production surface.
- `DestructibleStash_Medkit_Master` is also consumed by one DLC ItemContainer. Editing that leaf is therefore cross-context and cannot be called destructible-only.
- `DestructibleStash_SupMed_*` and `DestructibleStash_Stimulator` are defined but have no resolved incoming consumer path in the supplied graph; they stay excluded.

### Ammo crates

- Production `D_WoodenAmmoCrate_01` actors use direct `DestructibleStash_AmmoSNG` / `DestructibleStash_AmmoNATO` pools plus direct grenade pools; the production wiring does **not** use the rank-specific ammo wrapper family.
- Rank-specific `Ammo_*_{Newbie,Experienced,Veteran,Master}` wrappers are observed on test-map destructibles. The Master wrappers are additionally reused through four ItemGenerator links across three DLC ItemContainer actors (one bag contains both SNG and NATO links), so their leaf pools have a cross-context blast radius.
- Ammo pool rows use `Weight` + count ranges. That is selection/quantity structure, not a proven global drop-probability scalar.

### Grenade destructibles

- `DestructibleStash_F1` and `DestructibleStash_RGD` are direct production grenade pools on wooden ammo crates. Their local rows use `Weight` + count ranges, not `Chance`.
- Treating them as a generic percentage drop chance would therefore be semantically invented.

### SmartLoot overlay

- Production destructibles with SmartLoot: **32**. SmartLoot-only: **28**; dual ItemGenerator + SmartLoot: **4**.
- SmartLoot SIDs observed in strict production: `StashGrenade_Cheap` (9), `StashMedicine_Cheap` (8), `Stash_AmmoAll_Cheap` (7), `Stash_AmmoNATO_Smart` (3), `StashGrenade_Smart` (2), `StashFood_Cheap` (2), `StashMedicine_Smart` (1).
- These SmartLoot assignments remain a separate system and must not be silently folded into destructible ItemGenerator controls.

## Control-safety verdict

| Proposed idea | Verdict | Reason |
| --- | --- | --- |
| Destructible Food/Drink global chance | **REVIEW** | Wrapper and leaf layers mix `Weight` and `Chance`; 20 production Newbie references are unresolved. |
| Destructible Medical global chance | **REVIEW** | Newbie and higher ranks use different structures; Master medkit leaf also reaches a DLC ItemContainer. |
| Destructible Ammo amount | **STRUCTURAL CANDIDATE** | Exact ammo pool count ranges are mapped, but selection `Weight` must be preserved and cross-context Master wrapper paths reviewed. |
| Destructible Grenade chance | **NOT 1:1** | F1/RGD source rows use `Weight`, not `Chance`. |
| Destructible SmartLoot abundance | **SEPARATE DOMAIN** | SmartLoot is independent and only present on 32 strict production destructibles. |
| Prefix-wide `DestructibleStash*` edit | **NO** | The prefix includes production roots, test-only wrappers, cross-context dependencies, unresolved references and orphan definitions. |

## Required rule for future writer work

A destructible control must be built from an explicit allowlist of **raw/local generator rows plus resolved production physical consumers**. No control may target all `DestructibleStash*` definitions by prefix. Missing generator definitions and cross-context leaf pools must remain excluded unless separately resolved.


### 2026-10-09 — Breakable box ammunition source counts displayed by Ammo Yield

The real `loot.destructibleAmmoQuantity` scaler (native 0.5–2x, step 0.25x) now produces a **read-only live per-caliber stack-count preview**, independent of all four weight controls and of NPC corpse SmartLoot. The physical generator `GameData/ItemGeneratorPrototypes.cfg` defines these eight source entries, preserved in the writer's `special.destructibleAmmoProduction.pools`:

| Pool | Caliber | Item SID | Source stack at 1x |
| --- | --- | --- | ---: |
| SNG | 9×18 | `A918D` | 3–6 |
| SNG | 9×19 | `A919D` | 5–10 |
| SNG | 5.45 | `A545D` | 5–10 |
| SNG | 12 Gauge | `A012D` | 2–4 |
| NATO | .45 ACP | `A045D` | 5–10 |
| NATO | 9×19 | `A919D` | 5–10 |
| NATO | 5.56 | `A556D` | 5–10 |
| NATO | 12 Gauge | `A012D` | 5–10 |

Both min/max endpoints are scaled with the writer's **`ScalePositiveInteger(source, multiplier) = max(1, floor(source × multiplier + 0.5))`**, so 9×18 at 0.5x becomes **2–3**, at 1x remains **3–6**, and at 2x becomes **6–12**. The SNG and NATO 12 Gauge sources must not be merged because their counts differ. Preview label `ROUNDS IF ROLLED` makes clear these are possible stack sizes conditional on a generator selection, **not guaranteed drops**. A disabled 0x caliber weight retains the source amount for illustration but the card is subdued. The amounts never read `NPC_Ammo_Smart`, weapon-attached NPC ammo, world stashes, or other generator sources. Only WPF preview code and documentation changed; native CFG writer mappings remain intact. Local publish/Wine and in-game validation pending.
