namespace SkillCert.Domain.Reviewers;

/// <summary>How a review naming a classified reviewer becomes effective (spec §7).</summary>
public enum AffirmationPolicy
{
    /// <summary>Effective immediately; the review starts NotRequired.</summary>
    Automatic,

    /// <summary>The named reviewer must confirm; the review starts Pending.</summary>
    ReviewerConfirmation,
}
