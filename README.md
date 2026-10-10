# True Custom Difficulty

### 2026-10-10 — Human Squad Size and Individual Anomaly Damage vertical editors

**HUMAN SQUAD SIZE** is now four independent one-row Medical Items-style vertical bars (NEWBIE, EXPERIENCED, VETERAN, MASTER) on a shared **0–3x SQUAD SIZE** ruler. The native source domain remains **0.25–3x**, step **0.05x**, with each bar showing its *effective integer NPC cap* using the existing round-half-up rule and its multiplier in a muted secondary line. The actual CFG multiplier, rank-specific selection callback, muted reference marker, individual right-click reset, keyboard adjustment and R reset are preserved. No new domain or control is created.

**INDIVIDUAL ANOMALY DAMAGE** is now eleven independent vertical bars in one compact row (Electro, Chemical, Fire, Carousel, Toxic Cloud, Steam, Clicker, Razor, Soap Bubble, Fireball, Diamond). Their individual source values are shown above the handles; a common **0–200% DAMAGE** ruler measures each source relative to its own audited 1x reference, not absolute HP across unlike anomalies. All eleven native sources have a 0–2x reference-relative limit and preserve their different real CFG numbers and native steps (Razor is itself an x multiplier). Only changed current values turn gold; baseline ticks are muted. Exact source writer mapping, individual selection/help callback, right-click source reset, keyboard input and R reset remain unchanged. The anomaly panel and XAML host now measure **360 DIP**, human panel **350 DIP**, with compact one-row controls and no redundant text. Local build/Wine visual and in-game checks remain pending.


### 2026-10-10 — World Item Spawns heading

The former **WORLD PLACEMENT** panel heading is now **WORLD ITEM SPAWNS**. This is a display-title change only: existing placement sources, native controls, internal identifiers, presets and CFG/BPatch writers remain unchanged.


### 2026-10-10 — World Loot Depletion vertical bank (authoritative)

The **WORLD LOOT DEPLETION** editor uses five independent, directly editable vertical removal bars in one compact row: AMMO BOXES, F1 GRENADE BOXES, FOOD & DRINK BOXES, MEDICAL BOXES, and LOOSE WORLD LOOT. A single truthful **0–100% REMOVED** Y ruler, current percent above each bar, discreet native reference ticks, and category names below follow the Medical Items / Item Value Layers source-bar grammar. Each control retains its real 0–1 CFG range, 0.01 increment, distinct source writer, reset-on-right-click, keyboard arrow editing and panel-wide R reset. Only modified values are gold. The frame and XAML host both measure 330 DIP, minimum renderer width 600 DIP, with no second row or redundant legend. No loot-domain merge, unique-weapon override, preset or BPatch mapping change. Base/DLC1 population evidence remains in the source audit and parameter definitions rather than adding a noisy legend to the direct editor. Local build/Wine visual verification remains pending.


### 2026-10-10 — Item Value Layers vertical controls (authoritative)

The **ITEM VALUE LAYERS** editor is now a compact, one-row six-column vertical source bank, following the Medical Items direct-edit grammar. CONSUMABLES, AMMO, ARMOR, WEAPONS, ARTIFACTS and MUTANT PARTS each have an independent native vertical drag handle, a current `x` multiplier above the bar, a muted reference tick, and a short item caption below. All six audited CFG values range from **0.00x to 2.00x**, with **0.01x** steps, so the editor uses one truthful **0–2x VALUE** ruler at left rather than unrelated per-card scales. Only modified current numbers turn gold; no Base/Vanilla legend or duplicate bounds are shown. The frame measures 330 DIP high at a minimum 560-DIP width; the original six real parameters, distinct mutant-parts ItemPrototypes Cost writer, keyboard arrows, right-click individual reset and R all-reset remain intact. This visual change does not modify CFG targets, preset format or writer rules. Local publish/Wine and game validation remain pending.
Build fix (2026-10-10): `ItemPriceScalersRadar.OnKeyDown` uses a distinct loop-local name for the six-source R reset to avoid C# CS0136 shadowing of the selected key-control pattern variable. UI behavior and CFG mappings are unchanged; local build verification pending.



### 2026-10-10 — Medical Items shared Override Limits (current contract)

The 370-DIP Medical Items frame places **FOOD HEALING at top-left** and one canonical **OVERRIDE LIMITS checkbox at top-right**. This is a UI-only `medicalOverride` preset flag owned by `health.bandageHealing`; no synthetic CFG boolean or new writer route. OFF (default, including legacy presets) keeps all twelve sources' independent catalog maxima. ON expands their independent caps to the existing physical axes: **HEAL 200 HP, BLEED 100 PTS, RAD 200 RAD**. Individual sliders are never linked or synchronized. Disabling Override clamps only values beyond their respective original source caps; source edits persist via their existing CFG/BPatch writers. Checkbox retains canonical 20-DIP appearance, click/hover, right-click reset, whole-panel R reset, tab reset, and baseline reset. Numeric labels, compact row, reference ticks, and independent source writers remain unchanged. Local Wine visual and in-game confirmation pending.


### 2026-10-10 — One Override Limits checkbox, shared 270s ceiling

The `Consumable Effects → Effect Duration` board has one **OVERRIDE LIMITS** checkbox, OFF by default. **OFF** retains every drink's individual original maximum (Water 90s, Energy 135s, Vodka/Beer 60s, Magic Vodka 270s); three independently editable Water/Energy additional-effect durations retain their live parent cap. **ON** raises the independent maximum of *all eight drink durations* to the same **270 seconds** on the existing common ruler. This permits all drinks to have the same chosen duration, but does NOT synchronize any values: every source slider remains independent. Vanilla minimums, per-source rounding, original CFG field targets, preset values and modified-numeric highlighting remain.

Checkbox state is a UI-only `drinkDurationOverride` property persisted in the preset root; it adds no false CFG edit/parameter and by itself produces no mod. When disabling Override, out-of-range values are clamped back to their normal ceilings; otherwise all current values stay unchanged. Preset loading restores override mode before values. The five-ruler timeline and three secondary cards have a measured responsive layout with a universal minimal checkbox and keyboard/mouse/reset semantics. Real WPF/ViewModel BPatch regressions validate both 180s equality across all sources and native cap restoration. The exact UI rules are in both binding UI catalogs. Local publish/Wine and UE runtime checks remain pending.

### 2026-10-10 — Three independently editable short consumable durations

**Implemented as real CFG controls:** Water's stamina-regeneration 5 s (`WaterStamina2`), Energy's −100% SPDrain 5 s (`EnergeticStaminaPerAction2`, also Limited) and Energy's −30% SPDrain 15 s (`EnergeticStaminaPerAction1`, also Limited/Weird Kettle alternative). The five primary effect duration sliders are retained. Three compact new sliders appear directly below the common-seconds timeline, each with original vanilla seconds as its minimum and the **current, actual main duration** as its maximum (Water 30× main multiplier, Energy 45×). When the main shrinks, any longer child clamps to it; increasing main doesn't alter children. To enforce both minima coherently, Water's main multiplier cannot drop below 5/30×, and Energy's below 15/45×. The 1×-anchored 0.05× main steps are retained.

The catalog increases **236 → 239** settings, but direct source edit points remain **1,220**: three existing short-effect `Duration` targets move to independent **set** owners instead of being overwritten by the global main-duration `scale`. Presets store three new fields, with migration for older custom/factory presets; no new effect prototypes, action types or strength tuning. The compact responsive UI, both binding design catalogs, source audits and Survival writer tests are updated. In-game stacking of the simultaneous −100%/−30% effects is still not verified.

### 2026-10-10 — Energy Drink native effect verification

Original decoded GameData proves that regular Energy contains **-100% SPDrain / 5s** and **-30% SPDrain / 15s**, plus **+200% RegenStamina / 45s**. Limited Energy uses **-100% SPDrain / 5s** and **+500% RegenStamina / 45s** in its normal list; -30% / 15s appears on its alternate Weird Kettle effect path. SPDrain affects eight authored movement/melee action types. Do not invent a guaranteed combined effect: engine stacking is not confirmed. The Consumable duration preview now identifies the distinct regular/Limited effects in four compact source-driven lines; the native controls, actual CFG values, read-only tick semantics, source ranges and 0.05x steps remain unchanged. Renderer and parent measurement both use **240 DIP**. Full source mapping and a reproducible base ZIP audit: [Energy effect audit](docs/ENERGY-DRINK-EFFECT-SOURCE-AUDIT-2026-10-10.md). Local Wine/game verification remains pending.

### 2026-10-10 — Radiation Recovery label and compact sizing

The middle Radiation System slider is now **RADIATION RECOVERY** instead of a truncated technical clearance label. It continues to edit `survival.radiationRecovery` (`VitalParams.DegenRadiation`) in **PTS/S**. The shared compact-track renderer measures the actual right-hand value width, including modified-numeric typography, before allocating title space; no card widening or gameplay/CFG changes. Both UI catalogs and the project static regression gate reflect this. Local Wine visual acceptance remains pending.

### 2026-10-10 — Combat controls simplified

Combat retains **Mutant Hit Damage** and the **14-species Mutant Health** editor. The previous mutant attack-pacing adjustments and species ability-pacing dashboard are retired from the shipped parameter catalog, factory presets, writer and active tests. **Human Attack Frequency remains unchanged.** Older user presets may contain unsupported fields; those fields are ignored when loading and omitted on a subsequent save. The compact native slider follows the Stamina Usage value/rail hierarchy, and the two UI catalogs define the active design.

### 2026-10-10 — Unique Weapon Shuffle CFG writer baseline / audit reconciliation

The dedicated `loot.looseUniqueWeaponShuffle` control was already implemented in the World Placement UI and in `CfgPatchWriterService.WriteLooseUniqueWeaponShuffle`, but the central `cfg-writer-baseline.json` still declared **244** supported parameters and lacked its manifest-backed production route. This made the actual C# `ValidateBaseline` fail before export, and the Python audits report the parameter as unsupported. **Do not remove, defer or fake a single-field CFG edit for this control.**

**Correct current inventory:** **236** catalog entries, **150 visible aggregate anchors/standalone controls**, **86 hidden backing IDs**, **223 effective editable source dimensions**, **13 intentionally non-UI source IDs**, **73 hidden-but-editable backings**. The source-backed direct/special writer route union covers **236/236**, including the new nine-actor shuffle.

**Canonical production route:** `cfg-writer-baseline.json.special.looseUniqueWeaponShuffle` contains the real `parameterId`, embedded `manifestResource` and `expectedActorCount=9`. `CfgWriterBaseline` deserializes this exact route and `CfgPatchWriterService` validates it, includes it in supported count and uses its embedded manifest in the actual writer. The writer remains a real per-actor `{bpatch}` derangement of nine physical world pickups (five item/presentation fields per actor, no `removenode`), separate from regular World Weapon Shuffle.

**Regression gates:** `tools/audit_ui_cfg_contract.py` and `tools/audit_project_consistency.py` now agree on the new totals and discover the new special route. The project audit additionally verifies route identity, manifest resource, exactly nine distinct actor/item pairs and linkage to the real C# writer. `tests/LootWorldPlacementContract` checks that the loaded production baseline contains the precise route and matches the live catalog count; its existing **24 writer scenarios** already cover unique-only, DLC, mixed weapon and other placements. Run the two Python audits, then the Windows `.NET` writer tests and Wine application. Source/structure verification alone does not prove local publish or UE runtime acceptance.


### 2026-10-10 — Starvation OFF must disable its visual-anchor slider

**Bug observed in Wine UI:** Under MASTER → STARVATION PENALTIES, the EXTRA STAMINA COST slider stayed gold and interactive even with STARVATION MECHANICS OFF. Unlike the four other child sliders, the stamina-cost parameter (`survival.starvationStaminaDrain`) is also the visible `StarvationTimeline` **visual aggregate anchor**. The earlier `ParameterViewModel.IsControlEnabled` expression bypassed every dependency check for aggregate anchors, including the anchor's own `requiresToggle: advanced.starvationMechanics`.

**Correct contract:** `IsControlEnabled` always honors the real `requiresToggle` provider; being a visual anchor grants visibility, **never edit permission**. `IsControlVisible` separately preserves the Starvation composite row while the parent is OFF. OFF leaves **all five child values visible, grey and non-interactive** (no gold accent, mouse drag, keyboard edit, or child CFG output). Switching ON reactivates all five using their preserved settings. The independent parent toggle remains clickable. This is also the correct pattern for future dependency-gated visual anchors.

**Implementation:** `ParameterViewModel.IsControlEnabled` no longer ORs `IsVisualAggregateAnchor`. Existing `DrawMasterNativeCard` already uses `IsControlEnabled` to draw the active accent and handles, and `AddHorizontalTarget` rejects disabled hit areas. Added safety guards for stale keyboard selection, mouse hit targets and captured drags that could survive a parent toggle transition. No gameplay number, difficulty baseline, source CFG target, writer algorithm, control geometry or formatting has changed.

**Regression:** `tests/SurvivalPatchContract` now includes the Starvation anchor in its Veteran-disabled and Master-OFF/re-enable checks; the entire five-child dependency set must disable/re-enable together, keep its anchor row visible, preserve modified values and produce no child writer edit while OFF. The production suite adds two scenarios (from 85 to 87 based on the prior inventory). Static audit: `python3 tools/audit_master_dependency_ui.py`; both this source gate and actual Windows .NET suite should be run before release. The source inspection alone is **not** a successful WPF build or Wine/in-game visual acceptance.


### 2026-10-10 — Rationale CONTROLS compile syntax correction

Fixed a C# switch-expression punctuation typo in `ParameterViewModel.ControlHelp`: the `RadiationTreatment` arm ended in a semicolon rather than the required comma, causing `CS1003` at `ParameterViewModel.cs(66,134)`. The player-facing wording, right-panel layout and writer mappings are unchanged. `tools/audit_rationale_ui.py` now verifies that every named switch-expression instruction arm ends with a comma so this error cannot pass the source audit again. **Static arm review passes; an actual .NET 10 Windows publish and Wine UI acceptance remain to be run locally.**


### 2026-10-10 — Global right-hand rationale and CONTROLS pass (authoritative)

**Source of truth:** `MainWindow.xaml`, `ParameterViewModel.RationaleDisplay` and `ParameterViewModel.ControlHelp`. This updates the presentation of every gameplay tab without changing any CFG/preset parameter, values, calculations, writer route, sliders, hit targets, or actual reset behavior.

**Required reading order from top to bottom:** selected control name; WHAT IT DOES (**gameplay explanation only**); conditionally WHAT THIS VALUE MEANS; HIGHER / LOWER; WORKS WITH; CURRENT RESULT; one **CONTROLS** section; then the existing muted `TabSummary` context at the very bottom. All conditional sections retain their previous behavior. The interaction instructions **must not** be appended inside `RationaleDisplay`, and the title `CONTROLS` is authored **exactly once** in XAML, never emitted by a view-model string.

**Player-friendly controls grammar:** keep CONTROLS compact and plainly actionable (what to click/drag, what the control changes, how to reset); avoid CFG identifiers, explanation repetition, two full help paragraphs, generic repeated Vanilla/Base blurbs, duplicated headings and fake UI interactions. Use concise specialized guidance for **all 33** current `VisualAggregateFamily` types, including when the selected item is a child inside its group; use equivalent semantic guidance for specialized editors (mutant health/drop/group, stamina costs/recovery, medical healing, anomalies, world loot, etc.). Generic slider/range/choice/toggle fallbacks apply to ordinary controls. The guide is normal foreground text, not a bright callout, with a small single accent heading and no extra large card. Reuse existing right-column scrolling and responsive layout.

**Muted bottom text:** keep the short `TabSummary` only. Remove the old `SliderGuide` from the visible footer; it repeated instructions already covered by CONTROLS and conflicted with individual semantic controls. Do not put it back as a redundant long paragraph.

**Regression:** `python3 tools/audit_rationale_ui.py` asserts one heading, ordering ahead of the grey footer, all 33 family guides, generic fallbacks, preserved conditional meaning/results, and absence of duplicated old control text. It is static source verification only; local .NET publish, Wine visual layout and interaction remain unverified until run.


### 2026-10-10 — World Placement blank-panel regression fix

