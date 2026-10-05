using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Tests.Infrastructure;

namespace SkillCert.Api.Tests.Features.Auth;

[Collection(ApiCollection.Name)]
public sealed class LoginTests(ApiFactory api)
{
    [Fact]
    public async Task Valid_credentials_return_204_and_a_strict_http_only_auth_cookie()
    {
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = "candidate01@skillcert.test", password = ApiFactory.DemoPassword });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("skillcert.auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_body_returns_400_with_errors_per_field()
    {
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Theory]
    [InlineData("candidate02@skillcert.test", "wrong-password-123")]
    [InlineData("nobody@skillcert.test", ApiFactory.DemoPassword)]
    public async Task Wrong_password_or_unknown_email_both_return_a_bare_401(string email, string password)
    {
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Deactivated_user_cannot_sign_in()
    {
        const string email = "candidate10@skillcert.test";
        await SetActiveAsync(email, false);
        try
        {
            var response = await api.CreateClient().PostAsJsonAsync(
                "/api/auth/login", new { email, password = ApiFactory.DemoPassword });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await SetActiveAsync(email, true);
        }
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var client = api.CreateClient();
        await client.LoginAsync("candidate03@skillcert.test");

        var logout = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    private Task SetActiveAsync(string email, bool active) =>
        api.WithDbAsync(async db =>
        {
            var user = await db.DomainUsers.SingleAsync(u => u.Email == email);
            if (active)
            {
                user.Reactivate();
            }
            else
            {
                user.Deactivate();
            }

            await db.SaveChangesAsync();
        });
}
