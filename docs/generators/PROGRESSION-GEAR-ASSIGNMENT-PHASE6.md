# Progression gear phase 6 — sibling ammo discovery

This phase adds a **read-only** audit of the ammo category siblings of the 14 allowlisted source weapon recipes. It reads the extracted original `GameData/ItemGeneratorPrototypes.cfg` and reports ammo SIDs and other categories per generator. It does **not** claim that an ammo SID is compatible with a particular weapon; that needs game-native caliber/weapon mapping and an explicit linkage policy.

```bash
python tools/audit_progression_stash_ammo.py --cfg /path/to/GameData/ItemGeneratorPrototypes.cfg
python tools/audit_progression_stash_ammo.py --cfg /path/to/GameData/ItemGeneratorPrototypes.cfg --strict
```

The second command is intentionally conservative: any missing ammo or unclassified sibling category prevents progression to production. The report does not mutate the game, modify the existing World Weapon Density writer, or create a toggle. Whole-recipe reassignment stays offline until sibling ammo behavior, category ownership, and BPatch replacement are independently verified.

Known source example: `Garbage_DigSite_StashGenerator2_C59BE52D481BCA2E1EB1CE816B67EF71` has a `GunBucket_PP` weapon recipe and separate `A918A` ammo category. Merely moving the weapon recipe can leave unrelated ammo behind.
