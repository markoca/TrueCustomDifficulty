# Generic NPC Rare Trader Stock — S.T.A.L.K.E.R. 2.1 source / playtest contract

**Status:** test build/source-verified; real build, UE BPatch, independent NPC trade rolls and in-game item counts unverified.

This is **merchant sale inventory only**, not standard human equipment, corpse SmartLoot, loot generated at death, NPC pocket inventory, trader buying conditions, or world stashes. Scope is nine regular 2.1 base-game Generic NPC TradePrototypes (never story characters or templates).

## Native 2.1 TradePrototype-to-ItemGenerator mapping

| TradePrototype SID | Original stock root SID | First unused ItemGenerator category index |
|---|---|---:|
| GeneralNPC_TradePrototype | GeneralNPC_TradeItemGenerator | 2 |
| GeneralNPC_TradePrototype_Bandit | GeneralNPC_TradeItemGenerator_Bandit | 2 |
| GeneralNPC_TradePrototype_Corpus | GeneralNPC_TradeItemGenerator_Corpus | 2 |
| GeneralNPC_TradePrototype_Duty | GeneralNPC_TradeItemGenerator_Duty | 2 |
| GeneralNPC_TradePrototype_Freedom | GeneralNPC_TradeItemGenerator_Freedom | 2 |
| GeneralNPC_TradePrototype_Mercenary | GeneralNPC_TradeItemGenerator_Mercenary | 2 |
| GeneralNPC_TradePrototype_Militaries | GeneralNPC_TradeItemGenerator_Militaries | 2 |
| GeneralNPC_TradePrototype_Scientists | GeneralNPC_TradeItemGenerator_Scientists | 1 |
| GeneralNPC_TradePrototype_Spark | GeneralNPC_TradeItemGenerator_Spark | 1 |

All rows use `TradeGenerators/[0]` and source-`ConstTrue`; base `TradePrototypes.cfg` references the listed source root directly. The embedded 2.1 SELL runtime CSV independently names exactly these nine GENERIC_NPC_TRADER profiles, matching the source-root and base scope. The extra category is inserted only on each `TCD_TS_...` clone; the original root definitions and NPC actor inventories remain original.

## Exact test and normal behavior

- **OFF/unset**: no rare stock CFG emitted; normal presets/neutral economy patch expectations are retained.
- **TEST** (`TCD_GENERIC_NPC_RARE_STOCK=test`): first `Chance=1` rare *branch*, then first weighted item (100% branch chance; **not** every SID guaranteed); possible second weighted item with native `Chance=0.08`.
- **Normal opt-in** (`TCD_GENERIC_NPC_RARE_STOCK=1`): first `Chance=0.02`, then one item; 8% conditional second item when first branch succeeds. Nominal 0.16% double-slot chance, subject to engine semantics.
- **One/rarely two**: branch → two independent Chance members under `TCD_GNRS_RARE_MASTER` → type selectors with `Weight` values → type-correct item categories using one weighted item. Separate primary and secondary SID sets avoid a duplicate item between the two slots.

| Slot / pool | Exact regular item SIDs | Source category | Mix weight | Condition |
|---|---|---|---:|---|
| Main broken primary | GunObrez_SG, GunViper_PP, GunTOZ_SG, GunAK74_ST | WeaponPrimary | 58 | 0.02–0.17 |
| Main broken pistol | GunPM_HG | WeaponPistol | 16 | 0.02–0.17 |
| Main better primary | GunM860_SG | WeaponPrimary | 8 | 0.42–0.68 |
| Main common artifact | CArtifactThorn, CArtifactSlug, GArtifactBloodStone, GArtifactGravy | Artifact | 14 | n/a |
| Main attachment | RU_Silen_1, RU_ColimScope_1 | Attach | 4 | n/a |
| Second artifact | EArtifactBattery | Artifact | 50 | n/a |
| Second detector | Bear | Detector | 25 | n/a |
| Second better weapon | GunAKU_PP | WeaponPrimary | 25 | 0.42–0.68 |

These 2.1 SIDs occur in regular `GameData/ItemPrototypes.cfg`, with unambiguous item semantics; no quest-specific or named-unique item is introduced. Type selectors use native weighted `SubItemGenerator` entries (source precedents include Duty loadout weighted generator pools); the second-slot conditional uses native nested `Chance` (source precedents include the RC Bandit consumable/detector/artifact nested generators). Native item groups use `Weight` and single-item counts. These CFG conventions **support the design**, but only an actual game test can prove precise number/distribution of visible offers.

## Private generator construction and coexistence

For each affected generic trade, build `SellCloneSid("base", tradeSid, 0, originalRoot)` and reference-derive it from the exact original root. Extend only the first unused item generator category with one `SubItemGenerator` link to `TCD_GNRS_RARE_MASTER` with chance 1 (test) or 0.02 (normal opt-in). Private standalone `TCD_GNRS_...` generator nodes carry all item-selection and durability payload. Relink only that Generic NPC's `TradeGenerators/[0]/ItemGeneratorPrototypeSID` to the private root. The existing category-removal writer reuses precisely the same private clone name and patches separate group indexes, avoiding duplicate conflicting relinks. No unrelated trader profile is patched and NO changes are made to `ObjPrototypes.cfg`, NPC equipment `ItemGeneratorPrototypes` roots or `DifficultyPrototypes.cfg`.

## Local test

Launch the app *with environment variable set*, press Build Difficulty, then test a newly refreshed generic NPC merchant. For normal opt-in testing switch `test` to `1`; unset it for plain builds. On Linux/Wine:

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/
git pull
chmod +x publish.sh
./publish.sh
TCD_GENERIC_NPC_RARE_STOCK=test WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "./publish/win-x64/TrueCustomDifficulty.exe"
```

For Windows PowerShell use `$env:TCD_GENERIC_NPC_RARE_STOCK="test"` before launching the executable.

Run `python3 tools/audit_generic_npc_rare_trader_21.py` (static source/writer arrangement) and `./tests/run-wine.sh EconomyPatchContract` or the CI Windows contract. Source-BPatch and in-game activation are separate acceptance gates; 100% refers to the **bonus slot gate**, never per-item guarantee or per-actor independent generation.

### Game acceptance matrix

1. In TEST mode, newly refreshed trade stock from at least three distinct generic NPC archetypes should contain one bonus item, occasionally a second. Log actual SID, condition, actor/trade archetype and rank, plus save/refresh provenance.
2. Compare two physically distinct NPCs with the **same** TradePrototypeSID; record whether stock appears independently generated or profile-cached.
3. Test a broken weapon (2–17%) and a better one (42–68%); ensure original trade prices and buying limitations unchanged.
4. Disable an existing Generic NPC category (e.g. Ammo) and keep rare stock active. Only original Ammo offers should disappear; rare stock should remain and no duplicate trader root should be emitted.
5. Return to a fresh save after unsetting the variable and rebuilding; the special `TCD_GNRS_` definitions and TCD_TS generic redirects should no longer appear without unrelated SELL changes.
6. Kill the trader **only as a negative-control observation**, not an assertion that shop stock transfers to corpse inventory.

Do not publish the experimental opt-in as a stable gameplay feature until the tests pass. Later, after acceptance, expose a catalogued compact ON/off + chance control consistent with UI-DESIGN-LOCK.md.
