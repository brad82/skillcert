using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;
using SkillCert.Infrastructure.Records;

namespace SkillCert.Api.Features.MyRecord;

/// <summary>
/// GET /api/me/lists/{listId}/record: the candidate's training record for one required list as a PDF, rendered now
/// (spec §21–22). It is not archived and doesn't affect automatic archival. 404 unless the list is required of them.
/// </summary>
public static class GetMyCurrentRecordEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/lists/{listId:guid}/record", HandleAsync)
            .WithName("GetMyCurrentRecord")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

    internal static async Task<Results<FileContentHttpResult, NotFound, ForbidHttpResult>> HandleAsync(
        Guid listId, CurrentUserAccessor currentUser, SkillCertDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var record = await CandidateRecord.LoadAsync(db, userId, time.GetUtcNow(), cancellationToken);
        if (record.Lists.SingleOrDefault(l => l.Id == listId) is not { } list)
        {
            return TypedResults.NotFound();
        }

        var name = await db.DomainUsers.Where(u => u.Id == userId).Select(u => u.DisplayName).SingleAsync(cancellationToken);
        var pdf = TrainingRecordPdf.Render(TrainingRecordBuilder.Build(record, list, name, record.CompletionDate(list), time));
        return TypedResults.File(pdf, "application/pdf", TrainingRecordBuilder.FileName(list.Title, name));
    }
}
