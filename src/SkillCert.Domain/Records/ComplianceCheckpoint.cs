namespace SkillCert.Domain.Records;

/// <summary>
/// What the nightly run last observed for one user and list (spec §22: an operational checkpoint, never the
/// authority for current compliance). It turns "compliant now" into "newly compliant": archive on the first
/// compliant observation or a NonCompliant → Compliant change, and keep the established completion date while
/// compliance continues. Changes that reverse between runs are never seen, by design.
/// </summary>
public sealed class ComplianceCheckpoint
{
    private ComplianceCheckpoint()
    {
    }

    public ComplianceCheckpoint(Guid userId, Guid listId)
    {
        UserId = userId;
        CompetencyListId = listId;
    }

    public Guid UserId { get; private set; }

    public Guid CompetencyListId { get; private set; }

    public bool IsCompliant { get; private set; }

    /// <summary>The completion date established when compliance began; kept while it continues.</summary>
    public DateTimeOffset? CompletionDate { get; private set; }

    public DateTimeOffset? ObservedAt { get; private set; }

    /// <summary>True when a compliant observation now would be new: first ever, or after a non-compliant one.</summary>
    public bool WouldBeNewlyCompliant => !IsCompliant;

    public void ObserveCompliant(DateTimeOffset completionDate, DateTimeOffset at)
    {
        if (!IsCompliant)
        {
            CompletionDate = completionDate;
        }

        IsCompliant = true;
        ObservedAt = at;
    }

    public void ObserveNonCompliant(DateTimeOffset at)
    {
        IsCompliant = false;
        CompletionDate = null;
        ObservedAt = at;
    }
}
