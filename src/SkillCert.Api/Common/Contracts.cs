using SkillCert.Domain.Currency;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;

namespace SkillCert.Api.Common;

/// <summary>A competency's currency for the signed-in candidate, as every candidate screen shows it.</summary>
/// <param name="ExpiringSoon">Current, but expiring within 30 days (presentation only).</param>
public sealed record CurrencyDto(
    CurrencyStatus Status,
    CurrencyReason Reason,
    DateTimeOffset? AchievedAt,
    DateTimeOffset? ExpiresAt,
    bool ExpiringSoon,
    bool HasPendingReview)
{
    public static CurrencyDto From(CompetencyCurrency currency, CandidateRecord record) => new(
        currency.Status, currency.Reason, currency.AchievedAt, currency.ExpiresAt, record.IsExpiringSoon(currency), currency.HasPendingReview);
}

/// <summary>The lowest review level a skill accepts; every higher level may also sign (decision §2.5).</summary>
/// <param name="ClassificationCode">"Instructor" or "Supervisor" when <paramref name="Method"/> is Classified.</param>
/// <param name="Order">Sort key: Self 0, Peer 1, then by classification rank.</param>
public sealed record ReviewLevelDto(ReviewMethod Method, string? ClassificationCode, int Order)
{
    public static ReviewLevelDto From(LowestReviewLevel level) => new(level.Method, level.Classification?.Code, level.Order);
}
