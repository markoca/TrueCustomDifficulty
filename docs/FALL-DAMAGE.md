# FALL DAMAGE (2026-11-18)

| Control | Id | Range | Vanilla |
|---|---|---|---|
| FALL DAMAGE (Health tab) | `health.fallDamage` | 0–100 % of fall damage taken, step 5 % | 100 % |

* Writes `GameData/ObjPrototypes.cfg` → `Player.Protection.Fall = 100 − value` (BPatch, `setProtectionFromDamageFraction`).
  100 % → 0 (vanilla), 50 % → 50, 0 % → 100.
* Unit check: the Nexus mod *LESS FALL DAMAGE 50* (`zzzz_LESSFallDamage50_P.pak`, a full pre-2.1 `ObjPrototypes.cfg`)
  changes only `[0].Protection.Fall` from 0 to 50 for "half fall damage", so the value is a percent.
* Why `Player` and not `[0]`: in 2.1 `Player` (`refkey=[0]`) still has no own `Protection` block, so that mod does
  reach the player, but it also gives 50 % fall protection to every NPC and mutant that inherits `[0]` and, as a full
  file copy, reverts 2.1 values (e.g. `MinDialogInteractDistance` 75 → 85, removed `JoggingSpeed`,
  `DeathHitImpulseMultiplier`). Our BPatch touches the player only. `[0].Protection` is all 0, so nothing else is lost.
* Armor `Protection.Fall` is 0 on every item in vanilla; exoskeletons only stop the limp (`bPreventFromLimping`).
* Not exposed: limp thresholds (25 / 65 damage → 2.5 / 4.5 s), `StaminaFallingDamageCoef` 0.5, falling velocities.
* `runtime_test_required`: confirm in game that a fixed drop costs half the HP at 50 %.
