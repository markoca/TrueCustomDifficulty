## Ordinary stash actor region labels

Regenerate 831 source-backed actor region labels with `python tools/generate_stash_actor_regions.py`; see `docs/STASH-REGIONS.md`. Labels come from generator SID prefixes, not geographic polygons.

## Source-backed spawn-referenced item SID inventory

`docs/SPAWNABLE-ITEM-SIDS.md` lists every item SID backed by a base/DLC1 ItemPrototypes definition and a spawn-related CFG reference; the companion `src/TrueCustomDifficulty/Data/spawnable-item-sids.csv` retains the evidence paths and item type per SID. It is **not** an unconditional in-game spawn list; quest/rank/other gates still apply. The 7.62×39 cut ammo family (`A762D/A762A/A762E`) is excluded.

To regenerate from locally supplied extracted CFG archives (ZIPs are not committed to GitHub):

```bash
python3 tools/generate_spawn_item_sids.py \
  --game "/path/to/STALKER2-GameData-CFG.zip" \
  --dlc "/path/to/STALKER2-DLC1-CFG.zip"
python3 tools/audit_spawn_item_sids.py
```

The static audit also runs with `python3 tools/pre_release.py static`. It checks internal consistency only, not actual gameplay drops.

# External tools

## Reproduce regular-world stash allowlist from supplied CFG dump

Run `python tools/audit_regular_stash_actor_dump.py "STALKER2-GameData-CFG(20261016-142306).zip" --output /tmp/ordinary-stashes.csv --excluded-output /tmp/special-stashes.csv`. The verified user dump contains 902 main-world SmartLoot ItemContainers: 831 ordinary candidates and 71 protected blueprint/unique/quest-linked actors. The corresponding source-backed actor list and all 991 `StashPrototypes.cfg` world item rows are now embedded resources in `src/TrueCustomDifficulty/Data/`. Run `python tools/audit_regular_stash_source_inventory.py` and Windows `tests/LootSpecialGeneratorsContract` to check source semantics; do not claim game success without runtime verification.

## Regular stash source inventory

Run `python tools/audit_regular_stash_source_inventory.py` at the repository root to verify `tools/regular-stash-source-inventory.json` against the source-derived ItemGenerator and SmartLoot catalogs. This is a fail-closed source inventory for six regular stash categories (ammo, medical, food, drinks, grenades, coupons), **not** an active SmartLoot patch or a game runtime test. Source-level consumer edges must be added before introducing any actor-specific quest-safe CFG writer extensions.


## Final source-to-BPatch contract (2026-10-15)

Run `python tools/audit_final_cfg_mapping.py --out audit/final-cfg-mapping`. This validates the **248/248 catalog routes** (213 direct/1,230 concrete leaves, 35 special routes), exact current CFG target ownership, source evidence statuses, production dispatch methods, embedded manifest presence, difficulty/DLC guards, and **all twelve shipped factory profiles' direct changes** against source integer rounding. The `mapping.json`/`mapping.csv` output indexes each ID; the committed `tools/final_cfg_mapping_inventory.json` gives the source-by-source coverage. The independent Windows `tests/AllControlsPatchContract` project exercises the real WPF production writer for all **235** player-editable dimensions. See `docs/FINAL-CFG-MAPPING-AUDIT-2026-10-15.md` for what these checks **cannot** establish about game runtime.

This source directory contains first-party audit/test scripts. The published tools/ folder is for optional manually supplied external packer binaries.

True Custom Difficulty does **not** bundle or download third-party executables. Users supply them manually.

Published layout:

```text
TrueCustomDifficulty.exe
tools/
  repak.exe
  retoc.exe
```

## Required for Build Mod

A non-empty v0.1.51 build requires both tools:

- **repak** packs the generated BPatch CFG files into `TrueCustomDifficulty_P.pak` using PAK V11 and mount point `../../../`.
- **retoc** creates the matching UE5.5 `TrueCustomDifficulty_P.utoc` and `TrueCustomDifficulty_P.ucas` companion container.

The application does not download either executable. If one is missing, Build Mod stops with a clear error and does not publish a partial output.

The same tools may also be used manually for extraction/development/maintenance workflows, but those workflows are outside the normal player build path.

## Override verification harness

