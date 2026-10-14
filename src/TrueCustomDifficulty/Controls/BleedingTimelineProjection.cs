namespace TrueCustomDifficulty.Controls;

// The stage schedule is explicitly edited by the player. HP/TICK comes only from
// the real Low/Medium/Severe source values; 1 s per tick is an unverified cadence.
public sealed record BleedStageChange(int Second, string Stage);
public sealed record BleedTimelinePoint(int Second, double Hp, int Hits, string Stage);
public sealed record BleedStageSpan(
    string Stage, int FirstSecond, int LastSecond,
    int DamageTicks, double HpLost);
public sealed record BleedTimelineResult(
    IReadOnlyList<BleedTimelinePoint> Samples, int Hits, int? DeathSecond)
{
    public int Duration => Samples[^1].Second;
}

public static class BleedingTimelineProjection
{
    public static IReadOnlyList<BleedStageChange> SetStage(
        IReadOnlyList<BleedStageChange> existing, int fromSecond, string stage)
    {
        if (stage is not ("LOW" or "MEDIUM" or "SEVERE"))
            throw new ArgumentOutOfRangeException(nameof(stage));
        var entries = new SortedDictionary<int, string> { [1] = "LOW" };
        foreach (var change in existing)
        {
            if (change.Second is >= 1 and <= 3600
                && change.Stage is "LOW" or "MEDIUM" or "SEVERE")
                entries[change.Second] = change.Stage;
        }
        entries[Math.Clamp(fromSecond, 1, 3600)] = stage;

        // Collapse identical adjacent stages; later explicitly selected stages
        // of a different kind are preserved when an earlier tick is edited.
        var result = new List<BleedStageChange>();
        string? previous = null;
        foreach (var entry in entries)
        {
            if (entry.Value == previous)
                continue;
            result.Add(new BleedStageChange(entry.Key, entry.Value));
            previous = entry.Value;
        }
        return result;
    }

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

    // Actual modeled HP hits, never ornamental points.
    public static IReadOnlyList<BleedTimelinePoint> TimelineMarkers(BleedTimelineResult result)
    {
        var hits = new List<BleedTimelinePoint>();
        for (var i = 1; i < result.Samples.Count; i++)
            if (result.Samples[i].Hits > result.Samples[i - 1].Hits)
                hits.Add(result.Samples[i]);
        if (hits.Count <= 3)
            return hits;
        var marks = new List<BleedTimelinePoint> { hits[0] };
        for (var i = 1; i < hits.Count; i++)
            if (hits[i].Stage != hits[i - 1].Stage)
                marks.Add(hits[i]);
        if (marks.Count == 1)
            marks.Add(hits[(hits.Count - 1) / 2]);
        if (marks[^1].Second != hits[^1].Second)
            marks.Add(hits[^1]);
        return marks;
    }

    public static BleedTimelineResult Calculate(
        double maxHp, IReadOnlyList<BleedStageChange> stageChanges,
        double low, double medium, double severe)
    {
        var schedule = SetStage(stageChanges, 1,
            stageChanges.FirstOrDefault(c => c.Second == 1)?.Stage ?? "LOW");
        var hp = Math.Max(1.0, maxHp);
        var hits = 0;
        int? death = null;
        var points = new List<BleedTimelinePoint> { new(0, hp, 0, "NONE") };
        var stageIndex = 0;
        var stage = schedule[0].Stage;
        var idleTicks = 0;
        for (var sec = 1; sec <= 3600 && hp > 0; sec++)
        {
            while (stageIndex + 1 < schedule.Count && schedule[stageIndex + 1].Second <= sec)
                stage = schedule[++stageIndex].Stage;
            var amount = Math.Max(0.0, stage switch
            {
                "SEVERE" => severe,
                "MEDIUM" => medium,
                _ => low
            });
            if (amount > 0)
            {
                hits++;
                hp = Math.Max(0.0, hp - amount);
                idleTicks = 0;
            }
            else
                idleTicks++;
            points.Add(new BleedTimelinePoint(sec, hp, hits, stage));
            if (hp <= 0)
            {
                death = sec;
                break;
            }
            // Allow a zero-damage stage to remain editable for two minutes.
            // Do not invent natural stage decay or a death after damage stops.
            if (idleTicks >= 120 && stageIndex == schedule.Count - 1)
                break;
        }
        return new BleedTimelineResult(points, hits, death);
    }
}
