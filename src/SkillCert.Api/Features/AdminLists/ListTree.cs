using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminLists;

/// <param name="ParentNodeId">A heading of this list, or null for the top level.</param>
/// <param name="Index">Position among the new siblings; null appends.</param>
public sealed record AddHeadingRequest(Guid? ParentNodeId, string? Code, string Title, int? Index);

/// <param name="CompetencyIds">Library competencies to add, in this order.</param>
public sealed record AddCompetenciesRequest(Guid? ParentNodeId, IReadOnlyList<Guid> CompetencyIds, int? Index);

public sealed record RenameHeadingRequest(string? Code, string Title);

public sealed record MoveNodeRequest(Guid? ParentNodeId, int Index);

public sealed class AddHeadingRequestValidator : AbstractValidator<AddHeadingRequest>
{
    public AddHeadingRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(300);
        RuleFor(r => r.Code).MaximumLength(50);
        RuleFor(r => r.Index).GreaterThanOrEqualTo(0).When(r => r.Index is not null);
    }
}

public sealed class AddCompetenciesRequestValidator : AbstractValidator<AddCompetenciesRequest>
{
    public AddCompetenciesRequestValidator()
    {
        RuleFor(r => r.CompetencyIds).NotEmpty().Must(ids => ids.Count <= 200 && ids.Distinct().Count() == ids.Count)
            .WithMessage("Give up to 200 different competencies.");
        RuleFor(r => r.Index).GreaterThanOrEqualTo(0).When(r => r.Index is not null);
    }
}

public sealed class RenameHeadingRequestValidator : AbstractValidator<RenameHeadingRequest>
{
    public RenameHeadingRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(300);
        RuleFor(r => r.Code).MaximumLength(50);
    }
}

public sealed class MoveNodeRequestValidator : AbstractValidator<MoveNodeRequest>
{
    public MoveNodeRequestValidator()
    {
        RuleFor(r => r.Index).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// The tree editor's operations (spec §4, §19). Each returns the whole updated list. Refusals: 409
/// <c>list.duplicate-competency</c>, <c>list.cycle</c> and <c>list.invalid-parent</c>; 404 for an unknown node.
/// Changes are live (no list revisions); review history is never touched.
/// </summary>
public static class ListTreeEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{listId:guid}/headings", AddHeadingAsync)
            .WithName("AddHeading").WithValidation<AddHeadingRequest>().Documented();
        group.MapPost("/{listId:guid}/competencies", AddCompetenciesAsync)
            .WithName("AddCompetencies").WithValidation<AddCompetenciesRequest>().Documented();
        group.MapPut("/{listId:guid}/nodes/{nodeId:guid}", RenameHeadingAsync)
            .WithName("RenameHeading").WithValidation<RenameHeadingRequest>().Documented();
        group.MapPut("/{listId:guid}/nodes/{nodeId:guid}/position", MoveNodeAsync)
            .WithName("MoveNode").WithValidation<MoveNodeRequest>().Documented();
        group.MapDelete("/{listId:guid}/nodes/{nodeId:guid}", RemoveNodeAsync)
            .WithName("RemoveNode").Documented();
    }

    private static RouteHandlerBuilder Documented(this RouteHandlerBuilder builder) =>
        builder.Produces(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);

    internal static Task<Results<Ok<AdminListDto>, NotFound, ProblemHttpResult>> AddHeadingAsync(
        Guid listId, AddHeadingRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken) =>
        ListEditing.ApplyAsync(listId, "list.add-heading", list =>
        {
            var node = list.AddHeading(request.ParentNodeId, request.Code, request.Title, request.Index);
            return new { NodeId = node.Id, node.HeadingCode, node.HeadingTitle, node.ParentNodeId };
        }, currentUser, db, audit, cancellationToken);

    internal static async Task<Results<Ok<AdminListDto>, NotFound, ProblemHttpResult>> AddCompetenciesAsync(
        Guid listId, AddCompetenciesRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken)
    {
        var codes = await db.Competencies.Where(c => request.CompetencyIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Code, cancellationToken);
        if (codes.Count != request.CompetencyIds.Count)
        {
            return TypedResults.NotFound();
        }

        return await ListEditing.ApplyAsync(listId, "list.add-competencies", list =>
        {
            for (var i = 0; i < request.CompetencyIds.Count; i++)
            {
                list.AddCompetency(request.ParentNodeId, request.CompetencyIds[i], request.Index is { } index ? index + i : null);
            }

            return new { request.ParentNodeId, Codes = request.CompetencyIds.Select(id => codes[id]) };
        }, currentUser, db, audit, cancellationToken);
    }

    internal static Task<Results<Ok<AdminListDto>, NotFound, ProblemHttpResult>> RenameHeadingAsync(
        Guid listId, Guid nodeId, RenameHeadingRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken) =>
        ListEditing.ApplyAsync(listId, "list.rename-heading", list =>
        {
            var node = list.Nodes.SingleOrDefault(n => n.Id == nodeId) ?? throw new KeyNotFoundException();
            var before = new { node.HeadingCode, node.HeadingTitle };
            list.RenameHeading(nodeId, request.Code, request.Title);
            return new { NodeId = nodeId, Before = before, After = new { node.HeadingCode, node.HeadingTitle } };
        }, currentUser, db, audit, cancellationToken);

    internal static Task<Results<Ok<AdminListDto>, NotFound, ProblemHttpResult>> MoveNodeAsync(
        Guid listId, Guid nodeId, MoveNodeRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken) =>
        ListEditing.ApplyAsync(listId, "list.move-node", list =>
        {
            var node = list.Nodes.SingleOrDefault(n => n.Id == nodeId) ?? throw new KeyNotFoundException();
            var before = new { node.ParentNodeId, Index = node.SortOrder };
            list.Move(nodeId, request.ParentNodeId, request.Index);
            return new { NodeId = nodeId, Before = before, After = new { node.ParentNodeId, Index = node.SortOrder } };
        }, currentUser, db, audit, cancellationToken);

    internal static Task<Results<Ok<AdminListDto>, NotFound, ProblemHttpResult>> RemoveNodeAsync(
        Guid listId, Guid nodeId, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, CancellationToken cancellationToken) =>
        ListEditing.ApplyAsync(listId, "list.remove-node", list =>
        {
            var node = list.Nodes.SingleOrDefault(n => n.Id == nodeId) ?? throw new KeyNotFoundException();
            var removed = new { NodeId = nodeId, node.Kind, node.HeadingCode, node.HeadingTitle, node.CompetencyId, Descendants = list.Walk(nodeId).Count() };
            list.Remove(nodeId);
            return removed;
        }, currentUser, db, audit, cancellationToken);
}
