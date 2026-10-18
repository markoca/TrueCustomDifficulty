## 2026-10-19 — T1 attachment eligibility from real weapon fitting (LATEST)

**Current rule:** Newbie/T1 stash branches may select only `GunAK74_MagIncreased`, `GunUDP_MagIncreased` or `RU_GLaunch_1` as loose attachment bonus candidates. The original `GameData/ItemPrototypes.cfg` `FittingWeaponsSIDs` gives `GunAK74_ST` for the first and third and `GunUDP_HG` for the second; both weapons are among the eight audited T1 weapons in `gear-progression-ranks.csv`. Each candidate retains its existing T2 **world/trader progression tier**; T1 is a separate, explicitly source-verified weapon-compatibility exception, not a tier rewrite. The other 16 approved attachment candidates remain excluded from T1. T2–T4 keep the same tier `<= native stash rank` rule as before. Existing rank branches, 800/108 actor allowlists, two-distinct-category limit, deterministic mixer and native Chance remain unchanged. T1 attachment reward is a **loose item** and does not require a compatible weapon to coexist in the container. No extra UI control or fake drop percentage is introduced.

**Evidence:** original user-supplied `STALKER2-GameData-CFG(20261017-140154).zip`, `GameData/ItemPrototypes.cfg` SHA256 `f6b98e5931a3c936cd9f279aac23b39b45527b1995fc7008b0a7940eed790d42`. The complete 19-entry fitting list is recorded in `src/TrueCustomDifficulty/Data/stash-bonus-attachment-progressions.csv` alongside the separately curated source tier. Tests independently verify the exact three T1-compatible SIDs against the eight T1 weapon IDs. This changes the *eligibility* of T1 extra gear, not the nested-generator native syntax. A real generated CFG and freshly unpopulated in-game T1 stash must still be tested to establish whether an attachment is actually delivered. Earlier text saying "attachments T2+" or "no T1 attachment" is historical and superseded by this section.

## 2026-10-18 — Source-shaped CI fixture correction (LATEST)

Source-to-test reconciliation found a regression in the newly committed `tools/test_nested_stash_attachment_sources.py`: although the inspector had been updated for the genuine GameData singleton format, the synthetic test still authored `SubItemGenerator`, inner `Chance=0.5`, explicit count `1/1`, spaced `refurl; refkey` syntax and no original `[0]` parent. It therefore could not pass the source-pinned parser and was **not a valid CI acceptance result**. The fixture now includes the real `[0]` raw default fields and uses the correct `Attach`, inner `Chance=1`, un-authored inner counts and exact metadata syntax for 19 synthetic `Stash_Attach*` roots. Negative cases explicitly mutate source SID, category, Chance, counts, inheritance reference, and duplicate/recursive candidate fields. The original 19-entry evidence ledger is still checked independently against the production pool, not invented gameplay observations.

The original GameData file's SHA256 remains `4a06bc946eb9eaa7ff736421df7fd1bb6fe38fb91038bf5fffa005f680d64ac3` (9,101,641 bytes); original `SubItemGenerator` examples demonstrate valid native *syntax*, not successful TCD runtime drops. No production game behavior or UI was changed. **A merged source fixture is not evidence that GitHub Actions/.NET/Wine/gameplay tests have run.** Run `python3 tools/test_nested_stash_attachment_sources.py` locally or in a scheduled CI runner to verify the repaired fixture.

## 2026-10-18 — Original GameData recovered and all 19 original singleton roots verified (LATEST)

**Source actually inspected:** user's previously supplied `STALKER2-GameData-CFG(20261017-140154).zip`, entry `GameData/ItemGeneratorPrototypes.cfg` (9,101,641 bytes); raw source SHA256 `4a06bc946eb9eaa7ff736421df7fd1bb6fe38fb91038bf5fffa005f680d64ac3`. This ZIP was recovered from the user's existing file library for the audit; **it is NOT embedded or redistributed in the GitHub repository**. The 19-row reviewed, minimal derived original-source evidence is now committed as [STASH-ATTACHMENT-ORIGINAL-SINGLETON-CFG-2026-10-17.csv](../evidence/STASH-ATTACHMENT-ORIGINAL-SINGLETON-CFG-2026-10-17.csv). Independently checked all 19 root names, SID assignment, exact singleton inner path, line numbers, parent `refurl/refkey`, and original item categories and values against the decoded GameData CFG.

