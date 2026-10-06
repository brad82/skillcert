using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Common;
using SkillCert.Api.Features.AdminCompetencies;
using SkillCert.Api.Tests.Features.MyRecord;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Reviews;

namespace SkillCert.Api.Tests.Features.AdminCompetencies;

/// <summary>Each test creates its own competency (code "T-…"), so seeded skills and their currency stay untouched.</summary>
[Collection(ApiCollection.Name)]
public sealed class AdminCompetenciesTests(ApiFactory api)
{
    private static readonly JsonSerializerOptions Json = GetMyListsTests.Json;

    private static RevisionContentRequest Content(string title = "Test skill", int? days = 365, ReviewMethod lowest = ReviewMethod.Self, string? classification = null) =>
        new(title, null, "Description.", days, new ReviewLevelRequest(lowest, classification), [new("Guide", "https://example.org/guide", ResourceType.WebPage)]);

    private async Task<HttpClient> AdminAsync()
    {
        var client = api.CreateClient();
        await client.LoginAsync("admin@skillcert.test");
        return client;
    }

    private static async Task<AdminCompetencyDto> CreateAsync(HttpClient admin, RevisionContentRequest? content = null)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/competencies", new CreateCompetencyRequest($"T-{Guid.NewGuid():N}"[..12], content ?? Content()), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdminCompetencyDto>(Json))!;
    }

    /// <summary>candidate02 signs the competency off themselves 10 days ago (the test skills permit Self).</summary>
    private Task SelfSignAsync(Guid competencyId) => api.WithDbAsync(async db =>
    {
        var user = await db.DomainUsers.SingleAsync(u => u.Email == "candidate02@skillcert.test");
        var competency = await db.Competencies.Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications).SingleAsync(c => c.Id == competencyId);
        var at = DateTimeOffset.UtcNow.AddDays(-10);
        db.CompetencyReviews.Add(CompetencyReview.Record(user.Id, competency, ReviewOutcome.Competent, at, Reviewer.Self(user), null, null, null, at, user.Id));
        await db.SaveChangesAsync();
    });

    private async Task<CompetencyCurrency> CurrencyAsync(Guid competencyId)
    {
        CompetencyCurrency currency = null!;
        await api.WithDbAsync(async db =>
        {
            var user = await db.DomainUsers.SingleAsync(u => u.Email == "candidate02@skillcert.test");
            var competency = await db.Competencies.Include(c => c.Revisions).AsNoTracking().SingleAsync(c => c.Id == competencyId);
            var reviews = await db.CompetencyReviews.Where(r => r.CompetencyId == competencyId && r.CandidateUserId == user.Id).ToListAsync();
            currency = CurrencyEvaluator.Evaluate(reviews.Select(ReviewEvidence.From), CandidateRecord.Policies(competency), DateTimeOffset.UtcNow);
        });
        return currency;
    }

    private async Task<List<string>> AuditActionsAsync(Guid competencyId)
    {
        List<string> actions = [];
        await api.WithDbAsync(async db => actions = await db.AuditEntries.Where(e => e.EntityId == competencyId).OrderBy(e => e.At).Select(e => e.Action).ToListAsync());
        return actions;
    }

    [Fact]
    public async Task Non_administrators_get_403()
    {
        var instructor = api.CreateClient();
        await instructor.LoginAsync("instructor1@skillcert.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await instructor.GetAsync("/api/admin/competencies")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await instructor.PostAsJsonAsync("/api/admin/competencies", new CreateCompetencyRequest("X-1", Content()), Json)).StatusCode);
    }

    [Fact]
    public async Task Creating_stores_revision_one_closed_upward_and_a_taken_code_is_refused_after_normalising()
    {
        var admin = await AdminAsync();

        var created = await CreateAsync(admin, Content(lowest: ReviewMethod.Classified, classification: "Instructor"));
        var duplicate = await admin.PostAsJsonAsync("/api/admin/competencies", new CreateCompetencyRequest($"  {created.Code.ToLowerInvariant()} ", Content()), Json);
        var seeded = await admin.PostAsJsonAsync("/api/admin/competencies", new CreateCompetencyRequest("4.3.1", Content()), Json);

        Assert.Equal((1, "Instructor"), (created.Current.Number, created.Current.LowestReviewer.ClassificationCode));
        Assert.Equal(["Guide"], created.Current.Resources.Select(r => r.Title));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(CreateCompetencyEndpoint.DuplicateCodeType, (await seeded.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        Assert.Equal(["competency.create"], await AuditActionsAsync(created.Id));
    }

    [Fact]
    public async Task A_breaking_revision_ends_currency_and_a_non_breaking_one_keeps_it()
    {
        var admin = await AdminAsync();
        var kept = await CreateAsync(admin);
        var broken = await CreateAsync(admin);
        await SelfSignAsync(kept.Id);
        await SelfSignAsync(broken.Id);

        var nonBreaking = await admin.PostAsJsonAsync($"/api/admin/competencies/{kept.Id}/revisions", new PublishRevisionRequest(Content(days: 730), false), Json);
        var breaking = await admin.PostAsJsonAsync($"/api/admin/competencies/{broken.Id}/revisions", new PublishRevisionRequest(Content(), true), Json);

        var keptDto = (await nonBreaking.Content.ReadFromJsonAsync<AdminCompetencyDto>(Json))!;
        Assert.Equal((2, 730), (keptDto.Current.Number, keptDto.Current.RecertificationDays));
        Assert.Equal([2, 1], keptDto.Revisions.Select(r => r.Number));
        Assert.Equal(CurrencyStatus.Current, (await CurrencyAsync(kept.Id)).Status);
        Assert.Equal(365, (int)((await CurrencyAsync(kept.Id)).ExpiresAt!.Value - (await CurrencyAsync(kept.Id)).AchievedAt!.Value).TotalDays); // its own revision's interval

        Assert.Equal(HttpStatusCode.OK, breaking.StatusCode);
        var invalidated = await CurrencyAsync(broken.Id);
        Assert.Equal((CurrencyStatus.NotCertified, CurrencyReason.RevisionInvalidated), (invalidated.Status, invalidated.Reason));
        Assert.Equal(["competency.create", "competency.publish-revision"], await AuditActionsAsync(broken.Id));
    }

    [Fact]
    public async Task An_editorial_edit_keeps_currency_but_policy_changes_need_a_new_revision()
    {
        var admin = await AdminAsync();
        var created = await CreateAsync(admin);
        await SelfSignAsync(created.Id);

        var edited = await admin.PutAsJsonAsync($"/api/admin/competencies/{created.Id}/revision", Content(title: "Test skill, corrected"), Json);
        var recertChange = await admin.PutAsJsonAsync($"/api/admin/competencies/{created.Id}/revision", Content(days: 90), Json);
        var reviewerChange = await admin.PutAsJsonAsync($"/api/admin/competencies/{created.Id}/revision", Content(lowest: ReviewMethod.Peer), Json);

        var dto = (await edited.Content.ReadFromJsonAsync<AdminCompetencyDto>(Json))!;
        Assert.Equal((1, "Test skill, corrected"), (dto.Current.Number, dto.Current.Title));
        Assert.Equal(CurrencyStatus.Current, (await CurrencyAsync(created.Id)).Status);
        Assert.Equal(EditCurrentRevisionEndpoint.PolicyChangeType, (await recertChange.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        Assert.Equal(HttpStatusCode.Conflict, reviewerChange.StatusCode);
        Assert.Equal(["competency.create", "competency.edit-revision"], await AuditActionsAsync(created.Id));
    }

    [Fact]
    public async Task Deactivating_blocks_new_sign_offs_and_the_library_lists_it_inactive()
    {
        var admin = await AdminAsync();
        var created = await CreateAsync(admin);

        await admin.PutAsJsonAsync($"/api/admin/competencies/{created.Id}/active", new SetCompetencyActiveRequest(false), Json);
        var library = (await admin.GetFromJsonAsync<AdminCompetenciesResponse>($"/api/admin/competencies?search={created.Code}", Json))!;

        Assert.False(Assert.Single(library.Competencies).IsActive);
        var refused = await Record.ExceptionAsync(() => SelfSignAsync(created.Id));
        Assert.Equal(CompetencyReview.CompetencyInactive, Assert.IsType<SkillCert.Domain.DomainRuleException>(refused).Code);
    }

    [Fact]
    public async Task Invalid_content_is_refused_field_by_field()
    {
        var admin = await AdminAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/competencies", new CreateCompetencyRequest("",
            new RevisionContentRequest("", null, null, 0, new ReviewLevelRequest(ReviewMethod.Classified, null), [new("x", "ftp://nope", ResourceType.Video)])), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys;
        Assert.Contains("code", errors);
        Assert.Contains("content.Title", errors, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("content.RecertificationDays", errors, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("content.LowestReviewer.ClassificationCode", errors, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("content.Resources[0].Url", errors, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_new_competency_can_be_placed_in_a_list_in_one_step_and_a_bad_placement_creates_nothing()
    {
        var admin = await AdminAsync();
        var list = (await (await admin.PostAsJsonAsync("/api/admin/lists", new SkillCert.Api.Features.AdminLists.ListDetailsRequest($"Place {Guid.NewGuid():N}"[..16], null), Json))
            .Content.ReadFromJsonAsync<SkillCert.Api.Features.AdminLists.AdminListDto>(Json))!;
        list = (await (await admin.PostAsJsonAsync($"/api/admin/lists/{list.Id}/headings", new SkillCert.Api.Features.AdminLists.AddHeadingRequest(null, "1", "Section", null), Json))
            .Content.ReadFromJsonAsync<SkillCert.Api.Features.AdminLists.AdminListDto>(Json))!;
        var heading = list.Nodes.Single().Id;
        var code = $"T-{Guid.NewGuid():N}"[..12];

        var placed = await admin.PostAsJsonAsync("/api/admin/competencies",
            new CreateCompetencyRequest(code, Content(), new SkillCert.Api.Admin.ListPlacementRequest(list.Id, heading, null)), Json);
        var badCode = $"T-{Guid.NewGuid():N}"[..12];
        var nowhere = await admin.PostAsJsonAsync("/api/admin/competencies",
            new CreateCompetencyRequest(badCode, Content(), new SkillCert.Api.Admin.ListPlacementRequest(Guid.NewGuid(), null, null)), Json);

        var dto = (await placed.Content.ReadFromJsonAsync<AdminCompetencyDto>(Json))!;
        Assert.Equal([list.Id], dto.Lists.Select(l => l.Id));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, nowhere.StatusCode);
        Assert.Equal(SkillCert.Api.Admin.ListPlacement.ListNotFoundType, (await nowhere.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        var exists = true;
        await api.WithDbAsync(async db => exists = await db.Competencies.AnyAsync(c => c.Code == badCode));
        Assert.False(exists);
    }
}
