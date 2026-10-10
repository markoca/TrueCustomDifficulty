#!/usr/bin/env python3
"""Static consistency checks for the source package; requires only Python 3."""
from __future__ import annotations
import csv as _csv
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
DOCS = ROOT / "docs"
CSPROJ = ROOT / "src" / "TrueCustomDifficulty" / "TrueCustomDifficulty.csproj"

errors: list[str] = []
notes: list[str] = []
not_run_items: list[str] = []

def fail(msg: str) -> None: errors.append(msg)
def note(msg: str) -> None: notes.append(msg)
def not_run(msg: str) -> None: not_run_items.append(msg)

def walk_parameter_ids(value, path="special"):
    if isinstance(value, dict):
        for key, child in value.items():
            child_path = f"{path}.{key}"
            if key in {"parameterId", "ammoQuantityParameterId", "grenadeChanceParameterId", "grenadeQuantityParameterId", "couponChanceParameterId", "removalPercentParameterId"}:
                yield child_path, child
            elif key in {"psyParameterKeys", "mutantCooldownParameters"} and isinstance(child, dict):
                for parameter_id in child:
                    yield f"{child_path}[key]", parameter_id
            elif key == "weightParameterIds" and isinstance(child, dict):
                for parameter_id in child.values():
                    yield f"{child_path}[value]", parameter_id
            else:
                yield from walk_parameter_ids(child, child_path)
    elif isinstance(value, list):
        for i, child in enumerate(value):
            yield from walk_parameter_ids(child, f"{path}[{i}]")

parameters_doc = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
parameters = parameters_doc.get("parameters", [])
ids = [p.get("id") for p in parameters]
id_set = set(ids)

if len(ids) != len(id_set):
    dupes = sorted({x for x in ids if ids.count(x) > 1})
    fail(f"duplicate parameter ids: {', '.join(dupes)}")

expected = baseline.get("supportedParameterCount")
if expected != len(parameters):
    fail(f"supportedParameterCount={expected}, but parameters.json contains {len(parameters)} parameters")
else:
    note(f"parameter catalog count: {len(parameters)}")

edit_ids = {row.get("parameterId") for row in baseline.get("editPoints", []) if row.get("parameterId")}
missing_edit_ids = sorted(edit_ids - id_set)
if missing_edit_ids:
    fail("editPoints reference unknown parameter ids: " + ", ".join(missing_edit_ids))
note(f"writer edit points: {len(baseline.get('editPoints', []))} across {len(edit_ids)} parameter ids")

special_ids = []
for path, parameter_id in walk_parameter_ids(baseline.get("special", {})):
    if isinstance(parameter_id, str):
        special_ids.append(parameter_id)
        if parameter_id not in id_set:
            fail(f"{path} references unknown parameter id {parameter_id!r}")
note(f"special writer parameter references: {len(set(special_ids))} unique ids")

covered_ids = edit_ids | set(special_ids)
uncovered_ids = sorted(id_set - covered_ids)
if uncovered_ids:
    fail("parameter ids have no writer route: " + ", ".join(uncovered_ids))
else:
    note(f"writer route coverage: {len(covered_ids)}/{len(id_set)} parameter ids")

for parameter_id in baseline.get("deferredParameterIds", []):
    if parameter_id not in id_set:
        fail(f"deferredParameterIds references unknown parameter id {parameter_id!r}")

csproj = CSPROJ.read_text(encoding="utf-8")
resource_names = re.findall(r'<EmbeddedResource\s+Include="([^"]+)"', csproj)
missing_resources = []
for rel in resource_names:
    path = CSPROJ.parent / rel.replace('\\', '/')
    if not path.exists():
        missing_resources.append(rel)
if missing_resources:
    fail("missing EmbeddedResource files: " + ", ".join(missing_resources))
else:
    note(f"embedded resources present: {len(resource_names)}")

# Every runtime production manifest named by convention must be embedded.
manifest_files = sorted(DATA.glob("*-redirect-manifest.csv"))
for path in manifest_files:
    if path.name not in csproj:
        fail(f"runtime redirect manifest is not embedded: {path.name}")
note(f"runtime redirect manifests present: {len(manifest_files)}")

# Known production-real but definition-missing generator SIDs are hard writer boundaries.
# They may be documented in audit data, but must never appear in runtime redirect manifests.
hard_writer_boundaries = {"DestructibleStash_Food_Drink_Newbie", "DestructibleStash_Med_Newbie"}
for manifest in manifest_files:
    text = manifest.read_text(encoding="utf-8")
    for sid in hard_writer_boundaries:
        if sid in text:
            fail(f"hard writer boundary {sid!r} appears in runtime manifest {manifest.name}")
note(f"hard writer boundaries protected: {len(hard_writer_boundaries)}")


# Destructible ammo production is a closed, exact control surface.
ammo_manifest = DATA / "destructible-ammo-production-redirect-manifest.csv"
if ammo_manifest.exists():
    import csv
    from collections import Counter
    with ammo_manifest.open(encoding="utf-8-sig", newline="") as fh:
        ammo_rows = list(csv.DictReader(fh))
    source_counts = Counter(row.get("source_value") for row in ammo_rows)
    expected_ammo_counts = {"DestructibleStash_AmmoSNG": 983, "DestructibleStash_AmmoNATO": 252}
    if len(ammo_rows) != 1235:
        fail(f"destructible ammo manifest has {len(ammo_rows)} rows; expected 1235")
    if source_counts != expected_ammo_counts:
        fail(f"destructible ammo manifest source split is {dict(source_counts)!r}; expected {expected_ammo_counts!r}")
    if len({row.get("physical_destructible_sid") for row in ammo_rows}) != 1235:
        fail("destructible ammo manifest does not contain 1235 unique physical actor SIDs")
    bad_context = [row for row in ammo_rows if row.get("player_rank") != "ERank::Newbie" or row.get("level_name") != "WorldMap_WP"]
    if bad_context:
        fail(f"destructible ammo manifest contains {len(bad_context)} row(s) outside the closed Newbie/WorldMap production contract")
    else:
        note("destructible ammo surface: 1235 unique redirects (983 SNG / 252 NATO), closed context")

# Destructible grenade production is a closed, exact control surface.
grenade_manifest = DATA / "destructible-grenade-production-redirect-manifest.csv"
if grenade_manifest.exists():
    import csv
    from collections import Counter
    with grenade_manifest.open(encoding="utf-8-sig", newline="") as fh:
        grenade_rows = list(csv.DictReader(fh))
    source_counts = Counter(row.get("source_value") for row in grenade_rows)
    expected_grenade_counts = {"DestructibleStash_F1": 101, "DestructibleStash_RGD": 81}
    if len(grenade_rows) != 182:
        fail(f"destructible grenade manifest has {len(grenade_rows)} rows; expected 182")
    if source_counts != expected_grenade_counts:
        fail(f"destructible grenade manifest source split is {dict(source_counts)!r}; expected {expected_grenade_counts!r}")
    if len({row.get("physical_destructible_sid") for row in grenade_rows}) != 182:
        fail("destructible grenade manifest does not contain 182 unique physical actor SIDs")
    bad_context = [row for row in grenade_rows if row.get("spawned_prototype_sid") != "D_WoodenAmmoCrate_01" or row.get("player_rank") != "ERank::Newbie" or row.get("level_name") != "WorldMap_WP"]
    if bad_context:
        fail(f"destructible grenade manifest contains {len(bad_context)} row(s) outside the closed wooden-crate/Newbie/WorldMap contract")
    else:
        note("destructible grenade surface: 182 unique redirects (101 F1 / 81 RGD), closed context")

# Human NPC medical production uses only repeatable generic-human redirects.
medical_manifest = DATA / "npc-medical-production-redirect-manifest.csv"
if medical_manifest.exists():
    import csv
    with medical_manifest.open(encoding="utf-8-sig", newline="") as fh:
        medical_rows = list(csv.DictReader(fh))
    if len(medical_rows) != 33:
        fail(f"human NPC medical manifest has {len(medical_rows)} rows; expected 33 repeatable redirects")
    bad_named = [row for row in medical_rows if (row.get("prototype") or "").startswith("GeneralNPC_MIK_")]
    bad_prologue = [row for row in medical_rows if "Prologue" in (row.get("prototype") or "")]
    pools = {row.get("target_value") for row in medical_rows}
    if bad_named:
        fail(f"human NPC medical manifest contains {len(bad_named)} named MIK/Mykluha redirect(s)")
    if bad_prologue:
        fail(f"human NPC medical manifest contains {len(bad_prologue)} Prologue redirect(s)")
    if len(pools) != 6:
        fail(f"human NPC medical manifest targets {len(pools)} TCD pools; expected 6")
    if not bad_named and not bad_prologue and len(medical_rows) == 33 and len(pools) == 6:
        note("human NPC medical surface: 33 repeatable redirects / 6 TCD pools; named MIK and Prologue excluded")


# Generator base-topology invariants. These counts come from the raw/local source
# catalogs and consumer graph, not from UI decisions. They guard against silently
# widening a production tuning surface when the topology is regenerated.
_base_inv = ROOT / "docs" / "generators" / "generator-local-source-inventory.csv"
_base_inh = ROOT / "docs" / "generators" / "generator-inheritance-edges.csv"
_base_comp = ROOT / "docs" / "generators" / "generator-composition-edges.csv"
_base_ctx = ROOT / "docs" / "generators" / "generator-consumer-contexts.csv"
_base_weapon_loadouts = ROOT / "docs" / "generators" / "npc-weapon-production-loadouts.csv"
_base_med_manifest = DATA / "npc-medical-production-redirect-manifest.csv"
if all(path.exists() for path in [_base_inv, _base_inh, _base_comp, _base_ctx, _base_weapon_loadouts, _base_med_manifest]):
    import csv as _csv_base
    from collections import Counter as _Counter_base
    def _rows_base(path):
        with path.open(encoding="utf-8-sig", newline="") as fh:
            return list(_csv_base.DictReader(fh))
    _inv_rows = _rows_base(_base_inv)
    _inh_rows = _rows_base(_base_inh)
    _comp_rows = _rows_base(_base_comp)
    _ctx_rows = _rows_base(_base_ctx)
    _weapon_loadouts = _rows_base(_base_weapon_loadouts)
    _med_rows_base = _rows_base(_base_med_manifest)
    _ctx_counts = _Counter_base(row["consumer_context"] for row in _ctx_rows)
    _std_roots = {row["generator_sid"] for row in _ctx_rows if row["consumer_context"] == "human_npc_standard"}
    _weapon_roots = {row["loadout_generator"] for row in _weapon_loadouts}
    _med_roots = {row["prototype"] for row in _med_rows_base}
    _base_failures = []
    if len(_inv_rows) != 3452: _base_failures.append(f"generator roots={len(_inv_rows)} expected 3452")
    if len(_inh_rows) != 2968: _base_failures.append(f"inheritance edges={len(_inh_rows)} expected 2968")
    if len(_comp_rows) != 5783: _base_failures.append(f"composition edges={len(_comp_rows)} expected 5783")
    if len(_ctx_rows) != 13647: _base_failures.append(f"consumer-context edges={len(_ctx_rows)} expected 13647")
    if _ctx_counts.get("human_npc_standard") != 139: _base_failures.append(f"human_npc_standard edges={_ctx_counts.get('human_npc_standard')} expected 139")
    if len(_std_roots) != 99: _base_failures.append(f"human_npc_standard roots={len(_std_roots)} expected 99")
    if len(_weapon_roots) != 33 or _weapon_roots != _med_roots:
        _base_failures.append(f"isolated human production roots weapon={len(_weapon_roots)} medical={len(_med_roots)} expected identical 33-root sets")
    if _base_failures:
        fail("generator base topology drift: " + "; ".join(_base_failures))
    else:
        note("generator base topology locked: 3452 roots / 2968 inheritance / 5783 composition / 13647 consumer edges; human standard 139 edges / 99 roots -> isolated production 33 roots")

# Archived 0.2.40 triage matrix and summary are not distributed anymore.
# Their absence must not be confused with a passing audit. Production
# generator surfaces remain checked against the embedded manifests below.
triage_csv = DOCS / "generators" / "control-candidate-triage.csv"
triage_summary = DOCS / "generators" / "control-candidate-triage-summary.json"
_triage_rows = []
if triage_csv.exists() != triage_summary.exists():
    fail("archived generator triage evidence is incomplete (CSV vs JSON)")
elif triage_csv.exists():
    _triage_rows = list(_csv.DictReader(triage_csv.open(encoding="utf-8-sig", newline="")))
    _triage_summary = json.loads(triage_summary.read_text(encoding="utf-8"))
    if _triage_summary.get("triage_concepts") != len(_triage_rows):
        fail("archived generator triage count mismatches summary")
else:
    not_run("archived generator triage CSV/summary not included; active manifests checked separately")

# GuardQuestItem is interaction infrastructure, not tunable loot.
junk_rows_path = ROOT / "docs" / "generators" / "npc-junk-guardquest-generator-rows.csv"
context_rows_path = ROOT / "docs" / "generators" / "guardquest-contextual-action-preconditions.csv"
if junk_rows_path.exists():
    with junk_rows_path.open(encoding="utf-8-sig", newline="") as fh:
        _junk_rows = list(_csv.DictReader(fh))
    _bad = [row for row in _junk_rows if row.get("Chance") != "1.0" or row.get("MinCount") != "1.0" or row.get("MaxCount") != "1.0"]
    if len(_junk_rows) != 44:
        fail(f"GuardQuestItem generator audit has {len(_junk_rows)} rows; expected 44")
    if _bad:
        fail(f"GuardQuestItem marker audit contains {len(_bad)} non-vanilla chance/count row(s)")
    if context_rows_path.exists():
        with context_rows_path.open(encoding="utf-8-sig", newline="") as fh:
            _ctx_rows = list(_csv.DictReader(fh))
        if len(_ctx_rows) != 40:
            fail(f"GuardQuestItem contextual-action audit has {len(_ctx_rows)} rows; expected 40")
        else:
            note("GuardQuestItem protected: 44 generator rows at Chance=1/count=1-1; 40 contextual-action consumers")

# Human NPC dropped-weapon condition runtime-proof boundary.
_nw_entries = ROOT / "docs" / "generators" / "npc-weapon-production-entries.csv"
_nw_excl = ROOT / "docs" / "generators" / "npc-weapon-production-exclusions.csv"
if _nw_entries.exists() and _nw_excl.exists():
    with _nw_entries.open(encoding="utf-8-sig", newline="") as fh:
        _nwr = list(_csv.DictReader(fh))
    with _nw_excl.open(encoding="utf-8-sig", newline="") as fh:
        _nwx = list(_csv.DictReader(fh))
    _roots = {r.get("generator_sid") for r in _nwr}
    _van = [r for r in _nwr if r.get("MinDurability") == "0.25" and r.get("MaxDurability") == "0.5"]
    _exc = [r for r in _nwr if not (r.get("MinDurability") == "0.25" and r.get("MaxDurability") == "0.5")]
    _pro = [r for r in _nwx if "PROLOGUE" in (r.get("reason") or "")]
    _mik = [r for r in _nwx if "MIK" in (r.get("loadout_generator") or "")]
    if len(_nwr) != 121 or len(_roots) != 33 or len(_van) != 120 or len(_exc) != 1 or len(_pro) != 2 or len(_mik) != 4:
        fail(f"NPC weapon condition runtime boundary drifted: rows={len(_nwr)}, roots={len(_roots)}, vanilla={len(_van)}, exceptions={len(_exc)}, Prologue={len(_pro)}, MIK={len(_mik)}; expected 121/33/120/1/2/4")
    else:
        note("NPC dropped-weapon condition runtime candidate locked: 121 rows / 33 roots; durability 120x0.25-0.50 + 1x0.35-0.45; exclusions 2 Prologue + 4 MIK")

# Human NPC NVG has no eligible repeatable generic-human source edge after hard exclusions.
nvg_links_path = ROOT / "docs" / "generators" / "npc-equipment-nested-links.csv"
if nvg_links_path.exists():
    with nvg_links_path.open(encoding="utf-8-sig", newline="") as fh:
        _links = list(_csv.DictReader(fh))
    _nvg = [r for r in _links if r.get("target_pool") in {"GeneralNPC_Freedom_NVG", "GeneralNPC_Neutral_NVG"}]
    _mik = [r for r in _nvg if (r.get("source_loadout") or "").startswith("GeneralNPC_MIK_") and (r.get("runtime_consumers") or "").startswith("GeneralNPC_MykluhaFreedom_")]
    _custom = [r for r in _nvg if r.get("source_loadout") == "GeneralNPC_Neutral_Sniper_ItemGeneratorCustom"]
    if len(_nvg) != 5 or len(_mik) != 4 or len(_custom) != 1:
        fail(f"human NPC NVG exclusion boundary drifted: total={len(_nvg)}, MIK={len(_mik)}, custom={len(_custom)}; expected 5/4/1")
    else:
        note("human NPC NVG closed: 5 audited edges = 4 named MIK/Mykluha + 1 custom/scripted; 0 repeatable edges")

