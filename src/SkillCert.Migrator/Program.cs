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
builder.Services.AddScoped<DemoUserSeeder>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<SkillCertDbContext>();

var pending = string.Join(", ", await db.Database.GetPendingMigrationsAsync());
Log.ApplyingMigrations(logger, pending.Length == 0 ? "none" : pending);
await db.Database.MigrateAsync();
Log.DatabaseUpToDate(logger);

var config = host.Services.GetRequiredService<IConfiguration>();
if (config.GetValue<bool>("Seed:DemoUsers"))
{
    var password = config["Seed:DemoPassword"]
        ?? throw new InvalidOperationException("Seed:DemoPassword must be set when Seed:DemoUsers is true.");
    var created = await scope.ServiceProvider.GetRequiredService<DemoUserSeeder>().SeedAsync(password);
    Log.DemoUsersSeeded(logger, created);
}

return 0;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Pending migrations: {Migrations}")]
    public static partial void ApplyingMigrations(ILogger logger, string migrations);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database is up to date")]
    public static partial void DatabaseUpToDate(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo users created: {Created}")]
    public static partial void DemoUsersSeeded(ILogger logger, int created);
}
