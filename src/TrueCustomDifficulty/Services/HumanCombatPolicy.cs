using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// HUMAN TACTICS and HUMAN AWARENESS source map (2.1 base dump,
/// docs/combat/HUMAN-TACTICS-AND-AWARENESS.md).
///
/// TACTICS scale group token cooldowns in <c>CombatSynchronizationPrototypes.cfg</c>
/// (the same rank tables as SPECIAL ABILITY RATE): Easy/Medium/Hard inherit
/// <c>Default</c>, Master (<c>Stalker</c>) owns a table with about half the
/// pauses. Rate r writes source × reference / r, so the difficulty's own table
/// is reproduced exactly at its vanilla rate (2 on Master, 1 elsewhere).
///
/// ACCURACY divides <c>DispersionRadius</c> of all 77 NPC weapon settings;
/// SIGHT / HEARING / MEMORY scale the human vision scanner, the base hearing
/// sensor and the DefaultNPC threat memory (direct edit points).
/// FOG VISIBILITY changes only the global Fogy weather VisibilityCoef in AIGlobals.cfg.
/// Affects all AI consumers of that global weather setting; runtime validation pending.
/// </summary>
public static class HumanCombatPolicy
{
    public const string GrenadeRateId = "combat.npcGrenadeRate";
    public const string SuppressionRateId = "combat.npcSuppressionRate";
    public const string FlankRateId = "combat.npcFlankRate";
    public const string PushRateId = "combat.npcPushRate";
    public const string CamperPunishRateId = "combat.npcCamperPunishRate";
    public const string AccuracyId = "combat.npcAccuracy";
    public const string SightRangeId = "combat.npcSightRange";
    public const string HearingRangeId = "combat.npcHearingRange";
    public const string AlertMemoryId = "combat.npcAlertMemory";
    public const string FogVisibilityId = "combat.npcFogVisibility";
    public const string RainNoiseId = "combat.npcRainNoise";
    public const string StormNoiseId = "combat.npcStormNoise";

    /// <param name="Groups">FilterGroups index and its AllowedTags[0] (guarded on load).</param>
    /// <param name="DefaultSeconds">Newbie-rank Default pause (display).</param>
    /// <param name="MasterSeconds">Newbie-rank Master-table pause (display).</param>
    public sealed record TacticControl(
        string Id, string Label, string Verb,
        IReadOnlyList<(string Index, string Tag)> Groups,
        bool MasterHalves,
        (double Min, double Max) DefaultSeconds,
        (double Min, double Max) MasterSeconds);

    public static IReadOnlyList<TacticControl> Tactics { get; } = new TacticControl[]
    {
        new(GrenadeRateId, "GRENADES", "throws a grenade",
            new[] { ("[9]", "TokenTag.CombatAction.ThrowGrenade"), ("[16]", "TokenTag.CombatAction.Special.ThrowGrenade") },
            true, (20, 35), (10, 17)),
        new(SuppressionRateId, "SUPPRESSION", "pins you down",
            new[] { ("[10]", "TokenTag.CombatAction.SuppressiveFire") },
            true, (15, 25), (7, 12)),
        new(FlankRateId, "FLANKING", "flanks you",
            new[] { ("[5]", "TokenTag.CombatAction.Flank") },
            true, (10, 20), (5, 10)),
        new(PushRateId, "PUSH", "rushes your cover",
            new[] { ("[6]", "TokenTag.CombatAction.Advance.Action"), ("[7]", "TokenTag.CombatAction.Advance.Approach") },
            true, (12, 25), (6, 12)),
        new(CamperPunishRateId, "CAMPING PENALTY", "flushes you out",
            new[] { ("[8]", "TokenTag.CamperPunish") },
            false, (2, 5), (2, 5))
    };

    public static IReadOnlyList<string> TacticIds { get; } = Tactics.Select(t => t.Id).ToArray();

    public static IReadOnlyList<string> AwarenessIds { get; } =
        new[] { SightRangeId, HearingRangeId, AlertMemoryId, FogVisibilityId, RainNoiseId, StormNoiseId };