# Medic stock quantity candidate is exactly the two isolated direct-item medic pools.
_medic_entries = ROOT / "docs" / "generators" / "trader-local-source-entries.csv"
if _medic_entries.exists():
    with _medic_entries.open(encoding="utf-8-sig", newline="") as fh:
        _mr = list(_csv.DictReader(fh))
    _surface = [r for r in _mr if r.get("sid") in {"MedicUniversal_TradeItemGenerator", "MedicNoon_TradeItemGenerator"} and r.get("ItemPrototypeSID")]
    _univ = [r for r in _surface if r.get("sid") == "MedicUniversal_TradeItemGenerator"]
    _noon = [r for r in _surface if r.get("sid") == "MedicNoon_TradeItemGenerator"]
    _bad = [r for r in _surface if not r.get("MinCount") or not r.get("MaxCount") or not r.get("Chance") or not r.get("PlayerRank")]
    if len(_surface) != 48 or len(_univ) != 25 or len(_noon) != 23 or _bad:
        fail(f"medic stock quantity boundary drifted: total={len(_surface)}, universal={len(_univ)}, noon={len(_noon)}, malformed={len(_bad)}; expected 48/25/23/0")
    else:
        note("medic stock quantity candidate: 48 direct count rows = 25 Universal + 23 Noon; Chance/PlayerRank preserved")


# Trader Stock is intentionally not a global scalar; keep aggregate decision closed.
if triage_csv.exists():
    _trader = [row for row in _triage_rows if row.get("control_id") == "economy.traderStock"]
    if len(_trader) != 1 or _trader[0].get("status") != "NO_UI_AGGREGATE_MIXED_SEMANTICS":
        fail("aggregate Trader Stock decision drifted; expected NO_UI_AGGREGATE_MIXED_SEMANTICS")
    else:
        note("aggregate Trader Stock closed: mixed Chance/Weight/count/payload/rank semantics; subdomain controls only")

# Trader ammo quantity is incomplete by one vanilla no-count AVOG row.
_ammo_surface = ROOT / "docs" / "generators" / "trader-ammo-stock-quantity-surface.csv"
if _ammo_surface.exists():
    with _ammo_surface.open(encoding="utf-8-sig", newline="") as fh:
        _ar = list(_csv.DictReader(fh))
    _counted = [r for r in _ar if r.get("has_count") == "yes"]
    _missing = [r for r in _ar if r.get("has_count") != "yes"]
    _nato = [r for r in _ar if r.get("root") == "Trader_NatoAmmo_ItemGenerator"]
    if len(_ar) != 52 or len(_counted) != 51 or len(_missing) != 1 or _missing[0].get("ItemPrototypeSID") != "AVOG" or len(_nato) != 10:
        fail(f"trader ammo quantity boundary drifted: total={len(_ar)}, counted={len(_counted)}, missing={len(_missing)}, NATO={len(_nato)}; expected 52/51/1(AVOG)/10")
    else:
        note("trader ammo quantity closed: 52 rows / 51 count-bearing / 1 AVOG no-count exception; NATO remains cross-context")

# Trader weapon stock has mixed quantity/availability models; no global scalar.
_tw_path = ROOT / "docs" / "generators" / "trader-local-source-entries.csv"
if _tw_path.exists():
    with _tw_path.open(encoding="utf-8-sig", newline="") as fh:
        _twr = list(_csv.DictReader(fh))
    _weapon_cats = {"EItemGenerationCategory::WeaponPrimary", "EItemGenerationCategory::WeaponSecondary", "EItemGenerationCategory::WeaponPistol"}
    _weapons = [r for r in _twr if r.get("category") in _weapon_cats]
    _counted = [r for r in _weapons if r.get("MinCount") == "1" and r.get("MaxCount") == "1"]
    _him = [r for r in _weapons if r.get("root") == "HimzavodTrader_ItemGenerator"]
    _core = [r for r in _weapons if r.get("root") in {"Trader_T1_Guns_ItemGenerator","Trader_T2_Guns_ItemGenerator","Trader_T3_Guns_ItemGenerator","Trader_T4_Guns_ItemGenerator"}]
    _decision = [r for r in _triage_rows if r.get("control_id") == "economy.traderWeaponStock"] if triage_csv.exists() else []
    if len(_weapons) != 94 or len(_counted) != 51 or len(_him) != 28 or len(_core) != 34 or len(_decision) != 1 or _decision[0].get("status") != "NO_UI_MIXED_AVAILABILITY_MODELS":
        fail(f"trader weapon stock boundary drifted: weapons={len(_weapons)}, counted={len(_counted)}, Himzavod={len(_him)}, core={len(_core)}, decision={len(_decision)}; expected 94/51/28/34/1")
    else:
        note("trader weapon stock closed: 94 rows; 51 explicit count, 43 no-count; Chance/tier-link/Weight models remain distinct")


# Trader attachment stock mixes quantity and availability semantics; no global scalar.
_ta_path = ROOT / "docs" / "generators" / "trader-local-source-entries.csv"
_tn_path = ROOT / "docs" / "generators" / "trader-nested-targets.csv"
if _ta_path.exists() and _tn_path.exists():
    with _ta_path.open(encoding="utf-8-sig", newline="") as fh:
        _tar = list(_csv.DictReader(fh))
    _attach = [r for r in _tar if r.get("category") == "EItemGenerationCategory::Attach" and r.get("ItemPrototypeSID")]
    _c11 = [r for r in _attach if r.get("MinCount") == "1" and r.get("MaxCount") == "1"]
    _c12 = [r for r in _attach if r.get("MinCount") == "1" and r.get("MaxCount") == "2"]
    _noc = [r for r in _attach if not r.get("MinCount") and not r.get("MaxCount")]
    with _tn_path.open(encoding="utf-8-sig", newline="") as fh:
        _tnr = list(_csv.DictReader(fh))
    _expected_links = {"Trader_Attachments_T2_ItemGenerator": 31, "Trader_Attachments_T3_ItemGenerator": 17, "Trader_Attachments_T4_ItemGenerator": 9}
    _links = {sid: int(next((r.get("trader_link_count") or 0 for r in _tnr if r.get("target_sid") == sid), 0)) for sid in _expected_links}
    _decision = [r for r in _triage_rows if r.get("control_id") == "economy.traderAttachmentStock"] if triage_csv.exists() else []
    if len(_attach) != 37 or len(_c11) != 29 or len(_c12) != 4 or len(_noc) != 4 or _links != _expected_links or len(_decision) != 1 or _decision[0].get("status") != "NO_UI_MIXED_AVAILABILITY_QUANTITY_SURFACE":
        fail(f"trader attachment stock boundary drifted: attach={len(_attach)}, 1-1={len(_c11)}, 1-2={len(_c12)}, no-count={len(_noc)}, links={_links}, decision={len(_decision)}")
    else:
        note("trader attachment stock closed: 37 direct rows (29x1-1, 4x1-2, 4 no-count); T2/T3/T4 links 31/17/9")

# Human NPC attached-weapon ammo runtime-proof boundary (v0.2.34).
_nwa_entries = ROOT / "docs" / "generators" / "npc-weapon-production-entries.csv"
_nwa_excl = ROOT / "docs" / "generators" / "npc-weapon-production-exclusions.csv"
_nwa_contract = ROOT / "docs" / "generators" / "HUMAN-NPC-WEAPON-AMMO-RUNTIME-TEST.md"
if _nwa_entries.exists() and _nwa_excl.exists():
    with _nwa_entries.open(encoding="utf-8-sig", newline="") as fh:
        _awar = list(_csv.DictReader(fh))
    with _nwa_excl.open(encoding="utf-8-sig", newline="") as fh:
        _awax = list(_csv.DictReader(fh))
    _awa_roots = {r.get("generator_sid") for r in _awar}
    _awa_zero = [r for r in _awar if r.get("AmmoMinCount") == "0.0"]
    _awa_pos = [r for r in _awar if r.get("AmmoMinCount") not in {None, "", "0.0"}]
    _awa_normal = [r for r in _awar if r.get("ammo_max_field") == "AmmoMaxCount"]
    _awa_lower = [r for r in _awar if r.get("ammo_max_field") == "AmmoMaxcount"]
    _awa_missing = [r for r in _awar if not r.get("AmmoMinCount") or not r.get("ammo_max")]
    _awa_pro = [r for r in _awax if "PROLOGUE" in (r.get("reason") or "")]
    _awa_mik = [r for r in _awax if "MIK" in (r.get("loadout_generator") or "")]
    if len(_awar) != 121 or len(_awa_roots) != 33 or len(_awa_zero) != 81 or len(_awa_pos) != 40 or len(_awa_normal) != 118 or len(_awa_lower) != 3 or _awa_missing or len(_awa_pro) != 2 or len(_awa_mik) != 4 or not _nwa_contract.exists():
        fail(f"NPC weapon ammo runtime boundary drifted: rows={len(_awar)}, roots={len(_awa_roots)}, zeroMin={len(_awa_zero)}, positiveMin={len(_awa_pos)}, AmmoMaxCount={len(_awa_normal)}, AmmoMaxcount={len(_awa_lower)}, missing={len(_awa_missing)}, Prologue={len(_awa_pro)}, MIK={len(_awa_mik)}, contract={_nwa_contract.exists()}; expected 121/33/81/40/118/3/0/2/4/true")
    else:
        note("NPC weapon-ammo runtime candidate locked: 121 rows / 33 roots; 81 zero-min + 40 positive-min; max-key spelling 118+3; A/B contract 1-1 vs 77-77")

# Medic Stock Quantity runtime-proof boundary (v0.2.35).
_ms_entries = ROOT / "docs" / "generators" / "generator-local-source-item-entries.csv"
_ms_contract = ROOT / "docs" / "generators" / "MEDIC-STOCK-QUANTITY-RUNTIME-TEST.md"
if _ms_entries.exists():
    with _ms_entries.open(encoding="utf-8-sig", newline="") as fh:
        _ms_all = list(_csv.DictReader(fh))
    _ms = [r for r in _ms_all if r.get("sid") in {"MedicUniversal_TradeItemGenerator", "MedicNoon_TradeItemGenerator"} and r.get("ItemPrototypeSID")]
    _ms_u = [r for r in _ms if r.get("sid") == "MedicUniversal_TradeItemGenerator"]
    _ms_n = [r for r in _ms if r.get("sid") == "MedicNoon_TradeItemGenerator"]
    _ms_c1 = [r for r in _ms if r.get("Chance") in {"1", "1.0"}]
    _ms_sub = [r for r in _ms if r.get("Chance") not in {"1", "1.0"}]
    _ms_missing = [r for r in _ms if not r.get("MinCount") or not r.get("MaxCount")]
    _ms_dec = [r for r in _triage_rows if r.get("control_id") == "economy.medicStock"] if triage_csv.exists() else []
    if len(_ms) != 48 or len(_ms_u) != 25 or len(_ms_n) != 23 or len(_ms_c1) != 42 or len(_ms_sub) != 6 or _ms_missing or len(_ms_dec) != 1 or _ms_dec[0].get("status") != "STRUCTURALLY_READY_RUNTIME_PROOF" or not _ms_contract.exists():
        fail(f"medic stock runtime boundary drifted: rows={len(_ms)}, Universal={len(_ms_u)}, Noon={len(_ms_n)}, Chance1={len(_ms_c1)}, sub1={len(_ms_sub)}, missingCount={len(_ms_missing)}, decision={len(_ms_dec)}, contract={_ms_contract.exists()}; expected 48/25/23/42/6/0/1/true")
    else:
        note("medic stock runtime candidate locked: 48 count rows = 25 Universal + 23 Noon; 42 Chance=1 + 6 rarity rows; A/B contract 1-1 vs 20-20")

# Former isolated runtime-test generator/packer was removed from the current
# repository. Do not claim its historical evidence was re-run or silently
# require its obsolete scripts for modern production source validation.
_rt_old_files = [
    ROOT / "tools/runtime_tests/generate_runtime_test_patches.py",
    ROOT / "tools/runtime_tests/build_runtime_test_mod.py",
]
if any(path.exists() for path in _rt_old_files):
    note("[HISTORICAL] deprecated runtime test scripts remain available; no gameplay execution inferred")
else:
    not_run("old 0.2.36/0.2.37 test generator/packer not shipped; run current CFG/runtime acceptance independently")

# Current source/production boundary: NPC corpse bonus ammo was migrated
# away from 242 weapon-attached AmmoMin/Max direct edits into the dedicated
# NPC_Ammo_Smart clone route. The ordinary weapon attached ammo is untouched.
_prod_expected_direct = {
    "loot.droppedWeaponCondition": (242, {"MinDurability", "MaxDurability"}),
    "economy.medicStock": (96, {"MinCount", "MaxCount"}),
}
for _pid, (_count, _keys) in _prod_expected_direct.items():
    _pts = [p for p in baseline.get("editPoints", []) if p.get("parameterId") == _pid]
    if len(_pts) != _count or not {p.get("key") for p in _pts} <= _keys:
        fail(f"{_pid} current direct-production contract drifted: {len(_pts)} edit points")
_npc_ammo_direct = [p for p in baseline.get("editPoints", []) if p.get("parameterId") == "loot.npcWeaponAmmoSupply"]
_npc_ammo_special = baseline.get("special", {})
_npc_ammo_writer = (ROOT / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
if _npc_ammo_direct:
    fail("NPC corpse SmartLoot ammo unexpectedly returned to weapon-attached direct edits")
elif "loot.npcWeaponAmmoSupply" not in json.dumps(_npc_ammo_special) or "NPC_Ammo_Smart" not in _npc_ammo_writer:
    fail("NPC_Ammo_Smart corpse-bonus special writer route not found")
else:
    note("current NPC Ammo route: zero weapon-attached direct edits, source-cloned SmartLoot ammo; condition 242 and medic 96 direct edits preserved")

# Non-generator normal-UI coverage gate (v0.2.43).
_non_gen_expected = {
    "combat": (19, 14, 5),
    "health": (13, 10, 3),
    "stamina": (7, 6, 1),
    "survival": (32, 30, 2),
    "economy": (17, 12, 5),
    "equipment": (4, 3, 1),
}
_non_gen_doc = DOCS / "NON-GENERATOR-CONTROL-VERIFICATION.md"
_non_gen_actual = {}
for _tab, _expected in _non_gen_expected.items():
    _rows = [p for p in parameters if p.get("tab") == _tab and p.get("exposeInUi", True)]
    _non_gen_actual[_tab] = (
        len(_rows),
        sum(p.get("support", {}).get("status") == "confirmed" for p in _rows),
        sum(p.get("support", {}).get("status") == "runtime_test_required" for p in _rows),
    )
_hidden_contract = {
    "stamina.sprintDisableThreshold", "stamina.sprintRecoveryDelay", "stamina.lowRecoveryThreshold",
    "stamina.lowRecoveryStrength", "stamina.lowRecoveryDuration", "stamina.sprintUnderRunSpeedRegenCoef",
    "stamina.joggingRegenCoef", "stamina.slowRunRegenCoef", "stamina.sprintRegenCoef", "stamina.runRegenCoef",
    "economy.repairDifficultyCost",
}
_hidden_bad = sorted(
    p.get("id") for p in parameters
    if p.get("id") in _hidden_contract and p.get("exposeInUi", True)
)
if _non_gen_actual != _non_gen_expected:
    fail(f"non-generator normal-UI coverage drifted: {_non_gen_actual}; expected {_non_gen_expected}")
elif _hidden_bad:
    fail(f"audit-only controls leaked into normal UI: {_hidden_bad}")
elif not _non_gen_doc.exists():
    fail("non-generator verification document missing from docs/")
else:
    note("non-generator UI coverage: 92 visible anchors across Combat/Health/Stamina/Survival/Economy/Equipment; current grouped editors and 32 Survival controls accounted")

# Combat deep-audit release gate (v0.2.44).
_combat_doc = DOCS / "COMBAT-CONTROL-DEEP-AUDIT.md"
_combat_rows = {p.get("id"): p for p in parameters if p.get("tab") == "combat" and p.get("exposeInUi", True)}
_combat_runtime_expected = {
    "combat.mutantCooldown.bloodsucker",
    "combat.psySpawnCycleDelay", "combat.psyPhantomCount", "combat.psyBleeding", "combat.psyDurabilityDamage",
}
_combat_runtime_actual = {pid for pid, row in _combat_rows.items() if row.get("support", {}).get("status") == "runtime_test_required"}
_combat_inverse = {
    pid: [t.get("operation") for t in row.get("targets", [])]
    for pid, row in _combat_rows.items() if pid in {"combat.npcAttackCooldown", "combat.mutantAttackCooldown"}
}
_headshot = _combat_rows.get("combat.playerHeadshotMultiplier", {})
_repeated = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "repeatedHitProtectionRange"), {})
if len(_combat_rows) != 19 or _combat_runtime_actual != _combat_runtime_expected:
    fail(f"combat deep-audit visible surface drifted: controls={len(_combat_rows)}, runtime={sorted(_combat_runtime_actual)}")