Override mapping evidence is documented in `../OVERRIDE-VERIFICATION-HARNESS.md`; there is no automated verification harness in this repository.

## WPF aggregate / toggle visibility regression

UI aggregate bindings are maintained directly in WPF source and the UI documentation.

## Mixer fader pilot regression

The mixer contract is described in the UI design documentation; publishing does not run any UI test scripts.

## CFG placement audit

`python tools/audit_cfg_placement.py --output docs/cfg-placement/v0.2.53` verifies all catalog parameters against their concrete CFG writer routes. It is intentionally static and does not launch the application.

## Pre-release test runner

Run `python tools/pre_release.py list` in the repository root to view the registered tests. The `static`, `source`, `writer` and `all` modes, their platform requirements, evidence reports and mandatory manual game checks are described in [Pre-release testing](../docs/RELEASE-TESTING.md). Test logs and fixtures are written to ignored `audit/pre-release/` and do not form part of the runtime release.

### Original CFG ancestry and special arrays

`audit_cfg_source_values.py` now checks physically declared source
`{refkey=...}` ancestor values, reporting `inherited_exact`
separately from directly authored fields. Wrong, missing or cyclical
ancestry is not silently accepted. The CI-backed
`test_cfg_source_inheritance.py` validates this behavior.
`audit_combat_source_structures.py` checks the exact 68 Combat
source routes plus 14 Master agents / 66 cooldown tags / 2 PSY
effects using actual supplied CFG ZIPs. These are source facts,
not game-engine BPatch runtime tests.

### Health/Stamina direct CFG source and real writer

`audit_health_stamina_source.py` compares 69 source edit points against the original decoded GameData/DLC1 CFG archives, including one `Player→[0]` inherited VaultingParams value. `tests/HealthStaminaPatchContract` executes 46 real WPF model-to-CFG writer scenarios on Windows CI and stores generated patch evidence. Both are registered in `pre_release.py`; neither runs the game.

C2 writer scenario breakdown: 41 live Health/Stamina parameter/difficulty cases plus five Master mechanics toggle full-list replacement cases = 46; five original Stamina speed-state CFG definitions are source-only and are not loaded into the app.

### Phase 9 Survival coverage

`tests/SurvivalPatchContract` checks 59 production C# ViewModel-to-BPatch outputs on Windows. `pre_release.py source` now includes the original-source check for all 139 `survival.*` edit points. Neither source static parity nor generated CFG data proves in-game behavior. Details: [Survival Phase 9](../docs/DEEP-CFG-AUDIT-PHASE9-SURVIVAL-2026-10-09.md).
## Original consumable effect source verification

`python3 tools/audit_consumable_effect_source.py --base-zip "/path/to/STALKER2-GameData-CFG.zip"`

This compares Energy Drink's 5/15/45-second effects (native -100%/-30% SPDrain and regeneration), the regular/Limited/Weird Kettle lists, inherited eight-action scope, and Water duration sources with the original decoded CFG and the production writer baseline. The tool is a source-and-structure check, **not** gameplay stacking proof. It also runs in the source phase of `tools/pre_release.py`.


## Water and Energy short-duration controls

`tools/audit_consumable_effect_source.py --base-zip <original GameData ZIP>` verifies Water 30/5s and Energy 45/15/5s source identity and variant membership. `tools/audit_project_consistency.py` and `tools/audit_ui_cfg_contract.py` verify the three independent source-backed hidden aggregate controls and unique direct CFG route ownership. `tests/SurvivalPatchContract` executes the real WPF model and writer on Windows, including live maximum/cascades for the three short durations. Structural source tests do not prove UE runtime effect stacking.

## Global Drink Duration Override — 2026-10-10

The single universal-minimal `OVERRIDE LIMITS` checkbox in the native ConsumableMixer duration header is saved as preset-root `drinkDurationOverride` (default OFF), not a fake CFG source. OFF preserves per-drink native maxima and source-specific child caps; ON uses the existing common 270s timeline as a ceiling for all five primary and three secondary duration editors, independently. See both UI catalogs. The real Windows `tests/SurvivalPatchContract` exercises all eight values at 180s, the production writer and preset serialization. `tools/audit_project_consistency.py` guards the live binding/render/input code.
