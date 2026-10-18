## 2026-10-18 — FULL STASH WEAPON SOURCE AUDIT / REMOVAL POLICY (CURRENT)

Original extracted **base game v2.0.5** source: `STALKER2-GameData-CFG(20261017-181645).zip`; corresponding DLC1 source: `STALKER2-DLC1-CFG(10).zip`. Audited physical `ESpawnType::ItemContainer` actors on `WorldMap_WP` **and quest world sublevels**, across both `SpawnActorPrototypes.cfg` files. Followed all explicit `ItemGeneratorSettings[*].PrototypeSID` references, direct `WeaponPrimary`/`WeaponSecondary`/`WeaponPistol` `PossibleItems` (without assuming `Gun` in the name) and nested `SubItemGenerator` links, including **DLC actor references to base GameData generators**. The source-audit result is `src/TrueCustomDifficulty/Data/stash-weapon-source-inventory.csv`; `tools/audit_all_stash_weapons.py` reproducibly checks the source ZIPs and fails on any added/missing weapon entry or classification drift.

| Classification | CFG weapon entries | Policy |
| --- | ---: | --- |
| Approved ordinary base-game stash weapons | **13** | Remove original weapon item node when Extra Stash Gear ON |
| Quest-associated base and DLC1 | **16** | Keep source reward entirely unchanged |
| Potentially unique `Gun_Trophy_AR` | **1** | Keep; identity requires proof |
| Source-protected STC Malachite stash | **1** | Keep under 106-actor protected gate |
| DLC1 cross-archive weapons in non-allowlisted/special actors | **8** | Keep pending dedicated quest/identity/actor eligibility validation |
| **Total source-authored entries** | **39** | **13 remove / 26 keep** |

These are **39 distinct source entries in 38 physical actors**, not guaranteed live loot spawns. In particular, eight unapproved DLC1 actors reference ordinary weapon SIDs from base-game generator definitions, but their physical actors are not in the DLC1 112-actor approval manifest. Named quest/special generators and `Gun_Trophy_AR` remain protected without guessing. `StashPrototypes.cfg` contains no direct `Gun*` item declaration in this snapshot. The scan is CFG-literal and cannot prove third-party references, generated/previously opened inventory or engine runtime branch execution; nonresolved external generator references are reported, **not silently treated as weapon-free**.

**Production enforcement:** The exact 13-row `stash-authored-weapon-removal-manifest.csv` stays separate and intentionally unchanged. When the master checkbox is ON, the writer validates its 13 rows against the embedded 39-row classification catalog, also applying the existing regular and protected actor gates before emitting precisely scoped `PossibleItems/[i] : removenode` edits. There is no category-wide deletion and no patch to quest/unique/DLC1/special entries. When OFF, zero original weapon removals occur. Six existing compact tier gear controls, 0/RARE/MOD/FREQ meanings, 1+1 bonus template constraint and World Weapon Shuffle exclusivity remain unchanged. The user-facing UI and `parameters.json` parameter IDs do **not** change.

**Audit commands:** `python3 tools/audit_extra_stash_gear.py` validates catalog-to-writer source parity without raw ZIPs. For the original CFG source re-scan:
```bash
python3 tools/audit_all_stash_weapons.py \
  --game-zip "/path/to/STALKER2-GameData-CFG(20261017-181645).zip" \
  --dlc-zip "/path/to/STALKER2-DLC1-CFG(10).zip"
```
Source scan, static checks, C# compile, Wine and game testing are **distinct** forms of evidence; no claim of successful compile or live stash generation is made here. This section supersedes narrower older statements that the 14-row backlog covered every weapon-containing stash.

## Extra Stash Gear ON (updated 2026-10-18)

The table below documents the game's **original source**, not necessarily what will be present when the Extra Gear checkbox is enabled. The **13 non-Trophy** confirmed weapon rows now have their **original item entry explicitly removed** by the opt-in feature. This applies even when the six Extra Gear strength controls remain OFF. The **one `Gun_Trophy_AR` row remains intact** pending uniqueness review. STC Malachite's separate protected original weapon slot remains intact too. See [the machine-readable 13-row authorisation and removal policy](STASH-AUTHORED-WEAPON-REMOVAL-2026-10-18.md). The older statement below that all 14 entries remain untouched is historical, superseded by this checkbox-specific behavior. Source quantities remain an audit record, not a generated-mod promise.

