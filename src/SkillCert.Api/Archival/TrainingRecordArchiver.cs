using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain.Records;
using SkillCert.Infrastructure.Persistence;
using SkillCert.Infrastructure.Records;
using SkillCert.Infrastructure.Storage;

namespace SkillCert.Api.Archival;

/// <summary>What one archival pass did.</summary>
public sealed record ArchivalRunResult(int UsersChecked, int Archived, int Failed, bool Skipped);

/// <summary>
/// One nightly pass of the compliance engine (spec §22). For every active user and each list required of them:
/// <list type="bullet">
/// <item>compliant, and the checkpoint says it wasn't (or there is none) → archive a PDF, then record compliance;</item>
/// <item>compliant and already known compliant → keep the established completion date, archive nothing;</item>
/// <item>not compliant → record that, so the next compliant night archives again.</item>
/// </list>
/// The archive row is written only after the upload succeeds, in the same save as the checkpoint, so a failure
/// leaves no completed record and the next run retries. A unique index per user, list and completion date makes
/// re-runs and races harmless.
/// </summary>
public sealed partial class TrainingRecordArchiver(
    SkillCertDbContext db, IBlobStore blobs, TimeProvider time, ILogger<TrainingRecordArchiver> logger)
{
    public async Task<ArchivalRunResult> RunAsync(CancellationToken cancellationToken)
    {
        var userIds = await db.UserGroups
            .Where(g => g.IsActive)
            .SelectMany(g => g.Members)
            .Select(m => m.UserId)
            .Distinct()
            .Where(id => db.DomainUsers.Any(u => u.Id == id && u.IsActive))
            .ToListAsync(cancellationToken);
        return await RunForUsersAsync(userIds, cancellationToken);
    }

    public async Task<ArchivalRunResult> RunForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (!blobs.IsConfigured)
        {
            LogSkipped(logger);
            return new ArchivalRunResult(0, 0, 0, Skipped: true);
        }

        var now = time.GetUtcNowForStorage();
        var (archived, failed) = (0, 0);
        foreach (var userId in userIds)
        {
            var (userArchived, userFailed) = await RunForUserAsync(userId, now, cancellationToken);
            archived += userArchived;
            failed += userFailed;
            db.ChangeTracker.Clear();
        }

        LogFinished(logger, userIds.Count, archived, failed);
        return new ArchivalRunResult(userIds.Count, archived, failed, Skipped: false);
    }

    private async Task<(int Archived, int Failed)> RunForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await CandidateRecord.LoadAsync(db, userId, now, cancellationToken);
        var name = await db.DomainUsers.Where(u => u.Id == userId).Select(u => u.DisplayName).SingleAsync(cancellationToken);
        var checkpoints = await db.ComplianceCheckpoints.Where(c => c.UserId == userId).ToDictionaryAsync(c => c.CompetencyListId, cancellationToken);
        var (archived, failed) = (0, 0);

        foreach (var list in record.Lists)
        {
            var known = checkpoints.GetValueOrDefault(list.Id);
            var checkpoint = known ?? new ComplianceCheckpoint(userId, list.Id);

            if (!record.IsCompliant(list))
            {
                checkpoint.ObserveNonCompliant(now);
            }
            else if (!checkpoint.WouldBeNewlyCompliant)
            {
                checkpoint.ObserveCompliant(checkpoint.CompletionDate!.Value, now);
            }
            else
            {
                var completion = record.CompletionDate(list)!.Value;
                var alreadyArchived = await db.TrainingRecords.AnyAsync(
                    r => r.UserId == userId && r.CompetencyListId == list.Id && r.CompletionDate == completion
                        && r.Trigger == TrainingRecordTrigger.ComplianceAchieved,
                    cancellationToken);
                if (!alreadyArchived)
                {
                    try
                    {
                        var pdf = TrainingRecordPdf.Render(TrainingRecordBuilder.Build(record, list, name, completion, time));
                        var id = Guid.CreateVersion7(now);
                        var path = TrainingRecord.PathFor(id, userId, list.Id, completion);
                        await blobs.PutAsync(path, pdf, "application/pdf", cancellationToken);
                        db.TrainingRecords.Add(TrainingRecord.Archived(
                            id, userId, list.Id, completion, now, path,
                            Convert.ToHexStringLower(SHA256.HashData(pdf)), TrainingRecordTrigger.ComplianceAchieved));
                        archived++;
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        // Leave the checkpoint as it was: the next run sees the user newly compliant and retries.
                        LogArchiveFailed(logger, exception, userId, list.Id);
                        failed++;
                        continue;
                    }
                }

                checkpoint.ObserveCompliant(completion, now);
            }

            if (known is null)
            {
                db.ComplianceCheckpoints.Add(checkpoint);
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Another run archived the same completion first; this user is retried (and found archived) next run.
            LogSaveFailed(logger, exception, userId);
            return (0, failed + archived);
        }

        return (archived, failed);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Training-record archival skipped: blob storage isn't configured.")]
    private static partial void LogSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Training-record archival checked {Users} users: {Archived} archived, {Failed} failed.")]
    private static partial void LogFinished(ILogger logger, int users, int archived, int failed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Archiving the training record for user {UserId}, list {ListId} failed; it will be retried next run.")]
    private static partial void LogArchiveFailed(ILogger logger, Exception exception, Guid userId, Guid listId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Saving archival results for user {UserId} failed; they will be retried next run.")]
    private static partial void LogSaveFailed(ILogger logger, Exception exception, Guid userId);
}
