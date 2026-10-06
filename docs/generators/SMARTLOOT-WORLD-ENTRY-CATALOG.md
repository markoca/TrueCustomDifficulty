# World SmartLoot — Exact Entry Catalog

This is the complete exact rank/group/item catalog for the 13 world-facing SmartLoot prototypes deep-audited in `SMARTLOOT-WORLD-AUDIT.md`. Source line numbers are from `StashPrototypes.cfg`.

## `StashMedicine_Cheap`

**Domain:** MEDICAL  
**Prototype source line:** 110  
**Decision:** REVIEW — two independent consumable sets; exact fields are mapped, but no one-row medical-abundance scalar

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | ConsumablesParams | [0] | 117 | 0.f | 1.f | 3 |  |  |  |  |
| ERank::Newbie | ConsumablesParams | [1] | 130 | 0.f | 1.f | 3 |  |  |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | ConsumablesParams | [0] | [0] | 122 | Medkit |  | 5 | 3 | 5 |
| ERank::Newbie | ConsumablesParams | [1] | [0] | 135 | Bandage |  | 5 | 4 | 8 |

## `StashFood_Cheap`

**Domain:** FOOD / DRINK  
**Prototype source line:** 214  
**Decision:** REVIEW — three independent consumable sets; no one-row food-abundance scalar

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | ConsumablesParams | [0] | 221 | 0.f | 1.f | 3 |  |  |  |  |
| ERank::Newbie | ConsumablesParams | [1] | 234 | 0.f | 1.f | 3 |  |  |  |  |
| ERank::Newbie | ConsumablesParams | [2] | 247 | 0.f | 1.f | 3 |  |  |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | ConsumablesParams | [0] | [0] | 226 | Sausage |  | 5 | 1 | 2 |
| ERank::Newbie | ConsumablesParams | [1] | [0] | 239 | Bread |  | 5 | 1 | 2 |
| ERank::Newbie | ConsumablesParams | [2] | [0] | 252 | Energetic |  | 5 | 1 | 1 |

## `StashGrenade_Cheap`

**Domain:** GRENADES  
**Prototype source line:** 266  
**Decision:** REVIEW — one group but two weighted grenade items plus ItemSetCount; distinct from Smart grenade prototype

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | GrenadesParams | [0] | 273 | 0.f | 1.f | 5 |  |  |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | GrenadesParams | [0] | [0] | 278 | GrenadeRGD5 |  | 5 | 1 | 3 |
| ERank::Newbie | GrenadesParams | [0] | [1] | 284 | GrenadeF1 |  | 5 | 1 | 3 |

## `Stash_AmmoAll_Cheap`

