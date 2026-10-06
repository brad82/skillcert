using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminAudit;

/// <param name="EntityLabel">A readable name for the entity now (code, list title or user name); null if it's gone.</param>
/// <param name="Before">The stored before-snapshot, as JSON.</param>
public sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset At,
    Guid ActorUserId,
    string ActorName,
    string Action,
    string EntityType,
    Guid EntityId,
    string? EntityLabel,
    JsonElement? Before,
    JsonElement? After);

/// <param name="HasMore">True when another page follows; ask again with <c>before</c> = the last entry's At.</param>
/// <param name="Actors">Everyone who appears in the log (including former administrators), for the "Who" filter.</param>
public sealed record AuditEntriesResponse(IReadOnlyList<AuditEntryDto> Entries, bool HasMore, IReadOnlyList<string> EntityTypes, IReadOnlyList<AuditActorDto> Actors);

public sealed record AuditActorDto(Guid Id, string Name);

/// <summary>
/// GET /api/admin/audit: the audit log, newest first, filtered by entity type, entity, actor and a date range
/// (spec §23: audit visibility is administrative). Pages of 50 by keyset (<c>before</c>).
/// </summary>
public static class ListAuditEntriesEndpoint
{
    public const int PageSize = 50;

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("", HandleAsync).WithName("ListAuditEntries");

    internal static async Task<Ok<AuditEntriesResponse>> HandleAsync(
        string? entityType,
        Guid? entityId,
        Guid? actorUserId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateTimeOffset? before,
        SkillCertDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.AuditEntries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        if (entityId is { } id)
        {
            query = query.Where(e => e.EntityId == id);
        }

        if (actorUserId is { } actor)
        {
            query = query.Where(e => e.ActorUserId == actor);
        }

        if (from is { } start)
        {
            query = query.Where(e => e.At >= start);
        }

        if (to is { } end)
        {
            query = query.Where(e => e.At < end);
        }

        if (before is { } cursor)
        {
            query = query.Where(e => e.At < cursor);
        }

        var page = await query.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).Take(PageSize + 1).ToListAsync(cancellationToken);
        var entries = page.Take(PageSize).ToList();

        var userIds = entries.Select(e => e.ActorUserId).Concat(entries.Where(e => e.EntityType == "User").Select(e => e.EntityId)).Distinct().ToList();
        var users = await db.DomainUsers.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
        var competencyIds = entries.Where(e => e.EntityType == "Competency").Select(e => e.EntityId).Distinct().ToList();
        var codes = await db.Competencies.Where(c => competencyIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Code, cancellationToken);
        var listIds = entries.Where(e => e.EntityType == "CompetencyList").Select(e => e.EntityId).Distinct().ToList();
        var lists = await db.CompetencyLists.Where(l => listIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Title, cancellationToken);
        var entityTypes = await db.AuditEntries.Select(e => e.EntityType).Distinct().OrderBy(t => t).ToListAsync(cancellationToken);
        var actors = await db.DomainUsers
            .Where(u => db.AuditEntries.Any(e => e.ActorUserId == u.Id))
            .OrderBy(u => u.DisplayName)
            .Select(u => new AuditActorDto(u.Id, u.DisplayName))
            .ToListAsync(cancellationToken);

        string? Label(string type, Guid entity) => type switch
        {
            "User" => users.GetValueOrDefault(entity),
            "Competency" => codes.GetValueOrDefault(entity),
            "CompetencyList" => lists.GetValueOrDefault(entity),
            _ => null,
        };

        static JsonElement? Parse(string? json) => json is null ? null : JsonDocument.Parse(json).RootElement.Clone();

        return TypedResults.Ok(new AuditEntriesResponse(
            entries.Select(e => new AuditEntryDto(
                e.Id, e.At, e.ActorUserId, users.GetValueOrDefault(e.ActorUserId) ?? "Unknown user", e.Action, e.EntityType, e.EntityId,
                Label(e.EntityType, e.EntityId), Parse(e.Before), Parse(e.After))).ToList(),
            page.Count > PageSize,
            entityTypes,
            actors));
    }
}
