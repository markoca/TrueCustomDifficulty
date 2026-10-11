# Stamina CFG Mapping — v0.1.90

### 2026-10-11 — Compact Movement Limits and gamer-facing Exhaustion labels (latest authoritative contract)

**MOVEMENT LIMITS** in `StaminaCore` is now **two matching compact horizontal 116-DIP cards**, not a tall vertical vault bar paired with an oversized carry chart. **MAXIMUM VAULT HEIGHT** (`stamina.vaultMaxHeight`, native **70–300 uu**, 1-uu steps, vanilla **130 uu**) and **MAXIMUM CARRY WEIGHT** (`stamina.maxCarryWeight`, native **20–160 KG**, 5-KG steps, vanilla **80 KG**) share the **same title-left/value-right, rail, source-reference, handle and endpoint geometry**. Each card starts at **Y=191**, with its rail centered at card top +51, a 12-DIP sibling gap, and native min/max units in the bottom footer. The vault rail retains three **source-authored, clickable exact-value landmark captions**: **WAIST=130**, **EYES=225**, **ABOVE EYES=300**. At narrow widths ABOVE EYES drops one short label line to prevent overlap; clicking captions snaps the unchanged underlying source value exactly, not an approximation. The carry rail has only three intermediate kg calibration ticks (native quarter increments); it **does not** duplicate a separate “LOAD CAPACITY / KG” caption or suggest fabricated fatigue thresholds. Only a modified numeric token is highlighted, never its `uu`/`KG` unit or Vanilla/Base tick. Board height is **323 DIP** rather than 382, retaining the two top CORE STAMINA 105-DIP cards unchanged. Existing pointer, keyboard, right-click per-source reset, `R` family reset and original four CFG/preset mappings are preserved.

**EXHAUSTION & RECOVERY** retains its six independent source-native sliders and the 2+4 arrangement, 94-DIP card grammar, shared 0–60 SP *visual ruler* for the thresholds, real Sprint Lock **0–32.2 SP** interactive range, and compact responsive wrapping. The six current visible names, and corresponding catalog `label` metadata, are:
- **SPRINT STOPS BELOW** — `stamina.sprintDisableThreshold`: sprint lock threshold in SP
- **BONUS REGEN BELOW** — `stamina.lowRecoveryThreshold`: low-stamina emergency recovery trigger in SP
- **AIM SLOWS REGEN** — `stamina.aimRegenCoef`: percent penalty to regeneration *while aiming*, not a low-SP aim penalty
- **SPRINT RECOVERY DELAY** — `stamina.sprintRecoveryDelay`: time before recovery following sprint exhaustion, seconds
- **BONUS REGEN POWER** — `stamina.lowRecoveryStrength`: additional low-SP recovery strength in SP/s
- **BONUS REGEN DURATION** — `stamina.lowRecoveryDuration`: duration of that bonus, seconds

The names are user-facing wording only: no source range, step, transformation, unit, writer, constraint or save format is modified. Supersede the older vertically oriented Vault Height design, 175-DIP Movement card, 382-DIP StaminaCore height, and technical Exhaustion captions in dated catalog sections below.

Runtime source of truth: **STALKER 2 CFG dump 2.0.5**.

## Scope

The normal **Stamina** tab is intentionally limited to controls that have a clear player-facing meaning: stamina pool size, ordinary recovery, direct action drain/cost, water penalties, vault height and carry capacity.

Valid but over-technical/internal stamina fields remain mapped in this document and in the parameter catalog with `exposeInUi = false`. They are not deleted from the audit trail and are not written by normal presets/builds because no active UI `ParameterViewModel` is created for them.

Hunger/radiation/fatigue stamina penalties remain under **Survival**. Equipment-wear and item-weight controls now appear under **Survival**; their unchanged CFG sources retain their original equipment IDs.

## Normal UI controls

### Core stamina

| UI control | CFG target | Vanilla | Meaning |
|---|---|---:|---|
| Maximum Stamina | `ObjPrototypes.cfg -> Player.VitalParams.MaxSP` | 100 SP | Size of the player stamina pool. |
| Base Stamina Recovery | `Player.VitalParams.RegenSP` | 5 | Ordinary actor-level stamina regeneration. |

