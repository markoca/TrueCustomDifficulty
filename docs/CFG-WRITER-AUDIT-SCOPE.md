# CFG writer audit — current acceptance scope

**Effective 2026-10-20.** This document supersedes earlier RC instructions that required Wine, GitHub Actions, WPF visual tests or gameplay testing for this **CFG writer audit**. Those activities may be useful for other work, but they are not acceptance gates here.

## What we check

The audit checks whether TrueCustomDifficulty **writes the intended CFG changes**: original source identity and selected difficulty/DLC scope, destination CFG and nested key, resulting value and operation, reference/inheritance, correct removal or generator addition, accurate file/field accounting, absence of duplicate or collateral edits, and protection of authored/quest/unique items. Review each reported defect against real source/prototype data before changing any gameplay parameter or UI control.

### Two different kinds of evidence

1. **Source-level CFG routing audit (Linux or Windows, no app runtime):**
   ```bash
   python3 tools/pre_release.py cfg-routes --output audit/pre-release/cfg-routes
   ```
   This runs the current 287-ID mapping, incremental CFG destinations, Stash Content/Extra Gear, World Shuffle field isolation and world physical ActorSID protection checks. It writes `summary.json`, `mapping/mapping.json`, `mapping/mapping.csv` and `incremental-stash-routes.csv`. **PASS here does not prove that the C# writer ran or produced actual patch files.**

2. **Actual generated-output checks (only when outputs from the Windows writer are available):** inspect the **staged** `GameData/*_patch_TrueCustomDifficulty.cfg` and singleton `*.cfg_patch_TrueCustomDifficulty` outputs, comparing emitted roots, paths, values, removals, clones, exact physical file counts and changed parameter receipts with the frozen original CFG source and selected preset. Windows-native .NET writer-contract tests can be used for this step, but do **not** require or ask for Wine, app GUI screenshots, game launch or GitHub Actions. Without emitted files or executed writer fixtures, report **NOT RUN**, not PASS.

## Development and handoff

All fixes must be committed with their relevant tests and documentation directly to `main`, with the push verified. Do not rewrite the UI control system or change Vanilla/Base values as a workaround for outdated test expectations. Preserve `docs/UI-DESIGN-LOCK.md` and `docs/VISUAL-CONTROL-SYSTEM.md`. Handoff commands should contain `git pull` and the relevant CFG-only audit/build command, **not a Wine/GUI/gameplay launch step** unless separately requested.

Old Wine results remain historical evidence of past checks but have no bearing on the newly narrowed CFG audit acceptance status. `./publish.sh` is optional to create the Windows app; publishing alone is not proof that a generated CFG is correct.


### 2026-10-20 — Source-audit failure triage
The first cfg-routes summary on 930677c0 had 2 PASS and 4 FAIL but did not include exceptions. The full source mapper relied on an obsolete exact-string match for the missing-output guard; the actual writer now excludes stored-only NPC ammo with `dormantSavedOnly`. The Stash Gear audit expected a prior MIX2 hash expression and disallowed the correct CSV field name in comments. Both checks now assert present-day source semantics. Stash Content has a new `--cfg-only` branch so UI dimensions no longer gate this scoped CFG audit. The general Stash Content audit is still the original strict UI check. On failure, `summary.json` now includes `failureExcerpt` for diagnosis, including the still-unexplained Incremental Delta failure. None of these changes alters the actual game CFG writer.

## 2026-10-20 — AllControls source accounting parity

The live writer baseline contains **287 distinct source IDs** (217 direct, 70 special; 1,238 direct CFG leaves). The actual `AllControlsPatchContract` drives **270 instantiated ViewModel source parameters**, excluding **17 serializable source IDs**: 13 pre-existing internal/audit-only settings and 4 retained but unmounted legacy IDs (`environment.diamondHealthDamage`, `environment.diamondBleeding`, `loot.standardStashChanceScale`, `loot.rareStashChanceScale`). The catalog still contains all 287 and the direct/special routing audit still examines them. The 270 VM probes are **not** the same thing as the 182 individually visible UI controls; other backing source parameters are edited through aggregate UI families.

