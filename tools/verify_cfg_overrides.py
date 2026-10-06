#!/usr/bin/env python3
"""Verify STALKER2 TCD direct CFG writer overrides against the locked baseline.

This tool has two deliberately separate proof levels:

1. plan/suite: calculate the exact expected original -> override assignments from
   Data/parameters.json + Data/cfg-writer-baseline.json and emit a reference
   BPatch plus CSV/Markdown evidence. This proves the baseline/transform contract.
2. compare: parse a real production writer staging tree and require its concrete
   assignments to match the expected set exactly. This proves the actual writer
   output for an isolated single-control test.

The verifier covers both ordinary cfg-writer-baseline editPoints and the special
writer routes used by runtime-pending controls: PSY arrays, mutant cooldown
arrays, and generated clone/redirect loot surfaces. Reference patches model the
assignments the production writer is expected to emit for an isolated control.
"""
from __future__ import annotations

import argparse
import csv
import json
import math
import re
import shutil
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[1]
CATALOG_PATH = ROOT / "Data" / "parameters.json"
BASELINE_PATH = ROOT / "Data" / "cfg-writer-baseline.json"
PATCH_SUFFIX = "TrueCustomDifficulty"
EPS = 1e-7


@dataclass(frozen=True, order=True)
class Target:
    scope: str
    file: str
    prototype: str
    node_path: tuple[str, ...]
    key: str

    def display_path(self) -> str:
        parts = (self.prototype, *self.node_path, self.key)
        return ".".join(parts)


@dataclass(frozen=True)
class ExpectedRow:
    target: Target
    operation: str
    source_raw: str
    expected_raw: str

    @property
    def changed(self) -> bool:
        return not values_equivalent(self.source_raw, self.expected_raw)


@dataclass(frozen=True, order=True)
class StructureTarget:
    file: str
    prototype: str
    node_path: tuple[str, ...]

    def display_path(self) -> str:
        return ".".join((self.prototype, *self.node_path))


@dataclass(frozen=True)
class ExpectedStructure:
    target: StructureTarget
    mode: str  # bpatch | plain | ref
    ref_url: str = ""
    ref_key: str = ""

    def descriptor(self) -> str:
        if self.mode == "ref":
            return f"refurl={self.ref_url};refkey={self.ref_key}"
        return self.mode


def load_json(path: Path):
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def load_model():
    catalog = load_json(CATALOG_PATH)
    baseline = load_json(BASELINE_PATH)
    parameters = {p["id"]: p for p in catalog["parameters"]}
    return catalog, baseline, parameters


def resolve_difficulty_sid(catalog: dict, difficulty: str) -> str:
    profiles = catalog.get("difficultyProfiles") or {}
    if difficulty in profiles:
        return profiles[difficulty]["sid"]
    for profile in profiles.values():
        if str(profile.get("sid", "")).lower() == difficulty.lower():
            return profile["sid"]
    return difficulty


def scalar_value(parameter: dict, value) -> str:
    control = parameter.get("control")
    if control == "toggle":
        return "true" if bool(value) else "false"
    if control == "slider":
        return format_number(float(value))
    if control == "choiceSlider":
        return str(value)
    raise ValueError(f"unsupported scalar control {control!r}: {parameter.get('id')}")


def value_matches_vanilla(parameter: dict, value, difficulty: str) -> bool:
    vanilla = resolve_vanilla(parameter, difficulty)
    if parameter.get("control") == "toggle":
        return bool(value) is bool(vanilla)
    if isinstance(value, (int, float)) and isinstance(vanilla, (int, float)):
        return abs(float(value) - float(vanilla)) < EPS
    return str(value) == str(vanilla)


def parse_number(raw: str) -> float:
    text = raw.strip()
    percent = text.endswith("%")
    if percent:
        text = text[:-1].strip()
    if text.lower().endswith("f"):
        text = text[:-1].strip()
    if text.endswith("."):
        text += "0"
    value = float(text)
    return value / 100.0 if percent else value


def format_number(value: float) -> str:
    if abs(value) < 1e-10:
        value = 0.0
    # C# ToString("0.######", InvariantCulture): up to 6 decimals, no exponent.
    text = f"{value:.6f}".rstrip("0").rstrip(".")
    return text or "0"


def is_whole(value: float) -> bool:
    return abs(value - round(value)) < EPS


def format_like_source(value: float, source_raw: str) -> str:
    source = source_raw.strip()
    percent = source.endswith("%")
    float_suffix = source.lower().endswith("f")
    dot_float_style = float_suffix and len(source) >= 2 and source[-2] == "."
    explicit_positive = source.startswith("+")

    if percent:
        scaled = value * 100.0
        body = format_number(scaled)
        if explicit_positive and scaled > 0:
            body = "+" + body
        return body + "%"

    result = format_number(value)
    if float_suffix:
        if dot_float_style and is_whole(value):
            result = str(round(value)) + "."
        return result + "f"
    if explicit_positive and value > 0:
        result = "+" + result
    return result


def values_equivalent(a: str, b: str) -> bool:
    if a.strip().lower() == b.strip().lower():
        return True
    if a.strip().lower() in {"true", "false"} and b.strip().lower() in {"true", "false"}:
        return a.strip().lower() == b.strip().lower()
    try:
        return abs(parse_number(a) - parse_number(b)) < EPS
    except ValueError:
        return False


def transform_value(parameter: dict, operation: str, source_raw: str, value: float | str | bool) -> str:
    if operation == "set":
        control = parameter.get("control")
        if control == "toggle":
            return "true" if bool(value) else "false"
        if control == "slider":
            return format_like_source(float(value), source_raw)
        if control == "choiceSlider":
            return str(value)
        raise ValueError(f"unsupported direct set control {control!r}")
    if operation == "negateSet":
        return format_like_source(-float(value), source_raw)
    if operation == "inverseScale":
        number = float(value)
        if number <= EPS:
            raise ValueError("inverseScale requires a value greater than zero")
        return format_like_source(parse_number(source_raw) / number, source_raw)
    if operation == "scale":
        return format_like_source(parse_number(source_raw) * float(value), source_raw)
    if operation == "scaleNonZero":
        source = parse_number(source_raw)
        out = source if abs(source) < EPS else source * float(value)
        return format_like_source(out, source_raw)
    if operation == "scaleClamp01":
        source = parse_number(source_raw)
        out = min(1.0, max(0.0, source * float(value)))
        return format_like_source(out, source_raw)
    if operation in {"scaleIntegerHalfUp", "scaleIntegerHalfUpPreserveZero"}:
        source = parse_number(source_raw)
        preserve_zero = operation.endswith("PreserveZero")
        if preserve_zero and abs(source) < EPS:
            return format_like_source(0.0, source_raw)
        scaled = math.floor(source * float(value) + 0.5)
        scaled = max(0 if preserve_zero else 1, scaled)
        return format_like_source(float(scaled), source_raw)
    if operation == "setDeltaFromRetainedFraction":
        return format_like_source(float(value) - 1.0, source_raw)
    raise ValueError(f"unsupported direct writer operation {operation!r}")


