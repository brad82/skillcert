using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillCert.Api.Archival;
using SkillCert.Api.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;

namespace SkillCert.Api.Tests.Features.MyRecord;

/// <summary>Archives candidate01's compliant record with the real archiver, then removes it again.</summary>
[Collection(ApiCollection.Name)]
public sealed class GetMyArchivedRecordsTests(ApiFactory api)
{
    private const string Candidate = "candidate01@skillcert.test";

    private async Task<Guid> CandidateIdAsync()
    {
        Guid id = default;
        await api.WithDbAsync(async db => id = await db.DomainUsers.Where(u => u.Email == Candidate).Select(u => u.Id).SingleAsync());
        return id;
    }

    [Fact]
    public async Task A_candidate_lists_and_downloads_their_archives_and_nobody_else_can()
    {
        var candidateId = await CandidateIdAsync();
        try
        {
            await using (var scope = api.Services.CreateAsyncScope())
            {
                var result = await scope.ServiceProvider.GetRequiredService<TrainingRecordArchiver>().RunForUsersAsync([candidateId], CancellationToken.None);
                Assert.Equal(1, result.Archived);
            }

            var client = api.CreateClient();
            await client.LoginAsync(Candidate);
            var archives = (await client.GetFromJsonAsync<MyArchivedRecordsResponse>("/api/me/records", GetMyListsTests.Json))!.Records;
            var archive = Assert.Single(archives);
            Assert.Equal("AFA Skills Record", archive.ListTitle);

            var download = await client.GetAsync($"/api/me/records/{archive.Id}");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("application/pdf", download.Content.Headers.ContentType!.MediaType);
            Assert.Equal(api.Blobs.Blobs.Single().Value, await download.Content.ReadAsByteArrayAsync());

            var other = api.CreateClient();
            await other.LoginAsync("candidate02@skillcert.test");
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/me/records/{archive.Id}")).StatusCode);
            Assert.Empty((await other.GetFromJsonAsync<MyArchivedRecordsResponse>("/api/me/records", GetMyListsTests.Json))!.Records);
        }
        finally
        {
            await api.WithDbAsync(async db =>
            {
                await db.TrainingRecords.Where(r => r.UserId == candidateId).ExecuteDeleteAsync();
                await db.ComplianceCheckpoints.Where(c => c.UserId == candidateId).ExecuteDeleteAsync();
            });
            api.Blobs.Blobs.Clear();
        }
    }
}
