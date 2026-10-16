**CURRENT 2026-10-16 — SOURCE-NATIVE PREVIEW OVERRIDE:** The six Content knobs now show actual Basic ItemGenerator source Weight ratios in two separately gated native pools, plus explicitly separate original Chance field examples. Equal-sixth multiplier normalization has been removed. These data come from the embedded CFG writer baseline, not synthetic category odds or game telemetry. Visual contract, 1×/0×/3× audit and caveats: [current UI lock](UI-DESIGN-LOCK.md).

**Current visual override (2026-10-16):** CONTENT MIX keeps six native knobs with a separate *read-only* live normalized multiplier preview (explicitly NOT stash drop odds); EXTRA STASH GEAR is now three tier cards with two source-backed 4-stop horizontal mode rails each. The prior A+C gear-knob descriptions are historical. All persistent IDs, writer mappings, source boundaries and reserved authored gear roster stay unchanged. See [current UI lock](UI-DESIGN-LOCK.md).

**CURRENT 2026-10-16 — A+C STASHES MIXER:** The six Content Mix multipliers now use compact source-backed 3×2 native knob tiles (2×3 at smaller widths), and Extra Stash Gear uses three Basic/Standard/Rare cards with two independent native knobs each. The old shared horizontal plots and dot matrix are archived. No source ID, writer mapping, probability meaning or 45 reserved authored loot entries changed. For geometry, per-family keyboard/reset and focus rules see [the locked A+C contract](UI-DESIGN-LOCK.md); for pending Wine visual acceptance see [release test guide](RELEASE-TESTING.md).

**CURRENT 2026-10-16 — 12 native direct medical entries covered:** Two ordinary Richter Cabin direct Medkits are already covered; the same existing medical controls now additionally tune ten canonically named Medkit/ArmyMedkit/EcoMedkit/Bandage rows in six other regular stash wrappers. Nine Hercules/PSYBlocker booster rows and lowercase `medkit` remain out of this medical source scope, and 45 weapon/gear rows are explicitly reserved for a later randomized-gear feature. [Exact 22-row consumable inventory](generators/REGULAR-STASH-DIRECT-CONSUMABLES-2026-10-16.md) · [reserved gear queue](generators/STASH-GEAR-UNIQUE-RANDOMIZATION-BACKLOG.md).

**Pending base stash direct-item extension (audit 2026-10-16):** Six more regular WorldMap physical stash actors have ten canonically-spelled direct MEDICAL rows outside their normal GamePass/SmartLoot sources, with Chance=1; 11 ammo, two grenades and nine booster rows are also catalogued. These are **inventory only, not claimed as currently controlled**. [Exact actor+category+item roster](generators/REGULAR-STASH-DIRECT-ADDITIONS-AUDIT-2026-10-16.md).

**CURRENT post-prologue Cabin MEDICAL coverage (2026-10-16):** The original 803 base regular SmartLoot actor count and the 112 DLC1 scope are unchanged. Two eligible Cabin WorldMap ItemContainers (BigSafe actor `1C2FD1C141D6DA11E518758CC1CD36AE`, SmallSafe actor `59FDC1CD4D0D094270C695A18D84373A`) each contain an additional *fixed 100% Medkit* outside ordinary Cheap and SmartLoot generation. The same MEDICAL, Medkit Bias and Medkit MIN/MAX controls now apply to these two exact direct `ItemGenerator.[1]` entries, using minimal source-native patches. No unrelated quest/blueprint/mixed reward, UI control, preset, or knob style changed. [Exact evidence and implementation](generators/CABIN-DIRECT-MEDKITS-2026-10-16.md).

**CURRENT base-game Stashes safety fix (2026-10-16):** The source-audited actor protection now applies to **all three Basic / Standard / Rare ItemGenerator production redirect writers**, not only the SmartLoot adapter. There are **106 protected WorldMap_WP ItemContainer actor SIDs** in the original CFG (77 with Blueprint/Unique/Quest ItemGenerator references, 34 named clues, 5 overlapping). Existing tier manifests reference **105 direct/indirect protected actor redirects** (Basic 2 / Standard 17 / Rare 11); those assignments are **filtered at output** while original source manifest evidence remains intact. For example, `098E76F14ADD5CFF9C62508DB31013E3` contains both `GamePass_Stash_ItemGenerator_Rare` and `Stash_Blueprint_APB_Upgrade_2`: neither its SmartLoot nor its ItemGeneratorSettings may be redirected. The previously established 803 base ordinary SmartLoot actor allowlist and 112 DLC1 ordinary actor allowlist are unchanged. No control, preset, source multiplier, or UI geometry changed. [Full protection regression and source roster](generators/BASE-STASH-PROTECTED-ACTORS-2026-10-16.md).

