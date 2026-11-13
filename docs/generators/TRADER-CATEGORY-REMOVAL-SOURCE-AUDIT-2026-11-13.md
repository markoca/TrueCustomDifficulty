# Trader Category Removal — Pre-Implementation Source Audit (2026-11-13)

**Decision: conditional / NOT implementation-ready.** This is a classification and
consumer-boundary audit, **not** permission to add ON/OFF controls or to write
`removenode` edits. No CFG writer, UI, preset, gameplay parameter, or design-lock
catalog has been changed. All ten controls remain proposed.

## Evidence and limits

- Repository baseline: main at `07de448ad1ded10ade134a50cc1fdac1761b4978` (before this audit).
- Existing [Trader Stock Generator Deep Audit](TRADER-STOCK-AUDIT.md) and
  [Trader Stock Exact Local Source Entry Catalog](TRADER-ENTRY-CATALOG.md)
  are the inputs. They describe the previously extracted STALKER 2 base-game
  and DLC1 runtime CFG **2.0.5** snapshot, not a fresh extraction.
- The catalog contains physically attributed source rows (scope, exact
  generator SID, category-node index, entry index, item/generator target SID,
  original Chance/Weight/Count fields, rank, and native CFG line). It does
  **not** include the full original decoded `ItemGeneratorPrototypes.cfg` or
  `TradePrototypes.cfg` in this repository.
- The supporting consumer graph previously identified **96** trader-domain
  generator roots, **92** with resolved consumers, **4** orphan/review-only
  roots, **83** links to concrete `TradePrototype`s and **22** distinct nested
  target SIDs. A fresh upstream source ZIP/hash comparison was **NOT RUN**
  in this audit; no new original-CFG parity or runtime result is claimed.
- Reproducible *catalog* assertions: run
  `python3 tools/audit_trader_category_source.py`. Optional row ledger:
  `python3 tools/audit_trader_category_source.py --csv audit/trader-category-source.csv`.
  The ledger is a **candidate/review list**, never a writer allowlist.
  Full source and compiled writer acceptance still require separate tests.

## Exact 10-category classification of currently mapped source rows

There are **834** raw local source rows: **603 item rows + 231 nested links**.
The four orphan generator roots contribute **97** rows (93 item rows and
4 links) and are excluded. The remaining **737** rows contain **510**
direct item rows and **227** generator links.

The table counts **direct local item rows only**. Rows can represent rank
variants or entries from different traders and are not unique inventory
products. Nested generator edges may reference additional items and must
be handled separately. Therefore the figures are **not** promised
complete in-game removal coverage.

| UI category candidate | Direct rows | Distinct item SIDs | Roots | DLC1 direct rows | Source-safe interpretation |
| --- | ---: | ---: | ---: | ---: | --- |
| Weapons | 73 | 34 | 10 | 0 | Exact `Gun*` items, including 8 stored in native `Junk`; 34 rows belong to T1–T4 shared *trader-only* gun pools |
| Armor | 120 | 41 | 20 | 4 | Real suits/helmets incl. 8 in `Junk` and `DutyArmor_3_U1` / `DutyMask_1`; do not blanket-remove native `BodyArmor` |
| Attachments | 31 | 18 | 10 | 0 | Scopes, suppressors, launchers, and `GunUDP_MagIncreased`; 15 rows from trader-only T2–T4 pools |
| Ammo | 53 | 30 | 13 | 0 | Regular + launcher ammunition, including `AVOG` listed under native `Artifact`; 32 rows in trader-only T1–T4 |
| Medical | 67 | 6 | 14 | 2 | Bandage, Medkit, ArmyMedkit, EcoMedkit, AntiRad, Hercules booster |
| Food | 32 | 5 | 10 | 13 | Bread, Sausage, CannedFood, Milk, DLC1 MonolithDryRation |
| Drinks | 48 | 4 | 14 | 17 | Water, Beer, Vodka, Energetic |
| Artifacts | 24 | 13 | 8 | 0 | Only verified `CArtifact*` / `EArtifact*` / `GArtifact*` item SIDs |
| Detectors | 14 | 4 | 12 | 0 | Echo, Bear, Gilka, Veles |
| Grenades | 16 | 2 | 11 | 0 | Only throwable F1 and RGD-5; launcher rounds remain under Ammo |
| **Ten categories** | **478** | — | — | **36** | Candidate DIRECT rows, not full consumer closure |

**32 deliberately unassigned/protected direct rows:** 8 night-vision
`NVG_Gen1/2/3` rows (some located in `BodyArmor`), 21 `Binoculars_01/02/03`
rows (spread across `BodyArmor`, `Artifact`, and `Attach`), and three
`empty` sentinel entries (RC trader). These cannot be silently deleted by
a ten-category switch. They could become *separate* Night Vision / Binoculars
controls only after an explicit scope decision. No unknown active item SID
remains in this catalog snapshot.

## Source-level design verdict by category

