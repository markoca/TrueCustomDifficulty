# NPC Loot — S.T.A.L.K.E.R. 2.1 baseline / UI / CFG writer audit

**Date:** 2026-11-15  
**Scope:** ordinary repeatable human NPC loot; current GitHub `main` source and embedded `gameVersion: 2.1` CFG writer baseline.

## Evidence and limitations

- Read `src/TrueCustomDifficulty/Data/cfg-writer-baseline.json` (version 2.1), `src/TrueCustomDifficulty/Data/parameters.json`, `CfgPatchWriterService.cs`, `AggregateVisualControl.cs`, the 33-row NPC medical redirect CSV, and the existing LootSpecialGeneratorsContract / LootDirectPatchContract fixtures.
- The original source archives were previously compared for 2.1 in `docs/STALKER2-2.1-CFG-DOMAIN-AUDIT-2026-11-14.md`. That audit reports that `GameData/ItemGeneratorPrototypes.cfg` and `GameData/DifficultyPrototypes.cfg` were unchanged from the prior source dump. **This pass inspected the repository's pinned evidence, not a fresh extraction of the user's outer 2.1 ZIP.** Do not present it as a new full original-archive rebase or a live gameplay validation.
- These controls cannot guarantee an item on every corpse. NPC rank, actor generation, SmartLoot selection and upstream chance rolls remain independent of the modified leaves.

## Source ownership matrix

| UI control | CFG source and writer | Actual scope | Verdict |
| --- | --- | --- | --- |
| Extra Corpse Loot (`loot.npcCorpseBonus`) | `GameData/DifficultyPrototypes.cfg` → `EconomyDifficulty.CorpseSmartLoot.GeneralNPC`; special `setList` writer | Selects one exact extra SmartLoot list for the chosen difficulty: Novice / Easy = `NPC_Medicine_Smart, NPC_Ammo_Smart`; Stalker / Medium = `NPC_Water_Smart`; Veteran / Hard and Master / Stalker = none | Correct source mapping; runtime still unverified |
| Medical Drops (`loot.npcMedicalChance`) | `GameData/ItemGeneratorPrototypes.cfg`; six `TCD_GeneralNPC_Consumables_*` clones and 33 selected `ItemGeneratorPrototypeSID` redirects | Six normal-human role pools, 104 total consumables, **58 medical `Chance` fields**, others retained; multiplier scales only medical Chance and clamps it to 0–1 | Writer/source structurally consistent; **UI incorrectly gated it on optional corpse package** |
| Bonus Ammo Drops (`loot.npcWeaponAmmoSupply`) | `GameData/StashPrototypes.cfg`; `TCD_NPC_Ammo_Smart` clone + `DifficultyPrototypes.cfg` list substitution | 43 `MinCount/MaxCount` item entries in 24 SmartLoot caliber/rank groups, only when Extra Corpse Loot includes `NPC_Ammo_Smart` | Conditional writer and UI gating correct; not weapon-attached ammo |
| Dropped Weapon Condition (`loot.droppedWeaponCondition`) | `GameData/ItemGeneratorPrototypes.cfg`; direct `setRangeBound` | 242 exact leaves = 121 paired weapon rows in 33 repeatable-human generators. Source: 120 pairs 25–50%, one `GuardNPC_Corpus_Stormtrooper_ItemGenerator` exception 35–45%. Editing sets the chosen absolute range on all covered rows | Structural path/key parity; live drop/condition test outstanding |
| Mod Items (corpse sidecar) | `CfgPatchWriterService.ModItems.cs`, only explicitly audited third-party PAK generator branches | Percent Chance reduction on allowlisted mod additions to corpse loot; disabled by default; NOT native NPC loot | Separate scope; no legitimate conclusion from base-game baseline alone |

**Additional boundary:** normal weapon-attached ammunition is not changed by Bonus Ammo Drops. Existing weapon loadout rows and extra corpse SmartLoot generate different inventory sources. A 0x Medical Drops selection must not be interpreted as a global medicine ban while the Medicine+Ammo extra package is still selected. A bonus ammo 3x selection on default Veteran / None is intentionally dormant until the extra package is explicitly activated.

