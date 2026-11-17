# ARMY MEDKIT MIX — breakable medical boxes (2026-11-17)

`loot.medicalBoxArmyMedkitWeight` (hidden member of the Breakable Containers / AmmoDistribution
board, knob 0–3× / 0.25, vanilla 1×, `runtime_test_required`).

World medical boxes (`DestructibleStash_Med*` spawn actors, 711 + 3 × 553 + 5 in the base map) pick ONE
item per box from a Weight list that depends on the player rank:

| Rank | Generator | Medkit | Army Medkit | Eco Medkit | Army share (vanilla) |
|---|---|---:|---:|---:|---:|
| Newbie | `DestructibleStash_Med` (Medkit 1 / Bandage 2) | – | – | – | — |
| Experienced | `DestructibleStash_Medkit_Experienced` | 20 | 4 | 1 | 16% |
| Veteran | `DestructibleStash_Medkit_Veteran` | 20 | 10 | 5 | 29% |
| Master | `DestructibleStash_Medkit_Master` | 10 | 10 | 10 | 33% |

The control scales only the ArmyMedkit Weight (`ItemGenerator/[0]/PossibleItems/[1].Weight`, operation
`scale`, 3 direct edit points). 0× removes Army Medkits from the pick (the box then gives a Medkit or
Eco Medkit); 3× gives 36% / 55% / 60%. Box count stays with **Medical Boxes Removed**.

UI: below AMMO BOX CONTENT DISTRIBUTION, **MEDICAL BOXES** card = ARMY MEDKIT MIX knob + ARMY MEDKIT
PER BOX bars per rank (vanilla tick, only changed digits gold). Shares come from `MedicalBoxPolicy`.

Checks: `tools/audit_army_medkit.py` (with `TCD_BASE_DUMP` it re-reads the three source lists) and
`tools/audit_loot_direct_source.py` (kind `medicalBox`).
