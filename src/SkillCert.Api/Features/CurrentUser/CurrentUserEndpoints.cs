using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.CurrentUser;

public static class CurrentUserEndpoints
{
    public const string AdministratorCapability = "Administrator";

    /// <param name="Capabilities">Additive capabilities (spec §18): "Administrator" plus held reviewer classification codes.</param>
    public sealed record MeResponse(Guid Id, string DisplayName, string Email, IReadOnlyList<string> Capabilities);

    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", GetMeAsync).WithName("GetCurrentUser").WithTags("CurrentUser").RequireAuthorization();
        return app;
    }

    private static async Task<Results<Ok<MeResponse>, ForbidHttpResult>> GetMeAsync(
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
            // Signed in, but no active domain user: deactivated, or never provisioned.
            return TypedResults.Forbid();
        }

        IReadOnlyList<string> capabilities = me.IsAdministrator
            ? [AdministratorCapability, .. me.Classifications]
            : me.Classifications;

        return TypedResults.Ok(new MeResponse(me.Id, me.DisplayName, me.Email, capabilities));
    }
}
