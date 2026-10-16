#!/usr/bin/env python3
"""Audit the PHASE-2 physical stash consumer -> generator -> native branch graph.

Source-only, no game RNG/drop claims. Rebuild checked-in CSV deterministically:
    python tools/audit_stash_consumer_graph.py --write
Check that the CSV still matches the embedded writer source manifests:
    python tools/audit_stash_consumer_graph.py
Inspect evidence and exact native branches for one actor:
    python tools/audit_stash_consumer_graph.py --actor <32-character ActorSID>
    python tools/audit_stash_consumer_graph.py --actor <ActorSID> --expand-branches
"""
import argparse
import csv
import io
from collections import Counter, defaultdict
from pathlib import Path
from audit_stash_generator_branches import (
    inventory as native_branches,
    render as render_native_branches,
    OUT as NATIVE_BRANCH_CSV,
)

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
OUT = ROOT / "docs/generators/stash-consumer-generator-edges.csv"

FIELDS = [
    "source_scope", "physical_actor_sid", "source_layer", "edge_kind",
    "patch_file", "edit_prototype", "edit_path", "source_reference_sid",
    "canonical_generator_sid", "planned_clone_sid", "rank_gate",
    "source_group_layer", "branch_count", "guard_status", "guard_reason",
    "applicability", "consumer_provenance",
]
TIER_NAMES = (
    ("Basic", "cheap-stash-production-redirect-manifest.csv",
     "GamePass_Stash_ItemGenerator_Cheap", "Stash_Cheap_"),
    ("Standard", "medium-stash-production-redirect-manifest.csv",
     "GamePass_Stash_ItemGenerator_Common_Var1", "Stash_Medium_"),
    ("Rare", "expensive-stash-production-redirect-manifest.csv",
     "GamePass_Stash_ItemGenerator_Rare", "Stash_Expensive_"),
)
BASE_ACTORS = "smartloot-regular-world-actor-manifest.csv"
DLC_ACTORS = "dlc1-stash-eligible-actor-manifest.csv"
PROTECTED = "base-stash-protected-actor-manifest.csv"

def source_rows(filename):
    with (DATA / filename).open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))

def actor_tokens(field):
    # Ordered source refs, matching the writer's Split/TrimEntries semantics.
    return [token.strip() for token in field.split(",") if token.strip()]

