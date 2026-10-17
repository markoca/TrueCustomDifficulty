using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

// Read-only Phase 4 source browser. Branches are derived from the same TWO
// embedded, authoritative inputs consumed by the production stash writers.
// The phase-1 CSV in docs is an audit snapshot, not a second runtime table.
public sealed record StashBranchPreviewRow(
    int Index, string ItemSid, string SourceRaw,
    string MinCount, string MaxCount, CfgWriterCheapStashItem? TierItem);

public sealed record StashBranchPreview(
    string Layer, string Tier, string Prototype, int RankIndex,
    string RankGate, string DifficultyGate, string GroupPath,
    string Category, string Field, string MinSpawnChance,
    string MaxSpawnChance, string ItemSetCount,
    CfgWriterCheapStashCategory? TierCategory,
    IReadOnlyList<StashBranchPreviewRow> Items)
{
    public bool IsSmart => Layer == "SmartLoot";

    // A missing native gate means all selected ranks/difficulties, not a
    // fabricated gate or a numeric spawn probability.
    public bool AppliesTo(int rank, string difficulty)
        => (IsSmart ? RankIndex == rank
            : RankGate.Length == 0
              || RankGate.Split(',').Any(x => x.Trim().Equals(
                  "ERank::" + RankName(rank), StringComparison.Ordinal)))
           && (IsSmart || DifficultyGate.Length == 0
               || DifficultyGate.Split(',').Any(x => x.Trim().Equals(
                   "EGameDifficulty::" + difficulty, StringComparison.Ordinal)));

    public static string RankName(int rank) => rank switch
    {
        0 => "Newbie", 1 => "Experienced", 2 => "Veteran", 3 => "Master",
        _ => throw new ArgumentOutOfRangeException(nameof(rank))
    };

    public string EffectiveSelection(CfgWriterBaseline baseline,
        StashEffectiveState state, StashBranchPreviewRow item)
        => IsSmart
            ? state.SmartSelection(item.ItemSid, Field, item.SourceRaw)
            : state.TierField(baseline, Tier, TierCategory!, item.TierItem!, Field);

    public string EffectiveCount(CfgWriterBaseline baseline,
        StashEffectiveState state, StashBranchPreviewRow item, string field)
    {
        var raw = field == "MinCount" ? item.MinCount : item.MaxCount;
        if (raw.Length == 0) return "";
        if (!IsSmart)
            return state.TierField(baseline, Tier, TierCategory!, item.TierItem!, field);
        return state.SmartCount(item.ItemSid, field, raw,
            item.MinCount, item.MaxCount, Field);
    }

    public (string Min, string Max, string Sets) EffectiveSpawn(StashEffectiveState state)
    {
        if (!IsSmart) return ("", "", "");
        var min = MinSpawnChance;
        var max = MaxSpawnChance;
        var sets = ItemSetCount;
        if (StashEffectiveState.UniformSmartFactor(
                Items.Select(i => state.SmartSelectionFactor(i.ItemSid, Field)),
                out var factor)
            && (Field == "Chance" || factor <= 0.0000001))
        {
            if (min.Length > 0) min = StashEffectiveState.SmartSpawnEndpoint(min, factor);
            if (max.Length > 0) max = StashEffectiveState.SmartSpawnEndpoint(max, factor);
            if (factor <= 0 && sets.Length > 0) sets = "0";
        }
        return (min, max, sets);
    }
}

public static class StashBranchPreviewCatalog
{
    private const string SmartSource =
        "TrueCustomDifficulty.Data.smartloot-world-source-entries.csv";

    private sealed record SmartRecord(
        string Prototype, int Rank, string Group, int GroupIndex,
        int ItemIndex, string ItemSid, string Weight, string Chance,
        string MinCount, string MaxCount, string MinSpawn, string MaxSpawn, string Sets);

