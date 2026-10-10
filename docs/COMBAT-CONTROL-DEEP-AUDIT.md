# Combat Control Deep Audit — Active Contract (2026-10-10)

The current Combat catalog contains **30 source dimensions**: **26 confirmed direct mappings** and **4 PSY special routes**. Direct mappings expand to **64 concrete CFG edit points**: **48 difficulty-profile fields** and **16 mutant MaxHP edits**. This document supersedes earlier visible-count and special-writer inventories. Static mapping parity is not an in-game runtime result.

## Active visible controls

The regular Combat surface has **17 visible control entries**: **13 confirmed** and **4 PSY runtime-gated** entries. Composite controls can contain additional backing source values. Mutant Hit Damage (`combat.mutantDamage`) is a standalone scalar under Mutant Combat; the separate Mutant Health composite edits fourteen confirmed HP multipliers in a compact seven-row-by-two-column bank.

Human Attack Frequency (`combat.npcAttackCooldown`) remains functional. Its UI value is a rate multiplier that writes `NPCCombatDifficulty.NPC_AttackCooldown` using `inverseScale`. Human health, gun damage, accuracy, explosion damage, player lethality, repeated-hit protection, PSY and mutant health routes remain otherwise unchanged.

## Writer evidence

- All 12 direct difficulty scalars/booleans expand to four concrete difficulty edits each (48).
- The 14 mutant health parameters cover 16 independent `VitalParams.MaxHP` targets.
- The four PSY controls target two effects, `SpawnPSYPhantoms` and `SpawnPSYNPC`, through the existing PSY special writer.
- No two direct Combat parameters own the same concrete writer target.

**Verification:** `python3 tools/audit_combat_contract.py`, `python3 tools/audit_project_consistency.py`, `python3 tools/audit_ui_cfg_contract.py` and, with matching original CFG archives, `python3 tools/audit_combat_source_structures.py --base-zip BASE.zip --dlc1-zip DLC1.zip`. On Windows, run the production `CombatPatchContract` writer scenarios. Gameplay semantics and UE merge behavior still require in-game tests.
