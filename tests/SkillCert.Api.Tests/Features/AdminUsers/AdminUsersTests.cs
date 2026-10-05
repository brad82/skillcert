using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Features.AdminUsers;
using SkillCert.Api.Tests.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;

namespace SkillCert.Api.Tests.Features.AdminUsers;

[Collection(ApiCollection.Name)]
public sealed class AdminUsersTests(ApiFactory api)
{
    private static readonly JsonSerializerOptions Json = GetMyListsTests.Json;

    private async Task<HttpClient> SignInAsync(string email = "admin@skillcert.test")
    {
        var client = api.CreateClient();
        await client.LoginAsync(email);
        return client;
    }

    private async Task<Guid> UserIdAsync(string email)
    {
        Guid id = default;
        await api.WithDbAsync(async db => id = await db.DomainUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync());
        return id;
    }

    private async Task<List<(string Action, string? Before, string? After)>> AuditFor(Guid entityId)
    {
        List<(string, string?, string?)> entries = [];
        await api.WithDbAsync(async db => entries = (await db.AuditEntries.Where(e => e.EntityId == entityId).OrderBy(e => e.At).ToListAsync())
            .Select(e => (e.Action, e.Before, e.After)).ToList());
        return entries;
    }

    [Fact]
    public async Task Only_administrators_reach_admin_endpoints()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await SignInAsync("supervisor1@skillcert.test")).GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await (await SignInAsync()).GetAsync("/api/admin/users")).StatusCode);
    }

    [Fact]
    public async Task Users_are_listed_with_classifications_and_groups_and_can_be_searched()
    {
        var admin = await SignInAsync();

        var all = (await admin.GetFromJsonAsync<AdminUsersResponse>("/api/admin/users", Json))!;
        var found = (await admin.GetFromJsonAsync<AdminUsersResponse>("/api/admin/users?search=SUPERVISOR", Json))!;

        Assert.Equal(["Instructor", "Supervisor"], all.Classifications.Select(c => c.Code));
        var c07 = all.Users.Single(u => u.Email == "candidate07@skillcert.test");
        Assert.Equal(["Returning Patroller", "Senior Patroller"], c07.Groups);
        Assert.Equal(["Supervisor"], all.Users.Single(u => u.Email == "supervisor1@skillcert.test").Classifications);
        Assert.Equal(["Sam Supervisor", "Sofia Supervisor"], found.Users.Select(u => u.DisplayName));
    }

    [Fact]
    public async Task Deactivating_blocks_sign_in_is_audited_and_reactivating_restores_it()
    {
        var admin = await SignInAsync();
        var userId = await UserIdAsync("candidate10@skillcert.test");
        try
        {
            var deactivated = await admin.PutAsJsonAsync($"/api/admin/users/{userId}/active", new SetUserActiveRequest(false));
            Assert.False((await deactivated.Content.ReadFromJsonAsync<AdminUserDto>(Json))!.IsActive);
            var login = await api.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "candidate10@skillcert.test", password = ApiFactory.DemoPassword });
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

            await admin.PutAsJsonAsync($"/api/admin/users/{userId}/active", new SetUserActiveRequest(true));
            await SignInAsync("candidate10@skillcert.test");

            var audit = await AuditFor(userId);
            Assert.Equal(["user.deactivate", "user.reactivate"], audit.Select(a => a.Action));
            Assert.True(JsonDocument.Parse(audit[0].Before!).RootElement.GetProperty("isActive").GetBoolean());
            Assert.False(JsonDocument.Parse(audit[0].After!).RootElement.GetProperty("isActive").GetBoolean());
        }
        finally
        {
            await admin.PutAsJsonAsync($"/api/admin/users/{userId}/active", new SetUserActiveRequest(true));
        }
    }

    [Fact]
    public async Task An_administrator_cannot_deactivate_themselves()
    {
        var admin = await SignInAsync();
        var adminId = await UserIdAsync("admin@skillcert.test");

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{adminId}/active", new SetUserActiveRequest(false));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(SetUserActiveEndpoint.SelfDeactivationType, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
    }

    [Fact]
    public async Task Classifications_are_assigned_and_removed_with_an_audit_trail()
    {
        var admin = await SignInAsync();
        var userId = await UserIdAsync("candidate08@skillcert.test");
        try
        {
            var assigned = await admin.PutAsJsonAsync($"/api/admin/users/{userId}/classifications/Instructor", new SetUserClassificationRequest(true));
            Assert.Equal(["Instructor"], (await assigned.Content.ReadFromJsonAsync<AdminUserDto>(Json))!.Classifications);

            var removed = await admin.PutAsJsonAsync($"/api/admin/users/{userId}/classifications/Instructor", new SetUserClassificationRequest(false));
            Assert.Empty((await removed.Content.ReadFromJsonAsync<AdminUserDto>(Json))!.Classifications);

            Assert.Equal(["user.assign-classification", "user.remove-classification"], (await AuditFor(userId)).Select(a => a.Action));
            Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/admin/users/{userId}/classifications/Wizard", new SetUserClassificationRequest(true))).StatusCode);
        }
        finally
        {
            await admin.PutAsJsonAsync($"/api/admin/users/{userId}/classifications/Instructor", new SetUserClassificationRequest(false));
        }
    }
}
