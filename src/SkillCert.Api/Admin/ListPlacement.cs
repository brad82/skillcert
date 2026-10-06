using Microsoft.EntityFrameworkCore;
using SkillCert.Domain.Lists;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Admin;

/// <summary>Where new competencies go in a list: under a heading (or the top level), at a position (or the end).</summary>
public sealed record ListPlacementRequest(Guid ListId, Guid? ParentNodeId, int? Index);

/// <summary>
/// Places newly created competencies in a list in the same transaction as their creation, so a failure never
/// leaves them outside the list (development plan §2.10). Shared by "new competency" and CSV import.
/// </summary>
public static class ListPlacement
{
    public const string ListNotFoundType = "list.not-found";

    /// <returns>The tracked list, or the refusal (problem type and message) when the placement is unusable.</returns>
    internal static async Task<(CompetencyList? List, string? ProblemType, string? Problem)> LoadAsync(
        SkillCertDbContext db, ListPlacementRequest placement, CancellationToken cancellationToken)
    {
        var list = await db.CompetencyLists.Include(l => l.Nodes).SingleOrDefaultAsync(l => l.Id == placement.ListId, cancellationToken);
        if (list is null)
        {
            return (null, ListNotFoundType, "That list doesn't exist.");
        }

        if (placement.ParentNodeId is { } parentId
            && list.Nodes.SingleOrDefault(n => n.Id == parentId) is not { Kind: ListNodeKind.Heading })
        {
            return (null, CompetencyList.InvalidParent, "Choose a heading in that list.");
        }

        return (list, null, null);
    }

    internal static void Add(CompetencyList list, ListPlacementRequest placement, IReadOnlyList<Guid> competencyIds)
    {
        for (var i = 0; i < competencyIds.Count; i++)
        {
            list.AddCompetency(placement.ParentNodeId, competencyIds[i], placement.Index is { } index ? index + i : null);
        }
    }
}
