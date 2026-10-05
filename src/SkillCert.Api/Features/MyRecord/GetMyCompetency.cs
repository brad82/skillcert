using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.MyRecord;

/// <summary>One required competency for the signed-in candidate: the detail screen's Overview, Resources and History.</summary>
/// <param name="EffectiveReview">The review the status rests on (Last signed / By), when there is one.</param>
/// <param name="PendingReview">The latest review awaiting its reviewer's confirmation, when there is one.</param>
/// <param name="PartOf">Where the competency sits in each required list that holds it.</param>
/// <param name="History">Reviews and lapses, newest first.</param>
public sealed record MyCompetencyResponse(
    DateTimeOffset AsOf,
    Guid CompetencyId,
    string Code,
    string Title,
    string ShortTitle,
    string? Description,
    CurrencyDto Currency,
    ReviewLevelDto LowestReviewer,
    MyRevisionDto Revision,
    MyReviewDto? EffectiveReview,
    MyReviewDto? PendingReview,
    IReadOnlyList<MyCompetencyPathDto> PartOf,
    IReadOnlyList<MyResourceDto> Resources,
    IReadOnlyList<MyHistoryEntryDto> History);

/// <param name="RecertificationDays">Null: never expires.</param>
public sealed record MyRevisionDto(int Number, DateTimeOffset PublishedAt, int? RecertificationDays);

/// <param name="Headings">Section headings from the top down, e.g. "4 Basic Life Support", "4.3 CPR".</param>
public sealed record MyCompetencyPathDto(Guid ListId, string ListTitle, IReadOnlyList<string> Headings);

public sealed record MyResourceDto(string Title, Uri Url, ResourceType Type);

/// <param name="ClassificationCode">"Instructor" or "Supervisor" when <paramref name="Method"/> is Classified.</param>
/// <param name="DecidedAt">When the reviewer confirmed or rejected it.</param>
/// <param name="SignatureId">Fetch the image from /api/me/signatures/{id}.</param>
/// <param name="SignedWith">Other skills signed with the same signature (the same sitting). Presentation only (spec §9).</param>
public sealed record MyReviewDto(
    Guid Id,
    ReviewOutcome Outcome,
    DateTimeOffset ReviewedAt,
    string ReviewerName,
    ReviewMethod Method,
    string? ClassificationCode,
    int RevisionNumber,
    ConfirmationStatus ConfirmationStatus,
    DateTimeOffset? DecidedAt,
    string? RejectionReason,
    string? Comment,
    Guid? SignatureId,
    IReadOnlyList<MySignedWithDto> SignedWith);

public sealed record MySignedWithDto(Guid CompetencyId, string Code, string ShortTitle);

public enum MyHistoryEntryKind
{
    Review,
    Expired,
    Invalidated,
}

/// <param name="Review">Set for Review entries.</param>
/// <param name="RevisionNumber">The breaking revision, for Invalidated entries.</param>
public sealed record MyHistoryEntryDto(MyHistoryEntryKind Kind, DateTimeOffset At, MyReviewDto? Review, int? RevisionNumber);

