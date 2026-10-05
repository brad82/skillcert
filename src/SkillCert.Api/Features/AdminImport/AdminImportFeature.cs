using SkillCert.Api.Admin;

namespace SkillCert.Api.Features.AdminImport;

public static class AdminImportFeature
{
    /// <summary>Routes under /api/admin/imports, tag "AdminImport" (web feature admin-import).</summary>
    public static IEndpointRouteBuilder MapAdminImportFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapAdminGroup("imports", "AdminImport");
        PreviewCompetencyImportEndpoint.Map(group);
        ImportCompetenciesEndpoint.Map(group);
        return app;
    }
}
