namespace SkillCert.Domain.Users;

/// <summary>A user currently holds a reviewer classification (spec §7). Administrators assign and remove these.</summary>
public sealed class UserReviewerClassification
{
    private UserReviewerClassification()
    {
    }

    internal UserReviewerClassification(Guid userId, Guid reviewerClassificationId, DateTimeOffset assignedAt)
    {
        UserId = userId;
        ReviewerClassificationId = reviewerClassificationId;
        AssignedAt = assignedAt;
    }

    public Guid UserId { get; private set; }

    public Guid ReviewerClassificationId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }
}
