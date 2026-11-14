## 2026-11-14 — CURRENT Generic NPC GEAR checkbox + conditional CHANCE slider

In **Economy → Trader Stock → Generic NPC**, render an additional **GEAR** checkbox after the existing vanilla stock category checkboxes, using the SAME compact 34-DIP row, right-aligned native checkbox and changed-only accent outline. When GEAR is **checked**, display one **CHANCE** slider row directly **beneath GEAR**, without separate board, instructions, a long label, redundant baseline/Vanilla numbers or forced full-width sizing. Hide (collapse, not disable-in-place) this slider row when GEAR is OFF. Use the established Stamina Usage-style **label left, slider center, single value right** reading `N%` (white until changed from the stored 2% default, then yellow). Slider is `1..100%`, 1 percentage-point ticks. The 30% trader-table / 70% knob-bank layout remains unchanged.

Unlike the existing five Generic NPC SELL category switches, GEAR is a **non-vanilla additive stock feature**. GEAR is OFF by default, CHANCE retains 2% even while hidden, and both are serialized as `genericNpcRareGearEnabled` / `genericNpcRareGearChance` in optional preset sidecars. They are real interactive controls bound two-way to `MainViewModel` and the CFG writer, with reset, save/load and old-presets defaulting safely to OFF/2%. They do **not** invent original-game source parameter IDs or alter the 359-source catalog, 31 SELL cells, the original vanilla checkbox behavior or factory preset values. On an enabled build, only the nine source-exact 2.1 Generic NPC TradePrototype stock clones receive the added rare branch at the selected native `Chance`; at **100%** the branch is configured to always roll for gameplay testing. When disabled, **no bonus stock output** is generated. A rare offer is one weighted item, with an optional independent 8%-chance second item; weapon condition is mostly 2–17% and rarely 42–68%. Corpse loot and NPC loadouts are unaffected. The earlier environment-only `TCD_GENERIC_NPC_RARE_STOCK` test switch is retired.

Contract and test plan: `docs/generators/GENERIC-NPC-RARE-TRADER-STOCK-21.md`, `tools/audit_generic_npc_rare_trader_21.py`, EconomyPatchContract and PresetSaveContract. Native runtime verification remains pending.

## 2026-11-14 — Generic NPC rare trader stock, **explicit test opt-in**

The 2.1 nine **Generic NPC trader** profiles can offer a small **trade-only** rare bonus through private `TCD_TS_...` stock generator clones. Do **not** edit NPC equipment, CorpseStashSID, Difficulty CorpseSmartLoot, quest/named traders, original shared generators, or the 31 existing Trader Stock checkbox controls. This is an experimental source-native **new stock** path, distinct from the SELL writer's existing category-removal path; the two merge on the same per-trader root clone.

Because stock generation/refresh and new-array BPatch runtime behavior still need in-game proof, this is **OFF by default**, with NO new UI control, parameter/preset, or catalog change yet. To test, launch the app with `TCD_GENERIC_NPC_RARE_STOCK=test` in its environment before pressing **Build Difficulty**. The first optional trader-stock branch is then forced to `Chance=1` for every one of the nine Generic NPC trade profiles; this does **not** make every candidate SID appear independently. One **weighted** item is selected, with an independent **8% second-slot** attempt using a disjoint pool, for **one, rarely two distinct bonus items** (pending game-runtime confirmation). The normal-but-still-opt-in mode `TCD_GENERIC_NPC_RARE_STOCK=1` has a first branch `Chance=0.02` (2%) and the same conditional 8% second slot (nominal 0.16% double-offer). An unset variable or `0` emits no rare-stock patch. Invalid settings fail closed. This experiment does not persist into normal app presets.

The primary selector weights total 100: broken primary weapons 58, broken pistols 16, rarer better-condition weapons 8, true anomaly artifacts 14 and attachments 4. Broken weapon condition is **2–17%**; rarer better guns are **42–68%**. The second bonus selector uses only *different* item SIDs: Battery artifact (50), Bear detector (25), and AKU weapon (25; 42–68%). The single-item pools use native `Weight`, `MinCount=1`, `MaxCount=1` and type-correct `EItemGenerationCategory`, rather than independent high-`Chance` entries for every SID. Exact source SIDs and scopes are recorded in `docs/generators/GENERIC-NPC-RARE-TRADER-STOCK-21.md`; static and real-writer contract tests check the nine private trade redirects and merged SELL category removal.

