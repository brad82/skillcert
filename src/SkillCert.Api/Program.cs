using SkillCert.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<SkillCertDbContext>(
    SkillCertDbContextOptions.ConnectionName,
    configureDbContextOptions: SkillCertDbContextOptions.Configure);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
