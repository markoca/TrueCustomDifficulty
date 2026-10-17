# Progression gear — real source recipe audit and replacement regression (2026-10-18)

Audited the original extracted base `GameData/ItemGeneratorPrototypes.cfg` from `STALKER2-GameData-CFG(20261017-181645).zip`.

**Observed across all 14 allowlisted stash generator locations:** each listed source weapon SID occurs once in its generator. **Four** generators contain a separate `EItemGenerationCategory::Ammo` branch, **ten** do not. This directly contradicts any assumption that each weapon slot has a sibling ammo category. The distinct weapon item roster contains nine SIDs.

Added `tools/test_progression_whole_recipe_source.py` to validate the 14 exact nested source paths and simulate deterministic whole-block rotation within matching progression-tier/weapon-family groups. It checks conservation of complete recipe blocks, not merely item SID counts.

```bash
python tools/test_progression_whole_recipe_source.py --cfg /path/to/GameData/ItemGeneratorPrototypes.cfg
```

**Safety boundary:** in-memory recipe conservation is not proof that `struct.begin {bpatch}` can atomically replace existing `PossibleItems[n]` and remove stale `Upgrades` or `Attaches`. This test does not write a game patch and cannot establish game parser or runtime behavior. The four sibling ammo branches also require source-verified ownership; other ten weapon slots must not be assigned synthetic ammo.

Next production gate: validate a minimal whole-node replacement in the actual game CFG loader, and ensure old weapon-specific children are removed. Do not enable the new assignment in the app until verified.
