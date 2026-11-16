using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.ViewModels;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

// DLC1-only source-native clone of a separately authored ordinary CheapNoFresh
// stash pool. The six audited actors are the ONLY consumers redirected here;
// quest, clue, CNPP and named unique generator fields are never rewritten.
public sealed partial class CfgPatchWriterService
{
    private const string CheapNoFreshSid = "GamePass_Stash_ItemGenerator_CheapNoFresh";
    private const string TcdCheapNoFreshSid = "TCD_GamePass_Stash_ItemGenerator_CheapNoFresh";
    private const string CheapNoFreshItemResource =
        "TrueCustomDifficulty.Data.cheap-no-fresh-stash-item-entries.csv";

    private sealed record CheapNoFreshItem(
        int Category, int Item, string Sid, string Chance, string Weight,
        string MinCount, string MaxCount);

    private static IReadOnlyList<CheapNoFreshItem> LoadCheapNoFreshItems()
    {
        var rows = ReadStashCsv(CheapNoFreshItemResource, 7);
        var entries = rows.Select(x => new CheapNoFreshItem(
            int.Parse(x[0], CultureInfo.InvariantCulture),
            int.Parse(x[1], CultureInfo.InvariantCulture),
            x[2], x[3], x[4], x[5], x[6])).ToArray();
        if (entries.Length != 35 ||
            entries.Select(x => (x.Category, x.Item)).Distinct().Count() != 35 ||
            entries.Any(x => x.Category is < 0 or > 11 || x.Item is < 0 or > 5 ||
                x.Sid.Length == 0 || StashItemBiasId(x.Sid) is null ||
                (x.Chance.Length == 0 && x.Weight.Length == 0) ||
                !int.TryParse(x.MinCount, out _) || !int.TryParse(x.MaxCount, out _)))
            throw new InvalidDataException("DLC1 CheapNoFresh source inventory changed.");
        return entries;
    }

    private static int WriteDlcCheapNoFreshStashes(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        IReadOnlyDictionary<string, ParameterViewModel> all,
        IReadOnlyDictionary<string, double> stashPresence,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> written)
    {
        if (!hasDlc1)
            return 0;
        var cheapConfig = baseline.Special.CheapStashProduction;
        var ammoId = cheapConfig.AmmoQuantityParameterId;
        var grenadeChanceId = cheapConfig.GrenadeChanceParameterId;
        var grenadeAmountId = cheapConfig.GrenadeQuantityParameterId;
        // Extra Gear is produced per physical actor by WriteStashGearCoverage,
        // not by extending the shared CheapNoFresh pool or its legacy gear IDs.
        // Same inputs as the Basic tier template: the BASIC column of ADJUST
        // ODDS (stashMix cells, Basic AMMO presence), MEDICAL COMPOSITION,
        // AMMO AMOUNT, RGD-5 and STASH SUPPLY REDUCTION.
        var relevant = StashMixParameterIds.Concat(StashMedicalParameterIds)
            .Concat(new[] { ammoId, grenadeChanceId, grenadeAmountId, StashSupplyReductionId })
            .Where(modified.ContainsKey).ToArray();
        var presenceChanged = stashPresence.TryGetValue(StashPresenceViewModel.CheapAmmoId,
            out var cheapAmmoPresence) && Math.Abs(cheapAmmoPresence - 1.0) > 0.0000001;
        if (relevant.Length == 0 && !presenceChanged)
            return 0;

        var actors = DlcActors.Value.Where(x =>
            x.ActorSid.Length == 32 && CheapNoFreshActorIds.Contains(x.ActorSid))
            .ToArray();
        if (actors.Length != 6)
            throw new InvalidDataException("Expected six safe DLC1 CheapNoFresh actors.");
        var actorRows = ReadStashCsv(DlcStashActorResource, 4);
        if (actorRows.Count(x => x[2] == CheapNoFreshSid) != 6 ||
            actors.Any(x => !actorRows.Any(r => r[0] == x.ActorSid
                && r[2] == CheapNoFreshSid)))
            throw new InvalidDataException("DLC1 CheapNoFresh actor generator scope changed.");

        // CheapNoFresh has the Basic template's twelve categories (identical
        // category fields, verified against the 2.1 base dump) with its own
        // items. Build them as Basic categories so ONE rule applies:
        // StashEffectiveState.WeightListEdit (kept Weight + Empty) for Weight
        // lists and TierField("Cheap") for Chance rows and counts.
        var sourceItems = LoadCheapNoFreshItems();
        if (cheapConfig.TemplateCategories.Count != 12)
            throw new InvalidDataException("Basic template category count changed.");
        var categories = cheapConfig.TemplateCategories.Select((template, index) =>
            new CfgWriterCheapStashCategory
            {
                Node = template.Node,
                Fields = new Dictionary<string, string>(template.Fields, StringComparer.Ordinal),
                Items = sourceItems.Where(x => x.Category == index).OrderBy(x => x.Item)
                    .Select(x =>
                    {
                        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["ItemPrototypeSID"] = x.Sid
                        };
                        if (x.Chance.Length > 0) fields["Chance"] = x.Chance;
                        if (x.Weight.Length > 0) fields["Weight"] = x.Weight;
                        fields["MinCount"] = x.MinCount;
                        fields["MaxCount"] = x.MaxCount;
                        return new CfgWriterCheapStashItem
                        {
                            Node = $"[{x.Item}]",
                            Fields = fields
                        };
                    }).ToList()
            }).ToArray();
        if (categories.Any(c => c.Node != $"[{Array.IndexOf(categories, c)}]"
                || c.Items.Count == 0
                || c.Items.Select((item, i) => item.Node == $"[{i}]").Any(ok => !ok)))
            throw new InvalidDataException("CheapNoFresh category layout changed.");