The `SHUFFLE UNIQUE WEAPONS` setting remains **aggregate-only** (`exposeInUi=false`), so `MainViewModel` MUST materialize it as a live `ParameterViewModel` before passing `AllParameters` to `AggregateVisualControl`. The World Placement family has exactly six live IDs: `loot.worldWeaponShuffle`, `loot.worldWeaponDensity`, `loot.looseUniqueWeaponShuffle`, `loot.worldAttachmentShuffle`, `loot.worldDetectors`, `loot.blueprintShuffle`. All six must be present even though only `loot.worldWeaponShuffle` is the **visible aggregate anchor**. Otherwise the entire panel incorrectly renders `World placement data unavailable`.

`MainViewModel` now explicitly includes all six IDs in its materialization filter, includes Unique Weapon Shuffle in the aggregate modified-state wiring, and keeps it in World Placement sort order. `tests/LootWorldPlacementContract` checks the actual `MainViewModel.AllParameters`, anchor/visibility mapping, and `AggregateVisualControl`'s six bound sources; this guards the exact user-observed failure. Existing World Placement compact height and checkbox design, all real CFG mappings, writer logic and original-world eligibility remain unchanged. Windows/.NET publish and Wine screenshot acceptance still require execution.


### 2026-10-10 — Source-backed loose unique weapon shuffle (replaces removal control)

**Final approved behavior:** `SHUFFLE UNIQUE WEAPONS` is a **boolean toggle in WORLD PLACEMENT**, not a depletion slider. Parameter `loot.looseUniqueWeaponShuffle` (default OFF) is separate from ordinary `loot.worldWeaponShuffle`, weapon density, and `loot.looseWorldLootRemoved`. The World Loot Depletion panel remains the original **five** sliders; no unique-weapon percentage, removal path, or obsolete preset parameter is active.

**Compact UI contract:** World Placement aggregate has **six** settings (five independent toggles and one dependent weapon-density slider). Height 275 DIP. The left compact column contains regular weapon shuffle at local top=65/H44, its dependent density card at 122/H72, and unique weapon shuffle at 208/H44. Three other toggle pills remain on the right, 46 DIP high, 57 DIP apart. Reuse the established compact checkbox/toggle chrome, hover highlight, independent right-click reference reset and family R reset. No redundant Vanilla/Base labels or helper paragraphs inside the control.

**Original CFG evidence:** GameData `SpawnActorPrototypes.cfg`, exact `WorldMap_WP` actors with `SpawnOnStart=true`, `SpawnType=Item`, matching `ItemSID`/`SpawnedPrototypeSID`. Nine included ItemSIDs: `Gun_Tank_MG`, `Gun_S15_AR`, `GunArevPrecise_AR`, `Gun_SOFMOD_AR`, `Gun_GStreet_HG`, `Gun_Texas_SG`, `GunNightStalker_HG`, `Gun_Cavalier_SR`, `Gun_RatKiller_SMG`. Exact actor IDs and original presentation tuple are in `src/TrueCustomDifficulty/Data/loose-unique-weapon-shuffle-manifest.csv`, kept separate from regular weapon pools.

**Writer contract:** OFF emits no patch. ON uses a cryptographically seeded Sattolo cycle over the nine original weapon tuples, meaning **every item changes location and each original weapon exists exactly once**. Each original location keeps its actor SID, coordinates and rotation; only the `SpawnedPrototypeSID`, `ItemSID`, `MeshPath`, `Durability`, and `DisablePhysicsAndCollision` tuple is redirected together by exact per-actor `{bpatch}`. Nine output actor files, 45 exact assignment fields, **zero** `removenode` operations. Neither the ordinary weapon shuffle nor its density can edit any of these actor IDs. Quest/deferred pickup locations, trader rewards, NPC inventories and global item definitions are never targeted.

**Quest limits:** The nine actor/item SIDs were not directly referenced in the base or DLC1 `QuestNodePrototypes.cfg` searches; this does not certify every possible script/reward linkage. Do not expand the actor pool without separate source/quest audit. Production writer regression now asserts permutation conservation, no fixed points, original mesh/condition tuple fidelity and coexistence with other World Placement edits. Local Windows build, BPatch resolution and actual in-game placement/pickup remain **NOT RUN** until executed.


## NPC Ammo preview — all nine source caliber groups (2026-10-10)

NPC Ammo preview now shows nine actual `NPC_Ammo_Smart` caliber groups in a compact 3×3 grid, each with its earliest source rank and a live source-backed round range. This supersedes the former three-example Newbie-only preview. Only `Medicine + Ammo` activates the corpse bonus. The same 0–3x multiplier, positive half-up integer rounding, individual gold numeric highlighting and existing CFG/BPatch writer remain unchanged. Panel is 564 DIP high; local WPF/Wine compile and actual gameplay confirmation remain pending.


## Survival linked slider BPatch audit — Phase 9D

Added 13 constrained native-slider cascades plus three Master parent OFF/ON round-trip cases to the real Windows CFG writer harness. The suite now checks **85 Survival scenarios**, including absence of unrelated CFG changes. Independent original-source parity remains **139/139**. Actual in-game behavior is **NOT RUN**. Details: [Survival Phase 9](docs/DEEP-CFG-AUDIT-PHASE9-SURVIVAL-2026-10-09.md).

## Survival CFG/BPatch audit — Phase 9

The Survival source catalog has **54 parameters / 139 direct CFG edit
points**, with **53 currently instantiated live controls** and one
catalog-only alcohol threshold parameter. Extended to an **85-scenario**
Windows/.NET production writer contract, validating actual view-model
edits, precise BPatch fields and absence of unrelated updates.
All **139/139 original source CFG values** have now been independently compared against the supplied GameData/DLC1 archives and match exactly. New game versions must rerun the archive gate; gameplay runtime behavior remains **NOT RUN**. See [Phase 9 Survival audit](docs/DEEP-CFG-AUDIT-PHASE9-SURVIVAL-2026-10-09.md).


## Health + Stamina original CFG audit — Phase 8

Verified **69 original source CFG fields** from the supplied
GameData/DLC1 archives: **68 directly authored exact** plus one
original-source inherited `Player.VaultingParams.MaxObstacleHeight`
through `{refkey=[0]}`. New Windows/.NET writer contract covers all
**38 live Health/Stamina settings + 3 additional difficulty cases
(41 direct-parameter scenarios) plus five Master parent-toggle regression cases = 46 total)** with actual production WPF ViewModels and
BPatch output. See [Phase 8 evidence and runtime gaps](docs/DEEP-CFG-AUDIT-PHASE8-HEALTH-STAMINA-2026-10-09.md).
Actual in-game effects and the inherited nested BPatch merge remain
`NOT RUN` until manually confirmed.


## Phase 7 — inherited Medium BPatch and game-test presets

Five JSON presets under [Combat Runtime Acceptance](tests/CombatRuntimeAcceptance/README.md) isolate the in-game Stalker / CFG Medium inherited mutant section, with a Veteran / CFG Hard control. The real Windows contract harness imports these files through PresetService, applies live ViewModel values and checks their exact CFG output; total **82 production writer scenarios**. The game/Unreal runtime behavior remains explicitly **NOT RUN** until manually verified. No UI or gameplay/writer code was changed.


## Phase 6: original Combat CFG sources

Original GameData/DLC1 CFG archives confirm 66 directly authored
Combat source fields plus 2 explicitly inherited original values via
`Medium : struct.begin {refkey=Empty}`. The original Master
special arrays contain 14 indexed mutant agent SIDs, 66 exact
writer manifest. Added `tools/audit_combat_source_structures.py` and
a dedicated inheritance regression; these do NOT prove engine
BPatch merging or gameplay. Full evidence:
[Combat Phase 6](docs/DEEP-CFG-AUDIT-PHASE3-COMBAT-2026-10-09.md).


## Deep CFG audit Phase 5 — Combat direct writer execution

Extended the real Windows/.NET 10 writer harness to cover every
**13 direct difficulty-control × 4 Base Presets (52)** plus
**14 mutant-health family** paths, on top of 11 special-array checks
(**77 cases total**). Unlike static mapping checks, tests change each
live bound ViewModel and parse the actual generated BPatch output;
they check target SID, field, source-backed numeric transformation,
nested `VitalParams.MaxHP`, count and unrelated changes. Original
decoded-source ZIPs and actual in-game effects are separate
pre-release acceptance gates; see [Phase 3–5 Combat audit](docs/DEEP-CFG-AUDIT-PHASE3-COMBAT-2026-10-09.md)
and [release testing](docs/RELEASE-TESTING.md).

## Before each release candidate: repeatable test suite

Run `python tools/pre_release.py list` to view **all registered automated
tests**. Use `python tools/pre_release.py static` locally and the
[pre-release test runbook](docs/RELEASE-TESTING.md) for original
GameData/DLC1 archive parity, 11 real Windows Combat BPatch writer
scenarios, SHA-matched CI evidence, Linux publish, Wine UI checking and
the mandatory manual game acceptance. The evidence runner records
PASS / FAIL / NOT RUN under `audit/pre-release/`; a static green result
alone never authorizes a release.


A [GitHub Actions static contract workflow](.github/workflows/static-audit.yml) runs all three validators on main and pull requests.
This does not replace .NET publish or in-game runtime validation.

Current consistency audit is based on active sources and writer data, not deleted
v0.2.x CSV snapshots. See [Consistency audit modernization](docs/PROJECT-CONSISTENCY-AUDIT-2026-10-09.md)
for the distinction between static PASS and historical/runtime NOT RUN. Run:
`python tools/audit_ui_cfg_contract.py`, `python tools/audit_combat_contract.py`,
and `python tools/audit_project_consistency.py` after pulling main.

## Deep CFG audit — Phase 3: Combat (2026-10-09)



## Deep CFG audit — Phase 2: actual writer paths (2026-10-09)

Verified a **production-important structural issue**: all sixteen Mutant Health `MaxHP` writer routes used an empty nested path even though vanilla stores those values under `VitalParams.MaxHP`. Phase 1 had validated their source numbers but **not** the emitted BPatch hierarchy. Fixed all sixteen direct `nodePath=["VitalParams"]` edit points and aligned all fourteen catalog `targets[].path="VitalParams.MaxHP"` entries, without modifying presets or UI values. All sixteen nested values independently match the supplied original GameData CFG. Added `tools/audit_cfg_source_values.py`, a decoded-ZIP parser that checks the **complete 1,224 direct edit-point** source path/raw-value surface when run with the original vanilla + DLC1 archives, producing a triage report instead of silently assuming inherited fields are direct. Source parser correctness was checked across all 13 referenced input CFG files; the complete 1,224-point comparison and .NET/Wine/game runtime test are still pending. Detailed evidence and commands: [Phase 2 structural source audit](docs/DEEP-CFG-AUDIT-PHASE2-2026-10-09.md). All current UI design locks remain unchanged.



## Deep CFG audit — Phase 1 source-backed repair (2026-10-09)

A fresh audit against the user's vanilla GameData and DLC1 CFG archives found **six missing production writer baseline entries** for existing Mutant Health cards (Blind Dog, Boar, Flesh, Poltergeist, Snork and Tushkan). Their actual authored `VitalParams.MaxHP` values, along with all prior mutant HP sources, were independently checked: **14 mutant families / 16 actual MaxHP fields verified**. The writer baseline now covers the current **244-parameter catalog** with **1,224 direct CFG edit points** (202 distinct direct parameter IDs). All **13 direct-input CFG filenames** exist in the correct supplied base or DLC1 archive; no direct concrete target is multiply owned. 231 active editable dimensions are represented by 152 visible catalog anchors plus 79 hidden composite backings; 13 internal/audit-only controls remain excluded, including `survival.drunknessThresholdScale` (explicitly hidden until the OverDrunkness chain is understood).

Updated the outdated production integration and UI→CFG audit gates and repaired the former eight-mutant-health consistency gate. Added `tools/audit_cfg_dump_provenance.py` for reproducible archive-level input and Mutant HP verification. The older `docs/UI-CFG-DEEP-AUDIT.md` represents a **historical v0.9.26.51 snapshot**, not current release evidence; the authoritative initial phase results and follow-up audit queue are in [docs/DEEP-CFG-AUDIT-2026-10-09.md](docs/DEEP-CFG-AUDIT-2026-10-09.md). **Do not treat file presence or direct writer route coverage as runtime gameplay confirmation.** Other 1,208 direct source values and the special writer families remain for subsequent deep CFG passes; no v1.0 release is authorized.



## Master tab — final UI redesign pass (2026-10-09)

Master now has five compact, player-facing punishment panels: **Starvation Penalties**, **Severe Fatigue**, **Radiation Penalty**, **Low Health Penalty**, and **Consumable Effect Penalties**. The existing **5 native mechanics toggles and 15 source-bound child editors** are preserved, as are all original `AdditionalMechanicsEffects` dependencies, toggle-gated CFG output rules and preset IDs. ON makes the children editable; OFF preserves their staged values in disabled/muted tracks. All cards share the Stamina Usage-style label/current/rail anatomy, bounded native min/max references, gold only for changed numerics and attached units, and shared handle-only hover glow. Two compact columns reflow to one below 672 editor DIP.

Starvation has two native timing editors and a read-only preview of the authored delay plus three repeating damage tick positions, followed by three independently edited penalty scalers—no synthetic HP/s. Severe Fatigue is a five-card 2×3 group; Radiation and Low Health are each a compact switch/slider pairing. Consumable Effects is a three-card healing-retention group with the original 25%/71%/82% authored Master references and a subdued distinct 100%-healing guideline. No existing source CFG mapping, native step, parent-toggle behavior, authored activation threshold or actual effective item healing calculation is changed. `README.md`, `docs/MASTER-MAPPING.md`, `docs/UI-DESIGN-LOCK.md` and `docs/VISUAL-CONTROL-SYSTEM.md` are updated together. This concludes the requested tab-by-tab UI pass; **deep CFG audit follows separately**. Local .NET/Wine compile and acceptance remain pending.

### Anomalies build and four-section grouping fix (2026-10-09)

Fixed the duplicate `"environment"` case in `TabViewModel.SliderGuide` that caused **CS8510** during `net10.0-windows` publish. Added `environment` to the tab's `PropertyGroupDescription` grouping whitelist so the four source-backed Anomalies sections actually appear separately and in the approved order. Updated Equipment tooltip text to reflect its existing three-ring center control. The change does not touch CFG writer behavior, source parameters, or preset serialization. Compilation and Wine rendering must be verified using the local `publish.sh` workflow.

## A-Life — compact group-size UI redesign (2026-10-09)

The A-Life tab now has two compact source-bound panels. **Human Squad Size** shows four independent Newbie, Experienced, Veteran and Master cards, each displaying the actual resulting maximum human squad count in large type next to its 0.25–3x native multiplier. **Mutant Group Size** shows fourteen alphabetically ordered species cards in a two-column × seven-row grid. Every species card displays the independently scaled native encounter-count maxima at **N / E / V / M** player rank plus its real slider. Native 0.05x steps, authored source reference ticks, integer half-up rounding, and original disabled zero caps are preserved. Only reference-different individual numbers with their `x` suffix turn gold; only handles glow on hover. Clicking a row selects its correct source-specific right-hand rationale. Full A-Life sources, associated preset keys and CFG writer routes remain unchanged, and the hidden generic mutant scaler stays hidden.

The two panels are native **Director encounter/group cap** tuning, not A-Life population/respawn-rate tuning. Lair population and encounter frequency remain separate audit-only candidates. The four-part UI design and source audit contracts are recorded in `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md`, and `docs/ALIFE-MAPPING.md`. Local .NET publish and Wine visual acceptance pending.

## Anomalies tab redesign — four independent panels (2026-10-09)

