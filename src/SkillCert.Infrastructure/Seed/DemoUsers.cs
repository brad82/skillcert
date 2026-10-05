namespace SkillCert.Infrastructure.Seed;

/// <summary>Fake accounts for local dev and the hosted demo. Roles are assigned on the domain User in a later task.</summary>
public static class DemoUsers
{
    public sealed record DemoUser(string Email, string DisplayName, string Kind);

    public static readonly IReadOnlyList<DemoUser> All =
    [
        new("admin@skillcert.test", "Alex Admin", "Administrator"),
        new("instructor1@skillcert.test", "Ines Instructor", "Instructor"),
        new("instructor2@skillcert.test", "Ivan Instructor", "Instructor"),
        new("supervisor1@skillcert.test", "Sam Supervisor", "Supervisor"),
        new("supervisor2@skillcert.test", "Sofia Supervisor", "Supervisor"),
        .. Enumerable.Range(1, 10).Select(i =>
            new DemoUser($"candidate{i:00}@skillcert.test", $"Candidate {i:00}", "Candidate")),
    ];
}
