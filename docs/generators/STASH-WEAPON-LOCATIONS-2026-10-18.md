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
