## 2026-10-20 — RC candidate cleanup: diagnostic profile retired from app

The **RC - CFG Signal** diagnostic is no longer shipped, embedded or auto-created by the application. The preset picker contains the **12 official factory roles** plus any unrelated user profiles. During the first run after updating, the exact byte-for-byte stock diagnostic file is deleted. A user-edited diagnostic file is **moved without overwriting** into `Presets/Archived/` (with a numbered name if necessary), so it disappears from the picker without losing user changes. The 13-signal CFG contract remains in `tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json`, embedded **only in the test assembly**, not in the published EXE. This is release-candidate cleanup, **not** proof of a passing post-change build, Wine UI test, game-load test or in-game effects.

# RC - CFG Signal — controlled in-game mapping smoke test

**Target:** S.T.A.L.K.E.R. 2 game/source baseline **2.0.5** (confirm the installed game version before testing).  
**Fixture:** historical 13-signal *test-only* diagnostic in `tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json`; **not** a shipped preset or one of the 12 factory roles.  
**First use:** Friday, 2026-10-16, on an expendable test save / fresh New Game.  
**Verdict rules:** generated BPatch != game-loaded BPatch != actual observable gameplay effect.

**Praktični in-game Markdown checklist (13 checkboxova za VS Code):** [RC-CFG-SIGNAL-GAMEPLAY-CHECKLIST.md](RC-CFG-SIGNAL-GAMEPLAY-CHECKLIST.md). Označavanje je ručno; CFG/CI prolaz nije automatska potvrda gameplay efekta.

## Availability, reproducibility and safety

**Retired from the application on 2026-10-20.** The old auto-seeded diagnostic does not appear in the preset picker, and the production EXE contains no embedded RC fixture. Its 13 extreme inputs are retained solely at `tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json` for isolated automated CFG writer tests. On upgrade the untouched stock file is deleted; edited copies are backed up under `Presets/Archived/` with no overwrite. A player must not rely on automatic seeding. The steps below document optional historical A/B testing and do not imply the latest RC has passed gameplay checks.

The preset stores only the 13 test inputs below, `referenceDifficulty=Stalker`, and no override mode. Loading a sparse preset resets all other controls to their authored Stalker/vanilla references. This prevents unrelated tuning from masking the comparison. The input JSON format remains v1; editor Save creates a complete snapshot if you choose to save it.

**This is intentionally not a playable difficulty preset**: it includes almost-free enemy-gun combat, increased player damage, doubled player pools, larger A-Life caps and 100% eligible Blind Dog part Chance to make source misrouting obvious. Keep ordinary saves and production mods separate; do not use for balancing.

## Source-backed smoke matrix (13 independent test signals)

