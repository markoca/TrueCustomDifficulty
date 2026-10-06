# Breakable Box Ammo Distribution

Version: 0.9.26.44

## Scope

`Breakable Box Ammo Distribution` is a four-axis radar that changes only the `Weight` fields of ammo entries that already exist in the vanilla dedicated breakable-ammo pools.

**It never adds, removes, or substitutes a `PossibleItems` entry.**

That rule is deliberate: the control tunes vanilla selection distribution rather than expanding vanilla box contents.

## Player-facing axes

| Radar axis | Existing vanilla entries affected |
|---|---|
| **9 mm** | `A918D` (9×18) and `A919D` (9×19) |
| **5.45 / 5.56 mm** | `A545D` (5.45×39) and `A556D` (5.56×45) |
| **.45 ACP** | `A045D` |
| **12 Gauge** | `A012D` |

All affected vanilla entries have authored `Weight = 2`.

The same family multiplier is applied to each existing member of that family, wherever that member already exists.

Examples:

- `9 mm = 2x` -> existing `A918D` and `A919D` entries use Weight `4`
- `5.45 / 5.56 mm = 0.5x` -> existing `A545D` and `A556D` entries use Weight `1`
- `.45 ACP = 0x` -> existing `A045D` entry uses Weight `0`
- `12 Gauge = 1x` -> existing `A012D` entries remain Weight `2`

## Vanilla pool membership stays immutable

The two vanilla pool memberships remain exactly:

### Internal eastern pool

- `A918D`
- `A919D`
- `A545D`
- `A012D`

### Internal western pool

- `A045D`
- `A919D`
- `A556D`
- `A012D`

The internal pool names remain implementation detail and are not shown as player-facing controls.

In particular, the radar does **not** add:

- `A939D` / 9×39
- `A762NATOD` / 7.62×51
- `A762SniperD` / 7.62×54
- `A762D` / 7.62×39
- Gauss or launcher ammunition

The full caliber audit remains useful evidence about the wider game ammo domain, but those entries are intentionally outside this control.

## Semantics

Each axis runs from **0x to 3x** in **0.25x** steps.

- `0x` = existing members of that family have Weight 0
- `1x` = exact vanilla Weight
- `3x` = triple the authored Weight

The graph's **1x ring** is therefore a true vanilla reference on all four axes.

The control prevents a state where every existing candidate of either vanilla pool has Weight 0.

## Player-facing spawn percentages

The radar shows the multiplier together with the relative weighted spawn percentage for each concrete vanilla ammo variant.

For grouped families, each concrete caliber is shown on its own row:

- `9×18 Spawn ...%`
- `9×19 Spawn ...% / ...%`
- `5.45×39 Spawn ...%`
- `5.56×45 Spawn ...%`

`.45 ACP` and `12×70` use one detail row.

Some ammo variants are present in two different vanilla breakable-box pools. When that happens, the row shows the minimum-to-maximum Spawn range across those applicable pools. One-pool ammo shows one percentage. Every row therefore uses the same player-facing metric without exposing internal pool names.

At full vanilla `1x` settings all authored entries have `Weight = 2`, so every individual candidate is 25% inside its applicable four-entry pool. The grouped 9×18 / 9×19 axis does **not** merge those entries; it only drives both existing vanilla weights with one multiplier.

## Interaction

- drag a point to change that family multiplier
- arrow keys or `+` / `-` change the selected family by 0.25x
- right-click a point resets only that family to 1x
- `R` resets all four families to 1x

## Composition with Ammo from Breakable Boxes

The existing TCD-owned ammo pool clones are shared by both controls:

- **Breakable Box Ammo Distribution** changes only `Weight`
- **Ammo from Breakable Boxes** changes only `MinCount / MaxCount`

The writer composes both into the same clone and never creates competing pool definitions.

The audited physical redirect surface remains:

- 934 base
- 301 DLC
- 1,235 total

Status: `runtime_test_required`.
