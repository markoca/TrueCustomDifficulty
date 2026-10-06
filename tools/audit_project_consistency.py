#!/usr/bin/env python3
"""Static consistency checks for the source package; requires only Python 3."""
from __future__ import annotations
import json
import re, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
DOCS = ROOT / "docs"
CSPROJ = ROOT / "src" / "TrueCustomDifficulty" / "TrueCustomDifficulty.csproj"

errors: list[str] = []
notes: list[str] = []

def fail(msg: str) -> None: errors.append(msg)
def note(msg: str) -> None: notes.append(msg)

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

# Decision-layer CSV and JSON summary must describe the same current triage state.
import json as _json
triage_csv = ROOT / "docs" / "generators" / "control-candidate-triage.csv"
triage_summary = ROOT / "docs" / "generators" / "control-candidate-triage-summary.json"
if triage_csv.exists() and triage_summary.exists():
    import csv as _csv
    from collections import Counter as _Counter
    with triage_csv.open(encoding="utf-8-sig", newline="") as fh:
        _triage_rows = list(_csv.DictReader(fh))
    _summary = _json.loads(triage_summary.read_text(encoding="utf-8"))
    _counts = dict(sorted(_Counter(row["status"] for row in _triage_rows).items()))
    # v0.2.41: docs/generators is the single canonical generator evidence tree.
    # Legacy root/generators mirrors were removed to prevent silent stale-copy drift.
    if (ROOT / "generators").exists():
        fail("legacy top-level generators/ mirror returned; docs/generators/ is canonical")
    else:
        note("generator evidence layout canonical: docs/generators/ only (no legacy generators/ mirror)")
    if _summary.get("version") != "0.2.40":
        fail(f"triage summary version is {_summary.get('version')!r}; expected '0.2.40'")
    if _summary.get("triage_concepts") != len(_triage_rows):
        fail("triage summary concept count does not match control-candidate-triage.csv")
    if _summary.get("status_counts") != _counts:
        fail("triage summary status counts do not match control-candidate-triage.csv")
    _junk = [row for row in _triage_rows if row.get("control_id") == "loot.npcJunkMisc"]
    if len(_junk) != 1 or _junk[0].get("status") != "NO_UI_SYSTEM_MARKER":
        fail("Human NPC Junk/Misc triage decision is missing or not NO_UI_SYSTEM_MARKER")
    else:
        note(f"decision-layer summary synchronized: {len(_triage_rows)} concepts; NPC Junk/Misc closed as system marker")

# GuardQuestItem is interaction infrastructure, not tunable loot.
junk_rows_path = ROOT / "docs" / "generators" / "npc-junk-guardquest-generator-rows.csv"
context_rows_path = ROOT / "docs" / "generators" / "guardquest-contextual-action-preconditions.csv"
if junk_rows_path.exists():
    import csv as _csv
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
    import csv as _csv
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
    import csv as _csv
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
    import csv as _csv
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
    import csv as _csv
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
    import csv as _csv
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
    import csv as _csv
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
    import csv as _csv
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
    import csv as _csv
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

# Isolated runtime-test tooling (v0.2.36).
_rt_gen = ROOT / "tools" / "runtime_tests" / "generate_runtime_test_patches.py"
_rt_readme = ROOT / "tools" / "runtime_tests" / "README.md"
_rt_doc = ROOT / "docs" / "runtime-tests" / "README.md"
if not _rt_gen.exists() or not _rt_readme.exists() or not _rt_doc.exists():
    fail(f"runtime-test tooling incomplete: generator={_rt_gen.exists()}, readme={_rt_readme.exists()}, docs={_rt_doc.exists()}")
else:
    _rt_text = _rt_gen.read_text(encoding="utf-8")
    _required = [
        '"weapon-condition"', '"weapon-ammo"', '"medic-stock"',
        '"0.01"', '"0.95"', '"77"', '"20"',
        'expected 121 rows / 33 roots', 'expected 48/25/23'
    ]
    _missing_rt = [x for x in _required if x not in _rt_text]
    if _missing_rt:
        fail(f"runtime-test generator contract drifted; missing markers: {_missing_rt}")
    else:
        note("isolated runtime-test generator present: NPC condition direct MIN/MAX range, ammo 1/77, medic 1/20; production UI/writer enabled; runtime status remains unverified")


# Ready-to-install runtime-test packaging (v0.2.37).
_rt_pack = ROOT / "tools" / "runtime_tests" / "build_runtime_test_mod.py"
if not _rt_pack.exists():
    fail("runtime-test packer missing")
else:
    _rt_pack_text = _rt_pack.read_text(encoding="utf-8")
    _pack_required = [
        'REPAK_VERSION = "V11"', 'RETOC_VERSION = "UE5_5"',
        'MOUNT_POINT = "../../../"', 'TCD_RuntimeTest_',
        'validate_triplet', '"repak"', '"retoc"'
    ]
    _pack_missing = [x for x in _pack_required if x not in _rt_pack_text]
    if _pack_missing:
        fail(f"runtime-test packer contract drifted; missing markers: {_pack_missing}")
    else:
        note("runtime-test packer present: production-equivalent repak V11 + ../../../ + retoc UE5_5; triplet validation required")


# Former runtime candidates promoted to production writer/UI (v0.2.42), still runtime-unverified.
_prod_expected = {
    "loot.droppedWeaponCondition": (242, {"MinDurability", "MaxDurability"}),
    "loot.npcWeaponAmmoSupply": (242, {"AmmoMinCount", "AmmoMaxCount", "AmmoMaxcount"}),
    "economy.medicStock": (96, {"MinCount", "MaxCount"}),
}
for _pid, (_count, _keys) in _prod_expected.items():
    _pts = [p for p in baseline.get("editPoints", []) if p.get("parameterId") == _pid]
    _protos = {p.get("prototype") for p in _pts}
    _actual_keys = {p.get("key") for p in _pts}
    if len(_pts) != _count:
        fail(f"{_pid} production surface drifted: {len(_pts)} edit points; expected {_count}")
    elif _pid.startswith("loot.") and len(_protos) != 33:
        fail(f"{_pid} production roots drifted: {len(_protos)}; expected 33")
    elif _pid == "economy.medicStock" and _protos != {"MedicUniversal_TradeItemGenerator", "MedicNoon_TradeItemGenerator"}:
        fail(f"medic stock production roots drifted: {sorted(_protos)}")
    elif not _actual_keys.issubset(_keys) or not _keys.issuperset(_actual_keys):
        fail(f"{_pid} production field set drifted: {sorted(_actual_keys)}")
else:
    note("promoted runtime-gated production surfaces locked: condition 242 edits / ammo 242 edits on 33 roots; medic 96 edits on 48 rows")


# Non-generator normal-UI coverage gate (v0.2.43).
_non_gen_expected = {
    "combat": (19, 14, 5),
    "health": (13, 10, 3),
    "stamina": (7, 6, 1),
    "survival": (23, 23, 0),
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
    note("non-generator UI coverage locked: 83 visible controls across Combat/Health/Stamina/Survival/Economy/Equipment; Mutant Health + Mutant Special Attack Cooldown + Healing Amount + Action Costs + Item Price Scalers aggregated")

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


# World attachment progression-tier audit gate (v0.9.26.44).
_wa_tier_csv = DOCS / "generators" / "world-attachment-progression-tier-audit.csv"
_wa_tier_doc = DOCS / "generators" / "WORLD-ATTACHMENT-PROGRESSION-TIER-AUDIT.md"
if not _wa_tier_csv.exists() or not _wa_tier_doc.exists():
    fail("world attachment progression-tier audit evidence missing")
else:
    import csv as _wa_csv
    with _wa_tier_csv.open(encoding="utf-8", newline="") as _f:
        _wa_rows = list(_wa_csv.DictReader(_f))
    _wa_ids = {r.get("item_sid") for r in _wa_rows}
    _wa_tiers = {r.get("tier") for r in _wa_rows}
    _wa_counts = {}
    for _r in _wa_rows:
        _wa_counts[_r.get("tier")] = _wa_counts.get(_r.get("tier"), 0) + 1
    if len(_wa_rows) != 39 or len(_wa_ids) != 39:
        fail(f"world attachment tier audit must contain 39 unique rows; got {len(_wa_rows)}/{len(_wa_ids)}")
    elif not _wa_tiers <= {"T1", "T2", "T3", "T4"}:
        fail(f"world attachment tier audit contains invalid tiers: {_wa_tiers}")
    elif _wa_counts != {"T2": 18, "T3": 12, "T4": 9}:
        fail(f"world attachment tier distribution drifted: {_wa_counts}; expected T2=18/T3=12/T4=9")
    elif any(not r.get("confidence") or not r.get("rationale") for r in _wa_rows):
        fail("world attachment tier audit contains rows without confidence/rationale")
    else:
        note("world attachment progression tiers locked: 39 models = 18 T2 / 12 T3 / 9 T4; category+same/±1 tier is candidate shuffle boundary")


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


# Loose world detector audit gate (v0.9.26.44).
_wd_loose = DOCS / "generators" / "world-detector-loose-placement-audit.csv"
_wd_domain = DOCS / "generators" / "world-detector-master-domain-audit.csv"
if not _wd_loose.exists() or not _wd_domain.exists():
    fail("loose world detector audit evidence missing")
else:
    import csv as _wd_csv
    with _wd_loose.open(encoding="utf-8", newline="") as _f:
        _wd_rows = list(_wd_csv.DictReader(_f))
    _wd_safe = [r for r in _wd_rows if r.get("classification") == "SAFE_LOOSE_WORLD"]
    _wd_excluded = [r for r in _wd_rows if r.get("classification") == "DEFERRED_EXCLUDED"]
    _wd_base = [r for r in _wd_safe if r.get("scope") == "base"]
    _wd_dlc = [r for r in _wd_safe if r.get("scope") == "dlc1"]
    _wd_items = {}
    for _r in _wd_safe:
        _wd_items[_r["item_sid"]] = _wd_items.get(_r["item_sid"], 0) + 1
    if len(_wd_rows) != 8 or len(_wd_safe) != 7 or len(_wd_excluded) != 1:
        fail(f"loose detector surface drifted: rows/safe/excluded={len(_wd_rows)}/{len(_wd_safe)}/{len(_wd_excluded)}; expected 8/7/1")
    elif len(_wd_base) != 4 or len(_wd_dlc) != 3:
        fail(f"loose detector scope split drifted: base/dlc={len(_wd_base)}/{len(_wd_dlc)}; expected 4/3")
    elif _wd_items != {"Echo": 2, "Gilka": 5}:
        fail(f"loose detector multiset drifted: {_wd_items}; expected Echo=2/Gilka=5")
    elif _wd_excluded[0].get("spawn_on_start", "").lower() != "false":
        fail("loose detector excluded actor is no longer the SpawnOnStart=false Gilka")
    else:
        note("loose detector audit locked: 4 base / 7 DLC1-inclusive active WorldMap pickups; exact multiset Echo=2 + Gilka=5; one deferred Gilka excluded")

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







# Mutant health production pairing gate (v0.9.26.44).
_mhp_pairs = [
    ("combat.mutantHealth.bloodsucker", "combat.mutantCooldown.bloodsucker", {"Bloodsucker":"500","MistBloodsucker":"1000"}),
    ("combat.mutantHealth.chimera", "combat.mutantCooldown.chimera", {"Chimera":"1400"}),
    ("combat.mutantHealth.deer", "combat.mutantCooldown.deer", {"Deer":"600"}),
    ("combat.mutantHealth.pseudogiant", "combat.mutantCooldown.pseudogiant", {"Pseudogiant":"2500"}),
    ("combat.mutantHealth.pseudodog", "combat.mutantCooldown.pseudodog", {"PseudoDog":"300"}),
    ("combat.mutantHealth.burer", "combat.mutantCooldown.burer", {"Burer":"400","Burer_ShootingSpecial":"400"}),
    ("combat.mutantHealth.bayun", "combat.mutantCooldown.bayun", {"Bayun":"400"}),
    ("combat.mutantHealth.controller", "combat.mutantCooldown.controller", {"Controller":"500"}),
]
_mhp_rows_by_id = {row.get("id"): row for row in parameters}
_mhp_points = [p for p in baseline.get("editPoints", []) if p.get("parameterId", "").startswith("combat.mutantHealth.")]
_mhp_actual_points = {}
for _p in _mhp_points:
    _mhp_actual_points.setdefault(_p["parameterId"], {})[_p["prototype"]] = _p["sourceRaw"]

_mhp_sort_source = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_mhp_missing = []
for _health_id, _cooldown_id, _expected_targets in _mhp_pairs:
    _health = _mhp_rows_by_id.get(_health_id)
    _cooldown = _mhp_rows_by_id.get(_cooldown_id)
    if _health is None or _cooldown is None:
        _mhp_missing.append((_health_id, _cooldown_id))
        continue
    if _health.get("tab") != "combat" or _health.get("group") != "Mutant Health":
        _mhp_missing.append((_health_id, "health placement"))
    if _cooldown.get("group") != "Mutant Special Attack Cooldown":
        _mhp_missing.append((_cooldown_id, "cooldown placement"))
    if _health.get("support", {}).get("status") != "confirmed":
        _mhp_missing.append((_health_id, "support status"))
    _cooldown_label = str(_cooldown.get("label", ""))
    if _cooldown_id == "combat.mutantCooldown.bloodsucker":
        if _cooldown_label != "Mutant Special Attack Cooldown":
            _mhp_missing.append((_cooldown_id, "aggregate cooldown label"))
    elif not _cooldown_label.endswith("Special Ability Cooldown"):
        _mhp_missing.append((_cooldown_id, "player-facing cooldown label"))
    if _mhp_actual_points.get(_health_id) != _expected_targets:
        _mhp_missing.append((_health_id, _mhp_actual_points.get(_health_id)))

if _mhp_rows_by_id.get("combat.mutantHealth.bloodsucker", {}).get("label") != "Mutant Health":
    fail("Mutant Health aggregate anchor label drifted")
elif "Mist Bloodsucker" in _mhp_rows_by_id.get("combat.mutantCooldown.bloodsucker", {}).get("label", ""):
    fail("Bloodsucker cooldown player-facing label must not expose Mist Bloodsucker")
elif _mhp_missing:
    fail(f"mutant health/cooldown pairing drifted: {_mhp_missing}")
elif len(_mhp_points) != 10:
    fail(f"mutant health MaxHP edit-point count drifted: {len(_mhp_points)}; expected 10")
elif any(p.get("file") != "GameData/ObjPrototypes.cfg" or p.get("key") != "MaxHP" or p.get("operation") != "scale" for p in _mhp_points):
    fail("mutant health controls no longer map directly to ObjPrototypes MaxHP scale edits")
elif '"combat.mutantHealth.bloodsucker" => 130' not in _mhp_sort_source or '"combat.mutantCooldown.bloodsucker" => 140' not in _mhp_sort_source:
    fail("mutant health radar / cooldown-list Combat ordering missing")
else:
    note("mutant health production locked: 8 confirmed health multipliers / 10 direct MaxHP edits; health and special-ability cooldowns are separate aggregate radars")

# Mutant Health radar UI gate (v0.9.26.44).
_mhr_ids = [
    "combat.mutantHealth.bloodsucker",
    "combat.mutantHealth.chimera",
    "combat.mutantHealth.deer",
    "combat.mutantHealth.pseudogiant",
    "combat.mutantHealth.pseudodog",
    "combat.mutantHealth.burer",
    "combat.mutantHealth.bayun",
    "combat.mutantHealth.controller",
]
_mhr_rows_by_id = {row.get("id"): row for row in parameters}
_mhr_rows = [_mhr_rows_by_id.get(pid) for pid in _mhr_ids]
_mhr_anchor = _mhr_rows_by_id.get("combat.mutantHealth.bloodsucker")
_mhr_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "MutantHealthRadar.cs"
_mhr_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_mhr_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_mhr_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _mhr_rows):
    fail("Mutant Health radar backing parameter set is incomplete")
