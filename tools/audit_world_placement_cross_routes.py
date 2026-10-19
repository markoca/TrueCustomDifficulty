#!/usr/bin/env python3
"""Cross-route original CFG identity/leaf guard for safe simultaneous world shuffles.

Offline source-manifest test only. No .NET/Unreal runtime is inferred.
"""
import csv
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"


def rows(filename):
    with (DATA / filename).open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


weapon = rows("world-weapon-shuffle-manifest.csv")
attachment = rows("world-attachment-generator-manifest.csv")
blueprint = rows("blueprint-shuffle-manifest.csv")
detector = rows("world-detector-stash-manifest.csv")
authored_removal = rows("stash-authored-weapon-removal-manifest.csv")

assert len(weapon) == 14 and len(attachment) == 60
assert len(blueprint) == 77 and len(detector) == 1
assert len(authored_removal) == 13

weapon_root = {r["generator_sid"] for r in weapon}
attachment_root = {r["generator_sid"] for r in attachment}
blueprint_root = {r["generator_sid"] for r in blueprint}
detector_root = {r["generator_sid"] for r in detector}

shared = "ChemicalPlant_ChemicalTanks_StashGenerator6_8E3157704F88075AAB9F519551CDA57D"
assert weapon_root & attachment_root == {shared}, (
    "Regular weapon and attachment shuffles now share unexpected original CFG roots")
assert not ((weapon_root | attachment_root) & (blueprint_root | detector_root))
assert not (blueprint_root & detector_root)

weapon_slots = {(r["generator_sid"], r["node_path"]) for r in weapon}
attachment_slots = {(r["generator_sid"], r["entry_node_path"]) for r in attachment}
blueprint_slots = {(r["generator_sid"], r["node_path"]) for r in blueprint}
detector_slots = {(r["generator_sid"], r["node_path"]) for r in detector}

assert weapon_slots.isdisjoint(attachment_slots | blueprint_slots | detector_slots)
assert attachment_slots.isdisjoint(blueprint_slots | detector_slots)
assert blueprint_slots.isdisjoint(detector_slots)
assert {path for sid, path in weapon_slots if sid == shared} == {
    "ItemGenerator/[1]/PossibleItems/[0]"}
assert {path for sid, path in attachment_slots if sid == shared} == {
    "ItemGenerator/[2]/PossibleItems/[0]"}

# The 13 approved authored regular weapon rewards deliberately overlap the
# weapon shuffle exact leaves. Their simultaneous enablement MUST remain
# blocked before output is staged, not silently reconciled or blended.
approved = {(r["GeneratorSID"], r["NodePath"]) for r in authored_removal}
assert approved == weapon_slots - {
    (shared, "ItemGenerator/[1]/PossibleItems/[0]")
} or approved.issubset(weapon_slots)
assert len(approved & weapon_slots) == 13
writer = (ROOT / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs").read_text(
    encoding="utf-8")
assert "Extra Stash Gear and World Weapon Shuffle cannot both be enabled." in writer

print("PASS: weapon/attachment share one generator but zero CFG leaves; "
      "77 blueprints and one detector root stay separate; "
      "13 authored weapon removal collisions remain build-blocked")
