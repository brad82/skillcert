using Microsoft.EntityFrameworkCore;
using SkillCert.Domain.Competencies;
using SkillCert.Domain.Lists;
using SkillCert.Domain.Reviewers;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Users;
using SkillCert.Infrastructure.Persistence;

namespace SkillCert.Infrastructure.Seed;

/// <summary>
/// Generates review history for the demo candidates against the AFA Skills Record so every currency state
/// and confirmation state is on screen (development plan §1.5). Deterministic: fixed profiles for candidates
/// 01–05 and a fixed random seed for 06–10, with dates relative to "now". Sign-offs are given a section at a
/// time, so each interaction shares one ReviewedAt and one signature, as a real candidate-device sign-off would.
/// Skips if any review exists.
/// </summary>
public sealed class DemoHistorySeeder(SkillCertDbContext db, TimeProvider timeProvider)
{
    private const string AedCode = "4.4.1";
    private const int RandomSeed = 42;

    private DateTimeOffset _now;
    private Dictionary<Guid, Competency> _competencies = [];
    private ReviewerClassification _instructor = null!;
    private ReviewerClassification _supervisor = null!;
    private int _signatureCount;

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.CompetencyReviews.AnyAsync(cancellationToken))
        {
            return 0;
        }

        var list = await db.CompetencyLists.Include(l => l.Nodes).SingleOrDefaultAsync(l => l.Title == "AFA Skills Record", cancellationToken);
        if (list is null)
        {
            return 0;
        }

        _now = timeProvider.GetUtcNow();
        _competencies = await db.Competencies
            .Include(c => c.Revisions).ThenInclude(r => r.PermittedClassifications)
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        _instructor = await db.ReviewerClassifications.SingleAsync(c => c.Id == ReviewerClassification.InstructorId, cancellationToken);
        _supervisor = await db.ReviewerClassifications.SingleAsync(c => c.Id == ReviewerClassification.SupervisorId, cancellationToken);
        var users = await db.DomainUsers.Include(u => u.Classifications).ToDictionaryAsync(u => u.Email, cancellationToken);
        User U(string email) => users[$"{email}@skillcert.test"];

        // Each top-level section of the record, with every competency under it in display order.
        var sections = list.ChildrenOf(null)
            .Select(section => list.Walk(section.Id).Where(n => n.Kind == ListNodeKind.Competency).Select(n => _competencies[n.CompetencyId!.Value]).ToList())
            .ToList();

        var instructors = new[] { U("instructor1"), U("instructor2") };
        var supervisor1 = U("supervisor1");
        var supervisor2 = U("supervisor2");

        // 01: everything current.
        for (var i = 0; i < sections.Count; i++)
        {
            Sign(U("candidate01"), instructors[0], _instructor, sections[i], DaysAgo(30 + (i * 5)));
        }

        // 02: everything expiring within about two weeks (365-day recertification).
        for (var i = 0; i < sections.Count; i++)
        {
            Sign(U("candidate02"), instructors[1], _instructor, sections[i], DaysAgo(350 + i));
        }

        // 03: everything expired.
        for (var i = 0; i < sections.Count; i++)
        {
            Sign(U("candidate03"), instructors[0], _instructor, sections[i], DaysAgo(380 + (i * 3)));
        }

        // 04: mostly current; one section re-assessed Not Competent; one confirmed by a supervisor; two never reviewed.
        var c04 = U("candidate04");
        for (var i = 0; i <= 5; i++)
        {
            Sign(c04, instructors[1], _instructor, sections[i], DaysAgo(60));
        }

        Sign(c04, instructors[0], _instructor, sections[6], DaysAgo(200));
        Sign(c04, instructors[1], _instructor, sections[6], DaysAgo(20), ReviewOutcome.NotCompetent);
        var confirmed = Sign(c04, supervisor1, _supervisor, sections[7], DaysAgo(40));
        confirmed.ForEach(r => r.Confirm(supervisor1.Id, DaysAgo(30)));

        // 05: some current; one section awaiting a supervisor; one rejected by a supervisor.
        var c05 = U("candidate05");
        for (var i = 0; i <= 4; i++)
        {
            Sign(c05, instructors[0], _instructor, sections[i], DaysAgo(90));
        }

        Sign(c05, supervisor1, _supervisor, sections[5], DaysAgo(2));
        var rejected = Sign(c05, supervisor2, _supervisor, sections[6], DaysAgo(9));
        rejected.ForEach(r => r.Reject(supervisor2.Id, DaysAgo(5), "Not observed at this session. Please redo at the next practice night."));

        // 06–10: a seeded mix of current, expiring, expired, pending and never reviewed, section by section.
        var random = new Random(RandomSeed);
        for (var n = 6; n <= 10; n++)
        {
            var candidate = U($"candidate{n:00}");
            foreach (var section in sections)
            {
                var reviewer = instructors[random.Next(instructors.Length)];
                switch (random.Next(5))
                {
                    case 0: Sign(candidate, reviewer, _instructor, section, DaysAgo(random.Next(10, 300))); break;
                    case 1: Sign(candidate, reviewer, _instructor, section, DaysAgo(random.Next(345, 360))); break;
                    case 2: Sign(candidate, reviewer, _instructor, section, DaysAgo(random.Next(370, 500))); break;
                    case 3: Sign(candidate, supervisor1, _supervisor, section, DaysAgo(random.Next(1, 10))); break;
                    default: break; // never reviewed
                }
            }
        }

        // A breaking AED revision ten days ago invalidates everyone's earlier AED sign-off;
        // candidate 01 has already been re-assessed against it.
        var aed = _competencies.Values.Single(c => c.Code == AedCode);
        var current = aed.CurrentRevision;
        aed.PublishRevision(
            new RevisionContent(
                current.Title, current.ShortTitle, "Updated for the new AED model and pad placement.",
                current.RecertificationDays, current.AllowsSelfReview, current.AllowsPeerReview,
                current.PermittedClassifications.Select(p => p.ReviewerClassificationId).ToList(), current.Resources),
            invalidatesPreviousReviews: true,
            DaysAgo(10),
            byUserId: null);
        Sign(U("candidate01"), instructors[1], _instructor, [aed], DaysAgo(3));

        await db.SaveChangesAsync(cancellationToken);
        return await db.CompetencyReviews.CountAsync(cancellationToken);
    }

    private DateTimeOffset DaysAgo(int days) => _now.AddDays(-days);

    /// <summary>One candidate-device sign-off: every competency in the set, one ReviewedAt, one signature.</summary>
    private List<CompetencyReview> Sign(
        User candidate,
        User reviewerUser,
        ReviewerClassification classification,
        IEnumerable<Competency> competencies,
        DateTimeOffset reviewedAt,
        ReviewOutcome outcome = ReviewOutcome.Competent)
    {
        var signature = ReviewSignature.FromServerRenderedSvg(SignatureSvg(_signatureCount++), reviewedAt);
        db.ReviewSignatures.Add(signature);
        var reviewer = Reviewer.Classified(reviewerUser, classification);

        var reviews = competencies
            .Select(competency => CompetencyReview.Record(
                candidate.Id, competency, outcome, reviewedAt, reviewer, signature.Id,
                comment: null, opportunityId: null, createdAt: reviewedAt, createdByUserId: candidate.Id))
            .ToList();
        db.CompetencyReviews.AddRange(reviews);
        return reviews;
    }

    /// <summary>A plausible scribble, different per interaction. Placeholder until real captured signatures (Phase 2).</summary>
    private static string SignatureSvg(int n)
    {
        var a = 20 + (n * 7 % 40);
        var b = 80 - (n * 11 % 50);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 300 100\">"
            + $"<path d=\"M10 {b} C 50 {a}, 90 {100 - a}, 130 {b} S 210 {a}, 290 {100 - b}\" fill=\"none\" stroke=\"#1f1a1b\" stroke-width=\"3\" stroke-linecap=\"round\"/></svg>";
    }
}
