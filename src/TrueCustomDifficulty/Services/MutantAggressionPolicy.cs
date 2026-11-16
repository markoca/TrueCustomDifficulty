using System;
using System.Collections.Generic;
using System.Linq;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// MUTANT AGGRESSION source map (2.1 base dump, docs/combat/MUTANT-AGGRESSION.md).
///
/// ATTACK RATE per family writes the native
/// <c>DifficultyPrototypes.cfg → [difficulty].MutantCombatDifficulty.AgentCooldownMultipliers</c>
/// list: one entry per exact agent prototype SID (the game lists variants such
/// as <c>Bayun_Collar</c> separately), one <c>CooldownMultiplier</c> per
/// ability cooldown tag. Rate r writes multiplier 1/r (2× rate = half the
/// cooldown). Vanilla: only Master (<c>Stalker</c>) owns entries — 0.5 for
/// eight families — so those families read 2× on Master and 1× elsewhere.
///
/// SPECIAL ABILITY RATE scales the shared group token cooldowns
/// <c>TokenTag.Ability</c> and <c>TokenTag.Ability.Knockdown</c> in
/// <c>CombatSynchronizationPrototypes.cfg</c> (Default 8–12 s; Master 4–6 s,
/// i.e. 2×).
/// </summary>
public static class MutantAggressionPolicy
{
    public const string Prefix = "combat.mutantAttackRate.";
    public const string SpecialAbilityRateId = "combat.mutantSpecialAbilityRate";

    public sealed record Agent(string Sid, IReadOnlyList<string> Tags, bool VanillaOnMaster);

    public sealed record Family(string Key, string Label, IReadOnlyList<Agent> Agents)
    {
        public string ParameterId => Prefix + Key;
        public bool HasVanillaMasterEntries => Agents.Any(a => a.VanillaOnMaster);
    }

    private static string[] T(params string[] tags)
        => tags.Select(t => t.StartsWith("Abilities.", StringComparison.Ordinal)
            ? t : "Ability.Cooldown." + t).ToArray();

    private static readonly string[] BloodsuckerTags = T("RunAttack", "JumpAttack", "ClawAttack", "Roar");
    private static readonly string[] BurerTags =
        T("WeaponDrag", "WeaponRiseAndShoot", "Shield", "Throw", "ClawAttack", "WeaponDrag");
    private static readonly string[] BayunTags =
        T("ClawAttack", "JumpAttack", "Growl", "JumpToEnemy", "BlinkTeleport");
    private static readonly string[] ControllerTags =
        T("ClawAttack", "Abilities.PSYAttack", "Roar", "Abilities.RaiseDeadBody", "Abilities.ZombifyNPC");
    private static readonly string[] SnorkTags =
        T("ClawAttack", "KickAttack", "JumpAttack", "JumpToEnemy", "FlyThrough");

    // Alphabetical, same 14 families and labels as Mutant Health.
    public static IReadOnlyList<Family> Families { get; } = new Family[]
    {
        new("bayun", "Bayun", new Agent[]
        {
            new("Bayun", BayunTags, true), new("Bayun_Collar", BayunTags, true),
            new("Bayun_Collar_Clone", BayunTags, true), new("Bayun_E10_MQ01", BayunTags, true)
        }),
        new("blinddog", "Blind Dog", new Agent[]
        {
            new("Blinddog", T("RunAttack", "BiteAttack", "BiteAttackAnyAngle"), false),
            new("Blinddog_Collar", T("RunAttack", "BiteAttack", "BiteAttackAnyAngle"), false)
        }),
        new("bloodsucker", "Bloodsucker", new Agent[]
        {
            new("Bloodsucker", BloodsuckerTags, true),
            new("MistBloodsucker", T("RunAttack", "JumpAttack", "ClawAttack", "Roar", "BiteAttack"), true),
            new("Bloodsucker_Collar", BloodsuckerTags, false)
        }),
        new("boar", "Boar", new Agent[]
        {
            new("Boar", T("RunAttack", "ClawAttack", "Charge"), false)
        }),
        new("burer", "Burer", new Agent[]
        {
            new("Burer", BurerTags, true), new("Burer_ShootingSpecial", BurerTags, true)
        }),
        new("chimera", "Chimera", new Agent[]
        {
            new("Chimera", T("RunAttack", "Growl", "ClawAttack", "ShortJumpAttack",
                "LongJumpAttack", "FlyThrough"), true)
        }),
        new("controller", "Controller", new Agent[]
        {
            new("Controller", ControllerTags, true), new("Controller_Collar", ControllerTags, true),
            new("Controller_Deer", ControllerTags, false)
        }),
        new("deer", "Deer", new Agent[]
        {
            new("Deer", T("RunAttack", "JumpAttack", "Charge"), true)
        }),
        new("flesh", "Flesh", new Agent[]
        {
            new("Flesh", T("JumpAttack", "ClawAttack"), false)
        }),
        new("poltergeist", "Poltergeist", new Agent[]
        {
            new("Poltergeist", T("Throw", "ActivateAnomaly", "Shield"), false)
        }),
        new("pseudodog", "Pseudodog", new Agent[]
        {
            new("PseudoDog", T("ShockwaveAttack", "RunAttack", "BiteAttack"), true)
        }),
        new("pseudogiant", "Pseudogiant", new Agent[]
        {
            new("Pseudogiant", T("ShockwaveAttack", "RunAttack", "Charge"), true),
            new("Pseudogiant_Collar", T("ShockwaveAttack", "RunAttack", "Charge"), false)
        }),
        new("snork", "Snork", new Agent[]
        {
            new("Snork", SnorkTags, false), new("Snork_Collar", SnorkTags, false)
        }),
        new("tushkan", "Tushkan", new Agent[]
        {
            new("Tushkan", T("JumpAttack", "ClawAttack"), false)
        })
    };

    public static IReadOnlyList<string> ParameterIds { get; } =
        Families.Select(f => f.ParameterId).Append(SpecialAbilityRateId).ToArray();

    /// <summary>Vanilla Master multiplier for the eight listed families.</summary>
    public const double VanillaMasterCooldownMultiplier = 0.5;

    // ---- SPECIAL ABILITY RATE --------------------------------------------
    // Group-token filter entries scaled by SPECIAL ABILITY RATE: FilterGroups
    // index, its AllowedTags[0], and the Default (non-Master) cooldowns.
    public static IReadOnlyList<(string Index, string Tag, double Min, double Max)> AbilityTokenGroups { get; } =
        new[]
        {
            ("[3]", "TokenTag.Ability", 8.0, 12.0),
            ("[4]", "TokenTag.Ability.Knockdown", 8.0, 12.0)
        };

    public static IReadOnlyList<string> Ranks { get; } = new[] { "Newbie", "Experienced", "Veteran", "Master" };

    public static string SynchronizerSid(string difficultySid) => difficultySid switch
    {
        "Easy" => "CombatSynchronizerEasy",
        "Medium" => "CombatSynchronizerMedium",
        "Hard" => "CombatSynchronizerHard",
        "Stalker" => "CombatSynchronizerStalker",
        _ => throw new ArgumentOutOfRangeException(nameof(difficultySid), difficultySid,
            "No combat synchronizer for this difficulty.")
    };
}
