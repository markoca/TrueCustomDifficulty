#!/usr/bin/env python3
"""Regression fixtures for the original nested-stash attachment CFG inspector.

Synthetic fixture values are not asserted to be real game source values.
"""
import csv
import tempfile
import zipfile
from pathlib import Path
from audit_nested_stash_attachment_sources import (
    LEDGER, PINNED_SOURCE_SHA256, POOL, load_expected, load_original,
    parse_original,
)

rows = [
    dict(ItemSID=f"Attach{i}", Category="Magazine", ProgressionTier="T2",
         SourceGeneratorSID=f"Stash_Attach{i}",
         Evidence="EXACT_AUDITED_SINGLETON_GENERATOR")
    for i in range(19)
]
with tempfile.TemporaryDirectory() as directory:
    root = Path(directory)
    path = root / "progressions.csv"
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    expected = load_expected(path)
    # Match the actual, source-pinned [0] parent and 19 singleton leaf
    # structures. These SIDs remain deliberately synthetic.
    parent = """[0] : struct.begin
    ItemGenerator : struct.begin
        [0] : struct.begin
            Category = EItemGenerationCategory::None
            PossibleItems : struct.begin
                [0] : struct.begin
                    Chance = 0.f
                    MinCount = 0
                    MaxCount = 0
                struct.end
            struct.end
        struct.end
    struct.end
struct.end
"""
    source = parent + "".join(f"""Stash_Attach{i} : struct.begin {{refurl=../ItemGeneratorPrototypes.cfg;refkey=[0]}}
    SID = Stash_Attach{i}
    ItemGenerator : struct.begin
        [0] : struct.begin
            Category = EItemGenerationCategory::Attach
            PossibleItems : struct.begin
                [0] : struct.begin
                    ItemPrototypeSID = Attach{i}
                    Chance = 1
                struct.end
            struct.end
        struct.end
    struct.end
struct.end
""" for i in range(19))
    parsed = parse_original(source, expected)
    assert len(parsed) == 19
    assert all(row["AuthoredInnerChance"] == "1" for row in parsed)
    assert all(row["AuthoredCategory"] == "EItemGenerationCategory::Attach" for row in parsed)
    assert all(row["AuthoredMinCount"] == row["AuthoredMaxCount"] == "NOT_AUTHORED"
               for row in parsed)
    assert all(row["ParentMinCountRaw"] == row["ParentMaxCountRaw"] == "0"
               for row in parsed)
    assert all(row["EffectiveInheritedValues"]
               == "PARENT_FIELDS_RESOLVED_RUNTIME_SEMANTICS_UNVERIFIED"
               for row in parsed)
    file = root / "GameData" / "ItemGeneratorPrototypes.cfg"
    file.parent.mkdir()
    file.write_text(source, encoding="utf-8")
    assert load_original(file.parent.parent)[0].decode("utf-8") == source
    zipped = root / "original.zip"
    with zipfile.ZipFile(zipped, "w") as archive:
        archive.writestr("STALKER2/GameData/ItemGeneratorPrototypes.cfg", source)
    assert load_original(zipped)[0].decode("utf-8") == source
    invalid = [
        # Each mutation changes just one audited source contract.
        source.replace("Stash_Attach0 : struct.begin",
                       "InvalidGenerator : struct.begin", 1),
        source.replace("ItemPrototypeSID = Attach0", "ItemPrototypeSID = WrongSID", 1),
        source.replace("Chance = 1", "Chance = 0.5", 1),
        source.replace("Category = EItemGenerationCategory::Attach",
                       "Category = EItemGenerationCategory::SubItemGenerator", 1),
        source.replace("Chance = 1", "Chance = 1\n                    MinCount = 1", 1),
        source.replace("Chance = 1", "Chance = 1\n                    Chance = 0.8", 1),
        source.replace("Chance = 1",
                       "Chance = 1\n                    ItemGeneratorPrototypeSID = Cycle", 1),
        source.replace("{refurl=../ItemGeneratorPrototypes.cfg;refkey=[0]}",
                       "{refurl=../ItemGeneratorPrototypes.cfg;refkey=[1]}", 1),
        source.replace("Chance = 0.f", "Chance = 1.f", 1),
        source.replace("MaxCount = 0", "MaxCount = 1", 1),
    ]
    for broken in invalid:
        try:
            parse_original(broken, expected)
        except ValueError:
            pass
        else:
            raise AssertionError("Unsafe or ambiguous original source accepted")

# The small, reviewed evidence ledger is checked in CI without distributing
# the proprietary 9 MB original GameData generator file.
with LEDGER.open(encoding="utf-8", newline="") as f:
    evidence = list(csv.DictReader(f))
with POOL.open(encoding="utf-8", newline="") as f:
    production_pool = list(csv.DictReader(f))
assert len(evidence) == len(production_pool) == 19
assert len(PINNED_SOURCE_SHA256) == 64
assert {r["SourceGeneratorSID"] for r in evidence} == {
    r["SourceGeneratorSID"] for r in production_pool}
lookup = {r["SourceGeneratorSID"]: r for r in production_pool}
for row in evidence:
    pool_row = lookup[row["SourceGeneratorSID"]]
    assert row["ItemSID"] == pool_row["ItemSID"]
    assert row["AttachmentKind"] == pool_row["Category"]
    assert row["ProgressionTier"] == pool_row["ProgressionTier"]
    assert row["OriginalCategory"] == "EItemGenerationCategory::Attach"
    assert row["OriginalChance"] == "1"
    assert row["AuthoredMinCount"] == row["AuthoredMaxCount"] == "NOT_AUTHORED"
    assert row["TemplateMinCountRaw"] == row["TemplateMaxCountRaw"] == "0"
    assert row["ReferenceKey"] == "[0]"
    assert row["RuntimeVerified"] == "NO"
assert all(int(r["OriginalLine"]) > 0 for r in evidence)
assert len({r["OriginalLine"] for r in evidence}) == 19

print("PASS: source-shaped synthetic nested attachment fixtures and 19 pinned original-source evidence rows; no game-runtime claim")
