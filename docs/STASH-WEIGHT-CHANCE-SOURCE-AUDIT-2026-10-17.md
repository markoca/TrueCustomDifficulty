# Stash generation: Weight / Chance source classification (2026-10-17)

> **Research/reference only.** This is a source-level inventory from the supplied base-game and DLC1 CFG dumps, **not** a request to change generated CFG, UI, presets or runtime selection. Do not use this as evidence of actual player drop percentages. The current Stashes implementation scope, actor allowlists and UI contracts remain authoritative separately.

## Source snapshots and interpretation

- Base: `STALKER2-GameData-CFG(20261017-140154).zip` (paths beginning `GameData/`).
- DLC1: `STALKER2-DLC1-CFG(9).zip` (paths beginning `DLCGameData/DLC1/`).
- Inspected the full `StashPrototypes.cfg`, `CorpseClueStashPrototypes.cfg`, `ItemContainerPrototypes.cfg` and both `ItemGeneratorPrototypes.cfg` files, plus source examples in `SpawnActorPrototypes.cfg`. Counts below are syntactic/local authored rows under **named, stated source selections**, not a global number of active stashes. Inherited `refkey/refurl` branches must be resolved before inferring final values.
- Source words are preserved deliberately: **item `Weight`** = relative selection within an eligible group; **item `Chance`** = authored chance field in that row; **group `MinSpawnChance/MaxSpawnChance`** = separate SmartLoot spawn bounds; **`BaseSpawnChance/AddSpawnChance`** = clue discovery, not stash content. CFG alone does not establish the entire engine RNG algorithm or runtime probability.

## Result: different stash source families

| Source family / example | Local fields observed | Classification / boundary |
| --- | --- | --- |
| Basic / Cheap, `GamePass_Stash_ItemGenerator_Cheap` | 38 item `Weight` rows + 14 item `Chance` rows | **Both**, in different `ItemGenerator` groups; item type alone does not decide the field |
| Standard / Common Var1, `GamePass_Stash_ItemGenerator_Common_Var1` | 25 `Weight` + 14 `Chance` | **Both**; rank/difficulty/category gates retained |
| Rare / Expensive, `GamePass_Stash_ItemGenerator_Rare` | 23 `Weight` + 19 `Chance` | **Both**; rare coupons can be `Chance`, not a Weight-derived drop rate |
| `GamePass_Stash_ItemGenerator_Common_Var2` | 4 `Weight` + 5 `Chance` | **Both**, distinct from the usual Standard Var1 core |
| `Gamepass_Stash_ItemGenerator_Common` | Two *nested generator references*, both with `Weight` | **Weighted choice of subgenerators**, not directly collectible item weights |
| `GamePass_Stash_ItemGenerator_CheapNoFresh` | 25 `Weight` + 10 `Chance` | **Both**; separate ordinary stash candidate, not automatically part of main tier scope |
| `CNPPGamePass_Stash_ItemGenerator_Cheap` / `...Rare` | Cheap: 5 `Weight` + 10 `Chance`; Rare: 23 `Weight` + 10 `Chance` | **Both**, separate CNPP pools; do not presume active ordinary consumers |
| `Stash_Food_{rank}` and `Stash_Drink_{rank}` (four ranks each) | Food: 16 `Chance` rows; Drinks: 16 `Chance` rows | **Chance-only** in these local item entries |
| `Stash_SupMed_{rank}` (four ranks) | 8 `Chance` rows | **Chance-only** in these local entries |
| `Stash_Medkit_{rank}`, `Stash_Stimulator_{rank}`, `Stash_Grenade_{rank}` | 12 / 12 / 8 `Weight` rows respectively | **Weight-only** in these local entries; these are *not* the same thing as the three GamePass tiers |
| `StashMedFood_Cheap` | 4 `Chance` rows | **Chance-only** local item list |
| `StashAKMedFood_Medium`, `StashNeutralArmorMedFood_Expensive` | 6 / 9 `Weight` rows | **Weight-only** local item lists (including equipment), separate specialized reward pools |
| `DestructibleStash_*` | Different prototypes use `Weight` or `Chance` | **Mixed across this family**, but breakables are **not ordinary stash tuning** |
| WorldMap `*StashGenerator*` wrappers | Frequently `Category=SubItemGenerator`, `ItemGeneratorPrototypeSID=...`, `Weight=1` | **Weight of a referenced generator**; inspect its target before saying how physical items roll |
| Named quests / scripted base-game `*Stash*` generators | Many local item `Chance` rows; some `Weight` branches | **Variable**; not safe to classify as regular stash loot solely by SID text |
| DLC1 `DLC01_MQ06_Stash_ItemGenerator_Cheap` | 24 item `Weight` + 10 item `Chance` rows | **Both**, explicitly split by `Diff` / category branches; *quest-specific* |
| Other DLC1 quest stash generators (`DLC01_EQ00_Stash`, `DLC01_MQ06_Stash_teleport`, etc.) | Chance-based guaranteed-looking authored rows and some weighted items | **Chance / Weight / both**, per entry; do not convert into standard tiers |
| DLC1 `PlayerStashE03/E08_*Preset` | Mostly item `Chance`; template/default `Weight` can coexist on an item row | **Separate player-start presets**, not regular world stash randomization; existence of both keys on one row is not proof both are independently rolled |

