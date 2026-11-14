# Generic NPC Rare Trader Stock — S.T.A.L.K.E.R. 2.1 active UI/source contract

**Current 2026-11-14 behavior:** Generic NPC tab in **Economy → Trader Stock** has a **GEAR** checkbox after the five vanilla stock categories. When checked, a compact **CHANCE** slider appears immediately beneath it. When unchecked, no bonus rare-stock CFG edits are produced. These are two **persisted optional preset sidecar inputs**, not two fabricated 2.1 vanilla source parameters: the fixed 359-source catalog, factory preset values and original 31 SELL category IDs remain intact. The legacy `TCD_GENERIC_NPC_RARE_STOCK` environment flag is **retired**.

- GEAR default: **OFF**. CHANCE retained default: **2%**; adjustable **1–100%**, **1 percentage point per step**. Unchecking GEAR hides but retains the chosen chance for next use.
- **100%** is the in-app test setting: the rare branch is enabled for each of the nine generic stock profiles. It is **not** 100% per item SID or a guarantee that a particular living NPC has independent randomized stock.
- At any enabled chance, the first branch selects **one** weighted extra item. An independent native **8% second-slot** roll can add a different second item, subject to game-native execution; pending in-game acceptance.
- This ONLY affects **TradePrototypes.cfg** and private **ItemGeneratorPrototypes.cfg** clones. NPC equipment, corpse loot, world item containers, quest characters, buying restrictions and non-generic traders are unchanged.
- GEAR OFF + default vanilla SELL categories emits **zero extra CFG files**, and turning GEAR OFF after a test build fully removes its generated effects on the next Build Difficulty run.
- Save/Load/Reset All/Reset Economy preserve or restore the checkbox and chance. Older factory/custom presets missing both optional fields load with GEAR OFF and chance 2%.
- The same private cloned source generator is shared with existing Generic NPC OFF-category choices (no edit to original source stock roots).

**Source evidence:** verified against user-supplied original 2.1 CFG dump `STALKER2-2.1 cfg dump.zip` and embedded `trader-sells-2.1-runtime.csv`. BPatch engine behavior, precise one/two item outcome, actual refresh timing, damage percentages and actor-independent availability still require in-game testing.

## Exact source trade profile list

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

All nine roles are base-game, source `TradeGenerators/[0]`, and the new bonus link occupies the listed next-free category index in a per-trader `TCD_TS_...` clone.

## Source item pool, weights and durability

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

Broken weapons are 2–17% condition; rarer better guns are 42–68%. The main weighted selector assigns broken primary 58%, broken pistol 16%, better primary 8%, artifact 14%, attachment 4%; the second-slot selector assigns Battery 50%, Bear detector 25%, AKU weapon 25%. Different SIDs in primary/secondary pools prevent a duplicate bonus item. Pool selection is based on native Weight, not independent 100%-Chance rolls per candidate.

## Local build and in-game test

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/
git pull
chmod +x publish.sh
./publish.sh
WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "./publish/win-x64/TrueCustomDifficulty.exe"
```

Select **Economy → Trader Stock → Generic NPC**, tick **GEAR**, set **CHANCE to 100%**, and press **Build Difficulty**. Check several newly refreshed generic NPC traders; each should offer one and occasionally two bonus items if the BPatch generator semantics work as expected. Test 2%, 37%, the OFF state and combined vanilla Ammo OFF. Verify stock contents and condition, no corpse-loot effect, and whether stock is independently generated for two NPCs of the same profile. `tests/EconomyPatchContract` and `tests/PresetSaveContract` provide deterministic application-side checks; live game tests remain mandatory.
