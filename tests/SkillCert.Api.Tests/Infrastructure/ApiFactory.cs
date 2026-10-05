using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillCert.Infrastructure.Persistence;
using SkillCert.Infrastructure.Seed;
using Testcontainers.PostgreSql;

namespace SkillCert.Api.Tests.Infrastructure;

/// <summary>
/// The real API on a throwaway Postgres container: real migrations, real Identity, the full demo data seeded
/// (users, AFA list, groups, review history).
/// One instance per test run (see <see cref="ApiCollection"/>); tests that change shared data restore it.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoPassword = "SkillCert-test-2026";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkillCertDbContext>();
        await db.Database.MigrateAsync();
        await new DemoDataSeeder(
                ActivatorUtilities.CreateInstance<DemoUserSeeder>(scope.ServiceProvider),
                ActivatorUtilities.CreateInstance<DemoCatalogSeeder>(scope.ServiceProvider),
                ActivatorUtilities.CreateInstance<DemoHistorySeeder>(scope.ServiceProvider))
            .SeedAsync(DemoPassword);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:skillcert", _postgres.GetConnectionString());
    }

    /// <summary>Runs <paramref name="action"/> against the database in its own scope.</summary>
    public async Task WithDbAsync(Func<SkillCertDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<SkillCertDbContext>());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
