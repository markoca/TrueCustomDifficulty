using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.Controls;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    private static void VerifyGenericSupplyReduction()
    {
        var pristine = new MainViewModel(Catalog);
        var corpseId = "loot.genericCorpseReduction";
        var stashId = "loot.genericStashReduction";
        var corpse = pristine.AllParameters.Single(p => p.Id == corpseId);
        var stash = pristine.AllParameters.Single(p => p.Id == stashId);
        Check(Catalog.Parameters.Count == 361
            && Baseline.SupportedParameterCount == 361,
            "361 supported 2.1 sources / two scarcity masters drifted");
        foreach (var p in new[] { corpse, stash })
            Check(p.Minimum == 0 && p.Maximum == 1
                && p.TickFrequency == 0.05
                && p.ReferenceNumber == 0 && p.CurrentNumber == 0
                && !p.IsValueModifiedFromReference(),
                "Generic supply reduction changed its zero-default contract: " + p.Id);
        var npcBoard = new AggregateVisualControl
        {
            Family = "NpcLootLoadout", Parameters = pristine.AllParameters,
            NpcConsumables = pristine.NpcConsumableItems
        };
        var smart = pristine.AllParameters.Single(p => p.Id == "loot.npcCorpseBonus");
        var initialChoice = smart.CurrentText;
        smart.CurrentText = smart.ChoiceOptions.Single(x =>
            x.Value.Contains("NPC_Ammo_Smart", StringComparison.Ordinal)).Value;
        npcBoard.Measure(new System.Windows.Size(920.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 470.0) < 0.000001,
            "NPC bonus card must sit beside weapon condition");
        npcBoard.Measure(new System.Windows.Size(860.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 470.0) < 0.000001,
            "NPC board must stay width-capped at medium width");
        npcBoard.Measure(new System.Windows.Size(600.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 568.0) < 0.000001,
            "Narrow NPC ammo mode must keep the two-column middle tier");
        smart.CurrentText = smart.ChoiceOptions.Single(x =>
            !x.Value.Contains("NPC_Ammo_Smart", StringComparison.Ordinal)
            && !x.Value.Contains("NPC_Water_Smart", StringComparison.Ordinal)).Value;
        npcBoard.Measure(new System.Windows.Size(920.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 470.0) < 0.000001,
            "NPC NONE mode must collapse optional ammo controls");
        npcBoard.Measure(new System.Windows.Size(600.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 568.0) < 0.000001,
            "Narrow NPC NONE mode must collapse optional ammo controls");
        smart.CurrentText = initialChoice;

        // The third native field is not a fictitious group chance.
        var weighted = Baseline.Special.CheapStashProduction.TemplateCategories
            .SelectMany(x => x.Items).First(x => x.Fields.ContainsKey("Weight"));
        var sourceWeight = weighted.Fields["Weight"];
        var sourceMin = weighted.Fields["MinCount"];
        var weightedItemState = new StashEffectiveState(
            new Dictionary<string, double> { [stashId] = .5 },
            new[] { stashId }, "Easy");
        var weightedCategory = Baseline.Special.CheapStashProduction.TemplateCategories
            .Single(x => x.Items.Contains(weighted));
        Check(weightedItemState.TierField(Baseline, "Cheap", weightedCategory,
                weighted, "Weight") == sourceWeight,
            "Generic stash master changed native relative Weight distribution");
        var reducedMin = weightedItemState.TierField(Baseline, "Cheap",
            weightedCategory, weighted, "MinCount");
        Check(int.Parse(reducedMin, CultureInfo.InvariantCulture)
                == Math.Max(1, (int)Math.Floor(
                    int.Parse(sourceMin, CultureInfo.InvariantCulture) * .5 + .5)),
            "Generic stash master did not reduce native weighted count");
        var chanceRow = Baseline.Special.CheapStashProduction.TemplateCategories
            .SelectMany(c => c.Items.Where(i => i.Fields.ContainsKey("Chance"))
                .Select(i => (Category: c, Item: i))).First();
        Check(Math.Abs(StashEffectiveState.Number(
            weightedItemState.TierField(Baseline, "Cheap",
                chanceRow.Category, chanceRow.Item, "Chance"))
            - StashEffectiveState.Number(chanceRow.Item.Fields["Chance"]) / 2.0)
            < 0.000001, "Chance-only stash row was not reduced after source tuning");

        // Assert cloned source output at both native endpoints, with and
        // without separately edited optional CorpseSmartLoot packages.
        foreach (var reduction in new[] { 0.5, 1.0 })
        {
            foreach (var area in new[] { "corpse", "stash" })
            {
                var vm = new MainViewModel(Catalog)
                { SelectedReferenceDifficulty = area == "corpse" ? "Novice" : "Veteran" };
                var id = area == "corpse" ? corpseId : stashId;
                vm.AllParameters.Single(p => p.Id == id)
                    .SnapCurrentNumberToMarker(reduction);
                var dir = Path.Combine(Path.GetTempPath(),
                    "TCD_GenericSupplyReduction", area + "-" + reduction);
                try
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                    var write = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                    var output = ReadOutput(dir);
                    Check(write.EditCount > 0 && write.ParameterCount == 1,
                        "Generic supply control did not write concrete source CFG: " + area);
                    AssertProtectedBaseStashesUntouched(output, area + reduction);
                    if (area == "corpse")
                    {
                        Check(!output.ContainsKey("GameData/SpawnActorPrototypes.cfg"),
                            "Corpse supply reduction changed physical world actors");
                        var poolFile = output["GameData/ItemGeneratorPrototypes.cfg"];
                        var sourcePool = Baseline.Special.NpcMedicalLootProduction.Pools[0];
                        var sampleCategory = sourcePool.TemplateCategories[0];
                        var sampleItem = sampleCategory.Items[0];
                        var key = sourcePool.CustomSid + "/ItemGenerator/"
                            + sampleCategory.Node + "/PossibleItems/"
                            + sampleItem.Node + "/Chance";
                        var expected = StashEffectiveState.Number(
                            sampleItem.Fields["Chance"]) * (1.0 - reduction);
                        Check(poolFile.TryGetValue(key, out var actual)
                            && Math.Abs(Num(actual!) - expected) < 0.000001,
                            "Corpse master failed to reduce raw item Chance last");
                        var economy = output["GameData/DifficultyPrototypes.cfg"];
                        if (reduction == 1.0)
                            Check(economy.Any(kv => kv.Key.EndsWith(
                                    "CorpseSmartLoot", StringComparison.Ordinal)
                                && kv.Value == ""),
                                "100% corpse reduction did not remove optional bonus package");
                        else
                        {
                            var smart = output["GameData/StashPrototypes.cfg"];
                            var spawnKey = "TCD_Supply_NPC_Medicine_Smart"
                                + "/ItemGenerators/[0]/SmartLootParams/"
                                + "ConsumablesParams/[0]/MaxSpawnChance";
                            Check(smart.TryGetValue(spawnKey, out var endpoint)
                                && Math.Abs(Num(endpoint!) - .7 * .5) < 0.000001,
                                "Corpse master did not scale optional medical spawn endpoint");
                        }
                    }
                    else
                    {
                        Check(!output.ContainsKey("GameData/DifficultyPrototypes.cfg"),
                            "Stash reduction modified NPC bonus package selector");
                        var itemgen = output["GameData/ItemGeneratorPrototypes.cfg"];
                        var cheap = Baseline.Special.CheapStashProduction;
                        var group = cheap.TemplateCategories.First(c =>
                            c.Items.Any(i => i.Fields.ContainsKey("Weight")));
                        var item = group.Items.First(i => i.Fields.ContainsKey("Weight"));
                        var prefix = cheap.CustomGeneratorSid + "/ItemGenerator/"
                            + group.Node + "/PossibleItems/" + item.Node + "/";
                        Check(itemgen.TryGetValue(prefix + "Weight", out var weight)
                            && (reduction < 1.0 ? weight == item.Fields["Weight"]
                                : Math.Abs(Num(weight!)) < 0.000001),
                            "Stash master changed original Weight choice ratios");
                        Check(itemgen.TryGetValue(prefix + "MinCount", out var minCount)
                            && (reduction < 1.0 ? int.Parse(minCount!) >= 1 :
                                minCount == "0"),
                            "Stash master did not bound native group count");
                        var worldFile = output["GameData/StashPrototypes.cfg"];
                        var endpointKey = "TCD_StashMedicine_Cheap"
                            + "/ItemGenerators/[0]/SmartLootParams/"
                            + "ConsumablesParams/[0]/MaxSpawnChance";
                        Check(worldFile.TryGetValue(endpointKey, out var endpoint)
                            && Math.Abs(Num(endpoint!) - (1.0 - reduction))
                                < 0.000001,
                            "Stash master failed to scale native world SmartLoot group gate");
                    }
                }
                finally
                {
                    if (Keep is null && Directory.Exists(dir))
                        Directory.Delete(dir, recursive: true);
                }
            }
        }

        // The new source masters apply after advanced Chance values and
        // do not change the saved per-item definition.
        var mixed = new MainViewModel(Catalog)
        { SelectedReferenceDifficulty = "Veteran" };
        mixed.AllParameters.Single(p => p.Id == corpseId).SnapCurrentNumberToMarker(.5);
        var bandage = mixed.NpcConsumableItems.Single(p => p.Sid == "Bandage");
        bandage.Enabled = true;
        bandage.Multiplier = 2.0;
        var mixedDir = Path.Combine(Path.GetTempPath(), "TCD_GenericSupplyReduction", "mixed");
        try
        {
            if (Directory.Exists(mixedDir)) Directory.Delete(mixedDir, true);
            var built = new CfgPatchWriterService().Write(mixed, Catalog, null, mixedDir);
            Check(built.ParameterCount == 2,
                "Corpse master + per-item editor were not independently counted");
            var output = ReadOutput(mixedDir)["GameData/ItemGeneratorPrototypes.cfg"];
            var pool = Baseline.Special.NpcMedicalLootProduction.Pools[0];
            var candidate = pool.TemplateCategories
                .SelectMany(c => c.Items.Select(i => (c, i)))
                .First(z => z.i.Fields["ItemPrototypeSID"] == "Bandage");
            var key = pool.CustomSid + "/ItemGenerator/"
                + candidate.c.Node + "/PossibleItems/" + candidate.i.Node + "/Chance";
            var expectedChance = Math.Clamp(Num(candidate.i.Fields["Chance"]) * 2.0, 0.0, 1.0) * .5;
            Check(output.TryGetValue(key, out var chance)
                && Math.Abs(Num(chance!) - expectedChance) < 0.000001
                && Math.Abs(bandage.Multiplier - 2.0) < 0.000001,
                "Corpse master did not apply AFTER the per-SID Chance multiplier");
        }
        finally
        {
            if (Keep is null && Directory.Exists(mixedDir))
                Directory.Delete(mixedDir, true);
        }
        Console.WriteLine("PASS two generic supply masters: 92 human sources, "
            + "three guarded tiers, world SmartLoot endpoints, zero output and UI heights");
    }
}
