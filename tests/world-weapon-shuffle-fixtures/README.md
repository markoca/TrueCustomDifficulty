# World Weapon Shuffle CFG fixtures

These fixtures lock the **exact BPatch shape** expected from `PatchDocument.Render()` for the world-weapon replacement nodes.

- `25%` contains 2 compatible slots.
- `50%` contains 4 compatible slots.
- `100%` contains all 8 compatible slots.
- The fixture selection is deliberately fixed/audit-oriented (sorted slot order + first sorted compatible replacement), so it does **not** claim to reproduce a particular CLR `System.Random` seed.
- Runtime C# uses SHA-256-derived seed + `System.Random`; the existing regression audit separately proves that Amount is excluded from the seed and selection is a deterministic prefix.
- A real `dotnet build`/application run remains required on a machine with .NET 8 SDK to validate CLR execution and game runtime behavior.

The important structural property is that generator and parent nodes use `{bpatch}`, while the final `PossibleItems/[n]` node is a full replacement node. This deliberately drops weapon-specific vanilla `Upgrades`/`Attaches` while preserving only the neutral spawn fields recorded in the manifest.