The `environment` (Anomalies) tab has **17 unchanged source-backed values** regrouped into four full-width sections: **Global Anomaly Damage** (one compact horizontal native slider), **Individual Anomaly Damage** (11 source-specific damage editors, two columns), **Flycatcher Slowdown** (Combat-style square with bottom movement, left turn and interactive center joint), and **Special Anomaly Effects** (Razor Bleeding, Diamond Bleeding and Psy Drain as three compact horizontal native sliders). The previous six-vertical-fader mixer has been retired. Native CFG ranges/steps, selected-difficulty reference, preset IDs and writer mappings remain unchanged. Diamond Bleeding still edits real bleeding points, and Flycatcher still edits percentage of movement/turning speed retained — not absolute slowdown. The individual damage radar now selects the edited row's own rationale. Numerical highlights and handle hover follow the established Stamina/Combat design contracts. Full visual contract is in `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md`, `docs/UI-MIXER-GRID-SPEC.md` and `docs/ANOMALIES-MAPPING.md`. Local WPF/Wine build and visual acceptance remain pending.

## Equipment tab — two compact source-backed sections (2026-10-09)

The live Equipment UI has **four** editable source parameters and **two** full-width top-level sections. **Equipment Wear** now matches **Human Combat's interactive graph**: a centered native-axis square with three identical gold rings. Bottom adjusts Armor Toughness, left adjusts Player Weapon Wear Per Shot, and the **center drags both simultaneously** on their own real CFG ranges. The graph has Combat-style inline X heading/value above it and rotated Y heading/value beside it instead of two detached readout cards. Source current multipliers turn gold only when modified; axis ticks and grey selected-difficulty ghost stay muted. Right-click the center resets both, an outer ring resets its own, and R resets the family. The four historical Equipment Bonus Caps (Stamina Recovery, Bleeding Recovery, Carry Weight Bonus, Encumbrance Relief) remain **audit-only**. Their upper limits are known, but the game's complete use of those caps has not been validated; the current parameter catalog already excludes them.

**Item Weight** now uses two short horizontal native tracks for Ammo and Food & Medicine, 0–2x at 0.01x increments, replacing the tall two-fader mixer. All four live Equipment sources, distinct reference difficulties, native limits, stored preset values, source mappings and CFG writer behavior remain unchanged. UI contract updated in `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md`, `docs/UI-MIXER-GRID-SPEC.md`, `docs/EQUIPMENT-MAPPING.md` and `docs/EQUIPMENT-CONTROL-DEEP-AUDIT.md`. Local .NET build/Wine visual and actual gameplay checks remain required.

## Economy tab compact UI polish (2026-10-09)

The Economy tab still exposes all **22 existing player-facing source settings** across Coupons & Rewards (3), Trading (4), Trader Stock (2), Item Value Layers (6), Service Prices (3), and Repair Price by Reputation (4). This presentation-only update caps and centers editable Economy cards: up to 370 DIP each in two columns or 440 DIP in the narrow single column, rather than stretching each slider along a wide full-width section. Shared source cards have measured nonoverlapping left labels/right values, native min/max and reference ticks, and the universal **handle-only** hover glow. Numbers are accent-colored only when different from the reference, without redundant Base labels. Item Value category cards now center as a capped 2×3 bank with value-safe heading clipping.

Repair-by-reputation now shares the compact source-card layout, preserves its four real source-value-linked order constraints, and uses a subdued separate 1x reference marker in place of extra discount/surcharge text. Trader accepted-condition and broken-value editors still show their real percentage units. No simulated final prices, invented trader-stock amounts or CFG/preset semantics were changed. Both authoritative design catalogs were updated; local `.NET 10` publish and Wine visual acceptance are still required.

## Source-verified NPC Ammo and Breakable Box Ammo — 2026-10-09

The user-provided base GameData and DLC1 CFG dumps establish **three separate ammo domains**:

1. **NPC AMMO** (persistent key `loot.npcWeaponAmmoSupply`) now scales **generic NPC corpse-bonus** ammo from `GameData/StashPrototypes.cfg / NPC_Ammo_Smart`. The source has 43 items / 24 groups / 4 ranks, matched against the uploaded dump by all item coordinates and source counts (FNV32 **0x61073f67**). Writer creates `TCD_NPC_Ammo_Smart` from the real source and references it **only** on the selected difficulty when Extra Corpse Loot = `Medicine + Ammo`. Water/None preserve the chosen multiplier without emitting unused ammo overrides. Real Newbie-rank examples at 1x: 12 Gauge **2–6**, 5.45 **15–20**, .45 ACP **8–12**, live scaled with the writer's integer-half-up count rule. Original spawn chance, caliber, rank, item weight and `MainWeaponAmmoCount` policies are inherited unchanged.
2. **BREAKABLE BOX AMMO** independently uses `GameData/ItemGeneratorPrototypes.cfg / DestructibleStash_AmmoSNG/NATO`. Its Ammo Yield edits counts in eight generator rows; the four Ammo Family Weights independently edit those source weights. Physical SpawnActor wooden ammo boxes in base GameData and DLC1 reference these roots. The stacked SNG/NATO graphs label each caliber and live share, never using an NPC-source amount.
3. **WEAPON-ATTACHED NPC AMMO** is a third audited-only, currently unedited surface: 121 local standard-human weapon rows with `AmmoMinCount/AmmoMaxCount`. The old **242** direct editpoints have been retired from the current writer; earlier README release notes calling those edits the active `NPC Dropped Ammo Amount` describe historical behavior only.

The four-source `NPC LOOT & EQUIPMENT` panel now uses three compact full-click Extra Corpse Loot chips, live generic ammo examples, and a larger nearest-endpoint click/drag zone for the native MIN/MAX Dropped Weapon Condition range. The source mapping and interaction rules are authoritative in `docs/UI-DESIGN-LOCK.md` and `docs/VISUAL-CONTROL-SYSTEM.md`. **Only the CFG structures/values are verified**, not game runtime behavior, WPF build success, inheritance patch resolution or actual NPC corpse loot odds.


## Loot editor redesign (2026-10-09)

The approved Loot tab now shows **six** compact source-bound sections: World Loot Depletion (5), World Placement (6, including Shuffle Unique Weapons), NPC Loot & Equipment (4), Stash Rewards (6), Breakable Boxes (6) and Mutant Part Drops (14). All **40 original Loot source definitions**, native min/max/steps, selected vanilla-difficulty references, preset persistence, CFG mappings and reset behavior are retained.

**Breakable Boxes weights are caliber-identifiable inside both graphs:** SNG segments identify `9×18/9×19`, `5.45`, `12 Gauge`; NATO identifies `.45 ACP`, `9×19`, `5.56`, `12 Gauge`. Each segment has a permanent exact same-color swatch, caliber name and live normalized percentage. Color follows the actual weight source across both pools, even when segment order differs or share is zero. The two physical box-yield scalers remain separate from four relative ammo family weights. Stash tier amounts and three reward-roll sources are consolidated into one six-control board; dropped NPC weapon condition is one genuine MIN/MAX two-handle editor. No simulated global loot probabilities or writer behavior changes.

The effective Loot design is locked by [UI-DESIGN-LOCK.md](docs/UI-DESIGN-LOCK.md), [VISUAL-CONTROL-SYSTEM.md](docs/VISUAL-CONTROL-SYSTEM.md), and the relevant mixer override in [UI-MIXER-GRID-SPEC.md](docs/UI-MIXER-GRID-SPEC.md). A local .NET publish/Wine interaction check remains required.


## Stamina editor layout (2026-10-09)

The approved Stamina UI has four compact, source-bound cards in order: **Stamina & Movement** (4 native controls), **Stamina Usage** (six Movement/Melee action costs), **Exhaustion & Recovery** (two real-SP thresholds and four recovery controls), **Water Stamina Drain** (Medium and Deep water scalers). All 18 existing Stamina settings retain their native CFG paths, ranges, steps, saved presets and references. The two Stamina Usage/Exhaustion cards have separate source anchors and independent R/reset scopes; the water order constraint is preserved. Layout and interaction details are locked in [UI-DESIGN-LOCK.md](docs/UI-DESIGN-LOCK.md) and [VISUAL-CONTROL-SYSTEM.md](docs/VISUAL-CONTROL-SYSTEM.md). Local Wine acceptance remains pending.

## Supported target

The application targets **Windows x64** on **.NET 10 LTS** (`net10.0-windows`).

Linux/CachyOS is supported as the development host for cross-publishing and Wine testing. The development host requires a .NET 10 SDK; the repository-root `global.json` keeps builds on the .NET 10 SDK line while allowing newer .NET 10 feature/patch releases and prevents an accidental move to the next major SDK.

On CachyOS/Arch:

```bash
sudo pacman -Syu dotnet-sdk-10.0
```

`./publish.sh` cross-publishes a **self-contained win-x64** runtime to `publish/win-x64/`, so the target Windows machine does not need a separately installed .NET runtime.

## 16:9 desktop window

- Default size: **1600 × 900** WPF device-independent units (16:9). Minimum: **1024 × 576** (also 16:9).
- Startup fits the default to the available screen work area with a small margin; it does not open off-screen on smaller desktop resolutions.
- The custom maximize button and manual edge/corner resizing both preserve 16:9, including returning to the pre-maximized geometry.
- **Window ratio is distinct from card layout:** large semantic visual controls currently occupy full-width rows. A subsequent responsive grid pass will compact those panels; this change only makes the surrounding window use the intended screen geometry.
- **Mixer + responsive grid source of truth:** [UI-MIXER-GRID-SPEC.md](docs/UI-MIXER-GRID-SPEC.md). Anomaly Effects is now the first coded six-channel vertical mixer prototype; .NET/Wine verification remains pending.

---

# v0.9.26.52 — ALIFE group-size radars

- Added an **ALIFE** tab with separate **Human Group Size** and **Mutant Group Size** radars.
- Each radar has Newbie / Experienced / Veteran / Master axes and keeps **1x = vanilla**.
- Human axes scale the Director `EAgentType::Human` `MaxCount`; mutant axes scale all mutant-type `MaxCount` restrictions for that rank while preserving authored zero caps.
- These controls do **not** modify lair capacity, lair respawn, encounter frequency, or world-population counts.

# v0.9.26.52 — Rationale control reference

- Every rationale panel now ends with a concise **CONTROLS** block describing the actual mouse and keyboard interactions available for that control.
- Aggregate radars document:
  - drag point
  - legend hover
  - mouse wheel over legend text
  - right-click point/legend branch reset
  - Arrow keys / `+` / `-`
  - `R` reset all branches
- Standard sliders document drag/click, vanilla marker snap, wheel-over-value, right-click reset, arrows, Page Up/Down and Home/End.
- Range controls document separate MIN/MAX interaction and marker snapping.
- Choice sliders document discrete slider/marker interaction.
- Toggles document mouse click and Space.
- No gameplay, preset or CFG-writer semantics changed.

# v0.9.26.52 — Interactive radar legend reset

- All 10 aggregate radars now treat the rendered legend text itself as an interactive control surface.
- Mouse wheel behavior is unchanged: scrolling over a legend branch adjusts only that branch.
- Right-clicking a legend branch now performs the same branch reset as right-clicking its radar point.
- Legend hover/wheel/right-click hit testing is constrained to the actual rendered text extent instead of the full-width information row.
- The hit area is derived from the real WPF `FormattedText` width, including the actual right-aligned text placement, so empty space to the side of a legend no longer captures input.
- Existing radar-point right-click, drag, keyboard and `R` reset-all behavior remains unchanged.
- No gameplay, preset or CFG-writer semantics changed.

# v0.9.26.52 — Hardcore scarcity pass

- Reworked **Hardcore** around an intentionally poor, scavenger-like Zone:
  - Loose world loot removal: **80%**
  - Ammo boxes: **75% removed**
  - Food & drink boxes: **70% removed**
  - Medical boxes: **80% removed**
  - F1 boxes: **65% removed**
  - World weapon density: **40%**
  - NPC medical chance: **0.25x**
  - NPC weapon ammo supply: **0.25x**
  - Dropped weapon condition: **10–30%**
  - Cheap / Medium / Rare stash ammo: **0.25x / 0.50x / 0.50x**
  - Trader ammo / medic stock: **0.50x / 0.50x**
- Reworked **Hardcore+** into a near-barren profile:
  - Loose world loot removal: **90%**
  - Ammo / medical boxes: **90% removed**
  - Food & drink / F1 boxes: **85% removed**
  - World weapon density: **20%**
  - NPC medical chance: **0.10x**
  - NPC weapon ammo supply: **0.25x**
  - Dropped weapon condition: **5–20%**
  - All stash ammo tiers: **0.25x**
  - Trader ammo / medic stock: **0.25x / 0.25x**
- Both presets also tighten carry weight, item weight, repairs, prices, hunger, fatigue, radiation, bleeding, equipment durability and medical healing.
- Bandage Healing remains `0` and Food Restores Health remains `OFF` in both.
- Human HP philosophy remains unchanged: difficulty comes from scarcity, lethality and consequences rather than human bullet sponges.

# v0.9.26.52 — Preset-named build triplets

- Final mod output now includes the loaded preset name in all three files.
- Output stem format is `zzzzz_TrueCustomDifficulty_PRESET_NAME`.
- Examples:
  - `zzzzz_TrueCustomDifficulty_Hardcore.pak/.utoc/.ucas`
  - `zzzzz_TrueCustomDifficulty_Mutant_Hunter.pak/.utoc/.ucas`
  - `zzzzz_TrueCustomDifficulty_Anomaly_Runner.pak/.utoc/.ucas`
- Spaces and invalid filename characters in custom preset names are normalized to underscores.
- If no preset has been loaded, the fallback output stem is `zzzzz_TrueCustomDifficulty_Custom`.
- The build-success dialog now reports the actual final triplet filenames instead of the retired static names.
- No gameplay, CFG mapping, writer, preset value or packing semantics changed.

# v0.9.26.52 — Seven custom factory presets

- Replaces the old shipped `Novice / Stalker / Veteran / Master` preset files with seven custom factory presets:
  - Immersive
  - Tactical
  - Scavenger
  - Mutant Hunter
  - Anomaly Runner
  - Hardcore
  - Hardcore+
- The four vanilla difficulties remain the tool's internal reference/baseline model; each custom factory preset retains its own `referenceDifficulty`.
- Startup migration removes only the four exact retired factory filenames from the portable `Presets/` directory. Other user-created JSON presets are preserved.
- Integrates all 188 current player-editable settings in every new factory preset.
- Medical hard-preset rules are now part of the shipped profiles:
  - Hardcore / Hardcore+ / Mutant Hunter: Bandage Healing `0`, Food Restores Health `OFF`
  - Scavenger: Food Restores Health `OFF`, Bandage Healing `10`
- Weird Kettle remains outside the food-healing toggle and is not modified by these presets.
- Adds `docs/FACTORY-PRESETS.md` documenting the seven profiles and migration behavior.

## v0.9.26.52 — Food healing switch

- Adds **Food Restores Health** as a confirmed vanilla-ON toggle under Health → Healing Amount.
- OFF sets ordinary Bread / Fresh Bread / Canned Food / Sausage healing to `0 HP`.
- ON leaves their vanilla healing values unchanged: `8 / 15 / 18 / 12 HP`.
- The switch changes only `ValueMin` / `ValueMax` of those four normal `EEffectType::Health` effects.
- Hunger/satiety remains unchanged.
- **Weird Kettle alternative effects are explicitly untouched.**
- Existing Bandage Healing remains part of the Healing Amount radar and is not folded into this toggle.

## v0.9.26.44 — Radar legend hover + mouse wheel

- Adds branch-aware hover interaction to the text legend/info area of all ten aggregate radars.
- Hovering a branch turns its otherwise muted gray informational text white.
- Mouse wheel up/down over a legend branch adjusts **that branch only**.
- Each radar reuses its existing branch step/tick semantics:
  - custom 0.25x radar steps remain 0.25x where already defined
  - catalog-backed controls keep their existing `TickFrequency`
  - ammo-distribution safety constraints still go through the existing guarded setter
- Existing drag, keyboard, right-click reset and `R` reset behavior remains unchanged.
- No CFG mapping or gameplay semantics changed.

## v0.9.26.44 — Healing Amount + clean radar legends

- Renames the **Medical Healing** section to **Healing Amount**.
- Removes gray/yellow background fills from selected or modified text rows in radar legends/info panes.
- Selected/modified state remains visible through the existing font weight and text/accent color changes.
- Radar handles, geometry, gameplay values, CFG mappings and writer behavior are unchanged.

## v0.9.26.44 — Radar title cleanup

