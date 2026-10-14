namespace TrueCustomDifficulty.Controls;

// Conditional 1 s/charge projection; engine cadence not independently verified.
public sealed record BleedTimelinePoint(int Second, double Hp, double Bleed, int Hits, string Stage);
public sealed record BleedStageSpan(
    string Stage, int FirstSecond, int LastSecond,
    int DamageTicks, double HpLost);

public sealed record BleedTimelineResult(
    IReadOnlyList<BleedTimelinePoint> Samples,
    int Hits, int? DeathSecond, int? BleedEndsSecond)
{
    public int Duration => Samples[^1].Second;
}

public static class BleedingTimelineProjection
{
    // Stage spans follow the same real per-second projection as death counts.
    // No decorative equal-width phases: boundaries move with recovery and PTS.
    public static IReadOnlyList<BleedStageSpan> StageSegments(BleedTimelineResult result)
    {
        var spans = new List<BleedStageSpan>();
        if (result.Samples.Count < 2)
            return spans;
        var stage = result.Samples[1].Stage;
        var start = 1;
        var hits = 0;
        var lostHp = 0.0;
        for (var i = 1; i < result.Samples.Count; i++)
        {
            var now = result.Samples[i];
            if (now.Stage != stage)
            {
                spans.Add(new BleedStageSpan(stage, start, now.Second - 1, hits, lostHp));
                stage = now.Stage;
                start = now.Second;
                hits = 0;
                lostHp = 0.0;
            }
            var before = result.Samples[i - 1];
            hits += now.Hits - before.Hits;
            lostHp += Math.Max(0, before.Hp - now.Hp);
        }
        spans.Add(new BleedStageSpan(stage, start, result.Samples[^1].Second, hits, lostHp));
        return spans;
    }

    // Only real hits: first hit in each damaging stage and the final hit.
    // A single-stage bleed also gets a central landmark as in Starvation.
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
        var marks = new List<BleedTimelinePoint> { hits[0] };
        for (var i = 1; i < hits.Count; i++)
        {
            if (hits[i].Stage != hits[i - 1].Stage)
                marks.Add(hits[i]);
        }
        if (marks.Count == 1)
            marks.Add(hits[(hits.Count - 1) / 2]);
        if (marks[^1].Second != hits[^1].Second)
            marks.Add(hits[^1]);
        return marks;
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
