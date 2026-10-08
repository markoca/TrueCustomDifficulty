#!/usr/bin/env python3
"""Fail publish if user-locked UI source or specifications drift.

Update only after explicit UI-design approval, and document that approval.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LOCK = ROOT / "docs" / "UI-DESIGN-LOCK.json"


def blob_sha(data: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--update", action="store_true",
                        help="Explicitly re-snapshot locked UI after approved design change")
    parser.add_argument("--approval", default="",
                        help="Required UI registry ID (UI-XX) for an approved design change")
    args = parser.parse_args()

    data = json.loads(LOCK.read_text(encoding="utf-8"))
    assert data["state"] == "LOCKED", "UI design lock must remain LOCKED"
    files = data["lockedFiles"]
    assert files, "Missing UI snapshot"

    if args.update:
        if not args.approval.startswith("UI-") or not args.approval[3:].isdigit():
            parser.error("--update requires --approval UI-XX")
        # Explicit approval documentation is mandatory; tool cannot verify
        # human approval, so this mode must never run in build scripts.
        policy = (ROOT / "docs" / "UI-DESIGN-LOCK.md").read_text(encoding="utf-8")
        heading = "## Change approvals"
        record = policy.split(heading, 1)[-1] if heading in policy else ""
        if args.approval not in record:
            parser.error("Document the requested design change under Change approvals first")
        data["lockedFiles"] = {
            path: blob_sha((ROOT / path).read_bytes()) for path in files
        }
        data["approval"] = args.approval
        LOCK.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
        print("[PASS] Approved UI lock snapshot updated:", args.approval)
        return

    errors = []
    for name, expected in files.items():
        candidate = ROOT / name
        if not candidate.is_file():
            errors.append(f"missing locked file: {name}")
            continue
        actual = blob_sha(candidate.read_bytes())
        if actual != expected:
            errors.append(f"UI DESIGN LOCK VIOLATION: {name} (expected {expected}, got {actual})")
    if errors:
        for error in errors:
            print("[FAIL]", error)
        raise SystemExit(
            "UI design changed without approval. Restore the locked version or "
            "obtain explicit authorization and record it in docs/UI-DESIGN-LOCK.md."
        )
    print(f"[PASS] UI design lock: {len(files)} source/spec snapshots unchanged")


if __name__ == "__main__":
    main()
