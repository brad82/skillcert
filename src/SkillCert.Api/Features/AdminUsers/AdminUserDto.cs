using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminUsers;

/// <param name="Classifications">Reviewer classification codes held now, e.g. "Instructor".</param>
/// <param name="Groups">Names of the active groups the user belongs to.</param>
public sealed record AdminUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsActive,
    bool IsAdministrator,
    IReadOnlyList<string> Classifications,
    IReadOnlyList<string> Groups,
    DateTimeOffset CreatedAt);

internal static class AdminUserQuery
{
    /// <summary>Users with their classifications and groups, projected in one query.</summary>
    public static IQueryable<AdminUserDto> Project(SkillCertDbContext db, IQueryable<SkillCert.Domain.Users.User> users) =>
        users.Select(u => new AdminUserDto(
            u.Id,
            u.DisplayName,
            u.Email,
            u.IsActive,
            u.IsAdministrator,
            db.ReviewerClassifications
                .Where(rc => u.Classifications.Any(c => c.ReviewerClassificationId == rc.Id))
                .OrderBy(rc => rc.Rank)
                .Select(rc => rc.Code)
                .ToList(),
            db.UserGroups
                .Where(g => g.IsActive && g.Members.Any(m => m.UserId == u.Id))
                .OrderBy(g => g.Name)
                .Select(g => g.Name)
                .ToList(),
            u.CreatedAt));

    public static Task<AdminUserDto?> SingleAsync(SkillCertDbContext db, Guid id, CancellationToken cancellationToken) =>
        Project(db, db.DomainUsers.Where(u => u.Id == id)).SingleOrDefaultAsync(cancellationToken);
}