elif any(ops != ["inverseScale"] for ops in _combat_inverse.values()):
    fail(f"combat attack-frequency inverseScale contract drifted: {_combat_inverse}")
elif _repeated.get("order") != "nondecreasing" or _repeated.get("parameterIds") != ["combat.accumulatedDamageWeightMin", "combat.accumulatedDamageWeightMax"]:
    fail("repeated-hit Min<=Max CFG-value constraint drifted")
elif "extra damage" in str(_headshot.get("description", "")).lower() or "1.0x" not in str(_headshot.get("logic", "")):
    fail("headshot multiplier wording regressed to bonus/extra-damage semantics")
elif not _combat_doc.exists():
    fail("combat deep-audit document missing from docs/")
else:
    note("combat deep audit locked: 19 visible controls / 14 confirmed direct + 5 runtime-gated; Mutant Health and Mutant Special Attack Cooldown aggregated separately")

# Generator-backed UI end-to-end release gate (v0.2.42).
_e2e_doc = DOCS / "GENERATOR-CONTROL-END-TO-END-VERIFICATION.md"
_e2e_static = {
    "loot.npcCorpseBonus", "loot.expensiveStashMoneyChance",
    "loot.mutant.blindDogChance", "loot.mutant.bloodsuckerChance", "loot.mutant.boarChance",
    "loot.mutant.burerChance", "loot.mutant.catChance", "loot.mutant.chimeraChance",
    "loot.mutant.controllerChance", "loot.mutant.deerChance", "loot.mutant.fleshChance",
    "loot.mutant.poltergeistChance", "loot.mutant.pseudodogChance", "loot.mutant.pseudogiantChance",
    "loot.mutant.tushkanChance", "loot.mutant.snorkChance",
}
_e2e_runtime = {
    "loot.npcMedicalChance", "loot.cheapStashAmmoQuantity",
    "loot.cheapStashGrenadeChance", "loot.cheapStashGrenadeQuantity", "loot.mediumStashAmmoQuantity", "loot.destructibleAmmoQuantity",
    "loot.destructibleGrenadeQuantity", "loot.expensiveStashAmmoQuantity",
    "loot.droppedWeaponCondition", "loot.npcWeaponAmmoSupply", "economy.medicStock",
    "loot.blueprintShuffle", "loot.worldWeaponDensity", "loot.worldAttachmentShuffle", "loot.worldDetectors",
    "loot.ammoBoxWeight918",
}
_e2e_all = _e2e_static | _e2e_runtime
_catalog_ids = {p.get("id") for p in parameters}
_missing_e2e = sorted(_e2e_all - _catalog_ids)
if len(_e2e_all) != 32 or len(_e2e_static) != 16 or len(_e2e_runtime) != 16 or _missing_e2e:
    fail(f"generator UI E2E gate drifted: total={len(_e2e_all)}, static={len(_e2e_static)}, runtime={len(_e2e_runtime)}, missing={_missing_e2e}; expected 32/16/16/0")
elif not _e2e_doc.exists():
    fail("generator UI E2E verification document missing from docs/")
else:
    _bayun = next((p for p in parameters if p.get("id") == "loot.mutant.catChance"), None)
    if not _bayun or _bayun.get("label") != "Bayun Part Drop Chance":
        fail("Bayun compatibility contract drifted: legacy loot.mutant.catChance must retain player-facing Bayun label")
    else:
        note("generator UI E2E gate: 32 controls = 16 static 1:1 + 16 runtime-test-required; 0 invalid/partial; Ammo Box Distribution + Stash Ammo Amounts + Mutant Part Drop Chances aggregate radars included")



# World weapon unique-density distribution release gate (v0.9.26.44).
_world_weapon_pool = DATA / "world-weapon-shuffle-pool.csv"
_world_weapon_direct = DATA / "world-weapon-direct-manifest.csv"
_world_weapon_generator = DATA / "world-weapon-shuffle-manifest.csv"
if not (_world_weapon_pool.exists() and _world_weapon_direct.exists() and _world_weapon_generator.exists()):
    fail("world weapon distribution manifests are incomplete")
else:
    import csv
    with _world_weapon_pool.open(encoding="utf-8", newline="") as f:
        _ww_pool_rows = list(csv.DictReader(f))
    with _world_weapon_direct.open(encoding="utf-8", newline="") as f:
        _ww_direct_rows = list(csv.DictReader(f))
    with _world_weapon_generator.open(encoding="utf-8", newline="") as f:
        _ww_generator_rows = list(csv.DictReader(f))

    _ww_pool_sids = {r["item_sid"] for r in _ww_pool_rows}
    if len(_ww_pool_rows) != 36 or len(_ww_pool_sids) != 36:
        fail(f"world weapon pool must contain 36 unique regular models; got {len(_ww_pool_rows)} rows / {len(_ww_pool_sids)} unique")
    if any(not r.get("tier") for r in _ww_pool_rows):
        fail("world weapon pool contains an unmapped progression tier")

    _ww_base = [r for r in _ww_direct_rows if r["scope"] == "baseGame"]
    _ww_dlc = [r for r in _ww_direct_rows if r["scope"] == "dlc1"]
    if len(_ww_direct_rows) != 362 or len(_ww_base) != 282 or len(_ww_dlc) != 80:
        fail(f"world weapon direct manifest drifted: total/base/dlc={len(_ww_direct_rows)}/{len(_ww_base)}/{len(_ww_dlc)}; expected 362/282/80")
    if len({(r["scope"], r["actor_sid"]) for r in _ww_direct_rows}) != 362:
        fail("world weapon direct manifest actor targets are not unique")
    if any(r["item_sid"] not in _ww_pool_sids for r in _ww_direct_rows):
        fail("world weapon direct manifest contains item SIDs outside the 36-model regular pool")

    if len(_ww_generator_rows) != 14:
        fail(f"world weapon generator manifest drifted: {len(_ww_generator_rows)}; expected 14")
    if any(r["item_sid"] not in _ww_pool_sids for r in _ww_generator_rows):
        fail("world weapon generator manifest contains item SIDs outside the 36-model regular pool")

    _ww_all_base_sids = {r["item_sid"] for r in _ww_base} | {r["item_sid"] for r in _ww_generator_rows}
    if _ww_all_base_sids != _ww_pool_sids:
        fail("base world weapon locations do not cover every one of the 36 regular weapon models")

    note("world weapon distribution locked: 36 models; 296 base / 376 DLC1 safe locations; compatible redistribution with guaranteed minimum copies")


# World weapon compatibility-density semantic contract (v0.9.26.44).
_ww_writer_text = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
_ww_vm_text = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
if 'string.Equals(family, "Pistol"' not in _ww_writer_text:
    fail("world weapon compatibility no longer separates pistol and primary slots")
elif "Math.Abs(TierNumber(weapon.Tier) - TierNumber(location.Tier)) <= 1" not in _ww_writer_text:
    fail("world weapon compatibility no longer enforces +/-1 progression tier")
elif "var maximumGuaranteedCopies = hasDlc1 ? 8 : 6;" not in _ww_writer_text:
    fail("world weapon guaranteed-copy topology cap drifted from base=6 / DLC1=8")
elif "var requiredFlow = weaponCount * guaranteedCopies;" not in _ww_writer_text:
    fail("world weapon max-flow mandatory allocation is missing")
elif "underGuaranteed.Count > 0" not in _ww_writer_text:
    fail("world weapon post-allocation minimum-copy verification is missing")
elif '1 => "once"' not in _ww_vm_text or '2 => "twice"' not in _ww_vm_text:
    fail("world weapon live helper no longer renders once/twice copy wording")
else:
    note("world weapon density semantics locked: compatible pistol/primary +/-1 tier redistribution; live once/twice/N-times guarantee; caps base=6/DLC1=8")


# Historical evidence (not shipped in current repo) - never report as verified.
not_run("world attachment progression-tier snapshot: archived CSV/patch snapshot absent; current active catalog/manifest contracts checked elsewhere")

# World attachment shuffle production gate (v0.9.26.44).
_wa_pool = DATA / "world-attachment-shuffle-pool.csv"
_wa_direct = DATA / "world-attachment-direct-manifest.csv"
_wa_generator = DATA / "world-attachment-generator-manifest.csv"
if not (_wa_pool.exists() and _wa_direct.exists() and _wa_generator.exists()):
    fail("world attachment production manifests are incomplete")
else:
    import csv as _wa_csv2
    with _wa_pool.open(encoding="utf-8", newline="") as _f:
        _wa_pool_rows = list(_wa_csv2.DictReader(_f))
    with _wa_direct.open(encoding="utf-8", newline="") as _f:
        _wa_direct_rows = list(_wa_csv2.DictReader(_f))
    with _wa_generator.open(encoding="utf-8", newline="") as _f:
        _wa_generator_rows = list(_wa_csv2.DictReader(_f))

    _wa_pool_ids = {r["item_sid"] for r in _wa_pool_rows}
    _wa_surface_ids = {r["item_sid"] for r in _wa_direct_rows} | {r["item_sid"] for r in _wa_generator_rows}
    _wa_categories = {r["category"] for r in _wa_pool_rows}
    _wa_tiers2 = {r["tier"] for r in _wa_pool_rows}
    _wa_clone_ids = {r["clone_sid"] for r in _wa_generator_rows}

    if len(_wa_pool_rows) != 39 or len(_wa_pool_ids) != 39:
        fail(f"world attachment live pool drifted: {len(_wa_pool_rows)} rows / {len(_wa_pool_ids)} unique; expected 39/39")
    elif len(_wa_direct_rows) != 7 or len(_wa_generator_rows) != 60:
        fail(f"world attachment live surface drifted: direct={len(_wa_direct_rows)} generator={len(_wa_generator_rows)}; expected 7/60")
    elif len(_wa_clone_ids) != 55:
        fail(f"world attachment clone surface drifted: {len(_wa_clone_ids)}; expected 55")
    elif _wa_pool_ids != _wa_surface_ids:
        fail("world attachment live pool does not exactly match the full audited placement multiset model set")
    elif _wa_categories != {"Magazine", "Optic", "Suppressor", "Underbarrel"}:
        fail(f"world attachment category boundary drifted: {_wa_categories}")
    elif not _wa_tiers2 <= {"T2", "T3", "T4"}:
        fail(f"world attachment live pool contains unexpected tiers: {_wa_tiers2}")
    else:
        note("world attachment shuffle locked: 39 models / 67 DLC1-inclusive physical slots = 7 direct + 60 generator entries; 55 isolated clone groups; exact multiset preserved")


# World attachment writer semantic contract (v0.9.26.44).
_wa_writer_text = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
if 'const string parameterId = "loot.worldAttachmentShuffle";' not in _wa_writer_text:
    fail("world attachment production writer is missing")
elif "sourceCounts.Any" not in _wa_writer_text or "assignedCounts" not in _wa_writer_text:
    fail("world attachment writer no longer verifies exact source multiset preservation")
elif "Math.Abs(TierNumber(item.Tier) - TierNumber(slotTier)) <= 1" not in _wa_writer_text:
    fail("world attachment writer no longer enforces +/-1 progression tier")
elif "string.Equals(item.Category, slotCategory" not in _wa_writer_text:
    fail("world attachment writer no longer enforces hard category equality")
elif "../ItemGeneratorPrototypes.cfg" not in _wa_writer_text or "GetOrAddReferencedRoot" not in _wa_writer_text:
    fail("world attachment physical clone-and-redirect writer is missing")
elif '"SpawnedPrototypeSID", assigned' not in _wa_writer_text or '"ItemSID", assigned' not in _wa_writer_text:
    fail("world attachment direct pickup writer no longer patches both item identity fields")
else:
    note("world attachment writer semantics locked: exact multiset + category equality + +/-1 tier + 55 physical clone groups + dual-SID direct pickup replacement")


# Historical evidence (not shipped in current repo) - never report as verified.
not_run("loose detector provenance snapshot: archived CSV/patch snapshot absent; current active catalog/manifest contracts checked elsewhere")

# World detector spawn toggle production gate (v0.9.26.44).
_wd_direct = DATA / "world-detector-direct-manifest.csv"
_wd_stash = DATA / "world-detector-stash-manifest.csv"
if not _wd_direct.exists() or not _wd_stash.exists():
    fail("world detector production manifests are missing")
else:
    import csv as _wd_csv2
    with _wd_direct.open(encoding="utf-8", newline="") as _f:
        _wd_direct_rows = list(_wd_csv2.DictReader(_f))
    with _wd_stash.open(encoding="utf-8", newline="") as _f:
        _wd_stash_rows = list(_wd_csv2.DictReader(_f))
    _wd_base = [r for r in _wd_direct_rows if r["scope"] == "base"]
    _wd_dlc = [r for r in _wd_direct_rows if r["scope"] == "dlc1"]
    _wd_items = {}
    for _r in _wd_direct_rows:
        _wd_items[_r["item_sid"]] = _wd_items.get(_r["item_sid"], 0) + 1
    if len(_wd_direct_rows) != 7 or len(_wd_base) != 4 or len(_wd_dlc) != 3:
        fail(f"world detector direct production surface drifted: total/base/dlc={len(_wd_direct_rows)}/{len(_wd_base)}/{len(_wd_dlc)}; expected 7/4/3")
    elif _wd_items != {"Echo": 2, "Gilka": 5}:
        fail(f"world detector direct multiset drifted: {_wd_items}; expected Echo=2/Gilka=5")
    elif len(_wd_stash_rows) != 1 or _wd_stash_rows[0].get("item_sid") != "Bear" or _wd_stash_rows[0].get("scope") != "dlc1":
        fail("world detector stash surface must be the single active DLC1 X16 Bear entry")
    else:
        note("world detector toggle locked: OFF vanilla / ON removes 4 base direct or 7 direct + 1 X16 stash Bear with DLC1")







# Deep CFG audit 2026-10-09: 14 real mutant health scalers, including
# the six missing baseline rows now verified against the base ObjPrototypes
# decoded CFG. Only eight of these mutant families have cooldown editors.
_mhp_expected = {
    "combat.mutantHealth.bayun": {"Bayun":"400"},
    "combat.mutantHealth.blinddog": {"Blinddog":"63"},
    "combat.mutantHealth.bloodsucker": {"Bloodsucker":"500","MistBloodsucker":"1000"},
    "combat.mutantHealth.boar": {"Boar":"300"},
    "combat.mutantHealth.burer": {"Burer":"400","Burer_ShootingSpecial":"400"},
    "combat.mutantHealth.chimera": {"Chimera":"1400"},
    "combat.mutantHealth.controller": {"Controller":"500"},
    "combat.mutantHealth.deer": {"Deer":"600"},
    "combat.mutantHealth.flesh": {"Flesh":"280"},
    "combat.mutantHealth.poltergeist": {"Poltergeist":"400"},
    "combat.mutantHealth.pseudodog": {"PseudoDog":"300"},
    "combat.mutantHealth.pseudogiant": {"Pseudogiant":"2500"},
    "combat.mutantHealth.snork": {"Snork":"150"},
    "combat.mutantHealth.tushkan": {"Tushkan":"19"},
}
_mhp_rows_by_id = {row.get("id"): row for row in parameters}
_mhp_points = [
    p for p in baseline.get("editPoints", [])
    if p.get("parameterId", "").startswith("combat.mutantHealth.")
]
_mhp_actual = {}
for _point in _mhp_points:
    _mhp_actual.setdefault(_point["parameterId"], {})[_point["prototype"]] = _point["sourceRaw"]

if _mhp_actual != _mhp_expected or len(_mhp_points) != 16:
    fail(f"mutant health concrete CFG mappings drifted: entries={len(_mhp_points)} actual={_mhp_actual}")
for _pid, _expected_protos in _mhp_expected.items():
    _row = _mhp_rows_by_id.get(_pid)
    if not _row or _row.get("tab") != "combat" or _row.get("group") != "Mutant Health":
        fail(f"mutant health source missing/misplaced: {_pid}")
    if _row.get("control") != "slider" or _row.get("support", {}).get("status") != "confirmed":
        fail(f"mutant health status/control drifted: {_pid}")
    _slider = _row.get("slider", {})
    if (_slider.get("min"), _slider.get("max"), _slider.get("step")) != (0.05, 2, 0.05):
        fail(f"mutant health native UI bounds/step drifted: {_pid}")
    if (_row.get("vanilla") or {}).get("All") != 1:
        fail(f"mutant health vanilla multiplier drifted: {_pid}")
    _expected_targets = {
        (x["prototype"], x["path"], x["operation"])
        for x in _row.get("targets", [])
    }
    if _expected_targets != {(sid, "VitalParams.MaxHP", "scale") for sid in _expected_protos}:
        fail(f"mutant health catalog target-to-writer mismatch: {_pid}")
