using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminLists;

/// <summary>GET /api/admin/lists/{listId}: the list and its whole tree, for the tree editor.</summary>
public static class GetListEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/{listId:guid}", HandleAsync).WithName("GetList").Produces(StatusCodes.Status404NotFound);

    internal static async Task<Results<Ok<AdminListDto>, NotFound>> HandleAsync(Guid listId, SkillCertDbContext db, CancellationToken cancellationToken) =>
        await AdminListQuery.LoadAsync(db, listId, cancellationToken) is { } list ? TypedResults.Ok(list) : TypedResults.NotFound();
}
