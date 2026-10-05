using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;
using SkillCert.Infrastructure.Storage;

namespace SkillCert.Api.Features.MyRecord;

/// <summary>
/// GET /api/me/records/{recordId}: one of the candidate's archived PDFs, byte for byte as stored. 404 for anyone
/// else's record, or when the stored file can't be found.
/// </summary>
public static class GetMyArchivedRecordEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/records/{recordId:guid}", HandleAsync)
            .WithName("GetMyArchivedRecord")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

    internal static async Task<Results<FileContentHttpResult, NotFound, ForbidHttpResult>> HandleAsync(
        Guid recordId, CurrentUserAccessor currentUser, SkillCertDbContext db, IBlobStore blobs, TimeProvider time, CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var archive = await db.TrainingRecords
            .Where(r => r.Id == recordId && r.UserId == userId)
            .Join(db.CompetencyLists, r => r.CompetencyListId, l => l.Id, (r, l) => new { r.BlobPath, r.CompletionDate, l.Title })
            .SingleOrDefaultAsync(cancellationToken);
        if (archive is null || await blobs.GetAsync(archive.BlobPath, cancellationToken) is not { } pdf)
        {
            return TypedResults.NotFound();
        }

        var completed = TimeZoneInfo.ConvertTime(archive.CompletionDate, time.LocalTimeZone);
        return TypedResults.File(pdf, "application/pdf", TrainingRecordBuilder.FileName(archive.Title, $"completed {completed:yyyy-MM-dd}"));
    }
}
