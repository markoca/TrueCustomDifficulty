## 2026-10-20 — CURRENT Combat control retirement (supersedes earlier Attack Frequency / Accuracy UI locks)

**Retired, not replaced:** `combat.npcAttackCooldown` (Enemy Attack Rate / Attack Frequency) and `combat.npcHipAccuracy` (Enemy Shot Accuracy / Fire Accuracy) are no longer part of the playable control catalog, the Human Combat aggregate, any of the twelve shipped presets, or the production CFG writer baseline. These two scalar fields do not represent the full enemy attack-timing and aiming behavior systems; no invented one-knob proxy replaces them. Previously saved presets containing these retired IDs load with those keys ignored and omit them on resave. No generated CFG edit targets `NPCCombatDifficulty.NPC_AttackCooldown` or `NPCCombatDifficulty.HipAccuracyMultiplier` via these retired settings.

**Active Human Enemies editor:** independent X/Y **ENEMY HP** and **ENEMY GUN DAMAGE** plot with exterior axis handles and an interactive joint, plus **one** compact **EXPLOSION DAMAGE** knob. Use the existing 270° knob grammar, neutral references, number-only gold modified values and the same wide/narrow responsive layout. Remove every inactive three-knob/fader/Attack Rate/Accuracy drawing, dependency, help hint and preset migration. Do not stretch the surviving knob to fill the empty bank. All other combat source targets, the Stamina Usage layout conventions, and hover/reset behavior stay unchanged.

**Current authority:** **285** catalog/writer parameters, **180** player-visible controls (**15** Combat), **105** hidden; **12** shipped factory presets with **285** values each; **1230** direct CFG baseline edit points / **215** direct source IDs. The 2026-10-19 copy ledger is reduced to 180 current rows, without rewriting the historical wording decisions for other controls. Previous dated sections below remain design history wherever they conflict with this retirement.

# Combat Control Deep Audit — Active Contract (2026-10-10)

After the 2026-10-20 retirement, the Combat catalog contains **28 source dimensions**: **24 confirmed direct mappings** and **4 PSY special routes**. Direct mappings expand to **56 concrete CFG edit points**: **40 difficulty-profile fields** and **16 mutant MaxHP edits**. This document supersedes earlier visible-count and special-writer inventories. Static mapping parity is not an in-game runtime result.

## Active visible controls

The **17 control entries (13 confirmed + 4 PSY runtime-gated)** were the pre-retirement inventory, **not a current on-screen visibility assertion**. The current Combat view filters PSY and Repeated-Hits UI entries; the source parameters remain in catalogs, presets, and CFG writers. The active Human Combat Profile owns Explosion Damage (`combat.explosionDamage`). `BlastDamageBoard` and `MutantCombatPressure` were unreferenced legacy visual families and are removed in the 2026-10-15 cleanup. `combat.mutantDamage` remains a CFG source but is currently suppressed as a standalone UI row; the Mutant Health composite still edits fourteen distinct HP multipliers. The precise Combat visible-editor inventory needs a separate reconciliation pass.

Human Attack Frequency and Enemy Shot Accuracy are retired; no attack-pacing or shot-accuracy settings remain. Human HP, gun damage, explosion damage, player lethality, repeated-hit protection, PSY and mutant health routes remain otherwise unchanged.

## Writer evidence

- All 10 remaining direct difficulty scalars/booleans expand to four concrete difficulty edits each (40).
- The 14 mutant health parameters cover 16 independent `VitalParams.MaxHP` targets.
- The four PSY controls target two effects, `SpawnPSYPhantoms` and `SpawnPSYNPC`, through the existing PSY special writer.
- No two direct Combat parameters own the same concrete writer target.

**Verification:** `python3 tools/audit_combat_contract.py`, `python3 tools/audit_project_consistency.py`, `python3 tools/audit_ui_cfg_contract.py` and, with matching original CFG archives, `python3 tools/audit_combat_source_structures.py --base-zip BASE.zip --dlc1-zip DLC1.zip`. On Windows, run the production `CombatPatchContract` writer scenarios. Gameplay semantics and UE merge behavior still require in-game tests.

## 2026-10-15 dead-code pass 1

Removed only unreachable UI scaffolding: old BlastDamageBoard renderer and registration, MutantCombatPressure case and retired cooldown panel mapping, plus its obsolete formatter branch. Preserved all 30 Combat source definitions, 64 direct edit points, PSY special routes, preset keys, and active HumanCombatProfile / Mutant Health behavior. No runtime build or in-game verification was performed during the connector-side source audit. Follow-up candidates: hidden PSY and Repeated-Hits renderers, plus a dedicated check of current Combat visibility before any further deletion.

## 2026-10-15 dead-code pass 2

The visible Combat view explicitly filters four `combat.psy*` and three `combat.accumulatedDamage*` entries and their group headers. Removed unreachable `DrawPsyPhantomPressure` and `DrawRepeatedHitBand` renderers, dispatch/measurement/member entries, obsolete shared-input cases, UI-family/help maps and their aggregate-only subscriptions. Added a second `ParameterViewModel.IsControlVisible` safeguard. All seven source definitions and preset/CFG routes are retained, including both PSY override effects and the repeated-hit `MIN <= MAX` constraint. Actual Combat control layout is unchanged. Static audits verify retirement and writer parity; Wine and in-game acceptance are still local tests.

## 2026-10-15 dead-code pass 3

Removed five private aggregate renderer methods with no call sites: `DrawDualParameterHeading`, `DrawInlineHorizontalTrack`, `DrawCurveLine`, `AddVerticalPointTarget`, and `DrawChangedNumbers`. Eliminated the obsolete Combat paired-value cases and UI sort rankings for hidden parameters. Explosion Damage remains editable under Human Combat; `MainViewModel` now wires its modified state into the active Human Combat aggregate instead of the unused self-aggregate. All Combat sources, preset keys, original CFG targets, and active UI hit/keyboard/reset controls remain intact. The prior 17-item count is a catalog-exposed-source count, not a count of visible widgets. Local build/Wine verification pending.

## 2026-10-15 Combat cleanup — Mutant Health dead drawing paths

Source-level inspection confirmed no calls to `MutantHealthRadar.HpToX`, `ValueToY`, or `DrawRightAlignedText`. Deleted these helpers and the exclusive `GlobalHpMaximum` constant; removed the obsolete pipe-separator accent formatting, unused per-mutant `ShowMist` record field and boolean arguments from all fourteen entries. Names, original vanilla HP reference values, source parameter IDs, per-species horizontal rulers, hit damage controls and their inputs are unchanged. The compact geometry audit now tests the real 605-DIP and width/two-column constraints rather than a constant read only by a deleted method. No writer/preset/gameplay data changes. Static guards added; WPF/Wine runtime verification remains pending.
