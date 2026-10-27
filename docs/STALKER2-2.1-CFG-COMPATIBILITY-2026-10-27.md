# STALKER 2.1 CFG compatibility — source audit (2026-10-27)

## Inputs and limits

Compared the user's decoded **2.0** and **2.1** GameData/DLC1 CFG dumps with the current `main` TCD production mapping (embedded baseline still declares **2.0.5**). These are source audits, **not proof of in-game patch application**. No gameplay writer transforms or stash drop rates have been altered by this audit.

- 603 -> 605 CFGs; **22 existing changed**, **2 new**, **0 removed**.
- 1,230 direct writer edit points in `src/TrueCustomDifficulty/Data/cfg-writer-baseline.json`.
- Only two changed CFG files are direct writer sources: `GameData/CoreVariables.cfg` (**13 edit points**) and `DLCGameData/DLC1/ItemPrototypes.cfg` (**2 edit points**). All 15 exact prototype/node/key leaves retained their 2.0 values in 2.1; no changed direct source leaf was identified. Unchanged CFG files cannot contain changed source leaves.
- Other changed files include `GameData/SpawnActorPrototypes.cfg` and `DLCGameData/DLC1/SpawnActorPrototypes.cfg`. These are used by special stash/loot writer manifest routes and must be checked **against the actor CSV manifests**, not simply marked safe because no direct edit point targets them.
- Initial cross-check of the 24 removed actor IDs against five central stash actor manifests found **zero removed-ID overlaps**, but this is *not* exhaustive for other CSV routes. The command below performs broader manifest actor checks locally.
- `GameData/FastTravelPrototypes.cfg` is **byte-identical**. Legacy `economy.fastTravelCost` scales 183 `RequiredMoney` leaves, preserving the free/zero entry.
- Newly introduced `GameData/FastTravelEconomyPrototypes.cfg` defines `[1] SID = ALifeGuideEconomy` and `GeneralCoefficient = 1.3` for **roaming guides**. The existing hub-guide price knob **does not control these fees**. This is explicitly clarified in the parameter help/rationale. Avoid silently adding this 2.1-only CFG patch to a 2.0.5 baseline, which would break backwards compatibility.
- New A-Life dynamic streaming parameters in `CoreVariables.cfg` / platform `SettingsVariables` are independent of existing TCD NPC group-size controls. A source pass does not demonstrate that runtime spawn/simulation timing remains identical.
- Korshunov's isolated `AbilityPrototypes.cfg` melee change is not a TCD-owned direct edit.

## Reproducible 2.0 -> 2.1 audit

Run from the repository root with both original **outer** archives:

```bash
python3 tools/audit_game_21_cfg_compat.py \
  --old "/path/STALKER2-2.0 cfg dump.zip" \
  --new "/path/STALKER2-2.1 cfg dump.zip" \
  --json-out audit/stalker2-21-cfg-compat.json
```

The tool verifies that mapped direct leaves in *changed* source files still exist with unchanged source values, checks the new ALifeGuideEconomy source, and scans existing actor IDs in bundled CSV manifests for removal or changed `StashPrototypeSID`, `ItemGeneratorSettings`, `EnableSmartLootIfPossible`, and `SpawnedPrototypeSID` fields. It fails when an owned source was removed/changed, prints a structured JSON report, and never edits the source data.

## Audit correction (2026-10-27)

The previous actor root scanner used a flat regex terminating at the first nested
\`struct.end\`. It returned **zero matching actor roots**, yet printed PASS. That was
a false positive; no claim of actor/manifest compatibility is supported by that run.

The updated scanner uses real column-zero, 32-hex actor roots, slices only owned
actor blocks and explicitly **FAILS if zero actor manifest roots are matched**
in either the base or DLC1 dump. Actor changes in \`StashPrototypeSID\`,
\`ItemGeneratorSettings\`, \`SpawnedPrototypeSID\`, or
\`EnableSmartLootIfPossible\` are now compared for every identified owner.

Actual decoded-dump delta observed during this investigation:
- Base SpawnActor: 16 removed roots, 205 new roots; **87 pre-existing actor
  roots gain \`EnableSmartLootIfPossible = true\`**.
- DLC1 SpawnActor: 8 removed roots, 6 new roots; no changes to those four
  watched fields among unchanged actor roots.
- All 15 direct writer edit points in changed direct-source CFG files stayed
  at the same exact source values (the separate 2.0.5 embedded baseline
  is still not being re-versioned).

A two-actor representative audit against the actual user's 2.0/2.1 ZIPs
(one base SmartLoot stash and one DLC1 stash) now scans nonzero roots and
passes. That test does **not** substitute for a full run over the repository's
actual manifests; run the command below on the user's checkout for that.
Six synthetic negative/positive regression tests verify normal pass, nested
fields, disappearing actors, new SmartLoot flags, changed direct leaves and
the fail-on-zero case.

**Roaming guides:** do not add \`ALifeGuideEconomy\` to the legacy hub-guide
slider or to a 2.0.5 baseline. A separate 2.1-aware control requires a
reliable game-version capability check and actual game QA; no silent writer
change has been made.

## Required manual runtime regression

Test on the **2.1** game build with a clean save and controlled before/after comparisons:

1. **Hub guides**: base and modified `Guide Travel Fees` prices across two hub-to-hub routes; free routes remain free. **Roaming guides**: verify separately that the old knob does not change their fees.
2. **Stashes**: one basic/standard/rare regular stash per relevant tier, on a fresh save and with altered medical/ammo/chance settings; check ordinary, named, unique, and scripted protection. Reuse the documented three early Richter stash locations when possible.
3. **NPC corpse/Mutant loot**: compare modified vs base corpse ammo, medical loot and mutant part drops on identical combat encounters; specifically observe the new `EnableSmartLootIfPossible`-flagged actors.
4. **Extra Stash Gear**: verify item tier, T1-compatible attachments, max two eligible gear items, no quest/unique items, and coverage percentage on a new build.
5. **A-Life**: compare active NPC squad population and behavior when using Recommended and Dynamic streaming options; don't misinterpret unloaded NPCs as absent NPCs.
6. **DLC1**: check DLC1 stash/actor routes and artefact behavior with the installed DLC.

Only the executable game can establish practical runtime compatibility. **Do not bump `gameVersion` to 2.1 or claim a complete 2.1 baseline until every direct and special source has been revalidated.**

## 2026-10-27: single Guide Travel Fees control also covers roaming guides

The existing `economy.fastTravelCost` remains one slider, without changing any UI layout, preset schema or parameter ID.

For all versions, it continues to scale 182 paid `RequiredMoney` leaves in `GameData/FastTravelPrototypes.cfg`. On installed games where `repak list` confirms `GameData/FastTravelEconomyPrototypes.cfg[.bin]` exists, it additionally patches only the existing `[1]` (`SID = ALifeGuideEconomy`) prototype with `GeneralCoefficient = 1.3 × slider` and `MinPrice = 100 × slider`. Remaining regional, difficulty, reputation and rounding rules are unchanged. Thus 0× removes both the base coefficient and the source minimum price.

Feature detection uses the game installation associated with the configured ~mods folder and cached top-level .pak source lists. An edited travel fee with an unidentifiable game installation fails explicitly; the 2.0/2.0.5 game receives no roaming CFG. On vanilla 1×, neither source is patched.

The production C# EconomyPatchContract now includes five additional linked-source scenarios: 0×/0.5×/1×/2× for games with the new source and 0.5× for games without it. Runtime behavior still requires 2.1 in-game verification.
