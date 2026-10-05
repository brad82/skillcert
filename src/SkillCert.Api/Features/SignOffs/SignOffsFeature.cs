namespace SkillCert.Api.Features.SignOffs;

public static class SignOffsFeature
{
    /// <summary>Routes under /api/signoffs, OpenAPI tag "SignOffs" (the web app's sign-off feature owns this tag).</summary>
    public static IEndpointRouteBuilder MapSignOffsFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/signoffs").WithTags("SignOffs");
        GetSignOffReviewersEndpoint.Map(group);
        SignOffEndpoint.Map(group);
        return app;
    }
}
