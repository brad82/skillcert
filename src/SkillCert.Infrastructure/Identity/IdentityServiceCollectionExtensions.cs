using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    /// <summary>Identity stores and password rules shared by the API and the migrator's seeder.</summary>
    public static IdentityBuilder AddSkillCertIdentityCore(this IServiceCollection services) =>
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<SkillCertDbContext>();
}
