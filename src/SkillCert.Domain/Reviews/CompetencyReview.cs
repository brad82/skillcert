using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviewers;

namespace SkillCert.Domain.Reviews;

/// <summary>
/// One assessment of one candidate against one competency revision (spec §8). Assessment evidence is
/// immutable: a reassessment is a new review. The only change ever made is the one-time confirmation
/// decision. No batch or event identity is stored: reviews from one interaction merely share ReviewedAt
/// and a signature.
/// </summary>
public sealed class CompetencyReview
{
    public const string MethodNotPermitted = "review.method-not-permitted";
    public const string NotPending = "review.not-pending";
    public const string NotNamedReviewer = "review.not-named-reviewer";
    public const string CompetencyInactive = "review.competency-inactive";

    private CompetencyReview()
    {
    }

    public Guid Id { get; private set; }

    public Guid CandidateUserId { get; private set; }

    public Guid CompetencyId { get; private set; }

    public Guid CompetencyRevisionId { get; private set; }

    public ReviewOutcome Outcome { get; private set; }

    /// <summary>When the skill was shown. Server time; confirmation never moves it (spec §11).</summary>
    public DateTimeOffset ReviewedAt { get; private set; }

    public Guid ReviewerUserId { get; private set; }

    public ReviewMethod Method { get; private set; }

    /// <summary>The classification the reviewer held when assessing; survives its later removal.</summary>
    public Guid? ReviewerClassificationId { get; private set; }

    public string ReviewerName { get; private set; } = null!;

    public Guid? ReviewSignatureId { get; private set; }

    public string? Comment { get; private set; }

    /// <summary>Provenance only (spec §16): not a continuing condition of currency.</summary>
    public Guid? OpportunityId { get; private set; }

    public ConfirmationStatus ConfirmationStatus { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public Guid? ConfirmedByUserId { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    public Guid? RejectedByUserId { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    /// <summary>Counts as evidence for currency (spec §11).</summary>
    public bool IsAccepted => ConfirmationStatus is ConfirmationStatus.NotRequired or ConfirmationStatus.Confirmed;

    /// <summary>
    /// Records an assessment against the competency's <em>current</em> revision. The method must be permitted by
    /// that revision. A classification whose policy needs reviewer confirmation starts <see cref="ConfirmationStatus.Pending"/>;
    /// everything else is effective immediately.
    /// </summary>
    public static CompetencyReview Record(
        Guid candidateUserId,
        Competency competency,
        ReviewOutcome outcome,
        DateTimeOffset reviewedAt,
        Reviewer reviewer,
        Guid? signatureId,
        string? comment,
        Guid? opportunityId,
        DateTimeOffset createdAt,
        Guid createdByUserId)
    {
        if (!competency.IsActive)
        {
            throw new DomainRuleException(CompetencyInactive, $"{competency.Code} is no longer in use.");
        }

        var revision = competency.CurrentRevision;
        EnsureMethodPermitted(candidateUserId, revision, reviewer);

        return new CompetencyReview
        {
            Id = Guid.CreateVersion7(createdAt),
            CandidateUserId = candidateUserId,
            CompetencyId = competency.Id,
            CompetencyRevisionId = revision.Id,
            Outcome = outcome,
            ReviewedAt = reviewedAt,
            ReviewerUserId = reviewer.UserId,
            Method = reviewer.Method,
            ReviewerClassificationId = reviewer.Classification?.Id,
            ReviewerName = reviewer.DisplayName,
            ReviewSignatureId = signatureId,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            OpportunityId = opportunityId,
            ConfirmationStatus = reviewer.Classification?.AffirmationPolicy == AffirmationPolicy.ReviewerConfirmation
                ? ConfirmationStatus.Pending
                : ConfirmationStatus.NotRequired,
            CreatedAt = createdAt,
            CreatedByUserId = createdByUserId,
        };
    }

    /// <summary>
    /// The named reviewer accepts the claim. Achievement stays at <see cref="ReviewedAt"/>. Administrators cannot
    /// confirm on someone's behalf, and losing the classification later doesn't remove this right (spec §11).
    /// </summary>
    public void Confirm(Guid byUserId, DateTimeOffset at)
    {
        EnsureDecidableBy(byUserId);
        ConfirmationStatus = ConfirmationStatus.Confirmed;
        ConfirmedAt = at;
        ConfirmedByUserId = byUserId;
    }

    /// <summary>The named reviewer refuses the claim. The evidence is kept but never counts.</summary>
    public void Reject(Guid byUserId, DateTimeOffset at, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureDecidableBy(byUserId);
        ConfirmationStatus = ConfirmationStatus.Rejected;
        RejectedAt = at;
        RejectedByUserId = byUserId;
        RejectionReason = reason.Trim();
    }

    private void EnsureDecidableBy(Guid byUserId)
    {
        if (ConfirmationStatus != ConfirmationStatus.Pending)
        {
            throw new DomainRuleException(NotPending, "This sign-off has already been decided.");
        }

        if (byUserId != ReviewerUserId)
        {
            throw new DomainRuleException(NotNamedReviewer, "Only the reviewer named on this sign-off can decide it.");
        }
    }

    private static void EnsureMethodPermitted(Guid candidateUserId, CompetencyRevision revision, Reviewer reviewer)
    {
        var permitted = reviewer.Method switch
        {
            ReviewMethod.Self => reviewer.UserId == candidateUserId && revision.AllowsSelfReview,
            ReviewMethod.Peer => reviewer.UserId != candidateUserId && revision.AllowsPeerReview,
            ReviewMethod.Classified => reviewer.UserId != candidateUserId
                && reviewer.Classification is { } classification
                && revision.PermitsClassification(classification.Id),
            _ => false,
        };

        if (!permitted)
        {
            throw new DomainRuleException(
                MethodNotPermitted, $"\"{revision.Title}\" can't be signed off that way.");
        }
    }
}
