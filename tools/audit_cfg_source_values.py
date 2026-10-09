#!/usr/bin/env python3
"""Audit all direct writer edit points against decoded base/DLC1 CFG source.

Per source: resolve top-level prototype by exact block name then SID, walk
nodePath exactly and compare sourceRaw with the actual original property.
Structural failures and inherited/absent fields remain findings, never
silently interpreted as safe. This is NOT a gameplay/runtime verification.

Example:
  python tools/audit_cfg_source_values.py \
    --base-zip "$HOME/s2tools/cfg_dump/STALKER2-GameData-CFG.zip" \
    --dlc1-zip "$HOME/s2tools/cfg_dump/STALKER2-DLC1-CFG.zip" \
    --out audit/cfg-source-values.json --strict
"""
from __future__ import annotations

import argparse
import collections
import json
import re
import sys
import zipfile
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "src" / "TrueCustomDifficulty" / "Data" / "cfg-writer-baseline.json"
START = re.compile(r"^\s*(.*?)\s*:\s*struct\.begin(?:\s|$)")
END = re.compile(r"^\s*struct\.end\s*$")
FIELD = re.compile(r"^\s*([^=]+?)\s*=\s*(.*)$")
NUMBER = re.compile(r"^([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)(%|[fF])?$")


@dataclass
class CfgNode:
    name: str
    line: int
    values: dict[str, str] = field(default_factory=dict)
    children: list["CfgNode"] = field(default_factory=list)


def read_tree(contents: str) -> list[CfgNode]:
    roots: list[CfgNode] = []
    stack: list[CfgNode] = []
    for line_number, line in enumerate(contents.splitlines(), 1):
        if line.lstrip().startswith("//"):
            continue
        start = START.match(line)
        if start:
            node = CfgNode(start.group(1).strip(), line_number)
            (stack[-1].children if stack else roots).append(node)
            stack.append(node)
        elif END.match(line):
            if not stack:
                raise ValueError(f"Unexpected struct.end on line {line_number}")
            stack.pop()
        elif stack:
            value = FIELD.match(line)
            if value:
                key = value.group(1).strip()
                if key in stack[-1].values:
                    raise ValueError(f"Duplicate key {key} on line {line_number}")
                stack[-1].values[key] = value.group(2).strip()
    if stack:
        raise ValueError(f"Unterminated struct at line {stack[-1].line}")
    return roots


def compare_values(actual: str, expected: str) -> str:
    if actual == expected:
        return "exact"
    a, b = NUMBER.fullmatch(actual), NUMBER.fullmatch(expected)
    if a and b:
        av, bv = float(a.group(1)), float(b.group(1))
        if a.group(2) == "%":
            av /= 100.0
        if b.group(2) == "%":
            bv /= 100.0
        if abs(av - bv) <= 1e-8 * max(1.0, abs(av), abs(bv)):
            return "equivalent_format"
    if actual.lower() == expected.lower() and actual.lower() in {"true", "false"}:
        return "equivalent_format"
    return "raw_mismatch"


def check_point(point: dict, roots: list[CfgNode]) -> dict:
    prototype = point["prototype"]
    matches = [n for n in roots if n.name == prototype]
    lookup = "block"
    if not matches:
        matches = [n for n in roots if n.values.get("SID") == prototype]
        lookup = "SID"
    result = {
        "parameterId": point["parameterId"], "scope": point["scope"],
        "file": point["file"], "prototype": prototype,
        "nodePath": point.get("nodePath", []),
        "key": point["key"], "expectedRaw": point["sourceRaw"],
        "lookup": lookup,
    }
    if len(matches) != 1:
        return dict(result, status="missing_prototype" if not matches else "ambiguous_prototype")
    node = matches[0]
    result["prototypeLine"] = node.line
    for segment in point.get("nodePath") or []:
        matching_children = [n for n in node.children if n.name == segment]
        if len(matching_children) != 1:
            return dict(result,
                        status="missing_node" if not matching_children else "ambiguous_node",
                        atSegment=segment, availableChildren=[n.name for n in node.children[:12]])
        node = matching_children[0]
    if point["key"] not in node.values:
        return dict(result, status="missing_key", availableKeys=list(node.values)[:16])
    actual = node.values[point["key"]]
    return dict(result, status=compare_values(actual, point["sourceRaw"]),
                actualRaw=actual, leafNodeLine=node.line)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--base-zip", required=True, type=Path)
    ap.add_argument("--dlc1-zip", required=True, type=Path)
    ap.add_argument("--out", type=Path)
    ap.add_argument("--strict", action="store_true",
                    help="Exit nonzero on any discrepancy, including raw formatting")
    args = ap.parse_args()
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    points = baseline["editPoints"]
    by_file = collections.defaultdict(list)
    for point in points:
        by_file[(point["scope"], point["file"])].append(point)
    results = []
    file_summaries = []
    with zipfile.ZipFile(args.base_zip) as base, zipfile.ZipFile(args.dlc1_zip) as dlc:
        archives = {"baseGame": base, "dlc1": dlc}
        for (scope, file_name), file_points in sorted(by_file.items()):
            archive = archives.get(scope)
            if archive is None or file_name not in archive.namelist():
                results.extend(dict(parameterId=p["parameterId"], scope=scope,
                                    file=file_name, prototype=p["prototype"],
                                    status="missing_file") for p in file_points)
            else:
                roots = read_tree(archive.read(file_name).decode("utf-8-sig"))
                results.extend(check_point(p, roots) for p in file_points)
            subtotal = results[-len(file_points):]
            file_summaries.append({
                "scope": scope, "file": file_name, "total": len(file_points),
                "counts": dict(sorted(collections.Counter(r["status"] for r in subtotal).items())),
            })

    statuses = dict(sorted(collections.Counter(r["status"] for r in results).items()))
    findings = [r for r in results if r["status"] != "exact"]
    report = {
        "totalDirectEditPoints": len(points), "statusCounts": statuses,
        "files": file_summaries, "findings": findings,
        "interpretation": (
            "Exact source fields only. A missing source field can be inherited or "
            "optional; investigate before changing a writer. This audit does not "
            "validate special writers, mod order or runtime gameplay."
        ),
    }
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n",
                            encoding="utf-8")
    print("Direct CFG source value and structure audit")
    print(f"Targets checked: {len(points)} | Status: {statuses}")
    for f in file_summaries:
        print(f"  {f['scope']}/{f['file']}: {f['counts']}")
    for finding in findings[:60]:
        print("  INVESTIGATE", finding["status"], finding["parameterId"],
              finding["prototype"], "/".join(finding.get("nodePath") or []),
              finding.get("key"), "expected", finding.get("expectedRaw"),
              "actual", finding.get("actualRaw"))
    if len(findings) > 60:
        print(f"  ... plus {len(findings) - 60} other findings in JSON report")
    print("PASS" if not findings else "NEEDS TRIAGE — source findings not concealed")
    return int(args.strict and bool(findings))


if __name__ == "__main__":
    sys.exit(main())
