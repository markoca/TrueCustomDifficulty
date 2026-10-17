# Progression gear phase 9 — explicit stash item to weapon setup join

Added `tools/join_progression_stash_weapon_ammo.py`. This joins the 14 approved stash weapon item SIDs with the caliber/default-ammo audit, but **requires a separately verified item-to-weapon-setup map**. It never assumes the item SID equals the weapon setup SID.

```bash
python tools/audit_progression_weapon_calibers.py --weapon-cfg /path/to/GameData/WeaponGeneralSetupPrototypes.cfg --ammo-cfg /path/to/GameData/ItemPrototypes.cfg --output /tmp/progression-calibers.csv
python tools/join_progression_stash_weapon_ammo.py --calibers /tmp/progression-calibers.csv --item-setup-map /path/to/verified-item-setup-map.csv --output /tmp/progression-stash-weapon-ammo.csv
```

The mapping CSV schema is `item_sid,weapon_setup_sid,evidence_source`; an empty schema-only example is in `tools/data/progression-item-setup-map.template.csv`. Populate it **only** from actual game-native item prototype setup references or other individually audited evidence. Do not fill it by string similarity. Use `--strict` to reject any unresolved stash weapon.

This step creates an explicit evidence join, not a verified populated mapping. No game CFG writer, UI control, or loot output changes. Before production: obtain the 14 exact setup references, validate ammo sibling ownership, and test whole-node BPatch semantics.