**Exact original authoring (19/19):** `Stash_<ItemSID> : struct.begin {refurl=../ItemGeneratorPrototypes.cfg;refkey=[0]}`, category `EItemGenerationCategory::Attach`, exactly one `ItemGenerator/[0]/PossibleItems/[0]/ItemPrototypeSID = <ItemSID>`, and explicit **inner `Chance=1`**. The singleton's **inner `MinCount` and `MaxCount` are NOT_AUTHORED**, and likewise no inner `Weight` is authored. The original template `[0]` contains the raw leaf defaults `Chance=0.f`, `MinCount=0`, `MaxCount=0`, `Category=None`; the singleton explicitly overrides category, item SID and Chance but omits counts. These inherited source raw defaults are now observable rather than labelled entirely unknown. **Do not infer from those zeros whether the engine yields 0, 1 or another count for the singleton**: the game's item generation semantics, overrides/fallbacks and nested execution remain untested in-game.

**Original native syntax corroboration:** In the same original GameData file, `EItemGenerationCategory::SubItemGenerator` and `PossibleItems[0].ItemGeneratorPrototypeSID` are used in authored generator definitions (e.g., around lines 338–355 and 2137–2157). Those native examples can omit outer counts for a nested generator candidate, so the mod's *outer structural pattern* has a source precedent. This is **not** runtime proof that any one of our 19 specific `Stash_*` singleton references will execute and yield an attachment from a fresh physical stash.

**Verification strengthened:** `tools/audit_nested_stash_attachment_sources.py` now resolves and inspects the original `[0]` parent raw fields, rejects any drift from the reviewed singleton category, explicit Chance, source-root inheritance, count omission or exactly four authored leaf assignments. Its optional `--verify-ledger` checks the exact original raw GameData SHA256 and all 19 recorded source lines/fields, and the source-independent CI fixture checks ledger/production pool parity and bad CFG syntax using synthetic data. Run:

```bash
python3 tools/audit_nested_stash_attachment_sources.py \
  "STALKER2-GameData-CFG(20261017-140154).zip" \
  --verify-ledger \
  --out audit/stash-original-nested-attachments.csv
python3 tools/test_nested_stash_attachment_sources.py
```

**Acceptance boundary:** Confirmed original CFG evidence: 19/19 singleton outer-source references plus inner `Attach`/Chance 1 and absence of authored counts. Unverified: engine interpretation of the `[0]` count defaults and actual `SubItemGenerator` nesting into freshly generated eligible physical stash inventories. **No changes to production loot, UI, control catalogs, preset values, candidate pools or protected-actor rules.** Static code inspection/independent ZIP field checks do not count as a successful in-game test.

---

# Extra Stash Gear — native nested attachment audit and game acceptance (2026-10-18)

**Status: source reachability inspected; original singleton leaf CFG values NOT independently recovered; in-game execution NOT RUN.** This document deliberately separates three evidence tiers: (1) committed original-source manifest membership, (2) emitted TCD CFG semantics, and (3) actual Unreal-generated physical inventory. A reference to `ItemGeneratorPrototypeSID` is not proof that the engine produces its nested item, nor that a `Chance=1` outer entry overrides any inner condition.

## Confirmed from the current main source manifests

- Extra Gear uses one actor/rank-private `TCD_ExtraGear_{ActorSID}_{NEVM}` clone root per applicable rank, containing up to two distinct category entries chosen with the build-time weight mixer; `ATTACHMENTS` is eligible only from native T2/Experienced upward.
- The optional outer attachment entry is `EItemGenerationCategory::SubItemGenerator` + `PossibleItems/[0]/ItemGeneratorPrototypeSID = Stash_<ItemSID>` + independent `PossibleItems/[0]/Chance = <0..1>`. No direct `ItemPrototypeSID`, count, ammo or durability field is written on this outer entry. The production code does not mutate the original singleton generator referenced by that SID.
- `stash-bonus-attachment-progressions.csv` contains 19 approved references: 16 magazines, 2 suppressors, 1 underbarrel attachment, **no optics**; rank distribution T2=8 / T3=7 / T4=4. These specific references match paths in the original-source *manifest*, but that manifest contains **no per-prototype original inner `Chance`, `Weight`, count, category or inherited effective value**. Do not infer those values.
- The earlier .NET emitted-CFG test checks the outer link, category and Chance scalars at 0%, 37%, 100% and the T1 absence. It does not evaluate the game's nested execution.
- The baseline `cfg-writer-baseline.json` is parameter-edit evidence, **not** a complete dump of all 19 original `Stash_*` generator bodies. The original GameData CFG ZIP was named in project documentation but is not embedded as a full original generator source in the current main tree.

## New source-inspection tool

