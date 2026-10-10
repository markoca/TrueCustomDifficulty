# Pre-release test and evidence runbook

### Hunger Survival Profile LOW hover acceptance

Move the mouse over the LOW handle in the Hunger Survival Profile: **only the actual LOW handle at 0%** may show a golden hover halo; no ghost halo may appear at the plot center around −50%. Move away to confirm the halo disappears. Drag and arrow-edit LOW, then verify MEDIUM/SEVERE and the other compact tracks still work normally. The static consistency audit now guards both the legitimate native LOW halo call and suppression of the duplicate generic map-center overlay. Local Wine visual acceptance is still required.

### Mutant Properties clickable reference markers (Combat / Wine manual acceptance)

With the embedded `MUTANT HIT DAMAGE` slider visible, verify all four labels are **NOVICE / STALKER / VETERAN / MASTER** (no one-letter abbreviations). Hover each caption and corresponding dot: only that marker turns white and the pointer becomes a hand; moving away restores muted colors. Click each caption and each dot: the live slider must jump to the exact authored value **0.35x / 1.00x / 1.35x / 1.50x**, including the values outside the regular 0.10x drag grid. Verify no mouse drag starts on a marker click, normal rail dragging still works, right-clicking the markers and rail resets to the current Base Preset, and a modified value propagates through Save Preset and the existing Combat BPatch writer. The CombatPatchContract suite now verifies the exact four marker values and the production ViewModel snap method; WPF hit targeting and hover rendering still require the local Wine check.

### C3 duration stepping and parent-cap regression extension

In BOTH Override OFF and ON, verify Water Regen <= Water main and both Energy Cost effect durations <= Energy main, including after reducing the parent and after toggling override. With ON, parents may reach 270s but secondary effects must stop at their *current* parent seconds, not 270s. Test dragging and keyboard on all eight duration sources: OFF requires 1s increments, ON 5s increments (never 0.5s). Test that ViewModel CurrentNumber assignments obey these steps too, not just mouse snapping, and generated CFG retains source-exact targets. Local WPF/Wine and game-runtime acceptance remains pending.

### Drink Duration step manual acceptance (C3 extension)

With Override OFF, drag/click and arrow-step all five main and three secondary durations in 1s increments. With Override ON, repeat in 5s increments. Page Up/Down must move ten steps; verify independent native and extended caps, live child bounds in OFF, no value changes solely from switching ON, and no additional CFG targets. Local Wine UI and runtime verification pending.

**Canonical checklist for True Custom Difficulty before each release candidate.**
Run on the **exact Git commit** to be published. A static PASS is not
gameplay verification. Do not issue v1.0 without explicit approval.

## Test inventory