# STASH CONTENT & EXTRA STASH GEAR — implementation contract (2026-10-16)

**CURRENT 2026-10-16 — stricter quest/unique safety:** The supplied 831-row ordinary stash candidate CSV is a source inventory, not the full active patch list. In addition to the 71 explicit quest/unique/blueprint exclusions, 28 more candidate actors with named `ClueVariableSID` values are now withheld. Only **803** actors with `ClueVariableSID=EmptyInherited` are eligible for SmartLoot redirection. All existing Stashes controls, graph layout, source IDs and preset values remain unchanged; no vanilla SmartLoot root is globally edited.


**CURRENT 2026-10-16 — world SmartLoot coverage implemented:** The Stashes regular categories now operate across both the three audited ItemGenerator stash clones and 13 TCD reference-cloned SmartLoot world prototypes. The original source inventory includes 831 candidate WorldMap_WP ItemContainer actors, but the **current writer redirects only 803**: 28 candidates with named clue SIDs are withheld, in addition to the 71 Blueprint/Unique/Quest-linked exclusions outside the 831. The 2026-10-16 [follow-up audit](generators/STASH-SAFE-EXPANSION-FOLLOWUP-2026-10-16.md) records the 28 and 12 remaining non-SmartLoot review cases. All source rank/slot/item paths come from the uploaded CFG dump. This adds no new UI controls, and does not rewrite quest rewards, NPC corpses, breakable crates or already-generated inventory. Game and Wine validation still pending. [Source contract](generators/SMARTLOOT-PRODUCTION-EXTENSION-2026-10-16.md).


**CURRENT 2026-10-16 — ordinary additional stash source discovery:** Source mapping covers 7 world SmartLoot ammo families, 2 food/drink families, 2 grenade families and the 2 previously discovered medical SmartLoot families, plus 17 GamePass/CNPP core/helper generator SIDs. Details and exclusions: [full audit](generators/STASH-REGULAR-CATEGORIES-AUDIT-2026-10-16.md). This registry does **not** mean gameplay control coverage was added: existing Stash Content controls still write only the three regular TCD ItemGenerator clone families. Safe shared SmartLoot production redirects require the exact source actor consumer manifest absent from GitHub. Do not treat quest/unique/one-off items as ordinary rewards.

**CURRENT 2026-10-16:** Zero global MEDICAL or item-specific medical bias now zeros native entry counts in addition to Chance/Weight in audited regular stash generator clones. This guards against ambiguous all-zero weighted selection behavior; it does not cover previously populated, scripted/quest, separate CNPP, or unredirected stashes. Source-level tests were added, but gameplay is not yet verified.


## CURRENT 2026-10-16 — Graphical Stash Rewards and Medical Quantity

STASH REWARDS is a six-row chart: three native ammo-tier multipliers followed by RGD-5 Chance, RGD-5 Amount and Rare Coupons, all editable. MEDKIT QUANTITY is a separate 2x2 dot graph with MAX 0 ITEMS and AUTO as distinct stops. Stash Content and Extra Stash Gear retain their native bar/dot visualizations. Older rotary/standard-slider descriptions below are superseded for presentation only. Source item semantics and exact production clone ownership stay unchanged.


**Scope:** Source-backed optional controls for the production-only Basic, Standard and Rare stash generators. These controls are in **Stashes** and are distinct from **Stash Rewards** (ammo amounts and bonus grenade/coupon rolls), **Breakable Boxes**, **World Loot** and hand-authored one-off rewards.

**Evidence:** `STALKER2-GameData-CFG(20261016-101952).zip` → `GameData/ItemGeneratorPrototypes.cfg` (source game version 2.0.5, as tracked by project baseline). The source contains `Stash_Cheap_*`, `Stash_Medium_*`, `Stash_Expensive_*` wrappers pointing to `GamePass_Stash_ItemGenerator_Cheap`, `GamePass_Stash_ItemGenerator_Common_Var1` and `GamePass_Stash_ItemGenerator_Rare`. Categories use both `PossibleItems[*].Weight` and `Chance`; they may carry `Diff`/`PlayerRank` gates. Confirmed separately on normal authored stash prototypes: `WeaponPrimary` and `BodyArmor` categories. This is **structural source evidence, not game runtime proof**.

