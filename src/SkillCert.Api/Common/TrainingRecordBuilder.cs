using SkillCert.Domain.Lists;
using SkillCert.Infrastructure.Records;

namespace SkillCert.Api.Common;

/// <summary>
/// Builds a training-record PDF model for one list from a candidate's record. Dates are server-local
/// (development plan §1.5). A competency shows its sign-off date and evaluator only while it is Current.
/// </summary>
public static class TrainingRecordBuilder
{
    public static TrainingRecordModel Build(
        CandidateRecord record, CompetencyList list, string candidateName, DateTimeOffset? completionDate, TimeProvider time)
    {
        var zone = time.LocalTimeZone;
        DateOnly Local(DateTimeOffset at) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, zone).DateTime);

        var depths = new Dictionary<Guid, int>();
        var rows = list.Walk().Select(node =>
        {
            var depth = node.ParentNodeId is { } parent ? depths[parent] + 1 : 0;
            depths[node.Id] = depth;
            if (node.CompetencyId is not { } competencyId)
            {
                return new TrainingRecordRow(depth, IsHeading: true, node.HeadingCode, node.HeadingTitle!, null, null);
            }

            var competency = record.Competencies[competencyId];
            var revision = competency.CurrentRevision;
            var currency = record.Currency[competencyId];
            var effective = currency.IsCurrent ? record.Reviews.SingleOrDefault(r => r.Id == currency.EffectiveReviewId) : null;
            return new TrainingRecordRow(
                depth,
                IsHeading: false,
                competency.Code,
                revision.ShortTitle ?? revision.Title,
                effective is null ? null : Local(effective.ReviewedAt),
                effective?.ReviewerName);
        }).ToList();

        return new TrainingRecordModel(
            candidateName,
            list.Title,
            completionDate is { } completed ? Local(completed) : null,
            Local(record.AsOf),
            rows);
    }

    /// <summary>"AFA Skills Record - Candidate 01.pdf", safe for a Content-Disposition header.</summary>
    public static string FileName(string listTitle, string candidateName)
    {
        var name = $"{listTitle} - {candidateName}";
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.' ? c : '_').ToArray());
        return $"{safe.Trim()}.pdf";
    }
}
