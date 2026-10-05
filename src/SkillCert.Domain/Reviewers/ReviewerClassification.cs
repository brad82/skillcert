namespace SkillCert.Domain.Reviewers;

/// <summary>
/// Reviewer authority held by users, e.g. Instructor or Supervisor (spec §7).
/// Definitions are DB-configured; the POC has no UI to create or edit them.
/// </summary>
public sealed class ReviewerClassification
{
    public static readonly Guid InstructorId = new("0199a8c0-0000-7000-8000-000000000001");
    public static readonly Guid SupervisorId = new("0199a8c0-0000-7000-8000-000000000002");

    private ReviewerClassification()
    {
    }

    public ReviewerClassification(Guid id, string code, string name, AffirmationPolicy affirmationPolicy, int rank)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rank);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Code = code;
        Name = name;
        AffirmationPolicy = affirmationPolicy;
        Rank = rank;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public AffirmationPolicy AffirmationPolicy { get; private set; }

    /// <summary>
    /// Position in the review hierarchy (POC decision, development plan §2.5): Self &lt; Peer &lt; every classification,
    /// and classifications ordered by rank (Instructor 10 &lt; Supervisor 20). Permitting a level permits every higher one.
    /// </summary>
    public int Rank { get; private set; }
}