- Removes the redundant inner card title from exactly these aggregate radar sections:
  - Mutant Health
  - Medical Healing
  - Action Costs
  - Mutant Part Drop Chances
  - Item Price Scalers
- Renames the **Mutant Drops** section to **Mutant Part Drop Chances**.
- No radar data, interaction, gameplay values, CFG mappings or writer behavior changed.

## v0.9.26.44 — Larger radar fonts

- Slightly increases text sizes across all ten radar controls for better Wine/WPF readability.
- Applies to axis labels, live values, legend/detail text, info headings, ring captions and interaction/status hints.
- Keeps the compact single-column information layout and right alignment introduced in the previous UI passes.
- Radar geometry, spacing model, gameplay values, CFG mappings, steps, presets and writer routes are unchanged.

## v0.9.26.44 — Right-aligned radar information

- Keeps the existing left-radar / right-information layout.
- Changes radar information text to **right alignment** so rows share a clean visual edge under Wine/WPF.
- Applies consistently to all ten aggregate radar sections.
- Axis labels around the radar itself are unchanged.
- No gameplay values, CFG mappings, steps, presets, writers, or interaction semantics changed.

## v0.9.26.44 — Compact single-column radar text

- Tightens vertical spacing in every radar's right-side information pane for Wine/WPF rendering.
- Long radar legends now use **one right-side column** instead of two:
  - Mutant Part Drop Chances
  - Individual Anomaly Damage
  - Action Costs
  - Mutant Health
- Existing single-column radars were compacted further:
  - Breakable Box Ammo Distribution
  - World Loot Removal
  - Medical Healing
  - Item Price Scalers
- **Stash Ammo Amounts** now stacks Basic / Standard / Rare previews vertically in one right-side column.
- No gameplay values, CFG mappings, steps, presets, writer routes, or radar interaction semantics changed.

## v0.9.26.44 — Ammo radar compile fix

- Restores the missing `DrawClippedText(...)` helper in `AmmoDistributionRadar`.
- Fixes the four `CS0103` errors introduced by the horizontal radar-layout refactor.
- No UI semantics, values, CFG mappings, writer routes, or gameplay behavior changed.
- Adds a dedicated audit guard so a clipped-text call cannot remain without its helper definition.

## v0.9.26.44 — Unified radar side layout

- Reworks all ten aggregate radar sections to use one consistent horizontal composition:
  - **radar visualization on the left**
  - **live text / legend / technical information on the right**
- The right information pane owns the majority of the remaining horizontal space, so values and explanations are easier to scan.
- Updated radars:
  - Breakable Box Ammo Distribution
  - Stash Ammo Amounts
  - Mutant Part Drop Chances
  - Individual Anomaly Damage
  - World Loot Removal
  - Medical Healing
  - Action Costs
  - Item Price Scalers
  - Mutant Health
- Existing live information is preserved; only its visual placement changed.
- No parameter ranges, steps, CFG mappings, presets, writer routes, or gameplay semantics changed.

## v0.9.26.34 — Item Price Scalers 0.25x radar steps

- Changes **Item Price Scalers** radar interaction to fixed **0.25x** increments.
- Dragging snaps to `0.00 / 0.25 / 0.50 / ... / 2.00`.
- Arrow keys also move the selected category by `0.25x`.
- Vanilla remains `1x`; writer routes and price-multiplier semantics are unchanged.

## v0.9.26.34 — Item Price Scalers radar

- Combines the six **Item Price Scalers** into one radar:
  - Consumables
  - Ammo
  - Armor
  - Weapons
  - Artifacts
  - Mutant Parts
- Preserves each original control's:
  - current price multiplier
  - `1x` vanilla reference
  - original price meaning
  - radar interaction uses `0.25x` steps
  - presets and writer routes
- Radar scale is the actual existing `0x–2x` price multiplier; this is not a normalized approximation.
- Dragging snaps to `0.25x` steps.
- Right-click resets one category to `1x`; `R` resets all six.
- No mouse-wheel value control.

## v0.9.26.34 — Dedicated F1 boxes separated from World Ammo Boxes

- `World Ammo Boxes` now excludes all dedicated `DestructibleStash_F1` actors.
- Broad Ammo/RGD removal population is now **981 base + 334 DLC = 1,315**:
  - `DestructibleStash_AmmoSNG`
  - `DestructibleStash_AmmoNATO`
  - `DestructibleStash_RGD`
- `F1 Grenade Boxes` remains its own fully independent physical population: **61 base + 40 DLC = 101**.
- **World Loot Removal** is now a five-axis radar:
  - Ammo Boxes
  - F1 Grenade Boxes
  - Food & Drink
  - Medical Boxes
  - Loose Loot
- The radar keeps separate live Base / With DLC removal counts for every axis.
- The two writer surfaces are audited as disjoint, so `World Ammo Boxes` can no longer remove dedicated F1 boxes.

## v0.9.26.34 — Action Costs radar

- Combines the full **Action Costs** section into one seven-axis radar:
  - Sprint
  - Jump
  - Vault
  - Normal Melee
  - Strong Melee
  - Buttstock
  - Aiming
- Preserves each original slider's:
  - current raw value
  - unit / percentage semantics
  - vanilla value
  - relative multiplier
  - value meaning
  - original catalog step
  - preset and writer route
- Direct stamina costs remain raw stamina values; **Aiming** remains a positive player-facing recovery-penalty percentage backed by the existing signed CFG transformation.
- Dragging snaps back to each backing slider's original step.
- Right-click resets one action to vanilla; `R` resets all seven.
- No mouse-wheel value control.

## v0.9.26.34 — Medical Healing radar

- Combines the four **Medical Healing** sliders into one radar:
  - Bandage
  - Medkit
  - Army Medkit
  - Scientific Medkit
- Preserves each original direct-healing CFG route, raw HP value, catalog step, preset behavior, and vanilla reference.
- The live legend keeps the information the old controls emitted by showing:
  - current healing in HP
  - current multiplier vs vanilla
  - vanilla healing in HP
  - `Health restored`
- Radar scale is normalized to `0x–2x` of each item's own vanilla healing.
- Right-click resets one item to vanilla; `R` resets all four.
- No mouse-wheel value control.

## v0.9.26.34 — World Loot Removal radar

- Combines five independent world-loot removal controls into one **World Loot Removal** radar:
  - Ammo Boxes
  - Food & Drink Boxes
  - Medical Boxes
  - Loose Loot
- Preserves the original 0%-100% build-time removal semantics and existing writer routes.
- The live legend preserves and improves the emitted information by showing:
  - current removal percentage
  - exact base-game removals
  - exact DLC-inclusive removals
- Preview counts use the same `MidpointRounding.AwayFromZero` rule as the production writers.
- **F1 Grenade Boxes** is a separate radar axis and a disjoint physical population from ordinary Ammo/RGD boxes.
- No mouse-wheel value changes are used by this radar.

## v0.9.26.34 — Effective Damage in anomaly radar

- Adds **Effective Damage** directly to every row in the **Individual Anomaly Damage** radar legend.
- Each normal anomaly now shows `raw damage · individual x · Effective ...`.
- Razor keeps its special effective low/high damage representation from the existing computed-value logic.
- Effective values continue to update from the separate **Global Anomaly Damage** slider.
- No writer or gameplay semantics changed.

## v0.9.26.34 — Environment anomaly ordering

- Moves **Global Anomaly Damage** to the bottom of the Environment controls.
- Places **Individual Anomaly Damage** immediately below it as the final Environment control.
- No gameplay, preset, writer, or radar behavior changed.

## v0.9.26.24 — Individual Anomaly Damage radar

- Removes mouse-wheel value changes from the **Mutant Part Drop Chances** radar.
- Combines the 11 audited individual anomaly-damage controls into one **Individual Anomaly Damage** radar.
- Radar axes are normalized to `0x–2x` of each anomaly's own vanilla base damage, so unlike raw damage values they remain directly comparable.
- The backing parameters and writer routes remain unchanged; each radar point writes the same raw CFG value and respects the original catalog step.
- **Global Anomaly Damage** remains a separate slider and still composes on top of all individual anomaly values.
- Right-click resets one anomaly to vanilla; `R` resets all individual anomaly damage values.

## v0.9.26.24 — Mutant radar compile fix

- Fixes three C# build errors in `MutantDropRadar.cs` by explicitly converting vanilla `JsonElement` values with `GetDouble()`.
- Also fixes a latent aggregate-state recursion issue where the radar anchor's `IsModified` provider could call its own `IsModified` state.
- No gameplay/writer semantics changed.

## v0.9.26.24 — Mutant Part Drop Chances radar

- Replaces the long **Mutant Drops** slider stack with one combined **Mutant Part Drop Chances** radar.
- Keeps all original per-mutant writer routes and preset data intact; only the UI presentation changes.
- Shows a compact legend with each mutant's current chance and vanilla chance.
- Supports drag editing, mouse-wheel nudging, per-mutant right-click reset to vanilla, and `R` to reset the whole radar.

## v0.9.26.24 — Stash ammo preview units and alignment

- Adds `rounds` to every live stash-ammo count range, e.g. `18–24 rounds`.
- Left-aligns the **BASIC / STANDARD / RARE** column headings with the ammo definitions beneath them.
- Keeps each column clipped to its own width so longer ammo names cannot bleed into neighboring columns.

## v0.9.26.24 — Live Stash Ammo preview by tier

- Restores **Basic / Standard / Rare** labels around the three radar vertices.
- Replaces the old single Basic-only practical quantity text with three live preview columns below the radar:
  - **BASIC** on the left
  - **STANDARD** in the center
  - **RARE** on the right
- Each column shows its current multiplier plus the concrete ammo stack ranges produced by that tier.
- Preview values update immediately as the corresponding radar point moves.
- The preview uses the same positive round-half-up count scaling as the stash-ammo writer.
- Long tier-specific ammo rows are clipped inside their own column instead of overflowing the card.

## v0.9.26.24 — Clear Stash Ammo radar labels

- Moves the Stash Ammo Amounts tier labels into a fixed bottom legend:
  - **BASIC** on the left
  - **STANDARD** in the center
  - **RARE** on the right
- Each tier label shows its current multiplier directly underneath.
- The radar remains fully interactive; only the label layout changed.

## v0.9.26.24 — Stash Ammo Amounts radar

- Combines the existing **Basic Stash Ammo**, **Standard Stash Ammo**, and **Rare Stash Ammo** sliders into one **Stash Ammo Amounts** radar.
- Three axes: **Basic**, **Standard**, **Rare**.
- Each axis keeps the existing `0.25x–2x` quantity semantics in `0.25x` steps.
- `1x = VANILLA`.
- The radar changes only the already-audited ammo `MinCount/MaxCount` routes for each stash tier.
- Vanilla ammo `Chance`, `Weight`, rank gates, and `PossibleItems` membership are unchanged.
- Right-click resets only the selected tier; `R` resets all three.
- This removes two standalone Loot rows while preserving all three preset/writer values.

## v0.9.26.24 — Consistent ammo spawn metrics

- Breakable Box Ammo Distribution now gives every concrete ammo row the same information type.
- One-pool ammo shows a single weighted Spawn percentage.
- Ammo that exists in multiple vanilla breakable-box pools shows a Spawn range instead of two unlabeled percentages.
- Example: `0.75x · 9×19 · Spawn 20–21.4%`.

## v0.9.26.24 — Breakable Box Ammo Distribution readability polish

- Each grouped ammo-family row now shows the multiplier on every concrete ammo line, not only once at the group level.
- Breakable Box Ammo Distribution now uses tighter fonts and smarter left/right label anchoring to reduce overflow under Wine.
- The radar labels now use a more explicit inner-ring caption: `1x = VANILLA`.
- Label spacing and radar geometry were tightened to reduce excess empty space and side clipping.

## v0.9.26.24 — Ammo Distribution variant labels

- Removes the player-facing `E / W` abbreviations from Breakable Box Ammo Distribution.
- Each radar axis now shows the actual ammo variants it affects and their current effective `Weight`.
- Example: `9×18: 2 · 9×19: 2` at vanilla `1x`.
- Internal vanilla ammo-pool names remain implementation detail only.

## v0.9.26.24 — Wording cleanup + mutant health audit

- Renames **NPC Dropped Ammo Amount** to **NPC Dropped Ammo Amount**.
- Renames **Basic Stash RGD-5 Grenade Amount** to **Basic Stash RGD-5 Grenade Amount**.
- Renames the Breakable Box Ammo Distribution **9 mm** axis to **9×18 / 9×19 mm**.
- Adds a source audit for mutant `MaxHP`.
- Confirms mutant health is directly editable per mutant, but vanilla `DifficultyPrototypes.cfg` has no native mutant-HP-by-difficulty field.

## v0.9.26.24 — Grouped vanilla Breakable Box Ammo Distribution

- Replaces the six per-caliber radar axes with four player-facing ammo families:
  - **9 mm** = 9×18 + 9×19
  - **5.45 / 5.56 mm** = 5.45×39 + 5.56×45
  - **.45 ACP**
  - **12 Gauge**
- The radar now changes **only existing vanilla Weight fields**.
- Breakable Box Ammo Distribution never adds or changes `PossibleItems` membership.
- 9×39 and all 7.62 ammo remain outside breakable-ammo distribution.
- `1x` is true vanilla on every family axis.

## v0.9.26.24 — Full regular ammo caliber audit

Audit-only; no new caliber is added to breakable ammo boxes yet.

- Confirms **9 proven regular weapon calibers** across base + DLC.
- Current Breakable Box Ammo Distribution covers 6.
- Adds three clean future candidates: **9×39 mm**, **7.62×51 mm**, **7.62×54 mm**.
- Explicitly excludes **7.62×39 mm** for now because no audited base/DLC weapon uses exact `EAmmoCaliber::A762`.
- Keeps Gauss, grenade-launcher and RPG ammunition outside the regular ammo distribution domain.
- Records that newly added calibers must use **0x as vanilla**, because vanilla destructible ammo pools do not contain them.

## v0.9.26.24 — F1 box percentage semantics

- Inverts **F1 Grenade Boxes** to match the other world-box controls.
- **0%** = vanilla / remove none.
- **100%** = remove all audited F1 boxes.
- Helper now reports the number of F1 boxes removed.

## v0.9.26.24 — Radar Wine spacing and per-point reset

- Gives the Breakable Box Ammo Distribution radar more vertical room so bottom labels and helper text do not overlap under Wine.
- Right-clicking a radar point now resets **only that caliber** to `1x`.
- `R` still resets all six calibers to vanilla.

## v0.9.26.24 — Radar compile fix

- Fixes C# CS0136 variable shadowing in `AmmoDistributionRadar.OnKeyDown`.
- No gameplay or radar behavior changes.

## v0.9.26.24 — Breakable Box Ammo Distribution radar

- Adds one interactive **Breakable Box Ammo Distribution** spider/radar control under Breakable Containers.
- Six axes: 9×18 mm, 9×19 mm, .45 ACP, 5.45×39 mm, 5.56×45 mm and 12×70 Buckshot.
- Range is **0x–3x** in **0.25x** steps; the **1x vanilla ring** is shown directly on the graph.
- Drag points directly; keyboard adjustment and right-click reset are also supported.
- `9×19 mm` and `12×70` update both internal ammo pools from one player-facing axis.
- The radar changes only relative `Weight`; **Ammo from Breakable Boxes** continues to control MinCount/MaxCount.
- The writer prevents all candidates in either ammo pool from being set to 0x simultaneously.

## v0.9.26.24 — Destructible ammo weight audit

Audit-only; no ammo-weight controls are enabled yet.

- Maps the six ammo models used by dedicated breakable ammo boxes to player-facing calibers.
- Confirms both destructible ammo pools use equal vanilla weights: `2 / 2 / 2 / 2`.
- Proves the physical production surface: **934 base + 301 DLC = 1,235** ammo-box routes.
- Establishes six caliber-based candidate weight sliders without exposing internal SNG/NATO pool terminology.
- Keeps Weight (distribution) separate from MinCount/MaxCount (quantity).

## v0.9.26.24 — F1 box wording

- Player-facing F1 text now consistently uses **Box / Boxes**, never Crate.
- Player-facing F1 text now says **DLC**, not DLC1.
- Internal CFG/resource scope identifiers remain unchanged.

