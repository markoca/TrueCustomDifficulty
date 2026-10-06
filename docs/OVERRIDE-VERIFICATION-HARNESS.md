# Override Verification Harness — v0.2.52

The verifier now covers **all 30 visible `runtime_test_required` controls** and models both concrete assignments and CFG struct shape/reference metadata.

The tool is `tools/verify_cfg_overrides.py`.

## What it proves

Two proof levels remain deliberately separate:

1. **`CONTRACT_PASS`** — derives the exact expected override from `Data/parameters.json`, `Data/cfg-writer-baseline.json`, locked redirect manifests, and route-specific production-writer semantics.
2. **CFG-tree `PASS`** — `compare` parses any generated isolated override CFG tree and requires the same assignments **and** struct contract. This is a file-level check; it does not validate or launch the EXE.

For every case the evidence now contains:

- `diff.csv` — expected field assignments and source/target values;
- `structure.csv` — each expected struct node and whether it is `{bpatch}`, plain, or `{refurl=...;refkey=...}`;
- `expected-patch/` — an exact reference BPatch tree;
- `summary.json` / `summary.md`.

`compare` fails on missing, unexpected or wrong-value assignments and also on missing, unexpected or wrong struct mode/reference metadata. No executable verification is required for this workflow.

## Supported runtime-pending routes

| Route | Controls |
|---|---:|
| Ordinary baseline `editPoints` | 11 |
| Mutant cooldown arrays | 8 |
| PSY override arrays | 4 |
| Cheap-stash clone/redirect | 2 |
| NPC medical clone/redirect | 1 |
| Medium-stash clone/redirect | 1 |
| Expensive-stash clone/redirect | 1 |
| Destructible-ammo clone/redirect | 1 |
| Destructible-grenade clone/redirect | 1 |
| **Total** | **30** |

The verifier resolves player-facing difficulty names through `difficultyProfiles` before matching raw `DifficultyPrototypes.cfg` SIDs (`Master -> Stalker`, `Veteran -> Hard`, etc.).

## Master full suite

`docs/override-verification/v0.2.52/master/` contains the primary suite:

- **30 controls**;
- **90 cases**;
- sliders: `MIN / VANILLA / MAX`;
- toggles: `OFF / VANILLA / ON`;
- every `VANILLA` case emits **0 expected assignments**;
- all 90 cases are `CONTRACT_PASS`;
- all **58 non-zero** reference cases were parsed back and matched assignment + struct contracts exactly.

Examples of special-route assignment counts for one non-vanilla case:

| Control | Master expected assignments |
|---|---:|
| Bayun Special Ability Cooldown | 44 |
| PSY scalar/toggle sparse override | 4 |
| Human NPC Medical Loot Frequency | 527 |
| Cheap Stash Ammo/ Grenade control | 2,496 |
| Medium Stash Ammo Quantity | 1,395 |
| Ammo Crate Quantity | 1,273 |
| Grenade Crate Quantity | 196 |
| Expensive Stash Ammo Quantity | 1,455 |

These totals include the static clone/template fields and redirect assignments that the production writer actually emits, not only the dynamic player-controlled fields.

## Veteran special-array branch

Master uses sparse patches for PSY/mutant arrays because its raw SID (`Stalker`) already owns the canonical arrays. Lower baselines use a different C# branch that creates a complete localized subtree.

`docs/override-verification/v0.2.52/veteran-special-arrays/` explicitly verifies that second branch:

- **12 controls** (8 mutant cooldown + 4 PSY);
- **36 cases**;
- every `VANILLA` case emits 0 assignments;
- each non-vanilla mutant cooldown case emits the full **146-assignment** canonical array;
- each non-vanilla PSY scalar case emits **10 assignments**;
- `PSY Phantoms Cause Bleeding` proves the opposite boolean direction from Master: `ON` is the Veteran non-vanilla case;
- all **23 non-zero** cases self-compare exactly, including plain-vs-`{bpatch}` struct modes.

## Production bug found by the special-route audit

The canonical NPC medical redirect manifest has contained **33** production redirects since the named MIK/Mykluha exclusions were removed in v0.2.25. The C# writer still hard-coded an obsolete expectation of **37**.

That would make `Human NPC Medical Loot Frequency` throw `InvalidDataException` before producing an override. v0.2.52 changes the writer expectation to **33** and locks it against the actual manifest count.

## Parser hardening

Some real prototype SIDs contain spaces, e.g. `LesserZone_Cabin _StashGenerator...`. The old compare parser treated prototype names as `\S+` and could reject valid stash output. v0.2.52 parses the entire prototype name up to `: struct.begin` and the complete suite self-compare exercises those names.

## Commands

Run the complete runtime-pending Master suite:

```bash
python3 tools/verify_cfg_overrides.py suite \
  --difficulty Master \
  --output /tmp/tcd-runtime-pending-master
```

Generate one route contract:

```bash
python3 tools/verify_cfg_overrides.py plan \
  --parameter loot.npcMedicalChance \
  --value 0 \
  --difficulty Master \
  --output /tmp/tcd-npc-medical-zero
```

Compare an actual isolated production staging tree:

```bash
python3 tools/verify_cfg_overrides.py compare \
  --parameter loot.npcMedicalChance \
  --value 0 \
  --difficulty Master \
  --override-root /path/to/real/staging \
  --output /tmp/tcd-npc-medical-compare
```

A deliberate wrong-value mutation of the NPC-medical reference patch was also checked: `compare` returns `FAIL` with `wrongValue=1`. A second mutation changed only the generated root from `{refurl=../ItemGeneratorPrototypes.cfg;refkey=[0]}` to `{bpatch}`; assignments stayed identical, but `compare` correctly returned `FAIL` with `wrongStructure=1`.

## Boundary

This is still **not in-game runtime proof**. It proves that the expected override contract is deterministic and gives us an exact oracle for the real C# writer output. A control keeps `runtime_test_required` until its gameplay effect is observed in STALKER 2.
