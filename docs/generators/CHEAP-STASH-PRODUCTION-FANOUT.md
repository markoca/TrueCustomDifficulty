# Cheap stash production-only fan-out — runtime 2.0.5

This closes the scope question that blocked **Cheap Stash Ammo Quantity** and **Cheap Stash Grenade Chance**.

The shared vanilla parent `GamePass_Stash_ItemGenerator_Cheap` must **not** be edited globally. It is used by normal world loot, test content, quest/scripted paths and unresolved wrappers. The safe model is **redirect only confirmed production consumers to a TCD-owned tuned Cheap pool**.

## Final confirmed production scope

| Production branch | Physical normal-world containers | Redirect assignments | Safe action |
| --- | ---: | ---: | --- |
| Rank-based `Stash_Cheap_<rank>` usage | **213** | **847** rank-specific settings | Retarget only normal `WorldMap_WP` actor settings to TCD-owned rank wrappers. |
| Location-specific wrappers nesting the Cheap parent | **298** | **298** wrapper edges | Replace the wrapper-local `[0]/[0]` `ItemGeneratorPrototypeSID` with the TCD Cheap pool. |
| Direct normal-world references to the shared Cheap parent | **126** | **126** settings | Retarget only these `WorldMap_WP` settings to the TCD Cheap pool. |
| **Total** | **637** | **1271** | Production-only coverage; groups are disjoint. |

The 637 physical roots are disjoint, so this is not double-counting the same container through multiple branches.

## What stays vanilla

| Surface | Count | Decision |
| --- | ---: | --- |
| Location wrappers explicitly classified scripted/quest | **6** | Exclude. |
| Location wrappers with no resolved runtime consumer | **38** | Hold/unresolved; do not tune. |
| Direct Cheap-parent test-map containers | **1** | Exclude. |
| Direct Cheap-parent quest-level containers | **1** | Exclude. |
| Direct quest/action references to Cheap parent | **35** | Exclude. |
| Rank-wrapper settings on `TestMap_Balance` | **4** | Exclude. |

`HOLD_UNRESOLVED` is intentionally not treated as proof of test/quest use. It means only: **there is no resolved production consumer, therefore a generic player-facing control must not touch it.**

## Why vanilla rank wrappers are not patched globally

The four `Stash_Cheap_Newbie / Experienced / Veteran / Master` generators are shared by normal-world containers **and one `TestMap_Balance` actor**. Patching those wrapper definitions globally would violate the project's test-content exclusion rule. Therefore the normal-world SpawnActor settings are redirected individually to TCD-owned rank wrapper aliases while the test actor remains on the vanilla wrappers.

## Writer architecture implemented

The v0.2.6 writer creates these TCD-owned roots:

- `TCD_GamePass_Stash_ItemGenerator_Cheap` — tuned Cheap pool;
- `TCD_Stash_Cheap_Newbie`;
- `TCD_Stash_Cheap_Experienced`;
- `TCD_Stash_Cheap_Veteran`;
- `TCD_Stash_Cheap_Master`.

Each TCD root is generated from the audited complete vanilla Cheap template (**12 category blocks / 52 item rows**) while inheriting only the neutral `[0]` generator template. This deliberately avoids chained generator inheritance, which is unreliable in community CFG modding. A control alters only its audited fields:

- **Cheap Stash Ammo Quantity:** the three Ammo rows' `MinCount/MaxCount`; never the `40/30/30` selection weights;
- **Cheap Stash Grenade Chance:** the single `GrenadeRGD5.Chance=0.10` field; count remains `1`.

No food, medical, coupon, weight or difficulty/rank gate is to be rewritten as a side effect.

Then apply exactly the assignments in `cheap-stash-production-redirect-manifest.csv`.

### Redirect forms

**Location wrapper**

`ItemGenerator.[0].PossibleItems.[0].ItemGeneratorPrototypeSID`

changes from `GamePass_Stash_ItemGenerator_Cheap` to `TCD_GamePass_Stash_ItemGenerator_Cheap` only for the 298 confirmed production wrappers.

**Direct container**

The manifest identifies the exact `ItemGeneratorSettings.[n].PrototypeSID` entry by source order and source line. Only the 126 normal `WorldMap_WP` direct references are redirected.

**Rank-based container**

The 847 normal-world rank settings are redirected from the corresponding vanilla `Stash_Cheap_<rank>` SID to its TCD-owned rank alias. The single test-map actor is intentionally not included.

## External implementation precedent

The supplied **Improving Stashes Loot** PAK demonstrates the same technical pattern without being treated as balance evidence:

- it defines new ItemGenerator roots (`ISL_MasterGenerator...`);
- it replaces wrapper content with `SubItemGenerator` references to its custom generator graph.

That confirms that custom generator roots + redirection are a practical implementation pattern. We still preserve vanilla content/weights and use only our own audited field changes.

## Machine-readable files

- `cheap-stash-production-wrapper-allowlist.csv` — 298 confirmed production location wrappers and their exact local edge;
- `cheap-stash-direct-container-allowlist.csv` — 126 direct normal-world Cheap-parent settings;
- `cheap-stash-rank-container-allowlist.csv` — 847 rank-specific settings across 213 normal-world physical containers;
- `cheap-stash-production-redirect-manifest.csv` — all 1271 implemented production redirect assignments;
- `cheap-stash-production-exclusions.csv` — protected and unresolved surfaces;
- `cheap-stash-production-fanout-summary.json` — summary and implementation contract.

## Control readiness after this audit

| Control | Semantics | Scaling | Production scope | Runtime writer |
| --- | --- | --- | --- | --- |
| Cheap Stash Ammo Quantity | READY | READY | **READY** | **IMPLEMENTED (runtime test required)** |
| Cheap Stash Grenade Chance | READY | N/A (direct chance) | **READY** | **IMPLEMENTED (runtime test required)** |

The fan-out blocker and writer implementation are now closed. The remaining gate is runtime validation in game: confirm that both TCD-owned controls affect the intended normal-world Cheap stash paths while the documented quest/test/unresolved exclusions remain vanilla.
