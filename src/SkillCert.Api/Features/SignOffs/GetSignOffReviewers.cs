using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.SignOffs;

public sealed record SignOffReviewersResponse(IReadOnlyList<SignOffReviewerDto> Reviewers);

/// <param name="Method">How they would sign: the lowest level they hold that every skill permits.</param>
/// <param name="SignatureRequired">True for classified reviewers (Instructor, Supervisor).</param>
/// <param name="NeedsConfirmation">True when the claim waits for the reviewer to confirm it (Supervisor).</param>
public sealed record SignOffReviewerDto(
    Guid UserId,
    string DisplayName,
    ReviewMethod Method,
    string? ClassificationCode,
    bool SignatureRequired,
    bool NeedsConfirmation);

/// <summary>
/// GET /api/signoffs/reviewers?competencyId=…: every registered, active user who could sign all the given skills
/// for the signed-in candidate, and how (spec §10: no typed names). The candidate appears only for Self skills.
/// </summary>
public static class GetSignOffReviewersEndpoint
{
    public const int MaxCompetencies = 100;

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/reviewers", HandleAsync)
            .WithName("GetSignOffReviewers")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

    internal static async Task<Results<Ok<SignOffReviewersResponse>, ValidationProblem, ForbidHttpResult>> HandleAsync(
        [FromQuery(Name = "competencyId")] Guid[] competencyIds,
        CurrentUserAccessor currentUser,
        SkillCertDbContext db,
        CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } candidateId)
        {
            return TypedResults.Forbid();
        }

        if (competencyIds.Length is 0 or > MaxCompetencies)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["competencyId"] = [$"Give between 1 and {MaxCompetencies} competency ids."],
            });
        }

        var competencies = await SignOffLoader.CompetenciesAsync(db, competencyIds, cancellationToken);
        if (competencies is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["competencyId"] = ["Unknown competency."] });
        }

        var revisions = competencies.Select(c => c.CurrentRevision).ToList();
        var classifications = await db.ReviewerClassifications.AsNoTracking().ToListAsync(cancellationToken);
        var users = await db.DomainUsers.Include(u => u.Classifications).Where(u => u.IsActive).AsNoTracking().ToListAsync(cancellationToken);

        var reviewers = users
            .Select(user => ReviewHierarchy.ReviewerFor(user, candidateId, revisions, classifications))
            .OfType<Reviewer>()
            .OrderBy(r => r.Method == ReviewMethod.Self ? 0 : 1)
            .ThenBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new SignOffReviewerDto(
                r.UserId,
                r.DisplayName,
                r.Method,
                r.Classification?.Code,
                r.Method == ReviewMethod.Classified,
                r.Classification?.AffirmationPolicy == AffirmationPolicy.ReviewerConfirmation))
            .ToList();

        return TypedResults.Ok(new SignOffReviewersResponse(reviewers));
    }
}