## UI and parameter binding

Two independent source-backed **interactive WPF graphs** replace the former six-card slider layout: **STASH CONTENT** is a shared-axis horizontal bar plot (376 DIP), while **EXTRA STASH GEAR** is a three-tier × two-item-type discrete dot plot (370 DIP). Both center a plot bank up to 785 DIP wide and show **source controls**, never reconstructed inventory shares or in-game probabilities:

1. **STASH CONTENT**: `loot.stashMixAmmo`, `stashMixMedical`, `stashMixFood`, `stashMixDrinks`, `stashMixGrenades`, `stashMixCoupons`. Each range **0–3x**, **0.25x step**, **1x = vanilla**.
2. **EXTRA STASH GEAR**: `loot.stashGearCheapWeapon`, `stashGearCheapArmor`, `stashGearMediumWeapon`, `stashGearMediumArmor`, `stashGearExpensiveWeapon`, `stashGearExpensiveArmor`. Each has four **discrete** stops: **0 OFF**, **1 RARE**, **2 MODERATE**, **3 FREQUENT**. All factory profiles default to OFF. OFF means *no additional gear generated by TCD*; deliberately placed vanilla weapons and armors are retained.

The six content rows share native 0x/1x/2x/3x ticks, with each bar and live numeric value independently editable. The gear graph aligns BASIC, STANDARD and RARE rows against WEAPONS and ARMOR columns; each current dot is one of four discrete source levels, with one numeric/state readout and shared OFF/RARE/MOD/FREQ labels under the two axes. Each changed numeric token alone is gold, while category labels, mode names and neutral reference markers stay white/grey. Hover, click/drag/snapping, Home/End, R/right-click reset, save/load and CFG writer binding remain active for all 12 source IDs; anchors remain `loot.stashMixAmmo` and `loot.stashGearCheapWeapon`. Small bottom captions distinguish relative weight/chance from real drop probability.

## Exact selection transforms

Only existing, specifically classified `ItemPrototypeSID` entries in the three audited template categories are affected:

| UI category | Known generator entries |
|---|---|
| Ammo | `A*` where second character is numeric (the source ammo SID scheme) |
| Medical | `Bandage`, `Medkit`, `ArmyMedkit`, `EcoMedkit`, `AntiRad` |
| Food | `Bread`, `Sausage`, `CannedFood` |
| Drinks | `Vodka`, `Water`, `Milk`, `Energetic` |
| Grenades | `GrenadeRGD5`, `GrenadeF1` |
| Coupons | `MoneyCommon`, `MoneyRare`, `MoneyEpic`, `MoneyLegendary` |

`Weight' = max(0, authored Weight × multiplier)`. `Chance' = clamp(0,1, authored Chance × multiplier)`. A source `Weight=0`/`Chance=0` stays zero even if multiplier exceeds 1. Unmatched item SIDs and category/rank/difficulty labels remain unchanged. `Chance=1` cannot exceed 1; increasing an already-certain entry is a no-op for that entry. This is **not** a universal chance that a stash contains a certain category; changing relative `Weight` can rebalance selection only inside the applicable source branch. 1x preserves the exact original raw string (including `f` suffix/formatting).

Existing special values compose before the content bias is applied: Basic RGD-5 chance may be changed and then multiplied by Grenades bias; Rare coupon chance likewise composes with Coupons bias. Existing tier-specific Ammo Quantity still edits only `MinCount`/`MaxCount`. This is the *same generator template and redirect path*, never an independent override writer that would replace other TCD patches.

## Additional gear

When a gear selector is OFF, its category **does not exist** in the emitted new generator. For any other state, the TCD-only generator appends a separate `WeaponPrimary` or `BodyArmor` category for each `PlayerRank` (Newbie, Experienced, Veteran, Master) with **one non-unique, source-confirmed SID** each. Fields: `Chance` = **0.02/0.07/0.15** for RARE/MODERATE/FREQUENT; `MinCount=MaxCount=1`; `MinDurability=0.35, MaxDurability=0.65` for weapons; `MinDurability=0.45, MaxDurability=0.75` for armor; a new weapon starts with `AmmoMinCount=AmmoMaxCount=0`. These are source **per-entry Chance values**, not measured overall probabilities.

