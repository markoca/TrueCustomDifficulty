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
