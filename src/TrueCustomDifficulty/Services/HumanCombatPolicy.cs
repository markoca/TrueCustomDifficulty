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
        new(GrenadeRateId, "GRENADES", "grenade",
            new[] { ("[9]", "TokenTag.CombatAction.ThrowGrenade"), ("[16]", "TokenTag.CombatAction.Special.ThrowGrenade") },
            true, (20, 35), (10, 17)),
        new(SuppressionRateId, "SUPPRESSION", "suppressive fire",
            new[] { ("[10]", "TokenTag.CombatAction.SuppressiveFire") },
            true, (15, 25), (7, 12)),
        new(FlankRateId, "FLANKING", "flank",
            new[] { ("[5]", "TokenTag.CombatAction.Flank") },
            true, (10, 20), (5, 10)),
        new(PushRateId, "PUSH", "push",
            new[] { ("[6]", "TokenTag.CombatAction.Advance.Action"), ("[7]", "TokenTag.CombatAction.Advance.Approach") },
            true, (12, 25), (6, 12)),
        new(CamperPunishRateId, "CAMPING PENALTY", "flush-out",
            new[] { ("[8]", "TokenTag.CamperPunish") },
            false, (2, 5), (2, 5))
    };

    public static IReadOnlyList<string> TacticIds { get; } = Tactics.Select(t => t.Id).ToArray();

    public static IReadOnlyList<string> AwarenessIds { get; } =
        new[] { SightRangeId, HearingRangeId, AlertMemoryId };

    /// <summary>Board order of the HUMAN TACTICS cards (tactics, then accuracy).</summary>
    public static IReadOnlyList<string> TacticsBoardIds { get; } = TacticIds.Append(AccuracyId).ToArray();

    public static double Reference(TacticControl control, string difficultySid)
        => difficultySid == "Stalker" && control.MasterHalves ? 2.0 : 1.0;

    /// <summary>Card / panel text, e.g. "2x · grenade every 10–17 s".</summary>
    public static string TacticDisplay(TacticControl control, double rate, double reference)
    {
        var master = reference > 1.0;
        var (min, max) = master ? control.MasterSeconds : control.DefaultSeconds;
        var factor = (master ? reference : 1.0) / Math.Max(rate, 0.0000001);
        return string.Create(CultureInfo.InvariantCulture,
            $"{rate:0.##}x · {control.Verb} every {min * factor:0.#}–{max * factor:0.#} s");
    }

    // ---- display helpers for HUMAN AWARENESS / ACCURACY ------------------------
    public static string AccuracyDisplay(double factor)
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}x · spread {100.0 / factor:0}%");

    public static string SightDisplay(double factor)
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}x · {80.0 * factor:0} m / {25.0 * factor:0} m");

    public static string HearingDisplay(double factor)
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}x · steps {12.0 * factor:0.#} m");

    public static string MemoryDisplay(double factor)
        => string.Create(CultureInfo.InvariantCulture, $"{factor:0.##}x · search {30.0 * factor:0} s");
}
