using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Identity;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(r => r.Password).NotEmpty().MaximumLength(256);
    }
}

/// <summary>
/// POST /api/auth/login — password sign-in that issues the auth cookie. Deactivated users are refused
/// (spec §5). Every failure is a bare 401 so the response never reveals which accounts exist.
/// </summary>
public static class LoginEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/login", HandleAsync)
            .WithName("Login")
            .WithValidation<LoginRequest>()
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

    internal static async Task<Results<NoContent, UnauthorizedHttpResult>> HandleAsync(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        SkillCertDbContext db,
        CancellationToken cancellationToken)
    {
        var account = await signInManager.UserManager.FindByEmailAsync(request.Email.Trim());
        if (account is null)
        {
            return TypedResults.Unauthorized();
        }

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
}
