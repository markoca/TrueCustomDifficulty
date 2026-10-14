# Anomaly Identity and Runtime-Visibility Audit — 2026-10-14

## Outcome

- **RazorAnomaly = Glass Shards** (floating airborne shards of glass). This identification is supported by its source anomaly CFG and independent reports of actual gameplay. Rename UI labels, tooltips and player-facing rationales to **Glass Shards**; keep the internal `RazorAnomaly*` source SIDs and preset parameter IDs intact.
- **DiamondAnomaly is not RazorAnomaly.** Both have separate blueprint SIDs in `AnomalyPrototypes.cfg`. Diamond uses `BP_DiamondAnomaly` and effects `DiamondDPS`, `DiamondBleeding`, `DiamondEquipDPS`. Its prototype and damage parameters exist in published CFG snapshots. Yet community identification reports as late as late 2025/early 2026 list Diamond's in-game location/appearance as **unknown**. This is **not proof it is unused/cut**. Its two TCD editor controls are omitted from normal UI until a confirmed game-world encounter/source demonstrates their gameplay relevance.
- Preserve `environment.diamondHealthDamage` and `environment.diamondBleeding` in the catalog and BPatch writer, including original raw source IDs, so existing player presets do not lose data. Both catalog entries now have `exposeInUi=false`; neither should be a damage-radar lane or special-effects aggregate member. Regression tests may deliberately edit both IDs to verify backwards-compatible writer semantics; that does **not** imply a visible control or verified gameplay effect.

## Primary CFG evidence

- `Stalker2/GameLite/GameData/AnomalyPrototypes.cfg` from the community CFG archive:
  - `RazorAnomaly`: `BP_RazorAnomaly` blueprint, gravity element, speed threshold, `RazorAnomalyDamageDPSLow/High`, `RazorAnomalyLowBleed/HighBleed` and `RazorAnomalyCorrosionDPS` interaction effects.
  - `DiamondAnomaly`: separate `BP_DiamondAnomaly` blueprint and `DiamondDPS`, `DiamondBleeding`, `DiamondEquipDPS` interaction effects.
  - CFG link: https://github.com/chrisvblemos/stalker2cfg/blob/main/Stalker2/GameLite/GameData/AnomalyPrototypes.cfg
- `Stalker2/GameLite/GameData/EffectPrototypes.cfg` contains each independent Razor/Diamond source effect:
  https://github.com/chrisvblemos/stalker2cfg/blob/main/Stalker2/GameLite/GameData/EffectPrototypes.cfg
- Our newer independently audited runtime source values are documented in `ANOMALIES-MAPPING.md` and `DEEP-CFG-AUDIT-PHASE12-ENVIRONMENT-2026-10-09.md`; these documents establish CFG mapping, **not** game-world spawning or exact engine formulas.

## Independent gameplay/community corroboration

- Stalker Wiki, **Razor**: identifies glass shards levitating above ground, inflicting damage, bleeding and equipment wear, dependent on traversal speed. https://stalker.fandom.com/wiki/Razor
- Nexus, **Anomaly Names and Identifiers**: `RazorAnomaly` explicitly labelled floating glass shards, but `DiamondAnomaly` described as **UNKNOWN**. https://www.nexusmods.com/stalker2heartofchornobyl/mods/1876
- Nexus, **Anomaly Delete (Modular)**: author also cannot identify Diamond in gameplay, despite cataloguing its prototype. https://www.nexusmods.com/stalker2heartofchornobyl/mods/1830
- Stalker Wiki datamining talk: Razor and Diamond have different blueprints even though some Razor textures are internally named Diamond. https://stalker.fandom.com/wiki/Talk:Glass_Shards

## UI and compatibility contract

- Native editor: 10 anomaly damage lanes, including one **GLASS SHARDS** lane, and a Flycatcher-side **GLASS SHARDS BLEEDING** source card centered with max width 280 DIP; keep separate Psy Drain.
- No visible Diamond lane, effect card, fallback editor, or aggregate-member reset behavior.
- Catalog remains 17 Environment parameters; **15** appear through the current player-facing anomaly controls. Diamond targets remain explicitly documented CFG-only legacy entries; don't silently erase their ability to import old preset values.
- No changes to damage numbers, multiplier formulas, `MinSpeed`/`MaxSpeed` thresholds, physical source edit paths or existing 90 Environment writer scenarios.
- Verification gate in `tools/audit_project_consistency.py` asserts current labels, source mapping, exclusion from visible editors and preservation of catalog/writer targets.

## What would justify re-exposing Diamond?

A recorded encounter/location, confirmed world-spawn reference or repeatable game runtime test proving `DiamondAnomaly` is instantiated and the two effect prototypes influence it. Presence of `DiamondAnomaly` in a CFG prototype **alone** does not prove normal-game use.
