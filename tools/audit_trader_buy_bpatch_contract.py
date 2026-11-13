#!/usr/bin/env python3
"""Fail-closed 2.0.5 trader source audit and hypothetical BPatch tree probes.

Does not execute TCD's compiled C# writer, Unreal's parser, or the game.
"""
from __future__ import annotations

import argparse
from collections import Counter
import copy
import csv
import hashlib
import json
from pathlib import Path
import sys
import zipfile

from extract_trader_trade_inventory import BASE_ZIP_SHA, DLC_ZIP_SHA, c, field, parse_cfg, sha256

ROOT = Path(__file__).resolve().parents[1]
MEMBERS = {"base": "GameData/TradePrototypes.cfg",
           "dlc1": "DLCGameData/DLC1/TradePrototypes.cfg"}
HASHES = {"base": "c5115350d67d91a329b5922f0a51e9abe02f91075e5fbe24c1b3b4fa48f540eb",
          "dlc1": "7a72b5dfc112226c92c081e49ca6c3877b7cf00cd541964d07113253d37e31a5"}
TYPES = ("Weapon", "Armor", "Ammo", "Grenade", "Attach", "Consumable", "MutantLoot", "Artifact")
ZERO = {"SelmaTrader_TradePrototype", "Trader_Soviet_Rostok_TradePrototype"}


