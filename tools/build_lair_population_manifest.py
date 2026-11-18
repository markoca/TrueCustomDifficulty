#!/usr/bin/env python3
"""Build src/TrueCustomDifficulty/Data/lair-population-manifest.csv from the
game's LairPrototypes.cfg (base + DLC1).

One row per eligible faction/rank MaxSpawnQuantity. HUMAN / MUTANT decides
which LAIR POPULATION card scales it; the three
refill timers are scaled by REFILL SPEED for every row. Everything else is listed in the
EXCLUDED_* sets with the reason; see docs/ALIFE-LAIR-POPULATION.md.

Usage: build_lair_population_manifest.py BASE_LairPrototypes.cfg DLC1_LairPrototypes.cfg
"""
import csv
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from lair_cfg_parse import rows  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "src/TrueCustomDifficulty/Data/lair-population-manifest.csv"

HUMAN = {
    "Bandits", "Corpus", "Diggers", "Duty", "EnemyVarta", "Freedom", "IkarVarta",
    "Mercenaries", "Militaries", "Monolith", "NeutralBandits", "NeutralMSOP",
    "Neutrals", "Noon", "Spark", "SultanBandits", "Varta",
}
MUTANT = {
    "Bayun", "Blinddog", "Bloodsucker", "Boar", "Burer", "Chimera", "Controller",
    "Deer", "Flesh", "MoldyBlinddog", "Poltergeist", "Pseudodog", "Pseudogiant",
    "Rat", "Snork", "Tushkan", "Zombie",
}
# Not difficulty: friendly scientists, story / quest factions.
EXCLUDED_FACTIONS = {
    "Scientists": "friendly non-combat camps",
    "SIRCAA_Scientist": "story location",
    "MALACHITE_Scientist": "story location",
    "FW_Duty": "DLC1 story faction",
    "FW_Freedom": "DLC1 story faction",
    "Object68": "DLC1 story location",
}


def excluded_prototype(name: str) -> str:
    if name.startswith("["):
        return "empty test prototype"
    if name.startswith("Guard"):
        return "settlement guard post"
    if name.startswith(("SIRCAA_", "MALACHITE_", "FW_")) or name in ("Object68", "RestingLairDefault"):
        return "story / special prototype"
    return ""


def main(base: str, dlc: str) -> None:
    out = []
    for scope, file, path in (("baseGame", "GameData/LairPrototypes.cfg", base),
                              ("dlc1", "DLCGameData/DLC1/LairPrototypes.cfg", dlc)):
        for r in rows(path):
            if excluded_prototype(r["proto"]) or r["faction"] in EXCLUDED_FACTIONS:
                continue
            group = "human" if r["faction"] in HUMAN else "mutant" if r["faction"] in MUTANT else None
            if group is None:
                raise SystemExit(f"Unclassified lair faction {r['faction']} in {r['proto']}")
            out.append([scope, file, r["proto"], r["node"], r["faction"], r["rank"], group, r["max"],
                        r["initial"], r["full"], r["wipe"]])
    with OUT.open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(["Scope", "File", "Prototype", "FactionNode", "Faction", "Rank", "Group", "SourceMax",
                    "InitialRespawnSeconds", "FullRespawnSeconds", "WipeRespawnSeconds"])
        w.writerows(out)
    print(f"{len(out)} rows -> {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    main(sys.argv[1], sys.argv[2])