| Rank | Basic weapon / armor | Standard weapon / armor | Rare weapon / armor |
|---|---|---|---|
| Newbie | GunPM_HG / Newbee_Neutral_Armor | GunM860_SG / Zorya_Neutral_Armor | GunAK74_ST / Jemmy_Neutral_Armor |
| Experienced | GunUDP_HG / Jacket_Bandit_Armor | GunAK74_ST / Jemmy_Neutral_Armor | GunG37_ST / Nasos_Neutral_Armor |
| Veteran | GunM10_HG / SkinJacket_Bandit_Armor | GunViper_PP / Light_Mercenaries_Armor | GunDnipro_ST / SEVA_Neutral_Armor |
| Master | GunM860_SG / Middle_Bandit_Armor | GunG37_ST / Nasos_Neutral_Armor | GunKharod_ST / Exoskeleton_Neutral_Armor |

Every SID in this table was confirmed to occur in the uploaded `GameData/ItemGeneratorPrototypes.cfg` as an `ItemPrototypeSID`. The progression mapping and per-entry Chance stops are **TCD design choices** requiring in-game verification, not vanilla rarity facts.

## Production safety and writer composition

The existing independently audited production redirect manifests are *reused, unchanged*: Basic **1,271**, Standard **450**, Rare **305** consumer assignments. Changes appear only in new `TCD_*` generator definitions and in those existing approved redirects, conditional on a modified content/gear source (or an existing ammo/bonus source). Scripted, quest, test, unresolved and one-off stash generators outside those manifests stay vanilla. New gear categories start after existing item category arrays: Basic index **12**, Standard **10**, Rare **19**; weapon rank categories occupy `firstFree+0..3`, armor `firstFree+4..7`. This prevents collision with original entries. Existing bonus and ammo controls remain independently operational.

Writer modification count and catalog `supportedParameterCount` include the twelve new special writer dimensions. New parameters are included in all twelve embedded factory JSON presets and only exact old stock factory hashes are upgraded, never edited user presets.

## Required manual regression (not yet verified)

- [ ] Application starts under Wine; six-row bar plot and 3×2 dot plot render compactly at normal and minimum width, without clipping, duplicate titles, invented stash-drop percentages, highlighted baseline numbers or old per-source slider cards.
- [ ] Click/drag all six bars and all six dot-plot channels; verify native 0.25x and integer snapping, per-source keyboard/Home/End, hover cues, baseline reference markers, right-click/R reset and Save/Load for all twelve controls.
- [ ] Bias 1x + gear OFF emits no new stash template on its own; other non-stash controls are unchanged.
- [ ] Each of the six mix sources at 0x / 0.25x / 3x: no negative Weight/Chance, Chance ≤ 1, 0 stays 0 and unrelated SID values are untouched.
- [ ] Basic, Standard, Rare selectors tested independently in all four modes; verify category indices/rank/SID/Chance/MinCount/MaxCount/durability are emitted only for active mode.
- [ ] Combine Basic ammo amount + Basic RGD-5 chance/quantity + Grenades bias + Basic Weapon gear: one composed TCD clone; no value overwritten.
- [ ] Combine Rare ammo amount + Rare coupon chance + Coupons bias + Rare Armor gear: one composed TCD clone; no value overwritten.
- [ ] Test old 248-source edited presets: loading defaults new controls without discarding existing values; new 260-source Save/Load preserves them; pristine stock profiles update, edited copies remain byte-identical.
- [ ] Compare output manifest targets against expected 1,271/450/305 (base and DLC1 scope), never changing quest or scripted stash roots.
- [ ] In-game on *fresh, previously unpopulated* stash instances and player ranks, sample actual rates separately; until this is done label all gear and bias runtime effects **RUNTIME TEST REQUIRED**, not validated percentages.

## Implementation files

- Catalog `src/TrueCustomDifficulty/Data/parameters.json`, baseline `cfg-writer-baseline.json`.
- Native editor `Controls/AggregateVisualControl.cs`, mapping `ViewModels/ParameterViewModel.cs` / `MainViewModel.cs`.
- Production writer `Services/CfgPatchWriterService.cs` + `Services/CfgPatchWriterService.StashTuning.cs`.
- Factory presets, pristine-upgrade guard, UI catalogs and per-control QA checklist.

**No runtime or full Windows publish proof is claimed from GitHub source review.**
