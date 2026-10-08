#!/usr/bin/env python3
"""Static contract for the six-channel Anomalies mixer prototype.

Does not run WPF. Run alongside .NET publish and verify appearance under Wine.
"""

from __future__ import annotations

import json
import math
import re
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TrueCustomDifficulty"
CONTROL = SRC / "Controls" / "AggregateVisualControl.cs"
MAIN = SRC / "MainWindow.xaml.cs"
XAML = SRC / "MainWindow.xaml"
VIEWMODEL = SRC / "ViewModels" / "ParameterViewModel.cs"
CATALOG = SRC / "Data" / "parameters.json"
SPEC = ROOT / "docs" / "UI-MIXER-GRID-SPEC.md"

EXPECTED = (
    ("environment.anomalyDamage", 0.0, 2.5, 0.01, None),
    ("environment.flycatcherMovementSpeed", 0.0, 1.0, 0.01, 0.5),
    ("environment.flycatcherTurnSpeed", 0.0, 1.0, 0.01, 0.3),
    ("environment.razorBleeding", 0.0, 2.0, 0.01, 1.0),
    ("environment.diamondBleeding", 0.0, 20.0, 0.5, 10.0),
    ("environment.psyAnomalyIntensity", 0.0, 2.0, 0.01, 1.0),
)


def fail(message: str) -> None:
    raise SystemExit(f"[FAIL] Anomaly mixer: {message}")


def require(value: bool, message: str) -> None:
    if not value:
        fail(message)


def columns_for_width(board_width: float) -> int:
    inner = max(0.0, board_width - 32.0)
    count = (
        6 if inner >= 852.0 else
        4 if inner >= 596.0 else
        3 if inner >= 420.0 else
        2 if inner >= 272.0 else 1
    )
    while count > 1 and (inner - (count - 1) * 10.0) / count < 128.0:
        count -= 1
    return count


def check_geometry() -> None:
    # These widths cross each documented strip threshold, including the
    # normal centered 920-DIP board and minimum-width editor cases.
    for width in (920.0, 885.0, 884.0, 820.0, 628.0,
                  625.0, 520.0, 470.0, 452.0, 304.0, 300.0):
        count = columns_for_width(width)
        inner = width - 32.0
        lane = (inner - (count - 1) * 10.0) / count
        require(count > 0 and lane >= 128.0,
                f"too-narrow strip {lane:.1f} at width {width}")
        require(count * lane + (count - 1) * 10.0 <= inner + 1e-7,
                f"horizontal overflow at width {width}")
        rows = math.ceil(len(EXPECTED) / count)
        height = 56.0 + rows * 280.0 + (rows - 1) * 12.0 + 30.0
        last_bottom = 56.0 + (rows - 1) * 292.0 + 280.0
        require(height - last_bottom >= 30.0,
                f"footer overlaps last strip at width {width}")

    require(columns_for_width(920) == 6, "920-DIP board must fit six strips")
    require(columns_for_width(800) == 4, "800-DIP board must use four columns")
    require(columns_for_width(520) == 3, "520-DIP board must use three columns")
    require(columns_for_width(400) == 2, "400-DIP board must use two columns")


def main() -> None:
    visual = CONTROL.read_text(encoding="utf-8")
    vm = VIEWMODEL.read_text(encoding="utf-8")
    win = MAIN.read_text(encoding="utf-8")
    xml = XAML.read_text(encoding="utf-8")
    spec = SPEC.read_text(encoding="utf-8")
    by_id = {p["id"]: p for p in json.loads(CATALOG.read_text(encoding="utf-8"))["parameters"]}

    channels = re.search(
        r"private static readonly .*? AnomalyMixerChannels\s*=\s*\{(.*?)\};",
        visual, re.S,
    )
    require(channels is not None, "source-ordered mixer channel array is absent")
    actual = re.findall(r'\("(environment\.[a-zA-Z]+)",\s*"[^"]+",\s*"[^"]+"\)',
                        channels.group(1))
    require(tuple(actual) == tuple(x[0] for x in EXPECTED),
            f"channel order or ID mismatch: {actual}")

    for parameter_id, minimum, maximum, step, reference in EXPECTED:
        p = by_id.get(parameter_id)
        require(p is not None and p.get("control") == "slider",
                f"catalog source missing/not slider: {parameter_id}")
        slider = p["slider"]
        require((slider["min"], slider["max"], slider["step"])
                == (minimum, maximum, step),
                f"source range/step drift for {parameter_id}")
        require(slider.get("stepMode") == ("absolute" if step == 0.5 else None),
                f"source stepMode drift for {parameter_id}")
        if reference is not None:
            require(p["vanilla"] == {"All": reference},
                    f"reference drift for {parameter_id}")
        require(parameter_id in spec, f"pilot spec missing {parameter_id}")

    global_damage = by_id["environment.anomalyDamage"]["vanilla"]
    require(global_damage == {
        "Novice": 0.3, "Stalker": 1,
        "Veteran": 1.45, "Master": 1.75,
    }, "global-damage difficulty references changed")

    for fragment in (
        '"AnomalyEffectsBoard" => GetMixerHeight(width, AnomalyMixerChannels.Length)',
        "var columns = GetMixerColumns(ActualWidth, count);",
        "DrawMixerStrip(dc, P(source.Id)",
        "var currentY = ValueToY(parameter.CurrentNumber,",
        "var baseY = ValueToY(parameter.ReferenceNumber,",
        "AddVerticalTarget(parameter,",
        "HitKind.MixerBaseMarker",
        "target.Parameter.ResetToReference();",
        "parameter?.IsValueModifiedFromReference() == true",
        'Family == "AnomalyEffectsBoard"',
        "ParameterSelected?.Invoke(this,",
    ):
        require(fragment in visual, f"renderer feature missing: {fragment}")

    require("DrawHorizontalTrackCard(dc, P(item.Id)" not in visual,
            "old six horizontal tracks remain in the Anomalies renderer")
    require('ParameterSelected="AggregateVisualControl_ParameterSelected"' in xml,
            "rationale event missing in MainWindow XAML")
    require("main.SelectedTab.SelectedParameter = e.Parameter;" in win,
            "rationale event does not select fader source")
    require("main.SelectedTab.Parameters.Contains(e.Parameter)" in win,
            "rationale event does not limit selection to its tab")
    require('"AnomalyEffectsBoard" => "Six vertical faders' in vm,
            "anomaly rationale still describes old horizontal controls")

    ET.parse(XAML)
    check_geometry()
    print("[PASS] Six source-exact anomaly faders, Base/selection, layout and XAML guards")


if __name__ == "__main__":
    main()
