using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminUsers;

public sealed record SetUserClassificationRequest(bool Holds);

public sealed class SetUserClassificationRequestValidator : AbstractValidator<SetUserClassificationRequest>;

/// <summary>
/// PUT /api/admin/users/{userId}/classifications/{code}: assign or remove a reviewer classification (spec §7).
/// Removing one stops new reviews under it but keeps the right to decide claims already naming the user (spec §11).
/// </summary>
public static class SetUserClassificationEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{userId:guid}/classifications/{code}", HandleAsync)
            .WithName("SetUserClassification")
            .WithValidation<SetUserClassificationRequest>()
            .Produces(StatusCodes.Status404NotFound);

    internal static async Task<Results<Ok<AdminUserDto>, NotFound>> HandleAsync(
        Guid userId,
        string code,
        SetUserClassificationRequest request,
        CurrentUserAccessor currentUser,
        SkillCertDbContext db,
        AuditLog audit,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        var user = await db.DomainUsers.Include(u => u.Classifications).SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var classification = await db.ReviewerClassifications.SingleOrDefaultAsync(c => c.Code == code, cancellationToken);
        if (user is null || classification is null)
        {
            return TypedResults.NotFound();
        }

        var holds = user.Classifications.Any(c => c.ReviewerClassificationId == classification.Id);
        if (holds != request.Holds)
        {
            if (request.Holds)
            {
                user.AssignClassification(classification.Id, time.GetUtcNowForStorage());
            }
            else
            {
                user.RemoveClassification(classification.Id);
            }

            audit.Record(
                adminId,
                request.Holds ? "user.assign-classification" : "user.remove-classification",
                "User",
                user.Id,
                new { Classification = classification.Code, Holds = holds },
                new { Classification = classification.Code, request.Holds });
            await db.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok((await AdminUserQuery.SingleAsync(db, userId, cancellationToken))!);
    }
}
