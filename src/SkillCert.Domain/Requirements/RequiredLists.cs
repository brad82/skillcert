using SkillCert.Domain.Groups;
using SkillCert.Domain.Lists;

namespace SkillCert.Domain.Requirements;

/// <summary>
/// What a user is required to hold (spec §6): the distinct union of lists assigned to the active groups they
/// belong to, and the distinct competencies across those lists. A competency introduced by several lists or
/// group paths is still one requirement. Pure: callers load the groups and lists.
/// </summary>
public static class RequiredLists
{
    /// <summary>Active lists inherited through the user's active groups, distinct, ordered by title.</summary>
    public static IReadOnlyList<CompetencyList> For(
        Guid userId, IEnumerable<UserGroup> groups, IEnumerable<CompetencyList> lists)
    {
        var listIds = groups
            .Where(g => g.IsActive && g.HasMember(userId))
            .SelectMany(g => g.AssignedLists)
            .Select(a => a.CompetencyListId)
            .ToHashSet();

        return lists
            .Where(l => l.IsActive && listIds.Contains(l.Id))
            .DistinctBy(l => l.Id)
            .OrderBy(l => l.Title, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>Every distinct competency in the given lists' current trees.</summary>
    public static IReadOnlySet<Guid> Competencies(IEnumerable<CompetencyList> lists) =>
        lists.SelectMany(l => l.CompetencyIds).ToHashSet();
}
