using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// MUTANT AGGRESSION source map (2.1 base dump, docs/combat/MUTANT-AGGRESSION.md).
///
/// Real cooldowns live in <c>AbilityPrototypes.cfg</c> (TriggeredCooldowns →
/// Duration, seconds). The difficulty scales them per agent and per cooldown
/// tag through <c>DifficultyPrototypes.cfg → [difficulty].MutantCombatDifficulty
/// .AgentCooldownMultipliers</c>; vanilla Master (<c>Stalker</c>) lists 14 agents
/// at 0.5. Each family has two rates over its own cooldown tags (collected from
/// its abilities, all variants included):
///  * ATTACKS — claws, bites, runs, leaps, kicks, charges, evasions;
///  * ABILITIES — psi attacks, shields, throws, shockwaves, roars, teleports.
/// Rate r writes 1/r on every tag of the group for every agent of the family.
/// At the difficulty's vanilla value the game's own entries are written back.
///
/// SPECIAL ABILITY RATE scales the shared group token cooldowns
/// <c>TokenTag.Ability</c> / <c>TokenTag.Ability.Knockdown</c> in
/// <c>CombatSynchronizationPrototypes.cfg</c> (Default 8–12 s; Master 4–6 s).
/// </summary>
public static class MutantAggressionPolicy
{
    public const string Prefix = "combat.mutantAttackRate.";
    public const string AbilityPrefix = "combat.mutantAbilityRate.";
    public const string SpecialAbilityRateId = "combat.mutantSpecialAbilityRate";

    public enum Group { Attacks, Abilities }

    /// <param name="VanillaMasterTags">Tags the game lists for this agent on Master (0.5), in order.</param>
    public sealed record Agent(string Sid, IReadOnlyList<string> VanillaMasterTags);

    /// <param name="Headline">Representative own cooldown per group: short name and vanilla seconds.</param>
    public sealed record Family(
        string Key, string Label, IReadOnlyList<Agent> Agents,
        IReadOnlyList<string> AttackTags, IReadOnlyList<string> AbilityTags,
        (string Name, double Seconds)? AttackHeadline,
        (string Name, double Seconds)? AbilityHeadline)
    {
        public string ParameterId => Prefix + Key;
        public string AbilityParameterId => AbilityPrefix + Key;
        public bool HasAbilities => AbilityTags.Count > 0;

        public IReadOnlyList<string> Tags(Group group) => group == Group.Attacks ? AttackTags : AbilityTags;

        /// <summary>The game halves this group on Master (some agent lists one of its tags).</summary>
        public bool HasVanillaMasterEntries(Group group)
            => Agents.Any(a => a.VanillaMasterTags.Any(t => Tags(group).Contains(t)));
    }

    private static string[] T(params string[] tags)
        => tags.Select(t => t.StartsWith("Abilities.", StringComparison.Ordinal)
            ? t : "Ability.Cooldown." + t).ToArray();

    private static Agent A(string sid, params string[] vanillaTags) => new(sid, T(vanillaTags));

