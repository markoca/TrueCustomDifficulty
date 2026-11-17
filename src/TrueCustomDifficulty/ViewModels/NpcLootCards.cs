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
                "Extra loot that can turn up on a dead stalker on top of what they normally carry. "
                + "MED adds bandages and medkits, AMMO adds ammo for the gun you are using, WATER adds a bottle of water.\n\n"
                + "The packs are smart: they show up more often when you are running low on that item and rarely when your pockets are full.\n\n"
                + "Vanilla gives Novice MED + AMMO, Stalker WATER, and no packs on Veteran or Master. Any mix is allowed.",
                "Click a chip to switch that pack on or off. Right-click a chip to restore the base difficulty's packs."),
            new(NeedBoost, Anchor, "NEED BOOST", "Need Boost",
                "How hard the bonus packs react to what you are missing. "
                + "With full pockets a pack rolls about 10% of the time; when you are out of that item it climbs to 70–90%.\n\n"
                + "FLAT keeps a flat 10% no matter what you carry. 1× is vanilla. 2× makes the packs step in much harder when you are short.\n\n"
                + "The checkbox switches the boost off: the packs then react like vanilla and your dial setting is kept for later.\n\n"
                + "Needs at least one bonus pack switched on.",
                "Click the checkbox (or Space) to switch it ON/OFF. Drag the dial or use the mouse wheel; ←/→ change it in small steps, Home = FLAT, End = 2×. Right-click or R resets to 1× and ON."),
            new(Scarcity, "loot.genericCorpseReduction", "SCARCITY", "Scarcity",
                "Makes the Zone stingier with supplies on dead stalkers. "
                + "It cuts the chance of food, drinks and meds on every ordinary NPC, and the bonus packs too.\n\n"
                + "0% is vanilla, 50% halves everything, 100% means bodies carry no supplies at all.\n\n"
                + "Weapons, armor and the ammo loaded in their guns are not affected.",
                "Drag the dial or use the mouse wheel. Right-click resets to 0%."),
            new(MedDrops, "loot.npcMedicalChance", "MED DROPS", "Med Drops",
                "How often ordinary NPCs carry bandages, medkits, army medkits and anti-rad.\n\n"
                + "1× is vanilla, 0× means no meds on bodies, 2× doubles the odds (never above 100%).\n\n"
                + "A MEDICAL item you tick below takes over from this dial for that item.",
                "Drag the dial or use the mouse wheel. Right-click resets to the base difficulty."),
            new(ModCut, Anchor, "MOD LOOT SCARCITY", "Mod Loot Scarcity",
                "Only for supported loot mods: Point of No Return Backpacks Pack and Repair System. "
                + "Cuts how often their extra items show up on bodies.\n\n"
                + "0% keeps them as the mod intended, 100% removes them completely. Vanilla loot is never touched.",
                "Tick the box to use it, then drag the dial or use the mouse wheel. Right-click or R resets to 0%."),
            new(WeaponCondition, "loot.droppedWeaponCondition", "WEAPON CONDITION", "Weapon Condition",
                "The condition of guns you pick up from dead NPCs. "
                + "Vanilla is 25–50%, which is why most looted weapons are close to junk.\n\n"
                + "Set your own MIN and MAX. The dashed line shows the lowest condition traders still buy.\n\n"
                + "Only weapons change: grenades and armor keep their vanilla condition.",
                "Drag either handle along the rail. Right-click resets to the base difficulty."),
            new(AmmoPack, "loot.npcWeaponAmmoSupply", "AMMO PACK SIZE", "Ammo Pack Size",
                "How big the extra ammo stacks from the AMMO bonus pack are. "
                + "The table shows what you would find per caliber at the current setting.\n\n"
                + "Only works while the AMMO pack is on. It does not change the ammo already loaded in their guns.",
                "Drag the slider or use the mouse wheel. Right-click resets to the base difficulty."),
            new(Food, Anchor, "FOOD", "Food",
                "Bread, sausage and canned food on ordinary NPCs. "
                + "Tick an item and set how much more or less often it appears than in vanilla (1×).\n\n"
                + "The N / E / V / M bars show the chance at each of your ranks — Newbie, Experienced, Veteran, Master. "
                + "The outline is vanilla, the colour is your setting.",
                "Tick a row to edit it, then drag its slider or use ←/→. Untick, right-click or R returns it to vanilla."),
            new(Drinks, Anchor, "DRINKS", "Drinks",
                "Energy drinks, beer and vodka on ordinary NPCs. "
                + "Tick an item and set how much more or less often it appears than in vanilla (1×).\n\n"
                + "The N / E / V / M bars show the chance at each of your ranks. "
                + "Late-game stalkers carry far more drinks than early ones.",
                "Tick a row to edit it, then drag its slider or use ←/→. Untick, right-click or R returns it to vanilla."),
            new(Medical, Anchor, "MEDICAL", "Medical",
                "Bandages, medkits, army medkits and anti-rad on ordinary NPCs. "
                + "A ticked item ignores MED DROPS and uses its own setting instead.\n\n"
                + "The N / E / V / M bars show the chance at each of your ranks. "
                + "Army medkits and anti-rad only start to appear once you reach Veteran.",
                "Tick a row to edit it, then drag its slider or use ←/→. Untick, right-click or R returns it to MED DROPS."),
        }.ToDictionary(card => card.Key, StringComparer.Ordinal);
}