**Domain:** AMMO  
**Prototype source line:** 298  
**Decision:** REVIEW — 96 rank/slot/caliber groups and 284 item rows; no global ammo scalar

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | PrimaryWeaponParams | [0] | 305 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [1] | 325 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [2] | 351 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [3] | 371 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [4] | 397 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [5] | 429 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [6] | 461 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [7] | 487 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [8] | 513 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [9] | 539 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [0] | 567 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [1] | 587 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [2] | 613 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [3] | 633 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [4] | 659 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [5] | 691 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [6] | 723 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [7] | 749 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [8] | 775 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [9] | 801 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Newbie | PistolWeaponParams | [0] | 829 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Newbie | PistolWeaponParams | [1] | 849 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Newbie | PistolWeaponParams | [2] | 875 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Newbie | PistolWeaponParams | [3] | 895 | 0.f | 1.f |  | EAmmoCaliber::A939 | 15 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [0] | 934 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [1] | 954 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [2] | 980 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [3] | 1000 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [4] | 1026 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [5] | 1058 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [6] | 1090 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [7] | 1116 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [8] | 1142 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [9] | 1168 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [0] | 1196 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [1] | 1216 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [2] | 1242 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [3] | 1262 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [4] | 1288 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [5] | 1320 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [6] | 1352 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [7] | 1378 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [8] | 1404 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [9] | 1430 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Experienced | PistolWeaponParams | [0] | 1458 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | PistolWeaponParams | [1] | 1478 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | PistolWeaponParams | [2] | 1504 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | PistolWeaponParams | [3] | 1524 | 0.f | 1.f |  | EAmmoCaliber::A939 | 15 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [0] | 1563 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [1] | 1583 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [2] | 1609 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [3] | 1629 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [4] | 1655 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [5] | 1687 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [6] | 1719 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [7] | 1745 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [8] | 1771 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [9] | 1797 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [0] | 1825 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [1] | 1845 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [2] | 1871 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [3] | 1891 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [4] | 1917 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [5] | 1949 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [6] | 1981 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [7] | 2007 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [8] | 2033 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [9] | 2059 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Veteran | PistolWeaponParams | [0] | 2087 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PistolWeaponParams | [1] | 2107 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | PistolWeaponParams | [2] | 2133 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | PistolWeaponParams | [3] | 2153 | 0.f | 1.f |  | EAmmoCaliber::A939 | 15 |  |  |
| ERank::Master | PrimaryWeaponParams | [0] | 2192 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [1] | 2212 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [2] | 2238 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [3] | 2258 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [4] | 2284 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [5] | 2316 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [6] | 2348 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | PrimaryWeaponParams | [7] | 2374 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | PrimaryWeaponParams | [8] | 2400 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | PrimaryWeaponParams | [9] | 2426 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [0] | 2454 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [1] | 2474 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [2] | 2500 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [3] | 2520 | 0.f | 1.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [4] | 2546 | 0.f | 1.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [5] | 2578 | 0.f | 1.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [6] | 2610 | 0.f | 1.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [7] | 2636 | 0.f | 1.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | SecondaryWeaponParams | [8] | 2662 | 0.f | 1.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | SecondaryWeaponParams | [9] | 2688 | 0.f | 1.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Master | PistolWeaponParams | [0] | 2716 | 0.f | 1.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [1] | 2736 | 0.f | 1.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [2] | 2762 | 0.f | 1.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [3] | 2782 | 0.f | 1.f |  | EAmmoCaliber::A939 | 15 |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | PrimaryWeaponParams | [0] | [0] | 311 | A918D |  | 3 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [0] | [1] | 317 | A918A |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [0] | 331 | A045D |  | 3 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [1] | 337 | A045A |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [2] | 343 | A045E |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [2] | [0] | 357 | A919D |  | 3 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [2] | [1] | 363 | A919A |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [0] | 377 | A545D |  | 3 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [1] | 383 | A545A |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [2] | 389 | A545E |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [4] | [0] | 403 | A939D |  | 3 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [4] | [1] | 409 | A939A |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [4] | [2] | 415 | A939S |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [4] | [3] | 421 | A939E |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [5] | [0] | 435 | A556D |  | 3 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [5] | [1] | 441 | A556A |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [5] | [2] | 447 | A556S |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [5] | [3] | 453 | A556E |  | 1 | 30 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [6] | [0] | 467 | A762SniperD |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [6] | [1] | 473 | A762SniperA |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [6] | [2] | 479 | A762SniperS |  | 3 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [7] | [0] | 493 | A012D |  | 3 | 10 | 10 |
| ERank::Newbie | PrimaryWeaponParams | [7] | [1] | 499 | A012A |  | 1 | 10 | 10 |
| ERank::Newbie | PrimaryWeaponParams | [7] | [2] | 505 | A012E |  | 1 | 10 | 10 |
| ERank::Newbie | PrimaryWeaponParams | [8] | [0] | 519 | A762SniperD |  | 3 | 50 | 50 |
| ERank::Newbie | PrimaryWeaponParams | [8] | [1] | 525 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Newbie | PrimaryWeaponParams | [8] | [2] | 531 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Newbie | PrimaryWeaponParams | [9] | [0] | 545 | A762NATOD |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [9] | [1] | 551 | A762NATOA |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [9] | [2] | 557 | A762NATOS |  | 3 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [0] | [0] | 573 | A918D |  | 3 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [0] | [1] | 579 | A918A |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [0] | 593 | A045D |  | 3 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [1] | 599 | A045A |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [2] | 605 | A045E |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [0] | 619 | A919D |  | 3 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [1] | 625 | A919A |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [3] | [0] | 639 | A545D |  | 3 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [3] | [1] | 645 | A545A |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [3] | [2] | 651 | A545E |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [4] | [0] | 665 | A939D |  | 3 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [4] | [1] | 671 | A939A |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [4] | [2] | 677 | A939S |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [4] | [3] | 683 | A939E |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [5] | [0] | 697 | A556D |  | 3 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [5] | [1] | 703 | A556A |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [5] | [2] | 709 | A556S |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [5] | [3] | 715 | A556E |  | 1 | 30 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [6] | [0] | 729 | A762SniperD |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [6] | [1] | 735 | A762SniperA |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [6] | [2] | 741 | A762SniperS |  | 3 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [7] | [0] | 755 | A012D |  | 3 | 10 | 10 |
| ERank::Newbie | SecondaryWeaponParams | [7] | [1] | 761 | A012A |  | 1 | 10 | 10 |
| ERank::Newbie | SecondaryWeaponParams | [7] | [2] | 767 | A012E |  | 1 | 10 | 10 |
| ERank::Newbie | SecondaryWeaponParams | [8] | [0] | 781 | A762SniperD |  | 3 | 50 | 50 |
| ERank::Newbie | SecondaryWeaponParams | [8] | [1] | 787 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Newbie | SecondaryWeaponParams | [8] | [2] | 793 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Newbie | SecondaryWeaponParams | [9] | [0] | 807 | A762NATOD |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [9] | [1] | 813 | A762NATOA |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [9] | [2] | 819 | A762NATOS |  | 3 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [0] | [0] | 835 | A918D |  | 3 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [0] | [1] | 841 | A918A |  | 1 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [1] | [0] | 855 | A045D |  | 3 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [1] | [1] | 861 | A045A |  | 1 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [1] | [2] | 867 | A045E |  | 1 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [2] | [0] | 881 | A919D |  | 3 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [2] | [1] | 887 | A919A |  | 1 | 20 | 20 |
| ERank::Newbie | PistolWeaponParams | [3] | [0] | 901 | A939D |  | 3 | 30 | 30 |
| ERank::Newbie | PistolWeaponParams | [3] | [1] | 907 | A939A |  | 1 | 30 | 30 |
| ERank::Newbie | PistolWeaponParams | [3] | [2] | 913 | A939S |  | 1 | 30 | 30 |
| ERank::Newbie | PistolWeaponParams | [3] | [3] | 919 | A939E |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [0] | 940 | A918D |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [1] | 946 | A918A |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [0] | 960 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [1] | 966 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [2] | 972 | A045E |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [0] | 986 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [1] | 992 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [0] | 1006 | A545D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [1] | 1012 | A545A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [2] | 1018 | A545E |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [0] | 1032 | A939D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [1] | 1038 | A939A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [2] | 1044 | A939S |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [3] | 1050 | A939E |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [0] | 1064 | A556D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [1] | 1070 | A556A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [2] | 1076 | A556S |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [3] | 1082 | A556E |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [6] | [0] | 1096 | A762SniperD |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [6] | [1] | 1102 | A762SniperA |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [6] | [2] | 1108 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [7] | [0] | 1122 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [7] | [1] | 1128 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [7] | [2] | 1134 | A012E |  | 1 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [8] | [0] | 1148 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [8] | [1] | 1154 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [8] | [2] | 1160 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [9] | [0] | 1174 | A762NATOD |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [9] | [1] | 1180 | A762NATOA |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [9] | [2] | 1186 | A762NATOS |  | 3 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [0] | 1202 | A918D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [1] | 1208 | A918A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [0] | 1222 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [1] | 1228 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [2] | 1234 | A045E |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [0] | 1248 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [1] | 1254 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [0] | 1268 | A545D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [1] | 1274 | A545A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [2] | 1280 | A545E |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [0] | 1294 | A939D |  | 2 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [1] | 1300 | A939A |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [2] | 1306 | A939S |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [3] | 1312 | A939E |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [0] | 1326 | A556D |  | 2 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [1] | 1332 | A556A |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [2] | 1338 | A556S |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [3] | 1344 | A556E |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [6] | [0] | 1358 | A762SniperD |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [6] | [1] | 1364 | A762SniperA |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [6] | [2] | 1370 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [7] | [0] | 1384 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [7] | [1] | 1390 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [7] | [2] | 1396 | A012E |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [8] | [0] | 1410 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [8] | [1] | 1416 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [8] | [2] | 1422 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [9] | [0] | 1436 | A762NATOD |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [9] | [1] | 1442 | A762NATOA |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [9] | [2] | 1448 | A762NATOS |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [0] | [0] | 1464 | A918D |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [0] | [1] | 1470 | A918A |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [1] | [0] | 1484 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [1] | [1] | 1490 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [1] | [2] | 1496 | A045E |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [2] | [0] | 1510 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [2] | [1] | 1516 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [3] | [0] | 1530 | A939D |  | 2 | 30 | 30 |
| ERank::Experienced | PistolWeaponParams | [3] | [1] | 1536 | A939A |  | 1 | 30 | 30 |
| ERank::Experienced | PistolWeaponParams | [3] | [2] | 1542 | A939S |  | 1 | 30 | 30 |
| ERank::Experienced | PistolWeaponParams | [3] | [3] | 1548 | A939E |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [0] | 1569 | A918D |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [1] | 1575 | A918A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [0] | 1589 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [1] | 1595 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [2] | 1601 | A045E |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [0] | 1615 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [1] | 1621 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [0] | 1635 | A545D |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [1] | 1641 | A545A |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [2] | 1647 | A545E |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [0] | 1661 | A939D |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [1] | 1667 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [2] | 1673 | A939S |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [3] | 1679 | A939E |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [0] | 1693 | A556D |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [1] | 1699 | A556A |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [2] | 1705 | A556S |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [3] | 1711 | A556E |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [0] | 1725 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [1] | 1731 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [2] | 1737 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [7] | [0] | 1751 | A012D |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [7] | [1] | 1757 | A012A |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [7] | [2] | 1763 | A012E |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [8] | [0] | 1777 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | PrimaryWeaponParams | [8] | [1] | 1783 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | PrimaryWeaponParams | [8] | [2] | 1789 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | PrimaryWeaponParams | [9] | [0] | 1803 | A762NATOD |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [9] | [1] | 1809 | A762NATOA |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [9] | [2] | 1815 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [0] | 1831 | A918D |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [1] | 1837 | A918A |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [0] | 1851 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [1] | 1857 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [2] | 1863 | A045E |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [0] | 1877 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [1] | 1883 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [0] | 1897 | A545D |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [1] | 1903 | A545A |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [2] | 1909 | A545E |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [0] | 1923 | A939D |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [1] | 1929 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [2] | 1935 | A939S |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [3] | 1941 | A939E |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [0] | 1955 | A556D |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [1] | 1961 | A556A |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [2] | 1967 | A556S |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [3] | 1973 | A556E |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [6] | [0] | 1987 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [6] | [1] | 1993 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [6] | [2] | 1999 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [7] | [0] | 2013 | A012D |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [7] | [1] | 2019 | A012A |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [7] | [2] | 2025 | A012E |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [8] | [0] | 2039 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | SecondaryWeaponParams | [8] | [1] | 2045 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | SecondaryWeaponParams | [8] | [2] | 2051 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | SecondaryWeaponParams | [9] | [0] | 2065 | A762NATOD |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [9] | [1] | 2071 | A762NATOA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [9] | [2] | 2077 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Veteran | PistolWeaponParams | [0] | [0] | 2093 | A918D |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [0] | [1] | 2099 | A918A |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [1] | [0] | 2113 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [1] | [1] | 2119 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [1] | [2] | 2125 | A045E |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [2] | [0] | 2139 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [2] | [1] | 2145 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [3] | [0] | 2159 | A939D |  | 2 | 40 | 40 |
| ERank::Veteran | PistolWeaponParams | [3] | [1] | 2165 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | PistolWeaponParams | [3] | [2] | 2171 | A939S |  | 1 | 40 | 40 |
| ERank::Veteran | PistolWeaponParams | [3] | [3] | 2177 | A939E |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [0] | [0] | 2198 | A918D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [1] | 2204 | A918A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [0] | 2218 | A045D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [1] | 2224 | A045A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [2] | 2230 | A045E |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [2] | [0] | 2244 | A919D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [2] | [1] | 2250 | A919A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [3] | [0] | 2264 | A545D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [1] | 2270 | A545A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [2] | 2276 | A545E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [4] | [0] | 2290 | A939D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [4] | [1] | 2296 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [4] | [2] | 2302 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [4] | [3] | 2308 | A939E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [5] | [0] | 2322 | A556D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [5] | [1] | 2328 | A556A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [5] | [2] | 2334 | A556S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [5] | [3] | 2340 | A556E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [6] | [0] | 2354 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [6] | [1] | 2360 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [6] | [2] | 2366 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [7] | [0] | 2380 | A012D |  | 1 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [7] | [1] | 2386 | A012A |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [7] | [2] | 2392 | A012E |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [8] | [0] | 2406 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [8] | [1] | 2412 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [8] | [2] | 2418 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [9] | [0] | 2432 | A762NATOD |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [9] | [1] | 2438 | A762NATOA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [9] | [2] | 2444 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [0] | [0] | 2460 | A918D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [1] | 2466 | A918A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [0] | 2480 | A045D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [1] | 2486 | A045A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [2] | 2492 | A045E |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [2] | [0] | 2506 | A919D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [2] | [1] | 2512 | A919A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [3] | [0] | 2526 | A545D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [1] | 2532 | A545A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [2] | 2538 | A545E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [4] | [0] | 2552 | A939D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [4] | [1] | 2558 | A939A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [4] | [2] | 2564 | A939S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [4] | [3] | 2570 | A939E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [5] | [0] | 2584 | A556D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [5] | [1] | 2590 | A556A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [5] | [2] | 2596 | A556S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [5] | [3] | 2602 | A556E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [6] | [0] | 2616 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [6] | [1] | 2622 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [6] | [2] | 2628 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [7] | [0] | 2642 | A012D |  | 1 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [7] | [1] | 2648 | A012A |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [7] | [2] | 2654 | A012E |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [8] | [0] | 2668 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [8] | [1] | 2674 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [8] | [2] | 2680 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [9] | [0] | 2694 | A762NATOD |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [9] | [1] | 2700 | A762NATOA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [9] | [2] | 2706 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Master | PistolWeaponParams | [0] | [0] | 2722 | A918D |  | 1 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [0] | [1] | 2728 | A918A |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [1] | [0] | 2742 | A045D |  | 1 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [1] | [1] | 2748 | A045A |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [1] | [2] | 2754 | A045E |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [2] | [0] | 2768 | A919D |  | 1 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [2] | [1] | 2774 | A919A |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [3] | [0] | 2788 | A939D |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [3] | [1] | 2794 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [3] | [2] | 2800 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [3] | [3] | 2806 | A939E |  | 2 | 40 | 40 |

## `StashGrenade_Smart`

**Domain:** GRENADES  
**Prototype source line:** 2820  
**Decision:** NARROW STRUCTURAL CANDIDATE — one group / one item; MaxSpawnChance is a single exact source field, but runtime endpoint semantics still require proof

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | GrenadesParams | [0] | 2827 | 0.f | 0.02f | 1 |  |  |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | GrenadesParams | [0] | [0] | 2832 | GrenadeRGD5 | 1 |  | 1 | 1 |

## `StashMedicine_Smart`

