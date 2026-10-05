using SkillCert.Domain.Competencies;

namespace SkillCert.Domain.Reviews;

/// <summary>One competency in a sign-off and the reviewer's verdict on it.</summary>
public sealed record SignOffItem(Competency Competency, ReviewOutcome Outcome);

/// <summary>
/// One candidate-device interaction (spec §9–10): a reviewer assesses several competencies at once. Every review
/// shares one ReviewedAt and, when given, one signature. There is no batch entity; the reviews are the record.
/// </summary>
public static class SignOff
{
    public const string Duplicate = "signoff.duplicate";
    public const string SignatureRequired = "signoff.signature-required";
    public const string Empty = "signoff.empty";

    /// <param name="signature">Required for a classified reviewer (Instructor, Supervisor); optional otherwise.</param>
    public static IReadOnlyList<CompetencyReview> Record(
        Guid candidateUserId,
        Reviewer reviewer,
        IReadOnlyList<SignOffItem> items,
        ReviewSignature? signature,
        string? comment,
        DateTimeOffset reviewedAt,
        Guid createdByUserId)
    {
        if (items.Count == 0)
        {
            throw new DomainRuleException(Empty, "Choose at least one skill to sign off.");
        }

        if (items.Select(i => i.Competency.Id).Distinct().Count() != items.Count)
        {
            throw new DomainRuleException(Duplicate, "A skill can only be signed once per sign-off.");
        }

        if (reviewer.Method == ReviewMethod.Classified && signature is null)
        {
            throw new DomainRuleException(SignatureRequired, $"A {reviewer.Classification!.Name} sign-off needs a signature.");
        }

        return items
            .Select(item => CompetencyReview.Record(
                candidateUserId, item.Competency, item.Outcome, reviewedAt, reviewer, signature?.Id, comment,
                opportunityId: null, createdAt: reviewedAt, createdByUserId))
            .ToList();
    }
}
