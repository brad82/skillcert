using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkillCert.Infrastructure.Seed;

/// <summary>The approved AFA Skills Record transcription (Seed/afa-skills-record.json), embedded in this assembly.</summary>
internal sealed record AfaRecordDefinition(AfaListDefinition List, AfaDefaults Defaults, IReadOnlyList<AfaNode> Nodes)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static AfaRecordDefinition Load()
    {
        using var stream = typeof(AfaRecordDefinition).Assembly.GetManifestResourceStream("SkillCert.Seed.afa-skills-record.json")
            ?? throw new InvalidOperationException("The AFA Skills Record seed file is not embedded.");
        return JsonSerializer.Deserialize<AfaRecordDefinition>(stream, Json)
            ?? throw new InvalidOperationException("The AFA Skills Record seed file is empty.");
    }
}

internal sealed record AfaListDefinition(string Title, IReadOnlyList<string> AssignedGroups);

internal sealed record AfaDefaults(int? RecertificationDays, IReadOnlyList<string> PermittedMethods);

internal sealed record AfaNode(
    string Type,
    string Code,
    string Title,
    string? ShortTitle,
    int? RecertificationDays,
    IReadOnlyList<string>? PermittedMethods,
    IReadOnlyList<AfaNode>? Children)
{
    [JsonIgnore]
    public bool IsHeading => Type == "heading";
}