if any(p.get("scope") != "baseGame" or p.get("file") != "GameData/ObjPrototypes.cfg"
       or p.get("nodePath") != ["VitalParams"] or p.get("key") != "MaxHP" or p.get("operation") != "scale"
       for p in _mhp_points):
    fail("mutant health writer route must remain direct base-game ObjPrototypes MaxHP scale")
if _mhp_rows_by_id["combat.mutantHealth.bloodsucker"].get("label") != "Mutant Health":
    fail("mutant health aggregate anchor label drifted")
_mhp_cooldown_species = ("bloodsucker", "chimera", "deer", "pseudogiant",
                         "pseudodog", "burer", "bayun", "controller")
for _species in _mhp_cooldown_species:
    _cid = f"combat.mutantCooldown.{_species}"
    _crow = _mhp_rows_by_id.get(_cid)
    if not _crow or _crow.get("group") != "Mutant Combat":
        fail(f"missing or moved cooldown source: {_cid}")
    if _species == "bloodsucker" and _crow.get("label") != "Mutant Special Attack Cooldown":
        fail("mutant cooldown anchor label drifted")
note("deep CFG mutant HP route gate: 14 health multipliers / 16 MaxHP edit points; 8 separate cooldown families")

# The MutantHealthRadar is a TWO-COLUMN, 14-species current-health editor,
# not the historical eight-axis polar control. All 14 source ID + HP pairs
# must remain present in the renderer and live backing parameter graph.
_mhr_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "MutantHealthRadar.cs"
_mhr_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_mhr_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_mhr_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
if not _mhr_control.exists():
    fail("MutantHealthRadar.cs is missing")
else:
    _mhr_text = _mhr_control.read_text(encoding="utf-8")
    for _id, _values in _mhp_expected.items():
        if f'new("{_id}",' not in _mhr_text:
            fail(f"Mutant Health roster renderer missing source ID {_id}")
        if f'"{_id}"' not in _mhr_main_vm:
            fail(f"Mutant Health source not loaded in MainViewModel: {_id}")
    if _mhr_text.count('new("combat.mutantHealth.') != 14:
        fail("MutantHealthRadar must show exactly 14 real mutant groups")
    if "GlobalHpMaximum = 7500.0" not in _mhr_text or "DesiredHeight = 526.0" not in _mhr_text:
        fail("MutantHealthRadar compact 2-column geometry drifted")
    if "parameter.CurrentNumber" not in _mhr_text or "parameter.ReferenceNumber" not in _mhr_text:
        fail("MutantHealthRadar lost native live/reference source comparisons")
    if '<controls:MutantHealthRadar x:Name="MutantHealthRadarControl"' not in _mhr_xaml:
        fail("MainWindow does not render Mutant Health roster")
    if "mutantHealthParameterIds.Contains(p.Id)" not in _mhr_main_vm:
        fail("hidden mutant health backings are not loaded into the live VM graph")
    if "IsMutantHealthAnchor" not in _mhr_param_vm or "IsMutantHealthMember" not in _mhr_param_vm:
        fail("mutant health aggregate/backing visibility contract missing")
    note("Mutant Health compact roster: 14 confirmed, source backed 0.05–2x at 0.05x steps; authored MaxHP references validated")

_sac_ids = ["combat.mutantCooldown." + family for family in (
    "bloodsucker", "chimera", "deer", "pseudogiant", "pseudodog", "burer", "bayun", "controller"
)]
_sac_rows_by_id = {p.get("id"): p for p in parameters}
_sac_src_path = ROOT / "src/TrueCustomDifficulty/Controls/SpecialAbilityCooldownRadar.cs"
_sac_errors = []
for _pid in _sac_ids:
    _row = _sac_rows_by_id.get(_pid)
    if not _row:
        _sac_errors.append(f"missing {_pid}")
        continue
    _slider, _vanilla = _row.get("slider") or {}, _row.get("vanilla") or {}
    if _row.get("group") != "Mutant Combat" or _row.get("control") != "slider":
        _sac_errors.append(f"{_pid}: group/control changed")
    if (_slider.get("min"), _slider.get("max"), _slider.get("step")) != (1.0 / 3.0, 2, 0.25):
        _sac_errors.append(f"{_pid}: native wait limits must map 0.5-3x recharge / visible 0.25x rate ticks")
    if any(_vanilla.get(mode) != value for mode, value in
           (("Novice", 1), ("Stalker", 1), ("Veteran", 1), ("Master", 0.5))):
        _sac_errors.append(f"{_pid}: Base Preset reference changed")
if not _sac_src_path.exists():
    _sac_errors.append("SpecialAbilityCooldownRadar.cs missing")
else:
    _sac_src = _sac_src_path.read_text(encoding="utf-8")
    for _pid in _sac_ids:
        if f'new("{_pid}",' not in _sac_src:
            _sac_errors.append(f"{_pid}: absent from current renderer")
    if 'new("combat.mutantAttackCooldown",' not in _sac_src or 'new("combat.mutantDamage",' not in _sac_src:
        _sac_errors.append("current Mutant Combat frequency/damage axes missing")
    if ("selected.TickFrequency" not in _sac_src
            or "var rate = RechargeRate(selected.CurrentNumber)" not in _sac_src
            or "parameter.CurrentNumber = raw;" not in _sac_src):
        _sac_errors.append("0.25x recharge stepping must use inverse-rate keyboard and mouse controls")
_sac_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
if "specialAbilityCooldownParameterIds.Contains(p.Id)" not in _sac_vm:
    _sac_errors.append("Mutant Combat backing IDs missing from live VM")
if _sac_errors:
    fail("Mutant Combat cooldown contract: " + "; ".join(_sac_errors))
else:
    note("Mutant Combat cooldown: 8 real families, 0.25x displayed recharge steps (0.5-3x), N/S/V=1x Master=2x recharge; in-game runtime still unverified")

# Mutant health audit gate (v0.9.26.44).
_mh_path = DOCS / "combat" / "mutant-health-audit.csv"
_mh_doc = DOCS / "combat" / "MUTANT-HEALTH-AUDIT.md"
if not (_mh_path.exists() and _mh_doc.exists()):
    fail("mutant health audit evidence missing")
else:
    import csv as _mh_csv
    with _mh_path.open(encoding="utf-8", newline="") as _f:
        _mh_rows = list(_mh_csv.DictReader(_f))
    _mh_expected = {
        "BlindDog":"63","Bloodsucker":"500","Boar":"300","Burer":"400","Bayun":"400",
        "Chimera":"1400","Controller":"500","Deer":"600","Flesh":"280","Poltergeist":"400",
        "Pseudodog":"300","Pseudogiant":"2500","Snork":"150","Tushkan":"19",
    }
    _mh_actual = {r["player_name"]: r["max_hp"] for r in _mh_rows}
    if _mh_actual != _mh_expected:
        fail(f"mutant MaxHP audit drifted: {_mh_actual}")
    elif any(r["difficulty_scoped"] != "no" for r in _mh_rows):
        fail("mutant MaxHP audit incorrectly claims difficulty-scoped health")
    else:
        note("mutant health audit locked: 14 canonical mutant MaxHP values; direct per-mutant health confirmed; no native difficulty-scoped mutant HP field")

# Historical evidence (not shipped in current repo) - never report as verified.
not_run("regular ammo caliber source snapshot: archived CSV/patch snapshot absent; current active catalog/manifest contracts checked elsewhere")

# Current specialized WPF controls use native card/track geometry; filenames
# retained for WPF/XAML compatibility are not evidence of polar layout.
def verify_current_editor(label, class_name, parameter_ids, vm_marker, group_markers=()):
    path = ROOT / "src/TrueCustomDifficulty/Controls" / (class_name + ".cs")
    xaml = (ROOT / "src/TrueCustomDifficulty/MainWindow.xaml").read_text(encoding="utf-8")
    main_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
    param_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
    catalog = {p.get("id"): p for p in parameters}
    problems = []
    if not path.exists():
        problems.append(f"missing {class_name}.cs")
    else:
        source = path.read_text(encoding="utf-8")
        for pid in parameter_ids:
            if f'"{pid}"' not in source:
                problems.append(f"{pid}: missing from {class_name}")
        for gesture in ("OnMouseLeftButtonDown(", "OnMouseRightButtonDown(", "OnKeyDown("):
            if gesture not in source:
                problems.append(f"{class_name}: native {gesture} missing")
    for pid in parameter_ids:
        if pid not in catalog:
            problems.append(f"missing catalog parameter {pid}")
    if f'<controls:{class_name} x:Name="{class_name}Control"' not in xaml:
        problems.append(f"MainWindow missing {class_name} registration")
    if vm_marker not in main_vm:
        problems.append(f"MainViewModel missing {vm_marker}")
    for marker in group_markers:
        if marker not in param_vm:
            problems.append(f"ParameterViewModel missing {marker}")
    if problems:
        fail(label + " current renderer: " + "; ".join(problems))
    else:
        note(f"{label}: {len(parameter_ids)} current source inputs / native keyboard+pointer controls registered")

verify_current_editor("Mutant Part Drops", "MutantDropRadar", ("loot.mutant.blindDogChance", "loot.mutant.bloodsuckerChance", "loot.mutant.boarChance", "loot.mutant.burerChance", "loot.mutant.catChance", "loot.mutant.chimeraChance", "loot.mutant.controllerChance", "loot.mutant.deerChance", "loot.mutant.fleshChance", "loot.mutant.poltergeistChance", "loot.mutant.pseudodogChance", "loot.mutant.pseudogiantChance", "loot.mutant.snorkChance", "loot.mutant.tushkanChance",), "mutantDropParameterIds.Contains(p.Id)", ("IsMutantDropAnchor", "IsMutantDropMember"))

# Current UI grouping is canonical; retired 900/910 sort-index strings aren't.
_env_parameters = {p.get("id"): p for p in parameters}
if "environment.anomalyDamage" not in _env_parameters or "environment.electroAnomalyDamage" not in _env_parameters:
    fail("global vs individual anomaly parameter separation is missing")
elif not _env_parameters["environment.anomalyDamage"].get("exposeInUi", True):
    fail("global anomaly damage slider unexpectedly hidden")
else:
    note("Global vs Individual anomaly scopes are separate; historic numeric sort indices retired")

verify_current_editor("Individual Anomaly Damage", "AnomalyDamageRadar", ("environment.electroAnomalyDamage", "environment.chemicalAnomalyDamage", "environment.fireAnomalyDamage", "environment.carouselAnomalyDamage", "environment.toxicCloudDamage", "environment.steamAnomalyDamage", "environment.clickerHealthDamage", "environment.razorHealthDamage", "environment.soapBubbleHealthDamage", "environment.fireballHealthDamage", "environment.diamondHealthDamage",), "individualAnomalyDamageParameterIds.Contains(p.Id)", ("IsIndividualAnomalyDamageAnchor", "IsIndividualAnomalyDamageMember"))

verify_current_editor("Stash Ammo", "StashAmmoRadar", ("loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity", "loot.expensiveStashAmmoQuantity",), "stashAmmoParameterIds", ("IsStashAmmoAnchor", "IsStashAmmoMember"))

# Current Breakable Boxes aggregate production gate (UI-06, 2026-10-09).
# The retired AmmoDistributionRadar.cs is NOT a valid renderer or test target.
# The live six-source editor is AggregateVisualControl / AmmoDistribution.
_abd_weight_ids = (
    "loot.ammoBoxWeight918", "loot.ammoBoxWeight045",
    "loot.ammoBoxWeight545", "loot.ammoBoxWeight012",
)
_abd_yield_ids = ("loot.destructibleAmmoQuantity", "loot.destructibleGrenadeQuantity")
_abd_all_ids = _abd_yield_ids + _abd_weight_ids
_abd_rows_by_id = {row.get("id"): row for row in parameters}
_abd_rows = [_abd_rows_by_id.get(pid) for pid in _abd_all_ids]
_abd_expected_ranges = {
    "loot.destructibleAmmoQuantity": (0.5, 2.0, 0.25),
    "loot.destructibleGrenadeQuantity": (0.5, 2.0, 0.5),
    **{pid: (0.0, 3.0, 0.25) for pid in _abd_weight_ids},
}
_abd_baseline_map = (baseline.get("special", {}).get("destructibleAmmoProduction", {}) or {}).get("weightParameterIds", {})
_abd_expected_map = {
    "A918D": "loot.ammoBoxWeight918",
    "A919D": "loot.ammoBoxWeight918",
    "A045D": "loot.ammoBoxWeight045",
    "A545D": "loot.ammoBoxWeight545",
    "A556D": "loot.ammoBoxWeight545",
    "A012D": "loot.ammoBoxWeight012",
}
_abd_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "AggregateVisualControl.cs"
_abd_retired_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "AmmoDistributionRadar.cs"
_abd_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_abd_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_abd_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
_abd_writer = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
_abd_errors = []
if any(row is None for row in _abd_rows):
    _abd_errors.append("one or more of the six Breakable Boxes source parameters are missing")
if any(pid in _abd_rows_by_id for pid in {"loot.ammoBoxWeight919", "loot.ammoBoxWeight556"}):
    _abd_errors.append("obsolete per-caliber family parameters returned")
for _pid in _abd_all_ids:
    _row = _abd_rows_by_id.get(_pid)
    if _row is None:
        continue
    _slider = _row.get("slider") or {}
    if _row.get("control") != "slider" or (
        _slider.get("min"), _slider.get("max"), _slider.get("step")
    ) != _abd_expected_ranges[_pid] or (_row.get("vanilla") or {}).get("All") != 1.0:
        _abd_errors.append(f"{_pid}: native range/step/vanilla contract drifted")
_abd_anchor = _abd_rows_by_id.get("loot.ammoBoxWeight918")
if _abd_anchor and (
    not _abd_anchor.get("exposeInUi", True)
    or _abd_anchor.get("label") != "Breakable Box Ammo Distribution"
    or any(_abd_rows_by_id[pid].get("exposeInUi", True)
           for pid in _abd_weight_ids[1:] if pid in _abd_rows_by_id)
):
    _abd_errors.append("four-family aggregate anchor/visibility contract drifted")
if _abd_baseline_map != _abd_expected_map:
    _abd_errors.append(f"shared six-caliber weight mapping drifted: {_abd_baseline_map}")
if _abd_retired_control.exists() or "AmmoDistributionRadar" in _abd_xaml:
    _abd_errors.append("retired standalone AmmoDistributionRadar returned")
if not _abd_control.exists():
    _abd_errors.append("AggregateVisualControl.cs is missing")
else:
    _abd_control_text = _abd_control.read_text(encoding="utf-8")
    _abd_required_source = (
        'case "AmmoDistribution":',
        "private void DrawAmmoDistribution(",
        'DrawHorizontalTrackCard(dc, ammoYield,',
        'DrawHorizontalTrackCard(dc, P("loot.destructibleGrenadeQuantity"),',
        '"AMMO YIELD PREVIEW"',
        '"ROUNDS IF ROLLED"',
        'var countsModified = ammoYield.IsValueModifiedFromReference();',
        'item.MinCount * ammoYield.CurrentNumber + 0.5',
        'item.MaxCount * ammoYield.CurrentNumber + 0.5',
        'YieldRow("SNG", 173.0, new[]',
        'YieldRow("NATO", 219.0, new[]',
        'Pool("SNG POOL",',
        'Pool("NATO POOL",',
        'EnforceAmmoPoolValidity(',
        '"AmmoDistribution" => 680.0,',
        '"AmmoDistribution" => new[]',
        '"BreakableBoxYields" => Array.Empty<string>()',
        'protected override void OnMouseLeftButtonDown(',
        'protected override void OnMouseRightButtonDown(',
        'protected override void OnKeyDown(',
    )
    for _token in _abd_required_source:
        if _token not in _abd_control_text:
            _abd_errors.append(f"live aggregate source missing {_token!r}")
    # The eight literal Min/Max/Weight items must be traceable to the source
    # generator pools; do not validate deprecated polar-axis text instead.
    _source_pools = (baseline.get("special", {}).get("destructibleAmmoProduction", {}) or {}).get("pools", [])
    _source_items = [item for pool in _source_pools for item in pool.get("items", [])]
    if len(_source_pools) != 2 or len(_source_items) != 8:
        _abd_errors.append("SNG/NATO CFG provenance must contain exactly 2 pools / 8 item rows")
    else:
        for _caliber, _low, _high, _weight in (
            ("9×18", 3, 6, "weight918"), ("9×19", 5, 10, "weight918"),
            ("5.45", 5, 10, "weight545"), ("12 GA", 2, 4, "weight012"),
            (".45 ACP", 5, 10, "weight045"), ("9×19", 5, 10, "weight918"),
            ("5.56", 5, 10, "weight545"), ("12 GA", 5, 10, "weight012"),
        ):
            _token = f'(Caliber: "{_caliber}", MinCount: {_low}, MaxCount: {_high}, Weight: {_weight})'
            if _token not in _abd_control_text:
                _abd_errors.append(f"source-backed count preview missing {_token!r}")
