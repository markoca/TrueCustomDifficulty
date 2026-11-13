namespace TrueCustomDifficulty.Services;

/// <summary>
/// Eight exact native EItemType buy-denial categories. Separate from merchant
/// stock ItemGenerator categories and individual Food/Drinks/Medical labels.
/// </summary>
public static class TraderBuysPolicy
{
    public sealed record Role(string Id, string Label, string SourceRole, IReadOnlySet<string> VanillaBlocked);
    public sealed record Category(string Id, string Label, string NativeType);

    public static readonly IReadOnlyList<Category> Categories = new[]
    {
        new Category("weapon", "Weapons", "Weapon"),
        new Category("armor", "Armor", "Armor"),
        new Category("attach", "Attachments", "Attach"),
        new Category("ammo", "Ammo", "Ammo"),
        new Category("grenade", "Grenades", "Grenade"),
        new Category("artifact", "Artifacts", "Artifact"),
        new Category("consumable", "Consumables", "Consumable"),
        new Category("mutantLoot", "Mutant Parts", "MutantLoot")
    };

    public static readonly IReadOnlyList<Role> Roles = new[]
    {
        new Role("general", "General", "GENERAL_TRADER", new HashSet<string>(StringComparer.Ordinal)),
        new Role("medic", "Medic", "MEDIC", new HashSet<string>(StringComparer.Ordinal)
            { "Weapon", "Armor", "Ammo", "Grenade", "Attach", "MutantLoot" }),
        new Role("technician", "Technician", "TECHNICIAN", new HashSet<string>(StringComparer.Ordinal)
            { "Consumable", "Ammo", "Grenade" }),
        new Role("bartender", "Bartender", "BARTENDER", new HashSet<string>(StringComparer.Ordinal)
            { "MutantLoot" }),
        new Role("genericNpc", "Generic NPC", "GENERIC_NPC_TRADER", new HashSet<string>(StringComparer.Ordinal)
            { "Weapon", "Armor", "MutantLoot" })
    };

    public const string Prefix = "economy.traderBuys.";
    public const string AnchorId = "economy.traderBuys.general.weapon";
    public static string ParameterId(Role role, Category category) => Prefix + role.Id + "." + category.Id;
    public static bool DefaultAllowed(Role role, Category category)
        => !role.VanillaBlocked.Contains(category.NativeType);
}