**Domain:** MEDICAL  
**Prototype source line:** 2846  
**Decision:** REVIEW — 11 rank/set groups with endpoint, ItemSetCount, Weight and count semantics; broad production use is mostly dual-layer

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | ConsumablesParams | [0] | 2853 | 0.1f | 0.7f | 3 |  |  |  |  |
| ERank::Newbie | ConsumablesParams | [1] | 2866 | 0.1f | 0.7f | 5 |  |  |  |  |
| ERank::Experienced | ConsumablesParams | [0] | 2886 | 0.1f | 0.7f | 4 |  |  |  |  |
| ERank::Experienced | ConsumablesParams | [1] | 2899 | 0.1f | 0.7f | 5 |  |  |  |  |
| ERank::Experienced | ConsumablesParams | [2] | 2912 | 0.1f | 0.7f | 2 |  |  |  |  |
| ERank::Veteran | ConsumablesParams | [0] | 2932 | 0.1f | 0.8f | 3 |  |  |  |  |
| ERank::Veteran | ConsumablesParams | [1] | 2951 | 0.1f | 0.7f | 3 |  |  |  |  |
| ERank::Veteran | ConsumablesParams | [2] | 2964 | 0.1f | 0.7f | 3 |  |  |  |  |
| ERank::Master | ConsumablesParams | [0] | 2984 | 0.1f | 0.7f | 3 |  |  |  |  |
| ERank::Master | ConsumablesParams | [1] | 3003 | 0.1f | 0.7f | 5 |  |  |  |  |
| ERank::Master | ConsumablesParams | [2] | 3016 | 0.1f | 0.7f | 3 |  |  |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | ConsumablesParams | [0] | [0] | 2858 | Medkit |  | 1 | 1 | 1 |
| ERank::Newbie | ConsumablesParams | [1] | [0] | 2871 | Bandage |  | 1 | 1 | 2 |
| ERank::Experienced | ConsumablesParams | [0] | [0] | 2891 | Medkit |  | 1 | 1 | 1 |
| ERank::Experienced | ConsumablesParams | [1] | [0] | 2904 | Bandage |  | 1 | 1 | 2 |
| ERank::Experienced | ConsumablesParams | [2] | [0] | 2917 | Antirad |  | 1 | 1 | 1 |
| ERank::Veteran | ConsumablesParams | [0] | [0] | 2937 | Medkit |  | 5 | 1 | 1 |
| ERank::Veteran | ConsumablesParams | [0] | [1] | 2943 | ArmyMedkit |  | 1 | 1 | 1 |
| ERank::Veteran | ConsumablesParams | [1] | [0] | 2956 | Bandage |  | 1 | 2 | 3 |
| ERank::Veteran | ConsumablesParams | [2] | [0] | 2969 | Antirad |  | 1 | 1 | 1 |
| ERank::Master | ConsumablesParams | [0] | [0] | 2989 | Medkit |  | 4 | 1 | 1 |
| ERank::Master | ConsumablesParams | [0] | [1] | 2995 | ArmyMedkit |  | 1 | 1 | 1 |
| ERank::Master | ConsumablesParams | [1] | [0] | 3008 | Bandage |  | 1 | 1 | 2 |
| ERank::Master | ConsumablesParams | [2] | [0] | 3021 | Antirad |  | 5 | 1 | 1 |

## `StashFood_Smart`

**Domain:** FOOD / DRINK  
**Prototype source line:** 3035  
**Decision:** REVIEW — 12 rank/set groups and mixed item Chance/Weight semantics; no one-row food-abundance scalar

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | ConsumablesParams | [0] | 3042 | 0.f | 0.01f | 5 |  |  |  |  |
| ERank::Newbie | ConsumablesParams | [1] | 3067 | 0.f | 0.05f | 1 |  |  |  |  |
| ERank::Newbie | ConsumablesParams | [2] | 3080 | 0.f | 0.01f | 1 |  |  |  |  |
| ERank::Experienced | ConsumablesParams | [0] | 3100 | 0.f | 0.01f | 4 |  |  |  |  |
| ERank::Experienced | ConsumablesParams | [1] | 3125 | 0.f | 0.01f | 2 |  |  |  |  |
| ERank::Experienced | ConsumablesParams | [2] | 3138 | 0.f | 0.01f | 2 |  |  |  |  |
| ERank::Veteran | ConsumablesParams | [0] | 3164 | 0.f | 0.01f | 4 |  |  |  |  |
| ERank::Veteran | ConsumablesParams | [1] | 3189 | 0.f | 0.01f | 2 |  |  |  |  |
| ERank::Veteran | ConsumablesParams | [2] | 3202 | 0.f | 0.01f | 2 |  |  |  |  |
| ERank::Master | ConsumablesParams | [0] | 3234 | 0.f | 0.01f | 5 |  |  |  |  |
| ERank::Master | ConsumablesParams | [1] | 3265 | 0.f | 0.01f | 3 |  |  |  |  |
| ERank::Master | ConsumablesParams | [2] | 3278 | 0.f | 0.01f | 3 |  |  |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | ConsumablesParams | [0] | [0] | 3047 | Bread |  | 1 | 2 | 3 |
| ERank::Newbie | ConsumablesParams | [0] | [1] | 3053 | CannedFood |  | 1 | 1 | 1 |
| ERank::Newbie | ConsumablesParams | [0] | [2] | 3059 | Sausage |  | 1 | 2 | 3 |
| ERank::Newbie | ConsumablesParams | [1] | [0] | 3072 | Energetic |  | 1 | 1 | 1 |
| ERank::Newbie | ConsumablesParams | [2] | [0] | 3085 | Vodka |  | 1 | 1 | 1 |
| ERank::Experienced | ConsumablesParams | [0] | [0] | 3105 | Bread | 1 |  | 1 | 3 |
| ERank::Experienced | ConsumablesParams | [0] | [1] | 3111 | CannedFood | 1 |  | 1 | 2 |
| ERank::Experienced | ConsumablesParams | [0] | [2] | 3117 | Sausage | 1 |  | 1 | 3 |
| ERank::Experienced | ConsumablesParams | [1] | [0] | 3130 | Energetic | 1 |  | 1 | 2 |
| ERank::Experienced | ConsumablesParams | [2] | [0] | 3143 | Water | 1 |  | 1 | 2 |
| ERank::Experienced | ConsumablesParams | [2] | [1] | 3149 | Vodka | 1 |  | 1 | 2 |
| ERank::Veteran | ConsumablesParams | [0] | [0] | 3169 | Bread | 1 |  | 1 | 3 |
| ERank::Veteran | ConsumablesParams | [0] | [1] | 3175 | CannedFood | 1 |  | 1 | 2 |
| ERank::Veteran | ConsumablesParams | [0] | [2] | 3181 | Sausage | 1 |  | 1 | 3 |
| ERank::Veteran | ConsumablesParams | [1] | [0] | 3194 | Energetic |  | 1 | 1 | 2 |
| ERank::Veteran | ConsumablesParams | [2] | [0] | 3207 | Water | 1 |  | 1 | 2 |
| ERank::Veteran | ConsumablesParams | [2] | [1] | 3213 | Vodka | 1 |  | 0 | 2 |
| ERank::Veteran | ConsumablesParams | [2] | [2] | 3219 | Beer | 1 |  | 0 | 2 |
| ERank::Master | ConsumablesParams | [0] | [0] | 3239 | Bread | 1 |  | 1 | 3 |
| ERank::Master | ConsumablesParams | [0] | [1] | 3245 | CannedFood | 1 |  | 1 | 2 |
| ERank::Master | ConsumablesParams | [0] | [2] | 3251 | Sausage | 1 |  | 1 | 3 |
| ERank::Master | ConsumablesParams | [0] | [3] | 3257 | Milk | 1 |  | 0 | 2 |
| ERank::Master | ConsumablesParams | [1] | [0] | 3270 | Energetic |  | 1 | 1 | 3 |
| ERank::Master | ConsumablesParams | [2] | [0] | 3283 | Water | 1 |  | 1 | 2 |
| ERank::Master | ConsumablesParams | [2] | [1] | 3289 | Vodka | 1 |  | 1 | 2 |
| ERank::Master | ConsumablesParams | [2] | [2] | 3295 | Beer | 1 |  | 0 | 2 |

## `Stash_AmmoNATO_Smart`

