# World Gear Weapon Tier Matrix

Derived from direct regular weapon prototypes plus explicit `Trader_T1..T4_Guns_ItemGenerator` placement.
This is audit/reference data only; build code does not consume it yet.

## T1

- **Pistol:** `GunPM_HG`, `GunUDP_HG`
- **SMG:** `GunViper_PP`, `GunAKU_PP`
- **Rifle:** `GunAK74_ST`
- **Shotgun:** `GunObrez_SG`, `GunTOZ_SG`
- **Sniper:** `GunThreeLine_SP`

## T2

- **Pistol:** `GunM10_HG`, `GunKora_HG`
- **SMG:** `GunBucket_PP`, `GunFora230_PP`
- **Rifle:** `GunM16_ST`, `GunG37_ST`, `GunGvintar_ST`
- **Shotgun:** `GunM860_SG`
- **DMR:** `GunMark_SP`
- **Sniper:** `GunSVDM_SP`

## T3

- **Pistol:** `GunAPB_HG`, `GunRhino_HG`
- **SMG:** `GunIntegral_PP`, `GunZubr_PP`
- **Rifle:** `GunGrim_ST`, `GunLavina_ST`, `GunArev_ST`
- **Shotgun:** `GunSPSA_SG`
- **DMR:** `GunSKP_DMR`
- **Sniper:** `GunM701_SP`

## T4

- **Rifle:** `GunKharod_ST`, `GunDnipro_ST`
- **Shotgun:** `GunD12_SG`, `GunRam2_SG`
- **DMR:** `GunGP3A_DMR`
- **Sniper:** `GunSVU_SP`

## Review / excluded

- `GunFora_ST` — direct `TemplateRifle` prototype, but no explicit Trader tier evidence found; REVIEW.
- `GunGauss_SP` — special/endgame weapon; excluded from generic shuffle.
- `GunRpg7_GL` — launcher/special; excluded.
- Nonstandard launcher/data prototypes are excluded.
- Concrete-derived unique variants such as `Gun_Trophy_AR` are excluded from generic replacement pools.
