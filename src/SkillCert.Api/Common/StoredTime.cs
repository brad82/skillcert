namespace SkillCert.Api.Common;

public static class StoredTime
{
    /// <summary>
    /// Now, truncated to Postgres timestamp precision (microseconds). Use for any instant both stored and returned,
    /// so the response matches what a later read gives back (Linux clocks tick in 100 ns).
    /// </summary>
    public static DateTimeOffset GetUtcNowForStorage(this TimeProvider time)
    {
        var now = time.GetUtcNow();
        return now.AddTicks(-(now.Ticks % (TimeSpan.TicksPerMillisecond / 1000)));
    }
}
