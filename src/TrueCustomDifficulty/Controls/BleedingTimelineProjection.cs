namespace TrueCustomDifficulty.Controls;

// Read-only source-tick preview for the chosen bleeding stage.
// HP/TICK is taken directly from the authored profile. A 1-second cadence is
// displayed for comparison only; actual engine tick timing is unverified.
public sealed record BleedTickMark(int Number, double Seconds, double HpLost);
public sealed record BleedTimelineResult(
    string Stage, double HpPerTick, double StartingHp,
    IReadOnlyList<BleedTickMark> DisplayTicks, long? TicksToZero,
    double PreviewHpLost)
{
    public bool HasHpDamage => HpPerTick > 0;
}

public static class BleedingTimelineProjection
{
    // The timeline never chooses a stage based on guessed wound points or
    // turns into a draggable source. The player selects LOW/MEDIUM/SEVERE in
    // separate compact chips; these three displayed ticks are read-only.
    public static BleedTimelineResult Calculate(double maxHp, string stage,
        double low, double medium, double severe)
    {
        if (stage is not ("LOW" or "MEDIUM" or "SEVERE"))
            throw new ArgumentOutOfRangeException(nameof(stage));
        var hp = Math.Max(1.0, maxHp);
        var damage = Math.Max(0.0, stage switch
        {
            "SEVERE" => severe,
            "MEDIUM" => medium,
            _ => low
        });
        if (damage == 0.0)
            return new BleedTimelineResult(stage, 0.0, hp,
                Array.Empty<BleedTickMark>(), null, 0.0);

        var lethalCount = (long)Math.Ceiling(hp / damage);
        var count = (int)Math.Min(3, lethalCount);
        var marks = new List<BleedTickMark>(count);
        double previousHpLost = 0.0;
        for (var tick = 1; tick <= count; tick++)
        {
            var accumulated = Math.Min(hp, damage * tick);
            marks.Add(new BleedTickMark(tick, tick, accumulated - previousHpLost));
            previousHpLost = accumulated;
        }
        return new BleedTimelineResult(stage, damage, hp, marks, lethalCount,
            previousHpLost);
    }
}
