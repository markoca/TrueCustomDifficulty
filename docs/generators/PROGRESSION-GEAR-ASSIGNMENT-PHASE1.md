# Progression-aware gear assignment — phase 1

Status: **offline assignment planner only**, not connected to the CFG writer or UI. No gameplay mutation.

## Confirmed inputs

- `Data/gear-progression-ranks.csv`: 34 evidence-backed regular weapons, four armor progression anchors.
- `Data/world-weapon-shuffle-manifest.csv`: 14 allowlisted regular generator weapon slots, each carrying a source progression tier and family.
- `Data/world-weapon-shuffle-pool.csv`: 36 legacy models, including `GunFora_ST` (no explicit trader-tier evidence) and `GunPKP_MG` (absent from the approved gear catalog). **These two are excluded from this planner**, not deleted from the existing production pool.

## First implementation

Run `python tools/plan_progression_gear_assignment.py --seed 42` to print a reproducible CSV preview. The planner uses only source-approved weapon items, selects destinations only from the 14 allowlisted SAFE_WORLD generator slots, matches **exact progression tier AND weapon family**, and never moves unique or quest rewards. It shuffles eligible *source weapon SIDs* across same-tier/same-family destination slots, preserving each source SID's multiplicity and never assigning an item outside its tier. It does not create or remove item copies.

If a bucket contains only one slot, it is left unchanged. A seed changes assignments only within buckets of at least two slots. Armor is intentionally not assigned: no independently validated armor destination allowlist exists. Regional rank is also not inferred from location names. The planner's source-slot tier is not evidence that the surrounding region or every stash has that rank.

## Next production gates

1. Verify each destination's physical actor/consumer relation and preserve all authored item recipes (attachments, upgrades, ammo, counts) when moving equipment; never change a SID in place while retaining incompatible weapon-specific children.
2. Establish a separate safe armor destination manifest.
3. Establish the broader safe physical stash inventory and rank evidence before assigning gear to previously empty or different-content stash slots.
4. Only after the above, wire a distinct opt-in control, CFG writer and regression audits. The existing World Weapon Density behavior remains unchanged.
