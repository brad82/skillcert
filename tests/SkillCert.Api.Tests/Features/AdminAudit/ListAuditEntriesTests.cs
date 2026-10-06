using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Features.AdminAudit;
using SkillCert.Api.Features.AdminLists;
using SkillCert.Api.Tests.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Audit;

namespace SkillCert.Api.Tests.Features.AdminAudit;

[Collection(ApiCollection.Name)]
public sealed class ListAuditEntriesTests(ApiFactory api)
{
    [Fact]
    public async Task Entries_are_filtered_labelled_and_paged_newest_first()
    {
        var admin = api.CreateClient();
        await admin.LoginAsync("admin@skillcert.test");
        var title = $"Audited {Guid.NewGuid():N}"[..18];
        var created = (await (await admin.PostAsJsonAsync("/api/admin/lists", new ListDetailsRequest(title, null))).Content.ReadFromJsonAsync<AdminListDto>(GetMyListsTests.Json))!;
        await admin.PutAsJsonAsync($"/api/admin/lists/{created.Id}", new ListDetailsRequest(title, "Now described"));

        var forList = (await admin.GetFromJsonAsync<AuditEntriesResponse>($"/api/admin/audit?entityType=CompetencyList&entityId={created.Id}", GetMyListsTests.Json))!;

        Assert.Equal(["list.rename", "list.create"], forList.Entries.Select(e => e.Action));
        Assert.All(forList.Entries, e => Assert.Equal((title, "Alex Admin"), (e.EntityLabel, e.ActorName)));
        Assert.Equal("Now described", forList.Entries[0].After!.Value.GetProperty("after").GetProperty("description").GetString());
        Assert.Contains("CompetencyList", forList.EntityTypes);
        Assert.Contains(forList.Actors, a => a.Name == "Alex Admin");

        // 55 more entries for the same entity → two pages.
        await api.WithDbAsync(async db =>
        {
            var adminId = await db.DomainUsers.Where(u => u.Email == "admin@skillcert.test").Select(u => u.Id).SingleAsync();
            var start = DateTimeOffset.UtcNow.AddMinutes(-10);
            db.AuditEntries.AddRange(Enumerable.Range(0, 55).Select(i => AuditEntry.Record(adminId, "list.rename", "CompetencyList", created.Id, null, "{}", start.AddSeconds(i))));
            await db.SaveChangesAsync();
        });
        var first = (await admin.GetFromJsonAsync<AuditEntriesResponse>($"/api/admin/audit?entityId={created.Id}", GetMyListsTests.Json))!;
        var cursor = Uri.EscapeDataString(first.Entries[^1].At.ToString("O"));
        var second = (await admin.GetFromJsonAsync<AuditEntriesResponse>($"/api/admin/audit?entityId={created.Id}&before={cursor}", GetMyListsTests.Json))!;

        Assert.True(first.HasMore);
        Assert.Equal(ListAuditEntriesEndpoint.PageSize, first.Entries.Count);
        Assert.False(second.HasMore);
        Assert.Equal(57, first.Entries.Count + second.Entries.Count);
    }
}
