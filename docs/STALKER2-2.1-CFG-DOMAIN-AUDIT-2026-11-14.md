# S.T.A.L.K.E.R. 2.1 — CFG domain migration audit (2026-11-14)

## Scope and reproducible evidence

Compared the user's original outer CFG archives:
- STALKER2-2.0 cfg dump.zip SHA-256: 2fc93b7ec3389ea274b48c76d1082ff512fcd237c0d3c2c1c1a035e9814f7e71
- STALKER2-2.1 cfg dump.zip SHA-256: d1f38f28effae06d5009cbf823e268e9fa2db42ab4fdd0f4de4a8b0ef08228a9

The 2026-10-17 separately uploaded GameData and DLC1 source archives are byte-identical to the inner archives of the user's 2.0 dump. Source-diff evidence is therefore tied to the previously used 2.0/2.0.5 material; it is not evidence of 2.1 gameplay patch loading.

Source diff: **603 → 605 CFG files; 22 existing files changed, two added, zero deleted.** Added: GameData/FastTravelDestinationPrototypes.cfg and GameData/FastTravelEconomyPrototypes.cfg. The changed CFGs touched by direct writer targets are only GameData/CoreVariables.cfg and DLCGameData/DLC1/ItemPrototypes.cfg.

On the current main baseline: **359 source IDs, 1,448 direct edit points, 25 special writer groups**. Exactly **35 direct leaves** are in the two changed files; checked individually in both original archives against current sourceRaw: **35/35 present and numerically unchanged**. The other **1,413** direct edit points target byte-identical CFG files. This supports direct-source structural compatibility relative to the previous dump, not runtime behavior or proof that every older baseline value was valid to begin with.

## Domain status

| Domain | Catalog IDs | Direct CFG leaves | 2.1 source delta | Migration verdict |
| --- | ---: | ---: | --- | --- |
| Combat / Mutants | 28 | 56 | Direct files unchanged; Psy special source was not independently re-extracted | Direct-compatible; legacy baseline |
| Survival | 68 | 153 | Direct files unchanged | Direct-compatible; legacy baseline |
| Health | 22 | 47 | Direct files unchanged | Direct-compatible; legacy baseline |
| Advanced mechanics | 5 | 0 | Special listMembership on unchanged DifficultyPrototypes.cfg | Compatible source surface; legacy baseline |
| Stamina | 23 | 26 | Six CoreVariables.cfg leaves verified 2.0 = 2.1 = sourceRaw | Direct-compatible; legacy baseline |
| Economy | 94 | 409 | Eight changed-file leaves verified. Trader Sells has a 2.1-pinned runtime manifest. Trader Buys and other specials are not fully re-extracted | **Trader Sells migrated**; rest direct-compatible, rebase pending |
| Equipment | 6 | 331 | Twenty-one DLC1 item-weight leaves verified 2.0 = 2.1 = sourceRaw | Direct-compatible; legacy baseline |
| Environment | 17 | 42 | Direct files unchanged | Direct-compatible; legacy baseline |
| Loot / Stashes / World | 77 | 320 | Direct ItemGeneratorPrototypes.cfg unchanged; SpawnActorPrototypes.cfg changed and special manifests require full source validation | **Special-writer certification pending** |
| A-Life | 19 | 64 | Direct ALifeDirectorScenarioPrototypes.cfg unchanged; new 2.1 streaming changes outside TCD-owned routes | Direct-compatible; new settings not migrated |

Total direct leaves: 1,448. Catalog and writer baseline still declare gameVersion **2.0.5**; changing the version label alone would be false certification.

## Changed actor and special-writer boundary

Source comparison of SpawnActorPrototypes.cfg:
- Base: 16 original actor roots removed, 205 added; 87 surviving roots change one of the watched loot-related fields (the newly authored EnableSmartLootIfPossible flag).
- DLC1: eight original roots removed, six added; no watched-field changes in surviving roots.
- Focused check against 17 current production/stash/world actor CSV inventories found no overlap with the 24 removed actor roots. A 16-manifest pass likewise found no overlap with the 87 changed base actor roots. **These focused checks are not an exhaustive 46-CSV source registry audit** and do not prove that all special routes are unaffected.
- Actor changes need isolated verification of all impacted stash, crate, SmartLoot, world placement, NPC-loot and protection branches.

