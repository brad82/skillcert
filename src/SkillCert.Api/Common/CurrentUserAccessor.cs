using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Common;

/// <summary>Resolves the signed-in principal to the active domain user's id (null when there is none).</summary>
public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor, SkillCertDbContext db)
{
    private Guid? _userId;
    private bool _resolved;

    public async Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken)
    {
        if (_resolved)
        {
            return _userId;
        }

        var subjectId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _userId = subjectId is null
            ? null
            : await db.DomainUsers
                .Where(u => u.ExternalSubjectId == subjectId && u.IsActive)
                .Select(u => (Guid?)u.Id)
                .SingleOrDefaultAsync(cancellationToken);
        _resolved = true;
        return _userId;
    }
}
