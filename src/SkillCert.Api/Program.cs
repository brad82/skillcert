using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using SkillCert.Api.Features.Auth;
using SkillCert.Api.Common;
using SkillCert.Api.Features.CurrentUser;
using SkillCert.Api.Features.MyRecord;
using SkillCert.Api.Features.SignOffs;
using SkillCert.Infrastructure.Identity;
using SkillCert.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<SkillCertDbContext>(
    SkillCertDbContextOptions.ConnectionName,
    configureDbContextOptions: SkillCertDbContextOptions.Configure);

var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SkillCert");
// The build also starts the app (without a database) to write the OpenAPI document; only persist keys when there is one.
if (builder.Configuration.GetConnectionString(SkillCertDbContextOptions.ConnectionName) is not null)
{
    dataProtection.PersistKeysToDbContext<SkillCertDbContext>();
}

// Caddy terminates TLS and proxies to the API on the compose network; trust its forwarded scheme and client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.AddSkillCertIdentityCore().AddSignInManager();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "skillcert.auth";
    options.Cookie.HttpOnly = true;
    // The SPA is served from the same origin as the API (Vite proxy locally, Caddy on the demo).
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    // JSON API: answer with status codes, never redirect to a login page.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Enums as names; numbers as numbers only (otherwise OpenAPI types every int as number | string).
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapAuthFeature();
app.MapCurrentUserFeature();
app.MapMyRecordFeature();
app.MapSignOffsFeature();

app.Run();

public partial class Program;