| ID | Command / suite | Host | Verified scope |
| --- | --- | --- | --- |
| A1 | `python tools/audit_combat_contract.py` | Linux / Windows | 39 Combat settings, 68 direct routes, special-family mappings |
| A2 | `python tools/audit_ui_cfg_contract.py` | Linux / Windows | 245 catalog settings, 232 effective controls, 152 visible + 93 hidden, including registered nine-actor unique weapon shuffle writer |
| A3 | `python tools/audit_project_consistency.py` | Linux / Windows | Live catalog, manifest and UI source consistency; historical `[NOT RUN]` is not PASS |
| A4 | `python3 tools/audit_rationale_ui.py` | Linux / Windows | Global right-side WHAT IT DOES vs CONTROLS separation, 33 family guides and muted-footer ordering (static, not Wine proof) |
| A5 | `python3 tools/audit_master_dependency_ui.py` | Linux / Windows | Master starvation anchor respects OFF; five child controls inactive but still visible; mouse/keyboard safety and regression presence (source-level) |
| B1 | `python tools/audit_cfg_dump_provenance.py --base-zip BASE --dlc1-zip DLC1` | Linux / Windows | Actual source CFG filenames and 16 Mutant Health MaxHP fields |
| B2 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json --strict` | Linux / Windows | Every 1,224 direct source/CFG field check (strict) |
| B3 | Same as B2 plus `--parameter-prefix combat.` | Linux / Windows | 68 direct Combat source checks (strict) |
| C1 | `dotnet run --project tests/CombatPatchContract/CombatPatchContract.csproj -c Release -- --out audit/combat-patch-fixtures` | Windows / .NET 10 | 84 **real production writer** Master/Novice/Stalker/Veteran/PSY BPatch scenarios, including all 11 quarter-rate ticks for eight mutant abilities, 1.75× and exact 3× BPatch endpoints, and legacy clamping |
| B5 | `python tools/audit_health_stamina_source.py --base-zip BASE --dlc1-zip DLC1` | Linux / Windows | 69 Health/Stamina original source fields, including Player→[0] inheritance |
| C2 | `dotnet run --project tests/HealthStaminaPatchContract/HealthStaminaPatchContract.csproj -c Release -- --out audit/health-stamina-patch-fixtures` | Windows / .NET 10 | 46 writer Health/Stamina BPatch writer cases |
| B6 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix survival. --out REPORT.json --strict` | Linux / Windows | All 139 Survival original CFG source points (requires matching original ZIPs) |
| C3 | `dotnet run --project tests/SurvivalPatchContract/SurvivalPatchContract.csproj -c Release -- --out audit/survival-patch-fixtures` | Windows / .NET 10 | 87 Survival writer scenarios including Starvation visual-anchor OFF/ON, disabled child output gates, linked cascades and parent toggle round-trips |
| B8 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix equipment. --out REPORT.json --strict` | Linux / Windows | 113 active Equipment source points (101 base, 12 DLC1; inherited source reported explicitly) |
| C5 | `dotnet run --project tests/EquipmentPatchContract/EquipmentPatchContract.csproj -c Release -- --out audit/equipment-patch-fixtures` | Windows / .NET 10 | 32 real ViewModel-to-BPatch scenarios: four difficulties, edge cases, combined axes and DLC1 conditional routes |
| B9 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix environment. --out REPORT.json --strict` | Linux / Windows | 42 Environment raw-value source paths, 4 difficulty and 38 effects |
| C6 | `dotnet run --project tests/EnvironmentPatchContract/EnvironmentPatchContract.csproj -c Release -- --out audit/environment-patch-fixtures` | Windows / .NET 10 | 90 live Environment ViewModel-to-BPatch writer scenarios |
| B10 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix alife. --out REPORT.json --strict` | Linux / Windows | 64 original A-Life Director MaxCount raw-value targets |
| B11 | `python tools/audit_alife_director_source.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json` | Linux / Windows | Four exact Rank identities, 64 AgentType/MaxCount slots, index-to-species identity, two original zero ceilings |
| C7 | `dotnet run --project tests/ALifePatchContract/ALifePatchContract.csproj -c Release -- --out audit/alife-patch-fixtures` | Windows / .NET 10 | 94 real WPF ViewModel-to-Director BPatch scenarios |
| B12 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix loot. --out REPORT.json --strict` | Linux / Windows | 320 Loot direct source fields, including verified physical rare-stash template inheritance |
| B13 | `python tools/audit_loot_direct_source.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json` | Linux / Windows | 14 mutant one-part drops, 121 NPC weapon rows in 33 generators, Gamepass coupon progression identities |
| C8 | `dotnet run --project tests/LootDirectPatchContract/LootDirectPatchContract.csproj -c Release -- --out audit/loot-direct-patch-fixtures` | Windows / .NET 10 | 117 real direct Loot BPatch writer scenarios across four difficulties and mixed controls |
| D1 | `chmod +x publish.sh && ./publish.sh` | Linux / .NET 10 | Published executable and matching `BUILD-INFO.txt` source receipt |
| D2 | Wine UI + in-game manual test | Linux / game | Visual design lock, preset round-trip, PAK/UTOC/UCAS build, actual game effects |


**B4 — original Combat reference inheritance and special arrays:**
`python tools/audit_combat_source_structures.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json`.
Checks 68 Combat direct source routes, separately identifying two
verified Medium-to-Empty inherited source fields, all 14 Master
agents / 66 tags, and both PSY effects. Game BPatch integration
remains a manual acceptance gate. The source runner now executes
B1 through B4 with genuine base/DLC1 ZIPs.

