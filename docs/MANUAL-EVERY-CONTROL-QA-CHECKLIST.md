# True Custom Difficulty — kompletna ručna QA checklista

## 2026-10-16 — New stash systems (12 additional active source controls)

**Live inventory is now 260 catalogued / 250 instantiated ParameterViewModel sources (160 exposed anchors / 90 backing composite members, 10 non-instantiated)**, replacing the preceding 248/238 historical checklist snapshot. The original individual 238 rows below remain valid for previous controls. In addition, test all 12 new rows using the same **V (visual), I (interaction), P (preset), B (CFG build), G (gameplay)** test axes.

### STASH CONTENT — 0..3x, step 0.25, 1x untouched

- [ ] Ammo — `loot.stashMixAmmo`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Medical — `loot.stashMixMedical`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Food — `loot.stashMixFood`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Drinks — `loot.stashMixDrinks`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Grenades — `loot.stashMixGrenades`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Coupons — `loot.stashMixCoupons`: V[ ] I[ ] P[ ] B[ ] G[ ]

### EXTRA STASH GEAR — 0 OFF, 1 RARE, 2 MODERATE, 3 FREQUENT

- [ ] Basic Weapon — `loot.stashGearCheapWeapon`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Basic Armor — `loot.stashGearCheapArmor`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Standard Weapon — `loot.stashGearMediumWeapon`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Standard Armor — `loot.stashGearMediumArmor`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Rare Weapon — `loot.stashGearExpensiveWeapon`: V[ ] I[ ] P[ ] B[ ] G[ ]
- [ ] Rare Armor — `loot.stashGearExpensiveArmor`: V[ ] I[ ] P[ ] B[ ] G[ ]

Additional gates: check source `Weight` relative odds vs bounded source `Chance`, rank gates, no quest/scripted changes, Tier+Ammo+Coupon/Grenade composition, exact pristine-only factory preset updates. Do **not** mark game behavior PASS on CFG diff alone. Full mechanics: [STASH-CONTENT-GEAR-IMPLEMENTATION.md](STASH-CONTENT-GEAR-IMPLEMENTATION.md).

**Historical checklist below uses the prior 248/238 snapshot; its untested status remains correct.**


**Kataloški snapshot:** `main` `c5356f011070` (2026-10-16). Autoritet za izgled: [UI-DESIGN-LOCK.md](UI-DESIGN-LOCK.md) i [VISUAL-CONTROL-SYSTEM.md](VISUAL-CONTROL-SYSTEM.md). Autoritet za izvorne min/max/step i source ID: `src/TrueCustomDifficulty/Data/parameters.json` iz istog commita.

**Pokrivenost:** 248 source definicija; **238 aktivnih ParameterViewModel izvora** (od čega 148 glavnih UI sidara i 90 člana složenih panela); 10 izvornih ID-eva bez aktivnog editora. Svaka aktivna stavka ima svoju checkbox liniju. Nijedna ovde nije prijavljena kao stvarno testirana.

## Šta znači kompletiran checkbox

- Za **svaki pojedinačni parametar** prođi **V** = vizuelno (naziv, jedinica, trenutno stanje, compact/legible, bez clippinga/dupliranja); **I** = interakcija (miš, track/drag/knob/keyboard, native clamp i step, hover, reset, klikabilna marker skala); **P** = Save / promena / Load preset, identično stanje; **B** = Build + provera da je modifikovan deklarisani CFG path a ne drugi izvori; **G** = kontrolisani gameplay test, samo tamo gde ima smisla.
- Između V/I/P/B/G ostavi status zasebno. Primer reda: `V[✓] I[✓] P[ ] B[ ] G[N/A]`. Glavni `- [ ]` čekiraj tek kada je sve za tu stavku završeno ili jasno obeleženo kao N/A/Blocked. Ako neki rezultat padne: upiši FAIL i dokaze.
- **SIDRO** = glavni normalni editor ili vezana specijalna grafička kartica. **ČLAN** = aktivan source uređiv unutar složenog vizuelnog editora; izostanak samostalne kartice je nameran, ali ne i nedostupna vrednost u parent kontrolama. `exposeInUi:false` nije isto što i source isključen iz runtime modela.
- `raw min/max/step` su **izvorne vrednosti** iz JSON kataloga; UI ih može prikazivati kao x, %, HP, kg, s, RAD ili poseban ordinal. Za `runtime_test_required` nije dovoljno da CFG diff prođe — gameplay ostaje pending dok ga neko zaista ne proveri.

## 0. Preflight / proveravanje UI porodica

- [ ] `main` checkout i `git pull` uspešan; build `./publish.sh` bez grešaka, `BUILD-INFO.txt` odgovara current HEAD, nema zaostalog Wine procesa.
- [ ] Wine startup bez WPF exception-a, prozor i tabovi se otvaraju, toolbar i reference difficulty se vide.
- [ ] Na 1600×900 i 1024×576 proveriti scrollbar, minimalnu širinu, nema teksta koji izlazi iz svojih granica ili rasteže kartice preko cele širine.
- [ ] Slideri koriste konzistentan STAMINA USAGE raspored teksta/žute brojke/tracka, čitljiv min/max, hover i klikabilan marker.
- [ ] Svi 270° knobovi imaju isti font naslova i belih donjih granica, ujednačenu udaljenost; centralni readout zlatan samo kod modifikacije, donji izvedeni broj siv uz žute samo promenjene cifre.
- [ ] Custom chart/radar/timeline: hover na stvarnoj ručki (ne na dekoraciji), native snapping, korisni opisi, bez lažnih kontrola i preview-ova koji glume editore.
- [ ] Checkbox veličine/hover dosledni; OFF/ON nemaju redundantne duple natpise; parent disable blokira stvarnu child interakciju.
- [ ] Min/max range dva handle-a rade nezavisno, minimum nikad ne prelazi maksimum, reference marker vraća izabranu teškoću.
- [ ] Neizmenjene Vanilla/Base vrednosti nisu vizuelno istaknute. Samo stvarno izmenjeni izvorni brojevi i vezani derived brojevi postaju žuti.
- [ ] Test uradi sa Novice, Stalker, Veteran i Master kao reference difficulty; reset vraća odgovarajuću bazu, ne prethodnu izabranu težinu.
- [ ] Sačuvati kopije personal Presets, Output, user-settings i zaseban STALKER 2 save pre destruktivnih build/install testova.

## Combat — 30 aktivnih izvora

- [ ] **Tab-scenario:** Your Weapons: tri nezavisna 270° knoba (damage/headshot/jamming), pravilan snap, drugi ne menjaju vrednost prvog
- [ ] **Tab-scenario:** Human Enemies: dva nezavisna pravca na 2D grafiku (HP/weapon damage), tri dodatna kompaktna editora (frequency/accuracy/explosion), bez pogrešnih osa
- [ ] **Tab-scenario:** Mutant Combat: posebni Mutant Hit Damage i 14 individualnih mutant HP editora, uključujući Bloodsucker/Mist deljeni faktor
- [ ] **Tab-scenario:** Psy/Repeated Hits: nezavisne dve granice zaštite, healed-hit checkbox i Psy controls; nema fiktivnog procenta štete

### Human Enemies (5)

- [ ] **Human Enemy Health** — `combat.npcHealth` · SIDRO, slider, raw `0.25…2`, step `0.1`. *Očekivanje:* Changes how much damage human enemies can take before they die.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Human Enemy Gun Damage** — `combat.npcWeaponDamage` · SIDRO, slider, raw `0…2`, step `0.1`. *Očekivanje:* Changes how much damage you take from firearm hits by human enemies.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Human Attack Frequency** — `combat.npcAttackCooldown` · SIDRO, slider, raw `0.5…2`, step `0.1`. *Očekivanje:* Controls how frequently human enemies can make another attack.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Human Enemy Fire Accuracy** — `combat.npcHipAccuracy` · SIDRO, slider, raw `0…1`, step `0.1`. *Očekivanje:* Changes how accurate human enemy gunfire is.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Explosion Damage** — `combat.explosionDamage` · SIDRO, slider, raw `0…2`, step `0.1`. *Očekivanje:* Changes damage from explosions such as grenades and other explosive effects.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Your Weapons (3)

- [ ] **Your Weapon Damage** — `combat.playerWeaponDamage` · SIDRO, slider, raw `0.25…2`, step `0.25`. *Očekivanje:* Changes the base damage dealt by your weapons.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Your Headshot Damage** — `combat.playerHeadshotMultiplier` · SIDRO, slider, raw `0.5…4`, step `0.25`. *Očekivanje:* Scales the damage multiplier applied to your headshots; 1x is normal and 2x doubles the configured headshot damage.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Weapon Jamming Frequency** — `combat.weaponJamming` · SIDRO, slider, raw `0…2`, step `0.25`. *Očekivanje:* Changes how often worn weapons jam.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Mutant Health (15)

