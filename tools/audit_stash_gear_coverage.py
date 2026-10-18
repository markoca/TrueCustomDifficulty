#!/usr/bin/env python3
"""Static regression: exact eligible actors, player ranks, 34+4 gear pool and UI parity.

The per-actor selection is at BUILD time, not a native loot Chance.
Does not replace generated BPatch or in-game runtime acceptance tests.
"""
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = root / "src/TrueCustomDifficulty/Data"
def rows(name):
    with (data / name).open(encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f))
base = {r["ActorSID"]: r for r in rows("smartloot-regular-world-actor-manifest.csv")
        if r["ClueVariableSID"] == "EmptyInherited"}
dlc = {r["ActorSID"]: r for r in rows("dlc1-stash-eligible-actor-manifest.csv")}
physical = {r["actor_sid"]: r for r in rows("physical-stash-safety-catalog.csv")}
protected = {r["ActorSID"] for r in rows("base-stash-protected-actor-manifest.csv")}
ranks = rows("stash-gear-actor-ranks.csv")
pool = rows("gear-progression-ranks.csv")
assert len(base) == 803 and len(dlc) == 112 and len(ranks) == 915
assert len({r["ActorSID"] for r in ranks}) == len(ranks)
counts = Counter(r["Scope"] for r in ranks)
assert counts == {"base": 803, "dlc1": 112}
rank_counts = Counter(r["RankCodes"] for r in ranks if r["Scope"] == "base")
assert rank_counts == {"N": 295, "E": 196, "V": 174, "M": 92, "A": 45, "X": 1}
selected = []
for r in ranks:
    sid, scope, code = r["ActorSID"], r["Scope"], r["RankCodes"]
    assert sid not in protected
    assert scope in ("base", "dlc1")
    native = (base if scope == "base" else dlc)[sid]
    assert physical[sid]["safety_class"] == (
        "REGULAR_SMARTLOOT" if scope == "base" else "DLC1_ELIGIBLE_MANIFEST")
    source_refs = [s for s in native["OriginalItemGeneratorSIDs"].split(",") if s]
    assert len(source_refs) == int(r["ExistingGeneratorCount"]), sid
    assert code in ("N", "E", "V", "M", "A", "X")
    assert code != "A" or len(source_refs) == 4
    assert code != "X" or not source_refs
    if source_refs and sid != "1CBBBE27428A1766B7F911863C044833":
        selected.append(r)
assert Counter(r["Scope"] for r in selected) == {"base": 800, "dlc1": 108}
assert len(pool) == 142 and len({r["ItemSID"] for r in pool}) == 142
assert Counter((r["ItemClass"], r["ProgressionTier"]) for r in pool) == {
    ("Weapon", "T1"): 8, ("Weapon", "T2"): 10,
    ("Weapon", "T3"): 10, ("Weapon", "T4"): 6,
    ("Armor", "T1"): 7, ("Armor", "T2"): 10,
    ("Armor", "T3"): 13, ("Armor", "T4"): 9,
    ("Artifact", "T1"): 39, ("Artifact", "T2"): 17,
    ("Artifact", "T3"): 9, ("Artifact", "T4"): 4
}
spawnable = {r["sid"]: r for r in rows("spawnable-item-sids.csv")}
assert all(r["ItemSID"] in spawnable
           and spawnable[r["ItemSID"]]["category"] == {
               "Weapon": "weapons", "Armor": "armor", "Artifact": "artifacts"
           }[r["ItemClass"]]
           and r["UsagePolicy"] == "STASH_BONUS_POOL"
           and not any(x in r["ItemSID"].lower() for x in (
               "gauss", "rpg", "trophy", "weird", "quest",
               "fake", "prolog", "cnpp", "heart"))
           for r in pool)
standard = {sid for sid, r in spawnable.items()
            if r["category"] == "artifacts"
            and r["definition_origin"] == "base"
            and sid.startswith(("CArtifact", "EArtifact", "FArtifact", "GArtifact"))
            and not sid.endswith("_Fake")}
assert {r["ItemSID"] for r in pool if r["ItemClass"] == "Artifact"} == standard
assert all(r["ItemClass"] != "Armor" or
           (r["ItemSID"].endswith("_Armor")
            and not r["ItemSID"].startswith("NPC_"))
           for r in pool)
def h(row):
    key = "TCD-StashGear-2026-10-18|ACTOR|" + row["Scope"] + "|" + row["ActorSID"]
    return (int.from_bytes(hashlib.sha256(key.encode()).digest()[:8], "big"),
            row["ActorSID"])
for scope, num in (("base", 800), ("dlc1", 108)):
    candidates = sorted((x for x in selected if x["Scope"] == scope), key=h)
    assert len(candidates) == num
    previous = set()
    for percent in range(101):
        count = (num * percent + 50) // 100
        current = {x["ActorSID"] for x in candidates[:count]}
        assert len(current) == count and previous <= current
        previous = current
