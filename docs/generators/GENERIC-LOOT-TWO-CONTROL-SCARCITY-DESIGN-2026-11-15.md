## CURRENT 2026-11-18 — Factory preset usage and the danger of stacked scarcity

Factory presets now apply small but nonzero native `loot.genericCorpseReduction` (5–20%) and `loot.genericStashReduction` (10–15%) alongside the previous 50–81% ordinary loose-world-loot removal and per-item/per-category chance cuts. These controls affect distinct **guarded generic generator endpoints**; they are not a guarantee that a certain percentage of all real game items disappears. The combined difficulty cannot be estimated by adding the percentages. Source-native optional NPC bonus packages are empty in all twelve current factory presets, so changing only `loot.npcSmartLootEnabled` would not control the remaining direct NPC consumable chance; the latter is already independently tuned. Disabled stash Smart Loot and disabled extra gear stay untouched. Exact per-profile changes in `tools/factory_preset_scarcity_refine_2026_11_18.json`; in-game acceptance needs fresh NPC corpses and newly generated stashes.

# Generic loot reduction — two-control architecture proposal (S.T.A.L.K.E.R. 2.1)

## 2026-11-16 — CURRENT: two native generic supply-reduction controls

Two independent **source-backed** controls are now hosted inside the existing WPF families, without creating another full-width matrix. `loot.genericCorpseReduction` (NPC LOOT & EQUIPMENT) and `loot.genericStashReduction` (STASH LOOT MIX) both range from **0% to 100% in 5% steps**, with **0% original CFG/no edits** as the fixed default. Their main UI label is **SUPPLY REDUCTION**. Each source is part of the existing aggregate reset/selection family, and its real value is serialized in presets; old 359-value presets load with both new sources at their 0% defaults. Original item priority remains on the established `Weight` mixer. No Echo detector or weapon-attached ammo controls have been added.

**NPC board:** The four 88-DIP SmartLoot / Ammo Qty / Standard Med / Mod Items cardlets remain a single centered row at host width >=714 DIP or two pairs at narrower widths; the new reduction slider is an independent compact **328-DIP max-width, 74-DIP cardlet below those peers** rather than adding a fifth unreadably thin column. Bonus Ammo example/condition/per-item Chance bands move down by **82 DIP**. Measured family heights become **640 DIP** at >=714 and **738 DIP** below 714 (using the existing common preview/consumable top functions). The new master reduces 104 normal-human Food/Drinks/Medical CFG `Chance` rows in six cloned pools/33 approved redirects **after** checked absolute Chance and STANDARD MED, and scales the original 39 optional corpse SmartLoot spawn endpoint pairs. At 100%, it selects the existing native **None** bonus state in emitted CFG without overwriting the saved package choice. Carried weapons, attached ammo, armor, quest and named NPCs are unchanged.

**Stashes board:** At >=700 DIP host width, the new source control and existing **AMMO QUANTITY · ALL TIERS** are **two compact 56-DIP peer tracks side-by-side** above the original Weight mixer. At narrower widths they stack and all lower sections move by 64 DIP; measured panel heights **785 / 1159 DIP**. The master applies a single last-stage multiplier to eligible 22 ItemGenerator item Chance groups, reduces `MinCount/MaxCount` for the 19 Weight-only groups (positive one-item rolls cannot be halved, and selection ratios do not change), and scales native `MinSpawnChance`/`MaxSpawnChance` of all approved, private world SmartLoot clones. Existing category Weight ratios, spawned item SID lists, difficulty/rank gates, coupon source rules, protected actor guards, DLC1 allowlists, and separately opted-in Extra Stash Gear and mod items are unchanged. At 100%, source 0 endpoints, zero item selection and zero count are emitted; **live engine zero-endpoint/fallback behavior is not yet verified**. A slider percentage is NOT a promise that the same percentage of physical corpses or stashes will be empty.

**Code and tests:** `CfgPatchWriterService.GenericLootReduction.cs`, the existing NPC/three-tier/World SmartLoot writer partials, shared `StashEffectiveState`, `parameters.json` (361 params), baseline (361 supported), `MainViewModel`, WPF `AggregateVisualControl`, and `tests/LootSpecialGeneratorsContract/GenericSupplyReduction.cs` must change together. The tests exercise 0/50/100 baseline semantics and native group field identity. Static source/CFG acceptance is distinct from successful Wine display and game-runtime loot verification.

---


## 2026-11-15 — NATIVE SOURCE GATE VALIDATION (latest)

The exact reviewed 2.1 snapshots now establish a hard difference between native group types:

