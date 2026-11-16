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
//  * ATTACKS / ABILITIES per family → DifficultyPrototypes [difficulty]
//    .MutantCombatDifficulty.AgentCooldownMultipliers (full list, non-bpatch).
//  * SPECIAL ABILITY RATE → CombatSynchronizationPrototypes
//    CombatSynchronizer{difficulty}.{rank}.FilterGroups[3|4] cooldowns
//    (full rank tables, because Novice–Veteran inherit Default).
public sealed partial class CfgPatchWriterService
{
    private const string CombatSyncResource = "TrueCustomDifficulty.Data.combat-synchronization-2.1.json";

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

        // ---- ATTACKS / ABILITIES: AgentCooldownMultipliers -------------------
        var changed = MutantAggressionPolicy.RateParameterIds
            .Where(id => modifiedById.TryGetValue(id, out var p) && p.IsValueModifiedFromReference())
            .ToArray();
        if (changed.Length > 0)
        {
            var document = GetDocument(documents, "GameData/DifficultyPrototypes.cfg");
            var mutant = document.GetOrAddNode(difficultySid, new[] { "MutantCombatDifficulty" });
            // The whole list is replaced. A group at its difficulty vanilla
            // keeps the game's own entries (Master: 0.5 on the listed tags, in
            // the game's order); a changed group lists every tag of every
            // agent of the family with 1/rate; 1x writes nothing.
            mutant.RemoveAssignment("AgentCooldownMultipliers");
            mutant.RemoveChild("AgentCooldownMultipliers");
            var list = mutant.GetOrAddChild("AgentCooldownMultipliers", useBpatch: false);
            list.Clear();
            var index = 0;
            foreach (var family in MutantAggressionPolicy.Families)
            {
                var rates = new Dictionary<MutantAggressionPolicy.Group, (double Rate, double Reference)>();
                foreach (var group in groups)
                {
                    if (family.Tags(group).Count == 0)
                        continue;
                    var id = MutantAggressionPolicy.ParameterId(family, group);
                    if (!allById.TryGetValue(id, out var parameter))
                        throw new InvalidDataException("Missing mutant rate source: " + id);
                    var rate = parameter.CurrentNumber;
                    if (!double.IsFinite(rate) || rate <= 0.0)
                        throw new InvalidDataException("Invalid mutant rate: " + id);
                    rates[group] = (rate, MutantAggressionPolicy.Reference(family, group, difficultySid));
                }

                foreach (var agent in family.Agents)
                {
                    var entries = new List<(string Tag, double Multiplier)>();
                    // Groups at vanilla: the game's own Master entries, in order.
                    foreach (var tag in agent.VanillaMasterTags)
                    {
                        var group = family.AttackTags.Contains(tag)
                            ? MutantAggressionPolicy.Group.Attacks : MutantAggressionPolicy.Group.Abilities;
                        if (rates.TryGetValue(group, out var r) && Math.Abs(r.Rate - r.Reference) < 0.0000001
                            && r.Reference > 1.0)
                            entries.Add((tag, MutantAggressionPolicy.VanillaMasterCooldownMultiplier));
                    }
                    // Changed groups: every tag of the group.
                    foreach (var (group, r) in rates)
                    {
                        if (Math.Abs(r.Rate - r.Reference) < 0.0000001 || Math.Abs(r.Rate - 1.0) < 0.0000001)
                            continue;
                        entries.AddRange(family.Tags(group).Select(tag => (tag, 1.0 / r.Rate)));
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

        // ---- SPECIAL ABILITY RATE: group token cooldowns ---------------------
        if (modifiedById.TryGetValue(MutantAggressionPolicy.SpecialAbilityRateId, out var special)
            && special.IsValueModifiedFromReference())
        {
            var rate = special.CurrentNumber;
            if (!double.IsFinite(rate) || rate <= 0.0)
                throw new InvalidDataException("Invalid special ability rate.");
            var tables = LoadCombatSynchronizationTables();
            var sourceSid = difficultySid == "Stalker" ? "CombatSynchronizerStalker" : "Default";
            var document = GetDocument(documents, "GameData/CombatSynchronizationPrototypes.cfg");
            var root = document.GetOrAddNode(MutantAggressionPolicy.SynchronizerSid(difficultySid),
                Array.Empty<string>());
            foreach (var rank in MutantAggressionPolicy.Ranks)
            {
                var table = tables.GetProperty(sourceSid).GetProperty(rank);
                root.RemoveChild(rank);
                var rankNode = root.GetOrAddChild(rank, useBpatch: false);
                edits += WriteCombatSyncTree(rankNode, table, rate, new List<string> { rank });
            }
            writtenParameters.Add(MutantAggressionPolicy.SpecialAbilityRateId);
        }

        return edits;
    }

    private static JsonElement LoadCombatSynchronizationTables()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CombatSyncResource)
            ?? throw new InvalidDataException("Missing combat synchronization tables resource.");
        using var json = JsonDocument.Parse(stream);
        var tables = json.RootElement.GetProperty("tables").Clone();
        // Guard the two scaled filter groups in every table and rank.
        foreach (var sid in new[] { "Default", "CombatSynchronizerStalker" })
        foreach (var rank in MutantAggressionPolicy.Ranks)
        {
            var groups = tables.GetProperty(sid).GetProperty(rank)
                .GetProperty("children").GetProperty("FilterGroups").GetProperty("children");
            foreach (var (index, tag, _, _) in MutantAggressionPolicy.AbilityTokenGroups)
            {
                var allowed = groups.GetProperty(index).GetProperty("children")
                    .GetProperty("AllowedTags").GetProperty("fields");
                if (allowed.GetProperty("[0]").GetString() != tag)
                    throw new InvalidDataException($"Combat synchronization {sid}.{rank}.{index} tag drifted.");
            }
        }
        return tables;
    }

    // Full non-bpatch copy of one rank table; only the ability token groups
    // take the scaled Default cooldowns (8–12 s divided by the rate).
    private static int WriteCombatSyncTree(PatchNode node, JsonElement source, double rate, List<string> path)
    {
        var edits = 0;
        var tokenGroup = path.Count == 3 && path[1] == "FilterGroups"
            ? MutantAggressionPolicy.AbilityTokenGroups.FirstOrDefault(g => g.Index == path[2])
            : default;
        foreach (var field in source.GetProperty("fields").EnumerateObject())
        {
            var value = field.Value.GetString() ?? string.Empty;
            if (tokenGroup.Index is not null && field.Name is "CooldownMin" or "CooldownMax")
                value = ((field.Name == "CooldownMin" ? tokenGroup.Min : tokenGroup.Max) / rate)
                    .ToString("0.##", CultureInfo.InvariantCulture);
            node.SetAssignment(field.Name, value);
            edits++;
        }
        foreach (var child in source.GetProperty("children").EnumerateObject())
        {
            var childNode = node.GetOrAddChild(child.Name, useBpatch: false);
            path.Add(child.Name);
            edits += WriteCombatSyncTree(childNode, child.Value, rate, path);
            path.RemoveAt(path.Count - 1);
        }
        return edits;
    }
}
