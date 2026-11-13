# 2.1 Trader SELL source graph extraction — 2026-11-13

Input: uploaded `STALKER2-2.1 cfg dump.zip` (nested base + DLC1 archives).

A standalone extraction of `TradePrototypes.cfg` and `ItemGeneratorPrototypes.cfg` produced **3,150 reachable item-leaf occurrences** across **92 trade definitions**. Every followed generator reference resolved, with no cycles. The CSV records trade scope, trade SID, root, leaf generator scope/SID, four-part entry path, original EItemGenerationCategory, item SID, chance, weight, nested chain and optional child reference.

Source extraction script: `tools/extract_trader_21_graph.py`. The complete CSV was exported as conversation artifact `STALKER2_2.1_trader_offer_graph.csv` (SHA256 `ae96c393b27afc2c63bee7c5ae42940962c26d2da46bcc210ef2b6e7e02b1e71`). It is **not** yet an embedded app manifest.

Important discrepancies to resolve before replacing the production 2.0.5 CSV:

- 2.1 extraction yielded 3,150 reachable item-leaf occurrences versus 2,939 rows in the frozen 2.0.5 SELL resource. This is not a controlled apples-to-apples comparison until category exclusions, duplicates, source ranks and generator inheritance are reconciled.
- The standalone parser enumerates **3,452** top-level generator nodes, whereas an earlier SID-keyed audit counted **3,449**; investigate duplicate SIDs, anonymous nodes and cross-scope inheritance before asserting an authoritative generator census.
- The CSV preserves raw `EItemGenerationCategory`, **not** the UI's ten human categories. `Consumable` requires item-SID classification into food, drink and medical; other category mappings must be verified.
- The current writer expects exact source paths, nested-link coordinates, strict hashes and per-trade private clone semantics. Those invariants need a 2.1-specific test suite before changing the embedded resource.

No runtime/UI writer changes were made in this extraction step. The current application remains on the 2.0.5 SELL snapshot. Do not claim 2.1 trader override functionality yet.

## Classification validation (follow-up)

The source graph contains 3,150 item occurrences across **90** profiles with reachable item leaves (92 total TradePrototype definitions, two with no reachable leaves). There are 235 unique item SIDs. Source enum `Consumable` contains medical, food, drinks **and grenades**. The source also assigns binoculars to multiple raw categories and uses `Junk` for weapons and armor. Therefore, a direct enum-to-UI mapping would incorrectly hide or remove stock. A strict audit script `tools/audit_trader_21_categories.py` classifies by SID first, with a fail-closed fallback. Validated occurrence counts: Weapons 581; Armor 212; Attachments 299; Ammo 558; Medical 683; Food 281; Drinks 387; Artifacts 93; Detectors 15; Grenades 18; Binoculars 23. These counts are occurrences, not unique items. The audit flags 23 binocular occurrences and 16 Junk occurrences requiring SID-specific handling. Binoculars remain outside the current ten-category UI until an explicit product decision. This script was run successfully against the uploaded graph CSV. Runtime writer migration remains unimplemented.

## Generator census correction (2026-11-13)

Recounted top-level `ItemGeneratorPrototypes.cfg` entries using balanced `struct.begin`/`struct.end` depth and the top-level `SID` field directly from the supplied 2.1 nested ZIPs:

| Scope | Top-level definitions | Duplicate SIDs within scope |
|---|---:|---:|
| base | 3,086 | 0 |
| dlc1 | 366 | 0 |
| **(scope, SID) total** | **3,452** | **0** |

The two scopes share only the sentinel SID `empty`; unscoped unique SID strings therefore number **3,451**. The earlier **3,449** count was erroneous and must not be used as an integrity assertion. These counts concern all item generators, not just reachable trader stock roots. Do not conflate the 3,452 total with the 83 stock-root SIDs.

Writer lookup must use `(scope, SID)` and explicit DLC-to-base fallback where applicable; it must never merge same-SID records across scopes. This resolves the census discrepancy, but does not yet validate 2.1 category removal patch paths or authorize switching the production 2.0.5 manifest.

## Reproducible source extraction tool

Added `tools/extract_trader_21_graph.py` to reproduce the 2.1 raw offer graph from the user-supplied nested ZIP without external dependencies. Usage:

```bash
python3 tools/extract_trader_21_graph.py 'STALKER2-2.1 cfg dump.zip' trader-21-graph.csv
python3 tools/audit_trader_21_categories.py trader-21-graph.csv
```

The original standalone extraction was independently rerun locally and its output SHA256 matched the prior 777 KB CSV exactly: `ae96c393b27afc2c63bee7c5ae42940962c26d2da46bcc210ef2b6e7e02b1e71`. Counts: 92 trade definitions, 3,452 scope-qualified generators, 3,150 leaf occurrences, no unresolved references or cycles. The checked-in script implements the same extraction logic; CI/build execution of the checked-in revision has not been claimed.

**Migration blocker:** This graph does not yet contain the writer's original nested `OuterLinks` (parent reference patch coordinates) or its complete source-exact 2.0.5 role/category schema. The existing writer uses those coordinates to clone and relink shared stock. Therefore do not simply replace the embedded `trader-offers-source-rows.csv` with the raw graph, or change hard-coded SHA/census assertions. A proper migration must enrich and validate parent links, source scope and category mapping before runtime activation.

## Parent-link reconstruction audit (2026-11-13)

The 2.1 raw graph was joined back to the exact parent `ItemGeneratorPrototypeSID` assignments in both source `ItemGeneratorPrototypes.cfg` files. Source artifact: `STALKER2_2.1_trader_graph_with_parent_links.csv`, SHA256 `2acee116549142a12e4cbd28f8d834e2513afa107397db76b1c6ab6e4400e8bc`.

- 3,150 leaf occurrences across 90 profiles with reachable items.
- 578 direct leaves (chain depth 1); 2,572 nested leaves (depth 2); **no deeper leaf chain** in this extraction.
- 2,550 nested leaf occurrences have one parent reference; **22** have **two parent reference slots** to the same child generator.
- **242** distinct parent-reference positions, keyed by trade scope, trade SID, root SID and slot coordinates.
- Parent slots are encoded in the audit export as `rootSid:ItemGenerator/[n]/PossibleItems/[n]:chance=...:weight=...`; multiple slots are delimited by `|` for analysis only. This is **not** the legacy writer's single-`OuterLinks` format and must not be passed to it unmodified.
- Example: `SuslovTraderSQ14_TradePrototype` has two root slots, `[5]` and `[6]`, referencing `Trader_T3_Ammo_ItemGenerator`. Both must be redirected to the same per-trade private clone.

**Writer migration requirement:** normalize each parent slot into its own validated row (or change the writer to consume an array); group by scoped trade profile + target generator; clone each target once; redirect **all** source parent slots, even if multiple point to the same child; remove selected leaf nodes only from that private clone. Preserve source scope and fail closed on missing/ambiguous references. The existing 2.0.5 writer expects `OuterLinks` in a single-slot shape, so this 2.1 audit alone does not constitute a safe runtime migration.

## Writer preparation: multi-parent references

The SELL writer now accepts either a single legacy `OuterLinks` coordinate or multiple `|`-separated coordinates. It splits and deduplicates parent slots before assigning every slot to the same per-trade cloned child generator. Existing 2.0.5 single-slot records remain valid. The 2.1 manifest is **not yet activated**: the embedded resource, strict source hash, profile census, category mappings and UI remain on the prior source. A static source audit is available at `tools/audit_trader_21_parent_links.py`. This is not a substitute for compiling and validating generated CFG patches.