- Ordinary Basic/Standard/Rare `ItemGenerator`: **41** groups: **19 Weight**, **22 Chance**; **no authored group activation Chance/SpawnChance or ItemSetCount on any of the 41**. The 19 Weight-only groups contain **86 weighted item entries**, all with positive MinCount/MaxCount. Uniform Weight scaling leaves group selection ratios intact and cannot honestly guarantee scarcity/empty output.
- World `StashPrototypes` SmartLoot: **422** groups: **410 Weight**, **12 Chance**, **all with native MinSpawnChance and MaxSpawnChance**. **310** have positive MaxSpawnChance; **112** have source MaxSpawnChance=0 (110 Weight groups, two Chance groups), which must stay zero. Only **30** groups author ItemSetCount; 392 do not. Positive source endpoints give a plausible separate group-scarcity gate but game-level execution is **not yet confirmed**.
- Generic NPC consumable pools: **104 item Chance entries**, **zero item Weight entries**, reachable via **33** exact normal-human redirects. Those 33 parent links have source `Weight=1` but no independent native bundle activation Chance. Source-native item Chance reduction is feasible; optional NPC SmartLoot packages still require separate suppression acceptance.

Thus the two proposed *full-domain* scarcity masters must **not** ship by just scaling native Weight. Retain Weight as the existing configurable **distribution** mechanism. A true whole-group roll must be based on an actual supported engine gate rather than inferred from native selection weights. Next candidate: group spawn endpoints on World SmartLoot plus per-item Chance rows; unresolved areas are all 19 regular ItemGenerator Weight-only groups and optional NPC corpse bonus packages. Do not silently hide those gaps under a single label.

New pinned [463-group source gate inventory](generic-loot-weight-group-gates-2.1.csv), [read-only audited policy](GENERIC-LOOT-WEIGHT-GROUP-GATING-AUDIT-2.1-2026-11-15.md), and [regression checker](../../tools/audit_generic_loot_scarcity_gates_21.py) are the gate for implementation. Static evidence is not a game RNG proof; source-only approval does not authorize an experimental group-empty SID or unconditional 0/0 fallback behavior. No UI/CFG writer/preset semantics were changed during this check.

---


## Latest design refinement — REUSE NATIVE WEIGHT DISTRIBUTION (2026-11-15)

**Status: approved direction / architecture only.** The request is for a configurable **native Weight distribution** like the TCD project already has; do not create a new, large independent mixer or invent a new player-facing `Weight` interpretation. The two independent reduction masters below remain proposed; **distribution changes what an eligible group selects**, reduction changes **how often optional supplies are generated**. Neither is the other.

### Existing, actual project primitives

- `StashEffectiveState.TierFactor`, `ItemBiasId`, `TuneSelection`, `SmartSelectionFactor` and the production ItemGenerator/SmartLoot writers already implement source-typed, relative `Weight` multipliers. The native preview calls the same state calculator as the writer.
- `AggregateVisualControl.StashBranch.cs` already draws native Basic `Weight` composition and a normalized source-only preview. Its current `BasicVisibleWeightKinds = {1,2,3}` maps to **MEDICAL / FOOD / DRINKS**. Other persisted `stashMix*` parameter IDs exist, including hidden legacy sources; do not add more visible knobs merely because the IDs exist. Other source branches (for instance a coupon-bearing Basic rank) can have different native category membership; a preview of one branch must not be labeled a whole-world distribution.
- The **Breakable Box Ammo Distribution** four-axis radar is the other good precedent: each axis modifies only source items already present in its original pool, and the resulting per-item relative selection shares are computed **separately for each original pool**.

### Exact behavior

For each eligible native `Weight` group `g` containing authored items `i`:

```text
effectiveWeight(g,i) = originalWeight(g,i) × existingCategoryMultiplier(i)
                       × any permitted group-local multiplier(i)
relativeShare(g,i) = effectiveWeight(g,i)
                     / sum(effectiveWeight(g,j) for eligible j in g)
```

No item SID is added, no rank/difficulty gate is flattened, no native `Chance` is converted to `Weight`, and no direct NPC weapon/equipment generator is adjusted. Use a fixed original-pool comparison marker, matching existing TCD visual grammars. Each group has its own valid, nonzero denominator or an explicit native-safe all-zero guard; never silently normalize over unrelated physical generators, mixed `Chance` groups or different ranks.

**Audited example:** Basic Veteran consumable `Weight` group has Medical Bandage `2`, Food Bread/Sausage/CannedFood `5+5+3`, Drinks Milk `1`. Source total `16`. Native relative mix is Medical `12.5%`, Food `81.25%`, Drinks `6.25%`. Halving **Food only** makes weights `2 / 6.5 / 1`, total `9.5`, giving Medical `21.05%`, Food `68.42%`, Drinks `10.53%`. Halving **all three categories** makes `1 / 6.5 / 0.5`, total `8`, and **leaves the original relative shares unchanged**. Thus **one master scarcity slider must never be implemented as uniform Weight scaling**.

