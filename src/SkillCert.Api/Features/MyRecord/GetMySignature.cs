using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.MyRecord;

/// <summary>
/// GET /api/me/signatures/{signatureId}: the SVG of a signature on one of the candidate's own reviews. 404 for any
/// other signature, so ids can't be probed. The SVG is server-rendered (spec §10) and served with a CSP that blocks
/// scripts, so it is safe even if opened directly rather than through an &lt;img&gt;.
/// </summary>
public static class GetMySignatureEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/signatures/{signatureId:guid}", HandleAsync)
            .WithName("GetMySignature")
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
            .Where(s => s.Id == signatureId
                && db.CompetencyReviews.Any(r => r.ReviewSignatureId == s.Id && r.CandidateUserId == userId))
            .Select(s => new { s.Data, s.ContentType })
            .SingleOrDefaultAsync(cancellationToken);
        if (signature is null)
        {
            return TypedResults.NotFound();
        }

        http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'";
        http.Response.Headers.XContentTypeOptions = "nosniff";
        http.Response.Headers.CacheControl = "private, max-age=86400, immutable";
        return TypedResults.File(Encoding.UTF8.GetBytes(signature.Data), signature.ContentType);
    }
}
