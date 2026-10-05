using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Users;
using SkillCert.Infrastructure.Identity;
using SkillCert.Infrastructure.Persistence;
using static SkillCert.Infrastructure.Seed.DemoUsers;

namespace SkillCert.Infrastructure.Seed;

/// <summary>
/// Creates each demo login account and its linked domain User if missing. Safe to run on every migrator start.
/// </summary>
public sealed class DemoUserSeeder(
    UserManager<ApplicationUser> userManager,
    SkillCertDbContext db,
    TimeProvider timeProvider)
{
    public async Task<int> SeedAsync(string password, CancellationToken cancellationToken = default)
    {
        var created = 0;
        foreach (var demo in All)
        {
            var account = await userManager.FindByEmailAsync(demo.Email) ?? await CreateAccountAsync(demo, password);
            var subjectId = account.Id.ToString();

            if (await db.DomainUsers.AnyAsync(u => u.ExternalSubjectId == subjectId, cancellationToken))
            {
                continue;
            }

            db.DomainUsers.Add(CreateDomainUser(demo, subjectId, timeProvider.GetUtcNow()));
            await db.SaveChangesAsync(cancellationToken);
            created++;
        }

        return created;
    }

    private async Task<ApplicationUser> CreateAccountAsync(DemoUser demo, string password)
    {
        var account = new ApplicationUser { UserName = demo.Email, Email = demo.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(account, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seeding {demo.Email} failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        return account;
    }

    private static User CreateDomainUser(DemoUser demo, string subjectId, DateTimeOffset now)
    {
        var user = new User(subjectId, demo.DisplayName, demo.Email, now);
        switch (demo.Role)
        {
            case DemoRole.Administrator:
                user.GrantAdministrator();
                break;
            case DemoRole.Instructor:
                user.AssignClassification(ReviewerClassification.InstructorId, now);
                break;
            case DemoRole.Supervisor:
                user.AssignClassification(ReviewerClassification.SupervisorId, now);
                break;
            case DemoRole.Candidate:
                break;
        }

        return user;
    }
}
