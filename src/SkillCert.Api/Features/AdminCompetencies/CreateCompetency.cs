using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Domain.Competencies;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminCompetencies;

/// <param name="Code">As printed on the record, e.g. "4.3.1" or "9.1a". Unique after trimming and upper-casing.</param>
public sealed record CreateCompetencyRequest(string Code, RevisionContentRequest Content);

public sealed class CreateCompetencyRequestValidator : AbstractValidator<CreateCompetencyRequest>
{
    public CreateCompetencyRequestValidator()
    {
        RuleFor(r => r.Code).NotEmpty().MaximumLength(50);
        RuleFor(r => r.Content).NotNull().SetValidator(new RevisionContentRequestValidator());
    }
}

/// <summary>POST /api/admin/competencies: a new competency with revision 1. 409 when the code is taken (spec §26).</summary>
public static class CreateCompetencyEndpoint
{
    public const string DuplicateCodeType = "competency.duplicate-code";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("", HandleAsync)
            .WithName("CreateCompetency")
            .WithValidation<CreateCompetencyRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    internal static async Task<Results<Created<AdminCompetencyDto>, ProblemHttpResult>> HandleAsync(
        CreateCompetencyRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, TimeProvider time, CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        var normalized = Competency.Normalize(request.Code);
        if (await db.Competencies.AnyAsync(c => c.NormalizedCode == normalized, cancellationToken))
        {
            return RuleProblems.Create(DuplicateCodeType, $"A competency with code {request.Code.Trim()} already exists.", StatusCodes.Status409Conflict);
        }

        var classifications = await db.ReviewerClassifications.ToListAsync(cancellationToken);
        if (AdminCompetencyMapping.ToContent(request.Content, classifications) is not { } content)
        {
            return RuleProblems.Create(AdminCompetencyMapping.UnknownClassificationType, "That reviewer classification doesn't exist.");
        }

        var competency = Competency.Create(request.Code, content, time.GetUtcNowForStorage(), adminId);
        db.Competencies.Add(competency);
        audit.Record(adminId, "competency.create", "Competency", competency.Id, null,
            new { competency.Code, Revision = AdminCompetencyMapping.Snapshot(competency.CurrentRevision, classifications) });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return RuleProblems.Create(DuplicateCodeType, $"A competency with code {request.Code.Trim()} already exists.", StatusCodes.Status409Conflict);
        }

        var dto = (await AdminCompetencyMapping.LoadAsync(db, competency.Id, cancellationToken))!;
        return TypedResults.Created($"/api/admin/competencies/{competency.Id}", dto);
    }
}