| UI source ID | Stalker/source reference | RC value | Authored CFG target | In-game comparison / limitation |
| --- | ---: | ---: | --- | --- |
| `economy.startingMoney` | 0 coupons | **100,000** | `CoreVariables.cfg` / `Global.PlayerStartingMoney` | Start **two NEW games** without vs with the mod. Existing saves are not a valid check. If startup scripts override the balance, record FAIL/UNKNOWN rather than forcing a conclusion |
| `stamina.maxCarryWeight` | 80 | **160** | `ObjWeightParamsPrototypes.cfg` / `DefaultWeightParams.MaxInventoryMass` | Inspect default inventory capacity after loading; do not confuse an artifact/armor buff with the base limit |
| `stamina.maxSP` | 100 | **200** | `ObjPrototypes.cfg` / `Player.VitalParams.MaxSP` | Compare measured stamina capacity / time-to-empty with identical load, actions and buffs |
| `health.maxHP` | 100 | **200** | `ObjPrototypes.cfg` / `Player.VitalParams.MaxHP` (+ `HealthMechanics.FullHP`) | Compare measured maximum health, not the visual percentage bar alone; mapping still carries `runtime_test_required` |
| `health.medkitHealing` | 70 HP | **140 HP** | `EffectPrototypes.cfg` / `MedkitHealing3.ValueMin/ValueMax` | Injure the same player to a known amount below the raised cap; compare one ordinary medkit on both builds |
| `survival.waterInstantStaminaRestore` | 38% | **100%** | `EffectPrototypes.cfg` / `WaterStaminaInstant.ValueMin/ValueMax` | Drain stamina, drink one Water, compare instant recovered amount. Keep the 200 SP cap identical on both sides of this individual A/B test |
| `combat.npcWeaponDamage` | 0.85 | **0** | `DifficultyPrototypes.cfg` / `NPCCombatDifficulty.NPC_Weapon_BaseDamage` | Compare a single human-NPC firearm hit with identical armor/distance. Melee, anomaly and other damage channels may still hurt |
| `combat.playerWeaponDamage` | 1 | **2×** | `DifficultyPrototypes.cfg` / `EnvironmentDifficulty.Weapon_BaseDamage` | Same weapon, ammunition, target and hit location; compare time/shots-to-kill (armor may affect the observed ratio) |
| `environment.anomalyDamage` | 1 | **0** | `DifficultyPrototypes.cfg` / `EnvironmentDifficulty.Anomaly_Damage` | Compare one known damaging anomaly at same exposure, without treating status buildup, radiation or separate effect channels as proven absent |
| `health.vodkaRadiationRemoval` | 50 points | **100 points** | `EffectPrototypes.cfg` / `VodkaAntirad3.ValueMin/ValueMax` (negative delta via `negateSet`) | Establish a repeatable radiation level, use one Vodka, compare the removed amount; keep all other treatments fixed |
| `loot.mutant.blindDogChance` | 15% | **100%** | `ItemGeneratorPrototypes.cfg` / `BlinddogLootGenerator.PossibleItems[0].Chance` | Several **eligible** Blind Dog kills and loot checks. If another loot gate/exclusion applies, do not claim guaranteed game-wide drops |
| `alife.mutantSpawn.blindDog` | 1×: **4/6/8/12** | **3×: 12/18/24/36** | `ALifeDirectorScenarioPrototypes.cfg` / four `BlindDog` `MaxCount` leaves | **Maximum permitted group sizes**, not a promise that packs of 36 will actually spawn. A-Life spawning is runtime-pending beyond integer CFG verification |
| `equipment.weaponWearPerShot` | 1× | **2×** | `CharacterWeaponSettingsPrototypes.cfg` / player `DurabilityDamagePerShot` | Fire the same number of rounds with the same player weapon. Compare condition loss, without conflating NPC gun condition or jam frequency |

All 13 are source-mapped native gameplay parameters. The **only** changes in this preset are the numbers above. Some IDs are edited through existing visual-group controls (Medkit, Vodka and A-Life), not standalone slider rows; all three still map to independent native CFG fields. None is an invented, hidden-only pseudo-source.

## Test procedure for a defensible PASS

1. Record exact Git SHA, executable `BUILD-INFO.txt`, game version, selected Base difficulty **Stalker**, and active mods. Disable competing CFG/gameplay mods; back up your saves.
2. Run `python tools/audit_rc_signal_preset.py` and `python tools/audit_final_cfg_mapping.py --out audit/final-cfg-mapping` on this SHA. These verify metadata/mapping and cannot prove the game applied it.
3. On Windows with .NET 10, run `dotnet run --project tests/RCSignalPatchContract/RCSignalPatchContract.csproj -c Release -- --out audit/rc-signal-patch-fixtures`. Check staged prototype `{bpatch}` and global `cfg_patch` files. A Windows runner is required for the WPF project.
4. On Linux, `./publish.sh`, then launch through `WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "./publish/win-x64/TrueCustomDifficulty.exe"`. Select **RC - CFG Signal** from the preset picker and verify **all 13 values** appear unchanged.
5. Build the mod using user-provided `repak.exe` and `retoc.exe`, then verify the three nonempty `zzzzz_TrueCustomDifficulty_RC_-_CFG_Signal.{pak,utoc,ucas}` files. For an actual **container read-back**, unpack the `.pak` using a compatible repak tool, verify source-relative paths/entries against staged fixtures, and keep unpack logs. Their mere existence is *not* a container-format read-back PASS.
6. Use one vanilla/Stalker control save and one modded/Stalker state wherever the same save is valid. For each row, preferably make a **single-variable A/B build**: keep the RC signal profile loaded, reset ONLY that test parameter to Stalker reference (right-click), Build/Install and compare; then restore its RC value. A new save is mandatory for Starting Coupons. Run controlled trials with time-of-day, inventory, rank and weapon held constant where relevant.
7. Record per row: `source present`, `staged BPatch correct`, `PAK read-back correct`, `game loaded`, `effect observed`, along with screenshot, repeatable save, or video evidence. Mark `PASS` only for the layers actually checked; otherwise `FAIL` or `NOT TESTED`. One visible success **does not** prove all 248 mappings.

