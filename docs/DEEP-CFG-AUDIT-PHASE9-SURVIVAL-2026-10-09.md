# Deep CFG Audit — Phase 9: Survival

Date: 2026-10-09. This phase checks **production application bindings and
writer-generated BPatch text**, with original decoded GameData/DLC1 source
verification kept as an independent prerequisite.

## Source inventory and active binding

- Catalog: **54** `survival.*` definitions.
- Direct writer manifest: **139** source edit points, all currently
  declared under base-game CFG files; operations `set`, `negateSet`,
  `scale`.
- The application assembles live ViewModels from visible controls,
  slider-constraint backing members, and explicit grouped editor families.
  The one catalog item intentionally not instantiated is
  `survival.drunknessThresholdScale` (original source mapping exists but
  its standalone slider is hidden and absent from the active VM graph).
- That leaves **53 live numeric Survival parameters** in the
  UI/preset/writer graph. The source-only alcohol threshold should not
  be counted as operational app control; beer/vodka strengths and other
  drinking controls are separate actual editable values.

## Real C# writer checks

The Windows runner `tests/SurvivalPatchContract` directly constructs
`MainViewModel`, changes each live `ParameterViewModel`, and invokes
`CfgPatchWriterService.Write()`. It parses every resulting CFG to
verify the source prototype, nesting, `{bpatch}` flags, correct
`set` / `negateSet` / `scale` values, file location, counts and
absence of unrelated CFG fields. Six additional cases cover the
four game difficulties for radiation accumulation and bleeding
severity. Total **59 executable C# writer scenarios**.

The scenarios include hunger, bleeding, radiation, fatigue, sleep,
starvation (with its actual Master parent enablement), consumable
effect strength/duration and beer/vodka intoxication effects. Grouped
UI anchors that are yellow because a child changed must not add
unchanged BPatch fields or unrelated Master mechanics replacements.
The test also locks the exact one-item catalog vs app exclusion.

### Original decoded ZIP parity — independently executed

The supplied original decoded CFG archives were inspected directly on
**2026-10-09**. The 139 manifest source routes were compared with the
actual original file, prototype, nested node path, leaf key and
literal raw string, with **139/139 directly authored exact matches**,
**zero inherited source routes**, and **zero mismatches**.

- Base archive SHA-256:
  `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1`
- DLC1 archive SHA-256:
  `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6`

Permanent [Survival source audit evidence](evidence/SURVIVAL-ORIGINAL-CFG-2026-10-09.json)
records source identities, matching SHA fingerprints, all 54 parameter
source-point counts and explicit verification boundaries. The raw
decoded original archives were **not** committed to GitHub.

### Original decoded ZIP parity is a separate source gate

The successful original-source comparison above is independent of the writer manifest; a future game/CFG update must be rechecked against that release's source ZIPs.
Re-run the existing original CFG comparison with `--parameter-prefix
survival.`, including all **139** source fields, using matching
user-supplied decoded ZIPs:

```bash
python tools/audit_cfg_source_values.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2).zip" \
  --parameter-prefix survival. \
  --out audit/survival-source-values.json --strict
```

This gate is registered in `python tools/pre_release.py source`. Unlike
the writer-only Windows CI job, it does not run unless the matching
original CFG ZIPs are actually available on the operator's machine.

## Verification limits

The Windows writer suite verifies generated CFG text and active app
connections, **not** that S.T.A.L.K.E.R. 2 loads the patch or responds
to the values. In-game survival mechanics, starvation parent toggles,
consumable duration and effects, drinking thresholds, fatigue curves,
and any prototype inheritance are **NOT RUN** until tested on the
matching installed game. Existing `runtime_test_required` statuses
are not promoted by static proof.

## Pre-release invocation

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/
git pull
python tools/pre_release.py static
python tools/pre_release.py source \
  --base-zip "/path/to/STALKER2-GameData-CFG(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2).zip"
chmod +x publish.sh
./publish.sh
WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all \
  wine "publish/win-x64/TrueCustomDifficulty.exe"
```

Windows / .NET 10: `python tools/pre_release.py writer`, which runs
all three production writer contract suites. On Linux this check
remains `NOT RUN` and is supplied by the SHA-matched Windows CI jobs.
