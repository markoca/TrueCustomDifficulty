using System.Globalization;
using System.IO;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Converts the A-Life Director's authored integer MaxCount rows into effective
/// multiplier stops. Every selectable stop has a distinct actual BPatch output.
/// Both the controls and preset imports use this same source-backed policy.
/// </summary>
public static class ALifeIntegerCapSteps
{
    private const double Epsilon = 0.000001;

    // Use the same embedded MaxCount edit points as CfgPatchWriterService. Do not
    // maintain a second hand-authored copy of per-rank/per-species source caps.
    private static readonly Lazy<IReadOnlyDictionary<string, int[]>> Source = new(() =>
        new CfgWriterBaselineService().Load().EditPoints
            .Where(p => p.ParameterId.StartsWith("alife.", StringComparison.Ordinal)
                && p.Operation is "scaleIntegerHalfUp" or "scaleIntegerHalfUpPreserveZero")
            .GroupBy(p => p.ParameterId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(p => p.NodePath[1], StringComparer.Ordinal)
                    .Select(p => int.Parse(p.SourceRaw, CultureInfo.InvariantCulture))
                    .ToArray(),
                StringComparer.Ordinal));

    public static bool Handles(string id) =>
        id.StartsWith("alife.humanGroupSize.", StringComparison.Ordinal)
        || (id.StartsWith("alife.mutantSpawn.", StringComparison.Ordinal)
            && id != "alife.mutantSpawn.generic");

    public static IReadOnlyList<int> EffectiveCaps(string id, double multiplier)
    {
        if (!Source.Value.TryGetValue(id, out var source))
            throw new InvalidDataException("Missing A-Life integer CFG source: " + id);

        // The writer's half-up integer conversion is authoritative. Native
        // zero entries (Newbie Bloodsucker/Controller) remain disabled.
        return source.Select(cap => cap == 0 ? 0
            : Math.Max(1, (int)Math.Floor(cap * multiplier + 0.5))).ToArray();
    }

    public static IReadOnlyList<double> EffectiveSteps(
        string id, double minimum, double maximum, double nativeStep)
    {
        if (!Handles(id))
            throw new ArgumentException("Not an editable A-Life cap: " + id, nameof(id));
        if (nativeStep <= Epsilon || maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(nativeStep));

        var grid = new List<double>();
        var count = (int)Math.Floor((maximum - minimum) / nativeStep + Epsilon);
        for (var n = 0; n <= count; n++)
            grid.Add(Math.Round(minimum + n * nativeStep, 9));
        if (grid[^1] < maximum - Epsilon)
            grid.Add(maximum);

        var outputs = grid.Select(value => EffectiveCaps(id, value)).ToArray();
        var stops = new List<double>();
        for (var first = 0; first < grid.Count;)
        {
            var last = first;
            while (last + 1 < grid.Count && outputs[last + 1].SequenceEqual(outputs[first]))
                last++;

            // Preserve the exact neutral 1x value even if a whole plateau
            // around it produces identical integer limits.
            var referenceIndex = grid.FindIndex(first, last - first + 1,
                value => Math.Abs(value - 1.0) < Epsilon);
            double stop;
            if (referenceIndex >= 0)
                stop = grid[referenceIndex];
            else if (last == grid.Count - 1)
                stop = grid[last]; // Keep the real maximum endpoint reachable.
            else
                stop = grid[first]; // First native 0.05x step that changes a cap.

            stops.Add(stop);
            first = last + 1;
        }

        return stops;
    }

    public static double Snap(
        string id, double requested, double minimum, double maximum, double nativeStep)
    {
        var clamped = Math.Clamp(requested, minimum, maximum);
        var index = Math.Round((clamped - minimum) / nativeStep, MidpointRounding.AwayFromZero);
        var candidate = Math.Clamp(minimum + index * nativeStep, minimum, maximum);
        var wanted = EffectiveCaps(id, candidate);
        foreach (var step in EffectiveSteps(id, minimum, maximum, nativeStep))
        {
            if (EffectiveCaps(id, step).SequenceEqual(wanted))
                return step;
        }

        throw new InvalidDataException("A-Life cap has no selectable effective step: " + id);
    }

    public static double Move(
        string id, double current, double minimum, double maximum, double nativeStep, int direction)
    {
        var steps = EffectiveSteps(id, minimum, maximum, nativeStep);
        var normalized = Snap(id, current, minimum, maximum, nativeStep);
        if (direction > 0)
            return steps.FirstOrDefault(step => step > normalized + Epsilon, steps[^1]);
        if (direction < 0)
            return steps.LastOrDefault(step => step < normalized - Epsilon, steps[0]);
        return normalized;
    }
}