| Category | Source verdict | Requirement before implementation |
| --- | --- | --- |
| Weapons | **DIRECT-ROW CANDIDATE** | Filter by actual weapon SID across direct rows, T1–T4 stock, and native `Junk`; avoid entire `WeaponPrimary`/`Junk` category deletion |
| Armor | **DIRECT-ROW CANDIDATE** | Per-item removals only; preserve binoculars, NVG, `empty`, and quest/story-specific trader profiles until classified |
| Attachments | **DIRECT-ROW CANDIDATE** | Match actual attachments and extended magazine; preserve binoculars and isolated direct dealer offers |
| Ammo | **DIRECT + EDGE-REVIEW CANDIDATE** | Include individual ammo rows and all seller-side links, while NEVER editing shared `Trader_NatoAmmo_ItemGenerator` definition |
| Medical | **BLOCKED FOR COMPLETE GLOBAL OFF** | Direct trader-only rows are identifiable, but general shared consumable pool has mixed medical/food/drink entries; trader-only clone/rewire required for category-selective removal |
| Food | **BLOCKED FOR COMPLETE GLOBAL OFF** | `Trader_Cosnsumables_ItemGenerator` and `BartenderUniversal_TradeItemGenerator` definitions reach scripted/NPC consumers; cannot delete shared leaves |
| Drinks | **BLOCKED FOR COMPLETE GLOBAL OFF** | Same mixed shared-pool problem; medic/bartender drinks and DLC1 stock require item-by-item safety proof |
| Artifacts | **DIRECT-ROW CANDIDATE** | Use actual artifact SIDs, never every native `Artifact` row; spare grenades, binoculars and `AVOG` |
| Detectors | **DIRECT-ROW CANDIDATE** | Direct detector SIDs only; keep other gear and trader-local extras |
| Grenades | **DIRECT-ROW CANDIDATE** | Native `Artifact` contains F1/RGD-5 only in part; preserve normal artifacts and launcher ammo |

**Meaning of DIRECT-ROW CANDIDATE:** source targeting is *plausible* and
source-local. It is **not** an end-to-end proof that removing those nodes
on a running game suppresses every route, or that the resulting CFG/BPatch
load order and indexed removal semantics work. Do not present these
categories as implemented or guaranteed.

## Consumer boundaries — concrete shared-target hazards

The 231 total nested links include **29** seller links to
`Trader_Cosnsumables_ItemGenerator` (also used by `quest_scripted`);
**9** to `BartenderUniversal_TradeItemGenerator` (also used by named
human-NPC inventories); and **1** to
`Trader_NatoAmmo_ItemGenerator` (also used by `quest_scripted`).
These counts are asserted by the audit script.

- **Do not modify those shared generator definitions globally.**
  Directly changing a shared medical/food/drink leaf would alter
  non-trader consumers. Removing the whole trader-local link would also
  eliminate other allowed categories in the same pool.
- A category-specific OFF path would need **trader-only clone/rewire**
  (or a separately source-proven precise seller-local override) to
  preserve the vanilla non-merchant behavior. Verify every consumer
  of each original/new SID, the exact rewire path and the final BPatch.
- The NATO ammo pool consists of ammo-type content, so seller-side
  link suppression is a plausible category-wide OFF candidate;
  it still requires exact consumer closure and an in-game check.
- Additional cross-context targets
  `GeneralNPC_Consumables_Recon`, `GeneralNPC_Neutral_NVG` and
  `GeneralNPC_Neutral_WeaponPistol` appear only through the currently
  **unresolved** `HimzavodTrader_ItemGenerator` path in this catalog.
  Do **not** promote those links on name alone.
- `BartenderCNPP_TradeItemGenerator` includes a source-anomalous
  `Sausage` entry with an `empty` generator SID; preserve the node
  shape and classify its effective consumers before patching.

## Rank, specialization, DLC1 and refresh

- Keep `PlayerRank` gates unchanged. Removing one offering at a given
  rank is distinct from unlocking another weapon tier.
- Keep prices, reputation, `Chance`, `Weight`, `MinCount`,
  `MaxCount`, durability, attached ammo, and `TradePrototype` refresh
  unchanged in *category OFF* edits. Removing an item node, if safe, is
  not a stock amount/odds slider.
- The mapped local direct DLC1 items are limited to Armor (4), Medical
  (2), Food (13), and Drinks (17); DLC1 traders also reference base
  shared stock pools. No DLC1 direct weapon row does **not** mean DLC1
  trader weapon availability is absent.
- The original audit identifies 80 8-hour, one 1-day and two 7-day
  trade refresh conditions. Cached stock may survive a mod change until
  real regeneration. Test new and existing saves separately.
- Source node indices are **not** a proven safe batch-removal order;
  validate nested `removenode` structure, multiple deletions from one
  category, final leaf identity, and serialized CFG against original
  authoring on the matching game build. Never delete an entire
  category block containing mixed/protected SIDs.

## Required gates BEFORE any UI/control promotion

1. **Physical original CFG parity:** obtain the same-version decoded
   base + DLC1 originals; verify each selected generator/entry's
   native SID, rank, fields, container path, line and consumer identity.
   Existing catalog provenance alone is not a new original-source PASS.
2. **Full seller-only reachability:** resolve all concrete trade
   wrappers and cross-context links, explicitly classify quest,
   prologue, story and special traders, and prove either safe link
   isolation or cloning for mixed shared pools.
3. **Production BPatch contract:** test OFF/ON neutrality, correct
   source scopes, unaffected unrelated items/`empty`/NVG/binoculars,
   DLC1 gating, double removals within one pool, and no accidental
   count/chance/rank/refresh edits.
4. **In-game proof:** generated merchant stock after a real refresh
   under base-only and DLC1, older/newer saves and at least one
   special trader; verify no NPC inventory, quest rewards, or
   other loot was changed.
5. **Only then** add controls to `parameters.json`, relevant UI
   family and presets, keeping `UI-DESIGN-LOCK.md` and
   `VISUAL-CONTROL-SYSTEM.md` synchronized with actual controls.

**Current disposition:** source catalog has been classified for all
ten proposed categories. **No category has passed gates 1–4 in this
audit**, therefore **no category ON/OFF UI is approved yet**.