if '<controls:AggregateVisualControl x:Name="AggregateVisualControl"' not in _abd_xaml or 'Family="{Binding VisualAggregateFamily}"' not in _abd_xaml:
    _abd_errors.append("MainWindow is not bound to the active aggregate visual family")
if "ammoDistributionParameterIds.Contains(p.Id)" not in _abd_main_vm:
    _abd_errors.append("backing family weights are absent from live VM graph")
for _pid in _abd_all_ids:
    if f'["{_pid}"] = "AmmoDistribution"' not in _abd_param_vm:
        _abd_errors.append(f"{_pid}: not mapped to active AmmoDistribution family")
if '["AmmoDistribution"] = "loot.ammoBoxWeight918"' not in _abd_param_vm:
    _abd_errors.append("AmmoDistribution family no longer has the approved anchor")
if "WeightParameterIds" not in _abd_writer or "effectiveWeight" not in _abd_writer or "GetOrAddChild(itemDefinition.Node" not in _abd_writer:
    _abd_errors.append("destructible ammo CFG writer weight/count route missing")
if _abd_errors:
    fail("Breakable Boxes current UI/CFG contract drifted: " + "; ".join(_abd_errors))
else:
    note("Breakable Boxes UI-06 locked: six live aggregate sources; 8 conditional ammo ranges; 2 distinct SNG/NATO weight pools; no obsolete radar")


# Historical evidence (not shipped in current repo) - never report as verified.
not_run("ammo weights source CSV snapshot: archived CSV/patch snapshot absent; current active catalog/manifest contracts checked elsewhere")

verify_current_editor("World Loot Removal", "WorldLootRemovalRadar", ("loot.worldAmmoCratesRemoved", "loot.worldF1GrenadeBoxes", "loot.worldFoodDrinkCratesRemoved", "loot.worldMedicalCratesRemoved", "loot.looseWorldLootRemoved",), "worldLootRemovalParameterIds.Contains(p.Id)", ("IsWorldLootRemovalAnchor", "IsWorldLootRemovalMember"))

# F1 grenade box density surface gate (v0.9.26.44).
_f1_manifest = DATA / "world-f1-grenade-box-manifest.csv"
_f1_writer_text = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
_f1_vm_text = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
if not _f1_manifest.exists():
    fail("world F1 grenade box manifest missing")
else:
    import csv as _f1_csv
    with _f1_manifest.open(encoding="utf-8", newline="") as _f:
        _f1_rows = list(_f1_csv.DictReader(_f))
    _f1_base = [r for r in _f1_rows if r.get("scope") == "base"]
    _f1_dlc = [r for r in _f1_rows if r.get("scope") == "dlc1"]
    if len(_f1_rows) != 101 or len(_f1_base) != 61 or len(_f1_dlc) != 40:
        fail(f"F1 grenade box surface drifted: total/base/dlc={len(_f1_rows)}/{len(_f1_base)}/{len(_f1_dlc)}; expected 101/61/40")
    elif {r.get("spawned_prototype_sid") for r in _f1_rows} != {"D_WoodenAmmoCrate_01"}:
        fail("F1 grenade box manifest contains non-wooden-ammo-crate actors")
    elif {r.get("generator_sid") for r in _f1_rows} != {"DestructibleStash_F1"}:
        fail("F1 grenade box manifest contains non-F1 generator rows")
    elif 'const string parameterId = "loot.worldF1GrenadeBoxes";' not in _f1_writer_text:
        fail("F1 grenade box production writer missing")
    elif "var removeCount = Math.Clamp(" not in _f1_writer_text or "rows.Count * removedFraction" not in _f1_writer_text:
        fail("F1 grenade box writer no longer uses percentage-as-removed semantics")
    elif "SelectRandomPrefix(rows, removeCount);" not in _f1_writer_text:
        fail("F1 grenade box writer no longer samples removals without replacement")
    elif "Removes {baseCount}/61 base F1 boxes ({dlcCount}/101 with DLC)." not in _f1_vm_text:
        fail("F1 grenade box live helper drifted")
    else:
        note("F1 grenade boxes locked: 61 base + 40 DLC1 dedicated physical boxes; 0%=vanilla, percentage controls boxes removed, random without replacement")

# Blueprint shuffle world-placement release gate (v0.9.26.44).
_blueprint_manifest = ROOT / "src" / "TrueCustomDifficulty" / "Data" / "blueprint-shuffle-manifest.csv"
if not _blueprint_manifest.exists():
    fail("blueprint shuffle manifest missing")
else:
    with _blueprint_manifest.open(encoding="utf-8", newline="") as fh:
        _bp_rows = list(_csv.DictReader(fh))
    _bp_generators = [row.get("generator_sid") for row in _bp_rows]
    _bp_items = [row.get("item_sid") for row in _bp_rows]
    _bp_paths = [row.get("node_path") for row in _bp_rows]
    _bp_sources = [row.get("world_source_id") for row in _bp_rows]
    if len(_bp_rows) != 77:
        fail(f"blueprint shuffle manifest drifted: {len(_bp_rows)} rows; expected 77")
    elif len(set(_bp_generators)) != 77 or len(set(_bp_items)) != 77 or len(set(_bp_sources)) != 77:
        fail("blueprint shuffle manifest must remain one-to-one across generators, blueprint items and world source actors")
    elif any(not str(x).startswith("Stash_Blueprint_") for x in _bp_generators):
        fail("blueprint shuffle manifest contains a non-blueprint stash generator")
    elif any(not str(x).startswith("Blueprint_") for x in _bp_items):
        fail("blueprint shuffle manifest contains a non-blueprint item")
    elif any(x == "Blueprint_FaustPsyResist_Quest_1_1" for x in _bp_items):
        fail("quest blueprint leaked into world blueprint shuffle")
    elif any(x != "ItemGenerator/[0]/PossibleItems/[0]" for x in _bp_paths):
        fail("blueprint shuffle node-path contract drifted")
    else:
        note("blueprint shuffle locked: 77 unique world-only slots / 77 unique blueprint items; Faust quest + 2 unresolved stash blueprints excluded")


# Trader minimum buy-condition release gate (v0.9.26.44).
_trader_buy_manifest = ROOT / "src" / "TrueCustomDifficulty" / "Data" / "trader-min-buy-condition-manifest.csv"
if not _trader_buy_manifest.exists():
    fail("trader minimum buy-condition manifest missing")
else:
    with _trader_buy_manifest.open(encoding="utf-8", newline="") as fh:
        _tb_rows = list(_csv.DictReader(fh))
    _tb_pairs = [(r.get("file"), r.get("prototype")) for r in _tb_rows]
    _tb_scope_counts = {}
    for r in _tb_rows:
        _tb_scope_counts[r.get("scope")] = _tb_scope_counts.get(r.get("scope"), 0) + 1
    if len(_tb_rows) != 86 or len(set(_tb_pairs)) != 86:
        fail(f"trader minimum buy-condition manifest drifted: rows={len(_tb_rows)} unique={len(set(_tb_pairs))}; expected 86/86")
    elif _tb_scope_counts != {"base": 68, "dlc1": 18}:
        fail(f"trader minimum buy-condition scope drifted: {_tb_scope_counts}; expected base=68/dlc1=18")
    elif any(r.get("node_path") != "TradeGenerators/[0]" for r in _tb_rows):
        fail("trader minimum buy-condition node path drifted")
    elif any(r.get("weapon_key") != "WeaponSellMinDurability" or r.get("armor_key") != "ArmorSellMinDurability" for r in _tb_rows):
        fail("trader minimum buy-condition field contract drifted")
    elif any(r.get("vanilla") != "0.4f" for r in _tb_rows):
        fail("trader minimum buy-condition vanilla threshold drifted")
    else:
        note("trader minimum buy condition locked: 86 production profiles = 68 base + 18 DLC1; weapon+armor gate 40% vanilla")



# Historical evidence (not shipped in current repo) - never report as verified.
not_run("generator decision-matrix CSV snapshot: archived CSV/patch snapshot absent; current active catalog/manifest contracts checked elsewhere")

# Generator artifact layout cleanup gate (v0.2.41).
_layout_doc = ROOT / "docs" / "generators" / "ARTIFACT-LAYOUT.md"
_legacy_generators = ROOT / "generators"
if not _layout_doc.exists():
    fail("generator artifact layout policy missing")
elif _legacy_generators.exists():
    fail("legacy generators/ mirror exists; docs/generators/ must remain canonical")
else:
    note("generator artifact cleanup locked: docs/generators canonical; legacy generators mirror absent")

verify_current_editor("Medical Healing", "MedicalHealingRadar", ("health.bandageHealing", "health.medkitHealing", "health.armyMedkitHealing", "health.ecoMedkitHealing",), "medicalHealingParameterIds.Contains(p.Id)", ("IsMedicalHealingAnchor", "IsMedicalHealingMember"))

# Ordinary food healing switch gate (v0.9.26.51).
_food_healing = next((p for p in parameters if p.get("id") == "health.foodHealing"), None)
_food_healing_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") == "health.foodHealing"]
_food_expected = {
    ("BreadHealing1", "ValueMin", "8"), ("BreadHealing1", "ValueMax", "8"),
    ("FreshBreadHealing2", "ValueMin", "15"), ("FreshBreadHealing2", "ValueMax", "15"),
    ("CannedHealing2", "ValueMin", "18"), ("CannedHealing2", "ValueMax", "18"),
    ("SausageHealing2", "ValueMin", "12"), ("SausageHealing2", "ValueMax", "12"),
}
_food_actual = {(e.get("prototype"), e.get("key"), e.get("sourceRaw")) for e in _food_healing_points}
_writer_source = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
if not _food_healing or _food_healing.get("control") != "toggle" or (_food_healing.get("vanilla") or {}).get("All") is not True:
    fail("Food Restores Health must remain a vanilla-ON confirmed toggle")
elif _food_actual != _food_expected or any(e.get("operation") != "toggleSourceOrZero" for e in _food_healing_points):
    fail(f"ordinary food healing writer surface drifted: {_food_actual}")
elif any("WeirdKettle" in (e.get("prototype") or "") for e in _food_healing_points):
    fail("Food Restores Health must never modify Weird Kettle alternative effects")
elif "toggleSourceOrZero" not in _writer_source or "parameter.CurrentBoolean ? sourceRaw : FormatLikeSource(0.0, sourceRaw)" not in _writer_source:
    fail("Food Restores Health writer semantics drifted from vanilla-or-zero")
else:
    note("Food Restores Health locked: 4 ordinary food effects / 8 ValueMin+ValueMax edits; OFF=0 HP, ON=vanilla; Weird Kettle untouched")

# Health deep audit release gate (v0.2.45).
_health_doc = DOCS / "HEALTH-CONTROL-DEEP-AUDIT.md"
_health_ui = [p for p in parameters if p.get("tab") == "health" and p.get("exposeInUi", True)]
_health_confirmed = [p for p in _health_ui if p.get("support", {}).get("status") == "confirmed"]
_health_runtime = [p for p in _health_ui if p.get("support", {}).get("status") == "runtime_test_required"]
_treatment_ids = {
    "health.bandageBleeding", "health.medkitBleeding", "health.armyMedkitBleeding",
    "health.ecoMedkitBleeding", "health.ecoMedkitAntirad", "health.antiRadRadiation",
}
_health_all_ids = {p.get("id") for p in parameters if p.get("tab") == "health"}
_health_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") in _health_all_ids]
_treatment_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") in _treatment_ids]
_treatment_catalog = [p for p in _health_ui if p.get("id") in _treatment_ids]
_vm_source = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
if len(_health_ui) != 13 or len(_health_confirmed) != 10 or len(_health_runtime) != 3:
    fail(f"health deep audit status split drifted: total={len(_health_ui)} confirmed={len(_health_confirmed)} runtime={len(_health_runtime)}; expected 13/10/3")
elif len(_health_points) != 38:
    fail(f"health writer surface drifted: {len(_health_points)} edit points; expected 38")
elif len(_treatment_points) != 12 or any(e.get("operation") != "negateSet" for e in _treatment_points):
    fail("health treatment signed-delta boundary drifted: expected 12 negateSet edit points")
elif len(_treatment_catalog) != 6 or any((p.get("slider") or {}).get("min") != 0.0 for p in _treatment_catalog) or any(next(iter((p.get("vanilla") or {}).values())) <= 0 for p in _treatment_catalog):
    fail("health treatment UI magnitude contract drifted: six positive-magnitude controls with min=0 required")
elif "RequiresPositiveTreatmentMagnitudePresetMigration" not in _vm_source or "Math.Abs(pair.Value.Number.Value)" not in _vm_source:
    fail("pre-v0.2.45 negative treatment preset migration missing")
elif not _health_doc.exists():
    fail("health deep audit document missing from docs/")
else:
    note("health deep audit locked: 13 visible controls / 10 confirmed + 3 runtime-gated; Healing Amount aggregated; 38 edit points; six treatment sliders use positive magnitude -> negateSet CFG delta")


verify_current_editor("Action Costs", "ActionCostsRadar", ("stamina.sprintCost", "stamina.jumpCost", "stamina.vaultCost", "stamina.meleeNormalCost", "stamina.meleeStrongCost", "stamina.butstockCost", "stamina.aimRegenCoef",), "actionCostParameterIds.Contains(p.Id)", ("IsActionCostAnchor", "IsActionCostMember"))

# Seven Action Costs backings are all relevant to the stamina writer,
# even though the visible card may expose only the aggregate anchor.
_acr_ids = (
    "stamina.sprintCost", "stamina.jumpCost", "stamina.vaultCost",
    "stamina.meleeNormalCost", "stamina.meleeStrongCost",
    "stamina.butstockCost", "stamina.aimRegenCoef",
)

# Stamina deep audit release gate (v0.2.46).
_stamina_doc = DOCS / "STAMINA-CONTROL-DEEP-AUDIT.md"
_stamina_ui = [p for p in parameters if p.get("tab") == "stamina" and p.get("exposeInUi", True)]
_stamina_confirmed = [p for p in _stamina_ui if p.get("support", {}).get("status") == "confirmed"]
_stamina_runtime = [p for p in _stamina_ui if p.get("support", {}).get("status") == "runtime_test_required"]
_stamina_all = [p for p in parameters if p.get("tab") == "stamina"]
# Keep the original 13-control deep-audit writer surface: current visible stamina
# controls plus the six Action Costs backing rows hidden by the aggregate radar.
_stamina_ids = {p.get("id") for p in _stamina_ui} | set(_acr_ids)
_stamina_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") in _stamina_ids]
_stamina_rows = {p.get("id"): p for p in _stamina_all}
_aim = _stamina_rows.get("stamina.aimRegenCoef", {})
_aim_points = [e for e in _stamina_points if e.get("parameterId") == "stamina.aimRegenCoef"]
_vault = _stamina_rows.get("stamina.vaultMaxHeight", {})
_vault_markers = {(m.get("label"), m.get("value")) for m in _vault.get("markers", [])}
_water_constraint = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "waterStaminaDrainDepth"), {})
_melee_constraint = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "meleeStaminaCostStrength"), {})
_vm_source_stamina = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")

if len(_stamina_ui) != 7 or len(_stamina_confirmed) != 6 or len(_stamina_runtime) != 1:
    fail(f"stamina deep audit status split drifted: total={len(_stamina_ui)} confirmed={len(_stamina_confirmed)} runtime={len(_stamina_runtime)}; expected 7/6/1")
elif len(_stamina_points) != 15:
    fail(f"stamina writer surface drifted: {len(_stamina_points)} edit points; expected 15")
elif _aim.get("label") != "Aiming Stamina Recovery Penalty" or (_aim.get("vanilla") or {}).get("All") != 0.25:
    fail("aiming stamina player-facing magnitude contract drifted")
elif (_aim.get("slider") or {}).get("min") != 0.0 or (_aim.get("slider") or {}).get("max") != 0.5 or (_aim.get("display") or {}).get("primary") != "percent":
    fail("aiming stamina UI range/display drifted: expected positive 0..50% magnitude")
elif len(_aim_points) != 1 or _aim_points[0].get("operation") != "negateSet" or _aim_points[0].get("sourceRaw") != "-0.25":
    fail("aiming stamina signed CFG boundary drifted: expected one negateSet point over source -0.25")
elif "RequiresPositiveAimingPenaltyMagnitudePresetMigration" not in _vm_source_stamina or 'pair.Key == "stamina.aimRegenCoef"' not in _vm_source_stamina or "Math.Abs(pair.Value.Number.Value)" not in _vm_source_stamina:
    fail("pre-v0.2.46 negative aiming-coefficient preset migration missing")
