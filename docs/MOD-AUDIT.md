# MOD AUDIT — external item and generator compatibility (S.T.A.L.K.E.R. 2.1)

**Canonical incremental audit.** Append a numbered mod entry for each supplied mod; retain source evidence, unresolved questions, and any in-game acceptance results. The objective is to design mod-aware generation without breaking the existing game-2.1, source-exact True Custom Difficulty (TCD) writers. This file records observations and proposed integration contracts, **not an assertion of shipped mod support**.

## CURRENT SCOPE LOCK — generator spawn eligibility only (2026-11-15)

**Decision:** The only intended TCD integration for items added by external mods is **ALLOW SPAWN / BLOCK SPAWN via item generators**. This decision supersedes suggestions elsewhere in this historical audit to extend TCD into repair effectiveness, durability coefficients, equipment mechanics, stamina/effects, costs, item mass, charges, crafting, UI assets, quest behavior or mod configuration. Those details may be observed as identifiers/context but **are never TCD edit targets** for this feature.

**Functional target:** For each verified concrete mod-added item SID (or, where explicitly selected, a complete mod-defined generator group), allow the player to retain its original generation or prevent its generation through the audited `ItemGeneratorPrototypes` graph. Default is **ALLOW**, preserving the mod exactly. **BLOCK** must remove/exclude that SID from the relevant generator's `PossibleItems` paths and any other proven item-generation branches using safe source-level patches; do **not** destroy the item prototype or modify the original mod files. Avoid changing vanilla items merely because the pool is shared. Respect existing native TCD catalog/writer behavior and unchanged preset values. The exact patching strategy for mod-defined roots and inherited native roots must be verified before shipping.

**Coverage follows generator paths, not the item's UI/category label:** Trader stock, generic-NPC *trader* stock, NPC corpse/loadout generators, stashes and other actual generator-root references are covered only where **verified by the supplied CFGs**. Example: Repair System declares 12 concrete kit SIDs via 7 custom pools and 80 patched native roots (20 named traders, 1 generic trader, 4 stashes, 55 NPC loot). A future item-level BLOCK requires excluding the chosen SID from the four underlying rank pools (or all reachable equivalent generation leaves), rather than assuming that toggling one visible trader role prevents stash/NPC generation too. This is a **design requirement, not implemented behavior**.

**Explicit exclusions:** Quest ItemAdd/grants, scripts, spawned actors and world-placed pickups that do not use audited item generators; items already in inventories, existing saves or merchant stock generated earlier; item *use* behavior and the mod's own mechanics. Never promise to remove previously generated items. No probability sliders, weight tuning or additional control families under this scope. A mod that adds no item generator is documented as **not eligible for this feature**.

**Audit minimum from now on:** exact item SIDs and their template for identity; source generator SID, file/scope, `refurl/refkey`, `{bpatch}` target, complete parent-child link chain, concrete `PossibleItems` leaf and every reachable acquisition *generator* domain; relevant Chance/Weight/Count are *read-only source evidence* to disambiguate paths. No need to fully decode effect, mesh, quest or gameplay logic **unless it is necessary to prove that a particular spawn route is an item generator**. Record missing compressed generator files as blockers; do not make mod support dependent on irrelevant mechanics.

**UI constraint if/when implemented:** compact source-catalog-matching ON/OFF selection with no repeated explanatory copy, no width-expanding matrices or always-visible per-SID lists, modified-only highlighting, and per-control right-click reset to the active preset default. Update `docs/UI-DESIGN-LOCK.md`, `docs/VISUAL-CONTROL-SYSTEM.md`, runtime/preset contracts and tests in the *same commit* as any future UI change. **No UI or writer change is part of this scope clarification.**


## Audit protocol

For every new mod, record:

1. **Provenance:** supplied archive filenames, sizes, hashes, mod name/version if evidenced, completeness of each container set (PAK/UTOC/UCAS).
2. **Actual CFG evidence:** package mount, exact internal CFG paths, added root SIDs, `{bpatch}` targets, `refurl/refkey` inheritance, item SIDs, inventory properties, and all generator nodes (`Category`, `PossibleItems`, Chance, Count, Conditions, Weight).
3. **Acquisition routes:** trader stock, world/quest pickup, stashes, corpse loot, quest ItemAdd, and any asset or Blueprint-only route. Record **observed** and **unverified** separately.
4. **2.1 cross-reference:** join generator SIDs and parent links to the authoritative game-2.1 manifests; distinguish base, DLC1, and external-mod provenance; do not confuse a shared generator with individual traders.
5. **TCD writer interaction:** whether existing controls modify direct leaves, shared roots, inherited clones, or only fixed native SID inventories; note independent mod-added branches that TCD does not enumerate.
6. **Compatibility and safety:** conflicting patches, load-order unknowns, absent dependencies, runtime selection chance, and effects on item types or categories. No assumptions about runtime behavior from CFG syntax alone.
7. **Status / next evidence:** concrete item-generator allow/block feasibility, generator paths requiring extraction, runtime tests, and support decision. Other mod behavior is out of scope per CURRENT SCOPE LOCK.

Use source-exact names, including source misspellings. Never quietly map `Junk` to `Medical`, `Food` or `Drinks` just because the item inherits a consumable template. Never assume a leaf `Chance = 1` guarantees an item in stock: ancestor selection, roll and refresh semantics must be established in-game. Do not mutate original mod files.

### Audit index

| ID | Mod | CFG item SID | Generator hook | TCD compatibility status |
|---|---|---|---|---|
| 001 | Sleeping Bag (packaged plugin version `1.0`) | `SBM_Item` | `Trader_Cosnsumables_ItemGenerator` → new `SBM` category | **Source audited, runtime unverified; direct native category filtering does not cover this new leaf** |
| 002 | ZoneWatch / ZST — Zone Standard Time (NewContent PAK) | Not yet verified (item CFG Oodle) | `ItemGeneratorPrototypes/zZST_FieldWatch110.cfg` (Oodle) | **Container/index audited; exact generator and item definitions pending decompression** |
| 003 | Point of No Return — Backpacks Pack | Not decoded (Oodle) | Mod-owned ItemGeneratorPrototypes.cfg plus native-path zBackpackGeneratorPatch.cfg | **Container + plaintext audited; generator paths and item SIDs pending Oodle extraction** |
| 004 | Point of No Return — Repair System | **12 decoded item SIDs, common consumable template** | **7 source pools + 80 native-root BPatch additions** (20 traders, 1 generic trade, 4 stashes, 55 NPC loot) | **Source decoded and joined to TCD 2.1; runtime coexistence/repair effects pending** |
| 005 | OXA Standard / Oxide (A+B+C PAK set) | **80 new concrete SIDs** in decoded direct generator leaves; 32 original patched SIDs also referenced | **18 new generator roots + 4 native-root BPatch**; T2/T3/T4 attachment pools and Rostok NATO trader | **Generator CFG fully decoded; source-level ALLOW/BLOCK feasible with mixed-pool guards; runtime unverified** |
| 006 | Project Itemization Reborn (4 PAKs; ItemGenerators + Main, NC/OC) | **150 distinct mod item prototype SIDs, 113 statically reachable from native item generator patches** | **109 non-BPatch generator definitions, 217 patch definitions, plus 12 PackOfItemsGroup patches** | **All 101 CFGs decoded; mixed vanilla pools, 56 bskipref operations, two unresolved stash-clue generator references; game runtime unverified** |

---

## 001 — Sleeping Bag

**Input:** two matching container sets supplied on 2026-11-15: `Sleeping_Bag_N(3)` and `Sleeping_Bag_O(3)`, each with `.pak`, `.utoc`, `.ucas`. These suffixes are archive filenames, **not** an inferred game load order. The original input files were inspected read-only.

| File | Bytes | SHA-256 |
|---|---:|---|
| `Sleeping_Bag_N(3).pak` | 14,969 | `fb5f5cbbce8505c5d6337b31e7af5293998699ac753b793e9fe7e4e8cd532489` |
| `Sleeping_Bag_N(3).utoc` | 3,635 | `4c50497a2ac706ecb0cacaa4435b8e8868f25e0b01d97484b849fd31b46a7f64` |
| `Sleeping_Bag_N(3).ucas` | 606,256 | `dad7c374e8271b72a456537a0d67bfe505f5f073257dc5529a02b61e6411dfbe` |
| `Sleeping_Bag_O(3).pak` | 1,611 | `dfe9a9f148a80ed0a896a022ba58c7f7e2c96416d7ce5f08b851f0b5f5f30ea0` |
| `Sleeping_Bag_O(3).utoc` | 811 | `1ef1cb70c3ac6afb0c03217afe7a16baf1b54ff170b48d8d70036997c724c54b` |
| `Sleeping_Bag_O(3).ucas` | 9,792 | `ead1279d3d61cb6ed256f043747a963dc362094653dd52ea0e76287db9af7ae9` |

### Container layout and extraction limits

- **N.pak** mounts at `../../../Stalker2/Mods/Sleeping_Bag/`. Its Pak index names `AssetRegistry.bin`, `Sleeping_Bag.uplugin`, and **13** `Content/GameLite/ModGameData/Sleeping_Bag/` CFG files. The plugin descriptor identifies version `1.0`, author `hwk`, enabled `ModConfigurationMenu`, and an explicitly loaded mod.
- N.pak CFG filenames: `BoolProviderPrototypes/SBM_BProvider.cfg`; `EffectPrototypes/SBM_EPlayer.cfg`, `SBM_ERTPC.cfg`, `SBM_ESound.cfg`; `ItemPrototypes/SBM_Item.cfg`; `MeshPrototypes/SBM_Mesh.cfg`; `PostEffectProcessorPrototypes/SBM_Post_Processor.cfg`; `QuestNodePrototypes/SBM_QItem.cfg`, `SBM_QPopup.cfg`, `SBM_QRTPC.cfg`, `SBM_QSave.cfg`, `SBM_QSound.cfg`; `QuestPrototypes/SBM_Quest.cfg`.
- **Uncompressed N.pak entries fully read:** `SBM_Item.cfg`, `SBM_Mesh.cfg`, `SBM_Post_Processor.cfg`, `SBM_QItem.cfg`, `SBM_QRTPC.cfg`, `SBM_QSave.cfg`, `SBM_Quest.cfg` and `Sleeping_Bag.uplugin`. Six other N CFG entries (BoolProvider, three Effects, QPopup, QSound) are **Oodle-compressed and not fully decoded in this audit**. AssetRegistry is compressed too.
- **O.pak** mounts at `../../../Stalker2/Content/GameLite/GameData/`; both CFG entries are uncompressed and were read in full: `DifficultyPrototypes/SBM_Difficulty.cfg` and `ItemGeneratorPrototypes/DynamicItemGenerator/SBM_Trader.cfg`.
- **N.utoc** exposes packaged `Sleeping_Bag` cooked assets including `SBM.uasset`, `Config.uasset`, `SBM_WSleep_Widget.uasset`, and an inventory icon `.../UITextures/Inventory/SBM_Item.uasset`. **O.utoc** exposes generated localization, actor-patch and world-subsystem assets. The UTOC directory listings were inspected; UCAS Zen payloads and Blueprint behavior have **not** been fully reverse-engineered. The icon asset does not replace the text item-prototype definition.

### New item prototype — exact native inheritance

**Source:** `N.pak` → `Content/GameLite/ModGameData/Sleeping_Bag/ItemPrototypes/SBM_Item.cfg`.

```cfg
SBM_Item : struct.begin {refurl=@BaseGame/ItemPrototypes/ConsumablePrototypes.cfg;refkey=TemplateConsumable}
   SID = SBM_Item
   ConsumeOnUse = false
   UIUseSound = EUISound::UseConsumableBandage
   Icon = Texture2D'/Sleeping_Bag/GameLite/FPS_Game/UIRemaster/UITextures/Inventory/SBM_Item.SBM_Item'
   Icon1x1 = Texture2D'/Sleeping_Bag/GameLite/FPS_Game/UIRemaster/UITextures/Inventory/SBM_Item.SBM_Item'
   MeshPath = StaticMesh'/Game/_Stalker_2/props/general/SM_gen_sleeping_bag_01.SM_gen_sleeping_bag_01'
   MeshPrototypeSID = SBM_Item_Mesh
   ItemGridWidth = 3
   ItemGridHeight = 1
   SortGroup = ESortGroup::Stamina
   SortPriority = 2
   InventoryActionTime = 0
   bIsUsesLeftHand = false
   bIsUsesRightHand = false
   EffectPrototypeSIDs : struct.begin
      [*] = SBM_ERTPC_Use
   struct.end
   Cost = 500.0
   Weight = 0.5
struct.end
```

**Source-backed observations:** SID `SBM_Item`; inherits vanilla `TemplateConsumable`; deliberately not consumed on use; occupies 3×1 inventory cells; declared cost `500.0` and weight `0.5`. Its effect prototype SID `SBM_ERTPC_Use` is referenced; underlying effects are not audited from the compressed CFG. `SBM_Item_Mesh` has its own modded mesh prototype. Item acquisition is not limited to traders: `QuestNodePrototypes/SBM_QItem.cfg` declares `SBM_QGet_Item` with `NodeType = EQuestNodeType::ItemAdd`, `ItemSID = SBM_Item`, `ItemsCount = 1`, `Repeatable = true`, `AddToPlayerStash = false`.

