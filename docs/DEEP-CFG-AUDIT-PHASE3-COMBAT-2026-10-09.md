# Deep CFG Audit — Phase 3: Combat (updated 2026-10-10)

Branch: `main`. Source of truth: the current `parameters.json`, `cfg-writer-baseline.json`, `CfgPatchWriterService.cs` and the decoded base/DLC1 CFG archives used for provenance checks. Historic writer inventories are not the release contract.

| Domain | Catalog dimensions | Direct edit points | Evidence |
|---|---:|---:|---|
| Difficulty combat: player, humans, mutant hit damage, repeated hits, explosion | 12 | 48 | Four difficulty mappings per parameter |
| Mutant Health | 14 | 16 | Native `VitalParams.MaxHP` for 14 families and two extra variants |
| PSY combat | 4 | Structural writer | Two PSY effect identities; runtime verification required |
| **Total** | **30** | **64** | **26 confirmed direct + 4 runtime-gated** |

Novice = `Easy`, Stalker = `Medium`, Veteran = `Hard`, Master = `Stalker`. The source raw values, keys and operators of direct paths are audited. Only the human attack-rate control retains `inverseScale`; it writes `NPC_AttackCooldown` and its scope does not include mutants.

Mutant Hit Damage remains one native, compact scalar, while Mutant Health remains the independent 14-species control. The PSY writer continues to update the selected Master entries via `{bpatch}`, or to create complete two-effect override arrays on other difficulties. Such non-Master array insertion is structurally checked but still requires game-runtime verification. The repeated-hit controls preserve their own actual CFG-based minimum/maximum constraint.

The production suite checks generated CFG structure, not the Unreal Engine loader. Use `tools/audit_combat_contract.py`, `tools/audit_combat_source_structures.py` with supplied archives, `tools/audit_project_consistency.py` and `tests/CombatPatchContract`. Never treat a source-only PASS as proof of in-game effects. See [release testing](RELEASE-TESTING.md).
