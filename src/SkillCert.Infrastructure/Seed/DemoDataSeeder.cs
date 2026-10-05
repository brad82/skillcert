namespace SkillCert.Infrastructure.Seed;

/// <summary>
/// Everything the dev and demo environments start with, in dependency order: login accounts and users,
/// the AFA Skills Record and groups, then review history. Each step is idempotent, so the migrator runs
/// this on every start.
/// </summary>
public sealed class DemoDataSeeder(DemoUserSeeder users, DemoCatalogSeeder catalog, DemoHistorySeeder history)
{
    public sealed record Result(int UsersCreated, bool CatalogCreated, int ReviewsCreated);

    public async Task<Result> SeedAsync(string demoPassword, CancellationToken cancellationToken = default)
    {
        var usersCreated = await users.SeedAsync(demoPassword, cancellationToken);
        var catalogCreated = await catalog.SeedAsync(cancellationToken);
        var reviewsCreated = await history.SeedAsync(cancellationToken);
        return new(usersCreated, catalogCreated, reviewsCreated);
    }
}