`tools/audit_nested_stash_attachment_sources.py` is a **read-only local source inspector**, not a shipping runtime requirement. It accepts an original decoded `GameData/ItemGeneratorPrototypes.cfg` file, a directory containing exactly one such source or a source ZIP. It fails closed if any of the 19 audited `Stash_*` roots is missing/duplicated, if the explicitly authored `ItemGenerator/[0]/PossibleItems/[0]/ItemPrototypeSID` does not match its approved SID, if an additional authored `PossibleItems` branch exists or if the original has an unexpected nested `ItemGeneratorPrototypeSID`. It records exactly the authored inner `Chance`, `Weight`, `MinCount`, `MaxCount`, `Category` and inheritance reference metadata **without inventing omitted values**.

```bash
python3 tools/audit_nested_stash_attachment_sources.py "/path/to/STALKER2-GameData-CFG.zip" --out audit/stash-original-nested-attachments.csv
```

An `UNRESOLVED` inheritance marker or `NOT_AUTHORED` field is a **pending native-source investigation**, not a zero, one or success. Review parent `refurl/refkey` references recursively against the same original game-version dump before making any effective inner Chance/count claim. Do not multiply inner and outer percentages to invent a whole-stash drop chance. If the source structure fails the conservative singleton check, review the exact original nodes manually; do not silently expand the pool or weaken the check.

`tools/test_nested_stash_attachment_sources.py` uses synthetic fixture data (19 deliberate stand-ins, not real game values) to verify the inspector's parsing, directory/ZIP resolution and rejection of bad root names, wrong item SIDs, unexpected nested generators and duplicate fields. CI can execute this test **without** the copyrighted source CFG. No static test can confirm whether Unreal accepts `SubItemGenerator`, how/when the nested singleton runs, or whether the original container inventory was already generated.

## Manual in-game acceptance — still NOT RUN

Use clean, known mod load order and a **previously unopened/unpopulated** eligible physical stash; collect `ActorSID`, source rank branch, full emitted CFG paths, install DLC1 state, save/checkpoint, actual contents before/after, and screenshot/video. Do **not** reuse the same generated stash inventory as an independent roll. Select source-audited actor locations from `stash-gear-actor-ranks.csv` joined to the physical stash/region maps; do not substitute quest or uniquely protected containers.

| Scenario | Static emitted CFG expectation | In-game observation to record |
| --- | --- | --- |
| Master ON, Coverage 100%, Attachment 3× only, T1 | No private attachment generator or link on T1 | No T1 bonus generated by our patch; independently inspect native loot |
| Same settings, T2/T3/T4, Bonus Chance 0% | One eligible outer `SubItemGenerator` per native rank branch; outer `Chance=0` | Whether bonus attachment occurs despite zero outer Chance |
| Same settings, T2/T3/T4, Bonus Chance 100% | Same outer link; `Chance=1` | Whether an independent unattached magazine/suppressor/underbarrel item is actually delivered, whether a nested gate suppresses it |
| Same settings, Bonus Chance 37% | Outer `Chance=0.37` for each eligible attachment entry | Repeated distinct unopened eligible containers with exact rank/actor IDs; **do not infer a binomial physical-stash rate** |
| Weapon 1× + Attachment 3×, Coverage 100%, T2+ | Two distinct authored categories, one weapon and one nested attachment, independently gated by their outer `Chance` | Whether both, one or neither appears in a fresh container and whether original loot remains unchanged |
| Master OFF | No TCD private gear or approved original-weapon removal edits | Original weapon/attachment reward remains, subject to game's original native logic |
| Any setting, protected/quest/unique stash | No extra TCD actor/rank link for that actor | No TCD-sourced mutation; original special rewards preserved |

Before closing this audit, **first** inspect the original source CFG with the tool, resolve any `refurl/refkey` inheritance for the actual singleton roots, and **then** run genuine in-game cases. If source fields or engine behavior disprove the assumed outer `SubItemGenerator` validity, fix production code and document the native evidence together on main; do not apply a speculative gameplay fix. The current CI/CD, source parser, generated CFG and Wine UI tests are *separate acceptance layers*.

## Version / change scope

This phase adds the read-only inspector, synthetic regression fixture, CI invocation and an exact native-source/runtime checklist. It also fixes the obsolete `'tier < 2'` static test assertion to the current `x.Class != "Attachment" || tier >= 2` predicate; **T1 remains excluded**. No player-facing UI, UI lock/catalog, production loot writer, presets, eligibility manifests, candidate pools or current native chance values change. CI pass and game-runtime pass are never assumed from a commit.
