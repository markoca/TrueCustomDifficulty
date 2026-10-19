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