def read_csv(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8-sig") as stream:
        return list(csv.DictReader(stream))


def limits_of(node):
    lim = c(node, "BuyLimitations")
    if lim is None:
        return None
    if lim["children"] or lim["attrs"]:
        raise ValueError("Unexpected nested or inherited BuyLimitations node")
    fields = lim["fields"]
    values = []
    for i in range(len(fields)):
        value = fields.get(f"[{i}]")
        if value is None or not value.startswith("EItemType::"):
            raise ValueError("Sparse/non-enum BuyLimitations source")
        category = value.removeprefix("EItemType::")
        if category not in TYPES:
            raise ValueError("Unknown native enum " + category)
        values.append(category)
    if len(values) != len(set(values)):
        raise ValueError("Duplicate BuyLimitations native enum")
    return values


def make_probe(sid, updated):
    lines = [
        f"{sid} : struct.begin {{bpatch}}",
        "    TradeGenerators : struct.begin {bpatch}",
        "        [0] : struct.begin {bpatch}",
    ]
    if not updated:
        lines.append("            BuyLimitations : removenode")
    else:
        lines.append("            BuyLimitations : struct.begin")
        lines.extend(f"                [{i}] = EItemType::{v}" for i, v in enumerate(updated))
        lines.append("            struct.end")
    lines.extend(("        struct.end", "    struct.end", "struct.end", ""))
    return "\n".join(lines)


def check_probe(sid, original, updated):
    rendered = make_probe(sid, updated)
    for flag in (
        f"{sid} : struct.begin {{bpatch}}",
        "    TradeGenerators : struct.begin {bpatch}",
        "        [0] : struct.begin {bpatch}",
    ):
        if flag not in rendered:
            raise ValueError("Missing BPatch ancestor")
    if rendered.count("{bpatch}") != 3:
        raise ValueError("Wrong BPatch scope")
    if updated:
        roots, _ = parse_cfg(rendered.encode(), "hypothetical patch fixture")
        if list(roots) != [sid]:
            raise ValueError("Patch modified extra trader roots")
        root = roots[sid]
        one = c(root, "TradeGenerators")
        two = c(one, "[0]")
        if root["fields"] or set(root["children"]) != {"TradeGenerators"}:
            raise ValueError("Patch altered root siblings")
        if one["fields"] or set(one["children"]) != {"[0]"}:
            raise ValueError("Patch altered TradeGenerators siblings")
        if two["fields"] or set(two["children"]) != {"BuyLimitations"}:
            raise ValueError("Patch altered TradeGenerator scalar fields")
        if limits_of(two) != updated:
            raise ValueError("Patch did not retain exact list ordering")
    else:
        if rendered.count("BuyLimitations : removenode") != 1 or "BuyLimitations : struct.begin" in rendered:
            raise ValueError("Unsafe empty-list replacement")
    # Source-structure model: replacing only the BuyLimitations child must not
    # change the named seller rule's scalar siblings (money, condition, price).
    before = copy.deepcopy(original)
    after = copy.deepcopy(original)
    if updated:
        after["children"]["BuyLimitations"] = {
            "key": "BuyLimitations", "attrs": {}, "fields": {
                f"[{i}]": f"EItemType::{name}" for i, name in enumerate(updated)},
            "children": {}, "line": -1,
        }
    else:
        after["children"].pop("BuyLimitations", None)
    if before["fields"] != after["fields"] or before["attrs"] != after["attrs"]:
        raise ValueError("Simulated BPatch altered durability or pricing")
    for key in set(before["children"]) | set(after["children"]):
        if key != "BuyLimitations" and before["children"].get(key) != after["children"].get(key):
            raise ValueError("Simulated BPatch touched unrelated nested data")
    if (limits_of(after) or []) != updated:
        raise ValueError("Simulated BPatch list mismatch")
    return rendered


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--base-zip", type=Path, required=True)
    p.add_argument("--dlc1-zip", type=Path, required=True)
    p.add_argument("--manifest", type=Path, default=ROOT / "src/TrueCustomDifficulty/Data/trader-min-buy-condition-manifest.csv")
    p.add_argument("--buy-csv", type=Path, default=ROOT / "docs/generators/trader-buy-limits-86.csv")
    p.add_argument("--out", type=Path, required=True)
    args = p.parse_args(argv)
    roots = {}
    for scope, path, expected in (
        ("base", args.base_zip, BASE_ZIP_SHA),
        ("dlc1", args.dlc1_zip, DLC_ZIP_SHA),
    ):
        if sha256(path) != expected:
            raise ValueError(f"{scope} ZIP SHA-256 mismatch")
        with zipfile.ZipFile(path) as stream:
            content = stream.read(MEMBERS[scope])
        if hashlib.sha256(content).hexdigest() != HASHES[scope]:
            raise ValueError(f"{scope} original TradePrototypes source mismatch")
        roots[scope], _ = parse_cfg(content, MEMBERS[scope])
    manifests = read_csv(args.manifest)
    previous = read_csv(args.buy_csv)
    m = {(x["scope"], x["prototype"]): x for x in manifests}
    b = {(x["scope"], x["trade_sid"]): x for x in previous}
    if len(m) != 86 or len(b) != 86 or set(m) != set(b):
        raise ValueError("86 source trade SIDs differ from production manifest")
    stats = Counter()
    zero = set()
    probes = {}
    for (scope, sid), row in sorted(m.items()):
        g = c(roots[scope].get(sid), "TradeGenerators", "[0]")
        if not g:
            raise ValueError(f"Missing seller TradeGenerators/[0]: {scope}:{sid}")
        if set(c(roots[scope][sid], "TradeGenerators")["children"]) != {"[0]"}:
            raise ValueError(f"Multiple original seller generators: {scope}:{sid}")
        source_limits = limits_of(g)
        prior = b[(scope, sid)]
        if (source_limits or []) != (prior["buy_limitations"].split(";") if prior["buy_limitations"] else []):
            raise ValueError(f"Existing buyer CSV is stale: {scope}:{sid}")
        if (source_limits is None) != (prior["buy_limitations_state"] == "NOT_AUTHORED"):
            raise ValueError(f"Wrong source authored state: {scope}:{sid}")
        weapon, armor = field(g, "WeaponSellMinDurability"), field(g, "ArmorSellMinDurability")
        if (weapon, armor) not in (("0.4f", "0.4f"), ("0.0f", "0.0f"), ("", "")):
            raise ValueError(f"Unexpected original durability structure: {sid}")
        stats["local_40_percent" if weapon == "0.4f" else "local_0_percent" if weapon == "0.0f" else "local_missing"] += 1
        if weapon == "0.0f":
            zero.add(sid)
        if row["vanilla"] != ("0.0f" if weapon == "0.0f" else "0.4f"):
            raise ValueError(f"Production min-condition manifest baseline mismatch: {sid}")
        if source_limits is not None:
            stats["explicit_lists"] += 1
        else:
            stats["missing_lists"] += 1
        stats[scope] += 1
        for kind in TYPES:
            old = list(source_limits or [])
            new = [v for v in old if v != kind] if kind in old else old + [kind]
            stats["remove" if kind in old else "add"] += 1
            if not new:
                stats["empty"] += 1
            patch = check_probe(sid, g, new)
            if (sid, kind) in {
                ("Asylum_Medic_TradePrototype", "Consumable"),
                ("EgerTrader_TradePrototype", "Consumable"),
                ("Bartender_Zalesie_TradePrototype", "MutantLoot"),
                ("Trader_CementPlant_TradePrototype", "MutantLoot"),
                ("SelmaTrader_TradePrototype", "Weapon"),
            }:
                probes[sid + "__" + kind + ".cfg.txt"] = patch
            stats["probe_count"] += 1
    expected = dict(base=68, dlc1=18, explicit_lists=53, missing_lists=33,
                    local_40_percent=62, local_0_percent=2, local_missing=22,
                    add=506, remove=182, empty=13, probe_count=688)
    if dict(stats) != expected or zero != ZERO:
        raise ValueError(f"Native-source coverage drift: {dict(stats)} zero={sorted(zero)}")
    args.out.mkdir(parents=True, exist_ok=True)
    for name, source in probes.items():
        (args.out / name).write_text(source, encoding="utf-8")
    summary = {
        "result": "PASS_SOURCE_AND_HYPOTHETICAL_PATCH_TREE_ONLY",
        "counts": expected, "zero_condition_exceptions": sorted(zero),
        "not_verified": [
            "Unreal patch engine application", "compiled production writer output",
            "external mod conflict order", "in-game trader buying behavior",
        ],
    }
    (args.out / "trader-bpatch-source-verification.json").write_text(
        json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print("[PASS] 86 profiles; 53 lists; 688 source-only list add/remove probes")
    print("[PASS] 62 explicit 40%, 2 explicit 0%, 22 missing local durability fields")
    print("NOT VERIFIED: Unreal parser, actual game, external patch conflicts")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError) as exc:
        print("[FAIL] trader BPatch source contract:", exc, file=sys.stderr)
        raise SystemExit(1)