- [ ] **Mutant Hit Damage** — `combat.mutantDamage` · SIDRO, slider, raw `0…2`, step `0.1`. *Očekivanje:* Changes how much damage mutant attacks deal when they connect.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Mutant Health (mutantHealth.bloodsucker)** — `combat.mutantHealth.bloodsucker` · SIDRO, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Scales Bloodsucker-family HP with one multiplier. The radar shows the standard Bloodsucker HP; Mist Bloodsucker follows the same multiplier.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Chimera Health** — `combat.mutantHealth.chimera` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Increase or decrease Chimera HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Deer Health** — `combat.mutantHealth.deer` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Increase or decrease Deer HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Pseudogiant Health** — `combat.mutantHealth.pseudogiant` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Increase or decrease Pseudogiant HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Pseudodog Health** — `combat.mutantHealth.pseudodog` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Increase or decrease Pseudodog HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Burer Health** — `combat.mutantHealth.burer` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Increase or decrease Burer HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Bayun Health** — `combat.mutantHealth.bayun` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Increase or decrease Bayun HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Blind Dog Health** — `combat.mutantHealth.blinddog` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Scales Blind Dog maximum HP. Vanilla: 63 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Boar Health** — `combat.mutantHealth.boar` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Scales Boar maximum HP. Vanilla: 300 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Flesh Health** — `combat.mutantHealth.flesh` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Scales Flesh maximum HP. Vanilla: 280 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Poltergeist Health** — `combat.mutantHealth.poltergeist` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Scales Poltergeist maximum HP. Vanilla: 400 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Snork Health** — `combat.mutantHealth.snork` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Scales Snork maximum HP. Vanilla: 150 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Tushkan Health** — `combat.mutantHealth.tushkan` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Scales Tushkan maximum HP. Vanilla: 19 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Controller Health** — `combat.mutantHealth.controller` · ČLAN, slider, raw `0.05…2`, step `0.05`. *Očekivanje:* Increase or decrease Controller HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Taking Repeated Hits (3)

- [ ] **Repeated-Hit Protection - Maximum** — `combat.accumulatedDamageWeightMax` · SIDRO, slider, raw `0…1`, step `0.1`. *Očekivanje:* Sets the stronger end of the hidden protection the game can apply when several hits land close together.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Repeated-Hit Protection - Minimum** — `combat.accumulatedDamageWeightMin` · SIDRO, slider, raw `0…1`, step `0.1`. *Očekivanje:* Sets the weaker end of the hidden protection the game can apply when several hits land close together.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Healed Damage Still Counts** — `combat.accumulatedDamageIncludesHealedHealth` · SIDRO, toggle, OFF/ON. *Očekivanje:* Controls whether damage remains in the repeated-hit protection tracker after you heal it.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Psy Combat (4)

- [ ] **Psy Phantom Spawn-Cycle Delay** — `combat.psySpawnCycleDelay` · SIDRO, slider, raw `0.25…2`, step `0.1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Controls how long Psy encounters using the Master override wait before another phantom spawn cycle can begin.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Psy Phantom Count** — `combat.psyPhantomCount` · SIDRO, slider, raw `0…3`, step `0.1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Controls how many Psy phantoms the Master override adds to the affected encounters.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Psy Phantoms Cause Bleeding** — `combat.psyBleeding` · SIDRO, toggle, OFF/ON · **RUNTIME TEST REQUIRED**. *Očekivanje:* Lets the affected Master Psy phantoms inflict bleeding in addition to their normal threat.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Psy Equipment Durability Damage** — `combat.psyDurabilityDamage` · SIDRO, slider, raw `0…3`, step `0.1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Controls how much equipment durability the affected Master Psy enemies damage.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Survival — 52 aktivnih izvora

- [ ] **Tab-scenario:** Hunger: diskretni LOW/MEDIUM/SEVERE pragovi, oporavak stamine i well-fed; izbegnuta glatka fiktivna kriva
- [ ] **Tab-scenario:** Fatigue: svi LOW/MEDIUM/SEVERE threshold/aim/reload efekti, bez LOW reload izvora; četvororedni opis se ne seče
- [ ] **Tab-scenario:** Consumables: Water/Energy/Vodka/Beer/Magic Vodka/Vinca/Hercules/Psi Block — svaka jačina i trajanje nezavisni
- [ ] **Tab-scenario:** Water/Energy: proveriti specifične 5s/15s/30s/45s efekte, kompaktne mini-knobove, izvedenu sivu donju vrednost i žute samo promenjene cifre; izbeći -0%
- [ ] **Tab-scenario:** Alcohol: Beer/Vodka nezavisni, progresija SOBER/TIPSY/DRUNK/WASTED i bez izmišljenog OverDrunkness izvora
- [ ] **Tab-scenario:** Sleeping: pragovi mogućnosti/minimum sati i stvarni HP/stamina efekti pri buđenju bez lažnog time-simulatora
- [ ] **Tab-scenario:** Radiation: tri odvojena skalara exposure/recovery/damage; ne računati matematički jedinstveni rad HP/s
- [ ] **Tab-scenario:** Bleeding: dve top kontrole i 5 source vrednosti ispod, pragovi i damage steps, bez DEAD IN spekulacije

### Hunger (7)

- [ ] **Hunger Build-up Rate** — `survival.hungerRate` · SIDRO, slider, raw `0…0.03`, step `0.001`. *Očekivanje:* Changes how quickly you become hungry.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Low Hunger Threshold** — `survival.hungerLowThreshold` · ČLAN, slider, raw `0…100`, step `1`. *Očekivanje:* Sets when the Low hunger state begins.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medium Hunger Threshold** — `survival.hungerMediumThreshold` · ČLAN, slider, raw `0…100`, step `1`. *Očekivanje:* Sets when Medium hunger begins, including its stamina-recovery penalty and no-sleep state.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe Hunger Threshold** — `survival.hungerStrongThreshold` · ČLAN, slider, raw `0…100`, step `1`. *Očekivanje:* Sets when the standard Strong hunger state begins.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Hunger Stamina Recovery Penalty** — `survival.hungerMediumStaminaRegen` · SIDRO, slider, raw `0…0.66`, step `0.01`. *Očekivanje:* Controls how strongly medium hunger reduces stamina recovery.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe Hunger Stamina Recovery Penalty** — `survival.hungerStrongStaminaRegen` · SIDRO, slider, raw `0…1`, step `0.05`. *Očekivanje:* Controls how strongly severe hunger reduces stamina recovery.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Well-Fed Stamina Recovery Bonus** — `survival.wellFedStaminaRegen` · SIDRO, slider, raw `0…0.4`, step `0.01`. *Očekivanje:* Changes the stamina-recovery bonus you get while well fed. Vanilla gives +20%.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Fatigue / Sleepiness (10)

- [ ] **Fatigue Build-up Rate** — `survival.sleepinessRate` · SIDRO, slider, raw `0…0.02`, step `0.001`. *Očekivanje:* Changes how quickly you become tired and need sleep.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Low Fatigue Threshold** — `survival.sleepLowThreshold` · ČLAN, slider, raw `0…100`, step `1`. *Očekivanje:* Sets when the Low fatigue state begins.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medium Fatigue Threshold** — `survival.sleepMediumThreshold` · ČLAN, slider, raw `0…100`, step `1`. *Očekivanje:* Sets when Medium fatigue begins.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe Fatigue Threshold** — `survival.sleepStrongThreshold` · ČLAN, slider, raw `0…100`, step `1`. *Očekivanje:* Sets when Severe fatigue begins.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Energy Drink Fatigue Relief** — `survival.energyDrinkFatigueRelief` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales the amount of accumulated fatigue removed by energy-drink sleepiness-reduction effects.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Low Fatigue Aim-Time Penalty** — `survival.fatigueLowAimPenalty` · SIDRO, slider, raw `0…0.3`, step `0.01`. *Očekivanje:* At the Low Sleepiness state, vanilla increases aiming time by 15%.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medium Fatigue Aim-Time Penalty** — `survival.fatigueMediumAimPenalty` · SIDRO, slider, raw `0…0.5`, step `0.01`. *Očekivanje:* Vanilla raises the aiming-time penalty to +25% in the medium fatigue state.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe Fatigue Aim-Time Penalty** — `survival.fatigueStrongAimPenalty` · SIDRO, slider, raw `0…0.8`, step `0.01`. *Očekivanje:* Vanilla increases aiming time by +40% in the severe fatigue state.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medium Fatigue Reload-Time Penalty** — `survival.fatigueMediumReloadPenalty` · SIDRO, slider, raw `0…0.3`, step `0.01`. *Očekivanje:* Medium fatigue adds +15% to the reload-time effect in vanilla.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe Fatigue Reload-Time Penalty** — `survival.fatigueStrongReloadPenalty` · SIDRO, slider, raw `0…0.375`, step `0.0025`. *Očekivanje:* Controls how much severe fatigue slows your reloads. Vanilla severe fatigue adds 18.75% reload time.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Consumable Effects (19)

- [ ] **Water Stamina Gain** — `survival.waterInstantStaminaRestore` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Sets instant stamina restored by Water as an exact percentage of maximum stamina.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Energy Drink Stamina Gain** — `survival.energyDrinkInstantStaminaRestore` · ČLAN, slider, raw `0…1`, step `0.01`. *Očekivanje:* Sets instant stamina restored by normal Energy Drink as an exact percentage of maximum stamina.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Water Stamina Bonus** — `survival.waterStaminaEffectStrength` · ČLAN, slider, raw `0…2`, step `0.05`. *Očekivanje:* Scales WaterStamina2's native timed +25% Stamina effect independently of Water's action-stamina cost reduction and immediate 38% stamina refill. The prototype t…. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Stamina Cost** — `survival.waterStaminaCostReduction` · ČLAN, slider, raw `0…2`, step `0.05`. *Očekivanje:* Independently scales Water's native -10% SPDrain action-stamina-cost reduction without altering the distinct +25% Water Stamina effect or instant stamina gain.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Water Effect Duration** — `survival.waterEffectDuration` · SIDRO, slider, raw `0.16666666666666666…3`, step `0.05`. *Očekivanje:* Scales Water's main stamina-cost reduction duration; the shorter regeneration duration is independently adjustable.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Energy Drink Stamina Regeneration** — `survival.energyDrinkStaminaRecoveryStrength` · ČLAN, slider, raw `0…2`, step `0.05`. *Očekivanje:* Scales the sustained stamina-regeneration bonus from the normal Energy Drink without changing its instant refill, fatigue relief or duration.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Energy Drink Effect Duration** — `survival.energyDrinkEffectDuration` · SIDRO, slider, raw `0.3333333333333333…3`, step `0.05`. *Očekivanje:* Scales both normal and Limited Energy Drink's 45-second stamina regeneration; the two action-cost effect durations are independent.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Water +25% Max Stamina Duration** — `survival.waterRegenDuration` · ČLAN, slider, raw `5…90`, step `0.5`. *Očekivanje:* Duration of Water's +25% Max Stamina buff.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Energy Drink No Stamina Cost Duration** — `survival.energyFullCostDuration` · ČLAN, slider, raw `5…135`, step `0.5`. *Očekivanje:* Duration of Energy Drink's no-stamina-cost effect (100% SPDrain reduction).. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Energy Drink -30% Stamina Cost Duration** — `survival.energyPartialCostDuration` · ČLAN, slider, raw `15…135`, step `0.5`. *Očekivanje:* Duration of Energy Drink's -30% stamina cost effect.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Vodka Effect Duration** — `survival.vodkaEffectDuration` · SIDRO, slider, raw `0…3`, step `0.05`. *Očekivanje:* Scales the duration of Vodka's stamina-drain penalty.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Beer Effect Duration** — `survival.beerEffectDuration` · SIDRO, slider, raw `0…3`, step `0.05`. *Očekivanje:* Scales the duration of Beer's stamina-drain penalty.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Magic Vodka Effect Duration** — `survival.magicVodkaEffectDuration` · SIDRO, slider, raw `0…3`, step `0.05`. *Očekivanje:* Scales the duration of Dvupalov/Magic Vodka's psi-protection effect.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Vinca Bleed Recovery** — `survival.vincaBleedRecovery` · ČLAN, slider, raw `0…30`, step `1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Vinca increases bleeding recovery by the shown number of bleed points while its effect lasts.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Vinca Duration** — `survival.vincaDuration` · ČLAN, slider, raw `0…540`, step `5` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Duration of Vinca's ongoing bleeding recovery. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Hercules Carry Bonus** — `survival.herculesCarryBonus` · ČLAN, slider, raw `0…60`, step `1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Hercules temporarily grants the shown additional kilograms of carry capacity and adjusts the linked weight-penalty effect.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Hercules Duration** — `survival.herculesDuration` · ČLAN, slider, raw `0…900`, step `5` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Duration of both linked Hercules effects. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Psi Block Recovery** — `survival.psiBlockRecovery` · ČLAN, slider, raw `0…30`, step `1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Psi Block increases the displayed psi protection by the shown number of psi points for the effect duration.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Psi Block Duration** — `survival.psiBlockDuration` · ČLAN, slider, raw `0…300`, step `5` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Duration of Psi Block psi-point recovery. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Alcohol Tolerance (2)

