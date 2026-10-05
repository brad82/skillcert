using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminUsers;

/// <summary>GET /api/admin/users/{userId}.</summary>
public static class GetUserEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/{userId:guid}", HandleAsync).WithName("GetUser").Produces(StatusCodes.Status404NotFound);

    internal static async Task<Results<Ok<AdminUserDto>, NotFound>> HandleAsync(Guid userId, SkillCertDbContext db, CancellationToken cancellationToken) =>
        await AdminUserQuery.SingleAsync(db, userId, cancellationToken) is { } user ? TypedResults.Ok(user) : TypedResults.NotFound();
}
