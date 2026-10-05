using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.Approvals;

/// <summary>
/// POST /api/approvals/{signatureId}/confirm: the named reviewer accepts every claim from that sitting. Achievement
/// stays at ReviewedAt. 409 when the sitting was already decided; Rejected never comes back.
/// </summary>
public static class ConfirmApprovalEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/{signatureId:guid}/confirm", HandleAsync)
            .WithName("ConfirmApproval")
            .Documented();

    internal static Task<Results<NoContent, NotFound, ForbidHttpResult, ProblemHttpResult>> HandleAsync(
        Guid signatureId, CurrentUserAccessor currentUser, SkillCertDbContext db, TimeProvider time, CancellationToken cancellationToken) =>
        ApprovalGroup.DecideAsync(signatureId, (review, by, at) => review.Confirm(by, at), currentUser, db, time, cancellationToken);
}