**Domain:** AMMO  
**Prototype source line:** 3309  
**Decision:** REVIEW — 67 rank/slot/caliber groups; includes one raw MinCount > MaxCount anomaly that must be preserved, not normalized

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | PrimaryWeaponParams | [0] | 3316 | 0.f | 0.09f |  | EAmmoCaliber::A045 | 10 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [1] | 3330 | 0.f | 0.09f |  | EAmmoCaliber::A919 | 15 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [2] | 3344 | 0.f | 0.01f |  | EAmmoCaliber::A556 | 20 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [3] | 3358 | 0.f | 0.09f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [0] | 3374 | 0.f | 0.09f |  | EAmmoCaliber::A045 | 15 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [1] | 3388 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 15 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [2] | 3402 | 0.f | 0.09f |  | EAmmoCaliber::A556 | 20 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [3] | 3416 | 0.f | 0.09f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | PistolWeaponParams | [0] | 3432 | 0.f | 0.09f |  | EAmmoCaliber::A045 | 10 |  |  |
| ERank::Newbie | PistolWeaponParams | [1] | 3446 | 0.f | 0.09f |  | EAmmoCaliber::A919 | 15 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [0] | 3467 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [1] | 3487 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [2] | 3507 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [3] | 3521 | 0.f | 0.01f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [4] | 3541 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [5] | 3555 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [6] | 3575 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [7] | 3595 | 0.f | 0.01f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [0] | 3617 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [1] | 3637 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [2] | 3657 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [3] | 3671 | 0.f | 0.01f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [4] | 3691 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [5] | 3705 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [6] | 3725 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [7] | 3745 | 0.f | 0.01f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Experienced | PistolWeaponParams | [0] | 3767 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | PistolWeaponParams | [1] | 3787 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | PistolWeaponParams | [2] | 3807 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 15 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [0] | 3828 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [1] | 3854 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [2] | 3874 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [3] | 3900 | 0.f | 0.01f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [4] | 3932 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [5] | 3952 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [6] | 3978 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [7] | 3998 | 0.f | 0.01f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [0] | 4020 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [1] | 4046 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [2] | 4066 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [3] | 4092 | 0.f | 0.01f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [4] | 4124 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [5] | 4144 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [6] | 4170 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [7] | 4190 | 0.f | 0.01f |  | EAmmoCaliber::A762NATO | 10 |  |  |
| ERank::Veteran | PistolWeaponParams | [0] | 4212 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | PistolWeaponParams | [1] | 4232 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | PistolWeaponParams | [2] | 4252 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 15 |  |  |
| ERank::Master | PrimaryWeaponParams | [0] | 4285 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [1] | 4311 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [2] | 4331 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [3] | 4363 | 0.f | 0.01f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [4] | 4395 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | PrimaryWeaponParams | [5] | 4421 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | PrimaryWeaponParams | [6] | 4447 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | PrimaryWeaponParams | [7] | 4473 | 0.f | 0.01f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [0] | 4501 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [1] | 4527 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [2] | 4547 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [3] | 4579 | 0.f | 0.01f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [4] | 4611 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [5] | 4637 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | SecondaryWeaponParams | [6] | 4663 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | SecondaryWeaponParams | [7] | 4689 | 0.f | 0.01f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Master | PistolWeaponParams | [0] | 4717 | 0.f | 0.01f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [1] | 4743 | 0.f | 0.01f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [2] | 4763 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 15 |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | PrimaryWeaponParams | [0] | [0] | 3322 | A045D |  | 1 | 2 | 4 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [0] | 3336 | A919D |  | 1 | 5 | 10 |
| ERank::Newbie | PrimaryWeaponParams | [2] | [0] | 3350 | A556D | 0.5 |  | 5 | 10 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [0] | 3364 | A012D |  | 1 | 1 | 3 |
| ERank::Newbie | SecondaryWeaponParams | [0] | [0] | 3380 | A045D |  | 1 | 2 | 4 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [0] | 3394 | A919D |  | 1 | 5 | 10 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [0] | 3408 | A556D | 0.1 |  | 5 | 10 |
| ERank::Newbie | SecondaryWeaponParams | [3] | [0] | 3422 | A012D |  | 1 | 1 | 3 |
| ERank::Newbie | PistolWeaponParams | [0] | [0] | 3438 | A045D |  | 1 | 2 | 4 |
| ERank::Newbie | PistolWeaponParams | [1] | [0] | 3452 | A919D |  | 1 | 5 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [0] | 3473 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [1] | 3479 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [0] | 3493 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [1] | 3499 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [0] | 3513 | A939D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [0] | 3527 | A556D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [1] | 3533 | A556A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [0] | 3547 | A762SniperS |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [0] | 3561 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [1] | 3567 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [6] | [0] | 3581 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [6] | [1] | 3587 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [7] | [0] | 3601 | A762NATOS |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [7] | [1] | 3607 | A762NATOA |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [0] | 3623 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [1] | 3629 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [0] | 3643 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [1] | 3649 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [0] | 3663 | A939D |  | 2 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [0] | 3677 | A556D |  | 2 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [1] | 3683 | A556A |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [0] | 3697 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [0] | 3711 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [1] | 3717 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [6] | [0] | 3731 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [6] | [1] | 3737 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [7] | [0] | 3751 | A762NATOS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [7] | [1] | 3757 | A762NATOA |  | 1 | 10 | 10 |
| ERank::Experienced | PistolWeaponParams | [0] | [0] | 3773 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [0] | [1] | 3779 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [1] | [0] | 3793 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [1] | [1] | 3799 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [2] | [0] | 3813 | A939D |  | 2 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [0] | 3834 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [1] | 3840 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [2] | 3846 | A045E |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [0] | 3860 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [1] | 3866 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [0] | 3880 | A939D |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [1] | 3886 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [2] | 3892 | A939S |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [0] | 3906 | A556D |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [1] | 3912 | A556A |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [2] | 3918 | A556S |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [3] | 3924 | A556E |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [0] | 3938 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [1] | 3944 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [0] | 3958 | A012D |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [1] | 3964 | A012A |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [2] | 3970 | A012E |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [0] | 3984 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [1] | 3990 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | PrimaryWeaponParams | [7] | [0] | 4004 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [7] | [1] | 4010 | A762NATOA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [0] | 4026 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [1] | 4032 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [2] | 4038 | A045E |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [0] | 4052 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [1] | 4058 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [0] | 4072 | A939D |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [1] | 4078 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [2] | 4084 | A939S |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [0] | 4098 | A556D |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [1] | 4104 | A556A |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [2] | 4110 | A556S |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [3] | 4116 | A556E |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [0] | 4130 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [1] | 4136 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [0] | 4150 | A012D |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [1] | 4156 | A012A |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [2] | 4162 | A012E |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [6] | [0] | 4176 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | SecondaryWeaponParams | [6] | [1] | 4182 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | SecondaryWeaponParams | [7] | [0] | 4196 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [7] | [1] | 4202 | A762NATOA |  | 1 | 25 | 25 |
| ERank::Veteran | PistolWeaponParams | [0] | [0] | 4218 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [0] | [1] | 4224 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [1] | [0] | 4238 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [1] | [1] | 4244 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [2] | [0] | 4258 | A939D |  | 2 | 40 | 40 |
| ERank::Veteran | PistolWeaponParams | [2] | [1] | 4264 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | PistolWeaponParams | [2] | [2] | 4270 | A939S |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [0] | [0] | 4291 | A045D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [1] | 4297 | A045A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [2] | 4303 | A045E |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [0] | 4317 | A919D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [1] | 4323 | A919A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [2] | [0] | 4337 | A939D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [1] | 4343 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [2] | 4349 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [3] | 4355 | A939E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [0] | 4369 | A556D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [1] | 4375 | A556A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [2] | 4381 | A556S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [3] | 4387 | A556E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [4] | [0] | 4401 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [4] | [1] | 4407 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [4] | [2] | 4413 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [5] | [0] | 4427 | A012D |  | 1 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [5] | [1] | 4433 | A012A |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [5] | [2] | 4439 | A012E |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [6] | [0] | 4453 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [6] | [1] | 4459 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [6] | [2] | 4465 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [7] | [0] | 4479 | A762NATOD |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [7] | [1] | 4485 | A762NATOA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [7] | [2] | 4491 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [0] | [0] | 4507 | A045D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [1] | 4513 | A045A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [2] | 4519 | A045E |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [0] | 4533 | A919D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [1] | 4539 | A919A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [2] | [0] | 4553 | A939D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [1] | 4559 | A939A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [2] | 4565 | A939S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [3] | 4571 | A939E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [0] | 4585 | A556D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [1] | 4591 | A556A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [2] | 4597 | A556S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [3] | 4603 | A556E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [4] | [0] | 4617 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [4] | [1] | 4623 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [4] | [2] | 4629 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [5] | [0] | 4643 | A012D |  | 1 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [5] | [1] | 4649 | A012A |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [5] | [2] | 4655 | A012E |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [6] | [0] | 4669 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [6] | [1] | 4675 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [6] | [2] | 4681 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [7] | [0] | 4695 | A762NATOD |  | 1 | 25 | 15 |
| ERank::Master | SecondaryWeaponParams | [7] | [1] | 4701 | A762NATOA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [7] | [2] | 4707 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Master | PistolWeaponParams | [0] | [0] | 4723 | A045D |  | 1 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [0] | [1] | 4729 | A045A |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [0] | [2] | 4735 | A045E |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [1] | [0] | 4749 | A919D |  | 1 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [1] | [1] | 4755 | A919A |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [2] | [0] | 4769 | A939D |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [2] | [1] | 4775 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [2] | [2] | 4781 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [2] | [3] | 4787 | A939E |  | 2 | 40 | 40 |

## `Stash_AmmoSNG_Smart`

