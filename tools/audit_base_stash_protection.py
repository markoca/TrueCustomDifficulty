#!/usr/bin/env python3
"""Verify the source-backed 106-actor stash protection against all tier redirects.

This tests the repository-only safety contract without executing a Windows WPF
application. --source-zip optionally regenerates the expected IDs from the
exact base CFG dump rather than trusting hand-authored allowlist labels.
"""
import argparse
import collections
import csv
import re
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
SRC = ROOT / "src/TrueCustomDifficulty/Services"
ACTOR_FILE = "GameData/SpawnActorPrototypes.cfg"
TIER_COUNTS = {"cheap": 2, "medium": 17, "expensive": 11}
REASONS = {
    "SPECIAL_GENERATOR": 72,
    "NAMED_CLUE": 29,
    "SPECIAL_GENERATOR_AND_NAMED_CLUE": 5
}


def table(path):
    with path.open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def scan_source(path):
    with zipfile.ZipFile(path) as archive:
        content = archive.read(ACTOR_FILE)
    roots = list(re.finditer(rb"(?m)^([0-9A-F]{32}) : struct\.begin", content))
    protected = {}
    scanned = 0
    for i, match in enumerate(roots):
        actor = content[match.start():roots[i + 1].start() if i + 1 < len(roots) else len(content)]
        if (b"   LevelName = WorldMap_WP\n" not in actor
                or b"   SpawnType = ESpawnType::ItemContainer\n" not in actor):
            continue
        scanned += 1
        clue = re.search(rb"(?m)^   ClueVariablePrototypeSID\s*=\s*([^\r\n]*)", actor)
        named = clue is not None and clue.group(1).strip() not in (b"EmptyInherited", b"")
        gens = re.findall(rb"(?m)^\s+PrototypeSID\s*=\s*([^\r\n]*)", actor)
        special = any(tag in value for value in gens for tag in (b"Blueprint", b"Unique", b"Quest"))
        if special or named:
            reason = ("SPECIAL_GENERATOR_AND_NAMED_CLUE" if special and named else
                      "SPECIAL_GENERATOR" if special else "NAMED_CLUE")
            protected[match.group(1).decode("ascii")] = reason
    assert scanned == 1141, f"Base-world ItemContainer source count drift: {scanned}"
    return protected


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-zip", type=Path,
                        help="Optional exact STALKER2-GameData-CFG dump for independent re-extraction")
    args = parser.parse_args()

    entries = table(DATA / "base-stash-protected-actor-manifest.csv")
    found = {row["ActorSID"]: row["ProtectionReason"] for row in entries}
    assert len(entries) == len(found) == 106, "Protected actor roster must contain 106 distinct SIDs"
    assert all(re.fullmatch("[0-9A-F]{32}", sid) for sid in found), "Bad protected ActorSID"
    assert dict(collections.Counter(found.values())) == REASONS, "Source protection reason class drift"
    assert found["098E76F14ADD5CFF9C62508DB31013E3"] == "SPECIAL_GENERATOR", (
        "Blueprint + ordinary Rare stash actor protection missing")
    assert found["02E9BD8341141FEC43FBBB9B6E8F8891"] == "NAMED_CLUE", (
        "Named clue stash protection missing")

    # The source manifests stay verbatim for provenance; the runtime emits
    # only approved rows and no protected base ItemGeneratorSettings patch.
    total_blocked = 0
    direct_blocked = 0
    for tier, expected_direct in TIER_COUNTS.items():
        rows = table(DATA / f"{tier}-stash-production-redirect-manifest.csv")
        excluded = [row for row in rows if row["physical_container_sid"] in found]
        direct = [row for row in excluded if row["file"] == ACTOR_FILE]
        indirect = [row for row in excluded if row["file"] != ACTOR_FILE]
        expected_indirect = {"cheap": 10, "medium": 26, "expensive": 39}[tier]
        assert len(direct) == expected_direct and len(indirect) == expected_indirect, (
            f"{tier} protected direct/indirect redirects: "
            f"{len(direct)}/{len(indirect)} != {expected_direct}/{expected_indirect}")
        assert all(row["key"] == "PrototypeSID" and
                   row["node_path"].startswith("ItemGeneratorSettings.")
                   for row in direct), f"Unexpected direct {tier} writer field"
        assert all(row["file"] == "GameData/ItemGeneratorPrototypes.cfg" and
                   row["key"] == "ItemGeneratorPrototypeSID" for row in indirect), (
            f"Unexpected indirect wrapper redirect on protected {tier} actor")
        total_blocked += len(excluded)
        direct_blocked += len(direct)
    assert total_blocked == 105 and direct_blocked == 30

    writer = (SRC / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
    guard = (SRC / "CfgPatchWriterService.StashSmartLoot.cs").read_text(encoding="utf-8")
    project = (ROOT / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
    contract = (ROOT / "tests/LootSpecialGeneratorsContract/Program.cs").read_text(encoding="utf-8")
    assert writer.count("!IsApprovedStashTierRedirect(redirect.File, redirect.Prototype)") == 3, (
        "All 3 rank ItemGenerator redirect writers must enforce base AND DLC protection")
    assert "BaseProtectedStashActors.Value.Contains(physicalContainerSid)" in guard
    assert "ids.Count != 106" in guard and "special != 72" in guard
    assert "Data\\base-stash-protected-actor-manifest.csv" in project
    assert "VerifyBaseTierRedirectProtection();" in contract
    assert "AssertProtectedBaseStashesUntouched(outp, name);" in contract
    assert "AssertProtectedBaseStashesUntouched(paths, label);" in contract

    if args.source_zip:
        source = scan_source(args.source_zip)
        assert found == source, (
            f"Protected source drift; only in manifest={sorted(set(found)-set(source))}, "
            f"only in source={sorted(set(source)-set(found))}; "
            f"changed reasons={sorted(sid for sid in set(found)&set(source) if found[sid]!=source[sid])}")
        print("PASS: exact base CFG dump independently reproduces the 106-actor protection list.")

    print("PASS: 106 protected world stash actors; 77 special generator actors,")
    print("      34 named clues with 5 overlaps, 105 tier redirects excluded")
    print("      (Basic 2 direct + 10 indirect; Standard 17 + 26; Rare 11 + 39).")
    print("NOT VERIFIED: .NET build, generated CFG, Wine/Unreal runtime.")


if __name__ == "__main__":
    main()
