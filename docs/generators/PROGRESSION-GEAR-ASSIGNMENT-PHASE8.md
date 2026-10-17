# Progression gear phase 8 — weapon caliber evidence extraction

New audit: `tools/audit_progression_weapon_calibers.py`.

Inputs are **extracted game-native** `WeaponGeneralSetupPrototypes.cfg` and `ItemPrototypes.cfg`. The tool collects direct `AmmoCaliber` assignments from weapon setup blocks and direct `AmmoCaliber` + `AmmoType = Default` from item blocks. It outputs an evidence-only table, rejecting missing or ambiguous default ammo candidates with `--strict`. It does not guess from names or use regional progression as a substitute for evidence.

```bash
python tools/audit_progression_weapon_calibers.py --weapon-cfg /path/to/GameData/WeaponGeneralSetupPrototypes.cfg --ammo-cfg /path/to/GameData/ItemPrototypes.cfg --output /tmp/weapon-calibers.csv
```

**Important limitation:** `weapon_setup_sid` is not necessarily the stash `ItemPrototypeSID`. A verified cross-reference between the setup SID and item SID is still required before joining this table with the 14 stash recipes. Additionally, CFG inheritance and alternate `AmmoType` values may require explicit resolution; the script deliberately reports only direct assignments and refuses to claim inferred matches. No production CFG patches or UI controls are emitted. Run against actual extracted CFG and inspect the output before treating it as authoritative.
