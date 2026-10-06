# Runtime-proof patch generator

These helpers are **test-only legacy A/B runtime probes**. The three corresponding controls are now present in the normal UI/production writer but remain `runtime_test_required`. These scripts intentionally use exaggerated hand-authored LOW/HIGH values and therefore do not verify the production slider transform.

For structural original→override verification of the actual production slider contract, use `../verify_cfg_overrides.py`.

These helpers generate isolated BPatch staging trees for the same three controls while they wait for in-game proof:

| test | LOW | HIGH | locked surface |
|---|---:|---:|---|
| `weapon-condition` | `MinDurability=0.01`, `MaxDurability=0.05` | `0.95..1.00` | 121 weapon rows / 33 repeatable-human loadouts |
| `weapon-ammo` | `AmmoMinCount/Max=1..1` | `77..77` | same 121 / 33 surface; original `AmmoMaxCount` vs `AmmoMaxcount` spelling preserved |
| `medic-stock` | `MinCount/MaxCount=1..1` | `20..20` | 48 rows = 25 MedicUniversal + 23 MedicNoon |

Example:

```bash
python3 tools/runtime_tests/generate_runtime_test_patches.py \
  --test weapon-condition \
  --variant high \
  --output /tmp/tcd-condition-high
```

The output starts at `Stalker2/Content/...`, exactly like the normal writer staging tree. It can therefore be packed with the same repak/retoc process documented in `CFG-WRITER.md`.

Generate and test **one candidate at a time**. LOW and HIGH must never be installed together. Remove the previous test mod before changing variant.

The script refuses to generate output if the locked CSV surface drifts from the audited row/root counts. It changes only the fields named by the runtime contract; selection Chance/Weight, rank gates and unrelated loot fields are not emitted.

Runtime PASS/FAIL interpretation remains defined in:

- `HUMAN-NPC-DROPPED-WEAPON-CONDITION-RUNTIME-TEST.md`
- `HUMAN-NPC-WEAPON-AMMO-RUNTIME-TEST.md`
- `MEDIC-STOCK-QUANTITY-RUNTIME-TEST.md`

## Pack a ready-to-install runtime test mod

`build_runtime_test_mod.py` wraps the staging generator with the **same packing contract as the production app**: repak `V11`, mount point `../../../`, then retoc `UE5_5`. It does not bundle or download either tool.

Place `repak`/`retoc` (or their `.exe` forms) in `tools/`, or point to another directory with `--tools-dir`:

```bash
python3 tools/runtime_tests/build_runtime_test_mod.py \
  --test weapon-ammo \
  --variant high
```

Default output is `RuntimeTestOutput/` and uses unmistakable names such as:

```text
TCD_RuntimeTest_WeaponAmmo_HIGH_P.pak
TCD_RuntimeTest_WeaponAmmo_HIGH_P.utoc
TCD_RuntimeTest_WeaponAmmo_HIGH_P.ucas
```

Use `--keep-staging` only when you also want the generated loose BPatch tree for inspection. Build/install **one candidate and one variant at a time**; remove the previous runtime-test triplet before interpreting another test.
