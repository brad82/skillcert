using Microsoft.EntityFrameworkCore;

namespace SkillCert.Infrastructure.Persistence;

public sealed class SkillCertDbContext(DbContextOptions<SkillCertDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkillCertDbContext).Assembly);
    }
}
