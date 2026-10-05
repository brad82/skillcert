using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminUsers;

public sealed record AdminUsersResponse(IReadOnlyList<AdminUserDto> Users, IReadOnlyList<AdminClassificationDto> Classifications);

/// <summary>A reviewer classification an administrator can assign (spec §7: definitions are DB-configured).</summary>
public sealed record AdminClassificationDto(string Code, string Name, int Rank);

/// <summary>
/// GET /api/admin/users?search=: every user, ordered by name, optionally filtered by name or email. Includes the
/// assignable classifications so the screen needs one request. Deactivated users are listed too (spec §5).
/// </summary>
public static class ListUsersEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("", HandleAsync).WithName("ListUsers");

    internal static async Task<Ok<AdminUsersResponse>> HandleAsync(string? search, SkillCertDbContext db, CancellationToken cancellationToken)
    {
        var users = db.DomainUsers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            users = users.Where(u => EF.Functions.ILike(u.DisplayName, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        var list = await AdminUserQuery.Project(db, users.OrderBy(u => u.DisplayName)).ToListAsync(cancellationToken);
        var classifications = await db.ReviewerClassifications.OrderBy(c => c.Rank)
            .Select(c => new AdminClassificationDto(c.Code, c.Name, c.Rank))
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(new AdminUsersResponse(list, classifications));
    }
}
