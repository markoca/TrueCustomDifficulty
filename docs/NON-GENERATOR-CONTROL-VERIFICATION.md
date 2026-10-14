# Non-Generator Control Verification — v0.2.43

### 2026-10-14 — Equipment has its own tab again (CURRENT)

The non-generator normal UI inventory is **Combat 17 (13 confirmed / 4 runtime), Health 13 (10/3), Stamina 7 (6/1), Survival 31 (29/2), Economy 17 (12/5), Equipment 4 (3/1)**: **89 visible non-generator sources** total. The Equipment tab contains exactly the existing two groups Equipment Wear and Item Weight (four preserved `equipment.*` IDs); their 113 source-backed writer leaves are counted under Equipment rather than Survival. Survival retains 82 independent leaves. Prior 2026-10-13 Equipment-in-Survival numbers below are historical and superseded.

### 2026-10-13 — Current non-generator UI ownership (CURRENT; supersedes historical Equipment-tab inventory)

The current normal UI inventory is **Combat 17 (13 confirmed / 4 runtime), Health 13 (10/3), Stamina 7 (6/1), Survival 35 (32/3), Economy 17 (12/5)**, with **no separate Equipment tab**. The four stable Equipment source IDs `equipment.armorDurability`, `equipment.weaponWearPerShot`, `equipment.ammoWeight` and `equipment.consumableWeight` are **presented in Survival** without changing ownership of their original CFG targets. The total remains **89 visible non-generator controls** across these groups, unchanged by moving four records between display categories. The 195 current Survival-associated writer points consist of 82 originally under Survival and 113 moved Equipment points; this is a display/inventory reassignment, not a new writer or runtime effect. `tools/audit_project_consistency.py` now asserts this explicit breakdown and preserves the audit-only hide contracts. Older tables in this document are historical, not current UI tab counts.

### 2026-10-10 — Beer and Vodka independent RAD removal (latest binding contract)

Within the **single MEDICAL ITEMS card** the right-hand RADIATION REMOVAL bank now has **four compact vertical sliders on one row**: **SCIENTIFIC MEDKIT**, **ANTI-RAD**, **BEER** and **VODKA**. There is no second-row, duplicate standalone Radiation Treatment card, or separate consumable alcohol antirad slider. The RAD bank is given additional horizontal space by tightening the four gold HEAL / copper BLEED pairs on the left; at least 820 DIP board width is reserved so all 12 numeric columns remain one row without clipping. Each column uses the shared horizontal cap/white-on-hover and independent source-native drag/keyboard/right-click/R reset; the RAD bank keeps a separate teal 0–200 vertical scale, with numbers not conflated with HP or bleeding points.

**Actual original CFG prototypes:** `BeerAntirad1.ValueMin/ValueMax=-10/-10`; `VodkaAntirad3.ValueMin/ValueMax=-50/-50`. New independent positive-unit control IDs: `health.beerRadiationRemoval` (vanilla **10 RAD**, **0–20 RAD**, 5-RAD native steps) and `health.vodkaRadiationRemoval` (vanilla **50 RAD**, **0–100 RAD**, 5-RAD native steps). Scientific Medkit remains 60 RAD/120 max; Anti-Rad 100 RAD/200 max. Both new controls write exclusively to their own source prototypes via the existing positive-to-negative `negateSet` operation; neither affects intoxication, drink duration, alcohol tolerance or the other drink. The previous `survival.alcoholRadiationRemoval` shared 0–2x parameter and its common Beer/Vodka CFG ownership have been retired from the catalog/Consumable Effects UI. **Preset compatibility:** when an old preset still contains the shared multiplier and lacks new individual drink values, load converts it to Beer `10 × old multiplier` and Vodka `50 × old multiplier`, retaining even fractional authored RAD amounts; new presets persist the two independent positive-unit values. Thus default 1x restores 10/50 without changing how the original game works.

The existing Medical Items anchor `health.bandageHealing` reflects changes in every HEAL/BLEED/RAD source plus Food Healing, but production CFG writing remains strictly per-source. No new effect, source ID collision or duplicate writer output is allowed. This current specification overrides historical shared-alcohol and two-RAD-bank UI descriptions.

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

