# Deep CFG audit — Phase 3: Combat contract (2026-10-09)

Branch: `main`. Source of truth: live `parameters.json`, `cfg-writer-baseline.json`,
`CfgPatchWriterService.cs` and prior user-supplied GameData/DLC1 source-provenance
reports. **Static mapping verification is not an in-game gameplay test.**

## Current Combat inventory (supersedes historical v0.2.44 counts)

| Domain | Catalog dimensions | Direct CFG edit points | Evidence |
|---|---:|---:|---|
| Difficulty combat scalars/boolean (human, player, mutant global, repeated hits, explosion) | 13 | 52 (13 × 4 difficulties) | Catalog ↔ writer path, operator and vanilla value consistent |
| Mutant Health | 14 | 16 (Bloodsucker and Burer have two concrete variants each) | Exact nested `VitalParams.MaxHP`; all 16 previously checked against decoded vanilla source |
| Mutant ability cooldown | 8 | special writer | 14 agent SIDs from Master-only `AgentCooldownMultipliers`; runtime-test-required |
| PSY combat | 4 | special writer | Both `SpawnPSYPhantoms` and `SpawnPSYNPC`; runtime-test-required |
| **Total** | **39** | **68 direct** | **27 declared confirmed-source controls + 12 runtime-test-required controls** |

The 52 direct difficulty edit points and 16 health points have no competing
Combat direct owner and their declared catalog target paths match the writer
`nodePath + key`. The 52 `DifficultyPrototypes.cfg` sourceRaw literals agree
with the catalog's four vanilla markers; this is **internal parity**, not fresh
confirmation from the decoded ZIP files. All 16 concrete mutant HP targets
have `nodePath=["VitalParams"]`, `key="MaxHP"` and `operation="scale"`
following Phase 2.

Correct vanilla profile SID mapping: Novice = `Easy`, Stalker = `Medium`,
Veteran = `Hard`, Master = `Stalker`. General human/mutant attack frequency
uses `inverseScale` of authored attack *cooldown*. Special ability controls
use direct `CooldownMultiplier` values, **not** inverseScale. Repeated-hit
`Min/Max` remain raw curve weights, not percentages of final damage reduction.

## Special writer boundaries

- Master has existing agent and PSY override arrays. The current writer
  BPatch-updates only changed families/fields by Master source array index.
- Novice/Stalker/Veteran have no corresponding complete vanilla override
  arrays. Changing one special control causes the writer to emit a **full**
  14-agent cooldown array or two-entry PSY override array, with the other
  values derived from current control state. This is deterministic structurally
  but **unverified gameplay behavior**. The normal 1.0 entries may not be
  semantically identical to the original absence of the array.
- The source-derived cooldown mapping includes `Burer`/`Burer_ShootingSpecial`,
  Mist Bloodsucker and named Bayun/Controller variants. It includes
  `Bayun_E10_MQ01`, a story-labelled variant; this is a **negative-space
  review point**, not authorization to add story-specific controls or broaden
  quest edits. Also preserve the duplicate Burer `WeaponDrag` tag at
  distinct indices unless original CFG plus runtime evidence justifies a fix.
- The `Taking Repeated Hits` parameters are hidden from ordinary Combat
  UI but stay in preset/writer state by design. PSY controls are filtered out
  of the Combat tab view while still accepted from presets and the writer.
  A loaded old preset can therefore affect a control not currently exposed
  as an editable Combat row. Do not silently declare these inactive; review
  UI/preset policy separately before changing compatibility.

## Consistency audit runner regression (2026-10-09)

The whole-app `audit_project_consistency.py` initially raised a
`NameError` at the Special Ability Cooldowns radar check because its row
lookup referenced `_mhr_rows_by_id`. That map is created later in the
unrelated Medical Healing check. The cooldown section now initializes and
uses its own `_sac_rows_by_id` map before checking all eight parameter IDs.
This is a validator-only fix: gameplay mappings, UI and preset behavior are
unchanged. Run `python tools/audit_project_consistency.py` to verify the
complete audit in the local repository.

### Runner import and triage guard follow-up (2026-10-09)

The same whole-app validator then raised `NameError: _csv is not defined`
at the Blueprint Shuffle manifest gate, because its `csv as _csv` imports
were all inside earlier optional evidence branches. The CSV alias is now
imported unconditionally at module load, and the 11 redundant conditional
imports have been removed. This also covers later Trader, Generator Matrix,
Override Verification, and CFG Provenance CSV readers.