    public static IReadOnlyList<StashBranchPreview> Load(CfgWriterBaseline baseline)
    {
        var result = new List<StashBranchPreview>(463);
        AddTier(result, "Cheap", baseline.Special.CheapStashProduction.CustomGeneratorSid,
            baseline.Special.CheapStashProduction.TemplateCategories);
        AddTier(result, "Medium", baseline.Special.MediumStashAmmoProduction.CustomGeneratorSid,
            baseline.Special.MediumStashAmmoProduction.TemplateCategories);
        AddTier(result, "Expensive", baseline.Special.ExpensiveStashProduction.CustomGeneratorSid,
            baseline.Special.ExpensiveStashProduction.TemplateCategories);

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(SmartSource)
            ?? throw new InvalidDataException("Missing embedded SmartLoot source for branch browser.");
        using var reader = new StreamReader(stream);
        if (reader.ReadLine() is null)
            throw new InvalidDataException("Empty SmartLoot source CSV.");
        var smart = new List<SmartRecord>(991);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            // This writer-owned 14-column source contains no quoted fields.
            // Fail closed if its format changes instead of silently merging paths.
            var fields = line.Split(',');
            if (fields.Length != 14 || fields.Any(x => x.Contains('"')))
                throw new InvalidDataException("SmartLoot source CSV schema drift.");
            smart.Add(new SmartRecord(fields[0],
                int.Parse(fields[1], CultureInfo.InvariantCulture),
                fields[3],
                int.Parse(fields[4], CultureInfo.InvariantCulture),
                int.Parse(fields[5], CultureInfo.InvariantCulture),
                fields[6], fields[7], fields[8], fields[9], fields[10],
                fields[11], fields[12], fields[13]));
        }

        foreach (var group in smart.GroupBy(x =>
                     (x.Prototype, x.Rank, x.Group, x.GroupIndex)))
        {
            var ordered = group.OrderBy(x => x.ItemIndex).ToArray();
            var first = ordered[0];
            var field = first.Chance.Length > 0 ? "Chance" : "Weight";
            if (ordered.Any(x => (x.Chance.Length > 0 ? "Chance" : "Weight") != field
                || (field == "Chance" ? x.Chance : x.Weight).Length == 0))
                throw new InvalidDataException("Mixed/empty native SmartLoot source group.");
            result.Add(new StashBranchPreview(
                "SmartLoot", "", first.Prototype, first.Rank,
                "ERank::" + StashBranchPreview.RankName(first.Rank), "",
                $"ItemGenerators.[{first.Rank}].SmartLootParams.{first.Group}.[{first.GroupIndex}].Items",
                first.Group, field,
                first.MinSpawn, first.MaxSpawn, first.Sets, null,
                ordered.Select(x => new StashBranchPreviewRow(x.ItemIndex, x.ItemSid,
                    field == "Chance" ? x.Chance : x.Weight, x.MinCount,
                    x.MaxCount, null)).ToArray()));
        }

        if (smart.Count != 991 || result.Count != 463
            || result.Count(x => !x.IsSmart) != 41
            || result.Count(x => x.IsSmart) != 422
            || result.Sum(x => x.Items.Count) != 1124
            || result.Any(x => x.Items.Count == 0))
            throw new InvalidDataException("Stashes branch inventory drifted from phase-1 source.");
        return result;
    }

    private static void AddTier(List<StashBranchPreview> target,
        string tier, string cloneSid, IEnumerable<CfgWriterCheapStashCategory> categories)
    {
        if (!cloneSid.StartsWith("TCD_", StringComparison.Ordinal))
            throw new InvalidDataException("Unexpected tier clone SID.");
        foreach (var category in categories)
        {
            var field = category.Items[0].Fields.ContainsKey("Weight") ? "Weight" : "Chance";
            if (category.Items.Any(x => !x.Fields.ContainsKey(field)
                || x.Fields.ContainsKey(field == "Weight" ? "Chance" : "Weight")))
                throw new InvalidDataException("Mixed native ItemGenerator branch.");
            category.Fields.TryGetValue("PlayerRank", out var rank);
            category.Fields.TryGetValue("Diff", out var difficulty);
            category.Fields.TryGetValue("Category", out var kind);
            target.Add(new StashBranchPreview(
                "ItemGenerator", tier, cloneSid[4..], -1, rank ?? "",
                difficulty ?? "", "ItemGenerator." + category.Node + ".PossibleItems",
                kind?.Replace("EItemGenerationCategory::", "") ?? "",
                field, "", "", "", category,
                category.Items.Select(x => new StashBranchPreviewRow(
                    int.Parse(x.Node.Trim('[', ']'), CultureInfo.InvariantCulture),
                    x.Fields["ItemPrototypeSID"], x.Fields[field],
                    x.Fields.TryGetValue("MinCount", out var min) ? min : "",
                    x.Fields.TryGetValue("MaxCount", out var max) ? max : "",
                    x)).ToArray()));
        }
    }
}
