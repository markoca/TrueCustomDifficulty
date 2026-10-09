# Deep CFG / BPatch Audit — Phase 13: A-Life Director (2026-10-10)

## Production scope (actual application, not a proposed new feature)

**19** `alife.*` catalog IDs map to **64** direct, base-only
`MaxCount` source fields in
`GameData/ALifePrototypes/ALifeDirectorScenarioPrototypes.cfg`.
**18** parameters are active in `MainViewModel`; the remaining
`alife.mutantSpawn.generic` is a **catalogued, source-only** MutantGeneric
restriction (4 values), intentionally **not a normal UI control**.
Do not automatically promote it or treat it as an outer ceiling on species.

The current UI remains two source-backed families: four independent human
player-rank cards and fourteen mutant species cards, each with four
rank-specific count values. Existing UI locks remain unchanged.

The original Director has one `ALifeDirectorPreset` prototype (SID
`Default`) and this path:

```text
ALifeDirectorPreset
  ALifeScenarioNPCArchetypesLimitsPerPlayerRank
    [player rank 0..3]
      Rank = ERank::Newbie/Experienced/Veteran/Master
      Restrictions
        [agent type 0..15]
          AgentType = EAgentType::<specific species>
          MaxCount = <original integer>
```

Human restrictions: 4 `scaleIntegerHalfUp` rows, one per rank.
Generic + concrete mutant restrictions: 60 `scaleIntegerHalfUpPreserveZero`
rows, 4 ranks × 15 agent types. All edits go through the exact nested
`{bpatch}` hierarchy. Integer scaling uses half-up rounding. Original zero
ceilings are **never unlocked** by raising multipliers.

### Independent original ZIP source evidence — PASS

The uploaded original decoded archives were checked independently
against the complete 16-slot × 4-rank layout:

| Verified contract | Count / result |
| --- | --- |
| Actual Director prototype, SID, rank-list nesting | PASS |
| Physical rank identities / ordering | 4/4 |
| Exact slot-to-`AgentType` identities | **64/64** |
| Exact original `MaxCount` values versus mapped sourceRaw | **64/64** |
| Inherited values needed | 0 |
| Missing / erroneous / duplicate mappings | 0 |

Two authored Newbie **zero** ceilings are precisely Bloodsucker [2]
and Controller [5]. They must remain zero even when their species
multipliers increase. The generic restriction is the independent sibling
at slot [1], not a global multiplier or parent on the 14 species.

The two archive SHA-256s and full 64-point rank/species/value
evidence are in
[Phase 13 source evidence](evidence/ALIFE-DIRECTOR-ORIGINAL-CFG-2026-10-10.json).

The canonical source validation is **two independent checks**:
`tools/audit_cfg_source_values.py --parameter-prefix alife. --strict`
(64 raw original entries), plus
`tools/audit_alife_director_source.py` (four Rank / sixty-four AgentType
identities, full 16-slot inventory, zero caps and exact control ownership).
The latter closes the dangerous gap where two mutant slots have the same
numeric MaxCount but different `AgentType` strings.

```bash
python tools/audit_alife_director_source.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2)(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2)(2).zip" \
  --out audit/alife-director-structure.json
python tools/audit_cfg_source_values.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2)(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2)(2).zip" \
  --parameter-prefix alife. --out audit/alife-source-values.json --strict
```

Both are registered under `tools/pre_release.py source`. An original
source PASS covers this decoded game build and this manifest, not
later changes in the game's CFG data.

### Actual ViewModel → production BPatch writer (C7)

`tests/ALifePatchContract` calls the real production
`MainViewModel` and `CfgPatchWriterService.Write`. It validates live
and source-only catalog membership, strict rank/species array ownership,
integer half-up and zero-preservation semantics, nested `{bpatch}`
tags, exact output file/parameter/edit counts, absent collateral
CFG leaves and shared-radar control independence.

**94 tests**:
- Four untouched vanilla difficulties (no emitted BPatch).
- 18 active controls × four difficulty baselines = 72 independently
  changed slider scenarios.
- Twelve min/max variants (0.25x / 3x) including the zero-valued
  Bloodsucker/Controller Newbie restrictions.
- Six multi-control scenarios spanning human rank cards, mutant species
  siblings and human/mutant combinations.

```powershell
dotnet run --project tests/ALifePatchContract/ALifePatchContract.csproj -c Release -- --out audit/alife-patch-fixtures
```

The `alife-writer-windows` job runs on Windows/.NET 10 and retains
generated CFG fixture evidence. **VERIFIED: 94/94 PASS.** The real Windows production writer job
`alife-writer-windows` completed successfully in GitHub Actions run
`37997251466` on source commit `bb2c08bffba8b9afe5827fb8d1eb282e602b6971`.
The CI log includes the two zero-preserving mutant case (six emitted
fields), and a final `PASS: 94 A-Life production BPatch writer scenarios`.
The source archive and in-game behavior are separate verification boundaries;
repeat this suite against the exact future release commit.

## Hard boundaries: not a world population control

These 64 Director restrictions are **group/encounter maxima** and
are not:
- `LairPrototypes.cfg / MaxSpawnQuantity` (world lair population);
- lair refill/wipe timers and initial fractions;
- spawn cadence, Director default/override delays and encounter frequency;
- faction expansion, strategic `MinLairs`/`MaxLairs` state thresholds;
- scenario weights, story/quest-specific populations, or DLC1 lair settings.

The separate [ALIFE lair audit](ALIFE-LAIR-AUDIT.md) remains exploratory.
Do not generate arbitrary lair BPatch points or expose population/frequency
sliders without the existing eligibility and runtime audits.

**NOT RUN:** actual Unreal nested indexed-array merge interpretation,
in-game group spawning, population dynamics, mod load order, PAK/UTOC/UCAS
acceptance, and the UI visual check in Wine. The UI catalog, design-lock
documents and project version were not changed by this audit.
