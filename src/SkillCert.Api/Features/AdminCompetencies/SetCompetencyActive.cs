using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminCompetencies;

public sealed record SetCompetencyActiveRequest(bool IsActive);

public sealed class SetCompetencyActiveRequestValidator : AbstractValidator<SetCompetencyActiveRequest>;

/// <summary>
/// PUT /api/admin/competencies/{competencyId}/active: deactivate or reactivate. A deactivated competency takes no
/// new sign-offs; its history and list placements stay. Audited.
/// </summary>
public static class SetCompetencyActiveEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{competencyId:guid}/active", HandleAsync)
            .WithName("SetCompetencyActive")
            .WithValidation<SetCompetencyActiveRequest>()
            .Produces(StatusCodes.Status404NotFound);

    internal static async Task<Results<Ok<AdminCompetencyDto>, NotFound>> HandleAsync(
        Guid competencyId, SetCompetencyActiveRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        if (await AdminCompetencyMapping.TrackedAsync(db, competencyId, cancellationToken) is not { } competency)
        {
            return TypedResults.NotFound();
        }

        if (competency.IsActive != request.IsActive)
        {
            if (request.IsActive)
            {
                competency.Reactivate();
            }
            else
            {
                competency.Deactivate();
            }

            audit.Record(adminId, request.IsActive ? "competency.reactivate" : "competency.deactivate", "Competency", competency.Id,
                new { IsActive = !request.IsActive }, new { request.IsActive });
            await db.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok((await AdminCompetencyMapping.LoadAsync(db, competencyId, cancellationToken))!);
    }
}
