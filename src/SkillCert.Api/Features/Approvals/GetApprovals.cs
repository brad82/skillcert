using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.Approvals;

public sealed record ApprovalsResponse(IReadOnlyList<ApprovalGroupDto> Groups);

/// <param name="SignatureId">Identifies the sitting: decide it with /api/approvals/{signatureId}/confirm or /reject.</param>
public sealed record ApprovalGroupDto(
    Guid SignatureId,
    Guid CandidateUserId,
    string CandidateName,
    DateTimeOffset ReviewedAt,
    string? Comment,
    IReadOnlyList<ApprovalItemDto> Items);

public sealed record ApprovalItemDto(Guid ReviewId, Guid CompetencyId, string Code, string ShortTitle, ReviewOutcome Outcome, int RevisionNumber);

/// <summary>
/// GET /api/approvals: pending claims that name the signed-in user as reviewer, oldest sitting first. Holding the
/// classification now doesn't matter: a reviewer keeps the right to decide claims made while they held it.
/// </summary>
public static class GetApprovalsEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("", HandleAsync)
            .WithName("GetApprovals")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

    internal static async Task<Results<Ok<ApprovalsResponse>, ForbidHttpResult>> HandleAsync(
        CurrentUserAccessor currentUser, SkillCertDbContext db, CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var pending = await db.CompetencyReviews
            .Where(r => r.ReviewerUserId == userId && r.ConfirmationStatus == ConfirmationStatus.Pending && r.ReviewSignatureId != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var candidateIds = pending.Select(r => r.CandidateUserId).Distinct().ToList();
        var names = await db.DomainUsers.Where(u => candidateIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
        var competencyIds = pending.Select(r => r.CompetencyId).Distinct().ToList();
        var competencies = await db.Competencies.Include(c => c.Revisions)
            .Where(c => competencyIds.Contains(c.Id))
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var groups = pending
            .GroupBy(r => r.ReviewSignatureId!.Value)
            .Select(g =>
            {
                var first = g.First();
                return new ApprovalGroupDto(
                    g.Key,
                    first.CandidateUserId,
                    names[first.CandidateUserId],
                    first.ReviewedAt,
                    first.Comment,
                    g.Select(r =>
                    {
                        var competency = competencies[r.CompetencyId];
                        var current = competency.CurrentRevision;
                        return new ApprovalItemDto(
                            r.Id, r.CompetencyId, competency.Code, current.ShortTitle ?? current.Title, r.Outcome,
                            competency.Revisions.Single(v => v.Id == r.CompetencyRevisionId).RevisionNumber);
                    }).ToList());
            })
            .OrderBy(g => g.ReviewedAt)
            .ToList();

        return TypedResults.Ok(new ApprovalsResponse(groups));
    }
}
