#!/usr/bin/env python3
"""Static vanilla refkey inheritance regression, no game ZIPs necessary."""
from audit_cfg_source_values import read_tree, check_point, resolve_refkey_inheritance

SOURCE = """Empty : struct.begin
   SID = Empty
   MutantCombatDifficulty : struct.begin
      Mutant_BaseDamage = 1.0
      Mutant_AttackCooldown = 1.0
   struct.end
struct.end
Medium : struct.begin {refkey=Empty}
   SID = Medium
struct.end
"""

def probe(key: str, expected: str, text: str = SOURCE):
    point = dict(parameterId="combat.originalInheritance", scope="baseGame",
                 file="GameData/DifficultyPrototypes.cfg",
                 prototype="Medium", nodePath=["MutantCombatDifficulty"],
                 key=key, sourceRaw=expected)
    roots = read_tree(text)
    direct = check_point(point, roots)
    assert direct["status"] == "missing_node", direct
    return resolve_refkey_inheritance(point, roots, text.splitlines(), direct)

def main():
    for key in ("Mutant_BaseDamage", "Mutant_AttackCooldown"):
        result = probe(key, "1.0")
        assert result["status"] == "inherited_exact" and result["inheritedFrom"] == "Empty", result
        assert result["refkeyChain"] == ["Empty"], result
    mismatch = probe("Mutant_BaseDamage", "0.4")
    assert mismatch["status"] == "inherited_raw_mismatch", mismatch
    wrong_ref = probe("Mutant_BaseDamage", "1.0", SOURCE.replace("refkey=Empty", "refkey=Unknown"))
    assert wrong_ref["status"] == "missing_node" and wrong_ref["inheritanceStatus"] == "missing_or_ambiguous_ancestor", wrong_ref
    cycle = probe("Mutant_BaseDamage", "1.0", SOURCE.replace("refkey=Empty", "refkey=Medium"))
    assert cycle["status"] == "missing_node" and cycle["inheritanceStatus"] == "cycle", cycle
    no_ref = probe("Mutant_BaseDamage", "1.0", SOURCE.replace(" {refkey=Empty}", ""))
    assert no_ref["status"] == "missing_node" and no_ref["inheritanceStatus"] == "no_explicit_refkey_ancestor", no_ref
    print("PASS: direct-missing vs explicitly inherited Combat vanilla values, mismatch, wrong refkey and cycles")

if __name__ == "__main__":
    main()
