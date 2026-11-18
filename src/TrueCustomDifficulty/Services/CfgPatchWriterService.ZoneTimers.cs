using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// EMISSION WARNING (world emission stage timing) and TRADER RESTOCK (shared
// trade refresh conditions). See ZoneTimersPolicy.
public sealed partial class CfgPatchWriterService
{
    private static int WriteZoneTimers(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var edits = 0;

        if (modifiedById.TryGetValue(ZoneTimersPolicy.EmissionWarningId, out var warning)
            && warning.IsValueModifiedFromReference())
        {
            var seconds = Math.Round(warning.CurrentNumber);
            if (!double.IsFinite(seconds)
                || seconds < ZoneTimersPolicy.MinWarningSeconds
                || seconds > ZoneTimersPolicy.MaxWarningSeconds
                || ZoneTimersPolicy.EmissionEnd(seconds) > ZoneTimersPolicy.EmissionWeatherSeconds)
                throw new InvalidDataException("Invalid emission warning time.");
            var shift = ZoneTimersPolicy.Shift(seconds);
            var document = GetDocument(documents, ZoneTimersPolicy.EmissionFile);
            foreach (var (node, stage, start, duration) in ZoneTimersPolicy.Stages)
            {
                if (stage == "BeforeTheStorm")
                {
                    document.AddAssignment(ZoneTimersPolicy.EmissionDefault, new[] { "Stages", node },
                        "PhaseDuration", Seconds(seconds));
                    edits++;
                }
                else if (stage != "ActivateQuest")
                {
                    // Everything after the warning moves with it; durations stay.
                    document.AddAssignment(ZoneTimersPolicy.EmissionDefault, new[] { "Stages", node },
                        "PhaseStartTime", Seconds(start + shift));
                    edits++;
                }
            }
            // NPCs leave cover after the emission, not in the middle of it.
            document.AddAssignment(ZoneTimersPolicy.EmissionDefault,
                new[] { "AIEvents", ZoneTimersPolicy.AiEndNode },
                "AIEventStartTime", Seconds(ZoneTimersPolicy.VanillaAiEndSeconds + shift));
            edits++;
            writtenParameters.Add(ZoneTimersPolicy.EmissionWarningId);
        }

        if (modifiedById.TryGetValue(ZoneTimersPolicy.TraderRestockId, out var restock)
            && restock.IsValueModifiedFromReference())
        {
            var hours = (int)Math.Round(restock.CurrentNumber);
            if (hours < 1 || hours > 72)
                throw new InvalidDataException("Invalid trader restock time.");
            var document = GetDocument(documents, ZoneTimersPolicy.RestockFile);
            document.AddAssignment(ZoneTimersPolicy.NamedTraderCondition, Array.Empty<string>(),
                "Hours", hours.ToString(CultureInfo.InvariantCulture));
            edits++;
            var days = ZoneTimersPolicy.GenericRestockDays(hours);
            if (days != ZoneTimersPolicy.VanillaGenericRestockDays)
            {
                document.AddAssignment(ZoneTimersPolicy.GenericTraderCondition, Array.Empty<string>(),
                    "Days", days.ToString(CultureInfo.InvariantCulture));
                edits++;
            }
            writtenParameters.Add(ZoneTimersPolicy.TraderRestockId);
        }

        return edits;
    }

    private static string Seconds(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
}
