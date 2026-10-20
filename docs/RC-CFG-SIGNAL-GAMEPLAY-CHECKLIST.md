## 2026-10-20 — RC candidate cleanup: diagnostic profile retired from app

The **RC - CFG Signal** diagnostic is no longer shipped, embedded or auto-created by the application. The preset picker contains the **12 official factory roles** plus any unrelated user profiles. During the first run after updating, the exact byte-for-byte stock diagnostic file is deleted. A user-edited diagnostic file is **moved without overwriting** into `Presets/Archived/` (with a numbered name if necessary), so it disappears from the picker without losing user changes. The 13-signal CFG contract remains in `tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json`, embedded **only in the test assembly**, not in the published EXE. This is release-candidate cleanup, **not** proof of a passing post-change build, Wine UI test, game-load test or in-game effects.

# RC – CFG Signal: testiranje u igri

> **Istorijski test-only fixture:** `tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json` · **Base:** Stalker · **Ciljni CFG:** S.T.A.L.K.E.R. 2 2.0.5  
> Ovo je isključivo **in-game** checklist-a. Štikliraj `[x]` tek kada je efekat stvarno opažen i ponovljen u igri — ne samo kada je BPatch generisan.

**Commit / build:** ____________________  
**Datum i verzija igre:** ____________________  
**Test save / Nova igra:** ____________________  
**Ostali modovi:** ____________________

## Ekonomija

- [ ] **01. Starting Coupons — 0 → 100.000**  
  Pokrenuta potpuno **nova igra** sa RC profilom; nakon početka provereno da igrač raspolaže sa 100.000 kupona. Postojeći save nije validan test.  
  *Dokaz / napomena:* ____________________

## Igrač, zdravlje i stamina

- [ ] **02. Maximum Carry Weight — 80 → 160**  
  Otvoren inventar, potvrđen novi osnovni maksimalni kapacitet bez efekata oklopa, artefakata i drugih buffova.  
  *Dokaz / napomena:* ____________________

- [ ] **03. Maximum Stamina — 100 → 200**  
  Upoređen kapacitet stamine sa Stalker referencom u istim uslovima opterećenja i akcija (npr. trajanje sprinta).  
  *Dokaz / napomena:* ____________________

- [ ] **04. Maximum Health — 100 → 200**  
  Potvrđen *stvarni* maksimalni HP kroz kontrolisanu štetu i lečenje; procentualni HUD bar sam po sebi nije dokaz.  
  *Dokaz / napomena:* ____________________

- [ ] **05. Medkit Healing — 70 → 140 HP**  
  Sa dovoljno izgubljenog HP-a upotrebljen jedan običan medkit i upoređena količina vraćenog zdravlja. Paziti da oporavak nije ograničen punim HP-om ili drugim aktivnim kaznama.  
  *Dokaz / napomena:* ____________________

- [ ] **06. Water Instant Stamina — 38% → 100%**  
  Stamina ispražnjena, popijena jedna voda, potvrđen trenutni oporavak. Za izolovano poređenje koristiti **isti MaxSP** na obe strane testa.  
  *Dokaz / napomena:* ____________________

## Borba

- [ ] **07. NPC Gun Damage — 0.85× → 0×**  
  Ljudski NPC pucao na igrača u ponovljivim uslovima; potvrđen izostanak osnovne štete od metaka. Ovo **ne isključuje** melee, statusne efekte i ostale izvore štete.  
  *Dokaz / napomena:* ____________________

- [ ] **08. Player Weapon Damage — 1× → 2×**  
  Upoređena šteta ili broj pogodaka do smrti mete sa istim oružjem, municijom, neprijateljem i delom tela.  
  *Dokaz / napomena:* ____________________

- [ ] **09. Player Weapon Wear — 1× → 2×**  
  Iz istog oružja i početnog stanja ispaljen jednak broj metaka; upoređen pad izdržljivosti.  
  *Dokaz / napomena:* ____________________

## Anomalije i radijacija

- [ ] **10. Global Anomaly Damage — 1× → 0×**  
  Testirano isto vreme izloženosti istoj štetnoj anomaliji; potvrđen očekivani efekat na HP. Zasebni statusni efekti ili radijacija mogu i dalje delovati.  
  *Dokaz / napomena:* ____________________

- [ ] **11. Vodka Radiation Removal — 50 → 100 poena**  
  Pri poznatoj početnoj radijaciji upotrebljena jedna votka; upoređena uklonjena količina sa referentnim profilom.  
  *Dokaz / napomena:* ____________________

## Mutanti i A-Life

- [ ] **12. Blind Dog Part Drop Chance — 15% → 100%**  
  Ubijeno više *podobnih* slepih pasa i proveren loot. Beležiti uzorak; eventualni dodatni uslovi loot generatora ne smeju se pripisati ovom Chance parametru.  
  *Dokaz / napomena:* ____________________

- [ ] **13. Blind Dog A-Life Group Cap — 4 / 6 / 8 / 12 → 12 / 18 / 24 / 36**  
  Posmatrani ponovljeni susreti pri poznatom rangu igrača; zabeleženo da li se u igri uopšte uočava očekivano povećanje dozvoljene grupe. **Veći CFG maksimum ne garantuje veliki spawn**; ako nema dokaza, ostaviti neštiklirano.  
  *Rang / opažen broj / dokaz:* ____________________

## Završni zaključak

**Potvrđeno u igri:** ____ / 13  
**Neuspešni testovi:** ____________________  
**Nisu mogli biti potvrđeni:** ____________________  
**Linkovi ka screenshotovima / video snimcima / save-ovima:** ____________________

**Napomena:** Porediti Stalker bez moda i RC profil pod istim uslovima, najbolje izolovanom promenom jednog parametra. Uspešan BPatch ili uspešno učitavanje igre nije automatski PASS za efekat. Neštiklirano znači *nije potvrđeno*, ne nužno da ne radi.

Detaljan vodič: [RC-CFG-SIGNAL-TEST.md](RC-CFG-SIGNAL-TEST.md).
