namespace SkillCert.Api.Features.MyRecord;

public static class MyRecordFeature
{
    /// <summary>Routes under /api/me, OpenAPI tag "MyRecord" (the web app's my-record feature owns this tag).</summary>
    public static IEndpointRouteBuilder MapMyRecordFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me").WithTags("MyRecord");
        GetMyListsEndpoint.Map(group);
        GetMyCompetencyEndpoint.Map(group);
        GetMySignatureEndpoint.Map(group);
        return app;
    }
}
