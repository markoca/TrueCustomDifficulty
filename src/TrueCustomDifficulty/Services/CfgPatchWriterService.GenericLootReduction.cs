using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// Two optional-supply reduction sources.  Native Weight remains a within-pool
// distribution, not a fake whole-container RNG Chance.  No weapon/loadout,
// protected actor, scripted corpse, extra gear, or attached-ammo edit points.
public sealed partial class CfgPatchWriterService
{
    private const string NpcSupplyReductionId = "loot.genericCorpseReduction";
    private const string StashSupplyReductionId = "loot.genericStashReduction";
    // Logical sidecar editor id (not a catalog parameter).
    internal const string NpcNeedBoostEditorId = "corpseSmartLootNeedBoost";
    // Every authored corpse SmartLoot group uses MinSpawnChance = 0.1f.
    private const double NpcBonusMinSpawnChance = 0.1;

    private static double GenericSupplyRemaining(
        IReadOnlyDictionary<string, ParameterViewModel> all, string id)
    {
        if (!all.TryGetValue(id, out var parameter))
            throw new InvalidDataException("Missing generic supply reduction source: " + id);
        var value = parameter.CurrentNumber;
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
            throw new InvalidDataException("Invalid generic supply reduction: " + id);
        return 1.0 - value;
    }

    // Actual item Chance is a separate per-entry native roll.
    internal static string ReduceNativeChance(string raw, double remaining)
        => remaining >= 1.0 ? raw
            : FormatLikeSource(Math.Clamp(ParseNumber(raw) * remaining, 0.0, 1.0), raw);