### Native generator extension — exact CFG

**Source:** `O.pak` → `GameData/ItemGeneratorPrototypes/DynamicItemGenerator/SBM_Trader.cfg`.

```cfg
Trader_Cosnsumables_ItemGenerator : struct.begin {bpatch}
   ItemGenerator : struct.begin {bpatch}
      SBM : struct.begin
         Category = EItemGenerationCategory::Junk
         bAllowSameCategoryGeneration = true
         PossibleItems : struct.begin
            [*] : struct.begin
               ItemPrototypeSID = SBM_Item
               Chance = 1
               MinCount = 1
               MaxCount = 1
            struct.end
         struct.end
      struct.end
   struct.end
struct.end
```

- The root name really is `Trader_Cosnsumables_ItemGenerator` (**Cosnsumables**, not Consumables); never autocorrect this SID.
- The mod uses a named **`SBM` sibling category** inside `ItemGenerator` on an existing shared root, via `{bpatch}`, not a standalone replacement for all trader stock.
- This new branch has `Category = EItemGenerationCategory::Junk`, regardless of the underlying `TemplateConsumable` item inheritance. `PossibleItems/[*]` references `SBM_Item` at local Chance `1`, MinCount `1`, MaxCount `1`. **The effective probability per trader and the number of trader offers are not proven** by these local fields.
- The independent `O.pak` difficulty patch is `Stalker : struct.begin {bpatch}` → `AllowedSaveTypes` → `[*] = ESaveType::Quest`. It modifies saving permissions, not item generators.

### Verified link to TCD's S.T.A.L.K.E.R. 2.1 source

Cross-checked `src/TrueCustomDifficulty/Data/trader-sells-2.1-runtime.csv` (immutable source-exact writer manifest) for **exact** `generator_sid = Trader_Cosnsumables_ItemGenerator`:

- **510 source leaf-occurrence rows** in **30 distinct reachable trader profiles**: **29** `GENERAL_TRADER` profiles and **1** `TECHNICIAN` profile (the latter in DLC1).
- Original leaves catalogued under this generator include `Medical`, `Food`, `Drinks` across source rank/slot paths. Counts are vanilla manifest occurrences, not an assertion that 510 modded items or 30 NPCs sell the bag.
- `src/TrueCustomDifficulty/Services/CfgPatchWriterService.TraderSells.cs` loads the fixed-hash 2.1 manifest; on an unchecked category it identifies only **pre-enumerated source leaves**, creates **copy-on-write inherited generator clones**, removes those leaves from the clones and redirects relevant `TradePrototype` references to those clones.
- `src/TrueCustomDifficulty/Services/TraderSellsPolicy.cs` contains a fixed list of player-facing SELL categories: Weapons, Armor, Attachments, Ammo, Medical, Food, Drinks, Artifacts, Detectors and Grenades. It has **no Junk category**. Technician SELL policies exist but the technician role is intentionally hidden from the editor.

**Implications, not yet runtime-verified:**

1. **Direct filtering gap:** TCD's present SELL writer has no row for `SBM_Item` and therefore cannot explicitly delete, scale or classify this modded leaf with its current fixed source-manifest approach. Disabling Medical, Food or Drinks is **not** a source-backed removal of the new `SBM` Junk branch.
2. **Shared-root interaction:** TCD clones `Trader_Cosnsumables_ItemGenerator` through a `refurl/refkey` source reference when changing native stock. Whether the patched `SBM` sibling propagates through this inheritance, and whether patch/load order alters the outcome, requires an actual 2.1 in-game test. Do not equate a syntactically correct BPatch with proven compatibility.
3. **Parallel acquisition:** Removing an item from trader stock would not remove its separate `SBM_QGet_Item` quest grant. Trader stock, stashes, world spawns and quest grants must remain independently audited.
4. **Classification ambiguity:** The generator categorizes the item as `Junk` but the item prototype inherits `TemplateConsumable`. Any eventual mod-aware TCD classification must inspect **both** the generator category and the item's own prototype/inheritance and allow explicit exceptions. A string-prefix or `Junk → Consumables` guess is unsafe.

### Proposed mod-aware support contract (design only; no runtime/UI change)

- Treat the vanilla 2.1 manifests as authoritative **for native items only**. Maintain a distinct externally discovered overlay of `(mod identity, source file, prototype SID, generator root, generator path, target item SID, declared category, reference chain)`. Do not modify or fabricate a new vanilla source hash.
- Resolve item prototype and generator ancestry before mapping to any TCD group. Preserve unknown item classes rather than silently removing them. Record each item across all distribution routes; an item can be a consumable prototype while its trader group is Junk.
- Define explicit merge behavior for added categories and inherited copy-on-write clones, with collision detection by exact source root, named child, item SID and the source of each patch. Avoid rewriting vendor-owned CFG files.
- Establish a policy for unknown/mod-added items (follow an existing category, a separate mod-content control, or remain untouched) **only after** more concrete mods and in-game tests. Keep mod support opt-in until proven and avoid new permanent UI for one sample.
- Verify exact load order and stock refresh separately; source alone does not prove runtime acceptance. Existing category switches should keep their current 2.1 semantics until a documented design change is approved.

### Runtime acceptance checklist — pending

- [ ] Confirm normal Sleeping Bag gameplay with N+O installed and TCD absent: the bag can be acquired through trader stock and independently via its quest route.
- [ ] Record which real trader(s) expose `SBM_Item`, at what conditions, with repeat/refresh behavior, not just whether `Chance = 1`.
- [ ] Build TCD with its native categories unchanged; check for `SBM_Item` with TCD output installed.
- [ ] Disable **General → Medical / Food / Drinks** individually and together, rebuild, check that vanilla target leaves follow each switch and record what happens to the `SBM` Junk sibling.
- [ ] Test TCD writer clone/inheritance behavior with the mod's patch order, and determine whether either patch masks the other.
- [ ] If mod discovery is introduced, prove that the newly discovered `SBM_Item` is handled without changing the locked native 2.1 manifest or overwriting the mod's pack.
- [ ] Verify any affected preset serialization/UI design contracts only after functionality is designed and implemented.

**Audit status:** CFG-level item definition, trader insertion, fixed-2.1 TCD join, and independent quest grant established. Six compressed N.cfg files, Zen/Blueprint runtime behavior and actual mod+TCD game interaction remain open. **No mod support has been implemented by this audit.**

---

## Next entries

Append `002 — <Mod>`, `003 — <Mod>`, etc. Use the same columns, evidence standards, cross-reference and runtime gates. Keep negative findings and unresolved risks; update an earlier entry with dated addenda rather than erasing the original audit trail.

---

## 002 — ZoneWatch / ZST — Zone Standard Time

**Audit date:** 2026-11-15  
**Supplied source:** `ZoneWatchStalker2-Windows-NewContent(1).pak`, **25,481 bytes**; **SHA-256** `df9a997ed67c3f3871778c5d6fbb18cc11721b2205cb1915ce719eb2a0caf466`.  
**Scope:** this *one* user-supplied PAK, inspected read-only. No matching UCAS/UTOC or other distribution files were supplied. The suffix `(1)` is the upload filename; do not interpret it as a mod version or load order.  
**Provenance:** Unreal PAK **version 11**, mount point `../../../Stalker2/`, **22 file entries**. PAK primary index SHA-1, full directory index SHA-1, path-hash index SHA-1, and all per-entry compressed/uncompressed payload SHA-1 digests were successfully checked. No encrypted index. The compression method table identifies **Oodle**. No archive corruption found in the available evidence.

### Inventory: 19 CFGs across two mount branches

There are **19 CFG files**, plus `AssetRegistry.bin`, `ZoneWatch.uplugin`, and `ZoneWatch.upluginmanifest`. The CFGs are **nine stored (fully read)** and **ten Oodle-compressed (indexed and verified but not decompressed)**. The presence of a file does **not** prove what it does.

| Exact internal path relative to PAK mount | Payload | Uncompressed bytes | Audit scope |
|---|---|---:|---|
| `Content/GameLite/GameData/CoreVariables.cfg_patch_ZoneWatch` | Stored | 166 | Full text verified |
| `Content/GameLite/GameData/ItemGeneratorPrototypes/zZST_FieldWatch110.cfg` | Oodle | 12,239 | **Critical: contents not decoded** |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/ItemPrototypes.cfg` | Oodle | 7,056 | **Critical: item SID/prototype values not decoded** |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/MeshPrototypes.cfg` | Oodle | 2,751 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/EffectPrototypes/ZST_SprintGlance207.cfg` | Stored | 919 | Full text verified |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestNodePrototypes/ZST_Wear_ZST_FerrymanTally.cfg` | Oodle | 1,256 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestNodePrototypes/ZST_Wear_ZST_FieldWatch.cfg` | Oodle | 1,220 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestNodePrototypes/ZST_Wear_ZST_LabSentinel.cfg` | Oodle | 1,232 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestNodePrototypes/ZST_Wear_ZST_SignalmanHold.cfg` | Oodle | 1,256 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestNodePrototypes/ZST_Wear_ZST_SWatch.cfg` | Oodle | 1,172 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestNodePrototypes/ZST_Wear_ZST_ZoneSentinel.cfg` | Oodle | 1,244 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestNodePrototypes/ZST_Wear_ZST_ZoneSignal.cfg` | Oodle | 1,220 | Contents not decoded |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestPrototypes/ZST_Wear_ZST_FerrymanTally.cfg` | Stored | 103 | Full text verified |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestPrototypes/ZST_Wear_ZST_FieldWatch.cfg` | Stored | 97 | Full text verified |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestPrototypes/ZST_Wear_ZST_LabSentinel.cfg` | Stored | 99 | Full text verified |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestPrototypes/ZST_Wear_ZST_SignalmanHold.cfg` | Stored | 103 | Full text verified |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestPrototypes/ZST_Wear_ZST_SWatch.cfg` | Stored | 89 | Full text verified |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestPrototypes/ZST_Wear_ZST_ZoneSentinel.cfg` | Stored | 101 | Full text verified |
| `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/QuestPrototypes/ZST_Wear_ZST_ZoneSignal.cfg` | Stored | 97 | Full text verified |

**Package structure is meaningful:** the global `zZST_FieldWatch110.cfg` lives under the **native GameData ItemGeneratorPrototypes directory**, unlike the mod-owned `ItemPrototypes.cfg` under `Plugins/GameFeatures/ZoneWatch/Content/GameLite/ModGameData/ZoneWatch/`. This is a plausible interface with native item generation, **not evidence yet of a particular trader, new item SID, stock chance, or exact BPatch target**.

### What is established by full CFG reads

**Global CoreVariables patch:**

```cfg
DefaultConfig : struct.begin {bpatch}
    MarkerCompassWidgetClass = WidgetBlueprint'/ZoneWatch/Runtime/W_ZoneWatchCompassMarker.W_ZoneWatchCompassMarker'
struct.end
```

Only the compass marker widget assignment is present in the uploaded global CoreVariables patch. It is **not** an item generator or a TCD Economy control.

**Mod-owned effects file `ZST_SprintGlance207.cfg`:**

- `ZST_SprintGlance50` — inherits `@BaseGame/EffectPrototypes.cfg;refkey=[0]`, `Type = EEffectType::SPDrain`, `ValueMin = 50%`, `ValueMax = 50%`, `bIsPermanent = true`, `IsSaveable = false`, `DuplicationType = EDuplicateResolveType::KeepNew`, `AffectedStaminaActionTypes/[0] = EStaminaAction::Sprint`, `Positive = EBeneficial::Negative`.
- `ZST_SprintGlance100` — same fields with `ValueMin = ValueMax = 100%`.

These are **two distinct effect prototype SIDs** associated with sprint stamina drain. The file **does not prove** which watch, trigger, activation mode or actual gameplay condition applies them. Do not treat those `50%` and `100%` values as TCD Stamina control settings without tracing the owning quest/effect application path.

**Seven QuestPrototypes (each full text read):** `ZST_Wear_ZST_FerrymanTally`, `ZST_Wear_ZST_FieldWatch`, `ZST_Wear_ZST_LabSentinel`, `ZST_Wear_ZST_SignalmanHold`, `ZST_Wear_ZST_SWatch`, `ZST_Wear_ZST_ZoneSentinel` and `ZST_Wear_ZST_ZoneSignal`. Every one is a top-level `struct.begin` with `SID = <matching name>` and `DLC = BaseGame`. They demonstrate **seven quest roots**, not seven confirmed purchase stock entries or seven item prototypes. Their actual wear/equip nodes are in Oodle-compressed `QuestNodePrototypes`.

**Plugin metadata:** `ZoneWatch.uplugin` declares `FriendlyName = ZoneWatch`, `Version = 1`, `VersionName = 1.0`, `EnabledByDefault = true`, `ExplicitlyLoaded = true`, `BuiltInInitialFeatureState = Active`, `CanContainContent = true`, and optional enabled `ModConfigurationMenu`. These are **plugin descriptor values**, not a verified release/package version for the full ZST mod. `Plugins/ZoneWatch.upluginmanifest` embeds the same descriptor.

