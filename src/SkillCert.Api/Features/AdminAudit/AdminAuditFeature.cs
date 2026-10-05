using SkillCert.Api.Admin;

namespace SkillCert.Api.Features.AdminAudit;

public static class AdminAuditFeature
{
    /// <summary>Routes under /api/admin/audit, tag "AdminAudit" (web feature admin-audit).</summary>
    public static IEndpointRouteBuilder MapAdminAuditFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapAdminGroup("audit", "AdminAudit");
        ListAuditEntriesEndpoint.Map(group);
        return app;
    }
}
