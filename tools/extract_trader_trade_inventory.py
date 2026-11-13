#!/usr/bin/env python3
"""Extract real STALKER 2 trader SELL candidates + vanilla BUY restrictions.

Runs offline against *user-supplied, decoded* base and DLC1 CFG ZIPs.
Never treats missing BuyLimitations as confirmed ability to buy an item.
Generates no BPatch/UI or changed gameplay files.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict
import csv
import hashlib
import json
from pathlib import Path
import re
import sys
import zipfile

BASE_ZIP_SHA = "7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1"
DLC_ZIP_SHA = "3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6"
ORIG = {
    "base": {"trade": "GameData/TradePrototypes.cfg", "stock": "GameData/ItemGeneratorPrototypes.cfg", "items": "GameData/ItemPrototypes.cfg"},
    "dlc1": {"trade": "DLCGameData/DLC1/TradePrototypes.cfg", "stock": "DLCGameData/DLC1/ItemGeneratorPrototypes.cfg", "items": "DLCGameData/DLC1/ItemPrototypes.cfg"},
}
ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "src/TrueCustomDifficulty/Data/trader-min-buy-condition-manifest.csv"
ROLE_LEDGER = ROOT / "docs/generators/trader-role-scope-catalog.csv"
PROD_EXCLUDES = {"[0]", "BaseTraderNPC_Template", "BasicTrader", "NoTrade", "AllTraderNPC", "TradeTest"}
TYPES = ("Weapon", "Armor", "Ammo", "Grenade", "Attach", "Consumable", "MutantLoot", "Artifact")
# 8 native values actually observed in vanilla BuyLimitations; absence of a
# category in this observed set does not prove the engine cannot filter it.
RANKS = ("Newbie", "Experienced", "Veteran", "Master")
BEGIN = re.compile(r"^\s*(.*?)\s*:\s*struct\.begin\s*(\{[^}]*\})?\s*$")
END = re.compile(r"^\s*struct\.end\s*$")
FIELD = re.compile(r"^\s*(.*?)\s*=\s*(.*?)\s*$")
TOKEN = re.compile(r"\w+=([^;}]+)")

MEDICAL = set("AntiRad ArmyMedkit Bandage EcoMedkit Hercules Medkit".split())
FOOD = set("Bread CannedFood Milk MonolithDryRation Sausage".split())
DRINKS = set("Beer Energetic Vodka Water".split())
DETECTORS = set("Bear Echo Gilka Veles".split())
GRENADES = {"GrenadeF1", "GrenadeRGD5"}
AMMO = set("""A012A A012D A012E A045A A045D A045E A545A A545D A545E
A556A A556D A556E A556S A762NATOA A762NATOD A762NATOS
A762SniperA A762SniperD A762SniperS A918A A918D A919A A919D A0919A
A939D A939E A939S AGA AHEDP APG7V AVOG""".split())


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as s:
        for chunk in iter(lambda: s.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parse_cfg(content: bytes, identifier: str):
    text = content.decode("utf-8-sig")
    roots, stack = {}, []
    for line_no, line in enumerate(text.splitlines(), 1):
        m = BEGIN.match(line)
        if m:
            key = m.group(1).strip()
            attrs = dict(re.findall(r"(\w+)=([^;}\s]+)", m.group(2) or ""))
            n = {"key": key, "attrs": attrs, "fields": {}, "children": {}, "line": line_no}
            owner = stack[-1]["children"] if stack else roots
            if key in owner:
                raise ValueError(f"{identifier}:{line_no} duplicate struct {key}")
            owner[key] = n
            stack.append(n)
        elif END.match(line):
            if not stack:
                raise ValueError(f"{identifier}:{line_no} unexpected struct.end")
            stack.pop()
        elif stack:
            m = FIELD.match(line)
            if m:
                k, val = m.group(1).strip(), m.group(2).strip()
                if k in stack[-1]["fields"]:
                    raise ValueError(f"{identifier}:{line_no} duplicate assignment {k}")
                stack[-1]["fields"][k] = val
            elif line.strip() and not line.lstrip().startswith("//"):
                raise ValueError(f"{identifier}:{line_no} unsupported syntax {line[:80]!r}")
        elif line.strip() and not line.lstrip().startswith("//"):
            raise ValueError(f"{identifier}:{line_no} unknown top-level syntax")
    if stack:
        raise ValueError(f"{identifier}: unbalanced structs")
    return roots, text


def c(node, *path):
    for key in path:
        if node is None:
            return None
        node = node["children"].get(key)
    return node


def field(node, key, default=""):
    return node["fields"].get(key, default) if node else default


def boolstr(value):
    return str(value).strip().lower() == "true"


def sorted_nodes(node):
    if not node:
        return []
    return sorted(node["children"].values(), key=lambda x: (0, int(x["key"][1:-1])) if re.fullmatch(r"\[\d+\]", x["key"]) else (1, x["key"]))


def role_for_trade(sid: str) -> str:
    if sid.startswith("GeneralNPC_"):
        return "GENERIC_NPC_TRADER"
    if "Medic" in sid:
        return "MEDIC"
    if "Technician" in sid:
        return "TECHNICIAN"
    if "Bartender" in sid:
        return "BARTENDER"
    return "GENERAL_TRADER"


def category_for_sid(sid: str) -> str:
    if sid == "empty":
        return "Sentinel"
    if sid.startswith("Binoculars_"):
        return "Binoculars"
    if sid.startswith("NVG_"):
        return "NightVision"
    if sid == "GunUDP_MagIncreased":
        return "Attachments"
    if sid in MEDICAL: return "Medical"
    if sid in FOOD: return "Food"
    if sid in DRINKS: return "Drinks"
    if sid in GRENADES: return "Grenades"
    if sid in DETECTORS: return "Detectors"
    if sid in AMMO: return "Ammo"
    if sid.startswith(("CArtifact", "EArtifact", "GArtifact")): return "Artifacts"
    if sid.startswith(("EN_", "RU_")): return "Attachments"
    if sid.startswith("Gun"): return "Weapons"
    if sid.endswith(("_Armor", "_Helmet")) or sid in {"DutyArmor_3_U1", "DutyMask_1"}: return "Armor"
    return "UNCLASSIFIED"


def rank_filter(node):
    val = field(node, "PlayerRank")
    if not val:
        return set(RANKS)
    ranks = {x.strip().split("::")[-1] for x in val.split(",")}
    if not ranks <= set(RANKS):
        raise ValueError(f"unknown rank filter {ranks} in node {node['key']}")
    return ranks


def find_stock_target(sid, preferred_scope, stocks):
    # Cross-file SID resolution is common for DLC1 merchant wrappers.
    # The effective target is in the requesting DLC first (if present), then base.
    for scope in (preferred_scope, "base", "dlc1"):
        if sid in stocks[scope]:
            return scope, stocks[scope][sid]
    return None, None


def sell_rows(profile, stock_sid, stock_scope, stocks):
    """All reachable direct candidates; no claim that each appears every refresh."""
    out, missing, loops = [], [], []

    def visit(gen_sid, prefer, ranks, chain, odds, depth):
        if depth > 16:
            raise ValueError(f"excessive generator nesting {chain}")
        if gen_sid in chain:
            loops.append(" -> ".join(chain + (gen_sid,)))
            return
        scope, root = find_stock_target(gen_sid, prefer, stocks)
        if root is None:
            missing.append(gen_sid)
            return
        cat_holder = c(root, "ItemGenerator")
        if not cat_holder:
            missing.append(gen_sid+"/ItemGenerator")
            return
        for category in sorted_nodes(cat_holder):
            cat_ranks = ranks & rank_filter(category)
            if not cat_ranks:
                continue
            native_cat = field(category, "Category")
            possible = c(category, "PossibleItems")
            if not possible:
                continue
            for item in sorted_nodes(possible):
                sid = field(item, "ItemPrototypeSID")
                nested = field(item, "ItemGeneratorPrototypeSID")
                chance = field(item, "Chance")
                weight = field(item, "Weight")
                state = (
                    "ZERO_CHANCE" if chance and not float(chance.rstrip("f")) else
                    "ZERO_WEIGHT" if not chance and weight and not float(weight.rstrip("f")) else
                    "CANDIDATE"
                )
                if state != "CANDIDATE":
                    # preserve zero/off items as source rows, but distinguish them
                    pass
                path = "ItemGenerator/"+category["key"]+"/PossibleItems/"+item["key"]
                if sid and sid != "empty":
                    out.append({
                        "trade_sid": profile, "root_stock_sid": stock_sid,
                        "item_sid": sid, "item_category": category_for_sid(sid),
                        "generator_sid": gen_sid, "generator_scope": scope,
                        "generator_cfg_line": root["line"], "entry_cfg_line": item["line"],
                        "entry_cfg_path": path,
                        "generation_native_category": native_cat,
                        "player_ranks": ";".join(r for r in RANKS if r in cat_ranks),
                        "chance": chance, "weight": weight,
                        "min_count": field(item,"MinCount"), "max_count": field(item,"MaxCount"),
                        "direct_entry_state": state,
                        "nested_chain": ">".join(chain+(gen_sid,)),
                        "outer_links": ">".join(odds),
                    })
                if nested and nested != "empty":
                    # Rare pathological branches may have both item and nested SID.
                    # Keep both identities, no normalization into one.
                    visit(nested,scope,cat_ranks,chain+(gen_sid,),odds+(f"{gen_sid}:{path}:chance={chance}:weight={weight}",),depth+1)
    if stock_sid and stock_sid != "Empty":
        visit(stock_sid,stock_scope,set(RANKS),(),(),0)
    return out,missing,loops


def as_csv(path, rows, columns):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w",newline="",encoding="utf-8") as h:
        writer=csv.DictWriter(h,fieldnames=columns,lineterminator="\n",extrasaction="ignore")
        writer.writeheader();writer.writerows(rows)


def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--base-zip",type=Path,required=True)
    p.add_argument("--dlc1-zip",type=Path,required=True)
    p.add_argument("--out",type=Path,required=True)
    p.add_argument("--accept-new-hashes",action="store_true",help="Explicitly permit other CFG hashes for exploratory runs only")
    args=p.parse_args(argv)
    zip_paths={"base":args.base_zip,"dlc1":args.dlc1_zip}
    expected={"base":BASE_ZIP_SHA,"dlc1":DLC_ZIP_SHA}
    archive_hashes={scope:sha256(path) for scope,path in zip_paths.items()}
    if archive_hashes!=expected and not args.accept_new_hashes:
        raise ValueError(f"CFG archive hash mismatch; pass --accept-new-hashes only to explore: {archive_hashes}")
    data={}; decoded_hashes={}; filelines={}
    for scope,zippath in zip_paths.items():
        with zipfile.ZipFile(zippath) as z:
            data[scope]={}
            for kind,member in ORIG[scope].items():
                nodes,source=parse_cfg(z.read(member),member)
                data[scope][kind]=nodes
                decoded_hashes[f"{scope}/{kind}"]=hashlib.sha256(source.encode()).hexdigest()
                filelines[f"{scope}/{kind}"]=len(source.splitlines())
    trade={s:data[s]["trade"] for s in data}
    stock={s:data[s]["stock"] for s in data}
    if len(trade["base"])!=74 or len(trade["dlc1"])!=18:
        raise ValueError("Expected 74 base + 18 DLC trade prototypes")
    if len(stock["base"])<3000 or len(stock["dlc1"])<100:
        raise ValueError("Unreal CFG ItemGenerator roots unexpectedly small")

    buy_rows=[];offers=[];warnings=[];orphan_sale=[]
    for scope,roots in trade.items():
        for sid,root in roots.items():
            if scope=="base" and sid in PROD_EXCLUDES:
                continue
            assert sid not in PROD_EXCLUDES, sid
            gen_container = c(root,"TradeGenerators")
            rules = sorted_nodes(gen_container)
            if len(rules)!=1 or rules[0]["key"]!="[0]":
                warnings.append(f"{scope}:{sid} unexpected rule count {len(rules)}")
            rule = rules[0] if rules else None
            if not rule: continue
            limitations=c(rule,"BuyLimitations")
            limitations_list=[]
            if limitations:
                if limitations["children"]: raise ValueError(f"Nested BuyLimitations entries at {sid}")
                limitations_list=[v.split("::")[-1] for _,v in sorted(limitations["fields"].items(),key=lambda pair: int(pair[0].strip("[]")))]
            if len(limitations_list)!=len(set(limitations_list)):
                raise ValueError(f"Duplicate BuyLimitations items at {sid}")
            if not set(limitations_list)<=set(TYPES):
                raise ValueError(f"Unknown EItemType in {sid}: {set(limitations_list)-set(TYPES)}")
            assigned_generator=field(rule,"ItemGeneratorPrototypeSID")
            offered,missing,loops=sell_rows(sid,assigned_generator,scope,stock)
            offers+=({**x,"trade_scope":scope,"trader_type":role_for_trade(sid)} for x in offered)
            if missing: orphan_sale.append({"scope":scope,"trade":sid,"missing":sorted(set(missing))})
            if loops: warnings.append(f"{scope}:{sid} loop(s) {loops}")
            row={
                "scope":scope,"trade_sid":sid,"trader_type":role_for_trade(sid),
                "source_file":ORIG[scope]["trade"],"source_line":root["line"],
                "parent_refkey":root["attrs"].get("refkey",""),
                "stock_generator_sid":assigned_generator,
                "trade_generator_count":len(rules),"buy_limitations_state":"EXPLICIT" if limitations else "NOT_AUTHORED",
                "buy_limitations": ";".join(limitations_list),
                "buy_modifier":field(rule,"BuyModifier"),"sell_modifier":field(rule,"SellModifier"),
                "min_weapon_condition":field(rule,"WeaponSellMinDurability"),
                "min_armor_condition":field(rule,"ArmorSellMinDurability"),
                "refresh":field(root,"RefreshConditionSID"),
                "money":field(root,"Money"),"infinite_money":field(root,"bInfiniteMoney"),
                "reachable_offer_rows":len(offered),
                "distinct_offer_sids":len({x["item_sid"] for x in offered}),
                "offer_missing_refs":";".join(sorted(set(missing))),
            }
            for t in TYPES: row["buy_"+t]="BLOCKED_BY_BUYLIMITATIONS" if t in limitations_list else "NOT_BLOCKED_BY_BUYLIMITATIONS"
            buy_rows.append(row)

    if len(buy_rows)!=86 or sum(x["scope"]=="base" for x in buy_rows)!=68:
        raise ValueError(f"Unexpected production trade count {len(buy_rows)}")
    if sum(x["buy_limitations_state"]=="EXPLICIT" for x in buy_rows)!=53:
        raise ValueError("Expected 53 explicit production BuyLimitations lists")
    if any(x["offer_missing_refs"] for x in buy_rows):
        raise ValueError("Missing stock-generator targets in original CFG: " +
                         repr([(x["trade_sid"],x["offer_missing_refs"]) for x in buy_rows if x["offer_missing_refs"]]))
    if len(offers) != 2939 or len({x["item_sid"] for x in offers}) != 164:
        raise ValueError("Trader stock source census drift; inspect changed CFG graph")
    if any(x["item_category"] == "UNCLASSIFIED" for x in offers):
        raise ValueError("Unclassified stocked item SID: audit taxonomy first")

    # Distinct stock items by gameplay category and rank across source candidates.
    per_profile=defaultdict(list)
    for o in offers: per_profile[(o["trade_scope"],o["trade_sid"])].append(o)
    all_categories=("Weapons","Armor","Attachments","Ammo","Medical","Food","Drinks","Artifacts","Detectors","Grenades","NightVision","Binoculars","UNCLASSIFIED")
    summary=[]
    for r in buy_rows:
        offers_for=per_profile[(r["scope"],r["trade_sid"])]
        row={**r}
        for category in all_categories:
            vals={o["item_sid"] for o in offers_for if o["item_category"]==category and o["direct_entry_state"]=="CANDIDATE"}
            row["sell_"+category]=len(vals)
        summary.append(row)

    out=args.out;out.mkdir(parents=True,exist_ok=True)
    buycols=["scope","trade_sid","trader_type","source_file","source_line","parent_refkey","stock_generator_sid", "trade_generator_count","buy_limitations_state","buy_limitations","buy_modifier","sell_modifier","min_weapon_condition","min_armor_condition","refresh","money","infinite_money","reachable_offer_rows","distinct_offer_sids","offer_missing_refs"]+["buy_"+t for t in TYPES]
    offercols=["trade_scope","trade_sid","trader_type","root_stock_sid","item_sid","item_category","generator_sid","generator_scope","generator_cfg_line","entry_cfg_line","entry_cfg_path","generation_native_category","player_ranks","chance","weight","min_count","max_count","direct_entry_state","nested_chain","outer_links"]
    as_csv(out/"trader-buy-limits-86.csv",buy_rows,buycols)
    as_csv(out/"trader-offers-source-rows.csv",offers,offercols)
    as_csv(out/"trader-overview-86.csv",summary,buycols+["sell_"+x for x in all_categories])
    sid_rows=[]
    for profile in buy_rows:
        pathset=per_profile[(profile["scope"],profile["trade_sid"])]
        entry={"scope":profile["scope"],"trade_sid":profile["trade_sid"],"trader_type":profile["trader_type"]}
        for category in all_categories:
            candidates=sorted({o["item_sid"] for o in pathset if o["item_category"]==category and o["direct_entry_state"]=="CANDIDATE"})
            entry["sell_"+category+"_SIDs"]=";".join(candidates)
        sid_rows.append(entry)
    as_csv(out/"trader-offer-sids-86.csv",sid_rows,["scope","trade_sid","trader_type"]+["sell_"+c+"_SIDs" for c in all_categories])
    stat={
        "archive_sha256":archive_hashes,"decoded_cfg_sha256":decoded_hashes,
        "decoded_cfg_lines":filelines,
        "trade_roots":{s:len(v) for s,v in trade.items()},
        "stock_roots":{s:len(v) for s,v in stock.items()},
        "production_trades":len(buy_rows),
        "explicit_buy_limitation_lists":sum(x["buy_limitations_state"]=="EXPLICIT" for x in buy_rows),
        "missing_buy_limitation_lists":sum(x["buy_limitations_state"]=="NOT_AUTHORED" for x in buy_rows),
        "buy_rule_profiles":dict(Counter(x["buy_limitations"] or "(none)" for x in buy_rows)),
        "buy_restricted_count_by_item_type":dict(Counter(t for x in buy_rows for t in x["buy_limitations"].split(";") if t)),
        "by_trader_role":{},
        "offers_source_rows":len(offers),
        "offers_distinct_sid":len({x["item_sid"] for x in offers}),
        "offers_distinct_by_category":dict(Counter(x["item_category"] for x in offers)),
        "offer_missing":orphan_sale,
        "warnings":warnings,
    }
    for role in sorted({x["trader_type"] for x in buy_rows}):
        subset=[x for x in buy_rows if x["trader_type"]==role]
        stat["by_trader_role"][role]={
            "trade_count":len(subset),
            "explicit_buy_limitation_lists":sum(x["buy_limitations_state"]=="EXPLICIT" for x in subset),
            "limit_patterns":dict(Counter(x["buy_limitations"] or "(none)" for x in subset)),
        }
    (out/"trader-summary.json").write_text(json.dumps(stat,indent=2,ensure_ascii=False,default=str)+"\n",encoding="utf8")
    print(json.dumps(stat,indent=2,default=str))
    return 0

if __name__ == "__main__":
    sys.exit(main())