### Modded item/generator questions — **not yet resolved**

The exact bytes of the two most important CFG files remain Oodle-compressed in the supplied PAK:

1. **`ItemPrototypes.cfg` (7,056 uncompressed bytes):** determine every new item root SID; base prototype and `refurl/refkey`; item type, equipped slot, price, weight, tags, durability and quest flags. Names and counts of actual items **cannot safely be inferred** from quest filenames.
2. **`zZST_FieldWatch110.cfg` (12,239 uncompressed bytes):** enumerate all actual `ItemGeneratorPrototypes` roots, `{bpatch}` targets, `PossibleItems` leaves and `ItemPrototypeSID`/chance/count/weight values. Determine whether it patches **native shared generator roots** or creates **new standalone** mod roots (or both). This is the decisive distinction for TCD mod-aware stock policies.
3. **`QuestNodePrototypes` (seven separate compressed files):** trace item equip/grant/removal conditions and wear/unequip signals. Quest naming alone is not proof of in-game item acquisition.
4. **`MeshPrototypes.cfg` (2,751 bytes):** trace visible meshes once item SIDs are known.
5. **Cooked asset/runtime behavior:** not auditable from this PAK alone; the watch UI and game feature behavior may reside in other installed container files and require runtime testing.

The provided **Oodle-compressed bytes and original uncompressed lengths are positively identified and payload-hash verified**, but an Oodle decompressor was not available during this pass. Extracted encoded streams are **not** readable CFG; never interpret compressed strings as source text. The PAK file index is complete, so the list above is complete **for this specific PAK**, not necessarily for the full distributed mod.

### TCD 2.1 cross-reference and compatibility implications

- Checked `src/TrueCustomDifficulty/Data/trader-sells-2.1-runtime.csv` on `main`: it contains **no `ZST_` or `FieldWatch` item/generator SID string**. This is expected: it is a deliberately fixed-hash inventory of **vanilla 2.1** trader stock, not a registry for installed mods.
- The current `CfgPatchWriterService.TraderSells.cs` only removes **manifest-enumerated native leaves** via per-trade **copy-on-write inherited clones**; role flags are ten fixed native categories. Without an overlay containing the decoded new item SIDs and their actual parent generator graph, **no existing TCD SELL toggle has demonstrated control over these new watch items**.
- Unlike entry 001 (Sleeping Bag), this PAK's **exact patch shape is unresolved**. The shared-root `{bpatch}` behavior proven for Sleeping Bag **must not be assumed for ZoneWatch** just from the file's native directory or its `zZST_...` filename.
- `CoreVariables.cfg_patch_ZoneWatch` edits a compass marker widget class in `DefaultConfig`; this is not a direct collision with TCD's native item generator modification. A comprehensive settings/Blueprint collision audit is still needed before claiming full runtime compatibility.
- New `SPDrain` effects introduce a **potential** interaction with Stamina controls, which is separate from Economy stock. Do not modify these mod-owned effects through TCD without a requested design and verified effect routing.
- **No implementation decision yet:** mod-added items might enter traders via a patched shared generator, via mod-defined standalone generators referenced from trade profiles, or via quest/MCM commands. A universal mod audit must classify these routes independently. A dedicated mod overlay is preferable to altering or weakening the locked native 2.1 source manifest.

### Separate public context (not a substitute for the supplied CFG)

