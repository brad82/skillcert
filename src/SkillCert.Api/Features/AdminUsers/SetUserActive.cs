using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminUsers;

public sealed record SetUserActiveRequest(bool IsActive);

public sealed class SetUserActiveRequestValidator : AbstractValidator<SetUserActiveRequest>;

/// <summary>
/// PUT /api/admin/users/{userId}/active: deactivate or reactivate a user. Users are never deleted, because reviews
/// reference them (spec §5). A deactivated user can't sign in or be chosen as a reviewer. Administrators can't
/// deactivate themselves (409), so the system always keeps one way back in.
/// </summary>
public static class SetUserActiveEndpoint
{
    public const string SelfDeactivationType = "user.self-deactivation";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{userId:guid}/active", HandleAsync)
            .WithName("SetUserActive")
            .WithValidation<SetUserActiveRequest>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    internal static async Task<Results<Ok<AdminUserDto>, NotFound, ProblemHttpResult>> HandleAsync(
        Guid userId, SetUserActiveRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        var user = await db.DomainUsers.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (!request.IsActive && user.Id == adminId)
        {
            return RuleProblems.Create(SelfDeactivationType, "You can't deactivate your own account.", StatusCodes.Status409Conflict);
        }

        if (user.IsActive != request.IsActive)
        {
            var before = new { user.IsActive };
            if (request.IsActive)
            {
                user.Reactivate();
            }
            else
            {
                user.Deactivate();
            }

            audit.Record(adminId, request.IsActive ? "user.reactivate" : "user.deactivate", "User", user.Id, before, new { user.IsActive });
            await db.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok((await AdminUserQuery.SingleAsync(db, userId, cancellationToken))!);
    }
}
