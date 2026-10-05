using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Domain;
using SkillCert.Domain.Lists;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminLists;

public sealed record AdminListSummaryDto(Guid Id, string Title, string? Description, bool IsActive, int CompetencyCount, IReadOnlyList<string> Groups);

/// <param name="Nodes">Depth-first display order; rebuild the tree from ParentNodeId.</param>
public sealed record AdminListDto(Guid Id, string Title, string? Description, bool IsActive, IReadOnlyList<string> Groups, IReadOnlyList<AdminListNodeDto> Nodes);

/// <param name="Index">Position among its siblings, 0-based.</param>
/// <param name="Competency">Set for competency leaves.</param>
public sealed record AdminListNodeDto(
    Guid Id,
    Guid? ParentNodeId,
    int Depth,
    int Index,
    ListNodeKind Kind,
    string? HeadingCode,
    string? HeadingTitle,
    AdminListCompetencyDto? Competency);

public sealed record AdminListCompetencyDto(Guid Id, string Code, string Title, string ShortTitle, bool IsActive);

internal static class AdminListQuery
{
    public static async Task<AdminListDto?> LoadAsync(SkillCertDbContext db, Guid listId, CancellationToken cancellationToken)
    {
        var list = await db.CompetencyLists.Include(l => l.Nodes).AsNoTracking().SingleOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
        {
            return null;
        }

        var competencyIds = list.CompetencyIds.ToList();
        var competencies = await db.Competencies.Include(c => c.Revisions).AsNoTracking()
            .Where(c => competencyIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var groups = await GroupsAsync(db, listId, cancellationToken);

        var depths = new Dictionary<Guid, int>();
        var nodes = list.Walk().Select(node =>
        {
            var depth = node.ParentNodeId is { } parent ? depths[parent] + 1 : 0;
            depths[node.Id] = depth;
            AdminListCompetencyDto? competency = null;
            if (node.CompetencyId is { } id)
            {
                var c = competencies[id];
                var revision = c.CurrentRevision;
                competency = new AdminListCompetencyDto(c.Id, c.Code, revision.Title, revision.ShortTitle ?? revision.Title, c.IsActive);
            }

            return new AdminListNodeDto(node.Id, node.ParentNodeId, depth, node.SortOrder, node.Kind, node.HeadingCode, node.HeadingTitle, competency);
        }).ToList();

        return new AdminListDto(list.Id, list.Title, list.Description, list.IsActive, groups, nodes);
    }

    public static Task<List<string>> GroupsAsync(SkillCertDbContext db, Guid listId, CancellationToken cancellationToken) =>
        db.UserGroups.Where(g => g.AssignedLists.Any(a => a.CompetencyListId == listId)).OrderBy(g => g.Name).Select(g => g.Name).ToListAsync(cancellationToken);
}

/// <summary>
/// Applies one tree change to a tracked list, audits it and saves. The list enforces its rules (parents are headings,
/// no cycles, one occurrence per competency); the database's unique index catches two admins adding the same
/// competency at once. Refusals become 409, unknown nodes 404.
/// </summary>
internal static class ListEditing
{
    public static async Task<Results<Ok<AdminListDto>, NotFound, ProblemHttpResult>> ApplyAsync(
        Guid listId,
        string action,
        Func<CompetencyList, object> change,
        CurrentUserAccessor currentUser,
        SkillCertDbContext db,
        AuditLog audit,
        CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        var list = await db.CompetencyLists.Include(l => l.Nodes).SingleOrDefaultAsync(l => l.Id == listId, cancellationToken);
        if (list is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var details = change(list);
            audit.Record(adminId, action, "CompetencyList", list.Id, null, details);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (DomainRuleException exception)
        {
            return RuleProblems.From(exception, StatusCodes.Status409Conflict);
        }
        catch (DbUpdateException)
        {
            return RuleProblems.Create(CompetencyList.DuplicateCompetency, "That competency is already in this list.", StatusCodes.Status409Conflict);
        }

        return TypedResults.Ok((await AdminListQuery.LoadAsync(db, listId, cancellationToken))!);
    }
}
