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
/// Each move also has its own knob (rate k): the move's tag gets 1/(r·k).
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
            T("ClawAttack", "JumpAttack", "ShortJumpAttack", "LongJumpAttack", "JumpToEnemy", "FlyThrough"),
            T("CombatSummon"), ("claw", 5), ("clones", 10)),
        new("blinddog", "Blind Dog", new[] { A("Blinddog"), A("Blinddog_Collar"), A("MoldyBlinddog") },
            T("RunAttack", "BiteAttack", "BiteAttackAnyAngle"),
            Array.Empty<string>(), ("bite", 5), null),
        new("bloodsucker", "Bloodsucker", new[]
            {
                A("Bloodsucker", "RunAttack", "JumpAttack", "ClawAttack", "Roar"),
                A("MistBloodsucker", "RunAttack", "JumpAttack", "ClawAttack", "Roar", "BiteAttack"),
                A("Bloodsucker_Collar"), A("AshyBloodsucker"), A("PrologueBloodsucker")
            },
            T("RunAttack", "JumpAttack", "ClawAttack", "BiteAttack"),
            T("ActivateAnomaly"), ("claw", 6), ("anomaly", 3)),
        new("boar", "Boar", new[] { A("Boar"), A("HelmedBoar"), A("PorcupineBoar") },
            T("RunAttack", "ClawAttack", "Charge"),
            Array.Empty<string>(), ("gore", 10), null),
        new("burer", "Burer", new[]
            {
                A("Burer", "WeaponDrag", "WeaponRiseAndShoot", "Shield", "Throw", "ClawAttack", "WeaponDrag"),
                A("Burer_ShootingSpecial", "WeaponDrag", "WeaponRiseAndShoot", "Shield", "Throw", "ClawAttack", "WeaponDrag")
            },
            T("ClawAttack"),
            T("Throw", "ThrowGrenade", "WeaponDrag", "WeaponRiseAndShoot"), ("claw", 8), ("disarm", 8)),
        new("chimera", "Chimera", new[]
            {
                A("Chimera", "RunAttack", "Growl", "ClawAttack", "ShortJumpAttack", "LongJumpAttack", "FlyThrough")
            },
            T("ClawAttack", "RunAttack", "LongJumpAttack", "FlyThrough", "JumpToEnemy"),
            Array.Empty<string>(), ("leap", 5), null),
        new("controller", "Controller", new[]
            {
                A("Controller", "ClawAttack", "Abilities.PSYAttack", "Roar", "Abilities.RaiseDeadBody", "Abilities.ZombifyNPC"),
                A("Controller_Collar", "ClawAttack", "Abilities.PSYAttack", "Roar", "Abilities.RaiseDeadBody", "Abilities.ZombifyNPC"),
                A("Controller_Deer")
            },
            T("ClawAttack"),
            // CombatSummon: Controller_Deer calls blind dogs (CombatSummonAbility_Deer).
            T("Abilities.PSYAttack", "Abilities.RaiseDeadBody", "Abilities.ZombifyNPC", "CombatSummon"), ("claw", 10), ("psy", 12)),
        new("deer", "Deer", new[] { A("Deer", "RunAttack", "JumpAttack", "Charge") },
            T("RunAttack", "JumpAttack", "ClawAttack", "TurnAttack", "Charge"),
            T("CombatSummon"), ("claw", 4), ("dogs", 20)),
        new("flesh", "Flesh", new[] { A("Flesh"), A("BoggyFlesh") },
            T("JumpAttack", "ClawAttack"),
            Array.Empty<string>(), ("claw", 3), null),
        new("poltergeist", "Poltergeist", new[]
            {
                A("Poltergeist"), A("Poltergeist_Electro"), A("Poltergeist_Toxic"), A("Poltergeist_Fire"),
                A("Poltergeist_YanivToxicRozliv"), A("Poltergeist_Explosives")
            },
            T("AOEAttack"),
            T("Throw", "ActivateAnomaly"), ("burst", 3), ("anomaly", 3)),
        new("pseudodog", "Pseudodog", new[]
            {
                A("PseudoDog", "ShockwaveAttack", "RunAttack", "BiteAttack"),
                A("PseudoDogCombatSummon"), A("PseudoDogRetreatSummon")
            },
            // The game's Master list names ShockwaveAttack for PseudoDog, but no pseudodog
            // ability uses that tag (a no-op entry, kept as the game has it). ABILITIES: phantoms.
            T("RunAttack", "BiteAttack"),
            T("CombatSummon", "RetreatSummon"), ("bite", 7), ("phantoms", 20)),
        new("pseudogiant", "Pseudogiant", new[]
            {
                A("Pseudogiant", "ShockwaveAttack", "RunAttack", "Charge"), A("Pseudogiant_Collar")
            },
            T("ClawAttack", "RunAttack", "Charge"),
            T("ShockwaveAttack", "Throw"), ("stomp", 5), ("shockwave", 8)),
        new("snork", "Snork", new[] { A("Snork"), A("Snork_Collar") },
            T("ClawAttack", "KickAttack", "JumpAttack", "JumpToEnemy", "FlyThrough"),
            Array.Empty<string>(), ("kick", 7), null),
        new("tushkan", "Tushkan", new[] { A("Tushkan") },
            T("JumpAttack", "ClawAttack"),
            Array.Empty<string>(), ("claw", 3), null)
    };

    // ATTACKS sliders only. ABILITIES have no slider: each ability is its own knob
    // (2026-11-18), carrying the game's Master vanilla itself.
    public static IReadOnlyList<string> RateParameterIds { get; } =
        Families.Select(f => f.ParameterId).ToArray();


    // ---- One knob per move (docs/combat/MUTANT-COOLDOWNS.md) ---------------
    /// <param name="Slug">Id suffix: combat.mutantMoveRate.{family}.{slug}.</param>
    /// <param name="Label">Knob caption (short, CAPS on the board).</param>
    /// <param name="Name">Panel name of the move.</param>
    /// <param name="Seconds">Vanilla cooldown of the family's main agent, base CFG.</param>
    public sealed record Move(string Slug, string Label, string Name, string Tag, double Seconds);

    public const string MovePrefix = "combat.mutantMoveRate.";

    public static string MoveId(Family family, Move move) => MovePrefix + family.Key + "." + move.Slug;

    private static Move M(string slug, string label, string name, string tag, double seconds)
        => new(slug, label, name, tag.StartsWith("Abilities.", StringComparison.Ordinal) ? tag : "Ability.Cooldown." + tag, seconds);

    /// <summary>
    /// Every attack that gets its own knob under its group's slider, one per cooldown tag,
    /// in board order (shortest vanilla wait first). Only moves that hurt or bring enemies:
    /// roars, growls, howls, dodges, teleports and shields stay vanilla (no slider, no knob),
    /// and so do moves the CFG never triggers (Chimera short leap: ShortJumpAttackData
    /// Enabled = false) or that have no cooldown (Chimera claw, 0 s).
    /// </summary>
    public static IReadOnlyList<Move> Moves(Family family, Group group) => (family.Key, group) switch
    {
        ("bayun", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 5),
            M("jumpIn", "JUMP IN", "jump in", "JumpToEnemy", 6), M("leap", "LEAP", "leap", "JumpAttack", 8),
            M("flyThrough", "FLY-BY", "fly-through", "FlyThrough", 10) },
        ("bayun", Group.Abilities) => new[] { M("clones", "CLONES", "clone summon", "CombatSummon", 10) },
        ("blinddog", Group.Attacks) => new[] { M("bite", "BITE", "bite", "BiteAttack", 5), M("runBite", "RUN BITE", "running bite", "RunAttack", 7),
            M("anyAngleBite", "SPIN BITE", "bite from any angle", "BiteAttackAnyAngle", 16) },
        ("bloodsucker", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 6), M("run", "RUN", "running attack", "RunAttack", 6),
            M("leap", "LEAP", "leap", "JumpAttack", 20) },
        ("bloodsucker", Group.Abilities) => new[] { M("anomaly", "ANOMALY", "spawn anomaly", "ActivateAnomaly", 3) },
        ("boar", Group.Attacks) => new[] { M("run", "RUN", "running attack", "RunAttack", 5), M("gore", "GORE", "gore", "ClawAttack", 10),
            M("charge", "CHARGE", "charge", "Charge", 12) },
        ("burer", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 8) },
        ("burer", Group.Abilities) => new[] { M("grenades", "GRENADES", "grenade throws", "ThrowGrenade", 0.5),
            M("disarm", "DISARM", "disarm", "WeaponDrag", 8), M("throw", "THROW", "throw", "Throw", 10),
            M("weapons", "WEAPONS", "raise weapons", "WeaponRiseAndShoot", 10) },
        ("chimera", Group.Attacks) => new[] { M("longLeap", "LUNGE", "long leap", "LongJumpAttack", 5),
            M("run", "RUN", "running attack", "RunAttack", 5), M("jumpIn", "JUMP IN", "jump in", "JumpToEnemy", 6),
            M("flyThrough", "FLY-BY", "fly-through", "FlyThrough", 10) },
        ("controller", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 10) },
        ("controller", Group.Abilities) => new[] { M("psy", "PSY", "psy strike", "Abilities.PSYAttack", 12),
            M("corpse", "CORPSE", "raise corpse", "Abilities.RaiseDeadBody", 15), M("zombify", "ZOMBIFY", "zombify", "Abilities.ZombifyNPC", 15),
            M("dogs", "DOGS", "blind dog summon (Deer controller)", "CombatSummon", 20) },
        ("deer", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 4), M("leap", "LEAP", "leap", "JumpAttack", 8),
            M("charge", "CHARGE", "charge", "Charge", 10) },
        ("deer", Group.Abilities) => new[] { M("dogs", "DOGS", "blind dog summon", "CombatSummon", 20) },
        ("flesh", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 3), M("leap", "LEAP", "leap", "JumpAttack", 4) },
        ("poltergeist", Group.Attacks) => new[] { M("burst", "BURST", "area burst", "AOEAttack", 3) },
        ("poltergeist", Group.Abilities) => new[] { M("anomaly", "ANOMALY", "spawn anomaly", "ActivateAnomaly", 3),
            M("throw", "THROW", "throw", "Throw", 10) },
        ("pseudodog", Group.Attacks) => new[] { M("run", "RUN", "running attack", "RunAttack", 6), M("bite", "BITE", "bite", "BiteAttack", 7) },
        ("pseudodog", Group.Abilities) => new[] { M("retreatPhantoms", "RETREAT", "phantom summon while fleeing", "RetreatSummon", 10),
            M("phantoms", "PHANTOMS", "phantom summon", "CombatSummon", 20) },
        ("pseudogiant", Group.Attacks) => new[] { M("stomp", "STOMP", "stomp", "ClawAttack", 5), M("run", "RUN", "running attack", "RunAttack", 5),
            M("charge", "CHARGE", "charge", "Charge", 10) },
        ("pseudogiant", Group.Abilities) => new[] { M("shockwave", "WAVE", "shockwave", "ShockwaveAttack", 8),
            M("throw", "THROW", "throw (electrocollar variant)", "Throw", 10) },
        ("snork", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 5), M("kick", "KICK", "kick", "KickAttack", 7),
            M("leap", "LEAP", "leap", "JumpAttack", 12),
            M("jumpIn", "JUMP IN", "jump in", "JumpToEnemy", 12), M("flyThrough", "FLY-BY", "fly-through", "FlyThrough", 14) },
        ("tushkan", Group.Attacks) => new[] { M("claw", "CLAW", "claw", "ClawAttack", 3), M("leap", "LEAP", "leap", "JumpAttack", 8) },
        _ => Array.Empty<Move>()
    };

    /// <summary>Every move knob: family, group, move.</summary>
    public static IReadOnlyList<(Family Family, Group Group, Move Move)> MoveKnobs { get; } =
        Families.SelectMany(f => new[] { Group.Attacks, Group.Abilities }
            .SelectMany(g => Moves(f, g).Select(m => (f, g, m)))).ToArray();

    public static IReadOnlyList<string> MoveParameterIds { get; } =
        MoveKnobs.Select(k => MoveId(k.Family, k.Move)).ToArray();

    // Declared after MoveParameterIds: static initializers run in text order.
    public static IReadOnlyList<string> ParameterIds { get; } =
        RateParameterIds.Append(SpecialAbilityRateId).Concat(MoveParameterIds).ToArray();

    /// <summary>Difficulty vanilla of a move knob: an ability the game halves on Master is 2× there.</summary>
    public static double KnobReference(Family family, Group group, Move move, string difficultySid)
        => group == Group.Abilities && difficultySid == "Stalker"
            && family.Agents.Any(a => a.VanillaMasterTags.Contains(move.Tag)) ? 2.0 : 1.0;

    /// <summary>
    /// Real seconds of one move. Attacks: vanilla × ATTACKS slider × own knob.
    /// Abilities: vanilla × own knob alone (the knob holds the Master vanilla).
    /// </summary>
    public static double MoveSeconds(Family family, Group group, Move move,
        double attackRate, double attackReference, double knob, double knobReference)
        => group == Group.Attacks
            ? move.Seconds * Multiplier(family.Agents[0], move.Tag, attackRate, attackReference) / knob
            : move.Seconds * Multiplier(family.Agents[0], move.Tag, knob, knobReference);

    public static string Seconds(double seconds)
        => seconds.ToString(seconds < 10 ? "0.##" : "0.#", CultureInfo.InvariantCulture) + " s";

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
        ("chimera", Group.Attacks) => "Ability.Cooldown.LongJumpAttack",
        ("snork", Group.Attacks) => "Ability.Cooldown.KickAttack",
        ("poltergeist", Group.Attacks) => "Ability.Cooldown.AOEAttack",
        ("bayun", Group.Abilities) or ("pseudodog", Group.Abilities) or ("deer", Group.Abilities)
            => "Ability.Cooldown.CombatSummon",
        ("burer", Group.Abilities) => "Ability.Cooldown.WeaponDrag",
        ("bloodsucker", Group.Abilities) or ("poltergeist", Group.Abilities) => "Ability.Cooldown.ActivateAnomaly",
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
