using System.Net;
using System.Net.Http.Json;

namespace SkillCert.Api.Tests.Infrastructure;

public static class HttpClientExtensions
{
    /// <summary>Signs the client in; its cookie container keeps the auth cookie for later calls.</summary>
    public static async Task LoginAsync(this HttpClient client, string email, string password = ApiFactory.DemoPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
