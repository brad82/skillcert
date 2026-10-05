using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillCert.Infrastructure.Identity;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.Auth;

public static class AuthEndpoints
{
    public sealed record LoginRequest(string Email, string Password);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync).WithName("Login").AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).WithName("Logout").RequireAuthorization();

        return app;
    }

    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        SkillCertDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Email and password are required."],
            });
        }

        var account = await signInManager.UserManager.FindByEmailAsync(request.Email.Trim());
        if (account is null)
        {
            return TypedResults.Unauthorized();
        }

        // Deactivated users keep their history but can no longer sign in (spec §5).
        var subjectId = account.Id.ToString();
        var isActive = await db.DomainUsers.AnyAsync(
            u => u.ExternalSubjectId == subjectId && u.IsActive, cancellationToken);
        if (!isActive)
        {
            return TypedResults.Unauthorized();
        }

        var result = await signInManager.PasswordSignInAsync(
            account, request.Password, isPersistent: true, lockoutOnFailure: true);

        return result.Succeeded ? TypedResults.NoContent() : TypedResults.Unauthorized();
    }

    private static async Task<NoContent> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }
}
