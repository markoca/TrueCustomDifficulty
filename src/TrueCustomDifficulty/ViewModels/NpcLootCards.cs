namespace TrueCustomDifficulty.ViewModels;

/// <summary>
/// One clickable card of the NPC LOOT &amp; EQUIPMENT board: its on-card
/// caption plus the gamer-facing title, explanation and control hint shown
/// in the right-hand panel when the card is clicked. <see cref="OwnerId"/> is
/// the catalog parameter the panel binds to (sidecar cards use the board
/// anchor); the card text replaces that parameter's technical sections.
/// </summary>
public sealed record NpcLootCard(
    string Key, string OwnerId, string Caption, string Title, string Text, string Controls);

public static class NpcLootCards
{
    public const string Packs = "packs";
    public const string NeedBoost = "needBoost";
    public const string Scarcity = "scarcity";
    public const string MedDrops = "medDrops";
    public const string ModCut = "modCut";
    public const string WeaponCondition = "weaponCondition";
    public const string AmmoPack = "ammoPack";
    public const string Food = "FOOD";
    public const string Drinks = "DRINKS";
    public const string Medical = "MEDICAL";

    private const string Anchor = "loot.npcCorpseBonus";

    public static IReadOnlyDictionary<string, NpcLootCard> ByKey { get; } =
        new NpcLootCard[]
        {
            new(Packs, Anchor, "BONUS PACKS", "Bonus Packs",
                "Smart loot on bodies: a dead stalker may carry a bonus pack of what you are short on. MED = bandages and medkits, AMMO = rounds for your gun, WATER = a bottle of water. Vanilla: Novice MED + AMMO, Stalker WATER, Veteran and Master none. Any mix is allowed.",
                "Click a chip to switch that pack on or off. Right-click a chip to restore the base difficulty's packs."),
            new(NeedBoost, Anchor, "NEED BOOST", "Need Boost",
                "How hard the bonus packs react to what you are missing: about 10% with full pockets, up to 70–90% when you are out of that item. Vanilla: 1×. FLAT keeps a flat 10% no matter what you carry; 2× makes the packs step in much harder when you are short. Needs SMART LOOT ON and at least one bonus pack.",
                "Drag the dial or use the mouse wheel; ←/→ change it in small steps, Home = FLAT, End = 2×. Right-click or R resets to 1×."),
            new(Scarcity, "loot.genericCorpseReduction", "SCARCITY", "Scarcity",
                "Makes dead stalkers stingier with supplies: food, drinks, meds and the bonus packs all turn up less often on ordinary NPCs. Vanilla: 0%. 50% halves everything; 100% means bodies carry no supplies at all. Weapons, armor and the ammo loaded in their guns are not affected.",
                "Drag the dial or use the mouse wheel. Right-click resets to 0%."),
            new(MedDrops, "loot.npcMedicalChance", "MED DROPS", "Med Drops",
                "How often ordinary NPCs carry bandages, medkits, army medkits and anti-rad. Vanilla: 1×. 0× means no meds on bodies; 2× doubles the odds (never above 100%). A MEDICAL item you tick below takes over from this dial for that item.",
                "Drag the dial or use the mouse wheel. Right-click resets to the base difficulty."),
            new(ModCut, Anchor, "MOD LOOT SCARCITY", "Mod Loot Scarcity",
                "Only for supported loot mods (Point of No Return Backpacks Pack and Repair System): cuts how often their extra items show up on bodies. Vanilla: 0%, as the mod intended. 100% removes them completely; vanilla loot is never touched.",
                "Tick the box to use it, then drag the dial or use the mouse wheel. Right-click or R resets to 0%."),
            new(WeaponCondition, "loot.droppedWeaponCondition", "DROPPED WEAPON CONDITION", "Dropped Weapon Condition",
                "The condition of guns you pick up from dead NPCs. Vanilla: 25–50%, which is why most looted weapons are close to junk. Set your own MIN and MAX; the dashed line marks the lowest condition traders still buy. Only weapons change: grenades and armor keep their vanilla condition.",
                "Drag either handle along the rail. Right-click resets to the base difficulty."),
            new(AmmoPack, "loot.npcWeaponAmmoSupply", "AMMO PACK SIZE", "Ammo Pack Size",
                "How big the extra ammo stacks from the AMMO bonus pack are; the table shows what you would find per caliber at the current setting. Vanilla: 1×. Only works while the AMMO pack is on; the ammo already loaded in their guns does not change.",
                "Drag the slider or use the mouse wheel. Right-click resets to the base difficulty."),
            new(Food, Anchor, "FOOD", "Food",
                "Bread, sausage and canned food on ordinary NPCs. Tick an item and set how often it appears compared with vanilla (1×). The N / E / V / M bars show the chance at each of your ranks (Newbie, Experienced, Veteran, Master): outline = vanilla, colour = your setting.",
                "Tick a row to edit it, then drag its slider or use ←/→. Untick, right-click or R returns it to vanilla."),
            new(Drinks, Anchor, "DRINKS", "Drinks",
                "Energy drinks, beer and vodka on ordinary NPCs. Tick an item and set how often it appears compared with vanilla (1×). The N / E / V / M bars show the chance at each of your ranks; late-game stalkers carry far more drinks than early ones.",
                "Tick a row to edit it, then drag its slider or use ←/→. Untick, right-click or R returns it to vanilla."),
            new(Medical, Anchor, "MEDICAL", "Medical",
                "Bandages, medkits, army medkits and anti-rad on ordinary NPCs. A ticked item ignores MED DROPS and uses its own setting instead. The N / E / V / M bars show the chance at each of your ranks; army medkits and anti-rad only start to appear once you reach Veteran.",
                "Tick a row to edit it, then drag its slider or use ←/→. Untick, right-click or R returns it to MED DROPS."),
        }.ToDictionary(card => card.Key, StringComparer.Ordinal);
}
