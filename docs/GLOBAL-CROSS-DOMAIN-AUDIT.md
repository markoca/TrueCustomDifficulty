## 2026-10-19 — Current food-satiety extension

Current cross-domain census is **287 catalog IDs, 182 exposed definitions, 105 hidden**. Four independent `survival.*Satiety` food hunger-relief controls add four visible Survival sliders (Survival 35 visible, previous 31) and eight original `GameData/EffectPrototypes.cfg` direct writer leaves. Existing locked snapshots below describe the historical census and must not be read as an updated implementation inventory. Audit status remains **117 confirmed / 65 runtime-test-required** among 182 exposed settings. No claim of .NET/Wine/in-game verification.

# Global Cross-Domain Control Audit — v0.2.50

## 2026-10-14 — Environment anomaly identity UI correction (current)

The 247 original catalogued parameters and their CFG writer mappings remain. `environment.diamondBleeding` is now explicitly `exposeInUi=false` and `environment.diamondHealthDamage` remains catalog-hidden because their `DiamondAnomaly` prototype exists, but an in-game encounter is not confirmed. Player-facing Environment visible anchor/source accounting falls from **7 to 6** (global visible total **149→148**, hidden **98→99**, confirmed-visible **118→117**, native slider-visible **135→134**). The actual player-facing Anomalies aggregate shows 15 real controls: one global damage knob, 10 anomaly damage lanes, two Flycatcher axis sources, Glass Shards Bleeding and Psy Drain. Both Diamond original CFG targets remain available for legacy preset writer compatibility. `RazorAnomaly` is the **Glass Shards** anomaly; labels change but source IDs and CFG operations do not. See [Anomaly identity audit](ANOMALY-IDENTITY-AUDIT-2026-10-14.md).


### 2026-10-10 — Beer and Vodka independent RAD removal (latest binding contract)

Within the **single MEDICAL ITEMS card** the right-hand RADIATION REMOVAL bank now has **four compact vertical sliders on one row**: **SCIENTIFIC MEDKIT**, **ANTI-RAD**, **BEER** and **VODKA**. There is no second-row, duplicate standalone Radiation Treatment card, or separate consumable alcohol antirad slider. The RAD bank is given additional horizontal space by tightening the four gold HEAL / copper BLEED pairs on the left; at least 820 DIP board width is reserved so all 12 numeric columns remain one row without clipping. Each column uses the shared horizontal cap/white-on-hover and independent source-native drag/keyboard/right-click/R reset; the RAD bank keeps a separate teal 0–200 vertical scale, with numbers not conflated with HP or bleeding points.

**Actual original CFG prototypes:** `BeerAntirad1.ValueMin/ValueMax=-10/-10`; `VodkaAntirad3.ValueMin/ValueMax=-50/-50`. New independent positive-unit control IDs: `health.beerRadiationRemoval` (vanilla **10 RAD**, **0–20 RAD**, 5-RAD native steps) and `health.vodkaRadiationRemoval` (vanilla **50 RAD**, **0–100 RAD**, 5-RAD native steps). Scientific Medkit remains 60 RAD/120 max; Anti-Rad 100 RAD/200 max. Both new controls write exclusively to their own source prototypes via the existing positive-to-negative `negateSet` operation; neither affects intoxication, drink duration, alcohol tolerance or the other drink. The previous `survival.alcoholRadiationRemoval` shared 0–2x parameter and its common Beer/Vodka CFG ownership have been retired from the catalog/Consumable Effects UI. **Preset compatibility:** when an old preset still contains the shared multiplier and lacks new individual drink values, load converts it to Beer `10 × old multiplier` and Vodka `50 × old multiplier`, retaining even fractional authored RAD amounts; new presets persist the two independent positive-unit values. Thus default 1x restores 10/50 without changing how the original game works.

The existing Medical Items anchor `health.bandageHealing` reflects changes in every HEAL/BLEED/RAD source plus Food Healing, but production CFG writing remains strictly per-source. No new effect, source ID collision or duplicate writer output is allowed. This current specification overrides historical shared-alcohol and two-RAD-bank UI descriptions.

