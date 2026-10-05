using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace SkillCert.Infrastructure.Persistence;

public static class SkillCertDbContextOptions
{
    public const string ConnectionName = "skillcert";

    /// <summary>Shared EF options so the API, migrator and design-time tooling build an identical model.</summary>
    public static void Configure(DbContextOptionsBuilder options) =>
        options.UseSnakeCaseNamingConvention();

    public static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsAssembly(typeof(SkillCertDbContext).Assembly.GetName().Name);
}
