#!/usr/bin/env python3
"""Regression fixtures for the original nested-stash attachment CFG inspector.

Synthetic fixture values are not asserted to be real game source values.
"""
import csv
import tempfile
import zipfile
from pathlib import Path
from audit_nested_stash_attachment_sources import (
    load_expected, load_original, parse_original,
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
    source = "".join(f"""Stash_Attach{i} : struct.begin {{refurl=../ItemGeneratorPrototypes.cfg; refkey=[0]}}
    SID = Stash_Attach{i}
    ItemGenerator : struct.begin
        [0] : struct.begin
            Category = EItemGenerationCategory::SubItemGenerator
            PossibleItems : struct.begin
                [0] : struct.begin
                    ItemPrototypeSID = Attach{i}
                    Chance = 0.5
                    MinCount = 1
                    MaxCount = 1
                struct.end
            struct.end
        struct.end
    struct.end
struct.end
""" for i in range(19))
    parsed = parse_original(source, expected)
    assert len(parsed) == 19
    assert parsed[0]["AuthoredInnerChance"] == "0.5"
    assert parsed[0]["EffectiveInheritedValues"] == "UNRESOLVED"
    file = root / "GameData" / "ItemGeneratorPrototypes.cfg"
    file.parent.mkdir()
    file.write_text(source, encoding="utf-8")
    assert load_original(file.parent.parent)[0].decode("utf-8") == source
    zipped = root / "original.zip"
    with zipfile.ZipFile(zipped, "w") as archive:
        archive.writestr("STALKER2/GameData/ItemGeneratorPrototypes.cfg", source)
    assert load_original(zipped)[0].decode("utf-8") == source
    invalid = [
        source.replace("Stash_Attach0", "InvalidGenerator", 1),
        source.replace("ItemPrototypeSID = Attach0", "ItemPrototypeSID = WrongSID", 1),
        source.replace("Chance = 0.5",
                       "Chance = 0.5\n                    ItemGeneratorPrototypeSID = Loop", 1),
        source.replace("Chance = 0.5",
                       "Chance = 0.5\n                    Chance = 0.9", 1),
    ]
    for broken in invalid:
        try:
            parse_original(broken, expected)
        except ValueError:
            pass
        else:
            raise AssertionError("Unsafe or ambiguous original source accepted")

print("PASS: nested attachment source inspector fixtures (19 roots, folder/ZIP, malformed-source guards)")
