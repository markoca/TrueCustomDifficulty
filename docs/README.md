# Documentation index

- [Final 248-source CFG mapping and production BPatch writer audit (2026-10-15)](FINAL-CFG-MAPPING-AUDIT-2026-10-15.md): direct vs special source coverage, 108 A-Life integer no-op corrections, per-ID inventory, repeatable Python and Windows writer gates, explicit runtime limitations.
- [Consumable medicines — Vinca/Cinnamon, Psi Block, Hercules source audit (2026-10-11)](CONSUMABLE-MEDICINE-CFG-AUDIT-2026-10-11.md): verified source mappings, six implemented UI/CFG editors and outstanding runtime safeguards.
All project documentation lives under `docs/`. The repository root is intentionally reserved for build/run entry points.

## Production and architecture

- [Production implementation](PRODUCTION-IMPLEMENTATION.md)
- [Source layout](SOURCE-LAYOUT.md)
- [Architecture](ARCHITECTURE.md)
- [Preset format](PRESET-FORMAT.md)
- [CFG writer](CFG-WRITER.md)
- [Configuration baseline](CONFIG-BASELINE.md)

## CFG proof chain

- [CFG placement audit](CFG-PLACEMENT-AUDIT.md)
- [CFG provenance audit](CFG-PROVENANCE-AUDIT.md)
- [Global cross-domain audit](GLOBAL-CROSS-DOMAIN-AUDIT.md)
- [Non-generator control verification](NON-GENERATOR-CONTROL-VERIFICATION.md)
- [Override verification harness](OVERRIDE-VERIFICATION-HARNESS.md)

## Gameplay mappings / deep audits

- [Combat mapping](COMBAT-MAPPING.md) / [deep audit](COMBAT-CONTROL-DEEP-AUDIT.md)
- [Health mapping](HEALTH-MAPPING.md) / [deep audit](HEALTH-CONTROL-DEEP-AUDIT.md)
- [Stamina mapping](STAMINA-MAPPING.md) / [deep audit](STAMINA-CONTROL-DEEP-AUDIT.md)
- [Survival mapping](SURVIVAL-MAPPING.md) / [deep audit](SURVIVAL-CONTROL-DEEP-AUDIT.md)
- [Economy mapping](ECONOMY-MAPPING.md) / [deep audit](ECONOMY-CONTROL-DEEP-AUDIT.md)
- [Equipment mapping](EQUIPMENT-MAPPING.md) / [deep audit](EQUIPMENT-CONTROL-DEEP-AUDIT.md)
- [Loot mapping](LOOT-MAPPING.md) / [v0.3.2 ammo-content evidence](loot/v0.3.2/README.md)
- [Build-time crate reduction](CRATE-SPAWN-AUDIT.md)
- [Master mapping](MASTER-MAPPING.md)

## Current SmartLoot production extension (2026-10-16)

- [Actual CFG-dump world SmartLoot extension](generators/SMARTLOOT-PRODUCTION-EXTENSION-2026-10-16.md) — 13 source prototypes / 991 exact rows / 831 regular item-container actors, plus 71 protected Blueprint/Unique/Quest-linked actors. Embedded source CSVs and .NET output tests; gameplay validation pending.

## Generator research

- [Stash Weight / Chance source classification (2026-10-17)](STASH-WEIGHT-CHANCE-SOURCE-AUDIT-2026-10-17.md) — direct ItemGenerator, shared tier wrappers, 13 SmartLoot prototypes, nested generator weights, DLC1, physical/quest stash and corpse-clue discovery boundaries; research-only, no gameplay or UI changes.


- [Regular stash ordinary categories — ItemGenerator + SmartLoot inventory (2026-10-16)](generators/STASH-REGULAR-CATEGORIES-AUDIT-2026-10-16.md): 17 core generator SIDs, 13 SmartLoot generator SIDs, 2,143 production edges, explicit uniqueness/quest exclusions and missing manifest limitation. Source audit only, not gameplay changes.

- [Medical stash generators — all audited source families (2026-10-16)](generators/STASH-MEDICAL-ALL-SOURCE-AUDIT-2026-10-16.md): six medical GamePass ItemGenerator cores, untouched medical SmartLoot, medical destructibles, producer-layer boundaries and production-only next steps.
- [Generator map](GENERATOR-MAP.md)
- [Generator catalog](GENERATOR-CATALOG.md)
- [Generator gameplay groups](GENERATOR-GAMEPLAY-GROUPS.md)
- [Generator decision matrix](GENERATOR-DECISION-MATRIX.md)
- [Generator evidence index](generators/README.md)

## UI policy

- [Mixer + responsive grid — authoritative UI specification](UI-MIXER-GRID-SPEC.md)
- [Existing semantic visual control system](VISUAL-CONTROL-SYSTEM.md)
- [Standalone control redesign audit](UI-REMAINING-REDESIGN.md)
- [Slider constraints](UI-SLIDER-CONSTRAINTS.md)
- [Slider range policy](UI-SLIDER-RANGE-POLICY.md)
- [Vanilla preset audit](VANILLA-PRESET-AUDIT.md)

Historical machine-readable evidence remains in the versioned subdirectories below `docs/`.

## Locked UI design

- [UI Design Lock — mandatory approval-only contract](UI-DESIGN-LOCK.md)
- [UI snapshot manifest](UI-DESIGN-LOCK.json) — source hashes verified during publish