The static `audit_final_cfg_mapping.py` receipt previously said `editableDimensions: 274`/`auditOnlyNonUi: 13`. It now reports **270/17** and requires its exact excluded-ID set to equal the executable all-controls test's `NonUi` set. This prevents the static mapper reporting four non-instantiated controls as executable writer probes. This is **audit accounting only**: no CFG values, writer dispatch, presets, UI controls or game data changed. Source inspection confirms the exclusion identities; the Python audit and native C# writer have **not** been executed in this GitHub-only review, so they are not marked PASS.


## 2026-10-20 — cfg-routes 3 FAIL / 3 PASS follow-up (commit `1be73dd`)

The user's Linux `cfg-routes/summary.json` from `1be73dd0388555407bf2908e4be0d68731938e54` reported three passing static checks (Stash Gear Actor/Chance, World Shuffle isolation, physical ActorSID isolation) and three failing checks. A successful application publish/build is **separate evidence**, not a CFG-route audit PASS.

- **Full mapper — `combat.psySpawnCycleDelay` (Immersive):** The source report constructed `by_id` with `direct[pid]` lookups on a `defaultdict(list)`. This silently inserted every *special-writer* source into the direct source map as an empty entry. Subsequent factory-preset scanning then falsely classified special Psy overrides as direct CFG leaves. Use non-mutating `get` while rendering source metadata, and require that the direct ID count remains 217.
- **Incremental delta — `survival.breadSatiety`:** The 39-source increment consists of **35 Stashes special routes plus 4 Food Satiety direct routes**. The CSV destination audit accidentally sent Food entries to the fallback Standard/Rare Tier Chance case and demanded an `ItemGeneratorPrototypes.cfg` output. Food is now a separate `TransformValue` / `GameData/EffectPrototypes.cfg` cohort with the catalog's `negateSet` operation and native `ValueMin/ValueMax` leaves.
- **Stash Content/Extra Gear — Attachment Weight:** The test assumed every Extra Gear class weight has reference 1x. The registered `loot.stashGearAttachmentWeight` deliberately starts at **0x**, while Weapon/Armor/Artifact are **1x**. The audit now checks these distinct source values explicitly.

These repairs change only audit tools and this documentation. **No production C# writer, parameter catalog, factory presets, gameplay defaults, UI controls or source CFG data are changed.** Source inspection and a separate static cross-check of all 12 factory profiles found no missing applicable direct leaves or inert changed direct values. The complete six-step `cfg-routes` command was **not executed in this connector-only environment**, so a fresh user `summary.json` is needed before calling the audit PASS.

## 2026-10-20 — Static CFG routing accepted; actual writer output is next

The user-supplied Linux `cfg-routes/summary.json` for `de68d51b00b1b116838044067e7927de6f21ceea` confirms **6 PASS / 0 FAIL**, including full mapping, incremental 35 Stashes + 4 Food Satiety routes, Stash Content/Gear routing, actor/Chance routing, World Shuffle field separation and physical actor isolation. This receipt closes the **static source-routing** phase only. Actual C# writer-produced BPatch/cfg_patch bytes and game behavior are **not** verified by that result.

**Next executable CFG-output regression:** `AllControlsPatchContract` already invokes the production `CfgPatchWriterService.Write` for 270 mounted source IDs, checking physical staged files and selected source output. Its dormant-NPC-ammo guard previously enumerated only `*.cfg` while comparing with the writer's all-file `FileCount`; the guard now counts all staged files. A new explicit writer fixture modifies `economy.startingMoney` to 100, requires the real `Stalker2/Content/GameLite/GameData/CoreVariables.cfg_patch_TrueCustomDifficulty` singleton file and `PlayerStartingMoney = 100` content, and compares physical output count to the production receipt. This guards against accidentally mistaking the `*.cfg` prototype filename convention for all CFG output.

On a local Linux machine **if** the optional .NET 10 + Wine writer harness is available, execute `./tests/run-wine.sh AllControlsPatchContract`; to run all 13 production writer contracts and obtain a JSON summary, use `python3 tools/pre_release.py writer --wine --output audit/pre-release/cfg-writer-output`. These commands execute writer contracts, **not the game or UI**. They are optional evidence for the next phase, not retroactive conditions on the 6/6 static routing PASS. On Windows, execute `dotnet run --project tests/AllControlsPatchContract/AllControlsPatchContract.csproj -c Release` instead.