/// <summary>GET /api/me/competencies/{competencyId}: 404 unless the competency is in one of the candidate's required lists.</summary>
public static class GetMyCompetencyEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/competencies/{competencyId:guid}", HandleAsync)
            .WithName("GetMyCompetency")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

    internal static async Task<Results<Ok<MyCompetencyResponse>, NotFound, ForbidHttpResult>> HandleAsync(
        Guid competencyId, CurrentUserAccessor currentUser, SkillCertDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } userId)
        {
            return TypedResults.Forbid();
        }

        var record = await CandidateRecord.LoadAsync(db, userId, time.GetUtcNow(), cancellationToken);
        if (!record.Competencies.TryGetValue(competencyId, out var competency))
        {
            return TypedResults.NotFound();
        }

        var reviews = record.Reviews.Where(r => r.CompetencyId == competencyId && r.ReviewedAt <= record.AsOf).ToList();
        var signedWith = await SignedWithAsync(db, userId, competencyId, reviews, cancellationToken);
        MyReviewDto ToDto(CompetencyReview review) => Review(review, competency, record, signedWith);

        var currency = record.Currency[competencyId];
        var revision = competency.CurrentRevision;
        var lapses = CurrencyLapses.Find(reviews.Select(ReviewEvidence.From).ToList(), CandidateRecord.Policies(competency), record.AsOf);
        var history = reviews.Select(r => new MyHistoryEntryDto(MyHistoryEntryKind.Review, r.ReviewedAt, ToDto(r), null))
            .Concat(lapses.Select(l => new MyHistoryEntryDto(
                l.Reason == CurrencyReason.RevisionInvalidated ? MyHistoryEntryKind.Invalidated : MyHistoryEntryKind.Expired,
                l.At, null, l.RevisionNumber)))
            .OrderByDescending(e => e.At)
            .ThenByDescending(e => e.Review?.Id)
            .ToList();

        return TypedResults.Ok(new MyCompetencyResponse(
            record.AsOf,
            competency.Id,
            competency.Code,
            revision.Title,
            revision.ShortTitle ?? revision.Title,
            revision.Description,
            CurrencyDto.From(currency, record),
            GetMyListsEndpoint.Summary(competencyId, record).LowestReviewer,
            new MyRevisionDto(revision.RevisionNumber, revision.PublishedAt, revision.RecertificationDays),
            reviews.SingleOrDefault(r => r.Id == currency.EffectiveReviewId) is { } effective ? ToDto(effective) : null,
            reviews.Where(r => r.ConfirmationStatus == ConfirmationStatus.Pending).MaxBy(r => r.ReviewedAt) is { } pending ? ToDto(pending) : null,
            record.Lists.Where(l => l.Contains(competencyId)).Select(l => Path(l, competencyId)).ToList(),
            revision.Resources.Select(r => new MyResourceDto(r.Title, r.Url, r.Type)).ToList(),
            history));
    }

    private static MyReviewDto Review(
        CompetencyReview review, Competency competency, CandidateRecord record, ILookup<Guid, MySignedWithDto> signedWith) => new(
        review.Id,
        review.Outcome,
        review.ReviewedAt,
        review.ReviewerName,
        review.Method,
        record.Classifications.SingleOrDefault(c => c.Id == review.ReviewerClassificationId)?.Code,
        competency.Revisions.Single(r => r.Id == review.CompetencyRevisionId).RevisionNumber,
        review.ConfirmationStatus,
        review.ConfirmedAt ?? review.RejectedAt,
        review.RejectionReason,
        review.Comment,
        review.ReviewSignatureId,
        review.ReviewSignatureId is { } signatureId ? signedWith[signatureId].ToList() : []);

    /// <summary>The candidate's other reviews sharing a signature with these, keyed by signature.</summary>
    private static async Task<ILookup<Guid, MySignedWithDto>> SignedWithAsync(
        SkillCertDbContext db, Guid userId, Guid competencyId, IReadOnlyList<CompetencyReview> reviews, CancellationToken cancellationToken)
    {
        var signatureIds = reviews.Select(r => r.ReviewSignatureId).OfType<Guid>().Distinct().ToList();
        var others = await db.CompetencyReviews
            .Where(r => r.CandidateUserId == userId && r.CompetencyId != competencyId
                && r.ReviewSignatureId != null && signatureIds.Contains(r.ReviewSignatureId.Value))
            .Select(r => new { SignatureId = r.ReviewSignatureId!.Value, r.CompetencyId })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var otherIds = others.Select(o => o.CompetencyId).Distinct().ToList();
        var competencies = await db.Competencies
            .Include(c => c.Revisions)
            .Where(c => otherIds.Contains(c.Id))
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        return others
            .Select(o => (o.SignatureId, Competency: competencies[o.CompetencyId]))
            .OrderBy(o => o.Competency.Code, StringComparer.Ordinal)
            .ToLookup(o => o.SignatureId, o => new MySignedWithDto(
                o.Competency.Id, o.Competency.Code, o.Competency.CurrentRevision.ShortTitle ?? o.Competency.CurrentRevision.Title));
    }

    private static MyCompetencyPathDto Path(CompetencyList list, Guid competencyId)
    {
        var nodes = list.Nodes.ToDictionary(n => n.Id);
        var headings = new List<string>();
        for (var parent = list.Nodes.Single(n => n.CompetencyId == competencyId).ParentNodeId; parent is { } id; parent = nodes[id].ParentNodeId)
        {
            var heading = nodes[id];
            headings.Insert(0, heading.HeadingCode is { } code ? $"{code} {heading.HeadingTitle}" : heading.HeadingTitle!);
        }

        return new MyCompetencyPathDto(list.Id, list.Title, headings);
    }
}
