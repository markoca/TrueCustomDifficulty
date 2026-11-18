using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// MUTANT AGGRESSION (docs/combat/MUTANT-AGGRESSION.md):
//  * One knob per attack / ability → DifficultyPrototypes [difficulty]
//    .MutantCombatDifficulty.AgentCooldownMultipliers (full list, non-bpatch).
//  * One knob per move → the same list: the move's tag gets group × 1/knob.
//  * SPECIAL ABILITY RATE → CombatSynchronizationPrototypes
//    CombatSynchronizer{difficulty}.{rank}.FilterGroups[3|4] cooldowns
//    (full rank tables, because Novice–Veteran inherit Default).
public sealed partial class CfgPatchWriterService
{
    private const string CombatSyncResource = "TrueCustomDifficulty.Data.combat-synchronization-2.1.json";

    private static double PositiveRate(IReadOnlyDictionary<string, ParameterViewModel> allById, string id)
    {
        if (!allById.TryGetValue(id, out var parameter))
            throw new InvalidDataException("Missing mutant rate source: " + id);
        var rate = parameter.CurrentNumber;
        if (!double.IsFinite(rate) || rate <= 0.0)
            throw new InvalidDataException("Invalid mutant rate: " + id);
        return rate;
    }

    private static string FormatCooldownMultiplier(double value)
        => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static int WriteMutantAggression(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        string difficultySid,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var edits = 0;
        var groups = new[] { MutantAggressionPolicy.Group.Attacks, MutantAggressionPolicy.Group.Abilities };

        // ---- ATTACKS / ABILITIES / move knobs: AgentCooldownMultipliers -----
        var changed = MutantAggressionPolicy.MoveParameterIds
            .Where(id => modifiedById.TryGetValue(id, out var p) && p.IsValueModifiedFromReference())
            .ToArray();
        if (changed.Length > 0)
        {
            var document = GetDocument(documents, "GameData/DifficultyPrototypes.cfg");
            var mutant = document.GetOrAddNode(difficultySid, new[] { "MutantCombatDifficulty" });
            // The whole list is replaced. Every attack and ability is a knob k:
            // at its difficulty vanilla the game's own value (Master 0.5 on the
            // listed tags), else 1/k. Tags that end at exactly 1 are not written.
            mutant.RemoveAssignment("AgentCooldownMultipliers");
            mutant.RemoveChild("AgentCooldownMultipliers");
            var list = mutant.GetOrAddChild("AgentCooldownMultipliers", useBpatch: false);
            list.Clear();
            var index = 0;
            // Pause-gated knockdown moves may not beat the shared special-move pause.
            var specialRate = PositiveRate(allById, MutantAggressionPolicy.SpecialAbilityRateId);
            foreach (var family in MutantAggressionPolicy.Families)
            {
                var knobs = new Dictionary<string, (double Rate, double Reference)>(StringComparer.Ordinal);
                foreach (var group in groups)
                {
                    foreach (var move in MutantAggressionPolicy.Moves(family, group))
                    {
                        var id = MutantAggressionPolicy.MoveId(family, move);
                        var reference = MutantAggressionPolicy.KnobReference(family, move, difficultySid);
                        var rate = Math.Min(PositiveRate(allById, id),
                            MutantAggressionPolicy.KnobCap(family, move, specialRate, double.MaxValue, reference));
                        knobs[move.Tag] = (rate, reference);
                    }
                }

                foreach (var agent in family.Agents)
                {
                    // The game's own Master order first, then the knob tags.
                    var tags = agent.VanillaMasterTags.Concat(knobs.Keys).Distinct(StringComparer.Ordinal);
                    var entries = new List<(string Tag, double Multiplier)>();
                    foreach (var tag in tags)
                    {
                        // A knob is a one-tag group with its own Master vanilla. Tags
                        // without a knob (roars, growls, howls, dodges, teleports,
                        // shields) keep the game's own Master entry.
                        var multiplier = knobs.TryGetValue(tag, out var knob)
                            ? MutantAggressionPolicy.Multiplier(agent, tag, knob.Rate, knob.Reference)
                            : difficultySid == "Stalker" && agent.VanillaMasterTags.Contains(tag)
                                ? MutantAggressionPolicy.VanillaMasterCooldownMultiplier : 1.0;
                        if (Math.Abs(multiplier - 1.0) >= 0.0000001)
                            entries.Add((tag, multiplier));
                    }
                    if (entries.Count == 0)
                        continue;

                    var entry = list.GetOrAddChild($"[{index++}]", useBpatch: false);
                    entry.SetAssignment("AgentPrototypeSID", agent.Sid);
                    var cooldowns = entry.GetOrAddChild("CooldownMultipliers", useBpatch: false);
                    for (var t = 0; t < entries.Count; t++)
                    {
                        var tagNode = cooldowns.GetOrAddChild($"[{t}]", useBpatch: false);
                        tagNode.SetAssignment("CooldownTag", entries[t].Tag);
                        tagNode.SetAssignment("CooldownMultiplier", FormatCooldownMultiplier(entries[t].Multiplier));
                        edits++;
                    }
                }
            }
            foreach (var id in changed)
                writtenParameters.Add(id);
            edits++;
        }

        // ---- Group token cooldowns: SPECIAL ABILITY RATE + HUMAN TACTICS -----
        edits += WriteCombatSynchronization(modifiedById, allById, difficultySid, documents, writtenParameters);

        return edits;
    }