cfg = json.loads((data / "parameters.json").read_text(encoding="utf-8"))
ids = {x["id"]: x for x in cfg["parameters"]}
assert len(cfg["parameters"]) == 281
assert ids["loot.stashGearEnabled"]["control"] == "toggle"
assert ids["loot.stashGearCoveragePercent"]["slider"] == {
    "min": 0, "max": 100, "step": 1, "stepMode": "absolute"}
assert ids["loot.stashGearCoveragePercent"]["requiresToggle"] == "loot.stashGearEnabled"
assert not any(x in ids for x in ("loot.stashGearCheapWeapon", "loot.stashGearCheapArmor",
    "loot.stashGearMediumWeapon", "loot.stashGearMediumArmor",
    "loot.stashGearExpensiveWeapon", "loot.stashGearExpensiveArmor"))
baseline = json.loads((data / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
assert baseline["supportedParameterCount"] == 281
ui = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
writer = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashGearCoverage.cs").read_text(encoding="utf-8")
assert 'P("loot.stashGearCoveragePercent")' in ui and "DrawStashGearMode(" not in ui
assert 'StashGearStableHash("ACTOR|" + x.Scope + "|" + x.ActorSid)' in writer
assert 'ChooseStashGearClasses(all, actor.ActorSid, code, tier)' in writer
assert 'StashGearStableHash(key)' in writer
assert '"MIX2|" + actorSid' in writer
assert 'remaining.RemoveAt(i);' in writer
assert 'slot < 2' in writer
assert 'selectedClasses.Count > 2' in writer
assert 'selectedClasses.Distinct(StringComparer.Ordinal).Count()' in writer
assert '"ITEM|" + chosenClass' in writer
# Avoid CS0136: 'chosen' is the outer selected-actor collection,
# never a nested candidate local with the same name in rank loops.
assert "var chosen = SelectStashGearActors(" in writer
assert 'var selectedItem = chosenClass == "Attachment" ? null : eligible[' in writer
assert '"Weapon" when selectedItem!.ItemType == "Pistol"' in writer
assert 'item.SetAssignment("ItemPrototypeSID", selectedItem!.Sid)' in writer
assert writer.count("var chosen =") == 1
assert 'for (var categoryIndex = 0; categoryIndex < selectedClasses.Count;' in writer
assert 'itemGenerator.GetOrAddChild(' in writer
assert 'item.SetAssignment("Chance", itemChance)' in writer
assert 'itemCategory.SetAssignment("bAllowSameCategoryGeneration", "false");' in writer
assert 'item.SetAssignment("Chance", itemChance)' in writer
assert 'all.TryGetValue("loot.stashGearBonusChance", out var chanceControl)' in writer
assert 'CultureInfo.InvariantCulture' in writer and '/ 100.0).ToString("0.##"' in writer
chance = ids["loot.stashGearBonusChance"]
assert chance["requiresToggle"] == "loot.stashGearEnabled"
assert chance["control"] == "slider"
assert chance["vanilla"] == {"All": 50}
assert chance["slider"] == {"min": 0, "max": 100, "step": 1, "stepMode": "absolute"}
assert 'P("loot.stashGearBonusChance")' in ui
for kind in ("Weapon", "Armor", "Artifact", "Attachment"):
    row = ids["loot.stashGear" + kind + "Weight"]
    assert row["requiresToggle"] == "loot.stashGearEnabled"
    assert row["slider"] == {"min": 0, "max": 3, "step": 0.1, "stepMode": "absolute"}
    assert row["control"] == "slider"
assert 'MinCount", "1"' in writer and 'MaxCount", "1"' in writer
assert '"Attachment" => "SubItemGenerator"' in writer
assert 'item.SetAssignment("ItemGeneratorPrototypeSID",' in writer
assert 'tier < 2' in writer
assert 'FittingWeaponsSIDs' not in writer
attachment_rows = rows("stash-bonus-attachment-progressions.csv")
assert len(attachment_rows) == 19
assert Counter(r["ProgressionTier"] for r in attachment_rows) == {
    "T2": 8, "T3": 7, "T4": 4}
source_pool = {r["item_sid"]: r for r in rows("world-attachment-shuffle-pool.csv")}
source_rows = rows("world-attachment-generator-manifest.csv")
for candidate in attachment_rows:
    sid = candidate["ItemSID"]
    generator = candidate["SourceGeneratorSID"]
    assert candidate["Evidence"] == "EXACT_AUDITED_SINGLETON_GENERATOR"
    assert source_pool[sid]["tier"] == candidate["ProgressionTier"]
    assert generator == "Stash_" + sid
    originals = [r for r in source_rows if r["generator_sid"] == generator]
    assert originals and all(r["item_sid"] == sid
        and r["entry_node_path"] == "ItemGenerator/[0]/PossibleItems/[0]"
        for r in originals)
assert all("WeaponSID" not in candidate for candidate in attachment_rows)
print("PASS: 800 base + 108 DLC eligible physical actors; rank metadata; 34 weapons + 39 suits + 69 standard artifacts; four category weights and 19 rank-only attachment candidates; shared Bonus Chance; coverage stable")