elif sum(1 for row in _mhr_rows if row.get("exposeInUi", True)) != 1 or not _mhr_anchor.get("exposeInUi", True):
    fail("Mutant Health must expose exactly one aggregate radar card")
elif _mhr_anchor.get("label") != "Mutant Health":
    fail(f"Mutant Health aggregate label drifted: {_mhr_anchor.get('label')}")
elif any(
    row.get("group") != "Mutant Health"
    or row.get("control") != "slider"
    or (row.get("vanilla") or {}).get("All") != 1.0
    or (row.get("slider") or {}).get("min") != 0.25
    or (row.get("slider") or {}).get("max") != 3.0
    or (row.get("slider") or {}).get("step") != 0.05
    for row in _mhr_rows
):
    fail("Mutant Health radar backing controls drifted from 0.25x-3x / 0.05x / vanilla 1x contract")
elif not _mhr_control.exists():
    fail("MutantHealthRadar.cs is missing")
else:
    _mhr_text = _mhr_control.read_text(encoding="utf-8")
    if "Eight-axis editor" not in _mhr_text:
        fail("MutantHealthRadar lost its eight-axis contract")
    elif 'new("combat.mutantHealth.bloodsucker", "Bloodsucker", -90' not in _mhr_text:
        fail("MutantHealthRadar Bloodsucker axis missing")
    elif 'new("combat.mutantHealth.controller", "Controller", 225' not in _mhr_text:
        fail("MutantHealthRadar Controller axis missing")
    elif "RadarStep = 0.05" not in _mhr_text or "MinimumMultiplier = 0.25" not in _mhr_text or "MaximumMultiplier = 3.0" not in _mhr_text:
        fail("MutantHealthRadar range/step contract drifted")
    elif "Effective" not in _mhr_text or "Vanilla" not in _mhr_text or "Maximum health" not in _mhr_text:
        fail("MutantHealthRadar lost multiplier/effective-HP/vanilla information")
    elif "500 / 1000 HP" not in _mhr_text or "2500 HP" not in _mhr_text:
        fail("MutantHealthRadar authored HP references drifted")
    elif '<controls:MutantHealthRadar x:Name="MutantHealthRadarControl"' not in _mhr_xaml:
        fail("MainWindow does not render Mutant Health radar")
    elif "mutantHealthParameterIds.Contains(p.Id)" not in _mhr_main_vm:
        fail("hidden Mutant Health backing parameters are not loaded into live VM graph")
    elif "IsMutantHealthAnchor" not in _mhr_param_vm or "IsMutantHealthMember" not in _mhr_param_vm:
        fail("Mutant Health aggregate/backing visibility contract missing")
    else:
        note("Mutant Health radar locked: 8 confirmed MaxHP multipliers -> 1 radar; 0.05x step and effective HP information preserved")

_sac_ids = [
    "combat.mutantCooldown.bloodsucker",
    "combat.mutantCooldown.chimera",
    "combat.mutantCooldown.deer",
    "combat.mutantCooldown.pseudogiant",
    "combat.mutantCooldown.pseudodog",
    "combat.mutantCooldown.burer",
    "combat.mutantCooldown.bayun",
    "combat.mutantCooldown.controller",
]
_sac_rows = [_mhr_rows_by_id.get(pid) for pid in _sac_ids]
_sac_anchor = _mhr_rows_by_id.get("combat.mutantCooldown.bloodsucker")
_sac_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "SpecialAbilityCooldownRadar.cs"

if any(row is None for row in _sac_rows):
    fail("Special Ability Cooldowns backing parameter set is incomplete")
elif sum(1 for row in _sac_rows if row.get("exposeInUi", True)) != 1 or not _sac_anchor.get("exposeInUi", True):
    fail("Special Ability Cooldowns must expose exactly one aggregate radar card")
elif _sac_anchor.get("label") != "Mutant Special Attack Cooldown":
    fail(f"Mutant Special Attack Cooldown aggregate label drifted: {_sac_anchor.get('label')}")
elif any(
    row.get("group") != "Mutant Special Attack Cooldown"
    or row.get("control") != "slider"
    or (row.get("slider") or {}).get("min") != 0.25
    or (row.get("slider") or {}).get("max") != 2.0
    or (row.get("slider") or {}).get("step") != 0.01
    or (row.get("vanilla") or {}).get("Novice") != 1.0
    or (row.get("vanilla") or {}).get("Stalker") != 1.0
    or (row.get("vanilla") or {}).get("Veteran") != 1.0
    or (row.get("vanilla") or {}).get("Master") != 0.5
    for row in _sac_rows
):
    fail("Special Ability Cooldowns radar backing controls drifted from 0.25x-2x / 0.01x / N-S-V 1x / Master 0.5x contract")
elif not _sac_control.exists():
    fail("SpecialAbilityCooldownRadar.cs is missing")
else:
    _sac_text = _sac_control.read_text(encoding="utf-8")
    if "Eight-axis editor" not in _sac_text:
        fail("SpecialAbilityCooldownRadar lost its eight-axis contract")
    elif 'new("combat.mutantCooldown.bloodsucker", "Bloodsucker", -90)' not in _sac_text:
        fail("SpecialAbilityCooldownRadar Bloodsucker axis missing")
    elif 'new("combat.mutantCooldown.controller", "Controller", 225)' not in _sac_text:
        fail("SpecialAbilityCooldownRadar Controller axis missing")
    elif "0.5x MASTER VANILLA" not in _sac_text or "1x N/S/V VANILLA" not in _sac_text:
        fail("SpecialAbilityCooldownRadar lost its dual vanilla reference contract")
    elif "parameter.ReferenceNumber" not in _sac_text or "parameter.ResetToReference()" not in _sac_text:
        fail("SpecialAbilityCooldownRadar no longer follows selected Base Preset for modified/reset state")
    elif "lower = special abilities return sooner" not in _sac_text or "frequency only, not damage" not in _sac_text:
        fail("SpecialAbilityCooldownRadar lost cooldown meaning information")
    elif "parameter.TickFrequency" not in _sac_text or "MidpointRounding.AwayFromZero" not in _sac_text:
        fail("SpecialAbilityCooldownRadar no longer preserves original 0.01x step")
    elif '<controls:SpecialAbilityCooldownRadar x:Name="SpecialAbilityCooldownRadarControl"' not in _mhr_xaml:
        fail("MainWindow does not render Special Ability Cooldowns radar")
    elif "specialAbilityCooldownParameterIds.Contains(p.Id)" not in _mhr_main_vm:
        fail("hidden Special Ability Cooldowns backing parameters are not loaded into live VM graph")
    elif "IsSpecialAbilityCooldownAnchor" not in _mhr_param_vm or "IsSpecialAbilityCooldownMember" not in _mhr_param_vm:
        fail("Special Ability Cooldowns aggregate/backing visibility contract missing")
    else:
        note("Mutant Special Attack Cooldown radar locked: 8 cooldown multipliers -> 1 radar; selected Base Preset reset, vanilla profile, 0.01x step and meaning preserved")

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

# Full regular ammo caliber audit gate (v0.9.26.44).
_rc_path = DOCS / "generators" / "all-regular-ammo-caliber-audit.csv"
_rc_exclusions_path = DOCS / "generators" / "ammo-caliber-exclusions.csv"
_rc_doc = DOCS / "generators" / "FULL-REGULAR-AMMO-CALIBER-AUDIT.md"
if not (_rc_path.exists() and _rc_exclusions_path.exists() and _rc_doc.exists()):
    fail("full regular ammo caliber audit evidence missing")
else:
    import csv as _rc_csv
    with _rc_path.open(encoding="utf-8", newline="") as _f:
        _rc_rows = list(_rc_csv.DictReader(_f))
    with _rc_exclusions_path.open(encoding="utf-8", newline="") as _f:
        _rc_exclusions = list(_rc_csv.DictReader(_f))

    _rc_current = {r["default_ammo_sid"] for r in _rc_rows if r["current_breakable_box_status"] == "CURRENT"}
    _rc_add = {r["default_ammo_sid"] for r in _rc_rows if r["current_breakable_box_status"] == "ADD_CANDIDATE"}
    _rc_expected_current = {"A918D","A919D","A045D","A545D","A556D","A012D"}
    _rc_expected_add = {"A939D","A762NATOD","A762SniperD"}
    _rc_excluded = {r["sid"]:r["reason"] for r in _rc_exclusions}

    if len(_rc_rows) != 9:
        fail(f"regular ammo caliber audit drifted: {len(_rc_rows)} rows; expected 9")
    elif _rc_current != _rc_expected_current:
        fail(f"current destructible caliber set drifted: {_rc_current}")
    elif _rc_add != _rc_expected_add:
        fail(f"additional proven caliber candidates drifted: {_rc_add}")
    elif _rc_excluded.get("A762D") != "EXCLUDE_UNPROVEN_WEAPON_CALIBER":
        fail("7.62x39 exclusion evidence drifted")
    elif {r["caliber_label"] for r in _rc_rows} != {
        "9×18 mm","9×19 mm",".45 ACP","5.45×39 mm","5.56×45 mm",
        "9×39 mm","7.62×51 mm","7.62×54 mm","12×70 Buckshot"
    }:
        fail("player-facing regular caliber labels drifted")
    else:
        note("full regular ammo caliber audit locked: 9 proven regular calibers; 6 current box calibers + 3 add candidates; 7.62x39 excluded pending a proven weapon consumer")


# Mutant Part Drop Chances radar production gate (v0.9.26.44).
_mutant_ids = [
    "loot.mutant.blindDogChance", "loot.mutant.bloodsuckerChance", "loot.mutant.boarChance", "loot.mutant.burerChance",
    "loot.mutant.catChance", "loot.mutant.chimeraChance", "loot.mutant.controllerChance", "loot.mutant.deerChance",
    "loot.mutant.fleshChance", "loot.mutant.poltergeistChance", "loot.mutant.pseudodogChance", "loot.mutant.pseudogiantChance",
    "loot.mutant.snorkChance", "loot.mutant.tushkanChance",
]
_mutant_rows_by_id = {row.get("id"): row for row in parameters}
_mutant_rows = [_mutant_rows_by_id.get(pid) for pid in _mutant_ids]
_mutant_anchor = _mutant_rows_by_id.get("loot.mutant.blindDogChance")
_mutant_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "MutantDropRadar.cs"
_mutant_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_mutant_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_mutant_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _mutant_rows):
    fail("Mutant drop backing family parameter set is incomplete")
