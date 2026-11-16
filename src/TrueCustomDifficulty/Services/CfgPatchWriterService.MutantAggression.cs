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
//  * ATTACK RATE per family → DifficultyPrototypes [difficulty]
//    .MutantCombatDifficulty.AgentCooldownMultipliers (full list, non-bpatch).
//  * SPECIAL ABILITY RATE → CombatSynchronizationPrototypes
//    CombatSynchronizer{difficulty}.{rank}.FilterGroups[3|4] cooldowns
//    (full rank tables, because Novice–Veteran inherit Default).
public sealed partial class CfgPatchWriterService
{
    private const string CombatSyncResource = "TrueCustomDifficulty.Data.combat-synchronization-2.1.json";

    private static double MutantRateReference(MutantAggressionPolicy.Family family, string difficultySid)
        => difficultySid == "Stalker" && family.HasVanillaMasterEntries ? 2.0 : 1.0;

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

        // ---- ATTACK RATE: AgentCooldownMultipliers ----------------------------
        var changedFamilies = MutantAggressionPolicy.Families
            .Where(f => modifiedById.TryGetValue(f.ParameterId, out var p)
                && p.IsValueModifiedFromReference())
            .ToArray();
        if (changedFamilies.Length > 0)
        {
            var document = GetDocument(documents, "GameData/DifficultyPrototypes.cfg");
            var mutant = document.GetOrAddNode(difficultySid, new[] { "MutantCombatDifficulty" });
            // The whole list is replaced: vanilla families keep their exact
            // vanilla entries (Master: 0.5 for the listed agents), changed
            // families list every variant with multiplier 1/rate.
            mutant.RemoveAssignment("AgentCooldownMultipliers");
            mutant.RemoveChild("AgentCooldownMultipliers");
            var list = mutant.GetOrAddChild("AgentCooldownMultipliers", useBpatch: false);
            list.Clear();
            var index = 0;
            foreach (var family in MutantAggressionPolicy.Families)
            {
                if (!allById.TryGetValue(family.ParameterId, out var parameter))
                    throw new InvalidDataException("Missing mutant attack rate source: " + family.ParameterId);
                var rate = parameter.CurrentNumber;
                if (!double.IsFinite(rate) || rate <= 0.0)
                    throw new InvalidDataException("Invalid mutant attack rate: " + family.ParameterId);
                var reference = MutantRateReference(family, difficultySid);
                IEnumerable<(MutantAggressionPolicy.Agent Agent, double Multiplier)> entries;
                if (Math.Abs(rate - reference) < 0.0000001)
                    entries = difficultySid == "Stalker"
                        ? family.Agents.Where(a => a.VanillaOnMaster)
                            .Select(a => (a, MutantAggressionPolicy.VanillaMasterCooldownMultiplier))
                        : Enumerable.Empty<(MutantAggressionPolicy.Agent, double)>();
                else if (Math.Abs(rate - 1.0) < 0.0000001)
                    entries = Enumerable.Empty<(MutantAggressionPolicy.Agent, double)>();
                else
                    entries = family.Agents.Select(a => (a, 1.0 / rate));

                foreach (var (agent, multiplier) in entries)
                {
                    var entry = list.GetOrAddChild($"[{index++}]", useBpatch: false);
                    entry.SetAssignment("AgentPrototypeSID", agent.Sid);
                    var cooldowns = entry.GetOrAddChild("CooldownMultipliers", useBpatch: false);
                    for (var t = 0; t < agent.Tags.Count; t++)
                    {
                        var tag = cooldowns.GetOrAddChild($"[{t}]", useBpatch: false);
                        tag.SetAssignment("CooldownTag", agent.Tags[t]);
                        tag.SetAssignment("CooldownMultiplier", FormatCooldownMultiplier(multiplier));
                        edits++;
                    }
                }
            }
            foreach (var family in changedFamilies)
                writtenParameters.Add(family.ParameterId);
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