**Exact anchor locations (base `GameData/ItemGeneratorPrototypes.cfg`):** `StashMedFood_Cheap` near line 14380; specialized Medium/Expensive near 14414 and 14626; `Stash_Food_Newbie` near 19114; `Stash_Grenade_Newbie` near 19720; `Stash_Cheap/Medium/Expensive_{rank}` wrapper declarations at 19811–19847; source GamePass Cheap at 64305, Common Var1 at 64724, Var2 at 65035, Rare at 65105; generated world wrappers begin at 65697; CNPP Cheap at 78715, Rare at 78854, CheapNoFresh at 79232. **DLC1 `ItemGeneratorPrototypes.cfg`:** presets begin at 272; quest `DLC01_EQ00_Stash` at 57392; `DLC01_MQ06_Stash_ItemGenerator_Cheap` at 58172; teleport at 58459.

### Rank wrappers are not new independent item tables

All four `Stash_Cheap_{Newbie,Experienced,Veteran,Master}` roots have `refurl=ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg;refkey=GamePass_Stash_ItemGenerator_Cheap`; the four Medium roots reference `...Common_Var1`; the four Expensive roots reference `...Rare`. Their own local bodies contain **zero** item rows. Their effective contents inherit shared pools; multiplying counts by four here would incorrectly double/quadruple count loot definitions. Runtime rank and difficulty filtering still matters inside the referenced pools.

## SmartLoot world stashes: a second, independent system

The base `GameData/StashPrototypes.cfg` defines `ItemGenerators[*].Rank -> SmartLootParams -> {PrimaryWeaponParams,SecondaryWeaponParams,PistolWeaponParams,ConsumablesParams,GrenadesParams,HealthParams,...}`. Two distinct chance layers can appear:

1. At **group/slot level**, `MinSpawnChance` and `MaxSpawnChance` bound a spawn condition, alongside `ItemSetCount`, `PriorityCaliber`, `MainWeaponAmmoCount`, etc.
2. At **item level**, entries carry either `Weight` or `Chance` (plus `MinCount/MaxCount`), depending on prototype/group. Do not treat a common spawn bound as each item's chance.

The **13 ordinary world SmartLoot prototype SIDs** tracked in the current repo ordinary-stash inventory contain **991 local item rows: 967 authored `Weight` and 24 authored `Chance`**, distributed as follows (source rows only, not distinct spawn outcomes):

| SmartLoot examples | Classification (local items) |
| --- | --- |
| `Stash_AmmoAll_Cheap` (284), `Stash_Ammo_Smart_Cheap` (99), `Stash_Ammo_Smart_CommonRare` (51), `Stash_AmmoSNG_Smart` / `...MainLoot` (104 each) | Weight-only item rows |
| `Stash_AmmoNATO_Smart` / `...MainLoot` (151 each) | Mixed: 149 Weight + 2 Chance per prototype |
| `StashMedicine_Cheap` (2), `StashMedicine_Smart` (13), `StashFood_Cheap` (3), `StashGrenade_Cheap` (2) | Weight-only item rows, although their enclosing slots still expose Min/MaxSpawnChance |
| `StashFood_Smart` (26) | Mixed: 7 Weight + 19 Chance |
| `StashGrenade_Smart` (1) | Chance-only item row |

**Exact anchor locations (base `GameData/StashPrototypes.cfg`):** `StashMedicine_Cheap` 110, `StashGrenade_Cheap` 262, `Stash_AmmoAll_Cheap` 293, `StashGrenade_Smart` 2814, `StashMedicine_Smart` 2839, `StashFood_Smart` 3027, `Stash_AmmoNATO_Smart` 3300, `Stash_AmmoSNG_Smart` 4791, `Stash_AmmoSNG_Smart_MainLoot` 5824, `Stash_AmmoNATO_Smart_MainLoot` 6857, `Stash_Ammo_Smart_Cheap` 8348, `Stash_Ammo_Smart_CommonRare` 9309.

