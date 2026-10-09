# Pre-release test and evidence runbook

**Canonical checklist for True Custom Difficulty before each release candidate.**
Run on the **exact Git commit** to be published. A static PASS is not
gameplay verification. Do not issue v1.0 without explicit approval.

## Test inventory

| ID | Command / suite | Host | Verified scope |
| --- | --- | --- | --- |
| A1 | `python tools/audit_combat_contract.py` | Linux / Windows | 39 Combat settings, 68 direct routes, special-family mappings |
| A2 | `python tools/audit_ui_cfg_contract.py` | Linux / Windows | 244 catalog settings, 231 effective controls, writer ownership |
| A3 | `python tools/audit_project_consistency.py` | Linux / Windows | Live catalog, manifest and UI source consistency; historical `[NOT RUN]` is not PASS |
| B1 | `python tools/audit_cfg_dump_provenance.py --base-zip BASE --dlc1-zip DLC1` | Linux / Windows | Actual source CFG filenames and 16 Mutant Health MaxHP fields |
| B2 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json --strict` | Linux / Windows | Every 1,224 direct source/CFG field check (strict) |
| B3 | Same as B2 plus `--parameter-prefix combat.` | Linux / Windows | 68 direct Combat source checks (strict) |
| C1 | `dotnet run --project tests/CombatPatchContract/CombatPatchContract.csproj -c Release -- --out audit/combat-patch-fixtures` | Windows / .NET 10 | 77 **real production writer** Master/Novice/Stalker/Veteran/PSY BPatch scenarios |
| D1 | `chmod +x publish.sh && ./publish.sh` | Linux / .NET 10 | Published executable and matching `BUILD-INFO.txt` source receipt |
| D2 | Wine UI + in-game manual test | Linux / game | Visual design lock, preset round-trip, PAK/UTOC/UCAS build, actual game effects |

## Single repeatable runner

`tools/pre_release.py` invokes the registered subprocesses, preserves raw
logs, and saves `summary.json` under ignored
`audit/pre-release/<UTC>-<SHA>/` with exact Git SHA, command, platform and
per-step **PASS / FAIL / NOT RUN** statuses. Source runs additionally
save raw-value audit JSON; Windows runs save generated CFG fixtures.

```bash
# Inventory; does not execute any test.
python tools/pre_release.py list

# A1 / A2 / A3: standard Linux/Windows static check.
python tools/pre_release.py static

# B1 / B2 / B3: actual matching decoded CFG archives required.
python tools/pre_release.py source \
  --base-zip "/path/to/STALKER2-GameData-CFG(4).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(4).zip"
```

On **Windows + .NET 10**:

```powershell
# C1 — all 77 real writer cases.
python tools/pre_release.py writer

# A1/A2/A3 + B1/B2/B3 + C1.
python tools/pre_release.py all --base-zip "C:\path\to\GameData-CFG.zip" --dlc1-zip "C:\path\to\DLC1-CFG.zip"
```

On **Linux**, `all` runs static + source but reports **INCOMPLETE**
(exit code **2**) because C1 requires Windows WPF. Verify C1 using the
matching GitHub CI run. Exit codes: `0` requested automated tests passed;
`1` a test failed; `2` Windows-required coverage skipped.
**A zero exit code never indicates S.T.A.L.K.E.R. 2 gameplay acceptance.**

## GitHub CI at the exact release commit

[Static source and CFG contracts](https://github.com/markoca/TrueCustomDifficulty/actions/workflows/static-audit.yml)
executes A1–A3 on Ubuntu and C1 on Windows automatically for `main`,
pull requests and manual dispatch.

```bash
# Optional: gh CLI requires connected GitHub credentials.
gh run list --repo markoca/TrueCustomDifficulty --workflow static-audit.yml --limit 5
gh run view RUN_ID --repo markoca/TrueCustomDifficulty
```

Require both `audit` and `combat-writer-windows` jobs green for the
**intended SHA**, not some older commit. Save/download the CI artifact
`combat-patch-contract` containing generated CFG patches.

## Local Linux publish and manual runtime acceptance

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/

git pull
git rev-parse HEAD

python tools/pre_release.py static

chmod +x publish.sh
./publish.sh
cat publish/win-x64/BUILD-INFO.txt

WINEPREFIX="$HOME/.wine-s2" \
WINEDEBUG=-all \
wine "publish/win-x64/TrueCustomDifficulty.exe"
```

Verify UI against `docs/UI-DESIGN-LOCK.md` and
`docs/VISUAL-CONTROL-SYSTEM.md`: all tabs, values and reference markers,
changed-number highlighting, hit targets, resets, rationale, dependencies,
base presets and preset round-trips. With repak/retoc installed, build a
mod and check actual `.pak/.utoc/.ucas` output, installation and load order.
Run S.T.A.L.K.E.R. 2; neither the Python tests nor a successful publish
does this.

### Combat in-game matrix (manual, never automatically PASS)

Refer to [Combat Phase 3/4](DEEP-CFG-AUDIT-PHASE3-COMBAT-2026-10-09.md):

- [ ] Veteran Human Attack Frequency: source inverse cooldown and actual NPC cadence.
- [ ] Veteran Bloodsucker/Burer Health: distinct source variants and real HP.
- [ ] Master special cooldown: only selected family changes and ability cadence follows.
- [ ] Veteran special cooldown: inserted 14-agent array loads without side effects.
- [ ] Master/Veteran PSY: both effect SIDs, delay, count, bleed and durability tested.
- [ ] Legacy preset with hidden repeated-hit/PSY values: generated output is intentional.

The C1 tests only validate produced BPatch CFG text, **not Unreal Engine
merge semantics** or quest-specific side effects. Preserve
`runtime_test_required` until in-game behavior is verified.

## Release sign-off (copy and fill for every tag)

| Evidence | Status |
| --- | --- |
| Candidate SHA, 0.x version and `git status --short` | NOT RECORDED |
| A1–A3 `summary.json` static PASS at that SHA | NOT RUN |
| B1–B3 original matching decoded archive evidence | NOT RUN |
| C1 Windows CI 77/77 writer at that SHA | NOT RUN |
| D1 publish and `BUILD-INFO.txt` SHA match | NOT RUN |
| D2 Wine UI and preset smoke | NOT RUN |
| D2 PAK/UTOC/UCAS build, mod load and in-game test | NOT RUN |
| Combat and other runtime-pending gameplay acceptance | NOT RUN |
| Decision: READY / BLOCKED / approved exception | BLOCKED |

Do not release merely because the automated checks are green. Retain
the evidence folder, completed checklist and any approved exceptions
together with the candidate commit SHA.

### Phase 5 runtime contract coverage

The Windows production-writer job is extended to **77 separate executable
scenarios**: 11 special-array PSY/mutant cases, 52 direct difficulty
control cases and 14 Mutant Health family cases (16 nested MaxHP fields).
The current bound ViewModels and real `CfgPatchWriterService` are used.
These scenarios prove output structure/value and no collateral edits,
not independent correctness of every vanilla source value or in-game load.
The original GameData/DLC1 ZIP checks remain an independent B1–B3 gate.
