# Trader Sells — 2.1-only migration contract

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
