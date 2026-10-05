namespace SkillCert.Infrastructure.Seed;

/// <summary>Fake accounts for local dev and the hosted demo.</summary>
public static class DemoUsers
{
    public enum DemoRole
    {
        Candidate,
        Administrator,
        Instructor,
        Supervisor,
    }

    public sealed record DemoUser(string Email, string DisplayName, DemoRole Role);

    public static readonly IReadOnlyList<DemoUser> All =
    [
        new("admin@skillcert.test", "Alex Admin", DemoRole.Administrator),
        new("instructor1@skillcert.test", "Ines Instructor", DemoRole.Instructor),
        new("instructor2@skillcert.test", "Ivan Instructor", DemoRole.Instructor),
        new("supervisor1@skillcert.test", "Sam Supervisor", DemoRole.Supervisor),
        new("supervisor2@skillcert.test", "Sofia Supervisor", DemoRole.Supervisor),
        .. Enumerable.Range(1, 10).Select(i =>
            new DemoUser($"candidate{i:00}@skillcert.test", $"Candidate {i:00}", DemoRole.Candidate)),
    ];
}
