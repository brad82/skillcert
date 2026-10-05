using System.Text.Json;
using FluentValidation;

namespace SkillCert.Api.Common;

/// <summary>
/// Runs the FluentValidation validator for <typeparamref name="TRequest"/> before the handler and answers
/// 400 with a ValidationProblem (errors keyed by camelCase property name) when it fails. Attach with
/// <see cref="ValidationExtensions.WithValidation{TRequest}"/>; handlers can then assume a valid request.
/// </summary>
public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest, title: "Request body is required.");
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return await next(context);
        }

        var errors = result.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        return TypedResults.ValidationProblem(errors);
    }
}

public static class ValidationExtensions
{
    /// <summary>Validates the endpoint's <typeparamref name="TRequest"/> body and documents the 400 in OpenAPI.</summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>().ProducesValidationProblem();
}
