#!/usr/bin/env python3
"""Fail-closed static check for the source-backed stash geographic-region inventory."""
from generate_stash_actor_regions import BOUNDARIES, OUTPUT, generate, render

assert BOUNDARIES.is_file() and OUTPUT.is_file(), "Missing stash regional assets"
rows = generate()
assert rows, "Empty world stash manifest"
assert len(rows) == len({row["ActorSID"] for row in rows}), "Duplicate actor SID"
assert sum(row["Region"] != "UNRESOLVED" for row in rows) >= 800, "World-map polygon coverage drift"
assert OUTPUT.read_text(encoding="utf-8") == render(rows), (
    "Regional CSV has drifted from actor manifest/polygons; run tools/generate_stash_actor_regions.py"
)
by_id = {row["ActorSID"]: row for row in rows}
assert by_id["00312C9B4E94AEFFCFF3CF93F5017F49"]["Region"] == "Prypiat", (
    "CityCenter generator was incorrectly assigned to Rostok"
)
assert by_id["75B17AF54B2B381145BEFEB6A125FF0A"]["Region"] == "UNRESOLVED", (
    "A stash outside the polygons must not be assigned by nearest-neighbour inference"
)
print(f"PASS: {len(rows)} actors; "
      f"{sum(row['Region'] != 'UNRESOLVED' for row in rows)} polygon-labelled; "
      f"{sum(row['HintAgreement'] == 'CONFLICT' for row in rows)} prefix discrepancies")
