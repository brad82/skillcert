using SkillCert.Domain.Competencies;
using SkillCert.Domain.Reviews;
using SkillCert.Domain.Users;

namespace SkillCert.Domain.Reviewers;

/// <summary>The lowest review level a revision accepts, e.g. "Instructor or higher".</summary>
public sealed record LowestReviewLevel(ReviewMethod Method, ReviewerClassification? Classification)
{
    /// <summary>Sort key: Self 0, Peer 1, then classification ranks (all above Peer).</summary>
    public int Order => Method switch
    {
        ReviewMethod.Self => 0,
        ReviewMethod.Peer => 1,
        _ => 1 + Classification!.Rank,
    };
}

/// <summary>
/// The review hierarchy Self &lt; Peer &lt; Instructor &lt; Supervisor (POC decision, development plan §2.5): a revision
/// that permits a level permits every higher one. Revisions store the closed set, so evaluation stays an exact
/// "is this method permitted" check and the stored data says plainly who may sign.
///
/// Every writer of revision content (seeding, admin editing, CSV import) passes it through
/// <see cref="CloseUpward"/> first.
/// </summary>
public static class ReviewHierarchy
{
    /// <summary>Adds every level above the lowest permitted one.</summary>
    public static RevisionContent CloseUpward(RevisionContent content, IReadOnlyCollection<ReviewerClassification> classifications)
    {
        if (content.AllowsSelfReview)
        {
            return content with { AllowsPeerReview = true, PermittedClassificationIds = classifications.Select(c => c.Id).ToList() };
        }

        if (content.AllowsPeerReview)
        {
            return content with { PermittedClassificationIds = classifications.Select(c => c.Id).ToList() };
        }

        var permittedRanks = classifications.Where(c => content.PermittedClassificationIds.Contains(c.Id)).Select(c => c.Rank).ToList();
        if (permittedRanks.Count == 0)
        {
            return content; // nothing permitted: RevisionContent validation reports it
        }

        var lowest = permittedRanks.Min();
        return content with { PermittedClassificationIds = classifications.Where(c => c.Rank >= lowest).Select(c => c.Id).ToList() };
    }

    /// <summary>
    /// How <paramref name="reviewerUser"/> would sign every one of <paramref name="revisions"/> for the candidate:
    /// the lowest level they hold that all of them permit. Self for the candidate themselves, Peer for anyone else,
    /// then their classifications by rank, so a Supervisor who is also an Instructor signs as Instructor and no
    /// confirmation is needed. Null when they can't sign them all, or aren't active.
    /// </summary>
    public static Reviewer? ReviewerFor(
        User reviewerUser,
        Guid candidateUserId,
        IReadOnlyCollection<CompetencyRevision> revisions,
        IReadOnlyCollection<ReviewerClassification> classifications)
    {
        if (!reviewerUser.IsActive || revisions.Count == 0)
        {
            return null;
        }

        if (reviewerUser.Id == candidateUserId)
        {
            return revisions.All(r => r.AllowsSelfReview) ? Reviewer.Self(reviewerUser) : null;
        }

        if (revisions.All(r => r.AllowsPeerReview))
        {
            return Reviewer.Peer(reviewerUser);
        }

        var held = reviewerUser.Classifications.Select(c => c.ReviewerClassificationId).ToHashSet();
        var lowest = classifications
            .Where(c => held.Contains(c.Id) && revisions.All(r => r.PermitsClassification(c.Id)))
            .MinBy(c => c.Rank);
        return lowest is null ? null : Reviewer.Classified(reviewerUser, lowest);
    }

    /// <summary>The lowest level a revision accepts, for grouping the basket and for "… or higher" labels.</summary>
    public static LowestReviewLevel Lowest(CompetencyRevision revision, IReadOnlyCollection<ReviewerClassification> classifications)
    {
        if (revision.AllowsSelfReview)
        {
            return new(ReviewMethod.Self, null);
        }

        if (revision.AllowsPeerReview)
        {
            return new(ReviewMethod.Peer, null);
        }

        var lowest = classifications
            .Where(c => revision.PermitsClassification(c.Id))
            .MinBy(c => c.Rank)
            ?? throw new InvalidOperationException($"Revision {revision.Id} permits no review method.");
        return new(ReviewMethod.Classified, lowest);
    }
}
