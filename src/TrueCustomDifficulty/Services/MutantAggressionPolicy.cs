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
    public const string SummonRateId = "combat.mutantSummonRate";

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
            T("Growl"), ("leap", 8), ("growl", 60)),
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
            // The game's Master list names ShockwaveAttack for PseudoDog, but no pseudodog
            // ability uses that tag (a no-op entry); it rides with ATTACKS so the list stays
            // covered. Its phantom summons belong to SUMMON RATE, so ABILITIES is the howl.
            T("RunAttack", "BiteAttack", "ShockwaveAttack"),
            T("Howl"), ("bite", 7), ("howl", 30)),
        new("pseudogiant", "Pseudogiant", new[]
            {
                A("Pseudogiant", "ShockwaveAttack", "RunAttack", "Charge"), A("Pseudogiant_Collar")
            },
            T("ClawAttack", "RunAttack", "Charge"),
            T("ShockwaveAttack", "Roar", "Throw"), ("stomp", 5), ("shockwave", 8)),
        new("snork", "Snork", new[] { A("Snork"), A("Snork_Collar") },
            T("ClawAttack", "KickAttack", "JumpAttack", "JumpToEnemy", "FlyThrough", "Evasion"),
            T("Growl"), ("kick", 7), ("growl", 120)),
        new("tushkan", "Tushkan", new[] { A("Tushkan") },
            T("JumpAttack", "ClawAttack"),
            Array.Empty<string>(), ("claw", 3), null)
    };

    public static IReadOnlyList<string> RateParameterIds { get; } =
        Families.Select(f => f.ParameterId)
            .Concat(Families.Where(f => f.HasAbilities).Select(f => f.AbilityParameterId))
            .ToArray();

    public static IReadOnlyList<string> ParameterIds { get; } =
        RateParameterIds.Append(SpecialAbilityRateId).Append(SummonRateId).ToArray();

    // ---- Moves in real seconds (docs/combat/MUTANT-COOLDOWNS.md) -----------
    /// <summary>One move of the family's main agent: name, own cooldown tag, vanilla seconds.</summary>
    public sealed record Move(string Name, string Tag, double Seconds);

    private static Move M(string name, string tag, double seconds)
        => new(name, tag.StartsWith("Abilities.", StringComparison.Ordinal) ? tag : "Ability.Cooldown." + tag, seconds);

    /// <summary>Every move a group's knob reaches, in base CFG seconds (main agent, special variants left out).</summary>
    public static IReadOnlyList<Move> Moves(Family family, Group group) => (family.Key, group) switch
    {
        ("bayun", Group.Attacks) => new[] { M("dodge", "Evasion", 1), M("claw", "ClawAttack", 5), M("jump in", "JumpToEnemy", 6),
            M("leap", "JumpAttack", 8), M("fly-through", "FlyThrough", 10) },
        ("bayun", Group.Abilities) => new[] { M("teleport", "BlinkTeleport", 12), M("growl", "Growl", 60) },
        ("blinddog", Group.Attacks) => new[] { M("bite", "BiteAttack", 5), M("running bite", "RunAttack", 7),
            M("bite from any angle", "BiteAttackAnyAngle", 16) },
        ("blinddog", Group.Abilities) => new[] { M("shield", "Shield", 25) },
        ("bloodsucker", Group.Attacks) => new[] { M("turn swipe", "ClawAttack", 5), M("claw", "ClawAttack", 6),
            M("running attack", "RunAttack", 6), M("leap", "JumpAttack", 20) },
        ("bloodsucker", Group.Abilities) => new[] { M("spawn anomaly", "ActivateAnomaly", 3), M("Ashy roar", "Roar", 60) },
        ("boar", Group.Attacks) => new[] { M("running attack", "RunAttack", 5), M("gore", "ClawAttack", 10), M("charge", "Charge", 12) },
        ("burer", Group.Attacks) => new[] { M("claw", "ClawAttack", 8) },
        ("burer", Group.Abilities) => new[] { M("grenade throws", "ThrowGrenade", 0.5), M("disarm", "WeaponDrag", 8),
            M("throw", "Throw", 10), M("raise weapons", "WeaponRiseAndShoot", 10), M("shield", "Shield", 17) },
        ("chimera", Group.Attacks) => new[] { M("claw", "ClawAttack", 0), M("dodge", "Evasion", 0), M("long leap", "LongJumpAttack", 5),
            M("running attack", "RunAttack", 5), M("jump in", "JumpToEnemy", 6), M("short leap", "ShortJumpAttack", 8),
            M("fly-through", "FlyThrough", 10) },
        ("chimera", Group.Abilities) => new[] { M("growl", "Growl", 60) },
        ("controller", Group.Attacks) => new[] { M("claw", "ClawAttack", 10) },
        ("controller", Group.Abilities) => new[] { M("roar", "Roar", 7), M("psy strike", "Abilities.PSYAttack", 12),
            M("raise corpse", "Abilities.RaiseDeadBody", 15), M("zombify", "Abilities.ZombifyNPC", 15) },
        ("deer", Group.Attacks) => new[] { M("claw", "ClawAttack", 4), M("leap", "JumpAttack", 8), M("charge", "Charge", 10) },
        ("flesh", Group.Attacks) => new[] { M("claw", "ClawAttack", 3), M("leap", "JumpAttack", 4), M("turn swipe", "ClawAttack", 5) },
        ("poltergeist", Group.Attacks) => new[] { M("area burst", "AOEAttack", 3) },
        ("poltergeist", Group.Abilities) => new[] { M("spawn anomaly", "ActivateAnomaly", 3), M("throw", "Throw", 10),
            M("shield", "Shield", 20) },
        ("pseudodog", Group.Attacks) => new[] { M("turn bite", "BiteAttack", 5), M("running attack", "RunAttack", 6),
            M("bite", "BiteAttack", 7) },
        ("pseudodog", Group.Abilities) => new[] { M("howl", "Howl", 30) },
        ("pseudogiant", Group.Attacks) => new[] { M("stomp", "ClawAttack", 5), M("running attack", "RunAttack", 5),
            M("charge", "Charge", 10) },
        ("pseudogiant", Group.Abilities) => new[] { M("shockwave", "ShockwaveAttack", 8), M("throw", "Throw", 10),
            M("roar", "Roar", 60) },
        ("snork", Group.Attacks) => new[] { M("claw", "ClawAttack", 5), M("kick", "KickAttack", 7), M("dodge", "Evasion", 7),
            M("leap", "JumpAttack", 12), M("jump in", "JumpToEnemy", 12), M("fly-through", "FlyThrough", 14) },
        ("snork", Group.Abilities) => new[] { M("growl", "Growl", 120) },
        ("tushkan", Group.Attacks) => new[] { M("claw", "ClawAttack", 3), M("leap", "JumpAttack", 8) },
        _ => Array.Empty<Move>()
    };

    /// <summary>Panel lines, e.g. "psy strike 6 s": every move of the group at this rate.</summary>
    public static string MoveTimes(Family family, Group group, double rate, double reference)
        => string.Join("\n", Moves(family, group).Select(m =>
            $"{m.Name} {Seconds(m.Seconds * Multiplier(family.Agents[0], m.Tag, rate, reference))}"));

    private static string Seconds(double seconds)
        => seconds.ToString(seconds < 10 ? "0.##" : "0.#", CultureInfo.InvariantCulture) + " s";

    // ---- SUMMON RATE --------------------------------------------------------
    /// <summary>A summon of one mutant: who calls it, what arrives, its own tag and vanilla seconds.</summary>
    public sealed record Summon(string Name, string Tag, double Seconds, IReadOnlyList<string> Agents);

    /// <summary>Every summon SUMMON RATE scales (story Faust and DLC1 Jaded Chimera left out).</summary>
    public static IReadOnlyList<Summon> Summons { get; } = new[]
    {
        new Summon("pseudodog phantoms", "Ability.Cooldown.CombatSummon", 20, new[] { "PseudoDog" }),
        new Summon("pseudodog phantoms on retreat", "Ability.Cooldown.RetreatSummon", 10, new[] { "PseudoDog" }),
        new Summon("bayun clones", "Ability.Cooldown.CombatSummon", 10,
            new[] { "Bayun", "Bayun_Collar", "Bayun_E10_MQ01" }),
        new Summon("deer blind dogs", "Ability.Cooldown.CombatSummon", 20, new[] { "Deer", "Controller_Deer" })
    };

    /// <summary>Summon tags of one agent (empty when it summons nothing).</summary>
    public static IReadOnlyList<string> SummonTags(string agentSid)
        => Summons.Where(x => x.Agents.Contains(agentSid)).Select(x => x.Tag).Distinct().ToArray();

    /// <summary>Card value, e.g. "phantoms 20 s · clones 10 s · dogs 20 s".</summary>
    public static string SummonDisplay(double rate)
        => $"phantoms {Seconds(20 / rate)} · clones {Seconds(10 / rate)} · dogs {Seconds(20 / rate)}";

    /// <summary>Panel lines: every summon at this rate.</summary>
    public static string SummonTimes(double rate)
        => string.Join("\n", Summons.Select(x => $"{x.Name} {Seconds(x.Seconds / rate)}"));

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

    /// <summary>Panel text in real seconds, e.g. "psy every 6 s" (the family's headline cooldown).</summary>
    public static string RateDisplay(Family family, Group group, double rate, double reference)
        => CooldownDisplay(family, group, rate, reference)
            ?? $"{rate.ToString("0.##", CultureInfo.InvariantCulture)}× " + (group == Group.Attacks ? "attacks" : "abilities");

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
        ("pseudodog", Group.Abilities) => "Ability.Cooldown.Howl",
        ("controller", Group.Abilities) => "Abilities.PSYAttack",
        ("pseudogiant", Group.Abilities) => "Ability.Cooldown.ShockwaveAttack",
        _ => string.Empty
    };

    /// <summary>Board and panel value of SPECIAL ABILITY RATE (writer rounding).</summary>
    public static string SpecialAbilityDisplay(double rate)
        => string.Create(CultureInfo.InvariantCulture,
            $"{rate:0.##}× · special move every {8.0 / rate:0.##}–{12.0 / rate:0.##} s");

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
