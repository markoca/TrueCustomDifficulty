using System;
using System.Collections.Generic;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// ZONE WEATHER and TRADER RESTOCK source map (2.1 base dump,
/// docs/ZONE-WEATHER-AND-RESTOCK.md).
///
/// EMISSIONS / STORMS are direct edit points (DifficultyPrototypes.cfg
/// EnvironmentDifficulty.Weather_Emission_Weight / Weather_Storm_Weight per
/// difficulty). EMISSION WARNING and TRADER RESTOCK need coupled edits and
/// are written by CfgPatchWriterService.ZoneTimers.cs from the constants
/// below (audited against the dump by tools/audit_zone_timers.py).
/// </summary>
public static class ZoneTimersPolicy
{
    public const string EmissionFrequencyId = "environment.emissionFrequency";
    public const string StormFrequencyId = "environment.stormFrequency";
    public const string EmissionWarningId = "environment.emissionWarning";
    public const string TraderRestockId = "economy.traderRestockHours";

    public static IReadOnlyList<string> WeatherIds { get; } =
        new[] { EmissionFrequencyId, StormFrequencyId, EmissionWarningId };

    // ---- EMISSION WARNING ----------------------------------------------
    // EmissionPrototypes.cfg [0] (SID Default): the world emission (and the
    // EQ29 side quest). The story emissions [1]..[5] have their own Stages and
    // are never retimed. They inherit Default's AIEvents; the moved EmissionEnd
    // (100–250 s) still comes after every story emission is over (≤ 61 s).
    public const string EmissionFile = "GameData/EmissionPrototypes.cfg";
    public const string EmissionDefault = "[0]";
    public const double VanillaWarningSeconds = 60.0;
    public const double MinWarningSeconds = 30.0;
    public const double MaxWarningSeconds = 180.0;
    /// <summary>The Emission weather lasts 300 s; the stages must end inside it.</summary>
    public const double EmissionWeatherSeconds = 300.0;

    /// <summary>Latest story emission end (its own stages, never retimed).</summary>
    public const double LatestStoryEmissionEnd = 61.0;

    /// <summary>Stage node, vanilla start, vanilla duration (Default).</summary>
    public static IReadOnlyList<(string Node, string Stage, double Start, double Duration)> Stages { get; } = new[]
    {
        ("[0]", "BeforeTheStorm", 0.0, 60.0),
        ("[1]", "ActivateQuest", 0.0, 1.0),
        ("[2]", "ShockWave", 60.0, 10.0),
        ("[3]", "Active", 60.0, 60.0),
        ("[4]", "AfterTheStorm", 120.0, 20.0)
    };

    /// <summary>AIEvents [3] EmissionEnd: NPCs leave cover (vanilla 130 s).</summary>
    public const string AiEndNode = "[3]";
    public const double VanillaAiEndSeconds = 130.0;

    /// <summary>Seconds every stage after the warning moves by.</summary>
    public static double Shift(double warningSeconds) => warningSeconds - VanillaWarningSeconds;

    /// <summary>Last stage end at this warning (must stay ≤ 300 s).</summary>
    public static double EmissionEnd(double warningSeconds) => 140.0 + Shift(warningSeconds);

    /// <summary>
    /// Short in-dial line in player words for the ZONE WEATHER knobs.
    /// The weights are relative to the selected difficulty's vanilla.
    /// </summary>
    public static string? KnobText(string id, double value, double reference)
    {
        const double eps = 0.0001;
        return id switch
        {
            EmissionFrequencyId => value <= eps ? "NO RANDOM EMISSIONS"
                : Math.Abs(value - reference) < eps ? "VANILLA PACE"
                : value < reference ? "RARER EMISSIONS" : "MORE EMISSIONS",
            StormFrequencyId => value <= eps ? "NO STORMS"
                : Math.Abs(value - reference) < eps ? "VANILLA PACE"
                : value < reference ? "CLEARER SKIES" : "MORE RAIN & THUNDER",
            EmissionWarningId => "TO FIND COVER",
            _ => null
        };
    }

    // ---- TRADER RESTOCK ------------------------------------------------
    // BoolProviderPrototypes.cfg: the only refresh conditions trade uses.
    // 84 named traders (base + DLC1) refresh on TradeRegenHoursPassed8; the
    // generic stalker traders (BasicTrader, TraderNPC, AllTraderNPC,
    // RC_TraderNPC) on TradeRegenDaysPassed7 and follow the same ratio.
    public const string RestockFile = "GameData/BoolProviderPrototypes.cfg";
    public const string NamedTraderCondition = "TradeRegenHoursPassed8";
    public const string GenericTraderCondition = "TradeRegenDaysPassed7";
    public const int VanillaRestockHours = 8;
    public const int VanillaGenericRestockDays = 7;

    /// <summary>Generic stalker traders: 7 days × hours / 8, at least 1 day.</summary>
    public static int GenericRestockDays(int hours)
        => Math.Max(1, (int)Math.Floor(VanillaGenericRestockDays * hours / (double)VanillaRestockHours + 0.5));
}
