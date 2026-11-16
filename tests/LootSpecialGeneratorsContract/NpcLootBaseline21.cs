using System.Reflection;
using TrueCustomDifficulty.Controls;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    private static void VerifyNpcLootBaseline21()
    {
        // Source-level constraints: unrelated generators must never be treated
        // as one single "NPC loot" knob. This does not prove game RNG behavior.
        Check(Baseline.GameVersion == "2.1",
            "NPC loot contract requires the 2.1 CFG baseline");
        var corpse = Baseline.Special.SetList.Single(p =>
            p.ParameterId == "loot.npcCorpseBonus");
        Check(corpse.File == "GameData/DifficultyPrototypes.cfg"
              && corpse.Path == "EconomyDifficulty.CorpseSmartLoot.GeneralNPC",
            "Corpse SmartLoot selection source route drifted");

        var med = Baseline.Special.NpcMedicalLootProduction;
        Check(med.ParameterId == "loot.npcMedicalChance"
              && med.File == "GameData/ItemGeneratorPrototypes.cfg"
              && med.Pools.Count == 6
              && med.Pools.Sum(p => p.MedicalEntries.Count) == 58
              && med.Pools.Sum(p => p.TemplateCategories.Sum(c => c.Items.Count)) == 104,
            "Standard NPC medical pool/source scope drifted");

        var bonusAmmo = Baseline.Special.NpcCorpseAmmoProduction;
        Check(bonusAmmo.ParameterId == "loot.npcWeaponAmmoSupply"
              && bonusAmmo.CorpseChoiceParameterId == "loot.npcCorpseBonus"
              && bonusAmmo.SourceSid == "NPC_Ammo_Smart"
              && bonusAmmo.File == "GameData/StashPrototypes.cfg"
              && bonusAmmo.Entries.Count == 43
              && bonusAmmo.Entries.Select(e => (e.RankIndex, e.GroupIndex))
                    .Distinct().Count() == 24,
            "Conditional NPC bonus-ammo source route drifted");

        var condition = Baseline.EditPoints.Where(p =>
            p.ParameterId == "loot.droppedWeaponCondition").ToArray();
        // 2.1 ordinary-human scope: 33 original guard/custom roots plus
        // 59 roaming GeneralNPC_* / remaining GuardNPC_* roots (4 DLC1 NORD).
        Check(condition.Length == 1476
              && condition.Select(p => p.Prototype).Distinct().Count() == 92
              && condition.GroupBy(p => (p.Prototype,
                    Path: string.Join("/", p.NodePath))).Count() == 738
              && condition.All(p => p.Operation == "setRangeBound"
                    && (p.Scope == "baseGame"
                        ? p.File == "GameData/ItemGeneratorPrototypes.cfg"
                        : p.Scope == "dlc1"
                          && p.File == "DLCGameData/DLC1/ItemGeneratorPrototypes.cfg"))
              && condition.Count(p => p.Scope == "dlc1") == 80,
            "Native NPC weapon-condition rows drifted");

        // The original UI disabled BOTH Medical and Ammo on Water/None.
        // This was wrong: standard medical is an independent ItemGenerator
        // route and must remain interactive on Veteran's default None.
        var gate = typeof(AggregateVisualControl).GetMethod(
            "IsNpcBonusSliderLocked", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(gate is not null,
            "NPC bonus-only UI gate is missing");
        foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
        {
            var vm = new MainViewModel(Catalog)
            {
                SelectedReferenceDifficulty = difficulty
            };
            var corpseChoice = vm.AllParameters.Single(p =>
                p.Id == "loot.npcCorpseBonus");
            // Any subset of the three authored packages (8) is selectable;
            // the three vanilla lists must be among them byte-for-byte.
            Check(corpseChoice.ChoiceOptions.Count == 8
                && corpseChoice.ChoiceOptions.Select(x => x.Value)
                    .SequenceEqual(NpcCorpsePackages.AllCombinations)
                && corpseChoice.ChoiceOptions.Any(x =>
                    x.Value == "NPC_Medicine_Smart, NPC_Ammo_Smart")
                && corpseChoice.ChoiceOptions.Any(x =>
                    x.Value == "NPC_Water_Smart")
                && corpseChoice.ChoiceOptions.Any(x => x.Value == string.Empty),
                difficulty + ": corpse SmartLoot choices must be the 8 canonical package subsets");
            var medical = vm.AllParameters.Single(p =>
                p.Id == "loot.npcMedicalChance");
            var ammo = vm.AllParameters.Single(p =>
                p.Id == "loot.npcWeaponAmmoSupply");
            var board = new AggregateVisualControl
            {
                Parameters = vm.AllParameters,
                Family = "NpcLootLoadout"
            };
            foreach (var (choice, expectedAmmoLocked) in new[]
            {
                (string.Empty, true),
                ("NPC_Water_Smart", true),
                ("NPC_Medicine_Smart, NPC_Ammo_Smart", false)
            })
            {
                corpseChoice.CurrentText = choice;
                var medicalLocked = (bool)gate!.Invoke(board, new object?[] { medical })!;
                var ammoLocked = (bool)gate.Invoke(board, new object?[] { ammo })!;
                Check(!medicalLocked,
                    difficulty + "/" + choice
                    + ": standard NPC medical must stay editable independently");
                Check(ammoLocked == expectedAmmoLocked,
                    difficulty + "/" + choice
                    + ": bonus ammo must depend on NPC_Ammo_Smart only");
            }
        }

        // Every NPC LOOT card owns a real catalog parameter and replaces its
        // technical panel sections with gamer-facing text while selected.
        {
            var cardVm = new MainViewModel(Catalog);
            Check(NpcLootCards.ByKey.Count == 10, "NPC LOOT must expose ten cards");
            foreach (var card in NpcLootCards.ByKey.Values)
            {
                var owner = cardVm.AllParameters.SingleOrDefault(p => p.Id == card.OwnerId);
                Check(owner is not null && card.Caption.Length > 0 && card.Title.Length > 0
                    && card.Text.Length > 80 && card.Controls.Length > 20,
                    "NPC LOOT card is incomplete or has no owner: " + card.Key);
                owner!.PanelCard = card;
                Check(owner.PanelTitle == card.Title && owner.PanelRationale == card.Text
                    && owner.PanelControlHelp == card.Controls
                    && !owner.PanelHasLogic && !owner.PanelHasInteraction
                    && !owner.PanelHasValueMeaning,
                    "NPC LOOT card did not take over the panel: " + card.Key);
                owner.PanelCard = null;
                Check(owner.PanelTitle == owner.Label && owner.PanelRationale == owner.RationaleDisplay,
                    "Clearing an NPC LOOT card did not restore the parameter panel: " + card.Key);
            }
        }

        Console.WriteLine(
            "PASS NPC 2.1 baseline: corpse choice, 6/58/104 medical, 43 bonus ammo, "
            + "738 weapon rows / 92 roots; UI medical independent in all four difficulties");
    }
}