elif sum(1 for row in _mutant_rows if row.get("exposeInUi", True)) != 1 or not _mutant_anchor.get("exposeInUi", True):
    fail("Mutant Part Drop Chances must expose exactly one aggregate radar card")
elif _mutant_anchor.get("label") != "Mutant Part Drop Chances":
    fail(f"Mutant Part Drop Chances label drifted: {_mutant_anchor.get('label')}")
elif any(
    row.get("control") != "slider"
    or (row.get("slider") or {}).get("min") != 0.0
    or (row.get("slider") or {}).get("max") != 1.0
    or (row.get("slider") or {}).get("step") != 0.01
    for row in _mutant_rows
):
    fail("Mutant drop family slider contract drifted from 0%-100% / 1% step")
elif not _mutant_control.exists():
    fail("MutantDropRadar.cs is missing")
else:
    _mutant_control_text = _mutant_control.read_text(encoding="utf-8")
    if "Fourteen-axis editor" not in _mutant_control_text or 'new("loot.mutant.blindDogChance", "Blind Dog", -90)' not in _mutant_control_text:
        fail("MutantDropRadar implementation is incomplete")
    elif 'new("loot.mutant.catChance", "Bayun", 12.857)' not in _mutant_control_text:
        fail("MutantDropRadar lost the Bayun player-facing label")
    elif "vanilla" not in _mutant_control_text or "Arrow keys adjust selected mutant" not in _mutant_control_text:
        fail("MutantDropRadar lost its vanilla legend or usability guidance")
    elif "TryGetVanillaValue" not in _mutant_control_text or "element.GetDouble()" not in _mutant_control_text:
        fail("MutantDropRadar no longer converts JsonElement vanilla values to numeric doubles explicitly")
    elif '<controls:MutantDropRadar x:Name="MutantDropRadarControl"' not in _mutant_xaml:
        fail("MainWindow does not render the Mutant Part Drop Chances radar")
    elif "mutantDropParameterIds.Contains(p.Id)" not in _mutant_main_vm:
        fail("hidden mutant-drop backing parameters are not loaded into the live VM graph")
    elif "parameter.IsModified" in _mutant_main_vm[_mutant_main_vm.find("mutantDropAnchor"):_mutant_main_vm.find("var dependencyChildren")]:
        fail("mutant-drop aggregate modified provider recursively calls the anchor IsModified state")
    elif "IsMutantDropAnchor" not in _mutant_param_vm or "IsMutantDropMember" not in _mutant_param_vm:
        fail("mutant-drop aggregate/backing visibility contract is missing")
    else:
        note("Mutant Part Drop Chances locked: 14 existing per-mutant drop sliders -> 1 visible radar; original writer routes unchanged")

# Environment anomaly ordering gate (v0.9.26.44).
_env_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
if '"environment.anomalyDamage" => 900' not in _env_vm or '"environment.electroAnomalyDamage" => 910' not in _env_vm:
    fail("Environment anomaly ordering drifted: Global must be immediately above Individual at the bottom")
else:
    note("Environment ordering locked: Global Anomaly Damage immediately above Individual Anomaly Damage at tab bottom")

# Individual Anomaly Damage radar production gate (v0.9.26.44).
_iad_ids = [
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
]
_iad_rows_by_id = {row.get("id"): row for row in parameters}
_iad_rows = [_iad_rows_by_id.get(pid) for pid in _iad_ids]
_iad_anchor = _iad_rows_by_id.get("environment.electroAnomalyDamage")
_iad_global = _iad_rows_by_id.get("environment.anomalyDamage")
_iad_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "AnomalyDamageRadar.cs"
_iad_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_iad_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_iad_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _iad_rows):
    fail("Individual Anomaly Damage backing parameter set is incomplete")
elif sum(1 for row in _iad_rows if row.get("exposeInUi", True)) != 1 or not _iad_anchor.get("exposeInUi", True):
    fail("Individual Anomaly Damage must expose exactly one aggregate radar card")
elif _iad_anchor.get("label") != "Individual Anomaly Damage":
    fail(f"Individual Anomaly Damage aggregate label drifted: {_iad_anchor.get('label')}")
elif not _iad_global or not _iad_global.get("exposeInUi", True) or _iad_global.get("label") != "Global Anomaly Damage":
    fail("Global Anomaly Damage must remain a separate visible slider")
elif not _iad_control.exists():
    fail("AnomalyDamageRadar.cs is missing")
else:
    _iad_control_text = _iad_control.read_text(encoding="utf-8")
    if "Eleven-axis editor" not in _iad_control_text:
        fail("AnomalyDamageRadar implementation lost its eleven-axis contract")
    elif 'new("environment.electroAnomalyDamage", "Electro", -90)' not in _iad_control_text:
        fail("AnomalyDamageRadar Electro axis missing")
    elif 'new("environment.diamondHealthDamage", "Diamond", 237.273)' not in _iad_control_text:
        fail("AnomalyDamageRadar Diamond axis missing")
    elif "1x = VANILLA" not in _iad_control_text or "MaximumMultiplier = 2.0" not in _iad_control_text:
        fail("AnomalyDamageRadar lost its normalized 0x-2x vanilla-relative scale")
    elif "parameter.CurrentNumber = vanilla * multiplier" not in _iad_control_text:
        fail("AnomalyDamageRadar no longer maps normalized axes back to existing raw CFG values")
    elif "parameter.EffectiveDisplay" not in _iad_control_text:
        fail("AnomalyDamageRadar legend no longer shows effective damage after Global Anomaly Damage")
    else:
        _iad_keydown_start = _iad_control_text.find("protected override void OnKeyDown")
        _iad_keydown_end = _iad_control_text.find("private void UpdateAxisFromPoint", _iad_keydown_start)
        _iad_keydown_text = _iad_control_text[_iad_keydown_start:_iad_keydown_end]
        if "foreach (var parameter in _parameters)" in _iad_keydown_text:
            fail("AnomalyDamageRadar reintroduced CS0136-prone parameter shadowing in reset handling")
        elif '<controls:AnomalyDamageRadar x:Name="AnomalyDamageRadarControl"' not in _iad_xaml:
            fail("MainWindow does not render the Individual Anomaly Damage radar")
        elif "individualAnomalyDamageParameterIds.Contains(p.Id)" not in _iad_main_vm:
            fail("hidden individual anomaly-damage backing parameters are not loaded into the live VM graph")
        elif "SetEffectiveDamageMultiplierProvider" not in _iad_main_vm or "globalAnomalyDamage.CurrentNumber" not in _iad_main_vm:
            fail("Global Anomaly Damage composition was disconnected from individual anomaly damage controls")
        elif "IsIndividualAnomalyDamageAnchor" not in _iad_param_vm or "IsIndividualAnomalyDamageMember" not in _iad_param_vm:
            fail("individual anomaly-damage aggregate/backing visibility contract is missing")
        else:
            note("Individual Anomaly Damage locked: 11 direct controls -> 1 normalized radar; Global Anomaly Damage remains separate")

# Stash Ammo Amounts radar production gate (v0.9.26.44).
_sar_ids = [
    "loot.cheapStashAmmoQuantity",
    "loot.mediumStashAmmoQuantity",
    "loot.expensiveStashAmmoQuantity",
]
_sar_rows_by_id = {row.get("id"): row for row in parameters}
_sar_rows = [_sar_rows_by_id.get(pid) for pid in _sar_ids]
_sar_anchor = _sar_rows_by_id.get("loot.cheapStashAmmoQuantity")
_sar_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "StashAmmoRadar.cs"
_sar_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_sar_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_sar_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _sar_rows):
    fail("Stash Ammo Amounts radar parameter set is incomplete")
elif _sar_anchor.get("label") != "Stash Ammo Amounts":
    fail(f"Stash Ammo Amounts aggregate label drifted: {_sar_anchor.get('label')}")
elif not _sar_anchor.get("exposeInUi", True):
    fail("Stash Ammo Amounts aggregate anchor must remain visible")
elif any(row.get("exposeInUi", True) for row in _sar_rows[1:]):
    fail("Standard/Rare stash ammo backing controls must remain hidden behind the aggregate radar")
elif any(
    row.get("control") != "slider"
    or (row.get("vanilla") or {}).get("All") != 1.0
    or (row.get("slider") or {}).get("min") != 0.25
    or (row.get("slider") or {}).get("max") != 2.0
    or (row.get("slider") or {}).get("step") != 0.25
    for row in _sar_rows
):
    fail("Stash Ammo Amounts radar contract drifted from 0.25x-2x / 0.25x / vanilla 1x")
elif not _sar_control.exists():
    fail("StashAmmoRadar.cs is missing")
else:
    _sar_control_text = _sar_control.read_text(encoding="utf-8")
    if "Three-axis editor" not in _sar_control_text:
        fail("StashAmmoRadar implementation no longer documents its three-axis contract")
    elif 'new("loot.cheapStashAmmoQuantity", "Basic", -90)' not in _sar_control_text:
        fail("Basic stash ammo radar axis missing")
    elif 'new("loot.mediumStashAmmoQuantity", "Standard", 30)' not in _sar_control_text:
        fail("Standard stash ammo radar axis missing")
    elif 'new("loot.expensiveStashAmmoQuantity", "Rare", 150)' not in _sar_control_text:
        fail("Rare stash ammo radar axis missing")
    elif "1x = VANILLA" not in _sar_control_text:
        fail("StashAmmoRadar lost its vanilla reference ring")
    elif "DrawAmmoPreviewColumn" not in _sar_control_text or '"BASIC"' not in _sar_control_text or '"STANDARD"' not in _sar_control_text or '"RARE"' not in _sar_control_text:
        fail("StashAmmoRadar lost its three-column live ammo preview")
    elif "entry.Label" not in _sar_control_text or "ScaleCount" not in _sar_control_text:
        fail("StashAmmoRadar live preview no longer derives concrete ammo counts from LootHint entries")
    elif 'rounds";' not in _sar_control_text:
        fail("StashAmmoRadar live preview lost the player-facing rounds unit")
    elif 'new Rect(bounds.Left, bounds.Top - 6, bounds.Width, 14)' not in _sar_control_text:
        fail("StashAmmoRadar preview headings are no longer aligned with their definition columns")
    elif '<controls:StashAmmoRadar x:Name="StashAmmoRadarControl"' not in _sar_xaml:
        fail("MainWindow does not render the Stash Ammo Amounts radar")
    elif "stashAmmoParameterIds" not in _sar_main_vm or "loot.mediumStashAmmoQuantity" not in _sar_main_vm:
        fail("hidden stash-ammo backing parameters are not loaded into the live VM graph")
    elif "IsStashAmmoAnchor" not in _sar_param_vm or "IsStashAmmoMember" not in _sar_param_vm:
        fail("stash-ammo aggregate/backing visibility contract is missing")
    else:
        note("Stash Ammo Amounts locked: 3 existing quantity controls -> 1 visible radar; Basic/Standard/Rare writer semantics unchanged")

# Ammo Box Distribution radar production gate (v0.9.26.44).
_abd_ids = [
    "loot.ammoBoxWeight918",
    "loot.ammoBoxWeight545",
    "loot.ammoBoxWeight045",
    "loot.ammoBoxWeight012",
]
_abd_rows_by_id = {row.get("id"): row for row in parameters}
_abd_rows = [_abd_rows_by_id.get(pid) for pid in _abd_ids]
_abd_anchor = _abd_rows_by_id.get("loot.ammoBoxWeight918")
_abd_baseline_map = (baseline.get("special", {}).get("destructibleAmmoProduction", {}) or {}).get("weightParameterIds", {})
_abd_expected_map = {
    "A918D": "loot.ammoBoxWeight918",
    "A919D": "loot.ammoBoxWeight918",
    "A045D": "loot.ammoBoxWeight045",
    "A545D": "loot.ammoBoxWeight545",
    "A556D": "loot.ammoBoxWeight545",
    "A012D": "loot.ammoBoxWeight012",
}
_abd_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "AmmoDistributionRadar.cs"
_abd_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_abd_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_abd_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
_abd_writer = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")

if any(row is None for row in _abd_rows):
    fail("Ammo Box Distribution backing family parameter set is incomplete")
elif any(pid in _abd_rows_by_id for pid in {"loot.ammoBoxWeight919", "loot.ammoBoxWeight556"}):
    fail("obsolete per-caliber Ammo Box Distribution backing parameters still exist")
elif sum(1 for row in _abd_rows if row.get("exposeInUi", True)) != 1 or not _abd_anchor.get("exposeInUi", True):
    fail("Breakable Box Ammo Distribution must expose exactly one aggregate radar card")
elif _abd_anchor.get("label") != "Breakable Box Ammo Distribution":
    fail(f"Breakable Box Ammo Distribution label drifted: {_abd_anchor.get('label')}")
