namespace SkillCert.Domain.Records;

public enum TrainingRecordTrigger
{
    /// <summary>The nightly run saw the user become compliant with the list (spec §22).</summary>
    ComplianceAchieved,

    /// <summary>An administrator archived it by hand (Phase 5).</summary>
    AdministratorManual,
}

/// <summary>
/// An archived, immutable training-record PDF (spec §22). Created only after the PDF is safely in blob storage,
/// so a failed upload never looks like a completed archive. At most one ComplianceAchieved record exists per
/// user, list and completion date, which makes the nightly run idempotent.
/// </summary>
public sealed class TrainingRecord
{
    private TrainingRecord()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid CompetencyListId { get; private set; }

    /// <summary>Evidence-based: when the user completed the list, not when the PDF was made.</summary>
    public DateTimeOffset CompletionDate { get; private set; }

    public DateTimeOffset GeneratedAt { get; private set; }

    public string BlobPath { get; private set; } = null!;

    /// <summary>Lower-case hex SHA-256 of the stored PDF bytes.</summary>
    public string Sha256Hash { get; private set; } = null!;

    public TrainingRecordTrigger Trigger { get; private set; }

    /// <summary>The blob path for a new archive. Includes the id, so a retried upload never overwrites another.</summary>
    public static string PathFor(Guid id, Guid userId, Guid listId, DateTimeOffset completionDate) =>
        $"training-records/{userId}/{listId}/{completionDate:yyyyMMdd}-{id}.pdf";

    public static TrainingRecord Archived(
        Guid id, Guid userId, Guid listId, DateTimeOffset completionDate, DateTimeOffset generatedAt,
        string blobPath, string sha256Hash, TrainingRecordTrigger trigger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256Hash);
        return new TrainingRecord
        {
            Id = id,
            UserId = userId,
            CompetencyListId = listId,
            CompletionDate = completionDate,
            GeneratedAt = generatedAt,
            BlobPath = blobPath,
            Sha256Hash = sha256Hash,
            Trigger = trigger,
        };
    }
}
