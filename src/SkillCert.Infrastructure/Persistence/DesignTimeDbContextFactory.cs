using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SkillCert.Infrastructure.Persistence;

/// <summary>Used only by `dotnet ef` to build the model; it never connects.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SkillCertDbContext>
{
    public SkillCertDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SkillCertDbContext>()
            .UseNpgsql("Host=localhost;Database=skillcert_design", SkillCertDbContextOptions.ConfigureNpgsql);
        SkillCertDbContextOptions.Configure(options);
        return new SkillCertDbContext(options.Options);
    }
}
