# Generic Zombie Weapon Source Audit — v0.2.16

This audit corrects the older parent-backed interpretation and freezes the actual generic-zombie weapon source boundary before any Zombie Weapon Condition / Ammo UI implementation. It is based on the resolved generator catalog, not just inheritance links.

## Corrected runtime model

Across **93 generic production zombie roots**:

- **81** roots author local `WeaponPrimary` rows (**791 rows** total).
- **85** roots have a local `SubItemGenerator` link to one of **12 shared `GeneralNPC_*_WeaponPistol` pools**.
- **75** roots have both local primary weapons and a pistol link.
- **10** roots are pistol-only.
- **6** roots are local-primary-only.
- **2** roots have neither source and must remain weaponless: `CNPPZombie_Scientists_Recon_ItemGenerator` and `CNPPZombie_Scientists_Stormtrooper_ItemGenerator`.

The previous audit treated 12 CNPP children as if they inherited the parent `GeneralNPC_*` `WeaponPrimary` branches. **That is not what the resolved catalog contains.** The child-local category arrays replace that effective branch. Those 52 parent rows are therefore historical reference only and must not be materialized into zombie children.

## Actual tuning surface

| Surface | Rows / links | Safe zombie-only strategy |
|---|---:|---|
| Local primary weapons | 791 rows / 81 roots | edit child-local `MinDurability/MaxDurability` and/or attached ammo fields only |
| Shared pistol pools | 71 pool rows / 12 pools / 85 zombie links | clone each pool to a TCD-owned pistol pool and redirect only the 85 zombie links |
| Weaponless roots | 2 roots | leave untouched; never invent a weapon branch |
| Grenades | 91 local rows | independent candidate; edit only local `Chance` |

Together, condition/ammo tuning has **862 source weapon rows**: 791 primary + 71 pistol-pool rows. All 862 source rows use vanilla durability **0.25–0.50**. Ammo ranges remain source-specific and must be scaled per row rather than replaced with a single count.

## Why the pistol pools must be cloned

The 12 pistol pools are shared with living human NPCs and, for several factions, corpse/scripted paths. A global edit would therefore change non-zombie gameplay. The future safe implementation is **12 TCD pistol clones + 85 zombie-only link redirects**. The original `Chance` on each zombie pistol link remains unchanged; only the cloned pool weapon condition/ammo rows are tuned.

## Pistol pool inventory

The shared pistol surface contains **71 rows** and only five pistol SIDs: `GunPM_HG`, `GunUDP_HG`, `GunAPB_HG`, `GunKora_HG`, `GunM10_HG`. Selection `Weight` values remain vanilla.

## Local primary inventory

The 791 local-primary rows contain **32 unique weapon SIDs**. Most frequent source appearances are: `GunM860_SG` ×50, `GunG37_ST` ×47, `GunFora230_PP` ×45, `GunViper_PP` ×44, `GunBucket_PP` ×41, `GunSVDM_SP` ×41, `GunSPSA_SG` ×38, `GunAKU_PP` ×38, `GunAK74_ST` ×37, `GunM16_ST` ×33.

## Control decisions

| Control | Decision | Reason |
|---|---|---|
| **Zombie Weapon Condition** | **NO UI / AUDIT ONLY** | complete source boundary is known, but this loot/loadout control is intentionally not exposed |
| **Zombie Weapon Ammo** | **NO UI / AUDIT ONLY** | complete source boundary is known, but attached-ammo tuning remains documentation-only |
| **Zombie Grenade Frequency** | **IMPLEMENTED / RUNTIME TEST REQUIRED** | 91 exact child-local Chance rows; v0.2.17 scales source Chance only and is independent of weapon-source correction |
| Zombie Detector Chance | **NO UI** | no observable useful loot effect |

“Structurally ready” does **not** yet mean the UI controls are enabled. We still require an in-game proof that generated weapon durability and attached ammo fields survive the zombie death/loot path exactly as expected.

## Machine-readable companions

- `generic-zombie-weapon-source-boundary.csv` — all 93 roots classified as primary+pistol, primary-only, pistol-only, or weaponless.
- `generic-zombie-pistol-links.csv` — the exact 85 zombie-to-shared-pistol links and their vanilla link Chance.
- `generic-zombie-pistol-pool-entries.csv` — all 71 rows from the 12 shared pistol pools plus blast-radius metadata.
- `generic-zombie-historical-parent-blast-radius.csv` — why the old GeneralNPC parents must not be patched, while also documenting that their WeaponPrimary rows are not inherited.
- `generic-zombie-weapon-source-summary.json` — assertion summary.
