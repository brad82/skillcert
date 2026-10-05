namespace SkillCert.Domain.Competencies;

/// <summary>
/// Everything an administrator authors for one revision of a competency (spec §3).
/// </summary>
/// <param name="ShortTitle">The paper record's wording, shown under its heading (POC decision §2.2). Null → <paramref name="Title"/>.</param>
/// <param name="RecertificationDays">Elapsed 24-hour days until expiry; null means no time-based expiry.</param>
/// <param name="AllowsSelfReview">The candidate may sign themselves off.</param>
/// <param name="AllowsPeerReview">Another registered user may sign the candidate off.</param>
/// <param name="PermittedClassificationIds">Reviewer classifications (Instructor, Supervisor…) that may sign off.</param>
public sealed record RevisionContent(
    string Title,
    string? ShortTitle,
    string? Description,
    int? RecertificationDays,
    bool AllowsSelfReview,
    bool AllowsPeerReview,
    IReadOnlyCollection<Guid> PermittedClassificationIds,
    IReadOnlyList<RevisionResource> Resources)
{
    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        if (RecertificationDays is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RecertificationDays), RecertificationDays, "Recertification days must be a positive number of days, or null for no expiry.");
        }

        if (!AllowsSelfReview && !AllowsPeerReview && PermittedClassificationIds.Count == 0)
        {
            throw new ArgumentException("A competency must permit at least one review method.");
        }
    }
}
