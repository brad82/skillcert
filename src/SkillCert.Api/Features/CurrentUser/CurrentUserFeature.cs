namespace SkillCert.Api.Features.CurrentUser;

public static class CurrentUserFeature
{
    /// <summary>Routes under /api, OpenAPI tag "CurrentUser" (the web app's current-user feature owns this tag).</summary>
    public static IEndpointRouteBuilder MapCurrentUserFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("CurrentUser");
        GetCurrentUserEndpoint.Map(group);
        return app;
    }
}
