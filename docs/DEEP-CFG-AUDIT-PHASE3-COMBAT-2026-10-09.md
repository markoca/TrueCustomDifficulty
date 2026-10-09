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