The public [ZST — Zone Standard Time mod page](https://www.nexusmods.com/stalker2heartofchornobyl/mods/2721) describes wearable watches, trader offers, collectible lore watches and MCM spawning. Its page advertises **release 1.2.5** and 2.1 compatibility. The supplied PAK's plugin descriptor reports **`VersionName = 1.0`**. Those are **different metadata sources**: do not infer that the single PAK is the whole publicly described release, or use the published feature list as evidence of specific CFG generator paths/SIDs in this upload.

### Next evidence and acceptance gates

- [x] Verify PAK v11 index integrity, path hash index, full directory index and 22 named entries.
- [x] Separate **19 CFGs: nine full-text stored, ten Oodle-compressed**, with sizes and paths.
- [x] Record CoreVariables widget patch, both sprint-drain effect SIDs, seven quest roots and plugin metadata.
- [x] Cross-check fixed native 2.1 trader SELL manifest for `ZST_` and `FieldWatch` names.
- [ ] Oodle-decompress **`zZST_FieldWatch110.cfg` and `ItemPrototypes.cfg`**; extract all item/prototype/generator SIDs, native targets, chance and count fields.
- [ ] Decompress the seven wear quest nodes and mesh prototype to complete equip/grant and item evidence.
- [ ] Join decoded generator roots into native base/DLC1 `TradePrototype` and nested `ItemGeneratorPrototype` graph; identify traders/stock categories from CFG, not marketing text.
- [ ] Evaluate TCD copy-on-write inheritance/load order with ZoneWatch installed and native category toggles changed; test with game 2.1.
- [ ] Check missing full-mod archive/IoStore parts for cooked Blueprint/UI references if game runtime behavior becomes relevant.
- [ ] Define/implement mod-aware overlay only when exact source routing is verified and additional mod examples have been compared.

**Audit status:** archive-integrity, directory inventory, plugin, core patch, uncompressed effects and quest roots **complete**. Mod item-prototype identification and generation rules **open** due to Oodle compression. **No gameplay code, catalog control, UI, factory preset or TCD writer has been changed.**

---

## 003 — Point of No Return: Backpacks Pack

**Audited:** 2026-11-15. **Sources:** two user-supplied PAKs, inspected read-only. The audit records only what could be recovered from these exact packages; it does not assume any corresponding cooked assets or runtime files.

| Supplied archive | Exact bytes | SHA-256 | PAK mount |
|---|---:|---|---|
| `PointOfNoReturn-BackpacksPackStalker2-Windows-NewContent.pak` | 24,459 | `1fd91e43629889a279268c6b8a66d9e4bf310f5295f27f55fccd8dcc011490c6` | `../../../Stalker2/Mods/PointOfNoReturn-BackpacksPack/` |
| `PointOfNoReturn-BackpacksPackStalker2-Windows-OverrideContent.pak` | 1,876 | `5326668aba6a7a9fe2b1cfefe7081e2624f3c3dbb4a073244990520084089816` | `../../../Stalker2/Content/GameLite/GameData/ItemGeneratorPrototypes/` |

Both are Unreal **PAK version 11**. Verified primary-index, full-directory-index and path-hash-index SHA-1, plus every file entry's stored/compressed data SHA-1 against the archive headers. The archives contain **15 indexed files**: NewContent has 14 entries (12 CFGs, one `.uplugin`, one `AssetRegistry.bin`); OverrideContent has 1 CFG. Among 13 CFGs, **4 are stored/plaintext and fully read; 9 use Oodle and were not decompressed**. The size table below gives the *uncompressed size reported and cross-checked in archive metadata*, not the size of text successfully decoded.

### Complete file inventory

All paths are relative to their archive mount point. NewContent's CFG prefix is `Content/GameLite/ModGameData/PointOfNoReturn-BackpacksPack/`.

| Package | File under respective mount | Format | Uncompressed bytes |
|---|---|---|---:|
| NewContent | `/PointOfNoReturn-BackpacksPack.uplugin` | Stored, fully read | 504 |
| NewContent | `/AssetRegistry.bin` | Oodle | 46,971 |
| NewContent | `CameraShakeGroupPrototypes.cfg` | Stored, fully read | 644 |
| NewContent | `CameraShakePrototypes.cfg` | Oodle | 6,365 |
| NewContent | `EffectPrototypes.cfg` | Oodle | 35,131 |
| NewContent | `GlobalVariablePrototypes.cfg` | Stored, fully read | 646 |
| NewContent | `ItemGeneratorPrototypes.cfg` | Oodle | **32,682** |
| NewContent | `ItemPrototypes/BackpackItemPrototypes.cfg` | Oodle | **10,946** |
| NewContent | `MeshPrototypes.cfg` | Oodle | 3,755 |
| NewContent | `QuestNodePrototypes/BackpacksDemo.cfg` | Oodle | 14,856 |
| NewContent | `QuestNodePrototypes/MigrateBackpacks.cfg` | Oodle | 28,130 |
| NewContent | `QuestPrototypes/BackpacksDemo.cfg` | Stored, fully read | 535 |
| NewContent | `QuestPrototypes/MigrateBackpacks.cfg` | Stored, fully read | 541 |
| NewContent | `SpawnActorPrototypes/WorldMap_WP/BackpacksDemo.cfg` | Oodle | 10,865 |
| OverrideContent | `/zBackpackGeneratorPatch.cfg` | Oodle | **53,405** |

The two **physically separate generator sources** are the most important finding for TCD compatibility: a mod-owned `ItemGeneratorPrototypes.cfg` within NewContent and a `zBackpackGeneratorPatch.cfg` placed directly under the **native game's** `GameData/ItemGeneratorPrototypes/` via OverrideContent. The latter is a potential native generator integration point, **not** proof of which original root is patched, which item is injected, or whether the resulting contents appear at traders, stashes, NPCs or world spawns. Those require decompression of the actual CFG.

### Exact verified CFG evidence

**Global variable: `GlobalVariablePrototypes.cfg`** (the source includes a decorative PNR Team header):

~~~cfg
GVarBackpackSlotItem : struct.begin {refurl=@BaseGame/GlobalVariablePrototypes.cfg;refkey=[0]}
    SID = GVarBackpackSlotItem
    Type = EGlobalVariableType::String
    DefaultValue = DefaultBackpack
struct.end
~~~

This confirms one mod-owned global string-value prototype and its default value `DefaultBackpack`. The precise equipment behavior and state persistence must be checked against the compressed quest/effect definitions or runtime; `DefaultBackpack` is **not a proven item prototype SID**.

**Camera-shake group: `CameraShakeGroupPrototypes.cfg`**:

~~~cfg
BackpackCameraShakeGroup : struct.begin {refurl=@BaseGame/CameraShakeGroupPrototypes.cfg;refkey=[0]}
    SID = BackpackCameraShakeGroup
    Type = ECameraShakeGroupType::MaxCount
    MaxCount = 100
struct.end
~~~

This describes an effects/feedback group, not a generator or a loot entry.

**Quest roots:** `QuestPrototypes/BackpacksDemo.cfg` declares `BackpacksDemo` and `QuestPrototypes/MigrateBackpacks.cfg` declares `MigrateBackpacks`. Both are top-level `struct.begin` records with matching `SID` and `DLC = PointOfNoReturn-BackpacksPack`. Their actual item-grant/equip/cleanup behavior is not established: the relevant `QuestNodePrototypes` files are compressed. The separate `SpawnActorPrototypes/WorldMap_WP/BackpacksDemo.cfg` exists but is also compressed, so it is **not yet evidence of any particular spawned backpack or guaranteed world pickup**.

**Plugin metadata:** `PointOfNoReturn-BackpacksPack.uplugin` reports `FriendlyName = PointOfNoReturn-BackpacksPack`, `CreatedBy = PNR Team`, `Version = 1`, `VersionName = 1.0`, `EnabledByDefault = true`, `ExplicitlyLoaded = true`, `CanContainContent = true`, `Mod = true` and `BuiltInInitialFeatureState = Registered`. These are descriptor values rather than a tested mod version or proof of in-game activation. No MCM dependency was declared in this particular descriptor.

### TCD 2.1 compatibility analysis

1. **Native SELL manifest:** `src/TrueCustomDifficulty/Data/trader-sells-2.1-runtime.csv` contains no textual `Backpack`, `DefaultBackpack` or `PointOfNoReturn`. This only excludes the *currently known names* from the fixed original-game 2.1 inventory. Actual item SIDs from `BackpackItemPrototypes.cfg` are not yet decoded, so this is not a comprehensive per-SID comparison.
2. **Source-exact TCD writer:** `CfgPatchWriterService.TraderSells.cs` uses fixed native inventory rows and copy-on-write (COW) inherited generator clones for selectively disabled SELL categories. It does **not** dynamically import installed external mod CFGs. Therefore, it cannot be claimed to deliberately remove or regulate these yet-unknown new backpack item leaves. The original 2.1 manifest must remain unchanged.
3. **Two potential propagation routes:** mod-owned generators may define new roots and reference them from native patches, or the native-path patch may directly append item entries to existing original roots. Both are plausible, but **neither pattern is proven by filenames**. Exact `struct.begin`, `{bpatch}`, `refurl/refkey` and `PossibleItems` structure is needed.
4. **Category classification:** a wearable backpack cannot automatically be assigned to Armor, Consumables, Junk, Stash Gear or generic Equipment. Follow both the item prototype inheritance and actual generator category; do not derive semantics from the mod's name.
5. **Other distribution/side effects:** world map spawn actor, demo/migration quest nodes, and a large `EffectPrototypes.cfg` can affect acquisition or operation independently of the trader-stash generator graph. TCD should not touch these based on filenames.
6. **Load order/merging:** if TCD replaces native trader generator references with inherited COW clones, the interaction with a separate external patch may depend on resolved inheritance, BPatch order and actual reference targets. This is a **runtime test requirement**, not an established incompatibility.

### Comparison of the first three audited mods

| Mod | Evidence for mod items | Native generator integration | Additional routes |
|---|---|---|---|
| **001 Sleeping Bag** | Explicit `SBM_Item` prototype decoded | Explicit `{bpatch}` adds `SBM` category to shared original generator | Explicit quest `ItemAdd` grant |
| **002 ZoneWatch** | ItemPrototypes present, compressed | Native-path generator CFG present, compressed | Seven quest roots; behavior unverified |
| **003 Backpacks Pack** | `BackpackItemPrototypes.cfg` present, compressed | **Mod-owned generator file plus separate native-path generator patch**, both compressed | Demo + migration quest roots and world actor prototype present, behavior unverified |

### Remaining extraction and acceptance work

- [x] Verify both PAK v11 index structures and all 15 payload hashes.
- [x] Enumerate the 13 CFGs and read all four noncompressed CFGs plus plugin metadata.
- [x] Identify both the mod-owned generator and the separately packaged native-location generator patch.
- [x] Identify global slot variable, camera shake group and two quest roots without inventing item or spawn semantics.
- [ ] **Highest priority:** Oodle-decompress `OverrideContent/zBackpackGeneratorPatch.cfg` (53,405 bytes) and enumerate exact patched root SIDs, categories, indexed/named entries and `PossibleItems` item SIDs, chance, weights and counts.
- [ ] Decompress `NewContent/ItemGeneratorPrototypes.cfg` (32,682 bytes) and `ItemPrototypes/BackpackItemPrototypes.cfg` (10,946 bytes); join all item SIDs, inheritance and nested generator references.
- [ ] Decode quest-node/world-actor sources to distinguish vendor stock from quest grants, world spawns and other acquisition paths.
- [ ] Resolve source exact paths against game 2.1 trader, stash, corpse and world manifests; identify any real source category/rank mapping.
- [ ] Verify runtime precedence with and without TCD-generated patches, including stock refresh and independent mod mechanics.
- [ ] Decide mod-aware overlay/control behavior only after exact CFG routing and runtime evidence. Do not create a generic backpack toggle from filenames.

**Status:** container integrity, full two-archive inventory, four stored CFGs and plugin metadata **verified**. **Nine Oodle CFGs have not been decoded**, including both generator files, so mod item SID lists and direct stock integration remain pending. **Documentation only; no gameplay code, UI, presets or patch writer changed.**

---

## 004 — Point of No Return: Repair System

**Audit date:** 2026-11-15. **Scope:** both uploaded Unreal PAKs examined read-only (no repacking or changes to mod). No UCAS/UTOC or other associated files were supplied. Their filenames distinguish packaging roles and **do not establish game load order**. This entry is source inspection, not a claim that TCD already supports repair kits.

### Package provenance and verified inventory

| User-supplied file | Bytes | SHA-256 | Embedded mount point |
|---|---:|---|---|
| `PointOfNoReturn-RepairSystemStalker2-Windows-NewContent.pak` | 24,601 | `4da21f3a08c9ee1eb369c09a30e99a103c7e197a6469ab93cfe55199738e86c3` | `../../../Stalker2/Mods/PointOfNoReturn-RepairSystem/` |
| `PointOfNoReturn-RepairSystemStalker2-Windows-OverrideContent.pak` | 2,445 | `d22f05b500efdbc89989b9596cb87a2711ba896997f82378bcf99c00fb73c707` | `../../../Stalker2/Content/GameLite/GameData/` |

Both are **PAK format version 11**, with an Oodle compression-method entry and no encrypted index. Independently verified the primary-index, path-hash-index, and full-directory-index SHA-1 values, and each individual payload's SHA-1 against its archived entry header. Parsed the complete directory indexes; **13 total files, 11 CFGs:** NewContent **11 files (9 CFGs + plugin descriptor + AssetRegistry)**; OverrideContent **2 CFGs**. **Three CFGs were stored without compression and read in full; eight CFGs are Oodle-compressed and have not been decoded**. The decompressed byte lengths in the inventory are PAK header values, not a claim that compressed text has been inspected.

Exact relative paths from each mount:

| Archive | Relative file | Encoding | Original size |
|---|---|---|---:|
| NewContent | `AssetRegistry.bin` | Oodle | 51,331 B |
| NewContent | `PointOfNoReturn-RepairSystem.uplugin` | Stored / JSON decoded | 603 B |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/CameraShakeGroupPrototypes/RepairKitCameraShakeGroupPrototypes.cfg` | Stored / CFG decoded | 644 B |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/CameraShakePrototypes/RepairKitCameraShakePrototypes.cfg` | Oodle | 2,611 B |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/EffectPrototypes/RepairKitEffectPrototypes.cfg` | Oodle | 21,778 B |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/EffectPrototypes/RepairKitNotificationBlockers.cfg` | Stored / CFG decoded | 728 B |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/ItemGeneratorPrototypes/RepairKitItemGenerators.cfg` | **Oodle** | **11,238 B** |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/ItemPrototypes/RepairKitPrototypes.cfg` | **Oodle** | **10,363 B** |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/MeshPrototypes/RepairKitMeshPrototypes.cfg` | Oodle | 3,886 B |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/QuestNodePrototypes/RepairBonusWriteOff.cfg` | Oodle | 118,368 B |
| NewContent | `Content/GameLite/ModGameData/PointOfNoReturn-RepairSystem/QuestPrototypes/RepairBonusWriteOff.cfg` | Oodle | 6,890 B |
| OverrideContent | `CoreVariables.cfg_patch_pnr_rs` | Stored / CFG decoded | 609 B |
| OverrideContent | `ItemGeneratorPrototypes/zRepairKitGeneratorPatch.cfg` | **Oodle** | **56,987 B** |

The **different mount points are important**. The mod-owned `RepairKitItemGenerators.cfg` lives inside the feature's `ModGameData` tree, whereas `zRepairKitGeneratorPatch.cfg` is mounted under the **native** `Content/GameLite/GameData/ItemGeneratorPrototypes/` directory. This provides two *candidate* sides of item distribution: custom definitions and a game-facing patch. Its exact target generator SIDs, whether it uses `{bpatch}`, which vanilla categories it modifies, and whether it reaches traders, stashes, corpse loot or world spawns **cannot be established from the compressed file's name**.

### Verified complete text from the three stored CFGs

**OverrideContent → `CoreVariables.cfg_patch_pnr_rs`:**

~~~cfg
DefaultConfig : struct.begin {bpatch}
    DurabilityRound = 0.1
    LowDurabilityThreshold = 1
    ArmorDurabilityParamsCoef = 0.1
    HelmetDurabilityParamsCoef = 0.1
struct.end
~~~

The mod explicitly patches **four core durability-related fields** in `DefaultConfig`. The snippet above omits only the file's decorative comment header and preserves the CFG assignments. Interpret them as **authored values**, not as proven in-game percentage/damage multipliers; precise runtime calculations have not been established. In particular, this patch is a materially different compatibility surface than kit item generation.

**NewContent → `CameraShakeGroupPrototypes/RepairKitCameraShakeGroupPrototypes.cfg`:**

~~~cfg
RepairKitCameraShakeGroup : struct.begin {refurl=@BaseGame/CameraShakeGroupPrototypes.cfg;refkey=[0]}
    SID = RepairKitCameraShakeGroup
    Type = ECameraShakeGroupType::MaxCount
    MaxCount = 1
struct.end
~~~

Confirms a mod-owned camera-shake group limited by `MaxCount = 1`. No inference about number of repair kits, use charges or availability follows from this group.

**NewContent → `EffectPrototypes/RepairKitNotificationBlockers.cfg`:**

~~~cfg
BlockNotificationItemRemoveEffect : struct.begin {refurl=@BaseGame/EffectPrototypes.cfg;refkey=[0]}
    SID = BlockNotificationItemRemoveEffect
    Text = BlockNotificationItemRemoveEffect
    Type = EEffectType::FlagEffect
    bIsPermanent = true
    ShowUpgradeEffect = false
struct.end
~~~

Confirms one flag-effect prototype intended by its name to block an item-removal notification; its precise call sites and game behavior remain **unverified** until the associated effect/quest sources are decoded.

**Plugin metadata:** `PointOfNoReturn-RepairSystem.uplugin` declares `FriendlyName = PointOfNoReturn-RepairSystem`, `CreatedBy = PNR Team`, `Version = 1`, `VersionName = 1.0`, `CanContainContent = true`, `EnabledByDefault = true`, `ExplicitlyLoaded = true`, `BuiltInInitialFeatureState = Registered`, and `Mod = true`. It contains a plugin entry `LootingItems` with `Enabled = true`, `Optional = true`. This is a declared **optional integration**, not proof that the LootingItems plugin is installed or active. The descriptor's initially `Registered` state is not evidence of runtime activation.

### Item generator and prototype boundary — unverified, do not infer item SIDs

- **`RepairKitPrototypes.cfg` (10,363 B):** item prototype roots, `SID` values, base `refurl/refkey`, tags, `Cost`, `Weight`, use mechanics, counts/charges and ability to repair weapons versus armor **cannot yet be read**.
- **`RepairKitItemGenerators.cfg` (11,238 B):** generator root names, categories, `PossibleItems`, stock weights, chance, count and nested generators **cannot yet be read**.
- **`zRepairKitGeneratorPatch.cfg` (56,987 B):** exact native generator targets, `struct.begin {bpatch}` usage, referenced mod generators, add/replace behavior, source-game tier/category integration and vendor/loot distribution **cannot yet be read**.
- **`RepairBonusWriteOff.cfg`** exists both as a quest prototype (6,890 B) and large quest-node source (118,368 B); both are Oodle. Despite the filename, no specific charge depletion, item consumption, repair-discount or quest-grant behavior is confirmed.
- **`RepairKitEffectPrototypes.cfg`**, the mesh CFG and camera-shake CFG are also compressed. The one decoded notification-blocking effect does not prove any particular repair amount or object usage.

### TCD 2.1 integration and risk analysis

1. **Vanilla source-manifest boundary.** `src/TrueCustomDifficulty/Data/trader-sells-2.1-runtime.csv` has no textual `RepairKit`, `PointOfNoReturn-RepairSystem`, `RepairBonusWriteOff` or `BlockNotificationItemRemoveEffect` token. This is a negative check against *known names* only, not a check against unknown compressed prototype SIDs. The manifest is deliberately frozen to exact original game 2.1 sources; do not edit its hash, counts or provenance for the sake of a mod.
2. **Current SELL route.** `CfgPatchWriterService.TraderSells.cs` selects only source-manifest item leaves and creates inherited copy-on-write clones for modified roles/categories. It does not parse externally installed generator CFGs. Consequently no existing Economy checkbox has demonstrated deliberate control over repair kits. A new-root link or native-root `{bpatch}` append could propagate differently; verify decoded root and parent references before promising compatibility.
3. **Durability interaction is separate from generation.** `CoreVariables.cfg_patch_pnr_rs` edits `DurabilityRound` and `LowDurabilityThreshold`, which `docs/EQUIPMENT-CONTROL-DEEP-AUDIT.md` explicitly identifies as intentionally **unexposed** by TCD. The mod also sets `ArmorDurabilityParamsCoef` and `HelmetDurabilityParamsCoef`. TCD's `equipment.armorDurability` is recorded as editing `EnvironmentDifficulty.Armor_Durability` (runtime-test-required mapping), while `equipment.weaponWearPerShot` operates through weapon `DurabilityDamagePerShot` source entries. Therefore **not an identified same-field patch collision**, but a **possible compounded gameplay effect**: the repair mod's durability globals can change the resulting condition-loss behavior around TCD settings. Avoid silently neutralizing or overwriting these mod-defined coefficients.
4. **Economy repair prices are not kit generation.** TCD's existing `Service Prices` and reputation-linked repair cost controls must not be assumed to alter repair-kit drop probability, item cost, consumed charges or kit effectiveness. All need source-backed mappings, not a broad match on the word “repair.”
5. **Optional LootingItems integration.** Separate the item generation path from a possible plugin/Blueprint-managed item acquisition/use path. The optional descriptor entry should appear in a future dependency/compatibility report but is not a reason to change native trader categories.
6. **No premature UI.** A future mod-aware item overlay should join actual item SIDs from `RepairKitPrototypes.cfg` to *decoded* generator leaves, mod-owned roots, native root patch, and any independent quest paths, preserving owner/scope and collision risk. The classification as Medical/Consumables/Equipment cannot be reliably guessed from `RepairKit` naming alone.

### Contrast to previous audits

- **001 Sleeping Bag:** exact decoded `SBM_Item` and native shared-generator `{bpatch}` addition; independent quest grant known.
- **002 ZoneWatch:** mod-owned item prototypes + native-path generator file, but relevant CFGs Oodle; additional sprint-drain effects.
- **003 Backpacks Pack:** separate mod-owned and native-path generator sources, both compressed; extra global slot variable.
- **004 Repair System:** same **two-generator-source architecture** as 003, but **also explicitly modifies four global durability values**, and declares optional `LootingItems` integration. This must be assessed across **Loot/Trader AND Equipment Wear/repair gameplay**, not only vendor stock.

### Open checks and runtime acceptance

- [x] Verify both PAK v11 directory/index SHA-1 and all 13 entry payload hashes.
- [x] Record 11 CFG file paths and original lengths: 3 decoded, 8 Oodle.
- [x] Fully read `CoreVariables.cfg_patch_pnr_rs`, camera-shake group and notification blocker effect.
- [x] Note `LootingItems` optional descriptor entry and `BuiltInInitialFeatureState = Registered`.
- [x] Cross-check current TCD 2.1 source manifest for the mod's currently evidenced names; compare durability fields to current Equipment audit.
- [ ] Oodle-decompress **`zRepairKitGeneratorPatch.cfg` (56,987 B)** to identify exact native root patches, source paths and item SIDs.
- [ ] Decompress `RepairKitItemGenerators.cfg` and `RepairKitPrototypes.cfg`; enumerate all kit prototypes, inheritance, generation categories and chance/count fields.
- [ ] Decompress both `RepairBonusWriteOff.cfg` files and `RepairKitEffectPrototypes.cfg`; establish actual per-use repair logic, effect strength, kit consumption and any quest/spawn grants.
- [ ] Trace presence or absence of `LootingItems` runtime dependency and acquisition interfaces.
- [ ] Cross-reference decoded generator roots with game 2.1 trader, stash, corpse/world generation and source-safe TCD COW clones.
- [ ] Run mod-only/TCD-only/combined game 2.1 checks: appearance of each kit; effect of current TCD SELL/loot toggles; actual condition-loss changes at fixed TCD armor-wear settings; repair price independence.
- [ ] Design any mod-aware overlay/category mapping only once actual source paths and item prototypes are known and runtime interactions are tested.

**Status:** confirmed container integrity, complete 2-PAK inventory, 3 complete CFG texts, and plugin metadata. Critical **8 Oodle CFGs are not decompressed**, so named kit SIDs, exact generator integration, spawn probability and repair behavior are **not yet verifiable**. **Documentation-only audit: no TCD code, UI, presets, baseline manifests or generator writers changed.**

### 2026-11-15 decoded-source addendum — definitive generator and item map

**This section supersedes the earlier “not decoded” status for the following three CFGs only.** Original user-supplied PAK bytes were checked against each package's embedded SHA-1 digests before decoding. A Linux CI job compiled the GPL-licensed [q3k/pyooz](https://github.com/q3k/pyooz) (open-source Oodle/Kraken decoder) and successfully expanded the three actual packaged Kraken streams. Every result passed the exact expected decompressed-byte length and UTF-8 decode. Read-only decoding; no changes to either original mod PAK.

| Exact CFG in submitted PAK | Decoded bytes | SHA-256 of decoded content |
|---|---:|---|
| OverrideContent: `ItemGeneratorPrototypes/zRepairKitGeneratorPatch.cfg` | 56,987 | `88feb87d1fcf89f7f97a2c55c70a3d9f2d2f32bbfa71d6bf337b1a91230c585a` |
| NewContent: `ItemGeneratorPrototypes/RepairKitItemGenerators.cfg` | 11,238 | `c12acd2505f5b1ba0c9c398efb2bffb082be83439e6e6cbb757c486a6cd968b5` |
| NewContent: `ItemPrototypes/RepairKitPrototypes.cfg` | 10,363 | `b446af6e1eb5f71b7a005a13a5199a0c4bc4768d2f0e64fbd4f686705325e8b0` |

**Caution:** eight CFGs were compressed in the supplied 2-PAK set. **Three** of those eight are decoded here; the **five remaining** effect, quest, mesh and shake CFGs are still compressed/not audited at full-text level. TCD runtime coexistence is not proven by decoding alone.

#### Exact item prototype evidence: 12 concrete kits + 1 template

`RepairKitTemplate` inherits `@BaseGame/ItemPrototypes/ConsumablePrototypes.cfg;refkey=TemplateConsumable`. All twelve concrete kit SIDs below inherit `refkey=RepairKitTemplate`. This is a **Consumable prototype lineage**, not proof they should be governed by every vanilla consumable generator toggle. Values below are source-authored kit `Weight` and `Cost`, not spawn probabilities.

| Intended equipment group (source comment) | ItemPrototypeSID | Item Weight | Item Cost |
|---|---|---:|---:|
| Weapon | `Ramrod` | 0.3 | 1,200 |
| Weapon | `GunCleaningSolvent` | 0.2 | 1,700 |
| Weapon | `WeaponCleaningKit` | 0.6 | 3,100 |
| Weapon | `WeaponMediumRepairKit` | 1.2 | 3,700 |
| Weapon | `WeaponHeavyRepairKit` | 2.2 | 7,900 |
| Armor | `GlueTubeStar` | 0.12 | 1,200 |
| Armor | `GlueTubeH96` | 0.45 | 1,700 |
| Armor | `BasicSewingKit` | 0.5 | 3,100 |
| Helmet | `EmergencyHelmetKit` | 0.35 | 3,100 |
| Armor | `FieldArmorKit` | 0.7 | 4,300 |
| Armor | `HeavySewingKit` | 2.1 | 9,000 |
| Universal | `UniversalRepairKit` | 3.5 | 12,200 |

The source defines per-item `EffectPrototypeSIDs` for description, tier and use-effects; their implementation lives in the still-compressed `RepairKitEffectPrototypes.cfg`. The use-time strength, condition floor/ceiling and consumed-item behavior therefore remain unverified. Items are 1x1 grid by template except three explicit larger items: `WeaponHeavyRepairKit` (2x1), `HeavySewingKit` (2x1) and `UniversalRepairKit` (2x2).

#### Mod-owned pool graph — 7 new generator roots

All seven roots use `{refurl=@BaseGame/ItemGeneratorPrototypes.cfg;refkey=[0]}`, declare their own SID, and define an `ItemGenerator` subtree.

| Modded generator | Contents / source-defined weights |
|---|---|
| `RepairKitNewbiePool` | Ramrod 40; GlueTubeStar 40; GunCleaningSolvent 12; GlueTubeH96 12 |
| `RepairKitExperiencedPool` | Ramrod 25; GlueTubeStar 25; GunCleaningSolvent 30; GlueTubeH96 30; WeaponCleaningKit 12; BasicSewingKit 12; EmergencyHelmetKit 10 |
| `RepairKitVeteranPool` | GunCleaningSolvent 15; GlueTubeH96 15; WeaponCleaningKit 30; BasicSewingKit 30; EmergencyHelmetKit 25; WeaponMediumRepairKit 20; FieldArmorKit 18; WeaponHeavyRepairKit 5; HeavySewingKit 5 |
| `RepairKitMasterPool` | WeaponCleaningKit 15; BasicSewingKit 15; EmergencyHelmetKit 12; WeaponMediumRepairKit 25; FieldArmorKit 22; WeaponHeavyRepairKit 18; HeavySewingKit 18; UniversalRepairKit 6 |
| `RepairKitPoolGeneric` | Four `SubItemGenerator` groups conditioned individually on `PlayerRank` Newbie/Experienced/Veteran/Master, each referencing its matching rank pool with `Weight = 1`, `MinCount = MaxCount = 1` |
| `RepairKitStashPool` | Four rank-pool references with weights Newbie 85 / Experienced 15 / Veteran 1 / Master 1 |
| `RepairKitTraderPool` | Four rank-pool references with weights Newbie 65 / Experienced 25 / Veteran 8 / Master 2 |

Each rank pool uses `EItemGenerationCategory::Consumable` and per-item `Weight` fields, **not** per-item guaranteed chance. The selected-weight totals vary (e.g. the stash pool totals 102); don't silently normalize author-authored values or interpret weights as percentages without generator runtime evidence.

#### Exact native patch: 80 independent BPatch root additions

All 80 roots follow the same native-target pattern: `<NativeGeneratorSID> : struct.begin {bpatch}` → `ItemGenerator : struct.begin {bpatch}` → **new named `RepairKits` node**, `Category = EItemGenerationCategory::SubItemGenerator`, `PlayerRank = ERank::Newbie, ERank::Experienced, ERank::Veteran, ERank::Master`, `bAllowSameCategoryGeneration = true`; `PossibleItems/[*]` targets one of the seven mod-owned pool SIDs.

| Native source domain | Affected root count | Pool / chance / authored quantity |
|---|---:|---|
| Named trader stock | **20** | Based on trader region: `RepairKitNewbiePool`, `RepairKitExperiencedPool`, `RepairKitVeteranPool` or `RepairKitMasterPool`; `Chance = 1`; `MinCount = 5`, `MaxCount = 15` |
| Generic NPC **trading inventory** | **1** | `GeneralNPC_TradeItemGenerator` → `RepairKitTraderPool`; `Chance = 0.6`, `MinCount = 1`, `MaxCount = 3` |
| Stash generators | **4** | `GamePass_Stash_ItemGenerator_Cheap`, `GamePass_Stash_ItemGenerator_Common_Var1`, `GamePass_Stash_ItemGenerator_Common_Var2`, `GamePass_Stash_ItemGenerator_Rare` → `RepairKitStashPool`; `Chance = 0.05`, `MinCount = MaxCount = 1` |
| Generic NPC **loot** | **55** | faction/loadout `GeneralNPC_..._ItemGenerator` → `RepairKitPoolGeneric`; `Chance = 0.04`, `MinCount = MaxCount = 1` |

The **20 named trader roots**, exactly as authored:

- **Newbie:** `NewbeeVillageTrader_TradeItemGenerator`, `TraderChemicalPlant_TradeItemGenerator`, `TraderZalesie_TradeItemGenerator`.
- **Experienced:** `AsylumTrader_TradeItemGenerator`, `IkarTrader_TradeItemGenerator`, `ShevchenkoTrader_TradeItemGenerator`, `SultanskTrader_TradeItemGenerator`, `TraderTerikon_TradeItemGenerator`.
- **Veteran:** `CementPlantTrader_TradeItemGenerator`, `EgerTrader_TradeItemGenerator`, `MalakhitTrader_TradeItemGenerator`, `RedForestTrader_TradeItemGenerator`, `RostokTraderArmor_TradeItemGenerator`, `RostokTraderNato_TradeItemGenerator`, `RostokTraderSoviet_TradeItemGenerator`, `SelmaTrader_TradeItemGenerator`, `TraderSuska_TradeItemGenerator`, `VartaTrader_TradeItemGenerator`.
- **Master:** `PripyatTrader_TradeItemGenerator`, `YanovTrader_TradeItemGenerator`.

These groupings refer to the specific **pool each trader was assigned**, not the player's active difficulty/preset. One root is not necessarily a unique vendor NPC. All 20 named trader root SIDs are present in the TCD 2.1 source-exact SELL inventory and join to matching trade profiles; `GeneralNPC_TradeItemGenerator` also joins to `GeneralNPC_TradePrototype`. The other 59 additions are stash/corpse routes, not trader SELL inventory entries.

**Example of exact mod-owned patch semantics:**

~~~cfg
GeneralNPC_TradeItemGenerator : struct.begin {bpatch}
    ItemGenerator : struct.begin {bpatch}
        RepairKits : struct.begin
            Category = EItemGenerationCategory::SubItemGenerator
            PlayerRank = ERank::Newbie, ERank::Experienced, ERank::Veteran, ERank::Master
            bAllowSameCategoryGeneration = true
            PossibleItems : struct.begin
                [*] : struct.begin
                    ItemGeneratorPrototypeSID = RepairKitTraderPool
                    Chance = 0.6
                    MinCount = 1
                    MaxCount = 3
                struct.end
            struct.end
        struct.end
    struct.end
struct.end
~~~

The `0.6` and `0.04` are **per-patch PossibleItems Chance fields**, not proven actual percentages of NPCs offering or dropping kits, which may depend on ancestor rolls and refresh rules. Neither the 5–15 count nor pool Weight proves an actual vendor inventory count without runtime tests.

#### TCD compatibility: corrected conclusions after decoding

1. **Confirmed cross-domain overlap.** This mod extends native roots used by existing TCD Trader Stock, Generic NPC trade, Stashes and NPC corpse loot logic. These are all explicit source paths now, rather than speculative filename-based integrations.
2. **Existing TCD native SELL filters are incomplete for modded branches.** TCD `WriteTraderSells` reads a 2.1 fixed-hash inventory of **native leaves**, uses copy-on-write inherited clone SIDs, and removes only source-manifest leaves. None of the 12 repair-kit SIDs or new `RepairKits` branches appear in that manifest. Disabling Medical or other native category is **not** documented to remove repair-kit branches. The `SubItemGenerator` group category and `Consumable` child category must be tracked distinctly.
3. **Potential COW inheritance concern is specific and testable.** If a TCD-altered vendor's trade root points to a cloned generator inheriting the same source SID patched by Repair System, the `RepairKits` sibling could propagate — or fail to propagate — depending on BPatch/inheritance/load order. Test this at the actual 20 matching named roots plus the generic NPC trade root before promising automatic inclusion.
4. **Stashes and NPC loot require independent overlay routes.** TCD's existing native stash/generator controls may target other portions of those shared roots, and repair-kit additions can bypass pre-enumerated loot pools. Map 4 native stash target SIDs and 55 NPC loadout target SIDs explicitly if implementing mod-aware category filtering.
5. **Do not confuse repair kit item weights with generator weights.** `Weight = 0.2` in an item prototype represents item mass, whereas `Weight = 30` inside `PossibleItems` is weighted selection. Scaling or filtering one must not implicitly edit the other.
6. **Equipment/economy side effects remain separate:** the decoded item costs and weights alone do not prove that TCD global item-price scalers, service costs, consumable mass, armor wear, or weapon wear apply to them. The mod's independently verified `CoreVariables.cfg_patch_pnr_rs` modifies four durability globals; preserve those values until a deliberate compatibility design is established.
7. **No runtime claim:** direct CFG decoding validates author intent, not engine acceptance. Actual mod+TCD patch order, quantities, ranks, refresh, corpse rewards and use effects must still be tested in-game.

#### Remaining audit / implementation gates after successful decoding

- [x] Decode the **three critical CFGs**; verify expected sizes and record SHA-256.
- [x] Enumerate all **12** concrete kit item SIDs and their base `RepairKitTemplate`.
- [x] Enumerate **seven** new mod-defined generator pool roots and their item/pool weights.
- [x] Enumerate **80** exact original-game `{bpatch}` root additions (20 named traders + 1 generic trader + 4 stashes + 55 NPC loot).
- [x] Join 20 named trader roots plus generic trade root to TCD's exact game 2.1 SELL manifest.
- [ ] Decode remaining **five** compressed CFGs (repair effects, quest nodes/prototype, mesh and camera shake) for actual repair mechanics, quest acquisition and inventory use behavior.
- [ ] Run game 2.1 mod-only / TCD-only / combined acceptance; verify effect of TCD category/stock controls, native stash quantities, rank pools and actual repair kit results.
- [ ] Specify mod content overlay and per-domain inheritance/merge rules before implementing a new TCD mod-support feature.
- [ ] Keep source-payload redistributions out of the public repository; retain this derived SID and field audit as the canonical documentation.

**Revised status: source-level kit identity, rank pools and generation routes VERIFIED; in-game behavior and cross-patch merge behavior NOT YET VERIFIED. No TCD UI, gameplay, preset or writer changes.**

---

## 005 — OXA Standard (Oxide) — three PAK variants

**Audit date:** 2026-11-15. **Scope:** all three uploaded packages, read-only. **Current TCD goal is only generator-spawn ALLOW/BLOCK** (see CURRENT SCOPE LOCK). No runtime patch, preset or UI was changed for this entry.

### Verified source provenance / PAK boundaries

| Package | Exact bytes | SHA-256 | Mount point / role |
|---|---:|---|---|
| `OXA_StandardA_P.pak` | 110,813 | `8db9e973f11f1beaf619c7b54ef02a7474779b27dc8e2b2929395ca84e80ff81` | `../../../Stalker2/Content/GameLite/GameData/` — core 2.1 CFG changes, item generator prototypes |
| `OXA_StandardB_P.pak` | 146,698 | `2ca06d03653a10bcef5f3051f18e1ade83101f585e9195a13f3dc0cd6a8209ba` | `../../../Stalker2/Mods/Standard/` — `Standard.uplugin` + `AssetRegistry.bin` |
| `OXA_StandardC_P.pak` | 444,753 | `f5c54270b1ce45db64d57020b639e55708f06d17dc1528692af519bb9178200b` | `../../../` — DLC1 / Deluxe / Preorder / Ultimate source patches |

All three have a valid **PAK v11** footer and readable directory index. A has **51 CFG files** (50 Oodle-compressed + 1 stored); all 51 CFGs were decoded using the GPL open-source `gooz` Kraken-compatible shared library built in an isolated Linux runner, then extracted locally. C has **7 stored CFGs**, all read and digests verified. Together they supply **58 fully decoded CFGs**, of which **two in A** are generator configuration files. B has **2 files**, no CFG; its `Standard.uplugin` descriptor was independently extracted and SHA-1 validated against its PAK entry header. Its compressed `AssetRegistry.bin` was not decoded, as it is not a CFG source; B's directory-index flags require separate parsing and were not subjected to the same full-entry decoding as A/C. Index/mount observations are distinct from in-game load-order claims.

`Standard.uplugin`: `FriendlyName = "Standard"`; `CreatedBy = "Oxide"`; `VersionName = "1.0"`; `ExplicitlyLoaded = true`; `BuiltInInitialFeatureState = "Registered"`; `CanContainContent = true`. These are plugin-descriptor values, not proof of runtime feature activation. No external-gameplay component is assumed from the brand/name.

### Decoded item-generator graph

**Generator files:**
1. A → `ItemGeneratorPrototypes/DynamicItemGenerator_OXA.cfg` (**36,911** decoded bytes; 1,084 text lines).
2. A → `ItemGeneratorPrototypes/ItemGeneratorPrototypes_OXA.cfg` (**1,005** decoded bytes).

The first file declares **22 top-level generator roots**: **18 mod-defined inherited roots** with `refurl=../ItemGeneratorPrototypes.cfg;refkey=[0]` (15 direct item-category pools + 3 aggregator pools) and **4 patched existing original roots**. Its `PossibleItems` graph contains **119 direct ItemPrototypeSID leaf occurrences representing 112 distinct SIDs**, plus **19 nested `ItemGeneratorPrototypeSID` references**.

A cross-reference to **all decoded item-prototype CFGs within A and C** identifies those **112 distinct generator item SIDs** as:
- **80 new (mod-defined) concrete item SIDs** in the uploaded A prototype CFGs;
- **32 existing SIDs** that OXA **patches** with `{bpatch}`. Those are native/game-origin items and **must remain allowed** when the player blocks OXA-added item spawning.
- No unresolved direct item SID from those 119 leaves in the uploaded prototype sources.

**Important inventory distinction:** across 10 decoded item-prototype CFG files in A/C there are **327 top-level prototype definitions / patches**, comprising **150 new definitions** and **177 patches** (326 unique names across all these files; one original SID patched in more than one file). **Only 80** of the newly declared prototypes actually appear in `DynamicItemGenerator_OXA.cfg` item leaves. Many other declarations are templates, weapon component defaults, rails, sights, NVG pieces or functional data objects that are **not directly spawned by the audited generator**. Do not create an ON/OFF generator switch for each of the 150 merely because a prototype exists.

**Concrete generator group inventory — names are exact:**

| Direct generator family | Roots | Number of direct item leaves (occurrences) | Mod-defined / already existing (occurrences) |
|---|---:|---:|---:|
| Scopes | `OXATrade_ScopeBasicGenerator`, `OXATrade_ScopeStandardGenerator`, `OXATrade_ScopeAdvancedGenerator` | 31 | 26 / 5 |
| Muzzles | `OXATrade_MuzzleBasicGenerator`, `OXATrade_MuzzleStandardGenerator`, `OXATrade_MuzzleAdvancedGenerator` | 12 | 12 / 0 |
| Grips | `OXATrade_GripStandardGenerator` | 4 | 4 / 0 |
| Magazines | `OXATrade_MagBasicGenerator`, `OXATrade_MagStandardGenerator`, `OXATrade_MagAdvancedGenerator` | 32 | 5 / 27 |
| Silencers | `OXATrade_SilencerBasicGenerator`, `OXATrade_SilencerStandardGenerator`, `OXATrade_SilencerAdvancedGenerator` | 23 | 23 / 0 |
| Lasers | `OXATrade_LaserGenerator`, `OXATrade_LaserAdvancedGenerator` | 14 | 14 / 0 |
| Weapons inside standard aggregate | `OXATrade_AttachmentStandardGenerator` | 3 | 3 / 0 |
| **Total** | **15 source leaf-bearing generators** | **119** | **87 / 32** |

**Why 87 vs 80?** A few OXA-new SIDs occur in more than one tier, especially silencers/lasers; distinct selectable SID count is **80**, while generated leaves have **87 new-SID occurrences**. For per-SID blocking, all occurrences must be handled, not just the first one.

**Intermediate aggregator roots (3):**
- `OXATrade_AttachmentBasicGenerator`: child links to ScopeBasic, MuzzleBasic, GripStandard, MagBasic, SilencerBasic.
- `OXATrade_AttachmentStandardGenerator`: child links to ScopeStandard, MuzzleStandard, MagStandard, SilencerStandard, LaserGenerator; *also* includes three `EItemGenerationCategory::WeaponPrimary` direct leaves: `GunAKS74N_G2_ST`, `GunP30L_HG`, `GunGlock17_HG` (all OXA-new concrete prototype roots, `Chance = 0.1` per leaf, `MinDurability = MaxDurability = 1`).
- `OXATrade_AttachmentAdvancedGenerator`: child links to ScopeAdvanced, MuzzleAdvanced, MagAdvanced, SilencerAdvanced; `PlayerRank = ERank::Veteran, ERank::Master` on its group.

**Original-game generator integration (4 patches):**
- `Trader_Attachments_T2_ItemGenerator`: top-level `{bpatch}`, child `ItemGenerator : struct.begin {bskipref}`, new `[*]` points to `OXATrade_AttachmentBasicGenerator`.
- `Trader_Attachments_T3_ItemGenerator`: same with `OXATrade_AttachmentStandardGenerator`.
- `Trader_Attachments_T4_ItemGenerator`: same with `OXATrade_AttachmentAdvancedGenerator`.
- `RostokTraderNato_TradeItemGenerator`: `{bpatch}` appends named `OXA` under `ItemGenerator : struct.begin {bpatch}`; its two indexed children reference the T2 and T3 trader attachment generators, with `Chance = 1` each.

**Patch-shape risk:** `{bskipref}` differs from a plain `{bpatch}` leaf append. Treat it as a **potentially replaced/isolated underlying `ItemGenerator` subtree**, not as guaranteed preservation of original native entries. Do not assume TCD's native per-category SELL copy-on-write clone will inherit the same resolved children or that order is immaterial. Validate the real 2.1 merge/load behavior before implementing item-blocking edits.

The other decoded generator CFG `ItemGeneratorPrototypes_OXA.cfg` only edits `Arena_R3_Generator` with `{bpatch}`, authoring two original weapon leaves (`GunM701_SP`, `GunUDP_HG`) and an attached `EN_X8Scope_1`. This is a **native arena/test-style generator**, not a basis to classify the two native weapons as new mod loot. Under the user-approved scope, do not add a player-facing switch for this arena patch or edit quest/prologue routes.

### Relevant native 2.1 TCD manifest cross-reference

Compared the four exact patched generator root SIDs against `src/TrueCustomDifficulty/Data/trader-sells-2.1-runtime.csv` on `main`:

| OXA patched native SID | Vanilla 2.1 SELL manifest leaf occurrence rows | Distinct native trade profiles |
|---|---:|---:|
| `Trader_Attachments_T2_ItemGenerator` | 155 | 31 |
| `Trader_Attachments_T3_ItemGenerator` | 68 | 17 |
| `Trader_Attachments_T4_ItemGenerator` | 54 | 9 |
| `RostokTraderNato_TradeItemGenerator` | 41 | 1 |

These are **vanilla source inventory rows**, not 318 OXA items, 318 NPCs, or proof each profile generates every modded attachment. T2/T3/T4 appear in `GENERAL_TRADER` and `TECHNICIAN` profile paths; technicians remain hidden from the current TCD Trader Stock UI, not removed from the source. `RostokTraderNato_TradeItemGenerator` is a nested/native root in the one NATO Rostok trade profile. OXA's injected pool can affect multiple traders via the shared T2/T3/T4 generators; the named Rostok extension is an additional route to those pools.

The current `CfgPatchWriterService.TraderSells.cs` filters only pre-enumerated original 2.1 source leaves and creates **copy-on-write inherited generator clones** for edited vendor stock categories. Its fixed hash and source counts must be preserved. The new OXA pools/leaves are **not in that native manifest** and cannot be assumed to obey existing category OFF flags or new per-SID blocking without a separate mod-source overlay. Because these are **mixed pools**, naively deleting an entire T2/T3/T4 root or all `PossibleItems` entries would wrongly erase native attachments alongside mod additions.

### Generator-only ALLOW / BLOCK implementation contract for this mod

- **Default ALLOW:** do not touch mod generator definitions; original `Chance`, `Weight`, `MinCount`, `MaxCount`, `PlayerRank` and nested pool structure remain unchanged.
- **BLOCK all OXA-added spawned items:** suppress every branch/leaf that can resolve to the **80 mod-defined item SIDs**, while preserving the **32 native/previously existing** attachment SID selections and other unrelated native stock. The exact way of editing independent mod-defined roots vs. native `{bskipref}/{bpatch}` references requires a compatibility-safe CFG patch design and runtime validation.
- **Per-item BLOCK (if implemented):** match by exact concrete SID and **all** associated `PossibleItems` occurrences; block only its generator-derived eligibility. Examples: `OXA_Laser_3` appears in multiple laser tiers; modifying the item prototype itself is expressly out of scope.
- **Not eligible solely by prototype presence:** the other **70** new prototype declarations absent from this item's generator leaf set. They may be functionally installed attachments, templates, weapon defaults or accessible from separate scripts/quests, but they have no proven `ItemPrototypeSID` generator path in these packages.
- **No unrelated edits:** do not change weapon setup, perks, effects, upgrade behavior, item mass, price, magazine mechanics, DLC icons or asset metadata. The C package only patches existing DLC1/Deluxe/Preorder/Ultimate weapon/item setup and **contains no generator file**. The B package is the plugin/asset registry; no generator CFG.
- **User-facing control design (future code change only):** an independent compact allow/block selection for OXA spawn content should follow the locked control catalog and reset behavior. Do not produce hundreds of always-visible rows. The audit confirms eligibility, **not that TCD has shipped the control**.

### Remaining verification

- [x] Integrity and mount/source inventory of the three PAKs; all 58 CFGs in A/C decoded or read fully.
- [x] Parse 22 generator roots / 119 direct leaves / 19 generator links; match all 112 distinct direct item SIDs against mod prototype sources.
- [x] Identify mod-new vs native item definitions and the 4 patched native generators.
- [x] Join the four native roots to original 2.1 trader SELL manifest and flag `{bskipref}`.
- [ ] Prove source merge and COW inheritance with `{bskipref}` under game 2.1 and TCD patch order, without affecting native attachments.
- [ ] Test ALLOW vs. targeted BLOCK at an affected trader, including Rostok NATO, mixed pools and the three extra weapon entries.
- [ ] Test whether unique spawned non-generator acquisitions exist; these remain outside TCD scope.
- [ ] Before coding: define a deterministic per-mod CFG identity (hash + target generator paths), compatible output packaging and explicit opt-in; ensure absent mod does not generate dangling references.

**Status:** exact mod-owned item-generator graph and native-root relationships **source verified**, with **80 source-backed candidate spawned modded SIDs**. In-game merge/stock refresh and TCD ALLOW/BLOCK implementation **not yet verified**. **Audit/documentation only**.


### 2026-11-15 — Source compatibility verification (after MOD AUDIT 005)

**Executed checks, not hypothetical:** the original uploaded Standard A+C decoded CFG trees were examined together with TCD's current `main` 2.1 SELL manifest and writer. Reproduction guard: `tools/audit_mod_oxa_generators_21.py` (provide local decoded `--oxa-a` and `--oxa-c` for the full mod scan). Original PAK/CFG payloads are **not** committed.

**Result:**

| Check | Result | Evidence / boundary |
|---|---|---|
| All OXA generator roots and bridge references | **PASS** | 22 generator roots: 18 mod-owned + 4 patched native; 119 direct `ItemPrototypeSID` occurrences, 19 `ItemGeneratorPrototypeSID` links; no dangling links or cycles |
| Original vs modded leaves | **PASS** | 87 occurrences of 80 distinct new item SIDs; 32 occurrences of 32 distinct existing item SIDs in mixed pools |
| Native trader entrypoints | **PASS** | T2/T3/T4 + Rostok NATO are the four native integration roots; all have exact matches in TCD's current 2.1 SELL inventory |
| Reachability from these four native roots | **CORRECTION** | Only **76 of the 80 distinct new SIDs** are reachable. **`OXATrade_LaserAdvancedGenerator` has no inbound generator link** in the supplied A/C CFG files; its four unique `OXA_Laser_7`, `OXA_Laser_8`, `OXA_Laser_9`, `OXA_Laser_10` have no verified route to the native trader graph. Its 8 item occurrences do not represent confirmed stock |
| Safe *symbolic* source-only block | **PASS (model only)** | Removing OXA-new leaf SIDs from the source lists preserves all **32 existing item leaf occurrences** in original order. A per-SID BLOCK must remove all occurrences: 7 modded SIDs appear twice across pools |
| Trader Stock writer versus OXA `bskipref` | **COMPATIBILITY RISK CONFIRMED** | TCD COW clones inherited roots and removes leaves using **vanilla 2.1 indexed paths**. OXA uses `ItemGenerator {bskipref}` for all three tier roots. After replacing inherited child arrays, the original leaf indices may no longer refer to original entries. **Do not assume a disabled native Attachments category safely coexists with OXA** without engine-level merge and stock testing |
| Additional generator references in supplied A/C | **PASS** | All `ItemGeneratorPrototypeSID` links found in the decoded package occur in the audited generator CFG. The separate debug quest has four `SetItemGenerator` nodes referring to other rank-gear generators; not a proven direct grant of OXA SIDs |
| Non-generator direct item grants | **NOT OBSERVED** | No `EQuestNodeType::ItemAdd` or `SpawnedPrototypeSID` in the 58 supplied decoded CFGs. This **does not prove** absence of cooked-asset or game-owned alternate acquisition routes |
| Actual mod+TCD game engine merge/stock behavior | **NOT RUN** | No running STALKER 2 engine available; exact `bskipref` merge order, COW inheritance timing, final stock, and patch precedence remain unverified |

**Native 2.1 join** (exact rows and trade profiles; these counts describe *original-game manifest occurrences*, not actual OXA products):

| Native root | Source leaf rows | Distinct trade profiles | Distinct original direct indexed leaf paths |
|---|---:|---:|---:|
| `Trader_Attachments_T2_ItemGenerator` | 155 | 31 | 5 |
| `Trader_Attachments_T3_ItemGenerator` | 68 | 17 | 4 |
| `Trader_Attachments_T4_ItemGenerator` | 54 | 9 | 6 |
| `RostokTraderNato_TradeItemGenerator` | 41 | 1 | 14 |

**Known unsafe shortcut:** reusing the current TCD original-leaf removal paths against OXA's substituted tier `ItemGenerator` is **not verified safe**. Another unsafe shortcut is globally disabling tier roots or all mod-defined pools: that may delete existing (non-OXA) item selections because OXA pools contain **32 original SID occurrences**. Per-SID source filtering is logically possible but **must be translated into safe, deterministic generator CFG changes** before any shipping implementation.

**Recommended acceptance matrix (requires the game):** test original OXA alone, TCD alone, both with all Trader Stock categories ON, then both with General→Attachments OFF. In both modes test a T2/T3/T4 vendor and Rostok NATO, inspect final generated TradePrototype/ItemGenerator roots and logs, verify newly spawned OXA items, verify native attachments still appear when permitted, and compare mod load order. Test one duplicated SID (e.g. `OXA_Laser_3`) across all pools. Do not infer success from a syntactically valid BPatch alone. For a future item-level OFF, also verify native items in mixed pools remain and unconnected `OXA_Laser_7`–`10` are not incorrectly presented as active generator stock. Use developer logging `LogParserNodePatching` (Verbose) and `-ProtoLoadingMode=2` when inspecting engine patch application; see [Zone Kit patching guide](https://zonekit-support.stalker2.com/en-US/kb/article/172/how-to-patch-contents-of-a-cfg-file).

**Scope decision:** continue designing **only item-generator ALLOW/BLOCK**; no cost, weight, weapon configuration, chance or effect edits. **No gameplay writer/UI change made.**

---

## 006 — Project Itemization Reborn — four PAKs

**Audit date:** 2026-11-15. **Scope locked:** item-generator ALLOW/BLOCK only; *not* item effects, cost, mass, use, durability, or game difficulty. Four user-supplied PAKs were examined read-only. The figures below are from directly decoded CFG files, **not observed engine spawn rates**.

### Provenance and complete CFG decoding

| Uploaded file | Bytes | SHA-256 | PAK mount | CFGs successfully decoded |
|---|---:|---|---|---:|
| `ProjectItemizationReborn_ItemGenerators_NC.pak` | 49,993 | `43bf9bee5036f2d0a0a8debd2ba5216f6552c3cb2e4abfe677df89d17f648bc3` | `../../../Stalker2/Mods/ProjectItemizationReborn_ItemGenerators/` | 36 |
| `ProjectItemizationReborn_ItemGenerators_OC.pak` | 28,227 | `54937c3f31e5b92ace60ba845c34becf3b290107931a619a015ebcacca2fa613` | `../../../Stalker2/Content/GameLite/GameData/` | 31 |
| `ProjectItemizationReborn_Main_NC.pak` | 101,785 | `96bcb7a0f3eb34254c07c2f47982e8b8a43058ca4c802f6a939a1af4be1111bd` | `../../../Stalker2/Mods/ProjectItemizationReborn_Main/` | 33 |
| `ProjectItemizationReborn_Main_OC.pak` | 580 | `1dc20fbb6cf6b6f3b3ba905e9a7f6341e36af19bcd0b605a1ef075adfbacac03` | `../../../Stalker2/Content/GameLite/GameData/` | 1 |
| **Total** | | | | **101** |

All four are Unreal **PAK v11**. The primary, path-hash and directory indexes, as well as individual payload SHA-1 values, were checked by the extractor. All **101 actual CFG / CFG patch files** were decoded completely; the 344,201-byte `PackOfItemsGroupPrototypes/PIR_PackOfItemsGroupPrototypes.cfg` required independently expanding **two Kraken compression blocks**, not pretending a multi-block payload is one block. Neither original PAK nor unpacked original CFG bytes were written to the GitHub repository. Non-CFG binary assets and game runtime are outside this audit.

This is a materially larger mod than audits 001–005: both a mod-owned item-generator tree and extensive direct rewrites of vanilla generator roots, plus *another* native weighted-item generation system.

### Source census: definition ≠ generator eligibility

| Source class | Source-verified count | Important interpretation |
|---|---:|---|
| NewContent `ItemPrototypes` top-level declarations | **151** | **150 distinct SID names**; `PIR_cns_CigarettesDreamRegular` is defined **twice** (identical prototype body; repeated SID may still matter to the CFG loader) |
| Item-generator top-level declarations | **326** | **274 distinct root names**; 109 non-`{bpatch}` declarations (108 distinct names), 217 `{bpatch}` declarations (166 distinct target names) |
| Generator child links (`ItemGeneratorPrototypeSID`) | **375** | 363 refer to named generator roots declared in these PAKs, 11 refer to missing mod-owned stash-clue roots, 1 refers to a separately known base-game generator |
| Source `ItemPrototypeSID` occurrences in all `ItemGeneratorPrototypes` CFGs | **2,949** | Includes repeated difficulty/rank leaves and one *empty placeholder*; **1,519 occurrences** identify mod-defined items; source occurrence totals are **not** item counts in game |
| Distinct mod-defined SIDs referenced in those generator leaves | **119** | 31 of 150 prototype SIDs never appear in a `PossibleItems` leaf in the supplied item-generator graph |
| **Mod-defined SIDs reachable from a patched original generator root** | **113** | **Source-level ON/OFF candidates**. Their runtime activation and final roll/stock outcome still need testing |
| Distinct mod-defined generator-leaf SIDs with no demonstrated path from native root | **6** | Keep *out* of any “actively spawning” control until additional generator/actor references are verified |

The **51 duplicated native patch-target names** are principally repeated zombie roots and `MedicNoon_TradeItemGenerator` across source files; a further duplicated **mod-owned** root is `PIR_Stash_Ammo_A556E`, declared twice with identical source bodies. Do not treat declaration counts as unique engine roots. No generator-reference cycle was found among the declared roots, but see the unresolved references below.

### Generator families and corresponding spawn routes

The 166 **distinct patched native generator roots** fall into the following source-name and patch-file domains. A SID may be reachable through *several* families, so family counts must never be summed to obtain 113.

| Native target category | Distinct patched roots | Distinct newly defined item SIDs reachable through the graph |
|---|---:|---:|
| Named traders, medics and bartenders | **39** | 87 |
| Generic NPC **trading stock** | **9** | 87 |
| Human NPC loadout / loot | **25** | 87 |
| Zombie loadout / loot | **50** | 4 |
| Mutant loot generators | **14** | 24 |
| GamePass stash generators | **3** | 82 |
| Destructible stash / container generators | **23** | 27 |
| Prologue/quest-related original generator roots | **2** | 1 (also reached elsewhere; **do not target quest/prologue**) |
| Other native root | **1** | 0 |

Core mod-defined generator CFG locations (relative to `ItemGenerators_NC`'s `Content/GameLite/ModGameData/ProjectItemizationReborn_ItemGenerators/`):
- `ItemGeneratorPrototypes/ItemGeneratorPrototypes_PIR.cfg` (ammo and destructible generator definitions; 43 top-level roots; 543 item references).
- `ItemGeneratorPrototypes/PIR_ItemPools.cfg` (42 roots, 1,309 item references; named misc, food, medical, money and stash pools).
- Five `PIR_ItemPools_NPC_*.cfg` files (Bandits, Freedom, General, Monolith, Scientific; one root each).
- Seven `DynamicItemGenerator/DynamicItemGenerator_PIR_*.cfg` files (Bandit, Bartenders, Freedom, GeneralNPC, Medics, Monolith, Scientific).
- The native-path `ItemGenerators_OC/ItemGeneratorPrototypes` patches cover original traders, medics, bartenders, faction NPCs, zombies, mutants, destructibles and GamePass stashes.

**Generator-only classification:** from the 113 source-reachable new item SID names: **6** base components (`PIR_bcp_*`), **30** consumables (`PIR_cns_*`), **52** miscellaneous (`PIR_msc_*`), **25** mutant loot (`PIR_mlt_*`). These are namespace families from author-supplied SID names and prototype files, **not** replacements for native in-game item categories.

**Concrete candidate SID inventory, grouped for later whitelist/blacklist mapping:**

- **Base components (6):** `PIR_bcp_ElectronicParts`, `PIR_bcp_FabricParts`, `PIR_bcp_GlassParts`, `PIR_bcp_MetalParts`, `PIR_bcp_PlasticParts`, `PIR_bcp_WoodenParts`.
- **Consumables (30):** `PIR_cns_AdrenalineShot`, `PIR_cns_Amoxicillin`, `PIR_cns_Antidote`, `PIR_cns_Apple`, `PIR_cns_ArmyStimpak`, `PIR_cns_BagOfWeed`, `PIR_cns_CaffeineTablets`, `PIR_cns_CigarettesDreamLight`, `PIR_cns_CigarettesDreamMenthol`, `PIR_cns_CigarettesDreamRegular`, `PIR_cns_CigarettesSpark`, `PIR_cns_CigarettesWinston`, `PIR_cns_Cocaine`, `PIR_cns_EnergyBar`, `PIR_cns_GlucoseInjector`, `PIR_cns_GoldenCigaretteCase`, `PIR_cns_GunOil`, `PIR_cns_HomemadeStimpak`, `PIR_cns_Ibuprofen`, `PIR_cns_JarOfPickles`, `PIR_cns_Joint`, `PIR_cns_MorphineInjector`, `PIR_cns_Radioprotectant`, `PIR_cns_StaleApple`, `PIR_cns_StaleBread`, `PIR_cns_StaleSausage`, `PIR_cns_WrappedApple`, `PIR_cns_WrappedBread`, `PIR_cns_WrappedSausage`, `PIR_cns_Yadulin`.
- **Miscellaneous (52):** `PIR_msc_BoxOfSteelNails`, `PIR_msc_BrokenWalkieTalkie`, `PIR_msc_CellBatteries`, `PIR_msc_CircuitBoard`, `PIR_msc_CleanRag`, `PIR_msc_CookingPot`, `PIR_msc_CouponBundleSmall`, `PIR_msc_CouponBundleMedium`, `PIR_msc_CouponBundleLarge`, `PIR_msc_Dice`, `PIR_msc_DiceRigged`, `PIR_msc_DirtyRag`, `PIR_msc_DogCollar`, `PIR_msc_DollarBundle`, `PIR_msc_DuctTape`, `PIR_msc_FryingPan`, `PIR_msc_GlassCup`, `PIR_msc_GlassJar`, `PIR_msc_GlassVessel`, `PIR_msc_GoldChain`, `PIR_msc_GoldenCigaretteCaseEmpty`, `PIR_msc_GoldenRing`, `PIR_msc_GoldenTooth`, `PIR_msc_HemostaticClamp`, `PIR_msc_LeatherBelt`, `PIR_msc_LeatherGloves`, `PIR_msc_MagnifyingGlass`, `PIR_msc_Multitool`, `PIR_msc_NailHammer`, `PIR_msc_OldBook`, `PIR_msc_OldBoots`, `PIR_msc_OldRustyKeys`, `PIR_msc_Pencil`, `PIR_msc_PlasticWrap`, `PIR_msc_PlayingCards`, `PIR_msc_Pysanka1`, `PIR_msc_ReadingGlasses`, `PIR_msc_SafetyMatches`, `PIR_msc_SaltShaker`, `PIR_msc_Scalpel`, `PIR_msc_Screwdriver`, `PIR_msc_SharpeningStone`, `PIR_msc_Silverware`, `PIR_msc_SmokingPipe`, `PIR_msc_Socks`, `PIR_msc_SpoolOfThread`, `PIR_msc_SteelCompass`, `PIR_msc_SteelSpring`, `PIR_msc_ToiletPaperRoll`, `PIR_msc_Wrench`, `PIR_msc_WristWatch`, `PIR_msc_ZippoLighter`.
- **Mutant loot (25):** `PIR_mlt_BloodBlindDog`, `PIR_mlt_BloodBloodsucker`, `PIR_mlt_BloodBoar`, `PIR_mlt_BloodBurer`, `PIR_mlt_BloodCat`, `PIR_mlt_BloodChimera`, `PIR_mlt_BloodController`, `PIR_mlt_BloodDeer`, `PIR_mlt_BloodFlesh`, `PIR_mlt_BloodPoltergeist`, `PIR_mlt_BloodPseudogiant`, `PIR_mlt_BloodPseudodog`, `PIR_mlt_BloodRodent`, `PIR_mlt_BloodSnork`, `PIR_mlt_BloodZombie`, `PIR_mlt_BlindDogPaw`, `PIR_mlt_BlindDogTail`, `PIR_mlt_BloodsuckerHand`, `PIR_mlt_BoarEye`, `PIR_mlt_BoarHide`, `PIR_mlt_ControllerScalp`, `PIR_mlt_FleshHoof`, `PIR_mlt_PseudodogEar`, `PIR_mlt_RodentTail`, `PIR_mlt_SnorkFoot`.

**Six defined but not demonstrably reachable through any supplied patched native generator:** `PIR_dct_EchoUpgrade`, `PIR_dct_BearUpgrade`, `PIR_dct_GilkaUpgrade`, `PIR_dct_VelesUpgrade` (each appears in its own standalone, unreferenced `PIR_Detector_*Pool`); `PIR_msc_EmptyPropaneTank` and `PIR_msc_MoneyBriefcase` (only via the otherwise disconnected `PIR_MiscAllPool`). Importantly, `PIR_msc_MoneyBriefcase` also occurs in **21 explicitly authored world-actor SpawnActor CFGs**; these direct world placements are **outside the generator-only ON/OFF scope**, and blocking its unconnected generator leaf would not remove them.

**31 additional prototype SIDs never appear in direct item-generator leaves** (150 minus 119), chiefly 13 `PIR_fcp_*` faction patches, 16 `PIR_sci_*` stash-clue prototype definitions including template, `PIR_ItemTemplate`, and `PIR_msc_Empty`. Do not offer these as confirmed item-generator candidates solely because their prototypes exist. Any externally or dynamically supplied spawn source would require its own evidence.

### Second item-generation route: PackOfItemsGroupPrototypes

**Exact source:** `ItemGenerators_OC/PackOfItemsGroupPrototypes/PIR_PackOfItemsGroupPrototypes.cfg` (344,201 bytes decoded). It patches **12 existing root groups**: `Medkits`, `Stimulator`, `Drink`, `Food`, `Bandage`, `AntiRad`, `Cabinet_metal_big`, `Cabinet_metal_small`, `Cabinet_village`, `Cabinet_city`, `Cabinet_admin`, `Cabinet_science`.

- Each is `{bpatch}` with `PackOfItemsSettings : struct.begin {bskipref}`, carrying weighted `Items` across difficulty/rank groups.
- Contains **3,310 authored `ItemPrototypeSID` entries**, including **3,126 occurrences of 74 distinct PIR mod-defined item SIDs**, alongside **184 occurrences of 19 distinct non-PIR items**.
- The 74 PIR SIDs are **already within** the 119 identified `ItemGeneratorPrototypes` leaf SIDs; they are **not additional unique mod objects**. Yet they are an **independent generation route**. Blocking a PIR item solely from `ItemGeneratorPrototypes` can therefore **fail to stop its appearance** from cupboards/containers with `PackOfItemsSettings`.
- **TCD implementation must treat this as a separate generator overlay**, preserving the 19 existing items and all original constraints. A 1-click OFF must cover **both** item generation mechanisms if the selected SID occurs in both. No mass category removal and no weight/chance tuning are authorized.

### Explicit unresolved references, duplicates and CFG merge risk

1. **Missing mod-owned generator targets in the supplied four PAKs:** `PIR_StashClues_Loot` (10 `ItemGeneratorPrototypeSID` references) and `PIR_StashClues_Trade` (1 reference) have **no matching declared item-generator roots** in these decoded packages. This is a real cross-file graph gap; do **not** treat it as a valid or test-passed spawn route for `PIR_sci_*` stash clues. `Trader_T1_Guns_ItemGenerator` is the third external link target (one occurrence), and unlike those two it is confirmed in TCD's original 2.1 SELL manifest.
2. **Duplicate declarations:** `PIR_cns_CigarettesDreamRegular` appears twice with identical prototype body; `PIR_Stash_Ammo_A556E` appears twice with identical generator body. **51** native generator patch names also occur twice across different CFG files (mostly zombie targets, plus `MedicNoon_TradeItemGenerator`). Whether the engine merges/rejects duplicate named roots is **not established** by source parsing.
3. **Native COW compatibility:** PIR issues **56 `ItemGenerator {bskipref}` directives across 55 distinct native generator roots** in the ItemGenerator CFG sources (especially traders/medics, destructible and GamePass stashes). This replaces source inheritance; TCD's native 2.1 Trader Stock COW writer relies on inherited originals and pre-indexed native item leaves. A disabled native category **cannot be presumed safe** if PIR rewrites the target `ItemGenerator`. TCD's serializer does not presently have a mod-aware skip-ref reconciliation contract.
4. **TCD 2.1 verified shared roots:** `Trader_Cosnsumables_ItemGenerator` is in **510** vanilla SELL manifest leaf rows across **30** trade profiles; `GeneralNPC_TradeItemGenerator` appears in the generic NPC trader role; `MedicUniversal_TradeItemGenerator` is in **350** vanilla SELL rows across **14** profiles. PIR targets all three, as well as regional trader roots. This confirms a material intersecting source graph, **not** that these vendor stocks work together in game.
5. **Cross-mod risk with earlier audits:** PIR's `Trader_Cosnsumables_ItemGenerator` uses `ItemGenerator {bskipref}`, potentially conflicting with Sleeping Bag's `SBM` append (audit 001). PIR's GamePass stash root replacement may conflict with Repair System's `RepairKits` append (audit 004). PIR and OXA both patch `RostokTraderNato_TradeItemGenerator` but use separate named additions (`PIR_Items_Core` vs `OXA`); they *may* coexist, but load-order/merge acceptance remains untested. These are overlap risks, **not established defects**.
6. **Direct world placement and quest behavior excluded:** 21 direct MoneyBriefcase world actors, quest/item grants, pre-generated inventories and item effects are not within the approved TCD generator ON/OFF feature. Prologue/quest-specific generator roots likewise remain excluded.
7. **Do not equate CPP/CFG weights with item mass.** The mixed generator pools include original items, author-defined probabilities, counts and ranks. A correct BLOCK should suppress only the exact PIR SID in **every reachable leaf/weighted entry**, with default ALLOW leaving source CFGs unchanged. Do not remove parent pools containing vanilla items.

### Result, compatibility decision and acceptance tests

**Source status: COMPLETE for all four supplied PAK CFGs.** Parsed and cross-checked the independent new item prototype SIDs, 326 generator root declarations, 375 generator links and 12 additional weighted PackOfItems groups. **113 distinct PIR item SID candidates** have a source path from a patched native generator. The **6 disconnected leaves** and **31 prototype-only definitions** must not be exposed as positively spawnable without additional proof.

**Implementation status: NOT IMPLEMENTED.** Do not ship a blanket “disable PIR” by deleting shared generator roots, since that would remove vanilla items and risk affecting original trader, stash and corpse pools. A safe item-spawn filter must use exact item SIDs and all actual source paths (including `PackOfItemsSettings`) while preserving native pool entries; it must also handle source load order and generator inheritance.

**Required game 2.1 acceptance matrix:**
- [x] Extract/verify four PAKs and decode all 101 CFGs (including Kraken multi-block group).
- [x] Resolve item SID prototypes, source generator links, distinct native targets, mixed vanilla-vs-PIR leaf sets and disconnected paths.
- [x] Identify the separate weighted PackOfItemsGroups branch and its 12 root replacements.
- [ ] In game, compare PIR-only, TCD-only and PIR+TCD with default ALLOW; inspect native and modded drops at a named trader, Generic NPC trader, NPC corpse, mutant, stash and cabinet/medkit group.
- [ ] Specifically test `Trader_Cosnsumables_ItemGenerator` with TCD General→Medical/Food/Drinks filters and verify safe behavior of the `{bskipref}` rewritten root, including TCD copy-on-write references.
- [ ] Test item-specific OFF for one mixed-pool modded SID in both `ItemGenerator` and `PackOfItems` paths, confirming a native item in the same pool remains eligible. Test duplicate leaves/rank groups.
- [ ] Test combined PIR + Sleeping Bag, PIR + Repair System, PIR + OXA around the cited common roots and record real CFG patch precedence; no coexistence conclusion before engine tests.
- [ ] Determine why `PIR_StashClues_Loot` and `PIR_StashClues_Trade` are absent; verify whether an additional required companion mod provides them. Avoid generating dangling new references.
- [ ] Audit debug/prologue exclusions and store confirmed results before adding compact ON/OFF controls per UI catalog.

**No gameplay writer, UI control, preset storage, vanilla source manifest, or mod PAK has been changed by this audit.**
