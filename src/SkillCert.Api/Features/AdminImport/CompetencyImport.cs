using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Admin;
using SkillCert.Api.Common;
using SkillCert.Domain.Competencies;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.AdminImport;

/// <param name="Csv">The file's text (UTF-8). The browser reads the file; the server never stores it.</param>
public sealed record CompetencyImportRequest(string Csv);

public sealed class CompetencyImportRequestValidator : AbstractValidator<CompetencyImportRequest>
{
    public const int MaxLength = 1_000_000;

    public CompetencyImportRequestValidator()
    {
        RuleFor(r => r.Csv).NotEmpty().MaximumLength(MaxLength).WithMessage("Choose a CSV file under 1 MB.");
    }
}

/// <param name="Line">Line in the file where the row starts (the header is line 1).</param>
public sealed record CompetencyImportRowDto(
    int Line,
    string Code,
    string Title,
    ReviewLevelDto? LowestReviewer,
    int? RecertificationDays,
    int ResourceCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

/// <param name="CanImport">False when any row has an error or the file itself is unusable (spec §26: no partial import).</param>
public sealed record CompetencyImportPreviewResponse(bool CanImport, IReadOnlyList<string> FileErrors, IReadOnlyList<CompetencyImportRowDto> Rows);

public sealed record CompetencyImportResponse(int Created, IReadOnlyList<string> Codes);

internal static class CompetencyImportValidation
{
    public static async Task<CompetencyCsvResult> ValidateAsync(SkillCertDbContext db, string csv, CancellationToken cancellationToken)
    {
        var existing = (await db.Competencies.Select(c => c.NormalizedCode).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        var classifications = await db.ReviewerClassifications.AsNoTracking().ToListAsync(cancellationToken);
        return CompetencyCsv.Parse(csv, existing, classifications);
    }

    public static CompetencyImportPreviewResponse Preview(CompetencyCsvResult result) => new(
        result.CanImport,
        result.FileErrors,
        result.Rows.Select(r => new CompetencyImportRowDto(
            r.Line,
            r.Code,
            r.Title,
            r.LowestReviewer is { } level ? ReviewLevelDto.From(level) : null,
            r.Content?.RecertificationDays,
            r.Content?.Resources.Count ?? 0,
            r.Errors,
            r.Warnings)).ToList());
}

/// <summary>
/// POST /api/admin/imports/competencies/preview: validate the whole file and show what would be created, with
/// row-specific errors and warnings (spec §26). Changes nothing.
/// </summary>
public static class PreviewCompetencyImportEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/competencies/preview", HandleAsync)
            .WithName("PreviewCompetencyImport")
            .WithValidation<CompetencyImportRequest>();

    internal static async Task<Ok<CompetencyImportPreviewResponse>> HandleAsync(
        CompetencyImportRequest request, SkillCertDbContext db, CancellationToken cancellationToken) =>
        TypedResults.Ok(CompetencyImportValidation.Preview(await CompetencyImportValidation.ValidateAsync(db, request.Csv, cancellationToken)));
}

/// <summary>
/// POST /api/admin/imports/competencies: validate again, then create every competency and its revision 1 in one
/// transaction, published immediately (development plan §2.10). Any invalid row or conflicting code → 422 with the
/// preview and nothing created. A code taken by someone else meanwhile → 409, nothing created. Each created
/// competency gets a "competency.import" audit entry.
/// </summary>
public static class ImportCompetenciesEndpoint
{
    public const string ConflictType = "import.conflict";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/competencies", HandleAsync)
            .WithName("ImportCompetencies")
            .WithValidation<CompetencyImportRequest>()
            .Produces<CompetencyImportPreviewResponse>(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict);

    internal static async Task<Results<Ok<CompetencyImportResponse>, UnprocessableEntity<CompetencyImportPreviewResponse>, ProblemHttpResult>> HandleAsync(
        CompetencyImportRequest request, CurrentUserAccessor currentUser, SkillCertDbContext db, AuditLog audit, TimeProvider time, CancellationToken cancellationToken)
    {
        var adminId = await currentUser.AdminIdAsync(cancellationToken);
        var result = await CompetencyImportValidation.ValidateAsync(db, request.Csv, cancellationToken);
        if (!result.CanImport)
        {
            return TypedResults.UnprocessableEntity(CompetencyImportValidation.Preview(result));
        }

        var at = time.GetUtcNowForStorage();
        var created = result.Rows.Select(row => Competency.Create(row.Code, row.Content!, at, adminId)).ToList();
        db.Competencies.AddRange(created);
        foreach (var competency in created)
        {
            var revision = competency.CurrentRevision;
            audit.Record(adminId, "competency.import", "Competency", competency.Id, null,
                new { competency.Code, revision.Title, revision.RecertificationDays });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return RuleProblems.Create(ConflictType, "Another change created one of these codes meanwhile. Nothing was imported; preview again.", StatusCodes.Status409Conflict);
        }

        return TypedResults.Ok(new CompetencyImportResponse(created.Count, created.Select(c => c.Code).ToList()));
    }
}
