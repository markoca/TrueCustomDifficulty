#!/usr/bin/env python3
"""Static vanilla refkey inheritance regression, no game ZIPs necessary."""
from audit_cfg_source_values import read_tree, check_point, resolve_refkey_inheritance

SOURCE = """Empty : struct.begin
   SID = Empty
   MutantCombatDifficulty : struct.begin
      Mutant_BaseDamage = 1.0
   struct.end
struct.end
Medium : struct.begin {refkey=Empty}
   SID = Medium
struct.end
"""

def probe(key: str, expected: str, text: str = SOURCE,
          *, filename: str = "GameData/DifficultyPrototypes.cfg",
          prototype: str = "Medium"):
    point = dict(parameterId="combat.originalInheritance", scope="baseGame",
                 file="GameData/DifficultyPrototypes.cfg",
                 prototype="Medium", nodePath=["MutantCombatDifficulty"],
                 key=key, sourceRaw=expected)
    roots = read_tree(text)
    direct = check_point(point, roots)
    assert direct["status"] == "missing_node", direct
    return resolve_refkey_inheritance(point, roots, text.splitlines(), direct)

def main():
    for key in ("Mutant_BaseDamage",):
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
    # Gamepass Rare Stash production wrappers are thin refurl aliases.
    # The actual coupon Chance rows are authored under the parent SID
    # in the very same flattened decoded ItemGeneratorPrototypes.cfg.
    loot_source = """GamePass_Stash_ItemGenerator_Rare : struct.begin
   SID = GamePass_Stash_ItemGenerator_Rare
   ItemGenerator : struct.begin
      [11] : struct.begin
         PossibleItems : struct.begin
            [0] : struct.begin
               Chance = 0.2f
            struct.end
         struct.end
      struct.end
   struct.end
struct.end
Stash_Expensive_Newbie : struct.begin {refurl=ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg;refkey=GamePass_Stash_ItemGenerator_Rare}
   SID = Stash_Expensive_Newbie
struct.end
"""
    loot = dict(parameterId="loot.expensiveStashMoneyChance", scope="baseGame",
                file="GameData/ItemGeneratorPrototypes.cfg",
                prototype="Stash_Expensive_Newbie",
                nodePath=["ItemGenerator", "[11]", "PossibleItems", "[0]"],
                key="Chance", sourceRaw="0.2f")
    roots = read_tree(loot_source)
    direct = check_point(loot, roots)
    assert direct["status"] == "missing_node", direct
    found = resolve_refkey_inheritance(loot, roots, loot_source.splitlines(), direct)
    assert found["status"] == "inherited_exact" and found["inheritedRaw"] == "0.2f", found
    assert found["refkeyChain"] == ["GamePass_Stash_ItemGenerator_Rare"], found
    assert found["refurlEdges"] == [{
        "from": "Stash_Expensive_Newbie",
        "refurl": "ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg",
        "refkey": "GamePass_Stash_ItemGenerator_Rare",
        "resolution": "physical_parent_in_consolidated_ItemGeneratorPrototypes"
    }], found
    absent = loot_source.replace("Chance = 0.2f", "Chance = 0.1f")
    raw_mismatch = resolve_refkey_inheritance(
        loot, read_tree(absent), absent.splitlines(),
        check_point(loot, read_tree(absent)))
    assert raw_mismatch["status"] == "inherited_raw_mismatch", raw_mismatch
    no_parent = loot_source.replace("refkey=GamePass_Stash_ItemGenerator_Rare",
                                     "refkey=MissingGamePassTemplate")
    unresolved = resolve_refkey_inheritance(
        loot, read_tree(no_parent), no_parent.splitlines(),
        check_point(loot, read_tree(no_parent)))
    assert unresolved["inheritanceStatus"] == "missing_or_ambiguous_ancestor", unresolved
    foreign = dict(loot, file="GameData/StashPrototypes.cfg")
    blocked_foreign = resolve_refkey_inheritance(
        foreign, roots, loot_source.splitlines(), check_point(foreign, roots))
    assert blocked_foreign["inheritanceStatus"] == "refurl_not_verified", blocked_foreign
    print("PASS: vanilla refkey and restricted flattened Consumable/Gamepass aliases (mismatch/missing/foreign fail closed)")

if __name__ == "__main__":
    main()