Only `tests/AllControlsPatchContract/Program.cs` and this audit document changed; **no production writer, UI, gameplay defaults, source catalog or preset data** changed. The new C# test is marked **NOT RUN** until executed on a compatible Windows/Wine runtime.

## 2026-10-20 — Restore native 300 HP sleep default

Per user request, `survival.sleepHealthRestore` now starts/resets to **300 HP**, with the original reference unchanged and an editable **0–600 HP, 5-HP-step** range. Untouched projects no longer force a 100-HP SleepHealthRestore patch. Existing user presets and the source baseline are left unchanged. Updated the catalog, writer test expectations for Survival, Loot Special, RC Signal and AllControls, the static catalog check, rationale check, and both locked UI-design documents together. The supplied prior writer result at `9cba7b0` was 3 PASS / 10 FAIL; the revised contracts are **NOT RUN** until local execution. Unrelated Stamina label failures and coupling tests require independent follow-up.

## 2026-10-20 — Executable writer audit: 10/13 confirmed; three remaining assertions updated

The user's Linux/Wine `writer/summary.json` from `1dfbd0c3b95a88ab7b45ade3552898a8b53355e7` reports **10 PASS / 3 FAIL** out of 13 executed C# writer suites. Combat, Survival, Economy, Equipment, Environment, A-Life, World Removals, World Placement, AllControls (270 IDs), and RC Signal pass. The three failures occur at assertions about UI labels and aggregate source coupling, before the corresponding suites complete.

- **HealthStaminaPatchContract:** `Sprint Locks` and `Recovery Kicks In` are superseded labels. Both UI design locks and the parameter catalog authoritatively use **Sprint Stops Below** and **Boost Starts Below**. Update the test's expected labels without modifying either source ID, numeric limit, UI rendering, or CFG writer.
- **LootDirectPatchContract:** The old `Stash Reward Profile` anchor no longer exists. Rare Coupon Chance now highlights the current **Stash Content Mix** anchor, `loot.stashMixMedical`. Assert that aggregate highlight and verify that `loot.cheapStashAmmoQuantity` remains independently source-neutral, with no collateral ammo generator writes.
- **LootSpecialGeneratorsContract:** Editing the single authoritative Basic Ammo Quantity synchronizes the Standard and Rare persisted CFG source IDs in `MainViewModel`. In the explicit `all-stash-dlc` case, the test was incorrectly demanding those already synchronized values be vanilla immediately before applying the same requested value a second time. Accept only source values already equal to the exact coupled request; a conflicting target or unrelated modification still fails. The existing strict test of actual generated CFG field sets/values, protections, physical file totals and write receipts remains enabled.

These are **test-only corrections** with this audit scope documentation, in one commit on `main`. No source catalog, factory profile, UI/visual design, production C# writer or gameplay setting was changed. **New executable results are NOT RUN** until local Wine execution confirms the 13-suite outcome. The previous 6/6 static `cfg-routes` PASS remains independent.

## 2026-10-20 — Executable writer 12/13 follow-up: SmartLoot source-only accounting

The user-provided `writer/summary.json` at commit `d8f00fe5bdd88e4d8227ead73496ca3a77e4187f` confirms **12 PASS / 1 FAIL** for all 13 Windows/Wine writer contracts. All suites except `LootSpecialGeneratorsContract` pass. In the sole failing suite, the scenarios already completed 63 special generator/loot cases, 106 protected stash actors, native 300-HP zero-default output, and medical Weight/Chance independence. It stopped at the stale `smart-medkit-zero` check: `result.ParameterCount == 2`, inherited from when untouched sleep health still forced a separate 100-HP CFG edit.

**Updated source-only expectations:** five `VerifyWorldStashSmartLoot` scenarios now require exactly the single edited source, with independent validation of the entire live modified-ID set. The two `VerifyRegularStashDirectMedical` fixtures similarly expect one edited native source. The `VerifyDlcStashExpansion` receipts now assert the exact modified source IDs: one for each independent medical/Medium setting, or exactly three when the authoritative Basic Ammo Quantity slider synchronizes its Standard and Rare CFG backings. Full parsed BPatch leaf/value, protected-actor, redirection and physical output checks remain in force. No production C# writer, UI, presets, catalog, CFG source or gameplay setting is changed.

