# MOD AUDIT — external item and generator compatibility (S.T.A.L.K.E.R. 2.1)

**Canonical incremental audit.** Append a numbered mod entry for each supplied mod; retain source evidence, unresolved questions, and any in-game acceptance results. The objective is to design mod-aware generation without breaking the existing game-2.1, source-exact True Custom Difficulty (TCD) writers. This file records observations and proposed integration contracts, **not an assertion of shipped mod support**.

## Audit protocol

For every new mod, record:

1. **Provenance:** supplied archive filenames, sizes, hashes, mod name/version if evidenced, completeness of each container set (PAK/UTOC/UCAS).
2. **Actual CFG evidence:** package mount, exact internal CFG paths, added root SIDs, `{bpatch}` targets, `refurl/refkey` inheritance, item SIDs, inventory properties, and all generator nodes (`Category`, `PossibleItems`, Chance, Count, Conditions, Weight).
3. **Acquisition routes:** trader stock, world/quest pickup, stashes, corpse loot, quest ItemAdd, and any asset or Blueprint-only route. Record **observed** and **unverified** separately.
4. **2.1 cross-reference:** join generator SIDs and parent links to the authoritative game-2.1 manifests; distinguish base, DLC1, and external-mod provenance; do not confuse a shared generator with individual traders.
5. **TCD writer interaction:** whether existing controls modify direct leaves, shared roots, inherited clones, or only fixed native SID inventories; note independent mod-added branches that TCD does not enumerate.
6. **Compatibility and safety:** conflicting patches, load-order unknowns, absent dependencies, runtime selection chance, and effects on item types or categories. No assumptions about runtime behavior from CFG syntax alone.
7. **Status / next evidence:** complete or partial extraction, proposed classification, tests required, and explicit support decision.

Use source-exact names, including source misspellings. Never quietly map `Junk` to `Medical`, `Food` or `Drinks` just because the item inherits a consumable template. Never assume a leaf `Chance = 1` guarantees an item in stock: ancestor selection, roll and refresh semantics must be established in-game. Do not mutate original mod files.

### Audit index

| ID | Mod | CFG item SID | Generator hook | TCD compatibility status |
|---|---|---|---|---|
| 001 | Sleeping Bag (packaged plugin version `1.0`) | `SBM_Item` | `Trader_Cosnsumables_ItemGenerator` → new `SBM` category | **Source audited, runtime unverified; direct native category filtering does not cover this new leaf** |
| 002 | ZoneWatch / ZST — Zone Standard Time (NewContent PAK) | Not yet verified (item CFG Oodle) | `ItemGeneratorPrototypes/zZST_FieldWatch110.cfg` (Oodle) | **Container/index audited; exact generator and item definitions pending decompression** |

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
