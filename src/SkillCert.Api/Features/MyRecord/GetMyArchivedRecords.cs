using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.MyRecord;

public sealed record MyArchivedRecordsResponse(IReadOnlyList<MyArchivedRecordDto> Records);

/// <param name="ListTitle">The list's title now; the PDF keeps the title it was rendered with.</param>
/// <param name="CompletionDate">Evidence-based completion that triggered the archive.</param>
public sealed record MyArchivedRecordDto(Guid Id, Guid ListId, string ListTitle, DateTimeOffset CompletionDate, DateTimeOffset GeneratedAt);

/// <summary>GET /api/me/records: the candidate's archived training records, newest first (spec §22).</summary>
public static class GetMyArchivedRecordsEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/records", HandleAsync)
            .WithName("GetMyArchivedRecords")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

    internal static async Task<Results<Ok<MyArchivedRecordsResponse>, ForbidHttpResult>> HandleAsync(
        CurrentUserAccessor currentUser, SkillCertDbContext db, CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var records = await db.TrainingRecords
            .Where(r => r.UserId == userId)
            .Join(db.CompetencyLists, r => r.CompetencyListId, l => l.Id, (r, l) => new { r, l.Title })
            .OrderByDescending(x => x.r.GeneratedAt)
            .Select(x => new MyArchivedRecordDto(x.r.Id, x.r.CompetencyListId, x.Title, x.r.CompletionDate, x.r.GeneratedAt))
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(new MyArchivedRecordsResponse(records));
    }
}
