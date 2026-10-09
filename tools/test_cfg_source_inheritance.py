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

def probe(key: str, expected: str, text: str = SOURCE,\n          *, filename: str = "GameData/DifficultyPrototypes.cfg",\n          prototype: str = "Medium"):
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
    # The decoded runtime ItemPrototypes file flattens ConsumablePrototypes
    # definitions. Cross-file refurl is acceptable ONLY for this verified
    # filename+refurl pair AND when the referenced ancestor is physically
    # authored exactly once in the current source roots.
    item_text = """Bread : struct.begin
   SID = Bread
   Weight = 0.2
struct.end
FreshBread : struct.begin {refkey=Bread}
   SID = FreshBread
struct.end
EQ08_FreshBread : struct.begin {refurl=ConsumablePrototypes.cfg;refkey=FreshBread}
   SID = EQ08_FreshBread
struct.end
"""
    item = dict(parameterId="equipment.consumableWeight", scope="baseGame",
                file="GameData/ItemPrototypes.cfg", prototype="EQ08_FreshBread",
                nodePath=[], key="Weight", sourceRaw="0.2")
    roots = read_tree(item_text)
    direct = check_point(item, roots)
    assert direct["status"] == "missing_key", direct
    result = resolve_refkey_inheritance(item, roots, item_text.splitlines(), direct)
    assert result["status"] == "inherited_exact" and result["inheritedFrom"] == "Bread", result
    assert result["refkeyChain"] == ["FreshBread", "Bread"], result
    assert result["refurlEdges"] == [{
        "from": "EQ08_FreshBread", "refurl": "ConsumablePrototypes.cfg",
        "refkey": "FreshBread", "resolution": "physical_parent_in_consolidated_ItemPrototypes"
    }], result
    unknown_file = dict(item, file="GameData/OtherPrototypes.cfg")
    unknown_direct = check_point(unknown_file, roots)
    blocked = resolve_refkey_inheritance(unknown_file, roots, item_text.splitlines(), unknown_direct)
    assert blocked["status"] == "missing_key" and blocked["inheritanceStatus"] == "refurl_not_verified", blocked
    missing_parent_text = item_text.replace("refkey=FreshBread", "refkey=Unknown")  # both markers are changed
    missing_parent = resolve_refkey_inheritance(
        item, read_tree(missing_parent_text), missing_parent_text.splitlines(),
        check_point(item, read_tree(missing_parent_text)))
    assert missing_parent["status"] == "missing_key", missing_parent
    assert missing_parent["inheritanceStatus"] == "missing_or_ambiguous_ancestor", missing_parent
    print("PASS: vanilla refkey inheritance and validated flattened refurl; missing parents and foreign refurls fail closed")

if __name__ == "__main__":
    main()
