namespace SkillCert.Domain.Competencies;

/// <summary>Kinds of URL resource a revision can link to (spec §3). Uploaded files are out of POC scope.</summary>
public enum ResourceType
{
    WebPage,
    Video,
    Document,
}