**Domain:** AMMO  
**Prototype source line:** 4801  
**Decision:** REVIEW — 45 rank/slot/caliber groups with caliber-specific counts/weights

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | PrimaryWeaponParams | [0] | 4808 | 0.f | 0.09f |  | EAmmoCaliber::A918 | 10 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [1] | 4822 | 0.f | 0.09f |  | EAmmoCaliber::A545 | 20 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [2] | 4836 | 0.f | 0.09f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [3] | 4850 | 0.f | 0.09f |  | EAmmoCaliber::A762 | 20 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [0] | 4866 | 0.f | 0.09f |  | EAmmoCaliber::A918 | 10 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [1] | 4880 | 0.f | 0.09f |  | EAmmoCaliber::A545 | 20 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [2] | 4894 | 0.f | 0.09f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [3] | 4908 | 0.f | 0.09f |  | EAmmoCaliber::A762 | 20 |  |  |
| ERank::Newbie | PistolWeaponParams | [0] | 4924 | 0.f | 0.09f |  | EAmmoCaliber::A918 | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [0] | 4945 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [1] | 4965 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [2] | 4991 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [3] | 5005 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [4] | 5025 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [0] | 5047 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [1] | 5067 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [2] | 5093 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [3] | 5113 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [4] | 5133 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | PistolWeaponParams | [0] | 5155 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [0] | 5182 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [1] | 5202 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [2] | 5228 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [3] | 5254 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [4] | 5280 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [0] | 5308 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [1] | 5328 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [2] | 5354 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [3] | 5380 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [4] | 5406 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | PistolWeaponParams | [0] | 5434 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [0] | 5461 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [1] | 5481 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [2] | 5507 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [3] | 5539 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | PrimaryWeaponParams | [4] | 5565 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | PrimaryWeaponParams | [5] | 5591 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | SecondaryWeaponParams | [0] | 5619 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [1] | 5639 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [2] | 5665 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [3] | 5697 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [4] | 5723 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | SecondaryWeaponParams | [5] | 5749 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | PistolWeaponParams | [0] | 5777 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [1] | 5797 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | PrimaryWeaponParams | [0] | [0] | 4814 | A918D |  | 1 | 2 | 5 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [0] | 4828 | A545D |  | 1 | 5 | 10 |
| ERank::Newbie | PrimaryWeaponParams | [2] | [0] | 4842 | A012D |  | 1 | 1 | 3 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [0] | 4856 | A762SniperD |  | 3 | 5 | 10 |
| ERank::Newbie | SecondaryWeaponParams | [0] | [0] | 4872 | A918D |  | 1 | 2 | 4 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [0] | 4886 | A545D |  | 1 | 5 | 10 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [0] | 4900 | A012D |  | 1 | 1 | 3 |
| ERank::Newbie | SecondaryWeaponParams | [3] | [0] | 4914 | A762SniperD |  | 3 | 5 | 10 |
| ERank::Newbie | PistolWeaponParams | [0] | [0] | 4930 | A918D |  | 1 | 2 | 4 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [0] | 4951 | A918D |  | 2 | 20 | 60 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [1] | 4957 | A918A |  | 1 | 20 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [0] | 4971 | A545D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [1] | 4977 | A545A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [2] | 4983 | A545E |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [0] | 4997 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [0] | 5011 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [1] | 5017 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [0] | 5031 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [1] | 5037 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [0] | 5053 | A918D |  | 2 | 20 | 60 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [1] | 5059 | A918A |  | 1 | 20 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [0] | 5073 | A545D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [1] | 5079 | A545A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [2] | 5085 | A545E |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [0] | 5099 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [1] | 5105 | A762SniperA |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [0] | 5119 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [1] | 5125 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [0] | 5139 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [1] | 5145 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | PistolWeaponParams | [0] | [0] | 5161 | A918D |  | 2 | 20 | 40 |
| ERank::Experienced | PistolWeaponParams | [0] | [1] | 5167 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [0] | 5188 | A918D |  | 2 | 20 | 60 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [1] | 5194 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [0] | 5208 | A545D |  | 2 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [1] | 5214 | A545A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [2] | 5220 | A545E |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [0] | 5234 | A762SniperD |  | 2 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [1] | 5240 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [2] | 5246 | A762SniperS |  | 2 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [0] | 5260 | A012D |  | 2 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [1] | 5266 | A012A |  | 1 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [2] | 5272 | A012E |  | 1 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [0] | 5286 | A762SniperD |  | 2 | 100 | 100 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [1] | 5292 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [2] | 5298 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [0] | 5314 | A918D |  | 2 | 20 | 60 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [1] | 5320 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [0] | 5334 | A545D |  | 2 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [1] | 5340 | A545A |  | 1 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [2] | 5346 | A545E |  | 1 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [0] | 5360 | A762SniperD |  | 3 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [1] | 5366 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [2] | 5372 | A762SniperS |  | 3 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [0] | 5386 | A012D |  | 2 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [1] | 5392 | A012A |  | 1 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [2] | 5398 | A012E |  | 1 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [0] | 5412 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [1] | 5418 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [2] | 5424 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | PistolWeaponParams | [0] | [0] | 5440 | A918D |  | 2 | 20 | 40 |
| ERank::Veteran | PistolWeaponParams | [0] | [1] | 5446 | A918A |  | 1 | 20 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [0] | 5467 | A918D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [1] | 5473 | A918A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [0] | 5487 | A545D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [1] | [1] | 5493 | A545A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [1] | [2] | 5499 | A545E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [0] | 5513 | A939D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [1] | 5519 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [2] | 5525 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [3] | 5531 | A939E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [0] | 5545 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [3] | [1] | 5551 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [3] | [2] | 5557 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [4] | [0] | 5571 | A012D |  | 1 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [4] | [1] | 5577 | A012A |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [4] | [2] | 5583 | A012E |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [5] | [0] | 5597 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [5] | [1] | 5603 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [5] | [2] | 5609 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [0] | [0] | 5625 | A918D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [1] | 5631 | A918A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [0] | 5645 | A545D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [1] | [1] | 5651 | A545A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [1] | [2] | 5657 | A545E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [0] | 5671 | A939D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [1] | 5677 | A939A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [2] | 5683 | A939S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [3] | 5689 | A939E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [0] | 5703 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [3] | [1] | 5709 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [3] | [2] | 5715 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [4] | [0] | 5729 | A012D |  | 1 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [4] | [1] | 5735 | A012A |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [4] | [2] | 5741 | A012E |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [5] | [0] | 5755 | A762SniperD |  | 1 | 75 | 150 |
| ERank::Master | SecondaryWeaponParams | [5] | [1] | 5761 | A762SniperD |  | 2 | 75 | 150 |
| ERank::Master | SecondaryWeaponParams | [5] | [2] | 5767 | A762SniperD |  | 2 | 75 | 150 |
| ERank::Master | PistolWeaponParams | [0] | [0] | 5783 | A918D |  | 1 | 30 | 90 |
| ERank::Master | PistolWeaponParams | [0] | [1] | 5789 | A918A |  | 2 | 30 | 90 |
| ERank::Master | PistolWeaponParams | [1] | [0] | 5803 | A939D |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [1] | 5809 | A939A |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [2] | 5815 | A939S |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [3] | 5821 | A939E |  | 2 | 40 | 80 |

## `Stash_AmmoSNG_Smart_MainLoot`

**Domain:** AMMO  
**Prototype source line:** 5835  
**Decision:** SPECIAL / REVIEW — all 45 group endpoint pairs are 0/0 despite live production consumers; do not interpret endpoints as universal spawn chance

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | PrimaryWeaponParams | [0] | 5842 | 0.f | 0.f |  | EAmmoCaliber::A918 | 20 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [1] | 5856 | 0.f | 0.f |  | EAmmoCaliber::A545 | 40 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [2] | 5870 | 0.f | 0.f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [3] | 5884 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [0] | 5900 | 0.f | 0.f |  | EAmmoCaliber::A918 | 20 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [1] | 5914 | 0.f | 0.f |  | EAmmoCaliber::A545 | 40 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [2] | 5928 | 0.f | 0.f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [3] | 5942 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Newbie | PistolWeaponParams | [0] | 5958 | 0.f | 0.f |  | EAmmoCaliber::A918 | 20 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [0] | 5979 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [1] | 5999 | 0.f | 0.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [2] | 6025 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [3] | 6039 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [4] | 6059 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [0] | 6081 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [1] | 6101 | 0.f | 0.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [2] | 6127 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [3] | 6147 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [4] | 6167 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | PistolWeaponParams | [0] | 6189 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [0] | 6216 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [1] | 6236 | 0.f | 0.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [2] | 6262 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [3] | 6288 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [4] | 6314 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [0] | 6342 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [1] | 6362 | 0.f | 0.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [2] | 6388 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [3] | 6414 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [4] | 6440 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | PistolWeaponParams | [0] | 6468 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [0] | 6495 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [1] | 6515 | 0.f | 0.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [2] | 6541 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [3] | 6573 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | PrimaryWeaponParams | [4] | 6599 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | PrimaryWeaponParams | [5] | 6625 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | SecondaryWeaponParams | [0] | 6653 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [1] | 6673 | 0.f | 0.f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [2] | 6699 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [3] | 6731 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [4] | 6757 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | SecondaryWeaponParams | [5] | 6783 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | PistolWeaponParams | [0] | 6811 | 0.f | 0.f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [1] | 6831 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | PrimaryWeaponParams | [0] | [0] | 5848 | A918D |  | 1 | 20 | 20 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [0] | 5862 | A545D |  | 1 | 30 | 35 |
| ERank::Newbie | PrimaryWeaponParams | [2] | [0] | 5876 | A012D |  | 1 | 4 | 6 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [0] | 5890 | A762SniperD |  | 3 | 50 | 50 |
| ERank::Newbie | SecondaryWeaponParams | [0] | [0] | 5906 | A918D |  | 1 | 20 | 20 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [0] | 5920 | A545D |  | 1 | 30 | 35 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [0] | 5934 | A012D |  | 1 | 4 | 6 |
| ERank::Newbie | SecondaryWeaponParams | [3] | [0] | 5948 | A762SniperD |  | 3 | 50 | 50 |
| ERank::Newbie | PistolWeaponParams | [0] | [0] | 5964 | A918D |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [0] | 5985 | A918D |  | 2 | 20 | 60 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [1] | 5991 | A918A |  | 1 | 20 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [0] | 6005 | A545D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [1] | 6011 | A545A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [2] | 6017 | A545E |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [0] | 6031 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [0] | 6045 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [1] | 6051 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [0] | 6065 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [1] | 6071 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [0] | 6087 | A918D |  | 2 | 20 | 60 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [1] | 6093 | A918A |  | 1 | 20 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [0] | 6107 | A545D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [1] | 6113 | A545A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [2] | 6119 | A545E |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [0] | 6133 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [1] | 6139 | A762SniperA |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [0] | 6153 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [1] | 6159 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [0] | 6173 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [1] | 6179 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | PistolWeaponParams | [0] | [0] | 6195 | A918D |  | 2 | 20 | 40 |
| ERank::Experienced | PistolWeaponParams | [0] | [1] | 6201 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [0] | 6222 | A918D |  | 2 | 20 | 60 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [1] | 6228 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [0] | 6242 | A545D |  | 2 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [1] | 6248 | A545A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [2] | 6254 | A545E |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [0] | 6268 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [1] | 6274 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [2] | 6280 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [0] | 6294 | A012D |  | 2 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [1] | 6300 | A012A |  | 1 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [2] | 6306 | A012E |  | 1 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [0] | 6320 | A762SniperD |  | 2 | 100 | 100 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [1] | 6326 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [2] | 6332 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [0] | 6348 | A918D |  | 2 | 20 | 60 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [1] | 6354 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [0] | 6368 | A545D |  | 2 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [1] | 6374 | A545A |  | 1 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [2] | 6380 | A545E |  | 1 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [0] | 6394 | A762SniperD |  | 3 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [1] | 6400 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [2] | 6406 | A762SniperS |  | 3 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [0] | 6420 | A012D |  | 2 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [1] | 6426 | A012A |  | 1 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [2] | 6432 | A012E |  | 1 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [0] | 6446 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [1] | 6452 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [2] | 6458 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | PistolWeaponParams | [0] | [0] | 6474 | A918D |  | 2 | 20 | 40 |
| ERank::Veteran | PistolWeaponParams | [0] | [1] | 6480 | A918A |  | 1 | 20 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [0] | 6501 | A918D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [1] | 6507 | A918A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [0] | 6521 | A545D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [1] | [1] | 6527 | A545A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [1] | [2] | 6533 | A545E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [0] | 6547 | A939D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [1] | 6553 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [2] | 6559 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [3] | 6565 | A939E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [0] | 6579 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [3] | [1] | 6585 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [3] | [2] | 6591 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [4] | [0] | 6605 | A012D |  | 1 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [4] | [1] | 6611 | A012A |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [4] | [2] | 6617 | A012E |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [5] | [0] | 6631 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [5] | [1] | 6637 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [5] | [2] | 6643 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [0] | [0] | 6659 | A918D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [1] | 6665 | A918A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [0] | 6679 | A545D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [1] | [1] | 6685 | A545A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [1] | [2] | 6691 | A545E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [0] | 6705 | A939D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [1] | 6711 | A939A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [2] | 6717 | A939S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [3] | 6723 | A939E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [0] | 6737 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [3] | [1] | 6743 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [3] | [2] | 6749 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [4] | [0] | 6763 | A012D |  | 1 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [4] | [1] | 6769 | A012A |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [4] | [2] | 6775 | A012E |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [5] | [0] | 6789 | A762SniperD |  | 1 | 75 | 150 |
| ERank::Master | SecondaryWeaponParams | [5] | [1] | 6795 | A762SniperD |  | 2 | 75 | 150 |
| ERank::Master | SecondaryWeaponParams | [5] | [2] | 6801 | A762SniperD |  | 2 | 75 | 150 |
| ERank::Master | PistolWeaponParams | [0] | [0] | 6817 | A918D |  | 1 | 30 | 90 |
| ERank::Master | PistolWeaponParams | [0] | [1] | 6823 | A918A |  | 2 | 30 | 90 |
| ERank::Master | PistolWeaponParams | [1] | [0] | 6837 | A939D |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [1] | 6843 | A939A |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [2] | 6849 | A939S |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [3] | 6855 | A939E |  | 2 | 40 | 80 |

