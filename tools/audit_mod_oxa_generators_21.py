#!/usr/bin/env python3
"""OXA Standard vs TCD 2.1: source-only generator allow/block compatibility guard.

Run from a TCD repo checkout. For full OXA checks supply the decoded A/C CFG
directories; third-party CFGs and PAKs are never checked into TCD. This script
DOES NOT emulate UE's bskipref/bpatch parser or establish in-game compatibility.
"""
import argparse
import collections
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HEADER = re.compile(r"^([A-Za-z0-9_]+)\s*:\s*struct\.begin(?:\s*\{([^}]*)\})?", re.M)
ITEM = re.compile(r"\bItemPrototypeSID\s*=\s*(\w+)")
CHILD = re.compile(r"\bItemGeneratorPrototypeSID\s*=\s*(\w+)")
PATCHED = (
    "Trader_Attachments_T2_ItemGenerator",
    "Trader_Attachments_T3_ItemGenerator",
    "Trader_Attachments_T4_ItemGenerator",
    "RostokTraderNato_TradeItemGenerator",
)
SOURCE_SHA256 = "012b2ad47cdbe7d55587dc030001dd201e172d709ce53fa789e923570796183e"


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def roots(text):
    matches = list(HEADER.finditer(text))
    return {
        m.group(1): (
            m.group(2) or "",
            text[m.start():matches[i + 1].start() if i + 1 < len(matches) else len(text)]
        )
        for i, m in enumerate(matches)
    }


def audit_tcd():
    manifest = ROOT / "src/TrueCustomDifficulty/Data/trader-sells-2.1-runtime.csv"
    writer = ROOT / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.TraderSells.cs"
    rows = [line.split(",") for line in manifest.read_text(encoding="utf-8-sig").splitlines()[1:] if line]
    check(len(rows) == 2939, "TCD 2.1 SELL manifest changed")
    expected = ((155, 31, 5), (68, 17, 4), (54, 9, 6), (41, 1, 14))
    result = {}
    for root, (row_count, profile_count, path_count) in zip(PATCHED, expected):
        matched = [row for row in rows if row[3] == root or row[6] == root]
        profiles = {(row[0], row[1]) for row in matched}
        paths = {row[8] for row in matched if row[6] == root}
        check((len(matched), len(profiles), len(paths))
              == (row_count, profile_count, path_count), "Manifest drift: " + root)
        result[root] = {"native_leaf_occurrences": len(matched),
                        "trade_profiles": len(profiles),
                        "direct_leaf_paths": len(paths)}
    source = writer.read_text(encoding="utf-8-sig")
    for token in ("SellCloneSid(", "GetOrAddReferencedRoot(",
                  "AddRemovalNode(targetClone, path[..^1], path[^1])",
                  "GetDocument(documents, SellTradeFile(first.TradeScope)).AddAssignment("):
        check(token in source, "TCD COW writer changed: " + token)
    check("OXATrade_" not in source, "TCD now explicitly references OXA: re-audit")
    return {"status": "PASS", "source_manifest_rows": len(rows), "native_roots": result,
            "tcd_removal_paths": "native-2.1-indices",
            "mod_support_implemented": False}


