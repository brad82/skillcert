using SkillCert.Api.Admin;

namespace SkillCert.Api.Features.AdminUsers;

public static class AdminUsersFeature
{
    /// <summary>Routes under /api/admin/users, OpenAPI tag "AdminUsers" (web feature admin-users). Administrators only.</summary>
    public static IEndpointRouteBuilder MapAdminUsersFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapAdminGroup("users", "AdminUsers");
        ListUsersEndpoint.Map(group);
        GetUserEndpoint.Map(group);
        SetUserActiveEndpoint.Map(group);
        SetUserClassificationEndpoint.Map(group);
        return app;
    }
}
