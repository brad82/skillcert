using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Features.AdminImport;
using SkillCert.Api.Tests.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;

namespace SkillCert.Api.Tests.Features.AdminImport;

[Collection(ApiCollection.Name)]
public sealed class CompetencyImportTests(ApiFactory api)
{
    private const string Header = "Code,Title,RecertificationDays,PeerReview,InstructorReview";

    private async Task<HttpClient> AdminAsync()
    {
        var client = api.CreateClient();
        await client.LoginAsync("admin@skillcert.test");
        return client;
    }

    private static string Codes(int count, out string[] codes)
    {
        var tag = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        codes = Enumerable.Range(1, count).Select(n => $"IMP-{tag}-{n}").ToArray();
        return string.Join('\n', codes.Select(c => $"{c},Imported {c},365,,Y"));
    }

    private async Task<int> CountAsync(IEnumerable<string> codes)
    {
        var normalized = codes.Select(c => c.Trim().ToUpperInvariant()).ToList();
        var count = 0;
        await api.WithDbAsync(async db => count = await db.Competencies.CountAsync(c => normalized.Contains(c.NormalizedCode)));
        return count;
    }

    [Fact]
    public async Task A_clean_file_previews_then_imports_every_row_atomically_with_audit()
    {
        var admin = await AdminAsync();
        var csv = $"{Header}\n{Codes(3, out var codes)}";

        var preview = await (await admin.PostAsJsonAsync("/api/admin/imports/competencies/preview", new CompetencyImportRequest(csv))).Content
            .ReadFromJsonAsync<CompetencyImportPreviewResponse>(GetMyListsTests.Json);
        Assert.True(preview!.CanImport);
        Assert.Equal(0, await CountAsync(codes));

        var imported = await admin.PostAsJsonAsync("/api/admin/imports/competencies", new CompetencyImportRequest(csv));

        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        Assert.Equal(3, (await imported.Content.ReadFromJsonAsync<CompetencyImportResponse>())!.Created);
        Assert.Equal(3, await CountAsync(codes));
        var audited = 0;
        await api.WithDbAsync(async db => audited = await db.AuditEntries.CountAsync(e => e.Action == "competency.import"
            && db.Competencies.Any(c => c.Id == e.EntityId && codes.Contains(c.Code))));
        Assert.Equal(3, audited);
    }

    [Fact]
    public async Task A_file_with_one_duplicate_imports_nothing()
    {
        var admin = await AdminAsync();
        var csv = $"{Header}\n{Codes(3, out var codes)}\n{codes[1]},Again,365,,Y";

        var response = await admin.PostAsJsonAsync("/api/admin/imports/competencies", new CompetencyImportRequest(csv));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<CompetencyImportPreviewResponse>(GetMyListsTests.Json))!;
        Assert.Equal([3, 5], preview.Rows.Where(r => r.Errors.Count > 0).Select(r => r.Line));
        Assert.Equal(0, await CountAsync(codes));
    }

    [Fact]
    public async Task A_code_already_in_the_library_blocks_the_whole_file_even_if_inactive()
    {
        var admin = await AdminAsync();
        var csv = $"{Header}\n{Codes(2, out var codes)}\n4.3.1,Clash,365,Y,";

        var response = await admin.PostAsJsonAsync("/api/admin/imports/competencies", new CompetencyImportRequest(csv));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await CountAsync(codes));
    }

    [Fact]
    public async Task Non_administrators_cannot_preview_or_import()
    {
        var candidate = api.CreateClient();
        await candidate.LoginAsync("candidate01@skillcert.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await candidate.PostAsJsonAsync("/api/admin/imports/competencies/preview", new CompetencyImportRequest("Code,Title"))).StatusCode);
    }

    [Fact]
    public async Task An_import_can_go_straight_into_a_list_and_returns_the_new_ids()
    {
        var admin = await AdminAsync();
        var list = (await (await admin.PostAsJsonAsync("/api/admin/lists", new SkillCert.Api.Features.AdminLists.ListDetailsRequest($"Import {Guid.NewGuid():N}"[..17], null)))
            .Content.ReadFromJsonAsync<SkillCert.Api.Features.AdminLists.AdminListDto>(GetMyListsTests.Json))!;
        var csv = $"{Header}\n{Codes(2, out var codes)}";

        var badPreview = await (await admin.PostAsJsonAsync("/api/admin/imports/competencies/preview",
            new CompetencyImportRequest(csv, new SkillCert.Api.Admin.ListPlacementRequest(Guid.NewGuid(), null, null)))).Content
            .ReadFromJsonAsync<CompetencyImportPreviewResponse>(GetMyListsTests.Json);
        var imported = await admin.PostAsJsonAsync("/api/admin/imports/competencies",
            new CompetencyImportRequest(csv, new SkillCert.Api.Admin.ListPlacementRequest(list.Id, null, null)));

        Assert.False(badPreview!.CanImport);
        Assert.Equal(["That list doesn't exist."], badPreview.FileErrors);
        var result = (await imported.Content.ReadFromJsonAsync<CompetencyImportResponse>())!;
        Assert.Equal(codes, result.Competencies.Select(c => c.Code));
        var tree = (await admin.GetFromJsonAsync<SkillCert.Api.Features.AdminLists.AdminListDto>($"/api/admin/lists/{list.Id}", GetMyListsTests.Json))!;
        Assert.Equal(result.Competencies.Select(c => c.Id), tree.Nodes.Select(n => n.Competency!.Id));
    }
}
