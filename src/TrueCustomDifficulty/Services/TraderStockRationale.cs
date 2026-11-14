using System;
using System.Linq;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Read-only, source-verified 2.1 NPC-to-TradePrototype stock category reference.
/// This informational list never changes source-category editor membership
/// or per-trader CFG write behavior. Stage-specific profiles stay distinct.
/// </summary>
public static class TraderStockRationale
{
    private static readonly (string Name, string Scope, string TradeSid, string Categories)[] NamedProfiles =
    {
        ("Barkeep (Rostok)", "base", "Bartender_Rostok_TradePrototype", "Weapons, Food, Drinks, Artifacts, Detectors"),
        ("Ganja (Rostok Arena)", "base", "Bartender_RostokArena_TradePrototype", "Weapons, Ammo, Food, Drinks"),
        ("Ragman (Rostok)", "base", "Trader_Soviet_Rostok_TradePrototype", "Weapons, Attachments, Ammo, Medical, Food, Drinks, Artifacts, Grenades"),
        ("Garpun (DLC1)", "dlc1", "GarpunTrader_TradePrototype", "Weapons, Ammo, Medical, Food, Drinks"),
        ("Suslov (DLC1)", "dlc1", "SuslovTrader_TradePrototype", "Weapons, Ammo, Medical, Food, Drinks"),
        ("Suslov (DLC1, MQ11)", "dlc1", "SuslovTraderMQ11_TradePrototype", "Ammo, Medical, Food, Drinks"),
        ("Suslov (DLC1, SQ14)", "dlc1", "SuslovTraderSQ14_TradePrototype", "Weapons, Armor, Ammo, Medical, Food, Drinks"),
        ("Suslov (DLC1, Stage 2)", "dlc1", "SuslovTraderStage2_TradePrototype", "Weapons, Ammo, Medical, Food, Drinks"),
        ("Gurza (DLC1)", "dlc1", "GurzaTrader_TradePrototype", "Weapons, Ammo, Medical, Food, Drinks"),
        ("Gurza (DLC1, MQ11)", "dlc1", "GurzaTraderMQ11_TradePrototype", "Ammo, Medical, Food, Drinks"),
        ("Gurza (DLC1, SQ14)", "dlc1", "GurzaTraderSQ14_TradePrototype", "Weapons, Armor, Ammo, Medical, Food, Drinks"),
        ("Gurza (DLC1, Stage 2)", "dlc1", "GurzaTraderStage2_TradePrototype", "Weapons, Ammo, Medical, Food, Drinks"),
        ("Skoba (DLC1)", "dlc1", "SkobaMedic_TradePrototype", "Medical, Drinks"),
        ("Skoba (DLC1, Stage 2)", "dlc1", "SkobaMedicStage2_TradePrototype", "Medical, Drinks"),
        ("Veza (DLC1)", "dlc1", "VezaMedic_TradePrototype", "Medical, Drinks"),
        ("Veza (DLC1, Stage 2)", "dlc1", "VezaMedicStage2_TradePrototype", "Medical, Drinks"),
        ("Mednyi (DLC1)", "dlc1", "MednyiTrader_TradePrototype", "Weapons, Ammo, Medical, Food, Drinks"),
        ("Gorcak (DLC1)", "dlc1", "GorcakBartender_TradePrototype", "Food, Drinks"),
        ("Kolba (DLC1)", "dlc1", "KolbaBartender_TradePrototype", "Food, Drinks"),
        ("Bereza (DLC1)", "dlc1", "BerezaDLCTrader_TradePrototype", "Weapons, Armor, Attachments, Ammo, Medical, Food, Drinks, Binoculars"),
        ("Bolshak (DLC1, technician)", "dlc1", "BolshakTechnician_TradePrototype", "Weapons, Ammo, Medical, Food, Drinks"),
    };

    public static readonly string NamedCategories =
        "NAMED TRADER STOCK (2.1 SOURCE)" + Environment.NewLine +
        "Possible stock categories for verified NPC-to-trade profiles, not guaranteed items:" +
        Environment.NewLine +
        string.Join(Environment.NewLine,
            NamedProfiles.Select(p => p.Name + " — " + p.Categories)) +
        Environment.NewLine + Environment.NewLine +
        "MQ11, SQ14 and Stage 2 are distinct story-stage inventories. Binoculars are source-listed but have no category switch. Technician stock is shown for reference only; technicians have no Trader Stock tab. Trader Stock checkboxes change whole merchant roles, not one named trader. Availability still depends on the original chance, rank and refresh rules.";
}
