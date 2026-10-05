using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.Approvals;

/// <param name="Reason">Shown to the candidate on the review.</param>
public sealed record RejectApprovalRequest(string Reason);

public sealed class RejectApprovalRequestValidator : AbstractValidator<RejectApprovalRequest>
{
    public RejectApprovalRequestValidator()
    {
        RuleFor(r => r.Reason).NotEmpty().MaximumLength(500);
    }
}

/// <summary>POST /api/approvals/{signatureId}/reject: the named reviewer refuses every claim from that sitting, with a reason.</summary>
public static class RejectApprovalEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/{signatureId:guid}/reject", HandleAsync)
            .WithName("RejectApproval")
            .WithValidation<RejectApprovalRequest>()
            .Documented();

    internal static Task<Results<NoContent, NotFound, ForbidHttpResult, ProblemHttpResult>> HandleAsync(
        Guid signatureId,
        RejectApprovalRequest request,
        CurrentUserAccessor currentUser,
        SkillCertDbContext db,
        TimeProvider time,
        CancellationToken cancellationToken) =>
        ApprovalGroup.DecideAsync(signatureId, (review, by, at) => review.Reject(by, at, request.Reason), currentUser, db, time, cancellationToken);
}