def build_graph():
    branches = native_branches()  # source reconstruction, then checked CSV
    assert NATIVE_BRANCH_CSV.exists()
    assert NATIVE_BRANCH_CSV.read_text(encoding="utf-8") == (
        render_native_branches(branches)
    ), "Phase-1 native branch CSV differs from original CFG sources"
    groups = defaultdict(list)
    for branch in branches:
        groups[(branch["layer"], branch["source_sid"])].append(branch)
    assert len(branches) == 463 and len(groups) == 16

    protected = {r["ActorSID"]: r["ProtectionReason"]
                 for r in source_rows(PROTECTED)}
    base = source_rows(BASE_ACTORS)
    dlc = source_rows(DLC_ACTORS)
    approved_dlc = {r["ActorSID"] for r in dlc}
    assert len(protected) == 106
    assert len(base) == 831 and len(dlc) == len(approved_dlc) == 112
    assert sum(r["ClueVariableSID"] == "EmptyInherited"
               for r in base) == 803
    assert all(r["ClueVariableSID"] == "EmptyInherited" for r in dlc)
    original_actor_roster = {
        **{("base", r["ActorSID"]): r for r in base},
        **{("dlc1", r["ActorSID"]): r for r in dlc},
    }
    graph = []

    def add(**kwargs):
        assert set(kwargs) == set(FIELDS), set(kwargs) ^ set(FIELDS)
        assert len(kwargs["physical_actor_sid"]) == 32
        layer = kwargs["source_group_layer"]
        sid = kwargs["canonical_generator_sid"]
        expected_count = len(groups[(layer, sid)]) if layer else 0
        assert int(kwargs["branch_count"]) == expected_count, kwargs
        if kwargs["guard_status"] == "ELIGIBLE_CONDITIONAL":
            assert expected_count > 0 and layer, kwargs
        graph.append({field: str(kwargs[field]) for field in FIELDS})

    canonical_tier = {}
    for tier, filename, original, prefix in TIER_NAMES:
        canonical_tier[original] = original
        for rank in ("Newbie", "Experienced", "Veteran", "Master"):
            canonical_tier[prefix + rank] = original

        manifest = source_rows(filename)
        expected = {"Basic": 1271, "Standard": 450, "Rare": 305}[tier]
        assert len(manifest) == expected
        for row in manifest:
            sid = row["physical_container_sid"]
            path = row["file"]
            scope = "dlc1" if path == "DLCGameData/DLC1/SpawnActorPrototypes.cfg" else "base"
            assert path in (
                "GameData/SpawnActorPrototypes.cfg",
                "GameData/ItemGeneratorPrototypes.cfg",
                "DLCGameData/DLC1/SpawnActorPrototypes.cfg",
            )
            assert row["source_value"] in canonical_tier
            assert canonical_tier[row["source_value"]] == original
            assert row["target_value"] == "TCD_" + row["source_value"]
            assert row["key"] in ("PrototypeSID", "ItemGeneratorPrototypeSID")
            assert row["redirect_kind"] in (
                "DIRECT_CONTAINER_SETTING", "RANK_CONTAINER_SETTING",
                "LOCATION_WRAPPER_SUBGENERATOR")
            if sid in protected:
                status, reason = "BLOCKED_PROTECTED_ACTOR", protected[sid]
            elif scope == "dlc1" and row["prototype"] not in approved_dlc:
                status, reason = "BLOCKED_DLC_NOT_ALLOWLISTED", "not_in_112_dlc1_allowlist"
            else:
                status, reason = "ELIGIBLE_CONDITIONAL", "approved_writer_redirect"
            add(source_scope=scope,
                physical_actor_sid=sid,
                source_layer="TierRedirect",
                edge_kind=row["redirect_kind"],
                patch_file=path,
                edit_prototype=row["prototype"],
                edit_path=row["node_path"] + "." + row["key"],
                source_reference_sid=row["source_value"],
                canonical_generator_sid=original,
                planned_clone_sid=row["target_value"],
                rank_gate=row["player_rank"],
                source_group_layer="ItemGenerator",
                branch_count=len(groups[("ItemGenerator", original)]),
                guard_status=status,
                guard_reason=reason,
                applicability=("requires_tier_writer_change_and_dlc1"
                               if scope == "dlc1" else "requires_tier_writer_change"),
                consumer_provenance=filename)

    for scope, filename, actors in (
        ("base", BASE_ACTORS, base), ("dlc1", DLC_ACTORS, dlc)
    ):
        patch_file = ("GameData/SpawnActorPrototypes.cfg"
                      if scope == "base"
                      else "DLCGameData/DLC1/SpawnActorPrototypes.cfg")
        for actor in actors:
            sid = actor["ActorSID"]
            stash_references = actor_tokens(actor["OriginalStashPrototypeSIDs"])
            assert stash_references and len(stash_references) == len(set(stash_references))
            for prototype in stash_references:
                assert ("SmartLoot", prototype) in groups, (
                    "unindexed SmartLoot actor reference", scope, sid, prototype
                )
                if scope == "base" and actor["ClueVariableSID"] != "EmptyInherited":
                    status, reason = "BLOCKED_NAMED_CLUE", actor["ClueVariableSID"]
                else:
                    status, reason = "ELIGIBLE_CONDITIONAL", "approved_no_clue_actor"
                add(source_scope=scope,
                    physical_actor_sid=sid,
                    source_layer="SmartLoot",
                    edge_kind="STASH_PROTOTYPE_SID",
                    patch_file=patch_file,
                    edit_prototype=sid,
                    edit_path="StashPrototypeSID",
                    source_reference_sid=prototype,
                    canonical_generator_sid=prototype,
                    planned_clone_sid="TCD_" + prototype,
                    rank_gate="",
                    source_group_layer="SmartLoot",
                    branch_count=len(groups[("SmartLoot", prototype)]),
                    guard_status=status,
                    guard_reason=reason,
                    applicability=("requires_affected_smartloot_prototype_and_dlc1"
                                   if scope == "dlc1"
                                   else "requires_affected_smartloot_prototype"),
                    consumer_provenance=filename)

    special_sid = "GamePass_Stash_ItemGenerator_CheapNoFresh"
    special_actors = [r for r in dlc
                      if r["OriginalItemGeneratorSIDs"] == special_sid]
    assert len(special_actors) == 6
    for actor in special_actors:
        add(source_scope="dlc1",
            physical_actor_sid=actor["ActorSID"],
            source_layer="SpecialDlcCheapNoFresh",
            edge_kind="DLC1_SEPARATE_ITEMGENERATOR",
            patch_file="DLCGameData/DLC1/SpawnActorPrototypes.cfg",
            edit_prototype=actor["ActorSID"],
            edit_path="ItemGeneratorSettings",
            source_reference_sid=special_sid,
            canonical_generator_sid=special_sid,
            planned_clone_sid="TCD_" + special_sid,
            rank_gate="",
            source_group_layer="",
            branch_count=0, # intentionally NOT in 463 phase-1 branches
            guard_status="ELIGIBLE_SEPARATE_OUT_OF_INDEX",
            guard_reason="six_approved_dlc1_cheapnofresh",
            applicability="requires_cheapnofresh_change_and_dlc1",
            consumer_provenance=DLC_ACTORS)

    # Preserve the other original ItemGenerator references independently from
    # the tier patch edges: these are observed bindings, NOT write approvals.
    # An unknown/unresolved local wrapper must NEVER be recursively guessed.
    for (scope, sid), actor in original_actor_roster.items():
        patch_file = ("GameData/SpawnActorPrototypes.cfg"
                      if scope == "base"
                      else "DLCGameData/DLC1/SpawnActorPrototypes.cfg")
        for original in actor_tokens(actor["OriginalItemGeneratorSIDs"]):
            canonical = canonical_tier.get(original, original)
            indexed = ("ItemGenerator", canonical) in groups
            add(source_scope=scope,
                physical_actor_sid=sid,
                source_layer="ObservedItemGenerator",
                edge_kind="SOURCE_REFERENCE_ONLY",
                patch_file=patch_file,
                edit_prototype=sid,
                edit_path="ItemGeneratorSettings (original source inventory)",
                source_reference_sid=original,
                canonical_generator_sid=canonical,
                planned_clone_sid="",
                rank_gate="",
                source_group_layer="ItemGenerator" if indexed else "",
                branch_count=(len(groups[("ItemGenerator", canonical)])
                              if indexed else 0),
                guard_status="OBSERVED_ONLY",
                guard_reason=("original_reference_not_an_approval"
                              if indexed else "unresolved_or_out_of_phase1"),
                applicability="no_patch_inferred_from_source_reference",
                consumer_provenance=(BASE_ACTORS if scope == "base" else DLC_ACTORS))

    stats = Counter((e["source_layer"], e["guard_status"]) for e in graph)
    protected_tier = sum(n for (layer, status), n in stats.items()
                         if layer == "TierRedirect"
                         and status == "BLOCKED_PROTECTED_ACTOR")
    assert protected_tier == 105, stats
    assert stats[("TierRedirect", "BLOCKED_DLC_NOT_ALLOWLISTED")] == 29, stats
    assert stats[("TierRedirect", "ELIGIBLE_CONDITIONAL")] == 1892, stats
    assert stats[("SmartLoot", "ELIGIBLE_CONDITIONAL")] == 1816, stats
    assert stats[("SmartLoot", "BLOCKED_NAMED_CLUE")] == 56, stats
    assert stats[("SmartLoot", "BLOCKED_NAMED_CLUE")] > 0
    assert stats[("SpecialDlcCheapNoFresh", "ELIGIBLE_SEPARATE_OUT_OF_INDEX")] == 6
    assert sum(n for (kind, _), n in stats.items()
               if kind == "TierRedirect") == 2026
    assert sum(n for (kind, _), n in stats.items()
               if kind == "ObservedItemGenerator") == 1103
    assert len(graph) == 5007, len(graph)

    # Safety drift gate: generated source inventory cannot silently stay green
    # if the real writer stops invoking whole-actor protection or SmartLoot
    # no-clue / DLC allowlists. This is a static contract, NOT a runtime test.
    services = ROOT / "src/TrueCustomDifficulty/Services"
    tier_writer = (services / "CfgPatchWriterService.cs").read_text(
        encoding="utf-8")
    smart_writer = (services / "CfgPatchWriterService.StashSmartLoot.cs").read_text(
        encoding="utf-8")
    dlc_writer = (services / "CfgPatchWriterService.StashDlc1.cs").read_text(
        encoding="utf-8")
    assert tier_writer.count(
        "IsApprovedStashTierRedirect(redirect.File, redirect.Prototype, "
        "redirect.PhysicalContainerSid)") == 3
    for contract in (
        "BaseProtectedStashActors.Value.Contains(physicalContainerSid)",
        'file.Equals(DlcSmartLootActorFile',
        'x.ClueSid == "EmptyInherited"',
        'if (hasDlc1)',
        'rewritten.TryGetValue(x, out var replacement)',
        'if (affected.Length == 0)',
    ):
        assert contract in smart_writer, (
            "SmartLoot / tier protection writer contract drift", contract)
    assert 'actors.Length != 6' in dlc_writer
    assert 'CheapNoFreshActorIds.Contains(x.ActorSid)' in dlc_writer
    return graph, groups, stats