### Compact UI composition

The product goal is **as few new controls as possible**:
- Keep **CORPSE LOOT REDUCTION** and **STASH LOOT REDUCTION** as the only two *new proposed* high-level controls, one per existing family. Both default to zero reduction and must be safe no-ops for legacy presets.
- **Reuse**, rather than duplicate, the existing stash category mixer and its read-only normalized bar for native `Weight` groups. At wide resolution, retain compact row geometry and conditional source details; do not show a 463-group editor.
- For NPC supplies the verified generic-human consumable surface has **104 source `Chance` rows, not a common native Weight pool**. Its existing Food/Drinks/Medical SID-specific Chance editors remain the truthful way to configure those categories. Do not fabricate a shared 3-way NPC `Weight` distribution or turn 3 independent rolls into a `100%` composition bar.
- For optional NPC `CorpseSmartLoot` and world stash SmartLoot, only draw weighted composition where its **specific native ItemSet/Rank/Group** has authored `Weight`; retain source-native `Chance`, group spawn bounds, caliber and count separately.
- No new Echo detector Chance, weapon-attached ammo control, generic NPC grenade percentage, quest actor control, or separate distribution knobs per NPC class/stash tier.

### Actual scarcity still needs a distinct, source-supported gate

`Weight` has no intrinsic empty-result probability: `20:10:10` and `10:5:5` have identical relative selection **50%:25%:25%** when the group executes. A new **EMPTY/NO LOOT** axis would reduce actual output only if the game's exact generator schema supports a verified empty/no-pick candidate at that context; **none is approved by the present 2.1 audit**. Do **not** add an invented empty SID, an invalid `Weight` row, or an untested fake `None` candidate.

When a group has a native `Chance` gate, apply the scarcity factor `q = 1 - reduction` after existing detailed edits; its internal `Weight` distribution stays relative to the same original pool. For groups with no native optional activation gate, keep reduction unsupported **until** a local source-backed method is proven (including SmartLoot 0/0 endpoint and all-zero fallback acceptance). Preserve existing independent native `Chance` controls and source quantity bounds. A 100% master must not claim total optional-loot suppression while any eligible weighted group still produces items.

### Acceptance before implementation

1. Assert native Basic mixed-pool examples against the same `StashEffectiveState` logic that emits CFG: `2:13:1` => `12.5:81.25:6.25%`; Food 0.5 => `21.05:68.42:10.53%`; all three at 0.5 => same original mix.
2. Assert each `Weight` group is normalized **only over its native members**. Verify ammo-only weight groups do not become a fictitious general ammo abundance control.
3. Assert mixed `Chance` + `Weight`, zero-weight groups, Source `Chance=0`, special original `MaxSpawnChance=0`, selected NPC bonus packages and protected consumers each keep their original semantics.
4. Verify NPC 104 `Chance` entries, original 33 redirects, current 10 independent item toggles, optional bonus packages and stash Basic/Standard/Rare/13 world SmartLoot allowlists before any new publisher code.
5. Confirm reset/preset roundtrip, compact graphics, generated CFG and game-run acceptance on fresh ordinary humans and unopened generic stashes. No new gameplay claim from a composition preview alone.

**Relationship to previous design below:** The earlier two-master scarcity proposal remains a target, but its implementation **must reuse the proven native Weight mix for distribution** and must not assume that scaling relative weights uniformly lowers output. Implementation remains pending.

---


**Decision record — 2026-11-15. DESIGN ONLY; not an implemented UI, parameter, writer or build.**

## Goal and hard exclusions

Control the everyday loot economy through **two player-facing master sliders**, independently scoped to (1) repeatable ordinary human NPC corpses and (2) ordinary world stashes. The settings must describe the same kind of change even though the vanilla CFG uses different mechanisms. Avoid dozens of per-generator toggles, never invent universal physical-body/stash drop probabilities, and do not disturb existing detailed controls or preset persistence.

**Explicit exclusions from user:** NO weapon-attached ammunition quantity control or modifications; NO Echo detector Chance control. Preserve NPC equipped/drop weapon identity, weapon condition control, faction armor/loadout selection, NPC combat equipment, and all gameplay-only equipment.

**Protected:** quest/story/test/prologue, named NPC and scripted DeadBody overrides, zombie, mutant and trader generators, stash clues, blueprint/unique, DLC1 not allowlisted, destructible crates (separate category), and unrecognized mods/PAKs. The word `generic` refers to **approved consumer edges**, not a name-prefix match.