A new **13/13 PASS remains unverified** until running the complete Wine writer command again. Separate static `cfg-routes` was previously 6/6 PASS; game loading, UI and gameplay remain outside this automatic writer acceptance.

## 2026-10-20 — Executable writer audit 12/13: DLC Bandage native-field isolation

The user's Linux/Wine `writer/summary.json` for `b13162ffc27fa617917326c50e1028302234b74e` confirms **12 PASS / 1 FAIL**, with the only failed package `LootSpecialGeneratorsContract`. The latest completed checks include all five independent SmartLoot selection-zero cases, direct Cabin and ten ordinary medical row checks, 63 special cases, 106 protected stash actors and native 300 HP source-neutral initialization. The next test stopped at `VerifyDlcStashExpansion / bandage-zero` because it incorrectly required `GameData/StashPrototypes.cfg` for a **Chance-only** Bandage edit.

**Source-backed distinction:** the 991-row `smartloot-world-source-entries.csv` has 5 Bandage entries: they are **Weight-only**, with no native `Chance` column value. `loot.stashMedicalBandageChanceScale = 0` must change eligible actual Chance fields in the `CheapNoFresh` `ItemGeneratorPrototypes.cfg` but must **not** fabricate SmartLoot Weight edits or DLC1 `StashPrototypeSID` rewrites. The DLC Bandage scenario now enforces both positive native CheapNoFresh Chance emission and negative absence of a fictitious SmartLoot clone/redirect. Separately, `loot.stashMedkitMaxCount = -1` really disables Medkit SmartLoot and must continue to produce the clone and DLC actor redirects, now also checking its native Weight, MinCount and MaxCount all emit zero.

The future `GenericLootPreGame` Water-only NPC Ammo scenario also had a stale applied-count=1 expectation from the retired forced 100-HP sleep patch; it now requires **zero applied controls, CFG fields and files** while preserving the edited but dormant ammo multiplier in the UI/preset. These are contract/test/doc changes only; **production C# CFG writer, user UI, source catalog, presets, original CFG and gameplay values remain untouched**. Full **13/13 writer PASS is not yet established** until a fresh Wine execution reports it; 6/6 static routing was independently confirmed earlier.

## 2026-10-20 — Executable writer 12/13: native 0–1x Stash parity inputs

The Linux/Wine report at `9c8fefa2a8e95f80bb8cb23c58e33cd4c7b72794` confirms **12 PASS / 1 FAIL**. All earlier native SmartLoot, DLC1, actor-protection, direct-medical and special generator fixtures have passed, but `VerifyStashEffectiveParity` rejected `easy-bonus-composition: unchanged source loot.stashMixGrenades`. The old scenario supplied **1.5×** to a source-backed global category Weight slider now explicitly restricted to **0–1×**, so live `SnapCurrentNumberToMarker` clamps it to unchanged 1×. The same obsolete fixtures used 1.75× Coupons and 3× Food, including in the later `StashBranchParity` writer test.

Both production-output parity suites now exercise in-range **0.5×** category Weight variants, while retaining separate native Chance controls, category-specific medical weight biases, protected actor gates and byte-level parsed CFG comparisons. Before writer invocation, each fixture must verify the requested target lies inside the *actual live ViewModel slider bounds*, was reached exactly after snapping, changed its source, and produced **exactly** the intended modified source ID set. The pure model's food example now checks 17.77 baseline -> 11.27 with Food Weight 0.5× (the correct half-food value) rather than an unreachable 3× case. Native Chance-only fields remain provably unaffected by changing 0–1× category Weight.

Only `tests/LootSpecialGeneratorsContract/Program.cs`, `StashBranchParity.cs` and this document changed. No catalog, production C# writer, UI controls, gameplay source values, presets or safety gates were altered. **This commit has not yet been run through Wine; 13/13 PASS requires a fresh user summary.**

## 2026-10-20 — Executable CFG writer 12/13: Rare Coupons native Chance gate

