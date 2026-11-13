using System;
using System.Collections.Generic;
using System.Linq;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Exact 2.1 merchant SELL inventory categories, separate from native EItemType
/// buy restrictions. A disabled cell removes this category only for the
/// selected merchant role. Unavailable cells are deliberately not controls.
/// </summary>
public static class TraderSellsPolicy
{
    public sealed record Category(string Id, string Label);
    public static readonly IReadOnlyList<TraderBuysPolicy.Role> Roles = TraderBuysPolicy.Roles;

    public static readonly IReadOnlyList<Category> Categories = new[]
    {
        new Category("weapons", "Weapons"),
        new Category("armor", "Armor"),
        new Category("attachments", "Attachments"),
        new Category("ammo", "Ammo"),
        new Category("medical", "Medical"),
        new Category("food", "Food"),
        new Category("drinks", "Drinks"),
        new Category("artifacts", "Artifacts"),
        new Category("detectors", "Detectors"),
        new Category("grenades", "Grenades")
    };
    // Source-exact 86-playable-profile 2.1 inventory union; an absent combination
    // would be a no-op and must never become a misleading editable setting.
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Available =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["general"] = new HashSet<string>(StringComparer.Ordinal)
                { "weapons", "armor", "attachments", "ammo", "medical", "food", "drinks", "artifacts", "detectors", "grenades" },
            ["medic"] = new HashSet<string>(StringComparer.Ordinal)
                { "medical", "drinks", "artifacts", "detectors" },
            ["technician"] = new HashSet<string>(StringComparer.Ordinal)
                { "weapons", "attachments", "ammo", "medical", "food", "drinks" },
            ["bartender"] = new HashSet<string>(StringComparer.Ordinal)
                { "weapons", "ammo", "food", "drinks", "artifacts", "detectors" },
            ["genericNpc"] = new HashSet<string>(StringComparer.Ordinal)
                { "ammo", "medical", "food", "drinks", "grenades" }
        };

    public static readonly IReadOnlySet<string> Dlc1OnlyParameterIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "economy.traderSells.technician.weapons",
        "economy.traderSells.technician.ammo",
        "economy.traderSells.technician.medical",
        "economy.traderSells.technician.food",
        "economy.traderSells.technician.drinks"
    };

    public const string Prefix = "economy.traderSells.";
    public const string AnchorId = "economy.traderSells.general.weapons";
    public static bool IsAvailable(TraderBuysPolicy.Role role, Category category)
        => Available[role.Id].Contains(category.Id);
    public static string ParameterId(TraderBuysPolicy.Role role, Category category)
        => Prefix + role.Id + "." + category.Id;
    public static IEnumerable<string> ParameterIds =>
        Roles.SelectMany(role => Categories.Where(category => IsAvailable(role, category))
            .Select(category => ParameterId(role, category)));
}
