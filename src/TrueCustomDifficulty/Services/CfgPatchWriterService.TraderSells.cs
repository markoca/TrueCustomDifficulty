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
    private const string TraderSellsSourceResource = "TrueCustomDifficulty.Data.trader-sells-2.1-runtime.csv";
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
        // Normalize only Git checkout line endings. Any field or row drift
        // fails against the exact source-extracted S.T.A.L.K.E.R. 2.1 hash.
        using var sourceReader = new StreamReader(stream);
        var sourceText = sourceReader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceText)));
        if (!sourceHash.Equals(
                "9585C5B21BC13610DBC448650B2B053D582C852911782976076F5657F741E587",
                StringComparison.Ordinal))
            throw new InvalidDataException("Trader Sells requires the exact 2.1 source manifest.");
        using var reader = new StringReader(sourceText);
        const string expected = "trade_scope,trade_sid,trader_type,root_stock_sid,item_sid,item_category,"
            + "generator_sid,generator_scope,entry_cfg_path,direct_entry_state,nested_chain,"
            + "outer_links,trade_generator_slot,trade_generator_condition,root_generator_scope";
        if (reader.ReadLine() != expected)
            throw new InvalidDataException("Trader Sells 2.1 source manifest header changed.");
        var rows = new List<TraderSellOffer>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // This schema is 2.1-only: no 2.0.5 columns or slot fallback.
            var x = line.Split(',');
            if (x.Length != 15 || x.Any(v => v.Contains('"')))
                throw new InvalidDataException("Unexpected 2.1 trader stock CSV shape.");
            var row = new TraderSellOffer(x[0], x[1], x[2], x[3], x[4],
                x[5], x[6], x[7], x[8], x[9], x[10], x[11],
                int.Parse(x[12], System.Globalization.CultureInfo.InvariantCulture), x[14]);
            if (row.TradeScope is not ("base" or "dlc1")
                || row.GeneratorScope is not ("base" or "dlc1")
                || !TraderSellsPolicy.Roles.Any(r => r.SourceRole == row.TraderType)
                || !TraderSellsPolicy.Categories.Any(c => string.Equals(c.Label, row.Category, StringComparison.Ordinal))
                    && row.Category != "Binoculars"
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
            if (row.TradeGeneratorSlot != 0
                || x[13] != "ConstTrue"
                || row.RootGeneratorScope is not ("base" or "dlc1"))
                throw new InvalidDataException("Unsupported 2.1 playable trader slot or root scope: " + row.TradeSid);
            rows.Add(row);
        }
        if (rows.Count != 2939
            || rows.Count(x => x.GeneratorScope == "dlc1") != 113
            || rows.Count(x => x.State == "ZERO_CHANCE") != 6
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

    private const string TraderStockEntriesResource = "TrueCustomDifficulty.Data.trader-stock-entries-2.1.csv";
    private const string TraderStockEntriesHash =
        "98811084DAEED22AE2DEEB9A8A9FAE7DB119E07C9D875879F18E49519A586B3B";

    /// <summary>Source values of one trader stock entry (built from the 2.1 dump).</summary>
    private sealed record TraderStockEntry(
        string Scope, string GeneratorSid, string EntryPath, string ItemSid,
        string Field, string Value, string MinCount, string MaxCount,
        int ListCount, double ListWeightTotal);

    private static IReadOnlyDictionary<(string, string, string), TraderStockEntry> LoadTraderStockEntries()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(TraderStockEntriesResource)
            ?? throw new InvalidDataException("Missing trader stock entry values resource.");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        if (!hash.Equals(TraderStockEntriesHash, StringComparison.Ordinal))
            throw new InvalidDataException("Trader stock entry values require the exact 2.1 manifest.");
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines[0] != "generator_scope,generator_sid,entry_cfg_path,item_sid,field,value,"
                + "min_count,max_count,list_count,list_weight_total")
            throw new InvalidDataException("Trader stock entry values header changed.");
        var result = new Dictionary<(string, string, string), TraderStockEntry>();
        foreach (var line in lines.Skip(1))
        {
            var x = line.Split(',');
            if (x.Length != 10 || x[4] is not ("Chance" or "Weight"))
                throw new InvalidDataException("Unexpected trader stock entry value row.");
            result.Add((x[0], x[1], x[2]), new TraderStockEntry(x[0], x[1], x[2], x[3], x[4], x[5],
                x[6], x[7],
                x[8].Length == 0 ? 0 : int.Parse(x[8], System.Globalization.CultureInfo.InvariantCulture),
                x[9].Length == 0 ? 0 : double.Parse(x[9], System.Globalization.CultureInfo.InvariantCulture)));
        }
        if (result.Count != 608 || result.Values.Count(e => e.Field == "Weight") != 5)
            throw new InvalidDataException("Trader stock entry value census drifted (608 / 5 Weight).");
        return result;
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
        IReadOnlyDictionary<string, double> sellOdds,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        // Supply amounts: every Ammo / Medical offer of EVERY trader profile,
        // written as explicit source x factor counts into the per-trader
        // clones (never relying on inherited in-place edits).
        double Factor(string id) => modifiedById.TryGetValue(id, out var p) ? p.CurrentNumber : 1.0;
        var ammoFactor = Factor("economy.traderAmmoStock");
        var medicalFactor = Factor("economy.medicStock");
        // Partial odds per role x category (0 < x < 1); 0 is the SELL toggle.
        var odds = sellOdds.Where(pair => pair.Value > 0.0000001 && pair.Value < 1.0 - 0.0000001
                && TraderSellsPolicy.ParameterIds.Contains(pair.Key)
                && !modifiedById.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var changed = TraderSellsPolicy.Roles.SelectMany(role =>
            TraderSellsPolicy.Categories.Where(c => TraderSellsPolicy.IsAvailable(role, c))
                .Select(category => (Role: role, Category: category,
                    Id: TraderSellsPolicy.ParameterId(role, category))))
            .Where(x => modifiedById.ContainsKey(x.Id)).ToArray();
        // Individual item selection is a sidecar preset field; the canonical
        // 359 gameplay parameter IDs and the old role-level SELL flags survive.
        // Only existing source item SIDs may be removed.
        var disabledItems = TraderOffersPolicy.Decode(traderItemOverrides);
        var amountsChanged = Math.Abs(ammoFactor - 1.0) > 0.0000001
            || Math.Abs(medicalFactor - 1.0) > 0.0000001;
        if (changed.Length == 0 && disabledItems.Count == 0 && odds.Count == 0 && !amountsChanged)
            return 0;
        if (changed.Any(x => !modifiedById[x.Id].IsToggle || modifiedById[x.Id].CurrentBoolean))
            throw new InvalidDataException("Trader Sells only supports explicitly unchecked category removals.");

        var source = LoadTraderSellOffers();
        var entryValues = LoadTraderStockEntries();
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
            var labels = roleChanges.Select(c => c.Category.Label).ToHashSet(StringComparer.Ordinal);
            double OddsFor(TraderSellOffer r) => odds.TryGetValue(
                TraderSellsPolicy.Prefix + role.Id + "." + r.Category.ToLowerInvariant(), out var v) ? v : 1.0;
            double AmountFor(TraderSellOffer r) => r.Category switch
            {
                "Ammo" => ammoFactor, "Medical" => medicalFactor, _ => 1.0
            };
            bool Removed(TraderSellOffer r) => labels.Contains(r.Category)
                || roleItemKeys.Contains(TraderOffersPolicy.Key(role.Id, r.ItemSid));
            var affected = profile.Where(r => Removed(r)
                    || Math.Abs(OddsFor(r) - 1.0) > 0.0000001
                    || Math.Abs(AmountFor(r) - 1.0) > 0.0000001)
                .GroupBy(r => (r.GeneratorScope, r.GeneratorSid, r.EntryPath, r.ItemSid,
                    r.NestedChain, r.OuterLinks)).Select(g => g.First()).ToArray();
            if (affected.Length == 0) continue;

            // The root may have no direct leaves: all stock can be nested.
            // For 2.1, its scope must come from source inventory, never be
            // guessed from the TradePrototype scope (DLC trades can use base roots).
            var rootScope = profile.Where(x => x.GeneratorSid == first.RootSid)
                .Select(x => x.GeneratorScope).Distinct().ToArray();
            // Source resolved from 2.1 ItemGeneratorPrototypes definitions.
            // A DLC TradePrototype can inherit a base-game stock generator.
            var originalRootScope = first.RootGeneratorScope;
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
                    // The 2.1 source may reference one child from multiple parent
                    // positions. Redirect all positions, never the shared original.
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
                    var entry = entryValues[(leaf.GeneratorScope, leaf.GeneratorSid, leaf.EntryPath)];
                    if (entry.ItemSid != leaf.ItemSid)
                        throw new InvalidDataException("Trader stock entry value mismatch: " + leaf.EntryPath);
                    var keep = Removed(leaf) ? 0.0 : OddsFor(leaf);
                    if (entry.Field == "Chance" && keep <= 0.0000001)
                    {
                        // Chance offer removed: no other offer gains from it.
                        document.AddRemovalNode(targetClone, path[..^1], path[^1]);
                        edited++;
                        continue;
                    }
                    if (Math.Abs(keep - 1.0) > 0.0000001)
                    {
                        var sourceValue = ParseNumber(entry.Value);
                        if (entry.Field == "Chance")
                            document.AddAssignment(targetClone, path, "Chance",
                                FormatLikeSource(Math.Clamp(sourceValue * keep, 0.0, 1.0), entry.Value));
                        else
                        {
                            // Weight offer (single-entry native lists): keep x
                            // of its odds, the rest goes to a native Empty pick.
                            document.AddAssignment(targetClone, path, "Weight",
                                FormatLikeSource(sourceValue * keep, entry.Value));
                            var removed = sourceValue * (1.0 - keep);
                            if (removed > 0.0000001)
                            {
                                var emptyPath = path[..^1].Append($"[{entry.ListCount}]").ToArray();
                                document.AddAssignment(targetClone, emptyPath, "ItemPrototypeSID", "Empty");
                                document.AddAssignment(targetClone, emptyPath, "Weight",
                                    removed.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
                                edited++;
                            }
                        }
                        edited++;
                    }
                    var amount = AmountFor(leaf);
                    if (Math.Abs(amount - 1.0) > 0.0000001)
                        foreach (var (countKey, countRaw) in new[] { ("MinCount", entry.MinCount), ("MaxCount", entry.MaxCount) })
                        {
                            if (!int.TryParse(countRaw, System.Globalization.NumberStyles.Integer,
                                    System.Globalization.CultureInfo.InvariantCulture, out var sourceCount) || sourceCount <= 0)
                                continue;
                            document.AddAssignment(targetClone, path, countKey,
                                ScalePositiveInteger(sourceCount, amount).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            edited++;
                        }
                }
            }

            GetDocument(documents, SellTradeFile(first.TradeScope)).AddAssignment(
                first.TradeSid, new[] { "TradeGenerators", "[" + first.TradeGeneratorSlot + "]" },
                "ItemGeneratorPrototypeSID", rootClone);
            edited++;
            foreach (var c in roleChanges)
                if (affected.Any(r => r.Category == c.Category.Label))
                    writtenParameters.Add(c.Id);
            if (Math.Abs(ammoFactor - 1.0) > 0.0000001 && affected.Any(r => r.Category == "Ammo"))
                writtenParameters.Add("economy.traderAmmoStock");
            if (Math.Abs(medicalFactor - 1.0) > 0.0000001 && affected.Any(r => r.Category == "Medical"))
                writtenParameters.Add("economy.medicStock");
        }

        return edited;
    }
}
