namespace SkillCert.Domain.Groups;

/// <summary>
/// An administrator-configured assignment group, e.g. New Patroller (spec §6). Groups carry competency-list
/// requirements to their members; they are not reviewer authority (see ReviewerClassification) and are not
/// mutually exclusive. There are no direct user/list assignments: an exception is its own group.
/// </summary>
public sealed class UserGroup
{
    private readonly List<UserGroupMembership> _members = [];
    private readonly List<GroupListAssignment> _assignedLists = [];

    private UserGroup()
    {
    }

    public UserGroup(string name, string? description, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Guid.CreateVersion7(createdAt);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    /// <summary>An inactive group keeps its members and lists but imposes no requirements.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<UserGroupMembership> Members => _members;

    public IReadOnlyCollection<GroupListAssignment> AssignedLists => _assignedLists;

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public void AddMember(Guid userId, DateTimeOffset at)
    {
        if (!HasMember(userId))
        {
            _members.Add(new UserGroupMembership(Id, userId, at));
        }
    }

    public void RemoveMember(Guid userId) => _members.RemoveAll(m => m.UserId == userId);

    public void AssignList(Guid competencyListId, DateTimeOffset at)
    {
        if (_assignedLists.All(a => a.CompetencyListId != competencyListId))
        {
            _assignedLists.Add(new GroupListAssignment(Id, competencyListId, at));
        }
    }

    public void UnassignList(Guid competencyListId) => _assignedLists.RemoveAll(a => a.CompetencyListId == competencyListId);

    public void Rename(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}

public sealed class UserGroupMembership
{
    private UserGroupMembership()
    {
    }

    internal UserGroupMembership(Guid userGroupId, Guid userId, DateTimeOffset addedAt)
    {
        UserGroupId = userGroupId;
        UserId = userId;
        AddedAt = addedAt;
    }

    public Guid UserGroupId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }
}

public sealed class GroupListAssignment
{
    private GroupListAssignment()
    {
    }

    internal GroupListAssignment(Guid userGroupId, Guid competencyListId, DateTimeOffset assignedAt)
    {
        UserGroupId = userGroupId;
        CompetencyListId = competencyListId;
        AssignedAt = assignedAt;
    }

    public Guid UserGroupId { get; private set; }

    public Guid CompetencyListId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }
}
