# Trader Stock Aggregate Control Decision

**Baseline:** v0.2.29

## Verdict

`Trader Stock` is **NO UI — aggregate mixed semantics**. The audited trader domain does not expose one vanilla scalar that means total stock amount.

The domain combines independent mechanics:

- `Chance` — item/pool availability.
- `Weight` — selection weighting, not a generic probability.
- `MinCount` / `MaxCount` — quantity where those fields exist.
- durability and attached-ammo fields — payload state for equipment.
- `PlayerRank` — progression/unlock gating.
- TradePrototype refresh — a separate system from ItemGenerator stock rows.

Therefore no writer or UI control may scale the complete trader domain as one percentage. Future trader controls must be subdomain-specific and prove their own homogeneous field semantics and consumer boundary.

## Current subdomain disposition

| Subdomain | Decision |
| --- | --- |
| Medic consumable quantity | `STRUCTURALLY_READY_RUNTIME_PROOF` — 48 direct `MinCount/MaxCount` rows; preserve Chance and PlayerRank |
| Ammo | separate audit required before UI; 52 trader-only direct ammo rows are mapped, but one Soviet AVOG row has no count pair and NATO ammo has cross-context definition use |
| Weapons | **CLOSED NO UI**; 94 direct weapon rows / 11 roots mix item Chance, parent-pool Chance, Weight, optional counts, durability/ammo payload and PlayerRank. See `TRADER-WEAPON-STOCK-AUDIT.md`. |
| Attachments | **CLOSED NO UI**; 37 direct rows / 11 roots, mixed 1/1, 1/2 and no-count models. Shared T2–T4 pools are clean Chance-based trader-only subsets but not global stock. See `TRADER-ATTACHMENT-STOCK-AUDIT.md`. |
| General consumables | target definition is cross-context; trader-only behavior requires source-edge isolation or clone/rewire |
| Bartender stock | target definition is cross-context; do not globally edit the shared bartender pool |
| Armor / helmets / detectors | explicit trader-local rows only; category names are not sufficient selectors |

## Guardrail

`economy.traderStock` must remain `NO_UI_AGGREGATE_MIXED_SEMANTICS`. A future implementation must introduce a new, narrowly named subdomain control rather than reusing the aggregate ID.

## Remaining-domain closure

The remaining stock families were audited in `TRADER-REMAINING-STOCK-AUDIT.md`.

- General trader consumables: no UI — the clean count pool `Trader_Cosnsumables_ItemGenerator` is shared with quest/scripted contexts.
- Bartender universal consumables: no UI — cross-context with named human-NPC inventory consumers.
- Armor / artifacts / detectors / head gear: no UI — trader-specific mixed availability/count models.
- DLC1 CNPP bartender consumables and RC trader junk each have source-exact count surfaces, but they are intentionally kept out of the normal UI because they affect only one special stock model.

Normal Trader Stock UI remains limited to the already accepted broad quantity surfaces.

