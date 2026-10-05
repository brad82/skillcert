using Microsoft.AspNetCore.Identity;

namespace SkillCert.Infrastructure.Identity;

/// <summary>
/// Login account only. Domain identity lives on the domain User, linked by external subject id,
/// so the Phase 7 switch to OIDC replaces this without touching domain data.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>;
