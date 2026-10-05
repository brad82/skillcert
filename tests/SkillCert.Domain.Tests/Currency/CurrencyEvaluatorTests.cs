using SkillCert.Domain.Currency;
using SkillCert.Domain.Reviews;
using static SkillCert.Domain.Reviews.ConfirmationStatus;
using static SkillCert.Domain.Reviews.ReviewOutcome;

namespace SkillCert.Domain.Tests.Currency;

/// <summary>One test per rule in spec §11–12. Dates are 2026 unless stated; times UTC.</summary>
public sealed class CurrencyEvaluatorTests
{
    private static DateTimeOffset On(int month, int day, int hour = 12) => new(2026, month, day, hour, 0, 0, TimeSpan.Zero);

    /// <summary>A competency's revisions and one user's reviews of it.</summary>
    private sealed class Scenario
    {
        private readonly List<RevisionPolicy> _revisions = [];
        private readonly List<ReviewEvidence> _reviews = [];

        public Scenario Revision(int? recertDays = 365, bool breaking = false, DateTimeOffset? published = null)
        {
            _revisions.Add(new(Guid.NewGuid(), _revisions.Count + 1, recertDays, breaking, published ?? On(1, 1)));
            return this;
        }

        public ReviewEvidence Review(
            DateTimeOffset reviewedAt, ReviewOutcome outcome = Competent, ConfirmationStatus status = NotRequired,
            int revision = 1, DateTimeOffset? createdAt = null, Guid? id = null)
        {
            var evidence = new ReviewEvidence(
                id ?? Guid.NewGuid(), _revisions[revision - 1].Id, outcome, status, reviewedAt, createdAt ?? reviewedAt);
            _reviews.Add(evidence);
            return evidence;
        }

        public CompetencyCurrency At(DateTimeOffset asOf) => CurrencyEvaluator.Evaluate(_reviews, _revisions, asOf);
    }

    [Fact]
    public void Never_reviewed_is_not_certified()
    {
        var result = new Scenario().Revision().At(On(10, 4));

        Assert.Equal((CurrencyStatus.NotCertified, CurrencyReason.NeverReviewed), (result.Status, result.Reason));
        Assert.Null(result.EffectiveReviewId);
    }

    [Fact]
    public void Competent_review_is_current_until_its_expiry()
    {
        var s = new Scenario().Revision(recertDays: 365);
        var review = s.Review(On(10, 4));

        var result = s.At(On(10, 5));

        Assert.Equal(CurrencyStatus.Current, result.Status);
        Assert.Equal(review.Id, result.EffectiveReviewId);
        Assert.Equal(On(10, 4), result.AchievedAt);
        Assert.Equal(On(10, 4).AddDays(365), result.ExpiresAt);
    }

    [Fact]
    public void Expiry_is_elapsed_days_not_calendar_years()
    {
        // 2028 is a leap year: 365 days from 2027-03-01 is 2028-02-29, not 2028-03-01.
        var s = new Scenario().Revision(recertDays: 365);
        s.Review(new DateTimeOffset(2027, 3, 1, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2028, 2, 29, 12, 0, 0, TimeSpan.Zero), s.At(On(10, 4).AddYears(1)).ExpiresAt);
    }

    [Fact]
    public void Current_strictly_before_ExpiresAt_and_expired_at_it()
    {
        var s = new Scenario().Revision(recertDays: 30);
        s.Review(On(9, 1));
        var expiresAt = On(9, 1).AddDays(30);

        Assert.Equal(CurrencyStatus.Current, s.At(expiresAt.AddTicks(-1)).Status);
        var atExpiry = s.At(expiresAt);
        Assert.Equal((CurrencyStatus.Expired, CurrencyReason.RecertificationExpired), (atExpiry.Status, atExpiry.Reason));
    }

