using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Features.Approvals;
using SkillCert.Api.Features.SignOffs;
using SkillCert.Api.Tests.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;

namespace SkillCert.Api.Tests.Features.Approvals;

/// <summary>
/// Each test has instructor2 (in no group) sign two skills off with supervisor2, who has no seeded pending
/// claims, then cleans up its reviews.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ApprovalTests(ApiFactory api)
{
    private const string Candidate = "instructor2@skillcert.test";
    private const string Supervisor = "supervisor2@skillcert.test";
    private static readonly System.Text.Json.JsonSerializerOptions Json = GetMyListsTests.Json;

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = api.CreateClient();
        await client.LoginAsync(email);
        return client;
    }

    private async Task<SignOffResponse> SupervisorSignOffAsync()
    {
        Guid supervisorId = default, cpr = default, cpr2 = default;
        await api.WithDbAsync(async db =>
        {
            supervisorId = await db.DomainUsers.Where(u => u.Email == Supervisor).Select(u => u.Id).SingleAsync();
            cpr = await db.Competencies.Where(c => c.Code == "4.3.1").Select(c => c.Id).SingleAsync();
            cpr2 = await db.Competencies.Where(c => c.Code == "4.3.2").Select(c => c.Id).SingleAsync();
        });
        var candidate = await SignInAsync(Candidate);
        var response = await candidate.PostAsJsonAsync("/api/signoffs", new SignOffRequest(
            supervisorId, [new(cpr, ReviewOutcome.Competent), new(cpr2, ReviewOutcome.NotCompetent)], "Check depth.",
            new SignatureRequest(300, 100, [[[10, 50], [90, 20]]])), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignOffResponse>(Json))!;
    }

    private Task CleanUpAsync() => api.WithDbAsync(async db =>
    {
        var candidateId = await db.DomainUsers.Where(u => u.Email == Candidate).Select(u => u.Id).SingleAsync();
        await db.CompetencyReviews.Where(r => r.CandidateUserId == candidateId).ExecuteDeleteAsync();
    });

    private async Task<Guid> SignatureOfAsync(SignOffResponse signOff)
    {
        Guid signatureId = default;
        var reviewId = signOff.Reviews[0].ReviewId;
        await api.WithDbAsync(async db => signatureId = (await db.CompetencyReviews.SingleAsync(r => r.Id == reviewId)).ReviewSignatureId!.Value);
        return signatureId;
    }

    private async Task<IReadOnlyList<ConfirmationStatus>> StatusesAsync(SignOffResponse signOff)
    {
        var ids = signOff.Reviews.Select(r => r.ReviewId).ToList();
        IReadOnlyList<ConfirmationStatus> statuses = [];
        await api.WithDbAsync(async db => statuses = await db.CompetencyReviews.Where(r => ids.Contains(r.Id)).Select(r => r.ConfirmationStatus).ToListAsync());
        return statuses;
    }

    [Fact]
    public async Task Anonymous_requests_return_401()
    {
        var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/approvals")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/approvals/{Guid.NewGuid()}/confirm", null)).StatusCode);
    }

    [Fact]
    public async Task The_named_supervisor_sees_the_sitting_as_one_group_and_confirms_it_once()
    {
        try
        {
            var signOff = await SupervisorSignOffAsync();
            var supervisor = await SignInAsync(Supervisor);

            var queue = (await supervisor.GetFromJsonAsync<ApprovalsResponse>("/api/approvals", Json))!;
            var group = Assert.Single(queue.Groups);
            Assert.Equal(("Ivan Instructor", "Check depth.", signOff.ReviewedAt), (group.CandidateName, group.Comment, group.ReviewedAt));
            Assert.Equal(["4.3.1", "4.3.2"], group.Items.Select(i => i.Code).Order());
            Assert.Equal(HttpStatusCode.OK, (await supervisor.GetAsync($"/api/approvals/{group.SignatureId}/signature")).StatusCode);

            Assert.Equal(HttpStatusCode.NoContent, (await supervisor.PostAsync($"/api/approvals/{group.SignatureId}/confirm", null)).StatusCode);
            Assert.All(await StatusesAsync(signOff), s => Assert.Equal(ConfirmationStatus.Confirmed, s));
            Assert.Empty((await supervisor.GetFromJsonAsync<ApprovalsResponse>("/api/approvals", Json))!.Groups);

            var again = await supervisor.PostAsJsonAsync($"/api/approvals/{group.SignatureId}/reject", new RejectApprovalRequest("Changed my mind"));
            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        }
        finally
        {
            await CleanUpAsync();
        }
    }

    [Fact]
    public async Task Rejecting_needs_a_reason_and_is_final()
    {
        try
        {
            var signOff = await SupervisorSignOffAsync();
            var signatureId = await SignatureOfAsync(signOff);
            var supervisor = await SignInAsync(Supervisor);

            Assert.Equal(HttpStatusCode.BadRequest, (await supervisor.PostAsJsonAsync($"/api/approvals/{signatureId}/reject", new RejectApprovalRequest(" "))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await supervisor.PostAsJsonAsync($"/api/approvals/{signatureId}/reject", new RejectApprovalRequest("Not observed."))).StatusCode);

            Assert.All(await StatusesAsync(signOff), s => Assert.Equal(ConfirmationStatus.Rejected, s));
            Assert.Equal(HttpStatusCode.Conflict, (await supervisor.PostAsync($"/api/approvals/{signatureId}/confirm", null)).StatusCode);
        }
        finally
        {
            await CleanUpAsync();
        }
    }

    [Fact]
    public async Task Other_supervisors_and_administrators_cannot_decide_and_never_see_the_signature()
    {
        try
        {
            var signOff = await SupervisorSignOffAsync();
            var signatureId = await SignatureOfAsync(signOff);

            foreach (var email in new[] { "supervisor1@skillcert.test", "admin@skillcert.test", Candidate })
            {
                var other = await SignInAsync(email);
                Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsync($"/api/approvals/{signatureId}/confirm", null)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/approvals/{signatureId}/signature")).StatusCode);
                Assert.DoesNotContain((await other.GetFromJsonAsync<ApprovalsResponse>("/api/approvals", Json))!.Groups, g => g.SignatureId == signatureId);
            }

            Assert.All(await StatusesAsync(signOff), s => Assert.Equal(ConfirmationStatus.Pending, s));
            var unknown = await (await SignInAsync(Supervisor)).PostAsync($"/api/approvals/{Guid.NewGuid()}/confirm", null);
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        }
        finally
        {
            await CleanUpAsync();
        }
    }

    [Fact]
    public async Task Losing_the_classification_keeps_the_right_to_decide_existing_claims()
    {
        var signOff = await SupervisorSignOffAsync();
        await api.WithDbAsync(async db =>
        {
            var user = await db.DomainUsers.Include(u => u.Classifications).SingleAsync(u => u.Email == Supervisor);
            user.RemoveClassification(ReviewerClassification.SupervisorId);
            await db.SaveChangesAsync();
        });
        try
        {
            var signatureId = await SignatureOfAsync(signOff);
            var former = await SignInAsync(Supervisor);

            Assert.Single((await former.GetFromJsonAsync<ApprovalsResponse>("/api/approvals", Json))!.Groups);
            Assert.Equal(HttpStatusCode.NoContent, (await former.PostAsync($"/api/approvals/{signatureId}/confirm", null)).StatusCode);
        }
        finally
        {
            await api.WithDbAsync(async db =>
            {
                var user = await db.DomainUsers.Include(u => u.Classifications).SingleAsync(u => u.Email == Supervisor);
                user.AssignClassification(ReviewerClassification.SupervisorId, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync();
            });
            await CleanUpAsync();
        }
    }
}
