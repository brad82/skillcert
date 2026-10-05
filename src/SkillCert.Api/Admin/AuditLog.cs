using System.Text.Json;
using SkillCert.Api.Common;
using SkillCert.Domain.Audit;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Admin;

/// <summary>
/// Adds audit entries to the same unit of work as the change, so a change and its entry commit together
/// (spec §23). Snapshots are small anonymous objects of the values an admin would want to compare.
/// </summary>
public sealed class AuditLog(SkillCertDbContext db, TimeProvider time)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Record(Guid actorUserId, string action, string entityType, Guid entityId, object? before, object? after) =>
        db.AuditEntries.Add(AuditEntry.Record(
            actorUserId,
            action,
            entityType,
            entityId,
            before is null ? null : JsonSerializer.Serialize(before, Json),
            after is null ? null : JsonSerializer.Serialize(after, Json),
            time.GetUtcNowForStorage()));
}
