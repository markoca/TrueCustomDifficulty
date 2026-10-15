# Combat Control Deep Audit — Active Contract (2026-10-10)

The current Combat catalog contains **30 source dimensions**: **26 confirmed direct mappings** and **4 PSY special routes**. Direct mappings expand to **64 concrete CFG edit points**: **48 difficulty-profile fields** and **16 mutant MaxHP edits**. This document supersedes earlier visible-count and special-writer inventories. Static mapping parity is not an in-game runtime result.

## Active visible controls

The **17 control entries (13 confirmed + 4 PSY runtime-gated)** were an earlier inventory, **not a current on-screen visibility assertion**. The current Combat view filters PSY and Repeated-Hits UI entries; the source parameters remain in catalogs, presets, and CFG writers. The active Human Combat Profile owns Explosion Damage (`combat.explosionDamage`). `BlastDamageBoard` and `MutantCombatPressure` were unreferenced legacy visual families and are removed in the 2026-10-15 cleanup. `combat.mutantDamage` remains a CFG source but is currently suppressed as a standalone UI row; the Mutant Health composite still edits fourteen distinct HP multipliers. The precise Combat visible-editor inventory needs a separate reconciliation pass.

Human Attack Frequency (`combat.npcAttackCooldown`) remains functional. Its UI value is a rate multiplier that writes `NPCCombatDifficulty.NPC_AttackCooldown` using `inverseScale`. Human health, gun damage, accuracy, explosion damage, player lethality, repeated-hit protection, PSY and mutant health routes remain otherwise unchanged.

## Writer evidence

- All 12 direct difficulty scalars/booleans expand to four concrete difficulty edits each (48).
- The 14 mutant health parameters cover 16 independent `VitalParams.MaxHP` targets.
- The four PSY controls target two effects, `SpawnPSYPhantoms` and `SpawnPSYNPC`, through the existing PSY special writer.
- No two direct Combat parameters own the same concrete writer target.

**Verification:** `python3 tools/audit_combat_contract.py`, `python3 tools/audit_project_consistency.py`, `python3 tools/audit_ui_cfg_contract.py` and, with matching original CFG archives, `python3 tools/audit_combat_source_structures.py --base-zip BASE.zip --dlc1-zip DLC1.zip`. On Windows, run the production `CombatPatchContract` writer scenarios. Gameplay semantics and UE merge behavior still require in-game tests.

## 2026-10-15 dead-code pass 1

Removed only unreachable UI scaffolding: old BlastDamageBoard renderer and registration, MutantCombatPressure case and retired cooldown panel mapping, plus its obsolete formatter branch. Preserved all 30 Combat source definitions, 64 direct edit points, PSY special routes, preset keys, and active HumanCombatProfile / Mutant Health behavior. No runtime build or in-game verification was performed during the connector-side source audit. Follow-up candidates: hidden PSY and Repeated-Hits renderers, plus a dedicated check of current Combat visibility before any further deletion.
