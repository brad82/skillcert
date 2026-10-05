using System.Net;
using System.Net.Http.Json;
using SkillCert.Api.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;

namespace SkillCert.Api.Tests.Features.MyRecord;

[Collection(ApiCollection.Name)]
public sealed class GetMyCurrentRecordTests(ApiFactory api)
{
    /// <summary>Set to a folder to keep the rendered PDFs for a visual check against the paper record.</summary>
    private static readonly string? KeepPdfsIn = Environment.GetEnvironmentVariable("SKILLCERT_KEEP_PDFS");

    private async Task<(HttpClient Client, Guid ListId)> SignInAsync(string email)
    {
        var client = api.CreateClient();
        await client.LoginAsync(email);
        var lists = await client.GetFromJsonAsync<MyListsResponse>("/api/me/lists", GetMyListsTests.Json);
        return (client, lists!.Lists.Single().Id);
    }

    [Theory]
    [InlineData("candidate01@skillcert.test")]
    [InlineData("candidate04@skillcert.test")]
    public async Task A_candidate_downloads_their_record_as_a_pdf(string email)
    {
        var (client, listId) = await SignInAsync(email);

        var response = await client.GetAsync($"/api/me/lists/{listId}/record");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.EndsWith(".pdf", response.Content.Headers.ContentDisposition!.FileName!.Trim('"'), StringComparison.Ordinal);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
        if (KeepPdfsIn is not null)
        {
            await File.WriteAllBytesAsync(Path.Combine(KeepPdfsIn, $"{email.Split('@')[0]}.pdf"), bytes);
        }
    }

    [Fact]
    public async Task Lists_not_required_of_the_candidate_and_anonymous_requests_are_refused()
    {
        var (_, listId) = await SignInAsync("candidate01@skillcert.test");
        var admin = api.CreateClient();
        await admin.LoginAsync("admin@skillcert.test");

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/me/lists/{listId}/record")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync($"/api/me/lists/{listId}/record")).StatusCode);
    }
}
