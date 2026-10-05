using Microsoft.AspNetCore.Identity;
using SkillCert.Infrastructure.Identity;

namespace SkillCert.Infrastructure.Seed;

/// <summary>Creates the demo login accounts if missing. Safe to run on every migrator start.</summary>
public sealed class DemoUserSeeder(UserManager<ApplicationUser> userManager)
{
    public async Task<int> SeedAsync(string password)
    {
        var created = 0;
        foreach (var demo in DemoUsers.All)
        {
            if (await userManager.FindByEmailAsync(demo.Email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser { UserName = demo.Email, Email = demo.Email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Seeding {demo.Email} failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }

            created++;
        }

        return created;
    }
}
