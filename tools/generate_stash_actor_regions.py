#!/usr/bin/env python3
"""Generate an offline, evidence-labelled physical stash region atlas from world X/Y."""
from __future__ import annotations

import argparse
import csv
import io
import json
import math
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
SOURCE = DATA / "smartloot-regular-world-actor-manifest.csv"
BOUNDARIES = DATA / "stash-region-boundaries.json"
OUTPUT = DATA / "stash-regular-actor-regions.csv"
ALIASES = {
    "LesserZone": "Lesser Zone",
    "Cordon": "Cordon",
    "Garbage": "Garbage",
    "WildIsland": "Wild Island",
    "Wild": "Wild Island",
    "Zaton": "Zaton",
    "Backwater": "Zaton",
    "RedForest": "Red Forest",
    "BurntForest": "Burnt Forest",
    "Yantar": "Yantar",
    "CementFactory": "Cement Factory",
    "CoolingTowers": "Cooling Towers",
    "ChemicalPlant": "Chemical Plant",
    "Promzona": "Chemical Plant",
    "Malachite": "Malachite",
    "STCMalachite": "Malachite",
    "Swamps": "Swamps",
    "Swamp": "Swamps",
    "Duga": "Duga",
    "DugaRadar": "Duga",
    "Jupiter": "Jupiter",
    "Rostok": "Rostok",
    "SIRCAA": "SIRCAA",
    "SIIRCA": "SIRCAA",
    "CityCenter": "Rostok"
}
FIELDS = ["ActorSID","Region","RegionEvidence","OriginalItemGeneratorSIDs","DLC","X","Y","Z","GeneratorRegionHint","HintAgreement","ClosestRegion","ClosestBoundaryDistanceUnits"]

def inside(x: float, y: float, ring: list[list[float]]) -> bool:
    """Point-in-polygon using original world coords; include points precisely on edges."""
    hit = False
    prev = len(ring) - 1
    for i, (xi, yi) in enumerate(ring):
        xj, yj = ring[prev]
        cross = (xj - xi) * (y - yi) - (yj - yi) * (x - xi)
        if abs(cross) < 1e-8 and min(xi, xj) <= x <= max(xi, xj) and min(yi, yj) <= y <= max(yi, yj):
            return True
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
            hit = not hit
        prev = i
    return hit

def boundary_distance(x: float, y: float, ring: list[list[float]]) -> float:
    """Shortest distance to any polygon boundary segment in native world units."""
    distance = float("inf")
    for i, (a, b) in enumerate(ring):
        c, d = ring[(i + 1) % len(ring)]
        dx, dy = c - a, d - b
        t = max(0.0, min(1.0, ((x - a) * dx + (y - b) * dy) / (dx * dx + dy * dy or 1.0)))
        distance = min(distance, math.hypot(x - a - t * dx, y - b - t * dy))
    return distance

def generate() -> list[dict[str, str]]:
    metadata = json.loads(BOUNDARIES.read_text(encoding="utf-8"))
    boundaries = metadata["polygons"]
    assert len(boundaries) == 22 and len({p["region"] for p in boundaries}) == 22
    with SOURCE.open(encoding="utf-8", newline="") as f:
        actors = list(csv.DictReader(f))
    result = []
    seen = set()
    for actor in actors:
        sid = actor["ActorSID"]
        assert sid not in seen, f"Duplicate actor SID: {sid}"
        seen.add(sid)
        x, y = float(actor["X"]), float(actor["Y"])
        assert math.isfinite(x) and math.isfinite(y), sid
        matches = [p["region"] for p in boundaries if inside(x, y, p["polygon"])]
        assert len(matches) <= 1, f"Overlapping region polygons for {sid}: {matches}"
        region = matches[0] if matches else "UNRESOLVED"
        hints = {ALIASES[g.split("_", 1)[0]] for g in actor["OriginalItemGeneratorSIDs"].split(",")
                 if g.split("_", 1)[0] in ALIASES}
        hint = next(iter(hints)) if len(hints) == 1 else "UNRESOLVED"
        agreement = ("NOT_IN_POLYGON" if region == "UNRESOLVED" else
                     "NO_GENERATOR_HINT" if hint == "UNRESOLVED" else
                     "AGREES" if hint == region else "CONFLICT")
        nearest, distance_units = "", ""
        if region == "UNRESOLVED":
            closest_distance, nearest = min(
                (boundary_distance(x, y, p["polygon"]), p["region"]) for p in boundaries
            )
            distance_units = f"{closest_distance:.3f}"
        result.append({
            "ActorSID": sid,
            "Region": region,
            "RegionEvidence": "POLYGON_INTERIOR" if matches else "OUTSIDE_PUBLISHED_POLYGON",
            "OriginalItemGeneratorSIDs": actor["OriginalItemGeneratorSIDs"],
            "DLC": actor["DLC"],
            "X": actor["X"], "Y": actor["Y"], "Z": actor["Z"],
            "GeneratorRegionHint": hint,
            "HintAgreement": agreement,
            "ClosestRegion": nearest,
            "ClosestBoundaryDistanceUnits": distance_units,
        })
    return result

def render(rows: list[dict[str, str]]) -> str:
    buffer = io.StringIO(newline="")
    writer = csv.DictWriter(buffer, fieldnames=FIELDS, lineterminator="\n")
    writer.writeheader()
    writer.writerows(rows)
    return buffer.getvalue()

def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Validate tracked CSV without writing")
    args = parser.parse_args()
    rows = generate()
    data = render(rows)
    if args.check:
        assert OUTPUT.is_file() and OUTPUT.read_text(encoding="utf-8") == data, (
            "Regional CSV out of date: run tools/generate_stash_actor_regions.py"
        )
        print(f"PASS: {len(rows)} physical ordinary actors, "
              f"{sum(row['Region'] != 'UNRESOLVED' for row in rows)} in regions, "
              f"{sum(row['HintAgreement'] == 'CONFLICT' for row in rows)} source conflicts")
    else:
        OUTPUT.write_text(data, encoding="utf-8")
        print(f"WROTE: {OUTPUT} ({len(rows)} actors; {dict(Counter(r['Region'] for r in rows))})")

if __name__ == "__main__":
    main()