Other 2.1 additions:
- Roaming-guide fees use the new FastTravelEconomyPrototypes.cfg, handled conditionally by the existing Guide Travel Fees slider on installations where the source exists. The two hub/roaming systems need in-game regression.
- New streaming/A-Life variables are not automatically equivalent to existing NPC count or A-Life director controls.
- The full 2.1 Trader Sells runtime was activated separately and SHA-256 pinned (2939 rows, 86 playable profiles, 31 category parameter IDs). Its baseline special manifest pointer now matches runtime. The legacy Trader Buys manifest is still separate; do not call it 2.1-re-extracted simply because TradePrototypes.cfg is byte-identical.

## Gates before global 2.1 rebase / deprecating 2.0.5

1. Run the original-ZIP source mode against the active repository to cover all manifest actor roots and the 35 changed-file direct points: 
   ~~~bash
   python3 tools/audit_21_domain_migration.py --old "/path/STALKER2-2.0 cfg dump.zip" --new "/path/STALKER2-2.1 cfg dump.zip" --json-out audit/21-domain-migration.json
   ~~~
2. Re-extract / reconcile every special writer with the 2.1 original source, especially World, Stashes, SmartLoot/NPC loot, Trader Buys and producer graph references; reject missing roots, source-value drift and ambiguous DLC scope.
3. Only after every direct and special route passes, version the catalog, baseline, source labels, generator manifests and test contracts together. Do not bump just the gameVersion fields.
4. Confirm emitted BPatch and gameplay in S.T.A.L.K.E.R. 2.1, including DLC1, protected quest actors, trader buy/sell isolation, guide fees and changed SmartLoot actors. .NET writer tests are not game acceptance.

## CI and release evidence

The last inspected GitHub Actions run before this report (commit 0a906e6) had successful Windows Economy, Equipment, Combat, Survival, Health-Stamina, Environment, A-Life, Loot and AllControls writer jobs, plus successful Linux cross-build. The overall workflow was **red**, due to a separate existing Stashes A+C static UI-layout assertion in tools/audit_stashes_mixer_ui.py. Its success cannot be inferred from unrelated writer jobs.

tools/audit_21_domain_migration.py is an offline inventory guard by default, **not proof of 2.1 certification**. Supplying both source ZIPs enables the existing 2.1 CFG/manifest compatibility scanner. Full runtime and game QA remain outstanding.

## Trader Buys 2.1 source certification (2026-11-14)

The original base and DLC1 `TradePrototypes.cfg` files in the supplied 2.0 and 2.1 *outer* archives are **byte-identical**. Their 2.1 SHA-256 values are `c5115350d67d91a329b5922f0a51e9abe02f91075e5fbe24c1b3b4fa48f540eb` and `7a72b5dfc112226c92c081e49ca6c3877b7cf00cd541964d07113253d37e31a5`. A separate original-source parser reproduced all **86 BUY profiles**, **53 explicit native rejection lists**, and **33 non-authored lists**. Canonical source-to-manifest SHA-256: `51a68cb8ae6de4e15157fba5e2414241bc1954ac92e80fc23dca08eb403fed5f`; the 86 BUY roles also match the 2.1 SELL roles.

The existing BUY manifest remains unchanged *because its actual source bytes are unchanged*, not because the old baseline version is presumed valid. `tools/audit_trader_buy_21_source.py` enforces manifest and role parity offline, and with `--source "STALKER2-2.1 cfg dump.zip"` rechecks the two original CFG files. Direct durability fields are physically authored for **64** profiles (including two 0.0f exceptions); the remaining **22** do not author those fields but the existing manifest assigns the legacy 0.4f reference. The native engine default for those 22 is **not** proven by this comparison. Buyer gameplay acceptance remains unverified. Trader Buys is now verified as **2.1 source-equivalent**; this does not certify the rest of Economy or the global 2.1 baseline.

## Full original-ZIP actor delta vs all production manifests (2026-11-14)

