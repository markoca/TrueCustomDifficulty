# Progression gear — source CFG audit, 2026-10-18

Actual extracted base GameData source archive: `STALKER2-GameData-CFG(20261017-181645).zip`. The prior phase-8 and phase-11 scripts used incorrect assumed field syntax. This commit corrects them to **observed** game-native fields:

- Item prototype: `GeneralWeaponSetup = <setup SID>`.
- Prototype inheritance: `struct.begin {refkey=ParentSID}` (not `ref:`).
- Ammo prototype: `Caliber = EAmmoCaliber::...` and `AmmoType = EAmmoType::Default`.
- Weapon setup: `AmmoCaliber = EAmmoCaliber::...`.

Top-level CFG sections are parsed using unindented prototype declarations, avoiding nested `struct.begin` structures whose endings are not reliably counted by the previous parser.

Direct source inspection of **nine distinct weapon item SIDs** across the 14 allowlisted stash rows confirmed matching `GeneralWeaponSetup` references and existing weapon setup blocks:

| Item SID | Weapon setup caliber |
|---|---|
| GunUDP_HG | A045 |
| GunRam2_SG | A012 |
| GunBucket_PP | A918 |
| GunM860_SG | A012 |
| GunMark_SP | A762NATO |
| GunObrez_SG | A012 |
| GunSVDM_SP | A762Sniper |
| GunAPB_HG | A918 |
| GunGrim_ST | A939 |

These values are extracted-source observations, not inferred from names. The phase-8/11 corrected scripts were **not yet executed end-to-end** in the connected repository environment; independently inspecting these entries does not validate every writer/patch operation.

**Production remains blocked:** confirm sibling ammo category ownership, distinguish attached weapon ammo fields from separate loot ammo, and test whole-node BPatch replacement. No UI or production CFG writer changes.
