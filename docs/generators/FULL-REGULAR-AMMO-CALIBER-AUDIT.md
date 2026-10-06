# Full Regular Ammo Caliber Audit

Version: 0.9.26.44 — audit-only

## Conclusion

The breakable-ammo distribution should cover **9 proven regular weapon calibers**, not every `EAmmoCaliber` present in `ItemPrototypes.cfg`.

The current radar already covers 6 of those 9. Three regular calibers are missing and are clean candidates to add:

- **9×39 mm** — `A939D`
- **7.62×51 mm** — `A762NATOD`
- **7.62×54 mm** — `A762SniperD`

This would expand Breakable Box Ammo Distribution from **6 to 9 player-facing caliber axes**.

## Proven regular weapon calibers

| Caliber | Default ammo SID | Base weapons | DLC weapons | Current breakable boxes | Recommended pool |
|---|---|---:|---:|---|---|
| 9×18 mm | `A918D` | 5 | 0 | yes | Eastern |
| 9×19 mm | `A919D` | 6 | 0 | yes | both |
| .45 ACP | `A045D` | 3 | 0 | yes | Western |
| 5.45×39 mm | `A545D` | 6 | 2 | yes | Eastern |
| 5.56×45 mm | `A556D` | 5 | 2 | yes | Western |
| 9×39 mm | `A939D` | 4 | 1 | **no** | Eastern |
| 7.62×51 mm | `A762NATOD` | 3 | 2 | **no** | Western |
| 7.62×54 mm | `A762SniperD` | 5 | 2 | **no** | Eastern |
| 12×70 Buckshot | `A012D` | 7 regular/weapon-data consumers in base + 3 DLC | 3 | yes | both |

The base/DLC weapon counts above come from exact `AmmoCaliber` assignments in `WeaponGeneralSetupPrototypes.cfg`. Some setup entries are special weapon-data consumers rather than normal player guns, so the important gate is **non-zero proven weapon use**, not the exact count as a gameplay roster.

## Why the three missing calibers are safe candidates

### 9×39 mm — `A939D`

- regular `TemplateAmmo` descendant
- `AmmoType = Default`
- 30-round ammo pack
- used by proven weapons including Rhino / Gvintar / Grim / Lavina and a DLC AK-9 setup
- already appears broadly in vanilla generators, stashes and world content
- fits the existing Eastern-caliber breakable box family

### 7.62×51 mm — `A762NATOD`

- regular `TemplateAmmo` descendant
- `AmmoType = Default`
- 20-round ammo pack
- used by proven DMR/sniper setups in base and DLC
- already appears in vanilla ammo generators and stashes
- fits the existing Western-caliber breakable box family

### 7.62×54 mm — `A762SniperD`

- regular `TemplateAmmo` descendant
- `AmmoType = Default`
- 20-round ammo pack
- used by PKP / SVD / SVU / Three-Line / SKP-family setups plus DLC weapons
- already widely present in vanilla generators and stashes
- fits the existing Eastern-caliber breakable box family

## Important exclusion: 7.62×39 mm

`A762D` exists as a real ammo prototype and even has some DLC loose-world spawn actors.

However, the audited `WeaponGeneralSetupPrototypes.cfg` data contains **zero exact weapons with**:

`AmmoCaliber = EAmmoCaliber::A762`

in both base and DLC.

Therefore it is **not** included in the regular caliber radar at this stage. Adding it merely because the ammo prototype exists would violate the project's 1:1 evidence rule.

If a later DLC/mod weapon is proven to consume `EAmmoCaliber::A762`, it can be promoted then.

## Other excluded calibers

These are real ammo types but are not ordinary firearm-ammo distribution candidates:

- `AGA` — Gauss ammo
- `AVOG` — 40 mm VOG launcher round
- `AHEDP` — 40 mm HEDP launcher round
- `APG7V` — RPG-7 rocket

They should remain outside Breakable Box Ammo Distribution.

## Ammo variants

The radar should continue to represent **caliber**, not ammo variant.

For regular calibers, the game also defines variants such as:

- Armor Piercing
- Expanding
- Supersonic

The proposed breakable-box extension should add only each caliber's **Default** ammo SID. Variant distribution is a separate gameplay dimension and should not be mixed into the caliber graph.

## Recommended 9-axis distribution

Player-facing axes:

1. 9×18 mm
2. 9×19 mm
3. .45 ACP
4. 5.45×39 mm
5. 5.56×45 mm
6. 9×39 mm
7. 7.62×51 mm
8. 7.62×54 mm
9. 12×70 Buckshot

Suggested internal pool membership:

### Eastern pool

- 9×18 mm
- 9×19 mm
- 5.45×39 mm
- 9×39 mm
- 7.62×54 mm
- 12×70 Buckshot

### Western pool

- .45 ACP
- 9×19 mm
- 5.56×45 mm
- 7.62×51 mm
- 12×70 Buckshot

This preserves the existing two-pool topology and adds only proven default ammo types to the appropriate family. The player-facing UI still must not expose the internal pool names.

## Vanilla semantics for newly added calibers

There is one design issue we must handle explicitly:

The three missing calibers have **no authored Weight in the current destructible ammo pools**, so they do not have a true vanilla `1x` weight there.

Therefore, if we add them, **0x must be their actual vanilla state**.

The existing six calibers can keep:

- `1x` = vanilla authored `Weight = 2`

The three new calibers should use:

- `0x` = vanilla / absent from breakable ammo boxes
- values above `0x` = opt-in custom addition to that pool

This is more accurate than pretending `1x` is vanilla for content that vanilla does not include.

## Recommendation

Expand the radar to 9 axes, but visually distinguish:

- **vanilla box calibers** — default point at `1x`
- **additional proven calibers** — default point at `0x`

That preserves honest vanilla semantics while still allowing a complete regular-caliber distribution.

Status: **audit-only**. No new ammo calibers are added to breakable boxes in v0.9.26.44.

## Production decision after audit

The wider caliber audit is **not** used to expand breakable ammo-box `PossibleItems`.

Final production rule: **preserve the vanilla PossibleItems membership exactly** and group only the six existing box ammo SIDs into four player-facing families:

- 9×18 / 9×19 mm -> `A918D` + `A919D`
- 5.45 / 5.56 mm -> `A545D` + `A556D`
- .45 ACP -> `A045D`
- 12 Gauge -> `A012D`

The audited 9×39 and 7.62 calibers remain outside Breakable Box Ammo Distribution even though they are legitimate game ammunition.