Recomputed the native base and DLC1 SpawnActor roots from the supplied original 2.0 and 2.1 outer ZIPs (24 removed, 87 watched-field-changed, 211 newly introduced roots) and searched all **46** current embedded production CSVs for exact removed/changed actor GUID references. Result: **zero intersections**. `tools/audit_game_21_actor_manifests.py --old "/path/STALKER2-2.0 cfg dump.zip" --new "/path/STALKER2-2.1 cfg dump.zip"` reproduces this source check. It intentionally requires both archives and is not part of no-source CI, to prevent treating an archive-free check as real evidence. New-actor incorporation and behavioral tests remain pending.

## 2.1 added-actor classification (2026-11-14)

Re-extracted the exact 2.1 base/DLC1 `SpawnActorPrototypes.cfg` roots, using original 2.0 dumps for set subtraction. All **211** newly introduced roots are accounted for. Base (205): 132 FastTravelSecondaryPoint, 63 FastTravelPoint, three Trigger, three Snork Obj, two ContextualAction, one ElectroAnomaly and one Squad. DLC1 (six): three Trigger, two ContextualAction, one FastTravelPoint. Combined: **196 fast-travel-only roots**, and **15 quest/non-loot roots**. No newly added actor authors nonempty `StashPrototypeSID` or `ItemGeneratorSettings`. Four base quest Snork/Squad roots in `WorldMap_WP/E11_MQ01_LogicLevel_WP` have `EnableSmartLootIfPossible = true`: preserve quest-specific isolation; do not automatically add these to regular NPC loot manifests. No production stash/loot manifests need augmentation merely to account for these new actors.

The auditable extractor is `tools/audit_21_new_actor_scope.py`. Example:

```bash
python3 tools/audit_21_new_actor_scope.py --old "STALKER2-2.0 cfg dump.zip" --new "STALKER2-2.1 cfg dump.zip" --out audit/21-new-actor-scope.csv
```

It pins the actual decoded 2.1 actor CFG SHA-256 and all newly introduced root IDs, validates type census and each source field, and rejects any new nonempty stash/generator field. The generated CSV is evidence, not an embedded runtime source. This is original-source mapping evidence, **not** gameplay confirmation; 2.1 Fast Travel and quest NPC loot behavior remain untested.

The added-actor scope audit is source-only and uses both original archives; it must fail if new actor census, scope or stash/generator fields change. The separate Stashes Medical static readout assertion was updated to track the live source expression (no gameplay change).

The 2.1 new-actor extractor now has an offline parser/quest-isolation regression in GitHub Actions (`tests/test_game_21_new_actor_scope.py`). It checks non-loot travel classification, rejects unreviewed stash assignments, isolates quest Snork SmartLoot, and parses only top-level actor fields. CI has no original source ZIPs: the 211-actor full census is deliberately a separate original-archive check, not a synthetic PASS.

## Special CFG source-family identity gate (2026-11-14)

Direct original-archive comparison confirms **11 source CFGs** used by the generator, trader, stash, difficulty, A-Life and other special writer families are **byte-identical** between the supplied 2.0 and 2.1 dumps. Both ItemGeneratorPrototypes sources (base and DLC1), both TradePrototypes sources, StashPrototypes, DifficultyPrototypes, FastTravelPrototypes, CorpseClueStashPrototypes, ALife Director, ObjPrototypes and the base ItemPrototypes are identical. The DLC1 ItemPrototypes source *changed* and is intentionally excluded from this equivalence claim (its 21 changed-file direct leaves were compared separately). Base and DLC1 SpawnActorPrototypes also changed; actor-root checks and new-actor classification are separate evidence.

The new `tools/audit_21_special_source_families.py` pins SHA-256 values for these 11 source files and refuses to report source PASS without BOTH original archives. Run:

```bash
python3 tools/audit_21_special_source_families.py --old "/path/STALKER2-2.0 cfg dump.zip" --new "/path/STALKER2-2.1 cfg dump.zip" --json-out audit/21-special-source-families.json
```

CI runs `--inventory-only`, which explicitly reports `sourceStatus=NOT_RUN`, and cannot be mistaken for original archive verification. These checks establish CFG byte compatibility, **not** that every special writer's 2.1 item-generator link, SmartLoot semantic, game load order, or modified runtime output has been validated. Global version metadata remains 2.0.5.
