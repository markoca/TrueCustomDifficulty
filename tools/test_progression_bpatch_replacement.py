#!/usr/bin/env python3
"""Regression guard for whole-node replacement vs unsafe SID-only BPatch."""
import re
import unittest

BEGIN = re.compile(r"^\s*([^:]+?)\s*:\s*struct\.begin(?:\s+\{([^}]*)\})?\s*$")
END = re.compile(r"^\s*struct\.end\s*$")
ASSIGN = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$")

def parse(text):
    root = {"fields": {}, "children": {}}
    stack = [root]
    for line in text.splitlines():
        if not line.strip() or line.lstrip().startswith("//"):
            continue
        start = BEGIN.match(line)
        if start:
            name = start.group(1).strip()
            child = {"fields": {}, "children": {}, "attributes": start.group(2) or ""}
            parent = stack[-1]
            if name in parent["children"]:
                raise ValueError(f"Duplicate child: {name}")
            parent["children"][name] = child
            stack.append(child)
        elif END.match(line):
            if len(stack) == 1:
                raise ValueError("Unmatched struct.end")
            stack.pop()
        elif match := ASSIGN.match(line):
            stack[-1]["fields"][match.group(1)] = match.group(2)
        else:
            raise ValueError(f"Unknown CFG syntax: {line}")
    if len(stack) != 1:
        raise ValueError("Unclosed CFG block")
    return root

def merge_like_bpatch(original, patch):
    """Conservative field/child merge model; NOT a claim about Unreal semantics."""
    out = {"fields": dict(original["fields"]), "children": dict(original["children"])}
    out["fields"].update(patch["fields"])
    for name, child in patch["children"].items():
        if name in out["children"]:
            out["children"][name] = merge_like_bpatch(out["children"][name], child)
        else:
            out["children"][name] = child
    return out

OLD = """
[0] : struct.begin
    ItemPrototypeSID = GunBucket_PP
    Chance = 1.0
    Upgrades : struct.begin
        [0] : struct.begin
            UpgradeSID = BucketUpgrade
        struct.end
    struct.end
    Attaches : struct.begin
        [0] : struct.begin
            AttachmentSID = BucketMagazine
        struct.end
    struct.end
struct.end
"""
NEW = """
[0] : struct.begin
    ItemPrototypeSID = GunM860_SG
    Chance = 1.0
struct.end
"""

class ReplacementSafety(unittest.TestCase):
    def test_sid_only_patch_leaks_old_weapon_children(self):
        old = parse(OLD)["children"]["[0]"]
        new = parse(NEW)["children"]["[0]"]
        merged = merge_like_bpatch(old, new)
        self.assertEqual(merged["fields"]["ItemPrototypeSID"], "GunM860_SG")
        self.assertIn("Upgrades", merged["children"])
        self.assertIn("Attaches", merged["children"])

    def test_explicit_full_replacement_removes_old_children(self):
        old = parse(OLD)["children"]["[0]"]
        new = parse(NEW)["children"]["[0]"]
        replaced = new
        self.assertIn("Upgrades", old["children"])
        self.assertNotIn("Upgrades", replaced["children"])
        self.assertNotIn("Attaches", replaced["children"])
        self.assertEqual(replaced["fields"]["Chance"], "1.0")

    def test_parser_rejects_duplicate_nodes(self):
        with self.assertRaises(ValueError):
            parse(OLD + OLD)

if __name__ == "__main__":
    unittest.main()
