using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// LAIR POPULATION — eligible MaxSpawnQuantity per faction / rank in
// LairPrototypes.cfg (base + DLC1). See LairPopulationPolicy.
public sealed partial class CfgPatchWriterService
{
    private static int WriteLairPopulation(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var edits = 0;
        foreach (var id in LairPopulationPolicy.ParameterIds)
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
        return edits;
    }
}
