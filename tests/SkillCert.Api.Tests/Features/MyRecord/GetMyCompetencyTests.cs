using System.Net;
using System.Net.Http.Json;
using SkillCert.Api.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Reviews;

namespace SkillCert.Api.Tests.Features.MyRecord;

[Collection(ApiCollection.Name)]
public sealed class GetMyCompetencyTests(ApiFactory api)
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = GetMyListsTests.Json;

    private async Task<(HttpClient Client, MyListsResponse Lists)> SignInAsync(string email)
    {
        var client = api.CreateClient();
        await client.LoginAsync(email);
        return (client, (await client.GetFromJsonAsync<MyListsResponse>("/api/me/lists", Json))!);
    }

    private static Guid IdOf(MyListsResponse lists, string code) =>
        lists.Lists.SelectMany(l => l.Nodes).First(n => n.Competency?.Code == code).Competency!.CompetencyId;

    private static async Task<MyCompetencyResponse> GetAsync(HttpClient client, Guid competencyId) =>
        (await client.GetFromJsonAsync<MyCompetencyResponse>($"/api/me/competencies/{competencyId}", Json))!;

    [Fact]
    public async Task Anonymous_request_returns_401()
    {
        var response = await api.CreateClient().GetAsync($"/api/me/competencies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_competency_outside_the_candidates_lists_returns_404()
    {
        var (_, lists) = await SignInAsync("candidate01@skillcert.test");
        var cpr = IdOf(lists, "4.3.1");
        var admin = api.CreateClient();
        await admin.LoginAsync("admin@skillcert.test"); // in no group, so no required lists

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/me/competencies/{cpr}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/me/competencies/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Overview_has_content_policy_path_resources_and_the_effective_review()
    {
        var (client, lists) = await SignInAsync("candidate01@skillcert.test");

        var cpr = await GetAsync(client, IdOf(lists, "4.3.1"));

        Assert.Equal(("4.3.1", "One-rescuer adult CPR"), (cpr.Code, cpr.ShortTitle));
        Assert.NotNull(cpr.Description);
        Assert.Equal(CurrencyStatus.Current, cpr.Currency.Status);
        Assert.Equal((1, (int?)365), (cpr.Revision.Number, cpr.Revision.RecertificationDays));
        Assert.Equal("Instructor", cpr.LowestReviewer.ClassificationCode);
        var path = Assert.Single(cpr.PartOf);
        Assert.Equal("AFA Skills Record", path.ListTitle);
        Assert.Equal(2, path.Headings.Count);
        Assert.StartsWith("4 ", path.Headings[0], StringComparison.Ordinal);
        Assert.Equal([ResourceType.Video, ResourceType.WebPage, ResourceType.Document], cpr.Resources.Select(r => r.Type));
        var effective = cpr.EffectiveReview!;
        Assert.Equal((ReviewMethod.Classified, "Instructor"), (effective.Method, effective.ClassificationCode));
        Assert.Equal(cpr.Currency.AchievedAt, effective.ReviewedAt);
        Assert.NotEmpty(effective.SignedWith); // the whole section was signed in one sitting
        Assert.DoesNotContain(effective.SignedWith, s => s.Code == "4.3.1");
        Assert.Null(cpr.PendingReview);
    }

    [Fact]
    public async Task History_shows_a_breaking_revision_between_the_old_and_new_reviews()
    {
        var (client, lists) = await SignInAsync("candidate01@skillcert.test");

        var aed = await GetAsync(client, IdOf(lists, "4.4.1"));

        Assert.Equal(2, aed.Revision.Number);
        Assert.Equal(
            [MyHistoryEntryKind.Review, MyHistoryEntryKind.Invalidated, MyHistoryEntryKind.Review],
            aed.History.Select(h => h.Kind));
        Assert.Equal(2, aed.History[0].Review!.RevisionNumber);
        Assert.Equal(2, aed.History[1].RevisionNumber);
        Assert.Equal(1, aed.History[2].Review!.RevisionNumber);
    }

    [Fact]
    public async Task History_shows_when_an_unrenewed_skill_expired()
    {
        var (client, lists) = await SignInAsync("candidate03@skillcert.test");

        var cpr = await GetAsync(client, IdOf(lists, "4.3.1"));

        Assert.Equal(CurrencyStatus.Expired, cpr.Currency.Status);
        Assert.Equal(MyHistoryEntryKind.Expired, cpr.History[0].Kind);
        Assert.Equal(cpr.Currency.ExpiresAt, cpr.History[0].At);
    }

    [Fact]
    public async Task A_pending_claim_is_reported_without_changing_the_status()
    {
        var (client, lists) = await SignInAsync("candidate05@skillcert.test");
        var pendingId = lists.Lists.SelectMany(l => l.Nodes)
            .First(n => n.Competency?.Currency.HasPendingReview == true).Competency!.CompetencyId;

        var detail = await GetAsync(client, pendingId);

        var pending = detail.PendingReview!;
        Assert.Equal(ConfirmationStatus.Pending, pending.ConfirmationStatus);
        Assert.Equal("Supervisor", pending.ClassificationCode);
        Assert.NotEqual(CurrencyStatus.Current, detail.Currency.Status);
    }

    [Fact]
    public async Task A_candidate_can_fetch_their_own_signature_but_not_anyone_elses()
    {
        var (client, lists) = await SignInAsync("candidate01@skillcert.test");
        var signatureId = (await GetAsync(client, IdOf(lists, "4.3.1"))).EffectiveReview!.SignatureId!.Value;

        var own = await client.GetAsync($"/api/me/signatures/{signatureId}");
        var (other, _) = await SignInAsync("candidate02@skillcert.test");
        var someoneElses = await other.GetAsync($"/api/me/signatures/{signatureId}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal("image/svg+xml", own.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("<svg", await own.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Contains("default-src 'none'", own.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, someoneElses.StatusCode);
    }
}
