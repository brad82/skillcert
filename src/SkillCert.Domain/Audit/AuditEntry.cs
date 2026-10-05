namespace SkillCert.Domain.Audit;

/// <summary>
/// One important administrative configuration change (spec §23): who, when, what, on which entity, with the
/// relevant before/after values as JSON. Written in the same transaction as the change it describes. Review
/// evidence is never audited here: it is immutable and carries its own actor and time.
/// </summary>
public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset At { get; private set; }

    public Guid ActorUserId { get; private set; }

    /// <summary>Stable dotted name, e.g. "competency.publish-revision".</summary>
    public string Action { get; private set; } = null!;

    /// <summary>"Competency", "CompetencyList", "User"…</summary>
    public string EntityType { get; private set; } = null!;

    public Guid EntityId { get; private set; }

    /// <summary>JSON snapshot of the relevant values before the change; null for creations.</summary>
    public string? Before { get; private set; }

    /// <summary>JSON snapshot after the change; null for removals.</summary>
    public string? After { get; private set; }

    public static AuditEntry Record(
        Guid actorUserId, string action, string entityType, Guid entityId, string? before, string? after, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        return new AuditEntry
        {
            Id = Guid.CreateVersion7(at),
            At = at,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = before,
            After = after,
        };
    }
}
