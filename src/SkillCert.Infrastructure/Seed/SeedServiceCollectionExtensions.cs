using Microsoft.Extensions.DependencyInjection;

namespace SkillCert.Infrastructure.Seed;

public static class SeedServiceCollectionExtensions
{
    public static IServiceCollection AddDemoDataSeeding(this IServiceCollection services) =>
        services
            .AddScoped<DemoUserSeeder>()
            .AddScoped<DemoCatalogSeeder>()
            .AddScoped<DemoHistorySeeder>()
            .AddScoped<DemoDataSeeder>();
}
