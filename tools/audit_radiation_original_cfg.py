#!/usr/bin/env python3
"""Audit decoded original radiation CFG source archives against production mappings.

Usage:
  python3 tools/audit_radiation_original_cfg.py --base PATH.zip --dlc1 PATH.zip
  python3 tools/audit_radiation_original_cfg.py --base PATH.zip --dlc1 PATH.zip --json report.json

Checks static CFG facts and BPatch source equivalence. Does NOT simulate the game.
"""
import argparse
import json
import re
import zipfile
from collections import Counter
from pathlib import Path

STAGES = [
    ("LightRadiation_2", 1.0, "LightRadiationDamage", 0.2),
    ("MediumRadiation", 10.0, "MediumRadiationDamage", 0.3),
    ("StrongRadiation", 20.0, "StrongRadiationDamage", 0.48),
    ("RadiationLevel4", 30.0, "RadiationLevel4Damage", 0.816),
    ("RadiationLevel5", 40.0, "RadiationLevel5Damage", 1.468),
    ("RadiationLevel6", 50.0, "RadiationLevel6Damage", 2.2),
    ("RadiationLevel7", 60.0, "RadiationLevel7Damage", 4.18),
    ("RadiationLevel8", 70.0, "RadiationLevel8Damage", 8.37),
    ("RadiationLevel9", 80.0, "RadiationLevel9Damage", 17.58),
    ("RadiationLevel10", 90.0, "RadiationLevel10Damage", 38.67),
    ("DeadlyRadiation", 99.0, "DeadlyRadiationDamage", 20.0),
]


def block(source: str, name: str) -> str:
    expr = r"^" + re.escape(name) + r" : struct\.begin[^\n]*\n(.*?)^struct\.end\s*$"
    match = re.search(expr, source, flags=re.M | re.S)
    if not match:
        raise ValueError(f"Missing original effect prototype: {name}")
    return match.group(1)


def field(section: str, name: str) -> str:
    match = re.search(r"^\s*" + re.escape(name) + r"\s*=\s*([^\r\n]+)",
                      section, flags=re.M)
    if not match:
        raise ValueError(f"Missing original field: {name}")
    return match.group(1).strip()


def number(raw: str) -> float:
    return float(raw.removesuffix("f").removesuffix("%"))


def member(archive: Path, suffix: str) -> str:
    with zipfile.ZipFile(archive) as source:
        matches = [p for p in source.namelist() if p.endswith(suffix)]
        if len(matches) != 1:
            raise ValueError(f"Expected exactly one {suffix} in {archive}; found {matches}")
        return source.read(matches[0]).decode("utf-8-sig")


