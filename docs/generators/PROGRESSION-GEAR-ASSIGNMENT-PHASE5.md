# Progression gear phase 5 — source CFG parser correction and ammo gate

The prior phase-3/4 source parser expressions accidentally contained doubled backslashes in Python raw regex literals, so they could fail to recognize ordinary `struct.begin` and `struct.end` lines. Corrected both parsers to match the real game CFG syntax.

**Confirmed from extracted source** `ItemGeneratorPrototypes-for-audit.cfg`: `Garbage_DigSite_StashGenerator2_C59BE52D481BCA2E1EB1CE816B67EF71` has a `WeaponPrimary` recipe for `GunBucket_PP` with `Upgrades` and `Attaches`, plus a separate sibling `Ammo` category containing `A918A` (60 rounds). Moving only the complete weapon item block would still leave its linked sibling ammo at the old destination. This is a **production blocker**; the whole-recipe planner remains offline-only.

The safe implementation needs an explicit source-validated weapon+ammo bundle model and a test of whole-node BPatch replacement semantics, rather than a SID-only mutation. Do not change current World Weapon Density, introduce UI controls or claim production-ready stash assignment until these conditions are met. Run both source audits with the extracted `GameData/ItemGeneratorPrototypes.cfg` before proceeding.
