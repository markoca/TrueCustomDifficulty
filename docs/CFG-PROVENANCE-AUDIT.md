# CFG Provenance Audit — v0.2.54

## Purpose

This audit closes the static provenance chain behind the CFG writer without introducing any EXE/Wine/runtime verification workflow.

The question is deliberately narrow:

> For every writer target, do we have evidence that the targeted CFG file/root/node/key/source value came from the vanilla runtime source or from a later source inventory derived from that runtime mapping work?

Runtime gameplay behavior remains a separate concern for controls still marked `runtime_test_required`.

## Important evidence distinction

The original STALKER 2 runtime **2.0.5** CFG dump is **not bundled** in this source package and is no longer physically available in the current working storage. Therefore v0.2.54 does **not** pretend to freshly re-read the raw game dump.

Two provenance classes are kept separate:

1. **Historical runtime exact proof** — the retained v0.1.63 audit checked the then-current direct writer surface against the exact supplied runtime 2.0.5 dump, fingerprinted as:

   `739ecb4a60e2d98181cc6b326894d184b76a09c29b94a66c18d7b0da2c37a83a`

   That audit recorded **699/699 exact** ordinary edit points and separately checked Additional Mechanics, Corpse Smart Loot, PSY overrides.

2. **Fresh embedded-source proof** — writer surfaces added after that audit are rechecked in v0.2.54 against the source inventories/manifests retained under `docs/generators/` and `Data/`.

This distinction is intentional. `HISTORICAL_PASS` is not presented as a fresh raw-dump diff.

## Direct CFG edit points

Current direct writer inventory: **1370** edit points.

The count closes exactly:

| Provenance class | Edit points | Result |
|---|---:|---|
| Historical runtime 2.0.5 exact audit | 699 | `HISTORICAL_PASS` |
| Fresh embedded-source exact audit | 671 | `PASS` |
| **Total** | **1370** | **covered** |

The 671 later points are exactly four promoted/direct surfaces:

| Parameter | Edit points | Fresh source evidence |
|---|---:|---|
| `loot.droppedWeaponCondition` | 242 | `generator-local-source-item-entries.csv` |
| `loot.npcWeaponAmmoSupply` | 242 | `generator-local-source-item-entries.csv` |
| `economy.medicStock` | 96 | `trader-local-source-entries.csv` |
| **Total** | **671** | **671/671 exact** |

For every one of those 671 entries, the audit checks the concrete source identity used by the writer: prototype/root, category node, item node, field/key and original value.

The retained 699-entry current tuple set is fingerprinted in `summary.json` so it cannot silently drift after v0.2.54 without the consistency audit failing.

## Special writer routes

Current special writer route inventory: **25** parameters.

| Provenance class | Routes | Result |
|---|---:|---|
| Historical runtime-structure exact proof | 18 | `HISTORICAL_PASS` |
| Fresh clone/redirect source proof | 7 | `PASS` |
| **Total** | **25** | **covered** |

Historical structural proof covers:

- 5 `AdditionalMechanicsEffects` list-membership controls;
- 1 `CorpseSmartLoot.GeneralNPC` set-list control;
- 4 PSY array controls;
The 7 later clone/redirect routes are freshly checked against their vanilla source templates and audited production redirect manifests. Because Cheap Stash shares one production template between two controls, the 7 routes are backed by 6 source-template groups.

Fresh template result:

| Source-template group | Source fields matched |
|---|---:|
| Cheap Stash | 244/244 |
| Medium Stash | 188/188 |
| Expensive Stash | 229/229 |
| Destructible Ammo | 36/36 |
| Destructible Grenade | 12/12 |
| NPC Medical pools | 488/488 |
| **Total** | **1197/1197** |

Their canonical production redirect manifests contain **3476** source-grounded rows in total. Each retained manifest row carries the target source file/prototype/node/key/source value and an evidence line. Where a historical docs mirror exists it must match; where cleanup intentionally removed that mirror, the canonical `Data/` manifest must retain complete source-evidence fields.

## Result

Static CFG provenance is now closed at the level the project can honestly prove without rebundling the original game dump:

- **1370/1370** direct writer edit points provenance-covered;
- **25/25** special writer routes provenance-covered;
- later direct source-value mismatches: **0**;
- later special source-template mismatches: **0**;
- unsupported CFG writer routes: **0**.

Together with `CFG-PLACEMENT-AUDIT.md`, the static chain is now:

```text
UI/catalog parameter
  -> writer route
  -> CFG file
  -> physical prototype/root
  -> node path
  -> field/key
  -> operation
  -> source provenance
```

This proves where the tool writes and what vanilla/source structure that mapping came from. It does **not** automatically prove the final in-game behavior of controls that remain runtime-test-required.

## Machine-readable evidence

Canonical evidence lives under:

`docs/cfg-provenance/v0.2.54/`

Files:

- `summary.json`
- `direct-provenance.csv`
- `special-provenance.csv`
- `README.md`

Regenerate with:

```bash
python tools/audit_cfg_provenance.py
```

## v0.3.2 shared Cheap-stash quantity route

`loot.cheapStashGrenadeQuantity` reuses the already source-grounded Cheap-stash template/redirect surface. The current special-route projection is 26 routes: 18 historical runtime-structure routes plus 8 later clone/redirect routes. The added route does not add a new template or redirect surface; it controls the existing `GrenadeRGD5.MinCount/MaxCount` fields already present in the source-grounded Cheap template.