**Runtime acceptance still pending:** new BPatch category append under inherited roots, in-game individual-stock roll behavior, actual one/two item count, rank/refresh timing, weapon condition, final offer price, and whether different NPC actors sharing a TradePrototype resolve distinct stock. No corpse-drop relationship is assumed or requested.

# Trader Sells — 2.1-only migration contract

**Activated on main: 2026-11-14.** The SELL writer and item-offer UI now both consume only the SHA256-pinned 2.1 runtime manifest embedded as `Data/trader-sells-2.1-runtime.csv`. Legacy 2.0.5 SELL data remains solely as historical source evidence in `docs/generators/` and is not embedded. Existing 31 category parameter IDs and per-SID preset keys are preserved. The previous preparatory-status paragraphs below document the earlier migration stages, not the current runtime state. The 2.1 source-to-writer static regression is `tools/audit_trader_21_runtime.py`; Windows/Wine and in-game behavior require separate acceptance.

Decision: **S.T.A.L.K.E.R. 2.1 is the target and sole supported Trader Sells source**. The existing 2.0.5 inventory is deprecated and must be removed from runtime **only in the same change that activates a verified 2.1 replacement**. No silent fallback to 2.0.5.

## Migration gates

1. Reconcile all 92 2.1 TradePrototype definitions, including two profiles with no reachable stock leaves, against role assignments. Use the old 86-profile CSV only to identify role changes; do not treat it as an authoritative 2.1 source.
2. Verify the 3,150 2.1 stock leaf occurrences, 90 stocked profiles, source-scoped generator identity and 242 parent-reference positions. Explicitly handle 22 occurrences with two parent slots.
3. Resolve the 2.1 UI categories, including the 23 binocular occurrences (new UI category or explicit exclusion), and derive role availability from 2.1 rather than hard-coded 2.0.5 category unions.
4. Migrate the embedded resource and strict hash/census guard together; remove all executable 2.0.5 SELL source assumptions and version labels. Do not loosen validation to make the new CSV pass.
5. Verify clone/relink/removal operations in both GameData and DLC1, and prove an edited trade profile cannot mutate another trade profile's inventory.
6. Preserve preset identifiers and previously saved user settings where semantically compatible. Verify no-op and category OFF behavior. Update UI design catalogs in the same commit as any control changes.
7. Build, run static writer tests and inspect generated CFG patch output before declaring the migration complete.

## Reconciliation audit

Run `python3 tools/audit_trader_21_migration.py docs/generators/trader-buy-limits-86.csv STALKER2_2.1_trader_staging_manifest.csv`. This fails closed if 2.1 profiles with stock have no reviewed role assignment.

**Status:** migration contract and audit added; production runtime still uses the old source until all gates pass. Do not report 2.1 activation based on staging alone.

## Blocker verified: source root scope

The existing SELL writer infers the root generator file from direct leaves, or falls back to the trade profile scope. This is not sufficient for a source-exact 2.1 migration because a DLC trade can reference a base-game generator and a root can have only nested leaves. The 2.1 production resource must include an explicit `root_generator_scope` resolved against `ItemGeneratorPrototypes.cfg` definitions; reject ambiguous or missing roots. Do not replace the embedded 2.0.5 CSV with a partial 2.1 export. No 2.1 runtime activation is claimed here.

## 2.1 runtime manifest generator and activation gate

`tools/build_trader_21_runtime.py` now builds the intended 15-column writer resource from the 2.1 source archive, slot-enriched staging rows, and the 90-profile role reconciliation CSV. It verifies source generator scopes directly against `ItemGeneratorPrototypes.cfg`, including DLC trade profiles pointing at base generators, and fails closed on unknown or ambiguous root scopes, bad parent chains, missing role rows, incorrect profile counts, and wrong slot census.

Run:

```bash
python3 tools/build_trader_21_runtime.py 'STALKER2-2.1 cfg dump.zip' slots.csv docs/generators/trader-21-role-reconciliation.csv trader-sells-2.1-runtime.csv
```

**Activation blocker:** four extra profiles have unresolved role classification. The script intentionally refuses to emit a production-ready resource until they are reviewed. The existing app still embeds the old 2.0.5 resource. The runtime reader, resource embedding, output CFG tests, and removal of legacy census checks remain outstanding. No runtime migration or build success is claimed.

## Playable-trader scope decision

The 2.1 runtime manifest generator explicitly excludes the four source-only generic/template/test profiles (`AllTraderNPC`, `BaseTraderNPC_Template`, `BasicTrader`, `TradeTest`) rather than inventing merchant roles. They remain in the 90-profile source inventory, while the runtime output targets the 86 existing playable trade profiles. This deliberately excludes the seven `TradeTest` slot-1 leaves. The generator now uses an explicit zero-chance comparison instead of a Python chained comparison and checks that every expected exclusion is present. This does not yet change the embedded runtime resource or constitute full 2.1 activation.

