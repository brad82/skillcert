using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Features.SignOffs;
using SkillCert.Api.Tests.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Tests.Features.SignOffs;

/// <summary>
/// The candidate here is instructor2 (in no group), so seeded candidate profiles stay untouched; each test
/// deletes the reviews it creates.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SignOffTests(ApiFactory api)
{
    private const string Candidate = "instructor2@skillcert.test";
    private static readonly System.Text.Json.JsonSerializerOptions Json = GetMyListsTests.Json;
    private static readonly SignatureRequest Scribble = new(300, 100, [[[10, 50], [80, 20], [150, 70]]]);

    private async Task<HttpClient> SignInAsync(string email = Candidate)
    {
        var client = api.CreateClient();
        await client.LoginAsync(email);
        return client;
    }

    private async Task<Guid> CompetencyIdAsync(string code)
    {
        Guid id = default;
        await api.WithDbAsync(async db => id = await db.Competencies.Where(c => c.Code == code).Select(c => c.Id).SingleAsync());
        return id;
    }

    private async Task<Guid> UserIdAsync(string email)
    {
        Guid id = default;
        await api.WithDbAsync(async db => id = await db.DomainUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync());
        return id;
    }

    private Task DeleteCandidateReviewsAsync() => api.WithDbAsync(async db =>
    {
        var candidateId = await db.DomainUsers.Where(u => u.Email == Candidate).Select(u => u.Id).SingleAsync();
        await db.CompetencyReviews.Where(r => r.CandidateUserId == candidateId).ExecuteDeleteAsync();
    });

    private static async Task<string?> ProblemTypeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Type;

    [Fact]
    public async Task Anonymous_requests_return_401()
    {
        var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/signoffs/reviewers?competencyId={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/signoffs", new { })).StatusCode);
    }

    [Fact]
    public async Task Reviewers_are_registered_users_who_can_sign_every_skill_and_how()
    {
        var client = await SignInAsync("candidate04@skillcert.test");
        var cpr = await CompetencyIdAsync("4.3.1");

        var response = await client.GetFromJsonAsync<SignOffReviewersResponse>($"/api/signoffs/reviewers?competencyId={cpr}", Json);

        var reviewers = response!.Reviewers;
        Assert.Equal(["Ines Instructor", "Ivan Instructor", "Sam Supervisor", "Sofia Supervisor"], reviewers.Select(r => r.DisplayName).Order());
        var ines = reviewers.Single(r => r.DisplayName == "Ines Instructor");
        Assert.Equal((ReviewMethod.Classified, "Instructor", true, false), (ines.Method, ines.ClassificationCode, ines.SignatureRequired, ines.NeedsConfirmation));
        Assert.True(reviewers.Single(r => r.DisplayName == "Sam Supervisor").NeedsConfirmation);
    }

    [Fact]
    public async Task A_supervisor_sign_off_records_pending_reviews_sharing_one_time_and_one_rendered_signature()
    {
        var client = await SignInAsync();
        var (cpr, cpr2) = (await CompetencyIdAsync("4.3.1"), await CompetencyIdAsync("4.3.2"));
        var supervisor = await UserIdAsync("supervisor1@skillcert.test");
        try
        {
            var response = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(
                supervisor, [new(cpr, ReviewOutcome.Competent), new(cpr2, ReviewOutcome.NotCompetent)], "Good compressions.", Scribble), Json);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<SignOffResponse>(Json))!;
            Assert.Equal(("Sam Supervisor", "Supervisor"), (result.ReviewerName, result.ClassificationCode));
            Assert.All(result.Reviews, r => Assert.Equal(ConfirmationStatus.Pending, r.ConfirmationStatus));

            await api.WithDbAsync(async db =>
            {
                var stored = await db.CompetencyReviews.Where(r => result.Reviews.Select(x => x.ReviewId).Contains(r.Id)).ToListAsync();
                Assert.Single(stored.Select(r => (r.ReviewedAt, r.ReviewSignatureId)).Distinct());
                Assert.All(stored, r => Assert.Equal("Good compressions.", r.Comment));
                var svg = await db.ReviewSignatures.Where(s => s.Id == stored[0].ReviewSignatureId).Select(s => s.Data).SingleAsync();
                Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 300 100\">", svg, StringComparison.Ordinal);
            });

            var again = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(supervisor, [new(cpr, ReviewOutcome.Competent)], null, Scribble), Json);
            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
            Assert.Equal(SignOffEndpoint.PendingType, await ProblemTypeAsync(again));
        }
        finally
        {
            await DeleteCandidateReviewsAsync();
        }
    }

    [Fact]
    public async Task An_instructor_sign_off_counts_immediately()
    {
        var client = await SignInAsync();
        var instructor = await UserIdAsync("instructor1@skillcert.test");
        try
        {
            var response = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(
                instructor, [new(await CompetencyIdAsync("4.3.1"), ReviewOutcome.Competent)], null, Scribble), Json);

            var result = (await response.Content.ReadFromJsonAsync<SignOffResponse>(Json))!;
            Assert.Equal(ConfirmationStatus.NotRequired, Assert.Single(result.Reviews).ConfirmationStatus);
        }
        finally
        {
            await DeleteCandidateReviewsAsync();
        }
    }

    [Fact]
    public async Task Refusals_are_problems_the_web_app_can_branch_on()
    {
        var client = await SignInAsync();
        var cpr = await CompetencyIdAsync("4.3.1");
        var instructor = await UserIdAsync("instructor1@skillcert.test");
        var peer = await UserIdAsync("candidate01@skillcert.test");
        try
        {
            var unsigned = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(instructor, [new(cpr, ReviewOutcome.Competent)], null, null), Json);
            var notPermitted = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(peer, [new(cpr, ReviewOutcome.Competent)], null, Scribble), Json);
            var unknownReviewer = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(Guid.NewGuid(), [new(cpr, ReviewOutcome.Competent)], null, Scribble), Json);
            var offPad = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(
                instructor, [new(cpr, ReviewOutcome.Competent)], null, new SignatureRequest(300, 100, [[[500, 50]]])), Json);
            var duplicate = await client.PostAsJsonAsync("/api/signoffs", new SignOffRequest(
                instructor, [new(cpr, ReviewOutcome.Competent), new(cpr, ReviewOutcome.Competent)], null, Scribble), Json);

            Assert.Equal((HttpStatusCode.UnprocessableEntity, SignOff.SignatureRequired), (unsigned.StatusCode, await ProblemTypeAsync(unsigned)));
            Assert.Equal((HttpStatusCode.UnprocessableEntity, CompetencyReview.MethodNotPermitted), (notPermitted.StatusCode, await ProblemTypeAsync(notPermitted)));
            Assert.Equal(SignOffEndpoint.ReviewerNotFoundType, await ProblemTypeAsync(unknownReviewer));
            Assert.Equal(SignatureDrawing.Invalid, await ProblemTypeAsync(offPad));
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
            await api.WithDbAsync(async db => Assert.False(await db.CompetencyReviews.AnyAsync(r => r.CandidateUserId == db.DomainUsers.Single(u => u.Email == Candidate).Id)));
        }
        finally
        {
            await DeleteCandidateReviewsAsync();
        }
    }
}
