#!/usr/bin/env python3
"""Starvation Master ON/OFF UI dependency source gate (no .NET/Wine required).

This catches the visual-aggregate anchor bypass that previously left the
EXTRA STAMINA COST track active while STARVATION MECHANICS was OFF.
Run with: python3 tools/audit_master_dependency_ui.py
"""
from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src" / "TrueCustomDifficulty"
CATALOG = json.loads((APP / "Data" / "parameters.json").read_text(encoding="utf-8"))
VM = (APP / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
RENDER = (APP / "Controls" / "AggregateVisualControl.cs").read_text(encoding="utf-8")
HARNESS = (ROOT / "tests" / "SurvivalPatchContract" / "Program.cs").read_text(encoding="utf-8")


def check(ok: bool, label: str) -> None:
    if not ok:
        raise ValueError(label)


def main() -> None:
    controls = {p["id"]: p for p in CATALOG["parameters"]}
    parent = "advanced.starvationMechanics"
    anchor = "survival.starvationStaminaDrain"
    children = [p["id"] for p in controls.values() if p.get("requiresToggle") == parent]
    check(len(children) == 5 and anchor in children, "Starvation must have five dependency-gated child sliders")
    check(controls[parent]["control"] == "toggle", "Starvation parent is no longer a toggle")
    check('"StarvationTimeline"] = "survival.starvationStaminaDrain"' in VM,
          "The dependent stamina slider is no longer the visual anchor")

    enabled = re.search(r"public bool IsControlEnabled\s*=>\s*([^;]+);", VM)
    check(enabled is not None, "Missing IsControlEnabled binding")
    check(enabled.group(1).strip() == "_dependencyEnabledProvider?.Invoke() ?? true",
          "Aggregate anchor bypasses its requiresToggle dependency")
    visible = VM[VM.index("public bool IsControlVisible"):VM.index("public string ToggleStateLabel")]
    check("IsVisualAggregateAnchor || (_dependencyEnabledProvider?.Invoke() ?? true)" in visible,
          "Disabled Master child anchor must remain visible as the composite panel")

    card = RENDER[RENDER.index("private void DrawMasterNativeCard"):RENDER.index("private void DrawMasterCardBank")]
    check("var enabled = parameter.IsControlEnabled;" in card
          and "var changed = enabled && parameter.IsValueModifiedFromReference();" in card,
          "Master card ignores dependency state for accent and labels")
    check("if (enabled)" in card, "Master slider accent is not gated")
    check("if (!parameter.IsControlEnabled)" in RENDER[RENDER.index("private void AddHorizontalTarget"):
                                                   RENDER.index("private void AddVerticalTarget")],
          "Master slider hit target remains active while OFF")

    keyboard = RENDER[RENDER.index("protected override void OnKeyDown"):
                      RENDER.index("private void UpdateTarget")]
    check("if (_selectedParameter is null || !_selectedParameter.IsControlEnabled)" in keyboard,
          "A stale selected Master slider remains keyboard-editable while OFF")
    drag = RENDER[RENDER.index("private void UpdateTarget"):RENDER.index("private void EnforceOrderedTriplet")]
    check("if (!target.Parameter.IsControlEnabled || target.Kind == HitKind.Toggle)" in drag,
          "A captured drag can still edit a disabled Master slider")

    check('TestDisabled(id);' in HARNESS
          and HARNESS.count('("advanced.starvationMechanics", "survival.starvationStaminaDrain")') == 1,
          "Missing Starvation anchor ON/OFF writer regression")
    check('"survival.starvationStaminaDrain",\n' in HARNESS,
          "Missing Veteran OFF starvation-anchor output regression")

    # UI-design lock: 3 actual seconds/HP knobs over the passive timeline,
    # and the remaining 2 editable native tracks together below it.
    knob_block = RENDER[RENDER.index("var knobs = new (string Id, string Label, string Unit)[]",
                                   RENDER.index("private void DrawStarvationTimeline")):
                        RENDER.index("var previewTop = StarvationPreviewTop;")]
    for parameter_id in (
        "survival.starvationPenaltyDelay",
        "survival.starvationDamage",
        "survival.starvationDamageInterval",
    ):
        check(parameter_id in knob_block, f"Missing top starvation knob: {parameter_id}")
    check("DrawSourceKnob(dc, P(knobs[i].Id)" in knob_block
          and "rawReadout: true" in knob_block
          and "positivePrefix: false" in knob_block,
          "Starvation knobs no longer use independent source-native numeric dials")
    check('prominent: knobs[i].Id == "survival.starvationDamage"' in knob_block,
          "Only the central HP DAMAGE PER TICK dial may use hero prominence")
    check('prominent: channels[i].Id == "combat.playerHeadshotMultiplier"' in RENDER,
          "The original Headshot Damage hero knob must remain prominent")
    check("WeaponKnobRadius * (prominent ? 1.18 : 1.0)" in RENDER,
          "Hero knob enlargement must keep the existing radius/cell clamping")
    check("var previewTop = StarvationPreviewTop;" in RENDER
          and "StarvationPreviewTop + StarvationPreviewHeight + 12.0;" in RENDER,
          "Timeline layout no longer shares measured vertical offsets")
    effect_block = RENDER[RENDER.index("var effects = new (string Id, string Label, Func<double, string> Format)[]",
                                     RENDER.index("private void DrawStarvationTimeline")):
                          RENDER.index("private void DrawRepeatedHitBand")]
    check(effect_block.count('("survival.starvation') == 2
          and '"survival.starvationStaminaDrain"' in effect_block
          and '"survival.starvationHealthRegenPenalty"' in effect_block
          and '"survival.starvationDamage"' not in effect_block,
          "Two distinct lower Starvation sliders must not duplicate HP tick damage")
    check("StarvationEffectsTop, effectWidth, 80.0" in effect_block
          and "for (var i = 0; i < effects.Length; i++)" in effect_block,
          "The lower two slider cards must share one compact horizontal row")
    check('or "StarvationTimeline"))' in RENDER[RENDER.index("protected override void OnMouseWheel"):
                                                 RENDER.index("protected override void OnMouseLeftButtonUp")],
          "Starvation source knobs must respond to native-step mouse wheel input")

    print("PASS: Starvation Master toggle disables all 5 sliders, preserves visible panel,")
    print("      gates accent and mouse/keyboard input; .NET writer regression cases present")
    print("NOT VERIFIED: Windows .NET build, Wine visual acceptance, Unreal runtime")


if __name__ == "__main__":
    try:
        main()
    except (OSError, KeyError, ValueError) as error:
        print("FAIL:", error, file=sys.stderr)
        sys.exit(1)
