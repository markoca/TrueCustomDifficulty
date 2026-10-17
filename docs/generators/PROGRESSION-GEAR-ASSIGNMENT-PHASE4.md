# Progression-aware gear — phase 4: verified whole-recipe assignments

**Offline-only.** The source-aware planner now reads the original `GameData/ItemGeneratorPrototypes.cfg` and resolves the 14 approved `generator SID + ItemGenerator path` locations. It checks every source `ItemPrototypeSID`, verifies progression tier and weapon family against the explicit gear catalog, and shuffles **complete source weapon item blocks** only among matching tier/family destinations. It preserves the source multiplicity and uses deterministic per-bucket seeds.

Run:
```bash
python tools/plan_progression_whole_recipes.py --cfg /path/to/GameData/ItemGeneratorPrototypes.cfg --check
python tools/plan_progression_whole_recipes.py --cfg /path/to/GameData/ItemGeneratorPrototypes.cfg --seed 42
```

The second command prints source and destination identifiers plus complete copied source blocks. **This is deliberately NOT a deployable BPatch**: no whole-node patch syntax has yet been verified against the game parser. This avoids the unsafe earlier SID-only patch preview, which could leave weapon-specific `Upgrades`, `Attaches`, ammo and count settings from the previous weapon.

The source recipe audit and the full-recipe planner both require a real extracted CFG. No UI, production CFG writer, world density behavior, or armor pool has been changed. Production gating remains: verify BPatch whole-node replacement semantics with a fixture/game parser; establish how adjacent ammo categories should follow their weapon; prove safe physical actor destinations; add regression tests before enabling a toggle.
