## 2026-11-14 — 2.1 Trader Stock runtime activation

The 2.1 manifest is the sole embedded SELL inventory source, verified by SHA256 `9585c5b21bc13610dbc448650b2b053d582c852911782976076f5657f741e587`. The 86 playable profiles, 2,939 offers, 164 distinct item SIDs and 260 role/item selections retain their preset identifiers. Two DLC medic profiles reference base-game root generators; their source-resolved scopes are mandatory. The Suslov SQ14 ammo pool has two parent-link slots and both must be rewritten during clone/relink. The five formerly nonzero TraderNPC entries are now native zero-chance entries. In Rostok, AVOG is both Ammo and Artifacts in separate native source paths; the item-selector presents one AVOG under Ammo but a per-SID OFF removes both, while category-only OFF follows each path's native category. The 2.0.5 source remains in docs for historical audit only, not as runtime fallback.

The original 31 category toggles, compact five-role editor and `TraderItemOverrides` preset field are unchanged, so the UI design catalogs do not require layout changes. Static audit: `python3 tools/audit_trader_21_runtime.py`. Windows build, CFG writer patch and in-game stock refresh remain separate acceptance gates; do not equate static auditing with game behavior.

---

## 2026-11-13 — CURRENT TRADER STOCK item-offer editor (supersedes all BUY/SELL matrices)

Economy → Trader Stock is ONE compact 820-DIP stock editor, not a 5×9 BUY/SELL matrix. Choose one of five source merchant roles (General, Medic, Technician, Bartender, Generic NPC); filter by original stock category, search by source item SID or readable spaced SID, and independently retain/remove each original item offer. The scroll list is bounded to 365 DIP and 31-DIP item rows. Items are grouped by category when ALL is selected. The verified embedded 2.0.5 merchant manifest yields 164 distinct item SIDs and 260 role/item pairs (including NightVision and Binoculars); DLC1-only and original 0%-chance candidates are annotated. 0%-chance entries do not count as available offers. Source SHA256 is verified. Spaced SIDs are not claims about localized game names.

The TraderItemOverrides property is a validated deterministic sorted semicolon-separated set of roleId|itemSid pairs. It is saved as the optional traderItemOverrides field in preset JSON, so existing 359-parameter catalogs, writer baseline, 12 factory presets, and old preset files remain compatible and load with an empty item selection. Editing an item generates private per-trade generator clones and removes only that exact source SID path, preserving original stock chances, rank gates, quantities, refresh rules and other traders' inventories. Item selections do not create new stock, and an original 0%-chance entry is not magically made available by checking it. The original 31 seller-category toggles remain in presets and CFG writer; if a legacy category was disabled, enabling one of its items converts the category to individual exclusions for its remaining items. The 40 old BUY limitations are no longer shown; their saved changes still apply and a warning with RESET BUY RULES allows explicit clearing rather than silent data loss.

The visible catalog anchor moves from economy.traderBuys.general.weapon to economy.traderSells.general.weapons, keeping 359 total / 184 visible / 175 hidden. The existing TraderBuysMatrix class and XAML instance name are retained as internal compatibility names but now render the stock list. The active XAML trigger is IsTraderSellsAnchor; no second trader table exists. This is a source-backed selective removal editor, not a general-purpose generator authoring system. WPF compilation, Wine rendering and in-game stock refresh remain to be verified locally.

# Trader Sells — 5 × 10 stock-category matrix (2026-11-13)

**Implementation: source and production writer integrated; game-runtime acceptance pending.** The independent **Trader Buys** matrix controls what the merchant accepts from the player. **Trader Sells** controls categories offered *to the player* by changing merchant item generators only. These are not interchangeable systems.

## UI: compact 5×10 board

Five seller roles (General, Medic, Technician, Bartender, Generic NPC) and ten genuine gameplay categories: Weapons, Armor, Attachments, Ammo, Medical, Food, Drinks, Artifacts, Detectors and Grenades. These are item-SID categories, **not** raw `EItemGenerationCategory` blocks (which mix unrelated items in Artifact/Junk/Attach). There are **31** independently editable ON/OFF cells, sourced from the real profiles; the other **19** cells show a read-only dash because the role has no original candidate stock of that type.

- **Checked (default):** preserve all original merchant stock generation, random availability, rank gating and restock.
- **Unchecked:** suppress the entire chosen category for that merchant role by removing its item entries from a private per-merchant inherited generator graph.
- A clicked cell uses its **own reference-relative modified border**; another cell never lights up the top-left anchor.
- Deliberate two-line column headers; one 920-DIP-wide bounded card; no overflowing all-caps text, duplicate labels, extra Vanilla badges, or unnecessary standalone parameter rows.

