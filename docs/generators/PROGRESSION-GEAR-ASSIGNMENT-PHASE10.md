# Progression gear phase 10 — source-based item/setup reference audit

`tools/audit_progression_item_setup_references.py` searches the original extracted `ItemPrototypes.cfg` for **exact direct** weapon setup reference fields for the distinct weapon item SIDs in the 14-slot allowlist, and verifies the referenced setup exists in `WeaponGeneralSetupPrototypes.cfg`. It reports one row per distinct weapon SID, with explicit statuses for absent, ambiguous or unresolved evidence.

```bash
python tools/audit_progression_item_setup_references.py --item-cfg /path/to/GameData/ItemPrototypes.cfg --weapon-cfg /path/to/GameData/WeaponGeneralSetupPrototypes.cfg --output /tmp/progression-item-setup-audit.csv
```

**No inference:** only explicit direct fields `WeaponGeneralSetupSID`, `WeaponGeneralSetupPrototypeSID`, `WeaponSetupSID`, or `WeaponSetupPrototypeSID` count. These candidate field spellings must themselves be checked against the actual source. An absent direct reference is **not** proof the item lacks a weapon setup: inherited or indirect references need a separate verified inheritance resolver. The audit output includes a `status` column and is **not directly accepted** by the phase-9 join tool; review and export only validated rows as `item_sid,weapon_setup_sid,evidence_source`.

No original extracted CFG was available in the GitHub repository during this commit; no source-derived mapping was invented. No runtime patches, UI changes or automatic weapon+ammo movement. Next production gates remain source verification, inherited setup resolution, ammo ownership and tested whole-node patch semantics.
