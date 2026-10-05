using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SkillCert.Api.Archival;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Groups;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Records;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Users;
using SkillCert.Infrastructure.Persistence;
using SkillCert.Infrastructure.Storage;

namespace SkillCert.Api.Tests.Archival;

/// <summary>
/// Spec §22 with a fake clock: each test builds its own user, two-skill list and group, then runs the archiver for
/// that user only, night by night.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TrainingRecordArchiverTests(ApiFactory api)
{
    /// <summary>2 a.m. on the first test night.</summary>
    private static readonly DateTimeOffset Night1 = new(2027, 3, 10, 2, 0, 0, TimeSpan.Zero);

    /// <summary>A candidate whose only requirement is a fresh two-skill list that allows self sign-off.</summary>
    private sealed record Scenario(Guid UserId, Guid ListId, Guid[] CompetencyIds);

    private readonly FakeTimeProvider _clock = new(Night1);
    private readonly MemoryBlobStore _blobs = new();

    private async Task<Scenario> ArrangeAsync()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var setUpAt = Night1.AddDays(-400);
        Scenario scenario = null!;
        await api.WithDbAsync(async db =>
        {
            var classifications = await db.ReviewerClassifications.ToListAsync();
            var user = new User($"archive-{tag}", $"Archive {tag}", $"archive-{tag}@skillcert.test", setUpAt);
            var skills = Enumerable.Range(1, 2).Select(n => Competency.Create(
                $"ARC-{tag}-{n}",
                ReviewHierarchy.CloseUpward(new RevisionContent($"Archive skill {n}", null, null, 365, true, false, [], []), classifications),
                setUpAt,
                null)).ToList();
            var list = new CompetencyList($"Archive list {tag}", null, setUpAt);
            var heading = list.AddHeading(null, "1", "Section");
            skills.ForEach(s => list.AddCompetency(heading.Id, s.Id));
            var group = new UserGroup($"Archive group {tag}", null, setUpAt);
            group.AssignList(list.Id, setUpAt);
            group.AddMember(user.Id, setUpAt);
            db.AddRange(user, list, group);
            db.Competencies.AddRange(skills);
            await db.SaveChangesAsync();
            scenario = new Scenario(user.Id, list.Id, skills.Select(s => s.Id).ToArray());
        });
        return scenario;
    }

    private Task SignAsync(Scenario scenario, DateTimeOffset at, ReviewOutcome outcome = ReviewOutcome.Competent, int? onlySkill = null) =>
        api.WithDbAsync(async db =>
        {
            var user = await db.DomainUsers.SingleAsync(u => u.Id == scenario.UserId);
            var ids = onlySkill is { } index ? [scenario.CompetencyIds[index]] : scenario.CompetencyIds;
            foreach (var competency in await db.Competencies.Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications).Where(c => ids.Contains(c.Id)).ToListAsync())
            {
                db.CompetencyReviews.Add(CompetencyReview.Record(user.Id, competency, outcome, at, Reviewer.Self(user), null, null, null, at, user.Id));
            }

            await db.SaveChangesAsync();
        });

    private async Task<ArchivalRunResult> RunNightAsync(Scenario scenario, DateTimeOffset night)
    {
        _clock.SetUtcNow(night);
        await using var scope = api.Services.CreateAsyncScope();
        var archiver = new TrainingRecordArchiver(
            scope.ServiceProvider.GetRequiredService<SkillCertDbContext>(), _blobs, _clock, NullLogger<TrainingRecordArchiver>.Instance);
        return await archiver.RunForUsersAsync([scenario.UserId], CancellationToken.None);
    }

    private async Task<List<TrainingRecord>> ArchivesAsync(Scenario scenario)
    {
        List<TrainingRecord> records = [];
        await api.WithDbAsync(async db => records = await db.TrainingRecords.Where(r => r.UserId == scenario.UserId).OrderBy(r => r.GeneratedAt).ToListAsync());
        return records;
    }

    private async Task<ComplianceCheckpoint?> CheckpointAsync(Scenario scenario)
    {
        ComplianceCheckpoint? checkpoint = null;
        await api.WithDbAsync(async db => checkpoint = await db.ComplianceCheckpoints.SingleOrDefaultAsync(c => c.UserId == scenario.UserId));
        return checkpoint;
    }

    [Fact]
    public async Task Compliant_three_nights_running_archives_once_and_a_same_night_rerun_adds_nothing()
    {
        var scenario = await ArrangeAsync();
        await SignAsync(scenario, Night1.AddDays(-20), onlySkill: 0);
        await SignAsync(scenario, Night1.AddDays(-5), onlySkill: 1);

        Assert.Equal(1, (await RunNightAsync(scenario, Night1)).Archived);
        Assert.Equal(0, (await RunNightAsync(scenario, Night1.AddMinutes(30))).Archived);
        Assert.Equal(0, (await RunNightAsync(scenario, Night1.AddDays(1))).Archived);
        Assert.Equal(0, (await RunNightAsync(scenario, Night1.AddDays(2))).Archived);

        var archive = Assert.Single(await ArchivesAsync(scenario));
        Assert.Equal(Night1.AddDays(-5), archive.CompletionDate); // when the last skill became current
        Assert.Equal((Night1, TrainingRecordTrigger.ComplianceAchieved), (archive.GeneratedAt, archive.Trigger));
        var pdf = _blobs.Blobs[archive.BlobPath];
        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(pdf)), archive.Sha256Hash);
    }

    [Fact]
    public async Task Losing_and_regaining_compliance_archives_again_with_the_new_completion_date()
    {
        var scenario = await ArrangeAsync();
        await SignAsync(scenario, Night1.AddDays(-10));
        await RunNightAsync(scenario, Night1);

        await SignAsync(scenario, Night1.AddHours(10), ReviewOutcome.NotCompetent, onlySkill: 0);
        Assert.Equal(0, (await RunNightAsync(scenario, Night1.AddDays(1))).Archived);
        Assert.False((await CheckpointAsync(scenario))!.IsCompliant);

        await SignAsync(scenario, Night1.AddDays(1).AddHours(10), onlySkill: 0);
        Assert.Equal(1, (await RunNightAsync(scenario, Night1.AddDays(2))).Archived);

        Assert.Equal([Night1.AddDays(-10), Night1.AddDays(1).AddHours(10)], (await ArchivesAsync(scenario)).Select(a => a.CompletionDate));
    }

    [Fact]
    public async Task A_failed_upload_leaves_no_archive_and_the_next_night_retries_with_the_evidence_date()
    {
        var scenario = await ArrangeAsync();
        await SignAsync(scenario, Night1.AddDays(-3));
        _blobs.FailNextPut = true;

        var failed = await RunNightAsync(scenario, Night1);

        Assert.Equal((0, 1), (failed.Archived, failed.Failed));
        Assert.Empty(await ArchivesAsync(scenario));
        Assert.Null(await CheckpointAsync(scenario));

        Assert.Equal(1, (await RunNightAsync(scenario, Night1.AddDays(1))).Archived);
        Assert.Equal(Night1.AddDays(-3), Assert.Single(await ArchivesAsync(scenario)).CompletionDate);
    }

    [Fact]
    public async Task Continuing_compliance_keeps_the_established_completion_date_through_a_reassessment()
    {
        var scenario = await ArrangeAsync();
        await SignAsync(scenario, Night1.AddDays(-30));
        await RunNightAsync(scenario, Night1);

        await SignAsync(scenario, Night1.AddHours(12), onlySkill: 1);
        await RunNightAsync(scenario, Night1.AddDays(1));

        Assert.Single(await ArchivesAsync(scenario));
        var checkpoint = (await CheckpointAsync(scenario))!;
        Assert.Equal((true, Night1.AddDays(-30)), (checkpoint.IsCompliant, checkpoint.CompletionDate));
    }

    [Fact]
    public async Task Without_blob_storage_the_run_is_skipped()
    {
        await using var scope = api.Services.CreateAsyncScope();
        var archiver = new TrainingRecordArchiver(
            scope.ServiceProvider.GetRequiredService<SkillCertDbContext>(), new UnconfiguredBlobStore(), _clock, NullLogger<TrainingRecordArchiver>.Instance);

        Assert.True((await archiver.RunAsync(CancellationToken.None)).Skipped);
    }

    [Theory]
    [InlineData("2027-03-10T01:00:00Z", "2027-03-10T02:00:00Z")] // later today
    [InlineData("2027-03-10T02:00:00Z", "2027-03-11T02:00:00Z")] // exactly now → tomorrow
    [InlineData("2027-03-10T03:00:00Z", "2027-03-11T02:00:00Z")]
    public void The_next_run_is_the_next_local_run_time(string now, string expected)
    {
        var options = new ArchivalOptions(RunAt: new TimeOnly(2, 0));

        Assert.Equal(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), options.NextRun(DateTimeOffset.Parse(now, System.Globalization.CultureInfo.InvariantCulture), TimeZoneInfo.Utc));
    }
}
