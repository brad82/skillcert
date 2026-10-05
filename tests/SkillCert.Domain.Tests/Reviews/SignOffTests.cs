using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Tests.TestData;

namespace SkillCert.Domain.Tests.Reviews;

public sealed class SignOffTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static Competency Skill(string code) =>
        Competency.Create(code, Content.Revision(classifications: [People.Instructor.Id, People.Supervisor.Id]), People.Joined, null);

    private static ReviewSignature Signature() =>
        ReviewSignature.FromDrawing(new SignatureDrawing(300, 100, [[new(10, 10), new(50, 60)]]), At);

    [Fact]
    public void Every_review_shares_one_reviewed_at_and_one_signature()
    {
        var candidate = People.Candidate();
        var signature = Signature();
        var reviewer = Reviewer.Classified(People.Holding(People.Supervisor, "Sam"), People.Supervisor);

        var reviews = SignOff.Record(
            candidate.Id, reviewer, [new(Skill("4.3.1"), ReviewOutcome.Competent), new(Skill("4.3.2"), ReviewOutcome.NotCompetent)],
            signature, "Watch the depth.", At, candidate.Id);

        Assert.All(reviews, r => Assert.Equal((At, signature.Id, ConfirmationStatus.Pending), (r.ReviewedAt, r.ReviewSignatureId, r.ConfirmationStatus)));
        Assert.Equal([ReviewOutcome.Competent, ReviewOutcome.NotCompetent], reviews.Select(r => r.Outcome));
    }

    [Fact]
    public void Duplicates_and_a_missing_classified_signature_are_refused()
    {
        var candidate = People.Candidate();
        var skill = Skill("4.3.1");
        var instructor = Reviewer.Classified(People.Holding(People.Instructor, "Ines"), People.Instructor);

        var duplicate = Assert.Throws<DomainRuleException>(() => SignOff.Record(
            candidate.Id, instructor, [new(skill, ReviewOutcome.Competent), new(skill, ReviewOutcome.Competent)], Signature(), null, At, candidate.Id));
        var unsigned = Assert.Throws<DomainRuleException>(() => SignOff.Record(
            candidate.Id, instructor, [new(skill, ReviewOutcome.Competent)], null, null, At, candidate.Id));

        Assert.Equal(SignOff.Duplicate, duplicate.Code);
        Assert.Equal(SignOff.SignatureRequired, unsigned.Code);
    }

    [Fact]
    public void A_drawing_renders_as_server_owned_svg_and_out_of_bounds_points_are_refused()
    {
        var svg = new SignatureDrawing(300, 100, [[new(10.04, 20), new(30.55, 40)], [new(5, 5)]]).ToSvg();

        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 300 100\">", svg, StringComparison.Ordinal);
        Assert.Contains("d=\"M10 20L30.6 40M5 5L5 5\"", svg, StringComparison.Ordinal);
        Assert.Throws<DomainRuleException>(() => new SignatureDrawing(300, 100, [[new(301, 5)]]));
        Assert.Throws<DomainRuleException>(() => new SignatureDrawing(300, 100, []));
    }
}