## v0.9.26.24 — F1 Grenade Boxes

- Adds **F1 Grenade Boxes** as a World Loot density slider.
- **100%** keeps vanilla F1 box placement; **0%** removes every audited dedicated F1 box.
- Audited production surface: **61 base + 40 DLC1 = 101** physical `D_WoodenAmmoCrate_01` actors using `DestructibleStash_F1`.
- Selection is random without replacement on every Build.
- NPC, trader, quest/reward and shared consumable-generator F1 entries remain untouched.

## v0.9.26.24 — RGD-5 stash wording

- Renames **Basic Stash RGD-5 Chance** to **Basic Stash RGD-5 Grenade Chance** so the control is unambiguous.

## v0.9.26.24 — World detector toggle semantics

- Inverts **Remove World Detectors** to normal opt-in removal semantics.
- **OFF** is vanilla/no-op.
- **ON** removes the audited loose and safe-stash detector spawns.

## v0.9.26 — World detector spawn toggle

- Adds **World Detectors** as a simple ON/OFF World Loot toggle.
- ON preserves vanilla audited detector placement.
- OFF removes **4 base** active direct detector pickups.
- With DLC1 it additionally removes **3 Gilka direct pickups + 1 X16 stash Bear**, for **8 total** world detector spawns.
- Quest/reward, trader, NPC/player-loadout, test-map and deferred detector domains remain untouched.

## v0.9.26 — Loose world detector audit

Audit-only; no detector gameplay control is enabled yet.

- Proves **4 active base / 7 DLC1-inclusive** loose detector pickups.
- Safe loose detector models are only **Echo** and **Gilka**.
- Excludes one `Gilka` WorldMap actor with `SpawnOnStart=false`.
- Confirms Bear/Veles are not active loose WorldMap pickups; Svarog direct placement is test-map only.
- Keeps quest detector `EchoE01` completely outside the loose detector domain.
- Establishes exact-multiset shuffle as the safest future design: base `Echo×2 + Gilka×2`, DLC1 `Echo×2 + Gilka×5`.

## v0.9.26 — World Attachment Shuffle

- Adds **World Attachment Shuffle** as a standalone World Loot toggle.
- Shuffles the exact audited attachment multiset across **62 base / 67 DLC1-inclusive** physical slots.
- Hard category boundaries: optics, suppressors, magazines and underbarrel attachments.
- Progression movement is limited to **same / ±1 tier**.
- All **55 generator-backed physical usages** receive isolated TCD-owned clone-and-redirect generators.
- Direct attachment pickups patch both runtime item SID fields.
- Traders, benches, quest/reward and internal/default attachment surfaces remain untouched.
- Status remains `runtime_test_required` until the generated mod is tested in-game.

## v0.9.26 — World attachment progression-tier audit

Audit-only; no attachment gameplay control is enabled yet.

- Assigns progression tiers to all **39** conservative safe-world attachment models.
- Tier distribution: **18 T2 / 12 T3 / 9 T4**.
- Uses trader attachment tiers where they agree with world progression.
- Uses `FittingWeaponsSIDs`, attachment upgrade type, cost and world placement rank for magazine/underbarrel cases.
- Preserves actual world availability when trader grouping conflicts with world placement (`EN_Silen_1` -> T2).
- Establishes category + same/±1-tier as the candidate future shuffle boundary.

## v0.9.26 — World attachment placement audit

Audit-only; no attachment gameplay control is enabled yet.

- Identifies **54** real inventory-loot attachment prototypes from the 217 `TemplateAttach` descendants.
- Locks a conservative **39-model safe-world attachment pool**.
- Resolves **67** conservative physical placement usages across direct pickups and generator-backed world locations.
- Separates optics, suppressors, magazines and underbarrel attachments.
- Excludes quest/reward, trader/bench-only, internal/default and unproven attachment surfaces.
- Documents the two shared generator definitions that will require clone-and-redirect if physical locations are shuffled independently.

## v0.9.26 — Guaranteed weapon-copy density

- World Weapon Density now guarantees a minimum copy count for **every one of the 36 regular weapons**.
- The live helper changes with the slider: **once**, **twice**, **3 times**, etc.
- Compatible redistribution uses pistol/primary slot type plus at most **±1 progression tier**.
- Base topology guarantees up to **6 copies each**; DLC1 topology up to **8 copies each**.
- The writer uses exact max-flow allocation before filling additional density locations.

## v0.9.26 — World Weapon Density semantics

- **All 36 regular weapon models now spawn at every density value.**
- Density controls **how many safe locations contain weapons**, not how many weapon models are enabled.
- 0% = sparsest valid layout: **36 occupied locations**, exactly one per regular weapon model.
- 100% = all audited regular locations remain populated: **296 base / 376 with DLC1**.
- Intermediate values linearly fill the range between those limits and are freshly randomized per Build.
- Weapons remain only in locations where that same model was already authored, preserving progression placement.

## v0.9.26 — Unique World Weapon Density

- Reworks **World Weapon Shuffle** into a sparse unique distribution over the audited world-loot weapon surface.
- Adds **World Weapon Density** (0-100%, 5% steps).
- 100% means all **36 regular weapon models** survive once each; 50% means 18, 25% means 9.
- Each selected weapon survives in one random **authored** safe location; it is never transplanted into an unrelated progression location.
- Base game: **296** safe candidate locations. With DLC1: **376**.
- Quest/scripted, unique, Guard, Gauss, RPG and other special placements remain untouched.

## v0.9.26 — World weapon master-list audit

Audit-only release; current shuffle behavior is unchanged.

- Locks the regular base-game weapon master list at **36 models** from `ItemPrototypes.cfg`.
- Confirms all 34 existing shuffle-pool entries are legitimate current base weapon prototypes.
- Adds `GunFora_ST` (Fora-221, T2) and `GunPKP_MG` (RPM-74, T3) to the proposed future 36-model distribution pool.
- Documents why `myriflepack_` / `nwpack_` localization namespaces do not imply mod-only weapons.
- Keeps Gauss/RPG, unique variants and dev/test weapon data outside the regular pool.

## v0.9.26 — World weapon tier audit

Audit-only release. No gameplay behavior changed.

- `GunFora_ST` is now classified as **T2**.
- `GunPKP_MG` is now classified as **T3**.
- All 36 regular base-game weapon models now have a progression tier for the planned sparse unique world-weapon distribution system.

## v0.9.26 — World weapon placement audit

Audit-only release. No gameplay behavior changed.

- Proves **403** direct weapon actors on exact main `WorldMap_WP`.
- Conservatively classifies **362** of them as regular safe direct shuffle locations.
- Combined with the existing 14 proven stash-generator slots, the project now documents **376 proven safe weapon spawn slots** with DLC1 installed.
- Documents **36 regular base-game weapon models** for the proposed one-per-model world distribution system.

## v0.9.26 — Baseline helper wording

Updated the vanilla difficulty baseline helper text to:
**Keep your chosen vanilla difficulty and customize how it plays.**

## v0.9.26 — Loot display cleanup

- **NPC Dropped Ammo Amount** now uses `x` multiplier display instead of `%`.
- The yellow lower helper line is removed from **NPC Dropped Ammo Amount**.
- **Breakable Containers** quantity helper text no longer shows internal `Weight` values.

## v0.9.26 — Difficulty marker spacing

Combined difficulty markers now use spaces around the separator, e.g. **NOVICE / STALKER**.

## v0.9.26 — Uppercase difficulty markers

All player-facing difficulty markers shown on sliders now use full uppercase names: **NOVICE / STALKER / VETERAN / MASTER**.

## v0.9.26 — Full difficulty names

Vanilla difficulty markers now use full player-facing names: **Novice / Stalker / Veteran / Master** instead of abbreviated labels.

## v0.9.26 — Stash tier naming

Player-facing stash tier names are now:
- **Basic Stash** (internal Cheap tier)
- **Standard Stash** (internal Medium tier)
- **Rare Stash** (internal Expensive tier)

Internal parameter IDs, CFG generator names and writer mappings are unchanged.

## v0.9.26 — Combat UX and stash ammo range

- Clarifies **Healed Damage Still Counts** and adds a persistent helper line under the toggle.
- Replaces Unicode right-arrow glyphs in generated loot helper text with Wine-safe ASCII `->`.
- Cheap, Medium and Rare Stash Ammo now allow **0.25x** as the minimum amount.

## v0.9.26 — Complete anomaly damage display

- All direct anomaly health-damage controls now use `x` presentation instead of percentage presentation.
- Effective Damage is shown for Electro, Chemical, Fire, Carousel, Toxic Cloud, Steam, Clicker, Razor, Soap Bubble, Fireball and Diamond.
- Razor keeps its source-grounded two-tier damage model: low-speed 20-25 and high-speed 100 before multipliers.
- Flycatcher movement/turn speed, Razor Bleeding, Diamond Bleeding and Psy Intensity remain effect controls rather than being mislabeled as health damage.

## v0.9.26 — Economy labels and anomaly base-damage display

- `Minimum accepted condition` -> `Minimum accepted item condition`.
- `Mutant Parts Value` -> `Mutant Parts Cost`; value descriptor is now `Parts value`.
- `Coupon Rewards` is positioned immediately below `Starting Coupons`.
- Electro, Chemical, Fire, Carousel, Toxic Cloud and Steam Base Damage controls now display as vanilla-relative `x` multipliers instead of percentages. CFG values remain the same raw damage values.

## v0.9.26 — Trader Minimum Buy Condition

- Adds **Trader Minimum Buy Condition** under Economy > Trading.
- Direct 0-100% condition gate; vanilla is 40%.
- 0% allows completely broken weapons and armor to pass the trader condition gate.
- Explicit production allowlist: 68 base-game + 18 DLC1 trade profiles.
- Pricing, stock, money, refresh, discounts and repair rules are unchanged.

## v0.9.26 — Blueprint Shuffle

- Adds **Blueprint Shuffle** under World Loot.
- Audits 80 blueprint SIDs: 77 proven one-to-one world placements are eligible; the Faust quest blueprint and 2 unresolved stash blueprint generators are excluded.
- ON performs a fresh build-time derangement without replacement: same 77 blueprint locations, same 77 blueprint items, no duplicates, and no included blueprint remains at its vanilla location.
- OFF emits no blueprint patch.

# True Custom Difficulty


## v0.3.6 — DLC-aware Ammo / Food / Medical crate reduction

- Adds **Reduce Medical Crates** to the existing build-time crate-reduction system.
- Medical base population is grounded in the supplied FULL pak: **709** actors; the supplied HALF pak is a pure **353**-actor subset.
- Extends all three box categories through the exact same audited classifier into DLC1 rather than leaving the feature base-game-only.
- Final eligible populations with DLC are **1,315 Ammo/RGD** (981 + 334 DLC), **1,208 Food & Drink** (1,100 + 108), and **868 Medical** (709 + 159).
- Ammo DLC includes **151 SNG + 150 NATO + 33 RGD-5** box actors, preserving the same category semantics as the supplied FULL Ammo reference.
- The shared **Boxes Removed** slider still chooses a new random subset independently for every enabled category at each Build; DLC rows participate only when DLC1 is installed.
- At 50% with DLC1 enabled the exact removal counts are **708 Ammo / 604 Food & Drink / 434 Medical**.
- Weapons and Armor remain intentionally out of scope for now. See [`docs/CRATE-SPAWN-AUDIT.md`](docs/CRATE-SPAWN-AUDIT.md).

## v0.3.5 — Build-time Ammo / Food crate reduction

- Replaces the broad binary crate-spawn surface with two explicit reduction categories: **Ammo Crates** and **Food & Drink Crates**.
- Adds one shared **Boxes Removed** slider from 0% to 100%.
- Every Build chooses a new random subset independently for each enabled category, without replacement.
- Uses the supplied FULL paks as the exact canonical populations: **1,042 Ammo** actors and **1,100 Food & Drink** actors.
- The supplied HALF paks are verified pure subsets (517 / 541 actors) and serve as mechanism proof only; 50% in True Custom Difficulty removes the exact mathematical half: **521 Ammo / 550 Food**.
- Ammo/RGD removal excludes all dedicated F1 actors; the separate F1 surface owns those 61 base + 40 DLC boxes.
- No Medical, Weapons, Armor or DLC1 box population is exposed or extrapolated yet.
- Crate presence remains independent from loot-content/quantity controls. See [`docs/CRATE-SPAWN-AUDIT.md`](docs/CRATE-SPAWN-AUDIT.md).

## v0.3.3 — v0.3.2 compile hotfix

- Fixes the v0.3.2 C# compile error where `LootHintEntryDefinition` was accidentally declared twice in `Models/ParameterCatalog.cs`.
- Restores `DisplayDefinition` to its intended display-only shape; loot entry metadata lives only under `LootHintDefinition`.
- Adds a static guard against duplicate top-level C# type definitions in the canonical production source tree.
- No gameplay, CFG mapping, loot contents, Chance/Quantity semantics, parameter IDs or preset format changed.


## v0.3.2 — Loot contents and paired RGD-5 controls

- Basic-stash RGD-5 now has two independent controls: **Chance** and exact **Quantity (0–4)**.
- Ammo/stash/crate quantity controls show their actual authored ammo contents and vanilla → current stack ranges.
- Ammo names are grounded in the runtime `ItemPrototypes.cfg` caliber/ammo-type definitions instead of opaque SIDs.
- Weight-based selection is labelled as Weight, not misrepresented as a percentage Chance.
- Human NPC Dropped Ammo Amount explicitly follows the ammo payload of the selected weapon row; it is not a fixed caliber pool.

## v0.3.1 — Repository and publish layout cleanup

- Reduces the repository root from **51 files to 6** build/navigation entry points.
- Removes duplicated root audit/mapping Markdown and standalone JSON/CSV mirrors; canonical documentation now lives only under `docs/` and canonical audit/runtime data under `Data/`.
- Replaces the duplicated `docs/README.md` release log with a concise documentation index.
- Keeps `src/TrueCustomDifficulty/` as the single production C#/XAML tree.
- Changes both publish scripts to emit the portable Windows build under `publish/win-x64/` instead of copying runtime files back into the source root.
- The published runtime gets its own `tools/`, `Presets/`, and `Output/`, so user-supplied `repak`/`retoc` never mix with source Python tooling.
- Adds a permanent root-layout guard: documentation/data mirrors and runtime publish files are not allowed to reappear in the repository root.
- No gameplay mapping, parameter ID, CFG target, writer operator, preset format or runtime-test status changed.

See [`docs/README.md`](docs/README.md) for the documentation index and [`docs/SOURCE-LAYOUT.md`](docs/SOURCE-LAYOUT.md) for the canonical source/publish layout.

## v0.3.0 — Production implementation

- Freezes the audited gameplay scope as the production application contract: **178 catalog parameters = 167 visible controls + 11 hidden audit-only parameters**.
- Confirms the live catalog-driven UI renders **159 sliders + 7 toggles + 1 choice slider** across all 9 tabs; no gameplay control is manually instantiated outside the catalog path.
- Fixes a real dependency/output bug: custom Master child values are preserved in presets but are now **excluded from CFG output while their parent mechanics bundle is OFF**.
- Keeps the footer counts aligned with the effective build by counting only currently active controls.
- Fixes Master selection behavior so the side panel cannot stay focused on a child that was just hidden by switching its parent bundle OFF.
- Hardens startup catalog validation for tabs, control shapes, slider bounds/steps, vanilla value types/ranges and `requiresToggle` relationships.
- Removes all historical duplicate C#/XAML project trees. The **only** production source is now `src/TrueCustomDifficulty/`; the solution and both publish scripts point there.
- Adds `docs/PRODUCTION-IMPLEMENTATION.md`, `docs/SOURCE-LAYOUT.md`, and canonical production-integration evidence under `docs/production-integration/v0.3.0/`.
- No gameplay mapping, CFG target, preset ID or runtime-test status was changed. The v0.2.54 placement/provenance contract remains the source truth for where every control writes.

## External tools

The repository `tools/` directory contains project audit/verification scripts. Third-party runtime executables are **not** stored there.

True Custom Difficulty does **not** bundle or download `repak` or `retoc`; users supply them manually in the published runtime directory:

