using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminLists;

public sealed record AdminListsResponse(IReadOnlyList<AdminListSummaryDto> Lists);

/// <summary>GET /api/admin/lists: every competency list with its size and the groups it is assigned to.</summary>
public static class ListListsEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) => group.MapGet("", HandleAsync).WithName("ListLists");

    internal static async Task<Ok<AdminListsResponse>> HandleAsync(SkillCertDbContext db, CancellationToken cancellationToken)
    {
        var lists = await db.CompetencyLists
            .OrderBy(l => l.Title)
            .Select(l => new AdminListSummaryDto(
                l.Id,
                l.Title,
                l.Description,
                l.IsActive,
                l.Nodes.Count(n => n.CompetencyId != null),
                db.UserGroups.Where(g => g.AssignedLists.Any(a => a.CompetencyListId == l.Id)).OrderBy(g => g.Name).Select(g => g.Name).ToList()))
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(new AdminListsResponse(lists));
    }
}
