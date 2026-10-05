using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminCompetencies;

/// <param name="InvalidatesPreviousReviews">
/// The administrator's explicit choice (spec §3): true makes reviews against earlier revisions stop counting from now;
/// false keeps everyone's currency.
/// </param>
public sealed record PublishRevisionRequest(RevisionContentRequest Content, bool InvalidatesPreviousReviews);

public sealed class PublishRevisionRequestValidator : AbstractValidator<PublishRevisionRequest>
{
    public PublishRevisionRequestValidator()
    {
        RuleFor(r => r.Content).NotNull().SetValidator(new RevisionContentRequestValidator());
    }
}

/// <summary>POST /api/admin/competencies/{competencyId}/revisions: "Publish new revision" (spec §3). Audited.</summary>
public static class PublishRevisionEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/{competencyId:guid}/revisions", HandleAsync)
            .WithName("PublishRevision")
            .WithValidation<PublishRevisionRequest>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    internal static async Task<Results<Ok<AdminCompetencyDto>, NotFound, ProblemHttpResult>> HandleAsync(
        Guid competencyId,
        PublishRevisionRequest request,
        CurrentUserAccessor currentUser,
        SkillCertDbContext db,
        AuditLog audit,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        if (await AdminCompetencyMapping.TrackedAsync(db, competencyId, cancellationToken) is not { } competency)
        {
            return TypedResults.NotFound();
        }

        var classifications = await db.ReviewerClassifications.ToListAsync(cancellationToken);
        if (AdminCompetencyMapping.ToContent(request.Content, classifications) is not { } content)
        {
            return RuleProblems.Create(AdminCompetencyMapping.UnknownClassificationType, "That reviewer classification doesn't exist.");
        }

        var before = AdminCompetencyMapping.Snapshot(competency.CurrentRevision, classifications);
        var revision = competency.PublishRevision(content, request.InvalidatesPreviousReviews, time.GetUtcNowForStorage(), adminId);
        audit.Record(adminId, "competency.publish-revision", "Competency", competency.Id, before, AdminCompetencyMapping.Snapshot(revision, classifications));
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await AdminCompetencyMapping.LoadAsync(db, competencyId, cancellationToken))!);
    }
}