def resolve_vanilla(parameter: dict, difficulty: str) -> float | str | bool:
    vanilla = parameter.get("vanilla") or {}
    if difficulty in vanilla:
        return vanilla[difficulty]
    if "All" in vanilla:
        return vanilla["All"]
    # Friendly aliases for CLI use.
    aliases = {
        "easy": "Easy",
        "medium": "Medium",
        "hard": "Hard",
        "master": "Master",
    }
    key = aliases.get(difficulty.lower())
    if key and key in vanilla:
        return vanilla[key]
    raise ValueError(f"parameter {parameter['id']} has no vanilla value for {difficulty!r}: {vanilla}")


def direct_points(baseline: dict, parameter_id: str, difficulty: str, include_dlc1: bool) -> list[dict]:
    points = []
    for point in baseline.get("editPoints", []):
        if point.get("parameterId") != parameter_id:
            continue
        diff_sid = (point.get("difficultySid") or "").strip()
        if diff_sid and diff_sid.lower() != difficulty.lower():
            continue
        scope = (point.get("scope") or "").lower()
        if scope == "dlc1" and not include_dlc1:
            continue
        points.append(point)
    return points


def expected_rows(parameter: dict, baseline: dict, value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    points = direct_points(baseline, parameter["id"], difficulty, include_dlc1)
    if not points:
        raise ValueError(
            f"{parameter['id']} has no ordinary editPoints for difficulty={difficulty!r}; "
            "it may use a special writer route not supported by this verifier"
        )
    rows = []
    seen = set()
    for point in points:
        target = Target(
            point.get("scope") or "",
            point["file"].replace("\\", "/"),
            point["prototype"],
            tuple(point.get("nodePath") or []),
            point["key"],
        )
        if target in seen:
            raise ValueError(f"duplicate concrete target in baseline for {parameter['id']}: {target}")
        seen.add(target)
        source_raw = str(point.get("sourceRaw", ""))
        operation = point["operation"]
        out = transform_value(parameter, operation, source_raw, value)
        rows.append(ExpectedRow(target, operation, source_raw, out))
    return sorted(rows, key=lambda r: r.target)


def source_to_output_path(root: Path, logical_file: str) -> Path:
    logical = Path(logical_file)
    base = root / "Stalker2" / "Content" / "GameLite" / logical.parent
    if logical.name.lower().endswith("prototypes.cfg"):
        stem = logical.stem
        return base / stem / f"{stem}_patch_{PATCH_SUFFIX}.cfg"
    return base / f"{logical.name}_patch_{PATCH_SUFFIX}"


def natural_key(text: str):
    m = re.fullmatch(r"\[(\d+)\]", text)
    return (0, int(m.group(1))) if m else (1, text)


def render_reference_patch(rows: Iterable[ExpectedRow], output_root: Path,
                           structures: Iterable[ExpectedStructure] | None = None) -> list[Path]:
    changed = [r for r in rows if r.changed]
    structure_list = list(structures or [])
    by_file: dict[str, dict[str, dict[tuple[str, ...], dict[str, str]]]] = defaultdict(
        lambda: defaultdict(lambda: defaultdict(dict))
    )
    for row in changed:
        by_file[row.target.file][row.target.prototype][row.target.node_path][row.target.key] = row.expected_raw

    structure_map = {
        (s.target.file, s.target.prototype, s.target.node_path): s
        for s in structure_list
    }
    files = set(by_file) | {s.target.file for s in structure_list}
    written = []
    for logical_file in sorted(files):
        prototypes = set(by_file.get(logical_file, {})) | {
            s.target.prototype for s in structure_list if s.target.file == logical_file
        }
        path = source_to_output_path(output_root, logical_file)
        path.parent.mkdir(parents=True, exist_ok=True)
        lines = [
            "// Reference patch generated by tools/verify_cfg_overrides.py.",
            "// CONTRACT REFERENCE ONLY - compare it with production writer output.",
            f"// BPatch source: {logical_file}",
            "",
        ]

        def new_node():
            return {"assign": {}, "children": {}}

        for prototype in sorted(prototypes, key=natural_key):
            root = new_node()
            # Materialize structure-only nodes first.
            relevant_structures = [
                s for s in structure_list
                if s.target.file == logical_file and s.target.prototype == prototype
            ]
            for structure in sorted(relevant_structures, key=lambda x: (len(x.target.node_path), x.target.node_path)):
                node = root
                for segment in structure.target.node_path:
                    node = node["children"].setdefault(segment, new_node())
            # Then attach assignments.
            for node_path, assignments in by_file.get(logical_file, {}).get(prototype, {}).items():
                node = root
                for segment in node_path:
                    node = node["children"].setdefault(segment, new_node())
                node["assign"].update(assignments)

            def emit(name: str, node: dict, node_path: tuple[str, ...], indent: int):
                prefix = " " * (indent * 4)
                structure = structure_map.get((logical_file, prototype, node_path))
                if structure is None:
                    suffix = " {bpatch}"
                elif structure.mode == "bpatch":
                    suffix = " {bpatch}"
                elif structure.mode == "ref":
                    suffix = f" {{refurl={structure.ref_url};refkey={structure.ref_key}}}"
                elif structure.mode == "plain":
                    suffix = ""
                else:
                    raise ValueError(f"unsupported structure mode: {structure.mode}")
                lines.append(f"{prefix}{name} : struct.begin{suffix}")
                for key in sorted(node["assign"], key=natural_key):
                    value = node["assign"][key]
                    value_suffix = f" {value}" if value.strip() else ""
                    lines.append(f"{' ' * ((indent + 1) * 4)}{key} ={value_suffix}")
                for child_name in sorted(node["children"], key=natural_key):
                    emit(child_name, node["children"][child_name], (*node_path, child_name), indent + 1)
                lines.append(f"{prefix}struct.end")

            emit(prototype, root, (), 0)
            lines.append("")
        path.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8", newline="\n")
        written.append(path)
    return written


def parse_patch_file(path: Path) -> tuple[str, dict[Target, str], dict[StructureTarget, ExpectedStructure]]:
    logical_file = ""
    stack: list[str] = []
    assignments: dict[Target, str] = {}
    structures: dict[StructureTarget, ExpectedStructure] = {}
    source_re = re.compile(r"^\s*//\s*BPatch source:\s*(.+?)\s*$", re.I)
    begin_re = re.compile(r"^\s*(.+?)\s*:\s*struct\.begin(?:\s+\{(.*)\})?\s*$")
    assign_re = re.compile(r"^\s*([^=\s]+)\s*=\s*(.*?)\s*$")
    for lineno, raw in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
        m = source_re.match(raw)
        if m:
            logical_file = m.group(1).strip().replace("\\", "/")
            continue
        stripped = raw.strip()
        if not stripped or stripped.startswith("//"):
            continue
        m = begin_re.match(raw)
        if m:
            if not logical_file:
                raise ValueError(f"{path}:{lineno}: missing // BPatch source header before struct")
            name = m.group(1).strip()
            attrs = (m.group(2) or "").strip()
            stack.append(name)
            prototype = stack[0]
            node_path = tuple(stack[1:])
            if attrs.lower() == "bpatch":
                mode, ref_url, ref_key = "bpatch", "", ""
            elif attrs:
                pairs = {}
                for part in attrs.split(";"):
                    if "=" in part:
                        key, value = part.split("=", 1)
                        pairs[key.strip().lower()] = value.strip()
                if "refurl" in pairs or "refkey" in pairs:
                    mode = "ref"
                    ref_url = pairs.get("refurl", "")
                    ref_key = pairs.get("refkey", "")
                else:
                    raise ValueError(f"{path}:{lineno}: unsupported struct attributes {{{attrs}}}")
            else:
                mode, ref_url, ref_key = "plain", "", ""
            target = StructureTarget(logical_file, prototype, node_path)
            structure = ExpectedStructure(target, mode, ref_url, ref_key)
            if target in structures:
                raise ValueError(f"{path}:{lineno}: duplicate structure {target.display_path()}")
            structures[target] = structure
            continue
        if stripped == "struct.end":
            if not stack:
                raise ValueError(f"{path}:{lineno}: unmatched struct.end")
            stack.pop()
            continue
        m = assign_re.match(raw)
        if m:
            if not logical_file:
                raise ValueError(f"{path}:{lineno}: missing // BPatch source header")
            if not stack:
                raise ValueError(f"{path}:{lineno}: assignment outside a prototype")
            prototype = stack[0]
            node_path = tuple(stack[1:])
            key, value = m.group(1), m.group(2)
            target = Target("", logical_file, prototype, node_path, key)
            if target in assignments:
                raise ValueError(f"{path}:{lineno}: duplicate assignment {target.display_path()}")
            assignments[target] = value
            continue
        raise ValueError(f"{path}:{lineno}: unrecognized patch syntax: {raw!r}")
    if stack:
        raise ValueError(f"{path}: unterminated struct stack: {stack}")
    if not logical_file:
        raise ValueError(f"{path}: no // BPatch source header")
    return logical_file, assignments, structures


def discover_actual_contract(override_root: Path) -> tuple[dict[Target, str], dict[StructureTarget, ExpectedStructure]]:
    assignments: dict[Target, str] = {}
    structures: dict[StructureTarget, ExpectedStructure] = {}
    for path in sorted(override_root.rglob("*")):
        if not path.is_file() or "_patch_" not in path.name:
            continue
        try:
            _, parsed_assignments, parsed_structures = parse_patch_file(path)
        except UnicodeDecodeError:
            continue
        for target, value in parsed_assignments.items():
            key = Target("", target.file, target.prototype, target.node_path, target.key)
            if key in assignments:
                raise ValueError(f"duplicate concrete assignment across patch files: {key}")
            assignments[key] = value
        for target, structure in parsed_structures.items():
            if target in structures:
                raise ValueError(f"duplicate concrete structure across patch files: {target}")
            structures[target] = structure
    return assignments, structures


def discover_actual_assignments(override_root: Path) -> dict[Target, str]:
    assignments, _ = discover_actual_contract(override_root)
    return assignments


def normalized_expected_changed(rows: Iterable[ExpectedRow]) -> dict[Target, str]:
    out = {}
    for row in rows:
        if not row.changed:
            continue
        t = row.target
        key = Target("", t.file, t.prototype, t.node_path, t.key)
        out[key] = row.expected_raw
    return out


def write_case_evidence(output: Path, parameter: dict, value, difficulty: str,
                        rows: list[ExpectedRow], structures: list[ExpectedStructure] | None = None,
                        actual=None, actual_structures=None):
    structures = list(structures or [])
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True, exist_ok=True)
    reference_root = output / "expected-patch"
    patch_files = render_reference_patch(rows, reference_root, structures)

    with (output / "diff.csv").open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(["scope", "file", "prototype", "nodePath", "key", "operation", "original", "expectedOverride", "changed"])
        for row in rows:
            w.writerow([
                row.target.scope,
                row.target.file,
                row.target.prototype,
                ".".join(row.target.node_path),
                row.target.key,
                row.operation,
                row.source_raw,
                row.expected_raw,
                "yes" if row.changed else "no",
            ])

    with (output / "structure.csv").open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(["file", "prototype", "nodePath", "mode", "refUrl", "refKey"])
        for structure in structures:
            w.writerow([
                structure.target.file,
                structure.target.prototype,
                ".".join(structure.target.node_path),
                structure.mode,
                structure.ref_url,
                structure.ref_key,
            ])

    changed = sum(r.changed for r in rows)
    status = "CONTRACT_PASS"
    compare = None
    if actual is not None:
        expected = normalized_expected_changed(rows)
        missing = sorted(set(expected) - set(actual))
        unexpected = sorted(set(actual) - set(expected))
        wrong = sorted(k for k in set(expected) & set(actual) if not values_equivalent(expected[k], actual[k]))

        expected_struct = {s.target: s.descriptor() for s in structures}
        actual_struct = {k: v.descriptor() for k, v in (actual_structures or {}).items()}
        missing_struct = sorted(set(expected_struct) - set(actual_struct))
        unexpected_struct = sorted(set(actual_struct) - set(expected_struct))
        wrong_struct = sorted(
            k for k in set(expected_struct) & set(actual_struct)
            if expected_struct[k] != actual_struct[k]
        )

        status = "PASS" if not missing and not unexpected and not wrong and not missing_struct and not unexpected_struct and not wrong_struct else "FAIL"
        compare = {
            "missing": len(missing),
            "unexpected": len(unexpected),
            "wrongValue": len(wrong),
            "missingStructure": len(missing_struct),
            "unexpectedStructure": len(unexpected_struct),
            "wrongStructure": len(wrong_struct),
            "missingTargets": [k.display_path() for k in missing],
            "unexpectedTargets": [k.display_path() for k in unexpected],
            "wrongTargets": [
                {"target": k.display_path(), "expected": expected[k], "actual": actual[k]} for k in wrong
            ],
            "missingStructures": [k.display_path() for k in missing_struct],
            "unexpectedStructures": [k.display_path() for k in unexpected_struct],
            "wrongStructures": [
                {"target": k.display_path(), "expected": expected_struct[k], "actual": actual_struct[k]}
                for k in wrong_struct
            ],
        }

    summary = {
        "status": status,
        "parameterId": parameter["id"],
        "label": parameter.get("label"),
        "difficulty": difficulty,
        "inputValue": value,
        "totalBaselineEditPoints": len(rows),
        "expectedChangedEditPoints": changed,
        "expectedUnchangedEditPoints": len(rows) - changed,
        "expectedStructures": len(structures),
        "referencePatchFiles": len(patch_files),
        "comparison": compare,
    }
    (output / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    md = [
        f"# Override verification — `{parameter['id']}`",
        "",
        f"- Status: **{status}**",
        f"- Label: **{parameter.get('label', '')}**",
        f"- Difficulty: **{difficulty}**",
        f"- Input value: **{value}**",
        f"- Contract assignment rows: **{len(rows)}**",
        f"- Expected assignments: **{changed}**",
        f"- Expected non-assignments/no-ops: **{len(rows) - changed}**",
        f"- Expected struct nodes: **{len(structures)}**",
        f"- Reference patch files: **{len(patch_files)}**",
        "",
    ]
    if actual is None:
        md += [
            "`CONTRACT_PASS` means the locked baseline/special-route rules produced a deterministic",
            "assignment **and structure** contract. It does **not** claim that the C# production writer was executed.",
            "Use `compare` with a real single-control staging tree to obtain writer-output `PASS`.",
            "",
        ]
    else:
        md += [
            f"- Missing assignments: **{compare['missing']}**",
            f"- Unexpected assignments: **{compare['unexpected']}**",
            f"- Wrong values: **{compare['wrongValue']}**",
            f"- Missing struct nodes: **{compare['missingStructure']}**",
            f"- Unexpected struct nodes: **{compare['unexpectedStructure']}**",
            f"- Wrong struct mode/reference: **{compare['wrongStructure']}**",
            "",
        ]
    md += [
        "Field-level evidence is in `diff.csv`; structural mode/reference evidence is in `structure.csv`;",
        "the exact reference BPatch is under `expected-patch/`.",
        "",
    ]
    (output / "summary.md").write_text("\n".join(md), encoding="utf-8")
    return summary



def make_row(file: str, prototype: str, node_path, key: str, expected, *,
             source="<absent>", scope="baseGame", operation="special") -> ExpectedRow:
    return ExpectedRow(
        Target(scope, file.replace("\\", "/"), prototype, tuple(node_path), key),
        operation,
        str(source),
        str(expected),
    )


def manifest_path(resource_name: str) -> Path:
    marker = ".Data."
    name = resource_name.split(marker, 1)[1] if marker in resource_name else resource_name.rsplit(".", 1)[-1]
    return ROOT / "Data" / name


def load_redirect_rows(resource_name: str, include_dlc1: bool) -> list[ExpectedRow]:
    path = manifest_path(resource_name)
    if not path.is_file():
        raise ValueError(f"redirect manifest not found: {path}")
    rows: list[ExpectedRow] = []
    with path.open(encoding="utf-8-sig", newline="") as f:
        for record in csv.DictReader(f):
            logical_file = record["file"].strip().replace("\\", "/")
            explicit_scope = (record.get("scope") or "").strip()
            scope = explicit_scope or ("dlc1" if logical_file.lower().startswith("dlcgamedata/dlc1/") else "baseGame")
            if scope.lower() == "dlc1" and not include_dlc1:
                continue
            node_path = tuple(x.strip() for x in record["node_path"].split(".") if x.strip())
            rows.append(make_row(
                logical_file,
                record["prototype"].strip(),
                node_path,
                record["key"].strip(),
                record["target_value"].strip(),
                source=record.get("source_value", "<unknown>").strip(),
                scope=scope,
                operation="redirect",
            ))
    return rows


def append_template_rows(rows: list[ExpectedRow], file: str, sid: str, categories: list[dict], dynamic_value):
    rows.append(make_row(file, sid, (), "SID", sid, operation="generatedRoot"))
    for category in categories:
        category_node = category["node"]
        category_path = ("ItemGenerator", category_node)
        for key, raw in category.get("fields", {}).items():
            rows.append(make_row(file, sid, category_path, key, raw, operation="generatedStatic"))
        for item in category.get("items", []):
            item_node = item["node"]
            item_path = ("ItemGenerator", category_node, "PossibleItems", item_node)
            for key, raw in item.get("fields", {}).items():
                expected = dynamic_value(category_node, item_node, key, raw)
                rows.append(make_row(file, sid, item_path, key, expected, operation="generatedTemplate"))


def special_psy_rows(catalog: dict, parameter: dict, parameters: dict, baseline: dict, value, difficulty: str) -> list[ExpectedRow]:
    if value_matches_vanilla(parameter, value, difficulty):
        return []
    special = baseline["special"]
    pid = parameter["id"]
    key_map = special["psyParameterKeys"]
    if pid not in key_map:
        raise ValueError(f"{pid} is not a PSY special parameter")
    difficulty_sid = resolve_difficulty_sid(catalog, difficulty)
    full_tree = difficulty_sid.lower() != "stalker"
    rows: list[ExpectedRow] = []
    for entry in sorted(special["psyOverrides"].values(), key=lambda x: natural_key(x["index"])):
        base = ("NPCCombatDifficulty", "PsyPhantomNPCOverrides", entry["index"])
        rows.append(make_row("GameData/DifficultyPrototypes.cfg", difficulty_sid, base,
                             "OverrideEffectPrototypeSID", entry["effectSid"], source="<writer-emits>", operation="psyStatic"))
        ids = sorted(key_map) if full_tree else [pid]
        for current_id in ids:
            current_parameter = parameters[current_id]
            current_value = value if current_id == pid else resolve_vanilla(current_parameter, difficulty)
            rows.append(make_row("GameData/DifficultyPrototypes.cfg", difficulty_sid, base,
                                 key_map[current_id], scalar_value(current_parameter, current_value),
                                 source="<writer-emits>", operation="psyScalar"))
    return rows


def special_mutant_rows(catalog: dict, parameter: dict, parameters: dict, baseline: dict, value, difficulty: str) -> list[ExpectedRow]:
    if value_matches_vanilla(parameter, value, difficulty):
        return []
    special = baseline["special"]
    pid = parameter["id"]
    parameter_map = special["mutantCooldownParameters"]
    if pid not in parameter_map:
        raise ValueError(f"{pid} is not a mutant cooldown special parameter")
    difficulty_sid = resolve_difficulty_sid(catalog, difficulty)
    full_tree = difficulty_sid.lower() != "stalker"
    agent_to_parameter = {
        agent: parameter_id
        for parameter_id, agents in parameter_map.items()
        for agent in agents
    }
    allowed_agents = None if full_tree else set(parameter_map[pid])
    rows: list[ExpectedRow] = []
    families = sorted(special["mutantCooldownFamilies"].values(), key=lambda x: natural_key(x["index"]))
    for family in families:
        agent = family["agentPrototypeSid"]
        if allowed_agents is not None and agent not in allowed_agents:
            continue
        mapped_pid = agent_to_parameter.get(agent)
        if not mapped_pid:
            raise ValueError(f"no mutant parameter maps family {agent}")
        mapped_parameter = parameters[mapped_pid]
        mapped_value = value if mapped_pid == pid else resolve_vanilla(mapped_parameter, difficulty)
        family_path = ("MutantCombatDifficulty", "AgentCooldownMultipliers", family["index"])
        rows.append(make_row("GameData/DifficultyPrototypes.cfg", difficulty_sid, family_path,
                             "AgentPrototypeSID", agent, source="<writer-emits>", operation="mutantStatic"))
        for cooldown in sorted(family.get("cooldowns", []), key=lambda x: natural_key(x["index"])):
            cooldown_path = (*family_path, "CooldownMultipliers", cooldown["index"])
            rows.append(make_row("GameData/DifficultyPrototypes.cfg", difficulty_sid, cooldown_path,
                                 "CooldownTag", cooldown["tag"], source="<writer-emits>", operation="mutantStatic"))
            rows.append(make_row("GameData/DifficultyPrototypes.cfg", difficulty_sid, cooldown_path,
                                 "CooldownMultiplier", scalar_value(mapped_parameter, mapped_value),
                                 source="<writer-emits>", operation="mutantScalar"))
    return rows


def special_npc_medical_rows(parameter: dict, baseline: dict, value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    if value_matches_vanilla(parameter, value, difficulty):
        return []
    config = baseline["special"]["npcMedicalLootProduction"]
    rows: list[ExpectedRow] = []
    for pool in config["pools"]:
        medical = {(x["categoryNode"], x["entryNode"]): x for x in pool.get("medicalEntries", [])}
        def dynamic(cat, item, key, raw):
            entry = medical.get((cat, item))
            if entry is not None and key == "Chance":
                source = parse_number(entry["sourceChanceRaw"])
                return format_like_source(min(1.0, max(0.0, source * float(value))), entry["sourceChanceRaw"])
            return raw
        append_template_rows(rows, config["file"], pool["customSid"], pool["templateCategories"], dynamic)
    rows.extend(load_redirect_rows(config["redirectManifestResource"], include_dlc1))
    return rows


def scale_positive_integer(source: int, multiplier: float) -> str:
    return str(max(1, math.floor(int(source) * float(multiplier) + 0.5)))


def special_cheap_stash_rows(parameter: dict, baseline: dict, value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    if value_matches_vanilla(parameter, value, difficulty):
        return []
    config = baseline["special"]["cheapStashProduction"]
    pid = parameter["id"]
    ammo_id = config["ammoQuantityParameterId"]
    grenade_id = config["grenadeChanceParameterId"]
    grenade_quantity_id = config["grenadeQuantityParameterId"]
    if pid not in {ammo_id, grenade_id, grenade_quantity_id}:
        raise ValueError(f"{pid} is not a cheap-stash special parameter")
    ammo_multiplier = float(value) if pid == ammo_id else 1.0
    grenade_chance = float(value) if pid == grenade_id else parse_number(config["grenadeEntry"]["sourceChanceRaw"])
    grenade_quantity = int(round(float(value))) if pid == grenade_quantity_id else int(config["grenadeEntry"].get("sourceMinCount", 1))
    ammo = {x["entryNode"]: x for x in config["ammoEntries"]}
    grenade = config["grenadeEntry"]
    rows: list[ExpectedRow] = []
    def dynamic(cat, item, key, raw):
        entry = ammo.get(item) if cat == "[3]" else None
        if entry is not None and key == "MinCount":
            return scale_positive_integer(entry["sourceMinCount"], ammo_multiplier)
        if entry is not None and key == "MaxCount":
            return scale_positive_integer(entry["sourceMaxCount"], ammo_multiplier)
        if cat == grenade["categoryNode"] and item == grenade["entryNode"]:
            if key == "Chance":
                return format_like_source(grenade_chance, grenade["sourceChanceRaw"])
            if key in {"MinCount", "MaxCount"}:
                return str(grenade_quantity)
        return raw
    sids = [config["customGeneratorSid"], *[x["targetSid"] for x in config["rankWrappers"]]]
    for sid in sids:
        append_template_rows(rows, config["file"], sid, config["templateCategories"], dynamic)
    rows.extend(load_redirect_rows(config["redirectManifestResource"], include_dlc1))
    return rows


def special_medium_stash_rows(parameter: dict, baseline: dict, value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    if value_matches_vanilla(parameter, value, difficulty):
        return []
    config = baseline["special"]["mediumStashAmmoProduction"]
    ammo = {(x["categoryNode"], x["entryNode"]): x for x in config["ammoEntries"]}
    rows: list[ExpectedRow] = []
    def dynamic(cat, item, key, raw):
        entry = ammo.get((cat, item))
        if entry is not None and key == "MinCount":
            return scale_positive_integer(entry["sourceMinCount"], value)
        if entry is not None and key == "MaxCount":
            return scale_positive_integer(entry["sourceMaxCount"], value)
        return raw
    sids = [config["customGeneratorSid"], *[x["targetSid"] for x in config["rankWrappers"]]]
    for sid in sids:
        append_template_rows(rows, config["file"], sid, config["templateCategories"], dynamic)
    rows.extend(load_redirect_rows(config["redirectManifestResource"], include_dlc1))
    return rows


def special_expensive_stash_rows(parameter: dict, baseline: dict, value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    if value_matches_vanilla(parameter, value, difficulty):
        return []
    config = baseline["special"]["expensiveStashProduction"]
    ammo = {(x["categoryNode"], x["entryNode"]): x for x in config["ammoEntries"]}
    rows: list[ExpectedRow] = []
    def dynamic(cat, item, key, raw):
        entry = ammo.get((cat, item))
        if entry is not None and key == "MinCount":
            return scale_positive_integer(entry["sourceMinCount"], value)
        if entry is not None and key == "MaxCount":
            return scale_positive_integer(entry["sourceMaxCount"], value)
        return raw
    sids = [config["customGeneratorSid"], *[x["targetSid"] for x in config["rankWrappers"]]]
    for sid in sids:
        append_template_rows(rows, config["file"], sid, config["templateCategories"], dynamic)
    rows.extend(load_redirect_rows(config["redirectManifestResource"], include_dlc1))
    return rows


def special_destructible_rows(parameter: dict, config: dict, value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    if value_matches_vanilla(parameter, value, difficulty):
        return []
    rows: list[ExpectedRow] = []
    for pool in config["pools"]:
        def dynamic(cat, item, key, raw, pool=pool):
            definition = next((x for x in pool["items"] if x["node"] == item), None)
            if definition is not None and key == "MinCount":
                return scale_positive_integer(definition["sourceMinCount"], value)
            if definition is not None and key == "MaxCount":
                return scale_positive_integer(definition["sourceMaxCount"], value)
            return raw
        categories = [{"node": pool["categoryNode"], "fields": pool["categoryFields"], "items": pool["items"]}]
        append_template_rows(rows, config["file"], pool["targetSid"], categories, dynamic)
    rows.extend(load_redirect_rows(config["redirectManifestResource"], include_dlc1))
    return rows


def special_expected_rows(catalog: dict, parameter: dict, parameters: dict, baseline: dict,
                          value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    pid = parameter["id"]
    special = baseline.get("special") or {}
    if pid in (special.get("psyParameterKeys") or {}):
        return special_psy_rows(catalog, parameter, parameters, baseline, value, difficulty)
    if pid in (special.get("mutantCooldownParameters") or {}):
        return special_mutant_rows(catalog, parameter, parameters, baseline, value, difficulty)
    npc = special.get("npcMedicalLootProduction") or {}
    if pid == npc.get("parameterId"):
        return special_npc_medical_rows(parameter, baseline, value, difficulty, include_dlc1)
    cheap = special.get("cheapStashProduction") or {}
    if pid in {cheap.get("ammoQuantityParameterId"), cheap.get("grenadeChanceParameterId"), cheap.get("grenadeQuantityParameterId")}:
        return special_cheap_stash_rows(parameter, baseline, value, difficulty, include_dlc1)
    medium = special.get("mediumStashAmmoProduction") or {}
    if pid == medium.get("parameterId"):
        return special_medium_stash_rows(parameter, baseline, value, difficulty, include_dlc1)
    expensive = special.get("expensiveStashProduction") or {}
    if pid == expensive.get("ammoQuantityParameterId"):
        return special_expensive_stash_rows(parameter, baseline, value, difficulty, include_dlc1)
    destr_ammo = special.get("destructibleAmmoProduction") or {}
    if pid == destr_ammo.get("parameterId"):
        return special_destructible_rows(parameter, destr_ammo, value, difficulty, include_dlc1)
    destr_grenade = special.get("destructibleGrenadeProduction") or {}
    if pid == destr_grenade.get("parameterId"):
        return special_destructible_rows(parameter, destr_grenade, value, difficulty, include_dlc1)
    raise ValueError(f"{pid} has no supported direct or special verification route")


def expected_rows_for_parameter(catalog: dict, parameter: dict, parameters: dict, baseline: dict,
                                value, difficulty: str, include_dlc1: bool) -> list[ExpectedRow]:
    point_ids = {p.get("parameterId") for p in baseline.get("editPoints", [])}
    if parameter["id"] in point_ids:
        return expected_rows(parameter, baseline, value, resolve_difficulty_sid(catalog, difficulty), include_dlc1)
    return special_expected_rows(catalog, parameter, parameters, baseline, value, difficulty, include_dlc1)



def _put_structure(store: dict[StructureTarget, ExpectedStructure], file: str, prototype: str,
                   node_path: tuple[str, ...], mode: str, ref_url: str = "", ref_key: str = ""):
    target = StructureTarget(file.replace("\\", "/"), prototype, tuple(node_path))
    structure = ExpectedStructure(target, mode, ref_url, ref_key)
    previous = store.get(target)
    if previous is not None and previous != structure:
        raise ValueError(
            f"conflicting structure contract for {target.display_path()}: "
            f"{previous.descriptor()} vs {structure.descriptor()}"
        )
    store[target] = structure


def _add_bpatch_chain(store: dict[StructureTarget, ExpectedStructure], target: Target):
    _put_structure(store, target.file, target.prototype, (), "bpatch")
    for i in range(1, len(target.node_path) + 1):
        _put_structure(store, target.file, target.prototype, target.node_path[:i], "bpatch")


def _generated_reference_config(parameter_id: str, baseline: dict) -> tuple[str, str]:
    special = baseline.get("special") or {}
    candidates = [
        special.get("npcMedicalLootProduction") or {},
        special.get("cheapStashProduction") or {},
        special.get("mediumStashAmmoProduction") or {},
        special.get("expensiveStashProduction") or {},
        special.get("destructibleAmmoProduction") or {},
        special.get("destructibleGrenadeProduction") or {},
    ]
    for config in candidates:
        ids = {
            config.get("parameterId"),
            config.get("ammoQuantityParameterId"),
            config.get("grenadeChanceParameterId"),
            config.get("grenadeQuantityParameterId"),
        }
        if parameter_id in ids:
            return config.get("baseTemplateRefUrl", ""), config.get("baseTemplateRefKey", "")
    return "", ""


def expected_structures_for_parameter(catalog: dict, parameter: dict, baseline: dict,
                                      rows: list[ExpectedRow], value, difficulty: str) -> list[ExpectedStructure]:
    if not rows:
        return []
    pid = parameter["id"]
    route = verification_route(pid, baseline)
    store: dict[StructureTarget, ExpectedStructure] = {}

    if route == "directEditPoints":
        for row in rows:
            if row.changed:
                _add_bpatch_chain(store, row.target)
        return sorted(store.values(), key=lambda s: (s.target.file, natural_key(s.target.prototype), s.target.node_path))

    if route in {"psyArray", "mutantCooldownArray"}:
        difficulty_sid = resolve_difficulty_sid(catalog, difficulty)
        full_tree = difficulty_sid.lower() != "stalker"
        for row in rows:
            target = row.target
            _put_structure(store, target.file, target.prototype, (), "bpatch")
            if target.node_path:
                _put_structure(store, target.file, target.prototype, target.node_path[:1], "bpatch")
            for i in range(2, len(target.node_path) + 1):
                _put_structure(
                    store, target.file, target.prototype, target.node_path[:i],
                    "plain" if full_tree else "bpatch"
                )
        return sorted(store.values(), key=lambda s: (s.target.file, natural_key(s.target.prototype), s.target.node_path))

    # Clone/redirect routes: generated TCD roots are reference-based and their
    # descendants are complete plain structs; redirects patch existing trees.
    ref_url, ref_key = _generated_reference_config(pid, baseline)
    for row in rows:
        target = row.target
        if row.operation == "redirect":
            _add_bpatch_chain(store, target)
            continue
        _put_structure(store, target.file, target.prototype, (), "ref", ref_url, ref_key)
        for i in range(1, len(target.node_path) + 1):
            _put_structure(store, target.file, target.prototype, target.node_path[:i], "plain")
    return sorted(store.values(), key=lambda s: (s.target.file, natural_key(s.target.prototype), s.target.node_path))


def verification_route(parameter_id: str, baseline: dict) -> str:
    if any(p.get("parameterId") == parameter_id for p in baseline.get("editPoints", [])):
        return "directEditPoints"
    special = baseline.get("special") or {}
    if parameter_id in (special.get("psyParameterKeys") or {}):
        return "psyArray"
    if parameter_id in (special.get("mutantCooldownParameters") or {}):
        return "mutantCooldownArray"
    npc = special.get("npcMedicalLootProduction") or {}
    if parameter_id == npc.get("parameterId"):
        return "npcMedicalCloneRedirect"
    cheap = special.get("cheapStashProduction") or {}
    if parameter_id in {cheap.get("ammoQuantityParameterId"), cheap.get("grenadeChanceParameterId"), cheap.get("grenadeQuantityParameterId")}:
        return "cheapStashCloneRedirect"
    medium = special.get("mediumStashAmmoProduction") or {}
    if parameter_id == medium.get("parameterId"):
        return "mediumStashCloneRedirect"
    expensive = special.get("expensiveStashProduction") or {}
    if parameter_id == expensive.get("ammoQuantityParameterId"):
        return "expensiveStashCloneRedirect"
    for key, route in (("destructibleAmmoProduction", "destructibleAmmoCloneRedirect"),
                       ("destructibleGrenadeProduction", "destructibleGrenadeCloneRedirect")):
        config = special.get(key) or {}
        if parameter_id == config.get("parameterId"):
            return route
    return "unsupported"


def ensure_verifiable_parameter(parameter_id: str, parameters: dict, baseline: dict):
    if parameter_id not in parameters:
        raise ValueError(f"unknown parameter ID: {parameter_id}")
    route = verification_route(parameter_id, baseline)
    if route == "unsupported":
        raise ValueError(f"{parameter_id} has no supported verifier route")
    return parameters[parameter_id]


def runtime_pending_parameter_ids(catalog: dict) -> list[str]:
    return [
        p["id"] for p in catalog.get("parameters", [])
        if p.get("exposeInUi", True)
        and (p.get("support") or {}).get("status") == "runtime_test_required"
    ]


def ensure_direct_parameter(parameter_id: str, parameters: dict, baseline: dict):
    if parameter_id not in parameters:
        raise ValueError(f"unknown parameter ID: {parameter_id}")
    parameter = parameters[parameter_id]
    point_ids = {p.get("parameterId") for p in baseline.get("editPoints", [])}
    if parameter_id not in point_ids:
        raise ValueError(f"{parameter_id} is not an ordinary editPoint parameter; special writer verification is not implemented yet")
    return parameter


def run_plan(args, catalog, parameters, baseline):
    parameter = ensure_verifiable_parameter(args.parameter, parameters, baseline)
    value = args.value if args.value is not None else resolve_vanilla(parameter, args.difficulty)
    rows = expected_rows_for_parameter(catalog, parameter, parameters, baseline, value, args.difficulty, args.dlc1)
    structures = expected_structures_for_parameter(catalog, parameter, baseline, rows, value, args.difficulty)
    summary = write_case_evidence(args.output.resolve(), parameter, value, args.difficulty, rows, structures)
    summary["verificationRoute"] = verification_route(parameter["id"], baseline)
    (args.output.resolve() / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(f"[{summary['status']}] {parameter['id']}: {summary['expectedChangedEditPoints']} expected assignments")
    print(f"[OK] route: {summary['verificationRoute']}")
    print(f"[OK] evidence: {args.output.resolve()}")


def run_compare(args, catalog, parameters, baseline):
    parameter = ensure_verifiable_parameter(args.parameter, parameters, baseline)
    value = args.value if args.value is not None else resolve_vanilla(parameter, args.difficulty)
    rows = expected_rows_for_parameter(catalog, parameter, parameters, baseline, value, args.difficulty, args.dlc1)
    structures = expected_structures_for_parameter(catalog, parameter, baseline, rows, value, args.difficulty)
    actual, actual_structures = discover_actual_contract(args.override_root.resolve())
    summary = write_case_evidence(
        args.output.resolve(), parameter, value, args.difficulty, rows, structures,
        actual=actual, actual_structures=actual_structures,
    )
    summary["verificationRoute"] = verification_route(parameter["id"], baseline)
    (args.output.resolve() / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(f"[{summary['status']}] {parameter['id']}: expected {summary['expectedChangedEditPoints']} assignments, actual {len(actual)} assignments")
    if summary["comparison"]:
        c = summary["comparison"]
        print(
            f"[INFO] missing={c['missing']} unexpected={c['unexpected']} wrongValue={c['wrongValue']} "
            f"missingStruct={c['missingStructure']} unexpectedStruct={c['unexpectedStructure']} wrongStruct={c['wrongStructure']}"
        )
    print(f"[OK] route: {summary['verificationRoute']}")
    print(f"[OK] evidence: {args.output.resolve()}")
    if summary["status"] != "PASS":
        raise SystemExit(1)


def suite_case_values(parameter: dict, difficulty: str):
    control = parameter.get("control")
    if control == "slider":
        slider = parameter.get("slider") or {}
        if not isinstance(slider.get("min"), (int, float)) or not isinstance(slider.get("max"), (int, float)):
            raise ValueError(f"suite requires numeric slider min/max: {parameter['id']}")
        return [
            ("MIN", slider["min"]),
            ("VANILLA", resolve_vanilla(parameter, difficulty)),
            ("MAX", slider["max"]),
        ]
    if control == "toggle":
        return [
            ("OFF", False),
            ("VANILLA", resolve_vanilla(parameter, difficulty)),
            ("ON", True),
        ]
    raise ValueError(f"suite supports slider/toggle controls only: {parameter['id']} ({control})")


def run_suite(args, catalog, parameters, baseline):
    parameter_ids = args.parameters or runtime_pending_parameter_ids(catalog)
    out = args.output.resolve()
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)
    summaries = []
    for pid in parameter_ids:
        parameter = ensure_verifiable_parameter(pid, parameters, baseline)
        route = verification_route(pid, baseline)
        for variant, value in suite_case_values(parameter, args.difficulty):
            rows = expected_rows_for_parameter(catalog, parameter, parameters, baseline, value, args.difficulty, args.dlc1)
            structures = expected_structures_for_parameter(catalog, parameter, baseline, rows, value, args.difficulty)
            case_dir = out / pid.replace(".", "_") / variant.lower()
            summary = write_case_evidence(case_dir, parameter, value, args.difficulty, rows, structures)
            summary["variant"] = variant
            summary["verificationRoute"] = route
            (case_dir / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
            summaries.append(summary)
            print(f"[{summary['status']}] {pid}/{variant}: {summary['expectedChangedEditPoints']} expected assignments ({route})")

    with (out / "suite-summary.csv").open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(["parameterId", "route", "variant", "inputValue", "contractRows", "expectedAssignments", "nonAssignments", "expectedStructures", "status"])
        for summary in summaries:
            w.writerow([
                summary["parameterId"], summary["verificationRoute"], summary["variant"], summary["inputValue"],
                summary["totalBaselineEditPoints"], summary["expectedChangedEditPoints"],
                summary["expectedUnchangedEditPoints"], summary["expectedStructures"], summary["status"],
            ])

    md = [
        "# Runtime-pending override-verification suite",
        "",
        f"This suite covers **{len(parameter_ids)}** selected controls at their isolated edge/default cases for **{args.difficulty}**.",
        "Sliders use **MIN / VANILLA / MAX**; toggles use **OFF / VANILLA / ON**.",
        "`CONTRACT_PASS` proves deterministic baseline/special-route transformation only; actual C# writer output is verified later with `compare`.",
        "",
        "| Parameter | Route | Variant | Input | Contract rows | Expected assignments | Struct nodes | Status |",
        "|---|---|---|---:|---:|---:|---:|---|",
    ]
    for summary in summaries:
        md.append(
            f"| `{summary['parameterId']}` | {summary['verificationRoute']} | {summary['variant']} | {summary['inputValue']} | "
            f"{summary['totalBaselineEditPoints']} | {summary['expectedChangedEditPoints']} | {summary['expectedStructures']} | {summary['status']} |"
        )
    md += ["", "Each case contains `diff.csv`, `summary.json`, `summary.md` and, when assignments are expected, an `expected-patch/` reference tree.", ""]
    (out / "README.md").write_text("\n".join(md), encoding="utf-8")
    print(f"[OK] suite evidence: {out}")


def build_parser():
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest="command", required=True)

    def common(sp):
        sp.add_argument("--parameter", required=True)
        sp.add_argument("--value", type=float, help="player-facing numeric value; defaults to vanilla")
        sp.add_argument("--difficulty", default="Master", help="difficulty SID/key for difficulty-specific edit points/vanilla")
        sp.add_argument("--dlc1", action=argparse.BooleanOptionalAction, default=True, help="include DLC1 edit points (default: true)")
        sp.add_argument("--output", type=Path, required=True)

    plan = sub.add_parser("plan", help="emit expected original->override evidence and reference BPatch")
    common(plan)

    compare = sub.add_parser("compare", help="compare a real single-control writer staging tree with expected assignments")
    common(compare)
    compare.add_argument("--override-root", type=Path, required=True, help="production writer staging root")

    suite = sub.add_parser("suite", help="run MIN/VANILLA/MAX contract cases")
    suite.add_argument("--parameters", nargs="*", help="parameter IDs; defaults to all visible runtime_test_required controls")
    suite.add_argument("--difficulty", default="Master")
    suite.add_argument("--dlc1", action=argparse.BooleanOptionalAction, default=True)
    suite.add_argument("--output", type=Path, required=True)
    return p


def main():
    catalog, baseline, parameters = load_model()
    args = build_parser().parse_args()
    try:
        if args.command == "plan":
            run_plan(args, catalog, parameters, baseline)
        elif args.command == "compare":
            run_compare(args, catalog, parameters, baseline)
        else:
            run_suite(args, catalog, parameters, baseline)
    except (ValueError, KeyError) as exc:
        print(f"[FAIL] {exc}", file=sys.stderr)
        raise SystemExit(2)


if __name__ == "__main__":
    main()