    /// <summary>Board order of the HUMAN TACTICS cards (tactics, then accuracy).</summary>
    public static IReadOnlyList<string> TacticsBoardIds { get; } = TacticIds.Append(AccuracyId).ToArray();

    public static double Reference(TacticControl control, string difficultySid)
        => difficultySid == "Stalker" && control.MasterHalves ? 2.0 : 1.0;

    /// <summary>Source-accurate cooldown range for the selected reference difficulty and current rate.</summary>
    public static (double Min, double Max) CooldownSeconds(TacticControl control, double rate, double reference)
    {
        var (min, max) = reference > 1.0 ? control.MasterSeconds : control.DefaultSeconds;
        var factor = reference / Math.Max(rate, 0.0000001);
        return (min * factor, max * factor);
    }

    /// <summary>Panel text, e.g. "2x · throws a grenade every 10–17 s" (cards drop the factor).</summary>
    public static string TacticDisplay(TacticControl control, double rate, double reference)
    {
        var (min, max) = CooldownSeconds(control, rate, reference);
        return string.Create(CultureInfo.InvariantCulture,
            $"{rate:0.##}× · {control.Verb} every {min:0.#}–{max:0.#} s");
    }

    /// <summary>
    /// Card text without the slider factor: the concrete result only
    /// ("throws a grenade every 20–35 s", "spots you 80 m ahead, 25 m aside"). The factor is not a
    /// game value by itself; the selected-control panel still shows it.
    /// Null for ids outside HUMAN TACTICS / HUMAN AWARENESS.
    /// </summary>
    public static string? CardText(string id, double value, double reference)
    {
        var full = id switch
        {
            AccuracyId => AccuracyDisplay(value),
            SightRangeId => SightDisplay(value),
            HearingRangeId => HearingDisplay(value),
            AlertMemoryId => MemoryDisplay(value),
            FogVisibilityId => FogVisibilityDisplay(value),
            RainNoiseId => WeatherNoiseDisplay("rain", value),
            StormNoiseId => WeatherNoiseDisplay("thunder", value),
            _ when Tactics.FirstOrDefault(t => t.Id == id) is { } tactic => TacticDisplay(tactic, value, reference),
            _ => null
        };
        return full is null ? null : WithoutFactor(full);
    }

    /// <summary>Drops a leading "1.4× · " factor from a readout.</summary>
    public static string WithoutFactor(string display)
    {
        var dot = display.IndexOf(" · ", StringComparison.Ordinal);
        return dot > 0 && display[..dot].EndsWith('×') ? display[(dot + 3)..] : display;
    }

    // ---- display helpers for HUMAN AWARENESS / ACCURACY ------------------------
    public static string AccuracyDisplay(double factor)
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}× · shot spread {100.0 / factor:0}%");

    public static string SightDisplay(double factor)
        // Central vision (straight ahead) and peripheral vision (edge of view).
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}× · spots you {80.0 * factor:0} m ahead, {25.0 * factor:0} m aside");

    public static string HearingDisplay(double factor)
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}× · hears steps at {12.0 * factor:0.#} m");

    public static string MemoryDisplay(double factor)
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}× · searches for {30.0 * factor:0} s");

    // Persist native hearing coefficients; display their complementary masking.
    public static bool IsWeatherNoise(string id) => id is RainNoiseId or StormNoiseId;

    public static double NoiseMaskingPercent(double hearingCoefficient)
        => Math.Clamp((1.0 - hearingCoefficient) * 100.0, 0.0, 100.0);

    public static string WeatherNoiseDisplay(string weather, double hearingCoefficient)
        => string.Create(CultureInfo.InvariantCulture,
            $"{weather} noise {NoiseMaskingPercent(hearingCoefficient):0}%");

    public static string FogVisibilityDisplay(double coefficient)
        => string.Create(CultureInfo.InvariantCulture, $"fog visibility {coefficient * 100.0:0}%");
}
