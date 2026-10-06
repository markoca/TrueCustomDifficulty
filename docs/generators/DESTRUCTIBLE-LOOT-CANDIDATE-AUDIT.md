# Destructible Loot Candidate Audit

## Scope

This report evaluates the **production-world breakable/destructible ItemGenerator surface** for UI controls. It uses the audited SpawnActor wiring and local ItemGenerator rows already present in the project.

The goal is not to invent a generic "crate loot %" setting. The goal is to identify controls that map to one consistent source meaning across normal gameplay.

Production ItemGenerator coverage in the supplied data:

- **3,657** unique production destructible actors have an ItemGenerator path.
- **1,370** physical actors are in the food/drink family.
- **1,235** are ammo-pool actors.
- **869** are medical-pool actors.
- **182** are grenade-pool actors.
- **1** additional destructible actor uses the normal cross-system `Stash_Cheap_*` path and stays outside this family.
- These groups are disjoint at the physical-root level.
- SmartLoot-only / dual-SmartLoot destructibles are a separate system and are not folded into these ItemGenerator controls.

This is a **static production footprint**, not measured play-session frequency. It tells us how broadly a generator is wired into the normal world.

## Practical ranking

| Family | Production physical actors | Main physical props | Reachable items |
|---|---:|---|---|
| Food / Drink | 1,370 | DSP wooden crates, wooden boxes, two clay vases | Bread, Sausage, Canned Food, Milk, Water, Beer, Vodka, Energy Drink |
| Ammo | 1,235 | Almost entirely `D_WoodenAmmoCrate_01` | SNG: 9×18, 9×19, 5.45×39, 12ga; NATO: .45 ACP, 9×19, 5.56×45, 12ga |
| Medical | 869 | Almost entirely `D_MetallCrate_01` | Bandage, Medkit, Army Medkit, Scientific Medkit |
| Grenades | 182 | `D_WoodenAmmoCrate_01` | F1 or RGD-5 |

`D_WoodenAmmoCrate_01` alone accounts for **1,416** audited production ItemGenerator actors: 982 SNG ammo, 252 NATO ammo, 101 F1, and 81 RGD-5.

## Ammo crates

### SNG pool

`DestructibleStash_AmmoSNG`

| Item | Weight | Vanilla count |
|---|---:|---:|
| 9×18 (`A918D`) | 2 | 3–6 |
| 9×19 (`A919D`) | 2 | 5–10 |
| 5.45×39 (`A545D`) | 2 | 5–10 |
| 12ga (`A012D`) | 2 | 2–4 |

Production destructible footprint: **983** actors, of which 982 are normal wooden ammo crates and one is a world-map movable-chair exception.

### NATO pool

`DestructibleStash_AmmoNATO`

| Item | Weight | Vanilla count |
|---|---:|---:|
| .45 ACP (`A045D`) | 2 | 5–10 |
| 9×19 (`A919D`) | 2 | 5–10 |
| 5.56×45 (`A556D`) | 2 | 5–10 |
| 12ga (`A012D`) | 2 | 5–10 |

Production destructible footprint: **252** wooden ammo crates.

### Control decision — Destructible Ammo Quantity

**IMPLEMENTED — v0.2.9 / runtime test required**

A single control can scale only the eight `MinCount` / `MaxCount` endpoints while preserving every `Weight=2` value. That changes **how much ammo is produced without changing which ammo family is selected**.

Do **not** patch `DestructibleStash_AmmoSNG` or `DestructibleStash_AmmoNATO` globally. Both pools also feed non-destructible ItemContainer paths through existing wrapper dependencies. The safe implementation is the same pattern established for Cheap stash controls:

1. create TCD-owned SNG and NATO destructible pools from the audited vanilla rows;
2. modify count fields only;
3. redirect only confirmed production destructible actors.

A separate "Ammo Frequency" control is **not justified** by these roots: the production pools use `Weight`, not a local spawn `Chance`.

## Grenade crates

`DestructibleStash_F1` and `DestructibleStash_RGD` are unusually simple.

| Generator | Item | Weight | Count | Production actors |
|---|---|---:|---:|---:|
| `DestructibleStash_F1` | F1 | 1 | 1–2 | 101 |
| `DestructibleStash_RGD` | RGD-5 | 1 | 1–2 | 81 |

### Control decision — Destructible Grenade Quantity

**IMPLEMENTED — v0.2.10 / runtime test required**

This is a clean count-only surface: scale `MinCount/MaxCount`, preserve `Weight=1`, and leave the F1/RGD assignment of each physical crate unchanged. v0.2.10 implements this through two TCD-owned leaf pools and an embedded **182-assignment** production-only redirect manifest (101 F1 + 81 RGD-5).

