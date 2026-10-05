namespace SkillCert.Domain.Competencies;

/// <summary>
/// One independently assessable skill with an identity that outlives its revisions and the lists
/// that use it (spec §2). The current revision is always the highest-numbered one.
/// </summary>
public sealed class Competency
{
    private readonly List<CompetencyRevision> _revisions = [];

    private Competency()
    {
    }

    private Competency(string code, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7(createdAt);
        Code = code.Trim();
        NormalizedCode = Normalize(code);
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    /// <summary>The code as written on the record, e.g. "4.3.1" or "9.1a". Codes never determine order.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Trimmed, upper-cased code; unique across all competencies, active or not.</summary>
    public string NormalizedCode { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<CompetencyRevision> Revisions => _revisions;

    public CompetencyRevision CurrentRevision =>
        _revisions.MaxBy(r => r.RevisionNumber)
        ?? throw new InvalidOperationException($"Competency {Code} has no revisions loaded.");

    public static string Normalize(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return code.Trim().ToUpperInvariant();
    }

    /// <summary>Creates a competency with its first revision (never breaking: there is nothing to invalidate).</summary>
    public static Competency Create(string code, RevisionContent content, DateTimeOffset at, Guid? byUserId)
    {
        var competency = new Competency(code, at);
        competency._revisions.Add(new CompetencyRevision(
            competency.Id, revisionNumber: 1, content, invalidatesPreviousReviews: false, at, byUserId));
        return competency;
    }

    /// <summary>
    /// "Publish new revision" (spec §3): substantive changes, including recertification policy. The administrator
    /// chooses whether earlier reviews stay acceptable (<paramref name="invalidatesPreviousReviews"/> = false) or stop
    /// counting from <paramref name="at"/>. Existing reviews keep the interval of the revision they were assessed against.
    /// </summary>
    public CompetencyRevision PublishRevision(
        RevisionContent content, bool invalidatesPreviousReviews, DateTimeOffset at, Guid? byUserId)
    {
        var current = CurrentRevision;
        if (at < current.PublishedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(at), "A new revision cannot be published before the current one.");
        }

        var revision = new CompetencyRevision(
            Id, current.RevisionNumber + 1, content, invalidatesPreviousReviews, at, byUserId);
        _revisions.Add(revision);
        return revision;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