## `Stash_AmmoNATO_Smart_MainLoot`

**Domain:** AMMO  
**Prototype source line:** 6869  
**Decision:** SPECIAL / REVIEW — all 67 group endpoint pairs are 0/0 despite live production consumers; do not interpret endpoints as universal spawn chance

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | PrimaryWeaponParams | [0] | 6876 | 0.f | 0.f |  | EAmmoCaliber::A045 | 15 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [1] | 6890 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [2] | 6904 | 0.f | 0.f |  | EAmmoCaliber::A556 | 20 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [3] | 6918 | 0.f | 0.f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [0] | 6934 | 0.f | 0.f |  | EAmmoCaliber::A045 | 15 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [1] | 6948 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [2] | 6962 | 0.f | 0.f |  | EAmmoCaliber::A556 | 20 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [3] | 6976 | 0.f | 0.f |  | EAmmoCaliber::A012 | 6 |  |  |
| ERank::Newbie | PistolWeaponParams | [0] | 6992 | 0.f | 0.f |  | EAmmoCaliber::A045 | 15 |  |  |
| ERank::Newbie | PistolWeaponParams | [1] | 7006 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [0] | 7027 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [1] | 7047 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [2] | 7067 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [3] | 7081 | 0.f | 0.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [4] | 7101 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [5] | 7115 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [6] | 7135 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [7] | 7155 | 0.f | 0.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [0] | 7177 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [1] | 7197 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [2] | 7217 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [3] | 7231 | 0.f | 0.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [4] | 7251 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [5] | 7265 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [6] | 7285 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [7] | 7305 | 0.f | 0.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Experienced | PistolWeaponParams | [0] | 7327 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Experienced | PistolWeaponParams | [1] | 7347 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Experienced | PistolWeaponParams | [2] | 7367 | 0.f | 0.f |  | EAmmoCaliber::A939 | 15 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [0] | 7388 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [1] | 7414 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [2] | 7434 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [3] | 7460 | 0.f | 0.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [4] | 7492 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [5] | 7512 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [6] | 7538 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [7] | 7558 | 0.f | 0.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [0] | 7580 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [1] | 7606 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [2] | 7626 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [3] | 7652 | 0.f | 0.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [4] | 7684 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [5] | 7704 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [6] | 7730 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [7] | 7750 | 0.f | 0.f |  | EAmmoCaliber::A762NATO | 10 |  |  |
| ERank::Veteran | PistolWeaponParams | [0] | 7772 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Veteran | PistolWeaponParams | [1] | 7792 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Veteran | PistolWeaponParams | [2] | 7812 | 0.f | 0.f |  | EAmmoCaliber::A939 | 15 |  |  |
| ERank::Master | PrimaryWeaponParams | [0] | 7845 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [1] | 7871 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [2] | 7891 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [3] | 7923 | 0.f | 0.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [4] | 7955 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | PrimaryWeaponParams | [5] | 7981 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | PrimaryWeaponParams | [6] | 8007 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | PrimaryWeaponParams | [7] | 8033 | 0.f | 0.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [0] | 8061 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [1] | 8087 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [2] | 8107 | 0.f | 0.f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [3] | 8139 | 0.f | 0.f |  | EAmmoCaliber::A556 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [4] | 8171 | 0.f | 0.f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [5] | 8197 | 0.f | 0.f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | SecondaryWeaponParams | [6] | 8223 | 0.f | 0.f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | SecondaryWeaponParams | [7] | 8249 | 0.f | 0.f |  | EAmmoCaliber::A762NATO | 20 |  |  |
| ERank::Master | PistolWeaponParams | [0] | 8277 | 0.f | 0.f |  | EAmmoCaliber::A045 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [1] | 8303 | 0.f | 0.f |  | EAmmoCaliber::A919 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [2] | 8323 | 0.f | 0.f |  | EAmmoCaliber::A939 | 15 |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | PrimaryWeaponParams | [0] | [0] | 6882 | A045D |  | 1 | 10 | 15 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [0] | 6896 | A919D |  | 1 | 25 | 35 |
| ERank::Newbie | PrimaryWeaponParams | [2] | [0] | 6910 | A556D | 0.1 |  | 20 | 30 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [0] | 6924 | A012D |  | 1 | 4 | 6 |
| ERank::Newbie | SecondaryWeaponParams | [0] | [0] | 6940 | A045D |  | 1 | 10 | 15 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [0] | 6954 | A919D |  | 1 | 25 | 35 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [0] | 6968 | A556D | 0.1 |  | 20 | 30 |
| ERank::Newbie | SecondaryWeaponParams | [3] | [0] | 6982 | A012D |  | 1 | 4 | 6 |
| ERank::Newbie | PistolWeaponParams | [0] | [0] | 6998 | A045D |  | 1 | 10 | 15 |
| ERank::Newbie | PistolWeaponParams | [1] | [0] | 7012 | A919D |  | 1 | 25 | 35 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [0] | 7033 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [1] | 7039 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [0] | 7053 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [1] | 7059 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [0] | 7073 | A939D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [0] | 7087 | A556D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [1] | 7093 | A556A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [0] | 7107 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [0] | 7121 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [5] | [1] | 7127 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [6] | [0] | 7141 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [6] | [1] | 7147 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [7] | [0] | 7161 | A762NATOS |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [7] | [1] | 7167 | A762NATOA |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [0] | 7183 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [1] | 7189 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [0] | 7203 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [1] | 7209 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [0] | 7223 | A939D |  | 2 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [0] | 7237 | A556D |  | 2 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [1] | 7243 | A556A |  | 1 | 30 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [0] | 7257 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [0] | 7271 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [5] | [1] | 7277 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [6] | [0] | 7291 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [6] | [1] | 7297 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [7] | [0] | 7311 | A762NATOS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [7] | [1] | 7317 | A762NATOA |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [0] | [0] | 7333 | A045D |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [0] | [1] | 7339 | A045A |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [1] | [0] | 7353 | A919D |  | 2 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [1] | [1] | 7359 | A919A |  | 1 | 20 | 20 |
| ERank::Experienced | PistolWeaponParams | [2] | [0] | 7373 | A939D |  | 2 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [0] | 7394 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [1] | 7400 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [2] | 7406 | A045E |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [0] | 7420 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [1] | 7426 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [0] | 7440 | A939D |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [1] | 7446 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [2] | 7452 | A939S |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [0] | 7466 | A556D |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [1] | 7472 | A556A |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [2] | 7478 | A556S |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [3] | 7484 | A556E |  | 1 | 40 | 40 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [0] | 7498 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [1] | 7504 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [0] | 7518 | A012D |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [1] | 7524 | A012A |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [2] | 7530 | A012E |  | 1 | 15 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [0] | 7544 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [1] | 7550 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | PrimaryWeaponParams | [7] | [0] | 7564 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [7] | [1] | 7570 | A762NATOA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [0] | 7586 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [1] | 7592 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [2] | 7598 | A045E |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [0] | 7612 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [1] | 7618 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [0] | 7632 | A939D |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [1] | 7638 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [2] | 7644 | A939S |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [0] | 7658 | A556D |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [1] | 7664 | A556A |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [2] | 7670 | A556S |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [3] | 7676 | A556E |  | 1 | 40 | 40 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [0] | 7690 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [1] | 7696 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [0] | 7710 | A012D |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [1] | 7716 | A012A |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [5] | [2] | 7722 | A012E |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [6] | [0] | 7736 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | SecondaryWeaponParams | [6] | [1] | 7742 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Veteran | SecondaryWeaponParams | [7] | [0] | 7756 | A762NATOS |  | 1 | 15 | 15 |
| ERank::Veteran | SecondaryWeaponParams | [7] | [1] | 7762 | A762NATOA |  | 1 | 15 | 15 |
| ERank::Veteran | PistolWeaponParams | [0] | [0] | 7778 | A045D |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [0] | [1] | 7784 | A045A |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [1] | [0] | 7798 | A919D |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [1] | [1] | 7804 | A919A |  | 1 | 30 | 30 |
| ERank::Veteran | PistolWeaponParams | [2] | [0] | 7818 | A939D |  | 2 | 40 | 40 |
| ERank::Veteran | PistolWeaponParams | [2] | [1] | 7824 | A939A |  | 1 | 40 | 40 |
| ERank::Veteran | PistolWeaponParams | [2] | [2] | 7830 | A939S |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [0] | [0] | 7851 | A045D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [1] | 7857 | A045A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [2] | 7863 | A045E |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [0] | 7877 | A919D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [1] | 7883 | A919A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [2] | [0] | 7897 | A939D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [1] | 7903 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [2] | 7909 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [3] | 7915 | A939E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [0] | 7929 | A556D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [1] | 7935 | A556A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [2] | 7941 | A556S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [3] | 7947 | A556E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [4] | [0] | 7961 | A762SniperD |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [4] | [1] | 7967 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [4] | [2] | 7973 | A762SniperS |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [5] | [0] | 7987 | A012D |  | 1 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [5] | [1] | 7993 | A012A |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [5] | [2] | 7999 | A012E |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [6] | [0] | 8013 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [6] | [1] | 8019 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [6] | [2] | 8025 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [7] | [0] | 8039 | A762NATOD |  | 1 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [7] | [1] | 8045 | A762NATOA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [7] | [2] | 8051 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [0] | [0] | 8067 | A045D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [1] | 8073 | A045A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [2] | 8079 | A045E |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [0] | 8093 | A919D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [1] | 8099 | A919A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [2] | [0] | 8113 | A939D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [1] | 8119 | A939A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [2] | 8125 | A939S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [3] | 8131 | A939E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [0] | 8145 | A556D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [1] | 8151 | A556A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [2] | 8157 | A556S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [3] | 8163 | A556E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [4] | [0] | 8177 | A762SniperD |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [4] | [1] | 8183 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [4] | [2] | 8189 | A762SniperS |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [5] | [0] | 8203 | A012D |  | 1 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [5] | [1] | 8209 | A012A |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [5] | [2] | 8215 | A012E |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [6] | [0] | 8229 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [6] | [1] | 8235 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [6] | [2] | 8241 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [7] | [0] | 8255 | A762NATOD |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [7] | [1] | 8261 | A762NATOA |  | 2 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [7] | [2] | 8267 | A762NATOS |  | 1 | 25 | 25 |
| ERank::Master | PistolWeaponParams | [0] | [0] | 8283 | A045D |  | 1 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [0] | [1] | 8289 | A045A |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [0] | [2] | 8295 | A045E |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [1] | [0] | 8309 | A919D |  | 1 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [1] | [1] | 8315 | A919A |  | 2 | 30 | 30 |
| ERank::Master | PistolWeaponParams | [2] | [0] | 8329 | A939D |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [2] | [1] | 8335 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [2] | [2] | 8341 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PistolWeaponParams | [2] | [3] | 8347 | A939E |  | 2 | 40 | 40 |