elif any(
    row.get("control") != "slider"
    or (row.get("vanilla") or {}).get("All") != 1.0
    or (row.get("slider") or {}).get("min") != 0.0
    or (row.get("slider") or {}).get("max") != 3.0
    or (row.get("slider") or {}).get("step") != 0.25
    for row in _abd_rows
):
    fail("Ammo Box Distribution family slider contract drifted from 0x-3x / 0.25x / vanilla 1x")
elif _abd_baseline_map != _abd_expected_map:
    fail(f"Ammo Box Distribution grouped SID mapping drifted: {_abd_baseline_map}")
elif not _abd_control.exists():
    fail("AmmoDistributionRadar.cs is missing")
else:
    _abd_control_text = _abd_control.read_text(encoding="utf-8")
    if "Four-axis editor" not in _abd_control_text or "CalculateVariantDetails" not in _abd_control_text:
        fail("four-axis AmmoDistributionRadar implementation is incomplete")
    elif 'new("loot.ammoBoxWeight918", "9×18 / 9×19 mm", -90)' not in _abd_control_text:
        fail("9 mm grouped radar axis missing")
    elif 'new("loot.ammoBoxWeight545", "5.45 / 5.56 mm", 0)' not in _abd_control_text:
        fail("5.45 / 5.56 mm grouped radar axis missing")
    elif "E / W" in _abd_control_text:
        fail("Ammo Box Distribution leaked internal E/W pool abbreviations into player-facing UI")
    elif '9×18 · Spawn {P(nine18)}' not in _abd_control_text or '9×19 · Spawn {Range(nine19A, nine19B)}' not in _abd_control_text:
        fail("Breakable Box Ammo Distribution no longer shows consistent per-ammo spawn metrics")
    elif "At least one vanilla ammo family in each box pool must remain above 0x." not in _abd_control_text:
        fail("AmmoDistributionRadar lost its all-zero pool guard")
    elif '<controls:AmmoDistributionRadar x:Name="AmmoDistributionRadarControl"' not in _abd_xaml:
        fail("MainWindow does not render the Ammo Box Distribution radar")
    elif "ammoDistributionParameterIds.Contains(p.Id)" not in _abd_main_vm:
        fail("hidden radar backing family parameters are not loaded into the live VM graph")
    elif "IsAmmoDistributionAnchor" not in _abd_param_vm or "IsAmmoDistributionMember" not in _abd_param_vm:
        fail("radar aggregate/backing visibility contract is missing")
    elif "WeightParameterIds" not in _abd_writer or "effectiveWeight" not in _abd_writer:
        fail("destructible ammo writer no longer consumes grouped radar Weight mappings")
    elif "GetOrAddChild(itemDefinition.Node" not in _abd_writer:
        fail("destructible ammo writer structure changed unexpectedly")
    else:
        note("Ammo Box Distribution locked: 4 family axes / 6 existing vanilla ammo entries; PossibleItems membership immutable; shared writer composes Weight with Amount")

# Destructible ammo weight audit gate (v0.9.26.44).
_aw_doc = DOCS / "generators" / "DESTRUCTIBLE-AMMO-WEIGHT-AUDIT.md"
_aw_rows_path = DOCS / "generators" / "destructible-ammo-weight-audit.csv"
_aw_pool_path = DOCS / "generators" / "destructible-ammo-pool-usage-audit.csv"
if not (_aw_doc.exists() and _aw_rows_path.exists() and _aw_pool_path.exists()):
    fail("destructible ammo weight audit evidence missing")
else:
    import csv as _aw_csv
    with _aw_rows_path.open(encoding="utf-8", newline="") as _f:
        _aw_rows = list(_aw_csv.DictReader(_f))
    with _aw_pool_path.open(encoding="utf-8", newline="") as _f:
        _aw_pools = list(_aw_csv.DictReader(_f))

    _aw_sids = {r["item_sid"] for r in _aw_rows}
    _aw_expected_sids = {"A918D","A919D","A545D","A012D","A045D","A556D"}
    _aw_weights = {r["vanilla_generator_weight"] for r in _aw_rows}
    _aw_pool_counts = {r["pool_sid"]:(int(r["base_boxes"]),int(r["dlc_boxes"]),int(r["total_with_dlc"])) for r in _aw_pools}

    if len(_aw_rows) != 8 or _aw_sids != _aw_expected_sids:
        fail(f"destructible ammo weight audit drifted: rows={len(_aw_rows)} sids={sorted(_aw_sids)}")
    elif _aw_weights != {"2"}:
        fail(f"destructible ammo vanilla weights drifted: {_aw_weights}; expected all 2")
    elif any(r["normalized_weight_share"] != "0.25" for r in _aw_rows):
        fail("destructible ammo normalized vanilla weight share drifted from 25% per pool candidate")
    elif _aw_pool_counts != {
        "DestructibleStash_AmmoSNG": (832,151,983),
        "DestructibleStash_AmmoNATO": (102,150,252),
    }:
        fail(f"destructible ammo physical pool usage drifted: {_aw_pool_counts}")
    else:
        note("destructible ammo weight audit locked: 6 calibers / 8 pool entries, vanilla weights 2/2/2/2, physical surface 934 base + 301 DLC = 1235 routes")

# World Loot Removal radar production gate (v0.9.26.44).
_wlr_ids = [
    "loot.worldAmmoCratesRemoved",
    "loot.worldF1GrenadeBoxes",
    "loot.worldFoodDrinkCratesRemoved",
    "loot.worldMedicalCratesRemoved",
    "loot.looseWorldLootRemoved",
]
_wlr_rows_by_id = {row.get("id"): row for row in parameters}
_wlr_rows = [_wlr_rows_by_id.get(pid) for pid in _wlr_ids]
_wlr_anchor = _wlr_rows_by_id.get("loot.worldAmmoCratesRemoved")
_wlr_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "WorldLootRemovalRadar.cs"
_wlr_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_wlr_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_wlr_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _wlr_rows):
    fail("World Loot Removal backing parameter set is incomplete")
elif sum(1 for row in _wlr_rows if row.get("exposeInUi", True)) != 1 or not _wlr_anchor.get("exposeInUi", True):
    fail("World Loot Removal must expose exactly one aggregate radar card for all five removal controls")
elif _wlr_anchor.get("label") != "World Loot Removal":
    fail(f"World Loot Removal aggregate label drifted: {_wlr_anchor.get('label')}")
elif any(
    row.get("control") != "slider"
    or (row.get("vanilla") or {}).get("All") != 0.0
    or (row.get("slider") or {}).get("min") != 0.0
    or (row.get("slider") or {}).get("max") != 1.0
    or (row.get("slider") or {}).get("step") != 0.01
    for row in _wlr_rows
):
    fail("World Loot Removal family slider contract drifted from 0%-100% / 1% step / vanilla 0%")
elif not _wlr_control.exists():
    fail("WorldLootRemovalRadar.cs is missing")
else:
    _wlr_control_text = _wlr_control.read_text(encoding="utf-8")
    if "Five-axis editor" not in _wlr_control_text:
        fail("WorldLootRemovalRadar implementation lost its five-axis contract")
    elif 'new("loot.worldAmmoCratesRemoved", "Ammo Boxes", -90, 981, 1315)' not in _wlr_control_text:
        fail("WorldLootRemovalRadar ordinary ammo/RGD population contract drifted")
    elif 'new("loot.worldF1GrenadeBoxes", "F1 Grenade Boxes", -18, 61, 101)' not in _wlr_control_text:
        fail("WorldLootRemovalRadar dedicated F1 population contract drifted")
    elif 'new("loot.worldFoodDrinkCratesRemoved", "Food & Drink", 54, 1100, 1208)' not in _wlr_control_text:
        fail("WorldLootRemovalRadar food/drink population contract drifted")
    elif 'new("loot.worldMedicalCratesRemoved", "Medical Boxes", 126, 709, 868)' not in _wlr_control_text:
        fail("WorldLootRemovalRadar medical population contract drifted")
    elif 'new("loot.looseWorldLootRemoved", "Loose Loot", 198, 5996, 7233)' not in _wlr_control_text:
        fail("WorldLootRemovalRadar loose-loot population contract drifted")
    elif "MidpointRounding.AwayFromZero" not in _wlr_control_text:
        fail("WorldLootRemovalRadar count preview no longer matches production removal rounding")
    elif "Base: removes" not in _wlr_control_text or "With DLC:" not in _wlr_control_text:
        fail("WorldLootRemovalRadar lost its emitted base/DLC removal-count information")
    elif "independent audited physical population" not in _wlr_control_text:
        fail("WorldLootRemovalRadar lost its independent-population semantics")
    elif '<controls:WorldLootRemovalRadar x:Name="WorldLootRemovalRadarControl"' not in _wlr_xaml:
        fail("MainWindow does not render the World Loot Removal radar")
    elif "worldLootRemovalParameterIds.Contains(p.Id)" not in _wlr_main_vm:
        fail("hidden World Loot Removal backing parameters are not loaded into the live VM graph")
    elif '"loot.worldF1GrenadeBoxes"' not in _wlr_main_vm:
        fail("F1 Grenade Boxes is no longer part of the World Loot Removal aggregate VM family")
    elif "IsWorldLootRemovalAnchor" not in _wlr_param_vm or "IsWorldLootRemovalMember" not in _wlr_param_vm:
        fail("World Loot Removal aggregate/backing visibility contract is missing")
    else:
        note("World Loot Removal locked: 5 independent removal controls -> 1 radar; ordinary ammo/RGD excludes dedicated F1 boxes; live base/DLC counts preserved")

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



# Final generator decision matrix release gate (v0.2.40).
_matrix_csv = ROOT / "docs" / "generators" / "generator-decision-matrix.csv"
_matrix_summary = ROOT / "docs" / "generators" / "generator-decision-matrix-summary.json"
_matrix_doc = DOCS / "GENERATOR-DECISION-MATRIX.md"
if not _matrix_csv.exists() or not _matrix_summary.exists():
    fail("final generator decision matrix artifacts missing")
else:
    with _matrix_csv.open(encoding="utf-8-sig", newline="") as fh:
        _matrix_rows = list(_csv.DictReader(fh))
    _matrix_ids = [row.get("control_id") for row in _matrix_rows]
    _triage_ids = [row.get("control_id") for row in _triage_rows]
    _matrix_mirrors = [ROOT / "docs" / "generators" / "generator-decision-matrix.csv"]
    _matrix_summary_mirrors = [ROOT / "docs" / "generators" / "generator-decision-matrix-summary.json"]
    _ms = _json.loads(_matrix_summary.read_text(encoding="utf-8"))
    if len(_matrix_rows) != 29 or len(set(_matrix_ids)) != 29 or set(_matrix_ids) != set(_triage_ids):
        fail(f"final generator decision matrix coverage drift: rows={len(_matrix_rows)} unique={len(set(_matrix_ids))} triage_match={set(_matrix_ids)==set(_triage_ids)}; expected 29/29/true")
    elif not all(x.exists() for x in _matrix_mirrors + _matrix_summary_mirrors):
        fail("final generator decision matrix canonical evidence missing")
    elif _ms.get("decision_concepts") != 29 or _ms.get("total_generator_backed_ui_controls") != 29:
        fail("final generator decision matrix summary contract drifted")
    elif not _matrix_doc.exists():
        fail("final generator decision matrix document missing from docs/")
    else:
        note("final generator decision matrix locked: 29/29 concepts; lifecycle decisions canonical; verified UI projection is 29 controls")

# Generator artifact layout cleanup gate (v0.2.41).
_layout_doc = ROOT / "docs" / "generators" / "ARTIFACT-LAYOUT.md"
_legacy_generators = ROOT / "generators"
if not _layout_doc.exists():
    fail("generator artifact layout policy missing")
elif _legacy_generators.exists():
    fail("legacy generators/ mirror exists; docs/generators/ must remain canonical")
else:
    note("generator artifact cleanup locked: docs/generators canonical; legacy generators mirror absent")

# Medical Healing radar production gate (v0.9.26.44).
_mhr_ids = [
    "health.bandageHealing",
    "health.medkitHealing",
    "health.armyMedkitHealing",
    "health.ecoMedkitHealing",
]
_mhr_rows_by_id = {row.get("id"): row for row in parameters}
_mhr_rows = [_mhr_rows_by_id.get(pid) for pid in _mhr_ids]
_mhr_anchor = _mhr_rows_by_id.get("health.bandageHealing")
_mhr_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "MedicalHealingRadar.cs"
_mhr_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_mhr_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_mhr_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _mhr_rows):
    fail("Medical Healing backing parameter set is incomplete")
elif sum(1 for row in _mhr_rows if row.get("exposeInUi", True)) != 1 or not _mhr_anchor.get("exposeInUi", True):
    fail("Medical Healing must expose exactly one aggregate radar card")
elif _mhr_anchor.get("label") != "Healing Amount":
    fail(f"Medical Healing aggregate label drifted: {_mhr_anchor.get('label')}")