elif (_vault.get("slider") or {}).get("max") != 300.0 or ("ABOVE EYES", 300.0) not in _vault_markers:
    fail("maximum vault height upper-range contract drifted: slider and ABOVE EYES marker must both reach 300")
elif _water_constraint.get("order") != "nondecreasing" or _water_constraint.get("parameterIds") != ["stamina.waterMediumDrain", "stamina.waterStrongDrain"]:
    fail("water stamina Medium<=Deep value constraint drifted")
elif _melee_constraint.get("order") != "nondecreasing" or _melee_constraint.get("parameterIds") != ["stamina.meleeNormalCost", "stamina.meleeStrongCost"]:
    fail("melee stamina Normal<=Strong value constraint drifted")
elif not _stamina_doc.exists():
    fail("stamina deep audit document missing from docs/")
else:
    note("stamina deep audit locked: 7 visible controls / 6 confirmed + 1 runtime-gated; Action Costs aggregated; 15 edit points; Aim uses positive magnitude -> negateSet; vault ceiling 300")

# Survival deep audit release gate (v0.2.47).
_survival_doc = DOCS / "SURVIVAL-CONTROL-DEEP-AUDIT.md"
_survival_mapping = DOCS / "SURVIVAL-MAPPING.md"
_survival_ui = [p for p in parameters if p.get("tab") == "survival" and p.get("exposeInUi", True)]
_survival_confirmed = [p for p in _survival_ui if p.get("support", {}).get("status") == "confirmed"]
_survival_runtime = [p for p in _survival_ui if p.get("support", {}).get("status") == "runtime_test_required"]
_survival_ids = {p.get("id") for p in _survival_ui}
_survival_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") in _survival_ids]
_survival_rows = {p.get("id"): p for p in _survival_ui}
_hunger_penalty_ids = {"survival.hungerMediumStaminaRegen", "survival.hungerStrongStaminaRegen"}
_hunger_penalty_points = [e for e in _survival_points if e.get("parameterId") in _hunger_penalty_ids]
_hunger_medium = _survival_rows.get("survival.hungerMediumStaminaRegen", {})
_hunger_strong = _survival_rows.get("survival.hungerStrongStaminaRegen", {})
_hunger_constraint = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "hungerStaminaRecoverySeverity"), {})
_bleeding_constraint = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "bleedingDamageSeverity"), {})
_fatigue_aim_constraint = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "fatigueAimPenaltySeverity"), {})
_fatigue_reload_constraint = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "fatigueReloadPenaltySeverity"), {})
_low_bleeding_points = [e for e in _survival_points if e.get("parameterId") == "survival.bleedingLowDamage"]
_vm_source_survival = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_survival_mapping_text = _survival_mapping.read_text(encoding="utf-8") if _survival_mapping.exists() else ""

if len(_survival_ui) != 32 or len(_survival_confirmed) != 30 or len(_survival_runtime) != 2:
    fail(f"survival deep audit status split drifted: total={len(_survival_ui)} confirmed={len(_survival_confirmed)} runtime={len(_survival_runtime)}; expected 32/30/2")
elif len(_survival_points) != 93:
    fail(f"survival writer surface drifted: {len(_survival_points)} edit points; expected 93")
elif _hunger_medium.get("label") != "Hunger Stamina Recovery Penalty" or (_hunger_medium.get("vanilla") or {}).get("All") != 0.33:
    fail("medium hunger stamina recovery player-facing penalty contract drifted")
elif _hunger_strong.get("label") != "Severe Hunger Stamina Recovery Penalty" or (_hunger_strong.get("vanilla") or {}).get("All") != 0.7:
    fail("severe hunger stamina recovery player-facing penalty contract drifted")
elif (_hunger_medium.get("slider") or {}).get("min") != 0.0 or (_hunger_medium.get("slider") or {}).get("max") != 0.66:
    fail("medium hunger stamina penalty range drifted: expected 0..66%")
elif (_hunger_strong.get("slider") or {}).get("min") != 0.0 or (_hunger_strong.get("slider") or {}).get("max") != 1.0:
    fail("severe hunger stamina penalty range drifted: expected 0..100%")
elif len(_hunger_penalty_points) != 4 or any(e.get("operation") != "negateSet" for e in _hunger_penalty_points):
    fail("hunger stamina signed CFG boundary drifted: expected four negateSet edit points")
elif sorted(e.get("sourceRaw") for e in _hunger_penalty_points) != ["-33%", "-33%", "-70%", "-70%"]:
    fail("hunger stamina source baseline drifted: expected -33%/-70% ValueMin/ValueMax")
elif "RequiresPositiveHungerPenaltyMagnitudePresetMigration" not in _vm_source_survival or "IsPositiveHungerPenaltyMagnitudeParameter" not in _vm_source_survival or "new Version(0, 2, 47)" not in _vm_source_survival:
    fail("pre-v0.2.47 negative hunger-penalty preset migration missing")
elif _hunger_constraint.get("order") != "nondecreasing" or _hunger_constraint.get("parameterIds") != ["survival.hungerMediumStaminaRegen", "survival.hungerStrongStaminaRegen"]:
    fail("hunger stamina penalty Medium<=Severe value constraint drifted")
elif _bleeding_constraint.get("order") != "nondecreasing" or _bleeding_constraint.get("parameterIds") != ["survival.bleedingLowDamage", "survival.bleedingMediumDamage", "survival.bleedingStrongDamage"]:
    fail("bleeding damage Low<=Medium<=Severe value constraint drifted")
elif _fatigue_aim_constraint.get("order") != "nondecreasing" or _fatigue_aim_constraint.get("parameterIds") != ["survival.fatigueLowAimPenalty", "survival.fatigueMediumAimPenalty", "survival.fatigueStrongAimPenalty"]:
    fail("fatigue aim Low<=Medium<=Severe value constraint drifted")
elif _fatigue_reload_constraint.get("order") != "nondecreasing" or _fatigue_reload_constraint.get("parameterIds") != ["survival.fatigueMediumReloadPenalty", "survival.fatigueStrongReloadPenalty"]:
    fail("fatigue reload Medium<=Severe value constraint drifted")
elif len(_low_bleeding_points) != 2 or any(e.get("sourceRaw") != "1.0" for e in _low_bleeding_points) or (_survival_rows.get("survival.bleedingLowDamage", {}).get("vanilla") or {}).get("All") != 1.0:
    fail("Low Bleeding Damage runtime baseline drifted: expected 1.0/1.0")
elif "LowBleedingDamage = 0.33" in _survival_mapping_text:
    fail("stale LowBleedingDamage = 0.33 documentation returned")
elif not _survival_mapping.exists():
    fail("Survival mapping document missing from docs/")
elif not _survival_doc.exists():
    fail("Survival deep audit document missing from docs/")
else:
    note("survival current deep audit: 32 visible / 30 confirmed + 2 runtime-pending; 93 direct edit points; hunger sign/order preserved")

verify_current_editor("Item Price Scalers", "ItemPriceScalersRadar", ("economy.consumableCost", "economy.ammoCost", "economy.armorCost", "economy.weaponCost", "economy.artifactCost", "economy.mutantPartValue",), "itemPriceScalerParameterIds.Contains(p.Id)", ("IsItemPriceScalerAnchor", "IsItemPriceScalerMember"))

# Economy deep audit release gate (v0.2.48).
_economy_doc = DOCS / "ECONOMY-CONTROL-DEEP-AUDIT.md"
_economy_mapping = DOCS / "ECONOMY-MAPPING.md"
_economy_ui = [p for p in parameters if p.get("tab") == "economy" and p.get("exposeInUi", True)]
_economy_all = [p for p in parameters if p.get("tab") == "economy"]
_economy_rows = {p.get("id"): p for p in _economy_all}
_economy_confirmed = [p for p in _economy_ui if p.get("support", {}).get("status") == "confirmed"]
_economy_runtime = [p for p in _economy_ui if p.get("support", {}).get("status") == "runtime_test_required"]
_economy_runtime_expected = {"economy.buyCondition", "economy.sellCondition", "economy.medicStock", "economy.traderAmmoStock", "economy.traderMinBuyCondition"}
_economy_ids = {p.get("id") for p in _economy_all if p.get("id") != "economy.repairDifficultyCost"}
_economy_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") in _economy_ids]
_economy_runtime_actual = {p.get("id") for p in _economy_runtime}
_economy_buy = _economy_rows.get("economy.buyCondition", {})
_economy_sell = _economy_rows.get("economy.sellCondition", {})
_economy_fast = [e for e in _economy_points if e.get("parameterId") == "economy.fastTravelCost"]
_economy_medic = [e for e in _economy_points if e.get("parameterId") == "economy.medicStock"]
_economy_trader_ammo = [e for e in _economy_points if e.get("parameterId") == "economy.traderAmmoStock"]
_economy_mutant = [e for e in _economy_points if e.get("parameterId") == "economy.mutantPartValue"]
_economy_repair_constraint = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "repairPriceReputation"), {})
_economy_hidden_repair = next((p for p in parameters if p.get("id") == "economy.repairDifficultyCost"), {})
_economy_hidden_repair_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") == "economy.repairDifficultyCost"]
_economy_diff_map = {"Easy": "Novice", "Medium": "Stalker", "Hard": "Veteran", "Stalker": "Master"}
_economy_diff_ids = {
    "economy.buyCondition", "economy.sellCondition", "economy.consumableCost", "economy.ammoCost",
    "economy.armorCost", "economy.weaponCost", "economy.artifactCost", "economy.upgradeCost",
    "economy.mainQuestReward", "economy.sideQuestReward",
}
_economy_diff_mismatch = []
for _pid in sorted(_economy_diff_ids):
    _row = _economy_rows.get(_pid, {})
    _vanilla = _row.get("vanilla", {})
    _points = [e for e in _economy_points if e.get("parameterId") == _pid]
    _actual = {e.get("difficultySid"): float(e.get("sourceRaw")) for e in _points}
    _expected = {_sid: float(_vanilla.get(_name)) for _sid, _name in _economy_diff_map.items()}
    if _actual != _expected:
        _economy_diff_mismatch.append((_pid, _actual, _expected))

if len(_economy_ui) != 17 or len(_economy_confirmed) != 12 or len(_economy_runtime) != 5 or _economy_runtime_actual != _economy_runtime_expected:
    fail(f"economy deep audit status split drifted: total={len(_economy_ui)} confirmed={len(_economy_confirmed)} runtime={sorted(_economy_runtime_actual)}; expected 17/12/5")
elif len(_economy_points) != 405:
    fail(f"economy writer surface drifted: {len(_economy_points)} edit points; expected 405")
elif _economy_buy.get("label") != "Trader Buy-From-You Pricing" or _economy_sell.get("label") != "Trader Sell-To-You Pricing":
    fail("economy trader Buy/Sell player-direction labels drifted")
elif _economy_diff_mismatch:
    fail(f"economy difficulty-profile vanilla/source baselines drifted: {_economy_diff_mismatch}")
elif len(_economy_fast) != 183 or any(e.get("operation") != "scaleNonZero" for e in _economy_fast) or sum(float(e.get("sourceRaw")) == 0.0 for e in _economy_fast) != 1:
    fail("fast-travel economy surface drifted: expected 183 scaleNonZero points with exactly one zero-valued source")
elif len(_economy_medic) != 96 or any(e.get("operation") != "scaleIntegerHalfUp" for e in _economy_medic) or sum(e.get("key") == "MinCount" for e in _economy_medic) != 48 or sum(e.get("key") == "MaxCount" for e in _economy_medic) != 48:
    fail("medic-stock economy surface drifted: expected 48 MinCount + 48 MaxCount scaleIntegerHalfUp points")
elif len(_economy_trader_ammo) != 64 or any(e.get("operation") != "scaleIntegerHalfUp" for e in _economy_trader_ammo) or sum(e.get("key") == "MinCount" for e in _economy_trader_ammo) != 32 or sum(e.get("key") == "MaxCount" for e in _economy_trader_ammo) != 32:
    fail("trader-ammo-stock economy surface drifted: expected 32 MinCount + 32 MaxCount T1-T4 scaleIntegerHalfUp points")
elif len(_economy_mutant) != 15 or any(e.get("operation") != "scale" or e.get("key") != "Cost" for e in _economy_mutant) or sum(e.get("scope") == "baseGame" for e in _economy_mutant) != 14 or sum(e.get("scope") == "dlc1" for e in _economy_mutant) != 1:
    fail("mutant-parts economy surface drifted: expected 14 base-game + 1 DLC1 Cost scale targets")
elif _economy_repair_constraint.get("order") != "nondecreasing" or _economy_repair_constraint.get("parameterIds") != ["economy.repairFriend", "economy.repairNeutral", "economy.repairDisaffection", "economy.repairEnemy"]:
    fail("repair reputation Friendly<=Neutral<=Disliked<=Hostile value constraint drifted")
elif _economy_hidden_repair.get("exposeInUi", True) or len(_economy_hidden_repair_points) != 4 or any(e.get("operation") != "set" or float(e.get("sourceRaw")) != 1.0 for e in _economy_hidden_repair_points):
    fail("audit-only EconomyDifficulty.Repair_Cost boundary drifted")
elif not _economy_mapping.exists():
    fail("Economy mapping document missing from docs/")
elif not _economy_doc.exists():
    fail("Economy deep audit document missing from docs/")
else:
    note("economy deep audit locked: 17 visible controls / 12 confirmed + 5 runtime-gated; Item Price Scalers aggregated; 405 direct edit points + trader minimum buy-condition special writer; repair ordering and aggregate surfaces verified")

# Equipment deep audit release gate (v0.2.49).
_equipment_doc = DOCS / "EQUIPMENT-CONTROL-DEEP-AUDIT.md"
_equipment_mapping = DOCS / "EQUIPMENT-MAPPING.md"
_equipment_ui = [p for p in parameters if p.get("tab") == "equipment" and p.get("exposeInUi", True)]
_equipment_rows = {p.get("id"): p for p in _equipment_ui}
_equipment_confirmed = [p for p in _equipment_ui if p.get("support", {}).get("status") == "confirmed"]
_equipment_runtime = [p for p in _equipment_ui if p.get("support", {}).get("status") == "runtime_test_required"]
_equipment_ids = set(_equipment_rows)
_equipment_points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") in _equipment_ids]
_equipment_runtime_actual = {p.get("id") for p in _equipment_runtime}
_equipment_armor = [e for e in _equipment_points if e.get("parameterId") == "equipment.armorDurability"]
_equipment_ammo = [e for e in _equipment_points if e.get("parameterId") == "equipment.ammoWeight"]
_equipment_consumable = [e for e in _equipment_points if e.get("parameterId") == "equipment.consumableWeight"]
_equipment_wear = [e for e in _equipment_points if e.get("parameterId") == "equipment.weaponWearPerShot"]
_equipment_caps = {}
_equipment_diff_map = {"Easy": "Novice", "Medium": "Stalker", "Hard": "Veteran", "Stalker": "Master"}
_equipment_armor_row = _equipment_rows.get("equipment.armorDurability", {})
_equipment_armor_actual = {e.get("difficultySid"): float(e.get("sourceRaw")) for e in _equipment_armor}
_equipment_armor_expected = {
    sid: float((_equipment_armor_row.get("vanilla") or {}).get(name))
    for sid, name in _equipment_diff_map.items()
}
_equipment_cap_bad = []
for _pid, _expected_value in _equipment_caps.items():
    _pts = [e for e in _equipment_points if e.get("parameterId") == _pid]
    _row = _equipment_rows.get(_pid, {})
    if len(_pts) != 1 or _pts[0].get("operation") != "set" or _pts[0].get("key") != "MaxValue" or float(_pts[0].get("sourceRaw").rstrip("f")) != _expected_value or float((_row.get("vanilla") or {}).get("All")) != _expected_value:
        _equipment_cap_bad.append(_pid)

if len(_equipment_ui) != 4 or len(_equipment_confirmed) != 3 or len(_equipment_runtime) != 1 or _equipment_runtime_actual != {"equipment.armorDurability"}:
    fail(f"equipment deep audit status split drifted: total={len(_equipment_ui)} confirmed={len(_equipment_confirmed)} runtime={sorted(_equipment_runtime_actual)}; expected 4/3/1")
elif len(_equipment_points) != 113:
    fail(f"equipment writer surface drifted: {len(_equipment_points)} edit points; expected 117")
elif len(_equipment_armor) != 4 or any(e.get("operation") != "set" or e.get("key") != "Armor_Durability" for e in _equipment_armor) or _equipment_armor_actual != _equipment_armor_expected:
    fail(f"armor durability difficulty surface drifted: actual={_equipment_armor_actual}, expected={_equipment_armor_expected}")
elif len(_equipment_ammo) != 34 or any(e.get("operation") != "scale" or e.get("key") != "Weight" or e.get("scope") != "baseGame" for e in _equipment_ammo):
    fail("ammo-weight equipment surface drifted: expected 34 base-game Weight scale targets")