The triage rows used by the later Generator Decision Matrix are now
initialized independently of optional triage evidence. When that evidence
is missing, the validator records a clear `[FAIL]` rather than raising a
second `NameError`. These are audit-runner resilience changes only; no
production CFG or UI behavior changes. The complete validator still needs
local execution and any reported assertion failures must be assessed
independently.

### Retired AmmoDistributionRadar audit migration (2026-10-09)

The old whole-project validator attempted to read the deleted
`src/TrueCustomDifficulty/Controls/AmmoDistributionRadar.cs` directly,
raising `FileNotFoundError` before it could report normal assertions. The
active UI is now `AggregateVisualControl / AmmoDistribution`, with six source
parameters (Ammo Yield, Grenade Yield, four family weights), eight source-backed
conditional ammo count ranges, and separate SNG/NATO composition previews.

The retired `AmmoDistributionRadar` contract and its repeated historical
polar-legend/font/right-pane gates were replaced with guards against the
**current** aggregate, its CFG/source count model and all nine remaining
specialized class registrations. The old ten-radar text hitbox assumptions
are not silently treated as passes. Existing UI-DESIGN-LOCK and
VISUAL-CONTROL-SYSTEM remain authoritative; gameplay/UI sources were not
modified by this validator repair. The full project validator must still be
run locally and any reported `[FAIL]` is not considered resolved by this
migration.

### Whole-project validator modernization (2026-10-09)

The live `audit_ui_cfg_contract.py` audit passes with 244 catalog records,
231 editable dimensions, 1,224 direct edit points and zero direct collisions.
The legacy whole-project validator still included fixed assumptions from
v0.2.x (178 catalog parameters, CSV evidence since removed, the original
polar radar UIs, 242 weapon-attached NPC ammo edits). These must not be
misrepresented as current release tests. The validator now treats absent
archived snapshots as **HISTORICAL NOT RUN**, while checking embedded
production manifests and the source-backed live contracts separately.
No gameplay balances or existing factory preset numbers were altered by
this reconciliation. The headshot multiplier description and assembly
version metadata were synchronized to their existing runtime/catalog meaning.
Full local Python verification and in-game runtime tests remain separate.

## Automated reproductions

From the project root:

```bash
# Catalog-to-writer regression checks; needs no vanilla archives.
python tools/audit_combat_contract.py

# Original decoded CFG provenance (68 Combat direct edit points).
python tools/audit_cfg_source_values.py \
  --base-zip "$HOME/s2tools/cfg_dump/STALKER2-GameData-CFG.zip" \
  --dlc1-zip "$HOME/s2tools/cfg_dump/STALKER2-DLC1-CFG.zip" \
  --parameter-prefix combat. \
  --out audit/combat-source-values.json --strict

# Whole-app guards.
python tools/audit_ui_cfg_contract.py
python tools/audit_project_consistency.py
```

The new `--parameter-prefix` limits source resolution to Combat without
weakening the default whole-catalog check. Run with *actual* supplied ZIPs
(the originally uploaded files were named with `(4)` suffix; adjust paths
to local filenames). The full scoped 68-point archive comparison was **not
run in this GitHub-only audit environment**. Do not assert an unrun PASS.

## Phase 4: real writer-generated BPatch structure contracts (2026-10-09)

The production `WriteMutantCooldowns` previously used the *aggregate UI*
`IsModified` flag for the Bloodsucker anchor. Editing any of the other
seven cooldown families would make the unchanged Bloodsucker anchor appear
modified and emit its Master indices unnecessarily. The missing-parameter
gate could then reject that non-value change. Now the special-family
selection and final modified-support check use each parameter's
`IsValueModifiedFromReference()`. Aggregate UI coloring remains untouched.

The Windows contract harness additionally exposed the same aggregate
leak in `WritePsyOverrides`: changing Phantom Count made the
`PsyPhantomPressure` anchor (Spawn-Cycle Delay) look modified to the
production writer and generated an unnecessary `PSYDelayBeforeNewSpawnCycleModifier`
field on Master. PSY now also selects changed fields by independent
`IsValueModifiedFromReference()` checks. The shipped effect defaults and
control values are unchanged.

A standalone **Windows/.NET 10** self-contained console contract test project at
`tests/CombatPatchContract` runs the **real**
`CfgPatchWriterService.Write`, generates `DifficultyPrototypes.cfg`
patches, parses their nodes and assignments, and asserts:

