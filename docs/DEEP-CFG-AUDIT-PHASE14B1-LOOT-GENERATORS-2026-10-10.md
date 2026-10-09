# Deep CFG / BPatch Audit — Phase 14B1: Loot special generators (2026-10-10)

## Scope and nonclaims

The active Loot catalog has 40 parameters. Phase 14A separately verified
16 direct source IDs, 320 original direct effective CFG fields and 117
production writer scenarios. Phase 14B1 tests **14 distinct special
generator input IDs** using production `MainViewModel` and
`CfgPatchWriterService.Write()`. They are not represented by the
320-entry direct source list. This suite is not sufficient to call all 24
special Loot inputs verified.

The selected set covers:

- Human corpse smart-loot choice; NPC smart bonus ammo with 43 authored
  count entries in 24 groups over four ranks; NPC medical chance
  within six repeatable-human pools.
- Basic/Standard/Rare Stash ammo multipliers, Basic RGD-5 chance
  and quantity; exact reconstructed template category and item fields,
  physical generator clones, and redirect CSV fanouts.
- Breakable box ammo and grenade yields; the four ammo-family weight
  distribution multipliers; two independent SNG/NATO ammo and
  two independent F1/RGD grenade generator clones.
- Both base and DLC1 redirect applicability, including mixed-control
  cases where multiple special writers target a shared CFG document.

The writer does not use `IsModified` UI aggregate highlighting for
source-change eligibility. It uses `IsValueModifiedFromReference()`
to avoid triggering unrelated special generators when a child control
changes. The Phase 14A regression covers Rare Stash coupon-only edits,
and this suite extends the non-collateral contract to generator
combinations.

## C9: Exact production writer audit

`tests/LootSpecialGeneratorsContract` computes a full expected output
graph from the independently retained `cfg-writer-baseline.json`
special template declarations and embedded redirect CSV manifests,
using the values set on the **real ViewModel**. Actual production writer
files are parsed into file+prototype+node+key leaves. Checks include:

1. **Four vanilla difficulties:** no changed source, no output.
2. **Twelve numeric special IDs**, low/high endpoints and DLC1
   disabled/enabled (48 scenarios).
3. **Five combinations** involving mixed stash tiers, cheap RGD,
   human medical loot, ammo quantity/weights/grenades and DLC1.
4. **Six corpse cases**: original smart choice replacement, changed
   package choice, independent smart ammo quantity with package ON,
   and package-absent no-file/no-collateral writer behavior.

**63 registered cases total.** Writer checks all template item
assignments and exact redirect fanouts, relative quantity integer
round-half-up, retained non-target weight/chance fields, source
ammo-family weighting, medical chance capped 0–100%, file/control/edit
counts, clone refkey/refurl headers, and absence of unexpected output.

```powershell
dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release -- --out audit/loot-special-generators-fixtures
```

CI: `loot-special-generators-windows`; fixtures retained as
`loot-special-generators-contract` Actions artifact. The same
runner is registered in `tools/pre_release.py writer`.

**At authoring: registered, CI NOT RUN.** Completion must be confirmed
on the *same Git SHA* before marking the suite PASS.

## Source and runtime boundaries

The 320-point original source gate in Phase 14A does not include these
special templates or their 33, 450, 305, 1235 and 182 audited production
redirect CSV assignments. The template reconstruction is based on
manifests already retained in this repository; original decoded
GameData and DLC1 ZIP correspondence is **not reverified here**.
Original generator CFG audits are documented separately under
`docs/generators/`, and per-game-version lineage still requires the
matching original ZIPs. CI tests textual BPatch/CFG emission but not
Unreal refurl resolution, mod order, PAK acceptance or gameplay.

**Deferred Phase 14B2**: world loot crate/loose/F1 actor removal,
world weapon density and shuffle, attachment/blueprint shuffles,
detector removal and other physical world actor placement features.

No UI controls, baseline values, preset IDs or 0.x project version
were changed in this phase.