- [ ] **Beer Strength** — `survival.beerDrunknessStrength` · SIDRO, slider, raw `8…40`, step `1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Sets Beer drink strength against the current DrunknessMechanics stages: Low 0.001, Medium 30 and Strong 80.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Vodka Strength** — `survival.vodkaDrunknessStrength` · SIDRO, slider, raw `15…80`, step `5` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Sets how much Drunkness one Vodka adds against the current DrunknessMechanics stage boundaries.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Sleeping (4)

- [ ] **Sleep Becomes Available At** — `survival.sleepEligibilityThreshold` · SIDRO, slider, raw `0…100`, step `5`. *Očekivanje:* Controls how tired the player must be before normal sleeping is allowed.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Minimum Sleep** — `survival.minimumSleepHours` · SIDRO, slider, raw `1…14`, step `1`. *Očekivanje:* Sets the minimum sleep duration used by the default sleep settings.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Post-Sleep Health Restore** — `survival.sleepHealthRestore` · SIDRO, slider, raw `0…200`, step `5`. *Očekivanje:* Controls the direct health restore you receive after normal sleep. The tool starts at 100 HP (1x), adjustable from 0 to 200 HP; the vanilla CFG remains 300 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Post-Sleep Stamina Restore** — `survival.sleepStaminaRestore` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Normal sleep restores stamina with a vanilla +100% Stamina effect.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Radiation (3)

- [ ] **Radiation Accumulation** — `survival.radiationAccumulation` · SIDRO, slider, raw `0…2.5`, step `0.01`. *Očekivanje:* Scales the rate at which radiation is accumulated through the difficulty preset.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Radiation Recovery** — `survival.radiationRecovery` · SIDRO, slider, raw `0…0.1`, step `0.01`. *Očekivanje:* Controls how quickly accumulated radiation decreases naturally, in points per second.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Radiation Damage** — `survival.radiationDamage` · SIDRO, slider, raw `0…25`, step `0.01`. *Očekivanje:* Changes how much health damage accumulated radiation causes once radiation begins hurting you, without moving the vanilla radiation-state thresholds.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Bleeding (7)

- [ ] **Bleeding Severity** — `survival.bleedingSeverity` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales the difficulty-wide bleeding effect applied by sources that cause bleeding.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Natural Bleeding Recovery** — `survival.bleedingRecovery` · SIDRO, slider, raw `0…0.6`, step `0.05`. *Očekivanje:* Controls the player's base natural bleeding reduction rate.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medium Bleeding Threshold** — `survival.bleedingMediumThreshold` · ČLAN, slider, raw `1…99`, step `1`. *Očekivanje:* Sets when Medium bleeding begins.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe Bleeding Threshold** — `survival.bleedingStrongThreshold` · ČLAN, slider, raw `1…100`, step `1`. *Očekivanje:* Sets when Severe bleeding begins.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Low Bleeding Damage** — `survival.bleedingLowDamage` · SIDRO, slider, raw `0…2`, step `0.1`. *Očekivanje:* Controls the health damage dealt by low bleeding each time its damage tick occurs. Vanilla uses 1 damage.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medium Bleeding Damage** — `survival.bleedingMediumDamage` · SIDRO, slider, raw `0…3`, step `0.1`. *Očekivanje:* Controls the health damage dealt by medium bleeding each time its damage tick occurs. Vanilla uses 1.5 damage.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe Bleeding Damage** — `survival.bleedingStrongDamage` · SIDRO, slider, raw `0…4`, step `0.1`. *Očekivanje:* Controls the health damage dealt by severe bleeding each time its damage tick occurs. Vanilla uses 2 damage.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Health — 18 aktivnih izvora

- [ ] **Tab-scenario:** Core Health: pet različitih izvornih editora i native jedinice i bazni HP
- [ ] **Tab-scenario:** Medical: HEAL (4), BLEED (4), RAD (Scientific/Anti-Rad/Beer/Vodka), Food Healing zasebno; svi izvorni efekti ciljaju samo svoje CFG polje
- [ ] **Tab-scenario:** Ista kompaktna medicinska kartica, bez dupliciranih sekcija ili gubljenja numeričkih kolona na uskom prozoru
- [ ] **Tab-scenario:** Beer/Vodka RAD pozitivno prikazane vrednosti moraju se upisati u sopstvene negativne CFG sign vrednosti

### Radiation Treatment (4)

- [ ] **Beer Radiation Removal** — `health.beerRadiationRemoval` · ČLAN, slider, raw `0…30`, step `5`. *Očekivanje:* Sets radiation removed per Beer use, directly in positive game points.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Vodka Radiation Removal** — `health.vodkaRadiationRemoval` · ČLAN, slider, raw `0…100`, step `5`. *Očekivanje:* Sets radiation removed per Vodka use, directly in positive game points.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Scientific Medkit Radiation Removal** — `health.ecoMedkitAntirad` · SIDRO, slider, raw `0…120`, step `5`. *Očekivanje:* Controls how much radiation the Scientific Medkit removes when used.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Anti-Rad Radiation Removal** — `health.antiRadRadiation` · SIDRO, slider, raw `0…200`, step `5`. *Očekivanje:* Controls how much radiation the Anti-Rad removes when used.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Core Health (4)

- [ ] **Player Max Health** — `health.maxHP` · SIDRO, slider, raw `25…200`, step `5` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes your maximum health. The game's full-health feedback stays in sync with the new maximum.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Passive Health Recovery** — `health.baseRegen` · SIDRO, slider, raw `0…5`, step `0.5` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Controls ordinary passive health recovery on the player. Vanilla sets this to zero, so by default you do not slowly heal just by standing around.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Health Recovery Delay** — `health.regenDelay` · SIDRO, slider, raw `0…10`, step `1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Vanilla player VitalParams define RegenHPDelayTimeSeconds = 5.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Health Recovery Multiplier** — `health.regenerationScalar` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales health regeneration produced by compatible recovery effects. It does not create passive healing by itself: 100% keeps full effect strength, while lower v…. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Low Health (1)