elif any(
    row.get("control") != "slider"
    or (row.get("slider") or {}).get("min") != 0.0
    or (row.get("display") or {}).get("primary") != "percentOfVanilla"
    for row in _mhr_rows
):
    fail("Medical Healing backing controls drifted from direct vanilla-relative healing sliders")
elif not _mhr_control.exists():
    fail("MedicalHealingRadar.cs is missing")
else:
    _mhr_control_text = _mhr_control.read_text(encoding="utf-8")
    if "Four-axis editor" not in _mhr_control_text:
        fail("MedicalHealingRadar implementation lost its four-axis contract")
    elif 'new("health.bandageHealing", "Bandage", -90)' not in _mhr_control_text:
        fail("MedicalHealingRadar Bandage axis missing")
    elif 'new("health.ecoMedkitHealing", "Scientific Medkit", 180)' not in _mhr_control_text:
        fail("MedicalHealingRadar Scientific Medkit axis missing")
    elif "1x = VANILLA" not in _mhr_control_text or "MaximumMultiplier = 2.0" not in _mhr_control_text:
        fail("MedicalHealingRadar lost its normalized vanilla-relative scale")
    elif "HP ·" not in _mhr_control_text or "Vanilla" not in _mhr_control_text or "Health restored" not in _mhr_control_text:
        fail("MedicalHealingRadar lost its existing healing information in the live legend")
    elif "parameter.CurrentNumber = vanilla * multiplier" not in _mhr_control_text:
        fail("MedicalHealingRadar no longer maps normalized axes back to existing raw HP values")
    elif '<controls:MedicalHealingRadar x:Name="MedicalHealingRadarControl"' not in _mhr_xaml:
        fail("MainWindow does not render the Medical Healing radar")
    elif "medicalHealingParameterIds.Contains(p.Id)" not in _mhr_main_vm:
        fail("hidden Medical Healing backing parameters are not loaded into the live VM graph")
    elif "IsMedicalHealingAnchor" not in _mhr_param_vm or "IsMedicalHealingMember" not in _mhr_param_vm:
        fail("Medical Healing aggregate/backing visibility contract is missing")
    else:
        note("Healing Amount locked: 4 existing healing sliders -> 1 visible radar; raw HP, multiplier and vanilla information preserved")

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


# Action Costs radar production gate (v0.9.26.44).
_acr_ids = [
    "stamina.sprintCost",
    "stamina.jumpCost",
    "stamina.vaultCost",
    "stamina.meleeNormalCost",
    "stamina.meleeStrongCost",
    "stamina.butstockCost",
    "stamina.aimRegenCoef",
]
_acr_rows_by_id = {row.get("id"): row for row in parameters}
_acr_rows = [_acr_rows_by_id.get(pid) for pid in _acr_ids]
_acr_anchor = _acr_rows_by_id.get("stamina.sprintCost")
_acr_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "ActionCostsRadar.cs"
_acr_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_acr_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_acr_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _acr_rows):
    fail("Action Costs backing parameter set is incomplete")
elif sum(1 for row in _acr_rows if row.get("exposeInUi", True)) != 1 or not _acr_anchor.get("exposeInUi", True):
    fail("Action Costs must expose exactly one aggregate radar card")
elif _acr_anchor.get("label") != "Action Costs":
    fail(f"Action Costs aggregate label drifted: {_acr_anchor.get('label')}")
elif any(
    row.get("control") != "slider"
    or (row.get("vanilla") or {}).get("All") is None
    or (row.get("slider") or {}).get("min") != 0.0
    for row in _acr_rows
):
    fail("Action Costs backing controls drifted from positive direct stamina/penalty sliders")
elif not _acr_control.exists():
    fail("ActionCostsRadar.cs is missing")
else:
    _acr_control_text = _acr_control.read_text(encoding="utf-8")
    if "Seven-axis editor" not in _acr_control_text:
        fail("ActionCostsRadar implementation lost its seven-axis contract")
    elif 'new("stamina.sprintCost", "Sprint", -90' not in _acr_control_text:
        fail("ActionCostsRadar Sprint axis missing")
    elif 'new("stamina.aimRegenCoef", "Aiming", 218.571, true, "% penalty"' not in _acr_control_text:
        fail("ActionCostsRadar Aiming axis or percentage semantics missing")
    elif "1x = VANILLA" not in _acr_control_text or "MaximumMultiplier = 2.0" not in _acr_control_text:
        fail("ActionCostsRadar lost its normalized vanilla-relative scale")
    elif "FormatRawValue" not in _acr_control_text or "ValueMeaning" not in _acr_control_text or "Vanilla" not in _acr_control_text:
        fail("ActionCostsRadar lost current value / unit / vanilla / meaning information")
    elif "parameter.TickFrequency" not in _acr_control_text or "MidpointRounding.AwayFromZero" not in _acr_control_text:
        fail("ActionCostsRadar drag path no longer preserves backing slider step semantics")
    elif '<controls:ActionCostsRadar x:Name="ActionCostsRadarControl"' not in _acr_xaml:
        fail("MainWindow does not render the Action Costs radar")
    elif "actionCostParameterIds.Contains(p.Id)" not in _acr_main_vm:
        fail("hidden Action Costs backing parameters are not loaded into the live VM graph")
    elif "IsActionCostAnchor" not in _acr_param_vm or "IsActionCostMember" not in _acr_param_vm:
        fail("Action Costs aggregate/backing visibility contract is missing")
    else:
        note("Action Costs locked: 7 existing sliders -> 1 visible radar; raw values, units, vanilla references, meanings and original steps preserved")

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

if len(_survival_ui) != 23 or len(_survival_confirmed) != 23 or len(_survival_runtime) != 0:
    fail(f"survival deep audit status split drifted: total={len(_survival_ui)} confirmed={len(_survival_confirmed)} runtime={len(_survival_runtime)}; expected 23/23/0")
elif len(_survival_points) != 70:
    fail(f"survival writer surface drifted: {len(_survival_points)} edit points; expected 70")
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
    note("survival deep audit locked: 23/23 confirmed; 70 edit points; hunger penalties use positive magnitude -> negateSet; Low Bleeding runtime value 1.0")

# Item Price Scalers radar production gate (v0.9.26.44).
_ips_ids = [
    "economy.consumableCost",
    "economy.ammoCost",
    "economy.armorCost",
    "economy.weaponCost",
    "economy.artifactCost",
    "economy.mutantPartValue",
]
_ips_rows_by_id = {row.get("id"): row for row in parameters}
_ips_rows = [_ips_rows_by_id.get(pid) for pid in _ips_ids]
_ips_anchor = _ips_rows_by_id.get("economy.consumableCost")
_ips_control = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "ItemPriceScalersRadar.cs"
_ips_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_ips_main_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_ips_param_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")

if any(row is None for row in _ips_rows):
    fail("Item Price Scalers backing parameter set is incomplete")
elif sum(1 for row in _ips_rows if row.get("exposeInUi", True)) != 1 or not _ips_anchor.get("exposeInUi", True):
    fail("Item Price Scalers must expose exactly one aggregate radar card")
elif _ips_anchor.get("label") != "Item Price Scalers":
    fail(f"Item Price Scalers aggregate label drifted: {_ips_anchor.get('label')}")
elif any(
    row.get("control") != "slider"
    or (row.get("slider") or {}).get("min") != 0.0
    or (row.get("slider") or {}).get("max") != 2.0
    or (row.get("slider") or {}).get("step") != 0.01
    or (row.get("display") or {}).get("primary") != "multiplier"
    for row in _ips_rows
):
    fail("Item Price Scalers backing controls drifted from 0x-2x / 0.01 direct multiplier semantics")
elif any(any(float(v) != 1.0 for v in (row.get("vanilla") or {}).values()) for row in _ips_rows):
    fail("Item Price Scalers vanilla contract drifted from 1x")
elif not _ips_control.exists():
    fail("ItemPriceScalersRadar.cs is missing")
else:
    _ips_control_text = _ips_control.read_text(encoding="utf-8")
    if "Six-axis editor" not in _ips_control_text:
        fail("ItemPriceScalersRadar implementation lost its six-axis contract")
    elif 'new("economy.consumableCost", "Consumables", -90' not in _ips_control_text:
        fail("ItemPriceScalersRadar Consumables axis missing")
    elif 'new("economy.mutantPartValue", "Mutant Parts", 210' not in _ips_control_text:
        fail("ItemPriceScalersRadar Mutant Parts axis missing")
    elif "1x = VANILLA" not in _ips_control_text or "MaximumMultiplier = 2.0" not in _ips_control_text:
        fail("ItemPriceScalersRadar lost its 0x-2x / 1x vanilla scale")
    elif "Vanilla 1x" not in _ips_control_text or "ValueMeaning" not in _ips_control_text:
        fail("ItemPriceScalersRadar lost current multiplier / vanilla / value-meaning information")
    elif "RadarStep = 0.25" not in _ips_control_text or "Math.Round(value / RadarStep, MidpointRounding.AwayFromZero)" not in _ips_control_text:
        fail("ItemPriceScalersRadar lost its 0.25x radar-step contract")
    elif "parameter.CurrentNumber + (direction * RadarStep)" not in _ips_control_text:
        fail("ItemPriceScalersRadar keyboard stepping drifted from 0.25x")
    elif '<controls:ItemPriceScalersRadar x:Name="ItemPriceScalersRadarControl"' not in _ips_xaml:
        fail("MainWindow does not render the Item Price Scalers radar")
    elif "itemPriceScalerParameterIds.Contains(p.Id)" not in _ips_main_vm:
        fail("hidden Item Price Scalers backing parameters are not loaded into the live VM graph")
    elif "IsItemPriceScalerAnchor" not in _ips_param_vm or "IsItemPriceScalerMember" not in _ips_param_vm:
        fail("Item Price Scalers aggregate/backing visibility contract is missing")
    else:
        note("Item Price Scalers locked: 6 direct price multipliers -> 1 radar; 0.25x radar step; current x, vanilla, meanings and writer routes preserved")

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
if not _crate_manifest_src.is_file() or not _crate_manifest_root.is_file():
    fail("crate-reduction production manifest missing from canonical Data mirrors")
elif _crate_manifest_src.read_bytes() != _crate_manifest_root.read_bytes():
    fail("crate-reduction production manifest Data mirrors drifted")
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

