using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminCompetencies;

/// <summary>
/// What an administrator authors for a revision. Reviewers are given as the <em>lowest</em> permitted level; every
/// higher level is implied (development plan §2.5) and stored closed upward.
/// </summary>
/// <param name="RecertificationDays">Elapsed days until expiry; null for no expiry. 365 reads as "every year".</param>
public sealed record RevisionContentRequest(
    string Title,
    string? ShortTitle,
    string? Description,
    int? RecertificationDays,
    ReviewLevelRequest LowestReviewer,
    IReadOnlyList<ResourceRequest> Resources);

/// <param name="ClassificationCode">Required when <paramref name="Method"/> is Classified.</param>
public sealed record ReviewLevelRequest(ReviewMethod Method, string? ClassificationCode);

public sealed record ResourceRequest(string Title, string Url, ResourceType Type);

public sealed class RevisionContentRequestValidator : AbstractValidator<RevisionContentRequest>
{
    public RevisionContentRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(300);
        RuleFor(r => r.ShortTitle).MaximumLength(300);
        RuleFor(r => r.Description).MaximumLength(4000);
        RuleFor(r => r.RecertificationDays).InclusiveBetween(1, 36_500).When(r => r.RecertificationDays is not null);
        RuleFor(r => r.LowestReviewer).NotNull();
        RuleFor(r => r.LowestReviewer.Method).IsInEnum().When(r => r.LowestReviewer is not null);
        RuleFor(r => r.LowestReviewer.ClassificationCode).NotEmpty()
            .When(r => r.LowestReviewer is { Method: ReviewMethod.Classified })
            .WithMessage("Choose which classification can sign.");
        RuleFor(r => r.Resources).NotNull().Must(r => r.Count <= 20).WithMessage("At most 20 resources.");
        RuleForEach(r => r.Resources).ChildRules(resource =>
        {
            resource.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            resource.RuleFor(x => x.Url).NotEmpty().MaximumLength(2000)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                .WithMessage("Use a full http(s) link.");
            resource.RuleFor(x => x.Type).IsInEnum();
        });
    }
}

public sealed record AdminCompetencyListItemDto(
    Guid Id,
    string Code,
    string Title,
    string ShortTitle,
    bool IsActive,
    int RevisionNumber,
    int? RecertificationDays,
    ReviewLevelDto LowestReviewer,
    int ListCount);

public sealed record AdminResourceDto(string Title, string Url, ResourceType Type);

public sealed record AdminRevisionDto(
    int Number,
    string Title,
    string? ShortTitle,
    string? Description,
    int? RecertificationDays,
    ReviewLevelDto LowestReviewer,
    IReadOnlyList<AdminResourceDto> Resources,
    DateTimeOffset PublishedAt,
    bool InvalidatesPreviousReviews);

public sealed record AdminRevisionSummaryDto(int Number, string Title, DateTimeOffset PublishedAt, bool InvalidatesPreviousReviews, int? RecertificationDays);

public sealed record AdminListRefDto(Guid Id, string Title);

/// <param name="Revisions">Newest first. Older revisions are kept for audit and never assessed against.</param>
public sealed record AdminCompetencyDto(
    Guid Id,
    string Code,
    bool IsActive,
    AdminRevisionDto Current,
    IReadOnlyList<AdminRevisionSummaryDto> Revisions,
    IReadOnlyList<AdminListRefDto> Lists);

internal static class AdminCompetencyMapping
{
    public const string UnknownClassificationType = "competency.unknown-classification";

    /// <summary>Turns the request into closed-upward domain content; null when the classification code is unknown.</summary>
    public static RevisionContent? ToContent(RevisionContentRequest request, IReadOnlyCollection<ReviewerClassification> classifications)
    {
        var lowest = request.LowestReviewer;
        Guid[] permitted = [];
        if (lowest.Method == ReviewMethod.Classified)
        {
            if (classifications.SingleOrDefault(c => c.Code == lowest.ClassificationCode) is not { } classification)
            {
                return null;
            }

            permitted = [classification.Id];
        }

        var content = new RevisionContent(
            request.Title,
            request.ShortTitle,
            request.Description,
            request.RecertificationDays,
            AllowsSelfReview: lowest.Method == ReviewMethod.Self,
            AllowsPeerReview: lowest.Method == ReviewMethod.Peer,
            permitted,
            request.Resources.Select(r => new RevisionResource(r.Title, new Uri(r.Url), r.Type)).ToList());
        return ReviewHierarchy.CloseUpward(content, classifications);
    }

    public static AdminRevisionDto Revision(CompetencyRevision revision, IReadOnlyCollection<ReviewerClassification> classifications) => new(
        revision.RevisionNumber,
        revision.Title,
        revision.ShortTitle,
        revision.Description,
        revision.RecertificationDays,
        ReviewLevelDto.From(ReviewHierarchy.Lowest(revision, classifications)),
        revision.Resources.Select(r => new AdminResourceDto(r.Title, r.Url.ToString(), r.Type)).ToList(),
        revision.PublishedAt,
        revision.InvalidatesPreviousReviews);

    /// <summary>The values an audit entry compares.</summary>
    public static object Snapshot(CompetencyRevision revision, IReadOnlyCollection<ReviewerClassification> classifications) => new
    {
        revision.RevisionNumber,
        revision.Title,
        revision.ShortTitle,
        revision.Description,
        revision.RecertificationDays,
        LowestReviewer = ReviewHierarchy.Lowest(revision, classifications) is var level
            ? level.Classification?.Code ?? level.Method.ToString()
            : null,
        Resources = revision.Resources.Select(r => new { r.Title, Url = r.Url.ToString(), Type = r.Type.ToString() }),
        revision.InvalidatesPreviousReviews,
    };

    public static async Task<AdminCompetencyDto?> LoadAsync(SkillCertDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var competency = await db.Competencies
            .Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (competency is null)
        {
            return null;
        }

        var classifications = await db.ReviewerClassifications.AsNoTracking().ToListAsync(cancellationToken);
        var lists = await db.CompetencyLists
            .Where(l => l.Nodes.Any(n => n.CompetencyId == id))
            .OrderBy(l => l.Title)
            .Select(l => new AdminListRefDto(l.Id, l.Title))
            .ToListAsync(cancellationToken);
        return new AdminCompetencyDto(
            competency.Id,
            competency.Code,
            competency.IsActive,
            Revision(competency.CurrentRevision, classifications),
            competency.Revisions.OrderByDescending(r => r.RevisionNumber)
                .Select(r => new AdminRevisionSummaryDto(r.RevisionNumber, r.Title, r.PublishedAt, r.InvalidatesPreviousReviews, r.RecertificationDays))
                .ToList(),
            lists);
    }

    public static Task<Domain.Competencies.Competency?> TrackedAsync(SkillCertDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Competencies.Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications)
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken)!;
}
