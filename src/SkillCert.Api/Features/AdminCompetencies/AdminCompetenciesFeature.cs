using SkillCert.Api.Admin;

namespace SkillCert.Api.Features.AdminCompetencies;

public static class AdminCompetenciesFeature
{
    /// <summary>Routes under /api/admin/competencies, tag "AdminCompetencies" (web feature admin-competencies).</summary>
    public static IEndpointRouteBuilder MapAdminCompetenciesFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapAdminGroup("competencies", "AdminCompetencies");
        ListCompetenciesEndpoint.Map(group);
        GetCompetencyEndpoint.Map(group);
        CreateCompetencyEndpoint.Map(group);
        EditCurrentRevisionEndpoint.Map(group);
        PublishRevisionEndpoint.Map(group);
        SetCompetencyActiveEndpoint.Map(group);
        return app;
    }
}
