# Progression gear assignment — phase 2: CFG patch preview

This phase extends the read-only phase-1 planner with `--patch-preview`, emitting a **stdout-only** BPatch proposal for changed generator slots. It does not write files, modify the build, or enable a UI toggle.

Example: `python tools/plan_progression_gear_assignment.py --seed 42 --patch-preview`.

**Hard gate:** This preview changes only `ItemPrototypeSID`. It is **not** production-safe because the existing weapon slot may carry weapon-specific `Upgrades`, `Attaches`, ammunition and other nested recipe fields. Before applying, the CFG writer must move or regenerate a full compatible recipe, not leave stale child references. The existing production World Weapon Density writer is unchanged and currently performs SID-only generator replacements; it must be independently audited for this compatibility concern.

Next: source-preserving recipe extraction from GameData CFG for all 14 allowlisted slots, deterministic whole-recipe assignment, CFG parser/regression tests, then an opt-in production toggle. Armor requires its own proven safe destination list. Regional tier mapping remains unresolved.
