using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SkillCert.Infrastructure.Identity;

namespace SkillCert.Api.Features.Auth;

/// <summary>POST /api/auth/logout — clears the auth cookie.</summary>
public static class LogoutEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/logout", HandleAsync)
            .WithName("Logout")
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

    internal static async Task<NoContent> HandleAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }
}
