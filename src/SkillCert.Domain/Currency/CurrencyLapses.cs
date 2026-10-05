namespace SkillCert.Domain.Currency;

/// <summary>A moment a Current competency stopped being current, for history timelines.</summary>
/// <param name="Reason">RecertificationExpired or RevisionInvalidated.</param>
/// <param name="RevisionNumber">The breaking revision, for RevisionInvalidated.</param>
public sealed record CurrencyLapse(DateTimeOffset At, CurrencyReason Reason, int? RevisionNumber);

/// <summary>
/// When currency lapsed without a new review: expiry of the effective Competent review, or a breaking revision
/// invalidating it. Derived from <see cref="CurrencyEvaluator"/> by evaluating just before and at each candidate
/// moment, so the timeline can never disagree with the status rules.
/// </summary>
public static class CurrencyLapses
{
    public static IReadOnlyList<CurrencyLapse> Find(
        IReadOnlyCollection<ReviewEvidence> reviews,
        IReadOnlyCollection<RevisionPolicy> revisions,
        DateTimeOffset asOf)
    {
        var expiries = reviews
            .Select(r => revisions.SingleOrDefault(p => p.Id == r.RevisionId)?.RecertificationDays is { } days
                ? r.ReviewedAt.AddDays(days)
                : (DateTimeOffset?)null)
            .OfType<DateTimeOffset>();
        var breakingRevisions = revisions.Where(r => r.InvalidatesPreviousReviews).Select(r => r.PublishedAt);

        var lapses = new List<CurrencyLapse>();
        foreach (var at in expiries.Concat(breakingRevisions).Where(t => t <= asOf).Distinct().Order())
        {
            var before = CurrencyEvaluator.Evaluate(reviews, revisions, at.AddTicks(-1));
            var after = CurrencyEvaluator.Evaluate(reviews, revisions, at);
            if (!before.IsCurrent || after.IsCurrent || after.EffectiveReviewId != before.EffectiveReviewId)
            {
                continue; // not a lapse, or a new review landed at the same instant
            }

            var revisionNumber = after.Reason == CurrencyReason.RevisionInvalidated
                ? revisions.Where(r => r.InvalidatesPreviousReviews && r.PublishedAt == at).Max(r => r.RevisionNumber)
                : (int?)null;
            lapses.Add(new(at, after.Reason, revisionNumber));
        }

        return lapses;
    }
}
