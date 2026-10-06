# World Attachment Shuffle

Version: 0.9.26.44

## Production surface

`World Attachment Shuffle` is a standalone World Loot toggle.

It operates only on the conservative safe-world attachment surface established by the v0.9.22-v0.9.26.44 audits.

- **39 audited attachment models** with DLC1
- **38 models in the base-only physical surface**
- **67 physical attachment placement slots/usages with DLC1**
- **62 physical slots/usages without DLC1**
- **7 direct world pickups**
- **60 generator-backed physical entry slots with DLC1**
- **55 physical generator usages / clone groups**

`GunAK_MagPaired` is present only on the DLC1-inclusive safe surface, so a base-only build naturally preserves a 38-model source multiset.

## Shuffle contract

The shuffle preserves the **exact source-item multiset**.

It does not create more attachments, remove attachments, or change how many copies of a model exist in the audited surface. It only redistributes those existing attachment instances.

Hard compatibility boundaries:

- Optic -> Optic
- Suppressor -> Suppressor
- Magazine -> Magazine
- Underbarrel -> Underbarrel

Progression may move by at most **one tier**.

Therefore a T2 optic can move to a T2/T3 optic slot, but never into a magazine slot and never directly to a T4 slot.

The writer uses exact max-flow allocation over item-model counts and category/tier slot buckets. The original layout is always a valid solution, so a compatible full allocation must exist unless the audited manifests drift.

## Physical generator isolation

Every safe generator-backed physical usage receives its own TCD-owned generator clone:

`TCD_Attach_<actor SID>_<index>`

The clone inherits the original vanilla generator and overrides only the audited attachment `ItemPrototypeSID` fields.

The physical `SpawnActor` is then redirected to that clone.

This is intentionally done for **all 55 physical generator usages**, not only the two generator definitions already known to be shared. It prevents:

- two physical world locations from becoming coupled;
- a world shuffle from modifying trader/quest/bench consumers;
- future vanilla reuse of a generator definition from inheriting the shuffle accidentally.

For actors that reference the same generator at several rank paths, every matching `PrototypeSID` path is redirected to the same physical clone.

## Direct pickups

The 7 safe direct attachment pickups are patched by changing both:

- `SpawnedPrototypeSID`
- `ItemSID`

The world actor itself is retained.

## Exclusions

The shuffle does not touch:

- traders;
- technicians / benches;
- quest or reward attachment generators;
- internal/default magazines;
- rails, grips, lasers or internal muzzle components that failed the inventory-loot contract;
- unproven or deferred world usages;
- attachment prototypes outside the 39-model audited safe pool.

## UI

Player-facing helper:

`Redistribute optics, suppressors, magazines and underbarrel attachments across compatible world locations.`

The control is independent from World Weapon Shuffle, World Weapon Density and Blueprint Shuffle.

## Runtime status

`runtime_test_required`

Static project and production-integration audits verify the manifests, routing, category/tier boundaries, clone count and exact source-model coverage. Final validation still requires a published build and in-game confirmation of the cloned generator inheritance and direct pickup SID replacement.