Role inventory by count of active category cells: General 10; Medic 4; Technician 6; Bartender 6; Generic NPC 5 = **31 real parameters**. In particular, Technician Weapons/Ammo/Medical/Food/Drinks are **DLC1-only**; when DLC1 is absent those toggles are retained in presets but cause no output or false applied-edit count. An empty source combination never becomes an editable checkbox.

## Full source provenance

Authoritative **original STALKER 2 2.0.5** source: [trader-offers-source-rows.csv](trader-offers-source-rows.csv), SHA-256 `6b06e7f3b6b4844e4043f4f620f7b8a364ed7623728027315ad90e7d74c2e05f`. Source archives, independently checked against previous extraction:
- Base-game CFG ZIP SHA-256 `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1`
- DLC1 CFG ZIP SHA-256 `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6`

**2,939 fully traced vendor-to-leaf paths**, **86 production TradePrototypes (68 base, 18 DLC1)**, **62 leaf generator identities**, **608 unique item leaf paths**. Some source offers traverse an outer generator → inner stock pool; the CSV contains the exact node path for each leaf and `outer_links` for each nested edge. Original food/drinks/medical categories have distinct item SIDs even though all count as native Consumables for buying restrictions. Protect 32 possible NightVision/Binoculars source paths and the native source properties not involved in the selected category; there is no Night Vision/Binoculars toggle in this matrix.

## CFG writer: merchant-only copy on write

Files: `Services/TraderSellsPolicy.cs`, `Controls/TraderSellsMatrix.cs`, `Services/CfgPatchWriterService.TraderSells.cs`, `Data/cfg-writer-baseline.json`, and the exact source CSV embedded via `TrueCustomDifficulty.csproj`. Every build validates the embedded CSV SHA-256, its physical 2,939-row/86-profile/62-generator/608-leaf census, 68/18 source split, role identities, source paths and nested-edge shape before applying edited cells. Unexpected source changes fail closed.

For each **affected trade profile** (not each UI cell globally):
1. Create a deterministic, private `TCD_TS_...` root `ItemGeneratorPrototypeSID` **in the original generator's scope**, referencing the original root by `{refurl=../ItemGeneratorPrototypes.cfg;refkey=OriginalSid}`.
2. For entries physically on that root, suppress only item entries whose exact gameplay category is now OFF using `PossibleItems/[index] : removenode` below `{bpatch}` ancestors. Other item entries, rank gates, Chance/Weight, durability and count fields inherit unchanged.
3. For entries coming from a **nested source pool**, create a second private inherited pool clone, remove the selected leaves **there**, and rewrite the merchant root clone's exact `ItemGeneratorPrototypeSID` link to that private target. Repeated references to the same source pool under a merchant reuse the appropriate clone.
4. Patch only that merchant's `TradePrototypes.cfg` `TradeGenerators/[0]/ItemGeneratorPrototypeSID` to the private root. The existing **Trader Buys** `BuyLimitations` and **Minimum Sellable Condition** writes merge on the same `TradeGenerators/[0]` parent without interference.
5. **Never mutate an original shared definition**, including `Trader_Cosnsumables_ItemGenerator` (quest/scripted), `BartenderUniversal_TradeItemGenerator` (named NPC), `Trader_NatoAmmo_ItemGenerator` (scripted), or T1–T4 pools shared between merchant roles. One merchant may be restricted without changing any other merchant's stock.

Only source categories OFF in the preset trigger a production patch. A full-neutral preset creates no stock copies or trader-stock patches. Source `PlayerRank`, Chance, Weight, original generated quantities, inventory refresh conditions, monetary modifiers and original merchant buy-acceptance rules are preserved. The user must allow actual stock regeneration in-game; existing merchant inventory can remain cached.

## Audit and acceptance

- Catalog **359** source IDs (328 previous + 31 sellers); **185** catalog-visible anchors (183 old + Trader Buys + Trader Sells); **174** hidden source IDs (105 old + 39 Buy backings + 30 Sell backings).
- All twelve factory presets contain **359** source-backed values each, with the 31 new seller cells ON/neutral; older user presets missing them load at vanilla defaults.
- `EconomyPatchContract` includes neutral output, five-role selective removal, DLC-only gating, and coexistence with Buyer/condition patches (47 scenarios).
- `AllControlsPatchContract` exercises **342** live editable source IDs. `tools/audit_trader_sells_implementation.py` checks the frozen source identity, 31 real cells, scope, private-copy structure, untouched cross-context originals, and preset coverage; included in the pre-release static suite.

**Not a runtime guarantee:** .NET/WPF compilation, actual BPatch syntax and inheritance in the installed Unreal version, in-game vendor refresh, DLC1 runtime results, and interactions with third-party stock mods still need local testing. Source data is **2.0.5**; later game CFG versions require re-extraction before claims of compatibility.