    [Fact]
    public void No_recertification_interval_never_expires()
    {
        var s = new Scenario().Revision(recertDays: null);
        s.Review(On(1, 2));

        var result = s.At(On(1, 2).AddYears(20));

        Assert.Equal(CurrencyStatus.Current, result.Status);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public void Reviews_after_the_evaluation_time_are_ignored()
    {
        var s = new Scenario().Revision();
        s.Review(On(10, 4));

        Assert.Equal(CurrencyReason.NeverReviewed, s.At(On(10, 3)).Reason);
    }

    [Fact]
    public void Latest_review_wins_and_never_falls_back_to_an_older_favourable_one()
    {
        var s = new Scenario().Revision();
        s.Review(On(9, 1), Competent);
        var notCompetent = s.Review(On(10, 1), NotCompetent);

        var result = s.At(On(10, 4));

        Assert.Equal((CurrencyStatus.NotCompetent, CurrencyReason.NotCompetent), (result.Status, result.Reason));
        Assert.Equal(notCompetent.Id, result.EffectiveReviewId);
    }

    [Fact]
    public void Only_a_later_competent_review_restores_currency()
    {
        var s = new Scenario().Revision();
        s.Review(On(9, 1), NotCompetent);
        var competent = s.Review(On(9, 15), Competent);

        var result = s.At(On(10, 4));

        Assert.Equal(CurrencyStatus.Current, result.Status);
        Assert.Equal(competent.Id, result.EffectiveReviewId);
    }

    [Fact]
    public void Ties_on_ReviewedAt_break_by_CreatedAt_then_Id()
    {
        var s = new Scenario().Revision();
        s.Review(On(10, 4), Competent, createdAt: On(10, 4, 13));
        var laterCreated = s.Review(On(10, 4), NotCompetent, createdAt: On(10, 4, 14));
        Assert.Equal(laterCreated.Id, s.At(On(10, 5)).EffectiveReviewId);

        var t = new Scenario().Revision();
        var lowId = new Guid("00000000-0000-0000-0000-000000000001");
        var highId = new Guid("ffffffff-0000-0000-0000-000000000001");
        t.Review(On(10, 4), NotCompetent, id: lowId);
        t.Review(On(10, 4), Competent, id: highId);
        Assert.Equal(highId, t.At(On(10, 5)).EffectiveReviewId);
    }

    [Fact]
    public void Pending_and_rejected_reviews_do_not_displace_accepted_evidence()
    {
        var s = new Scenario().Revision();
        var accepted = s.Review(On(9, 1), Competent);
        s.Review(On(10, 1), NotCompetent, Pending);
        s.Review(On(10, 2), NotCompetent, Rejected);

        var result = s.At(On(10, 4));

        Assert.Equal(CurrencyStatus.Current, result.Status);
        Assert.Equal(accepted.Id, result.EffectiveReviewId);
        Assert.True(result.HasPendingReview);
    }

    [Fact]
    public void A_pending_claim_alone_leaves_the_user_not_certified_with_the_pending_flag()
    {
        var s = new Scenario().Revision();
        s.Review(On(10, 4), Competent, Pending);

        var result = s.At(On(10, 5));

        Assert.Equal(CurrencyReason.NeverReviewed, result.Reason);
        Assert.True(result.HasPendingReview);
    }

    [Fact]
    public void Confirmation_is_retrospective_achieved_on_the_review_date()
    {
        // Achieved Oct 4, confirmed Oct 18: the evidence stores ReviewedAt, so achievement and expiry run from Oct 4.
        var s = new Scenario().Revision(recertDays: 365);
        s.Review(On(10, 4), Competent, Confirmed);

        var asOfOct10 = s.At(On(10, 10));

        Assert.Equal(CurrencyStatus.Current, asOfOct10.Status);
        Assert.Equal(On(10, 4), asOfOct10.AchievedAt);
        Assert.Equal(On(10, 4).AddDays(365), asOfOct10.ExpiresAt);
    }

    [Fact]
    public void A_breaking_revision_invalidates_older_revision_evidence_from_its_publication()
    {
        var s = new Scenario().Revision().Revision(breaking: true, published: On(10, 1));
        s.Review(On(9, 1), revision: 1);

        Assert.Equal(CurrencyStatus.Current, s.At(On(9, 30)).Status);
        var after = s.At(On(10, 1));
        Assert.Equal((CurrencyStatus.NotCertified, CurrencyReason.RevisionInvalidated), (after.Status, after.Reason));
    }

    [Fact]
    public void A_non_breaking_revision_preserves_currency_and_the_original_interval()
    {
        var s = new Scenario().Revision(recertDays: 365).Revision(recertDays: 90, published: On(10, 1));
        s.Review(On(9, 1), revision: 1);

        var result = s.At(On(12, 31));

        Assert.Equal(CurrencyStatus.Current, result.Status);
        Assert.Equal(On(9, 1).AddDays(365), result.ExpiresAt);
    }

    [Fact]
    public void A_late_confirmation_does_not_bypass_a_newer_breaking_revision()
    {
        var s = new Scenario().Revision().Revision(breaking: true, published: On(10, 10));
        s.Review(On(10, 4), Competent, Confirmed, revision: 1); // confirmed on, say, Oct 18

        Assert.Equal(CurrencyReason.RevisionInvalidated, s.At(On(10, 18)).Reason);
    }

    [Fact]
    public void Reassessment_against_the_breaking_revision_restores_currency()
    {
        var s = new Scenario().Revision().Revision(breaking: true, published: On(10, 1));
        s.Review(On(9, 1), revision: 1);
        s.Review(On(10, 2), revision: 2);

        Assert.Equal(CurrencyStatus.Current, s.At(On(10, 4)).Status);
    }

    [Fact]
    public void Historical_query_answers_for_the_requested_time()
    {
        var s = new Scenario().Revision(recertDays: 30);
        s.Review(On(6, 1));
        s.Review(On(9, 1), NotCompetent);

        Assert.Equal(CurrencyStatus.Current, s.At(On(6, 15)).Status);
        Assert.Equal(CurrencyStatus.Expired, s.At(On(8, 1)).Status);
        Assert.Equal(CurrencyStatus.NotCompetent, s.At(On(9, 2)).Status);
    }

    [Fact]
    public void Works_from_real_reviews_via_ReviewEvidence_From()
    {
        var competency = SkillCert.Domain.Competencies.Competency.Create(
            "4.3.1", TestData.Content.Revision(self: true), On(1, 1), null);
        var candidate = TestData.People.Candidate();
        var review = CompetencyReview.Record(
            candidate.Id, competency, Competent, On(10, 4), Reviewer.Self(candidate), null, null, null, On(10, 4), candidate.Id);
        var revisions = competency.Revisions
            .Select(r => new RevisionPolicy(r.Id, r.RevisionNumber, r.RecertificationDays, r.InvalidatesPreviousReviews, r.PublishedAt))
            .ToList();

        var result = CurrencyEvaluator.Evaluate([ReviewEvidence.From(review)], revisions, On(10, 5));

        Assert.Equal(CurrencyStatus.Current, result.Status);
    }
}