elif len(_equipment_consumable) != 23 or any(e.get("operation") != "scale" or e.get("key") != "Weight" for e in _equipment_consumable) or sum(e.get("scope") == "baseGame" for e in _equipment_consumable) != 22 or sum(e.get("scope") == "dlc1" for e in _equipment_consumable) != 1:
    fail("consumable-weight equipment surface drifted: expected 22 base-game + 1 DLC1 Weight scale targets")
elif _equipment_cap_bad:
    fail(f"equipment effect-cap surface drifted: {_equipment_cap_bad}")
elif len(_equipment_wear) != 52 or any(e.get("operation") != "scaleNonZero" or e.get("key") != "DurabilityDamagePerShot" for e in _equipment_wear):
    fail("player weapon-wear equipment surface drifted: expected 52 DurabilityDamagePerShot scaleNonZero targets")
elif sum(e.get("scope") == "baseGame" for e in _equipment_wear) != 41 or sum(e.get("scope") == "dlc1" for e in _equipment_wear) != 11:
    fail("player weapon-wear scope split drifted: expected 41 base-game + 11 DLC1")
elif sum(float(e.get("sourceRaw").rstrip("f")) == 0.0 for e in _equipment_wear) != 1 or not any(e.get("prototype") == "UA_GLaunch_Player" and float(e.get("sourceRaw").rstrip("f")) == 0.0 for e in _equipment_wear):
    fail("player weapon-wear zero-preservation boundary drifted: expected only UA_GLaunch_Player = 0.0")
elif not any(e.get("prototype") == "GunArevPrecise_AR_Player" and abs(float(e.get("sourceRaw").rstrip("f")) - 2.875) < 1e-9 for e in _equipment_wear):
    fail("player weapon-wear current base-game surface missing GunArevPrecise_AR_Player = 2.875")
elif "40 `*_Player` entries" in _equipment_mapping.read_text(encoding="utf-8") or "39 are non-zero" in _equipment_mapping.read_text(encoding="utf-8"):
    fail("stale player weapon-wear 40/39 evidence count returned")
elif not _equipment_mapping.exists():
    fail("Equipment mapping document missing from docs/")
elif not _equipment_doc.exists():
    fail("Equipment deep audit document missing from docs/")
else:
    note("equipment deep audit locked: 4 controls / 3 confirmed + 1 runtime-gated; 113 edit points; weapon wear 41 base + 11 DLC1 with explicit zero preserved")




# Build-time random crate-reduction production surface (v0.3.6).
_crate_manifest_src = DATA / "crate-spawn-production-manifest.csv"
_crate_manifest_root = ROOT / "Data" / "crate-spawn-production-manifest.csv"
_crate_doc = DOCS / "CRATE-SPAWN-AUDIT.md"
_crate_expected = {
    "loot.worldAmmoCratesRemoved": {
        "total": 1315,
        "scopes": {"base": 981, "dlc1": 334},
        "prototypes": {"D_WoodenAmmoCrate_01"},
    },
    "loot.worldFoodDrinkCratesRemoved": {
        "total": 1208,
        "scopes": {"base": 1100, "dlc1": 108},
        "prototypes": {"D_WoodenDSPCrate_01", "D_WoodenDSPCrate_02", "D_WoodenDSPCrate_03"},
    },
    "loot.worldMedicalCratesRemoved": {
        "total": 868,
        "scopes": {"base": 709, "dlc1": 159},
        "prototypes": {"D_MetallCrate_01"},
    },
}
_crate_rows = []
if _crate_manifest_src.is_file():
    import csv as _crate_csv
    with _crate_manifest_src.open(newline="", encoding="utf-8-sig") as _f:
        _crate_rows = list(_crate_csv.DictReader(_f))
_crate_errors = []

# Dedicated F1 actors must never leak back into the broad Ammo/RGD removal surface.
_f1_disjoint_manifest = DATA / "world-f1-grenade-box-manifest.csv"
if _f1_disjoint_manifest.is_file():
    with _f1_disjoint_manifest.open(newline="", encoding="utf-8-sig") as _f:
        _f1_disjoint_rows = list(_crate_csv.DictReader(_f))
    _crate_ammo_actor_keys = {
        (r.get("scope"), r.get("actorSid"))
        for r in _crate_rows if r.get("parameterId") == "loot.worldAmmoCratesRemoved"
    }
    _f1_actor_keys = {(r.get("scope"), r.get("actor_sid")) for r in _f1_disjoint_rows}
    _crate_f1_overlap = _crate_ammo_actor_keys & _f1_actor_keys
    if _crate_f1_overlap:
        _crate_errors.append(f"dedicated F1 actors leaked into broad ammo removal surface: {len(_crate_f1_overlap)} overlaps")

_crate_seen_actor = {}
for _pid, _expected in _crate_expected.items():
    _rows = [r for r in _crate_rows if r.get("parameterId") == _pid]
    if len(_rows) != _expected["total"]:
        _crate_errors.append(f"{_pid}: {len(_rows)} rows != {_expected['total']}")
    _scopes = {}
    for _r in _rows:
        _scope = _r.get("scope")
        _scopes[_scope] = _scopes.get(_scope, 0) + 1
        _actor_key = (_scope, _r.get("actorSid"))
        _owner = _crate_seen_actor.setdefault(_actor_key, _pid)
        if _owner != _pid:
            _crate_errors.append(f"actor {_actor_key} belongs to both {_owner} and {_pid}")
        _expected_source = "DLCGameData/DLC1/SpawnActorPrototypes.cfg" if _scope == "dlc1" else "GameData/SpawnActorPrototypes.cfg"
        if _r.get("sourceFile") != _expected_source:
            _crate_errors.append(f"{_pid}: {_actor_key} sourceFile {_r.get('sourceFile')} != {_expected_source}")
    if _scopes != _expected["scopes"]:
        _crate_errors.append(f"{_pid}: scope counts {_scopes} != {_expected['scopes']}")
    _protos = {r.get("spawnedPrototypeSid") for r in _rows}
    if _protos != _expected["prototypes"]:
        _crate_errors.append(f"{_pid}: physical prototypes {_protos} != {_expected['prototypes']}")
import hashlib as _crate_hashlib
_crate_actor_fingerprints = {
    "loot.worldAmmoCratesRemoved": "e77a00c8f74b717c087d02de12fd99a5c61bbf348433ceef9d8860e8b00c5ab9",
    "loot.worldFoodDrinkCratesRemoved": "933e3d55f196459739d68682ed8b0ac48e2dfe60f8b0d337df485595a8bd6117",
    "loot.worldMedicalCratesRemoved": "ed7b4f82ef46d75a60efad5dd11ede27c264450a3310922d1ae4a22f42c70fd3",
}
for _pid, _expected_fp in _crate_actor_fingerprints.items():
    _ids = sorted(r.get("actorSid", "") for r in _crate_rows if r.get("parameterId") == _pid)
    _actual_fp = _crate_hashlib.sha256(("\n".join(_ids) + "\n").encode("utf-8")).hexdigest()
    if _actual_fp != _expected_fp:
        _crate_errors.append(f"{_pid}: actor-set fingerprint {_actual_fp} != {_expected_fp}")

_crate_ammo_generators = {}
for _r in [r for r in _crate_rows if r.get("parameterId") == "loot.worldAmmoCratesRemoved"]:
    _g = (_r.get("generatorFamily") or "").split(" | ")[0]
    _crate_ammo_generators[_g] = _crate_ammo_generators.get(_g, 0) + 1
_crate_expected_ammo_generators = {
    "DestructibleStash_AmmoSNG": 982,
    "DestructibleStash_AmmoNATO": 252,
    "DestructibleStash_RGD": 81,
}
if _crate_ammo_generators != _crate_expected_ammo_generators:
    _crate_errors.append(f"ammo generator split {_crate_ammo_generators} != {_crate_expected_ammo_generators}")
_crate_medical_generators = {
    (_r.get("generatorFamily") or "").split(" | ")[0]
    for _r in _crate_rows if _r.get("parameterId") == "loot.worldMedicalCratesRemoved"
}
if _crate_medical_generators != {"DestructibleStash_Med"}:
    _crate_errors.append(f"medical generator family drifted: {_crate_medical_generators}")
_crate_writer_source = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
if not _crate_manifest_src.is_file():
    fail("crate-reduction production manifest missing from canonical src/Data/")
elif len(_crate_rows) != 3391:
    fail(f"crate-reduction production manifest contains {len(_crate_rows)} rows; expected 3391")
elif _crate_errors:
    fail("crate-reduction production surface drifted: " + "; ".join(_crate_errors[:8]))
elif not all(token in _crate_writer_source for token in ["WriteCrateSpawnRemovals(", "SelectRandomPrefix(", "RandomNumberGenerator.GetInt32", ": removenode", "IsScopeApplicable(r.Scope, hasDlc1)"]):
    fail("crate-reduction random removenode writer path missing or DLC scope filtering removed")
elif not _crate_doc.exists():
    fail("crate-spawn audit document missing from docs/")
else:
    note("crate reduction locked: Ammo/RGD 981+334 DLC1 (F1 excluded) / Food 1100+108 DLC1 / Medical 709+159 DLC1; dedicated F1 is an independent 61+40 population")



# Preset-aware final triplet naming gate (v0.9.26.51).
_build_service_text = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "ModBuildService.cs").read_text(encoding="utf-8")
_build_vm_text = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_build_name_errors = []

if 'private const string OutputPrefix = "zzzzz_TrueCustomDifficulty_";' not in _build_service_text:
    _build_name_errors.append("preset output prefix drifted")
if "BuildOutputBaseName(vm.BuildPresetName)" not in _build_service_text:
    _build_name_errors.append("build no longer derives output name from loaded preset")
if "Path.GetInvalidFileNameChars()" not in _build_service_text or "char.IsWhiteSpace(ch) ? '_' : ch" not in _build_service_text:
    _build_name_errors.append("preset filename sanitization missing")
if 'return OutputPrefix + safe;' not in _build_service_text:
    _build_name_errors.append("sanitized preset name not appended to output prefix")
if 'public string BuildPresetName' not in _build_vm_text or '"Custom"' not in _build_vm_text:
    _build_name_errors.append("loaded-preset build name/fallback missing")
if "_loadedPresetName = string.IsNullOrWhiteSpace(preset.Name)" not in _build_vm_text:
    _build_name_errors.append("successful preset load does not capture preset name")
if "TrueCustomDifficulty_P.pak" in _build_vm_text or "TrueCustomDifficulty_P.utoc" in _build_vm_text or "TrueCustomDifficulty_P.ucas" in _build_vm_text:
    _build_name_errors.append("build dialog still advertises retired static triplet names")
if 'string.Join("\\n", result.Files.Select(Path.GetFileName))' not in _build_vm_text:
    _build_name_errors.append("build dialog does not report actual final triplet files")

if _build_name_errors:
    fail("preset-aware output naming drifted: " + "; ".join(_build_name_errors))
else:
    note("preset-aware triplet naming locked: zzzzz_TrueCustomDifficulty_<loaded preset>; safe underscores; Custom fallback")

