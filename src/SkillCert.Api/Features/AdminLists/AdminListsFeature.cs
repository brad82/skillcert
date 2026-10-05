using SkillCert.Api.Admin;

namespace SkillCert.Api.Features.AdminLists;

public static class AdminListsFeature
{
    /// <summary>Routes under /api/admin/lists, tag "AdminLists" (web feature admin-lists).</summary>
    public static IEndpointRouteBuilder MapAdminListsFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapAdminGroup("lists", "AdminLists");
        ListListsEndpoint.Map(group);
        GetListEndpoint.Map(group);
        CreateListEndpoint.Map(group);
        RenameListEndpoint.Map(group);
        ListTreeEndpoints.Map(group);
        return app;
    }
}
