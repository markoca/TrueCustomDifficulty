# Stamina Control Deep Audit — v0.2.46

### 2026-10-11 — Core Stamina matches Core Health (current authoritative layout)

The existing `StaminaCore` family is rendered under **CORE STAMINA** (replacing the former **STAMINA & MOVEMENT** outer title and redundant **STAMINA CORE** subheading). The top two 105-DIP primary resource cards are **MAX STAMINA** (`stamina.maxSP`, native **SP**) and **BASE RECOVERY** (`stamina.regeneration`, native **SP/s**). They share the **exact** `DrawCoreResourceRuler` renderer also used by the Core Health MAX HEALTH / PASSIVE RECOVERY cards: the same 12-DIP heading at +15/+12, 19-DIP right-aligned current value at -15/+6, 5-DIP horizontal rail and 7-DIP handle at card top +65, muted reference marker, and 10.4-DIP native min/max labels at bottom -20. Only edited **numeric tokens** become gold; neutral units, scale ticks and baseline markers remain subdued. Native SP-capacity and regeneration values/ranges/steps are not interchanged.

The new layout uses the Core Health header position (Y=21) and identical two-card geometry (Y=51, H=105, gap=12). The next line is **MOVEMENT LIMITS** (Y=173), followed by the **unchanged native vault-height vertical ruler** and **calibrated maximum carry-weight rail** (Y=191, H=175, gap=12). These lower controls retain their exact clickable landmarks, underlying `stamina.vaultMaxHeight` / `stamina.maxCarryWeight` values, drag/keyboard/reset and reference semantics. Board height is **382 DIP** (366-DIP bottom plus 16-DIP padding). There is **no duplicate heading**, no new gameplay control, no full-width slider and no change to the separate Stamina Usage panel.

All four parameters keep their original per-source mappings, preset serialization, validation, selection, reset, and CFG writer operations. This overrides older 351/336-DIP and 72-DIP StaminaCore visual descriptions without changing their gameplay audits.

Runtime reference: **STALKER 2 CFG dump 2.0.5**.

## Result

The current Stamina UI exposes **18 editable dimensions through 7 visible cards**. The Stamina Pressure card contains 12 backing dimensions: the original seven action/aiming axes plus five confirmed exhaustion/recovery axes.

- **17 confirmed** direct dimensions.
- **1 runtime-test-required** traversal control: `stamina.vaultMaxHeight`.
- **0 invalid or partial controls**.
- **21 concrete writer edit points** across the 18 editable dimensions.

The newly promoted backing axes are not separate engine-style cards. They are presented as one semantic **EXHAUSTION & LOW-SP RECOVERY** block inside Stamina Pressure.

## v0.2.46 corrections

### Aiming stamina sign boundary

The previous UI exposed the engine value `StaminaRegenStateCoefs[EStateTag::Aim].Value = -0.25` directly under the label **Aiming Stamina Cost**. That mapping was technically writable but player-facing direction was inverted: a stronger penalty meant moving the slider farther negative.

v0.2.46 changes the UI contract to **Aiming Stamina Recovery Penalty**:

- UI vanilla: `25%` (`0.25`).
- UI range: `0% .. 50%` (`0.00 .. 0.50`).
- Writer operation: `negateSet`.
- Vanilla output remains exactly `-0.25` in `CoreVariables.cfg`.
- Pre-v0.2.46 presets that stored a negative Aim coefficient are migrated with `abs(value)` on load, preserving gameplay.

The UI now follows the same direction as the other stamina cost/penalty controls: farther right means a stronger stamina penalty.

### Maximum Vault Height range

`stamina.vaultMaxHeight` already declared the explicit orientation marker `ABOVE EYES = 300` and its mapping/rationale described `300 uu` as the upper tuner limit, but the actual catalog slider stopped at `260`.

v0.2.46 fixes `slider.max` to **300**. The runtime status remains `runtime_test_required`; only the inconsistent UI ceiling was corrected.

## Control verification

