using System.Text.Json.Serialization;

namespace TrueCustomDifficulty.Models;

public sealed class CfgWriterBaseline
{
    [JsonPropertyName("writerBaselineVersion")]
    public int WriterBaselineVersion { get; set; }

    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = string.Empty;

    [JsonPropertyName("supportedParameterCount")]
    public int SupportedParameterCount { get; set; }

    [JsonPropertyName("deferredParameterIds")]
    public List<string> DeferredParameterIds { get; set; } = new();

    [JsonPropertyName("editPoints")]
    public List<CfgWriterEditPoint> EditPoints { get; set; } = new();

    [JsonPropertyName("special")]
    public CfgWriterSpecialBaseline Special { get; set; } = new();
}

public sealed class CfgWriterEditPoint
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("prototype")]
    public string Prototype { get; set; } = string.Empty;

    [JsonPropertyName("nodePath")]
    public List<string> NodePath { get; set; } = new();

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("sourceRaw")]
    public string SourceRaw { get; set; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("difficultySid")]
    public string DifficultySid { get; set; } = string.Empty;
}

public sealed class CfgWriterSpecialBaseline
{
    // Explicit source-backed route for the dedicated nine-actor shuffle writer.
    // Keeps Python audit coverage and real C# validation on one baseline.
    [JsonPropertyName("looseUniqueWeaponShuffle")]
    public CfgWriterLooseUniqueWeaponShuffle LooseUniqueWeaponShuffle { get; set; } = new();

    [JsonPropertyName("listMembership")]
    public List<CfgWriterListMember> ListMembership { get; set; } = new();

    [JsonPropertyName("setList")]
    public List<CfgWriterSetList> SetList { get; set; } = new();

    [JsonPropertyName("psyOverrides")]
    public Dictionary<string, CfgWriterPsyOverride> PsyOverrides { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("psyParameterKeys")]
    public Dictionary<string, string> PsyParameterKeys { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("cheapStashProduction")]
    public CfgWriterCheapStashProduction CheapStashProduction { get; set; } = new();

    [JsonPropertyName("mediumStashAmmoProduction")]
    public CfgWriterMediumStashAmmoProduction MediumStashAmmoProduction { get; set; } = new();

    [JsonPropertyName("expensiveStashProduction")]
    public CfgWriterExpensiveStashProduction ExpensiveStashProduction { get; set; } = new();

    [JsonPropertyName("npcMedicalLootProduction")]
    public CfgWriterNpcMedicalLootProduction NpcMedicalLootProduction { get; set; } = new();

    [JsonPropertyName("npcCorpseAmmoProduction")]
    public CfgWriterNpcCorpseAmmoProduction NpcCorpseAmmoProduction { get; set; } = new();

    [JsonPropertyName("destructibleAmmoProduction")]
    public CfgWriterDestructibleAmmoProduction DestructibleAmmoProduction { get; set; } = new();

    [JsonPropertyName("destructibleGrenadeProduction")]
    public CfgWriterDestructibleAmmoProduction DestructibleGrenadeProduction { get; set; } = new();

    [JsonPropertyName("crateSpawnProduction")]
    public CfgWriterCrateSpawnProduction CrateSpawnProduction { get; set; } = new();
}

public sealed class CfgWriterLooseUniqueWeaponShuffle
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("manifestResource")]
    public string ManifestResource { get; set; } = string.Empty;

    [JsonPropertyName("expectedActorCount")]
    public int ExpectedActorCount { get; set; }
}

