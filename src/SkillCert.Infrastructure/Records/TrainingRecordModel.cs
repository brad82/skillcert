namespace SkillCert.Infrastructure.Records;

/// <summary>
/// Everything a training-record PDF shows, already resolved to text and local dates (spec §21). The PDF must stand
/// on its own: nothing here is looked up again after rendering.
/// </summary>
/// <param name="CompletionDate">Null when the list isn't complete (a current-record download of a gap list).</param>
/// <param name="GeneratedOn">Server-local date of rendering, printed in the footer.</param>
/// <param name="Rows">The list as it stands, depth-first.</param>
public sealed record TrainingRecordModel(
    string CandidateName,
    string ListTitle,
    DateOnly? CompletionDate,
    DateOnly GeneratedOn,
    IReadOnlyList<TrainingRecordRow> Rows);

/// <param name="Depth">0 for a top-level section (dark band), 1+ for sub-sections (light band) and competencies.</param>
/// <param name="SignedOn">The effective sign-off date, shown only while the competency is Current.</param>
/// <param name="EvaluatorName">The reviewer-name snapshot on that review; printed as initials with a legend.</param>
public sealed record TrainingRecordRow(
    int Depth,
    bool IsHeading,
    string? Code,
    string Title,
    DateOnly? SignedOn,
    string? EvaluatorName);
