using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Groups;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Tests.Persistence;

/// <summary>
/// The Phase 1 model against real Postgres: aggregates round-trip, and the database enforces the
/// rules the domain enforces, even when something bypasses the domain (raw SQL, races).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CompetencyModelPersistenceTests(ApiFactory api)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static string UniqueCode() => $"T-{Guid.NewGuid():N}"[..12];

    private static RevisionContent Content(int? days = 365, params Guid[] classifications) => new(
        "CPR – one-rescuer adult", "One-rescuer adult CPR", "Adult CPR with one rescuer.", days,
        AllowsSelfReview: false, AllowsPeerReview: true,
        classifications.Length == 0 ? [ReviewerClassification.InstructorId] : classifications,
        [new RevisionResource("Heart & Stroke guide", new Uri("https://example.org/cpr"), ResourceType.WebPage)]);

    private async Task<Competency> SavedCompetencyAsync(params Guid[] classifications)
    {
        var competency = Competency.Create(UniqueCode(), Content(classifications: classifications), Now, null);
        await api.WithDbAsync(async db =>
        {
            db.Competencies.Add(competency);
            await db.SaveChangesAsync();
        });
        return competency;
    }

    private static async Task<Guid> UserIdAsync(SkillCertDbContext db, string email) =>
        await db.DomainUsers.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();

    private static async Task<string> SqlStateOfAsync(Func<Task> action)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(action);
        var postgres = error as PostgresException ?? error.InnerException as PostgresException;
        Assert.NotNull(postgres);
        return postgres.SqlState;
    }

    [Fact]
    public async Task A_competency_round_trips_with_revisions_resources_and_permitted_methods()
    {
        var competency = await SavedCompetencyAsync(ReviewerClassification.InstructorId, ReviewerClassification.SupervisorId);
        await api.WithDbAsync(async db =>
        {
            var tracked = await db.Competencies.Include(c => c.Revisions).SingleAsync(c => c.Id == competency.Id);
            tracked.PublishRevision(Content(days: 730), invalidatesPreviousReviews: true, Now.AddDays(1), null);
            await db.SaveChangesAsync();
        });

        await api.WithDbAsync(async db =>
        {
            var loaded = await db.Competencies
                .Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications)
                .SingleAsync(c => c.Id == competency.Id);

            Assert.Equal([1, 2], loaded.Revisions.Select(r => r.RevisionNumber).Order());
            var first = loaded.Revisions.Single(r => r.RevisionNumber == 1);
            Assert.Equal("One-rescuer adult CPR", first.ShortTitle);
            Assert.True(first.AllowsPeerReview);
            Assert.True(first.PermitsClassification(ReviewerClassification.SupervisorId));
            var resource = Assert.Single(first.Resources);
            Assert.Equal(new Uri("https://example.org/cpr"), resource.Url);
            Assert.Equal(ResourceType.WebPage, resource.Type);
            Assert.Equal(730, loaded.CurrentRevision.RecertificationDays);
            Assert.True(loaded.CurrentRevision.InvalidatesPreviousReviews);
        });
    }

    [Fact]
    public async Task Competency_codes_are_unique_after_normalization()
    {
        var code = UniqueCode();
        await api.WithDbAsync(async db =>
        {
            db.Competencies.Add(Competency.Create(code.ToLowerInvariant(), Content(), Now, null));
            await db.SaveChangesAsync();
        });

        var state = await SqlStateOfAsync(() => api.WithDbAsync(async db =>
        {
            db.Competencies.Add(Competency.Create($" {code.ToUpperInvariant()} ", Content(), Now, null));
            await db.SaveChangesAsync();
        }));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, state);
    }

    [Fact]
    public async Task A_list_tree_round_trips_in_display_order()
    {
        var a = await SavedCompetencyAsync();
        var b = await SavedCompetencyAsync();
        var list = new CompetencyList($"List {Guid.NewGuid():N}", null, Now);
        var section = list.AddHeading(null, "4", "Basic Life Support");
        var sub = list.AddHeading(section.Id, "4.3", "CPR");
        list.AddCompetency(sub.Id, b.Id);
        list.AddCompetency(sub.Id, a.Id, index: 0);
        await api.WithDbAsync(async db =>
        {
            db.CompetencyLists.Add(list);
            await db.SaveChangesAsync();
        });

        await api.WithDbAsync(async db =>
        {
            var loaded = await db.CompetencyLists.Include(l => l.Nodes).SingleAsync(l => l.Id == list.Id);
            Assert.Equal([a.Id, b.Id], loaded.CompetencyIds);
            Assert.Equal(["4", "4.3"], loaded.Walk().Where(n => n.Kind == ListNodeKind.Heading).Select(n => n.HeadingCode));
        });
    }

    [Fact]
    public async Task Two_editors_adding_the_same_competency_cannot_both_succeed()
    {
        var competency = await SavedCompetencyAsync();
        var list = new CompetencyList($"List {Guid.NewGuid():N}", null, Now);
        await api.WithDbAsync(async db =>
        {
            db.CompetencyLists.Add(list);
            await db.SaveChangesAsync();
        });

        // Both editors load the list before either saves, so the domain check passes for both.
        await using var first = api.Services.CreateAsyncScope();
        await using var second = api.Services.CreateAsyncScope();
        var firstDb = first.ServiceProvider.GetRequiredService<SkillCertDbContext>();
        var secondDb = second.ServiceProvider.GetRequiredService<SkillCertDbContext>();
        var firstCopy = await firstDb.CompetencyLists.Include(l => l.Nodes).SingleAsync(l => l.Id == list.Id);
        var secondCopy = await secondDb.CompetencyLists.Include(l => l.Nodes).SingleAsync(l => l.Id == list.Id);
        firstCopy.AddCompetency(null, competency.Id);
        secondCopy.AddCompetency(null, competency.Id);

        await firstDb.SaveChangesAsync();

        Assert.Equal(PostgresErrorCodes.UniqueViolation, await SqlStateOfAsync(() => secondDb.SaveChangesAsync()));
    }

    [Fact]
    public async Task The_database_refuses_a_child_under_a_competency_leaf_or_a_parent_from_another_list()
    {
        var competency = await SavedCompetencyAsync();
        var list = new CompetencyList($"List {Guid.NewGuid():N}", null, Now);
        var leaf = list.AddCompetency(null, competency.Id);
        var other = new CompetencyList($"Other {Guid.NewGuid():N}", null, Now);
        var otherHeading = other.AddHeading(null, "1", "Elsewhere");
        await api.WithDbAsync(async db =>
        {
            db.CompetencyLists.AddRange(list, other);
            await db.SaveChangesAsync();
        });

        Task InsertHeadingUnder(Guid parentId) => api.WithDbAsync(db => db.Database.ExecuteSqlAsync($"""
            INSERT INTO competency_list_nodes (id, competency_list_id, parent_node_id, kind, heading_title, sort_order)
            VALUES ({Guid.NewGuid()}, {list.Id}, {parentId}, 'Heading', 'Sneaky', 0)
            """));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, await SqlStateOfAsync(() => InsertHeadingUnder(leaf.Id)));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, await SqlStateOfAsync(() => InsertHeadingUnder(otherHeading.Id)));
    }

    [Fact]
    public async Task The_database_refuses_a_node_that_is_both_or_neither_heading_and_competency()
    {
        var competency = await SavedCompetencyAsync();
        var list = new CompetencyList($"List {Guid.NewGuid():N}", null, Now);
        await api.WithDbAsync(async db =>
        {
            db.CompetencyLists.Add(list);
            await db.SaveChangesAsync();
        });

        var state = await SqlStateOfAsync(() => api.WithDbAsync(db => db.Database.ExecuteSqlAsync($"""
            INSERT INTO competency_list_nodes (id, competency_list_id, kind, heading_title, competency_id, sort_order)
            VALUES ({Guid.NewGuid()}, {list.Id}, 'Competency', 'Both', {competency.Id}, 0)
            """)));

        Assert.Equal(PostgresErrorCodes.CheckViolation, state);
    }

    [Fact]
    public async Task A_group_round_trips_members_and_assigned_lists()
    {
        var list = new CompetencyList($"List {Guid.NewGuid():N}", null, Now);
        var group = new UserGroup($"Group {Guid.NewGuid():N}", null, Now);
        await api.WithDbAsync(async db =>
        {
            group.AddMember(await UserIdAsync(db, "candidate04@skillcert.test"), Now);
            group.AssignList(list.Id, Now);
            db.CompetencyLists.Add(list);
            db.UserGroups.Add(group);
            await db.SaveChangesAsync();
        });

        await api.WithDbAsync(async db =>
        {
            var loaded = await db.UserGroups.Include(g => g.Members).Include(g => g.AssignedLists).SingleAsync(g => g.Id == group.Id);
            Assert.Single(loaded.Members);
            Assert.Equal(list.Id, Assert.Single(loaded.AssignedLists).CompetencyListId);
        });
    }

    [Fact]
    public async Task A_supervisor_claim_round_trips_through_confirmation_with_a_shared_signature()
    {
        var competency = await SavedCompetencyAsync(ReviewerClassification.SupervisorId);
        Guid reviewId = default;
        await api.WithDbAsync(async db =>
        {
            var candidate = await db.DomainUsers.SingleAsync(u => u.Email == "candidate06@skillcert.test");
            var supervisor = await db.DomainUsers.Include(u => u.Classifications).SingleAsync(u => u.Email == "supervisor1@skillcert.test");
            var classification = await db.ReviewerClassifications.SingleAsync(c => c.Id == ReviewerClassification.SupervisorId);
            var tracked = await db.Competencies.Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications).SingleAsync(c => c.Id == competency.Id);
            var signature = ReviewSignature.FromServerRenderedSvg("<svg xmlns=\"http://www.w3.org/2000/svg\"/>", Now);
            var review = CompetencyReview.Record(
                candidate.Id, tracked, ReviewOutcome.Competent, Now, Reviewer.Classified(supervisor, classification),
                signature.Id, "Good compressions", null, Now, candidate.Id);
            db.ReviewSignatures.Add(signature);
            db.CompetencyReviews.Add(review);
            await db.SaveChangesAsync();
            reviewId = review.Id;

            review.Confirm(supervisor.Id, Now.AddDays(14));
            await db.SaveChangesAsync();
        });

        await api.WithDbAsync(async db =>
        {
            var loaded = await db.CompetencyReviews.SingleAsync(r => r.Id == reviewId);
            Assert.Equal(ConfirmationStatus.Confirmed, loaded.ConfirmationStatus);
            Assert.Equal(Now, loaded.ReviewedAt);
            Assert.Equal("Sam Supervisor", loaded.ReviewerName);
            Assert.NotNull(loaded.ReviewSignatureId);
        });
    }

    [Fact]
    public async Task The_database_refuses_inconsistent_confirmation_fields_and_a_revision_from_another_competency()
    {
        var competency = await SavedCompetencyAsync();
        var otherCompetency = await SavedCompetencyAsync();
        Guid reviewId = default;
        await api.WithDbAsync(async db =>
        {
            var candidate = await db.DomainUsers.SingleAsync(u => u.Email == "candidate07@skillcert.test");
            var instructor = await db.DomainUsers.Include(u => u.Classifications).SingleAsync(u => u.Email == "instructor2@skillcert.test");
            var classification = await db.ReviewerClassifications.SingleAsync(c => c.Id == ReviewerClassification.InstructorId);
            var tracked = await db.Competencies.Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications).SingleAsync(c => c.Id == competency.Id);
            var review = CompetencyReview.Record(
                candidate.Id, tracked, ReviewOutcome.Competent, Now, Reviewer.Classified(instructor, classification),
                null, null, null, Now, candidate.Id);
            db.CompetencyReviews.Add(review);
            await db.SaveChangesAsync();
            reviewId = review.Id;
        });

        Assert.Equal(PostgresErrorCodes.CheckViolation, await SqlStateOfAsync(() => api.WithDbAsync(db =>
            db.Database.ExecuteSqlAsync($"UPDATE competency_reviews SET confirmation_status = 'Confirmed' WHERE id = {reviewId}"))));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, await SqlStateOfAsync(() => api.WithDbAsync(db =>
            db.Database.ExecuteSqlAsync($"UPDATE competency_reviews SET competency_id = {otherCompetency.Id} WHERE id = {reviewId}"))));
    }
}
