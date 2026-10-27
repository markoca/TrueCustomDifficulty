#!/usr/bin/env python3
"""Synthetic 2.0/2.1 CFG audit regressions: no game files needed."""
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

MODULE = Path(__file__).resolve().parents[1] / "tools/audit_game_21_cfg_compat.py"
spec = importlib.util.spec_from_file_location("cfg_compat", MODULE)
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)

BASE = "00312C9B4E94AEFFCFF3CF93F5017F49"
DLC = "022F897D43F6B71D5677D4AC5C4F4DF6"


def make_dump(path, *, core="0.7", base=BASE, flag="", dlc=DLC):
    actor = ""
    if base:
        actor = (f"{base} : struct.begin\n   SID = {base}\n"
                 "   ItemGeneratorSettings : struct.begin\n"
                 "      [0] : struct.begin\n"
                 "         GeneratorPrototypeSID = Stash_Basic\n"
                 "      struct.end\n   struct.end\n"
                 "   SpawnedPrototypeSID = StashContainer\n"
                 + flag + "struct.end\n")
    with zipfile.ZipFile(path, "w") as outer:
        for label, files in (
            ("GameData", {
                "GameData/CoreVariables.cfg":
                    f"DefaultConfig : struct.begin\n   BaseRepairCostModifier = {core}\nstruct.end\n",
                "GameData/FastTravelEconomyPrototypes.cfg":
                    "[1] : struct.begin\n   SID = ALifeGuideEconomy\n"
                    "   GeneralCoefficient = 1.3\nstruct.end\n",
                "GameData/SpawnActorPrototypes.cfg":
                    "[0] : struct.begin\n  SID = empty\nstruct.end\n" + actor,
            }),
            ("DLC1", {
                "DLCGameData/DLC1/SpawnActorPrototypes.cfg":
                    (f"{dlc} : struct.begin\n   StashPrototypeSID = StashDLC\nstruct.end\n"
                     if dlc else "[0] : struct.begin\n  SID = empty\nstruct.end\n"),
            }),
        ):
            buf = io.BytesIO()
            with zipfile.ZipFile(buf, "w") as inside:
                for file, content in files.items():
                    inside.writestr(file, content)
            outer.writestr(label + ".zip", buf.getvalue())


class CompatTests(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        root = Path(tmp.name)
        self.old, self.new = root / "old.zip", root / "new.zip"
        data = root / "Data"
        data.mkdir()
        (data / "actors.csv").write_text(
            f"ActorSID,Reason\n{BASE},base\n{DLC},dlc\n", encoding="utf-8"
        )
        baseline = root / "baseline.json"
        baseline.write_text(json.dumps({
            "gameVersion": "2.0.5",
            "editPoints": [{
                "parameterId": "economy.repairCostBase",
                "file": "GameData/CoreVariables.cfg",
                "prototype": "DefaultConfig", "nodePath": [],
                "key": "BaseRepairCostModifier", "sourceRaw": "0.7"
            }]
        }), encoding="utf-8")
        prev = audit.DATA, audit.BASELINE
        audit.DATA, audit.BASELINE = data, baseline
        self.addCleanup(lambda: (
            setattr(audit, "DATA", prev[0]),
            setattr(audit, "BASELINE", prev[1])
        ))

    def test_nested_actor_is_found(self):
        make_dump(self.old)
        src = audit.inner_archives(self.old)["GameData/SpawnActorPrototypes.cfg"]
        watched = audit.actor_watch(src, {BASE})
        self.assertIn(BASE, watched)
        self.assertTrue(any("SpawnedPrototypeSID" in x for x in watched[BASE]))

    def test_identical_passes(self):
        make_dump(self.old)
        make_dump(self.new)
        r = audit.run(self.old, self.new)
        self.assertTrue(r["passed"], r["issues"])
        self.assertEqual(r["manifest_actors"]["GameData/SpawnActorPrototypes.cfg"]["manifest_actor_roots_old"], 1)

    def test_missing_actor_fails(self):
        make_dump(self.old)
        make_dump(self.new, base="")
        r = audit.run(self.old, self.new)
        self.assertFalse(r["passed"])
        self.assertTrue(any("REMOVED" in x for x in r["issues"]))

    def test_smartloot_flag_fails(self):
        make_dump(self.old)
        make_dump(self.new, flag="   EnableSmartLootIfPossible = true\n")
        r = audit.run(self.old, self.new)
        self.assertFalse(r["passed"])
        self.assertTrue(any("changed" in x and "fields" in x for x in r["issues"]))

    def test_direct_changed_leaf_fails(self):
        make_dump(self.old)
        make_dump(self.new, core="0.8")
        r = audit.run(self.old, self.new)
        self.assertFalse(r["passed"])
        self.assertTrue(any("CHANGED LEAF" in x for x in r["issues"]))

    def test_no_matched_manifest_actors_fails(self):
        make_dump(self.old, base="")
        make_dump(self.new, base="")
        r = audit.run(self.old, self.new)
        self.assertFalse(r["passed"])
        self.assertTrue(any("ZERO matched" in x for x in r["issues"]))


if __name__ == "__main__":
    unittest.main()