### Global Drink Duration Override test (C3, required)

The 94-case Survival writer contract now includes one real **global range
override** end-to-end case. Test both modes on all eight independently writable
Duration sources: OFF 90/135/60/60/270s per-drink limits, ON identical 270s
maxima on all sources, **all eight set to 180 seconds simultaneously** with
actual production WPF ViewModel → CFG writer BPatch equality, and OFF restoration
with minimal clamping. The checkbox is metadata and must never become a new
CFG edit. Preset JSON round-trips the `drinkDurationOverride` key.

Wine manual follow-up: at narrow and wide windows, verify the compact universal
checkbox and ruler axis labels do not overlap, Space toggles a focused checkbox,
right-click resets it, R resets the entire group, each independent native
duration has full 270s reach only with Override ON, and loading a saved
high-duration preset sets the mode before its values. Runtime UE acceptance is
still separate.

## Single repeatable runner

`tools/pre_release.py` invokes the registered subprocesses, preserves raw
logs, and saves `summary.json` under ignored
`audit/pre-release/<UTC>-<SHA>/` with exact Git SHA, command, platform and
per-step **PASS / FAIL / NOT RUN** statuses. Source runs additionally
save raw-value audit JSON; Windows runs save generated CFG fixtures.

```bash
# Inventory; does not execute any test.
python tools/pre_release.py list

# A1 / A2 / A3: standard Linux/Windows static check.
python tools/pre_release.py static

# B1 / B2 / B3: actual matching decoded CFG archives required.
python tools/pre_release.py source \
  --base-zip "/path/to/STALKER2-GameData-CFG(4).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(4).zip"
```

On **Windows + .NET 10**:

```powershell
# C1 (82 Combat cases) + C2 (46 Health/Stamina cases).
python tools/pre_release.py writer

# A1/A2/A3 + B1/B2/B3/B4/B5 + C1.
python tools/pre_release.py all --base-zip "C:\path\to\GameData-CFG.zip" --dlc1-zip "C:\path\to\DLC1-CFG.zip"
```

On **Linux**, `all` runs static + source but reports **INCOMPLETE**
(exit code **2**) because C1 and C2 require Windows WPF. Verify C1 using the
matching GitHub CI run. Exit codes: `0` requested automated tests passed;
`1` a test failed; `2` Windows-required coverage skipped.
**A zero exit code never indicates S.T.A.L.K.E.R. 2 gameplay acceptance.**

## GitHub CI at the exact release commit