- [ ] **Low-Health Recovery Rate** — `health.lowHealthRegen` · SIDRO, slider, raw `0…2`, step `0.1`. *Očekivanje:* Controls the positive health-regeneration rate supplied by the normal low-health recovery effect. Vanilla uses 1.0; higher values recover health faster while th…. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Healing Amount (5)

- [ ] **Healing Amount** — `health.bandageHealing` · SIDRO, slider, raw `0…40`, step `5`. *Očekivanje:* Changes how much HP the main healing items restore.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medkit Healing** — `health.medkitHealing` · ČLAN, slider, raw `0…140`, step `5`. *Očekivanje:* Controls how much health a standard Medkit restores before any Master-only healing penalty is applied.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Army Medkit Healing** — `health.armyMedkitHealing` · ČLAN, slider, raw `0…170`, step `5`. *Očekivanje:* Controls how much health an Army Medkit restores before any Master-only healing penalty is applied.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Scientific Medkit Healing** — `health.ecoMedkitHealing` · ČLAN, slider, raw `0…200`, step `5`. *Očekivanje:* Controls how much health a Scientific Medkit restores. Vanilla restores 100 HP.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Food Restores Health** — `health.foodHealing` · SIDRO, toggle, OFF/ON. *Očekivanje:* Controls whether ordinary food restores health. This affects Bread, Fresh Bread, Canned Food, and Sausage only. Satiety and Weird Kettle alternative effects are…. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Bleeding Treatment (4)

- [ ] **Bandage Bleeding Removal** — `health.bandageBleeding` · SIDRO, slider, raw `0…100`, step `5`. *Očekivanje:* Controls how much bleeding the Bandage removes when used.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medkit Bleeding Removal** — `health.medkitBleeding` · SIDRO, slider, raw `0…30`, step `5`. *Očekivanje:* Controls how much bleeding the Medkit removes when used.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Army Medkit Bleeding Removal** — `health.armyMedkitBleeding` · SIDRO, slider, raw `0…70`, step `5`. *Očekivanje:* Controls how much bleeding the Army Medkit removes when used.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Scientific Medkit Bleeding Removal** — `health.ecoMedkitBleeding` · SIDRO, slider, raw `0…50`, step `5`. *Očekivanje:* Controls how much bleeding the Scientific Medkit removes when used.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Stamina — 18 aktivnih izvora

- [ ] **Tab-scenario:** Core Stamina prikazuje stvarni capacity i regen uz ispravne jedinice 'stamina' i 'stamina/s'
- [ ] **Tab-scenario:** Action Costs prikaz i izvorna mesta Sprint, Jump, Vault i tri Melee stila; svi editabilni kroz zajednički vizuelni editor
- [ ] **Tab-scenario:** Exhaustion/Recovery — svi vidljivi source pragovi i trajanja, ne uvoditi nepostojeći WALK stamina editor
- [ ] **Tab-scenario:** Water medium/deep drain poredak; Encumbrance i Vault Height nezavisni od tih izvora

### Core Stamina (2)

- [ ] **Maximum Stamina** — `stamina.maxSP` · SIDRO, slider, raw `25…200`, step `5`. *Očekivanje:* Vanilla maximum stamina is 100. This changes the size of your stamina pool, not how fast it recovers or how much actions cost.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Base Stamina Recovery** — `stamina.regeneration` · SIDRO, slider, raw `2…10`, step `1`. *Očekivanje:* Controls the player's base stamina regeneration rate.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Exhaustion & Recovery (5)

- [ ] **Sprint Stops Below** — `stamina.sprintDisableThreshold` · ČLAN, slider, raw `0…32.2`, step `0.1`. *Očekivanje:* Below this stamina value, sprinting is disabled. Default: 16.1 of the vanilla 100 stamina pool.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Exhaustion Regen Delay** — `stamina.sprintRecoveryDelay` · ČLAN, slider, raw `0…2`, step `0.1`. *Očekivanje:* Seconds before stamina regeneration resumes after sprint exhaustion.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Boost Starts Below** — `stamina.lowRecoveryThreshold` · ČLAN, slider, raw `1…60`, step `1`. *Očekivanje:* Below this stamina value, the separate low-stamina recovery boost becomes eligible. Default: 30 stamina.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Boost Strength** — `stamina.lowRecoveryStrength` · ČLAN, slider, raw `0…20`, step `0.25`. *Očekivanje:* Regeneration strength of the temporary low-stamina recovery boost (10 stamina/s originally).. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Boost Duration** — `stamina.lowRecoveryDuration` · ČLAN, slider, raw `0.5…8`, step `0.1`. *Očekivanje:* Seconds that the conditional low-stamina stamina regeneration boost remains active (4 seconds originally).. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Traversal (1)

- [ ] **Maximum Vault Height** — `stamina.vaultMaxHeight` · SIDRO, slider, raw `70…300`, step `1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes the tallest obstacle you can vault over. The WAIST / EYES / ABOVE EYES markers are simple player-friendly height references.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Water (2)

- [ ] **Medium-Water Stamina Drain** — `stamina.waterMediumDrain` · SIDRO, slider, raw `0…2`, step `0.05`. *Očekivanje:* Changes the extra stamina drain while moving through medium-depth water.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Deep-Water Stamina Drain** — `stamina.waterStrongDrain` · SIDRO, slider, raw `0…3`, step `0.05`. *Očekivanje:* Changes the extra stamina drain while moving through deep water.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Action Costs (7)

- [ ] **Action Costs (sprintCost)** — `stamina.sprintCost` · SIDRO, slider, raw `0…10.5`, step `0.25`. *Očekivanje:* Changes stamina pressure from common player actions.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Jump Stamina Cost** — `stamina.jumpCost` · ČLAN, slider, raw `0…32`, step `1`. *Očekivanje:* Controls stamina consumed by a jump.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Vault Stamina Cost** — `stamina.vaultCost` · ČLAN, slider, raw `0…24`, step `1`. *Očekivanje:* Controls stamina consumed by vaulting.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Quick Melee Cost** — `stamina.meleeNormalCost` · ČLAN, slider, raw `0…32`, step `1`. *Očekivanje:* Controls stamina consumed by a quick melee attack.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Strong Melee Cost** — `stamina.meleeStrongCost` · ČLAN, slider, raw `0…50`, step `1`. *Očekivanje:* Controls stamina consumed by a strong melee attack.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Buttstock Melee Cost** — `stamina.butstockCost` · ČLAN, slider, raw `0…24`, step `1`. *Očekivanje:* Controls stamina consumed by a buttstock melee attack.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Aiming Regen Penalty** — `stamina.aimRegenCoef` · ČLAN, slider, raw `0…0.5`, step `0.05`. *Očekivanje:* Percent reduction in stamina regeneration specifically while aiming; 0% removes the penalty.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Encumbrance (1)

- [ ] **Maximum Carry Weight** — `stamina.maxCarryWeight` · SIDRO, slider, raw `20…160`, step `5`. *Očekivanje:* Sets the player's default maximum inventory mass.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Loot — 41 aktivnih izvora

- [ ] **Tab-scenario:** NPC Corpse Extra Loot izbori Medicine+Ammo / Water / None, NPC Medical, NPC Ammo i dual-handle Weapon Condition (MIN≤MAX)
- [ ] **Tab-scenario:** NPC Ammo read-only preview za tipove municije i readout samo ako korisnik izmeni source, bez dodatnih lažnih handle-ova
- [ ] **Tab-scenario:** Cheap/Standard/Rare stash količine, grenade chance/amount i rare coupon rewards, CFG writer nikad ne gazi međusobne kopije
- [ ] **Tab-scenario:** Breakable ammo yield, četiri family weight i grenade yield, 8 preview kalibara; preview znači moguća količina, ne garantovan drop
- [ ] **Tab-scenario:** 14 mutant-part chance izvora uključujući Bayun (interni 'cat' ostaje kompatibilan)
- [ ] **Tab-scenario:** World Loot Removal kategorije — kompaktno, bez redundantnog naziva, egzaktan produkcioni CFG scope
- [ ] **Tab-scenario:** World Placement — toggle, child Weapon Density, attachments, detectors, blueprints i unique weapons samo u deklarisanim produkcionim granicama
- [ ] **Tab-scenario:** Generatori ne menjaju quest/story/test/unresolved resurse koji nisu deo njihovog scope-a

### NPC Loot & Equipment (4)

- [ ] **Extra NPC Corpse Loot** — `loot.npcCorpseBonus` · SIDRO, choiceSlider, diskretan izbor ili poseban izvor. *Očekivanje:* Chooses the extra smart-loot package added to human NPC corpses: Medicine + Ammo, Water, or None.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **NPC Medical Drop** — `loot.npcMedicalChance` · SIDRO, slider, raw `0…2`, step `0.05` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how often ordinary human enemies carry Bandages, Medkits, Army Medkits, or Anti-Rad. It does not change how many items are in each stack.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **NPC Ammo Drops** — `loot.npcWeaponAmmoSupply` · SIDRO, slider, raw `0…3`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Scales the generic NPC_Ammo_Smart bonus ammunition stacks on ordinary NPC corpses. This is not NPC weapon-attached ammo and not Breakable Box Ammo.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **NPC Weapon Condition** — `loot.droppedWeaponCondition` · SIDRO, range, raw `0…1`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Sets the actual minimum and maximum condition of weapons carried by ordinary repeatable human NPC loadouts. Vanilla is left untouched until this control is changed.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Stashes (6)

- [ ] **Stash Ammo Amounts (cheapStashAmmoQuantity)** — `loot.cheapStashAmmoQuantity` · SIDRO, slider, raw `0.25…2`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes ammo amounts in Basic, Standard, and Rare stashes.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Basic Stash RGD-5 Grenade Chance** — `loot.cheapStashGrenadeChance` · SIDRO, slider, raw `0…1`, step `0.05` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how likely a Basic stash is to contain its RGD-5 grenade reward. This does not change how many grenades you get when the roll succeeds.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Basic Stash RGD-5 Grenade Amount** — `loot.cheapStashGrenadeQuantity` · SIDRO, slider, raw `0…4`, step `1` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how many RGD-5 grenades you receive when a Basic stash successfully rolls its grenade reward. The chance to receive grenades is controlled separately.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Standard Stash Ammo** — `loot.mediumStashAmmoQuantity` · ČLAN, slider, raw `0.25…2`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how much ammo Standard stashes give you. Which calibers can appear still follows vanilla progression.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Rare Stash Ammo** — `loot.expensiveStashAmmoQuantity` · ČLAN, slider, raw `0.25…2`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how much ammo Rare stashes give you. Vanilla rarity, progression and ammo-type selection remain unchanged; only the amount found changes.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Rare Stash Coupon Rewards** — `loot.expensiveStashMoneyChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Changes how likely an eligible Rare stash is to contain coupon rewards. The game still chooses the reward tier from vanilla progression.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Breakable Containers (6)

