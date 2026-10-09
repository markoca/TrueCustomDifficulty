#!/usr/bin/env python3
"""Fail closed if a World Weapon Shuffle source edit would flatten nested vanilla rewards.

Independent 2026-10-10 decoded original GameData proof: six physical
ItemGenerator stashes have Chance/MinCount/MaxCount inside Upgrades or
Attaches, not on their weapon PossibleItems leaf. An indexed item
replacement dropped those nested source blocks; only ItemPrototypeSID
may be authored as an incremental {bpatch} source field.
This static guard complements, but never replaces, Windows C11 runtime
production writer tests and original CFG ZIP source checks.
"""
from __future__ import annotations

import csv
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
WRITER = ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs"
CONTRACT = ROOT / "tests" / "LootWorldPlacementContract" / "Program.cs"

SOURCE_NESTED_ONLY = {
    "CityCenter_PolissyaHotel_StashGenerator3_07727ED446A8EB9D0F56B3ABB9542CAD": ("chance", "mincount", "maxcount"),
    "Garbage_DigSite_StashGenerator2_C59BE52D481BCA2E1EB1CE816B67EF71": ("mincount", "maxcount"),
    "Garbage_Factory_StashGenerator4_B3F81A794CB50D8DD300F794E6FDAF25": ("mincount", "maxcount"),
    "Jupiter_Market_StashGenerator2_A3B04A9949CA27AA3E62FD9F3CD08ECD": ("chance", "mincount", "maxcount"),
    "LesserZone_ARMS_StashGenerator2_A4D6A1EA48DDDEE35883379DFFD93F81": ("mincount", "maxcount"),
    "STCMalachite__StashGenerator_464E8FB14AEEC7B154602AB22BEA1EBC": ("chance", "mincount", "maxcount"),
}


def main() -> int:
    with (DATA / "world-weapon-shuffle-manifest.csv").open(newline="", encoding="utf-8") as file:
        rows = list(csv.DictReader(file))
    if len(rows) != 14:
        raise AssertionError(f"Expected 14 safe world weapon generator entries: {len(rows)}")
    by_sid = {row["generator_sid"]: row for row in rows}
    if len(by_sid) != 14:
        raise AssertionError("Duplicate safe world weapon generator identities")
    if sum(map(len, SOURCE_NESTED_ONLY.values())) != 15:
        raise AssertionError("Audit evidence has lost 15 known nested-only fields")
    for generator, fields in SOURCE_NESTED_ONLY.items():
        row = by_sid.get(generator)
        if row is None:
            raise AssertionError(f"Original stash generator missing: {generator}")
        for field in fields:
            if row[field].strip():
                raise AssertionError(
                    f"Source audit: {generator}/{field} is nested in Upgrades/Attaches, "
                    f"not authored on the primary weapon PossibleItems leaf; got {row[field]!r}"
                )

    source = WRITER.read_text(encoding="utf-8")
    start = source.index("WriteWorldWeaponDistribution(")
    # First occurrence is the call site. The definition has a return
    # signature, so locate it separately.
    start = source.index("private static (int EditCount, int FileCount) WriteWorldWeaponDistribution(")
    end = source.index("private static int WriteWorldAttachmentShuffle(", start)
    method = source[start:end]
    if re.search(r"\bAddReplacementNode\s*\(", method):
        raise AssertionError("World weapon writer still replaces an entire indexed item, erasing nested rewards")
    if "CopyIfPresent(" in method:
        raise AssertionError("World weapon source mechanics are being copied instead of inherited")
    if not re.search(r'itemGeneratorDocument\.AddAssignment\(\s*slot\.GeneratorSid,\s*path,\s*"ItemPrototypeSID",\s*assigned\.ItemSid\s*\)', method):
        raise AssertionError("Minimal world weapon indexed ItemPrototypeSID BPatch was removed")

    contract = CONTRACT.read_text(encoding="utf-8")
    for phrase in ("MergedNodes.Contains(path)", "nestedOnly", 'new[] { 7, 13, 14 }'):
        if phrase not in contract:
            raise AssertionError("C11 BPatch regression coverage missing: " + phrase)

    print("PASS: 14 world weapon generators, six nested reward roots, 15 excluded item-level source fields")
    print("PASS: incremental ItemPrototypeSID BPatch only; no whole indexed weapon reward replacement")
    print("PASS: C11 checks genuine nested {bpatch} and prevents old bad manifest rows")
    print("NOT VERIFIED: Windows execution, Unreal merge behavior, in-game reward compatibility")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
