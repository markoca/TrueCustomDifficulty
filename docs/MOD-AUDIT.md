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
| 003 | Point of No Return — Backpacks Pack | Not decoded (Oodle) | Mod-owned ItemGeneratorPrototypes.cfg plus native-path zBackpackGeneratorPatch.cfg | **Container + plaintext audited; generator paths and item SIDs pending Oodle extraction** |
| 004 | Point of No Return — Repair System | Item prototypes present, SIDs not decoded (Oodle) | Mod-owned `RepairKitItemGenerators.cfg` plus native-path `zRepairKitGeneratorPatch.cfg` | **PAKs and plaintext verified; generator and actual kit identities pending Oodle; additional CoreVariables durability interaction** |

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