- [ ] **Ammo from Breakable Boxes** — `loot.destructibleAmmoQuantity` · SIDRO, slider, raw `0.5…2`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how much ammunition you get from breakable ammo boxes. Ammo Box Distribution separately changes which ammo families are more common.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Breakable Box Ammo Distribution (ammoBoxWeight918)** — `loot.ammoBoxWeight918` · SIDRO, slider, raw `0…3`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes which ammo families are favored in breakable ammo boxes.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **.45 ACP Ammo Box Weight** — `loot.ammoBoxWeight045` · ČLAN, slider, raw `0…3`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how common .45 ACP ammo is in breakable ammo boxes.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **5.45 / 5.56 mm Ammo Box Weight** — `loot.ammoBoxWeight545` · ČLAN, slider, raw `0…3`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how common 5.45 / 5.56 mm ammo is in breakable ammo boxes.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **12 Gauge Ammo Box Weight** — `loot.ammoBoxWeight012` · ČLAN, slider, raw `0…3`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how common 12 Gauge ammo is in breakable ammo boxes.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Grenades from Breakable Boxes** — `loot.destructibleGrenadeQuantity` · SIDRO, slider, raw `0.5…2`, step `0.5` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how many grenades you get from dedicated breakable grenade boxes. F1 boxes contain F1 grenades and RGD-5 boxes contain RGD-5 grenades; grenade type rema…. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Mutant Part Drop Chances (14)

