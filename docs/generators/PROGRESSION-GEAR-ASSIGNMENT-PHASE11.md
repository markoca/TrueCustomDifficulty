# Progression gear phase 11 — inherited weapon setup references

The item/setup audit now follows explicit `struct.begin {ref: ParentSID}` chains in extracted `ItemPrototypes.cfg`. It verifies that a referenced weapon setup SID exists in `WeaponGeneralSetupPrototypes.cfg` and distinguishes `EXACT_DIRECT_REFERENCE` from `EXACT_INHERITED_REFERENCE`.

Safety guards reject missing/duplicate prototypes, inheritance cycles, multiple setup references, and missing weapon setups. The `evidence_source` field includes the actual inheritance chain. `--strict` exits unsuccessfully unless all distinct item SIDs from the 14 approved stash slots resolve.

Run:
```bash
python tools/audit_progression_item_setup_references.py --item-cfg /path/to/GameData/ItemPrototypes.cfg --weapon-cfg /path/to/GameData/WeaponGeneralSetupPrototypes.cfg --output /tmp/progression-item-setup-audit.csv --strict
```

The four candidate setup field spellings and inheritance format must be checked against real game data. The script does not assume item SID equals setup SID or invent a mapping. No production writer, UI, or game patch changes are made. A validated three-column `item_sid,weapon_setup_sid,evidence_source` file is still required by the phase-9 join tool.
