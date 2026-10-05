using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.CurrentUser;

/// <param name="Capabilities">Additive capabilities (spec §18): "Administrator" plus held reviewer classification codes.</param>
public sealed record CurrentUserResponse(Guid Id, string DisplayName, string Email, IReadOnlyList<string> Capabilities);

/// <summary>
/// GET /api/me — the signed-in user and their capabilities. 403 when the login account has no active
/// domain user (deactivated, or never provisioned).
/// </summary>
public static class GetCurrentUserEndpoint
{
    public const string AdministratorCapability = "Administrator";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/me", HandleAsync)
            .WithName("GetCurrentUser")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

    internal static async Task<Results<Ok<CurrentUserResponse>, ForbidHttpResult>> HandleAsync(
        ClaimsPrincipal principal, SkillCertDbContext db, CancellationToken cancellationToken)
    {
        var subjectId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        var me = await db.DomainUsers
            .Where(u => u.ExternalSubjectId == subjectId && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                u.Email,
                u.IsAdministrator,
                Classifications = db.ReviewerClassifications
                    .Where(rc => u.Classifications.Any(c => c.ReviewerClassificationId == rc.Id))
                    .OrderBy(rc => rc.Code)
                    .Select(rc => rc.Code)
                    .ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (me is null)
        {
            return TypedResults.Forbid();
        }

        IReadOnlyList<string> capabilities = me.IsAdministrator
            ? [AdministratorCapability, .. me.Classifications]
            : me.Classifications;

        return TypedResults.Ok(new CurrentUserResponse(me.Id, me.DisplayName, me.Email, capabilities));
    }
}