| Control | Parameter ID | Vanilla UI value | CFG target | Writer | Status |
|---|---|---:|---|---|---|
| Maximum Stamina | `stamina.maxSP` | 100 SP | `Player.VitalParams.MaxSP` | `set` | confirmed |
| Base Stamina Recovery | `stamina.regeneration` | 5 SP/s | `Player.VitalParams.RegenSP` | `set` | confirmed |
| Sprint Stamina Drain | `stamina.sprintCost` | 5.25 SP | `Player.StaminaPerAction.Sprint` | `set` | confirmed |
| Aiming Stamina Recovery Penalty | `stamina.aimRegenCoef` | 25% | `StaminaRegenStateCoefs[Aim].Value` | `negateSet` -> `-0.25` | confirmed |
| Sprint Stops Below | `stamina.sprintDisableThreshold` | 16.1 SP | `Player.VitalParams.StaminaDisableThresholds[0].Threshold` | `set` | confirmed |
| Sprint Restart Delay | `stamina.sprintRecoveryDelay` | 1 s | `Player.VitalParams.StaminaDisableThresholds[0].RegenerationDelay` | `set` | confirmed |
| Emergency Recovery Starts Below | `stamina.lowRecoveryThreshold` | 30 SP | `AdditionalRegenStaminaMechanics...NormalSP.ConditionValue` | `set` | confirmed |
| Emergency Recovery Amount | `stamina.lowRecoveryStrength` | 10 SP/s | `IncreaseStaminaPoints.ValueMin/ValueMax` | `set` x2 | confirmed |
| Emergency Recovery Duration | `stamina.lowRecoveryDuration` | 4 s | `IncreaseStaminaPoints.Duration` | `set` | confirmed |
| Jump Stamina Cost | `stamina.jumpCost` | 16 SP | `Player.StaminaPerAction.Jump` | `set` | confirmed |
| Vault Stamina Cost | `stamina.vaultCost` | 12 SP | `Player.StaminaPerAction.Vault` | `set` | confirmed |
| Normal Melee Cost | `stamina.meleeNormalCost` | 16 SP | `Player.StaminaPerAction.MeleeNormal` | `set` | confirmed |
| Strong Melee Cost | `stamina.meleeStrongCost` | 25 SP | `Player.StaminaPerAction.MeleeStrong` | `set` | confirmed |
| Buttstock Melee Cost | `stamina.butstockCost` | 12 SP | `Player.StaminaPerAction.MeleeButstock` | `set` | confirmed |
| Maximum Vault Height | `stamina.vaultMaxHeight` | 130 uu | `Player.VaultingParams.MaxObstacleHeight` | `set` | runtime test required |
| Medium-Water Stamina Drain | `stamina.waterMediumDrain` | +100% | `WaterMediumStaminaPenalty.ValueMin/ValueMax` | `set` x2 | confirmed |
| Deep-Water Stamina Drain | `stamina.waterStrongDrain` | +200% | `WaterStrongStaminaPenalty.ValueMin/ValueMax` | `set` x2 | confirmed |
| Maximum Carry Weight | `stamina.maxCarryWeight` | 80 kg | `DefaultWeightParams.MaxInventoryMass` | `set` | confirmed |

## Value-order constraints

Two linked Stamina relationships remain correct because they operate on the actual CFG/gameplay values:

- `Medium-Water Stamina Drain <= Deep-Water Stamina Drain`.
- `Normal Melee Cost <= Strong Melee Cost`.

The actively moved slider keeps priority and the neighbor moves only as far as required to restore the ordering.

## Explicit NO-UI boundary

The audit does **not** promote any of the following:

- Walk stamina drain or zero-valued Walk/Run/Jogging/Crouch action-cost entries.
- Sprint, Run, Jogging, SlowRun or SprintUnderRunSpeed regeneration coefficients.
- `SlowRunThreshold`.
- Jump movement coefficients or vault search/landing geometry.
- Energy-drink tolerance/overuse mechanics.

These fields are either internal state machinery, overlap a clearer normal control, or lack a complete player-facing 1:1 semantic contract.
