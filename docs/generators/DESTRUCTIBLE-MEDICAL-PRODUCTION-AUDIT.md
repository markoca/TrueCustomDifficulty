# Destructible Medical Production Audit

## Decision

**NO NORMAL UI CONTROL.** The production medical branch is mapped and understood, but neither a global `Medical Quantity` nor `Medical Frequency` slider is a truthful 1:1 abstraction of the vanilla CFG semantics.

## Production footprint

- **869 unique production-world medical destructible actors**.
- The physical surface is almost entirely `D_MetallCrate_01`; the audited wiring contains one `M_MetallTin_10` exception among the rank rows.
- `DestructibleStash_Med` is the Newbie/base production root and is wired on all **869** production actors.
- Experienced, Veteran and Master rank overrides are present on **549** of those actors each. These are rank alternatives on the same physical actors, not additional crates.
- Therefore the 2,516 production wiring rows must **not** be interpreted as 2,516 physical medical containers.

## Vanilla branch map

| Rank path | Root/wrapper | Child | Reachable medical items | Selection fields | Count surface |
|---|---|---|---|---|---|
| Newbie/base | `DestructibleStash_Med` | — | Bandage, Medkit | Weight | Medkit 1..1; Bandage 1..2 |
| Experienced | `DestructibleStash_Med_Experienced` | `DestructibleStash_Medkit_Experienced` | Medkit, Army Medkit, Scientific Medkit | wrapper Chance + child Weight | Medkit 1..2; others 1..1 |
| Veteran | `DestructibleStash_Med_Veteran` | `DestructibleStash_Medkit_Veteran` | Medkit, Army Medkit, Scientific Medkit | wrapper Chance + child Weight | all 1..1 |
| Master | `DestructibleStash_Med_Master` | `DestructibleStash_Medkit_Master` | Medkit, Army Medkit, Scientific Medkit | wrapper Chance + child Weight | all 1..1 |

The 11 directly relevant item rows contain only **two** variable `1..2` ranges. The remaining **nine** rows are fixed `1..1`.

## Why `Medical Quantity` stays out of the UI

Scaling positive integer `MinCount`/`MaxCount` values below vanilla cannot meaningfully reduce nine fixed `1..1` rows unless the tool invents `0` counts. Once zero is introduced, the control no longer changes only quantity: it can suppress an item outcome and therefore changes availability/frequency semantics as well.

An above-100% quantity control would be technically possible, but a one-sided control is not a clean counterpart to the other abundance controls and still needs production-only isolation for the shared Master leaf. Keep this branch audit-only unless a future design explicitly defines those semantics.

## Why `Medical Frequency` is not a valid global control

There is no single common frequency field across the branch:

- the Newbie/base root selects Bandage vs Medkit by **Weight**;
- higher-rank wrappers expose **Chance** for entering their medkit child pool;
- the child pools select Medkit / Army Medkit / Scientific Medkit by **Weight**.

Scaling these together would combine different CFG meanings. Scaling only weights changes composition, not a clean global spawn frequency. Therefore a `Medical Frequency` percentage would be a false abstraction.

## Cross-context boundary

`DestructibleStash_Medkit_Master` is not destructible-only. It is also consumed by **one non-destructible production ItemContainer**. Global edits to that leaf would leak outside the intended destructible domain.

Any future medical implementation must therefore use a TCD-owned clone plus an audited production-only redirect surface, rather than patching the shared vanilla leaf globally.

## Unresolved `DestructibleStash_Med_Newbie`

`DestructibleStash_Med_Newbie` has no definition in the supplied CFG corpus, but its five references are **not part of the normal production-world surface**:

- 4 × `TestMap_Vortex_WP1` (`test_debug`)
- 1 × `WorldMap_WP/E01_MQ01_LogicLevel_WP` (`quest_scripted`)

Normal production Newbie medical destructibles use the defined `DestructibleStash_Med` root. The unresolved SID must remain excluded from runtime production redirects and from UI claims.

## Final status

| Candidate | Status | Reason |
|---|---|---|
| Destructible Medical Quantity | **DEFER / NO UI** | 9/11 item rows are fixed 1..1; down-scaling requires invented zero-count semantics |
| Destructible Medical Frequency | **NO UI** | no common frequency field; Weight and Chance have different meanings |
| Medical item composition | **NO UI** | would directly rebalance Bandage/Medkit/Army/Scientific selection rather than core difficulty abundance |
| `DestructibleStash_Med_Newbie` | **EXCLUDE** | undefined and only test/quest-scripted references |
| Global edit of `DestructibleStash_Medkit_Master` | **FORBIDDEN** | shared with a non-destructible production ItemContainer |