def ensure(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def audit(base: Path, dlc: Path, repo: Path) -> dict:
    effect_cfg = member(base, "/EffectPrototypes.cfg")
    core_cfg = member(base, "/CoreVariables.cfg")
    player_cfg = member(base, "/ObjPrototypes.cfg")
    difficulty_cfg = member(base, "/DifficultyPrototypes.cfg")
    dlc_effect_cfg = member(dlc, "/EffectPrototypes.cfg")
    catalog = json.loads((repo / "src/TrueCustomDifficulty/Data/parameters.json")
                         .read_text(encoding="utf-8"))
    baseline = json.loads((repo / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json")
                          .read_text(encoding="utf-8"))
    parameters = {p["id"]: p for p in catalog["parameters"]}
    damage = parameters["survival.radiationDamage"]
    routes = [p for p in baseline["editPoints"]
              if p["parameterId"] == "survival.radiationDamage"]
    ensure(len(routes) == 22 and len(damage["targets"]) == 11,
           "Radiation magnitude writer lost one of its 22 source routes")
    for name in ("RadiationMechanics", "DeadlyRadiationDamage"):
        ensure(f"{name} : struct.begin" not in dlc_effect_cfg,
               f"DLC1 has a new global override of {name}; require explicit inheritance audit")

    root = effect_cfg.split("struct.end", 1)[0]
    ensure(number(field(root, "TimePerChargeMin")) == 1.0
           and number(field(root, "TimePerChargeMax")) == 1.0,
           "The root effect prototype did not inherit a 1-second charge interval")
    normal = block(effect_cfg, "RadiationMechanics")
    ensure("ConditionValue = 0.001f" in normal and "ConditionValue = 0.0f" in normal,
           "Original none/visual-only thresholds are missing")

    levels = []
    for stage, threshold, sid, value in STAGES:
        m = re.search(r"^\s{9}" + re.escape(stage)
                      + r" : struct\.begin\s*\n\s{12}ConditionValue = ([^\r\n]+)"
                      + r"(.*?)(?=^\s{9}struct\.end)", normal, flags=re.M | re.S)
        ensure(m is not None and abs(number(m.group(1)) - threshold) < 1e-9,
               f"Wrong normal radiation threshold: {stage}")
        ensure("EffectSID = " + sid in m.group(2), f"Wrong stage-effect attachment: {stage}")
        effect = block(effect_cfg, sid)
        ensure("Type = EEffectType::Damage" in effect
               and abs(number(field(effect, "ValueMin")) - value) < 1e-9
               and abs(number(field(effect, "ValueMax")) - value) < 1e-9
               and number(field(effect, "Charges")) == 1,
               f"Source magnitude/charge drift: {sid}")
        pair = [p for p in routes if p["prototype"] == sid]
        ensure(sorted(p["key"] for p in pair) == ["ValueMax", "ValueMin"]
               and all(p["operation"] == "scale"
                       and abs(number(p["sourceRaw"]) - value) < 1e-9 for p in pair),
               f"Actual writer targets do not match {sid}")
        ensure(damage["targets"][len(levels)]["prototype"] == sid,
               f"Stage order in catalog diverged: {sid}")
        levels.append(dict(stage=stage, thresholdRAD=threshold,
                           effect=sid, damageValue=value,
                           duration=field(effect, "Duration") if
                           re.search(r"^\s*Duration =", effect, flags=re.M) else None,
                           explicitChargeInterval=bool(
                               re.search(r"^\s*TimePerChargeMin =", effect, flags=re.M))))

    presets = {}
    for name, radioactivity, per_second in [
        ("Light", 15, 1), ("Medium", 45, 3), ("Strong", 75, 6),
        ("Deadly", 99, 33), ("RadBlock", 100, 50),
        ("Custom", 1, 1), ("Topaz", 15, 1)
    ]:
        match = re.search(r"Preset = ERadiationPreset::" + name
                          + r"\s*\n\s*RadioactivityValue = ([^\r\n]+)"
                          + r"\s*\n\s*RadiationPerSecondValue = ([^\r\n]+)",
                          core_cfg)
        ensure(match is not None and number(match.group(1)) == radioactivity
               and number(match.group(2)) == per_second,
               f"Incorrect radiation field preset: {name}")
        presets[name] = dict(radioactivity=radioactivity,
                             radiationPerSecond=per_second)

    hazard = block(effect_cfg, "RadBlockFieldDamage")
    ensure(number(field(hazard, "ValueMin")) == 9000
           and number(field(hazard, "ValueMax")) == 9000
           and number(field(hazard, "TimePerChargeMin")) == 2.7
           and number(field(hazard, "TimePerChargeMax")) == 2.7,
           "RadBlock-specific direct field damage must remain 9000 at 2.7 seconds")
    ensure(core_cfg.count("RadBlockFieldDamage") == 2,
           "Deadly and RadBlock field presets must each attach the hazard")
    ensure("DegenRadiation = 0.05" in player_cfg and
           "MaxRadiation = 100" in player_cfg and
           "RadiationMechanics" in player_cfg,
           "Player normal radiation vitals have drifted")
    for v in ("0.6", "1.0", "1.5", "1.85"):
        ensure("Radiation_AccumulationSpeed = " + v in difficulty_cfg,
               f"Missing normal radiation difficulty scalar {v}")
    master = block(effect_cfg, "RadiationNegativeMechanics")
    ensure("ConditionValue = 50" in master
           and "EffectSID = RadiationNegativeStaminaPenalty" in master,
           "Master radiation threshold or stamina penalty lost")

    treatments = ("Antirad4", "EcoMedkitAntirad3", "BeerAntirad1",
                  "VodkaAntirad3", "SleepRadiationRestore",
                  "Artifact_WeirdKettle_BeerAntirad",
                  "Artifact_WeirdKettle_VodkaAntirad",
                  "Artifact_WeirdKettle_LimitedEnergeticAntirad")
    treatment_values = {n: field(block(effect_cfg, n), "ValueMin")
                        for n in treatments}
    definitions = re.findall(
        r"(?ms)^([A-Za-z0-9_]+) : struct\.begin[^\n]*\n(.*?)^struct\.end",
        effect_cfg)
    counts = Counter()
    for _, proto in definitions:
        for cls in ("Radiation", "ProtectionRadiation", "DegenRadiation"):
            if re.search(r"^\s*Type = EEffectType::" + cls + r"\s*$",
                         proto, flags=re.M):
                counts[cls] += 1
    ensure(dict(counts) == {
        "Radiation": 86, "ProtectionRadiation": 102, "DegenRadiation": 28
    }, "Radiation effect type inventory has changed")
    return dict(stages=levels, fieldPresets=presets, radBlockDamage=9000,
                radBlockTimePerCharge=2.7, treatment=treatment_values,
                effectTypeCounts=dict(counts), playerMaxRadiation=100,
                passiveRecovery=0.05, masterRadiationThreshold=50,
                rootTimePerChargeDefault=1.0,
                explicitlyTimedNormalStages=sum(s["explicitChargeInterval"]
                                                 for s in levels),
                matchingWriterFields=len(routes),
                runtimeStatus="NOT VERIFIED: stage ticks, exposure physics, time to death")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", type=Path, required=True,
                        help="Decoded STALKER2-GameData-CFG ZIP")
    parser.add_argument("--dlc1", type=Path, required=True,
                        help="Decoded STALKER2-DLC1-CFG ZIP")
    parser.add_argument("--repo", type=Path,
                        default=Path(__file__).resolve().parents[1])
    parser.add_argument("--json", type=Path,
                        help="Optional machine-readable audit report output")
    opts = parser.parse_args()
    findings = audit(opts.base, opts.dlc1, opts.repo)
    if opts.json:
        opts.json.write_text(json.dumps(findings, indent=2) + "\n",
                             encoding="utf-8")
    print("PASS: 11 normal stages / 22 writer leaves; 7 field presets;")
    print("      player vitals, Master, treatments, RadBlock 9000 at 2.7s.")
    print("Effect classes:", findings["effectTypeCounts"])
    print("Runtime radiation kill countdown: NOT VERIFIED")


if __name__ == "__main__":
    main()
