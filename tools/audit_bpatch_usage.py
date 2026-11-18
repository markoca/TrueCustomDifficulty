#!/usr/bin/env python3
"""Fail-closed BPatch writer gate.

Every TCD control must reach the game as a BPatch edit of the original
CFG. The only allowed exceptions are the shapes below, pinned per writer
method with the reason the game needs them:

  * a whole list/array that the game reads as one value (an indexed bpatch
    would leave stale vanilla entries behind),
  * a refurl/refkey clone (a NEW TCD SID that copies a vanilla root),
  * a new TCD-only definition (AddPlainRoot),
  * removenode of one vanilla child,
  * a fresh appended array element inside a bpatch parent.

The gate also checks that direct edit points go through AddAssignment →
GetOrAddNode (bpatch all the way down), that every edit-point operation in
cfg-writer-baseline.json has a TransformValue case, and that every
hand-written raw CFG text struct carries {bpatch}.
"""
from __future__ import annotations

import glob
import json
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICES = ROOT / "src/TrueCustomDifficulty/Services"
BASELINE = ROOT / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"

LIST = "whole list the game reads as one value"
CLONE = "refurl clone: new TCD SID, vanilla root untouched"
CLONE_BODY = "body of a refurl clone (own SID, replaced wholesale)"
PLAIN = "new TCD-only definition"
REMOVE = "removenode of one vanilla child"
APPEND = "new appended element inside a bpatch parent"
PRIMITIVE = "PatchDocument primitive"

# (file, method, construct) -> (count, reason)
ALLOWED: dict[tuple[str, str, str], tuple[int, str]] = {
    ("GenericLootReduction", "WriteNpcBonusSupplyReduction", "ref"): (1, CLONE),
    ("GenericLootReduction", "WriteNpcBonusSupplyReduction", "full"): (1, CLONE_BODY),
    ("GenericNpcRareStock", "NewRareRoot", "plain"): (1, PLAIN),
    ("GenericNpcRareStock", "RareGroup", "full"): (3, PLAIN),
    ("GenericNpcRareStock", "RareItemPool", "full"): (1, PLAIN),
    ("GenericNpcRareStock", "RareReference", "full"): (1, PLAIN),
    ("GenericNpcRareStock", "WriteGenericNpcRareTraderStock", "ref"): (1, CLONE),
    ("GenericNpcRareStock", "WriteGenericNpcRareTraderStock", "full"): (3, CLONE_BODY),
    ("ModItems", "WriteAuditedModItemRemovals", "remove"): (1, REMOVE),
    ("MutantAggression", "WriteMutantAggression", "full"): (4, LIST + ": AgentCooldownMultipliers"),
    ("MutantAggression", "WriteCombatSynchronization", "full"): (1, LIST + ": CombatSynchronization rank table"),
    ("MutantAggression", "WriteCombatSyncTree", "full"): (1, LIST + ": CombatSynchronization rank table"),
    ("StashAuthoredWeapons", "WriteStashAuthoredWeaponRemovals", "remove"): (1, REMOVE),
    ("StashDlc1", "WriteDlcCheapNoFreshStashes", "ref"): (1, CLONE),
    ("StashDlc1", "WriteDlcCheapNoFreshStashes", "full"): (4, CLONE_BODY),
    ("StashGearCoverage", "WriteStashGearCoverage", "ref"): (1, CLONE),
    ("StashGearCoverage", "WriteStashGearCoverage", "full"): (5, CLONE_BODY),
    ("StashSmartLoot", "WriteWorldStashSmartLoot", "ref"): (1, CLONE),
    ("StashTuning", "WriteStashPresenceEmpty", "full"): (1, APPEND),
    ("TraderBuys", "WriteTraderBuys", "remove"): (1, REMOVE),
    ("TraderBuys", "WriteTraderBuys", "replace"): (1, LIST + ": BuyLimitations"),
    ("TraderSells", "WriteTraderSells", "remove"): (1, REMOVE),
    ("TraderSells", "WriteTraderSells", "ref"): (2, CLONE),
    ("WorldArmorRemovals", "WriteWorldArmorRemovals", "remove"): (1, REMOVE),
    ("", "AddPlainRoot", "plain"): (1, PRIMITIVE),
    ("", "AddPlainRoot", "new"): (1, PRIMITIVE),
    ("", "AddPlainRoot", "full"): (1, PRIMITIVE),
    ("", "AddRemovalNode", "remove"): (1, PRIMITIVE),
    ("", "AddRemovalNode", "full"): (1, PRIMITIVE),
    ("", "AddRemovalRoot", "removeroot"): (1, PRIMITIVE),
    ("", "AddReplacementNode", "replace"): (1, PRIMITIVE),
    ("", "AddReplacementNode", "full"): (1, PRIMITIVE),
    ("", "GetOrAddChild", "new"): (1, PRIMITIVE),
    ("", "GetOrAddReferencedRoot", "ref"): (1, PRIMITIVE),
    ("", "GetOrAddReferencedRoot", "new"): (1, PRIMITIVE),
    ("", "GetOrAddReferencedRoot", "full"): (1, PRIMITIVE),
    ("", "GetOrAddRoot", "new"): (1, PRIMITIVE),
    ("", "Render", "rawstruct"): (1, "renderer emits {bpatch}/refurl per node flag"),
    ("", "WriteAdditionalMechanics", "full"): (1, LIST + ": AdditionalMechanicsEffects"),
    ("", "WriteBlueprintShuffle", "replace"): (1, LIST + ": blueprint reward slot"),
    ("", "WriteCheapStashProduction", "ref"): (1, CLONE),
    ("", "WriteCheapStashTemplate", "full"): (4, CLONE_BODY),
    ("", "WriteCorpseSmartLoot", "full"): (1, LIST + ": CorpseSmartLoot package list"),
    ("", "WriteDestructibleAmmoProduction", "ref"): (1, CLONE),
    ("", "WriteDestructibleAmmoProduction", "full"): (4, CLONE_BODY),
    ("", "WriteDestructibleQuantityProduction", "ref"): (1, CLONE),
    ("", "WriteDestructibleQuantityProduction", "full"): (4, CLONE_BODY),
    ("", "WriteExpensiveStashProduction", "ref"): (1, CLONE),
    ("", "WriteExpensiveStashTemplate", "full"): (4, CLONE_BODY),
    ("", "WriteMediumStashAmmoProduction", "ref"): (1, CLONE),
    ("", "WriteMediumStashAmmoTemplate", "full"): (4, CLONE_BODY),
    ("", "WriteNpcCorpseAmmoProduction", "ref"): (1, CLONE),
    ("", "WriteNpcCorpseAmmoProduction", "full"): (1, LIST + ": CorpseSmartLoot package list"),
    ("", "WriteNpcMedicalLootProduction", "ref"): (1, CLONE),
    ("", "WriteNpcMedicalPoolTemplate", "full"): (4, CLONE_BODY),
    ("", "WritePsyOverrides", "full"): (2, LIST + ": PsyPhantomNPCOverrides below Master"),
    ("", "WriteWorldAttachmentShuffle", "ref"): (1, CLONE),
    ("", "WriteWorldDetectorSpawns", "remove"): (1, REMOVE),
    ("", "WriteWorldDetectorSpawns", "removeroot"): (1, "removenode of a whole vanilla spawn actor"),
    ("", "WriteWorldWeaponDistribution", "remove"): (1, REMOVE),
}

