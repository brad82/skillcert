using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Domain;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Features.SignOffs;

/// <param name="ReviewerUserId">A user from GET /api/signoffs/reviewers; the server re-derives how they sign.</param>
/// <param name="Signature">Required when the reviewer is classified (Instructor, Supervisor).</param>
public sealed record SignOffRequest(
    Guid ReviewerUserId,
    IReadOnlyList<SignOffItemRequest> Items,
    string? Comment,
    SignatureRequest? Signature);

public sealed record SignOffItemRequest(Guid CompetencyId, ReviewOutcome Outcome);

/// <param name="Strokes">Pen strokes, each a list of [x, y] points in pad pixels.</param>
public sealed record SignatureRequest(int Width, int Height, IReadOnlyList<IReadOnlyList<double[]>> Strokes);

public sealed record SignOffResponse(
    DateTimeOffset ReviewedAt,
    string ReviewerName,
    ReviewMethod Method,
    string? ClassificationCode,
    IReadOnlyList<SignOffResultDto> Reviews);

public sealed record SignOffResultDto(Guid ReviewId, Guid CompetencyId, string Code, string ShortTitle, ReviewOutcome Outcome, ConfirmationStatus ConfirmationStatus);

public sealed class SignOffRequestValidator : AbstractValidator<SignOffRequest>
{
    public SignOffRequestValidator()
    {
        RuleFor(r => r.ReviewerUserId).NotEmpty();
        RuleFor(r => r.Items).NotEmpty().Must(items => items.Count <= GetSignOffReviewersEndpoint.MaxCompetencies)
            .WithMessage($"At most {GetSignOffReviewersEndpoint.MaxCompetencies} skills per sign-off.");
        RuleFor(r => r.Items).Must(items => items.Select(i => i.CompetencyId).Distinct().Count() == items.Count)
            .WithMessage("Each skill can appear once.");
        RuleForEach(r => r.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.CompetencyId).NotEmpty();
            item.RuleFor(i => i.Outcome).IsInEnum();
        });
        RuleFor(r => r.Comment).MaximumLength(1000);
        RuleFor(r => r.Signature!).ChildRules(signature =>
        {
            signature.RuleFor(s => s.Width).InclusiveBetween(1, SignatureDrawing.MaxSide);
            signature.RuleFor(s => s.Height).InclusiveBetween(1, SignatureDrawing.MaxSide);
            signature.RuleFor(s => s.Strokes).NotEmpty()
                .Must(strokes => strokes.Count <= SignatureDrawing.MaxStrokes && strokes.Sum(s => s.Count) <= SignatureDrawing.MaxPoints)
                .WithMessage("The signature is too large.")
                .Must(strokes => strokes.All(s => s.Count > 0 && s.All(p => p.Length == 2)))
                .WithMessage("Each stroke is a non-empty list of [x, y] points.");
        }).When(r => r.Signature is not null);
    }
}

/// <summary>
/// POST /api/signoffs: the signed-in candidate's device records a reviewer's verdicts on several skills in one
/// transaction (spec §9–10). The server picks each skill's current revision, re-derives the reviewer's method,
/// stamps one ReviewedAt, renders and stores one signature, and snapshots the reviewer's name.
/// Supervisor sign-offs start Pending until the supervisor confirms them.
/// </summary>
public static class SignOffEndpoint
{
    public const string PendingType = "signoff.pending";
    public const string ReviewerNotFoundType = "signoff.reviewer-not-found";
    public const string UnknownCompetencyType = "signoff.unknown-competency";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("", HandleAsync)
            .WithName("SignOff")
            .WithValidation<SignOffRequest>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

    internal static async Task<Results<Ok<SignOffResponse>, ProblemHttpResult, ForbidHttpResult>> HandleAsync(
        SignOffRequest request,
        CurrentUserAccessor currentUser,
        SkillCertDbContext db,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        if (await currentUser.GetUserIdAsync(cancellationToken) is not { } candidateId)
        {
            return TypedResults.Forbid();
        }

        var competencyIds = request.Items.Select(i => i.CompetencyId).ToList();
        var competencies = await SignOffLoader.CompetenciesAsync(db, competencyIds, cancellationToken);
        if (competencies is null)
        {
            return RuleProblems.Create(UnknownCompetencyType, "One of those skills doesn't exist.");
        }

        var pending = await db.CompetencyReviews.AnyAsync(
            r => r.CandidateUserId == candidateId && competencyIds.Contains(r.CompetencyId) && r.ConfirmationStatus == ConfirmationStatus.Pending,
            cancellationToken);
        if (pending)
        {
            return RuleProblems.Create(PendingType, "A skill in this sign-off is already waiting for confirmation.", StatusCodes.Status409Conflict);
        }

        var reviewerUser = await db.DomainUsers.Include(u => u.Classifications)
            .SingleOrDefaultAsync(u => u.Id == request.ReviewerUserId && u.IsActive, cancellationToken);
        if (reviewerUser is null)
        {
            return RuleProblems.Create(ReviewerNotFoundType, "That reviewer isn't a registered, active user.");
        }

        var classifications = await db.ReviewerClassifications.ToListAsync(cancellationToken);
        var reviewer = ReviewHierarchy.ReviewerFor(reviewerUser, candidateId, competencies.Select(c => c.CurrentRevision).ToList(), classifications);
        if (reviewer is null)
        {
            return RuleProblems.Create(CompetencyReview.MethodNotPermitted, $"{reviewerUser.DisplayName} can't sign off all of these skills.");
        }

        var reviewedAt = time.GetUtcNowForStorage();
        try
        {
            var signature = request.Signature is { } drawn
                ? ReviewSignature.FromDrawing(
                    new SignatureDrawing(drawn.Width, drawn.Height, drawn.Strokes.Select(s => (IReadOnlyList<SignaturePoint>)s.Select(p => new SignaturePoint(p[0], p[1])).ToList()).ToList()),
                    reviewedAt)
                : null;
            var byId = competencies.ToDictionary(c => c.Id);
            var reviews = SignOff.Record(
                candidateId,
                reviewer,
                request.Items.Select(i => new SignOffItem(byId[i.CompetencyId], i.Outcome)).ToList(),
                signature,
                request.Comment,
                reviewedAt,
                createdByUserId: candidateId);

            if (signature is not null)
            {
                db.ReviewSignatures.Add(signature);
            }

            db.CompetencyReviews.AddRange(reviews);
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.Ok(new SignOffResponse(
                reviewedAt,
                reviewer.DisplayName,
                reviewer.Method,
                reviewer.Classification?.Code,
                reviews.Select(r =>
                {
                    var revision = byId[r.CompetencyId].CurrentRevision;
                    return new SignOffResultDto(r.Id, r.CompetencyId, byId[r.CompetencyId].Code, revision.ShortTitle ?? revision.Title, r.Outcome, r.ConfirmationStatus);
                }).ToList()));
        }
        catch (DomainRuleException exception)
        {
            return RuleProblems.From(exception);
        }
    }
}