    // Alphabetical, same 14 families and labels as Mutant Health.
    public static IReadOnlyList<Family> Families { get; } = new Family[]
    {
        new("bayun", "Bayun", new[]
            {
                A("Bayun", "ClawAttack", "JumpAttack", "Growl", "JumpToEnemy", "BlinkTeleport"),
                A("Bayun_Collar", "ClawAttack", "JumpAttack", "Growl", "JumpToEnemy", "BlinkTeleport"),
                A("Bayun_Collar_Clone", "ClawAttack", "JumpAttack", "Growl", "JumpToEnemy", "BlinkTeleport"),
                A("Bayun_E10_MQ01", "ClawAttack", "JumpAttack", "Growl", "JumpToEnemy", "BlinkTeleport")
            },
            T("ClawAttack", "JumpAttack", "ShortJumpAttack", "LongJumpAttack", "JumpToEnemy", "FlyThrough", "Evasion"),
            T("Growl", "BlinkTeleport"), ("claw", 5), ("teleport", 12)),
        new("blinddog", "Blind Dog", new[] { A("Blinddog"), A("Blinddog_Collar"), A("MoldyBlinddog") },
            T("RunAttack", "BiteAttack", "BiteAttackAnyAngle"),
            T("Shield"), ("bite", 5), ("shield", 25)),
        new("bloodsucker", "Bloodsucker", new[]
            {
                A("Bloodsucker", "RunAttack", "JumpAttack", "ClawAttack", "Roar"),
                A("MistBloodsucker", "RunAttack", "JumpAttack", "ClawAttack", "Roar", "BiteAttack"),
                A("Bloodsucker_Collar"), A("AshyBloodsucker"), A("PrologueBloodsucker")
            },
            T("RunAttack", "JumpAttack", "ClawAttack", "BiteAttack"),
            T("Roar", "ActivateAnomaly"), ("claw", 6), ("anomaly", 3)),
        new("boar", "Boar", new[] { A("Boar"), A("HelmedBoar"), A("PorcupineBoar") },
            T("RunAttack", "ClawAttack", "Charge"),
            Array.Empty<string>(), ("gore", 10), null),
        new("burer", "Burer", new[]
            {
                A("Burer", "WeaponDrag", "WeaponRiseAndShoot", "Shield", "Throw", "ClawAttack", "WeaponDrag"),
                A("Burer_ShootingSpecial", "WeaponDrag", "WeaponRiseAndShoot", "Shield", "Throw", "ClawAttack", "WeaponDrag")
            },
            T("ClawAttack"),
            T("Shield", "Throw", "ThrowGrenade", "WeaponDrag", "WeaponRiseAndShoot"), ("claw", 8), ("shield", 17)),
        new("chimera", "Chimera", new[]
            {
                A("Chimera", "RunAttack", "Growl", "ClawAttack", "ShortJumpAttack", "LongJumpAttack", "FlyThrough")
            },
            T("ClawAttack", "RunAttack", "ShortJumpAttack", "LongJumpAttack", "FlyThrough", "JumpToEnemy", "Evasion"),
            T("Growl"), ("leap", 8), ("growl", 30)),
        new("controller", "Controller", new[]
            {
                A("Controller", "ClawAttack", "Abilities.PSYAttack", "Roar", "Abilities.RaiseDeadBody", "Abilities.ZombifyNPC"),
                A("Controller_Collar", "ClawAttack", "Abilities.PSYAttack", "Roar", "Abilities.RaiseDeadBody", "Abilities.ZombifyNPC"),
                A("Controller_Deer")
            },
            T("ClawAttack"),
            T("Abilities.PSYAttack", "Roar", "Abilities.RaiseDeadBody", "Abilities.ZombifyNPC"), ("claw", 10), ("psy", 12)),
        new("deer", "Deer", new[] { A("Deer", "RunAttack", "JumpAttack", "Charge") },
            T("RunAttack", "JumpAttack", "ClawAttack", "TurnAttack", "Charge"),
            Array.Empty<string>(), ("claw", 4), null),
        new("flesh", "Flesh", new[] { A("Flesh"), A("BoggyFlesh") },
            T("JumpAttack", "ClawAttack"),
            Array.Empty<string>(), ("claw", 3), null),
        new("poltergeist", "Poltergeist", new[]
            {
                A("Poltergeist"), A("Poltergeist_Electro"), A("Poltergeist_Toxic"), A("Poltergeist_Fire"),
                A("Poltergeist_YanivToxicRozliv"), A("Poltergeist_Explosives")
            },
            T("AOEAttack"),
            T("Throw", "ActivateAnomaly", "Shield"), ("burst", 3), ("shield", 20)),
        new("pseudodog", "Pseudodog", new[]
            {
                A("PseudoDog", "ShockwaveAttack", "RunAttack", "BiteAttack"),
                A("PseudoDogCombatSummon"), A("PseudoDogRetreatSummon")
            },
            T("RunAttack", "BiteAttack"),
            T("ShockwaveAttack"), ("bite", 7), null),
        new("pseudogiant", "Pseudogiant", new[]
            {
                A("Pseudogiant", "ShockwaveAttack", "RunAttack", "Charge"), A("Pseudogiant_Collar")
            },
            T("ClawAttack", "RunAttack", "Charge"),
            T("ShockwaveAttack", "Roar", "Throw"), ("stomp", 5), ("shockwave", 8)),
        new("snork", "Snork", new[] { A("Snork"), A("Snork_Collar") },
            T("ClawAttack", "KickAttack", "JumpAttack", "JumpToEnemy", "FlyThrough", "Evasion"),
            T("Growl"), ("kick", 7), ("growl", 20)),
        new("tushkan", "Tushkan", new[] { A("Tushkan") },
            T("JumpAttack", "ClawAttack"),
            Array.Empty<string>(), ("claw", 3), null)
    };

