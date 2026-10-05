using Microsoft.EntityFrameworkCore;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Requirements;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Common;

/// <summary>
/// Everything about one candidate's required record at one moment: their required lists (group-inherited),
/// every competency in them with revisions, their reviews of those competencies and the currency of each.
/// Loaded in a handful of queries; shared by the lists, home and detail endpoints.
/// </summary>
public sealed class CandidateRecord
{
    /// <summary>"Expiring soon" is presentation only (spec §13): Current with expiry inside this window.</summary>
    public static readonly TimeSpan ExpiringSoonWindow = TimeSpan.FromDays(30);

    private CandidateRecord(
        DateTimeOffset asOf,
        IReadOnlyList<CompetencyList> lists,
        IReadOnlyDictionary<Guid, Competency> competencies,
        IReadOnlyList<CompetencyReview> reviews,
        IReadOnlyList<ReviewerClassification> classifications)
    {
        AsOf = asOf;
        Lists = lists;
        Competencies = competencies;
        Reviews = reviews;
        Classifications = classifications;
        Currency = competencies.Keys.ToDictionary(id => id, id => CurrencyEvaluator.Evaluate(
            reviews.Where(r => r.CompetencyId == id).Select(ReviewEvidence.From),
            Policies(competencies[id]),
            asOf));
    }

    public DateTimeOffset AsOf { get; }

    public IReadOnlyList<CompetencyList> Lists { get; }

    public IReadOnlyDictionary<Guid, Competency> Competencies { get; }

    public IReadOnlyList<CompetencyReview> Reviews { get; }

    public IReadOnlyList<ReviewerClassification> Classifications { get; }

    public IReadOnlyDictionary<Guid, CompetencyCurrency> Currency { get; }

    public bool IsExpiringSoon(CompetencyCurrency currency) =>
        currency.Status == CurrencyStatus.Current && currency.ExpiresAt is { } expiresAt && expiresAt - AsOf <= ExpiringSoonWindow;

    /// <summary>Spec §21: a non-empty list is compliant only when every competency in it is Current.</summary>
    public bool IsCompliant(CompetencyList list)
    {
        var ids = list.CompetencyIds.ToList();
        return ids.Count > 0 && ids.All(id => Currency[id].IsCurrent);
    }

    public static IReadOnlyCollection<RevisionPolicy> Policies(Competency competency) =>
        competency.Revisions
            .Select(r => new RevisionPolicy(r.Id, r.RevisionNumber, r.RecertificationDays, r.InvalidatesPreviousReviews, r.PublishedAt))
            .ToList();

    public static async Task<CandidateRecord> LoadAsync(
        SkillCertDbContext db, Guid userId, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var groups = await db.UserGroups
            .Include(g => g.Members.Where(m => m.UserId == userId))
            .Include(g => g.AssignedLists)
            .Where(g => g.IsActive && g.Members.Any(m => m.UserId == userId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var listIds = groups.SelectMany(g => g.AssignedLists).Select(a => a.CompetencyListId).Distinct().ToList();
        var candidateLists = await db.CompetencyLists
            .Include(l => l.Nodes)
            .Where(l => listIds.Contains(l.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var lists = RequiredLists.For(userId, groups, candidateLists);

        var competencyIds = RequiredLists.Competencies(lists).ToList();
        var competencies = await db.Competencies
            .Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications)
            .Where(c => competencyIds.Contains(c.Id))
            .AsSplitQuery()
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var reviews = await db.CompetencyReviews
            .Where(r => r.CandidateUserId == userId && competencyIds.Contains(r.CompetencyId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var classifications = await db.ReviewerClassifications.AsNoTracking().ToListAsync(cancellationToken);

        return new CandidateRecord(asOf, lists, competencies, reviews, classifications);
    }
}
