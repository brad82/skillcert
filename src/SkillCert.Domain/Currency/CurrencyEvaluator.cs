using SkillCert.Domain.Reviews;

namespace SkillCert.Domain.Currency;

/// <summary>
/// Derives currency for one user and one competency, independent of lists (spec §12). Pure: give it every
/// review of that user for that competency and every revision of the competency.
///
/// <list type="number">
/// <item>Evidence is accepted reviews (NotRequired or Confirmed) given at or before <c>asOf</c>.</item>
/// <item>The latest evidence wins: ReviewedAt, then CreatedAt, then Id. It never falls back to an older,
/// more favourable review.</item>
/// <item>Evidence against a revision older than a breaking revision published at or before <c>asOf</c>
/// is invalidated → NotCertified / RevisionInvalidated. A late confirmation doesn't bypass this.</item>
/// <item>NotCompetent evidence → NotCompetent. Only a later Competent review restores currency.</item>
/// <item>Competent evidence expires after its own revision's RecertificationDays (elapsed 24-hour days):
/// Current strictly before ExpiresAt, Expired from ExpiresAt on. No interval → never expires.</item>
/// </list>
///
/// Historical queries ask what currently accepted evidence says about <c>asOf</c>, not what was known then.
/// </summary>
public static class CurrencyEvaluator
{
    public static CompetencyCurrency Evaluate(
        IEnumerable<ReviewEvidence> reviews,
        IReadOnlyCollection<RevisionPolicy> revisions,
        DateTimeOffset asOf)
    {
        var inScope = reviews.Where(r => r.ReviewedAt <= asOf).ToList();
        var hasPending = inScope.Any(r => r.ConfirmationStatus == ConfirmationStatus.Pending);

        var latest = inScope
            .Where(r => r.ConfirmationStatus is ConfirmationStatus.NotRequired or ConfirmationStatus.Confirmed)
            .OrderByDescending(r => r.ReviewedAt)
            .ThenByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .FirstOrDefault();

        if (latest is null)
        {
            return new(CurrencyStatus.NotCertified, CurrencyReason.NeverReviewed, null, null, null, hasPending);
        }

        var assessedRevision = revisions.SingleOrDefault(r => r.Id == latest.RevisionId)
            ?? throw new ArgumentException($"Revision {latest.RevisionId} of review {latest.Id} was not supplied.", nameof(revisions));

        var invalidated = revisions.Any(r =>
            r.InvalidatesPreviousReviews
            && r.RevisionNumber > assessedRevision.RevisionNumber
            && r.PublishedAt <= asOf);
        if (invalidated)
        {
            return new(CurrencyStatus.NotCertified, CurrencyReason.RevisionInvalidated, latest.Id, null, null, hasPending);
        }

        if (latest.Outcome == ReviewOutcome.NotCompetent)
        {
            return new(CurrencyStatus.NotCompetent, CurrencyReason.NotCompetent, latest.Id, null, null, hasPending);
        }

        DateTimeOffset? expiresAt = assessedRevision.RecertificationDays is { } days
            ? latest.ReviewedAt.AddDays(days)
            : null;

        return asOf < expiresAt || expiresAt is null
            ? new(CurrencyStatus.Current, CurrencyReason.Current, latest.Id, latest.ReviewedAt, expiresAt, hasPending)
            : new(CurrencyStatus.Expired, CurrencyReason.RecertificationExpired, latest.Id, latest.ReviewedAt, expiresAt, hasPending);
    }
}
