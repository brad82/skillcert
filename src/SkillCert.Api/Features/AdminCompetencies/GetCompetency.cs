using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminCompetencies;

/// <summary>GET /api/admin/competencies/{competencyId}: current revision, revision history and the lists using it.</summary>
public static class GetCompetencyEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/{competencyId:guid}", HandleAsync).WithName("GetCompetency").Produces(StatusCodes.Status404NotFound);

    internal static async Task<Results<Ok<AdminCompetencyDto>, NotFound>> HandleAsync(Guid competencyId, SkillCertDbContext db, CancellationToken cancellationToken) =>
        await AdminCompetencyMapping.LoadAsync(db, competencyId, cancellationToken) is { } dto ? TypedResults.Ok(dto) : TypedResults.NotFound();
}