The user-provided Linux/Wine `writer/summary.json` for `4e67c4d792bb8eb56e0cc2380eb20c0a187a15fa` confirms **12 PASS / 1 FAIL**; both previous Stash parity production scenarios now pass, each checking 532 ItemGenerator fields and emitted SmartLoot. The only suite still failing is `LootSpecialGeneratorsContract`, at `VerifyStashBranchBrowserParity`: the old cross-difficulty `Rare Coupons Chance` assertion expects **0.3** when examining the **Easy** source row with the **Hard** difficulty's numeric settings.

The authoritative `cfg-writer-baseline.json` `expensiveStashProduction.couponEntries` explicitly maps `[11]`/Easy–Medium to source **0.2f** and `[15]`/Hard–Stalker to **0.1f**. The independent `loot.expensiveStashMoneyChance = 0.6` override applies **only when the source row's difficulty SID matches** the configured `StashEffectiveState.DifficultySid`. Thus Easy row under Hard settings stays **0.2**, and Hard row under Easy settings stays **0.1**, rather than an invented 0.3. The separate `loot.stashMixCoupons` is **Weight-only** (0–1×), and must never alter these native **Chance** leaves. The test now asserts original source values, both matching overrides, both nonmatching source retentions, and coupon Weight=0 immunity.

Only the executable test `tests/LootSpecialGeneratorsContract/StashBranchParity.cs` and this scope document change. No production C# writer, catalog, UI control, preset, original CFG or gameplay default was changed. The updated branch test and full 13-suite writer result are **NOT RUN**, so 13/13 PASS is not yet confirmed by Wine.

## 2026-10-20 — Executable writer 12/13: generic Stash category Weight vs native Chance

The user-supplied `writer/summary.json` from `c3328ab037a24bbc48518b6827ad1831e6056494` confirms **12 PASS / 1 FAIL**. The remaining `LootSpecialGeneratorsContract` has now completed the special Stash parity, native branch preview/writer, DLC1 and NPC cases, but stops at `stash-medical-zero` in `GenericLootPreGame.CheckWorldStashSourceFields`: the test demanded a zero `Chance` entry merely because `loot.stashMixMedical` **Weight** was set to zero.

The production snapshot `StashEffectiveState.SelectionFactor` keeps native `Chance` independent of global category `Weight`. The original Cheap template contains mixed Medical/Food/Drinks `Weight` and `Chance` entries; Cheap Grenades are **Chance-only**, while Cheap Coupons have **Weight** entries and Rare Coupons have eight separate difficulty-gated `Chance` entries. SmartLoot categories also include chance-only entries not touched by global Weight (the source inventory has 991 item rows). The executable generic test now validates every matching native Cheap Weight goes to zero, every original Chance stays unchanged, all genuinely affected SmartLoot Weight and count fields go to zero, and all native Chance-only SmartLoot rows remain **absent from the emitted BPatch**. For Coupons it checks the original Cheap Weight entries and all eight untouched Rare Chance entries; it also requires no invented Coupon SmartLoot patch. The test remains grounded in the actual baseline manifest and parsed physical output and still rejects unrelated NPC/DLC/quest actor rewrites.

Only `tests/LootSpecialGeneratorsContract/GenericLootPreGame.cs` and this document were changed; the production C# writer, numeric catalog, native source CSVs, UI, presets and gameplay were **not modified**. The revised test remains **NOT RUN**, so 13/13 cannot be asserted until the user's next Wine run.

## 2026-10-20 — Writer build blocker: malformed native Stash paths

The uploaded `writer/summary.json` for `85fcaecbfcfcdd22e626b29f5707727557d22982` reports **12 PASS / 1 FAIL**. The only failing suite, `LootSpecialGeneratorsContract`, could not compile: three Roslyn `CS1026: ) expected` diagnostics at `GenericLootPreGame.cs` lines **358, 380, 404**. All three source paths in the preceding test-only Weight/Chance rewrite accidentally used the incomplete expression `group.GroupPath.Replace(".", "/["`, missing the closing `)` and conflating the group delimiter with the item index.

The three repaired paths follow the established native CFG path construction:
`group.GroupPath.Replace(".", "/") + "/[" + item.Index + "]/"` (and `... + "]/Chance"` for Rare Coupons). This is a **syntax repair**, preserving all native Weight/Chance leaf comparisons, protected actor assertions, and strict source-isolation tests. The code modification and this note are in the same commit; production writer, UI, catalog, factory/user presets, original CFG files and gameplay values are unchanged.