[Static source and CFG contracts](https://github.com/markoca/TrueCustomDifficulty/actions/workflows/static-audit.yml)
executes A1–A3 on Ubuntu and C1 on Windows automatically for `main`,
pull requests and manual dispatch.

```bash
# Optional: gh CLI requires connected GitHub credentials.
gh run list --repo markoca/TrueCustomDifficulty --workflow static-audit.yml --limit 5
gh run view RUN_ID --repo markoca/TrueCustomDifficulty
```

Require both `audit` and `combat-writer-windows` jobs green for the
**intended SHA**, not some older commit. Save/download the CI artifact
`combat-patch-contract` containing generated CFG patches.

## Local Linux publish and manual runtime acceptance

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/

git pull
git rev-parse HEAD

python tools/pre_release.py static

chmod +x publish.sh
./publish.sh
cat publish/win-x64/BUILD-INFO.txt

WINEPREFIX="$HOME/.wine-s2" \
WINEDEBUG=-all \
wine "publish/win-x64/TrueCustomDifficulty.exe"
```

Verify UI against `docs/UI-DESIGN-LOCK.md` and
`docs/VISUAL-CONTROL-SYSTEM.md`: all tabs, values and reference markers,
changed-number highlighting, hit targets, resets, rationale, dependencies,
base presets and preset round-trips. With repak/retoc installed, build a
mod and check actual `.pak/.utoc/.ucas` output, installation and load order.
Run S.T.A.L.K.E.R. 2; neither the Python tests nor a successful publish
does this.

### Water / Energy live short-duration regression (required)

The current Survival writer suite checks three new independent CFG parameter IDs, ensures the main Water/Energy duration routes never own the same source prototype as their children, checks dynamic child maxima and cascade when the parent duration shrinks, and validates a real combined BPatch for all five edits. Required manual UI checks: Water 5s → up to current 30s main, Energy −100% 5s and −30% 15s → up to current 45s main, increasing parent does not automatically expand the child, shrinking parent clamps it, reference resets and old preset migration work. At minimum main, children still show their authored vanilla minimum, and no non-editable tail responds to input. Source archive checks must verify unchanged physical CFG targets; engine stacking of SPDrain modifiers is not yet in-game verified.

### Consumables: original Water / Energy Drink source audit (automatable)

Use the exact game CFG ZIP supplied for the target game version. Run
`python3 tools/audit_consumable_effect_source.py --base-zip "/path/to/STALKER2-GameData-CFG.zip" --out /tmp/tcd-consumable-effects.json`
to validate both `EffectPrototypes.cfg` and `ItemPrototypes.cfg`,
the native -30% / 15s and -100% / 5s SPDrain structures, inheritance,
the eight action types, Regular/Limited/Weird Kettle item membership and
the production duration-writer baseline. The same check is included in
`python3 tools/pre_release.py source --base-zip BASE.zip --dlc1-zip DLC1.zip`.
Source PASS does not establish how concurrent effects combine in-game.
See [source audit](ENERGY-DRINK-EFFECT-SOURCE-AUDIT-2026-10-10.md).

### Combat in-game matrix (manual, never automatically PASS)

Refer to [Combat Phase 3/4](DEEP-CFG-AUDIT-PHASE3-COMBAT-2026-10-09.md):

- [ ] Veteran Human Attack Frequency: source inverse cooldown and actual NPC cadence.
- [ ] Veteran Bloodsucker/Burer Health: distinct source variants and real HP.
- [ ] Master special cooldown: only selected family changes and ability cadence follows.
- [ ] Veteran special cooldown: inserted 14-agent array loads without side effects.
- [ ] Master/Veteran PSY: both effect SIDs, delay, count, bleed and durability tested.
- [ ] Legacy preset with hidden repeated-hit/PSY values: generated output is intentional.

The C1 tests only validate produced BPatch CFG text, **not Unreal Engine
merge semantics** or quest-specific side effects. Preserve
`runtime_test_required` until in-game behavior is verified.

## Release sign-off (copy and fill for every tag)

| Evidence | Status |
| --- | --- |
| Candidate SHA, 0.x version and `git status --short` | NOT RECORDED |
| A1–A3 `summary.json` static PASS at that SHA | NOT RUN |
| B1–B5 original matching decoded archive evidence | NOT RUN |
| C1 Windows CI 69/69 + C2 46/46 writer at that SHA | NOT RUN |
| D1 publish and `BUILD-INFO.txt` SHA match | NOT RUN |
| D2 Wine UI and preset smoke | NOT RUN |
| D2 PAK/UTOC/UCAS build, mod load and in-game test | NOT RUN |
| Combat and other runtime-pending gameplay acceptance | NOT RUN |
| Decision: READY / BLOCKED / approved exception | BLOCKED |

Do not release merely because the automated checks are green. Retain
the evidence folder, completed checklist and any approved exceptions
together with the candidate commit SHA.

### Phase 5 runtime contract coverage

The Windows production-writer job is extended to **82 separate executable
scenarios**: 11 special-array PSY/mutant cases, 52 direct difficulty
control cases and 14 Mutant Health family cases (16 nested MaxHP fields).
The current bound ViewModels and real `CfgPatchWriterService` are used.
These scenarios prove output structure/value and no collateral edits,
not independent correctness of every vanilla source value or in-game load.
The original GameData/DLC1 ZIP checks remain an independent B1–B5 gate.

### Phase 7 runtime acceptance: inherited mutant section

The Windows writer job now tests **82 scenarios**, including five importable
JSON presets for Stalker/Medium (inherited from Empty) and Veteran/Hard
(directly authored). Use
[Combat runtime test kit](../tests/CombatRuntimeAcceptance/README.md)
for isolated in-game tests. Even a green 82/82 C# result is **not**
gameplay evidence: game loading, Unreal inheritance/merge behavior and
mod packaging remain manual gates.

### Phase 8 source and writer tests

The fifth archive source gate, `B5`, confirms all **69 original
Health/Stamina direct writer field paths** and explicitly reports
`stamina.vaultMaxHeight` as inherited from
`Player : struct.begin {refkey=[0]}`. The Windows `C2` writer job
runs **46 independent production ViewModel→BPatch scenarios**,
with separate generated CFG evidence. Both CI jobs must be green
on the **same release commit**. Never confuse original source
ancestry with in-game BPatch merging.

See [Phase 8 detailed audit](DEEP-CFG-AUDIT-PHASE8-HEALTH-STAMINA-2026-10-09.md).

The five catalog-only Stamina internal speed-state parameters are intentionally absent from the actual application ViewModel and are **not** counted among C2 active writer scenarios. See Phase 8 for their precise IDs.

C2 now includes five Master original-mechanics full-list replacement cases in addition to its 41 live Health/Stamina scalar/toggle tests. The five internal-only stamina CFG definitions remain non-VM and are not counted as writer inputs.

### Phase 9: Survival real-writer validation

The `survival-writer-windows` CI job executes the real WPF ViewModel and production CFG writer for **53 live Survival parameters + 6 difficulty variants (69 cases (59 direct + 7 baseline/gating + 3 combined))**. All **139 direct source fields** are covered separately by strict original ZIP value comparison (B6) during `pre_release.py source`. One source-mapped alcohol threshold remains catalog-only, not an app control. Game loading/effects stay NOT RUN. See [Phase 9](DEEP-CFG-AUDIT-PHASE9-SURVIVAL-2026-10-09.md).

### Phase 9 original decoded Survival evidence (2026-10-09)

Using the matching uploaded original CFG ZIPs, all **139/139 Survival direct source field identities and literal raw values** matched, with no refkey inheritance required. The immutable source ZIP SHA-256 fingerprints, counts by all 54 IDs and limitations are recorded in [Survival source evidence](evidence/SURVIVAL-ORIGINAL-CFG-2026-10-09.json). The same source comparison is automatically repeated by the B6 gate when matching archives are provided to `pre_release.py source`. This is **not** a gameplay acceptance result.

### Phase 10 Economy (2026-10-09)

A strict original-source Economy prefix gate audits **409 direct CFG fields**, while Windows `economy-writer-windows` runs **34 real application-to-BPatch scenarios** including 68 base trader profile minimum-condition cases. Source ZIPs are required for the independent source gate, and installed-DLC plus game-load evidence remain NOT RUN. See [Phase 10](DEEP-CFG-AUDIT-PHASE10-ECONOMY-2026-10-09.md).

### Phase 11: Equipment (2026-10-09)

The four current Equipment controls map to **113 direct writer fields**:
4 difficulty-based armor toughness values, 52 player-weapon wear entries (41 base /
11 DLC1), 34 base ammo weights and 23 consumable weights (22 base / 1 DLC1).
The 41 base player weapons include one authored zero,
`UA_GLaunch_Player.DurabilityDamagePerShot=0.0`; the
`scaleNonZero` writer must preserve this value without emitting an unrelated leaf.

Windows `equipment-writer-windows` runs **32 real production scenarios**, including
vanilla no-op across all four difficulties, each control separately, 0x and 2x,
conditional DLC1 output and combined-axis/shared-file cases. This suite checks
exact paths, {bpatch} flags, values and no collateral leaf writes.
The independent **B8** source gate checks exact original ZIP content and
reports inherited {refkey} resolution separately. Both gates are required for
source confidence; neither is in-game acceptance. See
[Phase 11](DEEP-CFG-AUDIT-PHASE11-EQUIPMENT-2026-10-09.md).

### Phase 11 original Equipment archive evidence (2026-10-09)

The two uploaded original decoded CFG archives were checked independently
against the full 113-point current Equipment writer mapping: **103 directly
authored exact values + 10 explicit physically traced refkey values = 113/113**.
Three quest/special consumables use `{refurl=ConsumablePrototypes.cfg;refkey=...}`;
the required parents and original Weight fields exist in the runtime dump's
consolidated `GameData/ItemPrototypes.cfg`. The source validator's specifically
allowlisted flattened-refurl resolver and CI regression cover that layout; all
other unknown refurls remain findings. See
[original archive evidence](evidence/EQUIPMENT-ORIGINAL-CFG-2026-10-09.json)
and [Phase 11 details](DEEP-CFG-AUDIT-PHASE11-EQUIPMENT-2026-10-09.md).
Windows 32/32 production Equipment writer scenarios were PASS at 0ab7938.
**This does not verify Unreal BPatch interpretation or in-game effects.**

### Phase 12: Environment / Anomalies original source and BPatch contracts (2026-10-09)

All **17** live Environment backing source IDs are mapped to **42** original
base-game CFG edit points. The supplied original ZIPs confirm **42/42 physically
authored raw-value matches**, no inherited/missing/mismatched fields.
See [Phase 12](DEEP-CFG-AUDIT-PHASE12-ENVIRONMENT-2026-10-09.md) and
[original archive SHA-256 evidence](evidence/ENVIRONMENT-ORIGINAL-CFG-2026-10-09.json).
The Windows `environment-writer-windows` job independently executes **90**
production ViewModel-to-BPatch scenarios. Both checks are pre-release gates;
they are NOT evidence of Unreal runtime merge or gameplay effectiveness.

### Phase 13: A-Life Director rank/agent original source and writer (2026-10-10)

The current A-Life catalog maps **19 source IDs / 64 direct MaxCount
fields** into one base-game Director prototype. **18 inputs** are loaded in
the normal ViewModel; `alife.mutantSpawn.generic` is catalogued for
source auditing only, and must not become an in-game general mutant cap.
The original decoded ZIP source was independently checked: **64/64 exact
MaxCount values, 64/64 physical AgentType identities, 4/4 rank
identities**, two intentionally zero Newbie predator counts. No differences
or inheritance were required. See [Phase 13 detail](DEEP-CFG-AUDIT-PHASE13-ALIFE-2026-10-10.md)
and [original file SHA-256 and 16-slot matrix](evidence/ALIFE-DIRECTOR-ORIGINAL-CFG-2026-10-10.json).

`pre_release.py source` runs both B10 direct-value and B11 structural
AgentType / rank consistency checks. `alife-writer-windows` runs **94**
production WPF ViewModel to BPatch scenarios (C7). **Verified C7 PASS 94/94** at commit `bb2c08bffba8b9afe5827fb8d1eb282e602b6971`, GitHub Actions run `37997251466`. Source verification
and writer verification do not prove Unreal merge semantics or the
actual A-Life world/encounter results.

### Phase 14A: Direct Loot original source and production writer (2026-10-10)

Of the **40 current Loot parameters**, only **16** have **320 direct
CFG source points**: 14 mutant drop chances, 64 rare-stash
coupon assignments and 242 NPC weapon condition bounds.
All 320 original effective values match the uploaded decoded ZIP:
**256 authored exact + 64 physically traced inherited exact**,
zero source-value mismatches. Coupon routes are explicitly
refkey/refurl inherited from the *physically present consolidated*
`GamePass_Stash_ItemGenerator_Rare` source prototype. The 121
generic NPC weapon entries span 33 production generator roots and
include one original 35-45% condition outlier. The new B12 and B13
source gates reproduce the independent source and identity checks.
C8 runs 117 actual WPF ViewModel-to-CFG writer scenarios.

**Do not conflate this direct audit with complete Loot verification.**
The remaining **24 Loot catalog parameters use specialized writers**,
including stash production, corpse loot, breakable boxes, world
placements/removals and shuffle mechanics. They require distinct
Phase 14B writer/fixture audits, original generator provenance and
manual in-game acceptance. See
[Phase 14A](DEEP-CFG-AUDIT-PHASE14A-LOOT-DIRECT-2026-10-10.md)
and [archive fingerprints](evidence/LOOT-DIRECT-ORIGINAL-CFG-2026-10-10.json).

### Phase 14A Windows verification result (2026-10-10)

The real `loot-direct-writer-windows` test suite passed **117/117**
on commit `2c4d8b5770ef0199a0f814df367001a4bcfd8e97` in
GitHub Actions run `37999482003`. The first run had caught a
production defect: writer selection incorrectly used visual
`IsModified` (which includes aggregate highlights) instead of the
actual `IsValueModifiedFromReference()` source-change predicate.
Production selection was corrected; subsequent tests confirmed that
changing rare-stash coupon chance does **not** fan out into unrelated
stash ammo writer output. This is a real writer regression fix, not
a change in the UI. Source 320-point result is independent of this
C# validation; special Loot writers are still a separate Phase 14B.

### Phase 14B1: Loot special generator production BPatch gate

`dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release -- --out audit/loot-special-generators-fixtures`
runs **63 Windows production writer cases**: real source-backed values, NPC
medical/corpse smart loot and ammo, three stash tier clones, RGD reward,
breakable ammo/grenades, ammo family weight distribution, inherited
template category item fields, complete audited redirect CSV fanouts,
DLC1 routing, and cross-control isolation. Every actual generated leaf is
compared against independently computed manifest and input transforms.
The source manifest is a contract, not proof that the original game
accepts generated prototypes.

This suite does **not** claim coverage of the remaining world-actor
removal, weapon/attachment/blueprint shuffle, detectors or loose loot.
Those require Phase 14B2. The generator templates and embedded redirect
manifests also require original-source provenance and gameplay checks.
Do not treat registered tests as PASS before CI execution.

### Phase 14B2: Physical world Loot writer contracts

Run `dotnet run --project tests/LootWorldRemovalsContract/LootWorldRemovalsContract.csproj -c Release -- --out audit/loot-world-removals-fixtures` (**27 randomized-domain physical actor removal tests**) and `dotnet run --project tests/LootWorldPlacementContract/LootWorldPlacementContract.csproj -c Release -- --out audit/loot-world-placement-fixtures` (**20 weapon/attachment/blueprint/detector placement tests**) in Windows .NET 10. Both are registered in the pre-release writer runner and as `loot-world-removals-windows` and `loot-world-placement-windows` CI jobs. Expected results are invariant-based, never seeded snapshots. Covers all 10 previously untested special world Loot source controls. See [Phase 14B2](DEEP-CFG-AUDIT-PHASE14B2-WORLD-LOOT-2026-10-10.md).

These two suites validate actual app → BPatch paths and randomized target legality, not the original archive provenance or Unreal runtime.

### World Loot decoded original archive source gate (B14)

The `loot-world-original-source-structures` gate invokes:
`python tools/audit_loot_world_source.py --base-zip BASE --dlc1-zip DLC1 --out audit/loot-world-original-source.json`.
It streams only the selected physical root prototypes from the large
original GameData/DLC1 `SpawnActorPrototypes.cfg` files and checks
the physical actor identities, direct item prototype fields, each
box's generator family, precise blueprint and weapon/attachment
generator item indices, physical generator redirects and the one
DLC1 detector stash item. Every mismatch is a failing finding.
**This requires matching source ZIPs supplied locally and is not a
GitHub Windows CI original-source check** (CI has only the repo).
Run `python tools/pre_release.py source --base-zip ... --dlc1-zip ...`
before release; store the gate's output JSON. This still does not
prove in-game Unreal BPatch loading.

### Phase 14B2 verified Windows writer receipt

The same-SHA production CI run `38003085576`, commit
`fb59fb36b14a7e8d8593aaf0dcc67e5cdf8736d1`, has
**27/27 PASS** for randomized World Loot physical actor removals and
**20/20 PASS** for world weapon/shuffle/density, attachment multiset,
77-blueprint derangement and detector removal. The physical-removal
artifact retains per-scenario SHA-256 actor identity receipts and samples
after verifying every emitted actor. Also audited and corrected the
outdated Loot catalog statement that dedicated F1 boxes overlap ordinary
ammo/RGD boxes; these source manifests are disjoint, and the current
Ammo Box population is 981 base + 334 DLC1, not the older numbers.
`loot-world-original-source-structures` has been registered as a
separate strict original-ZIP source gate but **must actually run
against the original decoded archives** before B14 source provenance
can be marked PASS. Gameplay and Unreal mod loading remain NOT RUN.

### World Loot Phase 14B2 — original GameData/DLC1 source proof and post-fix gate (2026-10-10)

**Original decoded source verification is independently PASS** against the two
uploaded physical CFG ZIP archives: 1,315 Ammo/RGD Boxes, 1,208 Food/Drink
Boxes, 868 Medical Boxes, 101 dedicated F1 Boxes, 5,395 loose dynamic
packs and 1,838 loose static items; 362 direct safe world weapon actors,
seven direct attachments, 77 world-only blueprint generators and seven
direct detectors. Verified **131 unique ItemGenerator direct leaf
identities/values** and **58 distinct original SpawnActor generator
redirect fields**. Normalized source manifest identities, physical
original field values, cardinalities and original archive SHA-256 provenance:
[World Loot source evidence](evidence/LOOT-WORLD-ORIGINAL-CFG-2026-10-10.json).
This source proof was computed by an **independent decoded-archive
scanner**; keep the in-repository `B14`
`tools/audit_loot_world_source.py` CLI as a required exact release gate
and run it with locally supplied original ZIPs.

**Critical defect fixed, post-fix CI PENDING:** original decoded
`ItemGeneratorPrototypes.cfg` proves that six world weapon stash
generators carry 15 Chance/MinCount/MaxCount entries only under nested
`Upgrades` or `Attaches`, not as immediate weapon item fields. The
old `AddReplacementNode` writer stripped those nested rewards and
copied unrelated nested values to the wrong node. Production now
writes only a minimal indexed `ItemPrototypeSID` `{bpatch}` and
leaves original item mechanics/upgrade subtrees intact. Those 15
misclassified source columns were removed and the Windows C11 suite
now rejects their reintroduction and validates the minimal patch.

**The earlier 20/20 World Loot placement PASS was on the OLD writer**,
not the source-corrected writer. GitHub Actions subsequent attempts
were failing at the runner provision stage with **zero executed steps
on every job**, so they cannot be described as code or contract PASS/FAIL.
Re-run C11, plus all regression jobs, on the final release SHA as soon
as GitHub Actions can execute jobs normally. The original archive source
PASS is separate from the production code validation and Unreal runtime
merge/gameplay acceptance.

### Survival Phase 9B — baseline and parent-gating cases

Seven more Windows real writer regression cases cover the four untouched base difficulties and three modified-but-disabled Master Survival children on Veteran. C3 now comprises **69 cases**, with no change to original 139/139 source-value evidence. All engine/gameplay acceptance remains manual.

### Survival Phase 9C — 3 combined-family writer regressions

Three additional **real** Windows BPatch tests check simultaneous hunger/fatigue controls, overlapping energy-drink source prototypes and three distinct Master negative-mechanics families. C3 now comprises **69 scenarios**, maintaining the independently established 139/139 original-source status. Source correctness and in-game behavior are still separate gates.


### Phase 9D — Survival constraint cascade release gate

The Windows SurvivalPatchContract now runs **85 production writer
scenarios**: 69 earlier cases plus 13 linked slider cascade tests
(all seven actual Survival constraint families) and 3 Master parent
OFF/ON round-trips. It parses the real generated CFG to verify
exact path sets, accurate transformed values and no unrelated edits.
Original-source parity is independently verified by B6 against
the real decoded ZIP pair; Unreal runtime remains untested.