- [ ] **Mutant Part Drop Chances (mutant.blindDogChance)** — `loot.mutant.blindDogChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Changes mutant-part drop chances by mutant family.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Bloodsucker Part Drop Chance** — `loot.mutant.bloodsuckerChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Bloodsucker kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Boar Part Drop Chance** — `loot.mutant.boarChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Boar kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Burer Part Drop Chance** — `loot.mutant.burerChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Burer kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Bayun Part Drop Chance** — `loot.mutant.catChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Bayun kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Chimera Part Drop Chance** — `loot.mutant.chimeraChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Chimera kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Controller Part Drop Chance** — `loot.mutant.controllerChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Controller kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Deer Part Drop Chance** — `loot.mutant.deerChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Deer kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Flesh Part Drop Chance** — `loot.mutant.fleshChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Flesh kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Poltergeist Part Drop Chance** — `loot.mutant.poltergeistChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Poltergeist kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Pseudodog Part Drop Chance** — `loot.mutant.pseudodogChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Pseudodog kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Pseudogiant Part Drop Chance** — `loot.mutant.pseudogiantChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Pseudogiant kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Tushkan Part Drop Chance** — `loot.mutant.tushkanChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Tushkan kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Snork Part Drop Chance** — `loot.mutant.snorkChance` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how often an eligible Snork kill drops its mutant part. When it drops, you get 1 part.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### World Loot (11)

- [ ] **Ammo Boxes Removed** — `loot.worldAmmoCratesRemoved` · SIDRO, slider, raw `0…1`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how much supported world loot is removed by category.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **F1 Grenade Boxes** — `loot.worldF1GrenadeBoxes` · ČLAN, slider, raw `0…1`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Choose how many dedicated F1 grenade boxes are removed from the world. This is an independent physical population from the ordinary ammo/RGD box axis.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **World Food & Drink Boxes** — `loot.worldFoodDrinkCratesRemoved` · ČLAN, slider, raw `0…1`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Choose how many world food and drink boxes are removed. 0% keeps the vanilla world; higher values make food and drinks less abundant in exploration.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **World Medical Boxes** — `loot.worldMedicalCratesRemoved` · ČLAN, slider, raw `0…1`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Choose how many world medical boxes are removed. 0% keeps the vanilla world; higher values make easy medical supplies less common while exploring.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Loose World Loot** — `loot.looseWorldLootRemoved` · ČLAN, slider, raw `0…1`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Choose how much ordinary loose loot is removed from the world. This covers eligible ammo, consumables and grenades found lying around; 0% keeps vanilla placement.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Shuffle Unique Weapons** — `loot.looseUniqueWeaponShuffle` · ČLAN, toggle, OFF/ON · **RUNTIME TEST REQUIRED**. *Očekivanje:* Shuffle nine verified non-quest-referenced loose unique weapons among their nine existing world pickup locations. All weapons remain in the world once each.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **World Weapon Shuffle** — `loot.worldWeaponShuffle` · SIDRO, toggle, OFF/ON · **RUNTIME TEST REQUIRED**. *Očekivanje:* Redistributes regular world weapons across compatible locations whenever you build the mod. Pistols stay in pistol spots, primary weapons stay in primary spots,…. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **World Weapon Density** — `loot.worldWeaponDensity` · SIDRO, slider, raw `0…1`, step `0.05` · parent `loot.worldWeaponShuffle` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Controls how many compatible world-weapon locations stay populated. As density rises, the tool guarantees more copies of every one of the 36 regular weapon models.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **World Attachment Shuffle** — `loot.worldAttachmentShuffle` · SIDRO, toggle, OFF/ON · **RUNTIME TEST REQUIRED**. *Očekivanje:* Redistribute safe-world attachments across compatible locations each time you build.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Remove World Detectors** — `loot.worldDetectors` · SIDRO, toggle, OFF/ON · **RUNTIME TEST REQUIRED**. *Očekivanje:* Remove the proven non-quest detector pickups placed directly in the world and in safe stashes.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Blueprint Shuffle** — `loot.blueprintShuffle` · SIDRO, toggle, OFF/ON · **RUNTIME TEST REQUIRED**. *Očekivanje:* Shuffle eligible world blueprint placements each time you build. OFF keeps vanilla placement; ON creates a fresh one-to-one shuffle while preserving the same bl…. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Economy — 22 aktivnih izvora

- [ ] **Tab-scenario:** Coupons: Starting i Main/Side quest rewards, direktno menjivi bez dugog +/- kliktanja
- [ ] **Tab-scenario:** Trading: četiri nezavisna price/condition izvora i ispravan smer promene
- [ ] **Tab-scenario:** Price layers: svih šest kategorija i kompaktne čitljive ose
- [ ] **Tab-scenario:** Repair: Base Repair i Friend/Neutral/Disliked/Hostile vrednosti sa poretkom po constraints
- [ ] **Tab-scenario:** Stock: Trader Ammo i Medic Stock testirati uz kontrolisano osvežavanje inventara, ne već ranije učitane objekte

### Starting Coupons (1)

- [ ] **Starting Coupons** — `economy.startingMoney` · SIDRO, slider, raw `0…100000`, step `10`. *Očekivanje:* Sets how many coupons a new game starts with. It does not change quest rewards, trader prices, stash coupons, or the balance of an existing save.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Trading (4)

- [ ] **Trader Buy-From-You Pricing** — `economy.buyCondition` · SIDRO, slider, raw `0…2`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes the difficulty-profile BuyCondition used when a trader buys an item from you. Harder vanilla difficulties lower this setting.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Trader Sell-To-You Pricing** — `economy.sellCondition` · SIDRO, slider, raw `0…2`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes the difficulty-profile SellCondition used when a trader sells an item to you. Harder vanilla difficulties raise this setting.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Trader Minimum Buy Condition** — `economy.traderMinBuyCondition` · SIDRO, slider, raw `0…1`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Sets the minimum condition a weapon or armor must have before traders will buy it from you. 0% allows completely broken gear; 40% is vanilla.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Item Value at 0% Durability** — `economy.itemValueFloor` · SIDRO, slider, raw `0…0.2`, step `0.01`. *Očekivanje:* Sets how much of an item's base value remains when durability reaches 0%. Vanilla is 10%; the game's documented curve gives 55% value at 50% durability and 100%…. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Trader Stock (2)

- [ ] **Trader Ammo Stock Amount** — `economy.traderAmmoStock` · SIDRO, slider, raw `0.25…3`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how much ammo regular traders carry. Ammo types, progression requirements, and restock behavior stay vanilla.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Medic Stock Amount** — `economy.medicStock` · SIDRO, slider, raw `0.25…3`, step `0.25` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how many medical supplies medics carry for sale. Item availability, progression requirements, and restock timing stay vanilla.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Item Price Scalers (6)

- [ ] **Item Price Scalers (consumableCost)** — `economy.consumableCost` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes base item value by item category.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Ammo Cost** — `economy.ammoCost` · ČLAN, slider, raw `0…2`, step `0.01`. *Očekivanje:* The in-game difficulty presets expose Ammo_Cost directly and keep it at 1.00 on all four vanilla difficulties.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Armor Cost** — `economy.armorCost` · ČLAN, slider, raw `0…2`, step `0.01`. *Očekivanje:* The in-game difficulty presets expose Armor_Cost directly and keep it at 1.00 on all four vanilla difficulties.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Weapon Cost** — `economy.weaponCost` · ČLAN, slider, raw `0…2`, step `0.01`. *Očekivanje:* The in-game difficulty presets expose Weapon_Cost directly and keep it at 1.00 on all four vanilla difficulties.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Artifact Cost** — `economy.artifactCost` · ČLAN, slider, raw `0…2`, step `0.01`. *Očekivanje:* The in-game difficulty presets expose Artifact_Cost directly and keep it at 1.00 on all four vanilla difficulties.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Mutant Parts Cost** — `economy.mutantPartValue` · ČLAN, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes the base coupon value of all mutant parts together while keeping rare trophies more valuable than common ones.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Services (3)

- [ ] **Fast Travel Cost** — `economy.fastTravelCost` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales guide fast-travel prices while preserving the vanilla price differences between destinations.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Upgrade Cost** — `economy.upgradeCost` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Upgrade_Cost is a direct difficulty setting: 100% on Novice/Stalker, 125% on Veteran and 150% on Master.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Base Repair Price** — `economy.repairCostBase` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Sets the overall baseline for repair prices. Vanilla is 70%. Use it to make repairs cheaper or more expensive everywhere.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Repair per Reputation (4)

- [ ] **Hostile Repair Price** — `economy.repairEnemy` · SIDRO, slider, raw `0…3`, step `0.01`. *Očekivanje:* Changes repair prices when your reputation with the technician's faction is Enemy. Vanilla is 2x of the base repair price.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Disliked Repair Price** — `economy.repairDisaffection` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes repair prices when your reputation with the technician's faction is Disliked. Vanilla is 1.5x of the base repair price.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Neutral Repair Price** — `economy.repairNeutral` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes repair prices when your reputation with the technician's faction is Neutral. Vanilla is 1x of the base repair price.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Friendly Repair Price** — `economy.repairFriend` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes repair prices when your reputation with the technician's faction is Friend. Vanilla is 0.75x of the base repair price.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Coupon Rewards (2)

- [ ] **Main Quest Coupon Rewards** — `economy.mainQuestReward` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes the coupon rewards paid by main quests while preserving the vanilla difficulty-based reward scaling.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Side Quest Coupon Rewards** — `economy.sideQuestReward` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes the coupon rewards paid by side quests while preserving the vanilla difficulty-based reward scaling.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Equipment — 4 aktivnih izvora

- [ ] **Tab-scenario:** Wear: Armor Durability i Weapon Wear Per Shot kao dve zasebne ose
- [ ] **Tab-scenario:** Weight: Ammo Weight i Consumable Weight ne menjaju carry capacity niti broj predmeta u stacku

### Equipment Wear (2)

- [ ] **Armor Durability** — `equipment.armorDurability` · SIDRO, slider, raw `0.1…2`, step `0.01` · **RUNTIME TEST REQUIRED**. *Očekivanje:* Changes how quickly armor loses condition. Higher settings make armor last longer; lower settings make it wear out faster.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Player Weapon Wear Per Shot** — `equipment.weaponWearPerShot` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Changes how quickly player weapons lose condition when fired, while keeping each weapon's own vanilla wear rate relative to the others.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Item Weight (2)

- [ ] **Ammo Weight** — `equipment.ammoWeight` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales ammunition weight while preserving the vanilla weight differences between ammo types.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Consumable Weight** — `equipment.consumableWeight` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales food and medicine item weight while preserving the vanilla differences between consumables.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Anomalies — 15 aktivnih izvora

- [ ] **Tab-scenario:** Global anomaly damage jedan knob zajedno sa individualnim barovima, bez zasebnog rama oko njega
- [ ] **Tab-scenario:** 10 stvarnih individualnih anomaly HP damage kontrola abecedno; EFFECTIVE izvedeni brojevi reaguje na global i svoj izvor
- [ ] **Tab-scenario:** Glass Shards naziv za Razor, odvojeni HP/Bleeding source i posebne low/high damage grane
- [ ] **Tab-scenario:** Flycatcher Movement i Turn Speed su nezavisni; Psy Intensity nije HP damage
- [ ] **Tab-scenario:** Diamond health/bleeding ne smeju biti prikazani kao aktuelne edit kontrole

### Global Anomaly Damage (1)

- [ ] **Global Anomaly Damage** — `environment.anomalyDamage` · SIDRO, slider, raw `0…2.5`, step `0.01`. *Očekivanje:* This is the broad anomaly difficulty setting. It scales damaging anomaly output as a group while each anomaly keeps its own base damage value.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Individual Anomaly Damage (10)

- [ ] **Individual Anomaly Damage (electroAnomalyDamage)** — `environment.electroAnomalyDamage` · SIDRO, slider, raw `0…130`, step `1`. *Očekivanje:* Changes damage separately for each supported anomaly.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Chemical Base Damage** — `environment.chemicalAnomalyDamage` · ČLAN, slider, raw `0…60`, step `1`. *Očekivanje:* Fine-tunes Chemical damage without changing the other anomalies. Vanilla base damage is 30. Global Anomaly Damage still affects it too.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Fire Base Damage** — `environment.fireAnomalyDamage` · ČLAN, slider, raw `0…120`, step `1`. *Očekivanje:* Fine-tunes Fire damage without changing the other anomalies. Vanilla base damage is 60. Global Anomaly Damage still affects it too.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Carousel Base Damage** — `environment.carouselAnomalyDamage` · ČLAN, slider, raw `0…210`, step `1`. *Očekivanje:* Fine-tunes Carousel damage without changing the other anomalies. Vanilla base damage is 105. Global Anomaly Damage still affects it too.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Toxic Cloud Base Damage** — `environment.toxicCloudDamage` · ČLAN, slider, raw `0…50`, step `1`. *Očekivanje:* Fine-tunes Toxic Cloud damage without changing the other anomalies. Vanilla base damage is 25. Global Anomaly Damage still affects it too.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Steam Base Damage** — `environment.steamAnomalyDamage` · ČLAN, slider, raw `0…120`, step `1`. *Očekivanje:* Fine-tunes Steam damage without changing the other anomalies. Vanilla base damage is 60. Global Anomaly Damage still affects it too.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Clicker Health Damage** — `environment.clickerHealthDamage` · ČLAN, slider, raw `0…140`, step `1`. *Očekivanje:* Controls the direct health damage dealt by a Clicker hit without changing the anomaly's equipment corrosion or behavior.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Glass Shards Health Damage** — `environment.razorHealthDamage` · ČLAN, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales Glass Shards health damage at both movement-speed tiers while preserving the stronger high-speed hit.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Soap Bubble Health Damage** — `environment.soapBubbleHealthDamage` · ČLAN, slider, raw `0…200`, step `1`. *Očekivanje:* Controls the direct Chemical Burn health damage of the Soap Bubble anomaly.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Fireball Health Damage** — `environment.fireballHealthDamage` · ČLAN, slider, raw `0…200`, step `1`. *Očekivanje:* Controls the direct Burn health damage of the Fireball anomaly.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Flycatcher Slowdown (2)

- [ ] **Flycatcher Movement Speed** — `environment.flycatcherMovementSpeed` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how much of your normal movement speed remains while the Flycatcher slowdown is active.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Flycatcher Turn Speed** — `environment.flycatcherTurnSpeed` · SIDRO, slider, raw `0…1`, step `0.01`. *Očekivanje:* Controls how much of your normal look/turn speed remains while the Flycatcher slowdown is active.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Special Anomaly Effects (2)

- [ ] **Glass Shards Bleeding** — `environment.razorBleeding` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales Glass Shards bleeding at both movement-speed tiers while preserving the stronger high-speed bleeding effect.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Psy Anomaly Intensity** — `environment.psyAnomalyIntensity` · SIDRO, slider, raw `0…2`, step `0.01`. *Očekivanje:* Scales the Psy anomaly's continuous negative Psy-point effect without changing its other behavior.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## A-Life — 18 aktivnih izvora

- [ ] **Tab-scenario:** Human Groups: Newbie, Experienced, Veteran i Master na jednoj kompaktnoj interaktivnoj površini
- [ ] **Tab-scenario:** Mutant Groups: sve podržane vrste i tačno native integer snapping bez obavezne neposredne promene već učitane igre
- [ ] **Tab-scenario:** Razlikovati source CFG count od stvarno spawnowanog broja u već učitanom regionu

### Human Group Size (4)

- [ ] **Human Group Size (humanGroupSize.newbie)** — `alife.humanGroupSize.newbie` · SIDRO, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* Changes the largest human squad that can spawn at each player rank.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Experienced Human Group Size** — `alife.humanGroupSize.experienced` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* Changes the largest human squad that can spawn while the player is at Experienced rank. It does not change lair population or respawn speed.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Veteran Human Group Size** — `alife.humanGroupSize.veteran` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* Changes the largest human squad that can spawn while the player is at Veteran rank. It does not change lair population or respawn speed.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Master Human Group Size** — `alife.humanGroupSize.master` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* Changes the largest human squad that can spawn while the player is at Master rank. It does not change lair population or respawn speed.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Mutant Group Size (14)

- [ ] **Mutant Group Size (mutantSpawn.bloodsucker)** — `alife.mutantSpawn.bloodsucker` · SIDRO, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* Changes group size separately for each mutant species.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Blind Dog Spawn** — `alife.mutantSpawn.blindDog` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* blindDog species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Flesh Spawn** — `alife.mutantSpawn.flesh` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* flesh species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Controller Spawn** — `alife.mutantSpawn.controller` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* controller species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Poltergeist Spawn** — `alife.mutantSpawn.poltergeist` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* poltergeist species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player r…. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Snork Spawn** — `alife.mutantSpawn.snork` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* snork species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Boar Spawn** — `alife.mutantSpawn.boar` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* boar species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Chimera Spawn** — `alife.mutantSpawn.chimera` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* chimera species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Bayun Spawn** — `alife.mutantSpawn.bayun` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* bayun species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Tushkan Spawn** — `alife.mutantSpawn.tushkan` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* tushkan species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Rat Swarm Spawn** — `alife.mutantSpawn.ratSwarm` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* ratSwarm species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Burer Spawn** — `alife.mutantSpawn.burer` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* burer species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Pseudodog Spawn** — `alife.mutantSpawn.pseudodog` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* pseudodog species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Deer Spawn** — `alife.mutantSpawn.deer` · ČLAN, slider, raw `0.25…3`, step `0.05`. *Očekivanje:* deer species-specific A-Life encounter cap. Adjusts how many of this mutant can appear together while preserving the vanilla progression between player ranks.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Master — 20 aktivnih izvora

- [ ] **Tab-scenario:** Svaki Master toggle ON/OFF menja samo svoju grupu i enable stanje zavisnih slajdera, ne gubi prethodne sačuvane vrednosti
- [ ] **Tab-scenario:** Low-health i medical source se čuvaju zasebno od vanilla Health modifikatora
- [ ] **Tab-scenario:** Starvation, Radiation i Severe Fatigue imaju sve svoje source child parametre i ispravan parent dependency
- [ ] **Tab-scenario:** Master arrays se primenjuju samo kada korisnik stvarno uključi njihov parent, ne slučajno kroz druge tabove

### Low Health (2)

- [ ] **Master Low-Health Mechanics** — `advanced.lowHealthMechanics` · SIDRO, toggle, OFF/ON. *Očekivanje:* Turns on the extra low-health punishment used by vanilla Master.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Master Low-Health Recovery Penalty** — `health.masterLowHealthPenalty` · SIDRO, slider, raw `0…1`, step `0.01` · parent `advanced.lowHealthMechanics`. *Očekivanje:* Controls how strongly Master difficulty slows health recovery when you are badly wounded.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Medical Effect Modifier (4)

- [ ] **Master Bandage Healing** — `health.masterBandageModifier` · SIDRO, slider, raw `0…1`, step `0.01` · parent `advanced.masterEffectModifierMechanics`. *Očekivanje:* Controls how much of the normal Bandage healing remains while Master Consumable Effect Penalties are active. Vanilla Master keeps 25% of the normal heal.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Master Medkit Healing** — `health.masterMedkitModifier` · SIDRO, slider, raw `0…1`, step `0.01` · parent `advanced.masterEffectModifierMechanics`. *Očekivanje:* Controls how much of the normal Medkit healing remains while Master Consumable Effect Penalties are active. Vanilla Master keeps 71% of the normal heal.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Master Army Medkit Healing** — `health.masterArmyMedkitModifier` · SIDRO, slider, raw `0…1`, step `0.01` · parent `advanced.masterEffectModifierMechanics`. *Očekivanje:* Controls how much of the normal Army Medkit healing remains while Master Consumable Effect Penalties are active. Vanilla Master keeps 82% of the normal heal.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Master Consumable Effect Penalties** — `advanced.masterEffectModifierMechanics` · SIDRO, toggle, OFF/ON. *Očekivanje:* Turns on the extra consumable penalties used by vanilla Master. This includes the separate Bandage, Medkit and Army Medkit healing reductions shown below, plus …. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Starvation (6)

- [ ] **Master Starvation Penalties** — `advanced.starvationMechanics` · SIDRO, toggle, OFF/ON. *Očekivanje:* Enables the extra starvation punishment bundle that vanilla Master adds on top of the normal hunger system. It is a discrete mechanics switch, not another hunge…. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Starvation Stamina Drain** — `survival.starvationStaminaDrain` · SIDRO, slider, raw `0…4`, step `0.01` · parent `advanced.starvationMechanics`. *Očekivanje:* Changes the extra stamina cost of actions while the Master starvation penalty is active. Vanilla adds +200%.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Starvation Damage** — `survival.starvationDamage` · SIDRO, slider, raw `0…10`, step `0.5` · parent `advanced.starvationMechanics`. *Očekivanje:* The Master starvation bundle applies a dedicated Hunger damage effect with a vanilla value of 4 per charge.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Starvation Damage Interval** — `survival.starvationDamageInterval` · SIDRO, slider, raw `1…10`, step `1` · parent `advanced.starvationMechanics`. *Očekivanje:* Vanilla schedules the starvation damage charge every 10 seconds while the effect remains active.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Starvation Penalty Delay** — `survival.starvationPenaltyDelay` · SIDRO, slider, raw `0…6`, step `0.5` · parent `advanced.starvationMechanics`. *Očekivanje:* All three vanilla starvation penalties use a 3-second delay. This control keeps those delay values synchronized.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Starvation Health Recovery Penalty** — `survival.starvationHealthRegenPenalty` · SIDRO, slider, raw `0…1`, step `0.01` · parent `advanced.starvationMechanics`. *Očekivanje:* Vanilla Master applies a 100% health-recovery penalty while the starvation bundle is active.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Radiation (2)

- [ ] **Master Radiation Penalty** — `advanced.radiationMechanics` · SIDRO, toggle, OFF/ON. *Očekivanje:* Enables the additional Master radiation penalty rather than changing radiation accumulation or radiation damage itself.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Master Radiation Stamina Cost Increase** — `survival.radiationStaminaDrainPenalty` · SIDRO, slider, raw `0…1`, step `0.01` · parent `advanced.radiationMechanics`. *Očekivanje:* When Master Radiation Mechanics is active at heavy radiation, this adds an extra stamina cost to stamina-consuming actions. Vanilla is +50%; 0% removes the extr…. **V[ ] I[ ] P[ ] B[ ] G[ ]**

### Severe Fatigue (6)

- [ ] **Master Severe-Fatigue Penalties** — `advanced.fatigueMechanics` · SIDRO, toggle, OFF/ON. *Očekivanje:* Enables the Master-only severe-fatigue penalty bundle. This is separate from ordinary sleepiness build-up, aiming-time penalties and reload penalties.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe-Fatigue Max Stamina Loss** — `survival.severeFatigueMaxStamina` · SIDRO, slider, raw `0…0.4`, step `0.01` · parent `advanced.fatigueMechanics`. *Očekivanje:* Vanilla Master reduces maximum stamina by 20% while its severe-fatigue bundle is active.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe-Fatigue Carry Capacity Loss** — `survival.severeFatigueCarryWeight` · SIDRO, slider, raw `0…0.4`, step `0.01` · parent `advanced.fatigueMechanics`. *Očekivanje:* Vanilla Master applies a 20% loss to both the additional-inventory-weight and penalty-less-weight fatigue effects.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe-Fatigue Recoil Increase** — `survival.severeFatigueRecoil` · SIDRO, slider, raw `0…1.1`, step `0.01` · parent `advanced.fatigueMechanics`. *Očekivanje:* Vanilla Master adds a +55% recoil effect while the severe-fatigue bundle is active.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe-Fatigue Aim Sway** — `survival.severeFatigueAimSway` · SIDRO, slider, raw `0…0.8`, step `0.01` · parent `advanced.fatigueMechanics`. *Očekivanje:* Controls how much extra weapon sway Master severe fatigue adds while aiming.. **V[ ] I[ ] P[ ] B[ ] G[ ]**
- [ ] **Severe-Fatigue Shot Spread** — `survival.severeFatigueDispersion` · SIDRO, slider, raw `0…1`, step `0.01` · parent `advanced.fatigueMechanics`. *Očekivanje:* Changes how much severe fatigue increases weapon spread.. **V[ ] I[ ] P[ ] B[ ] G[ ]**

## Nije aktivna UI kontrola: deset kataloških ID-eva

Ovaj spisak **ne** služi za test klikabilnih kontrola. Očekuje se da aplikacija NE prikazuje fiktivne editore za ove izvorne ID-eve, mada su sačuvani u source katalogu:

- [ ] Ne postoji aktivni editor za `survival.drunknessThresholdScale` (Alcohol Stage Threshold Scale).
- [ ] Ne postoji aktivni editor za `stamina.sprintUnderRunSpeedRegenCoef` (Speed-Limited Sprint Stamina Effect).
- [ ] Ne postoji aktivni editor za `stamina.joggingRegenCoef` (Jogging-State Stamina Effect).
- [ ] Ne postoji aktivni editor za `stamina.slowRunRegenCoef` (Slow-Run-State Stamina Effect).
- [ ] Ne postoji aktivni editor za `stamina.sprintRegenCoef` (Full Sprint Stamina Effect).
- [ ] Ne postoji aktivni editor za `stamina.runRegenCoef` (Run-State Stamina Effect).
- [ ] Ne postoji aktivni editor za `economy.repairDifficultyCost` (Repair Cost (Difficulty)).
- [ ] Ne postoji aktivni editor za `environment.diamondHealthDamage` (Diamond Health Damage).
- [ ] Ne postoji aktivni editor za `environment.diamondBleeding` (Diamond Bleeding).
- [ ] Ne postoji aktivni editor za `alife.mutantSpawn.generic` (Global Mutant Spawn Count).

## 11 native međuzavisnosti / constraint testova

- [ ] **bleedingDamageSeverity**: `survival.bleedingLowDamage` ≤ `survival.bleedingMediumDamage` ≤ `survival.bleedingStrongDamage`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **fatigueAimPenaltySeverity**: `survival.fatigueLowAimPenalty` ≤ `survival.fatigueMediumAimPenalty` ≤ `survival.fatigueStrongAimPenalty`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **fatigueReloadPenaltySeverity**: `survival.fatigueMediumReloadPenalty` ≤ `survival.fatigueStrongReloadPenalty`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **waterStaminaDrainDepth**: `stamina.waterMediumDrain` ≤ `stamina.waterStrongDrain`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **fatigueStageThresholds**: `survival.sleepLowThreshold` ≤ `survival.sleepMediumThreshold` ≤ `survival.sleepStrongThreshold`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **hungerStageThresholds**: `survival.hungerLowThreshold` ≤ `survival.hungerMediumThreshold` ≤ `survival.hungerStrongThreshold`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **bleedingStageThresholds**: `survival.bleedingMediumThreshold` ≤ `survival.bleedingStrongThreshold`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **hungerStaminaRecoverySeverity**: `survival.hungerMediumStaminaRegen` ≤ `survival.hungerStrongStaminaRegen`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **repeatedHitProtectionRange**: `combat.accumulatedDamageWeightMin` ≤ `combat.accumulatedDamageWeightMax`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **repairPriceReputation**: `economy.repairFriend` ≤ `economy.repairNeutral` ≤ `economy.repairDisaffection` ≤ `economy.repairEnemy`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.
- [ ] **meleeStaminaCostStrength**: `stamina.meleeNormalCost` ≤ `stamina.meleeStrongCost`. Probaj na oba kraja, preklapanje ručki, nezavisni reset, Save/Load i ponovno računanje native clamping-a.

## Opšta regresija / build / gameplay

- [ ] Tab Reset vraća sve izvore unutar taba (uključujući composite članove) na trenutni reference preset; Reset All vraća sve tabove.
- [ ] Save/Load izmenjenog preseta sa vrednostima iz 3+ taba sačuva ceo source state, parent toggle stanja i sve special graph members.
- [ ] Fabrički profili (12) su prisutni; korisnički profili se ne presnimavaju pri ponovnom publish-u, pa ni kada korisnik menjа fabrički stock template.
- [ ] Promena samo jednog izvora generiše samo njegov očekivani CFG patch; promena više izvora pravilno kombinuje patcheve, bez gubljenja zajedničkih generatora.
- [ ] Build Difficulty iz validnog profila kreira ispravan .pak/.utoc/.ucas i instalira samo `zzzzz_TrueCustomDifficulty_` u sačuvani `~mods`.
- [ ] Bez odstupanja od izabrane reference no-op build pravilno uklanja stari TCD triplet i ostavlja nepovezane modove i korisničke preset fajlove.
- [ ] Settings: validan sačuvan `~mods` path je preduslov za instalaciju; Open ~mods folder se ne prikazuje za nepostojeću/nesačuvanu putanju.
- [ ] Build activity rail: dok je idle sakriven, ikonica miruje; za realni build pokazuje source stvarne faze (WRITING CFG / PACKING PAK / BUILDING IOSTORE / VERIFYING PACKAGE / PUBLISHING OUTPUT / INSTALLING / COMPLETE) bez lažnih procenata.
- [ ] Za vreme builda aplikacija zadržava tamnu temu (nikad bela WPF pozadina), radijacioni znak rotira, vrednosti/preseti/Settings ne mogu se menjati, nema konkurentnog builda.
- [ ] Build uspeh, build greška i vanilla/no-op vraćaju radijacioni znak u mirno stanje i uklanjaju progress rail pre dijaloga; neuspeh ostavlja raniji validan output/netaknute druge modove gde je predviđeno.
- [ ] Dva uzastopna `./publish.sh` zadržavaju inode foldera `publish/win-x64`, Dolphin ostaje konzistentan i `tools/`, `Presets/`, `Output/`, personal data ostaju.
- [ ] Relevantni `tools/pre_release.py static` i audit suite prođu, uz beleženje neuspeha. Statički audit nije Wine/gameplay PASS.
- [ ] Za runtime-gated vrednosti testira se samo kontrolisani fresh spawn/trader refresh/save; zabeleži bazu, custom, stvarni efekat i konflikt sa drugim modovima. Ne tvrditi gameplay PASS bez dokaza.

## Bug report šablon

```text
Datum / commit (BUILD-INFO):
Wine verzija / STALKER 2 verzija:
Rezolucija / UI scaling / reference difficulty:
Tab / grupa / parameter ID:
Faza (V/I/P/B/G):  Status (PASS/FAIL/BLOCKED/N/A):
Koraci za reprodukciju:
Očekivano:
Stvarno:
Dokaz (screenshot / JSON / CFG diff / build log / in-game):
```

**Pravilo:** nijedan test nije unapred označen kao PASS; opis očekivanog ponašanja je iz kataloga/source dokumentacije, nije obećanje runtime efekta bez proveravanja. Pri svakoj izmeni koda, grafičkih kontrola ili kataloga koja utiče na UI, obnoviti ovu listu i testirati pogođene stavke pre release-a.
