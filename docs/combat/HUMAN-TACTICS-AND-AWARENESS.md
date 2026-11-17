# HUMAN TACTICS and HUMAN AWARENESS (2026-11-16)

Combat tab, after HUMAN ENEMIES: two compact card boards. All nine controls are `runtime_test_required`.

## HUMAN TACTICS — CombatSynchronization group tokens

`GameData/CombatSynchronizationPrototypes.cfg`: a squad may start an action only when its group token is free;
`CooldownMin/Max` is the pause after it was used. `CombatSynchronizerEasy/Medium/Hard` inherit `Default`,
Master (`CombatSynchronizerStalker`) owns a table with about half the pauses. Each table has four enemy-rank
blocks (Newbie / Experienced / Veteran / Master = the enemy's rank).

| Card | Id | FilterGroups | Default (Newbie) | Master table | Vanilla rate |
|---|---|---|---|---|---|
| GRENADES | `combat.npcGrenadeRate` | [9] ThrowGrenade, [16] Special.ThrowGrenade | 20–35 s | 10–17 s | 1×, Master 2× |
| SUPPRESSION | `combat.npcSuppressionRate` | [10] SuppressiveFire | 15–25 s | 7–12 s | 1×, Master 2× |
| FLANKING | `combat.npcFlankRate` | [5] Flank | 10–20 s | 5–10 s | 1×, Master 2× |
| PUSH | `combat.npcPushRate` | [6] Advance.Action, [7] Advance.Approach | 12–25 / 25–40 s | 6–12 / 12–20 s | 1×, Master 2× |
| CAMPING PENALTY | `combat.npcCamperPunishRate` | [8] CamperPunish | 2–5 s | 2–5 s | 1× |

* Range 0.25×–3×. Rate r writes **source × reference / r** into the difficulty's own table, so the vanilla
  rate reproduces it exactly (Master 2× = the Master table). The card shows the resulting Newbie-rank pause,
  e.g. `2x · grenade every 10–17 s`.
* One writer (`WriteCombatSynchronization`) handles these five and SPECIAL ABILITY RATE: when any is changed it
  writes the four full rank tables (non-bpatch; Easy/Medium/Hard have no own ranks) with every controlled group
  scaled; the loader guards every group's tag.
* `MaxScore` (how many may act at once) and the weapon-type `CombatSynchronizationScore` are not changed.

## ENEMY ACCURACY (on the HUMAN TACTICS board)

`combat.npcAccuracy`, 0.25×–2×: divides `DispersionRadius` of all 77 `*_NPC` weapon settings
(`WeaponData/CharacterWeaponSettingsPrototypes.cfg`, 75 base + 2 DLC1; e.g. AK74 300, guard pistols 12).
The distance curve and the zombie addend are unchanged. The game does not change it by difficulty.
`NPC_AttackCooldown` / `HipAccuracyMultiplier` stay retired.

## HUMAN AWARENESS (human NPCs only; mutants have their own senses)

| Card | Id | CFG | Vanilla |
|---|---|---|---|
| SIGHT | `combat.npcSightRange` 0.25–3× | `AIPrototypes/VisionScannerPrototypes.cfg` DefaultNPC Central / PeripheralVisionDistance | 8000 / 2500 (ahead 80 m / side 25 m; card: `ahead 80 m · side 25 m`; panel adds the factor) |
| HEARING | `combat.npcHearingRange` 0.25–3× | `AIPrototypes/HearingSensorPrototypes.cfg` BaseHearingSensor SoundEvents[*].HearingDistance (14 non-zero) | shots 46 m, steps 12 m, voice 30 m … |
| MEMORY | `combat.npcAlertMemory` 0.25–3× (0.05 steps, like SIGHT / HEARING) | `AIPrototypes/ThreatPrototypes.cfg` [1] DefaultNPC: freeze 30 s × m, loss 30/s ÷ m; Actions[3] SearchEnemy freeze 30 s × m, loss 20/s ÷ m | ~30 s |

Humans use DefaultNPC (ObjPrototypes NPCBase); bosses / zombies inherit where they do not override; the
mutant sensors (`MutantsHearingSensor`, `DefaultMutants` threat) are separate and untouched.

## Not used

`NPC_Armor_Strike_Add` / `Armor_Strike_Add` (too abstract), `NPC_Weapon_Rank_Add` (no consumer in the CFG),
save rules (out of scope).

## Checks

`tools/audit_human_tactics.py` (with `TCD_BASE_DUMP=<dump>/base`), `audit_cfg_source_values.py` for the 97 direct
points (exact), `audit_combat_contract.py`.

**Cards (2026-11-17):** concrete results only (`grenade every 20–35 s`, `steps 12 m`, `search 30 s`, `spread 100%`); track ends show the figures at the slider limits; the factor stays in the selected-control panel.
