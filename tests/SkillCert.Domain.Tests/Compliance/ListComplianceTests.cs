using SkillCert.Domain.Compliance;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Reviews;
using static SkillCert.Domain.Reviews.ConfirmationStatus;
using static SkillCert.Domain.Reviews.ReviewOutcome;

namespace SkillCert.Domain.Tests.Compliance;

/// <summary>Spec §21. Dates are 2026 unless stated; times UTC noon.</summary>
public sealed class ListComplianceTests
{
    private static DateTimeOffset On(int month, int day, int year = 2026) => new(year, month, day, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Oct4 = On(10, 4);

    /// <summary>One competency's revisions and one user's reviews of it.</summary>
    private sealed class Skill
    {
        private readonly List<RevisionPolicy> _revisions = [];
        private readonly List<ReviewEvidence> _reviews = [];

        public Skill(int? recertDays = 365) => Revision(recertDays);

        public Skill Revision(int? recertDays = 365, bool breaking = false, DateTimeOffset? published = null)
        {
            _revisions.Add(new(Guid.NewGuid(), _revisions.Count + 1, recertDays, breaking, published ?? On(1, 1, 2024)));
            return this;
        }

        public Skill Review(DateTimeOffset at, ReviewOutcome outcome = Competent, ConfirmationStatus status = NotRequired, int revision = 1)
        {
            _reviews.Add(new(Guid.NewGuid(), _revisions[revision - 1].Id, outcome, status, at, at));
            return this;
        }

        public CompetencyEvidence Evidence => new(_reviews, _revisions);

        public CompetencyCurrency At(DateTimeOffset asOf) => CurrencyEvaluator.Evaluate(_reviews, _revisions, asOf);
    }

    [Fact]
    public void An_empty_list_is_never_compliant_and_one_non_current_competency_breaks_it()
    {
        var current = new Skill().Review(On(9, 1));
        var expired = new Skill(recertDays: 30).Review(On(1, 1));

        Assert.False(ListCompliance.IsCompliant([]));
        Assert.True(ListCompliance.IsCompliant([current.At(Oct4)]));
        Assert.False(ListCompliance.IsCompliant([current.At(Oct4), expired.At(Oct4)]));
        Assert.Null(ListCompliance.CompletionDate([current.Evidence, expired.Evidence], Oct4));
        Assert.Null(ListCompliance.CompletionDate([], Oct4));
    }

    [Fact]
    public void Completion_is_when_the_last_competency_became_current()
    {
        var a = new Skill().Review(On(9, 1));
        var b = new Skill().Review(On(9, 10));

        Assert.Equal(On(9, 10), ListCompliance.CompletionDate([a.Evidence, b.Evidence], Oct4));
    }

    [Fact]
    public void Removing_the_missing_requirement_completes_the_list_on_the_remaining_evidence()
    {
        // Spec §21: remaining competencies achieved 1 Sept; the missing one is removed from the list on 4 Oct.
        var a = new Skill().Review(On(9, 1));
        var b = new Skill().Review(On(9, 1));
        var missing = new Skill();

        Assert.Null(ListCompliance.CompletionDate([a.Evidence, b.Evidence, missing.Evidence], On(10, 3)));
        Assert.Equal(On(9, 1), ListCompliance.CompletionDate([a.Evidence, b.Evidence], Oct4));
    }

    [Fact]
    public void Renewing_before_expiry_continues_the_run_and_renewing_after_a_lapse_starts_a_new_one()
    {
        var renewedInTime = new Skill().Review(On(9, 1, 2025)).Review(On(8, 1));
        var renewedLate = new Skill().Review(On(1, 1, 2025)).Review(On(3, 1));

        Assert.Equal(On(9, 1, 2025), ListCompliance.CurrentSince(renewedInTime.Evidence, Oct4));
        Assert.Equal(On(3, 1), ListCompliance.CurrentSince(renewedLate.Evidence, Oct4));
    }

    [Fact]
    public void Not_competent_and_breaking_revisions_end_the_run()
    {
        var reassessed = new Skill().Review(On(1, 10)).Review(On(3, 1), NotCompetent).Review(On(5, 1));
        var revised = new Skill().Review(On(1, 10)).Revision(breaking: true, published: On(6, 1)).Review(On(7, 1), revision: 2);

        Assert.Equal(On(5, 1), ListCompliance.CurrentSince(reassessed.Evidence, Oct4));
        Assert.Equal(On(7, 1), ListCompliance.CurrentSince(revised.Evidence, Oct4));
    }

    [Fact]
    public void Completion_uses_the_review_date_not_when_a_supervisor_confirmed_it()
    {
        // Spec §11: achieved 4 Oct, confirmed 18 Oct; completion is 4 Oct. Evidence carries ReviewedAt only.
        var confirmed = new Skill().Review(Oct4, status: Confirmed);
        var pending = new Skill().Review(Oct4, status: Pending);

        Assert.Equal(Oct4, ListCompliance.CompletionDate([confirmed.Evidence], On(10, 18)));
        Assert.Null(ListCompliance.CompletionDate([pending.Evidence], On(10, 18)));
    }
}
