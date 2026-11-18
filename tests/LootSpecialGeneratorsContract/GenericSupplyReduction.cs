using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.Controls;
using TrueCustomDifficulty.Models;
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
        Check(Catalog.Parameters.Count == 452
            && Baseline.SupportedParameterCount == 452,
            "452 supported 2.1 sources / two scarcity masters drifted");
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
        smart.CurrentText = "NPC_Medicine_Smart, NPC_Ammo_Smart";
        npcBoard.Measure(new System.Windows.Size(920.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 470.0) < 0.000001,
            "NPC bonus card must sit beside weapon condition");
        npcBoard.Measure(new System.Windows.Size(860.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 470.0) < 0.000001,
            "NPC board must stay width-capped at medium width");
        npcBoard.Measure(new System.Windows.Size(600.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 568.0) < 0.000001,
            "Narrow NPC ammo mode must keep the two-column middle tier");
        smart.CurrentText = string.Empty;
        npcBoard.Measure(new System.Windows.Size(920.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 470.0) < 0.000001,
            "NPC NONE mode must collapse optional ammo controls");
        npcBoard.Measure(new System.Windows.Size(600.0, 1200.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 568.0) < 0.000001,
            "Narrow NPC NONE mode must collapse optional ammo controls");
        smart.CurrentText = initialChoice;

        // Weight lists: supply reduction keeps (1 - r) of the list's odds via
        // the appended native Empty pick (same rule as every ADJUST ODDS
        // cell); stack sizes stay vanilla (2026-11-16).
        var weighted = Baseline.Special.CheapStashProduction.TemplateCategories
            .SelectMany(x => x.Items).First(x => x.Fields.ContainsKey("Weight"));
        var sourceMin = weighted.Fields["MinCount"];
        var weightedItemState = new StashEffectiveState(
            new Dictionary<string, double> { [stashId] = .5 },
            new[] { stashId }, "Easy");
        var weightedCategory = Baseline.Special.CheapStashProduction.TemplateCategories
            .Single(x => x.Items.Contains(weighted));
        Check(weightedItemState.TierField(Baseline, "Cheap", weightedCategory,
                weighted, "MinCount") == sourceMin,
            "Generic stash master must not change Weight-list stack sizes");
        var halfEdit = weightedItemState.WeightListEdit(Baseline, "Cheap", weightedCategory);
        var sourceTotal = weightedCategory.Items.Sum(i =>
            StashEffectiveState.Number(i.Fields["Weight"]));
        Check(halfEdit is { } he && he.Empty is not null
                && Math.Abs(he.EmptyValue - sourceTotal / 2.0) < 0.000001
                && Math.Abs(he.Values.Sum() - sourceTotal / 2.0) < 0.000001,
            "Generic stash master must move half of a Weight list's odds to Empty");
        var chanceRow = Baseline.Special.CheapStashProduction.TemplateCategories
            .SelectMany(c => c.Items.Where(i => i.Fields.ContainsKey("Chance"))
                .Select(i => (Category: c, Item: i))).First();
        Check(Math.Abs(StashEffectiveState.Number(
            weightedItemState.TierField(Baseline, "Cheap",
                chanceRow.Category, chanceRow.Item, "Chance"))
            - StashEffectiveState.Number(chanceRow.Item.Fields["Chance"]) / 2.0)
            < 0.000001, "Chance-only stash row was not reduced after source tuning");

        // A non-vanilla package subset (Medicine + Water) is a valid native
        // list: the 50% master clones and links exactly those two packages.
        {
            var combo = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var choice = combo.AllParameters.Single(p => p.Id == "loot.npcCorpseBonus");
            choice.CurrentText = NpcCorpsePackages.Compose(new[]
                { NpcCorpsePackages.Water, NpcCorpsePackages.Medicine });
            Check(choice.CurrentText == "NPC_Medicine_Smart, NPC_Water_Smart"
                && choice.ChoiceOptions.Any(o => o.Value == choice.CurrentText),
                "Medicine + Water must be a canonical, selectable package subset");
            combo.AllParameters.Single(p => p.Id == corpseId).SnapCurrentNumberToMarker(.5);
            var comboDir = Path.Combine(Path.GetTempPath(), "TCD_GenericSupplyReduction", "combo");
            try
            {
                if (Directory.Exists(comboDir)) Directory.Delete(comboDir, true);
                new CfgPatchWriterService().Write(combo, Catalog, null, comboDir);
                var economy = ReadOutput(comboDir)["GameData/DifficultyPrototypes.cfg"];
                Check(economy.Any(kv => kv.Key.EndsWith("CorpseSmartLoot/GeneralNPC", StringComparison.Ordinal)
                        && kv.Value == "TCD_Supply_NPC_Medicine_Smart, TCD_Supply_NPC_Water_Smart"),
                    "Medicine + Water subset was not linked through the reduced clones");
            }
            finally
            {
                if (Keep is null && Directory.Exists(comboDir)) Directory.Delete(comboDir, true);
            }
        }

        // NEED BOOST stretches only the need-driven span of the selected
        // packages: Max = clamp(0.1 + (vanillaMax - 0.1) * boost), then the
        // corpse master scales Min and Max. 1x emits nothing.
        foreach (var (boost, reductionValue, expectedMax, expectedMin) in new[]
        {
            (0.0, 0.0, 0.1, 0.1),
            (2.0, 0.0, 1.0, 0.1),
            (1.5, 0.5, (0.1 + 0.6 * 1.5) * 0.5, 0.05),
            (1.0, 0.0, double.NaN, double.NaN)
        })
        {
            var boostVm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Novice" };
            boostVm.CorpseSmartLootNeedBoost = boost;
            if (reductionValue > 0.0)
                boostVm.AllParameters.Single(p => p.Id == corpseId).SnapCurrentNumberToMarker(reductionValue);
            var boostDir = Path.Combine(Path.GetTempPath(), "TCD_GenericSupplyReduction",
                "boost-" + boost.ToString(CultureInfo.InvariantCulture) + "-" + reductionValue.ToString(CultureInfo.InvariantCulture));
            try
            {
                if (Directory.Exists(boostDir)) Directory.Delete(boostDir, true);
                var boostWrite = new CfgPatchWriterService().Write(boostVm, Catalog, null, boostDir);
                var boostOutput = ReadOutput(boostDir);
                if (double.IsNaN(expectedMax))
                {
                    Check(boostWrite.EditCount == 0 && !boostOutput.ContainsKey("GameData/StashPrototypes.cfg"),
                        "NEED BOOST 1x must not emit any SmartLoot clone");
                    continue;
                }
                var boostSmart = boostOutput["GameData/StashPrototypes.cfg"];
                var boostGroup = "TCD_Supply_NPC_Medicine_Smart/ItemGenerators/[0]/SmartLootParams/ConsumablesParams/[0]/";
                Check(boostSmart.TryGetValue(boostGroup + "MaxSpawnChance", out var boostMax)
                    && Math.Abs(Num(boostMax!) - expectedMax) < 0.000001
                    && boostSmart.TryGetValue(boostGroup + "MinSpawnChance", out var boostMin)
                    && Math.Abs(Num(boostMin!) - expectedMin) < 0.000001,
                    "NEED BOOST endpoints wrong at " + boost + "x / reduction " + reductionValue);
                var boostEconomy = boostOutput["GameData/DifficultyPrototypes.cfg"];
                Check(boostEconomy.Any(kv => kv.Key.EndsWith("CorpseSmartLoot/GeneralNPC", StringComparison.Ordinal)
                        && kv.Value == "TCD_Supply_NPC_Medicine_Smart, TCD_Supply_NPC_Ammo_Smart"),
                    "NEED BOOST did not link both selected boosted packages");
                Check(boostWrite.ParameterCount == (reductionValue > 0.0 ? 2 : 1),
                    "NEED BOOST must count as one logical editor");
            }
            finally
            {
                if (Keep is null && Directory.Exists(boostDir)) Directory.Delete(boostDir, true);
            }
        }

        // Sidecar persistence: omitted = 1x, saved value round-trips.
        var boostPreset = Path.Combine(Path.GetTempPath(), "TCD_NeedBoost_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var boostFile = new PresetFile
            {
                PresetFormatVersion = PresetService.CurrentPresetVersion,
                ReferenceDifficulty = "Veteran",
                CorpseSmartLootNeedBoost = 1.35
            };
            new PresetService().Save(boostFile, boostPreset);
            var boostBack = new PresetService().Load(boostPreset);
            Check(boostBack.CorpseSmartLootNeedBoost is double boostLoaded && Math.Abs(boostLoaded - 1.35) < 0.000001,
                "NEED BOOST preset sidecar did not round-trip");
            Check(new PresetFile().CorpseSmartLootNeedBoost is null,
                "Old presets must omit NEED BOOST (loads as 1x)");
        }
        finally
        {
            if (File.Exists(boostPreset)) File.Delete(boostPreset);
        }

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
                            var smartStash = output["GameData/StashPrototypes.cfg"];
                            var spawnKey = "TCD_Supply_NPC_Medicine_Smart"
                                + "/ItemGenerators/[0]/SmartLootParams/"
                                + "ConsumablesParams/[0]/MaxSpawnChance";
                            Check(smartStash.TryGetValue(spawnKey, out var endpoint)
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
                            && Math.Abs(Num(weight!) - Num(item.Fields["Weight"])
                                * (1.0 - reduction)) < 0.0001,
                            "Stash master did not keep (1 - r) of the Weight odds");
                        var emptyKey = cheap.CustomGeneratorSid + "/ItemGenerator/"
                            + group.Node + "/PossibleItems/["
                            + group.Items.Count.ToString(CultureInfo.InvariantCulture)
                            + "]/ItemPrototypeSID";
                        Check(itemgen.TryGetValue(emptyKey, out var empty) && empty == "Empty",
                            "Stash master did not append the native Empty pick");
                        Check(itemgen.TryGetValue(prefix + "MinCount", out var minCount)
                            && minCount == item.Fields["MinCount"],
                            "Stash master must keep Weight-list stack sizes");
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
