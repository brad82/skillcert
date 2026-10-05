using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SkillCert.Infrastructure.Identity;

namespace SkillCert.Api.Features.Auth;

public static class AuthEndpoints
{
    public sealed record LoginRequest(string Email, string Password);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();

        return app;
    }

    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request, SignInManager<ApplicationUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Email and password are required."],
            });
        }

        // UserName is the email for every account, so a password sign-in by email works directly.
        var result = await signInManager.PasswordSignInAsync(
            request.Email.Trim(), request.Password, isPersistent: true, lockoutOnFailure: true);

        return result.Succeeded ? TypedResults.NoContent() : TypedResults.Unauthorized();
    }

    private static async Task<NoContent> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }
}
