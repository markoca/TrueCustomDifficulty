# Medium Stash Ammo Quantity — deep audit and production scope

This audit defines the safe runtime surface for a Medium-stash ammo-quantity control. It changes only integer ammo `MinCount/MaxCount`; all vanilla caliber-selection `Weight` values, progression-rank gates, consumables and coupon logic remain unchanged.

## Source ammo surface

- Shared parent: `GamePass_Stash_ItemGenerator_Common_Var1`
- Ammo rows: **25** across Newbie / Experienced / Veteran / Master progression blocks.
- The Newbie block intentionally contains `A919D` twice; this vanilla source fact is preserved.

| Rank | Item | Weight | Vanilla count | CFG line |
|---|---|---:|---:|---:|
| Newbie | `A919D` | 5 | 18–24 | 64762 |
| Newbie | `A545D` | 5 | 18–24 | 64768 |
| Newbie | `A919D` | 5 | 18–24 | 64774 |
| Newbie | `A012D` | 5 | 4–6 | 64780 |
| Newbie | `A045D` | 3 | 30–45 | 64786 |
| Experienced | `A919D` | 5 | 18–24 | 64824 |
| Experienced | `A545D` | 5 | 18–24 | 64830 |
| Experienced | `A012D` | 5 | 4–6 | 64836 |
| Experienced | `A045D` | 3 | 30–45 | 64842 |
| Experienced | `A556D` | 5 | 18–24 | 64848 |
| Veteran | `A919D` | 5 | 18–24 | 64861 |
| Veteran | `A545D` | 5 | 18–24 | 64867 |
| Veteran | `A762SniperS` | 5 | 25–30 | 64873 |
| Veteran | `A012D` | 3 | 4–6 | 64879 |
| Veteran | `A045D` | 3 | 30–45 | 64885 |
| Veteran | `A556D` | 5 | 18–24 | 64891 |
| Veteran | `A762SniperD` | 1 | 45–55 | 64897 |
| Master | `A919D` | 5 | 18–24 | 64910 |
| Master | `A545D` | 5 | 18–24 | 64916 |
| Master | `A762SniperS` | 5 | 5–10 | 64922 |
| Master | `A012D` | 3 | 4–6 | 64928 |
| Master | `A045D` | 3 | 30–45 | 64934 |
| Master | `A556D` | 5 | 18–24 | 64940 |
| Master | `A762SniperD` | 1 | 45–55 | 64946 |
| Master | `A939D` | 5 | 18–24 | 64952 |

## Confirmed production-only scope

| Production branch | Physical normal-world containers | Redirect assignments |
|---|---:|---:|
| Rank-based `Stash_Medium_<rank>` usage | **35** | **140** |
| Location-specific wrappers nesting `GamePass_Stash_ItemGenerator_Common_Var1` | **218** through 213 wrapper roots | **213** |
| Direct `WorldMap_WP` references to the shared parent | **97** | **97** |
| **Total** | **350** unique physical containers | **450** |

The three physical groups are disjoint. Wrapper redirects are performed at the wrapper-local `SubItemGenerator` edge, so one redirect can safely cover multiple normal-world consumers of the same production-only wrapper.

## Protected surfaces

- **14** authored/scripted or quest wrapper roots remain vanilla.
- **12** wrappers with no resolved runtime consumer remain on hold and vanilla.
- **4** `TestMap_Balance` rank settings remain vanilla.
- **4** `GDEQ1403_LogicLevel` rank settings remain vanilla.
- **2** direct `DLC01_ANCQ_CNPP_Level` uses of the shared parent remain vanilla.

## Runtime implementation contract

Create five TCD-owned complete Medium roots: the shared Medium parent alias plus four rank aliases. Materialize the complete audited vanilla Medium template (**10 category blocks / 39 item rows**) into each root, inheriting only neutral generator template `[0]`. Scale only the 25 ammo rows. Do not globally patch the vanilla shared parent or vanilla rank wrappers. Apply exactly `medium-stash-production-redirect-manifest.csv`.

## Scaling

Use **50–200% in 25% steps**, positive round-half-up per `MinCount` and `MaxCount`, clamped to at least 1. Because Medium ammo stacks are already relatively large, each 25% step changes the practical amount without creating false 5% precision.

## Status

`SEMANTICS: READY` · `SCALING: READY` · `PRODUCTION SCOPE: READY` · `WRITER/UI: IMPLEMENTED (runtime test required)`
