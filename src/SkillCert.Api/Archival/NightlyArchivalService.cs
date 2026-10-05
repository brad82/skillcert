namespace SkillCert.Api.Archival;

/// <summary>Settings (section "Archival").</summary>
/// <param name="Enabled">Off in tests, which drive <see cref="TrainingRecordArchiver"/> directly.</param>
/// <param name="RunAt">Server-local time of day for the nightly run, e.g. "02:00" (development plan §1.5).</param>
/// <param name="RunOnStartup">Also run once shortly after start: handy locally, where nobody waits for 2 a.m.</param>
public sealed record ArchivalOptions(bool Enabled = true, TimeOnly RunAt = default, bool RunOnStartup = false)
{
    public static ArchivalOptions From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Archival");
        return new ArchivalOptions(
            section.GetValue("Enabled", true),
            TimeOnly.TryParse(section["RunAt"], System.Globalization.CultureInfo.InvariantCulture, out var runAt) ? runAt : new TimeOnly(2, 0),
            section.GetValue("RunOnStartup", false));
    }

    /// <summary>The next server-local <see cref="RunAt"/> strictly after <paramref name="now"/>, as an instant.</summary>
    public DateTimeOffset NextRun(DateTimeOffset now, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var candidate = local.Date + RunAt.ToTimeSpan();
        if (candidate <= local.DateTime)
        {
            candidate = candidate.AddDays(1);
        }

        // Skip a wall-clock time that doesn't exist on a DST spring-forward night.
        while (zone.IsInvalidTime(candidate))
        {
            candidate = candidate.AddMinutes(30);
        }

        return new DateTimeOffset(candidate, zone.GetUtcOffset(candidate));
    }
}

/// <summary>Runs <see cref="TrainingRecordArchiver"/> every night at the configured local time (spec §22).</summary>
public sealed partial class NightlyArchivalService(
    IServiceScopeFactory scopes, TimeProvider time, ArchivalOptions options, ILogger<NightlyArchivalService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (options.RunOnStartup)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), time, stoppingToken);
            await RunOnceAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = options.NextRun(time.GetUtcNow(), time.LocalTimeZone);
            LogScheduled(logger, next);
            await Task.Delay(next - time.GetUtcNow(), time, stoppingToken);
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TrainingRecordArchiver>().RunAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRunFailed(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Next training-record archival run at {Next}.")]
    private static partial void LogScheduled(ILogger logger, DateTimeOffset next);

    [LoggerMessage(Level = LogLevel.Error, Message = "The training-record archival run failed; it runs again tomorrow.")]
    private static partial void LogRunFailed(ILogger logger, Exception exception);
}