# Existing authored weapon stashes — 2026-10-18

**14 documented physical stash-generator actors** offer existing authored weapons (9 distinct item SIDs). Original fields are source evidence, **not guaranteed in-game drops**. Each ActorSID is present as `REGULAR_SMARTLOOT` in the physical safety catalog. This is an authored-weapon subset, **not a claim to enumerate all dynamic procedural, quest or scripted weapon rewards**.

| Stash location / generator | Existing weapon | Native Chance | Native Weight | Min–Max count | ActorSID |
|---|---|---:|---:|---|---|
| `ChemicalPlant_ChemicalTanks_StashGenerator6` | `GunUDP_HG` | — | 1 | native–native | `8E3157704F88075AAB9F519551CDA57D` |
| `CityCenter_PolissyaHotel_StashGenerator3` | `GunRam2_SG` | 1 | 1000 | 1–1 | `07727ED446A8EB9D0F56B3ABB9542CAD` |
| `Garbage_DigSite_StashGenerator2` | `GunBucket_PP` | 1 | — | 2–2 | `C59BE52D481BCA2E1EB1CE816B67EF71` |
| `Garbage_Factory_StashGenerator4` | `GunM860_SG` | 1 | — | 1–1 | `B3F81A794CB50D8DD300F794E6FDAF25` |
| `Garbage_Plant_StashGenerator7` | `GunBucket_PP` | — | 1 | 1–1 | `4AB2B2CF48D0FC16B2E853BF27922273` |
| `Garbage_VehicleGraveyard_StashGenerator1` | `GunM860_SG` | 1 | — | native–native | `393A3DB04B0C6906EBF05299C8E3DEEE` |
| `Jupiter_Market_StashGenerator2` | `GunMark_SP` | 1 | 1000 | 1–1 | `A3B04A9949CA27AA3E62FD9F3CD08ECD` |
| `LesserZone_ARMS_StashGenerator2` | `GunObrez_SG` | 1 | — | 2–2 | `A4D6A1EA48DDDEE35883379DFFD93F81` |
| `LesserZone_WastewaterTreatmentPlant_StashGenerator1` | `GunUDP_HG` | — | 1 | native–native | `91D632284EAEDA16F6CD7098A676611F` |
| `Promzona_AhropromUnderground_StashGenerator3` | `GunMark_SP` | — | 1000 | native–native | `E67E4BD743B8868C0D9715826155DA1D` |
| `Promzona_StorageontheHill_StashGenerator2` | `Gun_Trophy_AR` | — | 1 | native–native | `1CBBBE27428A1766B7F911863C044833` |
| `Promzona_StorageontheHill_StashGenerator2` | `GunSVDM_SP` | 1 | — | 1–1 | `BB1EF91D4CEC1424FF522FBD6DCA3148` |
| `Promzona_Х5Lab_StashGenerator3` | `GunAPB_HG` | 1 | — | native–native | `837831724DFAE2B9000E369F7D767BA3` |
| `Wild_Island_WasteProcessingStation_StashGenerator3` | `GunSVDM_SP` | 1 | — | native–native | `0CC8DAA64C554EF5C010439FE1CC448B` |

Sources: `docs/generators/stash-gear-unique-randomization-backlog.csv` and `src/TrueCustomDifficulty/Data/physical-stash-safety-catalog.csv`.

These **14 original authored slots are unchanged** by Extra Stash Gear. `Gun_Trophy_AR` is specially reserved and excluded from all new bonus pools, as are quest and unique gear. Existing authored two-weapon counts (including `MinCount=2`) remain untouched. The new bonus-entry 1+1 design is separate; no claim of an absolute physical-stash two-item cap is made without in-game testing.

The wider native ItemGenerator and SmartLoot pools can produce additional gear through branches not represented by these 14 fixed authored weapon rows. A complete exhaustive physical list of runtime weapon-containing stashes requires validating all native consumer references and actual game generation; this reference report deliberately does not infer unverified weapon odds.
