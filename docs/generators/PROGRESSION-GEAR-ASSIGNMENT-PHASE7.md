# Progression gear phase 7 — category classification guard

The read-only ammo audit now recognizes both `Category` and `ItemCategory` assignment labels, as well as the `EItemGeneratorCategory::` prefix. It explicitly marks unrecognized categories for review and removes an unused, incorrectly scoped sibling path query. This is an audit correction, **not** a claim of proven ammo-to-weapon linkage.

The existing `docs/generators/FULL-REGULAR-AMMO-CALIBER-AUDIT.md` documents nine proven regular calibers and default ammo SIDs from `WeaponGeneralSetupPrototypes.cfg`, but it is not a per-weapon SID mapping. A weapon-to-ammo map must be derived from exact game-native weapon setup assignments and ammo prototype caliber declarations before the shuffle may move sibling ammo.

Run `python tools/audit_progression_stash_ammo.py --cfg /path/to/GameData/ItemGeneratorPrototypes.cfg --strict`. Even if it passes, strict mode only verifies category classification, **not** ammo compatibility. Whole-recipe production writer and UI remain blocked until caliber mapping and tested BPatch replacement semantics exist.
