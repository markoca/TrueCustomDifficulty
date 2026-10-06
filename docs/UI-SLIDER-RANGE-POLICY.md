# UI Slider Range Policy — v0.2.0

This release deliberately uses a conservative range-expansion policy. It is rebased from v0.1.99 and replaces the discarded broad-range v0.2.0 experiment.

A slider receives extra upper-range headroom only when its previous maximum was equal to, or very close to, an existing vanilla value. Existing 2x ranges are otherwise retained. Natural caps such as probabilities and protection weights are unchanged.

## Expanded controls

| Control | v0.1.99 max | v0.2.0 max | Rationale |
|---|---:|---:|---|
| PSY Phantom Count | 200% | 300% | Master already uses 200%, so the old cap provided no harder-than-Master headroom. |
| PSY Equipment Durability Damage | 200% | 300% | Master already uses 200%, so the old cap provided no harder-than-Master headroom. |
| Deep-Water Stamina Drain | 200% | 300% | Vanilla itself is 200%, so the old cap could not increase the effect at all. |
| Hostile Repair Price | 200% | 300% | Vanilla Hostile pricing is already 200%, so the old cap could not make it harsher. |
| Radiation Accumulation | 200% | 250% | Master is 185%; 250% gives modest extra headroom without an unnecessarily extreme range. |
| Global Anomaly Damage | 200% | 250% | Master is 175%; 250% gives extra headroom while individual anomaly damage controls remain capped at 2x. |

## Explicitly not expanded

Human/mutant direct damage, player weapon damage, health, healing, stamina pool/costs, generic economy scalars, item weights, armor durability, weapon wear, individual anomaly damage and other ordinary 2x controls keep their v0.1.99 ranges. The goal is useful headroom, not larger numbers for their own sake.