This audit treats the complete player-facing catalog as one surface instead of auditing tabs independently.

## UI surface

- Catalog parameters: **178**
- Visible gameplay controls: **167**
- Audit-only / hidden parameters: **11**
- Visible status split: **137 confirmed + 30 runtime-test-required**
- Control types: **159 sliders + 7 toggles + 1 choice slider**
- Duplicate player-facing labels: **0**
- Vanilla numeric values outside their current slider range: **0**
- Generator-backed controls: **27** = 16 confirmed + 11 runtime-test-required
- Non-generator visible controls: **140** = 121 confirmed + 19 runtime-test-required

Visible controls by tab:

| Tab | Controls |
|---|---:|
| Combat | 25 |
| Health | 15 |
| Stamina | 13 |
| Survival | 23 |
| Economy | 20 |
| Equipment | 8 |
| Master | 20 |
| Environment | 17 |
| Loot | 26 |
| **Total** | **167** |

The older **104** count remains valid only for the six-tab verification scope (Combat/Health/Stamina/Survival/Economy/Equipment). It is not the total UI count. `economy.medicStock` belongs to that six-tab count and is also one of the 27 generator-backed controls, so category totals must be treated as overlapping sets rather than added blindly.

## Hard writer-conflict check

Concrete writer targets were keyed by `(scope, file, prototype, nodePath, key)`. There are **0 collisions between different parameter IDs**. No two controls write the same concrete scalar/list field through independent writer routes.

The only intentional shared declared target is `EnvironmentDifficulty.AdditionalMechanicsEffects`. Five Master toggles use list-membership semantics, each with a distinct member:

- `advanced.lowHealthMechanics` → `RegenHealthNegativeMechanics`
- `advanced.starvationMechanics` → `HungerNegativeMechanics`
- `advanced.radiationMechanics` → `RadiationNegativeMechanics`
- `advanced.fatigueMechanics` → `SleepinessNegativeMechanics`
- `advanced.masterEffectModifierMechanics` → `MasterEffectModifierNegativeMechanics`

This is safe set-membership composition, not a target collision.

## Intentional cross-domain composition

Two cross-domain relationships are deliberate and must not be "deduplicated":

1. **Master mechanics toggles + child tuning sliders.** The toggle controls whether the mechanics bundle is active; the child sliders tune global effect prototypes. All **15** dependent sliders have a valid `requiresToggle` parent and are disabled in the UI while that parent is Off.
2. **Global Anomaly Damage + six base anomaly-damage controls.** The difficulty scalar and the individual EffectPrototype base damages are separate layers. The UI explicitly computes/displays the combined effective result for Electro, Chemical, Fire, Carousel, Toxic Cloud and Steam.

Shared parent CFG nodes such as `Player.VitalParams` and `DifficultyPrototypes.EnvironmentDifficulty` are only structural containers. Their child keys remain distinct, so they are not cross-control writer conflicts.

## v0.2.50 sign normalization

The global scan found the last four visible sliders still exposing engine-negative deltas even though their labels describe penalty/loss magnitude. They now store positive player-facing magnitudes and use `negateSet` when generating CFG:

| Control | UI vanilla | CFG vanilla |
|---|---:|---:|
| Master Low-Health Recovery Penalty | 70% | -70% |
| Starvation Health Recovery Penalty | 100% | -100% |
| Severe-Fatigue Max Stamina Loss | 20% | -20% |
| Severe-Fatigue Carry Capacity Loss | 20% | -20% |

Pre-v0.2.50 presets with negative values are migrated with absolute magnitude on load. This changes only UI representation; generated gameplay values remain identical.

## Decision

The current UI contains **167 distinct controls with no concrete writer collisions**. No control is removed in this release. The four Master signed-delta controls are normalized for consistent player-facing direction, and the complete UI/control accounting is now guarded as a release invariant.

## v0.3.2 loot-axis update

