# Physical ItemContainer safety classification — filtered inventory (2026-10-18)

The game CFG inventories **1,569** placed ItemContainer actors. **Only 915 actor SIDs are included in the filtered eligible inventory**, by direct intersection with the existing source-backed, guarded writer manifests. A map location, apparent regular item, generic ItemGenerator, or stash name never grants permission to edit an actor.

| Safety class | Count | Eligible for existing guarded stash edits? |
|---|---:|---|
| `REGULAR_SMARTLOOT` | **803** | Yes — base WorldMap, exact no-clue regular SmartLoot manifest |
| `DLC1_ELIGIBLE_MANIFEST` | **112** | Yes — exact DLC1 no-clue allowlist, subject to DLC-specific writer guards |
| `PROTECTED_BASE` | **106** | **No** — quest/unique/blueprint generators or named clue, including **28** otherwise regular SmartLoot candidates |
| `NON_WORLD_OR_TEST` | **67** | No — dev/test/blank/non-main-world levels |
| `WORLD_UNCLASSIFIED` | **481** | **No** — world container without positive ordinary-stash evidence |
| **Total** | **1,569** | **915 eligible / 654 excluded** |

**Whole-container rule:** if an actor is protected, do not modify even its ordinary ammo, consumables or medical entries. In particular, the 28 named-clue actors in the 831-row *candidate* SmartLoot catalog are excluded from the **803** eligible base actors. The 106 protected actors take precedence over inventory classification and DLC approval. The underlying base 106-ID denylist and DLC 112-ID allowlist remain the runtime source-of-truth protections.

**Machine-readable outputs:**
- `src/TrueCustomDifficulty/Data/physical-stash-safety-catalog.csv` — complete 1,569-row classified inventory, preserving all originally extracted coordinates/region context.
- `src/TrueCustomDifficulty/Data/physical-stash-eligible-catalog.csv` — 915-row derived, filtered **reference inventory** for the existing writer scope; not an independent authorization table.
- `src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv` — original **831** candidates with 28 clues, retained as evidence.
- `src/TrueCustomDifficulty/Data/base-stash-protected-actor-manifest.csv` — 106 exact protected base ActorSIDs.
- `src/TrueCustomDifficulty/Data/dlc1-stash-eligible-actor-manifest.csv` — 112 source-approved DLC actors.

Run `python3 tools/audit_physical_stash_safety.py --check` to assert both generated catalogs are exact and check actor identities, scope/origin, protection precedence and the 803/112/106/67/481 partition. Run without `--check` to regenerate both CSVs.

**Scope caveat:** the filtered file is an actor-level safety inventory, not proof that all 915 actors are spawned, accessible, discoverable or affected by every knob. Tier ItemGenerator wrappers, world SmartLoot, direct medical and DLC1 paths have distinct native consumers and additional production guards. Shared wrappers remain blocked whenever any linked actor is protected. A physical actor inventory also does not certify that a *shared source generator* has no special consumers elsewhere; never extend a writer from this CSV alone. Unclassified and quest-associated objects remain excluded until separately source-validated and deliberately allowlisted. No UI controls, presets, parameters or probability semantics were changed in this filtering update.

Region labels are derived from community polygons and do not decide safety. See [PHYSICAL-STASH-MAP-REGIONS.md](../PHYSICAL-STASH-MAP-REGIONS.md) and [BASE-STASH-PROTECTED-ACTORS-2026-10-16.md](BASE-STASH-PROTECTED-ACTORS-2026-10-16.md).
