namespace SkillCert.Domain.Lists;

/// <summary>
/// A named, ordered hierarchy of headings and library competencies, e.g. the AFA Skills Record (spec §4).
/// Owns its whole tree and enforces its rules: parents are headings of this list, no cycles, competency
/// leaves have no children, and a competency appears at most once. There is no list revision history:
/// changes are live, and archived PDFs keep the hierarchy as it was rendered.
/// </summary>
public sealed class CompetencyList
{
    public const string DuplicateCompetency = "list.duplicate-competency";
    public const string InvalidParent = "list.invalid-parent";
    public const string Cycle = "list.cycle";

    private readonly List<CompetencyListNode> _nodes = [];

    private CompetencyList()
    {
    }

    public CompetencyList(string title, string? description, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Id = Guid.CreateVersion7(createdAt);
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<CompetencyListNode> Nodes => _nodes;

    /// <summary>The distinct competencies currently in the list, in tree order.</summary>
    public IEnumerable<Guid> CompetencyIds => Walk().Where(n => n.CompetencyId is not null).Select(n => n.CompetencyId!.Value);

    public bool Contains(Guid competencyId) => _nodes.Any(n => n.CompetencyId == competencyId);

    /// <summary>Children of a heading (or the top level when null), in display order.</summary>
    public IReadOnlyList<CompetencyListNode> ChildrenOf(Guid? parentNodeId) =>
        _nodes.Where(n => n.ParentNodeId == parentNodeId).OrderBy(n => n.SortOrder).ToList();

    /// <summary>Depth-first, in display order: how the list is shown and printed.</summary>
    public IEnumerable<CompetencyListNode> Walk(Guid? parentNodeId = null)
    {
        foreach (var node in ChildrenOf(parentNodeId))
        {
            yield return node;
            foreach (var descendant in Walk(node.Id))
            {
                yield return descendant;
            }
        }
    }

    public CompetencyListNode AddHeading(Guid? parentNodeId, string? code, string title, int? index = null)
    {
        EnsureValidParent(parentNodeId);
        var node = CompetencyListNode.Heading(Id, parentNodeId, code, title);
        Insert(node, index);
        return node;
    }

    public CompetencyListNode AddCompetency(Guid? parentNodeId, Guid competencyId, int? index = null)
    {
        EnsureValidParent(parentNodeId);
        if (Contains(competencyId))
        {
            throw new DomainRuleException(DuplicateCompetency, "That competency is already in this list.");
        }

        var node = CompetencyListNode.Leaf(Id, parentNodeId, competencyId);
        Insert(node, index);
        return node;
    }

    public void RenameHeading(Guid nodeId, string? code, string title)
    {
        var node = Find(nodeId);
        if (node.Kind != ListNodeKind.Heading)
        {
            throw new DomainRuleException(InvalidParent, "Only headings have a title to rename.");
        }

        node.Rename(code, title);
    }

    /// <summary>Moves a node (with its subtree) under another heading or to the top level, at <paramref name="index"/>.</summary>
    public void Move(Guid nodeId, Guid? newParentNodeId, int index)
    {
        var node = Find(nodeId);
        EnsureValidParent(newParentNodeId);
        if (newParentNodeId is { } target && (target == nodeId || IsDescendant(target, of: nodeId)))
        {
            throw new DomainRuleException(Cycle, "A heading can't be moved inside itself.");
        }

        var oldParent = node.ParentNodeId;
        _nodes.Remove(node);
        Renumber(oldParent);
        node.MoveTo(newParentNodeId);
        Insert(node, index);
    }

    /// <summary>Removes a node and, for a heading, everything under it. Review history is untouched.</summary>
    public void Remove(Guid nodeId)
    {
        var node = Find(nodeId);
        foreach (var descendant in Walk(nodeId).ToList())
        {
            _nodes.Remove(descendant);
        }

        _nodes.Remove(node);
        Renumber(node.ParentNodeId);
    }

    public void Rename(string title, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private CompetencyListNode Find(Guid nodeId) =>
        _nodes.SingleOrDefault(n => n.Id == nodeId)
        ?? throw new KeyNotFoundException($"Node {nodeId} is not in list {Id}.");

    private void EnsureValidParent(Guid? parentNodeId)
    {
        if (parentNodeId is null)
        {
            return;
        }

        var parent = _nodes.SingleOrDefault(n => n.Id == parentNodeId)
            ?? throw new DomainRuleException(InvalidParent, "The parent must be a heading in this list.");
        if (parent.Kind != ListNodeKind.Heading)
        {
            throw new DomainRuleException(InvalidParent, "Competencies can't contain other items; choose a heading.");
        }
    }

    private bool IsDescendant(Guid nodeId, Guid of)
    {
        var current = _nodes.SingleOrDefault(n => n.Id == nodeId)?.ParentNodeId;
        while (current is { } parentId)
        {
            if (parentId == of)
            {
                return true;
            }

            current = _nodes.Single(n => n.Id == parentId).ParentNodeId;
        }

        return false;
    }

    private void Insert(CompetencyListNode node, int? index)
    {
        var siblings = ChildrenOf(node.ParentNodeId).ToList();
        var position = Math.Clamp(index ?? siblings.Count, 0, siblings.Count);
        siblings.Insert(position, node);
        _nodes.Add(node);
        for (var i = 0; i < siblings.Count; i++)
        {
            siblings[i].SortOrder = i;
        }
    }

    private void Renumber(Guid? parentNodeId)
    {
        var siblings = ChildrenOf(parentNodeId);
        for (var i = 0; i < siblings.Count; i++)
        {
            siblings[i].SortOrder = i;
        }
    }
}