The catalog now contains **179 parameters / 168 visible controls / 11 hidden audit-only entries**. The added visible control is `loot.cheapStashGrenadeQuantity`, giving Cheap-stash RGD-5 a real independent Chance + Quantity pair. Current visible status split is **137 confirmed + 31 runtime-test-required**. Generator-backed visible controls are now **28**; non-generator visible controls remain 140.

## 2026-11-13 — Remove World Armor (active source-backed control)

A single default-OFF **REMOVE WORLD ARMOR** checkbox is added to **World Item Spawns** in the Loot tab. It shares the existing compact right-hand toggle column, which now has four 44-pixel-high toggles at 49-pixel pitch, rather than adding a new card or slider. The World Item Spawns aggregate retains one visible anchor; this checkbox is an independently bound control.

When enabled at Build, exactly **76 source-audited direct base-world suit/helmet actors**, plus **18 DLC1 direct actors** when installed, are removed using per-ActorSID `removenode` patches. Six independently approved **regular physical-stash helmet item nodes** are removed with indexed ItemGenerator BPatch paths. No broad removal by armor item name is allowed. Vanilla source slots are otherwise inherited.

The source domains are `src/TrueCustomDifficulty/Data/world-armor-direct-manifest.csv` and `world-armor-stash-manifest.csv`; only safe `WorldMap_WP` normal wearable pickups and independently established non-quest stash generator/actor pairs qualify. Quest/story suits including X7, quest-linked stash armor, named-clue/shared reward generator slots, protected stashes, NPC/player equipment and merchant inventory are excluded. If Extra Stash Gear is also enabled, it keeps other bonus gear classes but suppresses its Armor bonus class, so this removal is not silently undone.

One toggle, not a second stash checkbox: `loot.worldArmorRemoved` with independently serialized ON/OFF default OFF. The source control is #286 (previously 285) and the player-visible control inventory is #181 (previously 180). The historical 180-control RC copy freeze predates this addition and remains an archive; this control has a new reviewed copy row. The real-world game result requires in-game validation, particularly with previously opened stashes.

## 2026-11-13 — Weapon and Armor Weight (CURRENT)

Equipment / Item Weight now has **four independent native horizontal source cards**: AMMO WEIGHT, FOOD & MEDICINE, WEAPON WEIGHT and ARMOR WEIGHT. The two new sliders use 0–2× / 0.01× steps, reference 1×, editable per-source preset storage, same hover highlights, native markers, keyboard, mouse, individual right-click reset and group R reset. This is a compact 2×2 bank (≥672 DIP, 248 DIP total) or a 4×1 column (<672 DIP, 436 DIP total); both measured and rendered geometry derive from `EconomyBoardHeight(width, 4)`. The original first two cards, Equipment Wear X/Y board and tab navigation stay unchanged.

The production source reader selected **112 base + 12 DLC1 weapons** (one tutorial gun omitted) and **86 base + 8 DLC1 suits/helmets** from the actual decoded `ItemPrototypes.cfg` `Type=Weapon/Armor` inheritance and `Weight`. Two separate direct CFG IDs `equipment.weaponWeight` and `equipment.armorWeight` scale their own resolved source weights, 1× produces no patch, 0× writes zero, and 2× doubles mass. Each exact concrete child overrides `Weight` even when that field was originally inherited. No templates or original zero-weight items become nonzero. Special named variants, NPC gear and quest-owned equipment with normal nonzero `Weight` remain in scope because this is an **item mass setting**, not a spawn/availability/removal setting.

Exact source and exclusions are tracked in `src/TrueCustomDifficulty/Data/equipment-weapon-armor-weight-source.csv` (218 distinct base/DLC1 rows), with accompanying `docs/equipment/WEAPON-ARMOR-WEIGHT-SOURCE-2026-11-13.md`. Direct writer entries increase from 1230 to 1448; total catalog dimensions 286 → 288 and player-visible sources 181 → 183. All twelve stock presets add independent neutral 1× values. Existing player-customized presets keep their values, and only byte-identical previous factory JSONs migrate. Gameplay and DLC load-order acceptance remain separate from source parity and Windows BPatch tests.