# Seven custom factory presets release gate (v0.9.26.51).
_factory_preset_names = [
    "Immersive",
    "Tactical",
    "Scavenger",
    "Mutant Hunter",
    "Anomaly Runner",
    "Hardcore",
    "Hardcore+",
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
    if len(_values) != 188:
        _factory_errors.append(f"{_name}: {len(_values)} values, expected 188")
    if _preset.get("referenceDifficulty") not in {"Novice", "Stalker", "Veteran", "Master"}:
        _factory_errors.append(f"{_name}: invalid referenceDifficulty={_preset.get('referenceDifficulty')}")
    if f'Presets\\{_name}.json' not in _factory_csproj_text:
        _factory_errors.append(f"{_name}: not embedded in csproj")
    if f'"{_name}"' not in _factory_service_text:
        _factory_errors.append(f"{_name}: not registered in FactoryPresetNames")

_factory_scarcity_expectations = {
    "Hardcore": {
        "loot.looseWorldLootRemoved": 0.80,
        "loot.worldAmmoCratesRemoved": 0.75,
        "loot.worldFoodDrinkCratesRemoved": 0.70,
        "loot.worldMedicalCratesRemoved": 0.80,
        "loot.worldWeaponDensity": 0.40,
        "loot.npcMedicalChance": 0.25,
        "loot.npcWeaponAmmoSupply": 0.25,
        "economy.traderAmmoStock": 0.50,
        "economy.medicStock": 0.50,
    },
    "Hardcore+": {
        "loot.looseWorldLootRemoved": 0.90,
        "loot.worldAmmoCratesRemoved": 0.90,
        "loot.worldFoodDrinkCratesRemoved": 0.85,
        "loot.worldMedicalCratesRemoved": 0.90,
        "loot.worldWeaponDensity": 0.20,
        "loot.npcMedicalChance": 0.10,
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
    fail("seven custom factory presets drifted: " + "; ".join(_factory_errors))
else:
    note("factory presets locked: 7 custom profiles / 188 values each; retired 4 files removed; hard-profile medical rules preserved")

# Rationale control-help gate (v0.9.26.51).
_parameter_vm_text = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
_control_help_errors = []
for _required in [
    "public string ControlHelp",
    "public bool IsAggregateRadarAnchor",
    'return $"{body}{Environment.NewLine}{Environment.NewLine}{ControlHelp}";',
    "Mouse: drag a radar point;",
    "mouse wheel over legend text adjusts that branch",
    "right-click a point or its legend text resets only that branch",
    "Arrow keys or +/- adjust it; R resets every branch in this radar.",
    "mouse wheel over the current-value box adjusts one step",
    "click a vanilla marker to snap",
    "right-click the slider or current-value box resets",
    "focus the checkbox and press Space to toggle it",
    "Home/End jump to minimum/maximum",
]:
    if _required not in _parameter_vm_text:
        _control_help_errors.append(f"missing {_required!r}")

if _parameter_vm_text.count("IsAggregateRadarAnchor =>") != 1:
    _control_help_errors.append("aggregate radar control-help classifier missing or duplicated")

if _control_help_errors:
    fail("rationale control-help drifted: " + "; ".join(_control_help_errors))
else:
    note("rationale controls locked: every rationale ends with type-aware mouse + keyboard help; radar/slider/range/choice/toggle covered")

# Radar legend text hitbox + right-click reset gate (v0.9.26.51).
_radar_legend_reset_files = [
    "AmmoDistributionRadar.cs",
    "StashAmmoRadar.cs",
    "MutantDropRadar.cs",
    "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs",
    "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs",
    "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs",
    "SpecialAbilityCooldownRadar.cs",
]
_radar_legend_reset_errors = []
for _radar_name in _radar_legend_reset_files:
    _radar_path = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _radar_name
    _radar_text = _radar_path.read_text(encoding="utf-8")
    for _required in [
        "private readonly Dictionary<int, Rect> _legendHitRects",
        "BeginLegendHitArea(",
        "EndLegendHitArea(",
        "IncludeLegendTextHit(bounds, formatted);",
        "formatted.WidthIncludingTrailingWhitespace",
        "var textLeft = bounds.Left + Math.Max(0.0, bounds.Width - textWidth);",
        "var axis = FindLegendAxis(point);",
        "if (axis < 0)\n            axis = FindHandleAxis(point,",
    ]:
        if _required not in _radar_text:
            _radar_legend_reset_errors.append(f"{_radar_name}: missing {_required!r}")

    _find_start = _radar_text.find("private int FindLegendAxis(Point point)")
    _find_end = _radar_text.find("protected override void OnKeyDown", _find_start)
    _find_body = _radar_text[_find_start:_find_end]
    if "_legendHitRects" not in _find_body or ".Contains(point)" not in _find_body:
        _radar_legend_reset_errors.append(f"{_radar_name}: FindLegendAxis is not text-extent based")
    if "GetInfoRect()" in _find_body or "rowHeight" in _find_body or "sectionHeight" in _find_body:
        _radar_legend_reset_errors.append(f"{_radar_name}: legacy full-row legend hitbox still active")

if _radar_legend_reset_errors:
    fail("radar legend reset/hitbox drifted: " + "; ".join(_radar_legend_reset_errors))
else:
    note("radar legend reset locked: 10/10 text-only hitboxes support hover + wheel + right-click branch reset")

# Radar legend hover + wheel interaction gate (v0.9.26.44).
_radar_legend_interaction_controls = [
    "AmmoDistributionRadar.cs",
    "MutantDropRadar.cs",
    "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs",
    "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs",
    "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs",
    "SpecialAbilityCooldownRadar.cs",
    "StashAmmoRadar.cs",
]
_radar_legend_interaction_errors = []
for _radar_name in _radar_legend_interaction_controls:
    _radar_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _radar_name).read_text(encoding="utf-8")
    if "private int _hoverAxis = -1;" not in _radar_text:
        _radar_legend_interaction_errors.append(f"{_radar_name}: hover branch state missing")
    if "private int FindLegendAxis(Point point)" not in _radar_text:
        _radar_legend_interaction_errors.append(f"{_radar_name}: legend hit-test missing")
    if "protected override void OnMouseWheel(MouseWheelEventArgs e)" not in _radar_text:
        _radar_legend_interaction_errors.append(f"{_radar_name}: legend wheel interaction missing")
    if "protected override void OnMouseLeave(MouseEventArgs e)" not in _radar_text:
        _radar_legend_interaction_errors.append(f"{_radar_name}: hover cleanup missing")
    if "hovered ? textBrush : mutedBrush" not in _radar_text:
        _radar_legend_interaction_errors.append(f"{_radar_name}: muted legend text does not brighten on hover")
if _radar_legend_interaction_errors:
    fail("radar legend hover/wheel interaction drifted: " + "; ".join(_radar_legend_interaction_errors))
else:
    note("radar legend interaction locked: 10/10 radars brighten muted branch text on hover and wheel-adjust that branch")

# Radar legend background cleanup gate (v0.9.26.44).
_radar_no_legend_background_controls = [
    "AmmoDistributionRadar.cs",
    "MutantDropRadar.cs",
    "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs",
    "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs",
    "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs",
    "SpecialAbilityCooldownRadar.cs",
    "StashAmmoRadar.cs",
]
_radar_legend_background_errors = []
for _radar_name in _radar_no_legend_background_controls:
    _radar_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _radar_name).read_text(encoding="utf-8")
    if "DrawRoundedRectangle(highlight" in _radar_text:
        _radar_legend_background_errors.append(f"{_radar_name}: legend selection/modification background returned")
if _radar_legend_background_errors:
    fail("radar legend background cleanup drifted: " + "; ".join(_radar_legend_background_errors))
else:
    note("radar legend backgrounds locked: selected/modified text rows use font/color only, no gray/yellow fill")

# Aggregate radar duplicate-title cleanup gate (v0.9.26.44).
_radar_title_cleanup_triggers = [
    "IsMutantHealthAnchor",
    "IsSpecialAbilityCooldownAnchor",
    "IsMedicalHealingAnchor",
    "IsActionCostAnchor",
    "IsMutantDropAnchor",
    "IsItemPriceScalerAnchor",
]
_radar_title_cleanup_xaml = (ROOT / "src" / "TrueCustomDifficulty" / "MainWindow.xaml").read_text(encoding="utf-8")
_radar_title_cleanup_errors = []
for _trigger in _radar_title_cleanup_triggers:
    _marker = f'<DataTrigger Binding="{{Binding {_trigger}}}" Value="True">'
    _start = _radar_title_cleanup_xaml.find(_marker)
    _end = _radar_title_cleanup_xaml.find("</DataTrigger>", _start)
    _block = _radar_title_cleanup_xaml[_start:_end]
    if '<Setter TargetName="ParameterLabel" Property="Visibility" Value="Collapsed" />' not in _block:
        _radar_title_cleanup_errors.append(f"{_trigger}: duplicate inner title visible")
if _radar_title_cleanup_errors:
    fail("aggregate radar duplicate-title cleanup drifted: " + "; ".join(_radar_title_cleanup_errors))
else:
    note("aggregate radar title cleanup locked: requested 6 radar cards hide duplicate inner titles")

# Radar font readability gate (v0.9.26.44).
_radar_font_controls = [
    "AmmoDistributionRadar.cs",
    "MutantDropRadar.cs",
    "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs",
    "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs",
    "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs",
    "SpecialAbilityCooldownRadar.cs",
    "StashAmmoRadar.cs",
]
_radar_font_errors = []
for _radar_name in _radar_font_controls:
    _radar_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _radar_name).read_text(encoding="utf-8")
    if not any(_font_token in _radar_text for _font_token in [", 9.8,", ", 10.0,", ", 10.2,", ", 10.3,"]):
        _radar_font_errors.append(f"{_radar_name}: expected enlarged radar text sizes missing")
if _radar_font_errors:
    fail("radar font readability layout drifted: " + "; ".join(_radar_font_errors))
else:
    note("radar font readability locked: all 10 aggregate radars use enlarged Wine-friendly text")

# Right-aligned radar information text gate (v0.9.26.44).
_radar_right_align_controls = [
    "AmmoDistributionRadar.cs",
    "MutantDropRadar.cs",
    "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs",
    "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs",
    "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs",
    "SpecialAbilityCooldownRadar.cs",
    "StashAmmoRadar.cs",
]
_radar_right_align_errors = []
for _radar_name in _radar_right_align_controls:
    _radar_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _radar_name).read_text(encoding="utf-8")
    if "formatted.TextAlignment = TextAlignment.Right;" not in _radar_text:
        _radar_right_align_errors.append(f"{_radar_name}: clipped info text is not right-aligned")
if _radar_right_align_errors:
    fail("right-aligned radar information layout drifted: " + "; ".join(_radar_right_align_errors))
else:
    note("radar info text alignment locked: 10/10 right-side information panes use clean right alignment")

# Compact single-column radar information layout gate (v0.9.26.44).
_radar_single_column_controls = [
    "AmmoDistributionRadar.cs",
    "MutantDropRadar.cs",
    "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs",
    "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs",
    "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs",
    "SpecialAbilityCooldownRadar.cs",
    "StashAmmoRadar.cs",
]
_radar_single_column_errors = []
for _radar_name in _radar_single_column_controls:
    _radar_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _radar_name).read_text(encoding="utf-8")
    if "GetInfoRect()" not in _radar_text:
        _radar_single_column_errors.append(f"{_radar_name}: no right-side info pane")
for _two_col_name in ["ActionCostsRadar.cs", "MutantHealthRadar.cs", "SpecialAbilityCooldownRadar.cs", "AnomalyDamageRadar.cs", "MutantDropRadar.cs"]:
    _two_col_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _two_col_name).read_text(encoding="utf-8")
    if "var right = new Rect(" in _two_col_text or "var rightRect = new Rect(" in _two_col_text:
        _radar_single_column_errors.append(f"{_two_col_name}: two-column legend layout returned")
_stash_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "StashAmmoRadar.cs").read_text(encoding="utf-8")
if "info.Top + sectionHeight" not in _stash_text or "info.Top + (sectionHeight * 2.0)" not in _stash_text:
    _radar_single_column_errors.append("StashAmmoRadar: Basic/Standard/Rare previews are not vertically stacked")
if _radar_single_column_errors:
    fail("compact single-column radar layout drifted: " + "; ".join(_radar_single_column_errors))
else:
    note("compact radar info layout locked: 10/10 radars use dense right-side text; long legends are single-column and Wine-safe")

# AmmoDistributionRadar helper compile gate (v0.9.26.44).
_ammo_radar_compile_text = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "AmmoDistributionRadar.cs").read_text(encoding="utf-8")
if "DrawClippedText(" in _ammo_radar_compile_text and "private void DrawClippedText(" not in _ammo_radar_compile_text:
    fail("AmmoDistributionRadar uses DrawClippedText without defining the helper")
else:
    note("AmmoDistributionRadar compile helper locked: DrawClippedText definition present")

# Horizontal radar layout gate (v0.9.26.44).
_radar_side_layout_controls = [
    "AmmoDistributionRadar.cs",
    "MutantDropRadar.cs",
    "AnomalyDamageRadar.cs",
    "WorldLootRemovalRadar.cs",
    "MedicalHealingRadar.cs",
    "ActionCostsRadar.cs",
    "ItemPriceScalersRadar.cs",
    "MutantHealthRadar.cs",
    "SpecialAbilityCooldownRadar.cs",
    "StashAmmoRadar.cs",
]
_radar_side_layout_errors = []
for _radar_name in _radar_side_layout_controls:
    _radar_path = ROOT / "src" / "TrueCustomDifficulty" / "Controls" / _radar_name
    if not _radar_path.exists():
        _radar_side_layout_errors.append(f"{_radar_name}: missing")
        continue
    _radar_text = _radar_path.read_text(encoding="utf-8")
    if "GetInfoRect()" not in _radar_text:
        _radar_side_layout_errors.append(f"{_radar_name}: right-side information pane missing")
    if "ActualWidth * 0.25" not in _radar_text:
        _radar_side_layout_errors.append(f"{_radar_name}: radar is no longer anchored to the left pane")
    if "new Point(ActualWidth / 2.0" in _radar_text and "return (new Point(ActualWidth / 2.0" in _radar_text:
        _radar_side_layout_errors.append(f"{_radar_name}: radar geometry drifted back to centered layout")
if _radar_side_layout_errors:
    fail("horizontal radar layout drifted: " + "; ".join(_radar_side_layout_errors))
else:
    note("horizontal radar layout locked: 10/10 aggregate radars use left visualization + dominant right information pane")

# UI -> real CFG deep-audit artifact gate (v0.9.26.44).
_ui_cfg_doc = DOCS / "UI-CFG-DEEP-AUDIT.md"
_ui_cfg_csv = DOCS / "ui-cfg-deep-audit.csv"
_ui_cfg_tool = ROOT / "tools" / "audit_ui_cfg_contract.py"
if not _ui_cfg_doc.exists() or not _ui_cfg_csv.exists() or not _ui_cfg_tool.exists():
    fail("UI -> real CFG deep-audit artifacts missing")
else:
    import csv as _ui_cfg_csv_mod
    with _ui_cfg_csv.open(newline="", encoding="utf-8-sig") as _f:
        _ui_cfg_rows = list(_ui_cfg_csv_mod.DictReader(_f))
    if len(_ui_cfg_rows) != 188:
        fail(f"UI -> real CFG deep-audit dimension count drifted: {len(_ui_cfg_rows)}; expected 188")
    elif any(r.get("result") != "PASS" for r in _ui_cfg_rows):
        fail("UI -> real CFG deep-audit CSV contains a non-PASS row")
    else:
        note("UI -> real CFG deep audit locked: 128 visible cards -> 188 editable dimensions; 188/188 writer-routed")

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
    "survival": 23,
    "economy": 17,
    "equipment": 4,
    "master": 20,
    "environment": 7,
    "loot": 18,
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

