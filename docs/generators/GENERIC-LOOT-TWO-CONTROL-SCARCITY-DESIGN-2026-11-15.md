# Generic loot reduction — two-control architecture proposal (S.T.A.L.K.E.R. 2.1)

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
