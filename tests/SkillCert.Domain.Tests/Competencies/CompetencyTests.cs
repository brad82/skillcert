using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Tests.TestData;

namespace SkillCert.Domain.Tests.Competencies;

public sealed class CompetencyTests
{
    private static readonly DateTimeOffset Sept1 = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_at_revision_1_which_is_never_breaking()
    {
        var competency = Competency.Create(" 4.3.1 ", Content.Revision(), Sept1, byUserId: null);

        Assert.Equal("4.3.1", competency.Code);
        Assert.True(competency.IsActive);
        var revision = Assert.Single(competency.Revisions);
        Assert.Equal(1, revision.RevisionNumber);
        Assert.False(revision.InvalidatesPreviousReviews);
        Assert.Same(revision, competency.CurrentRevision);
    }

    [Theory]
    [InlineData("9.1a", "9.1A")]
    [InlineData("  10.2e ", "10.2E")]
    public void Codes_are_normalized_for_uniqueness(string code, string normalized)
    {
        var competency = Competency.Create(code, Content.Revision(), Sept1, null);

        Assert.Equal(normalized, competency.NormalizedCode);
    }

    [Fact]
    public void Publishing_adds_the_next_revision_and_it_becomes_current()
    {
        var competency = Competency.Create("4.3.1", Content.Revision(), Sept1, null);

        var revision2 = competency.PublishRevision(
            Content.Revision(recertificationDays: 730), invalidatesPreviousReviews: true, Sept1.AddDays(30), null);

        Assert.Equal(2, revision2.RevisionNumber);
        Assert.True(revision2.InvalidatesPreviousReviews);
        Assert.Same(revision2, competency.CurrentRevision);
        Assert.Equal(365, competency.Revisions[0].RecertificationDays);
    }

    [Fact]
    public void A_revision_cannot_be_published_before_the_current_one()
    {
        var competency = Competency.Create("4.3.1", Content.Revision(), Sept1, null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            competency.PublishRevision(Content.Revision(), false, Sept1.AddDays(-1), null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Recertification_days_must_be_positive_or_null(int days)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Competency.Create("4.3.1", Content.Revision(recertificationDays: days), Sept1, null));
    }

    [Fact]
    public void Null_recertification_days_means_no_expiry_and_is_allowed()
    {
        var competency = Competency.Create("3.2", Content.Revision(recertificationDays: null), Sept1, null);

        Assert.Null(competency.CurrentRevision.RecertificationDays);
    }

    [Fact]
    public void A_revision_must_permit_at_least_one_review_method()
    {
        Assert.Throws<ArgumentException>(() =>
            Competency.Create("4.3.1", Content.Revision(self: false, peer: false, classifications: []), Sept1, null));
    }

    [Fact]
    public void Editorial_correction_changes_the_text_without_a_new_revision()
    {
        var competency = Competency.Create("4.3.1", Content.Revision(title: "CPR - one rescuer adlt"), Sept1, null);

        competency.CurrentRevision.CorrectEditorially(Content.Revision(
            title: "CPR – one-rescuer adult", classifications: [ReviewerClassification.InstructorId, ReviewerClassification.SupervisorId]));

        var revision = Assert.Single(competency.Revisions);
        Assert.Equal("CPR – one-rescuer adult", revision.Title);
        Assert.True(revision.PermitsClassification(ReviewerClassification.SupervisorId));
    }

    [Fact]
    public void Resources_must_be_absolute_http_urls()
    {
        Assert.Throws<ArgumentException>(() =>
            new RevisionResource("Guide", new Uri("/relative/path", UriKind.Relative), ResourceType.Document));
        Assert.Throws<ArgumentException>(() =>
            new RevisionResource("Guide", new Uri("ftp://example.org/x.pdf"), ResourceType.Document));
    }
}