Action costs remain raw SP values. Increasing MaxSP therefore changes how many vanilla-cost actions fit into a full bar without changing the action-cost fields themselves.

### Action costs / drain

Source: `ObjPrototypes.cfg -> Player.StaminaPerAction`.

| UI control | Vanilla | UI policy |
|---|---:|---|
| **Sprint Stamina Drain** | 5.25 SP | Primary player-facing sprint stamina control. |
| Jump Stamina Cost | 16 SP | Exposed. |
| Vault Stamina Cost | 12 SP | Exposed. |
| Normal Melee Cost | 16 SP | Exposed; linked to Strong Melee Cost by value. |
| Strong Melee Cost | 25 SP | Exposed; cannot fall below Normal Melee Cost. |
| Buttstock Melee Cost | 12 SP | Exposed. |

`Walk`, `Run`, `Jogging`, `Crouch`, `LowCrouch` and `Climb` are `0` in the Player `StaminaPerAction` block. They are intentionally not exposed. In particular, **Walk Stamina Drain is not a normal UI control**.

### Aiming state

`CoreVariables.cfg -> StaminaRegenStateCoefs[EStateTag::Aim] = -0.25` is exposed as **Aiming Stamina Recovery Penalty** under **Action Costs**. The engine stores the effect as a signed regeneration-state coefficient, but the normal UI now exposes the intuitive positive penalty magnitude: vanilla `25%` writes the original `-0.25` CFG value, `0%` removes the aiming recovery penalty, and `50%` writes `-0.50`. This is a continuous state penalty while Aim is held, not a one-time action charge.

### Traversal — maximum vault height

Runtime Player configuration:

```text
VaultingParams
  MinObstacleHeight = 70
  MaxObstacleHeight = 130
```

The tuner exposes **`Player.VaultingParams.MaxObstacleHeight`** directly with a range of `70..300` Unreal units.

Visible orientation points on the slider:

| Marker | Raw value | Purpose |
|---|---:|---|
| `VANILLA/WAIST` | 130 | Vanilla runtime value and low practical traversal reference. |
| `EYES` | 225 | Higher practical reference around face/head-level traversal. |
| `ABOVE EYES` | 300 | Upper tuner limit for slightly-above-head traversal. |

The raw CFG field and vanilla value are confirmed from runtime 2.0.5. Body-height names are UX reference labels, not engine metadata or literal anatomical centimetres. Actual reachable geometry also depends on collision, landing checks, `MinObstacleHeight`, search distance/radius, animation constraints and surrounding world geometry. High-value behavior therefore remains `runtime_test_required`.

### Water stamina pressure

Player water immersion uses `WaterContactInfo` thresholds:

- Medium begins at depth threshold `31`.
- High begins at `76`.
- Deadly begins at `126`.

The tuner leaves those depth thresholds untouched and exposes only the stamina-drain effects:

| Effect | Vanilla | Affected action types |
|---|---:|---|
| `WaterMediumStaminaPenalty` | +100% SP drain | Walk, Run, Sprint, Jump, Vault |
| `WaterStrongStaminaPenalty` | +200% SP drain | Walk, Run, Sprint, Jump, Vault |

`WaterStrongStaminaPenalty` is also used in the Deadly water state. `WaterDeadlyDamage` remains separate and is not changed.

### Encumbrance

The existing **Maximum Carry Weight** control remains:

- `ObjWeightParamsPrototypes.cfg -> DefaultWeightParams.MaxInventoryMass = 80`

The runtime also defines weight-effect thresholds at `50 / 60 / 70 / 80` and stamina-regeneration penalties of approximately `-10% / -20% / -35% / -50%`. They remain documented rather than exposed as independent sliders.

## Audit-only stamina mechanics — intentionally excluded from normal UI

These fields are real and remain mapped. They are hidden because they are internal/over-technical, duplicate a clearer player-facing control, or form a multi-field helper subsystem that would make the normal difficulty UI harder to understand.

