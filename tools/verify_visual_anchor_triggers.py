#!/usr/bin/env python3
"""Guard WPF aggregate controls against ordinary-toggle visibility precedence.

The Master Severe Fatigue aggregate is anchored on a toggle. A plain IsToggle
DataTrigger later in DataTemplate.Triggers can collapse ControlRow and hide the
whole graph, leaving only a checkbox. No WPF runtime is needed for this test.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml"
VM = ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs"
CATALOG = ROOT / "src" / "TrueCustomDifficulty" / "Data" / "parameters.json"

WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
X = "{http://schemas.microsoft.com/winfx/2006/xaml}"


def fail(reason: str) -> None:
    raise SystemExit(f"[FAIL] WPF aggregate toggle regression: {reason}")


def has_setter(trigger: ET.Element, target: str, value: str) -> bool:
    return any(
        node.attrib.get("TargetName") == target
        and node.attrib.get("Property") == "Visibility"
        and node.attrib.get("Value") == value
        for node in trigger.findall(f"{WPF}Setter")
    )


def main() -> None:
    root = ET.parse(XAML).getroot()
    template = next(
        (
            node for node in root.iter(f"{WPF}DataTemplate")
            if node.attrib.get(f"{X}Key") == "ParameterCardTemplate"
        ),
        None,
    )
    if template is None:
        fail("ParameterCardTemplate is missing")

    triggers = template.find(f"{WPF}DataTemplate.Triggers")
    if triggers is None:
        fail("ParameterCardTemplate has no triggers")

    aggregate = [
        t for t in triggers.findall(f"{WPF}DataTrigger")
        if t.attrib.get("Binding") == "{Binding IsVisualAggregateAnchor}"
        and t.attrib.get("Value") == "True"
    ]
    if len(aggregate) != 1 or not has_setter(
        aggregate[0], "AggregateVisualControl", "Visible"
    ):
        fail("aggregate trigger does not expose AggregateVisualControl")
    if has_setter(aggregate[0], "ControlRow", "Collapsed"):
        fail("aggregate trigger collapses its own ControlRow")

    guarded = []
    for trigger in triggers:
        if trigger.tag == f"{WPF}DataTrigger" and (
            trigger.attrib.get("Binding") == "{Binding IsToggle}"
            and trigger.attrib.get("Value") == "True"
        ):
            if has_setter(trigger, "ControlRow", "Collapsed"):
                fail("unguarded ordinary IsToggle trigger collapses ControlRow")

        if trigger.tag != f"{WPF}MultiDataTrigger":
            continue
        conditions = trigger.find(f"{WPF}MultiDataTrigger.Conditions")
        if conditions is None:
            continue
        actual = {
            (c.attrib.get("Binding"), c.attrib.get("Value"))
            for c in conditions.findall(f"{WPF}Condition")
        }
        expected = {
            ("{Binding IsToggle}", "True"),
            ("{Binding IsVisualAggregateAnchor}", "False"),
        }
        if expected.issubset(actual):
            guarded.append(trigger)

    if len(guarded) != 1:
        fail("expected exactly one ordinary toggle trigger gated away from aggregates")
    for target, value in [
        ("CurrentValueBox", "Collapsed"),
        ("ToggleControl", "Visible"),
        ("ControlRow", "Collapsed"),
    ]:
        if not has_setter(guarded[0], target, value):
            fail(f"guarded toggle rule missing {target} -> {value}")

    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    by_id = {p["id"]: p for p in catalog["parameters"]}
    vm = VM.read_text(encoding="utf-8")
    family = "SevereFatiguePenaltyBoard"
    anchor = "advanced.fatigueMechanics"
    members = [
        anchor,
        "survival.severeFatigueMaxStamina",
        "survival.severeFatigueCarryWeight",
        "survival.severeFatigueRecoil",
        "survival.severeFatigueAimSway",
        "survival.severeFatigueDispersion",
    ]
    if by_id.get(anchor, {}).get("control") != "toggle":
        fail("the Severe Fatigue aggregate's toggle anchor changed")
    if f'["{family}"] = "{anchor}"' not in vm:
        fail("the Severe Fatigue anchor map is missing")
    for member in members:
        if member not in by_id:
            fail(f"missing catalog backing member {member}")
        mapping = f'["{member}"] = "{family}"'
        if mapping not in vm:
            fail(f"missing family mapping {member}")

    print("[PASS] Master Severe Fatigue aggregate stays visible; ordinary toggles stay ordinary")


if __name__ == "__main__":
    main()
