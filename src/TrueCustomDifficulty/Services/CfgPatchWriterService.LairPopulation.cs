using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// LAIR POPULATION — eligible MaxSpawnQuantity per faction / rank in
// LairPrototypes.cfg (base + DLC1), plus REFILL SPEED over the same entries'
// three refill timers. See LairPopulationPolicy.
public sealed partial class CfgPatchWriterService
{
    private static int WriteLairPopulation(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var edits = 0;
        foreach (var id in LairPopulationPolicy.PopulationIds)
        {
            if (!modifiedById.TryGetValue(id, out var parameter)
                || !parameter.IsValueModifiedFromReference())
                continue;
            var multiplier = parameter.CurrentNumber;
            if (!double.IsFinite(multiplier) || multiplier <= 0.0)
                throw new InvalidDataException("Invalid lair population multiplier: " + id);

            var group = LairPopulationPolicy.GroupOf(id);
            var changed = 0;
            foreach (var row in LairPopulationPolicy.Rows)
            {
                if (row.Group != group || !IsScopeApplicable(row.Scope, hasDlc1))
                    continue;
                var value = LairPopulationPolicy.Scale(row.SourceMax, multiplier);
                if (value == row.SourceMax)
                    continue;
                GetDocument(documents, row.File).AddAssignment(row.Prototype,
                    new[] { "Preset", "PossibleInhabitantFactions", row.FactionNode,
                        "SpawnSettingsPerPlayerRanks", row.Rank },
                    "MaxSpawnQuantity", value.ToString(CultureInfo.InvariantCulture));
                changed++;
            }

            // A step that rounds every count back to vanilla is not a change.
            if (changed == 0)
                throw new InvalidDataException(
                    "LAIR POPULATION " + group.ToUpperInvariant() + " does not change any lair after rounding. " +
                    "Move it further from 1× or reset it.");
            writtenParameters.Add(id);
            edits += changed;
        }

        if (modifiedById.TryGetValue(LairPopulationPolicy.RefillSpeedId, out var refill)
            && refill.IsValueModifiedFromReference())
        {
            var speed = refill.CurrentNumber;
            if (!double.IsFinite(speed) || speed <= 0.0)
                throw new InvalidDataException("Invalid lair refill speed.");
            var changed = 0;
            foreach (var row in LairPopulationPolicy.Rows)
            {
                if (!IsScopeApplicable(row.Scope, hasDlc1))
                    continue;
                var document = GetDocument(documents, row.File);
                for (var t = 0; t < LairPopulationPolicy.TimerKeys.Count; t++)
                {
                    var value = LairPopulationPolicy.ScaleTimer(row.SourceTimers[t], speed);
                    if (Math.Abs(value - row.SourceTimers[t]) < 0.0001)
                        continue;
                    document.AddAssignment(row.Prototype,
                        new[] { "Preset", "PossibleInhabitantFactions", row.FactionNode,
                            "SpawnSettingsPerPlayerRanks", row.Rank },
                        LairPopulationPolicy.TimerKeys[t],
                        value.ToString("0.0", CultureInfo.InvariantCulture));
                    changed++;
                }
            }
            if (changed == 0)
                throw new InvalidDataException(
                    "LAIR REFILL SPEED does not change any timer. Move it further from 1× or reset it.");
            writtenParameters.Add(LairPopulationPolicy.RefillSpeedId);
            edits += changed;
        }
        return edits;
    }
}
