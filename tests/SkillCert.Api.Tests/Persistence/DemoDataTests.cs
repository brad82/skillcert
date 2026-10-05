using Microsoft.EntityFrameworkCore;
using SkillCert.Api.Tests.Infrastructure;
using SkillCert.Domain.Currency;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Reviews;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Api.Tests.Persistence;

/// <summary>The demo seed builds the real AFA record and puts every currency and confirmation state on screen.</summary>
[Collection(ApiCollection.Name)]
public sealed class DemoDataTests(ApiFactory api)
{
    [Fact]
    public async Task The_AFA_record_is_seeded_complete_and_in_paper_order()
    {
        await api.WithDbAsync(async db =>
        {
            var list = await db.CompetencyLists.Include(l => l.Nodes).SingleAsync(l => l.Title == "AFA Skills Record");
            var codes = await db.Competencies.ToDictionaryAsync(c => c.Id, c => c.Code);

            Assert.Equal(63, list.CompetencyIds.Count());
            Assert.Equal(23, list.Nodes.Count(n => n.Kind == ListNodeKind.Heading));
            Assert.Equal(["3", "4", "5", "6", "7", "8", "9", "10", "11", "12"], list.ChildrenOf(null).Select(n => n.HeadingCode));
            var orderedCodes = list.CompetencyIds.Select(id => codes[id]).ToList();
            Assert.Equal("3.1", orderedCodes[0]);
            Assert.Equal("12.2", orderedCodes[^1]);
            Assert.Equal(["10.2e", "10.2f", "10.2g", "10.2h", "10.2i"], orderedCodes.SkipWhile(c => c != "10.2e").Take(5));
        });
    }

    [Fact]
    public async Task Each_demo_candidate_shows_the_state_their_profile_promises()
    {
        await api.WithDbAsync(async db =>
        {
            var currency = await CurrencyByCandidateAsync(db, DateTimeOffset.UtcNow);

            Assert.All(currency["candidate01"], c => Assert.Equal(CurrencyStatus.Current, c.Status));

            Assert.All(currency["candidate02"].Where(c => c.Reason != CurrencyReason.RevisionInvalidated), c =>
            {
                Assert.Equal(CurrencyStatus.Current, c.Status);
                Assert.True(c.ExpiresAt < DateTimeOffset.UtcNow.AddDays(30), "expiring within 30 days");
            });

            Assert.Contains(currency["candidate03"], c => c.Status == CurrencyStatus.Expired);
            Assert.DoesNotContain(currency["candidate03"], c => c.Status == CurrencyStatus.Current);

            Assert.Contains(currency["candidate04"], c => c.Status == CurrencyStatus.NotCompetent);
            Assert.Contains(currency["candidate04"], c => c.Reason == CurrencyReason.NeverReviewed);

            Assert.Contains(currency["candidate05"], c => c.HasPendingReview);

            var everyone = currency.Values.SelectMany(c => c).ToList();
            Assert.Equal(Enum.GetValues<CurrencyStatus>().Order(), everyone.Select(c => c.Status).Distinct().Order());
            Assert.Equal(Enum.GetValues<CurrencyReason>().Order(), everyone.Select(c => c.Reason).Distinct().Order());
        });
    }

    [Fact]
    public async Task Every_confirmation_state_is_present()
    {
        await api.WithDbAsync(async db =>
        {
            var states = await db.CompetencyReviews.Select(r => r.ConfirmationStatus).Distinct().ToListAsync();

            Assert.Equal(Enum.GetValues<ConfirmationStatus>().Order(), states.Order());
        });
    }

    /// <summary>Currency for every AFA competency, per demo candidate (keyed by the email's local part).</summary>
    private static async Task<Dictionary<string, List<CompetencyCurrency>>> CurrencyByCandidateAsync(
        SkillCertDbContext db, DateTimeOffset asOf)
    {
        var list = await db.CompetencyLists.Include(l => l.Nodes).SingleAsync(l => l.Title == "AFA Skills Record");
        var competencyIds = list.CompetencyIds.ToList();
        var revisions = (await db.Competencies.Include(c => c.Revisions).Where(c => competencyIds.Contains(c.Id)).ToListAsync())
            .ToDictionary(
                c => c.Id,
                c => (IReadOnlyCollection<RevisionPolicy>)c.Revisions
                    .Select(r => new RevisionPolicy(r.Id, r.RevisionNumber, r.RecertificationDays, r.InvalidatesPreviousReviews, r.PublishedAt))
                    .ToList());
        var candidates = await db.DomainUsers.Where(u => u.Email.StartsWith("candidate")).ToListAsync();
        var reviews = await db.CompetencyReviews.ToListAsync();

        return candidates.ToDictionary(
            u => u.Email.Split('@')[0],
            u => competencyIds
                .Select(id => CurrencyEvaluator.Evaluate(
                    reviews.Where(r => r.CandidateUserId == u.Id && r.CompetencyId == id).Select(ReviewEvidence.From),
                    revisions[id],
                    asOf))
                .ToList());
    }
}
