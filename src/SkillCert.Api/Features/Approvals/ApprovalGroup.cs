using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.Approvals;

/// <summary>
/// One sitting awaiting a decision: the reviews sharing a signature (and so a candidate and ReviewedAt). It is
/// confirmed or rejected as a whole, in one transaction, and only by the reviewer it names (spec §11).
/// </summary>
internal static class ApprovalGroup
{
    public static async Task<Results<NoContent, NotFound, ForbidHttpResult, ProblemHttpResult>> DecideAsync(
        Guid signatureId,
        Action<CompetencyReview, Guid, DateTimeOffset> decide,
        CurrentUserAccessor currentUser,
        SkillCertDbContext db,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var reviews = await db.CompetencyReviews.Where(r => r.ReviewSignatureId == signatureId).ToListAsync(cancellationToken);
        if (reviews.Count == 0)
        {
            return TypedResults.NotFound();
        }

        // Administrators included: nobody decides on the named reviewer's behalf.
        if (reviews.Any(r => r.ReviewerUserId != userId))
        {
            return TypedResults.Forbid();
        }

        var now = time.GetUtcNow();
        try
        {
            foreach (var review in reviews)
            {
                decide(review, userId, now);
            }
        }
        catch (DomainRuleException exception)
        {
            return RuleProblems.From(exception, StatusCodes.Status409Conflict);
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    public static RouteHandlerBuilder Documented(this RouteHandlerBuilder builder) =>
        builder
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization();
}