def audit_oxa(a, c):
    source_path = a / "ItemGeneratorPrototypes/DynamicItemGenerator_OXA.cfg"
    data = source_path.read_bytes()
    check(hashlib.sha256(data).hexdigest() == SOURCE_SHA256,
          "OXA generator changed: cannot reuse this audit; decode and re-audit")
    source = roots(data.decode("utf-8-sig"))
    native = {sid for sid, (meta, _) in source.items() if "bpatch" in meta}
    owned = {sid for sid, (meta, _) in source.items() if "refurl=" in meta}
    check(len(source) == 22 and len(owned) == 18 and native == set(PATCHED),
          "OXA generator root identities changed")
    items = {sid: ITEM.findall(body) for sid, (_, body) in source.items()}
    children = {sid: CHILD.findall(body) for sid, (_, body) in source.items()}
    check(sum(map(len, items.values())) == 119 and sum(map(len, children.values())) == 19,
          "OXA direct leaf / generator-edge count changed")
    for sid, targets in children.items():
        check(all(t in source for t in targets), "Missing child generator: " + sid)
    for sid in PATCHED[:3]:
        body = source[sid][1]
        check("ItemGenerator : struct.begin {bskipref}" in body
              and len(children[sid]) == 1, "OXA T2/T3/T4 bskipref shape changed")
    check("OXA : struct.begin" in source[PATCHED[3]][1]
          and len(children[PATCHED[3]]) == 2, "Rostok native bridge changed")
    prototypes = {}
    cfgs = list(a.glob("ItemPrototypes/**/*.cfg")) + list(c.rglob("ItemPrototypes/**/*.cfg"))
    check(cfgs, "No decoded OXA item prototypes")
    for path in cfgs:
        for sid, (meta, _) in roots(path.read_text(encoding="utf-8-sig")).items():
            value = "original" if "bpatch" in meta else "modded"
            check(sid not in prototypes or prototypes[sid] == value,
                  "Inconsistent prototype classification: " + sid)
            prototypes[sid] = value
    direct = [sid for leaves in items.values() for sid in leaves]
    check(all(sid in prototypes for sid in direct), "Unclassified OXA item leaf")
    added = [sid for sid in direct if prototypes[sid] == "modded"]
    original = [sid for sid in direct if prototypes[sid] == "original"]
    added_set = set(added)
    check((len(direct), len(added), len(original), len(added_set), len(set(original)))
          == (119, 87, 32, 80, 32), "Mixed original vs mod-added leaf drift")
    visited = set()

    def visit(sid, stack=()):
        check(sid not in stack, "Generator cycle: " + sid)
        if sid in visited:
            return
        visited.add(sid)
        for child in children[sid]:
            visit(child, stack + (sid,))

    for root in PATCHED:
        visit(root)
    reachable = {x for sid in visited for x in items[sid]}
    orphan = sorted(added_set - reachable)
    check(len(reachable & added_set) == 76 and orphan ==
          ["OXA_Laser_10", "OXA_Laser_7", "OXA_Laser_8", "OXA_Laser_9"],
          "OXA new-item reachability drift")
    check(sorted(owned - visited) == ["OXATrade_LaserAdvancedGenerator"],
          "Unexpected disconnected generator")
    # Pure source-leaf filter model: removes only mod-created SIDs;
    # proves that native items can be identified and left unchanged.
    filtered = [sid for sid in direct if sid not in added_set]
    check(filtered == original and len(filtered) == 32,
          "Mod-only symbolic block changed original game items")
    repeated = {sid: n for sid, n in collections.Counter(added).items() if n > 1}
    check(len(repeated) == 7 and sum(n - 1 for n in repeated.values()) == 7,
          "Multiple-pool occurrences must all be handled by per-SID BLOCK")
    all_cfgs = list(a.rglob("*.cfg")) + list(c.rglob("*.cfg"))
    other_links = [p for p in all_cfgs if p != source_path
                   and "ItemGeneratorPrototypeSID" in p.read_text(encoding="utf-8-sig")]
    check(not other_links, "Additional mod generator references: revisit reachability")
    grants = [str(p) for p in all_cfgs
              if "EQuestNodeType::ItemAdd" in p.read_text(encoding="utf-8-sig")
              or "SpawnedPrototypeSID" in p.read_text(encoding="utf-8-sig")]
    check(not grants, "Non-generator acquisition paths discovered")
    debug = (a / "QuestNodePrototypes/ND_PCGears_Debug.cfg").read_text(encoding="utf-8-sig")
    check(debug.count("EQuestNodeType::SetItemGenerator") == 4,
          "OXA debug quest generator references changed")
    return {"status": "PASS", "generator_roots": 22, "owned_roots": 18,
            "original_roots_patched": list(PATCHED), "item_leaves": 119,
            "mod_item_occurrences": 87, "original_item_occurrences": 32,
            "unique_mod_item_sids": 80, "reachable_mod_item_sids": 76,
            "disconnected_mod_item_sids": orphan,
            "disconnected_generator": "OXATrade_LaserAdvancedGenerator",
            "repeated_mod_item_sids": dict(sorted(repeated.items())),
            "symbolic_block_original_leaves_preserved": 32,
            "quest_direct_grants_found": False,
            "debug_quest_set_item_generator_count": 4}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--oxa-a", type=Path, help="Decoded OXA Standard A CFG folder")
    parser.add_argument("--oxa-c", type=Path, help="Decoded OXA Standard C CFG folder")
    args = parser.parse_args()
    check(bool(args.oxa_a) == bool(args.oxa_c),
          "Provide both --oxa-a and --oxa-c or neither")
    output = {"tcd": audit_tcd()}
    output["oxa"] = (audit_oxa(args.oxa_a, args.oxa_c) if args.oxa_a
                     else {"status": "NOT RUN: supply external decoded CFGs"})
    output["runtime_merge"] = "UNVERIFIED: test in the game engine"
    output["runtime_spawn_allow_block"] = "NOT IMPLEMENTED: no TCD mod overlay yet"
    print(json.dumps(output, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