- Unchanged Master/Veteran emit no patch.
- Master Chimera updates *only* agent index [1], not the Bloodsucker
  anchor indices [0]/[13]; Master Burer retains both variant indices
  and the duplicate WeaponDrag tag at its two distinct indices.
- Novice/Stalker/Veteran emit a complete indexed 14-agent array,
  preserving all vanilla canonical tags and neutral untouched families.
- Master PSY modifies exactly one scalar per each of the two existing
  effect indices; Veteran creates the complete two-entry/four-field
  replacement subtree.
- Combined mutant+PSY changes coexist under the same selected
  difficulty root with no unrelated writer records.
- The patch is rooted under the correct named prototype, with the
  expected `{bpatch}` versus full-subtree replacement flags and
  accurate `EditCount`/`ParameterCount`.

These tests verify **generated CFG text and writer behavior**, not
whether Unreal successfully merges BPatch arrays or the game uses every
override as expected. Those remain runtime-test-required; no gameplay
status is promoted by this phase.

The historical single-source-tree check explicitly excludes this one
standalone test project from the forbidden production mirror scan. All other
unexpected external C#/XAML/CSPROJ copies continue to fail.

Local Windows command:
```powershell
dotnet run --project tests/CombatPatchContract/CombatPatchContract.csproj -c Release -- --out audit/combat-patch-fixtures
```
On Linux, the static audits still run normally; the Windows CI job
exercises the .NET/WPF production code and saves its generated patches.


## Phase 5: live UI → direct Combat BPatch route coverage (2026-10-09)

The Windows contract harness now additionally traverses all **13 direct
difficulty-controlled Combat settings × 4 base difficulties = 52 cases**,
plus all **14 Mutant Health controls**, for **77 executable writer scenarios**
when combined with the previous 11 special cooldown/PSY scenarios.

Unlike static catalog/manifest equivalence, these cases instantiate the
actual `MainViewModel`, mutate the live `ParameterViewModel`, execute the
production `CfgPatchWriterService.Write()` and parse the generated CFGs.
They assert selected Base Preset SID, expected logical output filename,
correct `{bpatch}` nodes, actual field names/values, no unintended
prototype or field changes, and accurate EditCount/ParameterCount. The
expected direct field identities are pinned independently of the live
baseline; its authored original values are used for numeric transformations.

The source-backed field identity assertion also pins
`Weapon_JammingMultiplier` under `NPCCombatDifficulty`, not
`EnvironmentDifficulty`—these nested WPF value routes are checked directly
against emitted CFG, not guessed from their gameplay labels.

Particular edge cases: both `NPC_AttackCooldown` and
`Mutant_AttackCooldown` use reciprocal scaling; boolean repeated-hit
source records serialize actual toggle state; moving Novice's maximum
repeated-hit weight from 1.0 to 0.9 correctly cascades the linked minimum
to 0.9, adding the **second intended field**; all other direct cases
remain single-field writes. Mutant Health must use nested
`VitalParams.MaxHP` for every source-backed species and preserve both
Bloodsucker and Burer variant SIDs (16 concrete fields across 14 cases).

**Evidence boundary:** all 77 cases exercise live WPF ViewModels and
output text, not S.T.A.L.K.E.R. 2 itself. The exact authored vanilla
`sourceRaw` values still require independent original GameData/DLC1 ZIP
verification. Do not promote special-array/gameplay runtime support.

## Runtime acceptance scenarios before promotion

1. Veteran: change only `Human Attack Frequency` (e.g. 1.5×),
   confirm patch writes `NPC_AttackCooldown = 1/1.5` under
   `Hard.NPCCombatDifficulty`; compare attack cadence in-game.
2. Veteran: change only `Mutant Health` for Bloodsucker/Burer;
   inspect separate `VitalParams.MaxHP` patches for both variants,
   check values and health behavior in game.
3. Master: change one mutant family's cooldown; prove that the patch
   touches only that family's canonical indices and does not modify
   unrelated agent families or PSY records.
4. Veteran: change a mutant special cooldown; inspect the full
   14-entry non-Master insertion and test whether the chosen ability
   reuses faster/slower **without collateral ability changes**.
5. Master and Veteran: change one PSY field; verify both effect SIDs,
   test spawn cycle, count, bleeding and equipment damage independently.
