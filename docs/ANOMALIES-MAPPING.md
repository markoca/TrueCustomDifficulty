# Anomalies Mapping

## Current UI scope

The player-facing tab is named **Anomalies**. Its internal tab ID remains `environment` for saved-preset compatibility.

Currently exposed controls are:

- Global Anomaly Damage (`EnvironmentDifficulty.Anomaly_Damage`)
- Electro base damage (`ElectroAnomaly`)
- Chemical base damage (`ChemicalDamage`)
- Fire base damage (`FireAnomalyDPS`)
- Carousel base damage (`CarouselAnomaly`)
- Toxic Cloud base damage (`ToxicCloudDamage`)
- Steam base damage (`SteamAnomalyDPS`)
- Clicker health damage (`ClickerAnomalyHit`)
- Flycatcher movement speed (`FlycatcherVelocityChange`)
- Flycatcher turn speed, using one UI value for both `FlycatcherTurnRateChangeYaw` and `FlycatcherTurnRateChangePitch`
- Razor health damage, scaling both `RazorAnomalyDamageDPSLow` and `RazorAnomalyDamageDPSHigh`
- Razor bleeding, scaling both `RazorAnomalyLowBleed` and `RazorAnomalyHighBleed`
- Soap Bubble health damage (`SoapBubbleDamage`)
- Fireball health damage (`FireBallAnomaly`)
- Diamond health damage (`DiamondDPS`)
- Diamond bleeding (`DiamondBleeding`)
- Psy anomaly intensity (`PsyAnomalyIncreaseRegen`)

## Flycatcher UI semantics

The raw Flycatcher CFG stores **negative change values**:

- `FlycatcherVelocityChange`: `ValueMin = -50%`, `ValueMax = -50%`
- `FlycatcherTurnRateChangeYaw`: `ValueMin = -0.7`, `ValueMax = -0.7`
- `FlycatcherTurnRateChangePitch`: `ValueMin = -0.7`, `ValueMax = -0.7`

Those raw values are correct for the engine but poor player-facing controls. The UI therefore shows the **speed that remains** while the effect is active:

| UI control | Vanilla UI value | Raw vanilla CFG | Future writer transform |
| --- | ---: | ---: | --- |
| Flycatcher Movement Speed | 50% | -0.50 | `CFG = UI - 1.0` |
| Flycatcher Turn Speed | 30% | -0.70 yaw / -0.70 pitch | `CFG = UI - 1.0` to both targets |

This keeps the direction intuitive: higher UI values mean the player can move/turn faster and the anomaly slows them less.

Yaw and pitch intentionally share one player-facing **Turn Speed** slider. The runtime values are identical and splitting them would expose an unnecessary technical distinction.

Projectile slowdown and HP/stamina regeneration penalties remain vanilla and are not exposed by these controls.

## Clicker UI semantics

`ClickerAnomaly` applies three separate interaction effects:

- `ClickerAnomalyHit` — direct health damage
- `ClickerCorrosion` — equipment corrosion
- `ClickerFlasBangComposite` — flash/concussion presentation

The tuner exposes **only Clicker Health Damage**. In runtime CFG 2.0.5, `ClickerAnomalyHit` uses `ValueMin = 70`, `ValueMax = 70`, Burn damage. Radius, charge time, cooldown, equipment corrosion and flash behavior remain vanilla.

## Razor semantics

Razor is intentionally split into two player-facing controls rather than one generic strength slider:

- **Razor Health Damage** scales `RazorAnomalyDamageDPSLow` (20-25 vanilla) and `RazorAnomalyDamageDPSHigh` (100 vanilla) together.
- **Razor Bleeding** scales `RazorAnomalyLowBleed` (20 vanilla) and `RazorAnomalyHighBleed` (60 vanilla) together.

The low/high movement thresholds stay vanilla, so the original velocity-dependent design is preserved. Razor corrosion/equipment damage remains untouched.

## Other newly exposed anomaly effects

| UI control | Runtime effect | Vanilla |
| --- | --- | ---: |
| Soap Bubble Health Damage | `SoapBubbleDamage` | 100 |
| Fireball Health Damage | `FireBallAnomaly` | 100 |
| Diamond Health Damage | `DiamondDPS` | 15 |
| Diamond Bleeding | `DiamondBleeding` | 10 |
| Psy Anomaly Intensity | `PsyAnomalyIncreaseRegen` | -1.7 raw (`DegenPsyPoints`) |

The Psy control is shown as a positive 0-200% intensity multiplier even though the raw runtime value is negative. 100% preserves -1.7, 0% removes the mapped effect, and 200% scales it to -3.4.

## Still intentionally excluded

Additional anomaly systems still contain behavior that is not appropriate for a clean difficulty slider, including Gravity-family behavior, Lava Lamp, Expulsion, Iron Wind/Bulb-related behavior, Razor corrosion and the non-damage parts of Soap Bubble/Fireball behavior.

Weather and emission scheduling/frequency remain out of scope.
