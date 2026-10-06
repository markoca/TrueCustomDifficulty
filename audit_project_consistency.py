#!/usr/bin/env python3
"""Compatibility entry point for the canonical project consistency audit.

The audit implementation lives in tools/audit_project_consistency.py.
Keeping this tiny launcher preserves the historical root command without
maintaining two independent audit implementations.
"""
from pathlib import Path
import runpy

AUDIT = Path(__file__).resolve().parent / "tools" / "audit_project_consistency.py"
if not AUDIT.is_file():
    raise SystemExit(f"Canonical audit not found: {AUDIT}")
runpy.run_path(str(AUDIT), run_name="__main__")
