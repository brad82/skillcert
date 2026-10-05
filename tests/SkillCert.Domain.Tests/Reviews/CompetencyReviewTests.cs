using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Tests.TestData;

namespace SkillCert.Domain.Tests.Reviews;

public sealed class CompetencyReviewTests
{
    private static readonly DateTimeOffset Oct4 = new(2026, 10, 4, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Oct18 = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private static Competency Cpr(bool self = false, bool peer = false, params Guid[] classifications) =>
        Competency.Create("4.3.1", Content.Revision(self: self, peer: peer, classifications: classifications), People.Joined, null);

    private static CompetencyReview Record(Competency competency, Guid candidateId, Reviewer reviewer) =>
        CompetencyReview.Record(
            candidateId, competency, ReviewOutcome.Competent, Oct4, reviewer,
            signatureId: null, comment: null, opportunityId: null, createdAt: Oct4, createdByUserId: candidateId);

    [Fact]
    public void An_instructor_review_is_effective_immediately_and_snapshots_the_reviewer()
    {
        var candidate = People.Candidate();
        var instructor = People.Holding(People.Instructor, "Ines Instructor");

        var review = Record(Cpr(classifications: People.Instructor.Id), candidate.Id, Reviewer.Classified(instructor, People.Instructor));

        Assert.Equal(ConfirmationStatus.NotRequired, review.ConfirmationStatus);
        Assert.True(review.IsAccepted);
        Assert.Equal("Ines Instructor", review.ReviewerName);
        Assert.Equal(ReviewMethod.Classified, review.Method);
        Assert.Equal(People.Instructor.Id, review.ReviewerClassificationId);
    }

    [Fact]
    public void Naming_a_supervisor_creates_a_pending_claim_that_does_not_count_yet()
    {
        var supervisor = People.Holding(People.Supervisor, "Sam Supervisor");

        var review = Record(Cpr(classifications: People.Supervisor.Id), People.Candidate().Id, Reviewer.Classified(supervisor, People.Supervisor));

        Assert.Equal(ConfirmationStatus.Pending, review.ConfirmationStatus);
        Assert.False(review.IsAccepted);
    }

    [Fact]
    public void Confirmation_is_retrospective_review_date_stays_put()
    {
        var supervisor = People.Holding(People.Supervisor, "Sam Supervisor");
        var review = Record(Cpr(classifications: People.Supervisor.Id), People.Candidate().Id, Reviewer.Classified(supervisor, People.Supervisor));

        review.Confirm(supervisor.Id, Oct18);

        Assert.Equal(ConfirmationStatus.Confirmed, review.ConfirmationStatus);
        Assert.Equal(Oct4, review.ReviewedAt);
        Assert.Equal(Oct18, review.ConfirmedAt);
        Assert.True(review.IsAccepted);
    }

    [Fact]
    public void Only_the_named_reviewer_can_decide_not_an_administrator()
    {
        var supervisor = People.Holding(People.Supervisor, "Sam Supervisor");
        var admin = People.Candidate("Alex Admin");
        admin.GrantAdministrator();
        var review = Record(Cpr(classifications: People.Supervisor.Id), People.Candidate().Id, Reviewer.Classified(supervisor, People.Supervisor));

        var error = Assert.Throws<DomainRuleException>(() => review.Confirm(admin.Id, Oct18));

        Assert.Equal(CompetencyReview.NotNamedReviewer, error.Code);
        Assert.Equal(ConfirmationStatus.Pending, review.ConfirmationStatus);
    }

    [Fact]
    public void Rejection_needs_a_reason_and_is_final()
    {
        var supervisor = People.Holding(People.Supervisor, "Sam Supervisor");
        var review = Record(Cpr(classifications: People.Supervisor.Id), People.Candidate().Id, Reviewer.Classified(supervisor, People.Supervisor));

        Assert.Throws<ArgumentException>(() => review.Reject(supervisor.Id, Oct18, " "));
        review.Reject(supervisor.Id, Oct18, "I did not observe this skill.");

        Assert.Equal(ConfirmationStatus.Rejected, review.ConfirmationStatus);
        Assert.False(review.IsAccepted);
        Assert.Equal(CompetencyReview.NotPending, Assert.Throws<DomainRuleException>(() => review.Confirm(supervisor.Id, Oct18)).Code);
    }

    [Fact]
    public void Losing_the_classification_after_the_claim_keeps_the_right_to_decide_it()
    {
        var supervisor = People.Holding(People.Supervisor, "Sam Supervisor");
        var review = Record(Cpr(classifications: People.Supervisor.Id), People.Candidate().Id, Reviewer.Classified(supervisor, People.Supervisor));

        supervisor.RemoveClassification(People.Supervisor.Id);
        review.Confirm(supervisor.Id, Oct18);

        Assert.Equal(ConfirmationStatus.Confirmed, review.ConfirmationStatus);
        Assert.Throws<DomainRuleException>(() => Reviewer.Classified(supervisor, People.Supervisor));
    }

    [Fact]
    public void Self_and_peer_are_effective_immediately_when_permitted()
    {
        var candidate = People.Candidate();
        var competency = Cpr(self: true, peer: true);

        Assert.Equal(ConfirmationStatus.NotRequired, Record(competency, candidate.Id, Reviewer.Self(candidate)).ConfirmationStatus);
        Assert.Equal(ConfirmationStatus.NotRequired, Record(competency, candidate.Id, Reviewer.Peer(People.Candidate("Pat Peer"))).ConfirmationStatus);
    }

    [Fact]
    public void A_method_the_revision_does_not_permit_is_refused()
    {
        var candidate = People.Candidate();
        var supervisorOnly = Cpr(classifications: People.Supervisor.Id);
        var instructor = People.Holding(People.Instructor, "Ines Instructor");

        Assert.Equal(CompetencyReview.MethodNotPermitted,
            Assert.Throws<DomainRuleException>(() => Record(supervisorOnly, candidate.Id, Reviewer.Self(candidate))).Code);
        Assert.Equal(CompetencyReview.MethodNotPermitted,
            Assert.Throws<DomainRuleException>(() => Record(supervisorOnly, candidate.Id, Reviewer.Classified(instructor, People.Instructor))).Code);
    }

    [Fact]
    public void Peer_cannot_be_the_candidate_and_self_cannot_be_someone_else()
    {
        var candidate = People.Candidate();
        var competency = Cpr(self: true, peer: true);

        Assert.Throws<DomainRuleException>(() => Record(competency, candidate.Id, Reviewer.Peer(candidate)));
        Assert.Throws<DomainRuleException>(() => Record(competency, candidate.Id, Reviewer.Self(People.Candidate("Other"))));
    }

    [Fact]
    public void New_reviews_always_use_the_current_revision()
    {
        var candidate = People.Candidate();
        var competency = Cpr(self: true);
        var revision2 = competency.PublishRevision(Content.Revision(self: true), false, People.Joined.AddDays(10), null);

        var review = Record(competency, candidate.Id, Reviewer.Self(candidate));

        Assert.Equal(revision2.Id, review.CompetencyRevisionId);
    }

    [Fact]
    public void Inactive_reviewers_and_inactive_competencies_are_refused()
    {
        var candidate = People.Candidate();
        var peer = People.Candidate("Pat Peer");
        peer.Deactivate();
        var retired = Cpr(peer: true, self: true);
        retired.Deactivate();

        Assert.Throws<DomainRuleException>(() => Reviewer.Peer(peer));
        Assert.Equal(CompetencyReview.CompetencyInactive,
            Assert.Throws<DomainRuleException>(() => Record(retired, candidate.Id, Reviewer.Self(candidate))).Code);
    }
}