    public static IReadOnlyList<string> RateParameterIds { get; } =
        Families.Select(f => f.ParameterId)
            .Concat(Families.Where(f => f.HasAbilities).Select(f => f.AbilityParameterId))
            .ToArray();

    public static IReadOnlyList<string> ParameterIds { get; } =
        RateParameterIds.Append(SpecialAbilityRateId).ToArray();

    /// <summary>Vanilla Master multiplier of the listed agents/tags.</summary>
    public const double VanillaMasterCooldownMultiplier = 0.5;

    public static string ParameterId(Family family, Group group)
        => group == Group.Attacks ? family.ParameterId : family.AbilityParameterId;

    /// <summary>Difficulty vanilla of a group's rate: 2 on Master where the game lists the group.</summary>
    public static double Reference(Family family, Group group, string difficultySid)
        => difficultySid == "Stalker" && family.HasVanillaMasterEntries(group) ? 2.0 : 1.0;

    /// <summary>Cooldown multiplier the game ends up with for one tag of one agent.</summary>
    public static double Multiplier(Agent agent, string tag, double rate, double reference)
    {
        if (Math.Abs(rate - reference) < 0.0000001)
            return reference > 1.0 && agent.VanillaMasterTags.Contains(tag)
                ? VanillaMasterCooldownMultiplier : 1.0;
        return 1.0 / rate;
    }

    /// <summary>Panel text, e.g. "2x · psy every 6 s" (rate + the family's headline cooldown).</summary>
    public static string RateDisplay(Family family, Group group, double rate, double reference)
    {
        var text = $"{rate.ToString("0.##", CultureInfo.InvariantCulture)}x";
        return CooldownDisplay(family, group, rate, reference) is { } cooldown
            ? text + " · " + cooldown
            : text + (group == Group.Attacks ? " attacks" : " abilities");
    }

    /// <summary>Card text, e.g. "psy every 6 s": the time between two uses of
    /// the headline move. Null when the group has no moves (e.g. Boar abilities).</summary>
    public static string? CooldownDisplay(Family family, Group group, double rate, double reference)
    {
        var headline = group == Group.Attacks ? family.AttackHeadline : family.AbilityHeadline;
        if (headline is not { } h)
            return null;
        var tag = HeadlineTag(family, group);
        var multiplier = Multiplier(family.Agents[0], tag, rate, reference);
        return $"{h.Name} every {(h.Seconds * multiplier).ToString("0.#", CultureInfo.InvariantCulture)} s";
    }

    // Tag behind each headline (first agent decides the vanilla Master case).
    private static string HeadlineTag(Family family, Group group) => (family.Key, group) switch
    {
        ("bayun", Group.Attacks) or ("bloodsucker", Group.Attacks) or ("burer", Group.Attacks)
            or ("controller", Group.Attacks) or ("deer", Group.Attacks) or ("flesh", Group.Attacks)
            or ("pseudogiant", Group.Attacks) or ("tushkan", Group.Attacks) => "Ability.Cooldown.ClawAttack",
        ("blinddog", Group.Attacks) or ("pseudodog", Group.Attacks) => "Ability.Cooldown.BiteAttack",
        ("boar", Group.Attacks) => "Ability.Cooldown.ClawAttack",
        ("chimera", Group.Attacks) => "Ability.Cooldown.ShortJumpAttack",
        ("snork", Group.Attacks) => "Ability.Cooldown.KickAttack",
        ("poltergeist", Group.Attacks) => "Ability.Cooldown.AOEAttack",
        ("bayun", Group.Abilities) => "Ability.Cooldown.BlinkTeleport",
        ("blinddog", Group.Abilities) or ("burer", Group.Abilities) or ("poltergeist", Group.Abilities)
            => "Ability.Cooldown.Shield",
        ("bloodsucker", Group.Abilities) => "Ability.Cooldown.ActivateAnomaly",
        ("chimera", Group.Abilities) or ("snork", Group.Abilities) => "Ability.Cooldown.Growl",
        ("controller", Group.Abilities) => "Abilities.PSYAttack",
        ("pseudogiant", Group.Abilities) => "Ability.Cooldown.ShockwaveAttack",
        _ => string.Empty
    };

    /// <summary>Board and panel value of SPECIAL ABILITY RATE (writer rounding).</summary>
    public static string SpecialAbilityDisplay(double rate)
        => string.Create(CultureInfo.InvariantCulture,
            $"{rate:0.##}x · special move every {8.0 / rate:0.##}–{12.0 / rate:0.##} s");

    // ---- SPECIAL ABILITY RATE --------------------------------------------
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
