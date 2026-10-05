using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Domain.Lists;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminLists;

public sealed record ListDetailsRequest(string Title, string? Description);

public sealed class ListDetailsRequestValidator : AbstractValidator<ListDetailsRequest>
{
    public ListDetailsRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(2000);
    }
}

/// <summary>POST /api/admin/lists: a new, empty list. Assign it to groups in Phase 6's groups admin.</summary>
public static class CreateListEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("", HandleAsync).WithName("CreateList").WithValidation<ListDetailsRequest>();

    internal static async Task<Created<AdminListDto>> HandleAsync(
        ListDetailsRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, TimeProvider time, CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        var list = new CompetencyList(request.Title, request.Description, time.GetUtcNowForStorage());
        db.CompetencyLists.Add(list);
        audit.Record(adminId, "list.create", "CompetencyList", list.Id, null, new { list.Title, list.Description });
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/api/admin/lists/{list.Id}", (await AdminListQuery.LoadAsync(db, list.Id, cancellationToken))!);
    }
}

/// <summary>PUT /api/admin/lists/{listId}: rename or redescribe. Archived PDFs keep the title they were rendered with.</summary>
public static class RenameListEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{listId:guid}", HandleAsync).WithName("RenameList").WithValidation<ListDetailsRequest>().Produces(StatusCodes.Status404NotFound);

    internal static Task<Results<Ok<AdminListDto>, NotFound, ProblemHttpResult>> HandleAsync(
        Guid listId, ListDetailsRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken) =>
        ListEditing.ApplyAsync(listId, "list.rename", list =>
        {
            var before = new { list.Title, list.Description };
            list.Rename(request.Title, request.Description);
            return new { Before = before, After = new { list.Title, list.Description } };
        }, currentUser, db, audit, cancellationToken);
}
