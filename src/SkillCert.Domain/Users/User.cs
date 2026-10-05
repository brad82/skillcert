namespace SkillCert.Domain.Users;

/// <summary>
/// A person in the system. Every user can act as a candidate (spec §5); there is no separate Candidate entity.
/// Authentication is separate: <see cref="ExternalSubjectId"/> links to the login account (Identity now, OIDC later).
/// </summary>
public sealed class User
{
    private readonly List<UserReviewerClassification> _classifications = [];

    private User()
    {
    }

    public User(string externalSubjectId, string displayName, string email, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Id = Guid.CreateVersion7(createdAt);
        ExternalSubjectId = externalSubjectId;
        DisplayName = displayName.Trim();
        Email = email.Trim();
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string ExternalSubjectId { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    /// <summary>Administrative capability (spec §18). Separate from reviewer classifications and groups.</summary>
    public bool IsAdministrator { get; private set; }

    /// <summary>Users are deactivated, never deleted, because historical reviews reference them (spec §5).</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<UserReviewerClassification> Classifications => _classifications;

    public void GrantAdministrator() => IsAdministrator = true;

    public void RevokeAdministrator() => IsAdministrator = false;

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    public void AssignClassification(Guid classificationId, DateTimeOffset assignedAt)
    {
        if (_classifications.Any(c => c.ReviewerClassificationId == classificationId))
        {
            return;
        }

        _classifications.Add(new UserReviewerClassification(Id, classificationId, assignedAt));
    }

    public void RemoveClassification(Guid classificationId) =>
        _classifications.RemoveAll(c => c.ReviewerClassificationId == classificationId);
}
