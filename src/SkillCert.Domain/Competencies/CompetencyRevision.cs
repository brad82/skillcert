namespace SkillCert.Domain.Competencies;

/// <summary>
/// One published version of a competency's content and policy (spec §3). Retained forever for audit;
/// only the newest revision receives new assessments, and the server picks it, never the client.
/// </summary>
public sealed class CompetencyRevision
{
    private readonly List<RevisionResource> _resources = [];
    private readonly List<RevisionPermittedClassification> _permittedClassifications = [];

    private CompetencyRevision()
    {
    }

    internal CompetencyRevision(
        Guid competencyId,
        int revisionNumber,
        RevisionContent content,
        bool invalidatesPreviousReviews,
        DateTimeOffset publishedAt,
        Guid? publishedByUserId)
    {
        content.Validate();
        Id = Guid.CreateVersion7(publishedAt);
        CompetencyId = competencyId;
        RevisionNumber = revisionNumber;
        InvalidatesPreviousReviews = invalidatesPreviousReviews;
        PublishedAt = publishedAt;
        PublishedByUserId = publishedByUserId;
        Apply(content);
    }

    public Guid Id { get; private set; }

    public Guid CompetencyId { get; private set; }

    public int RevisionNumber { get; private set; }

    public string Title { get; private set; } = null!;

    public string? ShortTitle { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Null means no time-based expiry. Arithmetic is elapsed days, never calendar years.</summary>
    public int? RecertificationDays { get; private set; }

    public bool AllowsSelfReview { get; private set; }

    public bool AllowsPeerReview { get; private set; }

    /// <summary>
    /// A breaking revision: reviews against earlier revisions stop counting from <see cref="PublishedAt"/>.
    /// Always false for the first revision.
    /// </summary>
    public bool InvalidatesPreviousReviews { get; private set; }

    public DateTimeOffset PublishedAt { get; private set; }

    public Guid? PublishedByUserId { get; private set; }

    public IReadOnlyList<RevisionResource> Resources => _resources;

    public IReadOnlyCollection<RevisionPermittedClassification> PermittedClassifications => _permittedClassifications;

    public bool PermitsClassification(Guid classificationId) =>
        _permittedClassifications.Any(p => p.ReviewerClassificationId == classificationId);

    /// <summary>
    /// Editorial correction of this revision (spec §3 "Administrative edits"): no new revision, no invalidation.
    /// Callers record the change in the audit log.
    /// </summary>
    public void CorrectEditorially(RevisionContent content)
    {
        content.Validate();
        Apply(content);
    }

    private void Apply(RevisionContent content)
    {
        Title = content.Title.Trim();
        ShortTitle = string.IsNullOrWhiteSpace(content.ShortTitle) ? null : content.ShortTitle.Trim();
        Description = string.IsNullOrWhiteSpace(content.Description) ? null : content.Description.Trim();
        RecertificationDays = content.RecertificationDays;
        AllowsSelfReview = content.AllowsSelfReview;
        AllowsPeerReview = content.AllowsPeerReview;

        // Fresh instances: content is often built from another revision's resources, and each revision owns its own.
        _resources.Clear();
        _resources.AddRange(content.Resources.Select(r => new RevisionResource(r.Title, r.Url, r.Type)));

        _permittedClassifications.RemoveAll(p => !content.PermittedClassificationIds.Contains(p.ReviewerClassificationId));
        foreach (var classificationId in content.PermittedClassificationIds.Distinct())
        {
            if (!PermitsClassification(classificationId))
            {
                _permittedClassifications.Add(new RevisionPermittedClassification(Id, classificationId));
            }
        }
    }
}

/// <summary>A reviewer classification allowed to sign off a revision.</summary>
public sealed class RevisionPermittedClassification
{
    private RevisionPermittedClassification()
    {
    }

    internal RevisionPermittedClassification(Guid competencyRevisionId, Guid reviewerClassificationId)
    {
        CompetencyRevisionId = competencyRevisionId;
        ReviewerClassificationId = reviewerClassificationId;
    }

    public Guid CompetencyRevisionId { get; private set; }

    public Guid ReviewerClassificationId { get; private set; }
}