// Audited 2.0.5 NPC_Ammo_Smart: 43 individually authored item/count rows
// in 24 groups across four progression rank blocks. This is NOT the 121
// weapon-attached ammo field surface and NOT DestructibleStash_AmmoSNG/NATO.
public sealed class CfgWriterNpcCorpseAmmoProduction
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("corpseChoiceParameterId")]
    public string CorpseChoiceParameterId { get; set; } = string.Empty;

    [JsonPropertyName("sourceSid")]
    public string SourceSid { get; set; } = string.Empty;

    [JsonPropertyName("targetSid")]
    public string TargetSid { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefUrl")]
    public string BaseTemplateRefUrl { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefKey")]
    public string BaseTemplateRefKey { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<CfgWriterNpcCorpseAmmoEntry> Entries { get; set; } = new();
}

public sealed class CfgWriterNpcCorpseAmmoEntry
{
    [JsonPropertyName("rankIndex")]
    public int RankIndex { get; set; }

    [JsonPropertyName("groupIndex")]
    public int GroupIndex { get; set; }

    [JsonPropertyName("itemIndex")]
    public int ItemIndex { get; set; }

    [JsonPropertyName("sourceSid")]
    public string SourceSid { get; set; } = string.Empty;

    [JsonPropertyName("minCount")]
    public int MinCount { get; set; }

    [JsonPropertyName("maxCount")]
    public int MaxCount { get; set; }
}

public sealed class CfgWriterCheapStashProduction
{
    [JsonPropertyName("ammoQuantityParameterId")]
    public string AmmoQuantityParameterId { get; set; } = string.Empty;

    [JsonPropertyName("grenadeChanceParameterId")]
    public string GrenadeChanceParameterId { get; set; } = string.Empty;

    [JsonPropertyName("grenadeQuantityParameterId")]
    public string GrenadeQuantityParameterId { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("customGeneratorSid")]
    public string CustomGeneratorSid { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefUrl")]
    public string BaseTemplateRefUrl { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefKey")]
    public string BaseTemplateRefKey { get; set; } = string.Empty;

    [JsonPropertyName("rankWrappers")]
    public List<CfgWriterCheapStashRankWrapper> RankWrappers { get; set; } = new();

    [JsonPropertyName("templateCategories")]
    public List<CfgWriterCheapStashCategory> TemplateCategories { get; set; } = new();

    [JsonPropertyName("ammoEntries")]
    public List<CfgWriterCheapStashAmmoEntry> AmmoEntries { get; set; } = new();

    [JsonPropertyName("grenadeEntry")]
    public CfgWriterCheapStashGrenadeEntry GrenadeEntry { get; set; } = new();

    [JsonPropertyName("redirectManifestResource")]
    public string RedirectManifestResource { get; set; } = string.Empty;
}

public sealed class CfgWriterCheapStashRankWrapper
{
    [JsonPropertyName("targetSid")]
    public string TargetSid { get; set; } = string.Empty;
}

public sealed class CfgWriterCheapStashCategory
{
    [JsonPropertyName("node")]
    public string Node { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("items")]
    public List<CfgWriterCheapStashItem> Items { get; set; } = new();
}

public sealed class CfgWriterCheapStashItem
{
    [JsonPropertyName("node")]
    public string Node { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.Ordinal);
}

public sealed class CfgWriterCheapStashAmmoEntry
{
    [JsonPropertyName("entryNode")]
    public string EntryNode { get; set; } = string.Empty;

    [JsonPropertyName("sourceMinCount")]
    public int SourceMinCount { get; set; }

    [JsonPropertyName("sourceMaxCount")]
    public int SourceMaxCount { get; set; }
}

public sealed class CfgWriterCheapStashGrenadeEntry
{
    [JsonPropertyName("categoryNode")]
    public string CategoryNode { get; set; } = string.Empty;

    [JsonPropertyName("entryNode")]
    public string EntryNode { get; set; } = string.Empty;

    [JsonPropertyName("sourceChanceRaw")]
    public string SourceChanceRaw { get; set; } = string.Empty;

    [JsonPropertyName("sourceMinCount")]
    public int SourceMinCount { get; set; }

    [JsonPropertyName("sourceMaxCount")]
    public int SourceMaxCount { get; set; }
}

public sealed class CfgWriterMediumStashAmmoProduction
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("customGeneratorSid")]
    public string CustomGeneratorSid { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefUrl")]
    public string BaseTemplateRefUrl { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefKey")]
    public string BaseTemplateRefKey { get; set; } = string.Empty;

    [JsonPropertyName("rankWrappers")]
    public List<CfgWriterCheapStashRankWrapper> RankWrappers { get; set; } = new();

    [JsonPropertyName("templateCategories")]
    public List<CfgWriterCheapStashCategory> TemplateCategories { get; set; } = new();

    [JsonPropertyName("ammoEntries")]
    public List<CfgWriterMediumStashAmmoEntry> AmmoEntries { get; set; } = new();

    [JsonPropertyName("redirectManifestResource")]
    public string RedirectManifestResource { get; set; } = string.Empty;
}

public sealed class CfgWriterMediumStashAmmoEntry
{
    [JsonPropertyName("categoryNode")]
    public string CategoryNode { get; set; } = string.Empty;

    [JsonPropertyName("entryNode")]
    public string EntryNode { get; set; } = string.Empty;

    [JsonPropertyName("sourceMinCount")]
    public int SourceMinCount { get; set; }

    [JsonPropertyName("sourceMaxCount")]
    public int SourceMaxCount { get; set; }
}


public sealed class CfgWriterExpensiveStashProduction
{
    [JsonPropertyName("ammoQuantityParameterId")]
    public string AmmoQuantityParameterId { get; set; } = string.Empty;

    [JsonPropertyName("couponChanceParameterId")]
    public string CouponChanceParameterId { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("customGeneratorSid")]
    public string CustomGeneratorSid { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefUrl")]
    public string BaseTemplateRefUrl { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefKey")]
    public string BaseTemplateRefKey { get; set; } = string.Empty;

    [JsonPropertyName("rankWrappers")]
    public List<CfgWriterCheapStashRankWrapper> RankWrappers { get; set; } = new();

    [JsonPropertyName("templateCategories")]
    public List<CfgWriterCheapStashCategory> TemplateCategories { get; set; } = new();

    [JsonPropertyName("ammoEntries")]
    public List<CfgWriterMediumStashAmmoEntry> AmmoEntries { get; set; } = new();

    [JsonPropertyName("couponEntries")]
    public List<CfgWriterExpensiveStashCouponEntry> CouponEntries { get; set; } = new();

    [JsonPropertyName("redirectManifestResource")]
    public string RedirectManifestResource { get; set; } = string.Empty;
}

public sealed class CfgWriterExpensiveStashCouponEntry
{
    [JsonPropertyName("categoryNode")]
    public string CategoryNode { get; set; } = string.Empty;

    [JsonPropertyName("entryNode")]
    public string EntryNode { get; set; } = string.Empty;

    [JsonPropertyName("difficultySids")]
    public List<string> DifficultySids { get; set; } = new();

    [JsonPropertyName("sourceChanceRaw")]
    public string SourceChanceRaw { get; set; } = string.Empty;
}

public sealed class CfgWriterNpcMedicalLootProduction
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefUrl")]
    public string BaseTemplateRefUrl { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefKey")]
    public string BaseTemplateRefKey { get; set; } = string.Empty;

    [JsonPropertyName("pools")]
    public List<CfgWriterNpcMedicalPool> Pools { get; set; } = new();

    [JsonPropertyName("redirectManifestResource")]
    public string RedirectManifestResource { get; set; } = string.Empty;
}

public sealed class CfgWriterNpcMedicalPool
{
    [JsonPropertyName("vanillaSid")]
    public string VanillaSid { get; set; } = string.Empty;

    [JsonPropertyName("customSid")]
    public string CustomSid { get; set; } = string.Empty;

    [JsonPropertyName("templateCategories")]
    public List<CfgWriterCheapStashCategory> TemplateCategories { get; set; } = new();

    [JsonPropertyName("medicalEntries")]
    public List<CfgWriterNpcMedicalEntry> MedicalEntries { get; set; } = new();
}

public sealed class CfgWriterNpcMedicalEntry
{
    [JsonPropertyName("categoryNode")]
    public string CategoryNode { get; set; } = string.Empty;

    [JsonPropertyName("entryNode")]
    public string EntryNode { get; set; } = string.Empty;

    [JsonPropertyName("sourceChanceRaw")]
    public string SourceChanceRaw { get; set; } = string.Empty;
}

public sealed class CfgWriterDestructibleAmmoProduction
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefUrl")]
    public string BaseTemplateRefUrl { get; set; } = string.Empty;

    [JsonPropertyName("baseTemplateRefKey")]
    public string BaseTemplateRefKey { get; set; } = string.Empty;

    [JsonPropertyName("pools")]
    public List<CfgWriterDestructibleAmmoPool> Pools { get; set; } = new();

    [JsonPropertyName("redirectManifestResource")]
    public string RedirectManifestResource { get; set; } = string.Empty;

    [JsonPropertyName("weightParameterIds")]
    public Dictionary<string, string> WeightParameterIds { get; set; } = new(StringComparer.Ordinal);
}

public sealed class CfgWriterDestructibleAmmoPool
{
    [JsonPropertyName("sourceSid")]
    public string SourceSid { get; set; } = string.Empty;

    [JsonPropertyName("targetSid")]
    public string TargetSid { get; set; } = string.Empty;

    [JsonPropertyName("categoryNode")]
    public string CategoryNode { get; set; } = string.Empty;

    [JsonPropertyName("categoryFields")]
    public Dictionary<string, string> CategoryFields { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("items")]
    public List<CfgWriterDestructibleAmmoItem> Items { get; set; } = new();
}

public sealed class CfgWriterDestructibleAmmoItem
{
    [JsonPropertyName("node")]
    public string Node { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("sourceMinCount")]
    public int SourceMinCount { get; set; }

    [JsonPropertyName("sourceMaxCount")]
    public int SourceMaxCount { get; set; }
}


public sealed class CfgWriterCrateSpawnProduction
{
    [JsonPropertyName("manifestResource")]
    public string ManifestResource { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<CfgWriterCrateSpawnCategory> Categories { get; set; } = new();
}

public sealed class CfgWriterCrateSpawnCategory
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("expectedActorCount")]
    public int ExpectedActorCount { get; set; }
}

public sealed class CfgWriterListMember
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("member")]
    public string Member { get; set; } = string.Empty;
}

public sealed class CfgWriterSetList
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

public sealed class CfgWriterPsyOverride
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("effectSid")]
    public string EffectSid { get; set; } = string.Empty;
}


public sealed class CfgWriteResult
{
    public string OutputRoot { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public int ParameterCount { get; init; }
    public int EditCount { get; init; }
}