```text
publish/win-x64/
  TrueCustomDifficulty.exe
  tools/
    repak.exe
    retoc.exe
  Presets/
  Output/
```

## v0.2.54 — CFG provenance closure

- Closes the static provenance chain for the complete writer surface without adding any EXE/Wine/runtime verification workflow.
- Direct writer surface is fully provenance-covered: **1370/1370** edit points = **699 historical runtime-2.0.5 exact** + **671 freshly revalidated against embedded source inventories**.
- The 671 later entries close exactly as Dropped Weapon Condition 242 + NPC Dropped Ammo Amount 242 + Zombie Grenade Frequency 91 + Medic Stock 96.
- Special writer surface is fully provenance-covered: **25/25** routes = 18 retained historical runtime-structure proofs + 7 freshly revalidated clone/redirect routes.
- Fresh clone template validation matches **1197/1197 source fields**; canonical production redirect manifests retain **3476 source-grounded rows**.
- Retains the original runtime-dump SHA-256 from the v0.1.63 raw-source audit and fingerprints the current retained 699-entry legacy tuple set so it cannot silently drift from this release onward.
- Explicitly records that the raw runtime 2.0.5 dump is not bundled, so historical raw-dump proof is never mislabeled as a fresh v0.2.54 raw-source re-read. See `docs/CFG-PROVENANCE-AUDIT.md`.

## v0.2.53 — CFG placement audit

- Added a project-wide **CFG-only** placement audit for all **178** writer-routed parameters. No EXE/Wine/headless verification is used or required.
- Locked **153 direct parameters / 1370 concrete edit points across 14 CFG files** and **25 explicit special writer routes**.
- Every direct edit point must agree with the catalog target on scope, CFG file, operation and field key; exact prototype/node/source values remain locked by `Data/cfg-writer-baseline.json`.
- Confirmed **0 cross-parameter direct CFG-field collisions** and **0 unsupported writer routes**.
- Added `tools/audit_cfg_placement.py`, `docs/CFG-PLACEMENT-AUDIT.md`, and canonical evidence under `docs/cfg-placement/v0.2.53/`.
- Explicitly keeps verification at the CFG mapping/output-contract level; runtime behavior remains a later in-game concern only for controls still marked `runtime_test_required`.

## v0.2.52 — Full runtime-pending override verification

- Expands `tools/verify_cfg_overrides.py` from 3 direct controls to **all 30 visible `runtime_test_required` controls**.
- Verifies both assignments and CFG struct shape (`{bpatch}`, plain structs and `{refurl/refkey}` roots); `compare` fails on structural drift as well as wrong values.
- Master suite: **30 controls / 90 cases**, every VANILLA case produces zero assignments, and all 58 non-zero reference cases self-compare exactly.
- Adds a Veteran special-array suite to exercise the lower-difficulty full-subtree writer branch: **12 controls / 36 cases**, with 23 non-zero cases self-compared.
- Fixes a real production bug in NPC Medical Loot Frequency: the writer still expected the obsolete 37-link redirect manifest even though the audited production manifest has **33** redirects.
- Hardens the patch parser for valid prototype SIDs containing spaces. Full evidence lives under `docs/override-verification/v0.2.52/`.

## v0.2.51 — Override verification harness

- Adds `tools/verify_cfg_overrides.py` for deterministic original→override verification of ordinary direct writer edit points.
- Separates baseline **CONTRACT_PASS** from actual production-writer `PASS`; the latter requires a real isolated writer staging tree.
- Initial MIN/VANILLA/MAX evidence covers NPC Weapon Condition, NPC Dropped Ammo Amount and Medic Stock Quantity.
- VANILLA produces zero expected assignments for all three controls; unexpected, missing or wrong-value assignments fail `compare`.
- Full field-level evidence and reference BPatch trees live under `docs/override-verification/v0.2.51/`.

## v0.2.50 — Global cross-domain control audit

- Audits the complete UI as one surface: **178 catalog parameters -> 167 visible controls + 11 hidden/audit-only parameters**.
- Locks the visible split at **137 confirmed + 30 runtime-test-required**, with **140 non-generator + 27 generator-backed** controls.
- Proves **0 concrete writer-target collisions** between different parameter IDs.
- Locks five distinct Master `AdditionalMechanicsEffects` list members and all **15** `requiresToggle` dependency edges as intentional composition.
- Normalizes the final four engine-negative Master penalty/loss sliders to positive player-facing magnitudes while `negateSet` preserves the exact original CFG values.
- Migrates pre-v0.2.50 presets carrying those old negative values without changing gameplay output.
- Documents Global Anomaly Damage + six individual anomaly base-damage controls as intentional layered composition, not duplicate controls.

## v0.2.49 — Equipment deep audit

All 8 normal Equipment controls were audited end-to-end. The surface is locked at 7 confirmed + 1 runtime-gated (`Armor Durability`) with 117 writer edit points. No gameplay control was added or removed. The only correction is evidence drift in Player Weapon Wear Per Shot: the current writer has 41 base-game player prototypes (40 non-zero + one explicit zero) plus 11 DLC1 entries, for 52 total wear edit points. See `docs/EQUIPMENT-CONTROL-DEEP-AUDIT.md`.

## v0.2.48 — Economy control deep audit

- Re-audited all **20 normal Economy controls** end-to-end against the locked catalog/writer baseline, covering **341 concrete CFG edit points**.
- Keeps the verified split at **17 confirmed + 3 runtime-test-required** (`Trader Buy-From-You Pricing`, `Trader Sell-To-You Pricing`, `Medic Stock Quantity`) with 0 invalid/partial controls.
- Clarifies the two trader condition labels so the engine's trader-side `BuyCondition`/`SellCondition` naming cannot be read backwards by the player; IDs, ranges, writer targets and gameplay output are unchanged.
- Locks Fast Travel at 183 `RequiredMoney` edit points (182 paid + one explicit zero), Medic Stock at 96 count edit points, and Mutant Parts Cost at 15 explicit Cost targets.
- Locks the repair reputation relationship to **Friendly <= Neutral <= Disliked <= Hostile** over actual CFG modifier values.
- Keeps `EconomyDifficulty.Repair_Cost` audit-only and keeps the previously closed broad trader-stock controls out of normal UI.

## v0.2.47 — Survival control deep audit

- Re-audited all **23 normal Survival controls** end-to-end against the locked runtime/writer baseline, covering **70 concrete CFG edit points**.
- Keeps all 23 controls: **23 confirmed, 0 runtime-gated, 0 invalid/partial**.
- Replaces raw negative medium/severe hunger stamina-regeneration values with positive player-facing **penalty magnitudes**: vanilla `33%` and `70%`; the writer uses `negateSet` to preserve runtime CFG `-33%` and `-70%`.
- Migrates pre-v0.2.47 presets carrying the old negative hunger values to the new positive representation without changing gameplay output.
- Changes the linked hunger severity constraint to the intuitive value relationship **Medium <= Severe** after the sign normalization.
- Corrects stale Survival documentation that still claimed `LowBleedingDamage = 0.33`; the audited runtime/catalog/writer value is `1.0`.
- Keeps thirst, survival-state activation thresholds, `SleepHunger`, emission-sleep permission and ambiguous movement penalties outside the normal UI.

## v0.2.46 — Stamina control deep audit

- Re-audited all **13 normal Stamina controls** end-to-end: UI semantics, vanilla values, ranges, writer operations and concrete CFG edit points.
- Keeps the verified split at **12 confirmed + 1 runtime-test-required** (`Maximum Vault Height`), with **15 concrete writer edit points** and no invalid/partial control.
- Replaces raw signed **Aiming Stamina Cost** with **Aiming Stamina Recovery Penalty**: UI stores `0..50%` positive magnitude and the writer uses `negateSet` so vanilla `25%` still writes the original `-0.25` CFG coefficient.
- Migrates pre-v0.2.46 presets that stored a negative Aim coefficient to the new positive magnitude without changing gameplay.
- Fixes `Maximum Vault Height` slider ceiling from the inconsistent `260` to the already-documented/marked `300 uu` upper reference.
- Keeps Walk drain, sprint exhaustion helpers, emergency low-SP recovery and internal movement-state coefficients out of the normal UI.
- Synchronizes application/package version metadata and adds a permanent audit check that catalog and canonical `.csproj` versions match.

## v0.2.43 — Non-Generator Control Verification

- Re-audited the normal Combat, Health, Stamina, Survival, Economy and Equipment UI against the locked CFG/writer baseline.
- Locked the current normal non-generator UI at **104 controls**: Combat 25, Health 15, Stamina 13, Survival 23, Economy 20, Equipment 8.
- No new controls were promoted merely because a CFG field exists; ambiguous, duplicate-layer and internal helper fields remain NO-UI/audit-only.
- Explicitly retains Walk stamina drain as NO-UI and keeps Human NPC Detector Chance out of the difficulty UI by design.
- Added `docs/NON-GENERATOR-CONTROL-VERIFICATION.md` and a consistency gate so hidden audit-only parameters cannot silently leak into the normal UI.


## Required for Build Mod

A non-empty build requires both tools:

- **repak** packs the generated BPatch CFG files into `TrueCustomDifficulty_P.pak` using PAK V11 and mount point `../../../`.
- **retoc** creates the matching UE5.5 `TrueCustomDifficulty_P.utoc` and `TrueCustomDifficulty_P.ucas` companion container.

The application does not download either executable. If one is missing, Build Mod stops with a clear error and does not publish a partial output.

The same tools may also be used manually for extraction/development/maintenance workflows, but those workflows are outside the normal player build path.



### v0.2.42 — Runtime-gated production controls

- Promotes **NPC Weapon Condition**, **NPC Dropped Ammo Amount**, and **Medic Stock Quantity** into the normal UI and production CFG writer without claiming runtime proof.
- Locks weapon condition to 242 `MinDurability/MaxDurability` edits and weapon ammo to 242 attached-ammo edits across exactly 121 weapon rows / 33 repeatable-human roots.
- Locks Medic Stock Quantity to 96 `MinCount/MaxCount` edits across exactly 48 rows: 25 Universal + 23 Noon.
- Preserves all hard exclusions: Prologue, named MIK/Mykluha, scripted/shared paths, non-medic trader stock, Chance, PlayerRank and refresh semantics.
- Generator-backed UI projection is now **27 controls**: **16 static verified + 11 runtime-test-required**.
- Runtime verification is intentionally deferred; later proof uses vanilla CFG -> generated override CFG structural diff plus in-game observation.

### v0.2.40 — Final generator decision matrix

- Adds a canonical 28-concept generator decision matrix and lifecycle projection.
- Separates implemented runtime-gated controls from not-yet-UI promotion candidates and closed NO-UI/audit-only decisions.
- Preserves the verified 24-control generator UI projection without changing gameplay writer behavior.
- Establishes the evidence-preservation rule for the next artifact-cleanup phase.

### v0.2.39 — Generator control end-to-end verification

- Audited every generator-backed control currently exposed in the normal UI through `UI -> parameter -> writer -> CFG -> production generator/consumer`.
- Locked the UI surface at **24 controls**: **16 static 1:1 verified**, **8 static-verified but runtime-test-required**, **0 invalid/partial controls**.
- Confirmed that production exclusions in stash/destructible/NPC controls are deliberate hard boundaries rather than hidden coverage gaps.
- Kept the legacy internal `loot.mutant.catChance` / `Cat*` CFG identifiers for preset/source compatibility while the player-facing name remains **Bayun**.
- Added `GENERATOR-CONTROL-END-TO-END-VERIFICATION.md` as the canonical release gate for current generator-backed UI controls.

### v0.2.38 — Generator baseline integrity check

- Re-ran the generator model from raw/local catalogs through inheritance, composition and consumer contexts before adding any new control.
- Locked the reproduced base topology: 3,452 roots / 2,968 inheritance edges / 5,783 composition rows / 13,647 consumer-context edges.
- Explicitly guards the distinction between 99 `human_npc_standard` roots and the isolated 33-root production surface used by medical/weapon runtime candidates.
- Clarified trader inventory as 96 discovered domain roots -> 92 roots with resolved `trader_stock` consumers; the remaining four stay review-only.
- Fixed stale root `control-candidate-triage-summary.json` (v0.2.36) and added byte-identical mirror enforcement across the then-current root / `generators/` / `docs/generators/` layout; v0.2.41 later removed those redundant mirrors.
- Added `GENERATOR-BASE-CHECK.md`. No production generator-control decision or 175-parameter writer route was widened.

### v0.2.37 — Ready-to-install runtime-test packaging

- Adds `tools/runtime_tests/build_runtime_test_mod.py`, which turns one locked LOW/HIGH runtime-test surface directly into a `.pak/.utoc/.ucas` triplet.
- Reuses the production packing contract exactly: repak V11, mount point `../../../`, retoc UE5.5.
- Third-party `repak`/`retoc` remain user-supplied; the runtime tooling neither bundles nor downloads them.
- Runtime-test artifacts use explicit names such as `TCD_RuntimeTest_WeaponAmmo_HIGH_P.*` and remain outside normal UI/preset/writer output.
- The packer validates all three non-empty files before publishing and can optionally retain the generated staging tree for inspection.

### v0.2.36 — Isolated runtime-proof patch tooling

- Adds `tools/runtime_tests/generate_runtime_test_patches.py`; it is deliberately outside the normal UI, preset format and production CFG writer.
- Generates LOW/HIGH BPatch staging trees for dropped weapon condition (`0.01..0.05` / `0.95..1.00`), attached weapon ammo (`1..1` / `77..77`) and medic stock quantity (`1..1` / `20..20`).
- Consumes the locked audit CSV surfaces and refuses generation if the expected 121/33 weapon or 48/25/23 medic boundaries drift.
- Preserves the source `AmmoMaxCount` / `AmmoMaxcount` spelling and emits only the fields required by the selected test contract.
- Runtime tests remain isolated: generating test patches does not promote any candidate into the normal product UI/writer.

### v0.2.35 — Medic Stock Quantity runtime-proof contract

- Locks the medic quantity candidate to 48 direct rows: 25 Universal + 23 Noon.
- Records the safety split: 42 rows at Chance=1 and 6 progression/rarity rows whose Chance must remain untouched.
- Adds a LOW 1–1 vs HIGH 20–20 in-game A/B contract requiring genuine stock regeneration/refresh on both medic paths.
- No UI/writer promotion is claimed until the runtime test passes.

### v0.2.34 — Human NPC weapon-ammo runtime-proof contract
- Locked the candidate to the proven 121-row / 33-loadout repeatable-human production boundary; 2 Prologue + 4 MIK/Mykluha roots remain excluded.
- Confirmed attached-ammo fields on 121/121 rows, including exact source spelling split 118 `AmmoMaxCount` + 3 `AmmoMaxcount`; 81 rows allow zero minimum and 40 require positive minimum.
- Added a LOW `1..1` vs HIGH `77..77` A/B test that isolates attached-ammo fields from loose-ammo generators and avoids zero/fallback ambiguity.
- Kept the control out of normal UI until runtime testing proves where the values manifest; final UI naming must follow observed behavior rather than assume total corpse-loot ammo.

### v0.2.33 — Human NPC dropped-weapon condition runtime-proof contract
- Reconciled stale deferred metadata with the proven production boundary: 121 direct weapon rows across 33 repeatable-human loadouts; 2 Prologue + 4 MIK/Mykluha roots remain hard-excluded.
- Added a strict LOW `0.01..0.05` vs HIGH `0.95..1.00` in-game A/B contract for proving that `MinDurability/MaxDurability` becomes observable dropped-weapon condition.
- Kept the control out of the normal UI until runtime proof; no gameplay writer behavior was promoted in this release.
- Added static guards for 121 rows, 33 roots, 120 vanilla `0.25..0.50` rows, one `0.35..0.45` exception, and the 2+4 exclusion boundary.

### v0.2.30 — Trader ammo stock quantity audit
- Audited 52 reusable trader ammo-pool rows across Soviet/T1/T2/T3/T4/NATO pools; 51 have vanilla `MinCount/MaxCount`.
- Closed `Trader Ammo Stock Quantity` as `NO_UI_INCOMPLETE_QUANTITY_SURFACE` because Soviet `AVOG` is a legitimate trader ammo row with no vanilla count fields.
- Preserved AVOG semantics instead of inventing count fields; documented NATO as cross-context (`trader_stock + quest_scripted`) and clone/rewire-only for any future trader-only edit.
- Added a machine-readable trader ammo quantity surface and consistency guard.

