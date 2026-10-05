using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Tests.TestData;

namespace SkillCert.Domain.Tests.Reviewers;

public sealed class ReviewHierarchyTests
{
    private static readonly ReviewerClassification[] All = [People.Instructor, People.Supervisor];

    [Fact]
    public void Permitting_instructor_also_permits_supervisor()
    {
        var closed = ReviewHierarchy.CloseUpward(Content.Revision(classifications: [People.Instructor.Id]), All);

        Assert.Equal([People.Instructor.Id, People.Supervisor.Id], closed.PermittedClassificationIds.Order());
        Assert.False(closed.AllowsPeerReview);
        Assert.False(closed.AllowsSelfReview);
    }

    [Fact]
    public void Permitting_supervisor_alone_stays_supervisor_only()
    {
        var closed = ReviewHierarchy.CloseUpward(Content.Revision(classifications: [People.Supervisor.Id]), All);

        Assert.Equal([People.Supervisor.Id], closed.PermittedClassificationIds);
    }

    [Fact]
    public void Permitting_self_permits_everything_and_peer_permits_every_classification()
    {
        var fromSelf = ReviewHierarchy.CloseUpward(Content.Revision(self: true, classifications: []), All);
        var fromPeer = ReviewHierarchy.CloseUpward(Content.Revision(peer: true, classifications: []), All);

        Assert.True(fromSelf.AllowsPeerReview);
        Assert.Equal(2, fromSelf.PermittedClassificationIds.Count);
        Assert.False(fromPeer.AllowsSelfReview);
        Assert.Equal(2, fromPeer.PermittedClassificationIds.Count);
    }

    [Theory]
    [InlineData(true, false, "Self")]
    [InlineData(false, true, "Peer")]
    [InlineData(false, false, "Instructor")]
    public void Lowest_level_names_the_basket_group(bool self, bool peer, string expected)
    {
        var content = ReviewHierarchy.CloseUpward(Content.Revision(self: self, peer: peer, classifications: [People.Instructor.Id]), All);
        var revision = Competency.Create("4.3.1", content, People.Joined, null).CurrentRevision;

        var lowest = ReviewHierarchy.Lowest(revision, All);

        Assert.Equal(expected, lowest.Classification?.Name ?? lowest.Method.ToString());
    }

    [Fact]
    public void A_supervisor_can_sign_an_instructor_level_skill_once_closed()
    {
        var supervisor = People.Holding(People.Supervisor, "Sam Supervisor");
        var competency = Competency.Create(
            "4.3.1", ReviewHierarchy.CloseUpward(Content.Revision(classifications: [People.Instructor.Id]), All), People.Joined, null);

        var review = CompetencyReview.Record(
            Guid.NewGuid(), competency, ReviewOutcome.Competent, People.Joined.AddDays(1),
            Reviewer.Classified(supervisor, People.Supervisor), null, null, null, People.Joined.AddDays(1), supervisor.Id);

        Assert.Equal(ConfirmationStatus.Pending, review.ConfirmationStatus);
    }

    private static CompetencyRevision Revision(bool self = false, bool peer = false, params Guid[] classifications) =>
        Competency.Create($"x{Guid.NewGuid():N}"[..8], ReviewHierarchy.CloseUpward(Content.Revision(self: self, peer: peer, classifications: classifications), All), People.Joined, null)
            .CurrentRevision;

    [Fact]
    public void A_reviewer_signs_at_the_lowest_level_they_hold_that_every_skill_permits()
    {
        var candidate = People.Candidate();
        var both = People.Holding(People.Instructor, "Ines and Sam");
        both.AssignClassification(People.Supervisor.Id, People.Joined);
        var instructorSkill = Revision(classifications: People.Instructor.Id);
        var supervisorSkill = Revision(classifications: People.Supervisor.Id);

        Assert.Equal("Instructor", ReviewHierarchy.ReviewerFor(both, candidate.Id, [instructorSkill], All)!.Classification!.Code);
        Assert.Equal("Supervisor", ReviewHierarchy.ReviewerFor(both, candidate.Id, [instructorSkill, supervisorSkill], All)!.Classification!.Code);
        Assert.Null(ReviewHierarchy.ReviewerFor(People.Holding(People.Instructor, "Ines"), candidate.Id, [instructorSkill, supervisorSkill], All));
    }

    [Fact]
    public void Anyone_else_signs_peer_skills_as_a_peer_and_the_candidate_signs_self_skills()
    {
        var candidate = People.Candidate();
        var supervisor = People.Holding(People.Supervisor, "Sam");

        Assert.Equal(ReviewMethod.Peer, ReviewHierarchy.ReviewerFor(supervisor, candidate.Id, [Revision(peer: true)], All)!.Method);
        Assert.Equal(ReviewMethod.Self, ReviewHierarchy.ReviewerFor(candidate, candidate.Id, [Revision(self: true)], All)!.Method);
        Assert.Null(ReviewHierarchy.ReviewerFor(candidate, candidate.Id, [Revision(peer: true)], All));
        Assert.Null(ReviewHierarchy.ReviewerFor(People.Candidate("Pat"), candidate.Id, [Revision(classifications: People.Instructor.Id)], All));
    }
}