Also present but **not ordinary world-stash scope**: `StashMedicine_Corpse`, `StashVodka_Corpse`, and `NPC_Ammo_Smart`, `NPC_Medicine_Smart`, `NPC_Water_Smart`. A `*_MainLoot` prototype may have 0/0 spawn endpoints even when it has authored item weights; no unconditional physical-loot assertion follows from its entries.

## Physical container, nested rolls, and corpse clue discovery

- `GameData/SpawnActorPrototypes.cfg` physically wires containers using `SpawnType=ESpawnType::ItemContainer`, sometimes **both** a comma-separated `StashPrototypeSID` and `ItemGeneratorSettings`. An observed actor links `StashMedicine_Smart,Stash_Ammo_Smart_CommonRare` plus `GamePass_Stash_ItemGenerator_Cheap`. These are **two source layers on one physical object**, not one combined numerical probability.
- Other actors select `PrototypeSID=Stash_Cheap_Newbie` / Experienced / Veteran / Master by `PlayerRank`. `ItemContainerPrototypes.cfg` models physical Bag/Backpack/Safe/cache containers; `CachePrototypeID`, `bOpeningInventory` and mesh/blueprint properties **do not encode a global item drop chance**.
- `GameData/CorpseClueStashPrototypes.cfg` has `BaseSpawnChance=0.02`, `AddSpawnChance=0.01`, `RankClue[*].Weight=10.0` and regional/rank settings. These control **clue / stash discovery selection**, not relative weights of ammo or medicine *inside* the stash. The precise accumulation algorithm is not proven by this CFG.
- `Chance=1` on a direct item entry does **not** mean 100% probability of finding that item in every stash in the world: its parent generator must first be selected/eligible, a branch may be rank/difficulty-gated, and quests, direct placement and other generators may intervene.
- `Attaches.Chance` and `Upgrades.Chance` nested inside a weapon item affect attached upgrades/accessories, **not** the weapon item's own `Chance`. Example: DLC1 `DLC01_MQ06_Stash_teleport` near lines 58495–58509. A field audit must respect nesting.

## Design implications / future reference (no changes authorized here)

1. **Separate source selection, group spawn, and stash discovery.** Never combine `Weight`, `Chance`, `Min/MaxSpawnChance`, and corpse clue chance into one percentage.
2. **A Weight-only global category control is not a full presence/absence control.** `0×` zeros eligible weighted rows, but separate Chance-only entries can still appear. Even when all entries in a pool are weighted, scaling every entry together can leave the *relative* selection unchanged. All-zero pools need fail-safe behavior and in-game verification.
3. **Expose Chance tuning only for explicit, source-backed and scoped fields.** Existing known native controls (e.g. Cheap RGD-5 and Rare Coupons) are meaningful exceptions; do not invent a catch-all Chance or treat Chance-only entries as Weight.
4. **Keep eligible ordinary WorldMap stash paths apart** from quest/unique/scripted/direct rewards, NPC corpses, destructibles, player-start stashes, and the clue-discovery subsystem. Changes to a shared source require a verified actor-consumer allowlist and clone/redirect protection.
5. **Document group-specific eligibility**, not generic per-item odds: identify tier, generator, branch, `Category`, `PlayerRank`/`Diff`, nested generator and field type before exposing controls. Never normalize weights across unrelated native groups or infer gameplay frequency from raw row counts.
6. **No UI/catalog change in this research commit.** For any subsequent UI work preserve `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md`, compact control geometry, and implementation/catalog parity; update code, catalog and tests in the same `main` commit.

## Related repository audits

- [Regular world stash ordinary categories](generators/STASH-REGULAR-CATEGORIES-AUDIT-2026-10-16.md) — existing 17 core candidates / 13 SmartLoot sources and source-to-actor classification.
- [Stashes implementation and protected consumers](STASH-CONTENT-GEAR-IMPLEMENTATION.md) — current writer scope and exclusions.
- [Generator topology](GENERATOR-MAP.md) and [loot mappings](LOOT-MAPPING.md).
- [Current UI lock](UI-DESIGN-LOCK.md) and [visual controls](VISUAL-CONTROL-SYSTEM.md).

**Validation status:** inspected supplied source CFGs and repository documentation; no runtime RNG instrumentation, in-game roll test, publish/Wine test, or new CFG patch was executed in this research change.
