#!/usr/bin/env python3
"""Guard WPF aggregate controls against ordinary-toggle visibility precedence.

Some aggregate boards are anchored on toggles (Severe Fatigue and World Loot).
An unguarded IsToggle DataTrigger can collapse ControlRow and hide the board.
No WPF runtime is needed for this static regression test.
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
MAIN_VM = ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs"
AGGREGATE = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "AggregateVisualControl.cs"
REMAINING = ROOT / "docs" / "UI-REMAINING-REDESIGN.md"

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
    # An aggregate can be anchored on choiceSlider (NPC corpse bonus) or range.
    # Its native editing controls must not stay visible beneath the board.
    for native in ("ChoiceSliderControl", "RangeControl"):
        if not has_setter(aggregate[0], native, "Collapsed"):
            fail(f"aggregate trigger does not hide native {native}")

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

    # A previous Loot UI pass accidentally wrote C# help arms ending in \\",
    # escaping the closing quote. That makes the next source line part of the
    # string and produces CS1010/CS1003 before WPF can even compile.
    for family in (
        "NpcLootLoadout", "WorldLootLayout", "StashRewardProfile", "BreakableBoxYields",
        "BlastDamageBoard", "PhysicalLimits", "TraderStockBoard", "ServiceCostsBoard",
        "ItemWeightBoard", "AnomalyEffectsBoard",
    ):
        prefix = f'"{family}" => "'
        arm = next(
            (line.strip() for line in vm.splitlines() if line.strip().startswith(prefix)),
            None,
        )
        if arm is None:
            fail(f"missing control-help arm: {family}")
        if not arm.endswith('",') or arm.endswith(chr(92) + '",'):
            fail(f"malformed or escaped C# control-help closing quote for {family}")
    toggle_families = [
        (
            "SevereFatiguePenaltyBoard",
            "advanced.fatigueMechanics",
            [
                "advanced.fatigueMechanics",
                "survival.severeFatigueMaxStamina",
                "survival.severeFatigueCarryWeight",
                "survival.severeFatigueRecoil",
                "survival.severeFatigueAimSway",
                "survival.severeFatigueDispersion",
            ],
        ),
        (
            "WorldLootLayout",
            "loot.worldWeaponShuffle",
            [
                "loot.worldWeaponShuffle",
                "loot.worldWeaponDensity",
                "loot.worldAttachmentShuffle",
                "loot.worldDetectors",
                "loot.blueprintShuffle",
            ],
        ),
    ]
    for family, anchor, members in toggle_families:
        if by_id.get(anchor, {}).get("control") != "toggle":
            fail(f"{family} no longer has the expected toggle anchor")
        if f'["{family}"] = "{anchor}"' not in vm:
            fail(f"missing aggregate anchor for {family}")
        for member in members:
            if member not in by_id:
                fail(f"missing catalog member {member}")
            if f'["{member}"] = "{family}"' not in vm:
                fail(f"missing family mapping for {member}")

    npc_family = "NpcLootLoadout"
    npc_members = {
        "loot.npcCorpseBonus": "choiceSlider",
        "loot.npcMedicalChance": "slider",
        "loot.npcWeaponAmmoSupply": "slider",
        "loot.droppedWeaponCondition": "range",
    }
    if f'["{npc_family}"] = "loot.npcCorpseBonus"' not in vm:
        fail("NPC loot aggregate has no discrete-choice anchor")
    for member, control in npc_members.items():
        if by_id.get(member, {}).get("control") != control:
            fail(f"NPC native control type changed for {member}")
        if f'["{member}"] = "{npc_family}"' not in vm:
            fail(f"NPC aggregate mapping missing {member}")

    # Regression guard for the remaining-slider consolidation. Membership must
    # agree across VM visibility, aggregate rendering/measurement and modified-state
    # wiring. This is intentionally source-only and runs on Linux before publish.
    orphan_families = {
        "BlastDamageBoard": ("combat.explosionDamage", [
            "combat.explosionDamage",
        ]),
        "CoreHealthDashboard": ("health.maxHP", [
            "health.maxHP", "health.baseRegen", "health.regenDelay",
            "health.regenerationScalar", "health.lowHealthRegen",
        ]),
        "PhysicalLimits": ("stamina.vaultMaxHeight", [
            "stamina.vaultMaxHeight", "stamina.maxCarryWeight",
        ]),
        "TraderStockBoard": ("economy.traderAmmoStock", [
            "economy.traderAmmoStock", "economy.medicStock",
        ]),
        "ServiceCostsBoard": ("economy.fastTravelCost", [
            "economy.fastTravelCost", "economy.upgradeCost",
            "economy.repairCostBase",
        ]),
        "ItemWeightBoard": ("equipment.ammoWeight", [
            "equipment.ammoWeight", "equipment.consumableWeight",
        ]),
        "AnomalyEffectsBoard": ("environment.anomalyDamage", [
            "environment.anomalyDamage", "environment.flycatcherMovementSpeed",
            "environment.flycatcherTurnSpeed", "environment.razorBleeding",
            "environment.diamondBleeding", "environment.psyAnomalyIntensity",
        ]),
    }
    main_vm = MAIN_VM.read_text(encoding="utf-8")
    visual = AGGREGATE.read_text(encoding="utf-8")
    for family, (anchor_id, members) in orphan_families.items():
        if f'["{family}"] = "{anchor_id}"' not in vm:
            fail(f"visual family {family} lacks its visible anchor")
        if f'case "{family}":' not in visual:
            fail(f"visual family {family} lacks OnRender dispatch")
        ids_match = re.search(
            rf'"{re.escape(family)}"\s*=>\s*new\[\]\s*\{{(.*?)\}}',
            visual, re.S,
        )
        if ids_match is None:
            fail(f"visual family {family} lacks source member binding")
        wire = re.search(
            rf'WireVisualAggregate\(\s*"{re.escape(anchor_id)}",(?P<rest>.*?)\);',
            main_vm, re.S,
        )
        if wire is None:
            fail(f"visual family {family} has no aggregate modified-state wiring")
        for param_id in members:
            definition = by_id.get(param_id)
            if definition is None or definition.get("control") != "slider":
                fail(f"native catalog slider changed or missing: {param_id}")
            if definition.get("tab") != by_id[anchor_id].get("tab"):
                fail(f"family crosses tabs: {family} / {param_id}")
            if f'["{param_id}"] = "{family}"' not in vm:
                fail(f"VM family map missing: {param_id}")
            if f'"{param_id}"' not in ids_match.group(1):
                fail(f"renderer GetFamilyIds missing: {param_id}")
            if f'"{param_id}"' not in wire.group("rest"):
                fail(f"modified-state listener missing: {param_id}")

    if "foreach (var marker in vault.VanillaMarkers)" not in visual or (
        "AddHorizontalTarget(vault," not in visual
    ):
        fail("vault height source markers lost their click-to-snap editor")

    remaining_text = REMAINING.read_text(encoding="utf-8")
    remaining_ids = re.findall(r"^- `([a-z][a-zA-Z0-9.]*)`", remaining_text, re.M)
    if len(remaining_ids) != 4 or "Default-control entries remaining: **4**" not in remaining_text:
        fail("remaining standalone-control audit is stale")
    for _family, (_anchor, members) in orphan_families.items():
        if set(members) & set(remaining_ids):
            fail(f"audited family still appears as standalone: {_family}")

    print("[PASS] WPF aggregate anchors and all orphan-control family bindings are valid")


if __name__ == "__main__":
    main()
