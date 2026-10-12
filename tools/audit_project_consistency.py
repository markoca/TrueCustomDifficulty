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
# Check each required knob family independently of the order of the shared
# wheel gate; adding a new approved rotary family must not regress old audits.
def knob_wheel_gate_contains(source: str, *families: str) -> bool:
    gate = next((line.strip() for line in source.splitlines()
                 if line.strip().startswith('if (Family is not (')), '')
    return all(f'"{family}"' in gate for family in families)

def not_run(msg: str) -> None: not_run_items.append(msg)

def walk_parameter_ids(value, path="special"):
    if isinstance(value, dict):
        for key, child in value.items():
            child_path = f"{path}.{key}"
            if key in {"parameterId", "ammoQuantityParameterId", "grenadeChanceParameterId", "grenadeQuantityParameterId", "couponChanceParameterId", "removalPercentParameterId"}:
                yield child_path, child
            elif key in {"psyParameterKeys"} and isinstance(child, dict):
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

# A runtime-implemented, manifest-backed world-weapon shuffle is a special
# writer route, not an ordinary single-field editPoint. Check its exact
# declaration, embedded source and implementation; a name-only entry must
# never satisfy CFG route coverage.
_unique_id = "loot.looseUniqueWeaponShuffle"
_unique_manifest = "TrueCustomDifficulty.Data.loose-unique-weapon-shuffle-manifest.csv"
_unique_route = baseline.get("special", {}).get("looseUniqueWeaponShuffle") or {}
if (_unique_route.get("parameterId") != _unique_id
        or _unique_route.get("manifestResource") != _unique_manifest
        or _unique_route.get("expectedActorCount") != 9):
    fail("unique world weapon shuffle production route/manifest registration drifted")
