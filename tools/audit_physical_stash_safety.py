#!/usr/bin/env python3
"""Fail-closed physical stash classification; inventory is not patch authorization."""
import argparse
import csv
import io
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
FIELDS = [
    "actor_sid", "origin", "inventory_class", "safety_class",
    "protection_reason", "level_name", "region", "region_evidence",
    "container_sid", "stash_prototype_sid",
]
APPROVED = {"REGULAR_SMARTLOOT", "DLC1_ELIGIBLE_MANIFEST"}


def read(name):
    with (DATA / name).open(newline="", encoding="utf-8") as stream:
        return list(csv.DictReader(stream))


def generate():
    actors = read("physical-stash-map-regions.csv")
    world = read("smartloot-regular-world-actor-manifest.csv")
    protected_rows = read("base-stash-protected-actor-manifest.csv")
    dlc_rows = read("dlc1-stash-eligible-actor-manifest.csv")

    protected = {r["ActorSID"]: r["ProtectionReason"] for r in protected_rows}
    world_ids = {r["ActorSID"] for r in world}
    ordinary = {r["ActorSID"] for r in world
                if r["ClueVariableSID"] == "EmptyInherited"}
    clue = world_ids - ordinary
    dlc = {r["ActorSID"] for r in dlc_rows}
    physical = {r["actor_sid"]: r for r in actors}

    # A regular-looking source on a named/unique/quest stash is NOT safe.
    # Source membership alone cannot promote an actor to an allowed target.
    assert len(actors) == len(physical) == 1569
    assert len(world) == len(world_ids) == 831
    assert len(ordinary) == 803 and len(clue) == 28
    assert len(protected_rows) == len(protected) == 106
    assert clue <= protected.keys(), "A named-clue stash escaped whole-actor protection"
    assert not ordinary.intersection(protected), "Protected actor entered ordinary scope"
    assert len(dlc_rows) == len(dlc) == 112
    assert not dlc.intersection(protected) and not dlc.intersection(world_ids)
    assert world_ids.union(protected, dlc) <= physical.keys()
    assert all(physical[sid]["origin"] == "base"
               and physical[sid]["level_name"].startswith("WorldMap_WP")
               for sid in world_ids.union(protected))
    assert all(physical[sid]["origin"] == "dlc1"
               and physical[sid]["level_name"].startswith("WorldMap_WP")
               for sid in dlc)
    assert all(r["ClueVariableSID"] == "EmptyInherited" for r in dlc_rows)

    result = []
    for actor in actors:
        sid = actor["actor_sid"]
        reason = ""
        # Whole-actor protection MUST be evaluated before any regular source.
        if sid in protected:
            cls = "PROTECTED_BASE"
            reason = protected[sid]
        elif sid in ordinary:
            cls = "REGULAR_SMARTLOOT"
        elif sid in dlc:
            cls = "DLC1_ELIGIBLE_MANIFEST"
        elif not actor["level_name"].startswith("WorldMap_WP"):
            cls = "NON_WORLD_OR_TEST"
        else:
            cls = "WORLD_UNCLASSIFIED"
        result.append({
            "actor_sid": sid,
            "origin": actor["origin"],
            "inventory_class": actor["inventory_class"],
            "safety_class": cls,
            "protection_reason": reason,
            "level_name": actor["level_name"],
            "region": actor["region"],
            "region_evidence": actor["region_evidence"],
            "container_sid": actor["container_sid"],
            "stash_prototype_sid": actor["stash_prototype_sid"],
        })

    expected = {
        "REGULAR_SMARTLOOT": 803,
        "DLC1_ELIGIBLE_MANIFEST": 112,
        "PROTECTED_BASE": 106,
        "NON_WORLD_OR_TEST": 67,
        "WORLD_UNCLASSIFIED": 481,
    }
    assert Counter(row["safety_class"] for row in result) == expected
    eligible = [row for row in result if row["safety_class"] in APPROVED]
    assert len(eligible) == 915
    assert {r["actor_sid"] for r in eligible} == ordinary.union(dlc)
    assert not {r["actor_sid"] for r in eligible}.intersection(protected)
    return result, eligible


def csv_text(rows):
    buf = io.StringIO()
    writer = csv.DictWriter(buf, fieldnames=FIELDS, lineterminator="\n")
    writer.writeheader()
    writer.writerows(rows)
    return buf.getvalue()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    complete, eligible = generate()
    for filename, rows in (
        ("physical-stash-safety-catalog.csv", complete),
        ("physical-stash-eligible-catalog.csv", eligible),
    ):
        target = DATA / filename
        expected = csv_text(rows)
        if args.check:
            assert target.read_text(encoding="utf-8") == expected, (
                f"{filename} drift: regenerate with this script without --check")
        else:
            target.write_text(expected, encoding="utf-8")
            print("Wrote", target)
    print("PASS: 1,569 classified; 803 base + 112 DLC1 approved; "
          "106 protected, 67 test/non-world, 481 unclassified excluded.")
    print("NOTE: eligible inventory mirrors existing guarded source scopes; "
          "it does not authorize new CFG writers or unclassified actors.")


if __name__ == "__main__":
    main()