6. Load a legacy preset containing nondefault hidden repeated-hit or
   PSY values; confirm the effective patch reflects intentional policy
   and that no invisible modifications evade review.

Keep catalog runtime flags unchanged until those in-game tests pass. No
new Combat UI control, designer lock change, gameplay range change or
v1.0 release is authorized by this mapping pass.


## Phase 6 — original Combat source CFG archive verification (2026-10-09)

Two user-supplied decoded source ZIPs were inspected:
- Base `STALKER2-GameData-CFG(2).zip`, SHA-256
  `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1`.
- DLC1 `STALKER2-DLC1-CFG(2).zip`, SHA-256
  `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6`.

All 68 Combat direct writer source paths were independently compared by
prototype, nested CFG path, leaf key and raw value. 66 are directly
authored exact matches. Two others are original-source inherited values:
Stalker player difficulty (CFG `Medium`) has no local
`MutantCombatDifficulty` node. The declaration
`Medium : struct.begin {refkey=Empty}` points to `Empty`, which
physically defines `Mutant_BaseDamage = 1.0` and
`Mutant_AttackCooldown = 1.0`. These correspond to the live
`combat.mutantDamage` and `combat.mutantAttackCooldown` routes.
They are therefore `inherited_exact`, not directly present.

Original Master (CFG `Stalker`) has 14 indexed
`AgentCooldownMultipliers` agents and 66 individual indexed
`CooldownTag` records. All original indexes, SIDs, tags, and their
0.5 multipliers match the writer special manifest, including both
Burer WeaponDrag tags at their separate indices. Two original PSY
effects match the writer manifest: SpawnPSYPhantoms and SpawnPSYNPC,
each with delay 0.5, count 2.0, bleeding true, durability 2.0.

New reproducible audit: `tools/audit_combat_source_structures.py`
checks original direct data, explicit refkey inheritance and Master
specials against the live manifest. The generic direct-source checker
now separates inherited_exact from exact, demands actual parent
declarations and values, rejects missing/invalid/cyclic parent chains,
and is regression-tested on CI. Source ZIPs are not committed.

IMPORTANT: original-source inheritance does NOT verify the Unreal
runtime application of a nested bpatch into the Medium prototype
(which has no direct MutantCombatDifficulty block). Master sparse
and non-Master inserted arrays likewise need in-game testing.
No existing CFG writer implementation, UI design, gameplay balance or
saved preset value was changed in this source audit.


## Phase 7 — inherited Medium real writer fixtures / controlled game run

Five importable, isolated preset JSON files have been added under
`tests/CombatRuntimeAcceptance/Presets/`. The same real production
`PresetService` and live `ParameterViewModel` used by the application
are now exercised by the Windows harness to generate and validate
the exact BPatch for:

1. Stalker vanilla (no edit → no CFG patch).
2. Stalker Mutant Hit Damage = 0.5x →
   `Medium.MutantCombatDifficulty.Mutant_BaseDamage=0.5`.
3. Stalker Mutant Attack Frequency = 1.5x →
   `Medium.MutantCombatDifficulty.Mutant_AttackCooldown=0.666667`.
4. Stalker combined → only those two assignments under one
   nested `MutantCombatDifficulty : struct.begin {bpatch}`.
5. Veteran control with the same values → target `Hard`,
   where the original mutant section is physically authored.

These **five new production writer scenarios** extend the existing 77
to **82**. The tests verify selected-game-difficulty → CFG SID
translation, importable serialized preset values, output structure,
field values, counts and no unrelated fields or mutant ability
arrays. The original Stalker `Medium` refkey and original Veteran
`Hard` source have already been independently verified in Phase 6.

**They do NOT prove that the engine applies BPatch to an inherited
subsection.** That requires actual one-mod-at-a-time in-game
observation, comparing Stalker vanilla, Stalker damage, Stalker
frequency, Stalker combined and Veteran combined. The complete
manual test instructions, expected output, safety and recording
table are in
[CombatRuntimeAcceptance](../tests/CombatRuntimeAcceptance/README.md).
All game outcomes start at **NOT RUN**. Do not change the production
writer, UI or runtime support flags based only on these static
or .NET-generated patch checks.

## Before releasing a build

The C# writer contract suite and Combat source checks are registered in [the pre-release test runbook](RELEASE-TESTING.md). Run `python tools/pre_release.py list` and follow its static/source/Windows writer procedure. The generated CFG test checks do not promote mutant cooldown or PSY runtime support states without in-game evidence.
