using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Stock generator copy-on-write: each affected TradePrototype receives
/// dedicated inherited generator clones, including nested shared pools.
/// No original generator is mutated, even where quest/NPC consumers share it.
/// </summary>
public sealed partial class CfgPatchWriterService
{
    private const string TraderSellsSourceResource = "TrueCustomDifficulty.Data.trader-offers-source-rows.csv";
    private sealed record TraderSellOffer(
        string TradeScope, string TradeSid, string TraderType, string RootSid,
        string ItemSid, string Category, string GeneratorSid, string GeneratorScope,
        string EntryPath, string State, string NestedChain, string OuterLinks,
        int TradeGeneratorSlot, string RootGeneratorScope);

    private static IReadOnlyList<TraderSellOffer> LoadTraderSellOffers()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(TraderSellsSourceResource)
            ?? throw new InvalidDataException("Missing source-exact Trader Sells inventory resource.");
        // Git for Windows can check out text CSV with CRLF while the committed
        // 2.0.5 source snapshot is LF. Normalize only line endings before the
        // SHA256 check; any actual source-value/field drift still fails closed.
        using var sourceReader = new StreamReader(stream);
        var sourceText = sourceReader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceText)));
        var is21 = sourceHash.Equals(
            "9585C5B21BC13610DBC448650B2B053D582C852911782976076F5657F741E587",
            StringComparison.Ordinal);
        if (!is21 && !sourceHash.Equals(
                "6B06E7F3B6B4844E4043F4F620F7B8A364ED7623728027315AD90E7D74C2E05F",
                StringComparison.Ordinal))
            throw new InvalidDataException("Trader Sells source integrity check failed.");
        using var reader = new StringReader(sourceText);
        const string expected = "trade_scope,trade_sid,trader_type,root_stock_sid,item_sid,item_category,"
            + "generator_sid,generator_scope,generator_cfg_line,entry_cfg_line,entry_cfg_path,"
            + "generation_native_category,player_ranks,chance,weight,min_count,max_count,"
            + "direct_entry_state,nested_chain,outer_links";
        const string expected21 = "trade_scope,trade_sid,trader_type,root_stock_sid,item_sid,item_category,"
            + "generator_sid,generator_scope,entry_cfg_path,direct_entry_state,nested_chain,"
            + "outer_links,trade_generator_slot,trade_generator_condition,root_generator_scope";
        if (reader.ReadLine() != (is21 ? expected21 : expected))
            throw new InvalidDataException("Trader Sells native source manifest header changed.");
        var rows = new List<TraderSellOffer>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // The original 2.0.5 extractor emits no quoted/comma-valued cells.
            // Fail closed on source shape drift rather than misrouting an entry.
            var x = line.Split(',');
            if (x.Length != (is21 ? 15 : 20) || x.Any(v => v.Contains('"')))
                throw new InvalidDataException("Unexpected trader stock CSV shape.");
            var row = is21
                ? new TraderSellOffer(x[0], x[1], x[2], x[3], x[4],
                    x[5], x[6], x[7], x[8], x[9], x[10], x[11],
                    int.Parse(x[12], System.Globalization.CultureInfo.InvariantCulture), x[14])
                : new TraderSellOffer(x[0], x[1], x[2], x[3], x[4],
                    x[5], x[6], x[7], x[10], x[17], x[18], x[19], 0, "");
            if (row.TradeScope is not ("base" or "dlc1")
                || row.GeneratorScope is not ("base" or "dlc1")
                || !TraderSellsPolicy.Roles.Any(r => r.SourceRole == row.TraderType)
                || !TraderSellsPolicy.Categories.Any(c => string.Equals(c.Label, row.Category, StringComparison.Ordinal))
                    && row.Category is not ("NightVision" or "Binoculars")
                || row.EntryPath.Split('/').Length != 4
                || !row.EntryPath.StartsWith("ItemGenerator/[", StringComparison.Ordinal)
                || !row.EntryPath.Contains("/PossibleItems/[", StringComparison.Ordinal)
                || row.State is not ("CANDIDATE" or "ZERO_CHANCE")
                || row.ItemSid.Length == 0 || row.RootSid.Length == 0
                || row.GeneratorSid.Length == 0)
                throw new InvalidDataException("Unsafe trader stock original entry: " + row.TradeSid);
            var chain = row.NestedChain.Split('>', StringSplitOptions.RemoveEmptyEntries);
            if (chain.Length is < 1 or > 2 || chain[0] != row.RootSid
                || chain[^1] != row.GeneratorSid
                || (chain.Length == 1) != string.IsNullOrWhiteSpace(row.OuterLinks))
                throw new InvalidDataException("Trader stock graph / nested edge mismatch: " + row.TradeSid);
            if (is21 && (row.TradeGeneratorSlot != 0
                || x[13] != "ConstTrue"
                || row.RootGeneratorScope is not ("base" or "dlc1")))
                throw new InvalidDataException("Unsupported 2.1 playable trader slot or root scope: " + row.TradeSid);
            rows.Add(row);
        }
        if (rows.Count != 2939
            || rows.Count(x => x.GeneratorScope == "dlc1") != 113
            || rows.Count(x => x.State == "ZERO_CHANCE") != (is21 ? 6 : 1)
            || rows.Select(x => (x.TradeScope, x.TradeSid)).Distinct().Count() != 86
            || rows.Select(x => (x.GeneratorScope, x.GeneratorSid)).Distinct().Count() != 62
            || rows.Select(x => (x.GeneratorScope, x.GeneratorSid, x.EntryPath, x.ItemSid))
                .Distinct().Count() != 608)
            throw new InvalidDataException("Trader Sells source census drifted (2939/86/62/608).");
        var profiles = rows.GroupBy(x => (x.TradeScope, x.TradeSid, x.TradeGeneratorSlot)).ToArray();
        if (profiles.Count(x => x.Key.TradeScope == "base") != 68
            || profiles.Count(x => x.Key.TradeScope == "dlc1") != 18
            || profiles.Any(p => p.Select(x => (x.RootSid, x.TraderType)).Distinct().Count() != 1))
            throw new InvalidDataException("Trader Sells 68/18 root / role inventory drifted.");
        foreach (var role in TraderSellsPolicy.Roles)
            if (profiles.Count(p => p.First().TraderType == role.SourceRole) != (role.Id switch
                {
                    "general" => 33, "medic" => 15, "technician" => 16,
                    "bartender" => 13, "genericNpc" => 9, _ => 0
                }))
                throw new InvalidDataException("Trader Sells role census drifted: " + role.Id);
        return rows;
    }

    private static string SellCloneSid(string scope, string tradeSid, int slot, string sourceSid)
    {
        var seed = slot == 0
            ? scope + "|" + tradeSid + "|" + sourceSid
            : scope + "|" + tradeSid + "|" + slot + "|" + sourceSid;
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return "TCD_TS_" + Convert.ToHexString(digest.AsSpan(0, 10));
    }

    private static string SellGeneratorFile(string scope) => scope switch
    {
        "base" => "GameData/ItemGeneratorPrototypes.cfg",
        "dlc1" => "DLCGameData/DLC1/ItemGeneratorPrototypes.cfg",
        _ => throw new InvalidDataException("Unknown native trader generator scope.")
    };
    private static string SellTradeFile(string scope) => scope switch
    {
        "base" => "GameData/TradePrototypes.cfg",
        "dlc1" => "DLCGameData/DLC1/TradePrototypes.cfg",
        _ => throw new InvalidDataException("Unknown native trade profile scope.")
    };

    private static int WriteTraderSells(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        string traderItemOverrides,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var changed = TraderSellsPolicy.Roles.SelectMany(role =>
            TraderSellsPolicy.Categories.Where(c => TraderSellsPolicy.IsAvailable(role, c))
                .Select(category => (Role: role, Category: category,
                    Id: TraderSellsPolicy.ParameterId(role, category))))
            .Where(x => modifiedById.ContainsKey(x.Id)).ToArray();
        // Individual item selection is a sidecar preset field; the canonical
        // 359 gameplay parameter IDs and the old role-level SELL flags survive.
        // Only existing source item SIDs may be removed.
        var disabledItems = TraderOffersPolicy.Decode(traderItemOverrides);
        if (changed.Length == 0 && disabledItems.Count == 0) return 0;
        if (changed.Any(x => !modifiedById[x.Id].IsToggle || modifiedById[x.Id].CurrentBoolean))
            throw new InvalidDataException("Trader Sells only supports explicitly unchecked category removals.");

        var source = LoadTraderSellOffers();
        var edited = 0;
        var cloneIdentities = new Dictionary<string, (string Scope, string Source)>(StringComparer.Ordinal);
        void RegisterClone(string sid, string scope, string original)
        {
            if (cloneIdentities.TryGetValue(sid, out var previous)
                && previous != (scope, original))
                throw new InvalidDataException("Trader Sells SID hash collision: " + sid);
            cloneIdentities[sid] = (scope, original);
        }

        foreach (var profile in source.GroupBy(r => (r.TradeScope, r.TradeSid, r.TradeGeneratorSlot)))
        {
            var first = profile.First();
            if (!IsScopeApplicable(first.TradeScope, hasDlc1)) continue;
            var roleChanges = changed.Where(c => c.Role.SourceRole == first.TraderType).ToArray();
            var role = TraderSellsPolicy.Roles.Single(x => x.SourceRole == first.TraderType);
            var roleItemKeys = disabledItems.Where(x =>
                x.StartsWith(role.Id + "|", StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);
            if (roleChanges.Length == 0 && roleItemKeys.Count == 0) continue;
            var labels = roleChanges.Select(c => c.Category.Label).ToHashSet(StringComparer.Ordinal);
            var affected = profile.Where(r => labels.Contains(r.Category) ||
                roleItemKeys.Contains(TraderOffersPolicy.Key(role.Id, r.ItemSid)))
                .GroupBy(r => (r.GeneratorScope, r.GeneratorSid, r.EntryPath, r.ItemSid,
                    r.NestedChain, r.OuterLinks)).Select(g => g.First()).ToArray();
            if (affected.Length == 0) continue;

            // The root may have no direct leaves: all stock can be nested.
            // For 2.1, its scope must come from source inventory, never be
            // guessed from the TradePrototype scope (DLC trades can use base roots).
            var rootScope = profile.Where(x => x.GeneratorSid == first.RootSid)
                .Select(x => x.GeneratorScope).Distinct().ToArray();
            var originalRootScope = first.RootGeneratorScope.Length != 0
                ? first.RootGeneratorScope
                : rootScope.Length switch
                {
                    1 => rootScope[0],
                    0 when first.RootSid == "MedicUniversal_TradeItemGenerator" => "base",
                    0 => first.TradeScope, // Legacy 2.0.5 fallback only.
                    _ => throw new InvalidDataException("Mixed trader original generator scope.")
                };
            if (profile.Any(x => x.RootGeneratorScope != first.RootGeneratorScope)
                || originalRootScope is not ("base" or "dlc1")
                || rootScope.Any(x => x != originalRootScope))
                throw new InvalidDataException("Trader Sells root generator source scope mismatch: " + first.TradeSid);
            var rootClone = SellCloneSid(first.TradeScope, first.TradeSid, first.TradeGeneratorSlot, first.RootSid);
            RegisterClone(rootClone, originalRootScope, first.RootSid);
            var rootDocument = GetDocument(documents, SellGeneratorFile(originalRootScope));
            var root = rootDocument.GetOrAddReferencedRoot(
                rootClone, "../ItemGeneratorPrototypes.cfg", first.RootSid);
            root.SetAssignment("SID", rootClone);
            edited++;

            foreach (var targetGroup in affected.GroupBy(r => (r.GeneratorScope, r.GeneratorSid)))
            {
                var t = targetGroup.First();
                var isRoot = t.GeneratorSid == first.RootSid;
                var targetClone = isRoot
                    ? rootClone
                    : SellCloneSid(first.TradeScope, first.TradeSid, first.TradeGeneratorSlot, t.GeneratorSid);
                if (!isRoot)
                {
                    RegisterClone(targetClone, t.GeneratorScope, t.GeneratorSid);
                    var targetDoc = GetDocument(documents, SellGeneratorFile(t.GeneratorScope));
                    var target = targetDoc.GetOrAddReferencedRoot(
                        targetClone, "../ItemGeneratorPrototypes.cfg", t.GeneratorSid);
                    target.SetAssignment("SID", targetClone);
                    edited++;
                    // A source generator can be referenced by multiple parent slots.
                    // The legacy 2.0.5 manifest contains one slot per row; the 2.1
                    // extractor records multiple slots separated by '|'. Redirect
                    // every slot to the same private child clone, never the source.
                    var linkPaths = targetGroup
                        .SelectMany(r => r.OuterLinks.Split('|', StringSplitOptions.RemoveEmptyEntries))
                        .Distinct(StringComparer.Ordinal);
                    foreach (var link in linkPaths)
                    {
                        var segments = link.Split(':');
                        if (segments.Length != 4 || segments[0] != first.RootSid
                            || !segments[1].StartsWith("ItemGenerator/[", StringComparison.Ordinal)
                            || segments[2].StartsWith("chance=", StringComparison.Ordinal) == false
                            || segments[3].StartsWith("weight=", StringComparison.Ordinal) == false)
                            throw new InvalidDataException("Trader Sells nested source path drift: " + link);
                        rootDocument.AddAssignment(rootClone,
                            segments[1].Split('/', StringSplitOptions.RemoveEmptyEntries),
                            "ItemGeneratorPrototypeSID", targetClone);
                        edited++;
                    }
                }
                var document = GetDocument(documents, SellGeneratorFile(t.GeneratorScope));
                foreach (var leaf in targetGroup.GroupBy(r => r.EntryPath, StringComparer.Ordinal)
                    .Select(g => g.First()))
                {
                    var path = leaf.EntryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    document.AddRemovalNode(targetClone, path[..^1], path[^1]);
                    edited++;
                }
            }

            GetDocument(documents, SellTradeFile(first.TradeScope)).AddAssignment(
                first.TradeSid, new[] { "TradeGenerators", "[" + first.TradeGeneratorSlot + "]" },
                "ItemGeneratorPrototypeSID", rootClone);
            edited++;
            foreach (var c in roleChanges)
                if (affected.Any(r => r.Category == c.Category.Label))
                    writtenParameters.Add(c.Id);
        }

        return edited;
    }
}