# Current 12-source-backed factory presets, 219 stored values (2026-10-09).
_factory_preset_names = [
    "Anomaly Runner", "Field Medic", "Hardcore+", "Hardcore",
    "Immersive", "Mercenary", "Mutant Hunter", "Outcast",
    "Scavenger", "Smuggler", "Tactical", "Wanderer",
]
_retired_factory_preset_names = ["Novice", "Stalker", "Veteran", "Master"]
_factory_preset_dir = ROOT / "src" / "TrueCustomDifficulty" / "Presets"
_factory_service_text = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "PresetDirectoryService.cs").read_text(encoding="utf-8")
_factory_csproj_text = (ROOT / "src" / "TrueCustomDifficulty" / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
_factory_errors = []

_factory_json_files = sorted(p.stem for p in _factory_preset_dir.glob("*.json"))
if _factory_json_files != sorted(_factory_preset_names):
    _factory_errors.append(f"shipped preset files={_factory_json_files}")

for _retired in _retired_factory_preset_names:
    if (_factory_preset_dir / f"{_retired}.json").exists():
        _factory_errors.append(f"retired preset still shipped: {_retired}")
    if f'Presets\\{_retired}.json' in _factory_csproj_text:
        _factory_errors.append(f"retired preset still embedded: {_retired}")

for _name in _factory_preset_names:
    _preset_path = _factory_preset_dir / f"{_name}.json"
    if not _preset_path.exists():
        _factory_errors.append(f"missing factory preset: {_name}")
        continue
    _preset = json.loads(_preset_path.read_text(encoding="utf-8"))
    _values = _preset.get("values", {})
    if len(_values) != 219:
        _factory_errors.append(f"{_name}: {len(_values)} values, expected 219")
    if _preset.get("referenceDifficulty") not in {"Novice", "Stalker", "Veteran", "Master"}:
        _factory_errors.append(f"{_name}: invalid referenceDifficulty={_preset.get('referenceDifficulty')}")
    if f'Presets\\{_name}.json' not in _factory_csproj_text:
        _factory_errors.append(f"{_name}: not embedded in csproj")
    if f'"{_name}"' not in _factory_service_text:
        _factory_errors.append(f"{_name}: not registered in FactoryPresetNames")

_factory_scarcity_expectations = {
    "Hardcore": {
        "loot.looseWorldLootRemoved": 0.79,
        "loot.worldAmmoCratesRemoved": 0.79,
        "loot.worldFoodDrinkCratesRemoved": 0.79,
        "loot.worldMedicalCratesRemoved": 0.79,
        "loot.worldWeaponDensity": 0.30,
        "loot.npcMedicalChance": 0.30,
        "loot.npcWeaponAmmoSupply": 0.25,
        "economy.traderAmmoStock": 0.25,
        "economy.medicStock": 0.25,
    },
    "Hardcore+": {
        "loot.looseWorldLootRemoved": 0.92,
        "loot.worldAmmoCratesRemoved": 0.92,
        "loot.worldFoodDrinkCratesRemoved": 0.92,
        "loot.worldMedicalCratesRemoved": 0.92,
        "loot.worldWeaponDensity": 0.15,
        "loot.npcMedicalChance": 0.15,
        "loot.npcWeaponAmmoSupply": 0.25,
        "economy.traderAmmoStock": 0.25,
        "economy.medicStock": 0.25,
    },
}
for _name, _expected in _factory_scarcity_expectations.items():
    _preset = json.loads((_factory_preset_dir / f"{_name}.json").read_text(encoding="utf-8"))
    for _pid, _value in _expected.items():
        _actual = _preset["values"][_pid].get("number")
        if _actual != _value:
            _factory_errors.append(f"{_name}: scarcity rule drifted {_pid}={_actual}, expected {_value}")

_factory_medical_expectations = {
    "Scavenger": (10, False),
    "Mutant Hunter": (0, False),
    "Hardcore": (0, False),
    "Hardcore+": (0, False),
}
for _name, (_bandage_expected, _food_expected) in _factory_medical_expectations.items():
    _preset = json.loads((_factory_preset_dir / f"{_name}.json").read_text(encoding="utf-8"))
    _bandage = _preset["values"].get("health.bandageHealing", {}).get("number")
    _food = _preset["values"].get("health.foodHealing", {}).get("boolean")
    if _bandage != _bandage_expected or _food is not _food_expected:
        _factory_errors.append(f"{_name}: medical rule drifted bandage={_bandage}, food={_food}")

if "LegacyFactoryPresetNames" not in _factory_service_text or "File.Delete(legacyPath)" not in _factory_service_text:
    _factory_errors.append("legacy factory preset migration cleanup missing")

if _factory_errors:
    fail("current 12 factory presets drifted: " + "; ".join(_factory_errors))
else:
    note("factory presets locked: 12 custom profiles / 219 values each; retired 4 files removed; hard-profile medical rules preserved")

# Current user-help contract no longer describes retired polar-radar gestures.
_control_help_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
for _required_help in ("public string ControlHelp", "IsVisualAggregateAnchor", "IsSpecialAbilityCooldownAnchor", "IsMutantHealthAnchor", "IsStashAmmoAnchor"):
    if _required_help not in _control_help_vm:
        fail(f"current type-aware control-help/aggregate classifier missing {_required_help!r}")
else:
    note("current control-help anchor registry present: aggregated editors and typed help; historical polar text not required")

# Current specialized editor source registration gate (2026-10-09).
# Previous 10/10 polar-radar legend geometry, wheel, font, and right-pane
# assertions are historical: current semantic editors are not all polar
# radars and Breakable Boxes is hosted by AggregateVisualControl.
# The approved interaction/shape rules now live in UI-DESIGN-LOCK.md and
# VISUAL-CONTROL-SYSTEM.md; the real CFG and UI checks run separately.
_semantic_radar_controls = [
    "MutantDropRadar.cs", "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs", "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs", "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs", "SpecialAbilityCooldownRadar.cs",
    "StashAmmoRadar.cs",
]
_semantic_editor_errors = []
_semantic_source_dir = ROOT / "src" / "TrueCustomDifficulty" / "Controls"
_semantic_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
for _name in _semantic_radar_controls:
    _path = _semantic_source_dir / _name
    _control_name = _path.stem
    if not _path.exists():
        _semantic_editor_errors.append(f"missing source {_name}")
        continue
    _control_text = _path.read_text(encoding="utf-8")
    if f"class {_control_name}" not in _control_text:
        _semantic_editor_errors.append(f"{_name}: class declaration missing")
    if f'<controls:{_control_name} x:Name=' not in _semantic_xaml:
        _semantic_editor_errors.append(f"{_name}: MainWindow registration missing")
if not (_semantic_source_dir / "AggregateVisualControl.cs").exists():
    _semantic_editor_errors.append("AggregateVisualControl.cs missing")
if not (DOCS / "UI-DESIGN-LOCK.md").exists() or not (DOCS / "VISUAL-CONTROL-SYSTEM.md").exists():
    _semantic_editor_errors.append("binding current UI lock documentation missing")
if (_semantic_source_dir / "AmmoDistributionRadar.cs").exists():
    _semantic_editor_errors.append("retired standalone AmmoDistributionRadar source reintroduced")
if _semantic_editor_errors:
    fail("semantic renderer registration drifted: " + "; ".join(_semantic_editor_errors))
else:
    note("semantic editor registration: 9 retained specialized classes + current AggregateVisualControl; no obsolete polar-only assumptions")


# Historical evidence (not shipped in current repo) - never report as verified.
not_run("old UI-CFG CSV snapshot (current tools/audit_ui_cfg_contract.py is authoritative): archived CSV/patch snapshot absent; current active catalog/manifest contracts checked elsewhere")

# XAML StaticResource integrity gate.
_xaml_files = list((ROOT / "src" / "TrueCustomDifficulty").rglob("*.xaml"))
_xaml_text = "\n".join(p.read_text(encoding="utf-8") for p in _xaml_files)
_xaml_declared_resources = set(re.findall(r'x:Key="([^"]+)"', _xaml_text))
_xaml_static_refs = set(re.findall(r'\{StaticResource\s+([^}\s]+)\}', _xaml_text))
_xaml_builtin_or_external = {"{x:Static"}
_xaml_missing_resources = sorted(ref for ref in _xaml_static_refs if ref not in _xaml_declared_resources)
if _xaml_missing_resources:
    fail(f"XAML StaticResource references missing declarations: {_xaml_missing_resources}")
else:
    note(f"XAML StaticResource integrity locked: {len(_xaml_static_refs)} references resolved")

# Global cross-domain control audit release gate (v0.2.50).
_global_doc = DOCS / "GLOBAL-CROSS-DOMAIN-AUDIT.md"
_global_visible = [p for p in parameters if p.get("exposeInUi", True)]
_global_hidden = [p for p in parameters if not p.get("exposeInUi", True)]
_global_visible_ids = {p.get("id") for p in _global_visible}
_global_tab_counts = {}
for _row in _global_visible:
    _tab = _row.get("tab")
    _global_tab_counts[_tab] = _global_tab_counts.get(_tab, 0) + 1
_global_expected_tabs = {
    "combat": 19,
    "health": 13,
    "stamina": 7,
    "survival": 32,
    "economy": 17,
    "equipment": 4,
    "master": 20,
    "environment": 7,
    "loot": 31,
    "alife": 2,
}
_global_confirmed = sum(p.get("support", {}).get("status") == "confirmed" for p in _global_visible)
_global_runtime = sum(p.get("support", {}).get("status") == "runtime_test_required" for p in _global_visible)
_global_control_counts = {}
for _row in _global_visible:
    _control = _row.get("control")
    _global_control_counts[_control] = _global_control_counts.get(_control, 0) + 1
_global_label_owners = {}
for _row in _global_visible:
    _label = _row.get("label")
    _global_label_owners.setdefault(_label, []).append(_row.get("id"))
_global_duplicate_labels = {label: ids for label, ids in _global_label_owners.items() if len(ids) > 1}
_global_out_of_range = []
for _row in _global_visible:
    _slider = _row.get("slider") or {}
    _lo, _hi = _slider.get("min"), _slider.get("max")
    if isinstance(_lo, (int, float)) and isinstance(_hi, (int, float)):
        for _name, _value in (_row.get("vanilla") or {}).items():
            if isinstance(_value, (int, float)) and not isinstance(_value, bool) and not (_lo - 1e-9 <= _value <= _hi + 1e-9):
                _global_out_of_range.append((_row.get("id"), _name, _value, _lo, _hi))
_global_generator_visible = _global_visible_ids & _e2e_all
_global_non_generator_visible = _global_visible_ids - _e2e_all
_global_non_generator_confirmed = sum(
    p.get("support", {}).get("status") == "confirmed"
    for p in _global_visible if p.get("id") in _global_non_generator_visible
)
_global_non_generator_runtime = sum(
    p.get("support", {}).get("status") == "runtime_test_required"
    for p in _global_visible if p.get("id") in _global_non_generator_visible
)

# A concrete CFG writer target may be touched by multiple edit rows for the same parameter,
# but never by two different parameter IDs.
_global_target_owners = {}
for _point in baseline.get("editPoints", []):
    _target = (
        _point.get("scope"),
        _point.get("file"),
        _point.get("prototype"),
        tuple(_point.get("nodePath") or []),
        _point.get("key"),
    )
    _global_target_owners.setdefault(_target, set()).add(_point.get("parameterId"))
_global_target_collisions = {
    target: sorted(owner for owner in owners if owner)
    for target, owners in _global_target_owners.items()
    if len({owner for owner in owners if owner}) > 1
}

_global_master_members_expected = {
    "advanced.lowHealthMechanics": "RegenHealthNegativeMechanics",
    "advanced.starvationMechanics": "HungerNegativeMechanics",
    "advanced.radiationMechanics": "RadiationNegativeMechanics",
    "advanced.fatigueMechanics": "SleepinessNegativeMechanics",
    "advanced.masterEffectModifierMechanics": "MasterEffectModifierNegativeMechanics",
}
_global_master_members_actual = {
    row.get("parameterId"): row.get("member")
    for row in (baseline.get("special", {}).get("listMembership") or [])
}

_global_dependency_expected = {
    "advanced.lowHealthMechanics": 1,
    "advanced.starvationMechanics": 5,
    "advanced.radiationMechanics": 1,
    "advanced.fatigueMechanics": 5,
    "advanced.masterEffectModifierMechanics": 3,
    "loot.worldWeaponShuffle": 1,
}
_global_dependency_actual = {}
_global_dependency_bad = []
_global_rows_by_id = {p.get("id"): p for p in parameters}
for _child in [p for p in _global_visible if p.get("requiresToggle")]:
    _parent_id = _child.get("requiresToggle")
    _global_dependency_actual[_parent_id] = _global_dependency_actual.get(_parent_id, 0) + 1
    _parent = _global_rows_by_id.get(_parent_id)
    if not _parent or _parent.get("control") != "toggle":
        _global_dependency_bad.append((_child.get("id"), _parent_id))

_global_positive_master_contract = {
    "health.masterLowHealthPenalty": (0.7, 1.0, 2),
    "survival.starvationHealthRegenPenalty": (1.0, 1.0, 2),
    "survival.severeFatigueMaxStamina": (0.2, 0.4, 2),
    "survival.severeFatigueCarryWeight": (0.2, 0.4, 4),
}
_global_positive_master_bad = []
for _pid, (_vanilla_expected, _max_expected, _point_count_expected) in _global_positive_master_contract.items():
    _row = _global_rows_by_id.get(_pid, {})
    _slider = _row.get("slider") or {}
    _vanilla = (_row.get("vanilla") or {}).get("All")
    _target_ops = [t.get("operation") for t in _row.get("targets", [])]
    _points = [e for e in baseline.get("editPoints", []) if e.get("parameterId") == _pid]
    if (
        _vanilla != _vanilla_expected
        or _slider.get("min") != 0.0
        or _slider.get("max") != _max_expected
        or not _target_ops
        or any(op != "negateSet" for op in _target_ops)
        or len(_points) != _point_count_expected
        or any(e.get("operation") != "negateSet" for e in _points)
    ):
        _global_positive_master_bad.append(_pid)

_global_negative_visible = []
for _row in _global_visible:
    _vanilla_values = (_row.get("vanilla") or {}).values()
    _slider = _row.get("slider") or {}
    if any(isinstance(v, (int, float)) and v < 0 for v in _vanilla_values):
        _global_negative_visible.append(_row.get("id"))
    elif isinstance(_slider.get("min"), (int, float)) and _slider.get("min") < 0:
        _global_negative_visible.append(_row.get("id"))

_global_vm_source = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_global_anomaly_ids = {
    "environment.electroAnomalyDamage",
    "environment.chemicalAnomalyDamage",
    "environment.fireAnomalyDamage",
    "environment.carouselAnomalyDamage",
    "environment.toxicCloudDamage",
    "environment.steamAnomalyDamage",
    "environment.clickerHealthDamage",
    "environment.razorHealthDamage",
    "environment.soapBubbleHealthDamage",
    "environment.fireballHealthDamage",
    "environment.diamondHealthDamage",
}
_global_anomaly_missing = sorted(pid for pid in _global_anomaly_ids if f'"{pid}"' not in _global_vm_source)

if len(parameters) != 244 or len(_global_visible) != 152 or len(_global_hidden) != 92:
    fail(f"global UI accounting drifted: catalog={len(parameters)} visible={len(_global_visible)} hidden={len(_global_hidden)}; expected 244/152/92")
elif _global_tab_counts != _global_expected_tabs:
    fail(f"global visible tab counts drifted: {_global_tab_counts}; expected {_global_expected_tabs}")
elif _global_confirmed != 120 or _global_runtime != 32:
    fail(f"global visible status split drifted: confirmed={_global_confirmed} runtime={_global_runtime}; expected 120/32")
elif _global_control_counts != {"slider": 138, "toggle": 12, "choiceSlider": 1, "range": 1}:
    fail(f"global visible control-type split drifted: {_global_control_counts}; expected 138 sliders / 12 toggles / 1 choiceSlider / 1 range")
elif _global_duplicate_labels:
    fail(f"duplicate player-facing labels detected: {_global_duplicate_labels}")
elif _global_out_of_range:
    fail(f"visible vanilla values escaped current slider ranges: {_global_out_of_range}")
elif (len(_global_generator_visible) + len(_global_non_generator_visible) != len(_global_visible)
      or _global_non_generator_confirmed + _global_non_generator_runtime != len(_global_non_generator_visible)):
    fail("global generator/non-generator partition/support coverage drifted")
elif _global_target_collisions:
    fail(f"cross-parameter concrete writer target collisions detected: {_global_target_collisions}")
elif _global_master_members_actual != _global_master_members_expected:
    fail(f"Master AdditionalMechanicsEffects membership contract drifted: {_global_master_members_actual}")
elif _global_dependency_actual != _global_dependency_expected or _global_dependency_bad:
    fail(f"Master dependency graph drifted: counts={_global_dependency_actual}, invalid={_global_dependency_bad}")
elif _global_positive_master_bad:
    fail(f"positive Master penalty/loss magnitude contract drifted: {_global_positive_master_bad}")
elif _global_negative_visible:
    fail(f"visible controls expose negative raw UI values again: {sorted(set(_global_negative_visible))}")
elif "RequiresPositiveMasterPenaltyMagnitudePresetMigration" not in _global_vm_source or "new Version(0, 2, 50)" not in _global_vm_source:
    fail("pre-v0.2.50 Master penalty/loss preset migration missing from canonical MainViewModel")
elif _global_anomaly_missing or "SetEffectiveDamageMultiplierProvider" not in _global_vm_source:
    fail(f"global anomaly layered-composition UI contract drifted; missing={_global_anomaly_missing}")
elif not _global_doc.exists():
    fail("global cross-domain audit document missing from docs/")
else:
    note("current cross-domain structural audit: 244 catalog / 152 visible / 92 hidden; 120 visible confirmed + 32 runtime-test-required; zero direct collisions")

# Historical evidence (not shipped in current repo) - never report as verified.
not_run("v0.2.51-v0.3.0 override/CFG-placement/provenance/production snapshots (current UI/Combat tools + active source checks are authoritative): archived CSV/patch snapshot absent; current active catalog/manifest contracts checked elsewhere")

# Canonical source boundary replaces the removed 0.3.0 fixed 178-control snapshot.
_canonical_src = ROOT / "src" / "TrueCustomDifficulty"
_legacy_source = []
for _source_path in ROOT.rglob("*"):
    if not _source_path.is_file():
        continue
    try:
        _source_path.relative_to(_canonical_src)
        continue
    except ValueError:
        pass
    # Standalone test projects are not duplicate production source trees.
    # Only explicitly registered Windows writer contract projects are exempt.
    if any(_source_path.is_relative_to(ROOT / "tests" / test_project)
           for test_project in ("CombatPatchContract", "HealthStaminaPatchContract", "SurvivalPatchContract", "EconomyPatchContract", "EquipmentPatchContract", "EnvironmentPatchContract", "ALifePatchContract", "LootDirectPatchContract", "LootSpecialGeneratorsContract", "LootWorldRemovalsContract", "LootWorldPlacementContract")):
        continue
    if _source_path.suffix.lower() in {".cs", ".xaml", ".csproj"}:
        _legacy_source.append(_source_path.relative_to(ROOT).as_posix())
if _legacy_source:
    fail("legacy C#/XAML/CSPROJ mirror outside canonical src tree: " + ", ".join(sorted(_legacy_source)))
else:
    note("current canonical C#/XAML/CSPROJ source tree has no duplicate mirrors")
# Canonical C# source must not contain duplicate top-level type definitions.
_cs_type_declarations = {}
_cs_type_pattern = re.compile(r"^(?:public|internal)\s+(?:(?:sealed|static|partial|abstract)\s+)*(?:class|record|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)\b", re.MULTILINE)
for _cs_path in _canonical_src.rglob("*.cs"):
    _cs_text = _cs_path.read_text(encoding="utf-8")
    for _match in _cs_type_pattern.finditer(_cs_text):
        _cs_name = _match.group(1)
        _cs_line = _cs_text.count("\n", 0, _match.start()) + 1
        _cs_type_declarations.setdefault(_cs_name, []).append(f"{_cs_path.relative_to(ROOT).as_posix()}:{_cs_line}")
_cs_duplicate_types = {name: locations for name, locations in _cs_type_declarations.items() if len(locations) > 1}
if _cs_duplicate_types:
    fail(f"duplicate top-level C# type definitions detected: {_cs_duplicate_types}")
else:
    note(f"canonical C# type uniqueness locked: {len(_cs_type_declarations)} top-level types / 0 duplicates")

# Keep application metadata aligned with the canonical publish project.
_app_version = str((parameters_doc.get("app") or {}).get("version", ""))
_csproj_version_match = re.search(r"<Version>([^<]+)</Version>", csproj)
_csproj_version = _csproj_version_match.group(1).strip() if _csproj_version_match else ""
if not _app_version or not _csproj_version_match:
    fail("application version metadata missing from catalog or canonical csproj")
elif _app_version != _csproj_version:
    fail(f"application version drifted: catalog={_app_version!r}, canonical csproj={_csproj_version!r}")
else:
    note(f"application version metadata synchronized: {_app_version}")

# Root stays intentionally lean; canonical audit/runtime data lives under Data/.
_required_root_files = {".gitignore", "README.md", "TrueCustomDifficulty.sln", "audit_project_consistency.py", "global.json", "publish.sh", "run-latest.sh"}
_optional_root_files = {"prompt.md"}  # local prompt may exist but is not needed for build/audit.
_actual_root_files = {p.name for p in ROOT.iterdir() if p.is_file()}
_root_missing = sorted(_required_root_files - _actual_root_files)
_root_unexpected = sorted(_actual_root_files - _required_root_files - _optional_root_files)
if _root_missing or _root_unexpected:
    fail(f"repository root file set drifted: missing={_root_missing}; unexpected={_root_unexpected}")
elif 'PUBLISH_DIR="$ROOT/publish/win-x64"' not in (ROOT / "publish.sh").read_text(encoding="utf-8"):
    fail("publish.sh no longer isolates runtime output under publish/win-x64")
else:
    note(f"current root/publish layout: required {len(_required_root_files)} files present; optional prompt.md not required; no unexpected root files")

print("True Custom Difficulty — current-source consistency audit")
for msg in notes:
    print(f"[OK] {msg}")
for msg in not_run_items:
    print(f"[NOT RUN] {msg}")
if errors:
    for msg in errors:
        print(f"[FAIL] {msg}")
    sys.exit(1)
print(f"PASS — active static checks only; {len(not_run_items)} archived/runtime evidence checks NOT RUN")
