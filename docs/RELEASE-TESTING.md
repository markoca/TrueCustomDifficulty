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
| C1 | `dotnet run --project tests/CombatPatchContract/CombatPatchContract.csproj -c Release -- --out audit/combat-patch-fixtures` | Windows / .NET 10 | 82 **real production writer** Master/Novice/Stalker/Veteran/PSY BPatch scenarios |
| B5 | `python tools/audit_health_stamina_source.py --base-zip BASE --dlc1-zip DLC1` | Linux / Windows | 69 Health/Stamina original source fields, including Player→[0] inheritance |
| C2 | `dotnet run --project tests/HealthStaminaPatchContract/HealthStaminaPatchContract.csproj -c Release -- --out audit/health-stamina-patch-fixtures` | Windows / .NET 10 | 46 writer Health/Stamina BPatch writer cases |
| B6 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix survival. --out REPORT.json --strict` | Linux / Windows | All 139 Survival original CFG source points (requires matching original ZIPs) |
| C3 | `dotnet run --project tests/SurvivalPatchContract/SurvivalPatchContract.csproj -c Release -- --out audit/survival-patch-fixtures` | Windows / .NET 10 | 59 actual app-to-Survival-BPatch writer scenarios |
| D1 | `chmod +x publish.sh && ./publish.sh` | Linux / .NET 10 | Published executable and matching `BUILD-INFO.txt` source receipt |
| D2 | Wine UI + in-game manual test | Linux / game | Visual design lock, preset round-trip, PAK/UTOC/UCAS build, actual game effects |


**B4 — original Combat reference inheritance and special arrays:**
`python tools/audit_combat_source_structures.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json`.
Checks 68 Combat direct source routes, separately identifying two
verified Medium-to-Empty inherited source fields, all 14 Master
agents / 66 tags, and both PSY effects. Game BPatch integration
remains a manual acceptance gate. The source runner now executes
B1 through B4 with genuine base/DLC1 ZIPs.

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
# C1 (82 Combat cases) + C2 (46 Health/Stamina cases).
python tools/pre_release.py writer

# A1/A2/A3 + B1/B2/B3/B4/B5 + C1.
python tools/pre_release.py all --base-zip "C:\path\to\GameData-CFG.zip" --dlc1-zip "C:\path\to\DLC1-CFG.zip"
```

On **Linux**, `all` runs static + source but reports **INCOMPLETE**
(exit code **2**) because C1 and C2 require Windows WPF. Verify C1 using the
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
| B1–B5 original matching decoded archive evidence | NOT RUN |
| C1 Windows CI 82/82 + C2 46/46 writer at that SHA | NOT RUN |
| D1 publish and `BUILD-INFO.txt` SHA match | NOT RUN |
| D2 Wine UI and preset smoke | NOT RUN |
| D2 PAK/UTOC/UCAS build, mod load and in-game test | NOT RUN |
| Combat and other runtime-pending gameplay acceptance | NOT RUN |
| Decision: READY / BLOCKED / approved exception | BLOCKED |

Do not release merely because the automated checks are green. Retain
the evidence folder, completed checklist and any approved exceptions
together with the candidate commit SHA.

### Phase 5 runtime contract coverage

The Windows production-writer job is extended to **82 separate executable
scenarios**: 11 special-array PSY/mutant cases, 52 direct difficulty
control cases and 14 Mutant Health family cases (16 nested MaxHP fields).
The current bound ViewModels and real `CfgPatchWriterService` are used.
These scenarios prove output structure/value and no collateral edits,
not independent correctness of every vanilla source value or in-game load.
The original GameData/DLC1 ZIP checks remain an independent B1–B5 gate.

### Phase 7 runtime acceptance: inherited mutant section

The Windows writer job now tests **82 scenarios**, including five importable
JSON presets for Stalker/Medium (inherited from Empty) and Veteran/Hard
(directly authored). Use
[Combat runtime test kit](../tests/CombatRuntimeAcceptance/README.md)
for isolated in-game tests. Even a green 82/82 C# result is **not**
gameplay evidence: game loading, Unreal inheritance/merge behavior and
mod packaging remain manual gates.

### Phase 8 source and writer tests

The fifth archive source gate, `B5`, confirms all **69 original
Health/Stamina direct writer field paths** and explicitly reports
`stamina.vaultMaxHeight` as inherited from
`Player : struct.begin {refkey=[0]}`. The Windows `C2` writer job
runs **46 independent production ViewModel→BPatch scenarios**,
with separate generated CFG evidence. Both CI jobs must be green
on the **same release commit**. Never confuse original source
ancestry with in-game BPatch merging.

See [Phase 8 detailed audit](DEEP-CFG-AUDIT-PHASE8-HEALTH-STAMINA-2026-10-09.md).

The five catalog-only Stamina internal speed-state parameters are intentionally absent from the actual application ViewModel and are **not** counted among C2 active writer scenarios. See Phase 8 for their precise IDs.

C2 now includes five Master original-mechanics full-list replacement cases in addition to its 41 live Health/Stamina scalar/toggle tests. The five internal-only stamina CFG definitions remain non-VM and are not counted as writer inputs.

### Phase 9: Survival real-writer validation

The `survival-writer-windows` CI job executes the real WPF ViewModel and production CFG writer for **53 live Survival parameters + 6 difficulty variants (59 tests)**. All **139 direct source fields** are covered separately by strict original ZIP value comparison (B6) during `pre_release.py source`. One source-mapped alcohol threshold remains catalog-only, not an app control. Game loading/effects stay NOT RUN. See [Phase 9](DEEP-CFG-AUDIT-PHASE9-SURVIVAL-2026-10-09.md).

### Phase 9 original decoded Survival evidence (2026-10-09)

Using the matching uploaded original CFG ZIPs, all **139/139 Survival direct source field identities and literal raw values** matched, with no refkey inheritance required. The immutable source ZIP SHA-256 fingerprints, counts by all 54 IDs and limitations are recorded in [Survival source evidence](evidence/SURVIVAL-ORIGINAL-CFG-2026-10-09.json). The same source comparison is automatically repeated by the B6 gate when matching archives are provided to `pre_release.py source`. This is **not** a gameplay acceptance result.

### Phase 10 Economy (2026-10-09)

A strict original-source Economy prefix gate audits **409 direct CFG fields**, while Windows `economy-writer-windows` runs **34 real application-to-BPatch scenarios** including 68 base trader profile minimum-condition cases. Source ZIPs are required for the independent source gate, and installed-DLC plus game-load evidence remain NOT RUN. See [Phase 10](DEEP-CFG-AUDIT-PHASE10-ECONOMY-2026-10-09.md).
