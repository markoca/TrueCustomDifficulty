# Physical ItemContainer safety classification audit

This audit classifies **all 1,569** physically placed base and DLC1 ItemContainer actors by **existing evidence only**. It does **not** authorize additional gameplay patching.

| Safety class | Actors | Meaning |
|---|---:|---|
| `REGULAR_SMARTLOOT` | 831 | Present in the existing regular 831-actor catalog; not a new approval |
| `DLC1_ELIGIBLE_MANIFEST` | 112 | Present in existing DLC1 eligible manifest; retain its existing scope and guards |
| `PROTECTED_BASE` | 78 | Present in base protected actor manifest; must remain untouched |
| `NON_WORLD_OR_TEST` | 67 | Not on a `WorldMap_WP` level; dev/test/blank/other maps, not automatically gameplay stashes |
| `WORLD_UNCLASSIFIED` | 481 | World-placed but **not yet proven safe**; must not be edited merely because of map placement |

The source files are `physical-stash-map-regions.csv`, `base-stash-protected-actor-manifest.csv`, and `dlc1-stash-eligible-actor-manifest.csv`. ActorSID is the only join key. Protection wins over DLC eligibility, and regular-manifest membership is preserved. No heuristic on container name, map region or stash prototype can upgrade a container to safe/regular. Some `WORLD_UNCLASSIFIED` actors can be quest/special; further source-graph/CFG validation is required before expansion.

**Regional unresolved:** of the 102 actors outside polygons, 36 have `WorldMap_WP`-prefixed levels; the rest are non-world/test/blank. These regional uncertainties do not change safety classifications.

Regenerate or verify: `python3 tools/audit_physical_stash_safety.py` or `python3 tools/audit_physical_stash_safety.py --check`. This is an inventory and documentation change only.