The new test has **not** been compiled or executed in this connector-only environment. The most recent confirmed executable result remains 12/13 PASS until a fresh Wine run.

## 2026-10-20 — Writer 12/13: World Weapon Shuffle / Extra Stash Gear mutual-exclusion contract

The user-provided Linux/Wine `writer/summary.json` for `8f8c92b94aa26a56cc711bf54d630abf198fc0bb` confirms **12 PASS / 1 FAIL**. The `LootSpecialGeneratorsContract` now passes all 13 generic NPC/Stash writer fixtures, including the native Weight/Chance isolation checks; its next check fails at `StashGearRemoval.VerifyExtraStashGearWeaponRemoval` because the test attempts to enable both `loot.stashGearEnabled` and `loot.worldWeaponShuffle` via the normal `MainViewModel` properties and then demands that the writer reject the double-ON state. The current UI contract deliberately prevents that state: **last toggle switched ON wins**, so the second setter automatically turns the first feature OFF.

The regression now tests *both* independent protections without weakening either: (1) normal UI toggles in **both directions** auto-disable the conflicting feature, leaving precisely one ON; (2) a malformed external/legacy dual-ON state is deliberately constructed inside the isolated test by setting only the private `ParameterViewModel._currentBoolean` backing field (reflection bypasses UI `PropertyChanged` handlers). The **actual production** `CfgPatchWriterService.Write` must then throw its existing `InvalidDataException` containing `cannot both be enabled`. A pre-existing staging sentinel must remain intact and no new files may appear, proving the unsafe combination is rejected **before staging changes**. This test does not circumvent or change the production UI normalization, build guard, original 13 approved removal leaves or protected weapon generators.

Only `tests/LootSpecialGeneratorsContract/StashGearRemoval.cs` and this audit scope were changed together; production writer, UI controls, catalogs, presets, original CFG source and gameplay values are untouched. **This new executable test has not run in the connector-only environment**; previous writer audit status remains 12/13 pending a fresh Wine report.

## 2026-10-20 — Executable 12/13: Stash Gear CFG line-ending parser on Wine

The user's Linux/Wine `writer/summary.json` for `ef4f361a9c620934c0ac1b79c5ed1076cd936bb0` confirms **12 PASS / 1 FAIL**; the `LootSpecialGeneratorsContract` now passes the Extra Stash Gear / World Weapon Shuffle mutual-exclusion test (UI last-toggle wins, and deliberately malformed dual-ON writer input is rejected before staging changes), 13 generic NPC/Stash tests, and the earlier real CFG parity checks. It stops at `StashGearCoverage.cs`'s "Default bonus generator must offer two different item categories" assertion.

**Cause found in test parser:** Production `PatchDocument.Render` uses `StringBuilder.AppendLine`, which under Windows/Wine emits CRLF. `StashGearCoverage` splits the emitted file at `'\\n'` and matches assignments such as `^\\s*Category = EItemGenerationCategory::(\\w+)$`, `^\\s*ItemPrototypeSID = (\\S+)$`, and `^\\s*Chance = 0\\.5$` using `RegexOptions.Multiline` / `(?m)`. The lingering CR prevents `$` from matching these lines. The test therefore miscounts otherwise present native category and item assignments. A separate strict parser in `StashGearNativeFields.cs` already explicitly normalizes CRLF.

This commit adds a dedicated **test-only** `ReadNormalizedStashGearCfg` helper to strip CR before all regex inspection of physical generated gear CFG output in both the ordinary coverage cases and the subsequent forced single-category/Chance cases. All original two-distinct-category, exact item/attachment count, rank gate, Chance, safe item, physical ActorSID and protected-stash assertions stay intact. The failed category-count assertion now reports its exact generator root and the observed item/reference/category counts to make future failures actionable. **The production-written CFG files are not modified**: this normalization applies only to the test's in-memory text. No writer, UI, catalog, presets or gameplay values were altered.

A new executed `13/13 PASS` is **not** yet verified; this revised test awaits a fresh user Wine report.
