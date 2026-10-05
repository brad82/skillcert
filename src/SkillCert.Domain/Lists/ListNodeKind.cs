namespace SkillCert.Domain.Lists;

public enum ListNodeKind
{
    /// <summary>A section or sub-section; may contain headings and competencies.</summary>
    Heading,

    /// <summary>A leaf referencing a library competency; never has children.</summary>
    Competency,
}
