using SkillCert.Domain.Currency;
using SkillCert.Domain.Reviews;
using static SkillCert.Domain.Reviews.ConfirmationStatus;
using static SkillCert.Domain.Reviews.ReviewOutcome;

namespace SkillCert.Domain.Tests.Currency;

public sealed class CurrencyLapsesTests
{
    private static DateTimeOffset On(int month, int day) => new(2026, month, day, 12, 0, 0, TimeSpan.Zero);

    private readonly List<RevisionPolicy> _revisions = [];
    private readonly List<ReviewEvidence> _reviews = [];

    private void Revision(int? recertDays, bool breaking = false, DateTimeOffset? published = null) =>
        _revisions.Add(new(Guid.NewGuid(), _revisions.Count + 1, recertDays, breaking, published ?? On(1, 1)));

    private void Review(DateTimeOffset at, ReviewOutcome outcome = Competent, ConfirmationStatus status = NotRequired, int revision = 1) =>
        _reviews.Add(new(Guid.NewGuid(), _revisions[revision - 1].Id, outcome, status, at, at));

    private IReadOnlyList<CurrencyLapse> At(DateTimeOffset asOf) => CurrencyLapses.Find(_reviews, _revisions, asOf);

    [Fact]
    public void An_unrenewed_review_lapses_at_its_expiry()
    {
        Revision(recertDays: 30);
        Review(On(2, 1));

        var lapse = Assert.Single(At(On(10, 4)));

        Assert.Equal((On(2, 1).AddDays(30), CurrencyReason.RecertificationExpired), (lapse.At, lapse.Reason));
    }

    [Fact]
    public void Expiry_in_the_future_or_renewed_in_time_is_not_a_lapse()
    {
        Revision(recertDays: 30);
        Review(On(2, 1));
        Review(On(2, 20)); // renewed before 3 March
        Review(On(9, 30)); // a later review that is still current

        var lapses = At(On(10, 4));

        Assert.Equal([On(2, 20).AddDays(30)], lapses.Select(l => l.At));
    }

    [Fact]
    public void A_breaking_revision_lapses_older_evidence_and_names_the_revision()
    {
        Revision(recertDays: 365);
        Revision(recertDays: 365, breaking: true, published: On(9, 1));
        Review(On(3, 1));

        var lapse = Assert.Single(At(On(10, 4)));

        Assert.Equal((On(9, 1), CurrencyReason.RevisionInvalidated, (int?)2), (lapse.At, lapse.Reason, lapse.RevisionNumber));
    }

    [Fact]
    public void Not_competent_or_pending_evidence_never_lapses()
    {
        Revision(recertDays: 30);
        Review(On(2, 1), outcome: NotCompetent);
        Review(On(4, 1), status: Pending);

        Assert.Empty(At(On(10, 4)));
    }
}