## Confirmed defect and correction

`AggregateVisualControl.IsNpcBonusSliderLocked` previously blocked **both** `loot.npcMedicalChance` and `loot.npcWeaponAmmoSupply` when Extra Corpse Loot did not contain `NPC_Ammo_Smart`. This contradicted the medical writer, which independently clones standard NPC medical ItemGenerator pools and applies its own 33 redirects. On Veteran and Master, the medical slider was greyed out under the default None state despite being an independently working source. On Stalker it was also greyed out under Water.

Correct the UI gate to **only** `loot.npcWeaponAmmoSupply`. Medical Drops remains editable under all three extra corpse package choices and all four difficulty references. Bonus Ammo Drops remains disabled for Water/None; reselecting Medicine+Ammo restores its saved multiplier. The fix changes no baseline, CFG generation, preset schema, source SIDs or group sizes.

## Existing writer behavior verified from code

1. `WriteCorpseSmartLoot` changes only the selected difficulty's `EconomyDifficulty.CorpseSmartLoot` subtree. It emits an empty assignment for None, the exact list for the other choices.
2. `WriteNpcCorpseAmmoProduction` early-returns when the selected list lacks `NPC_Ammo_Smart`. Otherwise it creates one source-referenced clone, writes 43 pairs of half-up-rounded counts, then substitutes `TCD_NPC_Ammo_Smart` in the same difficulty list. It does not change the vanilla SmartLoot prototype.
3. `WriteNpcMedicalLootProduction` only runs when its independent multiplier differs from reference. It writes complete source-backed consumable templates with **58 medical** scaled Chance fields, preserves remaining source items and quantities, and redirects 33 standard-human generator links. It has no dependency on CorpseSmartLoot.
4. The direct writer transforms `setRangeBound` into explicit Min/Max durability assignments. The embedded baseline pairs retain exact local source keys and exclude special/story actors; a single non-standard 35–45% source pair must not be silently treated as a different item family.
5. The existing `GenericLootPreGame` fixture covers medical 0x/2x on Veteran without extra SmartLoot, Novice with extra package and Novice/None, plus bonus-ammo active/inactive cases. `LootDirectPatchContract` checks 242 durability edit points. The new `VerifyNpcLootBaseline21` regression asserts these baseline inventories and that the UI locks **only** bonus ammo across all four difficulties and all three package selections.

## Residual risks / how to reproduce the reported gameplay issue

- **CFG emission is not runtime acceptance.** Test at least 1x, 0x, 2x standard Medical on Veteran / None, where bonus medicine is absent. Use newly spawned ordinary humans and record the source generator if possible. Don't use previously rolled corpses.
- Compare Novice / Medicine+Ammo vs Novice / None with Medical at 0x, to distinguish ordinary medical from the extra package. Medicine seen in the first case is not evidence of a broken normal-medical slider.
- Compare Bonus Ammo at 0x / 1x / 3x only while Medicine+Ammo is active, and separately verify Water/None remains dormant. Check loose ammo stacks, not ammunition already loaded in weapons.
- Compare dropped weapon condition at two distinct changed MIN/MAX ranges on newly generated ordinary repeatable-human NPCs; exclude named story NPCs, mutants and special corpse override actors.
- With third-party mods enabled, test the base-game NPC controls separately from corpse MOD ITEMS; validate installation hashes and patch load order. A structurally correct CFG can still be overridden by another mod at runtime.
- Re-run `dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release`, `dotnet run --project tests/LootDirectPatchContract/LootDirectPatchContract.csproj -c Release`, then the normal `./publish.sh` and Wine visual test. These commands are required validation, not claimed completed by the connector audit.

**Audit verdict:** one confirmed UI gating error fixed; no additional verified wrong source path/count found in the examined four native NPC controls. Static correctness does not prove corpse loot outcomes in game.