### v0.2.29 — Trader stock aggregate decision closure
- Closed the generic `Trader Stock` candidate as `NO_UI_AGGREGATE_MIXED_SEMANTICS`.
- Documented why Chance, Weight, count, durability/ammo payload, PlayerRank and TradePrototype refresh cannot be represented by one honest percentage.
- Preserved trader subdomains as separate audit targets; `Medic Stock Quantity` remains the already-proven runtime-proof candidate.
- Added a consistency guard preventing the aggregate trader control from drifting back into the UI candidate set.

### v0.2.28 — Medic stock quantity structural audit
- Proved a 48-row medic consumable count surface: 25 `MedicUniversal` + 23 `MedicNoon`.
- Added `Medic Stock Quantity` as `STRUCTURALLY_READY_RUNTIME_PROOF`: only `MinCount/MaxCount` may scale; Chance, PlayerRank, item selection and refresh stay vanilla.
- Kept medic availability/chance and non-medical medic extras out of the candidate.


### v0.2.32 — Trader attachment stock audit
- Closed global Trader Attachment Stock as `NO_UI_MIXED_AVAILABILITY_QUANTITY_SURFACE`.
- Audited 37 direct attachment rows across 11 roots: 29 count `1..1`, 4 count `1..2`, 4 no-count; item Chance ranges 0.1..1.0.
- Confirmed reusable T2/T3/T4 attachment pools are trader-only with 31/17/9 consumer links, but their per-item Chance controls composition/availability rather than quantity.
- Added a static guard for the attachment surface and tier consumer fan-out.

### v0.2.31 — Trader weapon stock audit
- Closed global Trader Weapon Stock as `NO_UI_MIXED_AVAILABILITY_MODELS`.
- Audited 94 direct weapon rows across 11 trader roots: 51 have explicit count=1 while 43 have no count fields.
- Documented separate per-item Chance, tier-link Chance and Weight-based trader models; none is a complete global weapon-stock scalar.
- Added a static consistency guard for the 94-row weapon surface and decision status.

## v0.2.45 — Health control deep audit

- Traces all 15 normal Health-tab controls from UI semantics through vanilla values and writer operations to concrete CFG targets.
- Locks the Health split at 12 statically confirmed + 3 runtime-test-required controls, with exactly 30 concrete writer edit points.
- Fixes six bleeding/radiation treatment sliders to use positive player-facing removal magnitudes while the writer emits the engine's negative delta via `negateSet`.
- Preserves old presets by migrating pre-v0.2.45 negative treatment values to their positive magnitude on load.
- Keeps `RegenHealthModifier`, medical Duration/Charges, `HPThresholdToKill`, action times and NPC wounded-state regeneration outside normal Health UI.
- Adds `docs/HEALTH-CONTROL-DEEP-AUDIT.md` and a consistency gate for the signed-delta boundary.

## v0.2.44 — Combat control deep audit

All 25 visible Combat controls were traced through UI semantics, vanilla values, writer operators and concrete CFG targets. No control was removed or newly promoted. The headshot description was corrected to describe `PlayerWeapon_HeadshotMultiplier` as a multiplier (1.0x neutral), and nested Master mutant/PSY overrides remain runtime-test-required. See `docs/COMBAT-CONTROL-DEEP-AUDIT.md`.


## v0.4.8 World Loot UI

World-loot reduction/shuffle controls are standalone sliders with an explicit `Vanilla` zero position. Ammo crates, Food & Drink crates, Medical crates, and World Weapon Shuffle no longer use enable checkboxes or a shared removal percentage. Pre-v0.4.8 presets are migrated by folding their old toggle state and percentage into the corresponding slider.

## Economy Trading compact redesign (2026-10-09)

The four independent trading controls now share a compact 2×2 horizontal-slider board (one column at narrow widths), using the same Base-reference ticks, handle styling and modified-value highlighting as the existing Stamina-style controls. Trader payout/shop scalers remain independent of the minimum accepted condition and broken-item value floor, and the design does not claim to calculate final trader prices. Native CFG mappings, ranges, preset values and reset behavior remain unchanged. This is the Trading-family portion of the Economy redesign; remaining Economy panels are not yet converted.

### 2026-10-09 — Economy reputation repair compact controls

User-authorized Economy redesign: replace the former read-only repair reputation curve plus four long source sliders with a compact two-column/two-row group of four **independently editable, horizontal Stamina-style cards** (single-column fallback below 672 DIP). The cards are FRIENDLY, NEUTRAL, DISLIKED and HOSTILE, preserving their original `economy.repairFriend`, `economy.repairNeutral`, `economy.repairDisaffection`, `economy.repairEnemy` source bindings. Show the current multiplier with neutral-at-Base/yellow-and-1.2-DIP-larger-when-modified formatting, a neutral one-line surcharge/discount cue compared with 1x, grey selected-Base marker, separate subtle 1x mark, original endpoint bounds and circular handle. Hover must highlight the handle only. Keep original direct drag, selected-slider keyboard edits and resets. The global linked constraint continues to enforce actual source-value ordering FRIENDLY <= NEUTRAL <= DISLIKED <= HOSTILE with priority given to the actively moved slider. No CFG writer/preset changes; 300 DIP wide and 526 DIP narrow board heights. This replaces **RepairCurve only**; the other Economy sections keep their recorded layout until individually converted.

### 2026-10-09 — Global concise-control rule (user-authorized)

Apply across all tabs and all reusable visual-control families: remove redundant instructional footer legends (gold=current, grey=Base, right-click/R reset), duplicate explicit Base reference prose when a reference tick already communicates the same information, and explanatory captions that do not add gameplay meaning. Retain unique game-mechanic explanations, real units, essential source semantics, value labels, state-dependent warnings, accessibility through tooltips, and native reference ticks. Preserve the existing visual family shape, draggable handles, source bindings, keyboard interactions, reset, preset persistence and CFG output. Render modified source numbers gold and +1.2 DIP larger only when genuinely different from the selected vanilla reference; all static text remains neutral. Do not globally strip meaningful subtitles/axis labels or change unrelated locked geometry to fill the vacated space without a specific redesign approval. First implementation pass removes repeated footers and Base prose from shared AggregateVisualControl families, including mixer, explosion, NPC equipment, world loot and toggle-led dashboards. This is a progressive cleanup contract; other dedicated custom renderers must be audited individually rather than assumed to have been changed.

### 2026-10-09 — Global compact-text cleanup: dedicated visual families


## Economy tab — full compact source-bound redesign (2026-10-09)

The Economy tab now has **six full-width semantic sections** with all **22** existing playable source settings visible once, plus one catalog audit-only repair scaler that deliberately stays hidden:

- COUPONS & REWARDS (3): starting coupons, main quest rewards, side quest rewards
- TRADING (4): buy-from-you / sell-to-you pricing, minimum acceptable condition, broken-item value floor
- TRADER STOCK (2): ammunition and medicine quantity scalers
- ITEM VALUE LAYERS (6): consumables, ammo, armor, weapons, artifacts and mutant parts
- SERVICE PRICES (3): fast travel, upgrades and global base repair %
- REPAIR PRICE BY REPUTATION (4): Friendly, Neutral, Disliked and Hostile

The former separate starting/reward rows and the former tall Services/Trader Stock mixer strips have been replaced by compact native horizontal sliders, matching the current Stamina-style controls. Six Item Value Layers use a 2×3 horizontal grid without redundant headings or payout examples. Trading and Repair remain their approved compact four-card layouts. Every native CFG source/slider range, tick, selected Base marker, preset, reset behavior and right-side rationale remains; only modified numeric tokens become gold and slightly larger. See `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md` and the authoritative `docs/UI-MIXER-GRID-SPEC.md` override.

GitHub source changes do not themselves demonstrate a successful .NET publish or Wine runtime. Local build and 16:9 responsiveness verification are required before visual acceptance.

### 2026-10-09 — Global Stamina Usage slider anatomy and neutral references (latest user-approved rule)

**This entry supersedes conflicting older slider-value positions and any requirement to visually emphasize Vanilla/Base values across ALL tabs.** For every ordinary horizontally editable source slider, use one coherent, non-overlapping top line: **source name on the left, editable CURRENT value right-aligned** (shared card left/right padding and reserved space so the label cannot run beneath the current value); place the continuous interactive rail directly below that line, and display any native min/max labels underneath the rail in the same neutral small typography. Do not alternate between left-justified current values on the next line, right-justified current values in the same family, or side-by-side current/Base figures. When a separate preview/semantic graph needs read-only numeric results, keep those unambiguously labeled as derived results rather than a second slider value.

Current source numbers remain text-colored at the selected reference and turn gold **only after a real source deviation**; modified current numbers grow by +1.2 DIP while all units, bounds, captions and explanatory text stay neutral. The interactive rail/handle can retain its gold affordance, but on hover only the **handle** glows. Never color a full card or the displayed value merely because the mouse hovers. **Vanilla/Base references are not highlighted**: retain a thin, unobtrusive grey tick for source comparison and native snapping/reset where already supported; where standard `VanillaMarkerBar` displays clickable reference labels, keep the labels/dots muted even during hover. Do not place gold/white duplicate Base numbers or instructive legends in card headers or footers.

The reusable `AggregateVisualControl.DrawHorizontalTrackCard` and `DrawCompactTrack` now follow this exact hierarchy. This affects shared health, stamina, fatigue, survival, loot, equipment and economy layouts without changing source bindings or hit-target semantics. `DrawDualParameterHeading` uses independent left-title/right-current pairs even for the two-axis graph. Existing Trading, Reputation Repair, Coupon, Service, Stock and Item Value cards already place the live value on the right. The compact vertical mixers, native semantic curves, separate multi-axis controls and Mutant Health's previously approved **species left / multiplier centered / actual HP right** remain differentiated where the meaningful control structure requires it; their **reference readouts still stay muted**, and vertical mixer editable numbers are no longer yellow at Base. Dedicated Action Costs, Medical Healing, Special Ability, Human/Mutant group, Anomaly Damage, Mutant Drops and World Loot horizontal rows use the same changed-number emphasis rule.

This is a visual presentation change, not a global rescale: preserve sliders' native min/max, source/CFG stepping and mapping, actual Base reference values, value-based cascades, right-click/R reset, selected-parameter rationale, preset round trips and writer output. Do not substitute a universal 1x or mask Base values in internal calculations. Local publish and Wine inspection are required to verify text widths and min-width layouts; do not claim they passed without running them.

### 2026-10-09 — One universal hover glow across every interactive control

**User-approved global visual change; supersedes all earlier per-family hover halo sizes, opacities, double-rings, whole-card hover backgrounds and selection-only glow.** All hand-drawn editor handles use the single helper `Controls/ControlHoverGlow.cs` with **halo radius 15 DIP / opacity 0.17** and **outline ring radius 11 DIP / opacity 0.67 / 1.6-DIP stroke**, drawn using the existing gold AccentBrush. Hover state alone enables it. Selected/focused/modified-but-not-hovered controls do not manufacture this glow.

- Shared aggregate renderers determine the current editable handle position from the actual source mapping and show the halo **only on the hovered active hit target**. Combined graph rings reuse the same helper at their actual drawn point. Toggles receive the same indicator-sized halo, not a glowing entire panel. Hovering a Base/Vanilla reference tick, selection-only area, static preview or noninteractive legend does not glow or edit values.
- Ordinary WPF controls use `UniversalHoverGlowEffect` in `App.xaml`, matching gold, radius 15 and opacity 0.67 with zero shadow depth. The same resource drives Button/PrimaryButton/window buttons, preset ComboBoxes and their options, TabItem, TextBox, CheckBox switch **thumb**, native Slider **thumb** and wheel-editable value box. On slider mouseover the track, label and current numeric text stay neutral; only the thumb gets hover effect. The ordinary parameter-card background must not change because a mouse is merely over the card.
- Disabled controls remain non-editable and do not receive active handle halos. Preserve focus styling, keyboard/touch/mouse behavior, native snapping, preset reset, CFG and value-reference calculations. Handle-only hover is strictly visual; no new gameplay parameter or hit surface.

The canonical drawing parameters are deliberately fixed in one class and the WPF equivalent is a single named effect resource. Do not introduce another custom blur, radius, selector-opacity or full-card hover wash in later UI work without an explicit user-approved change. Local publish + Wine acceptance is still required.

### Hunger Survival Profile stage captions (2026-10-09)

Fixed the Survival > Hunger State Ladder lower chart footer after a Wine screenshot showed that Medium/Severe stage descriptions were cut off and crowding the Hunger Points X-axis. LOW / MEDIUM / SEVERE now each have a fixed three-line-safe stage cell under the axis: stage name, `AT N PTS`, and (only for Medium/Severe) a concise `-N% RECOVERY`. A shared 344-DIP height constant reserves sufficient room both in rendering and WPF measure; no repeated dragging instruction is shown. Source-linked yellow numerics, axes, stage handles, baselines and gameplay settings are unchanged. Publish under Wine and inspect at minimum and normal window widths before accepting the result.

### 2026-10-09 — Universal minimal checkbox design (explicit approval; latest rule)

**Supersedes the earlier WPF pill switch, per-panel checkbox shapes, ON/OFF status-lane pills and the 2026-10-09 global hover-halo requirement specifically for boolean checkboxes.** Across all tabs, every ordinary boolean input renders as the same compact **20 × 20 DIP square**, 3-DIP corner radius, 1-DIP neutral border and dark `PanelAltBrush` face. The checked state is communicated **only by the gold checkmark** (two rounded 2.2-DIP strokes), not by a yellow card, yellow text, yellow track, extra ON/OFF caption or a glowing outline. The unchecked state has an empty square. Static control names remain neutral and adjacent to the box; long names fit in the remaining width without overlapping it.

**Hover policy for boolean controls only:** a slightly lighter neutral border on the square itself (`RadarMutedTextBrush`), no drop shadow, no halo, no animation, no changed font or card background. No ON/OFF label highlighting. Keyboard focus and checked state remain distinct from hover; disabled checkboxes are not editable. The broader universal `ControlHoverGlow` continues to govern sliders and graph handles; **checkboxes are deliberately exempt**.

**Single implementation contract:** WPF's global `CheckBox` template in `App.xaml` and the drawn `BooleanCheckboxChrome.Draw` in `Controls/BooleanCheckboxChrome.cs` share exactly the same size, corner radius, border, stroke, color and hover behavior. `AggregateVisualControl.DrawTogglePill` uses this chrome for all toggle-led panels (World Loot Layout, bundle toggles, healing/radiation/fatigue, starvation, equipment mechanics); `MedicalHealingRadar` uses it for Food Healing. No old slider-style toggle halo overlay may activate for `HitKind.Toggle`. Regular WPF parameter checkbox rows keep one label at left and the same square at right, without repeating ON/OFF.

**Nonvisual behavior unchanged:** each boolean retains its real `ParameterViewModel.CurrentBoolean`, enabled state, existing clickable row/target, click/Space/keyboard selection, right-click reference reset, visual-family R reset, preset persistence, source catalog and CFG writing. The Food Healing card remains clickable in its existing hit area, but only hovering the 20-DIP checkbox brightens its border. No changes to sliders, graphs, native step limits or gameplay values. Inspect the WPF template and draw-based panels under Wine; source/UI geometry checks are not a substitute for a runtime build.

### Health typography readability (2026-10-09)

Core Health and Medical Healing now use one shared, readable font hierarchy without changing their layouts or gameplay. Panel titles remain 12.5 DIP; control names and the Medkit Healing header use 11.4 DIP, current HP / recovery numbers use 12.8 DIP (14.0 DIP only when modified), and graph ticks plus slider endpoints use 10.4 DIP. Core Health uses an opt-in font treatment with measured text width, so shared sliders elsewhere are not restyled. The Food Healing label, all medkit captions and the previously tiny 8.4-DIP medical chart Y-axis labels are included. All existing sources, units, native bounds/steps, references, checkbox/slider interactions and CFG output remain the same. The change requires a local publish/Wine visual check.

