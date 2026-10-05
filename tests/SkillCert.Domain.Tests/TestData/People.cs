using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Users;

namespace SkillCert.Domain.Tests.TestData;

public static class People
{
    public static readonly DateTimeOffset Joined = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly ReviewerClassification Instructor =
        new(ReviewerClassification.InstructorId, "Instructor", "Instructor", AffirmationPolicy.Automatic, rank: 10);

    public static readonly ReviewerClassification Supervisor =
        new(ReviewerClassification.SupervisorId, "Supervisor", "Supervisor", AffirmationPolicy.ReviewerConfirmation, rank: 20);

    public static User Candidate(string name = "Jo Patroller") =>
        new($"sub-{Guid.NewGuid()}", name, $"{Guid.NewGuid():N}@skillcert.test", Joined);

    public static User Holding(ReviewerClassification classification, string name)
    {
        var user = Candidate(name);
        user.AssignClassification(classification.Id, Joined);
        return user;
    }
}
