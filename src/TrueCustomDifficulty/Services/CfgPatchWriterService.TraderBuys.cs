using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

public sealed partial class CfgPatchWriterService
{
    private const string TraderBuysResource = "TrueCustomDifficulty.Data.trader-buy-policy-manifest.csv";
    private sealed record TraderBuysRow(
        string Scope, string File, string Prototype, string Role, bool Authored, IReadOnlyList<string> Blocked);

    // Source-identity manifest verified byte-identical against original CFG 2.1 TradePrototypes,
    // NOT from merchant stock generators or the current user's price settings.
    private static IReadOnlyList<TraderBuysRow> LoadTraderBuysRows()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TraderBuysResource)
            ?? throw new InvalidDataException("Missing embedded trader buy policy manifest.");
        using var reader = new StreamReader(stream);
        if (reader.ReadLine() != "scope,file,prototype,role,state,blocked_types")
            throw new InvalidDataException("Unexpected trader buy policy manifest header.");
        var result = new List<TraderBuysRow>();
        var keys = new HashSet<(string Scope, string Prototype)>();
        var native = TraderBuysPolicy.Categories.Select(x => x.NativeType)
            .ToHashSet(StringComparer.Ordinal);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 6)
                throw new InvalidDataException("Malformed trader buy policy source row.");
            var expectedFile = c[0] switch
            {
                "base" => "GameData/TradePrototypes.cfg",
                "dlc1" => "DLCGameData/DLC1/TradePrototypes.cfg",
                _ => throw new InvalidDataException("Unknown trader buy policy scope.")
            };
            if (c[1] != expectedFile || string.IsNullOrWhiteSpace(c[2])
                || !keys.Add((c[0], c[2])) || !TraderBuysPolicy.Roles.Any(r => r.Id == c[3]))
                throw new InvalidDataException("Unexpected trader buy policy consumer identity.");
            var authored = c[4] switch
            {
                "EXPLICIT" => true,
                "NOT_AUTHORED" => false,
                _ => throw new InvalidDataException("Unexpected buy denial list state.")
            };
            var blocked = c[5].Length == 0
                ? Array.Empty<string>()
                : c[5].Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (blocked.Any(x => !native.Contains(x))
                || blocked.Distinct(StringComparer.Ordinal).Count() != blocked.Length
                || (authored && blocked.Length == 0) || (!authored && blocked.Length != 0))
                throw new InvalidDataException("Unsafe trader buy denial list source values.");
            result.Add(new TraderBuysRow(c[0], c[1], c[2], c[3], authored, blocked));
        }

        if (result.Count != 86
            || result.Count(x => x.Scope == "base") != 68
            || result.Count(x => x.Scope == "dlc1") != 18
            || result.Count(x => x.Authored) != 53)
            throw new InvalidDataException("Trader buy policy original 86/68/18/53 census drifted.");

        var expectedCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["general"] = 33, ["medic"] = 15, ["technician"] = 16,
            ["bartender"] = 13, ["genericNpc"] = 9
        };
        foreach (var pair in expectedCounts)
            if (result.Count(x => x.Role == pair.Key) != pair.Value)
                throw new InvalidDataException("Trader buy policy role count drift: " + pair.Key);
        var eger = result.Single(x => x.Prototype == "EgerTrader_TradePrototype");
        var bolshak = result.Single(x => x.Prototype == "BolshakTechnician_TradePrototype");
        if (!eger.Blocked.Contains("Consumable")
            || bolshak.Scope != "dlc1" || bolshak.Authored)
            throw new InvalidDataException("Trader buy policy Eger/Bolshak exception drift.");
        return result;
    }

    // Only overrides intentionally changed checkbox values, never a blanket
    // force-allow/block based on the role's simplified vanilla display state.
    // A mixed exception (Eger, Bolshak) is untouched while its role cell remains
    // at default; once overridden, this category is applied to the entire role.
    private static int WriteTraderBuys(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var changes = new Dictionary<string, List<(string ParameterId, string Native, bool Allow)>>(StringComparer.Ordinal);
        foreach (var role in TraderBuysPolicy.Roles)
        {
            foreach (var category in TraderBuysPolicy.Categories)
            {
                var id = TraderBuysPolicy.ParameterId(role, category);
                if (!modifiedById.TryGetValue(id, out var parameter)) continue;
                if (!parameter.IsToggle)
                    throw new InvalidDataException("Trader buy control must be a toggle: " + id);
                if (!changes.TryGetValue(role.Id, out var group))
                    changes[role.Id] = group = new List<(string, string, bool)>();
                group.Add((id, category.NativeType, parameter.CurrentBoolean));
            }
        }
        if (changes.Count == 0) return 0;

        // Exact source and clone boundary is checked even if only one seller
        // role is edited. Never use the ItemGenerator inventory as a buyer list.
        var rows = LoadTraderBuysRows();
        var editCount = 0;
        foreach (var row in rows)
        {
            if (!IsScopeApplicable(row.Scope, hasDlc1)
                || !changes.TryGetValue(row.Role, out var categoryChanges)) continue;
            var next = row.Blocked.ToList();
            foreach (var change in categoryChanges)
            {
                if (change.Allow)
                    next.Remove(change.Native);
                else if (!next.Contains(change.Native, StringComparer.Ordinal))
                    next.Add(change.Native);
            }
            if (next.SequenceEqual(row.Blocked, StringComparer.Ordinal))
                continue;

            var doc = GetDocument(documents, row.File);
            var path = new[] { "TradeGenerators", "[0]" };
            if (next.Count == 0)
            {
                // Empty explicit list has unverified runtime semantics.
                doc.AddRemovalNode(row.Prototype, path, "BuyLimitations");
            }
            else
            {
                // Native numeric array must be reindexed contiguously.
                // Only this child is replaced; siblings (including durability,
                // money and modifiers) retain {bpatch} merge semantics.
                var list = doc.AddReplacementNode(row.Prototype, path, "BuyLimitations");
                for (var i = 0; i < next.Count; i++)
                    list.SetAssignment($"[{i}]", "EItemType::" + next[i]);
            }
            editCount++;
            foreach (var change in categoryChanges)
            {
                // A changed role cell is actionable across the group, even if
                // this particular trader already had the desired category.
                writtenParameters.Add(change.ParameterId);
            }
        }
        return editCount;
    }
}
