namespace SkillCert.Api.Features.Auth;

public static class AuthFeature
{
    /// <summary>Routes under /api/auth, OpenAPI tag "Auth" (the web app's auth feature owns this tag).</summary>
    public static IEndpointRouteBuilder MapAuthFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");
        LoginEndpoint.Map(group);
        LogoutEndpoint.Map(group);
        return app;
    }
}
