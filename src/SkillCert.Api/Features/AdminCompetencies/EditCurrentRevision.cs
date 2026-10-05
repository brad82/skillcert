using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Domain.Reviewers;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminCompetencies;

/// <summary>
/// PUT /api/admin/competencies/{competencyId}/revision: "Edit current revision" (spec §3), an editorial fix to the
/// title, short title, description or resources. It never invalidates reviews. Recertification and who may sign
/// are policy: changing them needs a new revision (409 <c>competency.policy-change</c>). Audited.
/// </summary>
public static class EditCurrentRevisionEndpoint
{
    public const string PolicyChangeType = "competency.policy-change";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{competencyId:guid}/revision", HandleAsync)
            .WithName("EditCurrentRevision")
            .WithValidation<RevisionContentRequest>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    internal static async Task<Results<Ok<AdminCompetencyDto>, NotFound, ProblemHttpResult>> HandleAsync(
        Guid competencyId, RevisionContentRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        if (await AdminCompetencyMapping.TrackedAsync(db, competencyId, cancellationToken) is not { } competency)
        {
            return TypedResults.NotFound();
        }

        var classifications = await db.ReviewerClassifications.ToListAsync(cancellationToken);
        if (AdminCompetencyMapping.ToContent(request, classifications) is not { } content)
        {
            return RuleProblems.Create(AdminCompetencyMapping.UnknownClassificationType, "That reviewer classification doesn't exist.");
        }

        var revision = competency.CurrentRevision;
        var lowestNow = ReviewHierarchy.Lowest(revision, classifications);
        var policyChanged = revision.RecertificationDays != content.RecertificationDays
            || lowestNow.Method != request.LowestReviewer.Method
            || (lowestNow.Classification?.Code != request.LowestReviewer.ClassificationCode && request.LowestReviewer.Method == Domain.Reviews.ReviewMethod.Classified);
        if (policyChanged)
        {
            return RuleProblems.Create(PolicyChangeType, "Recertification and who can sign are policy: publish a new revision to change them.", StatusCodes.Status409Conflict);
        }

        var before = AdminCompetencyMapping.Snapshot(revision, classifications);
        revision.CorrectEditorially(content);
        audit.Record(adminId, "competency.edit-revision", "Competency", competency.Id, before, AdminCompetencyMapping.Snapshot(revision, classifications));
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await AdminCompetencyMapping.LoadAsync(db, competencyId, cancellationToken))!);
    }
}