### 2026-10-09 — Combat tab gamer-centric redesign (explicitly approved; authoritative)

The user approved the four-part Combat wireframe and specifically requested source+docs deployment on `main`. **This is the latest authoritative Combat UI design and supersedes conflicting 2026-10-08/09 vertical cooldown-fader, Human Combat side-fader, and oversize Mutant Health min-height notes.** Keep four separate **full-width (1×1)** sections in existing order, with only one in-card centered gold heading per section:

1. **YOUR WEAPONS** (`PlayerWeaponProfile`): the same three independent horizontal cards in a single row — WEAPON DAMAGE, HEADSHOT DAMAGE, JAM FREQUENCY. Reuse Stamina-style label-left / current-right / rail-below. Slightly increase only these labels to **11.2 DIP**, current numerals to **12.0 DIP** (13.2 when source-modified), and native endpoints to 10.0 DIP. Preserve all three genuine source parameters, **0.25x catalog increments** and 169-DIP board. No DPS simulation or new damage readout.
2. **HUMAN ENEMIES** (`HumanCombatProfile`): keep the square **180–280 DIP 2D graph** with X=enemy health and Y=enemy gun damage, independent gold exterior drag handles and an interactive central joint. **Replace the three right-hand vertical faders** with three **90-DIP horizontal cards stacked vertically**, ATTACK FREQUENCY, FIRE ACCURACY, EXPLOSION DAMAGE, with current values right-aligned and native min/max below. Fixed graph start (64,91), cards start X=plot.Right+16, Y=91/192/293, equal measured right-lane width; unchanged 465-DIP frame. The frame no longer renders the redundant bottom drag/axis legend: the axes and joint tooltip already explain interaction. Graph axis current values are neutral at source reference and gold/+1.2 only if modified. Attack Frequency has the verified 2x maximum and Fire Accuracy 1x maximum; native CFG steps and the explosion source remain unchanged.
3. **MUTANT COMBAT**: a compact standalone `combat.mutantDamage` slider using the standard Stamina-style label / current value / rail layout. Keep source-accurate endpoints and subdued reference ticks; no aggregate board or duplicate instructions.
4. **MUTANT HEALTH** (`MutantHealthRadar`): preserve the approved two-column seven-row Stamina-style card layout, alphabetical order of **14** independently editable mutants, native **0.05x–2x** source range and **0.05x source step**. Keep species name on the left, editable multiplier **centered**, derived actual `HP` **right-aligned**, single rail below, grey reference tick and handle-only glow. Improve tiny numbers without moving handles: species name **10.8 DIP**, multiplier **11.2 DIP**, actual HP **10.5 DIP**, and numeric change highlighting still +1.2 DIP. WPF minimum is brought into alignment with the unchanged renderer's **526 DIP** measured height (instead of the stale 672-DIP blank-space minimum). The target values `VanillaHp × sourceMultiplier` remain unchanged; no fake boss/Amok control.

**Global rules:** all displayed current numerics are neutral if equal to the selected difficulty reference; on change, only the numeric token (and attached ×/x) turns gold and grows 1.2 DIP; static labels and native endpoints remain neutral. No full-card hover, no hover on Base reference, no repeated section title, no instruction footer, no checkbox changes. Preserve existing same-source hit targets, source range/bounds, catalog steps, reference reset, keyboard drag semantics and preset/CFG writer. Psy/Repeated-Hit family mappings remain available in the catalog but are not added as new top-level Combat cards. Do not change the gameplay parameters, source ranges or writer from this UI redesign.

**Acceptance after publish:** verify that all visible Combat families render once and their full labels/values remain readable at normal and minimum WPF sizes; hover is identical. A local Wine build and visual test is still required.

### CFG-first advanced visual controls — development rule (2026-10-09)

Future tab-by-tab gamer-centric redesigns may use native interactive 2D plots, threshold/step profiles, event timelines, proportional comparisons, source-linked range editors, meaningful radars, discrete selectors and simple sliders, **according to the actual CFG semantics**. Existing sliders are not mandatory when a more faithful visual representation makes tuning clearer. Every proposed advanced chart must have documented source mappings, accurate axes, native steps/bounds, reference behavior, keyboard/reset access and no invented runtime calculations. Keep redundant explanatory prose out of the UI, use the existing hover/checkbox/typography standards and change the locked visual catalogs only with approved designs. The user approves a per-tab wireframe before the source and documentation are committed together to `main`. This policy adds no controls or CFG mutations by itself.

### 2026-10-09 — Survival tab gamer-centric CFG-first redesign (approved; current)

**This is the authoritative current Survival UI contract.** It supersedes the older "read-only duration preview + five vertical duration faders", "smooth bleeding-damage curve", "long one-line fatigue captions", "three intoxication tiles", and overly small stage/axis typography. The user explicitly approved the seven-section Survival redesign based on the catalog's **43 Survival definitions**. All real editable source controls remain available: 7 Hunger, 10 Fatigue, 9 Consumable Effects, 2 Alcohol Strength, 4 Sleep, 3 Radiation, 7 Bleeding, with one additional audit/hidden drunkness-threshold scaler not made into a fictitious new editor. No change to source IDs, CFG write paths, native min/max/steps, reset/presets, select-difficulty reference numbers, gameplay calculations, or established constraint cascades.

1. **Hunger State Ladder** (`HungerLadder`): preserve the accepted synchronized 0–100 HungerPoints preview and threshold-linked *discrete* stamina-recovery loss profile. The three thresholds stay native editing sources and Medium/Severe loss remains true positive stored magnitude / negative UI effect. Keep live stage labels in fixed cells and avoid smoothing authored step changes. Improve the stage preview heading to **11.0 DIP**. No fabricated elapsed-time-to-hunger simulation.
2. **Fatigue Response** (`FatigueCurve`): keep the two independent, real-Y-scale gold AIM TIME and pale RELOAD TIME stepped profiles with shared real 0–100 SleepinessPoints thresholds; LOW has no reload source. Preserve the separate white Y-only reload handles. Widen the footer vertically from one clipped sentence to one fixed **four-line-capable** stage cell in each third: LOW/MEDIUM/SEVERE label; `AT N PTS`; `AIM +N%`; where authored, `RELOAD +N%`. No forced LOW reload. The actual frame and `CompactFatigueHeight` must both use **365 DIP** instead of 320; lines at frame Y+262 / +282 / +304 / +326 and cells Y+260 height93, safely below X-axis ticks. Use 11-DIP stage preview headings and 10.2-DIP real axis captions. Golden modified numbers only when the corresponding source differs from the difficulty reference.
3. **Consumable Effects** (`ConsumableMixer`): retain four independent 0–2x top strength sliders with original native 0.05 steps, and three concise *read-only* effect readouts for Water/Energy/Limited Energy instant stamina, Water/Energy sustained buffs and Beer/Vodka radiation removal. The effect readouts now place **one effect per line** rather than clipping pipe-separated sentences; up to three 9.8-DIP effect lines under a 10.4-DIP title, highlighting/enlarging only changed numeric tokens by +1.2 DIP. No extra duplicate readout headings.
   
   **Replace all five vertical duration faders and the read-only longest-effect preview with one directly editable common-seconds timeline.** Five horizontally independent native source lanes are labeled WATER, ENERGY, VODKA, BEER and MAGIC VODKA. Its common **0–270 real-second** ruler shows 0/90/180/270 seconds. Proven longest authored duration prototypes are **30 / 45 / 20 / 20 / 90 seconds**, respectively, scaled by the same original native **0–3x multiplier** and **0.05x step**. Each lane renders an interactive gold handle at `SecondsX(authoredSeconds × currentMultiplier)`, a thin neutral selected-reference tick, a subdued uneditable tail *beyond that lane's own native maximum*, and a right-aligned exact current longest duration in seconds. WATER's shorter 5-second effect and ENERGY's shorter 5/15-second effects appear only as passive minor ticks at their scaled times, **not as independent controls**. Do not imply all five families can last 270 seconds. Native hit mapping uses the lane's physically shorter `0..authoredSeconds*3` track, rather than stretching the source multiplier across the entire 270s axis; any grey tail cannot receive hover/edit. `AddHorizontalTarget` retains source-native snapping, track selection, focused arrows and right-click/R reset. There must be **exactly nine editable sources** (four strengths + five durations), not duplicated read-only and interactive duration handles. Remove unused `DrawConsumableFader` and `ConsumableFaderColumns`, and measure actual new panel bottom as `previewTop + 181 + 14` rather than retaining the stale 179-DIP fader bank.
4. **Alcohol Tolerance / Drunkenness** (`DrunkennessControl`): keep the two native Beer/Vodka strength sliders and their deterministic already-audited number-of-drinks stage previews. The progression captions use legible ~10-DIP type. Four *read-only* fixed stage tiles now represent **SOBER (below 0.001), TIPSY (0.001+), DRUNK (30+) and WASTED (80+)**, with two separate lines per tile at minimum width and no fake thresholds. The existing hidden `survival.drunknessThresholdScale` stays hidden and OverDrunkness is not folded into the graph. Static stage boundaries and labels are muted, the source value and derived WASTED-after-N count are gold and +1.2 DIP **only if the corresponding Beer/Vodka source is modified**, and the old yellow VANILLA baseline caption disappears; its muted source-reference tick remains. All native source steps, mouse, keyboard, reset and saved preset semantics are unchanged.
5. **Sleeping System** (`SleepingDashboard`): preserve two distinct native access controls (required fatigue points / minimum hours) and two distinct true wake-up effects (HP / stamina effect), with no invented overnight recovery timeline or simulated sleep mechanics. Increase the RECOVERY WHEN YOU WAKE UP section heading to **11.4 DIP**; current source-value cards, reference markers and original editable mappings stay unchanged.
6. **Radiation System** (`RadiationBalance`): intentionally preserve **three independent editable sliders** for difficulty-scoped exposure multiplier, absolute passive points-per-second radiation clearance and harmful-radiation health-damage scalar. A combined exposure-minus-clearance graph or predicted HP/s curve would mix incompatible native units and pretend unverified runtime math. No composite "radiation balance" scalar is added. Existing native source/units, selection and resets remain.
7. **Bleeding** (`BleedingCurveControl`): retain two independent top sources, Bleeding Effect Strength and Natural Recovery, and all five lower native editable stage damage/threshold sources. Render the *current and dashed reference damage path as true LOW→MEDIUM→SEVERE **horizontal steps with vertical transitions*** rather than lines interpolating between discrete authored states. The accent fill follows the same stepped polygon to the chart's right edge. The existing native damage handles and separate Medium/Severe threshold handles stay at the true authored points, including ordered thresholds. Raise chart and native axis typography to ~10–11.4 DIP, and enlarge each independent stage caption zone to **44 DIP** with `STAGE AT N PTS` and `N HP/TICK` as distinct rows; no longer clip 35-DIP footer cells. The 458-DIP frame and existing pointer/CFG mappings remain unchanged.

**Shared invariants:** current values remain neutral at the selected vanilla difficulty reference; yellow and +1.2-DIP size apply only to source-modified numeric tokens; thin reference marks never receive highlight; labels do not overlap or turn gold solely on hover; all handle halos use `ControlHoverGlow` and boolean editors remain universal minimal checkboxes. Do not add redundant on-panel instructions or unrelated simulated gameplay. This contract applies only to Survival and does **not** unlock Combat or any other established tab. Full compile and Wine appearance checks remain the local acceptance step; source-level verification is not equivalent to having run them.

## Health — gamer-centric visual editors (2026-10-09)

Implemented the approved Health tab redesign across all 16 existing Health source controls. Core Health presents an interactive 25–200 HP Max Health capacity ruler plus four independent recovery editors. Medical Healing retains Bandage Healing and the minimal Food checkbox, while the three directly draggable medkit bars share a real HP comparison axis (their native maxima remain 140/170/200). Bleeding Treatment uses four directly draggable bleeding-point bars with real maximums 200/30/70/50. Radiation Treatment now has two horizontally draggable lanes on a shared 0–200 removal-point scale, capped for each item at Scientific Medkit 120 and Anti-Rad 200. Native steps, difficulty references, gold changed-only numeric highlighting, universal handle-hover, right-click/R reset, preset behavior and the signed treatment CFG writer remain intact.

The source/UI documents now lock Health section heights 348/400/376/250 DIP and clarify that none of these charts invent an HP-over-time simulation or a common source maximum. Build and Wine visual/interaction QA are pending.


### NPC Ammo Preview polish (2026-10-09)

The current Loot > NPC Loot & Equipment section is **463 DIP** tall. Under the real generic `NPC AMMO` source slider, its three source-grounded Newbie count examples are now separate compact read-only cards (`12 GA`, `5.45`, `.45 ACP`). Each label stays neutral while the complete `MIN–MAX` count range (including the en dash) becomes **gold and +1.2 DIP** only when the NPC Ammo multiplier differs from the selected Base Preset. It uses integer-half-up scaling from the audited `NPC_Ammo_Smart` counts. With `Water` or `None` selected under Extra Corpse Loot, the preview is subdued and shows an inactive hint; a separately customized ammo multiplier remains saved and its preview numbers remain gold. The improved preview has **no interactive hit surface**; the Dropped Weapon Condition two-handle native editor remains fully interactive below it. The independent Breakable Box Ammo source/writer/graphs are untouched. Build/Wine screenshot and game drop tests pending.

### Dropped Weapon Condition visual polish — 2026-10-09

The Loot > NPC Loot & Equipment section now measures **496 DIP**. Its dropped-weapon MIN/MAX range readouts are **centered as a balanced pair** above one horizontal track (instead of crowded into the right corner); numeric endpoints independently become gold, including `%`, only when changed from 25/50% baseline and grow +1.2 DIP. The old conspicuous dashed source reference is replaced by a quiet solid grey 25–50% band and tiny reference ticks. Both equal circular handles sit directly on the bar except when overlapping, where ±7 DIP temporary staggering preserves independent access. The 38-DIP clickable rail, native 1% increments, nearest-endpoint X/2D selection, guaranteed active drag/hover capture, keyboard navigation and both reset methods remain intact. Generic NPC Ammo/Medical, source-bound ammo samples and all Breakable Box controls are untouched. Source verified/compiled runtime acceptance: **local WPF publish and Wine interaction QA pending**.


### Breakable Box Ammo Yield live quantities (2026-10-09)

Loot > Breakable Boxes now measures **680 DIP** and contains a compact read-only **Ammo Yield Preview** below its two native Yield controls. Four separately labeled SNG stack count ranges at 1x: **9×18 3–6**, **9×19 5–10**, **5.45 5–10**, **12 Gauge 2–4**. NATO: **.45 ACP 5–10**, **9×19 5–10**, **5.56 5–10**, **12 Gauge 5–10**. These are eight real `DestructibleStash_AmmoSNG/NATO` source item rows, not samples from generic NPC SmartLoot. Every number updates immediately with **Ammo Yield 0.5–2x**, step **0.25x**, and the writer's positive integer half-up formula. These are *rounds if that caliber is rolled*, not guaranteed per box.

Three gameplay concepts remain independently displayed: Ammo Yield changes round counts; four Ammo Family Weights change their relative chance and appear in the already color-keyed SNG/NATO composition graphs; Grenade Yield changes only grenade box counts. Every caliber name stays neutral, and only the entire computed **MIN–MAX** numerical token becomes gold and +1.2 DIP when the Ammo Yield source differs from selected Base. Any 0x-weight caliber preview remains visible but subdued. No new CFG source or separate interactive hit target was introduced; the same six Loot edit sources, writer and reset semantics remain. Native mappings are recorded in `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md`, `docs/UI-MIXER-GRID-SPEC.md`, and `docs/generators/DESTRUCTIBLE-LOOT-AUDIT.md`. Local Windows publish/Wine and in-game behavior still require checking.

### Master mechanics full-list writer regression

Five additional executable checks confirm that a **real** Master mechanics parent toggle edits the full `AdditionalMechanicsEffects` array, while a child-only Health change leaves it untouched. The CI Health/Stamina writer suite therefore contains **46 scenarios**, independently of the **69 original source CFG values** and **five deliberately non-VM Stamina internal states**.
