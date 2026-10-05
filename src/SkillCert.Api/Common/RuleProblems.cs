using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Domain;

namespace SkillCert.Api.Common;

/// <summary>Business-rule refusals as problem responses with a stable <c>type</c> the web app branches on.</summary>
public static class RuleProblems
{
    public static ProblemHttpResult From(DomainRuleException exception, int statusCode = StatusCodes.Status422UnprocessableEntity) =>
        Create(exception.Code, exception.Message, statusCode);

    public static ProblemHttpResult Create(string type, string title, int statusCode = StatusCodes.Status422UnprocessableEntity) =>
        TypedResults.Problem(type: type, title: title, statusCode: statusCode);
}
