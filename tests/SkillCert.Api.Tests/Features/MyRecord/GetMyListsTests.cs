using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SkillCert.Api.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Reviews;

namespace SkillCert.Api.Tests.Features.MyRecord;

[Collection(ApiCollection.Name)]
public sealed class GetMyListsTests(ApiFactory api)
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private async Task<MyListsResponse> GetAsync(string email)
    {
        var client = api.CreateClient();
        await client.LoginAsync(email);
        return (await client.GetFromJsonAsync<MyListsResponse>("/api/me/lists", Json))!;
    }

    [Fact]
    public async Task Anonymous_request_returns_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/me/lists")).StatusCode);
    }

    [Fact]
    public async Task A_candidate_gets_the_AFA_record_in_display_order_with_depths()
    {
        var afa = Assert.Single((await GetAsync("candidate01@skillcert.test")).Lists);

        Assert.Equal("AFA Skills Record", afa.Title);
        Assert.Equal(86, afa.Nodes.Count);
        Assert.Equal("3", afa.Nodes[0].HeadingCode);
        var cpr = afa.Nodes.Single(n => n.Competency?.Code == "4.3.1");
        Assert.Equal(2, cpr.Depth);
        Assert.Equal(ListNodeKind.Competency, cpr.Kind);
        Assert.Equal("One-rescuer adult CPR", cpr.Competency!.ShortTitle);
        Assert.Equal(ReviewMethod.Classified, cpr.Competency.LowestReviewer.Method);
        Assert.Equal("Instructor", cpr.Competency.LowestReviewer.ClassificationCode);
    }

    [Fact]
    public async Task Candidate01_is_fully_current_and_compliant()
    {
        var afa = Assert.Single((await GetAsync("candidate01@skillcert.test")).Lists);

        Assert.True(afa.IsCompliant);
        Assert.Equal(63, afa.Counts.Total);
        Assert.Equal(63, afa.Counts.Current);
    }

    [Fact]
    public async Task Counts_reflect_expired_not_competent_and_pending_profiles()
    {
        var c02 = Assert.Single((await GetAsync("candidate02@skillcert.test")).Lists);
        var c03 = Assert.Single((await GetAsync("candidate03@skillcert.test")).Lists);
        var c04 = Assert.Single((await GetAsync("candidate04@skillcert.test")).Lists);
        var c05 = Assert.Single((await GetAsync("candidate05@skillcert.test")).Lists);

        Assert.True(c02.Counts.ExpiringSoon > 0);
        Assert.False(c03.IsCompliant);
        Assert.True(c03.Counts.Expired > 0);
        Assert.True(c04.Counts.NotCompetent > 0);
        Assert.True(c05.Counts.Pending > 0);
        var pending = c05.Nodes.Where(n => n.Competency?.Currency.HasPendingReview == true).ToList();
        Assert.NotEmpty(pending);
        Assert.All(pending, n => Assert.Equal(CurrencyStatus.NotCertified, n.Competency!.Currency.Status));
    }

    [Fact]
    public async Task A_user_in_no_group_has_no_required_lists()
    {
        Assert.Empty((await GetAsync("admin@skillcert.test")).Lists);
    }
}