## Highest-value first checks

**First five quick signals:** 100,000 starting coupons (new game), 160 default carry capacity, 200 maximum stamina, 140-point Medkit healing, and Water's 100% instant stamina restore. All have clear source numbers; if game display/scaling differs, retain logs rather than guessing.

**Then source-sensitive checks:** 0 NPC firearm coefficient, 0 global anomaly multiplier, 100-point Vodka anti-rad, 100% eligible dog part chance, and four real A-Life integer caps. These distinguish difficulty CFG, status effects, item-generator loot and Director-cap routing rather than just one successful Player prototype.

**Follow-up:** 200 HP, 2× player base damage and 2× weapon wear. Preserve the same save/equipment baseline. A-Life cap checks and loot-generator behavior should remain marked runtime-pending until actually observed.

## Per-candidate runtime result log (fill after real tests)

Candidate SHA: `NOT RECORDED`  
Game build: `NOT RECORDED`  
Test save / New Game identity: `NOT RECORDED`  
Other gameplay/CFG mods: `NOT RECORDED`

| Signal | Writer CFG | Packed PAK read-back | Game behavior | Evidence / caveat |
| --- | --- | --- | --- | --- |
| Starting Coupons 100,000 | NOT TESTED | NOT TESTED | NOT TESTED | Fresh New Game required |
| Carry Weight 160 | NOT TESTED | NOT TESTED | NOT TESTED | |
| MaxSP 200 | NOT TESTED | NOT TESTED | NOT TESTED | |
| MaxHP 200 | NOT TESTED | NOT TESTED | NOT TESTED | Runtime-pending original mapping |
| Medkit 140 HP | NOT TESTED | NOT TESTED | NOT TESTED | |
| Water Instant +100% SP | NOT TESTED | NOT TESTED | NOT TESTED | |
| NPC Gun Damage 0 | NOT TESTED | NOT TESTED | NOT TESTED | No claim for melee/other damage |
| Player Gun Damage 2x | NOT TESTED | NOT TESTED | NOT TESTED | |
| Anomaly Global Damage 0 | NOT TESTED | NOT TESTED | NOT TESTED | Status effects may persist |
| Vodka Radiation Removal 100 | NOT TESTED | NOT TESTED | NOT TESTED | |
| Blind Dog Part Chance 100% | NOT TESTED | NOT TESTED | NOT TESTED | Eligible generator only |
| Blind Dog ALife cap 3x | NOT TESTED | NOT TESTED | NOT TESTED | Cap is not guaranteed spawned group size |
| Player Weapon Wear 2x | NOT TESTED | NOT TESTED | NOT TESTED | |

A failed field at any level is **not** an automatic verdict about every other field; reproduce it as a one-setting patch, keep the generated CFG and record whether the game loaded the patched source. Leave any unsupported claim as `NOT TESTED`.

## Release gate

The `RC - CFG Signal` preset is a **diagnostic instrument**. Its schema, embedded C# writer output and static source checks are automatable; installation and S.T.A.L.K.E.R. 2 runtime effects are manual. No PASS for the full gameplay matrix is claimed by committing this file. If the current game version differs from the fingerprinted 2.0.5 CFG source archive, revalidate the original source mapping before attributing any failure or success to this mod.
