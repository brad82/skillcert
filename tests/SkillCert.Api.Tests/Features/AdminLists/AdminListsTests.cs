using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Features.AdminLists;
using SkillCert.Api.Tests.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Lists;

namespace SkillCert.Api.Tests.Features.AdminLists;

/// <summary>Each test builds its own list from seeded library competencies; the AFA list is only read.</summary>
[Collection(ApiCollection.Name)]
public sealed class AdminListsTests(ApiFactory api)
{
    private static readonly JsonSerializerOptions Json = GetMyListsTests.Json;

    private async Task<HttpClient> AdminAsync()
    {
        var client = api.CreateClient();
        await client.LoginAsync("admin@skillcert.test");
        return client;
    }

    private async Task<Guid[]> CompetencyIdsAsync(params string[] codes)
    {
        Dictionary<string, Guid> ids = [];
        await api.WithDbAsync(async db => ids = await db.Competencies.Where(c => codes.Contains(c.Code)).ToDictionaryAsync(c => c.Code, c => c.Id));
        return codes.Select(code => ids[code]).ToArray();
    }

    private static async Task<AdminListDto> ReadAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AdminListDto>(Json))!;
    }

    private static async Task<string?> ProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Type;
    }

    [Fact]
    public async Task An_administrator_builds_a_tree_with_headings_competencies_and_moves()
    {
        var admin = await AdminAsync();
        var (ppe, assess, cpr) = await CompetencyIdsAsync("3.2", "3.1", "4.3.1") switch { var ids => (ids[0], ids[1], ids[2]) };

        var list = await ReadAsync(await admin.PostAsJsonAsync("/api/admin/lists", new ListDetailsRequest($"Refresher {Guid.NewGuid():N}"[..20], null), Json));
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new AddHeadingRequest(null, "1", "Basics", null), Json));
        var basics = list.Nodes.Single().Id;
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/competencies", new AddCompetenciesRequest(basics, [ppe, assess], null), Json));
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new AddHeadingRequest(basics, "1.1", "CPR", 0), Json));
        var cprHeading = list.Nodes.Single(n => n.HeadingTitle == "CPR").Id;
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/competencies", new AddCompetenciesRequest(cprHeading, [cpr], null), Json));
        var assessNode = list.Nodes.Single(n => n.Competency?.Id == assess).Id;
        list = await ReadAsync(await admin.PutAsJsonAsync($"/api/admin/lists/{list.Id}/nodes/{assessNode}/position", new MoveNodeRequest(basics, 0), Json));
        list = await ReadAsync(await admin.PutAsJsonAsync($"/api/admin/lists/{list.Id}/nodes/{basics}", new RenameHeadingRequest("1", "Basic skills"), Json));

        Assert.Equal(
            ["Basic skills", "3.1", "CPR", "4.3.1", "3.2"],
            list.Nodes.Select(n => n.HeadingTitle ?? n.Competency!.Code));
        Assert.Equal([0, 1, 1, 2, 1], list.Nodes.Select(n => n.Depth));

        List<string> actions = [];
        await api.WithDbAsync(async db => actions = await db.AuditEntries.Where(e => e.EntityId == list.Id).OrderBy(e => e.At).Select(e => e.Action).ToListAsync());
        Assert.Equal(
            ["list.create", "list.add-heading", "list.add-competencies", "list.add-heading", "list.add-competencies", "list.move-node", "list.rename-heading"],
            actions);
    }

    [Fact]
    public async Task Duplicates_cycles_and_children_under_a_competency_are_refused()
    {
        var admin = await AdminAsync();
        var ppe = (await CompetencyIdsAsync("3.2"))[0];
        var list = await ReadAsync(await admin.PostAsJsonAsync("/api/admin/lists", new ListDetailsRequest($"Rules {Guid.NewGuid():N}"[..16], null), Json));
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new AddHeadingRequest(null, "1", "Outer", null), Json));
        var outer = list.Nodes.Single().Id;
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new AddHeadingRequest(outer, "1.1", "Inner", null), Json));
        var inner = list.Nodes.Single(n => n.HeadingTitle == "Inner").Id;
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/competencies", new AddCompetenciesRequest(inner, [ppe], null), Json));
        var leaf = list.Nodes.Single(n => n.Kind == ListNodeKind.Competency).Id;

        Assert.Equal(CompetencyList.DuplicateCompetency, await ProblemAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/competencies", new AddCompetenciesRequest(outer, [ppe], null), Json)));
        Assert.Equal(CompetencyList.Cycle, await ProblemAsync(await admin.PutAsJsonAsync($"/api/admin/lists/{list.Id}/nodes/{outer}/position", new MoveNodeRequest(inner, 0), Json)));
        Assert.Equal(CompetencyList.InvalidParent, await ProblemAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new AddHeadingRequest(leaf, null, "Under a leaf", null), Json)));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/lists/{list.Id}/nodes/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Removing_a_heading_removes_everything_under_it_and_leaves_reviews_alone()
    {
        var admin = await AdminAsync();
        var (ppe, assess) = await CompetencyIdsAsync("3.2", "3.1") switch { var ids => (ids[0], ids[1]) };
        var list = await ReadAsync(await admin.PostAsJsonAsync("/api/admin/lists", new ListDetailsRequest($"Remove {Guid.NewGuid():N}"[..17], null), Json));
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new AddHeadingRequest(null, "1", "Doomed", null), Json));
        var doomed = list.Nodes.Single().Id;
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new AddHeadingRequest(doomed, null, "Child", null), Json));
        var child = list.Nodes.Single(n => n.HeadingTitle == "Child").Id;
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/competencies", new AddCompetenciesRequest(child, [ppe], null), Json));
        list = await ReadAsync(await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/competencies", new AddCompetenciesRequest(null, [assess], null), Json));
        int reviewsBefore = 0, reviewsAfter = 0;
        await api.WithDbAsync(async db => reviewsBefore = await db.CompetencyReviews.CountAsync(r => r.CompetencyId == ppe));

        list = await ReadAsync(await admin.DeleteAsync($"/api/admin/lists/{list.Id}/nodes/{doomed}"));

        Assert.Equal("3.1", Assert.Single(list.Nodes).Competency!.Code);
        await api.WithDbAsync(async db => reviewsAfter = await db.CompetencyReviews.CountAsync(r => r.CompetencyId == ppe));
        Assert.Equal(reviewsBefore, reviewsAfter);
    }

    [Fact]
    public async Task The_seeded_afa_list_shows_its_size_and_groups()
    {
        var admin = await AdminAsync();

        var lists = (await admin.GetFromJsonAsync<AdminListsResponse>("/api/admin/lists", Json))!;

        var afa = lists.Lists.Single(l => l.Title == "AFA Skills Record");
        Assert.Equal(63, afa.CompetencyCount);
        Assert.Equal(["New Patroller", "Returning Patroller", "Senior Patroller"], afa.Groups);
    }
}
