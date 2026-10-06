# World Weapon Master List Audit

Version: 0.9.26.44

## Result

The definitive current **base-game regular weapon pool is 36 models**.

This is derived from the actual `GameData/ItemPrototypes.cfg`, not from localization namespace naming:

- Start with every direct child of the game's weapon family templates.
- Exclude 3 non-gameplay launcher/test data placeholders.
- Exclude `GunGauss_SP` and `GunRpg7_GL` as special/progression weapons.
- The remaining **36** are regular weapon models suitable for the future world-distribution system.

The previous live shuffle pool contains 34 of those 36. The only two regular base models missing from that old pool are:

- `GunFora_ST` — **Fora-221**, Rifle, **T2**
- `GunPKP_MG` — **RPM-74**, MG, **T3**

No existing entry in the old 34-model pool is invalid.

## Localization namespace correction

Several legitimate base-game weapon prototypes deliberately specify localization SIDs with historic/internal content-pack namespaces:

- `GunKora_HG` -> `myriflepack_GunKora_HG`
- `GunThreeLine_SP` -> `myriflepack_GunThreeLine_SP`
- `GunFora230_PP` -> `nwpack_GunFora230_PP`
- `GunArev_ST` -> `nwpack_GunArev_ST`
- `GunSKP_DMR` -> `nwpack_GunSKP_SP`
- `GunGP3A_DMR` -> `nwpack_GunGP3A_DMR`

They are still real entries in the current base `ItemPrototypes.cfg`. Therefore these localization prefixes must **not** be used as a mod-only exclusion rule.

## Regular pool by family

### DMR (3)

| SID | Name | Tier | Cost |
|---|---|---:|---:|
| `GunMark_SP` | Mark I EMR | T2 | 28000 |
| `GunSKP_DMR` | СКП | T3 | 33500 |
| `GunGP3A_DMR` | GP3A | T4 | 41000 |

### MG (1)

| SID | Name | Tier | Cost |
|---|---|---:|---:|
| `GunPKP_MG` | RPM-74 | T3 | 30000 |

### Pistol (6)

| SID | Name | Tier | Cost |
|---|---|---:|---:|
| `GunPM_HG` | ПТМ | T1 | 950 |
| `GunUDP_HG` | UDP Compact | T1 | 1425 |
| `GunKora_HG` | Кора | T2 | 6000 |
| `GunM10_HG` | M10 Gordon | T2 | 5700 |
| `GunAPB_HG` | АПСБ | T3 | 14000 |
| `GunRhino_HG` | Rhino | T3 | 10000 |

### Rifle (10)

| SID | Name | Tier | Cost |
|---|---|---:|---:|
| `GunAK74_ST` | АКМ-74С | T1 | 10000 |
| `GunFora_ST` | Fora-221 | T2 | 15000 |
| `GunG37_ST` | GP37 | T2 | 17000 |
| `GunGvintar_ST` | ВС Винтар | T2 | 21000 |
| `GunM16_ST` | AR416 | T2 | 14000 |
| `GunArev_ST` | ARev | T3 | 27000 |
| `GunGrim_ST` | Гром С-14 | T3 | 25000 |
| `GunLavina_ST` | СА Лавина | T3 | 26000 |
| `GunDnipro_ST` | Дњипро | T4 | 34000 |
| `GunKharod_ST` | Харод | T4 | 28000 |

### SMG (6)

| SID | Name | Tier | Cost |
|---|---|---:|---:|
| `GunAKU_PP` | АКМ-74У | T1 | 6000 |
| `GunViper_PP` | Viper 5 | T1 | 3000 |
| `GunBucket_PP` | Букет С-2 | T2 | 12000 |
| `GunFora230_PP` | Fora-230 | T2 | 13500 |
| `GunIntegral_PP` | Integral A | T3 | 17000 |
| `GunZubr_PP` | Зубр 19 | T3 | 20000 |

### Shotgun (6)

| SID | Name | Tier | Cost |
|---|---|---:|---:|
| `GunObrez_SG` | Скраћена двоцевка | T1 | 1700 |
| `GunTOZ_SG` | ТОЗ-34 | T1 | 3000 |
| `GunM860_SG` | M860 „Cracker” | T2 | 8000 |
| `GunSPSA_SG` | SPSA-14 | T3 | 17000 |
| `GunD12_SG` | Сајга Д-12 | T4 | 24000 |
| `GunRam2_SG` | Ram 2 | T4 | 29000 |

### Sniper (4)

| SID | Name | Tier | Cost |
|---|---|---:|---:|
| `GunThreeLine_SP` | Тролинијка | T1 | 3500 |
| `GunSVDM_SP` | СВДМ-2 | T2 | 24000 |
| `GunM701_SP` | M701 Super | T3 | 35000 |
| `GunSVU_SP` | СВУ-МК С-3 | T4 | 39000 |

## Excluded direct-template weapons

| SID | Name | Reason |
|---|---|---|
| `GunRpg7_GL` | РПГ-7у | Special/progression weapon; excluded from regular random world pool |
| `GunGauss_SP` | Гаусова пушка | Special/progression weapon; excluded from regular random world pool |
| `UA_GLaunch_Weapon_Data_En` | — | Launcher/test data placeholder, not a player weapon model |
| `UA_GLaunch_Weapon_Data_Ru` | — | Launcher/test data placeholder, not a player weapon model |
| `EN_BuckLaunch_Data` | — | Launcher/test data placeholder, not a player weapon model |

## Localized weapons that are not current base ItemPrototypes

The Serbian localization also contains names for `GunMosina_SP` and `GunRPD_MG`, but neither SID exists in the supplied current base `GameData/ItemPrototypes.cfg` nor the supplied DLC1 `ItemPrototypes.cfg`.

They therefore do **not** enter the current base-game world weapon pool merely because localization strings exist for them. Localization can retain strings for content that is absent from the active prototype set.

## Pool contract for the next design

The future density/distribution system should therefore use:

- **36 regular weapon models**
- every model has a confirmed localization name
- every model has a progression tier
- each model may spawn at most once per build
- `GunGauss_SP`, `GunRpg7_GL`, unique derivatives, DLC special variants and test data remain outside the regular pool

As of v0.9.26.44, the live `world-weapon-shuffle-pool.csv` is promoted to this audited **36-model** regular pool.

The same master list is retained in:

`docs/generators/world-weapon-regular-pool-proposed.csv`

for audit traceability.