## `Stash_Ammo_Smart_Cheap`

**Domain:** AMMO  
**Prototype source line:** 8361  
**Decision:** REVIEW — combines 36 groups at 0..0.01 with 4 groups at 0.1..0.8; rank/slot/caliber matrix must be preserved

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | SecondaryWeaponParams | [0] | 8368 | 0.1f | 0.8f |  | EAmmoCaliber::A012 | 4 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [1] | 8382 | 0.1f | 0.8f |  | EAmmoCaliber::A545 | 45 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [2] | 8396 | 0.1f | 0.8f |  | EAmmoCaliber::A919 | 45 |  |  |
| ERank::Newbie | PistolWeaponParams | [0] | 8412 | 0.1f | 0.8f |  | EAmmoCaliber::A918 | 16 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [0] | 8433 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [1] | 8453 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [2] | 8479 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [3] | 8493 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [4] | 8513 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [0] | 8535 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [1] | 8555 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [2] | 8581 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [3] | 8601 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [4] | 8621 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Experienced | PistolWeaponParams | [0] | 8643 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [0] | 8670 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [1] | 8690 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [2] | 8716 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [3] | 8742 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [4] | 8768 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [0] | 8796 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [1] | 8816 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [2] | 8842 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [3] | 8868 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | SecondaryWeaponParams | [4] | 8894 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | PistolWeaponParams | [0] | 8922 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [0] | 8949 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [1] | 8969 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [2] | 8995 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | PrimaryWeaponParams | [3] | 9027 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | PrimaryWeaponParams | [4] | 9053 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | PrimaryWeaponParams | [5] | 9079 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | SecondaryWeaponParams | [0] | 9107 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [1] | 9127 | 0.f | 0.01f |  | EAmmoCaliber::A545 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [2] | 9153 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |
| ERank::Master | SecondaryWeaponParams | [3] | 9185 | 0.f | 0.01f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | SecondaryWeaponParams | [4] | 9211 | 0.f | 0.01f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | SecondaryWeaponParams | [5] | 9237 | 0.f | 0.01f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | PistolWeaponParams | [0] | 9265 | 0.f | 0.01f |  | EAmmoCaliber::A918 | 30 |  |  |
| ERank::Master | PistolWeaponParams | [1] | 9285 | 0.f | 0.01f |  | EAmmoCaliber::A939 | 30 |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | SecondaryWeaponParams | [0] | [0] | 8374 | A012D |  | 1 | 1 | 3 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [0] | 8388 | A545D |  | 1 | 10 | 15 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [0] | 8402 | A919D |  | 1 | 10 | 15 |
| ERank::Newbie | PistolWeaponParams | [0] | [0] | 8418 | A918D |  | 1 | 6 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [0] | 8439 | A918D |  | 2 | 20 | 60 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [1] | 8445 | A918A |  | 1 | 20 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [0] | 8459 | A545D |  | 2 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [1] | 8465 | A545A |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [2] | 8471 | A545E |  | 1 | 30 | 30 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [0] | 8485 | A762SniperD |  | 2 | 20 | 20 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [0] | 8499 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [1] | 8505 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [0] | 8519 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [1] | 8525 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [0] | 8541 | A918D |  | 2 | 20 | 60 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [1] | 8547 | A918A |  | 1 | 20 | 30 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [0] | 8561 | A545D |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [1] | 8567 | A545A |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [2] | 8573 | A545E |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [0] | 8587 | A762SniperS |  | 2 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [1] | 8593 | A762SniperA |  | 1 | 20 | 20 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [0] | 8607 | A012D |  | 2 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [1] | 8613 | A012A |  | 1 | 10 | 10 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [0] | 8627 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [1] | 8633 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Experienced | PistolWeaponParams | [0] | [0] | 8649 | A918D |  | 2 | 20 | 40 |
| ERank::Experienced | PistolWeaponParams | [0] | [1] | 8655 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [0] | 8676 | A918D |  | 2 | 20 | 60 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [1] | 8682 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [0] | 8696 | A545D |  | 2 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [1] | 8702 | A545A |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [2] | 8708 | A545E |  | 1 | 30 | 30 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [0] | 8722 | A762SniperD |  | 2 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [1] | 8728 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [2] | 8734 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [0] | 8748 | A012D |  | 2 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [1] | 8754 | A012A |  | 1 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [2] | 8760 | A012E |  | 1 | 10 | 10 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [0] | 8774 | A762SniperD |  | 2 | 100 | 100 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [1] | 8780 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [2] | 8786 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [0] | 8802 | A918D |  | 2 | 20 | 60 |
| ERank::Veteran | SecondaryWeaponParams | [0] | [1] | 8808 | A918A |  | 1 | 20 | 30 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [0] | 8822 | A545D |  | 2 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [1] | 8828 | A545A |  | 1 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [1] | [2] | 8834 | A545E |  | 1 | 20 | 20 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [0] | 8848 | A762SniperD |  | 3 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [1] | 8854 | A762SniperA |  | 1 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [2] | [2] | 8860 | A762SniperS |  | 3 | 25 | 25 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [0] | 8874 | A012D |  | 2 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [1] | 8880 | A012A |  | 1 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [3] | [2] | 8886 | A012E |  | 1 | 10 | 10 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [0] | 8900 | A762SniperD |  | 2 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [1] | 8906 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | SecondaryWeaponParams | [4] | [2] | 8912 | A762SniperD |  | 1 | 50 | 50 |
| ERank::Veteran | PistolWeaponParams | [0] | [0] | 8928 | A918D |  | 2 | 20 | 40 |
| ERank::Veteran | PistolWeaponParams | [0] | [1] | 8934 | A918A |  | 1 | 20 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [0] | 8955 | A918D |  | 1 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [0] | [1] | 8961 | A918A |  | 2 | 30 | 30 |
| ERank::Master | PrimaryWeaponParams | [1] | [0] | 8975 | A545D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [1] | [1] | 8981 | A545A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [1] | [2] | 8987 | A545E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [0] | 9001 | A939D |  | 1 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [1] | 9007 | A939A |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [2] | 9013 | A939S |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [2] | [3] | 9019 | A939E |  | 2 | 40 | 40 |
| ERank::Master | PrimaryWeaponParams | [3] | [0] | 9033 | A762SniperD |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [3] | [1] | 9039 | A762SniperA |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [3] | [2] | 9045 | A762SniperS |  | 2 | 25 | 25 |
| ERank::Master | PrimaryWeaponParams | [4] | [0] | 9059 | A012D |  | 1 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [4] | [1] | 9065 | A012A |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [4] | [2] | 9071 | A012E |  | 2 | 15 | 15 |
| ERank::Master | PrimaryWeaponParams | [5] | [0] | 9085 | A762SniperD |  | 1 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [5] | [1] | 9091 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | PrimaryWeaponParams | [5] | [2] | 9097 | A762SniperD |  | 2 | 75 | 75 |
| ERank::Master | SecondaryWeaponParams | [0] | [0] | 9113 | A918D |  | 1 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [0] | [1] | 9119 | A918A |  | 2 | 30 | 30 |
| ERank::Master | SecondaryWeaponParams | [1] | [0] | 9133 | A545D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [1] | [1] | 9139 | A545A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [1] | [2] | 9145 | A545E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [0] | 9159 | A939D |  | 1 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [1] | 9165 | A939A |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [2] | 9171 | A939S |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [2] | [3] | 9177 | A939E |  | 2 | 40 | 40 |
| ERank::Master | SecondaryWeaponParams | [3] | [0] | 9191 | A762SniperD |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [3] | [1] | 9197 | A762SniperA |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [3] | [2] | 9203 | A762SniperS |  | 1 | 25 | 25 |
| ERank::Master | SecondaryWeaponParams | [4] | [0] | 9217 | A012D |  | 1 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [4] | [1] | 9223 | A012A |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [4] | [2] | 9229 | A012E |  | 2 | 15 | 15 |
| ERank::Master | SecondaryWeaponParams | [5] | [0] | 9243 | A762SniperD |  | 1 | 75 | 150 |
| ERank::Master | SecondaryWeaponParams | [5] | [1] | 9249 | A762SniperD |  | 2 | 75 | 150 |
| ERank::Master | SecondaryWeaponParams | [5] | [2] | 9255 | A762SniperD |  | 2 | 75 | 150 |
| ERank::Master | PistolWeaponParams | [0] | [0] | 9271 | A918D |  | 1 | 30 | 90 |
| ERank::Master | PistolWeaponParams | [0] | [1] | 9277 | A918A |  | 2 | 30 | 90 |
| ERank::Master | PistolWeaponParams | [1] | [0] | 9291 | A939D |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [1] | 9297 | A939A |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [2] | 9303 | A939S |  | 2 | 40 | 80 |
| ERank::Master | PistolWeaponParams | [1] | [3] | 9309 | A939E |  | 2 | 40 | 80 |

