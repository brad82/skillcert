namespace SkillCert.Domain.Lists;

/// <summary>
/// One node of a list's unified tree (spec §4): a heading or a competency leaf, ordered explicitly among
/// its siblings by <see cref="SortOrder"/>. Changed only through <see cref="CompetencyList"/>.
/// </summary>
public sealed class CompetencyListNode
{
    private CompetencyListNode()
    {
    }

    private CompetencyListNode(Guid listId, Guid? parentNodeId, ListNodeKind kind)
    {
        Id = Guid.CreateVersion7();
        CompetencyListId = listId;
        ParentNodeId = parentNodeId;
        Kind = kind;
    }

    public Guid Id { get; private set; }

    public Guid CompetencyListId { get; private set; }

    /// <summary>Null for top-level nodes. Always a heading node of the same list.</summary>
    public Guid? ParentNodeId { get; private set; }

    public ListNodeKind Kind { get; private set; }

    /// <summary>Heading code as printed on the record ("4", "4.1"); headings only, optional.</summary>
    public string? HeadingCode { get; private set; }

    /// <summary>Heading text; headings only.</summary>
    public string? HeadingTitle { get; private set; }

    /// <summary>The library competency; competency leaves only.</summary>
    public Guid? CompetencyId { get; private set; }

    /// <summary>Position among siblings, 0-based and contiguous.</summary>
    public int SortOrder { get; internal set; }

    internal static CompetencyListNode Heading(Guid listId, Guid? parentNodeId, string? code, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new CompetencyListNode(listId, parentNodeId, ListNodeKind.Heading)
        {
            HeadingCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            HeadingTitle = title.Trim(),
        };
    }

    internal static CompetencyListNode Leaf(Guid listId, Guid? parentNodeId, Guid competencyId) =>
        new(listId, parentNodeId, ListNodeKind.Competency) { CompetencyId = competencyId };

    internal void MoveTo(Guid? parentNodeId) => ParentNodeId = parentNodeId;

    internal void Rename(string? code, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        HeadingCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        HeadingTitle = title.Trim();
    }
}
