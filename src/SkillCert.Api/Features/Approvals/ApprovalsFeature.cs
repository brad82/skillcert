namespace SkillCert.Api.Features.Approvals;

public static class ApprovalsFeature
{
    /// <summary>Routes under /api/approvals, OpenAPI tag "Approvals" (the web app's approvals feature owns this tag).</summary>
    public static IEndpointRouteBuilder MapApprovalsFeature(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/approvals").WithTags("Approvals");
        GetApprovalsEndpoint.Map(group);
        GetApprovalSignatureEndpoint.Map(group);
        ConfirmApprovalEndpoint.Map(group);
        RejectApprovalEndpoint.Map(group);
        return app;
    }
}