else:
    _unique_rows_file = DATA / "loose-unique-weapon-shuffle-manifest.csv"
    if not _unique_rows_file.exists():
        fail("unique weapon shuffle production manifest is missing")
    else:
        with _unique_rows_file.open(encoding="utf-8", newline="") as _unique_fh:
            _unique_rows = list(_csv.DictReader(_unique_fh))
        if (len(_unique_rows) != 9
                or len({r["actorSid"] for r in _unique_rows}) != 9
                or len({r["itemSid"] for r in _unique_rows}) != 9):
            fail("unique weapon shuffle must reference exactly nine distinct actor/item pairs")
    _unique_writer = (ROOT / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
    _unique_model = (ROOT / "src/TrueCustomDifficulty/Models/CfgWriterBaseline.cs").read_text(encoding="utf-8")
    if ('WriteLooseUniqueWeaponShuffle(baseline.Special.LooseUniqueWeaponShuffle' not in _unique_writer
            or 'GetManifestResourceStream(route.ManifestResource)' not in _unique_writer
            or 'supported.Add(uniqueShuffleRoute.ParameterId)' not in _unique_writer
            or 'CfgWriterLooseUniqueWeaponShuffle LooseUniqueWeaponShuffle' not in _unique_model):
        fail("unique-weapon baseline route is not wired to actual production writer and model")
    else:
        note("unique world weapon shuffle: declared special writer / nine-actor manifest / live production route")

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
    "combat": (17, 13, 4),
    "health": (13, 10, 3),
    "stamina": (7, 6, 1),
    # Four source-native equipment editors are presented on Survival.
    "survival": (35, 32, 3),
    "economy": (17, 12, 5),
    "equipment": (0, 0, 0),
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
    note("non-generator UI coverage: 89 visible anchors across Combat/Health/Stamina/Survival/Economy/Equipment; current grouped editors and 31 Survival controls accounted")

# Combat deep-audit release gate (v0.2.44).
_combat_doc = DOCS / "COMBAT-CONTROL-DEEP-AUDIT.md"
_combat_rows = {p.get("id"): p for p in parameters if p.get("tab") == "combat" and p.get("exposeInUi", True)}
_combat_runtime_expected = {
    "combat.psySpawnCycleDelay", "combat.psyPhantomCount", "combat.psyBleeding", "combat.psyDurabilityDamage",
}
_combat_runtime_actual = {pid for pid, row in _combat_rows.items() if row.get("support", {}).get("status") == "runtime_test_required"}
_combat_inverse = {
    pid: [t.get("operation") for t in row.get("targets", [])]
    for pid, row in _combat_rows.items() if pid in {"combat.npcAttackCooldown"}
}
_headshot = _combat_rows.get("combat.playerHeadshotMultiplier", {})
_repeated = next((c for c in parameters_doc.get("sliderConstraints", []) if c.get("id") == "repeatedHitProtectionRange"), {})
if len(_combat_rows) != 17 or _combat_runtime_actual != _combat_runtime_expected:
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
    note("combat deep audit locked: 17 visible controls / 13 confirmed direct + 4 runtime-gated; Mutant Health remains a standalone aggregate")

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
# decoded CFG. The fourteen health routes are independent.
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
note("deep CFG mutant HP route gate: 14 health multipliers / 16 MaxHP edit points")

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
    if "GlobalHpMaximum = 7500.0" not in _mhr_text or "DesiredHeight = 605.0" not in _mhr_text:
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
    fail("global anomaly damage editor unexpectedly hidden")
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
        '"AmmoDistribution" => 715.0,',
        '"AmmoDistribution" => new[]',
        '"BreakableBoxYields" => Array.Empty<string>()',
        'protected override void OnMouseLeftButtonDown(',
        'protected override void OnMouseRightButtonDown(',
        'protected override void OnKeyDown(',
    )
    for _token in _abd_required_source:
        if _token not in _abd_control_text:
            _abd_errors.append(f"live aggregate source missing {_token!r}")
    # The only Loot knob bank must be the existing four independently mapped
    # ammo-family weights. Yield controls and the two pool previews are not knobs.
    _abd_knob_start = _abd_control_text.find('DrawText(dc, "AMMO FAMILY WEIGHTS"')
    _abd_knob_end = _abd_control_text.find('DrawText(dc, "AMMO POOL COMPOSITION"', _abd_knob_start)
    _abd_knob_code = (
        _abd_control_text[_abd_knob_start:_abd_knob_end]
        if _abd_knob_start >= 0 and _abd_knob_end > _abd_knob_start else ""
    )
    _abd_knob_expected = (
        '(weight918, "9×18 / 9×19")',
        '(weight045, ".45 ACP")',
        '(weight545, "5.45 / 5.56")',
        '(weight012, "12 GAUGE")',
        'DrawSourceKnob(dc, weights[i].Parameter, weights[i].Label,',
        'knobBankWidth = Math.Min(780.0, ActualWidth - margin * 2.0)',
        'const double knobCardTop = 298.0',
        'const double knobCardHeight = 181.0',
    )
    for _token in _abd_knob_expected:
        if _token not in _abd_knob_code:
            _abd_errors.append(f"ammo-family rotary UI missing {_token!r}")
    if "DrawHorizontalTrackCard" in _abd_knob_code:
        _abd_errors.append("ammo-family horizontal sliders returned")
    _abd_knob_input = (
        'if (Family is not (',
        'if (target.Kind == HitKind.Knob)',
        'protected override void OnMouseWheel(MouseWheelEventArgs e)',
        'EnforceFamilyConstraints(target.Parameter);',
        'private void EnforceAmmoPoolValidity(ParameterViewModel movedParameter)',
    )
    for _token in _abd_knob_input:
        if _token not in _abd_control_text:
            _abd_errors.append(f"ammo-family rotary interaction missing {_token!r}")
    if not knob_wheel_gate_contains(_abd_control_text, "AmmoDistribution"):
        _abd_errors.append("AmmoDistribution missing from shared knob wheel gate")
    if _abd_control_text.count('EnforceFamilyConstraints(target.Parameter);') < 2:
        _abd_errors.append("knob pointer and wheel edits must both enforce SNG pool validity")
    if any("2026-10-11 — Ammo Family Weights: four native rotary knobs"
           not in (ROOT / "docs" / doc).read_text(encoding="utf-8")
           for doc in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md")):
        _abd_errors.append("required Loot rotary knob catalog contract missing")
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
    note("Breakable Boxes: 2 yield rails + 4 native rotary weights; 8 source-backed ammo quantity previews; 2 read-only SNG/NATO pools, with active-knob validity checks")


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
elif len(_health_points) != 42:
    fail(f"health writer surface drifted: {len(_health_points)} edit points; expected 42")
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
elif _aim.get("label") != "Aiming Regen Penalty" or (_aim.get("vanilla") or {}).get("All") != 0.25:
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

if len(_survival_ui) != 35 or len(_survival_confirmed) != 32 or len(_survival_runtime) != 3:
    fail(f"survival deep audit status split drifted: total={len(_survival_ui)} confirmed={len(_survival_confirmed)} runtime={len(_survival_runtime)}; expected 35/32/3")
elif len(_survival_points) != 195:
    fail(f"survival writer surface drifted: {len(_survival_points)} edit points; expected 195 (82 native + 113 moved equipment)")
elif (sum(not p["id"].startswith("equipment.") for p in _survival_ui) != 31
      or sum(p["id"].startswith("equipment.") for p in _survival_ui) != 4
      or sum(not e["parameterId"].startswith("equipment.") for e in _survival_points) != 82
      or sum(e["parameterId"].startswith("equipment.") for e in _survival_points) != 113):
    fail("survival/equipment source IDs or writer partition drifted after moving equipment controls")
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
    note("survival current deep audit: 35 visible / 32 confirmed + 3 runtime-pending; 195 writer leaves (82 native + 113 equipment); hunger sign/order preserved")

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

# Economy dashboard approval contract, 2026-10-11. The seven-control
# baseline, native CFG writer surface and pre-existing responsive layouts
# must remain intact while the Economy-only grouping is compacted.
_economy_aggregate = (ROOT / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
_economy_parameter_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
_economy_responsive = (ROOT / "src/TrueCustomDifficulty/Infrastructure/ResponsiveParameterGridPanel.cs").read_text(encoding="utf-8")
_economy_xaml = (ROOT / "src/TrueCustomDifficulty/MainWindow.xaml").read_text(encoding="utf-8")
# Starting Coupons is now a directly editable slider, not a +/- stepper.
if (any(token in _economy_aggregate for token in
        ('HitKind.CouponDecrease', 'HitKind.CouponIncrease', 'HitKind.CouponValue'))
    or 'AddHorizontalTarget(coupons,' not in _economy_aggregate
    or 'coupons.CurrentNumber, coupons.Minimum' not in _economy_aggregate
    or 'new Rect(track.Left - 8.0, track.Top - 12.0,' not in _economy_aggregate
    or 'var raw = target.VisualMinimum + ratio *' not in _economy_aggregate):
    fail('Starting Coupons native direct-position rail / mouse snapping drifted')
_economy_layout_contract = (
    ('Money Flow group', '"MONEY FLOW"', _economy_parameter_vm),
    ('Market group', '"MARKET"', _economy_parameter_vm),
    ('Expenses group', '"EXPENSES"', _economy_parameter_vm),
    ('two-column money', 'new("economy-moneyflow", 0)', _economy_responsive),
    ('two-column money/Trade', 'new("economy-moneyflow", 1)', _economy_responsive),
    ('two-column expenses', 'new("economy-expenses", 0)', _economy_responsive),
    ('two-column expenses/Repair', 'new("economy-expenses", 1)', _economy_responsive),
    ('money header', 'Value="MONEY FLOW"', _economy_xaml),
    ('market header', 'Value="MARKET"', _economy_xaml),
    ('expenses header', 'Value="EXPENSES"', _economy_xaml),
)
for _name, _token, _source in _economy_layout_contract:
    if _token not in _source:
        fail(f"Economy dashboard responsive groups drifted: {_name}")
if any(f'Value="{old}"' in _economy_xaml
       for old in ('01 / MONEY FLOW', '02 / MARKET', '03 / EXPENSES')):
    fail("Economy section headings still show obsolete 01/02/03 numbering")
if not all(token in _economy_aggregate for token in (
    '=> width >= 390.0 ? 274.0 : 367.0;',
    '=> width >= 390.0 ? 242.0 : 438.0;',
    'Math.Max(49.0, (ActualHeight - 181.0) / 2.0)',
    'Math.Max(52.0, (ActualHeight - 259.0) / 2.0)',
)):
    fail("Economy responsive alignment/compact breakpoints drifted")
# The grouped semantic slot registry is a C# Dictionary, so duplicate keys
# throw during static initialization rather than merely showing a bad layout.
_economy_slot_keys = re.findall(r'\["([^"]+)"\]\s*=\s*new\(', _economy_responsive)
if len(_economy_slot_keys) != len(set(_economy_slot_keys)):
    fail("Economy responsive panel: duplicate semantic slots would crash at startup")
if not all(v in _economy_responsive for v in (
    'parameter.Id is not ("economy.startingMoney"',
    'or "economy.buyCondition" or "economy.fastTravelCost"',
    'or "economy.repairEnemy")',
)):
    fail("Economy pairable visual anchors reverted to full-width rows")
_economy_coupon_start = _economy_aggregate.find('private void DrawCouponFlow(')
_economy_coupon_end = _economy_aggregate.find('private void DrawTraderStockKnobs(', _economy_coupon_start)
_economy_coupon = (_economy_aggregate[_economy_coupon_start:_economy_coupon_end]
                   if 0 <= _economy_coupon_start < _economy_coupon_end else "")
_economy_service_start = _economy_aggregate.find('private void DrawServicePriceKnobs(')
_economy_service_end = _economy_aggregate.find('private void DrawTraderStockKnobs(', _economy_service_start)
_economy_service = (_economy_aggregate[_economy_service_start:_economy_service_end]
                    if 0 <= _economy_service_start < _economy_service_end else "")
_economy_coupon_contract = (
    'P("economy.startingMoney")',
    'P("economy.mainQuestReward")',
    'P("economy.sideQuestReward")',
    'DrawStartingCouponsRail(dc, P("economy.startingMoney")',
    'AddHorizontalTarget(coupons,',
    'coupons.CurrentNumber.ToString("N0", CultureInfo.InvariantCulture)',
    'new Rect(left, 175.0, cardWidth, 84.0)',
    'DrawTradingMeter(dc, P("economy.mainQuestReward")',
    'DrawTradingMeter(dc, P("economy.sideQuestReward")',
)
_economy_service_contract = (
    'P(channels[i].Id)',
    '("economy.fastTravelCost", "FAST TRAVEL")',
    '("economy.upgradeCost", "UPGRADES")',
    '("economy.repairCostBase", "BASE REPAIR")',
    'DrawSourceKnob(dc, P(channels[i].Id)',
    'percentageReadout: channels[i].Id == "economy.repairCostBase"',
)
if not all(token in _economy_coupon for token in _economy_coupon_contract):
    fail("Economy Coupons & Rewards direct coupon rail / reward meters drifted")
if not all(token in _economy_service for token in _economy_service_contract):
    fail("Economy Service Prices native three-knob sources drifted")
if not all(token in _economy_aggregate for token in (
    '"CouponFlow" => CouponBoardHeight(width)',
    '"ServiceCostsBoard" => ServicePriceBoardHeight(width)',
    'return new Size(width, GetDesiredHeight(Family, width));',
    'AddHorizontalTarget(coupons,',
    'var raw = target.VisualMinimum + ratio *',
    'percentageReadout ?',
    'var twoColumns = ActualWidth >= 390.0;',
)) or not knob_wheel_gate_contains(_economy_aggregate, "ServiceCostsBoard"):
    fail("Economy native coupon rail / service knob interaction and size contract drifted")
for _pid, _native in {
    "economy.startingMoney": (0, 100000, 10),
    "economy.mainQuestReward": (0, 2, .01),
    "economy.sideQuestReward": (0, 2, .01),
    "economy.fastTravelCost": (0, 2, .01),
    "economy.upgradeCost": (0, 2, .01),
    "economy.repairCostBase": (0, 2, .01),
}.items():
    _slider = (_economy_rows.get(_pid, {}).get("slider") or {})
    if tuple(_slider.get(k) for k in ("min", "max", "step")) != _native:
        fail(f"Economy redesign changed native CFG parameter range/step: {_pid}")
if any("2026-10-11 — Economy three-section dashboard" not in
       (ROOT / "docs" / _doc).read_text(encoding="utf-8")
       for _doc in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md")):
    fail("Economy three-section dashboard catalog contract missing")


# Equipment deep audit release gate (v0.2.49).
_equipment_doc = DOCS / "EQUIPMENT-CONTROL-DEEP-AUDIT.md"
_equipment_mapping = DOCS / "EQUIPMENT-MAPPING.md"
# Equipment parameters are grouped under Survival in the current catalog.
# Audit stable source IDs, never the (presentation-only) tab assignment.
_equipment_ui = [p for p in parameters
                 if (p.get("id") or "").startswith("equipment.")
                 and p.get("exposeInUi", True)]
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
    fail(f"equipment writer surface drifted: {len(_equipment_points)} edit points; expected 113")
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
if "PreShortDurationFactoryBlobs" not in (ROOT / "src/TrueCustomDifficulty/Services/PresetDirectoryService.cs").read_text(encoding="utf-8"):
    fail("exact-stock factory preset migration hashes for new consumable durations missing")
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
    if len(_values) != 213:
        _factory_errors.append(f"{_name}: {len(_values)} values, expected 213")
    for _short_id, _source_seconds in (
        ("survival.waterRegenDuration", 5.0),
        ("survival.energyFullCostDuration", 5.0),
        ("survival.energyPartialCostDuration", 15.0),
    ):
        _preset_entry = _values.get(_short_id, {})
        if _preset_entry.get("kind") != "slider" or _preset_entry.get("number") != _source_seconds:
            _factory_errors.append(f"{_name}: missing / nonvanilla new short-duration {_short_id}")

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
    note("factory presets locked: 12 custom profiles / 213 values each; retired 4 files removed; hard-profile medical rules preserved")

# Current user-help contract no longer describes retired polar-radar gestures.
_control_help_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
for _required_help in ("public string ControlHelp", "IsVisualAggregateAnchor", "IsMutantHealthAnchor", "IsStashAmmoAnchor"):
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
    "MutantHealthRadar.cs",
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


# Medical Items: four true source-backed HEAL/BLEED pairs share one
# compact native-vertical card; the old separate bleeding visual is retired.
_bar_cap = ROOT / "src/TrueCustomDifficulty/Controls/VerticalBarHandleChrome.cs"
_medical_path = ROOT / "src/TrueCustomDifficulty/Controls/MedicalHealingRadar.cs"
_medical_source = _medical_path.read_text(encoding="utf-8")
_medical_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
_medical_main_vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
_bleeding_caps_expected = {
    "health.bandageBleeding": 100,
    "health.medkitBleeding": 30,
    "health.armyMedkitBleeding": 70,
    "health.ecoMedkitBleeding": 50,
}
_bleeding_caps_by_id = {p.get("id"): p for p in parameters
                        if p.get("id") in _bleeding_caps_expected}
if any(_bleeding_caps_by_id.get(pid, {}).get("slider", {}).get("max") != maximum
       for pid, maximum in _bleeding_caps_expected.items()):
    fail("Medical Items BLEED native limits must be 100/30/70/50 PTS")
if not _bar_cap.exists():
    fail("Medical Items paired vertical cap chrome is missing")
else:
    _cap_source = _bar_cap.read_text(encoding="utf-8")
    if 'internal static class VerticalBarHandleChrome' not in _cap_source:
        fail("Medical Items must reuse canonical VerticalBarHandleChrome")
_required_pairs = (
    'private readonly ParameterViewModel?[] _medical',
    'private readonly ParameterViewModel?[] _bleeding',
    'byId.TryGetValue(BleedingIds[i], out _bleeding[i]);',
    'DrawCenteredText(dc, "MEDICAL ITEMS"',
    'PlotHeight = 151.0',
    'HealingRulerHp = 200.0',
    'BleedingRulerPts = 100.0',
    'DrawColumn(dc, row, HitAction.Healing, _medical[row]',
    'DrawColumn(dc, row, HitAction.Bleeding, _bleeding[row]',
    'VerticalBarHandleChrome.Draw(dc, new Point(centerX, currentY)',
    'new Rect(centerX - Math.Max(16.0, barWidth / 2.0 + 6.0)',
    'HitAction.Healing or HitAction.Bleeding',
    'ParameterAt(hit.Row, hit.Action)?.ResetToReference();',
    'foreach (var medicalItem in _medical.Concat(_bleeding).Concat(_radiation))',
    'ParameterAt(_selected, _selectedAction)',
    'BooleanCheckboxChrome.Draw(dc, _foodCheckboxRect,',
)
if any(token not in _medical_source for token in _required_pairs):
    fail("Medical Items paired 4x2 vertical editor/interaction contract drifted")
if 'BleedingTreatmentMixer' in _abd_control_text or 'BleedingTreatmentMixer' in _medical_vm:
    fail("Retired standalone Bleeding Treatment visual family is still reachable")
if '&& (!IsMedicalHealingMember || IsMedicalHealingAnchor)' not in _medical_vm:
    fail("Medical Items must keep child rows hidden, preserving only medical anchor")
if not all(f'"{pid}"' in _medical_vm and f'"{pid}"' in _medical_main_vm
           for pid in _bleeding_caps_expected):
    fail("Paired BLEED sources must participate in the live medical VM and modified-state graph")
if 'MinHeight="370"' not in _abd_xaml or 'MedicalHealingRadarControl' not in _abd_xaml:
    fail("Medical Items XAML height/renderer registration drifted")
else:
    note("Medical Items: four HEAL/BLEED native bars in one row, independent scales, food toggle")


# The independently edited original Exhaustion sources must remain in the
# gamer-first shared-zone / three-group renderer, not six generic cards.
_exhaustion_names = {
    "stamina.sprintDisableThreshold": "Sprint Locks",
    "stamina.lowRecoveryThreshold": "Recovery Kicks In",
    "stamina.aimRegenCoef": "Aiming Regen Penalty",
    "stamina.sprintRecoveryDelay": "Exhaustion Regen Delay",
    "stamina.lowRecoveryStrength": "Boost Strength",
    "stamina.lowRecoveryDuration": "Boost Duration",
}
# Guard the tooltip C# string concatenation contract. A stale '+ Environment.NewLine'
# statement after a completed return triggers CS0023 and CS0201 at build time.
if re.search(r';[ \t]*\n[ \t]*\+[ \t]*Environment\.NewLine\b', _medical_vm):
    fail("Tooltip concatenation after a terminated C# statement (CS0023/CS0201)")
else:
    note("Tooltip string concatenations have no orphaned Environment.NewLine expression")

_exhaustion_renderer = (ROOT / "src/TrueCustomDifficulty/Controls/ActionCostsRadar.cs").read_text(encoding="utf-8")
_exhaustion_catalog = {p["id"]: p for p in parameters if p["id"] in _exhaustion_names}
_exhaustion_render_markers = (
    'private const double ExhaustionWideHeight = 330.0;',
    'private const double ExhaustionNarrowHeight = 450.0;',
    'DrawStaminaZonesCard(dc,',
    'DrawRecoveryStateCard(dc, _aimParameter, "AIMING", "REGEN PENALTY"',
    'DrawRecoveryStateCard(dc, _sprintRecoveryDelay, "AFTER EXHAUSTION", "REGEN DELAY"',
    'DrawLowStaminaBoostCard(dc,',
    'DrawRecoverySourceTrack(dc, _lowRecoveryStrength, "STRENGTH"',
    'DrawRecoverySourceTrack(dc, _lowRecoveryDuration, "DURATION"',
    '"SPRINT LOCKS"', '"RECOVERY KICKS IN"',
    'const double staminaMin = 0.0;',
    'const double staminaMax = 60.0;',
    'y - 34.0', 'y + 2.0',
)
if (any(_exhaustion_catalog.get(pid, {}).get("label") != name
        for pid, name in _exhaustion_names.items())
    or any(marker not in _exhaustion_renderer for marker in _exhaustion_render_markers)
    or 'DrawExhaustionTrackCard(' in _exhaustion_renderer):
    fail("Stamina States: Exhaustion layout or six source-backed handles drifted")
else:
    note("Stamina States: two independently draggable 0..60 SP markers and three source-backed groups")

# Radiation Removal is no longer a separate AggregateVisualControl family.
# Four native-source vertical RAD columns must sit in one compact right bank,
# with a physically distinct 0..200 RAD axis, cap hover, and source-true hit map.
_radiation_sources = ("health.ecoMedkitAntirad", "health.antiRadRadiation",
                      "health.beerRadiationRemoval", "health.vodkaRadiationRemoval")
_radiation_caps = {"health.ecoMedkitAntirad": 120, "health.antiRadRadiation": 200,
                   "health.beerRadiationRemoval": 30, "health.vodkaRadiationRemoval": 100}
_radiation_catalog = {p.get("id"): p for p in parameters if p.get("id") in _radiation_sources}
if any(_radiation_catalog.get(pid, {}).get("slider", {}).get("max") != limit
       for pid, limit in _radiation_caps.items()):
    fail("Medical Items radiation-removal editable ceilings must be 120/200/30/100 RAD")
_rad_medical_tokens = (
    "private static readonly Brush RadiationAccent",
    "private const double RadiationRulerPts = 200.0;",
    '("health.ecoMedkitAntirad", "SCIENTIFIC MEDKIT")',
    '("health.antiRadRadiation", "ANTI-RAD")',
    "private readonly ParameterViewModel?[] _radiation = new ParameterViewModel?[4];",
    '("health.beerRadiationRemoval", "BEER")',
    '("health.vodkaRadiationRemoval", "VODKA")',
    "byId.TryGetValue(RadiationItems[i].Id, out _radiation[i]);",
    "DrawRadiationGroup(dc, radArea, chart, text, muted, border, panel);",
    'DrawCenteredText(dc, "RADIATION REMOVAL",',
    'DrawCenteredText(dc, "RAD", radAxisCenter,',
    "new RotateTransform(90.0,",
    'HitAction.Radiation => $"{parameter.CurrentNumber:0.#} RAD",',
    "DrawColumn(dc, i, HitAction.Radiation, _radiation[i],",
    "VerticalBarHandleChrome.Draw(dc, new Point(centerX, currentY)",
    "if (action == HitAction.Radiation)",
    "row < _radiation.Length ? _radiation[row] : null",
    "_medical.Concat(_bleeding).Concat(_radiation)",
    "action == HitAction.Healing ? barAccent : BleedingAccent",
)
if any(token not in _medical_source for token in _rad_medical_tokens):
    fail("Medical Items RAD bank or gold HEAL caption implementation drifted")
if "RadiationTreatment" in _abd_control_text or "RadiationTreatment" in _medical_vm:
    fail("Old separate Radiation Treatment aggregate family returned")
if any(f'"{pid}"' not in _medical_vm or f'"{pid}"' not in _medical_main_vm
       for pid in _radiation_sources):
    fail("Radiation Removal sources must remain in active Medical Items VM graph")
if '&& (!IsMedicalHealingMember || IsMedicalHealingAnchor)' not in _medical_vm:
    fail("Medical Items RAD controls must not become standalone rows")
else:
    note("Medical Items: right-hand four-column RAD bank, teal vertical 0..200 axis, gold HEAL captions")


# Beer / Vodka were once controlled by a single shared multiplier.
# Their original negative source deltas are now independently owned.
_alcohol_edits = [e for e in baseline.get("editPoints", [])
                  if e.get("parameterId") in {
                      "health.beerRadiationRemoval",
                      "health.vodkaRadiationRemoval"}]
if len(_alcohol_edits) != 4 or any(e.get("operation") != "negateSet"
                                   for e in _alcohol_edits):
    fail("Beer/Vodka RAD need four source-exact negative signed writer edits")
elif any(e.get("parameterId") == "survival.alcoholRadiationRemoval"
         for e in baseline.get("editPoints", [])):
    fail("Retired shared alcohol multiplier must not still write either source")
elif any(e.get("sourceRaw") !=
         ("-10" if e.get("parameterId") == "health.beerRadiationRemoval" else "-50")
         for e in _alcohol_edits):
    fail("Beer/Vodka RAD source values changed from -10/-50")
else:
    note("Beer/Vodka independent native radiation sources and sign-preserving writer")


# Shared Beer+Vodka RAD multiplier was replaced by independent HEAL-tab
# editors, and historical presets must continue to produce their old results.
_alcohol_preset_tokens = (
    'preset.Values.TryGetValue("survival.alcoholRadiationRemoval"',
    '("health.beerRadiationRemoval", 10.0)',
    '("health.vodkaRadiationRemoval", 50.0)',
    'source.SnapCurrentNumberToMarker(',
)
_consumable_source = (_semantic_source_dir / "AggregateVisualControl.cs").read_text(encoding="utf-8")
if any(token not in _medical_main_vm for token in _alcohol_preset_tokens):
    fail("Legacy Beer/Vodka shared multiplier preset migration has drifted")
elif '"survival.alcoholRadiationRemoval"' in _consumable_source:
    fail("Consumable Effects must not revive duplicate old shared alcohol RAD control")
else:
    note("Legacy 1x alcohol presets migrate to independent Beer 10/Vodka 50 RAD")


# Medical Items Y-axis labels must follow the matching slider colors
# and be rotated vertically inside the existing dedicated side gutters.
_medical_axis_contract = (
    'var hpAxisCenter = new Point(chart.Left - 38.0, axisMidY);',
    'new RotateTransform(-90.0, hpAxisCenter.X, hpAxisCenter.Y)',
    'DrawCenteredText(dc, "HP", hpAxisCenter, HealthTypography.Axis,',
    'accent, FontWeights.Bold);',
    'var ptsAxisCenter = new Point(chart.Right + 44.0, axisMidY);',
    'new RotateTransform(90.0, ptsAxisCenter.X, ptsAxisCenter.Y)',
    'DrawCenteredText(dc, "PTS", ptsAxisCenter, HealthTypography.Axis,',
    'BleedingAccent, FontWeights.Bold);',
)
if any(token not in _medical_source for token in _medical_axis_contract):
    fail("Medical Items left gold HP and right copper PTS Y-axis captions must stay vertical")
elif 'DrawText(dc, "HP", new Point(19.0, 108.0)' in _medical_source:
    fail("Medical Items obsolete horizontal HP axis caption returned")
else:
    note("Medical Items vertical Y-axis titles: gold HP left and copper PTS right")


# Medical Items final polish: compact pair spacing, subdued ceiling/reference
# ticks and stronger OFF checkbox edge, without changing canonical checkbox chrome.
_medical_polish_required = (
    'var spread = Math.Min(23.0, laneWidth * .22);',
    'CloneWithOpacity(muted, .38), .8',
    'CloneWithOpacity(muted, .68), 1.35',
    'var checkboxEdge = CloneWithOpacity(muted, .75);',
    '_foodCheckboxHovered, panel, checkboxEdge, accent);',
)
if any(token not in _medical_source for token in _medical_polish_required):
    fail("Medical Items compact pair/quiet guides/clear checkbox polish drifted")
else:
    note("Medical Items polish: tighter paired bars, muted reference lines, visible Food Healing border")


# Hunger Survival Profile LOW handle: avoid a second hover circle at the
# midpoint of the 2D plot. The real LOW handle owns its glow at Y(0).
_hunger_source = (_semantic_source_dir / "AggregateVisualControl.cs").read_text(encoding="utf-8")
_hunger_overlay = _hunger_source.split("private void DrawHoveredHandle(", 1)[-1].split(
    "private void DrawBlastDamageBoard(", 1)[0]
if (
    'DrawHumanAxisHandle(dc, lowHandle, accent, panel,' not in _hunger_source
    or 'Family == "HungerLadder"' not in _hunger_overlay
    or 'target.Kind == HitKind.Horizontal' not in _hunger_overlay
    or 'source.Id == "survival.hungerLowThreshold"' not in _hunger_overlay
):
    fail("Hunger LOW hover regression: local handle glow and duplicate-overlay suppression must coexist")
else:
    note("Hunger LOW single-hover contract: actual Y(0) handle glow, no generic plot-midpoint overlay")


# Hunger and Fatigue now share the SAME primary resource ruler as Core
# Health/Core Stamina. Protect geometry/units/source routing, not screenshots.
_primary_src = (_semantic_source_dir / "AggregateVisualControl.cs").read_text(encoding="utf-8")
_primary_ruler_start = _primary_src.find("private void DrawCoreResourceRuler(")
_primary_ruler_end = _primary_src.find("private void DrawYourWeaponsKnobs(", _primary_ruler_start)
_primary_ruler = (_primary_src[_primary_ruler_start:_primary_ruler_end]
                  if 0 <= _primary_ruler_start < _primary_ruler_end else "")
_hunger_start = _primary_src.find("private void DrawHungerLadder(")
_hunger_preview = _primary_src.find("const double previewTop = SurvivalPrimaryPreviewTop;", _hunger_start)
_hunger_first_row = (_primary_src[_hunger_start:_hunger_preview]
                     if 0 <= _hunger_start < _hunger_preview else "")
_fatigue_start = _primary_src.find("private void DrawFatigueCurve(")
_fatigue_preview = _primary_src.find("var previewTop = SurvivalPrimaryPreviewTop;", _fatigue_start)
_fatigue_first_row = (_primary_src[_fatigue_start:_fatigue_preview]
                      if 0 <= _fatigue_start < _fatigue_preview else "")
_primary_expected = {
    "Hunger": (_hunger_first_row, (
        'DrawCoreResourceRuler(dc, rate, "HUNGER RATE", "pts/s"',
        'DrawCoreResourceRuler(dc, wellFed, "WELL-FED STAMINA RECOVERY", "%"',
    )),
    "Fatigue": (_fatigue_first_row, (
        'DrawCoreResourceRuler(dc, rate, "FATIGUE RATE", "pts/s"',
        'DrawCoreResourceRuler(dc, relief, "ENERGY DRINK FATIGUE RELIEF", "x"',
    )),
}
for _title, (_renderer, _required) in _primary_expected.items():
    if (not all(token in _renderer for token in _required)
        or "DrawCompactTrack(dc," in _renderer
        or "CompactSlot(" in _renderer):
        fail(f"{_title} primary card renderer no longer matches Core Health/Core Stamina native rulers")
if not all(token in _primary_ruler for token in (
        '"pts/s" => $"{parameter.CurrentNumber:0.000} pts/s"',
        '"%" => $"{(parameter.CurrentNumber > Epsilon ? "+" : "")}{parameter.CurrentNumber * 100.0:0.#}%"',
        '"pts/s" => $"{parameter.Maximum:0.000} pts/s"',
        '"%" => $"{parameter.Maximum * 100.0:0.#}%"',
        'DrawLine(new Pen(muted, 1.8)',
        'AddHorizontalTarget(parameter,')):
    fail("Hunger/Fatigue native pts/s and percent readouts, references or interactions drifted")
if not all(token in _primary_src for token in (
        'private const double SurvivalPrimaryRulerTop = 51.0;',
        'private const double SurvivalPrimaryRulerHeight = 105.0;',
        'SurvivalPrimaryRulerTop + SurvivalPrimaryRulerHeight + 12.0;',
        'return SurvivalPrimaryPreviewTop + 80.0 + 51.0 + 365.0 + 12.0;',
        'var previewBottom = SurvivalPrimaryPreviewTop + 80.0;')):
    fail("Hunger/Fatigue live source height and responsive stage layout diverged")
for _file in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md"):
    if "2026-10-11 — Matched Hunger/Fatigue primary resource rulers" not in (DOCS / _file).read_text(encoding="utf-8"):
        fail(f"Shared Hunger/Fatigue primary ruler specification not synchronized: {_file}")
note("Hunger/Fatigue starting sliders: identical Core Health/Stamina ruler layout, native rate/bonus units, stage/chart height aligned")

# Radiation System source-to-renderer regression: short recovery terminology,
# and measured label width rather than the old fixed 155-DIP reservation.
_radiation_source = (_semantic_source_dir / "AggregateVisualControl.cs").read_text(encoding="utf-8")
_radiation_help = (ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
# Three native WPF rotary sources, with the middle recovery source in PTS/S
# rather than a fictitious x multiplier. Verify actual source dispatch.
_radiation_begin = _radiation_source.find('private void DrawRadiationBalance(')
_radiation_finish = _radiation_source.find('\n    private ', _radiation_begin + 15)
_radiation_renderer = (_radiation_source[_radiation_begin:_radiation_finish]
                       if 0 <= _radiation_begin < _radiation_finish else '')
_radiation_required = (
    '("survival.radiationAccumulation", "BUILD-UP")',
    '("survival.radiationRecovery", "RECOVERY")',
    '("survival.radiationDamage", "HP DAMAGE")',
    'DrawSourceKnob(dc, P(source.Id), source.Label,',
    'rateReadout: source.Id == "survival.radiationRecovery"',
    'const double centerExtra = 18.0;',
    'var sideWidth = (bankWidth - gap * 2.0 - centerExtra) / 3.0;',
    'var hero = i == 1;',
    'new Rect(knobLeft, 49.0, knobWidth, hero ? 188.0 : 181.0)',
    'prominent: hero);',
)
if (not all(item in _radiation_renderer for item in _radiation_required)
    or 'DrawNativeGauge(dc, P(source.Id)' in _radiation_renderer
    or not knob_wheel_gate_contains(_radiation_source, 'RadiationBalance')
    or 'rateReadout || nativeUnit is not null' not in _radiation_source
    or '"RadiationBalance" => RadiationBoardHeight' not in _radiation_source
    or 'RadiationBoardHeight(double width)' not in _radiation_source
    or '"RadiationBalance" => RadiationBoardHeight(width)' not in _radiation_source
    or 'DrawRadiationControls(dc, text, muted, soft, accent, panel);' not in _radiation_renderer
    or 'private void DrawRadiationControls(' not in _radiation_source
    or '"RADIATION LEVELS"' in _radiation_renderer
    or '"RAD CONTROLS"' in _radiation_source
    or '"RADIATION LEVEL"' in _radiation_source
    or 'RADIATION DAMAGE STAGES' in _radiation_renderer
    or 'foreach (var threshold in RadiationRingThresholds)' not in _radiation_source
    or 'DrawRadiationHudSegments(dc, center, currentRad,' not in _radiation_renderer
    or 'DrawRadiationHudTrefoil(dc, center, CloneWithOpacity(text, 0.82));' not in _radiation_renderer
    or 'private const int RadiationHudSegments = 32;' not in _radiation_source
    or 'private double _radLevel = 0.0;' not in _radiation_source
    or 'private const double RadiationDialTrackRadius = 122.0;' not in _radiation_source
    or 'return distance >= 108.0 && distance <= 136.0;' not in _radiation_source
    or 'Math.Clamp(Math.Round(angle * 100.0 / (Math.PI * 2.0)), 0.0, 100.0)' not in _radiation_source
    or 'DrawCenteredText(dc, currentRad.ToString(' not in _radiation_renderer
    or '29.0, accent, FontWeights.Bold);' not in _radiation_renderer
    or 'if (currentRad > 0.0)' not in _radiation_renderer
    or 'private const double RadiationHudSegmentGap = 0.36;' not in _radiation_source
    or 'private const int RadiationHudTickStep = 10;' not in _radiation_source
    or 'DrawRadiationRingSector(dc, center, radius, first, litEnd, active);' not in _radiation_source
    or 'if (litEnd > first + Epsilon)' not in _radiation_source
    or '"1–100 RAD"' in _radiation_renderer
    or '"FIXED RAD"' in _radiation_source
    or 'dc.DrawRoundedRectangle(panel, null, profile, 4.0, 4.0);' not in _radiation_renderer
    or 'dc.DrawRoundedRectangle(panel, null, area, 4.0, 4.0);' not in _radiation_source
    or 'DrawRoundedRectangle(panel, new Pen(border, 0.9)' in _radiation_renderer
    or 'DrawRoundedRectangle(panel, new Pen(border, .9)' in _radiation_source
    or '"DAMAGE PER TICK"' not in _radiation_source
    or '"DEAD AFTER"' not in _radiation_source
    or '" TICKS"' not in _radiation_source
    or '"DEATH ESTIMATE"' not in _radiation_source
    or 'var hasDeathEstimate = double.IsFinite(eta);' not in _radiation_source
    or 'if (hasDeathEstimate)' not in _radiation_source
    or '"MAX " + RadiationModelMaxHp.ToString(' in _radiation_source
    or 'void DrawStat(string value, double y, double fontSize, Brush ink, FontWeight weight)' not in _radiation_source
    or 'CreateText(value, fontSize, ink, weight, contentWidth)' not in _radiation_source
    or 'DrawStat(stage, wide ? 76.0 : 43.0,' not in _radiation_source
    or 'DrawStat(stageRange, wide ? 117.0 : 81.0,' not in _radiation_source
    or 'wide ? 192.0 : 140.0, 29.0,' not in _radiation_source
    or 'wide ? 273.0 : 199.0, 29.0, accent, FontWeights.Bold);' not in _radiation_source
    or 'wide ? 315.0 : 237.0, 27.0, text, FontWeights.Bold);' not in _radiation_source
    or 'DrawStat("NO RAD DAMAGE", wide ? 286.0 : 215.0,' not in _radiation_source
    or 'private const double RadiationDialCenterY = 183.0;' not in _radiation_source
    or 'profile.Top + RadiationDialCenterY' not in _radiation_renderer
    or 'Math.Ceiling(RadiationModelMaxHp / damagePerAssumedCharge)' not in _radiation_source
    or '"1 TICK/S ASSUMED · NO HEALING/PROTECTION"' in _radiation_source
    or '"CFG DAMAGE / CHARGE"' in _radiation_source
    or '"EST. HP ZERO · 1 TICK/S"' in _radiation_source
    or 'HUD-STYLE PREVIEW · NOT GAME HUD' in _radiation_renderer
    or '11 CFG STAGES · 99 RAD DEADLY' in _radiation_renderer
    or 'DrawRadiationRingSector(dc, center, radius, 0.0, currentRad, accent);' in _radiation_renderer
    or 'DrawRightAlignedText(dc, stageRange,' in _radiation_source
    or 'const double centerExtra = 18.0;' not in _radiation_renderer
    or 'prominent: hero);' not in _radiation_renderer
    or '("survival.radiationDamage", "HP DAMAGE"),\n            ("survival.radiationRecovery", "RECOVERY")' not in _radiation_renderer
    or 'const double outer = 96.0;' not in _radiation_source
    or 'const double inner = 27.0;' not in _radiation_source
    or 'dc.DrawEllipse(symbol, null, center, 23.0, 23.0);' not in _radiation_source
    or 'RadiationRingPoint(center, 137.0, tick)' not in _radiation_renderer
    or 'for (var tick = 10; tick <= 100; tick += RadiationHudTickStep)' not in _radiation_renderer
    or 'foreach (var threshold in RadiationRingThresholds)' in _radiation_renderer
    or 'threshold.ToString(' in _radiation_renderer
    or 'DrawCenteredText(dc, "RAD",' in _radiation_renderer
    or 'RadiationStepButton(' in _radiation_source
    or 'RadiationStepHit(' in _radiation_source
    or 'RadiationHudPolar(center, 28.0, angle)' in _radiation_source
    or 'RadiationLevelRail(' in _radiation_source
    or 'RadiationRailHit(' in _radiation_source
    or 'SetRadiationFromRail(' in _radiation_source
    or '_radLevelDragging' in _radiation_source
    or 'var stageRange = index < 0 ? "0 RAD"' not in _radiation_source
    or 'var stageTo = index < 0 ? 0.0' not in _radiation_source
    or 'private double RadiationTimeToZeroHp()' not in _radiation_source
    or 'RADIATION SIMULATION' in _radiation_source
    or 'RADIATION TIMELINE' in _radiation_source):
    fail("Radiation System three native source knobs, PTS/S unit or wheel mapping drifted")
elif '"RadiationBalance" => "Turn BUILD-UP' not in _radiation_help:
    fail("Radiation System context still describes retired vertical gauges")
else:
    note("Radiation: 0–100 ring, 6x central damage knob, 29-DIP stage/damage/ticks and separate time")
for _catalog, _heading in (
    ("UI-DESIGN-LOCK.md", "2026-10-13 — Radiation: unified large stats and centered HP DAMAGE hero knob"),
    ("VISUAL-CONTROL-SYSTEM.md", "2026-10-13 — Radiation information hierarchy and HP DAMAGE center hero"),
):
    if _heading not in (DOCS / _catalog).read_text(encoding="utf-8"):
        fail(f"Radiation UI/catalog visual lock missing or outdated: {_catalog}")

# Consumable timeline tiny ticks are two independently source-backed
# timed effect families. Guard both the authored durations and the live visual
# label/measurement contract, rather than inventing additional editable sources.
_duration_sources = {
    "survival.waterEffectDuration": {"WaterStaminaPerAction1": "30.0"},
    "survival.waterRegenDuration": {"WaterStamina2": "5.0"},
    "survival.energyDrinkEffectDuration": {
        "EnergeticStamina": "45.0", "EnergeticLimitedStamina": "45.0"},
    "survival.energyFullCostDuration": {"EnergeticStaminaPerAction2": "5.0"},
    "survival.energyPartialCostDuration": {"EnergeticStaminaPerAction1": "15.0"},
}
for _pid, _expected in _duration_sources.items():
    _actual = {
        point["prototype"]: point["sourceRaw"]
        for point in baseline.get("editPoints", [])
        if point.get("parameterId") == _pid
        and point.get("key") == "Duration"
    }
    if _actual != _expected:
        fail(f"consumable duration source/legend mapping drifted: {_pid}: {_actual} != {_expected}")
_duration_renderer = (_semantic_source_dir / "AggregateVisualControl.cs").read_text(encoding="utf-8")
for _token in (
    "private static double ConsumableTimelineHeight(double width)",
    "new Rect(17.0, previewTop, width, ConsumableTimelineHeight(ActualWidth))",
    "=> ConsumableMedicineKnobTop(width) + 181.0 + 14.0;",
    "var secondary = new ParameterViewModel[][]",
    "shortEffect.CurrentNumber",
    "DrawCompactTrack(dc, shortEffect, caption,",
    "(waterRegenTime, \"WATER +25% STAMINA EFFECT DURATION\")",
    "(energyFullCostTime, \"ENERGY DRINK -100% STAMINA COST DURATION\")",
    "(energyPartialCostTime, \"ENERGY DRINK -30% STAMINA COST DURATION\")",
):
    if _token not in _duration_renderer:
        fail(f"consumable short-duration native control/measurement missing: {_token}")
if _duration_renderer.count("DrawCompactTrack(dc, shortEffect, caption,") != 1:
    fail("consumable child duration track registration drifted")

# Source-native medicine magnitude/duration contracts; Hercules is coupled.
_medicine_contract = {
    "survival.vincaBleedRecovery": (("CinnamonDegenBleeding",), ("ValueMin", "ValueMax"), "10.0"),
    "survival.vincaDuration": (("CinnamonDegenBleeding",), ("Duration",), "180.0"),
    "survival.herculesCarryBonus": (("HerculesWeight", "HerculesWeight_Penalty"), ("ValueMin", "ValueMax"), "20.0"),
    "survival.herculesDuration": (("HerculesWeight", "HerculesWeight_Penalty"), ("Duration",), "300.0"),
    "survival.psiBlockRecovery": (("PSYBlockerIncreaseRegen",), ("ValueMin", "ValueMax"), "10.0"),
    "survival.psiBlockDuration": (("PSYBlockerIncreaseRegen",), ("Duration",), "60.0"),
}
for _medicine_id, (_protos, _keys, _raw) in _medicine_contract.items():
    _expected = {(proto, key, _raw) for proto in _protos for key in _keys}
    _observed = {(p["prototype"], p["key"], p["sourceRaw"])
                 for p in baseline["editPoints"] if p["parameterId"] == _medicine_id
                 and p["operation"] == "set"}
    if _observed != _expected:
        fail(f"medicine source/coupling drift for {_medicine_id}")
    if f'"{_medicine_id}"' not in _duration_renderer:
        fail(f"medicine editor absent from Consumable Mixer: {_medicine_id}")
_medicine_vm_source = (ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
_vm_medicine_mount = _medicine_vm_source.split("var consumableAggregateParameterIds =", 1)[-1].split("};", 1)[0]
for _medicine_id in _medicine_contract:
    if f'"{_medicine_id}"' not in _vm_medicine_mount:
        fail(f"medicine hidden aggregate source missing from live VM: {_medicine_id}")
if ("private static double ConsumableMedicineKnobTop(double width)" not in _duration_renderer
    or "private const double ConsumableDurationTop = 250.0;" not in _duration_renderer
    or "medicineLanes = new (ParameterViewModel Source, string Label)[]" not in _duration_renderer
    or "medicineMaximumSeconds = 900.0" not in _duration_renderer
    or "DrawSourceKnob(dc, vincaStrength, \"VINCA BLEED REGEN\"" not in _duration_renderer
    or "DrawSourceKnob(dc, herculesStrength, \"HERCULES CARRY\"" not in _duration_renderer
    or "DrawSourceKnob(dc, psiStrength, \"PSI BLOCK PROTECTION\"" not in _duration_renderer
    or "DrawConsumableMedicineEffects(" in _duration_renderer):
    fail("medicine strength knob and integrated 8-lane duration chart contract missing")
# The source-native amount is displayed as real gamer-facing units,
# not relative multiplier / percent or an unverified per-second rate.
_native_medicine_units = {
    "survival.vincaBleedRecovery": ("bleed pts", "BLEED PTS", "VINCA BLEED REGEN"),
    "survival.herculesCarryBonus": ("kg", "KG", "HERCULES CARRY"),
    "survival.psiBlockRecovery": ("psi pts", "PSI PTS", "PSI BLOCK PROTECTION"),
}
for _pid, (_unit, _face, _title) in _native_medicine_units.items():
    if (next(p for p in parameters if p["id"] == _pid)["display"].get("unit") != _unit
        or f'nativeUnit: "{_face}"' not in _duration_renderer
        or f'DrawSourceKnob(dc, ' not in _duration_renderer
        or f'"{_title}"' not in _duration_renderer):
        fail(f"medicine native points/kg UI unit contract drifted: {_pid}")
if ("else if (nativeUnit is not null)" not in _duration_renderer
    or "parameter.CurrentNumber > Epsilon" not in _duration_renderer):
    fail("medicine compact dial no longer displays signed values with their own source-native units")
# Source-exact seconds constraints: no fixed fake cap or duplicate writer ownership.
_short_duration_bases = {
    "survival.waterRegenDuration": ("survival.waterEffectDuration", 5.0, 30.0),
    "survival.energyFullCostDuration": ("survival.energyDrinkEffectDuration", 5.0, 45.0),
    "survival.energyPartialCostDuration": ("survival.energyDrinkEffectDuration", 15.0, 45.0),
}
_short_by_id = {p["id"]: p for p in parameters}
for _pid, (_main_id, _min_seconds, _main_seconds) in _short_duration_bases.items():
    _p = _short_by_id[_pid]
    _slider = _p["slider"]
    _main = _short_by_id[_main_id]
    _source = [e for e in baseline["editPoints"] if e.get("parameterId") == _pid]
    if (_p.get("exposeInUi", True) or
        _slider.get("min") != _min_seconds or
        _slider.get("max") != _main_seconds * 3.0 or
        _slider.get("step") != 0.5 or
        _slider.get("stepMode") != "absolute" or
        _p["vanilla"]["All"] != _min_seconds or
        len(_source) != 1 or _source[0]["operation"] != "set"):
        fail(f"consumable independent short-duration catalog/CFG contract drifted: {_pid}")
    if abs(_main["slider"]["min"] -
           max(p["slider"]["min"] for p in (
               _short_by_id[x] for x, z in _short_duration_bases.items()
               if z[0] == _main_id)) / _main_seconds) > 1e-9:
        fail(f"main duration lower bound no longer respects authored child minima: {_main_id}")
_main_source = (ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
_vm_source = (ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
for _token in ('LinkShortDrinkDuration("survival.waterRegenDuration"',
               'LinkShortDrinkDuration("survival.energyFullCostDuration"',
               'LinkShortDrinkDuration("survival.energyPartialCostDuration"',
               'child.RefreshDynamicMaximum()', "MigrateLegacyShortDuration("):
    if _token not in _main_source:
        fail(f"consumable runtime cap/migration handler missing: {_token}")
for _token in ("SetDynamicMaximumProvider(Func<double> provider)",
               "Math.Max(Minimum, _dynamicMaximumProvider())",
               'Id is "survival.waterEffectDuration" or "survival.energyDrinkEffectDuration"'):
    if _token not in _vm_source:
        fail(f"consumable maximum/rounding VM contract missing: {_token}")
# UI-only universal ceiling: no extra CFG source or catalog parameter.
_override_main = ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs"
_override_vm = ROOT / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs"
_override_preset = ROOT / "src/TrueCustomDifficulty/Models/PresetFile.cs"
_override_main_text = _override_main.read_text(encoding="utf-8")
_override_vm_text = _override_vm.read_text(encoding="utf-8")
_override_preset_text = _override_preset.read_text(encoding="utf-8")
if "survival.drinkDurationOverride" in _short_by_id:
    fail("drink-duration UI-only override must not become a fictitious CFG parameter")
for _token in (
    'var drinkDurationMode = parametersById["survival.waterInstantStaminaRestore"];',
    "const double sharedDrinkDurationMaximumSeconds = 270.0;",
    "drinkDurationMode.DrinkDurationOverrideEnabled",
    "sharedDrinkDurationMaximumSeconds / vanillaSeconds",
    "? sharedDrinkDurationMaximumSeconds",
    "main.RefreshDynamicMaximum();",
    "child.RefreshDynamicMaximum();",
    "const double sharedMedicineDurationMaximumSeconds = 900.0;",
    'parametersById["survival.vincaDuration"]',
    'parametersById["survival.herculesDuration"]',
    'parametersById["survival.psiBlockDuration"]',
    "medicineDuration.RefreshDynamicMaximum();",
    "DrinkDurationOverride = AllParameters.First(",
    "DrinkDurationOverrideEnabled = preset.DrinkDurationOverride;",
):
    if _token not in _override_main_text:
        fail(f"global Drink Duration Override VM/wiring/preset missing: {_token}")
for _token in (
    "public bool DrinkDurationOverrideEnabled",
    "Math.Max(Minimum, _dynamicMaximumProvider())",
):
    if _token not in _override_vm_text:
        fail(f"Drink Duration Override ParameterViewModel property/limits missing: {_token}")
if ('[JsonPropertyName("drinkDurationOverride")]' not in _override_preset_text
    or "public bool DrinkDurationOverride" not in _override_preset_text):
    fail("preset root missing UI-only drinkDurationOverride boolean")
for _token in (
    "OVERRIDE LIMITS",
    "BooleanCheckboxChrome.Draw(dc, checkboxRect, burst.DrinkDurationOverrideEnabled",
    "HitKind.DrinkDurationOverride",
    "previewTop + 67.0",
    "preview.Top + 300.0",
    "return 300.0 + rows * CompactTrackHeight",
    "const double medicineMaximumSeconds = 900.0;",
    "target.Parameter.DrinkDurationOverrideEnabled =",
    "_selectedParameter.DrinkDurationOverrideEnabled =",
):
    if _token not in _duration_renderer:
        fail(f"Drink Duration Override compact renderer/input contract missing: {_token}")
note("Consumable Duration Override: OFF native caps; ON drinks 270s, Vinca/Hercules/Psi Block 900s; single preset flag, no CFG boolean")

note("consumable duration source routes: five primary drink effects + three independently editable short effects; original CFG prototype ownership stays unique")

# The individual SPDrain magnitudes and variant memberships are locked
# to the supplied original EffectPrototypes/ItemPrototypes source audit.
_energy_duration = next(p for p in parameters if p.get("id") == "survival.energyDrinkEffectDuration")
_energy_notes = " ".join(_energy_duration.get("support", {}).get("notes", []))
for _expected_effect_fact in (
    "EnergeticStaminaPerAction1", "EnergeticStaminaPerAction2",
    "-30%", "15 s", "-100%", "5 s", "Energetic_Limited",
    "MeleeButstock", "Engine stacking",
):
    if _expected_effect_fact not in _energy_notes:
        fail(f"source-confirmed Energy drink effect fact missing from catalog: {_expected_effect_fact}")

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
    "combat": 17,
    "health": 13,
    "stamina": 7,
    "survival": 35,
    "economy": 17,
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
                _default = _row.get("default") or {}
                _start = _default.get("value")
                if not (_default.get("mode") == "fixedValue"
                    and isinstance(_start, (int, float))
                    and _lo <= _start <= _hi):
                    _global_out_of_range.append((_row.get("id"), _name, _value, _lo, _hi))
# Sleep's UI default is intentionally different from the immutable game
# source: this must not silently reclassify vanilla 300 HP as 100 HP.
_sleep_hp = next(p for p in parameters if p["id"] == "survival.sleepHealthRestore")
_sleep_default = _sleep_hp.get("default") or {}
_sleep_range = _sleep_hp.get("slider") or {}
_sleep_original = [
    e for e in baseline["editPoints"]
    if e.get("parameterId") == "survival.sleepHealthRestore"
]
if (next(iter(_sleep_hp["vanilla"].values())) != 300
    or _sleep_default != {"mode": "fixedValue", "value": 100}
    or any(_sleep_range.get(k) != v
           for k, v in (("min", 0), ("max", 200), ("step", 5), ("stepMode", "absolute")))
    or sorted(e.get("sourceRaw") for e in _sleep_original) != ["300", "300"]
    or set(e.get("key") for e in _sleep_original) != {"ValueMin", "ValueMax"}):
    fail("Sleep HP 100 editor default / 0..200 UI range / 300 vanilla CFG parity drifted")
else:
    note("Sleep: actual 300 HP source preserved; tool default 100 HP, editor 0..200 HP")

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

if len(parameters) != 247 or len(_global_visible) != 149 or len(_global_hidden) != 98:
    fail(f"global UI accounting drifted: catalog={len(parameters)} visible={len(_global_visible)} hidden={len(_global_hidden)}; expected 247/149/98")
elif _global_tab_counts != _global_expected_tabs:
    fail(f"global visible tab counts drifted: {_global_tab_counts}; expected {_global_expected_tabs}")
elif _global_confirmed != 118 or _global_runtime != 31:
    fail(f"global visible status split drifted: confirmed={_global_confirmed} runtime={_global_runtime}; expected 118/31")
elif _global_control_counts != {"slider": 135, "toggle": 12, "choiceSlider": 1, "range": 1}:
    fail(f"global visible control-type split drifted: {_global_control_counts}; expected 135 sliders / 12 toggles / 1 choiceSlider / 1 range")
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
    note("current cross-domain structural audit: 247 catalog / 149 visible / 98 hidden; 118 visible confirmed + 31 runtime-test-required; zero direct collisions")

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


# Your Weapons knob grammar: prevent a cosmetic sketch from replacing actual
# WPF input, source-backed limits, or the current design-catalog contract.
_knob_source = (ROOT / "src" / "TrueCustomDifficulty" / "Controls" / "AggregateVisualControl.cs").read_text(encoding="utf-8")
_knob_catalog = {p["id"]: p for p in parameters_doc["parameters"]}
_knob_expected = {
    "combat.playerWeaponDamage": (0.25, 2.0),
    "combat.playerHeadshotMultiplier": (0.5, 3.0),
    "combat.weaponJamming": (0.0, 2.0),
}
_knob_required_markers = (
    'case "PlayerWeaponProfile":',
    'DrawYourWeaponsKnobs(dc, text, muted, border, accent, panel);',
    '"PlayerWeaponProfile" => 242.0',
    'private const double WeaponKnobSweepAngle = 270.0;',
    'groupWidth = Math.Min(620.0, ActualWidth)',
    'HitKind.Knob',
    'if (target.Kind == HitKind.Knob)',
    'protected override void OnMouseWheel(MouseWheelEventArgs e)',
    'KnobPoint(center, radius + 6, baselineAngle)',
    'target.Parameter.CurrentNumber = Snap(target.Parameter, value)',
)
_knob_section_start = _knob_source.find("private void DrawYourWeaponsKnobs(")
_knob_section_end = _knob_source.find("private static Point KnobPoint(", _knob_section_start)
_knob_section = (_knob_source[_knob_section_start:_knob_section_end]
                 if _knob_section_start >= 0 and _knob_section_end > _knob_section_start else "")
_knob_missing = [item for item in _knob_required_markers if item not in _knob_source]
_knob_values = [
    f"{key} range/step drift"
    for key, (min_value, max_value) in _knob_expected.items()
    if key not in _knob_catalog
    or (_knob_catalog[key].get("slider") or {}).get("min") != min_value
    or (_knob_catalog[key].get("slider") or {}).get("max") != max_value
    or (_knob_catalog[key].get("slider") or {}).get("step") != 0.25
]
_knob_catalog_texts = [
    (ROOT / "docs" / doc).read_text(encoding="utf-8")
    for doc in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md")
]
if _knob_missing or _knob_values or not _knob_section or any(
    _knob_section.count(f'("{key}",') != 1 for key in _knob_expected
) or "DrawHorizontalTrackCard(dc, P(channel.Item1)" in _knob_source or any(
    "2026-10-11 — YOUR WEAPONS rotary knobs" not in doc
    for doc in _knob_catalog_texts
):
    fail(f"Your Weapons rotary contract drift: missing={_knob_missing}, values={_knob_values}, "
         f"active three-source section={bool(_knob_section)}")
else:
    note("Your Weapons WPF rotary control: three real 0.25x source knobs, 270° geometry, "
         "angular/scroll/keyboard interaction, bounded layout and matching design catalogs")

# One consistent center-readout contract applies to every shared 270-degree
# source knob: single-line readouts use a top-edge -8 DIP adjustment, and
# two-line rate / medicine readouts use -15 / +5 DIP. No per-family override.
_rotary_body = _knob_source.split('private void DrawSourceKnob(', 1)[-1].split(
    'private static int KnobVisibleStepStride(', 1)[0]
_rotary_expected = (
    'string? nativeUnit = null, bool prominent = false,',
    'var currentTop = center.Y - (rateReadout || nativeUnit is not null ? 15.0 : 8.0);',
    'else if (nativeUnit is not null)',
)
if (not all(marker in _rotary_body for marker in _rotary_expected)
    or _rotary_body.count('new Point(center.X, center.Y + 5.0),') != 2
    or 'readoutYOffset' in _knob_source
    or 'drinkStrengthReadoutYOffset' in _knob_source):
    fail('shared rotary knob value readouts no longer have uniform vertical centering')
else:
    note('shared rotary knobs: single-line and two-line readouts vertically centered globally')
for _ui_catalog in ('UI-DESIGN-LOCK.md', 'VISUAL-CONTROL-SYSTEM.md'):
    if '2026-10-12 — All rotary knob readouts vertically centered' not in (
        DOCS / _ui_catalog).read_text(encoding='utf-8'):
        fail(f'global knob readout placement missing from {_ui_catalog}')

# Global and Individual Anomaly Damage now share one AnomalyDamageRadar
# card. Do not accept the legacy stand-alone global renderer as sufficient:
# the combined editor must be writable and the duplicate anchor invisible.
_anomaly_knob_param = _knob_catalog.get('environment.anomalyDamage', {})
_anomaly_knob_slider = _anomaly_knob_param.get('slider') or {}
_anomaly_radar_source = (ROOT / 'src/TrueCustomDifficulty/Controls/AnomalyDamageRadar.cs').read_text(encoding='utf-8')
if ('DrawCenteredText(dc, $"{p.CurrentNumber:0.##}x", new Point(center.X, center.Y),'
    not in _anomaly_radar_source
    or 'new Point(center.X, center.Y + 3)' in _anomaly_radar_source):
    fail('Anomaly Damage independent dial readout does not use vertical-center coordinates')
_anomaly_vm_source = (ROOT / 'src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs').read_text(encoding='utf-8')
_anomaly_xaml_source = (ROOT / 'src/TrueCustomDifficulty/MainWindow.xaml').read_text(encoding='utf-8')
_anomaly_combined_required = (
    'byId.TryGetValue("environment.anomalyDamage", out var global)',
    'DrawGlobalDamageKnob(dc, text, muted, border, accent, panel);',
    'private void DrawGlobalDamageKnob(',
    'private void UpdateGlobalFromPoint(',
    'private void SetGlobalDamage(',
    'private static int NativeTickStride(double pixelsPerStep)',
    'var step = Math.Max(Epsilon, p.TickFrequency);',
    'var stride = NativeTickStride(spacing);',
    'var snapped = p.Minimum + Math.Round(',
    'protected override void OnMouseWheel(MouseWheelEventArgs e)',
    'global.CurrentNumber + direction * global.TickFrequency',
    '_globalKnobHit.Contains(point)',
    '_globalDamage?.ResetToReference();',
    'INDIVIDUAL DAMAGE  ·  BARS VS VANILLA',
    'DrawCenteredText(dc, "FINAL", new Point(center, 314)',
    'DrawCenteredText(dc, "BASE", new Point(center, 341)',
    'EffectiveDamageValue',
    'parameter.IsValueModifiedFromReference()',
    '_globalDamage?.IsValueModifiedFromReference() == true',
    'parameter.CurrentNumber',
    'ReferenceNumber',
    'const double top = 365.0, bottom = 526.0;',
)
_anomaly_combined_vm_required = (
    'Id == "environment.anomalyDamage"',
    '? "Individual Anomaly Damage"',
    '&& !Id.Equals("environment.anomalyDamage", StringComparison.Ordinal)',
)
if (_anomaly_knob_slider.get('min') != 0
    or _anomaly_knob_slider.get('max') != 2.5
    or _anomaly_knob_slider.get('step') != 0.01
    or not all(token in _anomaly_radar_source for token in _anomaly_combined_required)
    or not all(token in _anomaly_vm_source for token in _anomaly_combined_vm_required)
    or 'x:Name="AnomalyDamageRadarControl"' not in _anomaly_xaml_source
    or 'MinHeight="606"' not in _anomaly_xaml_source
    or any('2026-10-11 — Combined global and individual anomaly damage'
           not in (ROOT / 'docs' / doc).read_text(encoding='utf-8')
           for doc in ('UI-DESIGN-LOCK.md', 'VISUAL-CONTROL-SYSTEM.md'))):
    fail("Combined Anomaly Damage card drifted: Global 0–2.5x knob, 11 independent relative-to-vanilla sources, one visible anchor and explained effective/source readouts")
else:
    note("Combined Anomaly Damage: native 0–2.5x/0.01x global knob, 11 independent source bars, 100% vanilla-relative axis and effective/source readouts")

# Stash-tier ammo uses three independent native rotary controls; bonus rewards
# remain horizontal because they represent different chance/count mechanics.
_stash_knob_start = _knob_source.find('private void DrawCompactStashRewards(')
_stash_knob_end = _knob_source.find('DrawText(dc, "BONUS REWARDS"', _stash_knob_start)
_stash_knob_body = _knob_source[_stash_knob_start:_stash_knob_end]
_stash_knob_tokens = (
    '("loot.cheapStashAmmoQuantity", "BASIC")',
    '("loot.mediumStashAmmoQuantity", "STANDARD")',
    '("loot.expensiveStashAmmoQuantity", "RARE")',
    'DrawSourceKnob(dc, P(tier.Id), tier.Label,',
    'const double knobWidth = 194.0;',
    'const double knobHeight = 181.0;',
)
if (not all(token in _stash_knob_body for token in _stash_knob_tokens)
    or 'DrawNativeGauge(dc, P(tier.Id)' in _stash_knob_body
    or not knob_wheel_gate_contains(_knob_source, "StashRewardProfile")
    or any('2026-10-11 — Ammo by Stash Tier rotary' not in doc
           for doc in _knob_catalog_texts)):
    fail("Ammo by Stash Tier: three native rotary controls / interaction / catalogs drifted")
else:
    note("Ammo by Stash Tier: BASIC/STANDARD/RARE native source knobs; bonus reward rails unchanged")


# Trader Stock native rotary bank uses the existing input/grid infrastructure.
_stock_knob_source = _knob_source
if (not all(v in _stock_knob_source for v in (
    'private void DrawTraderStockKnobs(',
    '("economy.traderAmmoStock", "AMMO STOCK")',
    '("economy.medicStock", "MEDIC STOCK")',
    '"TraderStockBoard" => 242.0',
)) or not knob_wheel_gate_contains(_stock_knob_source, "TraderStockBoard") or any('2026-10-11 — Trader Stock native knobs' not in d
          for d in _knob_catalog_texts)):
    fail("Trader Stock dual source knob contract drifted")

# Two converted native rotary families must retain their original source mapping.
if not all(token in _knob_source for token in (
    '("combat.psySpawnCycleDelay", "SPAWN DELAY")',
    '("combat.psyPhantomCount", "PHANTOM COUNT")',
    '("combat.psyDurabilityDamage", "GEAR DAMAGE")',
    'DrawTogglePill(dc, bleed, "PHANTOM BLEEDING"',
    '("equipment.armorDurability", "ARMOR DURABILITY")',
    '("equipment.weaponWearPerShot", "WEAR PER SHOT")',
    '"PsyPhantomPressure" => 378.0',
    '"EquipmentWearBalance" => 242.0',
)) or not knob_wheel_gate_contains(_knob_source, "PsyPhantomPressure", "EquipmentWearBalance") or any('2026-10-11 — Psy Combat and Equipment Wear rotary banks' not in d
          for d in _knob_catalog_texts):
    fail("Psy Combat / Equipment Wear knob UI and interaction contract drifted")

# WPF dial graduation integrity, 2026-10-11. The old fixed "0..10"
# decoration did not match native 0.25x or 0.01x parameter steps. All eight
# knobs must share one source-derived native grid and real source endpoints.
_knob_tick_contract = (
    'var nativeStep = Math.Max(Epsilon, parameter.TickFrequency);',
    'var nativeIntervals = (int)Math.Floor(range / nativeStep + 0.000001);',
    'var pixelsPerStep = radius * WeaponKnobSweepAngle * Math.PI / 180.0',
    'var tickStride = KnobVisibleStepStride(pixelsPerStep);',
    'var value = parameter.Minimum + tickIndex * nativeStep;',
    'var ratio = Math.Clamp((value - parameter.Minimum) / range, 0.0, 1.0);',
    'DrawStepTick(parameter.Minimum + nativeIntervals * nativeStep, true);',
    'DrawStepTick(parameter.Maximum, true);',
    'const double minimumTickSpacing = 6.5;',
    'OnPropertyChanged(nameof(TickFrequency))',
)
_knob_tick_missing = [t for t in _knob_tick_contract[:-1] if t not in _knob_source]
_knob_dynamic_vm = (ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
_knob_tick_vm_ok = ('public double TickFrequency' in _knob_dynamic_vm
                    and 'or nameof(ParameterViewModel.TickFrequency)' in _knob_source)
if 'for (var tick = 0; tick <= 10; tick++)' in _knob_source or _knob_tick_missing or not _knob_tick_vm_ok:
    fail(f"rotary tick grid drifted: fixed 10-way scale returned, required source geometry missing={_knob_tick_missing}, dynamic VM={_knob_tick_vm_ok}")
else:
    note("all eight rotary graduations share the native min+N*step grid, source-aligned angle and change notifications")

# Independent numeric check of the 270-degree native grid, including the
# zoomed-out Global Anomaly Damage scale. A 47-DIP radius cannot resolve all
# 251 marks at 0.01x; visual marks represent every tenth *real* native step
# (0.10x) while the editing snap still moves by 0.01x.
_knob_native_cases = {
    "combat.playerWeaponDamage": (0.25, 2.0, 0.25, 1, 8),
    "combat.playerHeadshotMultiplier": (0.5, 3.0, 0.25, 1, 11),
    "combat.weaponJamming": (0.0, 2.0, 0.25, 1, 9),
    "environment.anomalyDamage": (0.0, 2.5, 0.01, 10, 26),
    "loot.ammoBoxWeight918": (0.0, 3.0, 0.25, 1, 13),
    "loot.ammoBoxWeight045": (0.0, 3.0, 0.25, 1, 13),
    "loot.ammoBoxWeight545": (0.0, 3.0, 0.25, 1, 13),
    "loot.ammoBoxWeight012": (0.0, 3.0, 0.25, 1, 13),
}
_knob_tick_failures = []
for _id, (_min, _max, _step, _stride, _expected_markers) in _knob_native_cases.items():
    _param = _knob_catalog.get(_id, {})
    _slider = _param.get('slider') or {}
    if any(abs(_slider.get(k, float('nan')) - target) > 1e-7
           for k, target in (('min', _min), ('max', _max), ('step', _step))):
        _knob_tick_failures.append(f"{_id}: catalog range/step changed")
        continue
    _radius = 47.0 if _id != "environment.anomalyDamage" else 47.0
    _native_intervals = round((_max - _min) / _step)
    _distance = _radius * 270.0 * __import__('math').pi / 180.0 / _native_intervals
    _minimum_gap = 6.5 / _distance
    _magnitude = 1
    while _magnitude < 100000 and _minimum_gap > 10 * _magnitude:
        _magnitude *= 10
    _visible_stride = next(k * _magnitude for k in (1, 2, 5, 10)
                           if _minimum_gap <= k * _magnitude)
    _ticks = list(range(0, _native_intervals + 1, _visible_stride))
    if _ticks[-1] != _native_intervals:
        _ticks.append(_native_intervals)
    _values = [_min + i * _step for i in _ticks]
    _angles = [135.0 + (v - _min) / (_max - _min) * 270.0 for v in _values]
    if (_visible_stride != _stride or len(_ticks) != _expected_markers
        or abs(_angles[0] - 135.0) > 1e-7
        or abs(_angles[-1] - 405.0) > 1e-7
        or any(abs((v - _min) / _step - round((v - _min) / _step)) > 1e-7
               for v in _values)):
        _knob_tick_failures.append(f"{_id}: marks/grid/stride mismatch")
if _knob_tick_failures:
    fail("eight rotary source grids misaligned: " + "; ".join(_knob_tick_failures))
else:
    note("eight source-native knob grids: weapon 8/11/9 marks; anomaly 26 marks at 0.10x; four ammo knobs 13 marks each")

# Seven 2026-10-11 real-source graphical editors: lock the current visible
# WPF geometry/interaction and the unchanged source graph, not historical UI.
_seven_src = (ROOT / 'src' / 'TrueCustomDifficulty' / 'Controls' / 'AggregateVisualControl.cs').read_text(encoding='utf-8')
_seven_vm = (ROOT / 'src' / 'TrueCustomDifficulty' / 'ViewModels' / 'ParameterViewModel.cs').read_text(encoding='utf-8')
_seven_catalog = {p['id']: p for p in parameters_doc['parameters']}
_seven_methods = {
    'RepairCurve': ('DrawRepairCurve', 324.0, ('economy.repairFriend', 'economy.repairNeutral', 'economy.repairDisaffection', 'economy.repairEnemy')),
    'TradingDashboard': ('DrawTradingDashboard', 273.0, ('economy.buyCondition', 'economy.sellCondition', 'economy.traderMinBuyCondition', 'economy.itemValueFloor')),
    'StashRewardProfile': ('DrawCompactStashRewards', None, ('loot.cheapStashAmmoQuantity', 'loot.mediumStashAmmoQuantity', 'loot.expensiveStashAmmoQuantity', 'loot.cheapStashGrenadeChance', 'loot.cheapStashGrenadeQuantity', 'loot.expensiveStashMoneyChance')),
    'SleepingDashboard': ('DrawSleepingDashboard', 370.0, ('survival.sleepEligibilityThreshold', 'survival.minimumSleepHours', 'survival.sleepHealthRestore', 'survival.sleepStaminaRestore')),
    'SevereFatiguePenaltyBoard': ('DrawSevereFatiguePenaltyBoard', 376.0, ('advanced.fatigueMechanics', 'survival.severeFatigueMaxStamina', 'survival.severeFatigueCarryWeight', 'survival.severeFatigueRecoil', 'survival.severeFatigueAimSway', 'survival.severeFatigueDispersion')),
    'RadiationBalance': ('DrawRadiationBalance', None, ('survival.radiationAccumulation', 'survival.radiationRecovery', 'survival.radiationDamage')),
    'StaminaCore': ('DrawStaminaMovementBoard', 323.0, ('stamina.maxSP', 'stamina.regeneration', 'stamina.vaultMaxHeight', 'stamina.maxCarryWeight')),
}
_seven_required_code = (
    'private void DrawNativeGauge(',
    'private void DrawMovementLimitRuler(',
    'private void DrawTradingMeter(',
    'AddVerticalTarget(parameter,',
    'AddHorizontalTarget(parameter,',
    'AddHorizontalTarget(minimumHours, cell, stepMap,',
    'const int hoursCount = 14;',
    'const double sharedTop = 3.0;',
    'Math.Min(780.0, ActualWidth - 34.0)',
    'Math.Min(606.0, ActualWidth - 34.0)',
    'if (Family == "SleepingDashboard"',
    'or "TradingDashboard" or "RepairCurve" or "SleepingDashboard"',
    'or "RadiationBalance" or "StaminaCore"',
)
_seven_expected_sliders = {
    'survival.minimumSleepHours': (1, 14, 1),
    'stamina.vaultMaxHeight': (70, 300, 1),
    'economy.repairFriend': (0, 2, 0.01),
    'economy.repairNeutral': (0, 2, 0.01),
    'economy.repairDisaffection': (0, 2, 0.01),
    'economy.repairEnemy': (0, 3, 0.01),
    'economy.buyCondition': (0, 2, 0.01),
    'economy.sellCondition': (0, 2, 0.01),
    'loot.cheapStashAmmoQuantity': (0.25, 2, 0.25),
    'loot.mediumStashAmmoQuantity': (0.25, 2, 0.25),
    'loot.expensiveStashAmmoQuantity': (0.25, 2, 0.25),
    'survival.radiationAccumulation': (0, 2.5, 0.01),
    'survival.radiationRecovery': (0, 0.1, 0.01),
    'survival.radiationDamage': (0, 6, 0.01),
}
_seven_errors = []
for _family, (_method, _height, _ids) in _seven_methods.items():
    if f'private void {_method}(' not in _seven_src:
        _seven_errors.append(f'{_family}: missing active renderer {_method}')
    if _height is not None and f'"{_family}" => {_height:.1f}' not in _seven_src:
        _seven_errors.append(f'{_family}: obsolete measured frame height')
    for _id in _ids:
        if _id not in _seven_catalog:
            _seven_errors.append(f'{_family}: source {_id} missing from catalog')
        if f'"{_id}"' not in _seven_src:
            _seven_errors.append(f'{_family}: renderer no longer references {_id}')
if ('"RadiationBalance" => RadiationBoardHeight' not in _seven_src
    or 'private const double RadiationProfileTop = 241.0;' not in _seven_src
    or 'private const double RadiationProfileHeight = 388.0;' not in _seven_src
    or 'private const double RadiationControlsNarrowHeight = 272.0;' not in _seven_src
    or 'private void DrawRadiationControls(' not in _seven_src
    or 'private const int RadiationHudSegments = 32;' not in _seven_src
    or 'private const int RadiationHudTickStep = 10;' not in _seven_src
    or 'const double inner = 27.0;' not in _seven_src
    or 'dc.DrawEllipse(symbol, null, center, 23.0, 23.0);' not in _seven_src
    or 'private static void DrawRadiationHudTrefoil(' not in _seven_src
    or 'private const double RadiationDialCenterY = 183.0;' not in _seven_src
    or 'private const double RadiationDialTrackRadius = 122.0;' not in _seven_src
    or 'private double _radLevel = 0.0;' not in _seven_src
    or 'RadiationRailHit(' in _seven_src
    or 'RadiationLevelRail(' in _seven_src
    or 'RadiationStepHit(' in _seven_src
    or 'RadiationStepButton(' in _seven_src):
    _seven_errors.append('RadiationBalance ring-only preview, enlarged HUD or zero default drifted')
for _id, _native in _seven_expected_sliders.items():
    _s = (_seven_catalog.get(_id, {}).get('slider') or {})
    if tuple(_s.get(k) for k in ('min', 'max', 'step')) != _native:
        _seven_errors.append(f'{_id}: native min/max/step drift')
for _marker in _seven_required_code:
    if _marker not in _seven_src:
        _seven_errors.append(f'missing seven-controller interaction/geometry marker {_marker}')
# Check each *active renderer* rather than assuming the old stash-tier
# Preserve source IDs and native interaction after approved rotary redesigns.
# Stash tiers AND RadiationBalance are now knobs, while RepairCurve retains
# its interactive four-point source graph; bonus rewards stay separate.
_seven_stash_start = _seven_src.find('private void DrawCompactStashRewards(')
_seven_stash_end = _seven_src.find('DrawText(dc, "BONUS REWARDS"', _seven_stash_start)
_seven_stash_body = (_seven_src[_seven_stash_start:_seven_stash_end]
                     if 0 <= _seven_stash_start < _seven_stash_end else '')
_seven_stash_native = (
    '("loot.cheapStashAmmoQuantity", "BASIC")',
    '("loot.mediumStashAmmoQuantity", "STANDARD")',
    '("loot.expensiveStashAmmoQuantity", "RARE")',
    'DrawSourceKnob(dc, P(tier.Id), tier.Label,',
    'Math.Min(606.0, ActualWidth - 34.0)',
)
_seven_bonus_start = _seven_src.find('DrawText(dc, "BONUS REWARDS"', _seven_stash_end)
_seven_stash_method_end = _seven_src.find('\n    private ', _seven_stash_end)
_seven_bonus_body = (_seven_src[_seven_bonus_start:_seven_stash_method_end]
                     if 0 <= _seven_bonus_start < _seven_stash_method_end else '')
_seven_bonus_native = (
    '("loot.cheapStashGrenadeChance", "RGD-5 CHANCE")',
    '("loot.cheapStashGrenadeQuantity", "RGD-5 AMOUNT")',
    '("loot.expensiveStashMoneyChance", "COUPON CHANCE")',
    'DrawHorizontalTrackCard(dc, P(rewards[i].Id), rewards[i].Label,',
)
_seven_radiation_start = _seven_src.find('private void DrawRadiationBalance(')
_seven_radiation_end = _seven_src.find('\n    private ', _seven_radiation_start)
_seven_radiation_body = (_seven_src[_seven_radiation_start:_seven_radiation_end]
                         if 0 <= _seven_radiation_start < _seven_radiation_end else '')
_seven_radiation_native = (
    '("survival.radiationAccumulation", "BUILD-UP")',
    '("survival.radiationRecovery", "RECOVERY")',
    '("survival.radiationDamage", "HP DAMAGE")',
    'DrawSourceKnob(dc, P(source.Id), source.Label,',
    'rateReadout: source.Id == "survival.radiationRecovery"',
    'prominent: hero);',
)
_seven_repair_start = _seven_src.find('private void DrawRepairCurve(')
_seven_repair_end = _seven_src.find('\n    private ', _seven_repair_start)
_seven_repair_body = (_seven_src[_seven_repair_start:_seven_repair_end]
                      if 0 <= _seven_repair_start < _seven_repair_end else '')
_seven_repair_native = (
    '("economy.repairFriend", "FRIENDLY")',
    '("economy.repairNeutral", "NEUTRAL")',
    '("economy.repairDisaffection", "DISLIKED")',
    '("economy.repairEnemy", "HOSTILE")',
    'AddVerticalTarget(parameter,',
)
if (not all(t in _seven_stash_body for t in _seven_stash_native)
    or 'DrawNativeGauge(dc, P(tier.Id)' in _seven_stash_body
    or not all(t in _seven_bonus_body for t in _seven_bonus_native)
    or not all(t in _seven_radiation_body for t in _seven_radiation_native)
    or 'DrawNativeGauge(dc, P(source.Id)' in _seven_radiation_body
    or not knob_wheel_gate_contains(_seven_src, 'RadiationBalance')
    or not all(t in _seven_repair_body for t in _seven_repair_native)
    or '"StashRewardProfile" => width >= 740.0 ? 382.0 : 465.0,' not in _seven_src
    or not knob_wheel_gate_contains(_seven_src, 'StashRewardProfile')):
    _seven_errors.append('stash-tier/radiation native knobs, bonus rails or repair graph lost source wiring')
if not all(t in _seven_vm for t in ('"RadiationBalance" => "Turn BUILD-UP',
                                  '"RepairCurve" => "Drag one reputation point',
                                  '"SleepingDashboard" => "Select 1–14 minimum sleep hours')):
    _seven_errors.append('new seven-control source help/provenance not updated')
if any('2026-10-11 — Seven gamer-centric native visual controllers' not in
       (ROOT / 'docs' / doc).read_text(encoding='utf-8')
       for doc in ('UI-DESIGN-LOCK.md', 'VISUAL-CONTROL-SYSTEM.md')):
    _seven_errors.append('latest seven-controller design catalogs missing')
if _seven_errors:
    fail('Seven source-native visual controls: ' + '; '.join(_seven_errors))
else:
    note('Seven source-backed visual editors: 7 renderers / 31 unchanged CFG members; stash-tier/radiation native knobs, separate bonus rewards, repair curve, stepped sleep and vault ruler; native frame heights match')


# Source-native compact Health/Stamina revision. Protect real parameter
# identity, renderer geometry, unit semantics and the original Usage editors.
_compact_health_start = _seven_src.find('private void DrawCoreHealthDashboard(')
_compact_health_end = _seven_src.find('\n    private ', _compact_health_start + 20)
_compact_health = (_seven_src[_compact_health_start:_compact_health_end]
                   if 0 <= _compact_health_start < _compact_health_end else '')
_compact_stamina_start = _seven_src.find('private void DrawStaminaMovementBoard(')
_compact_stamina_end = _seven_src.find('\n    // Fatigue stages', _compact_stamina_start)
_compact_stamina = (_seven_src[_compact_stamina_start:_compact_stamina_end]
                    if 0 <= _compact_stamina_start < _compact_stamina_end else '')
_compact_health_ids = (
    'health.maxHP', 'health.baseRegen', 'health.regenDelay',
    'health.regenerationScalar', 'health.lowHealthRegen'
)
_compact_stamina_ids = (
    'stamina.maxSP', 'stamina.regeneration',
    'stamina.vaultMaxHeight', 'stamina.maxCarryWeight'
)
if (not all(f'P("{pid}")' in _compact_health
            for pid in ('health.maxHP', 'health.baseRegen'))
    or not all(f'("{pid}",' in _compact_health
               for pid in ('health.regenDelay', 'health.regenerationScalar',
                           'health.lowHealthRegen'))
    or not all(pid in _seven_src for pid in _compact_health_ids)
    or 'DrawCoreResourceRuler(dc, hp, "MAX HEALTH", "HP",' not in _compact_health
    or 'DrawCoreResourceRuler(dc, passiveRecovery, "PASSIVE RECOVERY", "HP/s",' not in _compact_health
    or 'DrawHorizontalTrackCard(dc, P(sources[i].Id)' not in _compact_health
    or 'var columns = ActualWidth >= 700.0 ? 3 : 2;' not in _compact_health
    or '"CoreHealthDashboard" => width >= 700.0 ? 282.0 : 365.0' not in _seven_src
    or '"health.medkitHealing"' in _compact_health
    or '"health.ecoMedkitHealing"' in _compact_health):
    fail('Core Health source-specific HP, passive and responsive recovery controls drifted')
if (not all(f'P("{pid}")' in _compact_stamina for pid in _compact_stamina_ids)
    or 'DrawMovementLimitRuler(dc, P("stamina.vaultMaxHeight")' not in _compact_stamina
    or 'DrawMovementLimitRuler(dc, P("stamina.maxCarryWeight")' not in _compact_stamina
    or 'AddHorizontalTarget(parameter,' not in _seven_src
    or 'parameter.CurrentNumber' not in _seven_src
    or 'MOVEMENT LIMITS' not in _compact_stamina
    or '"StaminaCore" => 323.0' not in _seven_src):
    fail('Stamina Core/Movement Limits: native vault and calibrated kg limit drifted')
_action_usage_source = (ROOT / 'src/TrueCustomDifficulty/Controls/ActionCostsRadar.cs').read_text(encoding='utf-8')
_action_usage_xaml = (ROOT / 'src/TrueCustomDifficulty/MainWindow.xaml').read_text(encoding='utf-8')
if (not all(token in _action_usage_source for token in (
        'private const double UsageHeight = 187.0;',
        'new Rect(left, 48.0, half, 124.0)',
        'new Rect(left + half + gap, 48.0, half, 124.0)',
        'var top = card.Top + 25.0 + local * 31.0;',
        'MappingKind.RelativeToReference2X',
        'stamina.sprintCost', 'stamina.jumpCost', 'stamina.vaultCost',
        'stamina.meleeNormalCost', 'stamina.meleeStrongCost',
        'stamina.butstockCost',
    ))
    or 'MinHeight="187"' not in _action_usage_xaml):
    fail('Stamina Usage: six editable movement/melee channels or compact frame drifted')
for _doc in ('UI-DESIGN-LOCK.md', 'VISUAL-CONTROL-SYSTEM.md'):
    if '2026-10-11 — Radiation, Core Health, Stamina & Movement compact pass' not in (
            ROOT / 'docs' / _doc).read_text(encoding='utf-8'):
        fail(f'Compact Radiation/Health/Stamina binding catalog missing: {_doc}')

# Consumable Mixer: two absolute-percent refill knobs + two multiplier knobs
# above the single 8-lane duration graph, 3 medicine strength knobs below.
_consumer_ids = (
    "survival.waterInstantStaminaRestore",
    "survival.energyDrinkInstantStaminaRestore",
    "survival.waterStaminaEffectStrength",
    "survival.energyDrinkStaminaRecoveryStrength",
)
_consumer_catalog = {p["id"]: p for p in parameters_doc["parameters"]}
_consumer_source = _knob_source
_consumer_from = _consumer_source.find("private void DrawConsumableMixer(")
_duration_from = _consumer_source.find("var previewTop = ConsumableDurationTop;", _consumer_from)
_medicine_from = _consumer_source.find("var medicineKnobTop = ConsumableMedicineKnobTop(ActualWidth);", _duration_from)
_consumer_until = _consumer_source.find("// One true integer-hours selector", _medicine_from)
_consumer_drinks = (_consumer_source[_consumer_from:_duration_from]
                    if 0 <= _consumer_from < _duration_from else "")
_consumer_durations = (_consumer_source[_duration_from:_medicine_from]
                       if 0 <= _duration_from < _medicine_from else "")
_consumer_medicines = (_consumer_source[_medicine_from:_consumer_until]
                       if 0 <= _medicine_from < _consumer_until else "")
_consumer_errors = []
for _cid in _consumer_ids:
    _def = _consumer_catalog.get(_cid, {})
    _range = _def.get("slider") or {}
    _expected = (0, 1, 0.01) if "InstantStaminaRestore" in _cid else (0, 2, 0.05)
    if tuple(_range.get(k) for k in ("min", "max", "step")) != _expected:
        _consumer_errors.append(f"{_cid}: refill/strength native range/step drift")
    if "InstantStaminaRestore" in _cid and (
            _def.get("display", {}).get("primary") != "percent"
            or _range.get("stepMode") != "absolute"
            or len(_def.get("targets", [])) != 1
            or _def["targets"][0].get("operation") != "set"):
        _consumer_errors.append(f"{_cid}: absolute-percent writer/UI contract drift")
for _required in (
    'DrawSourceKnob(dc, burst, "WATER STAMINA GAIN",',
    'DrawSourceKnob(dc, energyBurst, "ENERGY DRINK STAMINA GAIN",',
    'DrawSourceKnob(dc, waterStamina, "WATER STAMINA BUFF",',
    'DrawSourceKnob(dc, energyStamina, "ENERGY DRINK REGEN",',
    'var knobBankWidth = Math.Min(720.0, ActualWidth - 34.0);',
):
    if _required not in _consumer_drinks:
        _consumer_errors.append(f"drink knob missing: {_required}")
for _required in (
    '"EFFECT DURATION  /  SECONDS"',
    'const double medicineMaximumSeconds = 900.0;',
    'var medicineLanes = new (ParameterViewModel Source, string Label)[]',
    '(vincaTime, "VINCA")',
    '(herculesTime, "HERCULES")',
    '(psiTime, "PSI BLOCK")',
    'DrawCompactTrack(dc, shortEffect, caption,',
):
    if _required not in _consumer_durations:
        _consumer_errors.append(f"duration lane missing: {_required}")
for _required in (
    'DrawSourceKnob(dc, vincaStrength, "VINCA BLEED REGEN",',
    'DrawSourceKnob(dc, herculesStrength, "HERCULES CARRY",',
    'DrawSourceKnob(dc, psiStrength, "PSI BLOCK PROTECTION",',
    'rawReadout: true',
    'nativeUnit: "BLEED PTS"',
    'nativeUnit: "KG"',
    'nativeUnit: "PSI PTS"',
):
    if _required not in _consumer_medicines:
        _consumer_errors.append(f"medicine knob missing below effects: {_required}")
if (_consumer_drinks.count("DrawSourceKnob(") != 4
    or _consumer_durations.count("DrawSourceKnob(") != 0
    or _consumer_medicines.count("DrawSourceKnob(") != 3):
    _consumer_errors.append("knob counts/positions have drifted")
if ('DrawRightAlignedText(dc, "MEDICINE"' in _consumer_source
    or "DrawConsumableEffectPreview(" in _consumer_source
    or "LIVE EFFECT PREVIEW" in _consumer_source
    or "ConsumableEffectPreviewTop" in _consumer_source
    or "ConsumableEffectPreviewHeight" in _consumer_source):
    _consumer_errors.append("removed stamina preview reintroduced")
if (not knob_wheel_gate_contains(_consumer_source, "ConsumableMixer")
    or 'or "RadiationBalance" or "StaminaCore" or "ConsumableMixer"' not in _consumer_source
    or 'private const double ConsumableDurationTop = 250.0;' not in _consumer_source
    or '=> ConsumableDurationTop + ConsumableTimelineHeight(width) + 14.0;' not in _consumer_source
    or '=> ConsumableMedicineKnobTop(width) + 181.0 + 14.0;' not in _consumer_source):
    _consumer_errors.append("interaction or compact height contract missing")
if any("2026-10-11 — Consumable Effects: compact knobs below duration graph"
       not in (ROOT / 'docs' / doc).read_text(encoding='utf-8')
       for doc in ('UI-DESIGN-LOCK.md', 'VISUAL-CONTROL-SYSTEM.md')):
    _consumer_errors.append("binding UI design catalog not updated")
if _consumer_errors:
    fail("Consumable Effects no-preview layout: " + "; ".join(_consumer_errors))
else:
    note("Consumable Effects: drink knobs, 8 duration lanes, medicine knobs below, no passive preview")

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
