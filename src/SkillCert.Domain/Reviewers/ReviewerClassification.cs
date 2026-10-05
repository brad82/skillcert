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

    public ReviewerClassification(Guid id, string code, string name, AffirmationPolicy affirmationPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Code = code;
        Name = name;
        AffirmationPolicy = affirmationPolicy;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public AffirmationPolicy AffirmationPolicy { get; private set; }
}
