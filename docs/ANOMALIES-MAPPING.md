> **Phase 12 (2026-10-09):** The current 17 Environment backing source parameters map to 42 physical vanilla CFG fields. An independent audit of the original GameData ZIP matched all 42 values exactly. CI tests run 90 real ViewModel→BPatch cases. Runtime Unreal effects remain unverified. See [Phase 12](DEEP-CFG-AUDIT-PHASE12-ENVIRONMENT-2026-10-09.md).

# Anomalies Mapping

## Current visual layout (2026-10-14)

The current `environment` tab exposes **15 source-backed controls** from a preserved catalog of **17 CFG parameters**: Global Anomaly Damage (1), 10 individual anomaly damage sources, two Flycatcher movement/turning axes, Glass Shards Bleeding (1), and Psy Drain (1). The two Diamond source parameters remain catalogued for legacy preset/CFG compatibility but have **no player-facing controls**: no independently confirmed in-game encounter or spawn has been identified. This is *not* evidence the Diamond CFG prototype is absent. See [Anomaly identity audit](ANOMALY-IDENTITY-AUDIT-2026-10-14.md).

The global damage knob and ten native individual-damage vertical rails remain in the same unified `AnomalyDamageRadar` card. Each rail continues to show actual source damage relative to its own reference (except Glass Shards' dedicated 0–2x coefficient). Flycatcher keeps its two-axis interactive graph; the same right-side board now shows just **GLASS SHARDS** bleeding, centered at a maximum of 280 DIP, and **PSY DRAIN** beneath it. Diamond leaves no empty slot. The separate generic Special Effects family, if rendered, offers only Glass Shards Bleeding and Psy Drain. No new CFG source, source scaling or gameplay mechanic is introduced.

The `RazorAnomaly` CFG SID is the in-game **Glass Shards** (floating broken glass) anomaly. Rename only visible UI/tooltip/catalog labels; retain original `environment.razorHealthDamage`, `environment.razorBleeding` preset IDs and `RazorAnomaly*` source prototypes. Global multiplier/protection and Razor's original speed thresholds remain independent of the two editable sources.

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
- Glass Shards health damage (`RazorAnomaly`), scaling both `RazorAnomalyDamageDPSLow` and `RazorAnomalyDamageDPSHigh`
- Glass Shards bleeding (`RazorAnomaly`), scaling both `RazorAnomalyLowBleed` and `RazorAnomalyHighBleed`
- Soap Bubble health damage (`SoapBubbleDamage`)
- Fireball health damage (`FireBallAnomaly`)
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

## Glass Shards (Razor CFG) semantics

Glass Shards, internally `RazorAnomaly`, is intentionally split into two player-facing controls rather than one generic strength slider:

- **Glass Shards Health Damage** scales `RazorAnomalyDamageDPSLow` (20-25 vanilla) and `RazorAnomalyDamageDPSHigh` (100 vanilla) together.
- **Glass Shards Bleeding** scales `RazorAnomalyLowBleed` (20 vanilla) and `RazorAnomalyHighBleed` (60 vanilla) together.

The low/high movement thresholds stay vanilla, so the original velocity-dependent design is preserved. Glass Shards equipment corrosion remains untouched.

## Other newly exposed anomaly effects

| UI control | Runtime effect | Vanilla |
| --- | --- | ---: |
| Soap Bubble Health Damage | `SoapBubbleDamage` | 100 |
| Fireball Health Damage | `FireBallAnomaly` | 100 |
| Psy Anomaly Intensity | `PsyAnomalyIncreaseRegen` | -1.7 raw (`DegenPsyPoints`) |

The Psy control is shown as a positive 0-200% intensity multiplier even though the raw runtime value is negative. 100% preserves -1.7, 0% removes the mapped effect, and 200% scales it to -3.4.

## CFG retained but excluded from the player-facing interface

The standalone `DiamondAnomaly` prototype is present in `AnomalyPrototypes.cfg` and refers to `DiamondDPS` and `DiamondBleeding` effects. Unlike Glass Shards, its game-world identity/placement has not been confirmed. Their two existing parameter IDs, baseline sources and writer tests are retained for round-tripping legacy presets, but neither editor is rendered. This can be reconsidered after explicit spawn/gameplay evidence.

## Still intentionally excluded

Additional anomaly systems still contain behavior that is not appropriate for a clean difficulty slider, including Gravity-family behavior, Lava Lamp, Expulsion, Iron Wind/Bulb-related behavior, Razor corrosion and the non-damage parts of Soap Bubble/Fireball behavior.

Weather and emission scheduling/frequency remain out of scope.
