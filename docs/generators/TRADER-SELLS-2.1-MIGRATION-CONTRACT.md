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
