using System;
using System.Collections.Generic;
using System.IO;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// ARTIFACT HUNTING — RARE LUCK (docs/environment/ARTIFACT-HUNTING.md).
// RESPAWN RATE, SPAWN CHANCE and ARTIFACTS PER SPAWN are direct edit points;
// only the rarity split needs the four shares of one rank together.
public sealed partial class CfgPatchWriterService
{
    private static int WriteArtifactRareLuck(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        if (!modifiedById.TryGetValue(ArtifactSpawnPolicy.RareLuckId, out var parameter)
            || !parameter.IsValueModifiedFromReference())
            return 0;

        var luck = parameter.CurrentNumber;
        if (!double.IsFinite(luck) || luck <= 0.0)
            throw new InvalidDataException("Invalid rare artifact luck.");

        var document = GetDocument(documents, ArtifactSpawnPolicy.File);
        var edits = 0;
        foreach (var spawner in ArtifactSpawnPolicy.Spawners)
        {
            for (var rank = 0; rank < ArtifactSpawnPolicy.Ranks.Count; rank++)
            {
                var vanilla = ArtifactSpawnPolicy.Vanilla(rank);
                // A zone without rare artifacts (Newbie) keeps its split.
                if (vanilla[2] + vanilla[3] <= 0.0)
                    continue;
                var shares = ArtifactSpawnPolicy.Rarity(rank, luck);
                var path = new[] { ArtifactSpawnPolicy.Ranks[rank], "RarityChance" };
                for (var r = 0; r < ArtifactSpawnPolicy.Rarities.Count; r++)
                {
                    document.AddAssignment(spawner, path, ArtifactSpawnPolicy.Rarities[r],
                        FormatLikeSource(shares[r], ArtifactSpawnPolicy.VanillaRarityRaw[rank][r]));
                    edits++;
                }
            }
        }

        writtenParameters.Add(ArtifactSpawnPolicy.RareLuckId);
        return edits;
    }
}