    /// <summary>
    /// One writer for every CombatSynchronizer control (they share the rank
    /// tables): when any is changed, the four full rank tables of the selected
    /// difficulty are written (non-bpatch, since Easy/Medium/Hard only inherit
    /// Default) with each control's groups scaled by reference / rate.
    /// </summary>
    private static int WriteCombatSynchronization(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        string difficultySid,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var controls = new List<(string Id, IReadOnlyList<string> Indices, double Reference)>
        {
            (MutantAggressionPolicy.SpecialAbilityRateId,
                MutantAggressionPolicy.AbilityTokenGroups.Select(g => g.Index).ToArray(),
                difficultySid == "Stalker" ? 2.0 : 1.0)
        };
        controls.AddRange(HumanCombatPolicy.Tactics.Select(c => (c.Id,
            (IReadOnlyList<string>)c.Groups.Select(g => g.Index).ToArray(),
            HumanCombatPolicy.Reference(c, difficultySid))));

        var changed = controls.Where(c => modifiedById.TryGetValue(c.Id, out var p)
            && p.IsValueModifiedFromReference()).Select(c => c.Id).ToArray();
        if (changed.Length == 0)
            return 0;

        // Factor per FilterGroups index: source x reference / rate.
        var factors = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (id, indices, reference) in controls)
        {
            if (!allById.TryGetValue(id, out var parameter))
                throw new InvalidDataException("Missing combat synchronization source: " + id);
            var rate = parameter.CurrentNumber;
            if (!double.IsFinite(rate) || rate <= 0.0)
                throw new InvalidDataException("Invalid combat synchronization rate: " + id);
            foreach (var index in indices)
                factors[index] = reference / rate;
        }

        var tables = LoadCombatSynchronizationTables();
        var sourceSid = difficultySid == "Stalker" ? "CombatSynchronizerStalker" : "Default";
        var document = GetDocument(documents, "GameData/CombatSynchronizationPrototypes.cfg");
        var root = document.GetOrAddNode(MutantAggressionPolicy.SynchronizerSid(difficultySid),
            Array.Empty<string>());
        var edits = 0;
        foreach (var rank in MutantAggressionPolicy.Ranks)
        {
            var table = tables.GetProperty(sourceSid).GetProperty(rank);
            root.RemoveChild(rank);
            var rankNode = root.GetOrAddChild(rank, useBpatch: false);
            edits += WriteCombatSyncTree(rankNode, table, factors, new List<string> { rank });
        }
        foreach (var id in changed)
            writtenParameters.Add(id);
        return edits;
    }

    private static JsonElement LoadCombatSynchronizationTables()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CombatSyncResource)
            ?? throw new InvalidDataException("Missing combat synchronization tables resource.");
        using var json = JsonDocument.Parse(stream);
        var tables = json.RootElement.GetProperty("tables").Clone();
        // Guard every scaled filter group in every table and rank.
        var guarded = MutantAggressionPolicy.AbilityTokenGroups.Select(g => (g.Index, g.Tag))
            .Concat(HumanCombatPolicy.Tactics.SelectMany(c => c.Groups))
            .ToArray();
        foreach (var sid in new[] { "Default", "CombatSynchronizerStalker" })
        foreach (var rank in MutantAggressionPolicy.Ranks)
        {
            var groups = tables.GetProperty(sid).GetProperty(rank)
                .GetProperty("children").GetProperty("FilterGroups").GetProperty("children");
            foreach (var (index, tag) in guarded)
            {
                var allowed = groups.GetProperty(index).GetProperty("children")
                    .GetProperty("AllowedTags").GetProperty("fields");
                if (allowed.GetProperty("[0]").GetString() != tag)
                    throw new InvalidDataException($"Combat synchronization {sid}.{rank}.{index} tag drifted.");
            }
        }
        return tables;
    }

    // Full non-bpatch copy of one rank table; the cooldowns of the controlled
    // filter groups are multiplied by their factor (1 = source value).
    private static int WriteCombatSyncTree(PatchNode node, JsonElement source,
        IReadOnlyDictionary<string, double> factors, List<string> path)
    {
        var edits = 0;
        var factor = path.Count == 3 && path[1] == "FilterGroups"
            && factors.TryGetValue(path[2], out var f) ? f : 1.0;
        foreach (var field in source.GetProperty("fields").EnumerateObject())
        {
            var value = field.Value.GetString() ?? string.Empty;
            if (Math.Abs(factor - 1.0) > 0.0000001 && field.Name is "CooldownMin" or "CooldownMax")
                value = (ParseNumber(value) * factor).ToString("0.##", CultureInfo.InvariantCulture);
            node.SetAssignment(field.Name, value);
            edits++;
        }
        foreach (var child in source.GetProperty("children").EnumerateObject())
        {
            var childNode = node.GetOrAddChild(child.Name, useBpatch: false);
            path.Add(child.Name);
            edits += WriteCombatSyncTree(childNode, child.Value, factors, path);
            path.RemoveAt(path.Count - 1);
        }
        return edits;
    }
}
