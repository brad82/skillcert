using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillCert.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<SkillCertDbContext>(
    SkillCertDbContextOptions.ConnectionName,
    configureDbContextOptions: SkillCertDbContextOptions.Configure);
builder.Services.ConfigureDbContext<SkillCertDbContext>(
    o => o.UseNpgsql(SkillCertDbContextOptions.ConfigureNpgsql));

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<SkillCertDbContext>();

var pending = string.Join(", ", await db.Database.GetPendingMigrationsAsync());
Log.ApplyingMigrations(logger, pending.Length == 0 ? "none" : pending);
await db.Database.MigrateAsync();
Log.DatabaseUpToDate(logger);

return 0;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Pending migrations: {Migrations}")]
    public static partial void ApplyingMigrations(ILogger logger, string migrations);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database is up to date")]
    public static partial void DatabaseUpToDate(ILogger logger);
}
