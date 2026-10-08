#!/usr/bin/env python3
"""Static 16:9 window geometry regression guard (no .NET/Wine needed)."""

from __future__ import annotations

import math
from pathlib import Path
from xml.etree import ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
XAML_PATH = ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml"
CODE_PATH = ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml.cs"
EXPECTED_RATIO = 16.0 / 9.0


def fail(message: str) -> None:
    raise SystemExit(f"[FAIL] Window aspect: {message}")


def check_ratio(width: float, height: float, label: str) -> None:
    if height <= 0.0 or not math.isclose(
        width / height, EXPECTED_RATIO, rel_tol=1e-9, abs_tol=1e-9
    ):
        fail(f"{label} is {width:g}x{height:g}, not 16:9")


def startup_geometry(
    width: float, min_width: float, min_height: float, work_width: float, work_height: float
) -> tuple[float, float]:
    """Python mirror of the WPF-DIP fit rule for representative test screens."""
    available_width = max(min_width, work_width * 0.94)
    available_height = max(min_height, work_height * 0.94)
    fitted_width = max(min_width, min(width, available_width, available_height * EXPECTED_RATIO))
    return fitted_width, fitted_width / EXPECTED_RATIO


def main() -> None:
    window = ET.parse(XAML_PATH).getroot()
    width = float(window.attrib["Width"])
    height = float(window.attrib["Height"])
    min_width = float(window.attrib["MinWidth"])
    min_height = float(window.attrib["MinHeight"])

    check_ratio(width, height, "default window")
    check_ratio(min_width, min_height, "minimum window")
    if min_width > width or min_height > height:
        fail("minimum exceeds default window size")
    if window.attrib.get("WindowStartupLocation") != "Manual":
        fail("startup coordinates must be centered after work-area fitting")
    if window.attrib.get("ResizeMode") != "CanResize":
        fail("manual 16:9 resize was disabled")

    code = CODE_PATH.read_text(encoding="utf-8")
    for expected in (
        "private const double AspectRatio = 16.0 / 9.0;",
        "private const double StartupWorkAreaFraction = 0.94;",
        "FitInitialWindowToWorkArea();",
        "private void FitInitialWindowToWorkArea()",
        "private void ToggleAspectMaximize()",
        "msg != WmSizing",
        "height = width / AspectRatio;",
        "width = height * AspectRatio;",
    ):
        if expected not in code:
            fail(f"missing native aspect constraint: {expected}")

    # A smaller monitor must shrink the startup window without losing 16:9.
    # On unusual displays smaller than the app's minimum size, the WPF minimum
    # understandably takes priority; ordinary desktop work areas fit.
    for work_width, work_height in ((1920, 1040), (1366, 728), (1280, 680), (1024, 600)):
        fitted_width, fitted_height = startup_geometry(
            width, min_width, min_height, work_width, work_height
        )
        check_ratio(fitted_width, fitted_height, "startup fit")
        if fitted_width > work_width + 0.001 or fitted_height > work_height + 0.001:
            fail(f"window clips a {work_width}x{work_height} work area")

    print("[PASS] Window default, minimum, fit, maximize and manual resize are 16:9")


if __name__ == "__main__":
    main()