    // ItemGenerator has no authored group-wide activation chance. For its
    // Weight groups reduce literal stack counts while preserving category
    // eligibility and relative Weight selection. At q>0 a one-item bundle
    // cannot be fractionally reduced: keep one. q=0 writes both counts zero.
    internal static string ReduceNativeCount(string raw, double remaining)
    {
        if (remaining >= 1.0) return raw;
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var amount) || amount < 0)
            throw new InvalidDataException("Invalid generic stash count: " + raw);
        if (amount == 0 || remaining <= 0.0) return "0";
        return Math.Max(1, (int)Math.Floor(amount * remaining + 0.5))
            .ToString(CultureInfo.InvariantCulture);
    }

    // Original 2.1 StashPrototypes.cfg group endpoints, transcribed from
    // SMARTLOOT-CORPSE-BONUS-ENTRY-CATALOG.md. Four ranks, exactly 39 groups.
    // Do not infer a generic corpses Chance from an ammo ItemSet or Weight.
    private static readonly IReadOnlyDictionary<string, double[][]> NpcBonusGroupMax =
        new Dictionary<string, double[][]>(StringComparer.Ordinal)
        {
            ["NPC_Medicine_Smart"] = new[]
            {
                new[] { .7, .7 },
                new[] { .7, .7, .7 },
                new[] { .8, .7, .7 },
                new[] { .7, .7, .7 }
            },
            ["NPC_Ammo_Smart"] = new[]
            {
                new[] { .8, .9, .8, .8 },
                new[] { .8, .8, .8, .8, .8 },
                new[] { .8, .8, .8, .8, .8, .8, .8 },
                new[] { .8, .8, .8, .8, .8, .8, .8, .8 }
            },
            ["NPC_Water_Smart"] = new[]
            {
                new[] { .7 }, new[] { .7 }, new[] { .7 }, new[] { .7 }
            }
        };

    private static int WriteNpcBonusSupplyReduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        IReadOnlyDictionary<string, ParameterViewModel> all,
        string difficultySid,
        double needBoost,
        IDictionary<string, PatchDocument> documents,
        ISet<string> written)
    {
        // Two composable transforms of the same SmartLoot endpoints:
        // NEED BOOST stretches the need-driven span Min→Max (0 = flat Min,
        // 1 = vanilla, 2 = doubled span), SCARCITY then scales both.
        if (!double.IsFinite(needBoost) || needBoost < 0.0 || needBoost > 2.0)
            throw new InvalidDataException("Corpse SmartLoot NEED BOOST must be 0–2x.");
        var reductionActive = modified.ContainsKey(NpcSupplyReductionId);
        var boostActive = Math.Abs(needBoost - 1.0) > 0.000001;
        if (!reductionActive && !boostActive) return 0;
        var remaining = reductionActive
            ? GenericSupplyRemaining(all, NpcSupplyReductionId) : 1.0;
        if (remaining >= 1.0 && !boostActive) return 0;
        if (!all.TryGetValue("loot.npcCorpseBonus", out var choice))
            throw new InvalidDataException("Missing Corpse SmartLoot selector.");
        var selected = choice.CurrentText.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Any canonical subset of the three authored packages is valid.
        if (selected.Any(sid => !NpcBonusGroupMax.ContainsKey(sid))
            || !NpcCorpsePackages.IsCanonical(choice.CurrentText))
            throw new InvalidDataException("Unexpected generic NPC bonus SmartLoot list.");

        var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
        var edits = 0;
        if (remaining > 0.0)
        {
            var config = baseline.Special.NpcCorpseAmmoProduction;
            foreach (var sid in selected)
            {
                // Compose with the pre-existing bonus-ammo *stack size* clone,
                // never create two competing definitions of the same package.
                var target = sid == config.SourceSid && modified.ContainsKey(config.ParameterId)
                    ? config.TargetSid
                    : "TCD_Supply_" + sid;
                var root = GetDocument(documents, "GameData/StashPrototypes.cfg")
                    .GetOrAddReferencedRoot(target, "../StashPrototypes.cfg", sid);
                root.SetAssignment("SID", target);
                edits++;

                var sourceGroups = NpcBonusGroupMax[sid];
                if (sourceGroups.Length != 4)
                    throw new InvalidDataException("NPC source rank drift: " + sid);
                for (var rank = 0; rank < sourceGroups.Length; rank++)
                {
                    var groupName = sid == "NPC_Ammo_Smart"
                        ? "PrimaryWeaponParams" : "ConsumablesParams";
                    for (var group = 0; group < sourceGroups[rank].Length; group++)
                    {
                        var node = root.GetOrAddChild("ItemGenerators", useBpatch: true)
                            .GetOrAddChild($"[{rank}]", useBpatch: true)
                            .GetOrAddChild("SmartLootParams", useBpatch: true)
                            .GetOrAddChild(groupName, useBpatch: true)
                            .GetOrAddChild($"[{group}]", useBpatch: true);
                        var boostedMax = Math.Clamp(NpcBonusMinSpawnChance
                            + (sourceGroups[rank][group] - NpcBonusMinSpawnChance) * needBoost,
                            0.0, 1.0);
                        node.SetAssignment("MinSpawnChance",
                            FormatLikeSource(NpcBonusMinSpawnChance * remaining, "0.1f"));
                        node.SetAssignment("MaxSpawnChance",
                            FormatLikeSource(boostedMax * remaining, "0.7f"));
                        edits += 2;
                    }
                }
                replacements.Add(sid, target);
            }
        }

        // 100%: the existing exact CorpseSmartLoot=None removal mechanism is
        // the authoritative OFF state. Persisted package choice is untouched.
        // 1–99%: link only the source-backed, privately cloned packages.
        if (selected.Length > 0)
        {
            var economy = GetDocument(documents, "GameData/DifficultyPrototypes.cfg")
                .GetOrAddNode(difficultySid, new[] { "EconomyDifficulty" });
            economy.RemoveChild("CorpseSmartLoot");
            economy.RemoveAssignment("CorpseSmartLoot");
            if (remaining <= 0.0)
                economy.SetAssignment("CorpseSmartLoot", string.Empty);
            else
            {
                var child = economy.GetOrAddChild("CorpseSmartLoot", useBpatch: false);
                child.Clear();
                child.SetAssignment("GeneralNPC", string.Join(", ",
                    selected.Select(sid => replacements[sid])));
            }
            edits++;
        }

        // Ordinary NPC Chance writer also emits 104 rows, including a
        // source-edited 100% sid, so a master always has concrete output.
        if (reductionActive)
            written.Add(NpcSupplyReductionId);
        if (boostActive && selected.Length > 0 && remaining > 0.0)
            written.Add(NpcNeedBoostEditorId);
        return edits;
    }
}
