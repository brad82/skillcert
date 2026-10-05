using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Api.Common;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Reviewers;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.MyRecord;

public sealed record MyListsResponse(DateTimeOffset AsOf, IReadOnlyList<MyListDto> Lists);

/// <param name="Nodes">Every node in display order (depth-first). Rebuild the tree from ParentNodeId.</param>
public sealed record MyListDto(Guid Id, string Title, bool IsCompliant, MyListCountsDto Counts, IReadOnlyList<MyListNodeDto> Nodes);

/// <summary>Counts over the list's distinct competencies. ExpiringSoon is a subset of Current; Pending overlaps others.</summary>
public sealed record MyListCountsDto(int Total, int Current, int ExpiringSoon, int Expired, int NotCompetent, int NotCertified, int Pending);

/// <param name="Competency">Set for competency leaves only.</param>
public sealed record MyListNodeDto(
    Guid Id,
    Guid? ParentNodeId,
    int Depth,
    ListNodeKind Kind,
    string? HeadingCode,
    string? HeadingTitle,
    MyCompetencySummaryDto? Competency);

/// <param name="ShortTitle">The paper record's wording; falls back to Title.</param>
public sealed record MyCompetencySummaryDto(
    Guid CompetencyId,
    string Code,
    string Title,
    string ShortTitle,
    CurrencyDto Currency,
    ReviewLevelDto LowestReviewer);

/// <summary>
/// GET /api/me/lists — the signed-in candidate's required lists (group-inherited, spec §6) with the hierarchy and
/// each competency's currency. Feeds Home (counts, compliance) and the Skills tree.
/// </summary>
public static class GetMyListsEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/lists", HandleAsync)
            .WithName("GetMyLists")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

    internal static async Task<Results<Ok<MyListsResponse>, ForbidHttpResult>> HandleAsync(
        CurrentUserAccessor currentUser, SkillCertDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var record = await CandidateRecord.LoadAsync(db, userId, time.GetUtcNow(), cancellationToken);
        return TypedResults.Ok(new MyListsResponse(record.AsOf, record.Lists.Select(l => ToDto(l, record)).ToList()));
    }

    private static MyListDto ToDto(CompetencyList list, CandidateRecord record)
    {
        var depths = new Dictionary<Guid, int>();
        var nodes = list.Walk().Select(node =>
        {
            var depth = node.ParentNodeId is { } parent ? depths[parent] + 1 : 0;
            depths[node.Id] = depth;
            return new MyListNodeDto(
                node.Id, node.ParentNodeId, depth, node.Kind, node.HeadingCode, node.HeadingTitle,
                node.CompetencyId is { } id ? Summary(id, record) : null);
        }).ToList();

        var currencies = list.CompetencyIds.Select(id => record.Currency[id]).ToList();
        var counts = new MyListCountsDto(
            Total: currencies.Count,
            Current: currencies.Count(c => c.Status == CurrencyStatus.Current),
            ExpiringSoon: currencies.Count(record.IsExpiringSoon),
            Expired: currencies.Count(c => c.Status == CurrencyStatus.Expired),
            NotCompetent: currencies.Count(c => c.Status == CurrencyStatus.NotCompetent),
            NotCertified: currencies.Count(c => c.Status == CurrencyStatus.NotCertified),
            Pending: currencies.Count(c => c.HasPendingReview));

        return new MyListDto(list.Id, list.Title, record.IsCompliant(list), counts, nodes);
    }

    internal static MyCompetencySummaryDto Summary(Guid competencyId, CandidateRecord record)
    {
        var competency = record.Competencies[competencyId];
        var revision = competency.CurrentRevision;
        return new MyCompetencySummaryDto(
            competency.Id,
            competency.Code,
            revision.Title,
            revision.ShortTitle ?? revision.Title,
            CurrencyDto.From(record.Currency[competencyId], record),
            ReviewLevelDto.From(ReviewHierarchy.Lowest(revision, record.Classifications)));
    }
}
