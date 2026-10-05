using System.Net;
using System.Net.Http.Json;
using SkillCert.Api.Features.CurrentUser;
using SkillCert.Api.Tests.Infrastructure;

namespace SkillCert.Api.Tests.Features.CurrentUser;

[Collection(ApiCollection.Name)]
public sealed class GetCurrentUserTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_request_returns_401()
    {
        var response = await api.CreateClient().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("admin@skillcert.test", "Alex Admin", new[] { "Administrator" })]
    [InlineData("instructor1@skillcert.test", "Ines Instructor", new[] { "Instructor" })]
    [InlineData("supervisor2@skillcert.test", "Sofia Supervisor", new[] { "Supervisor" })]
    [InlineData("candidate05@skillcert.test", "Candidate 05", new string[0])]
    public async Task Returns_the_signed_in_user_with_their_capabilities(
        string email, string displayName, string[] capabilities)
    {
        var client = api.CreateClient();
        await client.LoginAsync(email);

        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/me");

        Assert.NotNull(me);
        Assert.Equal(displayName, me.DisplayName);
        Assert.Equal(email, me.Email);
        Assert.Equal(capabilities, me.Capabilities);
    }
}
