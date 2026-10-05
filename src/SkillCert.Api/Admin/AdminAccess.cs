using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Admin;

/// <summary>
/// Administrator-only endpoints (spec §18). Signed-out callers get 401 from the cookie handler; signed-in users
/// who aren't active administrators get a bare 403. Handlers can then take the admin's id from
/// <see cref="CurrentUserAccessor"/>.
/// </summary>
public sealed class AdministratorFilter(CurrentUserAccessor currentUser, SkillCertDbContext db) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var cancellationToken = context.HttpContext.RequestAborted;
        var isAdministrator = await currentUser.GetUserIdAsync(cancellationToken) is { } userId
            && await db.DomainUsers.AnyAsync(u => u.Id == userId && u.IsAdministrator, cancellationToken);
        return isAdministrator ? await next(context) : TypedResults.Forbid();
    }
}

public static class AdminAccessExtensions
{
    /// <summary>A route group under /api/admin that only administrators can call.</summary>
    public static RouteGroupBuilder MapAdminGroup(this IEndpointRouteBuilder app, string prefix, string tag) =>
        app.MapGroup($"/api/admin/{prefix}")
            .WithTags(tag)
            .RequireAuthorization()
            .AddEndpointFilter<AdministratorFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

    /// <summary>The signed-in administrator's id; only valid behind <see cref="AdministratorFilter"/>.</summary>
    public static async Task<Guid> AdminIdAsync(this CurrentUserAccessor currentUser, CancellationToken cancellationToken) =>
        await currentUser.GetUserIdAsync(cancellationToken)
        ?? throw new InvalidOperationException("No signed-in user behind the administrator filter.");
}
