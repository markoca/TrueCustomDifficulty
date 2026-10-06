# Non-Generator Control Verification — v0.2.43

Runtime/source baseline: STALKER 2 CFG dump 2.0.5 and the project's locked writer baseline.

## Result

This pass re-audits the normal non-generator difficulty UI. It does **not** promote a field merely because it exists in CFG. A normal control must have a complete source surface, a defensible player-facing meaning, and a transform that does not pretend an unknown engine formula is known.

| Domain | Normal UI controls | Static-confirmed | Runtime-test-required | Decision |
|---|---:|---:|---:|---|
| Combat | 25 | 13 | 12 | KEEP CURRENT SURFACE |
| Health | 15 | 12 | 3 | KEEP CURRENT SURFACE |
| Stamina | 13 | 12 | 1 | KEEP CURRENT SURFACE |
| Survival | 23 | 23 | 0 | KEEP CURRENT SURFACE |
| Economy | 20 | 17 | 3 | KEEP CURRENT SURFACE |
| Equipment | 8 | 7 | 1 | KEEP CURRENT SURFACE |
| **Total** | **104** | **84** | **20** | **0 newly promoted controls** |

## Locked NO-UI boundaries

- **Combat:** keep `AccumulatedDamageReductionCurveWeightMaxDistance`, `NPC_Armor_Strike_Add`, `NPC_Weapon_Rank_Add`, other zeroed armor-strike additions, low-level recoil/dispersion and AI perception out of the normal difficulty UI.
- **Health:** keep `HPThresholdToKill`, `RegenHealthModifier`, medical effect `Duration`/`Charges`, medical `InventoryActionTime`, and NPC wounded-state helpers out until their engine/layering semantics justify a player-facing control.
- **Stamina:** Walk drain remains NO-UI. Sprint disable/recovery thresholds, low-SP emergency recovery helpers, SprintUnderRunSpeed/Run/Jogging/SlowRun state coefficients and other movement-helper fields remain audit-only. `Aim` remains the intentional exception because it maps directly to the player aiming state.
- **Survival:** negative-state activation thresholds stay vanilla. Thirst remains deferred because the dump does not expose a complete active thirst chain. Sleep hunger/emission sleep and ambiguous movement penalties remain unpromoted.
- **Economy:** `EconomyDifficulty.Repair_Cost` stays hidden to avoid a duplicate global repair-price layer. Trader aggregate stock/refresh and trader-specific buy/sell modifiers remain outside the global difficulty UI. Human NPC Detector Chance remains NO-UI by design even though its generator surface can be mapped.
- **Equipment:** protection caps, per-weapon jam durability thresholds, low-durability rounding helpers, armor protection values and general per-item weight rewrites remain outside this difficulty editor.

## Runtime-test backlog retained

Runtime-test-required does not mean invalid mapping. It means the CFG path/transform is structurally supported but final engine behavior still needs later original-vs-override/runtime verification. This pass deliberately preserves those statuses instead of promoting them to confirmed.

## Release decision

The six non-generator domains are considered **coverage-audited** at the current CFG baseline. Future additions should require new source evidence or a deliberate scope change, not another sweep of already rejected fields.

## v0.9.26.44 anomaly damage display

All direct anomaly **health-damage** controls use vanilla-relative `x` presentation and expose live **Effective Damage** after the current Global Anomaly Damage multiplier. The covered controls are Electro, Chemical, Fire, Carousel, Toxic Cloud, Steam, Clicker, Razor, Soap Bubble, Fireball and Diamond.

Razor is not flattened into a fake single raw value: its vanilla source has low-speed damage `20-25` and high-speed damage `100`, so Effective Damage preserves and displays both tiers.

Non-health effects such as Flycatcher movement/turn penalties, Razor Bleeding, Diamond Bleeding and Psy drain remain separate effect controls and are not labeled as Effective Damage.