def export(graph):
    stream = io.StringIO(newline="")
    writer = csv.DictWriter(stream, fieldnames=FIELDS, lineterminator="\n",
                            quoting=csv.QUOTE_MINIMAL)
    writer.writeheader()
    writer.writerows(graph)
    return stream.getvalue()

def show_actor(graph, groups, sid, expand):
    edges = [edge for edge in graph if edge["physical_actor_sid"] == sid]
    if not edges:
        raise SystemExit(f"No source/redirect edge for actor SID: {sid}")
    print(f"ACTOR {sid} · source-only graph; no output or RNG claim")
    for edge in edges:
        print(" | ".join((
            edge["source_scope"], edge["source_layer"], edge["edge_kind"],
            edge["guard_status"], edge["source_reference_sid"],
            f"native_branches={edge['branch_count']}",
            f"file={edge['patch_file']}", f"path={edge['edit_path']}",
        )))
        if expand and edge["source_layer"] != "ObservedItemGenerator":
            for branch in groups[(edge["source_group_layer"],
                                  edge["canonical_generator_sid"])]:
                print("    " + " | ".join((
                    branch["group_path"], branch["selection_field"],
                    "rank=" + branch["rank_gate"],
                    "difficulty=" + branch["difficulty_gate"],
                    "items=" + branch["item_count"],
                )))

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write", action="store_true",
                        help="Regenerate consumer source graph after auditing.")
    parser.add_argument("--actor", metavar="ActorSID",
                        help="Inspect one exact physical stash actor.")
    parser.add_argument("--expand-branches", action="store_true",
                        help="With --actor, expand all exact native group paths.")
    args = parser.parse_args()
    if args.expand_branches and not args.actor:
        parser.error("--expand-branches requires --actor")
    graph, groups, stats = build_graph()
    generated = export(graph)
    if args.write:
        OUT.write_text(generated, encoding="utf-8", newline="")
    else:
        assert OUT.exists() and OUT.read_text(encoding="utf-8") == generated, (
            f"Stash consumer graph drift: {OUT}. Inspect source inputs and "
            "writer guard changes before intentional --write regeneration."
        )
    print("PASS: 2,026 tier redirect edges, 105 protected, 29 DLC denied")
    print("PASS: 831 base / 112 DLC SmartLoot actor source inventories")
    print("PASS: 6 separately sourced DLC1 CheapNoFresh actor routes")
    print("PASS: 1,103 original ItemGenerator observation-only references")
    print("PASS: 5,007 source edges joined to 463 exact native branch IDs")
    print("PENDING: per-profile dynamic CFG writer/output parity and in-game RNG")
    if args.actor:
        show_actor(graph, groups, args.actor, args.expand_branches)

if __name__ == "__main__":
    main()