        var effective = new StashEffectiveState(all.Values, modified.Keys, "Easy")
            .WithValues(stashPresence);

        // RGD-5: CheapNoFresh rolls it at 100% (Basic: 10%). The Basic CHANCE
        // cell keeps its ratio to the Basic vanilla value here, and supply
        // reduction applies once, like every other Chance row.
        var sourceGrenadeChance = ParseNumber(cheapConfig.GrenadeEntry.SourceChanceRaw);
        if (sourceGrenadeChance <= 0)
            throw new InvalidDataException("Missing Cheap stash original RGD-5 chance.");
        var grenadeChanceChanged = modified.ContainsKey(grenadeChanceId);
        var grenadeAmountChanged = modified.ContainsKey(grenadeAmountId);
        var grenadeFactor = grenadeChanceChanged
            ? StashTuningCurrent(all, grenadeChanceId, sourceGrenadeChance) / sourceGrenadeChance
            : 1.0;
        var grenadeCount = (int)Math.Round(StashTuningCurrent(all,
            grenadeAmountId, cheapConfig.GrenadeEntry.SourceMinCount),
            MidpointRounding.AwayFromZero);

        var output = new List<(CfgWriterCheapStashCategory Category,
            List<(string Node, List<(string Field, string Value)> Fields)> Items,
            string? Empty)>();
        var changed = false;
        foreach (var category in categories)
        {
            var weightEdit = effective.WeightListEdit(baseline, "Cheap", category);
            var items = new List<(string Node, List<(string Field, string Value)> Fields)>();
            for (var i = 0; i < category.Items.Count; i++)
            {
                var item = category.Items[i];
                var isGrenade = category.Node == cheapConfig.GrenadeEntry.CategoryNode
                    && item.Fields["ItemPrototypeSID"] == "GrenadeRGD5";
                var fields = new List<(string Field, string Value)>();
                foreach (var (field, raw) in item.Fields)
                {
                    string value;
                    if (field == "ItemPrototypeSID")
                        value = raw;
                    else if (field == "Weight" && weightEdit is { } edit)
                        value = edit.Weights[i];
                    else if (isGrenade)
                    {
                        var off = grenadeChanceChanged && grenadeFactor <= 0
                            || grenadeAmountChanged && grenadeCount == 0;
                        value = field switch
                        {
                            "Chance" => off ? FormatLikeSource(0, raw)
                                : FormatLikeSource(Math.Clamp(ParseNumber(raw) * grenadeFactor
                                    * effective.SupplyRemaining(), 0.0, 1.0), raw),
                            _ => off ? "0" : grenadeAmountChanged
                                ? grenadeCount.ToString(CultureInfo.InvariantCulture) : raw
                        };
                    }
                    else
                        value = effective.TierField(baseline, "Cheap", category, item, field);
                    if (value != raw) changed = true;
                    fields.Add((field, value));
                }
                items.Add((item.Node, fields));
            }
            if (weightEdit?.Empty is not null) changed = true;
            output.Add((category, items, weightEdit?.Empty));
        }

        // A NoFresh clone is needed only when its own authored source fields change.
        // Stash bonus gear independently appends rank-gated actor-local generators.
        if (!changed)
            return 0;

        // Full template (like the Basic tier clones), so an appended Empty
        // entry is a plain list element, never a partial patch.
        var doc = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        var root = doc.GetOrAddReferencedRoot(TcdCheapNoFreshSid,
            "../ItemGeneratorPrototypes.cfg", CheapNoFreshSid);
        root.SetAssignment("SID", TcdCheapNoFreshSid);
        var generator = root.GetOrAddChild("ItemGenerator", useBpatch: false);
        var edits = 1;
        foreach (var (category, items, empty) in output)
        {
            var categoryNode = generator.GetOrAddChild(category.Node, useBpatch: false);
            foreach (var (key, value) in category.Fields)
            {
                categoryNode.SetAssignment(key, value);
                edits++;
            }
            var possible = categoryNode.GetOrAddChild("PossibleItems", useBpatch: false);
            foreach (var (node, fields) in items)
            {
                var itemNode = possible.GetOrAddChild(node, useBpatch: false);
                foreach (var (field, value) in fields)
                {
                    itemNode.SetAssignment(field, value);
                    edits++;
                }
            }
            edits += WriteStashPresenceEmpty(possible, category, empty);
        }

        var actorsDoc = GetDocument(documents, DlcSmartLootActorFile);
        foreach (var actor in actors)
        {
            actorsDoc.AddAssignment(actor.ActorSid,
                new[] { "ItemGeneratorSettings", "[0]" },
                "PrototypeSID", TcdCheapNoFreshSid);
            edits++;
        }
        foreach (var id in relevant)
            written.Add(id);
        return edits;
    }

    private static readonly HashSet<string> CheapNoFreshActorIds =
        new(StringComparer.Ordinal)
        {
            "19EB12D645F5460C36D417AA5A224259",
            "4DA6EC814DF17F44E9DF15B54A3B4F35",
            "8F5078E346A2F3D27B238CAF2D72650C",
            "9C2717884C046D315A65DA88E49F310C",
            "F4AF1C714AB9FC51031FF1B0DD87110A",
            "F943F3C84EB1391403756181AF2AB692"
        };
}