KINDS = (
    ("full", re.compile(r"useBpatch:\s*false")),
    ("replace", re.compile(r"\bAddReplacementNode\(")),
    ("remove", re.compile(r"\bAddRemovalNode\(")),
    ("removeroot", re.compile(r"\bAddRemovalRoot\(")),
    ("ref", re.compile(r"\bGetOrAddReferencedRoot\(")),
    ("plain", re.compile(r"\bAddPlainRoot\(")),
    ("new", re.compile(r"\bnew PatchNode\(")),
    ("rawstruct", re.compile(r'struct\.begin(?![^"\n]*\{bpatch\})')),
)
METHOD = re.compile(
    r"^\s*(?:private|public|internal|protected)\s+(?:static\s+)?(?:sealed\s+)?"
    r"[\w<>\[\],\s\(\)?.]+?\s+(\w+)\s*\(")


def scan() -> Counter:
    found: Counter = Counter()
    for path in sorted(glob.glob(str(SERVICES / "CfgPatchWriterService*.cs"))):
        name = Path(path).stem.replace("CfgPatchWriterService", "").lstrip(".")
        method = "?"
        for line in Path(path).read_text(encoding="utf-8").splitlines():
            m = METHOD.match(line)
            if m:
                method = m.group(1)
            for kind, rx in KINDS:
                found[(name, method, kind)] += len(rx.findall(line))
    return Counter({k: v for k, v in found.items() if v})


def main() -> int:
    errors: list[str] = []
    found = scan()
    for key, count in sorted(found.items()):
        if key not in ALLOWED:
            errors.append(f"unpinned non-bpatch construct {key} ×{count}")
        elif ALLOWED[key][0] != count:
            errors.append(f"{key}: expected ×{ALLOWED[key][0]}, found ×{count}")
    for key in ALLOWED:
        if key not in found:
            errors.append(f"pinned construct no longer present: {key}")

    main_src = (SERVICES / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
    if "document.AddAssignment(point.Prototype, point.NodePath, point.Key, value);" not in main_src:
        errors.append("direct edit points no longer go through AddAssignment")
    if not re.search(r"current = current\.GetOrAddChild\(segment, useBpatch: true\);", main_src):
        errors.append("GetOrAddNode no longer creates bpatch nodes")
    if 'node = new PatchNode(name, useBpatch: true);' not in main_src:
        errors.append("GetOrAddRoot no longer creates bpatch roots")

    switch = main_src[main_src.index("private static string TransformValue("):]
    switch = switch[:switch.index("_ => throw")]
    ops = set(re.findall(r'"(\w+)"\s*=>', switch))
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    used = Counter(p["operation"] for p in baseline["editPoints"])
    for op in sorted(set(used) - ops):
        errors.append(f"edit-point operation without writer case: {op}")

    if errors:
        for e in errors:
            print("FAIL", e)
        return 1
    reasons = Counter(r for _, r in ALLOWED.values())
    print(f"PASS bpatch usage: {len(baseline['editPoints'])} direct points via bpatch "
          f"({len(used)} operations), {sum(found.values())} pinned non-bpatch constructs "
          f"in {len(ALLOWED)} sites ({len(reasons)} reasons)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