## `Stash_Ammo_Smart_CommonRare`

**Domain:** AMMO  
**Prototype source line:** 9323  
**Decision:** REVIEW — 32 groups at 0.1..0.8 with rank/slot/caliber-specific counts and weights; high fan-out and almost entirely dual-layer

### Group rows

| Rank | Group | Set | Line | MinSpawnChance | MaxSpawnChance | ItemSetCount | PriorityCaliber | MainWeaponAmmoCount | MaxPrice | Curve |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: | --- | --- |
| ERank::Newbie | PrimaryWeaponParams | [0] | 9330 | 0.1f | 0.8f |  | EAmmoCaliber::A012 | 4 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [1] | 9344 | 0.1f | 0.8f |  | EAmmoCaliber::A545 | 45 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [2] | 9358 | 0.1f | 0.8f |  | EAmmoCaliber::A919 | 45 |  |  |
| ERank::Newbie | PrimaryWeaponParams | [3] | 9372 | 0.1f | 0.8f |  | EAmmoCaliber::A045 | 45 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [0] | 9388 | 0.1f | 0.8f |  | EAmmoCaliber::A012 | 4 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [1] | 9402 | 0.1f | 0.8f |  | EAmmoCaliber::A545 | 45 |  |  |
| ERank::Newbie | SecondaryWeaponParams | [2] | 9416 | 0.1f | 0.8f |  | EAmmoCaliber::A919 | 45 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [0] | 9437 | 0.1f | 0.8f |  | EAmmoCaliber::A012 | 4 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [1] | 9451 | 0.1f | 0.8f |  | EAmmoCaliber::A545 | 45 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [2] | 9465 | 0.1f | 0.8f |  | EAmmoCaliber::A919 | 45 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [3] | 9479 | 0.1f | 0.8f |  | EAmmoCaliber::A556 | 45 |  |  |
| ERank::Experienced | PrimaryWeaponParams | [4] | 9493 | 0.1f | 0.8f |  | EAmmoCaliber::A045 | 45 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [0] | 9509 | 0.1f | 0.8f |  | EAmmoCaliber::A012 | 4 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [1] | 9523 | 0.1f | 0.8f |  | EAmmoCaliber::A545 | 45 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [2] | 9537 | 0.1f | 0.8f |  | EAmmoCaliber::A919 | 45 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [3] | 9551 | 0.1f | 0.8f |  | EAmmoCaliber::A556 | 45 |  |  |
| ERank::Experienced | SecondaryWeaponParams | [4] | 9565 | 0.1f | 0.8f |  | EAmmoCaliber::A045 | 45 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [0] | 9586 | 0.1f | 0.8f |  | EAmmoCaliber::A918 | 45 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [1] | 9600 | 0.1f | 0.8f |  | EAmmoCaliber::A545 | 45 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [2] | 9620 | 0.1f | 0.8f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [3] | 9646 | 0.1f | 0.8f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [4] | 9672 | 0.1f | 0.8f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [5] | 9698 | 0.1f | 0.8f |  | EAmmoCaliber::A556 | 45 |  |  |
| ERank::Veteran | PrimaryWeaponParams | [6] | 9712 | 0.1f | 0.8f |  | EAmmoCaliber::A045 | 45 |  |  |
| ERank::Master | PrimaryWeaponParams | [0] | 9733 | 0.1f | 0.8f |  | EAmmoCaliber::A918 | 45 |  |  |
| ERank::Master | PrimaryWeaponParams | [1] | 9753 | 0.1f | 0.8f |  | EAmmoCaliber::A545 | 45 |  |  |
| ERank::Master | PrimaryWeaponParams | [2] | 9779 | 0.1f | 0.8f |  | EAmmoCaliber::A939 | 45 |  |  |
| ERank::Master | PrimaryWeaponParams | [3] | 9811 | 0.1f | 0.8f |  | EAmmoCaliber::A762Sniper | 20 |  |  |
| ERank::Master | PrimaryWeaponParams | [4] | 9837 | 0.1f | 0.8f |  | EAmmoCaliber::A012 | 10 |  |  |
| ERank::Master | PrimaryWeaponParams | [5] | 9863 | 0.1f | 0.8f |  | EAmmoCaliber::A762 | 50 |  |  |
| ERank::Master | PrimaryWeaponParams | [6] | 9889 | 0.1f | 0.8f |  | EAmmoCaliber::A556 | 45 |  |  |
| ERank::Master | PrimaryWeaponParams | [7] | 9903 | 0.1f | 0.8f |  | EAmmoCaliber::A045 | 45 |  |  |

### Item rows

| Rank | Group | Set | Item node | Line | Item SID | Chance | Weight | MinCount | MaxCount |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| ERank::Newbie | PrimaryWeaponParams | [0] | [0] | 9336 | A012D |  | 1 | 1 | 3 |
| ERank::Newbie | PrimaryWeaponParams | [1] | [0] | 9350 | A545D |  | 1 | 10 | 15 |
| ERank::Newbie | PrimaryWeaponParams | [2] | [0] | 9364 | A919D |  | 1 | 10 | 15 |
| ERank::Newbie | PrimaryWeaponParams | [3] | [0] | 9378 | A045D |  | 1 | 3 | 7 |
| ERank::Newbie | SecondaryWeaponParams | [0] | [0] | 9394 | A012D |  | 1 | 1 | 3 |
| ERank::Newbie | SecondaryWeaponParams | [1] | [0] | 9408 | A545D |  | 1 | 10 | 15 |
| ERank::Newbie | SecondaryWeaponParams | [2] | [0] | 9422 | A919D |  | 1 | 10 | 15 |
| ERank::Experienced | PrimaryWeaponParams | [0] | [0] | 9443 | A012D |  | 1 | 1 | 3 |
| ERank::Experienced | PrimaryWeaponParams | [1] | [0] | 9457 | A545D |  | 1 | 10 | 15 |
| ERank::Experienced | PrimaryWeaponParams | [2] | [0] | 9471 | A919D |  | 1 | 10 | 15 |
| ERank::Experienced | PrimaryWeaponParams | [3] | [0] | 9485 | A556D |  | 1 | 10 | 15 |
| ERank::Experienced | PrimaryWeaponParams | [4] | [0] | 9499 | A045D |  | 1 | 3 | 7 |
| ERank::Experienced | SecondaryWeaponParams | [0] | [0] | 9515 | A012D |  | 1 | 1 | 3 |
| ERank::Experienced | SecondaryWeaponParams | [1] | [0] | 9529 | A545D |  | 1 | 10 | 15 |
| ERank::Experienced | SecondaryWeaponParams | [2] | [0] | 9543 | A919D |  | 1 | 10 | 15 |
| ERank::Experienced | SecondaryWeaponParams | [3] | [0] | 9557 | A556D |  | 1 | 10 | 15 |
| ERank::Experienced | SecondaryWeaponParams | [4] | [0] | 9571 | A045D |  | 1 | 3 | 7 |
| ERank::Veteran | PrimaryWeaponParams | [0] | [0] | 9592 | A918D |  | 1 | 10 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [0] | 9606 | A545D |  | 5 | 10 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [1] | [1] | 9612 | A545A |  | 0 | 10 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [0] | 9626 | A762SniperD |  | 5 | 25 | 27 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [1] | 9632 | A762SniperA |  | 0 | 25 | 27 |
| ERank::Veteran | PrimaryWeaponParams | [2] | [2] | 9638 | A762SniperS |  | 5 | 25 | 27 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [0] | 9652 | A012D |  | 10 | 4 | 5 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [1] | 9658 | A012A |  | 0 | 4 | 5 |
| ERank::Veteran | PrimaryWeaponParams | [3] | [2] | 9664 | A012E |  | 0 | 3 | 4 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [0] | 9678 | A762SniperD |  | 5 | 10 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [1] | 9684 | A762SniperD |  | 0 | 10 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [4] | [2] | 9690 | A762SniperD |  | 0 | 10 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [5] | [0] | 9704 | A556D |  | 1 | 10 | 15 |
| ERank::Veteran | PrimaryWeaponParams | [6] | [0] | 9718 | A045D |  | 1 | 3 | 7 |
| ERank::Master | PrimaryWeaponParams | [0] | [0] | 9739 | A918D |  | 5 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [0] | [1] | 9745 | A918A |  | 1 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [1] | [0] | 9759 | A545D |  | 5 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [1] | [1] | 9765 | A545A |  | 0 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [1] | [2] | 9771 | A545E |  | 0 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [2] | [0] | 9785 | A939D |  | 5 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [2] | [1] | 9791 | A939A |  | 0 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [2] | [2] | 9797 | A939S |  | 1 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [2] | [3] | 9803 | A939E |  | 0 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [3] | [0] | 9817 | A762SniperD |  | 5 | 24 | 27 |
| ERank::Master | PrimaryWeaponParams | [3] | [1] | 9823 | A762SniperA |  | 0 | 24 | 27 |
| ERank::Master | PrimaryWeaponParams | [3] | [2] | 9829 | A762SniperS |  | 5 | 24 | 27 |
| ERank::Master | PrimaryWeaponParams | [4] | [0] | 9843 | A012D |  | 5 | 3 | 4 |
| ERank::Master | PrimaryWeaponParams | [4] | [1] | 9849 | A012A |  | 1 | 3 | 4 |
| ERank::Master | PrimaryWeaponParams | [4] | [2] | 9855 | A012E |  | 0 | 3 | 4 |
| ERank::Master | PrimaryWeaponParams | [5] | [0] | 9869 | A762SniperD |  | 5 | 20 | 25 |
| ERank::Master | PrimaryWeaponParams | [5] | [1] | 9875 | A762SniperD |  | 0 | 20 | 25 |
| ERank::Master | PrimaryWeaponParams | [5] | [2] | 9881 | A762SniperD |  | 0 | 20 | 25 |
| ERank::Master | PrimaryWeaponParams | [6] | [0] | 9895 | A556D |  | 1 | 10 | 15 |
| ERank::Master | PrimaryWeaponParams | [7] | [0] | 9909 | A045D |  | 1 | 3 | 7 |

