# CFG BPatch usage audit — 2026-11-18 (v0.9.27.0)

Gate: `tools/audit_bpatch_usage.py` (static, in `tools/pre_release.py` as `bpatch-usage`).

## Result

* **Direct controls:** all 2831 edit points in `cfg-writer-baseline.json` are written by `document.AddAssignment(point.Prototype, point.NodePath, point.Key, value)`. That goes through `GetOrAddNode`, which makes every node `{bpatch}` from the root to the key. Only values that differ from the source are written. All 12 operations used by edit points have a `TransformValue` case; an unknown operation fails the build.
* **Special writers:** 94 non-bpatch constructs at 60 code sites, each pinned to its writer method with a reason. A new site, or a changed count, fails the gate.
* **Raw text writers** (world actor removals and quest reward slots written as text): every `struct.begin` carries `{bpatch}`, and removals are `removenode`.
* **Routes:** `tools/audit_final_cfg_mapping.py` passes (catalog → production BPatch route). Each catalog id has exactly one route.

## Allowed non-bpatch shapes

| Shape | Why the game needs it | Writers |
|---|---|---|
| Whole list | The game reads the list as one value. An indexed bpatch would keep stale vanilla entries. | AgentCooldownMultipliers (mutant move knobs), CombatSynchronization rank tables (SPECIAL MOVES), AdditionalMechanicsEffects (Master mechanics toggles), CorpseSmartLoot package list, PsyPhantomNPCOverrides below Master, trader BuyLimitations, blueprint reward slot |
| refurl / refkey clone | A new TCD SID that copies a vanilla root; the vanilla root stays untouched. Its body is TCD's own and is written whole. | stash / corpse / box loot lists, NPC rare stock, trader sell clones, attachment shuffle, DLC1 cheap stashes, stash gear coverage |
| New TCD definition | A loot list that exists only in TCD (`AddPlainRoot`). | NPC rare trader stock |
| removenode | Removes one vanilla child or one vanilla spawn actor. | world armor / detectors / weapons, authored stash weapons, mod items, trader sells / buys |
| Appended element | A new `[N]` added inside a bpatch parent (Empty slot in Basic stashes). | STASH PRESENCE |

## Not verified here

The game's own BPatch merge and the in-game outcome. Those are the user's in-game tests (`docs/RELEASE-TESTING.md`).