## Source-scope writer guard

The SELL writer now carries `RootGeneratorScope` in its internal offer model and validates that every row for a trade slot agrees, that the scope is `base` or `dlc1`, and that any direct root leaves have the same scope. The legacy embedded CSV initializes the new field to empty and retains the old fallback. This makes the root-scope change backward compatible; the new field must be populated by the 2.1 loader before 2.1 activation. Static guard updated in `tools/audit_trader_sell_slots.py`.

## Dual-format reader prepared, activation pending (2026-11-13)

The SELL writer now recognizes a SHA256-pinned 15-column 2.1 runtime manifest alongside the existing 20-column 2.0.5 resource. The source-exact 2.1 playable manifest was generated locally from the actual 2.1 CFG archive: 2,939 rows, 86 playable profiles, 62 distinct leaf generators, 608 distinct leaf coordinates, 113 DLC generator rows, 6 zero-chance leaves. The four template/test profiles are excluded; all playable rows use slot 0 with `ConstTrue`. The generated file SHA256 is `9585c5b21bc13610dbc448650b2b053d582c852911782976076f5657f741e587`. Root generator scope is source-resolved and embedded per row.

**Important:** the 2.1 CSV is currently a local generated artifact, **not yet checked into GitHub**. The csproj still embeds the 2.0.5 CSV, so this is a preparatory reader change, not runtime migration. The resource must be added and switched in the same commit, followed by .NET compilation and CFG output tests. Do not claim completion until those steps are confirmed.


## CI UI contract reconciliation (2026-11-14)

The category-only Trader Stock editor intentionally does not bind `TraderOffersPolicy.Offers` as a visible individual-SID list. The 2.1 source still drives the saved `TraderItemOverrides` compatibility sidecar and the CFG writer. The UI-to-CFG audit now checks the active category editor binding (`TraderSellsPolicy.IsAvailable`) and preservation of the sidecar in the view model, rather than requiring the retired item-level UI. This is an audit-correction, not a gameplay or UI layout change.

The UI-contract script's source-root alias is explicit (`SRC`), preventing a `NameError` during the GitHub Actions audit.

Both legacy-named trader static checks now follow the category-only screen and assert view-model persistence of the saved item sidecar, without requiring the retired item-list XAML binding.

The full static CI chain also updates the independent Food Hunger Relief audit's stale UI census (184 visible parameters) and repairs its per-food preset-loop indentation; this changes checks only, not game values.

## Source registry reconciliation (2026-11-14)

The baseline special-writer metadata now points to TrueCustomDifficulty.Data.trader-sells-2.1-runtime.csv, matching the embedded production SELL writer. The legacy CSV under docs/generators stays historical only. This does NOT re-version the unrelated direct CFG or other special writer inventories; see docs/STALKER2-2.1-CFG-DOMAIN-AUDIT-2026-11-14.md.

## 2026-11-14: active C# source guard synchronized

`CfgPatchWriterService.ValidateBaseline` now requires `TrueCustomDifficulty.Data.trader-sells-2.1-runtime.csv` rather than the removed 2.0.5 CSV. Previous main had updated the JSON baseline without updating this guard, causing **all C# writer contracts to fail before patch generation**. This restores matching writer/source registry checks and does not change trader prices, category choices or generated CFG instructions.

## 2026-11-14 — CURRENT Trader Stock: technician role hidden from the stock editor

**Economy → Trader Stock has four player-facing role tabs:** General, Medic, Bartender, Generic NPC. Do **not** display a Technician tab or technician category checkboxes. In the 2.1 CFG, technician trade profiles and source stock paths exist, but these do not establish that ordinary repair/upgrade technicians expose a shop to the player. The rare selling technician is not justification for applying broad role-wide stock changes through the UI.

This is **UI filtering only**, not parameter deletion: all 31 original SELL IDs (including six technician SELL IDs), all 40 legacy BUY IDs, their preset values, DLC1 data, legacy compatibility sidecars and CFG writer routing remain intact. Existing customized technician settings are not silently discarded. Maintain the exact separation between `TraderSellsPolicy.Roles` (all five source roles for persistence/writer) and `TraderSellsPolicy.EditorRoles` (four visible tabs). No extra card, blank Technician placeholder, or default check highlight.

