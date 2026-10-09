# Deep CFG / BPatch Audit — Phase 10: Economy (2026-10-09)

## Current real data (not historical 0.2.x counts)

The current catalog has **23 Economy definitions**; **22** enter the actual application ViewModel. `economy.repairDifficultyCost` is source-only, deliberately excluded from the UI and production parameter list (4 manifest fields). The direct writer manifest has **409 source edit points** across 22 IDs, including **1 DLC1 point**. Its operations are `set`, `scale`, `scaleNonZero` and `scaleIntegerHalfUp`. One original free fast-travel route has `RequiredMoney=0`; `scaleNonZero` preserves zero and the writer intentionally omits the unchanged BPatch leaf.

The 22 live parameters divide into 21 direct-writer routes and 1 independent special writer. `economy.traderMinBuyCondition` is produced from an explicit 86-profile CSV: **68 base** and **18 DLC1** trader profiles; each has separate weapon and armor minimum-durability assignments.

## Executable production writer audit

New `tests/EconomyPatchContract` uses real `MainViewModel` and `CfgPatchWriterService.Write()`, not a simulated BPatch algorithm. **34 scenarios** cover the 21 direct live controls on Veteran, 12 additional difficulty profile cases (buy/sell pricing and two quest-coupon reward controls across Novice/Stalker/Master), and 1 special-writer test asserting 68 base traders / 136 exact leaf edits.

For each scenario the suite validates exact emitted file inventory, edited field counts and paths, prototype SID, full nested `{bpatch}` hierarchy, numeric transformations and the absence of collateral edits. It also requires the one catalog-only repair definition to remain excluded from the live application graph.

Windows/.NET 10 or matching GitHub Actions `economy-writer-windows`:

```powershell
dotnet run --project tests/EconomyPatchContract/EconomyPatchContract.csproj -c Release -- --out audit/economy-patch-fixtures
python tools/pre_release.py writer
```

**Current execution:** test registered; NOT RUN locally in this connector-only session. Verify same-SHA CI result before promoting to PASS.

## Independent original ZIP source gate

Strictly compare all **409 direct** Economy source fields with the matching original decoded archives, including the source-only 4 repair fields:

```bash
python tools/audit_cfg_source_values.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2).zip" \
  --parameter-prefix economy. \
  --out audit/economy-source-values.json --strict
```

`python tools/pre_release.py source` now runs the same prefix gate. **NOT RUN here:** the archived source ZIPs from the previous conversation are not mounted. Do not call a writer manifest match original CFG verification.

## Gameplay / scope exclusions

This audit does not prove in-game buy/sell price calculations, medic/ammo stock generation, starting money, paid routes, linked repair prices or trader condition threshold semantics. The 18 DLC1 trader profiles and 1 DLC1 mutant-part value need separate installed-DLC writer and game tests. Gameplay balance, UI lock specifications and 0.x versioning are unchanged.
