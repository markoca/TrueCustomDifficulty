using System;
using System.Collections.Generic;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// ARMY MEDKIT MIX in breakable medical boxes (2026-11-17).
///
/// World medical boxes use DestructibleStash_Med (player rank Newbie:
/// Medkit/Bandage, no Army Medkit) and DestructibleStash_Med_Experienced /
/// _Veteran / _Master, which point to DestructibleStash_Medkit_* Weight lists
/// (one pick per box). The control scales only the ArmyMedkit Weight
/// (PossibleItems [1]); Medkit and EcoMedkit keep theirs, so 0x removes Army
/// Medkits from the pick and higher values make them a larger share.
/// Direct edit points live in cfg-writer-baseline.json (operation "scale").
/// </summary>
public static class MedicalBoxPolicy
{
    public const string ArmyMedkitWeightId = "loot.medicalBoxArmyMedkitWeight";

    /// <summary>Source Weights (Medkit, ArmyMedkit, EcoMedkit) per player rank.</summary>
    public static readonly IReadOnlyList<(string Rank, string Prototype, double Medkit, double Army, double Eco)> Ranks =
        new[]
        {
            ("Experienced", "DestructibleStash_Medkit_Experienced", 20.0, 4.0, 1.0),
            ("Veteran", "DestructibleStash_Medkit_Veteran", 20.0, 10.0, 5.0),
            ("Master", "DestructibleStash_Medkit_Master", 10.0, 10.0, 10.0)
        };

    /// <summary>Army Medkit share of one box's pick (0–1) at a Weight factor.</summary>
    public static double ArmyShare(int index, double factor)
    {
        var (_, _, medkit, army, eco) = Ranks[index];
        var scaled = army * Math.Max(0.0, factor);
        var total = medkit + scaled + eco;
        return total <= 0.0 ? 0.0 : scaled / total;
    }
}
