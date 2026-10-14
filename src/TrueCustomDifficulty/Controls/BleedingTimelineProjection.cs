namespace TrueCustomDifficulty.Controls;

// Conditional 1 s/charge projection; engine cadence not independently verified.
public sealed record BleedTimelinePoint(int Second, double Hp, double Bleed, int Hits, string Stage);
public sealed record BleedTimelineResult(
    IReadOnlyList<BleedTimelinePoint> Samples,
    int Hits, int? DeathSecond, int? BleedEndsSecond)
{
    public int Duration => Samples[^1].Second;
}

public static class BleedingTimelineProjection
{
    // Three representative real HP-damage charges, never fake 1/2/3 hits.
    // Calculations still iterate the complete per-second sample sequence.
    public static IReadOnlyList<BleedTimelinePoint> TimelineMarkers(BleedTimelineResult result)
    {
        var hits = new List<BleedTimelinePoint>();
        for (var i = 1; i < result.Samples.Count; i++)
        {
            if (result.Samples[i].Hits > result.Samples[i - 1].Hits)
                hits.Add(result.Samples[i]);
        }
        if (hits.Count <= 3)
            return hits;
        return new[] { hits[0], hits[(hits.Count - 1) / 2], hits[^1] };
    }

    // Damage first, natural bleed-point recovery after each hypothetical tick.
    // Does not simulate healing, HP regen, armor, or future bleeding infliction.
    public static BleedTimelineResult Calculate(
        double maxHp, double startHpPercent, double startBleed,
        double recovery, double low, double medium, double severe,
        double mediumThreshold, double severeThreshold)
    {
        var hp = Math.Max(1, maxHp) * Math.Clamp(startHpPercent, 0, 100) / 100;
        var bleed = Math.Clamp(startBleed, 0, 100);
        var regen = Math.Max(0, recovery);
        var midAt = Math.Clamp(mediumThreshold, 0.1, 100);
        var highAt = Math.Clamp(severeThreshold, midAt, 100);
        var hits = 0;
        int? death = null;
        int? ends = bleed < 0.1 ? 0 : null;
        var points = new List<BleedTimelinePoint>
        {
            new(0, hp, bleed, 0, "NONE")
        };
        for (var sec = 1; sec <= 3600 && hp > 0 && bleed >= 0.1; sec++)
        {
            var stage = bleed >= highAt ? "SEVERE"
                : bleed >= midAt ? "MEDIUM" : "LOW";
            var amount = Math.Max(0, stage == "SEVERE" ? severe
                : stage == "MEDIUM" ? medium : low);
            if (amount > 0)
            {
                hits++;
                hp = Math.Max(0, hp - amount);
            }
            bleed = Math.Max(0, bleed - regen);
            points.Add(new(sec, hp, bleed, hits, stage));
            if (hp <= 0)
            {
                death = sec;
                break;
            }
            if (bleed < 0.1)
            {
                ends = sec;
                break;
            }
            if (regen == 0 && amount == 0)
                break; // No damage or decay at the unchanged stage.
        }
        return new BleedTimelineResult(points, hits, death, ends);
    }
}