## Exactly two master UI controls

1. **CORPSE LOOT REDUCTION** — 0–100%, default **0%**, compact 5% steps. Applies to the **optional supplies** appearing on ordinary human corpses. That means all 104 source `Chance` rows across six NPC role consumable pools/33 approved human source redirects (medical 58; food 26; drinks/stimulants 20) and, once separately validated, all **three selected optional Corpse SmartLoot packages** (`NPC_Medicine_Smart`, `NPC_Ammo_Smart`, `NPC_Water_Smart`) on the selected difficulty. Never alter the 121 weapon entries or weapon-attached ammo. The option selector still selects the same original package SID list: the master factor must modify effective bonus opportunity, not silently replace the saved package selection.
2. **STASH LOOT REDUCTION** — 0–100%, default **0%**, compact 5% steps. Applies to **ordinary** Basic/Standard/Rare stash ItemGenerator parent clones and rank wrappers AND eligible per-actor World Stash SmartLoot prototype clones, including native medicine, ammo, food, drinks, grenades and coupon entries where genuinely present. Preserve item eligibility, rank/difficulty gates, protected actors, custom mission/direct rewards and independent Extra Stash Gear.

These are **relative reduction controls**, not an absolute source `Chance` 0–100% editor and not a promise that exactly X% of physical corpses/stashes are empty. At **0%**, both are pure no-op: no new TCD source clones/redirects based only on these controls. At **50%**, halve a source `Chance` row after all existing detailed overrides. At **100%**, every approved *optional supply* producer **must** be suppressed, but no build may claim this endpoint until all underlying source paths, including Weight-only groups and SmartLoot fallback, have tests proving it. Equipment and quest items continue to exist at 100% reduction.

Default user-facing explanation: **"Reduce optional supplies on ordinary NPCs / unprotected world stashes. 0% keeps the original generator behavior. Equipment and quest loot are unaffected."**

## One semantic contract; native-field-specific implementation

Let `r = reductionPercent / 100`, `q = 1 - r`, `p_detailed` be the already-composed per-entry source `Chance` from TCD. Then:

```text
Chance items:       p_effective = clamp(p_detailed × q, 0, 1)
Weight-only groups: retain relative item Weight ratios;
                    gate whole optional group/pool with q (if source-safe)
                    DO NOT multiply every Weight by q: normalization cancels
Source quantities:  do not pretend integer Min/MaxCount is a probability;
                    leave them unchanged unless a tested zero-off guard requires it
SmartLoot groups:   apply source-grounded group roll suppression in a scoped
                    clone; preserve item Weight/Chance and rank/set/caliber
                    when retained; use source-safe zero-count/zero-set guard
                    only if tested against engine fallback
```

**Not yet verified:** generic, safe *partial* gate for arbitrary Weight-only ItemGenerator category groups or all three optional NPC Corpse SmartLoot packages; exact engine behavior of World SmartLoot zero endpoints and `ItemSetCount=0`. Do not implement a visually convincing slider that affects only Chance rows while silently leaving weighted supplies untouched. Do not simply scale all Weight entries in a group. Verify native group wrapper semantics and gameplay fallback, or limit a future shipped scope/label to precisely proven mechanisms and explain the gap.

### Composition and precedence

The saved detailed controls retain their original meanings. Derive each ordinary NPC item-specific source Chance first:

```text
NPC row = checked absolute SID Chance
       OR unchecked vanilla Chance × existing STANDARD MED (medical only)
       OR legacy checked source-relative modifier
final NPC row = NPC row × q_corpse
```

For stashes, run the existing `StashEffectiveState` once, preserving tier-specific native Chance modifiers, six-category Weight mixer, medical composition and quantities. Apply `q_stash` to the **already computed final Chance**; gate whole eligible Weight-only groups separately (do not scale their relative Weight); apply the same source-scoped reduction to parallel physical World SmartLoot so neither layer escapes the master. No second writer may overwrite a Chance field in a later patch. Preserve original absence of a SID; never introduce a new item to fill a disabled one.

At 0%, existing detailed settings continue to behave exactly as before. At >0%, the masters act as final scarcity caps and **take precedence** over advanced per-item absolute-Chance controls: a checked source 100% still becomes 50% at master 50%. The precise presentation must show both the detailed raw/effective value and the master suppression so users do not see two contradictory numbers.

