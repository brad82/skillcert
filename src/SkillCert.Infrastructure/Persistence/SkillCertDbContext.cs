using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Identity;

namespace SkillCert.Infrastructure.Persistence;

public sealed class SkillCertDbContext(DbContextOptions<SkillCertDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(SkillCertDbContext).Assembly);
    }
}