A "Grenade Chance/Frequency" control is **not supported by the local generator fields**. These roots do not have a `Chance`; their presence is determined by which physical destructible is wired to each generator.

The footprint is much smaller than ammo/food/medical (**182 actors**), so this is useful but lower priority than Destructible Ammo Quantity.

## Food / Drink crates

This family is the broadest physically (**1,370 actors**) but has the least uniform tuning surface.

### Newbie/base leaves

`DestructibleStash_Food` and `DestructibleStash_Drink` use **Weight**, with all item counts fixed at `1–1`.

Food:
- Sausage — Weight 0.3
- Bread — Weight 0.3
- Canned Food — Weight 0.2
- Milk — Weight 0.1

Drink:
- Water — Weight 0.3
- Beer — Weight 0.3
- Vodka — Weight 0.2
- Energy Drink — Weight 0.1

The base wrapper `DestructibleStash_Food_Drink` then links both child pools with `Weight=1`.

### Experienced / Veteran / Master

Higher-rank wrappers switch to **Chance**:
- Experienced wrapper: Food 0.6, Drink 0.6
- Veteran wrapper: Food 0.7, Drink 0.7
- Master wrapper: Food 0.8, Drink 0.8

The child item rows also use per-item Chance values, and their counts are a mix of `1–1` and `1–2`.

Across the 32 food/drink leaf item rows:
- **12** are fixed `1–1`;
- **20** are `1–2`.

There are also **20 production references** to `DestructibleStash_Food_Drink_Newbie`, but that generator definition is not present in the supplied CFG dump.

### Control decisions

**Food / Drink Quantity — DEFER**

Count-only editing is technically possible for the rows we can resolve, but a large part of the system is `1–1`. A below-100% quantity slider would therefore frequently do nothing unless we introduce zero-count semantics, which would turn a quantity control into an availability control.

**Food / Drink Frequency — NO UI**

There is no one frequency field. Newbie uses Weight, higher ranks use Chance at both wrapper and leaf levels, and one Newbie wrapper is unresolved. Combining these into one percentage would be a false abstraction.

## Medical crates

Physical footprint: **869** actors, almost all `D_MetallCrate_01`.

Base `DestructibleStash_Med`:
- Medkit — Weight 1, Count 1–1
- Bandage — Weight 2, Count 1–2

Higher-rank paths use a medkit child pool.

Experienced:
- Medkit — Weight 20, Count 1–2
- Army Medkit — Weight 4, Count 1–1
- Scientific Medkit — Weight 1, Count 1–1

Veteran:
- Medkit — Weight 20, Count 1–1
- Army Medkit — Weight 10, Count 1–1
- Scientific Medkit — Weight 5, Count 1–1

Master:
- Medkit — Weight 10, Count 1–1
- Army Medkit — Weight 10, Count 1–1
- Scientific Medkit — Weight 10, Count 1–1

Across the 11 directly relevant medical item rows:
- **9** are fixed `1–1`;
- only **2** have a `1–2` range.

`DestructibleStash_Medkit_Master` also reaches a non-destructible ItemContainer path, so that leaf cannot be globally patched for a destructible-only control.

### Control decisions

**Medical Quantity — DEFER**

The source supports count editing, but down-scaling would be ineffective on 9/11 rows unless we add zero-count behavior. This is not a good normal UI control yet.

**Medical Frequency — NO UI**

The medical system uses weighted item selection rather than a common spawn-Chance field. Changing weights changes the **composition of medical loot**, not a clean global frequency.

## Current implementation status

1. **Destructible Ammo Quantity** — implemented in v0.2.9; runtime test required.
2. **Destructible Grenade Quantity** — implemented in v0.2.10; runtime test required.
3. Food/Drink and Medical remain fully documented but out of the normal UI for now.

Ammo consumes the **1,235-assignment** production manifest in `DESTRUCTIBLE-AMMO-PRODUCTION-FANOUT.md`. Grenades consume the **182-assignment** production manifest in `DESTRUCTIBLE-GRENADE-PRODUCTION-FANOUT.md`. Both controls use TCD-owned leaf pools and preserve vanilla selection weights.

## Machine-readable files

- `destructible-loot-family-summary.csv`
- `destructible-loot-item-matrix.csv`
- `destructible-loot-physical-coverage.csv`
- `destructible-loot-control-surfaces.csv`
- `destructible-loot-candidate-summary.json`

The earlier raw audit files remain the source of truth for exact line-level wiring and source fields.