Optional Corpse SmartLoot package selector, BONUS AMMO QTY, weapon condition, Mod Items, Extra Stash Gear and stash type mix remain independently saved controls, not silently coupled by changing their saved values. **MOD ITEMS** keeps its own exact allowlist/hash-guarded removal percent, and is not affected by the two vanilla generic masters. The existing opt-in gear must remain independent so a 100% stash-supply reduction cannot remove intentional bonus gear or re-enable quest rewards.

## Structural scope and why one generic % writer is impossible today

| Source | Current verified boundary | Reduction mechanism decision |
| --- | --- | --- |
| NPC six consumable pools | **104 exact Chance rows / 33 private-clone redirects** | Can be composed after all existing per-SID Chance controls; 100% means Chance 0 on approved rows |
| Optional NPC Corpse SmartLoot | **Three named packages**, 13 medical + 43 ammo + 4 water item rows; 39 package group rows | Clone/gate group opportunity only after exact 2.1 source endpoint and engine checks; do not confuse the selected list with a Chance |
| Stash Basic/Standard/Rare ItemGenerator | **3 parent generators, 12 wrappers, 41 groups; 1,271 / 450 / 305 guarded tier manifest redirects** | Chance rows: per-entry final factor; Weight-only: *separate optional group gate* after proving safe native representation |
| Stash World SmartLoot | **13 prototypes, 422 groups, 991 item rows; 2,143 production consumer edges** | Actor-scoped proto clones, preserving rank/set fields, group spawn endpoints and item counts; test engine fallback, especially authored 0/0 MainLoot groups |
| Physical overrides | **99 DeadBody edges / 39 heterogeneous roots** | Excluded. They are not ordinary repeatable humans even if labels contain 'corpse' |
| Breakables and mod items | Separate allowlists and controls | Excluded from these two ordinary-loot masters |

The 2.1 baseline's shared `GamePass_Stash_ItemGenerator_Rare` also reaches the ordinary Neutral Sniper generator. Any global parent edit would contaminate the NPC/stash separation; only private clones and correctly guarded source edges are acceptable.

## UI constraints (not yet implemented)

Place one compact **CORPSE LOOT REDUCTION** peer in NPC Loot, one compact **STASH LOOT REDUCTION** peer in Stashes Content. Prefer one horizontal track per card, 0–100% reduction, tiny 0%/100% endpoints only if needed, native hover, R/right-click to zero reference, changed digits yellow only. Avoid adding a separate 14-control "Common Loot" panel, unnecessary rank selectors or full-width tracks. Keep source-specific existing editors on their existing advanced panels without duplicates; an eventual compactness review may reorganize them but must not delete persistence or silently hide functional controls.

Two new independent parameter/preset IDs are permissible **only after** the writer can satisfy both native paths. For migration, missing IDs mean 0% reduction, so existing presets preserve their prior output. When reduction=0% and no previous edits exist, new writer emits nothing and output stays byte-for-byte vanilla for these surfaces.

## Tests required BEFORE shipping slider behavior

1. Inventory guards: 33 NPC root redirects / 6 pool clones / 104 rows and selected-difficulty optional packages; Basic 12 / Standard 10 / Rare 19 groups, 13 SmartLoot prototypes and approved physical actors. Fail closed if 2.1 fixture changes or DLC not available.
2. Direct source Chance test at reduction 0/25/50/100%: `0.2 -> 0.2/0.15/0.1/0`, including an advanced checked 100% source Chance under the same final reduction factor.
3. Weighted group test: a 40:30:30 native selection ratio stays 40:30:30 **conditional on the group running**, while the whole optional group has source-supported reduction behavior. Regression test that mere `Weight *= q` is never used as the master suppression.
4. NPC selected bonus package test on Novice, Stalker, Veteran: preserve original MED+AMMO / WATER / NONE value; optional output reduction must not modify equipped or attached weapon ammo or standard NPC loadout.
5. World SmartLoot test: preserve original source 0/0 MainLoot endpoints, item count and group ItemSetCount semantics unless a tested explicit zero-off guard is needed; assert generated CFG is coherent, including disabled branch and all-zero weight fallback.
6. Isolation: unchanged named/quest/prologue/zombie/mutant/trader/physical corpse override sources, protected/clue-bearing world stashes, destructibles, standalone Mod Items, DLC1 missing/blocked actors, original shared Rare parent. Zero master leaves existing preset output unchanged.
7. Full generated CFG writer test + local Windows build/Wine compact UI, then newly generated ordinary NPC bodies and unopened unprotected world stashes in game (at least Vanilla, 50%, 100% profiles). Old corpses/previously initialized stashes cannot establish spawn-roll semantics.

**Status:** Design locked for next code pass, **not implemented**. Do not mark feature done or describe runtime acceptance until all required source classes pass their specific tests.
