using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.Approvals;

/// <summary>
/// GET /api/approvals/{signatureId}/signature: the signature on a sitting that names the signed-in user as
/// reviewer, so they can check it before deciding. 404 otherwise.
/// </summary>
public static class GetApprovalSignatureEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/{signatureId:guid}/signature", HandleAsync)
            .WithName("GetApprovalSignature")
            .Produces(StatusCodes.Status200OK, contentType: "image/svg+xml")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

    internal static async Task<Results<FileContentHttpResult, NotFound, ForbidHttpResult>> HandleAsync(
        Guid signatureId, CurrentUserAccessor currentUser, SkillCertDbContext db, HttpContext http, CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var signature = await db.ReviewSignatures
            .Where(s => s.Id == signatureId && db.CompetencyReviews.Any(r => r.ReviewSignatureId == s.Id && r.ReviewerUserId == userId))
            .Select(s => new { s.Data, s.ContentType })
            .SingleOrDefaultAsync(cancellationToken);
        if (signature is null)
        {
            return TypedResults.NotFound();
        }

        return SignatureFile.Serve(http, signature.Data, signature.ContentType);
    }
}
