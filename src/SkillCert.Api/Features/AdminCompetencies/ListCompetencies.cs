using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain.Reviewers;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminCompetencies;

public sealed record AdminCompetenciesResponse(IReadOnlyList<AdminCompetencyListItemDto> Competencies);

/// <summary>
/// GET /api/admin/competencies?search=: the competency library, active and inactive, ordered by code. Search
/// matches code or current title.
/// </summary>
public static class ListCompetenciesEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("", HandleAsync).WithName("ListCompetencies");

    internal static async Task<Ok<AdminCompetenciesResponse>> HandleAsync(string? search, SkillCertDbContext db, CancellationToken cancellationToken)
    {
        var competencies = await db.Competencies
            .Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var classifications = await db.ReviewerClassifications.AsNoTracking().ToListAsync(cancellationToken);
        var listCounts = await db.CompetencyLists
            .SelectMany(l => l.Nodes)
            .Where(n => n.CompetencyId != null)
            .GroupBy(n => n.CompetencyId!.Value)
            .Select(g => new { CompetencyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompetencyId, x => x.Count, cancellationToken);

        var needle = search?.Trim();
        var items = competencies
            .Select(c => (Competency: c, Revision: c.CurrentRevision))
            .Where(x => string.IsNullOrEmpty(needle)
                || x.Competency.Code.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || x.Revision.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (x.Revision.ShortTitle?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(x => x.Competency.Code, NaturalCodeComparer.Instance)
            .Select(x => new AdminCompetencyListItemDto(
                x.Competency.Id,
                x.Competency.Code,
                x.Revision.Title,
                x.Revision.ShortTitle ?? x.Revision.Title,
                x.Competency.IsActive,
                x.Revision.RevisionNumber,
                x.Revision.RecertificationDays,
                ReviewLevelDto.From(ReviewHierarchy.Lowest(x.Revision, classifications)),
                listCounts.GetValueOrDefault(x.Competency.Id)))
            .ToList();
        return TypedResults.Ok(new AdminCompetenciesResponse(items));
    }
}