| Parameter ID | CFG / vanilla | Why excluded |
|---|---|---|
| `stamina.sprintDisableThreshold` | Sprint `StaminaDisableThresholds.Threshold = 16.1` | **Sprint Stops Below** is a technical exhaustion threshold. The normal UI uses Sprint Stamina Drain instead. |
| `stamina.sprintRecoveryDelay` | Sprint `StaminaDisableThresholds.RegenerationDelay = 1 s` | Tied to the hidden exhaustion-threshold rule; not a clear independent player-facing difficulty control. |
| `stamina.lowRecoveryThreshold` | `AdditionalRegenStaminaMechanics` low-SP boundary = `30 SP` | Part of the emergency low-stamina helper subsystem; hidden together with its strength/duration. |
| `stamina.lowRecoveryStrength` | `IncreaseStaminaPoints ValueMin/ValueMax = 10` | Internal emergency-recovery strength; normal UI uses Base Stamina Recovery. |
| `stamina.lowRecoveryDuration` | `IncreaseStaminaPoints Duration = 4 s` | Internal emergency-recovery duration; normal UI uses Base Stamina Recovery. |
| `stamina.sprintRegenCoef` | `StaminaRegenStateCoefs[Sprint] = -1.40` | Valid Sprint-state coefficient, but exposing it next to Sprint Stamina Drain creates two different sprint stamina controls with overlapping player meaning. Kept vanilla. |
| `stamina.sprintUnderRunSpeedRegenCoef` | `SprintUnderRunSpeed = -0.65` | Internal engine speed state; exact transition semantics are not proven. |
| `stamina.runRegenCoef` | `Run = -0.65` | Internal engine state without a one-to-one player input mapping. |
| `stamina.joggingRegenCoef` | `Jogging = 0` | Internal engine state; no dedicated Jogging input. |
| `stamina.slowRunRegenCoef` | `SlowRun = 0` | Internal engine state and **not** the player Walk toggle. `SlowRunThreshold = 0.5` exists, but the complete transition rule is not proven. |

### Emergency recovery mechanics, for reference

`AdditionalRegenStaminaMechanics` has:

- `NormalSP.ConditionValue = 30`
- `LowSPThreshold.ConditionValue = 0`
- low-SP branch -> `IncreaseStaminaPoints`

`IncreaseStaminaPoints` is:

- `Type = RegenStamina`
- `ValueMin = ValueMax = 10`
- `Duration = 4.0 s`

This is a vanilla low-stamina assistance mechanism layered on top of ordinary stamina regeneration. The three controls are deliberately not exposed because tuning threshold + amount + duration independently adds complexity without improving the core difficulty workflow.

### Movement-state regeneration, for reference

Source: `CoreVariables.cfg -> StaminaRegenStateCoefs`.

| CFG state | Vanilla | Normal UI |
|---|---:|---|
| `Sprint` | -1.40 | Hidden; Sprint Stamina Drain is the single normal sprint control. |
| `SprintUnderRunSpeed` | -0.65 | Hidden; internal state. |
| `Run` | -0.65 | Hidden; internal state. |
| `Jogging` | 0 | Hidden; internal state. |
| `SlowRun` | 0 | Hidden; internal state, not Walk. |
| `Aim` | -0.25 | Exposed as **Aiming Stamina Recovery Penalty**: positive UI magnitude, negated by the writer to the signed CFG coefficient. |

## Other intentionally unexposed fields

- `SlowRunThreshold = 0.5`: state-selection semantics require runtime testing before it could ever become a difficulty control.
- `MovementParams.JumpSpeedCoef = 1` and `AirControlCoef = 0.1`: movement-feel tuning rather than stamina economy.
- Vault search radius, depth trace, landing offset/slope and window-detection fields: remain vanilla so Maximum Vault Height is the only traversal-geometry change in this pass.
- Energy-drink tolerance/overuse mechanics: separate consumable/tolerance subsystem, not mixed into base stamina controls.