if len(parameters) != 199 or len(_global_visible) != 128 or len(_global_hidden) != 71:
    fail(f"global UI accounting drifted: catalog={len(parameters)} visible={len(_global_visible)} hidden={len(_global_hidden)}; expected 199/128/71")
elif _global_tab_counts != _global_expected_tabs:
    fail(f"global visible tab counts drifted: {_global_tab_counts}; expected {_global_expected_tabs}")
elif _global_confirmed != 98 or _global_runtime != 30:
    fail(f"global visible status split drifted: confirmed={_global_confirmed} runtime={_global_runtime}; expected 98/30")
elif _global_control_counts != {"slider": 114, "toggle": 12, "choiceSlider": 1, "range": 1}:
    fail(f"global visible control-type split drifted: {_global_control_counts}; expected 114 sliders / 12 toggles / 1 choiceSlider / 1 range")
elif _global_duplicate_labels:
    fail(f"duplicate player-facing labels detected: {_global_duplicate_labels}")
elif _global_out_of_range:
    fail(f"visible vanilla values escaped current slider ranges: {_global_out_of_range}")
elif len(_global_generator_visible) != 17 or len(_global_non_generator_visible) != 111 or _global_non_generator_confirmed != 95 or _global_non_generator_runtime != 16:
    fail(f"global generator/non-generator split drifted: generator={len(_global_generator_visible)} non_generator={len(_global_non_generator_visible)} non_gen_status={_global_non_generator_confirmed}/{_global_non_generator_runtime}; expected 17/111 and 95/16")
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
    note("global cross-domain audit locked: 199 catalog -> 128 visible + 71 hidden; 98 confirmed + 30 runtime; 0 writer collisions; all 11 direct anomaly-damage controls use x display and Effective Damage composition")

# Override verification harness release gate (v0.2.51).
_override_tool = ROOT / "tools" / "verify_cfg_overrides.py"
_override_doc = DOCS / "OVERRIDE-VERIFICATION-HARNESS.md"
_override_suite = ROOT / "docs" / "override-verification" / "v0.2.51" / "suite-summary.csv"
_override_expected = {
    ("loot.droppedWeaponCondition", "MIN"): ("0.0", 242, 242, 0),
    ("loot.droppedWeaponCondition", "VANILLA"): ("1.0", 242, 0, 242),
    ("loot.droppedWeaponCondition", "MAX"): ("2.0", 242, 242, 0),
    ("loot.npcWeaponAmmoSupply", "MIN"): ("0.0", 242, 161, 81),
    ("loot.npcWeaponAmmoSupply", "VANILLA"): ("1.0", 242, 0, 242),
    ("loot.npcWeaponAmmoSupply", "MAX"): ("3.0", 242, 161, 81),
    ("economy.medicStock", "MIN"): ("0.25", 96, 84, 12),
    ("economy.medicStock", "VANILLA"): ("1.0", 96, 0, 96),
    ("economy.medicStock", "MAX"): ("3.0", 96, 96, 0),
}
_override_rows = {}
if _override_suite.exists():
    import csv as _csv
    with _override_suite.open(newline="", encoding="utf-8-sig") as _f:
        for _r in _csv.DictReader(_f):
            _override_rows[(_r.get("parameterId"), _r.get("variant"))] = (
                _r.get("inputValue"),
                int(_r.get("baselineEditPoints", "-1")),
                int(_r.get("expectedChanged", "-1")),
                int(_r.get("expectedUnchanged", "-1")),
            )
_override_reference_patches = list((ROOT / "docs" / "override-verification" / "v0.2.51").glob("**/expected-patch/**/*_patch_TrueCustomDifficulty.cfg"))
if not _override_tool.exists():
    fail("override verification harness missing: tools/verify_cfg_overrides.py")
elif not _override_doc.exists():
    fail("override verification harness document missing from docs/")
elif _override_rows != _override_expected:
    fail(f"v0.2.51 override verification suite drifted: {_override_rows}; expected {_override_expected}")
elif len(_override_reference_patches) != 6:
    fail(f"v0.2.51 override reference patch count drifted: {len(_override_reference_patches)}; expected 6 non-vanilla cases")
elif (ROOT / "tools" / "runtime_tests" / "README.md").read_bytes() != (ROOT / "docs" / "runtime-tests" / "README.md").read_bytes():
    fail("runtime-test README mirror drifted between tools/ and docs/")
else:
    _override_tool_text = _override_tool.read_text(encoding="utf-8")
    _required_tokens = ["CONTRACT_PASS", "scaleClamp01", "scaleIntegerHalfUpPreserveZero", "def run_compare", "def run_suite"]
    _missing_tokens = [token for token in _required_tokens if token not in _override_tool_text]
    if _missing_tokens:
        fail(f"override verifier implementation contract drifted; missing tokens: {_missing_tokens}")
    else:
        note("override verification harness locked: 3 controls x MIN/VANILLA/MAX; 6 reference patches; exact compare mode present")

# Full runtime-pending override verification release gate (v0.2.52).
_override52_root = ROOT / "docs" / "override-verification" / "v0.2.52"
_override52_master = _override52_root / "master" / "suite-summary.csv"
_override52_veteran = _override52_root / "veteran-special-arrays" / "suite-summary.csv"
_override52_self = _override52_root / "self-compare-summary.json"
_override52_summary = _override52_root / "summary.json"
_override52_runtime_ids = set()  # historical v0.2.52 set is loaded from its saved suite below
_override52_master_rows = []
_override52_veteran_rows = []
if _override52_master.exists():
    with _override52_master.open(newline="", encoding="utf-8-sig") as _f:
        _override52_master_rows = list(_csv.DictReader(_f))
if _override52_veteran.exists():
    with _override52_veteran.open(newline="", encoding="utf-8-sig") as _f:
        _override52_veteran_rows = list(_csv.DictReader(_f))
_override52_master_ids = {r.get("parameterId") for r in _override52_master_rows}
_override52_runtime_ids = set(_override52_master_ids)
_override52_veteran_ids = {r.get("parameterId") for r in _override52_veteran_rows}
_override52_expected_veteran_ids = {
    pid for pid in _override52_runtime_ids
    if pid.startswith("combat.mutantCooldown.") or pid.startswith("combat.psy")
}
_override52_routes = {}
for _r in _override52_master_rows:
    _override52_routes.setdefault(_r.get("parameterId"), _r.get("route"))
_override52_route_counts = {}
for _route in _override52_routes.values():
    _override52_route_counts[_route] = _override52_route_counts.get(_route, 0) + 1
_override52_expected_route_counts = {
    "directEditPoints": 11,
    "mutantCooldownArray": 8,
    "psyArray": 4,
    "cheapStashCloneRedirect": 2,
    "npcMedicalCloneRedirect": 1,
    "mediumStashCloneRedirect": 1,
    "expensiveStashCloneRedirect": 1,
    "destructibleAmmoCloneRedirect": 1,
    "destructibleGrenadeCloneRedirect": 1,
}
_override52_self_doc = json.loads(_override52_self.read_text(encoding="utf-8")) if _override52_self.exists() else {}
_override52_summary_doc = json.loads(_override52_summary.read_text(encoding="utf-8")) if _override52_summary.exists() else {}
_override52_manifest = ROOT / "Data" / "npc-medical-production-redirect-manifest.csv"
_override52_manifest_rows = 0
if _override52_manifest.exists():
    with _override52_manifest.open(newline="", encoding="utf-8-sig") as _f:
        _override52_manifest_rows = sum(1 for _ in _csv.DictReader(_f))
_override52_writer = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
_override52_tool_text = _override_tool.read_text(encoding="utf-8") if _override_tool.exists() else ""

if len(_override52_runtime_ids) != 30:
    fail(f"historical v0.2.52 runtime-pending evidence drifted: {len(_override52_runtime_ids)}; expected 38")
elif len(_override52_master_rows) != 90 or _override52_master_ids != _override52_runtime_ids:
    fail(f"v0.2.52 Master override suite drifted: rows={len(_override52_master_rows)} ids={len(_override52_master_ids)}; expected 90 rows / exact 30 runtime IDs")
elif any(r.get("status") != "CONTRACT_PASS" for r in _override52_master_rows):
    fail("v0.2.52 Master override suite contains a non-CONTRACT_PASS case")
elif any(int(r.get("expectedAssignments", "-1")) != 0 or int(r.get("expectedStructures", "-1")) != 0 for r in _override52_master_rows if r.get("variant") == "VANILLA"):
    fail("v0.2.52 Master VANILLA contract must emit zero assignments and zero struct nodes")
elif _override52_route_counts != _override52_expected_route_counts:
    fail(f"v0.2.52 verifier route projection drifted: {_override52_route_counts}; expected {_override52_expected_route_counts}")
elif len(_override52_veteran_rows) != 36 or _override52_veteran_ids != _override52_expected_veteran_ids:
    fail(f"v0.2.52 Veteran special-array suite drifted: rows={len(_override52_veteran_rows)} ids={sorted(_override52_veteran_ids)}")
elif any(r.get("status") != "CONTRACT_PASS" for r in _override52_veteran_rows):
    fail("v0.2.52 Veteran special-array suite contains a non-CONTRACT_PASS case")
elif any(int(r.get("expectedAssignments", "-1")) != 0 or int(r.get("expectedStructures", "-1")) != 0 for r in _override52_veteran_rows if r.get("variant") == "VANILLA"):
    fail("v0.2.52 Veteran VANILLA contract must emit zero assignments and zero struct nodes")
elif any(int(r.get("expectedAssignments", "-1")) != 146 for r in _override52_veteran_rows if r.get("parameterId", "").startswith("combat.mutantCooldown.") and r.get("variant") != "VANILLA"):
    fail("v0.2.52 Veteran mutant cooldown full-tree contract drifted: expected 146 assignments per non-vanilla case")
elif not any(r.get("parameterId") == "combat.psyBleeding" and r.get("variant") == "ON" and int(r.get("expectedAssignments", "-1")) == 10 for r in _override52_veteran_rows):
    fail("v0.2.52 Veteran PSY bleeding full-tree ON contract missing")
elif _override52_self_doc.get("master", {}).get("status") != "PASS" or _override52_self_doc.get("master", {}).get("nonzeroCasesSelfCompared") != 58:
    fail(f"v0.2.52 Master reference self-compare drifted: {_override52_self_doc.get('master')}")
elif _override52_self_doc.get("veteran-special-arrays", {}).get("status") != "PASS" or _override52_self_doc.get("veteran-special-arrays", {}).get("nonzeroCasesSelfCompared") != 23:
    fail(f"v0.2.52 Veteran reference self-compare drifted: {_override52_self_doc.get('veteran-special-arrays')}")
elif _override52_summary_doc.get("master", {}).get("allVanillaZero") is not True or _override52_summary_doc.get("master", {}).get("contractPass") is not True:
    fail("v0.2.52 aggregate Master override summary drifted")
elif _override52_manifest_rows != 33 or "redirects.Count != 33" not in _override52_writer or "standard-human scope of 33" not in _override52_writer or "redirects.Count != 37" in _override52_writer:
    fail(f"NPC medical production redirect contract drifted: manifest={_override52_manifest_rows}; writer must require exactly 33")
elif "def expected_structures_for_parameter" not in _override52_tool_text or "wrongStructure" not in _override52_tool_text or "(.+?)" not in _override52_tool_text:
    fail("v0.2.52 override verifier structural/parser contract missing")
elif not _override_doc.exists():
    fail("v0.2.52 override verification harness document missing from docs/")
else:
    note("override verification v0.2.52 locked: all 30 runtime controls / 90 Master cases + 36 Veteran array cases; assignment+structure compare; NPC medical 33-link writer fix")

# CFG placement audit release gate (v0.2.53).
_cfg53_tool = ROOT / "tools" / "audit_cfg_placement.py"
_cfg53_doc = DOCS / "CFG-PLACEMENT-AUDIT.md"
_cfg53_root = ROOT / "docs" / "cfg-placement" / "v0.2.53"
_cfg53_summary = _cfg53_root / "summary.json"
_cfg53_parameters = _cfg53_root / "parameter-summary.csv"
_cfg53_direct = _cfg53_root / "direct-targets.csv"
_cfg53_special = _cfg53_root / "special-routes.csv"
_cfg53_files = _cfg53_root / "cfg-file-summary.csv"
_cfg53_summary_doc = json.loads(_cfg53_summary.read_text(encoding="utf-8")) if _cfg53_summary.exists() else {}
_cfg53_expected_routes = {
    "cheapStashAmmoCloneRedirect": 1,
    "cheapStashGrenadeCloneRedirect": 1,
    "destructibleAmmoCloneRedirect": 1,
    "destructibleGrenadeCloneRedirect": 1,
    "directEditPoints": 153,
    "expensiveStashAmmoCloneRedirect": 1,
    "listMembership": 5,
    "mediumStashCloneRedirect": 1,
    "mutantCooldownArray": 8,
    "npcMedicalCloneRedirect": 1,
    "psyArray": 4,
    "setList": 1,
}
_cfg53_parameter_rows = []
_cfg53_direct_rows = []
_cfg53_special_rows = []
if _cfg53_parameters.exists():
    with _cfg53_parameters.open(newline="", encoding="utf-8-sig") as _f:
        _cfg53_parameter_rows = list(_csv.DictReader(_f))
