using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Users;

namespace SkillCert.Domain.Tests.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_user_is_active_with_no_capabilities_and_trimmed_names()
    {
        var user = new User("subject-1", "  Jo Patroller ", " jo@skillcert.test ", Now);

        Assert.True(user.IsActive);
        Assert.False(user.IsAdministrator);
        Assert.Empty(user.Classifications);
        Assert.Equal("Jo Patroller", user.DisplayName);
        Assert.Equal("jo@skillcert.test", user.Email);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Theory]
    [InlineData("", "Jo", "jo@skillcert.test")]
    [InlineData("subject-1", " ", "jo@skillcert.test")]
    [InlineData("subject-1", "Jo", "")]
    public void Requires_subject_name_and_email(string subjectId, string displayName, string email)
    {
        Assert.ThrowsAny<ArgumentException>(() => new User(subjectId, displayName, email, Now));
    }

    [Fact]
    public void Assigning_the_same_classification_twice_keeps_one_assignment()
    {
        var user = new User("subject-1", "Jo", "jo@skillcert.test", Now);

        user.AssignClassification(ReviewerClassification.InstructorId, Now);
        user.AssignClassification(ReviewerClassification.InstructorId, Now.AddDays(1));

        var assignment = Assert.Single(user.Classifications);
        Assert.Equal(Now, assignment.AssignedAt);
    }

    [Fact]
    public void Classifications_are_additive_and_removable_independently()
    {
        var user = new User("subject-1", "Jo", "jo@skillcert.test", Now);
        user.AssignClassification(ReviewerClassification.InstructorId, Now);
        user.AssignClassification(ReviewerClassification.SupervisorId, Now);

        user.RemoveClassification(ReviewerClassification.InstructorId);

        var remaining = Assert.Single(user.Classifications);
        Assert.Equal(ReviewerClassification.SupervisorId, remaining.ReviewerClassificationId);
    }

    [Fact]
    public void Deactivation_is_reversible_and_keeps_the_user()
    {
        var user = new User("subject-1", "Jo", "jo@skillcert.test", Now);

        user.Deactivate();
        Assert.False(user.IsActive);

        user.Reactivate();
        Assert.True(user.IsActive);
    }
}
