using SkillCert.Domain.Reviews;

namespace SkillCert.Domain.Currency;

public enum CurrencyStatus
{
    NotCertified,
    Current,
    Expired,
    NotCompetent,
}

public enum CurrencyReason
{
    NeverReviewed,
    Current,
    RecertificationExpired,
    RevisionInvalidated,
    NotCompetent,
}

/// <summary>The facts about one review that currency needs. Projectable straight from the database.</summary>
public sealed record ReviewEvidence(
    Guid Id,
    Guid RevisionId,
    ReviewOutcome Outcome,
    ConfirmationStatus ConfirmationStatus,
    DateTimeOffset ReviewedAt,
    DateTimeOffset CreatedAt)
{
    public static ReviewEvidence From(CompetencyReview review) => new(
        review.Id, review.CompetencyRevisionId, review.Outcome, review.ConfirmationStatus, review.ReviewedAt, review.CreatedAt);
}

/// <summary>The facts about one revision that currency needs.</summary>
public sealed record RevisionPolicy(
    Guid Id,
    int RevisionNumber,
    int? RecertificationDays,
    bool InvalidatesPreviousReviews,
    DateTimeOffset PublishedAt);

/// <summary>
/// A user's standing on one competency at one moment (spec §12).
/// <see cref="HasPendingReview"/> is separate from the status: a pending claim never changes it.
/// </summary>
/// <param name="EffectiveReviewId">The review the status rests on, when there is one.</param>
/// <param name="AchievedAt">When the effective Competent review was given (Current or Expired only).</param>
/// <param name="ExpiresAt">When a Competent review stops being current; null when it never expires.</param>
public sealed record CompetencyCurrency(
    CurrencyStatus Status,
    CurrencyReason Reason,
    Guid? EffectiveReviewId,
    DateTimeOffset? AchievedAt,
    DateTimeOffset? ExpiresAt,
    bool HasPendingReview)
{
    public bool IsCurrent => Status == CurrencyStatus.Current;
}
