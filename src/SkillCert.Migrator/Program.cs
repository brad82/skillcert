using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillCert.Infrastructure.Identity;
using SkillCert.Infrastructure.Persistence;
using SkillCert.Infrastructure.Seed;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<SkillCertDbContext>(
    SkillCertDbContextOptions.ConnectionName,
    configureDbContextOptions: SkillCertDbContextOptions.Configure);
builder.Services.ConfigureDbContext<SkillCertDbContext>(
    o => o.UseNpgsql(SkillCertDbContextOptions.ConfigureNpgsql));

builder.Services.AddSkillCertIdentityCore();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDemoDataSeeding();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<SkillCertDbContext>();

var pending = string.Join(", ", await db.Database.GetPendingMigrationsAsync());
Log.ApplyingMigrations(logger, pending.Length == 0 ? "none" : pending);
await db.Database.MigrateAsync();
Log.DatabaseUpToDate(logger);

var config = host.Services.GetRequiredService<IConfiguration>();
if (config.GetValue<bool>("Seed:DemoData"))
{
    var password = config["Seed:DemoPassword"]
        ?? throw new InvalidOperationException("Seed:DemoPassword must be set when Seed:DemoData is true.");
    var result = await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(password);
    Log.DemoDataSeeded(logger, result.UsersCreated, result.CatalogCreated, result.ReviewsCreated);
}

return 0;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Pending migrations: {Migrations}")]
    public static partial void ApplyingMigrations(ILogger logger, string migrations);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database is up to date")]
    public static partial void DatabaseUpToDate(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data: {Users} users created, AFA list created: {Catalog}, {Reviews} reviews created")]
    public static partial void DemoDataSeeded(ILogger logger, int users, bool catalog, int reviews);
}
