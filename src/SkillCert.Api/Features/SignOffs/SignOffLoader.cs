using Microsoft.EntityFrameworkCore;
using SkillCert.Domain.Competencies;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.SignOffs;

/// <summary>Loads the competencies a sign-off names, with the revision data the method check needs.</summary>
internal static class SignOffLoader
{
    /// <returns>Null when any id is unknown.</returns>
    public static async Task<IReadOnlyList<Competency>?> CompetenciesAsync(
        SkillCertDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        var competencies = await db.Competencies
            .Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications)
            .Where(c => distinct.Contains(c.Id))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        return competencies.Count == distinct.Count ? competencies : null;
    }
}
