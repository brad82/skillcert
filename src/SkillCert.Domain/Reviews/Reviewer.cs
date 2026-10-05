using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Users;

namespace SkillCert.Domain.Reviews;

/// <summary>
/// Who is giving a review and in what capacity. Every reviewer is a registered, active user (spec §10).
/// Built only through the factories, which check the reviewer is allowed to act that way.
/// </summary>
public sealed record Reviewer
{
    public const string NotPermitted = "review.reviewer-not-permitted";

    private Reviewer(User user, ReviewMethod method, ReviewerClassification? classification)
    {
        if (!user.IsActive)
        {
            throw new DomainRuleException(NotPermitted, $"{user.DisplayName} is no longer active.");
        }

        UserId = user.Id;
        DisplayName = user.DisplayName;
        Method = method;
        Classification = classification;
    }

    public Guid UserId { get; }

    /// <summary>Snapshot stored on the review, so later name changes never rewrite history.</summary>
    public string DisplayName { get; }

    public ReviewMethod Method { get; }

    public ReviewerClassification? Classification { get; }

    /// <summary>The candidate signs themselves off.</summary>
    public static Reviewer Self(User candidate) => new(candidate, ReviewMethod.Self, null);

    /// <summary>Another registered user, not a manually typed name.</summary>
    public static Reviewer Peer(User peer) => new(peer, ReviewMethod.Peer, null);

    /// <summary>A user holding <paramref name="classification"/> right now (e.g. Instructor, Supervisor).</summary>
    public static Reviewer Classified(User reviewer, ReviewerClassification classification)
    {
        if (reviewer.Classifications.All(c => c.ReviewerClassificationId != classification.Id))
        {
            throw new DomainRuleException(NotPermitted, $"{reviewer.DisplayName} is not a {classification.Name}.");
        }

        return new Reviewer(reviewer, ReviewMethod.Classified, classification);
    }
}
