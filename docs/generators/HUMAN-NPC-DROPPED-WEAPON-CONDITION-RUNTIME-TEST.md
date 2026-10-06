# NPC Weapon Condition — Runtime Test Contract

**Version:** v0.2.33  
**Control:** `loot.droppedWeaponCondition`  
**Static status:** `STRUCTURALLY_READY_RUNTIME_PROOF`  
**UI status:** not promoted; runtime proof required

## Proven static boundary

The repeatable standard-human production surface contains exactly **121 local weapon rows across 33 loadout generators**. Every row has `MinDurability` and `MaxDurability`; there are **zero nested/shared weapon dependencies** after hard exclusions.

- 120/121 rows use vanilla `0.25..0.50`.
- `GuardGunGrim_ST` in `GuardNPC_Corpus_Stormtrooper_ItemGenerator` is the sole exception at `0.35..0.45`.
- Hard exclusions: 2 Prologue loadouts + 4 named DLC `GeneralNPC_MIK_*` / Mykluha loadouts.
- Those six excluded roots account for all six historical shared-pistol links.

This proves writer targeting. It does **not** by itself prove that the source durability range is the final observable condition of a weapon carried/dropped by an NPC.

## Runtime A/B test

Build two diagnostic variants while changing **only** `MinDurability/MaxDurability` on the 121-row production allowlist. Do not change weapon-selection `Weight`/`Chance`, attached ammo, NPC health, corpse loot, or any excluded root.

| Variant | MinDurability | MaxDurability | Purpose |
| --- | ---: | ---: | --- |
| LOW | 0.01 | 0.05 | unmistakably near-broken weapon condition |
| HIGH | 0.95 | 1.00 | unmistakably near-pristine weapon condition |

Use ordinary repeatable human NPCs, not Prologue, named/story NPCs, MIK/Mykluha, zombies, quest-scripted targets or traders. Sample multiple kills from at least **three distinct ordinary role/loadout families** where practical (for example Stormtrooper, Sniper, CloseCombat/Recon).

## PASS criteria

1. LOW drops consistently land inside or clearly track the `0.01..0.05` source range.
2. HIGH drops consistently land inside or clearly track the `0.95..1.00` source range.
3. The LOW/HIGH result repeats across multiple ordinary production loadouts rather than one special NPC.
4. Weapon selection remains governed by the original per-row `Weight`/`Chance` values.
5. No excluded Prologue/named MIK/Mykluha/shared-pool source is modified by the generated patch.

If the game applies a deterministic post-process to durability, document that transform rather than accepting a loose visual correlation.

## FAIL / inconclusive criteria

- Dropped condition remains near vanilla despite the extreme source ranges.
- LOW and HIGH are not observably separable.
- Only a special/scripted NPC responds.
- The result changes because another durability/corpse system was modified at the same time.
- A post-process exists but cannot yet be mapped deterministically.

A failure does not justify compensating with invented multipliers. The control stays out of the normal UI until the actual runtime chain is understood.

## Promotion rule

On a clean PASS, promote `loot.droppedWeaponCondition` from `STRUCTURALLY_READY_RUNTIME_PROOF` to the normal UI/writer contract. The production target remains exactly the audited 121 rows; exclusions stay unchanged.

## v0.9.26.44 UI semantics

The production control is now **NPC Weapon Condition** and exposes direct condition bounds instead of a multiplier.

- default UI position: **MIN 25% / MAX 50%**
- untouched/default: emits no patch, preserving all authored vanilla rows, including the single 35%-45% outlier
- once modified: selected MIN/MAX are written directly to every audited `MinDurability` / `MaxDurability` field
- 1% UI steps
- `MIN <= MAX` is enforced by the range control
- pre-v0.9.26.44 multiplier presets are migrated to equivalent 25%-50%-based bounds on load

The runtime proof requirement remains: confirm the resulting dropped weapon condition in game.