if _cfg53_direct.exists():
    with _cfg53_direct.open(newline="", encoding="utf-8-sig") as _f:
        _cfg53_direct_rows = list(_csv.DictReader(_f))
if _cfg53_special.exists():
    with _cfg53_special.open(newline="", encoding="utf-8-sig") as _f:
        _cfg53_special_rows = list(_csv.DictReader(_f))

if not _cfg53_tool.exists():
    fail("CFG placement audit tool missing: tools/audit_cfg_placement.py")
elif not _cfg53_doc.exists():
    fail("CFG placement audit document missing from docs/")
elif not all(path.exists() for path in (_cfg53_summary, _cfg53_parameters, _cfg53_direct, _cfg53_special, _cfg53_files)):
    fail("v0.2.53 CFG placement evidence is incomplete")
elif _cfg53_summary_doc.get("status") != "PASS":
    fail(f"v0.2.53 CFG placement summary is not PASS: {_cfg53_summary_doc}")
elif (
    _cfg53_summary_doc.get("catalogParameters") != 178
    or _cfg53_summary_doc.get("visible") != 167
    or _cfg53_summary_doc.get("hidden") != 11
    or _cfg53_summary_doc.get("directParameters") != 153
    or _cfg53_summary_doc.get("specialParameters") != 25
    or _cfg53_summary_doc.get("directEditPoints") != 1370
    or _cfg53_summary_doc.get("directCfgFiles") != 14
    or _cfg53_summary_doc.get("directTargetCollisions") != 0
):
    fail(f"v0.2.53 CFG placement accounting drifted: {_cfg53_summary_doc}")
elif _cfg53_summary_doc.get("routeCounts") != _cfg53_expected_routes:
    fail(f"v0.2.53 CFG placement route split drifted: {_cfg53_summary_doc.get('routeCounts')}")
elif len(_cfg53_parameter_rows) != 178 or any(r.get("status") != "PASS" for r in _cfg53_parameter_rows):
    fail("v0.2.53 parameter placement summary must contain exactly 178 PASS rows")
elif len(_cfg53_direct_rows) != 1370:
    fail(f"v0.2.53 direct CFG target inventory drifted: {len(_cfg53_direct_rows)}; expected 1370")
elif len(_cfg53_special_rows) != 25:
    fail(f"v0.2.53 special CFG route inventory drifted: {len(_cfg53_special_rows)}; expected 25")
elif (ROOT / "tools" / "verify_production_writer.py").exists() or (ROOT / "docs" / "production-writer-verification").exists():
    fail("EXE/production-writer verification workflow is out of scope; CFG-only placement audit expected")
else:
    note("CFG placement v0.2.53 locked: 178/178 routed; 153 direct / 25 special; 1370 concrete direct fields across 14 CFG files; 0 collisions")

# CFG provenance closure release gate (v0.2.54).
_cfg54_tool = ROOT / "tools" / "audit_cfg_provenance.py"
_cfg54_doc = DOCS / "CFG-PROVENANCE-AUDIT.md"
_cfg54_root = ROOT / "docs" / "cfg-provenance" / "v0.2.54"
_cfg54_summary = _cfg54_root / "summary.json"
_cfg54_direct = _cfg54_root / "direct-provenance.csv"
_cfg54_special = _cfg54_root / "special-provenance.csv"
_cfg54_readme = _cfg54_root / "README.md"
_cfg54_summary_doc = json.loads(_cfg54_summary.read_text(encoding="utf-8")) if _cfg54_summary.exists() else {}
_cfg54_direct_rows = []
_cfg54_special_rows = []
if _cfg54_direct.exists():
    with _cfg54_direct.open(newline="", encoding="utf-8-sig") as _f:
        _cfg54_direct_rows = list(_csv.DictReader(_f))
if _cfg54_special.exists():
    with _cfg54_special.open(newline="", encoding="utf-8-sig") as _f:
        _cfg54_special_rows = list(_csv.DictReader(_f))
_cfg54_expected_later = {
    "economy.medicStock": 96,
    "loot.droppedWeaponCondition": 242,
    "loot.npcWeaponAmmoSupply": 242,
}
_cfg54_expected_legacy_fp = "0f93942bd60e55869e35ffa21ab05720876fb87520e540e7a78a0420c5f17eff"

if not _cfg54_tool.exists():
    fail("CFG provenance audit tool missing: tools/audit_cfg_provenance.py")
elif not _cfg54_doc.exists():
    fail("CFG provenance audit document missing from docs/")
elif not all(path.exists() for path in (_cfg54_summary, _cfg54_direct, _cfg54_special, _cfg54_readme)):
    fail("v0.2.54 CFG provenance evidence is incomplete")
elif _cfg54_summary_doc.get("status") != "PASS" or _cfg54_summary_doc.get("rawRuntimeDumpBundled") is not False:
    fail(f"v0.2.54 CFG provenance summary status/evidence distinction drifted: {_cfg54_summary_doc}")
elif _cfg54_summary_doc.get("runtimeDumpVersion") != "2.0.5" or _cfg54_summary_doc.get("runtimeDumpSha256") != "739ecb4a60e2d98181cc6b326894d184b76a09c29b94a66c18d7b0da2c37a83a":
    fail("v0.2.54 retained runtime provenance fingerprint drifted")
elif _cfg54_summary_doc.get("direct", {}).get("total") != 1370 or _cfg54_summary_doc.get("direct", {}).get("historicalRuntimeExact") != 699 or _cfg54_summary_doc.get("direct", {}).get("freshEmbeddedSourceExact") != 671 or _cfg54_summary_doc.get("direct", {}).get("freshEmbeddedSourceMismatches") != 0:
    fail(f"v0.2.54 direct provenance accounting drifted: {_cfg54_summary_doc.get('direct')}")
elif _cfg54_summary_doc.get("direct", {}).get("legacyCurrentTupleFingerprintSha256") != _cfg54_expected_legacy_fp:
    fail(f"v0.2.54 retained 699-entry legacy tuple fingerprint drifted: {_cfg54_summary_doc.get('direct', {}).get('legacyCurrentTupleFingerprintSha256')}")
elif _cfg54_summary_doc.get("laterDirectCounts") != _cfg54_expected_later:
    fail(f"v0.2.54 later direct provenance split drifted: {_cfg54_summary_doc.get('laterDirectCounts')}")
elif _cfg54_summary_doc.get("special", {}).get("total") != 25 or _cfg54_summary_doc.get("special", {}).get("historicalRuntimeStructureExact") != 18 or _cfg54_summary_doc.get("special", {}).get("freshEmbeddedCloneRedirect") != 7 or _cfg54_summary_doc.get("special", {}).get("freshTemplateFieldMatches") != 1197 or _cfg54_summary_doc.get("special", {}).get("freshRedirectManifestRows") != 3476 or _cfg54_summary_doc.get("special", {}).get("freshSpecialMismatches") != 0:
    fail(f"v0.2.54 special provenance accounting drifted: {_cfg54_summary_doc.get('special')}")
elif len(_cfg54_direct_rows) != 1370 or sum(r.get("status") == "PASS" for r in _cfg54_direct_rows) != 671 or sum(r.get("status") == "HISTORICAL_PASS" for r in _cfg54_direct_rows) != 699:
    fail("v0.2.54 direct provenance CSV must contain 699 HISTORICAL_PASS + 671 PASS rows")
elif len(_cfg54_special_rows) != 25 or sum(r.get("status") == "PASS" for r in _cfg54_special_rows) != 7 or sum(r.get("status") == "HISTORICAL_PASS" for r in _cfg54_special_rows) != 18:
    fail("v0.2.54 special provenance CSV must contain 18 HISTORICAL_PASS + 7 PASS rows")
else:
    note("CFG provenance v0.2.54 locked: 1370/1370 direct + 25/25 special; 699 historical raw-runtime exact + 671 fresh embedded-source exact; 7 clone routes / 1197 source-template fields / 3476 redirect rows")


# Production implementation integration gate (v0.3.0).
_prod_tool = ROOT / "tools" / "audit_production_integration.py"
_prod_doc = DOCS / "PRODUCTION-IMPLEMENTATION.md"
_source_doc = DOCS / "SOURCE-LAYOUT.md"
_prod_root = ROOT / "docs" / "production-integration" / "v0.3.0"
_prod_summary_path = _prod_root / "summary.json"
_prod_controls_path = _prod_root / "controls.csv"
_prod_dependencies_path = _prod_root / "dependencies.csv"
_prod_readme_path = _prod_root / "README.md"
_prod_summary = json.loads(_prod_summary_path.read_text(encoding="utf-8")) if _prod_summary_path.exists() else {}

_legacy_source = []
_canonical_src = ROOT / "src" / "TrueCustomDifficulty"
for _path in ROOT.rglob("*"):
    if not _path.is_file():
        continue
    try:
        _path.relative_to(_canonical_src)
        continue
    except ValueError:
        pass
    if _path.suffix.lower() in {".cs", ".xaml", ".csproj"}:
        _legacy_source.append(_path.relative_to(ROOT).as_posix())

_prod_main_vm = (_canonical_src / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
_prod_param_vm = (_canonical_src / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
_prod_tab_vm = (_canonical_src / "ViewModels" / "TabViewModel.cs").read_text(encoding="utf-8")
_prod_writer = (_canonical_src / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
_prod_catalog_service = (_canonical_src / "Services" / "ParameterCatalogService.cs").read_text(encoding="utf-8")

if not _prod_tool.exists():
    fail("production integration audit tool missing: tools/audit_production_integration.py")
elif not _prod_doc.exists():
    fail("production implementation document missing from docs/")
elif not _source_doc.exists():
    fail("source layout document missing from docs/")
elif not all(path.exists() for path in (_prod_summary_path, _prod_controls_path, _prod_dependencies_path, _prod_readme_path)):
    fail("v0.3.0 production integration evidence is incomplete")
elif _prod_summary.get("status") != "PASS":
    fail(f"v0.3.0 production integration summary is not PASS: {_prod_summary}")
elif (
    _prod_summary.get("catalogParameters") != 178
    or _prod_summary.get("visibleControls") != 167
    or _prod_summary.get("hiddenAuditOnly") != 11
    or _prod_summary.get("dependentControls") != 15
    or _prod_summary.get("writerRouted") != 178
    or _prod_summary.get("deferredWriterParameters") != 0
):
    fail(f"v0.3.0 production integration accounting drifted: {_prod_summary}")
elif _prod_summary.get("visibleControlTypes") != {"choiceSlider": 1, "slider": 159, "toggle": 7}:
    fail(f"v0.3.0 visible control-type split drifted: {_prod_summary.get('visibleControlTypes')}")
elif _legacy_source:
    fail("legacy C#/XAML project mirrors returned outside canonical src tree: " + ", ".join(sorted(_legacy_source)))
elif "p.IsModified && p.IsControlEnabled" not in _prod_writer:
    fail("production writer no longer suppresses disabled dependent controls")
elif "EnsureVisibleSelection" not in _prod_main_vm or "public void EnsureVisibleSelection()" not in _prod_tab_vm:
    fail("Master dependency selection repair is missing")
elif "OnPropertyChanged(nameof(IsControlVisible));" not in _prod_param_vm:
    fail("dependency visibility notification is missing")
elif "ValidateParameterShape(parameter);" not in _prod_catalog_service or "requires unknown toggle" not in _prod_catalog_service:
    fail("startup catalog/dependency validation is missing")
else:
    note("production integration v0.3.0 locked: 167 visible controls / 178 writer routes; active dependency output; single canonical src tree")

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
_allowed_root_files = {".gitignore", "README.md", "TrueCustomDifficulty.sln", "audit_project_consistency.py", "publish.sh", "publish.ps1"}
_actual_root_files = {p.name for p in ROOT.iterdir() if p.is_file()}
if _actual_root_files != _allowed_root_files:
    fail(f"repository root file set drifted: {sorted(_actual_root_files)}; expected {sorted(_allowed_root_files)}")
elif 'PUBLISH_DIR="$ROOT/publish/win-x64"' not in (ROOT / "publish.sh").read_text(encoding="utf-8"):
    fail("publish.sh no longer isolates runtime output under publish/win-x64")
elif '$publishDir = Join-Path $root "publish\\win-x64"' not in (ROOT / "publish.ps1").read_text(encoding="utf-8"):
    fail("publish.ps1 no longer isolates runtime output under publish/win-x64")
else:
    note("lean root/publish layout locked: 6 root files; docs under docs/, data under Data/, runtime under publish/win-x64/")

print("True Custom Difficulty — static consistency audit")
for msg in notes:
    print(f"[OK] {msg}")
if errors:
    for msg in errors:
        print(f"[FAIL] {msg}")
    sys.exit(1)
print("[OK] no static catalog/writer/resource inconsistencies found")
